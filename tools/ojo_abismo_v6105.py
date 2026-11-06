#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
ojo_abismo_v6105.py — v6.105 — LA FORJA DEL OJO DEL ABISMO AMARILLO.

LA LETRA DEL USUARIO: «sobre OjoAbismo.png, que sea un ojo amarillo
con una pupila en forma de estrella de 5 puntas».

El ojo del quinto tomo cambia de mirada: la rendija vertical muere y
nace LA PUPILA ESTRELLA DE 5 PUNTAS — el sello del grimorio en el
ojo del abismo. Todo lo demás es la casa:

  · La forma ALMENDRADA de siempre (el ojo rasgado — OjoTexturizado
    la gira hacia la presa y la aplasta al parpadear).
  · LA ESCLERÓTICA OSCURA (negro-violeta de la casa) con venas
    finas — ahora AMBARINAS (la sangre del ojo corre dorada).
  · EL IRIS AMARILLO RADIAL: del ORO VIVO del centro al ÁMBAR del
    borde, con el brillo interior y el moteado radial.
  · LA PUPILA ESTRELLA DE 5 PUNTAS: negra como el vacío, con el
    filo INCANDESCENTE ámbar brillando en las cinco puntas — la
    geometría PURA de PIL (diez vértices exactos, ni un píxel de
    azar — la estrella ES una estrella).
  · EL DESTELLO HÚMEDO especular arriba-izquierda.
  · Premultiplicado (q,q,q,q): rgb·m, a=255·m — la convención del
    lote aditivo de la casa.

Salida: AethonMod/Content/Effects/OjoAbismo.png (64×64, RGBA).

