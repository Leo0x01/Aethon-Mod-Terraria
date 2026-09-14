#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_rune_suns_v619.py — v6.19

LAS TEXTURAS DE LA FAMILIA DE LOS SOLES RÚNICOS + EL CETRO DEL TRUENO:

  1. SOMBRAS de proyectil (76×76, la lección v6.14.1: tModLoader
     auto-requesta la textura por defecto de TODO ModProjectile — sin
     este PNG el mod entero se desactiva al cargar):
       · RuneSunProjectile.png  — disco solar cálido + un anillo rúnico.
       · RunicLightning.png     — chispa eléctrica núcleo-blanco + halo oro.

  2. ICONOS de arma (28×30, patrón del OlvidoStaff: bastón diagonal +
     cabeza, supersampling 8×):
       · SolRunico1Staff.png .. SolRunico10Staff.png — bastón dorado con
         cabeza solar y N ANILLOS elípticos con inclinaciones ALTERNAS
         (la miniatura del concepto real: N anillos, direcciones opuestas).
       · StormRuneStaff.png — bastón con NÚCLEO ELÉCTRICO blanco y un
         rayo dentado dorado cruzándolo.
"""

import os
import numpy as np
from PIL import Image

HERE = os.path.dirname(__file__)
PROJ_DIR = os.path.join(HERE, '..', 'AethonMod')
PROJ_OUT = os.path.join(PROJ_DIR, 'Content', 'Projectiles', 'Cosmic')
WEAP_OUT = os.path.join(PROJ_DIR, 'Content', 'Weapons', 'Cosmic')

SS = 8  # supersampling de los iconos


# =====================================================================
#  1. LAS SOMBRAS DE PROYECTIL (76×76)
# =====================================================================

def make_sun_shadow():
    """Disco solar cálido (núcleo blanco-amarillo → dorado → fade) + UN
    anillo rúnico tenue — la identidad de la familia visible aun sin el
    renderer (y el PNG que tML exige al cargar)."""
    size = 76
    c = size / 2.0
    yy, xx = np.mgrid[0:size, 0:size]
    r = np.sqrt((xx - c + 0.5) ** 2 + (yy - c + 0.5) ** 2)

    img = np.zeros((size, size, 4), dtype=float)

    # --- el disco solar: núcleo sólido → dorado → fade radial ---
    core_r, glow_r = 13.0, 33.0
    # núcleo (blanco-amarillo, casi sólido)
    core = np.clip(1.0 - r / core_r, 0, 1) ** 0.65
    # halo dorado (cae suave)
    halo = np.clip(1.0 - (r - core_r * 0.4) / glow_r, 0, 1) ** 1.8
    halo = np.where(r > core_r * 0.4, halo, 0.0)

    # color: núcleo (255,250,225) → dorado (255,190,80) → ámbar (255,130,40)
    t_col = np.clip((r - 4.0) / (glow_r - 4.0), 0, 1)
    col = np.empty((size, size, 3), dtype=float)
    for k, (c0, c1, c2) in enumerate(((255, 250, 225), (255, 190, 80), (255, 130, 40))):
        a = np.array(c0, dtype=float)
        b = np.array(c1, dtype=float)
        cc = np.array(c2, dtype=float)
        # dos tramos: 0..0.35 núcleo→oro, 0.35..1 oro→ámbar
        t1 = np.clip(t_col / 0.35, 0, 1)
        t2 = np.clip((t_col - 0.35) / 0.65, 0, 1)
        col[..., k] = a * (1 - t1) + b * t1
        col[..., k] = np.where(t_col > 0.35, b * (1 - t2) + cc * t2, col[..., k])

    alpha = np.clip(core * 255 * 0.92 + halo * 130, 0, 255)
    img[..., 0:3] = col
    img[..., 3] = alpha

    # --- EL ANILLO RÚNICO (firma de la familia): elipse inclinada ---
    # r' respecto de la elipse inclinada (a=27, b=13, tilt=24°)
    a_e, b_e, tilt = 27.0, 13.0, np.deg2rad(24.0)
    dx, dy = xx - c + 0.5, yy - c + 0.5
    ct, st = np.cos(tilt), np.sin(tilt)
    ux = dx * ct + dy * st
    uy = -dx * st + dy * ct
    r_ell = np.sqrt((ux / a_e) ** 2 + (uy / b_e) ** 2)
    ring_band = np.exp(-((r_ell - 1.0) / 0.055) ** 2)   # banda fina en r'=1
    ring_alpha = ring_band * 92.0
    # color del aro: oro vivo (255,205,110)
    for k, target in enumerate((255, 205, 110)):
        w = np.clip(ring_band, 0, 1) * 0.9
        img[..., k] = img[..., k] * (1 - w) + target * w
    img[..., 3] = np.clip(img[..., 3] + ring_alpha, 0, 255)

    img[r > 37.5, 3] = 0
    return Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), 'RGBA')


def make_lightning_shadow():
    """Chispa eléctrica: núcleo blanco elongado + halo dorado + mini
    zigzag — la identidad del rayo del cetro."""
    size = 76
    c = size / 2.0
    yy, xx = np.mgrid[0:size, 0:size]

    img = np.zeros((size, size, 4), dtype=float)

    # --- el halo dorado (elipse amplia horizontal) ---
    r_h = np.sqrt(((xx - c + 0.5) / 30.0) ** 2 + ((yy - c + 0.5) / 17.0) ** 2)
    halo = np.exp(-(r_h ** 2.2) * 2.6)
    img[..., 0] = halo * 255 * 0.62
    img[..., 1] = halo * 205 * 0.62
    img[..., 2] = halo * 110 * 0.62
    img[..., 3] = halo * 110

    # --- EL ZIGZAG (la firma del rayo): polilínea dentada central ---
    pts = [(c - 26, c + 2), (c - 15, c - 9), (c - 6, c + 4),
           (c + 5, c - 6), (c + 14, c + 5), (c + 26, c - 2)]
    zig = np.zeros((size, size), dtype=float)
    for (x0, y0), (x1, y1) in zip(pts[:-1], pts[1:]):
        # distancia de cada píxel al segmento (aprox por muestreo denso)
        for t in np.linspace(0, 1, 80):
            px, py = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            d = np.sqrt((xx - px) ** 2 + (yy - py) ** 2)
            zig = np.maximum(zig, np.exp(-(d / 2.6) ** 2) * 0.9)
    # el rayo: funda oro + núcleo blanco
    core = np.exp(-((np.sqrt((xx - c) ** 2 + (yy - c) ** 2) * 0) + 0) * 0)  # placeholder
    narrow = np.maximum(zig * (np.exp(-(0 ** 2))), 0)
    # núcleo: zigzag más fino (re-muestrear con radio menor)
    corez = np.zeros((size, size), dtype=float)
    for (x0, y0), (x1, y1) in zip(pts[:-1], pts[1:]):
        for t in np.linspace(0, 1, 80):
            px, py = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            d = np.sqrt((xx - px) ** 2 + (yy - py) ** 2)
            corez = np.maximum(corez, np.exp(-(d / 1.1) ** 2))

    # funda del rayo (oro) sobre el halo
    img[..., 0] = np.maximum(img[..., 0], zig * 255 * 0.85)
    img[..., 1] = np.maximum(img[..., 1], zig * 205 * 0.85)
    img[..., 2] = np.maximum(img[..., 2], zig * 110 * 0.85)
    # núcleo del rayo (blanco)
    img[..., 0] = np.maximum(img[..., 0], corez * 255)
    img[..., 1] = np.maximum(img[..., 1], corez * 250)
    img[..., 2] = np.maximum(img[..., 2], corez * 235)
    img[..., 3] = np.clip(np.maximum(img[..., 3], zig * 235 + corez * 60), 0, 255)

    # recorte exterior suave
    r_out = np.sqrt((xx - c + 0.5) ** 2 + (yy - c + 0.5) ** 2)
    img[r_out > 37.5, 3] = 0
    return Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), 'RGBA')


# =====================================================================
#  2. LOS ICONOS (28×30, SS 8×)
# =====================================================================

W, H = 28, 30
OW, OH = W * SS, H * SS


def staff_base(img, gold_tone=True):
    """El BASTÓN diagonal (patrón del OlvidoStaff — tono dorado solar)."""
    ax, ay = 6.0 * SS, 27.0 * SS
    bx, by = 16.5 * SS, 6.5 * SS
    dx, dy = bx - ax, by - ay
    L = np.hypot(dx, dy)
    ux, uy = dx / L, dy / L
    nx_, ny_ = -uy, ux

    yy, xx = np.mgrid[0:OH, 0:OW]
    px, py = xx - ax, yy - ay
    along = px * ux + py * uy
    perp = px * nx_ + py * ny_

    t = np.clip(along / L, 0, 1)
    halfw = (2.1 - 0.9 * t) * SS
    staff = np.abs(perp) <= halfw
    inseg = (along >= -0.5 * SS) & (along <= L + 0.5 * SS)

    if gold_tone:
        base_r = 96 + 34 * (1 - t)
        base_g = 66 + 26 * (1 - t)
        base_b = 30 + 22 * (1 - t)
    else:
        base_r = 58 + 24 * (1 - t)
        base_g = 56 + 22 * (1 - t)
        base_b = 78 + 30 * (1 - t)
    edge = np.abs(perp) > (halfw - 0.9 * SS)
    staff_r = np.where(edge, base_r * 0.45, base_r)
    staff_g = np.where(edge, base_g * 0.45, base_g)
    staff_b = np.where(edge, base_b * 0.45, base_b)
    vein = 0.5 + 0.5 * np.sin(along / (2.2 * SS) * np.pi)
    staff_r *= (0.9 + 0.14 * vein)
    staff_g *= (0.9 + 0.10 * vein)
    staff_b *= (0.9 + 0.16 * vein)

    m = staff & inseg
    img[..., 0] = np.where(m, staff_r, img[..., 0])
    img[..., 1] = np.where(m, staff_g, img[..., 1])
    img[..., 2] = np.where(m, staff_b, img[..., 2])
    img[..., 3] = np.where(m, 255, img[..., 3])


def solar_head(img, cx, cy, R, tier=1):
    """La CABEZA SOLAR: disco radial cálido (núcleo blanco → dorado)."""
    yy, xx = np.mgrid[0:OH, 0:OW]
    r = np.hypot(xx - cx, yy - cy)

    # halo exterior cálido
    halo = np.exp(-((r / (R * 1.75)) ** 2) * 2.4)
    # cuerpo dorado
    body = np.clip(1 - r / (R * 1.12), 0, 1) ** 1.5
    # núcleo blanco
    core = np.clip(1 - r / (R * 0.55), 0, 1) ** 1.2

    # el tier sube el BRILLO del núcleo (la copia 10 arde)
    core_boost = 1.0 + 0.05 * (tier - 1)

    add_r = halo * 255 * 0.30 + body * 255 * 0.72 + core * 255 * 0.95 * core_boost
    add_g = halo * 195 * 0.30 + body * 195 * 0.72 + core * 248 * 0.95
    add_b = halo * 110 * 0.30 + body * 100 * 0.72 + core * 215 * 0.95
    add_a = halo * 120 + body * 235 + core * 255

    for k, layer in enumerate((add_r, add_g, add_b)):
        img[..., k] = np.minimum(np.maximum(img[..., k], layer), 255)
    img[..., 3] = np.minimum(np.maximum(img[..., 3], np.clip(add_a, 0, 255)), 255)


def rune_rings(img, cx, cy, n, tier):
    """N ANILLOS elípticos con INCLINACIONES alternas — la miniatura del
    concepto real (cada anillo en su propio plano, giro alterno)."""
    yy, xx = np.mgrid[0:OH, 0:OW]
    dx, dy = xx - cx, yy - cy

    for k in range(n):
        a = (4.6 + 0.50 * k) * SS          # semieje mayor
        b = a * (0.42 + 0.05 * (k % 3))    # achatado variable
        tilt = np.deg2rad(90.0 + 28.0 * k - 8.0 * (k % 2))  # planos distintos
        ct, st = np.cos(tilt), np.sin(tilt)
        ux = dx * ct + dy * st
        uy = -dx * st + dy * ct
        r_ell = np.sqrt((ux / a) ** 2 + (uy / b) ** 2)

        band = np.exp(-((r_ell - 1.0) / 0.028) ** 2)

        # el color del anillo: ORO / BLANCO alternos, AZUL cada 3º desde el 7
        if tier >= 7 and k % 3 == 2:
            cr, cg, cb = 135, 165, 255
        elif k % 2 == 0:
            cr, cg, cb = 255, 200, 95
        else:
            cr, cg, cb = 255, 245, 220

        w = np.clip(band, 0, 1)
        img[..., 0] = img[..., 0] * (1 - w) + cr * w
        img[..., 1] = img[..., 1] * (1 - w) + cg * w
        img[..., 2] = img[..., 2] * (1 - w) + cb * w
        img[..., 3] = np.clip(np.maximum(img[..., 3], band * 235), 0, 255)


def electric_head(img, cx, cy, R):
    """La CABEZA ELÉCTRICA del cetro: núcleo blanco-azul + rayo dentado."""
    yy, xx = np.mgrid[0:OH, 0:OW]
    r = np.hypot(xx - cx, yy - cy)

    halo = np.exp(-((r / (R * 1.8)) ** 2) * 2.4)
    core = np.clip(1 - r / (R * 0.85), 0, 1) ** 1.3

    # el mini rayo dentado (firma del arma)
    zig = np.zeros((OH, OW), dtype=float)
    pts = [(cx - R * 1.15, cy + R * 0.45), (cx - R * 0.35, cy - R * 0.55),
           (cx + R * 0.25, cy + R * 0.35), (cx + R * 1.15, cy - R * 0.45)]
    for (x0, y0), (x1, y1) in zip(pts[:-1], pts[1:]):
        for t in np.linspace(0, 1, 60):
            px, py = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            d = np.sqrt((xx - px) ** 2 + (yy - py) ** 2)
            zig = np.maximum(zig, np.exp(-(d / (1.4 * SS)) ** 2))

    add_r = halo * 150 * 0.35 + core * 235 * 0.9 + zig * 255 * 0.9
    add_g = halo * 180 * 0.35 + core * 240 * 0.9 + zig * 220 * 0.9
    add_b = halo * 255 * 0.35 + core * 255 * 0.9 + zig * 140 * 0.9
    add_a = halo * 130 + core * 240 + zig * 240

    for k, layer in enumerate((add_r, add_g, add_b)):
        img[..., k] = np.minimum(np.maximum(img[..., k], layer), 255)
    img[..., 3] = np.minimum(np.maximum(img[..., 3], np.clip(add_a, 0, 255)), 255)


def finish_icon(img, name):
    """Downsample SS× → 28×30 y guarda."""
    out = img.reshape(H, SS, W, SS, 4).mean(axis=(1, 3))
    out = np.clip(out, 0, 255).astype(np.uint8)
    # alpha binarizado suave (look Terraria nítido)
    a = out[..., 3].astype(float)
    a = np.where(a < 24, 0, np.where(a > 200, 255, a))
    out[..., 3] = a.astype(np.uint8)
    path = os.path.join(WEAP_OUT, name)
    Image.fromarray(out, 'RGBA').save(path)
    print('OK', path, out.shape)


# =====================================================================
#  MAIN
# =====================================================================

def main():
    os.makedirs(PROJ_OUT, exist_ok=True)
    os.makedirs(WEAP_OUT, exist_ok=True)

    # === 1. SOMBRAS ===
    sun_shadow = make_sun_shadow()
    p1 = os.path.join(PROJ_OUT, 'RuneSunProjectile.png')
    sun_shadow.save(p1)
    print('OK', p1, sun_shadow.size)

    bolt_shadow = make_lightning_shadow()
    p2 = os.path.join(PROJ_OUT, 'RunicLightning.png')
    bolt_shadow.save(p2)
    print('OK', p2, bolt_shadow.size)

    # verificación (invariantes del patrón v6.14.1)
    for name, path in (('SOL RÚNICO', p1), ('RAYO', p2)):
        a = np.array(Image.open(path).convert('RGBA'))
        corner = a[1, 1]
        n_visible = (a[:, :, 3] > 0).sum()
        assert corner[3] == 0, f'{name}: esquina debe ser transparente'
        assert 1500 < n_visible < 6500, f'{name}: masa visible rara: {n_visible}'
        print(f'  {name}: esquina_alpha={corner[3]} px_visibles={n_visible} ✓')

    # === 2. ICONOS DE LOS 10 SOLES ===
    for tier in range(1, 11):
        img = np.zeros((OH, OW, 4), dtype=float)
        staff_base(img, gold_tone=True)
        cx, cy = 18.5 * SS, 10.5 * SS
        solar_head(img, cx, cy, 4.6 * SS, tier)
        rune_rings(img, cx, cy, tier, tier)
        finish_icon(img, f'SolRunico{tier}Staff.png')

    # === 3. ICONO DEL CETRO DEL TRUENO ===
    img = np.zeros((OH, OW, 4), dtype=float)
    staff_base(img, gold_tone=False)
    electric_head(img, 18.5 * SS, 10.5 * SS, 5.0 * SS)
    finish_icon(img, 'StormRuneStaff.png')

    print('\nTODO GENERADO ✓')


if __name__ == '__main__':
    main()
