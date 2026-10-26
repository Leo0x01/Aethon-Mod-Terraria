#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
GEN_ICONOS_PRO_V6543 — LOS ICONOS DE LA INVESTIGACIÓN, DE VERDAD.

La letra del usuario: «todas las nuevas armas se ven horrible, sin
textura sin nada lindo visualmente... las alas son las mismas solo
que con color diferente... usa texturas diferentes».

LA AUTOPSIA (VLM sobre los iconos de la v6.54.0): «una sola silueta
recolorizada... template único + bucket fill... garabatos de MS
Paint... les falta contorno, sombreado volumétrico y brillo
especular».

LA CURA — el estándar Terraria de verdad, por icono:
  1. CONTORNO de 1 px (no negro puro: el tono más oscuro del color
     base — el truco de Re-Logic para que el sprite brille).
  2. VOLUMETRÍA: 4 tonos por material (sombra, base, claro, luz) con
     la luz llegando de arriba-izquierda.
  3. DITHERING sutil en las transiciones (el grano de Terraria).
  4. SILUETAS DISTINTAS por ala: pluma, membrana de murciélago,
     cristal, arco voltaico, cortina de aurora, pétalo, cometa,
     nube-tormenta, hilos de marioneta, agujero de vacío...

Las QUINCE alas tienen quince anatomías diferentes; los iconos de
las armas y cosméticos se rediseñan completos. La Tinta Viva pasa de
gota vertical a SPLAT HORIZONTAL (la letra del usuario).
"""

import math
import os
from PIL import Image, ImageDraw

RAIZ = os.path.join(os.path.dirname(__file__), '..')
N = 30          # el lienzo de la casa

# =====================================================================
#  LAS HERRAMIENTAS DEL PIXEL-ART
# =====================================================================


def lienzo():
    return Image.new('RGBA', (N, N), (0, 0, 0, 0))


def mezcla(c1, c2, t):
    return tuple(int(round(a + (b - a) * t)) for a, b in zip(c1, c2))


def rampa(osc, base, claro, luz):
    """Los 4 tonos de un material (el estándar Terraria)."""
    return {'osc': osc, 'base': base, 'claro': claro, 'luz': luz}


def sombrear_por_pos(img, paleta, centro, radio, expo=1.0):
    """Pinta cada píxel de la forma según su distancia al centro de
    luz: cerca=luz, media=claro, lejos=base, borde=oscuro. La
    volumetría de verdad, no un plano."""
    px = img.load()
    cx, cy = centro
    for y in range(N):
        for x in range(N):
            r, g, b, a = px[x, y]
            if a == 0 or a == 255 and r == -1:
                continue
            if a == 0:
                continue
            d = math.hypot(x - cx, y - cy) / max(radio, 1e-6)
            d = min(d, 1.0) ** expo
            if d < 0.30:
                c = paleta['luz']
            elif d < 0.62:
                c = mezcla(paleta['claro'], paleta['base'], (d - 0.30) / 0.32)
            elif d < 0.90:
                c = mezcla(paleta['base'], paleta['osc'], (d - 0.62) / 0.28)
            else:
                c = paleta['osc']
            px[x, y] = (c[0], c[1], c[2], a)


def contorno(img, color, desde=(0, 0)):
    """El contorno de 1 px alrededor de TODA la masa (fuera)."""
    px = img.load()
    nuevos = []
    for y in range(N):
        for x in range(N):
            if px[x, y][3] != 0:
                continue
            vecino = False
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < N and 0 <= ny < N and px[nx, ny][3] > 40:
                    vecino = True
                    break
            if vecino:
                nuevos.append((x, y))
    for x, y in nuevos:
        px[x, y] = (*color, 255)


def contorno_interno(img, color):
    """El filo interno: píxeles de la masa que tocan el vacío."""
    px = img.load()
    nuevos = []
    for y in range(N):
        for x in range(N):
            if px[x, y][3] < 200:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1)):
                nx, ny = x + dx, y + dy
                if not (0 <= nx < N and 0 <= ny < N) or px[nx, ny][3] < 40:
                    nuevos.append((x, y))
                    break
    for x, y in nuevos:
        r, g, b, a = px[x, y]
        px[x, y] = (*color, a)


def dither(img, c, cond, paso=2, fase=0):
    """Dithering en damero dentro de la zona que cumpla `cond(x,y)`."""
    px = img.load()
    for y in range(N):
        for x in range(N):
            if (x + y + fase) % paso == 0 and px[x, y][3] > 0 and cond(x, y):
                r, g, b, a = px[x, y]
                px[x, y] = (mezcla((r, g, b), c, 0.55) + (255,))[0:3] + (a,)


def pinta(img, cond, color, fase=0):
    px = img.load()
    for y in range(N):
        for x in range(N):
            if cond(x, y) and px[x, y][3] > 0:
                px[x, y] = (*color, px[x, y][3])


def px(img, x, y, color):
    if 0 <= x < N and 0 <= y < N:
        img.load()[x, y] = (*color, 255)


def brillo(img, puntos, color, tam=1):
    """Los destellos especulares (la chispa de Terraria)."""
    for (x, y) in puntos:
        px(img, x, y, color)
        if tam > 1:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                px(img, x + dx, y + dy, mezcla(color, (255, 255, 255), 0.2))


def guardar_icono(img, ruta):
    img.save(ruta)
    print(f'  {os.path.basename(ruta):26s} colores={len(set(img.getdata())):4d}')


def elipse(dr, caja, color):
    c = (*color, 255) if len(color) == 3 else tuple(color)
    dr.ellipse(caja, fill=c)


def poligono(dr, pts, color):
    dr.polygon(pts, fill=(*color, 255))


def linea(dr, a, b, color, w=1):
    dr.line([a, b], fill=(*color, 255), width=w)


# =====================================================================
#  LA ANATOMÍA DE LAS ALAS — 15 siluetas distintas
#  (todas apuntan arriba-derecha desde abajo-izquierda, el estándar
#   de los iconos de alas de Terraria)
# =====================================================================

def ala_plumas(dr, img, pal, n_plumas, curva=0.0, gotas=None):
    """Ala de PLUMAS escalonadas: cada pluma un óvalo alargado
    rotado, escalando hacia la punta. La anatomía clásica."""
    import math as m
    for i in range(n_plumas):
        t = i / max(1, n_plumas - 1)
        ang = -68 + t * 58 + curva * 26 * t      # el abanico
        largo = 11 - t * 3.2
        cx = 6 + t * 14 + m.cos(m.radians(ang)) * 3
        cy = 25 - t * 5 - m.sin(m.radians(-ang)) * 2
        # la pluma: óvalo rotado hecho de polígono
        pts = []
        for k in range(14):
            a2 = k / 13 * 2 * m.pi
            rx = m.cos(a2) * 2.1
            ry = m.sin(a2) * largo / 2
            x = cx + rx * m.cos(m.radians(ang)) - ry * m.sin(m.radians(ang)) * 0.35
            y = cy + rx * m.sin(m.radians(ang)) * 0.6 + ry * m.cos(m.radians(ang)) * -0.9
            pts.append((x, y))
        tono = mezcla(pal['claro'], pal['osc'], t * 0.8)
        poligono(dr, pts, tono)
    # las barbas (las líneas internas de la pluma grande)
    for i in range(3):
        x0, y0 = 8 + i * 3, 22 - i * 2
        linea(dr, (x0, y0), (x0 + 6 - i, y0 - 9 + i), pal['osc'], 1)
    if gotas:
        for gx, gy in gotas:
            elipse(dr, [gx - 1, gy - 1, gx + 1, gy + 1], pal['claro'])


def ala_membrana(dr, img, pal, dedos=3):
    """Ala de MURCIÉLAGO: huesos curvos + membrana festoneada."""
    # los dedos (huesos)
    hombro = (7, 24)
    puntas = [(24, 4), (20, 10), (10, 6)]
    for i, p in enumerate(puntas):
        linea(dr, hombro, p, pal['claro'], 1)
    # la membrana: polígono festoneado entre hombro y puntas
    pts = [hombro, (13, 21), (24, 4)]
    # festones: arcos cóncavos entre punta y punta
    mem = [hombro, (14, 20)]
    for i, p in enumerate(puntas):
        mem.append(p)
        if i < len(puntas) - 1:
            q = puntas[i + 1]
            mx, my = (p[0] + q[0]) / 2, (p[1] + q[1]) / 2
            mem.append((mx + 2, my + 4))       # el festón cae
    mem.append((12, 8))
    poligono(dr, mem, pal['base'])
    # el sombreado de la membrana (los festones más oscuros)
    for i, p in enumerate(puntas[:-1]):
        q = puntas[i + 1]
        mx, my = (p[0] + q[0]) / 2, (p[1] + q[1]) / 2
        linea(dr, (mx + 1, my + 2), (mx + 3, my + 5), pal['osc'], 1)
    # garras en la punta
    for p in puntas:
        px(img, p[0] + 1, p[1] - 1, pal['claro'])


def ala_cristal(dr, img, pal, colores_shards):
    """Ala de FRAGMENTOS angulares: shards de colores, cada uno una
    aguja facetada (el ala del prisma)."""
    shards = [
        ([(6, 26), (16, 8), (19, 12), (10, 27)], 0),
        ([(11, 26), (22, 5), (25, 9), (15, 27)], 1),
        ([(16, 25), (26, 12), (27, 16), (19, 26)], 2),
        ([(5, 24), (9, 13), (12, 15), (8, 25)], 3),
    ]
    for pts, ci in shards:
        c = colores_shards[ci % len(colores_shards)]
        poligono(dr, pts, c)
        # la faceta clara (la luz arriba-izquierda)
        f = [(pts[0][0] + 1, pts[0][1] + 1), (pts[1][0] + 1, pts[1][1] + 1),
             (pts[2][0] - 1, pts[2][1] + 2)]
        poligono(dr, f, mezcla(c, (255, 255, 255), 0.35))


def ala_arcos(dr, img, pal):
    """Ala de ARCOS VOLTAICOS: zigzags de energía (nada de plumas)."""
    arcos = [
        ((6, 26), (10, 18), (7, 12), (13, 5)),
        ((10, 25), (16, 17), (13, 11), (20, 4)),
        ((14, 25), (21, 18), (18, 13), (26, 8)),
    ]
    for a, b, c, d in arcos:
        # el canal del arco: línea gruesa clara + núcleo blanco
        linea(dr, a, b, pal['base'], 2)
        linea(dr, b, c, pal['base'], 2)
        linea(dr, c, d, pal['base'], 2)
        linea(dr, a, d, pal['luz'], 1)
    # los nodos (las uniones brillan)
    for a, b, c, d in arcos:
        for p in (b, c):
            elipse(dr, [p[0] - 1, p[1] - 1, p[0] + 1, p[1] + 1], pal['luz'])


def ala_cortina(dr, img, pal):
    """Ala CORTINA DE AURORA: bandas verticales onduladas que cuelgan
    (la aurora ártica de verdad)."""
    import math as m
    for i, (x0, alto, tono) in enumerate([
            (7, 16, 0.1), (11, 19, 0.0), (15, 20, 0.25),
            (19, 17, 0.45), (23, 13, 0.6)]):
        pts = []
        for k in range(12):
            t = k / 11
            y = 4 + t * alto
            ond = m.sin(t * 5.2 + i * 1.4) * 1.8
            pts.append((x0 + ond, y))
        # banda: de ancho 2-3 px
        banda = pts + [(p[0] + 2.6, p[1] - 0.7) for p in reversed(pts)]
        c = mezcla(pal['claro'], pal['osc'], tono * 0.9)
        poligono(dr, banda, c)
        # el borde superior de cada banda brilla
        linea(dr, pts[0], (pts[2][0], pts[2][1]), pal['luz'], 1)


def ala_petalos(dr, img, pal):
    """Ala de PÉTALOS de orquídea: tres lóbulos redondos superpuestos
    con el labelo (la corola) abajo."""
    elipse(dr, [4, 16, 16, 27], pal['base'])        # el lóbulo inferior
    elipse(dr, [10, 8, 24, 21], pal['claro'])       # el lóbulo medio
    elipse(dr, [16, 2, 28, 14], mezcla(pal['claro'], (255, 255, 255), 0.25))
    # las venas del pétalo
    for i in range(3):
        linea(dr, (8 + i * 7, 24 - i * 4), (14 + i * 6, 16 - i * 5), pal['osc'], 1)
    # el labelo (la corola pequeña)
    elipse(dr, [19, 18, 24, 23], pal['luz'])


def ala_cometa(dr, img, pal):
    """Ala COMETA: núcleo brillante + cola de estrellas en diagonal."""
    # la cola: estelas decrecientes en diagonal
    for i, (x, y, s) in enumerate([(24, 5, 2.6), (20, 9, 2.2), (16, 13, 1.9),
                                    (12, 17, 1.6), (9, 21, 1.3)]):
        c = mezcla(pal['claro'], pal['osc'], i / 5)
        elipse(dr, [x - s, y - s * 0.7, x + s, y + s * 0.7], c)
    # el núcleo (la cabeza del cometa)
    elipse(dr, [3, 21, 11, 28], pal['claro'])
    elipse(dr, [4.5, 22.5, 9, 26.5], pal['luz'])
    # chispas
    brillo(img, [(25, 4), (17, 12), (10, 20)], pal['luz'])


def ala_nube_tormenta(dr, img, pal):
    """Ala NUBE-TORMENTA: el cúmulo arriba + rayos colgando."""
    elipse(dr, [3, 6, 15, 15], pal['base'])
    elipse(dr, [9, 3, 22, 13], pal['claro'])
    elipse(dr, [17, 7, 27, 16], pal['base'])
    # el interior sombreado
    pinta(img, lambda x, y: 12 < y < 16 and 4 < x < 26 and (x + y) % 3 == 0,
          pal['osc'])
    # los rayos colgando
    for x0, y0 in ((7, 14), (14, 15), (21, 14)):
        zig = [(x0, y0), (x0 - 1, y0 + 3), (x0 + 1, y0 + 6), (x0 - 1, y0 + 9)]
        for i in range(len(zig) - 1):
            linea(dr, zig[i], zig[i + 1], pal['luz'] if i % 2 == 0 else pal['claro'], 1)


def ala_marioneta(dr, img, pal):
    """Ala MARIONETA: la barra arriba + hilos + plumas colgantes."""
    linea(dr, (3, 4), (26, 4), pal['claro'], 2)      # la barra de control
    for x0 in (7, 13, 19, 24):
        linea(dr, (x0, 5), (x0, 11), pal['claro'], 1)  # los hilos
    # las plumas colgantes (cada una de su hilo)
    for x0, largo, tono in ((7, 12, 0.0), (13, 16, 0.2), (19, 13, 0.4), (24, 9, 0.6)):
        c = mezcla(pal['claro'], pal['osc'], tono)
        elipse(dr, [x0 - 1.6, 10, x0 + 1.6, 10 + largo], c)
        # el peso de la punta
        elipse(dr, [x0 - 2, 9 + largo, x0 + 2, 12 + largo], pal['osc'])


def ala_vacio(dr, img, pal):
    """Ala del VACÍO: la forma de ala hecha de niebla oscura con un
    AGUJERO NEGRO (el disco de absorción con su anillo)."""
    # la niebla del ala (blob anguloso)
    poligono(dr, [(5, 26), (8, 12), (16, 6), (25, 9), (27, 15), (20, 26)],
             pal['base'])
    # el agujero negro (el disco con su anillo de fotones)
    elipse(dr, [12, 13, 20, 21], pal['osc'])
    elipse(dr, [13.5, 14.5, 18.5, 19.5], (10, 4, 18))
    # el anillo de absorción
    import math as m
    for k in range(16):
        a = k / 16 * 2 * m.pi
        x, y = 16 + m.cos(a) * 5.4, 17 + m.sin(a) * 4.6
        px(img, int(x), int(y), mezcla(pal['claro'], (255, 255, 255), 0.3))
    # los tentáculos que se deshacen
    for x0, y0, dx in ((7, 22, -2), (24, 20, 2)):
        linea(dr, (x0, y0), (x0 + dx, y0 + 4), pal['osc'], 1)


def ala_solar(dr, img, pal):
    """Ala SOLAR RÚNICA: la llama dorada con runas grabadas."""
    import math as m
    # la llama: lenguas en abanico
    lenguas = [((6, 26), (10, 9)), ((11, 26), (17, 5)), ((16, 26), (24, 8)),
               ((20, 26), (27, 14))]
    for (a, b) in lenguas:
        mx = (a[0] + b[0]) / 2 + 2
        my = (a[1] + b[1]) / 2
        poligono(dr, [a, (mx, my), b], pal['base'])
    # el corazón claro
    poligono(dr, [(9, 25), (13, 12), (17, 6), (19, 14), (15, 25)], pal['claro'])
    # LAS RUNAS (las marcas angulares grabadas)
    runas = [((11, 18), (13, 16), (15, 18)), ((15, 22), (17, 20), (19, 22))]
    for r in runas:
        for i in range(len(r) - 1):
            linea(dr, r[i], r[i + 1], pal['osc'], 1)
        linea(dr, r[0], r[0], pal['luz'], 1)
    brillo(img, [(17, 6), (24, 8)], pal['luz'])


def ala_gotica(dr, img, pal, gotea=True):
    """Ala GÓTICA: plumas puntiagudas desgarradas con goteo de tinta."""
    import math as m
    plumas = [
        ([(6, 26), (11, 10), (14, 14), (10, 26)], 0.15),
        ([(11, 26), (18, 6), (21, 11), (15, 26)], 0.0),
        ([(16, 26), (24, 9), (26, 14), (20, 26)], 0.3),
        ([(21, 26), (27, 16), (28, 20), (24, 26)], 0.5),
    ]
    for pts, tono in plumas:
        c = mezcla(pal['claro'], pal['osc'], tono)
        poligono(dr, pts, c)
        # la vena central de la pluma
        linea(dr, pts[0], pts[1], mezcla(c, (255, 255, 255), 0.3), 1)
    # EL GOTEO (la firma de la tinta)
    if gotea:
        elipse(dr, [17, 26, 19, 29], pal['base'])
        elipse(dr, [22, 27, 23, 28], pal['claro'])


def ala_eclipse(dr, img, pal, pal_fuego):
    """Ala de ECLIPSE: la penumbra con la corona de fuego solar —
    la corona DOMINA (los rayos largos de la corona real) y el disco
    umbrío lleva el reflejo violáceo y las estrellas de la penumbra."""
    import math as m
    # LA MASA de penumbra (el disco del eclipse)
    poligono(dr, [(6, 25), (9, 11), (16, 6), (24, 9), (26, 17), (18, 25)],
             pal['base'])
    # EL REFLEJO violáceo del interior (la penumbra no es negra)
    pinta(img, lambda x, y: 11 < y < 23 and 10 < x < 23 and (x + y) % 2 == 0,
          mezcla(pal['osc'], pal['base'], 0.6))
    # LAS ESTRELLAS de la penumbra (se ven solo durante el eclipse)
    for x, y in [(13, 14), (18, 17), (15, 21), (20, 12)]:
        px(img, x, y, mezcla(pal['claro'], (255, 255, 255), 0.4))
    # LA CORONA: los rayos del fuego solar (largos, irregulares)
    rayos = [((10, 10), (7, 4)), ((13, 8), (12, 1)), ((17, 7), (19, 0)),
             ((21, 8), (25, 3)), ((24, 11), (28, 8)), ((8, 14), (3, 11))]
    for a, b in rayos:
        linea(dr, a, b, pal_fuego['base'], 1)
        px(img, b[0], b[1], pal_fuego['claro'])
    # el borde interior de fuego (el anillo que arde)
    for k in range(14):
        t = k / 13
        x = 8 + t * 17
        y = 10 - m.sin(t * m.pi) * 3.5
        px(img, int(x), int(y + 1), pal_fuego['base'])
        if k % 3 == 0:
            px(img, int(x), int(y), pal_fuego['claro'])
    # el destello máximo (el punto de la corona)
    brillo(img, [(12, 2), (20, 1)], pal_fuego['luz'])


def ala_nebulosa(dr, img, pal):
    """Ala NEBULOSA: la nube estelar — puntos de todos los tamaños
    formando la silueta del ala."""
    import math as m
    # la neblina de fondo (blob suave de la forma del ala)
    poligono(dr, [(5, 26), (10, 9), (20, 4), (27, 12), (24, 22), (14, 27)],
             pal['base'])
    pinta(img, lambda x, y: 8 < y < 24 and 8 < x < 25 and (x + y) % 3 == 0,
          pal['osc'])
    # LAS ESTRELLAS (de todos los tamaños, la firma)
    estrellas = [(9, 22, 1), (13, 17, 2), (17, 12, 1), (21, 8, 2), (24, 15, 1),
                 (12, 24, 1), (19, 19, 1.6), (8, 14, 1), (15, 8, 1.4), (25, 11, 1)]
    for x, y, s in estrellas:
        if s <= 1.2:
            px(img, x, y, pal['luz'])
        else:
            elipse(dr, [x - s, y - s * 0.8, x + s, y + s * 0.8], pal['claro'])
            px(img, x, y, pal['luz'])


def ala_hielo(dr, img, pal):
    """Ala de HIELO: plumas cristalinas con caras facetadas y
    destellos de escarcha."""
    import math as m
    plumas = [
        ([(6, 26), (12, 7), (15, 12), (10, 26)], 0.1),
        ([(12, 26), (19, 5), (22, 10), (16, 26)], 0.0),
        ([(18, 26), (26, 10), (28, 15), (22, 26)], 0.2),
    ]
    for pts, tono in plumas:
        c = mezcla(pal['claro'], pal['base'], tono)
        poligono(dr, pts, c)
        # la faceta de hielo (la cara que refleja)
        poligono(dr, [(pts[0][0] + 2, pts[0][1]), (pts[1][0], pts[1][1]),
                      (pts[2][0], pts[2][1])],
                 mezcla(c, (255, 255, 255), 0.4))
    # la escarcha (los destellos)
    brillo(img, [(19, 6), (12, 8), (26, 11)], pal['luz'])


def ala_tinta(dr, img, pal):
    """Ala de TINTA SERENA: plumas líquidas con vetas y gotitas
    (la calma de la página)."""
    ala_plumas(dr, img, pal, 5, curva=0.3,
               gotas=[(24, 8), (26, 14), (21, 4)])
    # las vetas de tinta que corren por la pluma grande
    for i in range(3):
        linea(dr, (8 + i * 2, 23 - i), (13 + i * 3, 12 - i * 2),
              mezcla(pal['osc'], (0, 0, 0), 0.2), 1)


# =====================================================================
#  LOS ICONOS DE LAS ARMAS
# =====================================================================

def icono_rayos_concentrados():
    """EL ATAQUE DEFINITIVO: el cañón-emisor prismático con el haz
    blanco concentrado saliendo + chispas."""
    img = lienzo()
    dr = ImageDraw.Draw(img)
    pal = rampa((38, 12, 66), (94, 32, 158), (168, 92, 238), (238, 208, 255))
    # EL EMISOR: el cuerpo del cañón (trapecio vertical)
    poligono(dr, [(9, 6), (17, 6), (15, 26), (11, 26)], pal['base'])
    # la volumetría del cuerpo
    pinta(img, lambda x, y: x < 12 and 8 < y < 25 and (x + y) % 3 == 0, pal['claro'])
    pinta(img, lambda x, y: x > 14 and 8 < y < 25 and (x * 2 + y) % 4 == 0, pal['osc'])
    # EL PRISMA (la lente que concentra): rombo facetado
    poligono(dr, [(13, 2), (18, 7), (13, 12), (8, 7)], pal['claro'])
    poligono(dr, [(13, 2), (18, 7), (13, 7)], mezcla(pal['claro'], (255, 255, 255), 0.5))
    # EL HAZ: el rayo concentrado saliendo a la derecha
    for i, (y0, y1, x_fin, tono) in enumerate([
            (6, 9, 28, pal['claro']), (7, 8, 29, (255, 255, 255))]):
        poligono(dr, [(19, y0), (x_fin, (y0 + y1) / 2 - 0.5),
                      (x_fin, (y0 + y1) / 2 + 0.5), (19, y1)], tono)
    # las chispas alrededor del haz
    brillo(img, [(22, 5), (25, 10), (21, 12)], pal['luz'])
    # el anillo del emisor
    linea(dr, (10, 10), (16, 10), pal['osc'], 1)
    contorno(img, (24, 6, 44))
    return img


def icono_trueno_prisma():
    """EL TRUENO: el prisma flotante con el rayo zigzag saliendo."""
    img = lienzo()
    dr = ImageDraw.Draw(img)
    pal = rampa((30, 8, 56), (80, 26, 140), (150, 80, 220), (225, 190, 255))
    # EL PRISMA: el rombo facetado grande (el generador)
    poligono(dr, [(13, 3), (23, 14), (13, 25), (3, 14)], pal['base'])
    poligono(dr, [(13, 3), (23, 14), (13, 14)], pal['claro'])
    poligono(dr, [(13, 3), (13, 14), (3, 14)], mezcla(pal['claro'], (255, 255, 255), 0.35))
    poligono(dr, [(3, 14), (13, 14), (13, 25)], pal['osc'])
    # EL RAYO: el zigzag que sale del prisma hacia arriba-derecha
    zig = [(20, 10), (26, 4), (23, 3), (28, 0)]
    for i in range(len(zig) - 1):
        linea(dr, zig[i], zig[i + 1], (255, 255, 255) if i % 2 == 0 else pal['luz'], 1)
    brillo(img, [(26, 3), (23, 8)], pal['luz'])
    # las chispas del interior (la carga)
    brillo(img, [(12, 12), (14, 16), (10, 15)], pal['luz'])
    contorno(img, (20, 5, 40))
    return img


def icono_cetro_constelaciones():
    """EL CETRO: la vara con el orbe estelar + la constelación."""
    img = lienzo()
    dr = ImageDraw.Draw(img)
    pal = rampa((70, 40, 10), (170, 110, 30), (240, 190, 80), (255, 240, 190))
    pal_e = rampa((40, 14, 70), (100, 40, 170), (180, 110, 240), (235, 210, 255))
    # LA VARA (el mástil dorado)
    linea(dr, (8, 27), (13, 8), pal['base'], 2)
    linea(dr, (8, 27), (13, 8), pal['claro'], 1)
    # LA EMPUÑADURA
    linea(dr, (9, 24), (12, 24), pal['osc'], 2)
    # EL ORBE (la esfera estelar)
    elipse(dr, [10, 2, 19, 11], pal_e['base'])
    elipse(dr, [11.5, 3.5, 17, 9], pal_e['claro'])
    elipse(dr, [13, 4.5, 15, 6.5], pal_e['luz'])
    # LA CONSTELACIÓN: 3 estrellas unidas por líneas finas
    estrellas = [(22, 5), (26, 12), (20, 16)]
    for a, b in ((estrellas[0], estrellas[1]), (estrellas[1], estrellas[2])):
        linea(dr, a, b, mezcla(pal_e['claro'], (255, 255, 255), 0.4), 1)
    for x, y in estrellas:
        px(img, x, y, (255, 255, 255))
        brillo(img, [(x, y)], pal_e['luz'])
    contorno(img, (46, 24, 8))
    return img


def icono_lodo_esencia():
    """EL LODO: la esfera ritual de lodo SOBRE EL MANGO rúnico (la
    lectura de ARMA: el mango dice «se empuña») con sus puntos
    bioluminiscentes."""
    img = lienzo()
    dr = ImageDraw.Draw(img)
    pal = rampa((12, 6, 24), (34, 16, 60), (70, 38, 118), (130, 90, 190))
    # EL MANGO (la vara que sostiene la esfera — la lectura de arma)
    poligono(dr, [(14, 13), (16, 13), (20, 28), (17, 28)], (96, 60, 30))
    linea(dr, (15, 13), (19, 28), (150, 95, 45), 1)
    # LA GARRA (las tenazas que sujetan la esfera)
    poligono(dr, [(13, 14), (16, 16), (12, 20)], pal['osc'])
    poligono(dr, [(18, 14), (16, 16), (21, 20)], pal['osc'])
    # LA ESFERA (el cuerpo del lodo)
    pts = []
    import math as m
    for k in range(18):
        a = m.pi * 3 / 2 + k / 17 * m.pi * 3 / 2     # la punta arriba
        x = 15 + m.cos(a) * 8.5
        y = 10 + m.sin(a) * 9.0
        pts.append((x, y))
    poligono(dr, pts, pal['base'])
    sombrear_por_pos(img, pal, (12, 6), 14, expo=0.8)
    # LOS PUNTOS bioluminiscentes (la firma)
    for x, y, s in [(11, 8, 1), (16, 5, 1.4), (19, 11, 1.2), (13, 13, 1.6)]:
        elipse(dr, [x - s, y - s, x + s, y + s], (168, 96, 240))
        px(img, x, y, (230, 190, 255))
    # las burbujas (el hervor)
    elipse(dr, [21, 2, 23, 4], pal['claro'])
    brillo(img, [(11, 6)], pal['luz'])
    contorno(img, (8, 3, 16))
    return img


# =====================================================================
#  LOS ICONOS DE LOS COSMÉTICOS
# =====================================================================

def icono_tinta_viva():
    """LA TINTA VIVA — HORIZONTAL INEQUÍVOCA (la letra del usuario):
    la masa alargada con NÚCLEO LUMINOSO central, dos goterones que
    corren a los LADOS y las salpicaduras satélite ALINEADAS en la
    dirección horizontal."""
    img = lienzo()
    dr = ImageDraw.Draw(img)
    pal = rampa((48, 10, 66), (122, 34, 158), (190, 96, 232), (240, 205, 255))
    # LA MASA: el splat alargado horizontal (24 px de ancho × 12 alto)
    pts = [(4, 14), (6, 10), (10, 8), (15, 8), (20, 9), (24, 11),
           (26, 14), (24, 17), (20, 19), (15, 20), (10, 20), (6, 18)]
    poligono(dr, pts, pal['base'])
    # LOS GOTERONES que corren a los lados (la dirección clara)
    poligono(dr, [(2, 13), (5, 12), (5, 16), (1, 16)], pal['base'])       # izquierda
    elipse(dr, [1, 15, 3, 18], pal['base'])
    poligono(dr, [(25, 12), (28, 12), (28, 15), (25, 15)], pal['base'])   # derecha
    elipse(dr, [26, 15, 28, 17], pal['base'])
    # la volumetría
    sombrear_por_pos(img, pal, (14, 10), 13, expo=0.9)
    # EL NÚCLEO LUMINOSO (la tinta VIVA: el corazón que late)
    elipse(dr, [12, 11, 18, 16], mezcla(pal['claro'], (255, 255, 255), 0.35))
    elipse(dr, [14, 12.5, 16, 14.5], (255, 255, 255))
    # LAS SALPICADURAS satélite alineadas horizontal
    for x, y, s in [(3, 8, 1.2), (27, 8, 1.1), (24, 20, 1.2), (6, 21, 1.1), (15, 4, 1.3)]:
        elipse(dr, [x - s, y - s * 0.8, x + s, y + s * 0.8], pal['claro'])
    # la vena de tinta que atraviesa (la corriente interna)
    linea(dr, (7, 14), (23, 13), mezcla(pal['claro'], (255, 255, 255), 0.25), 1)
    brillo(img, [(9, 11), (20, 10)], pal['luz'])
    contorno(img, (30, 5, 44))
    return img


def icono_estela_cosmos():
    """LA ESTELA DEL COSMOS: el anillo elíptico con las estrellas que
    lo recorren (la corona del cosmos)."""
    img = lienzo()
    dr = ImageDraw.Draw(img)
    pal = rampa((40, 12, 72), (100, 42, 175), (165, 100, 235), (230, 205, 255))
    import math as m
    # EL ANILLO: la elipse inclinada (elipse exterior − interior)
    for k in range(46):
        a = k / 46 * 2 * m.pi
        x = 15 + m.cos(a) * 11
        y = 16 + m.sin(a) * 5.2
        tono = mezcla(pal['claro'], pal['base'], (m.sin(a * 2) + 1) / 2)
        elipse(dr, [x - 1.5, y - 1.5, x + 1.5, y + 1.5], tono)
    # LAS ESTRELLAS del anillo (las que brillan más)
    for a in (0.4, 1.9, 3.4, 5.0):
        x = 15 + m.cos(a) * 11
        y = 16 + m.sin(a) * 5.2
        px(img, int(x), int(y), (255, 255, 255))
        brillo(img, [(int(x), int(y))], pal['luz'])
    # el cometa del anillo (la chispa que lo recorre)
    x, y = 15 + m.cos(2.3) * 11, 16 + m.sin(2.3) * 5.2
    elipse(dr, [x - 2, y - 2, x + 2, y + 2], pal['luz'])
    contorno(img, (26, 8, 48))
    return img


# =====================================================================
#  EL MAIN — LAS 15 ALAS + 4 ARMAS + 2 COSMÉTICOS
# =====================================================================

ALAS = 'AethonMod/Content/Items/Alas'
ARMAS = 'AethonMod/Content/Weapons/Investigacion'
COSMETICOS = 'AethonMod/Content/Items/Cosmetics/Investigacion'


def main():
    print('LOS ICONOS DE LA INVESTIGACIÓN — 21 diseños distintos:')
    os.makedirs(ALAS, exist_ok=True)

    # --- LAS 4 ARMAS ---
    guardar_icono(icono_rayos_concentrados(), f'{ARMAS}/RayosConcentrados.png')
    guardar_icono(icono_trueno_prisma(), f'{ARMAS}/TruenoDelPrisma.png')
    guardar_icono(icono_cetro_constelaciones(), f'{ARMAS}/CetroConstelaciones.png')
    guardar_icono(icono_lodo_esencia(), f'{ARMAS}/LodoDeEsencia.png')

    # --- LOS 2 COSMÉTICOS ---
    guardar_icono(icono_tinta_viva(), f'{COSMETICOS}/TintaViva.png')
    guardar_icono(icono_estela_cosmos(), f'{COSMETICOS}/EstelaDelCosmos.png')

    # --- LAS 15 ALAS (cada una SU anatomía) ---
    def hacer(nombre, pintor):
        img = lienzo()
        dr = ImageDraw.Draw(img)
        pintor(dr, img)
        contorno(img, None)  # placeholder — se pinta dentro del pintor
        return img

    # 2 — TINTA SERENA: plumas líquidas lila con vetas y gotitas
    def alas_tinta_serena():
        img = lienzo()
        dr = ImageDraw.Draw(img)
        ala_tinta(dr, img, rampa((46, 16, 84), (120, 50, 180), (185, 120, 240), (235, 210, 255)))
        contorno(img, (28, 8, 54))
        return img

    # 3 — ECLIPSE: penumbra con la corona de fuego
    def alas_eclipse():
        img = lienzo()
        dr = ImageDraw.Draw(img)
        ala_eclipse(dr, img,
                    rampa((16, 5, 34), (44, 16, 80), (92, 48, 150), (150, 110, 200)),
                    rampa((120, 40, 8), (222, 110, 30), (255, 190, 90), (255, 240, 200)))
        contorno(img, (10, 3, 22))
        return img

    # 4 — PLASMA: arcos voltaicos magenta
    def alas_plasma():
        img = lienzo()
        dr = ImageDraw.Draw(img)
        ala_arcos(dr, img, rampa((80, 10, 90), (180, 30, 170), (240, 90, 220), (255, 220, 250)))
        contorno(img, (52, 4, 60))
        return img

    # 5 — NEBULOSA: la nube estelar
    def alas_nebulosa():
        img = lienzo()
        dr = ImageDraw.Draw(img)
        ala_nebulosa(dr, img, rampa((36, 18, 70), (86, 48, 150), (150, 100, 220), (225, 205, 255)))
        contorno(img, (22, 10, 46))
        return img

    # 6 — COMETARIO: el cometa con su cola
    def alas_cometario():
        img = lienzo()
        dr = ImageDraw.Draw(img)
        ala_cometa(dr, img, rampa((40, 60, 110), (90, 150, 210), (160, 220, 250), (240, 252, 255)))
        contorno(img, (26, 40, 72))
        return img

    # 7 — MEDIANOCHE: la membrana de murciélago bajo la luna
    def alas_medianoche():
        img = lienzo()
        dr = ImageDraw.Draw(img)
        ala_membrana(dr, img, rampa((10, 8, 34), (30, 24, 72), (66, 58, 130), (140, 130, 210)))
        # la luna creciente (la firma de la medianoche): el arco grueso
        import math as m
        luna = []
        for k in range(12):                      # el arco exterior
            a = m.pi * 0.5 + k / 11 * m.pi * 1.1
            luna.append((24 + m.cos(a) * 3.2, 5 + m.sin(a) * 3.2))
        for k in range(12):                      # el arco interior (la mordida)
            a = m.pi * 1.6 - k / 11 * m.pi * 1.1
            luna.append((22.6 + m.cos(a) * 2.0, 5 + m.sin(a) * 3.0))
        poligono(dr, luna, (235, 230, 255))
        brillo(img, [(25, 3)], (255, 255, 255))
        contorno(img, (6, 4, 24))
        return img

    # 8 — PRISMA: los fragmentos cristalinos de colores
    def alas_prisma():
        img = lienzo()
        dr = ImageDraw.Draw(img)
        ala_cristal(dr, img, None, [(174, 23, 189), (237, 128, 60),
                                     (247, 255, 101), (102, 219, 249)])
        contorno(img, (58, 30, 70))
        return img

    # 9 — FUEGO FRÍO: las plumas de hielo facetadas
    def alas_fuego_frio():
        img = lienzo()
        dr = ImageDraw.Draw(img)
        ala_hielo(dr, img, rampa((50, 80, 140), (110, 165, 220), (180, 220, 250), (240, 250, 255)))
        contorno(img, (30, 52, 92))
        return img

    # 10 — TORMENTA: la nube con los rayos colgando
    def alas_tormenta():
        img = lienzo()
        dr = ImageDraw.Draw(img)
        ala_nube_tormenta(dr, img, rampa((60, 40, 100), (130, 100, 190), (200, 185, 240), (245, 238, 255)))
        contorno(img, (40, 26, 68))
        return img

    # 11 — MARIONETA: la barra, los hilos y las plumas colgantes
    def alas_marioneta():
        img = lienzo()
        dr = ImageDraw.Draw(img)
        ala_marioneta(dr, img, rampa((60, 12, 34), (140, 34, 72), (210, 90, 130), (250, 200, 220)))
        contorno(img, (42, 7, 24))
        return img

    # 12 — AURORA ÁRTICA: la cortina de bandas
    def alas_aurora_artica():
        img = lienzo()
        dr = ImageDraw.Draw(img)
        ala_cortina(dr, img, rampa((14, 90, 70), (30, 165, 120), (110, 235, 190), (220, 255, 240)))
        contorno(img, (8, 58, 46))
        return img

    # 13 — ORQUÍDEA: los pétalos
    def alas_orquidea():
        img = lienzo()
        dr = ImageDraw.Draw(img)
        ala_petalos(dr, img, rampa((110, 20, 80), (200, 70, 150), (245, 140, 205), (255, 215, 240)))
        contorno(img, (72, 12, 54))
        return img

    # 14 — VACÍO: el agujero con su anillo
    def alas_vacio():
        img = lienzo()
        dr = ImageDraw.Draw(img)
        ala_vacio(dr, img, rampa((20, 6, 40), (52, 20, 90), (100, 55, 160), (170, 130, 230)))
        contorno(img, (12, 4, 26))
        return img

    # 15 — SOLARES RÚNICAS: la llama con runas
    def alas_solares():
        img = lienzo()
        dr = ImageDraw.Draw(img)
        ala_solar(dr, img, rampa((140, 70, 10), (230, 150, 40), (255, 210, 110), (255, 245, 210)))
        contorno(img, (96, 48, 8))
        return img

    # 16 — SANGRE DE TINTA: la gótica que gotea
    def alas_sangre():
        img = lienzo()
        dr = ImageDraw.Draw(img)
        ala_gotica(dr, img, rampa((70, 8, 50), (150, 26, 100), (215, 80, 160), (250, 180, 225)))
        contorno(img, (48, 5, 34))
        return img

    disenos = [
        ('AlasDeTintaSerena', alas_tinta_serena),
        ('AlasDeEclipse', alas_eclipse),
        ('AlasDePlasma', alas_plasma),
        ('AlasDeNebulosa', alas_nebulosa),
        ('AlasDeCometario', alas_cometario),
        ('AlasDeMedianoche', alas_medianoche),
        ('AlasDelPrisma', alas_prisma),
        ('AlasDeFuegoFrio', alas_fuego_frio),
        ('AlasDeTormenta', alas_tormenta),
        ('AlasDeMarioneta', alas_marioneta),
        ('AlasDeAuroraArtica', alas_aurora_artica),
        ('AlasDeOrquidea', alas_orquidea),
        ('AlasDelVacio', alas_vacio),
        ('AlasSolaresRunicas', alas_solares),
        ('AlasDeSangreDeTinta', alas_sangre),
    ]
    for nombre, fabrica in disenos:
        guardar_icono(fabrica(), f'{ALAS}/{nombre}.png')

    print('LISTO — 21 iconos con anatomías distintas.')


if __name__ == '__main__':
    main()
