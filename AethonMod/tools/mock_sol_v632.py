#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_sol_v632.py — EL MOCK 1:1 DE LA SUPERGIGANTE ROJA (v6.31/v6.32).

Reimplementa EN NUMPY el pipeline EXACTO que el juego dibuja:

  RuneSunRenderer.DrawSunBody (la técnica del sol original):
    1. BACKGLOW  (ALFA):   BloomCircleSmall ×0.95R backHot 0.7 · ×1.61R backRed 0.45
    2. AURA      (ADD):    WavyBlotchNoise 5.44R shine 0.24 (aprox. RadialShine)
    3. DISCO     (ALFA):   SunShader.fx PIXEL POR PIXEL (el HLSL traducido 1:1):
                           pellizco esférico + doble muestreo auto-desplazado +
                           manchas sustractivas + ríos de lava + corona 1/|d−0.5|
  RedSupergiantRenderer:
    4. CÉLULAS FRÍAS  (ALFA): 9 celdas GiantDeep 0.30·oscuridad
    5. ATMÓSFERA      (ADD):  3 velos glowTint (3.0R/2.2R/1.45R)
    6. CÉLULAS CALIENTES(ADD): 9 celdas SolarFire 0.42·conv
    7. ANILLO DE FUEGO(ADD):  FireRing 2.6R + 1.7R

El test crítico: FONDO CLARO (el cielo de día de Terraria) — la queja de
v6.30 era "invisible sobre fondo claro". Si el disco rojo sólido se ve
sobre el celeste, el fix funciona.

