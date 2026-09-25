#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_portales_v633.py — EL MOCK 1:1 DE LOS CUATRO DESGARROS NUEVOS.

Reimplementa en numpy las CUATRO primitivas de RiftLib v6.33 con SUS
MISMAS fórmulas (hash determinista, radios, alfas, colores hex) sobre el
cielo claro de Terraria — 4 paneles:
  1. DesgarroGlitch    (La Sutura Cuántica)
  2. PortalAnillos     (El Portal Dimensional)
  3. OjoEspacial       (El Pliegue del Espacio)
  4. HeridaElectrica   (La Herida Eléctrica)
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(os.path.dirname(BASE), "research", "v633")
os.makedirs(OUT, exist_ok=True)

# ---- el hash determinista de VFXCore (mismo espíritu) ----
def H01(seed, a, b):
    x = math.sin(seed * 127.1 + a * 311.7 + b * 74.7) * 43758.5453
    return x - math.floor(x)


def sky(w, h):
    yy = np.linspace(0, 1, h)[:, None]
    top = np.array([108, 152, 205], np.float32) / 255
    bot = np.array([168, 205, 235], np.float32) / 255
    img = top * (1 - yy) + bot * yy
    return np.broadcast_to(img, (h, w, 3)).copy()


def add_glow(img, cx, cy, rw, rh, rot, rgb, alpha):
    """Dibuja un glow elíptico rotado aditivo (aprox SoftGlow)."""
    h, w = img.shape[:2]
    x0, x1 = max(0, int(cx - rw)), min(w, int(cx + rw))
    y0, y1 = max(0, int(cy - rh)), min(h, int(cy + rh))
    if x1 <= x0 or y1 <= y0:
        return
    xs = np.arange(x0, x1) + 0.5 - cx
    ys = np.arange(y0, y1) + 0.5 - cy
    X, Y = np.meshgrid(xs, ys)
    cr, sr = math.cos(rot), math.sin(rot)
    U = (X * cr + Y * sr) / max(rw, 0.5)
    V = (-X * sr + Y * cr) / max(rh, 0.5)
    d = np.sqrt(U * U + V * V)
    fall = np.clip(1 - d, 0, 1) ** 1.6
    a = fall * alpha
    img[y0:y1, x0:x1] = np.clip(
        img[y0:y1, x0:x1] + np.array(rgb, np.float32)[None, None, :] * a[..., None], 0, 1)


def alpha_ellipse(img, cx, cy, rw, rh, rot, rgb, alpha):
    """Elipse SÓLIDA en pase alfa (para el negro del ojo)."""
    h, w = img.shape[:2]
    x0, x1 = max(0, int(cx - rw - 2)), min(w, int(cx + rw + 2))
    y0, y1 = max(0, int(cy - rh - 2)), min(h, int(cy + rh + 2))
    if x1 <= x0 or y1 <= y0:
        return
    xs = np.arange(x0, x1) + 0.5 - cx
    ys = np.arange(y0, y1) + 0.5 - cy
    X, Y = np.meshgrid(xs, ys)
    cr, sr = math.cos(rot), math.sin(rot)
    U = (X * cr + Y * sr) / max(rw, 0.5)
    V = (-X * sr + Y * cr) / max(rh, 0.5)
    inside = (U * U + V * V) <= 1.0
    src = np.array(rgb, np.float32)
    reg = img[y0:y1, x0:x1]
    a = inside * alpha
    img[y0:y1, x0:x1] = src[None, None, :] * a[..., None] + reg * (1 - a[..., None])


