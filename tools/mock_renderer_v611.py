#!/usr/bin/env python3
"""
mock_renderer_v611.py — SIMULACIÓN EXACTA del CrimsonBlackHoleRenderer.cs v6.11.

Replica el camino del juego píxel a píxel:
  · Cap(): quad con la textura OblivionBlob.png REAL (128x128, perfil en RGB,
    alfa 255), TAMAÑO TOTAL (2·len, 2·wid), rotado, tintado (c·m clampeado,
    alfa 255), blending aditivo (SourceAlpha, One) → contribución = g(d)·c·m.
  · La esfera negra: AlphaBlend con Color.Black (opaco) ENCIMA del pase
    aditivo; los rayos/chispas/bloom aditivos DESPUÉS (visibles dentro).
  · Fondo: negro (como el prototipo calibrado) y cielo azul (como el juego
    del usuario) para validar visibilidad en ambos.
"""
import numpy as np
from PIL import Image
import math

BLOB = np.array(Image.open(
    '/home/z/my-project/AethonMod/AethonMod/Content/Effects/Procedural/OblivionBlob.png'
).convert('RGBA')).astype(np.float64)  # RGB = perfil, A = 255

SQ = 0.88
TILT = -0.42
R = 46.0

def key_lerp(ts, vs, t):
    if t <= ts[0]: return vs[0]
    if t >= ts[-1]: return vs[-1]
    for i in range(len(ts) - 1):
        if ts[i] <= t <= ts[i+1]:
            u = (t - ts[i]) / (ts[i+1] - ts[i])
            u = u * u * (3 - 2 * u)
            return vs[i] + (vs[i+1] - vs[i]) * u
    return vs[-1]

def key_color(ts, vs, t):
    if t <= ts[0]: return vs[0]
    if t >= ts[-1]: return vs[-1]
    for i in range(len(ts) - 1):
        if ts[i] <= t <= ts[i+1]:
            u = min(1.0, max(0.0, (t - ts[i]) / (ts[i+1] - ts[i])))
            return tuple(vs[i][k] + (vs[i+1][k] - vs[i][k]) * u for k in range(3))
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

_blob_cache = {}
def blob_quads(len_px, wid_px):
    """Muestreo de la textura 128x128 reescalada a (2·len, 2·wid) total (bilinear vía PIL)."""
    w = max(2, int(round(len_px * 2))); h = max(2, int(round(wid_px * 2)))
    key = (w, h)
    if key not in _blob_cache:
        im = Image.fromarray(BLOB.astype(np.uint8))
        _blob_cache[key] = np.array(im.resize((w, h), Image.BILINEAR)).astype(np.float64)
    return _blob_cache[key]

class Canvas:
    def __init__(self, W, H, bg):
        self.W, self.H = W, H
        self.acc = np.zeros((H, W, 3), dtype=np.float64)
        self.base = np.zeros((H, W, 3), dtype=np.float64)
        self.base[:] = bg

    def cap(self, x, y, ln, wd, rot, color, alpha):
        if alpha <= 0.004: return
        m = alpha * 1.4
        tint = np.array([min(255.0, color[k] * m) for k in range(3)])
        tex = blob_quads(ln, wd)  # (h, w, 4): RGB=perfil (0..255), A=255
        th, tw = tex.shape[0], tex.shape[1]
        # rotar con PIL (bicúbico como el sampler del juego)
        tim = Image.fromarray(tex.astype(np.uint8)).rotate(
            math.degrees(rot), resample=Image.BICUBIC, expand=False)
        tarr = np.array(tim).astype(np.float64)
        # área de destino
        x0 = int(x - tw / 2); y0 = int(y - th / 2)
        # recortes
        sx0 = max(0, -x0); sy0 = max(0, -y0)
        sx1 = min(tw, self.W - x0); sy1 = min(th, self.H - y0)
        if sx1 <= sx0 or sy1 <= sy0: return
        dst = self.acc[y0+sy0:y0+sy1, x0+sx0:x0+sx1]
        src = tarr[sy0:sy1, sx0:sx1]
        # aditivo: contribución = perfil(RGB) · tinte (alfa del quad = 255)
        for k in range(3):
            dst[:, :, k] += src[:, :, k] * (tint[k] / 255.0)

    def black_disk(self, x, y, r):
        yy, xx = np.mgrid[0:self.H, 0:self.W]
        d2 = (xx - x) ** 2 + (yy - y) ** 2
        inside = d2 <= (r * 0.985) ** 2
        self.acc[inside] = 0.0

def pol(rr, theta, CX, CY):
    x = math.cos(theta) * rr * R
    y = math.sin(theta) * rr * R * SQ
    ca, sa = math.cos(TILT), math.sin(TILT)
    return CX + x * ca - y * sa, CY + x * sa + y * ca

