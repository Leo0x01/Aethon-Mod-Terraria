#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.93 — LA CIRUGÍA DEL RENGLÓN FINAL (hjson).
El agente del arma nueva editó el es-ES a mano y la herramienta expandió
los TABS del archivo entero (la lección histórica de la casa). Esta
cirugía reinserta SUS tres cambios con la indentación TAB de HEAD:
  1) el bloque del ítem RenglonFinal (entre RayoPruebaC y BolsaDefinitivas),
  2) el Tooltip nuevo de BolsaDefinitivas (menciona El Renglón Final),
  3) los DisplayNames de sus dos proyectiles (tras los de la Mirada).
ASERCIÓN de unicidad por corte; aborta sin escribir si algo no encaja.
Correr DESPUÉS de cirugia_v693_mirada.py y ANTES de cirugia_v693_despertar.py.
"""
import io, sys

RUTA = "AethonMod/Localization/es-ES_Mods.AethonMod.hjson"

def cirugia(t):
    # 1) EL BLOQUE DEL ÍTEM — anclado entre el cierre de RayoPruebaC y
    #    la bolsa (la posición que eligió el autor del arma).
    viejo = "\t}\n\n\tBolsaDefinitivas: {"
    nuevo = "\t}\n\n\tRenglonFinal: {\n" \
        "\t\tDisplayName: El Renglón Final\n" \
        "\t\tTooltip:\n" \
        "\t\t\t'''\n" \
        "\t\t\tLA ÚLTIMA LÍNEA DEL GRIMORIO: mantén el clic en LA CITACIÓN\n" \
        "\t\t\t(0,6 s — el reglón fantasma trazado entre dos plumas estelares)\n" \
        "\t\t\ty el Eterno TIENDE EL RENGLÓN: una barra de tinta de 4.800 px\n" \
        "\t\t\tque te ATRAVIESA y da UNA VUELTA ENTERA a la página borrando\n" \
        "\t\t\ttodo lo que toque (daño sostenido, 4 s). La tinta corre con\n" \
        "\t\t\tgrumo dendrítico a dos octavas, el charco dorado arde en el\n" \
        "\t\t\tescritor, las plumas escupen chispas y la línea ondula viva.\n" \
        "\t\t\tAl cerrar se contrae y estalla: EL PUNTO FINAL.\n" \
        "\t\t\t'''\n" \
        "\t}\n" \
        "\n" \
        "\tBolsaDefinitivas: {"
    n = t.count(viejo)
    assert n == 1, f"[bloque RenglonFinal] esperaba 1, hay {n}"
    t = t.replace(viejo, nuevo, 1)

    # 2) EL TOOLTIP NUEVO DE LA BOLSA — el viejo (de HEAD) muere.
    viejo2 = ("\t\t\tEL RAYO CONCENTRADO 2 — el ataque definitivo de la casa — y\n"
              "\t\t\tLA COPIA GANADORA de la pila de capas: el RAYO DE PRUEBA C —\n"
              "\t\t\tEL SUEÑO (A y B murieron con la elección). C es el haz de la\n"
              "\t\t\tIRA DE UN SER EXTERIOR, el ataque del despertar 50 del Eterno.\n")
    nuevo2 = ("\t\t\tLOS ATAQUES DEFINITIVOS DE LA CASA: EL RAYO CONCENTRADO 2 (el\n"
              "\t\t\tcono de plasma de 3.500 px), EL SUEÑO (la copia ganadora — el\n"
              "\t\t\thaz de la Ira de un Ser Exterior) y EL RENGLÓN FINAL (la barra\n"
              "\t\t\tde tinta de 4.800 px que gira borrando la página entera).\n")
    n2 = t.count(viejo2)
    assert n2 == 1, f"[tooltip BolsaDefinitivas] esperaba 1, hay {n2}"
    t = t.replace(viejo2, nuevo2, 1)

    # 3) LOS DISPLAYNAMES DE LOS PROYECTILES — tras los de la Mirada
    #    (creados por cirugia_v693_mirada.py, que corre ANTES).
    viejo3 = "\tMiradaRayoProj.DisplayName: El Rayo de la Mirada\n"
    nuevo3 = viejo3 + \
        "\tRenglonCargaProj.DisplayName: La Citación del Renglón\n" \
        "\tRenglonLineaProj.DisplayName: El Renglón Final\n"
    n3 = t.count(viejo3)
    assert n3 == 1, f"[displaynames proyectiles] esperaba 1, hay {n3}"
    t = t.replace(viejo3, nuevo3, 1)
    return t

def main():
    with io.open(RUTA, "r", encoding="utf-8", newline="") as f:
        t = f.read()
    # idempotencia: si ya está aplicado, salir en silencio.
    if "\tRenglonFinal: {" in t and "\tRenglonLineaProj.DisplayName" in t:
        print("YA APLICADA: el bloque del Renglón vive — nada que hacer")
        return 0
    try:
        nuevo = cirugia(t)
    except AssertionError as e:
        print(f"ABORTA (sin escribir): {e}")
        return 1
    with io.open(RUTA, "w", encoding="utf-8", newline="") as f:
        f.write(nuevo)
    print("CIRUGÍA RENGLÓN OK — 3 cortes (bloque + tooltip bolsa + displaynames)")
    return 0

if __name__ == "__main__":
    sys.exit(main())
