#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_3agujeros_v616.py — SIMULACIÓN EXACTA de los TRES agujeros nuevos:

  · CosmicBlackHoleRenderer v6.16 (nacido del script Unity del usuario)
  · UmbralBlackHoleRenderer  v6.16 (el agujero de la REFERENCIA)
  · BrumaBlackHoleRenderer   v6.17 (con la LIBRERÍA DE BRUMA)

sobre las texturas-pincel REALES del mod (SoftGlow/Ring/BlackDisk) y,
para la Bruma, sobre puffs fBm HORNEADOS con la MISMA matemática que
BrumaNoise.cs/BrumaBrushes.cs (value noise quintic + fBm lacunaridad 2 +
domain warping + ByMids + falloff smoothstep).

Modelo de blending del juego (FNA/XNA SpriteBatch):
  · Additive (SrcAlpha, One):  acc += texel.rgb · texel.a · tint.rgb · tint.a
  · AlphaBlend:                acc = texel.rgb·tint.rgb·(texel.a·tint.a)
                                       + acc·(1 − texel.a·tint.a)
Tint() del mod: rgb PLENO + alfa = f → brillo LINEAL.

Se traduce Draw() 1:1 (mismas constantes, mismo orden de capas) con
time=1.7 y seed=7. Fondo: espacio oscuro + variante de cielo diurno.
"""

import math
import numpy as np
from PIL import Image, ImageDraw

BASE = '/home/z/my-project/AethonMod/AethonMod/Content/Effects/Procedural'
GLOW = np.array(Image.open(f'{BASE}/SoftGlow.png').convert('RGBA')).astype(np.float64)
RING = np.array(Image.open(f'{BASE}/Ring.png').convert('RGBA')).astype(np.float64)
DISK = np.array(Image.open(f'{BASE}/BlackDisk.png').convert('RGBA')).astype(np.float64)


# =====================================================================
#  BRUMA NOISE — la MISMA matemática que BrumaNoise.cs
# =====================================================================

def bhash(x, y, seed):
    h = (seed * 374761393 + x * 668265263 + y * 1911520717) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFFFF) / 16777216.0


def quintic(t):
    return t * t * t * (t * (t * 6 - 15) + 10)


def bvalue(x, y, seed):
    xi, yi = int(math.floor(x)), int(math.floor(y))
    tx, ty = quintic(x - xi), quintic(y - yi)
    a = bhash(xi, yi, seed)
    b = bhash(xi + 1, yi, seed)
    c = bhash(xi, yi + 1, seed)
    d = bhash(xi + 1, yi + 1, seed)
    ab = a + (b - a) * tx
    cd = c + (d - c) * tx
    return ab + (cd - ab) * ty


def bfbm(x, y, seed, octaves=5):
    s, amp, norm = 0.0, 0.5, 0.0
    fx = fy = 1.0
    for _ in range(octaves):
        s += amp * bvalue(x * fx, y * fy, seed)
        norm += amp
        amp *= 0.5
        fx *= 2.0
        fy *= 2.0
    return s / norm


def warped_fbm(x, y, seed, warp=3.0):
    qx = bfbm(x, y, seed)
    qy = bfbm(x + 5.2, y + 1.3, seed)
    return bfbm(x + warp * qx, y + warp * qy, seed + 7)


def smoothstep(e0, e1, t):
    t = np.clip((t - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def by_mids(n, k=1.6):
    return 0.5 + (n - 0.5) * k


# =====================================================================
#  BRUMA BRUSHES — el MISMO horneado que BrumaBrushes.cs (8 variantes)
# =====================================================================

def hornear_puff(seed):
    S = 128
    yy, xx = np.mgrid[0:S, 0:S].astype(float)
    d = np.hypot(xx - S / 2 + 0.5, yy - S / 2 + 0.5) / (S * 0.5)
    falloff = 1.0 - smoothstep(0.55, 1.0, d)
    n = np.zeros((S, S))
    for j in range(S):
        for i in range(S):
            n[j, i] = warped_fbm(i * 4 / S, j * 4 / S, 977 + seed * 131, 3.0)
    n = by_mids(n, 1.6)
    a = np.clip(falloff * n * 1.35, 0, 1)
    tex = np.zeros((S, S, 4))
    tex[..., 0] = 255
    tex[..., 1] = 255
    tex[..., 2] = 255
    tex[..., 3] = a * 255
    return tex


PUFFS = [hornear_puff(s) for s in range(8)]


# =====================================================================
#  CANVAS — el modelo de blending del juego
# =====================================================================

class Canvas:
    def __init__(self, W, H, bg):
        self.W, self.H = W, H
        self.acc = np.zeros((H, W, 3), dtype=np.float64)
        self.acc[:] = np.array(bg) / 255.0

    def _sprite(self, tex, size, rot):
        im = Image.fromarray(tex.astype(np.uint8))
        im = im.resize((max(2, int(round(size[0]))), max(2, int(round(size[1])))),
                       Image.BILINEAR)
        if rot != 0:
            im = im.rotate(-math.degrees(rot), Image.BICUBIC, expand=True,
                           fillcolor=(0, 0, 0, 0))
        return np.array(im).astype(np.float64)

    def quad(self, tex, pos, size, rot, tint):
        if tint[3] <= 1:
            return
        spr = self._sprite(tex, size, rot)
        th, tw = spr.shape[:2]
        x0, y0 = int(pos[0] - tw / 2), int(pos[1] - th / 2)
        x1, y1 = x0 + tw, y0 + th
        if x1 <= 0 or y1 <= 0 or x0 >= self.W or y0 >= self.H:
            return
        sx0, sy0 = max(0, -x0), max(0, -y0)
        sx1, sy1 = tw - max(0, x1 - self.W), th - max(0, y1 - self.H)
        x0, y0 = max(0, x0), max(0, y0)
        sub = spr[sy0:sy1, sx0:sx1]
        src_a = (sub[..., 3:4] / 255.0) * (tint[3] / 255.0)
        src_rgb = (sub[..., :3] / 255.0) * (np.array(tint[:3]) / 255.0)
        dst = self.acc[y0:y0 + sy1 - sy0, x0:x0 + sx1 - sx0]
        self.acc[y0:y0 + sy1 - sy0, x0:x0 + sx1 - sx0] = \
            dst * (1 - src_a) + src_rgb * src_a

    def quad_add(self, tex, pos, size, rot, tint):
        if tint[3] <= 1:
            return
        spr = self._sprite(tex, size, rot)
        th, tw = spr.shape[:2]
        x0, y0 = int(pos[0] - tw / 2), int(pos[1] - th / 2)
        x1, y1 = x0 + tw, y0 + th
        if x1 <= 0 or y1 <= 0 or x0 >= self.W or y0 >= self.H:
            return
        sx0, sy0 = max(0, -x0), max(0, -y0)
        sx1, sy1 = tw - max(0, x1 - self.W), th - max(0, y1 - self.H)
        x0, y0 = max(0, x0), max(0, y0)
        sub = spr[sy0:sy1, sx0:sx1]
        contrib = ((sub[..., :3] / 255.0) *
                   (sub[..., 3:4] / 255.0) *
                   (np.array(tint[:3]) / 255.0) *
                   (tint[3] / 255.0))
        self.acc[y0:y0 + sy1 - sy0, x0:x0 + sx1 - sx0] += contrib

    def capsule(self, pos, ln, wd, rot, tint):
        self.quad_add(GLOW, pos, (ln + wd, wd * 1.9), rot, tint)

    def ring_quad(self, pos, radius, rot, tint):
        s = radius * 2.174
        self.quad_add(RING, pos, (s, s), rot, tint)

    def save(self, path):
        out = np.clip(self.acc * 255.0, 0, 255).astype(np.uint8)
        Image.fromarray(out).save(path)


def hash01(seed, a, b):
    h = (seed * 374761393 + a * 668265263 + b * 1911520717) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFFFF) / 16777216.0


def xna_mul(c, f):
    f = min(max(f, 0.0), 1.0)
    return (float(c[0]), float(c[1]), float(c[2]), min(255.0, 255.0 * f))


def lerp_c(c1, c2, t):
    return tuple(c1[k] + (c2[k] - c1[k]) * t for k in range(3))


def mk_ellipse(A, B, TILT):
    def ell(c, r, t):
        a, b = A * r, B * r
        ct, st = math.cos(t), math.sin(t)
        lx, ly = a * ct, b * st
        cr, sr = math.cos(TILT), math.sin(TILT)
        return (c[0] + lx * cr - ly * sr, c[1] + lx * sr + ly * cr)
    return ell


def draw_bolt(cv, start, end, seed, flick, width, halo, core):
    dx, dy = end[0] - start[0], end[1] - start[1]
    length = math.hypot(dx, dy)
    if length < 4:
        return
    dirv = (dx / length, dy / length)
    normal = (-dirv[1], dirv[0])
    amp = min(length * 0.16, 0.30 * width * 4)
    SEG = 6
    pts = []
    for s in range(SEG + 1):
        f = s / SEG
        env = math.sin(f * math.pi)
        jit = (hash01(seed, flick, s) - 0.5) * 2 * amp * env
        pts.append((start[0] + dirv[0] * length * f + normal[0] * jit,
                    start[1] + dirv[1] * length * f + normal[1] * jit))
    for s in range(SEG):
        a, b = pts[s], pts[s + 1]
        mid = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
        seg = (b[0] - a[0], b[1] - a[1])
        seglen = math.hypot(*seg)
        if seglen < 0.5:
            continue
        rot = math.atan2(seg[1], seg[0])
        cv.capsule(mid, seglen, width * 2.0, rot, halo)
        cv.capsule(mid, seglen, width * 0.8, rot, core)
        if 0 < s < SEG - 1 and hash01(seed, flick, s + 91) > 0.62:
            side = 1.0 if hash01(seed, flick, s + 37) > 0.5 else -1.0
            blen = (0.35 + 0.4 * hash01(seed, flick, s + 53)) * length * 0.25
            bd = (dirv[0] * 0.45 + normal[0] * side, dirv[1] * 0.45 + normal[1] * side)
            n = math.hypot(*bd)
            bd = (bd[0] / n, bd[1] / n)
            bEnd = (b[0] + bd[0] * blen, b[1] + bd[1] * blen)
            bMid = ((b[0] + bEnd[0]) / 2, (b[1] + bEnd[1]) / 2)
            bRot = math.atan2(bd[1], bd[0])
            cv.capsule(bMid, blen, width * 1.3, bRot, tuple(x * 0.6 for x in halo))
            cv.capsule(bMid, blen, width * 0.5, bRot, tuple(x * 0.6 for x in core))
    cv.quad_add(GLOW, start, (width * 5, width * 5), 0, halo)
    cv.quad_add(GLOW, start, (width * 2.6, width * 2.6), 0, core)
    cv.quad_add(GLOW, end, (width * 4, width * 4), 0, halo)


# =====================================================================
#  1. CÓSMICO — CosmicBlackHoleRenderer v6.16 (script Unity)
# =====================================================================

C_SPHERE = 50.0
C_A, C_B, C_TILT = 1.90, 1.18, -0.38
C_SPIN, C_BANDF, C_BANDS = 0.349, 20.0, 5.0
C_SEG = 44
C_RUNES_N, C_RUNE_R, C_RUNE_ORB = 8, 2.55, 0.10
C_WAVE_CYCLE, C_WAVE_N = 2.4, 2

C_RING_MAG = (255, 51, 204)
C_HOT_W = (255, 238, 252)
C_DVIO = (150, 40, 255)
C_BVIO = (170, 70, 255)
C_BPINK = (255, 80, 200)
C_RGOLD = (255, 170, 60)
C_RTIP = (255, 232, 170)
C_NEBV = (90, 30, 190)
C_NEBB = (40, 70, 200)
C_AURA = (190, 30, 150)

C_RUNES = [
    [(0, 7), (4, 0), (4, 0), (0, -7), (0, -7), (-4, 0), (-4, 0), (0, 7), (0, 5), (0, -5)],
    [(-3.5, 6), (-3.5, 2), (-3.5, 2), (3.5, -2), (3.5, -2), (3.5, -6)],
    [(-3.5, -5), (3.5, 5), (3.5, -5), (-3.5, 5), (-3.5, 0), (3.5, 0)],
    [(-3, 6.5), (3, 6.5), (3, 6.5), (0, 3.5), (-3, 1), (3, 1), (3, 1), (0, -2), (-3, -4.5), (3, -4.5), (0, -7), (0, -2)],
    [(-3.5, 6), (-3.5, -4), (-3.5, -4), (3.5, -4), (3.5, -4), (3.5, 6), (3.5, 6), (-3.5, 6), (-3.5, 1), (3.5, 1)],
    [(0, 7), (0, -7), (-4, 0), (4, 0), (-2.5, -2.5), (2.5, 2.5), (2.5, -2.5), (-2.5, 2.5)],
    [(0, 7), (0, -3), (0, -3), (-3.5, -6.5), (0, -3), (3.5, -6.5), (-2.5, 4), (2.5, 4)],
    [(0, 6.5), (3.5, 0), (3.5, 0), (0, -6.5), (0, -6.5), (-3.5, 0), (-3.5, 0), (0, 6.5), (-1.2, -1), (1.2, 1)],
]

c_ell = mk_ellipse(C_A, C_B, C_TILT)


def draw_cosmic(cv, center, scale, time, seed):
    r = C_SPHERE * max(scale, 0.02)
    if r < 2:
        return
    flick = int(time * 12.0)
    distortion = math.sin(time) * 0.3
    breathe = 1 + 0.03 * distortion
    rr = r * breathe

    # 0. aura oscura
    cv.quad(GLOW, center, (7.0 * rr, 7.0 * rr), 0, (14, 2, 18, 170))

    # 1. nebulosas + polvo
    for i in range(4):
        h = hash01(seed, 501 + i, 17)
        dirn = 1.0 if i % 2 == 0 else -1.0
        ang = h * math.tau + time * 0.05 * dirn
        dist = (2.8 + 0.8 * hash01(seed, 502 + i, 29)) * rr
        pos = (center[0] + math.cos(ang) * dist,
               center[1] + math.sin(ang) * dist * 0.8)
        size = (2.0 + 1.0 * hash01(seed, 503 + i, 41)) * rr
        c = C_NEBV if i % 2 == 0 else C_NEBB
        pulse = 0.75 + 0.25 * math.sin(time * 0.7 + i * 1.9)
        cv.quad_add(GLOW, pos, (size, size), ang,
                    xna_mul(c, (0.20 if i % 2 == 0 else 0.14) * pulse))
    for i in range(12):
        h = hash01(seed, 600 + i, 13)
        ang = h * math.tau + time * 0.03 * (1.0 if i % 2 == 0 else -1.0)
        dist = (2.2 + 3.1 * hash01(seed, 601 + i, 19)) * rr * 0.55
        pos = (center[0] + math.cos(ang) * dist, center[1] + math.sin(ang) * dist)
        pulse = 0.5 + 0.5 * math.sin(time * 1.5 + i * 2.4)
        c = (190, 25, 130) if h < 0.5 else (120, 30, 200)
        size = (0.10 + 0.10 * h) * rr
        cv.quad_add(GLOW, pos, (size, size), 0, xna_mul(c, 0.45 * pulse))

    # 2. ecos del anillo
    phase = time * C_BANDS * 0.20
    e1 = 0.55 + 0.45 * math.sin(phase)
    cv.ring_quad(center, 1.42 * rr, time * C_SPIN, xna_mul(C_RING_MAG, 0.16 * e1))
    e2 = 0.55 + 0.45 * math.sin(phase + math.pi)
    cv.ring_quad(center, 0.80 * rr, -time * C_SPIN * 0.7, xna_mul(C_DVIO, 0.14 * e2))
    cv.quad_add(GLOW, center, (4.4 * rr, 3.6 * rr), C_TILT,
                xna_mul(C_RING_MAG, 0.10 + 0.05 * distortion))

    # 3/5. ANILLO con las BANDAS del shader
    spin = time * C_SPIN
    for front in (False, True):
        t0 = 0.0 if front else math.pi
        span = math.pi
        bright = 1.30 if front else 0.90
        for s in range(C_SEG):
            t = t0 + span * (s + 0.5) / C_SEG
            tSpin = t + spin
            a = c_ell(center, rr, t - span / (C_SEG * 2))
            b = c_ell(center, rr, t + span / (C_SEG * 2))
            mid = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
            seg = (b[0] - a[0], b[1] - a[1])
            seglen = math.hypot(*seg)
            if seglen < 0.5:
                continue
            rot = math.atan2(seg[1], seg[0])
            glow = 0.5 + 0.5 * math.sin(tSpin * C_BANDF - time * C_BANDS)
            turb = 0.74 + 0.26 * hash01(seed, 700 + s, flick)
            inten = glow * turb * bright
            if inten > 0.82:
                c = C_HOT_W
            elif inten > 0.45:
                c = C_RING_MAG
            else:
                c = C_DVIO
            cv.capsule(mid, seglen, 0.36 * rr * (0.7 + inten), rot, xna_mul(c, 0.40 * inten))
            cv.capsule(mid, seglen, 0.12 * rr * inten, rot, xna_mul(c, 0.85 * inten))
            if inten > 0.86:
                cv.quad_add(GLOW, mid, (0.32 * rr, 0.32 * rr), rot,
                            xna_mul(C_HOT_W, 0.70 * (inten - 0.86) / 0.14))
        if not front:
            # 4. núcleo entre mitades
            cv.quad(DISK, center, (2.28 * r, 2.28 * r), 0, (255, 255, 255, 255))
            cv.ring_quad(center, 1.02 * r, time * 0.15,
                         xna_mul(C_RING_MAG, 0.40 + 0.12 * math.sin(time * 1.7)))

    # 6. corredores de fotones
    for i in range(3):
        t = time * (C_SPIN + 0.55 + 0.20 * i) + i * 2.1
        pos = c_ell(center, rr, t)
        tw = 0.65 + 0.35 * math.sin(time * 8 + i * 2.3)
        behind = c_ell(center, rr, t - 0.22)
        seg = (pos[0] - behind[0], pos[1] - behind[1])
        ln = math.hypot(*seg)
        if ln > 0.5:
            rot = math.atan2(seg[1], seg[0])
            bmid = ((pos[0] + behind[0]) / 2, (pos[1] + behind[1]) / 2)
            cv.capsule(bmid, ln, 0.14 * rr, rot, xna_mul(C_RING_MAG, 0.40 * tw))
        cv.quad_add(GLOW, pos, (1.05 * rr, 1.05 * rr), 0, xna_mul(C_RING_MAG, 0.35 * tw))
        cv.quad_add(GLOW, pos, (0.42 * rr, 0.42 * rr), 0, xna_mul(C_HOT_W, 0.85 * tw))

    # 7. rayos
    boltFlick = flick // 2
    white = (240, 215, 255)
    for i in range(2):
        if hash01(seed, 810 + i, boltFlick) < 0.10:
            continue
        a1 = hash01(seed, 811 + i, boltFlick) * math.tau
        a2 = a1 + math.pi * (0.5 + 0.7 * hash01(seed, 812 + i, boltFlick))
        r1 = 0.15 + 0.40 * hash01(seed, 813 + i, boltFlick)
        r2 = 0.15 + 0.40 * hash01(seed, 814 + i, boltFlick)
        s = (center[0] + math.cos(a1) * r1 * r, center[1] + math.sin(a1) * r1 * r)
        e = (center[0] + math.cos(a2) * r2 * r, center[1] + math.sin(a2) * r2 * r)
        draw_bolt(cv, s, e, seed + i * 37, boltFlick, r * 0.13,
                  xna_mul(C_BVIO, 0.75), xna_mul(white, 1.0))
    for i in range(2):
        if hash01(seed, 850 + i, boltFlick) < 0.30:
            continue
        bandPeak = -time * C_BANDS / C_BANDF + i * math.pi
        t = bandPeak + 0.5 * hash01(seed, 851 + i, boltFlick // 3)
        start = c_ell(center, r, t)
        ox, oy = start[0] - center[0], start[1] - center[1]
        n = math.hypot(ox, oy)
        if n < 0.01:
            continue
        ox, oy = ox / n, oy / n
        tang = (-oy * 0.35, ox * 0.35)
        end = (start[0] + (ox + tang[0]) * (1.35 + 0.60 * hash01(seed, 852 + i, boltFlick)) * r,
               start[1] + (oy + tang[1]) * (1.35 + 0.60 * hash01(seed, 852 + i, boltFlick)) * r)
        draw_bolt(cv, start, end, seed + 100 + i * 53, boltFlick, r * 0.10,
                  xna_mul(C_BVIO, 0.55), xna_mul(C_BPINK, 0.95))

    # 8. destellos polares
    pole = (-math.sin(C_TILT + math.pi / 2), math.cos(C_TILT + math.pi / 2))
    pulse = 0.7 + 0.3 * math.sin(time * 1.9)
    for side in (0, 1):
        dirv = pole if side == 0 else (-pole[0], -pole[1])
        rot = math.atan2(dirv[1], dirv[0])
        baseOff = C_B * rr * 0.85
        for k in range(3):
            f0, f1 = k / 3, (k + 1) / 3
            midF = (f0 + f1) / 2
            a = (center[0] + dirv[0] * (baseOff + f0 * 1.9 * rr),
                 center[1] + dirv[1] * (baseOff + f0 * 1.9 * rr))
            b = (center[0] + dirv[0] * (baseOff + f1 * 1.9 * rr),
                 center[1] + dirv[1] * (baseOff + f1 * 1.9 * rr))
            mid = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
            ln = math.hypot(b[0] - a[0], b[1] - a[1])
            w = (0.30 - 0.22 * midF) * rr
            c = lerp_c(C_HOT_W, C_RING_MAG, midF)
            cv.capsule(mid, ln, w, rot, xna_mul(c, 0.30 * pulse * (1 - midF * 0.5)))
        tip = (center[0] + dirv[0] * (baseOff + 1.95 * rr),
               center[1] + dirv[1] * (baseOff + 1.95 * rr))
        cv.quad_add(GLOW, tip, (0.5 * rr, 0.5 * rr), 0, xna_mul(C_HOT_W, 0.42 * pulse))

    # 9. runas doradas
    gs = max(r / 52.0, 0.25) * 1.35
    cv.ring_quad(center, C_RUNE_R * r, time * C_RUNE_ORB, xna_mul(C_RGOLD, 0.22))
    for g in range(C_RUNES_N):
        ang = g / C_RUNES_N * math.tau + time * C_RUNE_ORB
        floatR = C_RUNE_R * r + 2.4 * gs * math.sin(time * 1.35 + g * 0.9)
        bobY = 2.0 * gs * math.sin(time * 0.85 + g * 1.7)
        gp = (center[0] + math.cos(ang) * floatR,
              center[1] + math.sin(ang) * floatR + bobY)
        pulse = 0.75 + 0.25 * math.sin(time * 2.4 + g * 1.3)
        cv.quad_add(GLOW, gp, (36 * gs, 36 * gs), 0, xna_mul(C_RGOLD, 0.20 * pulse))
        strokes = C_RUNES[g % len(C_RUNES)]
        for s in range(0, len(strokes), 2):
            a = (gp[0] + strokes[s][0] * gs, gp[1] + strokes[s][1] * gs)
            b = (gp[0] + strokes[s + 1][0] * gs, gp[1] + strokes[s + 1][1] * gs)
            mid = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
            dl = math.hypot(b[0] - a[0], b[1] - a[1])
            if dl < 0.01:
                continue
            rot = math.atan2(b[1] - a[1], b[0] - a[0])
            localY = ((strokes[s][1] + strokes[s + 1][1]) * 0.5 + 7) / 14
            col = lerp_c(C_RTIP, C_RGOLD, 1 - localY * 0.25)
            cv.capsule(mid, dl, 3.8 * gs, rot, xna_mul(col, 0.85 * pulse))
        pp = (gp[0], gp[1] - 11.5 * gs)
        ppulse = 0.8 + 0.2 * math.sin(time * 3.0 + g * 2.0)
        cv.quad_add(GLOW, pp, (7.0 * gs, 7.0 * gs), 0, xna_mul(C_RGOLD, 0.62 * pulse))
        cv.quad_add(GLOW, pp, (3.2 * gs, 3.2 * gs), 0,
                    xna_mul((255, 240, 200), 0.9 * ppulse))

    # 10. ondas
    for w in range(C_WAVE_N):
        ph = ((time / C_WAVE_CYCLE) + w / C_WAVE_N) % 1.0
        radius = (1.15 + ph * 1.95) * r
        fade = (1 - ph) ** 2
        cv.ring_quad(center, radius, ph * 2.4 + w,
                     xna_mul((255, 205, 245), 0.30 * fade))

    # 11. motas
    for i in range(9):
        h = hash01(seed, 900 + i, 23)
        life = (time * 0.13 + h) % 1.0
        ang = hash01(seed, 901 + i, 31) * math.tau + time * 0.06 * (1.0 if i % 2 == 0 else -1.0)
        dist = (1.7 + life * 2.7) * r
        pos = (center[0] + math.cos(ang) * dist, center[1] + math.sin(ang) * dist)
        size = (0.14 + 0.13 * h) * r * 2
        alpha = math.sin(life * math.pi) * (0.55 + 0.45 * h)
        c = (C_RING_MAG if h < 0.40 else C_BVIO if h < 0.72 else
             C_RGOLD if h < 0.90 else (255, 250, 252))
        cv.quad_add(GLOW, pos, (size, size), 0, xna_mul(c, alpha))

    # 12. aura final
    aura = 0.85 + 0.15 * math.sin(time * 2.2)
    cv.quad_add(GLOW, center, (5.8 * rr, 5.8 * rr), 0, xna_mul(C_AURA, 0.22 * aura))


# =====================================================================
#  2. UMBRAL — UmbralBlackHoleRenderer v6.16 (LA REFERENCIA)
# =====================================================================

U_SPHERE = 46.0
U_A, U_B, U_TILT = 2.60, 1.40, -0.10   # disco: línea delantera a ~1.4R
U_WING_R, U_WING_SQ = 2.45, 0.92       # el ala: banda circular del frente
U_TOP_ARC, U_TOP_IN, U_BOT_ARC = 1.45, 1.22, 1.28  # arcos de lente (doble arriba)
U_DOP = 3.05
U_WEDGE_A, U_WEDGE_H = 4.45, 0.55
U_FLOW = 0.42
U_STREAKS = 26
U_FLICK = 4.0
U_VOLUMES = 9
U_SPIKE_A, U_SPIKE_L = -0.55, 1.45
U_BOLT_A = 0.55
U_RUNES_N, U_RUNE_R, U_RUNE_ORB, U_CSEG = 12, 2.55, 0.06, 30
U_WAVE_CYCLE, U_WAVE_N = 2.8, 2

U_HOT_IN = (250, 210, 220)
U_HOT_ROSE = (243, 128, 149)
U_ARC_SALMON = (248, 110, 95)
U_WING_MAG = (240, 41, 168)
U_MID_ROSE = (225, 74, 127)
U_ROSE = (183, 29, 83)
U_DROSE = (153, 14, 76)
U_WINE = (110, 17, 51)
U_MAGVIO = (130, 25, 145)
U_SPIKE_W = (250, 200, 225)
U_BOLT_OR = (255, 100, 0)
U_RGOLD = (240, 124, 65)
U_RTIP = (255, 205, 140)
U_FIL = (150, 100, 255)
U_WPINK = (200, 20, 120)
U_WCRIM = (160, 10, 50)
U_EY = (255, 180, 50)
U_ER = (200, 40, 40)
U_EW = (255, 255, 240)

U_RUNES = [
    [(-4, 5.5), (-4, -5.5), (-4, -5.5), (-2.5, -1), (-2.5, -1), (-1, -6.5), (-1, -6.5), (1, -1), (1, -1), (2.5, -6.5), (2.5, -6.5), (4, -1), (4, -1), (4, 5.5), (-4, 5.5), (4, 5.5)],
    [(-3, -6.5), (-3, 4), (-3, 4), (0, 7), (0, 7), (3, 4), (3, 4), (3, -6.5), (-3, -6.5), (3, -6.5)],
    [(-3.5, -3.5), (3.5, -3.5), (3.5, -3.5), (3.5, 1.5), (3.5, 1.5), (-3.5, 1.5), (-3.5, 1.5), (-3.5, -3.5), (-3.5, 1.5), (3.5, 6.5)],
    [(-4, 0), (0, -3), (0, -3), (4, 0), (4, 0), (0, 3), (0, 3), (-4, 0), (-2, -1.2), (2, -1.2)],
    [(2, -6.5), (-2, -6.5), (-2, -6.5), (-3.5, 0), (-3.5, 0), (-2, 6.5), (-2, 6.5), (2, 6.5), (2, 6.5), (0.5, 0), (0.5, 0), (2, -6.5)],
    [(-3.5, -6), (3.5, -6), (3.5, -6), (0, 6.5), (-1.5, 0), (1.5, 0)],
    [(0, -7), (0, 7), (-3, -2), (0, -5), (3, -2), (0, -5), (-2, 4.5), (2, 4.5)],
    [(0, -6.5), (0, 0), (0, 0), (-3.5, -2.5), (0, 0), (3.5, -2.5), (0, 0), (0, 6.5), (-2.5, 3.5), (2.5, 3.5)],
    [(-3.5, 7), (-3.5, -2), (-3.5, -2), (-1.5, -6.5), (-1.5, -6.5), (0, -1), (0, -1), (1.5, -6.5), (1.5, -6.5), (3.5, -2), (3.5, -2), (3.5, 7)],
    [(0, -6.5), (-4, 3.5), (-4, 3.5), (4, 3.5), (4, 3.5), (0, -6.5), (-2.5, 6.5), (2.5, 6.5)],
    [(-3.5, 7), (-3.5, -7), (-3.5, -7), (3.5, -7), (3.5, -7), (3.5, 7), (-3.5, 7), (3.5, 7), (-3.5, -3), (3.5, -3)],
    [(-2.5, 7), (-2.5, -3), (-2.5, -3), (0, -6.5), (0, -6.5), (2.5, -3), (2.5, -3), (2.5, 7), (-2.5, 7), (2.5, 7), (-1.2, 1), (1.2, 1)],
]

u_ell = mk_ellipse(U_A, U_B, U_TILT)


def u_ell_wing(c, r, t):
    # banda circular achatada del ala
    rad = U_WING_R * r
    return (c[0] + math.cos(t) * rad,
            c[1] + math.sin(t) * rad * U_WING_SQ)


def doppler(t):
    d = 0.5 + 0.5 * math.cos(t - U_DOP)
    return d * d * d


def wedge(t):
    dd = abs((t - U_WEDGE_A + math.pi) % math.tau - math.pi)
    k = min(max(1.0 - dd / U_WEDGE_H, 0.0), 1.0)
    return 1.0 - 0.85 * k * k


def draw_umbral(cv, center, scale, time, seed):
    r = U_SPHERE * max(scale, 0.02)
    if r < 2:
        return
    flick = int(time * U_FLICK)
    breathe = 1 + 0.012 * math.sin(time * 1.1)
    rr = r * breathe

    # 0. aura oscura
    cv.quad(GLOW, center, (7.4 * rr, 7.4 * rr), 0, (16, 2, 6, 175))

    # 1. polvo de fondo (denso cerca)
    for i in range(16):
        h = hash01(seed, 600 + i, 13)
        ang = h * math.tau + time * 0.025 * (1.0 if i % 2 == 0 else -1.0)
        dist = (1.6 + 3.4 * h * h) * rr * 0.62
        pos = (center[0] + math.cos(ang) * dist, center[1] + math.sin(ang) * dist)
        pulse = 0.5 + 0.5 * math.sin(time * 1.2 + i * 2.1)
        c = (120, 20, 20) if h < 0.55 else (180, 80, 20)
        size = (0.09 + 0.09 * h) * rr
        cv.quad_add(GLOW, pos, (size, size), 0, xna_mul(c, 0.50 * pulse))

    # 2. brumas/velos
    for w in (0, 1):
        baseAng = -0.65 if w == 0 else math.pi + 0.55
        dist = 2.35 * rr
        anchor = (center[0] + math.cos(baseAng) * dist,
                  center[1] + math.sin(baseAng) * dist * 0.8)
        cBase = U_WPINK if w == 0 else U_WCRIM
        for k in range(6):
            h1 = hash01(seed, 520 + w * 40 + k, 11)
            h2 = hash01(seed, 521 + w * 40 + k, 19)
            h3 = hash01(seed, 522 + w * 40 + k, 23)
            swirl = time * 0.10 * (1.0 if w == 0 else -1.0) + h1 * math.tau
            offR = (0.4 + 0.75 * h2) * rr
            pos = (anchor[0] + math.cos(swirl) * offR,
                   anchor[1] + math.sin(swirl) * offR * 0.7)
            size = (1.1 + 0.9 * h3) * rr
            alpha = 0.09 + 0.09 * h2
            breathe = 0.8 + 0.2 * math.sin(time * 0.6 + k * 1.8)
            cv.quad_add(GLOW, pos, (size, size * (0.55 + 0.35 * h1)), swirl,
                        xna_mul(cBase, alpha * breathe))

    # 3. EL ARCO DE LENTE SUPERIOR (banda gruesa + filo) — 200°..340° (y-abajo)
    a0, a1 = math.pi + 0.35, math.tau - 0.35
    for s in range(22):
        t = a0 + (s + 0.5) / 22 * (a1 - a0)
        dop = doppler(t)
        dirv = (math.cos(t), math.sin(t))
        pos = (center[0] + dirv[0] * U_TOP_ARC * r, center[1] + dirv[1] * U_TOP_ARC * r)
        tang = (-dirv[1], dirv[0])
        rot = math.atan2(tang[1], tang[0])
        ln = (a1 - a0) * U_TOP_ARC * r / 22 * 1.35
        cGlow = lerp_c(U_ARC_SALMON, U_HOT_ROSE, dop)
        pulse = 0.80 + 0.20 * math.sin(time * 2.0 + s * 0.7)
        cv.capsule(pos, ln, (0.30 + 0.20 * dop) * r, rot,
                   xna_mul(cGlow, (0.42 + 0.26 * dop) * pulse))
        cEdge = lerp_c(U_HOT_ROSE, U_HOT_IN, dop)
        cv.capsule(pos, ln, (0.055 + 0.05 * dop) * r, rot,
                   xna_mul(cEdge, (0.80 + 0.20 * dop) * pulse))
        pos2 = (center[0] + dirv[0] * U_TOP_IN * r, center[1] + dirv[1] * U_TOP_IN * r)
        cv.capsule(pos2, ln * 0.9, (0.05 + 0.04 * dop) * r, rot,
                   xna_mul(lerp_c(U_ARC_SALMON, U_HOT_ROSE, dop),
                           (0.30 + 0.30 * dop) * pulse))

    # 3b/5. DISCO FINO (estrías sobre la elipse delgada)
    flow = time * U_FLOW
    for front in (False, True):
        t0 = 0.0 if front else math.pi
        span = math.pi
        bright = 1.25 if front else 0.42
        for s in range(U_STREAKS):
            h0 = hash01(seed, 700 + s, flick // 2)
            a0 = t0 + ((h0 + flow / math.tau) % 1.0) * span
            radialJit = 0.96 + 0.12 * hash01(seed, 703 + s, 5)
            midT = a0 + 0.10
            dop = doppler(midT)
            arcLen = (0.22 + 0.50 * hash01(seed, 701 + s, flick // 2)) * (0.45 + 0.75 * dop)
            wBase = (0.11 + 0.17 * dop) * rr
            turb = 0.70 + 0.30 * hash01(seed, 702 + s, flick)
            wg = wedge(a0 + arcLen * 0.5)
            # banda interna (3 segmentos)
            for seg in range(3):
                ta = a0 + arcLen * seg / 3
                tb = a0 + arcLen * (seg + 1) / 3
                pa = u_ell(center, rr * radialJit, ta)
                pb = u_ell(center, rr * radialJit, tb)
                mid = ((pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2)
                d = (pb[0] - pa[0], pb[1] - pa[1])
                ln = math.hypot(*d)
                if ln < 0.5:
                    continue
                rot = math.atan2(d[1], d[0])
                inten = (0.25 + 0.75 * dop) * turb * bright * wg
                if dop > 0.62:
                    core = U_HOT_IN
                elif dop > 0.30:
                    core = U_HOT_ROSE
                elif dop > 0.10:
                    core = U_ROSE
                else:
                    core = U_DROSE
                cv.capsule(mid, ln, wBase * 2.1, rot,
                           xna_mul(lerp_c(core, U_MID_ROSE, 0.25), 0.55 * inten))
                cv.capsule(mid, ln, wBase, rot, xna_mul(core, 1.0 * inten))
                if turb > 0.82 and inten > 0.28:
                    cv.quad_add(GLOW, mid, (0.36 * rr, 0.36 * rr), rot,
                                xna_mul(U_HOT_IN, 0.95 * inten * (turb - 0.82) / 0.18))
            # banda media
            tm = a0 + arcLen * 0.5
            pm = u_ell(center, rr * 1.10, tm)
            pn = u_ell(center, rr * 1.10, tm + arcLen * 0.55)
            mid = ((pm[0] + pn[0]) / 2, (pm[1] + pn[1]) / 2)
            d = (pn[0] - pm[0], pn[1] - pm[1])
            ln = math.hypot(*d)
            if ln > 0.5:
                rot = math.atan2(d[1], d[0])
                inten = (0.30 + 0.55 * dop) * turb * bright * wg
                cv.capsule(mid, ln, wBase * 1.5, rot, xna_mul(U_MID_ROSE, 0.48 * inten))
            # banda externa
            tm = a0 + arcLen * 0.4
            pm = u_ell(center, rr * 1.22, tm)
            pn = u_ell(center, rr * 1.22, tm + arcLen * 0.45)
            mid = ((pm[0] + pn[0]) / 2, (pm[1] + pn[1]) / 2)
            d = (pn[0] - pm[0], pn[1] - pm[1])
            ln = math.hypot(*d)
            if ln > 0.5:
                rot = math.atan2(d[1], d[0])
                inten = (0.22 + 0.40 * dop) * turb * bright * wg
                cv.capsule(mid, ln, wBase * 1.2, rot, xna_mul(U_WINE, 0.40 * inten))
        if not front:
            # 4. núcleo + filamentos (los filamentos van al FINAL, sobre el
            # último repintado del vacío)
            cv.quad(DISK, center, (2.28 * r, 2.28 * r), 0, (255, 255, 255, 255))
            cv.ring_quad(center, 1.02 * r, time * 0.12,
                         xna_mul(U_MAGVIO, 0.34 + 0.10 * math.sin(time * 1.5)))

    # 5b. EL ALA BARRIDA (banda circular gorda del frente) — 34°..189°
    wA0, wA1 = 0.60, math.pi + 0.05
    for s in range(18):
        t = wA0 + (s + 0.5) / 18 * (wA1 - wA0)
        dop = doppler(t)
        wg = wedge(t)
        pos = u_ell_wing(center, rr, t)
        tang = (-math.sin(t), math.cos(t) * U_WING_SQ)
        rot = math.atan2(tang[1], tang[0])
        ln = (wA1 - wA0) * U_WING_R * rr / 18 * 1.35
        w = (0.52 + 0.34 * dop) * rr
        c = U_HOT_ROSE if dop > 0.55 else U_WING_MAG
        turb = 0.70 + 0.30 * hash01(seed, 752 + s, flick)
        a = (0.42 + 0.50 * dop) * turb * wg
        cv.capsule(pos, ln, w * 1.9, rot, xna_mul(c, a))
        cv.capsule(pos, ln, w * 0.9, rot, xna_mul(c, a * 0.8))
        if dop > 0.60:
            cw = lerp_c(U_HOT_ROSE, U_HOT_IN, (dop - 0.60) / 0.40)
            cv.capsule(pos, ln, w * 0.45, rot, xna_mul(cw, 0.55 * dop * turb))

    # 5b'. EL VACÍO VUELVE A DEVORAR (repintar el disco negro tras el ala)
    cv.quad(DISK, center, (2.28 * r, 2.28 * r), 0, (255, 255, 255, 255))
    cv.ring_quad(center, 1.02 * r, time * 0.12,
                 xna_mul(U_MAGVIO, 0.30 + 0.10 * math.sin(time * 1.5)))

    # 5c. EL ARCO INFERIOR magenta — 37°..160° (bajo la panza)
    a0b, a1b = 0.65, math.pi - 0.35
    for s in range(12):
        t = a0b + (s + 0.5) / 12 * (a1b - a0b)
        dop = doppler(t)
        dirv = (math.cos(t), math.sin(t))
        pos = (center[0] + dirv[0] * U_BOT_ARC * r, center[1] + dirv[1] * U_BOT_ARC * r)
        tang = (-dirv[1], dirv[0])
        rot = math.atan2(tang[1], tang[0])
        ln = (a1b - a0b) * U_BOT_ARC * r / 12 * 1.15
        c = lerp_c(U_WING_MAG, U_MID_ROSE, dop * 0.7)
        pulse = 0.80 + 0.20 * math.sin(time * 1.7 + s * 0.9)
        cv.capsule(pos, ln, (0.09 + 0.10 * dop) * r, rot,
                   xna_mul(c, (0.44 + 0.36 * dop) * pulse))

    # 6. LA PÚA de energía
    basePos = u_ell(center, rr, U_SPIKE_A)
    ox, oy = basePos[0] - center[0], basePos[1] - center[1]
    n = math.hypot(ox, oy)
    ox, oy = ox / n, oy / n
    dv = (ox, oy - 0.34)
    nn = math.hypot(*dv)
    dirv = (dv[0] / nn, dv[1] / nn)
    pulse = 0.80 + 0.20 * math.sin(time * 2.6)
    ln = U_SPIKE_L * rr * pulse
    cv.quad_add(GLOW, basePos, (1.5 * rr, 1.5 * rr), 0, xna_mul(U_SPIKE_W, 0.45 * pulse))
    cv.quad_add(GLOW, basePos, (0.7 * rr, 0.7 * rr), 0, xna_mul((255, 250, 245), 0.85 * pulse))
    rot = math.atan2(dirv[1], dirv[0])
    for k in range(4):
        f0, f1 = k / 4, (k + 1) / 4
        midF = (f0 + f1) / 2
        a = (basePos[0] + dirv[0] * f0 * ln, basePos[1] + dirv[1] * f0 * ln)
        b = (basePos[0] + dirv[0] * f1 * ln, basePos[1] + dirv[1] * f1 * ln)
        mid = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
        segLen = math.hypot(b[0] - a[0], b[1] - a[1])
        w = (0.34 - 0.26 * midF) * rr
        fade = (1 - midF * 0.65) * pulse
        c = lerp_c(U_SPIKE_W, U_MID_ROSE, midF * 0.7)
        cv.capsule(mid, segLen, w * 2.0, rot, xna_mul(c, 0.22 * fade))
        cv.capsule(mid, segLen, w, rot, xna_mul(c, 0.55 * fade))
    tip = (basePos[0] + dirv[0] * ln, basePos[1] + dirv[1] * ln)
    cv.quad_add(GLOW, tip, (0.75 * rr, 0.75 * rr), 0, xna_mul(U_SPIKE_W, 0.40 * pulse))
    cv.quad_add(GLOW, tip, (2.1 * rr, 0.16 * rr), rot, xna_mul(U_SPIKE_W, 0.35 * pulse))
    cv.quad_add(GLOW, tip, (0.16 * rr, 1.3 * rr), rot, xna_mul(U_SPIKE_W, 0.35 * pulse))

    # 7. rayo naranja
    if hash01(seed, 890, flick) >= 0.35:
        start = u_ell(center, r, U_BOLT_A)
        ox, oy = start[0] - center[0], start[1] - center[1]
        n = math.hypot(ox, oy)
        ox, oy = ox / n, oy / n
        dv = (ox + 0.18, oy + 0.55)
        nn = math.hypot(*dv)
        dirv = (dv[0] / nn, dv[1] / nn)
        ln = (1.45 + 0.65 * hash01(seed, 891, flick)) * r
        end = (start[0] + dirv[0] * ln, start[1] + dirv[1] * ln)
        draw_bolt(cv, start, end, seed + 300, flick, r * 0.11,
                  xna_mul((200, 55, 0), 0.55), xna_mul(U_BOLT_OR, 0.95))
        cv.quad_add(GLOW, start, (0.6 * r, 0.6 * r), 0, xna_mul((255, 220, 170), 0.55))

    # 8. círculo de runas CON HUECOS
    gs = max(r / 52.0, 0.25) * 1.75
    circleR = U_RUNE_R * r
    orbit = time * U_RUNE_ORB
    for s in range(U_CSEG):
        h = hash01(seed, 940 + s, 3)
        if h < 0.30:
            continue
        ta = s / U_CSEG * math.tau + orbit
        tb = (s + 1) / U_CSEG * math.tau + orbit
        pa = (center[0] + math.cos(ta) * circleR, center[1] + math.sin(ta) * circleR)
        pb = (center[0] + math.cos(tb) * circleR, center[1] + math.sin(tb) * circleR)
        mid = ((pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2)
        d = (pb[0] - pa[0], pb[1] - pa[1])
        ln = math.hypot(*d)
        if ln < 0.5:
            continue
        rot = math.atan2(d[1], d[0])
        pulse = 0.55 + 0.45 * math.sin(time * 1.6 + s * 1.1)
        fade = 0.35 + 0.65 * h
        cv.capsule(mid, ln, 1.7 * gs, rot, xna_mul(U_RGOLD, 0.42 * pulse * fade))
    for g in range(U_RUNES_N):
        ang = g / U_RUNES_N * math.tau + orbit
        floatR = circleR + 2.2 * gs * math.sin(time * 1.2 + g * 0.9)
        bobY = 1.8 * gs * math.sin(time * 0.8 + g * 1.7)
        gp = (center[0] + math.cos(ang) * floatR,
              center[1] + math.sin(ang) * floatR + bobY)
        gapRoll = hash01(seed, 960 + g, 5)
        # cerca de la púa (derecha-arriba)
        spikeScreen = math.atan2(-1, 1.6)
        nearSpike = abs((ang - spikeScreen + math.pi) % math.tau - math.pi) < 0.50
        presence = 0.0 if gapRoll < 0.18 else (0.25 if nearSpike else 1.0)
        if presence <= 0:
            continue
        pulse = (0.70 + 0.30 * math.sin(time * 2.2 + g * 1.3)) * presence
        cv.quad_add(GLOW, gp, (30 * gs, 30 * gs), 0, xna_mul(U_RGOLD, 0.18 * pulse))
        strokes = U_RUNES[g % len(U_RUNES)]
        for s in range(0, len(strokes), 2):
            a = (gp[0] + strokes[s][0] * gs, gp[1] + strokes[s][1] * gs)
            b = (gp[0] + strokes[s + 1][0] * gs, gp[1] + strokes[s + 1][1] * gs)
            mid = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
            dl = math.hypot(b[0] - a[0], b[1] - a[1])
            if dl < 0.01:
                continue
            rot = math.atan2(b[1] - a[1], b[0] - a[0])
            localY = ((strokes[s][1] + strokes[s + 1][1]) * 0.5 + 7) / 14
            col = lerp_c(U_RTIP, U_RGOLD, 1 - localY * 0.25)
            cv.capsule(mid, dl, 3.2 * gs, rot, xna_mul(col, 0.95 * pulse))
        pp = (gp[0], gp[1] - 11.0 * gs)
        cv.quad_add(GLOW, pp, (5.6 * gs, 5.6 * gs), 0, xna_mul(U_RGOLD, 0.50 * pulse))
        cv.quad_add(GLOW, pp, (2.6 * gs, 2.6 * gs), 0,
                    xna_mul((255, 235, 195), 0.85 * pulse))

    # 9. brasas con estelas
    for i in range(12):
        h = hash01(seed, 980 + i, 23)
        life = (time * 0.16 + h) % 1.0
        ang = hash01(seed, 981 + i, 31) * math.tau + time * 0.05 * (1.0 if i % 2 == 0 else -1.0)
        inward = (i % 2 == 0)
        distNear = 2.9 - life * 1.6 if inward else 1.6 + life * 2.2
        dist = distNear * r
        pos = (center[0] + math.cos(ang) * dist, center[1] + math.sin(ang) * dist)
        size = (0.10 + 0.12 * h) * r * 2
        alpha = math.sin(life * math.pi) * (0.55 + 0.45 * h)
        c = U_EY if h < 0.45 else U_ER if h < 0.85 else U_EW
        rx, ry = pos[0] - center[0], pos[1] - center[1]
        rn = math.hypot(rx, ry)
        if rn > 1:
            dx, dy = rx / rn, ry / rn
            motion = (-dx, -dy) if inward else (dx, dy)
            tail = (pos[0] - motion[0] * (0.45 + 0.55 * h) * r,
                    pos[1] - motion[1] * (0.45 + 0.55 * h) * r)
            tmid = ((pos[0] + tail[0]) / 2, (pos[1] + tail[1]) / 2)
            tlen = math.hypot(pos[0] - tail[0], pos[1] - tail[1])
            trot = math.atan2(motion[1], motion[0])
            cv.capsule(tmid, tlen, 0.09 * r, trot, xna_mul(c, 0.40 * alpha))
        cv.quad_add(GLOW, pos, (size, size), 0, xna_mul(c, alpha))

    # 10. ondas
    for w in range(U_WAVE_N):
        ph = ((time / U_WAVE_CYCLE) + w / U_WAVE_N) % 1.0
        radius = (1.15 + ph * 1.95) * r
        fade = (1 - ph) ** 2
        cv.ring_quad(center, radius, ph * 2.4 + w,
                     xna_mul((255, 205, 210), 0.28 * fade))

    # 11. AURA FINAL en ANILLO + 12. VACÍO FINAL + filamentos
    aura = 0.85 + 0.15 * math.sin(time * 1.8)
    cv.ring_quad(center, 2.6 * rr, time * 0.1,
                 xna_mul((200, 30, 90), 0.16 * aura))
    cv.quad(DISK, center, (2.28 * r, 2.28 * r), 0, (255, 255, 255, 255))
    cv.ring_quad(center, 1.02 * r, time * 0.12,
                 xna_mul(U_MAGVIO, 0.26 + 0.08 * math.sin(time * 1.5)))
    # filamentos violeta (encima del negro absoluto)
    for i in range(2):
        if hash01(seed, 860 + i, flick) < 0.15:
            continue
        a1 = math.pi * 1.25 + 0.5 * hash01(seed, 861 + i, flick)
        a2 = a1 - math.pi * (0.35 + 0.3 * hash01(seed, 862 + i, flick))
        r1 = 0.20 + 0.45 * hash01(seed, 863 + i, flick)
        r2 = 0.25 + 0.30 * hash01(seed, 864 + i, flick)
        s = (center[0] + math.cos(a1) * r1 * r, center[1] + math.sin(a1) * r1 * r)
        e = (center[0] + math.cos(a2) * r2 * r, center[1] + math.sin(a2) * r2 * r)
        draw_bolt(cv, s, e, seed + 200 + i * 61, flick, r * 0.075,
                  xna_mul(U_FIL, 0.16), xna_mul((205, 185, 255), 0.30))


# =====================================================================
#  3. BRUMA — BrumaBlackHoleRenderer v6.17 (LA LIBRERÍA)
# =====================================================================

B_SPHERE = 48.0
B_A, B_B, B_TILT = 1.75, 1.25, -0.33
B_FLOW = 0.38
B_SMOKELETS = 16
B_TEND_N, B_TEND_S, B_TEND_SW = 3, 2.85, 2.1
B_WAVE_CYCLE, B_WAVE_N = 2.7, 2

B_TEAL = (30, 140, 160)
B_CYAN = (90, 210, 235)
B_VIO = (70, 60, 190)
B_DB = (25, 45, 140)
B_PW = (225, 250, 255)
B_RIM = (60, 200, 220)
B_FM = (185, 235, 250)

b_ell = mk_ellipse(B_A, B_B, B_TILT)


def bruma_puff(cv, center, radius, color, seed, time, alpha=0.55,
               quality=1.0, velocity=(0.0, 0.0)):
    """BrumaFX.Puff 1:1 (sobre el mock)."""
    tex = PUFFS[((seed % 8) + 8) % 8]
    if radius < 1.5:
        return
    alpha = min(max(alpha, 0.0), 1.0)
    rot = time * 0.10 * (1.0 if (seed & 1) == 0 else -1.0)
    breathe = 1 + 0.08 * math.sin(time * 0.6 + seed)
    cv.quad_add(tex, center, (radius * 2 * breathe, radius * 2 * breathe),
                rot, xna_mul(color, alpha * 0.85))
    n = int(min(max(round(radius * 0.45 * quality), 2), 14))
    aBlob = 1 - (1 - alpha * 0.5) ** (1 / (n + 1))
    trail = min(max(math.hypot(*velocity) * 0.02, 0.0), 1.0)
    for i in range(n):
        h1 = bhash(i, 1, seed)
        h2 = bhash(i, 2, seed)
        h3 = bhash(i, 3, seed)
        h4 = bhash(i, 4, seed)
        ang = h1 * math.tau + time * 0.05 * (1.0 if h2 > 0.5 else -1.0)
        ring = (0.55 + 0.35 * h2) * radius
        pos = (center[0] + math.cos(ang) * ring,
               center[1] + math.sin(ang) * ring * 0.8)
        bBreathe = 1 + 0.12 * math.sin(time * 0.8 + h4 * math.tau)
        blobR = radius * (0.28 + 0.24 * h3) * bBreathe
        brot = time * (0.2 + 0.25 * h1) * (1.0 if h2 > 0.5 else -1.0)
        vl = math.hypot(*velocity)
        dirv = (velocity[0] / vl, velocity[1] / vl) if vl > 0.01 else (0.0, 0.0)
        size = (blobR * 2 * (1 + trail * 0.8 * abs(dirv[0])),
                blobR * 2 * (1 + trail * 0.8 * abs(dirv[1])))
        cv.quad_add(GLOW, pos, size, brot, xna_mul(color, aBlob))


def draw_bruma(cv, center, scale, time, seed):
    r = B_SPHERE * max(scale, 0.02)
    if r < 2:
        return
    breathe = 1 + 0.012 * math.sin(time * 1.2)
    rr = r * breathe

    # 0. aura oscura fría
    cv.quad(GLOW, center, (7.2 * rr, 7.2 * rr), 0, (3, 10, 16, 170))

    # 1. HALO: dos Cloud de la librería
    for ci, (rad, col, s, t, n, al) in enumerate([
            (2.6 * rr, B_TEAL, seed + 11, time, 5, 0.30),
            (3.3 * rr, B_VIO, seed + 47, time * 0.8, 4, 0.20)]):
        for i in range(n):
            h1 = bhash(100 + i, 7, s)
            h2 = bhash(101 + i, 11, s)
            h3 = bhash(102 + i, 13, s)
            t2 = t * 0.30 + h1 * math.tau
            # curl (aprox 2 muestras por eje)
            cx0 = bfbm(center[0] * 0.004 + h2, center[1] * 0.004 + h3, s + i)
            cx1 = bfbm(center[0] * 0.004 + h2 + 0.15, center[1] * 0.004 + h3, s + i)
            cy0 = bfbm(center[0] * 0.004 + h2, center[1] * 0.004 + h3 + 0.15, s + i)
            cy1 = bfbm(center[0] * 0.004 + h2, center[1] * 0.004 + h3 - 0.15, s + i)
            curlx = (cy0 - cy1) * 3.33
            curly = -(cx1 - cx0) * 3.33
            pos = (center[0] + math.cos(t2) * rad * 0.42 * h2 +
                   math.sin(t * 0.31 + h1 * 6.28) * rad * 0.18 + curlx * rad * 0.10,
                   center[1] + math.sin(t2 * 0.9) * rad * 0.36 * h3 +
                   math.sin(t * 0.71 + h1 * 12.56) * rad * 0.12 + curly * rad * 0.10)
            puffR = rad * (0.42 + 0.30 * h1)
            dist = math.hypot(pos[0] - center[0], pos[1] - center[1])
            a = al * (1.25 - 0.55 * dist / max(rad, 1))
            bruma_puff(cv, pos, puffR, col, s + i * 37, t + h2 * 10,
                       min(max(a, 0.05), 0.85), 0.7)

    # 2/5. ANILLO DE HUMO (fumarelitos)
    flow = time * B_FLOW
    for front in (False, True):
        t0 = 0.0 if front else math.pi
        span = math.pi
        bright = 1.15 if front else 0.85
        for s in range(B_SMOKELETS):
            h = hash01(seed, 700 + s, 3)
            t = t0 + ((h + flow / math.tau) % 1.0) * span
            pos = b_ell(center, rr, t)
            sizeR = (0.16 + 0.22 * hash01(seed, 701 + s, 7)) * rr
            n = bfbm(pos[0] * 0.012, pos[1] * 0.012 + time * 0.10, seed + 5, 3)
            a = (0.28 + 0.42 * n) * bright
            c = B_CYAN if n > 0.55 else B_TEAL
            bruma_puff(cv, pos, sizeR, c, seed + s * 61, time,
                       min(max(a, 0.05), 0.65), 0.45)
            ahead = b_ell(center, rr, t + 0.08)
            d = (ahead[0] - pos[0], ahead[1] - pos[1])
            ln = math.hypot(*d)
            if ln > 0.5:
                rot = math.atan2(d[1], d[0])
                mid = ((pos[0] + ahead[0]) / 2, (pos[1] + ahead[1]) / 2)
                cv.capsule(mid, ln, sizeR * 0.9, rot, xna_mul(c, 0.30 * a))
        if not front:
            # 3. VOLUTAS espiralando al núcleo
            for k in range(B_TEND_N):
                baseAng = time * (0.22 + 0.08 * k) + k * 2.1
                path = []
                for p in range(8):
                    f = p / 7
                    ang = baseAng + f * B_TEND_SW
                    rad = (B_TEND_S - (B_TEND_S - 0.45) * f) * r
                    path.append((center[0] + math.cos(ang) * rad * B_A / 1.75,
                                 center[1] + math.sin(ang) * rad * B_B / 1.25))
                # Tendril 1:1 (compacto: puffs por arco)
                total = sum(math.hypot(path[i + 1][0] - path[i][0],
                                       path[i + 1][1] - path[i][1])
                            for i in range(7))
                width = 0.55 * r
                steps = int(min(max(total / (width * 0.55), 3), 26))
                c = B_TEAL if k % 2 == 0 else B_VIO
                nk = bfbm(k * 3.7, time * 0.15, seed + k, 3)
                alphaT = 0.42 + 0.20 * nk
                for st in range(steps + 1):
                    f = st / steps
                    # punto en ruta por arco
                    target = f * total
                    acum = 0.0
                    pos = path[-1]
                    for i in range(7):
                        segl = math.hypot(path[i + 1][0] - path[i][0],
                                          path[i + 1][1] - path[i][1])
                        if acum + segl >= target and segl > 0:
                            ff = (target - acum) / segl
                            pos = (path[i][0] + (path[i + 1][0] - path[i][0]) * ff,
                                   path[i][1] + (path[i + 1][1] - path[i][1]) * ff)
                            break
                        acum += segl
                    prof = math.sin(f * math.pi * 0.9 + 0.15)
                    rw = width * 0.5 * min(max(prof, 0.25), 1.0)
                    sway = (math.sin(time * 0.5 + f * 4.5 + k) * width * 0.16 +
                            math.sin(time * 1.13 + f * 9.1 + k * 2) * width * 0.07)
                    # normal local
                    tgt2 = min(f * total + width * 0.5, total)
                    tgt0 = max(f * total - width * 0.5, 0)
                    acum = 0.0
                    pa = path[-1]
                    for i in range(7):
                        segl = math.hypot(path[i + 1][0] - path[i][0],
                                          path[i + 1][1] - path[i][1])
                        if acum + segl >= tgt2 and segl > 0:
                            ff = (tgt2 - acum) / segl
                            pa = (path[i][0] + (path[i + 1][0] - path[i][0]) * ff,
                                  path[i][1] + (path[i + 1][1] - path[i][1]) * ff)
                            break
                        acum += segl
                    acum = 0.0
                    pb = path[0]
                    for i in range(7):
                        segl = math.hypot(path[i + 1][0] - path[i][0],
                                          path[i + 1][1] - path[i][1])
                        if acum + segl >= tgt0 and segl > 0:
                            ff = (tgt0 - acum) / segl
                            pb = (path[i][0] + (path[i + 1][0] - path[i][0]) * ff,
                                  path[i][1] + (path[i + 1][1] - path[i][1]) * ff)
                            break
                        acum += segl
                    seg = (pa[0] - pb[0], pa[1] - pb[1])
                    if math.hypot(*seg) < 0.01:
                        seg = (1.0, 0.0)
                    nn = math.hypot(*seg)
                    normal = (-seg[1] / nn, seg[0] / nn)
                    endFade = 1 - 1.0 * f
                    nt = bfbm(pos[0] * 0.02, pos[1] * 0.02, seed + k, 3)
                    aT = alphaT * endFade * (0.45 + 0.55 * nt)
                    bruma_puff(cv, (pos[0] + normal[0] * sway,
                                    pos[1] + normal[1] * sway),
                               rw, c, seed + k * 97 + st * 53, time,
                               min(max(aT, 0.03), 0.8), 0.45)
            # 4. núcleo
            cv.quad(DISK, center, (2.28 * r, 2.28 * r), 0, (255, 255, 255, 255))
            cv.ring_quad(center, 1.02 * r, time * 0.13,
                         xna_mul(B_RIM, 0.36 + 0.12 * math.sin(time * 1.6)))

    # 6. anillo de fotones + corredores
    cv.ring_quad(center, 1.10 * r, time * 0.2,
                 xna_mul(B_CYAN, 0.20 + 0.08 * math.sin(time * 2.1)))
    for i in range(3):
        t = time * (0.9 + 0.25 * i) + i * 2.1
        pos = b_ell(center, rr, t)
        tw = 0.65 + 0.35 * math.sin(time * 8 + i * 2.3)
        behind = b_ell(center, rr, t - 0.22)
        d = (pos[0] - behind[0], pos[1] - behind[1])
        ln = math.hypot(*d)
        if ln > 0.5:
            rot = math.atan2(d[1], d[0])
            mid = ((pos[0] + behind[0]) / 2, (pos[1] + behind[1]) / 2)
            cv.capsule(mid, ln, 0.13 * rr, rot, xna_mul(B_CYAN, 0.40 * tw))
        cv.quad_add(GLOW, pos, (0.95 * rr, 0.95 * rr), 0, xna_mul(B_CYAN, 0.35 * tw))
        cv.quad_add(GLOW, pos, (0.40 * rr, 0.40 * rr), 0, xna_mul(B_PW, 0.85 * tw))

    # 7. chimeneas polares + agujas
    pole = (-math.sin(B_TILT + math.pi / 2), math.cos(B_TILT + math.pi / 2))
    pulse = 0.7 + 0.3 * math.sin(time * 1.8)
    for side in (0, 1):
        dirv = pole if side == 0 else (-pole[0], -pole[1])
        baseOff = B_B * rr * 0.9
        for i in range(5):
            phase = (time * 0.45 + i / 5) % 1.0
            riseR = (0.10 + 0.30 * phase) * rr
            along = baseOff + phase * 1.6 * rr
            pos = (center[0] + dirv[0] * along, center[1] + dirv[1] * along)
            n = bfbm(i * 2.3, time * 0.2 + side, seed + side * 31, 3)
            a = 0.30 * math.sin(phase * math.pi) * (0.5 + 0.5 * n)
            bruma_puff(cv, pos, riseR, B_CYAN if side == 0 else B_TEAL,
                       seed + side * 53 + i * 17, time,
                       min(max(a, 0.04), 0.5), 0.4,
                       (dirv[0] * phase * rr * 0.6, dirv[1] * phase * rr * 0.6))
        # agujas
        rot = math.atan2(dirv[1], dirv[0])
        for k in range(3):
            f0, f1 = k / 3, (k + 1) / 3
            midF = (f0 + f1) / 2
            a = (center[0] + dirv[0] * (baseOff + f0 * 1.7 * rr),
                 center[1] + dirv[1] * (baseOff + f0 * 1.7 * rr))
            b = (center[0] + dirv[0] * (baseOff + f1 * 1.7 * rr),
                 center[1] + dirv[1] * (baseOff + f1 * 1.7 * rr))
            mid = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
            ln = math.hypot(b[0] - a[0], b[1] - a[1])
            w = (0.28 - 0.20 * midF) * rr
            c = lerp_c(B_PW, B_CYAN, midF)
            cv.capsule(mid, ln, w, rot, xna_mul(c, 0.28 * pulse * (1 - midF * 0.5)))
        tip = (center[0] + dirv[0] * (baseOff + 1.75 * rr),
               center[1] + dirv[1] * (baseOff + 1.75 * rr))
        cv.quad_add(GLOW, tip, (0.45 * rr, 0.45 * rr), 0, xna_mul(B_PW, 0.40 * pulse))

    # 8. escarcha
    for i in range(10):
        h = hash01(seed, 900 + i, 23)
        life = (time * 0.12 + h) % 1.0
        ang = hash01(seed, 901 + i, 31) * math.tau + time * 0.05 * (1.0 if i % 2 == 0 else -1.0)
        dist = (1.7 + life * 2.6) * r
        pos = (center[0] + math.cos(ang) * dist, center[1] + math.sin(ang) * dist)
        size = (0.12 + 0.12 * h) * r * 2
        alpha = math.sin(life * math.pi) * (0.55 + 0.45 * h)
        c = (B_CYAN if h < 0.40 else B_TEAL if h < 0.70 else
             B_VIO if h < 0.90 else B_PW)
        cv.quad_add(GLOW, pos, (size, size), 0, xna_mul(c, alpha))

    # 9. ondas
    for w in range(B_WAVE_N):
        ph = ((time / B_WAVE_CYCLE) + w / B_WAVE_N) % 1.0
        radius = (1.15 + ph * 1.95) * r
        fade = (1 - ph) ** 2
        cv.ring_quad(center, radius, ph * 2.4 + w,
                     xna_mul((200, 245, 255), 0.28 * fade))

    # 10. aura final
    aura = 0.85 + 0.15 * math.sin(time * 1.9)
    cv.quad_add(GLOW, center, (5.8 * rr, 5.8 * rr), 0, xna_mul(B_TEAL, 0.20 * aura))


# =====================================================================
#  RENDER
# =====================================================================

OUT = '/home/z/my-project/AethonMod/research/agujeros_v616'
import os
os.makedirs(OUT, exist_ok=True)

DARK = (10, 8, 14)
SKY = (110, 165, 235)

for name, fn, r in [('cosmico', draw_cosmic, 1.0),
                    ('umbral', draw_umbral, 1.0),
                    ('bruma', draw_bruma, 1.0)]:
    for tag, bg in [('dark', DARK), ('sky', SKY)]:
        cv = Canvas(460, 460, bg)
        fn(cv, (230, 230), r, 1.7, 7)
        path = f'{OUT}/{name}_{tag}.png'
        cv.save(path)
        print('OK', path)

# ESCALAS (la librería de bruma a 0.35× y 1.0×)
for sc in (0.35, 1.0):
    cv = Canvas(460, 460, DARK)
    draw_bruma(cv, (230, 230), sc, 1.7, 7)
    path = f'{OUT}/bruma_escala_{sc}.png'
    cv.save(path)
    print('OK', path)

# HOJA COMPARATIVA
sheet = Image.new('RGB', (1420, 500), (12, 10, 16))
for i, name in enumerate(['cosmico_dark', 'umbral_dark', 'bruma_dark']):
    im = Image.open(f'{OUT}/{name}.png')
    sheet.paste(im, (10 + i * 470, 40))
d = ImageDraw.Draw(sheet)
d.text((10 + 0 * 470 + 150, 12), 'CÓSMICO (script Unity)', fill=(255, 120, 220))
d.text((10 + 1 * 470 + 140, 12), 'UMBRAL (la referencia)', fill=(255, 150, 90))
d.text((10 + 2 * 470 + 150, 12), 'BRUMA (la librería)', fill=(120, 220, 240))
sheet.save(f'{OUT}/hoja_3agujeros.png')
print('OK hoja')
