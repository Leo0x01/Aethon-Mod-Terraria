#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
genera_icono_renglon.py — EL ICONO DE «EL RENGLÓN FINAL» (v6.93).
44×44 dibujado a 6× (264×264) y reducido con LANCZOS — el estilo de la
casa: el grimorio abierto DEBAJO, el renglón de tinta ATRAVESÁNDOLO y
EL PUNTO FINAL (la estrella de 4 puntas) al final de la línea.
"""
from PIL import Image, ImageDraw, ImageFilter
import math

S = 6                      # supersampling
W = 44 * S                 # 264

def P(v):
    """coordenada en espacio 44 → espacio lienzo."""
    return int(round(v * S))

def colori(r, g, b, a=255):
    return (int(r), int(g), int(b), int(a))

img = Image.new('RGBA', (W, W), (0, 0, 0, 0))

# ------------------------------------------------------------------
# 1. EL GRIMORIO ABIERTO — la tapa violeta profundo con filo dorado
#    (vive en la mitad baja del icono, bajo el renglón)
# ------------------------------------------------------------------
libro = Image.new('RGBA', (W, W), (0, 0, 0, 0))
d = ImageDraw.Draw(libro)
# la tapa (rectángulo redondeado)
d.rounded_rectangle([P(5), P(26.5), P(39), P(42.5)], radius=P(3),
                    fill=colori(26, 13, 44, 255),
                    outline=colori(214, 158, 66, 255), width=1)
# el canto dorado del lomo bajo las hojas
d.line([P(22), P(28), P(22), P(41.5)], fill=colori(120, 84, 30, 255), width=1)

# ------------------------------------------------------------------
# 2. LAS HOJAS ABIERTAS — dos cuadriláteros violeta pálido
# ------------------------------------------------------------------
# hoja izquierda
d.polygon([(P(22), P(28)), (P(6.5), P(29.5)), (P(6.5), P(40.5)), (P(22), P(39.5))],
          fill=colori(224, 213, 244, 255))
# hoja derecha
d.polygon([(P(22), P(28)), (P(37.5), P(29.5)), (P(37.5), P(40.5)), (P(22), P(39.5))],
          fill=colori(210, 196, 236, 255))
# pliegues del lomo
d.polygon([(P(22), P(28)), (P(22), P(39.5)), (P(20.5), P(39)), (P(20.5), P(28.5))],
          fill=colori(178, 160, 214, 255))
d.polygon([(P(22), P(28)), (P(22), P(39.5)), (P(23.5), P(39)), (P(23.5), P(28.5))],
          fill=colori(178, 160, 214, 255))
# las líneas de texto de las hojas (tinta vieja)
for (x1, x2, y, a) in [
        (8.5, 19.5, 31.5, 205), (9, 20, 33.5, 165), (8.5, 18.5, 35.5, 205),
        (24.5, 35.5, 31.5, 205), (25, 36, 33.5, 165), (25.5, 35, 35.5, 205)]:
    d.line([P(x1), P(y), P(x2), P(y)], fill=colori(120, 92, 176, a), width=1)

img.alpha_composite(libro)

# ------------------------------------------------------------------
# 3. EL RENGLÓN — la barra de tinta que atraviesa la página
# ------------------------------------------------------------------
haz = Image.new('RGBA', (W, W), (0, 0, 0, 0))
dh = ImageDraw.Draw(haz)
yc = 21          # la altura del renglón (44-space)
# halo violeta (lo más ancho, difuso)
dh.rounded_rectangle([P(1), P(yc - 3.6), P(43), P(yc + 3.6)],
                     radius=P(3.6), fill=colori(122, 58, 220, 185))
haz = haz.filter(ImageFilter.GaussianBlur(3 * S))
# la tinta violeta
d2 = ImageDraw.Draw(haz)
d2.rounded_rectangle([P(1.5), P(yc - 2.2), P(42.5), P(yc + 2.2)],
                     radius=P(2.2), fill=colori(168, 84, 255, 235))
haz = haz.filter(ImageFilter.GaussianBlur(1.2 * S))
# el oro del renglón
d3 = ImageDraw.Draw(haz)
d3.rounded_rectangle([P(2.5), P(yc - 1.3), P(41.5), P(yc + 1.3)],
                     radius=P(1.3), fill=colori(255, 208, 116, 250))
# el núcleo blanco cálido
d4 = ImageDraw.Draw(haz)
d4.rounded_rectangle([P(3.5), P(yc - 0.7), P(40.5), P(yc + 0.7)],
                     radius=P(0.7), fill=colori(255, 246, 222, 255))
# el charco dorado del escritor (el centro del renglón)
d5 = ImageDraw.Draw(haz)
d5.ellipse([P(22 - 5.5), P(yc - 4.5), P(22 + 5.5), P(yc + 4.5)],
           fill=colori(255, 216, 130, 165))
haz = haz.filter(ImageFilter.GaussianBlur(1.5 * S))
img.alpha_composite(haz)

# ------------------------------------------------------------------
# 4. EL PUNTO FINAL — la estrella de 4 puntas al final de la línea
# ------------------------------------------------------------------
def estrella(cx, cy, r, w):
    pts = []
    for i in range(8):
        ang = math.pi / 2 * (i / 2) - math.pi / 2
        rr = r if i % 2 == 0 else w
        pts.append((P(cx + rr * math.cos(ang)), P(cy + rr * math.sin(ang))))
    return pts

glowf = Image.new('RGBA', (W, W), (0, 0, 0, 0))
dg = ImageDraw.Draw(glowf)
dg.ellipse([P(40 - 8), P(yc - 8), P(40 + 8), P(yc + 8)],
           fill=colori(255, 214, 120, 120))
glowf = glowf.filter(ImageFilter.GaussianBlur(3 * S))
img.alpha_composite(glowf)

star = Image.new('RGBA', (W, W), (0, 0, 0, 0))
ds = ImageDraw.Draw(star)
ds.polygon(estrella(40, yc, 7.5, 1.6), fill=colori(255, 236, 180, 255))
ds.polygon(estrella(40, yc, 4.2, 1.1), fill=colori(255, 252, 240, 255))
img.alpha_composite(star)

# la pluma del inicio (la otra punta): un destello menor violeta
star2 = Image.new('RGBA', (W, W), (0, 0, 0, 0))
ds2 = ImageDraw.Draw(star2)
ds2.polygon(estrella(3, yc, 3.6, 0.9), fill=colori(206, 140, 255, 235))
img.alpha_composite(star2)

# ------------------------------------------------------------------
# 5. LAS CHISPAS DE TINTA — puntitos dorados y violetas sobre la página
# ------------------------------------------------------------------
chis = Image.new('RGBA', (W, W), (0, 0, 0, 0))
dc = ImageDraw.Draw(chis)
for (x, y, r, col) in [
        (10, 12.5, 1.1, (255, 224, 150, 230)),
        (36, 14, 1.0, (196, 128, 255, 220)),
        (17, 17, 0.9, (168, 84, 255, 200)),
        (29, 16, 0.9, (255, 232, 170, 210)),
        (23, 10.5, 1.0, (216, 150, 255, 200)),
        (40, 24, 0.8, (255, 224, 150, 190)),
        (5, 26, 0.8, (196, 128, 255, 180))]:
    dc.ellipse([P(x - r), P(y - r), P(x + r), P(y + r)], fill=col)
img.alpha_composite(chis)

# ------------------------------------------------------------------
# 6. LA REDUCCIÓN — 264 → 44 con LANCZOS
# ------------------------------------------------------------------
final = img.resize((44, 44), Image.LANCZOS)
final.save('AethonMod/Content/Weapons/Definitivas/RenglonFinal.png')
print('RenglonFinal.png 44x44 OK')
