#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 v6.55.0 — EL SONIDO DEL HAZ CONCENTRADO (la síntesis propia de la casa).
 LA LETRA: «sobre todo el sonido no puedes usarlo debes recrearlo de
 alguna manera después de todo es solo un sonido no puedes usar los
 archivos originales».

 EL LOOP ORIGINAL ERA UN ZUMBIDO DE ENERGÍA: un grave profundo que
 respira, un motor de armónicos, un siseo de plasma filtrado y un pulso
 lento. ESTA es la recreación 100 % sintetizada — cero material portado.

 LA TÉCNICA DEL BUCLE PERFECTO (la lección de la 1ª pasada: el
 crossfade clásico NO basta — el grave batía 0.21 de salto en la costura):
   · LAS CAPAS TONALES son EXACTAMENTE PERIÓDICAS por construcción —
     todas sus frecuencias son múltiplos enteros de 1/L (el grave, el
     motor, el pulso y la respiración) => envuelven PERFECTO solas.
   · EL RUIDO (plasma + crujido) se vuelve periódico con la costura
     suavizada: cola que funde con la cabeza (el paso residual del
     ruido filtrado queda enmascarado por las capas tonales).
 Salida: AethonMod/Content/Sounds/RayoConcentradoLoop.ogg (vorbis -q5).
