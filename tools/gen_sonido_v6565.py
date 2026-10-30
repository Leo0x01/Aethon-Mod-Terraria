#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 v6.56.5 — LA CURA DEL PITIDO DE LA CARGA (la 3ª síntesis de la casa).
 LA LETRA: «en el sonido de carga escucho un pitido al final».

 EL DIAGNÓSTICO (forense espectral sobre el .ogg publicado en la v6.56.4):
 el "shimmer" del finale era UN SENO PURO de 1376 Hz (`0.05·final·sin(2π·1376t)`)
 que crecía durante los últimos 0.45 s — el análisis de ventanas de 0.5 s lo
 clavó: la línea de 1376 Hz vivía 21-32 dB sobre el suelo espectral durante
 todo el archivo (enmascarada por el coro) pero en la ventana t=4.5-5.0 s
 SALTABA a +58.3 dB sobre el suelo. Un tono puro aislado a todo volumen en
 la cola = un pitido de módem. El oído del usuario tenía razón.

 LA CURA (dos cambios, SOLO en la SALIDA 1 — el loop v3 no se toca):
   1. EL SENO MUERE → NACE EL SOPLO DE PRESIÓN: ruido de banda 1.1-3.4 kHz
      (butter orden 2, paso banda) hinchando con final^1.6 — energía de
      agudos que CRECE sin frecuencia definida: el oído lo lee como presión
      a punto de estallar, no como una nota. La anticipación del finale
      queda intacta, sin nada que "pitarse".
   2. EL FADE 25 ms → 60 ms: el grave termina el glide en 55 Hz; 25 ms son
      1.4 ciclos de esa fundamental = golpecillo DC en el corte. 60 ms son
      3.3 ciclos — el final exhala en vez de toser.
 Todo lo demás (capas, semilla, estéreo, normalización -2.0 dBFS) queda
 BYTE A BYTE equivalente al v6.56.4: misma rng (20265640), mismo orden de
 consumo del generador — las capas 1-4 producen exactamente las mismas
 muestras que la .56.4 (la capa 5 nueva consume rng AL FINAL, después de
 las capas 1-4, para no desplazar la secuencia de `aire`).
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
#  LA CARGA (one-shot 5.15 s) — idéntica a la .56.4 salvo el finale
# ======================================================================
print("== RayoConcentradoCarga.ogg (v6.56.5: el pitido muere, el soplo nace) ==")
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

# --- 2. EL CORO ARMÓNICO QUE SUBE (registro medio-grave 172-688 Hz +15%)
coro = np.zeros(n)
for f0, a, sube in [(172.0, 0.16, 0.14), (258.0, 0.11, 0.16),
                    (344.0, 0.075, 0.18), (516.0, 0.042, 0.20), (688.0, 0.022, 0.22)]:
    f = f0 * (1.0 + sube * prog)
    fase = 2 * np.pi * np.cumsum(f) / SR
    vib = 1.0 + 0.004 * np.sin(2 * np.pi * 5.5 * t + f0)
    crece = (0.28 + 0.72 * prog ** 1.3)
    coro += a * crece * vib * np.sin(fase)

# --- 3. EL PULSO ACELERANDO: 1.8→4.0 latidos/s
tasa = 1.8 + 2.2 * prog ** 1.6
fase_p = 2 * np.pi * np.cumsum(tasa) / SR
pulso_env = 0.5 + 0.5 * np.cos(fase_p)
pulso = pulso_env ** 2.2
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

