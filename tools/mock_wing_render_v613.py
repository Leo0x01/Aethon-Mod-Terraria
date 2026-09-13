#!/usr/bin/env python3
"""
mock_wing_render_v613.py — SIMULACIÓN ALPHA-BLEND DE LAS 8 ALAS NUEVAS v6.13.

Puerto fiel a Python de los 8 renderers C# reescritos con la técnica de las
coronas (WingStrokes: trazos, perlas, destellos 4 puntas, volúmenes) sobre
el compositor AlphaBlend del pase de jugador (out = dst·(1−a_eff) + tint·a_eff,
a_eff = perfil(textura)·alfa). Fondo: cielo de Terraria de día (el peor caso
para alas de luz) + silueta del jugador ENCIMA (las alas van detrás del cuerpo).

Verifica por ala: (1) ¿se lee como ALA? (2) anclaje al hombro, (3) silueta
legible contra el cielo, (4) técnica de coronas visible (trazos nítidos +
perlas + destellos), (5) reposo ≠ aleteo ≠ planeo.
"""
from PIL import Image
import numpy as np
import math

CW, CH = 300, 220
PI = math.pi
TAU = 2 * PI


def smoothstep(e0, e1, x):
    if isinstance(x, np.ndarray):
        t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
        return t * t * (3 - 2 * t)
    t = min(1.0, max(0.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def clamp(x, a, b):
    return a if x < a else (b if x > b else x)


def lerp(a, b, t):
    return a + (b - a) * t


def lerp3(c1, c2, t):
    return tuple(c1[k] + (c2[k] - c1[k]) * t for k in range(3))


def hash01(a, b, c=0):
    h = (a * 374761393 + b * 668265263 + c * 1911520717) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFFFF) / 16777216.0


# =====================================================================
#  COMPOSITOR ALPHA-BLEND (el modelo del pase de jugador)
# =====================================================================
def _profile(d, kind):
    if kind == "soft":
        return smoothstep(1.0, 0.0, d)
    if kind == "orb":
        core = (d < 0.55).astype(np.float64)
        return np.where(d < 0.55, 1.0, smoothstep(1.0, 0.55, d) * 0.96)
    if kind == "ring":
        return np.exp(-((d - 0.85) / 0.075) ** 2)
    return np.zeros_like(d)


class Canvas:
    def __init__(self, bg):
        self.W, self.H = CW, CH
        self.img = np.zeros((CH, CW, 3), dtype=np.float64)
        self.img[:] = bg

    def quad(self, x, y, sx, sy, rot, tint, alpha, kind="soft"):
        """Cuadro rotado AlphaBlend: dst = dst·(1−a) + tint·a."""
        if alpha <= 0.004 or sx <= 0.05 or sy <= 0.05:
            return
        # muestrear el quad en su caja local
        half = max(sx, sy) * 0.72 + 1.5
        x0, x1 = int(x - half), int(x + half) + 1
        y0, y1 = int(y - half), int(y + half) + 1
        x0, y0 = max(0, x0), max(0, y0)
        x1, y1 = min(self.W, x1), min(self.H, y1)
        if x0 >= x1 or y0 >= y1:
            return
        cr, sr = math.cos(rot), math.sin(rot)
        ys, xs = np.mgrid[y0:y1, x0:x1]
        dx = xs - x
        dy = ys - y
        # coords locales sin rotar
        lx = dx * cr + dy * sr
        ly = -dx * sr + dy * cr
        nx = np.abs(lx) / (sx * 0.5)
        ny = np.abs(ly) / (sy * 0.5)
        d = np.sqrt(nx * nx + ny * ny)
        a = _profile(np.clip(d, 0, None), kind) * alpha
        a = np.clip(a, 0, 1)[..., None]
        tint = np.array(tint, dtype=np.float64)
        self.img[y0:y1, x0:x1] = self.img[y0:y1, x0:x1] * (1 - a) + tint * a


# =====================================================================
#  WINGSTROKES — el vocabulario de las coronas (puerto de C#)
# =====================================================================
def stroke(cv, a, b, width, ca, cb, alpha):
    mid = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5)
    d = (b[0] - a[0], b[1] - a[1])
    ln = math.hypot(*d)
    if ln < 0.01 or alpha <= 0.01:
        return
    rot = math.atan2(d[1], d[0])
    c = lerp3(ca, cb, 0.5)
    cv.quad(mid[0], mid[1], ln + width, width, rot, c, alpha)


def chain(cv, pts, width, ca, cb, alpha):
    n = len(pts)
    if n < 2 or alpha <= 0.01:
        return
    for i in range(n - 1):
        stroke(cv, pts[i], pts[i + 1], width, lerp3(ca, cb, i / (n - 1)),
               lerp3(ca, cb, (i + 1) / (n - 1)), alpha)


def pearl(cv, pos, size, halo, core, alpha):
    if alpha <= 0.01:
        return
    cv.quad(pos[0], pos[1], size, size, 0, halo, alpha * 0.85)
    cv.quad(pos[0], pos[1], size * 0.45, size * 0.45, 0, core, alpha)


def flare4(cv, pos, ln, wide, c, alpha):
    if alpha <= 0.01:
        return
    cv.quad(pos[0], pos[1], ln, wide, 0, c, alpha * 0.9)
    cv.quad(pos[0], pos[1], wide, ln, 0, c, alpha * 0.9)
    cv.quad(pos[0], pos[1], wide * 1.5, wide * 1.5, 0, c, alpha)


def volume(cv, a, b, width, dark, alpha):
    mid = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5)
    d = (b[0] - a[0], b[1] - a[1])
    ln = math.hypot(*d)
    if ln < 0.01 or alpha <= 0.01:
        return
    rot = math.atan2(d[1], d[0])
    cv.quad(mid[0], mid[1], ln + width, width, rot, dark, alpha)


def bezier(a, c, b, u):
    mu = 1 - u
    return (mu * mu * a[0] + 2 * mu * u * c[0] + u * u * b[0],
            mu * mu * a[1] + 2 * mu * u * c[1] + u * u * b[1])


