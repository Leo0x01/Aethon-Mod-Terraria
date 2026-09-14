#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_storm_v621.py — LA VERIFICACIÓN VISUAL DE LA TORMENTA.

Traduce StormLib 1:1 a Python (mismas constantes, mismas capas) y monta un
mock de la descarga completa del Cetro del Trueno sobre las TEXTURAS
REALES del mod, con blending aditivo — para validar el look "rayo de
verdad" antes de que el usuario compile.

  · Panel A: EL TELEGRAPH (línea fina + anillo objetivo + carga).
  · Panel B: EL RAYO (multi-filamento cayendo del cielo + impacto).
  · Panel C: la MUERTE VIOLENTA (deathGrow ×3.2 — el rayo revienta).
"""

import os
import math
import numpy as np
from PIL import Image

HERE = os.path.dirname(__file__)
PROC = os.path.join(HERE, '..', 'AethonMod', 'Content', 'Effects', 'Procedural')
OUT = os.path.join(HERE, '..', 'research', 'storm_v621')

W, H = 460, 640


# =====================================================================
#  LA LIBRERÍA TRADUCIDA (StormLib → Python, 1:1)
# =====================================================================

def hash01(seed, a, b):
    h = (seed * 374761393 + a * 668265263 + b * 1911520717) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFFFF) / 16777216.0


def zig_path(start, end, seed, flick, segments=12, amp=24.0):
    sx, sy = start
    ex, ey = end
    dx, dy = ex - sx, ey - sy
    ln = math.hypot(dx, dy)
    if ln < 0.001:
        return [(sx, sy)] * (segments + 1)
    ux, uy = dx / ln, dy / ln
    nx, ny = -uy, ux
    amp_len = min(amp, ln * 0.30)
    pts = []
    for i in range(segments + 1):
        t = i / segments
        env = math.sin(t * math.pi) ** 0.8
        j = (hash01(seed, flick, i * 31 + 7) - 0.5) * 2 * amp_len * env
        d = (hash01(seed, flick, i * 13 + 3) - 0.5) * 2 * amp_len * 0.35 * env
        pts.append((sx + ux * ln * t + nx * j + ux * d,
                    sy + uy * ln * t + ny * j + uy * d))
    pts[0] = (sx, sy)
    pts[-1] = (ex, ey)
    return pts


def refine(pts, seed, flick, amp_scale=0.5):
    """EL REFINO FRACTAL (subdivisión de punto medio con sesgo cúbico)."""
    if len(pts) < 2:
        return pts
    out = []
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        out.append(a)
        seg = (b[0] - a[0], b[1] - a[1])
        ln = math.hypot(*seg)
        if ln < 0.5:
            continue
        ux, uy = seg[0] / ln, seg[1] / ln
        nx, ny = -uy, ux
        h = hash01(seed, flick * 3 + 1, i * 47 + 29) - 0.5
        j = math.copysign(abs(h) * 2 ** 1.5, h) * ln * 0.22 * amp_scale
        out.append(((a[0] + b[0]) * 0.5 + nx * j, (a[1] + b[1]) * 0.5 + ny * j))
    out.append(pts[-1])
    return out


def path_len(pts):
    return sum(math.hypot(pts[i + 1][0] - pts[i][0], pts[i + 1][1] - pts[i][1])
               for i in range(len(pts) - 1))


def taper_factor(kind, t):
    if kind == 'Linear':
        return 1.0 - 0.62 * t
    if kind == 'Impact':
        return 0.30 + 0.70 * (t ** 0.6)
    return max(0.35, math.sin(t * math.pi) ** 0.65)


class Canvas:
    """Blending ADITIVO con texturas rotadas (el SpriteBatch del mock)."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.buf = np.zeros((h, w, 3), dtype=np.float64)

    def quad(self, tex_arr, pos, size, rot, color, alpha):
        # tex_arr: float HxWx4 (RGB blanco, A=perfil) — el quad del mock.
        tw, th = size
        if tw < 0.1 or th < 0.1 or alpha <= 0.003:
            return
        cr, cg, cb = color
        # Muestrea el quad en una rejilla orientada.
        diag = int(math.hypot(tw, th)) + 1
        if diag > 1400:
            return
        xs = np.linspace(-tw / 2, tw / 2, min(diag, 400))
        ys = np.linspace(-th / 2, th / 2, min(diag, 400))
        gx, gy = np.meshgrid(xs, ys)
        c, s = math.cos(rot), math.sin(rot)
        px = pos[0] + gx * c - gy * s
        py = pos[1] + gx * s + gy * c
        # Coordenadas de textura (0..1 dentro del quad).
        u = (gx / tw + 0.5)
        v = (gy / th + 0.5)
        inb = (u >= 0) & (u <= 1) & (v >= 0) & (v <= 1)
        # Muestreo bilinear del perfil alfa de la textura.
        th_, tw_ = tex_arr.shape[0], tex_arr.shape[1]
        fx = np.clip(u * (tw_ - 1), 0, tw_ - 1)
        fy = np.clip(v * (th_ - 1), 0, th_ - 1)
        x0 = np.floor(fx).astype(int); x1 = np.minimum(x0 + 1, tw_ - 1)
        y0 = np.floor(fy).astype(int); y1 = np.minimum(y0 + 1, th_ - 1)
        wa = (fx - x0) * inb; wb = (fy - y0) * inb
        a00 = tex_arr[y0, x0]; a10 = tex_arr[y0, x1]
        a01 = tex_arr[y1, x0]; a11 = tex_arr[y1, x1]
        prof = (a00 * (1 - wa) * (1 - wb) + a10 * wa * (1 - wb) +
                a01 * (1 - wa) * wb + a11 * wa * wb)
        # Pinta aditivo en el buffer (recorte al lienzo).
        xi = np.round(px).astype(int)
        yi = np.round(py).astype(int)
        ok = (xi >= 0) & (xi < self.w) & (yi >= 0) & (yi < self.h) & inb
        if not np.any(ok):
            return
        r = np.clip(yi[ok], 0, self.h - 1)
        cq = np.clip(xi[ok], 0, self.w - 1)
        add = prof[ok][:, None] * np.array([cr, cg, cb]) * (alpha / 255.0)
        np.add.at(self.buf, (r, cq), add)

    def image(self, bg=(8, 9, 16)):
        out = np.clip(self.buf + np.array(bg), 0, 255).astype(np.uint8)
        return Image.fromarray(out, 'RGB')


