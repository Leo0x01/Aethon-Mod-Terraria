#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.107 — LOS TOOLTIPS DEL AURA 2 (es-ES + en-US; es-MX via sync) + build.txt."""
import io, sys

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria"

def reemplaza_en(ruta, viejo, nuevo, etiqueta):
    s = io.open(ruta, encoding="utf-8").read()
    if viejo not in s or s.count(viejo) != 1:
        print(f"FALLO: {etiqueta} (ocurrencias: {s.count(viejo)})")
        sys.exit(1)
    io.open(ruta, "w", encoding="utf-8", newline="\n").write(s.replace(viejo, nuevo))
    print(f"OK: {etiqueta}")

# ---------------------------------------------------------------- es-ES
reemplaza_en(
    f"{BASE}/AethonMod/Localization/es-ES_Mods.AethonMod.hjson",
    """		// v6.105: EL AURA DE JUICIO 2 — la copia silenciosa (la letra:
		// partículas más pequeñas, el efecto del estandarte, NADA sobre
		// el jugador, morado y negro, bruma y el semicírculo Perlin).""",
    """		// v6.105 → v6.107: EL AURA DE JUICIO 2 — la copia silenciosa
		// (la letra: partículas más pequeñas, el efecto del estandarte,
		// NADA sobre el jugador, morado y negro, bruma y el semicírculo
		// Perlin; la v6.107 lo pone DE PIE — la cúpula vertical, las
		// chispas naciendo del corazón y huyendo del portador).""",
    "es-ES comentario")

reemplaza_en(
    f"{BASE}/AethonMod/Localization/es-ES_Mods.AethonMod.hjson",
    '''		Linea3: "[c/B96EEA:Sobre cada impío: partículas pequeñas moradas y negras expulsadas del flanco que te mira, bruma a la deriva y LA MEDIA CÚPULA PERLIN que se disuelve en círculo sobre su cuerpo]"''',
    '''		Linea3: "[c/B96EEA:Sobre cada impío: LA CÚPULA PERLIN que se yergue vertical sobre su cabeza (la parte plana disuelta abajo), chispas moradas y negras que NACEN DE SU CORAZÓN y salen expulsadas SIEMPRE en tu dirección contraria, y bruma que se derrama por el mismo rumbo]"''',
    "es-ES Linea3")

# ---------------------------------------------------------------- en-US
reemplaza_en(
    f"{BASE}/AethonMod/Localization/en-US_Mods.AethonMod.hjson",
    """		// v6.105: EL AURA DE JUICIO 2 — la copia silenciosa (la letra:
		// partículas más pequeñas, el efecto del estandarte, NADA sobre
		// el jugador, morado y negro, bruma y el semicírculo Perlin).""",
    """		// v6.105 → v6.107: EL AURA DE JUICIO 2 — la copia silenciosa
		// (la letra: partículas más pequeñas, el efecto del estandarte,
		// NADA sobre el jugador, morado y negro, bruma y el semicírculo
		// Perlin; la v6.107 lo pone DE PIE — la cúpula vertical, las
		// chispas naciendo del corazón y huyendo del portador).""",
    "en-US comentario")

reemplaza_en(
    f"{BASE}/AethonMod/Localization/en-US_Mods.AethonMod.hjson",
    '''		Linea3: "[c/B96EEA:Upon each fiend: tiny purple-and-black sparks cast off the flank that faces you, drifting haze, and THE PERLIN HALF-DOME dissolving in a circle over its body]"''',
    '''		Linea3: "[c/B96EEA:Upon each fiend: THE PERLIN DOME standing upright over its head (the flat side dissolved below), tiny purple-and-black sparks BORN FROM ITS HEART and cast forth ALWAYS away from you, and haze pouring down the same trail]"''',
    "en-US Linea3")

# ---------------------------------------------------------------- build.txt
reemplaza_en(
    f"{BASE}/AethonMod/build.txt",
    "version = 6.106",
    "version = 6.107",
    "build.txt 6.107")

print("\nTOOLTIPS + VERSIÓN LISTOS")
