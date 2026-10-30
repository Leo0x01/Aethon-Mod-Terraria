#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 v6.56.6 — EL SYNC DEL ESPAÑOL v2: es-ES sigue siendo LA FUENTE ÚNICA; este
 script regenera los DOS espejos: es-MX y en-US.

 v6.56.4: el en-US fue RETIRADO («solo deja español es_es»). La .56.6 lo
 trae de vuelta como ESPEJO DE SEGURIDAD — no como idioma: tModLoader cae
 SIEMPRE al en-US cuando la clave no existe en la cultura activa del
 juego; sin en-US, un juego en INGLÉS (o alemán, francés…) mostraba LAS
 CLAVES CRUDAS («Mods.AethonMod.HUD.Nivel» en la barra de exp — la letra
 del usuario: «en la barra de exp en vez de decir nivel, dice
 mods.aethonmod.nivel o algo asi… y este problema esta en muchos
 lugares»). Con el en-US llevando EL MISMO TEXTO ESPAÑOL, el mod habla
 español en CUALQUIER idioma del juego y las claves crudas son
 IMPOSIBLES. La edición sigue siendo UNA: solo el es-ES.

 USO: editar SOLO los es-ES y correr  python3 tools/sync_es_mx_v6564.py
 (idempotente; aborta con aserción si algo inesperado pasa).
=============================================================================
"""
import os
import sys

BASE = os.path.join(os.path.dirname(__file__), "..", "AethonMod", "Localization")

FUENTES = [
    ("es-ES_Mods.AethonMod.hjson", "es-MX_Mods.AethonMod.hjson"),
    ("Escriba/es-ES_Mods.AethonMod.hjson", "Escriba/es-MX_Mods.AethonMod.hjson"),
    ("es-ES_Mods.AethonMod.hjson", "en-US_Mods.AethonMod.hjson"),
    ("Escriba/es-ES_Mods.AethonMod.hjson", "Escriba/en-US_Mods.AethonMod.hjson"),
]

CABECERA_ESPEJO = """// v6.56.6 — EL ESPEJO LATINOAMÉRICA: copia AUTOMÁTICA del es-ES (la fuente
// única del español de la casa). Contenido IDÉNTICO al es-ES: editar SOLO
// el es-ES y correr tools/sync_es_mx_v6564.py."""

CABECERA_FALLBACK = """// v6.56.6 — LA RED DE SEGURIDAD DEL FALLBACK: copia AUTOMÁTICA del es-ES
// con EL MISMO TEXTO ESPAÑOL. tModLoader cae al en-US cuando la cultura
// activa del juego no tiene una clave del mod: sin este archivo, un juego
// en inglés mostraba CLAVES CRUDAS («Mods.AethonMod.HUD.Nivel» en vez de
// «Grimorio · Nv X»). NO es un idioma del mod — es el mismo español
// viajando en la maleta del fallback. Editar SOLO el es-ES y correr
// tools/sync_es_mx_v6564.py."""


def cuerpo(ruta):
    """El cuerpo del hjson SIN su bloque de cabecera (// líneas + blancos)."""
    lineas = open(ruta, encoding="utf-8").read().split("\n")
    i = 0
    while i < len(lineas) and (lineas[i].startswith("//") or lineas[i].strip() == ""):
        i += 1
    assert i < len(lineas), f"{ruta}: archivo sin cuerpo"
    return lineas[i:]


def main():
    ok = 0
    for fuente, espejo in FUENTES:
        ruta_f = os.path.join(BASE, fuente)
        ruta_e = os.path.join(BASE, espejo)
        assert os.path.isfile(ruta_f), f"FALTA la fuente {ruta_f}"
        c = cuerpo(ruta_f)
        cab = CABECERA_FALLBACK if espejo.startswith("en-US") else CABECERA_ESPEJO
        with open(ruta_e, "w", encoding="utf-8") as f:
            f.write(cab + "\n" + "\n".join(c))
        # aserción de espejo: el cuerpo del espejo debe ser IDÉNTICO al de la fuente
        assert cuerpo(ruta_e) == c, f"{espejo}: el espejo no quedó idéntico"
        print(f"OK: {espejo} regenerado ({len(c)} líneas, espejo exacto de {fuente})")
        ok += 1
    print(f"SYNC COMPLETO: {ok} espejos regenerados (es-MX ×2 + en-US ×2). "
          f"El español viaja ahora en TODAS las maletas del motor.")


if __name__ == "__main__":
    sys.exit(main())
