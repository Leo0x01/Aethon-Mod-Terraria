#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.184 — EL VÁSTAGO DEL ETERNO v3 (la letra: «el Vastago ahora esta
mas feo que antes, intenta mejorar el sprite usando sforja»).

LA AUTOPSIA DE LA v2 (v6.183): el "librito" era un rectángulo duro de
tapas planas con ojos pequeños — quedó rígido y soso. LA v3 VUELVE A
LO ADORABLE (la esencia de una mascota de Terraria):
  · EL CUERPO: librito REGORDETE (esquinas redondeadas de 3 px, tapa
    violeta con LUZ en el borde superior y SOMBRA en el lomo — volumen
    de verdad, no una plancha).
  · LOS OJOS: GRANDES y BRILLANTES (6 px de alto con doble reflejo —
    el brillo grande arriba-izquierda + el chiquito abajo-derecha) y
    SIEMPRE en el mismo sitio (el parpadeo solo en el frame 3).
  · EL RUBOR: dos motitas rosas bajo los ojos (la ternura).
  · LA SONRISA: una curva de 3 px bajo los ojos (la v2 no sonreía).
  · LAS ALITAS: DOS PLUMAS DE PÁGINA cosidas al lomo (nacen DE DENTRO,
    simétricas) — dos poses de aleteo.
  · EL SEPARADOR: la cinta ORO colgando del pie del lomo con su puntita
    triangular.
  · EL CICLO: idle 0-3 (respiración de 1 px + parpadeo en el 3) ·
    vuelo 4-7 (cabeceo ±1 fila + aleteo de dos poses).
