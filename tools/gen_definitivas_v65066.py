#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
GEN_DEFINITIVAS_V65066.PY — LOS ICONOS DE LA BOLSA DE LAS ARMAS DEFINITIVAS.

La lección de las rondas anteriores (la letra del usuario): «son todas
una raya plana y no debe ser así… eran más elaboradas que eso». La
investigación (Task 2-a) midió los sprites endgame de Calamity: núcleo
brillante + gradiente + motivo temático + borde con brillo para leerse
de día y de noche. Este generador aplica ESA receta a los 13 iconos:

  · 12 armas (44×44) + LA BOLSA (30×30)
  · CADA icono: contorno oscuro → cuerpo con gradiente → RIM de luz →
    núcleo blanco → motivo propio (ojo/prisma/hojas/…) → destellos.
"""
from PIL import Image, ImageDraw, ImageFilter
import math, os, random

OUT = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Weapons/Definitivas'
OUT_BOLSA = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Items/Bolsas'
S = 44  # tamaño de los iconos de armas

ORO      = (255, 214, 120)
ORO_OSC  = (196, 140, 52)
AMBAR    = (255, 158, 64)
BLANCO   = (255, 250, 240)
VIOLETA  = (176, 118, 255)
VIO_OSC  = (104, 62, 168)
CIAN     = (120, 220, 255)
NEGRO    = (24, 14, 36)
ROJO     = (232, 58, 70)
PLATA    = (216, 226, 240)

rng = random.Random(66)

# ======================================================================
#  LOS PINCELES BASE
# ======================================================================

def lienzo(s=S):
    return Image.new('RGBA', (s, s), (0, 0, 0, 0))

def radial(s, centro, radio, c_int, c_ext):
    """Gradiente radial suave (para glows y cuerpos)."""
    img = Image.new('RGBA', (s, s), (0, 0, 0, 0))
    px = img.load()
    cx, cy = centro
    for y in range(s):
        for x in range(s):
            d = math.hypot(x - cx, y - cy) / max(radio, 1)
            if d <= 1.0:
                t = d ** 0.8
                r = int(c_int[0] + (c_ext[0] - c_int[0]) * t)
                g = int(c_int[1] + (c_ext[1] - c_int[1]) * t)
                b = int(c_int[2] + (c_ext[2] - c_int[2]) * t)
                a = int(c_int[3] + (c_ext[3] - c_int[3]) * t)
                px[x, y] = (r, g, b, a)
    return img

def vgrad(s, top, bot, x0=0, x1=None, y0=0, y1=None):
    """Gradiente vertical en una banda (para hojas/cuerpos)."""
    x1 = x1 if x1 is not None else s
    y1 = y1 if y1 is not None else s
    img = Image.new('RGBA', (s, s), (0, 0, 0, 0))
    px = img.load()
    h = max(y1 - y0, 1)
    for y in range(y0, y1):
        t = (y - y0) / h
        r = int(top[0] + (bot[0] - top[0]) * t)
        g = int(top[1] + (bot[1] - top[1]) * t)
        b = int(top[2] + (bot[2] - top[2]) * t)
        a = int(top[3] + (bot[3] - top[3]) * t)
        for x in range(x0, x1):
            px[x, y] = (r, g, b, a)
    return img

def over(base, capa):
    return Image.alpha_composite(base, capa)

def pincelada(img, cx, cy, dx, dy, largo, grosor, c_base, c_punta, brillo=1.0):
    """Un trazo de pincel que se estrecha (nada de líneas planas): cuerpo
    con gradiente base→punta + núcleo brillante + borde."""
    s = img.size[0]
    px = img.load()
    for i in range(largo):
        t = i / max(largo - 1, 1)
        x = cx + dx * i
        y = cy + dy * i
        w = max(grosor * (1.0 - 0.55 * t), 1.2)
        # el vaivén del pulso (la línea viva, no la regla)
        wob = math.sin(t * 6.0 + cx) * 0.9
        for k in range(-int(w) - 1, int(w) + 2):
            dist = abs(k) / max(w, 0.001)
            if dist > 1.0:
                continue
            nx = int(round(x - dy * (k + wob)))
            ny = int(round(y + dx * (k + wob)))
            if 0 <= nx < s and 0 <= ny < s:
                r = int(c_base[0] + (c_punta[0] - c_base[0]) * t)
                g = int(c_base[1] + (c_punta[1] - c_base[1]) * t)
                b = int(c_base[2] + (c_punta[2] - c_base[2]) * t)
                f = (1.0 - dist) * brillo
                f1 = min(max(f, 0.0), 1.0)
                viejo = px[nx, ny]
                a = min(255, int(255 * f1))
                px[nx, ny] = (int(r * f1 + viejo[0] * (1 - f1)),
                              int(g * f1 + viejo[1] * (1 - f1)),
                              int(b * f1 + viejo[2] * (1 - f1)),
                              max(a, viejo[3]))
    return img

def estrella4(img, cx, cy, largo, c, nucleo=None):
    """Estrella de 4 puntas (el destello de la casa) con núcleo blanco."""
    px = img.load()
    s = img.size[0]
    nucleo = nucleo or BLANCO
    for ang in range(4):
        a = ang * math.pi / 2 + math.pi / 4
        dx, dy = math.cos(a), math.sin(a)
        for i in range(largo):
            t = i / largo
            w = max((1.0 - t) * 2.2, 0.5)
            x = int(cx + dx * i)
            y = int(cy + dy * i)
            for k in (-1, 0, 1):
                nx = int(x - dy * k * w)
                ny = int(y + dx * k * w)
                if 0 <= nx < s and 0 <= ny < s and abs(k) <= w:
                    f = (1 - t) * (1 - abs(k) / max(w, 0.001))
                    viejo = px[nx, ny]
                    px[nx, ny] = (int(c[0] * f + viejo[0] * (1 - f)),
                                  int(c[1] * f + viejo[1] * (1 - f)),
                                  int(c[2] * f + viejo[2] * (1 - f)),
                                  max(viejo[3], int(255 * f)))
    # el núcleo
    for y in range(-2, 3):
        for x in range(-2, 3):
            if abs(x) + abs(y) <= 2:
                nx, ny = int(cx) + x, int(cy) + y
                if 0 <= nx < s and 0 <= ny < s:
                    f = 1.0 - (abs(x) + abs(y)) * 0.18
                    px[nx, ny] = (int(nucleo[0] * f), int(nucleo[1] * f), int(nucleo[2] * f), 255)
    return img

def chispa(img, x, y, c=BLANCO, tam=1):
    px = img.load()
    s = img.size[0]
    if 0 <= x < s and 0 <= y < s:
        viejo = px[x, y]
        px[x, y] = (c[0], c[1], c[2], 255)
    return img

def contorno(img, c=(20, 10, 30, 255), umbral=40):
    """El borde oscuro que hace leer el sprite en cualquier fondo."""
    s = img.size[0]
    borde = Image.new('RGBA', (s, s), (0, 0, 0, 0))
    bp = borde.load()
    px = img.load()
    for y in range(s):
        for x in range(s):
            if px[x, y][3] > umbral:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < s and 0 <= ny < s and px[nx, ny][3] <= umbral:
                        bp[nx, ny] = c
    return over(borde, img)

def rim(img, c=BLANCO, f=0.55):
    """El borde de luz del lado superior-izquierdo (la luz de la casa)."""
    s = img.size[0]
    px = img.load()
    for y in range(1, s):
        for x in range(1, s):
            if px[x, y][3] > 90:
                if px[x - 1, y][3] < 40 or px[x, y - 1][3] < 40:
                    r, g, b, a = px[x, y]
                    px[x, y] = (min(255, int(r + (255 - r) * f)),
                                min(255, int(g + (255 - g) * f)),
                                min(255, int(b + (255 - b) * f)), a)
    return img

# ======================================================================
#  1 — EL OJO DEL JUICIO: el ojo dorado radiante
# ======================================================================
def gen_ojo():
    img = lienzo()
    s = S
    cx, cy = 22, 22

    # EL HALO (el resplandor que enmarca)
    img = over(img, radial(s, (cx, cy), 21, (255, 214, 120, 90), (255, 158, 64, 0)))

    # LAS PESTAÑAS RADIANTES (8 rayos de luz alrededor)
    for k in range(8):
        a = k * math.pi / 4
        largo = 15 if k % 2 == 0 else 10
        img = pincelada(img, cx + math.cos(a) * 7, cy + math.sin(a) * 6,
                        math.cos(a), math.sin(a), largo, 2.2, ORO, AMBAR, 0.9)

    # LA ESCLERÓTICA (el almendro dorado)
    d = ImageDraw.Draw(img)
    for i, w in ((0, 6), ):
        pass
    # el almendro: dos arcos rellenos
    pts = []
    for t in range(60):
        a = t / 59 * math.pi
        pts.append((cx - math.cos(a) * 16, cy - math.sin(a) * 8))
    for t in range(60):
        a = t / 59 * math.pi
        pts.append((cx + math.cos(a) * 16, cy + math.sin(a) * 8 + 0))
    d.polygon(pts, fill=(250, 226, 170, 255))

    # EL IRIS (gradiente concéntrico oro→ámbar→violeta)
    iris = radial(s, (cx, cy), 9, (255, 214, 120, 255), (150, 70, 200, 255))
    iris = iris.resize((18, 18))
    mascara = Image.new('L', (18, 18), 0)
    ImageDraw.Draw(mascara).ellipse([0, 0, 17, 17], fill=255)
    iris.putalpha(mascara)
    img.paste(iris, (cx - 9, cy - 9), iris)

    # LA PUPILA (núcleo blanco + la mirada)
    img = over(img, radial(s, (cx, cy), 4, (255, 255, 255, 255), (255, 226, 150, 0)))

    # EL DESTELLO (la estrella que vive en la pupila)
    img = estrella4(img, cx, cy, 9, ORO)

    img = contorno(img)
    img = rim(img, BLANCO, 0.5)
    for (x, y) in ((7, 12), (36, 9), (10, 33), (35, 34)):
        chispa(img, x, y)
    return img

# ======================================================================
#  2 — EL PRISMA PRIMORDIAL: el cristal de siete facetas
# ======================================================================
def gen_prisma():
    img = lienzo()
    cx, cy = 20, 22
    d = ImageDraw.Draw(img)

    # EL HALO
    img = over(img, radial(S, (cx, cy), 20, (200, 180, 255, 70), (120, 220, 255, 0)))

    # EL CRISTAL (diamante vertical de 7 franjas del espectro)
    alto, semi = 19, 12
    facetas = [(255, 80, 80), (255, 160, 60), (255, 230, 100), (110, 255, 130),
               (110, 220, 255), (150, 130, 255), (220, 120, 255)]
    dd = ImageDraw.Draw(img)
    for k in range(7):
        t0, t1 = k / 7, (k + 1) / 7
        c = facetas[k]
        cs = (int(c[0] * 0.72), int(c[1] * 0.72), int(c[2] * 0.72))
        # faceta superior (triángulo punta→centro)
        dd.polygon([(cx + t0 * semi - semi * 0.5, cy),
                   (cx + t1 * semi - semi * 0.5, cy),
                   (cx + (t0 + t1) * 0.5 * semi - semi * 0.5, cy - alto)],
                  fill=(*c, 255))
        # faceta inferior
        dd.polygon([(cx + t0 * semi - semi * 0.5, cy),
                   (cx + t1 * semi - semi * 0.5, cy),
                   (cx + (t0 + t1) * 0.5 * semi - semi * 0.5, cy + alto * 0.8)],
                  fill=(*cs, 255))
    # LOS BORDES entre facetas (las aristas del cristal — legibilidad)
    dd = ImageDraw.Draw(img)
    for k in range(8):
        t = k / 7
        x0 = cx + t * semi - semi * 0.5
        dd.line([(x0, cy), (x0 + (t - 0.5) * 0, cy - alto)], fill=(30, 18, 50, 160), width=1)
    dd.line([(cx - semi * 0.5, cy), (cx, cy - alto)], fill=(30, 18, 50, 200), width=1)
    dd.line([(cx + semi * 0.5, cy), (cx, cy - alto)], fill=(30, 18, 50, 200), width=1)
    dd.line([(cx - semi * 0.5, cy), (cx, cy + alto * 0.8)], fill=(30, 18, 50, 200), width=1)
    dd.line([(cx + semi * 0.5, cy), (cx, cy + alto * 0.8)], fill=(30, 18, 50, 200), width=1)

    # EL NÚCLEO blanco (el corazón del prisma)
    img = over(img, radial(S, (cx, cy), 7, (255, 255, 255, 255), (255, 255, 255, 0)))

    # EL HAZ que sale (UNO solo, grueso y limpio — la promesa, no el ruido)
    img = pincelada(img, cx + 9, cy, 1.0, -0.22, 13, 2.0, facetas[2], BLANCO, 1.0)
    img = estrella4(img, cx + 22, cy - 4, 5, BLANCO)

    img = contorno(img)
    img = rim(img, BLANCO, 0.45)
    chispa(img, 8, 8); chispa(img, 36, 34)
    return img

# ======================================================================
#  3 — LA LLUVIA DE VERSOS: las dos hojas cruzadas
# ======================================================================
def gen_versos():
    img = lienzo()

    # LOS HALOS de las dos hojas
    img = over(img, radial(S, (14, 30), 16, (255, 214, 120, 80), (255, 214, 120, 0)))
    img = over(img, radial(S, (30, 14), 16, (176, 118, 255, 80), (176, 118, 255, 0)))

    # HOJA ORO (de abajo-izq a arriba-der) — el filo con lomo
    img = pincelada(img, 8, 36, 0.707, -0.707, 30, 3.2, ORO_OSC, ORO, 1.0)
    img = pincelada(img, 8, 36, 0.707, -0.707, 30, 1.2, ORO, BLANCO, 1.0)
    # HOJA VIOLETA (de arriba-izq a abajo-der)
    img = pincelada(img, 8, 8, 0.707, 0.707, 30, 3.2, VIO_OSC, VIOLETA, 1.0)
    img = pincelada(img, 8, 8, 0.707, 0.707, 30, 1.2, VIOLETA, BLANCO, 1.0)

    # EL CRUCE (la estrella del encuentro)
    img = estrella4(img, 21, 22, 10, BLANCO)
    img = over(img, radial(S, (21, 22), 8, (255, 255, 255, 200), (255, 214, 120, 0)))

    # LAS EMPUÑADURAS (los cabos de los versos)
    d = ImageDraw.Draw(img)
    d.ellipse([5, 33, 11, 39], fill=(70, 44, 20, 255))
    d.ellipse([5, 5, 11, 11], fill=(48, 28, 80, 255))

    img = contorno(img)
    img = rim(img, BLANCO, 0.4)
    for (x, y) in ((34, 30), (33, 12), (12, 22)):
        chispa(img, x, y)
    return img

# ======================================================================
#  4 — EL FILO CONSTELADO: la espada con estrellas en la hoja
# ======================================================================
def gen_filo():
    img = lienzo()
    d = ImageDraw.Draw(img)

    # EL HALO
    img = over(img, radial(S, (22, 18), 20, (255, 214, 120, 60), (176, 118, 255, 0)))

    # LA HOJA (vertical, azul-noche con gradiente)
    hoja = vgrad(S, (92, 108, 168, 255), (38, 34, 88, 255), 19, 25, 2, 32)
    img = over(img, hoja)
    # el filo (el borde luminoso de la hoja)
    img = pincelada(img, 20, 4, 0, 1, 28, 1.3, BLANCO, ORO, 0.9)
    img = pincelada(img, 24, 4, 0, 1, 28, 1.3, ORO, CIAN, 0.6)

    # LA PUNTA
    d.polygon([(19, 2), (25, 2), (22, 0)], fill=(230, 236, 255, 255))

    # LAS ESTRELLAS DE LA CONSTELACIÓN (4 nodos unidos por la hebra)
    nodos = [(22, 7), (22, 15), (22, 23), (22, 30)]
    for k in range(3):
        x0, y0 = nodos[k]; x1, y1 = nodos[k + 1]
        img = pincelada(img, x0, y0, 0, 1, int(y1 - y0), 0.8, ORO, ORO, 0.7)
    for (x, y) in nodos:
        img = estrella4(img, x, y, 4, ORO)

    # LA GUARDIA (la cruz dorada)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([12, 31, 32, 35], 2, fill=(255, 214, 120, 255))
    d.rounded_rectangle([14, 32, 30, 33], 1, fill=(255, 240, 190, 255))
    # EL PUÑO
    d.rounded_rectangle([20, 35, 24, 40], 1, fill=(120, 78, 30, 255))
    # EL POMO (la estrella)
    img = estrella4(img, 22, 41, 4, VIOLETA)

    img = contorno(img)
    img = rim(img, BLANCO, 0.35)
    chispa(img, 30, 10); chispa(img, 14, 26)
    return img

# ======================================================================
#  5 — EL PARTIDOR DE MUNDOS: la columna que parte la esfera
# ======================================================================
def gen_partidor():
    img = lienzo()
    d = ImageDraw.Draw(img)

    # LA ESFERA PARTIDA (dos mitades separándose)
    for lado in (-1, 1):
        cx = 22 + lado * 5
        cuerpo = radial(S, (cx, 28), 10, (110, 130, 190, 255), (28, 24, 66, 255))
        mascara = Image.new('L', (S, S), 0)
        dm = ImageDraw.Draw(mascara)
        if lado < 0:
            dm.ellipse([cx - 10, 18, cx + 9, 38], fill=255)
        else:
            dm.ellipse([cx - 9, 18, cx + 10, 38], fill=255)
        cuerpo.putalpha(mascara)
        img = over(img, cuerpo)
        # el borde interior roto (la herida brillante)
        d = ImageDraw.Draw(img)
        xh = 22 + lado * 3
        d.line([(xh, 19), (xh, 37)], fill=(255, 214, 120, 230), width=1)

    # LA COLUMNA (el pilar de luz vertical, ancho arriba, núcleo blanco)
    img = pincelada(img, 22, 1, 0, 1, 40, 3.4, ORO, BLANCO, 1.0)
    img = pincelada(img, 22, 1, 0, 1, 40, 1.4, BLANCO, BLANCO, 1.0)
    # el halo de la columna
    img = over(img, radial(S, (22, 20), 16, (255, 240, 200, 120), (255, 158, 64, 0)))

    # EL ESTALLIDO de la base
    img = estrella4(img, 22, 38, 8, ORO)

    img = contorno(img)
    img = rim(img, BLANCO, 0.4)
    chispa(img, 8, 12); chispa(img, 36, 16)
    return img

# ======================================================================
#  6 — LA GUADAÑA DEL VIEJO DIOS
# ======================================================================
def gen_guadana():
    img = lienzo()
    d = ImageDraw.Draw(img)

    # EL HALO (el aura violeta-dorada del arma)
    img = over(img, radial(S, (24, 16), 22, (176, 118, 255, 80), (255, 214, 120, 0)))

    # ===== LA HOJA: media luna SÓLIDA (polígono con gradiente por franja) =====
    # puntos del borde exterior y del interior (la cuchilla curva clásica)
    ext, inte = [], []
    for i in range(33):
        t = i / 32
        a = -1.42 + t * 2.35
        rext = 18.0
        rint = 11.0 + 4.5 * math.sin(t * math.pi)   # engorda al centro
        ext.append((24 + math.cos(a) * rext, 11 + math.sin(a) * rext))
        inte.append((24 + math.cos(a) * rint, 11 + math.sin(a) * rint))
    hoja = ext + inte[::-1]
    # relleno por franjas (del interior oscuro al borde ORO)
    for capa, (rin, rex, c) in enumerate([
        (10.5, 18.0, VIO_OSC), (11.5, 18.0, VIOLETA), (13.6, 18.0, (216, 170, 255, 255)),
        (15.6, 18.0, ORO), (17.0, 18.0, (255, 240, 200, 255))]):
        pts_c = []
        for i in range(33):
            t = i / 32
            a = -1.42 + t * 2.35
            pts_c.append((24 + math.cos(a) * rex, 11 + math.sin(a) * rex))
        for i in range(32, -1, -1):
            t = i / 32
            a = -1.42 + t * 2.35
            r = max(rin, 11.0 + 4.5 * math.sin(t * math.pi))
            pts_c.append((24 + math.cos(a) * r, 11 + math.sin(a) * r))
        d = ImageDraw.Draw(img)
        d.polygon(pts_c, fill=c)

    # EL FILO blanco (el borde que corta — el último cuarto exterior)
    d = ImageDraw.Draw(img)
    for i in range(30):
        t = i / 29
        a = -1.42 + t * 2.35
        x = 24 + math.cos(a) * 18.4
        y = 11 + math.sin(a) * 18.4
        if 0 <= int(x) < S and 0 <= int(y) < S:
            img.putpixel((int(x), int(y)), (*BLANCO, 255))

    # LA PUNTA brillante (el colmillo de arriba)
    img = estrella4(img, 6, 15, 5, BLANCO)
    img = over(img, radial(S, (7, 14), 6, (255, 255, 255, 170), (176, 118, 255, 0)))

    # ===== EL MANGO: asta diagonal SÓLIDA =====
    d = ImageDraw.Draw(img)
    pts_m = []
    for i in range(2):
        t = i
        for k in (-1, 1):
            w = 2.8 - 0.6 * t
            x = 25 - t * 13
            y = 13 + t * 30
            dx, dy = -13 / 33, 30 / 33
            pts_m.append((x + -dy * k * w, y + dx * k * w))
    d.polygon([(22.2, 12.4), (27.4, 14.6), (15.8, 42.6), (11.4, 41.2)], fill=(96, 58, 30, 255))
    # el reflejo del asta (la veta)
    d.line([(22, 15), (13, 41)], fill=(196, 148, 92, 255), width=1)
    # el engarce dorado (donde la hoja abraza el mango)
    d.ellipse([20, 8, 29, 17], outline=(255, 214, 120, 255))
    d.ellipse([21, 9, 28, 16], outline=(196, 140, 52, 255))
    d.ellipse([22, 10, 27, 15], fill=(150, 96, 40, 255))

    # EL ALMA (la llama fría en la punta inferior del filo)
    img = estrella4(img, 38, 21, 4, BLANCO)
    img = over(img, radial(S, (38, 21), 5, (255, 255, 255, 150), (176, 118, 255, 0)))

    img = contorno(img)
    img = rim(img, BLANCO, 0.3)
    chispa(img, 33, 4); chispa(img, 5, 33)
    return img

# ======================================================================
#  7 — EL SISTEMA DE AETHON: el sol y sus cuatro planetas
# ======================================================================
def gen_sistema():
    img = lienzo()

    # LAS ÓRBITAS (dos elipses tenues)
    d = ImageDraw.Draw(img)
    for (rx, ry, c) in ((15, 9, (255, 214, 120, 90)), (10, 6, (176, 118, 255, 80))):
        for t in range(64):
            a = t / 63 * math.pi * 2
            x = 22 + math.cos(a) * rx
            y = 22 + math.sin(a) * ry
            d.point((x, y), fill=c)

    # EL SOL (el centro dorado con su corona)
    img = over(img, radial(S, (22, 22), 12, (255, 250, 210, 255), (255, 158, 64, 0)))
    d = ImageDraw.Draw(img)
    d.ellipse([17, 17, 27, 27], fill=(255, 240, 180, 255))
    d.ellipse([19, 19, 25, 25], fill=(255, 255, 245, 255))
    for k in range(8):
        a = k * math.pi / 4
        img = pincelada(img, 22 + math.cos(a) * 6, 22 + math.sin(a) * 6,
                        math.cos(a), math.sin(a), 5, 1.2, ORO, AMBAR, 0.85)

    # LOS CUATRO CUERPOS (luna arriba, cometa derecha, estrella abajo, vacío izq)
    cuerpos = [(22, 7, PLATA, 3), (36, 22, CIAN, 3), (22, 37, VIOLETA, 3), (7, 22, (90, 40, 150), 3)]
    for (x, y, c, r) in cuerpos:
        img = over(img, radial(S, (x, y), 6, (*c, 220), (*c, 0)))
        d = ImageDraw.Draw(img)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(*c, 255))
        d.ellipse([x - r + 1, y - r + 1, x, y], fill=(min(255, c[0] + 60), min(255, c[1] + 60), min(255, c[2] + 60), 255))
    # el vacío con su rim
    d.ellipse([6, 21, 9, 24], outline=(200, 140, 255, 255))

    img = contorno(img)
    img = rim(img, BLANCO, 0.3)
    chispa(img, 33, 8); chispa(img, 10, 34)
    return img

# ======================================================================
#  8 — LA CONSTELACIÓN DEL JUICIO
# ======================================================================
def gen_constelacion():
    img = lienzo()

    # EL VELO (el fondo de la noche, muy tenue)
    img = over(img, radial(S, (22, 22), 22, (40, 26, 80, 110), (10, 6, 24, 0)))

    # LAS CINCO ESTRELLAS (el pentágono)
    nodos = []
    for k in range(5):
        a = -math.pi / 2 + k * 2 * math.pi / 5
        nodos.append((22 + math.cos(a) * 14, 22 + math.sin(a) * 14))

    # LOS HILOS (las líneas que unen, con su vida)
    for k in range(5):
        x0, y0 = nodos[k]
        x1, y1 = nodos[(k + 1) % 5]
        dx, dy = x1 - x0, y1 - y0
        n = max(int(math.hypot(dx, dy)), 1)
        img = pincelada(img, x0, y0, dx / n, dy / n, n, 1.0, ORO, ORO, 0.6)

    # LAS ESTRELLAS (cada una con su núcleo y su halo)
    for (x, y) in nodos:
        img = over(img, radial(S, (x, y), 7, (255, 250, 220, 160), (255, 214, 120, 0)))
        img = estrella4(img, x, y, 5, ORO)

    # LA ESTRELLA MAYOR (el sello del juicio, al centro)
    img = estrella4(img, 22, 22, 7, BLANCO)
    img = over(img, radial(S, (22, 22), 9, (255, 255, 255, 140), (176, 118, 255, 0)))

    img = contorno(img, (16, 8, 28, 255), 30)
    chispa(img, 13, 13); chispa(img, 33, 30)
    return img

# ======================================================================
#  9 — EL VÓRTICE DEVORADOR
# ======================================================================
def gen_vortice():
    img = lienzo()
    cx, cy = 22, 22

    # EL RIM EXTERIOR (la luz violeta que hace visible lo negro)
    img = over(img, radial(S, (cx, cy), 21, (104, 62, 168, 140), (40, 16, 70, 0)))

    # LA ESPIRAL (tres brazos que se enroscan)
    for brazo in range(3):
        prev = None
        for i in range(46):
            t = i / 45
            a = brazo * 2.094 - t * 4.6
            r = 19 * (1 - t) + 4
            x = cx + math.cos(a) * r
            y = cy + math.sin(a) * r
            w = 3.4 * (1 - t * 0.55)
            d = ImageDraw.Draw(img)
            color = (int(60 + 100 * t), int(30 + 40 * t), int(90 + 80 * t), int(255 * (1 - t * 0.3)))
            d.ellipse([x - w, y - w, x + w, y + w], fill=color)

    # EL DISCO NEGRO (la garganta)
    d = ImageDraw.Draw(img)
    d.ellipse([cx - 7, cy - 7, cx + 7, cy + 7], fill=(12, 6, 20, 255))

    # EL OJO (te mira — GRANDE y con contraste)
    img = over(img, radial(S, (cx, cy), 6, (255, 255, 255, 255), (255, 255, 255, 0)))
    d = ImageDraw.Draw(img)
    d.ellipse([cx - 4, cy - 4, cx + 4, cy + 4], fill=(255, 250, 240, 255))
    d.ellipse([cx - 2, cy - 2, cx + 2, cy + 2], fill=(198, 18, 24, 255))
    d.point((cx - 1, cy - 1), fill=(255, 255, 255, 255))

    # EL ANILLO DE ACRECIÓN (el rim dorado-violeta)
    d.ellipse([cx - 9, cy - 9, cx + 9, cy + 9], outline=(176, 118, 255, 255))
    d.ellipse([cx - 10, cy - 10, cx + 10, cy + 10], outline=(255, 214, 120, 120))

    img = contorno(img)
    chispa(img, 6, 10, (200, 140, 255)); chispa(img, 38, 34, (200, 140, 255))
    return img

# ======================================================================
#  10 — EL RÉQUIEM ESTELAR: la campana de luz
# ======================================================================
def gen_requiem():
    img = lienzo()
    d = ImageDraw.Draw(img)
    cx = 22

    # LOS HALOS (los anillos de la campanada)
    for r, a in ((19, 60), (14, 100)):
        d.ellipse([cx - r, 20 - r, cx + r, 20 + r], outline=(255, 214, 120, a))
        d.ellipse([cx - r + 1, 21 - r, cx + r - 1, 21 + r], outline=(255, 240, 200, a // 2))

    # LA CAMPANA (el cuerpo dorado — GRANDE, llena el lienzo)
    d.pieslice([10, 6, 34, 32], 180, 360, fill=(255, 214, 120, 255))
    d.rectangle([10, 18, 34, 27], fill=(255, 214, 120, 255))
    # el brillo lateral (el bronce pulido)
    d.pieslice([13, 6, 28, 32], 180, 360, fill=(255, 240, 190, 255))
    d.rectangle([13, 18, 28, 27], fill=(255, 240, 190, 255))
    # la falda
    d.pieslice([10, 21, 34, 39], 0, 180, fill=(196, 140, 52, 255))
    d.pieslice([13, 21, 28, 39], 0, 180, fill=(255, 226, 150, 255))
    # EL BADAJO
    d.ellipse([cx - 3, 35, cx + 4, 41], fill=(255, 250, 240, 255))
    # EL ASA
    d.rectangle([18, 3, 26, 8], fill=(196, 140, 52, 255))
    d.rectangle([19, 4, 25, 7], fill=(255, 240, 190, 255))

    # LA ESTRELLA (la campanada que estalla)
    img = estrella4(img, 22, 41, 6, BLANCO)

    img = contorno(img)
    img = rim(img, BLANCO, 0.4)
    chispa(img, 6, 8); chispa(img, 38, 10)
    return img

# ======================================================================
#  11 — LA IRA DEL CIELO: la nube y el rayo
# ======================================================================
def gen_ira():
    img = lienzo()
    d = ImageDraw.Draw(img)

    # LA NUBE (violeta-noche, tres lóbulos — COMPACTA y con rim claro)
    for (x, y, r) in ((13, 12, 8), (23, 10, 9), (31, 13, 7), (7, 14, 6)):
        nube = radial(S, (x, y), r, (128, 100, 190, 255), (52, 32, 96, 255))
        img = over(img, nube)
    # el borde inferior de la nube (la amenaza)
    d = ImageDraw.Draw(img)
    d.ellipse([4, 14, 12, 20], fill=(84, 56, 140, 255))
    d.ellipse([28, 12, 40, 20], fill=(84, 56, 140, 255))
    # el rim superior (la luz que la delata)
    d.arc([15, 3, 31, 17], 200, 320, fill=(230, 210, 255, 230))
    d.arc([5, 7, 19, 19], 210, 320, fill=(230, 210, 255, 180))

    # EL RAYO (el rayo dentado de oro, de la nube al suelo — GRUESO y limpio)
    puntos = [(23, 17), (16, 26), (21, 27), (14, 36), (20, 34), (17, 43)]
    for i in range(len(puntos) - 1):
        x0, y0 = puntos[i]
        x1, y1 = puntos[i + 1]
        dx, dy = x1 - x0, y1 - y0
        n = max(int(math.hypot(dx, dy)), 1)
        img = pincelada(img, x0, y0, dx / n, dy / n, n, 4.2, ORO, BLANCO, 1.0)
    # el núcleo blanco del rayo (la vena)
    for i in range(len(puntos) - 1):
        x0, y0 = puntos[i]
        x1, y1 = puntos[i + 1]
        dx, dy = x1 - x0, y1 - y0
        n = max(int(math.hypot(dx, dy)), 1)
        img = pincelada(img, x0, y0, dx / n, dy / n, n, 1.6, BLANCO, BLANCO, 1.0)

    # EL DESTELLO del impacto
    img = estrella4(img, 18, 42, 7, ORO)
    img = over(img, radial(S, (22, 12), 16, (255, 214, 120, 70), (176, 118, 255, 0)))

    # LOS TRAZOS de lluvia
    for (x, y) in ((8, 24), (36, 22), (33, 34), (6, 34)):
        d = ImageDraw.Draw(img)
        d.line([(x, y), (x - 1, y + 5)], fill=(150, 190, 255, 160), width=1)

    img = contorno(img)
    img = rim(img, BLANCO, 0.3)
    return img

# ======================================================================
#  12 — EL TESTAMENTO DE AETHON: el libro abierto con el ojo
# ======================================================================
def gen_testamento():
    img = lienzo()
    d = ImageDraw.Draw(img)

    # EL AURA (el resplandor del veredicto)
    img = over(img, radial(S, (22, 20), 22, (255, 214, 120, 90), (176, 118, 255, 0)))

    # ===== EL OJO GRANDE (arriba, con todo su detalle) =====
    # la esclerótica (el almendro blanco)
    pts = []
    for t in range(44):
        a = t / 43 * math.pi
        pts.append((22 - math.cos(a) * 13, 6 - math.sin(a) * 6))
    for t in range(44):
        a = t / 43 * math.pi
        pts.append((22 + math.cos(a) * 13, 6 + math.sin(a) * 6))
    d.polygon(pts, fill=(255, 252, 244, 255))
    # el iris (anillo violeta con borde dorado)
    d.ellipse([16, 2, 28, 10], fill=(104, 62, 168, 255))
    d.ellipse([17, 3, 27, 9], fill=(176, 118, 255, 255))
    # la pupila blanca con su destello
    d.ellipse([19, 4, 25, 8], fill=(255, 252, 244, 255))
    d.point((21, 5), fill=(104, 62, 168, 255))
    # EL SELLO (la estrella que corona el ojo)
    img = estrella4(img, 22, 6, 7, ORO)

    # ===== EL LIBRO ABIERTO (RECTO y NÍTIDO, abajo) =====
    # la tapa marrón (el borde que da volumen)
    d.polygon([(2, 16), (22, 12), (42, 16), (42, 41), (22, 43), (2, 41)], fill=(96, 60, 24, 255))
    # las páginas crema (las dos hojas abiertas)
    d.polygon([(4, 18), (22, 14), (22, 38), (4, 39)], fill=(255, 240, 200, 255))
    d.polygon([(40, 18), (22, 14), (22, 38), (40, 39)], fill=(255, 228, 168, 255))
    # las líneas de texto (la tinta)
    for k in range(4):
        y0 = 21 + k * 4
        d.line([(7, y0), (19, y0 - 1)], fill=(130, 84, 40, 255), width=1)
        d.line([(25, y0 - 1), (37, y0)], fill=(130, 84, 40, 255), width=1)
    # EL LOMO (la costura roja del grimorio)
    d.line([(22, 14), (22, 40)], fill=(198, 18, 24, 255), width=2)

    # LOS DOS RAYOS gruesos (la palabra que baja del ojo a las páginas)
    for k in (-1, 1):
        img = pincelada(img, 22 + k * 9, 11, k * 0.22, 1, 6, 2.0, ORO, BLANCO, 0.85)

    img = contorno(img)
    img = rim(img, BLANCO, 0.3)
    chispa(img, 3, 20); chispa(img, 41, 21)
    return img

# ======================================================================
#  13 — LA BOLSA DE LAS ARMAS DEFINITIVAS (30×30)
# ======================================================================
def gen_bolsa():
    s = 30
    img = Image.new('RGBA', (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # ===== EL COSTAL (la forma clásica de saco: cuello atado + panza) =====
    # LA PANZA (el cuerpo que cuelga — FLAT, 3 tonos de cuero)
    d.polygon([(4, 10), (26, 10), (28, 18), (25, 28), (5, 28), (2, 18)], fill=(232, 164, 76, 255))
    # la sombra baja (el cuero viejo)
    d.polygon([(3, 20), (27, 20), (25, 28), (5, 28)], fill=(178, 116, 48, 255))
    # el brillo alto (la luz del alba)
    d.polygon([(5, 11), (25, 11), (26, 15), (4, 15)], fill=(255, 236, 178, 255))
    # EL PLIEUE central (la arruga del saco)
    d.line([(15, 16), (15, 27)], fill=(178, 116, 48, 255), width=1)
    d.line([(9, 15), (8, 26)], fill=(210, 146, 64, 255), width=1)
    d.line([(21, 15), (22, 26)], fill=(210, 146, 64, 255), width=1)

    # ===== EL CUELLO ATADO (la gorguera del saco) =====
    d.rectangle([11, 4, 19, 11], fill=(196, 132, 56, 255))
    d.rectangle([12, 5, 18, 10], fill=(255, 214, 120, 255))
    # LA ATADURA (el cordón apretado — dos orejas a los lados)
    d.rectangle([9, 8, 21, 11], fill=(120, 70, 26, 255))
    d.rectangle([10, 9, 20, 10], fill=(255, 240, 200, 255))
    # las orejas del nudo
    d.polygon([(9, 8), (5, 6), (8, 11)], fill=(150, 96, 40, 255))
    d.polygon([(21, 8), (25, 6), (22, 11)], fill=(150, 96, 40, 255))

    # ===== EL SELLO (la estrella del veredicto — blanca, al centro de la panza) =====
    img = estrella4(img, 15, 21, 6, (255, 252, 244))
    # el rim violeta de la casa (la firma)
    d = ImageDraw.Draw(img)
    d.arc([4, 10, 26, 28], 25, 155, fill=(120, 60, 210, 255), width=1)

    img = contorno(img, (20, 10, 30, 255), 70)
    chispa(img, 2, 16); chispa(img, 28, 15)
    return img

# ======================================================================
#  LA TIRILLA FINAL (todo a disco + hoja de contacto para el VLM)
# ======================================================================
if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    generadores = [
        ('OjoDelJuicio', gen_ojo), ('PrismaPrimordial', gen_prisma),
        ('LluviaDeVersos', gen_versos), ('FiloConstelado', gen_filo),
        ('PartidorDeMundos', gen_partidor), ('GuadanaViejoDios', gen_guadana),
        ('SistemaAethon', gen_sistema), ('ConstelacionJuicio', gen_constelacion),
        ('VorticeDevorador', gen_vortice), ('RequiemEstelar', gen_requiem),
        ('IraDelCielo', gen_ira), ('TestamentoAethon', gen_testamento),
    ]
    for nombre, fn in generadores:
        img = fn()
        img.save(f'{OUT}/{nombre}.png')
        print(f'{nombre}.png {img.size}')

    bolsa = gen_bolsa()
    bolsa.save(f'{OUT_BOLSA}/BolsaDefinitivas.png')
    print(f'BolsaDefinitivas.png {bolsa.size}')

    # LA HOJA DE CONTACTO ×4 (para el ojo del VLM)
    nombres = [n for n, _ in generadores] + ['BolsaDefinitivas']
    celdas = []
    for n in nombres:
        ruta = f'{OUT_BOLSA}/BolsaDefinitivas.png' if n == 'BolsaDefinitivas' else f'{OUT}/{n}.png'
        im = Image.open(ruta).convert('RGBA')
        im = im.resize((im.width * 4, im.height * 4), Image.NEAREST)
        celdas.append((n, im))
    W = max(im.width for _, im in celdas) + 8
    H = sum(im.height + 18 for _, im in celdas) + 8
    hoja = Image.new('RGBA', (W, H), (36, 30, 48, 255))
    dd = ImageDraw.Draw(hoja)
    y = 4
    for n, im in celdas:
        hoja.paste(im, (4, y), im)
        dd.text((im.width + 12, y + im.height // 2 - 4), n, fill=(255, 240, 210, 255))
        y += im.height + 18
    hoja.save('/home/z/my-project/tools/tools/definitivas_v65066_hoja.png')
    print('hoja de contacto: tools/tools/definitivas_v65066_hoja.png')
