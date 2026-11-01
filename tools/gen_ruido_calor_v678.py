#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_ruido_calor_v678.py — EL RUIDO DE PLASMA DE LA DISTORSIÓN DE CALOR.

Genera RuidoPlasmaCalor.png (512², gris, TILEABLE): el campo que el
CalorDistorsionShader muestrea en TRES octavas con scrolls de convección
(diagonales opuestas — el aire hierve en su sitio). La receta (la
investigación v6.78):

  · SEAMLESS: value noise en retículas PERIÓDICAS (cada campo muestrea su
    retícula módulo el período) — el WRAP es exacto por construcción, sin
    promedios de esquinas.
  · CENTRADO EN 0,5: el shader remapea (r − 0,5)·2 → [−1, +1]; el gris
    medio es el aire quieto.
  · CARÁCTER PLASMÁTICO: fBm de 5 octavas (lacunaridad 2, ganancia 0,55)
    con DOMINIO WARP — dos campos de baja frecuencia tuercen las
    coordenadas de muestreo: nubes alargadas con VETAS (el heat haze de
    calidad lleva ruido de «humo», no de «arena» — la lección Rastertek).
  · CONTRASTE PLENO SIN CLIP DURO: normalización lineal al rango completo.

Uso: python3 tools/gen_ruido_calor_v678.py
"""
import math
import random
from PIL import Image

SIDE = 512
OUT = "AethonMod/Content/Effects/Procedural/RuidoPlasmaCalor.png"


class Ruido:
    """Value noise TILEABLE en una retícula periódica de período dado."""

    def __init__(self, period, semilla):
        rng = random.Random(semilla)
        self.period = period
        self.grid = [[rng.random() for _ in range(period)] for _ in range(period)]

    def __call__(self, x, y):
        """x, y en [0,1): celda = x·per; los índices de retícula van MÓDULO
        el período → la muestra en (0,0) y (1,0) es LA MISMA (wrap exacto)."""
        per = self.period
        fx, fy = x * per, y * per
        xi, yi = int(fx), int(fy)
        tx = fx - xi
        ty = fy - yi
        # smoothstep (la curva de la casa — RampaGlow et al.)
        tx = tx * tx * (3.0 - 2.0 * tx)
        ty = ty * ty * (3.0 - 2.0 * ty)
        x0, y0 = xi % per, yi % per
        x1, y1 = (xi + 1) % per, (yi + 1) % per
        g = self.grid
        v00, v10 = g[y0][x0], g[y0][x1]
        v01, v11 = g[y1][x0], g[y1][x1]
        return ((v00 * (1 - tx) + v10 * tx) * (1 - ty)
                + (v01 * (1 - tx) + v11 * tx) * ty)


def hacer_fbm(octavas, periodo_base, semilla):
    """fBm tileable: octavas con período duplicado (sigue dividiendo al
    tamaño — el wrap se conserva en TODAS las octavas)."""
    capas = [Ruido(periodo_base << o, semilla + o * 977)
             for o in range(octavas)]
    amplitudes = [0.55 ** o for o in range(octavas)]
    norma = sum(amplitudes)

    def fbm(x, y):
        t = 0.0
        for capa, a in zip(capas, amplitudes):
            t += a * capa(x, y)
        return t / norma

    return fbm


def main():
    # EL CAMPO PRINCIPAL: fBm de 5 octavas (período base 4 → 4, 8, 16, 32,
    # 64 celdas a lo ancho — la octava 1 del shader recorre ~7 celdas por
    # pantalla: nubes grandes que hieren, con detalle fino encima).
    principal = hacer_fbm(5, 4, 0xCA70)
    # EL DOMINIO WARP: dos campos SUAVES (2 octavas, período 3) que tuercen
    # las coordenadas — las nubes se alargan y tuercen (vetas de plasma).
    warp_x = hacer_fbm(2, 3, 0x1234)
    warp_y = hacer_fbm(2, 3, 0x5678)

    img = Image.new("L", (SIDE, SIDE))
    px = img.load()
    vmin, vmax = 1e9, -1e9
    vals = []
    for j in range(SIDE):
        fila = []
        for i in range(SIDE):
            u, v = i / SIDE, j / SIDE
            # el warp se aplica en espacio unitario y se envuelve: tileable
            wu = (u + 0.16 * (warp_x(u, v) - 0.5)) % 1.0
            wv = (v + 0.16 * (warp_y(u, v) - 0.5)) % 1.0
            n = principal(wu, wv)
            fila.append(n)
            vmin = min(vmin, n)
            vmax = max(vmax, n)
        vals.append(fila)

    span = max(vmax - vmin, 1e-9)
    for j in range(SIDE):
        for i in range(SIDE):
            t = (vals[j][i] - vmin) / span
            px[i, j] = int(round(t * 255))
    img.save(OUT)
    print("OK %s: %d×%d, rango [%.3f, %.3f] normalizado a 0-255"
          % (OUT, SIDE, SIDE, vmin, vmax))


if __name__ == "__main__":
    main()