def ring(img, cx, cy, r, grosor, rgb, alpha):
    """Un anillo (círculo con grosor) aditivo."""
    add_glow(img, cx, cy, r * 1.15, r * 1.15, 0, (0, 0, 0), 0)  # noop keeps lints quiet
    h, w = img.shape[:2]
    x0, x1 = max(0, int(cx - r - grosor)), min(w, int(cx + r + grosor))
    y0, y1 = max(0, int(cy - r - grosor)), min(h, int(cy + r + grosor))
    if x1 <= x0 or y1 <= y0:
        return
    xs = np.arange(x0, x1) + 0.5 - cx
    ys = np.arange(y0, y1) + 0.5 - cy
    X, Y = np.meshgrid(xs, ys)
    d = np.sqrt(X * X + Y * Y)
    band = np.clip(1 - np.abs(d - r) / max(grosor, 0.5), 0, 1) ** 1.3
    img[y0:y1, x0:x1] = np.clip(
        img[y0:y1, x0:x1] + np.array(rgb, np.float32)[None, None, :] * (band * alpha)[..., None], 0, 1)


def line(img, p0, p1, grosor, rgb, alpha):
    """Línea aditiva con grosor (para rejillas y arcos)."""
    x0, y0 = p0; x1, y1 = p1
    n = max(2, int(math.hypot(x1 - x0, y1 - y0)))
    for i in range(n + 1):
        t = i / n
        add_glow(img, x0 + (x1 - x0) * t, y0 + (y1 - y0) * t,
                 grosor, grosor * 0.6, 0, rgb, alpha)


# ============== LAS 4 PRIMITIVAS (fórmulas del C#) ==============
CIAN = (0, 176, 255); MAGENTA = (213, 0, 249); BLANCO = (255, 255, 255)
CIAN2 = (0, 229, 255); VIOLETA = (124, 77, 255); FUCSIA = (255, 64, 129)
CIAN3 = (0, 212, 255); AMBAR = (255, 184, 0)
CIAN4 = (0, 255, 255); VIOLETA2 = (138, 43, 226); HIELO = (224, 255, 255)


def desgarro_glitch(img, cx, cy, R, time, seed, alpha=1.0):
    open_ = 1.0
    # PASO 1 — el núcleo magenta (pase alfa, 8 rebanadas)
    for s in range(8):
        a0 = s * math.pi / 4 + 0.09 * math.sin(time * 1.1 + s)
        a1 = (s + 1) * math.pi / 4 - 0.09 * math.sin(time * 0.9 + s * 2)
        r0 = R * (0.62 + 0.30 * H01(seed, s, 107))
        r1 = R * (0.62 + 0.30 * H01(seed, s + 1, 109))
        # polígono de la rebanada
        pts = [(cx, cy),
               (cx + math.cos(a0) * r0, cy + math.sin(a0) * r0),
               (cx + math.cos((a0 + a1) / 2) * max(r0, r1), cy + math.sin((a0 + a1) / 2) * max(r0, r1)),
               (cx + math.cos(a1) * r1, cy + math.sin(a1) * r1)]
        # pintar con alpha_ellipse aproximada: dibujo con PIL en una máscara
        tmp = Image.new("L", (img.shape[1], img.shape[0]), 0)
        d = ImageDraw.Draw(tmp)
        d.polygon([(round(p[0]), round(p[1])) for p in pts], fill=int(120 * alpha * open_))
        m = np.asarray(tmp, np.float32) / 255
        src = np.array(MAGENTA, np.float32) / 255
        img[:] = src[None, None, :] * m[..., None] + img * (1 - m[..., None])
    # PASO 2 — el borde glitch (24 vértices)
    tick = time * 60
    for v in range(24):
        h1, h2, h3 = H01(seed, v, 113), H01(seed, v, 127), H01(seed, v, 131)
        ang = v * math.tau / 24
        rv = R * (0.70 + 0.34 * h1)
        flickHz = 6 + 14 * h2
        lit = math.sin(tick * flickHz * 0.1047 + h3 * 6.28) * 0.5 + 0.5
        if lit < 0.25:
            continue
        dx, dy = math.cos(ang), math.sin(ang)
        bo = rv + (6 + 16 * h3) * lit
        ast = CIAN2 if h1 > 0.5 else VIOLETA
        add_glow(img, cx + dx * bo, cy + dy * bo, 3 + 11 * h2, 2 + 5 * h3, ang,
                 ast, (0.45 + 0.55 * lit) * alpha)
        add_glow(img, cx + dx * rv, cy + dy * rv, R * 0.15, 2.2 + 2.5 * lit, ang + math.pi / 2,
                 ast, 0.60 * alpha * lit)
        if v % 3 == 0:
            line(img, (cx + dx * rv * 0.55, cy + dy * rv * 0.55),
                 (cx + dx * rv, cy + dy * rv), 2.0, FUCSIA, 0.42 * alpha * lit)
    # el núcleo cegador
    heart = 0.5 + 0.5 * math.sin(time * 9 + seed)
    add_glow(img, cx, cy, R * 0.475, R * 0.475, 0, MAGENTA, 0.34 * alpha)
    add_glow(img, cx, cy, R * 0.26, R * 0.26, 0, VIOLETA, 0.42 * alpha)
    add_glow(img, cx, cy, R * 0.15 * (1 + 0.12 * heart), R * 0.15 * (1 + 0.12 * heart), 0,
             BLANCO, (0.50 + 0.30 * heart) * alpha)


