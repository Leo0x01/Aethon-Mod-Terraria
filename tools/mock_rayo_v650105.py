#!/usr/bin/env python3
"""
v6.50.105 — MOCK VISUAL DEL RAYO DEL OJO (pre-flight del look).

El rayo del minion es la PRIMERA combinación nueva de la casa: la funda
morada aditiva (RayoStrip — probada) + EL NÚCLEO NEGRO en AlphaBlend
encima (nuevo). Este mock reproduce la matemática EXACTA del juego en
Python (Hash01/Ruido1D/Fbm1D/CanalPerlin porteados) y la compone con
PIL sobre un fondo de prueba, para validar con VLM que lee «negro con
bordes morados» ANTES de publicar.
"""
import math

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = '/home/z/my-project/download/Aethon-Mod-Terraria/tools/preview_ojos_v650105'


def h01(seed, a, b):
    """VFXCore.Hash01 — el hash de la casa (entero con wrap de 32 bits)."""
    def w32(x):
        return x & 0xFFFFFFFF
    h = w32(w32(seed * 374761393) + w32(a * 668265263) + w32(b * 1911520717))
    h = w32(h ^ (h >> 13))
    h = w32(h * 1274126177)
    h = w32(h ^ (h >> 16))
    return (h & 0xFFFFFF) / 16777216.0


def ruido1d(x, semilla):
    i = math.floor(x)
    f = x - i
    u = f * f * (3 - 2 * f)
    a = h01(semilla, i, 1013)
    b = h01(semilla, i + 1, 1013)
    return a + (b - a) * u


def fbm1d(x, semilla, octavas=3):
    suma = peso = 0.0
    norma = 0.0
    freq = 1.0
    for o in range(octavas):
        suma += ruido1d(x * freq, semilla + o * 131) * peso
        norma += peso
        peso *= 0.5
        freq *= 2
    return suma / max(norma, 1e-4)


def canal_perlin(a, b, seed, flick, amp_frac=0.10):
    """CanalPerlin del minion — VERBATIM."""
    length = math.hypot(b[0] - a[0], b[1] - a[1])
    if length < 4:
        return [a, b]
    dirx = (b[0] - a[0]) / length
    diry = (b[1] - a[1]) / length
    perpx, perpy = -diry, dirx
    amp = amp_frac * length
    amp = min(amp, length * 0.30)
    fase_m = flick * 0.15
    fase_j = flick * 0.47
    n = 48
    pts = []
    for i in range(n + 1):
        t = i / n
        m = (fbm1d(t * 2.8 + fase_m, seed, 3) - 0.5) * 2
        j = (fbm1d(t * 10 + fase_j, seed + 77, 2) - 0.5) * 2
        arco = (ruido1d(t * 0.5 + seed * 0.013, seed + 55) - 0.5) * 2
        d = t - 0.5
        d2 = d * d
        d5 = d2 * d2 * d
        env = math.exp(-5000 * d5 * d5)
        off = (m * 0.62 + j * 0.38) * amp + arco * 0.45 * amp
        pts.append((a[0] + dirx * length * t + perpx * off * env,
                    a[1] + diry * length * t + perpy * off * env))
    pts[0] = a
    pts[n] = b
    # Chaikin 1
    r = [pts[0]]
    for i in range(n):
        p, q = pts[i], pts[i + 1]
        r.append((p[0] + (q[0] - p[0]) * 0.25, p[1] + (q[1] - p[1]) * 0.25))
        r.append((p[0] + (q[0] - p[0]) * 0.75, p[1] + (q[1] - p[1]) * 0.75))
    r[-1] = pts[n]
    return r


