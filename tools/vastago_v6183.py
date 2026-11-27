#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.183 — LA MASCOTA LIMPIA: EL VÁSTAGO DEL ETERNO v2 (la letra:
«Vástago del Eterno es una mascota bonita, pero su sprite esta un
poco incorrecto, intenta mejorarlo»).

LA AUTOPSIA (VLM + píxel a píxel): la v6.91 nació de UNA IMAGEN
trazada («una sola imagen → transparencia real por flood-fill») — y
el trazado dejó TODO el rastro: decenas de morados casi iguales
(antialiasing), las ALAS flotando desconectadas con píxeles sueltos,
manchas de brillo aleatorias, el contorno perdido entre ala y cuerpo,
y los ojos/rubor movidos entre frames sin motivo.

LA V2 — PIXEL ART DE VERDAD (100% por código, como toda la casa):
  · CUERPO: el librito — tapa violeta (135,45,145) con su lomo
    oscuro, el FILO DE PÁGINAS claro ARRIBA (la lectura del libro
    cerrado), el separador ORO colgando del lomo.
  · LOS OJOS: dos almendras grandes con pupila, brillo y su rubor —
    SIEMPRE en el mismo sitio (el parpadeo SOLO en el frame 3).
  · LAS ALAS: pequeñas, claras, SIMÉTRICAS y COSIDAS al cuerpo
    (nacen DEL lomo — nunca flotando).
  · EL CICLO: idle 0-3 (la respiración 1 px + el parpadeo) ·
    vuelo 4-7 (el bob con inclinación y el aleteo de DOS poses).
  · CERO antialiasing: la paleta EXACTA de la casa + contorno
    (26,15,38) en cada silueta.
