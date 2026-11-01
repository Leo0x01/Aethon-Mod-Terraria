#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.77 — LA BOLSA DEL CRECIMIENTO: los PNG del kit (15 nuevos).

14 iconos de ítem (28×30, como las Ideas de la v6.50.19) + la bolsa
(24×26, como las 25 bolsas del arsenal). El estilo de la casa: formas
simples distinguibles, glow radial suave (smoothstep) y la paleta viva
de cada arma — un icono por idea de crecimiento.
"""

from PIL import Image, ImageDraw
import math

W, H = 28, 30      # ítems (el estándar de las Ideas)
BW, BH = 24, 26    # la bolsa (el estándar del arsenal)


def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3.0 - 2.0 * t)


def glow(draw_img, cx, cy, radio, nucleo, borde, r_n=0.35):
    """El glow radial de la casa (el patrón de todos los iconos)."""
    im = draw_img
    for y in range(im.height):
        for x in range(im.width):
            d = math.hypot(x - cx, y - cy) / max(radio, 1.0)
            if d > 1.0:
                continue
            f = smoothstep(r_n, 1.0, d)          # 0 dentro → 1 al borde
            r = int(nucleo[0] + (borde[0] - nucleo[0]) * f)
            g = int(nucleo[1] + (borde[1] - nucleo[1]) * f)
            b = int(nucleo[2] + (borde[2] - nucleo[2]) * f)
            a = im.getpixel((x, y))[3]
            if a == 0:
                im.putpixel((x, y), (r, g, b, 255))


def base():
    return Image.new("RGBA", (W, H), (0, 0, 0, 0))


def guardar(img, nombre):
    img.save(f"AethonMod/Content/Weapons/Cosmic/{nombre}.png")
    print("  ", nombre, "OK")


# ============ 1 · LA SEMILLA RÚNICA (dorado-violeta) ============
im = base()
glow(im, 14, 15, 13, (255, 250, 230), (110, 80, 220))
d = ImageDraw.Draw(im)
# la runa: glifo estrella de 4 puntas
d.polygon([(14, 3), (17, 12), (26, 15), (17, 18), (14, 27), (11, 18), (2, 15), (11, 12)],
          fill=(255, 240, 200, 255))
# el brote: la hojita que germina (la letra: «la runa GERMINA»)
d.ellipse((12, 4, 16, 8), fill=(190, 255, 205, 255))
guardar(im, "CrecimientoSemillaRunica")

# ============ 2 · LA PALABRA HUECA (ámbar) ============
im = base()
glow(im, 14, 15, 13, (255, 245, 215), (200, 120, 40))
d = ImageDraw.Draw(im)
# el glifo rectangular (la palabra) + sus versos
d.rectangle((9, 8, 19, 22), fill=(255, 235, 185, 255))
d.line((11, 12, 17, 12), fill=(120, 70, 20, 255), width=2)
d.line((11, 16, 17, 16), fill=(120, 70, 20, 255), width=2)
d.line((11, 20, 15, 20), fill=(120, 70, 20, 255), width=2)
guardar(im, "CrecimientoPalabraHueca")

# ============ 3 · LA VELA QUE ENGREÑE (naranja fuego) ============
im = base()
glow(im, 14, 12, 13, (255, 235, 200), (220, 110, 30))
d = ImageDraw.Draw(im)
# la llama (gota invertida) sobre la base del libro
d.polygon([(14, 3), (19, 12), (17, 18), (11, 18), (9, 12)], fill=(255, 225, 160, 255))
d.ellipse((11, 10, 17, 17), fill=(255, 250, 235, 255))
d.rectangle((7, 19, 21, 26), fill=(120, 80, 200, 255))   # el libro-base
d.rectangle((9, 21, 19, 24), fill=(70, 45, 140, 255))
guardar(im, "CrecimientoVelaEngrine")

# ============ 4 · EL COMETA DE PALABRAS (azul hielo) ============
im = base()
glow(im, 14, 15, 13, (235, 245, 255), (90, 140, 230))
d = ImageDraw.Draw(im)
# la cabeza pequeña (nace diminuto) y la cola que crece
d.ellipse((16, 5, 24, 13), fill=(240, 250, 255, 255))
d.ellipse((10, 13, 15, 18), fill=(185, 215, 255, 255), outline=None)
d.ellipse((5, 17, 9, 21), fill=(150, 190, 255, 255))
d.ellipse((1, 20, 4, 23), fill=(120, 165, 255, 255))
guardar(im, "CrecimientoCometaPalabras")

# ============ 5 · EL SELLO DIGITAL (violeta rúnico) ============
im = base()
glow(im, 14, 15, 13, (255, 245, 225), (140, 90, 230))
d = ImageDraw.Draw(im)
# la rueda: círculo con cruz + las páginas (6 marcas)
d.ellipse((4, 5, 24, 25), outline=(240, 220, 255, 255), width=2)
d.line((14, 7, 14, 23), fill=(240, 220, 255, 255), width=2)
d.line((6, 15, 22, 15), fill=(240, 220, 255, 255), width=2)
for i in range(6):
    a = math.pi * 2 * i / 6 - math.pi / 2
    d.ellipse((14 + 10 * math.cos(a) - 1.5, 15 + 10 * math.sin(a) - 1.5,
               14 + 10 * math.cos(a) + 1.5, 15 + 10 * math.sin(a) + 1.5),
              fill=(255, 232, 170, 255))
guardar(im, "CrecimientoSelloDigital")

# ============ 6 · EL LÁTIGO DE PERLIN (azul eléctrico) ============
im = base()
glow(im, 14, 15, 13, (240, 250, 255), (60, 120, 255))
d = ImageDraw.Draw(im)
# el zigzag del brazo (perlin en miniatura) con la punta brillante
pts = [(3, 24), (10, 16), (15, 20), (23, 7)]
d.line(pts, fill=(200, 230, 255, 255), width=3, joint="curve")
d.line(pts, fill=(255, 255, 255, 255), width=1)
d.ellipse((20, 4, 26, 10), fill=(240, 250, 255, 255))
guardar(im, "CrecimientoLatigoPerlin")

# ============ 7 · EL DEVORADOR MENOR (rosa-morado) ============
im = base()
glow(im, 14, 15, 13, (250, 235, 255), (170, 80, 220))
d = ImageDraw.Draw(im)
# la boquita abierta (media luna) con sus dientes
d.pieslice((3, 6, 25, 26), 200, 340, fill=(240, 215, 255, 255))
d.polygon([(7, 13), (9, 8), (11, 13)], fill=(90, 40, 160, 255))
d.polygon([(13, 11), (15, 7), (17, 11)], fill=(90, 40, 160, 255))
d.polygon([(19, 12), (21, 8), (23, 12)], fill=(90, 40, 160, 255))
guardar(im, "CrecimientoDevoradorMenor")

# ============ 8 · LA ESTRELLA DOMÉSTICA (blanco-dorado) ============
im = base()
glow(im, 14, 15, 13, (255, 252, 240), (235, 190, 80))
d = ImageDraw.Draw(im)
# el destello de 4 puntas cayendo (la puntita de arriba)
d.polygon([(14, 2), (17, 11), (25, 15), (17, 19), (14, 28), (11, 19), (3, 15), (11, 11)],
          fill=(255, 248, 225, 255))
d.ellipse((12, 12, 16, 17), fill=(255, 255, 250, 255))
guardar(im, "CrecimientoEstrellaDomestica")

# ============ 9 · LA PÁGINA CORTANTE (lila) ============
im = base()
glow(im, 14, 15, 13, (250, 245, 255), (150, 110, 235))
d = ImageDraw.Draw(im)
# la hoja doblada como daga (sesgada) con la línea del doblez
d.polygon([(5, 6), (22, 9), (24, 25), (7, 22)], fill=(245, 240, 255, 255))
d.line((13, 7, 15, 24), fill=(120, 80, 210, 255), width=2)
d.polygon([(7, 22), (11, 23), (9, 26)], fill=(255, 255, 255, 255))  # la punta
guardar(im, "CrecimientoPaginaCortante")

# ============ 10 · EL PRISMA FRAGMENTADO (turquesa) ============
im = base()
glow(im, 14, 15, 13, (235, 255, 250), (40, 190, 175))
d = ImageDraw.Draw(im)
# el triángulo partido en dos mitades (el hueco del corte)
d.polygon([(4, 24), (12, 24), (12, 7)], fill=(200, 255, 245, 255))
d.polygon([(16, 24), (24, 24), (16, 12)], fill=(150, 240, 230, 255))
d.line((14, 5, 14, 25), fill=(255, 255, 255, 255), width=1)  # el destello de la fisura
guardar(im, "CrecimientoPrismaFragmentado")

# ============ 11 · LA RUNA ORBITAL (ámbar orbitando) ============
im = base()
glow(im, 14, 15, 13, (255, 250, 235), (225, 160, 60))
d = ImageDraw.Draw(im)
# el centro (el libro) + los 3 glifos orbitando
d.ellipse((10, 11, 18, 19), fill=(255, 244, 215, 255))
for i, a in enumerate([0.4, 2.5, 4.6]):
    x = 14 + 10 * math.cos(a); y = 15 + 9 * math.sin(a)
    r = 2.0 + i * 0.6   # crecen con la órbita
    d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 228, 175, 255))
guardar(im, "CrecimientoRunaOrbital")

# ============ 12 · LA ESPORA DEL ESCRIBA (verde lima) ============
im = base()
glow(im, 14, 15, 13, (235, 255, 235), (60, 200, 90))
d = ImageDraw.Draw(im)
# el hongo: sombrero + tallo (la espora que crece en silencio)
d.pieslice((4, 4, 24, 20), 180, 360, fill=(205, 255, 210, 255))
d.rectangle((11, 14, 17, 26), fill=(240, 255, 240, 255))
d.ellipse((6, 8, 9, 11), fill=(255, 255, 255, 255))
d.ellipse((19, 7, 22, 10), fill=(255, 255, 255, 255))
guardar(im, "CrecimientoEsporaDelEscriba")

# ============ 13 · EL ECO DEL TRAZO (azul tinta) ============
im = base()
glow(im, 14, 15, 13, (245, 250, 255), (90, 100, 230))
d = ImageDraw.Draw(im)
# la S del eco: la ida y el regreso (la estela que se vuelve cuerpo)
d.arc((3, 5, 25, 25), 250, 70, fill=(210, 220, 255, 255), width=3)
d.arc((8, 10, 20, 22), 60, 240, fill=(160, 175, 255, 255), width=3)
d.ellipse((21, 6, 25, 10), fill=(255, 255, 255, 255))  # la cabeza del látigo
guardar(im, "CrecimientoEcoDelTrazo")

# ============ 14 · LA TINTA VIVA (morado tinta) ============
im = base()
glow(im, 14, 15, 13, (245, 240, 255), (110, 85, 225))
d = ImageDraw.Draw(im)
# la gota (semicírculo con punta arriba) — la opacidad crece al centro
d.polygon([(14, 3), (20, 12), (23, 20), (14, 27), (5, 20), (8, 12)],
          fill=(160, 135, 235, 255))
d.ellipse((9, 14, 19, 24), fill=(225, 218, 255, 255))   # el núcleo avivado
d.ellipse((12, 17, 16, 21), fill=(255, 255, 255, 255))
guardar(im, "CrecimientoTintaViva")

# ============ LA BOLSA (24×26, el estándar del arsenal) ============
im = Image.new("RGBA", (BW, BH), (0, 0, 0, 0))
glow(im, 12, 13, 12, (235, 255, 240), (30, 160, 90))
d = ImageDraw.Draw(im)
# el saco con el cuello fruncido (el patrón de las 26 bolsas)
d.polygon([(4, 10), (20, 10), (22, 13), (21, 24), (3, 24), (2, 13)],
          fill=(70, 150, 105, 255))
d.rectangle((8, 6, 16, 11), fill=(50, 120, 85, 255))
d.line((8, 6, 16, 6), fill=(255, 255, 255, 255), width=1)
# el brote del crecimiento (el glifo de la casa): la hojita que sube
d.line((12, 20, 12, 12), fill=(235, 255, 240, 255), width=2)
d.ellipse((12, 11, 17, 16), fill=(190, 255, 205, 255))
d.ellipse((7, 13, 12, 18), fill=(160, 245, 180, 255))
im.save("AethonMod/Content/Items/Bolsas/BolsaDelCrecimiento.png")
print("   BolsaDelCrecimiento OK (24x26)")

print("\n15 PNG del kit del crecimiento listos.")
