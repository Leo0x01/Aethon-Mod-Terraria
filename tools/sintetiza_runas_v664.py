#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.64 — LA CARGA SIN CAMPANA + EL SOL QUE ENCIENDE DE VERDAD

LA LETRA: «de todos los sonidos nuevos que creaste, bueno son horrible,
esa campana esta mal, crees que una campana es el sonido de nacimiento
de un sol runico y agujeros negros runicos, de hecho lo que me gusto fue
el sonido de nacimiento de los agujeros negros runicos, pero su sonido
de campana al cargar, tanto para el sol runico como para los agujeros
negros esta horrible, la campana no es el sonido correcto».

LA CURA (numpy → WAV 44,1 kHz estéreo → OGG vorbis):
  · RunaNaceSol  — EL HORNO QUE ENCIENDE (1,70 s), REESCRITO SIN
                   CAMPANA: la v6.63 era una campana inarmónica C5
                   (2,76×/4,07×/5,40× con decaimiento exponencial — el
                   timbre de campana de libro). El nuevo es EL ESPEJO
                   SOLAR del pozo que gustó (RunaNaceVacio): el golpe
                   CÁLIDO que SUBE (58→112 Hz — el inverso del 92→38
                   del pozo) + EL HORNO (42→175 Hz saturado subiendo) +
                   LA LLAMA (aire que se aviva) + EL CUERPO DORADO
                   (pila ARMÓNICA 110/220/330/440 con ataque LENTO de
                   300 ms — órgano que respira, jamás campana) + LOS
                   RAYOS (los destellos con vibrato del susurro rúnico
                   que gustó) + EL ARDOR (brasas + el asentamiento).
  · RunaCarga    — LA CARGA DEL RITUAL (2,85 s), NUEVA: el compás de
                   campanita (RunaCompas, tic-tac cada 20 ticks) MUERE
                   — una campana no es cómo CARGA un ritual. La carga
                   es UNA SOLA VOZ SOSTENIDA QUE SUBE: EL RUMBLE (44→98
                   Hz con armónicos y saturación cálida) + EL PULSO QUE
                   APRIETA (AM 3,2→8,5 Hz — la energía ganando ritmo) +
                   EL AIRE (viento filtrado creciendo) + LA CUERDA
                   (170→680 Hz ganando tono, bajito) + LAS CHISPAS
                   (granitos que escupen, más densos al final) + EL
                   VACÍO ANTES DE LA PUERTA (todo se hunde y el aire SE
                   ASPIRA hacia adentro — le entrega el turno al
                   nacimiento en silencio). El código la calza al plazo
                   del ritual con el pitch: sol 2,0 s (+0,50) · agujero
                   3,0 s (−0,06) · supremo (−0,10).
  · RunaNaceVacio — INTACTO (gustó tal cual: no se regenera).
  · RunaCompas   — BORRADO (la campana muerta no viaja en el .tmod).

La familia del haz (RayoConcentradoCarga/Loop) sigue mandando: síntesis
limpia, armónicos saturados cálidos, aire filtrado y graves con tanh —
CERO parciales inarmónicos con decaimiento exponencial (= campana) en
toda la casa.
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
    return L, R


def escribir_ogg(nombre, L, R, rms_meta=0.28):
    """Normaliza al pico de la casa (−1,5 dB) y sube la SONORIDAD con un
    limitador suave tanh (busca el rms meta SIN tocar el pico)."""
    pico = max(np.abs(L).max(), np.abs(R).max())
    if pico <= 0:
        return
    L, R = L / pico, R / pico
    if rms_meta > 0:
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


rng = np.random.default_rng(20261101)


