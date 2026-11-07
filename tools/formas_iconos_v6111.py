#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
formas_iconos_v6111.py — v6.111 — LA SEGUNDA CURA DE LOS CUADROS PLANOS
+ LOS CINCO SELLOS RENACIDOS.

LA LETRA DEL USUARIO: «la forma ascendida 6 tiene texturas que son
cuadrados planos, debes arreglar eso y ademas crear la regenera y
vuelve a crear la forma ascendida 5, 7, 8, 9, 10».

LA AUTOPSIA DE LA 6 (v6.110 falló): el hexagrama de oro DENTRO del
anillo de tinta es GEOMETRÍA ANGULOSA INSCRITA EN UN CÍRCULO — a 30×30
el ojo lee el CUADRO circunscrito, no la estrella. LA CURA v6.111: la
6 cambia de sello por ALGO HECHO SOLO DE CURVAS — EL HALO DE LOS
GLIFOS (el aura de la forma 6 lo lleva: «10 glifos de escritura viva
orbitando»): un anillo de oro con SUS GLIFOS curvos y el corazón de
pergamino. NINGUNA línea recta larga: nada que pueda leerse a cuadrado.

LOS SEIS SELLOS RENACIDOS (los cinco diseños murieron con sus auras):
  5 — EL RÍO DE PLUMAS: la banda curva de la bandada (plata/teal/violeta)
      con sus tres aves (glints) cabalgando el flujo.
  6 — EL HALO DE LOS GLIFOS (LA CURA): anillo de oro + 7 glifos curvos
      + corazón de pergamino con la pluma (la línea de tinta).
  7 — EL OJO DE LA GALAXIA: el núcleo blanco y LOS DOS BRAZOS
      espirales de oro con su polvo violeta entre ellos.
  8 — LAS OFANIM: la rueda de oro con EL OJO (almendra + iris) y su
      aro interior — la rueda dentro de la rueda.
  9 — EL CÁNTICO DE LAS LÁMPARAS: la copa de oro con su tallo y base,
      la llama ámbar de corazón marfil y su chispa subiendo.
  10 — EL TRONO: EL ARCO ESMERALDA (dos bandas curvas) sobre EL MAR DE
       CRISTAL (las dos líneas del vidrio) con su gota cayendo.

