#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_codice_vivo_v65072.py — EL CÓDICE VIVO v2 (la animación que SE VE).

La letra del usuario (v6.50.72): «el sprite del codice vivo esta mal hecho,
solo es la imagen fija subiendo en vertical sin animaciones ni nada, estoy
seguro que no usaste el codigo que te di para crear una animacion».

La .71 SÍ usó la matriz de codigo.txt, pero su animación era un pulso de
brillo en 9 tonos de la rampa (3–7% de los píxeles por frame) + un parpadeo
de 2 frames — a 128 px en juego se leía como ESTATICA. Esta versión vuelve a
nacer 100% de la MISMA MATRIZ (313×313, 26 colores) pero con TRES efectos
que no se pueden dejar de ver:

  1) EL ALIENTO — TODO el códice respira: brillo global 0.84..1.18 (la
     tinta late en cada píxel de color, no solo en una rampa).
  2) LA ONDA — un ANILLO de energía nace en el OJO y recorre el libro hacia
     afuera (radio 0→máximo a lo largo de los 8 frames, +50% de brillo en
     la cresta): la ola viajando se VE a cualquier tamaño.
  3) EL PARPADEO COMPLETO — el ojo central se CIERRA de verdad: frames
     5 (media), 6 (rendija 8%) y 7 (media): 3 de cada 8 frames son el
     cierre y la apertura. En la rendija el párpado queda levemente
     iluminado (la piel del ojo al cerrar).

Los frames se componen a 313×313 y se reducen (BOX + alfa binario + SNAP a
la paleta original de 26 colores) a:
  · STRIP PROYECTIL 128×1024 (8 frames de 128) — el códice que vuela.
  · STRIP ITEM 48×384 (8 frames de 48) — el icono que ANIMA en el
    inventario (Main.RegisterItemAnimation + AnimatesAsSoul).

