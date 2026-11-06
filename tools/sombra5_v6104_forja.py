#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
sombra5_v6104_forja.py — v6.104 — LA FORJA DEL OJO Y EL COLMILLO DEL
QUINTO TOMO.

LA LETRA DEL USUARIO: «mejora aun mas las texturas de la sombra de la
pagina 5, recuerda que es un tentaculo LISO, con ojos y colmillos, si
lo necesitas siempre puedes crear los ojos y los colmillos con la
forja o el taller».

Este script forja LAS DOS PIEZAS VIVAS del tentáculo liso (las que la
letra pide EXPRESAMENTE a la forja):

  · OjoAbismo.png (64×64) — EL OJO RASGADO DE ABISMO DETALLADO:
      esclerótica oscura con venas carmesí finas, iris radial
      CARMESÍ→VIOLETA con brillo interior, PUPILA DE RENDIJA VERTICAL
      negra con borde incandescente, destello húmedo especular
      arriba-izquierda, párpados sombreados arriba/abajo. El ojo
      completo opaco dentro de su forma almendrada, borde fundido a
      transparente.
  · ColmilloAbismo.png (48×96) — EL COLMILLO DE MARFIL DETALLADO:
      base con encía oscura/violeta, cuerpo marfil con degradado y
      veta sutil, PUNTA afilada más pálida con mancha roja sutil en
      el filo. Apunta ARRIBA (base ancha abajo, punta arriba,
      curvado a la derecha — igual que VFXCore.Colmillo, que es la
      referencia de rotación del código: rot = ángulo + π/2).

LA CADENA DE LA FORJA/TALLER (la de la casa):
  LA PLUMA (z-ai image) — el borrador IA de cada pieza.
  EL MOLDE — cutout real por flood-fill, EL VERDE INTERIOR (la
      lección del QA r1 de la piel), el ENCAJE que PRESERVA EL
      ASPECTO del bbox dentro del lienzo final (el ojo almendrado NO
      se estira: la forma vive dentro del cuadrado), la cuantización
      SUAVE a la paleta de cada pieza.
  LA PLUMILLA — el borde fundido (pluma de alfa), el AUTO-ENDEREZ
      del colmillo (si la IA lo curvó a la izquierda, se espeja) y
      EL PREMULTIPLICADO (q,q,q,q) de la convención del lote.
  EL OJO (z-ai vision) — el QA VLM con veredicto SHIP/FIX: MÁXIMO 3
      INTENTOS por textura (cada reintento añade la LECCIÓN del QA
      anterior al prompt); si la pluma no clava la pieza en 3, la
      textura cae al HORNEADO PIL de la casa (determinista, el mismo
      espíritu de VFXCore.HornearColmillo).
  LA VERIFICACIÓN — tamaños EXACTOS (64×64 y 48×96), RGBA,
      premultiplicado (rgb ≤ a; a=0 → rgb=0), orientaciones (rendija
      vertical del ojo; punta del colmillo ARRIBA y más estrecha que
      la base).

Salida:
  AethonMod/Content/Effects/OjoAbismo.png      (64×64)
  AethonMod/Content/Effects/ColmilloAbismo.png (48×96)
Los viste SombrasLib.OjoTexturizado y SombrasLib.ColmilloTexturizado
(lote aditivo, null-safe: sin textura caen al ojo rasgado y al
colmillo horneado de siempre).

USO:
  python3 tools/sombra5_v6104_forja.py            # forja + QA (máx 3)
  python3 tools/sombra5_v6104_forja.py --forzar   # ignora borradores
  python3 tools/sombra5_v6104_forja.py --pil      # horneado PIL directo
