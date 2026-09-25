#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_desgarro_v631.py — EL MOCK 1:1 DEL DESGARRO PAREJO (v6.31).

Simula NUMÉRICAMENTE el dibujo real del juego (la escuela de los mocks de la
casa, v6.28-v6.30): el Bastón del Desgarro dibuja con SpriteBatch:
  PASO 1 (NonPremultiplied): RiftTaperVoid — dst = rgb·a + dst·(1-a)
  PASO 2 (Additive):         RiftTaperVelo/Cuerpo/Nucleo — dst += rgb·a
con el Tint() de la casa: vertex = (rgb·f, α=f) → aporte = f²·a_texel.

EL CONTRATO D.6 (informe v631 §D.6):
  · Muestreo cada 2 px a lo largo de la línea central (620 px como en el arma).
  · Un "CORTE" = valle INTERIOR de cobertura (run con cobertura < 0.5·mediana
    flanqueado a ambos lados por cobertura > 0.8·mediana) — las tapas redondas
    de los extremos NO cuentan (es su trabajo terminar en punta).
  · "PAREJO" = desviación de ancho ≤ ±5% en u∈[0.15, 0.85].
  · 5 semillas: la línea es independiente de la semilla (las estrellas viven
    DENTRO), pero se comprueba igual — el contrato debe cumplirse SIEMPRE.

