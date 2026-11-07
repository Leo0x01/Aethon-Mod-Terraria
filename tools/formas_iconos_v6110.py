#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
formas_iconos_v6110.py — v6.110 — LA CURA DE LOS CUADROS PLANOS.

LA LETRA DEL USUARIO: «la forma ascendida 5, 6, 7 tiene texturas que son
cuadrados planos, debes arreglar eso y ademas crear la forma ascendida
8, 9 y 10».

LA AUTOPSIA (por qué se veían PLANOS): los iconos de la v6.102/.109
nacieron con trazos finos y MUCHA semitransparencia (la 5: 12% de
píxeles opacos; la 6: 21% opaco + 59% SEMI — un FANTASMA cuadrado; la
7: 20% opaco). A la escala del inventario (30×30 reales) el ojo lee un
CUADRO tenue de color, no una forma. LOS ICONOS DE LAS FORMAS 1-4 (los
que el usuario SÍ aprueba): 34-38% opaco, trazos GORDOS, silueta clara.

LA RECETA DE LA CASA v6.110 (el estándar de la familia):
  · TRAZOS GORDOS (3-4 px) y NÚCLEOS GRANDES (r ≥ 5) — lo que a 30×30
    SE LEE sin lupa.
  · OPACIDAD DE VERDAD: alpha 255 en el cuerpo, UN solo nivel de
    anti-alias (alpha ~150) en el borde — nada de fantasmas.
  · SILUETA INMISTAKABLE: cada luz con SU símbolo (el vórtice, el
    hexagrama, la estrella, LA ESPADA, las ondas, EL ÁRBOL).

LOS SEIS SELLOS:
  5 — EL VIENTO DEL ESPÍRITU: núcleo de plata + abrazos espirales
      GORDOS teal→violeta + ráfagas cortas.
  6 — EL ESCRIBA DE LA LUZ: HEXAGRAMA de oro macizo con sus seis nodos
      + el anillo de tinta violeta + corazón de pergamino.
  7 — LA ESTRELLA VIVA: núcleo blanco + OCHO rayos gordos oro/coral +
      dos prominencias.
  8 — LA ESPADA DEL JUICIO (NUEVA, San Miguel): la espada vertical
      platino con guarda de oro, pomo violeta y su halo del veredicto.
  9 — LA MÚSICA DE LAS ESFERAS (NUEVA, Gabriel): el núcleo marfil y
      TRES anillos de sonido con sus notas orbitando.
  10 — EL ÁRBOL DE LA VIDA (NUEVA, La Corona): los DIEZ sefirot unidos
       por sus senderos con Keter — la corona — arriba.