Uso: python3 tools/ojo_abismo_v6105.py
"""
import math
import os
import random

from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
BASE = os.path.join(AQUI, '..', 'AethonMod')
DEST = os.path.join(BASE, 'Content', 'Effects', 'OjoAbismo.png')

N = 64
CX, CY = N / 2.0, N / 2.0


# ====================================================================
#  EL RUIDO DE VALOR (el fBm ligero de la casa — moteado y venas)
# ====================================================================
def _valor(semilla, x, y):
    """El hash determinista → 0..1."""
    h = semilla * 374761393 + x * 668265263 + y * 2246822519
    h = (h ^ (h >> 13)) * 1274126177
    h = h ^ (h >> 16)
    return (h & 0xFFFFFFFF) / 0xFFFFFFFF


def ruido(x, y, semilla=0):
    """El value noise bilineal suave."""
    xi, yi = int(x), int(y)
    fx, fy = x - xi, y - yi
    fx = fx * fx * (3 - 2 * fx)
    fy = fy * fy * (3 - 2 * fy)
    a = _valor(semilla, xi, yi)
    b = _valor(semilla, xi + 1, yi)
    c = _valor(semilla, xi, yi + 1)
    d = _valor(semilla, xi + 1, yi + 1)
    return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy


def fbm(x, y, octavas=3, semilla=0):
    """El fBm de siempre: 3 octavas, la mitad de frecuencia al doble."""
    v = 0.0
    amp = 0.55
    f = 1.0
    for _ in range(octavas):
        v += amp * ruido(x * f, y * f, semilla)
        f *= 2.03
        amp *= 0.5
    return min(1.0, max(0.0, v))


# ====================================================================
#  LA GEOMETRÍA DE LA ESTRELLA DE 5 PUNTAS
# ====================================================================
def estrella5(cx, cy, radio_ext, radio_int, angulo=-math.pi / 2.0):
    """LOS DIEZ VÉRTICES de la estrella: la punta para ARRIBA
    (angulo −π/2 — el sello del grimorio)."""
    pts = []
    for k in range(10):
        r = radio_ext if k % 2 == 0 else radio_int
        a = angulo + k * math.pi / 5.0
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return pts


def dentro_poligono(px, py, pts):
    """El test par-impar (el punto dentro del polígono de la estrella)."""
    dentro = False
    j = len(pts) - 1
    for i in range(len(pts)):
        xi, yi = pts[i]
        xj, yj = pts[j]
        if (yi > py) != (yj > py):
            xint = (xj - xi) * (py - yi) / (yj - yi) + xi
            if px < xint:
                dentro = not dentro
        j = i
    return dentro


def dist_estrella(px, py, pts):
    """La distancia al borde de la estrella (para el filo suave)."""
    d = 1e9
    j = len(pts) - 1
    for i in range(len(pts)):
        x1, y1 = pts[j]
        x2, y2 = pts[i]
        dx, dy = x2 - x1, y2 - y1
        L2 = dx * dx + dy * dy
        if L2 > 0:
            t = max(0.0, min(1.0, ((px - x1) * dx + (py - y1) * dy) / L2))
            qx, qy = x1 + t * dx, y1 + t * dy
            d = min(d, math.hypot(px - qx, py - qy))
        j = i
    return d


# ====================================================================
#  LA MÁSCARA ALMENDRADA (la forma del ojo de siempre)
# ====================================================================
def mascara_ojo(x, y):
    """LA FORMA ALMENDRADA: dos arcos elípticos arriba y abajo con el
    canto externo estirado (el ojo rasgado). Devuelve (dentro, borde)
    — borde = distancia suavizada al filo (1 el corazón, 0 afuera)."""
    # la almendra: |dy| ≤ a(x) con a(x) el perfil de dos arcos
    t = (x - CX) / (N * 0.46)
    if abs(t) >= 1.0:
        return False, 0.0
    # el ojo se ESTIRA hacia la derecha (el canto externo) un 12%
    te = t * (1.06 if t > 0 else 0.92)
    # el perfil: dos medios-círculos de radios distintos (almendra)
    a = math.sqrt(max(0.0, 1.0 - te * te))
    alto = (N * 0.40) * a
    if alto < 0.4:
        return False, 0.0
    dy = y - CY
    if abs(dy) > alto:
        # el borde fundido: 2.2 px de pluma fuera de la almendra
        fuera = abs(dy) - alto
        if fuera > 2.2:
            return False, 0.0
        return True, 1.0 - fuera / 2.2
    # dentro de la almendra: el corazón opaco, un soplo de fundido al filo
    dentro = abs(dy) / alto
    return True, min(1.0, 0.72 + 0.28 * (1.0 - dentro))


def forjar():
    img = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    px = img.load()

    # LA ESTRELLA: pupila de 5 puntas (la punta ARRIBA)
    R_EXT = N * 0.205          # el radio de las puntas
    R_INT = N * 0.082          # el radio de los valles (la estrella AFILADA)
    pts = estrella5(CX + 1.5, CY - 1.0, R_EXT, R_INT)

    # LOS COLORES DE LA CASA (el ojo amarillo de la letra)
    ESCLER_SOMBRA = (16, 10, 24)      # la esclerótica en sombra
    ESCLER_MEDIA = (34, 22, 48)       # la esclerótica oscura
    ESCLER_ALTA = (58, 42, 74)        # la esclerótica violeta
    VENA = (172, 120, 30)             # las venas doradas apagadas
    VENA_VIVA = (214, 156, 40)        # la vena encendida
    IRIS_BORDE = (196, 138, 18)       # el iris en el borde: ÁMBAR
    IRIS_MEDIO = (238, 184, 26)       # el iris medio
    IRIS_CENTRO = (255, 226, 88)      # el oro vivo del centro
    IRIS_BRILLO = (255, 248, 205)     # el destello interior del iris
    PUPILA = (4, 2, 8)                # la estrella: el negro del vacío
    FILO = (255, 244, 168)            # el filo incandescente de las puntas
    DESTELLO = (255, 252, 235)        # el especular húmedo

    rng = random.Random(6105)

    for y in range(N):
        for x in range(N):
            dentro, borde = mascara_ojo(x, y)
            if not dentro or borde <= 0.0:
                continue

            dx = (x - CX) / (N * 0.5)
            dy = (y - CY) / (N * 0.5)
            r = math.hypot(dx, dy)

            # ------------------------------------------------------
            #  LA CAPA 1 — LA ESCLERÓTICA OSCURA (el lecho del ojo)
            # ------------------------------------------------------
            moteado = fbm(x * 0.16, y * 0.16, semilla=7)
            base = [
                int(ESCLER_SOMBRA[k] + (ESCLER_MEDIA[k] - ESCLER_SOMBRA[k]) * moteado)
                for k in range(3)
            ]
            # el párpado sombreado arriba/abajo (la almendra se hunde)
            sombra_par = min(1.0, abs(dy) * 1.5)
            base = [int(c * (1.0 - 0.35 * sombra_par)) for c in base]

            # ------------------------------------------------------
            #  LA CAPA 2 — LAS VENAS DORADAS (la sangre del ojo)
            # ------------------------------------------------------
            vena = fbm(x * 0.22 + 40, y * 0.22 - 17, octavas=2, semilla=13)
            if vena > 0.74 and r > 0.42:
                k = (vena - 0.74) / 0.26
                viva = int(vena * 255) % 2  # el parpadeo de la vena
                col = VENA_VIVA if viva else VENA
                base = [int(base[k2] + (col[k2] - base[k2]) * k * 0.55)
                        for k2 in range(3)]

            # ------------------------------------------------------
            #  LA CAPA 3 — EL IRIS AMARILLO RADIAL (la rueda dorada)
            # ------------------------------------------------------
            # el iris ocupa el corazón: radio 0.62 de la almendra
            r_iris = r / 0.62
            if r_iris < 1.0:
                ang = math.atan2(dy, dx)
                rayos = 0.5 + 0.5 * math.sin(ang * 22.0 + fbm(x * 0.3, y * 0.3, 1, 3) * 6.0)
                radial = 1.0 - r_iris  # 1 el centro, 0 el borde
                iris = [
                    int(IRIS_BORDE[k] + (IRIS_MEDIO[k] - IRIS_BORDE[k]) * min(1.0, radial * 1.8))
                    for k in range(3)
                ]
                if radial > 0.55:
                    k2 = (radial - 0.55) / 0.45
                    iris = [int(iris[k] + (IRIS_CENTRO[k] - iris[k]) * k2) for k in range(3)]
                # el moteado radial (los rayos del iris vivos)
                iris = [int(c * (0.82 + 0.18 * rayos)) for c in iris]
                # el brillo interior (el centro ENCENDE)
                if r_iris < 0.3:
                    k3 = 1.0 - r_iris / 0.3
                    iris = [int(iris[k] + (IRIS_BRILLO[k] - iris[k]) * k3 * 0.5)
                            for k in range(3)]
                mez = min(1.0, (1.0 - r_iris) * 3.2)
                base = [int(base[k] + (iris[k] - base[k]) * mez) for k in range(3)]

                # --------------------------------------------------
                #  LA CAPA 4 — LA PUPILA ESTRELLA DE 5 PUNTAS
                # --------------------------------------------------
                if dentro_poligono(x, y, pts):
                    d = dist_estrella(x, y, pts)
                    if d < 1.4:
                        # EL FILO INCANDESCENTE de la estrella
                        k4 = 1.0 - d / 1.4
                        col = [int(PUPILA[k] + (FILO[k] - PUPILA[k]) * k4)
                               for k in range(3)]
                    else:
                        col = list(PUPILA)
                        # el vacío interno con SU núcleo mínimo (apenas
                        # un latido en el centro exacto de la estrella)
                        dcentro = math.hypot(x - (CX + 1.5), y - (CY - 1.0))
                        if dcentro < 1.2:
                            k5 = (1.0 - dcentro / 1.2) * 0.20
                            col = [int(col[k] + (FILO[k] - col[k]) * k5)
                                   for k in range(3)]
                    base = col
                else:
                    # EL HALO DE LA ESTRELLA — el dorado se agolpa en las
                    # puntas (la estrella QUEMA el iris alrededor)
                    d = dist_estrella(x, y, pts)
                    if d < 3.0:
                        k6 = (1.0 - d / 3.0) * 0.5
                        base = [int(base[k] + (FILO[k] - base[k]) * k6)
                                for k in range(3)]

            # ------------------------------------------------------
            #  LA CAPA 5 — EL DESTELLO HÚMEDO (el especular)
            # ------------------------------------------------------
            dsx, dsy = x - (CX - N * 0.16), y - (CY - N * 0.17)
            ds = math.hypot(dsx, dsy) / (N * 0.115)
            if ds < 1.0:
                k7 = (1.0 - ds) ** 1.6
                base = [int(base[k] + (DESTELLO[k] - base[k]) * k7 * 0.92)
                        for k in range(3)]

            # ------------------------------------------------------
            #  EL ENSAMBLE + EL PREMULTIPLICADO (q,q,q,q)
            # ------------------------------------------------------
            a = int(255 * borde)
            base = [max(0, min(255, c)) for c in base]
            # premultiplicado: rgb·m con m = borde (la convención del lote)
            m = borde
            px[x, y] = (
                int(base[0] * m),
                int(base[1] * m),
                int(base[2] * m),
                a,
            )

    img.save(DEST)
    print(f"OjoAbismo.png forjado: {DEST} ({N}×{N})")


def verificar():
    """LA VERIFICACIÓN DE LA CASA: tamaño exacto, RGBA, premultiplicado
    (rgb ≤ a; a=0 → rgb=0), y LA ESTRELLA de verdad (el centro negro,
    las puntas hacia arriba, el iris amarillo)."""
    img = Image.open(DEST)
    ok = True
    if img.size != (64, 64):
        print(f"✗ TAMAÑO: {img.size} ≠ (64, 64)")
        ok = False
    if img.mode != 'RGBA':
        print(f"✗ MODO: {img.mode} ≠ RGBA")
        ok = False
    px = img.load()
    malos = 0
    amarillos = 0
    negros = 0
    for y in range(64):
        for x in range(64):
            r, g, b, a = px[x, y]
            if a == 0 and (r or g or b):
                malos += 1
            elif a > 0 and (r > a or g > a or b > a):
                malos += 1
            if a > 100:
                if r > 150 and g > 110 and b < 110:
                    amarillos += 1
                if r < 30 and g < 30 and b < 40:
                    negros += 1
    if malos:
        print(f"✗ PREMULTIPLICADO: {malos} píxeles fuera de convención")
        ok = False
    if amarillos < 150:
        print(f"✗ IRIS AMARILLO: solo {amarillos} píxeles dorados (se esperaba el iris entero)")
        ok = False
    if negros < 25:
        print(f"✗ ESTRELLA: solo {negros} píxeles de pupila (la estrella no nació)")
        ok = False
    # LA PUNTA ARRIBA: el cuerpo negro en el centro de la estrella Y la
    # punta superior presente (oscura o de filo — la punta ES el filo)
    cx, cy = 32 + 1.5, 32 - 1.0
    r_ext = 64 * 0.205
    # (1) EL CORAZÓN de la estrella: negro del vacío
    r, g, b, a = px[int(cx), int(cy)]
    if not (a > 100 and r < 40 and g < 40 and b < 50):
        print(f"✗ CORAZÓN DE LA ESTRELLA: ({r},{g},{b},{a}) no es el negro del vacío")
        ok = False
    # (2) LA PUNTA SUPERIOR (ang = −π/2): debe existir estrella ahí
    punta = False
    px_, py_ = cx + math.cos(-math.pi / 2) * r_ext, cy + math.sin(-math.pi / 2) * r_ext
    for dx in range(-2, 3):
        for dy in range(-2, 3):
            x, y = int(px_ + dx), int(py_ + dy)
            if 0 <= x < 64 and 0 <= y < 64:
                r, g, b, a = px[x, y]
                if a > 60 and (r < 60 or (r > 180 and g > 150)):
                    punta = True
    if not punta:
        print("✗ ORIENTACIÓN: la punta de la estrella no está ARRIBA")
        ok = False
    print("✓ OJO ABISSMO AMARILLO VERIFICADO" if ok else "✗ REVISAR")
    return ok


if __name__ == '__main__':
    forjar()
    verificar()
