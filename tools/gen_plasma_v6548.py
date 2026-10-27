#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 v6.54.8 — GENERADOR DE LAS TEXTURAS DE PLASMA MULTICAPA (la cura de los
 picos) — LA LETRA: «por que parece que todo tiene esos picos en mayor o
 menor medida... esa fea textura de picos no ayuda en nada... el rayo de
 fusión en sus texturas tiene varias capas, como distorsión, desenfoque,
 ruido perlin entre otras tecnicas para que se vea bien».
=============================================================================
 LA AUTOPSIA (Task 79-a, el VLM sobre el collage): las texturas viejas de
 la casa (RuidoTinta = «bandas de fBm en U», RayoConcentrado_2 = gaussiana
 casi sin detalle) eran GEOMETRÍA ANGULAR — al muestrearlas a lo largo de
 un haz, el ojo veía PICOS APILADOS (dientes de sierra). Las del original
 son PLASMA ORGÁNICO: fBm Perlin 1/f (espectro rosa, peine=0), domain
 warping, envolvente suave en V que SIEMPRE manda sobre el ruido, blur
 direccional sutil. TODO EN ESCALA DE GRISES: el color lo pone el tinte
 runtime (la regla de oro nº1 del informe).

 Genera (en Content/Effects/Procedural/):
   CarnePlasma.png   256x256  gris    — la carne del haz (reemplaza el
                                        ROL de RuidoTinta en TexRuido)
   SegundoRuido.png  256x256  gris    — el 2º ruido (el rol de
                                        RayoConcentrado_2 en TexRemolino)
   BitsChispa.png    512x512  blanco  — la máscara de chispas duras
                                        (el uBits del shader original)
   RampaGlow.png     256x256  gris    — la rampa de calor transversal
 El test anti-picos (FFT, peine=0) corre al final y CIERRA el pipeline.
