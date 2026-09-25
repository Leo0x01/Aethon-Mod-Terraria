#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_fragmento_v641.py — EL MOCK 1:1 DEL FRAGMENTO DE SUPERNOVA.

Render con la MATEMÁTICA EXACTA del FragmentoSupernovaMinion.PreDraw
(los mismos quads: cuerpo 110×40, alas 52×16 a ±0.44 rad, puntas 22×10,
pupila 34×12 alpha, halo Ring 60, glow 150×70, ecos orbitales 4+3)
sobre la textura SoftGlow real del mod — para validar el look ANTES del
juego: la pupila tiene que verse (alpha sobre aditivo), las alas tienen
que leerse como alas, los ecos tienen que respirar.
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GLOW = np.asarray(Image.open(os.path.join(BASE, "Content/Effects/Procedural/SoftGlow.png")).convert("RGBA"), dtype=np.float32) / 255.0
RING = np.asarray(Image.open(os.path.join(BASE, "Content/Effects/Procedural/Ring.png")).convert("RGBA"), dtype=np.float32) / 255.0

W, H = 360, 240
CX, CY = W // 2, H // 2


def quad_add(canvas, tex, x, y, wpx, hpx, rot, color, alpha):
    """Quad aditivo: textura escalada a wpx×hpx, rotada, color×alpha."""
    th, tw = tex.shape[:2]
    scaled = np.asarray(Image.fromarray((tex * 255).astype(np.uint8)).resize(
        (max(1, int(wpx)), max(1, int(hpx))), Image.BILINEAR), dtype=np.float32) / 255.0
    im = Image.fromarray((scaled * 255).astype(np.uint8)).rotate(math.degrees(rot), resample=Image.BILINEAR, expand=True)
    arr = np.asarray(im, dtype=np.float32) / 255.0
    ah, aw = arr.shape[:2]
    x0, y0 = int(x - aw / 2), int(y - ah / 2)
    # recorte
    sx0, sy0 = max(0, -x0), max(0, -y0)
    sx1, sy1 = min(aw, W - x0), min(ah, H - y0)
    if sx1 <= sx0 or sy1 <= sy0:
        return
    sub = arr[sy0:sy1, sx0:sx1]
    r = (color[0] / 255) * alpha
    g = (color[1] / 255) * alpha
    b = (color[2] / 255) * alpha
    a = sub[..., 3] * alpha
    region = canvas[y0 + sy0:y0 + sy1, x0 + sx0:x0 + sx1]
    region[..., 0] += sub[..., 0] * r * a
    region[..., 1] += sub[..., 1] * g * a
    region[..., 2] += sub[..., 2] * b * a


def quad_alpha(canvas, tex, x, y, wpx, hpx, rot, color, alpha):
    """Quad alpha (oscurece): dst = dst·(1−src·a)."""
    th, tw = tex.shape[:2]
    scaled = Image.fromarray((tex * 255).astype(np.uint8)).resize(
        (max(1, int(wpx)), max(1, int(hpx))), Image.BILINEAR)
    im = scaled.rotate(math.degrees(rot), resample=Image.BILINEAR, expand=True)
    arr = np.asarray(im, dtype=np.float32) / 255.0
    ah, aw = arr.shape[:2]
    x0, y0 = int(x - aw / 2), int(y - ah / 2)
    sx0, sy0 = max(0, -x0), max(0, -y0)
    sx1, sy1 = min(aw, W - x0), min(ah, H - y0)
    if sx1 <= sx0 or sy1 <= sy0:
        return
    sub = arr[sy0:sy1, sx0:sx1]
    a = sub[..., 3] * alpha
    region = canvas[y0 + sy0:y0 + sy1, x0 + sx0:x0 + sx1]
    region *= (1.0 - a)[..., None]


def componer_ojo(canvas, pos, rot, escala, tinte, teñir):
    cuerpo = tinte if teñir else (255, 220, 150)
    ala = tinte if teñir else (240, 120, 72)
    punta = tinte if teñir else (168, 48, 48)
    a = (tinte[3] / 255 * 0.8) if teñir else 0.85
    quad_add(canvas, GLOW, pos[0], pos[1], 110 * escala, 40 * escala, rot, cuerpo, a)
    for lado in (-1, 1):
        ox = pos[0] + math.cos(rot) * (38 * lado) - math.sin(rot) * (-4)
        oy = pos[1] + math.sin(rot) * (38 * lado) + math.cos(rot) * (-4)
        quad_add(canvas, GLOW, ox, oy, 52 * escala, 16 * escala, rot + lado * -0.44, ala, a)
        px = pos[0] + math.cos(rot) * (62 * lado) - math.sin(rot) * (-12)
        py = pos[1] + math.sin(rot) * (62 * lado) + math.cos(rot) * (-12)
        quad_add(canvas, GLOW, px, py, 22 * escala, 10 * escala, rot + lado * -0.6, punta, a * 0.9)


def render(t, rot, out):
    # base: cielo nocturno suave
    yy, xx = np.mgrid[0:H, 0:W]
    canvas = np.zeros((H, W, 3), dtype=np.float32)
    canvas[..., 2] += 0.10
    canvas[..., 1] += 0.06

    # EL PULSO TRIANGULAR (EcosLib)
    tp = t % 4.0 / 2.0
    if tp >= 1:
        tp = 2 - tp
    pulso = tp * 0.5 + 0.5
    tempo = t * 0.5 + t * 0.04

    # LOS ECOS ORBITALES (4 + 3)
    for i in range(4):
        rad = (i * 0.25 + tempo) * math.tau
        ex = CX + math.sin(rad) * (8 * (0.6 + 0.4 * pulso))
        ey = CY + math.cos(rad) * (8 * (0.6 + 0.4 * pulso))
        componer_ojo(canvas, (ex, ey), rot, 0.55, (255, 233, 197, 50), True)
    for i in range(3):
        rad = (i * 0.34 + tempo) * math.tau
        ex = CX + math.sin(rad) * (16 * (0.6 + 0.4 * pulso))
        ey = CY + math.cos(rad) * (16 * (0.6 + 0.4 * pulso))
        componer_ojo(canvas, (ex, ey), rot, 0.5, (244, 142, 72, 77), True)

    # EL CUERPO VIVO
    componer_ojo(canvas, (CX, CY), rot, 1.0, (255, 220, 150), False)

    # EL GLOW GOLDENROD PULSANTE (periodo 1.4 s)
    latido = math.cos(t % 1.4 / 1.4 * math.tau) * 0.5 + 0.5
    quad_add(canvas, GLOW, CX, CY, 150, 70, 0, (218, 165, 32), 0.35 * latido)

    # EL HALO (Ring 60px visible radius)
    s = 60 * 2.174
    quad_add(canvas, RING, CX, CY, s, s, t * 0.4, (218, 165, 32), 0.30)

    # LA PUPILA (pase alpha AL FINAL — oscurece TODO el brillo debajo)
    quad_alpha(canvas, GLOW, CX, CY, 34, 12, rot, (0, 0, 0), 0.75)

    np.clip(canvas, 0, 1, out=canvas)
    Image.fromarray((canvas * 255).astype(np.uint8)).save(out)


if __name__ == "__main__":
    outs = "/home/z/my-project/AethonMod/AethonMod/research/supernova_v641"
    for i, (t, rot) in enumerate([(0.0, 0.0), (0.7, 0.12), (2.1, -0.08), (3.4, 0.05)]):
        render(t, rot, os.path.join(outs, f"mock_fragmento_{i}.png"))
    print("4 mocks del fragmento generados")