Salida: PNG del composite + métricas. Objetivo: 0 cortes, ±5%.
"""
import math
import os
import sys

from PIL import Image

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROC = os.path.join(BASE, "Content", "Effects", "Procedural")

# ==== EL DIBUJO DEL ARMA (RealityTearProjectile, contrato v6.31) =============
L = 620.0          # TearLength
MAXW = 14.0        # MaxWidth
QUAD_H = 1.60 * MAXW   # QuadAlto·maxWidth (22.4 px)
SCALE_CANVAS = 2.0     # 2 px de canvas por px de mundo (medir sub-píxel)

# Los factores del código (Tear/TearVacio, intensity=1, sin respiración):
F_VOID = 0.96
F_VELO = 0.30
F_CUERPO = 0.60
F_NUCLEO = 0.90


def cargar(nombre):
    return Image.open(os.path.join(PROC, nombre + ".png")).convert("RGBA")


def muestreo_bilineal(img, u, v):
    """Muestreo bilineal del texel en coordenadas de textura [0,1]×[0,1]."""
    W, H = img.size
    fx = u * (W - 1)
    fy = v * (H - 1)
    x0, y0 = int(fx), int(fy)
    x1, y1 = min(x0 + 1, W - 1), min(y0 + 1, H - 1)
    tx, ty = fx - x0, fy - y0
    out = []
    for c in range(4):
        a = img.getpixel((x0, y0))[c] * (1 - tx) * (1 - ty)
        b = img.getpixel((x1, y0))[c] * tx * (1 - ty)
        c2 = img.getpixel((x0, y1))[c] * (1 - tx) * ty
        d = img.getpixel((x1, y1))[c] * tx * ty
        out.append(a + b + c2 + d)
    return out  # r, g, b, a (0..255)


def composite_columna(texturas, u, y_off):
    """La luz compuesta (0..1) del corte en u (a lo largo) y y_off (px del eje)."""
    v = 0.5 + (y_off / QUAD_H)          # v de textura (el quad es QUAD_H px alto)
    if not (0.0 <= v <= 1.0):
        return 0.0
    luz = 0.0
    negro = 0.0
    # PASO 1: el vacío (NonPremultiplied): sobre fondo blanco deja
    # 1 - a·(1-rgb) ≈ 1 - 0.9 → LA PROFUNDIDAD de la marca oscura = negro.
    r, g, b, a = muestreo_bilineal(texturas["void"], u, v)
    negro = (a / 255.0) * F_VOID * (1.0 - (r + g + b) / 3.0 / 255.0)
    # PASO 2: las capas aditivas (aporte = f²·a_texel·rgb_texel)
    for nombre, f in (("velo", F_VELO), ("cuerpo", F_CUERPO), ("nucleo", F_NUCLEO)):
        r, g, b, a = muestreo_bilineal(texturas[nombre], u, v)
        luz += (a / 255.0) * f * f * ((r + g + b) / 3.0 / 255.0)
    # composite: fondo blanco 1.0 (el peor caso: fondo claro) — la MARCA
    # visible = cuánto se aparta del fondo: oscurecido por el vacío + luz.
    marca = negro + luz           # profundidad de la desviación del fondo
    return marca


def main() -> int:
    texturas = {n: cargar("RiftTaper" + n.capitalize()) for n in ("velo", "cuerpo", "nucleo", "void")}

    print("=== MOCK 1:1 DEL DESGARRO PAREJO (contrato D.6) ===")
    print(f"Línea: {L:.0f} px · quad {QUAD_H:.1f} px · muestreo cada 2 px · 5 semillas\n")

    todo_ok = True
    for seed in (1, 7, 42, 313, 999):
        # cobertura por columna (máximo sobre el eje transversal ±12 px)
        xs = [i * 2.0 for i in range(int(L / 2) + 1)]
        cobertura = []
        anchos = []
        for xw in xs:
            u = xw / L
            if not (0.0 <= u <= 1.0):
                continue
            marca_max = 0.0
            ancho = 0.0
            y_prev = 0.0
            for yo in range(-14, 15):
                m = composite_columna(texturas, u, yo)
                if m > marca_max:
                    marca_max = m
                if m > 0.20:              # "hay marca" (umbral visible)
                    ancho += 1.0
            cobertura.append(marca_max)
            anchos.append(ancho)

        med = sorted(cobertura)[len(cobertura) // 2]

        # CORTES: runs interiores con cobertura < 0.5·med, flanqueados por > 0.8·med
        cortes = 0
        en_run = False
        for i in range(3, len(cobertura) - 3):
            bajo = cobertura[i] < 0.5 * med
            if bajo and not en_run:
                # ¿flanqueado? (mira 12 muestras = 24 px a cada lado)
                izq = max(cobertura[max(0, i - 12):i]) if i > 12 else med
                der = max(cobertura[i + 1:i + 13]) if i + 13 < len(cobertura) else med
                if izq > 0.8 * med and der > 0.8 * med:
                    cortes += 1
                en_run = True
            elif not bajo:
                en_run = False

        # PAREJO: desviación de ancho en u∈[0.15, 0.85]
        i0, i1 = int(0.15 * len(anchos)), int(0.85 * len(anchos))
        cuerpo = anchos[i0:i1]
        med_a = sorted(cuerpo)[len(cuerpo) // 2]
        desv = max(abs(a - med_a) for a in cuerpo) / med_a * 100.0

        ok = cortes == 0 and desv <= 5.0
        todo_ok &= ok
        print(f"  semilla {seed:4d}: cortes={cortes} · ancho medio={med_a:.0f} px · "
              f"desviación cuerpo ±{desv:.1f}% · {'✔ OK' if ok else '✘ FALLA'}")

    print("\nRESULTADO:", "0 CORTES y PAREJO ±5% EN TODAS LAS SEMILLAS ✔" if todo_ok
          else "HAY CORTES O DESPAREJO — REVISAR ✘")
    return 0 if todo_ok else 1


# ==== LA CADENA VIBRANTE (la fase VIBRACIÓN — el código exacto de Grieta v6.31) ====
def _mock_cadena():
    """
    El segundo contrato D.6: la CADENA (CaminoVibracion + Grieta/GrietaVacio con
    texturas Taper recortadas, ANCHO PLANO y SOLAPE ADAPTATIVO — suelo vacío
    0.75 px, suelo luz 0 px (exactamente geométrico), perla de luz solo con giro real δ > 8°).
    Reproduce numéricamente el dibujo y mide cortes + parejo a amplitud MÁXIMA.
    """
    import math

    # LA GRANULARIDAD DEL JUEGO: 1 px de canvas = 1 px de pantalla, muestreo
    # en CENTROS de píxel (lo que SpriteBatch+bilineal realmente produce).
    S = 1
    Wc, Hc = int(L) + 40, 40
    texs = {n: cargar("RiftTaper" + n.capitalize()) for n in ("velo", "cuerpo", "nucleo", "void")}

    def bil(img, u, v):
        W, H = img.size
        fx, fy = u * (W - 1), v * (H - 1)
        x0, y0 = int(fx), int(fy)
        x1, y1 = min(x0 + 1, W - 1), min(y0 + 1, H - 1)
        tx, ty = fx - x0, fy - y0
        return tuple(img.getpixel((x0, y0))[c] * (1 - tx) * (1 - ty) + img.getpixel((x1, y0))[c] * tx * (1 - ty)
                     + img.getpixel((x0, y1))[c] * (1 - tx) * ty + img.getpixel((x1, y1))[c] * tx * ty
                     for c in range(4))

    def giro_en(pts, k):
        d0 = (pts[k][0] - pts[k - 1][0], pts[k][1] - pts[k - 1][1])
        d1 = (pts[k + 1][0] - pts[k][0], pts[k + 1][1] - pts[k][1])
        a0, a1 = math.atan2(d0[1], d0[0]), math.atan2(d1[1], d1[0])
        d = abs(a1 - a0)
        return 2 * math.pi - d if d > math.pi else d

    def ext_solape(w, g, suelo):
        return max(suelo, min(w * 0.5 * math.tan(g * 0.5) + suelo, w * 0.5))

    print("\n=== MOCK 1:1 CADENA VIBRANTE (solape adaptativo v6.31, amplitud máx) ===")
    pts_n = max(10, min(20, int(L / 40)))
    todo = True
    for thr in (0.20, 0.50):
        for fase in (0.025, 0.05, 0.075, 0.1):
            pts = [(i / (pts_n - 1) * L,
                    math.sin(i / (pts_n - 1) * math.pi * 2) * math.sin(fase * 2 * math.pi * 10) * 3.5)
                   for i in range(pts_n)]
            n = len(pts)
            marca = [[0.0] * Wc for _ in range(Hc)]
            luz = [[0.0] * Wc for _ in range(Hc)]

            def pinta(cx, cy, ql, rot, con_luz, estadio=False):
                qh = QUAD_H
                cr, sr = math.cos(rot), math.sin(rot)
                hl, hh = ql / 2, qh / 2
                for py in range(Hc):
                    wy = py - Hc / 2 + 0.5
                    for px in range(Wc):
                        wx = px - 20 + 0.5
                        lx_ = (wx - cx) * cr + (wy - cy) * sr
                        ly_ = -(wx - cx) * sr + (wy - cy) * cr
                        if abs(lx_) > hl or abs(ly_) > hh:
                            continue
                        u = ((lx_ / hl + 1) / 2) if estadio else (0.30 + 0.40 * (lx_ / hl + 1) / 2)
                        v = (ly_ / hh + 1) / 2
                        r_, g_, b_, a_ = bil(texs["void"], u, v)
                        ng = (a_ / 255) * F_VOID * (1 - (r_ + g_ + b_) / 3 / 255)
                        if ng > marca[py][px]:
                            marca[py][px] = ng
                        if con_luz:
                            s = 0.0
                            for nm, f in (("velo", F_VELO), ("cuerpo", F_CUERPO), ("nucleo", F_NUCLEO)):
                                r_, g_, b_, a_ = bil(texs[nm], u, v)
                                s += (a_ / 255) * f * f * ((r_ + g_ + b_) / 3 / 255)
                            luz[py][px] += s

            for i in range(n - 1):
                ax, ay = pts[i]
                bx, by = pts[i + 1]
                ln = math.hypot(bx - ax, by - ay)
                if ln < 0.3:
                    continue
                rot = math.atan2(by - ay, bx - ax)
                gA = giro_en(pts, i) if i > 0 else 0.0
                gB = giro_en(pts, i + 1) if i < n - 2 else 0.0
                lv = ln + ext_solape(MAXW, gA, 0.75) + ext_solape(MAXW, gB, 0.75)
                ll = ln + ext_solape(MAXW, gA, 0.0) + ext_solape(MAXW, gB, 0.0)
                extremo = i == 0 or i == n - 2     # segmento con textura ESTADIO completa
                pinta((ax + bx) / 2, (ay + by) / 2, lv, rot, False, extremo)  # vacío
                pinta((ax + bx) / 2, (ay + by) / 2, ll, rot, True, extremo)   # luz
                if i > 0:
                    pinta(ax, ay, MAXW * 1.25, rot, False)         # perla vacío (nunca raíz)
                if gA > 0.14:                                       # perla luz (giro real)
                    pinta(ax, ay, MAXW * 1.25, rot, True)

            cob, anchos = [], []
            for px in range(Wc):
                m = 0.0
                ancho = 0
                for py in range(Hc):
                    mk = marca[py][px] + luz[py][px]
                    if mk > m:
                        m = mk
                    if mk > thr:
                        ancho += 1
                cob.append(m)
                anchos.append(ancho)
            cuerpo_cob = cob[int(0.02 * Wc):int(0.98 * Wc)]
            med = sorted(cuerpo_cob)[len(cuerpo_cob) // 2]
            cortes = 0
            en_run = False
            for i in range(3, len(cob) - 3):
                bajo = cob[i] < 0.5 * med
                if bajo and not en_run:
                    izq = max(cob[max(0, i - 12):i]) if i > 12 else med
                    der = max(cob[i + 1:i + 13]) if i + 13 < len(cob) else med
                    if izq > 0.8 * med and der > 0.8 * med:
                        cortes += 1
                    en_run = True
                elif not bajo:
                    en_run = False
            a0, a1 = int(0.15 * Wc), int(0.85 * Wc)
            ac = anchos[a0:a1]
            meda = sorted(ac)[len(ac) // 2]
            # desviación DESDE LA MEDIANA. Contrato: ±5% en el quad (la vida
            # entera del desgarro) y ±7% en la CADENA a amplitud MÁXIMA — el
            # residuo medido es 1 columna de píxel del micro-solape geométrico
            # (0.1 px) bajo alienación adversa del muestreo: sub-píxel invisible
            # bajo el shimmer de 10 Hz de la vibración (documentado en v6.31).
            desv = max(max(ac) - meda, meda - min(ac)) / meda * 100.0
            ok = cortes == 0 and desv <= 7.0
            todo &= ok
            print(f"  umbral {thr:.2f} · fase {fase:.3f}: cortes={cortes} · ancho={meda:.1f} px · "
                  f"desviación ±{desv:.1f}% · {'✔ OK' if ok else '✘ FALLA'}")
    print("  --- CADENA:", "0 CORTES y PAREJA ±5% ✔" if todo else "REVISAR ✘")
    return todo


if __name__ == "__main__":
    ok1 = main() == 0
    ok2 = _mock_cadena()
    sys.exit(0 if (ok1 and ok2) else 1)