def portal_anillos(img, cx, cy, radius, time, seed, alpha=1.0):
    progress = 1.0
    breathe = 1 + 0.05 * math.sin(time * 0.3 * math.tau + seed)
    open_ = progress ** 0.7
    for k in range(6):
        fk = k / 5
        anilloOpen = min(1.0, progress * (1.6 + k * 0.35) - k * 0.35)
        r = radius * (0.22 + 0.78 * fk) * anilloOpen * breathe
        if r < 2:
            continue
        anillo = tuple((1 - fk) * np.array(MAGENTA) + fk * np.array(CIAN))
        grosor = 3.2 + (6.5 - 3.2) * (1 - fk)
        alfa = (0.40 + 0.50 * (1 - fk)) * alpha * anilloOpen
        ring(img, cx, cy, r, grosor, anillo, alfa)
        spin = time * (0.35 + 0.55 * (1 - fk)) * (1 if k % 2 == 0 else -1)
        for g in range(8 + k * 2):
            h = H01(seed, k * 31 + g, 71)
            if h < 0.45:
                continue
            ang = spin * (1 + 0.15 * fk) + g * math.tau / (8 + k * 2)
            gp = (cx + math.cos(ang) * r, cy + math.sin(ang) * r)
            add_glow(img, *gp, grosor * 0.25, grosor * 1.2, ang + math.pi / 2,
                     tuple(0.65 * np.array(anillo) + 0.35 * 255), alfa * 0.85)
    # el polvo espiral
    for d_ in range(14):
        h1, h2 = H01(seed, d_, 79), H01(seed, d_, 83)
        t = (time * (0.25 + 0.3 * h2) + h1) % 1
        rr = radius * 1.05 * t * open_
        ang = h2 * math.tau + time * (1.8 + h1 * 1.2) * (1 if h1 > 0.5 else -1)
        a = 0.55 * alpha * (0.3 + 0.7 * math.sin(t * math.pi))
        add_glow(img, cx + math.cos(ang) * rr, cy + math.sin(ang) * rr, 4, 4, 0,
                 tuple(np.array(MAGENTA) * (1 - h1) + np.array(CIAN) * h1), a)
    # el núcleo blanco + el velo magenta del fondo (v6.33 b)
    add_glow(img, cx, cy, radius * 0.525, radius * 0.525, 0, MAGENTA, 0.16 * alpha)
    heart = 0.5 + 0.5 * math.sin(time * 0.3 * math.tau + 1.7)
    nr = radius * 0.20 * (1 + 0.10 * heart)
    add_glow(img, cx, cy, nr * 1.7, nr * 1.7, 0, CIAN, 0.35 * alpha)
    add_glow(img, cx, cy, nr * 1.0, nr * 1.0, 0, MAGENTA, 0.50 * alpha)
    add_glow(img, cx, cy, nr * 0.58, nr * 0.58, 0, BLANCO, (0.75 + 0.25 * heart) * alpha)


