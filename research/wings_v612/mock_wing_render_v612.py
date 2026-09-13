#!/usr/bin/env python3
"""
mock_wing_render_v612.py — SIMULACIÓN ALPHA-BLEND DE LAS 8 ALAS DE LUZ.

El pase de dibujado del jugador compone los DrawData con AlphaBlend (NO
aditivo) sobre texturas premultiplicadas — la lección v6.12. Este mock
replica ese modelo: out = dst·(1−a_eff) + tint·a_eff, con a_eff =
perfil(textura)·alfa. Fondo: cielo de Terraria de día (el PEOR caso para
leer alas de luz — si se leen ahí, se leen en cualquier sitio).

Verifica: geometría (lóbulo/cinta/disco), anclaje al hombro, silueta
legible contra el cielo, volumen + luz, y los 3 estados (reposo/aleteo/
planeo).
"""
from PIL import Image, ImageDraw
import numpy as np
import math

CW, CH = 280, 210          # canvas de simulación
GROUND_Y = 178
PI = math.pi


def smoothstep(e0, e1, x):
    t = min(1.0, max(0.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def hash01(a, b, c=0):
    h = (a * 374761393 + b * 668265263 + c * 1911520717) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFFFF) / 16777216.0


def lerp3(c1, c2, t):
    return tuple(c1[k] + (c2[k] - c1[k]) * t for k in range(3))


# =====================================================================
#  COMPOSITOR ALPHA-BLEND (el modelo del pase de jugador)
# =====================================================================
def _profile(d, kind):
    """Perfil alfa de las texturas de la librería (0..1)."""
    if kind == "soft":       # SoftGlow: núcleo→caída suave a d=1
        return smoothstep(1.0, 0.0, d)
    if kind == "orb":        # GlowOrb: núcleo sólido + borde suave
        if d < 0.55:
            return 1.0
        return smoothstep(1.0, 0.55, d) * 0.96
    if kind == "ring":       # Ring: aro fino a d≈0.85
        return math.exp(-((d - 0.85) / 0.075) ** 2)
    return 0.0


def quad(c, x, y, sx, sy, rot, tint, alpha, kind="soft"):
    """Cuadro rotado AlphaBlend: dst = dst·(1−a) + tint·a."""
    if alpha <= 0.004 or sx <= 0.05 or sy <= 0.05:
        return
    ca, sa = math.cos(rot), math.sin(rot)
    rad = max(sx, sy) + 1.0
    i0, i1 = max(0, int(x - rad)), min(CW, int(x + rad) + 1)
    j0, j1 = max(0, int(y - rad)), min(CH, int(y + rad) + 1)
    if i0 >= i1 or j0 >= j1:
        return
    ii, jj = np.meshgrid(np.arange(i0, i1), np.arange(j0, j1))
    dx = ii - x
    dy = jj - y
    # marco local (rotación inversa)
    lx = (dx * ca + dy * sa) / max(sx, 0.001)
    ly = (-dx * sa + dy * ca) / max(sy, 0.001)
    d = np.sqrt(lx * lx + ly * ly)
    prof = np.vectorize(lambda v: _profile(v, kind))(d)
    a = prof * alpha
    a = np.clip(a, 0.0, 1.0)[..., None]
    region = c[j0:j1, i0:i1]
    c[j0:j1, i0:i1] = region * (1 - a) + np.array(tint) * a


def sky_canvas():
    """Cielo de Terraria de día + suelo (el peor caso para alas de luz)."""
    c = np.zeros((CH, CW, 3), dtype=np.float32)
    top = np.array([0.44, 0.62, 0.86])       # azul cielo
    hor = np.array([0.72, 0.85, 0.96])       # horizonte claro
    for j in range(CH):
        t = min(1.0, j / GROUND_Y)
        c[j, :, :] = top + (hor - top) * t
    # suelo de césped
    c[GROUND_Y:, :, :] = np.array([0.22, 0.52, 0.24])
    # nubes suaves
    for (cx, cy, rw) in [(70, 40, 26), (200, 28, 20), (150, 62, 16)]:
        quad(c, cx, cy, rw, rw * 0.38, 0.0, (1.0, 1.0, 1.0), 0.55, "soft")
    return c


def player_silhouette(img, back):
    """Silueta del jugador (cabeza/torso/piernas) — se dibuja ENCIMA
    (las alas viven detrás del cuerpo)."""
    d = ImageDraw.Draw(img, "RGBA")
    cx = back[0]
    hy = back[1] - 16
    d.ellipse([cx - 6, hy - 12, cx + 6, hy], fill=(120, 120, 130, 255))
    d.rounded_rectangle([cx - 8, hy, cx + 8, hy + 40], radius=4,
                        fill=(140, 90, 70, 255))
    d.rectangle([cx - 7, hy + 40, cx - 2, hy + 66], fill=(90, 60, 50, 255))
    d.rectangle([cx + 2, hy + 40, cx + 7, hy + 66], fill=(90, 60, 50, 255))


def to_img(c, back):
    img = Image.fromarray((np.clip(c, 0, 1) * 255).astype(np.uint8))
    player_silhouette(img, back)
    return img


# =====================================================================
#  ctx: (back, open, flap_phase, flap_amp, time, alpha=1)
# =====================================================================
VQ = lambda r: r * 2.174     # RingQuadSize


# --------------------------------------------------------- MARIPOSA
def upper_lobe(u):
    th = (0.12 + (2.45 - 0.12) * u) * 0.5
    r = 1 - 0.18 * math.sin(u * PI)
    return (math.cos(th) * r * (0.55 + 0.5 * math.sin(th + 0.4)),
            math.sin(th) * r * 0.82)


def lower_lobe(u):
    th = (-0.25 + 1.45 * u) * 0.62
    r = 1 - 0.10 * math.sin(u * PI)
    return (math.cos(th) * r * 0.85, math.sin(th) * r * 0.60 + 0.15)


def butterfly(c, ctx):
    back, o, fp, fa, tm = ctx
    stroke = math.sin(fp)
    flap = (stroke ** 0.75 if stroke > 0 else stroke * 1.25) * fa
    o = min(1.15, o)
    for side in (-1, 1):
        root = (back[0] + side * 4, back[1] - 2)
        for lobe in range(2):
            upper = lobe == 0
            fa_ = (flap if upper else flap * 0.72) * 0.55
            plane = (1.25 + (0.12 - 1.25) * o) + fa_
            pc, ps = math.cos(plane), math.sin(plane)
            W = (46 if upper else 27) * (0.42 + 0.58 * o)
            H = (40 if upper else 25) * (0.42 + 0.58 * o)
            lr = (root[0] + (2 if upper else 5) * side, root[1] + (-1 if upper else 4))
            shape = upper_lobe if upper else lower_lobe
            # 1. VOLUMEN oscuro
            for cN in range(40 if upper else 22):
                cu = (hash01(side * 7 + lobe, cN, 9) + cN / (40 if upper else 22)) % 1.0
                cv = hash01(side * 7 + lobe, cN, 10)
                e = shape(cu)
                inn = (e[0] * (0.30 + 0.70 * cv), e[1] * (0.30 + 0.70 * cv))
                local = (inn[0] * W, -abs(inn[1]) * H * 0.9 - (0 if upper else 4))
                sp = (local[0] * pc - local[1] * ps, local[0] * ps + local[1] * pc)
                vol = lerp3((0.25, 0.05, 0.36), (0.44, 0.08, 0.50), 0.45 + 0.55 * cv)
                quad(c, lr[0] + sp[0] * side, lr[1] + sp[1], W * 0.30, H * 0.30, 0,
                     vol, 0.50 * (0.55 + 0.45 * o))
            # 2. MEMBRANA luminosa
            for cN in range(30 if upper else 16):
                cu = (hash01(side * 7 + lobe, cN, 1) + cN / (30 if upper else 16)) % 1.0
                cv = hash01(side * 7 + lobe, cN, 2)
                e = shape(cu)
                inn = (e[0] * (0.25 + 0.75 * cv), e[1] * (0.25 + 0.75 * cv))
                local = (inn[0] * W, -abs(inn[1]) * H * 0.9 - (0 if upper else 4))
                sp = (local[0] * pc - local[1] * ps, local[0] * ps + local[1] * pc)
                depth = 1 - cv * 0.7
                mem = lerp3((0.44, 0.0, 0.23), (0.99, 0.0, 0.59), 0.35 + 0.4 * cv)
                quad(c, lr[0] + sp[0] * side, lr[1] + sp[1], W * 0.34, H * 0.34, 0,
                     mem, 0.45 * depth * (0.6 + 0.4 * o))
            # 3. VENAS
            for v in range(5 if upper else 3):
                va = -0.15 + 1.35 * v / 4
                for s in range(8):
                    t = s / 7
                    bend = va * t + (1 - t) * 0.35
                    local = (math.sin(bend) * t * W * 0.92, -math.cos(bend) * t * H * 0.88)
                    sp = (local[0] * pc - local[1] * ps, local[0] * ps + local[1] * pc)
                    vein = lerp3((1.0, 0.61, 0.82), (1.0, 0.24, 0.75), t * 0.5)
                    th_ = 3.0 * (1 - t * 0.55) + 1.8
                    quad(c, lr[0] + sp[0] * side, lr[1] + sp[1], th_, th_, 0,
                         vein, 0.85 * (0.5 + 0.5 * o))
            # 4. BORDE dorado + OJO
            for r in range(24 if upper else 15):
                u = r / (24 if upper else 14)
                e = shape(u)
                local = (e[0] * W, -abs(e[1]) * H * 0.9 - (0 if upper else 4))
                sp = (local[0] * pc - local[1] * ps, local[0] * ps + local[1] * pc)
                rim = lerp3((1.0, 0.80, 0.38), (1.0, 0.94, 0.71), u * 0.6)
                quad(c, lr[0] + sp[0] * side, lr[1] + sp[1], 4.4, 4.4, 0,
                     rim, 0.95 * (0.55 + 0.45 * o))
            if upper and o > 0.3:
                el = (W * 0.62, -H * 0.48)
                sp = (el[0] * pc - el[1] * ps, el[0] * ps + el[1] * pc)
                eye = (lr[0] + sp[0] * side, lr[1] + sp[1])
                quad(c, eye[0], eye[1], VQ(6.0), VQ(6.0), 0, (1.0, 0.61, 0.82), 0.75, "ring")
                quad(c, eye[0], eye[1], 4.4, 4.4, 0, (0, 0, 0), 0.55, "orb")
        quad(c, root[0], root[1], 9.5, 12, 0, (1.0, 0.67, 0.31), 0.65)


# --------------------------------------------------------- HADA
def fairy(c, ctx):
    back, o, fp, fa, tm = ctx
    flutter = math.sin(fp) * (fa * 0.75 + 0.25)
    o = min(1.15, o)
    for side in (-1, 1):
        root = (back[0] + side * 4, back[1] - 3)
        for lobe in range(4):
            ba = {0: -1.28, 1: -0.88, 2: -0.30, 3: 0.10}[lobe]
            ln = {0: 26, 1: 31, 2: 21, 3: 16}[lobe]
            wid = {0: 9, 1: 10.5, 2: 8.5, 3: 6.5}[lobe]
            lag = lobe * 0.55
            ang = ba + flutter * (0.30 + 0.06 * lobe) * math.sin(fp - lag) * 1.6
            fold = 0.45 + (1 - 0.45) * o
            ang = -(-0.35 + (-ang - (-0.35)) * fold)
            dx, dy = math.cos(ang) * side, math.sin(ang)
            memrot = math.atan2(dy, dx)
            for cN in range(6):
                t = (cN + 0.5) / 6
                prof = math.sin(t * PI) * (1 - t * 0.35)
                w = wid * (0.55 + prof) * (0.5 + 0.5 * o)
                pos = (root[0] + dx * ln * t * fold, root[1] + dy * ln * t * fold + flutter * 1.2 * t)
                vol = lerp3((0.77, 0.44, 0.11), (0.91, 0.59, 0.24), t)
                quad(c, pos[0], pos[1], w * 2.3, ln / 6 * 1.6, memrot, vol,
                     0.55 * (0.55 + 0.45 * o))
                w2 = wid * (0.40 + prof * 0.6) * (0.5 + 0.5 * o)
                mem = lerp3((1.0, 0.85, 0.55), (1.0, 0.75, 0.47), t)
                quad(c, pos[0], pos[1], w2 * 1.7, ln / 6 * 1.3, memrot, mem,
                     0.45 * (0.5 + 0.5 * o))
            for s in range(10):
                t = s / 9
                prof = math.sin(t * PI)
                w = wid * (0.55 + prof) * (0.5 + 0.5 * o)
                ax, ay = dx, dy
                px_, py_ = -ay, ax
                for b in (-1, 1):
                    pos = (root[0] + ax * ln * t * fold + px_ * w * b,
                           root[1] + ay * ln * t * fold + py_ * w * b + flutter * 1.2 * t)
                    rim = lerp3((1.0, 0.94, 0.71), (1.0, 0.69, 0.31), t * 0.7)
                    quad(c, pos[0], pos[1], 3.4, 3.4, 0, rim, 0.90 * (0.55 + 0.45 * o))
            tip = (root[0] + dx * ln * fold, root[1] + dy * ln * fold + flutter * 1.2)
            quad(c, tip[0], tip[1], 4.6, 4.6, 0, (1.0, 0.96, 0.78), 0.85)
        for k in range(7):
            u, v = hash01(side, k, 11), hash01(side, k, 12)
            lk = k % 4
            ba = {0: -1.28, 1: -0.88, 2: -0.30, 3: 0.10}[lk]
            ln = {0: 26, 1: 31, 2: 21, 3: 16}[lk]
            fold = 0.45 + (1 - 0.45) * o
            ax, ay = math.cos(ba) * side, math.sin(ba)
            pos = (root[0] + ax * ln * u * fold - ay * (v - 0.5) * 16,
                   root[1] + ay * ln * u * fold + ax * (v - 0.5) * 16)
            ph = tm * (2.2 + 2.8 * v) + u * 37
            tw = (0.5 + 0.5 * math.sin(ph)) ** 3
            if tw > 0.05:
                sp = (1.0, 0.97, 0.86) if v > 0.5 else (1.0, 0.80, 0.94)
                quad(c, pos[0], pos[1], 3.6, 3.6, 0, sp, tw * 0.95)
        quad(c, root[0], root[1], 10, 10, 0, (1.0, 0.84, 0.59), 0.65)
        quad(c, root[0], root[1], 4.2, 4.2, 0, (1, 1, 1), 0.7)


# --------------------------------------------------------- HORIZONTE
def event_horizon(c, ctx):
    back, o, fp, fa, tm = ctx
    flap = math.sin(fp) * fa
    o = min(1.15, o)
    for side in (-1, 1):
        root = (back[0] + side * 7, back[1] + flap * 2.2)
        span = (54 + 16 * o) * (0.55 + 0.45 * o)
        lift = (40 + 14 * o) * (0.50 + 0.50 * o) * (1 + 0.10 * flap)
        stretch = 1 + 0.5 * 0 + 0.06 * flap
        prev = root
        for s in range(17):
            t = s / 16
            ax = 1 - (1 - t) ** 1.7
            ay = math.sin(t * PI * 0.88) * lift - t * t * lift * 0.22
            pos = (root[0] + side * ax * span * stretch, root[1] - ay)
            th = (4.6 - 3.5 * t) * (0.65 + 0.35 * o) + 1.1
            col = lerp3((1.0, 0.24, 0.75), (0.44, 0.0, 0.23), t * 0.85)
            inten = 0.72 + 0.42 * (1 - t * 0.6)
            d = (pos[0] - prev[0], pos[1] - prev[1])
            rot = math.atan2(d[1], d[0]) if (abs(d[0]) + abs(d[1])) > 0.01 else 0
            quad(c, pos[0], pos[1], th * 3.2, th * 1.7, rot, col, inten)
            if s % 3 == 1:
                j = (hash01(side, s, int(tm * 7)) - 0.5) * 3.4
                quad(c, pos[0] + j * 0.6, pos[1] + j, 3.2, 3.2, 0, (1.0, 0.61, 0.82), 0.65 * inten)
            prev = pos
        quad(c, prev[0], prev[1], 6.0, 6.0, 0, (1.0, 0.61, 0.82), 0.95)
        quad(c, prev[0], prev[1], VQ(10), VQ(10), 0, (1.0, 0.61, 0.82), 0.28, "ring")
        pulse = 0.85 + 0.25 * math.sin(tm * 2.6 + side * 1.3)
        orbR = 6.5
        quad(c, root[0], root[1], VQ(orbR * 1.55), VQ(orbR * 1.55), 0, (1.0, 0.61, 0.82), 0.75 * pulse, "ring")
        quad(c, root[0], root[1], orbR * 2, orbR * 2, 0, (0, 0, 0), 1.0, "orb")
        quad(c, root[0] - side * orbR * 0.55, root[1] - orbR * 0.72, 4.0, 4.0, 0, (1, 1, 1), 0.95 * pulse)
        if o > 0.45:
            for i in range(3):
                ang = tm * 3.1 + i * 2 * PI / 3 + side * 0.6
                bead = (root[0] + math.cos(ang) * orbR * 2.1, root[1] + math.sin(ang) * orbR * 0.8)
                quad(c, bead[0], bead[1], 4.2, 4.2, 0, (1.0, 0.24, 0.75), 0.95)
        hz = (root[0] + side * 24 * o, root[1] - 14 * o)
        quad(c, hz[0], hz[1], 74, 54, 0, (0.10, 0.04, 0.18), 0.60 * o)


# --------------------------------------------------------- ANILLO DE FOTONES
def photon_ring(c, ctx):
    back, o, fp, fa, tm = ctx
    flap = math.sin(fp) * fa
    o = min(1.15, o)
    for side in (-1, 1):
        root = (back[0] + side * 6, back[1] + flap * 2.0)
        pulse = 0.8 + 0.2 * math.sin(tm * 3.1 + side)
        quad(c, root[0], root[1], 13, 13, 0, (1.0, 0.24, 0.75), 0.70 * pulse)
        quad(c, root[0], root[1], 6.5, 6.5, 0, (1, 1, 1), 0.85 * pulse)
        # EL EJE DEL ALA (hombro→punta)
        wing_ang = -0.62 + 0.45 * 0.5 - flap * 0.30
        wdir = (math.cos(wing_ang) * side, math.sin(wing_ang))
        span = (52 + 12 * o) * (0.55 + 0.45 * o)
        # EL FILO DE ATAQUE (cinta dorada continua)
        prev = root
        for s in range(10):
            t = s / 9
            ax_ = 1 - (1 - t) ** 1.6
            ay_ = math.sin(t * PI * 0.55) * 0.30
            d_ = span * (ax_ + ay_ * 0.45)
            pos = (root[0] + wdir[0] * d_, root[1] + wdir[1] * d_)
            th = (4.4 - 3.2 * t) * (0.6 + 0.4 * o) + 1.2
            dd = (pos[0] - prev[0], pos[1] - prev[1])
            rot = math.atan2(dd[1], dd[0]) if abs(dd[0]) + abs(dd[1]) > 0.01 else 0
            edge = lerp3((1.0, 0.84, 0.47), (1.0, 0.98, 0.92), t * 0.7)
            quad(c, pos[0], pos[1], th * 3.4, th * 1.9, rot, edge, 0.90 * (0.55 + 0.45 * o))
            if s == 9:
                quad(c, pos[0], pos[1], 5.0, 5.0, 0, (1, 1, 1), 0.95)
            prev = pos
        # LAS PLUMAS DE ÓRBITA (3 aros barridos alineados al filo)
        for h in range(3):
            h01 = h / 2
            rx = (15 + 13 * h01) * (0.50 + 0.50 * o)
            ry = rx * 0.42
            d_ = (7 + 24 * h01) * (0.5 + 0.5 * o) + rx * 0.35
            ctr = (root[0] + wdir[0] * d_, root[1] + wdir[1] * d_)
            tilt = wing_ang - 0.10 * h01
            if side < 0:
                tilt = -tilt
            hoop = lerp3((1.0, 0.61, 0.82), (0.99, 0.0, 0.59), 0.25 + 0.45 * h01)
            ha = (0.55 + 0.35 * (1 - h01 * 0.5))
            quad(c, ctr[0], ctr[1], VQ(rx), VQ(ry), tilt, hoop, ha * 0.9, "ring")
            speed = 3.0 + 2.2 * h01 + fa * 2.0
            for i in range(2):
                ang = tm * speed + i * PI + h * 1.9 + side * 0.8
                loc = (math.cos(ang) * rx, math.sin(ang) * ry)
                ct_, st_ = math.cos(tilt), math.sin(tilt)
                ph = (ctr[0] + loc[0] * ct_ - loc[1] * st_,
                      ctr[1] + loc[0] * st_ + loc[1] * ct_)
                tw = 0.75 + 0.25 * math.sin(ang * 3 + h)
                quad(c, ph[0], ph[1], 8.5, 8.5, 0, (1.0, 0.24, 0.75), 0.60 * tw)
                quad(c, ph[0], ph[1], 4.2, 4.2, 0, (1, 1, 1), 0.90 * tw)


# --------------------------------------------------------- CORONA SOLAR
def solar(c, ctx):
    back, o, fp, fa, tm = ctx
    stroke = math.sin(fp)
    flap = (stroke ** 0.8 if stroke > 0 else stroke) * fa
    o = min(1.15, o)
    for side in (-1, 1):
        root = (back[0] + side * 6, back[1])
        hz = (root[0] + side * 21 * o, root[1] - 12 * o)
        quad(c, hz[0], hz[1], 84, 62, 0, (0.47, 0.16, 0.03), 0.50 * o)
        quad(c, hz[0], hz[1], 56, 42, 0, (0.35, 0.09, 0.02), 0.34 * o)
        for l in range(4):
            l01 = l / 3
            base = (5 + 13 * l01) * (0.52 + 0.48 * o)
            hgt = (30 + 19 * l01) * (0.52 + 0.48 * o) * (1 + 0.16 * flap)
            wdt = (15 + 8 * l01) * (0.52 + 0.48 * o)
            lr = (root[0] + side * base, root[1] + 2)
            sway = math.sin(tm * (1.1 + 0.3 * l01) + l * 1.7) * 2.2
            prev = lr
            for s in range(15):
                t = s / 14
                ax = (-wdt * 0.35 + (wdt + wdt * 0.35) * t)
                ay = math.sin(t * PI) * hgt * (1 - 0.10 * t)
                ax += math.sin(t * PI) * sway * (0.4 + l01 * 0.6)
                pos = (lr[0] + side * ax, lr[1] - ay)
                cooling = math.sin(t * PI)
                if cooling < 0.35:
                    col = lerp3((1.0, 0.98, 0.92), (1.0, 0.82, 0.43), cooling / 0.35)
                elif cooling < 0.75:
                    col = lerp3((1.0, 0.82, 0.43), (1.0, 0.51, 0.16), (cooling - 0.35) / 0.4)
                else:
                    col = lerp3((1.0, 0.51, 0.16), (0.78, 0.24, 0.08), (cooling - 0.75) / 0.25)
                th = (4.4 - 2.8 * cooling) * (0.6 + 0.4 * o)
                d = (pos[0] - prev[0], pos[1] - prev[1])
                rot = math.atan2(d[1], d[0]) if (abs(d[0]) + abs(d[1])) > 0.01 else 0
                flick = 1.0
                if cooling > 0.55:
                    flick = 0.65 + 0.35 * (1.0 if hash01(side + l, s, int(tm * 9)) > 0.45 else 0.3)
                quad(c, pos[0], pos[1], th * 3.0, th * 1.8, rot, col,
                     0.80 * flick * (0.5 + 0.5 * o))
                prev = pos
            apex = (lr[0] + side * (wdt * 0.5 + sway), lr[1] - hgt * 0.965)
            quad(c, apex[0], apex[1], 5.0, 5.0, 0, (1.0, 0.93, 0.75), 0.80 * (0.5 + 0.5 * o))
        quad(c, root[0], root[1], 11, 11, 0, (1.0, 0.96, 0.88), 0.95)
        quad(c, root[0], root[1], 22, 22, 0, (1.0, 0.78, 0.43), 0.55)
        quad(c, root[0], root[1], 36, 36, 0, (1.0, 0.51, 0.16), 0.36)


# --------------------------------------------------------- NEBULOSA
def nebula(c, ctx):
    back, o, fp, fa, tm = ctx
    flap = math.sin(fp) * fa
    o = min(1.15, o)
    for side in (-1, 1):
        root = (back[0] + side * 5, back[1] - 2)
        for cn in range(6):
            c01 = cn / 5
            drx = math.sin(tm * (0.7 + 0.4 * c01) + cn * 2.3 + side) * 3.5
            dry = math.sin(tm * (0.9 + 0.3 * c01) + cn * 1.7) * 2.5
            size = (16 + 17 * c01) * (0.42 + 0.58 * o) * (1 + 0.12 * math.sin(tm * (0.8 + 0.25 * c01) + cn * 3.1))
            outx = (12 + 30 * c01) * (0.45 + 0.55 * o)
            upy = (2 + 25 * c01) * (0.45 + 0.55 * o) * (1 + 0.08 * flap)
            ctr = (root[0] + side * (outx + drx), root[1] - upy + dry + flap * 2.0)
            quad(c, ctr[0], ctr[1], size * 2.2, size * 1.8, 0, (0.29, 0.07, 0.34), 0.55 * o)
            quad(c, ctr[0], ctr[1], size * 1.5, size * 1.25, 0, (0.66, 0.16, 0.75), 0.42 * o)
            quad(c, ctr[0], ctr[1], size * 0.8, size * 0.66, 0, (0.93, 0.43, 0.94), 0.30 * o)
        for f in range(3):
            f01 = f / 2
            prev = root
            for s in range(13):
                t = s / 12
                wig = math.sin(tm * 1.4 - t * 3.2 + f * 2.6) * (3.5 + 3 * t)
                ax = t * (50 + 18 * f01) * (0.45 + 0.55 * o)
                ay = math.sin(t * PI * 0.9) * (24 + 16 * f01) * (0.45 + 0.55 * o) - t * t * 8
                pos = (root[0] + side * ax + wig * 0.4 * side, root[1] - ay + wig + flap * 2.0)
                fil = lerp3((1.0, 0.51, 0.98), (1.0, 0.75, 1.0), t * 0.6)
                th = (3.4 - 2.2 * t) * (0.6 + 0.4 * o) + 1.6
                quad(c, pos[0], pos[1], th, th, 0, fil, 0.60 * o)
                prev = pos
        for k in range(5):
            u, v = hash01(side, k, 21), hash01(side, k, 22)
            sp = (root[0] + side * (8 + 38 * u) * (0.45 + 0.55 * o),
                  root[1] - (4 + 26 * v) * (0.45 + 0.55 * o) + flap * 2.0)
            ph = tm * (1.6 + 2.4 * v) + u * 41
            tw = (0.5 + 0.5 * math.sin(ph)) ** 2.5
            if tw > 0.04:
                st = (0.82, 0.98, 1.0) if v > 0.55 else (1.0, 0.96, 0.88)
                quad(c, sp[0], sp[1], 2.6, 2.6, 0, st, tw * 0.9)
                if tw > 0.75:
                    quad(c, sp[0], sp[1], 9.5, 1.1, 0, st, tw * 0.35)
                    quad(c, sp[0], sp[1], 1.1, 9.5, 0, st, tw * 0.35)
        quad(c, root[0], root[1], 14, 14, 0, (1.0, 0.55, 0.96), 0.60)


# --------------------------------------------------------- ECLIPSE
def eclipse(c, ctx):
    back, o, fp, fa, tm = ctx
    flap = math.sin(fp) * fa
    o = min(1.15, o)
    for side in (-1, 1):
        root = (back[0] + side * 6, back[1])
        # LA MEMBRANA: cuña de noche violeta root→lóbulo sup→lóbulo inf
        tip_up = (root[0] + side * (9 + 13 * o), root[1] - (2 + 9 * o) + flap * 1.8)
        tip_dn = (root[0] + side * (20 + 17 * o), root[1] + (10 + 9 * o) + flap * 1.8)
        for m in range(30):
            mt = hash01(side, m, 51)
            ms = hash01(side, m, 52)
            a = (root[0] + (tip_up[0] - root[0]) * mt, root[1] + (tip_up[1] - root[1]) * mt)
            b = (root[0] + (tip_dn[0] - root[0]) * mt, root[1] + (tip_dn[1] - root[1]) * mt)
            s_ = 0.15 + 0.70 * ms
            p = (a[0] + (b[0] - a[0]) * s_, a[1] + (b[1] - a[1]) * s_)
            taper = 1 - mt * 0.45
            vol = lerp3((0.12, 0.06, 0.17), (0.19, 0.10, 0.26), hash01(side, m, 53))
            quad(c, p[0], p[1], 13 * taper + 3, 11 * taper + 3, 0, vol,
                 0.52 * taper * (0.5 + 0.5 * o))
        # FILO SUPERIOR cromosférico de la raíz al lóbulo mayor
        prev = root
        for e in range(9):
            t = e / 8
            p = (root[0] + (tip_up[0] - root[0]) * t,
                 root[1] + (tip_up[1] - root[1]) * t - 4.5 * (1 - t * 0.4))
            dd = (p[0] - prev[0], p[1] - prev[1])
            rot = math.atan2(dd[1], dd[0]) if abs(dd[0]) + abs(dd[1]) > 0.01 else 0
            ec = lerp3((1.0, 0.90, 0.94), (0.82, 0.75, 0.92), t)
            quad(c, p[0], p[1], 7.5, 3.4, rot, ec, 0.85 * (0.5 + 0.5 * o))
            prev = p
        for disc in range(2):
            major = disc == 0
            R = (19 if major else 12.5) * (0.42 + 0.58 * o)
            ctr = (root[0] + side * ((9 + 13 * o) if major else (20 + 17 * o)),
                   root[1] + (-(2 + 9 * o) if major else (10 + 9 * o)) + flap * 1.8)
            for r in range(6):
                ang = 0.15 + (PI - 0.30) * r / 5
                dx, dy = math.cos(ang) * side, -math.sin(ang)
                bl = (R * 1.05 + R * 0.95 * hash01(side + disc, r, 31)) * (0.5 + 0.5 * o)
                wave = math.sin(tm * (1.0 + 0.35 * (r % 3)) + r * 1.9 + disc * 3.1)
                ln = bl * (1 + 0.10 * wave)
                rp = (ctr[0] + dx * (R + ln * 0.5), ctr[1] + dy * (R + ln * 0.5))
                rot = math.atan2(dy, dx)
                thick = 3.2 * (1 - 0.35 * (r % 2)) * (0.6 + 0.4 * o)
                rc = lerp3((0.92, 0.94, 1.0), (1.0, 0.88, 0.92), hash01(side + disc, r, 32))
                quad(c, rp[0], rp[1], ln, thick, rot, rc, 0.72 * (0.5 + 0.5 * o))
                quad(c, rp[0], rp[1], ln * 0.85, thick * 0.4, rot, (1.0, 0.98, 1.0), 0.55 * (0.5 + 0.5 * o))
            quad(c, ctr[0], ctr[1], VQ(R * 1.03), VQ(R * 1.03), 0, (1.0, 0.84, 0.89), 0.90 * (0.5 + 0.5 * o), "ring")
            quad(c, ctr[0], ctr[1], R * 2, R * 2, 0, (0, 0, 0), 1.0, "orb")
            if major:
                prom = 0.5 + 0.5 * math.sin(tm * 0.9)
                pp = (ctr[0] + side * R * 0.62, ctr[1] - R * 0.78)
                quad(c, pp[0], pp[1], 5.5 + 2.5 * prom, 4.0 + 1.8 * prom, 0, (1.0, 0.55, 0.67), 0.75 * prom)
        dusk = (root[0] + side * 16 * o, root[1] - 5 * o)
        quad(c, dusk[0], dusk[1], 68, 50, 0, (0.15, 0.08, 0.24), 0.45 * o)


# --------------------------------------------------------- COMETA
def comet(c, ctx):
    back, o, fp, fa, tm = ctx
    flap = math.sin(fp) * fa
    o = min(1.15, o)
    speed01 = 0.45
    sweep = speed01 * 0.9
    for side in (-1, 1):
        head = (back[0] + side * 7, back[1] - 3 + flap * 1.8)
        quad(c, head[0], head[1], 36, 36, 0, (1.0, 0.59, 0.27), 0.50)
        quad(c, head[0], head[1], 19, 19, 0, (1.0, 0.80, 0.51), 0.62)
        quad(c, head[0], head[1], 8.5, 8.5, 0, (1.0, 0.98, 0.94), 1.0)
        prev = head
        for s in range(19):
            t = s / 18
            L = (44 + 26 * o) * (0.55 + 0.45 * o) * (1 + sweep * 0.65 + 0.06 * flap)
            wave = math.sin(tm * 3.2 - t * 4.5) * (2.0 + 7.5 * t) * (0.35 + 0.65 * fa)
            cx = t * L
            cy = math.sin(t * PI * 0.75) * (30 * (0.45 + 0.55 * o)) * (1 - sweep * 0.55) - t * t * 11 * (1 - sweep * 0.4)
            cx = side * max(abs(cx), 6 + 30 * t)
            pos = (head[0] + cx * (1 - sweep * 0.25) + wave * 0.35 * side,
                   head[1] - cy + wave + flap * 1.8 * (1 - t * 0.5))
            th = (5.6 - 4.6 * t) * (0.55 + 0.45 * o) + 1.0
            if t < 0.18:
                col = lerp3((1.0, 0.98, 0.94), (1.0, 0.84, 0.47), t / 0.18)
            elif t < 0.55:
                col = lerp3((1.0, 0.84, 0.47), (1.0, 0.47, 0.24), (t - 0.18) / 0.37)
            else:
                col = lerp3((1.0, 0.47, 0.24), (0.59, 0.12, 0.10), (t - 0.55) / 0.45)
            stri = 0.8 + 0.2 * math.sin(t * 22 + tm * 4 + side * 2)
            d = (pos[0] - prev[0], pos[1] - prev[1])
            rot = math.atan2(d[1], d[0]) if (abs(d[0]) + abs(d[1])) > 0.01 else 0
            quad(c, pos[0], pos[1], th * 2.9, th * 1.6, rot, col,
                 0.78 * stri * (0.45 + 0.55 * o) * (1 - t * 0.25))
            prev = pos
        for m in range(4):
            u, v = hash01(side, m, 41), hash01(side, m, 42)
            tw = (0.5 + 0.5 * math.sin(tm * (2 + 3 * u) + v * 20)) ** 2
            mp = (prev[0] + (u - 0.5) * 14 + side * u * 8, prev[1] + (v - 0.5) * 12 - 3)
            quad(c, mp[0], mp[1], 3.0, 3.0, 0, (1.0, 0.78, 0.59), tw * 0.7)
        quad(c, prev[0], prev[1], 5.0, 5.0, 0, (1.0, 0.88, 0.71), 0.70)


# =====================================================================
#  ESCENAS: 8 estilos × 3 estados (reposo / aleteo / planeo)
# =====================================================================
STYLES = [
    ("EventHorizon", event_horizon), ("PhotonRing", photon_ring),
    ("Butterfly", butterfly), ("Fairy", fairy),
    ("SolarCorona", solar), ("Nebula", nebula),
    ("Eclipse", eclipse), ("Comet", comet),
]

STATES = [
    ("reposo", 0.22, 0.0, 0.0),          # (open, flap_phase, flap_amp)
    ("aleteo", 1.0, PI * 0.5, 1.0),
    ("planeo", 0.85, 2.4, 0.30),
]

TM = 1.7   # tiempo fijo (determinista)

sheet = Image.new("RGB", (CW * 3, CH * 8))
for row, (name, fn) in enumerate(STYLES):
    for col, (sname, o, fp, fa) in enumerate(STATES):
        c = sky_canvas()
        back = (CW // 2, 104)   # omóblatos: centro del torso −6px
        fn(c, (back, o, fp, fa, TM))
        img = to_img(c, back)
        d = ImageDraw.Draw(img)
        d.text((4, 4), f"{name} — {sname}", fill=(20, 20, 40))
        sheet.paste(img, (col * CW, row * CH))

sheet.save("/home/z/my-project/AethonMod/research/wings_v612/mock_v612_preview.png")
print("OK →", sheet.size, "escenas:", len(STYLES) * len(STATES))
