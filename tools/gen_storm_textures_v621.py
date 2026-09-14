#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_storm_textures_v621.py — v6.21

LAS TEXTURAS DE LA TORMENTA — nacidas de la investigación profunda de los
sistemas de rayos de los grandes mods (perfiles medidos por análisis visual
de las texturas reales del ecosistema) y re-creadas 100% aquí con numpy.

Convención de la casa: RGB = BLANCO (255) y el PERFIL vive en el canal
ALFA (así el tinte del quad colorea y la intensidad modular el alpha).

  1. BoltHalo.png   (256×64) — banda gaussiana SUAVE (el halo del rayo):
     núcleo ~22% del ancho, cae a ~20% de brillo al 50%, bordes ondulantes
     de baja frecuencia a lo largo.
  2. BoltCore.png   (256×64) — EL FILAMENTO de alto contraste: núcleo
     blanco que SERPENTEA verticalmente (paseo aleatorio con reversión a la
     media, acotado al 30% central), caída brutal, GRIETAS de alta
     frecuencia a lo largo + NODOS brillantes.
  3. BoltChain.png  (256×64) — la cadena eléctrica: óvalos gaussianos
     discretos (~cada 36 px, alturas alternas) unidos por un puente tenue.
  4. BoltImpact.png (96×96)  — el estallido de impacto: punto caliente
     central + 7 rayos radiales finos de longitudes desiguales.
