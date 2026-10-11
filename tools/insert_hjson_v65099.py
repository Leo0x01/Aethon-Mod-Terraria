#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# v6.50.99 — INSERTA LAS CLAVES hjson DE LAS 4 ARMAS DE LA EMPERATRIZ
# (A: Grimorio Estrellado, B: Eco, C: Veredicto, 4: Equinoccio) + la
# bolsa 20 + los 2 proyectiles + la sección Desborde.
# Formato del hjson .98: ESPACIOS (8/16/24), no tabs.
import sys

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"

ES = {
    "armas": '''        GrimorioEstrellado: {
                // v6.50.99: OPCIÓN A — EL CLON (la recomendada del estudio §13)
                DisplayName: El Grimorio Estrellado
                Tooltip:
                        \'\'\'
                        «El sello se rompió; la Emperatriz prestó su streak.»
                        OPCIÓN A — EL CLON: cuatro Estelas de Aethon por uso con la
                        IA del streak de la Emperatriz replicada al 100% (fuente,
                        deriva, persecución, rebote) y la PALETA CÓSMICA del grimorio:
                        oro, cian, magenta e índigo con 20 estelas.
                        OBSERVADO: cada golpe marca a la presa — a 5 marcas, crítico
                        garantizado (+50% de daño).
                        DESBORDE: el 5º disparo seguido detona la estela MASIVA
                        (×2.5) y te cuesta 5% de tu vida máxima.
                        Sin maná (arma de pruebas).
                        \'\'\'
        }

        EcoEmperatriz: {
                // v6.50.99: OPCIÓN B — VANILLA + HUE ARREGLADO + FX DE LA CASA
                DisplayName: El Eco de la Emperatriz
                Tooltip:
                        \'\'\'
                        «Su ataque, intacto; su arcoíris, devuelto.»
                        OPCIÓN B — EL VANILLA: dispara el Nightglow de Terraria (el
                        mismo proyectil del Grimorio del Eterno) pero con EL HUE
                        ARREGLADO: la fórmula literal del arma original — cada ráfaga
                        nace con su color y la paleta rota entre usos. Luz prismática,
                        destellos y el anillo giratorio de la casa.
                        La IA es 100% de Terraria: no se toca, y ahí está la gracia.
                        Sin maná (arma de pruebas).
                        \'\'\'
        }

        VeredictoEmperatriz: {
                // v6.50.99: OPCIÓN C — EL HÍBRIDO DE LA JEFA (el 873, friendly)
                DisplayName: El Veredicto de la Emperatriz
                Tooltip:
                        \'\'\'
                        «El trazo de la jefa, ahora obedece.»
                        OPCIÓN C — EL HÍBRIDO DE LA JEFA: el streak 873 de la
                        Emperatriz en versión amistosa — 40 estelas, escala 1.4,
                        ATRAVIESA PAREDES y estalla en anillo radial al morir, con el
                        arcoíris completo de su ataque.
                        DESBORDE: el 5º disparo seguido lanza el TRAZO MASIVO (daño
                        ×2.5, explosión triple) y te cuesta 5% de tu vida máxima.
                        Sin maná (arma de pruebas).
                        \'\'\'
        }

        Equinoccio: {
                // v6.50.99: LA CUARTA ARMA — OPCIÓN B DUAL (el día + la noche)
                DisplayName: El Equinoccio
                Tooltip:
                        \'\'\'
                        «Mitad aurora, mitad eclipse.»
                        LA CUARTA — OPCIÓN B DUAL: dos Nightglow vanilla por disparo.
                        EL DORADO sale con LUZ de sol: halo dorado pulsante y ocho
                        rayos giratorios. EL NEGRO exhala BRUMA de verdad: disco de
                        eclipse con rim violeta y humo negro sobre todo su rastro.
                        El día y la noche se separan sobre tu cabeza… y luego cazan.
                        Sin maná (arma de pruebas).
                        \'\'\'
        }

        BolsaEmperatriz: {
                // v6.50.99: LA BOLSA DE LA EMPERATRIZ — bolsa 20 (las 4 pruebas del proyectil)
                DisplayName: Bolsa de la Emperatriz
                Tooltip:
                        \'\'\'
                        Las cuatro pruebas del proyectil de la Emperatriz de la Luz.
                        El clon cósmico con el sistema Observado/Desborde (El Grimorio
                        Estrellado), el vanilla con el hue arreglado (El Eco de la
                        Emperatriz), el híbrido de la jefa con el Desborde masivo (El
                        Veredicto de la Emperatriz) y el día y la noche en un libro
                        (El Equinoccio).
                        Clic derecho para desplegarlas. Solo entrega lo que te falte.
                        \'\'\'
        }
''',
    "proyectiles": '''        EstelaAethonProjectile.DisplayName: La Estela de Aethon
        TrazoEmperatrizProjectile.DisplayName: El Trazo de la Emperatriz
''',
    "desborde": '''Desborde: {
        // v6.50.99: LOS TEXTOS DEL SISTEMA OBSERVADO/DESBORDE — la familia de la Emperatriz
        Aviso: ¡EL GRIMORIO SE DESBORDA! El 5º disparo detona el proyectil masivo — la Emperatriz cobra 5% de tu vida máxima.
        Observado: ¡OBSERVADO!
}

''',
}

