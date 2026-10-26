#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
GEN_TEXTURAS_PRO_V6543 — LA REHABILITACIÓN VISUAL DE LA INVESTIGACIÓN.

La letra del usuario (27/10): «el arma de rayos concentrado se ve
horrible... son todas una raya plana y no debe ser así... acaso no te
fijaste en como eran las texturas usadas en [el dios], no eran una
línea recta, eran más elaboradas que eso».

LA AUTOPSIA: las texturas de la v6.54.0 eran DEGRADADOS PLANOS
(RayoConcentrado_0: 256 colores = una rampa; _2 y _3: rectángulos).
Las REFERENCIAS (estudiadas del respaldo privado de investigación):
  _0 = fBm ANISOTRÓPICO × perfil vertical de campana (3281 colores)
  _1 = islas de chispas ESTIRADAS horizontal, alpha suave (aniso 0.26)
  _2 = perfil 1D gaussiano casi puro (aniso 0.12, [0,18,123,123,18])
  _3 = núcleo alto + falda ancha con piso de ruido (perfil [2..254..2])
  LiquidTrail = metaballs con RGB como MAPA DE DATOS
  SpaceTrail = bandas de flujo horizontal (aniso 0.27) RGB-dato
  DarkSludge = fBm multi-octava con REMOLINOS (domain warping)
  Lightning = núcleo continuo + ramas laterales finas

ESTE GENERADOR reconstruye TODAS las texturas de efecto de la
investigación con esas técnicas de verdad:
  · Ruido de gradiente TILEABLE (retícula que envuelve) + fBm
  · ANISOTROPÍA por número de celdas distinto en U y V
  · DOMAIN WARPING (remolinos) con campos que también envuelven
  · METABALLS TOROIDALES (distancia con wrap) para la tinta viva
  · PERFILES VERTICALES calibrados de las medidas de las referencias
  · Ramas de relámpago Bresenham serpenteantes con núcleo continuo
    (la fila central V=0.5 es la que muestrea TruenoPrismaRayo —
    la lección de la línea punteada)
  · Estrellas con brillo gaussiano + nebulosas fBm para el espacio