=============================================================================
"""
import os
import numpy as np
from PIL import Image

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Effects/Procedural"


# ------------------------------------------------------------ ruido tileable
def perlin_tileable(w, h, nx, ny, seed=0):
    """Perlin de gradiente con retícula envuelta => tileable perfecto X e Y."""
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
    """fBm: octavas con retícula nx·2^k (entera => wrap exacto)."""
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


def warp_campo(campo, fx, fy):
    """Domain warping bilineal con wrap (LA DISTORSIÓN del original)."""
    h, w = campo.shape
    Y, X = np.mgrid[0:h, 0:w]
    xs = (X + fx) % w
    ys = (Y + fy) % h
    x0 = np.floor(xs).astype(int)
    y0 = np.floor(ys).astype(int)
    dx = xs - x0
    dy = ys - y0
    x0 %= w
    x1 = (x0 + 1) % w
    y0 %= h
    y1 = (y0 + 1) % h
    return (campo[y0, x0] * (1 - dx) * (1 - dy) + campo[y0, x1] * dx * (1 - dy) +
            campo[y1, x0] * (1 - dx) * dy + campo[y1, x1] * dx * dy)


def blur_dir(img, sx, sy):
    """Gaussiano direccional circular vía FFT (preserva tileabilidad)."""
    out = img.astype(float)
    if sx > 0:
        n = out.shape[1]
        lags = np.fft.fftfreq(n, 1.0 / n)
        k = np.exp(-0.5 * (lags / sx) ** 2)
        k /= k.sum()
        out = np.real(np.fft.ifft(np.fft.fft(out, axis=1) * np.fft.fft(k)[None, :], axis=1))
    if sy > 0:
        n = out.shape[0]
        lags = np.fft.fftfreq(n, 1.0 / n)
        k = np.exp(-0.5 * (lags / sy) ** 2)
        k /= k.sum()
        out = np.real(np.fft.ifft(np.fft.fft(out, axis=0) * np.fft.fft(k)[:, None], axis=0))
    return out


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0 + 1e-12), 0, 1)
    return t * t * (3 - 2 * t)


def norm01(a):
    return (a - a.min()) / (a.max() - a.min() + 1e-9)


# ============================================================ RECETA 1: CARNE
def gen_plasma_carne(w=256, h=256, seed=7, pico=1.5, celdas=(12, 9),
                      octavas=5, gan=0.5, warp_amp=6.0, s=(0.22, 0.78),
                      blur=(0.5, 0.3), nucleo=0.50, cn=0.11):
    V = np.linspace(0, 1, h)[:, None] * np.ones((1, w))
    P = pico * smoothstep(0.15, 0.42, V) * (1 - 1.0 * smoothstep(0.50, 0.95, V))
    P += nucleo * np.exp(-((V - 0.47) / cn) ** 2)
    P *= smoothstep(0.005, 0.035, V) * smoothstep(0.995, 0.95, V)
    n = fbm(w, h, *celdas, octavas=octavas, gan=gan, seed=seed)
    wx = fbm(w, h, 3, 3, octavas=3, seed=seed + 50) * warp_amp
    wy = fbm(w, h, 3, 3, octavas=3, seed=seed + 90) * warp_amp * 0.4
    n = norm01(warp_campo(n, wx, wy))
    v = np.clip(P * smoothstep(s[0], s[1], n), 0, 1)
    return blur_dir(v, *blur)


# ============================================================ RECETA 2: BITS
def gen_bits(w=512, h=512, seed=21, celdas=(4, 8), octavas=2,
             cobertura=0.132, suav=2.5):
    n = fbm(w, h, *celdas, octavas=octavas, seed=seed)
    V = np.linspace(0, 1, h)[:, None]
    peso = 0.68 + 0.32 * np.exp(-((V - 0.5) / 0.26) ** 2)
    nw = n * peso
    thr = np.quantile(nw, 1 - cobertura)
    delta = 0.22 * nw.std()
    a = smoothstep(thr - delta, thr + delta, nw)
    a = blur_dir(a, suav, suav * 0.5)
    a = smoothstep(0.30, 0.70, a)
    a *= smoothstep(0.02, 0.09, V) * smoothstep(0.98, 0.91, V)
    return a


# ======================================================== RECETA 3: RAMPA
def gen_rampa_glow(w=256, h=256, seed=33, s_up=0.205, s_dn=0.165, p=2.7,
                   centro=0.53, tilt=0.03, grano=0.05):
    V = np.linspace(0, 1, h)
    d = V - centro
    sig = np.where(d < 0, s_up, s_dn)
    prof = np.exp(-np.clip(np.abs(d) / sig, 0, None) ** p)
    x = np.linspace(0, 1, w)
    env = prof[:, None] * (1 + tilt * np.cos(2 * np.pi * (x[None, :] + 0.31)))
    g = norm01(fbm(w, h, 3, 2, octavas=5, seed=seed)) - 0.5
    env = env * (1 + grano * 2 * g)
    return np.clip(env, 0, 1)


# ===================================================== RECETA 4: SEGUNDO RUIDO
def gen_segundo_ruido(w=256, h=256, seed=55, pico=1.5, celdas=(11, 9),
                      octavas=4, warp_amp=6.0, s=(0.30, 0.62), blur=(0.5, 0.3),
                      nucleo=0.55, cn=0.12, niveles=64):
    V = np.linspace(0, 1, h)[:, None] * np.ones((1, w))
    P = pico * smoothstep(0.18, 0.36, V) * (1 - 1.0 * smoothstep(0.54, 0.92, V))
    P += nucleo * np.exp(-((V - 0.50) / cn) ** 2)
    P *= smoothstep(0.005, 0.04, V) * smoothstep(0.995, 0.95, V)
    n = fbm(w, h, *celdas, octavas=octavas, seed=seed)
    wx = fbm(w, h, 3, 3, octavas=3, seed=seed + 7) * warp_amp
    wy = fbm(w, h, 3, 3, octavas=3, seed=seed + 13) * warp_amp * 0.4
    n = norm01(warp_campo(n, wx, wy))
    v = np.clip(P * smoothstep(s[0], s[1], n), 0, 1)
    v = blur_dir(v, *blur)
    return np.round(np.clip(v, 0, 1) * (niveles - 1)) / (niveles - 1)


# ------------------------------------------------------------------ métricas
def test_anti_picos(v, nombre, exigir_pendiente=True):
    """El test del informe 79-a: FFT por columnas -> peine=0 SIEMPRE; la
    pendiente -0.6..-1.6 SOLO para las carnes (regla 8 del informe — la
    rampa y los bits son texturas de FORMA (94 % DC) y se validan por
    perfil/peine, exactamente como hizo el agente)."""
    v = np.asarray(v, float)
    m = v - v.mean()
    S = np.abs(np.fft.rfft(m, axis=1)).mean(axis=0)
    S = S / (S.sum() + 1e-12)
    mitad = len(S) // 2
    peine = 0
    for b in range(mitad, len(S) - 4):
        loc = np.median(S[max(0, b - 4):b + 5])
        if S[b] > 4 * loc and S[b] > 2 * S[b - 1] and S[b] > 2 * S[b + 1]:
            peine += 1
    fs = np.arange(1, len(S))
    pend = float(np.polyfit(np.log10(fs), np.log10(S[1:] + 1e-12), 1)[0])
    ok = peine == 0 and (not exigir_pendiente or -1.6 <= pend <= -0.6)
    print(f"  {nombre}: peine={peine} pendiente={pend:.2f} -> {'OK' if ok else 'FALLO'}")
    return ok


def a_gris_png(v, ruta):
    Image.fromarray((np.clip(v, 0, 1) * 255).astype(np.uint8)).save(ruta)


def a_bits_png(a, ruta):
    rgba = np.zeros((*a.shape, 4), np.uint8)
    rgba[..., :3] = 255
    rgba[..., 3] = (np.clip(a, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(rgba).save(ruta)


# ------------------------------------------------------------------ main
if __name__ == "__main__":
    print("=== v6.54.8 — LA CURA DE LOS PICOS: las 4 texturas de plasma ===")
    todos_ok = True

    v0 = gen_plasma_carne(seed=7)
    a_gris_png(v0, f"{BASE}/CarnePlasma.png")
    todos_ok &= test_anti_picos(v0, "CarnePlasma   (la carne del haz)")

    v3 = gen_segundo_ruido(seed=55)
    a_gris_png(v3, f"{BASE}/SegundoRuido.png")
    todos_ok &= test_anti_picos(v3, "SegundoRuido  (el que hierve cruzado)")

    v1 = gen_bits(seed=21)
    a_bits_png(v1, f"{BASE}/BitsChispa.png")
    todos_ok &= test_anti_picos(v1, "BitsChispa    (las chispas duras)", exigir_pendiente=False)

    v2 = gen_rampa_glow(seed=33)
    a_gris_png(v2, f"{BASE}/RampaGlow.png")
    todos_ok &= test_anti_picos(v2, "RampaGlow     (la rampa de calor)", exigir_pendiente=False)

    print("\nTodas:", "OK — cero picos por construcción" if todos_ok else "HAY FALLOS")
    raise SystemExit(0 if todos_ok else 1)
