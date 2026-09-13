#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_fusion_olvido_projectile_tex.py — v6.14.1

FIX del client.log del usuario (13/9/2026):
    ReLogic.AssetLoadException: Asset could not be found:
        "Content/Projectiles/Cosmic/FusionBlackHoleProjectile"
        "Content/Projectiles/Cosmic/OlvidoBlackHoleProjectile"

tModLoader AUTO-REQUESTA la textura por defecto de todo ModProjectile
(namespace + nombre de clase) durante TransferAllAssets, aunque el
PreDraw devuelva false y jamás se dibuje. El commit v6.14 creó los dos
agujeros nuevos con TODO su arte procedural/VFX pero olvidó estos DOS
PNGs de sombra visual → el mod entero se desactivaba al cargar.

Patrón: calco EXACTO del CrimsonBlackHoleProjectile.png (v6.09),
medido píxel a píxel:
    · disco negro sólido (r <= 14, alpha 255)
    · rim de identidad  (pico r ≈ 18-22, alpha ~70)
    · desvanecido oscuro hacia r = 37, transparente en 38

Identidades:
    · FUSIÓN  — rim DOBLE: ámbar del Gargantua base (255,170,70)
                por dentro + carmesí del vacío (255,30,100) por fuera.
    · OLVIDO  — rim magenta profundo de la referencia Regicide
                (255,45,110) desvaneciendo a rojo oscuro (110,0,45).

Salida (76×76 RGBA, fondo transparente):
    Content/Projectiles/Cosmic/FusionBlackHoleProjectile.png
    Content/Projectiles/Cosmic/OlvidoBlackHoleProjectile.png
