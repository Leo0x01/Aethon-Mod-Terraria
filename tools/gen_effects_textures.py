#!/usr/bin/env python3
"""Genera las texturas de ruido y glow del sistema de efectos del mod.

Todas las texturas se generan proceduralmente con ruido de valor periódico
(tilable) de múltiples octavas + deformación de dominio. El script es la
fuente de verdad de las texturas: puede regenerarlas en cualquier momento.

Texturas generadas (512x512 salvo indicación):
  - FireNoiseB.png ................ ruido turbulento tipo fuego (gris en RGB)
  - DendriticNoiseZoomedOut.png ... ruido ridge filamentoso (gris en RGB)
  - WavyBlotchNoise.png ........... manchas suaves onduladas (gris en RGB)
  - PsychedelicWingTextureOffsetMap.png ... mapa de offsets UV (R,G indep., B=255)
  - BloomCircleSmall.png .......... glow radial (200x200 RGBA, perfil medido)
  - InvisiblePixel.png ............ píxel transparente 1x1
"""
import numpy as np
from PIL import Image
import os
import sys

OUT = os.path.join(os.path.dirname(__file__), '..', 'AethonMod',
                   'Content', 'Effects', 'Textures')

# ----------------------------------------------------------------------
# Ruido de valor periódico (tilable) con interpolación suave
# ----------------------------------------------------------------------

def _hash_pair(ix, iy, seed):
    """Hash determinista de coordenadas enteras -> [0,1)."""
    h = (ix * 374761393 + iy * 668265263 + seed * 144665477) & 0xFFFFFFFF
    h = (h ^ (h >> 13)) * 1274126177 & 0xFFFFFFFF
    h = h ^ (h >> 16)
    return (h & 0xFFFFFF) / float(0xFFFFFF)


def value_noise_grid(size, period, seed):
    """Ruido de valor en rejilla periódica de lado `period` (tilable)."""
    ys, xs = np.mgrid[0:size, 0:size].astype(float)
    u = xs / size * period
    v = ys / size * period
    ix = np.floor(u).astype(int) % period
    iy = np.floor(v).astype(int) % period
    fx = u - np.floor(u)
    fy = v - np.floor(v)
    # Quintic smoothstep para interpolación C2
    fx = fx * fx * fx * (fx * (fx * 6 - 15) + 10)
    fy = fy * fy * fy * (fy * (fy * 6 - 15) + 10)
    c00 = np.vectorize(lambda a, b: _hash_pair(a, b, seed))(ix, iy)
    c10 = np.vectorize(lambda a, b: _hash_pair((a + 1) % period, b, seed))(ix, iy)
    c01 = np.vectorize(lambda a, b: _hash_pair(a, (b + 1) % period, seed))(ix, iy)
    c11 = np.vectorize(lambda a, b: _hash_pair((a + 1) % period, (b + 1) % period, seed))(ix, iy)
    nx0 = c00 * (1 - fx) + c10 * fx
    nx1 = c01 * (1 - fx) + c11 * fx
    return nx0 * (1 - fy) + nx1 * fy


def fbm(size, base_period, octaves, seed, ridge=False, gain=0.5):
    """Suma de octavas de ruido de valor periódico (tilable en 512)."""
    acc = np.zeros((size, size), dtype=float)
    amp, total = 1.0, 0.0
    period = base_period
    for o in range(octaves):
        n = value_noise_grid(size, period, seed + o * 131)
        if ridge:
            n = 1.0 - np.abs(n * 2 - 1)   # crestas filamentosas
        acc += n * amp
        total += amp
        amp *= gain
        period = min(period * 2, size)
    return acc / total


def domain_warp(field, warp_x, warp_y, strength=0.06):
    """Deforma el dominio de `field` con dos campos de desplazamiento."""
    h, w = field.shape
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    sx = (xx + warp_x * strength * w) % w
    sy = (yy + warp_y * strength * h) % h
    x0 = np.floor(sx).astype(int) % w
    y0 = np.floor(sy).astype(int) % h
    x1 = (x0 + 1) % w
    y1 = (y0 + 1) % h
    fx = sx - np.floor(sx)
    fy = sy - np.floor(sy)
    v00 = field[y0, x0]
    v10 = field[y0, x1]
    v01 = field[y1, x0]
    v11 = field[y1, x1]
    return (v00 * (1 - fx) * (1 - fy) + v10 * fx * (1 - fy) +
            v01 * (1 - fx) * fy + v11 * fx * fy)


def remap(field, lo, hi):
    """Reescala al rango [lo, hi]."""
    f = field - field.min()
    if f.max() > 0:
        f = f / f.max()
    return lo + f * (hi - lo)


def save_gray(field, name, lo, hi, size=512):
    g = np.clip(remap(field, lo, hi), 0, 255).astype(np.uint8)
    rgb = np.stack([g, g, g], axis=-1)
    Image.fromarray(rgb, 'RGB').save(os.path.join(OUT, name))
    print(f"  {name}: rango {g.min()}-{g.max()} media {g.mean():.0f} std {g.std():.0f}")


# ----------------------------------------------------------------------
# Generadores
# ----------------------------------------------------------------------

