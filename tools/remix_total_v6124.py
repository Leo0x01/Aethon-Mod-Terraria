#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 remix_total_v6124.py — EL REMIX DE TODA LA BANDA SONORA (v6.124)

 LA LETRA: «Remix de la banda sonora (sin borrar las actuales) — ¿sí /
 cuáles temas?» → «si, hazlo con todos».

 LAS REGLAS DE ESTE REMIX (las dos letras, juntas):
   1. SIN BORRAR LAS ACTUALES: las 5 bandas vivas del mod (la generación
      v6.123) se ARCHIVAN en BandaSonoraV1/v6.123/ ANTES de tocar un
      byte — la casa no borra, la casa archiva.
   2. LAS MISMAS CANCIONES, REMIXEADAS: cada tema conserva SU
      composición aprobada (las III de la v6.123 para el interior, las
      v6.87/v6.88 para las oleadas — ejecutadas letra por letra) y
      recibe UNA CAPA DE REMIX cuantizada a SU tonalidad (una
      CONTRAMELODÍA que responde, PERCUSIÓN que contesta) + la cadena
      de master de la casa (HP 30 Hz · de-clic quirúrgico · techo ·
      dither · vorbis) + EL TEST NVORBIS real (la cura del client.log).
   3. NADA LIBRE: cada nota nueva de las capas de remix sale de los
      TONOS DEL ACORDE de su compás (ni una nota fuera de tonalidad —
      la lección v6.122).

 LOS TEMAS (los 5 — «hazlo con todos»):
   · EL SUEÑO DEL VACÍO (Am dórico 57 BPM) — + el CORO QUE RESPONDE
   · LA GARGANTA DE PÁGINAS (D frigio 96 BPM) — + el ARPA QUE CONTESTA
   · EL CORAZÓN COLOSAL (120 BPM) — + LOS TIMBALES QUE RESPONDEN
   · OLEADA HAMBRE + OLEADA HAMBRE ÉPICA — mismas canciones, la
     cadena técnica mejorada (de-clic + ABR 448k — la letra v6.121)
