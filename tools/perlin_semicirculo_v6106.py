#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
perlin_semicirculo_v6106.py — v6.106 — LA FORJA DE LA MEDIALUNA PERLIN.

LA LETRA DEL USUARIO (la corrección del Aura de Juicio 2): «el
semicirculo esta al revez […] ademas el semicirculo debe tener la parte
recta mas difuminada algo asi como una cupula o medialuna».

LA AUTOPSIA DE LA v6.105: la cúpula era un semicírculo SÓLIDO y el
difuminado circular vivía DEBAJO del diámetro (un «rabo» que salía hacia
el portador, FUERA del semicírculo) — el degradado estaba AL REVÉS: la
parte recta quedaba MACIZA y lo único suave sobraba por fuera.

LA CURA (la medialuna de verdad):

  · LA PARTE RECTA DISUELTA HACIA ADENTRO — el interior de la cúpula se
    desvanece con ISOPLETAS CIRCULARES (arcos concéntricos al arco, la
    «transparencia en forma de círculo» de la letra original viviendo
    DENTRO): solo queda VIVA la CONCHA del arco — una MEDIALUNA.
  · NADA DEBAJO DEL DIÁMETRO — el rabo de la v6.105 MUERE: la textura
    termina limpia en la línea base.
  · LOS CUERNOS DE LA MEDIALUNA — la concha se ADELGAZA hacia los dos
    extremos del diámetro (el ahusado clásico de la luna) y engorda en
    lo alto del arco.
  · EL BORDE CURVO sigue naciendo fundido del fondo (pluma corta) y
    ahora lleva UN CALOR DE FILO — el labio exterior de la medialuna
    arde un punto más que el cuerpo (el núcleo caliente de la v6.105
    MUERE: el corazón ya no existe, es transparencia).
  · EL RUIDO PERLIN VIVE EN EL COLOR — las vetas y celdas de la nube
    morada y negra (5 octavas + la fina), la silueta es SIEMPRE la
    geometría pura (la ley de la casa).

Salida: AethonMod/Content/Effects/PerlinSemicirculo.png (256×256,
premultiplicada — la convención del lote de la casa).

