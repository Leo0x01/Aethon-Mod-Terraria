#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 v6.56.4 — EL SYNC DEL ESPAÑOL: es-ES es LA FUENTE ÚNICA; este script
 regenera los espejos es-MX a partir de ella (la letra del usuario: «por
 ahora no es necesario que hayan varios, solo deja español es_es ya que es
 suficiente con eso»).

 POR QUÉ existe el espejo es-MX: el juego en «Español (Latinoamérica)» lee
 el archivo es-MX — SIN él caería al fallback (y el paquete en-US fue
 RETIRADO en la v6.56.4: caería a CLAVES SIN TRADUCIR). El contenido es UNO
 (una sola edición, un solo idioma que mantener); el es-MX es una COPIA
 automática, jamás se edita a mano.

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
]

CABECERA_ESPEJO = """// v6.56.4 — EL ESPEJO LATINOAMÉRICA: copia AUTOMÁTICA del es-ES (la fuente
// única del español de la casa). Este archivo existe porque el juego en
// «Español (Latinoamérica)» lee el es-MX: sin él caería al fallback (y el
// en-US ya no existe). Contenido IDÉNTICO al es-ES: editar SOLO el es-ES
// y correr tools/sync_es_mx_v6564.py."""


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
        with open(ruta_e, "w", encoding="utf-8") as f:
            f.write(CABECERA_ESPEJO + "\n" + "\n".join(c))
        # aserción de espejo: el cuerpo del espejo debe ser IDÉNTICO al de la fuente
        assert cuerpo(ruta_e) == c, f"{espejo}: el espejo no quedó idéntico"
        print(f"OK: {espejo} regenerado ({len(c)} líneas, espejo exacto de {fuente})")
        ok += 1
    print(f"SYNC COMPLETO: {ok} espejos regenerados. El en-US NO EXISTE (monolingüe por la casa).")


if __name__ == "__main__":
    sys.exit(main())