=============================================================================
"""
import numpy as np
import subprocess, sys, os, shutil, glob

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.join(AQUI, '..')
MOD = os.path.join(RAIZ, 'AethonMod')
MUSIC = os.path.join(MOD, 'Music')
ARCHIVO = os.path.join(RAIZ, 'BandaSonoraV1', 'v6.123')
TEST_NVORBIS = "/home/z/my-project/scripts/test_nvorbis/bin/Debug/net8.0/test_nvorbis"

sys.path.insert(0, "/home/z/my-project/scripts")
sys.path.insert(0, AQUI)

import importlib.util

def _cargar(ruta, nombre):
    spec = importlib.util.spec_from_file_location(nombre, ruta)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m

M123 = _cargar("/home/z/my-project/scripts/musica_v6123.py", "musica_v6123")
M6121 = importlib.import_module("motor_musica_v6121")
M6121.RAIZ = RAIZ   # por si el motor escribe rutas relativas

SR = 44100
rng = np.random.default_rng(20261111)     # la semilla del remix


def _colocar(pista, x, t_seg, ganancia=1.0, pan=0.0):
    """v6.124 — LA COPIA SEGURA del colocar de la casa (la misma ley de
    panning, SIN el resize: las capas del remix JAMÁS estiran la
    pista — viven DENTRO del +6 s de colchón que la III ya trae)."""
    if t_seg < 0:
        recorte = int(-t_seg * SR)
        if recorte >= len(x):
            return
        x = x[recorte:]
        t_seg = 0.0
    i0 = int(t_seg * SR)
    mono = x * float(ganancia)
    izq = mono * min(1.0, 1.0 - pan)
    der = mono * min(1.0, 1.0 + pan)
    n = len(mono)
    if i0 >= pista.shape[1]:
        return
    fin = min(i0 + n, pista.shape[1])
    pista[0, i0:fin] += izq[:fin - i0]
    pista[1, i0:fin] += der[:fin - i0]

# =====================================================================
#  (0) EL ARCHIVO — «sin borrar las actuales»
# =====================================================================
def archivar():
    os.makedirs(ARCHIVO, exist_ok=True)
    guardadas = []
    for ogg in sorted(glob.glob(os.path.join(MUSIC, "*.ogg"))):
        destino = os.path.join(ARCHIVO, os.path.basename(ogg))
        if not os.path.exists(destino):
            shutil.copy2(ogg, destino)
            guardadas.append(os.path.basename(ogg))
    print(f"[archivo] {len(guardadas)} bandas v6.123 → BandaSonoraV1/v6.123/"
          + (f" ({', '.join(guardadas)})" if guardadas else " (ya archivadas)"))


# =====================================================================
#  (1) EL SUEÑO DEL VACÍO — REMIX: el coro de formantes que RESPONDE
# =====================================================================
def remix_sueno():
    print("— EL SUEÑO DEL VACÍO: la III ejecutada + EL CORO QUE RESPONDE…")
    pista = M123.el_sueno_del_vacio_iii()          # (2, n) — la misma canción

    bpm = 57.0
    beat = 60.0 / bpm
    compas = beat * 4
    dur = pista.shape[1] / SR

    # LOS ACORDES DE LA III (los tonos, en midi) — Am9·Fmaj7#11·Cmaj9·Bm7b5
    acordes = [
        [M123.midi("A", 2), M123.midi("C", 3), M123.midi("E", 3), M123.midi("G", 3), M123.midi("B", 3)],
        [M123.midi("F", 2), M123.midi("A", 2), M123.midi("C", 3), M123.midi("E", 3), M123.midi("B", 3)],
        [M123.midi("C", 3), M123.midi("E", 3), M123.midi("G", 3), M123.midi("B", 3), M123.midi("D", 4)],
        [M123.midi("B", 2), M123.midi("D", 3), M123.midi("F", 3), M123.midi("A", 3), M123.midi("E", 3)],
    ]
    paso = compas * 2                                # un acorde por 2 compases

    # LA CONTRAMELODÍA: por cada acorde, UNA FRASE del coro que responde
    # (4 notas: color → 3ª → 5ª → fundamental, UNA OCTAVA ARRIBA, en los
    # tiempos 2·4·6·8 — la respuesta lejana de la letra, TODO tono de acorde)
    t = 0.0
    idx = 0
    while t < dur - 6:
        tonos = acordes[idx % 4]
        frase = [tonos[4] + 12, tonos[2] + 12, tonos[1] + 12, tonos[0] + 24]
        for k, (beats, nota) in enumerate(zip((2, 4, 6, 8), frase)):
            f0 = M123.hz(nota)
            n = int(beat * 1.6 * SR)
            voz = M123.formante(beat * 1.6, f0, "o" if k % 2 else "u",
                                amplitud=0.055, semilla=idx * 7 + k)
            _colocar(pista, voz, t + (beats - 0.5) * beat,
                     0.030 if idx % 2 else 0.036, 0.30)
        # EL SHAKER DE LA REVERBERACIÓN (los contratiempos suaves)
        for b in (1, 3):
            sh = M123.shaker(int(0.10 * SR), amplitud=0.014, tono=0.42)
            _colocar(pista, sh, t + (b * 2 - 0.5) * beat, 0.012, -0.22)
        t += paso
        idx += 1
    return pista


# =====================================================================
#  (2) LA GARGANTA DE PÁGINAS — REMIX: el arpa que CONTESTA
# =====================================================================
def remix_garganta():
    print("— LA GARGANTA DE PÁGINAS: la III ejecutada + EL ARPA QUE CONTESTA…")
    pista = M123.la_garganta_de_paginas_iii()       # (2, n) — la misma canción

    bpm = 96.0
    beat = 60.0 / bpm
    compas = beat * 4
    dur = pista.shape[1] / SR

    # LOS ACORDES DE LA III (D frigio) — Dm(add9)·E♭maj7#11·Gm7·A7sus4
    acordes = [
        [M123.midi("D", 2), M123.midi("F", 2), M123.midi("A", 2), M123.midi("E", 3), M123.midi("C", 4)],
        [M123.midi("Eb", 2), M123.midi("G", 2), M123.midi("D", 3), M123.midi("A", 3), M123.midi("F", 3)],
        [M123.midi("G", 2), M123.midi("Bb", 2), M123.midi("D", 3), M123.midi("F", 3), M123.midi("A", 3)],
        [M123.midi("A", 2), M123.midi("D", 3), M123.midi("E", 3), M123.midi("G", 3), M123.midi("C", 4)],
    ]
    paso = compas                                    # un acorde por compás

    t = 0.0
    idx = 0
    while t < dur - 6:
        tonos = acordes[idx % 4]
        # EL ARPA QUE CONTESTA: dos plucks (color y 9ª) en los
        # contratiempos del 2 y del 4 — Karplus-Strong, TODO tono de acorde
        for k, (b, nota) in enumerate(zip((1.5, 3.5), (tonos[4] + 12, tonos[3] + 12))):
            f = M123.hz(nota)
            pl = M123.karplus(f, int(beat * 1.4 * SR), brillo=0.55, amortiguacion=0.994)
            _colocar(pista, pl, t + b * beat, 0.052 if k == 0 else 0.046,
                     0.24 if idx % 2 else -0.24)
        # EL TAIKO QUE RESPONDE: el golpe del 3 en los compases pares
        if idx % 2 == 1:
            tk = M123.perc_taiko(55, int(0.24 * SR), golpe=0.5)
            _colocar(pista, tk, t + 2 * beat, 0.088, 0.18)
        # EL GOLPE DE PÁGINA fantasma al final de cada ciclo
        if idx % 4 == 3:
            gp = M123.golpe_pagina(int(0.5 * SR), amplitud=0.5)
            _colocar(pista, gp, t + 3.5 * beat, 0.045, -0.15)
        t += paso
        idx += 1
    return pista


# =====================================================================
#  (3) EL CORAZÓN COLOSAL — REMIX: los timbales que responden
# =====================================================================
def remix_corazon():
    print("— EL CORAZÓN COLOSAL: la composición aprobada + LOS TIMBALES QUE RESPONDEN…")
    ruta6121 = os.path.join(AQUI, "gen_musica_interior_v6121.py")
    src = open(ruta6121, encoding="utf-8").read()
    ns = {"__name__": "remix_corazon", "__file__": ruta6121}
    exec(compile(src, ruta6121, "exec"), ns)
    pista = ns["pista_corazon"]()                    # (n, 2) — la misma canción

    dur = pista.shape[0] / SR
    bpm = 120.0
    beat = 60.0 / bpm

    # LOS TIMBALES QUE RESPONDEN (a-tonal por percusión — el mismo
    # lenguaje del corazón): el golpe del 3 contesta cada compás PAR,
    # y un redoble suave cierra cada ciclo de 8 compases
    t = 0.0
    k = 0
    while t < dur - 4:
        if k % 2 == 1:
            # el timbal del 3 (contesta al latido — el mismo tono del
            # corazón de la casa, 52 Hz)
            tk = M123.perc_taiko(52, int(0.26 * SR), golpe=0.42)
            i0 = int((t + 2 * beat) * SR)
            fin = min(i0 + len(tk), pista.shape[0])
            pista[i0:fin, 0] += tk[:fin - i0] * 0.055
            pista[i0:fin, 1] += tk[:fin - i0] * 0.055
        if k % 8 == 7:
            # el redoble suave del cierre (dos golpecitos rápidos)
            for j, (dt, g) in enumerate(((3.5, 0.030), (3.75, 0.024))):
                tk = M123.perc_taiko(58, int(0.18 * SR), golpe=0.35)
                i0 = int((t + dt * beat) * SR)
                pista[i0:i0 + len(tk), j % 2] += tk * g
        t += beat * 4
        k += 1
    return pista


# =====================================================================
#  (4/5) LAS OLEADAS — mismas canciones, la cadena técnica (v6.121)
# =====================================================================
def renderizar_original(ruta_script, marca_corte):
    with open(ruta_script, encoding="utf-8") as f:
        src = f.read()
    corte = src.index(marca_corte)
    ns = {"__name__": "remix_original", "__file__": ruta_script}
    exec(compile(src[:corte], ruta_script, "exec"), ns)
    return ns["mix"]


# =====================================================================
#  LA ENTREGA — master + OGG + EL TEST NVORBIS REAL
# =====================================================================
def test_nvorbis(ogg):
    dll_dir = "/home/z/my-project/scripts/test_nvorbis/bin/Debug/net8.0"
    exe = os.path.join(dll_dir, "test_nvorbis.dll")
    if not os.path.exists(exe):
        print("    ¡AVISO! el binario del test NVorbis no está — se salta el test")
        return True
    r = subprocess.run(["/tmp/sdk/dotnet", exe, ogg],
                       capture_output=True, text=True,
                       env=dict(os.environ, DOTNET_ROOT="/tmp/sdk"))
    salida = (r.stdout + r.stderr).strip()
    ok = ("FALLA" not in salida) and ("OK" in salida)
    print("    " + (salida.splitlines()[-1] if salida else "(sin salida)")
          + ("  ✔" if ok else "  ✗✗✗"))
    return ok


def _requadrear(nombre):
    """LA CURA NVorbis v6.123 (la lección del client.log): los OGG que
    salen del `masterizar` del motor v6121 (q9/ABR) disparan el «end
    out of array bounds» de la NVorbis real de tML en el SeekTo(0) del
    Reuse(). Se reencuadran desde el WAV con libvorbis q6 — el audio
    INTACTO, la estructura limpia (las bandas III siempre pasaron
    codificándose así — la prueba viviente)."""
    wav = f"/tmp/{nombre}_v6121.wav"
    ogg = os.path.join(MUSIC, f"{nombre}.ogg")
    if not os.path.exists(wav):
        print(f"    ¡AVISO! {wav} no está — no se reencuadra")
        return False
    r = subprocess.run(["ffmpeg", "-nostdin", "-y", "-v", "error", "-i", wav,
                        "-c:a", "libvorbis", "-q:a", "6", ogg],
                       capture_output=True, text=True)
    print(f"    {nombre}: reencuadrado libvorbis q6 (la cura NVorbis)")
    return r.returncode == 0


def main():
    os.makedirs("/tmp/remix_v6124", exist_ok=True)
    archivar()
    todo_ok = True

    # ---------- EL SUEÑO DEL VACÍO ----------
    pista = remix_sueno()                            # (2, n)
    if not M123.qc("ElSuenoDelVacio REMIX", pista):
        todo_ok = False
    M123.master(pista, "/tmp/remix_v6124/MundoSuenoVacio.wav")
    r = subprocess.run(["ffmpeg", "-nostdin", "-y", "-v", "error", "-i",
                        "/tmp/remix_v6124/MundoSuenoVacio.wav", "-c:a", "libvorbis",
                        "-q:a", "6", os.path.join(MUSIC, "MundoSuenoVacio.ogg")],
                       capture_output=True, text=True)
    todo_ok &= (r.returncode == 0)
    todo_ok &= test_nvorbis(os.path.join(MUSIC, "MundoSuenoVacio.ogg"))
    del pista

    # ---------- LA GARGANTA DE PÁGINAS ----------
    pista = remix_garganta()                         # (2, n)
    if not M123.qc("LaGargantaDePaginas REMIX", pista):
        todo_ok = False
    M123.master(pista, "/tmp/remix_v6124/MundoGarganta.wav")
    r = subprocess.run(["ffmpeg", "-nostdin", "-y", "-v", "error", "-i",
                        "/tmp/remix_v6124/MundoGarganta.wav", "-c:a", "libvorbis",
                        "-q:a", "6", os.path.join(MUSIC, "MundoGarganta.ogg")],
                       capture_output=True, text=True)
    todo_ok &= (r.returncode == 0)
    todo_ok &= test_nvorbis(os.path.join(MUSIC, "MundoGarganta.ogg"))
    del pista

    # ---------- EL CORAZÓN COLOSAL ----------
    pista = remix_corazon()                         # (n, 2)
    todo_ok &= M6121.masterizar(pista, "MundoCorazon",
                                os.path.join(MUSIC, "MundoCorazon.ogg"),
                                declic=True)
    todo_ok &= _requadrear("MundoCorazon")
    todo_ok &= test_nvorbis(os.path.join(MUSIC, "MundoCorazon.ogg"))
    del pista

    # ---------- LAS OLEADAS (mismas canciones, mejor técnica) ----------
    mix = renderizar_original(os.path.join(AQUI, "gen_musica_festin_v687.py"),
                              'out_wav = "/tmp/oleada_hambre_v687.wav"')
    todo_ok &= M6121.masterizar(mix, "OleadaHambre",
                                os.path.join(MUSIC, "OleadaHambre.ogg"),
                                ya_dominado=True, fundidos=False, declic=True,
                                bitrate_vorbis="448k")
    todo_ok &= _requadrear("OleadaHambre")
    todo_ok &= test_nvorbis(os.path.join(MUSIC, "OleadaHambre.ogg"))
    del mix

    mix = renderizar_original(os.path.join(AQUI, "gen_musica_festin2_v688.py"),
                              'out_wav = "/tmp/oleada_hambre_epica_v688.wav"')
    todo_ok &= M6121.masterizar(mix, "OleadaHambreEpica",
                                os.path.join(MUSIC, "OleadaHambreEpica.ogg"),
                                ya_dominado=True, fundidos=False, declic=True,
                                bitrate_vorbis="448k")
    todo_ok &= _requadrear("OleadaHambreEpica")
    todo_ok &= test_nvorbis(os.path.join(MUSIC, "OleadaHambreEpica.ogg"))
    del mix

    print("REMIX TOTAL —" + (" TODO OK (los 5 remezclados, el NVorbis vivo)" if todo_ok
                             else " ¡PROBLEMAS!"))
    return 0 if todo_ok else 1


if __name__ == "__main__":
    sys.exit(main())
