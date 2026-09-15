#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_rift_v628.py — RIFTLIB v2: LAS TEXTURAS DEL DESGARRO CONTINUO.

La lección raíz de la investigación v6.28 (INFORME_RIFTLIB_V2.md, medido con
PIL sobre la TrailGlow.png de la casa): LA TrailGlow TIENE EL DEFECTO — su
alfa rampa 3→204 A LO LARGO del eje de longitud y su color es CIAN PURO
(0,255,255). CADA junta de los segmentos era una franja casi transparente y
CIAN = las "interrupciones azules" del usuario. NINGUNA textura de línea de
la casa es uniforme a lo largo.

EL CONTRATO NUEVO (el del ecosistema, verificado en Calamity
BloomLineThick/LineThick/ScarletDevilStreak):
  · UNIFORME a lo largo del eje de longitud (columnas idénticas).
  · Gradiente SOLO a lo ancho (la sección transversal).
  · SIN color horneado (máscara en escala de grises — el color lo pone el
    Tinte del llamador).

ESTE GENERADOR PRODUCE (todas 100% procedurales, reproducibles):
  · RiftTaperVelo.png   (512×64) — EL DESGARRO RECTO EN UN SOLO QUAD:
    par de labios SUAVES (σ 0.42W) a ±0.31W + envolvente tenue, con el
    PERFIL DE LONGITUD horneado (lens sin^0.6 · respiración 15% a 4 ciclos
    — el look "nebulosa" del Dimension-Tearing Disk) y NÚCLEO nulo.
  · RiftTaperCuerpo.png (512×64) — el par de labios MEDIOS (σ 0.20W) a
    ±0.31W con el mismo perfil de longitud.
  · RiftTaperNucleo.png (512×64) — el NÚCLEO RAZOR central (ancho 0.14W,
    caída dura) con perfil de longitud (mínimo 1.5 px en el centro).
  · RiftTaperVoid.png   (512×64) — LA BANDA DE VACÍO para el pase NO
    premultiplicado: negro RGB=8 con alfa duro (ancho 0.62W) + 1 px de
    feather — el agujero en la escena.
  · RiftLip.png         (64×16)  — EL LABIO UNIFORME para caminos
    FRACTURADOS: sección gaussiana (pico 250/255) IDÉNTICA en las 64
    columnas — para segmentos con juntas de perla, CERO variación
    longitudinal.
  · RiftCore.png        (64×16)  — el NÚCLEO UNIFORME (banda dura 30%
    + feather 1px) para el camino fracturado.

