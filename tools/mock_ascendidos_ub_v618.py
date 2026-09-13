#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_ascendidos_ub_v618.py — Task D2 — SANITY CHECK VISUAL (compacto).

Traduce a Python la LIBRERÍA LightningCore.cs (FlickTick/Flicker,
BoltPoints/ArcPoints, Bolt con doble tira + RAMAS + gorros, Arc) y las
mejoras eléctricas de los ASCENDIDOS de Umbral y Bruma para verificar
de un vistazo (sobre las texturas-pincel REALES del mod y el blending
aditivo del juego):

  UMBRAL ASCENDIDO:
    · la LLUVIA DE RAYOS ancla al círculo de runas y CAE afuera-abajo;
    · el ARCO DORADO gira a ~1.38·R alrededor del horizonte (doble filo).

  BRUMA ASCENDIDA:
    · las CORONAS DE ESCARCHA chispean a 3 radios distintos (~10 Hz);
    · los RAYOS GELIDOS escapan del anillo de humo;
    · los CRISTALES DE HIELO flotan con destello angular.

Solo las capas eléctricas nuevas sobre una esfera negra simple (el
renderer completo vive en C#). time=1.7, seed=7 (la convención del mod).
"""

import math
import os
import numpy as np
from PIL import Image

BASE = '/home/z/my-project/AethonMod/AethonMod/Content/Effects/Procedural'
GLOW = np.array(Image.open(f'{BASE}/SoftGlow.png').convert('RGBA')).astype(np.float64)
OUT = '/home/z/my-project/AethonMod/research/ascendidos_v618'
os.makedirs(OUT, exist_ok=True)


# =====================================================================
#  LA MATEMÁTICA DE LightningCore.cs (traducción 1:1)
# =====================================================================

def vhash(seed, a, b):
    h = (seed * 374761393 + a * 668265263 + b * 1911520717) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFFFF) / 16777216.0


def flick_tick(time, hz):
    return int(time * hz)


def flicker(seed, flick, alive=0.85):
    return vhash(seed, 9871, flick) < alive


def bolt_points(start, end, seed, flick, segments=7, amp=14.0):
    pts = []
    dx, dy = end[0] - start[0], end[1] - start[1]
    ln = math.hypot(dx, dy)
    if ln < 0.001:
        return [tuple(start)] * (segments + 1)
    ux, uy = dx / ln, dy / ln
    nx, ny = -uy, ux
    amp_len = min(amp, ln * 0.22)
    for i in range(segments + 1):
        t = i / segments
        env = math.sin(t * math.pi)
        jit = (vhash(seed, flick, i * 31 + 7) - 0.5) * 2.0 * amp_len * env
        pts.append((start[0] + ux * (ln * t) + nx * jit,
                    start[1] + uy * (ln * t) + ny * jit))
    pts[0] = tuple(start)
    pts[segments] = tuple(end)
    return pts


def arc_points(center, radius, a1, a2, seed, flick, count=9, amp=0.16):
    pts = []
    for i in range(count + 1):
        t = i / count
        ang = a1 + (a2 - a1) * t
        r = radius * (1.0 + (vhash(seed, flick, i * 17 + 3) - 0.5) * 2.0
                      * amp * math.sin(t * math.pi))
        pts.append((center[0] + math.cos(ang) * r,
                    center[1] + math.sin(ang) * r))
    return pts


# =====================================================================
#  CANVAS — el modelo de blending del juego (patrón mock_3agujeros_v616)
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

    def save(self, path):
        out = np.clip(self.acc * 255.0, 0, 255).astype(np.uint8)
        Image.fromarray(out).save(path)
        print('OK', path)


def tint(c, f):
    return (c[0], c[1], c[2], 255.0 * max(0.0, min(1.0, f)))


def black_disk(cv, center, R):
    yy, xx = np.mgrid[0:cv.H, 0:cv.W]
    d = np.hypot(xx - center[0], yy - center[1])
    cv.acc[d <= R] = 0.0


def soft_rim(cv, center, R, color, f):
    yy, xx = np.mgrid[0:cv.H, 0:cv.W]
    d = np.hypot(xx - center[0], yy - center[1])
    band = np.exp(-((d - R) / (R * 0.055)) ** 2) * f
    for k in range(3):
        cv.acc[..., k] += (color[k] / 255.0) * band


# --- LightningCore.Bolt 1:1 (funda + núcleo + RAMAS + gorros) ---

def lc_bolt(cv, pts, seed, flick, width, halo, core, alpha=1.0, core_scale=0.30):
    if pts is None or len(pts) < 2 or alpha <= 0.01:
        return
    n = len(pts)
    for i in range(n - 1):
        a, b = pts[i], pts[i + 1]
        seg = (b[0] - a[0], b[1] - a[1])
        seg_len = math.hypot(*seg)
        if seg_len < 0.5:
            continue
        rot = math.atan2(seg[1], seg[0])
        mid = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5)
        t = (i + 0.5) / (n - 1)
        wob = 0.78 + 0.42 * math.sin(t * math.pi)

        # 1) FUNDA
        cv.quad_add(GLOW, mid, (seg_len + width, width * 2.05 * wob), rot,
                    (halo[0], halo[1], halo[2], halo[3] * alpha))
        # 2) NÚCLEO
        cv.quad_add(GLOW, mid, (seg_len + width * core_scale,
                                width * core_scale * 2.4), rot,
                    (core[0], core[1], core[2], core[3] * alpha))

        # 3) RAMAS con decaimiento
        if 0 < i < n - 2 and vhash(seed, flick, i * 41 + 19) > 0.66:
            side = 1.0 if vhash(seed, flick, i * 7 + 23) > 0.5 else -1.0
            branch_len = seg_len * (1.4 + 1.1 * vhash(seed, flick, i * 53 + 31))
            ux, uy = seg[0] / seg_len, seg[1] / seg_len
            nx, ny = -uy, ux
            bd = (ux * 0.4 + nx * side, uy * 0.4 + ny * side)
            bl = math.hypot(*bd)
            bd = (bd[0] / bl, bd[1] / bl)
            b_end = (b[0] + bd[0] * branch_len, b[1] + bd[1] * branch_len)
            b_mid = ((b[0] + b_end[0]) * 0.5, (b[1] + b_end[1]) * 0.5)
            b_rot = math.atan2(bd[1], bd[0])
            cv.quad_add(GLOW, b_mid, (branch_len + width * 0.5, width * 1.05),
                        b_rot, (halo[0], halo[1], halo[2], halo[3] * 0.55 * alpha))
            cv.quad_add(GLOW, b_mid, (branch_len + width * 0.15, width * 0.42),
                        b_rot, (core[0], core[1], core[2], core[3] * 0.55 * alpha))

    # 4) GORROS
    for p in (pts[0], pts[-1]):
        cv.quad_add(GLOW, p, (width * 3.4, width * 3.4), 0.0,
                    (halo[0], halo[1], halo[2], halo[3] * 0.75 * alpha))
        cv.quad_add(GLOW, p, (width * 1.7, width * 1.7), 0.0,
                    (core[0], core[1], core[2], core[3] * 0.95 * alpha))


def lc_bolt_line(cv, start, end, seed, flick, width, halo, core,
                 alpha=1.0, segments=7, amp=14.0):
    lc_bolt(cv, bolt_points(start, end, seed, flick, segments, amp),
            seed, flick, width, halo, core, alpha)


def lc_arc(cv, center, radius, a1, a2, seed, flick, width, halo, core,
           alpha=1.0, count=9):
    lc_bolt(cv, arc_points(center, radius, a1, a2, seed, flick, count),
            seed, flick, width, halo, core, alpha)


# =====================================================================
#  UMBRAL ASCENDIDO — lluvia de rayos + arco dorado
# =====================================================================

def mock_umbral(time=1.7, seed=7):
    cv = Canvas(420, 420, bg=(14, 4, 8))
    center = (210.0, 210.0)
    r = 46.0
    RUNE_RADIUS, BOLT_COUNT, BOLT_HZ, BOLT_LEN = 2.55, 4, 8.0, 1.60
    GOLD_HZ, GOLD_R, GOLD_SPIN = 9.0, 1.38, 0.85

    # esfera + rim violeta + hint del círculo de runas
    soft_rim(cv, center, r * 1.02, (130, 25, 145), 0.30)
    cv.quad_add(GLOW, center, (RUNE_RADIUS * 2 * r, RUNE_RADIUS * 2 * r), 0.0,
                tint((240, 124, 65), 0.10))
    black_disk(cv, center, r * 1.00)

    # ⚡ LA LLUVIA DE RAYOS (DrawLightningRain 1:1)
    lflick = flick_tick(time, BOLT_HZ)
    for i in range(BOLT_COUNT):
        bseed = seed + 310 + i * 97
        if not flicker(bseed, lflick, 0.80):
            continue
        base_ang = (i / BOLT_COUNT * math.tau +
                    time * 0.10 * (1 if i % 2 == 0 else -1) +
                    0.55 * vhash(seed, 330 + i, 3))
        start = (center[0] + math.cos(base_ang) * RUNE_RADIUS * r,
                 center[1] + math.sin(base_ang) * RUNE_RADIUS * r)
        ox, oy = start[0] - center[0], start[1] - center[1]
        ol = math.hypot(ox, oy)
        ox, oy = ox / ol, oy / ol
        dx, dy = ox + 0.16, oy + 0.62
        dl = math.hypot(dx, dy)
        dirv = (dx / dl, dy / dl)
        ln = BOLT_LEN * r * (0.80 + 0.55 * vhash(seed, 340 + i, lflick))
        end = (start[0] + dirv[0] * ln, start[1] + dirv[1] * ln)
        w = max(r * 0.085, 2.2)
        lc_bolt_line(cv, start, end, bseed, lflick, w,
                     tint((205, 60, 0), 0.55), tint((255, 205, 130), 0.95),
                     1.0, 7, r * 0.16)
        cv.quad_add(GLOW, start, (0.55 * r, 0.55 * r), 0.0,
                    tint((255, 225, 175), 0.60))

    # ⚡ EL ARCO DORADO (DrawGoldenArc 1:1 — calibrado v2)
    gflick = flick_tick(time, GOLD_HZ)
    if flicker(seed + 777, gflick, 0.90):
        a0 = time * GOLD_SPIN
        w = max(r * 0.10, 2.6)
        lc_arc(cv, center, GOLD_R * r, a0, a0 + math.pi / 2, seed + 777, gflick,
               w, tint((255, 185, 70), 0.62), tint((255, 246, 208), 0.95), 1.0, 9)
        if flicker(seed + 778, gflick, 0.70):
            lc_arc(cv, center, GOLD_R * 1.12 * r, a0 + 0.35,
                   a0 + 0.35 + math.pi / 2 * 0.8, seed + 778, gflick, w * 0.62,
                   tint((255, 185, 70), 0.42), tint((255, 246, 208), 0.80), 1.0, 8)

    cv.save(f'{OUT}/umbral_ascendido_lightning.png')


# =====================================================================
#  BRUMA ASCENDIDA — coronas + rayos gelidos + cristales
# =====================================================================

def mock_bruma(time=1.7, seed=7):
    cv = Canvas(420, 420, bg=(4, 10, 16))
    center = (210.0, 210.0)
    r = 48.0
    RING_A, RING_B, RING_TILT = 1.75, 1.25, -0.33

    def ellipse(t, rad):
        ex = RING_A * rad * math.cos(t)
        ey = RING_B * rad * math.sin(t)
        c, s = math.cos(RING_TILT), math.sin(RING_TILT)
        return (center[0] + ex * c - ey * s, center[1] + ex * s + ey * c)

    soft_rim(cv, center, r * 1.02, (60, 200, 220), 0.32)
    # hint del anillo de humo
    for k in range(24):
        t = k / 24 * math.tau + time * 0.38
        p = ellipse(t, r)
        cv.quad_add(GLOW, p, (0.38 * r, 0.38 * r), 0.0, tint((90, 210, 235), 0.10))
    black_disk(cv, center, r * 1.00)

    # ⚡ LAS CORONAS DE ESCARCHA (DrawFrostCrowns 1:1)
    cflick = flick_tick(time, 10.0)
    spin = time * 0.55
    for k in range(3):
        cseed = seed + 8100 + k * 131
        if not flicker(cseed, cflick, 0.88):
            continue
        radius = (1.18 + 0.13 * k) * r
        a0 = spin * (1.0 if k % 2 == 0 else -0.7) + k * 2.2
        span = 1.35 - 0.18 * k
        w = max(r * (0.080 - 0.010 * k), 2.2)
        lc_arc(cv, center, radius, a0, a0 + span, cseed, cflick, w,
               tint((90, 210, 235), 0.50), tint((225, 250, 255), 0.95), 1.0, 9)

    # ⚡ LOS RAYOS GELIDOS (DrawGelidBolts 1:1)
    bflick = flick_tick(time, 9.0)
    for i in range(2):
        bseed = seed + 9200 + i * 173
        if not flicker(bseed, bflick, 0.78):
            continue
        t = time * (0.16 * (1 if i % 2 == 0 else -1)) + i * math.pi + 0.6
        start = ellipse(t, r)
        ox, oy = start[0] - center[0], start[1] - center[1]
        ol = math.hypot(ox, oy)
        ox, oy = ox / ol, oy / ol
        bx, by = 0.22 * (1 if i == 0 else -1), -0.30
        dx, dy = ox + bx, oy + by
        dl = math.hypot(dx, dy)
        dirv = (dx / dl, dy / dl)
        ln = 1.75 * r * (0.85 + 0.40 * vhash(seed, 9210 + i, 3))
        end = (start[0] + dirv[0] * ln, start[1] + dirv[1] * ln)
        w = max(r * 0.075, 2.2)
        lc_bolt_line(cv, start, end, bseed, bflick, w,
                     tint((90, 210, 235), 0.55), tint((225, 250, 255), 0.95),
                     1.0, 7, r * 0.15)
        cv.quad_add(GLOW, start, (0.55 * r, 0.55 * r), 0.0,
                    tint((185, 235, 250), 0.55))

    # LOS CRISTALES DE HIELO (DrawIceCrystals 1:1)
    for i in range(6):
        h = vhash(seed, 9500 + i, 29)
        ang = h * math.tau + time * 0.05 * (1 if i % 2 == 0 else -1)
        dist = (1.9 + 1.3 * vhash(seed, 9501 + i, 31)) * r
        pos = (center[0] + math.cos(ang) * dist + 3 * math.sin(time * 0.7 + i * 1.9),
               center[1] + math.sin(ang) * dist * 0.85 + 4 * math.sin(time * 0.5 + i * 2.7))
        rot = i * 0.8 + time * 0.12
        glint = 0.35 + 0.65 * (0.5 + 0.5 * math.sin(time * (0.5 + 0.3 * h) + i * 2.3)) ** 2
        size = (0.16 + 0.14 * h) * r
        for rr_ in (rot, rot + math.pi / 2):
            cv.quad_add(GLOW, pos, (size * 2.6, size * 0.22), rr_,
                        tint((185, 235, 250), 0.55 * glint))
        cv.quad_add(GLOW, pos, (size * 1.5, size * 0.16), rot + math.pi / 4,
                    tint((225, 250, 255), 0.40 * glint))
        cv.quad_add(GLOW, pos, (size * 0.9, size * 0.9), 0.0,
                    tint((225, 250, 255), 0.75 * glint))

    cv.save(f'{OUT}/bruma_ascendido_lightning.png')


if __name__ == '__main__':
    mock_umbral()
    mock_bruma()
    print('\nmock ascendidos OK')
