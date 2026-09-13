#!/usr/bin/env python3
"""
mock_blackhole_v613.py — SIMULACIÓN del CrimsonBlackHoleRenderer v6.13
(base funcional v6.12 + las SIETE capas nuevas de personalidad).

Modelo idéntico al juego: Cap() con la textura OblivionBlob.png REAL
(perfil en RGB, alfa 255), tamaño total (2·len, 2·wid), tinte c·1.4
clampeado, blending aditivo; la esfera negra con AlphaBlend opaco entre
pases. Las capas nuevas:
  0.5 ONDAS DE ESPACIO-TIEMPO (textura Ring real, aplastada e inclinada)
  2.5 PULSOS DE FOTONES (2 destellos corriendo el anillo)
  3.5 CHORROS RELATIVISTAS (2 haces polares + bolas viajando)
  3.6 CORRIENTES DE MATERIA (5 espirales cayendo tras el horizonte)
  3.7 LLAMARADAS DEL DISCO (prominencias periódicas)
  3.8 ARCOS DE EINSTEIN (filamentos pálidos arriba/abajo)
  6.5 RIM VIOLETA (el borde del horizonte respirando)
"""
import numpy as np
from PIL import Image
import math

BLOB = np.array(Image.open(
    '/home/z/my-project/AethonMod/AethonMod/Content/Effects/Procedural/OblivionBlob.png'
).convert('RGBA')).astype(np.float64)

RING = np.array(Image.open(
    '/home/z/my-project/AethonMod/AethonMod/Content/Effects/Procedural/Ring.png'
).convert('RGBA')).astype(np.float64)

BLACK = np.array(Image.open(
    '/home/z/my-project/AethonMod/AethonMod/Content/Effects/Procedural/BlackDisk.png'
).convert('RGBA')).astype(np.float64)

SQ = 0.88
TILT = -0.42
R = 46.0
SWIRL = 0.16


def key_lerp(ts, vs, t):
    if t <= ts[0]:
        return vs[0]
    if t >= ts[-1]:
        return vs[-1]
    for i in range(len(ts) - 1):
        if ts[i] <= t <= ts[i + 1]:
            u = (t - ts[i]) / (ts[i + 1] - ts[i])
            u = u * u * (3 - 2 * u)
            return vs[i] + (vs[i + 1] - vs[i]) * u
    return vs[-1]


def key_color(ts, vs, t):
    if t <= ts[0]:
        return vs[0]
    if t >= ts[-1]:
        return vs[-1]
    for i in range(len(ts) - 1):
        if ts[i] <= t <= ts[i + 1]:
            u = min(1.0, max(0.0, (t - ts[i]) / (ts[i + 1] - ts[i])))
            return tuple(vs[i][k] + (vs[i + 1][k] - vs[i][k]) * u for k in range(3))
    return vs[-1]


def hash01(seed, a, b):
    h = (seed * 374761393 + a * 668265263 + b * 1911520717) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFFFF) / 16777216.0


BLADE = dict(
    t=[0.0, 1.0], th=[175.0, 352.0],
    roT=[0.00, 0.25, 0.45, 0.65, 0.80, 0.90, 1.00],
    ro=[2.30, 3.30, 3.00, 3.30, 3.20, 4.20, 5.90],
    tkT=[0.00, 0.30, 0.55, 0.80, 1.00],
    tk=[1.00, 0.80, 0.55, 0.32, 0.07],
    briT=[0.00, 0.30, 0.72, 0.86, 1.00],
    bri=[0.60, 0.80, 1.00, 0.78, 0.52],
    colT=[0.00, 0.30, 0.55, 0.72, 0.84, 0.93, 1.00],
    col=[(255, 42, 122), (255, 80, 160), (255, 150, 205), (255, 250, 155),
         (255, 110, 200), (222, 38, 98), (145, 18, 48)],
)
LOWER = dict(
    t=[0.0, 1.0], th=[20.0, 205.0],
    roT=[0.00, 0.25, 0.50, 0.65, 0.80, 1.00],
    ro=[1.95, 3.20, 6.30, 6.00, 4.80, 2.60],
    tkT=[0.00, 0.35, 0.60, 0.85, 1.00],
    tk=[0.65, 0.50, 0.35, 0.18, 0.08],
    briT=[0.00, 0.30, 0.50, 0.75, 1.00],
    bri=[0.60, 0.78, 0.82, 0.50, 0.22],
    colT=[0.00, 0.30, 0.50, 0.75, 1.00],
    col=[(255, 60, 140), (255, 82, 172), (250, 45, 125), (222, 36, 100), (140, 18, 55)],
)


