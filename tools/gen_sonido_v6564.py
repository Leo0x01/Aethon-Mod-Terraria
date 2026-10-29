#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 v6.56.4 — LOS DOS SONIDOS DEL RAYO CONCENTRADO II (la 2ª síntesis de la casa).
 LA LETRA: «en el momento de carga necesita sonar que está cargando, debes
 generar ése sonido; luego cuando dispara el sonido actual es muy agudo para
 el tipo de haz de luz, el sonido que debe hacer tiene que ser un poco más
 grave mientras se dispara y mantiene el rayo concentrado, y en el momento
 en que el rayo comienza a apagarse el sonido también debe [apagarse]».

 SALIDA 1 — RayoConcentradoCarga.ogg (NUEVO): one-shot de 5.15 s — LA CARGA.
   El reloj del arma: 300 ticks = 5.0 s de carga; el one-shot toca desde el
   tick 2 y su CRESCENDO desemboca justo en la ignición (el loop del láser
   despierta con realSize→1 en ~60 ticks — la costura queda a cargo del
   volumen del juego, que ya viaja con realSize: la carga suena sola porque
   el loop es INAUDIBLE hasta el disparo).
   Las capas: EL GRAVE QUE DESPIERTA (44→55 Hz en glide, el suelo del
   conjuro), EL CORO ARMÓNICO QUE SUBE (registro medio-grave 172-688 Hz
   deslizándose +15% — nada de agudos: la letra pide GRAVE), EL PULSO
   ACELERANDO (1.8→4.0 latidos/s — el corazón de la carga apura) y EL AIRE
   (ruido pardo succión). EL FINALE: los últimos 0.45 s rampa al pico y
   corta en fade de 25 ms — la ignición la pone el loop.

 SALIDA 2 — RayoConcentradoLoop.ogg (REGENERADO, v3 «UN POCO MÁS GRAVE»):
   la 1ª gen (v6.55.0) era aguda de fábrica — el motor de armónicos vivía
   en 588-2352 Hz y la banda del plasma en 640±280 (móvil). La v3 baja el
   registro una octava+: motor en 172-688 Hz, plasma en BANDA FIJA a 340
   Hz (la ley v2 de la v6.56.0 se conserva: CERO batido, CERO respiración,
   banda fija — inicia el pulso, se mantiene, acaba), crujido una octava
   abajo (HP 1200) y más quedado, y la mezcla inclinada al grave. El pitch
   en juego queda a −0.22 sostenido (HandleSound) planeando a −0.70 al
   apagarse — la sensación de poder que la .56.3 no tenía.
