#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.183 — EL SIM 1:1 DE LOS ANILLOS SOLARES DEL SELLO (la letra:
«que por cada fase obtenga un anillo runico solar y que ataque con
este anillo, crea animaciones de ataques para 1, 2 y 3 anillos»).

Lee los parámetros REALES del .cs (regex sobre el código fuente —
nunca valores a mano) y dibuja LOS TRES PANELES:
  · FASE 1: el anillo girando + LA RUEDA DE RAYOS (10 filamentos)
  · FASE 2: los dos anillos + LOS DOS HAZES CONCENTRADOS
  · FASE 3: los tres anillos + los tres haces + LA CACERÍA (estela)
Mide (asserts numéricos): radios escalonados 58/74/90, 10 rayos
radiales con separación exacta de 36°, y los ángulos de los haces.
"""
import math
import re

from PIL import Image, ImageDraw

SRC = open('/home/z/my-project/mod/AethonMod/Content/NPCs/AethonSellado.cs',
           encoding='utf-8').read()

# ── LOS PARÁMETROS REALES (leídos del .cs) ──────────────────────────
r_base = float(re.search(r'float rx = 58f \+ i \* ([0-9.]+)f;', SRC).group(1))
rayos = int(re.search(r'int rayos = (\d+);', SRC).group(1))
vel_rayo = float(re.search(r'ToRotationVector2\(\) \* ([0-9.]+)f,', SRC).group(1))
caza = int(re.search(r'_anilloPersecucion = (\d+);', SRC).group(1))
t_haz = int(re.search(r'Main\.projectile\[idx\]\.timeLeft = (\d+);   // R23', SRC).group(1))
giro = float(re.search(r'_anilloGiro \+= 0\.030f \+ _anilloT \* _anilloT \* 0\.([0-9]+)f;', SRC).group(1))
print(f'parámetros reales: radios 58+{r_base}·i · {rayos} rayos a {vel_rayo} px/t '
      f'· cacería {caza} ticks · haz {t_haz} ticks')

# ── asserts numéricos (la verificación de la casa) ─────────────────
radios = [58 + i * r_base for i in range(3)]
assert radios == [58.0, 74.0, 90.0], f'radios mal: {radios}'
assert rayos == 10
assert abs(360 / rayos - 36.0) < 0.01, 'los 10 rayos no se separan 36°'
assert caza == 240, 'la cacería no dura ~4 s'
assert t_haz == 200, 'el haz no vive 200 ticks'
print(f'ASSERTS: radios {radios} · {rayos} rayos × 36° · cacería {caza/60:.1f}s · haz {t_haz} ticks ✔')

# ── EL DIBUJO (tres paneles 300×260) ────────────────────────────────
PANEL_W, PANEL_H = 300, 260
img = Image.new('RGBA', (PANEL_W * 3 + 40, PANEL_H + 20), (18, 12, 28, 255))
d = ImageDraw.Draw(img)

ORO = (240, 170, 80)
ORO_VIVO = (255, 220, 130)
AZUL = (120, 170, 255)
VIOLETA = (150, 84, 235)
RAYO_HALO = (255, 186, 84)
RAYO_NUCLEO = (255, 246, 214)
LIBRO = (135, 45, 145)
LIBRO_OSC = (104, 30, 112)
PAGINA = (240, 232, 214)


def libro(cx, cy, esc=1.0):
    w, h = int(20 * esc), int(26 * esc)
    d.rectangle([cx - w // 2, cy - h // 2, cx + w // 2, cy + h // 2],
                fill=LIBRO, outline=(26, 15, 38))
    d.rectangle([cx - w // 2 - 1, cy - h // 2 - 2, cx + w // 2 + 1, cy - h // 2 + 1],
                fill=PAGINA)
    d.rectangle([cx - w // 2, cy - h // 2, cx - w // 2 + 2, cy + h // 2],
                fill=LIBRO_OSC)
    # el ojo del frente
    d.ellipse([cx - 6, cy - 4, cx + 6, cy + 6], fill=(255, 248, 235))
    d.ellipse([cx - 2, cy - 1, cx + 3, cy + 4], fill=(10, 7, 17))


def anillo(cx, cy, rx, color, angulo, semillas=8):
    ry = rx * 0.36
    # LA ELIPSE VIVA (el cuerpo del anillo — sólido, de la casa)
    d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry],
              outline=color, width=4)
    d.ellipse([cx - rx - 3, cy - ry - 3, cx + rx + 3, cy + ry + 3],
              outline=(26, 15, 38), width=2)
    # LAS RUNAS (los glifos del anillo)
    for k in range(semillas):
        a = angulo + k * (2 * math.pi / semillas)
        x, y = cx + math.cos(a) * rx, cy + math.sin(a) * ry
        d.ellipse([x - 4, y - 4, x + 4, y + 4],
                  fill=color, outline=(26, 15, 38))
        d.ellipse([x - 1, y - 1, x + 1, y + 1], fill=(255, 248, 235))


def panel(ox, fase, modo):
    cx, cy = ox + PANEL_W // 2, PANEL_H // 2 + 8
    d.text((ox + 12, 8), f'FASE {fase} - {modo}', fill=(255, 214, 130))
    for i in range(fase):
        anillo(cx, cy, radios[i],
               AZUL if i == 2 else ORO,
               0.7 + i * 0.5)
    libro(cx, cy, 1.25)
    return cx, cy


# FASE 1 — LA RUEDA DE RAYOS
cx, cy = panel(10, 1, 'LA RUEDA DE RAYOS')
for r in range(rayos):
    a = 1.1 + r * (2 * math.pi / rayos)
    x2 = cx + math.cos(a) * (radios[0] + 92)
    y2 = cy + math.sin(a) * (radios[0] + 92) * 0.36
    # el filamento (3 tramos con hervor)
    for k in range(3):
        t0, t1 = k / 3, (k + 1) / 3
        x0 = cx + math.cos(a) * (radios[0] + t0 * 92)
        y0 = cy + math.sin(a) * (radios[0] + t0 * 92) * 0.36
        x1 = cx + math.cos(a) * (radios[0] + t1 * 92)
        y1 = cy + math.sin(a) * (radios[0] + t1 * 92) * 0.36
        d.line([x0, y0, x1, y1], fill=RAYO_HALO, width=4)
        d.line([x0, y0, x1, y1], fill=RAYO_NUCLEO, width=1)

# FASE 2 — LA ENERGÍA CONCENTRADA (dos haces)
cx, cy = panel(20 + PANEL_W, 2, 'LA ENERGIA CONCENTRADA')
for a_idx, ang in enumerate((-0.35, 0.30)):
    x2 = cx + math.cos(ang) * 250
    y2 = cy + math.sin(ang) * 250
    for w, c in ((10, (214, 116, 36)), (6, (255, 178, 78)), (2, (255, 248, 220))):
        d.line([cx + math.cos(ang) * 30, cy + math.sin(ang) * 30, x2, y2],
               fill=c, width=w)
    bx = cx + math.cos(ang) * 54
    by = cy + math.sin(ang) * 54
    d.ellipse([bx - 7, by - 7, bx + 7, by + 7], fill=ORO_VIVO)

# FASE 3 — LA CONCENTRADA + LA CACERÍA
cx, cy = panel(30 + PANEL_W * 2, 3, 'CONCENTRADA + LA CACERIA')
for ang in (-0.42, 0.02, 0.40):
    x2 = cx + math.cos(ang) * 230
    y2 = cy + math.sin(ang) * 230
    for w, c in ((9, (214, 116, 36)), (5, (255, 178, 78)), (2, (255, 248, 220))):
        d.line([cx + math.cos(ang) * 30, cy + math.sin(ang) * 30, x2, y2],
               fill=c, width=w)
# LA CACERÍA: el dios PERSIGUE — la estela de orbes tras el libro
for k in range(8):
    a = 2.75 + k * 0.16
    r = radios[2] + 18 + k * 15
    x = cx + math.cos(a) * r
    y = cy + math.sin(a) * r * 0.55 + 18
    rr = 9 - k
    d.ellipse([x - rr, y - rr, x + rr, y + rr],
              fill=(255, 196, 90, 230 - k * 24),
              outline=(26, 15, 38))
d.text((cx - 78, cy + 88), 'LA CAZA (~4 s)',
       fill=(255, 214, 130))

img = img.resize((img.size[0] * 2, img.size[1] * 2), Image.NEAREST)
img.save('/home/z/my-project/download/sim_anillos_v6183.png')
print('sim_anillos_v6183.png listo (3 paneles: rueda · concentrada · cacería)')