def feather(cv, root, tip, bend, w_root, w_tip, c_root, c_mid, c_tip, dark, alpha,
             pearl_=True, vol_alpha=0.55, pearl_scale=2.6):
    if alpha <= 0.01:
        return
    prev = root
    for i in range(1, 8):
        u = i / 7.0
        p = bezier(root, bend, tip, u)
        w = lerp(w_root, w_tip, u) * 0.5
        volume(cv, prev, p, w * 2.6, dark, vol_alpha * alpha)
        ca = lerp3(c_root, c_mid, u * 2) if u < 0.5 else lerp3(c_mid, c_tip, (u - 0.5) * 2)
        up = (u - 1 / 7.0)
        cp = lerp3(c_root, c_mid, up * 2) if up < 0.5 else lerp3(c_mid, c_tip, (up - 0.5) * 2)
        stroke(cv, prev, p, w * 1.6, cp, ca, 0.92 * alpha)
        stroke(cv, prev, p, w * 0.55, lerp3(ca, (255, 255, 255), 0.45),
               lerp3(c_tip, (255, 255, 255), 0.55), 0.75 * alpha)
        prev = p
    if pearl_:
        pearl(cv, tip, w_tip * pearl_scale, lerp3(c_tip, (255, 255, 255), 0.4), (255, 252, 250),
              0.95 * alpha)


# =====================================================================
#  EL CONTEXTO DE DIBUJO
# =====================================================================
class Ctx:
    def __init__(self, back, open_, flap_phase, flap_amp, time_, speedx=0.0):
        self.Back = back
        self.Open = open_
        self.FlapPhase = flap_phase
        self.FlapAmp = flap_amp
        self.Time = time_
        self.Direction = 1
        self.GravDir = 1.0
        self.Alpha = 1.0
        self.SpeedX = speedx
        self.Rise = 0.0


# =====================================================================
#  1 — HORIZONTE DE SUCESOS (plumas dobladas al vacío)
# =====================================================================
def render_event_horizon(cv, ctx):
    open_ = clamp(ctx.Open, 0, 1.1)
    flap = math.sin(ctx.FlapPhase) * ctx.FlapAmp
    FEATHERS = 7

    for side in (-1, 1):
        root = (ctx.Back[0] + side * 3, ctx.Back[1] - 2)
        void_r = 4.5 + 1.5 * open_
        cv.quad(root[0], root[1], void_r * 2.4, void_r * 2.4, 0, (16, 8, 26), 0.92 * ctx.Alpha, "orb")

        for s in range(10):
            th = s * TAU / 10 + ctx.Time * 0.9
            th2 = (s + 1) * TAU / 10 + ctx.Time * 0.9
            a = (root[0] + math.cos(th) * void_r * 1.25, root[1] + math.sin(th) * void_r * 1.25)
            b = (root[0] + math.cos(th2) * void_r * 1.25, root[1] + math.sin(th2) * void_r * 1.25)
            dop = 0.55 + 0.45 * max(0.0, math.sin(th))
            stroke(cv, a, b, 1.6, (235, 200, 255), (255, 250, 255),
                   (0.55 + 0.40 * dop) * ctx.Alpha)
        pearl(cv, root, 6.5, (190, 150, 255), (255, 250, 255), 0.9 * ctx.Alpha)

        for f in range(FEATHERS):
            u = f / (FEATHERS - 1)
            base_ang = lerp(-0.38, 0.62, u)
            lag = f * 0.42
            flap_ang = math.sin(ctx.FlapPhase - lag) * ctx.FlapAmp * 0.38
            fold = lerp(0.44, 1.0, open_)
            ang = (base_ang + flap_ang) * fold + 0.10 * (1 - open_) * (1 - u)
            L = lerp(32, 52, math.sin(u * PI)) * (0.50 + 0.50 * open_)
            sweep = clamp(ctx.SpeedX * ctx.Direction * 0.05, -0.55, 0.55)
            dir_ = (math.cos(ang) * side, math.sin(ang) * ctx.GravDir)
            tip = (root[0] + dir_[0] * L, root[1] + dir_[1] * L)
            bend_amt = (0.42 + sweep * 0.8) * fold
            bend = (root[0] + dir_[0] * L * 0.52 - side * math.cos(ang) * L * bend_amt * 0.45,
                    root[1] + dir_[1] * L * 0.52)
            w_root = 6.8 + 2.6 * (1 - abs(u - 0.45))
            feather(cv, root, tip, bend, w_root, 2.1,
                    (96, 38, 148), (198, 62, 176), (255, 168, 218), (38, 14, 62),
                    ctx.Alpha * (0.78 + 0.22 * open_), True, 0.68, 3.3)

        for k in range(3):
            h = hash01(side * 7 + k, k * 3 + 1, 5)
            tw = 0.5 + 0.5 * math.sin(ctx.Time * (3 + h * 3) + h * 9)
            if tw < 0.6:
                continue
            px = root[0] + side * (14 + h * 34) * open_
            py = root[1] + (h - 0.5 * k) * 22 * open_ - 6 + 2.5 * math.sin(ctx.Time * 1.7 + h * 7)
            pearl(cv, (px, py), 5.0, (220, 170, 255), (255, 255, 252), 0.85 * tw * ctx.Alpha)


