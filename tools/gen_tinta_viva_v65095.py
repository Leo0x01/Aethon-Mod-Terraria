#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
GEN_TINTA_VIVA_v65095 — LA LÁGRIMA DE TINTA (el disparo propio del libro).

La letra del usuario: «tengo una idea, te dare un sprite para un disparo,
tu has que el disparo tenga efectos, te dare el codigo como siempre, eso es
mejor que darte la imagen png, quiero que con este proyectil seas creativo,
lo animes y crees una nueva arma con el proyectil» — el sprite EXACTO
(607×1344, RLE [n,R,G,B,A] base64, «sin pérdida · sin manipulación») es la
fuente autoritativa; de él nacen:

  LagrimaDeTintaProjectile.png   28×304  EL STRIP de 4 frames 28×76 —
                                        LA RESPIRACIÓN de la lágrima:
                                        escala a lo largo [0.955, 0.98,
                                        1.0, 0.965] (las puntas laten)
                                        + PULSO del corazón de marfil
                                        (los claros se avivan ×1.0/×1.15/
                                        ×1.28/×0.92 por frame — el núcleo
                                        respira ANTES de que el draw le
                                        ponga el halo aditivo encima).
  TintaViva.png                  44×44  EL ICONO del arma — la lágrima
                                        ROTADA -35° (punta arriba-
                                        derecha) al Lanczos desde la
                                        fuente (rotar ANTES de reducir:
                                        el borde queda limpio).

