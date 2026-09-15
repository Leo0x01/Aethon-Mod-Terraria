#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_v629_assets.py — LOS PNGs DE v6.29.

1. LAS DIEZ BOLSAS POR CATEGORÍA (30×30): la silueta de saco cosmológico
   de la casa, cada una con SU COLOR DE CATEGORÍA y su emblema frontal:
     · BolsaProbador        — oro del kit + llave
     · BolsaFundacionales   — cian + estrella nova
     · BolsaClasicosCosmicos— dorado clásico + galaxia espiral
     · BolsaAgujerosNegros  — violeta + anillo de acreción
     · BolsaSolesRunicos    — naranja solar + disco con rayos
     · BolsaEstrellasReales — azul estelar + constelación
     · BolsaArmasLibrerias  — prisma + rayo
     · BolsaBastonesCreativos— teal + reloj de arena
     · BolsaExhumados       — carmesí-hueso + sello exhumado
     · BolsaCosmeticos      — rosa + corona
2. EL RENCOR PRIMORDIAL (28×30): el TOMO oscuro con el círculo de
   transmutación rúnico carmesí-ámbar en la tapa.
3. LA EMINENCIA ATROZ (30×30): la masa espectral pálida con los ojos
   carmesí asomando.
4. LAS DOS SOMBRAS de proyectil (76×76): el círculo del rencor y la
   congregación de espíritus (requisito de autoload de la casa).

