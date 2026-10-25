#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_goozma_v6522.py — v6.52.2 — LOS ICONOS DEL DIOS ANTIGUO.
Las copias de GOOZMA (Hunt of the Old God): la bolsa, las 4 armas y la
masa cromática — pixel art limpio (la lección del icono de la .65: el
VLM lee mejor lo SIMPLE) con la paleta OIL del jefe (magenta→naranja→
amarillo→verde→cian).
"""
import math
import os
import struct
import zlib

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

OIL = [(174, 23, 189), (237, 128, 60), (247, 255, 101), (176, 234, 85), (102, 219, 249)]

def png(path, img):
    h = len(img); w = len(img[0])
    raw = b""
    for fila in img:
        raw += b"\x00" + b"".join(struct.pack("4B", *px) for px in fila)
    def chunk(tipo, datos):
        c = tipo + datos
        return struct.pack(">I", len(datos)) + c + struct.pack(">I", zlib.crc32(c) & 0xFFFFFFFF)
    header = struct.pack(">8B", 137, 80, 78, 71, 13, 10, 26, 10)
    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)
    data = header + chunk(b"IHDR", ihdr) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(data)
    print("OK", path)

def lienzo(w, h):
    return [[(0, 0, 0, 0)] * w for _ in range(h)]

def px(img, x, y, c):
    if 0 <= y < len(img) and 0 <= x < len(img[0]):
        img[y][x] = (c[0], c[1], c[2], c[3] if len(c) > 3 else 255)

def oil(frac):
    """La paleta OIL interpolada (0..1)."""
    t = frac % 1.0 * len(OIL)
    i = int(t) % len(OIL); j = (i + 1) % len(OIL)
    f = t - int(t)
    return tuple(int(OIL[i][k] + (OIL[j][k] - OIL[i][k]) * f) for k in range(3))

def linea(img, x0, y0, x1, y1, c, grosor=1):
    pasos = max(abs(x1 - x0), abs(y1 - y0)) * 2 + 1
    for s in range(pasos + 1):
        t = s / pasos
        x = round(x0 + (x1 - x0) * t); y = round(y0 + (y1 - y0) * t)
        for dx in range(-(grosor // 2), grosor // 2 + 1):
            for dy in range(-(grosor // 2), grosor // 2 + 1):
                px(img, x + dx, y + dy, c)

def generar_todos():
    # ============ 1. LA BOLSA DEL DIOS ANTIGUO (30×30) ============
    img = lienzo(30, 30)
    # EL SLIME oscuro (la silueta de Goozma)
    for y in range(8, 28):
        for x in range(5, 25):
            dx = (x - 14.5) / 9.5; dy = (y - 18) / 9.0
            d = dx * dx + dy * dy
            if d <= 1.0:
                rim = d > 0.62
                c = oil((y - 8) / 20.0 * 0.7) if rim else (58, 26, 74)
                px(img, x, y, c)
    # LA CORONA dorada
    for x in range(10, 20):
        px(img, x, 5, (247, 215, 80))
    for i, x in enumerate(range(10, 20, 3)):
        for y in range(6, 8):
            px(img, x, y, (255, 232, 120))
    # EL OJO (la mirada del dios)
    for x in range(11, 19):
        px(img, x, 15, (240, 235, 255)); px(img, x, 16, (240, 235, 255))
    for x in range(13, 17):
        px(img, x, 15, (120, 20, 40)); px(img, x, 16, (120, 20, 40))
    px(img, 14, 15, (20, 4, 10)); px(img, 15, 15, (20, 4, 10))
    px(img, 14, 16, (20, 4, 10)); px(img, 15, 16, (20, 4, 10))
    png(os.path.join(RAIZ, "AethonMod/Content/Items/Bolsas/BolsaDelDiosAntiguo.png"), img)

    # ============ 2. EL RAYO DE FUSIÓN (30×30) ============
    img = lienzo(30, 30)
    # EL EMISOR (el cristal violeta abajo-izquierda)
    for y in range(20, 29):
        for x in range(3, 12):
            if abs(x - 7.5) + abs(y - 24) < 6:
                px(img, x, y, (94, 44, 140) if (x + y) % 3 else (130, 70, 190))
    for y in range(22, 27):
        for x in range(5, 10):
            if abs(x - 7.5) + abs(y - 24) < 3:
                px(img, x, y, (220, 190, 255))
    # EL HAZ arcoíris hacia arriba-derecha (el arcoíris A LO LARGO)
    for s in range(22):
        t = s / 21.0
        x = round(9 + t * 18); y = round(21 - t * 19)
        c = oil(t)
        linea(img, x, y + 1, x + 1, y, c, 2)
    # LA BOCA blanca
    for dy in range(-1, 2):
        for dx in range(-1, 2):
            px(img, 10 + dx, 20 + dy, (255, 253, 246))
    png(os.path.join(RAIZ, "AethonMod/Content/Weapons/Goozma/RayoDeFusion.png"), img)

    # ============ 3. EL LÁSER ARCOÍRIS (30×30) ============
    img = lienzo(30, 30)
    # EL CURVÓN: la C invertida con el arcoíris a lo largo
    for s in range(26):
        t = s / 25.0
        ang = math.pi * 0.15 + t * math.pi * 1.05
        r = 11.5 - t * 2.5
        x = round(15 + math.cos(ang + 0.5) * r * 1.05)
        y = round(16 + math.sin(ang + 0.5) * r)
        c = oil(t)
        linea(img, x, y, x, y, c, 2)
        px(img, x, y, tuple(min(255, v + 70) for v in c))
    # LA CABEZA blanca al final
    for dy in range(-1, 2):
        for dx in range(-1, 2):
            px(img, 24 + dx, 18 + dy, (255, 255, 255))
    png(os.path.join(RAIZ, "AethonMod/Content/Weapons/Goozma/LaserArcoiris.png"), img)

    # ============ 4. EL DESTRUCTOR DE PRISMAS (30×30) ============
    img = lienzo(30, 30)
    # LOS 5 RAYOS en abanico (el shuriken)
    for k in range(5):
        ang = -math.pi / 2 + k * math.pi * 2 / 5
        c = oil(k / 5.0)
        for s in range(13):
            t = s / 12.0
            x = round(15 + math.cos(ang) * t * 13)
            y = round(15 + math.sin(ang) * t * 13)
            g = 2 if t < 0.75 else 1
            linea(img, x, y, x, y, c, g)
    # EL CENTRO oscuro + blanco
    for y in range(12, 19):
        for x in range(12, 19):
            if (x - 15) ** 2 + (y - 15) ** 2 <= 7:
                px(img, x, y, (40, 18, 58))
    for y in range(14, 17):
        for x in range(14, 17):
            px(img, x, y, (240, 235, 255))
    png(os.path.join(RAIZ, "AethonMod/Content/Weapons/Goozma/DestructorDePrismas.png"), img)

    # ============ 5. EL TRUENO DE GELATINA (30×30) ============
    img = lienzo(30, 30)
    # EL RAYO QUEBRADO arcoíris
    pts = [(6, 26), (10, 19), (14, 22), (13, 14), (17, 17), (22, 6), (19, 12), (24, 9)]
    for i in range(len(pts) - 1):
        t0 = i / (len(pts) - 1); t1 = (i + 1) / (len(pts) - 1)
        c0 = oil(t0); c1 = oil(t1)
        pasos = max(abs(pts[i + 1][0] - pts[i][0]), abs(pts[i + 1][1] - pts[i][1])) + 1
        for s in range(pasos):
            t = s / pasos
            x = round(pts[i][0] + (pts[i + 1][0] - pts[i][0]) * t)
            y = round(pts[i][1] + (pts[i + 1][1] - pts[i][1]) * t)
            c = tuple(int(c0[k] + (c1[k] - c0[k]) * t) for k in range(3))
            linea(img, x, y, x, y, c, 2)
    # LA GOTA de gelatina (la firma)
    for dy in range(3):
        for dx in range(3):
            px(img, 21 + dx, 4 + dy, (176, 234, 85))
    png(os.path.join(RAIZ, "AethonMod/Content/Weapons/Goozma/TruenoDeGelatina.png"), img)

    # ============ 6. LA MASA CROMÁTICA (22×22) ============
    img = lienzo(22, 22)
    for y in range(22):
        for x in range(22):
            dx = (x - 10.5) / 9.0; dy = (y - 11) / 8.5
            d = dx * dx + dy * dy
            if d <= 1.0:
                c = oil(y / 22.0)
                if d > 0.72:
                    c = tuple(int(v * 0.55) for v in c)
                px(img, x, y, c)
    # EL BRILLO interno (el alma de la masa)
    for y in range(9, 13):
        for x in range(8, 13):
            if (x - 10) ** 2 + (y - 11) ** 2 <= 3:
                px(img, x, y, (255, 255, 245))
    png(os.path.join(RAIZ, "AethonMod/Content/Weapons/Goozma/MasaCromatica.png"), img)

    print("TODOS LOS ICONOS DEL DIOS ANTIGUO GENERADOS")

if __name__ == "__main__":
    generar_todos()
