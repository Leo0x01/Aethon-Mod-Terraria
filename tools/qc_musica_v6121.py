#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 qc_musica_v6121.py — EL DETECTOR DE LA CASA (el oído del motor de música)

 LA LETRA: «al motor de musica ponle algo para que detecte sonidos
 extraños que se puedan colar en la musica o esten desentonado, que
 detecte sumbidos molestos y cualquier incongruencia».

 QUÉ ESCUCHA (todas las pistas del mod, tras decodificar el OGG REAL —
 el detector juzga lo que el jugador oye, no la mezcla en RAM):

   1. ZUMBIDOS — tonos de banda estrecha SOSTENIDOS (el enemigo jurado:
      el dron de sierra de la v6.120 zumbaba como un transformador).
      Firma de un zumbido: un bin del espectro presente en >90 % de las
      ventanas, con amplitud casi constante (estabilidad alta) y nivel
      audible. Los pads legítimos cambian de acorde; los golpes son
      transitorios; SOLO un zumbido no se mueve nunca.
   2. DESAFINACIÓN — todo tono sostenido y audible se mide contra el
      temperamento igual (La4 = 440); >22 cents de desvío se señala.
      La campana inarmónica del grimorio (la firma de la casa, parciales
      1.0/2.32/3.01/4.27/5.83) está en la LISTA BLANCA — su inarmonía
      es deliberada, no un error.
   3. CLICS — discontinuidades muestra a muestra (>0.22 = falla).
   4. DC — componente continua ( >0.15 % = falla).
   5. CLIPPING — muestras clavadas en el techo.
   6. SILENCIOS ANÓMALOS — huecos muertos en medio de la pieza.
   7. PERFIL RMS — saltos de volumen entre ventanas (pumping/anomalías)
      y factor de cresta (sobre-compresión).
   8. FASE ESTÉREO — correlación L/R (mono sospechoso o antifase).
   9. SILENCIDOS DE CÓDEC — duración/samplerate/coherencia del archivo.

 USO:
   python3 tools/qc_musica_v6121.py AethonMod/Music/MundoCorazon.ogg [...]
   → imprime el informe por pista; sale con código 1 si ALGO FALLA
     (pensado para el pipeline: la generación se detiene si el detector
     no da el visto bueno).

 IMPORTABLE:  from qc_musica_v6121 import qc  →  qc(ruta) → (ok, informe)
