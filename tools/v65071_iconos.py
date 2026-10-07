#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.50.71 — LOS ÚTILES DEL ESCRIBA: los 3 iconos pixel-art 38x38.

La familia de tentáculos .69 (Azote/Mordida/Cría) quedó JUBILADA — «las
3 nuevas armas se ven mal» — y su sitio lo toman LOS ÚTILES DEL ESCRIBA.
Las lecciones VLM de la .68/.69 siguen mandando: SILUETA GRUESA ante
todo (a 30px de inventario el detalle fino se lo come el ojo), el
BLANCO DEL HUESO prende sobre cualquier fondo, el rim VIOLETA de 1px
lee la masa negra, el ROJO VISCERAL es la firma de acento de la casa.

Los 3 conceptos (cada uno con su gesto inconfundible):
  LA MANO DEL ESCRIBA — la garra que avanza hacia el espectador: palma
  redonda y grande con EL OJO RASGADO en el centro (el gesto del arma),
  4 dedos fusiformes (finos-gordos-finos) con PUNTAS DE HUESO en
  abanico + el pulgar lateral gordo.
  LAS TIJERAS DE LA PÁGINA — la X abierta: dos hojas curvas de sombra
  (finas en el gozne, gordas al centro, afiladas a la punta) con el
  CANTO INTERIOR BLANCO —el filo de hueso— y el REMACHE violeta vivo
  en el cruce; las dos anillas abajo cierran la lectura.
  LA PÁGINA ARRANCADA — el marco rectangular de sombra (4 barras
  gruesas) que encierra algo oscuro: RENGLONES violeta tenues, la
  línea de MARGEN ROJO vertical y la ESQUINA ARRANCADA con su zigzag
  blanco — el desgarro de fibra de la hoja.

