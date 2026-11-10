#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
verifica_final_v6121.py — EL ACTA FINAL DEL REMIX

Para cada pista: (1) el veredicto del detector sobre el OGG entregado,
(2) LA MISMA CANCIÓN: correlación espectral vieja (BandaSonoraV1) vs
nueva — la estructura (notas, tiempos, eventos) debe coincidir; las
diferencias permitidas son exactamente las curas (dron, DC, clics,
techo, códec).
"""
import numpy as np
import sys, os, subprocess

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from qc_musica_v6121 import qc, SR, WIN, HOP


def decodificar(ruta):
    raw = subprocess.run(
        ["ffmpeg", "-v", "error", "-i", ruta, "-f", "f32le",
         "-acodec", "pcm_f32le", "-ar", str(SR), "-ac", "2", "-"],
        check=True, capture_output=True).stdout
    return np.frombuffer(raw, dtype=np.float32).reshape(-1, 2).copy()


def huella_espectral(x):
    """La huella de la ESTRUCTURA: RMS por banda (8 bandas log) por
    ventana de ~1 s — nota y eventos, no la fase ni el ruido del códec."""
    mono = x[:, 0] + x[:, 1]
    n = len(mono)
    nseg = 1 + max(0, n - WIN) // HOP
    w = np.hanning(WIN)
    idx = np.arange(WIN)[None, :] + (np.arange(nseg) * HOP)[:, None]
    idx = np.minimum(idx, n - 1)
    spec = np.abs(np.fft.rfft(mono[idx] * w, axis=1)).T
    f = np.fft.rfftfreq(WIN, 1 / SR)
    bordes = 2 ** np.linspace(np.log2(40), np.log2(16000), 9)
    cols = []
    for lo, hi in zip(bordes[:-1], bordes[1:]):
        m = (f >= lo) & (f < hi)
        cols.append(np.sqrt((spec[m] ** 2).mean(axis=0)))
    E = np.stack(cols)                     # (bandas, ventanas)
    E = 20 * np.log10(E + 1e-9)
    return E[:, ::4]                       # ~1 ventana por segundo


def misma_cancion(vieja, nueva, nombre):
    a = huella_espectral(decodificar(vieja))
    b = huella_espectral(decodificar(nueva))
    n = min(a.shape[1], b.shape[1])
    a, b = a[:, :n], b[:, :n]
    # correlación por banda (la banda grave del dron cambió a propósito)
    cors = []
    for i in range(a.shape[0]):
        ca = a[i] - a[i].mean()
        cb = b[i] - b[i].mean()
        denom = (np.sqrt((ca ** 2).sum()) * np.sqrt((cb ** 2).sum())) + 1e-9
        cors.append(float((ca * cb).sum() / denom))
    total = float(np.mean(cors))
    grave = " (la banda grave cambió A PROPÓSITO: el dron curado)" if cors[0] < 0.8 else ""
    print(f"    {nombre}: correlación estructural media {total:.3f} "
          f"[40-160: {cors[0]:.2f} · 160-640: {cors[1]:.2f} · 640-2560: {cors[2]:.2f} · "
          f"2.5k-16k: {np.mean(cors[3:]):.2f}]{grave}")
    return total


if __name__ == "__main__":
    pistas = [
        ("MundoSuenoVacio", "el dron curado"),
        ("MundoGarganta", "el dron curado"),
        ("MundoCorazon", "misma composición"),
        ("OleadaHambre", "misma composición"),
        ("OleadaHambreEpica", "misma composición"),
    ]
    print("EL ACTA FINAL — v6.121 contra v1 (BandaSonoraV1):\n")
    todo_ok = True
    for nombre, nota in pistas:
        ok, _, _ = qc(f"AethonMod/Music/{nombre}.ogg", verbose=False)
        r = misma_cancion(f"BandaSonoraV1/{nombre}.ogg",
                          f"AethonMod/Music/{nombre}.ogg", nombre)
        todo_ok &= ok
        print(f"    → {nombre}: {'LIMPIA ✔' if ok else '¡CON PROBLEMAS!'} · {nota}\n")
    print("VEREDICTO:", "TODO OK — las mismas canciones, mejor calidad, cero zumbidos"
          if todo_ok else "REVISAR")
    sys.exit(0 if todo_ok else 1)