"""

import os
import numpy as np
from PIL import Image

HERE = os.path.dirname(__file__)
PROJ_DIR = os.path.join(HERE, '..', 'AethonMod')
OUT_DIR = os.path.join(PROJ_DIR, 'Content', 'Effects', 'Procedural')

W, H = 256, 64
IMP = 96


def rng(seed):
    """Generador determinista propio (mulberry32) — sin semillas globales."""
    state = [seed & 0xFFFFFFFF]

    def _next():
        state[0] = (state[0] + 0x6D2B79F5) & 0xFFFFFFFF
        t = state[0]
        t = ((t ^ (t >> 15)) * t) & 0xFFFFFFFF
        t = (t ^ (t + (t << 7))) & 0xFFFFFFFF
        return ((t ^ (t >> 17)) & 0xFFFFFFFF) / 4294967296.0

    return _next


def save(arr, name):
    """RGB blanco + alpha = perfil (convención del proyecto)."""
    h, w = arr.shape
    img = np.zeros((h, w, 4), dtype=np.uint8)
    img[..., 0:3] = 255
    img[..., 3] = np.clip(arr * 255.0, 0, 255).astype(np.uint8)
    path = os.path.join(OUT_DIR, name)
    Image.fromarray(img, 'RGBA').save(path)
    print(f"  {name}: {w}x{h}  alpha[min={arr.min():.3f} max={arr.max():.3f} media={arr.mean():.3f}]")


# =====================================================================
#  1. BOLTHALO — la banda suave del halo
# =====================================================================

def gen_bolt_halo():
    r = rng(20260921)
    # Ondulación de baja frecuencia del eje central a lo largo.
    ph1, ph2 = r() * 6.28, r() * 6.28
    u = np.linspace(0.0, 1.0, W)
    axis = 0.5 + 0.045 * np.sin(u * 2 * np.pi * 3 + ph1) \
             + 0.030 * np.sin(u * 2 * np.pi * 7 + ph2)
    # Modulación suave de brillo a lo largo (0.85..1.0).
    mood = 0.85 + 0.15 * (0.5 + 0.5 * np.sin(u * 2 * np.pi * 2.3 + ph1 * 1.7))

    v = np.linspace(0.0, 1.0, H)[:, None]          # (H,1) — a lo ancho
    # Perfil gaussiano ancho (σ≈0.28) alrededor del eje ondulante.
    prof = np.exp(-((v - axis[None, :]) / 0.28) ** 2)
    arr = prof * mood[None, :]
    save(arr, 'BoltHalo.png')


# =====================================================================
#  2. BOLTCORE — EL FILAMENTO (la textura que hace el rayo real)
# =====================================================================

def gen_bolt_core():
    r = rng(77110)
    # --- EL PASEO DEL NÚCLEO: reversión a la media, acotado al 30% central.
    walk = [0.0]
    for i in range(1, W):
        step = (r() - 0.5) * 0.055
        # reversión: tira del eje hacia el centro (el filamento no se escapa).
        step -= walk[-1] * 0.18
        walk.append(np.clip(walk[-1] + step, -0.15, 0.15))
    walk = np.array(walk)
    axis = 0.5 + walk

    # --- GRIETAS: brillo a lo largo con bloques de alta frecuencia y bordes
    #     suavizados (algunos tramos apagados, otros encendidos).
    block = 16
    raw = np.array([0.45 + 0.85 * r() for _ in range(W // block + 1)])
    crackle = np.repeat(raw, block)[:W]
    # suaviza las transiciones (media móvil de 5).
    kern = np.convolve(np.ones(5) / 5.0, np.ones(1))
    crackle = np.convolve(np.concatenate([crackle[:2], crackle, crackle[-2:]]),
                          np.ones(5) / 5.0, mode='same')[2:2 + W]

    # --- NODOS brillantes: destellos periódicos irregulares sobre el eje.
    nodes = np.zeros(W)
    pos = 14 + int(r() * 20)
    while pos < W - 6:
        amp = 0.35 + 0.75 * r()
        sigma = 4.5 + r() * 4.5
        x = np.arange(W)
        nodes += amp * np.exp(-((x - pos) / sigma) ** 2)
        pos += 34 + int(r() * 26)

    v = np.linspace(0.0, 1.0, H)[:, None]
    # Grosor del filamento modulada a lo largo (la vena respira).
    wfrac = 0.075 + 0.030 * (0.5 + 0.5 * np.sin(u_sin(W, 2.7, r() * 6.28)))
    # Perfil ESTRECHO y empinado (σ≈0.155) alrededor del eje serpenteante.
    prof = np.exp(-((v - axis[None, :]) / 0.155) ** 2)
    prof = prof ** 1.15                      # caída aún más agresiva

    arr = prof * crackle[None, :] * (1.0 + nodes[None, :] * 0.9)
    arr = np.clip(arr, 0.0, 1.0)
    save(arr, 'BoltCore.png')


def u_sin(n, freq, phase):
    return np.linspace(0.0, 1.0, n) * 2 * np.pi * freq + phase


# =====================================================================
#  3. BOLTCHAIN — la cadena de eslabones eléctricos
# =====================================================================

def gen_bolt_chain():
    r = rng(424242)
    u = np.arange(W)
    v = np.linspace(0.0, 1.0, H)[:, None]

    # El puente tenue que une las cuentas.
    bridge = 0.22 * np.exp(-((v - 0.5) / 0.12) ** 2) * np.ones((1, W))

    # Las cuentas: óvalos gaussianos discretos con alturas alternas.
    beads = np.zeros((H, W))
    pos = 8
    flip = 1
    while pos < W - 8:
        cy = 0.5 + flip * 0.065
        su = 8.5 + r() * 3.5
        sv = 0.16 + r() * 0.05
        beads += np.exp(-(((u[None, :] - pos) / su) ** 2
                          + ((v - cy) / sv) ** 2))
        pos += 34 + int(r() * 10)
        flip = -flip

    arr = np.clip(bridge + beads, 0.0, 1.0)
    save(arr, 'BoltChain.png')


# =====================================================================
#  4. BOLTIMPACT — el estallido del impacto
# =====================================================================

def gen_bolt_impact():
    r = rng(96310)
    c = IMP / 2.0
    yy, xx = np.mgrid[0:IMP, 0:IMP]
    x = (xx - c + 0.5) / c            # -1..1
    y = (yy - c + 0.5) / c
    rad = np.sqrt(x ** 2 + y ** 2)
    ang = np.arctan2(y, x)

    # El punto caliente central.
    arr = np.exp(-(rad / 0.14) ** 2)

    # SIETE rayos radiales finos de longitudes desiguales.
    for i in range(7):
        a0 = i * 2 * np.pi / 7 + (r() - 0.5) * 0.35
        reach = 0.62 + r() * 0.34               # longitud del rayo
        # ancho angular: más gordo cerca del centro.
        sig = 0.052 + 0.030 * np.clip(1.0 - rad / 0.55, 0, 1)
        d = np.abs(np.arctan2(np.sin(ang - a0), np.cos(ang - a0)))
        ray = np.exp(-(d / sig) ** 2) * np.exp(-((rad / reach) ** 1.7)) * 0.85
        arr += ray

    # Un halo suave de base para que nunca quede hueco en el centro.
    arr += 0.28 * np.exp(-(rad / 0.55) ** 2)
    arr = np.clip(arr, 0.0, 1.0)
    save(arr, 'BoltImpact.png')


if __name__ == '__main__':
    os.makedirs(OUT_DIR, exist_ok=True)
    print("Generando las texturas de LA TORMENTA (v6.21)...")
    gen_bolt_halo()
    gen_bolt_core()
    gen_bolt_chain()
    gen_bolt_impact()
    print("OK — 4 texturas en", OUT_DIR)