Paleta de la familia (la de SombraDeLaPagina): masas #11071a..#36104c,
rim/energia violeta #8409be..#d927f6, hueso #f6ebf5, rojo #c61218.
PIL puro, determinista, cero azar.
"""
from PIL import Image
import math
import os

W = H = 38

# --- LA PALETA DE LA FAMILIA ---
MASA         = (17, 7, 26, 255)     # #11071a — la sombra mas profunda
CUERPO       = (30, 13, 48, 255)    # #1e0d30 — el cuerpo de la sombra
LUMEN        = (54, 16, 76, 255)    # #36104c — el volumen alto
VIOLETA      = (132, 9, 190, 255)   # #8409be — el rim que lee la masa
VIOLETA_VIVO = (217, 39, 246, 255)  # #d927f6 — la energia que brilla
HUESO        = (246, 235, 245, 255) # #f6ebf5 — el blanco hueso
HUESO_SOMBRA = (206, 188, 216, 255) # la transicion del hueso
ROJO         = (198, 18, 24, 255)   # #c61218 — el acento visceral


def lienzo():
    return Image.new("RGBA", (W, H), (0, 0, 0, 0))


def px(im, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H:
        im.putpixel((x, y), c)


def getpx(im, x, y):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H:
        return im.getpixel((x, y))
    return (0, 0, 0, 0)


def disco(im, cx, cy, r, c):
    for y in range(max(0, int(cy - r - 1)), min(H, int(cy + r + 2))):
        for x in range(max(0, int(cx - r - 1)), min(W, int(cx + r + 2))):
            if math.hypot(x - cx, y - cy) <= r + 0.35:
                im.putpixel((x, y), c)


def agujero(im, cx, cy, r):
    """el disco transparente (los ojos de las anillas)"""
    for y in range(max(0, int(cy - r - 1)), min(H, int(cy + r + 2))):
        for x in range(max(0, int(cx - r - 1)), min(W, int(cx + r + 2))):
            if math.hypot(x - cx, y - cy) <= r + 0.35:
                im.putpixel((x, y), (0, 0, 0, 0))


def elipse(im, cx, cy, rx, ry, c):
    for y in range(H):
        for x in range(W):
            dx, dy = (x - cx) / rx, (y - cy) / ry
            if dx * dx + dy * dy <= 1.0:
                im.putpixel((x, y), c)


def cadena(im, p0, pc, p1, rperfil, cf, n=None):
    """LA QUIJADA DE LA CASA: cadena de discos sobre una bezier
    cuadratica, con perfil de radio rperfil(t) — el fuso
    (fina en la base, gorda al nudillo, fina otra vez). cf puede ser
    color fijo o funcion cf(t) (para las puntas de hueso)."""
    (x0, y0), (xc, yc), (x1, y1) = p0, pc, p1
    lon = math.hypot(x1 - x0, y1 - y0)
    n = n or max(12, int(lon * 2.2))
    pts = []
    for i in range(n + 1):
        t = i / n
        x = (1 - t) ** 2 * x0 + 2 * (1 - t) * t * xc + t ** 2 * x1
        y = (1 - t) ** 2 * y0 + 2 * (1 - t) * t * yc + t ** 2 * y1
        pts.append((x, y))
    for i, (x, y) in enumerate(pts):
        t = i / n
        col = cf(t) if callable(cf) else cf
        disco(im, x, y, rperfil(t), col)
    return pts


def bezier(p0, pc, p1, t):
    (x0, y0), (xc, yc), (x1, y1) = p0, pc, p1
    return ((1 - t) ** 2 * x0 + 2 * (1 - t) * t * xc + t ** 2 * x1,
            (1 - t) ** 2 * y0 + 2 * (1 - t) * t * yc + t ** 2 * y1)


def es_masa(p):
    return p[3] > 0 and (p[0] + p[1] + p[2]) <= 200


def rim(im):
    """EL RIM VIOLETA de 1px: todo pixel vacio pegado a la masa oscura.
    Es lo que lee la silueta negra sobre fondos claros y oscuros."""
    marca = []
    for y in range(H):
        for x in range(W):
            if im.getpixel((x, y))[3] == 0:
                for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1),
                               (-1, -1), (1, -1), (-1, 1), (1, 1)):
                    if es_masa(getpx(im, x + dx, y + dy)):
                        marca.append((x, y))
                        break
    for (x, y) in marca:
        im.putpixel((x, y), VIOLETA)


def almendra(im, cx, cy, rx, ry, ang, c):
    """el ojo rasgado: elipse rotada (la comisura sube)"""
    ca, sa = math.cos(ang), math.sin(ang)
    for y in range(H):
        for x in range(W):
            dx, dy = x - cx, y - cy
            ux = ca * dx + sa * dy
            uy = -sa * dx + ca * dy
            if (ux / rx) ** 2 + (uy / ry) ** 2 <= 1.0:
                im.putpixel((x, y), c)


# ======================================================================
#  1. LA MANO DEL ESCRIBA — la garra que avanza hacia el espectador
# ======================================================================
def gen_mano():
    im = lienzo()

    # LA MUNECA — nace del borde bajo y se pierde fuera de cuadro:
    # la garra VIENE HACIA TI (avanza sobre sus dedos, como arana)
    cadena(im, (19, 37), (19, 33), (19, 28), lambda t: 4.2, MASA)

    # LA PALMA — la masa redonda y grande, con el peso abajo
    elipse(im, 19, 23, 9.5, 8.2, CUERPO)
    elipse(im, 21, 27, 5.5, 3.6, MASA)

    # LOS NUDILLOS — los bultos claros de donde nacen los dedos
    for (bx, by) in [(12, 17.5), (16, 15.5), (20, 15), (24, 16.5)]:
        disco(im, bx, by, 1.7, LUMEN)

    # LOS 4 DEDOS FUSIFORMES — finos en la palma, engrosados al
    # nudillo, finos otra vez: el abanico que se abre sobre la presa
    dedos = [
        ((12, 17.5), (6.5, 11.5), (3.5, 4.5)),  # el indice, el mas abierto
        ((16, 15.5), (13.5, 8), (10.5, 1.5)),
        ((20, 15), (19.5, 8), (19, 0.8)),       # el corazon, el mas largo
        ((24, 16.5), (27.5, 9.5), (28.5, 2.5)),
    ]
    fusor = lambda t: 0.7 + 1.2 * math.sin(math.pi * t) ** 0.75
    for (p0, pc, p1) in dedos:
        cadena(im, p0, pc, p1, fusor, CUERPO)

    # EL PULGAR — lateral, gordo y corto, el opuesto que cierra el gesto
    pulgar = ((27, 25), (31.5, 23.5), (35, 19.5))
    cadena(im, pulgar[0], pulgar[1], pulgar[2],
           lambda t: 1.0 + 1.1 * math.sin(math.pi * t) ** 0.8, CUERPO)

    rim(im)

    # LAS PUNTAS DE HUESO — el abanico blanco que se abre (post-rim:
    # el blanco prende sobre cualquier fondo, no necesita borde)
    hueso_dedo = lambda t: (HUESO if t >= 0.80 else
                            HUESO_SOMBRA if t >= 0.72 else CUERPO)
    for (p0, pc, p1) in dedos:
        cadena(im, p0, pc, p1, fusor, hueso_dedo)
    cadena(im, pulgar[0], pulgar[1], pulgar[2],
           lambda t: 1.0 + 1.1 * math.sin(math.pi * t) ** 0.8,
           lambda t: (HUESO if t >= 0.76 else
                      HUESO_SOMBRA if t >= 0.68 else CUERPO))

    # EL OJO DE LA PALMA — EL GESTO DEL ARMA: el ojo rasgado que no
    # parpadea (rim violeta + escleroteca de hueso + pupila roja)
    almendra(im, 19, 22.5, 5.5, 2.7, -0.30, VIOLETA)
    almendra(im, 19, 22.5, 4.6, 2.0, -0.30, HUESO)
    disco(im, 19.8, 22.6, 1.35, ROJO)

    # LOS ACENTOS — la energia en los nudillos y el aranazo visceral
    px(im, 16, 13, VIOLETA_VIVO)
    px(im, 22, 13, VIOLETA_VIVO)
    px(im, 13, 26, ROJO)
    px(im, 14, 27, ROJO)
    return im


# ======================================================================
#  2. LAS TIJERAS DE LA PAGINA — la X abierta, el filo de hueso
# ======================================================================
def gen_tijeras():
    im = lienzo()
    piv = (19, 19.3)

    # EL PERFIL DE LA HOJA — fina en el gozne, gorda al centro,
    # afilada a la punta de aguja (el mismo fuso de los dedos)
    def filo(t):
        r = 0.85 + 1.5 * math.sin(math.pi * t) ** 0.8
        if t > 0.85:                       # la punta se cierra en aguja
            r *= 1.0 - (t - 0.85) * 3.0
        return r

    # PIEZA A: anilla abajo-izquierda -> gozne -> punta arriba-derecha
    # PIEZA B: la espeja. Las dos se CRUZAN en el remache: la X abierta
    cadena(im, (13.1, 28.4), (15.8, 24), piv, lambda t: 1.35, CUERPO)
    cadena(im, piv, (25.6, 10.6), (29, 3.6), filo, CUERPO)
    cadena(im, (24.9, 28.4), (22.2, 24), piv, lambda t: 1.35, CUERPO)
    cadena(im, piv, (12.4, 10.6), (9, 3.6), filo, CUERPO)

    # LAS ANILLAS — los ojos de los tiradores (donut con agujero)
    for (ax, ay) in [(11.3, 31.3), (26.7, 31.3)]:
        disco(im, ax, ay, 3.4, CUERPO)
        agujero(im, ax, ay, 1.8)

    rim(im)

    # EL CANTO INTERIOR BLANCO — el filo de hueso: la linea que corre
    # por el borde de cada hoja MIRANDO a la otra (por donde muerde)
    for (p0, pc, p1) in [(piv, (25.6, 10.6), (29, 3.6)),
                         (piv, (12.4, 10.6), (9, 3.6))]:
        (x0, y0), (xc, yc), (x1, y1) = p0, pc, p1
        n = 40
        for i in range(n + 1):
            t = i / n
            if not (0.10 <= t <= 0.95):
                continue
            x, y = bezier(p0, pc, p1, t)
            dx = 2 * (1 - t) * (xc - x0) + 2 * t * (x1 - xc)
            dy = 2 * (1 - t) * (yc - y0) + 2 * t * (y1 - yc)
            L = math.hypot(dx, dy) or 1.0
            dx, dy = dx / L, dy / L
            ix, iy = dy, -dx           # el lado que mira al eje
            r = filo(t)
            c = HUESO if t >= 0.30 else HUESO_SOMBRA
            px(im, x + ix * (r - 0.45), y + iy * (r - 0.45), c)

    # EL REMACHE — el ojo violeta vivo del cruce (donde la pagina muerde)
    disco(im, piv[0], piv[1], 3.0, MASA)
    for y in range(H):
        for x in range(W):
            d = math.hypot(x - piv[0], y - piv[1])
            if 2.0 < d <= 3.0:
                px(im, x, y, VIOLETA)
    disco(im, piv[0], piv[1], 1.3, VIOLETA_VIVO)

    # LOS ACENTOS — la tension del cruce y los tiradores que sangran
    px(im, 17, 17, ROJO)
    px(im, 11, 35, ROJO)
    px(im, 27, 35, ROJO)
    return im


# ======================================================================
#  3. LA PAGINA ARRANCADA — el marco, los renglones, el desgarro
# ======================================================================
def gen_pagina():
    im = lienzo()
    x0, y0, x1, y1 = 5, 4, 32, 33
    grosor = 3

    # EL MARCO — las 4 barras gruesas de la hoja (la sombra que encuadra)
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            en_barra = (y < y0 + grosor or y > y1 - grosor or
                        x < x0 + grosor or x > x1 - grosor)
            if en_barra:
                im.putpixel((x, y), CUERPO)

    # la luz de la hoja: el canto superior e izquierdo mas altos
    for x in range(x0, x1 + 1):
        im.putpixel((x, y0), LUMEN)
    for y in range(y0, y1 + 1):
        im.putpixel((x0, y), LUMEN)

    # EL VACIO — lo oscuro que la pagina encierra (la sombra escrita)
    for y in range(y0 + grosor, y1 - grosor + 1):
        for x in range(x0 + grosor, x1 - grosor + 1):
            im.putpixel((x, y), MASA)

    # el canto interior del marco — la linea LUMEN que separa las
    # barras del vacio: es lo que hace leer EL MARCO a 30px
    for x in range(x0 + grosor, x1 - grosor + 1):
        im.putpixel((x, y0 + grosor), LUMEN)
        im.putpixel((x, y1 - grosor), LUMEN)
    for y in range(y0 + grosor, y1 - grosor + 1):
        im.putpixel((x0 + grosor, y), LUMEN)
        im.putpixel((x1 - grosor, y), LUMEN)

    # LOS RENGLONES — violeta tenues, rayados a trazo discontinuo
    for ry in (10, 13, 16, 19, 22, 25, 28):
        for x in range(9, 29):
            if (x - 9) % 4 != 3:
                im.putpixel((x, ry), VIOLETA)

    # LA MARGEN — la linea roja vertical del cuaderno
    for y in range(y0 + grosor, y1 - grosor + 1):
        im.putpixel((11, y), ROJO)

    # EL DESGARRO — la esquina inferior-derecha ARRANCADA: para cada
    # fila, todo lo que quede mas alla del tajo zigzagueante desaparece
    tajo = {33: 24, 32: 26, 31: 25, 30: 28, 29: 27, 28: 30,
            27: 29, 26: 32}
    for y, tx in tajo.items():
        for x in range(tx, W):
            im.putpixel((x, y), (0, 0, 0, 0))

    rim(im)

    # EL ZIGZAG BLANCO — la fibra del desgarro: el borde de hueso de la
    # hoja rota (2px en los dientes, la huella del arrancon)
    for y, tx in tajo.items():
        px(im, tx - 1, y, HUESO)
        if y % 2 == 1:
            px(im, tx - 2, y, HUESO)

    # LOS ACENTOS — la energia que se escapa por donde se rompio
    px(im, 26, 28, VIOLETA_VIVO)
    px(im, 23, 32, VIOLETA_VIVO)
    return im


# --- EL VOLCATO ASCII (verificacion a nivel de matriz) ---
def volcado(im, titulo):
    print("--- " + titulo)
    tabla = {MASA[:3]: "#", CUERPO[:3]: "x", LUMEN[:3]: "l",
             VIOLETA[:3]: "v", VIOLETA_VIVO[:3]: "V", HUESO[:3]: "W",
             HUESO_SOMBRA[:3]: "w", ROJO[:3]: "R"}
    for y in range(H):
        fila = []
        for x in range(W):
            p = im.getpixel((x, y))
            fila.append("." if p[3] == 0 else tabla.get(p[:3], "?"))
        print("".join(fila))


if __name__ == "__main__":
    base = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                        "..", "AethonMod", "Content", "Weapons", "Sombras")
    tmp = "/tmp/iconos71"
    os.makedirs(tmp, exist_ok=True)

    iconos = [
        ("ManoDelEscriba.png", gen_mano),
        ("TijerasDeLaPagina.png", gen_tijeras),
        ("PaginaArrancada.png", gen_pagina),
    ]
    for nombre, fn in iconos:
        im = fn()
        assert im.size == (38, 38) and im.mode == "RGBA"
        im.save(os.path.join(base, nombre))
        im.save(os.path.join(tmp, nombre))
        im.resize((38 * 8, 38 * 8), Image.NEAREST).save(
            os.path.join(tmp, nombre.replace(".png", "_x8.png")))
        volcado(im, nombre)
        print(nombre, "OK")

    # LA HOJA DE CONTACTO — SombraDeLaPagina (la aprobada) + los 3
    # nuevos, lado a lado x6, para la verificacion de familia
    ref = Image.open(os.path.join(base, "SombraDeLaPagina.png")).convert("RGBA")
    imgs = [ref] + [fn() for _, fn in iconos]
    esc, sep = 6, 14
    hoja = Image.new("RGBA", (38 * esc * 4 + sep * 5, 38 * esc + sep * 2),
                     (40, 40, 46, 255))
    for k, im in enumerate(imgs):
        grande = im.resize((38 * esc, 38 * esc), Image.NEAREST)
        hoja.paste(grande, (sep + k * (38 * esc + sep), sep), grande)
    hoja.save(os.path.join(tmp, "contact.png"))
    print("hoja de contacto OK ->", os.path.join(tmp, "contact.png"))
