#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
estandarte_v6100.py — EL ESTANDARTE DEL JUICIO, FORJADO FIEL AL
ORIGINAL (v6.100).

LA LETRA DEL USUARIO: «el estandarte del juicio no tiene nada que ver
con el original y se ve peor, procedamos como siempre, primero copia
exactamente la version original al proyecto como investigacion».

LA INVESTIGACIÓN (research/README-ESTANDARTE.md): el ORIGINAL es un
mástil oscuro con soporte en cadena del que cuelga un COLGANTE DORADO
que se AFILA hacia abajo (pennant), con un EMBLEMA GRANDE naranja/rojo
al centro y LLAMAS ENVOLVENTES subiendo por la tela — 5 marcos que
hacen flicker a las llamas y mecer la tela.

LA VERSIÓN INTERRUMPIDA de la v6.100 falló el fiel: el VLM la leyó
como «linterna/antorcha» (tela cilíndrica, emblema mini, llamitas
tímidas). ESTA forja corrige los TRES pecados:

  1. LA TELA ES UN PENNANT — 18 px de ancho arriba afilando a PUNTA
     abajo (la silueta triangular del original), con sombreado de
     pliegues verticales y mecido de ±1-2 px por marco.
  2. EL EMBLEMA ES GRANDE — un SOL LLAMEANTE de 12×12 px al centro
     de la tela (naranja rojizo con núcleo blanco-cálido), el juicio
     de la Casa Sagrada.
  3. LAS LLAMAS ENVUELVEN — lenguas de fuego nacen del borde de la
     tela y del travesaño, ALTAS (hasta 7 px), cada una con su flicker
     determinista por marco, más 3 chispas que suben.

REGLA PERMANENTE DE LA CASA: TRANSPARENCIA NATIVA — el lienzo RGBA
nace transparente y solo se pintan los píxeles del estandarte; jamás
se post-procesa un fondo.

SALIDAS:
  · Content/Items/Accessories/EstandarteDelJuicioItem.png  (30×390 =
    5 marcos de 30×78 — el ítem animado a 6 ticks, como el original)
  · Content/DrawLayers/EstandarteBannerTrasero.png (30×280 = 5
    marcos de 30×56 — la tela de la espalda, meciéndose)

