#!/usr/bin/env python3
# gen_w1a_assets_v631.py — LOS SPRITES DE ÍTEM DE LAS 2 ARMAS DE STAR TOMB (v6.31)
#
# 1. SembradorCementeralStaff.png (30×30): bastón grafito vertical con cabeza
#    de ESTRELLA DE 4 PUNTAS violeta-azul (la semilla del púlsar) y una
#    media-vuelta de anillo orbital (la magnetosfera) — paleta exacta de la
#    ficha R3: violeta (138,79,255) / azul (120,181,255) / blanco-caliente
#    (199,214,255) / profundo (31,26,128).
# 2. ColapsoMagnetarStaff.png (30×30): bastón grafito más oscuro con cabeza
#    de JAULA MAGNÉTICA: un anillo cerrado con grietas blancas (la corteza a
#    punto de romperse) y el núcleo comprimido dentro.
#
# Estilo: pixel-art DURO de la casa, contorno exterior 1px violeta-negro,
# esquinas transparentes verificadas (mismo contrato que gen_v630_assets.py).

from PIL import Image
import math, os

OUTW = os.path.join(os.path.dirname(__file__), '..', 'Content', 'Weapons', 'Cosmic')
os.makedirs(OUTW, exist_ok=True)

W = H = 30

# --- LA PALETA (ficha R3 — Star Tomb) ---
CONTORNO   = (24, 16, 46, 255)     # violeta-negro del contorno
GRAFITO    = (74, 78, 104, 255)    # cuerpo del bastón
GRAFITO_CL = (104, 110, 142, 255)  # bruma clara del cuerpo
PROFUNDO   = (31, 26, 128, 255)    # base profunda de la corteza
VIOLETA    = (138, 79, 255, 255)   # violeta neutrón
AZUL       = (120, 181, 255, 255)  # azul frío de la magnetosfera
CALIENTE   = (199, 214, 255, 255)  # núcleo degenerado blanco-azul

