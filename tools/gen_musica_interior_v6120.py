#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 gen_musica_interior_v6120.py — LAS TRES BANDAS SONORAS EXCLUSIVAS DEL
 MUNDO INTERIOR DEL ETERNO (v6.120 — el llano de obsidiana)

 LA LETRA: «crea una banda sonora exclusiva para el mundo interior, crea
 3 banda sonoras que sonaran aleatoriamente, manten el contexto del mundo
 interior del grimorio del eterno, como crees que seria el mundo interior
 de una entidad cosmica».

 EL CONCEPTO DE LA CASA — cómo es el interior de una entidad cósmica:
 EL CUERPO DE UN DIOS QUE DEVORA MUNDOS no suena como un bioma — suena
 como TRES CARAS DEL MISMO SUEÑO. Las tres piezas comparten LA FIRMA DE
 LA CASA (la campana inarmónica del grimorio + el latido lub-dub + el
 negro-violeta de la paleta traducido a acústica: drones sub, coros con
 formantes, reverb inmensa) y cada una es UN ÓRGANO DEL CUERPO:

   1. «EL SUEÑO DEL VACÍO»    (Music/MundoSuenoVacio.ogg)  —  58 BPM
      EL SUEÑO entre mundos devorados: el dron sub del fondo, los pads
      violetas respirando en olas de ~8 s, el coro lejano con formantes,
      la campana suelta que atraviesa la nada y EL LATIDO lento (45 por
      minuto — el corazón del dios dormido). La vastedad hecha sonido:
      la pieza del llano bajo el eclipse, cuando no pasa nada — porque
      el TODO es lo que pasa.

   2. «LA GARGANTA DE PÁGINAS» (Music/MundoGarganta.ogg)    —  92 BPM
      EL TRAGADERO: los SUSURROS (ruido filtrado con formantes — las
      páginas hablando entre ellas), el arpa pizzicato en arpegios
      menores (el franela de las hojas doradas), glissandi que CAEN
      (la garganta tragando) y el ostinato grave caminando. La pieza
      del movimiento: algo baja por el cuerpo del dios — tú quizá.

   3. «EL CORAZÓN COLOSAL»     (Music/MundoCorazon.ogg)     — 120 BPM
      EL ÓRGANO QUE TODO LO MUEVE: taikos EN el latido (el kick ES el
      lub-dub), el corno épico de la casa (saw aditivo), el coro de
      CUATRO voces solemnes, los timbales de guerra y la campana
      tubular sentenciando los compases. La pieza del majestuoso: el
      corazón de amatista latiendo bajo el llano — gigantesco, vivo,
      eterno.

 LA TÉCNICA (la casa, 100% síntesis numpy — el patrón de
 gen_musica_festin_v687.py: saw/square/tri ADITIVOS limitados a Nyquist,
 lowpass, envolventes ADSR, reverb de peines, mezcla tanh + limitador,
 QC de clics por RMS de ventanas, vorbis q5 vía ffmpeg). DETERMINISTA:
 rng con semilla fija — las tres piezas se regeneran idénticas.

 USO: python3 tools/gen_musica_interior_v6120.py
