#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_sigilos_v634.py — MOCK 1:1 de las DOS LIBRERÍAS DE SIGNOS MÁGICOS.

Traducción numérica EXACTA (mismas fórmulas y constantes que el C#) de:
  · SIGILOLIB — SelloSolar (aro doble + 12 runas + 4 nodos + glifo maestro
    + polvo) y SistemaAnillos tier 6 (la corona del sol).
  · ORBITALIB — SelloVacio (anillo energético con las 20 bandas sin(20t−5τ),
    ecos, fotones, aro del horizonte, ondas) + núcleo negro.

Render en espacio de pantalla con aditivo flotante + tonemapping, como los
mocks de la casa (mock_sol_v632.py / mock_portales_v633.py).
"""
import math
import numpy as np
from PIL import Image

W, H = 460, 460           # lienzo por panel
SS = 2                    # super-sampling (anti-alias)
CW, CH = W * SS, H * SS


def hash01(seed, a, b):
    h = (seed * 374761393 + a * 668265263 + b * 1911520717) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFFFF) / 16777216.0


def soft_glow(size):
    """SoftGlow procedural FIEL: núcleo concentrado + caída rápida (r³)."""
    n = size
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    c = (n - 1) / 2.0
    r = np.sqrt(((xx - c) ** 2 + (yy - c) ** 2)) / c
    a = np.exp(-np.power(r, 3.0) * 5.0).astype(np.float32)
    a[a < 0.003] = 0.0
    return a


GLOW = soft_glow(96)
GN = GLOW.shape[0]


def ring_tex(size):
    """Ring fino procedural: toro delgado con núcleo brillante."""
    n = size
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    c = (n - 1) / 2.0
    r = np.sqrt(((xx - c) ** 2 + (yy - c) ** 2)) / c
    # radio visible ~0.46 del quad (calibración 2.174 → r_vis = 1/2.174)
    d = (r - 0.460) / 0.055
    a = np.exp(-d * d * 2.2).astype(np.float32)
    a[a < 0.003] = 0.0
    return a


RING = ring_tex(128)
RN = RING.shape[0]


def black_disk(size):
    n = size
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    c = (n - 1) / 2.0
    r = np.sqrt(((xx - c) ** 2 + (yy - c) ** 2)) / c
    a = np.clip((0.460 - r) / 0.06, 0.0, 1.0)
    return a.astype(np.float32)


DISK = black_disk(128)


class Canvas:
    """Lienzo aditivo flotante (RGB × alfa) — el buffer de VFXCore."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.buf = np.zeros((h, w, 3), np.float32)
        self.mask = np.zeros((h, w), np.float32)  # pase alfa (núcleo negro)

    def quad(self, x, y, sx, sy, rot, col, alpha, tex=None):
        """Quad centrado con tinte (R,G,B × intensidad-alfa)."""
        if alpha <= 0.003:
            return
        tex = GLOW if tex is None else tex
        tn = tex.shape[0]
        # esquinas del quad rotado
        cr, sr = math.cos(rot), math.sin(rot)
        hx, hy = sx * 0.5, sy * 0.5
        corners = [(cx * hx, cy * hy) for cx, cy in
                   ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        pts = []
        for px, py in corners:
            pts.append((x + px * cr - py * sr, y + px * sr + py * cr))
        xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
        x0, x1 = max(0, int(min(xs)) - 1), min(self.w, int(max(xs)) + 2)
        y0, y1 = max(0, int(min(ys)) - 1), min(self.h, int(max(ys)) + 2)
        if x0 >= x1 or y0 >= y1:
            return
        # bbox local: muestrear la textura con la rotación inversa
        X, Y = np.meshgrid(np.arange(x0, x1, dtype=np.float32),
                           np.arange(y0, y1, dtype=np.float32))
        dx, dy = X - x, Y - y
        lx = (dx * cr + dy * sr) / max(hx, 0.001)  # -1..1
        ly = (-dx * sr + dy * cr) / max(hy, 0.001)
        u = (lx * 0.5 + 0.5) * (tn - 1)
        v = (ly * 0.5 + 0.5) * (tn - 1)
        ok = (u >= 0) & (u <= tn - 1) & (v >= 0) & (v <= tn - 1)
        if not ok.any():
            return
        ui = np.clip(u, 0, tn - 1.001); vi = np.clip(v, 0, tn - 1.001)
        # bilinear
        u0 = ui.astype(np.int32); v0 = vi.astype(np.int32)
        fu = ui - u0; fv = vi - v0
        t = (tex[v0, u0] * (1 - fu) * (1 - fv) + tex[v0, u0 + 1] * fu * (1 - fv) +
             tex[v0 + 1, u0] * (1 - fu) * fv + tex[v0 + 1, u0 + 1] * fu * fv)
        t = t * ok
        a = t * alpha
        self.buf[y0:y1, x0:x1, 0] += col[0] * a
        self.buf[y0:y1, x0:x1, 1] += col[1] * a
        self.buf[y0:y1, x0:x1, 2] += col[2] * a

    def quad_mask(self, x, y, sx, sy, col, tex=None):
        """Pase ALFA (el núcleo negro tapa lo de atrás)."""
        tex = DISK if tex is None else tex
        tn = tex.shape[0]
        x0, x1 = int(x - sx * 0.5) - 1, int(x + sx * 0.5) + 2
        y0, y1 = int(y - sy * 0.5) - 1, int(y + sy * 0.5) + 2
        x0, y0 = max(0, x0), max(0, y0)
        x1, y1 = min(self.w, x1), min(self.h, y1)
        if x0 >= x1 or y0 >= y1:
            return
        X, Y = np.meshgrid(np.arange(x0, x1, dtype=np.float32),
                           np.arange(y0, y1, dtype=np.float32))
        u = (X - x) / max(sx, 0.001) * 0.5 + 0.5
        v = (Y - y) / max(sy, 0.001) * 0.5 + 0.5
        ui = np.clip(u * (tn - 1), 0, tn - 1.001)
        vi = np.clip(v * (tn - 1), 0, tn - 1.001)
        u0 = ui.astype(np.int32); v0 = vi.astype(np.int32)
        fu = ui - u0; fv = vi - v0
        t = (tex[v0, u0] * (1 - fu) * (1 - fv) + tex[v0, u0 + 1] * fu * (1 - fv) +
             tex[v0 + 1, u0] * (1 - fu) * fv + tex[v0 + 1, u0 + 1] * fu * fv)
        # mezcla alfa: el disco tapa
        m = np.clip(t, 0, 1)
        self.mask[y0:y1, x0:x1] = np.maximum(self.mask[y0:y1, x0:x1], m)
        dark = m[..., None]
        self.buf[y0:y1, x0:x1] *= (1.0 - dark)   # negro ABSOLUTO como el BlackDisk del juego

    def capsule(self, x, y, length, width, rot, col, alpha):
        sx = length + width
        sy = width * 1.9
        self.quad(x, y, sx, sy, rot, col, alpha)

    def ring_fine(self, x, y, r_vis, rot, col, alpha):
        s = r_vis * 2.174
        self.quad(x, y, s, s, rot, col, alpha, tex=RING)

    def tonemap(self, bg=(8, 9, 14)):
        # ADITIVO PURO como el juego: SpriteBatch Additive sobre fondo
        # oscuro, clamp simple SIN gamma (la gamma 1/2.2 empastaba: elevaba
        # los halos 0.08→0.32 y mataba el contraste trazo/halo 4:1→1.7:1).
        img = np.clip(self.buf, 0.0, 1.0)
        out = np.empty_like(img)
        for c in range(3):
            out[..., c] = img[..., c] * (1.0 - bg[c] / 255.0) + bg[c] / 255.0
        return np.clip(out * 255, 0, 255).astype(np.uint8)


# ════════════════════════════════════════════════════════════════════
#  LAS TABLAS DE RUNAS (EXACTAS del C# — SigiloLib)
# ════════════════════════════════════════════════════════════════════
RUNAS_SOLARES = [
    [(0, -4.5), (0, 4.5), (-4.5, 0), (4.5, 0), (-3, -3), (-1.2, -1.2), (3, -3), (1.2, -1.2), (-3, 3), (-1.2, 1.2), (3, 3), (1.2, 1.2)],
    [(0, 6.5), (0, 1), (0, 1), (-3, -2), (-3, -2), (0, -5), (0, -5), (3, -2), (3, -2), (0, 1), (-1.5, -6.5), (1.5, -6.5)],
    [(0, -5), (0, 5), (-5, 0), (5, 0), (-3.5, -3.5), (3.5, 3.5), (3.5, -3.5), (-3.5, 3.5), (-2.2, 0), (2.2, 0), (0, -2.2), (0, 2.2)],
    [(0, -7), (0, 7), (-3.2, -3.5), (0, -0.5), (3.2, -3.5), (0, -0.5), (-3.2, 3.5), (0, 0.5), (3.2, 3.5), (0, 0.5)],
    [(-3.5, 7), (-3.5, -5), (-3.5, -5), (0, -7), (0, -7), (3.5, -5), (3.5, -5), (3.5, 7), (-3.5, 7), (3.5, 7), (0, -4), (0, 7)],
    [(-4, 5), (-4, -2), (-4, -2), (-1.5, -5.5), (-1.5, -5.5), (0, -1.5), (0, -1.5), (1.5, -5.5), (1.5, -5.5), (4, -2), (4, -2), (4, 5), (-4, 5), (4, 5)],
    [(-4, 6.5), (3, -1), (3, -1), (0, -6.5), (0, -6.5), (4, -3), (1.5, 2), (4.5, 1.5), (-1.5, 1), (1.5, 4)],
    [(0, -6.5), (-4, 0), (-4, 0), (0, 6.5), (0, 6.5), (4, 0), (4, 0), (0, -6.5), (-2.2, 0), (2.2, 0), (0, -4), (0, 4)],
]


# ════════════════════════════════════════════════════════════════════
#  SIGILOLIB — las fórmulas exactas
# ════════════════════════════════════════════════════════════════════
def s_ring_a(k):
    k1 = min(k, 9); k2 = max(0, k - 9)
    return 1.62 + 0.44 * k1 + 0.30 * k2


def s_ring_flat(k): return 0.34 + 0.07 * (k % 3)


def s_ring_tilt(k, time, prec):
    t = -0.55 + 0.20 * k
    if prec:
        t += 0.10 * math.sin(time * (0.35 + 0.06 * k) + k * 1.9)
    return t


def s_ring_spin(k): return (1.0 if k % 2 == 0 else -1.0) * (0.26 + 0.045 * k)


def s_runes_of_ring(k): return 6 + 2 * min(k, 9) + 1 * max(0, k - 9)


def ellipse_point(cx, cy, a, b, tilt, t):
    ct, st = math.cos(t), math.sin(t)
    lx, ly = a * ct, b * st
    cr, sr = math.cos(tilt), math.sin(tilt)
    return (cx + lx * cr - ly * sr, cy + lx * sr + ly * cr)


def tangente(a, b, tilt, t):
    dlx, dly = -a * math.sin(t), b * math.cos(t)
    cr, sr = math.cos(tilt), math.sin(tilt)
    dx, dy = dlx * cr - dly * sr, dlx * sr + dly * cr
    return math.atan2(dy, dx)


def s_aro(c, cx, cy, a, b, tilt, spin, time, k, body, fade):
    SEG = 30
    px, py = ellipse_point(cx, cy, a, b, tilt, spin)
    for s in range(1, SEG + 1):
        t = spin + s / SEG * math.tau
        qx, qy = ellipse_point(cx, cy, a, b, tilt, t)
        mx, my = (px + qx) * 0.5, (py + qy) * 0.5
        dx, dy = qx - px, qy - py
        ln = math.hypot(dx, dy)
        if ln > 0.5:
            rot = math.atan2(dy, dx)
            depth = 0.55 + 0.45 * math.sin(t + math.pi / 2) * math.cos(tilt)
            pulse = 0.70 + 0.30 * math.sin(time * 1.8 + k * 1.3 + s * 0.35)
            wd = max(2.2, 0.052 * a) * (1.0 + 0.35 * depth)
            c.capsule(mx, my, ln, wd, rot, body,
                      (0.30 + 0.30 * depth) * pulse * fade)
        px, py = qx, qy


def s_runa_orbitando(c, cx, cy, a, b, tilt, ang, time, k, g, body, tip, gs, fade):
    breathe = 1.0 + 0.045 * math.sin(time * 1.35 + g * 0.9 + k * 0.5)
    gx, gy = ellipse_point(cx, cy, a * breathe, b * breathe, tilt, ang)
    tan_ang = tangente(a * breathe, b * breathe, tilt, ang)
    grot = tan_ang + math.pi / 2
    pulse = 0.75 + 0.25 * math.sin(time * 2.4 + g * 1.3 + k * 0.8)
    c.quad(gx, gy, 34 * gs, 34 * gs, 0.0, body, 0.20 * pulse * fade)
    strokes = RUNAS_SOLARES[(g + k) % len(RUNAS_SOLARES)]
    for si in range(0, len(strokes), 2):
        ax, ay = strokes[si]; bx, by = strokes[si + 1]
        la = (ax * gs, ay * gs); lb = (bx * gs, by * gs)
        ra = (la[0] * math.cos(grot) - la[1] * math.sin(grot) + gx,
              la[0] * math.sin(grot) + la[1] * math.cos(grot) + gy)
        rb = (lb[0] * math.cos(grot) - lb[1] * math.sin(grot) + gx,
              lb[0] * math.sin(grot) + lb[1] * math.cos(grot) + gy)
        mx, my = (ra[0] + rb[0]) * 0.5, (ra[1] + rb[1]) * 0.5
        dx, dy = rb[0] - ra[0], rb[1] - ra[1]
        ln = math.hypot(dx, dy)
        if ln < 0.01:
            continue
        rot = math.atan2(dy, dx)
        local_y = ((ay + by) * 0.5 + 7.0) / 14.0
        col = tuple(tip[i] + (body[i] - tip[i]) * (1.0 - local_y * 0.25) for i in range(3))
        c.capsule(mx, my, ln, 3.3 * gs, rot, col, 0.85 * pulse * fade)
    # perla
    pdx, pdy = (0 * math.cos(grot) - (-11.5 * gs) * math.sin(grot),
                0 * math.sin(grot) + (-11.5 * gs) * math.cos(grot))
    c.quad(gx + pdx, gy + pdy, 7.0 * gs, 7.0 * gs, 0.0, body, 0.60 * pulse * fade)
    c.quad(gx + pdx, gy + pdy, 3.2 * gs, 3.2 * gs, 0.0, tip, 0.9 * pulse * fade)


def s_sistema_anillos(c, cx, cy, R, time, seed, tier, rg, lifeT, alpha=1.0):
    n = min(tier, 20)
    gs = max(R / 52.0, 0.25) * 1.45
    for k in range(n):
        a = s_ring_a(k) * R
        b = a * s_ring_flat(k)
        tilt = s_ring_tilt(k, time, tier >= 7)
        spin = time * s_ring_spin(k)
        rc = s_runes_of_ring(k)
        blue = tier >= 7 and k % 3 == 2
        body = (0.529, 0.647, 1.0) if blue else (1.0, 0.745, 0.314)
        tip = (0.843, 0.902, 1.0) if blue else (1.0, 0.941, 0.725)
        if rg > 0:
            body = tuple(body[i] + ((1.0, 0.353, 0.157)[i] - body[i]) * rg * 0.45 for i in range(3))
        fade = (1.0 - lifeT * 0.55) * alpha
        s_aro(c, cx, cy, a, b, tilt, spin, time, k, body, fade)
        for g in range(rc):
            ang = g / rc * math.tau + spin
            s_runa_orbitando(c, cx, cy, a, b, tilt, ang, time, k, g, body, tip, gs, fade)


def s_sello_solar(c, cx, cy, radius, time, seed, alpha=1.0):
    body = (1.0, 0.745, 0.314)   # CuerpoOro (255,190,80)
    tip = (1.0, 0.941, 0.725)    # PuntaOro (255,240,185)
    tilt = 0.14
    spin = time * 0.22
    gs = max(radius / 52.0, 0.30)   # v6.34b: proporción de la casa
    # 1. aro doble (v6.34c: interior BIEN separado 0.66r)
    s_aro(c, cx, cy, radius, radius * 0.94, tilt, spin, time, 0, body, alpha)
    s_aro(c, cx, cy, radius * 0.66, radius * 0.62, tilt, -spin * 1.35, time, 1, tip, alpha * 0.85)
    # 2. las 8 runas (el alfabeto completo)
    for g in range(8):
        ang = g / 8 * math.tau + spin
        s_runa_orbitando(c, cx, cy, radius, radius * 0.94, tilt, ang, time, 0, g,
                         body, tip, gs, alpha)
    # 3. nodos cardinales
    for n in range(4):
        ang = n * math.pi / 2 + spin * 0.25
        nx, ny = ellipse_point(cx, cy, radius * 1.02, radius * 0.96, tilt, ang)
        px_ = radius * 0.085
        pulse = 0.80 + 0.20 * math.sin(time * 3.0 + n * 1.57)
        c.quad(nx, ny, px_ * 2.4, px_ * 2.4, 0.0, body, 0.55 * pulse * alpha)
        c.quad(nx, ny, px_ * 1.0, px_ * 1.0, 0.0, tip, 0.90 * pulse * alpha)
        flare = px_ * 3.2 * (0.85 + 0.15 * math.sin(time * 2.2 + n * 1.57))
        w = max(1.6, px_ * 0.16)
        c.capsule(nx, ny, flare, w, 0.0, tip, 0.50 * pulse * alpha)
        c.capsule(nx, ny, flare, w, math.pi / 2, tip, 0.50 * pulse * alpha)
    # 4. glifo maestro central GRANDE + destello 4 puntas (v6.34c)
    strokes = RUNAS_SOLARES[7]
    grot = -spin * 0.30
    esc = gs * 2.3
    flare_c = radius * 0.52 * (0.85 + 0.15 * math.sin(time * 2.0))
    w_c = max(1.8, radius * 0.022)
    c.capsule(cx, cy, flare_c, w_c, 0.0, tip, 0.35 * alpha)
    c.capsule(cx, cy, flare_c, w_c, math.pi / 2, tip, 0.35 * alpha)
    pulse = 0.75 + 0.25 * math.sin(time * 2.4 + 3.0 * 1.3)
    c.quad(cx, cy, 34 * esc, 34 * esc, 0.0, body, 0.20 * pulse * alpha * 0.95)
    for si in range(0, len(strokes), 2):
        ax, ay = strokes[si]; bx, by = strokes[si + 1]
        la = (ax * esc, ay * esc); lb = (bx * esc, by * esc)
        ra = (la[0] * math.cos(grot) - la[1] * math.sin(grot) + cx,
              la[0] * math.sin(grot) + la[1] * math.cos(grot) + cy)
        rb = (lb[0] * math.cos(grot) - lb[1] * math.sin(grot) + cx,
              lb[0] * math.sin(grot) + lb[1] * math.cos(grot) + cy)
        mx, my = (ra[0] + rb[0]) * 0.5, (ra[1] + rb[1]) * 0.5
        dx, dy = rb[0] - ra[0], rb[1] - ra[1]
        ln = math.hypot(dx, dy)
        if ln < 0.01:
            continue
        local_y = ((ay + by) * 0.5 + 7.0) / 14.0
        col = tuple(tip[i] + (body[i] - tip[i]) * (1.0 - local_y * 0.25) for i in range(3))
        c.capsule(mx, my, ln, 3.3 * esc, math.atan2(dy, dx), col, 0.85 * pulse * alpha * 0.95)
    # 5. polvo
    for i in range(9):
        h = hash01(seed, 910 + i, 23)
        d = 1.0 if i % 2 == 0 else -1.0
        ang = h * math.tau + time * 0.10 * d
        r = radius * 1.18 * (0.72 + 0.48 * hash01(seed, 911 + i, 31))
        px_, py_ = cx + math.cos(ang) * r, cy + math.sin(ang) * r * 0.82
        tw = 0.45 + 0.55 * math.sin(time * 2.6 + i * 2.0)
        sz = (0.08 + 0.06 * h) * radius * 1.18
        c.quad(px_, py_, sz, sz, 0.0, tip, 0.50 * tw * alpha * 0.80)


# ════════════════════════════════════════════════════════════════════
#  ORBITALIB — las fórmulas exactas
# ════════════════════════════════════════════════════════════════════
OA, OB, OTILT = 1.90, 1.18, -0.38
OSPIN = 0.349
BANDF, BANDS = 20.0, 5.0
OSEG = 44
HOT = (1.0, 0.941, 0.863)      # (255,240,220)
VIVO = (1.0, 0.267, 0.102)     # (255,68,26)
PROF = (0.784, 0.118, 0.078)   # (200,30,20)


def o_ellipse(cx, cy, r, t):
    return ellipse_point(cx, cy, OA * r, OB * r, OTILT, t)


def o_anillo_energia(c, cx, cy, rr, time, seed, flick, front, alpha=1.0):
    t0 = 0.0 if front else math.pi
    span = math.pi
    bmul = 1.30 if front else 0.90
    spin = time * OSPIN
    for s in range(OSEG):
        t = t0 + span * (s + 0.5) / OSEG
        tspin = t + spin
        ax, ay = o_ellipse(cx, cy, rr, t - span / (OSEG * 2))
        bx, by = o_ellipse(cx, cy, rr, t + span / (OSEG * 2))
        mx, my = (ax + bx) * 0.5, (ay + by) * 0.5
        dx, dy = bx - ax, by - ay
        seg = math.hypot(dx, dy)
        if seg < 0.5:
            continue
        rot = math.atan2(dy, dx)
        glow = 0.5 + 0.5 * math.sin(tspin * BANDF - time * BANDS)
        turb = 0.74 + 0.26 * hash01(seed, 700 + s, flick)
        inten = glow * turb * bmul
        if inten > 0.82:
            col = HOT
        elif inten > 0.45:
            col = VIVO
        else:
            col = PROF
        c.capsule(mx, my, seg, 0.36 * rr * (0.7 + inten), rot, col, 0.40 * inten * alpha)
        c.capsule(mx, my, seg, 0.12 * rr * inten, rot, col, 0.85 * inten * alpha)
        if inten > 0.86:
            c.quad(mx, my, 0.32 * rr, 0.32 * rr, rot, HOT, 0.70 * (inten - 0.86) / 0.14 * alpha)


def o_ecos(c, cx, cy, rr, time, distortion, alpha=1.0):
    phase = time * BANDS * 0.20
    e1 = 0.55 + 0.45 * math.sin(phase)
    c.ring_fine(cx, cy, 1.42 * rr, time * OSPIN, VIVO, 0.16 * e1 * alpha)
    e2 = 0.55 + 0.45 * math.sin(phase + math.pi)
    c.ring_fine(cx, cy, 0.80 * rr, -time * OSPIN * 0.7, PROF, 0.14 * e2 * alpha)
    c.quad(cx, cy, 4.4 * rr, 3.6 * rr, OTILT, VIVO, (0.10 + 0.05 * distortion) * alpha)


def o_fotones(c, cx, cy, rr, time, alpha=1.0):
    for i in range(3):
        t = time * (OSPIN + 0.55 + 0.20 * i) + i * 2.1
        px_, py_ = o_ellipse(cx, cy, rr, t)
        tw = 0.65 + 0.35 * math.sin(time * 8 + i * 2.3)
        bx, by = o_ellipse(cx, cy, rr, t - 0.22)
        dx, dy = px_ - bx, py_ - by
        ln = math.hypot(dx, dy)
        if ln > 0.5:
            c.capsule((px_ + bx) * 0.5, (py_ + by) * 0.5, ln, 0.14 * rr,
                      math.atan2(dy, dx), VIVO, 0.40 * tw * alpha)
        c.quad(px_, py_, 1.05 * rr, 1.05 * rr, 0.0, VIVO, 0.35 * tw * alpha)
        c.quad(px_, py_, 0.42 * rr, 0.42 * rr, 0.0, HOT, 0.85 * tw * alpha)


def o_ondas(c, cx, cy, r, time, seed, alpha=1.0):
    for i in range(2):
        fase = (time / 2.4 + i / 2 + hash01(seed, 810 + i, 37)) % 1.0
        growth = math.sin(fase * math.pi)
        if growth < 0.06:
            continue
        radio = (1.0 + 1.6 * fase) * r
        a = 0.20 * (1.0 - fase) * growth * alpha
        c.ring_fine(cx, cy, radio, time * 0.4 * (1 if i % 2 == 0 else -1), VIVO, a)


def o_sello_vacio(c, cx, cy, radius, time, seed, alpha=1.0):
    flick = int(time * 12)
    distortion = math.sin(time) * 0.3
    rr = radius * (1.0 + 0.03 * distortion)
    o_ecos(c, cx, cy, rr, time, distortion, alpha)
    # EXPOSICIÓN DE MOVIMIENTO (3 sub-frames = captura con obturador
    # abierto 1/18s — honesta con el juego real: las bandas VIAJAN a
    # 5 rad/s y los fotones orbitan; un frame congelado las mataba)
    for dt, sub in ((0.10, 1/3), (0.05, 1/3), (0.0, 1/3)):
        t2 = time - dt
        f2 = int(t2 * 12)
        o_anillo_energia(c, cx, cy, rr * (1.0 + 0.03 * math.sin(t2) * 0.3), t2, seed, f2,
                         front=False, alpha=alpha * sub)
    c.ring_fine(cx, cy, 1.02 * radius, time * 0.15, VIVO,
                0.40 + 0.12 * math.sin(time * 1.7))
    # núcleo negro ENTRE mitades (el consumidor lo pone; aquí simulado)
    c.quad_mask(cx, cy, 2.28 * radius, 2.28 * radius, None)
    for dt, sub in ((0.10, 1/3), (0.05, 1/3), (0.0, 1/3)):
        t2 = time - dt
        f2 = int(t2 * 12)
        o_anillo_energia(c, cx, cy, rr * (1.0 + 0.03 * math.sin(t2) * 0.3), t2, seed, f2,
                         front=True, alpha=alpha * sub)
        o_fotones(c, cx, cy, rr, t2, alpha * sub)
    o_ondas(c, cx, cy, radius, time, seed, alpha)


# ════════════════════════════════════════════════════════════════════
#  RENDER
# ════════════════════════════════════════════════════════════════════
def panel_sigilo():
    c = Canvas(CW, CH)
    cx, cy = CW * 0.28, CH * 0.5
    time, seed = 4.7, 31298
    # EL SOL con su sistema de anillos (tier 6)
    R = 52 * SS * 0.56
    # cuerpo solar simple (disco dorado sólido + halo)
    c.quad(cx, cy, 5.6 * R, 5.6 * R, 0.0, (0.85, 0.55, 0.18), 0.08)
    c.quad(cx, cy, 2.4 * R, 2.4 * R, 0.0, (1.0, 0.72, 0.28), 0.28)
    for _ in range(3):
        c.quad(cx, cy, 2.05 * R, 2.05 * R, 0.0, (1.0, 0.86, 0.55), 0.42)
    s_sistema_anillos(c, cx, cy, R, time, seed, 6, 0.0, 0.15)
    # EL SELLO SOLAR a ESCALA REAL DE JUEGO (r≈88px en pantalla)
    sx, sy = CW * 0.74, CH * 0.5
    s_sello_solar(c, sx, sy, 88 * SS, time, seed, 1.0)
    return c.tonemap()


def panel_orbita():
    c = Canvas(CW, CH)
    cx, cy = CW * 0.30, CH * 0.5
    time, seed = 4.7, 4711
    # el vórtice cósmico completo (radio 50·1.4)
    radius = 50 * SS * 1.05
    # aura oscura
    c.quad(cx, cy, 7.0 * radius, 7.0 * radius, 0.0, (0.078, 0.016, 0.024), 0.0)  # (no-op visual en aditivo)
    o_sello_vacio(c, cx, cy, radius, time, seed, 1.0)
    # LA CORONA DE ARCOS (sección B — al buffer)
    horizon = 96 * SS
    ccx, ccy = CW * 0.74, CH * 0.66
    time_c = time
    from_cr = ((0.616, 0.055, 0.220), (0.827, 0.110, 0.345))  # CrimsonCourt aprox
    for l in range(5):
        t01 = l / 4.0
        breathe = 1.0 + 0.06 * math.sin(time_c * 2.2 + l * 1.7)  # Breathe aprox
        sway = 0.18 * (0.5 * math.sin(time_c * 0.9 + l * 2.6))
        hwL = horizon * (1.55 - 0.30 * t01) * (1 - sway) * breathe
        hwR = horizon * (1.55 - 0.30 * t01) * (1 + sway) * breathe
        apexH = horizon * (2.30 - 0.95 * t01) * breathe
        baseY = ccy - horizon * 1.06
        for s in range(19):
            ang = s / 18 * math.pi
            edge = math.sin(ang); side = math.cos(ang)
            hw = hwL if side < 0 else hwR
            px_ = ccx - math.cos(ang) * hw
            py_ = baseY - edge * apexH
            col = tuple(from_cr[0][i] + (from_cr[1][i] - from_cr[0][i]) * edge for i in range(3))
            thick = 0.16 + 0.13 * edge * (1 - 0.35 * edge)
            inten = 0.30 + 0.70 * edge
            ppx = horizon * thick
            c.quad(px_, py_, ppx * 2, ppx * 2, 0.0, col, inten * 0.8)
        for s in range(1, 18):
            ang = s / 18 * math.pi
            edge = math.sin(ang); side = math.cos(ang)
            hw = (hwL if side < 0 else hwR) * 0.66
            px_ = ccx - math.cos(ang) * hw
            py_ = baseY - edge * apexH * 0.72
            epx = horizon * 0.09
            c.quad(px_, py_, epx * 2, epx * 2, 0.0, (0.45, 0.06, 0.16), 0.5)
        kp = 0.85 + 0.30 * math.sin(time_c * 3.1 + l * 2.3)
        ax_, ay_ = ccx, baseY - apexH
        kpx = horizon * 0.34
        c.quad(ax_, ay_, kpx * 2, kpx * 2, 0.0, (0.95, 0.35, 0.10), kp * 0.8)
        fl = horizon * 0.85 * kp
        c.quad(ax_, ay_, fl, horizon * 0.10, 0.0, (1.0, 0.62, 0.25), kp * 0.7)
        c.quad(ax_, ay_, horizon * 0.10, fl, 0.0, (1.0, 0.62, 0.25), kp * 0.7)
        c.quad(ax_, ay_, horizon * 0.28, horizon * 0.28, 0.0, (1.0, 0.95, 0.85), kp * 0.75)
    return c.tonemap()


def main():
    import os
    out = os.path.join(os.path.dirname(__file__), "..", "research", "v634")
    os.makedirs(os.path.abspath(out), exist_ok=True)
    img1 = Image.fromarray(panel_sigilo()).resize((W, H), Image.LANCZOS)
    p1 = os.path.abspath(os.path.join(out, "MOCK_SIGILOLIB_v634.png"))
    img1.save(p1)
    img2 = Image.fromarray(panel_orbita()).resize((W, H), Image.LANCZOS)
    p2 = os.path.abspath(os.path.join(out, "MOCK_ORBITALIB_v634.png"))
    img2.save(p2)
    print(p1)
    print(p2)


if __name__ == "__main__":
    main()
