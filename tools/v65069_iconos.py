#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.50.69 — LOS 3 ICONOS DE LA FAMILIA DE TENTÁCULOS (Azote/Mordida/Cría).

Las lecciones VLM de la .68 (rondas 1-4): SILUETAS SÓLIDAS (nada de
checkerboard), el BLANCO DEL HUESO prende sobre cualquier fondo, el
borde VIOLETA de 1px lee la masa negra, el FILO ROJO continuo es la
firma de la familia, y a 30px MANDA la silueta gruesa — el detalle
fino se come. La firma de cada hermana: EL AZOTE = la S curva del
látigo con la punta de hueso · LA MORDIDA = la cabeza GORDA con las
tres garras cerrándose · LA CRÍA = las tres cabezas pequeñas con sus
ojos rasgados."""
from PIL import Image
import math

W = H = 30
NEGRO = (8, 4, 7, 255)
HUMO = (18, 10, 28, 255)
VIOLETA = (118, 74, 190, 255)
VIOLETA_CLARO = (156, 108, 226, 255)
BLANCO = (250, 248, 244, 255)
CREMA = (214, 206, 190, 255)
ROJO = (198, 18, 24, 255)
ROJO_CLARO = (232, 60, 52, 255)

def lienzo():
    return Image.new("RGBA", (W, H), (0, 0, 0, 0))

def px(im, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H:
        im.putpixel((x, y), c)

def disco(im, cx, cy, r, c):
    for y in range(H):
        for x in range(W):
            d = math.hypot(x - cx, y - cy)
            if d <= r:
                px(im, x, y, c)
            elif d <= r + 1 and c != NEGRO:
                px(im, x, y, c)

def vástago(im, pts, grosor, c=NEGRO, borde=VIOLETA):
    """Un tallo grueso por puntos (la masa del tentáculo) con borde 1px."""
    for (x, y) in pts:
        for dy in range(-grosor, grosor + 1):
            for dx in range(-grosor, grosor + 1):
                if dx * dx + dy * dy <= grosor * grosor:
                    px(im, x + dx, y + dy, c)
    # el borde violeta (los píxeles VACÍOS pegados a la masa)
    for (x, y) in pts:
        for dy in range(-grosor - 1, grosor + 2):
            for dx in range(-grosor - 1, grosor + 2):
                if grosor * grosor < dx * dx + dy * dy <= (grosor + 1) ** 2:
                    if im.getpixel((max(0, min(W - 1, x + dx)), max(0, min(H - 1, y + dy))))[3] == 0:
                        px(im, x + dx, y + dy, borde)

# ======================================================================
#  EL AZOTE — la S del látigo: tallo fino diagonal con la CURVA del
#  chasquido, la PUNTA DE HUESO (3 barbas blancas) y el filo rojo
# ======================================================================
def gen_azote():
    im = lienzo()
    # LA S DEL LÁTIGO — de la esquina inferior-izq (la mano) subiendo en
    # curva con el RETROCESO del chasquido (la punta se vuelve sobre sí)
    pts = []
    for i in range(26):
        t = i / 25.0
        # la columna: sale de (4,26), sube, y la PUNTA se curva a la
        # derecha con el gancho del azote
        x = 4 + t * 17 + 3.6 * math.sin(t * 2.5) * t
        y = 26 - t * 20 + 2.4 * math.sin(t * 3.1)
        pts.append((x, y))
    vástago(im, pts, 2)
    # LA BASE GORDA (nace de la mano) — el bulbo del tentáculo
    disco(im, 4, 26, 3, NEGRO)
    for y in range(H):
        for x in range(W):
            if math.hypot(x - 4, y - 26) <= 4 and im.getpixel((x, y))[3] == 0:
                px(im, x, y, VIOLETA)

    # EL FILO ROJO — la línea de energía que corre por el lomo
    for i, (x, y) in enumerate(pts):
        if i % 1 == 0:
            px(im, x + 2, y - 2, ROJO if i < 20 else ROJO_CLARO)

    # LA PUNTA DE HUESO — tres barbas blancas en abanico (el arpón)
    tx, ty = pts[-1]
    for k, ang in enumerate([0.35, 0.0, -0.35]):
        for m in range(6):
            px(im, tx + 2 + m * math.cos(ang - 0.5), ty - 1 + m * math.sin(ang - 0.5), BLANCO if m < 5 else CREMA)
    # la gota de bruma en la punta (la exhalación)
    disco(im, tx + 5, ty - 3, 1.6, HUMO)
    return im

# ======================================================================
#  LA MORDIDA — la cabeza GORDA: masa negra ENORME coronada de TRES
#  GARRAS BLANCAS cerrándose sobre la herida roja
# ======================================================================
def gen_mordida():
    im = lienzo()
    # EL CUERPO GORDO — corto y muscular, de la esquina al centro
    pts = []
    for i in range(12):
        t = i / 11.0
        pts.append((3 + t * 9, 27 - t * 9))
    vástago(im, pts, 3)
    disco(im, 3, 27, 3, NEGRO)

    # LA CABEZA GIGANTE — la masa de bruma (disco doble: humo + núcleo)
    disco(im, 17, 14, 8.6, NEGRO)
    disco(im, 19, 12, 6.4, HUMO)
    # el borde violeta de la cabeza (la lectura nocturna)
    for y in range(H):
        for x in range(W):
            d = math.hypot(x - 17, y - 14)
            if 8.6 < d <= 9.8 and im.getpixel((x, y))[3] == 0:
                px(im, x, y, VIOLETA)

    # LA HERIDA — el resplandor rojo DENTRO de la boca
    disco(im, 20, 14, 2.6, ROJO)
    px(im, 21, 13, ROJO_CLARO)

    # LAS TRES GARRAS DE HUESO — cerrándose sobre la herida desde arriba,
    # medio y abajo (el aplastador): curvas blancas con la punta crema
    for (bx, by, dx, dy) in [(13, 6, 1, 1), (10, 13, 1, 0), (13, 21, 1, -1)]:
        for m in range(7):
            cx = bx + dx * m + (0.22 * m if dy == 0 else 0)
            cy = by + dy * m
            px(im, cx, cy, BLANCO if m < 6 else CREMA)
            px(im, cx, cy + (1 if dy < 0 else -1 if dy > 0 else 1), CREMA if m < 6 else BLANCO)
    return im

# ======================================================================
#  LA CRÍA — las tres cabezas pequeñas con OJOS RASGADOS, en triángulo,
#  los cuerpecitos convergiendo a la esquina (nacen del portador)
# ======================================================================
def gen_cria():
    # v3 (la ronda VLM 2: «lacks a strong, singular focal point»): JERARQUÍA —
    # UNA CABEZA GRANDE al centro con el OJO GRANDE (5x3 blanco + pupila
    # 2x2 roja — la firma de la camada) y DOS cabecitas asomando detrás
    # (una abajo-izq, otra arriba-der), cada una con su ojito; los tres
    # cuerpos GORDOS al bulbo del portador en la esquina
    im = lienzo()
    bulbo = (3, 27)

    # LAS DOS PEQUEÑAS (detrás — se dibujan primero)
    pequeñas = [(10, 8), (25, 21)]
    for (cx, cy) in pequeñas:
        pts = []
        for i in range(14):
            t = i / 13.0
            pts.append((bulbo[0] + t * (cx - bulbo[0]) + 1.5 * math.sin(t * 3.2) * (1 - t),
                        bulbo[1] + t * (cy - bulbo[1])))
        vástago(im, pts, 1)
        disco(im, cx, cy, 4.2, NEGRO)
        # el rim violeta claro (la lectura — v3: más brillo arriba-izq)
        for y in range(H):
            for x in range(W):
                d = math.hypot(x - cx, y - cy)
                if 4.2 < d <= 5.2 and im.getpixel((x, y))[3] == 0:
                    px(im, x, y, VIOLETA)
        # el ojito (2px blanco + pupila) mirando al jefe (la cabeza grande)
        mira = math.atan2(16 - cy, 17 - cx)
        ex, ey = cx + 1.6 * math.cos(mira), cy + 1.6 * math.sin(mira)
        px(im, ex, ey, BLANCO)
        px(im, ex + math.cos(mira), ey + math.sin(mira), BLANCO)
        px(im, ex + math.cos(mira) * 0.5, ey + math.sin(mira) * 0.5, ROJO)

    # LA GRANDE (el foco — al centro, delante)
    cx, cy = 17, 16
    pts = []
    for i in range(16):
        t = i / 15.0
        pts.append((bulbo[0] + t * (cx - bulbo[0]) + 2.0 * math.sin(t * 3.0) * (1 - t),
                    bulbo[1] + t * (cy - bulbo[1])))
    vástago(im, pts, 2)
    disco(im, cx, cy, 7.4, NEGRO)
    disco(im, cx + 0.8, cy - 0.8, 5.6, HUMO)
    for y in range(H):
        for x in range(W):
            d = math.hypot(x - cx, y - cy)
            if 7.4 < d <= 8.6 and im.getpixel((x, y))[3] == 0:
                px(im, x, y, VIOLETA)
    # EL OJO GRANDE — almendra BLANCA 5x3 + pupila ROJA 2x2 (MIRÁNDOTE:
    # recto al frente del icono — la camada vigila al que la porta)
    for m in range(-2, 3):
        alto = 2 if abs(m) < 2 else 1
        for dy in range(-alto, alto + 1):
            px(im, cx + m + 1, cy - 1 + dy, BLANCO)
    disco(im, cx + 2, cy - 1, 1.2, ROJO)

    # LA MANO DEL PORTADOR — el bulbo de donde nacen las tres
    disco(im, bulbo[0], bulbo[1], 3.4, NEGRO)
    for y in range(H):
        for x in range(W):
            if math.hypot(x - bulbo[0], y - bulbo[1]) <= 4.4 and im.getpixel((x, y))[3] == 0:
                px(im, x, y, VIOLETA)
    return im

if __name__ == "__main__":
    import os
    base = os.path.join(os.path.dirname(__file__), "..", "AethonMod", "Content", "Weapons", "Sombras")
    icons = [
        ("AzoteDeLaPagina.png", gen_azote),
        ("MordidaDeLaPagina.png", gen_mordida),
        ("CriaDeLaPagina.png", gen_cria),
    ]
    for nombre, fn in icons:
        im = fn()
        im.save(os.path.join(base, nombre))
        print(nombre, "OK")

    # LA HOJA DE CONTACTO (para el VLM: los tres juntos, ×4)
    hoja = Image.new("RGBA", (30 * 3 * 4 + 20 * 5, 30 * 4 + 20 * 2), (40, 40, 46, 255))
    for k, (nombre, fn) in enumerate(icons):
        im = fn().resize((30 * 4, 30 * 4), Image.NEAREST)
        hoja.paste(im, (20 + k * (30 * 4 + 20), 20), im)
    hoja.save(os.path.join(os.path.dirname(__file__), "v65069_hoja_iconos.png"))
    print("hoja de contacto OK")
