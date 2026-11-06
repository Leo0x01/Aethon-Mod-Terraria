#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
perlin_semicirculo_v6105.py — v6.105 — LA FORJA DEL SEMICÍRCULO PERLIN.

LA LETRA DEL USUARIO (el efecto del Aura de Juicio 2 sobre los
enemigos): «sobre los enemigos agrega un efecto que sea una textura
de perlin circular y que solo sea un semicirculo con un difuminado y
transparencia en forma de circulo en su parte plana».

LA PIEZA (256×256, premultiplicada — la convención del lote de la
casa):

  · UNA TEXTURA DE PERLIN CIRCULAR — ruido fBm de 4 octavas VIVO
    dentro de un disco (la nube de energía del vacío exterior, con
    vetas finas y celdas que respiran).
  · QUE SOLO SEA UN SEMICÍRCULO — la mitad INFERIOR del disco muere:
    queda la media cúpula superior (la cúpula que ARDE sobre el
    impío).
  · CON UN DIFUMINADO Y TRANSPARENCIA EN FORMA DE CÍRCULO EN SU
    PARTE PLANA — el diámetro NO es un corte recto: bajo la línea
    base la textura se desvanece siguiendo EL ARCO DE UN CÍRCULO
    (profundo en el centro, corto en los extremos — la huella
    circular de la media cúpula deshaciéndose en el aire).
  · EL BORDE CURVO también se difumina (suave, sin recorte): la
    cúpula NACE fundida del fondo.
  · LOS COLORES DE LA CASA horneados: el ruido oscila entre EL
    NEGRO-VIOLETA (46,22,74) y EL VIOLETA VIVO (185,110,255) con
    vetas de VIOLETA CLARO (224,176,255) — morado y negro, la casa.

Salida: AethonMod/Content/Effects/PerlinSemicirculo.png (256×256).

