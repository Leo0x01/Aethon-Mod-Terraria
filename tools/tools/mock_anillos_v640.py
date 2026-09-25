#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_anillos_v640.py — EL MOCK 1:1 DE LOS COSMÉTICOS RÚNICOS (v6.40).

El pipeline EXACTO verificado contra el binario real:
  · Carga de texturas de tML (ImageIO.ToRaw): RGBA straight, PERO los
    píxeles con alfa==0 pasan a RGB=0 ("mirror XNA behaviour of zeroing
    out textures with full alpha zero" — el comentario literal del fuente).
  · Shader de SpriteBatch (FNA SpriteEffect.fx): src = tex × tint
    (canal a canal — sin premultiplicar).
  · BlendState.Additive del FNA.dll REAL (decodificado del IL del .cctor):
    ColorSourceBlend=SourceAlpha, ColorDestinationBlend=One
    → out.rgb = src.rgb × src.a + dst.rgb   (¡el alfa SÍ modula!)
  · BlendState.AlphaBlend: One / InverseSourceAlpha (fórmula PREMULTIPLICADA)
    → out.rgb = src.rgb + dst.rgb × (1 − src.a)
    Con texturas straight (RGB=255) el RGB entra ENTERO = blobs over-bright.

Se simulan (todo medido de los .cs reales, constantes literales):
  A. EL ANILLO RÚNICO DORSAL (AnilloDorsalRenderer → OrbitaLib, aditivo)
     a escala de jugador (altura 42 → r = 0.348·42 = 14.616 px).
  B. LA CORONA RÚNICA ROSADA (SigiloLib.ArcoGloria, camino DrawData del
     pase de jugador = AlphaBlend) a escala 1.
  C. REFERENCIA: la misma CoronaConjuro a r=60 (la escala del Supremo).

Salida: research/anillos_v640/MOCK_ANILLOS_v640.png
"""
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import os

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = os.path.join(RAIZ, "research", "anillos_v640")
os.makedirs(OUT_DIR, exist_ok=True)

# ==================================================================
#  1. LA CARGA (ImageIO.ToRaw de tML: alfa=0 → RGB=0)
# ==================================================================
def cargar(path):
    im = Image.open(path).convert("RGBA")
    a = np.array(im).astype(np.float64)
    zero = a[:, :, 3] == 0
    a[zero, 0:3] = 0.0
    return a

TEX_GLOW = cargar(os.path.join(RAIZ, "Content/Effects/Procedural/SoftGlow.png"))
TEX_RING = cargar(os.path.join(RAIZ, "Content/Effects/Procedural/Ring.png"))

# ==================================================================
#  2. EL SAMPLER (bilineal, el tap 2×2 por píxel destino — sin mipmaps)
# ==================================================================
def muestra_bilineal(tex, u, v):
    """u, v en coords de TEXEL (0..W-1). Devuelve RGBA float. Vectorizado."""
    h, w = tex.shape[0], tex.shape[1]
    x0 = np.floor(u).astype(np.int64); y0 = np.floor(v).astype(np.int64)
    fx = u - x0; fy = v - y0
    x0c = np.clip(x0, 0, w - 1); x1c = np.clip(x0 + 1, 0, w - 1)
    y0c = np.clip(y0, 0, h - 1); y1c = np.clip(y0 + 1, 0, h - 1)
    t00 = tex[y0c, x0c]; t10 = tex[y0c, x1c]
    t01 = tex[y1c, x0c]; t11 = tex[y1c, x1c]
    fx = fx[..., None]; fy = fy[..., None]
    out = (t00 * (1 - fx) * (1 - fy) + t10 * fx * (1 - fy) +
           t01 * (1 - fx) * fy + t11 * fx * fy)
    return out

# ==================================================================
#  3. EL QUAD (SpriteBatch.Draw: origen centro, escala size/texSize, rot)
# ==================================================================
def quad(canvas, tex, pos, size, rot, tint_rgb, tint_a, modo):
    """
    canvas: np (H,W,3) float 0..255 (el destino acumulado).
    modo: 'add' (SrcA/One) | 'alpha' (One/InvSrcA — el pase del jugador).
    tint_rgb: (r,g,b) 0..255 — el RGB del tinte.
    tint_a: 0..255 — el ALFA del tinte.
    """
    cx, cy = pos
    sw, sh = size
    tw, th = tex.shape[1], tex.shape[0]
    # bbox en pantalla
    half = max(sw, sh) * 0.5 + 2.0
    x0 = max(0, int(cx - half)); x1 = min(canvas.shape[1] - 1, int(cx + half) + 1)
    y0 = max(0, int(cy - half)); y1 = min(canvas.shape[0] - 1, int(cy + half) + 1)
    if x1 <= x0 or y1 <= y0: return
    xs = np.arange(x0, x1 + 1) + 0.5
    ys = np.arange(y0, y1 + 1) + 0.5
    gx, gy = np.meshgrid(xs, ys)
    # a espacio local (centrado en pos)
    dx = gx - cx; dy = gy - cy
    c, s = math.cos(-rot), math.sin(-rot)
    lx = dx * c - dy * s
    ly = dx * s + dy * c
    # el quad mide size px TOTALES → half-extends sw/2, sh/2
    dentro = (np.abs(lx) <= sw / 2 + 0.5) & (np.abs(ly) <= sh / 2 + 0.5)
    if not dentro.any(): return
    # texel UV: local + centro_tex → escala tex/size
    u = (lx + sw / 2) * (tw / sw)
    v = (ly + sh / 2) * (th / sh)
    u = np.clip(u - 0.5, 0, tw - 1.001)
    v = np.clip(v - 0.5, 0, th - 1.001)
    texel = muestra_bilineal(tex, u, v)
    # el shader: src = tex × tint (canal a canal)
    src_rgb = texel[:, :, 0:3] * (np.array(tint_rgb)[None, None, :] / 255.0)
    src_a = (texel[:, :, 3] / 255.0) * (tint_a / 255.0)
    # el blend EXACTO
    dst = canvas[y0:y1 + 1, x0:x1 + 1, :]
    if modo == 'add':
        contrib = src_rgb * src_a[:, :, None]
        nuevo = dst + contrib
    else:  # alpha: One / InverseSourceAlpha
        contrib = src_rgb
        nuevo = src_rgb + dst * (1.0 - src_a[:, :, None])
    nuevo = np.clip(nuevo, 0, 255)
    region = dentro[:, :, None]
    canvas[y0:y1 + 1, x0:x1 + 1, :] = np.where(region, nuevo, dst)

def capsula(canvas, mid, largo, ancho, rot, tint_rgb, tint_a, modo):
    quad(canvas, TEX_GLOW, mid, (largo + ancho, ancho * 1.9), rot, tint_rgb, tint_a, modo)

def anillo_fino(canvas, pos, radio_visible, rot, tint_rgb, tint_a, modo):
    s = radio_visible * 2.174
    quad(canvas, TEX_RING, pos, (s, s), rot, tint_rgb, tint_a, modo)

# ==================================================================
#  4. TINT DE LA CASA (RGB intacto, alfa = intensidad)
# ==================================================================
def tint(rgb, f):
    return (rgb, max(0.0, min(1.0, f)) * 255.0)

# Color × float de XNA (los glows de la corona rosa lo usan: RGB y ALFA × f)
def color_mul(rgb, f):
    f = max(0.0, min(1.0, f))
    return (tuple(c * f for c in rgb), 255.0 * f)

# ==================================================================
#  5. LOS ALFABETOS (literales de OrbitaLib.RunasVacio / SigiloLib.RunasCorona)
# ==================================================================
RUNAS_VACIO = [
    [(0,-7),(0,7),(-3.5,-3),(0,-6.5),(3.5,-3),(0,-6.5),(-3.5,3.5),(3.5,3.5),(-2,5.5),(2,5.5)],           # S0
    [(0,7),(0,-4),(0,-4),(-3,-7),(0,-4),(3,-7),(-2.5,0),(2.5,0),(-2.5,3),(2.5,3)],                        # S1
    [(-3.5,7),(-3.5,-5),(-3.5,-5),(3.5,-5),(3.5,-5),(3.5,7),(-3.5,-5),(0,-7),(-1.5,1.5),(1.5,1.5)],       # S2
    [(0,7),(0,-7),(-4,0),(4,0),(-2.5,-4.5),(2.5,4.5),(2.5,-4.5),(-2.5,4.5)],                               # S3
    [(-3.5,6),(-3.5,-4),(-3.5,-4),(3.5,-4),(3.5,-4),(3.5,6),(-3.5,6),(3.5,6),(-3.5,-6.5),(3.5,-6.5),(0,-4),(0,-6.5)],  # S4
    [(-3,6),(-3,-2),(-3,-2),(3,-6),(3,-6),(3,2),(3,2),(-2.5,6),(-1.5,-6.5),(1.5,-6.5)],                    # S5
    [(-4,0),(0,-4),(0,-4),(4,0),(4,0),(0,4),(0,4),(-4,0),(-1.5,0),(1.5,0),(0,-7),(0,-4.5),(0,4.5),(0,7)],  # S6
    [(-4,5.5),(-4,-5.5),(-4,-5.5),(-2,-1),(-2,-1),(0,-6.5),(0,-6.5),(2,-1),(2,-1),(4,-5.5),(4,-5.5),(4,5.5),(-4,5.5),(4,5.5)],  # S7
]
RUNAS_CORONA = [
    [(-2.5,7),(-2.5,-7),(-2.5,-2),(3,-6),(-2.5,3),(2.5,-1.5)],      # R0 lanza
    [(-4,-6),(0,3),(4,-6),(0,3),(-2,5.5),(2,5.5)],                  # R1 cáliz
    [(-3,7),(-3,-7),(3,7),(3,-7),(-3,-5),(3,-5),(-3,2),(3,2)],      # R2 puerta
    [(0,7),(0,-7),(-4,0),(4,0),(-3,-5),(3,5),(3,-5),(-3,5)],        # R3 estrella
    [(-2,7),(0.5,1),(0.5,1),(-2,-3),(-2,-3),(2.5,-7)],              # R4 rayo
    [(-3,6),(-1,-6),(1,-6),(3,6),(-1.5,-6),(1.5,-6)],               # R5 arco
    [(-3,6),(-3,-2),(-3,-2),(3,-6),(3,-6),(3,2),(3,2),(-2,6)],      # R6 espiral
    [(0,7),(0,-7),(-3,-2),(0,-7),(0,-2),(3,-7),(-2,4),(2,4)],       # R7 trono
]

# Las paletas (literales)
RUNA_BLANCA = (255,245,220);  RUNA_BLANCA_TIP = (255,252,240)
RUNA_DORADA = (255,180,70);   RUNA_DORADA_TIP = (255,235,175)
RUNA_VIOLETA = (110,130,255); RUNA_VIOLETA_TIP = (205,220,255)
GLYPH_BASE = (255,0,85); PEARL = (255,153,187); PEARL_CORE = (255,240,245); ARC_HALO = (255,20,147)

def lerp(c1, c2, t):
    return tuple(c1[i] + (c2[i] - c1[i]) * t for i in range(3))

# ==================================================================
#  6. RUNA VACÍA (OrbitaLib.RunaVacia — literal)
# ==================================================================
def runa_vacia(canvas, glyph_pos, time, g, strokes, body, tip, gs, alpha_mul, modo, nervioso=False):
    pulse = 0.70 + 0.30 * math.sin(time * 2.8 + g * 1.7) if nervioso else 0.75 + 0.25 * math.sin(time * 2.4 + g * 1.3)
    glow_size = 30.0 if nervioso else 36.0
    glow_alpha = 0.18 if nervioso else 0.20
    quad(canvas, TEX_GLOW, glyph_pos, (glow_size * gs, glow_size * gs), 0.0, *tint(body, glow_alpha * pulse * alpha_mul), modo)
    w = 3.2 if nervioso else 3.4
    a = 0.80 if nervioso else 0.85
    for s in range(0, len(strokes), 2):
        pa = (glyph_pos[0] + strokes[s][0] * gs, glyph_pos[1] + strokes[s][1] * gs)
        pb = (glyph_pos[0] + strokes[s+1][0] * gs, glyph_pos[1] + strokes[s+1][1] * gs)
        mid = ((pa[0]+pb[0])*0.5, (pa[1]+pb[1])*0.5)
        delta = (pb[0]-pa[0], pb[1]-pa[1]); ln = math.hypot(*delta)
        if ln < 0.01: continue
        rot = math.atan2(delta[1], delta[0])
        local_y = ((strokes[s][1] + strokes[s+1][1]) * 0.5 + 7.0) / 14.0
        col = lerp(tip, body, 1.0 - local_y * 0.25)
        capsula(canvas, mid, ln, w * gs, rot, *tint(col, a * pulse * alpha_mul), modo)
    pearl_off = 10.5 if nervioso else 11.5
    pearl_a = 6.2 if nervioso else 7.0
    pearl_b = 2.9 if nervioso else 3.2
    pearl_pos = (glyph_pos[0], glyph_pos[1] - pearl_off * gs)
    pearl_pulse = pulse if nervioso else 0.8 + 0.2 * math.sin(time * 3.0 + g * 2.0)
    quad(canvas, TEX_GLOW, pearl_pos, (pearl_a * gs, pearl_a * gs), 0.0, *tint(body, (0.55 if nervioso else 0.62) * pulse * alpha_mul), modo)
    quad(canvas, TEX_GLOW, pearl_pos, (pearl_b * gs, pearl_b * gs), 0.0, *tint(tip, 0.9 * pearl_pulse * alpha_mul), modo)

# ==================================================================
#  7. CÍRCULO RÚNICO (OrbitaLib.CirculoRunico — literal)
# ==================================================================
def circulo_runico(canvas, center, r, time, radius, count, orbit, body, tip, gs, offset, ring_alpha, alpha_mul, modo):
    anillo_fino(canvas, center, radius * r, time * orbit, *tint(body, ring_alpha * alpha_mul), modo)
    breathe_amp, breathe_hz, breathe_ph = 2.4, 1.35, 0.9
    bob_amp, bob_hz, bob_ph = 2.0, 0.85, 1.7
    tabla = RUNAS_VACIO
    for g in range(count):
        ang = g / count * math.tau + time * orbit
        float_r = radius * r + breathe_amp * gs * math.sin(time * breathe_hz + g * breathe_ph)
        bob_y = bob_amp * gs * math.sin(time * bob_hz + g * bob_ph)
        glyph_pos = (center[0] + math.cos(ang) * float_r, center[1] + math.sin(ang) * float_r + bob_y)
        strokes = tabla[(g + offset) % len(tabla)]
        runa_vacia(canvas, glyph_pos, time, g, strokes, body, tip, gs, alpha_mul, modo)

# CoronaConjuro (literal) + anillo de fotones del dorsal
def corona_conjuro(canvas, center, r, time, alpha_mul, glyph_mul, modo):
    gs = max(r / 52.0, 0.25) * 1.40 * glyph_mul
    # dorado: AroMedio=2.62, 8, GiroMedio=0.10
    circulo_runico(canvas, center, r, time, 2.62, 8, 0.10, RUNA_DORADA, RUNA_DORADA_TIP, gs, 0, 0.24, alpha_mul, modo)
    # violeta: AroExterno=3.30, 6, GiroExterno=-0.075, gs×0.85
    circulo_runico(canvas, center, r, time, 3.30, 6, -0.075, RUNA_VIOLETA, RUNA_VIOLETA_TIP, gs * 0.85, 3, 0.18, alpha_mul, modo)
    # blanco: AroIntimo=2.02, 6, GiroIntimo=0.16, gs×0.92
    circulo_runico(canvas, center, r, time, 2.02, 6, 0.16, RUNA_BLANCA, RUNA_BLANCA_TIP, gs * 0.92, 6, 0.20, alpha_mul, modo)

def dorsal_actual(canvas, center, altura, time, modo='add'):
    """AnilloDorsalRenderer.Draw — literal."""
    r = altura * 0.348
    fotones = 0.30 + 0.12 * math.sin(time * 1.7)
    anillo_fino(canvas, center, 1.35 * r, -time * 0.06, *tint(RUNA_BLANCA, fotones), modo)
    corona_conjuro(canvas, center, r, time, 1.0, 2.4, modo)

# ==================================================================
#  8. ARCO GLORIA (SigiloLib.ArcoGloria — literal, camino DrawData/Alpha)
# ==================================================================
def arco_gloria(canvas, anchor, scale, time, alpha=1.0, modo='alpha'):
    arc_radius = 26.0 * scale
    arc_center = (anchor[0], anchor[1] - 13.0 * scale)
    for h in range(3):
        ha = -math.pi / 2 + (h - 1) * 0.62
        hpos = (arc_center[0] + math.cos(ha) * arc_radius * 0.92,
                arc_center[1] + math.sin(ha) * arc_radius * 0.92)
        hpulse = 0.7 + 0.3 * math.sin(time * 1.6 + h * 2.1)
        # VFXCore.Quad(hpos, ArcHalo * (0.10·hpulse·alpha), 30×30) — Color×float
        quad(canvas, TEX_GLOW, hpos, (30.0 * scale, 30.0 * scale), 0.0, *color_mul(ARC_HALO, 0.10 * hpulse * alpha), modo)
    for g in range(8):
        angle = -math.pi + math.pi * 0.11 + g / 7.0 * math.pi * 0.78
        float_r = arc_radius + 2.4 * scale * math.sin(time * 1.35 + g * 0.9)
        bob_y = 2.0 * scale * math.sin(time * 0.85 + g * 1.7)
        glyph_pos = (arc_center[0] + math.cos(angle) * float_r,
                     arc_center[1] + math.sin(angle) * float_r + bob_y)
        pulse = 0.75 + 0.25 * math.sin(time * 2.4 + g * 1.3)
        strokes = RUNAS_CORONA[g % 8]
        stroke_w = 2.1 * scale
        for s in range(0, len(strokes), 2):
            a = (glyph_pos[0] + strokes[s][0] * scale, glyph_pos[1] + strokes[s][1] * scale)
            b = (glyph_pos[0] + strokes[s+1][0] * scale, glyph_pos[1] + strokes[s+1][1] * scale)
            mid = ((a[0]+b[0])*0.5, (a[1]+b[1])*0.5)
            delta = (b[0]-a[0], b[1]-a[1]); ln = math.hypot(*delta)
            if ln < 0.01: continue
            rot = math.atan2(delta[1], delta[0])
            local_y = ((strokes[s][1] + strokes[s+1][1]) * 0.5 + 7.0) / 14.0
            stroke_col = lerp(GLYPH_BASE, PEARL, 1.0 - local_y * 0.75)
            # VFXCore.Quad(mid, strokeCol * (pulse·alpha), (len+w, w), rot) — Color×float
            quad(canvas, TEX_GLOW, mid, (ln + stroke_w, stroke_w), rot, *color_mul(stroke_col, pulse * alpha), modo)
        pearl_pulse = 0.8 + 0.2 * math.sin(time * 3.0 + g * 2.0)
        pearl_pos = (glyph_pos[0], glyph_pos[1] - 11.5 * scale)
        quad(canvas, TEX_GLOW, pearl_pos, (7.0 * scale, 7.0 * scale), 0.0, *color_mul(PEARL, pulse * alpha), modo)
        quad(canvas, TEX_GLOW, pearl_pos, (3.2 * scale, 3.2 * scale), 0.0, *color_mul(PEARL_CORE, pearl_pulse * alpha), modo)

# ==================================================================
#  9. EL JUGADOR (silueta simple para contexto)
# ==================================================================
def silueta_jugador(draw, cx, top, h=42, w=20):
    # cuerpo simple: cabeza + torso + piernas
    draw.ellipse([cx - 5, top, cx + 5, top + 10], outline=(200, 190, 180), width=1)
    draw.rectangle([cx - 8, top + 11, cx + 8, top + 28], outline=(200, 190, 180), width=1)
    draw.rectangle([cx - 8, top + 28, cx + 8, top + 42], outline=(160, 150, 140), width=1)

# ==================================================================
#  10. LOS PANELES
# ==================================================================
def fondo_noche(w, h):
    base = np.zeros((h, w, 3)); base[:, :] = (18, 24, 38)
    ruido = np.random.RandomState(7).randint(-4, 5, (h, w, 3))
    return np.clip(base + ruido, 0, 255)

def fondo_dia(w, h):
    base = np.zeros((h, w, 3)); base[:, :] = (108, 148, 205)
    ruido = np.random.RandomState(11).randint(-6, 7, (h, w, 3))
    return np.clip(base + ruido, 0, 255)

def panel(w, h, fondo_fn, dibujar, etiqueta):
    canvas = fondo_fn(w, h)
    # 1. el dibujo (glows/runas → numpy canvas)
    dibujar(canvas)
    # 2. la silueta ENCIMA (vanilla: proyectiles antes que jugadores → el
    #    cuerpo del jugador TAPA lo que tiene detrás)
    pil = Image.fromarray(np.clip(canvas, 0, 255).astype(np.uint8))
    d = ImageDraw.Draw(pil)
    d.text((8, 6), etiqueta, fill=(255, 255, 255))
    return np.array(pil).astype(np.float64)

PW, PH = 320, 260
TIME = 2.35  # un instante arbitrario de la animación

# --- A. DORAL ACTUAL (aditivo) a escala jugador ---
def dib_dorsal(canvas):
    cx, cy = PW // 2, PH // 2 + 10
    dorsal_actual(canvas, (cx, cy), 42.0, TIME, 'add')

paneles = []
paneles.append(panel(PW, PH, fondo_noche, dib_dorsal, "A. DORSAL ACTUAL (aditivo) - noche"))
paneles.append(panel(PW, PH, fondo_dia, dib_dorsal, "A. DORSAL ACTUAL (aditivo) - dia"))

# --- B. CORONA ROSA ACTUAL (AlphaBlend del pase de jugador) ---
def dib_rosa(canvas):
    cx, cy = PW // 2, PH // 2 + 20
    arco_gloria(canvas, (cx, cy - 21 + 42 * 0.22 + 8), 1.0, TIME, 1.0, 'alpha')

paneles.append(panel(PW, PH, fondo_noche, dib_rosa, "B. CORONA ROSA ACTUAL (AlphaBlend) - noche"))
paneles.append(panel(PW, PH, fondo_dia, dib_rosa, "B. CORONA ROSA ACTUAL (AlphaBlend) - dia"))

# --- C. REFERENCIA: CoronaConjuro a r=60 (el Supremo, aditivo) ---
def dib_supremo(canvas):
    cx, cy = PW // 2, PH // 2
    corona_conjuro(canvas, (cx, cy), 60.0, TIME, 1.0, 1.0, 'add')

paneles.append(panel(PW, PH, fondo_noche, dib_supremo, "C. REFERENCIA Supremo r=60 (aditivo) - noche"))

# --- D. ZOOM ×3 del dorsal actual (para inspección) ---
def dib_dorsal_zoom(canvas):
    cx, cy = PW // 2, PH // 2
    dorsal_actual(canvas, (cx, cy), 42.0 * 3.0, TIME, 'add')  # truco: altura×3 = misma geometría a ×3

paneles.append(panel(PW, PH, fondo_noche, dib_dorsal_zoom, "D. DORSAL ×3 (inspeccion aliasing)"))

# --- E. EL SELLO DEL GÉNESIS NO — pero el AnillosSol: aro elíptico por cápsulas (como el sol, DrawData/alpha) ---
# El sol dibuja aros con CÁPSULAS (EmitAroElipse): esto es lo que el usuario ve "bien" en los soles.
def dib_aro_solar(canvas):
    cx, cy = PW // 2, PH // 2
    R = 42 * 0.46
    for k in range(7):
        a = (1.62 + k * 0.30) * R
        b = a * (0.78 - k * 0.022)
        tilt = 0.22 * math.sin(TIME * 0.35 + k * 1.9) if k >= 5 else 0.11 * (k % 2) + 0.06 * math.sin(TIME * 0.3 + k)
        spin = TIME * (0.24 if k % 2 == 0 else -0.18)
        segs = 36
        cuerpo = RUNA_DORADA if k % 3 != 2 else (135, 165, 255)
        pulse = 0.78 + 0.08 * math.sin(TIME * 1.30)
        for s in range(segs):
            t0 = s / segs * math.tau + spin
            t1 = (s + 1) / segs * math.tau + spin
            p0 = (cx + math.cos(t0) * a * math.cos(tilt) - math.sin(t0) * b * math.sin(tilt),
                  cy + math.cos(t0) * a * math.sin(tilt) + math.sin(t0) * b * math.cos(tilt))
            p1 = (cx + math.cos(t1) * a * math.cos(tilt) - math.sin(t1) * b * math.sin(tilt),
                  cy + math.cos(t1) * a * math.sin(tilt) + math.sin(t1) * b * math.cos(tilt))
            mid = ((p0[0]+p1[0])*0.5, (p0[1]+p1[1])*0.5)
            delta = (p1[0]-p0[0], p1[1]-p0[1]); ln = math.hypot(*delta)
            rot = math.atan2(delta[1], delta[0])
            capsula(canvas, mid, ln, max(2.2, 0.052 * a) * (1.0 + 0.35 * (s / segs)),
                    rot, *tint(cuerpo, (0.30 + 0.30 * (s / segs)) * pulse), 'alpha')

paneles.append(panel(PW, PH, fondo_noche, dib_aro_solar, "E. ARO SOLAR por capsulas (DrawData alpha) - noche"))

# --- Componer la hoja ---
cols = 2
filas = (len(paneles) + 1) // cols
hoja = np.full((filas * (PH + 10) + 30, cols * (PW + 10) + 10, 3), 12.0)
for i, p in enumerate(paneles):
    fy, fx = divmod(i, cols)
    y = 20 + fy * (PH + 10); x = 10 + fx * (PW + 10)
    hoja[y:y + PH, x:x + PW] = p

titulo = Image.fromarray(np.clip(hoja, 0, 255).astype(np.uint8))
dd = ImageDraw.Draw(titulo)
try:
    fnt = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 16)
except Exception:
    fnt = ImageFont.load_default()
dd.text((12, 2), "MOCK 1:1 v6.40 — cosméticos rúnicos: el pipeline EXACTO (carga tML + shader FNA + blends reales)", fill=(240, 240, 240), font=fnt)

out = os.path.join(OUT_DIR, "MOCK_ANILLOS_v640.png")
titulo.save(out)
print("Guardado:", out)
print("Paneles:", len(paneles))
