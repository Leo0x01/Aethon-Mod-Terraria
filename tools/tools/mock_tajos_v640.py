#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_tajos_v640.py — EL MOCK 1:1 DEL BASTÓN DE LOS TAJOS (v6.40).

Simula EL RENDER EXACTO de TajoLib.Tajo (el mismo pipeline del mock de
anillos: carga tML + shader FNA + BlendState.Additive REAL =
SourceAlpha/One) con las MISMAS fórmulas del .cs (perfil de lente,
campana, revelado, 3 capas, puntas) — y el florecer completo del
proyectil (7 tajos desacoplados, pops escalonados).

Paneles:
  1. UN Tajo grande (radio 80) a brillo pleno — la inspección de capas.
  2. EL FLORECER completo (los 7 tajos en t=26 — varios ya muriendo).
  3. LA SECUENCIA (t=16..30 cada 2) — la causalidad retrasada visible.
"""
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import os

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = os.path.join(RAIZ, "research", "tajos_v640")
os.makedirs(OUT_DIR, exist_ok=True)

# === El pipeline (idéntico al mock de anillos) ===
def cargar(path):
    im = Image.open(path).convert("RGBA")
    a = np.array(im).astype(np.float64)
    zero = a[:, :, 3] == 0
    a[zero, 0:3] = 0.0
    return a

TEX_GLOW = cargar(os.path.join(RAIZ, "Content/Effects/Procedural/SoftGlow.png"))
TEX_BANDA = cargar(os.path.join(RAIZ, "Content/Effects/Procedural/BoltHalo.png"))
TEX_VENA = cargar(os.path.join(RAIZ, "Content/Effects/Procedural/BoltCore.png"))

def muestra_bilineal(tex, u, v):
    h, w = tex.shape[0], tex.shape[1]
    x0 = np.floor(u).astype(np.int64); y0 = np.floor(v).astype(np.int64)
    fx = u - x0; fy = v - y0
    x0c = np.clip(x0, 0, w - 1); x1c = np.clip(x0 + 1, 0, w - 1)
    y0c = np.clip(y0, 0, h - 1); y1c = np.clip(y0 + 1, 0, h - 1)
    t00 = tex[y0c, x0c]; t10 = tex[y0c, x1c]
    t01 = tex[y1c, x0c]; t11 = tex[y1c, x1c]
    fx = fx[..., None]; fy = fy[..., None]
    return (t00 * (1 - fx) * (1 - fy) + t10 * fx * (1 - fy) +
            t01 * (1 - fx) * fy + t11 * fx * fy)

def quad(canvas, pos, size, rot, tint_rgb, tint_a, tex=None):
    T = TEX_GLOW if tex is None else tex
    cx, cy = pos
    sw, sh = size
    tw, th = T.shape[1], T.shape[0]
    half = max(sw, sh) * 0.5 + 2.0
    x0 = max(0, int(cx - half)); x1 = min(canvas.shape[1] - 1, int(cx + half) + 1)
    y0 = max(0, int(cy - half)); y1 = min(canvas.shape[0] - 1, int(cy + half) + 1)
    if x1 <= x0 or y1 <= y0: return
    xs = np.arange(x0, x1 + 1) + 0.5
    ys = np.arange(y0, y1 + 1) + 0.5
    gx, gy = np.meshgrid(xs, ys)
    dx = gx - cx; dy = gy - cy
    c, s = math.cos(-rot), math.sin(-rot)
    lx = dx * c - dy * s; ly = dx * s + dy * c
    dentro = (np.abs(lx) <= sw / 2 + 0.5) & (np.abs(ly) <= sh / 2 + 0.5)
    if not dentro.any(): return
    u = np.clip((lx + sw / 2) * (tw / sw) - 0.5, 0, tw - 1.001)
    v = np.clip((ly + sh / 2) * (th / sh) - 0.5, 0, th - 1.001)
    texel = muestra_bilineal(T, u, v)
    src_rgb = texel[:, :, 0:3] * (np.array(tint_rgb)[None, None, :] / 255.0)
    src_a = (texel[:, :, 3] / 255.0) * (tint_a / 255.0)
    dst = canvas[y0:y1 + 1, x0:x1 + 1, :]
    nuevo = dst + src_rgb * src_a[:, :, None]
    nuevo = np.clip(nuevo, 0, 255)
    canvas[y0:y1 + 1, x0:x1 + 1, :] = np.where(dentro[:, :, None], nuevo, dst)

def capsula(canvas, mid, largo, ancho, rot, rgb, f):
    quad(canvas, mid, (largo + ancho, ancho * 1.9), rot, rgb, max(0, min(1, f)) * 255)

def tint(rgb, f):
    return (rgb, max(0, min(1, f)) * 255)

# === LAS FÓRMULAS LITERALES DE TajoLib ===
def perfil_lente(u):
    s = math.sin(math.pi * max(0, min(1, u)))
    return max(s, 0.001) ** 0.55

def campana(t):
    t = max(0, min(1, t))
    return min(t / 0.55, 1) * min((1 - t) / 0.45, 1)

def punto_arco(centro, radio, ang):
    return (centro[0] + math.cos(ang) * radio, centro[1] + math.sin(ang) * radio)

SEGMENTOS = 28
TAJO_BLANCO = (255, 251, 240)
TAJO_ORO = (255, 214, 130)
ANCHO = 4.6

def tajo(canvas, centro, radio, ang0, ang1, prog, brillo, seed, time=2.35):
    """TajoLib.Tajo — versión BANDA (la del .cs v6.40 final)."""
    if brillo <= 0.02 or radio < 6: return
    prog = max(0, min(1, prog))
    if prog <= 0.01: return
    span = ang1 - ang0

    def segmento(tex, r, alto, f):
        for i in range(SEGMENTOS):
            u0 = i / SEGMENTOS
            if u0 >= prog: break
            um = (u0 + (i + 1) / SEGMENTOS) * 0.5
            gu = perfil_lente(um)
            a = punto_arco(centro, r, ang0 + span * u0)
            b = punto_arco(centro, r, ang0 + span * (i + 1) / SEGMENTOS)
            mid = ((a[0]+b[0])*0.5, (a[1]+b[1])*0.5)
            ln = math.hypot(b[0]-a[0], b[1]-a[1])
            rot = math.atan2(b[1]-a[1], b[0]-a[0])
            quad(canvas, mid, (ln + 2.0, alto * (0.40 + 0.60*gu) if False else alto),
                 rot, *tint_n((0,0,0), 0))  # placeholder

    # (implementación con las 3 capas — abajo)
    def seg_banda(tex, r, alto_base, col, fbase, flick=True):
        for i in range(SEGMENTOS):
            u0 = i / SEGMENTOS
            if u0 >= prog: break
            um = (u0 + (i + 1) / SEGMENTOS) * 0.5
            gu = perfil_lente(um)
            fl = 0.82 + 0.18*math.sin(time*38 + i*2.7 + seed % 7) if flick else 1.0
            a = punto_arco(centro, r, ang0 + span*u0)
            b = punto_arco(centro, r, ang0 + span*(i+1)/SEGMENTOS)
            mid = ((a[0]+b[0])/2, (a[1]+b[1])/2)
            ln = math.hypot(b[0]-a[0], b[1]-a[1])
            rot = math.atan2(b[1]-a[1], b[0]-a[0])
            alto = alto_base * (0.40 + 0.60*gu) if alto_base < 3 else alto_base*(0.45+0.55*gu)
            quad(canvas, mid, (ln + 2.0, alto), rot, *tint(col, fbase*fl*(0.35+0.65*gu)), tex=tex)

    # 1. ECO (BoltHalo fino a radio 0.88)
    seg_banda(TEX_BANDA, radio*0.88, ANCHO*1.9, TAJO_BLANCO, 0.14*brillo, flick=False)
    # 2. HALO (BoltHalo ancho, parpadea)
    seg_banda(TEX_BANDA, radio, ANCHO*6.2, TAJO_ORO, 0.20*brillo, flick=True)
    # 3. NÚCLEO (BoltCore fino)
    seg_banda(TEX_VENA, radio, ANCHO*2.8, TAJO_BLANCO, 0.95*brillo, flick=False)

    # 4. EL FRENTE del revelado
    if prog < 1:
        angf = ang0 + span * prog
        pf = punto_arco(centro, radio, angf)
        quad(canvas, pf, (ANCHO*2.6, ANCHO*2.6), 0, *tint(TAJO_BLANCO, 0.65*brillo))
        quad(canvas, pf, (ANCHO*4.8, ANCHO*0.65), 0, *tint(TAJO_BLANCO, 0.45*brillo))
        quad(canvas, pf, (ANCHO*0.65, ANCHO*4.8), 0, *tint(TAJO_BLANCO, 0.45*brillo))
    # 5. LAS PUNTAS
    if prog >= 0.999:
        for ang in (ang0, ang1):
            p = punto_arco(centro, radio, ang)
            quad(canvas, p, (ANCHO*2.2, ANCHO*2.2), 0, *tint(TAJO_ORO, 0.55*brillo))
            quad(canvas, p, (ANCHO*1.0, ANCHO*1.0), 0, *tint(TAJO_BLANCO, 0.92*brillo))

# === El estado del florecer (las MISMAS fórmulas del proyectil) ===
def hash01(seed, a, b):
    h = (seed * 374761393 + a * 668265263 + b * 1911520717) & 0xFFFFFFFF
    if h >= 0x80000000: h -= 0x100000000
    h ^= (h >> 13) & 0xFFFFFFFF
    h = (h * 1274126177) & 0xFFFFFFFF
    if h >= 0x80000000: h -= 0x100000000
    h ^= (h >> 16) & 0xFFFFFFFF
    return (h & 0xFFFFFF) / 16777216.0

def florecer(mark, seed, edad):
    """Dibuja los 7 tajos del proyectil a la edad dada."""
    canvas = np.zeros((PH, PW, 3)); canvas[:, :] = (18, 24, 38)
    arcs = []
    for i in range(7):
        h1, h2, h3, h4 = (hash01(seed, 11+i*7, 3), hash01(seed, 12+i*7, 5),
                          hash01(seed, 13+i*7, 7), hash01(seed, 14+i*7, 9))
        centro = (mark[0] + (h1-0.5)*128, mark[1] + (h2-0.5)*128)
        radio = 54 + h3 * 46
        span = 1.6 + h4 * 0.8
        base = hash01(seed, 15+i*7, 13) * math.tau
        a0, a1 = (base, base+span) if i % 2 == 0 else (base+span, base)
        pop = 16 + i * 3 + int(h2 * 2.99)
        arcs.append((centro, radio, a0, a1, pop))
    # la marca (fase 2: si aún no florece)
    if edad < 16:
        t = max(0, (edad - 6)) / 10.0
        pulso = 0.5 + 0.5*math.sin(2.35*14)
        quad(canvas, mark, (10+16*t, 10+16*t), 0, *tint((255,226,150), (0.25+0.55*t)*(0.6+0.4*pulso)))
        quad(canvas, mark, (4, 4), 0, *tint((255,251,240), 0.7*(0.5+0.5*pulso)))
    for (centro, radio, a0, a1, pop) in arcs:
        t_arco = edad - pop
        if t_arco < 0 or t_arco > 15: continue
        # revelado con easeOutBack
        tt = min(t_arco / 7.0, 1.0)
        c = 1.35; u = tt - 1
        reveal = 1 + u*u*((c+1)*u + c) if 0 < tt < 1 else (0 if tt <= 0 else 1)
        cam = campana(t_arco / 15.0)
        radio_v = radio * (1 + 0.32 * (1 - (1-min(t_arco/15.0,1.0))**2))
        tajo(canvas, centro, radio_v, a0, a1, reveal, cam, seed)
    return canvas

PW, PH = 240, 200

# === LOS PANELES ===
paneles = []

# 1. UN Tajo grande (inspección de capas)
c1 = np.zeros((PH, PW, 3)); c1[:, :] = (18, 24, 38)
tajo(c1, (120, 100), 80, -2.2, -0.6, 1.0, 1.0, 17)
paneles.append((c1, "1. UN Tajo (radio 80, pleno)"))

# 2. EL FLORECER completo (t=26)
c2 = florecer((120, 100), 71, 26)
paneles.append((c2, "2. EL FLORECER (t=26)"))

# 3. LA SECUENCIA (t=14, 17, 20, 23, 26, 29)
for t in [14, 17, 20, 23, 26, 29]:
    c = florecer((120, 100), 71, t)
    paneles.append((c, f"t={t}"))

# === Componer ===
def componer(paneles, cols, ancho, alto, titulo):
    filas = (len(paneles) + cols - 1) // cols
    hoja = np.full((filas * (alto + 10) + 34, cols * (ancho + 10) + 10, 3), 12.0)
    for i, (p, lbl) in enumerate(paneles):
        fy, fx = divmod(i, cols)
        y = 26 + fy * (alto + 10); x = 10 + fx * (ancho + 10)
        hoja[y:y + alto, x:x + ancho] = p
    im = Image.fromarray(np.clip(hoja, 0, 255).astype(np.uint8))
    d = ImageDraw.Draw(im)
    try:
        fnt = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 15)
        fnt2 = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 11)
    except Exception:
        fnt = ImageFont.load_default(); fnt2 = fnt
    d.text((12, 4), titulo, fill=(240, 240, 240), font=fnt)
    for i, (p, lbl) in enumerate(paneles):
        fy, fx = divmod(i, cols)
        y = 26 + fy * (alto + 10); x = 10 + fx * (ancho + 10)
        d.text((x + 4, y + 2), lbl, fill=(200, 200, 210), font=fnt2)
    return im

# Hoja A: la inspección + el florecer
hojaA = componer(paneles[:2], 2, PW, PH, "MOCK 1:1 v6.40 — EL Tajo (TajoLib, aditivo real SrcA/One)")
hojaA.save(os.path.join(OUT_DIR, "MOCK_Tajo_DETALLE.png"))
print("detalle OK")

# Hoja B: la secuencia (la causalidad retrasada)
hojaB = componer(paneles[2:], 3, PW, PH, "MOCK 1:1 v6.40 — EL CORTE DIFERIDO (secuencia de 2 en 2 ticks)")
hojaB.save(os.path.join(OUT_DIR, "MOCK_Tajo_SECUENCIA.png"))
print("secuencia OK")