def ojo_espacial(img, cx, cy, R, time, seed, alpha=1.0, tilt=-0.55):
    open_ = 1.0
    # PASO 1 — el núcleo negro (doble elipse)
    for sgn in (1, -1):
        ex = cx + math.cos(tilt) * R * 0.34 * sgn
        ey = cy + math.sin(tilt) * R * 0.34 * sgn
        alpha_ellipse(img, ex, ey, R * 0.78, R * 0.78 * 0.575, tilt + math.pi / 2,
                      (8 / 255, 8 / 255, 14 / 255), 0.92 * alpha)
    # PASO 2 — la rejilla que converge
    for g in range(9):
        ang = g * math.tau / 9 + tilt * 0.5
        borde = np.array([cx + math.cos(ang) * R * 0.95, cy + math.sin(ang) * R * 0.95])
        tang = np.array([-math.sin(ang), math.cos(ang)])
        warp = 0.35 * math.sin(time * 0.9 + g * 1.3) * R
        mid = (borde + np.array([cx, cy])) * 0.5 + tang * warp
        line(img, tuple(borde), tuple(mid), 2.2, CIAN3, 0.45 * alpha)
        line(img, tuple(mid), (cx, cy), 1.6, AMBAR, 0.35 * alpha)
    for c in range(3):
        rr = R * (0.30 + 0.28 * c)
        ring(img, cx, cy, rr, 2.0, CIAN3, 0.20 * alpha)
    # PASO 3 — el rim + ámbar
    for sgn in (1, -1):
        ex = cx + math.cos(tilt) * R * 0.34 * sgn
        ey = cy + math.sin(tilt) * R * 0.34 * sgn
        add_glow(img, ex, ey, R * 0.78 * 1.15, R * 0.78 * 0.66, tilt + math.pi / 2,
                 AMBAR, 0.16 * alpha)
        ring_ellip = lambda r, a: None
        # rim: dos anillos elípticos (aprox con múltiples glows)
        for k in range(30):
            a = k * math.tau / 30
            px = ex + math.cos(a) * R * 0.78 * math.cos(tilt + math.pi / 2) * 1.12 \
                 - math.sin(a) * R * 0.78 * 0.575 * math.sin(tilt + math.pi / 2) * 1.12
            py = ey + math.cos(a) * R * 0.78 * math.sin(tilt + math.pi / 2) * 1.12 \
                 + math.sin(a) * R * 0.78 * 0.575 * math.cos(tilt + math.pi / 2) * 1.12
            add_glow(img, px, py, 3.2, 3.2, 0, CIAN3, 0.55 * alpha)
    # el cuello brillante
    add_glow(img, cx, cy, R * 0.275, R * 0.275, 0, (255, 250, 205), 0.50 * alpha)
    add_glow(img, cx, cy, R * 0.11, R * 0.11, 0, (255, 250, 205), 0.85 * alpha)
    # PASO 4 — la succión
    for d_ in range(10):
        h1, h2 = H01(seed, d_, 101), H01(seed, d_, 103)
        t = 1 - ((time * (0.30 + 0.35 * h2) + h1) % 1)
        ang = h2 * math.tau + time * 0.8 * (h1 - 0.5) * 2
        px = cx + math.cos(ang) * R * 1.1 * t
        py = cy + math.sin(ang) * R * 1.1 * t
        col = tuple(np.array(AMBAR) * (1 - t) + np.array(CIAN3) * t)
        add_glow(img, px, py, 3 + 3 * t, 2.5, ang, col, 0.50 * alpha * t)


