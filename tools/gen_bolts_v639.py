#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_bolts_v637.py — v6.39 — LA REPARACIÓN DE LAS TEXTURAS DE LOS RAYOS.

Nace del informe research/rayos_v639/INFORME_RAYOS_ELECTRICOS_Y_DESGARROS.md:

  BUG RAÍZ 1 — EL LOTE ADITIVO IGNORA EL ALFA. BlendState.Additive de XNA/FNA
  es (Src=One, Dst=One): aporte.rgb = textura.rgb × tinte.rgb, el canal alfa
  de AMBOS no entra NUNCA en la ecuación. Las texturas v6.21 llevaban el
  filamento SOLO en el alfa (RGB=blanco) → en el juego los rayos dibujaban
  RECTÁNGULOS SÓLIDOS y la intensidad de StormLib.Tint (en el alfa del tinte)
  era ignorada. La convención correcta EXISTE en la casa desde v5.x (SoftGlow,
  GlowOrb…: perfil horneado en RGB) — estas texturas la olvidaron.

  BUG RAÍZ 2 — EL PERFIL A LO LARGO DEL EJE X. BoltCore v6.21 llevaba grietas
  (bloques de 16 px), nodos (cada 34-60 px) y serpenteo horneados a lo ancho
  de la textura; StrandImpl estira la textura COMPLETA en cada sub-segmento de
  ≤42 px → cada sección del rayo repetía TODO el patrón → el brillo se CORTABA
  POR SECCIONES (el reporte del usuario).

LA REPARACIÓN (la técnica canónica de los rayos 2D de los grandes):
  · El filamento es UNA BANDA UNIFORME a lo largo (solo 3 px de fundido en
    cada extremo para el antialias de las juntas): el brillo es CONTINUO de
    punta a punta; la variación vive en la geometría y en el crackle por
    punto del propio renderer (interpolado), nunca en la textura.
  · El perfil va PREMULTIPLICADO en RGB (RGB=perfil, alfa=perfil — la
    convención SoftGlow de la casa): el lote aditivo lo respeta, y el lote
    alfa lo respeta igual.
  · La nitidez "eléctrica" se compone por CAPAS DE ANCHO (halo ×2 / cuerpo
    ×1 / núcleo ×0.26) — no por ruido horneado.

