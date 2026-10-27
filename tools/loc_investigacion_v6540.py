#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""loc_investigacion_v6540.py — v6.54.0 — LA LOCALIZACIÓN ×3 DE LA
INVESTIGACIÓN (bolsas nuevas, 6 items, 15 alas, 7 proyectiles).
Inserta antes del cierre de los bloques Items/Projectiles de cada hjson."""

import re

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"

def bloque_items(es):
    def T(clase, nombre, lineas, extras=()):
        c = "\t\t%s: {\n" % clase
        c += "\t\t\tDisplayName: %s\n" % nombre
        c += "\t\t\tTooltip:\n\t\t\t\t'''\n"
        c += "".join("\t\t\t\t\t%s\n" % l for l in lineas)
        c += "\t\t\t\t'''\n"
        for k, v in extras:
            c += "\t\t\t%s: %s\n" % (k, v)
        c += "\t\t}\n\n"
        return c

    PRUEBA_ES = ("Prueba", "Arma de pruebas — la receta es 10 de madera.")
    PRUEBA_EN = ("Prueba", "Test weapon — the recipe is 10 wood.")

    s = "\n\t\t// v6.54.0 — LA INVESTIGACIÓN DE LAS TEXTURAS DEL DIOS (Task 74-a/b/c)\n\n"

    if es:
        s += T("BolsaDeAlasPrisma", "La Bolsa de las Alas", [
            "Las QUINCE alas de código de la investigación: el mismo",
            "sistema de la Aurora de Tinta, cada una con SU paleta, SU",
            "tempo y SU ruido (fBm, wisps, starfield, seda, vermiforme,",
            "ridged, células, plasma). Vuelo 240, sin daño de caída.",
            "Clic derecho para desplegarlas. Solo entrega lo que falte."])
        s += T("BolsaInvestigacion", "La Bolsa de la Investigación", [
            "Los items nacidos del estudio de las texturas del dios:",
            "Rayos Concentrados, Trueno del Prisma, Lodo de Esencia,",
            "Cetro de Constelaciones, Estela del Cosmos y Tinta Viva —",
            "TODO re-escrito 100 % para el mod, cero material portado.",
            "Dentro viaja la Bolsa de las Alas.",
            "Clic derecho para desplegarla. Solo entrega lo que falte."])
        s += T("BolsaMaestra", "El Cofre del Arsenal", [
            "LAS VEINTITRÉS BOLSAS del arsenal dentro de una sola: la",
            "del Probador, la de los Fundacionales, la de los Soles",
            "Rúnicos… y todas las demás hasta la del dios.",
            "Ábrela y despliega la que necesites.",
            "Clic derecho. Solo entrega lo que te falte."])
        s += T("RayosConcentrados", "Rayos Concentrados", [
            "«Cuatro capas de luz multiplicadas: el rayo de fusión, tuyo.»",
            "LA técnica del ataque estrella del dios, 100 % re-escrita:",
            "el cuerpo de ruido, las chispas de doble muestreo, el perfil",
            "de brillo y el núcleo+falda — multiplicados por vértice,",
            "con el arcoíris corriendo y el núcleo blanco-caliente.",
            "Carga 45 t (los soles rúnicos crecen) y FUEGO: 2000 px",
            "de haz que respira. Daño por segmentos, como el original."],
            [PRUEBA_ES])
        s += T("TruenoDelPrisma", "El Trueno del Prisma", [
            "El relámpago ramificado del dios, nacido de nuevo: DOS",
            "muestras de la misma textura desfazadas 0.5 y multiplicadas",
            "(solo las vetas que coinciden sobreviven), la polilínea que",
            "se AQUIETA tras el impacto y el arcoíris desvaneciéndose.",
            "1 rayo grande + hasta 2 pequeños a los vecinos."],
            [PRUEBA_ES])
        s += T("LodoDeEsencia", "El Lodo de Esencia", [
            "Los charcos bioluminiscentes morados: la base oscura",
            "ondulando (warp senoidal) y cientos de puntos violetas",
            "latiendo encima. Los charcos SE HINCHAN al acumularse.",
            "Daña a quien los pise."],
            [PRUEBA_ES])
        s += T("CetroConstelaciones", "El Cetro de Constelaciones", [
            "Invoca estrellas que orbitan y se repelen; las cercanas se",
            "unen con LÍNEAS de daño real (la constelación del esbirro",
            "estelar del dios, 100 % casa). La primera dibuja la viñeta",
            "del área girando doble. Hasta 5 estrellas vivas."],
            [PRUEBA_ES])
        s += T("EstelaDelCosmos", "La Estela del Cosmos", [
            "El anillo tipo Saturno del esbirro estelar del dios, 100 %",
            "casa y MORADO/NEGRO: la banda con auto-interferencia (la",
            "misma textura a dos velocidades multiplicadas) y los",
            "canales como máscaras — cuerpo violeta, línea lila",
            "caliente, borde azul-violeta. Respira y gira tras de ti."],
            [("Titulo", "EL ANILLO SATURNO, MORADO Y NEGRO"),
             ("Linea1", "El anillo líquido del esbirro estelar del dios"),
             ("Linea2", "Puro adorno — accesorio o vanidad")])
        s += T("TintaViva", "La Tinta Viva", [
            "El cordón líquido del dios, hecho tuyo: DOS capas de tinta",
            "multiplicadas (el encaje de agujeros que se abre y cierra),",
            "el color nacido de la LUT negro→morado→lila y la onda que",
            "se encrespa. La cola sigue TU rastro real de movimiento."],
            [("Titulo", "EL CORDÓN LÍQUIDO DEL DIOS, HECHO TUYO"),
             ("Linea1", "La cola de goo irisado que sigue tu rastro"),
             ("Linea2", "Puro adorno — accesorio o vanidad")])
        s += "\t\tAlasPrismaBase: {\n\t\t\tEstilo: Alas de código — paleta, tempo y ruido propios\n\t\t}\n\n"
        alas = [
            ("AlasDeTintaSerena", "Alas de Tinta Serena",
             "El lila de la página en calma. Aleteo lento, plumas largas."),
            ("AlasDeEclipse", "Alas de Eclipse",
             "El morado profundo del cielo tapado. Plumas anchas, barrido fuerte."),
            ("AlasDePlasma", "Alas de Plasma",
             "Fucsia vibrante: aleteo nervioso y plumas cortas que titilan."),
            ("AlasDeNebulosa", "Alas de Nebulosa",
             "La nebulosa violeta fluye por las plumas — vuela entre estrellas."),
            ("AlasDeCometario", "Alas de Cometario",
             "La estela del cometa: blanco, cian y violeta corriendo."),
            ("AlasDeMedianoche", "Alas de Medianoche",
             "El cielo estrellado entero en cada pluma. Luz tenue."),
            ("AlasDelPrisma", "Alas del Prisma",
             "El arcoíris completo corre por el abanico."),
            ("AlasDeFuegoFrio", "Alas de Fuego Frío",
             "Ondas de hielo lila — curvas suaves que ondean despacio."),
            ("AlasDeTormenta", "Alas de Tormenta",
             "Violeta eléctrico: aleteo de trueno y puntas rasgadas."),
            ("AlasDeMarioneta", "Alas de Marioneta",
             "El carmesí de la Emperatriz. Filosas, plegadas, teatrales."),
            ("AlasDeAuroraArtica", "Alas de Aurora Ártica",
             "El telón verde-cian del norte — plumas larguísimas."),
            ("AlasDeOrquidea", "Alas de Orquídea",
             "El abanico rosa-magenta que se abre como una flor."),
            ("AlasDelVacio", "Alas del Vacío",
             "Negro absoluto con borde violeta. Planea — casi fantasmal."),
            ("AlasSolaresRunicas", "Alas Solares Rúnicas",
             "El oro de los soles del grimorio. Luz cálida y densa."),
            ("AlasDeSangreDeTinta", "Alas de Sangre de Tinta",
             "Magenta profundo que gotea células — la tinta que muerde."),
        ]
        for clase, nombre, desc in alas:
            s += T(clase, nombre, [desc,
                "Vuelo 240 · sin daño de caída · se pliega ante alas reales."])
    else:
        s += T("BolsaDeAlasPrisma", "The Wings Bag", [
            "The FIFTEEN code-born wings of the research: the same",
            "system as the Ink Aurora, each with ITS palette, tempo and",
            "noise (fBm, wisps, starfield, silk, vermiform, ridged,",
            "cells, plasma). 240 flight time, no fall damage.",
            "Right-click to deploy. Only delivers what you lack."])
        s += T("BolsaInvestigacion", "The Research Bag", [
            "The items born from studying the god's effect textures:",
            "Concentrated Rays, Prism Thunder, Essence Sludge,",
            "Constellation Scepter, Cosmic Trail and Living Ink — all",
            "rewritten 100 % for the mod, zero ported material.",
            "The Wings Bag rides inside.",
            "Right-click to deploy. Only delivers what you lack."])
        s += T("BolsaMaestra", "The Arsenal Chest", [
            "ALL TWENTY-THREE bags of the arsenal inside a single one:",
            "the Tester's, the Foundational, the Runic Suns'… every bag",
            "up to the god's. Open it and deploy the one you need.",
            "Right-click. Only delivers what you lack."])
        s += T("RayosConcentrados", "Concentrated Rays", [
            "\"Four layers of light multiplied: the fusion ray, yours.\"",
            "The god's signature attack technique, rewritten 100 %:",
            "the noise body, the double-sampled sparks, the glow profile",
            "and the core+skirt — multiplied per vertex, with the",
            "rainbow running and the white-hot core. 45 t of charge",
            "(the runic suns grow) then FIRE: 2000 px of breathing beam.",
            "Segment damage, just like the original."],
            [PRUEBA_EN])
        s += T("TruenoDelPrisma", "The Prism Thunder", [
            "The god's branching bolt, born anew: TWO samples of the",
            "same texture offset by 0.5 and multiplied (only the",
            "matching veins survive), the polyline that SETTLES after",
            "the strike, and the rainbow fading away. 1 big bolt + up",
            "to 2 small ones on the neighbors."],
            [PRUEBA_EN])
        s += T("LodoDeEsencia", "The Essence Sludge", [
            "The bioluminescent purple puddles: the dark base rippling",
            "(sine warp) with hundreds of violet dots pulsing on top.",
            "Puddles SWELL as they stack. Hurts whoever steps in them."],
            [PRUEBA_EN])
        s += T("CetroConstelaciones", "The Constellation Scepter", [
            "Summons stars that orbit and repel each other; the close",
            "ones join through damage-dealing LINES (the god's",
            "stellar minion's constellation, 100 % ours). The first one",
            "draws the double-spinning area vignette. Up to 5 stars."],
            [PRUEBA_EN])
        s += T("EstelaDelCosmos", "The Cosmic Trail", [
            "The Saturn-like ring of the god's stellar minion, 100 %",
            "ours and PURPLE/BLACK: the band with self-interference",
            "(the same texture at two speeds multiplied) and channels as",
            "masks — violet body, hot lilac line, blue-violet edge.",
            "It breathes and spins behind you."],
            [("Titulo", "THE SATURN RING, PURPLE AND BLACK"),
             ("Linea1", "The stellar minion's liquid ring"),
             ("Linea2", "Pure decoration — accessory or vanity")])
        s += T("TintaViva", "The Living Ink", [
            "The god's liquid cord, made yours: TWO ink layers",
            "multiplied (the lace of holes that opens and closes), the",
            "color born from the black→purple→lilac LUT, and the",
            "curling wave. The tail follows YOUR real trail of motion."],
            [("Titulo", "THE GOD'S LIQUID CORD, MADE YOURS"),
             ("Linea1", "The iridescent goo tail that follows your trail"),
             ("Linea2", "Pure decoration — accessory or vanity")])
        s += "\t\tAlasPrismaBase: {\n\t\t\tEstilo: Code-born wings — their own palette, tempo and noise\n\t\t}\n\n"
        alas = [
            ("AlasDeTintaSerena", "Serene Ink Wings",
             "The calm page's lilac. Slow flapping, long feathers."),
            ("AlasDeEclipse", "Eclipse Wings",
             "The deep purple of a covered sky. Wide feathers, strong sweep."),
            ("AlasDePlasma", "Plasma Wings",
             "Vibrant fuchsia: nervous flapping and short flickering feathers."),
            ("AlasDeNebulosa", "Nebula Wings",
             "The violet nebula flows through the feathers — fly among stars."),
            ("AlasDeCometario", "Comet Wings",
             "The comet's trail: white, cyan and violet running."),
            ("AlasDeMedianoche", "Midnight Wings",
             "The whole starry sky on every feather. Faint light."),
            ("AlasDelPrisma", "Wings of the Prisma",
             "The full rainbow runs across the fan."),
            ("AlasDeFuegoFrio", "Cold Fire Wings",
             "Waves of lilac ice — soft curves swaying slowly."),
            ("AlasDeTormenta", "Storm Wings",
             "Electric violet: thunder flapping and torn tips."),
            ("AlasDeMarioneta", "Puppet Wings",
             "The Empress's crimson. Sharp, folded, theatrical."),
            ("AlasDeAuroraArtica", "Arctic Aurora Wings",
             "The north's green-cyan curtain — very long feathers."),
            ("AlasDeOrquidea", "Orchid Wings",
             "The pink-magenta fan that opens like a flower."),
            ("AlasDelVacio", "Wings of the Void",
             "Absolute black with a violet edge. Glides — almost ghostly."),
            ("AlasSolaresRunicas", "Runic Solar Wings",
             "The gold of the grimoire's suns. Warm, dense light."),
            ("AlasDeSangreDeTinta", "Ink-Blood Wings",
             "Deep magenta dripping cells — the ink that bites."),
        ]
        for clase, nombre, desc in alas:
            s += T(clase, nombre, [desc,
                "240 flight time · no fall damage · folds before real wings."])
    return s

def bloque_projectiles(es):
    if es:
        filas = [
            ("RayosConcentradosHeld", "Los Rayos Concentrados"),
            ("TruenoPrismaRayo", "El Rayo del Trueno del Prisma"),
            ("LodoGlob", "El Globo de Lodo"),
            ("LodoCharco", "El Charco de Esencia"),
            ("EstrellaPrisma", "La Estrella del Prisma"),
            ("EstelaCosmosHalo", "La Estela del Cosmos"),
            ("TintaVivaHalo", "La Tinta Viva"),
        ]
    else:
        filas = [
            ("RayosConcentradosHeld", "The Concentrated Rays"),
            ("TruenoPrismaRayo", "The Prism Thunder Bolt"),
            ("LodoGlob", "The Sludge Glob"),
            ("LodoCharco", "The Essence Puddle"),
            ("EstrellaPrisma", "The Prisma Star"),
            ("EstelaCosmosHalo", "The Cosmic Trail"),
            ("TintaVivaHalo", "The Living Ink"),
        ]
    return "".join("\t\t%s.DisplayName: %s\n" % f for f in filas)

def insertar(ruta, es):
    with open(ruta, "r", encoding="utf-8") as fh:
        lineas = fh.readlines()
    out = list(lineas)
    # Los bloques anidados cierran con TAB — la primera línea '^}' tras
    # '^Items: {' ES el cierre del bloque (ídem Projectiles).
    def cierre(bloque):
        ini = next(i for i, l in enumerate(out)
                   if re.match(r"^%s: \{\s*$" % bloque, l))
        j = ini + 1
        while j < len(out) and not re.match(r"^\}", out[j]):
            j += 1
        return j
    j = cierre("Items")
    out.insert(j, bloque_items(es))
    j = cierre("Projectiles")
    out.insert(j, "\t\t// v6.54.0 — LA INVESTIGACIÓN\n")
    out.insert(j + 1, bloque_projectiles(es))
    with open(ruta, "w", encoding="utf-8") as fh:
        fh.writelines(out)
    print("  ✓ %s" % ruta.split("/")[-1])

if __name__ == "__main__":
    insertar(BASE + "es-MX_Mods.AethonMod.hjson", True)
    insertar(BASE + "es-ES_Mods.AethonMod.hjson", True)
    insertar(BASE + "en-US_Mods.AethonMod.hjson", False)
    print("LISTO")