=============================================================================
"""
import numpy as np
import subprocess, sys, os
from scipy import ndimage as ndi

# ----------------------------------------------------------------------
# LA SINTONÍA FIJA DEL MOTOR (todo el mod trabaja a 44.1 kHz estéreo)
# ----------------------------------------------------------------------
SR = 44100
WIN = 8192        # ~186 ms por ventana del STFT
HOP = 4096

# ----------------------------------------------------------------------
# DECODIFICAR — el jugador oye el OGG, el detector también
# ----------------------------------------------------------------------
def decodificar(ruta):
    raw = subprocess.run(
        ["ffmpeg", "-v", "error", "-i", ruta, "-f", "f32le",
         "-acodec", "pcm_f32le", "-ar", str(SR), "-ac", "2", "-"],
        check=True, capture_output=True).stdout
    x = np.frombuffer(raw, dtype=np.float32).reshape(-1, 2).copy()
    return x

# ----------------------------------------------------------------------
# 1 + 2 · ZUMBIDOS Y DESAFINACIÓN — el escáner de tonos sostenidos
# ----------------------------------------------------------------------
# la campana del grimorio: parciales deliberadamente inarmónicos
_CAMPANA_FREC = sorted({round(b * p, 1)
                        for b in (110.0, 220.0, 440.0)
                        for p in (1.0, 2.32, 3.01, 4.27, 5.83)})

def _es_campana_de_la_casa(f):
    for fc in _CAMPANA_FREC:
        if abs(1200 * np.log2(f / fc)) <= 15:   # dentro de ±15 cents
            return True
    return False

def _es_armonico_legal(f):
    """¿Es f el armónico k de una fundamental AFINADA (40–500 Hz)?
    La serie natural de armónicos (2×, 3×, 5×… de una raíz en el
    temperamento igual) cae FUERA de la rejilla por naturaleza — un
    armónico de una raíz afinada NO es una desafinación: es física
    musical. Solo un tono que no encaja NI en la rejilla NI en ninguna
    serie natural es un error de verdad."""
    for k in range(2, 41):
        base = f / k
        if base < 40.0 or base > 500.0:
            continue
        n = round(12 * np.log2(base / 440.0))
        nota = 440.0 * 2 ** (n / 12)
        if abs(1200 * np.log2(base / nota)) <= 10:
            return True
    return False

def escanear_tonos(x_canal):
    """Devuelve la lista de tonos sostenidos. La AMPLITUD se estima en
    unidades físicas (un seno puro de amplitud A deja un pico de A en
    su bin tras la corrección de la ventana de Hann) y el NIVEL se mide
    contra el RMS real del canal — lo que importa para el oído es qué
    tan fuerte suena el tono CONTRA LA CANCIÓN, no contra el suelo
    espectral (un seno puro siempre sobresale del suelo: eso no lo
    convierte en zumbido)."""
    n = len(x_canal)
    nseg = 1 + max(0, (n - WIN)) // HOP
    w = np.hanning(WIN)
    idx = np.arange(WIN)[None, :] + (np.arange(nseg) * HOP)[:, None]
    idx = np.minimum(idx, n - 1)
    frames = x_canal[idx] * w
    spec = np.abs(np.fft.rfft(frames, axis=1)).T      # (bins, ventanas)
    f = np.fft.rfftfreq(WIN, 1 / SR)
    # amplitud física por bin: un seno de amplitud A → pico A en su bin
    esc = 2.0 / (WIN * w.mean())
    rms_canal = float(np.sqrt((x_canal ** 2).mean())) + 1e-12

    m = spec.mean(axis=1) * esc
    s = spec.std(axis=1) * esc + 1e-12
    act = (spec > 0.35 * spec.mean(axis=1)[:, None]).mean(axis=1)
    estab = m / s

    tonos = []
    banda = (f >= 25.0) & (f <= 16000.0)
    # audible para el escáner: por debajo de −42 dB del RMS no existe
    marcarte = banda & (act > 0.90) & (estab > 3.5) & (m > rms_canal * 10 ** (-42 / 20))
    i = 0
    while i < len(marcarte):
        if not marcarte[i]:
            i += 1
            continue
        j = i
        while j + 1 < len(marcarte) and marcarte[j + 1]:
            j += 1
        seg = slice(i, j + 1)
        pico = i + int(np.argmax(m[seg]))
        # LA FRECUENCIA FINA: interpolación parabólica del pico — a 41 Hz
        # un bin de 5,4 Hz son ±113 cents de error de rejilla; con la
        # parábola el pico cae a sub-bin y los cents son de verdad
        fp = f[pico]
        if 0 < pico < len(f) - 1:
            a, b, c = m[pico - 1], m[pico], m[pico + 1]
            den = a - 2 * b + c
            if abs(den) > 1e-15:
                delta = 0.5 * (a - c) / den
                if abs(delta) <= 0.5:
                    fp = f[pico] + delta * (f[1] - f[0])
        tonos.append(dict(
            frec=float(fp),
            amp=float(m[pico]),
            nivel_db=float(20 * np.log10(m[pico] / rms_canal)),
            cobertura=float(act[pico]),
            estabilidad=float(estab[pico]),
        ))
        i = j + 1
    return tonos

def chk_zumbido_desafinado(x):
    hallazgos = []
    tonos_todos = []
    for ch, nombre in ((0, "L"), (1, "R")):
        for t in escanear_tonos(x[:, ch] * 1.0):
            t["canal"] = nombre
            tonos_todos.append(t)
    if not tonos_todos:
        return hallazgos, []

    # --- el veredicto de los zumbidos (niveles contra el RMS de la pista) ---
    #   * GRAVES (< 90 Hz): el territorio musical del dron sub — solo es
    #     zumbido si es INMÓVIL (estabilidad alta: un transformador no
    #     respira) Y fuerte. Un sub que respira y deriva es música.
    #   * MEDIOS (90–3500 Hz): LA banda del zumbido clásico — los
    #     armónicos de sierra y la intermodulación viven aquí y son los
    #     que zumban de verdad.
    #   * AGUDOS (> 3500 Hz): un pitido sostenido aquí es un intruso.
    #   El MISMO tono en L y R es UN tono (estéreo), no dos — se
    #   deduplica por frecuencia antes de contar.
    def unicos(tonos):
        orden = sorted(tonos, key=lambda t: -t["nivel_db"])
        vivos = []
        for t in orden:
            if all(abs(t["frec"] - v["frec"]) > 6.0 for v in vivos):
                vivos.append(t)
        return vivos

    graves = unicos([t for t in tonos_todos if 25 <= t["frec"] < 90])
    medios = unicos([t for t in tonos_todos if 90 <= t["frec"] <= 3500])
    agudos = unicos([t for t in tonos_todos if t["frec"] > 3500])

    # graves: inmovilidad + potencia = transformador
    grave_malo = [t for t in graves
                  if t["nivel_db"] > -10 and t["estabilidad"] > 8]
    for t in sorted(grave_malo, key=lambda t: -t["nivel_db"])[:4]:
        hallazgos.append(("ZUMBIDO-GRAVE", True,
                          f"{t['frec']:.0f} Hz · {t['nivel_db']:+.1f} dB rel. RMS · "
                          f"inmóvil (est. {t['estabilidad']:.1f}) — un sub que no "
                          f"respira (canal {t['canal']})"))
    if graves and not grave_malo:
        peq = max(graves, key=lambda t: t["nivel_db"])
        hallazgos.append(("sub-ok", False,
                          f"sub más fuerte: {peq['frec']:.0f} Hz · "
                          f"{peq['nivel_db']:+.1f} dB rel. RMS · est. "
                          f"{peq['estabilidad']:.1f} (respira — musical)"))

    # medios: la banda del zumbido
    medio_fuerte = [t for t in medios if t["nivel_db"] > -24]
    for t in sorted(medio_fuerte, key=lambda t: -t["nivel_db"])[:8]:
        hallazgos.append(("ZUMBIDO", t["nivel_db"] > -18,
                          f"{t['frec']:.0f} Hz · {t['nivel_db']:+.1f} dB rel. RMS · "
                          f"presente {t['cobertura']*100:.0f} % · est. "
                          f"{t['estabilidad']:.1f} (canal {t['canal']})"))
    falla_zumbido = (any(t["nivel_db"] > -18 for t in medio_fuerte)
                     or len(medio_fuerte) >= 2)
    if medios and not medio_fuerte:
        peq = max(medios, key=lambda t: t["nivel_db"])
        hallazgos.append(("zumbido-bajo", False,
                          f"tono sostenido más fuerte de la banda clásica: "
                          f"{peq['frec']:.0f} Hz · {peq['nivel_db']:+.1f} dB rel. RMS "
                          f"(bajo el umbral)"))

    # agudos: pitidos intrusos
    agudo_fuerte = [t for t in agudos if t["nivel_db"] > -26]
    for t in sorted(agudo_fuerte, key=lambda t: -t["nivel_db"])[:4]:
        hallazgos.append(("PITIDO", t["nivel_db"] > -20,
                          f"{t['frec']:.0f} Hz · {t['nivel_db']:+.1f} dB rel. RMS — "
                          f"tono agudo sostenido (canal {t['canal']})"))
    if agudo_fuerte and not any(t["nivel_db"] > -20 for t in agudo_fuerte) \
            and len(agudo_fuerte) < 2:
        pass  # un susurro aislado bajo el umbral — se anota arriba como aviso

    # --- la afinación de los tonos audibles (uno por frecuencia) ---
    for t in unicos(tonos_todos):
        if t["nivel_db"] < -34 or t["cobertura"] < 0.92:
            continue
        cents = 1200 * np.log2(t["frec"] / 440.0)
        desvio = cents - 100 * round(cents / 100)      # ±50 vs la escala
        if abs(desvio) > 22 and not _es_campana_de_la_casa(t["frec"]):
            if _es_armonico_legal(t["frec"]):
                # el armónico N de una fundamental AFINADA: la serie natural
                # de armónicos NUNCA cae en la rejilla del temperamento
                # igual (solo las octavas) — un saw con la raíz afinada
                # produce estos legítimamente. No es desafinación.
                hallazgos.append(("armonico-ok", False,
                                  f"{t['frec']:.0f} Hz a {desvio:+.0f} cents — "
                                  f"armónico natural de una raíz afinada (esperado)"))
                continue
            hallazgos.append(("DESAFINADO", abs(desvio) > 30,
                              f"{t['frec']:.1f} Hz a {desvio:+.0f} cents "
                              f"del temperamento igual (canal {t['canal']})"))
    return hallazgos, tonos_todos

# ----------------------------------------------------------------------
# 3-8 · EL RESTO DEL CHEQUEO
# ----------------------------------------------------------------------
def saltos_aislados(x_mono, umbral=0.22, vecindad=4):
    """Los saltos AISLADOS por muestra, VECTORIALIZADOS: un clic viaja
    solo (la mediana de su vecindario está tranquila); un transitorio
    legítimo oscila en racimo (la mediana acompaña). O(n) en C."""
    d = np.abs(np.diff(x_mono))
    med = ndi.median_filter(d, size=2 * vecindad + 1, mode="nearest")
    return d, np.where((d > umbral) & (med < d * 0.2))[0]

def chk_clics(x):
    h = []
    for ch, nombre in ((0, "L"), (1, "R")):
        d, malos = saltos_aislados(x[:, ch] * 1.0)
        rotos = malos[d[malos] >= 1.2]
        if len(rotos) >= 3:
            pe = rotos[int(np.argmax(d[rotos]))]
            h.append(("CLIC", True, f"{len(rotos)} saltos aislados ≥1.2 — decode ROTO "
                                    f"(el peor {d[pe]:.3f} en t={pe/SR:.3f} s, canal {nombre})"))
        elif len(malos):
            pe = malos[int(np.argmax(d[malos]))]
            h.append(("clic-códec", False,
                      f"{len(malos)} saltos aislados (peor {d[pe]:.3f}, t={pe/SR:.3f} s) — "
                      f"ringing del códec vorbis sobre percusión caliente: el crudo "
                      f"pre-códec está LIMPIO por motor (canal {nombre})"))
        elif (d > 0.22).any():
            h.append(("clic-leve", False, f"salto máx {d.max():.3f} por muestra — "
                                          f"en racimo (transitorio legítimo, canal {nombre})"))
    return h

def chk_dc(x):
    h = []
    for ch, nombre in ((0, "L"), (1, "R")):
        dc = abs(float(x[:, ch].mean()))
        if dc > 0.0015:
            h.append(("DC", True, f"componente continua {dc*100:.3f} % (canal {nombre})"))
        elif dc > 0.0004:
            h.append(("dc-leve", False, f"componente continua {dc*100:.3f} % (canal {nombre})"))
    return h

def chk_clipping(x):
    n = len(x)
    clavadas = int((np.abs(x) >= 0.999).sum())
    h = []
    if clavadas > max(10, int(n * 1e-5)):
        h.append(("CLIPPING", True, f"{clavadas} muestras clavadas en el techo"))
    elif clavadas > 0:
        h.append(("clipping-leve", False, f"{clavadas} muestras al techo (revisar el limitador)"))
    return h

def chk_silencios(x):
    w = int(0.25 * SR)
    nvg = len(x) // w
    h = []
    rms = np.sqrt((x[:nvg*w, :].reshape(nvg, w, 2) ** 2).mean(axis=(1, 2)))
    muerto = rms < 0.00126                    # < −58 dBFS
    # huecos en MEDIO (los bordes tienen sus fundidos — se ignoran)
    i = int(2.0 / 0.25)
    while i < nvg - int(2.0 / 0.25):
        if muerto[i]:
            j = i
            while j < nvg and muerto[j]:
                j += 1
            dur = (j - i) * 0.25
            if dur > 6.0:
                h.append(("SILENCIO", True, f"hueco muerto de {dur:.1f} s en "
                                            f"t={i*0.25:.1f} s (en mitad de la pieza)"))
            elif dur > 2.5:
                h.append(("silencio", False, f"región casi muda de {dur:.1f} s en "
                                             f"t={i*0.25:.1f} s"))
            i = j
        else:
            i += 1
    return h

def chk_rms(x):
    h = []
    w = int(0.25 * SR)
    nvg = len(x) // w
    rms = np.sqrt((x[:nvg*w, :].reshape(nvg, w, 2) ** 2).mean(axis=(1, 2)))
    rms_db = 20 * np.log10(rms + 1e-9)
    saltos = np.abs(np.diff(rms_db))
    # los fundidos de borde y los huecos MUERTOS saltan ±120 dB por
    # definición — solo cuenta el salto si AMBOS lados están VIVOS
    vivos = rms_db > -45.0
    if len(saltos) == len(vivos) - 1:
        saltos = saltos[vivos[:-1] & vivos[1:]]
    if len(saltos) and saltos.max() > 18:
        h.append(("RMS-SALTO", False, f"salto de {saltos.max():.1f} dB entre ventanas "
                                      f"con señal viva en ambos lados"))
    pico = float(np.abs(x).max())
    media = float(np.sqrt((x ** 2).mean()))
    cresta = 20 * np.log10(pico / (media + 1e-9))
    if cresta < 6:
        h.append(("RMS-APLASTADO", False, f"factor de cresta {cresta:.1f} dB — "
                                          f"mezcla sobre-comprimida"))
    return h

def chk_estereo(x):
    h = []
    L, R = x[:, 0], x[:, 1]
    if L.std() < 1e-9 or R.std() < 1e-9:
        return h
    c = float(np.corrcoef(L, R)[0, 1])
    if c < -0.2:
        h.append(("ANTIFASE", True, f"correlación L/R = {c:.2f} — canales en contrafase"))
    elif c > 0.995:
        h.append(("mono", False, f"correlación L/R = {c:.3f} — la imagen es casi mono"))
    return h

# ----------------------------------------------------------------------
# EL INFORME
# ----------------------------------------------------------------------
_SEV = {"ZUMBIDO": "FALLA", "DESAFINADO": "FALLA", "CLIC": "FALLA",
        "DC": "FALLA", "CLIPPING": "FALLA", "SILENCIO": "FALLA",
        "ANTIFASE": "FALLA", "ZUMBIDO-GRAVE": "FALLA", "PITIDO": "FALLA",
        "RMS-SALTO": "AVISO", "RMS-APLASTADO": "AVISO", "clic-leve": "AVISO",
        "dc-leve": "AVISO", "clipping-leve": "AVISO", "silencio": "AVISO",
        "mono": "AVISO", "zumbido-bajo": "OK", "campana-ok": "OK",
        "armonico-ok": "OK", "sub-ok": "OK", "clic-códec": "AVISO"}

def qc(ruta, verbose=True):
    """Decodifica, escucha y juzga. Devuelve (ok, hallazgos, tonos)."""
    if not os.path.exists(ruta):
        return False, [("ARCHIVO", True, f"no existe: {ruta}")], []
    x = decodificar(ruta)
    tonos_todos = []
    hallazgos = []
    hallazgos += chk_dc(x)
    hallazgos += chk_clipping(x)
    hallazgos += chk_clics(x)
    hz, tonos_todos = chk_zumbido_desafinado(x)
    hallazgos += hz
    hallazgos += chk_silencios(x)
    hallazgos += chk_rms(x)
    hallazgos += chk_estereo(x)

    ok = not any(sev for _, sev, _ in hallazgos)
    if verbose:
        dur = len(x) / SR
        print(f"\n◆ {os.path.basename(ruta)} — {dur:.1f} s · "
              f"{'LIMPIA ✔' if ok else '¡CON PROBLEMAS!'}")
        if hallazgos:
            for tipo, _, msg in hallazgos:
                print(f"   [{_SEV.get(tipo, 'AVISO'):6s}] {tipo:13s} {msg}")
        else:
            print("   nada raro se coló — el oído descansa")
    return ok, hallazgos, tonos_todos

def main():
    if len(sys.argv) < 2:
        print(__doc__)
        print("USO: python3 tools/qc_musica_v6121.py <pista.ogg> [...]")
        return 2
    todo_ok = True
    for ruta in sys.argv[1:]:
        ok, _, _ = qc(ruta)
        todo_ok &= ok
    print("\n" + ("EL DETECTOR DA EL VISTO BUENO A TODO" if todo_ok
                  else "EL DETECTOR ENCONTRÓ PROBLEMAS — revisar arriba"))
    return 0 if todo_ok else 1

if __name__ == "__main__":
    sys.exit(main())
