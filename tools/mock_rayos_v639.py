#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_rayos_v639.py — v6.39 — EL MOCK 1:1 DE LA REPARACIÓN.

Renderiza con PIL+numpy (blending aditivo SIMULADO EXACTO: aporte =
perfil.rgb × tinte.rgb — el alfa no entra, la semántica One/One de
BlendState.Additive) las recetas NUEVAS picadas de los .cs, junto a las
ANTIGUAS (v6.21/v6.33) reconstruidas de sus generadores, para el
ANTES/DESPUÉS del informe:

  PANEL 1 — EL RAYO (StrandImpl):
    A) ANTES: sub-segmentos 42 px, quads solapados subLen+w·2, la textura
       BoltCore v6.21 (grietas 16 px + nodos + serpenteo horneados a lo
       largo) estirada COMPLETA por sub-segmento, Tint antiguo (intensidad
       en el alfa → invisible: TODO a brillo máximo).
    B) AHORA: la banda uniforme + normal media + largo exacto proyectado +
       extensión de giro + crackle por punto interpolado + Tint
       premultiplicado (v6.39).

  PANEL 2 — LOS ARCOS DE LA HERIDA (PintaArco/StormArc):
    A) ANTES: SoftGlow RADIAL estirado (funde a 0 en los extremos de cada
       quad) + solapes sl+w·1.6/0.8/0.22 → franjas oscuras y cuentas.
    B) AHORA: BoltHalo/BoltCore bandas + largo exacto + extensión de giro.

  PANEL 3 — EL DESGARRO:
    A) ANTES: UN QUAD recto (la "fea línea recta").
    B) AHORA: CaminoDesgarro (el borde rasgado) + segmentos con solape
       adaptativo + perlas + RiftTaper PREMULTIPLICADAS + arcos CORRIENDO
       POR DENTRO del rasgado (PintaArco v6.39) + ramas.