Uso: python3 tools/perlin_semicirculo_v6105.py
"""
import math
import os

from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
BASE = os.path.join(AQUI, '..', 'AethonMod')
DEST = os.path.join(BASE, 'Content', 'Effects', 'PerlinSemicirculo.png')

N = 256
CX, CY = N / 2.0, 150.0        # el centro del disco EN el diámetro (bajado:
                               # la cúpula, su pluma Y el fade de la base
                               # caben con margen — QA r3: nada cortado)
R = 96.0                       # el radio del semicírculo (margen 8 px)
FADE_BORDE = 14.0              # el difuminado del arco curvo (QA r4: la
                               # pluma CORTA — la silueta se lee NÍTIDA)
FADE_BASE = 54.0               # la PROFUNDIDAD del difuminado circular de la
                               # base (QA r3: MÁS dramático — el arco se LEE)

# LOS COLORES DE LA CASA (horneados en la textura)
NEGRO_VIOLETA = (40, 22, 66)      # QA r5: subido — el cuerpo oscuro se lee
                                   # CUERPO, no hueco, sobre cualquier fondo
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
            #  1) LA MÁSCARA DEL SEMICÍRCULO — solo la mitad
            #     superior del disco VIVE (la media cúpula)
            # ======================================================
            dentro_disco = d < R + FADE_BORDE
            arriba = dy < FADE_BASE  # la base tiene su zona de fade
            if not dentro_disco or not arriba:
                continue

            # ======================================================
            #  2) EL DIFUMINADO DEL BORDE CURVO — la cúpula nace
            #     fundida del fondo (nada de recortes): plena a
            #     d ≤ R−FADE_BORDE, muerta a d ≥ R+FADE_BORDE
            # ======================================================
            if d <= R - FADE_BORDE:
                borde = 1.0
            elif d >= R + FADE_BORDE:
                continue
            else:
                borde = suave((R + FADE_BORDE - d) / (2.0 * FADE_BORDE))

            # ======================================================
            #  3) EL DIFUMINADO CIRCULAR DE LA PARTE PLANA — la
            #     letra: «transparencia en forma de círculo en su
            #     parte plana». Bajo el diámetro (dy > 0) el alfa
            #     muere dentro de UN CÍRCULO de radio FADE_BASE
            #     centrado en el corazón del disco: LAS ISOPLETAS
            #     DEL FADE SON ARCOS CIRCULARES (la huella del
            #     círculo — profundo al centro, corta en los
            #     extremos; la receta del QA r4: el desvanecimiento
            #     VIVE dentro del círculo, no en una recta).
            # ======================================================
            base = 1.0
            if dy > 0.0:
                dr = math.hypot(dx, dy)
                base = suave((FADE_BASE - dr) / FADE_BASE)
            if base <= 0.0:
                continue

            # ======================================================
            #  4) EL RUIDO PERLIN CIRCULAR — LA TEXTURA INTERIOR:
            #     la letra pide «una textura de perlin circular» con
            #     la SILUETA de un semicírculo — el ruido VIVE en el
            #     COLOR (las vetas y celdas de la nube), NUNCA en el
            #     contorno. ESCALA UNIFORME (QA r2: la escala variable
            #     por distancia ESTIRABA el ruido en rayas radiales —
            #     la nube es ISÓTROPA, orgánica en toda la cúpula)
            # ======================================================
            n = fbm(x * 0.043, y * 0.043, octavas=5, semilla=6105)
            # LA OCTAVA FINA (QA r3: densidad — los huecos negros
            # grandes se parten en celdas menudas)
            n = 0.82 * n + 0.18 * fbm(x * 0.11, y * 0.11, octavas=2, semilla=99)
            # EL CONTRASTE de la nube: vetas vivas y huecos profundos
            n = suave((n - 0.28) / 0.52)

            # EL NÚCLEO CALIENTE — el corazón arde (QA r5: bajado a
            # 0.30 — el brillo ENMASCARABA los arcos del fade de la
            # base; el corazón vive, los arcos se leen)
            nucleo = max(0.0, 1.0 - d / (R * 0.55))
            n = min(1.0, n * 0.72 + nucleo * 0.30)

            # ======================================================
            #  5) LOS COLORES DE LA CASA — el ruido oscila del
            #     negro-violeta al violeta vivo con vetas claras
            # ======================================================
            if n < 0.45:
                k = n / 0.45
                c0, c1 = NEGRO_VIOLETA, VIOLETA_PROFUNDO
            elif n < 0.78:
                k = (n - 0.45) / 0.33
                c0, c1 = VIOLETA_PROFUNDO, VIOLETA_VIVO
            else:
                k = (n - 0.78) / 0.22
                c0, c1 = VIOLETA_VIVO, VIOLETA_CLARO
            col = [int(c0[i] + (c1[i] - c0[i]) * k) for i in range(3)]

            # ======================================================
            #  6) EL ALFA FINAL + EL PREMULTIPLICADO (q,q,q,q)
            #     LA SILUETA ES LA GEOMETRÍA (la cúpula SÓLIDA y
            #     limpia — la QA v1: el ruido JAMÁS rompe el contorno)
            # ======================================================
            a = borde * base
            a = max(0.0, min(1.0, a))
            if a <= 0.004:
                continue
            # el cuerpo denso: la cúpula es CUERPO SÓLIDO (QA r3:
            # se lee CÚPULA, no nube — densidad plena en el corazón)
            a_cuerpo = a * (0.95 + 0.05 * nucleo)
            alfa = int(255 * a_cuerpo)
            if alfa <= 2:
                continue
            m = a_cuerpo
            px[x, y] = (
                int(col[0] * m),
                int(col[1] * m),
                int(col[2] * m),
                alfa,
            )

    img.save(DEST)
    print(f"PerlinSemicirculo.png forjado: {DEST} ({N}×{N})")


def verificar():
    """LA VERIFICACIÓN DE LA CASA: tamaño, RGBA, premultiplicado, la
    GEOMETRÍA (nada bajo −FADE_BASE del diámetro; el pico de masa en
    la mitad superior; el fade de la base en FORMA DE CÍRCULO —
    profundo al centro, corto en los extremos)."""
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

    # 2) LA MITAD INFERIOR MUERE — a y > CY + FADE_BASE no hay NADA
    vivos_abajo = 0
    for y in range(int(CY + FADE_BASE) + 2, 256, 2):
        for x in range(0, 256, 2):
            if px[x, y][3] > 4:
                vivos_abajo += 1
    if vivos_abajo > 0:
        print(f"✗ SEMICÍRCULO: {vivos_abajo} píxeles vivos bajo la zona del fade")
        ok = False

    # 3) LA MASA VIVE ARRIBA — el corazón (CX, CY−R·0.45) tiene alfa alto
    arriba = px[int(CX), int(CY - R * 0.45)][3]
    if arriba < 100:
        print(f"✗ LA CÚPULA VACÍA: alfa del corazón = {arriba}")
        ok = False

    # 4) EL FADE DE LA BASE ES CIRCULAR — el píxel (CX, CY + FADE_BASE·0.45)
    #    (centro, hondo en el fade) debe tener MÁS alfa que
    #    (CX ± R·0.8, CY + FADE_BASE·0.45) (extremo, el fade muere pronto)
    yy = int(CY + FADE_BASE * 0.45)
    centro = px[int(CX), yy][3]
    extremo = max(px[int(CX + R * 0.8), yy][3], px[int(CX - R * 0.8), yy][3])
    if centro <= extremo + 6:
        print(f"✗ FADE CIRCULAR: centro={centro} ≤ extremo={extremo} (la base no es circular)")
        ok = False

    # 5) EL BORDE CURVO SE DIFUMINA — en el tope (CX, CY−R−FADE_BORDE·0.3)
    #    hay alfa intermedio (fundido), no un corte
    yf = int(CY - R - FADE_BORDE * 0.3)
    fundido = px[int(CX), yf][3] if 0 <= yf < 256 else -1
    if not (4 < fundido < 200):
        print(f"✗ BORDE CURVO: alfa del fundido = {fundido} (debe ser intermedio)")
        ok = False

    print("✓ PERLIN SEMICÍRCULO VERIFICADO" if ok else "✗ REVISAR")
    return ok


if __name__ == '__main__':
    forjar()
    verificar()
