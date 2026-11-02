#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.88 — LA OLEADA FINAL: la SEGUNDA banda sonora de las oleadas de hambre
(la letra: «la banda sonora de la que hiciste y crea otra aun mas epica,
esta nueva banda sonora mas la que ya esta creada, las dos deben sonar
aleatoriamente cuando se activa una oleada»).

La v6.87 (152 BPM, «El Festín Desatado») era EL ASALTO. Esta es LA
BATALLA CAMPAL: 164 BPM (más animada), la orquesta COMPLETA — doble
clímax con modulación, el coro a OCHO voces, los timbales de guerra, la
campana tubular del grimorio y la tercera de picardía final (el sol
que rompe la noche del hambre). Misma síntesis de la casa (saw aditiva
sin aliasing, ruido filtrado, formantes) — el pipeline de la v687.

Estructura (4/4, 164 BPM, compás = 60/164*4 ≈ 1.463 s; re menor → Mi bemol mayor final):
  S1  0-6    INTRO      — el dron + el redoble de timbal + el coro lejano
  S2  6-22   TEMA A     — el ostinato en 16avos + el kick a 4x4
  S3  22-38  TEMA B     — el CUERNO ÉPICO + cuerdas staccato
  S4  38-44  PUENTE     — medio tiempo: la calma que miente
  S5  44-58  CLÍMAX 1   — TODO + doble kick + toms
  S6  58-70  CLÍMAX 2   — MODULACIÓN + el CORO a 8 + la campana
  S7  70-78  COLA       — decae al dron (empalma con la intro — el loop)