def gen_fire_noise_b():
    """Ruido turbulento tipo fuego: fBm de alta frecuencia + warp fuerte."""
    print("FireNoiseB.png (ruido turbulento tipo fuego)")
    base = fbm(512, 8, 5, seed=7717, gain=0.55)
    wx = fbm(512, 4, 3, seed=4421)
    wy = fbm(512, 4, 3, seed=9281)
    warped = domain_warp(base, wx, wy, strength=0.35)
    # Realce de contrastes tipo llama (potencia suave)
    warped = np.power(warped, 1.25)
    save_gray(warped, 'FireNoiseB.png', 28, 186)


def gen_dendritic_noise():
    """Ruido dendrítico: fBm ridge (filamentos ramificados) + warp moderado."""
    print("DendriticNoiseZoomedOut.png (ruido ridge filamentoso)")
    base = fbm(512, 6, 6, seed=3313, ridge=True, gain=0.52)
    wx = fbm(512, 3, 3, seed=5171)
    wy = fbm(512, 3, 3, seed=8123)
    warped = domain_warp(base, wx, wy, strength=0.18)
    # Curva de tono oscura: fondo predominante + filamentos brillantes
    warped = np.power(warped, 2.2)
    save_gray(warped, 'DendriticNoiseZoomedOut.png', 20, 230)


def gen_wavy_blotch_noise():
    """Manchas suaves onduladas: fBm de baja frecuencia, muy difuso."""
    print("WavyBlotchNoise.png (manchas onduladas)")
    base = fbm(512, 4, 4, seed=6241, gain=0.6)
    wx = fbm(512, 3, 2, seed=1193)
    wy = fbm(512, 3, 2, seed=4783)
    warped = domain_warp(base, wx, wy, strength=0.22)
    # Apertura suave: solo las manchas más intensas sobreviven
    warped = np.clip((warped - 0.35) / 0.65, 0, 1)
    warped = np.power(warped, 1.6)
    save_gray(warped, 'WavyBlotchNoise.png', 0, 231)


def gen_psychedelic_offset_map():
    """Mapa de offsets UV: R y G con ruido independiente suave, B=255."""
    print("PsychedelicWingTextureOffsetMap.png (mapa de offsets RG)")
    size = 512
    field_r = fbm(size, 6, 4, seed=9137, gain=0.55)
    field_g = fbm(size, 6, 4, seed=2593, gain=0.55)
    wrx = fbm(size, 3, 2, seed=6113)
    wry = fbm(size, 3, 2, seed=7331)
    field_r = domain_warp(field_r, wrx, wry, strength=0.25)
    field_g = domain_warp(field_g, wry, wrx, strength=0.25)
    # Contraste alto alrededor del pivote: histograma ancho
    field_r = np.clip((field_r - 0.5) * 2.4 + 0.5, 0, 1)
    field_g = np.clip((field_g - 0.5) * 2.4 + 0.5, 0, 1)
    r = np.clip(remap(field_r, 0, 255), 0, 255).astype(np.uint8)
    g = np.clip(remap(field_g, 0, 255), 0, 255).astype(np.uint8)
    b = np.full((size, size), 255, dtype=np.uint8)
    rgb = np.stack([r, g, b], axis=-1)
    Image.fromarray(rgb, 'RGB').save(os.path.join(OUT, 'PsychedelicWingTextureOffsetMap.png'))
    print(f"  R: {r.min()}-{r.max()} media {r.mean():.0f} std {r.std():.0f}")
    print(f"  G: {g.min()}-{g.max()} media {g.mean():.0f} std {g.std():.0f}")


def gen_bloom_circle_small():
    """Glow radial blanco (200x200 RGBA): falloff radial suave hasta negro."""
    print("BloomCircleSmall.png (glow radial)")
    size = 200
    ys, xs = np.mgrid[0:size, 0:size].astype(float)
    cy = cx = (size - 1) / 2.0
    r = np.sqrt((xs - cx) ** 2 + (ys - cy) ** 2) / (size / 2.0)
    # Perfil de falloff: gaussiana recortada ajustada al brillo del núcleo
    v = 236.0 * np.exp(-(r / 0.62) ** 2 * 2.1)
    v[r >= 1.0] = 0.0
    v = np.clip(v, 0, 255).astype(np.uint8)
    rgba = np.stack([v, v, v, np.full_like(v, 255)], axis=-1)
    Image.fromarray(rgba, 'RGBA').save(os.path.join(OUT, 'BloomCircleSmall.png'))
    print(f"  centro={v[size//2, size//2]} borde={v[0, size//2]}")


def gen_invisible_pixel():
    """Píxel transparente 1x1 (canvas invisible para los shaders)."""
    print("InvisiblePixel.png (píxel transparente)")
    Image.fromarray(np.zeros((1, 1, 4), dtype=np.uint8), 'RGBA').save(
        os.path.join(OUT, 'InvisiblePixel.png'))


def main():
    os.makedirs(OUT, exist_ok=True)
    gen_fire_noise_b()
    gen_dendritic_noise()
    gen_wavy_blotch_noise()
    gen_psychedelic_offset_map()
    gen_bloom_circle_small()
    gen_invisible_pixel()
    print("\nTodas las texturas regeneradas proceduralmente.")


if __name__ == '__main__':
    sys.path.insert(0, os.path.dirname(__file__))
    main()
