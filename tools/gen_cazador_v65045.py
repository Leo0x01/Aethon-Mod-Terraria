#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_cazador_v65045.py — LA TEXTURA DEL CAZADOR ASTRAL (v6.50.45).

El NPC se dibuja en mundo por SierpesLib.ManadaAstral (PreDraw devuelve
false — 100% luz), pero tML exige SU textura (bestiario/registro) — y la
casa no quiere un cuadrado muerto: la textura ES EL CAZADOR tal como se
ve en pelea: UN COMETA DE LUZ con la CABEZA facetada (el núcleo blanco,
el filo de oro, el lomo violeta) y LA COLA DE TRES CUENTAS menguantes
(la distancia elástica hecha visible). 34×20 px, supermuestreo ×8.
Determinista (hash propio, CERO random). Salida:
  AethonMod/Content/NPCs/CazadorAstral.png (34×20 RGBA)
"""
from PIL import Image, ImageDraw

SS = 8
W, H = 34, 20


def h01(a, b, c):
    h = (a * 374761393 + b * 668265263 + c * 2147483647) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFFFF) / 0xFFFFFF


def mix(c1, c2, f):
    return tuple(int(c1[i] + (c2[i] - c1[i]) * f) for i in range(3))


def s(v):
    return v * SS


im = Image.new("RGBA", (s(W), s(H)), (0, 0, 0, 0))
d = ImageDraw.Draw(im)


def poly(pts, col):
    d.polygon(pts, fill=col + (255,) if len(col) == 3 else col)


def rect(x0, y0, x1, y1, col):
    d.rectangle([x0, y0, x1 - 1, y1 - 1], fill=col + (255,) if len(col) == 3 else col)


def glow(cx, cy, r, col, amax):
    r = int(r)
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            dd2 = (dx * dx + dy * dy) / (r * r)
            if dd2 >= 1.0:
                continue
            a = int(amax * (1.0 - dd2) ** 2)
            if a <= 2:
                continue
            x, y = int(cx + dx), int(cy + dy)
            if 0 <= x < s(W) and 0 <= y < s(H):
                base = im.getpixel((x, y))
                if len(base) < 4 or base[3] == 0:
                    im.putpixel((x, y), col + (a,))
                else:
                    im.putpixel((x, y), (
                        min(255, base[0] + col[0] * a // 255),
                        min(255, base[1] + col[1] * a // 255),
                        min(255, base[2] + col[2] * a // 255),
                        min(255, base[3] + a)))


# --- EL GLOBO (el cometa se lee primero) ---
glow(s(10), s(10), s(9), (190, 220, 255), 66)
glow(s(10), s(10), s(5), (255, 250, 240), 55)

cx, cy = s(10.4), s(10.0)   # el corazón del cazador (lo usan TODOS)

# --- EL CUERPO QUE SE ESTIRA (la nuca → la primera cuenta): se dibuja
#     ANTES de la cabeza (la cabeza va ENCIMA) — un cono decreciente:
#     alto junto a la nuca (±3.2), fino al llegar a la cuenta (±1.6) ---
poly([(cx - s(4.0), cy - s(3.2)), (s(19.5), cy - s(1.6)),
      (s(19.5), cy + s(1.6)), (cx - s(4.0), cy + s(3.2))],
     (176, 150, 238))
for k in range(5):
    fx = cx - s(3.2) + (s(19.5) - cx + s(3.2)) * (k + 0.5) / 5
    fh = 3.2 - (3.2 - 1.6) * (k + 0.5) / 5
    rect(fx - SS // 2, cy - s(fh) * 0.55, fx + SS // 2, cy + s(fh) * 0.55,
         mix((196, 170, 250), (150, 126, 210), k / 4.0))

# --- LA CABEZA FACETADA (mirando a la derecha): el rombo del bastón ---
hw, hh = s(4.6), s(4.4)
tip = (cx + hw, cy)                    # el hocico
up = (cx - s(1.4), cy - hh)
dn = (cx - s(1.4), cy + hh)
nape = (cx - s(4.2), cy)
poly([tip, up, nape, dn], (150, 122, 214))          # base violeta
poly([tip, up, (cx, cy)], (255, 238, 196))          # el lomo superior: ORO
poly([up, nape, (cx, cy)], (208, 188, 255))         # la frente: violeta claro
poly([dn, nape, (cx, cy)], (124, 100, 194))         # la mandíbula: violeta hondo
poly([tip, dn, (cx, cy)], (176, 150, 238))          # el hocico: medio
# EL NÚCLEO: la vena blanca hasta el hocico (ANCHA — 2.8 px: tras el
# LANCZOS 8:1 la de 1.4 se perdía en la mezcla)
poly([(cx - s(1.4), cy - s(2.8)), (cx + s(1.4), cy - s(2.8)),
      (cx + s(1.0), cy + s(3.1)), (cx, cy + s(3.7)),
      (cx - s(1.0), cy + s(3.1))], (255, 252, 242))
# EL FILO DE ORO
d.line([tip, up, nape, dn, tip], fill=(255, 218, 132, 255), width=max(1, SS // 3))
# EL GLINT del hocico (1.5 px)
rect(tip[0] - s(0.75), tip[1] - s(0.75), tip[0] + s(0.75), tip[1] + s(0.75),
     (255, 255, 252))

# --- LA COLA DE TRES CUENTAS (la distancia elástica visible) ---
beads = [(s(19.5), s(10.0), s(2.5)), (s(24.3), s(10.0), s(2.0)),
         (s(28.4), s(10.0), s(1.55))]
for bx, by, br in beads:
    glow(bx, by, br * 2.2, (150, 200, 255), 60)
    r = int(br)
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            dd2 = (dx * dx + dy * dy) / (br * br)
            if dd2 <= 1.0:
                c = mix((208, 232, 255), (124, 100, 194), dd2)
                x, y = int(bx + dx), int(by + dy)
                if 0 <= x < s(W) and 0 <= y < s(H):
                    im.putpixel((x, y), c + (255,))
    # EL NÚCLEO de la cuenta (el 45% central, BLANCO)
    nr = max(1, int(br * 0.45))
    for dy in range(-nr, nr + 1):
        for dx in range(-nr, nr + 1):
            if (dx * dx + dy * dy) <= nr * nr:
                x, y = int(bx + dx), int(by + dy)
                if 0 <= x < s(W) and 0 <= y < s(H):
                    im.putpixel((x, y), (255, 252, 242, 255))

# --- LAS DOS ESTELAS (los trazos de velocidad) ---
for k, (ex, ey, L) in enumerate(((s(15.5), s(4.2), s(5.5)),
                                 (s(16.5), s(15.8), s(4.6)))):
    d.line([(ex, ey), (ex - L, ey)], fill=(150, 200, 255, 150), width=SS // 3)

# --- EL RUIDO suave del cuerpo (la talla de la luz) ---
for y in range(s(H)):
    for x in range(s(W)):
        p = im.getpixel((x, y))
        if len(p) < 4 or p[3] == 0:
            continue
        m = 1.0 + (h01(x, y, 65047) - 0.5) * 0.09
        im.putpixel((x, y), (min(255, int(p[0] * m)), min(255, int(p[1] * m)),
                             min(255, int(p[2] * m)), p[3]))

out = im.resize((W, H), Image.LANCZOS)
out.save("AethonMod/Content/NPCs/CazadorAstral.png")
print(" CazadorAstral.png", out.size)
