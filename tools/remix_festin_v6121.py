#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 remix_festin_v6121.py — EL REMIX DE LAS DOS OLEADAS (v6.121)

 LA LETRA: «luego intenta remixear las bandas sonoras existente,
 recuerda deben ser las mismas canciones solo con mejor calidad».

 LAS MISMAS CANCIONES, GARANTIZADO: este script EJECUTA el código de
 composición de la v6.87 y de la v6.88 LETRA POR LETRA (sus semillas
 fijas 20260404 y 20261102 intactas, su masterización aprobada intacta
 — tanh de la casa, sus fundidos, la sala épica de la v6.88). Lo único
 que cambia es la CAPA TÉCNICA de entrega:

   · HP 30 Hz — la DC medida (0,54 % en OleadaHambre) muere.
   · EL DE-CLIC QUIRÚRGICO — solo saltos AISLADOS por muestra (los
     transitorios legítimos vienen en racimo y no se tocan).
   · TECHO 0.85 — el vorbis q5 con techo 0,89 reconstruía POR ENCIMA
     del techo al decodificar (hasta 161 muestras clippeadas y miles
     de saltos >1,0 por muestra en la Épica); 0,85 + q9 lo mata.
   · DITHER TPDF + VORBIS q9 (antes q5).
   · EL DETECTOR (qc_musica_v6121) juzga cada OGG real — sin su visto
     bueno, el archivo no se entrega.
=============================================================================
"""
import numpy as np
import sys, os, gc

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from motor_musica_v6121 import masterizar, informe_crudo
from qc_musica_v6121 import saltos_aislados


def renderizar_original(ruta_script, marca_corte):
    """Ejecuta el script original hasta justo antes de escribir su WAV:
    la composición Y su masterización aprobada quedan intactas."""
    with open(ruta_script, encoding="utf-8") as f:
        src = f.read()
    corte = src.index(marca_corte)
    ns = {"__name__": "remix_original", "__file__": ruta_script}
    exec(compile(src[:corte], ruta_script, "exec"), ns)
    return ns["mix"]


def contar_aislados(x, umbral=0.22, vecindad=4):
    """Cuenta los saltos aislados por muestra (lo que el detector
    considera CLIC) — la medición del crudo, antes del códec."""
    return sum(len(saltos_aislados(x[:, ch] * 1.0, umbral=umbral,
                                   vecindad=vecindad)[1]) for ch in (0, 1))


def main():
    todo_ok = True

    # ---------- 1 · OLEADA HAMBRE (la v6.87, «EL FESTÍN DESATADO») ----------
    print("[1] OLEADA HAMBRE — la v6.87 ejecutada letra por letra (semilla 20260404):")
    mix = renderizar_original("tools/gen_musica_festin_v687.py",
                              'out_wav = "/tmp/oleada_hambre_v687.wav"')
    informe_crudo(mix, "OleadaHambre (antes)")
    n_aisl = contar_aislados(mix)
    print(f"    saltos aislados en el CRUDO: {n_aisl} "
          f"({'el de-clic los neutraliza' if n_aisl else 'limpio de nacimiento'})")
    todo_ok &= masterizar(mix, "OleadaHambre", "AethonMod/Music/OleadaHambre.ogg",
                          ya_dominado=True, fundidos=False, declic=True,
                          bitrate_vorbis="448k")
    del mix
    gc.collect()

    # ---------- 2 · OLEADA HAMBRE ÉPICA (la v6.88, «LA OLEADA FINAL») ----------
    print("[2] OLEADA HAMBRE ÉPICA — la v6.88 ejecutada letra por letra (semilla 20261102):")
    mix = renderizar_original("tools/gen_musica_festin2_v688.py",
                              'out_wav = "/tmp/oleada_hambre_epica_v688.wav"')
    informe_crudo(mix, "OleadaHambreEpica (antes)")
    n_aisl = contar_aislados(mix)
    print(f"    saltos aislados en el CRUDO: {n_aisl} "
          f"({'el de-clic los neutraliza' if n_aisl else 'limpio de nacimiento'})")
    todo_ok &= masterizar(mix, "OleadaHambreEpica", "AethonMod/Music/OleadaHambreEpica.ogg",
                          ya_dominado=True, fundidos=False, declic=True,
                          bitrate_vorbis="448k")
    del mix
    gc.collect()

    print("LISTO —" + (" TODO OK, EL DETECTOR DA EL VISTO BUENO" if todo_ok
                       else " ¡EL DETECTOR ENCONTRÓ PROBLEMAS!"))
    return 0 if todo_ok else 1


if __name__ == "__main__":
    sys.exit(main())
