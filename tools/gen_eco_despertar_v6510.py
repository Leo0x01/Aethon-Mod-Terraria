#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# ======================================================================
#  gen_eco_despertar_v6510.py — LA TEXTURA DEL ECO DEL DESPERTAR.
#
#  La llave de pruebas de EL DESPERTAR (v6.51.0): un SIGILO RUNICO
#  despierto — el anillo de oro rúnico de los soles (la matemática de
#  SigiloLib: el aro con sus glifos) rodeando un NÚCLEO VIOLETA que
#  está ABRIÉNDOSE (el ojo del grimorio entreabriendo: la pupila clara
#  asomando entre párpados de tinta). 24×24 px, contorno oscuro estilo
#  Terraria, fondo transparente.
# ======================================================================
import os
import math

from PIL import Image

BASE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
DEST = os.path.normpath(os.path.join(
    BASE, "AethonMod", "Content", "Items", "Llamados", "EcoDelDespertar.png"))

W, H = 24, 24
CX, CY = 12, 12

OUTLINE = (26, 14, 40, 255)        # el contorno oscuro de la casa
ORO = (240, 196, 96, 255)          # el oro de los soles rúnicos
ORO_CLARO = (255, 232, 160, 255)   # el brillo del oro
ORO_OSCURO = (170, 126, 48, 255)
VIOLETA = (150, 90, 230, 255)      # el núcleo despierto
VIOLETA_CLARO = (208, 168, 255, 255)
TINTA = (30, 14, 52, 255)          # la tinta de los párpados
PUPILA = (238, 228, 252, 255)      # la luz que asoma

R_EXTERIOR = 10.4    # el borde del aro
R_INTERIOR = 7.2     # el hueco del aro
R_NUCLEO = 5.4       # el núcleo violeta
R_PUPILA = 2.2       # la pupila que abre


def main():
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = im.load()

    for y in range(H):
        for x in range(W):
            dx, dy = x - CX, y - CY
            d = math.hypot(dx, dy)
            ang = math.atan2(dy, dx)

            # ===== EL ARO DORADO =====
            if R_INTERIOR <= d <= R_EXTERIOR:
                # LOS GLIFOS: 8 runas alrededor (ondulación del radio —
                # la geometría de los anillos de la casa).
                glifo = 0.55 + 0.45 * math.cos(ang * 8.0)
                if d >= R_EXTERIOR - 1.2 or d <= R_INTERIOR + 1.0:
                    c = OUTLINE
                elif glifo > 0.86:
                    c = ORO_CLARO
                elif glifo < 0.30:
                    c = ORO_OSCURO
                else:
                    c = ORO
                px[x, y] = c
                continue

            # ===== EL NÚCLEO: el ojo entreabierto =====
            if d <= R_NUCLEO:
                if d >= R_NUCLEO - 1.0:
                    px[x, y] = OUTLINE          # el borde del núcleo
                    continue
                # LOS PÁRPADOS DE TINTA: la apertura vertical en el centro
                # (el ojo ABRIÉNDOSE: tinta arriba y abajo, luz al centro).
                apertura = 2.6 * math.exp(-((x - CX) ** 2) / 6.0)
                if abs(dy) <= apertura:
                    px[x, y] = PUPILA if d <= R_PUPILA + 1.6 else VIOLETA_CLARO
                elif abs(dy) <= apertura + 1.2:
                    px[x, y] = VIOLETA
                else:
                    px[x, y] = TINTA
                continue

    # ===== EL DESTELLO (la chispa del despertar, esquina NE) =====
    chispa = [(18, 4), (19, 5), (17, 3), (20, 6)]
    for (x, y) in chispa:
        if 0 <= x < W and 0 <= y < H:
            px[x, y] = (255, 244, 200, 255)

    im.save(DEST)
    print("OK —", DEST, im.size)


if __name__ == "__main__":
    main()