Uso: python3 tools/perlin_semicirculo_v6106.py
"""
import math
import os

from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
BASE = os.path.join(AQUI, '..', 'AethonMod')
DEST = os.path.join(BASE, 'Content', 'Effects', 'PerlinSemicirculo.png')

N = 256
CX, CY = N / 2.0, 150.0        # el centro del disco EN el diámetro (la
                               # base de la medialuna cae donde SIEMPRE:
                               # el corazón del impío — el código del
                               # ancla no se toca)
R = 96.0                       # el radio del semicírculo (2R=192, el
                               # contrato del dibujado)
FADE_BORDE = 12.0              # la pluma del labio curvo (corta: la
                               # silueta se lee NÍTIDA)
D_MUERTE = 26.0                # debajo de este radio TODO muere: el
                               # corazón de la cúpula es TRANSPARENCIA
                               # (la parte recta disuelta)
D_PLENA = 62.0                 # desde este radio la concha es PLENA
CUERNO = 0.75                  # el ahusado de los cuernos (rad desde la
                               # horizontal): la concha muere EN la
                               # línea base y engorda hacia lo alto

# LOS COLORES DE LA CASA (horneados en la textura)
NEGRO_VIOLETA = (40, 22, 66)
VIOLETA_PROFUNDO = (72, 36, 118)
VIOLETA_VIVO = (150, 84, 235)
VIOLETA_CLARO = (224, 176, 255)


# ====================================================================
#  EL RUIDO PERLIN DE LA CASA (value noise fBm — el de siempre)
# ====================================================================
def _valor(semilla, x, y):
    h = semilla * 374761393 + x * 668265263 + y * 2246822519
    h = (h ^ (h >> 13)) * 1274126177
    h = h ^ (h >> 16)
    return (h & 0xFFFFFFFF) / 0xFFFFFFFF


def ruido(x, y, semilla=0):
    xi, yi = int(x), int(y)
    fx, fy = x - xi, y - yi
    fx = fx * fx * (3 - 2 * fx)
    fy = fy * fy * (3 - 2 * fy)
    a = _valor(semilla, xi, yi)
    b = _valor(semilla, xi + 1, yi)
    c = _valor(semilla, xi, yi + 1)
    d = _valor(semilla, xi + 1, yi + 1)
    return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy


def fbm(x, y, octavas=4, semilla=0):
    v = 0.0
    amp = 0.55
    f = 1.0
    for _ in range(octavas):
        v += amp * ruido(x * f, y * f, semilla + (octavas * 17))
        f *= 2.07
        amp *= 0.5
    return min(1.0, max(0.0, v))


def suave(k):
    """El smoothstep de la casa."""
    k = max(0.0, min(1.0, k))
    return k * k * (3.0 - 2.0 * k)


def forjar():
    img = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    px = img.load()

    for y in range(N):
        for x in range(N):
            dx = x - CX
            dy = y - CY
            d = math.hypot(dx, dy)

            # ======================================================
            #  1) LA MÁSCARA — SOLO la media cúpula superior, y
            #     NADA bajo el diámetro (el rabo de la v6.105 MUERE:
            #     la parte plana se difumina HACIA ADENTRO)
            # ======================================================
            if dy >= -1.0:
                continue
            if d >= R + FADE_BORDE:
                continue

            # ======================================================
            #  2) EL LABIO CURVO — la concha nace fundida del fondo
            #     (plena a d ≤ R−FADE_BORDE, muerta a d ≥ R+FADE_BORDE)
            # ======================================================
            if d <= R - FADE_BORDE:
                borde = 1.0
            else:
                borde = suave((R + FADE_BORDE - d) / (2.0 * FADE_BORDE))
                if borde <= 0.0:
                    continue

            # ======================================================
            #  3) LA PARTE RECTA DISUELTA — LA MEDIALUNA: la
            #     transparencia en forma de círculo vive AHORA DENTRO
            #     de la cúpula — isopletas CIRCULARES (arcos
            #     concéntricos al labio): la concha es plena desde
            #     D_PLENA hacia afuera y se muere hacia adentro hasta
            #     D_MUERTE. El corazón de la cúpula (la parte recta)
            #     es TRANSPARENCIA PURA — «la parte recta más
            #     difuminada, algo así como una cúpula o medialuna».
            # ======================================================
            plana = suave((d - D_MUERTE) / (D_PLENA - D_MUERTE))
            if plana <= 0.0:
                continue

            # ======================================================
            #  4) LOS CUERNOS DE LA MEDIALUNA — la concha se
            #     ADELGAZA hacia los extremos de la línea base (el
            #     ahusado de la luna) y engorda hacia el vértice:
            #     theta = el ángulo sobre la horizontal (0 en la
            #     base, 90° en lo alto del arco).
            # ======================================================
            theta = math.atan2(-dy, abs(dx))
            cuerno = suave(theta / CUERNO)
            if cuerno <= 0.0:
                continue

            # ======================================================
            #  5) EL RUIDO PERLIN — LA TEXTURA INTERIOR: las vetas y
            #     celdas de la nube morada y negra VIVEN EN EL COLOR,
            #     JAMÁS en el contorno (la ley de la casa). Escala
            #     uniforme: la nube es ISÓTROPA.
            # ======================================================
            n = fbm(x * 0.043, y * 0.043, octavas=5, semilla=6106)
            n = 0.82 * n + 0.18 * fbm(x * 0.11, y * 0.11, octavas=2, semilla=99)
            n = suave((n - 0.28) / 0.52)

            # EL CALOR DEL FILO — el labio exterior de la concha arde
            # un punto más que el cuerpo (el sustituto del núcleo
            # caliente muerto: el fuego vive AHORA en el borde)
            filo = suave((d - (R - 26.0)) / 26.0) * 0.30
            n = min(1.0, n + filo)

            # ======================================================
            #  6) LOS COLORES DE LA CASA — el ruido oscila del
            #     negro-violeta al violeta vivo con vetas claras
            # ======================================================
            if n < 0.45:
                kk = n / 0.45
                c0, c1 = NEGRO_VIOLETA, VIOLETA_PROFUNDO
            elif n < 0.78:
                kk = (n - 0.45) / 0.33
                c0, c1 = VIOLETA_PROFUNDO, VIOLETA_VIVO
            else:
                kk = (n - 0.78) / 0.22
                c0, c1 = VIOLETA_VIVO, VIOLETA_CLARO
            col = [int(c0[i] + (c1[i] - c0[i]) * kk) for i in range(3)]

            # ======================================================
            #  7) EL ALFA FINAL + EL PREMULTIPLICADO (q,q,q,q) —
            #     la SILUETA ES LA GEOMETRÍA: labio × medialuna ×
            #     cuernos (el ruido JAMÁS rompe el contorno)
            # ======================================================
            a = borde * plana * cuerno
            if a <= 0.004:
                continue
            alfa = int(255 * a)
            if alfa <= 2:
                continue
            px[x, y] = (
                int(col[0] * a),
                int(col[1] * a),
                int(col[2] * a),
                alfa,
            )

    img.save(DEST)
    print(f"PerlinSemicirculo.png forjado (LA MEDIALUNA): {DEST} ({N}×{N})")


def verificar():
    """LA VERIFICACIÓN DE LA CASA: tamaño, RGBA, premultiplicado, y LA
    CORRECCIÓN DE LA LETRA — (a) NADA bajo el diámetro (el rabo murió),
    (b) LA PARTE RECTA DISUELTA (el corazón de la cúpula es
    transparencia), (c) LA CONCHA VIVA (la medialuna se lee), (d) las
    ISOPLETAS CIRCULARES (el fade sube con el radio), (e) LOS CUERNOS
    (la concha se ahusa hacia la línea base)."""
    img = Image.open(DEST)
    ok = True
    if img.size != (256, 256) or img.mode != 'RGBA':
        print(f"✗ FORMATO: {img.size} {img.mode}")
        return False
    px = img.load()

    # 1) PREMULTIPLICADO
    malos = 0
    for y in range(0, 256, 3):
        for x in range(0, 256, 3):
            r, g, b, a = px[x, y]
            if (a == 0 and (r or g or b)) or (a > 0 and (r > a or g > a or b > a)):
                malos += 1
    if malos:
        print(f"✗ PREMULTIPLICADO: {malos} muestras fuera de convención")
        ok = False

    # 2) NADA BAJO EL DIÁMETRO (el rabo de la v6.105 MURIÓ)
    vivos_abajo = 0
    for y in range(152, 256):
        for x in range(0, 256, 2):
            if px[x, y][3] > 4:
                vivos_abajo += 1
    if vivos_abajo > 0:
        print(f"✗ EL RABO VIVE: {vivos_abajo} píxeles bajo el diámetro")
        ok = False

    # 3) LA PARTE RECTA DISUELTA — el corazón (bajo el vértice, cerca
    #    de la base) es TRANSPARENCIA (la v6.105 tenía ~250 allí)
    corazon = px[int(CX), int(CY) - 8][3]
    if corazon > 12:
        print(f"✗ LA PARTE RECTA MACIZA: alfa del corazón = {corazon} (debe ser ≤ 12)")
        ok = False

    # 4) LA CONCHA VIVE — el cuerpo de la medialuna (bajo el vértice,
    #    en la banda del arco: d=80) se lee FUERTE
    concha = px[int(CX), int(CY - 80)][3]
    if concha < 130:
        print(f"✗ LA MEDIALUNA VACÍA: alfa de la concha = {concha} (debe ser ≥ 130)")
        ok = False

    # 5) LAS ISOPLETAS CIRCULARES — por el rayo de 50° el alfa SUBE con
    #    el radio (d=40 < d=75 < d=86): el fade es un abanico de ARCOS
    import math as m
    th = m.radians(50)
    def alfa_rayo(d):
        xx = int(round(CX + d * m.cos(th)))
        yy = int(round(CY - d * m.sin(th)))
        return px[xx, yy][3]
    a40, a75, a86 = alfa_rayo(40), alfa_rayo(75), alfa_rayo(86)
    if not (a40 < a75 <= a86 + 8):
        print(f"✗ ISOPLETAS: d40={a40} d75={a75} d86={a86} (deben crecer con el radio)")
        ok = False

    # 6) LOS CUERNOS — a un mismo radio (d=80), la concha del CUERNO
    #    (θ=60°) es más GRUESA que la del extremo (θ=15°)
    def alfa_pol(d, grados):
        th = m.radians(grados)
        xx = int(round(CX + d * m.cos(th)))
        yy = int(round(CY - d * m.sin(th)))
        return px[xx, yy][3]
    cuerno = alfa_pol(80, 15)
    cuerpo = alfa_pol(80, 60)
    if not (cuerno < cuerpo - 25):
        print(f"✗ CUERNOS: θ15°={cuerno} no se ahusa frente a θ60°={cuerpo}")
        ok = False

    # EL MAPA — para el ojo de la forja (16×16)
    print("    EL MAPA DE LA MEDIALUNA (16×16):")
    for gy in range(16):
        row = ''
        for gx in range(16):
            xa, xb = gx * 16, (gx + 1) * 16
            ya, yb = gy * 16, (gy + 1) * 16
            tot = cnt = 0
            for yy in range(ya, yb, 3):
                for xx in range(xa, xb, 3):
                    tot += px[xx, yy][3]; cnt += 1
            v = tot / max(cnt, 1)
            row += '#' if v > 200 else '+' if v > 120 else '.' if v > 50 else ':' if v > 10 else ' '
        print(f'    {gy * 16:4d} {row}')

    print("✓ LA MEDIALUNA PERLIN VERIFICADA" if ok else "✗ REVISAR")
    return ok


if __name__ == '__main__':
    forjar()
    verificar()
