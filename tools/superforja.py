#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
superforja.py — LA SUPERFORJA: LAS TRES HERRAMIENTAS DE LOS REELS + LAS
SIETE NUEVAS DE LA INVESTIGACIÓN, TODAS EN UNA (v6.98).

LA LETRA DEL USUARIO: «une las 3 herramientas en una, ademas busca mas
herramientas similares y de utilidad que creas que nos puedan ser utiles
y crea una super herramienta con todas ellas».

══════════════════════════════════════════════════════════════════════
LAS TRES ORIGINALES (LA FORJA v6.95 — la unión de los 3 reels):
══════════════════════════════════════════════════════════════════════
  LA PLUMA  (pxl)    — el borrador IA con QA (pintar → mirar → refinar)
  EL MOLDE  (sprite-gen) — imagen → sprite: cutout → rejilla → paleta →
                       contorno → halo → highlight
  EL ALMA   (pixelfx) — el preview del efecto de píxel que lo da vida

══════════════════════════════════════════════════════════════════════
LAS SIETE NUEVAS (la investigación de la sesión — cada una, la IDEA
recreada por la casa, sin una línea ni una textura de nadie):
══════════════════════════════════════════════════════════════════════
  EL PUENTE (restore-pixel-art / fix-pixelart / los escaladores xBR-hqx
             de la investigación) — el render IA viene con la rejilla
             SUCIA (píxeles a medias): EL PUENTE detecta la rejilla
             nativa (bloques N×N), toma el color DOMINANTE de cada celda
             y restaura el pixel art REAL en su resolución nativa.
             Además EL ESCALÓN 2× de la casa (los bordes que se
             conservan: la idea EPX de siempre, escrita aquí) para
             re-escalar previews sin ensuciar.
  EL ESPEJO (framelet — «static sprites into dynamic sprite sheet» y la
             técnica del Vástago del Eterno de la .91) — un sprite
             estático → TIRA ANIMADA procedimental: respirar (squash &
             stretch antifase), flotar (bob + inclinación), latir
             (pulso de brillo), vaivén (balanceo). LA TIRA VERTICAL de
             tML lista para AnimationFrameHeight + el manifiesto.
  EL TAMBOR (los generadores de dithering de la investigación) — el
             difuminado de la paleta: Bayer 4×4/8×8 ordenado y
             Floyd-Steinberg por difusión de error. Para los cielos, los
             velos y las coronas en paleta contada.
  LA RAMPA (palette.joannahosking / Huetrivo y la ley del hue-shift) —
             del color base a la RAMPA COMPLETA: sombras que se van al
             frío, luces que se van al cálido, paso a paso, con su PNG de
             muestras y sus hex listos para copiar.
  EL ATLAS  (MaxRects — soimy/maxrects-packer / PyTexturePacker) — los
             frames sueltos → LA TIRA VERTICAL de tML · EL ATLAS COMPACTO
             (el empaque de la casa: guillotina por mejor lado, con
             detección de duplicados por huella de píxeles) · el
             MANIFIESTO JSON con todo.
  LA REFINA (el paso «mirar → refinar» de pxl, ahora quirúrgico) — en
             vez de regenerar el borrador desde cero, EDICIÓN DIRIGIDA
             con image-edit: «la hoja más larga», «más dorado», y el
             resto del dibujo queda intacto.
  EL OJO    (el QA VLM de la casa, ahora DENTRO de la cadena) — el
             montaje pasa por la visión: la nota y los defectos
             listados, sin salir de la herramienta.
  EL CORTE  (el cutout UNIVERSAL de la v6.98) — CUALQUIER fondo se va
             (no solo el croma verde: el azul-noche #1a2a6e y el
             casi-negro de un render también): el color del fondo se
             huele en el anillo del borde, la inundación con correa
             (BFS por distancia global + degradados locales) lo arranca
             píxel por píxel, y el borde anti-aliased se DES-FRINGEA
             (des-mezcla del sujeto puro). Tiras de N frames también
             (--frames 4 para el altar).
  + LOS NUEVOS EFECTOS DEL ALMA (los 25 VFX libres de la investigación:
    explosiones, hielo, rayos, escudos, portales) — fuego · hielo · rayo
    · escudo · portal se suman a brasa · humo · glitch · consagración.

══════════════════════════════════════════════════════════════════════
LA CADENA DE LA SUPERFORJA (el comando `cadena` — TODO en uno):
══════════════════════════════════════════════════════════════════════
  LA PLUMA → EL PUENTE → EL MOLDE → EL ESPEJO → EL ALMA → EL ATLAS →
  EL MONTAJE → EL OJO (QA VLM)
  (borrador   (rejilla  (sprite de  (la tira    (el     (la tira  (todo     (la nota
   IA)         nativa)   la casa)    animada)    efecto)  + atlas)   junto)    y los defectos)

USO:
  # LA CADENA COMPLETA (el arma nueva de la casa, de cero a QA):
  python3 tools/superforja.py cadena \
      --nombre prueba --prompt "un cetro dorado..." \
      --ancho 36 --alto 44 --efecto fuego --animacion vida --frames 4 --qa

  # cada herramienta por separado (todas se combinan con --nombre):
  python3 tools/superforja.py pluma   --nombre x --prompt "..." [--refinar "..."]
  python3 tools/superforja.py refina  --nombre x --nota "la hoja más larga"
  python3 tools/superforja.py puente  --nombre x [--borrador img.png] [--x2 N]
  python3 tools/superforja.py molde   --nombre x [--borrador img.png] --ancho 36 --alto 44
  python3 tools/superforja.py espejo  --nombre x --animacion vida --frames 6
  python3 tools/superforja.py alma    --nombre x --efecto portal [--tira]
  python3 tools/superforja.py tambor  --imagen img.png --matriz bayer8 --colores 6
  python3 tools/superforja.py rampa   --base "#D89030" [--pasos 7] [--hue 22]
  python3 tools/superforja.py atlas   --nombre x [--tira] [--maxrects]
  python3 tools/superforja.py ojo     --nombre x   (el QA VLM sobre el montaje)
  python3 tools/superforja.py corta   --imagen sprite.png [--tolerancia 48]
                                         [--frames 4] [--preview]  (fondo → transparente)
  python3 tools/superforja.py forjar  ...          (la cadena corta de la .95: pluma → molde → alma)

SALIDAS (en /tmp/superforja_<nombre>/):
  borrador.png / borrador_refinado.png   LA PLUMA (+ LA REFINA)
  nativo.png                             EL PUENTE (la rejilla restaurada)
  sprite.png + highlight.png             EL MOLDE
  tira.png + manifiesto.json             EL ESPEJO / EL ATLAS
  efecto_<n>.png · efecto_tira.png       EL ALMA
  atlas.png + atlas.json                 EL ATLAS (el empaque compacto)
  montaje_qa.png · qa_vision.txt         EL MONTAJE + EL OJO
  rampa_<nombre>.png                     LA RAMPA / EL TAMBOR (con --dest)

