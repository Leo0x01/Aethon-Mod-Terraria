#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_codice_vivo_v65071.py — EL CÓDICE VIVO (arma mágica) · pipeline de render
y animación desde el sprite DEFINIDO POR CÓDIGO.

Fuente: /home/z/my-project/upload/codigo.txt — objeto PALETA (26 colores,
claves a..z) + matriz PIXELES (313 filas de 313 chars; '.' = pixel apagado
= alfa 0; letra = color de la paleta con alfa 255). El PNG original del
usuario (Designer - 2026-10-06T213044.674.png) es el MISMO sprite a 4x con
capa semitransparente; el render por código lo reproduce a alfa plena
(verificado por VLM: mismo grimorio).

Assets producidos (los que usa el mod):
  1) AethonMod/Content/Projectiles/CodiceVivo/CodiceVivoProjectile.png
     STRIP vertical 128×1024: 8 frames de 128×128 (frame 0 arriba … 7 abajo,
     sin espacios). Dos animaciones simultáneas sobre la MATRIZ (313×313):
       a) PULSO DE ENERGÍA — la RAMPA de 9 tonos vivos (mismos miembros que
          la lista de la especificación, ordenada por luminancia DESCENDENTE:
          c,q,i,u,j,k,m,l,z) late en onda: brillo *= 0.72+0.28·sen(2π(f/8 +
          idx/9)); escala RGB proporcional (mantiene el matiz, clamp 0-255).
          Los colores fuera de la rampa quedan intactos.
       b) PARPADEO (frames 6-7) — el OJO CENTRAL cierra: los píxeles del ojo
          por encima/debajo de la franja central viva se pintan con el COLOR
          DE PÁRPADO (color oscuro más frecuente en el anillo de 8px alrededor
          del ojo). Frame 6: franja 30% del alto; frame 7: 12% (rendija).
          Frames 0-5: ojo abierto. Solo el ojo central: ni satélites ni runas.
  2) AethonMod/Content/Weapons/CodiceVivo/CodiceVivo.png
     ICONO de item 40×40 desde el frame 0 (ojo abierto). Si la legibilidad
     VLM < 6/10 se aplican los MODOS DE REFUERZO del ojo (ver generar_icono).

Detección del ojo central (documentada, determinista):
  - CC (BFS 4-conectado) de casi-blancos {c,r}; la mayor (64×15, "swirl" de
    las páginas) FALLA la validación de forma amígdalo-compacta (aspecto
    0.35..3.0, fill>=0.25) => no es un ojo (es el brillo especular de las
    páginas, confirmado por VLM).
  - Se relaja el umbral de casi-blanco a luminancia>180 {c,r,q,i}: la mayor
    CC pasa a ser el OJO CENTRAL real (19×31, almendra vertical con esclerótica
    blanca y pupila-slit magenta; VLM: ojo mayor y más central del sprite).
  - Máscara de parpadeo = píxeles no vacíos dentro del bbox de esa CC
    (anatomía completa del ojo: puntas + esclerótica + iris).
  - Parpado = color oscuro (fuera de rampa y de blancos) más frecuente en el
    anillo de 8px alrededor del ojo.

Downscale 313->128/40: Image.BOX + binarización de alfa (>=128 -> 255) +
SNAP a la paleta original de 26 colores (distancia euclídea RGB) = pixel-art
limpio; el alfa 0 se conserva (zonos transparentes siguen transparentes).

Determinista: sin random, sin red. Intermedios (frames 313, contact sheet,
side-by-side) en /tmp/codice/. Uso:
  python3 gen_codice_vivo_v65071.py [--icon-contrast 1|2|3]
    --icon-contrast: refuerzo de contraste del ICONO (1=ninguno por defecto;
    2/3 = remedio VLM si la legibilidad < 6/10).