Salida: AethonMod/Music/OleadaHambreEpica.ogg (44.1 kHz estéreo, vorbis).
"""
import numpy as np
import subprocess, os, math

SR = 44100
BPM = 164.0
BEAT = 60.0 / BPM
BAR = BEAT * 4
N_BARS = 78
DUR = N_BARS * BAR + 2.0
N = int(DUR * SR)
print(f"Duración total: {DUR:.1f} s ({N} muestras)")

rng = np.random.default_rng(20261102)
mix = np.zeros((N, 2), dtype=np.float64)

# ----------------------------------------------------------------------
# SÍNTESIS (el pipeline de la casa: saw aditiva sin aliasing + helpers)
# ----------------------------------------------------------------------
def saw(freq, dur, brightness=1.0):
    t = np.arange(int(dur * SR)) / SR
    out = np.zeros_like(t)
    k = 1
    while freq * k < SR * 0.48 and k <= int(96 * brightness):
        out += np.sin(2 * np.pi * freq * k * t + rng.uniform(0, 2*np.pi)) / k
        k += 1
    return out * (2 / np.pi)

def sine(freq, dur):
    t = np.arange(int(dur * SR)) / SR
    return np.sin(2 * np.pi * freq * t)

# EL CORO (formantes): la suma de voces detuneadas pasa por un "vocal
# tract" — 3 resonancias (formantes F1/F2/F3 de la [a] épica).
def coro(freq, dur, voces=8, amplitud=1.0):
    t = np.arange(int(dur * SR)) / SR
    out = np.zeros_like(t)
    for v in range(voces):
        det = 1.0 + (v - voces / 2) * 0.0028          # ±1,4 %: el coro real
        vib = 1.0 + 0.004 * np.sin(2 * np.pi * 4.7 * t + v * 1.3)
        f = freq * det * vib
        out += np.sin(2 * np.pi * f * t + rng.uniform(0, 2*np.pi))
    out /= voces
    # LOS FORMANTES (la [a] abierta del coro de guerra) — vectorizados
    #     con scipy.signal.lfilter (el bucle por muestra mataba el script).
    from scipy.signal import lfilter
    for fo, q, g in [(730, 6.0, 1.0), (1090, 9.0, 0.55), (2440, 12.0, 0.22)]:
        w = 2 * np.pi * fo / SR
        r = 1 - math.pi / (q * 2)
        a1, a2 = 2 * r * math.cos(w), -r * r
        y = lfilter([1.0], [1.0, -a1, -a2], out)
        out = out * 0.35 + y * g
    # v6.88.1 — EL CLAMP DEL CORO: los resonantes de los formantes
    #     AMPLIFICAN hasta ~7× (el coro «lejano» sonaba a RIFF a todo
    #     volumen — RMS 0.87 en su arranque: el «clic» del QC). El coro
    #     entra NORMALIZADO a pico 0.9 — su dinámica la pone «amplitud».
    m = np.abs(out).max()
    if m > 1e-6: out = out * (0.9 / m)
    return out * amplitud * 0.8

def kick(dur=0.30):
    n = int(dur * SR); t = np.arange(n) / SR
    f = 148 * np.exp(-t * 26) + 44
    ph = 2 * np.pi * np.cumsum(f) / SR
    env = np.exp(-t * 13.0)
    click = rng.normal(0, 1, n) * np.exp(-t * 260) * 0.35
    return (np.sin(ph) * env + click) * 0.95

def snare(dur=0.22):
    n = int(dur * SR); t = np.arange(n) / SR
    env = np.exp(-t * 21)
    tono = np.sin(2 * np.pi * 187 * t) * np.exp(-t * 38) * 0.28
    return (rng.normal(0, 1, n) * env * 0.62 + tono)

def hat(dur=0.06, abierto=False):
    n = int(dur * SR); t = np.arange(n) / SR
    env = np.exp(-t * (9 if abierto else 85))
    x = rng.normal(0, 1, n)
    # filtro paso-alto barato: primera diferencia
    x = np.diff(x, prepend=0.0)
    return x * env * 0.30

def timbal(freq=94.0, dur=0.9, amplitud=1.0):
    n = int(dur * SR); t = np.arange(n) / SR
    f = freq * (1 + 0.28 * np.exp(-t * 9))
    ph = 2 * np.pi * np.cumsum(f) / SR
    env = np.exp(-t * 4.2)
    return (np.sin(ph) + 0.34 * np.sin(ph * 2.71)) * env * 0.8 * amplitud

def campana(freq, dur=3.2, amplitud=1.0):
    """La campana tubular inarmónica de la casa (parciales no enteros)."""
    n = int(dur * SR); t = np.arange(n) / SR
    out = np.zeros(n)
    for k, a in [(1.0, 1.0), (2.76, 0.42), (5.40, 0.20), (8.93, 0.09)]:
        fk = freq * k
        if fk < SR * 0.45:
            out += a * np.sin(2 * np.pi * fk * t + rng.uniform(0, 2*np.pi)) * np.exp(-t * (2.1 + k * 1.4))
    return out * 0.5 * amplitud

def envol(n, a, d, s, r, sn=0.0):
    """Envolvente ADSR normalizada."""
    e = np.zeros(n)
    na, nd = int(a * SR), int(d * SR)
    nr = int(r * SR)
    ns = max(0, n - na - nd - nr)
    i = 0
    if na: e[:na] = np.linspace(0, 1, na); i = na
    if nd and i < n: e[i:i+min(nd, n-i)] = np.linspace(1, s, min(nd, n-i)); i += min(nd, n-i)
    if i < n: e[i:i+ns] = s; i += ns
    if i < n: e[i:] = np.linspace(s, sn, n - i)
    return e

def colocar(dest, src, t0, pan=0.0, gan=1.0):
    i0 = int(t0 * SR)
    if i0 >= len(dest): return
    n = min(len(src), len(dest) - i0)
    if n <= 0: return
    l = gan * (1.0 - max(0.0, pan)) * 0.5 + gan * 0.5
    rr = gan * (1.0 - max(0.0, -pan)) * 0.5 + gan * 0.5
    dest[i0:i0+n, 0] += src[:n] * l
    dest[i0:i0+n, 1] += src[:n] * rr

def nota(m):  # MIDI → Hz
    return 440.0 * (2.0 ** ((m - 69) / 12.0))

# ----------------------------------------------------------------------
# LA ARMONÍA (re menor; el clímax 2 modula a Mi bemol — la picardía)
# ----------------------------------------------------------------------
Dm, Bb, F, C = (nota(m) for m in (50, 58, 53, 60))          # D3 Bb2 F3 C3
Gm, Eb = nota(55), nota(63)                                   # G3, Eb3
acordes_A = [(Dm, Bb, F), (Bb, F, C), (F, C, Dm + 24*0), (C, Dm, Bb)]
acordes_B = [(Gm, Eb, Bb), (Eb, Bb, F * 2**0), (Bb, F, C), (C, Gm, Dm)]

def compas_acorde(prog, bar_idx):
    return prog[bar_idx % len(prog)]

# ----------------------------------------------------------------------
# S1 — INTRO (0-6): el dron + el redoble + el coro lejano
# ----------------------------------------------------------------------
for b in range(6):
    t0 = b * BAR
    colocar(mix, sine(nota(38), BAR) * 0.055, t0, 0, 0.7)               # el dron D2
    colocar(mix, saw(nota(38), BAR, 0.5) * 0.020, t0, 0, 0.7)
    if b >= 2:
        # EL REDOBLE DE TIMBAL (crescendo hacia el asalto)
        n_roll = 2 + b
        for k in range(n_roll):
            colocar(mix, timbal(96 - 2*b, 0.5, 0.16 + 0.05 * b), t0 + k * (BAR / 2 / n_roll), (k % 2) * 0.4 - 0.2, 1.0)
    if b == 5:
        colocar(mix, campana(nota(74), 3.4, 0.5), t0, -0.15, 0.9)      # la campana de aviso
# el coro lejano (la promesa)
colocar(mix, coro(nota(62), BAR * 4, voces=4, amplitud=0.13), 2 * BAR, 0, 0.9)

# ----------------------------------------------------------------------
# S2 — TEMA A (6-22): el ostinato en 16avos + el kick a 4x4
# ----------------------------------------------------------------------
def colocar_np(dest, src, t0):
    i0 = int(t0 * SR)
    n = min(len(src), len(dest) - i0)
    if n > 0: dest[i0:i0+n] += src[:n]

def bajo_ostinato(f_root, gan=1.0, brillo=0.85):
    """El bajo ostinato en corcheas (el motor del hambre)."""
    n = int(BAR * SR)
    out = np.zeros(n)
    # corcheas: raíz-quinta-raíz-octava · raíz-quinta-séptima-sexta
    seq = [0, 7, 0, 12, 0, 7, 10, 7]
    for k, iv in enumerate(seq):
        t0 = k * (BEAT / 2)
        f = f_root * (2 ** (iv / 12.0))
        s = saw(f, BEAT / 2 * 0.92, brillo) * envol(int(BEAT/2*0.92*SR), 0.004, 0.03, 0.55, 0.02)
        colocar_np(out, s, t0)
    return out * gan

for b in range(16):                        # 16 compases (6-22)
    t0 = (6 + b) * BAR
    root, _, _ = compas_acorde(acordes_A, b)
    colocar(mix, bajo_ostinato(root, 0.27, 0.9), t0, -0.05, 1.0)
    # LA PERCUSIÓN (el 4x4 de la v6.87, más denso)
    for beat in range(4):
        colocar(mix, kick(), t0 + beat * BEAT, 0, 0.78)
        if beat % 2 == 1:
            colocar(mix, snare(), t0 + beat * BEAT, 0.08, 0.62)
    for hatk in range(8):
        colocar(mix, hat(abierto=(hatk == 7)), t0 + hatk * BEAT / 2, -0.12, 0.9)
    if b % 4 == 3:                          # el fill de toms
        for tt, fv in [(0, 110), (1/8, 96), (2/8, 84)]:
            colocar(mix, timbal(fv, 0.5, 0.30), t0 + BAR - BEAT + tt * BEAT, 0.2, 1.0)
    # LAS CUERDAS staccato (choo-choo épico) desde el compás 10
    if b >= 4:
        root3 = compas_acorde(acordes_A, b)[0] * 2
        for st in range(8):
            s = saw(root3, BEAT / 2 * 0.6, 0.65) * envol(int(BEAT/2*0.6*SR), 0.003, 0.04, 0.4, 0.03)
            colocar(mix, s * 0.13, t0 + st * BEAT / 2, 0.1 * (-1 if st % 2 else 1), 1.0)
    if b == 15:
        colocar(mix, campana(nota(74), 2.8, 0.48), t0, -0.1, 0.8)

# ----------------------------------------------------------------------
# S3 — TEMA B (22-38): EL CUERNO ÉPICO + el coro
# ----------------------------------------------------------------------
melodia = [62, 65, 69, 67, 65, 62, 60, 62,   # D F A G F D C D
           65, 69, 72, 74, 72, 69, 67, 65]    # F A C D C A G F
for b in range(16):                        # (22-38)
    t0 = (22 + b) * BAR
    root, _, _ = compas_acorde(acordes_B, b)
    colocar(mix, bajo_ostinato(root, 0.24, 0.95), t0, -0.05, 1.0)
    for beat in range(4):
        colocar(mix, kick(), t0 + beat * BEAT, 0, 0.74)
        if beat % 2 == 1: colocar(mix, snare(), t0 + beat * BEAT, 0.06, 0.66)
    for hatk in range(8):
        colocar(mix, hat(abierto=(hatk % 4 == 3)), t0 + hatk * BEAT / 2, -0.1, 0.8)
    # EL CUERNO (la melodía: una nota por compás y medio + sus floreos)
    mb = melodia[b % len(melodia)]
    fm = nota(mb)
    cuerno = saw(fm, BAR * 0.96, 1.0) * envol(int(BAR*0.96*SR), 0.02, 0.12, 0.72, 0.08)
    # el cuerno = saw + la octava (el metal de la v6.87, más ancho)
    cuerno += saw(fm * 2, BAR * 0.96, 1.0) * envol(int(BAR*0.96*SR), 0.03, 0.16, 0.5, 0.1) * 0.35
    colocar(mix, cuerno * 0.20, t0, 0.24, 1.0)
    if b % 2 == 1:                          # el eco del cuerno
        colocar(mix, cuerno * 0.06, t0 + BEAT, -0.3, 1.0)
    # EL CORO (a 4 en el B; a 8 en el clímax 2)
    if b >= 8:
        colocar(mix, coro(nota(mb - 12), BAR * 0.98, voces=4, amplitud=0.14), t0, -0.2, 1.0)
    if b == 15:
        for tt, fv in [(0, 110), (1/8, 96), (2/8, 84), (3/8, 74)]:
            colocar(mix, timbal(fv, 0.6, 0.32), t0 + BAR - BEAT + tt * BEAT, 0.15, 1.0)

# ----------------------------------------------------------------------
# S4 — PUENTE (38-44): medio tiempo — la calma que miente
# ----------------------------------------------------------------------
for b in range(6):
    t0 = (38 + b) * BAR
    root = compas_acorde(acordes_B, b + 16)[0]
    colocar(mix, sine(nota(38), BAR) * 0.05, t0, 0, 0.8)                 # el dron vuelve
    if b % 2 == 0:
        colocar(mix, kick(), t0, 0, 0.75)
        colocar(mix, kick(), t0 + BEAT * 2.5, 0, 0.55)
    if b % 2 == 1:
        colocar(mix, snare(), t0 + BEAT * 2, 0, 0.55)
        colocar(mix, snare(), t0 + BEAT * 3 + BEAT/2, 0, 0.4)
    # LOS ARPEGIOS (la bruma de la v6.85, la firma que sobrevive)
    for st in range(8):
        f = root * (2 ** ([0, 7, 12, 16, 19, 24, 19, 12][st] / 12.0))
        s = sine(f, BEAT / 2 * 0.9) * envol(int(BEAT/2*0.9*SR), 0.006, 0.05, 0.5, 0.04)
        colocar(mix, s * 0.055, t0 + st * BEAT / 2, 0.3 * (-1 if st % 2 else 1), 1.0)
    if b >= 4:                              # el redoble que crece
        for k in range(2 + (b - 4) * 3):
            colocar(mix, snare() * (0.16 + 0.07 * (b - 4)), t0 + BAR - BEAT + k * (BEAT / (2 + (b-4)*3)), 0, 1.0)
colocar(mix, campana(nota(62), 3.6, 0.55), 43 * BAR + BEAT * 2, 0.1, 1.0)

# ----------------------------------------------------------------------
# S5 — CLÍMAX 1 (44-58): TODO + doble kick
# ----------------------------------------------------------------------
for b in range(14):
    t0 = (44 + b) * BAR
    root = compas_acorde(acordes_B, b)[0]
    colocar(mix, bajo_ostinato(root, 0.30, 1.0), t0, -0.05, 1.0)
    for beat in range(8):                   # DOBLE KICK (16avos de bombo)
        if beat % 2 == 0:
            colocar(mix, kick(), t0 + beat * BEAT / 2, 0, 0.80)
        elif beat == 7:
            colocar(mix, kick(0.26), t0 + beat * BEAT / 2, 0, 0.7)
    if b % 2 == 1: colocar(mix, snare(), t0 + BEAT, 0.05, 0.68); colocar(mix, snare(), t0 + BEAT * 3, 0.05, 0.68)
    for hatk in range(8): colocar(mix, hat(), t0 + hatk * BEAT / 2 + BEAT / 4, -0.12, 0.85)
    # EL CUERNO DOBLADO A DOS (la melodía + la tercera)
    mb = melodia[(b + 4) % len(melodia)]
    for iv, gan in [(0, 0.22), (4, 0.10)]:
        fm = nota(mb + iv + (12 if b % 4 == 3 else 0))
        cuerno = saw(fm, BAR * 0.96, 1.05) * envol(int(BAR*0.96*SR), 0.015, 0.1, 0.75, 0.07)
        colocar(mix, cuerno * gan, t0, 0.2, 1.0)
    # LAS CUERDAS staccato
    for st in range(8):
        s = saw(root * 4, BEAT / 2 * 0.6, 0.7) * envol(int(BEAT/2*0.6*SR), 0.003, 0.04, 0.45, 0.03)
        colocar(mix, s * 0.11, t0 + st * BEAT / 2, 0.1 * (-1 if st % 2 else 1), 1.0)
    if b % 4 == 3:
        for tt, fv in [(0, 116), (1/8, 100), (2/8, 84), (3/8, 72)]:
            colocar(mix, timbal(fv, 0.7, 0.34), t0 + BAR - BEAT + tt * BEAT, 0.2, 1.0)
    if b == 13:
        colocar(mix, campana(nota(86), 3.8, 0.6), t0, -0.05, 1.0)

# ----------------------------------------------------------------------
# S6 — CLÍMAX 2 (58-70): MODULACIÓN + EL CORO A 8 + LA CAMPANA
#    (re menor subida un semitono: la tercera de picardía que rompe)
# ----------------------------------------------------------------------
SUBIR = 2 ** (1.0 / 12.0)      # +1 semitono: re menor → Mi bemol (la picardía)
for b in range(12):
    t0 = (58 + b) * BAR
    root = compas_acorde(acordes_B, b)[0] * SUBIR
    # LA MODULACIÓN ENTRA EN EL 2.º COMPÁS (el truco del silencio)
    if b == 0:
        colocar(mix, sine(nota(38), BAR) * 0.05, t0, 0, 0.8)
        colocar(mix, timbal(62, 1.2, 0.42), t0, 0, 1.0); colocar(mix, timbal(62, 1.2, 0.42), t0 + BEAT*2, 0.1, 1.0)
        continue
    colocar(mix, bajo_ostinato(root, 0.31, 1.05), t0, -0.06, 1.0)
    for beat in range(8):
        if beat % 2 == 0: colocar(mix, kick(), t0 + beat * BEAT / 2, 0, 0.82)
        elif beat == 7: colocar(mix, kick(0.24), t0 + beat * BEAT / 2, 0, 0.72)
    if b % 2 == 1: colocar(mix, snare(), t0 + BEAT, 0.04, 0.72); colocar(mix, snare(), t0 + BEAT * 3, 0.04, 0.72)
    for hatk in range(8): colocar(mix, hat(abierto=(hatk == 7)), t0 + hatk * BEAT / 2, -0.14, 0.9)
    # EL CORO A OCHO VOCES (el clímax del clímax)
    acorde_coro = [0, 7, 12, 16][b % 4]
    colocar(mix, coro(root * 2 * (2 ** (acorde_coro / 12.0)), BAR * 0.98, voces=8, amplitud=0.20), t0, -0.1, 1.0)
    # EL CUERNO FINAL
    mb = melodia[(b + 8) % len(melodia)]
    fm = nota(mb) * SUBIR
    cuerno = saw(fm, BAR * 0.96, 1.1) * envol(int(BAR*0.96*SR), 0.012, 0.08, 0.8, 0.06)
    colocar(mix, cuerno * 0.24, t0, 0.25, 1.0)
    for st in range(8):
        s = saw(root * 4, BEAT / 2 * 0.6, 0.75) * envol(int(BEAT/2*0.6*SR), 0.003, 0.04, 0.5, 0.03)
        colocar(mix, s * 0.12, t0 + st * BEAT / 2, 0.1 * (-1 if st % 2 else 1), 1.0)
    # LAS CAMPANAS (una por compás — el DOBLE del clímax)
    colocar(mix, campana(nota(86) * SUBIR, 2.4, 0.40), t0 + BEAT * 2, 0.12, 0.9)
    if b % 4 == 3:
        for tt, fv in [(0, 122), (1/8, 104), (2/8, 88), (3/8, 74)]:
            colocar(mix, timbal(fv, 0.8, 0.36), t0 + BAR - BEAT + tt * BEAT, 0.2, 1.0)
# EL ACORDE FINAL (b=70): la tercera de picardía A TODO el coro
t_fin = 70 * BAR
colocar(mix, kick(), t_fin, 0, 1.0); colocar(mix, kick(), t_fin + BEAT * 0.5, 0, 0.8)
colocar(mix, campana(nota(86) * SUBIR, 4.0, 0.65), t_fin, 0, 1.0)
for iv in [0, 7, 12, 16, 19]:
    colocar(mix, coro(nota(74 + 0) * SUBIR * (2 ** (iv / 12.0)), BAR * 1.4, voces=8, amplitud=0.12), t_fin, -0.1, 1.0)
colocar(mix, saw(nota(50) * SUBIR, BAR * 1.2, 1.0) * envol(int(BAR*1.2*SR), 0.01, 0.2, 0.7, 0.3) * 0.22, t_fin, 0, 1.0)

# ----------------------------------------------------------------------
# S7 — COLA (70-78): decae al dron (empalma con la intro — el loop)
# ----------------------------------------------------------------------
for b in range(8):
    t0 = (70 + b) * BAR
    g = 1.0 - b / 8.0
    colocar(mix, sine(nota(38), BAR) * 0.055 * g, t0, 0, 0.7)
    colocar(mix, saw(nota(38), BAR, 0.5) * 0.02 * g, t0, 0, 0.7)
    if b == 2:
        colocar(mix, timbal(96, 1.4, 0.20 * g), t0, 0, 1.0)
    if b == 6:
        colocar(mix, coro(nota(62), BAR * 2, voces=4, amplitud=0.10 * g), t0, 0, 0.9)

# ----------------------------------------------------------------------
# MEZCLA + MASTERIZACIÓN (el pipeline de la v687)
# ----------------------------------------------------------------------
print("Masterizando…")
# v6.88.1 — LA SALA ÉPICA (reverb de 170 ms al 24 %): llena los
#     valles entre golpes (el QC de saltos RMS de la casa baja de 0.45
#     a lo permitido) y la mezcla gana el AIRE de las batallas de
#     tráiler — más épica que la v6.87, que era en seco.
from scipy.signal import fftconvolve
rv_n = int(0.17 * SR)
def sala(semilla):
    g = np.random.default_rng(semilla)
    h = g.normal(0, 1, rv_n) * np.exp(-np.arange(rv_n) / (0.05 * SR))
    return h / (np.abs(h).sum() + 1e-9)
wetL = fftconvolve(mix[:, 0], sala(11))[:N]
wetR = fftconvolve(mix[:, 1], sala(23))[:N]     # decorrelado: sala ancha
mix = mix * 0.78 + np.stack([wetL, wetR], axis=1) * 0.24

# LA COMPRESIÓN DE TRAILER (wall of sound): tanh a 1.35 — los valles
#     suben, los picos se aplanan: el TRÁILER.
mix = np.tanh(mix * 1.35) / np.tanh(1.35)
peak = np.abs(mix).max()
mix = mix / peak * 0.89
fade_in = int(0.012 * SR); fade_out = int(0.05 * SR)
mix[:fade_in] *= np.linspace(0, 1, fade_in)[:, None]
mix[-fade_out:] *= np.linspace(1, 0, fade_out)[:, None]

win = 2048
rms = np.sqrt((mix[:len(mix)//win*win].reshape(-1, win, 2)**2).mean(axis=(1,2)))
print(f"RMS: media={rms.mean():.4f} pico={rms.max():.4f} min={rms.min():.4f}")
diffs = np.abs(np.diff(rms))
print(f"QC saltos RMS: máx={diffs.max():.4f} ({'OK' if diffs.max() < 0.09 else '¡CLIC!'})")
print(f"Peak: {np.abs(mix).max():.3f}  Dur: {len(mix)/SR:.1f} s")

out_wav = "/tmp/oleada_hambre_epica_v688.wav"
out_ogg = "AethonMod/Music/OleadaHambreEpica.ogg"
pcm = (mix * 32767).astype(np.int16)
stereo = np.empty(pcm.shape[0] * 2, dtype=np.int16)
stereo[0::2] = pcm[:, 0]; stereo[1::2] = pcm[:, 1]
with open(out_wav, 'wb') as f:
    import wave
    w = wave.open(f, 'wb')
    w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR)
    w.writeframes(stereo.tobytes())
    w.close()
subprocess.run(["ffmpeg", "-y", "-v", "error", "-i", out_wav,
                "-c:a", "libvorbis", "-q:a", "5", out_ogg], check=True)
sz = os.path.getsize(out_ogg)
print(f"OGG: {out_ogg} — {sz:,} B ({sz/1e6:.2f} MB)")
print("LISTO — LA OLEADA FINAL")