# =====================================================================
#  2 — ANILLO DE FOTONES (anillos orbitales)
# =====================================================================
def render_photon_ring(cv, ctx):
    open_ = clamp(ctx.Open, 0, 1.1)
    flutter = math.sin(ctx.FlapPhase) * ctx.FlapAmp
    breathe = 1 + 0.035 * math.sin(ctx.Time * 2.0)
    RING_SEGS = 20

    for side in (-1, 1):
        root = (ctx.Back[0] + side * 3, ctx.Back[1] - 2)

        # --- LOS HUESOS DEL ALA + LA MEMBRANA (ronda 3) ---
        fold0 = lerp(0.45, 1.0, open_)
        bone1 = (root[0] + side * 33 * fold0, root[1] - 27 * fold0)
        bone2 = (root[0] + side * 27 * fold0, root[1] + 9 * fold0)
        # la membrana entre los huesos (bandas translúcidas)
        for w in range(3):
            t0 = (w + 1) / 4
            p1 = lerp_pt(root, bone1, t0)
            p2 = lerp_pt(root, bone2, t0)
            mid = ((p1[0] + p2[0]) * 0.5, (p1[1] + p2[1]) * 0.5)
            span = math.hypot(p2[0] - p1[0], p2[1] - p1[1])
            rot_w = math.atan2(p2[1] - p1[1], p2[0] - p1[0])
            len_w = math.hypot(bone1[0] - root[0], bone1[1] - root[1]) * 0.34
            cv.quad(mid[0], mid[1], span, len_w, rot_w, (58, 24, 96), 0.40 * ctx.Alpha * fold0)
            cv.quad(mid[0], mid[1], span * 0.75, len_w * 0.75, rot_w, (148, 84, 210), 0.24 * ctx.Alpha * fold0)
        volume(cv, root, bone1, 9, (44, 18, 76), 0.55 * ctx.Alpha)
        volume(cv, root, bone2, 8, (44, 18, 76), 0.5 * ctx.Alpha)
        stroke(cv, root, bone1, 5.0, (150, 84, 205), (236, 180, 250), 0.9 * ctx.Alpha)
        stroke(cv, root, bone2, 4.2, (150, 84, 205), (236, 180, 250), 0.9 * ctx.Alpha)
        pearl(cv, bone1, 8.5, (225, 170, 250), (255, 250, 255), 0.9 * ctx.Alpha)
        pearl(cv, bone2, 7.5, (225, 170, 250), (255, 250, 255), 0.85 * ctx.Alpha)

        for ring in range(2):
            upper = ring == 0
            rx = (31 if upper else 20) * (0.40 + 0.60 * open_) * breathe
            ry = (17 if upper else 10.5) * (0.40 + 0.60 * open_) * breathe
            tilt = (-0.34 if upper else 0.28) * side
            lag = 0.0 if upper else 0.55
            bob = math.sin(ctx.FlapPhase - lag) * ctx.FlapAmp * (5.5 if upper else 3.5)
            ring_tilt = tilt + flutter * (0.16 if upper else 0.10)
            c = (root[0] + side * (20 if upper else 15) * open_,
                 root[1] + (-9 if upper else 7) * open_ + bob)

            cv.quad(c[0], c[1], rx * 1.5, ry * 1.6, ring_tilt, (52, 20, 84), 0.42 * ctx.Alpha * open_)

            pts = []
            for s in range(RING_SEGS + 1):
                th = s * TAU / RING_SEGS
                ex, ey = math.cos(th) * rx, math.sin(th) * ry
                ct, st = math.cos(ring_tilt), math.sin(ring_tilt)
                pts.append((c[0] + ex * ct - ey * st, c[1] + ex * st + ey * ct))
            for s in range(RING_SEGS):
                th = s * TAU / RING_SEGS
                dop = 0.45 + 0.55 * max(0.0, math.sin(th + PI * 0.15))
                ca = lerp3((150, 70, 200), (255, 190, 235), dop)
                lead = max(0.0, math.sin(th + PI * 0.15))
                w = lerp(2.6, 4.6, lead)
                stroke(cv, pts[s], pts[s + 1], w, ca, ca,
                       ctx.Alpha * (0.50 + 0.45 * dop) * (0.55 + 0.45 * open_))

            for k in range(4):
                h = hash01(ring * 11 + k, k * 5 + 2, 9)
                speed = (1.5 if upper else 1.9) * (0.8 + 0.5 * h) * (0.5 + 0.5 * open_)
                th = ctx.Time * speed + h * TAU
                for e in range(3):
                    eth = th - e * 0.16
                    ex, ey = math.cos(eth) * rx, math.sin(eth) * ry
                    ct, st = math.cos(ring_tilt), math.sin(ring_tilt)
                    p = (c[0] + ex * ct - ey * st, c[1] + ex * st + ey * ct)
                    if e == 0:
                        tw = 0.8 + 0.2 * math.sin(ctx.Time * 7 + k * 2.2)
                        pearl(cv, p, 7.5, (255, 180, 230), (255, 253, 255), 0.95 * tw * ctx.Alpha)
                    else:
                        eth2 = eth - 0.09
                        p2 = (c[0] + math.cos(eth2) * rx * ct - math.sin(eth2) * ry * st,
                              c[1] + math.cos(eth2) * rx * st + math.sin(eth2) * ry * ct)
                        stroke(cv, p2, p, 1.7, (200, 120, 190), (255, 190, 235),
                               0.55 * ctx.Alpha / e)

            crest = (c[0] + math.cos(-PI / 2) * rx * math.cos(ring_tilt) - math.sin(-PI / 2) * ry * math.sin(ring_tilt),
                     c[1] + math.cos(-PI / 2) * rx * math.sin(ring_tilt) + math.sin(-PI / 2) * ry * math.cos(ring_tilt))
            crest_tw = 0.75 + 0.25 * math.sin(ctx.Time * 3.2 + ring * 1.4)
            pearl(cv, crest, 6.0, (230, 160, 235), (255, 250, 255), 0.85 * crest_tw * ctx.Alpha)

        pearl(cv, root, 8.0, (190, 110, 220), (255, 248, 255), 0.9 * ctx.Alpha)


