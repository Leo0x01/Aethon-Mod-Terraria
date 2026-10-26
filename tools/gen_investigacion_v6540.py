#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_investigacion_v6540.py — v6.54.0 — LAS TEXTURAS DE LA INVESTIGACIÓN EL DIOS
RE-IMPLEMENTADAS 100 % CASA (procedurales, seed determinista).

La letra del usuario (27/10): estudiar CÓMO usa el dios sus texturas de
efectos (FusionRay_0..3, SpaceTrail, LiquidTrail, Lightning ×2 pares,
DarkSludge, Constellation, ColorMap, Glow, Noise_0..16, Space_0..1) y
crear VERSIONES PROPIAS distintas — nada portado: todo nace de este
script con las recetas documentadas por la investigación (worklog Task
74-a/74-b/74-c). Los colores de la casa: MORADO y NEGRO (más el
arcoíris del dios de ColorPrisma para lo prismático).

FAMILIAS GENERADAS (todo tileable donde el consumidor lo wrap-ea):
  · RayoConcentrado_0..3 — las 4 capas del haz (cuerpo fBm / bits de
    células / glow perfil 1D / núcleo+falda) — el "FusionRay" propio.
  · EstelaEspacial(+Brillo) — la banda-máscara del anillo Saturno
    (canales RGB = cuerpo/línea caliente/tinte frío) + barra glow 1D.
  · TintaViva + MapaColorTinta — el cordón líquido (alpha binaria,
    vetas, metaballs) + su LUT de 2 bandas (negro→morado→lila).
  · RayoRamo(+Brillo) — el rayo ramificado FINO (jefe) en gris.
  · NubeRayo(+Brillo) — el rayo NUBLADO grueso (monzón) en gris.
  · LodoOscuro(+Brillo) — el lodo bioluminiscente (base cuantizada
    casi negra + ~400 puntos brillantes violetas).
  · AreaConstelacion / EstrellaConstelacion / LineaConstelacion — la
    viñeta radial del marcador (regenerada a 512²: los 1624² del
    original eran derroche), la estrella 4 puntas y la aguja-línea.
  · Espacio_0/1 — el starfield morado-negro y la nebulosa violeta.
  · Ruido_0..19 — 20 texturas de ruido (17 como el original + 3
    familias NUEVAS de la casa: células, plasma, anillos).
