#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_esencia_hambre_v650110.py — LA TEXTURA DE LA ESENCIA DE HAMBRE (24×24).

Ítem de pruebas de la v6.50.110: «crea un item que le de hambre al
grimorio eterno para comprobar si funciona, que suba el hambre de 5 en 5
y que no sea consumible».

EL DISEÑO (la familia del ojo — el libro nervioso): UNA PÁGINA NEGRA
torcida (el pliego del grimorio) con el borde violáceo, el IRIS ROJO del
hambre en el centro (el ojo que demanda comida) y las GOTAS de apetito
cayendo. Paleta de la casa: negro #0B0712, borde violeta #7A4FD1, iris
rojo #E54637 con núcleo ámbar #FFB03C (la rampa del rojo del Nervioso).
"""
import os

W = H = 24
OUT = os.path.join(os.path.dirname(__file__), "..",
                   "AethonMod", "Content", "Items", "EsenciaHambre.png")

# --- paleta de la casa ---
NEGRO      = (11, 7, 18, 255)      # el pliego
NEGRO_SUAVE= (24, 16, 38, 255)     # el pliegue
VIOLETA    = (122, 79, 209, 255)   # el borde del grimorio
VIOLETA_CL = (196, 138, 255, 255)  # el filo
ROJO       = (229, 70, 55, 255)    # el iris del hambre
ROJO_OSC   = (140, 30, 26, 255)    # el borde del iris
AMBAR      = (255, 176, 60, 255)   # el núcleo (la rampa del Nervioso)
BLANCO     = (255, 240, 230, 255)  # el brillo
TRANSP     = (0, 0, 0, 0)

def clamp(v): return max(0, min(23, int(round(v))))

def pagina(px):
    """LA PÁGINA: un pliego negro torcido (la hoja del grimorio) con
    esquinas dobladas — un rectángulo rotado ~8° recortado al lienzo."""
    import math
    ang = math.radians(-8.0)
    ca, sa = math.cos(ang), math.sin(ang)
    for y in range(H):
        for x in range(W):
            # centro del lienzo
            dx, dy = x - 11.5, y - 11.5
            # rotar AL REVÉS (preguntar si el pixel cae en la página)
            rx = dx * ca + dy * sa
            ry = -dx * sa + dy * ca
            # la página: 15 ancho × 18 alto con esquina doblada (arriba-der)
            inx = abs(rx) <= 7.5
            iny = -9.0 <= ry <= 9.0
            if inx and iny:
                # la esquina doblada (triángulo arriba-derecha)
                if rx > 2.0 and ry < -4.0 and (rx - 2.0) + (ry + 4.0) * -1.0 > 4.0:
                    continue
                px[y][x] = NEGRO

def pliegues(px):
    """las líneas del pliego: dos vetas suaves verticales (la hoja vivida)."""
    for y in range(H):
        for x in (8, 15):
            if px[y][x] == NEGRO and 4 <= y <= 20:
                px[y][x] = NEGRO_SUAVE

def borde(px):
    """EL BORDE VIOLETA: el contorno de la página (el filo del grimorio)."""
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

def iris(px):
    """EL IRIS DEL HAMBRE: el ojo rojo de la familia — elipse roja con
    borde oscuro, núcleo ámbar y el brillo blanco (vivo, no cerrado)."""
    import math
    cx, cy = 12.0, 12.5
    for y in range(H):
        for x in range(W):
            if px[y][x] == TRANSP:
                continue
            d = math.sqrt(((x - cx) / 4.6) ** 2 + ((y - cy) / 4.6) ** 2)
            if d <= 1.0:
                px[y][x] = ROJO
            elif d <= 1.22:
                px[y][x] = ROJO_OSC
    # el núcleo ámbar
    for y in range(H):
        for x in range(W):
            if px[y][x] == ROJO:
                d = math.sqrt((x - cx) ** 2 + (y - cy) ** 2)
                if d <= 1.8:
                    px[y][x] = AMBAR
    # el brillo (arriba-izquierda del iris)
    for (bx, by) in ((10, 10), (11, 10), (10, 11)):
        if px[by][bx] in (ROJO, AMBAR, ROJO_OSC):
            px[by][bx] = BLANCO

def gotas(px):
    """LAS GOTAS DEL APETITO: tres gotitas cayendo bajo el iris (la
    comida que exige) — ámbar arriba, rojas abajo."""
    gota = [((14, 18), AMBAR), ((16, 20), ROJO), ((13, 21), ROJO)]
    for (gx, gy), c in gota:
        for y in range(H):
            for x in range(W):
                if px[y][x] != TRANSP:
                    continue
                d = abs(x - gx) + abs(y - gy)
                if (abs(x - gx) <= 0 and abs(y - gy) <= 1) or (abs(x - gx) + abs(y - gy) <= 1):
                    px[y][x] = c

def runas(px):
    """LA MARCA: tres píxeles de runa en la parte baja de la página (el
    sello del ayuno — la firma de los erosionados)."""
    for (rx, ry) in ((7, 17), (17, 17), (12, 19)):
        if px[ry][rx] == NEGRO or px[ry][rx] == NEGRO_SUAVE:
            px[ry][rx] = VIOLETA_CL

def main():
    px = [[TRANSP for _ in range(W)] for _ in range(H)]
    pagina(px)
    pliegues(px)
    borde(px)
    runas(px)
    iris(px)
    gotas(px)

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
