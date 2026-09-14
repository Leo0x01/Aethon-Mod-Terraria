#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_lumen_v622.py — v6.22

LAS TEXTURAS DE LA LUZ Y DEL FUEGO — nacidas de la investigación profunda
de las técnicas de luz del ecosistema (bloom apilado en capas invertidas,
lanzas de doble pasada, destellos de 4 puntas, rayos de luz estirados) y
re-creadas 100% aquí con numpy.

Convención de la casa: RGB = BLANCO (255) y el PERFIL vive en el canal
ALFA (así el tinte del quad colorea y la intensidad modular el alpha).

  1. LumenBloom.png  (200×200) — EL BLOOM UNIVERSAL: núcleo sólido ~12%
     + caída gaussiana ancha (σ≈0.30) — una sola textura para TODA la
     luz del mod (la lección nº2 de la investigación).
  2. LumenBlade.png  (64×256) — LA LANZA DE LUZ: núcleo caliente fino +
     cuerpo suave, punta afilada (taper) — doble pasada color+blanco.
  3. LumenFlare.png  (128×128) — EL DESTELLO DE 4 PUNTAS: cruz principal
     + cruz diagonal secundaria ×0.45 + punto caliente central.
  4. FlameBrush.png  (24×36) — LA CELDA DE FUEGO: puf gaussiano alargado
     vertical, masa baja (el fuego pesa abajo) — la unidad del campo de
     intensidades del fuego procedural.
  5. SOMBRAS de proyectil (76×76, la lección v6.14.1):
       · EclipsePrimordialProjectile.png — disco negro + anillo de fotones
         dorado + aro rúnico + rayos.
  6. ICONOS (28×30, SS 8×, patrón del SolRunico):
       · SolRunico11Staff.png .. SolRunico20Staff.png — N anillos elípticos
         (11..20), con acentos PRISMÁTICOS desde el 17.
       · EclipsePrimordialStaff.png — bastón oscuro + cabeza eclipse.
  7. ICONOS de cosmético (30×30):
       · FireVeilItem.png     — la llama envolvente.
       · RuneRingCrownItem.png — los anillos rúnicos cruzados.
       · RunicHaloWings.png   — el anillo-halo rúnico (+ su _Wings.png
         8×8 TOTALMENTE TRANSPARENTE, el truco del sprite en blanco).
