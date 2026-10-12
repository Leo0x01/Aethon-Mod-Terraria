#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_pagina_blanca_v650112.py — LA TEXTURA DE LA PÁGINA EN BLANCO (24×24).

Ítem de pruebas de la v6.50.112: «necesito un item que reinicie el
contador de los jefes para verificar como devora los jefes, este iten no
debe ser consumible».

EL DISEÑO (la hermana de la Esencia de Hambre): la MISMA página negra
torcida del grimorio (borde violáceo, esquina doblada) pero EN BLANCO —
las líneas de tinta de los festines escritos SE ESTÁN BORRANDO: sólidas
a la izquierda, desvaneciéndose en dither a la derecha, con MOTAS de
tinta alzándose del pliego (la memoria que se disuelve en humo).
Paleta de la casa: negro #0B0712, borde violeta #7A4FD1, filo #C48AFF,
tinta desvanecida #5A3C8C.
"""
import os

W = H = 24
OUT = os.path.join(os.path.dirname(__file__), "..",
                   "AethonMod", "Content", "Items", "PaginaEnBlanco.png")

# --- paleta de la casa ---
NEGRO      = (11, 7, 18, 255)      # el pliego
NEGRO_SUAVE= (24, 16, 38, 255)     # el pliegue
VIOLETA    = (122, 79, 209, 255)   # el borde del grimorio
VIOLETA_CL = (196, 138, 255, 255)  # el filo / las motas
TINTA      = (90, 60, 140, 255)    # la tinta desvanecida
TINTA_VIVA = (150, 105, 220, 255)  # la tinta aún fresca
TRANSP     = (0, 0, 0, 0)

def clamp(v): return max(0, min(23, int(round(v))))

def pagina(px):
    """LA PÁGINA: un pliego negro torcido (la hoja del grimorio) con
    esquina doblada — la MISMA geometría de la Esencia de Hambre."""
    import math
    ang = math.radians(-8.0)
    ca, sa = math.cos(ang), math.sin(ang)
    for y in range(H):
        for x in range(W):
            dx, dy = x - 11.5, y - 11.5
            rx = dx * ca + dy * sa
            ry = -dx * sa + dy * ca
            inx = abs(rx) <= 7.5
            iny = -9.0 <= ry <= 9.0
            if inx and iny:
                if rx > 2.0 and ry < -4.0 and (rx - 2.0) + (ry + 4.0) * -1.0 > 4.0:
                    continue
                px[y][x] = NEGRO

def pliegues(px):
    """las líneas del pliego: dos vetas suaves verticales."""
    for y in range(H):
        for x in (8, 15):
            if px[y][x] == NEGRO and 4 <= y <= 20:
                px[y][x] = NEGRO_SUAVE

def borde(px):
    """EL BORDE VIOLETA: el contorno de la página."""
    for y in range(H):
        for x in range(W):
            if px[y][x] != NEGRO and px[y][x] != NEGRO_SUAVE:
                continue
            vecino_libre = False
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < W and 0 <= ny < H:
                        if px[ny][nx] == TRANSP:
                            vecino_libre = True
            if vecino_libre:
                px[y][x] = VIOLETA
    # el filo brillante arriba (la luz de la esquina doblada)
    for x in range(W):
        if px[4][x] == VIOLETA:
            px[3][x] = VIOLETA_CL

def lineas_borradas(px):
    """LA TINTA QUE SE BORRA: tres renglones de festín que se desvanecen
    de izquierda a derecha — sólidos al inicio, dither al final (la
    página olvida lo escrito). El renglón de ABAJO ya casi no existe."""
    # (y, x_solido_hasta, x_dither_hasta, color)
    renglones = [
        (7,  (7, 10), (11, 13), TINTA_VIVA),   # el título del festín
        (10, (7, 13), (14, 17), TINTA),        # la lista de jefes
        (13, (7, 11), (12, 14), TINTA),
        (16, None,    (7, 9),   TINTA),        # apenas un rastro
    ]
    for y, solido, dither, c in renglones:
        if solido:
            for x in range(solido[0], solido[1] + 1):
                if px[y][x] in (NEGRO, NEGRO_SUAVE):
                    px[y][x] = c
        if dither:
            x = dither[0]
            fin = dither[1]
            alternar = True
            while x <= fin:
                if alternar and px[y][x] in (NEGRO, NEGRO_SUAVE):
                    px[y][x] = c
                alternar = not alternar
                x += 1

def motas(px):
    """LAS MOTAS DE TINTA: el polvillo violáceo que se ALZA del pliego
    donde la escritura se disuelve (la memoria en humo) — FUERA del
    borde derecho de la página, subiendo en diagonal."""
    motas_px = [(19, 7), (20, 9), (19, 11), (21, 10), (18, 5)]
    for (mx, my) in motas_px:
        if 0 <= mx < W and 0 <= my < H and px[my][mx] == TRANSP:
            px[my][mx] = VIOLETA_CL

def main():
    px = [[TRANSP for _ in range(W)] for _ in range(H)]
    pagina(px)
    pliegues(px)
    borde(px)
    lineas_borradas(px)
    motas(px)

    from PIL import Image
    im = Image.new("RGBA", (W, H))
    for y in range(H):
        for x in range(W):
            im.putpixel((x, y), px[y][x])
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    im.save(OUT)
    print(f"OK {OUT} {W}x{H}")

if __name__ == "__main__":
    main()