# =====================================================================
#  3 — MARIPOSA CÓSMICA (vitral estelar)
# =====================================================================
def render_butterfly(cv, ctx):
    open_ = clamp(ctx.Open, 0, 1.15)
    stroke_ = math.sin(ctx.FlapPhase)
    flap = (stroke_ ** 0.75 if stroke_ > 0 else stroke_ * 1.25) * ctx.FlapAmp
    VEINS = 5

    for side in (-1, 1):
        root = (ctx.Back[0] + side * 4, ctx.Back[1] - 2)

        for lobe in range(2):
            upper = lobe == 0
            flap_angle = (flap if upper else flap * 0.72) * 0.52
            fold_angle = lerp(1.22, 0.10, open_)
            plane = fold_angle + flap_angle
            pc, ps = math.cos(plane), math.sin(plane)
            W = (44 if upper else 26) * (0.40 + 0.60 * open_)
            H = (40 if upper else 24) * (0.40 + 0.60 * open_)
            lobe_root = (root[0] + (2 if upper else 5) * side, root[1] + (-1 if upper else 4))

            def l2w(local):
                wx = local[0] * pc - local[1] * ps
                wy = local[0] * ps + local[1] * pc
                return (lobe_root[0] + wx * side, lobe_root[1] + wy)

            outline = []
            for s in range(11):
                u = s / 10.0
                theta = lerp(0.14, 2.5, u)
                r = 1 - 0.16 * math.sin(u * PI)
                outline.append(l2w((math.cos(theta) * r * (0.52 + 0.48 * math.sin(theta + 0.4)) * W,
                                    math.sin(theta) * r * H)))

            vein_tips = []
            for v in range(VEINS):
                u = (v + 1) / (VEINS + 1)
                oi = min(1 + int(u * 8.5), 10)
                vein_tips.append(outline[oi])

            for cell in range(VEINS):
                a, b = vein_tips[cell], vein_tips[cell + 1] if cell + 1 < VEINS else outline[10]
                for k in range(3):
                    t = (k + 0.5) / 3
                    cpos = (lobe_root[0] + ((a[0] - lobe_root[0]) * (0.28 + 0.34 * t) +
                                            (b[0] - lobe_root[0]) * (0.28 + 0.34 * t)) * 0.5,
                            lobe_root[1] + ((a[1] - lobe_root[1]) * (0.28 + 0.34 * t) +
                                            (b[1] - lobe_root[1]) * (0.28 + 0.34 * t)) * 0.5)
                    cv.quad(cpos[0], cpos[1], W * 0.30 * (1 - t * 0.25), H * 0.26 * (1 - t * 0.25),
                            plane * side, (64, 22, 104), 0.50 * ctx.Alpha)
                    cv.quad(cpos[0], cpos[1], W * 0.22 * (1 - t * 0.25), H * 0.19 * (1 - t * 0.25),
                            plane * side, (168, 62, 178), 0.34 * ctx.Alpha)

            lobe_mid = l2w((W * 0.5, 0))
            cv.quad(lobe_mid[0], lobe_mid[1], W * 0.9, H * 0.85, plane * side, (46, 16, 76), 0.42 * ctx.Alpha)
            cv.quad(lobe_mid[0], lobe_mid[1], W * 0.72, H * 0.68, plane * side, (130, 44, 150), 0.22 * ctx.Alpha)

            for v in range(VEINS):
                tip = vein_tips[v]
                d = (tip[0] - lobe_root[0], tip[1] - lobe_root[1])
                bend = (lobe_root[0] + d[0] * 0.55, lobe_root[1] + d[1] * 0.55 - H * 0.14 * (v - 2) * 0.4)
                m1, m2 = bezier(lobe_root, bend, tip, 0.34), bezier(lobe_root, bend, tip, 0.68)
                stroke(cv, lobe_root, m1, 2.0, (255, 132, 196), (255, 132, 196), 0.80 * ctx.Alpha)
                stroke(cv, m1, m2, 1.7, (255, 132, 196), (255, 214, 238), 0.80 * ctx.Alpha)
                stroke(cv, m2, tip, 1.4, (255, 214, 238), (255, 214, 238), 0.80 * ctx.Alpha)

            chain(cv, outline, 2.6, (255, 178, 92), (255, 232, 168), 0.92 * ctx.Alpha)
            echo = [lerp_pt(lobe_root, outline[int(s / 8 * 10)], 0.86) for s in range(9)]
            chain(cv, echo, 1.2, (255, 210, 140), (255, 240, 210), 0.45 * ctx.Alpha)

            if upper:
                eye = lerp_pt(lobe_root, outline[6], 0.62)
                eye_tw = 0.8 + 0.2 * math.sin(ctx.Time * 2.1 + side * 1.2)
                pearl(cv, eye, 11.5, (140, 60, 190), (246, 238, 255), 0.95 * eye_tw * ctx.Alpha)
                cv.quad(eye[0], eye[1], 16.3, 16.3, 0, (255, 218, 150), 0.75 * ctx.Alpha, "ring")

            for p in range(3):
                oi = min(3 + p * 3, 10)
                tw = 0.6 + 0.4 * math.sin(ctx.Time * 3.4 + oi * 2.6)
                pearl(cv, outline[oi], 5.2, (255, 196, 130), (255, 250, 240), 0.8 * tw * ctx.Alpha)


def lerp_pt(a, b, t):
    return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)


# =====================================================================
#  4 — HADA DE POLVO ESTELAR (pétalos)
# =====================================================================
def render_fairy(cv, ctx):
    open_ = clamp(ctx.Open, 0, 1.1)
    flutter = math.sin(ctx.FlapPhase) * (ctx.FlapAmp * 0.75 + 0.25)

    for side in (-1, 1):
        root = (ctx.Back[0] + side * 4, ctx.Back[1] - 3)

        for lobe in range(4):
            base_ang = {0: -1.28, 1: -0.88, 2: -0.30, 3: 0.10}[lobe]
            ln = {0: 27, 1: 32, 2: 22, 3: 17}[lobe]
            wid = {0: 10, 1: 11.5, 2: 9, 3: 7}[lobe]
            lag = lobe * 0.55
            ang = base_ang + flutter * (0.30 + 0.06 * lobe) * math.sin(ctx.FlapPhase - lag) * 1.6
            fold = lerp(0.48, 1.0, open_)
            ang = -lerp(-0.35, -ang, fold)
            dirx, diry = math.cos(ang) * side, math.sin(ang) * ctx.GravDir
            tip = (root[0] + dirx * ln * fold, root[1] + diry * ln * fold + flutter * 1.4)
            perp = (-diry, dirx * side)
            mid = ((root[0] + tip[0]) * 0.5, (root[1] + tip[1]) * 0.5)
            w_mid = wid * fold

            for c in range(3):
                t = (c + 0.5) / 3
                cpos = (root[0] + dirx * ln * fold * t, root[1] + diry * ln * fold * t + flutter * 1.4 * t)
                profile = math.sin(t * PI) * (1 - t * 0.30)
                csize = (w_mid * (0.8 + profile) * 1.9, ln * fold / 3 * 1.5)
                crot = math.atan2(diry, dirx)
                cv.quad(cpos[0], cpos[1], csize[0], csize[1], crot, (124, 68, 16), 0.46 * ctx.Alpha * fold)
                cv.quad(cpos[0], cpos[1], csize[0] * 0.8, csize[1] * 0.8, crot, (212, 128, 34), 0.30 * ctx.Alpha * fold)

            edge_a0 = (root[0] + perp[0] * w_mid * 0.30, root[1] + perp[1] * w_mid * 0.30)
            edge_b0 = (root[0] - perp[0] * w_mid * 0.30, root[1] - perp[1] * w_mid * 0.30)
            edge_a = (mid[0] + perp[0] * w_mid * 0.85, mid[1] + perp[1] * w_mid * 0.85)
            edge_b = (mid[0] - perp[0] * w_mid * 0.85, mid[1] - perp[1] * w_mid * 0.85)
            c_base, c_mid, c_tip = (214, 118, 30), (255, 178, 70), (255, 232, 168)
            stroke(cv, edge_a0, edge_a, 2.1, c_base, c_mid, 0.88 * ctx.Alpha)
            stroke(cv, edge_a, tip, 1.6, c_mid, c_tip, 0.88 * ctx.Alpha)
            stroke(cv, edge_b0, edge_b, 2.1, c_base, c_mid, 0.88 * ctx.Alpha)
            stroke(cv, edge_b, tip, 1.6, c_mid, c_tip, 0.88 * ctx.Alpha)

            for v in range(3):
                spread = (v - 1) * 0.34
                vdir = (math.cos(ang + spread) * side, math.sin(ang + spread))
                vtip = (root[0] + vdir[0] * ln * fold * 0.82, root[1] + vdir[1] * ln * fold * 0.82)
                vmid = (root[0] + vdir[0] * ln * fold * 0.45 + perp[0] * w_mid * 0.18 * (v - 1),
                        root[1] + vdir[1] * ln * fold * 0.45 + perp[1] * w_mid * 0.18 * (v - 1))
                stroke(cv, root, vmid, 1.4, (255, 196, 110), (255, 196, 110), 0.72 * ctx.Alpha)
                stroke(cv, vmid, vtip, 1.1, (255, 196, 110), (255, 244, 214), 0.72 * ctx.Alpha)

            tw = 0.75 + 0.25 * math.sin(ctx.Time * 6.5 + lobe * 1.9 + side)
            pearl(cv, tip, 8.0, (255, 196, 100), (255, 253, 244), 0.95 * tw * ctx.Alpha)

            for k in range(2):
                h = hash01(side * 9 + lobe, k * 4 + 1, 3)
                mtw = 0.5 + 0.5 * math.sin(ctx.Time * (4 + h * 4) + h * 11)
                if mtw < 0.62:
                    continue
                t = 0.35 + h * 0.5
                ppos = (root[0] + dirx * ln * fold * t + perp[0] * w_mid * (0.55 + 0.3 * k),
                        root[1] + diry * ln * fold * t + perp[1] * w_mid * (0.55 + 0.3 * k))
                pearl(cv, ppos, 4.6, (255, 214, 130), (255, 255, 250), 0.85 * mtw * ctx.Alpha)

        pearl(cv, root, 7.5, (255, 178, 80), (255, 250, 235), 0.9 * ctx.Alpha)


