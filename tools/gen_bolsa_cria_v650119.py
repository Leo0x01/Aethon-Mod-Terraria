#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# ======================================================================
#  gen_bolsa_cria_v650119.py — LA TEXTURA DE LA BOLSA DE LA CRÍA.
#
#  La letra del usuario (v6.50.119): «crea una bolsa donde esten las
#  mascotas, las 2» — el saco que contiene a la pareja del recuerdo:
#  MITAD DORADA (la cría de luz) y MITAD VIOLETA OSCURA (la gemela),
#  cosidas por una costura oscura, con el lazo morado del grimorio y
#  UNA chispa de cada mitad. 30×30 (la convención de las bolsas de la
#  casa), contorno oscuro estilo Terraria, fondo transparente.
# ======================================================================
import hashlib
import os

from PIL import Image

BASE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
DEST = os.path.normpath(os.path.join(
    BASE, "AethonMod", "Content", "Items", "Llamados", "BolsaDeLaCria.png"))

W = H = 30
CX = 15  # el centro del saco (0..29 → 14.5: el 15 carga la costura)

# La silueta del saco por fila (y → mitad del ancho).
SACO = {
    4: 3, 5: 4, 6: 5, 7: 5, 8: 6, 9: 7,
    10: 8, 11: 10, 12: 11, 13: 12, 14: 12, 15: 12, 16: 12, 17: 11,
    18: 11, 19: 10, 20: 10, 21: 9, 22: 8, 23: 7, 24: 6, 25: 6,
    26: 5, 27: 4, 28: 4, 29: 3,
}

OUTLINE = (26, 15, 38, 255)     # el contorno oscuro de la casa
SEAM = (44, 22, 74, 255)        # la costura que cose a la pareja
RIBBON = (150, 60, 210, 255)    # el lazo morado del grimorio
RIBBON_OS = (98, 38, 150, 255)  # la sombra del lazo
KNOT = (198, 120, 255, 255)     # el nudo del lazo
SPARK_L = (255, 255, 225, 255)  # la chispa de LUZ
SPARK_O = (225, 190, 255, 255)  # la chispa de OSCURIDAD
GLINT = (255, 255, 255, 255)    # el brillo blanco
EAR_L = (176, 124, 64, 255)     # la oreja dorada del saco atado
EAR_O = (86, 40, 160, 255)      # la oreja violeta


def lerp(a, b, f):
    return tuple(int(a[i] + (b[i] - a[i]) * f) for i in range(3)) + (255,)


def cuerpo(x, y):
    """El color del cuerpo del saco: mitad dorada, mitad violeta, con
    rampa hacia abajo y sombra en los bordes exteriores."""
    f = (y - 4) / 25.0
    if x < CX:
        c = lerp((255, 232, 160), (196, 138, 80), f)
    elif x == CX:
        return SEAM
    else:
        c = lerp((140, 72, 235), (60, 24, 110), f)
    # la sombra de volumen: los 2 px exteriores de cada fila.
    w = SACO[y]
    if abs(x - CX) >= w - 1:
        k = 0.74 if x < CX else 0.70
        c = tuple(int(c[i] * k) for i in range(3)) + (255,)
    return c


def main():
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = img.load()

    # 1) EL CUERPO DEL SACO.
    for y, w in SACO.items():
        for x in range(CX - w, CX + w + 1):
            px[x, y] = cuerpo(x, y)

    # 2) EL LAZO (la atadura del cuello, y=3..7).
    lazo = {
        3: (12, 20), 4: (10, 21), 5: (10, 21), 6: (11, 20), 7: (12, 19),
    }
    for y, (x0, x1) in lazo.items():
        for x in range(x0, x1 + 1):
            borde = x == x0 or x == x1 or y == 3 or y == 7
            px[x, y] = RIBBON_OS if borde else RIBBON
    # el nudo brillante del centro.
    for y in (4, 5, 6):
        for x in (14, 15, 16):
            if (x, y) != (14, 6) and (x, y) != (16, 6):
                px[x, y] = KNOT

    # 3) LAS OREJAS (las puntas del saco atado, asomando sobre el lazo).
    for (x, y), c in {(6, 10): EAR_L, (6, 9): EAR_L, (7, 8): EAR_L,
                      (24, 10): EAR_O, (24, 9): EAR_O, (23, 8): EAR_O}.items():
        px[x, y] = c

    # 4) LAS CHISPAS (una por mitad — la pareja) y el brillo.
    for x in (5, 6):
        for y in (13, 14):
            px[x, y] = SPARK_L
    for x in (23, 24):
        for y in (16, 17):
            px[x, y] = SPARK_O
    px[17, 12] = GLINT
    px[18, 12] = GLINT

    # 5) EL CONTORNO OSCURO (todo píxel vacío que toque la silueta).
    solid = {(x, y) for y in range(H) for x in range(W) if px[x, y][3] > 0}
    contorno = set()
    for (x, y) in solid:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < W and 0 <= ny < H and (nx, ny) not in solid:
                contorno.add((nx, ny))
    for (x, y) in contorno:
        px[x, y] = OUTLINE

    img.save(DEST)

    # El reporte de la casa: tamaño + md5 + vista ASCII.
    with open(DEST, "rb") as f:
        dato = f.read()
    md5 = hashlib.md5(dato).hexdigest()
    print(f"BolsaDeLaCria.png → {DEST}")
    print(f"  {img.size[0]}x{img.size[1]} · {len(dato)} B · md5 {md5}")
    print("  (G=oro L=lazo O=oscura #=contorno ·=chispa)")
    for y in range(H):
        fila = []
        for x in range(W):
            r, g, b, a = px[x, y]
            if a == 0:
                fila.append(" ")
            elif (r, g, b) == OUTLINE[:3]:
                fila.append("#")
            elif (r, g, b) in (RIBBON[:3], RIBBON_OS[:3], KNOT[:3]):
                fila.append("L")
            elif (r, g, b) in (SPARK_L[:3], GLINT[:3]):
                fila.append("·")
            elif x < CX:
                fila.append("G")
            else:
                fila.append("O")
        print("  " + "".join(fila))


if __name__ == "__main__":
    main()
