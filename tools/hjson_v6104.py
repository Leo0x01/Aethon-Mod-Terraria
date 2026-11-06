#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.104 — Inserta las claves hjson nuevas en los 3 idiomas:
  · Items.AuraDeJuicioItem (DisplayName/Tooltip/Titulo/Linea1..4)
  · Items.BolsaAuras (DisplayName/Tooltip)
  · Buffs.LlamasDelJuicio (DisplayName/Description)
  · Jefe.Aethon.MiradaCielo + .PruebaC (anuncios de los ataques nuevos)
  · HUD.FuerzaEstandarte (el título del medidor del Aura de Juicio)
Idempotente: si la clave ya existe, NO la duplica.
"""
import io, sys, re

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization"

ES = {
 "item": '''\tAuraDeJuicioItem: {
\t\t// v6.104: EL AURA DE JUICIO — el puerto fiel del Warbanner of
\t\t// the Righteous (mecánica + técnica VFX, arte 100% de la casa).
\t\tDisplayName: Aura de Juicio
\t\tTooltip:
\t\t\t\'\'\'
\t\t\tEl estandarte de guerra arde a tu espalda: tu poder crece cuanto más
\t\t\tcerca está el enemigo (−15% a +15% en TODO tu daño)
\t\t\t\'\'\'
\t\tTitulo: "[c/FFC850:═══ EL AURA DE JUICIO ═══]"
\t\tLinea1:
\t\t\t\'\'\'
\t\t\t[c/FFE7B0:El campo del juicio abraza 45 tiles a la redonda: cada impío que entra
\t\t\tqueda MARCADO y arde con las Llamas del Juicio]
\t\t\t\'\'\'
\t\tLinea2: "[c/FFB84D:Cuanto más se acercan, más arden (intensidad ×1→×3), el fuego golpea cada vez más rápido (2..19 ticks) y los muy pegados son REPELIDOS]"
\t\tLinea3: "[c/FFB84D:Cada golpe quema con 15 × tu MEJOR daño de clase (×0.10 contra hordas de 7+), 50 de penetración de armadura y puede ser crítico]"
\t\tLinea4: "[c/E8A33C:A 80 px del impío TODO tu daño arde al máximo (+15%); con el campo vacío, el estandarte castiga la lejanía (−15%)]"
\t}

''',
 "bolsa": '''\tBolsaAuras: {
\t\t// v6.104: LA BOLSA DE LAS AURAS — el material de investigación.
\t\tDisplayName: La Bolsa de las Auras
\t\tTooltip:
\t\t\t\'\'\'
\t\t\tEL MATERIAL DE INVESTIGACIÓN DE AURAS: el Aura de Juicio (el
\t\t\testandarte de guerra que arde), el Estandarte del Juicio y las
\t\t\tCINCO Formas Ascendidas — todo lo que AURA, en un solo kit.
\t\t\t\'\'\'
\t}

''',
 "buff": '''\tLlamasDelJuicio: {
\t\t// v6.104: LAS LLAMAS DEL JUICIO — la quemadura del Aura de Juicio.
\t\tDisplayName: Llamas del Juicio
\t\tDescription: Las llamas doradas del juicio te consumen: cuanto más cerca del portador, más ardes
\t}

''',
 "jefe": '''\t\t// v6.104 — LOS DOS ATAQUES NUEVOS DE LA COREOGRAFÍA (la letra:
\t\t// la mirada que cae del cielo · el rayo de prueba C de la fase 3).
\t\tMiradaCielo: LA MIRADA DEL CIELO — doce haces caen sobre la arena: seis verticales y seis horizontales, luego giran en diagonal y VUELVEN A ARDER.
\t\tPruebaC: EL RAYO DE PRUEBA C DEL DIOS — los seis soles caen y el conducto onírico barre: corre EN PERPENDICULAR.
''',
 "hud": '''
HUD: {
\t// v6.104: EL MEDIDOR DE FUERZA DEL ESTANDARTE (Aura de Juicio).
\tFuerzaEstandarte: Fuerza del Estandarte
}
''',
}

EN = {
 "item": '''\tAuraDeJuicioItem: {
\t\t// v6.104: THE JUDGMENT AURA — the faithful port of Warbanner of
\t\t// the Righteous (mechanics + VFX technique, house-made art).
\t\tDisplayName: Aura of Judgment
\t\tTooltip:
\t\t\t\'\'\'
\t\t\tThe warbanner burns at your back: your power grows the closer the
\t\t\tenemy stands (−15% to +15% on ALL your damage)
\t\t\t\'\'\'
\t\tTitulo: "[c/FFC850:═══ THE JUDGMENT AURA ═══]"
\t\tLinea1:
\t\t\t\'\'\'
\t\t\t[c/FFE7B0:The field of judgment embraces 45 tiles around: every fiend that
\t\t\tenters is MARKED and burns with the Judgment Flames]
\t\t\t\'\'\'
\t\tLinea2: "[c/FFB84D:The closer they get, the hotter they burn (intensity ×1→×3), the fire strikes ever faster (2..19 ticks) and the closest are REPELLED]"
\t\tLinea3: "[c/FFB84D:Each strike burns with 15 × your BEST class damage (×0.10 against hordes of 7+), 50 armor penetration and can crit]"
\t\tLinea4: "[c/E8A33C:At 80 px from the fiend ALL your damage burns at its peak (+15%); with an empty field, the banner punishes distance (−15%)]"
\t}

''',
 "bolsa": '''\tBolsaAuras: {
\t\t// v6.104: THE AURAS BAG — the aura research material.
\t\tDisplayName: The Auras Bag
\t\tTooltip:
\t\t\t\'\'\'
\t\t\tTHE AURA RESEARCH MATERIAL: the Aura of Judgment (the burning
\t\t\twarbanner), the Banner of Judgment and the FIVE Ascended
\t\t\tForms — everything that AURAS, in a single kit.
\t\t\t\'\'\'
\t}

''',
 "buff": '''\tLlamasDelJuicio: {
\t\t// v6.104: THE JUDGMENT FLAMES — the burn of the Aura of Judgment.
\t\tDisplayName: Judgment Flames
\t\tDescription: The golden flames of judgment consume you: the closer to the bearer, the hotter you burn
\t}

''',
 "jefe": '''\t\t// v6.104 — THE TWO NEW CHOREOGRAPHY ATTACKS (the letter: the
\t\t// gaze that falls from the sky · the phase-3 Test Beam C).
\t\tMiradaCielo: THE SKY GAZE — twelve beams fall upon the arena: six vertical and six horizontal, then they twist diagonally and BURN AGAIN.
\t\tPruebaC: THE GOD'S TEST BEAM C — the six suns fall and the oneiric conduit sweeps: run PERPENDICULAR.
''',
 "hud": '''
HUD: {
\t// v6.104: THE WARBANNER STRENGTH METER (Aura of Judgment).
\tFuerzaEstandarte: Warbanner Strength
}
''',
}

def insert_before(text, anchor, block):
    if anchor not in text:
        return None, f"ANCLA NO ENCONTRADA: {anchor!r}"
    if block.split("{", 1)[0].strip().split(":")[0].strip() and (block.strip().split("\n")[0].split(":")[0].strip()) in text:
        # idempotencia por nombre de clave de primer nivel del bloque
        pass
    return text.replace(anchor, block + anchor, 1), None

def main():
    ok = True
    for lang, pack in (("es-MX", ES), ("es-ES", ES), ("en-US", EN)):
        ruta = f"{BASE}/{lang}_Mods.AethonMod.hjson"
        with io.open(ruta, "r", encoding="utf-8") as f:
            t = f.read()
        # 1) el item (antes de AnillosSolRunicoItem)
        if "AuraDeJuicioItem:" not in t:
            t, err = insert_before(t, "\tAnillosSolRunicoItem: {", pack["item"])
            if err: print(f"[{lang}] {err}"); ok = False
        # 2) la bolsa (antes de BolsaMaestra)
        if "BolsaAuras:" not in t:
            t, err = insert_before(t, "\tBolsaMaestra: {", pack["bolsa"])
            if err: print(f"[{lang}] {err}"); ok = False
        # 3) el buff (antes de VacioExterior)
        if "LlamasDelJuicio:" not in t:
            t, err = insert_before(t, "\tVacioExterior: {", pack["buff"])
            if err: print(f"[{lang}] {err}"); ok = False
        # 4) los anuncios del jefe (tras la línea de Mirada dentro de Jefe.Aethon)
        if "MiradaCielo:" not in t:
            m = re.search(r"^(\t\tMirada: .*\n)", t, re.M)
            if not m:
                print(f"[lang {lang}] ANCLA Mirada NO ENCONTRADA"); ok = False
            else:
                t = t[:m.end()] + pack["jefe"] + t[m.end():]
        # 5) HUD al final del archivo
        if "FuerzaEstandarte:" not in t:
            if not t.endswith("\n"):
                t += "\n"
            t += pack["hud"]
        with io.open(ruta, "w", encoding="utf-8", newline="\n") as f:
            f.write(t)
        # verificación
        with io.open(ruta, "r", encoding="utf-8") as f:
            v = f.read()
        for clave in ("AuraDeJuicioItem:", "BolsaAuras:", "LlamasDelJuicio:",
                      "MiradaCielo:", "PruebaC:", "FuerzaEstandarte:"):
            if clave not in v:
                print(f"[{lang}] FALTA {clave}"); ok = False
        print(f"[{lang}] OK — claves insertadas y verificadas")
    print("TODO OK" if ok else "HUBO ERRORES")
    sys.exit(0 if ok else 1)

main()