Downscale: LANCZOS puro (la lección .86 — el código puro da mejor calidad
y los párpados quedaron píxel sobre píxel así). Determinista: sin random.
"""
import base64
import re

import numpy as np
from PIL import Image

UP = '/home/z/my-project/upload/proyectil.txt'
DEST_PROY = ('/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/'
             'Content/Projectiles/Sombras/LagrimaDeTintaProjectile.png')
DEST_ARMA = ('/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/'
             'Content/Weapons/Sombras/TintaViva.png')
PREVIEW = '/tmp/tinta_preview_v65095.png'

FW, FH = 28, 76            # frame del proyectil
N_FRAMES = 4
ICONO = 44                 # lado del icono del arma
ROT = -35                  # grados del icono (punta arriba-derecha)

# LA RESPIRACIÓN: (escala a lo largo, avivo del corazón de marfil) —
# el VLM pidió MÁS delta de brillo entre frames (a velocidad de juego el
# latido tiene que LEERSE): ×1.0/×1.22/×1.45/×0.86
RESPIRACION = [(0.940, 1.00), (0.980, 1.22), (1.000, 1.45), (0.955, 0.86)]


def decodificar():
    """RLE [count,R,G,B,A] base64 → RGBA 607×1344 (el sprite EXACTO)."""
    data = open(UP, encoding='utf-8').read()
    rle = base64.b64decode(re.search(r'rle:"([^"]+)"', data).group(1))
    assert len(rle) % 5 == 0, f'RLE truncado: {len(rle)} bytes'
    w = int(re.search(r'w:(\d+)', data).group(1))
    h = int(re.search(r'h:(\d+)', data).group(1))
    buf = bytearray(w * h * 4)
    p = i = 0
    while i + 5 <= len(rle):
        n, r, g, b, a = rle[i:i + 5]
        i += 5
        for _ in range(n):
            buf[p] = r; buf[p + 1] = g; buf[p + 2] = b; buf[p + 3] = a
            p += 4
    assert p == w * h * 4, f'pixeles {p // 4} != {w * h}'
    img = np.frombuffer(bytes(buf), dtype=np.uint8).reshape(h, w, 4)
    alpha = img[..., 3] > 10
    ys, xs = np.where(alpha)
    return img[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def avivar(img, k):
    """EL PULSO: los píxeles CLAROS (el corazón de marfil) se avivan hacia
    blanco ×(1+k); los oscuros ni se enteran (la sombra no late)."""
    if k <= 0:
        return img
    f = img.astype(np.float64)
    lum = f[..., :3].mean(axis=2)
    # máscara suave: 0 en ≤150, 1 en ≥210 (la rampa evita el borde duro)
    mascara = np.clip((lum - 150.0) / 60.0, 0.0, 1.0)[..., None]
    f[..., :3] = f[..., :3] + (255.0 - f[..., :3]) * (k - 1.0) * mascara
    return np.clip(f, 0, 255).astype(np.uint8)


def frame(img, escala, avivo):
    """Un frame 28×76: la fuente respira a lo largo (centrada) y el
    corazón late — la lágrima cabe ENTERA siempre (escala ≤ 1)."""
    alto = max(2, round(FH * escala))
    fuente = avivar(img, avivo)
    ch = Image.fromarray(fuente).resize((FW, alto), Image.LANCZOS)
    lienzo = Image.new('RGBA', (FW, FH), (0, 0, 0, 0))
    lienzo.paste(ch, (0, (FH - alto) // 2))
    return np.array(lienzo)


def icono(img):
    """El icono: rotar -35° en ALTA resolución y bajar al Lanczos —
    la lágrima diagonal cabe en el cuadro y LEE como arma."""
    grande = Image.fromarray(img).resize(
        (FW * 6, round(FW * 6 * img.shape[0] / img.shape[1])), Image.LANCZOS)
    # rotar alrededor del centro con fondo transparente
    rot = grande.rotate(ROT, resample=Image.BICUBIC, expand=True)
    # el cuadro 44×44 con 1 px de aire (VLM: no tocar el borde del slot):
    # la lágrima ocupa el ~93 % del lado, CENTRADA
    lado = min(rot.size)
    margen = round(lado * 0.035)
    caja = ((rot.size[0] - lado) // 2 + margen, (rot.size[1] - lado) // 2 + margen,
            (rot.size[0] + lado) // 2 - margen, (rot.size[1] + lado) // 2 - margen)
    lado -= 2 * margen
    rot = rot.crop(caja).resize((ICONO - 2, ICONO - 2), Image.LANCZOS)
    lienzo = Image.new('RGBA', (ICONO, ICONO), (0, 0, 0, 0))
    lienzo.paste(rot, (1, 1))
    return np.array(lienzo)


def main():
    img = decodificar()
    print(f'fuente recortada: {img.shape[1]}×{img.shape[0]} '
          f'(ratio {img.shape[1] / img.shape[0]:.4f} → {FW}/{FH} = {FW / FH:.4f})')

    # === EL STRIP DEL PROYECTIL: 4 frames apilados ===
    frames = [frame(img, e, a) for e, a in RESPIRACION]
    strip = np.concatenate(frames, axis=0)
    Image.fromarray(strip).save(DEST_PROY)
    print(f'{DEST_PROY}: {strip.shape[1]}×{strip.shape[0]} '
          f'({N_FRAMES} frames de {FW}×{FH})')

    # === EL ICONO DEL ARMA ===
    ic = icono(img)
    Image.fromarray(ic).save(DEST_ARMA)
    print(f'{DEST_ARMA}: {ic.shape[1]}×{ic.shape[0]}')

    # === PREVIEW (×6, los 4 frames + icono) ===
    gran = np.kron(strip, np.ones((6, 6, 1), dtype=np.uint8))
    icg = np.kron(ic, np.ones((int(6 * FH / ICONO), 6, 1), dtype=np.uint8))
    alto = gran.shape[0]
    lienzo = np.zeros((alto, gran.shape[1] + 20 + icg.shape[1], 4), dtype=np.uint8)
    lienzo[:gran.shape[0], :gran.shape[1]] = gran
    off = (alto - icg.shape[0]) // 2
    lienzo[off:off + icg.shape[0], gran.shape[1] + 20:] = icg
    Image.fromarray(lienzo).save(PREVIEW)
    print(f'preview: {PREVIEW} (frames a la izquierda ×6, icono a la derecha)')


if __name__ == '__main__':
    main()