REGLA PERMANENTE: transparencia NATIVA (el lienzo nace alfa=0).
"""
import math
import os

from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
BASE = os.path.join(AQUI, '..', 'AethonMod')
DEST = os.path.join(BASE, 'Content', 'Items', 'Cosmetics')

N = 30  # el lienzo de la familia


# ---------------------------------------------------------------- utilidades
def nuevo():
    return Image.new('RGBA', (N, N), (0, 0, 0, 0))


def px(im, x, y, c, a=255):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < N and 0 <= y < N:
        im.putpixel((x, y), (c[0], c[1], c[2], a))


def disco(im, cx, cy, r, c, corazon=None, r_corazon=None):
    """UN NÚCLEO GORDO Y OPACO — el estándar v6.110: cuerpo macizo,
    UN anillo de anti-alias en el borde."""
    for y in range(N):
        for x in range(N):
            d = math.hypot(x - cx, y - cy)
            if d < r - 0.7:
                px(im, x, y, corazon if (corazon and d < r_corazon) else c)
            elif d < r + 0.45:
                px(im, x, y, corazon if (corazon and d < r_corazon) else c, 150)


def linea_gorda(im, x0, y0, x1, y1, c, grosor=3, alma=None):
    """EL TRAZO GORDO — línea de Bresenham con PINCEL cuadrado y su
    ALMA (la vena central más clara, el 'edge highlight' de Terraria)."""
    pasos = int(max(abs(x1 - x0), abs(y1 - y0)) * 2.5) + 1
    for s in range(pasos):
        t = s / (pasos - 1)
        x = x0 + (x1 - x0) * t
        y = y0 + (y1 - y0) * t
        half = (grosor - 1) / 2
        for dy in range(-int(half) - 1, int(half) + 2):
            for dx in range(-int(half) - 1, int(half) + 2):
                d = math.hypot(dx, dy)
                if d <= half:
                    px(im, x + dx, y + dy, alma if (alma and d < half - 0.8) else c)
                elif d <= half + 0.75:
                    px(im, x + dx, y + dy, c, 150)


def arco(im, cx, cy, r, a0, a1, c, grosor=3, alma=None, squish=1.0):
    """EL ARCO GORDO — el trazo curvo de los sellos (colas vivas)."""
    pasos = int(abs(a1 - a0) * r * 1.6) + 2
    for s in range(pasos):
        t = s / (pasos - 1)
        th = a0 + (a1 - a0) * t
        x = cx + math.cos(th) * r
        y = cy + math.sin(th) * r * squish
        half = (grosor - 1) / 2
        for dy in range(-int(half) - 1, int(half) + 2):
            for dx in range(-int(half) - 1, int(half) + 2):
                d = math.hypot(dx, dy)
                if d <= half:
                    px(im, x + dx, y + dy, alma if (alma and d < half - 0.8) else c)
                elif d <= half + 0.75:
                    px(im, x + dx, y + dy, c, 150)


def chispa4(im, cx, cy, largo, c, alma):
    """LA CRUZ DE 4 PUNTAS — el destello de la casa (dos barras)."""
    linea_gorda(im, cx - largo, cy, cx + largo, cy, c, 2, alma)
    linea_gorda(im, cx, cy - largo, cx, cy + largo, c, 2, alma)
    disco(im, cx, cy, 2.4, alma)


# ---------------------------------------------------------------- paletas
# 5 — EL VIENTO: plata → teal → violeta
PLATA = (232, 240, 252)
PLATA_ALMA = (250, 252, 255)
TEAL = (66, 214, 176)
TEAL_CLARO = (150, 240, 210)
VIOLETA = (150, 84, 235)

# 6 — EL ESCRIBA: pergamino → oro → tinta violeta
PERGAMINO = (255, 250, 238)
ORO = (255, 208, 96)
ORO_HONDO = (212, 152, 44)
TINTA = (148, 88, 226)

# 7 — LA ESTRELLA: nuclear → oro vivo → coral
NUCLEAR = (255, 255, 252)
ORO_VIVO = (255, 216, 110)
CORAL = (255, 128, 96)
CORAL_HONDO = (232, 92, 70)

# 8 — LA ESPADA: platino acero → oro → violeta real
PLATINO = (226, 236, 250)
PLATINO_ALMA = (248, 250, 255)
ORO_VEREDICTO = (255, 198, 78)
VIOLETA_REAL = (168, 96, 255)
VIOLETA_HONDO = (118, 58, 208)

# 9 — LA MÚSICA: marfil → rosa alba → violeta
MARFIL = (255, 252, 240)
ROSA = (255, 178, 204)
ROSA_CLARO = (255, 214, 228)
VIOLETA_MELODIA = (176, 120, 244)

# 10 — EL ÁRBOL: blanco corona → oro corona → violeta hondo
BLANCO_CORONA = (255, 255, 252)
ORO_CORONA = (255, 228, 128)
ORO_HONDO_CORONA = (222, 168, 52)
VIOLETA_ARBOL = (128, 74, 226)


# ================================================================ EL SELLO 5
def sello5(im):
    """EL VIENTO — núcleo GRANDE de plata y TRES abrazos espirales
    GORDOS (la firma: el anillo que NO cierra), teal→violeta. r2 (QA
    VLM: «hojas dispersas = confetti» — MÁS CUERPO: abrazos
    GORDÍSIMOS de 5 px que ENVUELVEN, motas ligadas al anillo)."""
    cx, cy = 15.0, 15.0
    # LOS ABRAZOS: barridos LARGOS que envuelven — GORDOS de 5 px.
    abrazos = [
        (11.8, math.radians(178), math.radians(-52), TEAL, TEAL_CLARO),
        (10.2, math.radians(52), math.radians(-86), VIOLETA, ROSA_CLARO),
        (12.8, math.radians(196), math.radians(282), TEAL_CLARO, TEAL),
    ]
    for radio, a0, a1, c, alma in abrazos:
        arco(im, cx, cy, radio, a0, a1, c, 6, alma, squish=0.96)
    # EL NÚCLEO: plata maciza r=7.6 con corazón blanco r=3.6.
    disco(im, cx, cy, 7.6, PLATA, PLATA_ALMA, 3.6)
    # RÁFAGAS CORTAS Y GORDAS (4): nacen DONDE MUEREN los abrazos
    # (ligadas al anillo — nada de confetti suelto).
    for th_deg, r0, r1, c in ((150, 14.0, 19.0, TEAL),
                              (-16, 14.0, 19.5, VIOLETA),
                              (106, 13.5, 17.5, TEAL_CLARO),
                              (-46, 14.0, 18.5, TEAL)):
        th = math.radians(th_deg)
        linea_gorda(im, cx + math.cos(th) * r0, cy + math.sin(th) * r0 * 0.96,
                    cx + math.cos(th) * r1, cy + math.sin(th) * r1 * 0.96, c, 4, None)
    # LAS DOS MOTAS DEL ALIENTO (SOLO dos, gordas, en el eje del abrazo).
    disco(im, 3, 10, 1.8, TEAL)
    disco(im, 27, 21, 1.8, VIOLETA)


# ================================================================ EL SELLO 6
def sello6(im):
    """EL ESCRIBA — EL HEXAGRAMA de oro macizo (dos triángulos GORDOS
    con alma blanca), sus SEIS nodos, el anillo de tinta y el corazón."""
    cx, cy = 15.0, 15.0
    r = 10.2
    # LOS DOS TRIÁNGULOS (el sello de Salomón): puntas arriba/abajo.
    for k in range(2):
        pts = []
        for i in range(3):
            th = -math.pi / 2 + (i * 2 + k) * math.pi / 3
            pts.append((cx + math.cos(th) * r, cy + math.sin(th) * r * 0.98))
        for i in range(3):
            x0, y0 = pts[i]
            x1, y1 = pts[(i + 1) % 3]
            linea_gorda(im, x0, y0, x1, y1, ORO, 3, PERGAMINO)
    # EL ANILLO DE TINTA (r 13.2, GORDO de 4 px, con alma clara).
    arco(im, cx, cy, 13.3, 0, 2 * math.pi, TINTA, 4, (216, 178, 255), squish=1.0)
    # LOS SEIS NODOS (las puntas de los triángulos — discos de oro).
    for i in range(6):
        th = -math.pi / 2 + i * math.pi / 3
        disco(im, cx + math.cos(th) * r, cy + math.sin(th) * r * 0.98, 2.4,
              ORO_VEREDICTO, PERGAMINO, 1.1)
    # EL CORAZÓN DEL ESCRIBA (r3 QA: el foco APAGADO — GRANDE y
    # radiante como los blancos de la familia: pergamino r=4.6 con
    # corazón blanco caliente r=2.4 y su gota de tinta).
    disco(im, cx, cy, 4.6, PERGAMINO, BLANCO_CORONA, 2.4)
    px(im, 15, 13, TINTA); px(im, 16, 13, TINTA)
    px(im, 15, 14, TINTA)
    # LAS DOS GOTAS de tinta orbitando el anillo.
    disco(im, cx + 13.3, cy, 1.8, VIOLETA_MELODIA)
    disco(im, cx - 13.3, cy, 1.8, VIOLETA_MELODIA)


# ================================================================ EL SELLO 7
def sello7(im):
    """LA ESTRELLA — núcleo blanco GRANDE, OCHO rayos GORDOS
    alternando largo/corto (oro vivo / coral) y DOS prominencias
    ANCLADAS a las puntas (r2 QA: nada de píxeles huérfanos)."""
    cx, cy = 15.0, 15.0
    # LOS OCHO RAYOS: 4 largos (oro vivo) + 4 cortes ROMBO (coral —
    # r3 QA: «púas finas» → ROMBOS MACIZOS de 7 px de ancho, cortos).
    for i in range(8):
        th = i * math.pi / 4 - math.pi / 2
        if i % 2 == 0:
            largo = 13.8
            linea_gorda(im, cx + math.cos(th) * 4.4, cy + math.sin(th) * 4.4,
                        cx + math.cos(th) * largo, cy + math.sin(th) * largo,
                        ORO_VIVO, 5, NUCLEAR)
        else:
            # EL ROMBO CORTO: una púa CORAL GORDA con su disco en la
            # punta (la prominencia hecha masa, no una línea fina).
            largo = 10.2
            linea_gorda(im, cx + math.cos(th) * 4.4, cy + math.sin(th) * 4.4,
                        cx + math.cos(th) * largo, cy + math.sin(th) * largo,
                        CORAL, 7, CORAL_HONDO)
            disco(im, cx + math.cos(th) * (largo - 0.6),
                  cy + math.sin(th) * (largo - 0.6), 2.6, CORAL, CORAL_HONDO, 1.2)
    # EL NÚCLEO: blanco nuclear r=6.8 con el corazón cegador r=3.4.
    disco(im, cx, cy, 6.8, ORO_VIVO, NUCLEAR, 3.4)
    # LAS PROMINENCIAS: dos arcos de coral GORDOS anclados ENTRE rayos
    # (los pies sobre los brazos de la estrella — nada suelto).
    arco(im, cx, cy, 9.6, math.radians(22), math.radians(68), CORAL, 3,
         CORAL_HONDO, squish=0.94)
    arco(im, cx, cy, 9.6, math.radians(202), math.radians(248), CORAL, 3,
         CORAL_HONDO, squish=0.94)


# ================================================================ EL SELLO 8
def sello8(im):
    """LA ESPADA DEL JUICIO — la espada vertical: hoja de platino con
    filo blanco, guarda de oro con gemas violeta, pomo violeta y EL
    SOL DEL VEREDICTO detrás (r3 QA: fuera el ruido — el sol GORDO
    llena el vacío y da el foco; nada de destellos sueltos)."""
    # EL SOL DEL VEREDICTO (el disco de oro DETRÁS de la hoja — el
    # foco radiante que llena el halo: r=8.6 con corazón blanco).
    disco(im, 15, 14, 8.6, ORO_VEREDICTO, BLANCO_CORONA, 3.6)
    # EL HALO DEL VEREDICTO (el aro violeta GORDO que enmarca).
    arco(im, 15, 14, 12.9, 0, 2 * math.pi, VIOLETA_HONDO, 4,
         VIOLETA_REAL, squish=1.0)
    # LA HOJA (x 12-18, y 2-19): platino con alma blanca — GORDA.
    for y in range(2, 20):
        for x in range(12, 19):
            px(im, x, y, PLATINO_ALMA if 13 <= x <= 17 else PLATINO)
    # LA VENA DEL VEREDICTO (el oro vivo que corre por el alma).
    for y in (3, 8, 13, 17):
        px(im, 15, y, ORO_VEREDICTO)
    # LA PUNTA (el triángulo que cierra la hoja).
    px(im, 15, 0, BLANCO_CORONA)
    for x in (14, 15, 16):
        px(im, x, 1, PLATINO_ALMA)
    # LA GUARDA (y 20-21, x 7-23): oro macizo con sus DOS gemas.
    for x in range(7, 24):
        px(im, x, 20, ORO_VEREDICTO)
        px(im, x, 21, ORO_HONDO if x % 2 else ORO_VEREDICTO)
    disco(im, 7, 20, 1.6, VIOLETA_REAL)
    disco(im, 23, 20, 1.6, VIOLETA_REAL)
    # LAS DOS BALANZAS (los platillos del juicio: rombos de oro
    # colgando de los extremos de la guarda — el peso del veredicto).
    for bx in (4, 26):
        disco(im, bx, 23, 2.2, ORO_VEREDICTO, BLANCO_CORONA, 1.0)
        px(im, bx, 22, ORO_CORONA)
    # LA EMPUÑADURA (x 14-16, y 22-25): violeta hondo con alma.
    for y in range(22, 26):
        px(im, 14, y, VIOLETA_HONDO)
        px(im, 15, y, VIOLETA_REAL)
        px(im, 16, y, VIOLETA_HONDO)
    # EL POMO (la joya que cierra la espada).
    disco(im, 15, 27, 2.6, ORO_VEREDICTO, BLANCO_CORONA, 1.2)


# ================================================================ EL SELLO 9
def sello9(im):
    """LA MÚSICA DE LAS ESFERAS — el núcleo marfil y TRES anillos de
    sonido (rosa / violeta / oro) con SUS NOTAS orbitando. r2 (QA VLM:
    «el más lavado» — MÁS CONTRASTE: cada anillo con SU sombra honda
    por debajo (el rim de Terraria), núcleo más grande, notas gordas)."""
    cx, cy = 15.0, 15.0
    # EL RIM HONDO (la sombra del instrumento: un aro oscuro que
    # enmarca TODO y da el contraste que faltaba).
    arco(im, cx, cy, 14.3, 0, 2 * math.pi, (74, 36, 128), 3, VIOLETA_MELODIA)
    # LOS TRES ANILLOS (ondas concéntricas — el sonido hecho visible).
    arco(im, cx, cy, 8.4, 0, 2 * math.pi, ROSA, 4, ROSA_CLARO)
    arco(im, cx, cy, 11.6, 0, 2 * math.pi, VIOLETA_MELODIA, 4, (216, 186, 255))
    arco(im, cx, cy, 14.3, 0, 2 * math.pi, ORO_CORONA, 3, (255, 240, 190))
    # EL NÚCLEO: marfil macizo r=6.0 con corazón blanco.
    disco(im, cx, cy, 6.0, MARFIL, BLANCO_CORONA, 2.8)
    # LAS NOTAS ORBITANDO (3 cuentas gordas, una por anillo, en su
    # ángulo — la melodía viaja).
    for r, th_deg, c, alma in ((8.4, 35, ORO_CORONA, BLANCO_CORONA),
                               (11.6, 215, ROSA, ROSA_CLARO),
                               (14.3, 305, VIOLETA_MELODIA, (216, 186, 255))):
        th = math.radians(th_deg)
        disco(im, cx + math.cos(th) * r, cy + math.sin(th) * r, 2.2, c, alma, 1.0)


# =============================================================== EL SELLO 10
def sello10(im):
    """EL ÁRBOL DE LA VIDA — los DIEZ SEFIROT unidos por sus senderos
    de violeta, con KETER (la corona) arriba: la década completa."""
    # EL MAPA SAGRADO (x, y) — tres columnas: rigor, equilibrio, piedad.
    nodos = {
        'keter':   (15, 3),
        'jojmah':  (23, 7), 'binah': (7, 7),
        'daat':    (15, 10),
        'jesed':   (23, 14), 'gevura': (7, 14), 'tiferet': (15, 16),
        'netzaj':  (23, 22), 'hod': (7, 22),
        'iesod':   (15, 23),
        'maljut':  (15, 27),
    }
    # LOS SENDEROS (las 22 venas — aquí las maestras): violeta fino.
    senderos = [
        ('keter', 'jojmah'), ('keter', 'binah'), ('keter', 'tiferet'),
        ('jojmah', 'binah'), ('jojmah', 'jesed'), ('jojmah', 'tiferet'),
        ('jojmah', 'daat'),
        ('binah', 'gevura'), ('binah', 'tiferet'), ('binah', 'daat'),
        ('daat', 'tiferet'),
        ('jesed', 'gevura'), ('jesed', 'tiferet'), ('jesed', 'netzaj'),
        ('gevura', 'tiferet'), ('gevura', 'hod'),
        ('tiferet', 'netzaj'), ('tiferet', 'hod'), ('tiferet', 'iesod'),
        ('netzaj', 'hod'), ('netzaj', 'iesod'), ('hod', 'iesod'),
        ('iesod', 'maljut'),
    ]
    for a, b in senderos:
        x0, y0 = nodos[a]
        x1, y1 = nodos[b]
        linea_gorda(im, x0, y0, x1, y1, VIOLETA_ARBOL, 2, (196, 158, 250))
    # LOS NODOS: oro macizo con corazón blanco (el estándar v6.110).
    for nombre, (x, y) in nodos.items():
        if nombre == 'daat':
            continue  # DAAT es la puerta oculta: SOLO un destello tenue
        disco(im, x, y, 2.6, ORO_CORONA, BLANCO_CORONA, 1.2)
    # KETER — LA CORONA: el nodo mayor con sus TRES picos y su halo.
    kx, ky = nodos['keter']
    disco(im, kx, ky + 1, 3.2, ORO_CORONA, BLANCO_CORONA, 1.5)
    for dx in (-3, 0, 3):
        px(im, kx + dx, ky - 3, BLANCO_CORONA)
        px(im, kx + dx, ky - 2, ORO_CORONA)
    # EL HALO DE LA DÉCADA: el aro exterior GORDO que abraza el árbol.
    arco(im, 15, 15, 13.6, 0, 2 * math.pi, ORO_HONDO_CORONA, 3, ORO_CORONA)
    # DAAT — la puerta oculta (un destello pequeño entre tiferet y keter).
    dx_, dy_ = nodos['daat']
    disco(im, dx_, dy_ - 3, 1.6, (196, 158, 250), BLANCO_CORONA, 0.7)


# ------------------------------------------------------------------- salida
SELOS = {
    'FormaAscendidaCincoItem.png': sello5,
    'FormaAscendidaSeisItem.png': sello6,
    'FormaAscendidaSieteItem.png': sello7,
    'FormaAscendidaOchoItem.png': sello8,
    'FormaAscendidaNueveItem.png': sello9,
    'FormaAscendidaDiezItem.png': sello10,
}


def main():
    # LA HOJA DE CONTACTO para EL OJO (la familia entera, 1..10).
    familia = ['FormaAscendidaItem.png', 'FormaAscendidaDosItem.png',
               'FormaAscendidaTresItem.png', 'FormaAscendidaCuatroItem.png',
               'FormaAscendidaCincoItem.png', 'FormaAscendidaSeisItem.png',
               'FormaAscendidaSieteItem.png', 'FormaAscendidaOchoItem.png',
               'FormaAscendidaNueveItem.png', 'FormaAscendidaDiezItem.png']
    zoom, gap = 8, 4
    celda = 30 * zoom + gap
    hoja = Image.new('RGBA', (celda * 10, 30 * zoom), (26, 24, 36, 255))
    for idx, nombre in enumerate(familia):
        if nombre in SELOS:
            im = nuevo()
            SELOS[nombre](im)
            im.save(os.path.join(DEST, nombre))
        else:
            im = Image.open(os.path.join(DEST, nombre)).convert('RGBA')
        # LA VERIFICACIÓN DE LA CASA: opacidad de la familia.
        op = sum(1 for yy in range(30) for xx in range(30)
                 if im.getpixel((xx, yy))[3] > 200) / 900.0
        print(f'  {nombre}: {100 * op:.0f}% opaco')
        grande = im.resize((30 * zoom, 30 * zoom), Image.NEAREST)
        hoja.paste(grande, (idx * celda + gap // 2, 0), grande)
    hoja.save('/tmp/formas_v6110_qa.png')
    print('hoja de contacto: /tmp/formas_v6110_qa.png')


if __name__ == '__main__':
    main()