def blade_point(b, t, rot, CX, CY):
    ro = key_lerp(b['roT'], b['ro'], t)
    tk = key_lerp(b['tkT'], b['tk'], t)
    rm = ro - tk * 0.5
    th = math.radians(key_lerp(b['t'], b['th'], t)) + rot
    return pol(rm, th, CX, CY), th, rm, tk, ro

def draw_blade(cv, b, n_seg, rot, time, seed, CX, CY, filament_boost, jitter):
    for i in range(n_seg):
        t = i / (n_seg - 1)
        jr = (hash01(seed, i * 7 + 1, 13) - 0.5) * jitter
        (x, y), th, rm, tk, ro = blade_point(b, t, rot, CX, CY)
        rm += jr * 0.5
        x, y = pol(rm, th, CX, CY)
        eps = 1.5 / n_seg
        (x2, y2), _, _, _, _ = blade_point(b, min(t + eps, 1.0), rot, CX, CY)
        rot_a = math.atan2(y2 - y, x2 - x)
        seg_len = max(math.hypot(x2 - x, y2 - y) * 1.55, 3.0)
        col = key_color(b['colT'], b['col'], t)
        bri = key_lerp(b['briT'], b['bri'], t)
        stroke = 0.70 + 0.30 * math.sin(21 * t + time * 3.1 + math.sin(7.7 * t) * 2.0)
        stroke *= 0.90 + 0.10 * hash01(seed, i * 31 + 5, 77)
        al = min(2.1, bri * stroke * 1.3)
        cv.cap(x, y, seg_len, tk * R * 0.50, rot_a, col, al)
        for f in range(3):
            off = (f - 1) * 0.30 * tk
            fx, fy = pol(rm + off, th, CX, CY)
            fcol = tuple(col[k] + (255 - col[k]) * (0.60 if f == 1 else 0.35) for k in range(3))
            fal = al * filament_boost * (0.55 if f == 1 else 0.34)
            cv.cap(fx, fy, seg_len * 0.92, max(1.6, tk * R * 0.14), rot_a, fcol, fal)
        if 0.5 < t < 0.95 and hash01(seed, i * 13 + 3, 91) > 0.62:
            trail_r = ro + 0.18 + 0.5 * hash01(seed, i, 55)
            trail_th = th + 0.10
            for s in range(3):
                ur = trail_r + s * 0.55
                uth = trail_th + s * 0.16
                ux, uy = pol(ur, uth, CX, CY)
                ucol = tuple(col[k] + ((140 if k == 0 else 25 if k == 1 else 70) - col[k]) * (0.5 + 0.4 * s / 2) for k in range(3))
                cv.cap(ux, uy, 0.5 * R * (1 - s * 0.25), 0.11 * R, uth + TILT, ucol,
                       0.45 * (1 - s * 0.3) * bri)

