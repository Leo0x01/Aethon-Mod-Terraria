#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.107 — LA CIRUGÍA DEL ANCLA (parte 3): rótulos de sección."""
import io, sys

RUTA = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Projectiles/AuraJuicio2Ancla.cs"
s = io.open(RUTA, encoding="utf-8").read()
orig = s

def reemplaza(viejo, nuevo, etiqueta):
    global s
    if viejo not in s:
        print(f"FALLO: {etiqueta} — no hallado")
        sys.exit(1)
    if s.count(viejo) != 1:
        print(f"FALLO: {etiqueta} — {s.count(viejo)} ocurrencias")
        sys.exit(1)
    s = s.replace(viejo, nuevo)
    print(f"OK: {etiqueta}")

reemplaza(
    """        //  EL TEATRO DE LOS IMPIOS — LAS PARTÍCULAS PEQUEÑAS Y LA BRUMA
        //  (fase de UPDATE: el spawn de polvos; la bruma y el semicírculo
        //  viven en el render).""",
    """        //  EL TEATRO DE LOS IMPIOS — LAS CHISPAS DEL CORAZÓN (fase de
        //  UPDATE: el spawn de polvos naciendo del corazón del impío; la
        //  bruma y la cúpula viven en el render).""",
    "rótulo teatro")

reemplaza(
    """        //  EL RENDER — NADA sobre el jugador: SOLO los impíos. LA BRUMA
        //  (los puffs de BrumaFX en la misma dirección e intensidad
        //  variable) y EL SEMICÍRCULO PERLIN (la media cúpula morada y
        //  negra con el difuminado circular en su parte plana).""",
    """        //  EL RENDER — NADA sobre el jugador: SOLO los impíos. LA BRUMA
        //  DEL CORAZÓN (los puffs de BrumaFX naciendo del corazón y
        //  derramándose hacia fuera, en la misma dirección e intensidad
        //  variable) y LA MEDIALUNA VERTICAL (la cúpula morada y negra
        //  que se corona sobre el impío).""",
    "rótulo render")

reemplaza(
    "        /// <summary>EL VESTIDO DE UN IMPIO — la bruma (misma dirección,\n        /// intensidad variable) y el SEMICÍRCULO PERLIN.</summary>",
    "        /// <summary>EL VESTIDO DE UN IMPIO — la bruma del corazón (misma\n        /// dirección e intensidad variable) y LA MEDIALUNA VERTICAL.</summary>",
    "rótulo vestido")

reemplaza(
    "                //     …Y SU ECO — una segunda medialuna más GRANDE y\n                //     TENUE (la aura que gotea del arco hacia afuera).",
    "                //     …Y SU ECO — una segunda medialuna más GRANDE y\n                //     TENUE (la aura que gotea de la cúpula hacia afuera).",
    "rótulo eco")

io.open(RUTA, "w", encoding="utf-8", newline="\n").write(s)
print(f"\nESCRITO: {len(orig)} → {len(s)} chars")