# =====================================================================
#  5 — CORONA SOLAR (prominencias — la corona de arcos como alas)
# =====================================================================
def render_solar_corona(cv, ctx):
    open_ = clamp(ctx.Open, 0, 1.1)
    flap = math.sin(ctx.FlapPhase) * ctx.FlapAmp
    LOOPS, SEGMENTS = 3, 14

    def temp_color(u):
        if u < 0.45:
            return lerp3((196, 44, 22), (255, 110, 36), u / 0.45)
        return lerp3((255, 110, 36), (255, 232, 150), (u - 0.45) / 0.55)

    for side in (-1, 1):
        root = (ctx.Back[0] + side * 3, ctx.Back[1] - 1)
        core_pulse = 0.85 + 0.15 * math.sin(ctx.Time * 2.4)
        cv.quad(root[0], root[1], 26, 26, 0, (120, 30, 10), 0.55 * ctx.Alpha)
        cv.quad(root[0], root[1], 14, 14, 0, (255, 130, 40), 0.65 * ctx.Alpha)
        pearl(cv, root, 9.0, (255, 170, 70), (255, 250, 235), 0.95 * ctx.Alpha)

        for l in range(LOOPS):
            t01 = l / (LOOPS - 1)
            breathe = 1 + 0.05 * math.sin(ctx.Time * (2.1 + t01 * 0.6) + l * 1.7)
            sway = 0.16 * math.sin(ctx.Time * 0.85 + l * 2.6)
            flap_tilt = flap * (0.28 - 0.08 * t01)
            reach = lerp(20, 34, t01) * (0.45 + 0.55 * open_)
            half_wl = reach * (1.06 - sway) * breathe
            half_wr = reach * (0.86 + sway) * breathe
            apex_ang = lerp(-0.95, -0.35, t01) * side + flap_tilt * 0.4
            apex_h = lerp(30, 22, t01) * (0.45 + 0.55 * open_) * breathe
            base_pos = (root[0] + side * (4 + t01 * 6), root[1] + 2)

            pts = []
            for s in range(SEGMENTS + 1):
                u = s / SEGMENTS
                arc = u * PI
                edge, side_sign = math.sin(arc), math.cos(arc)
                hx = -math.cos(arc) * (half_wl if side_sign < 0 else half_wr) * side
                hy = -edge * apex_h
                aa = apex_ang * edge * 0.35
                ca, sa = math.cos(aa), math.sin(aa)
                wob = 1.6 * math.sin(u * 9 + ctx.Time * (3 + t01)) * edge
                pts.append((base_pos[0] + hx * ca - hy * sa,
                            base_pos[1] + hx * sa + hy * ca + wob))

            for s in range(SEGMENTS):
                u = s / SEGMENTS
                edge = math.sin(u * PI)
                ca, cb = temp_color(u), temp_color((s + 1) / SEGMENTS)
                w = lerp(2.2, 4.4, edge) * (0.7 + 0.3 * open_)
                stroke(cv, pts[s], pts[s + 1], w, ca, cb, 0.90 * ctx.Alpha * (0.6 + 0.4 * open_))

            for s in range(1, SEGMENTS - 1, 2):
                a = lerp_pt(base_pos, pts[s], 0.68)
                b = lerp_pt(base_pos, pts[s + 1], 0.68)
                stroke(cv, a, b, 1.4, (255, 220, 150), (255, 240, 190), 0.5 * ctx.Alpha)

            apex = pts[SEGMENTS // 2]
            knot_pulse = 0.85 + 0.30 * math.sin(ctx.Time * 3.1 + l * 2.3)
            cv.quad(apex[0], apex[1], 16, 16, 0, (255, 138, 60), knot_pulse * 0.55 * ctx.Alpha)
            flare4(cv, apex, 15 + 6 * t01, 2.6, (255, 178, 96), knot_pulse * 0.85 * ctx.Alpha)
            pearl(cv, apex, 6.5, (255, 170, 90), (255, 250, 238), knot_pulse * 0.9 * ctx.Alpha)

        for k in range(4):
            h = hash01(side * 13 + k, k * 7 + 2, 5)
            tw = 0.5 + 0.5 * math.sin(ctx.Time * (2.5 + h * 3) + h * 13)
            if tw < 0.66:
                continue
            pos = (root[0] + side * (6 + h * 30) * open_,
                   root[1] + (4 - h * 22) * open_ + 2.0 * math.sin(ctx.Time * 1.9 + h * 8))
            pearl(cv, pos, 4.8, (255, 130, 50), (255, 244, 224), 0.8 * tw * ctx.Alpha)


# =====================================================================
#  6 — NEBULOSA VIVA (plumas maestras + nube + estrellas con difracción)
# =====================================================================
def render_nebula(cv, ctx):
    open_ = clamp(ctx.Open, 0, 1.1)
    breathe = 1 + 0.05 * math.sin(ctx.Time * 1.3)
    flap = math.sin(ctx.FlapPhase) * ctx.FlapAmp * 0.4
    QUILLS, BLOBS, STARS, FILAMENTS = 6, 6, 5, 3

    for side in (-1, 1):
        root = (ctx.Back[0] + side * 3, ctx.Back[1] - 2)

        # --- EL ESQUELETO: plumas maestras (la silueta de ala) ---
        for q in range(QUILLS):
            u = q / (QUILLS - 1)
            base_ang = lerp(-0.72, 0.52, u)
            lag = q * 0.38
            flap_ang = math.sin(ctx.FlapPhase - lag) * ctx.FlapAmp * 0.30
            fold = lerp(0.46, 1.0, open_)
            ang = (base_ang + flap_ang) * fold
            L = lerp(26, 42, math.sin(u * PI)) * (0.48 + 0.52 * open_)
            dir_ = (math.cos(ang) * side, math.sin(ang))
            tip = (root[0] + dir_[0] * L, root[1] + dir_[1] * L)
            bend_amt = 0.30 * fold
            bend = (root[0] + dir_[0] * L * 0.5 - side * math.cos(ang) * L * bend_amt * 0.4,
                    root[1] + dir_[1] * L * 0.5)
            w_root = 6.0 + 2.0 * (1 - abs(u - 0.45))
            feather(cv, root, tip, bend, w_root, 1.8,
                    (84, 40, 130), (150, 78, 190), (216, 130, 235), (30, 14, 52),
                    ctx.Alpha * (0.75 + 0.25 * open_), True, 0.62, 3.0)

        for b in range(BLOBS):
            h1 = hash01(side * 17 + b, b * 3 + 1, 7)
            h2 = hash01(side * 17 + b, b * 5 + 2, 11)
            drift_x = (h1 - 0.5) * 8 * math.sin(ctx.Time * 0.4 + h2 * 9)
            drift_y = (h2 - 0.5) * 10 * math.sin(ctx.Time * 0.33 + h1 * 7)
            u = b / (BLOBS - 1)
            reach = lerp(18, 40, math.sin(u * PI)) * (0.40 + 0.60 * open_)
            ang = lerp(-0.75, 0.55, u) + flap * 0.15 * (1 - u)
            pos = (root[0] + math.cos(ang) * reach * side + drift_x,
                   root[1] + math.sin(ang) * reach * 0.7 + drift_y)
            size = lerp(14, 26, math.sin(u * PI)) * (0.5 + 0.5 * open_) * breathe
            dark = [(64, 26, 104), (18, 72, 88), (96, 26, 84)][b % 3]
            light = [(142, 84, 208), (58, 158, 178), (212, 92, 176)][b % 3]
            cv.quad(pos[0], pos[1], size * 2.0, size * 1.7, ang, dark,
                    0.36 * ctx.Alpha * (0.55 + 0.45 * open_))
            cv.quad(pos[0], pos[1], size * 1.4, size * 1.2, ang, light,
                    0.26 * ctx.Alpha * (0.55 + 0.45 * open_))

        for f in range(FILAMENTS):
            h1 = hash01(side * 23 + f, f * 9 + 4, 17)
            f_ang = lerp(-0.55, 0.35, f / (FILAMENTS - 1)) + flap * 0.1
            f_len = lerp(26, 42, h1) * (0.40 + 0.60 * open_)
            prev = (root[0] + side * 5, root[1] + h1 * 6)
            for s in range(1, 6):
                u = s / 5
                wob = 4.5 * math.sin(u * 6.5 + ctx.Time * 1.6 + h1 * 8)
                p = (root[0] + math.cos(f_ang) * f_len * u * side + wob * 0.4,
                     root[1] + math.sin(f_ang) * f_len * 0.6 * u + wob)
                fa = lerp3((190, 70, 200), (240, 150, 255), u)
                stroke(cv, prev, p, 1.7, fa, fa, 0.62 * ctx.Alpha)
                prev = p

        for s in range(STARS):
            h1 = hash01(side * 29 + s, s * 11 + 5, 19)
            h2 = hash01(side * 29 + s, s * 13 + 6, 23)
            reach = lerp(14, 44, h1) * (0.42 + 0.58 * open_)
            ang = lerp(-0.85, 0.65, h2) + flap * 0.12
            pos = (root[0] + math.cos(ang) * reach * side + 2.5 * math.sin(ctx.Time * 0.5 + h1 * 9),
                   root[1] + math.sin(ang) * reach * 0.75 + 2.5 * math.cos(ctx.Time * 0.45 + h2 * 7))
            tw = 0.55 + 0.45 * math.sin(ctx.Time * (1.8 + h2 * 2.6) + h1 * 12)
            if tw < 0.30:
                continue
            pearl(cv, pos, 9.0, (220, 190, 255), (255, 255, 252), 0.95 * tw * ctx.Alpha)
            flare4(cv, pos, 13 + 6 * h2, 2.1, (235, 215, 255), 0.75 * tw * ctx.Alpha)

        core_tw = 0.8 + 0.2 * math.sin(ctx.Time * 2.2)
        pearl(cv, root, 10, (200, 160, 255), (255, 252, 255), 0.95 * core_tw * ctx.Alpha)
        flare4(cv, root, 15, 2.2, (225, 200, 255), 0.6 * core_tw * ctx.Alpha)


# =====================================================================
#  7 — ECLIPSE TOTAL (ronda 2: plumas negras + puntas cromosféricas)
# =====================================================================
def render_eclipse(cv, ctx):
    open_ = clamp(ctx.Open, 0, 1.1)
    flap = math.sin(ctx.FlapPhase) * ctx.FlapAmp
    FEATHERS, RAYS, RIM = 6, 5, 12

    for side in (-1, 1):
        root = (ctx.Back[0] + side * 4, ctx.Back[1] - 2)

        # --- RAYOS DE CORONA (detrás del abanico) ---
        for ray in range(RAYS):
            h = hash01(side * 7, ray * 5 + 1, 3)
            th = -PI * 0.88 + ray / (RAYS - 1) * PI * 1.05 + (h - 0.5) * 0.38
            th += 0.10 * math.sin(ctx.Time * 2.1 + ray * 1.9 + h * 6)
            ray_len = lerp(16, 34, h) * (0.45 + 0.55 * open_) * \
                (0.85 + 0.15 * math.sin(ctx.Time * 1.6 + ray * 2.4))
            dir_ = (math.cos(th) * side, math.sin(th))
            n = math.hypot(*dir_)
            if n < 0.001:
                continue
            dir_ = (dir_[0] / n, dir_[1] / n)
            base_r = 9 + 5 * open_
            a = (root[0] + dir_[0] * base_r, root[1] + dir_[1] * base_r)
            b = (root[0] + dir_[0] * (base_r + ray_len), root[1] + dir_[1] * (base_r + ray_len))
            bend = (root[0] + dir_[0] * (base_r + ray_len * 0.5),
                    root[1] + dir_[1] * (base_r + ray_len * 0.5) - 2.5)
            m1, m2 = bezier(a, bend, b, 0.35), bezier(a, bend, b, 0.7)
            rc0, rc1 = (255, 250, 235), (196, 210, 255)
            stroke(cv, a, m1, 1.8, rc0, rc0, 0.50 * ctx.Alpha)
            stroke(cv, m1, m2, 1.5, rc0, rc1, 0.45 * ctx.Alpha)
            stroke(cv, m2, b, 1.2, rc1, rc1, 0.36 * ctx.Alpha)

        # --- EL DISCO DE ECLIPSE EN EL HOMBRO ---
        void_r = 6.0 + 1.5 * open_
        cv.quad(root[0], root[1], void_r * 2.3, void_r * 2.3, 0, (14, 12, 32), 0.94 * ctx.Alpha, "orb")
        for s in range(RIM):
            th = s * TAU / RIM
            th2 = (s + 1) * TAU / RIM
            a = (root[0] + math.cos(th) * void_r * 1.22, root[1] + math.sin(th) * void_r * 1.22)
            b = (root[0] + math.cos(th2) * void_r * 1.22, root[1] + math.sin(th2) * void_r * 1.22)
            height = 0.5 + 0.5 * math.sin(th + PI / 2)
            rc = lerp3((205, 216, 255), (255, 253, 248), height)
            stroke(cv, a, b, lerp(1.2, 1.9, height), rc, rc, (0.4 + 0.5 * height) * ctx.Alpha)

        # --- LAS PLUMAS NEGRAS ---
        for f in range(FEATHERS):
            u = f / (FEATHERS - 1)
            base_ang = lerp(-0.60, 0.55, u)
            lag = f * 0.45
            flap_ang = math.sin(ctx.FlapPhase - lag) * ctx.FlapAmp * 0.34
            fold = lerp(0.42, 1.0, open_)
            ang = (base_ang + flap_ang) * fold
            L = lerp(30, 50, math.sin(u * PI)) * (0.50 + 0.50 * open_)
            sweep = clamp(ctx.SpeedX * ctx.Direction * 0.04, -0.45, 0.45)
            dir_ = (math.cos(ang) * side, math.sin(ang))
            tip = (root[0] + dir_[0] * L, root[1] + dir_[1] * L)
            bend_amt = (0.34 + sweep * 0.7) * fold
            bend = (root[0] + dir_[0] * L * 0.52 - side * math.cos(ang) * L * bend_amt * 0.45,
                    root[1] + dir_[1] * L * 0.52)
            w_root = 6.4 + 2.4 * (1 - abs(u - 0.45))
            feather(cv, root, tip, bend, w_root, 2.0,
                    (26, 22, 54), (52, 46, 96), (122, 118, 168), (10, 9, 26),
                    ctx.Alpha * (0.80 + 0.20 * open_), True, 0.80, 3.0)

            # la PUNTA CROMOSFÉRICA (perla blanco-caliente)
            rim_tw = 0.75 + 0.25 * math.sin(ctx.Time * 2.8 + f * 1.7 + side)
            pearl(cv, tip, 8.5, (216, 226, 255), (255, 253, 250), 0.95 * rim_tw * ctx.Alpha)
            if f % 2 == 0:
                flare4(cv, tip, 9, 1.8, (228, 236, 255), 0.5 * rim_tw * ctx.Alpha)

        # --- DESTELLO DE TOTALIDAD (solo al volar) ---
        if open_ > 0.65:
            t_pulse = 0.7 + 0.3 * math.sin(ctx.Time * 2.4)
            vis = (open_ - 0.65) / 0.35
            crown = (root[0], root[1] - 26 * open_)
            flare4(cv, crown, 16, 2.2, (235, 240, 255), 0.55 * t_pulse * vis * ctx.Alpha)


# =====================================================================
#  8 — COMETA CARMESÍ (estelas)
# =====================================================================
def render_comet(cv, ctx):
    open_ = clamp(ctx.Open, 0, 1.1)
    flap = math.sin(ctx.FlapPhase) * ctx.FlapAmp
    speed_t = clamp(abs(ctx.SpeedX) / 9, 0, 1)
    STREAKS = 3

    def streak_color(t):
        if t < 0.25:
            return lerp3((255, 244, 232), (255, 120, 80), t / 0.25)
        if t < 0.6:
            return lerp3((255, 120, 80), (232, 50, 60), (t - 0.25) / 0.35)
        return lerp3((232, 50, 60), (255, 150, 160), (t - 0.6) / 0.4)

    for side in (-1, 1):
        root = (ctx.Back[0] + side * 3, ctx.Back[1] - 2)
        core_pulse = 0.85 + 0.15 * math.sin(ctx.Time * 4.2)
        cv.quad(root[0], root[1], 22, 22, 0, (120, 16, 30), 0.5 * ctx.Alpha)
        cv.quad(root[0], root[1], 12, 12, 0, (255, 90, 70), 0.55 * ctx.Alpha)

        for k in range(STREAKS):
            u = k / (STREAKS - 1)
            base_ang = lerp(-0.58, 0.46, u)
            lag = k * 0.5
            flap_ang = math.sin(ctx.FlapPhase - lag) * ctx.FlapAmp * 0.3
            fold = lerp(0.46, 1.0, open_)
            ang = (base_ang + flap_ang) * fold
            L = lerp(34, 50, math.sin(u * PI)) * (0.50 + 0.50 * open_) * (1 + speed_t * 0.55)
            sweep = clamp(ctx.SpeedX * ctx.Direction * 0.05, -0.5, 0.5)
            bend_amt = (0.32 + sweep * 0.7 + speed_t * 0.22) * fold
            dir_ = (math.cos(ang) * side, math.sin(ang))
            head = (root[0] + dir_[0] * (7 + 4 * k) * fold, root[1] + dir_[1] * (7 + 4 * k) * fold)
            tip = (head[0] + dir_[0] * L, head[1] + dir_[1] * L)
            bend = (head[0] + dir_[0] * L * 0.5 - side * math.cos(ang) * L * bend_amt * 0.5,
                    head[1] + dir_[1] * L * 0.5)

            prev = head
            for s in range(1, 8):
                t = s / 7
                p = bezier(head, bend, tip, t)
                wave = 0.72 + 0.28 * math.sin(t * 7 - ctx.Time * 6.5 + u * 2)
                fade = 1 - t * t * 0.55
                w = lerp(7.5, 1.2, t) * (0.7 + 0.3 * open_)
                ca, cb = streak_color(t - 1 / 7), streak_color(t)
                volume(cv, prev, p, w * 2.2, (70, 10, 22), 0.40 * ctx.Alpha * fade)
                stroke(cv, prev, p, w * 1.5, ca, cb, 0.88 * ctx.Alpha * fade * wave)
                prev = p

            h_pulse = 0.8 + 0.2 * math.sin(ctx.Time * 5.5 + k * 1.8)
            cv.quad(head[0], head[1], 16, 16, 0, (255, 120, 80), 0.5 * h_pulse * ctx.Alpha)
            pearl(cv, head, 9.5, (255, 140, 100), (255, 252, 248), 0.95 * h_pulse * ctx.Alpha)
            flare4(cv, head, 12, 2.2, (255, 200, 170), 0.65 * h_pulse * ctx.Alpha)

        # --- LA MEMBRANA DE SUSTENTACIÓN entre las velas ---
        for m in range(STREAKS - 1):
            for s in range(1, 6):
                t = s / 5
                ha = (root[0] + math.cos((lerp(-0.58, 0.46, m / (STREAKS - 1))) * lerp(0.46, 1.0, open_)) * side * (7 + 4 * m) * lerp(0.46, 1.0, open_),
                      root[1] + math.sin((lerp(-0.58, 0.46, m / (STREAKS - 1))) * lerp(0.46, 1.0, open_)) * (7 + 4 * m) * lerp(0.46, 1.0, open_))
                hb = (root[0] + math.cos((lerp(-0.58, 0.46, (m + 1) / (STREAKS - 1))) * lerp(0.46, 1.0, open_)) * side * (7 + 4 * (m + 1)) * lerp(0.46, 1.0, open_),
                      root[1] + math.sin((lerp(-0.58, 0.46, (m + 1) / (STREAKS - 1))) * lerp(0.46, 1.0, open_)) * (7 + 4 * (m + 1)) * lerp(0.46, 1.0, open_))
                # simplificación: extremos aproximados en t
                a = lerp_pt(ha, (ha[0] + 40 * side, ha[1] - 8), t)
                b = lerp_pt(hb, (hb[0] + 40 * side, hb[1] - 8), t)
                mid = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5)
                w_out = math.hypot(b[0] - a[0], b[1] - a[1])
                if w_out < 1:
                    continue
                fade = 1 - t * t * 0.6
                rot_m = math.atan2(b[1] - a[1], b[0] - a[0])
                cv.quad(mid[0], mid[1], w_out, 9 * (1 - t * 0.5), rot_m, (96, 14, 26), 0.38 * ctx.Alpha * fade)
                cv.quad(mid[0], mid[1], w_out * 0.8, 7 * (1 - t * 0.5), rot_m, (232, 60, 60), 0.26 * ctx.Alpha * fade)

        for sp in range(8):
            h1 = hash01(side * 31 + sp, sp * 7 + 1, 5)
            h2 = hash01(side * 31 + sp, sp * 9 + 2, 9)
            tw = 0.5 + 0.5 * math.sin(ctx.Time * (3 + h2 * 4) + h1 * 15)
            if tw < 0.58:
                continue
            ang = lerp(-0.55, 0.35, h1)
            reach = (8 + h2 * 38) * (0.45 + 0.55 * open_) * (1 + speed_t * 0.4)
            pos = (root[0] + math.cos(ang) * reach * side - side * speed_t * reach * 0.35,
                   root[1] + math.sin(ang) * reach * 0.6)
            pearl(cv, pos, 5.5, (255, 150, 130), (255, 252, 250), 0.85 * tw * ctx.Alpha)


