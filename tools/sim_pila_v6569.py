#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.56.9 — SIM 1:1 DE LA PILA DE CAPAS DEL RC2 (la regla de la casa:
«siempre analisa lo que haces para ver si crea picos o no»).

Replica EXACTO el dibujo del RayosPruebaProjectile:
  · la polilínea de 1500 puntos sobre el eje del haz
  · StripWidth(p) = p^0.8 · cbrt(lerp) · 1.0 · 3500 · (1.1+cos·(0.08−p·0.06))
  · cada capa: semiancho × k, perfil RampaGlow (gauss medido), color × alfa
  · aditivo, y el cuerpo (aprox shader: plasma + núcleo) al centro
Renderiza las 3 recetas y un ANÁLISIS DE PICOS: mide el borde exterior de
la silueta compuesta y cuenta discontinuidades (saltos > umbral = picos).
"""
from PIL import Image
import math

W, H = 960, 540
OUT = "/home/z/my-project/download/Aethon-Mod-Terraria/tools/tools/"

# EL PERFIL RAMPA MEDIDO (V=0..1, lum 0..255) — interpolación lineal
_perfil = [0,0,0,0,1,6,24,61,121,187,233,254,255,236,168,84,26,5,0,0,0,0]
def rampa(v):
    v = max(0.0, min(1.0, v))
    x = v * 20
    i = int(x)
    if i >= 20: return _perfil[20] / 255.0
    return (_perfil[i] + (x - i) * (_perfil[i+1] - _perfil[i])) / 255.0

def hash01(seed, a, b):
    n = (seed * 374761393 + a * 668265263 + b * 2246822519) & 0xFFFFFFFF
    n = ((n ^ (n >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((n ^ (n >> 16)) & 0xFFFFFFFF) / 4294967295.0

# LA PALETA DEL CONJURO
PALETA = [(150,62,235),(104,34,182),(208,46,88),(178,52,214)]
def conjuro(reloj):
    t = reloj % 48.0
    seg = t / 12.0
    i = int(seg) % 4
    j = (i + 1) % 4
    f = seg - int(seg)
    a, b = PALETA[i], PALETA[j]
    return tuple(a[k] + (b[k]-a[k]) * f for k in range(3))

def lerp1(a, b, t):
    return a + (b - a) * t

def get_lerp_value(frm, to, v):
    x = (v - frm) / (to - frm)
    return max(0.0, min(1.0, x))

def strip_width(p, time, real=1.0):
    start = p ** 0.8
    cap = max(0.0, get_lerp_value(1.01, 0.7, p)) ** (1/3)
    return start * cap * real * 3500.0 * (1.1 + math.cos(time) * (0.08 - p * 0.06))

RECETAS = {
    "A": dict(kb=2.60, ab=0.09, kg=1.75, ag=0.20, k2=0, a2=0, kn=0.34, an=0.80, soles=5),
    "B": dict(kb=3.10, ab=0.14, kg=2.10, ag=0.28, k2=0, a2=0, kn=0.42, an=0.95, soles=8),
    "C": dict(kb=3.60, ab=0.20, kg=2.50, ag=0.24, k2=1.90, a2=0.12, kn=0.28, an=0.65, soles=6),
}

def render(receta, time=90.0, reloj=120.0, cuerpo_shader=True, ruta=None):
    R = RECETAS[receta]
    img = Image.new("RGB", (W, H), (10, 8, 16))
    px = img.load()
    boca = (90, 300)
    ang = -0.16  # ~9° hacia arriba
    ux, uy = math.cos(ang), math.sin(ang)
    px_, py_ = -uy, ux
    largo = 3500 * (0.5 + 1.0 * 0.5)

    for y in range(H):
        for x in range(W):
            rx, ry = x - boca[0], y - boca[1]
            along = rx * ux + ry * uy
            perp = rx * px_ + ry * py_
            if along < -20 or along > largo: continue
            p = max(0.0, min(1.0, along / largo))
            ap = abs(perp)
            semi = strip_width(p, time)
            if ap > semi * R["kb"]: continue

            rr = gg = bb = 0.0

            # (1) BRUMA
            if ap < semi * R["kb"]:
                c = conjuro(reloj - p * 48.0)
                f = rampa(0.5 + (perp / (semi * R["kb"])) * 0.5)
                sombra = (c[0]*0.42, c[1]*0.34, c[2]*0.52)
                rr += sombra[0] * R["ab"] * f
                gg += sombra[1] * R["ab"] * f
                bb += sombra[2] * R["ab"] * f

            # (2) GLOW
            if ap < semi * R["kg"]:
                c = conjuro(reloj - p * 48.0)
                f = rampa(0.5 + (perp / (semi * R["kg"])) * 0.5)
                rr += c[0] * R["ag"] * f
                gg += c[1] * R["ag"] * f
                bb += c[2] * R["ag"] * f

            # (3) GLOW²
            if R["a2"] > 0 and ap < semi * R["k2"]:
                c = conjuro(reloj - p * 48.0)
                cal = tuple(lerp1(c[k], (255,246,255)[k], 0.28) for k in range(3))
                f = rampa(0.5 + (perp / (semi * R["k2"])) * 0.5)
                rr += cal[0] * R["a2"] * f
                gg += cal[1] * R["a2"] * f
                bb += cal[2] * R["a2"] * f

            # (4) CUERPO (aprox del shader: plasma fBm + gradiente + núcleo)
            if cuerpo_shader and ap < semi:
                c = conjuro(reloj - p * 48.0)
                v = 0.5 + (perp / semi) * 0.5
                # carne fBm aproximada: ruido hash suavizado + scroll
                n = (hash01(int((p*90+v*40+time*0.8)*3), int(v*70), 7) * 0.55
                     + hash01(int((p*50)*5), int((v*90+time*0.6)*3), 11) * 0.45)
                env = rampa(v)
                cuerpo = n * env * (1.0 - 0.75 * max(0.0, (p - 0.5) / 0.5))
                tono = (c[0]*0.62/255, c[1]*0.5/255, c[2]*0.95/255)
                rr += 255 * cuerpo * tono[0] * 1.35
                gg += 255 * cuerpo * tono[1] * 1.35
                bb += 255 * cuerpo * tono[2] * 1.35
                # núcleo del shader
                lum = cuerpo
                nucleo = max(0.0, (lum - 0.42) / 0.58) * rampa(v)
                rr += 255 * nucleo * 0.9; gg += 250 * nucleo * 0.9; bb += 255 * nucleo * 0.9

            # (5) NÚCLEO (la vena)
            if ap < max(1.0, semi * R["kn"]):
                c = (255, 246, 255)
                f = rampa(0.5 + (perp / max(1.0, semi * R["kn"])) * 0.5)
                rr += c[0] * R["an"] * f * 0.55
                gg += c[1] * R["an"] * f * 0.55
                bb += c[2] * R["an"] * f * 0.55

            px[x, y] = (min(255, int(rr)), min(255, int(gg)), min(255, int(bb)))

    # (6) SOLES CONDUCTO (4 puntas en las márgenes)
    for i in range(R["soles"]):
        ritmo = 0.045 + 0.05 * hash01(97, 610 + i, 7)
        p = (hash01(97, 600 + i, 5) * 0.88 + 2.0 * ritmo) % 1.0
        semi = strip_width(p, time)
        lado = 1 if i % 2 == 0 else -1
        margen = (0.62 + 0.30 * hash01(97, 620 + i, 11)) * semi * lado
        cx = boca[0] + ux * (p * largo) + px_ * margen
        cy = boca[1] + uy * (p * largo) + py_ * margen
        size = max(9.0, semi * (0.085 + 0.045 * hash01(97, 630 + i, 13))) * 0.55
        arde = 0.42 + 0.58 * math.sin(2.0 * 6.3 + i * 2.39)
        if arde <= 0.05: continue
        for t in range(-int(size*2.2), int(size*2.2)+1):
            for l1 in (-1, 1):
                yy = int(cy + l1 * max(1.0, size*0.55 - abs(t)*0.18))
                xx = int(cx + t)
                if 0 <= xx < W and 0 <= yy < H:
                    r0,g0,b0 = px[xx,yy]
                    a = 200 * arde
                    px[xx,yy] = (min(255,int(r0+a)), min(255,int(g0+a*0.97)), min(255,int(b0+a*0.88)))
        for t in range(-int(size), int(size)+1):
            for l1 in (-1, 1):
                xx = int(cx + l1 * max(1.0, size*0.55 - abs(t)*0.18))
                yy = int(cy + t)
                if 0 <= xx < W and 0 <= yy < H:
                    r0,g0,b0 = px[xx,yy]
                    a = 200 * arde
                    px[xx,yy] = (min(255,int(r0+a)), min(255,int(g0+a*0.97)), min(255,int(b0+a*0.88)))
        # corazón dorado
        for dy in range(-2, 3):
            for dx in range(-2, 3):
                xx, yy = int(cx)+dx, int(cy)+dy
                if 0 <= xx < W and 0 <= yy < H and dx*dx+dy*dy <= 4:
                    r0,g0,b0 = px[xx,yy]
                    a = 210 * arde
                    px[xx,yy] = (min(255,int(r0+a)), min(255,int(g0+a*0.84)), min(255,int(b0+a*0.47)))

    if ruta:
        img.save(ruta)

    # ============ EL ANÁLISIS DE PICOS ============
    # El borde exterior de la silueta por columna: el bruma es la capa más
    # ancha — su borde debe ser LISO (el análisis del peine de la casa).
    picos = 0
    borde_prev = None
    saltos = []
    for x in range(80, W - 10):
        alto = None
        for y in range(0, H):
            r, g, b = px[x, y]
            if b > 28 and r > 10 and not (r > 230 and g > 230):  # violeta no-blanco
                alto = y; break
        if alto is None: continue
        if borde_prev is not None:
            salto = alto - borde_prev
            if abs(salto) > 4:
                picos += 1
                saltos.append((x, salto))
        borde_prev = alto
    return picos, saltos

for rec in ("A", "B", "C"):
    picos, saltos = render(rec, ruta=f"{OUT}sim_pila_{rec}_v6569.png")
    print(f"RECETA {rec}: picos={picos} saltos={saltos[:6]}")
print("SIM COMPLETO — PNG en tools/tools/")
