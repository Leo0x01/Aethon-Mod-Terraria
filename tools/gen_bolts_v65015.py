#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_bolts_v65015.py — v6.50.15 — LA ECUACIÓN LINEAL DE LAS BANDAS DE LOS RAYOS.

Forense R55-d (el usuario: "los rayos siguen estando mal"): las bandas v6.39
horneaban RGB=perfil y alfa=perfil asumiendo que el lote aditivo era
(One, One) — "el lote aditivo suma textura.rgb y PASA del alfa" — pero la
sonda v6.50.9 contra el FNA real del tML 2026.07.3.0 demostró que
BlendState.Additive es (SourceAlpha, One) Y que el loader PREMULTIPLICA los
PNG (P.rgb = rgb·alfa/255). Con el tinte √f de la casa, el aporte de cada
quad quedaba:

    aporte = P.rgb · P.a · color · f = perfil² · perfil · color · f
           = perfil³ · color · f        ← EL PERFIL AL CUBO

La gaussiana del BoltHalo (σ≈0.28 de la banda, FWHM≈34%) colapsada al cubo
deja un hilo de 1-2px: los rayos se leían como FILAMENTOS FINOS sin halo —
exactamente el reporte. El Tint √f de v6.50.9 arregló la escala de f, no la
FORMA del perfil.

LA REPARACIÓN — hornear RGB=255 y alfa=√perfil:
    P.rgb = 1 · √perfil = √perfil    (el premult del loader)
    P.a   = √perfil
    aporte = √perfil · √perfil · color · f = perfil · color · f   ← LINEAL

La gaussiana recupera su anchura REAL y la pila de capas del renderer
(bloom ×4.6 · halo ×2.2 · cuerpo ×1 · vena ×¼, v6.50.15) vuelve a leerse
como descarga. BoltImpact recibe el mismo tratamiento (su estallido radial
estaba igual de colapsado).

NO se tocan: BoltChain.png (retirado sin consumidores desde v6.50.8),
SoftGlow/GlowOrb/Ring (la convención de la casa para velos — horneada en
RGB desde v5.x y calibrada a ojo durante años; cambiarla movería TODO el
mod), y las RiftTaper (su propio camino de dibujo).
"""

import os
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ_DIR = os.path.join(HERE, '..', 'AethonMod')
OUT_DIR = os.path.join(PROJ_DIR, 'Content', 'Effects', 'Procedural')

W, H = 256, 64


def save_linear(arr, name):
    """v6.50.15 — LA CONVENCIÓN LINEAL: RGB=255, alfa=√perfil.

    Tras el premult del loader: P.rgb = P.a = √perfil → el aporte en el
    lote aditivo cae perfil·color·f (LINEAL en el perfil, no al cubo)."""
    h, w = arr.shape
    v = np.clip(arr, 0.0, 1.0)
    a = np.clip(np.sqrt(v) * 255.0, 0, 255).astype(np.uint8)
    img = np.zeros((h, w, 4), dtype=np.uint8)
    img[..., 0:3] = 255                     # RGB = blanco puro
    img[..., 3] = a                          # alfa = √perfil
    path = os.path.join(OUT_DIR, name)
    Image.fromarray(img, 'RGBA').save(path)
    fila = a[H // 2]
    print(f"  {name}: {w}x{h}  LINEAL  alfa[centro fila] "
          f"x0..3={fila[:4].tolist()} xmid={fila[w//2-2:w//2+3].tolist()} "
          f"max={a.max()}")


def fundido_extremos(arr, px=3):
    """El ÚNICO perfil longitudinal permitido: 3 px de subida/bajada en los
    extremos (el margen de antialias de las juntas del ribbon)."""
    w = arr.shape[1]
    if w <= px * 2:
        return arr
    rampa = np.ones(w)
    rampa[:px] = np.linspace(0.0, 1.0, px)
    rampa[-px:] = np.linspace(1.0, 0.0, px)
    return arr * rampa[None, :]


def gen_bolt_halo():
    v = np.linspace(0.0, 1.0, H)[:, None]
    # Perfil gaussiano ancho (σ≈0.28): la funda exterior — IGUAL que v6.39
    # (la forma era correcta; lo que mataba era el perfil³ del pipeline).
    prof = np.exp(-((v - 0.5) / 0.28) ** 2)
    arr = fundido_extremos(np.repeat(prof, W, axis=1))
    save_linear(arr, 'BoltHalo.png')


def gen_bolt_core():
    v = np.linspace(0.0, 1.0, H)[:, None]
    # Perfil ESTRECHO y empinado (σ≈0.155, caída ^1.15): la vena caliente.
    prof = np.exp(-((v - 0.5) / 0.155) ** 2) ** 1.15
    arr = fundido_extremos(np.repeat(prof, W, axis=1))
    save_linear(arr, 'BoltCore.png')


IMP = 96


def gen_bolt_impact():
    # (re-generación del patrón v6.39 exacto — rng mulberry32 propio)
    state = [96310 & 0xFFFFFFFF]

    def r():
        state[0] = (state[0] + 0x6D2B79F5) & 0xFFFFFFFF
        t = state[0]
        t = ((t ^ (t >> 15)) * t) & 0xFFFFFFFF
        t = (t ^ (t + (t << 7))) & 0xFFFFFFFF
        return ((t ^ (t >> 17)) & 0xFFFFFFFF) / 4294967296.0

    c = IMP / 2.0
    yy, xx = np.mgrid[0:IMP, 0:IMP]
    x = (xx - c + 0.5) / c
    y = (yy - c + 0.5) / c
    rad = np.sqrt(x ** 2 + y ** 2)
    ang = np.arctan2(y, x)

    arr = np.exp(-(rad / 0.14) ** 2)

    for i in range(7):
        a0 = i * 2 * np.pi / 7 + (r() - 0.5) * 0.35
        reach = 0.62 + r() * 0.34
        sig = 0.052 + 0.030 * np.clip(1.0 - rad / 0.55, 0, 1)
        d = np.abs(np.arctan2(np.sin(ang - a0), np.cos(ang - a0)))
        ray = np.exp(-(d / sig) ** 2) * np.exp(-((rad / reach) ** 1.7)) * 0.85
        arr += ray

    arr += 0.28 * np.exp(-(rad / 0.55) ** 2)
    arr = np.clip(arr, 0.0, 1.0)
    save_linear(arr, 'BoltImpact.png')


if __name__ == '__main__':
    os.makedirs(OUT_DIR, exist_ok=True)
    print("GENERANDO LAS BANDAS LINEALES v6.50.15 (RGB=255, alfa=√perfil)...")
    gen_bolt_halo()
    gen_bolt_core()
    gen_bolt_impact()
    print("OK — bandas lineales en", OUT_DIR)