Patrón PIL+numpy de la casa (gen_v627_assets.py).
"""
import os
import numpy as np
from PIL import Image

PROJ_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                        '..', 'AethonMod')
BOLSA_OUT = os.path.join(PROJ_DIR, 'Content', 'Items', 'Bolsas')
WPN_OUT = os.path.join(PROJ_DIR, 'Content', 'Weapons', 'Cosmic')
PRJ_OUT = os.path.join(PROJ_DIR, 'Content', 'Projectiles', 'Cosmic')

SS = 8  # supersampling


# =====================================================================
#  LOS HELPERS DE LA CASA
# =====================================================================

def new_canvas(w, h):
    return np.zeros((h * SS, w * SS, 4), dtype=np.float32)


def downsample(img, w, h):
    pil = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), 'RGBA')
    pil = pil.resize((w, h), Image.LANCZOS)
    return np.array(pil, dtype=np.uint8)


def add_color(img, px, py, radius, color, alpha):
    """Añade luz (aditiva por canal) con caída gaussiana."""
    H, W = img.shape[0], img.shape[1]
    if radius <= 0.1 or alpha <= 0.01:
        return
    x0, x1 = max(0, int(px - radius * 3)), min(W, int(px + radius * 3) + 1)
    y0, y1 = max(0, int(py - radius * 3)), min(H, int(py + radius * 3) + 1)
    if x0 >= x1 or y0 >= y1:
        return
    xs = np.arange(x0, x1) - px
    ys = np.arange(y0, y1) - py
    gx = np.exp(-(xs / radius) ** 2)
    gy = np.exp(-(ys / radius) ** 2)
    g = np.outer(gy, gx) * alpha
    img[y0:y1, x0:x1, 0] += g * color[0]
    img[y0:y1, x0:x1, 1] += g * color[1]
    img[y0:y1, x0:x1, 2] += g * color[2]
    img[y0:y1, x0:x1, 3] = np.maximum(img[y0:y1, x0:x1, 3], g * 255.0)


def set_alpha_floor(img):
    """El alfa nunca baja del brillo máximo (sprites de luz)."""
    lum = img[:, :, :3].max(axis=2)
    img[:, :, 3] = np.maximum(img[:, :, 3], lum)


def add_line(img, x0, y0, x1, y1, width, color, alpha=1.0):
    """Una línea aditiva con grosor (cápsula)."""
    L = np.hypot(x1 - x0, y1 - y0)
    if L < 0.5:
        return
    n = max(2, int(L / (0.6 * SS)))
    for t in np.linspace(0, 1, n):
        px = x0 + (x1 - x0) * t
        py = y0 + (y1 - y0) * t
        add_color(img, px, py, width * 0.5, color, alpha)


def add_ring(img, cx, cy, radius, width, color, alpha=1.0):
    """Un anillo aditivo."""
    H, W = img.shape[0], img.shape[1]
    ys = np.arange(H)
    xs = np.arange(W)
    XX, YY = np.meshgrid(xs, ys)
    rr = np.sqrt((XX - cx) ** 2 + (YY - cy) ** 2)
    aro = np.exp(-np.abs(rr - radius) / width) * alpha
    img[:, :, 0] += aro * color[0]
    img[:, :, 1] += aro * color[1]
    img[:, :, 2] += aro * color[2]
    img[:, :, 3] = np.maximum(img[:, :, 3], aro * 255)


# =====================================================================
#  1. LAS DIEZ BOLSAS (30×30) — una por categoría
# =====================================================================

def gen_bolsa(nombre, cuerpo, borde, interior, cordel, emblema):
    """
    La bolsa de la casa parametrizada:
      cuerpo  = (r,g,b) del saco base
      borde   = color del limbo que deja ver la luz
      interior= color del cosmos guardado
      cordel  = color del lazo
      emblema = función(cx, cy, img) que dibuja el sello frontal
    """
    W, H = 30, 30
    img = new_canvas(W, H)
    cx = W * SS / 2
    cy = H * SS / 2 + 1.5 * SS

    # --- EL CUERPO DEL SACO ---
    ys = np.arange(H * SS)
    xs = np.arange(W * SS)
    XX, YY = np.meshgrid(xs, ys)
    body_r = 11.2 * SS
    body_cy = cy + 2.2 * SS
    ell = ((XX - cx) / body_r) ** 2 + ((YY - body_cy) / (9.6 * SS)) ** 2
    neck_half = 3.4 * SS
    neck = (np.abs(XX - cx) < neck_half) & \
           (YY > cy - 7.5 * SS) & (YY < body_cy - 3.0 * SS)
    shape = (ell < 1.0) | neck

    # El interior: gradiente radial del color de la categoría.
    dist = np.sqrt(((XX - cx) / body_r) ** 2 + ((YY - body_cy) / (9.6 * SS)) ** 2)
    dist = np.clip(dist, 0, 1)
    for c in range(3):
        base_c = interior[c]
        edge_c = interior[c] // 5
        channel = base_c + (edge_c - base_c) * dist
        img[:, :, c] += np.where(shape, channel, 0.0).astype(np.float32)
    img[:, :, 3] += np.where(shape, 250.0, 0.0)

    # --- EL LIMBO LUMINOSO ---
    rim = np.exp(-np.abs(ell - 0.92) * 9.0)
    img[:, :, 0] += rim * borde[0]
    img[:, :, 1] += rim * borde[1]
    img[:, :, 2] += rim * borde[2]
    img[:, :, 3] = np.maximum(img[:, :, 3], rim * 210)

    # --- EL CORDEL ---
    add_color(img, cx - neck_half - 1.2 * SS, cy - 6.6 * SS, 1.5 * SS, cordel, 0.95)
    add_color(img, cx + neck_half + 1.2 * SS, cy - 6.6 * SS, 1.5 * SS, cordel, 0.95)
    add_color(img, cx, cy - 7.8 * SS, 1.9 * SS, cordel, 1.0)

    # --- EL EMBLEMA DE LA CATEGORÍA ---
    emblema(cx, body_cy - 0.6 * SS, img)

    set_alpha_floor(img)
    out = downsample(img, W, H)
    Image.fromarray(out).save(os.path.join(BOLSA_OUT, nombre + '.png'))
    print(f"  ✓ Bolsas/{nombre}.png ({W}×{H})")


# ---------------------------------------------------------------------
#  LOS EMBLEMAS (el sello frontal de cada categoría)
# ---------------------------------------------------------------------

def emblema_llave(cx, cy, img):
    """EL PROBADOR: la llave del kit (el aro + el vástago dentado)."""
    add_ring(img, cx, cy - 2.2 * SS, 2.4 * SS, 0.75 * SS, (255, 226, 130), 0.95)
    add_line(img, cx, cy, cx, cy + 5.6 * SS, 1.7 * SS, (255, 226, 130), 0.95)
    add_line(img, cx, cy + 4.6 * SS, cx + 2.4 * SS, cy + 4.6 * SS, 1.2 * SS,
             (255, 226, 130), 0.9)
    add_line(img, cx, cy + 6.2 * SS, cx + 1.8 * SS, cy + 6.2 * SS, 1.2 * SS,
             (255, 226, 130), 0.9)


def emblema_nova(cx, cy, img):
    """LOS FUNDACIONALES: la nova de 4 puntas cian."""
    for k in range(4):
        a = k * np.pi / 2 + 0.25
        ex = cx + np.cos(a) * 4.6 * SS
        ey = cy + np.sin(a) * 4.6 * SS
        for tt in np.linspace(0, 1, 16):
            px = cx + (ex - cx) * tt
            py = cy + (ey - cy) * tt
            wdt = (1.8 - 1.5 * tt) * SS
            add_color(img, px, py, wdt * 0.55, (120, 230, 255), 0.95)
    add_color(img, cx, cy, 1.6 * SS, (240, 252, 255), 1.0)


def emblema_galaxia(cx, cy, img):
    """LOS CLÁSICOS CÓSMICOS: la galaxia espiral dorada."""
    for brazo in range(2):
        a0 = brazo * np.pi
        for tt in np.linspace(0.15, 1.0, 22):
            a = a0 + tt * 2.6
            r = 1.4 * SS + tt * 4.4 * SS
            px = cx + np.cos(a) * r
            py = cy + np.sin(a) * r * 0.75
            add_color(img, px, py, (1.3 - 0.7 * tt) * SS, (255, 216, 120), 0.8)
    add_color(img, cx, cy, 1.7 * SS, (255, 246, 200), 1.0)


def emblema_agujero(cx, cy, img):
    """LOS AGUJEROS NEGROS: el anillo de acreción con el disco oscuro."""
    add_ring(img, cx, cy, 4.8 * SS, 0.85 * SS, (190, 110, 255), 0.95)
    add_ring(img, cx, cy, 3.4 * SS, 0.55 * SS, (240, 190, 255), 0.55)
    # EL DISCO NEGRO (el horizonte)
    H, W = img.shape[0], img.shape[1]
    ys = np.arange(H)
    xs = np.arange(W)
    XX, YY = np.meshgrid(xs, ys)
    rr = np.sqrt((XX - cx) ** 2 + (YY - cy) ** 2)
    disco = np.clip(1.0 - rr / (3.0 * SS), 0, 1) ** 0.5
    img[:, :, 0] *= (1 - disco * 0.97)
    img[:, :, 1] *= (1 - disco * 0.97)
    img[:, :, 2] *= (1 - disco * 0.97)
    img[:, :, 3] = np.maximum(img[:, :, 3], disco * 255)


def emblema_sol(cx, cy, img):
    """LOS SOLES RÚNICOS: el disco solar con rayos orbitales."""
    add_color(img, cx, cy, 3.6 * SS, (255, 170, 60), 0.95)
    add_color(img, cx, cy, 1.9 * SS, (255, 236, 170), 1.0)
    for k in range(8):
        a = k * np.pi / 4 + 0.2
        r0, r1 = 4.4 * SS, 6.4 * SS
        add_line(img, cx + np.cos(a) * r0, cy + np.sin(a) * r0,
                 cx + np.cos(a) * r1, cy + np.sin(a) * r1,
                 1.1 * SS, (255, 200, 90), 0.9)


def emblema_constelacion(cx, cy, img):
    """LAS ESTRELLAS REALES: la constelación (puntos GRANDES + hilos + núcleo)."""
    # EL NÚCLEO (la estrella central mayor — el ancla visual).
    add_color(img, cx - 0.8 * SS, cy + 0.6 * SS, 2.2 * SS, (170, 210, 255), 0.95)
    add_color(img, cx - 0.8 * SS, cy + 0.6 * SS, 1.0 * SS, (255, 255, 255), 1.0)
    pts = [(-4.6, -3.4), (-2.2, 1.8), (3.0, -2.6), (4.2, 2.8), (-2.8, 4.0)]
    for (px, py) in pts:
        add_color(img, cx + px * SS, cy + py * SS, 1.6 * SS, (190, 220, 255), 0.95)
        add_color(img, cx + px * SS, cy + py * SS, 0.65 * SS, (255, 255, 255), 1.0)
    orden = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 0), (1, 4)]
    for (i, j) in orden:
        a, b = pts[i], pts[j]
        add_line(img, cx + a[0] * SS, cy + a[1] * SS,
                 cx + b[0] * SS, cy + b[1] * SS, 0.7 * SS, (140, 190, 255), 0.65)


def emblema_rayo(cx, cy, img):
    """LAS LIBRERÍAS: el rayo prisma (el concierto de StormLib)."""
    camino = [(0.6, -5.2), (-1.4, -1.2), (0.9, -1.2), (-0.7, 5.0)]
    for i in range(len(camino) - 1):
        a, b = camino[i], camino[i + 1]
        col = [(255, 130, 220), (190, 130, 255), (130, 220, 255)][i]
        add_line(img, cx + a[0] * SS, cy + a[1] * SS,
                 cx + b[0] * SS, cy + b[1] * SS, 1.8 * SS, col, 0.9)
    add_color(img, cx + camino[-1][0] * SS, cy + camino[-1][1] * SS,
              1.4 * SS, (255, 255, 255), 0.9)


def emblema_reloj(cx, cy, img):
    """LOS CREATIVOS: el reloj de arena (dos triángulos de luz)."""
    add_line(img, cx - 4.2 * SS, cy - 4.4 * SS, cx + 4.2 * SS, cy - 4.4 * SS,
             1.0 * SS, (120, 240, 214), 0.9)
    add_line(img, cx - 4.2 * SS, cy + 4.4 * SS, cx + 4.2 * SS, cy + 4.4 * SS,
             1.0 * SS, (120, 240, 214), 0.9)
    # LOS TRIÁNGULOS
    for s in (-1, 1):
        pts = [(cx - 3.6 * SS, cy + s * 4.0 * SS), (cx + 3.6 * SS, cy + s * 4.0 * SS),
               (cx, cy + s * 0.4 * SS)]
        for i in range(3):
            a, b = pts[i], pts[(i + 1) % 3]
            add_line(img, a[0], a[1], b[0], b[1], 0.8 * SS, (120, 240, 214), 0.75)
    add_color(img, cx, cy, 1.1 * SS, (240, 255, 250), 0.95)


def emblema_exhumado(cx, cy, img):
    """LOS EXHUMADOS: el sello exhumado — el círculo partido + la chispa."""
    # El círculo de transmutación en miniatura (dorado-carmesí).
    add_ring(img, cx, cy, 4.6 * SS, 0.7 * SS, (255, 120, 140), 0.9)
    add_ring(img, cx, cy, 3.1 * SS, 0.5 * SS, (255, 210, 130), 0.7)
    # LA ESTRELLA interior (el homenaje FMA en 5 puntas).
    pts = []
    for k in range(5):
        a = k * 2 * np.pi / 5 - np.pi / 2
        pts.append((cx + np.cos(a) * 2.6 * SS, cy + np.sin(a) * 2.6 * SS))
    orden = [0, 2, 4, 1, 3, 0]
    for i in range(len(orden) - 1):
        a, b = pts[orden[i]], pts[orden[i + 1]]
        add_line(img, a[0], a[1], b[0], b[1], 0.62 * SS, (255, 244, 214), 0.9)
    add_color(img, cx, cy, 0.9 * SS, (255, 250, 240), 1.0)


def emblema_corona(cx, cy, img):
    """LOS COSMÉTICOS: la corona rosa."""
    base_y = cy + 3.0 * SS
    add_line(img, cx - 4.4 * SS, base_y, cx + 4.4 * SS, base_y, 1.0 * SS,
             (255, 160, 225), 0.95)
    picos = [(-4.4, 3.0), (-2.2, -2.0), (0, -4.2), (2.2, -2.0), (4.4, 3.0)]
    for i in range(len(picos) - 1):
        a, b = picos[i], picos[i + 1]
        add_line(img, cx + a[0] * SS, cy + a[1] * SS,
                 cx + b[0] * SS, cy + b[1] * SS, 0.9 * SS, (255, 160, 225), 0.85)
    for (px, py) in [(-2.2, -2.0), (0, -4.2), (2.2, -2.0)]:
        add_color(img, cx + px * SS, cy + py * SS, 0.8 * SS, (255, 220, 245), 1.0)
    add_color(img, cx, base_y - 0.6 * SS, 0.7 * SS, (255, 235, 250), 0.95)


# =====================================================================
#  2. EL RENCOR PRIMORDIAL (28×30) — el tomo del ritual
# =====================================================================

def gen_rencor():
    W, H = 28, 30
    img = new_canvas(W, H)
    cx = W * SS / 2
    ys = np.arange(H * SS)
    xs = np.arange(W * SS)
    XX, YY = np.meshgrid(xs, ys)

    # --- EL CUERPO DEL TOMO (tapa + lomo) ---
    tapa_r = 9.6 * SS
    tapa_cy = 15.4 * SS
    dentro = (np.abs(XX - cx - 1.2 * SS) < tapa_r) & \
             (np.abs(YY - tapa_cy) < 9.2 * SS)
    # Redondeo de esquinas suave.
    dist_c = np.sqrt(np.maximum(np.abs(XX - cx - 1.2 * SS) - tapa_r * 0.55, 0) ** 2 +
                     np.maximum(np.abs(YY - tapa_cy) - 9.2 * SS * 0.55, 0) ** 2)
    dentro = dist_c < 6.4 * SS

    # La tapa: cuero carmesí muy oscuro (el tomo del rencor).
    t_cuero = np.clip(dist_c / (6.4 * SS), 0, 1)
    img[:, :, 0] += np.where(dentro, 88 + 62 * (1 - t_cuero), 0)
    img[:, :, 1] += np.where(dentro, 18 + 16 * (1 - t_cuero), 0)
    img[:, :, 2] += np.where(dentro, 42 + 30 * (1 - t_cuero), 0)
    img[:, :, 3] += np.where(dentro, 252.0, 0.0)

    # EL LOMO (la izquierda del libro, más oscura + costuras).
    lomo = (np.abs(XX - cx + 7.4 * SS) < 2.2 * SS) & dentro
    img[:, :, 0] += np.where(lomo, -30, 0)
    img[:, :, 1] += np.where(lomo, -4, 0)
    img[:, :, 2] += np.where(lomo, -12, 0)

    # --- EL BORDE DORADO DE LAS PÁGINAS (la derecha) ---
    for k, yy in enumerate(np.linspace(tapa_cy - 7.4 * SS, tapa_cy + 7.4 * SS, 7)):
        add_color(img, cx + 9.3 * SS, yy, 0.62 * SS, (255, 220, 140), 0.75)

    # --- EL CÍRCULO DE TRANSMUTACIÓN EN LA TAPA ---
    sccx = cx + 1.6 * SS
    sccy = tapa_cy - 0.6 * SS
    # EL ANILLO EXTERIOR dorado (MÁS GRUESO Y BRILLANTE).
    add_ring(img, sccx, sccy, 5.9 * SS, 0.80 * SS, (255, 210, 110), 1.0)
    # EL ANILLO INTERIOR carmesí.
    add_ring(img, sccx, sccy, 4.2 * SS, 0.55 * SS, (255, 110, 130), 0.95)
    # LA ESTRELLA DE 5 PUNTAS (el homenaje FMA).
    pts = []
    for k in range(5):
        a = k * 2 * np.pi / 5 - np.pi / 2
        pts.append((sccx + np.cos(a) * 3.4 * SS, sccy + np.sin(a) * 3.4 * SS))
    orden = [0, 2, 4, 1, 3, 0]
    for i in range(len(orden) - 1):
        a, b = pts[orden[i]], pts[orden[i + 1]]
        add_line(img, a[0], a[1], b[0], b[1], 0.80 * SS, (255, 250, 226), 1.0)
    # LAS 6 RUNAS del anillo (muescas radiales doradas).
    for k in range(6):
        a = k * np.pi / 3 + 0.35
        add_color(img, sccx + np.cos(a) * 5.05 * SS, sccy + np.sin(a) * 5.05 * SS,
                  1.0 * SS, (255, 244, 190), 1.0)
    # EL CORAZÓN del círculo (la chispa del ritual).
    add_color(img, sccx, sccy, 1.9 * SS, (255, 150, 90), 1.0)
    add_color(img, sccx, sccy, 0.9 * SS, (255, 252, 244), 1.0)

    # --- EL BROCHE (el cierre del tomo) ---
    add_color(img, cx - 7.2 * SS, tapa_cy - 6.6 * SS, 1.2 * SS, (255, 214, 110), 0.9)
    add_color(img, cx - 7.2 * SS, tapa_cy + 6.6 * SS, 1.2 * SS, (255, 214, 110), 0.9)

    set_alpha_floor(img)
    out = downsample(img, W, H)
    Image.fromarray(out).save(os.path.join(WPN_OUT, 'RencorPrimordialStaff.png'))
    print(f"  ✓ RencorPrimordialStaff.png ({W}×{H})")


# =====================================================================
#  3. LA EMINENCIA ATROZ (30×30) — la masa espectral
# =====================================================================

def gen_eminencia():
    W, H = 30, 30
    img = new_canvas(W, H)
    cx = W * SS / 2
    ys = np.arange(H * SS)
    xs = np.arange(W * SS)
    XX, YY = np.meshgrid(xs, ys)

    # --- EL CUERPO DEL FANTASMA (pixel-art DURO: cúpula + falda ondulada) ---
    # La cúpula: semicírculo superior.
    top_cy = 13.0 * SS
    dome = np.sqrt((XX - cx) ** 2 + (YY - top_cy) ** 2) < 8.6 * SS
    dome &= (YY <= top_cy)
    # El cuerpo: rectángulo con la falda ONDULADA abajo.
    faldón = np.abs(XX - cx) < 8.6 * SS
    faldón &= (YY > top_cy) & (YY < 24.5 * SS)
    # La onda de la falda: seno que corta los picos.
    onda_y = 22.2 * SS + 2.3 * SS * np.sin((XX - cx) / (8.6 * SS) * np.pi * 3.0 + np.pi * 0.5)
    faldón &= (YY < np.maximum(onda_y, 24.5 * SS * 0))
    faldón &= (YY < 24.6 * SS)
    cuerpo = dome | faldón

    # El RELLENO SÓLIDO (pálido de hueso — sin degradé).
    img[:, :, 0] += np.where(cuerpo, 232.0, 0)
    img[:, :, 1] += np.where(cuerpo, 240.0, 0)
    img[:, :, 2] += np.where(cuerpo, 242.0, 0)
    img[:, :, 3] += np.where(cuerpo, 255.0, 0)

    # LA SOMBRA INTERIOR (el lado derecho del cuerpo, más gris).
    sombra_lado = cuerpo & (XX > cx + 3.2 * SS)
    img[:, :, 0] -= np.where(sombra_lado, 34.0, 0)
    img[:, :, 1] -= np.where(sombra_lado, 26.0, 0)
    img[:, :, 2] -= np.where(sombra_lado, 24.0, 0)

    # EL CONTORNO OSCURO DURO (2 px de línea violeta-negra).
    from scipy import ndimage as _nd
    cuerpo8 = cuerpo.astype(np.uint8)
    borde = (_nd.binary_dilation(cuerpo8, iterations=2).astype(bool) &
             ~_nd.binary_erosion(cuerpo8, iterations=2).astype(bool))
    img[:, :, 0] = np.where(borde, 44.0, img[:, :, 0])
    img[:, :, 1] = np.where(borde, 26.0, img[:, :, 1])
    img[:, :, 2] = np.where(borde, 56.0, img[:, :, 2])
    img[:, :, 3] = np.where(borde, 255.0, img[:, :, 3])

    # --- LOS DOS OJOS (zócalos OSCUROS + escleróticas + pupilas carmesí) ---
    ojos = [(cx - 3.4 * SS, top_cy - 1.2 * SS, 2.2 * SS),
            (cx + 3.4 * SS, top_cy - 1.2 * SS, 2.2 * SS)]
    for (ox, oy, orr) in ojos:
        zócalo = (np.abs(XX - ox) < 1.7 * SS) & (np.abs(YY - oy) < 2.3 * SS)
        img[:, :, 0] = np.where(zócalo, 30.0, img[:, :, 0])
        img[:, :, 1] = np.where(zócalo, 14.0, img[:, :, 1])
        img[:, :, 2] = np.where(zócalo, 34.0, img[:, :, 2])
        # La esclerótica (blanca dura) dentro del zócalo.
        img[:, :, 0] = np.where(zócalo & (np.abs(XX - ox) < 1.15 * SS) &
                                (np.abs(YY - oy) < 1.5 * SS), 250.0, img[:, :, 0])
        img[:, :, 1] = np.where(zócalo & (np.abs(XX - ox) < 1.15 * SS) &
                                (np.abs(YY - oy) < 1.5 * SS), 252.0, img[:, :, 1])
        img[:, :, 2] = np.where(zócalo & (np.abs(XX - ox) < 1.15 * SS) &
                                (np.abs(YY - oy) < 1.5 * SS), 255.0, img[:, :, 2])
        # La pupila CARMESÍ (mirando al centro-abajo).
        add_color(img, ox + 0.4 * SS, oy + 0.5 * SS, 0.62 * SS, (255, 40, 70), 1.0)

    # --- LA BOCA ABIERTA (el óvalo oscuro del grito) ---
    boca = ((XX - cx) ** 2 / (2.6 * SS) ** 2 +
            (YY - (top_cy + 3.6 * SS)) ** 2 / (1.9 * SS) ** 2) < 1.0
    img[:, :, 0] = np.where(boca, 26.0, img[:, :, 0])
    img[:, :, 1] = np.where(boca, 10.0, img[:, :, 1])
    img[:, :, 2] = np.where(boca, 28.0, img[:, :, 2])
    # La lengua carmesí.
    add_color(img, cx, top_cy + 4.4 * SS, 0.7 * SS, (255, 70, 95), 1.0)

    # --- LAS BRASAS CARMESÍ (los espíritus atrapados en la falda) ---
    for (px, py, pr) in [(-6.6, 20.6, 0.75), (6.8, 19.8, 0.65), (0.4, 23.4, 0.55)]:
        add_color(img, cx + px * SS, py * SS, pr * SS, (255, 80, 105), 1.0)

    # --- EL AURA (UN anillo tenue — el toque espectral) ---
    add_ring(img, cx, top_cy + 2.0 * SS, 11.6 * SS, 1.4 * SS, (150, 170, 200), 0.30)

    set_alpha_floor(img)
    out = downsample(img, W, H)
    Image.fromarray(out).save(os.path.join(WPN_OUT, 'EminenciaAtrozStaff.png'))
    print(f"  ✓ EminenciaAtrozStaff.png ({W}×{H})")


# =====================================================================
#  4. LAS DOS SOMBRAS DE PROYECTIL (76×76)
# =====================================================================

def sombra_base():
    """El disco oscuro con rim — el requisito de autoload de la casa."""
    W, H = 76, 76
    img = new_canvas(W, H)
    cx, cy = W * SS / 2, H * SS / 2
    return img, cx, cy, W, H


def gen_sombra_rencor():
    img, cx, cy, W, H = sombra_base()
    ys = np.arange(H * SS)
    xs = np.arange(W * SS)
    XX, YY = np.meshgrid(xs, ys)
    rr = np.sqrt((XX - cx) ** 2 + (YY - cy) ** 2)

    # EL DISCO OSCURO (la sombra del círculo del ritual).
    disco = np.clip(1.0 - rr / (26 * SS), 0, 1) ** 0.6
    img[:, :, 0] += disco * 34
    img[:, :, 1] += disco * 16
    img[:, :, 2] += disco * 24
    img[:, :, 3] += disco * 200

    # EL RIM CARMESÍ (la luz que se escapa del borde).
    rim = np.exp(-np.abs(rr - 26 * SS) / (2.4 * SS))
    img[:, :, 0] += rim * 200
    img[:, :, 1] += rim * 40
    img[:, :, 2] += rim * 70
    img[:, :, 3] = np.maximum(img[:, :, 3], rim * 220)

    # LA ESTRELLA tenue dentro (la sombra del sello FMA).
    pts = []
    for k in range(5):
        a = k * 2 * np.pi / 5 - np.pi / 2
        pts.append((cx + np.cos(a) * 15 * SS, cy + np.sin(a) * 15 * SS))
    orden = [0, 2, 4, 1, 3, 0]
    for i in range(len(orden) - 1):
        a, b = pts[orden[i]], pts[orden[i + 1]]
        add_line(img, a[0], a[1], b[0], b[1], 1.6 * SS, (120, 30, 44), 0.85)

    set_alpha_floor(img)
    out = downsample(img, W, H)
    Image.fromarray(out).save(os.path.join(PRJ_OUT, 'RencorPrimordialProjectile.png'))
    print(f"  ✓ RencorPrimordialProjectile.png ({W}×{H}) — sombra")


def gen_sombra_eminencia():
    img, cx, cy, W, H = sombra_base()
    ys = np.arange(H * SS)
    xs = np.arange(W * SS)
    XX, YY = np.meshgrid(xs, ys)
    rr = np.sqrt((XX - cx) ** 2 + (YY - cy) ** 2)

    # LA MASA OSCURA (la sombra de la congregación — borde roto).
    ang = np.arctan2(YY - cy, XX - cx)
    ruido = 1.0 + 0.15 * np.sin(ang * 5.0 + 1.2) + 0.10 * np.sin(ang * 9.0 + 4.0)
    masa = rr / (28 * SS * ruido)
    dentro = masa < 1.0
    t_m = np.clip(1.0 - masa, 0, 1)

    img[:, :, 0] += np.where(dentro, 26 + 26 * t_m, 0)
    img[:, :, 1] += np.where(dentro, 30 + 30 * t_m, 0)
    img[:, :, 2] += np.where(dentro, 34 + 34 * t_m, 0)
    img[:, :, 3] += np.where(dentro, 196.0, 0.0)

    # EL RIM PÁLIDO-CARMESÍ (el aura espectral de la sombra).
    halo = np.exp(-np.abs(masa - 1.0) * 5.0)
    img[:, :, 0] += halo * 130
    img[:, :, 1] += halo * 120
    img[:, :, 2] += halo * 130
    img[:, :, 3] = np.maximum(img[:, :, 3], halo * 210)

    # LOS DOS OJOS de la sombra (huecos más oscuros con chispa).
    for (ox, oy, r) in [(0.6, -2.6, 3.4), (-5.2, 3.4, 1.6)]:
        ex, ey = cx + ox * SS, cy + oy * SS
        ojo = np.exp(-((XX - ex) ** 2 + (YY - ey) ** 2) / (2 * (r * SS) ** 2))
        img[:, :, 0] += ojo * 60
        img[:, :, 1] += ojo * 50
        img[:, :, 2] += ojo * 58
        img[:, :, 3] = np.maximum(img[:, :, 3], ojo * 235)

    set_alpha_floor(img)
    out = downsample(img, W, H)
    Image.fromarray(out).save(os.path.join(PRJ_OUT, 'EminenciaAtrozProjectile.png'))
    print(f"  ✓ EminenciaAtrozProjectile.png ({W}×{H}) — sombra")


# =====================================================================
#  EL MAIN
# =====================================================================

if __name__ == '__main__':
    os.makedirs(BOLSA_OUT, exist_ok=True)
    print("gen_v629_assets.py — LOS EXHUMADOS + LAS DIEZ BOLSAS:")

    print("  [1] LAS DIEZ BOLSAS POR CATEGORÍA")
    gen_bolsa('BolsaProbador',
              cuerpo=None, borde=(160, 130, 60), interior=(96, 66, 22),
              cordel=(255, 214, 110), emblema=emblema_llave)
    gen_bolsa('BolsaFundacionales',
              cuerpo=None, borde=(90, 190, 220), interior=(14, 52, 78),
              cordel=(140, 235, 255), emblema=emblema_nova)
    gen_bolsa('BolsaClasicosCosmicos',
              cuerpo=None, borde=(200, 170, 90), interior=(70, 50, 16),
              cordel=(255, 214, 110), emblema=emblema_galaxia)
    gen_bolsa('BolsaAgujerosNegros',
              cuerpo=None, borde=(140, 80, 210), interior=(30, 12, 56),
              cordel=(190, 120, 255), emblema=emblema_agujero)
    gen_bolsa('BolsaSolesRunicos',
              cuerpo=None, borde=(230, 130, 50), interior=(76, 34, 10),
              cordel=(255, 190, 90), emblema=emblema_sol)
    gen_bolsa('BolsaEstrellasReales',
              cuerpo=None, borde=(110, 150, 235), interior=(16, 26, 66),
              cordel=(150, 195, 255), emblema=emblema_constelacion)
    gen_bolsa('BolsaArmasLibrerias',
              cuerpo=None, borde=(170, 100, 230), interior=(38, 16, 64),
              cordel=(220, 150, 255), emblema=emblema_rayo)
    gen_bolsa('BolsaBastonesCreativos',
              cuerpo=None, borde=(70, 200, 180), interior=(10, 48, 46),
              cordel=(120, 240, 214), emblema=emblema_reloj)
    gen_bolsa('BolsaExhumados',
              cuerpo=None, borde=(200, 80, 100), interior=(52, 14, 24),
              cordel=(255, 130, 150), emblema=emblema_exhumado)
    gen_bolsa('BolsaCosmeticos',
              cuerpo=None, borde=(210, 120, 190), interior=(58, 22, 52),
              cordel=(255, 160, 225), emblema=emblema_corona)

    print("  [2] LAS DOS ARMAS EXHUMADAS")
    gen_rencor()
    gen_eminencia()

    print("  [3] LAS DOS SOMBRAS")
    gen_sombra_rencor()
    gen_sombra_eminencia()

    print("HECHO — 14 PNGs de v6.29.")