TODO local, sin nube (la regla de sprite-gen), y EL MOLDE usa EXACTAMENTE
las funciones que produjeron los sprites vivos del mod (la fuente de
verdad de gen_altar_v695.py).
"""
import argparse
import colorsys
import hashlib
import json
import math
import os
import random
import subprocess
import sys

from PIL import Image, ImageDraw

AQUI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, AQUI)
from gen_altar_v695 import (PALETA, bbox, celdas, contorno, cutout,
                            cuantizar, halo, highlight)

RAIZ = os.path.join(AQUI, '..', 'AethonMod')

# === LA PALETA DE LA CASA (para el prompt de la pluma) ===
PALETA_PROMPT = ("deep midnight-blue ink #182878 body, near-black #0A0A2A shadows, "
                 "golden amber clasps #D89030 with #FFC850 highlights, glowing bright "
                 "fuchsia #F000C0 magic accents, teal #5ADCB4 flames, violet #9643EB runes")

EFECTOS = ['brasa', 'humo', 'glitch', 'consagracion',
           'fuego', 'hielo', 'rayo', 'escudo', 'portal',
           'eclipse', 'sigilo', 'agujero', 'orbe',
           'disolucion', 'aura', 'destello', 'runas']

# ====================================================================
#  v6.121 — EL CÓDIGO DE PIXELFX, COPIADO Y ADAPTADO (la letra: «revisa
#  que la forja o el taller tenga el codigo de este repo, asegurate de
#  que no pierda calidad de ninguna manera, copia y adapta, y tambien
#  mejora para nuestro juego» — github.com/patch-gg/pixelfx)
#
#  LO COPIADO Y ADAPTADO — LA ARQUITECTURA DE PIXELFX (su corazón, la
#  razón por la que sus 244 efectos se ven vivos):
#   1. EL DOBLE BÚFER: un búfer de COLOR y un búfer de LUZ — la luz se
#      compone EN ADITIVO (premultiplied alpha: rgb·a se SUMA, el alfa
#      se MAXEA): el brillo respira ALREDEDOR del sprite SIN lavarlo
#      jamás (la calidad NO se pierde: los 9 efectos de siempre quedan
#      INTACTOS píxel a píxel — el teatro es una capa NUEVA).
#   2. EL LIENZO EXTENDIDO: los efectos de pixelfx viven en el espacio
#      LOCAL ALREDEDOR del origen (no recortados al bounding box): el
#      pad de 26 px deja respirar coronas, halos y anillos.
#   3. draw(p, t, o): cada efecto, UN TROCITO DE CÓDIGO determinista
#      (semilla de la casa) que pinta el frame t — la firma de su API.
#   4. CERO BLUR: pixelfx es pixel PURO — la luz es CRISP, sin
#      difuminados que traicionen el pixel art (la lección del pilar).
#
#  LO MEJORADO PARA NUESTRO JUEGO: los 8 efectos nuevos salen de SUS
#  categorías (shadow · arcane · holy · status · spawn&death ·
#  impacts · transitions) vestidos con LA PALETA DEL VACÍO — el
#  eclipse del grimorio, el sigilo del conjuro, el agujero de los
#  staves, la disolución de la devoración:
#   · eclipse    (shadow — su «eclipse»: LA FIRMA DEL MOD)
#   · sigilo     (shadow — su «hex sigil»: el círculo rúnico girando)
#   · agujero    (shadow — su «black hole»: el pozo y su anillo)
#   · orbe       (arcane — su «arcane orb»: el orbe y sus satélites)
#   · disolucion (spawn&death — su «dissolve burn»: la devoración)
#   · aura       (holy — su «halo»: el halo dorado y su columna)
#   · destello   (impacts — su «hit flash»: el fogonazo y la onda)
#   · runas      (arcane — su «magic circle cast»: las runas orbitando)
# ====================================================================
_EFECTOS_PIXELFX = {'eclipse', 'sigilo', 'agujero', 'orbe',
                    'disolucion', 'aura', 'destello', 'runas'}
_PAD = 26                       # el lienzo extendido de pixelfx

VACIO_H = (26, 14, 44)          # el vacío hondo (la casa)
VIOLETA_V = (150, 84, 235)      # el violeta del vacío
LILA_V = (196, 112, 255)        # el lila vivo
ORO_V = (255, 214, 130)         # el oro del veredicto
ORO_H = (212, 168, 84)          # el oro hondo


def carpeta_de(nombre, dest=None):
    c = dest or f'/tmp/superforja_{nombre}'
    os.makedirs(c, exist_ok=True)
    return c


def cargar_sprite(args):
    """El sprite de entrada para espejo/alma/atlas: --sprite explícito o
    el sprite.png de la carpeta de la forja."""
    if getattr(args, 'sprite', None):
        return Image.open(args.sprite).convert('RGBA')
    p = os.path.join(carpeta_de(args.nombre, getattr(args, 'dest', None)), 'sprite.png')
    if not os.path.exists(p):
        raise SystemExit(f'falta el sprite: usa --sprite o pasa antes por el molde ({p})')
    return Image.open(p).convert('RGBA')


# ====================================================================
#  ETAPA 1 — LA PLUMA (pxl: pintar → mirar → refinar)
# ====================================================================
def la_pluma(prompt, destino, tamano='864x1152'):
    """EL BORRADOR IA con la skill de generación (el paso «pintar» de
    pxl; el «mirar» lo hace EL OJO del final — y el «refinar» ahora es
    LA REFINA quirúrgica, no la regeneración)."""
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


def la_refina(origen, nota, destino, tamano='864x1152'):
    """LA REFINA QUIRÚRGICA (el paso «refinar» de pxl hecho derecho): en
    vez de volver a pintar el borrador desde cero, la EDICIÓN DIRIGIDA
    sobre la imagen — «la hoja más larga» cambia la hoja y el resto
    queda intacto. (La llamada va DIRECTA al endpoint de la casa: el
    CLI manda `image` y la API de esta era exige `images: [{url}]` —
    el formato documentado por la skill de edición.)"""
    import base64
    import urllib.request
    # el borrador IA llega a veces siendo JPEG con abrigo de .png — el
    # data URL debe declarar lo que los bytes SON de verdad
    im = Image.open(origen).convert('RGB')
    im.save(destino + '.tmp.png', 'PNG')
    b64 = base64.b64encode(open(destino + '.tmp.png', 'rb').read()).decode()
    cfg = json.load(open('/etc/.z-ai-config'))
    cuerpo = {
        'prompt': (f"Refine this pixel art sprite: {nota}. "
                   f"Keep everything else EXACTLY the same: same pose, same "
                   f"palette, same chunky pixel style, same green chroma key "
                   f"background, same framing."),
        'images': [{'url': f'data:image/png;base64,{b64}'}],
        'size': tamano,
    }
    req = urllib.request.Request(
        cfg['baseUrl'] + '/images/generations/edit',
        data=json.dumps(cuerpo).encode(),
        headers={'Content-Type': 'application/json',
                 'Authorization': f"Bearer {cfg['apiKey']}",
                 'X-Chat-Id': cfg.get('chatId', ''),
                 'X-User-Id': cfg.get('userId', ''),
                 'X-Token': cfg.get('token', ''),
                 'X-Z-AI-From': 'Z'}, method='POST')
    try:
        with urllib.request.urlopen(req, timeout=280) as r:
            d = json.load(r)
        item = d['data'][0]
        if 'base64' in item:
            open(destino, 'wb').write(base64.b64decode(item['base64']))
        elif 'url' in item:
            urllib.request.urlretrieve(item['url'], destino)
        else:
            raise KeyError('la respuesta no trae imagen')
    finally:
        if os.path.exists(destino + '.tmp.png'):
            os.remove(destino + '.tmp.png')
    if not os.path.exists(destino):
        raise SystemExit('LA REFINA falló: la edición no devolvió imagen')
    print(f'  LA REFINA: {nota!r} → {destino}')


# ====================================================================
#  ETAPA 2 — EL PUENTE (restore-pixel-art: la rejilla nativa restaurada)
# ====================================================================
def _pureza(im, n, muestras=180):
    """LA PUREZA DE LAS CELDAS: la fracción de celdas n×n donde un solo
    color domina (≥ 60%) — el árbitro final de la rejilla (una celda
    que abarca DOS píxeles nativos NO es pura)."""
    w, h = im.size
    px = im.load()
    random.seed(5)
    total = (w // n) * (h // n)
    if total <= 0:
        return 0.0
    idx = [(random.randrange(w // n), random.randrange(h // n))
           for _ in range(min(muestras, total))]
    puras = 0
    for cx, cy in idx:
        votos = {}
        for yy in range(n):
            for xx in range(n):
                c = px[cx * n + xx, cy * n + yy]
                votos[c] = votos.get(c, 0) + 1
        if max(votos.values()) >= n * n * 0.6:
            puras += 1
    return puras / len(idx)


def detectar_rejilla(im):
    """LA REJILLA NATIVA — DOS PRUEBAS (el método de la casa):
    (1) LA ALINEACIÓN DE BORDES: los bordes de verdad (donde el color
        cambia) deben caer TODOS en los límites de la rejilla — el
        período que alinea ≥ 75% de los bordes en UNA clase residual
        es CANDIDATO (una rejilla más grande que la verdadera deja
        bordes caer DENTRO de sus celdas: pierde);
    (2) ENTRE LOS MEJORES, LA MÁS GRANDE: cuando varias rejillas
        explican los bordes por igual (el x4 limpio: la 2 y la 4
        alinean al 100%), la más GRANDE es la respuesta (la navaja de
        la casa — celdas mayores, la misma explicación);
        + LA PUREZA como examen final: las celdas n×n deben ser de
        verdad uniformes (≥ 62%), o el candidato cae.
    Si nada pasa las pruebas, no hay rejilla (el borrador pasa tal
    cual — el molde ya sabe medir)."""
    w, h = im.size
    px = im.load()

    def bordes(vertical):
        """las líneas donde el color cambia (muestreadas por rendimiento)"""
        out = []
        if vertical:
            for x in range(1, w):
                cambios = sum(1 for y in range(0, h, 4)
                              if px[x, y][:3] != px[x - 1, y][:3] and
                              px[x, y][3] == px[x - 1, y][3])
                if cambios > max(4, h // 16):
                    out.append(x)
        else:
            for y in range(1, h):
                cambios = sum(1 for x in range(0, w, 4)
                              if px[x, y][:3] != px[x, y - 1][:3] and
                              px[x, y][3] == px[x, y - 1][3])
                if cambios > max(4, w // 16):
                    out.append(y)
        return out

    def candidatos(edges, tam):
        """los períodos que alinean ≥ 75% de los bordes en una clase"""
        out = []
        if len(edges) < 8:
            return out
        for p in range(2, 17):
            if tam % p:
                continue
            hist = {}
            for e in edges:
                hist[e % p] = hist.get(e % p, 0) + 1
            s = max(hist.values()) / len(edges)
            if s >= 0.75:
                out.append((p, s))
        return out

    cands = {}
    for p, s in candidatos(bordes(True), w) + candidatos(bordes(False), h):
        cands[p] = max(cands.get(p, 0.0), s)
    if not cands:
        return 1, 0.0
    # EL GRUPO DE LOS MEJORES (dentro de 5%) → entre ellos, EL MÁS GRANDE
    # que pase la pureza
    tope = max(cands.values())
    grupo = sorted((p for p, s in cands.items() if s >= tope - 0.05), reverse=True)
    for p in grupo:
        if p > max(w, h) // 8:          # una rejilla mayor que la imagen no tiene sentido
            continue
        pu = _pureza(im, p)
        if pu >= 0.62:
            return p, min(cands[p], pu)
    return 1, 0.0


def restaurar_nativo(im, n):
    """EL PIXEL ART REAL: cada celda N×N → su color DOMINANTE (la
    mayoría manda — los bordes sucios pierden). Sale la imagen en su
    resolución nativa w/n × h/n."""
    if n <= 1:
        return im.copy()
    w, h = im.size
    # la MODA por celda (el color más votado manda — los bordes sucios
    # del render pierden); el NEAREST del centro como árbitro de
    # empates (la celda no clara se queda con su centro).
    centro = im.resize((w // n, h // n), Image.NEAREST)
    px, px2 = im.load(), centro.load()
    out = Image.new('RGBA', (w // n, h // n))
    po = out.load()
    for y in range(h // n):
        for x in range(w // n):
            # la votación de la celda: el color que más píxeles tiene
            votos = {}
            for yy in range(y * n, (y + 1) * n):
                for xx in range(x * n, (x + 1) * n):
                    c = px[xx, yy]
                    votos[c] = votos.get(c, 0) + 1
            mejor_c = max(votos.items(), key=lambda kv: kv[1])[0]
            po[x, y] = mejor_c if votos[mejor_c] > n * n * 0.5 else px2[x, y]
    return out


def el_escalon(im, veces=1):
    """EL ESCALÓN 2× DE LA CASA (la idea EPX de siempre, escrita aquí):
    cada píxel → 2×2, y las ESQUINAS heredan el color del vecino cuando
    los dos lados de la esquina coinciden (los bordes diagonales quedan
    limpios, sin la escalera rota del NEAREST crudo)."""
    for _ in range(veces):
        w, h = im.size
        px = im.load()
        out = Image.new('RGBA', (w * 2, h * 2))
        po = out.load()
        for y in range(h):
            for x in range(w):
                E = px[x, y]
                B = px[x, y - 1] if y > 0 else E
                H = px[x, y + 1] if y < h - 1 else E
                D = px[x - 1, y] if x > 0 else E
                F = px[x + 1, y] if x < w - 1 else E
                e0 = e1 = e2 = e3 = E
                if B != H and D != F:
                    if D == B: e0 = D
                    if F == B: e1 = F
                    if D == H: e2 = D
                    if F == H: e3 = F
                po[x * 2, y * 2] = e0
                po[x * 2 + 1, y * 2] = e1
                po[x * 2, y * 2 + 1] = e2
                po[x * 2 + 1, y * 2 + 1] = e3
        im = out
    return im


def el_puente(origen, destino):
    """EL PUENTE COMPLETO: detecta la rejilla → restaura el nativo →
    guarda. Si la detección no está segura (frac < 0,55), el borrador
    pasa tal cual (el molde ya sabe medir)."""
    im = Image.open(origen).convert('RGBA')
    n, frac = detectar_rejilla(im)
    if n > 1 and frac >= 0.55:
        nativo = restaurar_nativo(im, n)
        nativo.save(destino)
        print(f'  EL PUENTE: rejilla {n}×{n} detectada (confianza {frac:.0%}) '
              f'→ {im.size} a {nativo.size}: {destino}')
        return destino, n
    im.save(destino)
    print(f'  EL PUENTE: sin rejilla clara (mejor candidato {n}×, confianza '
          f'{frac:.0%}) — el borrador pasa tal cual')
    return destino, 1


# ====================================================================
#  ETAPA 3 — EL MOLDE (sprite-gen: cutout → rejilla → paleta → contorno)
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
#  ETAPA 3½ — EL CORTE (el cutout UNIVERSAL: CUALQUIER fondo, no solo
#  el croma verde del borrador — el fondo azul-noche o negro de un
#  render también se va, píxel por píxel)
#
#  LA LETRA: «hay un problema con la creacion de sprite y es que todos
#  tienen un fondo oscuro, y eso esta mal, todos los sprite deben tener
#  fondo transparente».
#
#  LAS TRES LEYES DEL CORTE:
#   1. EL COLOR DEL MARCO — el fondo se huele en el ANILLO del borde
#      (la moda de las celdas cuantizadas del marco): el fondo del
#      cuadro, sea verde croma, azul-noche #1a2a6e o casi-negro.
#   2. LA INUNDACIÓN CON CORREA — BFS desde los píxeles del borde que
#      se parecen al fondo; un píxel entra si se parece al fondo
#      (distancia global ≤ tolerancia) O a su vecino ya aceptado
#      (distancia local ≤ 11, los degradados y viñetas del render
#      también se van) PERO SIN SOLTARSE del fondo (correa: a lo sumo
#      2.2× la tolerancia del color original — la inundación nunca se
#      come el dibujo).
#   3. EL DES-FRINGE — el borde anti-aliased contra el fondo queda
#      teñido: el anillo de píxeles que toca el fondo se DES-MEZCLA
#      (cuánto sujeto hay en el color observado → ese es su alfa, y su
#      color es el sujeto puro). Sin halos oscuros jamás.
# ====================================================================
def _color_fondo(px, w, h, grosor=2):
    """El color DOMINANTE del anillo del borde + su cobertura (0..1)."""
    muestras = []
    for x in range(w):
        for dy in range(grosor):
            muestras.append(px[x, dy])
            muestras.append(px[x, h - 1 - dy])
    for y in range(h):
        for dx in range(grosor):
            muestras.append(px[dx, y])
            muestras.append(px[w - 1 - dx, y])
    cajas = {}
    for (r, g, b, a) in muestras:
        llave = (r >> 4, g >> 4, b >> 4)
        cajas[llave] = cajas.get(llave, 0) + 1
    llave_moda = max(cajas, key=cajas.get)
    rs = gs = bs = cnt = 0
    for (r, g, b, a) in muestras:
        if (r >> 4, g >> 4, b >> 4) == llave_moda:
            rs += r; gs += g; bs += b; cnt += 1
    return (rs // max(1, cnt), gs // max(1, cnt), bs // max(1, cnt)), \
        cajas[llave_moda] / float(len(muestras))


def _dist(c1, c2):
    return math.sqrt((c1[0] - c2[0]) ** 2 + (c1[1] - c2[1]) ** 2 +
                     (c1[2] - c2[2]) ** 2)


def _cortar_fondo(im, tolerancia=48.0, correa=2.2, local=11.0, peine=True, cielo=False):
    """UNA imagen → (RGBA sin fondo, % quitado, color del fondo)."""
    w, h = im.size
    px = im.load()
    fondo, cobertura = _color_fondo(px, w, h)
    correa_max = tolerancia * correa
    # EL MAPA: 0 sin ver · 1 fondo · 2 sujeto
    mapa = bytearray(w * h)
    pila = []
    for x in range(w):
        for y in (0, h - 1):
            i = y * w + x
            if mapa[i] == 0 and _dist(px[x, y], fondo) <= tolerancia:
                mapa[i] = 1; pila.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            i = y * w + x
            if mapa[i] == 0 and _dist(px[x, y], fondo) <= tolerancia:
                mapa[i] = 1; pila.append((x, y))
    # LA INUNDACIÓN (BFS con las dos puertas y la correa)
    while pila:
        x, y = pila.pop()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if nx < 0 or ny < 0 or nx >= w or ny >= h:
                continue
            i = ny * w + nx
            if mapa[i] != 0:
                continue
            c = px[nx, ny]
            d_fondo = _dist(c, fondo)
            if d_fondo <= tolerancia:
                mapa[i] = 1; pila.append((nx, ny)); continue
            # la puerta local: el degradado del vecino (con la correa)
            if (d_fondo <= correa_max and
                    _dist(c, px[x, y]) <= local):
                mapa[i] = 1; pila.append((nx, ny))
    # EL DES-FRINGE (anillo de profundidad 2 alrededor del fondo): el
    # píxel teñido se DES-MEZCLA — cuánto sujeto lleva el color
    # observado es su alfa; su color es el sujeto puro. Sin halos.
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    po = out.load()
    quitados = 0
    tope = tolerancia * 1.35
    for y in range(h):
        for x in range(w):
            i = y * w + x
            if mapa[i] == 1:
                quitados += 1
                continue
            r, g, b, a = px[x, y]
            if a > 0:
                # la distancia al fondo más cercano (Chebyshev ≤ 2)
                cerca = 99
                for dy in (-2, -1, 0, 1, 2):
                    ny = y + dy
                    if ny < 0 or ny >= h:
                        continue
                    base = ny * w
                    for dx in (-2, -1, 0, 1, 2):
                        nx = x + dx
                        if nx < 0 or nx >= w:
                            continue
                        paso = max(abs(dx), abs(dy))
                        if mapa[base + nx] == 1 and paso < cerca:
                            cerca = paso
                if cerca <= 2:
                    d = _dist((r, g, b), fondo)
                    if d < tope:
                        k = d / tope * (1.0 - 0.22 * (cerca - 1))
                        k = max(0.06, k)   # cuánto SUJETO lleva dentro
                        alfa = max(0, min(255, int(a * k)))
                        if k > 0.08:
                            r2 = (r - (1 - k) * fondo[0]) / k
                            g2 = (g - (1 - k) * fondo[1]) / k
                            b2 = (b - (1 - k) * fondo[2]) / k
                            r2 = max(0, min(255, int(r2)))
                            g2 = max(0, min(255, int(g2)))
                            b2 = max(0, min(255, int(b2)))
                        else:
                            r2, g2, b2 = r, g, b
                        po[x, y] = (r2, g2, b2, alfa)
                        continue
            po[x, y] = (r, g, b, a)
    # EL PEINE — las islas diminutas oscuras que flotan cerca del marco
    # (los restos de la viñeta del render): se van; las chispas y los
    # destellos del DIBUJO (colores vivos, aunque sean chicos) quedan.
    if peine:
        from collections import deque as _dq
        visto = bytearray(w * h)
        islas = []
        for yy in range(h):
            for xx in range(w):
                i0 = yy * w + xx
                if visto[i0] or po[xx, yy][3] <= 8:
                    continue
                q = _dq([(xx, yy)])
                visto[i0] = 1
                pix = []
                while q:
                    cx, cy = q.popleft()
                    pix.append((cx, cy))
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        nx, ny = cx + dx, cy + dy
                        if 0 <= nx < w and 0 <= ny < h and not visto[ny * w + nx] \
                                and po[nx, ny][3] > 8:
                            visto[ny * w + nx] = 1
                            q.append((nx, ny))
                islas.append(pix)
        if islas:
            islas.sort(key=len, reverse=True)
            cuerpo = set(islas[0])
            for isl in islas[1:]:          # la mayor es el dibujo
                n = len(isl)
                if n > 12:
                    continue
                lum = sum(0.30 * po[x, y][0] + 0.55 * po[x, y][1] +
                          0.15 * po[x, y][2] for x, y in isl) / n
                df = sum(_dist(po[x, y], fondo) for x, y in isl) / n
                # LA ISLA RODEADA DE FONDO (solo --cielo): las
                # estrellitas de un fondo estrellado flotan en la nada
                # (su anillo ≥ 70% transparente) — se van aunque sean
                # brillantes. FUERA de --cielo las chispas flotantes
                # del DIBUJO (las brasas de la lámpara) se quedan.
                anillo = 0
                hueco = 0
                for x, y in isl:
                    for dy in (-2, -1, 0, 1, 2):
                        for dx in (-2, -1, 0, 1, 2):
                            if dx == 0 and dy == 0:
                                continue
                            nx, ny = x + dx, y + dy
                            if 0 <= nx < w and 0 <= ny < h:
                                anillo += 1
                                if po[nx, ny][3] <= 8:
                                    hueco += 1
                rodeada = anillo > 0 and hueco >= anillo * 0.70
                if lum < 42.0 or df < tolerancia * 0.75 or (cielo and rodeada):
                    for x, y in isl:
                        po[x, y] = (0, 0, 0, 0)
    return out, 100.0 * quitados / (w * h), fondo, cobertura


def el_corte(im, tolerancia=48.0, frames=1, peine=True, cielo=False):
    """EL CORTE UNIVERSAL: una imagen (o una TIRA de N frames verticales)
    → todo el fondo transparente, frame por frame (cada frame puede
    tener su propio fondo). Devuelve (RGBA, [estadísticas por frame])."""
    if frames <= 1:
        out, pct, fondo, cob = _cortar_fondo(im, tolerancia, peine=peine, cielo=cielo)
        return out, [(pct, fondo, cob)]
    w, h = im.size
    alto_f = h // frames
    if alto_f * frames != h:
        raise SystemExit(f'la tira {w}x{h} no se parte en {frames} frames')
    trozos, stats = [], []
    for f in range(frames):
        cuadro = im.crop((0, f * alto_f, w, (f + 1) * alto_f))
        out, pct, fondo, cob = _cortar_fondo(cuadro, tolerancia, peine=peine, cielo=cielo)
        trozos.append(out)
        stats.append((pct, fondo, cob))
    tira = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    for f, t in enumerate(trozos):
        tira.paste(t, (0, f * alto_f))
    return tira, stats


def _damero(w, h, c1=(46, 46, 58), c2=(66, 66, 82), t=8):
    """El damero de la transparencia (para VER el corte)."""
    im = Image.new('RGB', (w, h), c1)
    d = ImageDrawDraw(im)
    for y in range(0, h, t):
        for x in range(0, w, t):
            if (x // t + y // t) % 2 == 0:
                d.rectangle([x, y, x + t - 1, y + t - 1], fill=c2)
    return im


def ImageDrawDraw(im):
    from PIL import ImageDraw
    return ImageDraw.Draw(im)


# ====================================================================
#  ETAPA 4 — EL ESPEJO (framelet: el sprite estático → la tira viva)
# ====================================================================
def _canvas_como(sprite):
    return Image.new('RGBA', sprite.size, (0, 0, 0, 0))


def _anclar_abajo(fr, base):
    """el frame anclado al PISO (los pies no patinan: la casa del idle)"""
    c = _canvas_como(base)
    x = (base.width - fr.width) // 2
    y = base.height - fr.height
    c.paste(fr, (x, max(0, y)), fr)
    return c


def _squash(fr, sy, sx=None):
    """LA RESPIRACIÓN: squash & stretch en antifase (el volumen vive)"""
    sx = sx if sx is not None else 1.0 / sy
    w, h = fr.size
    nw, nh = max(1, round(w * sx)), max(1, round(h * sy))
    return fr.resize((nw, nh), Image.NEAREST)


def _vaiven(fr, ang):
    """EL BALANCEO: rotación NEAREST anclada al piso"""
    if abs(ang) < 0.2:
        return fr
    w, h = fr.size
    rot = fr.rotate(ang, resample=Image.NEAREST, center=(w / 2, h),
                    fillcolor=(0, 0, 0, 0))
    return rot.crop((0, 0, w, h))


def _brillo(fr, k):
    """EL LATIDO: el pulso de brillo (k > 1 brilla, k < 1 se apaga)"""
    if abs(k - 1.0) < 0.02:
        return fr
    r, g, b, a = fr.split()
    r = r.point(lambda v: min(255, int(v * k)))
    g = g.point(lambda v: min(255, int(v * k)))
    b = b.point(lambda v: min(255, int(v * k)))
    out = Image.merge('RGBA', (r, g, b, a))
    return out


def el_espejo(sprite, animacion='vida', frames=6):
    """LA TIRA ANIMADA de la casa (la técnica del Vástago del Eterno de
    la .91, automatizada): los ciclos SIN animar a mano —
      vida     = respirar + el vaivén sutil (el idle de la mascota)
      respirar = squash & stretch antifase, anclado al piso
      flotar   = bob vertical + inclinación (el vuelo)
      latir    = el pulso de brillo (los objetos mágicos)
      vaiven   = el balanceo (las campanas, los inciensos)
    Devuelve la lista de frames (todos del tamaño del original)."""
    random.seed(11)
    out = []
    w, h = sprite.size
    for f in range(frames):
        t = f / max(1, frames)             # el ciclo COMPLETO: el frame
        fase = 2 * math.pi * t             # 0..2π (y el último = el primero)
        if animacion == 'respirar':
            sy = 1.0 + 0.07 * math.sin(fase)
            fr = _anclar_abajo(_squash(sprite, sy), sprite)
        elif animacion == 'flotar':
            dy = round(2.5 * math.sin(fase))
            ang = 4.5 * math.cos(fase)
            fr = _canvas_como(sprite)
            rot = _vaiven(sprite, ang)
            fr.paste(rot, (0, dy), rot)
        elif animacion == 'latir':
            k = 1.0 + 0.30 * (0.5 + 0.5 * math.sin(fase))
            fr = _brillo(sprite, k)
        elif animacion == 'vaiven':
            fr = _anclar_abajo(_vaiven(sprite, 8.0 * math.sin(fase)), sprite)
        else:  # vida — el idle completo
            sy = 1.0 + 0.07 * math.sin(fase)
            ang = 4.5 * math.cos(fase)
            fr = _anclar_abajo(_vaiven(_squash(sprite, sy), ang), sprite)
        out.append(fr)
    print(f'  EL ESPEJO: {frames} frames del ciclo «{animacion}»')
    return out


def la_tira(frames_list, destino, ruta_tml=None):
    """LA TIRA VERTICAL de tML (la convención de siempre: los frames
    apilados; AnimationFrameHeight = el alto de UN frame) + el
    manifiesto JSON con todo."""
    w = max(f.width for f in frames_list)
    h = max(f.height for f in frames_list)
    tira = Image.new('RGBA', (w, h * len(frames_list)), (0, 0, 0, 0))
    for i, f in enumerate(frames_list):
        tira.paste(f, ((w - f.width) // 2, i * h + (h - f.height) // 2), f)
    tira.save(destino)
    manifiesto = {
        'tira': destino,
        'frames': len(frames_list),
        'frame_w': w, 'frame_h': h,
        'tira_w': w, 'tira_h': h * len(frames_list),
        'tML': {
            'AnimationFrameHeight': h,
            'nota': ('SetAnimationFrame(this) con AnimationFrameHeight = frame_h '
                     'y FrameCounter/MainFrameY por tiempo — la convención v6.97 '
                     'del AltarOfrendasTile'),
        },
        'destino_sugerido': ruta_tml,
    }
    with open(os.path.splitext(destino)[0] + '_manifiesto.json', 'w') as f:
        json.dump(manifiesto, f, indent=2, ensure_ascii=False)
    print(f'  LA TIRA: {destino} ({w}x{h * len(frames_list)} = {len(frames_list)} frames de {w}x{h})')
    return destino, manifiesto


# ====================================================================
#  ETAPA 5 — EL ALMA (pixelfx: el efecto que lo da vida — 9 efectos)
# ====================================================================
def el_alma(sprite, efecto='brasa', frames=3):
    """EL PREVIEW VIVO: cada efecto, un trocito de código (como manda el
    reel de pixelfx). Los 4 de la casa (brasa · humo · glitch ·
    consagración) + LOS CINCO NUEVOS de la investigación VFX (fuego ·
    hielo · rayo · escudo · portal)."""
    random.seed(7)
    out = []
    w, h = sprite.size
    px = sprite.load()
    for f in range(frames):
        fr = sprite.copy()
        pf = fr.load()
        t = f / max(1, frames - 1)          # el tiempo de PROGRESO (0 → 1)
        tl = f / frames                      # el tiempo de BUCLE (0 → 1-1/n:
                                             # el frame N-1 empalma con el 0)
        random_shift = (random.uniform(0, 6.28), random.uniform(0, 6.28))
        for y in range(h):
            for x in range(w):
                r, g, b, a = px[x, y]
                if a == 0:
                    continue
                if efecto == 'brasa':
                    n = (math.sin(x * 1.7 + tl * 9.0) * 0.5 + 0.5) * \
                        (math.sin(y * 0.9 - tl * 14.0) * 0.5 + 0.5)
                    if n > 0.62 and y > h * 0.45:
                        k = (n - 0.62) / 0.38
                        pf[x, y] = (int(r + (255 - r) * k),
                                    int(g * (1 - k * 0.7)),
                                    int(b * (1 - k * 0.9)), 255)
                elif efecto == 'humo':
                    n = math.sin(x * 0.8 + tl * 6.0 + y * 0.35) * 0.5 + 0.5
                    if y < h * 0.4 and n > 0.55 and random.random() < 0.5:
                        k = (1 - y / (h * 0.4)) * 0.5
                        pf[x, y] = (int(r + (200 - r) * k),
                                    int(g + (200 - g) * k),
                                    int(b + (200 - b) * k), 255)
                elif efecto == 'glitch':
                    if (y + f * 5) % 11 < 2 and random.random() < 0.8:
                        dx = random.choice([-3, -2, 2, 3])
                        nx = min(w - 1, max(0, x + dx))
                        pf[x, y] = px[nx, y]
                elif efecto == 'consagracion':
                    linea = int(h * (0.15 + t * 0.8))
                    if abs(y - linea) < 2 and random.random() < 0.8:
                        pf[x, y] = (240, 0, 192, 255)
                # ==== LOS CINCO NUEVOS (la investigación VFX) ====
                elif efecto == 'fuego':
                    # EL FUEGO QUE QUEMA DE VERDAD: la FRONTERA LLAMEANTE
                    # por columna (dos ondas que suben y bajan en bucle) + el
                    # GRADIENTE DEL INCENDIO con textura (amarillo candente
                    # abajo → naranja → rojo → chamuscado arriba) — el objeto
                    # sigue LEÍBLE debajo (la cima nunca se quema del todo)
                    onda1 = math.sin(x * 0.9 + tl * 2 * math.pi * 2) * 0.5 + 0.5
                    onda2 = math.sin(x * 2.7 - tl * 2 * math.pi * 3 + random_shift[0]) * 0.5 + 0.5
                    frente = h * (0.45 + 0.20 * onda1 + 0.12 * onda2)
                    if y > h - frente:                       # la zona quemada
                        kk = (y - (h - frente)) / max(1.0, frente)
                        textura = 0.9 + 0.1 * math.sin(x * 0.6 + y * 1.4 + tl * 4 * math.pi)
                        kk = (0.42 + 0.58 * min(1.0, kk)) * textura   # el fuego manda pero el objeto respira
                        kk = min(1.0, kk)
                        fr1 = int(255 * kk + r * (1 - kk))
                        fg1 = int(215 * kk * kk + g * (1 - kk))
                        fb1 = int(55 * kk * kk * kk + b * (1 - kk))
                        pf[x, y] = (min(255, fr1), min(255, fg1), min(255, fb1), 255)
                    elif random.random() < 0.035 and y < h * 0.25:
                        # las chispas que escapan arriba
                        pf[x, y] = (255, 190, 70, 255)
                elif efecto == 'hielo':
                    # EL HIELO: el cristal que crece desde arriba + destellos
                    frente = int(h * (1.05 - t * 0.75))
                    if y <= frente and random.random() < 0.16:
                        pf[x, y] = (int(r * 0.45), int(g * 0.85 + 70), 255, 255)
                    elif y <= frente:
                        k = 0.28 * (1 - y / max(1, frente))
                        pf[x, y] = (int(r * (1 - k)), int(g * (1 - k * 0.2)), 255, 255)
                elif efecto == 'rayo':
                    # EL RAYO: el fogonazo blanco en UN frame + el borde eléctrico
                    if f == frames // 2:
                        pf[x, y] = (255, 255, 255, 255)
                    elif random.random() < 0.045:
                        pf[x, y] = (140, 240, 255, 255)
                elif efecto == 'escudo':
                    # EL ESCUDO: la cáscara a un píxel de la silueta que late
                    es_borde = False
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < w and 0 <= ny < h and px[nx, ny][3] == 0:
                            es_borde = True
                            break
                    if es_borde:
                        k = 0.5 + 0.5 * math.sin(tl * 2 * math.pi)
                        pf[x, y] = (int(r * (1 - k * 0.6)),
                                    int(g * (1 - k * 0.1)),
                                    255, 255)
                elif efecto == 'portal':
                    # EL PORTAL VIVO: la ESPIRAL de tres brazos que gira
                    # alrededor del centro (la firma visual del portal) + el
                    # tono que gira con la distancia (fucsia→violeta→teal)
                    dx, dy = x - w / 2, y - h / 2
                    d = math.sqrt(dx * dx + dy * dy) / (max(w, h) / 2)
                    ang = math.atan2(dy, dx)
                    brazo = math.sin(3 * ang + d * 7.0 - tl * 2 * math.pi)
                    tono = (tl + d * 0.5) % 1.0
                    rr, gg, bb = colorsys.hsv_to_rgb(0.56 + 0.30 * tono, 0.9, 1.0)
                    # los brazos brillan hasta el blanco; los valles se
                    # oscurecen (la espiral se VE girar)
                    k = max(0.22, 0.5 + 0.48 * brazo)
                    pf[x, y] = (int(r * (1 - k) + 255 * rr * k),
                                int(g * (1 - k) + 255 * gg * k),
                                int(b * (1 - k) + 255 * bb * k), 255)
        # v6.121 — EL TEATRO DE PIXELFX: los efectos nuevos viven
        # ALREDEDOR del sprite (el lienzo extendido + el doble búfer).
        # Los 9 de siempre vuelven INTACTOS (ni un píxel tocado).
        if efecto in _EFECTOS_PIXELFX:
            out.append(_teatro_pixelfx(efecto, sprite, f, frames))
        else:
            out.append(fr)
    print(f'  EL ALMA: {frames} frames del efecto «{efecto}»')
    return out


# ====================================================================
#  v6.121 — EL MOTOR DE PIXELFX ADAPTADO (doble búfer + lienzo
#  extendido): cada efecto, UN TROCITO DE CÓDIGO (draw(p, t, o))
# ====================================================================
def _luz_aditiva(base, luz):
    """LA COMPOSICIÓN DE PIXELFX — el búfer de LUZ se SUMA en
    premultiplied alpha: result.rgb = base.rgb·base.a + luz.rgb·luz.a;
    result.a = max. El brillo NUNCA lava: se ACUMULA (aditivo de
    verdad, no un alpha_composite encima)."""
    import numpy as np
    b = np.asarray(base.convert('RGBA'), dtype=np.float32) / 255.0
    l = np.asarray(luz.convert('RGBA'), dtype=np.float32) / 255.0
    out = b.copy()
    ba = b[..., 3:4]
    out[..., :3] = np.clip(b[..., :3] * ba + l[..., :3] * l[..., 3:4], 0.0, 1.0)
    out[..., 3:4] = np.clip(ba + l[..., 3:4], 0.0, 1.0)
    return Image.fromarray((out * 255.0).round().astype('uint8'))


def _teatro_pixelfx(efecto, sprite, frame, frames):
    """EL FRAME VIVO del efecto de teatro: el lienzo extendido
    (sprite al centro + _PAD por lado), el búfer de COLOR (píxeles
    sólidos del efecto) y el búfer de LUZ (el resplandor aditivo).
    Determinista: TODO sale de la fase (la semilla ya la puso
    el_alma)."""
    fase = frame / max(1, frames)
    w, h = sprite.size
    W, H = w + _PAD * 2, h + _PAD * 2
    lienzo = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    lienzo.paste(sprite, (_PAD, _PAD))
    color = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    luz = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    dc = ImageDraw.Draw(color)
    dl = ImageDraw.Draw(luz)
    cx, cy = W / 2.0, H / 2.0
    R = max(w, h) / 2.0

    # === EL ECLIPSE (shadow: «eclipse») — LA FIRMA DEL MOD =========
    if efecto == 'eclipse':
        # el disco negro en el cenit del sprite
        dr = R * 0.52
        dx, dy = cx, _PAD + h * 0.06
        pulso = 0.5 + 0.5 * math.sin(fase * math.pi * 2)
        dc.ellipse([dx - dr, dy - dr, dx + dr, dy + dr], fill=(12, 6, 22, 255))
        dc.ellipse([dx - dr, dy - dr, dx + dr, dy + dr],
                   outline=LILA_V + (215,), width=2)
        # LA CORONA: 12 radios alternos (el abanico del dios)
        for k in range(12):
            a = fase * math.pi / 3 + k * math.pi / 6
            largo = dr * (1.18 + (0.42 if k % 2 == 0 else 0.18) + 0.10 * pulso)
            x1 = dx + math.cos(a) * dr * 1.06
            y1 = dy + math.sin(a) * dr * 1.06
            x2 = dx + math.cos(a) * largo
            y2 = dy + math.sin(a) * largo
            dc.line([(x1, y1), (x2, y2)],
                    fill=VIOLETA_V + (235,), width=2)
        # el aro de la corona (el abrazo)
        dc.ellipse([dx - dr * 1.10, dy - dr * 1.10, dx + dr * 1.10, dy + dr * 1.10],
                   outline=LILA_V + (200,), width=2)
        # LA LUZ: el resplandor violeta del cenit
        dl.ellipse([dx - dr * 1.7, dy - dr * 1.7, dx + dr * 1.7, dy + dr * 1.7],
                   fill=VIOLETA_V + (int(46 + 30 * pulso),))
        dl.ellipse([dx - dr * 1.15, dy - dr * 1.15, dx + dr * 1.15, dy + dr * 1.15],
                   fill=LILA_V + (int(30 + 24 * pulso),))

    # === EL SIGILO (shadow: «hex sigil») ============================
    elif efecto == 'sigilo':
        rx, ry = w * 0.72, h * 0.46
        gira = fase * math.pi * 2
        dc.ellipse([cx - rx, cy - ry, cx + rx, cy + ry],
                   outline=ORO_H + (225,), width=2)
        dc.ellipse([cx - rx * 0.72, cy - ry * 0.72, cx + rx * 0.72, cy + ry * 0.72],
                   outline=VIOLETA_V + (160,), width=1)
        # LAS 8 MUESCAS RÚNICAS caminando (el conjuro orbitando)
        for k in range(8):
            a = gira + k * math.pi / 4
            mx = cx + math.cos(a) * rx
            my = cy + math.sin(a) * ry
            dc.line([(mx - 2, my - 4), (mx + 2, my + 4)],
                    fill=ORO_V + (255,), width=2)
        # el ojo del sigilo (el punto que todo lo ve)
        dc.ellipse([cx - 4, cy - 4, cx + 4, cy + 4], fill=ORO_V + (255,))
        # LA LUZ: el círculo arde detrás del sprite
        dl.ellipse([cx - rx, cy - ry, cx + rx, cy + ry],
                   fill=VIOLETA_V + (36,))
        dl.ellipse([cx - 10, cy - 10, cx + 10, cy + 10],
                   fill=ORO_V + (60,))

    # === EL AGUJERO (shadow: «black hole») =========================
    elif efecto == 'agujero':
        r_in = R * 0.30
        r_out = R * 0.52
        gira = fase * math.pi * 2
        # EL POZO (el negro que devora la luz)
        dc.ellipse([cx - r_in, cy - r_in, cx + r_in, cy + r_in],
                   fill=(8, 4, 16, 255))
        # EL ANILLO DE ACRECIÓN (violeta → oro: la espiral de tres
        # brazos que gira — el pozo come y el anillo brilla)
        for paso in range(3):
            fr = paso / 3
            rr = r_in + (r_out - r_in) * (0.35 + 0.65 * fr)
            col = tuple(int(VIOLETA_V[i] * (1 - fr) + ORO_V[i] * fr) for i in range(3))
            for k in range(3):
                a0 = gira + k * math.pi * 2 / 3 + fr * 0.9
                a1 = a0 + 0.55
                pasos = 7
                pts = []
                for s in range(pasos + 1):
                    aa = a0 + (a1 - a0) * s / pasos
                    pts.append((cx + math.cos(aa) * rr, cy + math.sin(aa) * rr))
                dc.line(pts, fill=col + (235,), width=3)
        # LA FRONTERA (el horizonte de eventos)
        dc.ellipse([cx - r_in * 1.12, cy - r_in * 1.12, cx + r_in * 1.12, cy + r_in * 1.12],
                   outline=(6, 3, 12, 255), width=2)
        # LOS DESPOJOS orbitando hacia adentro
        for k in range(5):
            a = -gira * 1.6 + k * math.pi * 2 / 5
            rr = r_out * (1.15 - 0.22 * ((fase + k / 5) % 1.0))
            dx2 = cx + math.cos(a) * rr
            dy2 = cy + math.sin(a) * rr * 0.82
            dc.ellipse([dx2 - 2, dy2 - 2, dx2 + 2, dy2 + 2], fill=ORO_V + (255,))
        # LA LUZ: el pozo arde alrededor
        dl.ellipse([cx - r_out * 1.02, cy - r_out * 0.88, cx + r_out * 1.02, cy + r_out * 0.88],
                   fill=VIOLETA_V + (66,))
        dl.ellipse([cx - r_in * 1.25, cy - r_in * 1.25, cx + r_in * 1.25, cy + r_in * 1.25],
                   fill=ORO_V + (58,))

    # === EL ORBE (arcane: «arcane orb») ============================
    elif efecto == 'orbe':
        bob = math.sin(fase * math.pi * 2) * 5.0
        ox, oy = cx, cy - h * 0.16 + bob
        r = max(10.0, h * 0.13)
        # el núcleo (el cristal vivo)
        dc.ellipse([ox - r, oy - r, ox + r, oy + r], fill=VIOLETA_V + (255,))
        dc.ellipse([ox - r * 0.55, oy - r * 0.55, ox + r * 0.55, oy + r * 0.55],
                   fill=LILA_V + (255,))
        dc.ellipse([ox - r * 0.22, oy - r * 0.28, ox + r * 0.10, oy - r * 0.04],
                   fill=(240, 226, 255, 255))
        # LOS TRES SATÉLITES (la órbita de la casa)
        for k in range(3):
            a = fase * math.pi * 2 + k * math.pi * 2 / 3
            sx = ox + math.cos(a) * r * 2.1
            sy = oy + math.sin(a) * r * 0.8
            dc.ellipse([sx - 3, sy - 3, sx + 3, sy + 3], fill=ORO_V + (255,))
            dl.ellipse([sx - 5, sy - 5, sx + 5, sy + 5], fill=ORO_V + (70,))
            # la estela del satélite (los tres pasos previos)
            for t in (1, 2):
                aa = a - t * 0.16
                ex = ox + math.cos(aa) * r * 2.1
                ey = oy + math.sin(aa) * r * 0.8
                dc.ellipse([ex - 2, ey - 2, ex + 2, ey + 2],
                           fill=VIOLETA_V + (150 - t * 50,))
        # LA LUZ: el aura del orbe — LA CURA DEL OJO: CONTENIDA y
        # FUERTE (nítida, no un charco difuso)
        dl.ellipse([ox - r * 1.9, oy - r * 1.9, ox + r * 1.9, oy + r * 1.9],
                   fill=VIOLETA_V + (72,))
        dl.ellipse([ox - r * 1.25, oy - r * 1.25, ox + r * 1.25, oy + r * 1.25],
                   fill=LILA_V + (64,))

    # === LA DISOLUCIÓN (spawn&death: «dissolve burn») ==============
    elif efecto == 'disolucion':
        # EL CUERPO SE DESHACE de abajo hacia arriba (la devoración):
        # bajo la línea, los píxeles se vuelven brasas y se van
        # LA CURA DEL OJO: la línea NACE bajo el sprite (frame 0
        # INTACTO) y SUBE con los frames — antes el frame 0 ya venía
        # medio devorado
        linea = int(h * (1.02 - 1.15 * fase))
        sp = sprite.copy()
        ps = sp.load()
        for y in range(max(0, linea), h):
            for x in range(w):
                if ps[x, y][3] == 0:
                    continue
                k = (y - linea) / max(1, h - linea)
                if random.random() < 0.22 + 0.48 * k:
                    ps[x, y] = (int(255 * (1 - k * 0.5)), int(170 * (1 - k)), int(60 * (1 - k)), 255)
                elif random.random() < 0.30 * k:
                    ps[x, y] = (0, 0, 0, 0)
        lienzo.paste(sp, (_PAD, _PAD))
        # LAS BRASAS: la FRONTERA ARDE siempre (la línea de la
        # devoración visible en CADA frame) + las que escapan arriba
        for k in range(10):
            sem = k * 13 + 5
            bx = _PAD + (sem * 37 % w)
            by = _PAD + max(0, min(h - 1, linea + (sem * 7 % 5) - 2))
            dc.ellipse([bx - 2, by - 2, bx + 2, by + 2], fill=(255, 190, 90, 255))
        for k in range(12):
            sem = k * 17 + 3
            bx = _PAD + (sem * 31 % w)
            by = _PAD + max(0, linea) + (sem * 53 % max(1, h - max(0, linea)))
            sube = (fase * 26 + (sem % 7) * 3) % (h * 0.5)
            dc.ellipse([bx - 2, by - sube, bx + 2, by - sube + 2],
                       fill=ORO_V + (235,),)
        dl.ellipse([cx - w * 0.5, _PAD + max(0, linea) - 6,
                    cx + w * 0.5, _PAD + max(0, linea) + 10],
                   fill=(250, 140, 60, 58))
        # LA LUZ: el resplandor de las brasas bajo la línea
        dl.ellipse([cx - w * 0.55, _PAD + max(0, linea) + h * 0.10,
                    cx + w * 0.55, _PAD + max(0, linea) + h * 0.55],
                   fill=(250, 140, 60, 44))

    # === EL AURA (holy: «halo») ====================================
    elif efecto == 'aura':
        respira = 0.5 + 0.5 * math.sin(fase * math.pi * 2)
        rx, ry = w * 0.42, h * 0.10
        hy2 = _PAD + h * 0.06
        # EL HALO DORADO sobre el sprite
        dc.ellipse([cx - rx, hy2 - ry, cx + rx, hy2 + ry],
                   outline=ORO_V + (255,), width=3)
        dc.ellipse([cx - rx * 0.72, hy2 - ry * 0.72, cx + rx * 0.72, hy2 + ry * 0.72],
                   outline=ORO_H + (180,), width=1)
        # LA COLUMNA DE LUZ detrás del sprite (el pilar del veredicto)
        ancho_col = w * (0.50 + 0.08 * respira)
        dl.polygon([(cx - ancho_col * 0.5, _PAD), (cx + ancho_col * 0.5, _PAD),
                    (cx + w * 0.28, _PAD + h), (cx - w * 0.28, _PAD + h)],
                   fill=ORO_V + (44,))
        # el halo arde
        dl.ellipse([cx - rx * 1.2, hy2 - ry * 1.6, cx + rx * 1.2, hy2 + ry * 1.6],
                   fill=ORO_V + (int(56 + 44 * respira),))

    # === EL DESTELLO (impacts: «hit flash») ========================
    elif efecto == 'destello':
        # frame 0: EL FOGONAZO — LA CURA DEL OJO: va al BÚFER DE LUZ
        # (aditivo blanco-violeta: el fondo oscuro VIVE, nada de gris plano)
        if frame == 0:
            dl.rectangle([_PAD, _PAD, _PAD + w, _PAD + h],
                         fill=(236, 214, 255, 132))
        # LA ONDA DE CHOQUE: el anillo que EXPANDE y se apaga —
        # LA CURA DEL OJO: en el frame 0 el anillo NACE GRUESO al
        # centro (el destello SE LEE desde el primer marco)
        crece = fase
        r = R * (0.22 + 1.05 * crece)
        alfa = int(245 * (1.0 - crece) ** 1.2)
        dc.ellipse([cx - r, cy - r * 0.92, cx + r, cy + r * 0.92],
                   outline=VIOLETA_V + (alfa,), width=4 if fase < 0.5 else 3)
        r2 = r * 0.62
        dc.ellipse([cx - r2, cy - r2 * 0.92, cx + r2, cy + r2 * 0.92],
                   outline=ORO_V + (max(0, alfa - 40),), width=2)
        # LAS CHISPAS DEL IMPACTO (6, fijas, encendidas al inicio)
        for k in range(6):
            a = k * math.pi / 3 + 0.35
            d2 = R * (0.42 + 0.62 * crece)
            sx = cx + math.cos(a) * d2
            sy = cy + math.sin(a) * d2 * 0.9
            dc.ellipse([sx - 2, sy - 2, sx + 2, sy + 2],
                       fill=ORO_V + (max(0, alfa),))
        # LA LUZ: el impacto arde
        dl.ellipse([cx - r, cy - r, cx + r, cy + r],
                   fill=LILA_V + (max(0, alfa // 3),))

    # === LAS RUNAS (arcane: «magic circle cast») ===================
    elif efecto == 'runas':
        gira = fase * math.pi * 2
        rx, ry = w * 0.66, h * 0.50
        # EL CÍRCULO DEL CONJURO (el elipsoide guía)
        dc.ellipse([cx - rx, cy - ry, cx + rx, cy + ry],
                   outline=ORO_H + (215,), width=2)
        # LAS SEIS RUNAS ORBITANDO (los glifos de la casa: T,L,I)
        glifos = [[(0, -5), (0, 5)], [(-3, -5), (0, -2), (0, 5)], [(-4, 2), (4, 2)],
                  [(0, -5), (0, 0), (4, 0)], [(-3, 0), (3, 0), (3, 5)], [(0, -5), (-4, 5), (4, 5)]]
        for k in range(6):
            a = gira + k * math.pi / 3
            mx = cx + math.cos(a) * rx
            my = cy + math.sin(a) * ry
            parpadeo = 195 + int(60 * (0.5 + 0.5 * math.sin(fase * math.pi * 4 + k)))
            for gl in glifos[k]:
                dc.line([(mx + gl[0] * 1.0, my + gl[1] * 1.0),
                         (mx + gl[0] * 1.0, my + gl[1] * 1.0 + 3)],
                        fill=ORO_V + (parpadeo,), width=3)
            # la luz de la runa — LA CURA DEL OJO: viva, no tenue
            dl.ellipse([mx - 6, my - 6, mx + 6, my + 6], fill=ORO_V + (76,))
        # LA LUZ del círculo entero
        dl.ellipse([cx - rx * 0.8, cy - ry * 0.8, cx + rx * 0.8, cy + ry * 0.8],
                   fill=VIOLETA_V + (26,))

    # === LA COMPOSICIÓN DE PIXELFX: color ENCIMA, LUZ ADITIVA =====
    lienzo.alpha_composite(color)
    return _luz_aditiva(lienzo, luz)


# ====================================================================
#  ETAPA 6 — EL TAMBOR (el dithering de la paleta contada)
# ====================================================================
BAYER4 = [[0, 8, 2, 10],
          [12, 4, 14, 6],
          [3, 11, 1, 9],
          [15, 7, 13, 5]]


def _bayer8():
    """la matriz Bayer 8×8 de la casa (los dos niveles del 4×4
    entrelazados: el umbral fino por celda, 0..63)"""
    m = [[0] * 8 for _ in range(8)]
    for y in range(8):
        for x in range(8):
            m[y][x] = BAYER4[y % 4][x % 4] * 4 + BAYER4[(y + 1) % 4][(x + 1) % 4]
    return m


def _paleta_rgb(palette=None, colores=None, im=None):
    """la paleta de la casa o la cuantización a N colores de la imagen"""
    if palette == 'casa':
        return [c[:3] for c in PALETA if len(c) >= 3]
    if colores and im is not None:
        q = im.convert('RGB').quantize(colors=colores, method=Image.MEDIANCUT)
        pal = q.getpalette()
        return [tuple(pal[i:i + 3]) for i in range(0, colores * 3, 3)]
    return [c[:3] for c in PALETA if len(c) >= 3]


def el_tambor(origen, destino, matriz='bayer4', colores=None, palette=None):
    """EL DIFUMINADO: Bayer ordenado (el tejido regular de los velos) o
    Floyd-Steinberg (la difusión de error — los degradados suaves). La
    paleta: la de la casa (--palette casa) o la de la imagen contada a N
    colores (--colores N)."""
    im = Image.open(origen).convert('RGB')
    w, h = im.size
    px = im.load()
    pal = _paleta_rgb(palette, colores, im)

    def mas_cercano(c):
        best, bd = None, 1 << 30
        for p in pal:
            d = (c[0] - p[0]) ** 2 + (c[1] - p[1]) ** 2 + (c[2] - p[2]) ** 2
            if d < bd:
                best, bd = p, d
        return best

    out = Image.new('RGB', (w, h))
    po = out.load()
    if matriz in ('bayer4', 'bayer8'):
        m = BAYER4 if matriz == 'bayer4' else _bayer8()
        n = len(m)
        for y in range(h):
            for x in range(w):
                r, g, b = px[x, y]
                # EL UMBRAL ORDENADO: el vaivén periódico de la matriz
                # (u-0.5 ∈ [-½, ½)) empuja el color antes de buscarlo
                # en la paleta — el tejido regular de los velos.
                k = ((m[y % n][x % n] + 0.5) / (n * n) - 0.5) * 56.0
                c = mas_cercano((max(0, min(255, int(r + k))),
                                 max(0, min(255, int(g + k))),
                                 max(0, min(255, int(b + k)))))
                po[x, y] = c
    else:  # floyd
        err = [[(0, 0, 0)] * w for _ in range(h)]
        for y in range(h):
            for x in range(w):
                r, g, b = px[x, y]
                er, eg, eb = err[y][x]
                c = mas_cercano((r + er, g + eg, b + eb))
                dr, dg, db = r + er - c[0], g + eg - c[1], b + eb - c[2]
                for dx, dy, k in ((1, 0, 7 / 16), (-1, 1, 3 / 16), (0, 1, 5 / 16), (1, 1, 1 / 16)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and ny < h:
                        pr, pg, pb = err[ny][nx]
                        err[ny][nx] = (pr + dr * k, pg + dg * k, pb + db * k)
                po[x, y] = c
    out.save(destino)
    print(f'  EL TAMBOR: {matriz} a {len(pal)} colores → {destino}')
    return destino


# ====================================================================
#  ETAPA 7 — LA RAMPA (el hue-shift de las tres casas)
# ====================================================================
def la_rampa(base_hex, pasos=7, hue=24, frio=262, calido=42, destino=None):
    """LA RAMPA DE LA CASA: del color base a la escalera completa —
    las SOMBRAS giran hacia el FRÍO (violeta 262°), las LUCES hacia el
    CÁLIDO (ámbar 42°), como máximo `hue` grados (la ley del hue-shift
    del pixel arte de verdad). Sale el PNG de muestras y los HEX listos
    para el prompt de la pluma."""
    def hacia(h_from, h_to):
        """el camino más corto entre dos tonos, en vueltas [-½, ½)"""
        d = (h_to - h_from) % 1.0
        if d > 0.5:
            d -= 1.0
        return d

    base = base_hex.lstrip('#')
    r, g, b = (int(base[i:i + 2], 16) for i in (0, 2, 4))
    hh, ss, vv = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    h_frio, h_calido = frio / 360.0, calido / 360.0
    colores = []
    for i in range(pasos):
        v = 0.22 + 0.73 * (i / max(1, pasos - 1))          # oscuridad → luz
        k = (i / max(1, pasos - 1)) * 2 - 1                 # -1 sombra … +1 luz
        if k < 0:
            # LA SOMBRA: gira hacia el frío (máx. `hue`°) y satura un poco
            d = hacia(hh, h_frio)
            salto = math.copysign(min(abs(d), hue / 360.0), d) if d else 0.0
            hn = hh + salto * (-k)
            sn = min(1.0, ss * (1 + 0.18 * -k))
        else:
            # LA LUZ: gira hacia el cálido (máx. `hue`°) y desatura un poco
            d = hacia(hh, h_calido)
            salto = math.copysign(min(abs(d), hue / 360.0), d) if d else 0.0
            hn = hh + salto * k
            sn = max(0.0, ss * (1 - 0.28 * k))
        rn, gn, bn = colorsys.hsv_to_rgb(hn % 1.0, sn, v)
        colores.append((int(rn * 255), int(gn * 255), int(bn * 255)))
    if destino:
        sw = 36
        lienzo = Image.new('RGB', (sw * pasos, sw), (24, 24, 34))
        for i, c in enumerate(colores):
            for y in range(sw):
                for x in range(sw):
                    lienzo.putpixel((i * sw + x, y), c)
        lienzo.save(destino)
    hexes = ['#%02X%02X%02X' % c for c in colores]
    print('  LA RAMPA (%s, %d pasos, hue %d°):' % (base_hex, pasos, hue))
    print('   ', ' '.join(hexes))
    if destino:
        print('   ', destino)
    return colores, hexes, destino


# ====================================================================
#  ETAPA 8 — EL ATLAS (la tira tML + el empaque compacto + el manifiesto)
# ====================================================================
def _huella(im):
    """la huella de píxeles (la detección de duplicados del empaque)"""
    return hashlib.md5(im.tobytes()).hexdigest()


def el_atlas(frames_paths, destino_base, tira=True, maxrects=True, bin_w=512, bin_h=512):
    """EL EMPAQUE: (a) LA TIRA VERTICAL de tML (la convención del juego)
    y (b) EL ATLAS COMPACTO — el empaque de la casa: los frames por
    altura, cada uno a su mejor hueco (la idea guillotina por mejor
    lado), con los DUPLICADOS contados una sola vez (misma huella =
    mismo hueco). + el manifiesto JSON con todo."""
    frames = [Image.open(p).convert('RGBA') for p in frames_paths]
    manifiesto = {'origen': [os.path.basename(p) for p in frames_paths], 'frames': len(frames)}
    resultado = {}
    if tira and frames:
        w = max(f.width for f in frames)
        h = max(f.height for f in frames)
        tira_im = Image.new('RGBA', (w, h * len(frames)), (0, 0, 0, 0))
        for i, f in enumerate(frames):
            tira_im.paste(f, ((w - f.width) // 2, i * h + (h - f.height) // 2), f)
        p_tira = destino_base + '_tira.png'
        tira_im.save(p_tira)
        manifiesto['tira'] = {'archivo': p_tira, 'frame_w': w, 'frame_h': h,
                              'frames': len(frames),
                              'tML_AnimationFrameHeight': h}
        resultado['tira'] = p_tira
        print(f'  EL ATLAS (tira tML): {p_tira} ({len(frames)} frames de {w}x{h})')
    if maxrects and frames:
        # los duplicados: una huella, un hueco
        vistas, unicos = {}, []
        for f in frames:
            hsh = _huella(f)
            if hsh not in vistas:
                vistas[hsh] = len(unicos)
                unicos.append(f)
        # el orden: los más altos primero (la estantería de la casa)
        unicos.sort(key=lambda f: (-f.height, -f.width))
        atlas = Image.new('RGBA', (bin_w, bin_h), (0, 0, 0, 0))
        libres = [(0, 0, bin_w, bin_h)]        # (x, y, w, h) — guillotina
        posiciones = []
        for f in unicos:
            mejor, mejor_x, mejor_y = None, 0, 0
            for (x, y, w_, h_) in libres:
                if f.width <= w_ and f.height <= h_:
                    sobra = (w_ - f.width) * (h_ - f.height)
                    if mejor is None or sobra < mejor:
                        mejor, mejor_x, mejor_y = sobra, x, y
            if mejor is None:
                continue                        # no cabe: se queda fuera (aviso)
            atlas.paste(f, (mejor_x, mejor_y), f)
            posiciones.append({'x': mejor_x, 'y': mejor_y, 'w': f.width, 'h': f.height})
            # la guillotina: el hueco usado se parte en dos (derecha y abajo)
            nuevos = []
            for (x, y, w_, h_) in libres:
                if (mejor_x < x + w_ and mejor_x + f.width > x and
                        mejor_y < y + h_ and mejor_y + f.height > y):
                    if mejor_x + f.width < x + w_:
                        nuevos.append((mejor_x + f.width, y, x + w_ - (mejor_x + f.width), h_))
                    if mejor_y + f.height < y + h_:
                        nuevos.append((x, mejor_y + f.height, w_, y + h_ - (mejor_y + f.height)))
                    if mejor_x > x:
                        nuevos.append((x, y, mejor_x - x, h_))
                    if mejor_y > y:
                        nuevos.append((x, y, w_, mejor_y - y))
                else:
                    nuevos.append((x, y, w_, h_))
            libres = nuevos
        p_atlas = destino_base + '_atlas.png'
        atlas.save(p_atlas)
        manifiesto['atlas'] = {'archivo': p_atlas, 'bin': [bin_w, bin_h],
                               'unicos': len(unicos), 'duplicados': len(frames) - len(unicos),
                               'posiciones': posiciones}
        resultado['atlas'] = p_atlas
        print(f'  EL ATLAS (compacto): {p_atlas} ({len(unicos)} únicos, '
              f'{len(frames) - len(unicos)} duplicados fundidos)')
    p_json = destino_base + '.json'
    with open(p_json, 'w') as f:
        json.dump(manifiesto, f, indent=2, ensure_ascii=False)
    resultado['manifiesto'] = p_json
    print(f'  EL MANIFIESTO: {p_json}')
    return resultado


# ====================================================================
#  EL MONTAJE + EL OJO (todo junto para el ojo — y el ojo de la casa)
# ====================================================================
def el_montaje(carpeta, sprite, hl, piezas_extra, x=8, nombre_base='montaje_qa'):
    piezas = []
    if sprite is not None:
        piezas.append(('sprite x%d' % x, sprite.resize((sprite.width * x, sprite.height * x), Image.NEAREST)))
        if hl is not None:
            piezas.append(('highlight x%d' % x, hl.resize((hl.width * x, hl.height * x), Image.NEAREST)))
    for nombre, img in piezas_extra:
        piezas.append((nombre, img.resize((img.width * x, img.height * x), Image.NEAREST)
                       if img.width < 200 else img))
    if not piezas:
        return None
    alto = max(p[1].height for p in piezas) + 30
    ancho = sum(p[1].width for p in piezas) + 20 * (len(piezas) + 1)
    lienzo = Image.new('RGBA', (ancho, alto), (24, 24, 34, 255))
    dx = 20
    for nombre, img in piezas:
        lienzo.paste(img, (dx, 22), img)
        dx += img.width + 20
    destino = os.path.join(carpeta, f'{nombre_base}.png')
    lienzo.save(destino)
    print(f'  EL MONTAJE: {destino}')
    return destino


def el_ojo(montaje, destino_txt, contexto='sprite'):
    """EL OJO DE LA CASA (el QA VLM — el paso «mirar» de pxl, ahora
    dentro de la cadena): la nota y los defectos, listos para LA REFINA."""
    r = subprocess.run(['z-ai', 'vision', '-p',
                        f"QA this pixel art {contexto} montage for a Terraria-style mod. "
                        f"Rate 0-10: silhouette readability, palette consistency, "
                        f"outline quality, overall charm. List concrete defects (max 5, "
                        f"short). Answer in Spanish, 4 lines max.",
                        '-i', montaje], capture_output=True, text=True, timeout=180)
    crudo = (r.stdout or '') + '\n' + (r.stderr or '')
    veredicto = ''
    # el stdout trae el JSON de la visión: sacar SOLO el contenido
    ini = (r.stdout or '').find('{')
    if ini >= 0:
        try:
            d = json.loads(r.stdout[ini:])
            veredicto = d['choices'][0]['message']['content'].strip()
        except Exception:
            veredicto = ''
    if not veredicto:
        veredicto = crudo.strip()[-1500:] or 'EL OJO no respondió'
    with open(destino_txt, 'w') as f:
        f.write(veredicto + '\n')
    print('  EL OJO (QA VLM):')
    for linea in veredicto.splitlines()[:6]:
        print('   ', linea)
    return veredicto


# ====================================================================
#  LOS COMANDOS
# ====================================================================
def cmd_pluma(args):
    c = carpeta_de(args.nombre, args.dest)
    print('LA SUPERFORJA — LA PLUMA (pxl: el borrador IA)')
    borrador = os.path.join(c, 'borrador.png')
    if os.path.exists(borrador) and not args.forzar:
        print(f'  ya existe {borrador} (--forzar para regenerar)')
    else:
        la_pluma(args.prompt, borrador, args.tamano)
    if args.refinar:
        la_refina(borrador, args.refinar, os.path.join(c, 'borrador_refinado.png'), args.tamano)
    return borrador


def cmd_refina(args):
    c = carpeta_de(args.nombre, args.dest)
    origen = args.borrador or os.path.join(c, 'borrador.png')
    if not os.path.exists(origen):
        origen = os.path.join(c, 'borrador_refinado.png')
    if not os.path.exists(origen):
        raise SystemExit(f'no hay borrador que refinar en {c}')
    print('LA SUPERFORJA — LA REFINA (la edición dirigida)')
    la_refina(origen, args.nota, os.path.join(c, 'borrador_refinado.png'), args.tamano)


def cmd_puente(args):
    c = carpeta_de(args.nombre, args.dest)
    origen = args.borrador
    if not origen:
        for cand in ('borrador_refinado.png', 'borrador.png'):
            if os.path.exists(os.path.join(c, cand)):
                origen = os.path.join(c, cand)
                break
    if not origen or not os.path.exists(origen):
        raise SystemExit('falta --borrador (o pasa antes por la pluma)')
    print('LA SUPERFORJA — EL PUENTE (la rejilla nativa restaurada)')
    destino = os.path.join(c, 'nativo.png')
    el_puente(origen, destino)
    if args.x2:
        el_escalon(Image.open(destino), args.x2).save(os.path.join(c, f'nativo_x{2 ** args.x2}.png'))
        print(f'  EL ESCALÓN 2^{args.x2}: {c}/nativo_x{2 ** args.x2}.png')


def cmd_molde(args):
    c = carpeta_de(args.nombre, args.dest)
    origen = args.borrador
    if not origen:
        for cand in ('nativo.png', 'borrador_refinado.png', 'borrador.png'):
            if os.path.exists(os.path.join(c, cand)):
                origen = os.path.join(c, cand)
                break
    if not origen or not os.path.exists(origen):
        raise SystemExit('falta --borrador (o pasa antes por la pluma/el puente)')
    print('LA SUPERFORJA — EL MOLDE (sprite-gen: cutout → rejilla → paleta)')
    sprite, hl = el_molde(origen, args.ancho, args.alto,
                          con_contorno=not args.sin_contorno,
                          con_halo=not args.sin_halo)
    sprite.save(os.path.join(c, 'sprite.png'))
    hl.save(os.path.join(c, 'highlight.png'))
    print(f'  guardados: {c}/sprite.png + highlight.png')


def cmd_espejo(args):
    c = carpeta_de(args.nombre, args.dest)
    sprite = cargar_sprite(args)
    print('LA SUPERFORJA — EL ESPEJO (la tira animada procedimental)')
    frames = el_espejo(sprite, args.animacion, args.frames)
    la_tira(frames, os.path.join(c, 'tira.png'))
    # el GIF de la casa para VERLO moverse (el preview del ciclo)
    gif_frames = []
    for f in frames:
        f2 = f.resize((f.width * 6, f.height * 6), Image.NEAREST)
        fondo = Image.new('RGBA', f2.size, (24, 24, 34, 255))
        fondo.paste(f2, (0, 0), f2)
        gif_frames.append(fondo.convert('P', palette=Image.ADAPTIVE))
    gif_frames[0].save(os.path.join(c, 'ciclo.gif'), save_all=True,
                       append_images=gif_frames[1:], duration=140, loop=0)
    print(f'  EL CICLO (gif): {c}/ciclo.gif')


def cmd_alma(args):
    c = carpeta_de(args.nombre, args.dest)
    sprite = cargar_sprite(args)
    print('LA SUPERFORJA — EL ALMA (pixelfx: el efecto que lo da vida)')
    frames = el_alma(sprite, args.efecto, args.frames)
    for i, fr in enumerate(frames):
        fr.save(os.path.join(c, f'efecto_{i + 1}.png'))
    if args.tira:
        la_tira(frames, os.path.join(c, 'efecto_tira.png'))


def cmd_tambor(args):
    destino = args.destino or '/tmp/superforja_tambor.png'
    carpeta = os.path.dirname(destino)
    if carpeta:
        os.makedirs(carpeta, exist_ok=True)
    print('LA SUPERFORJA — EL TAMBOR (el dithering de la paleta contada)')
    el_tambor(args.imagen, destino, args.matriz, args.colores, args.palette)


def cmd_rampa(args):
    destino = args.destino or f'/tmp/superforja_rampa_{args.base.lstrip("#")}.png'
    carpeta = os.path.dirname(destino)
    if carpeta:
        os.makedirs(carpeta, exist_ok=True)
    print('LA SUPERFORJA — LA RAMPA (el hue-shift de las tres casas)')
    la_rampa(args.base, args.pasos, args.hue, destino=destino)


def cmd_atlas(args):
    c = carpeta_de(args.nombre, args.dest)
    if args.frames:
        rutas = []
        for patr in args.frames:
            if os.path.isdir(patr):
                rutas += sorted(os.path.join(patr, f) for f in os.listdir(patr)
                                if f.endswith('.png'))
            else:
                rutas.append(patr)
    else:
        rutas = sorted(os.path.join(c, f) for f in os.listdir(c)
                       if f.startswith('efecto_') and f.endswith('.png') and 'tira' not in f)
        if not rutas:
            rutas = [os.path.join(c, 'sprite.png')]
    if not rutas or not any(os.path.exists(p) for p in rutas):
        raise SystemExit('no hay frames: usa --frames <png/carpeta> o pasa antes por el alma')
    print('LA SUPERFORJA — EL ATLAS (la tira tML + el empaque compacto)')
    el_atlas(rutas, os.path.join(c, 'atlas'), tira=args.tira, maxrects=args.maxrects)


def cmd_ojo(args):
    c = carpeta_de(args.nombre, args.dest)
    montaje = args.montaje or os.path.join(c, 'montaje_qa.png')
    if not os.path.exists(montaje):
        raise SystemExit(f'no hay montaje ({montaje})')
    print('LA SUPERFORJA — EL OJO (el QA VLM de la casa)')
    el_ojo(montaje, os.path.join(c, 'qa_vision.txt'))


def cmd_corta(args):
    """EL CORTE: el cutout universal sobre una imagen real del mod (o de
    cualquier lado) — el fondo se va, el dibujo queda, el borde se
    des-fringea. Con --preview: el montaje antes/después sobre damero."""
    ruta = args.imagen
    if not os.path.exists(ruta):
        raise SystemExit(f'no existe {ruta}')
    im = Image.open(ruta).convert('RGBA')
    print(f'LA SUPERFORJA — EL CORTE (el cutout universal de {os.path.basename(ruta)})')
    out, stats = el_corte(im, tolerancia=args.tolerancia, frames=args.frames,
                          peine=not args.sin_peine, cielo=args.cielo)
    for f, (pct, fondo, cob) in enumerate(stats):
        print(f'  frame {f + 1}/{len(stats)}: fondo '
              f'#{fondo[0]:02X}{fondo[1]:02X}{fondo[2]:02X} '
              f'(cobertura del borde {cob * 100:.0f}%) · quitado {pct:.1f}%')
        if pct > 92:
            print('   (AVISO: se quitó CASI TODO — revisa la tolerancia)')
        if pct < 1.0:
            print('   (AVISO: casi no se quitó nada — sube la tolerancia)')
    destino = args.destino or os.path.splitext(ruta)[0] + '_corta.png'
    out.save(destino)
    print(f'  → {destino}')
    if args.preview:
        # el montaje: original | cortado, sobre damero, ×4
        Z = 4
        piezas = [im.crop((0, f * (im.height // args.frames),
                            im.width, (f + 1) * (im.height // args.frames)))
                  for f in range(args.frames)] if args.frames > 1 else [im]
        cortadas = [out.crop((0, f * (im.height // args.frames),
                              im.width, (f + 1) * (im.height // args.frames)))
                    for f in range(args.frames)] if args.frames > 1 else [out]
        filas = []
        for orig, cort in zip(piezas, cortadas):
            celda_w = max(orig.width, cort.width) * Z + 16
            celda_h = max(orig.height, cort.height) * Z + 16
            fila = Image.new('RGB', (celda_w * 2 + 24, celda_h), (18, 18, 26))
            for k, (fr, etiqueta) in enumerate(((orig, 'ANTES'), (cort, 'DESPUES'))):
                frz = fr.resize((fr.width * Z, fr.height * Z), Image.NEAREST)
                base = _damero(frz.width + 12, frz.height + 12)
                base.paste(frz, (6, 6), frz)
                fila.paste(base, (k * (celda_w + 12) + 6, 6))
            filas.append(fila)
        W = max(f.width for f in filas)
        montaje = Image.new('RGB', (W, sum(f.height for f in filas) + 8), (18, 18, 26))
        y = 4
        for f in filas:
            montaje.paste(f, ((W - f.width) // 2, y))
            y += f.height
        prev = os.path.splitext(destino)[0] + '_preview.png'
        montaje.save(prev)
        print(f'  preview → {prev}')
    return destino


def _correr_cadena(args, con_animacion, con_qa):
    """LA CADENA DE LA SUPERFORJA: pluma → puente → molde → (espejo) →
    alma → (atlas) → montaje → (ojo)."""
    c = carpeta_de(args.nombre, args.dest)
    # 1 — LA PLUMA
    borrador = args.borrador
    if not borrador:
        print('LA SUPERFORJA — LA CADENA · 1/6 LA PLUMA (pxl)')
        borrador = os.path.join(c, 'borrador.png')
        if os.path.exists(borrador) and not args.forzar:
            print(f'  ya existe {borrador} (--forzar para regenerar)')
        else:
            la_pluma(args.prompt, borrador, args.tamano)
    else:
        print('LA SUPERFORJA — LA CADENA · 1/6 LA PLUMA (borrador existente)')
    # 2 — EL PUENTE
    print('LA SUPERFORJA — LA CADENA · 2/6 EL PUENTE (la rejilla nativa)')
    nativo = os.path.join(c, 'nativo.png')
    if args.sin_puente:
        nativo = borrador
        print('  (--sin-puente: el borrador pasa directo)')
    else:
        el_puente(borrador, nativo)
    # 3 — EL MOLDE
    print('LA SUPERFORJA — LA CADENA · 3/6 EL MOLDE (sprite-gen)')
    sprite, hl = el_molde(nativo, args.ancho, args.alto,
                          con_contorno=not args.sin_contorno,
                          con_halo=not args.sin_halo)
    # 3½ — EL CORTE (la regla de la v6.98: TODO lo que sale de la
    # superforja tiene fondo transparente — el croma verde del molde Y
    # cualquier fondo oscuro residual)
    sprite, _stats = el_corte(sprite, tolerancia=40.0)
    sprite.save(os.path.join(c, 'sprite.png'))
    hl.save(os.path.join(c, 'highlight.png'))
    # 4 — EL ESPEJO (si pide animación)
    piezas_extra = []
    if con_animacion:
        print('LA SUPERFORJA — LA CADENA · 4/6 EL ESPEJO (la tira viva)')
        frames = el_espejo(sprite, args.animacion, args.frames)
        la_tira(frames, os.path.join(c, 'tira.png'))
        gif_frames = []
        for f in frames:
            f2 = f.resize((f.width * 6, f.height * 6), Image.NEAREST)
            fondo = Image.new('RGBA', f2.size, (24, 24, 34, 255))
            fondo.paste(f2, (0, 0), f2)
            gif_frames.append(fondo.convert('P', palette=Image.ADAPTIVE))
        gif_frames[0].save(os.path.join(c, 'ciclo.gif'), save_all=True,
                           append_images=gif_frames[1:], duration=140, loop=0)
        # el segundo frame del ciclo al montaje (la diferencia visible)
        if len(frames) > 1:
            piezas_extra.append(('espejo f2', frames[1]))
    # 5 — EL ALMA
    print('LA SUPERFORJA — LA CADENA · 5/6 EL ALMA (pixelfx)')
    frames_e = el_alma(sprite, args.efecto, 3)
    for i, fr in enumerate(frames_e):
        fr.save(os.path.join(c, f'efecto_{i + 1}.png'))
    piezas_extra.append((f'alma {args.efecto} f1', frames_e[0]))
    if con_animacion and args.atlas:
        print('LA SUPERFORJA — LA CADENA · 5½ EL ATLAS (el empaque)')
        rutas = [os.path.join(c, f'efecto_{i + 1}.png') for i in range(3)]
        el_atlas(rutas, os.path.join(c, 'atlas'), tira=True, maxrects=True)
    # 6 — EL MONTAJE (+ EL OJO)
    print('LA SUPERFORJA — LA CADENA · 6/6 EL MONTAJE' + (' + EL OJO' if con_qa else ''))
    montaje = el_montaje(c, sprite, hl, piezas_extra, x=args.x8)
    if con_qa and montaje:
        el_ojo(montaje, os.path.join(c, 'qa_vision.txt'),
               contexto=f'{args.efecto} effect, {args.animacion if con_animacion else "static"} sprite')
    print(f'\nLISTO: {c}/')
    print('  sprite.png · highlight.png · tira.png (si animó) · efecto_*.png · montaje_qa.png' +
          (' · qa_vision.txt' if con_qa else ''))
    return c


def cmd_forjar(args):
    """la cadena corta de la .95 (compatibilidad con LA FORJA): pluma →
    molde → alma → montaje."""
    args.sin_puente = False
    return _correr_cadena(args, con_animacion=False, con_qa=False)


def cmd_cadena(args):
    """la cadena COMPLETA de la superforja."""
    return _correr_cadena(args, con_animacion=(args.animacion != 'ninguna'),
                          con_qa=args.qa)


def main():
    ap = argparse.ArgumentParser(
        description='LA SUPERFORJA: las 3 herramientas de los reels + las 7 nuevas '
                    'de la investigación, todas en una',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__.split('USO:')[1].split('SALIDAS')[0] if 'USO:' in __doc__ else None)
    sub = ap.add_subparsers(dest='cmd', required=True)

    def comunes(p, con_prompt=True, con_sprite=False):
        p.add_argument('--nombre', required=True, help='la carpeta /tmp/superforja_<nombre>')
        if con_prompt:
            p.add_argument('--prompt', help='LA PLUMA: qué pintar (la paleta la pone la forja)')
            p.add_argument('--borrador', help='el borrador ya existente (salta la pluma)')
            p.add_argument('--tamano', default='864x1152',
                           help='el tamaño del borrador IA (default 864x1152)')
            p.add_argument('--forzar', action='store_true', help='regenerar aunque exista')
        if con_sprite:
            p.add_argument('--sprite', help='el sprite ya final (salta el molde)')
        p.add_argument('--dest', help='la carpeta de salida (default /tmp/superforja_<nombre>)')
        return p

    # la pluma + la refina
    p = comunes(sub.add_parser('pluma', help='el borrador IA (pxl)'))
    p.add_argument('--refinar', help='LA REFINA: la edición dirigida tras el borrador')
    p.set_defaults(fn=cmd_pluma)

    p = sub.add_parser('refina', help='la edición dirigida sobre el borrador')
    p.add_argument('--nombre', required=True, help='la carpeta /tmp/superforja_<nombre>')
    p.add_argument('--nota', required=True, help='qué cambiar (el resto queda intacto)')
    p.add_argument('--borrador', help='el origen (default: el borrador de la carpeta)')
    p.add_argument('--tamano', default='864x1152')
    p.add_argument('--dest', help='la carpeta de salida (default /tmp/superforja_<nombre>)')
    p.set_defaults(fn=cmd_refina)

    # el puente
    p = comunes(sub.add_parser('puente', help='la rejilla nativa restaurada'), con_prompt=False)
    p.add_argument('--borrador', help='la imagen de entrada (default: el borrador de la carpeta)')
    p.add_argument('--x2', type=int, default=0, help='el escalón 2^N de la casa para el preview')
    p.set_defaults(fn=cmd_puente)

    # el molde
    p = comunes(sub.add_parser('molde', help='cutout → rejilla → paleta → contorno → halo'), con_prompt=False)
    p.add_argument('--borrador', help='la imagen de entrada (default: nativo/borrador de la carpeta)')
    p.add_argument('--ancho', type=int, default=36)
    p.add_argument('--alto', type=int, default=54)
    p.add_argument('--sin-contorno', action='store_true')
    p.add_argument('--sin-halo', action='store_true')
    p.set_defaults(fn=cmd_molde)

    # el espejo
    p = comunes(sub.add_parser('espejo', help='el sprite estático → la tira animada'),
                con_prompt=False, con_sprite=True)
    p.add_argument('--animacion', default='vida',
                   choices=['vida', 'respirar', 'flotar', 'latir', 'vaiven'])
    p.add_argument('--frames', type=int, default=6)
    p.set_defaults(fn=cmd_espejo)

    # el alma
    p = comunes(sub.add_parser('alma', help='el preview del efecto (pixelfx)'),
                con_prompt=False, con_sprite=True)
    p.add_argument('--efecto', default='brasa', choices=EFECTOS)
    p.add_argument('--frames', type=int, default=3)
    p.add_argument('--tira', action='store_true', help='además la tira vertical de los frames')
    p.set_defaults(fn=cmd_alma)

    # el tambor
    p = sub.add_parser('tambor', help='el dithering Bayer/Floyd a la paleta contada')
    p.add_argument('--imagen', required=True)
    p.add_argument('--matriz', default='bayer4', choices=['bayer4', 'bayer8', 'floyd'])
    p.add_argument('--colores', type=int, default=0, help='cuantizar la imagen a N colores')
    p.add_argument('--palette', default='casa', choices=['casa', 'imagen'])
    p.add_argument('--destino', default='/tmp/superforja_tambor.png')
    p.set_defaults(fn=cmd_tambor)

    # la rampa
    p = sub.add_parser('rampa', help='la rampa hue-shift desde un color base')
    p.add_argument('--base', required=True, help='el color base "#D89030"')
    p.add_argument('--pasos', type=int, default=7)
    p.add_argument('--hue', type=int, default=24, help='la fuerza del giro (grados)')
    p.add_argument('--destino', help='el PNG de muestras (default /tmp)')
    p.set_defaults(fn=cmd_rampa)

    # el atlas
    p = comunes(sub.add_parser('atlas', help='frames → tira tML + atlas compacto + manifiesto'),
                con_prompt=False)
    p.add_argument('--frames', nargs='+', help='pngs o carpetas (default: los efectos de la carpeta)')
    p.add_argument('--tira', dest='tira', action='store_true', default=True,
                   help='la tira vertical tML (default: sí)')
    p.add_argument('--sin-tira', dest='tira', action='store_false')
    p.add_argument('--maxrects', action='store_true', help='además el atlas compacto')
    p.set_defaults(fn=cmd_atlas)

    # el ojo
    p = comunes(sub.add_parser('ojo', help='el QA VLM sobre el montaje'), con_prompt=False)
    p.add_argument('--montaje', help='(default: montaje_qa.png de la carpeta)')
    p.set_defaults(fn=cmd_ojo)

    # el corte (el cutout universal: cualquier fondo → transparente)
    p = sub.add_parser('corta', help='el cutout UNIVERSAL: cualquier fondo → transparente')
    p.add_argument('--imagen', required=True, help='el PNG a cortar')
    p.add_argument('--destino', help='(default: <imagen>_corta.png)')
    p.add_argument('--tolerancia', type=float, default=48.0,
                   help='la distancia de color al fondo (default 48)')
    p.add_argument('--frames', type=int, default=1,
                   help='la tira vertical de N frames (ej: el altar 4)')
    p.add_argument('--preview', action='store_true',
                   help='el montaje antes/después sobre damero')
    p.add_argument('--sin-peine', action='store_true',
                   help='desactivar la barra de islas diminutas oscuras')
    p.add_argument('--cielo', action='store_true',
                   help='fondo estrellado: las islas brillantes rodeadas de nada también se van')
    p.set_defaults(fn=cmd_corta)

    # la cadena corta (la .95) y la completa
    for nombre, ayuda, fn in (
            ('forjar', 'la cadena corta de la .95: pluma → molde → alma → montaje', cmd_forjar),
            ('cadena', 'LA CADENA COMPLETA: pluma → puente → molde → espejo → alma → atlas → montaje → ojo', cmd_cadena)):
        p = comunes(sub.add_parser(nombre, help=ayuda))
        p.add_argument('--ancho', type=int, default=36)
        p.add_argument('--alto', type=int, default=54)
        p.add_argument('--efecto', default='brasa', choices=EFECTOS)
        p.add_argument('--sin-contorno', action='store_true')
        p.add_argument('--sin-halo', action='store_true')
        p.add_argument('--sin-puente', action='store_true', help='saltar el puente')
        p.add_argument('--x8', type=int, default=8)
        if nombre == 'cadena':
            p.add_argument('--animacion', default='ninguna',
                           choices=['ninguna', 'vida', 'respirar', 'flotar', 'latir', 'vaiven'])
            p.add_argument('--frames', type=int, default=6, help='frames de la tira')
            p.add_argument('--atlas', action='store_true', help='además el empaque del atlas')
            p.add_argument('--qa', action='store_true', help='EL OJO: el QA VLM al final')
        p.set_defaults(fn=fn)

    args = ap.parse_args()
    args.fn(args)


if __name__ == '__main__':
    main()
