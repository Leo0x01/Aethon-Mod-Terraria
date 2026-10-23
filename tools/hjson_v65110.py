#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.51.10 — cirugía hjson (3 idiomas):
   + Duende.Miedo1..4 / Duende.Burla1..3 (la escena del yunque)
   + Despertar.DevoraInventario (la devoración de la mochila)
   - Despertar.FaltaEsencia / FaltaDinero / FaltaMineral (los rechazos
     de los platos CAÍDOS murieron: la mesa se sirve del inventario)
"""
import io, sys

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"

TEXTO = {
"es-MX": {
"miedo": [
"Duende.Miedo1: ¡¿QUÉ ES ESO?! ¡¿QUÉ LLEVAS?! ¡Ni lo acerques al yunque! Ese libro me MIRÓ, ¿eh? ¡ME MIRÓ… Y PARPADEÓ!",
"Duende.Miedo2: Nononono. Yo reforjo METALES, no… no… ¿eso qué es? ¡No pienso tocarlo! La última cosa «viva» que toqué en la forja me dejó sin dos dedos ¡y sin el sueño!",
"Duende.Miedo3: ¡Lárgate con ese… esa COSA de mi taller! El yunque es para ACERO, no para lo que sea que ESO tenga por páginas… ¿Oíste eso? ¡Acaba de REÍRSE! ¡POR EL HIERRO SAGRADO!",
"Duende.Miedo4: Te lo digo de goblin a goblin: hay tintas que no se borran y libros que no se reparan… ¡y ESO es AMBAS COSAS! ¡Fuera! ¡FUERA DE MI TIENDA!",
],
"burla": [
"Duende.Burla1: «(El Eterno hojea, aburrido) ¿ESTE era el gran artesano? Un enano con un martillo de juguete… ni mi página índice lo teme.»",
"Duende.Burla2: «(El Eterno ríe entre páginas) ¿Reforjarme? ¡JA! Duende: tu yunque es la cuna… yo soy la bestia que se comió al herrero.»",
"Duende.Burla3: «(El Eterno susurra) Tiembla, chatarra barbuda: tu miedo es lo más sabio que harás hoy. Al menos el enanito sabe reconocer a sus mayores.»",
],
"devora": "\tDevoraInventario: ✦ El Eterno SE SIRVIÓ SOLO: la ofrenda del despertar {0} salió derecha de tu inventario.",
},
"es-ES": {
"miedo": [
"Duende.Miedo1: ¡¿QUÉ ES ESO?! ¡¿QUÉ LLEVAS?! ¡Ni lo acerques al yunque! Ese libro me MIRÓ, ¿eh? ¡ME MIRÓ… Y PARPADEÓ!",
"Duende.Miedo2: Nononono. Yo reforjo METALES, no… no… ¿eso qué es? ¡No pienso tocarlo! La última cosa «viva» que toqué en la forja me dejó sin dos dedos ¡y sin el sueño!",
"Duende.Miedo3: ¡Lárgate con ese… esa COSA de mi taller! El yunque es para ACERO, no para lo que sea que ESO tenga por páginas… ¿Oíste eso? ¡Acaba de REÍRSE! ¡POR EL HIERRO SAGRADO!",
"Duende.Miedo4: Te lo digo de goblin a goblin: hay tintas que no se borran y libros que no se reparan… ¡y ESO es AMBAS COSAS! ¡Fuera! ¡FUERA DE MI TIENDA!",
],
"burla": [
"Duende.Burla1: «(El Eterno hojea, aburrido) ¿ESTE era el gran artesano? Un enano con un martillo de juguete… ni mi página índice lo teme.»",
"Duende.Burla2: «(El Eterno ríe entre páginas) ¿Reforjarme? ¡JA! Duende: tu yunque es la cuna… yo soy la bestia que se comió al herrero.»",
"Duende.Burla3: «(El Eterno susurra) Tiembla, chatarra barbuda: tu miedo es lo más sabio que harás hoy. Al menos el enanito sabe reconocer a sus mayores.»",
],
"devora": "\tDevoraInventario: ✦ El Eterno SE SIRVIÓ SOLO: la ofrenda del despertar {0} salió derecha de tu inventario.",
},
"en-US": {
"miedo": [
"Duende.Miedo1: WHAT IS THAT?! WHAT ARE YOU CARRYING?! Don't you DARE bring it near my anvil! That book LOOKED at me, you hear? IT LOOKED AT ME… AND BLINKED!",
"Duende.Miedo2: No-no-no. I reforge METAL, not… not… what even IS that thing?! I'm not touching it! The last \u201calive\u201d thing I touched in this forge cost me two fingers AND my sleep!",
"Duende.Miedo3: Get that… that THING out of my shop! The anvil is for STEEL, not for whatever THAT has for pages… Did you hear that?! IT JUST LAUGHED! SACRED IRON, IT LAUGHED!",
"Duende.Miedo4: I'll tell you goblin to goblin: there are inks that never wash off and books that can't be fixed… and THAT THING IS BOTH! Out! OUT OF MY SHOP!",
],
"burla": [
"Duende.Burla1: «(The Eternal browses, bored) THIS was the great craftsman? A dwarf with a toy hammer… not even my index page fears him.»",
"Duende.Burla2: «(The Eternal laughs between its pages) Reforge ME?! HA! Tinkerer: your anvil is the cradle… I am the beast that ate the smith.»",
"Duende.Burla3: «(The Eternal whispers) Tremble, bearded scrap: your fear is the wisest thing you'll do today. At least the little man knows his betters.»",
],
"devora": "\tDevoraInventario: ✦ The Eternal HELPED ITSELF: the awakening {0} offering came straight out of your inventory.",
},
}

MUERTES = ["FaltaEsencia:", "FaltaDinero:", "FaltaMineral:"]

for lang, textos in TEXTO.items():
    ruta = BASE + lang + "_Mods.AethonMod.hjson"
    with io.open(ruta, encoding="utf-8") as f:
        lineas = f.readlines()

    # 1) ELIMINAR las claves muertas de los rechazos
    vivas = []
    muertas = 0
    for l in lineas:
        if any(k in l for k in MUERTES):
            muertas += 1
            continue
        vivas.append(l)
    lineas = vivas

    # 2) INSERTAR Duende.Miedo/Burla tras Duende.ArmaCorrompida
    idx = None
    for i, l in enumerate(lineas):
        if l.startswith("Duende.ArmaCorrompida:"):
            idx = i
            break
    assert idx is not None, f"{lang}: no encontré Duende.ArmaCorrompida"
    nuevas = [l + "\n" for l in textos["miedo"] + textos["burla"]]
    lineas[idx + 1:idx + 1] = nuevas

    # 3) INSERTAR Despertar.DevoraInventario tras EsenciaDevorada
    idx2 = None
    for i, l in enumerate(lineas):
        if l.startswith("\tEsenciaDevorada:"):
            idx2 = i
            break
    assert idx2 is not None, f"{lang}: no encontré EsenciaDevorada"
    lineas[idx2 + 1:idx2 + 1] = [textos["devora"] + "\n"]

    with io.open(ruta, "w", encoding="utf-8", newline="\n") as f:
        f.writelines(lineas)
    print(f"{lang}: +{len(nuevas)+1} claves nuevas, -{muertas} muertas")

print("OK")
