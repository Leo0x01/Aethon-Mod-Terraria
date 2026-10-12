#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# v6.50.110 — INSERTA LAS CLAVES hjson DE LA ESENCIA DE HAMBRE (el
# probador del hambre del Eterno) en las TRES localizaciones.
# Respeta el formato de CADA archivo: es-ES/en-US usan TABS (\t, \t\t,
# \t\t\t) y es-MX usa ESPACIOS (8/16/24) — el script lo DETECTA del
# bloque CarnadaDelGrimorio y calza la indentación.
import sys, re

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"

# --- el contenido (por idioma) ---
ITEM = {
"es-ES": """@I1@EsenciaHambre: {
@I2@DisplayName: Esencia de Hambre
@I2@Tooltip:
@I3@'''
@I3@Sube el hambre del Grimorio del Eterno +5% por uso.
@I3@El probador del reloj nervioso — sin esperar una hora.
@I3@Reutilizable — no se consume.
@I3@Objeto de pruebas — eliminar antes de publicar.
@I3@'''
@I1@}}""",
"en-US": """@I1@EsenciaHambre: {
@I2@DisplayName: Hunger Essence
@I2@Tooltip:
@I3@'''
@I3@Raises the Eternal Grimoire's hunger +5% per use.
@I3@The nervous clock tester — no waiting a full hour.
@I3@Reusable — not consumed.
@I3@Testing item — remove before publishing.
@I3@'''
@I1@}}""",
"es-MX": """@I1@EsenciaHambre: {
@I2@DisplayName: Esencia de Hambre
@I2@Tooltip:
@I3@'''
@I3@Sube el hambre del Grimorio del Eterno +5% por uso.
@I3@El probador del reloj nervioso — sin esperar una hora.
@I3@Reutilizable — no se consume.
@I3@Objeto de pruebas — eliminar antes de publicar.
@I3@'''
@I1@}}""",
}

TOP = {
"es-ES": """EsenciaHambre: {
@I1@Sube: El apetito del Grimorio del Eterno sube a {0}%.
}}""",
"en-US": """EsenciaHambre: {
@I1@Sube: The Eternal Grimoire's appetite rises to {0}%.
}}""",
"es-MX": """EsenciaHambre: {
@I1@Sube: El apetito del Grimorio del Eterno sube a {0}%.
}}""",
}

def indent_of(lang, src):
    """Detecta la indentación (tabs o espacios) del archivo leyendo el
    bloque del ítem CarnadaDelGrimorio."""
    m = re.search(r"CarnadaDelGrimorio: \{\n([ \t]+)DisplayName:", src)
    if not m:
        raise SystemExit(f"{lang}: no encontré el bloque CarnadaDelGrimorio")
    i2 = m.group(1)
    if "\t" in i2:
        return "\t", "\t\t", "\t\t\t"          # i1, i2, i3 (tabs)
    n = len(i2) // 2                            # 16 espacios → base 8
    return " " * n, " " * (2 * n), " " * (3 * n)

def insert_after_block(src, header, block):
    """Inserta `block` justo después del bloque que empieza con `header`
    (busca su cierre: la línea `}` con la MISMA indentación del header)."""
    i = src.find(header)
    if i < 0:
        raise SystemExit(f"no encontré: {header!r}")
    # el cierre: la primera línea que es SOLO '}' al nivel del header
    j = src.find("\n}\n", i)
    if j < 0:
        raise SystemExit(f"no encontré el cierre de: {header!r}")
    j += 3                                      # tras el "\n}\n"
    return src[:j] + block + "\n" + src[j:]

def main():
    ok = True
    for lang in ("es-ES", "en-US", "es-MX"):
        path = BASE + f"{lang}_Mods.AethonMod.hjson"
        src = open(path, "rb").read().decode("utf-8")

        if "EsenciaHambre:" in src:
            print(f"{lang}: YA tiene EsenciaHambre — nada que hacer")
            continue

        i1, i2, i3 = indent_of(lang, src)

        # 1) el ítem (dentro de Items { … }) — tras el bloque CarnadaDelGrimorio
        item = ITEM[lang].replace('@I1@', i1).replace('@I2@', i2).replace('@I3@', i3).replace("}}", "}")
        # el cierre del ítem usa la indentación de nivel 1 (la de los nombres)
        m = re.search(r"(CarnadaDelGrimorio: \{.*?\n" + re.escape(i1) + r"\}\n)", src, re.S)
        if not m:
            raise SystemExit(f"{lang}: no pude aislar el bloque del ítem Carnada")
        item_block = "\n" + item + "\n"
        src = src[:m.end(1)] + item_block + src[m.end(1):]

        # 2) la clave de la voz (top-level) — tras el bloque Carnada: { … }
        top = TOP[lang].replace('@I1@', i1).replace("}}", "}")
        src = insert_after_block(src, "Carnada: {", top + "\n")

        open(path, "wb").write(src.encode("utf-8"))

        # verificación: paréntesis balanceados + el ítem presente
        v = open(path, "rb").read().decode("utf-8")
        abren = v.count("{"); cierran = v.count("}")
        assert abren == cierran, f"{lang}: llaves desbalanceadas {abren}/{cierran}"
        assert v.count("EsenciaHambre:") == 2, f"{lang}: faltan bloques EsenciaHambre"
        if lang == "es-MX":
            assert "\t" not in v, "es-MX: se coló un TAB"
        print(f"{lang}: OK (llaves {abren}/{cierran}, 2 bloques EsenciaHambre)")

    print("TODO OK")

if __name__ == "__main__":
    main()
