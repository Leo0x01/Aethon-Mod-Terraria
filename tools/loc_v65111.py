#!/usr/bin/env python3
# v6.51.11 — LOCALIZATION SURGERY (3 idiomas en paralelo):
#  · CAIDA: 4 claves muertas fuera (ItemCorrompido, DevoradoCuenta,
#    ArmaCorrompidaAviso, DevoradoTooltipOjos) + DevoradoTooltip
#    re-escrita a 2 args (el festín del PORTADOR).
#  · LEVELUPTESTER: la Migracion mal ubicada fuera (es-ES comentada,
#    en-US viva) — vive en DESPERTAR.
#  · DESPERTAR: 2 muertas fuera (EsenciaDevorada, DevoraInventario) +
#    el bloque nuevo v6.51.11 (Migracion re-pariente + 8 claves nuevas
#    de LA MESA DEL SUELO y EL FESTÍN DEL PORTADOR).
import re, sys

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"

DEAD_CAIDA = ("ItemCorrompido:", "DevoradoCuenta:", "ArmaCorrompidaAviso:", "DevoradoTooltipOjos:")
DEAD_DESPERTAR = ("EsenciaDevorada:", "DevoraInventario:")

TOOLTIP_ES = "\tDevoradoTooltip: Festín del portador: {0} esencias corruptas comidas (+{1}% de daño a TODO tu ataque)"
TOOLTIP_EN = "\tDevoradoTooltip: Bearer's feast: {0} corrupt essences eaten (+{1}% damage to ALL your attacks)"

COMENT_ES = [
    "\t// v6.51.11 — LA MESA DEL SUELO + EL FESTÍN DEL PORTADOR: el libro",
    "\t// come de los items CAÍDOS delante de él (con el Eterno en la mano),",
    "\t// SOLO lo que le falta del peldaño; las esencias que EL JUGADOR come",
    "\t// suben SU propio daño (+0,1% c/u, tope +100% con 1000 — al tope,",
    "\t// cada esencia comida es 1 moneda de oro).",
]
COMENT_EN = [
    "\t// v6.51.11 — THE GROUND TABLE + THE BEARER'S FEAST: the book eats",
    "\t// from items DROPPED before it (with the Eternal in hand), ONLY what",
    "\t// the rung still needs; the essences THE PLAYER eats raise THEIR own",
    "\t// damage (+0.1% each, cap +100% at 1000 — at the cap, every essence",
    "\t// eaten becomes 1 gold coin).",
]

CLAVES_ES = [
    "\tMigracion: ✦ El despertar y el festín ahora viven EN ESTE grimorio (cada libro, su propia escalera).",
    "\tBocadoEsencias: ✦ El Eterno come {0} esencias del suelo ({1}/{2} del peldaño).",
    "\tBocadoMonedas: ✦ El Eterno come las monedas del suelo — faltan {0}.",
    "\tBocadoMineral: ✦ El Eterno come {0} lingotes del suelo ({1}/{2}).",
    "\tBocadoArma: ✦ ¡El Eterno devora EL ARMA CORRUPTA del peldaño!",
    "\tMigracionPoder: ✦ El viejo poder devorado ({0} esencias) pasa a su portador: +{1}% de daño a TODO.",
    "\tTopeEsencias: ✧ ¡TOPE DEL FESTÍN: +{0}% de daño PARA SIEMPRE! Desde ahora, cada esencia comida es 1 moneda de oro.",
    "\tEsenciaTopeTooltip: Festín completo: +100% de daño — cada esencia comida se vuelve 1 moneda de oro",
    "\tEsenciaProgresoTooltip: Festín del portador: {0}/{1} esencias comidas (+{2}% de daño a TODO)",
]
CLAVES_EN = [
    "\tMigracion: ✦ The awakening and the feast now live IN THIS grimoire (each book, its own ladder).",
    "\tBocadoEsencias: ✦ The Eternal eats {0} essences off the ground ({1}/{2} of the rung).",
    "\tBocadoMonedas: ✦ The Eternal eats the coins off the ground — {0} still missing.",
    "\tBocadoMineral: ✦ The Eternal eats {0} bars off the ground ({1}/{2}).",
    "\tBocadoArma: ✦ The Eternal devours THE CORRUPTED WEAPON of the rung!",
    "\tMigracionPoder: ✦ The old devoured power ({0} essences) passes to its bearer: +{1}% damage to ALL.",
    "\tTopeEsencias: ✧ FEAST CAP REACHED: +{0}% damage FOREVER! From now on, every essence eaten becomes 1 gold coin.",
    "\tEsenciaTopeTooltip: Feast complete: +100% damage — every essence eaten becomes 1 gold coin",
    "\tEsenciaProgresoTooltip: Bearer's feast: {0}/{1} essences eaten (+{2}% damage to ALL)",
]


def process(nombre, lang):
    ruta = BASE + nombre
    with open(ruta, encoding="utf-8") as f:
        lineas = f.read().split("\n")

    es = lang.startswith("es")
    bloque = (COMENT_ES + CLAVES_ES) if es else (COMENT_EN + CLAVES_EN)
    if lang == "es-MX":
        bloque = COMENT_ES + CLAVES_ES[1:]   # Migracion ya vive arriba en Despertar

    out, seccion, insertado = [], None, False
    bajas = {"caida": 0, "lut": 0, "despertar": 0, "tooltip": 0}
    for ln in lineas:
        m = re.match(r"^([A-Za-z0-9_.]+): \{$", ln)
        if m:
            seccion = m.group(1)
        st = ln.lstrip()
        viva = ln.startswith("\t") and not st.startswith("//")

        if seccion == "Caida":
            if viva and st.startswith(DEAD_CAIDA):
                bajas["caida"] += 1
                continue
            if viva and st.startswith("DevoradoTooltip:"):
                out.append(TOOLTIP_ES if es else TOOLTIP_EN)
                bajas["tooltip"] += 1
                continue
        if seccion == "LevelUpTester":
            if st.startswith("Migracion:") or st.startswith("// Migracion:"):
                bajas["lut"] += 1
                continue
        if seccion == "Despertar":
            if viva and st.startswith(DEAD_DESPERTAR):
                bajas["despertar"] += 1
                continue
        out.append(ln)
        if seccion == "Despertar" and st.startswith("CerrojoEsbirros:") and not insertado:
            out.extend(bloque)
            insertado = True

    with open(ruta, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(out))
    print(f"{nombre}: caida_bajas={bajas['caida']} tooltip={bajas['tooltip']} "
          f"lut_migracion={bajas['lut']} despertar_bajas={bajas['despertar']} "
          f"insertado={'SI' if insertado else 'NO'}")


process("es-ES_Mods.AethonMod.hjson", "es-ES")
process("es-MX_Mods.AethonMod.hjson", "es-MX")
process("en-US_Mods.AethonMod.hjson", "en-US")
print("OK")