SALIDA: VastagoDelEternoPet.png (40×384 — 8 frames de 40×48) + el
reporte ASCII por frame.
"""
import hashlib

from PIL import Image

W, H = 40, 48          # el canvas de cada frame
N = 8                 # 4 idle + 4 vuelo

# ── LA PALETA (exacta — cero degradés) ──────────────────────────────
OUT = (26, 15, 38, 255)        # el contorno de la casa
TAPA = (135, 45, 145, 255)     # la tapa violeta del librito
TAPA_OSC = (104, 30, 112, 255) # la sombra del lomo
TAPA_CLARO = (168, 74, 175, 255)  # la luz de la tapa
PAGINA = (240, 232, 214, 255)  # el filo de páginas
PAGINA_SOMBRA = (221, 205, 189, 255)
ORO = (255, 196, 90, 255)      # el separador dorado
ORO_OSC = (196, 138, 62, 255)
BLANCO = (255, 248, 235, 255)  # los ojos
NEGRO = (10, 7, 17, 255)       # las pupilas
RUBOR = (232, 116, 150, 255)   # el rubor
ALA = (238, 236, 250, 255)     # el ala clara
ALA_SOMBRA = (196, 194, 222, 255)

CUERPO_X0, CUERPO_X1 = 13, 27   # 15 de ancho
CUERPO_Y0, CUERPO_Y1 = 17, 37   # 21 de alto


def nuevo():
    return Image.new('RGBA', (W, H), (0, 0, 0, 0))


def px(im, x, y, c):
    if 0 <= x < W and 0 <= y < H:
        im.putpixel((x, y), c)


def rect(im, x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            px(im, x, y, c)


def outline_alrededor(im, cond):
    """Contorno: todo píxel VACÍO que toque uno sólido se vuelve OUT."""
    solidos = {(x, y) for y in range(H) for x in range(W) if cond(x, y)}
    for (x, y) in list(solidos):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1),
                       (1, 1), (1, -1), (-1, 1), (-1, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < W and 0 <= ny < H and (nx, ny) not in solidos:
                px(im, nx, ny, OUT)


def dibujar_cuerpo(im, bob, inclinacion):
    """El librito: tapa + filo de páginas ARRIBA + separador oro.
    bob: -1..1 px vertical · inclinacion: -1..1 px horizontal del lomo."""
    x0 = CUERPO_X0 + inclinacion
    x1 = CUERPO_X1 + inclinacion
    y0 = CUERPO_Y0 + bob
    y1 = CUERPO_Y1 + bob

    # EL FILO DE PÁGINAS (arriba — el libro cerrado se lee por arriba)
    rect(im, x0 + 1, y0, x1 - 1, y0 + 1, PAGINA)
    rect(im, x0 + 2, y0 + 1, x1 - 2, y0 + 1, PAGINA_SOMBRA)
    # LA TAPA (el cuerpo violeta)
    rect(im, x0, y0 + 2, x1, y1, TAPA)
    # el lomo oscuro (la izquierda del libro) y la luz (la derecha)
    rect(im, x0, y0 + 2, x0, y1, TAPA_OSC)
    rect(im, x1, y0 + 2, x1, y1, TAPA_CLARO)
    # LA COSTURA DEL CUERPO: la esquina de páginas se redondea
    px(im, x0, y0 + 1, TAPA)
    px(im, x1, y0 + 1, TAPA)
    px(im, x0 + 1, y0, PAGINA_SOMBRA)
    px(im, x1 - 1, y0, PAGINA_SOMBRA)
    # EL SEPARADOR ORO (la cinta que cuelga del lomo, abajo)
    sx = x0 + 3
    rect(im, sx, y1 - 5, sx, y1 - 1, ORO)
    px(im, sx, y1 - 5, ORO_OSC)
    px(im, sx, y1 - 1, ORO_OSC)
    return x0, x1, y0, y1


def dibujar_ojos(im, x0, x1, y0, cerrado):
    """Las dos almendras grandes — SIEMPRE en el mismo sitio, BIEN
    separadas (la lección de la autopsia: se tocaban)."""
    ey = y0 + 8
    for ex in (x0 + 2, x1 - 6):      # 5 de ancho CADA UNA, hueco entre ambas
        if cerrado:
            rect(im, ex, ey + 3, ex + 4, ey + 3, NEGRO)   # la línea del sueño
            continue
        rect(im, ex, ey, ex + 4, ey + 4, BLANCO)
        px(im, ex, ey, TAPA)             # la almendra: esquinas fundidas
        px(im, ex + 4, ey, TAPA)
        px(im, ex, ey + 4, TAPA)
        px(im, ex + 4, ey + 4, TAPA)
        # LA PUPILA 2×2 al centro (mira AL FRENTE — siempre igual)
        rect(im, ex + 1, ey + 2, ex + 2, ey + 3, NEGRO)
        # EL BRILLO (la chispa de la casa, arriba-derecha)
        px(im, ex + 3, ey + 1, (255, 255, 255, 255))
    # EL RUBOR (separado de los ojos — la lección de la autopsia)
    if not cerrado:
        for ex in (x0 + 1, x1 - 2):
            px(im, ex, ey + 6, RUBOR)
            px(im, ex + (1 if ex == x0 + 1 else -1), ey + 6, RUBOR)


def dibujar_alas(im, x0, x1, y0, pose):
    """Las alas claras: UNA CRECIENTE de dos plumas de grosor, cosida
    al lomo — dos poses (alzada/bajada) para el aleteo de dos tiempos.
    pose: 0 alzada · 1 bajada."""
    wy = y0 + 4 + pose * 6
    for lado in (-1, 1):
        bx = x0 if lado < 0 else x1
        for p in range(4):          # la creciente: hacia afuera y arriba
            for q in range(2):      # dos plumas de grosor
                x = bx + lado * (p + 1)
                y = wy - p + q
                c = ALA if (p + q) < 4 else ALA_SOMBRA
                px(im, x, y, c)
        # LA COSTURA (el ala NACE del lomo — jamás flotando)
        px(im, bx, wy, TAPA_CLARO)
        px(im, bx, wy + 1, TAPA)


def frame_idle(n):
    """0..3: la respiración (bob 0/-1/0/+1) + el parpadeo SOLO en el 3."""
    im = nuevo()
    bob = (0, -1, 0, 1)[n]
    cerrado = n == 3
    x0, x1, y0, y1 = dibujar_cuerpo(im, bob, 0)
    dibujar_ojos(im, x0, x1, y0, cerrado)
    dibujar_alas(im, x0, x1, y0, n % 2)      # el suave vaivén de dos tiempos
    outline_alrededor(im, lambda x, y: im.getpixel((x, y))[3] > 0)
    return im


def frame_vuelo(n):
    """4..7: el bob con INCLINACIÓN (la zancada del aire) + aleteo fuerte."""
    im = nuevo()
    fase = n - 4
    bob = (0, -2, 0, 2)[fase]
    incl = (-1, -1, 1, 1)[fase]
    x0, x1, y0, y1 = dibujar_cuerpo(im, bob, incl)
    dibujar_ojos(im, x0, x1, y0, False)
    dibujar_alas(im, x0, x1, y0, fase % 2)
    # (v6.183 — LA ESTELA MURIÓ: el VLM la leía como píxeles sueltos;
    # el aleteo de dos poses ES la animación, sin polvillo)
    outline_alrededor(im, lambda x, y: im.getpixel((x, y))[3] > 0)
    return im


# ── EL ATLAS ────────────────────────────────────────────────────────
atlas = Image.new('RGBA', (W, H * N), (0, 0, 0, 0))
for n in range(N):
    f = frame_idle(n) if n < 4 else frame_vuelo(n)
    atlas.paste(f, (0, n * H))
atlas.save('/home/z/my-project/mod/AethonMod/Content/Projectiles/Cosmetic/VastagoDelEternoPet.png')
with open('/home/z/my-project/mod/AethonMod/Content/Projectiles/Cosmetic/VastagoDelEternoPet.png', 'rb') as fh:
    print('VastagoDelEternoPet.png 40×384 · md5', hashlib.md5(fh.read()).hexdigest())

# ── EL REPORTE ASCII (frame 0 y frame 4 — la verificación de la casa) ──
for n in (0, 4):
    print(f'  frame {n} ({"idle" if n < 4 else "vuelo"}):')
    f = frame_idle(n) if n < 4 else frame_vuelo(n)
    for y in range(H):
        fila = ''
        for x in range(W):
            r, g, b, a = f.getpixel((x, y))
            if a < 40:
                fila += '.'
            elif (r, g, b) == (26, 15, 38):
                fila += '#'
            elif (r, g, b) in ((255, 248, 235), (255, 255, 255)):
                fila += 'O'    # ojo/blanco
            elif (r, g, b) == (10, 7, 17):
                fila += '@'    # pupila
            elif (r, g, b) == (232, 116, 150):
                fila += 'r'    # rubor
            elif (r, g, b) in ((240, 232, 214), (221, 205, 189)):
                fila += '='    # páginas
            elif (r, g, b) in ((255, 196, 90), (196, 138, 62)):
                fila += '$'    # el separador oro
            elif (r, g, b) in ((238, 236, 250), (196, 194, 222)):
                fila += 'w'    # alas
            elif (r, g, b) == (135, 45, 145):
                fila += 'B'    # tapa
            elif (r, g, b) in ((104, 30, 112), (168, 74, 175)):
                fila += 'b'    # sombra/luz tapa
            else:
                fila += '?'
        print('  ' + fila)
