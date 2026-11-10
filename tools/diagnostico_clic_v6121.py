#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
diagnostico_clic_v6121.py — ¿EL ANTI-ALIAS DE 19 kHz MATA EL RING DEL VORBIS?

El crudo pre-códec queda LIMPIO (0 aislados a cualquier umbral tras el
de-clic); los saltos que quedan los FABRICA el vorbis reconstruyendo
ruido de platillos cerca de Nyquist. Se prueba: LP anti-alias 19 kHz y
18 kHz antes del q9 — el aire inaudible >19 kHz se va, el ringing con él.
"""
import numpy as np
import sys, os, subprocess, wave

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__))))
from motor_musica_v6121 import sube, baja, _con_calentamiento, declic_aislados
from remix_festin_v6121 import renderizar_original, contar_aislados

CACHE = "/tmp/epica_master_v6121.npy"


def a_ogg(mix, nombre, calidad):
    pcm = (np.clip(mix, -1, 1) * 32767).astype(np.int16)
    out_wav = f"/tmp/diag_{nombre}.wav"
    stereo = np.empty(pcm.shape[0] * 2, dtype=np.int16)
    stereo[0::2] = pcm[:, 0]; stereo[1::2] = pcm[:, 1]
    with open(out_wav, "wb") as f:
        w = wave.open(f, "wb")
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(44100)
        w.writeframes(stereo.tobytes()); w.close()
    out_ogg = f"/tmp/diag_{nombre}.ogg"
    subprocess.run(["ffmpeg", "-y", "-v", "error", "-i", out_wav,
                    "-c:a", "libvorbis", "-q:a", str(calidad), out_ogg], check=True)
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", out_ogg, "-f", "f32le",
                          "-acodec", "pcm_f32le", "-ar", "44100", "-ac", "2", "-"],
                         check=True, capture_output=True).stdout
    return np.frombuffer(raw, dtype=np.float32).reshape(-1, 2).copy()


def perfil(x, etiqueta):
    print(f"  {etiqueta}:")
    for u in (0.22, 0.50, 1.0):
        print(f"      aislados ≥ {u:.2f}: {contar_aislados(x, umbral=u)}")
    print(f"      clipped (≥0.999): {int((np.abs(x) >= 0.999).sum())} · "
          f"pico {np.abs(x).max():.3f}")


if __name__ == "__main__" and os.path.exists(CACHE):
    mix = np.load(CACHE)
    print("La Épica (cacheada, ya HP30 + de-clic + techo 0.85):")
else:
    mix = renderizar_original("tools/gen_musica_festin2_v688.py",
                              'out_wav = "/tmp/oleada_hambre_epica_v688.wav"')
    print("La Épica, crudo de la v6.88 (render fresco):")
    for ch in (0, 1):
        mix[:, ch] = _con_calentamiento(sube, mix[:, ch], 30.0, 2)
    mix, n = declic_aislados(mix)
    print(f"  tras HP30 + de-clic: {n} tratados · "
          f"aislados restantes ≥0.22: {contar_aislados(mix)}")
    mix = mix / max(float(np.abs(mix).max()), 1e-9) * 0.85
    np.save(CACHE, mix)

perfil(mix, "(a) PRE-códec, techo 0.85")
perfil(a_ogg(mix, "epica_q9", 9), "(b) OGG q9 SIN anti-alias")
aa19 = np.array(mix, dtype=np.float64)
for ch in (0, 1):
    aa19[:, ch] = baja(aa19[:, ch], 19000.0, 2)
perfil(a_ogg(aa19, "epica_aa19", 9), "(e) OGG q9 + LP 19 kHz anti-alias")
aa18 = np.array(mix, dtype=np.float64)
for ch in (0, 1):
    aa18[:, ch] = baja(aa18[:, ch], 18000.0, 2)
perfil(a_ogg(aa18, "epica_aa18", 9), "(f) OGG q9 + LP 18 kHz anti-alias")
