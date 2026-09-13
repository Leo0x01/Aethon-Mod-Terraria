#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_olvido_staff_icon_v615.py — v6.15

Regenera el icono del OlvidoBlackHoleStaff con la NUEVA identidad del
Agujero Negro del Olvido 100% código: bastón oscuro-violeta y un
mini-agujero VIOLETA/ROSA con anillo de plasma, runa dorada y destello
arcano.

El icono anterior (v6.14) mostraba el mini-agujero carmesí de la
referencia roja — esa referencia fue BORRADA del proyecto por directiva
del usuario ("borra todo rastro de la referencia").

Salida: Content/Weapons/Cosmic/OlvidoBlackHoleStaff.png (28×30 RGBA).
"""

import os
import numpy as np
from PIL import Image

W, H = 28, 30
SS = 8  # supersampling
OW, OH = W * SS, H * SS
OUT = os.path.join(os.path.dirname(__file__), '..',
                   'AethonMod', 'Content', 'Weapons', 'Cosmic',
                   'OlvidoBlackHoleStaff.png')


def build():
    yy, xx = np.mgrid[0:OH, 0:OW].astype(float)

    img = np.zeros((OH, OW, 4), dtype=float)

    # ---------------- EL BASTÓN (diagonal, madera oscura-violeta) --------
    ax, ay = 6.0 * SS, 27.0 * SS
    bx, by = 16.5 * SS, 6.5 * SS
    dx, dy = bx - ax, by - ay
    L = np.hypot(dx, dy)
    ux, uy = dx / L, dy / L           # dirección del bastón
    nx_, ny_ = -uy, ux                # normal

    px = xx - ax
    py = yy - ay
    along = px * ux + py * uy         # proyección sobre el eje
    perp = px * nx_ + py * ny_        # distancia lateral al eje

    t = np.clip(along / L, 0, 1)      # 0 base → 1 punta
    halfw = (2.1 - 0.9 * t) * SS      # se afina hacia la punta
    staff = np.abs(perp) <= halfw
    inseg = (along >= -0.5 * SS) & (along <= L + 0.5 * SS)

    base_r = 62 + 26 * (1 - t)
    base_g = 38 + 14 * (1 - t)
    base_b = 58 + 34 * (1 - t)
    edge = np.abs(perp) > (halfw - 0.9 * SS)
    staff_r = np.where(edge, base_r * 0.45, base_r)
    staff_g = np.where(edge, base_g * 0.45, base_g)
    staff_b = np.where(edge, base_b * 0.45, base_b)
    vein = 0.5 + 0.5 * np.sin(along / (2.2 * SS) * np.pi)
    staff_r = staff_r * (0.9 + 0.14 * vein)
    staff_g = staff_g * (0.9 + 0.10 * vein)
    staff_b = staff_b * (0.9 + 0.16 * vein)

    m_staff = staff & inseg
    img[..., 0] = np.where(m_staff, staff_r, img[..., 0])
    img[..., 1] = np.where(m_staff, staff_g, img[..., 1])
    img[..., 2] = np.where(m_staff, staff_b, img[..., 2])
    img[..., 3] = np.where(m_staff, 255, img[..., 3])

    # ---------------- EL MINI-AGUJERO (sobre la punta) -------------------
    cx, cy = 19.5 * SS, 9.5 * SS
    r = np.hypot(xx - cx, yy - cy)

    R = 4.6 * SS                      # radio del horizonte negro

    # --- 1. halo exterior (aura violeta tenue) ---
    halo = np.exp(-((r - 1.45 * R) / (0.85 * R)) ** 2)
    a_halo = halo * 90
    better_halo = a_halo > img[..., 3]
    img[..., 0] = np.where(better_halo, 150, img[..., 0])
    img[..., 1] = np.where(better_halo, 70, img[..., 1])
    img[..., 2] = np.where(better_halo, 200, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_halo)

    # --- 2. anillo de plasma (elipse achatada, gradiente térmico) ---
    ring_hi = 1.75 * R
    ex = (xx - cx) / ring_hi
    ey = (yy - cy) / (ring_hi * 0.72)
    re = np.hypot(ex, ey)
    ring_band = np.exp(-((re - 1.0) / 0.28) ** 2)

    th = np.arctan2(ey, ex)
    hot = 0.5 + 0.5 * np.cos(th - 0.6)
    hot2 = hot * hot

    ring_r = 170 + 85 * hot2           # violeta → casi blanco
    ring_g = 60 + 180 * hot2           # → rosa caliente
    ring_b = 200 + 55 * hot2           # violeta → blanco-lila
    a_ring = ring_band * 255

    m_ring = a_ring > img[..., 3]
    img[..., 0] = np.where(m_ring, ring_r, img[..., 0])
    img[..., 1] = np.where(m_ring, ring_g, img[..., 1])
    img[..., 2] = np.where(m_ring, ring_b, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_ring)

    # --- 3. el horizonte negro (disco sólido) ---
    disk = r <= R
    img[..., 0] = np.where(disk, 8, img[..., 0])
    img[..., 1] = np.where(disk, 3, img[..., 1])
    img[..., 2] = np.where(disk, 12, img[..., 2])
    img[..., 3] = np.where(disk, 255, img[..., 3])

    # --- 4. runa dorada (cruz angular a la izquierda del agujero) ---
    rx, ry = 11.5 * SS, 13.5 * SS
    rcx = xx - rx
    rcy = yy - ry
    rune = ((np.abs(rcx) <= 0.85 * SS) & (np.abs(rcy) <= 2.4 * SS)) | \
           ((np.abs(rcy) <= 0.85 * SS) & (np.abs(rcx) <= 2.4 * SS))
    img[..., 0] = np.where(rune, 255, img[..., 0])
    img[..., 1] = np.where(rune, 205, img[..., 1])
    img[..., 2] = np.where(rune, 95, img[..., 2])
    img[..., 3] = np.where(rune, 255, img[..., 3])

    # --- 5. destello de 4 puntas (la perla arcana dorada) ---------------
    sx, sy = 23.5 * SS, 5.5 * SS
    dxs = np.abs(xx - sx)
    dys = np.abs(yy - sy)
    star_v = np.exp(-(dxs / (1.6 * SS)) ** 2) * np.exp(-(dys / (0.42 * SS)) ** 2)
    star_h = np.exp(-(dys / (1.6 * SS)) ** 2) * np.exp(-(dxs / (0.42 * SS)) ** 2)
    star = np.maximum(star_v, star_h)
    m_star = star > 0.25
    img[..., 0] = np.where(m_star, 255, img[..., 0])
    img[..., 1] = np.where(m_star, 235, img[..., 1])
    img[..., 2] = np.where(m_star, 190, img[..., 2])
    img[..., 3] = np.where(m_star, 255, img[..., 3])

    # ---------------- recorte y downsample ----------------
    img = np.clip(img, 0, 255).astype(np.uint8)
    out = Image.fromarray(img)
    out = out.resize((W, H), Image.LANCZOS)

    a = np.array(out)
    a[..., 3][a[..., 3] < 28] = 0
    out = Image.fromarray(a)
    out.save(OUT)
    print('OK', OUT, out.size, out.mode)

    b = np.array(out)
    print('disco(pxm):', b[10, 20], ' esquina:', b[1, 1])


if __name__ == '__main__':
    build()
