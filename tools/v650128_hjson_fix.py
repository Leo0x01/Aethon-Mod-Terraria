#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v650128_hjson_fix.py — REPARA los marcadores ''' del Tooltip de
ItemCorrupto en los 3 hjson (la triple comilla de Python se comió los
delimitadores de hjson: el multilinea necesita su ''' de apertura y
cierre)."""
import re

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"

for nombre in ["es-MX_Mods.AethonMod.hjson", "es-ES_Mods.AethonMod.hjson",
               "en-US_Mods.AethonMod.hjson"]:
    ruta = BASE + nombre
    with open(ruta, encoding="utf-8") as f:
        texto = f.read()

    # EL PATRÓN ROTO: "Tooltip:" seguido DIRECTO del texto (sin ''').
    # Bloque ItemCorrupto: desde su apertura hasta su cierre.
    m = re.search(r"( \*{8})?(\s*)ItemCorrupto: \{(\s*\n\s*DisplayName:[^\n]*\n\s*Tooltip:\n)(.*?)(\n\s*\})",
                  texto, re.S)
    if not m:
        print(f"[{nombre}] no hallé el bloque (¿ya está reparado?)")
        continue

    cuerpo = m.group(4)
    if "'''" in cuerpo:
        print(f"[{nombre}] ya tiene ''' — intacto")
        continue

    # Reindentar el cuerpo a la profundidad del multilinea (24 espacios)
    # y envolverlo con sus '''.
    lineas = [ln.strip() for ln in cuerpo.split("\n") if ln.strip()]
    indent = "\n" + " " * 24
    nuevo = " " * 24 + "'''" + indent + indent.join(lineas) + indent + "'''"

    texto = texto[:m.start(4)] + nuevo + texto[m.end(4):]
    with open(ruta, "w", encoding="utf-8", newline="\n") as f:
        f.write(texto)
    print(f"[{nombre}] REPARADO ({len(lineas)} líneas de tooltip)")
