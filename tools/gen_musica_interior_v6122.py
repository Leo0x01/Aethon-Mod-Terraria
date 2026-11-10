#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 gen_musica_interior_v6122.py — LAS DOS BANDAS QUE SUENAN MAL, RENACIDAS
 DIFERENTES (v6.122).

 LA LETRA: «MundoGarganta.ogg y MundoSuenoVacio.ogg siguen sonando mal,
 mejor regeneralas que sean diferentes, esta vez cuida mejor la musica».

 QUÉ MUERE (la autopsia musical de las v6.120/v6.121):
   · Las DOS eran COLCHAS DE ACORDES sin melodía — nada que cantar, nada
     que recordar: ambiente, no música.
   · Las campanas DESAFINADAS al azar (±1/4 de tono) y las chispas a
     frecuencias LIBRES (1200-240 Hz cualquiera): disonancias contra los
     acordes de abajo — el «suena mal» persistente.
   · El ostinato de la garganta tiraba NOTAS AL AZAR de una escala
     oscura (E-F#-G-A-A# con A# contra Bm: la fricción de siempre).

 QUÉ NACE (las canciones NUEVAS — diferentes, la letra lo pide):
   · EL SUEÑO DEL VACÍO II — Re menor (i–VI–III–VII: Dm9 → B♭maj7 →
     F → Cadd9), 58 BPM, con UNA MELODÍA ESCRITA de flauta (la pentatónica
     de Re menor — consonante por construcción), arpegios de arpa
     CUANTIZADOS al acorde (nada de notas libres), la campana EN
     TONALIDAD y el latido de 42 del dios dormido.
   · LA GARGANTA DE PÁGINAS II — Mi menor (i–VI–III–VII: Em → C → G →
     D), 92 BPM, con EL RITMO DE VERDAD (el pulso grave 1-3 y la
     sonaja de las páginas en los contratiempos), UN BAJO QUE CAMINA
     (root-quinta-octava-quinta, nota por tiempo), arpegios de arpa en
     corcheas CUANTIZADOS, los glissandi que CAEN A LA FUNDAMENTAL del
     acorde que viene (nada de caer a ciegas) y la campana en Mi.
   · TODO CUANTIZADO A LA TONALIDAD: ni una nota libre, ni una campana
     desafinada, ni una chispa fuera de la pentatónica — LA CURA de los
     «sigue sonando mal».
   · La cadena de master del motor v6.121 (sala por impulso, paso-alto
     30 Hz, techo 0.85, dither, vorbis q9) + EL DETECTOR de guardia.

 USO: python3 tools/gen_musica_interior_v6122.py
=============================================================================
"""
import numpy as np
import sys, os

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from motor_musica_v6121 import SR, baja, sube, reverb_sala, masterizar, dron_respirando
from gen_musica_interior_v6121 import (sine, adsr, lowpass, highpass, pan,
                                        colocar, colocar_st)

rng = np.random.default_rng(20261110)     # semilla NUEVA — canciones nuevas

# ----------------------------------------------------------------------
#  LOS LADRILLOS DE LA CASA (los de siempre + las voces nuevas)
# ----------------------------------------------------------------------
def tri_f(freq, dur):
    """Triángulo suave (el bajo que camina, el arpa grave)."""
    t = np.arange(int(dur * SR)) / SR
    out = np.zeros_like(t)
    k, s = 1, 1.0
    while freq * k < SR * 0.45 and k <= 12:
        if k % 2 == 1:
            out += s * np.sin(2 * np.pi * freq * k * t) / (k * k)
            s = -s
        k += 1
    return out * (8 / (np.pi * np.pi))


def flauta(freq, dur, vibrato=0.0025):
    """LA VOZ DE FLAUTA (la melodía nueva): seno + su segunda al 35 %,
    vibrato lento de ±2,5 cents (el aire del sueño)."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    vib = 1.0 + vibrato * np.sin(2 * np.pi * 4.7 * t + rng.uniform(0, 6))
    fase = 2 * np.pi * freq * np.cumsum(vib) / SR
    out = np.sin(fase) + 0.35 * np.sin(2 * fase)
    return out * adsr(n, a=0.10, d=0.08, s=0.82, r=min(0.35, dur * 0.3))


def arpa(freq, dur=0.9, brillo=1.0):
    """LA NOTA DE ARPA: seno + 3º armónico tenue, decaimiento
    exponencial dulce (la hoja que suena y se apaga)."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    out = np.sin(2 * np.pi * freq * t) + 0.22 * brillo * np.sin(2 * np.pi * freq * 3.01 * t)
    return out * np.exp(-t / 0.34) * adsr(n, a=0.004, d=0.05, s=0.9, r=0.2)


def campana_tonal(freq, dur=5.0, gain=0.14):
    """LA CAMPANA EN TONALIDAD (la cura de la desafinada): parciales
    INARMÓNICOS suaves pero la FUNDAMENTAL EXACTA — suena a campana
    lejana SIN chocar con la armonía."""
    t = np.arange(int(dur * SR)) / SR
    out = np.zeros_like(t)
    for k, rel in enumerate((1.0, 2.0, 2.98, 4.14)):
        out += np.sin(2 * np.pi * freq * rel * t + rng.uniform(0, 6)) / (1 + k * 1.7)
    return out * np.exp(-t / 1.9) * gain


def golpe_grave(freq=54.0, dur=0.24, gain=0.30):
    """EL PULSO GRAVE del tragadero (1 y 3): seno con caída de tono —
    el paso por la garganta, no una bomba."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    f = freq * (1.0 - 0.32 * (t / dur))
    fase = 2 * np.pi * np.cumsum(f) / SR
    return np.sin(fase) * np.exp(-t / 0.075) * gain


def sonaja(dur=0.06, gain=0.045):
    """LA SONAJA DE LAS PÁGINAS (los contratiempos): ruido brevísimo
    filtrado — hojas secándose entre sí."""
    n = int(dur * SR)
    ruido = rng.standard_normal(n) * np.exp(-np.arange(n) / SR / 0.018)
    return lowpass(highpass(ruido, 2600), 7200) * gain


def colocar_frase(mix, frase, beat, t0=0.0, gain=0.16, p=0.0):
    """UNA FRASE MELODICA: [(beat_arranque, dur_beats, freq), ...]."""
    for (b, db, f) in frase:
        nota = flauta(f, db * beat) * gain
        L, R = pan(nota, p)
        colocar_st(mix, lowpass(L, 2600), lowpass(R, 2600), t0 + b * beat)


# ----------------------------------------------------------------------
#  1. EL SUEÑO DEL VACÍO II — Re menor · 58 BPM · LA NUEVA (diferente)
# ----------------------------------------------------------------------
def pista_sueno(dur=116.0):
    BPM = 58.0
    beat = 60.0 / BPM
    compas = beat * 4
    n = int(dur * SR)
    mix = np.zeros((n, 2))
    print("  [1] EL SUEÑO DEL VACÍO II — Re menor · la melodía de flauta nueva")

    # LA ARMONÍA NUEVA: i–VI–III–VII en Re menor (consonante, dulce)
    D3, F3, G3, A3 = 146.83, 174.61, 196.00, 220.00
    Bb2, C3, E3, C4 = 116.54, 130.81, 164.81, 261.63
    E4, D4, A4, C5, D5, F4, G4 = 329.63, 293.66, 440.00, 523.25, 587.33, 349.23, 392.00
    acordes = [
        ("Dm9",  [D3, F3, A3, 329.63]),        # D-F-A-E
        ("Bb7",  [Bb2, D3, F3, A3]),           # Bb-D-F-A
        ("F",    [F3 * 0.5, C3, F3, A3]),      # F-C-F-A
        ("Cadd9", [C3, E3, G3, D4]),           # C-E-G-D
    ]
    paso_acorde = compas * 2                    # un acorde por 2 compases

    # EL DRON QUE RESPIRA (la cura v6.121 — el fondo sin transformador)
    base = 36.71  # D1 — la nueva raíz del sueño
    dL, dR = dron_respirando(rng, [
        (base,       0.30, 0.045, 0.083),
        (base * 1.5, 0.055, 0.052, 0.094),     # A1 — la quinta
        (base * 2,   0.012, 0.048, 0.071),
        (base * 3,   0.004, 0.057, 0.066),
    ], dur, tope_pan=0.3)
    mix[:, 0] += dL
    mix[:, 1] += dR

    # LOS PADS — saws FILTRADOS (900 Hz: nada de zumbido), cruce suave
    t = 0.0
    idx = 0
    while t < dur - 8:
        nombre, ac = acordes[idx % 4]
        vida = paso_acorde * 1.9
        for f in ac:
            pa = sine(f, min(vida, dur - t)) * 0.10
            pa += sine(f * 2.001, min(vida, dur - t)) * 0.025   # el aire
            pa *= adsr(len(pa), a=paso_acorde * 0.42, d=paso_acorde * 0.3,
                       s=0.62, r=paso_acorde * 0.7)
            pa = lowpass(pa, 900)
            L, R = pan(pa, rng.uniform(-0.45, 0.45))
            colocar_st(mix, L, R, t)
        t += paso_acorde
        idx += 1

    # LA MELODÍA DE FLAUTA — ESCRITA (la novedad: una canción de verdad).
    #    Frase A (2 pasadas de la progresión, la 2ª una octava arriba).
    fraseA = [
        # Dm9: sube despertando
        (0.0, 2.0, A4), (2.0, 1.0, C5), (3.0, 4.0, D5), (7.5, 0.5, A4),
        # Bb7: la calma
        (8.0, 2.0, F4), (10.0, 1.0, G4), (11.0, 4.5, A4),
        # F: asciende
        (16.0, 1.5, C5), (17.5, 1.5, A4), (19.0, 4.0, G4),
        # Cadd9: resuelve y duerme
        (24.0, 1.5, E4), (25.5, 1.0, D4), (27.0, 4.5, D4),
    ]
    largo_frase = 32.0                        # en beats (8 compases)
    t0 = compas                                 # entra tras un compás
    while t0 < dur - largo_frase * beat - 4:
        pasada = int((t0 - compas) / (largo_frase * beat))
        octava = 1.0 if pasada % 2 == 0 else 2.0
        g = 0.16 if pasada % 2 == 0 else 0.10
        frase_oct = [(b, db, f * octava) for (b, db, f) in fraseA]
        colocar_frase(mix, frase_oct, beat, t0, gain=g, p=-0.12)
        t0 += largo_frase * beat

    # EL ARPA DEL SUEÑO — arpegio CUANTIZADO al acorde (una nota por
    #    2 beats, ascendente — la cajita del dios dormido)
    t = compas * 2
    idx = 1
    while t < dur - 6:
        nombre, ac = acordes[idx % 4]
        for k, f in enumerate(ac):
            nota = arpa(f * 2, 1.4) * 0.10
            L, R = pan(nota, -0.3 + 0.2 * k)
            colocar_st(mix, L, R, t + k * beat)
        idx += 1
        t += paso_acorde

    # LA CAMPANA EN TONALIDAD (A5 — la quinta de Dm: ya no desafina)
    t = 14.0
    while t < dur - 8:
        cam = campana_tonal(880.0, 6.0, gain=0.12)
        L, R = pan(cam, rng.uniform(-0.5, 0.5))
        colocar_st(mix, lowpass(L, 2400), lowpass(R, 2400), t)
        t += rng.uniform(13.0, 19.0)

    # EL LATIDO LENTO — 42/minuto (la firma del sueño, intacta)
    lb = _latido(dur, 42.0, gain=0.5, tono=49.0)
    colocar(mix, lb, 0.0, 0.8)
    colocar(mix, lb, 0.03, 0.28, p=0.4)

    # EL POLVO ESTELAR CUANTIZADO (pentatónica de Dm arriba — ni una
    #    chispa libre: la cura de las disonancias)
    penta_alta = [D5 * 2, F4 * 2, A4 * 2, C5 * 2]
    t = 2.0
    while t < dur - 4:
        f = penta_alta[int(rng.integers(0, 4))]
        ch = sine(f, 1.4) * np.exp(-np.arange(int(1.4 * SR)) / SR / 0.38) * 0.016
        L, R = pan(ch, rng.uniform(-0.75, 0.75))
        colocar_st(mix, L, R, t)
        t += rng.uniform(1.4, 3.8)

    return mix


def _latido(dur, bpm, gain=0.5, tono=48.0):
    """El latido lub-dub de la casa (el del motor, semilla nueva)."""
    n = int(dur * SR)
    out = np.zeros(n)
    periodo = 60.0 / bpm
    t = 0.0
    while t < dur - 1.2:
        for (offset, g, dur_g, f) in ((0.0, 1.0, 0.16, tono),
                                      (0.22, 0.62, 0.12, tono * 1.18)):
            nn = int(dur_g * SR)
            tt = np.arange(nn) / SR
            f_eff = f * (1.0 - 0.28 * tt / dur_g)
            fase = 2 * np.pi * np.cumsum(f_eff) / SR
            golpe = np.sin(fase) * np.exp(-tt / 0.055) * g * gain
            i0 = int((t + offset) * SR)
            if i0 + nn <= n:
                out[i0:i0 + nn] += golpe
        t += periodo
    return out


# ----------------------------------------------------------------------
#  2. LA GARGANTA DE PÁGINAS II — Mi menor · 92 BPM · LA NUEVA (diferente)
# ----------------------------------------------------------------------
def pista_garganta(dur=124.0):
    BPM = 92.0
    beat = 60.0 / BPM
    compas = beat * 4
    n = int(dur * SR)
    mix = np.zeros((n, 2))
    print("  [2] LA GARGANTA DE PÁGINAS II — Mi menor · el ritmo y el bajo que caminan")

    # LA ARMONÍA NUEVA: i–VI–III–VII en Mi menor (la épica consonante)
    E2, G2, B2, C3, D3, E3 = 82.41, 98.00, 123.47, 130.81, 146.83, 164.81
    G3, B3, C4, D4, E4, F_3, A3 = 196.00, 246.94, 261.63, 293.66, 329.63, 185.00, 220.00
    progresion = [
        ("Em", [E2, E3, G3, B3]),
        ("C",  [C3 * 0.5, C3, E3, G3]),
        ("G",  [G2, D3, G3, B3]),
        ("D",  [D3 * 0.5, D3, F_3, A3]),
    ]
    paso_acorde = compas                       # un acorde por compás (camina)

    # EL DRON QUE RESPIRA — Mi1 (la tensión del tragadero, sin zumbido)
    base = 41.20  # E1
    dL, dR = dron_respirando(rng, [
        (base,       0.27, 0.050, 0.110),
        (base * 2,   0.012, 0.044, 0.087),
        (base * 1.5, 0.006, 0.052, 0.078),
    ], dur, tope_pan=0.25, brillo=0.45)
    mix[:, 0] += dL
    mix[:, 1] += dR

    # EL RITMO DE VERDAD (la novedad): el PULSO GRAVE en 1 y 3 + LA
    #    SONAJA de las páginas en los contratiempos — el tragadero camina.
    t = compas * 2
    while t < dur - 4:
        for b in (0.0, 2.0):                   # 1 y 3
            colocar(mix, golpe_grave(52.0 + rng.uniform(-1.5, 1.5)),
                    t + b * beat, 0.9, p=rng.uniform(-0.1, 0.1))
        for b in (1.0, 3.0):                   # los contratiempos
            su = sonaja(gain=0.036)
            L, R = pan(su, rng.uniform(-0.5, 0.5))
            colocar_st(mix, L, R, t + b * beat + beat * 0.5)
        t += compas

    # EL BAJO QUE CAMINA (la novedad): root–quinta–octava–quinta, una
    #    nota por tiempo, siguiendo la progresión — el caminante hondo.
    t = compas * 2
    idx = 2
    while t < dur - 4:
        nombre, ac = progresion[idx % 4]
        raiz = ac[0]
        patron = [raiz, raiz * 1.5, raiz * 2, raiz * 1.5]
        for b, f in enumerate(patron):
            nota = tri_f(f, beat * 0.95) * 0.26
            nota *= adsr(len(nota), a=0.008, d=0.06, s=0.72, r=0.12)
            nota = lowpass(nota, 420)
            L, R = pan(nota, rng.uniform(-0.12, 0.12))
            colocar_st(mix, L, R, t + b * beat + rng.uniform(-0.004, 0.004))
        idx += 1
        t += compas

    # EL ARPA DE PÁGINAS — arpegio en CORCHEAS, CUANTIZADO al acorde
    #    (nada de notas al azar: la hoja que suena, en tonalidad)
    t = compas
    idx = 1
    while t < dur - 6:
        nombre, ac = progresion[idx % 4]
        secuencia = [ac[1], ac[2], ac[3], ac[2], ac[3], ac[1], ac[2], ac[1]]
        for k, f in enumerate(secuencia):
            if rng.random() < 0.85:
                nota = arpa(f * 2, 0.8, brillo=0.9) * 0.11
                L, R = pan(nota, -0.55 + 0.16 * k)
                colocar_st(mix, L, R, t + k * (beat / 2) + rng.uniform(-0.005, 0.005))
        idx += 1
        t += compas

    # LOS SUSURROS — las páginas hablando (más tenues que antes)
    t = 7.0
    while t < dur - 6:
        su = _susurro(rng.uniform(0.7, 1.6), gain=0.10)
        L, R = pan(su, rng.uniform(-0.7, 0.7))
        colocar_st(mix, L, R, t)
        t += rng.uniform(3.2, 6.5)

    # LOS GLISSANDI QUE CAEN A LA FUNDAMENTAL (la cura: cada caída ENDS
    #    en la raíz del acorde QUE VIENE — antes caían a ciegas y sus
    #    colas disonaban contra la armonía)
    t = compas * 4
    idx_acorde = 4
    while t < dur - 8:
        nombre_sig, ac_sig = progresion[(idx_acorde + 1) % 4]
        destino = ac_sig[0] * 2
        f0 = destino * rng.uniform(2.2, 3.4)
        dur_g = rng.uniform(1.3, 2.1)
        nn = int(dur_g * SR)
        tt = np.arange(nn) / SR
        fdrop = destino + (f0 - destino) * np.exp(-tt / (dur_g * 0.5))
        fase = 2 * np.pi * np.cumsum(fdrop) / SR
        gl = np.sin(fase) * np.exp(-tt / (dur_g * 0.55)) * 0.085
        gl = lowpass(gl, 1500)
        L, R = pan(gl, rng.uniform(-0.45, 0.45))
        colocar_st(mix, L, R, t)
        t += compas * 4
        idx_acorde += 4

    # LA CAMPANA EN Mi (la sentencia, afinada)
    t = compas * 8
    while t < dur - 10:
        cam = campana_tonal(659.26, 5.0, gain=0.11)     # E5
        L, R = pan(cam, rng.uniform(-0.2, 0.2))
        colocar_st(mix, lowpass(L, 2000), lowpass(R, 2000), t)
        t += compas * 8

    # EL LATIDO — 56/minuto (el tragadero vivo)
    lb = _latido(dur, 56.0, gain=0.40, tono=50.0)
    colocar(mix, lb, 0.0, 0.75)

    return mix


def _susurro(dur, gain=0.3):
    """El susurro de formantes de la casa (más tenue) — vectorizado con
    scipy (el resonante de dos formantes por lfilter: rápido y estable)."""
    from scipy.signal import lfilter
    n = int(dur * SR)
    ruido = rng.standard_normal(n)
    out = np.zeros(n)
    for f, g in ((500.0, 0.6), (1300.0, 0.35), (2600.0, 0.14)):
        w0 = 2 * np.pi * f / SR
        r = 0.982
        a = [1.0, -2 * r * np.cos(w0), r * r]
        out += lfilter([1.0], a, ruido) * g
    out /= (np.abs(out).max() + 1e-9)
    env = adsr(n, a=0.18 * dur, d=0.2 * dur, s=0.7, r=0.5 * dur)
    return out * env * gain * 0.7


# ----------------------------------------------------------------------
#  MAIN — la sala + la masterización + EL DETECTOR de guardia
# ----------------------------------------------------------------------
def main():
    print("LA FORJA v6.122 — LAS DOS BANDAS RENACIDAS (diferentes, cuidadas):")
    todo_ok = True
    for generadora, nombre, archivo in [
        (pista_sueno,    "MundoSuenoVacio", "AethonMod/Music/MundoSuenoVacio.ogg"),
        (pista_garganta, "MundoGarganta",   "AethonMod/Music/MundoGarganta.ogg"),
    ]:
        mix = generadora()
        mix = reverb_sala(mix, mezcla=0.22)
        todo_ok &= masterizar(mix, nombre, archivo)
        del mix
    print("LISTO —" + (" TODO OK, EL DETECTOR DA EL VISTO BUENO" if todo_ok
                       else " ¡EL DETECTOR ENCONTRÓ PROBLEMAS!"))
    return 0 if todo_ok else 1


if __name__ == "__main__":
    sys.exit(main())