def fractal_path(p0, p1, seed, slot, gens=5, chaos=0.15):
    pts = [np.array(p0, np.float64), np.array(p1, np.float64)]
    ln = np.linalg.norm(pts[1] - pts[0])
    offset = ln * chaos
    for g in range(gens):
        count = len(pts)
        for i in range(count - 1):
            a, b = pts[i * 2], pts[i * 2 + 1]
            seg = b - a
            sl = np.linalg.norm(seg)
            mid = (a + b) * 0.5
            if sl > 0.01:
                normal = np.array([-seg[1] / sl, seg[0] / sl])
                j = (H01(seed, slot * 31 + g, i * 61 + 13) - 0.5) * 2
                mid = mid + normal * (j * offset)
            pts.insert(i * 2 + 1, mid)
        offset *= 0.5
    return pts


def herida_electrica(img, ox, oy, dx, dy, length, w, time, seed, alpha=1.0):
    rot = math.atan2(dy, dx)
    perp = np.array([-dy, dx])
    mid = (ox + dx * length * 0.5, oy + dy * length * 0.5)
    # 1. el vacío (alfa)
    tmp = Image.new("L", (img.shape[1], img.shape[0]), 0)
    d = ImageDraw.Draw(tmp)
    p0 = (ox - perp[0] * w * 0.65, oy - perp[1] * w * 0.65)
    p1 = (ox + dx * length - perp[0] * w * 0.65, oy + dy * length - perp[1] * w * 0.65)
    p2 = (ox + dx * length + perp[0] * w * 0.65, oy + dy * length + perp[1] * w * 0.65)
    p3 = (ox + perp[0] * w * 0.65, oy + perp[1] * w * 0.65)
    d.polygon([p0, p1, p2, p3], fill=int(240 * alpha))
    m = np.asarray(tmp, np.float32) / 255
    src = np.array([5, 5, 16], np.float32) / 255
    img[:] = src[None, None, :] * m[..., None] + img * (1 - m[..., None])
    # 2. la luz
    add_glow(img, mid[0], mid[1], length / 2 + w, w * 0.92, rot, VIOLETA2, 0.55 * alpha)
    add_glow(img, mid[0], mid[1], length / 2 + w, w * 0.72, rot, CIAN4, 0.30 * alpha)
    add_glow(img, mid[0], mid[1], length / 2 + w * 0.5, w * 0.50, rot, CIAN4, 0.85 * alpha)
    add_glow(img, mid[0], mid[1], length / 2 + w * 0.22, w * 0.19, rot, HIELO, 1.0 * alpha)
    # aberración
    ab = 1.6 + 1.2 * math.sin(time * 13)
    add_glow(img, mid[0] + perp[0] * ab, mid[1] + perp[1] * ab, length / 2 + w, w * 0.58, rot,
             (255, 40, 40), 0.16 * alpha)
    add_glow(img, mid[0] - perp[0] * ab, mid[1] - perp[1] * ab, length / 2 + w, w * 0.58, rot,
             CIAN4, 0.16 * alpha)
    # 3. los arcos voltaicos (StormArc ×3 — fractal + 3 capas)
    for a in range(3):
        h = H01(seed, a, 157)
        t0 = 0.12 + 0.24 * h + a * 0.22
        a0 = np.array([ox + dx * length * t0, oy + dy * length * t0]) + perp * (w * 0.22 * (h - 0.5) * 2)
        a1 = np.array([ox + dx * length * min(1, t0 + 0.30 + 0.2 * h),
                       oy + dy * length * min(1, t0 + 0.30 + 0.2 * h)]) \
             - perp * (w * 0.22 * (H01(seed, a, 163) - 0.5) * 2)
        pts = fractal_path(tuple(a0), tuple(a1), seed + a * 31, int(time * 6))
        for i in range(len(pts) - 1):
            p, q = pts[i], pts[i + 1]
            seg = q - p
            sl = np.linalg.norm(seg)
            if sl < 0.3:
                continue
            ang = math.atan2(seg[1], seg[0])
            pm = (p + q) * 0.5
            for wd, col, al in [(3.5 * 0.8, (30, 80, 168), 0.30),
                                (3.5 * 0.4, (94, 179, 255), 0.55),
                                (3.5 * 0.22, (255, 255, 255), 0.90)]:
                add_glow(img, pm[0], pm[1], sl / 2 + wd * 0.8, wd * 0.5, ang, col,
                         al * 0.65 * alpha)
    # 3b. LAS RAMIFICACIONES (v6.33 b)
    for r in range(5):
        h1, h2, h3 = H01(seed, r, 173), H01(seed, r, 179), H01(seed, r, 181)
        if h1 < 0.30:
            continue
        t0 = 0.12 + 0.76 * h2
        nace = (ox + dx * length * t0, oy + dy * length * t0)
        lado = 1 if h3 > 0.5 else -1
        ang_r = rot + lado * (0.96 + 0.35 * h1)
        largo_r = length * (0.10 + 0.13 * h2)
        fin = (nace[0] + math.cos(ang_r) * largo_r, nace[1] + math.sin(ang_r) * largo_r)
        pts = fractal_path(nace, fin, seed + 601 + r * 43, int(time * 24))
        for i in range(len(pts) - 1):
            p, q = pts[i], pts[i + 1]
            seg = q - p
            sl = np.linalg.norm(seg)
            if sl < 0.3:
                continue
            ang = math.atan2(seg[1], seg[0])
            pm = (p + q) * 0.5
            add_glow(img, pm[0], pm[1], sl / 2 + 5.5, 2.7, ang, VIOLETA2, 0.75 * alpha)
            add_glow(img, pm[0], pm[1], sl / 2 + 2.5, 1.2, ang, CIAN4, 0.85 * alpha)
    # 4. chispas en los extremos
    for ex, ey in [(ox, oy), (ox + dx * length, oy + dy * length)]:
        for s in range(4):
            h1 = H01(seed + 5, s, 31)
            ang = h1 * math.tau
            add_glow(img, ex + math.cos(ang) * 14, ey + math.sin(ang) * 14, 5, 3, ang,
                     CIAN4, 0.6 * alpha)


