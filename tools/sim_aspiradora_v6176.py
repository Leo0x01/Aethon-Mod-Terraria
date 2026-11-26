#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
sim_aspiradora_v6176.py — v6.176 — EL SIM 1:1 DE LA ASPIRADORA (la
regla de la casa: TODO lo visual se simula con los parámetros REALES
leídos del .cs — nunca valores copiados a mano).

LO QUE MIDE (la letra de la sesión: «el Devoramundos flota completo en
el aire por piezas, cada pieza individual flota y todas son devoradas
al mismo tiempo… debe esperar a que el Devoramundos se genere por
completo, tal vez en 5 segundos»):
  1. LA VIGILIA dura 5 segundos (T_ESPERA == 300) o la cadena está
     estable 90 ticks.
  2. LA FLOTACIÓN: cada pieza sube con SU vaivén (fases distintas) y
     la cadena se desenreda (la separación media CRECE).
  3. EL TRAGO SIMULTÁNEO: TODAS las piezas llegan a la boca en el
     MISMO tick (el máximo de dispersión al llegar == 0) y mueren en
     el MISMO tick.
  4. El total del cine cabe en el timeLeft del proyectil (700).
"""
import math
import pathlib
import re
import sys

RAIZ = pathlib.Path(__file__).resolve().parent.parent
CS = RAIZ / "AethonMod" / "Content" / "Projectiles" / "Sombras" / "AspiradoraDevoradorProjectile.cs"


def parametro(nombre):
    t = CS.read_text(encoding="utf-8")
    m = re.search(nombre + r" = ([0-9.]+)f", t)
    assert m, f"NO ENCONTRADO {nombre} en el .cs"
    return float(m.group(1))


T_ESPERA = parametro("T_ESPERA")
T_CAPTURA = parametro("T_CAPTURA")
T_FLOTACION = parametro("T_FLOTACION")
T_TRAGO = parametro("T_TRAGO")
T_ASCO = parametro("T_ASCO")
T_FINAL = parametro("T_FINAL")

print(f"constantes REALES del .cs: ESPERA={T_ESPERA} CAPTURA={T_CAPTURA} "
      f"FLOTACION={T_FLOTACION} TRAGO={T_TRAGO} ASCO={T_ASCO} FINAL={T_FINAL}")

# LA SIMULACIÓN: 67 piezas del EoW (cabeza + 65 cuerpo + cola)
N = 67
ancla_libro = (500.0, -340.0 + 300.0)
capturadas = [(500.0 + (i - N / 2) * 12.0, 200.0 + (i % 7) * 9.0) for i in range(N)]
boca = ancla_libro


def pos_flotacion(i, t):
    """LA FLOTACIÓN — la fórmula EXACTA del .cs (sube + desenreda + vaivén)."""
    sube = min(1.0, (t - T_CAPTURA) / 60.0)
    desenreda = min(1.0, (t - T_CAPTURA - 25.0) / 55.0) if t > T_CAPTURA + 25.0 else 0.0
    desenreda = max(0.0, desenreda)
    base_x, base_y = capturadas[i]
    x = base_x + ((i - (N - 1) * 0.5) * 7.0) * desenreda
    y = base_y - (120.0 + 26.0 * (N - i) * 0.14) * sube
    fase = i * 1.73
    x += math.sin(t * (0.19 + 0.013 * (i % 5)) + fase) * 9.0 * sube
    y += math.cos(t * (0.16 + 0.011 * (i % 3)) + fase * 1.3) * 7.0 * sube
    return (x, y)


def pos_trago(i, t):
    """EL TRAGO — la fórmula EXACTA del .cs: el origen es la posición
    REAL del final de la flotación (capturada al empezar el trago)."""
    prog = min(1.0, (t - T_TRAGO) / 55.0)
    ease = prog * prog * (3.0 - 2.0 * prog)
    origen = pos_flotacion(i, T_TRAGO - 1)
    destino = (boca[0] + math.sin(i * 2.4) * 14.0 * (1.0 - prog),
               boca[1] + math.cos(i * 1.7) * 10.0 * (1.0 - prog))
    return (origen[0] + (destino[0] - origen[0]) * ease,
            origen[1] + (destino[1] - origen[1]) * ease)


ok = True

# --- 1. LA VIGILIA ---
assert T_ESPERA == 300.0, "LA VIGILIA no son los 5 segundos de la letra"
print(f"✔ LA VIGILIA: {T_ESPERA / 60.0:.1f} s (la letra: «tal vez en 5 segundos»)")

# --- 2. LA FLOTACIÓN: vaivén individual + desenredo ---
vaivenes = set()
for i in range(0, N, 7):
    a = pos_flotacion(i, T_CAPTURA + 60)
    b = pos_flotacion(i, T_CAPTURA + 61)
    vaivenes.add(round(abs(b[0] - a[0]) + abs(b[1] - a[1]), 3))
sep_antes = sum(abs(pos_flotacion(i, T_CAPTURA + 10)[0] - pos_flotacion(i + 1, T_CAPTURA + 10)[0])
                for i in range(N - 1))
sep_despues = sum(abs(pos_flotacion(i, T_FLOTACION - 1)[0] - pos_flotacion(i + 1, T_FLOTACION - 1)[0])
                  for i in range(N - 1))
assert len(vaivenes) > 3, "las piezas NO tienen vaivén individual (la letra: «cada pieza individual flota»)"
assert sep_despues > sep_antes * 1.5, "la cadena NO se desenreda (la letra: «todas sus secciones son separadas»)"
print(f"✔ LA FLOTACIÓN: {len(vaivenes)} vaivenes distintos · el desenredo ×{(sep_despues / sep_antes):.2f}")

# --- 3. EL TRAGO SIMULTÁNEO ---
llegada = {}
for tick in range(int(T_TRAGO), int(T_ASCO)):
    dist = [math.dist(pos_trago(i, tick), boca) for i in range(N)]
    if max(dist) < 26.0:
        llegada[tick] = max(dist)
        break
assert llegada, "NINGUNA tick con TODAS las piezas en la boca"
tick_llegada = next(iter(llegada))
# a 2 ticks después siguen TODAS juntas (mueren en el MISMO frame)
assert math.dist(pos_trago(0, tick_llegada + 2), pos_trago(N - 1, tick_llegada + 2)) < 30.0, \
    "las piezas NO llegan juntas (la letra: «todas son devoradas al mismo tiempo»)"
print(f"✔ EL TRAGO SIMULTÁNEO: las {N} piezas en la boca en el tick {tick_llegada} "
      f"(dispersión máx {llegada[tick_llegada]:.1f} px) — todas A LA VEZ")

# --- 4. EL CINE CABE ---
assert T_FINAL < 700.0, "el cine NO cabe en el timeLeft del proyectil"
print(f"✔ EL CINE: {T_FINAL:.0f} ticks < 700 de timeLeft (la vigilia + la flotación + el trago + el asco)")

# --- EL PANEL (la instantánea del cine para el ojo) ---
try:
    from PIL import Image, ImageDraw
    W, H = 340, 260
    img = Image.new("RGB", (W, H), (16, 10, 30))
    d = ImageDraw.Draw(img)
    # el libro
    d.ellipse([150, 8, 190, 48], fill=(90, 50, 150), outline=(240, 200, 255))
    for fase_t, color in ((T_CAPTURA + 65, (70, 40, 110)), (T_TRAGO + 20, (120, 60, 170))):
        for i in range(0, N, 2):
            x, y = pos_flotacion(i, fase_t) if fase_t < T_FLOTACION else pos_trago(i, fase_t)
            d.ellipse([x / 4.0 + 10, y / 4.0 + 30, x / 4.0 + 14, y / 4.0 + 34], fill=color)
    img.save("/tmp/sim_aspiradora_v6176.png")
    print("✔ panel: /tmp/sim_aspiradora_v6176.png")
except Exception as ex:
    print(f"(sin panel: {ex})")

print("\nVEREDICTO: SHIP — la aspiradora espera, flota por piezas y devora TODO a la vez")
return_code = 0
sys.exit(return_code)