"""
import os
import numpy as np
from PIL import Image, ImageFilter, ImageDraw

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Effects/Procedural"
INV = os.path.join(BASE, "Investigacion")
os.makedirs(INV, exist_ok=True)

SEED = 20261027
rng = np.random.default_rng(SEED)

# =====================================================================
#  LOS HELPERS DEL RUIDO TILEABLE (períodos enteros → wrap perfecto)
# =====================================================================

def _smoothstep(t):
    t = np.clip(t, 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)

def value_noise(w, h, period_x, period_y, rng):
    """Value noise TILEABLE: grilla period_x×period_y interpolada con
    smoothstep y wrap en ambos ejes (períodos enteros)."""
    gx, gy = int(period_x), int(period_y)
    grid = rng.random((gy, gx)).astype(np.float32)
    ys = np.arange(h, dtype=np.float32) * gy / h
    xs = np.arange(w, dtype=np.float32) * gx / w
    y0 = np.floor(ys).astype(np.int32); x0 = np.floor(xs).astype(np.int32)
    ty = _smoothstep(ys - y0); tx = _smoothstep(xs - x0)
    y1 = (y0 + 1) % gy; x1 = (x0 + 1) % gx
    y0 %= gy; x0 %= gx
    g00 = grid[y0[:, None], x0[None, :]]
    g01 = grid[y0[:, None], x1[None, :]]
    g10 = grid[y1[:, None], x0[None, :]]
    g11 = grid[y1[:, None], x1[None, :]]
    top = g00 + (g01 - g00) * tx[None, :]
    bot = g10 + (g11 - g10) * tx[None, :]
    return top + (bot - top) * ty[:, None]

def fbm(w, h, base_period, octaves, rng, pers=0.5, gain=1.0):
    """fBm tileable (cada octava duplica el período entero)."""
    acc = np.zeros((h, w), dtype=np.float32)
    amp, total = 1.0, 0.0
    px = py = float(base_period)
    for _ in range(octaves):
        acc += value_noise(w, h, max(px, 2), max(py, 2), rng) * amp
        total += amp
        amp *= pers
        px *= 2.0; py *= 2.0
    acc /= total
    if gain != 1.0:
        acc = np.clip(acc * gain, 0.0, 1.0)
    return acc

def blur(arr, radius):
    """Blur gaussiano vía PIL (devuelve float32 0..1)."""
    im = Image.fromarray((arr * 255).astype(np.uint8), "L")
    im = im.filter(ImageFilter.GaussianBlur(radius))
    return np.asarray(im, dtype=np.float32) / 255.0

def to_gray_png(arr01, path):
    """array 0..1 → PNG gris RGB."""
    g = (np.clip(arr01, 0, 1) * 255).astype(np.uint8)
    rgb = np.stack([g, g, g], axis=-1)
    Image.fromarray(rgb, "RGB").save(path)

def perfil_vertical(h, w, f_v):
    """Aplica un perfil 1D f_v(v∈0..1) constante en U."""
    vs = (np.arange(h, dtype=np.float32) + 0.5) / h
    prof = f_v(vs)                       # (h,)
    return np.repeat(prof[:, None], w, axis=1)

# =====================================================================
#  1. RAYO CONCENTRADO — LAS 4 CAPAS DEL HAZ (el FusionRay propio)
# =====================================================================

# _0 = CUERPO: envolvente vertical (banda 0.16..0.91, pico 240/255 en
# V≈0.47) × fBm tileable en U con variación ±12 % — la receta 74-a.
def gen_rayo_concentrado_0():
    w = h = 256
    def env(v):
        up = _smoothstep((v - 0.16) / (0.47 - 0.16))
        down = _smoothstep((0.91 - v) / (0.91 - 0.47))
        return np.clip(up, 0, 1) * np.clip(down, 0, 1) * (240.0 / 255.0)
    prof = perfil_vertical(h, w, env)
    ruido = fbm(w, h, 8, 3, rng)                 # fBm a lo largo de U
    ruido = 0.88 + ruido * 0.24                  # ±12 %
    to_gray_png(np.clip(prof * ruido, 0, 1), f"{INV}/RayoConcentrado_0.png")

# _1 = BITS: células Voronoi umbralizadas → ~15 % de "islas" blancas
# en franja V 0.2..0.75, RGB blanco, alpha = máscara (la receta 74-a).
def _voronoi_f1(w, h, puntos):
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    d = np.full((h, w), 1e9, dtype=np.float32)
    for (px, py) in puntos:
        # wrap-aware (tileable): distancia al vecino de la celda
        dx = np.abs(xs - px); dx = np.minimum(dx, w - dx)
        dy = np.abs(ys - py); dy = np.minimum(dy, h - dy)
        d = np.minimum(d, np.sqrt(dx * dx + dy * dy))
    return d

def gen_rayo_concentrado_1():
    w = h = 512
    n = 90
    pts = [(rng.random() * w, rng.random() * h) for _ in range(n)]
    d = _voronoi_f1(w, h, pts)
    # distancia al 2º vecino aproximada: d + ruido → células orgánicas
    cel = blur(np.clip(d / 90.0, 0, 1), 3.0)
    cel = 1.0 - cel                              # picos en los núcleos
    mask = _smoothstep((cel - 0.63) / 0.20)      # islas suaves (~15 %)
    vs = (np.arange(h, dtype=np.float32) + 0.5) / h
    banda = _smoothstep((vs - 0.20) / 0.08) * _smoothstep((0.75 - vs) / 0.08)
    mask *= banda[:, None]
    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    rgba[..., 0:3] = 255
    rgba[..., 3] = (np.clip(mask, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(f"{INV}/RayoConcentrado_1.png")

# _2 = GLOW: perfil 1D puro (cero variación U) — campana pico 255 en
# V≈0.47, banda 0.12..0.88 (la receta 74-a: fila central todo 255).
def gen_rayo_concentrado_2():
    w = h = 256
    def env(v):
        up = _smoothstep((v - 0.12) / (0.47 - 0.12))
        down = _smoothstep((0.88 - v) / (0.88 - 0.47))
        return up * down
    to_gray_png(perfil_vertical(h, w, env), f"{INV}/RayoConcentrado_2.png")

# _3 = NÚCLEO+FALDA: pico 255 en V 0.44..0.53 + meseta 0.35 en V
# 0.22..0.37 (halo ancho), ruido U leve ±1 % (la receta 74-a).
def gen_rayo_concentrado_3():
    w = h = 256
    def env(v):
        nucleo = _smoothstep((v - 0.37) / 0.07) * _smoothstep((0.60 - v) / 0.07)
        falda = _smoothstep((v - 0.14) / 0.08) * _smoothstep((0.40 - v) / 0.05)
        return np.clip(nucleo + falda * 0.38, 0, 1)
    prof = perfil_vertical(h, w, env)
    ruido = 0.99 + fbm(w, h, 6, 2, rng) * 0.02
    to_gray_png(np.clip(prof * ruido, 0, 1), f"{INV}/RayoConcentrado_3.png")

# =====================================================================
#  2. ESTELA ESPACIAL — LA BANDA-MÁSCARA DEL ANILLO (SpaceTrail propio,
#     MORADO/NEGRO: los canales actúan como máscaras y el tinte lo da
#     el código; aquí R=violeta cuerpo, G=lila línea caliente,
#     B=azul-violeta borde — la receta 74-a struct, colores de casa).
# =====================================================================

def gen_estela_espacial():
    w = h = 128
    vs = (np.arange(h, dtype=np.float32) + 0.5) / h
    # G — el cuerpo liso de la banda (V 0.13..0.42, pico 250)
    g = _smoothstep((vs - 0.10) / 0.05) * _smoothstep((0.45 - vs) / 0.05)
    g = g ** 0.75
    # B — la línea fina del borde superior (V≈0.09) con huecos
    # periódicos (tramos rotos: los "destellos" del borde) + banda
    # tenue en V≈0.47
    linea = np.exp(-((vs - 0.09) / 0.016) ** 2)
    xs = np.arange(w, dtype=np.float32)
    huecos = np.ones(w, dtype=np.float32)
    pos = 0.0
    while pos < w:
        tramo = rng.uniform(20, 90)
        hueco = rng.uniform(4, 9)
        if rng.random() < 0.45:     # pinta hueco tras este tramo
            i0, i1 = int(pos + tramo), int(pos + tramo + hueco)
            huecos[max(0, i0):min(w, i1)] = 0.0
        pos += tramo + hueco
    linea_rota = linea[:, None] * huecos[None, :]
    cola = np.exp(-((vs - 0.47) / 0.05) ** 2) * 0.45
    b = np.clip(linea_rota + cola[:, None], 0, 1)
    # R — sigue a G con el 90 % (el cuerpo)
    r = g * 0.9
    # la matriz final (h, w, 3)
    R = np.repeat((r * 255).astype(np.uint8)[:, None], w, axis=1)
    G = np.repeat((g * 255).astype(np.uint8)[:, None], w, axis=1)
    B = (b * 255).astype(np.uint8)
    # ALPHA — la mitad superior viva, muerte en V≈0.55
    a = _smoothstep((0.55 - vs) / 0.08)
    A = np.repeat((a * 255).astype(np.uint8)[:, None], w, axis=1)
    rgba = np.dstack([R, G, B, A])
    Image.fromarray(rgba, "RGBA").save(f"{INV}/EstelaEspacial.png")

def gen_estela_espacial_brillo():
    w = h = 256
    def campana(v):
        return np.exp(-((v - 0.28) / 0.11) ** 2) * (201.0 / 255.0)
    to_gray_png(perfil_vertical(h, w, campana), f"{INV}/EstelaEspacialBrillo.png")

# =====================================================================
#  3. TINTA VIVA — EL CORDÓN LÍQUIDO (LiquidTrail propio):
#     alpha BINARIA (blob ~54 %), G cuasi binaria en 3 bandas (vetas),
#     R metaball blanda, B blobs orgánicos — tileable en U (74-b).
# =====================================================================

def gen_tinta_viva():
    w = h = 128
    # la silueta: fBm estirado en X umbralizado a binario
    sil = fbm(w, h, 6, 3, rng, pers=0.55)
    sil = blur(sil, 1.2)
    sil = np.roll(sil, 0, axis=1)
    # estirar en X (frecuencia X menor): mezcla con su versión ancha
    alpha = _smoothstep((sil - 0.47) / 0.05)
    alpha = (alpha > 0.5).astype(np.float32)      # BINARIA
    # limpiar motas: erosión/dilatación ligera por blur + umbral
    alpha = (blur(alpha, 1.0) > 0.45).astype(np.float32)
    # G — las vetas: otro fBm tronchado a 3 bandas {0.12, 0.71, 0.9}
    vetas = fbm(w, h, 10, 3, rng)
    vetas = blur(vetas, 1.5)
    g = np.where(vetas < 0.38, 0.12, np.where(vetas < 0.62, 0.71, 0.90))
    # R — la máscara blanda (metaballs): blur fuerte del ruido
    r = blur(fbm(w, h, 4, 2, rng), 6.0)
    # B — blobs orgánicos blandos más grandes
    b = blur(fbm(w, h, 5, 2, rng), 4.0) * 0.85
    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    rgba[..., 0] = (r * 255).astype(np.uint8)
    rgba[..., 1] = (g * 255).astype(np.uint8)
    rgba[..., 2] = (b * 255).astype(np.uint8)
    rgba[..., 3] = (alpha * 255).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(f"{INV}/TintaViva.png")

def gen_mapa_color_tinta():
    """El LUT de 2 bandas del cordón (ColorMap_0 propio, colores casa):
    fila CLARA (Y≈0.25): negro #07030D → morado #4B1E7A → lila #C9A6FF;
    fila OSCURA (Y≈0.75): la misma rampa comprimida hacia el negro."""
    w, h = 64, 12
    clara = [(7, 3, 13), (23, 10, 38), (43, 18, 66), (75, 30, 122),
             (117, 52, 173), (168, 105, 224), (201, 166, 255)]
    oscura = [(7, 3, 13), (11, 5, 20), (17, 8, 30), (26, 12, 44),
              (38, 17, 62), (52, 24, 82), (66, 32, 100)]
    img = np.zeros((h, w, 3), dtype=np.uint8)
    for fila, rampa in ((5, clara), (11, oscura)):     # mitad sup / inf
        xs = np.linspace(0, len(rampa) - 1, w)
        for x in range(w):
            t = xs[x]; i = int(t); f = t - i
            i = min(i, len(rampa) - 2)
            c = tuple(int(rampa[i][k] + (rampa[i + 1][k] - rampa[i][k]) * f)
                      for k in range(3))
            img[max(0, fila - 5):fila, x] = c
    Image.fromarray(img, "RGB").save(f"{INV}/MapaColorTinta.png")

# =====================================================================
#  4. RAYO RAMO — EL RELÁMPAGO FINO (Lightning del jefe propio):
#     fBm umbral percentil ~92 → filamentos, ventana gaussiana vertical
#     σ≈0.12 centrada V=0.5, bordes a 0, tileable U y V (74-b).
# =====================================================================

def _rayo_base(w, h, estirar_x, cobertura, blur_n):
    r = fbm(w, h, 8, 4, rng)
    if estirar_x:                       # estirado horizontal (monzón)
        ancho = blur(r, 3.0)
        r = 0.35 * r + 0.65 * ancho
    r = blur(r, blur_n)
    thr = np.quantile(r, 1.0 - cobertura)
    fil = _smoothstep((r - thr) / 0.06) # filamentos duros
    vs = (np.arange(h, dtype=np.float32) + 0.5) / h
    ventana = np.exp(-((vs - 0.5) / 0.13) ** 2)
    fil = fil * ventana[:, None]
    # forzar bordes V a 0 (tile vertical limpio)
    fil[0:3, :] = 0; fil[-3:, :] = 0
    return fil

def gen_rayo_ramo():
    w = h = 256
    fino = _rayo_base(w, h, estirar_x=False, cobertura=0.078, blur_n=1.0)
    to_gray_png(np.clip(fino, 0, 1), f"{INV}/RayoRamo.png")
    glow = blur(np.clip(fino * 1.6, 0, 1), 9.0)
    to_gray_png(np.clip(glow, 0, 1), f"{INV}/RayoRamoBrillo.png")

# =====================================================================
#  5. NUBE RAYO — EL RELÁMPAGO NUBLADO DEL MONZÓN (más grueso y
#     horizontal, umbral bajo ~11 %, glow tenue techo 0.83 — 74-b).
# =====================================================================

def gen_nube_rayo():
    w = h = 256
    grueso = _rayo_base(w, h, estirar_x=True, cobertura=0.113, blur_n=2.0)
    to_gray_png(np.clip(grueso, 0, 1), f"{INV}/NubeRayo.png")
    glow = blur(np.clip(grueso * 1.2, 0, 1), 14.0) * 0.83
    to_gray_png(np.clip(glow, 0, 1), f"{INV}/NubeRayoBrillo.png")

# =====================================================================
#  6. LODO OSCURO — EL CHARCO BIOLUMINISCENTE (DarkSludge propio,
#     tinte MORADO en vez de azul — la receta 74-b):
#     base = value-noise 2 octavas cuantizado a 6 niveles en [8,23];
#     brillo = ~400 puntos gaussianos violetas sobre negro.
# =====================================================================

def gen_lodo_oscuro():
    w = h = 128
    r = fbm(w, h, 4, 2, rng)
    niveles = [9, 12, 14, 16, 19, 23]
    idx = np.clip((r * 6).astype(np.int32), 0, 5)
    gris = np.array(niveles, dtype=np.float32)[idx]
    # el tinte frío-MORADO de la casa: B > R > G
    img = np.zeros((h, w, 3), dtype=np.uint8)
    img[..., 0] = (gris * 1.00).astype(np.uint8)   # R
    img[..., 1] = (gris * 0.86).astype(np.uint8)   # G
    img[..., 2] = (gris * 1.18).astype(np.uint8)   # B
    Image.fromarray(img, "RGB").save(f"{INV}/LodoOscuro.png")

def gen_lodo_oscuro_brillo():
    w = h = 128
    lienzo = np.zeros((h, w), dtype=np.float32)
    n = 400
    for _ in range(n):
        cx, cy = rng.random() * w, rng.random() * h
        sigma = rng.uniform(1.2, 3.2)
        amp = rng.uniform(0.35, 1.0)
        xs = np.arange(w, dtype=np.float32)[None, :]
        ys = np.arange(h, dtype=np.float32)[:, None]
        # wrap para tilear (dibujo con espejo en los bordes)
        for dx in (-w, 0, w):
            for dy in (-h, 0, h):
                lienzo += amp * np.exp(-(((xs + dx - cx) ** 2 +
                                          (ys + dy - cy) ** 2) /
                                         (2 * sigma * sigma)))
    lienzo = np.clip(lienzo, 0, 1)
    img = np.zeros((h, w, 3), dtype=np.uint8)
    img[..., 0] = (lienzo * 58).astype(np.uint8)    # R violeta
    img[..., 1] = (lienzo * 22).astype(np.uint8)    # G tenue
    img[..., 2] = (lienzo * 110).astype(np.uint8)   # B violeta
    Image.fromarray(img, "RGB").save(f"{INV}/LodoOscuroBrillo.png")

# =====================================================================
#  7. LA CONSTELACIÓN — la viñeta del área (regenerada a 512²), la
#     estrella 4 puntas y la aguja-línea (recetas 74-a).
# =====================================================================

def gen_area_constelacion():
    w = h = 512
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    r = np.sqrt((xs - w / 2) ** 2 + (ys - h / 2) ** 2) / (w * 0.5)
    v = np.zeros_like(r)
    dentro = r < 0.96
    v[dentro] = 210.0 * np.power(np.clip((r[dentro] - 0.4) / 0.56, 0, 1), 2.0) / 255.0
    filo = (r >= 0.96) & (r < 0.98)
    v[filo] = 1.0
    to_gray_png(v, f"{INV}/AreaConstelacion.png")

def gen_estrella_constelacion():
    w = h = 34
    xs = np.arange(w, dtype=np.float32) - (w - 1) / 2
    gx = np.exp(-(xs / 2.2) ** 2)
    gy = np.exp(-(xs / 2.2) ** 2)
    cruz = np.minimum(1.0, gx[None, :] + gy[:, None])
    brillo = cruz ** 1.6
    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    rgba[..., 0:3] = 255
    rgba[..., 3] = (brillo * 255).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(f"{INV}/EstrellaConstelacion.png")

def gen_linea_constelacion():
    w = h = 144
    xs = np.arange(w, dtype=np.float32) - (w - 1) / 2
    ys = np.arange(h, dtype=np.float32) / (h - 1)      # 0 arriba → 1 abajo
    gx = np.exp(-(xs / 3.0) ** 2)
    rampa = _smoothstep(ys / 0.5)                       # crece hacia el centro (y=0.5·h)
    rampa = np.where(ys <= 0.5, rampa, _smoothstep((1.0 - ys) / 0.5))
    a = gx[None, :] * rampa[:, None]
    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    rgba[..., 0:3] = 235
    rgba[..., 3] = (a * 255).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(f"{INV}/LineaConstelacion.png")

# =====================================================================
#  8. EL ESPACIO — el starfield morado-negro (Space_0 propio, con
#     estrellas LILA además de blancas) y la nebulosa violeta (Space_1).
# =====================================================================

def gen_espacio_0():
    w = h = 1024
    fondo = np.zeros((h, w, 3), dtype=np.float32)
    fondo[..., 0] = 10; fondo[..., 1] = 6; fondo[..., 2] = 15   # morado-negro
    neb = fbm(w, h, 4, 4, rng)
    neb = np.clip((neb - 0.45) * 1.8, 0, 1) * 0.22
    fondo[..., 0] += neb * 66; fondo[..., 1] += neb * 26; fondo[..., 2] += neb * 104
    # las estrellas (con wrap en los bordes)
    n = 1500
    img = Image.fromarray(np.clip(fondo, 0, 255).astype(np.uint8), "RGB")
    from PIL import ImageDraw
    d = ImageDraw.Draw(img)
    for _ in range(n):
        x, y = rng.random() * w, rng.random() * h
        tam = 1 + (rng.random() < 0.18) + (rng.random() < 0.05)
        brillo = rng.uniform(150, 255)
        lila = rng.random() < 0.35
        col = (int(brillo * 0.85), int(brillo * 0.7), int(brillo)) if lila \
            else (int(brillo), int(brillo), int(brillo))
        for dx in (-w, 0, w):
            for dy in (-h, 0, h):
                xx, yy = x + dx, y + dy
                if -4 <= xx <= w + 4 and -4 <= yy <= h + 4:
                    d.ellipse([xx - tam / 2, yy - tam / 2,
                               xx + tam / 2, yy + tam / 2], fill=col)
    img.save(f"{INV}/Espacio_0.png")

def gen_espacio_1():
    w = h = 512
    n = fbm(w, h, 3, 4, rng)
    n2 = fbm(w, h, 6, 3, rng)
    mezcla = 0.7 * n + 0.3 * n2
    # la rampa VIOLETA de la casa: #0A0510 → #2B1243 → #6B2FA0 → #B87FE8
    rampa = [(10, 5, 16), (43, 18, 67), (107, 47, 160), (184, 127, 232)]
    t = np.clip(mezcla, 0, 1) * (len(rampa) - 1)
    i = np.clip(t.astype(np.int32), 0, len(rampa) - 2)
    f = (t - i)[..., None]
    arr = np.array(rampa, dtype=np.float32)
    img = arr[i] + (arr[i + 1] - arr[i]) * f
    Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB").save(
        f"{INV}/Espacio_1.png")

# =====================================================================
#  9. LOS RUIDOS — 20 texturas (17 como el original + 3 familias nuevas)
#     Ruido_0 = 1024², el resto 512². Gris, tileables (74-c).
# =====================================================================

def _save_ruido(arr, nombre):
    to_gray_png(np.clip(arr, 0, 1), f"{BASE}/{nombre}.png")

def gen_ruidos():
    # _0 (1024²) — fBm nubes contraste medio (la textura "principal")
    _save_ruido(np.clip(fbm(1024, 1024, 8, 4, rng) * 1.15, 0, 1), "Ruido_0")
    # _1 — fBm contraste alto
    _save_ruido(np.clip((fbm(512, 512, 6, 4, rng) - 0.5) * 1.9 + 0.5, 0, 1), "Ruido_1")
    # _2 — STARFIELD denso (estrellas RE-BRILLANTES: el blur apaga, se
    # re-pinchan los centros a 255 tras suavizar)
    star = np.zeros((512, 512), dtype=np.float32)
    pts2 = []
    for _ in range(700):
        x, y = int(rng.random() * 512), int(rng.random() * 512)
        pts2.append((x, y))
        star[y, x] = 1.0
        if rng.random() < 0.3:
            star[(y + 1) % 512, x] = 0.6; star[y, (x + 1) % 512] = 0.6
    s2 = blur(star, 0.5)
    for (x, y) in pts2:
        s2[y, x] = 1.0
    _save_ruido(s2, "Ruido_2")
    # _3 — STARFIELD tenue con blobs
    star2 = np.zeros((512, 512), dtype=np.float32)
    pts3 = []
    for _ in range(240):
        x, y = int(rng.random() * 512), int(rng.random() * 512)
        pts3.append((x, y))
        star2[y, x] = 0.9
    s3 = blur(star2, 0.5) + blur(fbm(512, 512, 5, 3, rng), 8) * 0.18
    for (x, y) in pts3:
        s3[y, x] = 0.95
    _save_ruido(s3, "Ruido_3")
    # _4 — WISPS oscuros (nubes dispersas sobre negro)
    w4 = fbm(512, 512, 4, 3, rng)
    w4 = _smoothstep((w4 - 0.52) / 0.3) * (0.25 + 0.75 * fbm(512, 512, 9, 3, rng))
    _save_ruido(w4, "Ruido_4")
    # _5 — TURBULENCIA granulada densa
    t5 = np.abs(fbm(512, 512, 12, 3, rng) - 0.5) * 2
    _save_ruido(1.0 - t5, "Ruido_5")
    # _6 — MOTAS dispersas
    m6 = blur((fbm(512, 512, 10, 2, rng) > np.quantile(fbm(512, 512, 10, 2, rng), 0.9)).astype(np.float32), 1.0)
    _save_ruido(m6, "Ruido_6")
    # _7 — fBm suave
    _save_ruido(blur(fbm(512, 512, 6, 4, rng), 1.2), "Ruido_7")
    # _8 — ONDAS DE SEDA (bandas onduladas de bajo contraste)
    xs = np.arange(512, dtype=np.float32)[None, :]
    ys = np.arange(512, dtype=np.float32)[:, None]
    ond = np.sin(xs * 0.10 + fbm(512, 512, 4, 3, rng) * 9.0)
    _save_ruido(0.5 + ond * 0.16, "Ruido_8")
    # _9 — VERMIFORME (venas entrelazadas — el look "llama")
    v9 = 1.0 - np.abs(fbm(512, 512, 7, 4, rng) - 0.5) * 2
    _save_ruido(np.power(np.clip(v9, 0, 1), 1.6), "Ruido_9")
    # _10 — fBm suave oscuro
    _save_ruido(blur(fbm(512, 512, 5, 3, rng), 2.0) * 0.75, "Ruido_10")
    # _11 — GRANO fino denso (casi blanco con blur)
    g11 = value_noise(512, 512, 128, 128, rng)
    _save_ruido(blur(g11, 0.5) * 0.95 + 0.05, "Ruido_11")
    # _12 — fBm suave (variante)
    _save_ruido(blur(fbm(512, 512, 8, 4, rng), 1.5), "Ruido_12")
    # _13 — fBm MUY suave (baja frecuencia — distorsión de haces)
    _save_ruido(blur(fbm(512, 512, 3, 3, rng), 3.5), "Ruido_13")
    # _14 — PUFFS brillantes dispersos
    p14 = np.zeros((512, 512), dtype=np.float32)
    for _ in range(90):
        cx, cy = rng.random() * 512, rng.random() * 512
        s = rng.uniform(4, 14)
        xs = np.arange(512, dtype=np.float32)[None, :]
        ys = np.arange(512, dtype=np.float32)[:, None]
        p14 += rng.uniform(0.5, 1.0) * np.exp(-(((xs - cx) ** 2 + (ys - cy) ** 2) / (2 * s * s)))
    _save_ruido(np.clip(p14, 0, 1), "Ruido_14")
    # _15 — RIDGED (crestas afiladas diagonales)
    r15 = 1.0 - np.abs(2 * fbm(512, 512, 6, 4, rng) - 1)
    _save_ruido(np.power(r15, 2.2), "Ruido_15")
    # _16 — fBm suave (variante 3)
    _save_ruido(blur(fbm(512, 512, 7, 3, rng), 1.0), "Ruido_16")
    # ===== LAS 3 FAMILIAS NUEVAS DE LA CASA =====
    # _17 — CÉLULAS (Voronoi F2−F1: las membranas)
    pts = [(rng.random() * 512, rng.random() * 512) for _ in range(48)]
    ys2, xs2 = np.mgrid[0:512, 0:512].astype(np.float32)
    d1 = np.full((512, 512), 1e9, dtype=np.float32)
    d2 = np.full((512, 512), 1e9, dtype=np.float32)
    for (px, py) in pts:
        dx = np.abs(xs2 - px); dx = np.minimum(dx, 512 - dx)
        dy = np.abs(ys2 - py); dy = np.minimum(dy, 512 - dy)
        dd = np.sqrt(dx * dx + dy * dy)
        m = dd < d1
        d2 = np.where(m, d1, np.minimum(d2, dd))
        d1 = np.where(m, dd, d1)
    _save_ruido(np.clip(1.0 - (d2 - d1) / 34.0, 0, 1), "Ruido_17")
    # _18 — PLASMA sinusoidal (las 3 ondas clásicas cruzadas)
    xs3 = np.arange(512, dtype=np.float32)[None, :] / 512 * 2 * np.pi
    ys3 = np.arange(512, dtype=np.float32)[:, None] / 512 * 2 * np.pi
    pl = (np.sin(xs3 * 3) + np.sin(ys3 * 4) + np.sin((xs3 + ys3) * 2)) / 3
    _save_ruido(0.5 + pl * 0.42, "Ruido_18")
    # _19 — ANILLOS concéntricos orgánicos (ondas de agua)
    rr = np.sqrt((xs2 - 256) ** 2 + (ys2 - 256) ** 2)
    wrap = np.minimum(rr, np.sqrt((512 - xs2 + 0) ** 2 + (512 - ys2) ** 2))
    rings = np.sin(rr * 0.16 + fbm(512, 512, 4, 3, rng) * 7.0)
    _save_ruido(0.5 + rings * 0.35, "Ruido_19")

# =====================================================================
#  10. LOS ICONOS DE LOS ÍTEMS (30×30, morados de la casa)
# =====================================================================

ICONOS = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content"

def _icono(nombre, carpeta, dibujar):
    w = h = 30
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    dibujar(d, w, h)
    ruta = os.path.join(ICONOS, carpeta, f"{nombre}.png")
    os.makedirs(os.path.dirname(ruta), exist_ok=True)
    img.save(ruta)
    return ruta

def _plumas_icono(d, w, h, colores, largo=13):
    """El abanico de 5 plumas (la firma de las alas de la casa)."""
    cx, cy = w * 0.5, h * 0.62
    import math
    for i in range(5):
        t = i / 4
        ang = -math.pi / 2 + (t - 0.5) * 2.1
        L = largo * (1.0 - 0.35 * t)
        x2 = cx + math.cos(ang) * L * 1.35
        y2 = cy + math.sin(ang) * L
        col = colores[i % len(colores)]
        for k, grosor in enumerate((4, 2)):
            d.line([cx, cy, x2, y2], fill=col, width=grosor)
            x2b = cx + (x2 - cx) * 0.97
            y2b = cy + (y2 - cy) * 0.97
            x2, y2 = x2b, y2b
        # núcleo claro
        d.line([cx, cy, cx + math.cos(ang) * L * 1.35, cy + math.sin(ang) * L],
               fill=(255, 255, 255, 200), width=1)

def gen_iconos():
    import math
    morado = (140, 60, 210, 255); lila = (200, 160, 255, 255)
    oscuro = (40, 12, 66, 255); blanco = (245, 240, 255, 255)

    # === LAS 6 ARMAS/COSMÉTICOS DE LA INVESTIGACIÓN ===
    # Rayos Concentrados — el haz grueso diagonal con núcleo blanco
    def f_rayos(d, w, h):
        for k, (grosor, col) in enumerate([(13, (90, 30, 150, 130)),
                                           (8, (140, 60, 210, 200)),
                                           (3, blanco)]):
            d.line([4, 24, 26, 6], fill=col, width=grosor)
        d.ellipse([10, 8, 18, 16], fill=blanco)
        d.ellipse([2, 21, 9, 28], fill=lila)
    _icono("RayosConcentrados", "Weapons/Investigacion", f_rayos)

    # Trueno del Prisma — el zigzag de 3 colores
    def f_trueno(d, w, h):
        pts = [(17, 2), (8, 13), (14, 14), (9, 27), (22, 11), (15, 11), (20, 2)]
        for i in range(len(pts) - 1):
            col = [morado, lila, blanco, lila, morado, lila][i]
            d.line([pts[i], pts[i + 1]], fill=col, width=3)
    _icono("TruenoDelPrisma", "Weapons/Investigacion", f_trueno)

    # Lodo de Esencia — el charco con puntos brillantes
    def f_lodo(d, w, h):
        d.polygon([(3, 20), (8, 15), (16, 13), (24, 15), (28, 20),
                   (24, 24), (8, 24)], fill=oscuro, outline=morado)
        for (x, y) in [(8, 19), (13, 17), (19, 20), (23, 18), (11, 22), (17, 15)]:
            d.ellipse([x - 1, y - 1, x + 1, y + 1], fill=lila)
        d.polygon([(15, 3), (19, 10), (15, 13), (11, 10)], fill=morado)
    _icono("LodoDeEsencia", "Weapons/Investigacion", f_lodo)

    # Cetro de Constelaciones — la estrella + las líneas
    def f_cetro(d, w, h):
        d.line([(6, 24), (14, 16), (22, 22)], fill=(120, 70, 180, 200), width=1)
        d.line([(14, 16), (24, 9)], fill=(120, 70, 180, 200), width=1)
        d.line([(14, 16), (5, 12)], fill=(120, 70, 180, 200), width=1)
        for (x, y) in [(6, 24), (24, 9), (5, 12), (22, 22)]:
            d.ellipse([x - 2, y - 2, x + 2, y + 2], fill=lila)
        # la estrella grande
        cx, cy = 14, 16
        d.line([(cx - 8, cy), (cx + 8, cy)], fill=blanco, width=2)
        d.line([(cx, cy - 8), (cx, cy + 8)], fill=blanco, width=2)
        d.ellipse([cx - 3, cy - 3, cx + 3, cy + 3], fill=blanco)
    _icono("CetroConstelaciones", "Weapons/Investigacion", f_cetro)

    # Estela del Cosmos — el anillo elíptico morado
    def f_estela(d, w, h):
        d.ellipse([3, 10, 27, 22], outline=morado, width=3)
        d.ellipse([6, 12, 24, 20], outline=lila, width=1)
        d.ellipse([12, 12, 18, 18], fill=blanco)
    _icono("EstelaDelCosmos", "Items/Cosmetics/Investigacion", f_estela)

    # Tinta Viva — la gota alargada con vetas
    def f_tinta(d, w, h):
        d.polygon([(15, 2), (20, 12), (23, 22), (15, 28), (7, 22), (10, 12)],
                  fill=oscuro, outline=morado)
        d.line([(13, 6), (13, 24)], fill=lila, width=2)
        d.line([(17, 10), (18, 22)], fill=(120, 70, 180, 255), width=1)
        d.ellipse([12, 8, 16, 12], fill=blanco)
    _icono("TintaViva", "Items/Cosmetics/Investigacion", f_tinta)

    # === LAS 15 ALAS (el abanico con la paleta de cada una) ===
    alas = [
        ("AlasDeTintaSerena", [(201, 166, 255), (140, 60, 210), (75, 30, 122)]),
        ("AlasDeEclipse", [(157, 92, 232), (75, 18, 119), (20, 6, 31)]),
        ("AlasDePlasma", [(255, 61, 219), (199, 36, 184), (122, 47, 209)]),
        ("AlasDeNebulosa", [(184, 127, 232), (209, 92, 255), (110, 123, 232)]),
        ("AlasDeCometario", [(255, 243, 214), (125, 232, 255), (92, 59, 209)]),
        ("AlasDeMedianoche", [(245, 240, 255), (48, 26, 96), (14, 8, 30)]),
        ("AlasDelPrisma", [(174, 23, 189), (247, 255, 101), (102, 219, 249)]),
        ("AlasDeFuegoFrio", [(234, 246, 255), (185, 166, 255), (125, 209, 232)]),
        ("AlasDeTormenta", [(240, 230, 255), (178, 76, 232), (61, 26, 92)]),
        ("AlasDeMarioneta", [(227, 92, 133), (138, 26, 61), (26, 10, 20)]),
        ("AlasDeAuroraArtica", [(125, 232, 154), (125, 232, 224), (185, 166, 255)]),
        ("AlasDeOrquidea", [(255, 138, 209), (232, 79, 199), (166, 61, 232)]),
        ("AlasDelVacio", [(107, 47, 160), (40, 14, 66), (8, 3, 13)]),
        ("AlasSolaresRunicas", [(255, 246, 220), (232, 166, 61), (255, 232, 154)]),
        ("AlasDeSangreDeTinta", [(209, 47, 166), (122, 26, 77), (20, 6, 20)]),
    ]
    for nombre, paleta in alas:
        cols = [(r, g, b, 255) for (r, g, b) in paleta]
        def f_ala(d, w, h, cols=cols):
            _plumas_icono(d, w, h, cols)
        _icono(nombre, "Items/Alas", f_ala)

# =====================================================================
#  EL MAIN
# =====================================================================

if __name__ == "__main__":
    pasos = [
        ("RayoConcentrado_0 (cuerpo fBm)", gen_rayo_concentrado_0),
        ("RayoConcentrado_1 (bits células)", gen_rayo_concentrado_1),
        ("RayoConcentrado_2 (glow 1D)", gen_rayo_concentrado_2),
        ("RayoConcentrado_3 (núcleo+falda)", gen_rayo_concentrado_3),
        ("EstelaEspacial (banda máscara)", gen_estela_espacial),
        ("EstelaEspacialBrillo (barra 1D)", gen_estela_espacial_brillo),
        ("TintaViva (cordón líquido)", gen_tinta_viva),
        ("MapaColorTinta (LUT 2 bandas)", gen_mapa_color_tinta),
        ("RayoRamo + Brillo (finos)", gen_rayo_ramo),
        ("NubeRayo + Brillo (monzón)", gen_nube_rayo),
        ("LodoOscuro (base)", gen_lodo_oscuro),
        ("LodoOscuroBrillo (biolumín)", gen_lodo_oscuro_brillo),
        ("AreaConstelacion (viñeta 512)", gen_area_constelacion),
        ("EstrellaConstelacion", gen_estrella_constelacion),
        ("LineaConstelacion (aguja)", gen_linea_constelacion),
        ("Espacio_0 (starfield morado)", gen_espacio_0),
        ("Espacio_1 (nebulosa violeta)", gen_espacio_1),
        ("Ruido_0..19 (20 familias)", gen_ruidos),
        ("Iconos de items y alas", gen_iconos),
    ]
    for titulo, fn in pasos:
        fn()
        print(f"  ✓ {titulo}")
    print("LISTO — texturas de la investigación generadas.")
