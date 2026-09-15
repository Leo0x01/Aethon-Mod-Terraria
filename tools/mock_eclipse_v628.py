SKIP_DISK = False
#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_eclipse_v628.py — EL MOCK 1:1 DEL ECLIPSE TOTAL (el rediseño v6.28).

Simula el pipeline tML/FNA (PNGs premultiplicados al cargar, aditivo
dst += tex·tint, ocultador dst = src·a + dst·(1−a)) y pinta CUATRO momentos
del nuevo Eclipse Primordial:
  · PANEL 1 — EL CRECIENTE (formT 0.55): el disco de la noche SE DESLIZA
    sobre el sol — la luna comiéndose a la luz.
  · PANEL 2 — EL TOTAL (formT 1, corona despierta): el día murió — el
    disco negro con su rim violeta, LA CORONA de streamers asimétricos
    (ecuatoriales largos, polares plumosos), la cromosfera roja y las
    prominencias.
  · PANEL 3 — LA ÚLTIMA LUZ (ultima 0.6): la corona AVIVADA y el ANILLO
    DE DIAMANTE brillando en el limbo.
  · PANEL 4 — EL RETORNO (novaT 0.5): el disco IMPLODE, la corona soplada
    y el núcleo CEGADOR.
"""
import os
import numpy as np
from PIL import Image

PROJ = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'AethonMod')
EFX = os.path.join(PROJ, 'Content', 'Effects')
PRO = os.path.join(EFX, 'Procedural')

W, H = 560, 560
SUNR = 110.0          # el sol primordial (74 px del juego ×1.5 del mock)
DISCO_K = 0.94

WhiteIncan = np.array([255, 250, 240], np.float32)
SunGold = np.array([255, 205, 110], np.float32)
ChromoRed = np.array([255, 70, 45], np.float32)
PromRed = np.array([255, 110, 60], np.float32)
PromGold = np.array([255, 190, 90], np.float32)
NightViolet = np.array([140, 90, 235], np.float32)
RuneGold = np.array([255, 180, 70], np.float32)
RuneViolet = np.array([110, 130, 255], np.float32)
RuneWhite = np.array([255, 245, 220], np.float32)


def load_premult(path):
    a = np.array(Image.open(path).convert('RGBA'), np.float32)
    return a[:, :, :3] * (a[:, :, 3:4] / 255.0), a[:, :, 3] / 255.0


TEX = {
    'glow': load_premult(os.path.join(PRO, 'SoftGlow.png')),
    'ring': load_premult(os.path.join(PRO, 'Ring.png')),
    'black': load_premult(os.path.join(PRO, 'BlackDisk.png')),
    'orb': load_premult(os.path.join(EFX, 'GlowOrb.png')),
    'star': load_premult(os.path.join(PRO, 'Star.png')),
}


def h01(seed, a, b):
    x = np.uint32((seed * 374761393 + a * 668265263 + b * 2246822519) & 0xFFFFFFFF)
    x = (x ^ (x >> np.uint32(13))) * np.uint32(1274126177)
    return ((x ^ (x >> np.uint32(16))) & np.uint32(0xFFFFFF)) / float(0xFFFFFF)


def quad(dst, texname, pos, size, rot, tint, alpha, additive=True):
    tex = TEX[texname][0] if additive else TEX[texname][0]
    ta = TEX[texname][1]
    th, tw = tex.shape[:2]
    if size[0] < 0.1 or size[1] < 0.1 or alpha <= 0.01:
        return
    c, s = np.cos(rot), np.sin(rot)
    ex, ey = size[0] / 2, size[1] / 2
    corners = np.array([[ex, ey], [-ex, ey], [ex, -ey], [-ex, -ey]])
    rc = corners @ np.array([[c, s], [-s, c]]).T
    x0 = int(np.floor(pos[0] + rc[:, 0].min())); x1 = int(np.ceil(pos[0] + rc[:, 0].max()))
    y0 = int(np.floor(pos[1] + rc[:, 1].min())); y1 = int(np.ceil(pos[1] + rc[:, 1].max()))
    x0, y0 = max(x0, 0), max(y0, 0)
    x1, y1 = min(x1, dst.shape[1]), min(y1, dst.shape[0])
    if x0 >= x1 or y0 >= y1:
        return
    yy, xx = np.mgrid[y0:y1, x0:x1]
    dx, dy = xx - pos[0], yy - pos[1]
    u = dx * c + dy * s
    v = -dx * s + dy * c
    uu = (u / size[0] + 0.5) * (tw - 1)
    vv = (v / size[1] + 0.5) * (th - 1)
    m = (uu >= -0.5) & (uu < tw - 0.5) & (vv >= -0.5) & (vv < th - 0.5)
    if not m.any():
        return
    u0 = np.clip(np.floor(uu), 0, tw - 1).astype(np.int32)
    v0 = np.clip(np.floor(vv), 0, th - 1).astype(np.int32)
    u1 = np.clip(u0 + 1, 0, tw - 1)
    v1 = np.clip(v0 + 1, 0, th - 1)
    fu = (uu - u0)[:, :, None]
    fv = (vv - v0)[:, :, None]
    t00 = tex[v0, u0]; t10 = tex[v0, u1]; t01 = tex[v1, u0]; t11 = tex[v1, u1]
    texs = (t00 * (1 - fu) * (1 - fv) + t10 * fu * (1 - fv) +
            t01 * (1 - fu) * fv + t11 * fu * fv)
    a00 = ta[v0, u0]; a10 = ta[v0, u1]; a01 = ta[v1, u0]; a11 = ta[v1, u1]
    fu2 = fu[:, :, 0]; fv2 = fv[:, :, 0]
    as_ = (a00 * (1 - fu2) * (1 - fv2) + a10 * fu2 * (1 - fv2) +
           a01 * (1 - fu2) * fv2 + a11 * fu2 * fv2) * alpha
    tint = np.array(tint, np.float32) / 255.0
    region = dst[y0:y1, x0:x1]
    if additive:
        src = texs * tint[None, None, :] * alpha
        region += np.where(m[:, :, None], src, 0.0)
    else:
        # NON-premultiplied: dst = src.rgb·src.a + dst·(1−src.a) — el OCULTADOR.
        fu2 = fu[:, :, 0]; fv2 = fv[:, :, 0]
        rgb = texs  # negro premult
        a2 = as_ * (TEX[texname][1][v0, u0] * 0 + 1.0)  # alfa del tex ya en premult
        a2 = as_
        for ch in range(3):
            region[:, :, ch] = np.where(
                m, rgb[:, :, ch] * 0.0 + region[:, :, ch] * (1 - a2 * 0.97),
                region[:, :, ch])
        # pinta el negro sólido donde el alfa es alto
        mhard = m & (as_ > 0.5)
        region[mhard, 0] = 4.0
        region[mhard, 1] = 4.0
        region[mhard, 2] = 6.0


def draw_eclipse(dst, center, sunR, time, seed, formT=1.0, ultima=0.0, novaT=0.0):
    global SKIP_DISK
    SKIP_DISK = novaT > 0
    R = sunR * (1 + 0.04 * np.sin(time * 1.6 + seed))
    C = np.array(center, np.float64)

    # === 1. EL SOL PRIMORDIAL ===
    quad(dst, 'glow', C, (R * 2.35, R * 2.35), 0, SunGold, 0.42)
    quad(dst, 'glow', C, (R * 1.95, R * 1.95), 0, WhiteIncan, 0.30)
    quad(dst, 'glow', C, (R * 1.62, R * 1.62), 0, WhiteIncan, 0.85)
    quad(dst, 'orb', C, (R * 1.5, R * 1.5), 0, np.array([255, 255, 255]), 0.95)
    for k in range(7):
        h1, h2 = h01(seed, 601 + k, 3), h01(seed, 607 + k, 7)
        ang = h1 * 2 * np.pi + time * (0.05 + 0.03 * h2)
        rr = R * (0.15 + 0.55 * h2)
        p = C + np.array([np.cos(ang), np.sin(ang)]) * rr
        cell = 0.55 + 0.45 * np.sin(time * (0.5 + 0.4 * h1) + k * 1.9)
        quad(dst, 'glow', p, (R * 0.55, R * 0.55), 0, SunGold, 0.16 * cell)

    # === 2. EL DISCO DE LA NOCHE (el ocultador) ===
    if novaT <= 0 and not SKIP_DISK:
        slide = formT * formT * (3 - 2 * formT)
        settle = np.sin(time * 42) * (1 - formT) * 2.2 if formT > 0.85 else 0
        offset = np.array([-R * 2.6, -R * 0.5]) * (1 - slide) + np.array([settle, settle * 0.4])
        diskR = R * DISCO_K
        diskC = C + offset
        quad(dst, 'black', diskC, (diskR * 2.1, diskR * 2.1), 0, np.array([0, 0, 0]), 0.97, additive=False)
        rimA = 0.34 * (0.75 + 0.25 * np.sin(time * 3.1 + seed))
        quad(dst, 'ring', diskC, (diskR * 2.06, diskR * 2.06), 0, NightViolet, rimA)
        quad(dst, 'ring', diskC, (diskR * 2.0, diskR * 2.0), 0, WhiteIncan, rimA * 0.85)

    # === 3. LA CORONA (streamers) — velo ANULAR (no centrado: no lava el disco) ===
    wake = min(formT * 1.4, 1.0)
    grow = 1 + ultima * 0.35 + novaT * 1.6
    quad(dst, 'ring', C, (R * 1.34 * 2.174,) * 2, 0, WhiteIncan, 0.15 * wake)
    quad(dst, 'ring', C, (R * 1.10 * 2.174,) * 2, 0, WhiteIncan, 0.22 * wake)
    for s in range(12):
        h1, h2, h3, h4 = (h01(seed, 701 + s, 3), h01(seed, 709 + s, 7),
                          h01(seed, 719 + s, 11), h01(seed, 727 + s, 13))
        ang = s / 12 * 2 * np.pi + h1 * 0.35
        lat = abs(np.sin(ang + np.pi / 4))
        length = R * (1.15 + (2.55 - 1.15) * lat * lat) * (0.75 + 0.5 * h2) * grow
        length *= 1 + 0.09 * np.sin(time * (0.35 + 0.3 * h3) + s * 2.3)
        curve = np.sin(ang + np.pi / 4) * (0.28 + 0.22 * h4)
        baseR = R * (0.92 + 0.05 * h3)
        d = np.array([np.cos(ang), np.sin(ang)])
        perp = np.array([-d[1], d[0]])
        wBase = R * (0.16 + 0.13 * lat) * (0.8 + 0.4 * h1)
        for i in range(4):
            f0, f1 = i / 4, (i + 1) / 4
            p0 = C + d * (baseR + length * f0) + perp * (curve * length * f0 * f0)
            p1 = C + d * (baseR + length * f1) + perp * (curve * length * f1 * f1)
            wseg = wBase * (1 - (f0 + f1) * 0.5) + R * 0.02
            mid = (p0 + p1) / 2
            delta = p1 - p0
            ln = np.linalg.norm(delta)
            if ln < 0.5:
                continue
            rot = np.arctan2(delta[1], delta[0])
            flow = 0.72 + 0.28 * np.sin(time * (2.2 + 1.5 * h2) - f0 * 9 + s)
            quad(dst, 'glow', mid, (ln + wseg * 2.2, wseg * 3.0), rot, WhiteIncan, 0.10 * wake * flow)
            quad(dst, 'glow', mid, (ln + wseg * 1.1, wseg * 1.5), rot, WhiteIncan, 0.20 * wake * flow)
            col = (WhiteIncan + (SunGold - WhiteIncan) * f0 * 0.6)
            quad(dst, 'glow', mid, (ln + wseg * 0.7, wseg * 0.7), rot, col, 0.30 * wake * flow)
            quad(dst, 'glow', p1, (wseg * 2.4, wseg * 2.4), rot, WhiteIncan, 0.14 * wake * flow)
        if h4 > 0.45:
            fTip = 1.06 + 0.05 * np.sin(time * 0.9 + s * 1.7)
            tip = C + d * (baseR + length * fTip) + perp * (curve * length * fTip * fTip)
            quad(dst, 'orb', tip, (R * 0.07 * (0.7 + 0.6 * h2),) * 2, 0, WhiteIncan, 0.4 * wake)

    # === 4. LA CROMOSFERA + PERLAS DE BAILY ===
    if formT > 0.92:
        vis = (formT - 0.92) / 0.08
        quad(dst, 'ring', C, (R * 2.10, R * 2.10), 0, ChromoRed, 0.30 * vis)
        quad(dst, 'ring', C, (R * 2.045, R * 2.045), 0, ChromoRed, 0.22 * vis)
        for b in range(7):
            h1 = h01(seed, 801 + b, 3)
            ang = h1 * 2 * np.pi + time * 0.03
            p = C + np.array([np.cos(ang), np.sin(ang)]) * R * 1.03
            tw = 0.4 + 0.6 * max(0, np.sin(time * (3 + 2 * h1) + b * 2.6))
            if tw < 0.15:
                continue
            quad(dst, 'orb', p, (R * 0.055 * (0.8 + 0.5 * tw),) * 2, 0, PromGold, 0.7 * vis * tw)

    # === 5. LAS PROMINENCIAS ===
    if formT > 0.88:
        vis = (formT - 0.88) / 0.12
        for pr in range(5):
            h1, h2, h3 = h01(seed, 851 + pr, 3), h01(seed, 857 + pr, 7), h01(seed, 863 + pr, 11)
            ang = h1 * 2 * np.pi + time * 0.05 * (1 if h2 > 0.5 else -1)
            anchor = C + np.array([np.cos(ang), np.sin(ang)]) * R * 0.97
            outDir = np.array([np.cos(ang), np.sin(ang)])
            life = 0.5 + 0.5 * np.sin(time * (0.45 + 0.35 * h2) + pr * 2.1)
            if life < 0.25:
                continue
            len_ = R * (0.18 + 0.30 * h3) * life * vis
            side = np.array([-outDir[1], outDir[0]])
            for i in range(3):
                f0, f1 = i / 3, (i + 1) / 3
                p0 = anchor + outDir * (len_ * f0) + side * (len_ * 0.55 * f0 * f0)
                p1 = anchor + outDir * (len_ * f1) + side * (len_ * 0.55 * f1 * f1)
                mid = (p0 + p1) / 2
                delta = p1 - p0
                sl = np.linalg.norm(delta)
                if sl < 0.5:
                    continue
                rot = np.arctan2(delta[1], delta[0])
                flick = 0.65 + 0.35 * np.sin(time * (9 + 6 * h3) + i * 5.1 + pr * 3.7)
                wseg = R * (0.070 - 0.012 * i) * (0.8 + 0.4 * h2) * life
                quad(dst, 'glow', mid, (sl + wseg * 2, wseg * 2.4), rot, PromRed, 0.24 * vis * flick)
                quad(dst, 'glow', mid, (sl + wseg * 1.1, wseg * 1.3), rot,
                     (PromRed + (PromGold - PromRed) * 0.45), 0.34 * vis * flick)
                quad(dst, 'glow', mid, (sl + wseg * 0.6, wseg * 0.6), rot, PromGold, 0.42 * vis * flick)

    # === 6. EL ANILLO DE DIAMANTE ===
    if formT > 0.96:
        vis = (formT - 0.96) / 0.04
        speed = 0.13 * (1 + ultima * 6)
        ang = seed * 0.7 + time * speed
        p = C + np.array([np.cos(ang), np.sin(ang)]) * R * 0.985
        breathe = 0.9 + 0.1 * np.sin(time * 6.3 + seed)
        quad(dst, 'glow', p, (R * 0.85 * breathe, R * 0.85 * breathe), 0, WhiteIncan, 0.34 * vis)
        quad(dst, 'glow', p, (R * 0.40 * breathe, R * 0.40 * breathe), 0, WhiteIncan, 0.6 * vis)
        tangent = np.array([-np.sin(ang), np.cos(ang)])
        rotT = np.arctan2(tangent[1], tangent[0])
        k = R * 0.9 * vis * breathe
        quad(dst, 'star', p, (k, max(2.2, R * 0.035)), rotT, WhiteIncan, 0.95 * vis)
        quad(dst, 'star', p, (k * 0.55, max(2.0, R * 0.03)), rotT + np.pi / 2, WhiteIncan, 0.85 * vis)
        quad(dst, 'orb', p, (R * 0.10, R * 0.10), 0, np.array([255, 255, 255]), 0.95 * vis)

    # === 7. LOS TRES CÍRCULOS RÚNICOS (solo aros — los glifos van en juego) ===
    for rad, col, a in ((2.4, RuneWhite, 0.20), (3.2, RuneGold, 0.17), (4.0, RuneViolet, 0.15)):
        quad(dst, 'ring', C, (rad * R * 2.174, rad * R * 2.174), 0, col, a)

    # === 8. EL RETORNO: EL NÚCLEO CEGADOR ===
    if novaT > 0:
        flash = np.sin(novaT * np.pi)
        if flash > 0.02:
            quad(dst, 'glow', C, (R * 5.5 * (0.5 + 0.8 * flash),) * 2, 0, WhiteIncan, 0.55 * flash)
            quad(dst, 'glow', C, (R * 3.6 * (0.6 + 0.8 * flash),) * 2, 0, WhiteIncan, 0.85 * flash)
            quad(dst, 'glow', C, (R * 1.8, R * 1.8), 0, SunGold, 0.65 * flash)
            quad(dst, 'star', C, (R * 6.4 * flash, max(3.0, R * 0.07)), 0, WhiteIncan, 0.9 * flash)
            quad(dst, 'star', C, (R * 6.4 * flash, max(3.0, R * 0.07)), np.pi / 2, WhiteIncan, 0.9 * flash)
            quad(dst, 'star', C, (R * 3.8 * flash, max(2.0, R * 0.04)), 0, SunGold, 0.85 * flash)
            quad(dst, 'star', C, (R * 3.8 * flash, max(2.0, R * 0.04)), np.pi / 2, SunGold, 0.85 * flash)
        diskDie = novaT ** 1.6
        if diskDie < 0.98:
            rDie = R * DISCO_K * (1 - diskDie)
            quad(dst, 'black', C, (rDie * 2.1, rDie * 2.1), 0, np.array([0, 0, 0]),
                 0.96 * (1 - diskDie * 0.6), additive=False)


def main():
    print("mock_eclipse_v628.py — EL MOCK 1:1 DEL ECLIPSE TOTAL")
    seed = 747
    time = 2.9
    canvas = np.zeros((H * 4, W, 3), np.float32)
    fondo = np.zeros((H, W, 3), np.float32) + np.array([30, 24, 44], np.float32)

    panels = [
        ("EL CRECIENTE (formT 0.55)", dict(formT=0.55, ultima=0.0)),
        ("EL TOTAL (formT 1.0)", dict(formT=1.0, ultima=0.0)),
        ("LA ÚLTIMA LUZ (ultima 0.6)", dict(formT=1.0, ultima=0.6)),
        ("EL RETORNO (novaT 0.5)", dict(formT=1.0, ultima=1.0, novaT=0.5)),
    ]
    for i, (name, kw) in enumerate(panels):
        p = fondo.copy()
        draw_eclipse(p, (W * 0.5, H * 0.5), SUNR, time + i * 0.8, seed, **kw)
        canvas[i * H:(i + 1) * H] = p
        print(f"  · PANEL {i + 1}: {name}")

    out = np.clip(canvas, 0, 255).astype(np.uint8)
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mock_eclipse_v628.png')
    Image.fromarray(out).save(path)
    print(f"  + {path} ({W}x{H * 4})")


if __name__ == '__main__':
    main()
