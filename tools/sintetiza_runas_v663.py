#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.63 — LAS TRES VOCES RÚNICAS DE LA CASA (la cura del «no se escucha bien»)

LA LETRA: «el sonido nuevo de los agujeros negros runicos y de los soles
runicos no se escucha bien y ademas todavia sigue apareciendo el original
de terraria, debes mejorar los sonidos de cuando aparecen los agujeros
negros runicos y los soles runicos».

LA CURA, EN TRES PIEZAS (numpy → WAV 44,1 kHz estéreo → OGG vorbis):
  · RunaNaceSol    — EL SOL ENCIENDE (1,50 s): campana inarmónica C5 +
                     el ascenso (260→1040 Hz) + aire + chispas + quinta
                     cálida; eco estéreo 130/260 ms. Pico −1,5 dB.
  · RunaNaceVacio  — EL POZO CAE (1,90 s): el golpe seco (92→38 Hz) +
                     el descenso (130→29 Hz, saturado cálido) + temblor
                     oscuro + LA INHALACIÓN (el vacío respirando) +
                     susurro rúnico + el asentamiento. Pico −1,5 dB —
                     SIN CLIPPING (la v6.62 picaba 1,03: la distorsión
                     que se leía como «no se escucha bien»).
  · RunaCompas     — EL PULSO DEL RITUAL (0,17 s): el tic-tac propio que
                     reemplaza AL Item122 de Terraria TODO el parto
                     (660 Hz + parcial 2,76× + sub) — suave, corto, con
                     el pitch lo sube el ritual en el código.