def strand(cv, tex_halo, tex_core, pts, seed, flick, width,
           halo, core, alpha=1.0, taper='Center'):
    if len(pts) < 2 or alpha <= 0.01:
        return
    total = path_len(pts)
    if total < 1:
        return
    SUB = 42.0
    arc, k = 0.0, 0
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        seg = (b[0] - a[0], b[1] - a[1])
        ln = math.hypot(*seg)
        if ln < 0.35:
            arc += ln
            continue
        m = max(1, math.ceil(ln / SUB))
        for s_i in range(m):
            f0, f1 = s_i / m, (s_i + 1) / m
            p0 = (a[0] + seg[0] * f0, a[1] + seg[1] * f0)
            p1 = (a[0] + seg[0] * f1, a[1] + seg[1] * f1)
            sub = (p1[0] - p0[0], p1[1] - p0[1])
            sub_len = math.hypot(*sub)
            if sub_len < 0.3:
                arc += sub_len
                continue
            rot = math.atan2(sub[1], sub[0])
            mid = ((p0[0] + p1[0]) * 0.5, (p0[1] + p1[1]) * 0.5)
            t_mid = (arc + sub_len * 0.5) / total
            w = width * taper_factor(taper, t_mid)
            crackle = 0.62 + 0.38 * hash01(seed, flick, k * 41 + 17)
            # 1) HALO (contenido) 2) CUERPO 3) NÚCLEO razor-fino
            cv.quad(tex_halo, mid, (sub_len + w * 2.0, w * 2.0), rot,
                    halo, 255 * 0.30 * alpha * crackle)
            cv.quad(tex_core, mid, (sub_len + w * 1.0, w * 1.0), rot,
                    halo, 255 * 0.85 * alpha * crackle)
            cv.quad(tex_core, mid, (sub_len + w * 0.45, w * 0.26), rot,
                    core, 255 * 1.0 * alpha)
            arc += sub_len
            k += 1
    # Gorros (2 escalas).
    for p in (pts[0], pts[-1]):
        cv.quad(tex_glow, p, (width * 3.2, width * 3.2), 0, halo, 255 * 0.42 * alpha)
        cv.quad(tex_glow, p, (width * 1.8, width * 1.8), 0, core, 255 * 0.78 * alpha)