También reborna las 3 capas de LUZ del desgarro (RiftTaper Velo/Cuerpo/
Núcleo): leen los PNG v6.31, hornean RGB = RGB·(alfa/255) y dejan el alfa
como está — el desgarro recupera sus labios y su filo en el lote aditivo
(que también les ignoraba el alfa). RiftTaperVoid NO se toca (se dibuja en
el lote no-premultiplicado, donde el alfa sí manda y su negro es sagrado).
"""

import os
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ_DIR = os.path.join(HERE, '..', 'AethonMod')
OUT_DIR = os.path.join(PROJ_DIR, 'Content', 'Effects', 'Procedural')

W, H = 256, 64


def rng(seed):
    """Generador determinista propio (mulberry32) — sin semillas globales."""
    state = [seed & 0xFFFFFFFF]

    def _next():
        state[0] = (state[0] + 0x6D2B79F5) & 0xFFFFFFFF
        t = state[0]
        t = ((t ^ (t >> 15)) * t) & 0xFFFFFFFF
        t = (t ^ (t + (t << 7))) & 0xFFFFFFFF
        return ((t ^ (t >> 17)) & 0xFFFFFFFF) / 4294967296.0

    return _next


def save_premul(arr, name):
    """La convención de la casa v6.39: RGB = PERFIL (premultiplicado — el
    lote aditivo suma textura.rgb y PASA del alfa) y alfa = perfil (el lote
    alfa sigue funcionando igual que siempre)."""
    h, w = arr.shape
    a = np.clip(arr * 255.0, 0, 255).astype(np.uint8)
    img = np.zeros((h, w, 4), dtype=np.uint8)
    img[..., 0:3] = a[..., None]          # RGB = el perfil, horneado
    img[..., 3] = a
    path = os.path.join(OUT_DIR, name)
    Image.fromarray(img, 'RGBA').save(path)
    fila = a[h // 2]
    print(f"  {name}: {w}x{h}  perfil[centro fila] "
          f"x0..3={fila[:4].tolist()} xmid={fila[w//2-2:w//2+3].tolist()} "
          f"xfin={fila[-4:].tolist()}")


def fundido_extremos(arr, px=3):
    """El ÚNICO perfil longitudinal permitido: 3 px de subida/bajada en los
    extremos (el margen de antialias de las juntas del ribbon)."""
    w = arr.shape[1]
    if w <= px * 2:
        return arr
    rampa = np.ones(w)
    rampa[:px] = np.linspace(0.0, 1.0, px)
    rampa[-px:] = np.linspace(1.0, 0.0, px)
    return arr * rampa[None, :]


# =====================================================================
#  1. BOLTHALO — la banda suave del halo (UNIFORME a lo largo)
# =====================================================================

def gen_bolt_halo():
    v = np.linspace(0.0, 1.0, H)[:, None]          # (H,1) — a lo ancho
    # Perfil gaussiano ancho (σ≈0.28): la funda exterior.
    prof = np.exp(-((v - 0.5) / 0.28) ** 2)
    arr = fundido_extremos(np.repeat(prof, W, axis=1))
    save_premul(arr, 'BoltHalo.png')


# =====================================================================
#  2. BOLTCORE — el filamento (UNIFORME a lo largo, sin grietas ni nodos)
# =====================================================================

def gen_bolt_core():
    v = np.linspace(0.0, 1.0, H)[:, None]
    # Perfil ESTRECHO y empinado (σ≈0.155, caída ^1.15) — la vena caliente.
    prof = np.exp(-((v - 0.5) / 0.155) ** 2) ** 1.15
    arr = fundido_extremos(np.repeat(prof, W, axis=1))
    save_premul(arr, 'BoltCore.png')


# =====================================================================
#  3. BOLTCHAIN — la cadena de eslabones (cuentas discretas, RGB horneado)
# =====================================================================

def gen_bolt_chain():
    r = rng(424242)
    u = np.arange(W)
    v = np.linspace(0.0, 1.0, H)[:, None]

    # El puente tenue que une las cuentas.
    bridge = 0.22 * np.exp(-((v - 0.5) / 0.12) ** 2) * np.ones((1, W))

    # Las cuentas: óvalos gaussianos discretos con alturas alternas.
    beads = np.zeros((H, W))
    pos = 8
    flip = 1
    while pos < W - 8:
        cy = 0.5 + flip * 0.065
        su = 8.5 + r() * 3.5
        sv = 0.16 + r() * 0.05
        beads += np.exp(-(((u[None, :] - pos) / su) ** 2
                          + ((v - cy) / sv) ** 2))
        pos += 34 + int(r() * 10)
        flip = -flip

    arr = np.clip(bridge + beads, 0.0, 1.0)
    save_premul(arr, 'BoltChain.png')


# =====================================================================
#  4. BOLTIMPACT — el estallido radial (RGB horneado — antes era invisible
#     en aditivo: un CUADRADO sólido de color)
# =====================================================================

IMP = 96

def gen_bolt_impact():
    r = rng(96310)
    c = IMP / 2.0
    yy, xx = np.mgrid[0:IMP, 0:IMP]
    x = (xx - c + 0.5) / c
    y = (yy - c + 0.5) / c
    rad = np.sqrt(x ** 2 + y ** 2)
    ang = np.arctan2(y, x)

    arr = np.exp(-(rad / 0.14) ** 2)

    # SIETE rayos radiales finos de longitudes desiguales.
    for i in range(7):
        a0 = i * 2 * np.pi / 7 + (r() - 0.5) * 0.35
        reach = 0.62 + r() * 0.34
        sig = 0.052 + 0.030 * np.clip(1.0 - rad / 0.55, 0, 1)
        d = np.abs(np.arctan2(np.sin(ang - a0), np.cos(ang - a0)))
        ray = np.exp(-(d / sig) ** 2) * np.exp(-((rad / reach) ** 1.7)) * 0.85
        arr += ray

    arr += 0.28 * np.exp(-(rad / 0.55) ** 2)
    arr = np.clip(arr, 0.0, 1.0)

    h, w = arr.shape
    a = np.clip(arr * 255.0, 0, 255).astype(np.uint8)
    img = np.zeros((h, w, 4), dtype=np.uint8)
    img[..., 0:3] = a[..., None]
    img[..., 3] = a
    path = os.path.join(OUT_DIR, 'BoltImpact.png')
    Image.fromarray(img, 'RGBA').save(path)
    print(f"  BoltImpact.png: {w}x{h}  RGB horneado "
          f"(antes: blanco puro → cuadrado sólido en aditivo)")


# =====================================================================
#  5. LAS CAPAS DE LUZ DEL DESGARRO — RiftTaper Velo/Cuerpo/Núcleo
#     rebornadas: RGB = RGB·(alfa/255), alfa intacto.
# =====================================================================

def rebornar_rift_taper():
    for name in ('RiftTaperVelo', 'RiftTaperCuerpo', 'RiftTaperNucleo'):
        path = os.path.join(OUT_DIR, f'{name}.png')
        img = np.array(Image.open(path).convert('RGBA')).astype(np.float64)
        rgb, a = img[..., 0:3], img[..., 3]
        # EL HORNEADO: lo que el lote aditivo debe sumar es EXACTAMENTE
        # rgb·alfa/255 (la apariencia que el mock v6.31 validó). El alfa
        # queda intacto para el lote no-premultiplicado (nadie lo usa con
        # estas tres, pero la compatibilidad es gratis).
        img[..., 0:3] = np.clip(rgb * (a / 255.0)[..., None], 0, 255)
        Image.fromarray(img.astype(np.uint8), 'RGBA').save(path)
        r = img[..., 0]
        print(f"  {name}.png rebornada: RGB[{r.min():.0f}..{r.max():.0f}] "
              f"(antes RGB max=255 con alfa {a.min():.0f}..{a.max():.0f})")


if __name__ == '__main__':
    os.makedirs(OUT_DIR, exist_ok=True)
    print("GENERANDO LAS TEXTURAS DE LA TORMENTA v6.39 (la reparación)...")
    gen_bolt_halo()
    gen_bolt_core()
    gen_bolt_chain()
    gen_bolt_impact()
    rebornar_rift_taper()
    print("OK — bandas uniformes premultiplicadas + RiftTaper rebornadas en", OUT_DIR)
