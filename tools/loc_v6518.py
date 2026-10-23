#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# loc_v6518.py — v6.51.8 — LA CIRUGÍA hjson DE LOS PELDAÑOS Y EL CAMINO
# (la regla de la casa: JAMÁS la herramienta Edit sobre los hjson —
#  cirugía Python por líneas exactas, con asserts):
#   · FaltaMineral (el rechazo de la ofrenda mineral) tras FaltaDinero.
#   · Camino* (las 12 descripciones de la escalera de hitos) tras HitoMaximo.
# ×3 idiomas simétricos (es-ES · en-US · es-MX).

import io, sys

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"

NUEVAS = {
    "es-ES": {
        "FaltaMineral": "✦ El grimorio aparta el plato: pide {0} × {1} y a esa pila le faltan {2}.",
        "CaminoMejora1": "la MEJORA 1 de los ojos (+50% de daño a un ojo)",
        "CaminoSol": "el SOL RÚNICO de 1 anillo (2 ojos mejorados, 2/min)",
        "CaminoAgujeros": "los AGUJEROS NEGROS RÚNICOS (5 ojos mejorados, 1/min)",
        "CaminoMejora2": "la MEJORA 2 de los ojos",
        "CaminoSol2": "el sol de 2 ANILLOS (3/min)",
        "CaminoMascota": "LA MASCOTA — el grimorio escupe al minigrimorio",
        "CaminoAscendidos": "los agujeros ASCENDIDOS (más daño)",
        "CaminoMejora3": "la MEJORA 3 de los ojos",
        "CaminoSol3": "el sol de 3 ANILLOS (4/min)",
        "CaminoMejora4": "la MEJORA FINAL de los ojos",
        "CaminoSupremo": "agujeros 2/min… y EL SUPREMO/AURORA (8 ojos con la mejora 4)",
        "CaminoMaximo": "TODO el escuadrón MÁS FUERTE y MÁS RÁPIDO",
    },
    "en-US": {
        "FaltaMineral": "✦ The grimoire pushes the dish away: it demands {0} × {1} and that pile is {2} short.",
        "CaminoMejora1": "eye UPGRADE 1 (+50% damage to one eye)",
        "CaminoSol": "the 1-ring RUNIC SUN (2 upgraded eyes, 2/min)",
        "CaminoAgujeros": "the RUNIC BLACK HOLES (5 upgraded eyes, 1/min)",
        "CaminoMejora2": "eye UPGRADE 2",
        "CaminoSol2": "the 2-ring sun (3/min)",
        "CaminoMascota": "THE PET — the grimoire spits the mini-grimoire",
        "CaminoAscendidos": "the ASCENDED black holes (more damage)",
        "CaminoMejora3": "eye UPGRADE 3",
        "CaminoSol3": "the 3-ring sun (4/min)",
        "CaminoMejora4": "the FINAL eye upgrade",
        "CaminoSupremo": "holes 2/min… and THE SUPREME/AURORA (8 eyes with upgrade 4)",
        "CaminoMaximo": "ALL the squad STRONGER and FASTER",
    },
    "es-MX": {
        "FaltaMineral": "✦ El grimorio aparta el plato: pide {0} × {1} y a esa pila le faltan {2}.",
        "CaminoMejora1": "la MEJORA 1 de los ojos (+50% de daño a un ojo)",
        "CaminoSol": "el SOL RÚNICO de 1 anillo (2 ojos mejorados, 2/min)",
        "CaminoAgujeros": "los AGUJEROS NEGROS RÚNICOS (5 ojos mejorados, 1/min)",
        "CaminoMejora2": "la MEJORA 2 de los ojos",
        "CaminoSol2": "el sol de 2 ANILLOS (3/min)",
        "CaminoMascota": "LA MASCOTA — el grimorio escupe al minigrimorio",
        "CaminoAscendidos": "los agujeros ASCENDIDOS (más daño)",
        "CaminoMejora3": "la MEJORA 3 de los ojos",
        "CaminoSol3": "el sol de 3 ANILLOS (4/min)",
        "CaminoMejora4": "la MEJORA FINAL de los ojos",
        "CaminoSupremo": "agujeros 2/min… y EL SUPREMO/AURORA (8 ojos con la mejora 4)",
        "CaminoMaximo": "TODO el escuadrón MÁS FUERTE y MÁS RÁPIDO",
    },
}

ORDEN_CAMINO = [
    "CaminoMejora1", "CaminoSol", "CaminoAgujeros", "CaminoMejora2",
    "CaminoSol2", "CaminoMascota", "CaminoAscendidos", "CaminoMejora3",
    "CaminoSol3", "CaminoMejora4", "CaminoSupremo", "CaminoMaximo",
]

for lang, claves in NUEVAS.items():
    ruta = BASE + f"{lang}_Mods.AethonMod.hjson"
    with io.open(ruta, "r", encoding="utf-8") as f:
        lineas = f.readlines()

    # --- 1) NADA de duplicados (la cirugía es idempotente) ---
    for k in claves:
        for ln in lineas:
            if ln.startswith("\t" + k + ":"):
                raise SystemExit(f"[ABORT] {lang}: la clave {k} ya existe")

    # --- 2) LOS ANCLAJES (líneas exactas) ---
    ancla_dinero = None
    ancla_maximo = None
    for i, ln in enumerate(lineas):
        if ln.startswith("\tFaltaDinero:"):
            ancla_dinero = i
        if ln.startswith("\tHitoMaximo:"):
            ancla_maximo = i
    assert ancla_dinero is not None, f"{lang}: no hallé FaltaDinero"
    assert ancla_maximo is not None, f"{lang}: no hallé HitoMaximo"
    assert ancla_maximo > ancla_dinero, f"{lang}: orden raro de anclas"

    # --- 3) LA INSERCIÓN: FaltaMineral tras FaltaDinero ---
    nuevas_dinero = ["\tFaltaMineral: " + claves["FaltaMineral"] + "\n"]
    lineas[ancla_dinero + 1:ancla_dinero + 1] = nuevas_dinero
    # el ancla de HitoMaximo se movió +1
    ancla_maximo += 1

    # --- 4) LA INSERCIÓN: los 12 Camino* tras HitoMaximo ---
    nuevas_camino = ["\t" + k + ": " + claves[k] + "\n" for k in ORDEN_CAMINO]
    lineas[ancla_maximo + 1:ancla_maximo + 1] = nuevas_camino

    # --- 5) ESCRITURA (UTF-8, sin BOM, finales de línea intactos) ---
    with io.open(ruta, "w", encoding="utf-8", newline="") as f:
        f.writelines(lineas)

    print(f"[OK] {lang}: +1 FaltaMineral +12 Camino* "
          f"({len(lineas)} líneas totales)")

print("CIRUGÍA COMPLETA: 13 claves × 3 idiomas = 39 claves nuevas")