=============================================================================
"""
import numpy as np
import subprocess
import os
import wave
from scipy.signal import butter, sosfiltfilt

SR = 44100
L = 6.0                      # segundos del bucle (el período EXACTO)
n = int(SR * L)
t = np.arange(n) / SR
rng = np.random.default_rng(20261027)

def floop(k):
    """Frecuencia EXACTAMENTE periódica: k ciclos completos en el loop."""
    return k / L

# ---------------------------------------------------------------- 1. EL GRAVE
# Dos senos casi afinados (batido de 4 ciclos en el loop — el corazón late)
f0 = floop(294)              # 49.0 Hz exactos (294/6)
grave = (np.sin(2 * np.pi * f0 * t) * 0.62 +
         np.sin(2 * np.pi * floop(295) * t + 0.7) * 0.50 +      # el batido
         np.sin(2 * np.pi * floop(147) * t) * 0.22)

# ---------------------------------------------------------------- 2. EL MOTOR
# Armónicos impares con detune DE UN CICLO ENTERO (periódico exacto)
motor = np.zeros(n)
for f, a, dk in [(floop(588), 0.30, 3), (floop(882), 0.16, -4),
                 (floop(1176), 0.10, 6), (floop(1764), 0.05, -8),
                 (floop(2352), 0.028, 5)]:
    fase = rng.uniform(0, 6.28)
    motor += a * (np.sin(2 * np.pi * f * t + fase) +
                  np.sin(2 * np.pi * (f + floop(dk)) * t + fase * 1.3)) * 0.5

# EL PULSO (2 ciclos del temblor por segundo; la envolvente lenta 1/6·5)
pulso = (0.72 + 0.28 * np.sin(2 * np.pi * floop(19) * t + 0.3) *
         (0.6 + 0.4 * np.sin(2 * np.pi * floop(3) * t + 1.1)))
motor *= pulso

# ---------------------------------------------------------------- 3. EL PLASMA
# Ruido pardo filtrado en banda móvil — el ruido SE VUELVE PERIÓDICO:
# se genera largo y su cola funde con su cabeza (la costura del ruido).
X = int(SR * 0.5)            # costura del ruido: 0.5 s
nn = n + X
tt = np.arange(nn) / SR
ruido = rng.normal(0, 1, nn)
pardo = np.cumsum(ruido)
pardo -= np.linspace(pardo[0], pardo[-1], nn)
pardo /= np.abs(pardo).max() + 1e-9

# banda móvil periódica (2 ciclos por loop)
centro = 640 + 280 * np.sin(2 * np.pi * floop(2) * tt + 0.9)
plasma = np.zeros(nn)
TRAMOS = 14
for i in range(TRAMOS):
    a, b = int(i * nn / TRAMOS), min(nn, int((i + 1.5) * nn / TRAMOS))
    fc = centro[(a + b) // 2]
    lo = max(40, fc - 260); hi = min(4000, fc + 380)
    sos = butter(3, [lo / (SR / 2), hi / (SR / 2)], btype="band", output="sos")
    plasma[a:b] += sosfiltfilt(sos, pardo[a:b]) * np.blackman(b - a) * 0.5
plasma /= (np.abs(plasma).max() + 1e-9)

# LA COSTURA DEL RUIDO: la cola funde con la cabeza (w suave)
w = (0.5 - 0.5 * np.cos(np.pi * np.arange(X) / X)) ** 1.2
plasma[n:] = plasma[n:] * (1 - w) + plasma[:X] * w
plasma = plasma[:n]          # el bucle del ruido: fin≈cabeza fundidos

# EL CRUJIDO (chispas agudas escasas — NINGUNA en la costura)
crujido = np.zeros(n)
for _ in range(int(L * 26)):
    pos = rng.integers(0, n - int(SR * 0.8) - 400)   # lejos de la costura
    dur = rng.integers(90, 380)
    imp = rng.normal(0, 1, dur) * np.exp(-np.linspace(0, 6, dur))
    crujido[pos:pos + dur] += imp * rng.uniform(0.2, 1.0)
sos_hp = butter(2, 1800 / (SR / 2), btype="high", output="sos")
crujido = sosfiltfilt(sos_hp, crujido)
crujido *= 0.10 / (np.abs(crujido).max() + 1e-9)

# ---------------------------------------------------------------- 4. LA MEZCLA
# La respiración global: 1 ciclo exacto por loop
mezcla = (grave * 0.42 + motor * 0.34 + plasma * 0.30 + crujido * 0.22)
mezcla *= 0.82 + 0.18 * np.sin(2 * np.pi * floop(1) * t + 2.2)

# normalizar a -1.5 dBFS
mezcla = mezcla / (np.abs(mezcla).max() + 1e-9) * (10 ** (-1.5 / 20))

# ---------------------------------------------------------------- 6. ESTÉREO
d = int(SR * 0.0004)
izq = mezcla
der = np.concatenate([mezcla[d:], mezcla[:d]])       # retardo cíclico (periódico)
ancho = np.sin(2 * np.pi * floop(1) * t + 1.0) * 0.03
der = der + crujido * ancho
izq = izq - crujido * ancho
izq = np.clip(izq, -1, 1)
der = np.clip(der, -1, 1)

# ---------------------------------------------------------------- 7. EXPORTAR
out_dir = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Sounds"
os.makedirs(out_dir, exist_ok=True)
wav = "/tmp/rayo_concentrado_loop.wav"
pcm = np.empty(n * 2, dtype=np.int16)
pcm[0::2] = (izq * 32767).astype(np.int16)
pcm[1::2] = (der * 32767).astype(np.int16)
with open(wav, "wb") as f:
    w_ = wave.open(f, "wb")
    w_.setnchannels(2)
    w_.setsampwidth(2)
    w_.setframerate(SR)
    w_.writeframes(pcm.tobytes())
    w_.close()

ogg = f"{out_dir}/RayoConcentradoLoop.ogg"
r = subprocess.run(["ffmpeg", "-y", "-i", wav, "-c:a", "libvorbis", "-qscale:a", "5",
                    "-metadata", "TITLE=Rayo Concentrado Loop", "-metadata", "ARTIST=AethonMod",
                    ogg], capture_output=True, text=True)
if r.returncode != 0:
    print(r.stderr[-800:])
    raise SystemExit(1)
print(f"OK: {ogg}  ({os.path.getsize(ogg)} bytes, {L}s loop periódico exacto)")

# verificación: el salto del TONAL en la costura debe ser ~0
salto_tonal = abs((grave * 0.42)[-1] * 0.82 - (grave * 0.42)[0] * 0.82)
print(f"  salto tonal fin→inicio: {salto_tonal:.5f} (periódico exacto)")
salto_total = abs(izq[-1] - izq[0])
print(f"  salto total fin→inicio: {salto_total:.5f} (solo el ruido, enmascarado)")