# =====================================================================
#  LA SILUETA DEL JUGADOR + ESCENAS
# =====================================================================
RENDERERS = [
    ("Horizonte", render_event_horizon),
    ("AnilloFotones", render_photon_ring),
    ("Mariposa", render_butterfly),
    ("Hada", render_fairy),
    ("CoronaSolar", render_solar_corona),
    ("Nebulosa", render_nebula),
    ("Eclipse", render_eclipse),
    ("Cometa", render_comet),
]


def sky_bg():
    return (108, 160, 220)


def draw_player(cv):
    """Silueta simple: cabeza + torso (encima de las alas, como el juego)."""
    x, y = CW // 2, CH - 80
    dark = (52, 60, 76)
    cv.quad(x, y - 16, 18, 18, 0, dark, 0.98, "orb")     # cabeza
    cv.quad(x, y + 4, 22, 34, 0, dark, 0.98, "orb")      # torso


def render_scene(renderer, state, time_=2.0):
    """Un canvas con el ala en el estado dado."""
    back = (CW // 2, CH - 96)   # omóplatos del jugador
    if state == "rest":
        ctx = Ctx(back, 0.15, 0.0, 0.0, time_)
    elif state == "flap":
        ctx = Ctx(back, 1.0, 1.2, 1.0, time_)
    elif state == "glide":
        ctx = Ctx(back, 0.9, 0.0, 0.0, time_)
    else:  # sprint
        ctx = Ctx(back, 1.0, 0.6, 1.0, time_, speedx=8.0)

    cv = Canvas(sky_bg())
    renderer(cv, ctx)
    draw_player(cv)
    return cv.img


def main():
    states = ["rest", "flap", "glide", "sprint"]
    rows = len(RENDERERS)
    cols = len(states)
    sheet = np.zeros((rows * (CH + 6), cols * (CW + 6), 3), dtype=np.uint8)
    sheet[:] = (24, 24, 30)

    for r, (name, renderer) in enumerate(RENDERERS):
        for c, state in enumerate(states):
            img = render_scene(renderer, state, time_=2.0 + r * 0.7)
            y0 = r * (CH + 6) + 3
            x0 = c * (CW + 6) + 3
            sheet[y0:y0 + CH, x0:x0 + CW] = np.clip(img, 0, 255).astype(np.uint8)

    Image.fromarray(sheet).save(
        '/home/z/my-project/research/wings_v613/mock_v613_sheet.png')
    print('sheet:', sheet.shape)

    # una escena grande de cada ala en vuelo (para el VLM con detalle)
    for name, renderer in RENDERERS:
        img = render_scene(renderer, "flap", time_=2.3)
        Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).save(
            f'/home/z/my-project/research/wings_v613/mock_v613_{name}.png')
    print('OK')


if __name__ == '__main__':
    main()
