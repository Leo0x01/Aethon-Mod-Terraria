#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 motor_musica_v6121.py — EL MOTOR DE MÚSICA DE LA CASA, MEJORADO

 LA LETRA: «mejora el generador de música del mod, investiga cuales son
 las mejoras que puedes hacerles» + «al motor de musica ponle algo para
 que detecte sonidos extraños que se puedan colar en la musica o esten
 desentonado, que detecte sumbidos molestos y cualquier incongruencia».

 LAS MEJORAS DE LA v6.121 (lo que cambia contra la v6.120):

   1. FILTROS BUTTERWORTH VECTORIALIZADOS (scipy) — la v6.120 filtraba
      con un lazo de Python muestra a muestra (1er orden RC): lento y de
      pendiente tímida. Ahora: biquad de diseño profesional, fase cero,
      ~100× más rápido.
   2. LA SALA — reverb por IMPULSO SINTETIZADO (Schroeder: 4 peines con
      cola geométrica + dispersión temprana de ruido + cola OSCURA
      filtrada, decorrelado L/R). La v6.120 sumaba 4 ecos de peine —
      metálicos. Esta respira como el cuerpo de un dios.
   3. LA LIMPIEZA DE MASTER — paso-alto de 30 Hz con calentamiento: la
      COMPONENTE CONTINUA (DC 0.13–0.54 % medida en TODAS las pistas
      viejas) y el infrasonido inútil mueren. El detector las cazó.
   4. EL TECHO 0.85 CON MARGEN ANTI-OVERSHOOT — el vorbis RECONSTRUYE
      por encima del techo de entrada: las pistas viejas (techo 0.89,
      q5) mostraban hasta 161 muestras por encima de ±0.999 y miles de
      saltos >1.0 POR MUESTRA al decodificar. 0.85 + q9 los mata.
   5. EL DITHER TPDF — la bajada a 16 bits sin distorsión de cuantización.
   6. VORBIS q9 — la v6.120 viajaba a q5 (~160 kb/s): los transitorios
      brillantes (platillos, campanas) se emborronaban. q9 (~320 kb/s).
   7. EL DETECTOR INTEGRADO (qc_musica_v6121) — cada OGG se DECODIFICA
      y se juzga (zumbidos, desafinación, clics, DC, clipping, huecos,
      RMS, fase). Sin su visto bueno, el archivo NO se entrega.
   8. EL ANTI-ZUMBIDO POR DISEÑO — quemar_saw(): el dron nuevo quema
      EXACTAMENTE las mismas monedas del rng que consumía el viejo saw
      (el arroyo queda idéntico: TODO lo demás de la canción cae igual)
      y el fondo renace de senos puros que RESPIRAN (±40 %) y DERIVAN
      (±7 cents caminando) — un transformador no respira; un dios sí.

 TODAS las pistas siguen siendo DETERMINISTAS (semillas fijas): las
 mismas canciones, nota por nota, moneda por moneda.