Salidas en research/rayos_v639/: MOCK_RAYOS_v639.png (paneles 1-2) y
MOCK_DESGARRO_v639.png (panel 3).
"""

import os
import math
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.join(HERE, '..', 'AethonMod')
EFF = os.path.join(PROJ, 'Content', 'Effects')
OUT = os.path.join(HERE, '..', 'research', 'rayos_v639')


# =====================================================================
#  EL NÚCLEO DE ADITIVO SIMULADO (la semántica One/One EXACTA)
# =====================================================================

class Lienzo:
    """Un buffer de píxeles con blending aditivo real: aporte.rgb =
    textura.rgb × tinte.rgb (el ALFA NO ENTRA — BlendState.Additive)."""

    def __init__(self, w, h, fondo=(12, 10, 20)):
        self.w, self.h = w, h
        self.buf = np.zeros((h, w, 3), dtype=np.float64)
        self.buf[:, :] = fondo

    def quad(self, tex_perfil, center, size, rot_rad, color, f):
        """Dibuja un quad centrado/rotado con la textura de perfil (HxW
        floats 0..1) estirada a size px. El 'color' es (r,g,b) 0..255 y
        f la intensidad — PREMULTIPLICADO (v6.39): rgb·f."""
        if size[0] < 0.5 or size[1] < 0.5:
            return
        r, g, b = color
        cr, cg, cb = r * f, g * f, b * f
        tex8 = (np.clip(tex_perfil, 0, 1) * 255).astype(np.uint8)
        nw = max(1, int(round(size[0])))
        nh = max(1, int(round(size[1])))
        img = Image.fromarray(tex8).resize((nw, nh), Image.BILINEAR)
        deg = math.degrees(rot_rad)
        if abs(deg) > 0.01:
            img = img.rotate(deg, resample=Image.BILINEAR, expand=True)
        patch = np.asarray(img, dtype=np.float64) / 255.0
        ph, pw = patch.shape
        x0 = int(round(center[0] - pw / 2))
        y0 = int(round(center[1] - ph / 2))
        xa, ya = max(0, x0), max(0, y0)
        xb, yb = min(self.w, x0 + pw), min(self.h, y0 + ph)
        if xa >= xb or ya >= yb:
            return
        sub = patch[ya - y0:yb - y0, xa - x0:xb - x0]
        self.buf[ya:yb, xa:xb, 0] += sub * cr
        self.buf[ya:yb, xa:xb, 1] += sub * cg
        self.buf[ya:yb, xa:xb, 2] += sub * cb

    def alpha_quad(self, tex_rgba, center, size, rot_rad, rgba):
        """Un quad ALFA (NonPremultiplied): mezcla src.rgb·src.a."""
        if size[0] < 0.5 or size[1] < 0.5:
            return
        tex = np.asarray(tex_rgba, dtype=np.float64) / 255.0
        # estirar
        nw, nh = max(1, int(round(size[0]))), max(1, int(round(size[1])))
        im = Image.fromarray(tex).resize((nw, nh), Image.BILINEAR)
        deg = math.degrees(rot_rad)
        if abs(deg) > 0.01:
            im = im.rotate(deg, resample=Image.BILINEAR, expand=True)
        patch = np.asarray(im, dtype=np.float64)
        ph, pw = patch.shape[:2]
        x0 = int(round(center[0] - pw / 2))
        y0 = int(round(center[1] - ph / 2))
        xa, ya = max(0, x0), max(0, y0)
        xb, yb = min(self.w, x0 + pw), min(self.h, y0 + ph)
        if xa >= xb or ya >= yb:
            return
        sub = patch[ya - y0:yb - y0, xa - x0:xb - x0]
        src_rgb = sub[..., 0:3] * np.array(rgba[:3], dtype=np.float64) / 255.0
        a = sub[..., 3] * rgba[3]
        dst = self.buf[ya:yb, xa:xb]
        dst *= (1 - a[..., None])
        dst += src_rgb * a[..., None]

    def guardar(self, name, brillo=1.0):
        img = np.clip(self.buf * brillo, 0, 255).astype(np.uint8)
        os.makedirs(OUT, exist_ok=True)
        path = os.path.join(OUT, name)
        Image.fromarray(img).save(path)
        print(f"  {name} -> {path}")


# =====================================================================
#  LAS TEXTURAS (las NUEVAS reales del disco + la v6.21 reconstruida)
# =====================================================================

def cargar_perfil(nombre):
    img = Image.open(os.path.join(EFF, 'Procedural', nombre)).convert('RGBA')
    a = np.asarray(img, dtype=np.float64)
    return a[..., 0] / 255.0    # RGB = perfil (premultiplicado v6.39)

HALO = cargar_perfil('BoltHalo.png')      # banda gaussiana ancha UNIFORME
VENA = cargar_perfil('BoltCore.png')      # vena estrecha UNIFORME

# LA RADIAL (SoftGlow — para los GORROS y el ANTES de PintaArco: GlowOrb,
# el pincel de puntos de la casa).
SOFT = np.asarray(Image.open(os.path.join(EFF, 'GlowOrb.png')).convert('L'), dtype=np.float64) / 255.0

# LA BOLTCORE v6.21 RECONSTRUIDA (grietas + nodos + serpenteo horneados —
# el generador original, para el ANTES honesto).
def boltcore_v621():
    rng_state = [77110 & 0xFFFFFFFF]

    def nxt():
        rng_state[0] = (rng_state[0] + 0x6D2B79F5) & 0xFFFFFFFF
        t = rng_state[0]
        t = ((t ^ (t >> 15)) * t) & 0xFFFFFFFF
        t = (t ^ (t + (t << 7))) & 0xFFFFFFFF
        return ((t ^ (t >> 17)) & 0xFFFFFFFF) / 4294967296.0

    W, H = 256, 64
    walk = [0.0]
    for i in range(1, W):
        step = (nxt() - 0.5) * 0.055
        step -= walk[-1] * 0.18
        walk.append(np.clip(walk[-1] + step, -0.15, 0.15))
    walk = np.array(walk)
    axis = 0.5 + walk
    block = 16
    raw = np.array([0.45 + 0.85 * nxt() for _ in range(W // block + 1)])
    crackle = np.repeat(raw, block)[:W]
    kern = np.ones(5) / 5.0
    crackle = np.convolve(np.concatenate([crackle[:2], crackle, crackle[-2:]]),
                          kern, mode='same')[2:2 + W]
    nodes = np.zeros(W)
    pos = 14 + int(nxt() * 20)
    while pos < W - 6:
        amp = 0.35 + 0.75 * nxt()
        sigma = 4.5 + nxt() * 4.5
        x = np.arange(W)
        nodes += amp * np.exp(-((x - pos) / sigma) ** 2)
        pos += 34 + int(nxt() * 26)
    v = np.linspace(0.0, 1.0, H)[:, None]
    wfrac = 0.075 + 0.030 * (0.5 + 0.5 * np.sin(np.linspace(0, 1, W) * 2 * np.pi * 2.7))
    prof = np.exp(-((v - axis[None, :]) / 0.155) ** 2) ** 1.15
    arr = np.clip(prof * crackle[None, :] * (1.0 + nodes[None, :] * 0.9), 0, 1)
    return arr

CORE_V621 = boltcore_v621()


# =====================================================================
#  LOS HASHES DE LA CASA (idénticos a VFXCore.Hash01)
# =====================================================================

def hash01(seed, a, b):
    h = (seed * 374761393 + a * 668265263 + b * 1911520717) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFFFF) / 16777216.0


# =====================================================================
#  LA GEOMETRÍA (ZigPath y FractalPath — idénticos a StormLib)
# =====================================================================

def zig_path(start, end, seed, flick, segments=12, amp=24.0):
    delta = np.array(end) - np.array(start)
    ln = float(np.linalg.norm(delta))
    if ln < 0.001:
        return [np.array(start)] * (segments + 1)
    d = delta / ln
    n = np.array([-d[1], d[0]])
    amp_len = min(amp, ln * 0.30)
    pts = []
    for i in range(segments + 1):
        t = i / segments
        env = math.sin(t * math.pi) ** 0.8
        j = (hash01(seed, flick, i * 31 + 7) - 0.5) * 2 * amp_len * env
        q = (hash01(seed, flick, i * 13 + 3) - 0.5) * 2 * amp_len * 0.35 * env
        p = np.array(start) + d * (ln * t) + n * j + d * q
        pts.append(p)
    pts[0] = np.array(start)
    pts[segments] = np.array(end)
    return pts


def fractal_path(start, end, seed, flick, generations=5, chaos=0.15):
    ln = float(np.linalg.norm(np.array(end) - np.array(start)))
    if ln < 2:
        return [np.array(start), np.array(end)]
    pts = [np.array(start, dtype=float), np.array(end, dtype=float)]
    offset = ln * max(0.02, min(chaos, 0.30))
    for g in range(generations):
        count = len(pts)
        for i in range(count - 1):
            a = pts[i * 2]
            b = pts[i * 2 + 1]
            seg = b - a
            sl = float(np.linalg.norm(seg))
            mid = (a + b) * 0.5
            if sl > 0.01:
                n = np.array([-seg[1] / sl, seg[0] / sl])
                j = (hash01(seed, flick * 31 + g, i * 61 + 13) - 0.5) * 2
                mid = mid + n * (j * offset)
            pts.insert(i * 2 + 1, mid)
        offset *= 0.5
    return pts


# =====================================================================
#  EL CAMINO DEL DESGARRO (idéntico a RiftLib.CaminoDesgarro)
# =====================================================================

def camino_desgarro(origin, d, length, seed, time, jag_amp):
    if length < 8:
        return [np.array(origin), np.array(origin) + np.array(d) * length]
    points = max(10, min(26, int(length / 26) + 1))
    d = np.array(d, dtype=float)
    d = d / np.linalg.norm(d)
    perp = np.array([-d[1], d[0]])
    drift = 1 + 0.15 * math.sin(time * 1.9 + seed * 0.7)

    def jag(seed, k):
        return ((hash01(seed, k, 301) - 0.5) * 2 * jag_amp
                + (hash01(seed, k, 307) - 0.5) * 2 * jag_amp * 0.45
                + (hash01(seed, k, 311) - 0.5) * 2 * jag_amp * 0.22)

    pts = []
    for i in range(points):
        f = i / (points - 1)
        env = math.sin(f * math.pi) ** 0.7
        k = int(round(f * length / 26))
        p = np.array(origin) + d * (length * f) + perp * (jag(seed, k) * env * drift)
        pts.append(p)
    pts[0] = np.array(origin)
    pts[-1] = np.array(origin) + d * length
    return pts


def angulo_entre(d0, d1):
    n0, n1 = np.linalg.norm(d0), np.linalg.norm(d1)
    if n0 < 0.01 or n1 < 0.01:
        return 0.0
    c = np.clip(np.dot(d0 / n0, d1 / n1), -1, 1)
    return math.acos(c)


def ext_junta(w, giro):
    return min(max(w * 0.5 * math.tan(giro * 0.5) + 1.2, 1.2), w * 0.5 + 1.2)


# =====================================================================
#  PANEL 1 — EL RAYO: ANTES (v6.21) vs AHORA (v6.39)
# =====================================================================

def rayo_antes(L, start, end, seed, flick, width, halo_rgb, core_rgb):
    """La receta v6.21 MEDIDA: sub-segmentos 42 px, quads con
    subLen+w·2.0 (¡apilamiento!), BoltCore v6.21 estirada COMPLETA y el
    Tint antiguo: la intensidad en el ALFA → INVISIBLE en aditivo."""
    pts = zig_path(start, end, seed, flick)
    # Tint v6.21: rgb SIN escalar (la intensidad 0.30/0.85/1.0 vivía en el
    # alfa — que el lote aditivo ignora): TODO sale a brillo máximo.
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        seg = b - a
        ln = float(np.linalg.norm(seg))
        if ln < 0.35:
            continue
        m = max(1, int(math.ceil(ln / 42.0)))          # SubMax = 42
        for s in range(m):
            p0 = a + seg * (s / m)
            p1 = a + seg * ((s + 1) / m)
            sub = p1 - p0
            sl = float(np.linalg.norm(sub))
            if sl < 0.30:
                continue
            rot = math.atan2(sub[1], sub[0])
            mid = (p0 + p1) * 0.5
            L.quad(CORE_V621, mid, (sl + width * 2.0, width * 2.0), rot, halo_rgb, 0.30)
            L.quad(CORE_V621, mid, (sl + width * 1.0, width * 1.0), rot, halo_rgb, 0.85)
            L.quad(CORE_V621, mid, (sl + width * 0.45, width * 0.26), rot, core_rgb, 1.0)


def rayo_ahora(L, start, end, seed, flick, width, halo_rgb, core_rgb):
    """La receta v6.39 (StrandImpl): banda uniforme + normal media +
    largo exacto + extensión de giro + crackle por punto + Tint
    premultiplicado."""
    pts = zig_path(start, end, seed, flick)
    total = sum(float(np.linalg.norm(pts[i + 1] - pts[i])) for i in range(len(pts) - 1))
    arc = 0.0

    def brillo(i):
        return 0.66 + 0.34 * hash01(seed, flick, i * 41 + 17)

    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        seg = b - a
        ln = float(np.linalg.norm(seg))
        if ln < 0.35:
            arc += ln
            continue
        prevv = a - pts[i - 1] if i > 0 else seg
        nextt = pts[i + 2] - b if i < len(pts) - 2 else seg
        n0, n1 = np.linalg.norm(prevv), np.linalg.norm(nextt)
        avg = (prevv / max(n0, 1e-6) + nextt / max(n1, 1e-6))
        if np.linalg.norm(avg) < 0.03:
            avg = seg
        avg = avg / np.linalg.norm(avg)
        rot = math.atan2(avg[1], avg[0])
        mid = (a + b) * 0.5
        t_mid = (arc + ln * 0.5) / total
        # Taper Center:
        w = width * max(0.35, math.sin(t_mid * math.pi) ** 0.65)
        crackle = (brillo(i) + brillo(i + 1)) * 0.5
        largo = float(np.dot(seg, avg))
        if largo < 0.35:
            arc += ln
            continue
        largo += ext_junta(w, angulo_entre(prevv, seg)) + ext_junta(w, angulo_entre(seg, nextt))
        # Tres capas — la banda UNIFORME premultiplicada:
        L.quad(HALO, mid, (largo, w * 2.0), rot, halo_rgb, 0.30 * crackle)
        L.quad(VENA, mid, (largo, w * 1.0), rot, halo_rgb, 0.85 * crackle)
        L.quad(VENA, mid, (largo, w * 0.26), rot, core_rgb, 1.0)
        arc += ln
    # Los GORROS (puntos radiales — SoftGlow es el pincel CORRECTO aquí).
    for p in (pts[0], pts[-1]):
        L.quad(SOFT, p, (width * 3.2, width * 3.2), 0, halo_rgb, 0.42)
        L.quad(SOFT, p, (width * 1.8, width * 1.8), 0, core_rgb, 0.78)


# =====================================================================
#  PANEL 2 — LOS ARCOS (PintaArco): ANTES vs AHORA
# =====================================================================

def arco_antes(L, start, end, seed, slot, width, glow_rgb, mid_rgb, core_rgb):
    """La receta v6.33 MEDIDA: FractalPath + SoftGlow RADIAL estirado con
    solapes sl+w·1.6/0.8/0.22 (funde a 0 en cada extremo → franjas)."""
    pts = fractal_path(start, end, seed, slot)
    for i in range(1, len(pts) - 1):
        es_mid = (i & (i - 1)) == 0
        amp = 6.0 if es_mid else 3.0
        pts[i] = pts[i] + np.array([(hash01(seed, slot, i * 19 + 7) - 0.5) * 2 * amp,
                                    (hash01(seed, slot + 31, i * 23 + 3) - 0.5) * 2 * amp])
    total = sum(float(np.linalg.norm(pts[i + 1] - pts[i])) for i in range(len(pts) - 1))
    arc = 0.0
    for i in range(len(pts) - 1):
        seg = pts[i + 1] - pts[i]
        sl = float(np.linalg.norm(seg))
        if sl < 0.30:
            arc += sl
            continue
        prevv = pts[i] - pts[i - 1] if i > 0 else seg
        nextt = pts[i + 2] - pts[i + 1] if i < len(pts) - 2 else seg
        n0, n1 = np.linalg.norm(prevv), np.linalg.norm(nextt)
        avg = prevv / max(n0, 1e-6) + nextt / max(n1, 1e-6)
        if np.linalg.norm(avg) < 0.03:
            avg = seg
        avg /= np.linalg.norm(avg)
        rot = math.atan2(avg[1], avg[0])
        pos = (pts[i] + pts[i + 1]) * 0.5
        t_mid = (arc + sl * 0.5) / total
        w = width * min(max(math.sin(t_mid * math.pi) + 0.35, 0.3), 1.0)
        # EL ANTES: SoftGlow estirado + solapes compensadores.
        L.quad(SOFT, pos, (sl + w * 1.6, w * 1.6), rot, glow_rgb, 0.30)
        L.quad(SOFT, pos, (sl + w * 0.8, w * 0.8), rot, mid_rgb, 0.55)
        L.quad(SOFT, pos, (sl + w * 0.4, w * 0.22), rot, core_rgb, 0.90)
        arc += sl


def arco_ahora(L, start, end, seed, slot, width, glow_rgb, mid_rgb, core_rgb):
    """La receta v6.39 (PintaArco): bandas uniformes + largo exacto +
    extensión de giro."""
    pts = fractal_path(start, end, seed, slot)
    for i in range(1, len(pts) - 1):
        es_mid = (i & (i - 1)) == 0
        amp = 6.0 if es_mid else 3.0
        pts[i] = pts[i] + np.array([(hash01(seed, slot, i * 19 + 7) - 0.5) * 2 * amp,
                                    (hash01(seed, slot + 31, i * 23 + 3) - 0.5) * 2 * amp])
    total = sum(float(np.linalg.norm(pts[i + 1] - pts[i])) for i in range(len(pts) - 1))
    arc = 0.0
    for i in range(len(pts) - 1):
        seg = pts[i + 1] - pts[i]
        sl = float(np.linalg.norm(seg))
        if sl < 0.30:
            arc += sl
            continue
        prevv = pts[i] - pts[i - 1] if i > 0 else seg
        nextt = pts[i + 2] - pts[i + 1] if i < len(pts) - 2 else seg
        n0, n1 = np.linalg.norm(prevv), np.linalg.norm(nextt)
        avg = prevv / max(n0, 1e-6) + nextt / max(n1, 1e-6)
        if np.linalg.norm(avg) < 0.03:
            avg = seg
        avg /= np.linalg.norm(avg)
        rot = math.atan2(avg[1], avg[0])
        pos = (pts[i] + pts[i + 1]) * 0.5
        t_mid = (arc + sl * 0.5) / total
        w = width * min(max(math.sin(t_mid * math.pi) + 0.35, 0.3), 1.0)
        largo = float(np.dot(seg, avg))
        if largo < 0.30:
            arc += sl
            continue
        largo += ext_junta(w, angulo_entre(prevv, seg)) + ext_junta(w, angulo_entre(seg, nextt))
        L.quad(HALO, pos, (largo, w * 1.6), rot, glow_rgb, 0.30)
        L.quad(HALO, pos, (largo, w * 0.8), rot, mid_rgb, 0.55)
        L.quad(VENA, pos, (largo, w * 0.30), rot, core_rgb, 0.90)
        arc += sl


# =====================================================================
#  PANEL 3 — EL DESGARRO: ANTES (quad recto) vs AHORA (rasgado + arcos)
# =====================================================================

TAPER_VOID = np.asarray(Image.open(os.path.join(EFF, 'Procedural', 'RiftTaperVoid.png')).convert('RGBA'), dtype=np.float64) / 255.0
TAPER_VELO = np.asarray(Image.open(os.path.join(EFF, 'Procedural', 'RiftTaperVelo.png')).convert('RGBA'), dtype=np.float64) / 255.0
TAPER_CUERPO = np.asarray(Image.open(os.path.join(EFF, 'Procedural', 'RiftTaperCuerpo.png')).convert('RGBA'), dtype=np.float64) / 255.0
TAPER_NUCLEO = np.asarray(Image.open(os.path.join(EFF, 'Procedural', 'RiftTaperNucleo.png')).convert('RGBA'), dtype=np.float64) / 255.0


def taper_quad_alpha(L, tex, center, largo, alto, rot, rgba, estadio=False):
    """El TaperQuad de la casa: recorte a la meseta (u∈[0.30,0.70]) salvo
    estadio completo; ALFA blend."""
    h, w = tex.shape[0], tex.shape[1]
    if estadio:
        src = tex
    else:
        x0, x1 = int(w * 0.3), int(w * 0.7)
        src = tex[:, x0:x1]
    sh, sw = src.shape[:2]
    nw, nh = max(1, int(round(largo))), max(1, int(round(alto)))
    im = Image.fromarray((src * 255).astype(np.uint8), 'RGBA').resize((nw, nh), Image.BILINEAR)
    deg = math.degrees(rot)
    if abs(deg) > 0.01:
        im = im.rotate(deg, resample=Image.BILINEAR, expand=True)
    patch = np.asarray(im, dtype=np.float64)
    ph, pw = patch.shape[:2]
    x0 = int(round(center[0] - pw / 2))
    y0 = int(round(center[1] - ph / 2))
    xa, ya = max(0, x0), max(0, y0)
    xb, yb = min(L.w, x0 + pw), min(L.h, y0 + ph)
    if xa >= xb or ya >= yb:
        return
    sub = patch[ya - y0:yb - y0, xa - x0:xb - x0]
    # rgba = (r,g,b,a) 0..255 — el tinte de la casa.
    src_rgb = sub[..., 0:3] * (np.array(rgba[:3], dtype=np.float64) / 255.0)
    a = sub[..., 3] * rgba[3]
    # Nota: RGB ya viene PREMULTIPLICADO (v6.39) — el lote alfa añade
    # rgb·alfa: mismo resultado visual que el mock v6.31 validó.
    dst = L.buf[ya:yb, xa:xb]
    dst *= (1 - a[..., None])
    dst += src_rgb * a[..., None]


def desgarro_antes(L, origin, d, length, max_width, seed, time, paleta):
    """v6.31: UN SOLO QUAD recto de punta a punta (la 'fea línea recta')."""
    h = 1.60 * max_width
    rot = math.atan2(d[1], d[0])
    center = np.array(origin) + np.array(d) * (length * 0.5)
    breathe = 1 + 0.06 * math.sin(time * 2.2 + seed)
    # Void
    tex = TAPER_VOID
    im = Image.fromarray((tex * 255).astype(np.uint8), 'RGBA').resize(
        (int(length), int(h)), Image.BILINEAR)
    deg = math.degrees(rot)
    im = im.rotate(deg, resample=Image.BILINEAR, expand=True)
    patch = np.asarray(im, dtype=np.float64)
    ph, pw = patch.shape[:2]
    x0 = int(round(center[0] - pw / 2)); y0 = int(round(center[1] - ph / 2))
    xa, ya = max(0, x0), max(0, y0); xb, yb = min(L.w, x0 + pw), min(L.h, y0 + ph)
    sub = patch[ya - y0:yb - y0, xa - x0:xb - x0]
    a = sub[..., 3] * (0.96 * breathe)
    dst = L.buf[ya:yb, xa:xb]
    dst *= (1 - a[..., None])
    dst += sub[..., 0:3] * a[..., None]
    # Luz (los TRES quads — RGB premultiplicado × tinte):
    for tex, rgb, f in ((TAPER_VELO, paleta[0], 0.30), (TAPER_CUERPO, paleta[1], 0.60),
                        (TAPER_NUCLEO, paleta[-1], 0.90)):
        L.quad(tex[..., 0], center, (length, h), rot, rgb, f)


def desgarro_ahora(L, origin, d, length, max_width, seed, time, paleta):
    """v6.39: el desgarro RASGADO + los arcos CORRIENDO POR DENTRO."""
    jag = min(8.0, max(2.5, max_width * 0.32))
    camino = camino_desgarro(origin, d, length, seed, time, jag)
    h = 1.60 * max_width
    d = np.array(d, dtype=float); d /= np.linalg.norm(d)
    perp = np.array([-d[1], d[0]])

    def giro_en(k):
        d0 = camino[k] - camino[k - 1]
        d1 = camino[k + 1] - camino[k]
        return angulo_entre(d0, d1)

    # VOID por segmento + perlas.
    for i in range(len(camino) - 1):
        a, b = camino[i], camino[i + 1]
        ln = float(np.linalg.norm(b - a))
        if ln < 0.30:
            continue
        rot = math.atan2(b[1] - a[0] if False else (b - a)[1], (b - a)[0])
        ga = giro_en(i) if i > 0 else 0.0
        gb = giro_en(i + 1) if i < len(camino) - 2 else 0.0
        ext = lambda g: min(max(h * 0.5 * math.tan(g * 0.5) + 0.75, 0.75), h * 0.5)
        largo = ln + ext(ga) + ext(gb)
        extremo = i == 0 or i == len(camino) - 2
        breathe = 1 + 0.06 * math.sin(time * 2.2 + seed)
        taper_quad_alpha(L, TAPER_VOID, (a + b) / 2, largo, h, rot,
                         (255, 255, 255, 0.96 * breathe), extremo)
        if i > 0:
            taper_quad_alpha(L, TAPER_VOID, a, h * 0.75, h, rot,
                             (255, 255, 255, 0.96 * breathe))
    # LA LUZ por segmento (velo/cuerpo/núcleo — aditivo, RGB=perfil).
    for i in range(len(camino) - 1):
        a, b = camino[i], camino[i + 1]
        ln = float(np.linalg.norm(b - a))
        if ln < 0.30:
            continue
        seg = b - a
        rot = math.atan2(seg[1], seg[0])
        ga = giro_en(i) if i > 0 else 0.0
        gb = giro_en(i + 1) if i < len(camino) - 2 else 0.0
        ext = lambda g: min(max(h * 0.5 * math.tan(g * 0.5), 0.0), h * 0.5)
        largo = ln + ext(ga) + ext(gb)
        extremo = i == 0 or i == len(camino) - 2
        mid = (a + b) / 2
        for tex, rgb, f in ((TAPER_VELO, paleta[0], 0.30), (TAPER_CUERPO, paleta[1], 0.60),
                            (TAPER_NUCLEO, paleta[-1], 0.90)):
            # (estadio en los extremos: simplificado a meseta en el mock)
            L.quad(tex[:, int(tex.shape[1]*0.3):int(tex.shape[1]*0.7), 0],
                   mid, (largo, h), rot, rgb, f)
        if ga > 0.14:      # LA PERLA solo con giro real
            for tex, rgb, f in ((TAPER_VELO, paleta[0], 0.30), (TAPER_CUERPO, paleta[1], 0.60),
                                (TAPER_NUCLEO, paleta[-1], 0.90)):
                L.quad(tex[:, int(tex.shape[1]*0.3):int(tex.shape[1]*0.7), 0],
                       a, (h * 0.75, h), rot, rgb, f)
    # LOS ARCOS CORRIENDO POR DENTRO del rasgado (PintaArco v6.39 ×3).
    cian = (0, 229, 255)
    violeta = (120, 130, 255)
    blanco = (240, 250, 255)
    glow = (30, 80, 168)
    mid = (94, 179, 255)
    for t in range(3):
        i0 = max(1, min(len(camino) - 4, 2 + t * 5 + (seed % 3)))
        i1 = min(i0 + 4, len(camino) - 1)
        a0 = camino[i0] + perp * (max_width * 0.10 * (hash01(seed, t, 157) - 0.5) * 2)
        a1 = camino[i1] - perp * (max_width * 0.10 * (hash01(seed, t, 163) - 0.5) * 2)
        arco_ahora(L, tuple(a0), tuple(a1), seed + t * 31, 7 + t, 3.5, glow, mid, blanco)
    # LAS RAMAS (Bolt v6.39 desde el camino).
    for r in range(5):
        h1 = hash01(seed, r, 173)
        h2 = hash01(seed, r, 179)
        h3 = hash01(seed, r, 181)
        if h1 < 0.30:
            continue
        idx = (0.12 + 0.76 * h2) * (len(camino) - 1)
        i0 = max(1, min(len(camino) - 2, int(idx)))
        nace = camino[i0]
        segl = camino[min(i0 + 1, len(camino) - 1)] - camino[i0]
        rot_local = math.atan2(segl[1], segl[0]) if np.linalg.norm(segl) > 0.01 else math.atan2(d[1], d[0])
        lado = 1 if h3 > 0.5 else -1
        ang = rot_local + lado * (0.96 + 0.35 * h1)
        largo_r = length * (0.10 + 0.13 * h2)
        fin = nace + np.array([math.cos(ang), math.sin(ang)]) * largo_r
        rayo_ahora(L, tuple(nace), tuple(fin), seed + 601 + r * 43, 5, 4.5, violeta, blanco)


# =====================================================================
#  LA COMPOSICIÓN
# =====================================================================

if __name__ == '__main__':
    print("MOCK v6.39 — la reparación de los rayos y los desgarros...")

    # ---- PANEL 1+2: EL RAYO Y LOS ARCOS (antes/después) ----
    L = Lienzo(860, 560, fondo=(10, 12, 24))
    violeta = (110, 90, 255)
    cian = (90, 190, 255)
    blanco = (240, 250, 255)

    # ANTES (fila 1): el rayo v6.21 con su textura modulada y sus solapes.
    rayo_antes(L, (60, 70), (800, 70), 331, 12, 9.0, violeta, blanco)
    arco_antes(L, (60, 190), (800, 190), 913, 7, 5.0, (30, 80, 168), (94, 179, 255), blanco)
    # AHORA (fila 2): la receta v6.39.
    rayo_ahora(L, (60, 340), (800, 340), 331, 12, 9.0, violeta, blanco)
    arco_ahora(L, (60, 460), (800, 460), 913, 7, 5.0, (30, 80, 168), (94, 179, 255), blanco)
    L.guardar('MOCK_RAYOS_v639.png')

    # ---- PANEL 3: EL DESGARRO (antes/después) ----
    L2 = Lienzo(860, 640, fondo=(18, 8, 24))
    paleta = [(150, 80, 255), (80, 200, 255), (235, 245, 255)]
    # ANTES: la línea recta pura (v6.31).
    desgarro_antes(L2, (70, 120), (1, 0), 720, 18, 77, 3.1, paleta)
    # AHORA: el borde RASGADO con arcos dentro (v6.39).
    desgarro_ahora(L2, (70, 430), (1, 0), 720, 18, 77, 3.1, paleta)
    L2.guardar('MOCK_DESGARRO_v639.png', brillo=1.25)

    print("OK — mocks listos en research/rayos_v639/")
