#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# v6.50.112 — INSERTA LAS CLAVES hjson DE LA PÁGINA EN BLANCO (el
# probador de la devoración de jefes) en las TRES localizaciones.
# Formatos ACTUALES del árbol (el packager los reescribió en la .111):
# · Items: bloque con indentación — es-ES/en-US TABS (\t, \t\t, \t\t\t),
#   es-MX ESPACIOS (8/16/24) — se DETECTA del bloque EsenciaHambre.
# · Mensajes top-level: es-MX BLOQUE (EsenciaHambre: { … }), es-ES/en-US
#   CLAVES PLANAS (EsenciaHambre.Sube: …) — cada quien su formato.
import sys, re

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"

# --- el ítem (dentro de Items { … }) ---
ITEM = {
"es-ES": """@I1@PaginaEnBlanco: {
@I2@DisplayName: Página en Blanco
@I2@Tooltip:
@I3@'''
@I3@Borra la memoria de festines del Grimorio del Eterno.
@I3@Cada tipo de jefe volverá a estrenar SU PRIMERA CENA.
@I3@Reutilizable — no se consume.
@I3@Objeto de pruebas — eliminar antes de publicar.
@I3@'''
@I1@}""",
"en-US": """@I1@PaginaEnBlanco: {
@I2@DisplayName: Blank Page
@I2@Tooltip:
@I3@'''
@I3@Wipes the Eternal Grimoire's feast memory.
@I3@Every boss type will debut its FIRST DINNER again.
@I3@Reusable — not consumed.
@I3@Testing item — remove before publishing.
@I3@'''
@I1@}""",
"es-MX": """@I1@PaginaEnBlanco: {
@I2@DisplayName: Página en Blanco
@I2@Tooltip:
@I3@'''
@I3@Borra la memoria de festines del Grimorio del Eterno.
@I3@Cada tipo de jefe volverá a estrenar SU PRIMERA CENA.
@I3@Reutilizable — no se consume.
@I3@Objeto de pruebas — eliminar antes de publicar.
@I3@'''
@I1@}""",
}

# --- los mensajes (top-level) ---
BLOQUE = {   # es-MX: aún en bloque
"es-MX": """PaginaEnBlanco: {
@I1@Reset: El grimorio olvida sus {0} festines — cada jefe volverá a estrenar su primera cena.
@I1@Nada: El grimorio no recuerda ningún festín — toda primera cena está por ocurrir.
}""",
}
PLANO = {    # es-ES / en-US: el packager las aplanó
"es-ES": """PaginaEnBlanco.Reset: El grimorio olvida sus {0} festines — cada jefe volverá a estrenar su primera cena.
PaginaEnBlanco.Nada: El grimorio no recuerda ningún festín — toda primera cena está por ocurrir.""",
"en-US": """PaginaEnBlanco.Reset: The grimoire forgets its {0} feasts — every boss will debut its first dinner again.
PaginaEnBlanco.Nada: The grimoire remembers no feasts — every first dinner is yet to come.""",
}

def indent_of(lang, src):
    """Detecta la indentación (tabs o espacios) del archivo leyendo el
    bloque del ítem EsenciaHambre (Items: …)."""
    m = re.search(r"EsenciaHambre: \{\n([ \t]+)DisplayName:", src)
    if not m:
        raise SystemExit(f"{lang}: no encontré el bloque EsenciaHambre")
    i2 = m.group(1)
    if "\t" in i2:
        return "\t", "\t\t", "\t\t\t"
    n = len(i2) // 2
    return " " * n, " " * (2 * n), " " * (3 * n)

def main():
    for lang in ("es-ES", "en-US", "es-MX"):
        path = BASE + f"{lang}_Mods.AethonMod.hjson"
        src = open(path, "rb").read().decode("utf-8")

        if "PaginaEnBlanco" not in src:
            i1, i2, i3 = indent_of(lang, src)

            # 1) el ítem — justo tras el bloque EsenciaHambre de Items
            item = ITEM[lang].replace('@I1@', i1).replace('@I2@', i2).replace('@I3@', i3)
            m = re.search(r"(EsenciaHambre: \{.*?\n" + re.escape(i1) + r"\}\n)", src, re.S)
            if not m:
                raise SystemExit(f"{lang}: no pude aislar el bloque del ítem EsenciaHambre")
            src = src[:m.end(1)] + item + "\n\n" + src[m.end(1):]

            # 2) los mensajes top-level
            if lang in BLOQUE:
                # es-MX: bloque — tras el cierre del bloque EsenciaHambre top-level
                top = BLOQUE[lang].replace('@I1@', i1)
                m2 = re.search(r"(?m)^EsenciaHambre: \{.*?\n\}\n", src, re.S)
                if not m2:
                    raise SystemExit(f"{lang}: no encontré el bloque top-level EsenciaHambre")
                src = src[:m2.end(0)] + top + "\n\n" + src[m2.end(0):]
            else:
                # es-ES / en-US: claves planas — tras la línea EsenciaHambre.Sube
                m2 = re.search(r"(?m)^EsenciaHambre\.Sube: .*\n", src)
                if not m2:
                    raise SystemExit(f"{lang}: no encontré EsenciaHambre.Sube plana")
                src = src[:m2.end(0)] + PLANO[lang] + "\n\n" + src[m2.end(0):]

            open(path, "wb").write(src.encode("utf-8"))
        else:
            print(f"{lang}: ya tenía PaginaEnBlanco — sólo verifico")

        # verificación: llaves balanceadas + el ítem y las claves presentes
        v = open(path, "rb").read().decode("utf-8")
        abren = v.count("{"); cierran = v.count("}")
        assert abren == cierran, f"{lang}: llaves desbalanceadas {abren}/{cierran}"
        assert "PaginaEnBlanco:" in v, f"{lang}: falta el bloque del ítem"
        if lang in BLOQUE:
            # es-MX: mensajes en BLOQUE (PaginaEnBlanco: { Reset/Nada })
            assert re.search(r"(?m)^PaginaEnBlanco: \{\n[ \t]+Reset: ", v), \
                f"{lang}: falta el bloque de mensajes"
            assert re.search(r"(?m)^PaginaEnBlanco: \{\n[ \t]+Reset: .*\n[ \t]+Nada: ", v, re.S), \
                f"{lang}: falta Nada en el bloque de mensajes"
        else:
            assert "PaginaEnBlanco.Reset" in v and "PaginaEnBlanco.Nada" in v, \
                f"{lang}: faltan las claves planas de los mensajes"
        if lang == "es-MX":
            assert "\t" not in v, "es-MX: se coló un TAB"
        print(f"{lang}: OK (llaves {abren}/{cierran}, ítem + mensajes)")

    print("TODO OK")

if __name__ == "__main__":
    main()