# ══════════════════════════════════════════════════════════════════
#  RUNACARGA — LA CARGA DEL RITUAL (2,85 s sostenidos, SIN campana)
# ══════════════════════════════════════════════════════════════════
def runa_carga():
    DUR = 2.85
    t = t_axis(DUR)
    L = np.zeros(len(t))
    R = np.zeros(len(t))

    T_HUSH = 2.35          # dónde empieza EL VACÍO ANTES DE LA PUERTA
    T_SUBE = 2.30          # dónde la construcción llega a tope

    # ——— EL RUMBLE DEL ÚTERO: 44→98 Hz subiendo (armónicos 1/2/3 con
    #      saturación cálida — el grave que se SIENTE crecer) ———
    def rumble(det):
        f = 44.0 * (98.0 / 44.0) ** np.clip(t / T_HUSH, 0, 1) * det
        ph = 2 * np.pi * np.cumsum(f) / SR
        s = np.sin(ph) + 0.45 * np.sin(2 * ph) + 0.22 * np.sin(3 * ph)
        return np.tanh(1.35 * s) * 0.62

    # ——— LA ENVOLVENTE MAESTRA: entra suave (0,25 s), GANA cuerpo
    #      hasta el tope, y en EL VACÍO se hunde (×0,30) para
    #      entregarle el turno al nacimiento en silencio ———
    env = np.clip(t / 0.25, 0, 1) * (0.55 + 0.45 * np.clip(t / T_SUBE, 0, 1))
    hush = np.where(t > T_HUSH,
                    np.clip(1 - (t - T_HUSH) / 0.32, 0.30, 1), 1.0)
    env = env * hush

    # ——— EL PULSO QUE APRIETA: AM que ACELERA (3,2→8,5 Hz — la
    #      energía ganando ritmo, como un motor subiendo de vueltas;
    #      fase integrada: sin saltos) y APRIETA (profundidad 0,15→0,55) ———
    rate = 3.2 + (8.5 - 3.2) * np.clip(t / T_HUSH, 0, 1) ** 1.5
    phr = 2 * np.pi * np.cumsum(rate) / SR
    depth = 0.15 + 0.40 * np.clip(t / 2.2, 0, 1)
    am = 1.0 - depth * (0.5 + 0.5 * np.sin(phr))

    L += rumble(1.0012) * env * am
    R += rumble(0.9988) * env * am

    # ——— EL AIRE: viento filtrado que crece con el ritual (dos
    #      capas: el soplo grave + el aliento medio) ———
    aire1 = bandpass(rng.standard_normal(len(t)), 220, 800)
    aire1 *= swell(t, 0.50, T_HUSH) * (0.65 + 0.35 * am) * 0.30
    aire2 = bandpass(rng.standard_normal(len(t)), 700, 1800)
    aire2 *= swell(t, 0.90, T_HUSH) * 0.14
    L += (aire1 + aire2) * 0.97
    R += (aire1 + aire2) * 1.03

    # ——— LA CUERDA: 170→680 Hz ganando tono (bajito — el filo del
    #      hechizo afinándose) con vibrato lento ———
    dur_c = 2.30
    n_c = int(dur_c * SR)
    t_c = np.arange(n_c) / SR
    f_c = 170.0 * (680.0 / 170.0) ** (t_c / dur_c)
    f_c = f_c + 7.0 * np.sin(2 * np.pi * 5.0 * t_c)
    ph_c = 2 * np.pi * np.cumsum(f_c) / SR
    cuerda = (np.sin(ph_c) + 0.40 * np.sin(2 * ph_c))
    cuerda *= (0.30 + 0.70 * (t_c / dur_c) ** 1.3) * 0.11
    cuerda = fade(cuerda, 0.30, 0.25)
    colocar(L, R, cuerda * 0.55, cuerda * 0.45, 0.35)

    # ——— LAS CHISPAS: granitos que escupen (más densos hacia el
    #      final — la runa saturándose) ———
    for _ in range(14):
        u = rng.random() ** 1.6          # sesgo hacia el final
        t0 = 0.45 + 1.75 * u
        n_g = int(rng.uniform(0.008, 0.020) * SR)
        grano = bandpass(rng.standard_normal(n_g),
                         rng.uniform(1400, 4800), 6500, soft=900)
        pico_g = np.abs(grano).max()
        if pico_g > 0:
            grano = grano / pico_g * rng.uniform(0.05, 0.12)
        grano = fade(grano, 0.001, 0.004)
        pan = rng.uniform(0.2, 0.8)
        i0 = int(t0 * SR)
        n = min(len(grano), len(L) - i0)
        if n > 0:
            L[i0:i0 + n] += grano[:n] * (1.0 - pan)
            R[i0:i0 + n] += grano[:n] * pan

    # ——— EL VACÍO ANTES DE LA PUERTA: todo lo demás se hundió y el
    #      aire SE ASPIRA hacia adentro (ruido con banda que sube,
    #      pico a los 2,70 s y CORTE — le entrega el turno al
    #      nacimiento en silencio) ———
    n_v = int(0.42 * SR)
    t_v = np.arange(n_v) / SR
    suck = bandpass(rng.standard_normal(n_v), 500, 2400, soft=300)
    env_v = np.sin(np.pi * np.clip(t_v / 0.40, 0, 1) ** 0.7) ** 2
    env_v[t_v > 0.38] *= np.clip(1 - (t_v[t_v > 0.38] - 0.38) / 0.04, 0, 1)
    suck *= env_v * 0.34
    colocar(L, R, suck * 0.96, suck * 1.04, T_HUSH)

    L = fade(L, 0.012, 0.10)
    R = fade(R, 0.012, 0.10)
    return L, R


