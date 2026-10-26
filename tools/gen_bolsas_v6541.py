#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# ======================================================================
#  gen_bolsas_v6541.py — LAS TRES TEXTURAS QUE FALTARON EN LA v6.54.0.
#
#  La letra del usuario (client.log de la .54): «hay errores» — tML
#  DESACTIVÓ el mod porque BolsaDeAlasPrisma, BolsaInvestigacion y
#  BolsaMaestra (las tres bolsas nuevas de la investigación) nacieron
#  SIN su .png. Este script crea los tres iconos 30×30:
#
#    · BOLSA DE LAS ALAS    — la silueta clásica de la casa (la misma
#      bolsa de BolsaArmasRayo) recolorida morado-negro con dither, el
#      cordón LILA (201,166,255) y DOS ALAS AURORA desplegadas detrás
#      del cuello — tres plumas por lado en rampa lila→violeta→profundo
#      con puntas blancas (el abanico de AlasAuroraLib en miniatura).
#
#    · BOLSA DE LA INVESTIGACIÓN — la misma silueta en púrpura profundo
#      con dither, el cordón MORADO DEL MOD (140,60,210) y en la ventana
#      central UNA CONSTELACIÓN: cuatro estrellas blancas unidas por
#      líneas tenues + el rayo concentrado (zigzag violeta pálido) — las
#      dos técnicas insignia de la investigación.
#
#    · EL COFRE DEL ARSENAL (BolsaMaestra) — NO es saco: es un COFRE
#      DORADO abierto con las mini-bolsas asomando de la boca: tapa
#      abierta con filos morados, resplandor cálido, TRES sacos violáceos
#  con lazo lila (y el del centro dorado), esquinas reforzadas moradas
#      y la cerradura morada con ojo de oro.
#
#  Convención de la casa: 30×30 RGBA, fondo transparente, contorno
#  oscuro estilo Terraria (26,15,38), reporte ASCII + md5 al final.
# ======================================================================
import hashlib
import os

from PIL import Image
import numpy as np

BASE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
CARPETA = os.path.normpath(os.path.join(
    BASE, "AethonMod", "Content", "Items", "Bolsas"))
SRC = os.path.join(CARPETA, "BolsaArmasRayo.png")

W = H = 30
OUTLINE = (26, 15, 38, 255)   # el contorno oscuro de la casa


# ---------------------------------------------------------------------
#  utilidades
# ---------------------------------------------------------------------
def dentro(y, x):
    return 0 <= y < H and 0 <= x < W


def poner(img, y, x, c):
    if dentro(y, x):
        img[y][x] = c