def fork_tree(trunk, seed, flick, branch_scale=0.5, max_branches=3):
    strands = [(trunk, 1.0, 1.0)]
    if len(trunk) < 4:
        return strands
    for b in range(max_branches):
        idx = 2 + int(hash01(seed, flick * 7 + b, 313) * (len(trunk) - 4))
        idx = max(1, min(idx, len(trunk) - 2))
        origin = trunk[idx]
        seg = (trunk[idx + 1][0] - trunk[idx - 1][0],
               trunk[idx + 1][1] - trunk[idx - 1][1])
        sl = math.hypot(*seg)
        if sl < 0.01:
            continue
        seg = (seg[0] / sl, seg[1] / sl)
        side = 1 if hash01(seed, flick * 3 + b, 617) > 0.5 else -1
        spread = 0.45 + 0.45 * hash01(seed, flick + b, 619)
        ang = math.atan2(seg[1], seg[0]) + side * spread
        dirv = (math.cos(ang), math.sin(ang))
        trunk_len = path_len(trunk)
        branch_len = trunk_len * (0.14 + 0.16 * hash01(seed, b, 623))
        STEPS = 7
        pts, pos, total_rot = [], origin, 0.0
        for s in range(STEPS + 1):
            pts.append(pos)
            if s == STEPS:
                break
            rot = (hash01(seed, flick + s * 13, 811 + b) - 0.5) * 0.6
            rot -= total_rot * 0.3
            ang2 = math.atan2(dirv[1], dirv[0]) + rot
            dirv = (math.cos(ang2), math.sin(ang2))
            total_rot += rot
            pos = (pos[0] + dirv[0] * branch_len / STEPS,
                   pos[1] + dirv[1] * branch_len / STEPS)
        strands.append((pts, branch_scale, 0.85))
    return strands


def multi_bolt(cv, start, end, seed, flick, width, haloA, haloB, core,
               alpha=1.0, amp=26.0, segments=12):
    trunk = refine(zig_path(start, end, seed, flick, segments, amp), seed, flick)
    strand(cv, tex_halo, tex_core, trunk, seed, flick, width, haloA, core, alpha)
    for f, (pts, ws, al) in enumerate(fork_tree(trunk, seed, flick, 0.40, 4)[1:]):
        strand(cv, tex_halo, tex_core, pts, seed + 17 + f, flick,
               width * ws, haloA, core, alpha * al, 'Linear')
    dx, dy = end[0] - start[0], end[1] - start[1]
    ln = math.hypot(dx, dy)
    if ln > 40:
        ux, uy = dx / ln, dy / ln
        nx, ny = -uy, ux
        if hash01(seed + 101, 1234 + flick, 977) < 0.55:
            off = (nx * ln * 0.035, ny * ln * 0.035)
            ptsB = refine(zig_path((start[0] + off[0] * 1.2, start[1] + off[1] * 1.2),
                                  (end[0] + off[0] * 0.4, end[1] + off[1] * 0.4),
                                  seed + 211, flick, segments, amp * 0.8), seed + 211, flick)
            strand(cv, tex_halo, tex_core, ptsB, seed + 211, flick,
                   width * 0.52, haloB, core, alpha * 0.85)
        if hash01(seed + 307, 1234 + flick, 977) < 0.45:
            off = (-nx * ln * 0.045, -ny * ln * 0.045)
            ptsC = refine(zig_path((start[0] + off[0] * 0.8, start[1] + off[1] * 0.8),
                                  (end[0] + off[0] * 0.3, end[1] + off[1] * 0.3),
                                  seed + 419, flick, segments + 3, amp * 0.9),
                          seed + 419, flick)
            strand(cv, tex_halo, tex_core, ptsC, seed + 419, flick,
                   width * 0.38, haloA, core, alpha * 0.70)
        # LOS PELOS (hair) caóticos finísimos
        for hair in range(2):
            chance = 0.50 if hair == 0 else 0.40
            if hash01(seed + 533 + hair * 97, 1234 + flick, 977) >= chance:
                continue
            hs = seed + 533 + hair * 97
            sgn = 1 if hair == 0 else -1
            k = 0.02 + 0.03 * hash01(hs, 3, 997)
            off = (nx * ln * k * sgn, ny * ln * k * sgn)
            ptsH = refine(zig_path((start[0] + off[0], start[1] + off[1]),
                                  (end[0] + off[0] * 0.2, end[1] + off[1] * 0.2),
                                  hs, flick, segments + 5, amp * 1.35), hs, flick, 0.7)
            strand(cv, tex_halo, tex_core, ptsH, hs, flick,
                   width * (0.24 if hair == 0 else 0.20),
                   haloB if hair == 0 else haloA, core, alpha * 0.55)


def impact_flash(cv, pos, size, color, intensity, spin):
    if intensity <= 0.02:
        return
    white = (255, 250, 235)
    cv.quad(tex_impact, pos, (size * 2.0, size * 2.0), spin, color, 255 * 0.50 * intensity)
    cv.quad(tex_impact, pos, (size * 1.25, size * 1.25), -spin * 0.7, color, 255 * 0.70 * intensity)
    cv.quad(tex_glow, pos, (size * 0.62, size * 2.35), 0, color, 255 * 0.40 * intensity)
    cv.quad(tex_glow, pos, (size * 2.35, size * 0.62), 0, color, 255 * 0.40 * intensity)
    cv.quad(tex_glow, pos, (size * 0.40, size * 1.55), 0, white, 255 * 0.50 * intensity)
    cv.quad(tex_glow, pos, (size * 1.55, size * 0.40), 0, white, 255 * 0.50 * intensity)
    cv.quad(tex_glow, pos, (size * 0.85, size * 0.85), 0, color, 255 * 0.80 * intensity)
    cv.quad(tex_glow, pos, (size * 0.42, size * 0.42), 0, white, 255 * 0.95 * intensity)