# ══════════════════════════════════════════════════════════════════
#  RUNANACESOL — EL HORNO QUE ENCIENDE (1,70 s, SIN campana)
#  El espejo solar del pozo que gustó (RunaNaceVacio).
# ══════════════════════════════════════════════════════════════════
def runa_nace_sol():
    DUR = 1.70
    t = t_axis(DUR)
    L = np.zeros(len(t))
    R = np.zeros(len(t))

    # ——— EL GOLPE CÁLIDO: 58→112 Hz SUBIENDO (el inverso exacto del
    #      92→38 del pozo — donde el pozo CAE, el sol SE ALZA) + su
    #      octava, ataque suave de 12 ms: se LEE el nacimiento pero es
    #      un golpe de pecho, jamás un ding ———
    n_g = int(0.60 * SR)
    t_g = np.arange(n_g) / SR
    golpe = chirp(58, 112, 0.13)
    golpe = np.pad(golpe, (0, n_g - len(golpe)))
    golpe *= np.exp(-t_g / 0.26)
    golpe += 0.30 * np.sin(2 * np.pi * 118 * t_g) * np.exp(-t_g / 0.10)
    golpe = np.tanh(1.30 * golpe) * 0.90
    golpe *= fade(np.ones(n_g), 0.012, 0.02)
    colocar(L, R, golpe, golpe, 0.0)

    # ——— EL HORNO QUE SUBE: 42→175 Hz (donde el pozo bajaba 130→29,
    #      el horno SUBE — el sol ganando masa) con armónicos y
    #      saturación cálida, y se ASIENTA en el ardor ———
    dur_p = 1.15
    n_p = int(dur_p * SR)
    t_p = np.arange(n_p) / SR
    f_p = 42.0 * (175.0 / 42.0) ** (t_p / dur_p)
    ph = 2 * np.pi * np.cumsum(f_p) / SR
    horno = np.sin(ph) + 0.50 * np.sin(2 * ph) + 0.25 * np.sin(3 * ph)
    horno = np.tanh(1.50 * horno) * 0.72
    env_h = swell(t_p, 0.20, 0.80)
    env_h *= np.where(t_p > 0.85,
                      np.clip(1 - (t_p - 0.85) / 0.55, 0.35, 1), 1)
    horno *= env_h
    horno = fade(horno, 0.02, 0.18)
    colocar(L, R, horno * 0.98, horno * 1.02, 0.02)

    # ——— LA LLAMA: la bocanada del encendido (dos capas de aire
    #      filtrado — el soplo grave y el aliento medio — con el
    #      parpadeo del fuego a 7 Hz) ———
    llama1 = bandpass(rng.standard_normal(len(t)), 300, 1000)
    flick = 0.82 + 0.18 * np.sin(2 * np.pi * 7.0 * t)
    llama1 *= swell(t, 0.12, 0.85) * flick * 0.42
    llama2 = bandpass(rng.standard_normal(len(t)), 900, 2600)
    llama2 *= swell(t, 0.30, 0.95) * 0.20
    decae = np.clip(1 - (t - 1.00) / 0.60, 0, 1)
    decae = np.where(t > 1.00, decae, 1.0)
    L += llama1 * 0.97 * decae + llama2 * decae
    R += llama1 * 1.03 * decae + llama2 * decae

    # ——— EL CUERPO DORADO: la pila ARMÓNICA de La2 (110/220/330/440 —
    #      TODAS armónicas de 110, la pila del haz) con ataque LENTO
    #      de 300 ms y temblor suave — un órgano que respira: el
    #      reemplazo de la campana muerta ———
    trem = 0.82 + 0.18 * np.sin(2 * np.pi * 4.3 * t)
    cuerpo = (0.30 * np.sin(2 * np.pi * 110 * 1.002 * t)
              + 0.26 * np.sin(2 * np.pi * 220 * 0.999 * t)
              + 0.20 * np.sin(2 * np.pi * 330 * 1.001 * t)
              + 0.16 * np.sin(2 * np.pi * 440 * 0.998 * t))
    cuerpo *= swell(t, 0.30, 1.00) * trem
    L += cuerpo * 0.96
    R += cuerpo * 1.04

    # ——— LOS RAYOS: los destellos con vibrato del susurro rúnico
    #      (los mismos que firman al pozo — la firma que gustó) ———
    for t0, f, pan in [(0.10, 2600, 0.32), (0.48, 3400, 0.70), (0.88, 4200, 0.45)]:
        l, r = ping(t0, f, tau=0.22, dur=DUR - t0, pan=pan, vib=10.0)
        colocar(L, R, l * 0.09, r * 0.09, t0)

    # ——— EL ARDOR: las brasas que escupen al despegar + el
    #      asentamiento (55 Hz, la nota del vuelo) ———
    for _ in range(9):
        t0 = rng.uniform(0.55, 1.55)
        n_g = int(rng.uniform(0.008, 0.016) * SR)
        grano = bandpass(rng.standard_normal(n_g),
                         rng.uniform(800, 2400), 5000, soft=800)
        pico_g = np.abs(grano).max()
        if pico_g > 0:
            grano = grano / pico_g * rng.uniform(0.04, 0.08)
        grano = fade(grano, 0.001, 0.003)
        pan = rng.uniform(0.25, 0.75)
        i0 = int(t0 * SR)
        n = min(len(grano), len(L) - i0)
        if n > 0:
            L[i0:i0 + n] += grano[:n] * (1.0 - pan)
            R[i0:i0 + n] += grano[:n] * pan
    n_a = int(0.85 * SR)
    t_a = np.arange(n_a) / SR
    asen = np.tanh(1.3 * np.sin(2 * np.pi * 55 * t_a)) * 0.26
    asen *= np.sin(np.pi * np.clip(t_a / 0.85, 0, 1)) ** 0.7
    colocar(L, R, asen, asen, DUR - 0.85)

    # ——— EL ECO DE LA CÁMARA RÚNICA ———
    L = eco(L, 120, 0.28, 240, 0.13)
    R = eco(R, 131, 0.28, 251, 0.13)

    L = fade(L, 0.003, 0.09)
    R = fade(R, 0.003, 0.09)
    return L, R


if __name__ == "__main__":
    os.makedirs(DEST, exist_ok=True)
    print("v6.64 — la carga sin campana + el sol que enciende:")
    escribir_ogg("RunaNaceSol", *runa_nace_sol(), rms_meta=0.27)
    escribir_ogg("RunaCarga", *runa_carga(), rms_meta=0.25)
    # LA CAMPANA MUERTA NO VIAJA: RunaCompas.ogg se borra del mod
    viejo = os.path.join(DEST, "RunaCompas.ogg")
    if os.path.exists(viejo):
        os.remove(viejo)
        print("  RunaCompas.ogg  BORRADO (la campana muerta no viaja)")
    # RunaNaceVacio queda INTACTO (gustó tal cual — no se regenera)
    print("listo.")