=============================================================================
"""
import numpy as np
import subprocess, os, math, wave

SR = 44100
rng = np.random.default_rng(20261109)

# ----------------------------------------------------------------------
# SÍNTESIS — los mismos ladrillos aditivos de la casa (cero aliasing)
# ----------------------------------------------------------------------

def saw(freq, dur, brightness=1.0):
    t = np.arange(int(dur * SR)) / SR
    out = np.zeros_like(t)
    k = 1
    while freq * k < SR * 0.48 and k <= int(80 * brightness):
        out += np.sin(2 * np.pi * freq * k * t + rng.uniform(0, 2 * np.pi)) / k
        k += 1
    return out * (2 / np.pi)

def square_wave(freq, dur, duty=0.5):
    t = np.arange(int(dur * SR)) / SR
    out = np.zeros_like(t)
    k = 1
    while freq * k < SR * 0.48 and k <= 60:
        if k % 2 == 1:
            out += np.sin(2 * np.pi * freq * k * t) / k
        k += 1
    return out * (4 / np.pi) * duty

def tri(freq, dur):
    t = np.arange(int(dur * SR)) / SR
    out = np.zeros_like(t)
    k, s = 1, 1.0
    while freq * k < SR * 0.48 and k <= 40:
        if k % 2 == 1:
            out += s * np.sin(2 * np.pi * freq * k * t) / (k * k)
            s = -s
        k += 1
    return out * (8 / (np.pi * np.pi))

def sine(freq, dur, fase=0.0):
    t = np.arange(int(dur * SR)) / SR
    return np.sin(2 * np.pi * freq * t + fase)

def lowpass(x, cutoff, orden=2):
    cutoff = np.clip(cutoff, 20.0, SR * 0.45)
    RC = 1.0 / (2 * np.pi * cutoff)
    dt = 1.0 / SR
    y = np.empty_like(x)
    alpha = dt / (RC + dt)
    acc = 0.0
    for i in range(len(x)):
        acc += alpha * (x[i] - acc)
        y[i] = acc
    for _ in range(orden - 1):
        acc = 0.0
        for i in range(len(x)):
            acc += alpha * (y[i] - acc)
            y[i] = acc
    return y

def highpass(x, cutoff):
    return x - lowpass(x, cutoff)

def adsr(n, a=0.01, d=0.05, s=0.7, r=0.3):
    """La envolvente en MUESTRAS (a/d/r en segundos)."""
    na, nd, nr = int(a*SR), int(d*SR), int(r*SR)
    e = np.ones(n) * s
    if na > 0: e[:na] = np.linspace(0, 1, na)
    if nd > 0: e[na:na+nd] = np.linspace(1, s, nd)
    if nr > 0 and n > nr: e[-nr:] = np.linspace(e[-nr-1] if n > nr else s, 0, nr)
    return e

def reverb(x, mezcla=0.30, colas=(0.093, 0.117, 0.139, 0.163)):
    """El pozo de peines de la casa: cuatro colas primas cruzadas."""
    y = np.copy(x)
    for i, cd in enumerate(colas):
        dly = int(cd * SR)
        buf = np.copy(x)
        eco = np.zeros_like(x)
        eco[dly:] = x[:-dly] if dly < len(x) else 0
        # dos ecos decrecientes por cola
        eco2 = np.zeros_like(x)
        d2 = dly * 2
        if d2 < len(x):
            eco2[d2:] = x[:-d2] * 0.5
        y += (eco + eco2) * (mezcla / (i + 1)) * 0.9
    # pre-delays suaves
    return y * (1.0 / (1.0 + mezcla * 0.9))

def pan(x, p=0.0):
    """True stereo desde mono: constante-power + ancho de la casa."""
    ang = (p + 1) * np.pi / 4
    L = x * np.cos(ang)
    R = x * np.sin(ang)
    return L, R

def colocar(mix, x, t0, gain=1.0, p=0.0):
    """Coloca un mono en la mezcla estéreo al tiempo t0 (s)."""
    i0 = int(t0 * SR)
    n = len(x)
    if i0 >= len(mix): return
    n = min(n, len(mix) - i0)
    L, R = pan(x[:n], p)
    mix[i0:i0+n, 0] += L * gain
    mix[i0:i0+n, 1] += R * gain

def colocar_st(mix, L, R, t0, gain=1.0):
    i0 = int(t0 * SR)
    n = min(len(L), len(mix) - i0)
    if n <= 0: return
    mix[i0:i0+n, 0] += L[:n] * gain
    mix[i0:i0+n, 1] += R[:n] * gain

def latido_lubdub(dur, bpm_corazon, gain=0.5, tono=52.0):
    """EL LATIDO DE LA CASA: lub-dub (dos golpes, el segundo apagado)."""
    n = int(dur * SR)
    out = np.zeros(n)
    periodo = 60.0 / bpm_corazon
    t = 0.0
    while t < dur:
        for golpe, (f, g, dur_g) in enumerate([(tono, 1.0, 0.16), (tono*0.87, 0.55, 0.13)]):
            i0 = int((t + golpe * 0.19) * SR)
            if i0 + int(dur_g*SR) >= n: break
            tt = np.arange(int(dur_g * SR)) / SR
            # el golpe del corazón: seno grave con pitch-drop (el THUMP físico)
            fdrop = f * (1.0 + 0.35 * np.exp(-tt / 0.02))
            fase = 2 * np.pi * np.cumsum(fdrop) / SR
            golpe_i = np.sin(fase) * np.exp(-tt / 0.055) * g
            out[i0:i0+len(golpe_i)] += golpe_i
        t += periodo
    return out * gain

def campana_inarmonica(freq, dur, parciales=(1.0, 2.32, 3.01, 4.27, 5.83), gain=1.0):
    """LA CAMPANA DEL GRIMORIO — la firma inarmónica de la casa."""
    n = int(dur * SR)
    out = np.zeros(n)
    for i, p in enumerate(parciales):
        out += sine(freq * p, dur) * np.exp(-np.arange(n) / SR / (0.9 / (i + 1))) / (i + 1)
    return out * gain / len(parciales)

def susurro(dur, gain=0.3):
    """EL SUSURRO DE LAS PÁGINAS: ruido con formantes (dos bandas)."""
    n = int(dur * SR)
    r = rng.standard_normal(n) * 0.5
    r = lowpass(r, 900) * 0.7 + lowpass(r, 2600) * 0.4
    env = adsr(n, a=0.2, d=0.2, s=0.8, r=0.5)
    return r * env * gain

# ----------------------------------------------------------------------
# 1. EL SUEÑO DEL VACÍO — 58 BPM · el dios dormido entre mundos
# ----------------------------------------------------------------------
def pista_sueno(dur=118.0):
    BPM = 58.0
    n = int(dur * SR)
    mix = np.zeros((n, 2))
    print("  [1] EL SUEÑO DEL VACÍO — 58 BPM · el dron, los pads, el coro y el latido lento")

    # EL DRON DEL FONDO — el negro-violeta traducido a acústica
    base = 41.2  # E1 — el fondo del vacío
    for f, g in [(base, 0.42), (base*2, 0.20), (base*3, 0.07), (base*1.5, 0.05)]:
        dr = saw(f, dur, brightness=0.5) * g
        # el dron RESPIRA en olas de ~8 s
        olas = 0.75 + 0.25 * np.sin(np.arange(len(dr)) / SR * (2*np.pi/7.7) + f)
        dr *= olas
        L, R = pan(dr, rng.uniform(-0.3, 0.3))
        colocar_st(mix, L, R, 0.0)

    # LOS PADS VIOLETAS — acordes que nacen y mueren (el sueño gira)
    #    Em: E-G-B · Cmaj7 sin raíz: la calma del devorado
    acordes = [
        [164.81, 196.00, 246.94],          # E3-G3-B3
        [130.81, 164.81, 196.00, 246.94],  # C3-E3-G3-B3
        [110.00, 130.81, 164.81, 196.00],  # Am inferior
        [123.47, 146.83, 185.00, 220.00],  # Bm — la duda del sueño
    ]
    t = 0.0
    paso_acorde = 60.0 / BPM * 8            # un acorde por 8 tiempos (~8.3 s)
    while t < dur - 10:
        ac = acordes[int((t / paso_acorde)) % len(acordes)]
        vida = paso_acorde * 2.4             # los pads SE CRUZAN (sostienen)
        for f in ac:
            pa = saw(f, min(vida, dur - t), brightness=0.35) * 0.11
            pa *= adsr(len(pa), a=paso_acorde*0.45, d=paso_acorde*0.4, s=0.55, r=paso_acorde*0.8)
            # el vibrato del sueño (lento, ±3 cents)
            vib = 1.0 + 0.0016 * np.sin(np.arange(len(pa)) / SR * 0.9 + rng.uniform(0, 6))
            pa = lowpass(pa * vib, 1400)
            L, R = pan(pa, rng.uniform(-0.5, 0.5))
            colocar_st(mix, L, R, t)
        t += paso_acorde

    # EL CORO LEJANO — formantes «ah» sobre los acordes (la casa)
    t = paso_acorde * 2
    while t < dur - 12:
        ac = acordes[int((t / paso_acorde)) % len(acordes)]
        vida = paso_acorde * 1.6
        for f in [v * 2 for v in ac[:3]]:    # una octava arriba
            voz = saw(f, min(vida, dur - t), brightness=0.3) * 0.05
            voz *= adsr(len(voz), a=paso_acorde*0.5, d=0.1, s=0.5, r=paso_acorde*0.6)
            voz = lowpass(voz, 1100)
            L, R = pan(voz, rng.uniform(-0.6, 0.6))
            colocar_st(mix, L, R, t)
        t += paso_acorde * 2

    # LA CAMPANA SUELTA — atravesando la nada (una cada ~14 s, desafinada
    # al azar entre dos tonos — la campana del grimorio, lejana)
    t = 9.0
    while t < dur - 8:
        f = 220.0 * (2 ** rng.uniform(-0.12, 0.06))
        cam = campana_inarmonica(f, 6.0, gain=0.16)
        cam *= np.exp(-np.arange(len(cam)) / SR / 2.2)
        L, R = pan(cam, rng.uniform(-0.55, 0.55))
        colocar_st(mix, lowpass(L, 1800), lowpass(R, 1800), t)
        t += rng.uniform(11.0, 17.0)

    # EL LATIDO LENTO — 42 por minuto: el corazón del dios dormido
    lb = latido_lubdub(dur, 42.0, gain=0.55, tono=48.0)
    # un eco del latido (la cámara del pecho)
    colocar(mix, lb, 0.0, 0.8)
    colocar(mix, lb, 0.03, 0.3, p=0.4)

    # EL POLVO ESTELAR — campanitas diminutas muy arriba (las SUAS)
    t = 0.0
    while t < dur - 4:
        f = rng.uniform(1200, 2400)
        ch = sine(f, 1.2) * np.exp(-np.arange(int(1.2*SR))/SR/0.35) * 0.020
        L, R = pan(ch, rng.uniform(-0.8, 0.8))
        colocar_st(mix, L, R, t)
        t += rng.uniform(0.9, 3.2)

    return mix

# ----------------------------------------------------------------------
# 2. LA GARGANTA DE PÁGINAS — 92 BPM · el tragadero susurrante
# ----------------------------------------------------------------------
def pista_garganta(dur=122.0):
    BPM = 92.0
    beat = 60.0 / BPM
    n = int(dur * SR)
    mix = np.zeros((n, 2))
    print("  [2] LA GARGANTA DE PÁGINAS — 92 BPM · susurros, arpa pizzicato y glissandi que caen")

    # EL DRON CAMINANTE — más tenso que el sueño (la garganta viva)
    base = 46.25  # F#1
    for f, g in [(base, 0.34), (base*2, 0.16), (base*2.9966, 0.05)]:
        dr = saw(f, dur, brightness=0.45) * g
        pulso = 0.72 + 0.28 * np.sin(np.arange(len(dr)) / SR * (2*np.pi/5.1) + f)
        dr *= pulso
        L, R = pan(dr, rng.uniform(-0.25, 0.25))
        colocar_st(mix, L, R, 0.0)

    # EL OSTINATO GRAVE — el caminar por el tragadero (pizzicato de cuerdas)
    #    patrón: E-E-F#-E · B-A#-F#-E (16avos sueltos, no maquinaria)
    escala = [41.20, 46.25, 49.00, 55.00, 58.27]  # E-F#-G-A-A# (la casa oscura)
    compas = beat * 4
    t = compas * 2
    paso = beat * 0.5
    while t < dur - 8:
        for i in range(8):
            if rng.random() < 0.66:
                f = escala[int(rng.integers(0, len(escala)))]
                if i in (0, 4): f = escala[0]     # los tiempos fuertes, la raíz
                pl = tri(f, 0.28) * np.exp(-np.arange(int(0.28*SR))/SR/0.07) * 0.30
                pl = lowpass(pl, 700)
                L, R = pan(pl, rng.uniform(-0.4, 0.4))
                colocar_st(mix, L, R, t + i * paso + rng.uniform(-0.008, 0.008))
        t += compas

    # EL ARPA DE PÁGINAS — arpegios pizzicato brillantes (las hojas doradas)
    #    progresión: Em · Cmaj7 · Am · Bm (la misma voz del sueño, despierta)
    arpegios = [
        [164.81, 196.00, 246.94, 329.63],   # E-G-B-E
        [130.81, 164.81, 196.00, 246.94],   # C-E-G-B
        [110.00, 130.81, 164.81, 220.00],   # A-C-E-A
        [123.47, 146.83, 185.00, 246.94],   # B-D-F#-B
    ]
    t = compas
    while t < dur - 10:
        arp = arpegios[int((t / compas)) % 4]
        for vuelta in range(2):
            for k, f in enumerate(arp):
                if rng.random() < 0.8:
                    nota = sine(f * (1 if vuelta else 2), 0.5)
                    nota *= np.exp(-np.arange(len(nota))/SR/0.16) * 0.14
                    nota = highpass(lowpass(nota, 2600), 300)
                    L, R = pan(nota, rng.uniform(-0.65, 0.65))
                    colocar_st(mix, L, R, t + (vuelta*4 + k) * (beat/2) + rng.uniform(-0.006, 0.006))
        t += compas * 2

    # LOS SUSURROS — las páginas hablando (formantes de ruido, cortos)
    t = 6.0
    while t < dur - 6:
        su = susurro(rng.uniform(0.7, 1.8), gain=0.16)
        L, R = pan(su, rng.uniform(-0.75, 0.75))
        colocar_st(mix, L, R, t)
        t += rng.uniform(2.2, 5.5)

    # LOS GLISSANDI QUE CAEN — la garganta tragando (cada ~9 s)
    t = 12.0
    while t < dur - 8:
        f0 = rng.uniform(700, 1100)
        dur_g = rng.uniform(1.2, 2.2)
        nn = int(dur_g * SR)
        tt = np.arange(nn) / SR
        fdrop = f0 * np.exp(-tt / (dur_g * 0.55))
        fase = 2 * np.pi * np.cumsum(fdrop) / SR
        gl = np.sin(fase) * np.exp(-tt / (dur_g * 0.45)) * 0.10
        gl = lowpass(gl, 1600)
        L, R = pan(gl, rng.uniform(-0.5, 0.5))
        colocar_st(mix, L, R, t)
        t += rng.uniform(7.5, 12.0)

    # LA CAMPANA sentencia cada 4 compases (más presente que en el sueño)
    t = compas * 4
    while t < dur - 10:
        cam = campana_inarmonica(220.0, 5.0, gain=0.13)
        L, R = pan(cam, rng.uniform(-0.2, 0.2))
        colocar_st(mix, lowpass(L, 2000), lowpass(R, 2000), t)
        t += compas * 4

    # EL LATIDO — 56/minuto (más vivo que el sueño, menos que el corazón)
    lb = latido_lubdub(dur, 56.0, gain=0.42, tono=50.0)
    colocar(mix, lb, 0.0, 0.75)

    return mix

# ----------------------------------------------------------------------
# 3. EL CORAZÓN COLOSAL — 120 BPM · el órgano que todo lo mueve
# ----------------------------------------------------------------------
def pista_corazon(dur=126.0):
    BPM = 120.0
    beat = 60.0 / BPM
    compas = beat * 4
    n = int(dur * SR)
    mix = np.zeros((n, 2))
    print("  [3] EL CORAZÓN COLOSAL — 120 BPM · taikos en el latido, corno, coro y timbales")

    def tambor(f0, dur_t, gain):
        nn = int(dur_t * SR)
        tt = np.arange(nn) / SR
        fdrop = f0 * (1.0 + 1.4 * np.exp(-tt / 0.012))
        fase = 2 * np.pi * np.cumsum(fdrop) / SR
        cuerpo = np.sin(fase) * np.exp(-tt / 0.14)
        parche = rng.standard_normal(nn) * np.exp(-tt / 0.030) * 0.7
        # LA CURA DEL CLIC: TODO golpe de tambor nace de CERO — una rampa
        # de 1,5 ms (el palillo TOCA, no TELEPORTA: sin discontinuidad)
        rampa = np.minimum(np.arange(nn) / (0.0015 * SR), 1.0)
        return (lowpass(parche, 3000) * 0.5 + cuerpo) * rampa

    # LA ESTRUCTURA (compases): intro 4 · A 8 · B 8 · puente 4 · clímax 12 · cola 4
    def seccion(t0, t1, intensidad):
        t = t0
        while t < t1:
            # EL KICK-LATIDO: lub en 1 y 3, dub en 2.5 y 4.5 — EL LATIDO ES EL RITMO
            for off in (0.0, 2.0):
                colocar(mix, tambor(58.0, 0.4, 1.0), t + off * beat, 0.62 * intensidad)
            for off in (1.5, 3.5):
                colocar(mix, tambor(44.0, 0.3, 0.6), t + off * beat, 0.34 * intensidad)
            # LOS TAIKOS — el pulso del cuerpo (mitad de los compases)
            if rng.random() < 0.62 * intensidad + 0.2:
                for off in (0.0, 1.0, 2.0, 3.0):
                    if rng.random() < 0.7:
                        tk = tambor(rng.uniform(80, 120), 0.5, 1.0)
                        colocar(mix, tk, t + off * beat + rng.uniform(0, 0.02),
                                0.24 * intensidad, p=rng.uniform(-0.35, 0.35))
            # LOS TIMBALES de guerra — al final de cada 4 compases (el aviso)
            if int(t / compas) % 4 == 3:
                for i in range(4):
                    tb = tambor(98.0 - i * 4, 0.6, 0.8)
                    colocar(mix, tb, t + 3.0 * beat + i * (beat/4), 0.3 * intensidad,
                            p=rng.uniform(-0.5, 0.5))
            t += compas

    # EL DRON REAL — bajo ostinato en corcheas (el motor de sangre)
    escala = [41.20, 41.20, 49.00, 46.25, 41.20, 41.20, 55.00, 49.00]
    t = compas * 4
    while t < dur - compas * 4:
        for i in range(8):
            f = escala[i % 8]
            nota = saw(f, beat * 0.5, brightness=0.4) * 0.20
            nota *= adsr(len(nota), a=0.012, d=0.05, s=0.55, r=0.12)
            nota = lowpass(nota, 520)
            L, R = pan(nota, -0.18 if i % 2 else 0.18)
            colocar_st(mix, L, R, t + i * (beat/2))
        t += compas

    # EL CORNO ÉPICO — la melodía de la casa (frases de 2 compases)
    melodia = [
        (164.81, 2.0), (196.00, 1.0), (246.94, 1.0), (233.08, 2.0), (196.00, 2.0),
        (164.81, 1.5), (146.83, 0.5), (164.81, 2.0), (123.47, 4.0),
        (185.00, 2.0), (220.00, 1.0), (246.94, 1.0), (233.08, 2.0), (196.00, 2.0),
        (164.81, 2.0), (196.00, 1.0), (164.81, 1.0), (146.83, 4.0),
    ]
    t = compas * 6
    nota_i = 0
    while t < dur - compas * 6:
        # el corno toca en las secciones A/B/clímax (no en la intro)
        if t > compas * 6:
            for rep in range(2):                    # dos vocos de corno (octava)
                f = melodia[nota_i % len(melodia)][0] * (1 if rep == 0 else 2.0)
                dur_m = melodia[nota_i % len(melodia)][1] * beat
                cuerno = saw(f, min(dur_m * 1.15, dur - t), brightness=0.55) * 0.11
                cuerno *= adsr(len(cuerno), a=0.06, d=0.1, s=0.62, r=0.25)
                cuerno = lowpass(cuerno, 1900)
                L, R = pan(cuerno, -0.3 if rep == 0 else 0.3)
                colocar_st(mix, L, R, t, gain=1.0 if rep == 0 else 0.4)
        t += melodia[nota_i % len(melodia)][1] * beat
        nota_i += 1

    # EL CORO SOLEMNE — cuatro voces «ah» en los clímax (cada 8 compases)
    acordes = [
        [164.81, 196.00, 246.94],          # Em
        [130.81, 164.81, 246.94],          # C(7)
        [110.00, 164.81, 220.00],          # Am
        [123.47, 146.83, 185.00],          # Bm
    ]
    t = compas * 8
    while t < dur - compas * 4:
        ac = acordes[int(t / compas) % 4]
        vida = compas * 2
        for vi, f in enumerate(ac):
            for oct in (1.0, 2.0):
                voz = saw(f * oct, min(vida, dur - t), brightness=0.30) * 0.035
                voz *= adsr(len(voz), a=vida*0.2, d=vida*0.1, s=0.6, r=vida*0.3)
                voz = lowpass(voz, 1300)
                L, R = pan(voz, rng.uniform(-0.7, 0.7))
                colocar_st(mix, L, R, t)
        t += compas * 2

    # LA CAMPANA TUBULAR — sentencia cada 8 compases
    t = compas * 8
    while t < dur - 6:
        cam = campana_inarmonica(110.0, 8.0, gain=0.15)
        L, R = pan(cam, 0.0)
        colocar_st(mix, lowpass(L, 1500), lowpass(R, 1500), t)
        t += compas * 8

    # LAS SECCIONES RÍTMICAS (la coreografía de intensidad)
    seccion(compas * 2, compas * 10, 0.55)        # A — despierta
    seccion(compas * 10, compas * 18, 0.75)       # B — camina
    seccion(compas * 18, compas * 22, 0.30)       # puente — solo el latido
    seccion(compas * 22, compas * 34, 1.0)       # clímax — TODO
    seccion(compas * 34, dur - compas * 2, 0.42)  # cola — decae

    # EL LATIDO QUE CRUZA TODO — 60/minuto (el tempo Y el corazón)
    lb = latido_lubdub(dur, 60.0, gain=0.5, tono=46.0)
    colocar(mix, lb, 0.0, 0.9)

    return mix

# ----------------------------------------------------------------------
# MASTERIZACIÓN + SALIDA (el patrón de la casa)
# ----------------------------------------------------------------------
def masterizar(mix, nombre, out_ogg):
    mix = np.tanh(mix * 0.9) / np.tanh(0.9)
    peak = np.abs(mix).max()
    mix = mix / peak * 0.85
    fade_in = int(0.012 * SR); fade_out = int(0.08 * SR)
    mix[:fade_in] *= np.linspace(0, 1, fade_in)[:, None]
    mix[-fade_out:] *= np.linspace(1, 0, fade_out)[:, None]

    win = 2048
    rms = np.sqrt((mix[:len(mix)//win*win].reshape(-1, win, 2) ** 2).mean(axis=(1, 2)))
    diffs = np.abs(np.diff(rms))
    # EL QC DE CLICS DE VERDAD: saltos MUESTRA A MUESTRA (un clic es una
    # discontinuidad — un ataque de tambor es un transitorio LIMPIO: nace
    # de cero con su rampa). Umbral 0.22 de rango pleno por muestra.
    salto = max(np.abs(np.diff(mix[:, 0])).max(), np.abs(np.diff(mix[:, 1])).max())
    clic = salto < 0.22
    print(f"    {nombre}: dur {len(mix)/SR:.1f} s · peak {np.abs(mix).max():.2f} · "
          f"RMS {rms.mean():.4f} · salto/muestra {salto:.4f} {'OK' if clic else '¡CLIC!'}")

    out_wav = f"/tmp/{nombre}_v6120.wav"
    pcm = (mix * 32767).astype(np.int16)
    stereo = np.empty(pcm.shape[0] * 2, dtype=np.int16)
    stereo[0::2] = pcm[:, 0]; stereo[1::2] = pcm[:, 1]
    with open(out_wav, 'wb') as f:
        w = wave.open(f, 'wb')
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(stereo.tobytes())
        w.close()
    subprocess.run(["ffmpeg", "-y", "-v", "error", "-i", out_wav,
                    "-c:a", "libvorbis", "-q:a", "5", out_ogg], check=True)
    sz = os.path.getsize(out_ogg)
    print(f"    OGG: {out_ogg} — {sz:,} B ({sz/1e6:.2f} MB)")
    return clic

def main():
    print("LA FORJA v6.120 — LAS TRES BANDAS DEL MUNDO INTERIOR:")
    ok = True
    for generadora, nombre, archivo in [
        (pista_sueno,    "MundoSuenoVacio",  "AethonMod/Music/MundoSuenoVacio.ogg"),
        (pista_garganta, "MundoGarganta",   "AethonMod/Music/MundoGarganta.ogg"),
        (pista_corazon,  "MundoCorazon",    "AethonMod/Music/MundoCorazon.ogg"),
    ]:
        mix = generadora()
        # EL ANCHO DE LA CASA: el reverb de peines cruza el cuerpo entero
        for ch in range(2):
            mix[:, ch] = reverb(mix[:, ch], mezcla=0.24)
        ok &= masterizar(mix, nombre, archivo)
        del mix
    print("LISTO —" + (" TODO OK" if ok else " ¡REVISAR CLICS!"))

if __name__ == "__main__":
    main()
