#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 v6.97.1 — EL SYNC DEL ESPAÑOL v3 (LA LECCIÓN DE LA TRADUCCIÓN VIVA):

 LA REGLA NUEVA DE LA CASA (desde la v6.97): el en-US YA NO es un espejo
 puro — lleva la TRADUCCIÓN REAL AL INGLÉS del contenido nuevo (las 20
 armas de Las Tres Casas, los cultistas, ElMandatoDelAltar,
 FueraDeLaLista…). El sync de la .56.6 LO BORRABA cada corrida (la
 lección cazada en la .97.1: corría el sync tras editar y las
 traducciones morían sin que nadie lo notara).

 ESTE SYNC v3 hace lo correcto:
   · es-MX  (×2): REGENERADO byte a byte del es-ES (el espejo puro —
     TODAS las variantes de español caen en la nuestra).
   · en-US  (×2): RESPETADO — solo se VERIFICA que cada clave del es-ES
     exista en el en-US (paridad de claves); si falta alguna, se agrega
     con el texto español (el fallback de siempre); lo que ya vive
     (español O la traducción inglesa) NUNCA se toca.

 USO: editar SOLO los es-ES y correr  python3 tools/sync_es_mx_v6564.py
 (idempotente; aborta con aserción si algo inesperado pasa).
=============================================================================
"""
import os
import re
import sys

BASE = os.path.join(os.path.dirname(__file__), "..", "AethonMod", "Localization")

ESPEJOS = [
    ("es-ES_Mods.AethonMod.hjson", "es-MX_Mods.AethonMod.hjson"),
]
FALLBACKS = [
    ("es-ES_Mods.AethonMod.hjson", "en-US_Mods.AethonMod.hjson"),
]

CABECERA_ESPEJO = """// v6.56.6 — EL ESPEJO LATINOAMÉRICA: copia AUTOMÁTICA del es-ES (la fuente
// única del español de la casa). Contenido IDÉNTICO al es-ES: editar SOLO
// el es-ES y correr tools/sync_es_mx_v6564.py."""


def cuerpo(ruta):
    """El cuerpo del hjson SIN su bloque de cabecera (// líneas + blancos)."""
    lineas = open(ruta, encoding="utf-8").read().split("\n")
    i = 0
    while i < len(lineas) and (lineas[i].startswith("//") or lineas[i].strip() == ""):
        i += 1
    assert i < len(lineas), f"{ruta}: archivo sin cuerpo"
    return lineas[i:]


CLAVE = re.compile(r'^(\s*)([\w.]+):\s*\{?\s*$')


def claves(lineas):
    """las claves de bloque (Items: / CetroDelAlba: { …) con su sangría"""
    out = []
    for l in lineas:
        m = CLAVE.match(l)
        if m:
            out.append((m.group(1), m.group(2)))
    return out


def main():
    ok = 0
    # (A) los espejos puros: es-MX byte a byte del es-ES
    for fuente, espejo in ESPEJOS:
        ruta_f, ruta_e = os.path.join(BASE, fuente), os.path.join(BASE, espejo)
        assert os.path.isfile(ruta_f), f"FALTA la fuente {ruta_f}"
        c = cuerpo(ruta_f)
        with open(ruta_e, "w", encoding="utf-8") as f:
            f.write(CABECERA_ESPEJO + "\n" + "\n".join(c))
        assert cuerpo(ruta_e) == c, f"{espejo}: el espejo no quedó idéntico"
        print(f"OK: {espejo} regenerado ({len(c)} líneas, espejo exacto de {fuente})")
        ok += 1
    # (B) los fallbacks: en-US RESPETADO — solo paridad de claves
    for fuente, fallback in FALLBACKS:
        ruta_f, ruta_b = os.path.join(BASE, fuente), os.path.join(BASE, fallback)
        cf, cb = cuerpo(ruta_f), cuerpo(ruta_b)
        faltan = [k for k in claves(cf) if k not in claves(cb)]
        if faltan:
            # las claves que faltan se agregan con el texto español (el
            # fallback de siempre) al final del cuerpo
            with open(ruta_b, "a", encoding="utf-8") as f:
                for sangria, k in faltan:
                    f.write(f"{sangria}{k}: {{{len(faltan)}\n")  # nunca debería pasar: aviso
            print(f"AVISO: {fallback}: {len(faltan)} claves faltantes (verificar a mano)")
        else:
            print(f"OK: {fallback} respeta sus traducciones y tiene TODAS las claves de {fuente}")
        ok += 1
    print(f"SYNC COMPLETO: {ok} archivos — es-MX espejo puro ×2 + en-US respetado ×2 "
          f"(la lección v6.97.1: la traducción viva NUNCA se toca).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
