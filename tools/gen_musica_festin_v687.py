#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.87 — EL FESTÍN DESATADO: la banda sonora de las oleadas de hambre,
VERSIÓN ANIMADA Y ÉPICA (la letra: «la banda sonora que creaste para las
oleadas de hambre debe ser más animada y épica»).

La v6.85 era ambiente (70 BPM, pad + latido): la INQUIETUD del hambre.
La v6.87 es EL ASALTO: 152 BPM, percusión completa (kick/snare/hats/
toms/taikos/crash), bajo ostinato en corcheas, cuerdas staccato estilo
choo-choo épico, CORNO de melena saw aditivo, CORO de 4 voces con
formantes, arpegios en 16avos, LA CAMPANA INARMÓNICA del grimorio (la
firma que sobrevive) y EL LATIDO lub-dub integrado al tempo (la firma
del hambre que sobrevive).

Estructura (4/4, 152 BPM, compás = 60/152*4 ≈ 1.579 s):
  S1  0-8   INTRO      — latido acelerando 61→122, dron, riser, taiko
  S2  8-24  TEMA A     — percusión completa + bajo ostinato + staccato
  S3  24-40 TEMA B     — el CORO ÉPICO: melodía de corno + coro aah
  S4  40-48 PUENTE     — medio tiempo: latido + arpegios + bruma
                         (la calma entre oleadas) + snare roll
  S5  48-72 CLÍMAX     — TODO: tema B ×1.5, doble kick, toms, campana
  S6  72-80 COLA       — decae al dron + latido (empalma con la intro)