def render(time=0.0, seed=7, bg=(0, 0, 0), W=660, H=540, CX=250, CY=255):
    rot = time * 0.16
    cv = Canvas(W, H, bg)
    # 1. halo
    cv.cap(CX, CY, 5.6 * R, 3.7 * R * SQ, TILT, (125, 18, 55), 0.13)
    cv.cap(CX, CY, 3.3 * R, 2.3 * R * SQ, TILT, (185, 36, 85), 0.12)
    # 2. anillo interior + rim
    for i in range(48):
        phi = i * 2 * math.pi / 48
        x, y = pol(1.48, phi, CX, CY)
        rot_a = math.atan2(math.cos(phi) * SQ, -math.sin(phi)) + TILT
        ang = math.degrees(phi) % 360
        merge = 0.5 + 0.5 * math.cos(math.radians(ang - 95 + math.degrees(rot)))
        brillo = 0.46 + 0.40 * max(0.0, merge)
        flick = 0.86 + 0.14 * math.sin(9 * phi + time * 6.0 + i * 2.3)
        col = (255, 66, 142) if math.sin(phi) < 0 else (255, 40, 108)
        cv.cap(x, y, 0.52 * R, 0.26 * R, rot_a, col, brillo * flick * 0.72)
        xi, yi = pol(1.26, phi, CX, CY)
        hot = 0.38 + 0.50 * max(0.0, merge)
        cv.cap(xi, yi, 0.40 * R, 0.16 * R, rot_a, (255, 238, 198), hot * flick * 0.45)
    # 3. hojas
    draw_blade(cv, BLADE, 112, rot, time, seed, CX, CY, 1.0, 0.30)
    draw_blade(cv, LOWER, 72, rot, time, seed, CX, CY, 0.65, 0.22)
    # 4. hotspot + nudo + aguja + mechones
    (hx, hy), hth, _, _, _ = blade_point(BLADE, 0.72, rot, CX, CY)
    cv.cap(hx, hy, 1.5 * R, 0.85 * R, hth, (255, 240, 168), 0.85)
    cv.cap(hx, hy, 0.65 * R, 0.40 * R, hth, (255, 253, 232), 1.10)
    kx, ky = pol(3.0, math.radians(355), CX, CY)
    cv.cap(kx, ky, 0.55 * R, 0.34 * R, math.radians(355), (255, 246, 150), 0.55)
    (tx, ty), tth, _, _, _ = blade_point(BLADE, 0.97, rot, CX, CY)
    cv.cap(tx, ty, 0.4 * R, 0.14 * R, tth, (255, 210, 170), 0.5)
    for wI in range(3):
        wth = math.radians(352 + wI * 9) + rot
        for s in range(3):
            wr = 5.9 + s * 0.35 + 0.2 * hash01(wI, s, 3)
            wx, wy = pol(wr, wth, CX, CY)
            wc = (200 + (110 - 200) * s / 2, 45 + (18 - 45) * s / 2, 95 + (48 - 95) * s / 2)
            cv.cap(wx, wy, 0.45 * R * (1 - s * 0.2), 0.10 * R, wth, wc, 0.50 - s * 0.12)
    # 5. esfera negra
    cv.black_disk(CX, CY, R)
    # 6. rayos dentro
    fr = int(time * 7)
    for bi in range(3):
        if (fr + bi * 3) % 5 >= 2: continue
        th0 = math.radians(-60 + bi * 130 + (fr * 47) % 360)
        x0, y0 = CX + math.cos(th0) * R * 0.92, CY + math.sin(th0) * R * 0.92
        x1, y1 = CX + math.cos(th0 + 1.9) * R * 0.45, CY + math.sin(th0 + 1.9) * R * 0.45
        px, py = x0, y0
        for s in range(1, 6):
            u = s / 5
            jx = (hash01(fr, bi * 10 + s, 11) - 0.5) * R * 0.40 * (1 - u)
            jy = (hash01(fr, bi * 10 + s, 17) - 0.5) * R * 0.40 * (1 - u)
            nx, ny = x0 + (x1 - x0) * u + jx, y0 + (y1 - y0) * u + jy
            mx, my = (px + nx) / 2, (py + ny) / 2
            ra = math.atan2(ny - py, nx - px)
            ln = math.hypot(nx - px, ny - py) * 0.8
            cv.cap(mx, my, max(ln, 1.5), 1.3, ra, (150, 170, 255), 0.30)
            if s in (2, 4) and hash01(fr, bi * 7 + s, 23) > 0.4:
                bra = ra + (hash01(fr, s, 29) - 0.5) * 1.6
                bx, by = mx + math.cos(bra) * R * 0.20, my + math.sin(bra) * R * 0.20
                cv.cap((mx + bx) / 2, (my + by) / 2, R * 0.12, 1.1, bra, (170, 185, 255), 0.32)
            px, py = nx, ny
    # 7. chispas
    sparks = [(0.72, 3.6), (0.55, 2.7), (0.82, 4.6), (0.30, 2.3), (0.62, 3.4), (0.90, 5.2), (0.12, 2.0)]
    for k, (ts_, rs_) in enumerate(sparks):
        tw = 0.5 + 0.5 * math.sin(time * (5 + k * 1.7) + k * 2.9)
        if tw < 0.55: continue
        th = math.radians(key_lerp(BLADE['t'], BLADE['th'], ts_)) + rot
        x, y = pol(rs_, th, CX, CY)
        cv.cap(x, y, 2.6, 2.6, 0, (255, 252, 222), 1.0 * tw)
        cv.cap(x, y, 5.5, 5.5, 0, (255, 238, 165), 0.34 * tw)
    # 8. bloom
    cv.cap(hx, hy, 2.8 * R, 1.8 * R, hth, (255, 195, 135), 0.20)
    cv.cap(CX, CY, 3.5 * R, 2.6 * R * SQ, TILT, (255, 115, 185), 0.09)

    # composición final: base + acumulador aditivo, clip 255
    out = np.clip(cv.base + cv.acc, 0, 255).astype(np.uint8)
    return Image.fromarray(out)

if __name__ == '__main__':
    render(0.0).save('/tmp/v611_black.png')
    render(0.0, bg=(120, 160, 250)).save('/tmp/v611_sky.png')
    render(2.3, bg=(120, 160, 250)).save('/tmp/v611_sky_t2.png')
    print('guardados /tmp/v611_black.png /tmp/v611_sky.png /tmp/v611_sky_t2.png')