# =====================================================================
#  LA ESCENA
# =====================================================================

def load(name):
    im = Image.open(os.path.join(PROC, name)).convert('RGBA')
    return np.asarray(im, dtype=np.float64)[:, :, 3] / 255.0


tex_halo = load('BoltHalo.png')
tex_core = load('BoltCore.png')
tex_chain = load('BoltChain.png')
tex_impact = load('BoltImpact.png')
tex_glow = load('SoftGlow.png')

GOLD = (255, 195, 85)
GOLDW = (255, 225, 140)
BLUE = (150, 180, 255)
WHITE = (255, 250, 235)


def panel_telegraph():
    cv = Canvas(W, H)
    seed, flick = 55, 3
    sky = (W * 0.5 + 90, -160)
    strike = (W * 0.5 - 60, H - 90)
    charge = 0.7
    pts = zig_path(sky, strike, seed, flick, 10, 14.0)
    for i in range(len(pts) - 1):
        seg = (pts[i + 1][0] - pts[i][0], pts[i + 1][1] - pts[i][1])
        ln = math.hypot(*seg)
        rot = math.atan2(seg[1], seg[0])
        mid = ((pts[i][0] + pts[i + 1][0]) * 0.5, (pts[i][1] + pts[i + 1][1]) * 0.5)
        cv.quad(tex_halo, mid, (ln + 6, 6.0), rot, GOLDW, 255 * 0.35 * charge)
    strand(cv, tex_halo, tex_core, pts, seed, flick, 2.6, GOLD, WHITE, 0.6 * charge)
    # anillo objetivo
    rr = 39.0
    ring = np.zeros((128, 128), dtype=np.float64)
    yy, xx = np.mgrid[0:128, 0:128]
    rad = np.sqrt((xx - 63.5) ** 2 + (yy - 63.5) ** 2)
    ring[:] = np.exp(-((rad - 44) / 7.0) ** 2)
    cv.quad(ring, strike, (rr * 2.4, rr * 2.4), 0.4, GOLDW, 255 * 0.45)
    cv.quad(tex_glow, strike, (22, 22), 0, GOLDW, 255 * 0.5)
    return cv.image()


def panel_strike(death=False):
    cv = Canvas(W, H)
    seed, flick = 55, 3
    sky = (W * 0.5 + 90, -160)
    strike = (W * 0.5 - 60, H - 90)
    alpha = 1.0
    amp = 26.0
    if death:
        alpha = 0.55
        amp = 26.0 * 3.2
    multi_bolt(cv, sky, strike, seed, flick, 11.0, GOLD, BLUE, WHITE, alpha, amp, 14)
    # cadenas
    for c, tgt in enumerate([(90, 300), (330, 260)]):
        if hash01(seed + 61 + c * 37, 1234 + flick, 977) < 0.55:
            strand(cv, tex_halo, tex_chain,
                   zig_path(strike, tgt, seed + 61 + c * 37, flick, 7, 9.0),
                   seed + 61 + c * 37, flick, 4.4, BLUE, (230, 240, 255),
                   0.9 * alpha, 'Linear')
    if not death:
        impact_flash(cv, strike, 62.0, GOLDW, 1.0, 0.8)
        impact_flash(cv, strike, 43.0, GOLDW, 0.6, -1.1)
    else:
        impact_flash(cv, strike, 30.0, GOLDW, 0.35, 0.8)
    return cv.image()


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    SS = 2  # supersampling: mata el aliasing del mock (el juego muestrea bilinear)
    globals()['W'], globals()['H'] = W * SS, H * SS
    a = panel_telegraph()
    b = panel_strike(False)
    c = panel_strike(True)
    a = a.resize((a.width // SS, a.height // SS), Image.LANCZOS)
    b = b.resize((b.width // SS, b.height // SS), Image.LANCZOS)
    c = c.resize((c.width // SS, c.height // SS), Image.LANCZOS)
    sheet = Image.new('RGB', (a.width * 3 + 40, a.height + 20), (6, 7, 12))
    for i, im in enumerate((a, b, c)):
        sheet.paste(im, (10 + i * (a.width + 10), 10))
    sheet.save(os.path.join(OUT, 'mock_storm_v621.png'))
    b.save(os.path.join(OUT, 'mock_storm_strike.png'))
    print('OK →', os.path.join(OUT, 'mock_storm_v621.png'))
