#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_olvido_v615.py — SIMULACIÓN EXACTA del OlvidoBlackHoleRenderer v6.15
(100% código) sobre las texturas-pincel REALES del mod.

Modelo de blending del juego (FNA/XNA SpriteBatch):
  · Additive (SrcAlpha, One):  acc += texel.rgb · texel.a · tint.rgb · tint.a
  · AlphaBlend:                acc = texel.rgb·tint.rgb·(texel.a·tint.a)
                                       + acc·(1 − texel.a·tint.a)
  · Color * f (XNA): escala los CUATRO canales → brillo efectivo ∝ f².

Se traduce Draw() 1:1 (mismas constantes, mismo orden de capas) con
time=1.7 y seed=7 (whoAmI=0). Fondo: espacio oscuro (como la referencia)
y una variante de cielo diurno.
"""

import math
import numpy as np
from PIL import Image

BASE = '/home/z/my-project/AethonMod/AethonMod/Content/Effects/Procedural'
GLOW = np.array(Image.open(f'{BASE}/SoftGlow.png').convert('RGBA')).astype(np.float64)
RING = np.array(Image.open(f'{BASE}/Ring.png').convert('RGBA')).astype(np.float64)
DISK = np.array(Image.open(f'{BASE}/BlackDisk.png').convert('RGBA')).astype(np.float64)

# ---- constantes del renderer (1:1) ----
SPHERE = 52.0
RING_A, RING_B, RING_TILT = 1.85, 1.06, -0.42
PLASMA_FLOW = 0.55
HOTSPOT_BASE = 2.18
RING_SEGMENTS = 44
FLICK_HZ = 12.0
RUNE_COUNT = 10
RUNE_RADIUS = 2.62
RUNE_ORBIT = 0.10
WAVE_CYCLE = 2.6

HOT_CORE = (255, 244, 210)
MID_PINK = (255, 60, 160)
LOW_VIOLET = (185, 40, 215)
BOLT_VIOLET = (140, 40, 255)
BOLT_PINK = (255, 90, 190)
RUNE_GOLD = (255, 150, 40)
RUNE_TIP = (255, 225, 150)
NEB_PURPLE = (110, 30, 190)
NEB_BLUE = (50, 80, 210)
AURA_VIOLET = (120, 40, 190)

RUNES = [
    [(-2.5, 7), (-2.5, -7), (-2.5, -2), (3, -6), (-2.5, 3), (2.5, -1.5)],
    [(-4, -6), (0, 3), (4, -6), (0, 3), (-2, 5.5), (2, 5.5)],
    [(-3, 7), (-3, -7), (3, 7), (3, -7), (-3, -5), (3, -5), (-3, 2), (3, 2)],
    [(0, 7), (0, -7), (-4, 0), (4, 0), (-3, -5), (3, 5), (3, -5), (-3, 5)],
    [(-2, 7), (0.5, 1), (0.5, 1), (-2, -3), (-2, -3), (2.5, -7)],
    [(-3, 6), (-1, -6), (1, -6), (3, 6), (-1.5, -6), (1.5, -6)],
    [(-3, 6), (-3, -2), (-3, -2), (3, -6), (3, -6), (3, 2), (3, 2), (-2, 6)],
    [(0, 7), (0, -7), (-3, -2), (0, -7), (0, -2), (3, -7), (-2, 4), (2, 4)],
    [(0, 7), (0, -4), (0, -4), (-3, -7), (0, -4), (3, -7), (-2, 1), (2, 1)],
    [(-4, 0), (0, -4.5), (0, -4.5), (4, 0), (4, 0), (0, 4.5), (0, 4.5), (-4, 0), (-1.5, 0), (1.5, 0)],
]


def hash01(seed, a, b):
    h = (seed * 374761393 + a * 668265263 + b * 1911520717) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFFFF) / 16777216.0


def xna_mul(c, f):
    """Tint() del renderer v6.15: rgb PLENO + alfa = f → brillo LINEAL
    (el patrón validado del Cometa Estelar)."""
    return (float(c[0]), float(c[1]), float(c[2]), min(255.0, 255.0 * f))


def lerp_c(c1, c2, t):
    return tuple(c1[k] + (c2[k] - c1[k]) * t for k in range(3))


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
        """tint = (r, g, b, a) YA escalado (post xna_mul)."""
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


def ellipse(c, r, t):
    a, b = RING_A * r, RING_B * r
    ct, st = math.cos(t), math.sin(t)
    lx, ly = a * ct, b * st
    cr, sr = math.cos(RING_TILT), math.sin(RING_TILT)
    return (c[0] + lx * cr - ly * sr, c[1] + lx * sr + ly * cr)


def draw(cv, center, scale, time, seed):
    r = SPHERE * max(scale, 0.02)
    if r < 2:
        return
    flick = int(time * FLICK_HZ)
    breathe = 1 + 0.012 * math.sin(time * 1.3)
    rr = r * breathe

    # 0. aura oscura (ALPHA)
    cv.quad(GLOW, center, (7.0 * rr, 7.0 * rr), 0, (8, 2, 16, 170))

    # 1. nebulosas + polvo
    for i in range(5):
        h = hash01(seed, 501 + i, 17)
        dirn = 1.0 if i % 2 == 0 else -1.0
        ang = h * math.tau + time * 0.05 * dirn
        dist = (2.7 + 0.9 * hash01(seed, 502 + i, 29)) * rr
        pos = (center[0] + math.cos(ang) * dist,
               center[1] + math.sin(ang) * dist * 0.8)
        size = (2.0 + 1.1 * hash01(seed, 503 + i, 41)) * rr
        c = NEB_PURPLE if i % 2 == 0 else NEB_BLUE
        pulse = 0.75 + 0.25 * math.sin(time * 0.7 + i * 1.9)
        cv.quad_add(GLOW, pos, (size, size), ang, xna_mul(c, (0.22 if i % 2 == 0 else 0.15) * pulse))
    for i in range(14):
        h = hash01(seed, 600 + i, 13)
        ang = h * math.tau + time * 0.03 * (1.0 if i % 2 == 0 else -1.0)
        dist = (2.2 + 3.3 * hash01(seed, 601 + i, 19)) * rr * 0.55
        pos = (center[0] + math.cos(ang) * dist,
               center[1] + math.sin(ang) * dist)
        pulse = 0.5 + 0.5 * math.sin(time * 1.5 + i * 2.4)
        c = (180, 20, 60) if h < 0.5 else (140, 10, 90)
        size = (0.10 + 0.10 * h) * rr
        cv.quad_add(GLOW, pos, (size, size), 0, xna_mul(c, 0.45 * pulse))

    # 2/5. anillo de plasma
    def plasma_ring(front):
        t0 = 0.0 if front else math.pi
        span = math.pi
        bright = 1.25 if front else 0.92
        hotspot = HOTSPOT_BASE + time * PLASMA_FLOW
        for s in range(RING_SEGMENTS):
            t = t0 + span * (s + 0.5) / RING_SEGMENTS
            a = ellipse(center, rr, t - span / (RING_SEGMENTS * 2))
            b = ellipse(center, rr, t + span / (RING_SEGMENTS * 2))
            mid = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5)
            seg = (b[0] - a[0], b[1] - a[1])
            seg_len = math.hypot(*seg)
            if seg_len < 0.5:
                continue
            rot = math.atan2(seg[1], seg[0])
            hot = 0.5 + 0.5 * math.cos(t - hotspot)
            hot = 0.35 + 0.65 * hot * hot
            turb = 0.72 + 0.28 * hash01(seed, 700 + s, flick)
            inten = hot * turb * bright
            if inten > 0.85:
                c = HOT_CORE
            elif inten > 0.55:
                c = MID_PINK
            else:
                c = LOW_VIOLET
            cv.capsule(mid, seg_len, 0.34 * rr * (0.7 + inten), rot,
                       xna_mul(c, 0.40 * inten))
            cv.capsule(mid, seg_len, 0.11 * rr * inten, rot,
                       xna_mul(c, 0.80 * inten))
            if inten > 0.88:
                cv.quad_add(GLOW, mid, (0.30 * rr, 0.30 * rr), rot,
                            xna_mul(HOT_CORE, 0.65 * (inten - 0.88) / 0.12))

    plasma_ring(front=False)

    # 3. brazos espirales
    arm_phase = time * 0.18
    for arm in range(2):
        base_t = arm_phase + arm * math.pi
        for k in range(14):
            f = k / 13.0
            t = base_t + f * 1.35
            grow = 1 + f * 0.95
            pos = ellipse(center, rr * grow, t)
            nxt = ellipse(center, rr * grow, t + 1.35 / 14)
            mid = ((pos[0] + nxt[0]) * 0.5, (pos[1] + nxt[1]) * 0.5)
            seg = (nxt[0] - pos[0], nxt[1] - pos[1])
            ln = math.hypot(*seg)
            if ln < 0.5:
                continue
            rot = math.atan2(seg[1], seg[0])
            fade = (1 - f) ** 1.5
            c = lerp_c(MID_PINK, LOW_VIOLET, f)
            cv.capsule(mid, ln, (0.34 - 0.20 * f) * rr, rot, xna_mul(c, 0.40 * fade))
            cv.capsule(mid, ln, (0.11 - 0.07 * f) * rr, rot, xna_mul(c, 0.60 * fade))

    # 4. núcleo (ALPHA opaco) + filo violeta
    cv.quad(DISK, center, (2.28 * r, 2.28 * r), 0, (255, 255, 255, 255))
    rim_a = 0.38 + 0.12 * math.sin(time * 1.7)
    cv.ring_quad(center, 1.02 * r, time * 0.15, xna_mul(BOLT_VIOLET, rim_a))

    # 5. anillo delantero
    plasma_ring(front=True)

    # 6. corredores de fotones
    for i in range(3):
        t = time * (1.25 + 0.22 * i) + i * 2.1
        pos = ellipse(center, rr, t)
        twinkle = 0.65 + 0.35 * math.sin(time * 8 + i * 2.3)
        cv.quad_add(GLOW, pos, (1.05 * rr, 1.05 * rr), 0, xna_mul(MID_PINK, 0.35 * twinkle))
        cv.quad_add(GLOW, pos, (0.42 * rr, 0.42 * rr), 0, xna_mul(HOT_CORE, 0.85 * twinkle))

    # 7. rayos eléctricos
    def draw_bolt(start, end, bseed, bwidth, halo, core):
        delta = (end[0] - start[0], end[1] - start[1])
        length = math.hypot(*delta)
        if length < 4:
            return
        dirn = (delta[0] / length, delta[1] / length)
        nrm = (-dirn[1], dirn[0])
        amp = min(length * 0.16, 0.30 * bwidth * 4)
        pts = []
        for s in range(7):
            f = s / 6.0
            env = math.sin(f * math.pi)
            jit = (hash01(bseed, bolt_flick, s) - 0.5) * 2 * amp * env
            pts.append((start[0] + dirn[0] * length * f + nrm[0] * jit,
                        start[1] + dirn[1] * length * f + nrm[1] * jit))
        for s in range(6):
            a, b = pts[s], pts[s + 1]
            mid = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5)
            seg = (b[0] - a[0], b[1] - a[1])
            ln = math.hypot(*seg)
            if ln < 0.5:
                continue
            rot = math.atan2(seg[1], seg[0])
            cv.capsule(mid, ln, bwidth * 2.0, rot, halo)
            cv.capsule(mid, ln, bwidth * 0.8, rot, core)
            # RAMA lateral corta donde el hash lo pide
            if 0 < s < 5 and hash01(bseed, bolt_flick, s + 91) > 0.62:
                side = 1.0 if hash01(bseed, bolt_flick, s + 37) > 0.5 else -1.0
                branch_len = (0.35 + 0.4 * hash01(bseed, bolt_flick, s + 53)) * length * 0.25
                bdir = (dirn[0] * 0.45 + nrm[0] * side, dirn[1] * 0.45 + nrm[1] * side)
                bl = math.hypot(*bdir)
                bdir = (bdir[0] / bl, bdir[1] / bl)
                bend = (b[0] + bdir[0] * branch_len, b[1] + bdir[1] * branch_len)
                bmid = ((b[0] + bend[0]) * 0.5, (b[1] + bend[1]) * 0.5)
                brot = math.atan2(bdir[1], bdir[0])
                bh = (halo[0], halo[1], halo[2], halo[3] * 0.6)
                bc = (core[0], core[1], core[2], core[3] * 0.6)
                cv.capsule(bmid, branch_len, bwidth * 1.3, brot, bh)
                cv.capsule(bmid, branch_len, bwidth * 0.5, brot, bc)
        cv.quad_add(GLOW, start, (bwidth * 5, bwidth * 5), 0, halo)
        cv.quad_add(GLOW, start, (bwidth * 2.6, bwidth * 2.6), 0, core)
        cv.quad_add(GLOW, end, (bwidth * 4, bwidth * 4), 0, halo)

    bolt_flick = flick // 2
    bolt_core_white = (235, 200, 255)
    for i in range(4):
        if hash01(seed, 810 + i, bolt_flick) < 0.08:
            continue
        a1 = hash01(seed, 811 + i, bolt_flick) * math.tau
        a2 = a1 + math.pi * (0.5 + 0.7 * hash01(seed, 812 + i, bolt_flick))
        r1 = 0.15 + 0.40 * hash01(seed, 813 + i, bolt_flick)
        r2 = 0.15 + 0.40 * hash01(seed, 814 + i, bolt_flick)
        start = (center[0] + math.cos(a1) * r1 * r, center[1] + math.sin(a1) * r1 * r)
        end = (center[0] + math.cos(a2) * r2 * r, center[1] + math.sin(a2) * r2 * r)
        draw_bolt(start, end, seed + i * 37, r * 0.13,
                  xna_mul(BOLT_VIOLET, 0.75), xna_mul(bolt_core_white, 1.0))
    for i in range(2):
        if hash01(seed, 850 + i, bolt_flick) < 0.30:
            continue
        t = HOTSPOT_BASE + time * PLASMA_FLOW + i * math.pi + \
            0.6 * hash01(seed, 851 + i, bolt_flick // 3)
        start = ellipse(center, r, t)
        outw = (start[0] - center[0], start[1] - center[1])
        ol = math.hypot(*outw)
        if ol < 0.01:
            continue
        outw = (outw[0] / ol, outw[1] / ol)
        tang = (-outw[1] * 0.35, outw[0] * 0.35)
        k = 1.30 + 0.60 * hash01(seed, 852 + i, bolt_flick)
        end = (start[0] + (outw[0] + tang[0]) * k * r,
               start[1] + (outw[1] + tang[1]) * k * r)
        draw_bolt(start, end, seed + 100 + i * 53, r * 0.09,
                  xna_mul(BOLT_VIOLET, 0.55), xna_mul(BOLT_PINK, 0.95))

    # 8. destellos polares
    pole = (-math.sin(RING_TILT + math.pi / 2), math.cos(RING_TILT + math.pi / 2))
    pulse = 0.7 + 0.3 * math.sin(time * 1.7)
    for side in range(2):
        dirn = pole if side == 0 else (-pole[0], -pole[1])
        rot = math.atan2(dirn[1], dirn[0])
        base_off = RING_B * rr * 0.85
        for k in range(3):
            f0, f1 = k / 3.0, (k + 1) / 3.0
            mid_f = (f0 + f1) * 0.5
            a = (center[0] + dirn[0] * (base_off + f0 * 1.9 * rr),
                 center[1] + dirn[1] * (base_off + f0 * 1.9 * rr))
            b = (center[0] + dirn[0] * (base_off + f1 * 1.9 * rr),
                 center[1] + dirn[1] * (base_off + f1 * 1.9 * rr))
            mid = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5)
            ln = math.hypot(b[0] - a[0], b[1] - a[1])
            w = (0.30 - 0.22 * mid_f) * rr
            c = lerp_c(HOT_CORE, MID_PINK, mid_f)
            cv.capsule(mid, ln, w, rot, xna_mul(c, 0.30 * pulse * (1 - mid_f * 0.5)))
        tip = (center[0] + dirn[0] * (base_off + 1.95 * rr),
               center[1] + dirn[1] * (base_off + 1.95 * rr))
        cv.quad_add(GLOW, tip, (0.5 * rr, 0.5 * rr), 0, xna_mul(HOT_CORE, 0.42 * pulse))

    # 9. runas doradas
    glyph_scale = max(r / 52.0, 0.25) * 1.35
    cv.ring_quad(center, RUNE_RADIUS * r, time * RUNE_ORBIT, xna_mul(RUNE_GOLD, 0.25))
    for g in range(RUNE_COUNT):
        ang = g / RUNE_COUNT * math.tau + time * RUNE_ORBIT
        float_r = RUNE_RADIUS * r + 2.4 * glyph_scale * math.sin(time * 1.35 + g * 0.9)
        bob_y = 2.0 * glyph_scale * math.sin(time * 0.85 + g * 1.7)
        gpos = (center[0] + math.cos(ang) * float_r,
                center[1] + math.sin(ang) * float_r + bob_y)
        pulse = 0.75 + 0.25 * math.sin(time * 2.4 + g * 1.3)
        cv.quad_add(GLOW, gpos, (36 * glyph_scale, 36 * glyph_scale), 0,
                    xna_mul(RUNE_GOLD, 0.20 * pulse))
        strokes = RUNES[g % len(RUNES)]
        for s in range(0, len(strokes), 2):
            a = (gpos[0] + strokes[s][0] * glyph_scale,
                 gpos[1] + strokes[s][1] * glyph_scale)
            b = (gpos[0] + strokes[s + 1][0] * glyph_scale,
                 gpos[1] + strokes[s + 1][1] * glyph_scale)
            mid = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5)
            delta = (b[0] - a[0], b[1] - a[1])
            ln = math.hypot(*delta)
            if ln < 0.01:
                continue
            rot = math.atan2(delta[1], delta[0])
            local_y = ((strokes[s][1] + strokes[s + 1][1]) * 0.5 + 7) / 14
            col = lerp_c(RUNE_TIP, RUNE_GOLD, 1 - local_y * 0.25)
            cv.capsule(mid, ln, 3.8 * glyph_scale, rot, xna_mul(col, 0.85 * pulse))
        pearl = (gpos[0], gpos[1] - 11.5 * glyph_scale)
        pearl_pulse = 0.8 + 0.2 * math.sin(time * 3.0 + g * 2.0)
        cv.quad_add(GLOW, pearl, (7 * glyph_scale, 7 * glyph_scale), 0,
                    xna_mul(RUNE_GOLD, 0.62 * pulse))
        cv.quad_add(GLOW, pearl, (3.2 * glyph_scale, 3.2 * glyph_scale), 0,
                    xna_mul((255, 235, 190), 0.9 * pearl_pulse))

    # 10. ondas de distorsión
    for w in range(2):
        phase = ((time / WAVE_CYCLE) + w / 2.0) % 1.0
        radius = (1.15 + phase * 1.95) * r
        fade = (1 - phase) * (1 - phase)
        cv.ring_quad(center, radius, phase * 2.4 + w,
                     xna_mul((225, 200, 255), 0.30 * fade))

    # 11. partículas luminosas
    for i in range(9):
        h = hash01(seed, 900 + i, 23)
        life = (time * 0.13 + h) % 1.0
        ang = hash01(seed, 901 + i, 31) * math.tau + time * 0.06 * (1.0 if i % 2 == 0 else -1.0)
        dist = (1.7 + life * 2.7) * r
        pos = (center[0] + math.cos(ang) * dist, center[1] + math.sin(ang) * dist)
        size = (0.14 + 0.13 * h) * r * 2
        alpha = math.sin(life * math.pi) * (0.55 + 0.45 * h)
        if h < 0.40:
            c = MID_PINK
        elif h < 0.72:
            c = BOLT_VIOLET
        elif h < 0.90:
            c = RUNE_GOLD
        else:
            c = (255, 245, 235)
        cv.quad_add(GLOW, pos, (size, size), 0, xna_mul(c, alpha))

    # 12. aura mística
    aura = 0.85 + 0.15 * math.sin(time * 2.0)
    cv.quad_add(GLOW, center, (5.6 * rr, 5.6 * rr), 0, xna_mul(AURA_VIOLET, 0.22 * aura))


def main():
    W = H = 760
    center = (W / 2, H / 2)

    # fondo espacial: negro con estrellas tenues (como la referencia)
    rng = np.random.default_rng(42)
    space = np.zeros((H, W, 3), dtype=np.uint8)
    star_n = 220
    ys = rng.integers(0, H, star_n)
    xs = rng.integers(0, W, star_n)
    br = rng.integers(30, 110, star_n)
    space[ys, xs] = br[:, None]

    cv1 = Canvas(W, H, space)
    draw(cv1, center, 1.0, 1.7, 7)
    cv1.save('/tmp/olvido_v615_dark.png')

    # variante: otro instante (turbulencia diferente)
    cv2 = Canvas(W, H, space)
    draw(cv2, center, 1.0, 2.93, 7)
    cv2.save('/tmp/olvido_v615_dark2.png')

    # cielo diurno (legibilidad)
    sky = np.zeros((H, W, 3), dtype=np.uint8)
    sky[:] = (135, 185, 235)
    cv3 = Canvas(W, H, sky)
    draw(cv3, center, 1.0, 1.7, 7)
    cv3.save('/tmp/olvido_v615_day.png')

    print('mocks guardados: /tmp/olvido_v615_dark.png, dark2, day')


if __name__ == '__main__':
    main()