def h2(x, y):
    """Hash determinista de píxel (detalle sin Main.rand)."""
    n = (x * 374761393 + y * 668265263) & 0xFFFFFFFF
    n = ((n ^ (n >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((n ^ (n >> 16)) & 0xFFFFFF) / 16777216.0

def guardar(im, nombre):
    ruta = os.path.join(OUTW, nombre)
    im.save(ruta)
    import numpy as np
    a = np.array(im.convert('RGBA'))
    esquinas = [int(a[0,0,3]), int(a[0,-1,3]), int(a[-1,0,3]), int(a[-1,-1,3])]
    print(f"  {nombre}: {im.size}, esquinas alfa {esquinas}")

# ----------------------------------------------------------------------
# 1 — EL SEMBRADOR: bastón con cabeza de estrella de 4 puntas + órbita
# ----------------------------------------------------------------------
def sembrador():
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = im.load()

    cx, cy = 15.0, 9.0        # centro de la estrella-cabeza

    # --- EL ASTA (vertical, ligera curva de bastón antiguo) ---
    for y in range(14, 29):
        x = 15 + (1 if y > 21 else 0)
        px[x, y] = GRAFITO
        px[x - 1, y] = GRAFITO_CL if h2(x - 1, y) > 0.45 else GRAFITO
        px[x + 1, y] = GRAFITO_CL if h2(x + 1, y) > 0.62 else GRAFITO
        # contorno del asta
        if px[x - 2, y][3] == 0: px[x - 2, y] = CONTORNO
        if px[x + 2, y][3] == 0: px[x + 2, y] = CONTORNO
    px[14, 29] = CONTORNO
    px[17, 28] = CONTORNO

    # --- LA ESTRELLA DE 4 PUNTAS (la semilla del púlsar) ---
    def dentro_estrella(x, y):
        dx, dy = abs(x - cx), abs(y - cy)
        d = dx + dy                      #rombo de 4 puntas
        if d <= 5.2:
            return True
        # las puntas finas (cruz de destello)
        return (dx <= 0.6 and dy <= 7.6) or (dy <= 0.6 and dx <= 7.6)

    for y in range(1, 19):
        for x in range(7, 24):
            if dentro_estrella(x, y):
                dx, dy = abs(x - cx), abs(y - cy)
                d = dx + dy
                if d < 1.4:
                    px[x, y] = CALIENTE              # el núcleo degenerado
                elif d < 3.2:
                    px[x, y] = AZUL if h2(x, y) > 0.35 else CALIENTE
                else:
                    px[x, y] = VIOLETA if h2(x, y) > 0.30 else PROFUNDO

    # --- EL CONTORNO DE LA ESTRELLA (1px, solo donde toca vacío) ---
    for y in range(1, 19):
        for x in range(7, 24):
            if px[x, y][3] == 0 and dentro_estrella(x, y) is False:
                for ox in (-1, 0, 1):
                    for oy in (-1, 0, 1):
                        xx, yy = x + ox, y + oy
                        if 0 <= xx < W and 0 <= yy < H and dentro_estrella(xx, yy):
                            px[x, y] = CONTORNO
                            break
                    else:
                        continue
                    break

    # --- LA MEDIA ÓRBITA (la magnetosfera que la envuelve) ---
    for t in range(64):
        a = math.pi * (0.08 + 0.84 * t / 63.0)     # arco superior abierto abajo
        r = 8.4
        x = int(round(cx + math.cos(a) * r))
        y = int(round(cy - math.sin(a) * r * 0.62))  # achatada (órbita vista en ángulo)
        if 0 <= x < W and 0 <= y < H and px[x, y][3] == 0:
            px[x, y] = AZUL if h2(x, y) > 0.4 else VIOLETA

    return im

# ----------------------------------------------------------------------
# 2 — EL COLAPSO: bastón oscuro con cabeza de jaula magnética agrietada
# ----------------------------------------------------------------------
def colapso():
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = im.load()

    cx, cy = 15.0, 9.0

    # --- EL ASTA (más oscura, comprimida) ---
    for y in range(15, 29):
        x = 15
        px[x, y] = (56, 58, 84, 255)
        px[x - 1, y] = (82, 86, 116, 255) if h2(x - 1, y) > 0.5 else (56, 58, 84, 255)
        px[x + 1, y] = (82, 86, 116, 255) if h2(x + 1, y) > 0.66 else (56, 58, 84, 255)
        if px[x - 2, y][3] == 0: px[x - 2, y] = CONTORNO
        if px[x + 2, y][3] == 0: px[x + 2, y] = CONTORNO
    px[14, 29] = CONTORNO
    px[17, 28] = CONTORNO

    # --- LA JAULA: anillo grueso con NÚCLEO dentro ---
    R_ext, R_int = 7.6, 4.6
    for y in range(0, 19):
        for x in range(6, 25):
            dx, dy = x - cx, y - cy
            d = math.sqrt(dx * dx + dy * dy)
            if R_int <= d <= R_ext:
                # LA GRIETA BLANCA: la corteza a punto de ceder — una falla
                # diagonal determinista + motas hash
                falla = abs((dx + dy * 1.35) - 1.8) < 0.9
                if falla or h2(x, y) > 0.87:
                    px[x, y] = CALIENTE
                elif h2(x, y) > 0.55:
                    px[x, y] = AZUL
                else:
                    px[x, y] = VIOLETA if h2(x + 31, y + 7) > 0.4 else PROFUNDO
            elif d < R_int:
                # EL NÚCLEO COMPRIMIDO (materia degenerada)
                if d < 1.6:
                    px[x, y] = CALIENTE
                elif h2(x, y) > 0.5:
                    px[x, y] = AZUL
                else:
                    px[x, y] = PROFUNDO

    # --- LOS POLOS: los dos casques donde nacerá el estallido ---
    for sx in (-1, 1):
        x = int(round(cx + sx * 7.0))
        y = int(round(cy))
        for oy in (-1, 0, 1):
            for ox in (-1, 0, 1):
                xx, yy = x + ox, y + oy
                if 0 <= xx < W and 0 <= yy < H:
                    px[xx, yy] = CALIENTE if (ox == 0 and oy == 0) else AZUL

    # --- EL CONTORNO DE LA JAULA ---
    for y in range(0, 19):
        for x in range(6, 25):
            if px[x, y][3] == 0:
                dx, dy = x - cx, y - cy
                d = math.sqrt(dx * dx + dy * dy)
                if d <= R_ext + 1.05:
                    px[x, y] = CONTORNO

    return im

print("gen_w1a_assets_v631 — las 2 armas de Star Tomb:")
guardar(sembrador(), 'SembradorCementeralStaff.png')
guardar(colapso(), 'ColapsoMagnetarStaff.png')