SALIDA: VastagoDelEternoPet.png (40×384 — 8 frames de 40×48) + hoja de
contacto para el QA VLM.
"""
from PIL import Image, ImageDraw

W, H = 40, 48
N = 8

# ── LA PALETA (la casa — cero degradés, píxel duro) ───────────────────
OUT = (24, 14, 36, 255)          # el contorno
OUT_SUAVE = (44, 26, 62, 255)    # el contorno interior (el volumen)
TAPA = (138, 54, 150, 255)       # la tapa violeta
TAPA_CLARO = (182, 92, 190, 255) # la luz de la tapa (borde superior)
TAPA_OSC = (104, 34, 114, 255)   # la sombra del lomo (borde inferior)
PAGINA = (244, 238, 224, 255)    # el filo de páginas (los laterales)
PAGINA_SOMBRA = (222, 208, 190, 255)
ORO = (255, 202, 96, 255)        # la cinta separadora
ORO_OSC = (198, 142, 62, 255)
BLANCO = (255, 252, 244, 255)    # el ojo
NEGRO = (16, 8, 24, 255)         # la pupila
RUBOR = (238, 128, 156, 255)     # el rubor
ALA = (246, 244, 252, 255)        # la pluma de página
ALA_SOMBRA = (204, 200, 228, 255)


def px(d, x, y, c):
    d.point((x, y), fill=c)


def cuerpo(d, x0, y0, bob=0, sonrisa=True):
    """EL LIBRITO REGORDETE: 26×20, esquinas de 3 px, con volumen.
    (x0,y0) = esquina superior izquierda del bloque del libro."""
    y0 += bob
    w, h = 26, 20
    # LA TAPA (el bloque redondeado)
    for yy in range(h):
        for xx in range(w):
            # esquinas redondeadas de 3 px
            esq = 3
            dx = min(xx, w - 1 - xx)
            dy = min(yy, h - 1 - yy)
            if dx + dy < esq:
                continue
            c = TAPA
            if dy == 0 or dx == w - 1:
                c = TAPA_CLARO      # la luz del borde superior/lateral
            elif yy >= h - 3:
                c = TAPA_OSC        # la sombra del pie
            px(d, x0 + xx, y0 + yy, c)
    # EL CONTORNO (la silueta de la casa — 1 px alrededor)
    for yy in range(-1, h + 1):
        for xx in range(-1, w + 1):
            dentro = 0 <= xx < w and 0 <= yy < h
            if dentro:
                dx = min(xx, w - 1 - xx); dy = min(yy, h - 1 - yy)
                if dx + dy < 3:
                    dentro = False
            if not dentro:
                # solo el anillo (no rellenar fuera)
                vecino = False
                for ox, oy in ((0, 1), (0, -1), (1, 0), (-1, 0)):
                    nx, ny = xx + ox, yy + oy
                    if 0 <= nx < w and 0 <= ny < h:
                        ndx = min(nx, w - 1 - nx); ndy = min(ny, h - 1 - ny)
                        if ndx + ndy >= 3:
                            vecino = True
                if vecino:
                    px(d, x0 + xx, y0 + yy, OUT)
    # EL FILO DE PÁGINAS (el canto del libro — la derecha del lomo):
    # v6.184 r2 (el QA VLM): LA LÍNEA DE LUZ — el primer px de cada
    # segmento lleva BLANCO (el canto BRILLA — se lee libro a leguas)
    for yy in range(3, h - 3):
        px(d, x0 + w, y0 + yy, BLANCO if yy % 4 == 0 else PAGINA)
        if yy % 4 == 2:
            px(d, x0 + w, y0 + yy, PAGINA_SOMBRA)
    px(d, x0 + w, y0 + 3, OUT_SUAVE)
    px(d, x0 + w, y0 + h - 4, OUT_SUAVE)
    return x0, y0, w, h


def ojos(d, x0, y0, bob=0, abierto=True):
    """LOS OJOS GRANDES Y BRILLANTES — siempre en el mismo sitio."""
    y = y0 + 6 + bob
    # ojo izquierdo (5 ancho) y derecho (5 ancho), separados por 2 px
    for ex in (x0 + 5, x0 + 15):
        # LA ALMENDRA BLANCA (5×6, esquinas suaves)
        for oy in range(6):
            for ox in range(5):
                if (ox == 0 or ox == 4) and (oy == 0 or oy == 5):
                    continue
                c = BLANCO
                if oy == 0:
                    c = (255, 255, 255, 255)
                px(d, ex + ox, y + oy, c)
        if abierto:
            # LA PUPILA (2×3 negra, centrada-baja — la mirada viva)
            for oy in range(3):
                px(d, ex + 1, y + 2 + oy, NEGRO)
                px(d, ex + 2, y + 2 + oy, NEGRO)
            # EL BRILLO GRANDE (arriba-izquierda de la pupila)
            px(d, ex + 1, y + 2, (255, 255, 255, 255))
            # EL BRILLO CHICO (abajo-derecha)
            px(d, ex + 3, y + 5, (230, 226, 240, 255))
        else:
            # CERRADO — UNA LÍNEA SUAVE de 3 px centrada (el blink
            # discreto de la casa: nada de "^^")
            for ox in (1, 2, 3):
                px(d, ex + ox, y + 3, NEGRO)
            px(d, ex + 2, y + 2, (255, 255, 255, 255))  # la pestañita de luz
    # EL RUBOR (dos motitas bajo los ojos — la ternura)
    if abierto:
        px(d, x0 + 4, y + 7, RUBOR); px(d, x0 + 5, y + 7, RUBOR)
        px(d, x0 + 19, y + 7, RUBOR); px(d, x0 + 20, y + 7, RUBOR)
        # LA SONRISITA (tres px en curva)
        px(d, x0 + 11, y + 7, OUT_SUAVE)
        px(d, x0 + 12, y + 8, OUT_SUAVE)
        px(d, x0 + 13, y + 7, OUT_SUAVE)


def alitas(d, cx, y0, bob=0, pose=0):
    """LAS DOS PLUMAS DE PÁGINA — cosidas al lomo, simétricas.
    pose 0: abajo (carga) · pose 1: arriba (aleteo).
    v6.184 r2 (el QA VLM): en vuelo la pluma NACE DE DENTRO del cuerpo
    (el primer px del ala COMPARTE fila con el lomo — jamás 1-2 px de
    hueco: la pluma sale DE la axila del libro)."""
    y = y0 + bob
    for lado in (-1, 1):
        xbase = cx + lado * 15
        if pose == 0:
            forma = [(0, 0), (1, -1), (2, -2), (1, -3), (0, -4), (0, -5), (1, -6)]
        else:
            # ARRIBA pero ANCLADA: la base toca el cuerpo (fila +9 → +8)
            forma = [(0, 1), (1, 0), (2, -2), (1, -4), (0, -5), (0, -6), (1, -7)]
        for (fx, fy) in forma:
            xx = xbase + (fx if lado == -1 else -fx)
            yy = y + 9 + fy
            px(d, xx, yy, ALA)
            px(d, xx, yy + 1, ALA_SOMBRA)
            px(d, xx - lado, yy, ALA)


def separador(d, x0, y0, bob=0):
    """LA CINTA ORO colgando del pie del lomo — BIEN VISIBLE (el QA:
    se perdia tras el cuerpo): 2 px de ancho, nace DEL BORDE inferior y
    cuelga 5 px por FUERA del libro con su punta triangular."""
    y = y0 + bob
    x = x0 + 21
    for k in range(5):
        px(d, x, y + 19 + k, ORO)
        px(d, x + 1, y + 19 + k, ORO)
    px(d, x, y + 24, ORO_OSC)
    px(d, x + 1, y + 24, ORO_OSC)
    px(d, x, y + 25, ORO_OSC)
    px(d, x + 1, y + 25, ORO_OSC)


def frame(indice):
    """Un frame del atlas."""
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    vuelo = indice >= 4
    fase = indice % 4
    # LA RESPIRACIÓN (idle: 1 px) · EL VUELO (bob ±2 — más vivo que el idle)
    bob = 0
    if vuelo:
        bob = -2 if fase in (1, 2) else 2
    else:
        bob = -1 if fase in (1, 2) else 0
    # EL ANCLAJE: el libro centrado, algo abajo
    x0 = 7
    y0 = 16
    # LAS ALITAS DETRÁS (vuelo: aleteo de dos poses)
    alitas(d, x0 + 13, y0, bob, pose=(1 if fase in (1, 2) else 0) if vuelo else 0)
    # EL CUERPO ENCIMA
    bx, by, w, h = cuerpo(d, x0, y0, bob)
    # LOS OJOS (parpadeo SOLO en la fase 3 del idle)
    abierto = not (not vuelo and fase == 3)
    ojos(d, x0, y0, bob, abierto)
    # EL SEPARADOR
    separador(d, x0, y0, bob)
    # EL VAIVÉN DEL VUELO: el cabeceo es el BOB (±1 fila — el cizallado
    # AFFINE mataba la simetría de los ojos: MUERE)
    return im


def main():
    atlas = Image.new('RGBA', (W, H * N), (0, 0, 0, 0))
    for i in range(N):
        f = frame(i)
        atlas.paste(f, (0, i * H))
    atlas.save('AethonMod/Content/Projectiles/Cosmetic/VastagoDelEternoPet.png')
    print('atlas 40×384 OK — 8 frames')

    # LA HOJA DE CONTACTO para el QA VLM (idle y vuelo, sobre fondo día/noche)
    hoja = Image.new('RGBA', (W * 4 + 50, H * 2 + 40), (0, 0, 0, 255))
    dia = (110, 166, 220, 255)
    noche = (24, 18, 42, 255)
    for fila, (fondo, ini) in enumerate(((dia, 0), (noche, 4))):
        for k in range(4):
            celda = Image.new('RGBA', (W + 10, H + 10), fondo)
            celda.paste(atlas.crop((0, (ini + k) * H, W, (ini + k + 1) * H)), (5, 5),
                        atlas.crop((0, (ini + k) * H, W, (ini + k + 1) * H)))
            hoja.paste(celda, (10 + k * (W + 10), 10 + fila * (H + 20)))
    hoja = hoja.resize((hoja.width * 3, hoja.height * 3), Image.NEAREST)
    hoja.save('/home/z/my-project/scripts/tmp/vastago_v6184_hoja.png')
    print('hoja de contacto OK:', hoja.size)


if __name__ == '__main__':
    main()