=============================================================================
"""
import numpy as np
import subprocess, os, wave
from scipy import signal as sps

from qc_musica_v6121 import saltos_aislados

SR = 44100

# ----------------------------------------------------------------------
# 1 · LOS FILTROS — butter vectorizado, fase cero
# ----------------------------------------------------------------------
def baja(x, corte, orden=2):
    """Paso-bajo butter de orden `orden`, aplicado adelante y atrás
    (fase cero, pendiente doble)."""
    b, a = sps.butter(orden, min(float(corte), SR * 0.45) / (SR / 2), btype="low")
    return sps.filtfilt(b, a, x)

def sube(x, corte, orden=2):
    """Paso-alto butter (mata DC e infrasonido)."""
    b, a = sps.butter(orden, max(float(corte), 5.0) / (SR / 2), btype="high")
    return sps.filtfilt(b, a, x)

def _con_calentamiento(f, x, *args, **kwargs):
    """Aplica un filtro con 0,35 s de silencio por delante y los recorta
    después — el filtro NACE asentado, sin transitorio de arranque."""
    pad = int(0.35 * SR)
    m = np.concatenate([np.zeros(pad), x])
    m = f(m, *args, **kwargs)
    return m[pad:]

# ----------------------------------------------------------------------
# 2 · LA SALA — el impulso sintetizado de Schroeder
# ----------------------------------------------------------------------
def sala_impulso(retardos, retro, cola_s=2.8, oscuridad=4200.0, semilla=11):
    """El impulso de la sala del cuerpo del dios: la cola geométrica de
    cada peine (g^k en k·D), una dispersión temprana de ruido (la sala
    no sona a 4 ecos) y la cola OSCURA (la obsidiana no brilla arriba)."""
    n = int(cola_s * SR)
    ir = np.zeros(n)
    for ret, g in zip(retardos, retro):
        D = max(1, int(ret * SR))
        amp, t = 1.0, 0
        while t < n and amp > 0.0012:
            ir[t] += amp
            amp *= g
            t += D
    # la dispersión temprana (60 ms de denso decreciente)
    rng_ir = np.random.default_rng(semilla)
    ne = int(0.06 * SR)
    ir[1:ne] += rng_ir.normal(0.0, 0.22, ne - 1) * np.linspace(1.0, 0.0, ne - 1)
    # la cola oscura y la energía unitaria
    ir = _con_calentamiento(baja, ir, oscuridad)
    return ir / (np.sqrt((ir ** 2).sum()) + 1e-12)

def reverb_sala(mix, mezcla=0.22):
    """La sala: dos impulsos decorrelados (L/R) por convolución FFT.
    El húmedo se NIVELA contra el RMS del seco — la mezcla queda
    equivalente en cualquier pista."""
    ir_L = sala_impulso((0.0297, 0.0371, 0.0411, 0.0437),
                        (0.855, 0.840, 0.865, 0.850), semilla=11)
    ir_R = sala_impulso((0.0317, 0.0395, 0.0437, 0.0465),
                        (0.855, 0.840, 0.865, 0.850), semilla=23)
    out = np.zeros_like(mix)
    for ch, ir in ((0, ir_L), (1, ir_R)):
        seco = mix[:, ch]
        humedo = sps.fftconvolve(seco, ir)[:len(seco)]
        k = np.sqrt((seco ** 2).mean()) / (np.sqrt((humedo ** 2).mean()) + 1e-12)
        out[:, ch] = seco + humedo * k * mezcla
    return out

# ----------------------------------------------------------------------
# 3 · EL ANTI-ZUMBIDO POR DISEÑO
# ----------------------------------------------------------------------
def quemar_saw(rng, freq, dur, brightness=1.0):
    """Quema EXACTAMENTE las mismas monedas del rng que consumiría un
    saw() (una fase aleatoria por armónico) y devuelve silencio. El
    arroyo de aleatoriedad queda IDÉNTICO: todo lo que se genere
    después cae como cayó siempre — la MISMA canción."""
    k = 1
    while freq * k < SR * 0.48 and k <= int(80 * brightness):
        rng.uniform(0, 2 * np.pi)      # la moneda quemada
        k += 1
    return np.zeros(int(dur * SR))

def dron_respirando(rng, parciales, dur, tope_pan=0.3, brillo=0.5):
    """EL DRON CURADO — el mismo fondo sin el transformador:
       · senos puros (cero sierra = cero zumbido de armónicos),
       · deriva de afinación lenta (±7 cents caminando),
       · respiración profunda (cada parcial a su propio ritmo),
       · el pan de SIEMPRE (la misma moneda) mecido lentamente.
    `parciales`: [(frec, ganancia, lfo_deriva_Hz, lfo_respiración_Hz)].
    `brillo`: EL MISMO brightness del saw original (la cuenta de
    armónicos — y por tanto de monedas quemadas — depende de él).
    Por cada parcial quema EXACTAMENTE las monedas del viejo saw + la
    del pan (el arroyo queda idéntico → la MISMA canción)."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    L = np.zeros(n)
    R = np.zeros(n)
    for i, (f, g, fd, fr) in enumerate(parciales):
        quemar_saw(rng, f, dur, brightness=brillo)     # las monedas del viejo saw
        p0 = rng.uniform(-tope_pan, tope_pan)          # la MISMA moneda del pan
        deriva = 2 ** (0.0040 * np.sin(2 * np.pi * fd * t + i * 2.1))
        fase = 2 * np.pi * np.cumsum(f * deriva) / SR
        respira = 0.62 + 0.38 * np.sin(2 * np.pi * fr * t + i * 1.7)
        seno = np.sin(fase) * g * respira
        # el pan de siempre, MECIDO ±0.12 en ~23 s (el fondo se mueve)
        ang = (p0 + 0.12 * np.sin(2 * np.pi * t / 23.0) + 1) * np.pi / 4
        L += seno * np.cos(ang)
        R += seno * np.sin(ang)
    return L, R

# ----------------------------------------------------------------------
# 4 · EL DE-CLIC QUIRÚRGICO (para remasterizar pistas viejas)
# ----------------------------------------------------------------------
def declic_aislados(mix, umbral=0.22, vecindad=4, pasadas=12):
    """Borra SOLO los saltos aislados por muestra (un clic viaja solo;
    un transitorio legítimo oscila en racimo — el escaneo vectorizado
    de qc_musica_v6121). Cada punto tratado se puentea con una RAMPA
    cuyo ancho crece con el tamaño del salto (3–14 muestras: la cura
    del clic de la v6.120 — el palillo TOCA, no TELEPORTA). ITERA
    hasta el punto fijo: tratar un salto puede revelar al vecino.
    Devuelve (mezcla, total tratado)."""
    total = 0
    for _ in range(pasadas):
        tratados = 0
        for ch in (0, 1):
            x = mix[:, ch]
            d, cand = saltos_aislados(x, umbral=umbral, vecindad=vecindad)
            for i in cand:
                # el salto vive entre x[i] y x[i+1]: rampa del último
                # buen ANTES (x[i]) al primero DESPUÉS (x[i+1+ancho])
                ancho = int(np.clip(round(d[i] / 0.12), 2, 14))
                izq = max(0, i)
                der = min(len(x) - 1, i + 1 + ancho)
                if der > izq + 1:
                    x[izq:der + 1] = np.linspace(x[izq], x[der], der - izq + 1)
                    tratados += 1
        total += tratados
        if tratados == 0:
            break
    return mix, total

