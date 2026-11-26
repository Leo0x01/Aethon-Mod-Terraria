#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_proyeccion_v6178.py — LAS CUATRO TEXTURAS DEL RAYO DE VERDAD.

v6.178 — la letra: «el visor del proyectado no se ve como el arma real,
se ve horrible y mal hecho, lo que me importa es el ataque que hace debes
copiar su tecnica ya que esta tecnica es basica en muchos mods».

EL PLANO decompilado del arma real (CiclopeProyectado v1.0.1, el .tmod
que subió el usuario) usa CUATRO texturas para el rayo concentrado:
  Haz        16×64   — la rodaja del tubo (X=a lo largo, Y=a lo ancho,
                      perfil suave en Y: brilla al centro, muere en el borde)
  HazBorde   64×64   — la TAPA redonda del extremo (desvanece en X)
  RayoCuerpo 256×64  — el plasma que FLUYE (fBm tileable en X, perfil Y suave)
  RayoFlujo  256×64  — el segundo flujo (más fino, otra escala)

Estas son LAS DE LA CASA: 100% procedurales (numpy + PIL), generadas por
este script — cero material de terceros (la regla de la casa v6.54.0).
Mismos FORMATOS que el plano (para que la técnica copiada luzca igual),
contenido 100% nuestro: fBm periódico en X + perfil de coseno en Y,
premultiplicado (convención rgb<=a de la casa).

