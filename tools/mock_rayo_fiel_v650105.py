#!/usr/bin/env python3
"""
v6.50.105 — EL MOCK FIEL DEL RAYO (2ª ronda).

El 1er mock (VLM: «sin borde morado») usó parámetros flojos de dibujo;
éste reproduce EL COMPOSITE DEL JUEGO píxel a píxel:
  · la FUNDA: la gaussiana de RayoStrip (ColF ±1/±0.42/0 · 1.55·w,
    alfas .03/.10/.24) en SOFT-ADD (dst += src·(1−dst));
  · la VENA: VenF ±0.3·(0.72·w) alfas .14/.85/.14 — el filo brillante;
  · EL NÚCLEO NEGRO: cadena de glows radiales (SoftGlow ≈ (1−d/r)^1.3),
    7→4 px (la recalibración post-VLM), AlphaBlend encima.
"""
import math

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

W, H = 340, 220


def h01(seed, a, b):
    w32 = lambda x: x & 0xFFFFFFFF
    h = w32(w32(seed * 374761393) + w32(a * 668265263) + w32(b * 1911520717))
    h = w32(h ^ (h >> 13)); h = w32(h * 1274126177); h = w32(h ^ (h >> 16))
    return (h & 0xFFFFFF) / 16777216.0


def ruido1d(x, s):
    i = math.floor(x); f = x - i; u = f * f * (3 - 2 * f)
    a = h01(s, i, 1013); b = h01(s, i + 1, 1013)
    return a + (b - a) * u


def fbm1d(x, s, octavas=3):
    suma = peso = norma = 0.0; freq = 1.0
    for o in range(octavas):
        suma += ruido1d(x * freq, s + o * 131) * peso
        norma += peso; peso *= 0.5; freq *= 2
    return suma / max(norma, 1e-4)


def canal(a, b, seed, flick, amp=0.10):
    L = math.hypot(b[0] - a[0], b[1] - a[1])
    dx, dy = (b[0] - a[0]) / L, (b[1] - a[1]) / L
    px, py = -dy, dx
    A = min(amp * L, L * 0.30)
    fm, fj = flick * 0.15, flick * 0.47
    pts = []
    for i in range(49):
        t = i / 48
        m = (fbm1d(t * 2.8 + fm, seed, 3) - 0.5) * 2
        j = (fbm1d(t * 10 + fj, seed + 77, 2) - 0.5) * 2
        arco = (ruido1d(t * 0.5 + seed * 0.013, seed + 55) - 0.5) * 2
        d = t - 0.5; d2 = d * d; d5 = d2 * d2 * d
        env = math.exp(-5000 * d5 * d5)
        off = (m * 0.62 + j * 0.38) * A + arco * 0.45 * A
        pts.append((a[0] + dx * L * t + px * off * env,
                    a[1] + dy * L * t + py * off * env))
    pts[0], pts[-1] = a, b
    r = [pts[0]]
    for i in range(48):
        p, q = pts[i], pts[i + 1]
        r.append((p[0] + (q[0] - p[0]) * 0.25, p[1] + (q[1] - p[1]) * 0.25))
        r.append((p[0] + (q[0] - p[0]) * 0.75, p[1] + (q[1] - p[1]) * 0.75))
    r[-1] = pts[-1]
    return r