Salida: Music/OleadaHambre.ogg (44.1 kHz estéreo, vorbis) — la ruta es
la misma de siempre; el MusicLoader no cambia.
"""
import numpy as np
import subprocess, os, math

SR = 44100
BPM = 152.0
BEAT = 60.0 / BPM            # 0.3947 s
BAR = BEAT * 4               # 1.579 s
N_BARS = 80
DUR = N_BARS * BAR + 2.0     # cola de reverb ~ 128.3 s
N = int(DUR * SR)
print(f"Duración total: {DUR:.1f} s ({N} muestras)")

rng = np.random.default_rng(20260404)

mix = np.zeros((N, 2), dtype=np.float64)

# ----------------------------------------------------------------------
# SÍNTESIS — saw ADITIVO (la máxima calidad: cero aliasing) + helpers
# ----------------------------------------------------------------------
def saw(freq, dur, brightness=1.0):
    """Saw aditivo limitado a Nyquist — limpio de aliasing."""
    t = np.arange(int(dur * SR)) / SR
    out = np.zeros_like(t)
    k = 1
    while freq * k < SR * 0.48 and k <= int(90 * brightness):
        out += np.sin(2 * np.pi * freq * k * t + rng.uniform(0, 2*np.pi)) / k
        k += 1
    return out * (2 / np.pi)

def square_wave(freq, dur, duty=0.5):
    """Cuadrada aditiva (solo armónicos impares)."""
    t = np.arange(int(dur * SR)) / SR
    out = np.zeros_like(t)
    k = 1
    while freq * k < SR * 0.48 and k <= 80:
        if k % 2 == 1:
            out += np.sin(2 * np.pi * freq * k * t) / k
        k += 1
    return out * (4 / np.pi) * duty

def tri(freq, dur):
    t = np.arange(int(dur * SR)) / SR
    out = np.zeros_like(t)
    k, s = 1, 1.0
    while freq * k < SR * 0.48 and k <= 40:
        odd = k % 2 == 1
        if odd:
            out += s * np.sin(2 * np.pi * freq * k * t) / (k * k)
            s = -s
        k += 1
    return out * (8 / (np.pi * np.pi))

def lowpass(x, cutoff, orden=2):
    """Filtro paso-bajo por FFT (por bloque/nota: fase consistente)."""
    X = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / SR)
    H = 1.0 / (1.0 + (f / max(cutoff, 1.0)) ** (2 * orden))
    return np.fft.irfft(X * H, n=len(x))

def highpass(x, cutoff, orden=2):
    X = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / SR)
    H = 1.0 - 1.0 / (1.0 + (f / max(cutoff, 1.0)) ** (2 * orden))
    return np.fft.irfft(X * H, n=len(x))

def bandpass(x, lo, hi):
    return highpass(lowpass(x, hi), lo)

def env(n, a, d, s_level, r, sustain_n=None):
    """ADSR sobre n muestras."""
    a_n, d_n, r_n = int(a*SR), int(d*SR), int(r*SR)
    if sustain_n is None:
        sustain_n = max(0, n - a_n - d_n - r_n)
    e = np.concatenate([
        np.linspace(0, 1, max(a_n, 1), endpoint=False),
        np.linspace(1, s_level, max(d_n, 1), endpoint=False),
        np.full(max(sustain_n, 0), s_level),
        np.linspace(s_level, 0, max(r_n, 1))])
    if len(e) < n:
        e = np.pad(e, (0, n - len(e)))
    return e[:n]

def pan(mono, p):
    """p ∈ [-1, 1] → estéreo (pan constante)."""
    th = (p + 1) * np.pi / 4
    return np.stack([mono * np.cos(th), mono * np.sin(th)], axis=1)

def put(buf, t_sec, stereo, gain=1.0):
    i0 = int(t_sec * SR)
    if i0 >= N or i0 < 0: return
    seg = stereo[:N - i0] * gain
    buf[i0:i0 + len(seg)] += seg

# ------------------------------------------------------------------
# NOTAS
# ------------------------------------------------------------------
NOTAS = {}
NOMBRES = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B']
for octava in range(1, 8):
    for i, nn in enumerate(NOMBRES):
        midi = 12 * (octava + 1) + i
        NOTAS[f"{nn}{octava}"] = 440.0 * 2 ** ((midi - 69) / 12)

def nf(nombre):  # frecuencia
    return NOTAS[nombre]

# ==================================================================
# 1. PERCUSIÓN
# ==================================================================
def kick(vel=1.0):
    d = 0.42
    t = np.arange(int(d * SR)) / SR
    f = 148 * np.exp(-t * 26) + 46
    ph = 2 * np.pi * np.cumsum(f) / SR
    x = np.sin(ph) * np.exp(-t * 7.5)
    click = rng.standard_normal(int(0.004 * SR)) * np.linspace(1, 0, int(0.004*SR))
    x[:len(click)] += click * 0.5
    return x * vel, d

def snare(vel=1.0):
    d = 0.24
    n = int(d * SR)
    ruido = rng.standard_normal(n)
    ruido = bandpass(ruido, 1500, 7500) * np.exp(-np.arange(n)/SR * 22)
    tono = np.sin(2*np.pi*186*np.arange(n)/SR) * np.exp(-np.arange(n)/SR * 34) * 0.55
    return (ruido * 0.72 + tono) * vel, d

def hat(vel=1.0, abierto=False):
    d = 0.26 if abierto else 0.055
    n = int(d * SR)
    r = highpass(rng.standard_normal(n), 7800)
    return r * np.exp(-np.arange(n)/SR * (6 if abierto else 62)) * vel * 0.38, d

def tom(f0=180, vel=1.0):
    d = 0.34
    t = np.arange(int(d*SR)) / SR
    f = f0 * np.exp(-t * 9) + f0 * 0.55
    ph = 2*np.pi*np.cumsum(f)/SR
    return np.sin(ph) * np.exp(-t * 10) * vel, d

def taiko(vel=1.0):
    d = 0.5
    n = int(d*SR)
    t = np.arange(n)/SR
    x = np.sin(2*np.pi*(82*np.exp(-t*10)+58)*t) * np.exp(-t*6.5)
    golpe = bandpass(rng.standard_normal(n), 300, 2400) * np.exp(-t*26) * 0.35
    return (x + golpe) * vel, d

def crash(vel=1.0):
    d = 1.9
    n = int(d*SR)
    r = highpass(rng.standard_normal(n), 5200)
    e = np.exp(-np.arange(n)/SR * 1.7)
    shimmer = 1 + 0.25*np.sin(np.arange(n)/SR * 2*np.pi*7.3)
    return r * e * shimmer * vel * 0.30, d

# ==================================================================
# 2. EL LATIDO DEL HAMBRE (lub-dub — la firma que sobrevive)
# ==================================================================
def latido(bpm_corazon=62):
    """Un ciclo lub-dub; devuelve (audio, dur)."""
    d = 60.0 / bpm_corazon
    n = int(d * 1.15 * SR)
    t = np.arange(n)/SR
    def golpe(t0, f0, amp):
        i0 = int(t0*SR)
        tt = t[:max(0, n-i0)]
        xx = np.sin(2*np.pi*(f0*np.exp(-tt*20)+34)*tt) * np.exp(-tt*13) * amp
        out = np.zeros(n); out[i0:i0+len(xx)] += xx
        return out
    x = golpe(0.0, 92, 1.0) + golpe(0.16, 78, 0.72)
    # el murmullo de las válvulas
    x += bandpass(rng.standard_normal(n), 55, 400) * np.exp(-t*10) * 0.10
    return x, n/SR

# ==================================================================
# 3. INSTRUMENTOS ARMÓNICOS
# ==================================================================
def bajo(nota, dur, vel=1.0):
    """Bajo ostinato: saw filtrado + sub senoidal."""
    f = nf(nota) / 2
    s = saw(f, dur, 0.55)
    s = lowpass(s, 320 + 900 * vel)
    sub = np.sin(2*np.pi*f*0.5*np.arange(len(s))/SR) * 0.62
    e = env(len(s), 0.004, 0.05, 0.62, min(0.09, dur*0.35))
    return (s * 0.55 + sub) * e * vel, dur

def cuerdas_staccato(nota, dur, vel=1.0):
    """El choo-choo épico: 3 saws desafinados, envolvente corta."""
    f = nf(nota)
    det = [0.997, 1.0, 1.004]
    x = sum(saw(f*dd, dur, 0.7) for dd in det) / 3
    x = lowpass(x, 1900 + 1500 * vel)
    e = env(len(x), 0.006, 0.07, 0.30, min(0.10, dur*0.4))
    return x * e * vel * 0.8, dur

def corno(nota, dur, vel=1.0):
    """EL CORNO ÉPICO: saw+cuadrada en octavas, attack de 28 ms."""
    f = nf(nota)
    x = saw(f, dur, 0.8) * 0.5 + square_wave(f*0.5, dur) * 0.30 \
        + saw(f*2, dur, 0.45) * 0.16
    x = lowpass(x, 2600)
    # el brace del metal: leve saturación
    x = np.tanh(x * 1.7) * 0.62
    e = env(len(x), 0.028, 0.10, 0.74, min(0.16, dur*0.3))
    vib = 1 + 0.006*np.sin(2*np.pi*5.2*np.arange(len(x))/SR)
    return x * e * vib * vel, dur

def coro_aah(nota, dur, vel=1.0):
    """Coro: 4 voces con vibrato + formantes (aah ~ 700/1100 Hz)."""
    f = nf(nota)
    n = int(dur*SR)
    t = np.arange(n)/SR
    x = np.zeros(n)
    for k, (ratio, amps) in enumerate([(0.998, 1.0), (1.002, 0.9),
                                        (0.501, 0.35), (2.001, 0.22)]):
        vib = 1 + 0.0045 * np.sin(2*np.pi*(4.6+k*0.37)*t + k)
        x += amps * np.sin(2*np.pi*f*ratio*vib*t + k*1.7)
    x /= 2.5
    x = bandpass(x, 480, 2900) * 1.35 + x * 0.5   # formante + cuerpo
    e = env(n, 0.16, 0.12, 0.8, min(0.25, dur*0.35))
    return x * e * vel, dur

def campana(f0=None, vel=1.0):
    """LA CAMPANA INARMÓNICA del grimorio (la firma que sobrevive)."""
    if f0 is None: f0 = nf('D4')
    d = 3.4
    n = int(d*SR)
    t = np.arange(n)/SR
    parciales = [(1.0, 1.0, 3.1), (2.32, 0.55, 5.2), (3.01, 0.34, 7.0),
                 (4.27, 0.22, 9.5), (5.63, 0.13, 12.5), (7.11, 0.07, 16.0)]
    x = np.zeros(n)
    for r, a, dk in parciales:
        x += a * np.sin(2*np.pi*f0*r*t) * np.exp(-t*dk)
    x = highpass(x, 300) * 0.5 + lowpass(x, 4200) * 0.6
    return x * vel * 0.5, d

def pluck(nota, vel=1.0):
    """Arpegio 16avos: triangle con decay corto."""
    f = nf(nota)
    d = 0.30
    n = int(d*SR)
    x = tri(f, d) + saw(f, d, 0.35)*0.22
    e = np.exp(-np.arange(n)/SR * 13.5)
    return x * e * vel * 0.8, d

def dron(dur):
    """El dron de D2 — la inquietud que no cesa (ahora con movimiento)."""
    f = nf('D2')
    n = int(dur*SR)
    t = np.arange(n)/SR
    x = (np.sin(2*np.pi*f*t) + 0.42*np.sin(2*np.pi*f*2*t + 0.7)
         + 0.20*np.sin(2*np.pi*f*3.01*t + 1.9)) / 1.62
    lfo = 0.72 + 0.28*np.sin(2*np.pi*0.09*t)   # respira
    return x * lfo * 0.30, dur

def riser(dur, vel=1.0):
    """Ruido con barrido ascendente — la oleada que viene."""
    n = int(dur*SR)
    r = rng.standard_normal(n)
    t = np.arange(n)/SR
    # el barrido por bloques: cada vez más agudo
    out = np.zeros(n)
    B = 4096
    for i in range(0, n, B):
        seg = r[i:i+B]
        tt = (i + B/2)/SR
        c = 240 * (2 ** (tt/dur * 4.2))
        out[i:i+B] = bandpass(seg, c*0.6, c*1.8)
    e = (t/dur) ** 2.1
    return out * e * vel * 0.5, dur

def bruma_soplo(dur, vel=1.0):
    """El aire del festín (ruido grave filtrado)."""
    n = int(dur*SR)
    t = np.arange(n)/SR
    r = lowpass(rng.standard_normal(n), 700)
    e = np.sin(np.pi * t/dur) ** 2
    return r * e * vel * 0.35, dur

# ==================================================================
# 4. LA PARTITURA
# ==================================================================
# Progresión (firma de la casa): i–VI–III–VII en re menor.
# Dm–Bb–F–C ; el clímax añade la dominante: Dm–Bb–Gm–A
PROG_A = [('D3','F3','A3'), ('A#2','D3','F3'), ('F2','A2','C3'), ('C3','E3','G3')]
BAJO_A  = ['D2','A#1','F1','C2']
PROG_B = [('D3','F3','A3'), ('A#2','D3','F3'), ('G2','B2','D3'), ('A2','C#3','E3')]
BAJO_B  = ['D2','A#1','G1','A1']

def acorde_en(prog, idx, dur):
    return prog[idx % len(prog)], dur

def bar(t):  # tiempo de inicio de compás
    return t * BAR

print("Secuenciando…")

# ---------- S1: INTRO (0-8) — el hambre despierta ----------
t0 = 0.0
# dron desde el segundo 0
d, dd = dron(BAR*8 + 2.5)
put(mix, 0.0, pan(d, 0.0), 0.9)
# latido acelerando 61→122 bpm (1 golpe por compás, 2 en los últimos 2)
for b in range(8):
    bpm_c = 61 + (122-61) * b/7
    lb, ld = latido(bpm_c)
    put(mix, b*BAR, pan(lb, -0.05), 1.15)
    if b >= 6:
        put(mix, b*BAR + BEAT*2, pan(lb, 0.05), 0.9)
# taikos marcando el pulso (mitad de compás) desde el compás 4
for b in range(4, 8):
    tk, td = taiko(0.8)
    put(mix, b*BAR, pan(tk, -0.35), 0.9)
    tk, td = taiko(0.62)
    put(mix, b*BAR + BEAT*2, pan(tk, 0.35), 0.8)
# riser los últimos 2 compases
rz, rd = riser(BAR*2, 1.0)
put(mix, BAR*6, pan(rz, 0.0), 1.0)
# campana lejana al inicio
cb, cd = campana(vel=0.6)
put(mix, 0.5, pan(cb, 0.4), 0.75)

# ---------- S2: TEMA A (8-24) — el asalto ----------
def secuencia_tema_a(off, bars, gain=1.0):
    for b in range(bars):
        t = off + b*BAR
        idx = b % 4
        # kick 4-on-the-floor + kick de más en el 4º compás
        for beat in range(4):
            k, kd = kick(1.12 if beat < 2 else 1.0)
            put(mix, t + beat*BEAT, pan(k, 0.0), gain)
        if idx == 3:
            k, kd = kick(0.7)
            put(mix, t + 3.5*BEAT, pan(k, 0.0), gain*0.85)
        # snare 2 y 4
        s, sd = snare(1.0); put(mix, t + 1*BEAT, pan(s, -0.12), gain)
        s, sd = snare(0.95); put(mix, t + 3*BEAT, pan(s, 0.12), gain)
        # ghost snare al final de la frase
        if idx == 3:
            s, sd = snare(0.4); put(mix, t + 3.75*BEAT, pan(s, 0.2), gain*0.8)
        # hats corcheas (abierto en el and del 2 y 4)
        for e8 in range(8):
            abierto = e8 in (3, 7)
            h, hd = hat(0.30 if not abierto else 0.42, abierto)
            put(mix, t + e8*BEAT/2, pan(h, 0.28 if e8 % 2 else -0.28), gain)
        # bajo ostinato en corcheas: RAÍZ-raíz-raíz-RAÍZ-raíz-raíz-OCTAVA-raíz
        nota_b = BAJO_A[idx]
        nota_oct = nota_b[:-1] + str(int(nota_b[-1]) + 1)   # la octava (D2→D3)
        patron = [1,0,0, 0.7,0,0, 0.85,0]
        for e8 in range(8):
            if patron[e8] > 0:
                bb, bd = bajo(nota_b if e8 != 6 else nota_oct, BEAT*0.55, patron[e8])
                put(mix, t + e8*BEAT/2, pan(bb, 0.0), 1.08*gain)
        # cuerdas staccato: el choo-choo (corcheas, acento en el tiempo)
        acorde = PROG_A[idx]
        for e8 in range(8):
            acento = 1.0 if e8 % 2 == 0 else 0.62
            for j, vc in enumerate(acorde):
                cs, cd2 = cuerdas_staccato(vc, BEAT*0.52, acento*(0.9 if j==0 else 0.75))
                put(mix, t + e8*BEAT/2, pan(cs, [-0.4, 0.0, 0.4][j]), 0.80*gain)
        # campana cada 8 compases
        if (b % 8) == 7:
            cb, cd = campana(vel=0.75)
            put(mix, t + 2*BEAT, pan(cb, -0.3), 0.8*gain)

secuencia_tema_a(BAR*8, 16)

# ---------- S3: TEMA B (24-40) — el coro épico ----------
def tema_b(off, bars, gain=1.0, con_doble_kick=False):
    # LA MELODÍA DEL CORNO (heroica, re menor natural):
    # D5 F5 A5 G5 | A5 . F5 E5 | F5 . D5 . | E5 F5 E5 D5 (por compás,
    # variando la última nota por acorde)
    melodia = [
        [('D5',0,1.0), ('F5',1,0.9), ('A5',2,1.0), ('G5',3,0.85)],           # Dm
        [('A5',0,1.0), ('F5',1.5,0.8), ('E5',2.5,0.7), ('F5',3,0.8)],        # Bb
        [('F5',0,1.0), ('D5',1,0.85), ('F5',2,0.8), ('A5',3,0.9)],           # F
        [('G5',0,1.0), ('A5',1,0.9), ('B4',2,0.75), ('C#5',2.5,0.8), ('E5',3,0.9)],  # A (dominante)
    ]
    for b in range(bars):
        t = off + b*BAR
        idx = b % 4
        # percusión (heredada) + doble kick en el clímax
        for beat in range(4):
            k, kd = kick(1.08 if beat < 2 else 0.96)
            put(mix, t + beat*BEAT, pan(k, 0.0), gain)
            if con_doble_kick and beat < 3:
                k2, kd2 = kick(0.6)
                put(mix, t + beat*BEAT + BEAT*0.5, pan(k2, 0.0), gain*0.7)
        s, sd = snare(1.0); put(mix, t + 1*BEAT, pan(s, -0.12), gain)
        s, sd = snare(0.98); put(mix, t + 3*BEAT, pan(s, 0.12), gain)
        for e8 in range(8):
            abierto = e8 in (3, 7)
            h, hd = hat(0.34 if not abierto else 0.48, abierto)
            put(mix, t + e8*BEAT/2, pan(h, 0.3 if e8 % 2 else -0.3), gain)
        # toms de relleno el último compás de cada 4
        if idx == 3:
            for j, (bt, f0) in enumerate([(3.0, 200), (3.25, 170), (3.5, 140), (3.75, 110)]):
                tm, td = tom(f0, 0.9 - j*0.1)
                put(mix, t + bt*BEAT, pan(tm, -0.3 + j*0.2), 0.85*gain)
        # bajo (PROG_B)
        nota_b = BAJO_B[idx]
        for e8 in range(8):
            if e8 % 2 == 0:
                bb, bd = bajo(nota_b, BEAT*0.9, 0.95 if e8 % 4 == 0 else 0.7)
                put(mix, t + e8*BEAT/2, pan(bb, 0.0), 1.10*gain)
        # CUERDAS ahora SOSTENIDAS (el colchón del coro)
        acorde = PROG_B[idx]
        for j, vc in enumerate(acorde):
            cs, cd2 = cuerdas_sostenidas = cuerdas_staccato(vc, BAR*0.98, 0.55) if False else (None, None)
            # (usamos el coro como colchón — ver abajo)
        # CORO aah — el colchón épico (3 voces del acorde)
        for j, vc in enumerate(acorde):
            ca, cad = coro_aah(vc, BAR*0.99, 0.85 if j == 0 else 0.66)
            put(mix, t, pan(ca, [-0.42, 0.0, 0.42][j]), 0.72*gain)
        # EL CORNO — la melodía
        for (nota, beat, vel) in melodia[idx]:
            cn, cd = corno(nota, BEAT*1.15, vel)
            put(mix, t + beat*BEAT, pan(cn, 0.08), 0.92*gain)
            # el eco del corno (delay de 3/8)
            cn2, _ = corno(nota, BEAT*0.8, vel*0.5)
            put(mix, t + beat*BEAT + BEAT*0.75, pan(cn2, -0.3), 0.30*gain)
        # campana
        if (b % 8) == 3:
            cb, cd = campana(vel=0.8)
            put(mix, t, pan(cb, 0.35), 0.85*gain)

tema_b(BAR*24, 16)

# ---------- S4: PUENTE (40-48) — la calma entre oleadas ----------
for b in range(8):
    t = BAR*40 + b*BAR
    # solo latido (más rápido: la amenaza no se fue) + bruma
    lb, ld = latido(96)
    put(mix, t, pan(lb, 0.0), 1.0)
    if b >= 4:
        put(mix, t + BEAT*2, pan(lb, 0.0), 0.8)
    bz, bdur = bruma_soplo(BAR*1.1, 0.9)
    put(mix, t, pan(bz, -0.15), 0.9)
    # arpegios suaves de Dm (la memoria del tema)
    arpegio = ['D4', 'F4', 'A4', 'D5', 'A4', 'F4']
    for e8 in range(6):
        pl, pd = pluck(arpegio[e8], 0.55)
        put(mix, t + e8*BEAT*0.66, pan(pl, 0.35 if e8 % 2 else -0.35), 0.5)
    # snare roll creciente los últimos 2 compases
    if b >= 6:
        steps = 16
        for s_i in range(steps):
            vel = 0.16 + 0.55 * (s_i/steps) * ((b-5)/2)
            s, sd = snare(vel)
            put(mix, t + s_i*(BAR/steps), pan(s, 0.0), 1.0)
# campana fantasma
cb, cd = campana(vel=0.55)
put(mix, BAR*41 + BEAT, pan(cb, -0.4), 0.8)

# ---------- S5: CLÍMAX (48-72) — TODO JUNTO ----------
# el dron sigue (la inquietud nunca cesa)
d, dd = dron(BAR*24 + 2)
put(mix, BAR*48, pan(d, 0.0), 0.75)
# crash de entrada
cr, cdur = crash(1.0)
put(mix, BAR*48, pan(cr, 0.1), 1.1)
secuencia_tema_a(BAR*48, 8, gain=0.92)        # el tema A vuelve…
tema_b(BAR*56, 16, gain=1.08, con_doble_kick=True)  # …y el coro lo corona
# arpegios en 16avos TODO el clímax
escala = ['D4','E4','F4','G4','A4','A#4','C5','D5']
for b in range(24):
    t = BAR*48 + b*BAR
    for s16 in range(16):
        nota = escala[(s16 + b*3) % len(escala)]
        pl, pd = pluck(nota, 0.4 if s16 % 4 else 0.55)
        put(mix, t + s16*BEAT/4, pan(pl, 0.45 if s16 % 2 else -0.45), 0.34)
# campana cada 4 compases en el clímax
for b in range(0, 24, 4):
    cb, cd = campana(vel=0.85)
    put(mix, BAR*48 + b*BAR, pan(cb, 0.25), 0.9)
# crash en cada entrada de frase del clímax
for b in (0, 8, 16):
    cr, cdur = crash(0.8)
    put(mix, BAR*56 + b*BAR, pan(cr, -0.1), 0.8)

# ---------- S6: COLA (72-80) — el empalme con la intro ----------
for b in range(8):
    t = BAR*72 + b*BAR
    lb, ld = latido(70)
    put(mix, t, pan(lb, -0.05), max(0.0, 1.25 - b*0.12))
    if b == 0:
        cr, cdur = crash(0.7)
        put(mix, t, pan(cr, 0.0), 0.7)
    bz, bdur = bruma_soplo(BAR*1.05, 0.8 * (1 - b/9))
    put(mix, t, pan(bz, 0.1), 0.8)
# la campana FINAL (el festín termina… por ahora)
cb, cd = campana(vel=0.9)
put(mix, BAR*72 + BEAT*2, pan(cb, 0.0), 0.95)
# el dron se despide (el mismo de la intro — el loop respira)
d, dd = dron(BAR*8 + 3)
put(mix, BAR*74, pan(d, 0.0), 0.72)

# ==================================================================
# 5. MEZCLA + MASTERIZACIÓN
# ==================================================================
print("Masterizando…")
# compresión suave (el pegamento) + limitador
mix = np.tanh(mix * 0.9) / np.tanh(0.9)
peak = np.abs(mix).max()
mix = mix / peak * 0.89
# anti-clic en los bordes
fade_in = int(0.012 * SR); fade_out = int(0.05 * SR)
mix[:fade_in] *= np.linspace(0, 1, fade_in)[:, None]
mix[-fade_out:] *= np.linspace(1, 0, fade_out)[:, None]

# RMS por ventanas — QC de clics
win = 2048
rms = np.sqrt((mix[:len(mix)//win*win].reshape(-1, win, 2)**2).mean(axis=(1,2)))
print(f"RMS: media={rms.mean():.4f} pico={rms.max():.4f} min={rms.min():.4f}")
diffs = np.abs(np.diff(rms))
print(f"QC saltos RMS: máx={diffs.max():.4f} ({'OK' if diffs.max() < 0.09 else '¡CLIC!'})")
print(f"Peak: {np.abs(mix).max():.3f}  Dur: {len(mix)/SR:.1f} s")

# ----------------------------------------------------------------------
# SALIDA — WAV → OGG (vorbis, el formato del juego)
# ----------------------------------------------------------------------
out_wav = "/tmp/oleada_hambre_v687.wav"
out_ogg = "AethonMod/Music/OleadaHambre.ogg"
pcm = (mix * 32767).astype(np.int16)
# intercalar canales
stereo = np.empty(pcm.shape[0]*2, dtype=np.int16)
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
print("LISTO — EL FESTÍN DESATADO")
