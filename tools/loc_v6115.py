#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
loc_v6115.py — LAS CLAVES NUEVAS DE LA v6.115 ×3 IDIOMAS (indentación
de ESPACIOS — la convención del hjson de la casa).

  · Items.MundoInteriorEternoItem — EL ÍTEM DE LA PUERTA.
  · Tiles.AltarPrismaTile.MapEntry — EL PRISMA DEL ETERNO.
  · Submundo.Devorado — el susurro del trago.
  · PrismaInterior.* — LA VENTANA CON BOTONES (volver / invocar).
"""
import io

BASE = "AethonMod/Localization/"

# ======================================================================
#  LOS TEXTOS POR IDIOMA (8/16 espacios — el estilo del archivo)
# ======================================================================
ES = {
    "item": """
        // v6.115 — EL ÍTEM DE LA PUERTA (la letra: «para que el altar nos
        // lleve a otro mundo crea un nuevo item que se fabrica con esencia
        // del eterno en el altar, cuando introduces 1000 esencias se fabrica
        // el item Mundo Interior del Eterno»). Cada etiqueta de color vive
        // en UNA sola línea física (el parser de Terraria lee por línea).
        MundoInteriorEternoItem: {
                DisplayName: Mundo Interior del Eterno
                Tooltip: "El arte de las mil esencias — el grimorio devora al portador y lo despierta dentro de su propio cuerpo"
                Titulo: "[c/B96EEA:═══ EL MUNDO INTERIOR DEL ETERNO ═══]"
                Linea1: "[c/D9B3FF:AL USARLO: el grimorio del Eterno desciende, se abre y TE DEVORA — sin daño: el trago es la puerta]"
                Linea2: "[c/D9B3FF:el mundo se llena de bruma… y al disiparse nace EL INTERIOR: el eclipse permanente de las oleadas, morado y lleno de bruma]"
                Linea3: "[c/78788C:EL PRISMA DE COLORES de la plaza te devuelve al mundo real — o INVOCÁ A AETHON desde su ventana de botones]"
        }
""",
    "tile": "        AltarPrismaTile.MapEntry: El Prisma del Eterno\n",
    "devorado": "        Devorado: EL GRIMORIO TE DEVORA — la bruma lo inunda todo…\n",
    "prisma": """
// v6.115 — LA VENTANA DEL PRISMA (la letra: «este tiene la capacidad
// de enviar de regreso al jugador y ademas de invocar a aethon usando
// una ventana con botones para seleccionar lo que se quiere hacer»).
PrismaInterior: {
        Titulo: EL PRISMA DEL ETERNO
        Subtitulo: el altar de colores del interior — elige tu camino
        BotonVolver: VOLVER AL MUNDO REAL
        BotonInvocar: INVOCAR A AETHON
        YaVive: AETHON YA CAMINA POR EL INTERIOR
        Leyenda: La llamada: el altar se materializa y EL GRIMORIO flota justo encima — de la ceremonia nacen los fragmentos
        NombreCorto: El Prisma del Eterno
}
""",
}

EN = {
    "item": """
        // v6.115 — THE DOOR ITEM (the letter: "so the altar takes us to
        // another world, create a new item crafted with Essence of the
        // Eternal at the altar — 1000 essences forge the Inner World of
        // the Eternal"). Every color tag lives on ONE physical line.
        MundoInteriorEternoItem: {
                DisplayName: Inner World of the Eternal
                Tooltip: "The art of a thousand essences — the grimoire devours its bearer and wakes him inside its own body"
                Titulo: "[c/B96EEA:═══ INNER WORLD OF THE ETERNAL ═══]"
                Linea1: "[c/D9B3FF:ON USE: the Eternal grimoire descends, opens and DEVOURS YOU — no damage: the swallow is the door]"
                Linea2: "[c/D9B3FF:the world fills with mist… and as it clears THE INTERIOR is born: the hunger waves' permanent eclipse, purple and full of mist]"
                Linea3: "[c/78788C:THE PRISM OF COLORS at the plaza returns you to the real world — or SUMMON AETHON from its button window]"
        }
""",
    "tile": "        AltarPrismaTile.MapEntry: The Prism of the Eternal\n",
    "devorado": "        Devorado: THE GRIMOIRE DEVOURS YOU — the mist floods everything…\n",
    "prisma": """
// v6.115 — THE PRISM WINDOW (the letter: "it can send the player back
// and also summon Aethon using a window with buttons to choose").
PrismaInterior: {
        Titulo: THE PRISM OF THE ETERNAL
        Subtitulo: the interior's altar of colors — choose your path
        BotonVolver: RETURN TO THE REAL WORLD
        BotonInvocar: SUMMON AETHON
        YaVive: AETHON ALREADY WALKS THE INTERIOR
        Leyenda: The calling: the altar materializes and THE GRIMOIRE floats just above — the shards are born of the ceremony
        NombreCorto: The Prism of the Eternal
}
""",
}

ANCHOR_ITEM_ES = "                                las ofanim y el aura del vacío… y el UNO que los viste todos.\n                        '''\n        }\n"
ANCHOR_ITEM_EN = "                                ophanim and the void aura… and the ONE wearing them all.\n                        '''\n        }\n"
ANCHOR_TILE = "        AltarOfrendasTile.MapEntry: "
ANCHOR_RED_ES = "        RedCaida: EL VACÍO TE DEVUELVE A LA PLAZA — nadie cae dentro del libro.\n}\n"
ANCHOR_RED_EN = "        RedCaida: THE VOID RETURNS YOU TO THE PLAZA — no one falls inside the book.\n}\n"


def aplicar(ruta, dic, ancla_item, ancla_red):
    with io.open(ruta, "r", encoding="utf-8") as f:
        txt = f.read()

    # 1 — EL ÍTEM tras la bolsa de la forma eterna.
    assert ancla_item in txt, "ancla item no hallada en " + ruta
    txt = txt.replace(ancla_item, ancla_item + dic["item"], 1)

    # 2 — EL MAP ENTRY del prisma tras el del altar.
    idx = txt.find(ANCHOR_TILE)
    assert idx >= 0, "ancla tiles no hallada en " + ruta
    fin = txt.find("\n", idx)
    txt = txt[:fin + 1] + dic["tile"] + txt[fin + 1:]

    # 3 — EL SUSURRO DEL TRAGO + LA VENTANA DEL PRISMA tras RedCaida.
    assert ancla_red in txt, "ancla submundo no hallada en " + ruta
    reemplazo = ancla_red.replace("}\n", "") + dic["devorado"] + "}\n" + dic["prisma"]
    txt = txt.replace(ancla_red, reemplazo, 1)

    with io.open(ruta, "w", encoding="utf-8") as f:
        f.write(txt)
    print("✓ " + ruta)


aplicar(BASE + "es-MX_Mods.AethonMod.hjson", ES, ANCHOR_ITEM_ES, ANCHOR_RED_ES)
aplicar(BASE + "es-ES_Mods.AethonMod.hjson", ES, ANCHOR_ITEM_ES, ANCHOR_RED_ES)
aplicar(BASE + "en-US_Mods.AethonMod.hjson", EN, ANCHOR_ITEM_EN, ANCHOR_RED_EN)
print("✓✓✓ las claves de la v6.115 viven en los TRES idiomas")
