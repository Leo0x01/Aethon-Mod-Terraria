#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.50.68 — CIRUGÍA HJSON: jubilar a las Tres Hermanas (Marea/Mirada/Nido)
y dar de alta a los TRES CONCEPTOS NUEVOS (Pluma/Hoja/Sello) + los fixes
de La Sombra (raíz del jugador + boca→bruma devoradora) en los tooltips,
en los 3 idiomas (es-MX primario, es-ES espejo, en-US).

La lección de la v6.50.66: JAMÁS Edit/MultiEdit sobre el hjson (normaliza
tabs→espacios y lo destroza) — aquí TODO es reemplazo exacto con tabs
literales via Python, con verificación de llaves balanceadas al final.
"""
import re
import sys

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"

T = "\t"

# ======================================================================
#  LOS BLOQUES NUEVOS (español — es-MX y es-ES espejo)
# ======================================================================

PLUMA_ES = (
    f"{T}PlumaDeLaPagina: {{\n"
    f"{T}{T}// v6.50.68 — ARMA NUEVA 1 (reemplaza a La Marea de la Página — «conceptos diferentes», la letra del usuario)\n"
    f"{T}{T}DisplayName: La Pluma de la Página\n"
    f"{T}{T}Tooltip:\n"
    f"{T}{T}{T}'''\n"
    f"{T}{T}{T}«La página se escribe con lo que atraviesa.»\n"
    f"{T}{T}{T}LA PLUMA DEL ESCRIBA: cada disparo es un TRAZO DE TINTA —\n"
    f"{T}{T}{T}TRES AGUJAS DE HUESO en abanico estrecho, con guía suave\n"
    f"{T}{T}{T}(la tinta busca la palabra) y el RASTRO DE TINTA ondulando\n"
    f"{T}{T}{T}detrás del vuelo. Atraviesan hasta 4 enemigos; al clavarse\n"
    f"{T}{T}{T}SALPICAN: anillo violeta, bruma negra y gotas de tinta.\n"
    f"{T}{T}{T}Si tu tinta mata a un jefe: LA LLUVIA DE TINTA — el festín\n"
    f"{T}{T}{T}de las agujas que caen del cielo.\n"
    f"{T}{T}{T}(Concepto nuevo de las sombras — 100% código, ni un sprite.)\n"
    f"{T}{T}{T}'''\n"
    f"{T}}}"
)

HOJA_ES = (
    f"{T}HojaDeLaPagina: {{\n"
    f"{T}{T}// v6.50.68 — ARMA NUEVA 2 (reemplaza a La Mirada de la Página — «hoja»: página Y filo)\n"
    f"{T}{T}DisplayName: La Hoja de la Página\n"
    f"{T}{T}Tooltip:\n"
    f"{T}{T}{T}'''\n"
    f"{T}{T}{T}«Hoja» es página. «Hoja» es filo. Esta arma es las dos cosas.\n"
    f"{T}{T}{T}EL MOLINO: CUATRO HOJAS-FILO de sombra se materializan en\n"
    f"{T}{T}{T}bruma y ORBITAN a su portador, girando cada vez más rápido\n"
    f"{T}{T}{T}(cortan a cualquiera que se acerque)… y al llenarse, SE\n"
    f"{T}{T}{T}DISPARAN en abanico hacia el cursor, atravesiendo todo a su\n"
    f"{T}{T}{T}paso con estelas y su fantasma de giro detrás. La página,\n"
    f"{T}{T}{T}doblada hasta cortar.\n"
    f"{T}{T}{T}Si tu filo mata a un jefe: EL MOLINO DE FILOS — el festín\n"
    f"{T}{T}{T}del círculo que se ciñe.\n"
    f"{T}{T}{T}(Concepto nuevo de las sombras — 100% código, ni un sprite.)\n"
    f"{T}{T}{T}'''\n"
    f"{T}}}"
)

SELLO_ES = (
    f"{T}SelloDeLaPagina: {{\n"
    f"{T}{T}// v6.50.68 — ARMA NUEVA 3 (reemplaza a El Nido de la Página — el sello que aprisiona la página)\n"
    f"{T}{T}DisplayName: El Sello de la Página\n"
    f"{T}{T}Tooltip:\n"
    f"{T}{T}{T}'''\n"
    f"{T}{T}{T}«Todo contrato se firma dos veces: una en tinta, otra en sangre.»\n"
    f"{T}{T}{T}EL SELLO: no persigue ni perfora — MARCA EL TERRENO. El\n"
    f"{T}{T}{T}CÍRCULO RÚNICO se dibuja solo donde apuntas (anillos dobles\n"
    f"{T}{T}{T}contrarrotantes, marcas que parpadean, el piso de sombra)…\n"
    f"{T}{T}{T}y al completarse, ERUPCIONA: SEIS GARRAS brotan del perímetro\n"
    f"{T}{T}{T}hacia el centro y la bruma estalla. Después, el POSO de\n"
    f"{T}{T}{T}bruma negra queda MORDIENDO el terreno un buen rato.\n"
    f"{T}{T}{T}Si tu sello mata a un jefe: EL SELLO DEL JUICIO — el festín\n"
    f"{T}{T}{T}del círculo que se ciñe sobre el reo.\n"
    f"{T}{T}{T}(Concepto nuevo de las sombras — 100% código, ni un sprite.)\n"
    f"{T}{T}{T}'''\n"
    f"{T}}}"
)

# ======================================================================
#  LOS BLOQUES NUEVOS (inglés — en-US)
# ======================================================================

PLUMA_EN = (
    f"{T}PlumaDeLaPagina: {{\n"
    f"{T}{T}// v6.50.68 — NEW WEAPON 1 (replaces The Tide of the Page — \"different concepts\", the user's word)\n"
    f"{T}{T}DisplayName: The Quill of the Page\n"
    f"{T}{T}Tooltip:\n"
    f"{T}{T}{T}'''\n"
    f"{T}{T}{T}\"The page is written with what it pierces.\"\n"
    f"{T}{T}{T}THE SCRIBE'S QUILL: every shot is a STROKE OF INK — THREE\n"
    f"{T}{T}{T}BONE NEEDLES in a tight fan, with gentle homing (the ink\n"
    f"{T}{T}{T}seeks its word) and the INK TRAIL rippling behind the flight.\n"
    f"{T}{T}{T}They pierce up to 4 enemies; on impact they SPLATTER: violet\n"
    f"{T}{T}{T}ring, black mist and ink drops.\n"
    f"{T}{T}{T}If your ink kills a boss: THE INK RAIN — the feast of needles\n"
    f"{T}{T}{T}falling from the sky.\n"
    f"{T}{T}{T}(New shadow concept — 100% code, not a single sprite.)\n"
    f"{T}{T}{T}'''\n"
    f"{T}}}"
)

HOJA_EN = (
    f"{T}HojaDeLaPagina: {{\n"
    f"{T}{T}// v6.50.68 — NEW WEAPON 2 (replaces The Gaze of the Page — \"hoja\": page AND blade)\n"
    f"{T}{T}DisplayName: The Blade of the Page\n"
    f"{T}{T}Tooltip:\n"
    f"{T}{T}{T}'''\n"
    f"{T}{T}{T}A \"hoja\" is a page. A \"hoja\" is a blade. This weapon is both.\n"
    f"{T}{T}{T}THE MILL: FOUR SHADOW BLADES materialize in mist and ORBIT\n"
    f"{T}{T}{T}their bearer, spinning faster and faster (they cut anything\n"
    f"{T}{T}{T}that comes close)... and when the mill is full, IT FIRES: all\n"
    f"{T}{T}{T}four launch in a fan toward the cursor, piercing everything in\n"
    f"{T}{T}{T}their path with trails and their spinning ghost behind. The\n"
    f"{T}{T}{T}page, folded until it cuts.\n"
    f"{T}{T}{T}If your blade kills a boss: THE BLADE MILL — the feast of the\n"
    f"{T}{T}{T}circle that tightens.\n"
    f"{T}{T}{T}(New shadow concept — 100% code, not a single sprite.)\n"
    f"{T}{T}{T}'''\n"
    f"{T}}}"
)

SELLO_EN = (
    f"{T}SelloDeLaPagina: {{\n"
    f"{T}{T}// v6.50.68 — NEW WEAPON 3 (replaces The Nest of the Page — the seal that binds the page)\n"
    f"{T}{T}DisplayName: The Seal of the Page\n"
    f"{T}{T}Tooltip:\n"
    f"{T}{T}{T}'''\n"
    f"{T}{T}{T}\"Every contract is signed twice: once in ink, once in blood.\"\n"
    f"{T}{T}{T}THE SEAL: it doesn't chase or pierce — it MARKS THE GROUND.\n"
    f"{T}{T}{T}The RUNIC CIRCLE draws itself wherever you aim (counter-\n"
    f"{T}{T}{T}rotating twin rings, blinking marks, the shadow floor)...\n"
    f"{T}{T}{T}and when complete, IT ERUPTS: SIX CLAWS sprout from the rim\n"
    f"{T}{T}{T}toward the center and the mist bursts. Afterwards, the black\n"
    f"{T}{T}{T}mist POOL keeps BITING the ground for a good while.\n"
    f"{T}{T}{T}If your seal kills a boss: THE SEAL OF JUDGMENT — the feast of\n"
    f"{T}{T}{T}the circle that tightens over the condemned.\n"
    f"{T}{T}{T}(New shadow concept — 100% code, not a single sprite.)\n"
    f"{T}{T}{T}'''\n"
    f"{T}}}"
)

# ======================================================================
#  LOS FIXES DE LA SOMBRA (tooltip: raíz del jugador + boca→devorador)
# ======================================================================

SOMBRA_ES_VIEJO = (
    f"{T}{T}{T}ni un sprite: la masa, los ojos que se abren de golpe, los\n"
    f"{T}{T}{T}colmillos y la garganta, TODO dibujado por el motor.\n"
    f"{T}{T}{T}Nace de la SOMBRA DE TUS PIES, caza a cualquier distancia,\n"
    f"{T}{T}{T}muerde y drena — y el cuerpo entero y la boca EXHALAN\n"
    f"{T}{T}{T}BRUMA NEGRA. A 1 de vida: el festín.\n"
)
SOMBRA_ES_NUEVO = (
    f"{T}{T}{T}ni un sprite: la masa muscular, los ojos que se abren de\n"
    f"{T}{T}{T}golpe, la carne que fluye, TODO dibujado por el motor.\n"
    f"{T}{T}{T}Nace DE TU CUERPO — vueles o camines, la sombra va CONTIGO —,\n"
    f"{T}{T}{T}caza a cualquier distancia, muerde y drena: la punta se\n"
    f"{T}{T}{T}disuelve en BRUMA NEGRA y al clavarse, EL DEVORADOR — la masa\n"
    f"{T}{T}{T}que CUBRE ENTERO al jefe — traga con pulso mientras las almas\n"
    f"{T}{T}{T}vuelven a ti por el cuerpo. A 1 de vida: el festín.\n"
)

SOMBRA_EN_VIEJO = (
    f"{T}{T}{T}single sprite: the mass, the eyes that snap open, the\n"
    f"{T}{T}{T}fangs and the throat, ALL drawn by the engine.\n"
    f"{T}{T}{T}It rises from the SHADOW AT YOUR FEET, hunts at any\n"
    f"{T}{T}{T}distance, bites and drains — and the whole body and\n"
    f"{T}{T}{T}mouth exhale BLACK MIST. At 1 HP: the feast.\n"
)
SOMBRA_EN_NUEVO = (
    f"{T}{T}{T}single sprite: the muscled mass, the eyes that snap open,\n"
    f"{T}{T}{T}the flesh that flows, ALL drawn by the engine.\n"
    f"{T}{T}{T}It rises FROM YOUR OWN BODY — fly or walk, the shadow goes\n"
    f"{T}{T}{T}WITH YOU —, hunts at any distance, bites and drains: the tip\n"
    f"{T}{T}{T}dissolves into BLACK MIST, and on the bite THE DEVOURER — the\n"
    f"{T}{T}{T}mass that COVERS the whole boss — swallows in pulse while its\n"
    f"{T}{T}{T}souls travel back to you through the body. At 1 HP: the feast.\n"
)

# ======================================================================
#  LA BOLSA DE LAS SOMBRAS (los cuatro conceptos)
# ======================================================================

BOLSA_COMENTARIO_VIEJO = f"{T}{T}// v6.50.66: THE PAGE FAMILY — the 3 old weapons retired by the user; now the Shadow + its 3 SISTERS\n"
BOLSA_COMENTARIO_NUEVO = f"{T}{T}// v6.50.68: THE FOUR CONCEPTS — the 3 sisters retired by the user (\"different concepts\"); now the Shadow + Quill/Blade/Seal\n"

BOLSA_ES_VIEJO = (
    f"{T}{T}{T}La base intacta (La Sombra de la Página) y sus TRES HERMANAS:\n"
    f"{T}{T}{T}el océano de tres cabezas (La Marea), la pared de ojos con su\n"
    f"{T}{T}{T}ojo colosal (La Mirada) y la madre de huevos y garras (El\n"
    f"{T}{T}{T}Nido). Todas exhalan bruma negra.\n"
)
BOLSA_ES_NUEVO = (
    f"{T}{T}{T}La base intacta (La Sombra de la Página) y los TRES CONCEPTOS\n"
    f"{T}{T}{T}nuevos: el trazo de tinta que atraviesa (La Pluma), el molino\n"
    f"{T}{T}{T}de filos que orbita y se dispara (La Hoja) y el círculo rúnico\n"
    f"{T}{T}{T}que marca y reclama el terreno (El Sello). Todas exhalan\n"
    f"{T}{T}{T}bruma negra.\n"
)

BOLSA_EN_VIEJO = (
    f"{T}{T}{T}The untouched base (The Shadow of the Page) and its THREE\n"
    f"{T}{T}{T}SISTERS: the three-headed ocean (The Tide), the wall of eyes\n"
    f"{T}{T}{T}with its colossal eye (The Gaze) and the mother of eggs and\n"
    f"{T}{T}{T}claws (The Nest). All of them breathe black mist.\n"
)
BOLSA_EN_NUEVO = (
    f"{T}{T}{T}The untouched base (The Shadow of the Page) and the THREE NEW\n"
    f"{T}{T}{T}CONCEPTS: the ink stroke that pierces (The Quill), the blade\n"
    f"{T}{T}{T}mill that orbits and fires (The Blade) and the runic circle\n"
    f"{T}{T}{T}that marks and claims the ground (The Seal). All of them\n"
    f"{T}{T}{T}breathe black mist.\n"
)

# ======================================================================
#  LA CIRUGÍA
# ======================================================================

def reemplazo_exacto(texto, viejo, nuevo, etiqueta, contador):
    if viejo not in texto:
        print(f"  !! NO ENCONTRADO: {etiqueta}")
        sys.exit(1)
    if texto.count(viejo) != 1:
        print(f"  !! AMBIGUO ({texto.count(viejo)}x): {etiqueta}")
        sys.exit(1)
    contador[etiqueta] = 1
    return texto.replace(viejo, nuevo)


def reemplazo_bloque(texto, patron, nuevo, etiqueta, contador):
    m = re.search(patron, texto, re.DOTALL)
    if not m:
        print(f"  !! BLOQUE NO ENCONTRADO: {etiqueta}")
        sys.exit(1)
    contador[etiqueta] = 1
    return texto[:m.start()] + nuevo + texto[m.end():]


def operar(archivo, es):
    ruta = BASE + archivo
    with open(ruta, "r", encoding="utf-8") as f:
        texto = f.read()
    hecho = {}
    idioma = "es" if es else "en"

    # 1) LOS FIXES DE LA SOMBRA
    texto = reemplazo_exacto(texto,
        SOMBRA_ES_VIEJO if es else SOMBRA_EN_VIEJO,
        SOMBRA_ES_NUEVO if es else SOMBRA_EN_NUEVO,
        "Sombra-tooltip", hecho)

    # 2) LOS TRES ÍTEMS NUEVOS reemplazan a las hermanas (bloques completos)
    if es:
        texto = reemplazo_bloque(texto, r"\tMareaDeLaPagina: \{.*?\n\t\}", PLUMA_ES, "item-Pluma", hecho)
        texto = reemplazo_bloque(texto, r"\tMiradaDeLaPagina: \{.*?\n\t\}", HOJA_ES, "item-Hoja", hecho)
        texto = reemplazo_bloque(texto, r"\tNidoDeLaPagina: \{.*?\n\t\}", SELLO_ES, "item-Sello", hecho)
    else:
        texto = reemplazo_bloque(texto, r"\tMareaDeLaPagina: \{.*?\n\t\}", PLUMA_EN, "item-Pluma", hecho)
        texto = reemplazo_bloque(texto, r"\tMiradaDeLaPagina: \{.*?\n\t\}", HOJA_EN, "item-Hoja", hecho)
        texto = reemplazo_bloque(texto, r"\tNidoDeLaPagina: \{.*?\n\t\}", SELLO_EN, "item-Sello", hecho)

    # 3) LOS DISPLAYNAME DE LOS PROYECTILES
    proys = [
        ("MareaPaginaProjectile", "PlumaPaginaProjectile", "La Pluma de la Página" if es else "The Quill of the Page"),
        ("MiradaPaginaProjectile", "HojaPaginaProjectile", "La Hoja de la Página" if es else "The Blade of the Page"),
        ("NidoPaginaProjectile", "SelloPaginaProjectile", "El Sello de la Página" if es else "The Seal of the Page"),
    ]
    for viejo, nuevo, nombre in proys:
        patron = rf"\t{viejo}\.DisplayName: .*\n"
        m = re.search(patron, texto)
        if not m:
            print(f"  !! PROYECTIL NO ENCONTRADO: {viejo}")
            sys.exit(1)
        texto = texto[:m.start()] + f"{T}{nuevo}.DisplayName: {nombre}\n" + texto[m.end():]
        hecho[f"proy-{nuevo}"] = 1

    # 4) LA BOLSA
    texto = reemplazo_exacto(texto, BOLSA_COMENTARIO_VIEJO, BOLSA_COMENTARIO_NUEVO, "bolsa-comentario", hecho)
    texto = reemplazo_exacto(texto,
        BOLSA_ES_VIEJO if es else BOLSA_EN_VIEJO,
        BOLSA_ES_NUEVO if es else BOLSA_EN_NUEVO,
        "bolsa-tooltip", hecho)

    # 5) VERIFICACIONES (la lección de la .66: llaves balanceadas + sin muertos)
    for muerto in ("MareaDeLaPagina", "MiradaDeLaPagina", "NidoDeLaPagina",
                   "MareaPaginaProjectile", "MiradaPaginaProjectile", "NidoPaginaProjectile"):
        if muerto in texto:
            print(f"  !! RESIDUO VIVO: {muerto}")
            sys.exit(1)
    for vivo in ("PlumaDeLaPagina", "HojaDeLaPagina", "SelloDeLaPagina",
                 "PlumaPaginaProjectile", "HojaPaginaProjectile", "SelloPaginaProjectile"):
        if vivo not in texto:
            print(f"  !! NUEVO AUSENTE: {vivo}")
            sys.exit(1)
    abren = texto.count("{")
    cierran = texto.count("}")
    if abren != cierran:
        print(f"  !! LLAVES DESCUADRADAS: {{={abren} }}={cierran}")
        sys.exit(1)

    with open(ruta, "w", encoding="utf-8", newline="") as f:
        f.write(texto)
    print(f"  {archivo}: {len(hecho)} operaciones OK · llaves {abren}/{cierran} · cero residuos")


operar("es-MX_Mods.AethonMod.hjson", es=True)
operar("es-ES_Mods.AethonMod.hjson", es=True)
operar("en-US_Mods.AethonMod.hjson", es=False)
print("CIRUGÍA COMPLETA: 3/3 idiomas")