def pol(rr, theta, cx, cy):
    x = math.cos(theta) * rr * R
    y = math.sin(theta) * rr * R * SQ
    ca, sa = math.cos(TILT), math.sin(TILT)
    return (cx + x * ca - y * sa, cy + x * sa + y * ca)


def pol_tangent(rr, theta):
    eps = 0.02
    a = pol(rr, theta - eps, 0, 0)
    b = pol(rr, theta + eps, 0, 0)
    return math.atan2(b[1] - a[1], b[0] - a[0])


class Canvas:
    def __init__(self, W, H, bg):
        self.W, self.H = W, H
        self.acc = np.zeros((H, W, 3), dtype=np.float64)
        self.acc[:] = bg

    def _rot_tex(self, tex, w, h, rot):
        im = Image.fromarray(tex.astype(np.uint8))
        if rot != 0:
            im = im.rotate(-math.degrees(rot), Image.BICUBIC, expand=True)
        im = im.resize((max(2, w), max(2, h)), Image.BILINEAR)
        return np.array(im).astype(np.float64)

    def cap(self, x, y, ln, wd, rot, color, alpha):
        if alpha <= 0.004:
            return
        m = alpha * 1.4
        tint = np.array([min(255.0, color[k] * m) for k in range(3)])
        tex = self._rot_tex(BLOB, int(round(ln * 2)), int(round(wd * 2)), rot)
        th, tw = tex.shape[:2]
        x0, y0 = int(x - tw / 2), int(y - th / 2)
        x1, y1 = x0 + tw, y0 + th
        if x1 <= 0 or y1 <= 0 or x0 >= self.W or y0 >= self.H:
            return
        sx0, sy0 = max(0, -x0), max(0, -y0)
        sx1, sy1 = tw - max(0, x1 - self.W), th - max(0, y1 - self.H)
        x0, y0 = max(0, x0), max(0, y0)
        prof = tex[sy0:sy1, sx0:sx1, :3] / 255.0
        self.acc[y0:y0 + sy1 - sy0, x0:x0 + sx1 - sx0] += prof * tint

    def ring_quad(self, x, y, radius, color, alpha):
        """v6.13: textura Ring aplastada e inclinada."""
        if alpha <= 0.004:
            return
        m = alpha * 1.4
        tint = np.array([min(255.0, color[k] * m) for k in range(3)])
        s = radius * 2.174
        tex = self._rot_tex(RING, int(round(s)), int(round(s * SQ)), TILT)
        th, tw = tex.shape[:2]
        x0, y0 = int(x - tw / 2), int(y - th / 2)
        x1, y1 = x0 + tw, y0 + th
        if x1 <= 0 or y1 <= 0 or x0 >= self.W or y0 >= self.H:
            return
        sx0, sy0 = max(0, -x0), max(0, -y0)
        sx1, sy1 = tw - max(0, x1 - self.W), th - max(0, y1 - self.H)
        x0, y0 = max(0, x0), max(0, y0)
        prof = tex[sy0:sy1, sx0:sx1, :3] / 255.0
        self.acc[y0:y0 + sy1 - sy0, x0:x0 + sx1 - sx0] += prof * tint

    def black_disk(self, x, y):
        r = R
        tex = self._rot_tex(BLACK, int(round(r * 2)), int(round(r * 2)), 0)
        th, tw = tex.shape[:2]
        x0, y0 = int(x - tw / 2), int(y - th / 2)
        x1, y1 = x0 + tw, y0 + th
        sx0, sy0 = max(0, -x0), max(0, -y0)
        sx1, sy1 = tw - max(0, x1 - self.W), th - max(0, y1 - self.H)
        x0, y0 = max(0, x0), max(0, y0)
        # AlphaBlend con negro opaco: dst = dst·(1−a) 
        a = tex[sy0:sy1, sx0:sx1, 3:4] / 255.0 * 0.94
        self.acc[y0:y0 + sy1 - sy0, x0:x0 + sx1 - sx0] *= (1 - a)


