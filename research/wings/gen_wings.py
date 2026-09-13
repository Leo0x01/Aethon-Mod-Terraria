#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_wings.py — v6.06 (v2) — GENERADOR PROCEDURAL DE ALAS AETHON.

8 spritesheets vanilla (tira vertical de 4 frames) + 10 iconos + 2 blanks.

v2 — mejoras tras la primera validación VLM:
  - AUTO-ENCAJE: cada frame se recorta y recentra tras renderizar (ancla
    del ala a FW/2, FH/2+6) → imposible recortar bordes, centrado perfecto.
  - SIMETRÍA ESPEJO TOTAL: la misma semilla rng para ambos lados.
  - Frame plegado (0) con SQUASH horizontal 0.66 (se mete hacia la espalda)
    y tilt abajo; aleteos arriba/abajo mucho más diferenciados.
  - Plumas/cristales con PRE-PASE de contorno oscuro (separación nítida).
  - Membrana rediseñada: brazo de guía + abanico correcto (arriba→abajo).
  - Eclipse rediseñado: rayos LARGOS en abanico + arco de limbo = silueta
    de ala; discos negros más pequeños como acento.
  - Nebulosa alargada (5 lóbulos), núcleo reducido, wisps fuera.
  - Glacial con más contraste y carámbanos nítidos.
