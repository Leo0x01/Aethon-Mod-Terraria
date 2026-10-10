#!/usr/bin/env python3
"""v6.50.91 — LA CURA DEL 19º INCIDENTE DEL ESPEJO: la herramienta de edición
normalizó TABS→ESPACIOS en TODO el en-US (2.081 líneas). El archivo fue
restaurado desde git; este parche re-aplica los 3 cambios con bytes EXACTOS
(la lección del Task 66: en hjson grandes, editar por script Python)."""
import io
import sys

P = 'AethonMod/Localization/en-US_Mods.AethonMod.hjson'
t = io.open(P, encoding='utf-8', newline='').read()
orig = t

# ---- 1. EL CÓDICE VIVO: el bloque entero muere (comentario de la purga) ----
viejo1 = (
    "\tCodiceVivo: {\n"
    "\t\t// v6.50.72 — THE USER'S TEST v2: a sprite born from CODE turned into\n"
    "\t\t// an ANIMATED weapon, completely separate from the rest (Tester's\n"
    "\t\t// Bag). The .71 animation read as static — this one BREATHES.\n"
    "\t\tDisplayName: The Living Codex\n"
    "\t\tTooltip:\n"
    "\t\t\t'''\n"
    "\t\t\t\"A book that drew itself — one line of code at a time.\"\n"
    "\t\t\tTHE TEST SPRITE: this codex has no hand-drawn sprite — it was BORN\n"
    "\t\t\tFROM CODE (the pixel matrix, 26 colors) and that's why it can\n"
    "\t\t\tANIMATE for real: THE WHOLE BOOK BREATHES, a WAVE of living ink\n"
    "\t\t\ttravels from its eye outwards, and THE EYE BLINKS SHUT — watch it\n"
    "\t\t\tin your inventory. When used, the codex LEAPS from your hands,\n"
    "\t\t\tFLIES TO WHERE YOU CLICK and hovers THERE, firing FOUR VOLLEYS of\n"
    "\t\t\tTHREE GUIDED VIOLET SPARKS each — enemies or no enemies, the\n"
    "\t\t\tattack ALWAYS shows — then returns to you like a boomerang.\n"
    "\t\t\t(A standalone test weapon — 100% code-born sprite.)\n"
    "\t\t\t'''\n"
    "\t}\n"
)
nuevo1 = (
    "\t// v6.50.91 — THE LIVING CODEX WAS DELETED ENTIRELY (the user's word:\n"
    "\t// \"El Codice Vivo lo puedes borrar por completo ya no es necesario\").\n"
    "\t// Its levitation — the book that leaves your hands and hovers — now\n"
    "\t// lives in the NERVOUS GRIMOIRE's spirit (hunt and escape).\n"
)
if t.count(viejo1) != 1:
    sys.exit(f'FALLO 1: bloque CodiceVivo encontrado {t.count(viejo1)} veces')
t = t.replace(viejo1, nuevo1)

# ---- 2. LOS PROYECTILES: mueren los del Códice, nacen los del Nervioso ----
viejo2 = (
    "\tCodiceVivoChispa.DisplayName: Codice Vivo Chispa\n"
    "\tCodiceVivoProjectile.DisplayName: Codice Vivo Projectile\n"
)
nuevo2 = (
    "\t// v6.50.91 — the Nervous One's spirit and its hunting shadow\n"
    "\tGrimorioNerviosoFlotante.DisplayName: The Nervous Grimoire\n"
    "\tSombraPaginaCaza.DisplayName: The Nervous Grimoire's Shadow of the Page\n"
)
if t.count(viejo2) != 1:
    sys.exit(f'FALLO 2: DisplayNames encontrados {t.count(viejo2)} veces')
t = t.replace(viejo2, nuevo2)

# ---- 3. HAMBRE: la voz del Nervioso (sub-bloque nuevo) ----
viejo3 = (
    "Hambre: {\n"
    "\tBarra: Hunger {0}\n"
    "\tCalmado: The eye watches you, serene. It blinks when it wants.\n"
    "\tInquieto: It blinks faster and faster… something is missing.\n"
    "\tFurioso: The iris ignites RED. The fury is coming.\n"
    "\tMaximo: IT IS HUNGRY. It never stops staring.\n"
    "\tReinicio: The grimoire's appetite has calmed down.\n"
    "}\n"
)
nuevo3 = (
    "Hambre: {\n"
    "\tBarra: Hunger {0}\n"
    "\tCalmado: The eye watches you, serene. It blinks when it wants.\n"
    "\tInquieto: It blinks faster and faster… something is missing.\n"
    "\tFurioso: The iris ignites RED. The fury is coming.\n"
    "\tMaximo: IT IS HUNGRY. It never stops staring.\n"
    "\tReinicio: The grimoire's appetite has calmed down.\n"
    "\t// v6.50.91 — THE NERVOUS ONE: restless, hungry, angry — \"I want to\n"
    "\t// eat and it's hungry\". Not fear: appetite with a voice.\n"
    "\tNervioso: {\n"
    "\t\tTranquilo: The hunter waits. It watches, patient, hungry.\n"
    "\t\tCazando: OUT HUNTING — it floats above you, seeking its own food.\n"
    "\t\tImpaciente: IMPATIENT — it's hungry and won't stop saying so.\n"
    "\t\tFugitivo: IT ESCAPED! Sick of hunger, it keeps away from you.\n"
    "\t\tFrase1: I'm hungry…\n"
    "\t\tFrase2: I want to eat. NOW.\n"
    "\t\tFrase3: Are you feeding me today… or what?\n"
    "\t\tFrase4: I'm starving! HUNGRY!\n"
    "\t\tFrase5: HUNGER! HUNGER! HUNGER!\n"
    "\t\tSaleACazar: If you won't feed me, I'll go hunt for myself.\n"
    "\t\tSaciado: …satiated. For now.\n"
    "\t\tEscapa: I've HAD IT! I'm off!\n"
    "\t\tAbsorbe: −1% hunger\n"
    "\t}\n"
    "}\n"
)
if t.count(viejo3) != 1:
    sys.exit(f'FALLO 3: bloque Hambre encontrado {t.count(viejo3)} veces')
t = t.replace(viejo3, nuevo3)

# ---- guardado con bytes EXACTOS (sin normalización) ----
io.open(P, 'w', encoding='utf-8', newline='').write(t)
print(f'OK: 3 parches aplicados | {len(orig)} → {len(t)} B '
      f'(delta {len(t) - len(orig):+d})')
print(f'tabs restantes: {t.count(chr(9))}')
