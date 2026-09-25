#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_v635_assets.py — LOS ICONOS DE LOS TRES ACCESORIOS DE LAS LIBRERÍAS (v6.35).

Pixel-art 30×30 estilo el mod (los sellos y anillos que RODEAN al jugador):
  · SelloGenesisItem      — el círculo mágico dorado: aro doble + 8 runas de pie
                            + el sello interior azul-estelar con el glifo del astro
  · AnillosSolRunicoItem  — el sistema orbital: 3 elipses inclinadas alternas
                            (oro, azul, oro) con puntitos de runas y el sol central
  · AnillosHorizonteItem  — el disco de acreción inclinado: bandas naranja→carmesí
                            viajando, el aro fino del horizonte y el núcleo negro

Y el sprite 76×76 del halo (glow suave rojo-naranja — el dibujo real lo hace
SelloVacio de OrbitaLib).
"""
import math
import os
import random

from PIL import Image, ImageDraw

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
W = os.path.join(BASE, "Content", "Items", "Accessories")
P = os.path.join(BASE, "Content", "Projectiles", "Cosmetic")


def sello(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    rng = random.Random(353511)
    cx, cy = 15, 15

    # --- EL HALO tenue del sello (el glow del círculo mágico). ---
    for r in (14, 13):
        for k in range(int(r * 6)):
            a = k * math.tau / int(r * 6)
            if rng.random() < 0.42:
                continue
            d.point((round(cx + math.cos(a) * r), round(cy + math.sin(a) * r)),
                    fill=(255, 190, 80, 70))

    # --- EL ARO DOBLE exterior (corpulento + eco interior). ---
    for k in range(64):
        a = k * math.tau / 64
        rr = 11.4
        c = (255, 214, 106, 255) if k % 5 else (255, 240, 185, 255)
        d.point((round(cx + math.cos(a) * rr), round(cy + math.sin(a) * rr)), fill=c)
    for k in range(44):
        a = k * math.tau / 44
        rr = 10.2
        d.point((round(cx + math.cos(a) * rr), round(cy + math.sin(a) * rr)),
                fill=(255, 240, 185, 190))

    # --- LAS OCHO RUNAS de pie (tracitos angulares sobre el aro). ---
    for g in range(8):
        a = g * math.tau / 8 + 0.18
        px = cx + math.cos(a) * 11.4
        py = cy + math.sin(a) * 11.4
        # Cada runa: 2 trazos en L (la firma de la escritura solar).
        d.point((round(px), round(py)), fill=(255, 240, 185, 255))
        d.point((round(px + 1), round(py - 1)), fill=(255, 214, 106, 255))
        d.point((round(px - 1), round(py - 1)), fill=(255, 214, 106, 255))

    # --- LOS CUATRO NODOS CARDINALES (perlas). ---
    for n in range(4):
        a = n * math.pi / 2
        d.point((round(cx + math.cos(a) * 11.4), round(cy + math.sin(a) * 11.4)),
                fill=(255, 255, 240, 255))

    # --- EL SELLO INTERIOR azul-estelar (contrarrotando). ---
    for k in range(40):
        a = k * math.tau / 40
        rr = 6.2
        c = (135, 165, 255, 235) if k % 4 else (215, 230, 255, 255)
        d.point((round(cx + math.cos(a) * rr), round(cy + math.sin(a) * rr)), fill=c)

    # --- EL GLIFO DEL ASTRO en el corazón (un sol de 4 puntas + cuerpo). ---
    for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
        d.point((cx + dx, cy + dy), fill=(215, 230, 255, 255))
    d.point((cx + 2, cy), fill=(135, 165, 255, 255))
    d.point((cx - 2, cy), fill=(135, 165, 255, 255))
    d.point((cx, cy + 2), fill=(135, 165, 255, 255))
    d.point((cx, cy - 2), fill=(135, 165, 255, 255))

    # --- EL POLVO rúnico exterior. ---
    for k in range(9):
        a = rng.random() * math.tau
        rr = 13.6 + rng.random() * 0.9
        d.point((round(cx + math.cos(a) * rr), round(cy + math.sin(a) * rr)),
                fill=(255, 240, 185, 150))
    im.save(path)


def anillos_sol(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx, cy = 15, 15

    # --- EL ARO PRINCIPAL tipo Saturno: UNA elipse casi horizontal,
    #     trazo continuo de 2 px dorado; la MITAD DE ABAJO (el frente
    #     de la órbita) pasa POR DELANTE del sol, la de arriba se
    #     corta detrás (así se lee la profundidad sin ruido). ---
    rx, ry, tilt = 12.2, 4.4, -0.13
    pasos = 240
    for k in range(pasos):
        a = k * math.tau / pasos
        ex = math.cos(a) * rx
        ey = math.sin(a) * ry
        x = cx + ex * math.cos(tilt) - ey * math.sin(tilt)
        y = cy + ex * math.sin(tilt) + ey * math.cos(tilt)
        # La espalda (arriba) se corta donde pasa tras el sol.
        if ey < 0 and abs(ex) < 4.0:
            continue
        # El frente brilla un pelín más (blanco-oro).
        frente = ey > 0
        c = (255, 235, 160, 255) if frente else (255, 190, 80, 255)
        for dx, dy in ((0, 0), (1, 0)):
            d.point((round(x) + dx, round(y) + dy), fill=c)

    # --- EL SEGUNDO PLANO: un arco AZUL-estelar corto arriba-derecha
    #     (la insinuación del aro impar antihorario — sin cruzar). ---
    for k in range(34):
        a = -0.9 + k * 0.075
        ex = math.cos(a) * 8.8
        ey = math.sin(a) * 7.2
        x = cx + ex * 0.55 - ey * 0.83
        y = cy + ex * 0.83 + ey * 0.55 - 3
        d.point((round(x), round(y)), fill=(135, 165, 255, 250))

    # --- EL CORAZÓN: el sol pequeño y BRILLANTE (disco limpio). ---
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            d.point((cx + dx, cy + dy), fill=(255, 250, 225, 255))
    for dx, dy in ((2, 0), (-2, 0), (0, 2), (0, -2)):
        d.point((cx + dx, cy + dy), fill=(255, 214, 106, 240))

    # --- DOS motas de polvo fijo. ---
    d.point((4, 6), fill=(215, 230, 255, 170))
    d.point((25, 22), fill=(215, 230, 255, 170))
    im.save(path)


def anillos_hizonte(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx, cy = 15, 16

    # --- EL DISCO DE ACRECIÓN inclinado (−0.38 de la casa): bandas
    #     naranja→carmesí viajando, trazo CONTINUO de 2 px. ---
    rx, ry, tilt = 12.2, 6.0, -0.38
    pasos = 260
    for k in range(pasos):
        a = k * math.tau / pasos
        glow = math.sin(a * 20 + 2.4) * 0.5 + 0.5   # LA FÓRMULA FIEL
        ex = math.cos(a) * rx
        ey = math.sin(a) * ry
        x = cx + ex * math.cos(tilt) - ey * math.sin(tilt)
        y = cy + ex * math.sin(tilt) + ey * math.cos(tilt)
        if glow > 0.82:
            c = (255, 245, 225, 255)               # pico incandescente
        elif glow > 0.45:
            c = (255, 130, 50, 255)                # media banda
        else:
            c = (168, 38, 20, 245)                 # valle profundo
        for dx, dy in ((0, 0), (1, 0)):
            d.point((round(x) + dx, round(y) + dy), fill=c)

    # --- EL ARO FINO DEL HORIZONTE (círculo interior continuo). ---
    for k in range(120):
        a = k * math.tau / 120
        ex = math.cos(a) * 6.6
        ey = math.sin(a) * 4.0
        x = cx + ex * math.cos(tilt) - ey * math.sin(tilt)
        y = cy + ex * math.sin(tilt) + ey * math.cos(tilt)
        d.point((round(x), round(y)), fill=(255, 110, 55, 240))

    # --- EL NÚCLEO NEGRO (el portador vive ahí — el icono lo insinúa). ---
    for dx in range(-2, 3):
        for dy in range(-1, 2):
            if dx * dx / 3.4 + dy * dy / 1.6 <= 1.0:
                d.point((cx + dx, cy + dy), fill=(8, 4, 6, 235))

    # --- DOS chispas fijas arriba del disco (fotones limpios). ---
    d.point((23, 7), fill=(255, 250, 230, 255))
    d.point((6, 23), fill=(255, 250, 230, 255))
    im.save(path)


def glow_proyectil(path, rgb):
    """Sprite 76×76: glow radial suave (placeholder — SelloVacio dibuja)."""
    im = Image.new("RGBA", (76, 76), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx = cy = 38
    for r in range(34, 0, -1):
        f = (1 - r / 34) ** 1.6
        d.ellipse((cx - r, cy - r, cx + r, cy + r),
                  fill=(int(rgb[0] * f), int(rgb[1] * f), int(rgb[2] * f), int(150 * f)))
    im.save(path)


sello(os.path.join(W, "SelloGenesisItem.png"))
anillos_sol(os.path.join(W, "AnillosSolRunicoItem.png"))
anillos_hizonte(os.path.join(W, "AnillosHorizonteItem.png"))

glow_proyectil(os.path.join(P, "AnillosSingularesHalo.png"), (255, 96, 40))

print("4 PNGs generados")