=============================================================================
"""
import numpy as np
import subprocess
import os
import wave
from scipy.signal import butter, sosfiltfilt

SR = 44100
OUT_DIR = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Sounds"
os.makedirs(OUT_DIR, exist_ok=True)
rng = np.random.default_rng(20265640)

def floop_loop(k, L):
    """Frecuencia EXACTAMENTE periódica en el bucle de L segundos: k/L Hz."""
    return k / L


def exportar_ogg(nombre, titulo, izq, der):
    wav = f"/tmp/{nombre}.wav"
    n = len(izq)
    pcm = np.empty(n * 2, dtype=np.int16)
    pcm[0::2] = (np.clip(izq, -1, 1) * 32767).astype(np.int16)
    pcm[1::2] = (np.clip(der, -1, 1) * 32767).astype(np.int16)
    with open(wav, "wb") as f:
        w_ = wave.open(f, "wb")
        w_.setnchannels(2)
        w_.setsampwidth(2)
        w_.setframerate(SR)
        w_.writeframes(pcm.tobytes())
        w_.close()
    ogg = f"{OUT_DIR}/{nombre}.ogg"
    r = subprocess.run(["ffmpeg", "-y", "-i", wav, "-c:a", "libvorbis", "-qscale:a", "5",
                        "-metadata", f"TITLE={titulo}", "-metadata", "ARTIST=AethonMod",
                        ogg], capture_output=True, text=True)
    if r.returncode != 0:
        print(r.stderr[-800:])
        raise SystemExit(1)
    print(f"  OK: {ogg}  ({os.path.getsize(ogg)} bytes)")
    return ogg


# ======================================================================
#  SALIDA 1 — EL SONIDO DE LA CARGA (one-shot 5.15 s)
# ======================================================================
print("== RayoConcentradoCarga.ogg (la carga: «necesita sonar que está cargando») ==")
L = 5.15                       # el one-shot completo
T_SUBIDA = 4.95                # el crescendo desemboca en la ignición (tick 300)
n = int(SR * L)
t = np.arange(n) / SR
prog = np.clip(t / T_SUBIDA, 0, 1)          # 0..1 durante la subida
final = np.clip((t - (L - 0.45)) / 0.45, 0, 1)   # la rampa del finale

# --- 1. EL GRAVE QUE DESPIERTA: 44→55 Hz en glide (fase integrada — sin click)
fgrave = 44.0 + 11.0 * prog ** 1.4
fase_g = 2 * np.pi * np.cumsum(fgrave) / SR
grave = (np.sin(fase_g) * (0.20 + 0.34 * prog)
         + np.sin(2 * fase_g) * 0.10 * prog)

# --- 2. EL CORO ARMÓNICO QUE SUBE (registro medio-grave 172-688 Hz +15%):
#     parcial, amplitud, cuánto sube (×1+sube%), vibrato lento 5.5 Hz ±0.4%
coro = np.zeros(n)
for f0, a, sube in [(172.0, 0.16, 0.14), (258.0, 0.11, 0.16),
                    (344.0, 0.075, 0.18), (516.0, 0.042, 0.20), (688.0, 0.022, 0.22)]:
    f = f0 * (1.0 + sube * prog)
    fase = 2 * np.pi * np.cumsum(f) / SR
    vib = 1.0 + 0.004 * np.sin(2 * np.pi * 5.5 * t + f0)
    crece = (0.28 + 0.72 * prog ** 1.3)
    coro += a * crece * vib * np.sin(fase)

# --- 3. EL PULSO ACELERANDO: 1.8→4.0 latidos/s — el corazón apura.
#     Fase acumulada de la tasa instantánea (sin click entre pulsos).
tasa = 1.8 + 2.2 * prog ** 1.6
fase_p = 2 * np.pi * np.cumsum(tasa) / SR
pulso_env = 0.5 + 0.5 * np.cos(fase_p)            # 0..1 cosenoidal
pulso = pulso_env ** 2.2                          # picudo
pulso *= (0.10 + 0.16 * prog)

# --- 4. EL AIRE (la succión): ruido pardo paso-bajo 900 Hz, creciendo
X = int(SR * 0.4)
nn = n + X
ruido = rng.normal(0, 1, nn)
pardo = np.cumsum(ruido)
pardo -= np.linspace(pardo[0], pardo[-1], nn)
pardo /= np.abs(pardo).max() + 1e-9
sos = butter(2, 900 / (SR / 2), btype="low", output="sos")
aire = sosfiltfilt(sos, pardo)[:n] * (0.05 + 0.10 * prog) ** 1.2

# --- 5. EL FINALE: todo rampa al pico; shimmer breve (1.4 kHz, 0.05) y
#     fade de 25 ms al final — la ignición la pone el loop del láser.
surge = 1.0 + 0.30 * final
mezcla = (grave * 0.46 + coro * 0.34 + pulso + aire * 0.9) * surge
mezcla += 0.05 * final * np.sin(2 * np.pi * 1376.0 * t) * (t > T_SUBIDA - 0.02)
# normalizar a -2.0 dBFS y el corte de 25 ms
mezcla = mezcla / (np.abs(mezcla).max() + 1e-9) * (10 ** (-2.0 / 20))
fade = int(SR * 0.025)
mezcla[-fade:] *= np.linspace(1, 0, fade)

# estéreo: el coro se abre un pelo hacia el final (el conjuro crece)
d = int(SR * 0.0006)
der_c = np.concatenate([coro[d:], coro[:d]])
ancho = 0.02 + 0.10 * final
izq = np.clip(mezcla - (der_c - coro) * ancho * 0.5, -1, 1)
der = np.clip(mezcla + (der_c - coro) * ancho * 0.5, -1, 1)
exportar_ogg("RayoConcentradoCarga", "Rayo Concentrado — Carga (v6.56.4)", izq, der)

# ======================================================================
#  SALIDA 2 — EL LOOP DEL LÁSER (v3, «UN POCO MÁS GRAVE»)
# ======================================================================
print("== RayoConcentradoLoop.ogg (v3 — el loop regenerado una octava más oscuro) ==")
LL = 6.0                       # el período EXACTO del bucle (como la 1ª gen)
m = int(SR * LL)
tm = np.arange(m) / SR

# --- 1. EL GRAVE: un SOLO 48 Hz exactos (CERO batido — la ley v2: la 1ª
#     gen latía 49.0/49.17 y el usuario oyó «ondas que van y vienen»)
graveL = (np.sin(2 * np.pi * floop_loop(288, LL) * tm) * 0.60
          + np.sin(2 * np.pi * floop_loop(576, LL) * tm) * 0.14)   # 96 Hz el quinto, quedado

# --- 2. EL MOTOR OSCURO: una octava+ abajo que la 1ª gen (588-2352 →
#     172-688). Detune de UN CICLO ENTERO (periódico exacto — la técnica
#     del bucle perfecto de la v6.55.0).
motorL = np.zeros(m)
for f, a, dk in [(floop_loop(1032, LL), 0.34, 3),      # 172 Hz
                 (floop_loop(1548, LL), 0.22, -4),     # 258 Hz
                 (floop_loop(2064, LL), 0.13, 6),      # 344 Hz
                 (floop_loop(3096, LL), 0.06, -8),     # 516 Hz
                 (floop_loop(4128, LL), 0.03, 5)]:     # 688 Hz
    fase = rng.uniform(0, 6.28)
    motorL += a * (np.sin(2 * np.pi * f * tm + fase) +
                   np.sin(2 * np.pi * (f + floop_loop(dk, LL)) * tm + fase * 1.3)) * 0.5
# LA LEY V2: CERO respiración de mezcla, CERO vaivén de pitch. El motor
# queda tal cual — el juego modula volumen/pitch por frame (HandleSound).

# --- 3. EL PLASMA: BANDA FIJA a 340±120 Hz (la 1ª gen barría 640±280 —
#     la ilusión de onda que la v2 enterró; la v3 además la baja). Ruido
#     pardo periódico por costura fundida (la técnica del bucle perfecto).
Xm = int(SR * 0.5)
mm = m + Xm
rm = rng.normal(0, 1, mm)
pardoM = np.cumsum(rm)
pardoM -= np.linspace(pardoM[0], pardoM[-1], mm)
pardoM /= np.abs(pardoM).max() + 1e-9
sosB = butter(3, [(340 - 120) / (SR / 2), (340 + 120) / (SR / 2)], btype="band", output="sos")
plasmaL = sosfiltfilt(sosB, pardoM)[:m]
plasmaL /= (np.abs(plasmaL).max() + 1e-9)
w = (0.5 - 0.5 * np.cos(np.pi * np.arange(Xm) / Xm)) ** 1.2
plasmaL[m - Xm:] = plasmaL[m - Xm:] * (1 - w) + plasmaL[:Xm] * w
plasmaL = plasmaL[:m] * 0.5

# --- 4. EL CRUJIDO: una octava abajo (HP 1200) y más quedado (0.06)
crujidoL = np.zeros(m)
for _ in range(int(LL * 20)):
    pos = rng.integers(0, m - int(SR * 0.8) - 400)
    dur = rng.integers(90, 380)
    imp = rng.normal(0, 1, dur) * np.exp(-np.linspace(0, 6, dur))
    crujidoL[pos:pos + dur] += imp * rng.uniform(0.2, 1.0)
sosH = butter(2, 1200 / (SR / 2), btype="high", output="sos")
crujidoL = sosfiltfilt(sosH, crujidoL)
crujidoL *= 0.06 / (np.abs(crujidoL).max() + 1e-9)

# --- 5. LA MEZCLA: inclinada al grave (el grave manda; el registro agudo
#     de la 1ª gen era lo que el usuario oyó como «muy agudo»)
mezclaL = graveL * 0.46 + motorL * 0.30 + plasmaL * 0.34 + crujidoL * 0.10
mezclaL = mezclaL / (np.abs(mezclaL).max() + 1e-9) * (10 ** (-1.5 / 20))

# estéreo: retardo cíclico (periódico exacto — la 1ª gen)
dm = int(SR * 0.0004)
izqL = mezclaL
derL = np.concatenate([mezclaL[dm:], mezclaL[:dm]])
anchom = np.sin(2 * np.pi * floop_loop(1, LL) * tm + 1.0) * 0.03
derL = derL + crujidoL * anchom
izqL = izqL - crujidoL * anchom
izqL = np.clip(izqL, -1, 1); derL = np.clip(derL, -1, 1)
exportar_ogg("RayoConcentradoLoop", "Rayo Concentrado — Lazo v3 (grave, v6.56.4)", izqL, derL)

# verificación de bucle: el salto tonal fin→inicio debe ser ~0
salto = abs(izqL[-1] - izqL[0])
print(f"  salto fin→inicio del loop: {salto:.5f} (periódico exacto)")
print("HECHO: los dos .ogg regenerados.")