def main():
    W = H = 460
    time = 6.18
    seed = 313

    paneles = []
    # 1 — DESGARRO GLITCH
    img = sky(W, H)
    desgarro_glitch(img, W / 2, H / 2, 130, time, seed)
    paneles.append(("SUTURA_CUANTICA", img))
    # 2 — PORTAL ANILLOS
    img = sky(W, H)
    portal_anillos(img, W / 2, H / 2, 150, time, seed)
    paneles.append(("PORTAL_DIMENSIONAL", img))
    # 3 — OJO ESPACIAL
    img = sky(W, H)
    ojo_espacial(img, W / 2, H / 2, 160, time, seed, tilt=-0.55)
    paneles.append(("PLIEGUE_ESPACIO", img))
    # 4 — HERIDA ELÉCTRICA
    img = sky(W, H)
    herida_electrica(img, 70, H * 0.72, 0.87, -0.5, 340, 24, time, seed)
    paneles.append(("HERIDA_ELECTRICA", img))

    # compone la tira 2×2
    out = Image.new("RGB", (W * 2 + 30, H * 2 + 30), (250, 250, 252))
    for i, (name, arr) in enumerate(paneles):
        p = Image.fromarray((np.clip(arr, 0, 1) * 255).astype(np.uint8))
        x = (i % 2) * (W + 10) + 10
        y = (i // 2) * (H + 10) + 10
        out.paste(p, (x, y))
    out.save(os.path.join(OUT, "MOCK_DESGARROS_v633.png"))
    for name, arr in paneles:
        p = Image.fromarray((np.clip(arr, 0, 1) * 255).astype(np.uint8))
        p.save(os.path.join(OUT, f"MOCK_{name}_v633.png"))
    print("mocks generados:", ", ".join(n for n, _ in paneles))


if __name__ == "__main__":
    main()