def blade_point(b, t, rot, cx, cy):
    ro = key_lerp(b['roT'], b['ro'], t)
    tk = key_lerp(b['tkT'], b['tk'], t)
    rm = ro - tk * 0.5
    theta = math.radians(key_lerp(b['t'], b['th'], t)) + rot
    return pol(rm, theta, cx, cy)


def draw_blade(cv, b, n_seg, rot, time, seed, cx, cy, filament_boost, jitter):
    for i in range(n_seg):
        t = i / (n_seg - 1)
        jr = (hash01(seed, i * 7 + 1, 13) - 0.5) * jitter
        ro = key_lerp(b['roT'], b['ro'], t)
        tk = key_lerp(b['tkT'], b['tk'], t)
        rm = ro - tk * 0.5 + jr * 0.5
        th = math.radians(key_lerp(b['t'], b['th'], t)) + rot
        pos = pol(rm, th, cx, cy)
        pos2 = blade_point(b, min(t + 1.5 / n_seg, 1.0), rot, cx, cy)
        rotA = math.atan2(pos2[1] - pos[1], pos2[0] - pos[0])
        segLen = max(math.hypot(pos2[0] - pos[0], pos2[1] - pos[1]) * 1.55, 3.0)
        col = key_color(b['colT'], b['col'], t)
        bri = key_lerp(b['briT'], b['bri'], t)
        stroke = 0.70 + 0.30 * math.sin(21 * t + time * 3.1 + math.sin(7.7 * t) * 2)
        stroke *= 0.90 + 0.10 * hash01(seed, i * 31 + 5, 77)
        al = min(2.1, bri * stroke * 1.3)
        cv.cap(pos[0], pos[1], segLen, tk * R * 0.50, rotA, col, al)

        for f in range(3):
            off = (f - 1) * 0.30 * tk
            fp = pol(rm + off, th, cx, cy)
            fcol = tuple(col[k] + ((255, 255, 235)[k] - col[k]) * (0.60 if f == 1 else 0.35) for k in range(3))
            fal = al * filament_boost * (0.55 if f == 1 else 0.34)
            cv.cap(fp[0], fp[1], segLen * 0.92, max(1.6, tk * R * 0.14), rotA, fcol, fal)

        if 0.5 < t < 0.95 and hash01(seed, i * 13 + 3, 91) > 0.62:
            trailR = ro + 0.18 + 0.5 * hash01(seed, i, 55)
            trailTh = th + 0.10
            for s in range(3):
                ur = trailR + s * 0.55
                uth = trailTh + s * 0.16
                up = pol(ur, uth, cx, cy)
                ucol = tuple(col[k] + ((140, 25, 70)[k] - col[k]) * (0.5 + 0.4 * s / 2) for k in range(3))
                cv.cap(up[0], up[1], 0.5 * R * (1 - s * 0.25), 0.11 * R, uth + TILT, ucol,
                       0.45 * (1 - s * 0.3) * bri)