LA RECETA v6.110 INTACTA (trazos gordos 3-4 px, núcleos r>=5, UN solo
anti-alias, silueta inmistakable, transparencia nativa).
"""
import math
import os

import formas_iconos_v6110 as base
from formas_iconos_v6110 import (N, nuevo, px, disco, linea_gorda, arco,
                                 chispa4, DEST)
from PIL import Image

# ------------------------------------------------ LA CURA v6.111: px-max
# LA ENFERMEDAD (la autopsia de los fantasmas): el px de la v6.110
# escribía el borde AA (150) DESPUÉS de que el sello vecino ya había
# puesto el interior OPACO — cada paso del pincel BORRABA el opaco del
# paso anterior (putpixel sin comparar). Resultado: bandas enteras
# quedaban en semi (hasta 45%) = el CUADRO FANTASMA que el usuario ve.
# LA CURA: alpha NUNCA baja — solo sube (comparar y conservar el máximo).
def px(im, x, y, c, a=255):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < N and 0 <= y < N:
        actual = im.getpixel((x, y))
        if a > actual[3]:
            im.putpixel((x, y), (c[0], c[1], c[2], a))


def disco(im, cx, cy, r, c, corazon=None, r_corazon=None):
    for y in range(N):
        for x in range(N):
            d = math.hypot(x - cx, y - cy)
            if d < r - 0.7:
                px(im, x, y, corazon if (corazon and d < r_corazon) else c)
            elif d < r + 0.45:
                px(im, x, y, corazon if (corazon and d < r_corazon) else c, 150)


def linea_gorda(im, x0, y0, x1, y1, c, grosor=3, alma=None):
    pasos = int(max(abs(x1 - x0), abs(y1 - y0)) * 2.5) + 1
    for s in range(pasos):
        tt = s / (pasos - 1)
        x = x0 + (x1 - x0) * tt
        y = y0 + (y1 - y0) * tt
        half = (grosor - 1) / 2
        for dy in range(-int(half) - 1, int(half) + 2):
            for dx in range(-int(half) - 1, int(half) + 2):
                d = math.hypot(dx, dy)
                if d <= half:
                    px(im, x + dx, y + dy, alma if (alma and d < half - 0.8) else c)
                elif d <= half + 0.75:
                    px(im, x + dx, y + dy, c, 150)


def arco(im, cx, cy, r, a0, a1, c, grosor=3, alma=None, squish=1.0):
    pasos = int(abs(a1 - a0) * r * 1.6) + 2
    for s in range(pasos):
        tt = s / (pasos - 1)
        th = a0 + (a1 - a0) * tt
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


# ----------------------------------------------------- paletas v6.111
# 7 — LA GALAXIA: la estrella heredó su oro y SU coral (las puntas)
VIOLETA_POLVO = (150, 108, 220)
# 8 — LAS OFANIM: el platino de la casa
PLATINO_ALMA = (248, 250, 255)
ORO = (255, 208, 96)
# 9 — LA LÁMPARA: el ámbar del coro
AMBAR = (255, 214, 140)
# 10 — EL TRONO: el cian cristal y la esmeralda del arco
ESMERALDA = (110, 235, 190)
CIAN = (190, 245, 250)

# ----------------------------------------------------------------- sellos
def sello5(im):
    """EL RÍO DE PLUMAS — v2 (EL OJO: «mancha curva» -> UN solo río
    GORDO y legible + el núcleo + sus aves cabalgando EN el río)."""
    # EL RÍO: UN arco GORDO (7 px) — la media banda inferior, clara.
    arco(im, 15, 6, 15.5, math.radians(55), math.radians(125),
         base.TEAL, 7, base.TEAL_CLARO)
    # LA CONTRA-CORRIENTE: un arco corto y GRUESO arriba (la mitad que
    # nada al revés — separado del río, SIN mezclarse).
    arco(im, 15, 25, 14.0, math.radians(235), math.radians(305),
         base.VIOLETA, 5)
    # LA PLATA VIVA: el núcleo GORDO en el CRUCE de ambos.
    disco(im, 15, 15, 8.2, base.PLATA, base.PLATA_ALMA, 4.0)
    # LAS TRES AVES: glints GRANDES cabalgando el río (SOBRE la banda).
    disco(im, 22, 14.5, 3.2, base.PLATA_ALMA)
    disco(im, 8, 15.5, 3.2, base.TEAL_CLARO)
    disco(im, 15, 5.5, 3.0, base.VIOLETA)


def sello6(im):
    """EL HALO DE LOS GLIFOS (LA CURA v6.111) — solo CURVAS."""
    # EL ANILLO DE ORO (gordo: el halo macizo).
    arco(im, 15, 15, 10.4, 0, math.tau, base.ORO, 5, base.ORO_HONDO)
    # EL CORAZÓN DE PERGAMINO (la página GORDA del escriba).
    disco(im, 15, 15, 6.2, base.PERGAMINO, (255, 255, 250), 3.0)
    # LOS SIETE GLIFOS: marcas curvas GORDAS sobre el anillo.
    for k in range(7):
        th = math.tau * k / 7.0 + 0.22
        gx, gy = 15 + math.cos(th) * 10.4, 15 + math.sin(th) * 10.4
        tang = th + math.pi / 2
        arco(im, gx, gy, 2.4, tang - 1.2, tang + 1.2, base.PERGAMINO, 3)
    # LA PLUMA: la rúbrica de tinta (curva GORDA).
    arco(im, 15, 15, 4.2, math.radians(150), math.radians(30),
         base.TINTA, 3)
    disco(im, 18.4, 17.6, 2.0, base.TINTA)


def sello7(im):
    """EL OJO DE LA GALAXIA — v3 (EL OJO r2: «mancha confusa» -> UN
    SOLO BRAZO envolvente: la lectura de huracán — un núcleo GORDO y
    SU brazo que lo envuelve casi vuelta y media, abriéndose)."""
    # EL NÚCLEO (blanco de corazón oro, GORDO).
    disco(im, 15, 15, 7.6, base.NUCLEAR, base.ORO_VIVO, 3.8)
    # EL BRAZO ÚNICO: cadena de discos GRANDES que envuelve el núcleo
    # (r 8.2 -> 13.6 en ~1.45 vueltas — la espiral de huracán).
    pasos = 9
    for s in range(pasos):
        u = s / (pasos - 1)
        th = math.radians(-90) + u * math.radians(520)
        r = 8.2 + 5.4 * u
        x, y = 15 + math.cos(th) * r, 15 + math.sin(th) * r
        c = base.CORAL if u >= 0.8 else base.ORO_VIVO
        disco(im, x, y, 4.4 - 1.0 * u, c)
        disco(im, x, y, 1.9 - 0.4 * u, base.NUCLEAR)
    # EL POLVO (las motas violetas en el exterior, lejos del brazo).
    disco(im, 26, 24, 2.4, VIOLETA_POLVO)
    disco(im, 4, 26, 2.2, base.CORAL_HONDO)


def sello8(im):
    """LAS OFANIM — la rueda de oro con EL OJO y su aro interior."""
    # LA RUEDA (el anillo GORDO de oro).
    arco(im, 15, 15, 11.0, 0, math.tau, base.ORO_VEREDICTO, 5, PLATINO_ALMA)
    # EL ARO INTERIOR (la rueda dentro de la rueda — violeta).
    arco(im, 15, 15, 4.8, 0, math.tau, base.VIOLETA_REAL, 3)
    # EL OJO (la almendra: dos arcos GORDOS que se encuentran).
    arco(im, 15, 17.4, 6.4, math.radians(198), math.radians(342),
         base.PLATINO, 4)                       # el párpado de arriba
    arco(im, 15, 12.6, 6.4, math.radians(18), math.radians(162),
         base.PLATINO, 4)                       # el párpado de abajo
    # EL IRIS (el oro que mira) y su pupila blanca.
    disco(im, 15, 15, 3.4, base.ORO_VEREDICTO, PLATINO_ALMA, 1.7)
    # LAS CUATRO MARCAS de la llanta (los dientes de la rueda).
    for k in range(4):
        th = math.tau * k / 4.0 + math.pi / 4
        mx, my = 15 + math.cos(th) * 11.0, 15 + math.sin(th) * 11.0
        disco(im, mx, my, 2.2, base.PLATINO)


def sello9(im):
    """EL CÁNTICO DE LAS LÁMPARAS — v2 (EL OJO: «llama se mezcla con el
    halo» -> el halo rosa MUERE; la llama crece y SU corazón marfil)."""
    # EL TALLO y LA BASE (el pie de la lámpara, GORDO).
    linea_gorda(im, 15, 20, 15, 25, base.VIOLETA_MELODIA, 4)
    linea_gorda(im, 8, 26.5, 22, 26.5, base.VIOLETA_MELODIA, 5)
    # LA COPA (el cuenco: un arco MUY GORDO que abre arriba).
    arco(im, 15, 21.5, 8.0, math.radians(22), math.radians(158),
         ORO, 7, base.ORO_HONDO)
    # LA LLAMA (la gota viva: cuerpo ámbar ENORME, corazón marfil).
    disco(im, 15, 9.5, 6.6, AMBAR, base.MARFIL, 3.4)
    disco(im, 15, 5.2, 4.0, base.MARFIL)
    # LA CHISPA que sube (el humo de la alabanza — lejos de la llama).
    disco(im, 23, 6.5, 2.0, base.ROSA_CLARO)
    disco(im, 7, 5.0, 1.8, base.ROSA_CLARO)


def sello10(im):
    """EL TRONO — EL ARCO ESMERALDA sobre EL MAR DE CRISTAL."""
    # EL ARCO (dos bandas GORDAS: hondo afuera, esmeralda adentro).
    arco(im, 15, 19, 12.4, math.radians(180), math.radians(360),
         base.VIOLETA_ARBOL, 4)
    arco(im, 15, 19, 9.6, math.radians(180), math.radians(360),
         ESMERALDA, 5)
    # EL MAR DE CRISTAL (las líneas GORDAS del vidrio).
    linea_gorda(im, 3, 23, 27, 23, ESMERALDA, 5, CIAN)
    linea_gorda(im, 6, 27, 24, 27, CIAN, 3)
    # EL DESTELLO del encuentro (donde el arco toca el mar).
    disco(im, 15, 19.5, 3.2, base.BLANCO_CORONA)
    # LAS GOTAS (las lágrimas del arco cayendo al mar).
    disco(im, 20, 13.5, 2.2, CIAN)
    disco(im, 10, 15.5, 2.0, CIAN)
    disco(im, 15, 8.0, 1.8, base.ORO_CORONA)


# ------------------------------------------------------------------ main
SELOS = {
    'FormaAscendidaCincoItem.png': sello5,
    'FormaAscendidaSeisItem.png': sello6,
    'FormaAscendidaSieteItem.png': sello7,
    'FormaAscendidaOchoItem.png': sello8,
    'FormaAscendidaNueveItem.png': sello9,
    'FormaAscendidaDiezItem.png': sello10,
}

def main():
    os.makedirs(DEST, exist_ok=True)
    for nombre, fn in SELOS.items():
        im = nuevo()
        fn(im)
        ruta = os.path.join(DEST, nombre)
        im.save(ruta)
        # LA ESTADÍSTICA DE LA CASA: % de píxeles OPACOS (los aprobados
        # 1-4: 34-38%; la v6.110 fallida: 12-21% — el fantasma cuadrado).
        alfa = [im.getpixel((x, y))[3] for y in range(N) for x in range(N)]
        opacos = sum(1 for a in alfa if a > 200) / (N * N)
        semis = sum(1 for a in alfa if 40 < a <= 200) / (N * N)
        print(f'{nombre}: {opacos:.0%} opaco · {semis:.0%} semi '
              f'{"OK" if opacos >= 0.30 else "¡FANTASMA!"}')

if __name__ == '__main__':
    main()