Salida: research/v632/MOCK_SOL_SUPERGIGANTE.png + métricas de cobertura.
"""
import math
import os

import numpy as np
from PIL import Image

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TEX = os.path.join(BASE, "Content", "Effects", "Textures")
PROC = os.path.join(BASE, "Content", "Effects", "Procedural")
OUT = os.path.join(os.path.dirname(BASE), "research", "v632")
os.makedirs(OUT, exist_ok=True)


def carga(path):
    """RGBA float [0,1] (H,W,4)."""
    return np.asarray(Image.open(path).convert("RGBA"), dtype=np.float32) / 255.0


def tex2d(tex, u, v):
    """tex2D con wrap (Repeat) y bilinear — u,v pueden ser arrays (H,W)."""
    h, w = tex.shape[:2]
    u = np.mod(u, 1.0)
    v = np.mod(v, 1.0)
    x = u * (w - 1)
    y = v * (h - 1)
    x0 = np.floor(x).astype(np.int64)
    y0 = np.floor(y).astype(np.int64)
    fx = (x - x0)[..., None]
    fy = (y - y0)[..., None]
    x1 = np.minimum(x0 + 1, w - 1)
    y1 = np.minimum(y0 + 1, h - 1)
    c00 = tex[y0, x0]
    c10 = tex[y0, x1]
    c01 = tex[y1, x0]
    c11 = tex[y1, x1]
    top = c00 * (1 - fx) + c10 * fx
    bot = c01 * (1 - fx) + c11 * fx
    return top * (1 - fy) + bot * fy


def sat(x):
    return np.clip(x, 0.0, 1.0)


def lerp(a, b, t):
    return a + (b - a) * t


# ====================== EL SUNSHADER — TRADUCIÓN 1:1 ==========================
def sun_shader(dendritic, wavy, psychedelic, coords_u, coords_v,
               mainColor, darkerColor, accentFactor,
               sphereSpinTime, globalTime, coronaIntensityFactor):
    """El PixelShaderFunction de SunShader.fx, línea por línea."""
    # coords*2-1 por CANAL
    cx = coords_u * 2 - 1
    cy = coords_v * 2 - 1
    distSqr = (cx * cx + cy * cy) * 2

    # Opacidad: LerpInverso(0.5, 0.42, distSqr) = sat((0.5-x)/0.08)
    opacidad = sat((0.5 - distSqr) / 0.08)

    # Pellizco esférico
    pellizco = (1 - np.sqrt(np.abs(1 - distSqr))) / np.maximum(distSqr, 1e-6) + 0.045
    esfera_u = coords_u * pellizco + sphereSpinTime
    esfera_v = coords_v * pellizco

    # Doble muestreo auto-desplazado (s0 = dendritic)
    desplazamiento = tex2d(dendritic, esfera_u, esfera_v)[..., 0] * 0.41 + globalTime * 0.3
    brillo_u = esfera_u + desplazamiento
    texturaBrillo = tex2d(dendritic, brillo_u, esfera_v)[..., :3]

    # Glow del núcleo
    glowNucleo = sat(1 - distSqr * 0.91)

    # Base
    res = (pellizco[..., None] * mainColor * 0.777
           + glowNucleo[..., None] * darkerColor
           + texturaBrillo)

    # Manchas: hacia darker donde brillo bajo
    m = (sat(1 - texturaBrillo[..., 0]) * 0.8)[..., None]
    res = lerp(res, np.broadcast_to(darkerColor, res.shape), m)

    # Resta sustractiva (s1 = wavy): resultado -= (1-accent)·ruido·1.1
    acento = tex2d(wavy, esfera_u * 2, esfera_v * 2)[..., 0]
    res = res - (1 - accentFactor) * acento[..., None] * 1.1

    # Ríos de lava (s2 = psychedelic como offset): ruido²·2.1
    off = tex2d(psychedelic, coords_u, coords_v + globalTime * 0.4)
    offsetUVu = off[..., 0]
    offsetUVv = off[..., 1]
    lava = tex2d(wavy,
                 esfera_u * 1.2 + offsetUVu * 0.04,
                 esfera_v * 1.2 + offsetUVv * 0.04)[..., 0]
    res = res + (lava ** 2)[..., None] * 2.1

    # Corona: anillo del limbo, 1/|distSqr−0.5+offsets|
    fundido = sat((distSqr - 0.2) / 0.3) * sat((1.91 - distSqr) / 0.93) * coronaIntensityFactor
    brilloCorona = fundido / np.abs(distSqr - 0.5 + offsetUVv * 0.04 + 0.04)

    out = opacidad[..., None] * np.concatenate([res, np.ones_like(res[..., :1])], axis=-1)
    corona_rgb = np.broadcast_to(mainColor, res.shape)
    # (el alfa de la corona: el shader da float4(mainColor,1)·brillo → a=brillo)
    out_a = opacidad + brilloCorona
    out_rgb = out[..., :3] + corona_rgb * brilloCorona[..., None]
    return out_rgb, out_a


# ============================ COMPOSICIÓN =====================================
def blend_alfa(dst, rgb, a):
    """AlphaBlend premult: dst = rgb + dst·(1−a)."""
    return rgb + dst * (1.0 - a[..., None])


def blend_add(dst, rgb):
    return np.clip(dst + rgb, 0, 1)


def sprite_glow(dst, tex, cx, cy, diam, color, alpha, blend):
    """Dibuja una textura cuadrada centrada (escala uniforme) — como spriteBatch.Draw."""
    h, w = dst.shape[:2]
    ts = tex.shape[0]
    half = diam / 2
    x0, x1 = int(cx - half), int(cx + half)
    y0, y1 = int(cy - half), int(cy + half)
    sx0, sy0 = max(0, x0), max(0, y0)
    sx1, sy1 = min(w, x1), min(h, y1)
    if sx1 <= sx0 or sy1 <= sy0:
        return dst
    u = (np.arange(sx0, sx1) + 0.5 - x0) / max(x1 - x0, 1)
    v = (np.arange(sy0, sy1) + 0.5 - y0) / max(y1 - y0, 1)
    uu, vv = np.meshgrid(u, v)
    t = tex2d(tex, uu, vv)
    a = t[..., 3] * alpha
    rgb = t[..., :3] * color[None, None, :] * a[..., None]  # premult
    if blend == "alfa":
        region = dst[sy0:sy1, sx0:sx1]
        dst[sy0:sy1, sx0:sx1] = blend_alfa(region, rgb, a)
    else:
        dst[sy0:sy1, sx0:sx1] = blend_add(dst[sy0:sy1, sx0:sx1], rgb)
    return dst


def main():
    # ---- parámetros del arma (RedSupergiantProjectile + Renderer) ----
    R = 105.0                       # BodyPx (v6.31: 90→105)
    SPIN = 0.35                     # el giro LENTO del coloso
    TIME = 6.18                     # un instante del juego
    LIFE_T = 0.25
    fade = 1 - LIFE_T * 0.30        # 0.925

    # paleta (Draw del RedSupergiantRenderer, collapse=0)
    mainC = np.array([255, 130, 95], np.float32) / 255
    darkerC = np.array([148, 32, 16], np.float32) / 255
    accentC = np.array([120, 0, 0], np.float32) / 255
    backHot = np.array([255, 90, 40], np.float32) / 255
    backRed = np.array([255, 40, 15], np.float32) / 255
    shineC = np.array([255, 110, 60], np.float32) / 255

    dendritic = carga(os.path.join(TEX, "DendriticNoiseZoomedOut.png"))
    wavy = carga(os.path.join(TEX, "WavyBlotchNoise.png"))
    psychedelic = carga(os.path.join(TEX, "PsychedelicWingTextureOffsetMap.png"))
    bloom = carga(os.path.join(TEX, "BloomCircleSmall.png"))
    glow = carga(os.path.join(PROC, "SoftGlow.png"))
    firering = carga(os.path.join(PROC, "FireRing.png"))

    # ---- canvas: el cielo CLARO de Terraria (el test crítico) ----
    W = H = 640
    yy = np.linspace(0, 1, H)[:, None]
    sky_top = np.array([108, 152, 205], np.float32) / 255
    sky_bot = np.array([168, 205, 235], np.float32) / 255
    img = sky_top * (1 - yy) + sky_bot * yy
    img = np.broadcast_to(img, (H, W, 3)).copy()

    cx = cy = W / 2

    # === 1. BACKGLOW (ALFA) — BloomCircleSmall ===
    img = sprite_glow(img, bloom, cx, cy, R * 0.95 * 2 * 0.5 * 2, backHot, 0.7 * fade, "alfa")
    # (0.95R de ESCALA del sol: bScale·0.95 con bloom dibujado a BodyPx →
    #  ancho final ≈ 0.95·R·2·(bloom_px/BodyPx)... el sol dibuja bloom a su
    #  tamaño nativo ×bScale → ancho = 0.95R×(2·bloom_px/…); usamos 0.95·2R
    #  de ancho total ≈ proporción del juego con textura 64px/BodyPx)
    img = sprite_glow(img, bloom, cx, cy, 1.61 * R * 2 * 0.5, backRed, 0.45 * fade, "alfa")

    # === 2. AURA (ADD) — WavyBlotchNoise 5.44R (aprox del RadialShine) ===
    aw = int(5.44 * R)
    x0, x1 = int(cx - aw / 2), int(cx + aw / 2)
    u = (np.arange(x0, x1) + 0.5 - x0) / aw
    v = np.full_like(u, 0.5)
    fila = tex2d(wavy, u, v)[None, :, :3]
    aura = np.repeat(fila, aw, axis=0) * shineC * (0.24 * fade)
    # falloff radial suave (lo que el RadialShine añade)
    d = np.sqrt(((np.arange(aw) - aw / 2 + 0.5) / (aw / 2)) ** 2)
    fall = np.clip(1 - d, 0, 1) ** 1.5
    aura *= (fall[:, None] * fall[None, :])[..., None]
    sub = img[x0:x1, x0:x1]
    img[x0:x1, x0:x1] = blend_add(sub, aura)

    # === 3. EL DISCO — SunShader 1:1 (el quad 3R×3R) ===
    qw = int(R * 3)
    qx0, qy0 = int(cx - qw / 2), int(cy - qw / 2)
    uu = (np.arange(qx0, qx0 + qw) + 0.5 - qx0) / qw
    vv = (np.arange(qy0, qy0 + qw) + 0.5 - qy0) / qw
    UU, VV = np.meshgrid(uu, vv)
    rgb, a = sun_shader(dendritic, wavy, psychedelic, UU, VV,
                        mainC, darkerC, accentC,
                        TIME * SPIN, TIME, 0.05)
    # el vértice: Color.White·alphaMul (premult) → rgb·fade, a·fade
    rgb_premult = np.clip(rgb * (fade), 0, None)
    a_eff = np.clip(a * fade, 0, 1)
    region = img[qy0:qy0 + qw, qx0:qx0 + qw]
    img[qy0:qy0 + qw, qx0:qx0 + qw] = blend_alfa(region, rgb_premult, a_eff)

    disco = img.copy()

    # === 4. CÉLULAS FRÍAS (ALFA) — 9 celdas deterministas ===
    def hash01(seed, k, m):
        x = math.sin(seed * 127.1 + k * 311.7 + m * 74.7) * 43758.5453
        return x - math.floor(x)

    seed = 42
    GiantDeep = np.array([140, 30, 20], np.float32) / 255
    GiantOrange = np.array([255, 140, 60], np.float32) / 255
    for k in range(9):
        h1, h2, h3 = hash01(seed, 401 + k, 3), hash01(seed, 409 + k, 7), hash01(seed, 419 + k, 11)
        ang = h1 * math.tau + TIME * (0.045 + 0.03 * h2) * (1 if k % 2 == 0 else -1)
        convHz = 0.08 + 0.05 * h3
        conv = math.sin(TIME * convHz * math.tau + h2 * math.tau)
        rr = R * (0.20 + 0.48 * h2) * (1 + 0.16 * conv)
        px = cx + math.cos(ang) * rr
        py = cy + math.sin(ang) * rr
        oscuridad = max(0.0, min(1.0, 0.5 - 0.5 * conv))
        if oscuridad < 0.20:
            continue
        size = R * (0.20 + 0.16 * h3) * (1 + 0.30 * conv)
        img = sprite_glow(img, glow, px, py, size * 2, GiantDeep,
                          0.30 * oscuridad * fade, "alfa")

    # === 5. ATMÓSFERA (ADD) — 3 velos ===
    glowTint = backHot
    img = sprite_glow(img, glow, cx, cy, R * 3.0, glowTint, 0.16 * fade, "add")
    img = sprite_glow(img, glow, cx, cy, R * 2.2, glowTint, 0.24 * fade, "add")
    img = sprite_glow(img, glow, cx, cy, R * 1.45,
                      (glowTint + GiantOrange) / 2, 0.34 * fade, "add")

    # === 6. CÉLULAS CALIENTES (ADD) — la rampa SolarFire ===
    SolarFire = [np.array(c, np.float32) / 255 for c in
                 [(255, 240, 200), (255, 190, 80), (255, 120, 30), (255, 60, 10), (200, 20, 5)]]
    for k in range(9):
        h1, h2, h3 = hash01(seed, 401 + k, 3), hash01(seed, 409 + k, 7), hash01(seed, 419 + k, 11)
        ang = h1 * math.tau + TIME * (0.045 + 0.03 * h2) * (1 if k % 2 == 0 else -1)
        convHz = 0.08 + 0.05 * h3
        conv = math.sin(TIME * convHz * math.tau + h2 * math.tau)
        if conv <= 0.15:
            continue
        rr = R * (0.20 + 0.48 * h2) * (1 + 0.16 * conv)
        px = cx + math.cos(ang) * rr
        py = cy + math.sin(ang) * rr
        rel = min(1.0, rr / R)
        temp = max(0.0, min(1.0, 0.42 + 0.20 * conv + 0.12 * (1 - rel)))
        cell = SolarFire[int(temp * (len(SolarFire) - 1))]
        size = R * (0.20 + 0.16 * h3) * (1 + 0.30 * conv)
        img = sprite_glow(img, glow, px, py, size * 2, cell, 0.42 * fade * conv, "add")

    # === 7. ANILLO DE FUEGO (ADD) ===
    img = sprite_glow(img, firering, cx, cy, R * 2.6, glowTint, 0.14 * fade, "add")
    img = sprite_glow(img, firering, cx, cy, R * 1.7,
                      (glowTint + GiantOrange) / 2, 0.18 * fade, "add")

    # ---- MÉTRICAS: ¿el disco es VISIBLE sobre el cielo claro? ----
    # radio del disco según el shader: distSqr<0.5 → |c|<0.5 → r_uv=0.25 → 0.75R
    r_disco = 0.75 * R
    dy, dx = np.mgrid[0:H, 0:W]
    dist = np.sqrt((dx - cx) ** 2 + (dy - cy) ** 2)
    dentro = dist < r_disco * 0.9
    sky_ref = np.array([140, 180, 220], np.float32) / 255
    dif = np.abs(img - sky_ref).sum(axis=2)
    cobertura = (dif[dentro] > 0.25).mean()

    # ---- salida ----
    out = Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8))
    out.save(os.path.join(OUT, "MOCK_SOL_SUPERGIGANTE.png"))
    disco_img = Image.fromarray((np.clip(disco, 0, 1) * 255).astype(np.uint8))
    disco_img.save(os.path.join(OUT, "MOCK_SOL_SUPERGIGANTE_SOLO_DISCO.png"))

    print(f"Radio del disco visible: {r_disco:.0f}px (quad 3R={R*3:.0f})")
    print(f"Cobertura del disco sobre cielo claro: {cobertura*100:.1f}% (los píxeles que difieren del cielo)")
    print(f"Brillo medio del disco: {img[dentro].mean():.3f} · cielo: {sky_ref.mean():.3f}")
    print(f"OK: {'SÍ' if cobertura > 0.95 else 'NO — INVISIBLE'}")


if __name__ == "__main__":
    main()