def render(time=2.0, seed=7, bg=(0, 0, 0), W=680, H=560, CX=260, CY=260, scale=1.0):
    global R
    R = 46.0 * scale
    rot = time * SWIRL
    cv = Canvas(W, H, bg)

    # ==== 0.5 ONDAS DE ESPACIO-TIEMPO ====
    RIPPLE = 2.8
    for wv in range(2):
        age = (time / RIPPLE + wv * 0.5) % 1.0
        rr = R * (1.9 + age * 5.3)
        a = 0.26 * (1 - age) * min(age * 7, 1)
        cv.ring_quad(CX, CY, rr, (255, 95, 185), a)

    # ==== 1. HALO ====
    for ln, wd, col, al in [(5.6, 3.7, (125, 18, 55), 0.13), (3.3, 2.3, (185, 36, 85), 0.12)]:
        cv.cap(CX, CY, ln * R, wd * R * SQ, TILT, col, al)

    # ==== 2. ANILLO INTERIOR + rim ====
    NR = 48
    for i in range(NR):
        phi = i * 2 * math.pi / NR
        pos = pol(1.48, phi, CX, CY)
        rotA = math.atan2(math.cos(phi) * SQ, -math.sin(phi)) + TILT
        ang = math.degrees(phi)
        merge = 0.5 + 0.5 * math.cos(math.radians(ang - 95 + math.degrees(rot)))
        brillo = 0.46 + 0.40 * max(0, merge)
        flick = 0.86 + 0.14 * math.sin(9 * phi + time * 6 + i * 2.3)
        col = (255, 66, 142) if math.sin(phi) < 0 else (255, 40, 108)
        cv.cap(pos[0], pos[1], 0.52 * R, 0.26 * R, rotA, col, brillo * flick * 0.72)
        hotPos = pol(1.26, phi, CX, CY)
        hot = 0.38 + 0.50 * max(0, merge)
        cv.cap(hotPos[0], hotPos[1], 0.40 * R, 0.16 * R, rotA, (255, 238, 198), hot * flick * 0.45)

    # ==== 2.5 PULSOS DE FOTONES ====
    for pp in range(2):
        pTh = rot * 2.4 + pp * math.pi
        ppos = pol(1.46, pTh, CX, CY)
        tang = pol_tangent(1.46, pTh)
        pPulse = 0.75 + 0.25 * math.sin(time * 9 + pp * 2.0)
        cv.cap(ppos[0], ppos[1], 0.52 * R, 0.15 * R, tang, (255, 246, 238), 0.80 * pPulse)
        cv.cap(ppos[0], ppos[1], 0.17 * R, 0.075 * R, tang, (255, 255, 252), 1.05 * pPulse)
        trail = pol(1.46, pTh - 0.14, CX, CY)
        cv.cap(trail[0], trail[1], 0.34 * R, 0.085 * R, tang, (255, 190, 215), 0.45 * pPulse)

    # ==== 3. LAS DOS HOJAS ====
    draw_blade(cv, BLADE, 112, rot, time, seed, CX, CY, 1.0, 0.30)
    draw_blade(cv, LOWER, 72, rot, time, seed, CX, CY, 0.65, 0.22)

    # ==== 3.5 CHORROS RELATIVISTAS ====
    jdir = (-math.sin(TILT), math.cos(TILT))
    jet_len = 4.4
    for j in range(2):
        d = 1.0 if j == 0 else -1.0
        for s in range(11):
            u = (s + 0.5) / 11
            pos = (CX + jdir[0] * d * (0.30 + u * jet_len) * R,
                   CY + jdir[1] * d * (0.30 + u * jet_len) * R)
            wOut = 0.30 * (1 - u * 0.82)
            fade = (1 - u) * (0.16 + 0.84 * min(u * 4, 1))
            cv.cap(pos[0], pos[1], 0.34 * R, wOut * R, TILT - math.pi / 2, (255, 205, 235), 0.72 * fade)
            cv.cap(pos[0], pos[1], 0.42 * R, wOut * 1.7 * R, TILT - math.pi / 2, (168, 110, 255), 0.34 * fade)
        for k in range(3):
            bu = 0.45 + ((time * 0.42 + k / 3 + j * 0.5) % 1.0) * 3.6
            bpos = (CX + jdir[0] * d * bu * R, CY + jdir[1] * d * bu * R)
            bp = 0.65 + 0.35 * math.sin(time * 7 + k * 2.1 + j * 1.3)
            cv.cap(bpos[0], bpos[1], 0.17 * R, 0.17 * R, 0, (255, 248, 252), 1.0 * bp)
            cv.cap(bpos[0], bpos[1], 0.40 * R, 0.40 * R, 0, (200, 130, 255), 0.40 * bp)

    # ==== 3.6 CORRIENTES DE MATERIA ====
    for st in range(5):
        h1 = hash01(seed, st, 3)
        h2 = hash01(seed, st, 7)
        cyc = 2.6 + h1 * 1.7
        ph = (time / cyc + h2) % 1.0
        baseAng = h1 * 2 * math.pi
        for k in range(8):
            u = ph - k * 0.028
            if u < 0:
                continue
            ease = u ** 1.45
            rr = 5.4 + (1.44 - 5.4) * ease
            th = baseAng + u * 4.6 + rot * 0.6
            pos = pol(rr, th, CX, CY)
            tang = pol_tangent(rr, th)
            mix = 1 - (rr - 1.44) / 3.96
            col = tuple((255, 70, 130)[k2] + ((255, 244, 250)[k2] - (255, 70, 130)[k2]) * mix for k2 in range(3))
            fadeIn = min(u * 9, 1)
            fadeOut = (1 - u) / 0.10 if u > 0.90 else 1
            al = 0.85 * fadeIn * fadeOut * (1 - k * 0.09)
            cv.cap(pos[0], pos[1], 0.34 * R * (1 - 0.45 * mix), 0.10 * R, tang, col, al)

    # ==== 3.7 LLAMARADAS DEL DISCO ====
    jn = jdir
    FLARE = 3.4
    for fl in range(3):
        ft = (time + fl * 1.13) % FLARE
        if ft > 2.1:
            continue
        life = ft / 2.1
        env = math.sin(life * math.pi)
        at = 0.28 + fl * 0.16
        fpos = blade_point(BLADE, at, rot, CX, CY)
        fTh = math.radians(key_lerp(BLADE['t'], BLADE['th'], at)) + rot
        fRo = key_lerp(BLADE['roT'], BLADE['ro'], at)
        pts = []
        for s in range(8):
            u = s / 7
            h = math.sin(u * math.pi) * 1.15 * env
            rr = fRo + u * 1.30
            th = fTh + u * 0.26
            p = pol(rr, th, CX, CY)
            pts.append((p[0] + jn[0] * h * R, p[1] + jn[1] * h * R))
        for s in range(7):
            a, b = pts[s], pts[s + 1]
            mid = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5)
            ra = math.atan2(b[1] - a[1], b[0] - a[0])
            ln = max(math.hypot(b[0] - a[0], b[1] - a[1]) * 0.85, 2.5)
            u = (s + 1) / 7
            fc = tuple((255, 120, 80)[k2] + ((255, 235, 190)[k2] - (255, 120, 80)[k2]) * math.sin(u * math.pi) for k2 in range(3))
            cv.cap(mid[0], mid[1], ln, 0.11 * R * (1 - u * 0.35), ra, fc, 0.55 * env)

    # ==== 3.8 ARCOS DE EINSTEIN ====
    for arc in range(2):
        a0 = -0.42 if arc == 0 else math.pi - 0.42
        for s in range(11):
            th = a0 + s / 10 * 1.55
            pos = pol(1.80, th, CX, CY)
            tang = pol_tangent(1.80, th)
            ab = 0.15 * math.sin(s / 10 * math.pi)
            cv.cap(pos[0], pos[1], 0.22 * R, 0.040 * R, tang, (255, 205, 230), ab)

    # ==== 4. HOTSPOT + nudo + aguja + mechones ====
    hx = blade_point(BLADE, 0.72, rot, CX, CY)
    hTh = math.atan2(hx[1] - CY, hx[0] - CX)
    cv.cap(hx[0], hx[1], 1.5 * R, 0.85 * R, hTh, (255, 240, 168), 0.85)
    cv.cap(hx[0], hx[1], 0.65 * R, 0.40 * R, hTh, (255, 253, 232), 1.10)
    knot = pol(3.0, math.radians(355), CX, CY)
    cv.cap(knot[0], knot[1], 0.55 * R, 0.34 * R, math.radians(355), (255, 246, 150), 0.55)
    tip = blade_point(BLADE, 0.97, rot, CX, CY)
    tTh = math.atan2(tip[1] - CY, tip[0] - CX)
    cv.cap(tip[0], tip[1], 0.4 * R, 0.14 * R, tTh, (255, 210, 170), 0.5)
    for wI in range(3):
        wTh = math.radians(352 + wI * 9) + rot
        for s in range(3):
            wr = 5.9 + s * 0.35 + 0.2 * hash01(wI, s, 3)
            wp = pol(wr, wTh, CX, CY)
            wc = tuple((200, 45, 95)[k2] + ((110, 18, 48)[k2] - (200, 45, 95)[k2]) * s / 2 for k2 in range(3))
            cv.cap(wp[0], wp[1], 0.45 * R * (1 - s * 0.2), 0.10 * R, wTh, wc, 0.50 - s * 0.12)

    # ==== 5. ESFERA NEGRA (come la luz) ====
    cv.black_disk(CX, CY)

    # ==== 6. RAYOS AZUL-VIOLETA ====
    frame = int(time * 7)
    for bi in range(3):
        if (frame + bi * 3) % 5 >= 2:
            continue
        th0 = math.radians(-60 + bi * 130 + (frame * 47) % 360)
        p0 = (CX + math.cos(th0) * R * 0.92, CY + math.sin(th0) * R * 0.92)
        p1 = (CX + math.cos(th0 + 1.9) * R * 0.45, CY + math.sin(th0 + 1.9) * R * 0.45)
        prev = p0
        for s in range(1, 6):
            u = s / 5
            jx = (hash01(frame, bi * 10 + s, 11) - 0.5) * R * 0.40 * (1 - u)
            jy = (hash01(frame, bi * 10 + s, 17) - 0.5) * R * 0.40 * (1 - u)
            nxt = (p0[0] + (p1[0] - p0[0]) * u + jx, p0[1] + (p1[1] - p0[1]) * u + jy)
            mid = ((prev[0] + nxt[0]) * 0.5, (prev[1] + nxt[1]) * 0.5)
            ra = math.atan2(nxt[1] - prev[1], nxt[0] - prev[0])
            ln = max(math.hypot(nxt[0] - prev[0], nxt[1] - prev[1]) * 0.8, 1.5)
            cv.cap(mid[0], mid[1], ln, 1.3, ra, (150, 170, 255), 0.30)
            prev = nxt

    # ==== 6.5 RIM VIOLETA ====
    rimPulse = 0.55 + 0.45 * math.sin(time * 1.7)
    for s in range(18):
        th = s * 2 * math.pi / 18 + rot * 0.22
        pos = pol(1.045, th, CX, CY)
        tang = pol_tangent(1.045, th)
        flick = 0.8 + 0.2 * math.sin(6 * th + time * 3.5)
        cv.cap(pos[0], pos[1], 0.21 * R, 0.045 * R, tang, (196, 138, 255), 0.34 * rimPulse * flick)

    # ==== 7. CHISPAS ====
    sparkT = [0.72, 0.55, 0.82, 0.30, 0.62, 0.90, 0.12]
    sparkR = [3.6, 2.7, 4.6, 2.3, 3.4, 5.2, 2.0]
    for k in range(len(sparkT)):
        tw = 0.5 + 0.5 * math.sin(time * (5 + k * 1.7) + k * 2.9)
        if tw < 0.55:
            continue
        th = math.radians(key_lerp(BLADE['t'], BLADE['th'], sparkT[k])) + rot
        sp = pol(sparkR[k], th, CX, CY)
        cv.cap(sp[0], sp[1], 2.6, 2.6, 0, (255, 252, 222), 1.0 * tw)
        cv.cap(sp[0], sp[1], 5.5, 5.5, 0, (255, 238, 165), 0.34 * tw)

    # ==== 8. BLOOM ====
    cv.cap(hx[0], hx[1], 2.8 * R, 1.8 * R, hTh, (255, 195, 135), 0.20)
    cv.cap(CX, CY, 3.5 * R, 2.6 * R * SQ, TILT, (255, 115, 185), 0.09)

    return np.clip(cv.acc, 0, 255).astype(np.uint8)


def main():
    for name, bg in [('black', (0, 0, 0)), ('sky', (108, 160, 220))]:
        for t, tag in [(2.0, 'a'), (2.6, 'b'), (3.1, 'c')]:
            img = render(time=t, seed=7, bg=bg)
            Image.fromarray(img).save(
                f'/home/z/my-project/research/blackhole/mock_v613_{name}_{tag}.png')
    print('OK')


if __name__ == '__main__':
    main()
