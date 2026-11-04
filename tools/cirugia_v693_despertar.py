#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.93 — LA CIRUGÍA DEL HJSON (Despertar + Altar + Estatuas).
La lección de la casa: los edits manuales sobre el hjson GRANDE expanden
tabs y destruyen el diff — esta cirugía toca SOLO las líneas de la misión,
con aserción de unicidad por corte (si algo no encaja, aborta sin escribir).
Indentación: TABS, como HEAD (2 tabs dentro de Hambre.Nervioso, 1 tab el resto).
"""
import io, sys

RUTA = "AethonMod/Localization/es-ES_Mods.AethonMod.hjson"

def cirugia(texto):
    cortes = []

    def corte(viejo, nuevo, nombre):
        nonlocal texto
        n = texto.count(viejo)
        assert n == 1, f"[{nombre}] esperaba 1 ocurrencia, hay {n}"
        cortes.append(nombre)
        texto = texto.replace(viejo, nuevo)

    # 1) Absorbe: el signo lo pone cada caso (v6.93 — la cura del «−+1%»).
    corte("\t\tAbsorbe: −{0}% hambre\n",
          "\t\tAbsorbe: {0}% hambre\n", "Absorbe")

    # 2) BocadoMonedas: al BANCO del despertar (retroactivo).
    corte("\tBocadoMonedas: ✦ El Eterno come las monedas del suelo — faltan {0}.\n",
          "\tBocadoMonedas: ✦ El Eterno devora las monedas del suelo (+{0} al banco del despertar).\n",
          "BocadoMonedas")

    # 3) BocadoMaterial (nuevo — el festín de materiales de la v6.93).
    corte("\tBocadoArma: ✦ ¡El Eterno devora EL ARMA del peldaño!\n",
          "\tBocadoArma: ✦ ¡El Eterno devora EL ARMA del peldaño!\n"
          "\tBocadoMaterial: ✦ El Eterno devora {0} × {1} del suelo (+{2} EXP al libro).\n",
          "BocadoMaterial")

    # 4) ComoEstatuas (nuevo — la ley de las estatuas en el ALTAR).
    corte("\tComoOleadas: • ¡Las OLEADAS DE HAMBRE suben la probabilidad!\n",
          "\tComoOleadas: • ¡Las OLEADAS DE HAMBRE suben la probabilidad!\n"
          "\tComoEstatuas: • Las ESTATUAS no alimentan: si el grimorio mata a una criatura nacida de una estatua, su hambre SUBE +0,1%\n",
          "ComoEstatuas")

    # 5) ReqJefeCaido / ReqJefeViva — la letra nueva de la v6.93.
    corte("\tReqJefeCaido: La presa del peldaño: {0} — DEVORADO\n",
          "\tReqJefeCaido: {0} — Devorado\n", "ReqJefeCaido")
    corte("\tReqJefeViva: La presa del peldaño: {0} — Todavía VIVO. El grimorio exige su caza\n",
          "\tReqJefeViva: {0} — todavía está vivo\n", "ReqJefeViva")

    # 6) Las claves nuevas de la LISTA (v6.93 — el formato de la letra).
    corte("\tReqJefeViva: {0} — todavía está vivo\n",
          "\tReqJefeViva: {0} — todavía está vivo\n"
          "\tReqJefeTitulo: El siguiente Jefe debe ser Devorado:\n"
          "\tReqDevorarTitulo: Devorar Armas o Materiales (al menos 2):\n"
          "\tReqOpcionBloqueada: (bloqueada)\n"
          "\tHitoTitulo: Próximo hito del Despertar:\n"
          "\tDanoGrimorio: +{0}% de daño a TODO el grimorio\n"
          "\tNotaFinal: (todo lo que el grimorio come se CONSAGRA en el Altar de la Consagración — menos las esencias — y se suelta DELANTE del jugador con el grimorio SELECCIONADO: el grimorio SOLO devora lo que su portador ARROJA al suelo)\n",
          "ClavesNuevas")

    # 7) Caida.MaterialDevorado (nuevo — el aviso del festín de materiales).
    corte("\tArmaDevorada: ✦ El Eterno devora a {0}: +{1} EXP · +1 {2}\n",
          "\tArmaDevorada: ✦ El Eterno devora a {0}: +{1} EXP · +1 {2}\n"
          "\tMaterialDevorado: ✦ El Eterno devora {0} ×{1}: +{2} EXP (material del festín)\n",
          "MaterialDevorado")

    return texto, cortes

def main():
    try:
        with io.open(RUTA, "r", encoding="utf-8", newline="") as f:
            texto = f.read()
    except Exception as e:
        print(f"ABORTA: no pude leer {RUTA}: {e}")
        return 1
    try:
        nuevo, cortes = cirugia(texto)
    except AssertionError as e:
        print(f"ABORTA (sin escribir): {e}")
        return 1
    if nuevo == texto:
        print("ABORTA: la cirugía no cambió nada")
        return 1
    with io.open(RUTA, "w", encoding="utf-8", newline="") as f:
        f.write(nuevo)
    print(f"CIRUGÍA v6.93 OK — {len(cortes)} cortes: {', '.join(cortes)}")
    return 0

if __name__ == "__main__":
    sys.exit(main())