USO: python3 tools/estandarte_v6100.py
"""
import math
from PIL import Image

RUTA_ITEM = 'AethonMod/Content/Items/Accessories/EstandarteDelJuicioItem.png'
RUTA_ESPALDA = 'AethonMod/Content/DrawLayers/EstandarteBannerTrasero.png'

ANCHO, ALTO_MARCO, MARCOS = 30, 78, 5

# ============================================================
#  LA PALETA DE LA CASA SAGRADA (el oro y la luz)
# ============================================================
ORO_VIVO = (255, 200, 80, 255)       # la tela al sol
ORO_ALTO = (255, 226, 138, 255)      # el pliegue que da la luz
ORO_SOMBRA = (196, 132, 42, 255)     # el pliegue que no la ve
ORO_HONDO = (150, 94, 30, 255)       # el borde de la tela
EMBLEMA_ROJO = (204, 66, 30, 255)    # el sol del juicio — el rojo
EMBLEMA_ROJO2 = (232, 108, 40, 255)  # el halo del sol
NUCLEO = (255, 243, 190, 255)        # el corazón blanco-cálido
FUEGO_VIVO = (255, 160, 40, 255)     # las lenguas que suben
FUEGO_ALTO = (255, 222, 120, 255)    # la punta de la lengua
MASTIL = (62, 58, 82, 255)           # el mástil — hierro nocturno
MASTIL_LUZ = (104, 100, 132, 255)    # el filo del mástil
REMATE = (255, 226, 138, 255)        # el regatón dorado del mástil
CHISPA = (255, 240, 180, 255)        # las chispas que vuelan


def h01(semilla, t):
    """El hash determinista de la casa (murmur-3 finalizer)."""
    h = (semille := int(semilla) * 0x27D4EB2D + int(t) * 0x165667B1) & 0xFFFFFFFF
    h ^= h >> 15; h = (h * 0x85EBCA6B) & 0xFFFFFFFF
    h ^= h >> 13; h = (h * 0xC2B2AE35) & 0xFFFFFFFF
    h ^= h >> 16
    return h / 0xFFFFFFFF


class Lienzo:
    """Un marco del estandarte: lienzo RGBA transparente con pintado
    de celdas (pixel art de la casa — cada celda 1 px)."""

    def __init__(self):
        self.px = [[(0, 0, 0, 0)] * ANCHO for _ in range(ALTO_MARCO)]

    def pon(self, x, y, c):
        if 0 <= x < ANCHO and 0 <= y < ALTO_MARCO:
            self.px[y][x] = c

    def hay(self, x, y):
        return 0 <= x < ANCHO and 0 <= y < ALTO_MARCO and self.px[y][x][3] != 0

    def imagen(self):
        img = Image.new('RGBA', (ANCHO, ALTO_MARCO))
        img.putdata([c for fila in self.px for c in fila])
        return img


# ============================================================
#  EL ESTANDARTE DE UN MARCO
# ============================================================
def forjar_marco(marco):
    """Forja el marco `marco` (0..4) del estandarte-ítem (30×78)."""
    L = Lienzo()
    t = marco / MARCOS * math.tau          # la fase del ciclo
    mecer = math.sin(t) * 2.6              # la tela se mece ±2.6 px (viva)
    # ------------------------------------------------------------
    # 1) EL MÁSTIL — hierro nocturno de y=1 a y=77, x=4-6, con su
    #    filo de luz a la izquierda y el REMATE dorado arriba.
    # ------------------------------------------------------------
    for y in range(1, 78):
        L.pon(4, y, MASTIL_LUZ)
        L.pon(5, y, MASTIL)
        L.pon(6, y, MASTIL)
    L.pon(5, 0, REMATE); L.pon(6, 0, REMATE)
    L.pon(4, 1, REMATE); L.pon(7, 1, MASTIL)
    # ------------------------------------------------------------
    # 2) EL TRAVESAÑO — la barra de la que cuelga la tela (y=8-10),
    #    saliendo del mástil hacia la derecha, con su cadena corta.
    # ------------------------------------------------------------
    for x in range(5, 24):
        L.pon(x, 8, MASTIL_LUZ if x % 3 == 0 else MASTIL)
        L.pon(x, 9, MASTIL)
    L.pon(23, 7, MASTIL); L.pon(23, 10, MASTIL)   # el tope del gancho
    # LA CADENA: 2 eslabones diagonales hasta la tela
    L.pon(21, 10, MASTIL_LUZ); L.pon(20, 11, MASTIL_LUZ)
    # ------------------------------------------------------------
    # 3) LA TELA — EL PENNANT: ancho 18 arriba (x=7..24 en y=12)
    #    afilando a PUNTA en y=66. El borde izquierdo sigue al mástil;
    #    el derecho entra 1 px cada 3 filas. El mecido desplaza la
    #    mitad baja (la tela pivota desde el travesaño) y una ONDA
    #    viaja por los pliegues (la tela no es tabla).
    # ------------------------------------------------------------
    for y in range(12, 67):
        prog = (y - 12) / 54.0                 # 0 arriba → 1 en la punta
        # la punta se afila: el ancho mengua de 18 a 1
        ancho = max(1, round(18 * (1.0 - prog) ** 1.15))
        # EL MECIDO: la torsión crece hacia la punta (la tela pivota)
        swing = round(mecer * prog + math.sin(t * 2.0 + prog * 5.0) * 0.8 * prog)
        x0 = 7 + swing
        x1 = x0 + ancho - 1
        for x in range(x0, x1 + 1):
            u = (x - x0) / max(1, ancho - 1)   # 0 borde izq → 1 borde der
            # LOS PLIEGUES: 3 columnas de sombra que dan volumen — la
            # ONDA viaja: el patrón se desplaza con la altura y el marco
            pliegue = math.sin(u * math.pi * 2.6 + t * 1.6 + prog * 4.5)
            if u < 0.08 or u > 0.92:
                c = ORO_HONDO                   # los bordes de la tela
            elif pliegue > 0.45:
                c = ORO_ALTO                    # el pliegue al sol
            elif pliegue < -0.45:
                c = ORO_SOMBRA                  # el pliegue en sombra
            else:
                c = ORO_VIVO
            L.pon(x, y, c)
    # ------------------------------------------------------------
    # 4) EL EMBLEMA — EL SOL LLAMEANTE DEL JUICIO (17×17 DOMINANDO
    #    la tela, y=24..42): un disco rojizo con 8 puntas de llama y
    #    el NÚCLEO blanco-cálido — LATA con el marco. ES la firma.
    # ------------------------------------------------------------
    late = 0.5 + 0.5 * math.sin(t * 2.0)        # el latido del sol
    cx, cy = 14 + round(mecer * 0.3), 33
    for dy in range(-9, 10):
        for dx in range(-9, 10):
            d = math.hypot(dx, dy)
            ang = math.atan2(dy, dx)
            # LAS PUNTAS: el radio crece en 8 direcciones (la llama)
            punta = 0.72 + 0.28 * max(0.0, math.cos(ang * 8 - t))
            r = 8.6 * punta * (0.93 + 0.07 * late)
            if d <= r:
                if d <= 3.0 + 1.2 * late:
                    c = NUCLEO                  # el corazón late
                elif d <= r * 0.70:
                    c = EMBLEMA_ROJO
                else:
                    c = EMBLEMA_ROJO2
                L.pon(cx + dx, cy + dy, c)
    # ------------------------------------------------------------
    # 5) EL FUEGO QUE ENVUELVE — como el original: la tela ARDE desde
    #    ARRIBA (las lenguas nacen del borde superior y suben
    #    fundiéndose con el travesaño — la tela arde consumiéndose),
    #    MÁS las lenguas de ambos bordes siguiendo el afilado y la
    #    corona de la punta. Lenguas ANCHAS (2-3 px) y ALTAS.
    # ------------------------------------------------------------
    # EL FUEGO DE LA CIMBRA — la masa principal: lenguas de 5-9 px
    # cada 2 px a lo largo del borde superior de la tela (y=12).
    for i, lx in enumerate(range(8, 24, 2)):
        f = h01(31 + i * 3, marco * 7 + i)
        alto = 5 + round(f * 4) + (1 if lx in (12, 16) else 0)
        for k in range(alto):
            y = 12 - k
            if y < 1:
                break
            ancho = 1 if k > alto - 3 else (2 if k > 1 else 3)
            for dx in range(ancho):
                c = FUEGO_ALTO if k >= alto - 2 else FUEGO_VIVO
                L.pon(lx + dx - (ancho // 2) + round((f - 0.5) * (k // 4)), y, c)
    # las lenguas del BORDE DERECHO (siguen la tela que se afila)
    for y in range(16, 62, 3):
        prog = (y - 12) / 54.0
        swing = round(mecer * prog)
        borde_der = 7 + swing + max(1, round(18 * (1.0 - prog) ** 1.15)) - 1
        f = h01(31 + y, marco * 7)
        alto = 3 + round(f * 4)
        for k in range(alto):
            x = borde_der + (0 if k < 2 else 1)   # la lengua SE APOYA en el borde
            L.pon(x, y - k, FUEGO_ALTO if k >= alto - 2 else FUEGO_VIVO)
            if k < 2:
                L.pon(x - 1, y - k, FUEGO_VIVO)  # el abrazo al borde
    # las lenguas del BORDE IZQUIERDO (pegadas al mástil, subiendo)
    for y in range(18, 58, 4):
        prog = (y - 12) / 54.0
        swing = round(mecer * prog)
        f = h01(91 + y, marco * 7)
        alto = 3 + round(f * 4)
        for k in range(alto):
            x = 6 + swing
            L.pon(x, y - k, FUEGO_ALTO if k >= alto - 1 else FUEGO_VIVO)
            if k < 2:
                L.pon(x + 1, y - k, FUEGO_VIVO)
    # las lenguas del TRAVESAÑO (los extremos arden más alto)
    for i, (lx, ly_base, alto) in enumerate([(9, 9, 6), (13, 9, 7), (17, 10, 7), (21, 10, 6)]):
        f = h01(31 + i, marco * 7 + i)          # el flicker de ESTA lengua
        alto_f = max(4, alto - 1 + round(f * 2))
        deriva = round((f - 0.5) * 2)           # la lengua se ladilla
        for k in range(alto_f):
            y = ly_base - k
            x = lx + deriva * (k // 3)
            ancho = 1 if k > alto_f - 3 else (2 if k > 1 else 3)
            for dx in range(ancho):
                c = FUEGO_ALTO if k >= alto_f - 2 else FUEGO_VIVO
                L.pon(x + dx - (ancho // 2), y, c)
    # LA CORONA DE LA PUNTA (la cola del pennant ARDE — 3 lenguas)
    for i in range(3):
        f = h01(77 + i, marco * 5)
        py = 66 + i - round(f * 2)
        px_ = 8 + i * 4 + round(mecer)
        L.pon(px_, py, FUEGO_VIVO)
        L.pon(px_, py - 1, FUEGO_ALTO)
        L.pon(px_ - 1, py, FUEGO_VIVO)
        if f > 0.6:
            L.pon(px_ + 1, py - 2, FUEGO_ALTO)
    # ------------------------------------------------------------
    # 6) LAS CHISPAS — 5 puntos blanco-cálido que suben SOBRE la
    #    tela (nunca bajo la punta), cada una en SU posición.
    # ------------------------------------------------------------
    for i in range(5):
        sx = 8 + int(h01(50 + i, 0) * 15) + round(mecer * 0.5)
        sy = 20 - int((marco / MARCOS + h01(51 + i, 3)) * 14) % 15
        L.pon(sx, max(2, sy), CHISPA)
    return L


# ============================================================
#  LA TELA DE LA ESPALDA (30×56 por marco) — el mismo pennant sin
#  mástil largo: el regatón corto, la tela meciéndose A LA ESPALDA
#  DEL JUGADOR (el DrawLayer la ancla a la espalda y la voltea).
# ============================================================
ALTO_ESPALDA = 56

class LienzoEspalda(Lienzo):
    def __init__(self):
        self.px = [[(0, 0, 0, 0)] * ANCHO for _ in range(ALTO_ESPALDA)]


def forjar_espalda(marco):
    L = LienzoEspalda()
    t = marco / MARCOS * math.tau
    mecer = math.sin(t) * 2.2
    # EL ASTA CORTA (la que el portador carga — baja hasta la punta
    # de la tela: sin muñón flotante)
    for y in range(2, 52):
        L.pon(4, y, MASTIL_LUZ)
        L.pon(5, y, MASTIL)
        L.pon(6, y, MASTIL)
    L.pon(5, 0, REMATE); L.pon(6, 0, REMATE); L.pon(4, 1, REMATE)
    # EL TRAVESAÑO
    for x in range(5, 23):
        L.pon(x, 7, MASTIL_LUZ if x % 3 == 0 else MASTIL)
        L.pon(x, 8, MASTIL)
    # LA TELA (pennant corto: y=10..48)
    for y in range(10, 49):
        prog = (y - 10) / 39.0
        ancho = max(1, round(16 * (1.0 - prog) ** 1.15))
        swing = round(mecer * prog + math.sin(t * 2.0 + prog * 5.0) * 0.7 * prog)
        x0 = 6 + swing
        for x in range(x0, x0 + ancho):
            u = (x - x0) / max(1, ancho - 1)
            pliegue = math.sin(u * math.pi * 2.6 + t * 1.6 + prog * 4.5)
            if u < 0.08 or u > 0.92:
                c = ORO_HONDO
            elif pliegue > 0.45:
                c = ORO_ALTO
            elif pliegue < -0.45:
                c = ORO_SOMBRA
            else:
                c = ORO_VIVO
            L.pon(x, y, c)
    # EL EMBLEMA (sol grande — 8.2 px de radio, domina la tela)
    late = 0.5 + 0.5 * math.sin(t * 2.0)
    cx, cy = 13 + round(mecer * 0.3), 24
    for dy in range(-8, 9):
        for dx in range(-8, 9):
            d = math.hypot(dx, dy)
            ang = math.atan2(dy, dx)
            punta = 0.72 + 0.28 * max(0.0, math.cos(ang * 8 - t))
            r = 8.2 * punta * (0.93 + 0.07 * late)
            if d <= r:
                c = NUCLEO if d <= 2.8 + 1.0 * late else (EMBLEMA_ROJO if d <= r * 0.7 else EMBLEMA_ROJO2)
                L.pon(cx + dx, cy + dy, c)
    # EL FUEGO DE LA CIMBRA — la masa principal (como el ítem: la tela
    # arde desde arriba, las lenguas se funden con el travesaño)
    for i, lx in enumerate(range(8, 22, 2)):
        f = h01(31 + i * 3, marco * 7 + i)
        alto = 4 + round(f * 3)
        for k in range(alto):
            y = 10 - k
            if y < 1:
                break
            ancho = 1 if k > alto - 3 else (2 if k > 1 else 3)
            for dx in range(ancho):
                c = FUEGO_ALTO if k >= alto - 2 else FUEGO_VIVO
                L.pon(lx + dx - (ancho // 2) + round((f - 0.5) * (k // 4)), y, c)
    # LAS LLAMAS del borde derecho (siguen el afilado)
    for y in range(14, 46, 3):
        prog = (y - 10) / 39.0
        swing = round(mecer * prog)
        borde_der = 6 + swing + max(1, round(16 * (1.0 - prog) ** 1.15)) - 1
        f = h01(31 + y, marco * 7)
        alto = 3 + round(f * 3)
        for k in range(alto):
            L.pon(borde_der + (0 if k < 2 else 1), y - k, FUEGO_ALTO if k >= alto - 2 else FUEGO_VIVO)
            if k < 2:
                L.pon(borde_der - 1, y - k, FUEGO_VIVO)
    # las del borde izquierdo (pegadas al asta, subiendo)
    for y in range(16, 42, 4):
        prog = (y - 10) / 39.0
        swing = round(mecer * prog)
        f = h01(91 + y, marco * 7)
        alto = 3 + round(f * 3)
        for k in range(alto):
            L.pon(6 + swing, y - k, FUEGO_ALTO if k >= alto - 1 else FUEGO_VIVO)
            if k < 2:
                L.pon(7 + swing, y - k, FUEGO_VIVO)
    # las del travesaño
    for i, (lx, ly, alto) in enumerate([(9, 7, 5), (13, 7, 6), (17, 8, 6), (20, 8, 5)]):
        f = h01(31 + i, marco * 7 + i)
        alto_f = max(4, alto - 1 + round(f * 2))
        for k in range(alto_f):
            x = lx + round((f - 0.5) * 2) * (k // 3)
            ancho = 1 if k > alto_f - 3 else (2 if k > 1 else 3)
            for dx in range(ancho):
                L.pon(x + dx - (ancho // 2), ly - k, FUEGO_ALTO if k >= alto_f - 2 else FUEGO_VIVO)
    # la corona de la punta
    for i in range(2):
        f = h01(77 + i, marco * 5)
        py = 47 + i - round(f * 2)
        px_ = 8 + i * 4 + round(mecer)
        L.pon(px_, py, FUEGO_VIVO); L.pon(px_, py - 1, FUEGO_ALTO)
        L.pon(px_ - 1, py, FUEGO_VIVO)
    # LAS CHISPAS
    for i in range(4):
        sx = 8 + int(h01(60 + i, 0) * 13) + round(mecer * 0.5)
        sy = 16 - int((marco / MARCOS + h01(61 + i, 3)) * 12) % 13
        L.pon(sx, max(2, sy), CHISPA)
    return L


def main():
    # EL ÍTEM: 5 marcos de 30×78 apilados → 30×390
    tira = Image.new('RGBA', (ANCHO, ALTO_MARCO * MARCOS), (0, 0, 0, 0))
    for m in range(MARCOS):
        marco = forjar_marco(m).imagen()
        tira.paste(marco, (0, m * ALTO_MARCO))
    tira.save(RUTA_ITEM)
    # LA ESPALDA: 5 marcos de 30×56 → 30×280
    tira2 = Image.new('RGBA', (ANCHO, ALTO_ESPALDA * MARCOS), (0, 0, 0, 0))
    for m in range(MARCOS):
        marco = forjar_espalda(m).imagen()
        tira2.paste(marco, (0, m * ALTO_ESPALDA))
    tira2.save(RUTA_ESPALDA)
    print(f'OK — {RUTA_ITEM} {tira.size} · {RUTA_ESPALDA} {tira2.size}')


if __name__ == '__main__':
    main()
