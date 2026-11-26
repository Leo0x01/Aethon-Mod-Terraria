#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_visor_v6177.py — LOS SPRITES DEL VISOR DEL PROYECTADO (v6.177).

La letra: «el arma del ciclope no tiene nada que ver te voy a dar el
archivo .tmod del ciclope proyectado» — la recreación 100% de la casa
(sprites por código, cero material ajeno) del arma del meme:
EL VISOR DEL PROYECTADO — un visor con UNA lente magenta que dispara
el rayo óptico rojo, y las DIEZ MEJORAS de los jefes (la carta que
nunca le diste, la captura del visto, el «no eres tú, soy yo», los
tres proyectores, las flores amarillas, el «ya lo superé», el «hay
más peces» y la luna que te iba a bajar).

Estilo de la casa: silueta + rampa de paleta + brillo de punta
(píxel duro, NADA de Lanczos — la lección de las dagas v6.128).
"""
import os
from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(__file__), '..', 'AethonMod',
                   'Content', 'Items', 'Investigaciones')
os.makedirs(OUT, exist_ok=True)


def nuevo(w, h):
    img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def guardar(img, nombre):
    ruta = os.path.join(OUT, nombre + '.png')
    img.save(ruta)
    print(f'  {nombre}.png {img.size[0]}x{img.size[1]}')


# ════════════════════════════════════════════════════════════════════
#  EL VISOR — 48×20: la banda pálida con SU lente magenta y los broches
# ════════════════════════════════════════════════════════════════════
def visor():
    img, d = nuevo(48, 20)
    # LA BANDA: marfil hueso con sombra inferior (tres tonos)
    banda = [(236, 226, 206), (206, 192, 168), (168, 152, 128)]
    for x in range(6, 42):
        # la banda es curva: sube en el centro
        y0 = 4 + (1 if 14 <= x <= 33 else 0)
        for y in range(y0, y0 + 10):
            tono = 0 if y <= y0 + 3 else (1 if y <= y0 + 7 else 2)
            d.point((x, y), fill=banda[tono])
    # los EXTREMOS: las patillas que abrazan la cabeza
    for x in range(2, 6):
        for y in range(6, 13):
            d.point((x, y), fill=banda[1])
    for x in range(42, 46):
        for y in range(6, 13):
            d.point((x, y), fill=banda[1])
    # EL CONTORNO de la casa (tinta azul-noche)
    tinta = (16, 20, 46, 255)
    for x in range(6, 42):
        d.point((x, 4 + (1 if 14 <= x <= 33 else 0)), fill=tinta)
        d.point((x, 14 + (1 if 14 <= x <= 33 else 0) - 1 + 6), fill=tinta)
    # LA LENTE: el ojo magenta del cíclope (en el centro, GRANDE)
    cx, cy = 24, 9
    magenta_rampa = [(255, 120, 200), (232, 62, 160), (168, 20, 110), (96, 8, 64)]
    for y in range(5, 14):
        for x in range(16, 32):
            dx, dy = x - cx, y - cy
            r2 = dx * dx + dy * dy * 1.35
            if r2 <= 40:
                tono = 0 if r2 < 8 else (1 if r2 < 22 else 2)
                d.point((x, y), fill=magenta_rampa[tono])
            elif r2 <= 52:
                d.point((x, y), fill=tinta)
    # el BRILLO de punta: la chispa blanca del ojo (arriba-izquierda)
    d.point((21, 7), fill=(255, 240, 250, 255))
    d.point((22, 7), fill=(255, 240, 250, 255))
    d.point((21, 8), fill=(255, 210, 240, 255))
    # LOS BROCHES dorados a los lados de la lente
    oro = (255, 200, 80, 255)
    for x, y in [(14, 7), (14, 12), (33, 7), (33, 12)]:
        d.point((x, y), fill=oro)
    # la CORREA superior: la cinta que ciñe (línea oscura arriba)
    for x in range(8, 40, 2):
        d.point((x, 3), fill=(52, 40, 66, 255))
    guardar(img, 'VisorDelProyectado')


# ════════════════════════════════════════════════════════════════════
#  LAS DIEZ MEJORAS — 30×30 cada una (píxel duro de la casa)
# ════════════════════════════════════════════════════════════════════
def carta():
    """La carta que nunca le diste: el sobre arrugado con su sello."""
    img, d = nuevo(30, 30)
    papel = [(238, 224, 196), (208, 188, 152), (160, 140, 108)]
    tinta = (16, 20, 46, 255)
    for y in range(8, 23):
        for x in range(5, 25):
            tono = 0 if y < 12 else (1 if y < 19 else 2)
            d.point((x, y), fill=papel[tono])
    # el PLIEGUE del sobre (la V central)
    for i in range(10):
        x = 6 + i
        d.point((x, 8 + i), fill=tinta)
        d.point((x, 9 + i), fill=papel[2])
        d.point((29 - 4 - i, 8 + i), fill=tinta)
        d.point((29 - 4 - i, 9 + i), fill=papel[2])
    # el contorno
    for x in range(5, 25):
        d.point((x, 8), fill=tinta); d.point((x, 22), fill=tinta)
    for y in range(8, 23):
        d.point((5, y), fill=tinta); d.point((24, y), fill=tinta)
    # el SELLO de lacre rojo — la promesa que nunca se envió
    for y in range(14, 18):
        for x in range(13, 17):
            d.point((x, y), fill=(168, 30, 40, 255) if (x + y) % 3 else (210, 60, 70, 255))
    # el ARRUGÓN: la línea del doblez en la mochila
    for x in range(8, 22, 3):
        d.point((x, 20), fill=(120, 100, 74, 255))
    guardar(img, 'CartaQueNuncaLeDiste')


def captura():
    """La captura del visto: el celular negro con su doble check."""
    img, d = nuevo(30, 30)
    tinta = (16, 20, 46, 255)
    cuerpo = [(38, 38, 52), (26, 26, 38)]
    for y in range(4, 26):
        for x in range(9, 21):
            tono = 0 if x in (10, 11, 18, 19) or y in (5, 6) else 1
            d.point((x, y), fill=cuerpo[tono if tono < 2 else 1])
    # contorno
    for y in range(4, 26):
        d.point((9, y), fill=tinta); d.point((20, y), fill=tinta)
    for x in range(9, 21):
        d.point((x, 4), fill=tinta); d.point((x, 25), fill=tinta)
    # la PANTALLA: la burbuja y el «visto» a las 2:47 a.m.
    for y in range(9, 18):
        for x in range(11, 19):
            d.point((x, y), fill=(18, 18, 28, 255))
    # la burbuja de texto (violeta de la casa)
    for y in range(10, 13):
        for x in range(12, 18):
            d.point((x, y), fill=(148, 72, 178, 255))
    # EL DOBLE CHECK — leído (azul): la herida
    for i in range(3):
        d.point((13 + i, 15 + i), fill=(80, 160, 255, 255))
        d.point((16 + i, 14 + i), fill=(80, 160, 255, 255))
    d.point((16, 17), fill=(80, 160, 255, 255))
    d.point((19, 16), fill=(80, 160, 255, 255))
    d.point((20, 15), fill=(80, 160, 255, 255))
    guardar(img, 'CapturaDelVisto')


def noeres():
    """El «no eres tú, soy yo»: el corazón partido a medias."""
    img, d = nuevo(30, 30)
    gris = [(120, 110, 130), (90, 82, 100), (62, 56, 72)]
    tinta = (16, 20, 46, 255)
    pts = []
    for y in range(6, 24):
        for x in range(5, 25):
            # silueta de corazón clásico
            dx = (x - 15) / 10.0
            dy = (y - 14) / 10.0
            if dy <= 0.9 and (dx * dx + dy * dy - 0.55) ** 3 - dx * dx * dy ** 3 < 0:
                pts.append((x, y))
    for (x, y) in pts:
        mitad = x < 15
        grieta = abs(x - 15) <= (y % 4 == 0 and 0)
        tono = 0 if y < 12 else (1 if y < 19 else 2)
        if y == 23 or (x in (6, 24)) or y == 6:
            d.point((x, y), fill=tinta)
        else:
            d.point((x, y), fill=gris[tono])
    # LA GRIETA: el corte limpio que separa las mitades
    for y in range(6, 24):
        if y > 7:
            d.point((15, y), fill=(0, 0, 0, 0))
            d.point((14, y), fill=tinta)
            d.point((16, y), fill=tinta)
    # el vacío de la grieta (2px transparentes — partido de verdad)
    guardar(img, 'NoEresTuSoyYo')


def proyector(tam, lente, nombre):
    """Los TRES proyectores (salón · cine · IMAX): mismo linaje, tamaños y lentes."""
    img, d = nuevo(30, 30)
    tinta = (16, 20, 46, 255)
    grises = [(116, 122, 140), (82, 86, 102), (56, 58, 72)]
    if tam == 'salon':
        x0, x1, y0, y1 = 8, 24, 14, 22      # el chico sobre su carrito
        carretes = False
        ruedas = True
    elif tam == 'cine':
        x0, x1, y0, y1 = 6, 25, 10, 22
        carretes = True
        ruedas = False
    else:
        x0, x1, y0, y1 = 4, 27, 9, 23       # EL IMAX: el grande
        carretes = True
        ruedas = False
    for y in range(y0, y1):
        for x in range(x0, x1):
            tono = 0 if y == y0 or x == x0 else (1 if y < y1 - 2 else 2)
            d.point((x, y), fill=grises[tono])
    for x in range(x0, x1):
        d.point((x, y0), fill=tinta); d.point((x, y1 - 1), fill=tinta)
    for y in range(y0, y1):
        d.point((x0, y), fill=tinta); d.point((x1 - 1, y), fill=tinta)
    # LA LENTE: el cañón de la luz
    lx = x1 - 1
    lh = (y1 - y0) if tam == 'imax' else max(5, (y1 - y0) - 2)
    ly = (y0 + y1) // 2 - lh // 2
    for y in range(ly, ly + lh):
        d.point((lx, y), fill=grises[1])
        d.point((lx + 1, y), fill=grises[2])
    for y in range(ly + 1, ly + lh - 1):
        d.point((lx + 2, y), fill=lente)
        if tam == 'imax':
            d.point((lx + 3, y), fill=lente)
    # LOS CARRETES arriba (cine e IMAX)
    if carretes:
        r = 3 if tam == 'cine' else 4
        cx = x0 + (4 if tam == 'cine' else 6)
        cy = y0 - r - 1
        for yy in range(cy - r, cy + r + 1):
            for xx in range(cx - r, cx + r + 1):
                ddx, ddy = xx - cx, yy - cy
                if ddx * ddx + ddy * ddy <= r * r:
                    d.point((xx, yy), fill=grises[0] if ddx * ddx + ddy * ddy < (r - 1) ** 2 else grises[2])
        d.point((cx, cy), fill=tinta)
    # LAS RUEDAS del carrito del salón (se lee como proyector de profe)
    if ruedas:
        for wx in (10, 21):
            for yy in range(23, 26):
                for xx in range(wx, wx + 3):
                    if (xx - wx - 1) ** 2 + (yy - 24) ** 2 <= 2:
                        d.point((xx, yy), fill=(40, 42, 52, 255))
        for x in range(9, 23):
            d.point((x, 22), fill=tinta)   # la base del carrito
    # EL HAZ que ya salió
    for i, c in enumerate([(lente[0], lente[1], lente[2], 255 - i * 60) for i in range(2)]):
        if lx + 3 + i < 30:
            d.point((lx + 3 + i, (y0 + y1) // 2), fill=c)
    guardar(img, nombre)


def flores():
    """Las flores amarillas que nadie recogió: el ramito marchito."""
    img, d = nuevo(30, 30)
    tallo = (86, 120, 60, 255)
    # los TALLOS: tres, atados abajo
    for i, x in enumerate((10, 15, 20)):
        for y in range(16, 27):
            d.point((x, y), fill=tallo if (y + i) % 7 else (60, 88, 44, 255))
    # las FLORES: cabezas GRANDES y redondas (5 pétalos alrededor del centro)
    amarillo = (250, 214, 64, 255)
    amarillo2 = (232, 186, 40, 255)
    centro = (200, 124, 24, 255)
    for fx, fy in ((9, 10), (15, 7), (21, 10)):
        # el ANILLO de pétalos (círculo relleno de 4 de radio)
        for y in range(fy - 3, fy + 4):
            for x in range(fx - 3, fx + 4):
                if (x - fx) ** 2 + (y - fy) ** 2 <= 10:
                    d.point((x, y), fill=amarillo if (x + y) % 3 else amarillo2)
        # el CORAZÓN ámbar
        d.point((fx, fy), fill=centro)
        d.point((fx - 1, fy), fill=centro)
        d.point((fx + 1, fy), fill=centro)
        d.point((fx, fy - 1), fill=centro)
        d.point((fx, fy + 1), fill=centro)
    # el TALLO ROTO: una flor ya cayó (la que no se dio)
    for i in range(3):
        d.point((24 + i, 26 - i), fill=(120, 96, 30, 255))
    # la CINTA roja del ramo
    for x in range(12, 19):
        d.point((x, 21), fill=(168, 60, 70, 255))
    guardar(img, 'FloresAmarillas')


def yapaselo():
    """El «ya lo superé»: la tablilla tallada en piedra (igual de falso)."""
    img, d = nuevo(30, 30)
    piedra = [(128, 128, 132), (100, 100, 106), (74, 74, 80)]
    tinta = (16, 20, 46, 255)
    for y in range(7, 23):
        for x in range(6, 24):
            tono = 0 if y < 11 else (1 if y < 19 else 2)
            if (x + 2 * y) % 11 == 0:
                tono = 2
            d.point((x, y), fill=piedra[tono])
    for x in range(6, 24):
        d.point((x, 7), fill=tinta); d.point((x, 22), fill=tinta)
    for y in range(7, 23):
        d.point((6, y), fill=tinta); d.point((23, y), fill=tinta)
    # EL GRABADO: tres rayitas — «ya lo superé» (nadie lee más)
    for fila in range(3):
        y = 11 + fila * 4
        for x in range(9, 20 - fila * 2):
            d.point((x, y), fill=(52, 52, 60, 255))
    # la GRIETA de la mentira (la piedra ya está rota)
    for i in range(5):
        d.point((20 + i // 2, 7 + i * 3), fill=(40, 40, 46, 255))
    guardar(img, 'YaLoSupere')


def pez():
    """Hay más peces en el mar: el pez que casi te mata (pinchos)."""
    img, d = nuevo(30, 30)
    azul = [(88, 150, 210), (60, 116, 172), (40, 84, 130)]
    tinta = (16, 20, 46, 255)
    for y in range(10, 21):
        for x in range(8, 23):
            dx = (x - 15) / 8.0
            dy = (y - 15) / 5.5
            if dx * dx + dy * dy <= 1.05:
                tono = 0 if dy < -0.2 else (1 if dy < 0.45 else 2)
                d.point((x, y), fill=azul[tono])
    # la COLA
    for i in range(5):
        for y in range(13 - i // 2, 18 + i // 2):
            d.point((22 + i, y), fill=azul[1 + (i > 2)])
    # el OJO: el que te mira con desprecio
    d.point((11, 13), fill=(250, 250, 250, 255))
    d.point((11, 14), fill=(16, 20, 46, 255))
    # la ALETA de arriba
    for i in range(4):
        d.point((13 + i, 9 - i), fill=azul[0])
        d.point((14 + i, 8 - i), fill=azul[1])
    # LOS PINCHOS: «este casi te mata» (las espinas del alma)
    for i in range(3):
        d.point((12 + i * 3, 11), fill=(230, 230, 240, 255))
        d.point((12 + i * 3, 10), fill=(180, 190, 210, 255))
    guardar(img, 'HayMasPeces')


def luna():
    """La luna que te ibas a bajar: la luna creciente CON su escalera."""
    img, d = nuevo(30, 30)
    dorado = [(252, 218, 110), (216, 172, 60), (166, 126, 40)]
    tinta = (16, 20, 46, 255)
    cx, cy = 15, 14
    for y in range(3, 26):
        for x in range(4, 26):
            dx, dy = x - cx, y - cy
            if dx * dx + dy * dy <= 121:
                dx2, dy2 = x - (cx + 7), y - cy
                if dx2 * dx2 + dy2 * dy2 >= 100:
                    tono = 0 if dx * dx + dy * dy < 60 else 2
                    d.point((x, y), fill=dorado[tono])
    # la cara del borde (el contorno creciente)
    for y in range(3, 26):
        for x in range(4, 26):
            dx, dy = x - cx, y - cy
            if 110 <= dx * dx + dy * dy <= 121:
                dx2, dy2 = x - (cx + 7), y - cy
                if dx2 * dx2 + dy2 * dy2 >= 100:
                    d.point((x, y), fill=tinta)
    # LA ESCALERA apoyada en la luna — la que ibas a subir
    for y in range(6, 25):
        d.point((22, y), fill=(150, 112, 60, 255))
        d.point((25, y), fill=(150, 112, 60, 255))
    for y in range(7, 25, 4):
        for x in range(22, 26):
            d.point((x, y), fill=(190, 150, 84, 255))
    guardar(img, 'LunaQueTeIbaABajar')


if __name__ == '__main__':
    print('· EL VISOR DEL PROYECTADO (sprites de la casa):')
    visor()
    carta()
    captura()
    noeres()
    proyector('salon', (250, 240, 160, 255), 'ProyectorDelSalon')
    proyector('cine', (255, 210, 90, 255), 'ProyectorDeCine')
    proyector('imax', (120, 200, 255, 255), 'ProyectorImax')
    flores()
    yapaselo()
    pez()
    luna()
    print('OK — 11 sprites')
