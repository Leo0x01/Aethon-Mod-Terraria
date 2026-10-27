#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 v6.55.0 — LA CURA DE LOS PICOS EN LOS BITS (la 2ª ronda del diagnóstico).
=============================================================================
 LA AUTOPSIA (simulación numpy del pipeline completo de HazFusionEspina):
 las tiras texturizadas son suaves (r=1.09 px) pero LA PASADA DE BITS
 producía zigzag cada ~16.6 px con r=7.13 — cada blob de la máscara se
 lee como UN PICO individual pegado al borde del haz (la cobertura del
 12 % con la envolvente V débil del v6.54.8 — peso piso 0.68 — dejaba
 blobs vivos hasta V=0.09, y la pasada se dibuja a 1.15× el semiancho
 del cuerpo: los blobs SOBRESALÍAN del borde).

 LA CURA (textura): los blobs viven SOLO en el CORAZÓN transversal —
 envolvente V FUERTE (blobs totalmente fuera de V<0.20 y V>0.80), la
 misma cobertura, el mismo alargado 2-3:1, los mismos bordes suaves.
 (La cura del código — semiancho y vMin/vMax de la pasada — va en
 DefinitivasLib: 0.98× y 0.08..0.92.)
=============================================================================
"""
import numpy as np
from PIL import Image

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Effects/Procedural"


def perlin_tileable(w, h, nx, ny, seed=0):
    rng = np.random.default_rng(seed)
    ang = rng.uniform(0, 2 * np.pi, (ny, nx))
    gx, gy = np.cos(ang), np.sin(ang)
    x = np.linspace(0, nx, w, endpoint=False)
    y = np.linspace(0, ny, h, endpoint=False)
    X, Y = np.meshgrid(x, y)
    x0 = np.floor(X).astype(int) % nx
    y0 = np.floor(Y).astype(int) % ny
    x1 = (x0 + 1) % nx
    y1 = (y0 + 1) % ny
    fx = X - np.floor(X)
    fy = Y - np.floor(Y)
    ux = fx * fx * fx * (fx * (fx * 6 - 15) + 10)
    uy = fy * fy * fy * (fy * (fy * 6 - 15) + 10)
    n00 = gx[y0, x0] * fx + gy[y0, x0] * fy
    n10 = gx[y0, x1] * (fx - 1) + gy[y0, x1] * fy
    n01 = gx[y1, x0] * fx + gy[y1, x0] * (fy - 1)
    n11 = gx[y1, x1] * (fx - 1) + gy[y1, x1] * (fy - 1)
    return n00 * (1 - ux) * (1 - uy) + n10 * ux * (1 - uy) + n01 * (1 - ux) * uy + n11 * ux * uy


def fbm(w, h, nx, ny, octavas=5, lac=2.0, gan=0.5, seed=0):
    total = np.zeros((h, w))
    amp = 1.0
    norm = 0.0
    for o in range(octavas):
        sx = max(1, round(nx * lac ** o))
        sy = max(1, round(ny * lac ** o))
        total += amp * perlin_tileable(w, h, sx, sy, seed + 1013 * o)
        norm += amp
        amp *= gan
    return total / norm


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def blur_dir(a, sx, sy):
    """Blur gaussiano separable (sigma por eje) con bordes espejados."""
    from scipy.ndimage import gaussian_filter
    return gaussian_filter(a, sigma=(sy, sx), mode="reflect")


def gen_bits_v6550(w=512, h=512, seed=21, celdas=(4, 8), octavas=2,
                   cobertura=0.132, suav=2.5):
    n = fbm(w, h, *celdas, octavas=octavas, seed=seed)
    V = np.linspace(0, 1, h)[:, None]
    # === LA CURA v6.55.0: LA ENVOLVENTE FUERTE ===
    # Los blobs viven SOLO en el corazón (0.28..0.72); fuera de ahí la
    # máscara muere por completo — NINGÚN blob puede asomarse al borde
    # de la tira, viva donde viva la pasada que la muestrea.
    peso = smoothstep(0.18, 0.32, V) * smoothstep(0.82, 0.68, V)
    nw = n * peso
    thr = np.quantile(nw, 1 - cobertura)
    delta = 0.22 * nw.std()
    a = smoothstep(thr - delta, thr + delta, nw)
    a = blur_dir(a, suav, suav * 0.5)
    a = smoothstep(0.30, 0.70, a)
    # el portón final: la V entera fuera del corazón = CERO
    a *= smoothstep(0.22, 0.32, V) * smoothstep(0.78, 0.68, V)
    return a


if __name__ == "__main__":
    a = gen_bits_v6550()
    # PNG RGBA: blanco donde hay blob (el alfa lleva la máscara — el
    # contrato del BasicEffect texturizado de la casa).
    rgba = np.zeros((a.shape[0], a.shape[1], 4), dtype=np.uint8)
    rgba[..., 0] = 255
    rgba[..., 1] = 255
    rgba[..., 2] = 255
    rgba[..., 3] = (a * 255).astype(np.uint8)
    Image.fromarray(rgba).save(f"{BASE}/BitsChispa.png")

    # === TEST: NINGÚN blob en la zona del borde ===
    filas = a.mean(axis=1)
    fuera = max(filas[:int(0.20 * 512)].max(), filas[int(0.80 * 512):].max())
    dentro = filas[int(0.30 * 512):int(0.70 * 512)].mean()
    print(f"BitsChispa v6.55.0: cobertura={100*(a>0.5).mean():.1f}%")
    print(f"  zona del borde (V<0.20 | V>0.80): máx={fuera:.4f} (debe ser ~0)")
    print(f"  corazón (0.30..0.70): media={dentro:.3f}")
    assert fuera < 0.02, "¡Un blob escapó al borde!"
    print("  TEST DEL BORDE: OK — los blobs viven solo en el corazón")