# --- 5. EL FINALE (LA CURA v6.56.5): todo rampa al pico y EL SOPLO DE
#     PRESIÓN — ruido de banda 1.1-3.4 kHz SIN frecuencia definida (el
#     seno puro de 1376 Hz de la .56.4 era el pitido que el usuario oyó:
#     +58 dB sobre el suelo en la cola). El rng se consume AQUÍ, al final,
#     para no desplazar la secuencia de las capas 1-4 respecto a la .56.4.
surge = 1.0 + 0.30 * final
mezcla = (grave * 0.46 + coro * 0.34 + pulso + aire * 0.9) * surge
ruidoF = rng.normal(0, 1, nn)
sosF = butter(2, [1100.0 / (SR / 2), 3400.0 / (SR / 2)], btype="band", output="sos")
presion = sosfiltfilt(sosF, ruidoF)[:n]
presion /= np.abs(presion).max() + 1e-9
mezcla += 0.16 * final ** 1.6 * presion
# normalizar a -2.0 dBFS y el corte de 60 ms (55 Hz × 3.3 ciclos — la .56.4
# cortaba en 25 ms = 1.4 ciclos = golpecillo)
mezcla = mezcla / (np.abs(mezcla).max() + 1e-9) * (10 ** (-2.0 / 20))
fade = int(SR * 0.06)
mezcla[-fade:] *= np.linspace(1, 0, fade)

# estéreo: el coro se abre un pelo hacia el final (el conjuro crece)
d = int(SR * 0.0006)
der_c = np.concatenate([coro[d:], coro[:d]])
ancho = 0.02 + 0.10 * final
izq = np.clip(mezcla - (der_c - coro) * ancho * 0.5, -1, 1)
der = np.clip(mezcla + (der_c - coro) * ancho * 0.5, -1, 1)
exportar_ogg("RayoConcentradoCarga", "Rayo Concentrado — Carga (v6.56.5, sin pitido)", izq, der)

# ======================================================================
#  VERIFICACIÓN ANTI-PITIDO (detector calibrado): un PITIDO audible es
#  un tono puro = PROMINENCIA LOCAL > 20 dB sobre el vecindario ±90 Hz
#  (sin ±12 Hz del propio pico) Y nivel absoluto > -50 dBFS. La .56.4
#  daba 1376 Hz a -37.2 dBFS / +31.2 dB de prominencia en la cola.
#  (La 1ª versión de este verificador comparaba contra la mediana de
#  toda la banda >900 Hz — una región casi vacía = piso numérico del
#  FFT — y daba 120 dB para la falda inaudible del filtro del aire.)
# ======================================================================
print("\n== verificación espectral anti-pitido (ventanas de 0.5 s) ==")
mono = (izq + der) * 0.5
W = int(0.5 * SR)
win = np.hanning(W)
peor_prom, peor_dbfs = 0.0, -999.0
for i in range(0, n - W + 1, W):
    seg = mono[i:i + W]
    f2 = np.fft.rfftfreq(W, 1 / SR)
    F2 = np.abs(np.fft.rfft(seg * win)) / (W / 4)
    dbfs2 = 20 * np.log10(F2 + 1e-12)
    m = f2 > 900
    f9, d9 = f2[m], dbfs2[m]
    if len(d9) == 0:
        continue
    pk = int(np.argmax(d9))
    fpk, dpk = f9[pk], d9[pk]
    nb = (np.abs(f9 - fpk) > 12) & (np.abs(f9 - fpk) < 90)
    nbdb = float(np.mean(d9[nb])) if nb.any() else -120.0
    prom = dpk - nbdb
    peor_prom = max(peor_prom, prom if dpk > -50 else 0.0)
    peor_dbfs = max(peor_dbfs, dpk)
    print(f"  t={i/SR:4.1f}s: pico {fpk:6.0f} Hz a {dpk:6.1f} dBFS | vecindario {nbdb:6.1f} | prominencia {prom:5.1f} dB")
print(f"\n  peor TONO con nivel audible (> -50 dBFS): {peor_prom:.1f} dB de prominencia")
print("  (un pitido real: > 20 dB — la .56.4 publicada daba +31.2 dB a -37.2 dBFS)")
if peor_prom > 20.0:
    raise SystemExit("FALLO: todavía hay un tono puro audible — NO publicar")
print("  OK: sin pitido.")
print("HECHO: RayoConcentradoCarga.ogg v6.56.5 regenerado (el loop v3 NO se toca).")
