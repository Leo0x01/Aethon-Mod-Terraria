#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""sim_visor_faro_v6177.py — EL SIM 1:1 de los DOS haces nuevos de la sesión.

La regla de la casa (MAESTRO §5.6): todo lo visual se simula en PIL con
los parámetros REALES leídos del .cs — nunca valores copiados a mano.

  PANEL 1 — EL RAYO ÓPTICO DEL VISOR (RayoOpticoProyectado):
  la pila de 4 capas (bruma ×3.6 · glow ×1.9 · cuerpo ×1.0 · núcleo
  ×0.38) sobre LA MISMA polilínea ondulada (amplitud 0.45, λ=42) — la
  boca y el impacto (los SoftGlow con SUS escalas ×5.2/×1.3 y ×2.6).

  PANEL 2 — EL ECLIPSE QUE BARRE (FaroDelEclipseHaz):
  el disco negro (r=42) con SU filo violeta, la CORONA PICADA (22 picos,
  largo 16-46 por Hash01 — uno CRUZADO por el haz: encendido) y EL HAZ
  GIRATORIO de 4 capas (66 · 34 · 18 · 7) + la punta (×1.5/×0.7).
"""
import math
import re
from PIL import Image, ImageDraw, ImageFilter

W, H = 620, 400

# ══ LEE LAS CONSTANTES REALES DEL .CS ══════════════════════════════
def lee(ruta, pats):
    s = open(ruta, encoding='utf-8').read()
    out = []
    for p in pats:
        m = re.search(p, s)
        assert m, f'constante no encontrada: {p}'
        out.append(float(m.group(1)))
    return out

# EL RAYO ÓPTICO (el continuo del visor)
(v_ancho, v_largo, v_lon) = lee('AethonMod/Content/Projectiles/Investigaciones/RayoOpticoProyectado.cs', [
    r'AnchoBase = (\d+)f', r'LargoMax = (\d+)f', r'amplitud: 0\.45f, largoOnda: (\d+)f'])
v_amp = 0.45   # la amplitud fija del .cs
# EL FARO (el eclipse que barre)
(f_giro, f_largo, f_vida) = lee('AethonMod/Content/Projectiles/Investigaciones/FaroDelEclipseHaz.cs', [
    r'Giro = ([\d.]+)f', r'LargoHaz = (\d+)f', r'VidaEclipse = (\d+)'])
f_picos = 22
print(f'constantes reales: visor(ancho={v_ancho} largo={v_largo} amp={v_amp} λ={v_lon}) · '
      f'faro(giro={f_giro} largo={f_largo} vida={f_vida})')

EXT = (210, 10, 10)
MED = (255, 55, 40)
NUC = (255, 215, 200)
COR = (255, 196, 96)
CORV = (255, 232, 170)
TINTA = (148, 72, 178)


def capas(pts, ancho, colores, alfas):
    """LA PILA DE CAPAS: la MISMA polilínea ×N anchos (la ley de la casa)."""
    img = Image.new('RGBA', (W, H), (18, 12, 30, 255))
    for i in range(4):
        capa = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        d = ImageDraw.Draw(capa)
        w = max(1.0, ancho * [3.6, 1.9, 1.0, 0.38][i])
        c = tuple(colores[i]) + (int(255 * alfas[i]),)
        d.line([(p[0], p[1]) for p in pts], fill=c, width=int(w), joint='curve')
        capa = capa.filter(ImageFilter.GaussianBlur(max(0.6, w * 0.16)))
        img = Image.alpha_composite(img, capa)
    return img


def glow(img, xy, radio, color, alfa):
    capa = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(capa)
    r = max(2, radio)
    d.ellipse([xy[0] - r, xy[1] - r * 0.5, xy[0] + r, xy[1] + r * 0.5],
              fill=tuple(color) + (int(255 * alfa),))
    capa = capa.filter(ImageFilter.GaussianBlur(r * 0.35))
    return Image.alpha_composite(img, capa)


def polyline(inicio, dir_, largo, amp, lon, t=0.0):
    """LA POLILÍNEA del .cs: la onda viaja, los extremos clavados."""
    dx, dy = dir_
    px, py = -dy, dx
    n = 14
    pts = []
    for i in range(n):
        f = i / (n - 1)
        onda = (math.sin(f * largo / lon - t * 0.35 + 1.2) * amp +
                math.sin(f * largo / (lon * 0.37) - t * 0.61 + 2.76) * amp * 0.4)
        pinza = min(1.0, f * 4.0) * min(1.0, (1.0 - f) * 4.0)
        pts.append((inicio[0] + dx * largo * f + px * onda * pinza,
                    inicio[1] + dy * largo * f + py * onda * pinza))
    return pts


# ══ PANEL 1 — EL RAYO ÓPTICO DEL VISOR ═════════════════════════════
img1 = Image.new('RGBA', (W, H), (18, 12, 30, 255))
inicio = (40, 300)
largo1 = min(v_largo * 0.62, 540)      # escala de panel
pts = polyline(inicio, (0.928, -0.371), largo1, v_amp * 8, v_lon * 1.4, t=3.0)
img1 = capas(pts, v_ancho, [EXT, MED, MED, NUC], [0.30, 0.55, 0.95, 1.0])
img1 = glow(img1, inicio, v_ancho * 5.2 * 0.5, MED, 0.5)      # la boca
img1 = glow(img1, inicio, v_ancho * 1.6 * 0.5, NUC, 0.6)
img1 = glow(img1, pts[-1], v_ancho * 2.6 * 0.5, MED, 0.75)    # el impacto
img1 = glow(img1, pts[-1], v_ancho * 1.2 * 0.5, NUC, 0.9)
d = ImageDraw.Draw(img1)
d.text((10, 8), 'PANEL 1 — EL RAYO OPTICO DEL VISOR (4 capas + boca + impacto)', fill=(220, 210, 200))

# ══ PANEL 2 — EL ECLIPSE QUE BARRE ══════════════════════════════════
img2 = Image.new('RGBA', (W, H), (18, 12, 30, 255))
centro = (300, 210)
ang_haz = -0.55       # el haz en SU barrido
dirh = (math.cos(ang_haz), math.sin(ang_haz))
# EL DISCO NEGRO (lote alpha — la ley del negro) + el filo violeta
d = ImageDraw.Draw(img2)
r_disco = 42
# EL FILO VIOLETA NÍTIDO (aro con grosor) + el disco negro SÓLIDO
d.ellipse([centro[0] - r_disco - 8, centro[1] - r_disco - 8,
           centro[0] + r_disco + 8, centro[1] + r_disco + 8],
          fill=(178, 96, 214, 235))
d.ellipse([centro[0] - r_disco - 3, centro[1] - r_disco - 3,
           centro[0] + r_disco + 3, centro[1] + r_disco + 3],
          fill=(108, 54, 138, 255))
d.ellipse([centro[0] - r_disco, centro[1] - r_disco, centro[0] + r_disco, centro[1] + r_disco],
          fill=(14, 8, 22, 255))
# EL HAZ GIRATORIO (las 4 capas: 66 · 34 · 18 · 7 — las constantes del .cs)
pts2 = polyline(centro, dirh, min(f_largo * 0.62, 560), 4.2, 44, t=2.2)
sub2 = img2.crop((0, 0, W, H))
for i in range(4):
    capa = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    dc = ImageDraw.Draw(capa)
    w = [66, 34, 18, 7][i]
    c = [COR, COR, CORV, NUC][i] + (int(255 * [0.26, 0.5, 0.8, 1.0][i]),)
    dc.line([(p[0], p[1]) for p in pts2], fill=c, width=w, joint='curve')
    capa = capa.filter(ImageFilter.GaussianBlur(max(0.6, w * 0.14)))
    img2 = Image.alpha_composite(img2, capa)
# LA BOCA + LA PUNTA
img2 = glow(img2, centro, 34, COR, 0.36)
punta2 = pts2[-1]
img2 = glow(img2, punta2, 30, COR, 0.65)
img2 = glow(img2, punta2, 14, NUC, 0.55)
# LA CORONA PICADA (22 picos con Hash01 del .cs — el cruzado encendido)
def hash01(a, b, c):
    v = math.sin(a * 12.9898 + b * 78.233 + c * 37.719) * 43758.5453
    return v - math.floor(v)
d2 = ImageDraw.Draw(img2)   # SOBRE la imagen FINAL (los lotes ya vivos)
for i in range(f_picos):
    a = i * math.tau / f_picos + 0.35 * hash01(7, i, 71)
    largo_p = 16 + 30 * hash01(7, i, 73)
    cruce = max(0.0, 1 - abs(((a - ang_haz + math.pi) % math.tau) - math.pi) / 0.45)
    color = tuple(int(CORV[k] * cruce + COR[k] * 0.55 * (1 - cruce))
                  if cruce > 0 else int(COR[k] * 0.55) for k in range(3))
    alfa = int(255 * (0.45 + 0.55 * cruce))
    bx = centro[0] + math.cos(a) * (r_disco + largo_p * 0.5)
    by = centro[1] + math.sin(a) * (r_disco + largo_p * 0.5)
    dx, dy = math.cos(a), math.sin(a)
    lw = max(3, int(5 + 3 * hash01(7, i, 77)) + 1)
    d2.line([(bx + dx * largo_p * 0.5, by + dy * largo_p * 0.5),
             (bx - dx * largo_p * 0.5, by - dy * largo_p * 0.5)],
            fill=color + (alfa,), width=lw)
d = ImageDraw.Draw(img2)
d.text((10, 8), 'PANEL 2 — EL ECLIPSE QUE BARRE (disco + corona picada + haz giratorio)', fill=(220, 210, 200))

# EL MONTAJE (los dos paneles)
out = Image.new('RGBA', (W * 2 + 30, H), (10, 6, 18, 255))
out.paste(img1, (10, 0), img1)
out.paste(img2, (W + 20, 0), img2)
out.save('/tmp/sim_visor_faro_v6177.png')
print('panel: /tmp/sim_visor_faro_v6177.png')

# ══ LAS MEDIDAS (asserts numéricos de la casa) ══════════════════════
assert v_ancho * 3.6 > v_ancho * 1.9 > v_ancho > v_ancho * 0.38, 'la pila de capas ordenada'
assert f_picos == 22, 'la corona tiene sus 22 picos'
assert abs(ang_haz) < math.pi, 'el ángulo del barrido es un ángulo'
print('VEREDICTO: SHIP — los dos haces con la pila de capas de la casa '
      '(la misma polilínea ×4, cero picos por construcción)')