"""

import math
import os

import numpy as np
from PIL import Image, ImageDraw
from scipy.ndimage import binary_dilation

HERE = os.path.dirname(os.path.abspath(__file__))
MOD_ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "AethonMod"))
OUT_DIR = os.path.join(MOD_ROOT, "Content", "Items", "Wings")

# Lienzo de TRABAJO generoso (se auto-recorta después por estilo)
FW, FH = 200, 148
SS = 4
CX, CY = FW // 2, FH // 2
GAP = 11

# Los 4 estados vanilla: apertura, squash de envergadura, dy, tilt (rad)
FRAMES = [
    dict(o=0.40, sq=0.56, dy=+9, tilt=+0.18),   # 0 — plegadas (metidas en la espalda)
    dict(o=0.88, sq=0.98, dy=-10, tilt=-0.13),  # 1 — aleteo arriba (elevación marcada)
    dict(o=1.00, sq=1.00, dy=+1, tilt=+0.00),   # 2 — extendidas
    dict(o=0.80, sq=0.90, dy=+9, tilt=+0.14),   # 3 — aleteo abajo
]

ANCHOR_DY = 6   # el ancla del ala cae 6px DEBAJO del centro del frame final


def canvas_ss():
    return np.zeros((FH * SS, FW * SS, 4), dtype=np.float64)


def ellipse_ddf(c, cx, cy, rx, ry, rot, color, alpha=1.0, edge=0.16):
    H, W = c.shape[:2]
    cx, cy = cx * SS, cy * SS
    rx, ry = max(rx * SS, 0.6), max(ry * SS, 0.6)
    pad = int(max(rx, ry) + SS * 2)
    x0, x1 = max(0, int(cx - pad)), min(W, int(cx + pad + 1))
    y0, y1 = max(0, int(cy - pad)), min(H, int(cy + pad + 1))
    if x1 <= x0 or y1 <= y0:
        return
    ys, xs = np.mgrid[y0:y1, x0:x1]
    dx = (xs - cx) / max(rx, 1e-6)
    dy = (ys - cy) / max(ry, 1e-6)
    if abs(rot) > 1e-4:
        cr, sr = math.cos(rot), math.sin(rot)
        rdx = dx * cr + dy * sr
        rdy = -dx * sr + dy * cr
    else:
        rdx, rdy = dx, dy
    d = np.sqrt(rdx * rdx + rdy * rdy)
    soft = max(edge, 1.0 / max(rx, ry) * 1.5)
    a = np.clip((1.0 - d) / soft + 1.0, 0.0, 1.0)
    a = np.clip((a - 0.5) * 2.0, 0.0, 1.0)
    m = a > 0.02
    if not m.any():
        return
    aa = a[m] * float(alpha)
    blk = c[y0:y1, x0:x1]
    cur = blk[m]
    dst_a = cur[:, 3] / 255.0
    out_a = np.clip(aa + dst_a * (1 - aa), 0.0, 1.0)
    src = np.array(color, dtype=np.float64)
    cur[:, 0] = (src[0] * aa + cur[:, 0] * dst_a * (1 - aa)) / np.maximum(out_a, 1e-6)
    cur[:, 1] = (src[1] * aa + cur[:, 1] * dst_a * (1 - aa)) / np.maximum(out_a, 1e-6)
    cur[:, 2] = (src[2] * aa + cur[:, 2] * dst_a * (1 - aa)) / np.maximum(out_a, 1e-6)
    cur[:, 3] = out_a * 255.0
    blk[m] = cur


def poly_mask(pts):
    img = Image.new("L", (FW * SS, FH * SS), 0)
    dr = ImageDraw.Draw(img)
    dr.polygon([(x * SS, y * SS) for (x, y) in pts], fill=255)
    return np.asarray(img, dtype=np.float64) > 0.5


def blit_poly(c, pts, color, alpha=1.0):
    m = poly_mask(pts)
    if not m.any():
        return
    a = float(alpha)
    src = np.array(color, dtype=np.float64)
    cur = c[m]
    dst_a = cur[:, 3] / 255.0
    out_a = np.clip(a + dst_a * (1 - a), 0.0, 1.0)
    cur[:, 0] = (src[0] * a + cur[:, 0] * dst_a * (1 - a)) / np.maximum(out_a, 1e-6)
    cur[:, 1] = (src[1] * a + cur[:, 1] * dst_a * (1 - a)) / np.maximum(out_a, 1e-6)
    cur[:, 2] = (src[2] * a + cur[:, 2] * dst_a * (1 - a)) / np.maximum(out_a, 1e-6)
    cur[:, 3] = out_a * 255.0
    c[m] = cur


def line_beads(c, p0, p1, color, alpha, w0, w1, n=14):
    x0, y0 = p0
    x1, y1 = p1
    for i in range(n):
        t = i / (n - 1)
        cx = x0 + (x1 - x0) * t
        cy = y0 + (y1 - y0) * t
        w = w0 + (w1 - w0) * t
        ellipse_ddf(c, cx, cy, w, w, 0.0, color, alpha, edge=0.10)


def bezier(p0, p1, p2, n):
    pts = []
    for i in range(n):
        t = i / (n - 1)
        x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0]
        y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]
        pts.append((x, y))
    return pts


def pixelize(small, outline_color=None, glow=False, light_top=0.20):
    a = small[:, :, 3] / 255.0
    rgb = small[:, :, :3].copy()
    if light_top > 0:
        h = a.shape[0]
        yy = np.linspace(1.0, -1.0, h)[:, None]
        rgb *= (1.0 + light_top * 0.5 * yy)[:, :, None]
    rgb = np.clip(rgb, 0, 255)
    out = np.dstack([rgb, a * 255.0])
    if not glow and outline_color is not None:
        solid = a > 0.55
        edge = (a > 0.06) & ~solid
        vacio = ~solid
        struct = np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]], bool)
        borde_solid = solid & binary_dilation(vacio, structure=struct)
        oc = np.array(outline_color, dtype=np.float64)
        out[:, :, :3][edge | borde_solid] = oc
        out[:, :, 3][edge | borde_solid] = 255.0
        out[:, :, 3][a <= 0.06] = 0.0
    return out


def frame_to_pil(out):
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))


def downsample(c):
    h, w = FH, FW
    return c.reshape(h, SS, w, SS, 4).mean(axis=(1, 3))


# ============================================================================
#  ESTILOS
# ============================================================================

def draw_feathered(c, style, o, dy, tilt, pal):
    """Plumas en abanico con PRE-PASE de contorno oscuro (separación nítida)."""
    n = style.get("n", 6)
    ang0 = math.radians(style.get("ang0", 56))
    ang1 = math.radians(style.get("ang1", 8))
    maxL = style.get("maxL", 80.0)
    maxW = style.get("maxW", 10.0)
    len0 = style.get("len0", 0.62)
    sq = style.get("sq", 1.0)
    for side in (-1, 1):
        sx, sy = CX + side * GAP, CY + dy
        for k in range(n):
            frac = k / (n - 1)
            ang = ang0 + (ang1 - ang0) * frac + tilt * (1.0 - frac) * 0.9
            L = maxL * (len0 + (1 - len0) * frac) * o * sq
            W = maxW * (0.8 + 0.45 * frac)
            d = (side * math.cos(ang), -math.sin(ang))
            mx, my = sx + d[0] * (L * 0.5 + 2), sy + d[1] * (L * 0.5 + 2)
            rot = math.atan2(d[1], d[0])
            t = 0.5 + 0.5 * d[1]
            col = tuple(np.array(pal["hi"]) * (1 - t * 0.55) + np.array(pal["mid"]) * (t * 0.55))
            # pre-pase de contorno (separación entre plumas)
            ellipse_ddf(c, mx, my, L * 0.52 + 1.4, W + 1.2, rot, pal["deep"], 1.0, edge=0.07)
            ellipse_ddf(c, mx, my, L * 0.52, W, rot, col, 1.0, edge=0.07)
            if k < n - 1:
                cvx = sx + d[0] * L * 0.22
                cvy = sy + d[1] * L * 0.22 - 2
                ellipse_ddf(c, cvx, cvy, L * 0.18 + 1, W * 0.72 + 1, rot, pal["deep"], 1.0, edge=0.07)
                ellipse_ddf(c, cvx, cvy, L * 0.18, W * 0.72, rot, tuple(np.array(pal["hi"]) * 0.94), 1.0, edge=0.07)
        ellipse_ddf(c, sx, sy - 2, 13 * o, 8 * o, 0.0, pal["deep"], 1.0, edge=0.10)
        ellipse_ddf(c, sx, sy - 2, 11.5 * o, 6.8 * o, 0.0, pal["hi"], 1.0, edge=0.10)


def detail_fossil(c, o, dy, sq, pal):
    H = c.shape[0]
    band = (np.arange(H) % (5 * SS)) < SS
    m = c[:, :, 3] > 40
    c[:, :, :3][m & band[:, None]] *= 0.86
    for side in (-1, 1):
        sx, sy = CX + side * GAP, CY + dy
        for adeg in (50, 32, 15):
            a = math.radians(adeg)
            d = (side * math.cos(a), -math.sin(a))
            line_beads(c, (sx + d[0] * 8, sy + d[1] * 8 - 2),
                       (sx + d[0] * 52 * o * sq, sy + d[1] * 52 * o * sq - 2),
                       pal["vein"], 0.95, 1.3, 0.7, n=9)
        ellipse_ddf(c, sx, sy - 3, 5.0, 5.0, 0.0, pal["shard"], 1.0, edge=0.10)


def detail_glacial(c, o, dy, sq, pal):
    for side in (-1, 1):
        sx, sy = CX + side * GAP, CY + dy
        for k, adeg in enumerate((50, 38, 26, 14)):
            a = math.radians(adeg)
            d = (side * math.cos(a), -math.sin(a))
            L = 66 * o * sq * (0.7 + 0.3 * k / 3)
            line_beads(c, (sx + d[0] * L * 0.6, sy + d[1] * L * 0.6),
                       (sx + d[0] * L, sy + d[1] * L),
                       pal["core"], 0.95, 3.4, 1.0, n=8)


def draw_membrane(c, style, o, dy, tilt, pal):
    """Membrana demoníaca: BRAZO de guía + abanico correcto (arriba→abajo)."""
    fingers = style.get("fingers", 4)
    sq = style.get("sq", 1.0)
    for side in (-1, 1):
        sx, sy = CX + side * GAP, CY + dy
        # brazo de guía (codo) — el borde de ataque del ala
        elbow = (sx + side * 30 * o * sq, sy - 26 * o)
        line_beads(c, (sx, sy - 4), elbow, pal["bone_hi"], 1.0, 4.6, 3.2, n=8)
        tips = []
        for k in range(fingers):
            frac = k / (fingers - 1)
            ang = math.radians(56 - 64 * frac + tilt * 36 * (1 - frac))
            L = style.get("maxL", 84) * (0.52 + 0.48 * (1 - abs(frac - 0.42) * 1.35)) * o * sq
            d = (side * math.cos(ang), -math.sin(ang))
            # los dedos nacen a lo largo del brazo (0 en el codo, 1 en el hombro)
            tb = 0.85 * (1 - frac * 0.55)
            bx = sx + (elbow[0] - sx) * tb
            by = sy - 4 + (elbow[1] - sy + 4) * tb
            tip = (bx + d[0] * L * 0.78, by + d[1] * L * 0.78)
            tips.append(tip)
            line_beads(c, (bx, by), tip, pal["bone"], 1.0, 3.2, 1.3, n=11)
        # membrana festoneada entre puntas
        pts = [(sx - side * 2, sy - 6)]
        for i in range(len(tips) - 1):
            t0, t1 = tips[i], tips[i + 1]
            mid = ((t0[0] + t1[0]) * 0.5, (t1[1] + t0[1]) * 0.5)
            pull = 0.17
            anchor = (sx + side * 8, sy - 4)
            q = (mid[0] + (anchor[0] - mid[0]) * pull, mid[1] + (anchor[1] - mid[1]) * pull)
            pts += bezier(t0, q, t1, 10)
        pts += [tips[-1], (sx + side * 10, sy + 16 * o), (sx - side * 4, sy + 2)]
        blit_poly(c, pts, pal["membrane"], style.get("alpha", 0.86))
        # luz del borde superior (rim) para separar del fondo
        rim = [(px, py - 1.5) for (px, py) in bezier((sx, sy - 5), elbow, tips[0], 12)]
        for j in range(len(rim) - 1):
            line_beads(c, rim[j], rim[j + 1], pal["rim"], 0.9, 1.6, 1.0, n=3)
        # venas fucsia prolongadas sobre la membrana (simétricas)
        for k in range(fingers - 1):
            frac = k / (fingers - 1)
            ang = math.radians(56 - 64 * ((frac + 0.5) / fingers))
            d = (side * math.cos(ang), -math.sin(ang))
            L = style.get("maxL", 84) * 0.66 * o * sq
            line_beads(c, (sx + d[0] * 10, sy + d[1] * 10 - 6),
                       (sx + d[0] * L, sy + d[1] * L), pal["vein"], 0.9, 1.6, 0.5, n=10)
        # estrellas internas (MISMA semilla ambos lados → simetría espejo)
        rng = np.random.default_rng(77)
        for i in range(6):
            fx = rng.uniform(0.25, 0.9)
            fy = rng.uniform(0.1, 0.62)
            px = sx + side * style.get("maxL", 84) * o * sq * fx
            py = sy - 10 - 52 * fy * o
            ellipse_ddf(c, px, py, 0.9, 0.9, 0.0, pal["star"], 0.95, edge=0.05)


def draw_flame(c, style, o, dy, tilt, pal):
    """Fuego: lenguadas por espinas bezier — rng SIMÉTRICO por lado."""
    licks = style.get("licks", 5)
    sq = style.get("sq", 1.0)
    rng = np.random.default_rng(31)          # MISMA semilla: simetría espejo
    wobs = [rng.uniform(-1.5, 1.5) for _ in range(licks * 16)]
    for side in (-1, 1):
        sx, sy = CX + side * (GAP - 2), CY + dy + 6
        ellipse_ddf(c, sx, sy - 2, 10 * o, 8 * o, 0.0, pal["core"], 1.0, edge=0.25)
        ellipse_ddf(c, sx, sy - 2, 6.5 * o, 5.2 * o, 0.0, pal["hot"], 1.0, edge=0.25)
        for k in range(licks):
            frac = k / (licks - 1)
            ang = math.radians(46 - 52 * frac + tilt * 30 * (1 - frac))
            L = style.get("maxL", 82) * (0.60 + 0.40 * (1 - abs(frac - 0.55))) * o * sq
            d = (side * math.cos(ang), -math.sin(ang))
            p0 = (sx + d[0] * 4, sy + d[1] * 4)
            p2 = (sx + d[0] * L, sy + d[1] * L - 5 - 7 * (1 - frac))
            p1 = ((p0[0] + p2[0]) * 0.5 - side * 5, (p0[1] + p2[1]) * 0.5 - 7)
            spine = bezier(p0, p1, p2, 16)
            for i, (bx, by) in enumerate(spine):
                t = i / (len(spine) - 1)
                w = style.get("w0", 9.0) * (1.0 - t * 0.78) * o
                wob = wobs[k * 16 + i]
                if t < 0.22:
                    col, al = pal["hot"], 1.0
                elif t < 0.55:
                    tt = (t - 0.22) / 0.33
                    col = tuple(np.array(pal["hot"]) * (1 - tt) + np.array(pal["mid"]) * tt)
                    al = 0.95
                else:
                    tt = (t - 0.55) / 0.45
                    col = tuple(np.array(pal["mid"]) * (1 - tt) + np.array(pal["deep"]) * tt)
                    al = 0.95 - 0.30 * tt
                ellipse_ddf(c, bx + wob * (1 - t) * side, by, w, w * 0.86, 0.0, col, al, edge=0.30)
        for k in range(4):
            ang = math.radians(40 - 60 * k / 3)
            d = (side * math.cos(ang), -math.sin(ang))
            px = sx + d[0] * style.get("maxL", 82) * o * sq
            py = sy + d[1] * style.get("maxL", 82) * o * sq - 6
            ellipse_ddf(c, px, py, 1.2, 1.2, 0.0, pal["core"], 0.9, edge=0.05)


def draw_crystal(c, style, o, dy, tilt, pal):
    """Cristal: shards facetados con pre-pase de contorno — rng simétrico."""
    shards = style.get("shards", 5)
    sq = style.get("sq", 1.0)
    rng = np.random.default_rng(97)
    sparks = [(rng.uniform(0.5, 0.9), rng.uniform(0.2, 0.75), rng.uniform()) for _ in range(3)]
    for side in (-1, 1):
        sx, sy = CX + side * GAP, CY + dy
        for k in range(shards):
            frac = k / (shards - 1)
            ang = math.radians(54 - 66 * frac + tilt * 38 * (1 - frac))
            L = style.get("maxL", 82) * (0.55 + 0.45 * (1 - abs(frac - 0.42) * 1.3)) * o * sq
            d = (side * math.cos(ang), -math.sin(ang))
            perp = (-d[1], d[0])
            base = (sx + d[0] * 3, sy + d[1] * 3 - 2)
            tip = (sx + d[0] * L, sy + d[1] * L)
            w = style.get("maxW", 8.5) * (0.85 + 0.3 * (1 - frac))
            m1 = (base[0] + perp[0] * w + d[0] * L * 0.42, base[1] + perp[1] * w + d[1] * L * 0.42)
            m2 = (base[0] - perp[0] * w + d[0] * L * 0.42, base[1] - perp[1] * w + d[1] * L * 0.42)
            blit_poly(c, [base, m1, tip, m2], pal["deep"], 1.0)
            mw = w * 0.8
            mm1 = (base[0] + perp[0] * mw + d[0] * L * 0.42, base[1] + perp[1] * mw + d[1] * L * 0.42)
            mm2 = (base[0] - perp[0] * mw + d[0] * L * 0.42, base[1] - perp[1] * mw + d[1] * L * 0.42)
            blit_poly(c, [base, mm1, tip, mm2], pal["mid"], 1.0)
            f1 = (base[0] + perp[0] * w * 0.15 + d[0] * L * 0.20, base[1] + perp[1] * w * 0.15 + d[1] * L * 0.20)
            blit_poly(c, [f1, mm1, tip], pal["hi"], 0.9)
            f2 = (base[0] - perp[0] * w * 0.15 + d[0] * L * 0.20, base[1] - perp[1] * w * 0.15 + d[1] * L * 0.20)
            blit_poly(c, [f2, mm2, tip], pal["deep"], 0.85)
            line_beads(c, base, tip, pal["core"], 0.9, 1.2, 0.4, n=10)
        ellipse_ddf(c, sx, sy - 2, 9 * o, 7.5 * o, 0.0, pal["core"], 1.0, edge=0.2)
        ellipse_ddf(c, sx, sy - 2, 5.5 * o, 4.8 * o, 0.0, pal["hi"], 1.0, edge=0.2)
        for (fx, fy, fa) in sparks:
            ang = math.radians(50 - 75 * fa)
            d = (side * math.cos(ang), -math.sin(ang))
            px = sx + d[0] * style.get("maxL", 82) * o * sq * fx
            py = sy + d[1] * 62 * o * sq * fy
            s = 2.4
            ellipse_ddf(c, px, py, s, s * 0.35, 0.0, pal["core"], 1.0, edge=0.1)
            ellipse_ddf(c, px, py, s * 0.35, s, 0.0, pal["core"], 1.0, edge=0.1)


def draw_nebula(c, style, o, dy, tilt, pal):
    """Nebulosa: 5 lóbulos alargados translúcidos + borde cian brillante."""
    lobes = style.get("lobes", 5)
    sq = style.get("sq", 1.0)
    for side in (-1, 1):
        sx, sy = CX + side * GAP, CY + dy
        lobe_tips = []
        for k in range(lobes):
            frac = k / (lobes - 1)
            ang = math.radians(54 - 70 * frac + tilt * 34 * (1 - frac))
            L = style.get("maxL", 82) * (0.60 + 0.40 * (1 - abs(frac - 0.45) * 1.25)) * o * sq
            d = (side * math.cos(ang), -math.sin(ang))
            lobe_tips.append((sx + d[0] * L, sy + d[1] * L))
        pts = [(sx, sy - 5)]
        for i in range(len(lobe_tips) - 1):
            t0, t1 = lobe_tips[i], lobe_tips[i + 1]
            mid = ((t0[0] + t1[0]) * 0.5, (t0[1] + t1[1]) * 0.5)
            pull = 0.24
            q = (mid[0] + (sx - mid[0]) * pull, mid[1] + (sy - mid[1]) * pull)
            pts += bezier(t0, q, t1, 12)
        pts += [lobe_tips[-1], (sx + side * 9, sy + 15 * o), (sx - side * 3, sy + 3)]
        blit_poly(c, pts, pal["body"], style.get("alpha", 0.68))
        inner = [(px * 0.84 + sx * 0.16, py * 0.84 + sy * 0.16) for (px, py) in pts]
        blit_poly(c, inner, pal["body_hi"], 0.5)
        for i in range(len(lobe_tips) - 1):
            t0, t1 = lobe_tips[i], lobe_tips[i + 1]
            mid = ((t0[0] + t1[0]) * 0.5, (t0[1] + t1[1]) * 0.5)
            pull = 0.24
            q = (mid[0] + (sx - mid[0]) * pull, mid[1] + (sy - mid[1]) * pull)
            arc = bezier(t0, q, t1, 12)
            for j in range(len(arc) - 1):
                line_beads(c, arc[j], arc[j + 1], pal["edge"], 0.95, 1.5, 1.1, n=3)
        ellipse_ddf(c, sx, sy - 2, 4.5 * o, 4 * o, 0.0, pal["edge_core"], 0.95, edge=0.25)


def draw_eclipse(c, style, o, dy, tilt, pal):
    """Eclipse: rayos LARGOS en abanico + arco de limbo = silueta de ala."""
    rays = style.get("rays", 9)
    sq = style.get("sq", 1.0)
    for side in (-1, 1):
        sx, sy = CX + side * (GAP + 6), CY + dy - 4
        R = style.get("disc", 11.0) * (0.8 + 0.2 * o)
        # rayos de la corona — abanico ALA (de arriba a abajo-abajo)
        for k in range(rays):
            frac = k / (rays - 1)
            ang = math.radians(64 - 116 * frac + tilt * 28)
            d = (side * math.cos(ang), -math.sin(ang))
            long = (k % 2 == 0)
            L = style.get("maxL", 84) * (0.50 if long else 0.30) * o * sq
            perp = (-d[1], d[0])
            base0 = (sx + d[0] * R * 0.9, sy + d[1] * R * 0.9)
            tip = (sx + d[0] * (R + L), sy + d[1] * (R + L) - (4 if long else 1))
            w = 3.0 if long else 2.0
            p1 = (base0[0] + perp[0] * w, base0[1] + perp[1] * w)
            p2 = (base0[0] - perp[0] * w, base0[1] - perp[1] * w)
            blit_poly(c, [p1, tip, p2], pal["ray"] if long else pal["ray_hi"], 0.95)
        # halo + ARCO DE LIMBO (contorno de ala que abraza los rayos)
        ellipse_ddf(c, sx, sy, R * 1.6, R * 1.6, 0.0, pal["ray"], 0.28, edge=0.6)
        arc = []
        for i in range(20):
            a = math.radians(66 - 118 * i / 19)
            rr = R + style.get("maxL", 84) * 0.56 * o * sq
            arc.append((sx + side * math.cos(a) * rr, sy - math.sin(a) * rr * 0.86))
        for j in range(len(arc) - 1):
            line_beads(c, arc[j], arc[j + 1], pal["rim"], 0.75, 1.5, 0.9, n=3)
        # DISCO NEGRO pequeño con filo dorado
        ellipse_ddf(c, sx, sy, R, R, 0.0, pal["rim"], 1.0, edge=0.06)
        ellipse_ddf(c, sx, sy, R * 0.86, R * 0.86, 0.0, pal["disc"], 1.0, edge=0.04)
        for i in range(7):
            a = math.radians(200 - 140 * i / 6)
            ellipse_ddf(c, sx + math.cos(a) * R * 0.92, sy + math.sin(a) * R * 0.92,
                        1.2, 1.2, 0.0, pal["ray_hi"], 0.9, edge=0.08)
        dax, day = sx + side * R * 0.35, sy - R * 0.85
        ellipse_ddf(c, dax, day, 2.6, 0.8, 0.0, pal["ray_hi"], 1.0, edge=0.1)
        ellipse_ddf(c, dax, day, 0.8, 2.6, 0.0, pal["ray_hi"], 1.0, edge=0.1)


def draw_nova(c, style, o, dy, tilt, pal):
    """Supernova: anillos de choque + plumas de choque blancas-rosadas."""
    sq = style.get("sq", 1.0)
    for side in (-1, 1):
        sx, sy = CX + side * (GAP - 2), CY + dy
        for rr, aa in ((52, 0.55), (34, 0.7)):
            prev = None
            for i in range(26):
                a = math.radians(148 + 104 * i / 25)
                px = sx + side * math.cos(a) * rr * o * sq
                py = sy - abs(math.sin(a)) * rr * o * sq * 0.60 - 4
                if prev:
                    line_beads(c, prev, (px, py), pal["ring"], aa * 0.85, 1.6, 1.0, n=3)
                prev = (px, py)
    draw_flame(c, dict(licks=4, maxL=style.get("maxL", 80), w0=8.0), o, dy - 2, tilt, pal)
    for side in (-1, 1):
        sx, sy = CX + side * (GAP - 2), CY + dy + 4
        ellipse_ddf(c, sx, sy - 2, 10 * o, 8 * o, 0.0, pal["mid"], 0.85, edge=0.3)
        ellipse_ddf(c, sx, sy - 2, 6.5 * o, 5.5 * o, 0.0, pal["hi"], 1.0, edge=0.25)
        ellipse_ddf(c, sx, sy - 2, 3.5 * o, 3 * o, 0.0, pal["core"], 1.0, edge=0.2)


# ============================================================================
#  CONFIGURACIÓN
# ============================================================================

WINGS = {
    "SolarNovaWings": dict(
        style="flame", glow=True, outline=(58, 14, 6),
        pal=dict(core=(255, 252, 235), hot=(255, 214, 90), mid=(255, 122, 30),
                 deep=(198, 54, 18)),
        conf=dict(licks=5, maxL=82, w0=9.0),
    ),
    "QuantumPlasmaWings": dict(
        style="crystal", glow=False, outline=(5, 38, 58),
        pal=dict(core=(240, 254, 255), hi=(160, 245, 255), mid=(70, 205, 240),
                 deep=(22, 140, 190)),
        conf=dict(shards=5, maxL=82, maxW=8.5),
    ),
    "EtherealVoidWings": dict(
        style="membrane", glow=False, outline=(14, 3, 22),
        pal=dict(membrane=(42, 12, 66), bone=(96, 76, 120), bone_hi=(128, 104, 152),
                 rim=(255, 96, 200), vein=(252, 0, 150), star=(255, 255, 255)),
        conf=dict(fingers=4, maxL=84, alpha=0.88),
    ),
    "GlacialEtherWings": dict(
        style="feathered", glow=False, outline=(30, 58, 95),
        pal=dict(core=(246, 252, 255), hi=(208, 240, 255), mid=(120, 190, 235),
                 deep=(45, 105, 170)),
        conf=dict(n=6, ang0=56, ang1=8, maxL=80, maxW=10, len0=0.62),
        detail=detail_glacial,
    ),
    "GenesisFossilWings": dict(
        style="feathered", glow=False, outline=(51, 36, 26),
        pal=dict(core=(255, 196, 86), hi=(232, 217, 168), mid=(201, 168, 106),
                 deep=(143, 107, 61), vein=(212, 160, 60), shard=(255, 196, 86)),
        conf=dict(n=6, ang0=56, ang1=8, maxL=82, maxW=11, len0=0.62),
        detail=detail_fossil,
    ),
    "NebulaPillarWings": dict(
        style="nebula", glow=True, outline=None,
        pal=dict(body=(168, 60, 190), body_hi=(210, 102, 230), edge=(124, 246, 255),
                 edge_core=(220, 252, 255)),
        conf=dict(lobes=5, maxL=82, alpha=0.68),
    ),
    "EclipseWings": dict(
        style="eclipse", glow=False, outline=None,
        pal=dict(disc=(12, 10, 16), rim=(255, 224, 102), ray=(255, 243, 196),
                 ray_hi=(255, 252, 235)),
        conf=dict(rays=9, maxL=84, disc=11.0),
    ),
    "SupernovaWings": dict(
        style="nova", glow=True, outline=(66, 20, 110),
        pal=dict(core=(255, 255, 255), hot=(255, 243, 176), hi=(255, 243, 176),
                 mid=(255, 110, 176), deep=(184, 76, 255), ring=(180, 240, 255)),
        conf=dict(maxL=80),
    ),
}

STYLE_FN = dict(feathered=draw_feathered, membrane=draw_membrane, flame=draw_flame,
                crystal=draw_crystal, nebula=draw_nebula, eclipse=draw_eclipse,
                nova=draw_nova)


# ============================================================================
#  AUTO-ENCAJE: render → recorte → ancla a (FW/2, FH_final/2 + ANCHOR_DY)
# ============================================================================

def render_frames(spec):
    """Devuelve [(frame_pil, ancla_y)] con el ancla movida bajo el centro."""
    out = []
    for fr in FRAMES:
        c = canvas_ss()
        conf = dict(spec["conf"])
        conf["sq"] = fr["sq"]
        STYLE_FN[spec["style"]](c, conf, fr["o"], fr["dy"], fr["tilt"], spec["pal"])
        if spec.get("detail"):
            spec["detail"](c, fr["o"], fr["dy"], fr["sq"], spec["pal"])
        small = downsample(c)
        out.append(pixelize(small, outline_color=spec.get("outline"),
                            glow=spec.get("glow", False)))
    return out


def autofit(frames):
    """Recorta cada frame al contenido y recoloca el ANCLA del ala.

    El tamaño final = (máx. contenido ENCIMA del ancla) + (máx. contenido
    DEBAJO) + padding: el ancla queda en su sitio natural y ningún frame
    puede tocarse con los bordes. El dibujado vanilla pone el CENTRO del
    frame en el centro-del-torso → el conjunto del ala se centra solo.
    """
    ups, downs = [], []
    for i, f in enumerate(frames):
        a = f[:, :, 3]
        ys, xs = np.where(a > 20)
        anchor_y = CY + FRAMES[i]["dy"]
        ups.append(anchor_y - ys.min())
        downs.append(ys.max() - anchor_y)
    pad = 3
    max_up = int(max(ups)) + pad
    max_dn = int(max(downs)) + pad
    FH_new = max(max_up + max_dn, 48)
    # Múltiplo de 4 OBLIGATORIO: vanilla usa texture.Frame(1, 4) con
    # división entera (altura/4) — una altura no múltipla desalinearía
    # los frames hasta 3 px y recortaría la última fila.
    FH_new = (FH_new + 3) // 4 * 4
    result = []
    for i, f in enumerate(frames):
        ty = max_up - int(CY + FRAMES[i]["dy"])   # ancla → fila max_up
        canvas = np.zeros((FH_new, FW, 4), dtype=np.float64)
        y0 = max(0, ty)
        y1 = min(FH_new, ty + FH)
        if y1 > y0:
            canvas[y0:y1, :, :] = f[y0 - ty:y1 - ty, :, :]
        result.append(canvas)
    return result, FH_new


def render_sheet(name, spec):
    frames = render_frames(spec)
    frames, fh = autofit(frames)
    sheet = Image.new("RGBA", (FW, fh * 4), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        sheet.paste(frame_to_pil(f), (0, i * fh))
    sheet.save(os.path.join(OUT_DIR, f"{name}_Wings.png"))
    return sheet


# ============================================================================
#  ICONOS (30×24)
# ============================================================================

ICON_W, ICON_H = 30, 24


def render_icon(name, spec):
    global CX, CY, GAP
    old = (CX, CY, GAP)
    CX, CY, GAP = FW // 2, FH // 2, 4
    c = canvas_ss()
    conf = dict(spec["conf"])
    conf["sq"] = 1.0
    STYLE_FN[spec["style"]](c, conf, 1.0, 0, 0.0, spec["pal"])
    if spec.get("detail"):
        spec["detail"](c, 1.0, 0, 1.0, spec["pal"])
    CX, CY, GAP = old
    small = downsample(c)
    out = pixelize(small, outline_color=spec.get("outline"), glow=spec.get("glow", False))
    img = frame_to_pil(out)
    a = np.asarray(img)[:, :, 3]
    ys, xs = np.where(a > 20)
    if len(xs):
        img = img.crop((max(0, int(xs.min()) - 1), max(0, int(ys.min()) - 1),
                        min(FW, int(xs.max()) + 2), min(FH, int(ys.max()) + 2)))
    img = img.resize((ICON_W, ICON_H), Image.LANCZOS)
    img.save(os.path.join(OUT_DIR, f"{name}.png"))
    return img


def icon_special(name, kind):
    c = canvas_ss()
    cx, cy = FW / 2, FH / 2
    if kind == "horizon":
        for k in range(3):
            rr = 14 + k * 9
            prev = None
            for i in range(16):
                a = math.radians(180 + 160 * i / 15)
                px = cx + math.cos(a) * rr
                py = cy - abs(math.sin(a)) * rr * 0.55 - 2
                if prev:
                    line_beads(c, prev, (px, py),
                               (255, 60, 190) if k < 2 else (252, 0, 150),
                               0.95, 2.6 - k * 0.5, 1.8 - k * 0.3, n=3)
                prev = (px, py)
        ellipse_ddf(c, cx, cy - 2, 10, 10, 0.0, (255, 155, 210), 1.0, edge=0.08)
        ellipse_ddf(c, cx, cy - 2, 8.4, 8.4, 0.0, (0, 0, 0), 1.0, edge=0.04)
        ellipse_ddf(c, cx, cy - 2, 3, 3, 0.0, (255, 255, 255), 0.9, edge=0.1)
    else:
        for k in range(5):
            adeg = 66 - 76 * k / 4
            a = math.radians(adeg)
            L = 14 + 18 * (0.6 + 0.4 * (k % 2))
            d = (math.cos(a), -math.sin(a))
            for ring in range(3):
                t = 0.35 + 0.33 * ring
                px, py = cx + d[0] * L * t, cy + d[1] * L * t
                rr = 3.4 - ring * 0.6
                prev = None
                for i in range(9):
                    aa = math.radians(360 * i / 8)
                    qx = px + math.cos(aa) * rr
                    qy = py + math.sin(aa) * rr
                    if prev:
                        line_beads(c, prev, (qx, qy),
                                   (255, 155, 210) if ring else (255, 255, 255),
                                   0.9 - ring * 0.15, 1.1, 0.8, n=2)
                    prev = (qx, qy)
        ellipse_ddf(c, cx, cy - 2, 7, 7, 0.0, (120, 40, 160), 0.8, edge=0.2)
        ellipse_ddf(c, cx, cy - 2, 4.8, 4.8, 0.0, (0, 0, 0), 1.0, edge=0.06)
    small = downsample(c)
    out = pixelize(small, outline_color=None, glow=True)
    img = frame_to_pil(out)
    a = np.asarray(img)[:, :, 3]
    ys, xs = np.where(a > 20)
    if len(xs):
        img = img.crop((int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))
    img = img.resize((ICON_W, ICON_H), Image.LANCZOS)
    img.save(os.path.join(OUT_DIR, f"{name}.png"))
    return img


def blank_wings(name):
    Image.new("RGBA", (8, 8), (0, 0, 0, 0)).save(
        os.path.join(OUT_DIR, f"{name}_Wings.png"))


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    previews = []
    for name, spec in WINGS.items():
        sheet = render_sheet(name, spec)
        icon = render_icon(name, spec)
        previews.append((name, sheet, icon))
        print(f"OK {name}: sheet {sheet.size}, icono {icon.size}")
    blank_wings("EventHorizonWings")
    blank_wings("PhotonRingWings")
    icon_special("EventHorizonWings", "horizon")
    icon_special("PhotonRingWings", "photon")
    print("OK blanks + iconos tecnica-coronas")

    # hoja de contacto: columnas de igual altura (la máxima)
    Hmax = max(s.height for _, s, _ in previews)
    cols = []
    for name, sheet, icon in previews:
        col = Image.new("RGBA", (FW + 10, Hmax + 34), (24, 24, 28, 255))
        col.paste(sheet, (5, 26), sheet)
        col.paste(icon, (5, 4), icon)
        cols.append((name, col))
    W = sum(c.width for _, c in cols)
    H = max(c.height for _, c in cols)
    contact = Image.new("RGBA", (W, H), (24, 24, 28, 255))
    x = 0
    for _, col in cols:
        contact.paste(col, (x, 0), col)
        x += col.width
    contact.save(os.path.join(HERE, "preview.png"))
    print(f"OK preview: research/wings/preview.png ({W}x{H})")


if __name__ == "__main__":
    main()
