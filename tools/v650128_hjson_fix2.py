#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v650128_hjson_fix2.py — MUEVE el bloque ItemCorrupto al INTERIOR del
bloque Items: { } (quedó top-level al final del archivo — la clave del
juego es Items.ItemCorrupto.DisplayName, no ItemCorrupto.DisplayName)."""
import re

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"

for nombre in ["es-MX_Mods.AethonMod.hjson", "es-ES_Mods.AethonMod.hjson",
               "en-US_Mods.AethonMod.hjson"]:
    ruta = BASE + nombre
    with open(ruta, encoding="utf-8") as f:
        texto = f.read()

    # 1. CORTAR el bloque top-level ItemCorrupto (al final del archivo).
    m = re.search(r"\n\s*ItemCorrupto: \{.*?\n\s*\}\n", texto, re.S)
    if not m:
        print(f"[{nombre}] no hallé el bloque top-level (¿ya movido?)")
        continue
    bloque = m.group(0).strip("\n")
    # quitar una level de indentación si lo tiene (lo repondremos igual)
    texto = (texto[:m.start()] + "\n" + texto[m.end():]).rstrip("\n") + "\n"

    # 2. HALLAR el bloque Items: { ... } y su CIERRE (el primer "}" en
    # columna 0 tras la apertura).
    ap = re.search(r"^Items: \{\s*$", texto, re.M)
    if not ap:
        print(f"[{nombre}] ERROR: no hallé Items: {{"); continue
    cierre = texto.find("\n}\n", ap.end())
    if cierre < 0:
        print(f"[{nombre}] ERROR: no hallé el cierre de Items"); continue

    # 3. INSERTAR el bloque justo antes del cierre (indentación interna:
    #     8 espacios como sus hermanos).
    lineas = bloque.split("\n")
    interno = "\n".join(("    " + ln) if ln.strip() else ln for ln in lineas)
    texto = texto[:cierre] + "\n\n    // v6.50.128 — LA ESENCIA CORRUPTA (la comida del altar)\n" + interno + texto[cierre:]

    with open(ruta, "w", encoding="utf-8", newline="\n") as f:
        f.write(texto)
    print(f"[{nombre}] MOVIDO dentro de Items (línea ~{texto[:cierre].count(chr(10))})")

# VERIFICACIÓN: la clave Items.ItemCorrupto existe (dentro del bloque) y
# NO queda ningún ItemCorrupto top-level.
for nombre in ["es-MX_Mods.AethonMod.hjson", "es-ES_Mods.AethonMod.hjson",
               "en-US_Mods.AethonMod.hjson"]:
    ruta = BASE + nombre
    with open(ruta, encoding="utf-8") as f:
        t = f.read()
    ap = re.search(r"^Items: \{\s*$", t, re.M)
    ci = t.find("\n}\n", ap.end())
    dentro = "ItemCorrupto:" in t[ap.start():ci]
    despues = re.search(r"^ItemCorrupto: \{", t[ci:], re.M) is not None
    print(f"[{nombre}] dentro-de-Items={dentro} top-level-suelto={despues}")