Determinista: sin azar, sin red. Intermedios y verificación en /tmp/codice72.
Uso: python3 tools/gen_codice_vivo_v65072.py
"""
import math
import os
import re
import sys
from collections import Counter, deque

from PIL import Image

# --------------------------------------------------------------------------
# Rutas y constantes
# --------------------------------------------------------------------------
SRC_CODIGO = '/home/z/my-project/upload/codigo.txt'
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # repo raíz
OUT_STRIP = os.path.join(ROOT, 'AethonMod', 'Content', 'Projectiles',
                         'CodiceVivo', 'CodiceVivoProjectile.png')
OUT_ITEM = os.path.join(ROOT, 'AethonMod', 'Content', 'Weapons',
                        'CodiceVivo', 'CodiceVivo.png')
TMP = '/tmp/codice72'

LADO = 313          # lado de la matriz fuente
N_FRAMES = 8
PROY_LADO = 128     # frame del strip del proyectil
ITEM_LADO = 48      # frame del strip del item

# EL ALIENTO: onda TRIANGULAR (paso CONSTANTE entre frames — el seno dejaba
# dos frames casi gemelos en el pico y el snap a paleta se los comía)
ALIENTO_TRING = [-1.0, -0.5, 0.0, 0.5, 1.0, 0.5, 0.0, -0.5]
ALIENTO_BASE = 1.00
ALIENTO_AMP = 0.24          # 0.76 .. 1.24 — paso de 0.12 por frame
ALIENTO = [ALIENTO_BASE + ALIENTO_AMP * v for v in ALIENTO_TRING]

# EL PARPADEO: fracción viva del ojo por frame (None = abierto)
PARPADEO = {5: 0.55, 6: 0.08, 7: 0.55}

# LA ONDA: radio de la cresta (px, desde el centro del ojo) por frame y ancho
ONDA_RADIO = [30 + 45 * f for f in range(N_FRAMES)]   # 30..345, salto de 45
ONDA_SIGMA = 26.0
ONDA_PICO = 0.55     # cuánto se tiñe la cresta hacia la tinta viva

# --------------------------------------------------------------------------
# Parseo del sprite definido por código (idéntico al fuente del usuario)
# --------------------------------------------------------------------------
def parsear():
    with open(SRC_CODIGO, encoding='utf-8') as fh:
        txt = fh.read()
    mpal = re.search(r'const\s+PALETA\s*=\s*\{(.*?)\};', txt, re.S)
    if not mpal:
        raise SystemExit('PALETA no encontrada en ' + SRC_CODIGO)
    pal = {k: tuple(int(v[i:i + 2], 16) for i in (0, 2, 4))
           for k, v in re.findall(r"([a-z])\s*:\s*'#([0-9a-fA-F]{6})'", mpal.group(1))}
    mpix = re.search(r'const\s+PIXELES\s*=\s*\[(.*?)\];', txt, re.S)
    if not mpix:
        raise SystemExit('PIXELES no encontrada en ' + SRC_CODIGO)
    filas = re.findall(r'"([^"]*)"', mpix.group(1))
    # la matriz del usuario: 313 filas de 313 chars (canvas cuadrado)
    if len(filas) != LADO or any(len(f) != LADO for f in filas):
        raise SystemExit('dimensiones inesperadas: %d filas' % len(filas))
    if len(pal) != 26:
        raise SystemExit('paleta inesperada: %d colores' % len(pal))
    usados = set(''.join(filas)) - {'.'}
    if not usados <= set(pal):
        raise SystemExit('claves fuera de paleta: %s' % (usados - set(pal)))
    return pal, filas


def luminancia(rgb):
    return 0.2126 * rgb[0] + 0.7152 * rgb[1] + 0.0722 * rgb[2]


# --------------------------------------------------------------------------
# Detección del OJO CENTRAL (la escalera de la .71: casi-blancos con
# validación de forma; la mayor CC de luminancia>180 es el ojo real —
# el "swirl" de las páginas es alargado y no pasa la prueba de amígdalo)
# --------------------------------------------------------------------------
def componentes_conexas(filas, conjunto):
    vistos = [[False] * LADO for _ in range(LADO)]
    comps = []
    for y in range(LADO):
        for x in range(LADO):
            if filas[y][x] in conjunto and not vistos[y][x]:
                cola = deque([(y, x)])
                vistos[y][x] = True
                pix = []
                while cola:
                    cy, cx = cola.popleft()
                    pix.append((cy, cx))
                    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        ny, nx = cy + dy, cx + dx
                        if 0 <= ny < LADO and 0 <= nx < LADO \
                                and filas[ny][nx] in conjunto and not vistos[ny][nx]:
                            vistos[ny][nx] = True
                            cola.append((ny, nx))
                comps.append(pix)
    return sorted(comps, key=len, reverse=True)


def bbox_de(pix):
    ys = [p[0] for p in pix]
    xs = [p[1] for p in pix]
    return min(xs), min(ys), max(xs), max(ys)


def forma_ojo(pix):
    x0, y0, x1, y1 = bbox_de(pix)
    w, h = x1 - x0 + 1, y1 - y0 + 1
    if len(pix) < 80 or w < 6 or h < 6:
        return False
    aspecto = w / float(h)
    fill = len(pix) / float(w * h)
    return 0.35 <= aspecto <= 3.0 and fill >= 0.25


def detectar_ojo(pal, filas):
    """Devuelve (mascara_ojo, bbox, letra_parpaado)."""
    # colores claros (luminancia > 170): esclerótica y brillos del ojo
    claros = {k for k, v in pal.items() if luminancia(v) > 170}
    ojo = None
    for c in componentes_conexas(filas, claros):
        if forma_ojo(c):
            ojo = c
            break
    if ojo is None:
        raise SystemExit('ojo central no detectado')

    x0, y0, x1, y1 = bbox_de(ojo)
    # máscara ANATÓMICA: todo píxel no vacío dentro del bbox del ojo
    mascara = set((y, x) for y in range(y0, y1 + 1) for x in range(x0, x1 + 1)
                  if filas[y][x] != '.')
    centro = ((x0 + x1) * 0.5, (y0 + y1) * 0.5)

    # PÁRPADO: el color OSCURO más frecuente del anillo de 8 px alrededor
    R = 8
    anillo = Counter()
    for y in range(max(0, y0 - R), min(LADO, y1 + R + 1)):
        for x in range(max(0, x0 - R), min(LADO, x1 + R + 1)):
            ch = filas[y][x]
            if ch == '.' or (y, x) in mascara:
                continue
            if luminancia(pal[ch]) < 120:
                anillo[ch] += 1
    if not anillo:
        raise SystemExit('anillo sin colores oscuros')
    parpado = anillo.most_common(1)[0][0]
    return mascara, (x0, y0, x1, y1), parpado, centro, anillo.most_common(3)


# --------------------------------------------------------------------------
# Render de UN frame (f: 0..7) a 313×313 RGBA
# --------------------------------------------------------------------------
def clamp255(v):
    return 0 if v < 0 else (255 if v > 255 else int(round(v)))


def render_frame(f, pal, filas, mascara_ojo, bbox_ojo, parpado, centro):
    aliento = ALIENTO[f]
    radio = ONDA_RADIO[f]
    dos_sigma2 = 2.0 * ONDA_SIGMA * ONDA_SIGMA
    tinta = pal['q']          # #f0a9f6 — la tinta viva de la cresta
    parp_base = pal[parpado]

    # franja viva del ojo en los frames de parpadeo
    _, y0, _, y1 = bbox_ojo
    h = y1 - y0 + 1
    if f in PARPADEO:
        frac = PARPADEO[f]
        a = y0 + int(math.ceil((0.5 - frac / 2.0) * h))
        b = y0 + int(math.floor((0.5 + frac / 2.0) * h))
        if b < a:
            b = a
    else:
        a, b = -1, -1

    rgb_parpado = parp_base
    alto = len(filas)
    img = Image.new('RGBA', (LADO, alto), (0, 0, 0, 0))
    px = img.load()
    cx, cy = centro

    for y in range(alto):
        fila = filas[y]
        for x in range(LADO):
            ch = fila[x]
            if ch == '.':
                continue
            # (3) EL PARPADEO: fuera de la franja viva, PÁRPADO
            if f in PARPADEO and (y, x) in mascara_ojo and not (a <= y <= b):
                # la piel del párpado al cerrar: levemente iluminada
                px[x, y] = (clamp255(rgb_parpado[0] * 1.18),
                            clamp255(rgb_parpado[1] * 1.18),
                            clamp255(rgb_parpado[2] * 1.18), 255)
                continue
            base = pal[ch]
            # (1) EL ALIENTO — todo el libro respira
            k = aliento
            r, g, b = base[0] * k, base[1] * k, base[2] * k
            # (2) LA ONDA — anillo de TINTA VIVA que viaja desde el ojo:
            # en la cresta, el píxel se MEZCLA hacia el violeta claro
            d = math.hypot(x - cx, y - cy)
            mezcla = ONDA_PICO * math.exp(-((d - radio) ** 2) / dos_sigma2)
            if mezcla > 0.02:
                r += (tinta[0] - r) * mezcla
                g += (tinta[1] - g) * mezcla
                b += (tinta[2] - b) * mezcla
            px[x, y] = (clamp255(r), clamp255(g), clamp255(b), 255)
    return img


# --------------------------------------------------------------------------
# Downscale pixel-art (BOX + alfa binario + SNAP a la paleta original)
# --------------------------------------------------------------------------
def reducir(im, lado, paleta_rgb):
    chico = im.resize((lado, lado), Image.BOX).convert('RGBA')
    out = Image.new('RGBA', (lado, lado), (0, 0, 0, 0))
    po, pc = chico.load(), out.load()
    for y in range(lado):
        for x in range(lado):
            r, g, b, a = po[x, y]
            if a < 128:
                continue
            mejor = min(paleta_rgb,
                        key=lambda c: (c[0] - r) ** 2 + (c[1] - g) ** 2 + (c[2] - b) ** 2)
            pc[x, y] = mejor + (255,)
    return out


# --------------------------------------------------------------------------
# MAIN
# --------------------------------------------------------------------------
def main():
    os.makedirs(TMP, exist_ok=True)
    pal, filas = parsear()
    mascara, bbox, parpado, centro, top_anillo = detectar_ojo(pal, filas)
    print('OJO: bbox=%s centro=(%.0f,%.0f) parpado=%r %s (anillo: %s)' % (
        bbox, centro[0], centro[1], parpado, pal[parpado], top_anillo))
    paleta_rgb = list(pal.values())

    frames = [render_frame(f, pal, filas, mascara, bbox, parpado, centro)
              for f in range(N_FRAMES)]
    for f, im in enumerate(frames):
        im.save(os.path.join(TMP, 'frame_%d.png' % f))

    # STRIPS
    proy = [reducir(im, PROY_LADO, paleta_rgb) for im in frames]
    item = [reducir(im, ITEM_LADO, paleta_rgb) for im in frames]

    strip_p = Image.new('RGBA', (PROY_LADO, PROY_LADO * N_FRAMES), (0, 0, 0, 0))
    strip_i = Image.new('RGBA', (ITEM_LADO, ITEM_LADO * N_FRAMES), (0, 0, 0, 0))
    for f in range(N_FRAMES):
        strip_p.paste(proy[f], (0, f * PROY_LADO))
        strip_i.paste(item[f], (0, f * ITEM_LADO))
    strip_p.save(OUT_STRIP)
    strip_i.save(OUT_ITEM)
    print('strips: %s (%s) · %s (%s)' % (
        OUT_STRIP, strip_p.size, OUT_ITEM, strip_i.size))

    # VERIFICACIÓN 1: los frames DIFIEREN de verdad (la lección de la .71)
    import hashlib
    hashes = [hashlib.md5(fr.tobytes()).hexdigest() for fr in proy]
    assert len(set(hashes)) == N_FRAMES, 'HAY FRAMES IDÉNTICOS'
    for f in range(N_FRAMES - 1):
        dif = sum(1 for p1, p2 in zip(proy[f].getdata(), proy[f + 1].getdata())
                  if p1 != p2)
        pct = 100.0 * dif / (PROY_LADO * PROY_LADO)
        print('  f%d→f%d: %5.1f%% de píxeles cambian' % (f, f + 1, pct))
        if pct < 8.0:
            raise SystemExit('frame %d demasiado parecido al anterior (%.1f%%)' % (f, pct))

    # VERIFICACIÓN 2: el ojo cierra (frames 5/6/7 vs 0)
    x0, y0, x1, y1 = bbox
    esc = PROY_LADO / float(LADO)
    caja = (int(x0 * esc), int(y0 * esc), min(int((x1 + 1) * esc), PROY_LADO - 1),
            min(int((y1 + 1) * esc), PROY_LADO - 1))
    def claros_ojo(im):
        n = 0
        for y in range(caja[1], caja[3] + 1):
            for x in range(caja[0], caja[2] + 1):
                if 0 <= x < im.width and 0 <= y < im.height:
                    r, g, b, a = im.getpixel((x, y))
                    if a and (0.2126 * r + 0.7152 * g + 0.0722 * b) > 150:
                        n += 1
        return n
    abierto = claros_ojo(proy[0])
    cerrado = claros_ojo(proy[6])
    print('  ojo: %d px claros abierto vs %d en la rendija (caja %s)' % (
        abierto, cerrado, caja))
    if abierto <= 0 or cerrado > abierto * 0.35:
        raise SystemExit('el parpadeo no cierra el ojo suficiente')

    # VERIFICACIÓN 3: contact sheet + GIF (para el ojo humano y el VLM)
    hoja = Image.new('RGB', (PROY_LADO * N_FRAMES + 10 * (N_FRAMES + 1), PROY_LADO + 20),
                     (24, 20, 28))
    for f in range(N_FRAMES):
        hoja.paste(proy[f], (10 + f * (PROY_LADO + 10), 10))
    hoja.save(os.path.join(TMP, 'contact_128.png'))
    gif = frames[0].resize((256, 256), Image.NEAREST)
    gif.save(os.path.join(TMP, 'codice_anim.gif'), save_all=True, append_images=[
        frames[i].resize((256, 256), Image.NEAREST) for i in range(1, N_FRAMES)],
        duration=110, loop=0)
    print('verificación: %s/contact_128.png + codice_anim.gif' % TMP)
    print('OK — animación BOLD: aliento %.2f..%.2f · onda %d..%d px · parpadeo %s' % (
        min(ALIENTO), max(ALIENTO), ONDA_RADIO[0], ONDA_RADIO[-1],
        {k: v for k, v in sorted(PARPADEO.items())}))


if __name__ == '__main__':
    main()