"""
import argparse
import hashlib
import math
import os
import re
import sys
from collections import Counter, deque

from PIL import Image, ImageDraw

# --------------------------------------------------------------------------
# Rutas
# --------------------------------------------------------------------------
SRC_CODIGO = '/home/z/my-project/upload/codigo.txt'
SRC_PNG_ORIGINAL = '/home/z/my-project/upload/Designer - 2026-10-06T213044.674.png'
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # repo raíz
DIR_PROY = os.path.join(ROOT, 'AethonMod', 'Content', 'Projectiles', 'CodiceVivo')
DIR_ARMA = os.path.join(ROOT, 'AethonMod', 'Content', 'Weapons', 'CodiceVivo')
OUT_STRIP = os.path.join(DIR_PROY, 'CodiceVivoProjectile.png')
OUT_ICONO = os.path.join(DIR_ARMA, 'CodiceVivo.png')
TMP = '/tmp/codice'

N_FRAMES = 8
LADO = 313  # lado de la matriz fuente
STRIP_LADO = 128   # lado de cada frame del strip
ICONO_LADO = 40    # lado del icono de item

# Parpadeo: franja central viva (fracción del alto del ojo) por frame
FRANJA = {6: 0.30, 7: 0.12}
FRAMES_PARPADEO = (6, 7)

# --------------------------------------------------------------------------
# Parseo del sprite definido por código
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


def clamp255(v):
    return 0 if v < 0 else (255 if v > 255 else v)


# --------------------------------------------------------------------------
# Detección del ojo central + color de párpado
# --------------------------------------------------------------------------
def componentes_conexas(filas, conjunto):
    """CC (BFS 4-conectado) de las celdas cuyo char esta en `conjunto`."""
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
    """True si la CC lee como amígdalo compacto (no como curva/rayo)."""
    x0, y0, x1, y1 = bbox_de(pix)
    w, h = x1 - x0 + 1, y1 - y0 + 1
    if len(pix) < 80 or w < 6 or h < 6:
        return False
    aspecto = w / h
    fill = len(pix) / float(w * h)
    return 0.35 <= aspecto <= 3.0 and fill >= 0.25


def detectar_ojo(pal, filas, rampa, blancos_extra):
    """Devuelve (mascara_parpadeo, bbox, color_parpaado, bitacora).

    Escalera de umbrales de casi-blanco (la especificación pide >=30px de
    ancho con relajación a luminancia>200; en este sprite la mayor CC de
    {c,r} es el brillo especular de las PÁGINAS (64×15, aspecto 4.27) y no
    un ojo, así que se valida por forma y se relaja a luminancia>180, con lo
    que la mayor CC pasa a ser el OJO CENTRAL real).
    """
    log = []
    escalera = [('casi-blancos {c,r}', {'c', 'r'})]
    if blancos_extra:
        escalera.append(('luminancia>180 {c,r,q,i}', blancos_extra))
    ojo = None
    for nombre, conjunto in escalera:
        comps = componentes_conexas(filas, conjunto)
        log.append('[umbral %s] %d CC; top: %s' % (
            nombre, len(comps),
            '; '.join('CC%d %dpx bbox=%s aspect=%.2f fill=%.2f%s' % (
                i, len(c), bbox_de(c),
                (bbox_de(c)[2] - bbox_de(c)[0] + 1) / float(bbox_de(c)[3] - bbox_de(c)[1] + 1),
                len(c) / float((bbox_de(c)[2] - bbox_de(c)[0] + 1) * (bbox_de(c)[3] - bbox_de(c)[1] + 1)),
                ' <=OJO' if forma_ojo(c) else '')
                for i, c in enumerate(comps[:3]))))
        for c in comps:
            if forma_ojo(c):
                ojo = (nombre, c)
                break
        if ojo:
            break
    if ojo is None:
        raise SystemExit('ojo central no detectado')
    nombre, cc_ojo = ojo
    log.append('OJO CENTRAL = mayor CC valida bajo %s: %dpx bbox=%s' % (
        nombre, len(cc_ojo), bbox_de(cc_ojo)))

    # Color de párpado: oscuro (fuera de rampa/blancos) mas frecuente en el
    # anillo de 8px alrededor del ojo.
    x0, y0, x1, y1 = bbox_de(cc_ojo)
    mascara = set(cc_ojo)
    R = 8
    anillo = Counter()
    for y in range(max(0, y0 - R), min(LADO, y1 + R + 1)):
        for x in range(max(0, x0 - R), min(LADO, x1 + R + 1)):
            ch = filas[y][x]
            if ch == '.' or (y, x) in mascara:
                continue
            if any((yy, xx) in mascara
                   for yy in range(y - R, y + R + 1)
                   for xx in range(x - R, x + R + 1)
                   if 0 <= yy < LADO and 0 <= xx < LADO):
                anillo[ch] += 1
    fuera = set(rampa) | {'r'}
    oscuros = [(k, v) for k, v in anillo.most_common() if k not in fuera]
    if not oscuros:
        raise SystemExit('anillo sin colores oscuros')
    llave, n = oscuros[0]
    parpado = llave
    log.append('anillo 8px: oscuros top=%s => COLOR DE PÁRPADO %r %s (x%d)' % (
        [(k, v) for k, v in oscuros[:4]], llave, pal[llave], n))

    # Mascara de parpadeo: anatomía completa del ojo (todo pixel no vacío
    # dentro del bbox de la CC del ojo, esclerótica + iris + puntas).
    x0, y0, x1, y1 = bbox_de(cc_ojo)
    mascara_ojo = [(y, x) for y in range(y0, y1 + 1) for x in range(x0, x1 + 1)
                   if filas[y][x] != '.']
    return mascara_ojo, (x0, y0, x1, y1), parpado, log


def banda_viva(bbox, frac):
    """Filas [a,b] de la franja central viva (fracción del alto del ojo)."""
    _, y0, _, y1 = bbox
    h = y1 - y0 + 1
    a = y0 + int(math.ceil((0.5 - frac / 2.0) * h))
    b = y0 + int(math.floor((0.5 + frac / 2.0) * h))
    if b < a:
        b = a
    return a, b


# --------------------------------------------------------------------------
# Render de frames
# --------------------------------------------------------------------------
def color_pulsado(rgb, factor):
    return (clamp255(int(round(rgb[0] * factor))),
            clamp255(int(round(rgb[1] * factor))),
            clamp255(int(round(rgb[2] * factor))))


def render_frame(f, pal, filas, rampa, mascara_ojo, bbox_ojo, parpado,
                 ojo_pleno=False):
    """Frame f (0..7) a 313×313 RGBA: pulso de energia + parpadeo (6-7).

    `parpado` (letra de la paleta) se recibe como color de párpado y pinta
    los píxeles del ojo fuera de la franja central viva. Con `ojo_pleno` los
    colores de la rampa DENTRO del ojo van a brillo pico (factor 1.0; solo lo
    usa el icono para maximizar el contraste del ojo).
    """
    factores = {}
    for idx, letra in enumerate(rampa):
        factores[letra] = 0.72 + 0.28 * math.sin(2.0 * math.pi * (f / 8.0 + idx / 9.0))
    img = Image.new('RGBA', (LADO, LADO), (0, 0, 0, 0))
    px = img.load()
    dentro = set(mascara_ojo)
    if f in FRAMES_PARPADEO:
        a, b = banda_viva(bbox_ojo, FRANJA[f])
    else:
        a, b = -1, -1
    rgb_parpado = pal[parpado] + (255,) if parpado else None
    for y in range(LADO):
        fila = filas[y]
        for x in range(LADO):
            ch = fila[x]
            if ch == '.':
                continue
            if f in FRAMES_PARPADEO and (y, x) in dentro and not (a <= y <= b):
                px[x, y] = rgb_parpado
                continue
            fac = factores.get(ch)
            if ojo_pleno and fac and (y, x) in dentro:
                fac = 1.0
            px[x, y] = (color_pulsado(pal[ch], fac) + (255,)) if fac else (pal[ch] + (255,))
    return img


# --------------------------------------------------------------------------
# Downscale: BOX + binarización de alfa + snap a la paleta original
# --------------------------------------------------------------------------
def _lista_paleta(pal):
    return sorted(pal.items(), key=lambda kv: kv[0])  # orden fijo a..z


def snap_a_paleta(im, pal):
    """Snap RGB a la paleta original (euclídea); conserva alfa 0."""
    items = _lista_paleta(pal)
    colores = [v for _, v in items]
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            mejor, mejor_d = None, None
            for c in colores:
                d = (c[0] - r) ** 2 + (c[1] - g) ** 2 + (c[2] - b) ** 2
                if mejor_d is None or d < mejor_d:
                    mejor, mejor_d = c, d
            px[x, y] = (mejor[0], mejor[1], mejor[2], 255)
    return im


def binarizar_alfa(im, umbral=128):
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            px[x, y] = (r, g, b, 255) if a >= umbral else (0, 0, 0, 0)
    return im


def reducir(im, lado, pal):
    """313 -> lado con BOX, alfa binario (>=128) y snap a la paleta."""
    chico = im.resize((lado, lado), Image.BOX)
    binarizar_alfa(chico)
    snap_a_paleta(chico, pal)
    return chico


def generar_icono(pal, filas, rampa, mascara, bbox, modo):
    """Icono de item 40×40 desde el frame 0 (ojo abierto), BOX + snap.

    Modos de refuerzo del CONTRASTE DEL OJO (remedio VLM si legibilidad <6/10,
    máximo 3 rondas):
      1 = frame_0 tal cual;
      2 = fuente con el ojo a brillo pico + ganancia ×2.0 del bloque del ojo
          tras el BOX (la esclerótica de 3-4 px no sobrevive sola al promedio
          BOX 313→40; a ×2 clampa a blanco/magenta vivo y el ojo queda como
          foco luminoso del icono);
      3 = modo 2 con bloque del ojo quirúrgico (claro→blanco c lum>105,
          medio→magenta m lum>40, oscuro→casi negro a) + OSCURECIDO SELECTIVO
          del resto: solo los tonos medios GRISÁCEOS del cuerpo del libro
          (luminancia<=170 y croma<=80, p.ej. e/g/h/j) se atenúan ×0.45; los
          vivos (m/k/l/f/q, croma>80) y los claros (luminancia>170) se
          conservan = silueta de libro oscuro sólido con páginas encendidas
          y ojo blanco.
      4 = modo 3 pero con EL OJO A SU PROPIA ESCALA: la región del ojo
          (bbox+1px) se reduce aparte con BOX 19×31→5×8 (la esclerótica no
          sobrevive al promedio del frame completo) y se compone centrada en
          la posición real del ojo, con el mismo umbral quirúrgico = almendra
          blanca de 5×8 con slit magenta/oscuro, protagonista del icono.
    Todo determinista; el resto del icono es frame_0 BOX puro.
    """
    if modo == 1:
        src = render_frame(0, pal, filas, rampa, mascara, bbox, None)
        chico = src.resize((ICONO_LADO, ICONO_LADO), Image.BOX)
        en_bloque = lambda x, y: False
    else:
        src = render_frame(0, pal, filas, rampa, mascara, bbox, None, ojo_pleno=True)
        chico = src.resize((ICONO_LADO, ICONO_LADO), Image.BOX)
        x0, y0, x1, y1 = bbox
        esc = ICONO_LADO / float(LADO)
        bx0, by0 = int(x0 * esc), int(y0 * esc)
        bx1 = min(ICONO_LADO - 1, int((x1 + 1) * esc))
        by1 = min(ICONO_LADO - 1, int((y1 + 1) * esc))
        en_bloque = lambda x, y: bx0 <= x <= bx1 and by0 <= y <= by1
        p = chico.load()
        for y in range(by0, by1 + 1):
            for x in range(bx0, bx1 + 1):
                r, g, b, a = p[x, y]
                if not a:
                    continue
                if modo == 2:
                    p[x, y] = (clamp255(r * 2), clamp255(g * 2), clamp255(b * 2), 255)
                else:
                    lum = luminancia((r, g, b))
                    if lum > 105:
                        p[x, y] = pal['c'] + (255,)
                    elif lum > 40:
                        p[x, y] = pal['m'] + (255,)
                    else:
                        p[x, y] = pal['a'] + (255,)
    if modo == 3:
        p = chico.load()
        for y in range(ICONO_LADO):
            for x in range(ICONO_LADO):
                if en_bloque(x, y):
                    continue
                r, g, b, a = p[x, y]
                if not a:
                    continue
                lum = luminancia((r, g, b))
                croma = max(r, g, b) - min(r, g, b)
                if lum <= 170 and croma <= 80:
                    p[x, y] = (int(r * 0.45), int(g * 0.45), int(b * 0.45), 255)
    if modo == 4:
        p = chico.load()
        for y in range(ICONO_LADO):
            for x in range(ICONO_LADO):
                r, g, b, a = p[x, y]
                if not a:
                    continue
                lum = luminancia((r, g, b))
                croma = max(r, g, b) - min(r, g, b)
                if lum <= 170 and croma <= 80:
                    p[x, y] = (int(r * 0.45), int(g * 0.45), int(b * 0.45), 255)
        x0, y0, x1, y1 = bbox
        ojo = src.crop((x0 - 1, y0 - 1, x1 + 2, y1 + 2)).resize((5, 8), Image.BOX)
        op = ojo.load()
        for y in range(8):
            for x in range(5):
                r, g, b, a = op[x, y]
                if not a:
                    continue
                lum = luminancia((r, g, b))
                op[x, y] = (pal['c'] + (255,)) if lum > 105 else \
                    ((pal['m'] + (255,)) if lum > 40 else (pal['a'] + (255,)))
        cx = (x0 + x1 + 1) / 2.0 * ICONO_LADO / float(LADO)
        cy = (y0 + y1 + 1) / 2.0 * ICONO_LADO / float(LADO)
        px0 = int(round(cx - 2.5))
        py0 = int(round(cy - 4.0))
        for y in range(8):
            for x in range(5):
                r, g, b, a = op[x, y]
                if a and 0 <= py0 + y < ICONO_LADO and 0 <= px0 + x < ICONO_LADO:
                    p[px0 + x, py0 + y] = (r, g, b, 255)
    binarizar_alfa(chico)
    snap_a_paleta(chico, pal)
    return chico


# --------------------------------------------------------------------------
# Verificaciones
# --------------------------------------------------------------------------
def md5_bytes(b):
    return hashlib.md5(b).hexdigest()


def verificar_strip(ruta, pal):
    im = Image.open(ruta)
    assert im.size == (STRIP_LADO, STRIP_LADO * N_FRAMES), 'strip %s' % (im.size,)
    hashes = []
    for f in range(N_FRAMES):
        rec = im.crop((0, f * STRIP_LADO, STRIP_LADO, (f + 1) * STRIP_LADO))
        hashes.append(md5_bytes(rec.tobytes()))
    todos_iguales = len(set(hashes)) == 1
    assert not todos_iguales, 'los 8 frames del strip son identicos'
    pares = [(f, g) for f in range(N_FRAMES) for g in range(f + 1, N_FRAMES)
             if hashes[f] == hashes[g]]
    print('  strip %s: %dx%d OK; md5 frames %s; colisiones entre frames: %s' % (
        os.path.basename(ruta), im.size[0], im.size[1],
        ['%s..' % h[:8] for h in hashes], pares if pares else 'ninguna'))
    return hashes


def verificar_ojo_matrices(m0, m7, mascara, bbox, parpado):
    """Diferencia frame_0 vs frame_7 dentro del bbox del ojo, a nivel MATRIZ."""
    dentro = set(mascara)
    _, y0, _, y1 = bbox
    a, b = banda_viva(bbox, FRANJA[7])
    changed = 0
    vivos_banda = 0
    for (y, x) in mascara:
        c0, c7 = m0[y][x], m7[y][x]
        if c0 != c7:
            changed += 1
        if a <= y <= b and c7 != parpado:
            vivos_banda += 1
    total = len(mascara)
    fuera_banda = sum(1 for (y, x) in mascara if not (a <= y <= b))
    parpadeados = sum(1 for (y, x) in mascara if not (a <= y <= b) and m7[y][x] == parpado)
    print('  ojo (matriz): bbox=%s mascara=%dpx; f0 vs f7 cambian %d/%d (%.0f%%); '
          'fuera de banda f7 con parpado %d/%d; banda viva con brillo %d/%d px' % (
              bbox, total, changed, total, 100.0 * changed / total,
              fuera_banda, parpadeados, vivos_banda,
              sum(1 for (y, x) in mascara if a <= y <= b)))
    assert changed > total * 0.5, 'el parpadeo no cambia el ojo (matriz)'
    assert parpadeados > fuera_banda * 0.9, 'la zona fuera de banda no es parpado'
    assert vivos_banda >= (b - a + 1), 'la banda viva perdio el brillo'


def matrices_frame(img):
    """Matriz[y][x] = letra equivalente NO usada; aqui: tupla RGB o None."""
    px = img.load()
    return [[px[x, y][:3] if px[x, y][3] else None for x in range(LADO)]
            for y in range(LADO)]


# --------------------------------------------------------------------------
# Main
# --------------------------------------------------------------------------
def main():
    ap = argparse.ArgumentParser(description='Generador Códice Vivo (render+animación)')
    ap.add_argument('--icon-mode', type=int, default=1, choices=(1, 2, 3, 4),
                    help='modo del icono: 1=frame_0 puro; 2=ojo a brillo pico + '
                         'ganancia ×2 del bloque del ojo; 3=umbral quirúrgico del ojo '
                         '+ oscurecido selectivo; 4=ojo compuesto a su propia escala')
    args = ap.parse_args()

    os.makedirs(TMP, exist_ok=True)
    os.makedirs(DIR_PROY, exist_ok=True)
    os.makedirs(DIR_ARMA, exist_ok=True)

    pal, filas = parsear()
    print('parse OK: 26 colores, matriz 313×313')

    # --- rampa de energia (verificada por luminancia; orden DESC por brillo):
    # mismos 9 miembros que la especificacion, ordenados para que la onda
    # recorra del tono mas vivo al mas profundo y el frame 0 quede con los
    # blancos altos (frame heroe del icono).
    rampa = [k for k, _ in sorted(
        [('c', 0), ('q', 0), ('i', 0), ('u', 0), ('j', 0), ('k', 0), ('m', 0), ('l', 0), ('z', 0)],
        key=lambda kv: -luminancia(pal[kv[0]]))]
    assert sorted(rampa) == sorted(list('qkmluzcij')), 'rampa != especificacion'
    print('rampa (luminancia DESC):', ' '.join('%s#%02x%02x%02x(%d)' % (c, pal[c][0], pal[c][1], pal[c][2], luminancia(pal[c])) for c in rampa))

    # --- frame base + verificacion visual contra el original
    base = render_frame(0, pal, filas, rampa, [], (0, 0, 0, 0), None)
    base.save(os.path.join(TMP, 'frame_base.png'))
    original = Image.open(SRC_PNG_ORIGINAL).convert('RGBA')
    lado = min(original.size)
    original = original.crop((0, 0, lado, lado)).resize((LADO, LADO), Image.NEAREST)
    comp = Image.new('RGBA', (LADO * 2 + 10, LADO), (28, 28, 38, 255))
    comp.paste(original, (0, 0), original)
    comp.paste(base, (LADO + 10, 0), base)
    comp.save(os.path.join(TMP, 'side_by_side.png'))
    print('frame_base + side_by_side en', TMP)

    # --- deteccion del ojo
    blanco_extra = {k for k in pal if luminancia(pal[k]) > 180}
    mascara, bbox, parpado, log = detectar_ojo(pal, filas, rampa, blanco_extra)
    for linea in log:
        print(' ', linea)
    print('  franja viva f6 (30%%): filas %s; f7 (12%%): filas %s' % (
        banda_viva(bbox, FRANJA[6]), banda_viva(bbox, FRANJA[7])))
    print('  parpado %r %s' % (parpado, pal[parpado]))

    # --- 8 frames 313
    frames = []
    for f in range(N_FRAMES):
        im = render_frame(f, pal, filas, rampa, mascara, bbox, parpado)
        im.save(os.path.join(TMP, 'frame_%d.png' % f))
        frames.append(im)
    print('frames 313 en', TMP)

    # --- contact sheet 4×2 con etiquetas
    cw, ch, barra = LADO, LADO, 18
    hoja = Image.new('RGB', (cw * 4, (ch + barra) * 2), (24, 24, 32))
    d = ImageDraw.Draw(hoja)
    for f in range(N_FRAMES):
        cx, cy = (f % 4) * cw, (f // 4) * (ch + barra)
        d.rectangle([cx, cy, cx + cw - 1, cy + barra - 1], fill=(0, 0, 0))
        d.text((cx + 6, cy + 3), '#%d' % f, fill=(255, 220, 80))
        hoja.paste(frames[f], (cx, cy + barra), frames[f])
    hoja.save(os.path.join(TMP, 'contact_sheet.png'))
    print('contact_sheet 4×2 en', TMP)

    # --- strip 128×1024
    strip = Image.new('RGBA', (STRIP_LADO, STRIP_LADO * N_FRAMES), (0, 0, 0, 0))
    for f in range(N_FRAMES):
        chico = reducir(frames[f], STRIP_LADO, pal)
        strip.paste(chico, (0, f * STRIP_LADO))
    strip.save(OUT_STRIP)
    print('strip ->', OUT_STRIP)

    # --- icono 40×40 (frame 0, ojo abierto) con modo de refuerzo VLM
    icono = generar_icono(pal, filas, rampa, mascara, bbox, args.icon_mode)
    icono.save(OUT_ICONO)
    print('icono (modo %d) ->' % args.icon_mode, OUT_ICONO)

    # --- verificaciones
    print('VERIFICACION:')
    verificar_strip(OUT_STRIP, pal)
    m0 = matrices_frame(frames[0])
    m7 = matrices_frame(frames[7])
    # matriz por letra: relabelfica cada pixel con la clave de paleta mas cercana
    def relabel(img):
        px = img.load()
        out = [[None] * LADO for _ in range(LADO)]
        items = _lista_paleta(pal)
        for y in range(LADO):
            for x in range(LADO):
                if px[x, y][3] == 0:
                    continue
                r, g, b = px[x, y][:3]
                llave, mejor = None, None
                for k, v in items:
                    d = (v[0] - r) ** 2 + (v[1] - g) ** 2 + (v[2] - b) ** 2
                    if mejor is None or d < mejor:
                        llave, mejor = k, d
                out[y][x] = llave
        return out
    r0, r7 = relabel(frames[0]), relabel(frames[7])
    verificar_ojo_matrices(r0, r7, mascara, bbox, parpado)
    ic = Image.open(OUT_ICONO)
    assert ic.size == (ICONO_LADO, ICONO_LADO)
    colores = set()
    for y in range(ICONO_LADO):
        for x in range(ICONO_LADO):
            r, g, b, a = ic.load()[x, y]
            if a:
                colores.add((r, g, b))
    dentro_pal = colores <= set(pal.values())
    print('  icono %s: %dx%d OK; %d colores, todos de la paleta: %s' % (
        os.path.basename(OUT_ICONO), ic.size[0], ic.size[1], len(colores), dentro_pal))
    assert dentro_pal
    print('LISTO: %s + %s' % (OUT_STRIP, OUT_ICONO))


if __name__ == '__main__':
    main()