Salida: AethonMod/Content/Effects/Procedural/Proyeccion{Haz,Borde,Cuerpo,Flujo}.png
"""
import numpy as np
from PIL import Image
import os

SEMILLA = 6178
rng = np.random.default_rng(SEMILLA)
DESTINO = os.path.join(os.path.dirname(__file__), "..", "AethonMod",
                       "Content", "Effects", "Procedural")

# ----------------------------------------------------------------------
# EL PERFIL SUAVE EN Y (a lo ancho del haz): coseno — 0 en los bordes,
# 1 al centro. EL MISMO de las cuatro texturas: las capas se funden.
# ----------------------------------------------------------------------
def perfil_y(alto):
    t = (np.arange(alto) + 0.5) / alto          # 0..1
    return np.sin(t * np.pi) ** 1.25             # coseno apretado: centro vivo

# ----------------------------------------------------------------------
# EL fBm PERIÓDICO EN X (tileable de verdad: la textura se repite a lo
# largo del haz SIN costura — la clave de la técnica del plano)
# ----------------------------------------------------------------------
def fbm_periodico_x(ancho, alto, octavas=(1, 2, 4, 8, 16), base=None, ganancia=1.0):
    if base is None:
        base = rng
    ruido = np.zeros((alto, ancho), dtype=np.float64)
    amplitud_total = 0.0
    for o in octavas:
        k = o  # frecuencia en X (la Y queda libre: el perfil la viste)
        # la componente de baja frecuencia en Y da las lenguas largas
        alto_y = 4
        m = (base.random((alto_y, k * 2)), base.random((alto_y, k * 2)))
        grilla = np.zeros((alto_y, k * 2), dtype=np.float64)
        # interpolación suave (value noise) en una malla envolvente en X
        # (periódica: los bordes X se pegan) y libre en Y
        for iy in range(alto_y):
            for ix in range(k * 2):
                grilla[iy, ix] = m[0][iy, ix]
        # --- interpolación bilineal con envoltura periódica en X ---
        fine_y = np.linspace(0, alto_y - 1, alto)
        fine_x = np.linspace(0, k * 2, ancho, endpoint=False)
        iy0 = np.floor(fine_y).astype(int) % alto_y
        iy1 = (iy0 + 1) % alto_y
        fx = fine_x % (k * 2)
        ix0 = np.floor(fx).astype(int) % (k * 2)
        ix1 = (ix0 + 1) % (k * 2)   # ← LA COSTURA MUERE: el vecino derecho del último es el primero
        tx = (fx - np.floor(fx))[None, :]
        ty = (fine_y - np.floor(fine_y))[:, None]
        sx = 3 * tx ** 2 - 2 * tx ** 3
        sy = 3 * ty ** 2 - 2 * ty ** 2 + 0 * ty  # placeholder, se corrige abajo
        sy = (3 * ty ** 2 - 2 * ty ** 3)
        g00 = grilla[iy0][:, ix0]
        g01 = grilla[iy0][:, ix1]
        g10 = grilla[iy1][:, ix0]
        g11 = grilla[iy1][:, ix1]
        capa = (g00 * (1 - sy) * (1 - sx) + g01 * (1 - sy) * sx +
                g10 * sy * (1 - sx) + g11 * sy * sx)
        # normaliza y suma (1/f: espectro rosa — la ley de la casa)
        amplitud = 1.0 / o
        ruido += amplitud * (capa - capa.mean()) / (capa.std() + 1e-9)
        amplitud_total += amplitud
    ruido /= amplitud_total
    # a [0,1] con realce suave
    ruido = (ruido - ruido.min()) / (ruido.max() - ruido.min() + 1e-9)
    ruido = ruido ** 1.15
    return ruido * ganancia

def fbm2_periodico_x(ancho, alto, octavas=(1, 2, 4, 8, 16), base=None):
    """El value noise directo (sin normalización por octava) — el grano vivo."""
    if base is None:
        base = rng
    ruido = np.zeros((alto, ancho), dtype=np.float64)
    amp_t = 0.0
    for o in octavas:
        k = o
        alto_y = 6
        grilla = base.random((alto_y, k * 2))
        fine_y = np.linspace(0, alto_y - 1, alto)
        fine_x = np.linspace(0, k * 2, ancho, endpoint=False)
        iy0 = np.floor(fine_y).astype(int) % alto_y
        iy1 = (iy0 + 1) % alto_y
        fx = fine_x % (k * 2)
        ix0 = np.floor(fx).astype(int) % (k * 2)
        ix1 = (ix0 + 1) % (k * 2)   # periodicidad en X
        tx = (fx - np.floor(fx))[None, :]
        ty = (fine_y - np.floor(fine_y))[:, None]
        sx = 3 * tx ** 2 - 2 * tx ** 3
        sy = 3 * ty ** 2 - 2 * ty ** 3
        g00 = grilla[iy0][:, ix0]
        g01 = grilla[iy0][:, ix1]
        g10 = grilla[iy1][:, ix0]
        g11 = grilla[iy1][:, ix1]
        capa = (g00 * (1 - sy) * (1 - sx) + g01 * (1 - sy) * sx +
                g10 * sy * (1 - sx) + g11 * sy * sx)
        amp = 1.0 / (o ** 0.9)
        ruido += amp * capa
        amp_t += amp
    return np.clip(ruido / amp_t * 1.35, 0, 1)

def a_png(arr, ruta):
    # PREMULTIPLICADO de la casa: rgb <= a (el alfa lleva la máscara)
    a = np.clip(arr * 255, 0, 255).astype(np.uint8)
    img = Image.fromarray(a, "L")
    img.save(ruta)
    print(f"  {os.path.basename(ruta):28s} {img.size[0]}×{img.size[1]}")

def a_png_rgb(arr, ruta, tinte=(255, 255, 255)):
    m = np.clip(arr, 0, 1)[..., None]
    rgb = np.array(tinte, dtype=np.float64)[None, None, :] / 255.0
    rgba = np.zeros((arr.shape[0], arr.shape[1], 4), dtype=np.float64)
    rgba[..., :3] = np.clip(m * rgb, 0, 1)     # premultiplicado
    rgba[..., 3] = np.clip(m, 0, 1)            # el alfa lleva la máscara
    out = (np.clip(rgba, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(out, "RGBA").save(ruta)
    print(f"  {os.path.basename(ruta):28s} {out.shape[1]}×{out.shape[0]}")

os.makedirs(DESTINO, exist_ok=True)
print("[proyección] forjando las cuatro texturas de la casa (semilla %d)…" % SEMILLA)

# ── 1. PROYECCION HAZ (16×64): la rodaja del tubo — perfil Y puro ──
p = perfil_y(64)
a_png(np.repeat(p[:, None], 16, axis=1),      # (64 alto, 16 ancho)
      os.path.join(DESTINO, "ProyeccionHaz.png"))

# ── 2. PROYECCION BORDE (64×64): la tapa — desvanece en X (desde X=64
#    hacia X=0 se muere: la punta redonda del tubo) + el perfil Y ──
ancho, alto = 64, 64
fx = np.linspace(0, 1, ancho) ** 0.8       # 0 en X=0 (la punta que muere) → 1 en X=63 (la boca del tubo)
capa = np.repeat(fx[None, :], alto, axis=0) * np.repeat(perfil_y(alto)[:, None], ancho, axis=1)
a_png(capa, os.path.join(DESTINO, "ProyeccionBorde.png"))

# ── 3. PROYECCION CUERPO (256×64): EL PLASMA QUE FLUYE — fBm tileable
#    en X (espectro rosa), vetas alargadas a lo largo, perfil Y suave ──
cuerpo = fbm2_periodico_x(256, 64)
# alarga las vetas: promedia con su copia desplazada (aniso suave)
cuerpo = cuerpo * 0.72 + np.roll(cuerpo, 24, axis=1) * 0.28
cuerpo = (cuerpo - cuerpo.min()) / (cuerpo.max() - cuerpo.min() + 1e-9)
cuerpo = np.clip(cuerpo ** 1.18, 0, 1)
cuerpo *= np.repeat(perfil_y(64)[:, None], 256, axis=1)
a_png(cuerpo, os.path.join(DESTINO, "ProyeccionCuerpo.png"))

# ── 4. PROYECCION FLUJO (256×64): el segundo flujo — MÁS FINO (otras
#    octavas base) para que cruce al cuerpo y hierva ──
flujo = fbm2_periodico_x(256, 64, octavas=(2, 4, 8, 16, 32))
flujo = flujo * 0.66 + np.roll(flujo, -40, axis=1) * 0.34
flujo = (flujo - flujo.min()) / (flujo.max() - flujo.min() + 1e-9)
flujo = np.clip(flujo ** 1.3, 0, 1)
flujo *= np.repeat(perfil_y(64)[:, None], 256, axis=1) * 0.95
a_png(flujo, os.path.join(DESTINO, "ProyeccionFlujo.png"))

# ── LA VERIFICACIÓN DE LA COSTURA (el test de la técnica): la última
#    columna y la primera deben fundirse SIN salto ──
for nombre in ("ProyeccionCuerpo", "ProyeccionFlujo"):
    img = np.asarray(Image.open(os.path.join(DESTINO, nombre + ".png")), dtype=np.float64)
    salto = abs(img[:, -1, 3] - img[:, 0, 3]).max() if img.ndim == 3 else abs(img[:, -1] - img[:, 0]).max()
    print(f"  costura {nombre}: salto máx {salto:.1f}/255 (<= 26 = FUNDIDA)")

print("[proyección] LISTO — cuatro texturas de la casa en Effects/Procedural/")