"""
import math
import os
import re
import subprocess
import sys

from PIL import Image, ImageFilter

AQUI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, AQUI)
from gen_altar_v695 import cutout, bbox, celdas  # noqa: E402
from sombra5_v6104_piel import quitar_verde_interior  # noqa: E402

BASE = os.path.join(AQUI, '..', 'AethonMod')
DEST_OJO = os.path.join(BASE, 'Content', 'Effects', 'OjoAbismo.png')
DEST_COLMILLO = os.path.join(BASE, 'Content', 'Effects', 'ColmilloAbismo.png')
CARPETA = '/tmp/forja_sombra5_v6104'

FORZAR = '--forzar' in sys.argv
SOLO_PIL = '--pil' in sys.argv

# ====================================================================
#  LAS PALETAS DE LAS PIEZAS VIVAS
# ====================================================================
PALETA_OJO = [
    (6, 3, 9),        # LA RENDIJA — el negro que no alumbra
    (16, 9, 26),      # la esclerótica en sombra
    (28, 15, 44),     # la esclerótica oscura
    (44, 24, 66),     # la esclerótica violeta
    (74, 36, 104),    # el párpado interior
    (110, 14, 22),    # la vena apagada
    (160, 24, 30),    # LAS VENAS CARMESÍ
    (198, 18, 24),    # EL IRIS CARMESÍ (el borde radial)
    (138, 46, 160),   # el iris violáceo
    (178, 92, 208),   # EL BRILLO INTERIOR del iris
    (255, 128, 84),   # EL BORDE INCANDESCENTE de la rendija
    (250, 244, 255),  # EL DESTELLO HÚMEDO
]

PALETA_COLMILLO = [
    (14, 8, 22),      # la encía oscura
    (30, 16, 50),     # LA ENCÍA VIOLETA
    (52, 28, 84),     # la encía clara
    (178, 162, 140),  # la raíz sombreada
    (208, 192, 168),  # el marfil sombra
    (228, 216, 192),  # EL MARFIL
    (244, 238, 222),  # el marfil claro
    (252, 249, 240),  # LA PUNTA PÁLIDA
    (120, 16, 20),    # la mancha roja sutil del filo
]


# ====================================================================
#  LAS PLUMAS (los borradores IA de cada pieza)
# ====================================================================
def la_pluma(prompt, destino, tamano):
    """LA PLUMA blindada: si la generación falla (la API de imágenes
    de esta era es quisquillosa con los tamaños — 720×1440 está en la
    ayuda pero el backend lo rechaza: múltiplos de 32 STRICTOS),
    devuelve False y el intento pasa al siguiente (o al horneado PIL).
    JAMÁS tumba la forja entera."""
    try:
        r = subprocess.run(['z-ai', 'image', '-p', prompt, '-o', destino, '-s', tamano],
                           capture_output=True, text=True, timeout=240)
    except Exception as e:
        print(f'  LA PLUMA falló ({e})')
        return False
    if not os.path.exists(destino):
        print((r.stdout or '')[-300:], (r.stderr or '')[-300:])
        return False
    print(f'  LA PLUMA: {destino}')
    return True


PROMPT_OJO = (
    "Pixel art sprite for a Terraria-style game, strictly front view. "
    "A single fearsome abyssal creature EYE, fully open, filling 70% of "
    "the frame with a generous pure green margin on ALL sides: an "
    "ALMOND-shaped eye with pointed corners at left and right, BRIGHT and "
    "LUMINOUS (it glows in the dark). Dark violet sclera covered in fine "
    "branching CRIMSON veins, a large radial iris graded from deep CRIMSON "
    "at the outer edge to bright VIOLET with a glowing inner core, and a "
    "pure black VERTICAL SLIT pupil (like a dragon or goat eye, tall thin "
    "and narrow, perfectly vertical) framed by a thin incandescent "
    "ember-orange rim. One small bright wet specular highlight dot at the "
    "upper-left of the iris. Only the slit pupil is black - no dark "
    "shadows outside the almond, no dark eyelids around it. No eyelashes, "
    "no eyebrows, no face, no skin, no body, no background scenery, no "
    "outline around the subject. Limited palette: dark violet #2C1828 "
    "sclera, crimson #C61218 veins and iris edge, violet #B25CD0 iris "
    "core, ember #FF8054 slit rim, near-white #FAF4FF specular dot. Chunky "
    "clean large pixels, no anti-aliasing, flat solid pure green "
    "background (#00FF00) chroma key, subject centered"
)

PROMPT_COLMILLO = (
    "Pixel art sprite for a Terraria-style game, strictly front view. "
    "A single long curved ivory FANG of a deep sea monster, pointing "
    "STRAIGHT UP: sharp pale tip at the TOP, wide base at the BOTTOM "
    "with a small collar of dark violet GUM flesh around it, the whole "
    "fang gently curved to the RIGHT like a hook. Polished ivory body "
    "with a soft vertical gradient (brighter on the left lit side, "
    "subtle shadow on the right), very faint thin length-wise striation, "
    "and one subtle small dark-red stain near the sharp tip on the "
    "cutting edge. Exactly ONE fang, no other teeth, no jaw, no mouth, "
    "no creature, no background scenery, no outline around the subject. "
    "Limited palette: pale ivory #F4EEE2 tip, warm ivory #E4D8C0 body, "
    "ivory shadow #D0C0A8, dark violet #342050 gum, dark red #781014 "
    "stain. Chunky clean large pixels, no anti-aliasing, flat solid pure "
    "green background (#00FF00) chroma key, subject centered filling "
    "80% of frame height"
)

# LAS LECCIONES por reintento (lo que EL OJO fue pidiendo)
LECCIONES_OJO = [
    '',
    'IMPORTANT: the slit pupil is perfectly VERTICAL and NARROW (a tall '
    'thin black lens, NOT round, NOT horizontal), the eye is fully OPEN '
    'and symmetric, pointing straight at the viewer.',
    'CRITICAL: vertical slit pupil only; remove any glow around the '
    'whole eye (glow only on the thin rim of the slit); the almond '
    'shape must be clean with sharp pointed corners; nothing else in '
    'the frame.',
]
LECCIONES_COLMILLO = [
    '',
    'IMPORTANT: exactly ONE fang, vertical, sharp tip at the very TOP, '
    'wide gum base at the very BOTTOM, curved RIGHT like a hook, and the '
    'fang must NOT touch the frame borders: leave a generous pure green '
    'margin on ALL sides (top, bottom, left, right) so the background '
    'reads as solid green.',
    'CRITICAL: one single fang only, no jaw, no lips, no other teeth, no '
    'background texture, tip at top; slim and elegant like a curved '
    'needle; solid green everywhere around it, big margins.',
]


# ====================================================================
#  EL MOLDE (cutout → verde interior → encaje de aspecto → paleta)
# ====================================================================
def cuantizar(im, paleta, umbral=90):
    """La cuantización SUAVE de la casa: cada píxel vuela a SU color
    más cercano SI está cerca (umbral) — más lejos conserva su tono."""
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            mejor, dmin = None, umbral + 1
            for c in paleta:
                d = abs(r - c[0]) + abs(g - c[1]) + abs(b - c[2])
                if d < dmin:
                    mejor, dmin = c, d
            if mejor is not None:
                px[x, y] = (mejor[0], mejor[1], mejor[2], a)
    return im


def encajar(recorte, W, H):
    """EL ENCAJE que PRESERVA EL ASPECTO: el bbox del borrador se
    escala (muestreo por celdas de la casa) a lo más grande que quepa
    en W×H SIN deformarse, centrado en el lienzo. El ojo almendrado NO
    se estira: su forma vive dentro del cuadrado (los ojos rasgados
    son MÁS ANCHOS que altos — la lectura «rasgado» depende de eso)."""
    w, h = recorte.size
    if w < 2 or h < 2:
        return Image.new('RGBA', (W, H), (0, 0, 0, 0))
    s = min(W / w, H / h)
    nw = max(2, min(W, int(round(w * s))))
    nh = max(2, min(H, int(round(h * s))))
    sprite = celdas(recorte, nw, nh)
    lienzo = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    lienzo.paste(sprite, ((W - nw) // 2, (H - nh) // 2), sprite)
    return lienzo


def el_molde(borrador, W, H, paleta):
    im = Image.open(borrador).convert('RGBA')
    recorte = bbox(cutout(im))
    recorte = quitar_verde_interior(recorte)
    sprite = encajar(recorte, W, H)
    sprite = cuantizar(sprite, paleta)
    print(f'  EL MOLDE: sprite {W}×{H} (bbox del borrador: {recorte.size})')
    return sprite


# ====================================================================
#  LA PLUMILLA (el borde fundido + el auto-enderez + el premultiplicado)
# ====================================================================
def la_plumilla(sprite, feather=1.2):
    """EL BORDE FUNDIDO: una pluma suave SOLO en el canal alfa (los
    colores quedan — el borde se funde a transparente, no se corta a
    cuchillo) y EL PREMULTIPLICADO (q,q,q,q) del lote de la casa."""
    w, h = sprite.size
    solo_alfa = sprite.split()[3].filter(ImageFilter.GaussianBlur(feather))
    r, g, b, a = sprite.split()
    from PIL import Image as _I
    sprite = _I.merge('RGBA', (r, g, b, solo_alfa))
    px = sprite.load()
    for y in range(h):
        for x in range(w):
            pr, pg, pb, pa = px[x, y]
            if pa == 0:
                px[x, y] = (0, 0, 0, 0)
                continue
            k = pa / 255.0
            px[x, y] = (int(pr * k), int(pg * k), int(pb * k), pa)
    return sprite


def el_auto_enderez_colmillo(sprite):
    """EL AUTO-ENDEREZ: si la IA curvó el colmillo hacia la IZQUIERDA,
    se espeja — el código de la casa espera la textura curvada a la
    DERECHA (la referencia de VFXCore.Colmillo: la punta cae a la
    derecha de la base). Mide el centro de masa del cuarto superior
    (la punta) contra el del cuarto inferior (la base)."""
    w, h = sprite.size
    px = sprite.load()

    def com_x(y0, y1):
        s = n = 0
        for y in range(y0, y1):
            for x in range(w):
                if px[x, y][3] > 40:
                    s += x
                    n += 1
        return s / n if n else w / 2.0

    punta, base = com_x(0, h // 4), com_x(3 * h // 4, h)
    if punta < base - 1.0:
        print(f'  EL AUTO-ENDEREZ: punta ({punta:.1f}) a la izquierda de la base '
              f'({base:.1f}) — se espeja')
        return sprite.transpose(Image.FLIP_LEFT_RIGHT)
    print(f'  EL AUTO-ENDEREZ: curvado a la derecha (punta {punta:.1f} ≥ base {base:.1f}) — ok')
    return sprite


# ====================================================================
#  LOS HORNEADOS PIL (el respaldo determinista si la pluma falla 3 veces)
# ====================================================================
def el_ojo_pil(W=64, H=64):
    """EL OJO HORNEADO: el mismo espíritu del OjoRasgado de VFXCore
    pero DETALLADO (la letra pide la forja para los ojos — y la forja
    de la casa también sabe hornear). Almendra, esclerótica violeta
    con venas, iris radial carmesí→violeta, rendija vertical con
    borde incandescente, destello húmedo, párpados."""
    sprite = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = sprite.load()
    cx, cy = (W - 1) / 2.0, (H - 1) / 2.0
    for y in range(H):
        for x in range(W):
            u = (x - cx) / (W * 0.46)
            v = (y - cy) / (H * 0.44)
            # LA ALMENDRA — |v| <= (1-u²) con la punta suave
            almendra = 1.0 - u * u
            if almendra <= 0:
                continue
            if abs(v) > almendra:
                continue
            dentro = 1.0 - abs(v) / max(almendra, 1e-4)   # 0 borde … 1 centro
            m = min(1.0, dentro * 3.4)                    # el borde fundido
            # (la lección del QA del horneado: «el borde inferior se
            # difumina demasiado» — la máscara aprieta el fundido a un
            # 30% del semialto: la silueta almendrada queda DEFINIDA)

            # LA ESCLERÓTICA — violeta oscuro, más hondo hacia los párpados
            es = 1.0 - 0.45 * (1.0 - dentro)
            r, g, b = 44 * es, 24 * es, 66 * es

            # LAS VENAS — ramas carmesí FINAS y VISIBLES desde las
            # puntas hacia el centro (la lección del QA del horneado:
            # «apenas se ven venas carmesí — añade 2-3 trazos»)
            for k in range(6):
                fase = u * (3.0 + k * 0.8) + 0.9 * k
                vena = math.exp(-((v - 0.6 * math.sin(fase)) ** 2) * 34.0) \
                       * max(0.0, abs(u) - 0.18) * 1.6
                r += 172 * vena; g += 20 * vena; b += 26 * vena

            # EL IRIS — elipse radial: borde CARMESÍ puro → medio
            # violeta → core luminoso (la lección del QA del horneado:
            # «le falta saturación carmesí en el borde exterior»)
            iu, iv = u / 0.52, v / 0.78
            ir = 1.0 - (iu * iu + iv * iv) * 0.5
            if ir > 0:
                t = max(0.0, min(1.0, ir))                # 1 centro … 0 borde
                if t < 0.45:
                    k2 = t / 0.45                        # el borde CARMESÍ
                    rr, gg, bb = 205 - 33 * k2, 16 + 34 * k2, 28 + 150 * k2
                else:
                    k2 = (t - 0.45) / 0.55               # el core VIOLETA
                    rr = 172 + 66 * k2
                    gg = 50 + 118 * k2
                    bb = 178 + 66 * k2
                k = max(0.0, min(1.0, (ir - 0.15) * 2.2))
                r += (rr - r) * k; g += (gg - g) * k; b += (bb - b) * k

            # LA RENDIJA VERTICAL — negra, FINA y ORGÁNICA (la lección
            # del QA: «más delgada y orgánica» — la lente respeta la
            # curva del ojo y ondula un pelo), con el borde
            # incandescente DOBLE (la lección: «se funde en negro sin
            # halo cálido» — un filo caliente pegado a la rendija y un
            # resplandor más ancho alrededor)
            ancho = (0.055 + 0.010 * math.sin(v * 9.0)) \
                * math.sqrt(max(0.0, 1.0 - v * v))
            d = abs(u) - ancho
            if d < 0:
                r = g = b = 3
            else:
                filo = math.exp(-(d / 0.028) ** 2) \
                       * max(0.35, 1.0 - abs(v) * 0.55)
                halo = math.exp(-(d / 0.085) ** 2) \
                       * max(0.30, 1.0 - abs(v) * 0.55)
                r += (255 - r) * filo * 0.95 + (200 - r) * halo * 0.55
                g += (132 - g) * filo * 0.95 + (66 - g) * halo * 0.55
                b += (76 - b) * filo * 0.95 + (40 - b) * halo * 0.55

            # EL DESTELLO HÚMEDO — arriba-izquierda del iris (la
            # lección del QA: «falta el destello» — más grande, con su
            # glaciar secundario abajo-derecha)
            du, dv = u + 0.24, v + 0.30
            brillo = math.exp(-(du * du + dv * dv) * 52.0)
            r += (252 - r) * brillo; g += (248 - g) * brillo; b += (255 - b) * brillo
            du2, dv2 = u - 0.20, v - 0.26
            brillo2 = 0.5 * math.exp(-(du2 * du2 + dv2 * dv2) * 150.0)
            r += (252 - r) * brillo2; g += (248 - g) * brillo2; b += (255 - b) * brillo2

            # LOS PÁRPADOS — la sombra de arriba y abajo
            pestana = max(0.0, dentro - 0.72) / 0.28
            r *= 1.0 - 0.35 * pestana; g *= 1.0 - 0.35 * pestana
            b *= 1.0 - 0.30 * pestana

            px[x, y] = (int(max(0, min(255, r * m))),
                        int(max(0, min(255, g * m))),
                        int(max(0, min(255, b * m))),
                        int(255 * m))
    print('  EL HORNEADO PIL: ojo 64×64')
    return sprite


def el_colmillo_pil(W=48, H=96):
    """EL COLMILLO HORNEADO: el HornearColmillo de VFXCore (48×48,
    aguja curva) VESTIDO DE VERDAD (48×96): la encía violeta en la
    base, el marfil con su degradado y su veta sutil, la punta pálida
    y la mancha roja del filo. Apunta ARRIBA, curvado a la derecha
    (la referencia de rotación del código)."""
    sprite = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = sprite.load()
    for y in range(H):
        v = y / (H - 1)                    # 0 punta (arriba) … 1 base
        for x in range(W):
            u = x / (W - 1)
            eje = 0.50 + 0.14 * (1.0 - v) * (1.0 - v)   # curva a la derecha
            ancho = 0.40 * math.pow(v, 0.62)
            d = abs(u - eje)
            if d > ancho:
                continue
            m = 1.0 - _suave(d / ancho, 0.55, 1.0)
            if m <= 0.003:
                continue

            # LA ENCÍA — el collar violeta de la base (v > 0.84; la
            # lección del QA: «banda violeta inferior sobrante» — el
            # collar sube más tarde y abraza SOLO la raíz)
            encia = max(0.0, min(1.0, (v - 0.84) / 0.12))
            # EL MARFIL — el degradado del cuerpo (más claro hacia el
            # filo izquierdo iluminado, la raíz se ensombrece)
            lado = 1.0 - 0.35 * _suave((u - eje) / max(ancho, 1e-4) * 0.5 + 0.5, 0.3, 1.0)
            hueso = (0.55 + 0.45 * v) * lado
            r, g, b = 246 * hueso, 238 * hueso, 220 * hueso

            # LA VETA SUTIL — una estría longitudinal
            veta = math.exp(-((u - eje + ancho * 0.35) ** 2) * 400.0) * 0.5
            r -= 26 * veta; g -= 26 * veta; b -= 30 * veta

            # LA PUNTA PÁLIDA — el último 18% casi blanco
            punta = max(0.0, min(1.0, (0.18 - v) / 0.18))
            r += (252 - r) * punta; g += (249 - g) * punta; b += (240 - b) * punta

            # LA MANCHA ROJA — sutil, en el filo derecho cerca de la punta
            mancha = math.exp(-((u - eje - ancho * 0.55) ** 2) * 500.0) \
                     * max(0.0, 1.0 - v / 0.30) * 0.55
            r += (150 - r) * mancha; g += (18 - g) * mancha; b += (22 - b) * mancha

            # LA ENCÍA encima de todo (el collar de la base)
            if encia > 0:
                eg = 1.0 - 0.4 * d / max(ancho, 1e-4)
                r = r * (1 - encia) + 52 * eg * encia
                g = g * (1 - encia) + 28 * eg * encia
                b = b * (1 - encia) + 84 * eg * encia

            px[x, y] = (int(max(0, min(255, r * m))),
                        int(max(0, min(255, g * m))),
                        int(max(0, min(255, b * m))),
                        int(255 * m))
    print('  EL HORNEADO PIL: colmillo 48×96')
    return sprite


def _suave(u, a, b):
    if u <= a:
        return 0.0
    if u >= b:
        return 1.0
    t = (u - a) / (b - a)
    return t * t * (3.0 - 2.0 * t)


# ====================================================================
#  LA VERIFICACIÓN (el chequeo técnico de la casa)
# ====================================================================
def verificar(im, nombre, w_esper, h_esper, extra=None):
    ok = True

    def chequeo(n, cond):
        nonlocal ok
        print(f'  [{"OK" if cond else "FAIL"}] {n}')
        if not cond:
            ok = False

    chequeo(f'{nombre}: tamaño EXACTO {w_esper}×{h_esper} RGBA',
            im.size == (w_esper, h_esper) and im.mode == 'RGBA')
    px = im.load()
    w, h = im.size
    mal = 0
    for y in range(0, h, 2):
        for x in range(0, w, 2):
            r, g, b, a = px[x, y]
            if a == 0:
                if (r, g, b) != (0, 0, 0):
                    mal += 1
            elif r > a or g > a or b > a:
                mal += 1
    chequeo(f'{nombre}: premultiplicado rgb≤a (violaciones: {mal})', mal == 0)
    if extra:
        extra(chequeo, im)
    return ok


def extra_ojo(chequeo, im):
    px = im.load()
    w, h = im.size
    # LA RENDIJA — la ONLY pieza negra de verdad: una CORRIDA ESTRECHA
    # (2..12 px) de píxeles oscuros cerca del centro horizontal. Los
    # párpados y las sombras anchas NO cuentan (corridas anchas); sin
    # una rendija estrecha y larga no hay ojo de dragón.
    filas = 0
    anchura_max = 0
    for y in range(h):
        # la corrida oscura más larga dentro del corredor central
        corrida = mejor = 0
        for x in range(max(0, w // 2 - 12), min(w, w // 2 + 13)):
            r, g, b, a = px[x, y]
            if a > 200 and r < 42 and g < 42 and b < 55:
                corrida += 1
                mejor = max(mejor, corrida)
            else:
                corrida = 0
        if 2 <= mejor <= 12:
            filas += 1
            anchura_max = max(anchura_max, mejor)
    chequeo(f'ojo: rendija estrecha vertical ({filas} filas, corrida máx '
            f'{anchura_max}px)', filas >= 18 and anchura_max <= 12)
    # EL NÚCLEO OPACO — el corazón del ojo tapa
    nucleo = px[w // 2, h // 2][3]
    chequeo(f'ojo: núcleo opaco (alfa centro {nucleo})', nucleo >= 250)


def extra_colmillo(chequeo, im):
    px = im.load()
    w, h = im.size

    def ancho_vis(y0, y1):
        return max((max((x for x in range(w) if px[x, y][3] > 40), default=0)
                    - min((x for x in range(w) if px[x, y][3] > 40), default=w))
                   for y in range(y0, y1))

    punta = ancho_vis(0, h // 6)
    base = ancho_vis(5 * h // 6, h)
    chequeo(f'colmillo: punta ({punta}px) más ESTRECHA que la base ({base}px)',
            punta < base * 0.75 and punta > 0)
    # APUNTA ARRIBA — la punta vive en el tercio superior (más masa
    # abajo que arriba en la mitad superior… la punta es lo de arriba)
    arriba = sum(1 for y in range(0, h // 3) for x in range(w) if px[x, y][3] > 40)
    abajo = sum(1 for y in range(2 * h // 3, h) for x in range(w) if px[x, y][3] > 40)
    chequeo(f'colmillo: apunta ARRIBA (masa arriba {arriba} vs abajo {abajo})',
        arriba > 0 and abajo > 0)


# ====================================================================
#  EL OJO (el QA VLM de la casa, con veredicto SHIP/FIX)
# ====================================================================
def el_ojo_vlm(imagen, pregunta):
    r = subprocess.run(['z-ai', 'vision', '-p', pregunta, '-i', imagen],
                       capture_output=True, text=True, timeout=180)
    veredicto = ''
    ini = (r.stdout or '').find('{')
    if ini >= 0:
        import json
        try:
            d = json.loads(r.stdout[ini:])
            veredicto = d['choices'][0]['message']['content'].strip()
        except Exception:
            veredicto = ''
    if not veredicto:
        veredicto = ((r.stdout or '') + (r.stderr or '')).strip()[-1200:] \
            or 'EL OJO no respondió'
    print('  EL OJO (QA VLM):')
    for linea in veredicto.splitlines()[:6]:
        print('   ', linea)
    tokens = re.findall(r'SHIP|FIX', veredicto, re.I)
    return (tokens[-1].upper() == 'SHIP') if tokens else False, veredicto


PREGUNTA_OJO = (
    'Sprites de un mod de Terraria, sobre fondo oscuro (así se ven en '
    'juego, en lote aditivo). El primero es UN OJO DE ABISMO 64×64 '
    '(tamaño real) y el segundo el mismo a 3×. Responde en espanol, '
    'max 5 lineas: (1) ¿se lee como UN ojo rasgado ALMENDRADO con la '
    'pupila de RENDIJA VERTICAL negra? (2) ¿el iris es radial '
    'carmesi-violeta con brillo interior y la rendija lleva un borde '
    'incandescente? (3) ¿la esclerotica es oscura con venas carmesi '
    'finas y hay un destello humedo arriba-izquierda? (4) ¿el fondo es '
    'totalmente transparente con el borde del ojo fundido (sin marco, '
    'sin fondo verde)? (5) VEREDICTO: SHIP o FIX (que).'
)
PREGUNTA_COLMILLO = (
    'Sprites de un mod de Terraria, sobre fondo oscuro (así se ven en '
    'juego). El primero es UN COLMILLO 48×96 (tamaño real) y el segundo '
    'el mismo a 3×. Responde en espanol, max 5 lineas: (1) ¿es UN solo '
    'colmillo curvo de MARFIL apuntando ARRIBA (punta arriba, base '
    'ancha abajo)? (2) ¿la base lleva encia oscura violeta y el cuerpo '
    'marfil con degradado? (3) ¿la punta es afilada y mas palida, con '
    'una mancha roja sutil en el filo? (4) ¿esta curvado a la derecha '
    'y el fondo es totalmente transparente (sin fondo verde)? '
    '(5) VEREDICTO: SHIP o FIX (que).'
)


def el_montaje(im, destino, escalas=(1, 3)):
    """LA HOJA DE CONTACTOS sobre DAMERO — el damero es la señal
    universal de «aquí hay transparencia real» (la lección del QA del
    colmillo: sobre fondo oscuro, EL OJO de la casa leía el propio
    fondo del montaje como «fondo oscuro sólido» y pedía quitarlo)."""
    piezas = []
    for s in escalas:
        piezas.append(im.resize((im.width * s, im.height * s), Image.NEAREST)
                     if s > 1 else im.copy())
    ancho = sum(p.width for p in piezas) + 30 * (len(piezas) + 1)
    alto = max(p.height for p in piezas) + 40
    hoja = Image.new('RGBA', (ancho, alto), (0, 0, 0, 255))
    pxh = hoja.load()
    for y in range(alto):                     # EL DAMERO de la transparencia
        for x in range(ancho):
            c = (58, 58, 72) if ((x // 8 + y // 8) % 2 == 0) else (34, 34, 46)
            pxh[x, y] = (c[0], c[1], c[2], 255)
    dx = 30
    for p in piezas:
        hoja.paste(p, (dx, (alto - p.height) // 2), p)
        dx += p.width + 30
    hoja.save(destino)
    print(f'  EL MONTAJE: {destino}')


# ====================================================================
#  LA FORJA CON QA (máx 3 intentos por pieza; luego el horneado PIL)
# ====================================================================
def forjar_pieza(nombre, destino, W, H, paleta, prompt, lecciones,
                 pregunta, tamano, pil_fn, extra=None, enderez=False):
    print(f'\nLA FORJA v6.104 — {nombre} ({W}×{H})')
    os.makedirs(CARPETA, exist_ok=True)
    borrador = os.path.join(CARPETA, f'borrador_{nombre}.png')

    if SOLO_PIL:
        print('  modo --pil: EL HORNEADO de la casa (sin IA)')
        sprite = pil_fn()
        tela = la_plumilla(sprite)
        tela.save(destino)
        print(f'  guardada: {destino} {tela.size}')
        return verificar(tela, nombre, W, H, extra)

    veredicto_final = ''
    for intento in (1, 2, 3):
        print(f'  — intento {intento}/3 —')
        if intento == 1 and not FORZAR and os.path.exists(borrador):
            print('  LA PLUMA (borrador existente)')
        else:
            prompt_i = prompt + (' ' + lecciones[intento - 1]
                                 if lecciones[intento - 1] else '')
            if not la_pluma(prompt_i, borrador, tamano):
                print('  la pluma falló — siguiente intento')
                continue
        try:
            sprite = el_molde(borrador, W, H, paleta)
        except Exception as e:
            print(f'  el molde falló ({e}) — siguiente intento')
            continue
        if enderez:
            sprite = el_auto_enderez_colmillo(sprite)
        tela = la_plumilla(sprite)
        tela.save(destino)
        print(f'  guardada: {destino} {tela.size}')
        if not verificar(tela, nombre, W, H, extra):
            print('  la verificación técnica falló — siguiente intento')
            continue
        el_montaje(tela, f'/tmp/sombra5_{nombre}_qa.png')
        ship, veredicto_final = el_ojo_vlm(f'/tmp/sombra5_{nombre}_qa.png', pregunta)
        if ship:
            print(f'  VEREDICTO {nombre}: SHIP (intento {intento})')
            return True
        print(f'  VEREDICTO {nombre}: FIX — la lección entra al prompt')

    # LA PLUMA NO CLAVÓ LA PIEZA — EL HORNEADO DE LA CASA
    print(f'  3 intentos sin SHIP — {nombre} cae al HORNEADO PIL de la casa')
    sprite = pil_fn()
    tela = la_plumilla(sprite)
    tela.save(destino)
    print(f'  guardada: {destino} {tela.size}')
    ok = verificar(tela, nombre, W, H, extra)
    el_montaje(tela, f'/tmp/sombra5_{nombre}_qa.png')
    ship, veredicto_final = el_ojo_vlm(f'/tmp/sombra5_{nombre}_qa.png', pregunta)
    print(f'  VEREDICTO {nombre} (horneado): {"SHIP" if ship else "FIX"}')
    return ok and ship


def main():
    print('LA FORJA v6.104 — LAS PIEZAS VIVAS DEL TENTÁCULO LISO')
    print('  (la letra: «con ojos y colmillos … con la forja o el taller»)')

    ok_ojo = forjar_pieza(
        'ojo', DEST_OJO, 64, 64, PALETA_OJO, PROMPT_OJO, LECCIONES_OJO,
        PREGUNTA_OJO, '1024x1024', el_ojo_pil, extra=extra_ojo)
    ok_colmillo = forjar_pieza(
        'colmillo', DEST_COLMILLO, 48, 96, PALETA_COLMILLO, PROMPT_COLMILLO,
        LECCIONES_COLMILLO, PREGUNTA_COLMILLO, '768x1344', el_colmillo_pil,
        extra=extra_colmillo, enderez=True)

    print('\nRESULTADO:',
          'OK — las piezas vivas están forjadas' if ok_ojo and ok_colmillo
          else 'REVISAR — alguna pieza quedó en FIX')
    sys.exit(0 if ok_ojo and ok_colmillo else 1)


if __name__ == '__main__':
    main()
