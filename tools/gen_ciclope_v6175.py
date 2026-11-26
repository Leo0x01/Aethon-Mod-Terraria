#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_ciclope_v6175.py — LA SUPERFORJA: el sprite del CÍCLOPE PROYECTADO
(análisis VLM del vídeo «Me Proyecte» de Chopilas): figura humanoide
PÁLIDA tipo maniquí (beige/crema), cabeza OVALADA sin rasgos, UN SOLO
OJO rosa-magenta, cuerpo delgado con brazos — la proyección del
portador. 64×96 px (cuerpo alto y flaco como el maniquí del vídeo).
"""
from PIL import Image, ImageDraw
import pathlib

RAIZ = pathlib.Path(__file__).resolve().parent.parent / "AethonMod"
DEST = RAIZ / "Content" / "NPCs" / "CiclopeProyectado.png"

W, H = 64, 96
img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# === LA PALETA (la del vídeo: pálido/maniquí + ojo magenta) ===
PIEL      = (226, 214, 196, 255)   # beige pálido
PIEL_SOM  = (196, 180, 158, 255)   # sombra de la piel
PIEL_LUZ  = (240, 232, 216, 255)   # luz
OJO       = (255, 90, 200, 255)    # magenta vivo
OJO_NUC   = (255, 220, 245, 255)   # núcleo casi blanco
BRILLO    = (255, 150, 230, 255)   # brillo del ojo

def px(x, y, c):
    img.putpixel((int(x), int(y)), c)

# --- LA CABEZA OVALADA (centro arriba, sin boca, sin nariz: SOLO el ojo) ---
cx = W // 2
head_cy = 16
rw, rh = 13, 16    # radios de la cabeza
for y in range(H):
    for x in range(W):
        dx = (x - cx) / rw
        dy = (y - head_cy) / rh
        dist = dx * dx + dy * dy
        if dist <= 1.0:
            # sombreado: lado izquierdo más oscuro (la luz del vídeo cae a la derecha)
            t = x / W
            base = PIEL_SOM if t < 0.38 else (PIEL if t < 0.75 else PIEL_LUZ)
            img.putpixel((x, y), base)

# --- EL OJO ÚNICO (magenta, grande, rasgado vertical — el del vídeo) ---
eye_cx, eye_cy = cx + 1, head_cy
erx, ery = 7, 6
for y in range(H):
    for x in range(W):
        dx = (x - eye_cx) / erx
        dy = (y - eye_cy) / ery
        dist = dx * dx + dy * dy
        if dist <= 1.0:
            if dist > 0.72:
                img.putpixel((x, y), (40, 20, 50, 255))      # borde oscuro del ojo
            elif dist > 0.30:
                img.putpixel((x, y), OJO)                     # iris magenta
            else:
                img.putpixel((x, y), OJO_NUC)                 # núcleo brillante
# EL REFLEJO (la chispa del ojo vivo)
for k in range(3):
    px(eye_cx - 2 + k, eye_cy - 2 + k, BRILLO)
    px(eye_cx + 2 - k, eye_cy - 2 + k, BRILLO)

# --- EL TORSO (delgado, hombros estrechos — maniquí) ---
torso_top, torso_bot = 33, 62
for y in range(torso_top, torso_bot):
    # el ancho se estrecha hacia la cintura
    fr = (y - torso_top) / (torso_bot - torso_top)
    half = 11 - fr * 3.5
    for x in range(W):
        if abs(x - cx) <= half:
            t = (x - (cx - half)) / (2 * half)
            base = PIEL_SOM if t < 0.35 else (PIEL if t < 0.78 else PIEL_LUZ)
            img.putpixel((x, y), base)

# --- LOS BRAZOS EXTENDIDOS (la pose de ataque del vídeo: abiertos) ---
arm_y = 37
for k in range(-1, 2):     # grosor de 3 px
    for x in range(cx - 26, cx - 11):    # brazo izquierdo (abierto)
        y = arm_y + k + (x - (cx - 26)) // 8   # cae levemente
        if 0 <= y < H:
            img.putpixel((x, y), PIEL_SOM if k == -1 else PIEL)
    for x in range(cx + 11, cx + 27):    # brazo derecho
        y = arm_y + k - (x - (cx + 11)) // 8   # cae levemente (simétrico)
        if 0 <= y < H:
            img.putpixel((x, y), PIEL if k != 1 else PIEL_LUZ)

# --- LAS PIERNAS (dos columnas delgadas) ---
for y in range(torso_bot, 88):
    half_top = 8 if y < 70 else 6
    for k in range(5):
        lx = cx - half_top + k
        rx = cx + half_top - k
        if 0 <= lx < W:
            img.putpixel((lx, y), PIEL_SOM if k < 2 else PIEL)
        if 0 <= rx < W:
            img.putpixel((rx, y), PIEL if k < 3 else PIEL_LUZ)

# --- EL VUELO (sin pies: la proyección FLOTA — se desvanece abajo) ---
for y in range(88, H):
    alfa = max(0, 255 - (y - 88) * 32)
    for x in range(W):
        r, g, b, a = img.getpixel((x, y))
        if a > 0:
            img.putpixel((x, y), (r, g, b, alfa))

# --- EL CONTORNO (la línea de tinta del dibujo: 1px oscuro alrededor) ---
orig = img.copy()
CONTORNO = (52, 40, 60, 255)
for y in range(H):
    for x in range(W):
        if orig.getpixel((x, y))[3] == 0:
            vecino = False
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < W and 0 <= ny < H and orig.getpixel((nx, ny))[3] > 40:
                    vecino = True
                    break
            if vecino:
                img.putpixel((x, y), CONTORNO)

img.save(DEST)
print("sprite:", DEST, img.size)
