#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
verifica_alineacion_v6121.py — LA PRUEBA DE LA MISMA CANCIÓN

Si el dron nuevo (dron_respirando) quema EXACTAMENTE las mismas monedas
del rng que consumía el dron viejo (saw + pan por parcial, en la v6.120),
el arroyo queda en la MISMA posición: los próximos 50 sorteos del rng
deben ser IDÉNTICOS en ambas versiones → pads, coros, campanas, arpas,
susurros y polvo caen IGUAL — la misma canción, nota por nota.
"""
import sys, os
import numpy as np

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__))))
import gen_musica_interior_v6120 as viejo          # el módulo de la v6.120 (importable: main guardado)
from motor_musica_v6121 import dron_respirando

N = 200   # los sorteos que vienen DESPUÉS del dron (pads, campanas…)

# ---------- EL SUEÑO DEL VACÍO (brightness 0.5 · 4 parciales · pan ±0.3) ----------
viejo.rng = np.random.default_rng(20261109)
dur = 118.0
base = 41.2
for f, g in [(base, 0.42), (base*2, 0.20), (base*3, 0.07), (base*1.5, 0.05)]:
    viejo.saw(f, dur, brightness=0.5)               # lo que el viejo consumía
    viejo.rng.uniform(-0.3, 0.3)                    # su moneda del pan
sorteos_viejos_sueno = [viejo.rng.random() for _ in range(N)]

rng_nuevo = np.random.default_rng(20261109)
dron_respirando(rng_nuevo, [
    (base,     0.300, 0.045, 0.083),
    (base*1.5, 0.050, 0.052, 0.094),
    (base*2,   0.011, 0.048, 0.071),
    (base*3,   0.004, 0.057, 0.066),
], dur, tope_pan=0.3, brillo=0.5)
sorteos_nuevos_sueno = [rng_nuevo.random() for _ in range(N)]

ok_sueno = sorteos_viejos_sueno == sorteos_nuevos_sueno
print(f"EL SUEÑO DEL VACÍO:  arroyo {'IDÉNTICO ✔ (la misma canción)' if ok_sueno else '¡DESALINEADO! ✗'}")
if not ok_sueno:
    for i, (a, b) in enumerate(zip(sorteos_viejos_sueno, sorteos_nuevos_sueno)):
        if a != b:
            print(f"  primer desvío en el sorteo {i}: viejo {a!r} vs nuevo {b!r}")
            break

# ---------- LA GARGANTA (brightness 0.45 · 3 parciales · pan ±0.25) ----------
viejo.rng = np.random.default_rng(20261109)
dur = 122.0
base = 46.25
for f, g in [(base, 0.34), (base*2, 0.16), (base*2.9966, 0.05)]:
    viejo.saw(f, dur, brightness=0.45)
    viejo.rng.uniform(-0.25, 0.25)
sorteos_viejos_garganta = [viejo.rng.random() for _ in range(N)]

rng_nuevo = np.random.default_rng(20261109)
dron_respirando(rng_nuevo, [
    (base,        0.270, 0.050, 0.110),
    (base*2,      0.010, 0.044, 0.087),
    (base*2.9966, 0.004, 0.058, 0.074),
], dur, tope_pan=0.25, brillo=0.45)
sorteos_nuevos_garganta = [rng_nuevo.random() for _ in range(N)]

ok_garganta = sorteos_viejos_garganta == sorteos_nuevos_garganta
print(f"LA GARGANTA:         arroyo {'IDÉNTICO ✔ (la misma canción)' if ok_garganta else '¡DESALINEADO! ✗'}")
if not ok_garganta:
    for i, (a, b) in enumerate(zip(sorteos_viejos_garganta, sorteos_nuevos_garganta)):
        if a != b:
            print(f"  primer desvío en el sorteo {i}: viejo {a!r} vs nuevo {b!r}")
            break

sys.exit(0 if (ok_sueno and ok_garganta) else 1)