"""

import os
import numpy as np
from PIL import Image

SIZE = 76
CENTER = SIZE / 2.0
OUT_DIR = os.path.join(os.path.dirname(__file__), '..', 'AethonMod',
                       'Content', 'Projectiles', 'Cosmic')


def make_shadow(rim_inner, rim_outer, core_r=14.0, peak_r=19.5,
                fade_r=37.0, peak_alpha=70.0):
    """Sombra visual: disco negro + rim de identidad desvaneciéndose.

    rim_inner: color RGBA del pico del rim (se interpola con rim_outer
               hacia fuera, igual que el patrón carmesí 255,30,100 → 120,0,40).
    """
    yy, xx = np.mgrid[0:SIZE, 0:SIZE]
    r = np.sqrt((xx - CENTER + 0.5) ** 2 + (yy - CENTER + 0.5) ** 2)

    img = np.zeros((SIZE, SIZE, 4), dtype=np.uint8)

    # --- 1. DISCO NEGRO SÓLIDO (horizonte) ---
    disk = r <= core_r
    img[disk] = (0, 0, 0, 255)

    # --- 2. RIM DE IDENTIDAD (pico → desvanecido) ---
    # alpha: campana que sube rápido hasta peak_r y cae suave hasta fade_r
    ring = ~disk & (r <= fade_r)
    rr = r[ring]
    # subida (core_r → peak_r) y caída (peak_r → fade_r)
    rise = np.clip((rr - core_r) / max(peak_r - core_r, 1e-3), 0.0, 1.0)
    fall = 1.0 - np.clip((rr - peak_r) / max(fade_r - peak_r, 1e-3), 0.0, 1.0)
    # campana asimétrica: subida brusca (rim nítido junto al horizonte),
    # caída larga (respiración lejana) — como el carmesí original
    alpha = peak_alpha * (rise ** 1.6) * (fall ** 1.1)
    # mezcla de color: rim_inner en el pico → rim_outer hacia fuera
    t = np.clip((rr - peak_r) / max(fade_r - peak_r, 1e-3), 0.0, 1.0) ** 0.8
    ci = np.array(rim_inner, dtype=float)
    co = np.array(rim_outer, dtype=float)
    rgb = ci[None, :] * (1.0 - t)[:, None] + co[None, :] * t[:, None]

    img[ring, 0] = np.clip(rgb[:, 0], 0, 255).astype(np.uint8)
    img[ring, 1] = np.clip(rgb[:, 1], 0, 255).astype(np.uint8)
    img[ring, 2] = np.clip(rgb[:, 2], 0, 255).astype(np.uint8)
    img[ring, 3] = np.clip(alpha, 0, 255).astype(np.uint8)

    # --- 3. RECORTE LIMPIO DEL BORDE (círculo perfecto, sin dientes) ---
    img[r > fade_r, 3] = 0
    return Image.fromarray(img, 'RGBA')


def main():
    os.makedirs(OUT_DIR, exist_ok=True)

    # === FUSIÓN: rim DOBLE — ámbar del Gargantua + carmesí del vacío ===
    fusion = make_shadow(
        rim_inner=(255, 175, 80),    # ámbar del disco de acreción del base
        rim_outer=(200, 20, 90),     # carmesí del vórtice del vacío
        core_r=14.0, peak_r=19.5, fade_r=37.0, peak_alpha=72.0)
    # aro ámbar extra JUSTO fuera del horizonte (la firma del Gargantua)
    # → lo horneamos elevando el canal del pico: segunda pasada aditiva
    fusion_np = np.array(fusion).astype(int)
    yy, xx = np.mgrid[0:SIZE, 0:SIZE]
    r = np.sqrt((xx - CENTER + 0.5) ** 2 + (yy - CENTER + 0.5) ** 2)
    amber_ring = (r > 14.5) & (r < 17.5)
    boost = np.clip(1.0 - np.abs(r - 15.8) / 1.7, 0.0, 1.0) ** 1.5
    # el ámbar gana en la banda interior (mezcla hacia 255,185,95)
    for k, target in enumerate((255, 185, 95)):
        band = amber_ring & (boost > 0.05)
        w = boost[band] * 0.85
        fusion_np[band, k] = np.clip(
            fusion_np[band, k] * (1.0 - w) + target * w, 0, 255)
    fusion_np[amber_ring, 3] = np.clip(
        fusion_np[amber_ring, 3] + (boost[amber_ring] * 38.0), 0, 255)
    fusion = Image.fromarray(fusion_np.astype(np.uint8), 'RGBA')

    path_f = os.path.join(OUT_DIR, 'FusionBlackHoleProjectile.png')
    fusion.save(path_f)
    print('OK', path_f, fusion.size, fusion.mode)

    # === OLVIDO: rim magenta profundo de la referencia Regicide ===
    olvido = make_shadow(
        rim_inner=(255, 45, 110),    # magenta del anillo de fotones
        rim_outer=(110, 0, 45),      # rojo oscurísimo del vacío rojizo
        core_r=14.0, peak_r=18.5, fade_r=37.0, peak_alpha=74.0)
    path_o = os.path.join(OUT_DIR, 'OlvidoBlackHoleProjectile.png')
    olvido.save(path_o)
    print('OK', path_o, olvido.size, olvido.mode)

    # --- verificación del patrón (mismas invariantes que el carmesí) ---
    for name, path in (('FUSIÓN', path_f), ('OLVIDO', path_o)):
        a = np.array(Image.open(path).convert('RGBA'))
        center_px = a[38, 38]
        corner_px = a[1, 1]
        n_opaque = (a[:, :, 3] == 255).sum()
        n_visible = (a[:, :, 3] > 0).sum()
        assert tuple(center_px) == (0, 0, 0, 255), f'{name}: centro debe ser negro sólido, dio {center_px}'
        assert corner_px[3] == 0, f'{name}: esquina debe ser transparente, dio {corner_px}'
        assert 2000 < n_visible < 5000, f'{name}: masa visible fuera de rango: {n_visible}'
        print(f'  {name}: centro={tuple(center_px)} esquina_alpha={corner_px[3]} '
              f'px_visibles={n_visible} px_opacos={n_opaque} ✓')


if __name__ == '__main__':
    main()