La mezcla respeta LA FAMILIA DEL HAZ (RayoConcentradoCarga/Loop): síntesis
limpia, campanas inarmónicas, aire filtrado y graves controlados — nada
de samples, nada de Terraria.
"""
import numpy as np
import subprocess, os, wave

SR = 44100
DEST = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Sounds"
PEAK = 0.84  # −1,5 dB: NUNCA clipping


def t_axis(dur):
    return np.arange(int(SR * dur)) / SR


def fade(x, fin_s, fout_s):
    n = len(x)
    fi, fo = int(fin_s * SR), int(fout_s * SR)
    if fi > 0:
        x[:fi] *= np.linspace(0, 1, fi)
    if fo > 0:
        x[-fo:] *= np.linspace(1, 0, fo)
    return x


def chirp(f0, f1, dur, shape="lin"):
    """Seno con glide (fase integrada — sin clicks de frecuencia)."""
    t = t_axis(dur)
    if shape == "lin":
        f = f0 + (f1 - f0) * (t / dur)
    else:  # exp
        f = f0 * (f1 / f0) ** (t / dur)
    ph = 2 * np.pi * np.cumsum(f) / SR
    return np.sin(ph)


def bandpass(x, lo, hi, soft=140.0):
    """Pasa-banda FFT con bordes de coseno elevado (sin ringing)."""
    X = np.fft.rfft(x)
    fr = np.fft.rfftfreq(len(x), 1 / SR)
    m = np.ones_like(fr)
    bajo = fr < lo
    m[bajo] = 0.5 * (1 + np.cos(np.pi * np.clip((lo - fr[bajo]) / soft, 0, 1)))
    alto = fr > hi
    m[alto] = 0.5 * (1 + np.cos(np.pi * np.clip((fr[alto] - hi) / soft, 0, 1)))
    return np.fft.irfft(X * m, len(x))


def eco(x, d1_ms, g1, d2_ms, g2):
    y = x.copy()
    d1, d2 = int(d1_ms * SR / 1000), int(d2_ms * SR / 1000)
    y[d1:] += g1 * x[:-d1]
    y[d2:] += g2 * x[:-d2]
    return y


def swell(t, t_sube, t_baja):
    """Envolvente subir-mesar-bajar (curvas suaves, sin esquinas)."""
    e = np.ones_like(t)
    e[t < t_sube] = t[t < t_sube] / t_sube
    e = np.where(e >= 1.0, 1.0, e)
    mask = t > t_baja
    e[mask] = np.clip(1 - (t[mask] - t_baja) / max(t[-1] - t_baja, 1e-6), 0, 1)
    return e


def ping(t0, freq, tau, dur, pan, vib=0.0, fmod=6.0):
    """Un destello sine con decaimiento exponencial y paneo (0=izq, 1=der)."""
    n = int((dur - t0) * SR)
    if n <= 0:
        return np.zeros(0), np.zeros(0)
    t = np.arange(n) / SR
    if vib > 0:
        freq_t = freq + vib * np.sin(2 * np.pi * fmod * t)
        ph = 2 * np.pi * np.cumsum(freq_t) / SR
        s = np.sin(ph)
    else:
        s = np.sin(2 * np.pi * freq * t)
    s *= np.exp(-t / tau)
    s *= fade(np.ones(n), 0.002, 0.01)
    L = s * (1.0 - pan)
    R = s * pan
    return L, R


def colocar(L, R, l, r, t0):
    """Suma un fragmento (l, r) empezando en t0 segundos."""
    i = int(t0 * SR)
    n = len(l)
    if i + n > len(L):
        n = len(L) - i
        l, r = l[:n], r[:n]
    L[i:i + n] += l[:n]
    R[i:i + n] += r[:n]


def escribir_ogg(nombre, L, R, rms_meta=0.28):
    """Normaliza al pico de la casa (−1,5 dB) y sube la SONORIDAD con un
    limitador suave tanh (busca el rms meta SIN tocar el pico — la cura
    del «no se escucha bien»: presente en el mezclador, sin distorsión)."""
    pico = max(np.abs(L).max(), np.abs(R).max())
    if pico <= 0:
        return
    L, R = L / pico, R / pico
    if rms_meta > 0:
        # búsqueda binaria de la ganancia del limitador: tanh comprime
        # los picos hacia adentro y deja que la masa suba de voz
        lo, hi = 0.5, 8.0
        for _ in range(28):
            g = 0.5 * (lo + hi)
            k = PEAK / np.tanh(g)
            mL = k * np.tanh(g * L)
            mR = k * np.tanh(g * R)
            rms = np.sqrt(0.5 * ((mL ** 2).mean() + (mR ** 2).mean()))
            if rms < rms_meta:
                lo = g
            else:
                hi = g
        L, R = mL, mR
    else:
        L, R = L * PEAK, R * PEAK
    ent = np.empty(len(L) * 2, dtype=np.int16)
    ent[0::2] = np.clip(L * 32767, -32768, 32767).astype(np.int16)
    ent[1::2] = np.clip(R * 32767, -32768, 32767).astype(np.int16)
    wav_path = f"/tmp/{nombre}.wav"
    with wave.open(wav_path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(ent.tobytes())
    ogg_path = os.path.join(DEST, f"{nombre}.ogg")
    subprocess.run(["ffmpeg", "-y", "-v", "quiet", "-i", wav_path,
                    "-c:a", "libvorbis", "-qscale:a", "5", ogg_path], check=True)
    # informe
    a = np.asarray(ent, dtype=np.float64) / 32768.0
    mono = 0.5 * (a[0::2] + a[1::2])
    print(f"  {nombre}.ogg  dur={len(L)/SR:.2f}s  pico={np.abs(a).max():.3f}  "
          f"rms={np.sqrt((mono**2).mean()):.3f}  "
          f"{os.path.getsize(ogg_path)} bytes")


rng = np.random.default_rng(20261031)


# ══════════════════════════════════════════════════════════════════
#  RUNANACESOL — EL SOL ENCIENDE (1,50 s)
# ══════════════════════════════════════════════════════════════════
def runa_nace_sol():
    DUR = 1.50
    t = t_axis(DUR)
    L = np.zeros(len(t))
    R = np.zeros(len(t))

    # ——— EL GOLPE DE ENCENDIDO: campana inarmónica C5 (el ataque del
    #      nacimiento — SE LEE el momento en que el sol aparece) ———
    f0 = 523.25
    for ratio, amp, tau in [(1.00, 1.00, 0.55), (2.00, 0.40, 0.34),
                            (2.76, 0.50, 0.46), (4.07, 0.22, 0.26),
                            (5.40, 0.17, 0.20)]:
        det = 1.0 + rng.uniform(-0.0016, 0.0016)
        L += amp * np.sin(2 * np.pi * f0 * ratio * det * t) * np.exp(-t / tau)
        R += amp * np.sin(2 * np.pi * f0 * ratio / det * t) * np.exp(-t / tau)

    # ——— EL ASCENSO: la pila armónica que sube 260→1040 Hz (el sol que
    #      se enciende y gana fuerza para salir disparado) ———
    dur_ri = 0.85
    n_ri = int(dur_ri * SR)
    t_ri = np.arange(n_ri) / SR
    f_t = 260.0 + (1040.0 - 260.0) * (t_ri / dur_ri) ** 1.6
    ph = 2 * np.pi * np.cumsum(f_t) / SR
    riser = np.zeros(n_ri)
    for k in range(1, 7):
        riser += (1.0 / k) * np.sin(k * ph)
    riser *= 0.34
    env_ri = np.sin(np.pi * np.clip(t_ri / dur_ri, 0, 1) ** 0.8) ** 2
    riser *= env_ri
    colocar(L, R, riser * 0.97, riser * 1.03, 0.0)

    # ——— EL AIRE: la bocanada del encendido (ruido filtrado) ———
    aire = bandpass(rng.standard_normal(len(t)), 900, 3200)
    aire *= swell(t, 0.30, 0.95) * 0.55
    L += aire
    R += aire

    # ——— LA QUINTA CÁLIDA: el cuerpo del sol (C5+G5+C4, temblor lento) ———
    trem = 0.80 + 0.20 * np.sin(2 * np.pi * 4.2 * t)
    cuerpo = (0.30 * np.sin(2 * np.pi * 523.25 * 1.003 * t)
              + 0.22 * np.sin(2 * np.pi * 784.86 * 0.997 * t)
              + 0.20 * np.sin(2 * np.pi * 261.63 * t))
    cuerpo *= swell(t, 0.22, 1.05) * trem
    L += cuerpo * 0.95
    R += cuerpo * 1.05

    # ——— LAS CHISPAS: destellos estérereos repartidos en el primer segundo ———
    for _ in range(7):
        t0 = rng.uniform(0.03, 0.95)
        f = rng.uniform(2200, 5400)
        pan = rng.uniform(0.15, 0.85)
        l, r = ping(t0, f, tau=0.06, dur=DUR - t0, pan=pan)
        colocar(L, R, l * rng.uniform(0.10, 0.17), r * rng.uniform(0.10, 0.17), t0)

    # ——— EL ECO DE LA CÁMARA RÚNICA ———
    L = eco(L, 130, 0.30, 260, 0.14)
    R = eco(R, 143, 0.30, 271, 0.14)

    L = fade(L, 0.003, 0.07)
    R = fade(R, 0.003, 0.07)
    return L, R


# ══════════════════════════════════════════════════════════════════
#  RUNANACEVACIO — EL POZO CAE (1,90 s)
# ══════════════════════════════════════════════════════════════════
def runa_nace_vacio():
    DUR = 1.90
    t = t_axis(DUR)
    L = np.zeros(len(t))
    R = np.zeros(len(t))

    # ——— EL GOLPE SECO: la puerta al abrirse (92→38 Hz en 140 ms + su
    #      segunda armónica) — EL ATAQUE del nacimiento, se lee claro ———
    n_g = int(0.75 * SR)
    t_g = np.arange(n_g) / SR
    golpe = chirp(92, 38, 0.14)
    golpe = np.pad(golpe, (0, n_g - len(golpe)))
    golpe *= np.exp(-t_g / 0.30)
    golpe += 0.25 * np.sin(2 * np.pi * 76 * t_g) * np.exp(-t_g / 0.12)
    golpe *= fade(np.ones(n_g), 0.003, 0.02)
    colocar(L, R, golpe, golpe, 0.0)

    # ——— EL POZO: el descenso 130→29 Hz (exp: cae y se asienta) con
    #      saturación cálida — el grave que se SIENTE ———
    dur_p = 1.05
    n_p = int(dur_p * SR)
    t_p = np.arange(n_p) / SR
    f_p = 130.0 * (29.0 / 130.0) ** (t_p / dur_p)
    ph = 2 * np.pi * np.cumsum(f_p) / SR
    pozo = np.tanh(1.6 * np.sin(ph)) * 0.85
    pozo *= swell(t_p, 0.28, 0.72)
    pozo = fade(pozo, 0.01, 0.18)
    colocar(L, R, pozo * 0.98, pozo * 1.02, 0.02)

    # ——— EL TEMBLOR OSCURO: parciales casi-disonantes de 196 Hz
    #      (parecitos desafinados que baten — la materia que tiembla) ———
    for ratio, amp in [(1.00, 0.30), (1.48, 0.22), (2.13, 0.16), (2.87, 0.10)]:
        bati = 0.80 + 0.20 * np.sin(2 * np.pi * 3.3 * t)
        s = (amp * np.sin(2 * np.pi * 196 * ratio * 1.0035 * t)
             + amp * np.sin(2 * np.pi * 196 * ratio * 0.9965 * t))
        L += 0.5 * s * np.exp(-t / 0.95) * bati
        R += 0.5 * (amp * np.sin(2 * np.pi * 196 * ratio * 1.0050 * t)
                    + amp * np.sin(2 * np.pi * 196 * ratio * 0.9980 * t)) \
            * np.exp(-t / 0.95) * bati

    # ——— LA INHALACIÓN: el vacío RESPIRA hacia adentro (ruido con AM
    #      que sube — el aire que el agujero se traga) ———
    aliento = bandpass(rng.standard_normal(len(t)), 380, 1000)
    am = 0.55 + 0.45 * np.sin(2 * np.pi * 5.2 * t - np.pi / 2)
    aliento *= am * swell(t, 0.75, 1.45) * 0.42
    L += aliento * 0.96
    R += aliento * 1.04

    # ——— EL SUSURRO RÚNICO: tres destellos agudos, bajitos (la runa
    #      que firma la puerta) ———
    for t0, f, pan in [(0.12, 2900, 0.30), (0.50, 2350, 0.72), (0.85, 3300, 0.45)]:
        l, r = ping(t0, f, tau=0.25, dur=DUR - t0, pan=pan, vib=12.0)
        colocar(L, R, l * 0.10, r * 0.10, t0)

    # ——— EL ASENTAMIENTO: el pozo ya está (34 Hz, la nota final) ———
    n_a = int(0.95 * SR)
    t_a = np.arange(n_a) / SR
    asen = np.tanh(1.4 * np.sin(2 * np.pi * 34 * t_a)) * 0.32
    asen *= np.sin(np.pi * np.clip(t_a / 0.95, 0, 1)) ** 0.7
    colocar(L, R, asen, asen, DUR - 0.95)

    L = fade(L, 0.003, 0.13)
    R = fade(R, 0.003, 0.13)
    return L, R


# ══════════════════════════════════════════════════════════════════
#  RUNACOMPAS — EL PULSO DEL RITUAL (0,17 s)
# ══════════════════════════════════════════════════════════════════
def runa_compas():
    DUR = 0.17
    t = t_axis(DUR)
    s = (np.sin(2 * np.pi * 660 * t)
         + 0.38 * np.sin(2 * np.pi * 660 * 2.76 * t)
         + 0.30 * np.sin(2 * np.pi * 330 * t))
    s *= np.exp(-t / 0.042)
    aire = bandpass(rng.standard_normal(len(t)), 3000, 5600)
    aire *= np.exp(-t / 0.014) * 0.15
    L = fade(s * 0.97 + aire, 0.002, 0.04)
    R = fade(s * 1.03 + aire, 0.002, 0.04)
    # el compás es EDUCADO: pico a −3 dB (suena seguido)
    pico = max(np.abs(L).max(), np.abs(R).max())
    if pico > 0:
        L, R = L / pico * 0.70, R / pico * 0.70
    return L, R


if __name__ == "__main__":
    os.makedirs(DEST, exist_ok=True)
    print("v6.63 — las tres voces rúnicas:")
    escribir_ogg("RunaNaceSol", *runa_nace_sol(), rms_meta=0.27)
    escribir_ogg("RunaNaceVacio", *runa_nace_vacio(), rms_meta=0.29)
    escribir_ogg("RunaCompas", *runa_compas(), rms_meta=0.15)
    print("listo.")
