#!/usr/bin/env python3
"""v6.50.90 — LA REFERENCIA AL DIFUNTO (hjson, parte 2).

El tooltip del Inestable (v6.50.89) decía «Tiembla como el Tembloroso y
sufre las interferencias del Errático» — pero el Tembloroso quedó BORRADO
en la .90: un jugador nuevo no puede comparar con un libro que no existe.
La cura: el tooltip describe el temblar sin citar al difunto. El resto
del bloque queda INTACTO (byte a byte)."""

import io
import sys

MX = 'AethonMod/Localization/es-MX_Mods.AethonMod.hjson'
EN = 'AethonMod/Localization/en-US_Mods.AethonMod.hjson'

# --- es-MX (8 espacios de base) ---
MX_VIEJO = "                        apetito. Tiembla como el Tembloroso y sufre las\n"
MX_NUEVO = "                        apetito. Tiembla sin descanso y sufre las\n"

# --- en-US (tabs) ---
T = '\t'
EN_VIEJO = f"{T}{T}{T}trembles like the Trembling one and suffers the\n"
EN_NUEVO = f"{T}{T}{T}trembles without rest and suffers the\n"

fallos = 0
for ruta, viejo, nuevo, nombre in ((MX, MX_VIEJO, MX_NUEVO, 'es-MX'),
                                   (EN, EN_VIEJO, EN_NUEVO, 'en-US')):
    txt = io.open(ruta, encoding='utf-8', newline='').read()
    veces = txt.count(viejo)
    if veces != 1:
        print(f'\u2717 {nombre}: la línea del difunto aparece {veces} veces (esperado 1) — ABORTADO')
        fallos += 1
        continue
    io.open(ruta, 'w', encoding='utf-8', newline='').write(txt.replace(viejo, nuevo))
    print(f'  \u2713 {nombre}: el tooltip del Inestable ya no cita al difunto')

sys.exit(1 if fallos else 0)