def main():
    w, h = 340, 220
    # el fondo de prueba: un paisaje ruidoso COLORIDO (para juzgar la
    # lectura del negro sobre cualquier fondo)
    rng = np.random.default_rng(7)
    fondo = np.zeros((h, w, 4), np.uint8)
    fondo[..., 0] = 90 + 80 * np.sin(np.arange(w)[None, :] * 0.11)
    fondo[..., 1] = 110 + 60 * np.sin(np.arange(h)[:, None] * 0.07)
    fondo[..., 2] = 140 + 70 * np.sin((np.arange(w)[None, :] + np.arange(h)[:, None]) * 0.05)
    fondo += rng.integers(0, 18, (h, w, 1)).astype(fondo.dtype)
    fondo[..., 3] = 255
    base = Image.fromarray(fondo, 'RGBA').convert('RGB')

    # el ojo en el origen y un monstruo de prueba en la punta
    ojo = Image.open('/home/z/my-project/download/Aethon-Mod-Terraria/'
                     'AethonMod/Content/Projectiles/OjoEterno_Abierto.png').convert('RGBA')
    iris = Image.open('/home/z/my-project/download/Aethon-Mod-Terraria/'
                      'AethonMod/Content/Weapons/GrimorioHambriento_Iris.png').convert('RGBA')
    a = (52, 96)     # el centro del ojo
    b = (285, 150)   # el monstruo

    # el canal (semilla del juego: whoAmI*31+5; whoAmI=3 → 98; flick 3)
    canal = canal_perlin(a, b, 3 * 31 + 5, 3)

    # === 1. LA FUNDA MORADA (soft-add: dst += rgb·(1−dst)) ===
    # aproximación PIL: capa aditiva — la gaussiana de RayoStrip
    # (ColF ±1/±0.42/0 × 1.55w con alfa 0.03/0.10/0.24 + la vena 0.85)
    morado = (148, 72, 224)
    claro = (232, 214, 255)
    funda = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    dr = ImageDraw.Draw(funda)
    # la vena (el filo caliente ~5.8px) + la funda (~25px total)
    pasos = 30
    arcos = [0.0]
    for i in range(1, len(canal)):
        arcos.append(arcos[-1] + math.dist(canal[i - 1], canal[i]))
    total = arcos[-1]
    seg = 0
    s = 0.0
    while s <= total:
        while seg + 1 < len(canal) and arcos[seg + 1] < s:
            seg += 1
        tramo = arcos[seg + 1] - arcos[seg]
        f = (s - arcos[seg]) / tramo if tramo > 0.1 else 0.0
        px = canal[seg][0] + (canal[seg + 1][0] - canal[seg][0]) * f
        py = canal[seg][1] + (canal[seg + 1][1] - canal[seg][1]) * f
        k = s / max(1.0, total)
        # la FUNDA (SoftGlow morado ~25px)
        rr = 12.5 * (1 - 0.4 * k)
        dr.ellipse([px - rr, py - rr, px + rr, py + rr], fill=morado + (46,))
        # la VENA (el filo claro ~6px)
        rv = 3.2 * (1 - 0.5 * k)
        dr.ellipse([px - rv, py - rv, px + rv, py + rv], fill=claro + (210,))
        s += 4.0
    funda = funda.filter(ImageFilter.GaussianBlur(2.2))
    # soft-add manual
    fa = np.array(funda, float) / 255.0
    fb = np.array(base, float) / 255.0
    add = np.clip(fb + fa[..., :3] * fa[..., 3:] * (1 - fb), 0, 1)
    base = Image.fromarray((add * 255).astype(np.uint8), 'RGB')

    # === 2. EL NÚCLEO NEGRO (AlphaBlend: las cápsulas que oscurecen) ===
    negro = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    dn = ImageDraw.Draw(negro)
    s = 0.0
    seg = 0
    while s <= total:
        while seg + 1 < len(canal) and arcos[seg + 1] < s:
            seg += 1
        tramo = arcos[seg + 1] - arcos[seg]
        f = (s - arcos[seg]) / tramo if tramo > 0.1 else 0.0
        px = canal[seg][0] + (canal[seg + 1][0] - canal[seg][0]) * f
        py = canal[seg][1] + (canal[seg + 1][1] - canal[seg][1]) * f
        k = s / max(1.0, total)
        wcap = (11 - 6 * k) * 0.9
        dn.ellipse([px - wcap, py - wcap, px + wcap, py + wcap],
                   fill=(8, 3, 14, 200))
        s += 7.0
    negro = negro.filter(ImageFilter.GaussianBlur(1.5))
    base = Image.alpha_composite(base.convert('RGBA'), negro).convert('RGB')

    # === 3. EL OJO (con iris mirando al objetivo) y el monstruo ===
    hoja = base.convert('RGBA')
    hoja.alpha_composite(ojo.resize((88, 74)), (a[0] - 44, a[1] - 37))
    dirx, diry = b[0] - a[0], b[1] - a[1]
    nrm = math.hypot(dirx, diry)
    dirx, diry = dirx / nrm, diry / nrm
    mx, my = a[0] + dirx * 7.5, a[1] + diry * 3.5
    iris_s = iris.resize((17, 17))
    hoja.alpha_composite(iris_s, (int(mx - 8.5), int(my - 8.5)))
    dr2 = ImageDraw.Draw(hoja)
    dr2.ellipse([b[0] - 16, b[1] - 24, b[0] + 16, b[1] + 20], fill=(120, 40, 60, 255),
                outline=(60, 20, 30, 255), width=3)   # el monstruo de prueba
    # la bruma en la punta (el humo oscuro)
    bruma = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    db = ImageDraw.Draw(bruma)
    for i in range(7):
        ang = i / 7 * 6.28
        bx = b[0] + math.cos(ang) * 14
        by = b[1] - 8 + math.sin(ang) * 10
        rr = 13 + 5 * math.sin(i * 2.1)
        db.ellipse([bx - rr, by - rr, bx + rr, by + rr], fill=(18, 10, 28, 120))
        db.ellipse([bx - rr * 0.5, by - rr * 0.5, bx + rr * 0.5, by + rr * 0.5],
                   fill=(64, 42, 96, 90))
    bruma = bruma.filter(ImageFilter.GaussianBlur(3.5))
    hoja = Image.alpha_composite(hoja, bruma)
    hoja = hoja.resize((w * 2, h * 2), Image.LANCZOS)
    hoja.save(f'{OUT}/mock_rayo.png')
    print('mock:', f'{OUT}/mock_rayo.png', hoja.size)


if __name__ == '__main__':
    main()