def main():
    rng = np.random.default_rng(7)
    fy = np.arange(H)[:, None]; fx = np.arange(W)[None, :]
    fondo = np.zeros((H, W, 3))
    fondo[..., 0] = 90 + 80 * np.sin(fx * 0.11)
    fondo[..., 1] = 110 + 60 * np.sin(fy * 0.07)
    fondo[..., 2] = 140 + 70 * np.sin((fx + fy) * 0.05)
    fondo += rng.integers(0, 18, (H, W, 3))
    fondo = np.clip(fondo / 255.0, 0, 1)

    A_P, B_P = (52, 96), (285, 150)
    pts = canal(A_P, B_P, 3 * 31 + 5, 3)
    arcs = [0.0]
    for i in range(1, len(pts)):
        arcs.append(arcs[-1] + math.dist(pts[i - 1], pts[i]))
    total = arcs[-1]

    MORADO = np.array([148, 72, 224]) / 255.0
    CLARO = np.array([232, 214, 255]) / 255.0
    ColF = np.array([-1.0, -0.42, 0.0, 0.42, 1.0])
    ColA = np.array([0.03, 0.10, 0.24, 0.10, 0.03])
    VenF = np.array([-0.30, 0.0, 0.30])
    VenA = np.array([0.14, 0.85, 0.14])

    yy, xx = np.mgrid[0:H, 0:W]
    add_buf = np.zeros((H, W, 3))

    def punto_y_normal(s):
        seg = 0
        while seg + 1 < len(arcs) and arcs[seg + 1] < s:
            seg += 1
        tramo = arcs[seg + 1] - arcs[seg]
        f = (s - arcs[seg]) / tramo if tramo > 0.1 else 0.0
        p = (pts[seg][0] + (pts[seg + 1][0] - pts[seg][0]) * f,
             pts[seg][1] + (pts[seg + 1][1] - pts[seg][1]) * f)
        q0 = pts[max(seg - 1, 0)]
        q1 = pts[min(seg + 2, len(pts) - 1)]
        tang = np.array([q1[0] - q0[0], q1[1] - q0[1]], float)
        n = np.linalg.norm(tang)
        tang = tang / n if n > 0.01 else np.array([1.0, 0.0])
        return p, np.array([-tang[1], tang[0]])

    def blob(p, radio, color, alfa):
        dd = np.hypot(xx - p[0], yy - p[1])
        m = (1 - np.clip(dd / radio, 0, 1)) ** 1.3 * alfa
        add_buf[:] += color[None, None, :] * m[..., None]

    s = 0.0
    while s <= total:
        p, norm = punto_y_normal(s)
        k = s / max(1.0, total)
        # LA VENA (semiancho 0.36·8 = 2.88 px — el filo brillante)
        wv = 2.88 * (1 - 0.3 * k)
        for cf, ca in zip(VenF, VenA):
            blob((p[0] + norm[0] * wv * cf, p[1] + norm[1] * wv * cf),
                 1.6, CLARO, 0.9 * ca * (1 - 0.3 * k))
        # LA FUNDA (semiancho 1.55·8 = 12.4 px — la falda morada)
        wf = 12.4 * (1 - 0.4 * k)
        for cf, ca in zip(ColF, ColA):
            blob((p[0] + norm[0] * wf * cf, p[1] + norm[1] * wf * cf),
                 2.6, MORADO, 0.9 * ca * (1 - 0.4 * k))
        s += 3.0

    dst = fondo.copy()
    src = np.clip(add_buf, 0, 1)
    dst = np.clip(dst + src * (1 - dst), 0, 1)      # el soft-add de RayoStrip

    # EL NÚCLEO NEGRO (AlphaBlend): cadena de glows radiales 7→4 px
    neg = np.zeros((H, W), float)
    s = 0.0
    while s <= total:
        p, norm = punto_y_normal(s)
        k = s / max(1.0, total)
        wcap = (7.0 - 3.0 * k) / 2.0
        dd = np.hypot(xx - p[0], yy - p[1])
        neg = np.maximum(neg, (1 - np.clip(dd / wcap, 0, 1)) ** 1.3 * 0.84)
        s += 3.0
    neg_rgb = np.array([8, 3, 14]) / 255.0
    out = dst * (1 - neg[..., None]) + neg_rgb[None, None, :] * neg[..., None]

    img = Image.fromarray((out * 255).astype(np.uint8), 'RGB').convert('RGBA')
    ojo = Image.open('/home/z/my-project/download/Aethon-Mod-Terraria/'
                     'AethonMod/Content/Projectiles/OjoEterno_Abierto.png').convert('RGBA')
    iris = Image.open('/home/z/my-project/download/Aethon-Mod-Terraria/'
                      'AethonMod/Content/Weapons/GrimorioHambriento_Iris.png').convert('RGBA')
    img.alpha_composite(ojo.resize((88, 74)), (A_P[0] - 44, A_P[1] - 37))
    nrm = math.hypot(B_P[0] - A_P[0], B_P[1] - A_P[1])
    dx, dy = (B_P[0] - A_P[0]) / nrm, (B_P[1] - A_P[1]) / nrm
    img.alpha_composite(iris.resize((17, 17)),
                        (int(A_P[0] + dx * 7.5 - 8.5), int(A_P[1] + dy * 3.5 - 8.5)))
    d2 = ImageDraw.Draw(img)
    d2.ellipse([B_P[0] - 16, B_P[1] - 24, B_P[0] + 16, B_P[1] + 20],
               fill=(120, 40, 60, 255), outline=(60, 20, 30, 255), width=3)
    bruma = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    db = ImageDraw.Draw(bruma)
    for i in range(7):
        ang = i / 7 * 6.28
        bx, by = B_P[0] + math.cos(ang) * 14, B_P[1] - 8 + math.sin(ang) * 10
        rr = 13 + 5 * math.sin(i * 2.1)
        db.ellipse([bx - rr, by - rr, bx + rr, by + rr], fill=(18, 10, 28, 120))
        db.ellipse([bx - rr * 0.5, by - rr * 0.5, bx + rr * 0.5, by + rr * 0.5],
                   fill=(64, 42, 96, 90))
    bruma = bruma.filter(ImageFilter.GaussianBlur(3.5))
    img = Image.alpha_composite(img, bruma)
    img = img.resize((W * 2, H * 2), Image.LANCZOS)
    out_path = ('/home/z/my-project/download/Aethon-Mod-Terraria/'
                'tools/preview_ojos_v650105/mock_rayo_fiel.png')
    img.save(out_path)
    print('mock fiel:', out_path, img.size)


if __name__ == '__main__':
    main()