"""

import os
import numpy as np
from PIL import Image

HERE = os.path.dirname(__file__)
PROJ_DIR = os.path.join(HERE, '..', 'AethonMod')
PROC_OUT = os.path.join(PROJ_DIR, 'Content', 'Effects', 'Procedural')
PROJ_OUT = os.path.join(PROJ_DIR, 'Content', 'Projectiles', 'Cosmic')
WEAP_OUT = os.path.join(PROJ_DIR, 'Content', 'Weapons', 'Cosmic')
COSM_OUT = os.path.join(PROJ_DIR, 'Content', 'Items', 'Cosmetics')
WING_OUT = os.path.join(PROJ_DIR, 'Content', 'Items', 'Wings')

SS = 8  # supersampling de los iconos


def save_profile(arr, name):
    """RGB blanco + alpha = perfil (convención del proyecto)."""
    h, w = arr.shape
    img = np.zeros((h, w, 4), dtype=np.uint8)
    img[..., 0:3] = 255
    img[..., 3] = np.clip(arr * 255.0, 0, 255).astype(np.uint8)
    path = os.path.join(PROC_OUT, name)
    Image.fromarray(img, 'RGBA').save(path)
    print(f"  {name}: {w}x{h} alpha[min={arr.min():.3f} max={arr.max():.3f} media={arr.mean():.3f}]")


# =====================================================================
#  1. LumenBloom — EL BLOOM UNIVERSAL (200×200)
# =====================================================================

def make_lumen_bloom():
    size = 200
    c = size / 2.0
    yy, xx = np.mgrid[0:size, 0:size]
    r = np.sqrt((xx - c + 0.5) ** 2 + (yy - c + 0.5) ** 2) / c  # 0..1+

    # núcleo sólido (12% central) + falda gaussiana ancha (σ≈0.30)
    core = np.clip(1.0 - r / 0.13, 0, 1) ** 0.8
    skirt = np.exp(-((r / 0.30) ** 2)) * 0.85
    # leve anillo de "energía" al 35% (el borde vivo de la luz)
    ring = np.exp(-(((r - 0.34) / 0.09) ** 2)) * 0.10
    alpha = np.maximum(np.maximum(core, skirt), ring)
    return alpha


# =====================================================================
#  2. LumenBlade — LA LANZA DE LUZ (64×256)
# =====================================================================

def make_lumen_blade():
    w, h = 64, 256
    yy, xx = np.mgrid[0:h, 0:w]
    u = (xx - w / 2.0 + 0.5) / (w / 2.0)     # -1..1横向
    v = 1.0 - yy / (h - 1.0)                  # 0 abajo (base) → 1 arriba (punta)

    # TAPER: la hoja se abre desde la punta (arriba estrecho, abajo ancho).
    taper = 0.22 + 0.78 * (1.0 - v) ** 0.7
    uu = u / np.maximum(taper, 0.05)

    # NÚCLEO caliente (la vena blanca central, fina).
    core = np.exp(-((uu / 0.16) ** 2))
    # CUERPO suave (la funda de color).
    body = np.exp(-((uu / 0.46) ** 2)) * 0.62
    # filos: dos líneas finas a ±0.62 del ancho (el corte de la hoja).
    edge = np.exp(-(((np.abs(uu) - 0.66) / 0.07) ** 2)) * 0.30

    alpha = np.maximum(np.maximum(core, body), edge)

    # La PUNTA se afila (fade superior) y la BASE respira (fade inferior suave).
    tipFade = np.clip(v / 0.10, 0, 1) ** 0.5
    baseFade = np.clip((1.0 - v) / 0.06, 0, 1) ** 0.5 * 0.25 + 0.75
    alpha = alpha * tipFade * baseFade

    # vetas verticales sutiles (la luz CORRE por dentro de la lanza).
    vein = 0.94 + 0.06 * np.sin(v * np.pi * 14.0 + u * 2.0)
    alpha = alpha * vein
    return alpha


# =====================================================================
#  3. LumenFlare — EL DESTELLO DE 4 PUNTAS (128×128)
# =====================================================================

def make_lumen_flare():
    size = 128
    c = size / 2.0
    yy, xx = np.mgrid[0:size, 0:size]
    x = (xx - c + 0.5) / c
    y = (yy - c + 0.5) / c

    # CRUZ PRINCIPAL: brazos horizontales/verticales con caída a lo largo.
    def arm(dx, dy, along_fall=2.6, thin=0.055):
        d_perp = np.abs(-dy if False else 0)
        return d_perp

    # horizontal arm: |y| pequeño, decae con |x|
    armH = np.exp(-((y / 0.055) ** 2)) * np.exp(-((np.abs(x) / 0.92) ** 2) * along_fall if False else 0)
    # (implementado directamente, sin closure para claridad)
    armH = np.exp(-((y / 0.052) ** 2)) * (1.0 - np.clip(np.abs(x), 0, 1)) ** 2.2
    armV = np.exp(-((x / 0.052) ** 2)) * (1.0 - np.clip(np.abs(y), 0, 1)) ** 2.2
    # CRUZ DIAGONAL secundaria (×0.45, más ancha y corta).
    d1 = (x + y) / np.sqrt(2.0)
    d2 = (x - y) / np.sqrt(2.0)
    armD = np.exp(-((d2 / 0.085) ** 2)) * (1.0 - np.clip(np.abs(d1), 0, 1)) ** 3.0 * 0.45
    armD2 = np.exp(-((d1 / 0.085) ** 2)) * (1.0 - np.clip(np.abs(d2), 0, 1)) ** 3.0 * 0.45
    # PUNTO CALIENTE central.
    r = np.sqrt(x ** 2 + y ** 2)
    hot = np.clip(1.0 - r / 0.16, 0, 1) ** 1.1
    halo = np.exp(-((r / 0.42) ** 2)) * 0.22

    alpha = np.maximum(np.maximum(np.maximum(armH, armV),
                                  np.maximum(armD, armD2)),
                       np.maximum(hot, halo * 0.5 + hot))
    alpha = np.clip(alpha, 0, 1)
    return alpha


# =====================================================================
#  4. FlameBrush — LA CELDA DE FUEGO (24×36)
# =====================================================================

def make_flame_brush():
    w, h = 24, 36
    yy, xx = np.mgrid[0:h, 0:w]
    # centro de masa BAJO (el fuego pesa hacia la base) + cola suave arriba.
    cx = (w - 1) / 2.0
    cy = h * 0.62
    u = (xx - cx + 0.5) / (w * 0.5)
    v = (yy - cy + 0.5) / (h * 0.5)

    # puf elíptico: ancho arriba y abajo distinto (lengua de llama).
    widening = 1.0 + 0.35 * np.clip(-v, 0, 1)          # arriba se estrecha menos… no: arriba ESTRECHO
    widening = 1.0 - 0.30 * np.clip(v, 0, 1) + 0.10 * np.clip(-v, 0, 1)
    uu = u / np.maximum(widening, 0.4)

    body = np.exp(-((uu ** 2) * 2.1 + (v ** 2) * 1.45))
    # núcleo caliente bajo (la brasa de la celda).
    hot = np.exp(-((uu ** 2) * 3.4 + ((v + 0.22) ** 2) * 2.6)) * 1.0
    alpha = np.maximum(body * 0.85, hot * 0.95)
    return np.clip(alpha, 0, 1)


# =====================================================================
#  5. LA SOMBRA DEL PROYECTIL ECLIPSE (76×76)
# =====================================================================

def make_eclipse_shadow():
    size = 76
    c = size / 2.0
    yy, xx = np.mgrid[0:size, 0:size]
    r = np.sqrt((xx - c + 0.5) ** 2 + (yy - c + 0.5) ** 2)

    img = np.zeros((size, size, 4), dtype=float)

    # --- el vacío: disco NEGRO absoluto ---
    black = np.clip(1.0 - r / 16.0, 0, 1) ** 0.4
    img[..., 0] = np.maximum(img[..., 0], black * 14)
    img[..., 1] = np.maximum(img[..., 1], black * 8)
    img[..., 2] = np.maximum(img[..., 2], black * 26)
    img[..., 3] = np.maximum(img[..., 3], black * 255)

    # --- el ANILLO DE FOTONES dorado abrazando la sombra ---
    ring = np.exp(-(((r - 20.0) / 2.6) ** 2))
    img[..., 0] = np.maximum(img[..., 0], ring * 255 * 0.95)
    img[..., 1] = np.maximum(img[..., 1], ring * 205 * 0.95)
    img[..., 2] = np.maximum(img[..., 2], ring * 90 * 0.95)
    img[..., 3] = np.maximum(img[..., 3], ring * 245)

    # --- el ARO RÚNICO exterior (elipse inclinada, la herencia solar) ---
    dx, dy = xx - c, yy - c
    tilt = np.deg2rad(-24.0)
    ct, st = np.cos(tilt), np.sin(tilt)
    ux = dx * ct + dy * st
    uy = -dx * st + dy * ct
    r_ell = np.sqrt((ux / 31.0) ** 2 + (uy / 11.0) ** 2)
    rune = np.exp(-((r_ell - 1.0) / 0.06) ** 2) * 0.85
    img[..., 0] = np.maximum(img[..., 0], rune * 255)
    img[..., 1] = np.maximum(img[..., 1], rune * 245)
    img[..., 2] = np.maximum(img[..., 2], rune * 215)
    img[..., 3] = np.maximum(img[..., 3], rune * 225)

    # --- RAYOS violeta-azul escapando (la tormenta interior) ---
    for i, ang in enumerate(np.linspace(0, 2 * np.pi, 7, endpoint=False)):
        if i % 2 == 0:
            continue
        for t in np.linspace(0.32, 0.92, 40):
            px, py = c + np.cos(ang) * t * 36.0, c + np.sin(ang) * t * 36.0 * 0.8
            d = np.sqrt((xx - px) ** 2 + (yy - py) ** 2)
            wgt = (1.0 - t) * 0.8 + 0.2
            g = np.exp(-((d / 1.8) ** 2)) * wgt
            img[..., 0] = np.maximum(img[..., 0], g * 170)
            img[..., 1] = np.maximum(img[..., 1], g * 120)
            img[..., 2] = np.maximum(img[..., 2], g * 255)
            img[..., 3] = np.maximum(img[..., 3], g * 200)

    # --- halo dorado tenue de fondo ---
    halo = np.exp(-((r / 33.0) ** 2) * 2.6)
    img[..., 0] = np.maximum(img[..., 0], halo * 120)
    img[..., 1] = np.maximum(img[..., 1], halo * 85)
    img[..., 2] = np.maximum(img[..., 2], halo * 40)
    img[..., 3] = np.maximum(img[..., 3], halo * 90)

    # recorte exterior suave
    r_out = np.sqrt((xx - c + 0.5) ** 2 + (yy - c + 0.5) ** 2)
    img[r_out > 37.5, 3] = 0
    return Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), 'RGBA')


# =====================================================================
#  6. LOS ICONOS DE BASTÓN (28×30, SS 8×)
# =====================================================================

W, H = 28, 30
OW, OH = W * SS, H * SS


def staff_base(img, gold_tone=True, dark_tone=False):
    """El BASTÓN diagonal (patrón del OlvidoStaff)."""
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

    if dark_tone:
        base_r = 44 + 20 * (1 - t)
        base_g = 34 + 16 * (1 - t)
        base_b = 62 + 30 * (1 - t)
    elif gold_tone:
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
    """La CABEZA SOLAR: disco radial cálido (núcleo blanco → dorado).
    v6.22: la SEGUNDA DÉCADA progresa — núcleo MÁS BLANCO Y CEGADOR con
    el tier (19-20 vira al blanco-azulado celestial)."""
    yy, xx = np.mgrid[0:OH, 0:OW]
    r = np.hypot(xx - cx, yy - cy)

    halo = np.exp(-((r / (R * 1.75)) ** 2) * 2.4)
    body = np.clip(1 - r / (R * 1.12), 0, 1) ** 1.5
    core = np.clip(1 - r / (R * 0.55), 0, 1) ** 1.2

    core_boost = 1.0 + 0.07 * (tier - 1)
    # tier ≥ 19: el núcleo vira al BLANCO-AZUL celestial; ≥ 17 al blanco puro.
    core_g, core_b = 248, 215
    if tier >= 19:
        core_g, core_b = 252, 250
    elif tier >= 17:
        core_g, core_b = 250, 235

    add_r = halo * 255 * 0.30 + body * 255 * 0.72 + core * 255 * 0.95 * core_boost
    add_g = halo * 195 * 0.30 + body * 195 * 0.72 + core * core_g * 0.95
    add_b = halo * 110 * 0.30 + body * 100 * 0.72 + core * core_b * 0.95
    add_a = halo * 120 + body * 235 + core * 255

    for k, layer in enumerate((add_r, add_g, add_b)):
        img[..., k] = np.minimum(np.maximum(img[..., k], layer), 255)
    img[..., 3] = np.minimum(np.maximum(img[..., 3], np.clip(add_a, 0, 255)), 255)


def rune_rings(img, cx, cy, n, tier):
    """N ANILLOS elípticos con inclinaciones alternas. v6.22: packing
    más TIGHTO para caber 11..20 anillos + acentos prismáticos ≥17."""
    yy, xx = np.mgrid[0:OH, 0:OW]
    dx, dy = xx - cx, yy - cy

    step = 0.40 if n <= 12 else (0.35 if n <= 16 else 0.31)
    for k in range(n):
        a = (4.4 + step * k) * SS
        b = a * (0.42 + 0.05 * (k % 3))
        tilt = np.deg2rad(90.0 + 28.0 * k - 8.0 * (k % 2))
        ct, st = np.cos(tilt), np.sin(tilt)
        ux = dx * ct + dy * st
        uy = -dx * st + dy * ct
        r_ell = np.sqrt((ux / a) ** 2 + (uy / b) ** 2)

        band = np.exp(-((r_ell - 1.0) / 0.030) ** 2)

        # el color: ORO / BLANCO alternos, AZUL cada 3º desde el 7 —
        # y PRISMA (violeta→cian) cada 3º desde el 17 (la corona prismática).
        if tier >= 17 and k % 3 == 1:
            cr, cg, cb = 178, 120, 255
        elif tier >= 7 and k % 3 == 2:
            cr, cg, cb = 135, 165, 255
        elif k % 2 == 0:
            cr, cg, cb = 255, 200, 95
        else:
            cr, cg, cb = 255, 245, 220

        # los anillos profundos bajan su alpha (que no empasten el icono).
        a_mult = 1.0 - 0.020 * k
        w = np.clip(band * a_mult, 0, 1)
        img[..., 0] = img[..., 0] * (1 - w) + cr * w
        img[..., 1] = img[..., 1] * (1 - w) + cg * w
        img[..., 2] = img[..., 2] * (1 - w) + cb * w
        img[..., 3] = np.clip(np.maximum(img[..., 3], band * 235 * a_mult), 0, 255)

    # v6.22 (feedback VLM): destellos exteriores desde el 17 — las
    # partículas prismáticas del sistema completo.
    if tier >= 17:
        for i in range(5):
            ang = i / 5.0 * 2 * np.pi + 0.6
            rr = 12.2 * SS
            px, py = cx + np.cos(ang) * rr, cy + np.sin(ang) * rr * 0.9
            d = np.hypot(xx - px, yy - py)
            sp = np.exp(-((d / (0.9 * SS)) ** 2)) * 0.9
            cr2, cg2, cb2 = (255, 245, 220) if i % 2 == 0 else (178, 120, 255)
            w2 = np.clip(sp, 0, 1)
            img[..., 0] = img[..., 0] * (1 - w2) + cr2 * w2
            img[..., 1] = img[..., 1] * (1 - w2) + cg2 * w2
            img[..., 2] = img[..., 2] * (1 - w2) + cb2 * w2
            img[..., 3] = np.clip(np.maximum(img[..., 3], sp * 240), 0, 255)


def eclipse_head(img, cx, cy, R):
    """La CABEZA ECLIPSE: disco negro + anillo de fotones dorado + rayos
    violeta + una chispa solar orbitando."""
    yy, xx = np.mgrid[0:OH, 0:OW]
    dx, dy = xx - cx, yy - cy
    r = np.hypot(dx, dy)

    # halo dorado tenue
    halo = np.exp(-((r / (R * 2.1)) ** 2) * 2.2)
    # disco NEGRO
    black = np.clip(1 - r / (R * 0.92), 0, 1) ** 0.5
    # anillo de fotones dorado
    ring = np.exp(-(((r - R * 1.02) / (R * 0.16)) ** 2))
    # núcleo blanco del anillo (la frontera ardiente)
    ringHot = np.exp(-(((r - R * 1.02) / (R * 0.07)) ** 2))

    # rayos violeta radiando
    rays = np.zeros((OH, OW), dtype=float)
    for ang in np.linspace(0, 2 * np.pi, 9, endpoint=False):
        if int(np.degrees(ang)) % 2 == 0:
            pass
        for t in np.linspace(0.35, 0.98, 30):
            px, py = cx + np.cos(ang) * t * R * 2.0, cy + np.sin(ang) * t * R * 2.0 * 0.85
            d = np.sqrt((xx - px) ** 2 + (yy - py) ** 2)
            rays = np.maximum(rays, np.exp(-((d / (0.85 * SS)) ** 2)) * (1.0 - t * 0.55))

    # el pequeño SOL orbitando (la mitad solar de la fusión)
    sx, sy = cx + R * 1.65, cy - R * 1.30
    rs = np.hypot(xx - sx, yy - sy)
    mini_sun = np.clip(1 - rs / (R * 0.45), 0, 1) ** 1.2

    img[..., 0] = np.maximum(img[..., 0],
                             halo * 120 + ring * 235 + ringHot * 255 + rays * 165 + mini_sun * 255)
    img[..., 1] = np.maximum(img[..., 1],
                             halo * 85 + ring * 190 + ringHot * 245 + rays * 110 + mini_sun * 225)
    img[..., 2] = np.maximum(img[..., 2],
                             halo * 45 + ring * 80 + ringHot * 215 + rays * 255 + mini_sun * 130)
    a = np.maximum(halo * 90 + ring * 240 + ringHot * 255 + rays * 180 + mini_sun * 255,
                   black * 255)
    img[..., 3] = np.minimum(np.maximum(img[..., 3], np.clip(a, 0, 255)), 255)
    # el disco negro PINTA encima (es opaco: el vacío)
    m = black > 0.5
    img[..., 0] = np.where(m & (ring < 0.25), 12, img[..., 0])
    img[..., 1] = np.where(m & (ring < 0.25), 7, img[..., 1])
    img[..., 2] = np.where(m & (ring < 0.25), 22, img[..., 2])
    img[..., 3] = np.where(m & (ring < 0.25), 255, img[..., 3])


def finish_icon(img, name, out_dir=WEAP_OUT):
    """Downsample SS× → 28×30 y guarda."""
    out = img.reshape(H, SS, W, SS, 4).mean(axis=(1, 3))
    out = np.clip(out, 0, 255).astype(np.uint8)
    a = out[..., 3].astype(float)
    a = np.where(a < 24, 0, np.where(a > 200, 255, a))
    out[..., 3] = a.astype(np.uint8)
    path = os.path.join(out_dir, name)
    Image.fromarray(out, 'RGBA').save(path)
    print('OK', path, out.shape)


# =====================================================================
#  7. LOS ICONOS DE COSMÉTICO (30×30, SS 8×)
# =====================================================================

CW, CH = 30, 30
OCW, OCH = CW * SS, CH * SS


def finish_cosm(img, name):
    out = img.reshape(CH, SS, CW, SS, 4).mean(axis=(1, 3))
    out = np.clip(out, 0, 255).astype(np.uint8)
    a = out[..., 3].astype(float)
    a = np.where(a < 24, 0, np.where(a > 200, 255, a))
    out[..., 3] = a.astype(np.uint8)
    path = os.path.join(COSM_OUT, name)
    Image.fromarray(out, 'RGBA').save(path)
    print('OK', path, out.shape)


def make_fire_veil_icon():
    """La llama envolvente: tres lenguas de fuego con núcleo blanco."""
    img = np.zeros((OCH, OCW, 4), dtype=float)
    yy, xx = np.mgrid[0:OCH, 0:OCW]
    c = OCW / 2.0

    # tres lenguas: central alta, laterales inclinadas
    tongues = [
        (0.0, 0.30, 1.00, 1.00),   # (dx_f, altura, ancho, alpha)
        (-0.42, 0.22, 0.72, 0.85),
        (0.40, 0.25, 0.66, 0.80),
        (-0.16, 0.16, 0.5, 0.6),
        (0.18, 0.14, 0.45, 0.55),
    ]
    base_y = OCH * 0.88

    for dx_f, hgt_f, w_f, a_f in tongues:
        tipx = c + dx_f * OCW * 0.9
        tipy = base_y - hgt_f * OCH * 1.35
        # la lengua: catenaria de puntos del borde a la punta
        for t in np.linspace(0, 1, 60):
            # borde izquierdo y derecho suben hacia la punta
            bw = (1.0 - t) ** 0.8 * w_f * OCW * 0.20
            px = tipx * t + c * (1 - t)
            py = tipy * t + base_y * (1 - t)
            # ondulación viva
            wob = np.sin(t * 9.0 + dx_f * 12.0) * bw * 0.18
            for sgn in (-1, 1):
                ex, ey = px + sgn * bw + wob, py
                d = np.sqrt((xx - ex) ** 2 + (yy - ey) ** 2)
                g = np.exp(-((d / (1.15 * SS)) ** 2)) * (1.0 - t * 0.35)
                # color por ALTURA local (de abajo-arriba: rojo→naranja→amarillo)
                hgt = 1.0 - np.clip((base_y - yy) / (base_y - tipy + 1e-6), 0, 1)
                cr = 255
                cg = 90 + 150 * (1 - hgt)
                cb = 25 + 60 * (1 - hgt) ** 2
                aa = g * a_f * 235
                m = g * a_f > 0.02
                img[..., 0] = np.where(m, np.maximum(img[..., 0], cr * g), img[..., 0])
                img[..., 1] = np.where(m, np.maximum(img[..., 1], cg * g), img[..., 1])
                img[..., 2] = np.where(m, np.maximum(img[..., 2], cb * g), img[..., 2])
                img[..., 3] = np.maximum(img[..., 3], aa)

    # NÚCLEO BLANCO-AMARILLO PURO (el corazón que QUEMA — feedback VLM:
    # blanco puro #FFF en el centro para que arda de verdad)
    d = np.hypot(xx - c, yy - (base_y - OCH * 0.16))
    core = np.exp(-((d / (OCH * 0.10)) ** 2))
    core2 = np.exp(-((d / (OCH * 0.17)) ** 2)) * 0.6
    img[..., 0] = np.maximum(img[..., 0], (core + core2) * 255)
    img[..., 1] = np.maximum(img[..., 1], (core + core2) * 255)
    img[..., 2] = np.maximum(img[..., 2], core * 255 + core2 * 200)
    img[..., 3] = np.maximum(img[..., 3], (core + core2) * 255)

    # recorte circular suave
    r_out = np.hypot(xx - c, yy - OCH * 0.52)
    img[r_out > OCH * 0.50, 3] = 0
    return img


def make_ring_crown_icon():
    """Los TRES ANILLOS RÚNICOS cruzados alrededor del centro."""
    img = np.zeros((OCH, OCW, 4), dtype=float)
    yy, xx = np.mgrid[0:OCH, 0:OCW]
    cx, cy = OCW * 0.5, OCH * 0.5
    dx, dy = xx - cx, yy - cy

    rings = [
        (11.5, 11.5, 0.0, (255, 200, 95)),    # círculo ORO
        (12.5, 5.2, np.deg2rad(28), (255, 245, 220)),  # elipse BLANCA inclinada
        (12.5, 5.2, np.deg2rad(-28), (135, 165, 255)),  # elipse AZUL contra-inclinada
    ]
    for a, b, tilt, (cr, cg, cb) in rings:
        ct, st = np.cos(tilt), np.sin(tilt)
        ux = dx * ct + dy * st
        uy = -dx * st + dy * ct
        r_ell = np.sqrt((ux / (a * SS)) ** 2 + (uy / (b * SS)) ** 2)
        band = np.exp(-((r_ell - 1.0) / 0.055) ** 2)
        # runas: 8 puntos brillantes sobre el anillo
        for i in range(8):
            ang = i / 8.0 * 2 * np.pi
            rx = cx + np.cos(ang) * a * SS * np.cos(tilt) - np.sin(ang) * b * SS * np.sin(tilt)
            ry = cy + np.cos(ang) * a * SS * np.sin(tilt) + np.sin(ang) * b * SS * np.cos(tilt)
            d = np.hypot(xx - rx, yy - ry)
            pearl = np.exp(-((d / (1.1 * SS)) ** 2))
            band = np.maximum(band, pearl * 0.9)
        w = np.clip(band, 0, 1)
        img[..., 0] = img[..., 0] * (1 - w) + cr * w
        img[..., 1] = img[..., 1] * (1 - w) + cg * w
        img[..., 2] = img[..., 2] * (1 - w) + cb * w
        img[..., 3] = np.clip(np.maximum(img[..., 3], band * 240), 0, 255)

    # brillo central suave (el jugador en el interior)
    d = np.hypot(dx, dy)
    core = np.exp(-((d / (5.5 * SS)) ** 2)) * 0.5
    img[..., 0] = np.maximum(img[..., 0], core * 255)
    img[..., 1] = np.maximum(img[..., 1], core * 235)
    img[..., 2] = np.maximum(img[..., 2], core * 200)
    img[..., 3] = np.maximum(img[..., 3], core * 130)

    r_out = np.hypot(dx, dy)
    img[r_out > 14.2 * SS, 3] = 0
    return img


def make_halo_wings_icon():
    """EL ANILLO-HALO: aro dorado grande con runas + destello."""
    img = np.zeros((OCH, OCW, 4), dtype=float)
    yy, xx = np.mgrid[0:OCH, 0:OCW]
    cx, cy = OCW * 0.5, OCH * 0.5
    dx, dy = xx - cx, yy - cy
    r = np.hypot(dx, dy)

    # halo de fondo
    halo = np.exp(-((r / (10.5 * SS)) ** 2) * 1.6)
    # el ARO principal (ancho, dorado)
    ring = np.exp(-(((r - 8.6 * SS) / (1.5 * SS)) ** 2))
    ringHot = np.exp(-(((r - 8.6 * SS) / (0.6 * SS)) ** 2))
    # el aro interior fino (contrarroto, blanco-azul)
    ring2 = np.exp(-(((r - 6.4 * SS) / (0.55 * SS)) ** 2)) * 0.85

    # RUNAS: 10 perlas sobre el aro principal
    pearls = np.zeros((OCH, OCW), dtype=float)
    for i in range(10):
        ang = i / 10.0 * 2 * np.pi + 0.3
        px, py = cx + np.cos(ang) * 8.6 * SS, cy + np.sin(ang) * 8.6 * SS
        d = np.hypot(xx - px, yy - py)
        pearls = np.maximum(pearls, np.exp(-((d / (1.05 * SS)) ** 2)))

    # destello de 4 puntas central (el "motor" del halo)
    x = dx / (OCW * 0.5)
    y = dy / (OCH * 0.5)
    armH = np.exp(-((y / 0.045) ** 2)) * (1.0 - np.clip(np.abs(x), 0, 1)) ** 2.0
    armV = np.exp(-((x / 0.045) ** 2)) * (1.0 - np.clip(np.abs(y), 0, 1)) ** 2.0
    flare = np.maximum(armH, armV) * 0.8

    img[..., 0] = np.maximum(img[..., 0], halo * 130 + ring * 235 + ringHot * 255 +
                             ring2 * 170 + pearls * 255 + flare * 255)
    img[..., 1] = np.maximum(img[..., 1], halo * 95 + ring * 195 + ringHot * 250 +
                             ring2 * 200 + pearls * 240 + flare * 245)
    img[..., 2] = np.maximum(img[..., 2], halo * 45 + ring * 90 + ringHot * 220 +
                             ring2 * 255 + pearls * 200 + flare * 235)
    img[..., 3] = np.clip(np.maximum(halo * 95 + ring * 245 + ringHot * 255 +
                                     ring2 * 210 + pearls * 250 + flare * 235, 0), 0, 255)

    img[r > 14.4 * SS, 3] = 0
    return img


# =====================================================================
#  MAIN
# =====================================================================

def main():
    print("=== v6.22 · TEXTURAS DE LUZ Y FUEGO ===")
    os.makedirs(PROC_OUT, exist_ok=True)
    os.makedirs(WEAP_OUT, exist_ok=True)
    os.makedirs(COSM_OUT, exist_ok=True)
    os.makedirs(WING_OUT, exist_ok=True)

    # 1-4: las texturas de perfil
    save_profile(make_lumen_bloom(), 'LumenBloom.png')
    save_profile(make_lumen_blade(), 'LumenBlade.png')
    save_profile(make_lumen_flare(), 'LumenFlare.png')
    save_profile(make_flame_brush(), 'FlameBrush.png')

    # 5: la sombra del proyectil eclipse
    p = os.path.join(PROJ_OUT, 'EclipsePrimordialProjectile.png')
    make_eclipse_shadow().save(p)
    print('OK', p)

    # 6: los iconos de los bastones solares 11..20
    roman = {11: 'XI', 12: 'XII', 13: 'XIII', 14: 'XIV', 15: 'XV',
             16: 'XVI', 17: 'XVII', 18: 'XVIII', 19: 'XIX', 20: 'XX'}
    for tier in range(11, 21):
        img = np.zeros((OH, OW, 4), dtype=float)
        staff_base(img, gold_tone=True)
        cx, cy = 16.5 * SS, 6.5 * SS
        R = 4.6 * SS
        solar_head(img, cx, cy, R, tier)
        rune_rings(img, cx, cy, tier, tier)
        finish_icon(img, f'SolRunico{tier}Staff.png')

    # el icono del BASTÓN DEL ECLIPSE PRIMORDIAL
    img = np.zeros((OH, OW, 4), dtype=float)
    staff_base(img, gold_tone=False, dark_tone=True)
    eclipse_head(img, 16.5 * SS, 6.5 * SS, 5.2 * SS)
    finish_icon(img, 'EclipsePrimordialStaff.png')

    # 7: los iconos de cosmético
    finish_cosm(make_fire_veil_icon(), 'FireVeilItem.png')
    finish_cosm(make_ring_crown_icon(), 'RuneRingCrownItem.png')
    finish_cosm(make_halo_wings_icon(), 'RunicHaloWings.png')

    # el _Wings.png 8×8 TOTALMENTE TRANSPARENTE (el truco del sprite
    # en blanco: vanilla no dibuja NADA — todo lo pinta nuestra capa)
    blank = np.zeros((8, 8, 4), dtype=np.uint8)
    p = os.path.join(WING_OUT, 'RunicHaloWings_Wings.png')
    Image.fromarray(blank, 'RGBA').save(p)
    print('OK', p, '(8x8 transparente)')

    print("=== LISTO v6.22 ===")


if __name__ == '__main__':
    main()