VALIDACIÓN: el generador mide sus propias texturas (uniformidad de columnas
en RiftLip/RiftCore; continuidad — sin ceros interiores en las filas activas
de los Taper) y falla si el contrato se rompe.
"""
import os
import numpy as np
from PIL import Image

PROJ_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                        '..', 'AethonMod')
OUT_DIR = os.path.join(PROJ_DIR, 'Content', 'Effects', 'Procedural')

W_TAPER, H_TAPER = 512, 64      # el desgarro recto (un solo quad)
W_LIP, H_LIP = 64, 16           # el labio uniforme del camino fracturado

SS = 4  # supersampling vertical (el horizontal es analítico)

# LA GEOMETRÍA DEL QUAD RECTO (el contrato numérico):
#   El quad se dibuja con ALTURA = 1.60·maxWidth → v=±1 equivale a
#   ±0.80·maxWidth del eje. Con W(x) = maxWidth·lens(x) y h(x)=lens(x):
#     · labios (y borde del vacío) a ±0.31·W(x) → v = 0.3875·h(x)
#     · velo σ 0.28·W(x) → v = 0.35·h(x) (borde ext ~1.09·h — la cola
#       2σ ≈ 0.07 de pico se recorta sin leerse)
#     · cuerpo σ 0.17·W(x) → v = 0.2125·h(x) · núcleo half 0.10·W(x) →
#       v = 0.125·h(x)
#   — los labios cabalgan EXACTAMENTE el borde del vacío (0.62·W de ancho),
#   la misma anatomía del RiftLib v1 pero CERO juntas.


def lens(x):
    """EL PERFIL DE LONGITUD horneado: sin^0.6 (gordo al centro, aguja en
    las puntas) × la respiración "nebulosa" de 4 ciclos (±15% — la lección
    Dimension-Tearing Disk del informe v6.28)."""
    f = x / W_TAPER
    base = np.sin(np.pi * f) ** 0.6
    nebulosa = 1.0 + 0.15 * np.sin(4.0 * np.pi * f)
    return np.clip(base * nebulosa, 0.0, 1.0)


def columna_taper(y_norm, x):
    """La sección transversal del desgarro en la columna x (y_norm −1..+1):
    h(x) = lens(x) — la MITAD-ANCHURA VIVA del corte en unidades de
    ±0.80·maxWidth (v=±1): los labios cabalgan el borde del vacío a
    0.3875·h(x) y el velo alcanza ~1.09·h en su cola exterior."""
    h = lens(x)
    return np.abs(y_norm) <= h, h


def gauss(u, sigma):
    return np.exp(-(u * u) / (2.0 * sigma * sigma + 1e-9))


def gen_taper_pair(lip_off, lip_sigma, power, peak):
    """El par de labios (velo/cuerpo): dos gaussianas a ±lip_off·h(x) con
    ancho lip_sigma·h(x) — la sección clásica del desgarro con su PERFIL DE
    LONGITUD horneado (cero juntas: es UN quad). lip_off=0.477 → los labios
    cabalgan el borde del vacío (±0.31·W(x) px)."""
    img = np.zeros((H_TAPER * SS, W_TAPER, 4), dtype=np.float32)
    # v: −1..+1 (posición vertical relativa del quad)
    vs = (np.arange(H_TAPER * SS) + 0.5) / (H_TAPER * SS) * 2.0 - 1.0
    for x in range(W_TAPER):
        inside, h = columna_taper(vs, x)
        if h <= 0.004:
            continue
        g1 = gauss(vs - lip_off * h, lip_sigma * h + 0.012)
        g2 = gauss(vs + lip_off * h, lip_sigma * h + 0.012)
        # el VALLE entre los labios queda para el pase de vacío (lo tapa el
        # RiftTaperVoid) — aquí solo los labios de luz.
        a = np.clip(g1 + g2, 0.0, 1.0) ** power * peak
        m = a > 0.004
        col = img[:, x, :]
        col[m, 0] = 255.0   # BLANCO NEUTRO (máscara — el color es del tinte)
        col[m, 1] = 255.0
        col[m, 2] = 255.0
        col[m, 3] = a[m] * 255.0
    return img


def gen_taper_core():
    """EL NÚCLEO RAZOR: DOS bandas duras (una por labio, half 0.125·h(x) —
    el blanco que UNIFICA visualmente cada labio en una herida continua) —
    el núcleo vive SOBRE los labios como en RiftLib v1 (el centro queda
    para el vacío), con feather ~1 px."""
    img = np.zeros((H_TAPER * SS, W_TAPER, 4), dtype=np.float32)
    vs = (np.arange(H_TAPER * SS) + 0.5) / (H_TAPER * SS) * 2.0 - 1.0
    for x in range(W_TAPER):
        inside, h = columna_taper(vs, x)
        if h <= 0.004:
            continue
        half = 0.125 * h
        feather = 1.2 / H_TAPER          # ~1 px de feather (AA estándar)
        band = lambda c: np.clip(
            (half + feather - np.abs(vs - c)) / (2.0 * feather + 1e-9), 0.0, 1.0)
        a = np.clip(band(0.3875 * h) + band(-0.3875 * h), 0.0, 1.0)
        m = a > 0.004
        col = img[:, x, :]
        col[m, 0] = 255.0
        col[m, 1] = 255.0
        col[m, 2] = 255.0
        col[m, 3] = a[m] * 255.0
    return img


def gen_taper_void():
    """LA BANDA DE VACÍO (pase NO-premultiplicado): negro RGB=8, alfa DURO
    de half 0.477·h(x) (= ±0.31·W(x) px — el borde del vacío donde cabalgan
    los labios) ... la banda que OCLUYE la escena — el interior del desgarro
    es un agujero, no una franja oscura. 1 px de feather en los bordes."""
    img = np.zeros((H_TAPER * SS, W_TAPER, 4), dtype=np.float32)
    vs = (np.arange(H_TAPER * SS) + 0.5) / (H_TAPER * SS) * 2.0 - 1.0
    for x in range(W_TAPER):
        inside, h = columna_taper(vs, x)
        if h <= 0.004:
            continue
        half = 0.3875 * h                # el borde del vacío (±0.31·W px)
        feather = 1.2 / H_TAPER
        a = np.clip((half + feather - np.abs(vs)) / (2.0 * feather + 1e-9), 0.0, 1.0) * 0.96
        m = a > 0.004
        col = img[:, x, :]
        col[m, 0] = 8.0                    # NEGRO (premultiplicado a ojo:
        col[m, 1] = 8.0                    # RGB casi 0 — ocluye aunque sea
        col[m, 2] = 10.0                   # de día)
        col[m, 3] = a[m] * 255.0
    return img


def gen_uniform(profile):
    """EL LABIO/NÚCLEO UNIFORME (camino fracturado): las W_LIP columnas
    son IDÉNTICAS (la sección transversal only) — cero variación
    longitudinal, el requisito del contrato anti-juntas."""
    img = np.zeros((H_LIP * SS, W_LIP, 4), dtype=np.float32)
    vs = (np.arange(H_LIP * SS) + 0.5) / (H_LIP * SS) * 2.0 - 1.0
    a = profile(vs)
    m = a > 0.004
    for x in range(W_LIP):
        col = img[:, x, :]
        col[m, 0] = 255.0
        col[m, 1] = 255.0
        col[m, 2] = 255.0
        col[m, 3] = a[m] * 255.0
    return img


def lip_profile(vs):
    """La gaussiana del labio (pico 250/255, σ 0.36 — la medición del
    informe: pico ≥250 para que los SOLAPAMIENTOS aditivos no caven
    valles)."""
    return gauss(vs, 0.36) * (250.0 / 255.0)


def core_profile(vs):
    """El núcleo duro (banda 30% + feather 1 px)."""
    half, feather = 0.30, 1.0 / H_LIP
    return np.clip((half + feather - np.abs(vs)) / (2.0 * feather + 1e-9), 0.0, 1.0)


def downsample(img, w, h):
    pil = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), 'RGBA')
    pil = pil.resize((w, h), Image.LANCZOS)
    return np.array(pil, dtype=np.uint8)


def save(img, w, h, name):
    out = downsample(img, w, h)
    # PNG de fondo TRANSPARENTE (alpha real, no premultiplicado en disco —
    # el lote NonPremultiplied del llamador multiplica al vuelo).
    path = os.path.join(OUT_DIR, name)
    Image.fromarray(out, 'RGBA').save(path)
    print(f"  + {name}: {w}x{h} ({os.path.getsize(path)} B)")
    return out


def validar(name, arr, uniform=False, taper=False, taperBandas=False):
    """EL CONTRATO MEDIDO (el generador se auto-verifica):"""
    a = arr[:, :, 3].astype(np.float32) / 255.0
    if uniform:
        # 1) UNIFORMIDAD de columnas: la desviación entre columnas debe ser
        #    ~0 en la fila central (cero variación longitudinal).
        center = a[H_LIP // 2, :]
        desv = float(np.abs(center - center.mean()).max())
        assert desv < 0.02, f"{name}: columnas NO uniformes (desv {desv:.3f})"
        print(f"  · {name}: uniformidad de columnas OK (desv máx {desv:.4f})")
    if taper:
        # 2) CONTINUIDAD: en la FILA CENTRAL el alfa no puede caer a 0 entre
        #    el 10% y el 90% de la longitud (cero juntas = cero huecos).
        center = a[:, W_TAPER // 10: 9 * W_TAPER // 10][H_TAPER // 2]
        holes = int((center < 0.05).sum())
        assert holes == 0, f"{name}: {holes} huecos en la fila central"
        # 3) SIN COLOR horneado: RGB debe ser ~blanco donde hay alfa.
        m = a > 0.5
        if m.any():
            rgb = arr[:, :, :3][m].astype(np.float32)
            sat = float(np.abs(rgb - rgb.mean(axis=1, keepdims=True)).max())
            assert sat < 12.0, f"{name}: color horneado (sat {sat:.1f})"
        print(f"  · {name}: continuidad central OK (0 huecos) · máscara neutra OK")
    if taperBandas:
        # 2-bis) CONTINUIDAD DE LAS BANDAS (núcleo sobre los labios): cada
        # columna del 10%..90% debe tener NÚCLEO en alguna fila.
        zone = a[:, W_TAPER // 10: 9 * W_TAPER // 10]
        colmax = zone.max(axis=0)
        holes = int((colmax < 0.30).sum())
        assert holes == 0, f"{name}: {holes} columnas sin núcleo"
        m = a > 0.5
        if m.any():
            rgb = arr[:, :, :3][m].astype(np.float32)
            sat = float(np.abs(rgb - rgb.mean(axis=1, keepdims=True)).max())
            assert sat < 12.0, f"{name}: color horneado (sat {sat:.1f})"
        print(f"  · {name}: banda continua en TODAS las columnas OK · máscara neutra OK")
    if 'Void' in name:
        # 4) EL VACÍO ES NEGRO: RGB ≤ 16 donde el alfa > 0.5.
        m = a > 0.5
        mx = float(arr[:, :, :3][m].max())
        assert mx <= 16.0, f"{name}: el vacío no es negro (max {mx:.0f})"
        print(f"  · {name}: vacío negro OK (RGB máx {mx:.0f})")


def main():
    print("gen_rift_v628.py — RIFTLIB v2: LAS TEXTURAS DEL DESGARRO CONTINUO")
    os.makedirs(OUT_DIR, exist_ok=True)

    print("\n[1] EL DESGARRO RECTO EN UN SOLO QUAD (perfil de longitud horneado)")
    v = save(gen_taper_pair(0.3875, 0.35, 1.0, 0.55), W_TAPER, H_TAPER, 'RiftTaperVelo.png')
    validar('RiftTaperVelo.png', v, taper=True)
    c = save(gen_taper_pair(0.3875, 0.2125, 1.2, 0.88), W_TAPER, H_TAPER, 'RiftTaperCuerpo.png')
    validar('RiftTaperCuerpo.png', c, taper=True)
    n = save(gen_taper_core(), W_TAPER, H_TAPER, 'RiftTaperNucleo.png')
    validar('RiftTaperNucleo.png', n, taperBandas=True)
    d = save(gen_taper_void(), W_TAPER, H_TAPER, 'RiftTaperVoid.png')
    validar('RiftTaperVoid.png', d, taper=True)

    print("\n[2] EL CAMINO FRACTURADO (uniforme a lo largo — cero juntas)")
    l = save(gen_uniform(lip_profile), W_LIP, H_LIP, 'RiftLip.png')
    validar('RiftLip.png', l, uniform=True)
    k = save(gen_uniform(core_profile), W_LIP, H_LIP, 'RiftCore.png')
    validar('RiftCore.png', k, uniform=True)

    print("\nCONTRATO COMPLETO: 6 texturas neutras, uniformes/continuas, "
          "listas para RiftLib v2 (cero interrupciones azules).")


if __name__ == '__main__':
    main()
