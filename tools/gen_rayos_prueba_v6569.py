#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.56.9 — LOS ICONOS DE LAS COPIAS DEL RC2 (RayoPruebaA/B/C).
Familia: el conducto violeta-magenta con vena blanca, la corona rúnica
en la boca y la LETRA de la receta en oro. 44×44 RGBA, pixel-art limpio.
"""
from PIL import Image, ImageDraw, ImageFont
import math, os

BASE = os.path.join(os.path.dirname(__file__), "..", "AethonMod", "Content",
                    "Weapons", "Definitivas")

W = H = 44

def hash01(seed, a, b):
    n = (seed * 374761393 + a * 668265263 + b * 2246822519) & 0xFFFFFFFF
    n = ((n ^ (n >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((n ^ (n >> 16)) & 0xFFFFFFFF) / 4294967295.0

def gen_icon(letra, bruma_k, tint, ruta):
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    px = img.load()

    # ===== EL CONDUCTO: diagonal de la boca (abajo-izq) a la punta (arriba-der) =====
    boca = (8, 36)
    punta = (40, 4)
    dx, dy = punta[0] - boca[0], punta[1] - boca[1]
    largo = math.hypot(dx, dy)
    ux, uy = dx / largo, dy / largo      # eje del haz
    px_, py_ = -uy, ux                    # perpendicular

    # perfil de anchos a lo largo (como el RC2: p^0.8 engorda y capa al filo)
    def semi(p):
        return max(1.0, (p ** 0.8) * (1.0 - 0.55 * max(0.0, p - 0.72) / 0.28) * 10.5 * (1.0 + 0.06 * math.sin(p * 9)))

    for y in range(H):
        for x in range(W):
            rx, ry = x - boca[0], y - boca[1]
            along = rx * ux + ry * uy
            perp = abs(rx * px_ + ry * py_)
            if along < -3 or along > largo + 3:
                continue
            p = max(0.0, min(1.0, along / largo))
            s = semi(p)

            # LA BRUMA (×bruma_k): violeta profundo casi disuelto
            if perp < s * bruma_k:
                f = 1.0 - (perp / (s * bruma_k))
                a = int(70 * (f ** 2.2))
                if a > 0:
                    c = px[x, y]
                    nv = (tint[0] // 3, tint[1] // 3, tint[2] // 3, min(255, c[3] + a))
                    px[x, y] = nv

            # EL GLOW (×1.75): la envolvente de la paleta
            if perp < s * 1.75:
                f = 1.0 - (perp / (s * 1.75))
                a = int(150 * (f ** 2.0))
                if a > 0:
                    c = px[x, y]
                    nv = (max(c[0], tint[0]), max(c[1], tint[1]), max(c[2], tint[2]),
                          min(255, c[3] + a))
                    px[x, y] = nv

            # EL CUERPO (×1.0): plasma con textura hash suave
            if perp < s:
                f = 1.0 - (perp / s)
                ruido = 0.75 + 0.25 * hash01(x * 7 + 1, y * 13 + 3, int(p * 30))
                a = int(235 * (f ** 1.6) * ruido)
                if a > 0:
                    c = px[x, y]
                    nv = (max(c[0], tint[0]), max(c[1], tint[1]), max(c[2], tint[2]),
                          min(255, c[3] + a))
                    px[x, y] = nv

            # EL NÚCLEO (×0.34): la vena blanca
            if perp < max(1.0, s * 0.34):
                f = 1.0 - (perp / max(1.0, s * 0.34))
                a = int(255 * (f ** 1.2))
                c = px[x, y]
                nv = (max(c[0], 255), max(c[1], 248), max(c[2], 255),
                      min(255, c[3] + a))
                px[x, y] = nv

    # ===== LA CORONA RÚNICA en la boca (arco doble con glifos) =====
    for i in range(9):
        ang = -0.55 + i * (math.pi + 1.1) / 8.0
        r1, r2 = 4.6, 7.4
        gx, gy = boca[0] + math.cos(ang) * ((r1 + r2) / 2), boca[1] + math.sin(ang) * ((r1 + r2) / 2)
        for yy in range(int(gy) - 1, int(gy) + 2):
            for xx in range(int(gx) - 1, int(gx) + 2):
                if 0 <= xx < W and 0 <= yy < H and hash01(xx, yy, i) > 0.35:
                    c = px[xx, yy]
                    px[xx, yy] = (255, 214, 120, min(255, c[3] + 235))
    # el aro
    for i in range(90):
        ang = -0.55 + i * (math.pi + 1.1) / 89.0
        ax, ay = boca[0] + math.cos(ang) * 4.6, boca[1] + math.sin(ang) * 4.6
        bx, by = boca[0] + math.cos(ang) * 7.4, boca[1] + math.sin(ang) * 7.4
        d.line([(ax, ay), (bx, by)], fill=(255, 128, 104, 190), width=1)

    # ===== LOS SOLES DE 4 PUNTAS en las márgenes =====
    soles = [(0.38, -1), (0.62, 1), (0.82, -1)]
    for si, (sp, lado) in enumerate(soles):
        p = sp
        s = semi(p)
        cx = boca[0] + ux * (p * largo) + px_ * (lado * s * 1.35)
        cy = boca[1] + uy * (p * largo) + py_ * (lado * s * 1.35)
        sz = 2.6
        for t in range(-3, 4):
            for l1 in (-1, 1):
                x1, y1 = int(cx + t * 1.0), int(cy + l1 * (sz - abs(t) * 0.55))
                if 0 <= x1 < W and 0 <= y1 < H:
                    c = px[x1, y1]
                    a = 255 if abs(t) <= 1 else 170
                    px[x1, y1] = (255, 248, 225, min(255, c[3] + a))
        x2, y2 = int(cx), int(cy)
        if 0 <= x2 < W and 0 <= y2 < H:
            c = px[x2, y2]
            px[x2, y2] = (255, 214, 120, min(255, c[3] + 235))

    # ===== LA LETRA DE LA RECETA (TrueType con trazo — legible a 44×44) =====
    try:
        fuente = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 17)
    except Exception:
        fuente = ImageFont.load_default()
    d2 = ImageDraw.Draw(img)
    # contorno oscuro grueso + relleno oro con sombra
    d2.text((27, 22), letra, font=fuente, fill=(40, 12, 48, 255),
            stroke_width=3, stroke_fill=(40, 12, 48, 255))
    d2.text((28, 21), letra, font=fuente, fill=(255, 214, 120, 255),
            stroke_width=1, stroke_fill=(120, 60, 20, 255))

    img.save(ruta)
    print(f"OK {os.path.basename(ruta)}")

gen_icon("A", 2.6, (150, 62, 235), os.path.join(BASE, "RayoPruebaA.png"))
gen_icon("B", 3.1, (208, 46, 88), os.path.join(BASE, "RayoPruebaB.png"))
gen_icon("C", 3.6, (120, 60, 214), os.path.join(BASE, "RayoPruebaC.png"))
print("FAMILIA COMPLETA")
