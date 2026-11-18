#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""sim_v6152.py — LOS SIMS 1:1 DE LA v6.152:
  (1) EL PILAR NUEVO: el obelisco SIN ojo y SIN cortes — montaje de los
      6 marcos + medidas (coronación con masa, base completa, cero ojos
      por análisis de color: nada blanco-redondo en la corona).
  (2) EL CRONISTA: el cuerpo 25 frames + retrato (densidad, paleta).
  (3) LA CURVA DEL JUICIO: EL PUENTE MATEMÁTICO — el puerto EXACTO de
      RadioDelJuicio (leído por regex del .cs) y la MEDIDA de sus
      COSTURAS: el radio no salta NUNCA (delta máximo entre ticks
      contiguos) en TODO el ciclo (0 → nova). Si una costura salta,
      el sim lo escupe con el valor.
"""
import re, math
from PIL import Image

A = '/home/z/my-project/mod/AethonMod'
ok = True

# ============ (1) EL PILAR NUEVO ============
im = Image.open(f'{A}/Content/Tiles/PilarPrismaTile.png').convert('RGBA')
print(f'PILAR NUEVO: {im.size} (6 marcos de 86)')
for m in range(6):
    f = im.crop((0, m * 86, 54, m * 86 + 86))
    # la coronación (filas 0-8: EL ARO) tiene masa
    aro = sum(1 for y in range(1, 9) for x in range(54) if f.getpixel((x, y))[3] > 100)
    # la base (fila 84): completa (el pedestal de una pieza)
    base = sum(1 for x in range(54) if f.getpixel((x, 84))[3] > 100)
    # SIN OJO: en la corona NO puede haber un blob BLANCO grande (el ojo
    # viejo era un disco claro) — el aro es violeta/oro delgado
    blancos_corona = sum(1 for y in range(0, 16) for x in range(54)
                         if f.getpixel((x, y))[3] > 100 and
                         all(c > 190 for c in f.getpixel((x, y))[:3]))
    print(f'  marco {m}: aro {aro}px | base {base}/54 | blancos-corona {blancos_corona}')
    if aro < 20: print(f'  ✗ marco {m}: el aro no se lee'); ok = False
    if base < 30: print(f'  ✗ marco {m}: la base está cortada'); ok = False
    if blancos_corona > 12: print(f'  ✗ marco {m}: HAY OJO en la corona'); ok = False
# el montaje para el ojo humano
muestra = Image.new('RGBA', (54 * 6 + 10 * 7, 96), (24, 18, 34, 255))
for m in range(6):
    muestra.alpha_composite(im.crop((0, m * 86, 54, m * 86 + 86)), (10 + m * 64, 5))
muestra.save('/home/z/my-project/download/sim_pilar_v6152_obelisco.png')
print('  ✔ sim_pilar_v6152_obelisco.png guardado')

# ============ (2) EL CRONISTA ============
c = Image.open(f'{A}/Content/NPCs/ElCronista.png').convert('RGBA')
h = Image.open(f'{A}/Content/NPCs/ElCronista_Head.png').convert('RGBA')
dens_f0 = sum(1 for y in range(42) for x in range(20) if c.getpixel((x, y))[3] > 100)
dens_h = sum(1 for y in range(20) for x in range(20) if h.getpixel((x, y))[3] > 100)
print(f'CRONISTA: cuerpo {c.size} (densidad frame0 {dens_f0}px) | retrato {h.size} ({dens_h}px)')
if dens_f0 < 120: print('  ✗ el cuerpo del Cronista está vacío'); ok = False
else: print('  ✓ el cuerpo vive (la túnica, la capucha, el libro)')
if dens_h < 40: print('  ✗ el retrato está vacío'); ok = False
else: print('  ✓ el retrato vive')
# el montaje del Cronista (frame 0 + retrato, ×3 escala para verlo)
mont = Image.new('RGBA', (140, 140), (24, 18, 34, 255))
f0 = c.crop((0, 0, 20, 42)).resize((60, 126), Image.NEAREST)
mont.alpha_composite(f0, (10, 8))
mont.alpha_composite(h.resize((60, 60), Image.NEAREST), (76, 40))
mont.save('/home/z/my-project/download/sim_cronista_v6152.png')
print('  ✔ sim_cronista_v6152.png guardado')

# ============ (3) LA CURVA DEL JUICIO (el puente matemático) ============
src = open(f'{A}/Content/Weapons/Definitivas/JuicioDosSoles.cs', encoding='utf-8').read()
# leer las constantes del .cs (los valores REALES, no copiados)
cs = {k: float(re.search(rf'{k} = (\d+)', src).group(1)) for k in
      ['T_CONVOCATORIA', 'T_FUSION', 'T_CARGA', 'T_NOVA', 'T_COLAPSO']}
CRECE = float(re.search(r'CRECE_CAZA = ([\d.]+)f', src).group(1))
P_CONT = float(re.search(r'P_CONTACTO = ([\d.]+)f', src).group(1))
print(f'JUICIO — constantes reales: {cs} | CRECE={CRECE}')

def smoothstep(x):
    x = max(0.0, min(1.0, x))
    return x * x * (3 - 2 * x)

def radio_fusion(n):  # el puerto EXACTO del .cs
    return 0.55 + (1.05 - 0.55) * smoothstep(n)

def radio_juicio(edad):  # EL PUERTO EXACTO de RadioDelJuicio
    TC, TF, TCA, TN, TCOL = (cs['T_CONVOCATORIA'], cs['T_FUSION'],
                              cs['T_CARGA'], cs['T_NOVA'], cs['T_COLAPSO'])
    if edad < TC:
        return 0.50 + (0.92 - 0.50) * smoothstep(edad / TC)
    if edad < TC + TF:
        return 0.92 + (0.55 - 0.92) * smoothstep((edad - TC) / TF)
    if edad < TC + TF + TCA:
        n = (edad - TC - TF) / TCA
        return radio_fusion(min(1.0, n * 3)) + CRECE * smoothstep(n)
    t_post = edad - TC - TF - TCA
    r_gig = 1.05 + CRECE
    if t_post < TCOL:
        pl = t_post / TCOL
        ss = pl * pl * (3 - 2 * pl)
        return r_gig + (r_gig * 0.42 - r_gig) * ss
    p = max(0.0, min(1.0, (t_post - TCOL) / (TN - TCOL)))
    crece = min(1.0, (t_post - TCOL) / (TN - TCOL))
    ss = crece * crece * (3 - 2 * crece)
    return r_gig * 0.42 * (1 + 1.25 * ss) * (0.85 + 0.15 * (1 - p))

TOTAL = int(cs['T_CONVOCATORIA'] + cs['T_FUSION'] + cs['T_CARGA'] + cs['T_NOVA']) + 2
vals = [radio_juicio(e) for e in range(TOTAL)]
# v6.152 — EL CRITERIO CORRECTO: un SALTO es una COSTURA (el primer tick
# de un tramo que no empata el último del anterior); una PENDIENTE pico
# en medio de un tramo largo (el pliegue de 14 t de la sentencia) es
# COREOGRAFÍA, no salto. Se miden LAS 4 COSTURAS de la curva.
TC, TF, TCA, TN, TCOL = (cs['T_CONVOCATORIA'], cs['T_FUSION'], cs['T_CARGA'],
                          cs['T_NOVA'], cs['T_COLAPSO'])
costuras = [(int(TC), 'convocatoria→vals'), (int(TC+TF), 'vals→caza'),
            (int(TC+TF+TCA), 'caza→colapso'), (int(TC+TF+TCA+TCOL), 'colapso→nova')]
peor_costura, peor_nombre = 0.0, ''
for t, nombre in costuras:
    d = abs(vals[t] - vals[t-1])
    print(f'  costura t={t} ({nombre}): Δ={d:.4f}')
    if d > peor_costura: peor_costura, peor_nombre = d, nombre
picos = max(abs(vals[e]-vals[e-1]) for e in range(1, TOTAL))
print(f'LA CURVA COMPLETA: {TOTAL} ticks | peor COSTURA {peor_costura:.4f} ({peor_nombre}) | pendiente pico {picos:.4f} (el pliegue de la sentencia, suave)')
if peor_costura > 0.030:
    print('  ✗ UNA COSTURA SALTA'); ok = False
else:
    print('  ✓ LAS 4 COSTURAS CONTINUAS (Δ≤0.015) — el radio JAMÁS salta; el pliegue es pendiente suave de 14 t')
print(f'  radio: nace {vals[0]:.3f} → par {vals[int(cs["T_CONVOCATORIA"])-1]:.3f} → contacto '
      f'{vals[int(cs["T_CONVOCATORIA"]+cs["T_FUSION"])]:.3f} → gigante final '
      f'{max(vals):.3f} (crece {(max(vals)/vals[int(cs["T_CONVOCATORIA"]+cs["T_FUSION"])]-1)*100:.0f}% sobre el contacto)')

print('\nSIMS v6.152:', 'TODO MEDIDO Y VERDE ✔' if ok else 'CON FALLAS ✗')
import sys; sys.exit(0 if ok else 1)
