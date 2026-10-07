#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
SYNC_ES_ES — v6.50.71 — TODAS LAS VARIANTES DE ESPAÑOL CAEN EN LA NUESTRA.

La letra del usuario: «tengo el juego en español, pero el juego sale en
inglés, haz que todas las variantes de español caigan en nuestra versión
en español». La .70 BORRÓ el es-ES por decreto (el juego del usuario vive
en «Español» = cultura es-ES → fallback INGLÉS); la cura: es-ES regresa
como ESPEJO GENERADO de es-MX (el idioma primario) — mismas claves, mismos
textos, solo cambia el comentario de cabecera.

REGLA DE LA CASA: editar SOLO es-MX_Mods.AethonMod.hjson (con tabs
literales) y correr este script — jamás editar el espejo a mano.

Invariantes que verifica (o MUERE):
  1. El cuerpo (todo lo que no son comentarios de cabecera) de es-ES es
     BYTE-IDÉNTICO al cuerpo de es-MX.
  2. Misma cantidad de DisplayName/Tooltip en ambos.
Idempotente: correrlo dos veces produce el mismo archivo.
"""

import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
CARPETA = REPO / "AethonMod" / "Localization"
ES_MX = CARPETA / "es-MX_Mods.AethonMod.hjson"
ES_ES = CARPETA / "es-ES_Mods.AethonMod.hjson"

CABECERA_ES_MX = """\
// v6.50.65 — ESPAÑOL LATINOAMÉRICA (es-MX): el idioma PRIMARIO del mod
// (la letra del usuario: «el idioma del juego debe ser español latinoamérica,
// no español españa»). v6.50.71 — TODAS LAS VARIANTES DE ESPAÑOL CAEN AQUÍ:
// el juego en «Español (España)» y en «Español (Latinoamérica)» lee ESTE
// archivo (es-ES es un ESPEJO GENERADO de este — tools/sync_es_es_v65071.py).
"""

CABECERA_ES_ES = """\
// ESPEJO GENERADO de es-MX_Mods.AethonMod.hjson — NO EDITAR A MANO.
// v6.50.71 — «todas las variantes de español caen en nuestra versión en
// español»: el juego en «Español (España)» (cultura es-ES) lee ESTE archivo,
// idéntico al es-MX (Latinoamérica, el idioma primario del mod).
// Regenerar tras editar el es-MX: python3 tools/sync_es_es_v65071.py
"""


def cuerpo(texto: str) -> str:
    """El archivo SIN sus comentarios de cabecera (todo desde la primera
    línea que no empieza con //)."""
    lineas = texto.split("\n")
    i = 0
    while i < len(lineas) and lineas[i].startswith("//"):
        i += 1
    return "\n".join(lineas[i:])


def main() -> int:
    if not ES_MX.exists():
        print(f"FALTA: {ES_MX}")
        return 1

    texto_mx = ES_MX.read_text(encoding="utf-8")
    cuerpo_mx = cuerpo(texto_mx)

    # --- 1 · reescribir la cabecera del es-MX (idempotente) ---
    nuevo_mx = CABECERA_ES_MX + cuerpo_mx
    if nuevo_mx != texto_mx:
        ES_MX.write_text(nuevo_mx, encoding="utf-8")
        print(f"es-MX: cabecera actualizada ({len(nuevo_mx)} B)")
    else:
        print("es-MX: cabecera ya vigente (sin cambios)")

    # --- 2 · generar el espejo es-ES ---
    texto_es = CABECERA_ES_ES + cuerpo_mx
    ES_ES.write_text(texto_es, encoding="utf-8")
    print(f"es-ES: espejo generado ({len(texto_es)} B)")

    # --- 3 · invariantes ---
    texto_es_leido = ES_ES.read_text(encoding="utf-8")
    if cuerpo(texto_es_leido) != cuerpo_mx:
        print("FATAL: el cuerpo de es-ES NO es idéntico al de es-MX")
        return 1
    dn_mx = texto_mx.count("DisplayName:")
    dn_es = texto_es_leido.count("DisplayName:")
    tt_mx = texto_mx.count("Tooltip:")
    tt_es = texto_es_leido.count("Tooltip:")
    if dn_mx != dn_es or tt_mx != tt_es:
        print(f"FATAL: claves desalineadas (es-MX {dn_mx}/{tt_mx} vs es-ES {dn_es}/{tt_es})")
        return 1
    print(f"VERIFICADO: cuerpos idénticos · DisplayName {dn_mx} == {dn_es} · Tooltip {tt_mx} == {tt_es}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
