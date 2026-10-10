#!/usr/bin/env python3
"""v6.50.90 — EL TERCER ASIENTO CAMBIA DE DUEÑO (hjson).

La letra del usuario: «el tercer libro borralo, y crea otro con las
mismas caracteristicas, temblor, glisheado, y nervioso».

En es-MX y en-US: el bloque GrimorioHambrientoTembloroso queda BORRADO y
entra GrimorioHambrientoNervioso (temblor .87 + glitch .88 + nerviosismo:
ojo ansioso y sobresaltos que rompen el libro).

REGLA DE LA CASA (incidentes 15/17 del espejo): los hjson se tocan con
Python NUNCA con la herramienta Edit (que normaliza tabs→espacios). El
es-MX lleva 8 espacios de base; el en-US TABS — aquí se respetan.
"""
import io
import sys

MX = 'AethonMod/Localization/es-MX_Mods.AethonMod.hjson'
EN = 'AethonMod/Localization/en-US_Mods.AethonMod.hjson'

# --- es-MX: base de 8 espacios (como vive el archivo) ---
MX_VIEJO = (
    "        GrimorioHambrientoTembloroso: {\n"
    "                DisplayName: Grimorio Hambriento Tembloroso\n"
    "                Tooltip:\n"
    "                        '''\n"
    "                        La copia TEMBLOROSA \u2014 el mismo ojo hambriento, pero\n"
    "                        este libro tiembla: un escalofr\u00edo suave que crece con\n"
    "                        el hambre, del susurro saciado al temblor del apetito\n"
    "                        total. El libro y su ojo tiemblan juntos.\n"
    "                        Clic izq: descarga perseguidora (pruebas, sin man\u00e1).\n"
    "                        Clic der: reinicia el apetito para repetir la demo.\n"
    "                        Objeto de pruebas \u2014 comparar con el original.\n"
    "                        '''\n"
    "        }\n"
)

MX_NUEVO = (
    "        // v6.50.90 \u2014 EL TERCER ASIENTO CAMBIA DE DUE\u00d1O: el Tembloroso\n"
    "        // queda BORRADO y entra EL NERVIOSO (temblor + glitch + nervios).\n"
    "        GrimorioHambrientoNervioso: {\n"
    "                DisplayName: Grimorio Hambriento Nervioso\n"
    "                Tooltip:\n"
    "                        '''\n"
    "                        La copia NERVIOSA \u2014 el mismo ojo hambriento, pero\n"
    "                        este libro vive asustado: tiembla sin parar, se\n"
    "                        sobresalta de golpe (y cada susto lo rompe en tiras\n"
    "                        un instante) y su mirada salta de un lado a otro,\n"
    "                        revisando. Cuanto m\u00e1s hambre, m\u00e1s sustos y m\u00e1s\n"
    "                        roto queda.\n"
    "                        Clic izq: descarga perseguidora (pruebas, sin man\u00e1).\n"
    "                        Clic der: reinicia el apetito para repetir la demo.\n"
    "                        Objeto de pruebas \u2014 comparar con el original.\n"
    "                        '''\n"
    "        }\n"
)

# --- en-US: TABS (el incidente 15 — el paquete .87 llevaba espacios) ---
T = '\t'
EN_VIEJO = (
    f"{T}GrimorioHambrientoTembloroso: {{\n"
    f"{T}{T}DisplayName: Trembling Hungry Grimoire\n"
    f"{T}{T}Tooltip:\n"
    f"{T}{T}{T}'''\n"
    f"{T}{T}{T}The TREMBLING copy \u2014 the same hungry eye, but this book\n"
    f"{T}{T}{T}shivers: a soft shudder that grows with hunger, from a\n"
    f"{T}{T}{T}sated whisper to the tremble of a full appetite. The\n"
    f"{T}{T}{T}book and its eye tremble together.\n"
    f"{T}{T}{T}Left click: homing bolt (testing, no mana).\n"
    f"{T}{T}{T}Right click: resets the appetite to replay the demo.\n"
    f"{T}{T}{T}Testing item \u2014 compare against the original.\n"
    f"{T}{T}{T}'''\n"
    f"{T}}}\n"
)

EN_NUEVO = (
    f"{T}// v6.50.90 \u2014 THE THIRD SEAT CHANGES HANDS: the Trembling one is\n"
    f"{T}// DELETED and THE NERVOUS ONE enters (shudder + glitch + nerves).\n"
    f"{T}GrimorioHambrientoNervioso: {{\n"
    f"{T}{T}DisplayName: Nervous Hungry Grimoire\n"
    f"{T}{T}Tooltip:\n"
    f"{T}{T}{T}'''\n"
    f"{T}{T}{T}The NERVOUS copy \u2014 the same hungry eye, but this book\n"
    f"{T}{T}{T}lives frightened: it shivers nonstop, flinches out of\n"
    f"{T}{T}{T}nowhere (and each startle breaks it into strips for a\n"
    f"{T}{T}{T}beat), and its gaze darts side to side, checking. The\n"
    f"{T}{T}{T}more hunger, the more startles \u2014 and the more it breaks.\n"
    f"{T}{T}{T}Left click: homing bolt (testing, no mana).\n"
    f"{T}{T}{T}Right click: resets the appetite to replay the demo.\n"
    f"{T}{T}{T}Testing item \u2014 compare against the original.\n"
    f"{T}{T}{T}'''\n"
    f"{T}}}\n"
)

fallos = 0
for ruta, viejo, nuevo, nombre in ((MX, MX_VIEJO, MX_NUEVO, 'es-MX'),
                                   (EN, EN_VIEJO, EN_NUEVO, 'en-US')):
    txt = io.open(ruta, encoding='utf-8', newline='').read()
    veces = txt.count(viejo)
    if veces != 1:
        print(f'\u2717 {nombre}: el bloque Tembloroso aparece {veces} veces (esperado 1) — ABORTADO')
        fallos += 1
        continue
    txt = txt.replace(viejo, nuevo)
    assert 'GrimorioHambrientoTembloroso' not in txt, f'{nombre}: queda rastro del Tembloroso'
    assert txt.count('GrimorioHambrientoNervioso') == 1
    io.open(ruta, 'w', encoding='utf-8', newline='').write(txt)
    # checks de formato de la casa
    if nombre == 'en-US':
        n_tabs = txt.count('\t')
        print(f'  \u2713 {nombre}: Tembloroso fuera, Nervioso dentro ({n_tabs} tabs intactos)')
    else:
        print(f'  \u2713 {nombre}: Tembloroso fuera, Nervioso dentro')

sys.exit(1 if fallos else 0)
