#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_rift_v628.py — EL MOCK 1:1 DEL DESGARRO CONTINUO (RiftLib v2).

Simula el pipeline EXACTO de tML/FNA:
  1. tML carga los PNG PREMULTIPLICADOS (rgb × alfa — por eso los PNG de la
     casa funcionan y los SetData runtime necesitaban el fix v6.25).
  2. SpriteBatch.Additive: dst += tex_premult_rgb × tint.rgb / 255.
  3. El pase de VACÍO (NonPremultiplied): dst = src.rgb×src.a + dst×(1−src.a).

RENDERIZA:
  · PANEL A — LA LÍNEA RECTA (v1 reconstruida con TrailGlow por segmentos):
    el defecto original — juntas con franja cian casi transparente.
  · PANEL B — LA LÍNEA RECTA v2 (UN QUAD RiftTaper*): la misma anatomía
    (labios ±0.31W + núcleo + vacío + estrellas) SIN ninguna junta.
  · PANEL C — LA HERIDA FRACTURADA v2 (RiftLip + perlas en los vértices):
    el camino Lichtenberg continuo aunque gire.
  · PANEL D — LA VIBRACIÓN (CaminoVibracion con RiftLip + perlas).

MIDE (el veredicto numérico): huecos = filas activas cuya señal cae bajo
0.10 del pico local a lo largo de la línea; interrupciones = mínimos locales
de la envolvente superior < 35% del máximo. v1 tenía cientos; v2 debe tener 0.
"""
import os
import numpy as np
from PIL import Image

PROJ = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'AethonMod')
EFX = os.path.join(PROJ, 'Content', 'Effects')
PRO = os.path.join(EFX, 'Procedural')

W, H = 900, 240                    # lienzo del mock
TEAR_LEN, MAXW = 620.0, 14.0       # los números del arma
PALETA = {                          # el arma: velo violeta / cuerpo carmesí / núcleo blanco
    'velo': np.array([150, 80, 255], np.float32),
    'cuerpo': np.array([255, 60, 130], np.float32),
    'nucleo': np.array([235, 245, 255], np.float32),
}


def load_premult(path):
    """El PNG como lo carga tML: PREMULTIPLICADO."""
    a = np.array(Image.open(path).convert('RGBA'), np.float32)
    rgb = a[:, :, :3] * (a[:, :, 3:4] / 255.0)
    return rgb, a[:, :, 3] / 255.0


def quad_additive(dst, tex, pos, size, rot, tint, alpha):
    """SpriteBatch.Additive con textura premultiplicada: dst += tex·tint.
    pos = centro del quad en pantalla; size = (len, w) px; rot = rad."""
    th, tw = tex.shape[:2]
    if size[0] < 0.1 or size[1] < 0.1:
        return
    c, s = np.cos(rot), np.sin(rot)
    # Muestra el quad en pantalla (inversa del draw): bounding box del quad.
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
    # Coordenadas en espacio del quad (u a lo largo, v a lo ancho).
    dx, dy = xx - pos[0], yy - pos[1]
    u = dx * c + dy * s
    v = -dx * s + dy * c
    # Muestreo bilineal simple (nearest con suavizado de área: SS 2×)
    uu = (u / size[0] + 0.5) * (tw - 1)
    vv = (v / size[1] + 0.5) * (th - 1)
    m = (uu >= -0.5) & (uu < tw - 0.5) & (vv >= -0.5) & (vv < th - 0.5)
    if not m.any():
        return
    # Muestreo BILINEAL (el LinearClamp del juego).
    u0 = np.clip(np.floor(uu), 0, tw - 1).astype(np.int32)
    v0 = np.clip(np.floor(vv), 0, th - 1).astype(np.int32)
    u1 = np.clip(u0 + 1, 0, tw - 1)
    v1 = np.clip(v0 + 1, 0, th - 1)
    fu = (uu - u0)[:, :, None]
    fv = (vv - v0)[:, :, None]
    t00 = tex[v0, u0]; t10 = tex[v0, u1]; t01 = tex[v1, u0]; t11 = tex[v1, u1]
    texs = (t00 * (1 - fu) * (1 - fv) + t10 * fu * (1 - fv) +
            t01 * (1 - fu) * fv + t11 * fu * fv)
    src = texs * (np.array(tint, np.float32) / 255.0)[None, None, :] * alpha
    region = dst[y0:y1, x0:x1]
    region += np.where(m[:, :, None], src, 0.0)


def quad_void(dst, tex_rgb, tex_a, pos, size, rot, alpha):
    """Pase NonPremultiplied: dst = src.rgb·src.a + dst·(1−src.a) (ocluYE)."""
    th, tw = tex_rgb.shape[:2]
    if size[0] < 0.1 or size[1] < 0.1:
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
    fu2 = fu[:, :, 0]; fv2 = fv[:, :, 0]
    t00 = tex_a[v0, u0]; t10 = tex_a[v0, u1]; t01 = tex_a[v1, u0]; t11 = tex_a[v1, u1]
    a = (t00 * (1 - fu2) * (1 - fv2) + t10 * fu2 * (1 - fv2) +
         t01 * (1 - fu2) * fv2 + t11 * fu2 * fv2) * alpha
    r00 = tex_rgb[v0, u0]; r10 = tex_rgb[v0, u1]; r01 = tex_rgb[v1, u0]; r11 = tex_rgb[v1, u1]
    src = (r00 * (1 - fu) * (1 - fv) + r10 * fu * (1 - fv) +
           r01 * (1 - fu) * fv + r11 * fu * fv)  # ya premultiplicado (negro)
    region = dst[y0:y1, x0:x1]
    for ch in range(3):
        region[:, :, ch] = np.where(m, src[:, :, ch] * 1.0 + region[:, :, ch] * (1 - a), region[:, :, ch])


def apertura(progress):
    if progress < 0.08:
        t = progress / 0.08
        return 1 - (1 - t) ** 3
    if progress > 0.85:
        return max(0.0, 1 - (progress - 0.85) / 0.15)
    return 1.0


def lens(f):
    return np.clip(np.sin(np.pi * f) ** 0.6 * (1 + 0.15 * np.sin(4 * np.pi * f)), 0, 1)


def h01(seed, a, b):
    x = np.uint32((seed * 374761393 + a * 668265263 + b * 2246822519) & 0xFFFFFFFF)
    x = (x ^ (x >> np.uint32(13))) * np.uint32(1274126177)
    return ((x ^ (x >> np.uint32(16))) & np.uint32(0xFFFFFF)) / float(0xFFFFFF)


def camino_grieta(origin, dirv, seed, points=26, pmin=22.0, pmax=46.0, curv=8.0, fallas=4.0):
    pts = [np.array(origin, np.float64)]
    bearing0 = np.arctan2(dirv[1], dirv[0])
    drift = 0.0
    for i in range(1, points):
        g = (h01(seed, i, 101) - 0.5) * 2 * curv * (0.25 * i) * np.pi / 180
        drift = np.clip(drift + g, -0.9, 0.9)
        if h01(seed, i, 211) < 1 / fallas:
            drift = np.clip(drift + (h01(seed, i, 307) - 0.5) * 1.2, -0.9, 0.9)
        bearing = bearing0 + drift
        paso = np.linspace(pmin, pmax, 1)[0] if False else (pmin + (pmax - pmin) * h01(seed, i, 401))
        if i == points - 1:
            paso *= 0.5
        pts.append(pts[-1] + np.array([np.cos(bearing), np.sin(bearing)]) * paso)
    return np.array(pts)


def camino_vibracion(origin, dirv, length, amp, t, nodos=2):
    n = 23
    perp = np.array([-dirv[1], dirv[0]], np.float64)
    pts = []
    for i in range(n):
        f = i / (n - 1)
        onda = np.sin(f * np.pi * nodos) * np.sin(t * 2 * np.pi * 10)
        pts.append(np.array(origin) + dirv * (length * f) + perp * (onda * amp))
    return np.array(pts)


def ancho_camino(camino, maxw):
    total = sum(np.linalg.norm(camino[i] - camino[i - 1]) for i in range(1, len(camino)))
    ws = [maxw]
    arc = 0.0
    for i in range(1, len(camino)):
        arc += np.linalg.norm(camino[i] - camino[i - 1])
        ws.append(maxw * (1 - arc / total) ** 0.9)
    return np.array(ws)


def estrella_star(dst, center, charge, seed):
    """La estrella de ruptura (4 puntas DoG — simplificada del mock v626)."""
    k = 3.25 * charge * 1.12
    star_rgb, star_a = load_premult(os.path.join(PRO, 'Star.png'))
    for rot, largo in ((np.pi / 2, 8 * k * 2.4), (0, 5 * k * 2.4)):
        quad_additive(dst, star_rgb, center, (largo, max(1.4 * k * 0.6, 1.6)),
                      rot, PALETA['nucleo'], 0.9)
    quad_additive(dst, star_rgb, center, (8 * k * 2.4 * 0.8, max(1.2 * k * 0.5, 1.4)),
                  np.pi / 2, PALETA['cuerpo'], 0.55)


# ==================================================================
#  PANEL A — v1 RECONSTRUIDA: la TrailGlow por segmentos (EL DEFECTO)
# ==================================================================
def panel_v1(dst, origin, dirv, seed):
    tg_rgb, tg_a = load_premult(os.path.join(EFX, 'TrailGlow.png'))
    bd_rgb, bd_a = load_premult(os.path.join(PRO, 'BlackDisk.png'))
    rot = np.arctan2(dirv[1], dirv[0])
    perp = np.array([-dirv[1], dirv[0]])
    segs = int(TEAR_LEN / 48)
    for i in range(segs):
        f = (i + 0.5) / segs
        lensv = np.sin(f * np.pi) ** 0.6
        wseg = MAXW * lensv
        if wseg < 0.4:
            continue
        a = origin + dirv * (TEAR_LEN * i / segs)
        b = origin + dirv * (TEAR_LEN * (i + 1) / segs)
        mid = (a + b) / 2
        edge = 0.31 * wseg
        for lado in (-1, 1):
            lip = mid + perp * (edge * lado)
            quad_additive(dst, tg_rgb, lip, (TEAR_LEN / segs + wseg * 0.5, wseg * 0.34),
                          rot, PALETA['cuerpo'], 0.6)
            quad_additive(dst, tg_rgb, lip, (TEAR_LEN / segs + wseg * 0.3, max(wseg * 0.10, 0.8)),
                          rot, PALETA['nucleo'], 0.9)


# ==================================================================
#  PANEL B — v2: EL DESGARRO EN UN SOLO QUAD
# ==================================================================
def panel_v2_recta(dst, origin, dirv, seed, progress=0.45):
    velo, _ = load_premult(os.path.join(PRO, 'RiftTaperVelo.png'))
    cuerpo, _ = load_premult(os.path.join(PRO, 'RiftTaperCuerpo.png'))
    nucleo, _ = load_premult(os.path.join(PRO, 'RiftTaperNucleo.png'))
    void_rgb, void_a = load_premult(os.path.join(PRO, 'RiftTaperVoid.png'))
    rot = np.arctan2(dirv[1], dirv[0])
    h = 1.60 * MAXW * apertura(progress)
    center = origin + dirv * (TEAR_LEN / 2)

    # 1. EL VACÍO (ocluYE — la banda negra).
    quad_void(dst, void_rgb, void_a, center, (TEAR_LEN, h), rot, 0.96)
    # 2. LOS TRES QUADS DE LUZ (velo → cuerpo → núcleo).
    quad_additive(dst, velo, center, (TEAR_LEN, h), rot, PALETA['velo'], 0.30)
    quad_additive(dst, cuerpo, center, (TEAR_LEN, h), rot, PALETA['cuerpo'], 0.60)
    quad_additive(dst, nucleo, center, (TEAR_LEN, h), rot, PALETA['nucleo'], 0.90)

    # 3. LAS ESTRELLAS del interior (deterministas).
    orb_rgb, _ = load_premult(os.path.join(EFX, 'GlowOrb.png'))
    for j in range(16 + seed % 13):
        h2 = h01(seed, j, 7)
        h3 = h01(seed, j, 11)
        h4 = h01(seed, j, 17)
        x = h2 * TEAR_LEN
        f = x / TEAR_LEN
        half = 0.31 * MAXW * np.sin(f * np.pi) ** 0.6
        y = (h3 - 0.5) * 2 * half * 0.85
        tw = 0.45 + 0.55 * np.sin(h4 * 6.28 + j * 2.1)
        s = 1 + 2 * h4
        quad_additive(dst, orb_rgb, origin + dirv * x + perp_of(dirv) * y, (s, s), 0,
                      PALETA['nucleo'], tw)


def perp_of(d):
    return np.array([-d[1], d[0]], np.float64)


# ==================================================================
#  PANEL C — v2: LA HERIDA FRACTURADA (RiftLip + perlas)
# ==================================================================
def panel_v2_grieta(dst, origin, dirv, seed, progress=0.15):
    lip, _ = load_premult(os.path.join(PRO, 'RiftLip.png'))
    core, _ = load_premult(os.path.join(PRO, 'RiftCore.png'))
    bd_rgb, bd_a = load_premult(os.path.join(PRO, 'BlackDisk.png'))
    camino = camino_grieta(origin, dirv, seed)
    ws = ancho_camino(camino, MAXW)
    vida = (1 - progress) ** 0.8

    # 1. EL VACÍO por segmento (BlackDisk estirado — extremos redondos).
    for i in range(len(camino) - 1):
        a, b = camino[i], camino[i + 1]
        wseg = (ws[i] + ws[i + 1]) / 2
        if wseg < 0.5:
            continue
        rot = np.arctan2(b[1] - a[1], b[0] - a[0])
        quad_void(dst, bd_rgb, bd_a, (a + b) / 2,
                  (np.linalg.norm(b - a) + wseg * 0.35, wseg * 0.62 * vida + 0.8), rot, 0.94 * vida)

    # 2. LA CADENA SIN HUECOS: velo + cuerpo + núcleo + PERLA por vértice.
    for i in range(len(camino) - 1):
        a, b = camino[i], camino[i + 1]
        wseg = (ws[i] + ws[i + 1]) / 2
        if wseg < 0.4:
            continue
        rot = np.arctan2(b[1] - a[1], b[0] - a[0])
        ln = np.linalg.norm(b - a) + wseg * 0.9
        mid = (a + b) / 2
        quad_additive(dst, lip, mid, (ln, wseg * 1.6), rot, PALETA['velo'], 0.30 * vida)
        quad_additive(dst, lip, mid, (ln, wseg), rot, PALETA['cuerpo'], 0.60 * vida)
        quad_additive(dst, core, mid, (ln, wseg * 0.8), rot, PALETA['nucleo'], 0.90 * vida)
        if i > 0:
            # LA PERLA: el round-join que mata los huecos de las esquinas.
            quad_additive(dst, lip, a, (wseg * 1.15, wseg * 1.6), rot, PALETA['velo'], 0.30 * vida)
            quad_additive(dst, lip, a, (wseg * 1.15, wseg), rot, PALETA['cuerpo'], 0.60 * vida)
            quad_additive(dst, core, a, (wseg * 1.15, wseg * 0.8), rot, PALETA['nucleo'], 0.90 * vida)

    estrella_star(dst, camino[0], 0.9, seed)


# ==================================================================
#  PANEL D — v2: LA VIBRACIÓN (onda estacionaria creciendo)
# ==================================================================
def panel_v2_vibracion(dst, origin, dirv, seed, amp=3.5, t=0.0):
    lip, _ = load_premult(os.path.join(PRO, 'RiftLip.png'))
    core, _ = load_premult(os.path.join(PRO, 'RiftCore.png'))
    camino = camino_vibracion(origin, dirv, TEAR_LEN, amp, t)
    ws = ancho_camino(camino, MAXW)
    for i in range(len(camino) - 1):
        a, b = camino[i], camino[i + 1]
        wseg = (ws[i] + ws[i + 1]) / 2
        rot = np.arctan2(b[1] - a[1], b[0] - a[0])
        ln = np.linalg.norm(b - a) + wseg * 0.9
        mid = (a + b) / 2
        quad_additive(dst, lip, mid, (ln, wseg * 1.6), rot, PALETA['velo'], 0.30)
        quad_additive(dst, lip, mid, (ln, wseg), rot, PALETA['cuerpo'], 0.60)
        quad_additive(dst, core, mid, (ln, wseg * 0.8), rot, PALETA['nucleo'], 0.90)
        if i > 0:
            quad_additive(dst, lip, a, (wseg * 1.15, wseg * 1.6), rot, PALETA['velo'], 0.30)
            quad_additive(dst, lip, a, (wseg * 1.15, wseg), rot, PALETA['cuerpo'], 0.60)
            quad_additive(dst, core, a, (wseg * 1.15, wseg * 0.8), rot, PALETA['nucleo'], 0.90)


# ==================================================================
#  EL VEREDICTO NUMÉRICO (huecos e interrupciones a lo largo de la línea)
# ==================================================================
def _env_franja(dst, y_lo, y_hi, x_lo, x_hi):
    zone = dst[max(0, y_lo):y_hi, x_lo:x_hi]
    lum = zone[:, :, 0] * 0.30 + zone[:, :, 1] * 0.55 + zone[:, :, 2] * 0.15
    return lum.max(axis=0)


def _env_camino(dst, camino, half=14, x_lo=180, x_hi=600):
    """La envolvente que SIGUE la polilínea POR ARCO (muestreo denso a lo largo
    del camino — cada 4 px de arco, el brillo máx en ±half px del punto)."""
    # Densificar la polilínea cada 4 px de arco.
    pts = []
    for i in range(len(camino) - 1):
        a, b = np.asarray(camino[i], float), np.asarray(camino[i + 1], float)
        n = max(1, int(np.linalg.norm(b - a) / 4))
        for t in np.linspace(0, 1, n, endpoint=False):
            pts.append(a + (b - a) * t)
    pts.append(np.asarray(camino[-1], float))
    env = []
    for p in pts:
        x0 = int(max(0, p[0] - 3)); x1 = int(min(dst.shape[1], p[0] + 4))
        y0 = int(max(0, p[1] - half)); y1 = int(min(dst.shape[0], p[1] + half))
        if x1 <= x0 or y1 <= y0:
            env.append(0.0)
            continue
        zone = dst[y0:y1, x0:x1]
        lum = zone[:, :, 0] * 0.30 + zone[:, :, 1] * 0.55 + zone[:, :, 2] * 0.15
        env.append(float(lum.max()))
    return np.array(env)


def veredicto(dst, y_lo, y_hi, label, x_lo=60, x_hi=860, camino=None):
    """La ENVOLVENTE de brillo a lo largo de la línea (franja recta o siguiendo
    el camino). Huecos = tramos con señal < 10% del pico; interrupciones =
    tramos < 35% del pico (los 'dash gaps' del usuario)."""
    env = _env_camino(dst, camino, x_lo=x_lo, x_hi=x_hi) if camino is not None \
        else _env_franja(dst, y_lo, y_hi, x_lo, x_hi)
    pico = env.max()
    if pico <= 0:
        print(f"  · {label}: VACÍO (error)")
        return
    k = np.ones(3) / 3
    envs = np.convolve(env, k, mode='same')
    huecos = int((envs < 0.10 * pico).sum())
    bajas_px = int((envs < 0.35 * pico).sum())
    print(f"  · {label}: pico={pico:6.1f} huecos(<10%)={huecos:3d}px en baja(<35%)={bajas_px:3d}px "
          f"→ {'CONTINUA' if huecos == 0 and bajas_px < 20 else 'INTERRUMPIDA'}")


def main():
    print("mock_rift_v628.py — EL MOCK 1:1 DEL DESGARRO CONTINUO (RiftLib v2)")
    np.seterr(all='ignore')

    dirv = np.array([1.0, 0.0])                        # horizontal: la métrica es exacta
    seed = 747

    canvas = np.zeros((H * 4, W, 3), np.float32)
    fondo = np.zeros((H, W, 3), np.float32) + np.array([26, 18, 38], np.float32)  # noche violeta

    # PANEL A (fila 0): v1 — el defecto.
    panelA = fondo.copy()
    panel_v1(panelA, np.array([80.0, H * 0.5]), dirv, seed)
    canvas[0:H] = panelA
    # PANEL B (fila 1): v2 — la recta en UN QUAD.
    panelB = fondo.copy()
    panel_v2_recta(panelB, np.array([80.0, H * 0.5]), dirv, seed)
    estrella_star(panelB, np.array([80.0, H * 0.5]), 0.5, seed)
    canvas[H:2 * H] = panelB
    # PANEL C (fila 2): v2 — la herida fracturada.
    panelC = fondo.copy()
    panel_v2_grieta(panelC, np.array([80.0, H * 0.45]), dirv, seed)
    canvas[2 * H:3 * H] = panelC
    # PANEL D (fila 3): v2 — la vibración.
    panelD = fondo.copy()
    panel_v2_vibracion(panelD, np.array([80.0, H * 0.5]), dirv, seed, amp=3.5, t=0.31)
    canvas[3 * H:4 * H] = panelD

    out = np.clip(canvas, 0, 255).astype(np.uint8)
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'mock_rift_v628.png')
    Image.fromarray(out).save(path)
    print(f"  + {path} ({W}x{H * 4})")

    print("\nEL VEREDICTO NUMÉRICO (la envolvente de brillo a lo largo de la línea):")
    # SOLO LA ZONA SOSTENIDA (el 15-85% de la línea — las puntas aguja
    # legítimamente se apagan por el lens; la CONTINUIDAD se exige al cuerpo).
    veredicto(canvas[0:H], H // 2 - 24, H // 2 + 24, "A· v1 TrailGlow por segmentos ", 180, 600)
    veredicto(canvas[H:2 * H], H // 2 - 24, H // 2 + 24, "B· v2 UN QUAD RiftTaper     ", 180, 600)
    veredicto(canvas[2 * H:3 * H], 0, H, "C· v2 grieta RiftLip+perlas ", 180, 600,
              camino=camino_grieta(np.array([80.0, H * 0.45]), dirv, seed))
    veredicto(canvas[3 * H:4 * H], 0, H, "D· v2 vibración             ", 180, 600,
              camino=camino_vibracion(np.array([80.0, H * 0.5]), dirv, TEAR_LEN, 3.5, 0.31))


if __name__ == '__main__':
    main()