EN = {
    "armas": '''        GrimorioEstrellado: {
                // v6.50.99: OPTION A — THE CLONE (the study's recommended one)
                DisplayName: The Starry Grimoire
                Tooltip:
                        \'\'\'
                        "The seal broke; the Empress lent her streak."
                        OPTION A — THE CLONE: four Aethon Wakes per use with the
                        Empress's streak AI replicated 100% (fountain, drift, homing,
                        bounce) and the grimoire's COSMIC PALETTE: gold, cyan, magenta
                        and indigo with 20 afterimages.
                        OBSERVED: every hit marks the prey — at 5 marks, guaranteed
                        crit (+50% damage).
                        OVERFLOW: the 5th consecutive shot detonates the MASSIVE wake
                        (×2.5) and costs you 5% of your max life.
                        No mana (test weapon).
                        \'\'\'
        }

        EcoEmperatriz: {
                // v6.50.99: OPTION B — VANILLA + FIXED HUE + HOUSE FX
                DisplayName: The Empress's Echo
                Tooltip:
                        \'\'\'
                        "Her attack, untouched; her rainbow, returned."
                        OPTION B — THE VANILLA: fires Terraria's own Nightglow (the
                        same projectile as the Grimoire of the Eternal) but with THE
                        HUE FIXED: the original weapon's literal formula — every burst
                        is born with its color and the palette rotates between uses.
                        Prismatic light, sparkles and the house's spinning ring.
                        The AI is 100% Terraria's: untouched, and that's the point.
                        No mana (test weapon).
                        \'\'\'
        }

        VeredictoEmperatriz: {
                // v6.50.99: OPTION C — THE BOSS HYBRID (the 873, friendly)
                DisplayName: The Empress's Verdict
                Tooltip:
                        \'\'\'
                        "The boss's stroke, now it obeys."
                        OPTION C — THE BOSS HYBRID: the Empress's own 873 streak gone
                        friendly — 40 afterimages, 1.4 scale, PIERCES WALLS and bursts
                        into a radial ring on death, in the full rainbow of her attack.
                        OVERFLOW: the 5th consecutive shot launches the MASSIVE STROKE
                        (×2.5 damage, triple burst) and costs you 5% of your max life.
                        No mana (test weapon).
                        \'\'\'
        }

        Equinoccio: {
                // v6.50.99: THE FOURTH WEAPON — OPTION B, DUAL (day + night)
                DisplayName: The Equinox
                Tooltip:
                        \'\'\'
                        "Half dawn, half eclipse."
                        THE FOURTH — DUAL OPTION B: two vanilla Nightglows per shot.
                        THE GOLDEN one leaves with SUNLIGHT: pulsing golden halo and
                        eight spinning rays. THE BLACK one breathes real MIST: an
                        eclipse disc with a violet rim and black smoke over its whole
                        trail.
                        Day and night part ways above your head... then they hunt.
                        No mana (test weapon).
                        \'\'\'
        }

        BolsaEmperatriz: {
                // v6.50.99: THE EMPRESS BAG — bag 20 (the 4 projectile trials)
                DisplayName: Empress Bag
                Tooltip:
                        \'\'\'
                        The four trials of the Empress of Light's projectile.
                        The cosmic clone with the Observed/Overflow system (The Starry
                        Grimoire), the vanilla one with the fixed hue (The Empress's
                        Echo), the boss hybrid with the massive Overflow (The Empress's
                        Verdict) and day and night in one book (The Equinox).
                        Right-click to deploy them. Only delivers what you're missing.
                        \'\'\'
        }
''',
    "proyectiles": '''        EstelaAethonProjectile.DisplayName: The Aethon Wake
        TrazoEmperatrizProjectile.DisplayName: The Empress's Stroke
''',
    "desborde": '''Desborde: {
        // v6.50.99: THE OBSERVED/OVERFLOW SYSTEM TEXTS — the Empress weapons family
        Aviso: THE GRIMOIRE OVERFLOWS! The 5th shot detonates the massive projectile — the Empress takes 5% of your max life.
        Observado: OBSERVED!
}

''',
}


def insertar(nombre_archivo, paquete):
    ruta = BASE + nombre_archivo
    with open(ruta, "r", encoding="utf-8") as f:
        lineas = f.readlines()

    # 1) ARMAS + BOLSA: tras el bloque GrimorioSellado (la última entrada de Items)
    i = 0
    while i < len(lineas) and not lineas[i].startswith("        GrimorioSellado: {"):
        i += 1
    if i >= len(lineas):
        print(f"✗ {nombre_archivo}: no hallé GrimorioSellado"); sys.exit(1)
    while i < len(lineas) and lineas[i].rstrip("\n") != "        }":
        i += 1
    i += 1  # la línea tras el cierre del bloque
    lineas[i:i] = ["\n", paquete["armas"]]

    # 2) PROYECTILES: justo tras 'Projectiles: {'
    i = 0
    while i < len(lineas) and lineas[i].rstrip("\n") != "Projectiles: {":
        i += 1
    if i >= len(lineas):
        print(f"✗ {nombre_archivo}: no hallé Projectiles"); sys.exit(1)
    lineas[i + 1:i + 1] = [paquete["proyectiles"]]

    # 3) DESBORDE: justo antes de 'Configs: {'
    i = 0
    while i < len(lineas) and lineas[i].rstrip("\n") != "Configs: {":
        i += 1
    if i >= len(lineas):
        print(f"✗ {nombre_archivo}: no hallé Configs"); sys.exit(1)
    lineas[i:i] = [paquete["desborde"]]

    with open(ruta, "w", encoding="utf-8", newline="\n") as f:
        f.writelines(lineas)
    print(f"✓ {nombre_archivo}: armas + proyectiles + desborde insertados")


insertar("es-MX_Mods.AethonMod.hjson", ES)
insertar("es-ES_Mods.AethonMod.hjson", ES)
insertar("en-US_Mods.AethonMod.hjson", EN)
print("LOS TRES IDIOMAS LISTOS")
