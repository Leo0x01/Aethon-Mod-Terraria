#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# ======================================================================
#  gen_tinta_cometa_v650126.py — LA TEXTURA DE LA TINTA DEL COMETA.
#
#  La letra del usuario (v6.50.126): «crea un item para activar el
#  evento, este será un item de prueba» — EL FRASCO DEL CIELO: un
#  viale de vidrio violeta con la TINTA NEGRA Y MORADA del cometa
#  (la misma del cráter), una estrella de 4 puntas cayendo en
#  diagonal DENTRO de la tinta (el cometa en miniatura) y su corcho.
#  26×30 px, contorno oscuro estilo Terraria, fondo transparente.
# ======================================================================
import os

from PIL import Image

BASE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
DEST = os.path.normpath(os.path.join(
    BASE, "AethonMod", "Content", "Items", "Llamados", "TintaDelCometa.png"))

W, H = 26, 30
CX = 13

OUTLINE = (24, 12, 36, 255)      # el contorno oscuro de la casa
VIDRIO = (186, 156, 228, 255)    # el vidrio violeta
VIDRIO_OSCURO = (128, 96, 182, 255)
BRILLO = (236, 226, 255, 255)    # el brillo del vidrio
CORCHO = (168, 124, 78, 255)     # el corcho
CORCHO_OSCURO = (118, 84, 50, 255)
TINTA_ALTA = (52, 24, 88, 255)   # la tinta arriba (más morada)
TINTA_MEDIA = (34, 12, 60, 255)
TINTA_FONDO = (16, 5, 26, 255)   # la tinta abajo (casi negra)
ESTRELLA = (232, 214, 255, 255)  # el núcleo de la estrella
ESTRELLA_HALO = (150, 60, 210, 255)  # el halo del cometa
COLA = (108, 40, 172, 255)       # la cola del cometa

# La silueta por fila (y → mitad del ancho desde CX):
#   y2-6 corcho · y7-8 cuello · y9-27 cuerpo · y28 base redondeada
SILUETA = {
    2: 2, 3: 2, 4: 3, 5: 3, 6: 3,           # corcho
    7: 2, 8: 2,                              # cuello
    9: 3, 10: 4, 11: 5, 12: 5,               # hombros
    13: 6, 14: 6, 15: 7, 16: 7, 17: 7, 18: 7,
    19: 7, 20: 8, 21: 8, 22: 8, 23: 8, 24: 8,
    25: 8, 26: 8, 27: 8, 28: 7, 29: 6,
}


def dentro(x, y):
    m = SILUETA.get(y)
    return m is not None and abs(x - CX) <= m


def main():
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = im.load()

    # ===== EL FRASCO (silueta → contorno + interior) =====
    for y in range(H):
        m = SILUETA.get(y)
        if m is None:
            continue
        for x in range(CX - m - 1, CX + m + 2):
            if not dentro(x, y):
                continue
            borde = (abs(x - CX) == m) or (not dentro(x, y - 1)) or \
                    (not dentro(x, y + 1)) or \
                    (y == 2) or (y == 29 and abs(x - CX) >= 4)
            if borde:
                px[x, y] = OUTLINE
            elif y <= 6:
                # EL CORCHO (con su veta)
                px[x, y] = CORCHO if (x % 3 != 0) else CORCHO_OSCURO
            elif y <= 8:
                # EL CUELLO de vidrio (transparente: solo bordes)
                px[x, y] = VIDRIO if abs(x - CX) >= m else (0, 0, 0, 0)
            else:
                # EL CUERPO: tinta que se oscurece hacia el fondo
                t = (y - 9) / 19.0
                if t < 0.33:
                    px[x, y] = TINTA_ALTA
                elif t < 0.66:
                    px[x, y] = TINTA_MEDIA
                else:
                    px[x, y] = TINTA_FONDO
                # El vidrio se ve en los LATERALES internos.
                if abs(x - CX) == m - 1:
                    px[x, y] = VIDRIO_OSCURO
                if abs(x - CX) == m - 2 and y <= 13:
                    px[x, y] = TINTA_ALTA

    # ===== EL COMETA DENTRO DE LA TINTA (estrella 4 puntas + cola) =====
    ex, ey = 15, 17   # el núcleo (dentro del cuerpo)
    for y in range(11, 26):
        for x in range(4, 22):
            if not dentro(x, y) or y <= 8:
                continue
            d = abs(x - ex) + abs(y - ey)
            if d == 0:
                px[x, y] = ESTRELLA
            elif d <= 1:
                px[x, y] = ESTRELLA if (abs(x - ex) + abs(y - ey)) <= 1 else ESTRELLA_HALO
            elif d == 2 and (abs(x - ex) == 2 or abs(y - ey) == 2):
                px[x, y] = ESTRELLA_HALO
    # LA COLA: diagonal hacia abajo-izquierda desde el núcleo.
    for i in range(1, 8):
        x, y = ex - i, ey + i
        if dentro(x, y) and y > 8:
            px[x, y] = COLA if i < 6 else ESTRELLA_HALO
            if i < 5 and dentro(x + 1, y):
                px[x + 1, y] = COLA

    # ===== EL BRILLO DEL VIDRIO (arriba-izquierda) =====
    px[CX - 5, 12] = BRILLO
    px[CX - 4, 12] = BRILLO
    px[CX - 5, 13] = BRILLO

    os.makedirs(os.path.dirname(DEST), exist_ok=True)
    im.save(DEST)
    print(f"TintaDelCometa.png {im.size} -> {DEST}")


if __name__ == "__main__":
    main()
