#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 taller_pixelart.py — v6.121 — EL SHIM DE LA FUSIÓN (la letra: «une La
 Forja y El Taller — que se mencionen por separado es molesto»).

 LA HISTORIA (para qué servía cada una — la respuesta a la letra):
   · LA FORJA (v6.95, tools/taller_pixelart.py): la CADENA CORTA que
     unió los TRES RELES en un comando — pxl (LA PLUMA: el borrador IA
     pintado con la paleta del libro) + sprite-gen (EL MOLDE: cutout,
     rejilla, paleta, contorno Terraria, highlight) + pixelfx (EL
     ALMA: el efecto de píxel sobre el sprite). Nació para forjar EL
     ALTAR DE LA CAÍDA y las 20 armas dignas.
   · EL TALLER (v6.98, tools/superforja.py): LA SUPERFORJA — el
     mismo corazón AGRANDADO: la cadena corta de La Forja VIVE DENTRO
     (el comando `forjar`) rodeada de las SIETE herramientas nuevas
     de la investigación (EL PUENTE que restaura la rejilla nativa,
     EL ESPEJO que anima, EL TAMBOR del dithering, LA RAMPA del
     hue-shift, EL ATLAS, LA REFINA quirúrgica, EL OJO del QA VLM,
     EL CORTE universal) — y desde la v6.121, EL ALMA ES PIXELFX DE
     VERDAD (github.com/patch-gg/pixelfx copiado y adaptado: doble
     búfer color+luz, lienzo extendido, 17 efectos).

 LA FUSIÓN (v6.121): UNA SOLA HERRAMIENTA — **LA SUPERFORJA**
 (`tools/superforja.py`). Este archivo YA NO ES una herramienta: es
 el ALIAS DE COMPATIBILIDAD — las recetas viejas que llaman «la
 forja» o «el taller» siguen funcionando y aterrizan en la
 SUPERFORJA. Nada se pierde, todo vive en una casa.

 USO (como siempre — todo se reenvía):
   python3 tools/taller_pixelart.py --nombre x --prompt "..." \
       --ancho 36 --alto 54 --efecto fuego
     → superforja.py forjar …
   python3 tools/taller_pixelart.py --nombre x --sprite sp.png \
       --efecto eclipse
     → superforja.py alma …
=============================================================================
"""
import os
import subprocess
import sys

AQUI = os.path.dirname(os.path.abspath(__file__))
SUPERFORJA = os.path.join(AQUI, "superforja.py")


def main():
    # la carpeta de la receta vieja (forja_<n>) se respeta si no hay --dest
    extra = []
    if "--dest" not in sys.argv and "--nombre" in sys.argv:
        i = sys.argv.index("--nombre")
        if i + 1 < len(sys.argv):
            extra = ["--dest", "/tmp/forja_" + sys.argv[i + 1]]
    if "--sprite" in sys.argv:
        # EL ALMA sola (el modo «solo el efecto» de la v6.95)
        cmd = [sys.executable, SUPERFORJA, "alma"] + sys.argv[1:] + ["--tira"] + extra
    else:
        # LA CADENA CORTA (la forja de siempre — pluma → molde → alma → montaje)
        cmd = [sys.executable, SUPERFORJA, "forjar"] + sys.argv[1:] + extra
    print("EL TALLER = LA FORJA = LA SUPERFORJA (la fusión v6.121 — un solo nombre)")
    r = subprocess.run(cmd)
    sys.exit(r.returncode)


if __name__ == "__main__":
    main()
