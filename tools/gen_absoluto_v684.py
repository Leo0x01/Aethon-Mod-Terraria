#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""AgujeroAbsolutoBlackHoleProjectile.png — EL SPRITE DEL AGUJERO ABSOLUTO.

Aunque en juego el agujero vive 100% en el renderer procedural
(AgujeroAbsolutoBlackHoleRenderer), tModLoader exige el PNG del proyectil.
Sprite con EL PERFIL ABSOLUTO de la casa: el gradiente del más allá
(violeta del vacío → cian estelar → oro blanco), rim cian, negro platino.

Formato: 76×76 RGBA (el mismo lienzo de los black holes de la casa).
"""
from PIL import Image
import math

W = 76
cx = cy = W / 2.0
img = Image.new('RGBA', (W, W), (0, 0, 0, 0))
px = img.load()

# --- LA PALETA ABSOLUTA (del AgujeroAbsolutoStaff) ---
VIOLETA_VACIO = (108, 66, 214)     # el borde exterior: el vacío
CIAN_ESTELAR  = (80, 220, 255)     # el photon ring: la estrella
ORO_BLANCO    = (255, 244, 214)    # el toque de más allá
NEGRO_PLATINO = (6, 5, 12, 255)    # el núcleo: nada

R_NUCLEO = 20.0    # el horizonte: negro absoluto
R_RING   = 22.5    # el photon ring (donde la luz orbita)
R_HALO   = 35.0    # hasta dónde llega el más allá

def mezcla(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))

for y in range(W):
    for x in range(W):
        dx, dy = x - cx + 0.5, y - cy + 0.5
        r = math.hypot(dx, dy)
        if r > R_HALO:
            continue
        ang = math.atan2(dy, dx)  # -pi..pi (para el arco de oro)

        if r <= R_NUCLEO:
            # EL NÚCLEO — negro platino (nada escapa)
            px[x, y] = NEGRO_PLATINO
            continue

        if r <= R_RING + 1.5:
            # EL PHOTON RING — cian estelar ARDIENDO, pico justo fuera del núcleo
            t = max(0.0, (r - R_NUCLEO) / (R_RING + 1.5 - R_NUCLEO))
            brillo = math.sin(t * math.pi)          # 0 → 1 → 0
            # el arco de ORO BLANCO (lado izquierdo: la luz que llega)
            oro = max(0.0, math.cos(ang)) ** 3      # fuerte a la izquierda
            color = mezcla(CIAN_ESTELAR, ORO_BLANCO, oro * 0.55)
            color = mezcla((0, 0, 0), color, 0.35 + 0.65 * brillo)
            a = int(round(140 + 115 * brillo))
            px[x, y] = (min(255, color[0]), min(255, color[1]),
                        min(255, color[2]), min(255, a))
            continue

        # EL HALO — violeta del vacío muriendo hacia fuera
        t = (r - R_RING - 1.5) / (R_HALO - R_RING - 1.5)   # 0..1
        fade = (1.0 - t) ** 2.2
        # el gradiente del más allá: cian (cerca) → violeta (lejos)
        color = mezcla(CIAN_ESTELAR, VIOLETA_VACIO, min(1.0, t * 1.35))
        # el toque de oro en el borde interior del halo (lado izquierdo)
        oro = max(0.0, math.cos(ang)) ** 4 * (1.0 - t) * 0.45
        color = mezcla(color, ORO_BLANCO, oro)
        a = int(round(120 * fade))
        if a > 0:
            px[x, y] = (color[0], color[1], color[2], a)

out = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Projectiles/Cosmic/AgujeroAbsolutoBlackHoleProjectile.png"
img.save(out, optimize=True)
print("OK", out, img.size)

# Verificación: el negro del núcleo, el cian del ring, el alpha del halo
nucleo = px[38, 38]
ring = px[38, int(38 + 22.5)] if int(38 + 22.5) < W else None
halo = px[38, int(38 + 30)]
print("núcleo (negro):", nucleo)
print("ring (cian):", ring)
print("halo (violeta→transparente):", halo)