Todas TILEABLES en U y V (el sampler M() del FajaCPU envuelve).
"""

import math
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

# =====================================================================
#  LA SEMILLA Y LAS RUTAS
# =====================================================================

SEMILLA = 6543
RAIZ = os.path.join(os.path.dirname(__file__), '..')
CARPETA = os.path.join(RAIZ, 'AethonMod', 'Content', 'Effects',
                       'Procedural', 'Investigacion')


# =====================================================================
#  EL NÚCLEO — RUIDO DE GRADIENTE TILEABLE + fBm + HERRAMIENTAS
# =====================================================================

def ruido_gradiente(w, h, cx, cy, rng):
    """Ruido de gradiente (Perlin) con la retícula cx×cy ENVUELTA:
    tileable de verdad en ambos ejes. Devuelve ~[-1, 1]."""
    cx = max(2, int(cx))
    cy = max(2, int(cy))
    ang = rng.uniform(0.0, 2.0 * np.pi, (cy, cx))
    gx, gy = np.cos(ang), np.sin(ang)

    fx = np.arange(w) * (cx / w)          # 0 .. cx (sin el último)
    fy = np.arange(h) * (cy / h)
    x0 = np.floor(fx).astype(int) % cx
    y0 = np.floor(fy).astype(int) % cy
    x1 = (x0 + 1) % cx
    y1 = (y0 + 1) % cy
    tx = fx - np.floor(fx)
    ty = fy - np.floor(fy)
    # el fade de Perlin (quintic)
    sx = tx * tx * tx * (tx * (tx * 6 - 15) + 10)
    sy = ty * ty * ty * (ty * (ty * 6 - 15) + 10)

    filas0, filas1 = np.ix_(y0, x0), None
    # productos punto por esquina (índices envueltos)
    n00 = gx[np.ix_(y0, x0)] * tx[None, :] + gy[np.ix_(y0, x0)] * ty[:, None]
    n01 = gx[np.ix_(y0, x1)] * (tx - 1)[None, :] + gy[np.ix_(y0, x1)] * ty[:, None]
    n10 = gx[np.ix_(y1, x0)] * tx[None, :] + gy[np.ix_(y1, x0)] * (ty - 1)[:, None]
    n11 = gx[np.ix_(y1, x1)] * (tx - 1)[None, :] + gy[np.ix_(y1, x1)] * (ty - 1)[:, None]

    arriba = n00 + (n01 - n00) * sx[None, :]
    abajo = n10 + (n11 - n10) * sx[None, :]
    n = arriba + (abajo - arriba) * sy[:, None]
    return np.clip(n * 1.45, -1.0, 1.0)


def fbm(w, h, celdas_u, celdas_v, octavas, rng, persist=0.5, lac=2.0):
    """fBm tileable: octavas de ruido de gradiente con celdas ×2 cada
    una (todas envuelven — la suma también envuelve). NORMALIZADO a
    [-1, 1] pleno (el Perlin crudo rara vez pasa de ±0.7 — sin esta
    normalización todas las texturas quedan al 60 % de contraste)."""
    total = np.zeros((h, w), dtype=np.float64)
    amp, norm = 1.0, 0.0
    cu, cv = float(celdas_u), float(celdas_v)
    for _ in range(octavas):
        total += ruido_gradiente(w, h, cu, cv, rng) * amp
        norm += amp
        amp *= persist
        cu *= lac
        cv *= lac
    total /= norm
    pico = np.abs(total).max()
    if pico > 1e-6:
        total = total / pico          # el rango PLENO — el contraste de verdad
    return total


def fbm_aniso(w, h, celdas_u, celdas_v, octavas, rng, **kw):
    """fBm con más celdas en V que en U → estructuras ESTIRADAS a lo
    largo de U (el flujo horizontal de las referencias)."""
    return fbm(w, h, celdas_u, celdas_v, octavas, rng, **kw)


def warp(fuente, campo_u, campo_v, fuerza):
    """DOMAIN WARPING tileable: muestrea `fuente` en coordenadas
    desplazadas por los campos (ambos envuelven → el resultado envuelve)."""
    h, w = fuente.shape
    ys, xs = np.meshgrid(np.arange(h) / h, np.arange(w) / w, indexing='ij')
    u = (xs + campo_u * fuerza) % 1.0
    v = (ys + campo_v * fuerza) % 1.0
    return bilineal_envolvente(fuente, u, v)


def bilineal_envolvente(arr, u, v):
    """Muestreo bilineal con wrap en ambos ejes (el sampler M de la casa)."""
    h, w = arr.shape
    x = u * w - 0.5
    y = v * h - 0.5
    x0 = np.floor(x).astype(int)
    y0 = np.floor(y).astype(int)
    fx = x - x0
    fy = y - y0
    x0 %= w
    y0 %= h
    x1 = (x0 + 1) % w
    y1 = (y0 + 1) % h
    a = arr[y0, x0]
    b = arr[y0, x1]
    c = arr[y1, x0]
    d = arr[y1, x1]
    return (a + (b - a) * fx) * (1 - fy) + (c + (d - c) * fx) * fy


def a_u8(x):
    """float 0..1 → byte con clamping."""
    return (np.clip(x, 0.0, 1.0) * 255.0 + 0.5).astype(np.uint8)


def guardar(arr_o_img, nombre):
    """Guarda (y verifica) una textura de la investigación."""
    ruta = os.path.join(CARPETA, nombre + '.png')
    img = arr_o_img if isinstance(arr_o_img, Image.Image) else Image.fromarray(arr_o_img)
    img.save(ruta)
    n_col = len(set(img.convert('RGBA').getdata()))
    print(f'  {nombre:24s} {img.size!s:12s} colores={n_col:5d}')
    return ruta


def gris(x, alpha=1.0):
    """Mapa float → RGBA en escala de grises (el contrato de las capas)."""
    h, w = x.shape[:2]
    g = a_u8(x)
    a = a_u8(np.full_like(g, alpha, dtype=np.float64) if not isinstance(alpha, np.ndarray) else alpha)
    return np.dstack([g, g, g, a])


def perfil_campana(h, ancho, piso=0.0):
    """El perfil vertical de campana de las referencias (gaussiana con
    hombros suaves), muestreado en cada fila."""
    v = (np.arange(h) + 0.5) / h - 0.5          # -0.5 .. 0.5
    return piso + (1.0 - piso) * np.exp(-((v / ancho) ** 2) * 2.2)


# =====================================================================
#  1. EL RAYO CONCENTRADO — LAS CUATRO CAPAS DEL ATAQUE DEFINITIVO
# =====================================================================

def gen_rayo_concentrado(rng):
    print('EL RAYO CONCENTRADO — las 4 capas (la referencia medida):')

    # --- _0: EL CUERPO DE PLASMA RUIDOSO (perfil [0..238..0] + fBm 2.4:1)
    w, h = 256, 256
    cuerpo = fbm_aniso(w, h, 7, 17, 5, rng, persist=0.55)      # vetas 2.4:1 a lo largo de U
    remolino_u = fbm(w, h, 3, 3, 3, rng) * 0.5
    remolino_v = fbm(w, h, 3, 3, 3, rng) * 0.5
    cuerpo = warp(cuerpo, remolino_u, remolino_v, 0.10)        # el plasma REMOLINA
    cuerpo = (cuerpo + 1.0) * 0.5                              # 0..1
    # EL REALCE de alto contraste (las vetas vivas del original)
    cuerpo = np.clip(cuerpo ** 1.6 * 1.35, 0.0, 1.0)
    perfil = perfil_campana(h, ancho=0.30)                     # campana ancha
    fino = (fbm(w, h, 40, 40, 2, rng) + 1) * 0.5               # grano fino
    cap0 = cuerpo * perfil[:, None] * (0.72 + 0.28 * fino)
    cap0 = cap0 / max(cap0.max(), 1e-6) * 0.97
    guardar(gris(cap0), 'RayoConcentrado_0')

    # --- _1: LAS ISLAS DE CHISPAS (estiradas 4:1, alpha suave y esparcido)
    w1, h1 = 512, 512
    islas = chispas_islas(w1, h1, rng, n_islas=42, sx=34.0, sy=9.0)
    islas2 = chispas_islas(w1, h1, rng, n_islas=64, sx=13.0, sy=6.0)
    # EL NÚCLEO DURO de cada isla (las chispas titilan con centro blanco)
    islas3 = chispas_islas(w1, h1, rng, n_islas=26, sx=9.0, sy=4.0)
    alpha = np.clip(islas * 0.75 + islas2 * 0.55 + islas3 * 0.9, 0.0, 1.0)
    alpha = alpha ** 1.15                                      # huecos de verdad entre islas
    alpha = alpha / max(alpha.max(), 1e-6)
    guardar(gris(np.zeros((h1, w1)), alpha), 'RayoConcentrado_1')

    # --- _2: EL PERFIL 1D DE BRILLO (gaussiana + variación perceptible —
    #     la referencia era suave pero no MUERTA: el 12 % de vida en U)
    w2, h2 = 256, 256
    perfil2 = perfil_campana(h2, ancho=0.16, piso=0.0)          # gaussiana comprimida
    var_g = (fbm(w2, h2, 4, 6, 2, rng) + 1) * 0.5
    var_f = (fbm(w2, h2, 14, 20, 2, rng) + 1) * 0.5
    var_u = 0.86 + 0.14 * (0.6 * var_g + 0.4 * var_f)          # respira a lo largo
    cap2 = perfil2[:, None] * var_u * 0.80
    guardar(gris(cap2), 'RayoConcentrado_2')

    # --- _3: EL NÚCLEO + LA FALDA (perfil [2, ..., 254, ..., 2] + ruido
    #     caótico en la falda — las chispas que escapan del núcleo)
    w3, h3 = 256, 256
    falda = perfil_campana(h3, ancho=0.30, piso=0.0) * 0.40
    nucleo = perfil_campana(h3, ancho=0.085, piso=0.0) * 0.62
    cuerpo3 = fbm_aniso(w3, h3, 5, 13, 4, rng, persist=0.55)
    cuerpo3 = (cuerpo3 + 1) * 0.5
    # EL CAOS DE LA FALDA: vetas que se desprenden (el ruido pesa MÁS
    # lejos del centro — la falda hierve, el núcleo es denso y limpio)
    caos = fbm_aniso(w3, h3, 8, 22, 4, rng, persist=0.6)
    caos = np.clip((caos + 1) * 0.5, 0, 1) ** 1.8
    peso_caos = np.abs((np.arange(h3) + 0.5) / h3 - 0.5) * 2.0   # 0 centro → 1 bordes
    cap3 = 0.008 + falda + nucleo
    cap3 = cap3 * (0.55 + 0.45 * cuerpo3)
    cap3 = cap3 + caos * peso_caos[:, None] * perfil_campana(h3, 0.34, 0.0)[:, None] * 0.16
    cap3 = cap3 / max(cap3.max(), 1e-6)
    guardar(gris(cap3), 'RayoConcentrado_3')


def chispas_islas(w, h, rng, n_islas, sx, sy):
    """Metaballs toroidales ESTIRADOS: islas de chispa con caída suave
    (la referencia _1: aniso 0.26 — 4× más largas que altas)."""
    campo = np.zeros((h, w), dtype=np.float64)
    xs = np.arange(w) / w
    ys = np.arange(h) / h
    X, Y = np.meshgrid(xs, ys)
    for _ in range(n_islas):
        cx, cy = rng.uniform(0, 1), rng.uniform(0.42, 0.58)     # ISLAS EN LA BANDA CENTRAL
        amp = rng.uniform(0.55, 1.0)
        # distancia toroidal (wrap en ambos ejes)
        dx = np.minimum(np.abs(X - cx), 1 - np.abs(X - cx)) / (sx / w)
        dy = np.minimum(np.abs(Y - cy), 1 - np.abs(Y - cy)) / (sy / h)
        campo += amp * np.exp(-(dx * dx + dy * dy) * 0.5)
    return np.clip(campo - 0.18, 0.0, None) / max(campo.max() - 0.18, 1e-6)


# =====================================================================
#  2. EL TRUENO DEL PRISMA — LOS FILAMENTOS RAMIFICADOS
#     (LA LECCIÓN: TruenoPrismaRayo muestrea SOLO la fila V=0.5 —
#      la fila central debe ser un NÚCLEO CONTINUO ondulante, no
#      puntos dispersos; las ramas viven a los lados)
# =====================================================================

def gen_rayo_ramo(rng):
    print('EL TRUENO DEL PRISMA — filamentos con núcleo continuo:')

    w, h = 256, 256
    nuc = np.zeros((h, w), dtype=np.float64)
    fil = np.zeros((h, w), dtype=np.float64)

    # EL NÚCLEO CONTINUO: banda central brillante con modulación fractal
    u = np.arange(w) / w
    # la intensidad del núcleo varía como un rayo de verdad (grueso/finas)
    grosor = 0.055 + 0.028 * (0.5 + 0.5 * np.sin(u * 2 * np.pi * 5.0)
                               + 0.35 * np.sin(u * 2 * np.pi * 13.0 + 1.7)) / 1.35
    brillo = 0.82 + 0.18 * (0.5 + 0.5 * np.sin(u * 2 * np.pi * 9.0 + 0.6)
                             + 0.4 * np.sin(u * 2 * np.pi * 23.0)) / 1.4
    v = (np.arange(h) + 0.5) / h - 0.5
    for i in range(w):
        d = np.abs(v - 0.0)
        g = grosor[i]
        nuc[:, i] = np.exp(-(d / g) ** 2 * 2.0) * brillo[i]

    # LAS RAMAS: polilíneas serpenteantes que nacen del núcleo y mueren
    # hacia los bordes (Bresenham suave con broche gaussiano)
    img_fil = Image.new('L', (w, h), 0)
    dr = ImageDraw.Draw(img_fil)
    for rama in range(26):
        lado = 1 if rama % 2 == 0 else -1
        x = rng.uniform(0, w)
        y = h * 0.5
        pasos = rng.integers(14, 30)
        pend = lado * rng.uniform(0.35, 1.1)
        pts = [(x, y)]
        for s in range(pasos):
            x += rng.uniform(4, 11)
            y += pend * rng.uniform(2, 7) + rng.uniform(-2.5, 2.5)
            if y < h * 0.06 or y > h * 0.94:
                break
            pts.append((x % w, y))          # wrap U: la rama que sale vuelve por la izquierda
        # el broche: grosor que nace gordo y muere fino
        for s in range(len(pts) - 1):
            t = s / max(1, len(pts) - 2)
            ancho_px = max(0, round(2.6 * (1 - t) - 0.4))
            if ancho_px <= 0:
                break
            brillo_pt = int(255 * (0.85 - 0.55 * t))
            dr.line([pts[s], pts[s + 1]], fill=brillo_pt, width=ancho_px)
    fil = np.asarray(img_fil, dtype=np.float64) / 255.0

    # LA COMPOSICIÓN: núcleo × (0.9) + ramas × 0.75 — y un grano fino
    grano = (fbm(w, h, 30, 30, 2, rng) + 1) * 0.5
    total = np.clip(nuc * (0.82 + 0.18 * grano) + fil * 0.72 * (0.65 + 0.35 * grano), 0, 1)
    guardar(gris(total), 'RayoRamo')

    # EL BRILLO: núcleo difuminado + LAS RAMAS AÚN LEGIBLES a través
    # del halo (el neón del original: el zigzag se ACUSA en el glow)
    nucleo_glow = Image.fromarray(a_u8(nuc)).filter(ImageFilter.GaussianBlur(9))
    ramas_glow = Image.fromarray(a_u8(fil)).filter(ImageFilter.GaussianBlur(5))
    g_n = np.asarray(nucleo_glow, dtype=np.float64) / 255.0
    g_r = np.asarray(ramas_glow, dtype=np.float64) / 255.0
    glow = np.clip(g_n * 0.85 + g_r * 0.55, 0, 1)
    guardar(gris(glow * 1.02), 'RayoRamoBrillo')


# =====================================================================
#  3. LA ESTELA ESPACIAL — RGB COMO DATOS (el anillo del cosmos)
#     (muestreada a filas V fijas: A=0.35, G=0.18, R=0.38, B=0.07 —
#      cada canal su propia máscara de flujo horizontal)
# =====================================================================

def gen_estela_espacial(rng):
    print('LA ESTELA ESPACIAL — canales-dato con flujo horizontal:')

    # LA BANDA (128×128, RGBA): A = la interferencia (vetas 3:1),
    # G = la línea caliente fina, R = el velo violeta, B = el borde azul.
    w, h = 128, 128
    venas = fbm_aniso(w, h, 5, 14, 4, rng, persist=0.55)
    venas = warp(venas, fbm(w, h, 3, 3, 2, rng), fbm(w, h, 3, 3, 2, rng), 0.14)
    venas = np.clip(((venas + 1) * 0.5) ** 1.45 * 1.5, 0, 1)

    caliente = fbm_aniso(w, h, 6, 22, 3, rng)                     # filamentos 3.7:1
    caliente = np.clip(((caliente + 1) * 0.5) ** 2.6 * 2.6, 0, 1)

    velo = fbm_aniso(w, h, 4, 9, 4, rng)
    velo = np.clip(((velo + 1) * 0.5) ** 1.2, 0, 1)

    borde = fbm_aniso(w, h, 5, 11, 3, rng)
    borde = np.clip(((borde + 1) * 0.5) ** 2.0, 0, 1)

    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    rgba[..., 0] = a_u8(velo * 0.9)          # R = el violeta
    rgba[..., 1] = a_u8(caliente * 0.95)     # G = la línea lila caliente
    rgba[..., 2] = a_u8(borde * 0.85)        # B = el borde azul-violeta
    rgba[..., 3] = a_u8(venas * 0.92)        # A = la interferencia
    guardar(rgba, 'EstelaEspacial')

    # LA BARRA GLOW (256×256): la línea comprimida (aniso 0.12 de la ref)
    w2, h2 = 256, 256
    perfil = perfil_campana(h2, ancho=0.085, piso=0.0)
    var = 0.92 + 0.08 * ((fbm(w2, h2, 3, 14, 2, rng) + 1) * 0.5)
    guardar(gris(perfil[:, None] * var * 0.8), 'EstelaEspacialBrillo')


# =====================================================================
#  4. EL LODO DE ESENCIA — EL CHARCO OSCURO CON REMOLINOS
# =====================================================================

def gen_lodo_oscuro(rng):
    print('EL LODO DE ESENCIA — remolinos bioluminiscentes:')

    # LA BASE (128×128): fBm 5 octavas con DOMAIN WARPING (los
    # remolinos del original) cuantizado a 7 niveles oscuros.
    w, h = 128, 128
    base = fbm(w, h, 4, 4, 5, rng, persist=0.6)
    wu = fbm(w, h, 3, 3, 3, rng)
    wv = fbm(w, h, 3, 3, 3, rng)
    base = warp(base, wu, wv, 0.22)                             # ¡los REMOLINOS!
    base = (base + 1) * 0.5
    niveles = 7
    cuant = np.round(base * (niveles - 1)) / (niveles - 1)
    cuant = 0.10 + cuant * 0.28                                 # oscuro: 0.10..0.38
    guardar(gris(cuant), 'LodoOscuro')

    # EL BRILLO (128×128): los puntos bioluminiscentes violetas —
    # esparcidos, con latido, algunos en cúmulos.
    rng2 = np.random.default_rng(SEMILLA + 77)
    puntos = np.zeros((h, w), dtype=np.float64)
    xs = np.arange(w)
    ys = np.arange(h)
    X, Y = np.meshgrid(xs, ys)
    for _ in range(26):
        cx, cy = rng2.uniform(0, w), rng2.uniform(0, h)
        n = rng2.integers(1, 5)
        for _ in range(n):
            px = (cx + rng2.uniform(-9, 9)) % w
            py = (cy + rng2.uniform(-7, 7)) % h
            s = rng2.uniform(1.6, 4.2)
            amp = rng2.uniform(0.35, 1.0)
            dx = np.minimum(np.abs(X - px), w - np.abs(X - px))
            dy = np.minimum(np.abs(Y - py), h - np.abs(Y - py))
            puntos += amp * np.exp(-((dx * dx + dy * dy) / (2 * s * s)))
    puntos = np.clip(puntos - 0.10, 0, None)
    puntos = puntos / max(puntos.max(), 1e-6)
    puntos = puntos ** 1.35
    guardar(gris(puntos), 'LodoOscuroBrillo')


# =====================================================================
#  5. EL CETRO DE CONSTELACIONES — EL ÁREA, LA ESTRELLA Y LA LÍNEA
# =====================================================================

def gen_constelaciones(rng):
    print('EL CETRO DE CONSTELACIONES — área, estrella y línea:')

    # EL ÁREA (512×512): la viñeta radial con estrellas embebidas y
    # nebulosa tenue (la referencia: 1624², 4764 colores).
    w, h = 512, 512
    ys, xs = np.meshgrid(np.arange(h), np.arange(w), indexing='ij')
    cx, cy = (w - 1) / 2, (h - 1) / 2
    r = np.sqrt(((xs - cx) / (w * 0.5)) ** 2 + ((ys - cy) / (h * 0.5)) ** 2)
    vineta = np.clip(1.0 - r, 0, 1) ** 1.8
    nebulosa = fbm(w, h, 5, 5, 4, rng, persist=0.5)
    nebulosa = np.clip((nebulosa + 1) * 0.5, 0, 1) ** 1.5
    area = np.clip(vineta * (0.34 + 0.30 * nebulosa), 0, 1)
    # las estrellas embebidas (dentro de la viñeta)
    rng2 = np.random.default_rng(SEMILLA + 91)
    estrellas = np.zeros((h, w), dtype=np.float64)
    for _ in range(130):
        ang = rng2.uniform(0, 2 * np.pi)
        rad = rng2.uniform(0, 0.46) * w * 0.5
        sx_, sy_ = cx + math.cos(ang) * rad, cy + math.sin(ang) * rad
        s = rng2.uniform(1.2, 3.6)
        dx = xs - sx_
        dy = ys - sy_
        estrellas += np.exp(-(dx * dx + dy * dy) / (2 * s * s)) * rng2.uniform(0.4, 1.0)
    area = np.clip(area + estrellas * 0.5 * vineta, 0, 1)
    guardar(gris(area, alpha=np.clip(vineta * 1.25, 0, 1)), 'AreaConstelacion')

    # LA ESTRELLA (34×34): 4 puntas con núcleo blanco (la de la casa,
    # ahora con destello real: puntas largas U/D, cortas L/R).
    w2 = 34
    ys2, xs2 = np.mgrid[0:w2, 0:w2]
    c2 = (w2 - 1) / 2
    dx = (xs2 - c2) / (w2 * 0.5)
    dy = (ys2 - c2) / (w2 * 0.5)
    punta_v = np.exp(-(np.abs(dx) / 0.10) ** 2) * np.clip(1 - np.abs(dy), 0, 1) ** 1.6
    punta_h = np.exp(-(np.abs(dy) / 0.10) ** 2) * np.clip(1 - np.abs(dx), 0, 1) ** 2.6 * 0.8
    nucleo = np.exp(-((dx * dx + dy * dy) / 0.028))
    estrella = np.clip(nucleo + punta_v * 0.9 + punta_h * 0.55, 0, 1)
    guardar(gris(estrella, alpha=np.clip(estrella * 1.3, 0, 1)), 'EstrellaConstelacion')

    # LA LÍNEA (144×144): la aguja-lente — núcleo fino horizontal con
    # halo que muere en los extremos (la referencia ConstellationLine).
    w3, h3 = 144, 144
    perfil = perfil_campana(h3, ancho=0.075, piso=0.0)
    extremos = np.clip(1.0 - (np.abs(np.arange(w3) / w3 - 0.5) * 2) ** 1.6, 0, 1)
    var = 0.9 + 0.1 * ((fbm(w3, h3, 8, 3, 2, rng) + 1) * 0.5)
    linea = perfil[:, None] * extremos[None, :] * var
    linea = linea / max(linea.max(), 1e-6) * 0.95
    halo = perfil_campana(h3, ancho=0.22, piso=0.0)[:, None] * extremos[None, :] * 0.30
    guardar(gris(np.clip(linea + halo, 0, 1), alpha=np.clip(linea * 1.4 + halo, 0, 1)),
            'LineaConstelacion')


# =====================================================================
#  6. EL ESPACIO — LOS CAMPOS ESTELARES (para las alas y el cetro)
# =====================================================================

def gen_espacio(rng):
    print('EL ESPACIO — campos estelares con nebulosas:')

    for nombre, w, n_est, semilla in (('Espacio_0', 1024, 760, 5),
                                      ('Espacio_1', 512, 320, 17)):
        h = w
        rng2 = np.random.default_rng(SEMILLA + semilla)
        ys, xs = np.mgrid[0:h, 0:w]
        campo = np.zeros((h, w), dtype=np.float64)

        # las estrellas (tres tamaños: polvo, medianas, las brillantes)
        for _ in range(n_est):
            sx_, sy_ = rng2.uniform(0, w), rng2.uniform(0, h)
            tipo = rng2.uniform(0, 1)
            if tipo < 0.72:
                s, amp = 0.7, rng2.uniform(0.35, 0.7)       # polvo
            elif tipo < 0.94:
                s, amp = 1.4, rng2.uniform(0.6, 0.9)        # medianas
            else:
                s, amp = 2.8, rng2.uniform(0.9, 1.0)        # brillantes
            dx = np.minimum(np.abs(xs - sx_), w - np.abs(xs - sx_))
            dy = np.minimum(np.abs(ys - sy_), h - np.abs(ys - sy_))
            campo += amp * np.exp(-((dx * dx + dy * dy) / (2 * s * s)))

        # las nebulosas (fBm warpeado, MUY tenue — el fondo no compite)
        neb = fbm(w, h, 4, 4, 4, rng, persist=0.55)
        neb = warp(neb, fbm(w, h, 3, 3, 2, rng), fbm(w, h, 3, 3, 2, rng), 0.30)
        neb = np.clip((neb + 1) * 0.5, 0, 1) ** 2.2 * 0.16

        total = np.clip(campo + neb, 0, 1)
        guardar(gris(total), nombre)


# =====================================================================
#  7. LA TINTA VIVA — METABALLS CON FLUJO HORIZONTAL + LA LUT
#     (LA LETRA: «la textura está puesta de forma vertical cuando
#      debe estar puesta de forma horizontal» — las vetas corren a
#      lo largo de U, que es a lo largo de la cola en juego)
# =====================================================================

def gen_tinta_viva(rng):
    print('LA TINTA VIVA — metaballs horizontales + la LUT:')

    # LA MASA (128×128, RGBA): metaballs toroidales ESTIRADOS a lo
    # largo de U (σx≈2.2σy) + gotitas satélite; RGB = MAPA DE DATOS
    # (R=denso, G=perímetro, B=delgado — el contrato de la LUT).
    w, h = 128, 128
    xs = np.arange(w) / w
    ys = np.arange(h) / h
    X, Y = np.meshgrid(xs, ys)
    densidad = np.zeros((h, w), dtype=np.float64)
    for _ in range(15):
        cx = rng.uniform(0, 1)
        cy = rng.uniform(0.34, 0.66)
        sx = rng.uniform(0.030, 0.075)                      # estiradas a lo largo de U
        sy = sx * rng.uniform(0.38, 0.62)
        amp = rng.uniform(0.55, 1.0)
        dx = np.minimum(np.abs(X - cx), 1 - np.abs(X - cx)) / sx
        dy = np.minimum(np.abs(Y - cy), 1 - np.abs(Y - cy)) / sy
        densidad += amp * np.exp(-((dx * dx + dy * dy) * 0.5))
    # las gotitas satélite (redondas, fuera de la masa)
    for _ in range(22):
        cx = rng.uniform(0, 1)
        cy = rng.uniform(0.10, 0.90)
        s = rng.uniform(0.008, 0.018)
        dx = np.minimum(np.abs(X - cx), 1 - np.abs(X - cx)) / s
        dy = np.minimum(np.abs(Y - cy), 1 - np.abs(Y - cy)) / s
        densidad += rng.uniform(0.5, 0.9) * np.exp(-((dx * dx + dy * dy) * 0.5))
    # LAS VETAS: estrías que corren a lo largo de U dentro de la masa
    vetas = fbm_aniso(w, h, 6, 20, 4, rng, persist=0.5)
    vetas = (vetas + 1) * 0.5
    densidad = densidad * (0.72 + 0.28 * vetas)
    dmax = densidad.max()

    denso = np.clip((densidad - 0.52 * dmax) / (0.30 * dmax), 0, 1)
    medio = np.clip((densidad - 0.30 * dmax) / (0.30 * dmax), 0, 1)
    fino = np.clip(densidad / (0.42 * dmax), 0, 1)

    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    rgba[..., 0] = a_u8(denso)
    rgba[..., 1] = a_u8(medio * 0.9)
    rgba[..., 2] = a_u8(fino * 0.8)
    # EL ALPHA: la masa viva con borde suave (binaria + antialias)
    alfa = np.clip((densidad - 0.26 * dmax) / (0.16 * dmax), 0, 1)
    rgba[..., 3] = a_u8(alfa)
    guardar(rgba, 'TintaViva')

    # LA LUT (64×12): dos filas — la clara (V≈0.2) y la oscura (V≈0.8);
    # U = el desplazamiento de tono (magenta → violeta → azul profundo).
    lw, lh = 64, 12
    u = np.arange(lw) / lw
    # la fila clara: lila-brillante → magenta → violeta claro
    clara_r = 0.55 + 0.45 * np.clip(np.sin(u * np.pi), 0, 1) ** 0.8
    clara_g = 0.30 + 0.34 * np.exp(-((u - 0.35) ** 2) * 9.0) + 0.18 * np.exp(-((u - 0.8) ** 2) * 7.0)
    clara_b = 0.55 + 0.35 * np.exp(-((u - 0.15) ** 2) * 6.0) + 0.28 * np.exp(-((u - 0.72) ** 2) * 8.0)
    # la fila oscura: violeta profundo → magenta oscuro → azul noche
    oscura_r = 0.34 + 0.30 * np.exp(-((u - 0.42) ** 2) * 10.0)
    oscura_g = 0.10 + 0.16 * np.exp(-((u - 0.30) ** 2) * 8.0)
    oscura_b = 0.40 + 0.34 * np.exp(-((u - 0.62) ** 2) * 7.0)
    lut = np.zeros((lh, lw, 3), dtype=np.uint8)
    for fila in range(lh):
        t = fila / (lh - 1)                                  # 0=clara .. 1=oscura
        lut[fila, :, 0] = a_u8(clara_r + (oscura_r - clara_r) * t)
        lut[fila, :, 1] = a_u8(clara_g + (oscura_g - clara_g) * t)
        lut[fila, :, 2] = a_u8(clara_b + (oscura_b - clara_b) * t)
    guardar(lut, 'MapaColorTinta')


# =====================================================================
#  8. LOS RUIDOS PLANOS — Ruido_2 y Ruido_3 (std 14/8.8 = muertos)
# =====================================================================

def gen_ruidos(rng):
    print('LOS RUIDOS DE LAS ALAS — los dos planos regenerados:')

    # Ruido_2: nubes blandas (para Medianoche) — contraste bajo, suave
    w = 512
    nubes = fbm(w, w, 4, 4, 4, rng, persist=0.6)
    nubes = (nubes + 1) * 0.5
    nubes = 0.30 + np.clip(nubes, 0, 1) * 0.52
    guardar(gris(nubes), '../Ruido_2')

    # Ruido_3: cirros estirados (para Eclipse) — vetas 6:1 suaves
    cirros = fbm_aniso(w, w, 3, 18, 4, rng, persist=0.5)
    cirros = (cirros + 1) * 0.5
    cirros = np.clip(cirros ** 1.5 * 1.25, 0, 1) * 0.72 + 0.10
    guardar(gris(cirros), '../Ruido_3')


# =====================================================================
#  EL MAIN
# =====================================================================

if __name__ == '__main__':
    os.makedirs(CARPETA, exist_ok=True)
    rng = np.random.default_rng(SEMILLA)

    print('LA REHABILITACIÓN VISUAL — v6.54.3')
    print('=' * 62)

    gen_rayo_concentrado(rng)
    gen_rayo_ramo(rng)
    gen_estela_espacial(rng)
    gen_lodo_oscuro(rng)
    gen_constelaciones(rng)
    gen_espacio(rng)
    gen_tinta_viva(rng)
    gen_ruidos(rng)

    print('=' * 62)
    print('LISTO — texturas de efecto regeneradas con estructura real.')
