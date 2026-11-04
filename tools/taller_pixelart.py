#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
taller_pixelart.py — LA FORJA: LAS TRES HERRAMIENTAS UNA SOLA (v6.95).

La letra del usuario: «¿podrías unir estas 3 herramientas en 1 sola y
convertirla en algo que se pueda usar? además dime como se podrian usar
correctamente y como se podrian unir y servir para nuestro mod» —

LOS TRES REELS:
  1. pixelfx   (patch-gg/pixelfx) — 244 efectos de píxel listos (fuego,
                humo, glitch, CRT, aberración…) — cada uno, un trocito
                de código. EL ALMA del sprite.
  2. sprite-gen (aldegad/sprite-gen) — una imagen → sprites jugables:
                transparencia real (flood-fill), rejilla, ciclos
                idle/andar/atacar, atlas + manifiesto. EL MOLDE.
  3. pxl       — «pixel art as code»: el agente PINTA el sprite paso a
                paso en un lienzo mínimo y lo exporta. LA PLUMA.

LA UNIÓN = LA CADENA DE LA CASA (el pipeline que ya vive en el repo,
ahora UN comando):
  LA PLUMA  →  EL MOLDE  →  EL ALMA
  (borrador    (cutout →      (el efecto de píxel
   IA con QA    rejilla →      de la casa encima:
   VLM, como    paleta →       brasa/humo/glitch —
   pxl: pintar  contorno →     el preview «vivo»
   → mirar →    highlight →    antes de tocar el
   refinar)     item)          C# del juego)

USO:
  # el ciclo completo (borrador IA → sprite → efecto → montaje QA):
  python3 tools/taller_pixelart.py \
      --nombre mi_sprite --prompt "un altar de tinta azul..." \
      --ancho 36 --alto 54 --efecto brasa

  # solo el molde (un borrador ya existente):
  python3 tools/taller_pixelart.py --nombre altar \
      --borrador /tmp/altar_borrador.png --ancho 36 --alto 54

  # solo el alma (un sprite ya final):
  python3 tools/taller_pixelart.py --nombre ojo \
      --sprite AethonMod/Content/Projectiles/OjoEterne_Medio.png \
      --efecto humo --x8

SALIDAS (en /tmp/forja_<nombre>/):
  borrador.png      el borrador IA (la pluma)
  sprite.png        el sprite final con la paleta de la casa (el molde)
  highlight.png     la silueta amarilla de la convención tML
  efecto_<n>.png    el preview del efecto (el alma), 3 frames
  montaje_qa.png    TODO junto para el ojo (y el QA VLM)

TODO local, sin nube (la regla de sprite-gen), y cada etapa usa EXACTA-
MENTE las funciones que ya produjeron los sprites vivos del mod (la
fuente de verdad de gen_altar_v695.py).
"""
import argparse
import os
import subprocess
import sys

from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, AQUI)
from gen_altar_v695 import (PALETA, bbox, celdas, contorno, cutout,
                            cuantizar, halo, highlight)

RAIZ = os.path.join(AQUI, '..', 'AethonMod')

# === LA PALETA DE LA CASA (para el prompt de la pluma) ===
PALETA_PROMPT = ("deep midnight-blue ink #182878 body, near-black #0A0A2A shadows, "
                 "golden amber clasps #D89030 with #FFC850 highlights, glowing bright "
                 "fuchsia #F000C0 magic accents, teal #5ADCB4 flames, violet #9643EB runes")


# ====================================================================
#  ETAPA 1 — LA PLUMA (el método pxl: borrar → renderizar → mirar)
# ====================================================================
def la_pluma(prompt, destino, tamano='864x1152'):
    """EL BORRADOR IA con la skill de generación (el paso «pintar» de
    pxl; el «mirar» lo hace el QA VLM del final — la ronda de refinar
    es regenerar con el prompt corregido)."""
    prompt_full = (f"Pixel art sprite for a Terraria-style game, strictly front view. "
                   f"{prompt}. Limited palette: {PALETA_PROMPT}. Chunky clean large "
                   f"pixels, strong dark outline, no anti-aliasing, flat solid pure "
                   f"green background (#00FF00) chroma key, subject centered filling "
                   f"80% of frame height")
    r = subprocess.run(['z-ai', 'image', '-p', prompt_full, '-o', destino, '-s', tamano],
                       capture_output=True, text=True, timeout=240)
    if not os.path.exists(destino):
        print(r.stdout[-400:] if r.stdout else '', r.stderr[-400:] if r.stderr else '')
        raise SystemExit('LA PLUMA falló: la generación no devolvió imagen')
    print(f'  LA PLUMA: {destino}')


# ====================================================================
#  ETAPA 2 — EL MOLDE (el método sprite-gen: cutout → rejilla → atlas)
# ====================================================================
def el_molde(origen, w, h, con_contorno=True, con_halo=True):
    """EL SPRITE de la casa: cutout real por flood-fill, rejilla por
    celdas, paleta del LIBRO, contorno Terraria (y el halo del conjuro
    si hay magia). Devuelve (sprite, highlight)."""
    borrador = Image.open(origen).convert('RGBA')
    recorte = bbox(cutout(borrador))
    sprite = celdas(recorte, w, h)
    sprite = cuantizar(sprite)
    if con_halo:
        sprite = halo(sprite)
    if con_contorno:
        sprite = contorno(sprite)
    print(f'  EL MOLDE: sprite {w}x{h} (bbox del borrador: {recorte.size})')
    return sprite, highlight(sprite)


# ====================================================================
#  ETAPA 3 — EL ALMA (el catálogo pixelfx: el efecto que lo da vida)
# ====================================================================
def el_alma(sprite, efecto='brasa', frames=3):
    """EL PREVIEW VIVO: los perfiles de PixelFxCore (el motor de píxel
    del Taller, el port verificado de pixelfx) — aquí en Python, solo
    para VER el efecto antes de escribir el C# del juego. Cada efecto
    es un trocito de código, como manda el reel."""
    import math
    import random
    random.seed(7)
    out = []
    w, h = sprite.size
    px = sprite.load()
    for f in range(frames):
        fr = sprite.copy()
        pf = fr.load()
        t = f / max(1, frames - 1)
        for y in range(h):
            for x in range(w):
                r, g, b, a = px[x, y]
                if a == 0:
                    continue
                if efecto == 'brasa':
                    # EL FUEGO de pixelfx: el ruido que SUBE y prende el borde bajo
                    n = (math.sin(x * 1.7 + t * 9.0) * 0.5 + 0.5) * \
                        (math.sin(y * 0.9 - t * 14.0) * 0.5 + 0.5)
                    if n > 0.62 and y > h * 0.45:
                        k = (n - 0.62) / 0.38
                        pf[x, y] = (int(r + (255 - r) * k),
                                    int(g * (1 - k * 0.7)),
                                    int(b * (1 - k * 0.9)), 255)
                elif efecto == 'humo':
                    # EL HUMO: la columna que se deshace arriba
                    n = math.sin(x * 0.8 + t * 6.0 + y * 0.35) * 0.5 + 0.5
                    if y < h * 0.4 and n > 0.55 and random.random() < 0.5:
                        k = (1 - y / (h * 0.4)) * 0.5
                        pf[x, y] = (int(r + (200 - r) * k),
                                    int(g + (200 - g) * k),
                                    int(b + (200 - b) * k), 255)
                elif efecto == 'glitch':
                    # EL GLITCH: las bandas que se corren
                    if (y + f * 5) % 11 < 2 and random.random() < 0.8:
                        dx = random.choice([-3, -2, 2, 3])
                        nx = min(w - 1, max(0, x + dx))
                        pf[x, y] = px[nx, y]
                elif efecto == 'consagracion':
                    # LA CONSAGRACIÓN de la casa: el brillo fucsia que baja
                    linea = int(h * (0.15 + t * 0.8))
                    if abs(y - linea) < 2 and random.random() < 0.8:
                        pf[x, y] = (240, 0, 192, 255)
        out.append(fr)
    print(f'  EL ALMA: {frames} frames del efecto «{efecto}»')
    return out


# ====================================================================
#  EL MONTAJE — TODO junto para el ojo (y el QA VLM de la casa)
# ====================================================================
def el_montaje(carpeta, borrador, sprite, hl, frames_efecto, x=8):
    piezas = []
    if sprite is not None:
        piezas.append(('sprite x%d' % x, sprite.resize((sprite.width * x, sprite.height * x), Image.NEAREST)))
        if hl is not None:
            piezas.append(('highlight x%d' % x, hl.resize((hl.width * x, hl.height * x), Image.NEAREST)))
        for i, fr in enumerate(frames_efecto):
            piezas.append((f'efecto {i + 1}', fr.resize((fr.width * x, fr.height * x), Image.NEAREST)))
    if not piezas:
        return None
    alto = max(p[1].height for p in piezas) + 30
    ancho = sum(p[1].width for p in piezas) + 20 * (len(piezas) + 1)
    lienzo = Image.new('RGBA', (ancho, alto), (24, 24, 34, 255))
    dx = 20
    for nombre, img in piezas:
        lienzo.paste(img, (dx, 22), img)
        dx += img.width + 20
    destino = os.path.join(carpeta, 'montaje_qa.png')
    lienzo.save(destino)
    print(f'  EL MONTAJE: {destino}')
    return destino


def main():
    ap = argparse.ArgumentParser(description='LA FORJA: la pluma + el molde + el alma')
    ap.add_argument('--nombre', required=True, help='el nombre del sprite (la carpeta /tmp/forja_<nombre>)')
    ap.add_argument('--prompt', help='LA PLUMA: qué pintar (sin la paleta: la pone la forja)')
    ap.add_argument('--borrador', help='el borrador ya existente (salta la pluma)')
    ap.add_argument('--sprite', help='el sprite ya final (solo el alma)')
    ap.add_argument('--ancho', type=int, default=36, help='el ancho del sprite final (px)')
    ap.add_argument('--alto', type=int, default=54, help='el alto del sprite final (px)')
    ap.add_argument('--efecto', default='brasa',
                    choices=['brasa', 'humo', 'glitch', 'consagracion'],
                    help='EL ALMA: el efecto del preview')
    ap.add_argument('--sin-contorno', action='store_true', help='sin el contorno Terraria')
    ap.add_argument('--sin-halo', action='store_true', help='sin el halo del conjuro')
    ap.add_argument('--x8', type=int, default=8, help='el factor del montaje QA')
    args = ap.parse_args()

    carpeta = f'/tmp/forja_{args.nombre}'
    os.makedirs(carpeta, exist_ok=True)

    borrador = args.borrador
    if args.sprite:
        sprite = Image.open(args.sprite).convert('RGBA')
        hl = highlight(sprite)
        print(f'LA FORJA — solo el alma sobre {args.sprite}')
    else:
        if borrador is None:
            if not args.prompt:
                ap.error('hace falta --prompt, --borrador o --sprite')
            print('LA FORJA — etapa 1: LA PLUMA (pxl: pintar con la IA)')
            borrador = os.path.join(carpeta, 'borrador.png')
            la_pluma(args.prompt, borrador)
        else:
            print('LA FORJA — etapa 1: LA PLUMA (borrador existente)')
        print('LA FORJA — etapa 2: EL MOLDE (sprite-gen: cutout → rejilla → paleta)')
        sprite, hl = el_molde(borrador, args.ancho, args.alto,
                              con_contorno=not args.sin_contorno,
                              con_halo=not args.sin_halo)
        sprite.save(os.path.join(carpeta, 'sprite.png'))
        hl.save(os.path.join(carpeta, 'highlight.png'))
        print(f'  guardados: {carpeta}/sprite.png + highlight.png')

    print('LA FORJA — etapa 3: EL ALMA (pixelfx: el efecto que lo da vida)')
    frames = el_alma(sprite, args.efecto)
    for i, fr in enumerate(frames):
        fr.save(os.path.join(carpeta, f'efecto_{i + 1}.png'))

    print('LA FORJA — el montaje QA (el paso «mirar» de la casa)')
    destino = el_montaje(carpeta, borrador, sprite, hl, frames, args.x8)
    print(f'\nLISTO: {destino}')
    print('el QA VLM de la casa:  z-ai vision -p "QA..." -i', destino)


if __name__ == '__main__':
    main()