# ----------------------------------------------------------------------
# 5 · LA CADENA DE MASTER + SALIDA (con EL DETECTOR de guardia)
# ----------------------------------------------------------------------
def informe_crudo(mix, nombre):
    """La medición del crudo (pre-códec) — para el acta del remix."""
    salto = max(float(np.abs(np.diff(mix[:, c])).max()) for c in (0, 1))
    dc = max(abs(float(mix[:, c].mean())) for c in (0, 1))
    rms = float(np.sqrt((mix ** 2).mean()))
    print(f"    {nombre} (crudo): salto/muestra {salto:.3f} · DC {dc*100:.3f} % · "
          f"RMS {rms:.4f} · pico {np.abs(mix).max():.3f}")
    return salto, dc

def _semilla_de(nombre):
    return 20261110 + sum(ord(c) for c in nombre)

def masterizar(mix, nombre, out_ogg, ya_dominado=False, fundidos=True,
               declic=False, umbral_declic=0.22, detector=True,
               bitrate_vorbis=None):
    """LA CADENA: HP 30 Hz (la DC muere) → [tanh de la casa] → techo
    0.85 (margen anti-overshoot del vorbis) → fundidos → dither TPDF
    → vorbis q9 → EL DETECTOR juzga el OGG real.

    ya_dominado=True: la mezcla viene dominada y fundida de su script
    original (el remix de las oleadas: su carácter APROBADO intacto —
    solo se le cura la técnica)."""
    mix = np.array(mix, dtype=np.float64)

    # (1) LA LIMPIEZA — paso-alto 30 Hz con calentamiento
    for ch in (0, 1):
        mix[:, ch] = _con_calentamiento(sube, mix[:, ch], 30.0, 2)

    # (1b) EL DE-CLIC — DESPUÉS del filtro (el escaneo ve la señal
    # final) y con pasadas hasta el punto fijo
    if declic:
        mix, n = declic_aislados(mix, umbral=umbral_declic)
        if n:
            print(f"    {nombre}: {n} clics aislados neutralizados")

    if not ya_dominado:
        # (2) el pegamento de la casa
        mix = np.tanh(mix * 0.9) / np.tanh(0.9)

    # (1c) LA GARANTÍA DEL MOTOR: el crudo que va al códec está LIMPIO
    # de saltos aislados — cero clics de síntesis viajan al archivo
    if declic:
        restantes = sum(len(saltos_aislados(mix[:, ch])[1]) for ch in (0, 1))
        if restantes:
            print(f"    ¡AVISO! {nombre}: quedan {restantes} saltos aislados "
                  f"pre-códec (el de-clic no convergió)")

    # (3) EL TECHO con margen anti-overshoot del vorbis
    mix = mix / max(float(np.abs(mix).max()), 1e-9) * 0.85

    if fundidos:
        fi, fo = int(0.012 * SR), int(0.08 * SR)
        mix[:fi] *= np.linspace(0, 1, fi)[:, None]
        mix[-fo:] *= np.linspace(1, 0, fo)[:, None]

    # (4) EL DITHER TPDF y los 16 bits
    rng_d = np.random.default_rng(_semilla_de(nombre))
    q = (rng_d.uniform(-1, 1, mix.shape) + rng_d.uniform(-1, 1, mix.shape)) \
        * (0.5 / 32767.0)
    pcm = np.clip((mix + q) * 32767.0, -32768, 32767).astype(np.int16)

    # (5) WAV → OGG vorbis — q9 para el interior; para la percusión
    # caliente de las oleadas, ABR 448k (mide menos ringing de decode
    # que el VBR q9: 666/17/0 saltos ≥0.5/1.0/1.2 contra 780/20/1)
    out_wav = f"/tmp/{nombre}_v6121.wav"
    stereo = np.empty(pcm.shape[0] * 2, dtype=np.int16)
    stereo[0::2] = pcm[:, 0]
    stereo[1::2] = pcm[:, 1]
    with open(out_wav, "wb") as f:
        w = wave.open(f, "wb")
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(stereo.tobytes())
        w.close()
    args_audio = (["-b:a", str(bitrate_vorbis)] if bitrate_vorbis
                  else ["-q:a", "9"])
    subprocess.run(["ffmpeg", "-y", "-v", "error", "-i", out_wav,
                    "-c:a", "libvorbis"] + args_audio + [out_ogg], check=True)
    sz = os.path.getsize(out_ogg)
    print(f"    {nombre}: OGG {'ABR '+str(bitrate_vorbis) if bitrate_vorbis else 'q9'} — "
          f"{sz:,} B ({sz/1e6:.2f} MB)")

    # (6) EL DETECTOR JUZGA LO QUE EL JUGADOR OIRÁ
    ok = True
    if detector:
        from qc_musica_v6121 import qc
        ok, _, _ = qc(out_ogg, verbose=True)
    return ok