def contorno_final(img):
    """Todo píxel vacío que toque la silueta se vuelve contorno."""
    solid = {(x, y) for y in range(H) for x in range(W) if img[y][x][3] > 0}
    cont = set()
    for (x, y) in solid:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1),
                       (1, 1), (1, -1), (-1, 1), (-1, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < W and 0 <= ny < H and (nx, ny) not in solid:
                cont.add((nx, ny))
    for (x, y) in cont:
        img[y][x] = OUTLINE
    return img


def guardar(img_arr, nombre):
    out = Image.fromarray(img_arr.astype(np.uint8))
    ruta = os.path.join(CARPETA, nombre)
    out.save(ruta)
    with open(ruta, "rb") as f:
        md5 = hashlib.md5(f.read()).hexdigest()
    print(f"{nombre} → {ruta}  ({out.size[0]}x{out.size[1]}, md5 {md5})")
    return ruta


def ascii_reporte(ruta, mapa):
    """mapa: lista de (letra, [colores RGB]) — primer match gana."""
    a = np.array(Image.open(ruta).convert('RGBA')).astype(int)
    print("  (W=blanco w=claro +=medio x=cuerpo #=contorno ~=ventana .=nada)")
    for y in range(H):
        fila = ""
        for x in range(W):
            r, g, b, al = a[y][x]
            if al < 40:
                fila += "."
                continue
            letra = None
            for c, cols in mapa:
                if (r, g, b) in cols:
                    letra = c
                    break
            if letra is None:
                letra = "~" if al < 200 else "x"
            fila += letra
        print("  " + fila)


# ---------------------------------------------------------------------
#  LA SILUETA CLÁSICA (BolsaArmasRayo) — clasificación de píxeles
# ---------------------------------------------------------------------
base = np.array(Image.open(SRC).convert("RGBA")).astype(int)
alpha = base[:, :, 3]

es_borde = np.zeros((H, W), dtype=bool)     # semitransparente que toca el exterior
es_ventana = np.zeros((H, W), dtype=bool)   # semitransparente interior (el glow central)
for y in range(H):
    for x in range(W):
        a = alpha[y, x]
        if a <= 40 or a >= 200:
            continue
        toca_fuera = any(
            dentro(y + dy, x + dx) and alpha[y + dy, x + dx] < 10
            for dy in (-2, -1, 0, 1, 2) for dx in (-2, -1, 0, 1, 2))
        if toca_fuera:
            es_borde[y, x] = True
        else:
            es_ventana[y, x] = True

# el cordón: los píxeles OPACOS de las filas del cuello (y<=7) — la
# atadura de la bolsa clásica (en la del rayo son las runas del cuello).
es_cordon = np.zeros((H, W), dtype=bool)
for y in range(0, 8):
    for x in range(W):
        if alpha[y, x] >= 200:
            es_cordon[y, x] = True


def silueta_clasica(cuerpo1, cuerpo2, velo, negro, cordon, cordon_osc, nudo):
    """La silueta de la casa recolorida — devuelve la img lista."""
    img = np.zeros((H, W, 4), dtype=int)
    for y in range(H):
        for x in range(W):
            if alpha[y, x] <= 40:
                continue
            if es_borde[y, x]:
                img[y][x] = (*negro, 255)
            elif es_ventana[y, x]:
                img[y][x] = (*negro, 120)          # la ventana: se pinta aparte
            elif es_cordon[y, x]:
                borde_cordon = alpha[y, x - 1 if x > 0 else 0] < 200 or \
                    alpha[y, x + 1 if x < W - 1 else W - 1] < 200
                c = cordon_osc if borde_cordon else cordon
                img[y][x] = (*c, 255)
            else:
                d = ((x + y * 2) % 5) < 2
                c = cuerpo1 if d else cuerpo2
                if y <= 12 and ((x * 3 + y) % 7) < 2:
                    c = velo                       # la penumbra del lomo
                img[y][x] = (*c, 255)
    return img


# =====================================================================
#  1 — BOLSA DE LAS ALAS (BolsaDeAlasPrisma)
# =====================================================================
def gen_bolsa_alas():
    LILA = (201, 166, 255)        # ColorFiesta de la clase
    LILA_OSC = (150, 118, 214)
    NUDO = (238, 222, 255)
    CUERPO_1 = (38, 25, 60)       # masa morado-negra, lado claro del dither
    CUERPO_2 = (23, 15, 40)       # lado oscuro
    VELO = (68, 46, 102)          # penumbra
    NEGRO = (10, 7, 17)
    BLANCO = (248, 244, 255)

    # --- las ALAS AURORA detrás (se dibujan primero; el saco tapa la base)
    img = np.zeros((H, W, 4), dtype=int)
    # tres plumas por lado, en abanico desde el cuello (13,10)/(16,10).
    plumas = [
        # (base, punta, color)  — Bézier con control que arquea hacia arriba
        ((13, 11), (2, 4),  (216, 190, 255)),   # la larga, lila pálido
        ((14, 12), (2, 9),  (170, 120, 250)),   # la media, violeta
        ((14, 13), (4, 14), (120, 70, 210)),    # la corta, profunda
    ]

    def bezier(p0, p1, p2, n=24):
        pts = []
        for i in range(n + 1):
            t = i / n
            cx = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t ** 2 * p2[0]
            cy = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t ** 2 * p2[1]
            pts.append((cx, cy))
        return pts

    for lado in (-1, 1):  # -1 izquierda, +1 derecha (espejo en x=14.5)
        for (bx, by), (tx, ty), col in plumas:
            b = (bx if lado < 0 else 29 - bx, by)
            t = (tx if lado < 0 else 29 - tx, ty)
            # el control arquea la pluma por arriba (curva de ala)
            ctrl = ((b[0] + t[0]) // 2, min(b[1], t[1]) - 3)
            pts = bezier(b, ctrl, t)
            for i, (px, py) in enumerate(pts):
                # grosor 2, afinando hacia la punta
                g = 2 if i < len(pts) - 6 else 1
                for dy in range(g):
                    for dx in range(2):
                        poner(img, int(py) + dy, int(px) + dx, (*col, 255))
            # la punta blanca de la pluma
            poner(img, t[1], t[0], (*BLANCO, 255))
            poner(img, t[1] + 1, t[0] + (1 if lado < 0 else 0), (*BLANCO, 255))

    # --- el SACO clásico encima
    saco = silueta_clasica(CUERPO_1, CUERPO_2, VELO, NEGRO,
                           LILA, LILA_OSC, NUDO)
    for y in range(H):
        for x in range(W):
            if alpha[y, x] > 40 and saco[y][x][3] > 0:
                img[y][x] = saco[y][x]

    # el nudo brillante del cordón
    poner(img, 2, 15, (*NUDO, 255))
    poner(img, 3, 15, (*NUDO, 255))

    # --- la ventana (la REAL del saco clásico: x 11-17, y 12-22): el
    #     DESTELLO de la aurora — estrella de 4 puntas SIMÉTRICA en el vidrio
    poner(img, 16, 14, (*BLANCO, 255)); poner(img, 16, 15, (*BLANCO, 255))
    poner(img, 17, 14, (*BLANCO, 255)); poner(img, 17, 15, (*BLANCO, 255))
    poner(img, 14, 14, (*LILA, 255));    poner(img, 14, 15, (*LILA, 255))    # brazo arriba
    poner(img, 19, 14, (*LILA, 255));    poner(img, 19, 15, (*LILA, 255))    # brazo abajo
    poner(img, 16, 12, (*LILA, 255));    poner(img, 17, 12, (*LILA, 255))    # brazo izq
    poner(img, 16, 17, (*LILA, 255));    poner(img, 17, 17, (*LILA, 255))    # brazo der
    # chispitas del cuerpo
    poner(img, 10, 8, (*VELO, 255));    poner(img, 21, 10, (*VELO, 255))
    poner(img, 7, 21, (*VELO, 255))

    img = contorno_final(img)
    return guardar(img, "BolsaDeAlasPrisma.png")


# =====================================================================
#  2 — BOLSA DE LA INVESTIGACIÓN (BolsaInvestigacion)
# =====================================================================
def gen_bolsa_investigacion():
    MORADO = (140, 60, 210)       # ColorFiesta de la clase — el morado del mod
    MORADO_OSC = (96, 38, 152)
    NUDO = (198, 130, 255)
    CUERPO_1 = (32, 19, 54)
    CUERPO_2 = (19, 11, 34)
    VELO = (56, 34, 88)
    NEGRO = (8, 5, 14)
    BLANCO = (244, 240, 252)
    PALE = (178, 148, 226)

    img = silueta_clasica(CUERPO_1, CUERPO_2, VELO, NEGRO,
                          MORADO, MORADO_OSC, NUDO)
    poner(img, 2, 15, (*NUDO, 255))
    poner(img, 3, 15, (*NUDO, 255))

    # --- LA VENTANA: el cosmos de la investigación ---
    # LA VENTANA REAL del saco clásico: x 11-17, y 12-22 (verificada contra
    # la silueta — la primera versión dibujaba fuera y las líneas se perdían).
    # la constelación Casiopea: cuatro estrellas en zigzag unidas (el Cetro)
    estrellas = [(12, 13), (15, 12), (16, 16), (13, 18)]
    lineas = [((12, 13), (15, 12)), ((15, 12), (16, 16)), ((16, 16), (13, 18))]

    LINEA_COL = (196, 168, 240)   # la línea de la constelación: pálida pero LEÍBLE

    def linea(p0, p1, col):
        """Bresenham 1px tenue."""
        x0, y0 = p0
        x1, y1 = p1
        dx, dy = abs(x1 - x0), abs(y1 - y0)
        sx = 1 if x0 < x1 else -1
        sy = 1 if y0 < y1 else -1
        err = dx - dy
        while True:
            if es_ventana[y0, x0]:
                poner(img, y0, x0, (*col, 170))
            if (x0, y0) == (x1, y1):
                break
            e2 = 2 * err
            if e2 > -dy:
                err -= dy
                x0 += sx
            if e2 < dx:
                err += dx
                y0 += sy

    for p0, p1 in lineas:
        linea(p0, p1, LINEA_COL)

    # las estrellas (núcleo blanco + halo pale)
    for (sx, sy) in estrellas:
        poner(img, sy, sx, (*BLANCO, 255))
        poner(img, sy, sx + 1, (*PALE, 200))
        poner(img, sy + 1, sx, (*PALE, 200))
    poner(img, 12, 13, (*BLANCO, 255))  # la mayor brilla doble
    poner(img, 13, 13, (*BLANCO, 255))

    # EL RAYO CONCENTRADO: zigzag violeta pálido en el bajo de la ventana
    # (dentro de la región real: filas 20-23, x 11-16)
    bolt = [(12, 20), (14, 21), (13, 22), (16, 21), (15, 23), (12, 22)]
    for (bx, by) in bolt:
        if es_ventana[by, bx]:
            poner(img, by, bx, (216, 190, 255, 235))
    poner(img, 23, 13, (*PALE, 255))   # chispa suelta en el cuerpo
    poner(img, 7, 10, (*VELO, 255))
    poner(img, 22, 22, (*VELO, 255))
    poner(img, 8, 24, (*VELO, 255))

    img = contorno_final(img)
    return guardar(img, "BolsaInvestigacion.png")


# =====================================================================
#  3 — EL COFRE DEL ARSENAL (BolsaMaestra)
# =====================================================================
def gen_cofre_maestra():
    ORO_1 = (255, 224, 160)       # la luz del oro
    ORO_2 = (244, 196, 116)       # ColorFiesta de la clase (255,196,107)
    ORO_3 = (208, 146, 78)        # la sombra del oro
    MORADO = (104, 56, 168)       # las bandas reforzadas
    MORADO_OSC = (72, 38, 120)
    NUDO = (201, 166, 255)
    VIOLETA = (116, 66, 186)      # los mini-sacos (brillitos: se leen sobre el negro)
    VIOLETA_OSC = (82, 44, 138)
    RIM_SACO = (201, 166, 255)    # el borde lila de los mini-sacos
    ORO_SACO = (240, 190, 110)    # el mini-saco dorado (el del centro)
    ORO_SACO_OSC = (188, 138, 70)
    GLYPH = (255, 226, 150)       # el ojo de la cerradura
    NEGRO = (22, 13, 32)

    img = np.zeros((H, W, 4), dtype=int)

    # --- EL INTERIOR abierto (la boca del cofre, y=9..13): negro con
    #     resplandor cálido al fondo — las bolsas asoman de ahí.
    for y in range(9, 14):
        for x in range(5, 25):
            img[y][x] = (*NEGRO, 255)
    for x in range(7, 23):                       # el resplandor del fondo
        poner(img, 12, x, (255, 180, 96, 150))
        poner(img, 13, x, (255, 150, 70, 110))

    # --- LOS TRES MINI-SACOS asomando (el tesoro: ¡son BOLSAS!)
    #     (los dibuja el helper: cuerpo redondito + lazo de 1px)
    def mini_saco(cx, base_y, cuerpo, cuerpo_osc, lazo):
        for dy in range(-4, 1):
            anch = 3 if dy >= -2 else 2
            for dx in range(-anch, anch + 1):
                c = cuerpo_osc if abs(dx) == anch else cuerpo
                poner(img, base_y + dy, cx + dx, (*c, 255))
        # el rim lila del borde superior: lo que hace LEÍBLE el saco en el negro
        poner(img, base_y - 4, cx, (*RIM_SACO, 255))
        poner(img, base_y - 4, cx - 1, (*RIM_SACO, 255))
        poner(img, base_y - 4, cx + 1, (*RIM_SACO, 255))
        poner(img, base_y - 3, cx - 2, (*RIM_SACO, 255))
        poner(img, base_y - 3, cx + 2, (*RIM_SACO, 255))
        poner(img, base_y - 5, cx, (*lazo, 255))     # el nudo del lazo

    mini_saco(9, 12, VIOLETA, VIOLETA_OSC, NUDO)
    mini_saco(15, 13, ORO_SACO, ORO_SACO_OSC, (26, 15, 38))
    mini_saco(21, 12, VIOLETA, VIOLETA_OSC, NUDO)
    # el rim del saco dorado es el propio oro claro
    poner(img, 9, 14, (*ORO_1, 255)); poner(img, 9, 15, (*ORO_1, 255))
    poner(img, 9, 16, (*ORO_1, 255)); poner(img, 10, 13, (*ORO_1, 255))
    poner(img, 10, 17, (*ORO_1, 255))

    # --- LA TAPA abierta (y=1..8): trapecio dorado con filos morados
    for y in range(1, 9):
        # el trapecio se ensancha hacia abajo (la tapa entreabierta hacia atrás)
        margen = max(0, (8 - y) // 2)
        for x in range(4 + margen, 26 - margen):
            f = (y - 1) / 7.0
            c = tuple(int(ORO_1[i] + (ORO_3[i] - ORO_1[i]) * f) for i in range(3))
            if x <= 5 + margen or x >= 24 - margen:      # los filos morados
                c = MORADO if y < 4 else MORADO_OSC
            if y == 1:                                    # el brillo superior
                c = ORO_1
            img[y][x] = (*c, 255)

    # --- EL CUERPO del cofre (y=14..27): oro con bandas moradas
    for y in range(14, 28):
        for x in range(3, 27):
            f = (y - 14) / 13.0
            c = tuple(int(ORO_2[i] + (ORO_3[i] - ORO_2[i]) * f) for i in range(3))
            if x <= 5 or x >= 24:                        # esquinas reforzadas
                c = MORADO_OSC if (y >= 25 or x <= 4 or x >= 25) else MORADO
            if y >= 25:                                   # la banda inferior
                c = MORADO_OSC
            img[y][x] = (*c, 255)

    # --- LA CERRADURA (el cierre del cofre): placa morada + ojo de oro
    for y in range(14, 19):
        for x in range(13, 17):
            c = MORADO if 14 < y < 18 else MORADO_OSC
            img[y][x] = (*c, 255)
    poner(img, 16, 14, (*GLYPH, 255)); poner(img, 17, 14, (*GLYPH, 255))  # el ojo
    poner(img, 17, 15, (*GLYPH, 255))                                       # el tallo

    # --- la línea de bisagras y el brillo de la tapa
    for x in range(6, 24, 3):
        poner(img, 8, x, (*MORADO_OSC, 255))
    poner(img, 2, 8, (255, 240, 200, 255))
    poner(img, 26, 20, (*GLYPH, 255))
    poner(img, 4, 16, (255, 214, 150, 255))

    img = contorno_final(img)
    return guardar(img, "BolsaMaestra.png")


# =====================================================================
if __name__ == "__main__":
    r1 = gen_bolsa_alas()
    r2 = gen_bolsa_investigacion()
    r3 = gen_cofre_maestra()

    print()
    print("=== BolsaDeAlasPrisma (a=ala W=blanco L=lila) ===")
    ascii_reporte(r1, [("W", [(248, 244, 255), (238, 222, 255)]),
                       ("L", [(201, 166, 255)]),
                       ("a", [(216, 190, 255), (170, 120, 250), (120, 70, 210)])])
    print("=== BolsaInvestigacion (W=estrella E=rayo L=linea) ===")
    ascii_reporte(r2, [("W", [(244, 240, 252), (198, 130, 255)]),
                       ("E", [(216, 190, 255)]),
                       ("L", [(178, 148, 226), (196, 168, 240)])])
    print("=== BolsaMaestra (G=oro M=morado S=saco-violeta) ===")
    ascii_reporte(r3, [("G", [(255, 224, 160), (244, 196, 116), (208, 146, 78),
                              (240, 190, 110), (255, 226, 150), (188, 138, 70)]),
                       ("M", [(104, 56, 168), (72, 38, 120)]),
                       ("S", [(116, 66, 186), (82, 44, 138), (201, 166, 255)])])
