#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.56.9 — hjson tab-safe: añade LAS COPIAS DEL RC2 (RayoPruebaA/B/C +
RayosPruebaProjectile) al es-ES (LA FUENTE ÚNICA). La lección de la .56.8:
el es-ES solo se edita con scripts tab-safe (el editor expande tabs).
"""
import io, os, sys

RUTA = os.path.join(os.path.dirname(__file__), "..", "AethonMod", "Localization",
                    "es-ES_Mods.AethonMod.hjson")

T = "\t"

bloque_armas = (
    f"\n{T}RayoPruebaA: {{\n"
    f"{T}{T}DisplayName: Rayo de Prueba A — La Equilibrada\n"
    f"{T}{T}Tooltip:\n"
    f"{T}{T}{T}'''\n"
    f"{T}{T}{T}COPIA DE PRUEBA DEL RAYO CONCENTRADO 2 — RECETA A\n"
    f"{T}{T}{T}(el original queda intacto; esta copia se ajusta hasta\n"
    f"{T}{T}{T}dar con el look correcto). LA PILA DE CAPAS EQUILIBRADA:\n"
    f"{T}{T}{T}bruma ambiental ×2.6, envolvente de glow ×1.75, el plasma\n"
    f"{T}{T}{T}del shader y la vena núcleo ×0.34, con 5 soles de 4 puntas\n"
    f"{T}{T}{T}corriendo por las márgenes del conducto. La misma carga de\n"
    f"{T}{T}{T}300 ticks (los soles IMPLOTAN hacia la boca) y el mismo\n"
    f"{T}{T}{T}cono de 3500 px. Compara con B y C y elige la ganadora.\n"
    f"{T}{T}{T}'''\n"
    f"{T}}}\n"
    f"\n{T}RayoPruebaB: {{\n"
    f"{T}{T}DisplayName: Rayo de Prueba B — El Titán\n"
    f"{T}{T}Tooltip:\n"
    f"{T}{T}{T}'''\n"
    f"{T}{T}{T}COPIA DE PRUEBA DEL RAYO CONCENTRADO 2 — RECETA B\n"
    f"{T}{T}{T}(el original queda intacto). EL TITÁN: TODO al máximo —\n"
    f"{T}{T}{T}bruma ambiental ×3.1, envolvente de glow ×2.1, vena núcleo\n"
    f"{T}{T}{T}×0.42 casi blanca y 8 soles de 4 puntas. El haz más gordo\n"
    f"{T}{T}{T}y dramático de la familia: el conducto como una tormenta\n"
    f"{T}{T}{T}violeta. La misma conducta del RC2. Compara con A y C.\n"
    f"{T}{T}{T}'''\n"
    f"{T}}}\n"
    f"\n{T}RayoPruebaC: {{\n"
    f"{T}{T}DisplayName: Rayo de Prueba C — El Sueño\n"
    f"{T}{T}Tooltip:\n"
    f"{T}{T}{T}'''\n"
    f"{T}{T}{T}COPIA DE PRUEBA DEL RAYO CONCENTRADO 2 — RECETA C\n"
    f"{T}{T}{T}(el original queda intacto). EL SUEÑO: el desenfoque\n"
    f"{T}{T}{T}máximo — bruma ambiental ×3.6, DOBLE envolvente de glow\n"
    f"{T}{T}{T}(×2.5 + ×1.9), núcleo suave ×0.28 y 6 soles. El conducto\n"
    f"{T}{T}{T}onírico: todo fundido, como mirar el rayo a través de\n"
    f"{T}{T}{T}un velo. La misma conducta del RC2. Compara con A y B.\n"
    f"{T}{T}{T}'''\n"
    f"{T}}}\n"
)

linea_proyectil = f"{T}RayosPruebaProjectile.DisplayName: Los Rayos de Prueba del RC2\n"

with io.open(RUTA, "r", encoding="utf-8") as f:
    texto = f.read()

# 1) LOS BLOQUES DE ARMAS: insertar justo DESPUÉS del bloque RayoConcentradoDos
ancla_armas = "RayoConcentradoDos: {"
assert texto.count(ancla_armas) == 1, f"ancla armas: {texto.count(ancla_armas)}"
i0 = texto.index(ancla_armas)
# encontrar el cierre del bloque: la línea "\t}" que sigue
i1 = texto.index("\n\t}\n", i0) + len("\n\t}\n")
texto = texto[:i1] + bloque_armas + texto[i1:]

# 2) LA LÍNEA DEL PROYECTIL: después de RayoConcentradoDosProjectile.DisplayName
ancla_proj = "\tRayoConcentradoDosProjectile.DisplayName: "
assert texto.count(ancla_proj) == 1, f"ancla proj: {texto.count(ancla_proj)}"
ip = texto.index(ancla_proj)
ip2 = texto.index("\n", ip) + 1
texto = texto[:ip2] + linea_proyectil + texto[ip2:]

# 3) EL TOOLTIP DE LA BOLSA: mencionar las copias
viejo_bolsa = (
    f"\tBolsaDefinitivas: {{\n"
    f"\t\tDisplayName: La Bolsa de las Armas Definitivas\n"
    f"\t\tTooltip:\n"
    f"\t\t\t'''\n"
    f"\t\t\tEL RAYO CONCENTRADO 2 — el ataque definitivo de la casa.\n"
    f"\t\t\tEl plasma de la casa (carne fBm anti-picos), el gradiente\n"
    f"\t\t\tPRISMÁTICO que viaja y los soles rúnicos de la carga.\n"
    f"\t\t\t'''\n"
    f"\t}}\n"
)
nuevo_bolsa = (
    f"\tBolsaDefinitivas: {{\n"
    f"\t\tDisplayName: La Bolsa de las Armas Definitivas\n"
    f"\t\tTooltip:\n"
    f"\t\t\t'''\n"
    f"\t\t\tEL RAYO CONCENTRADO 2 — el ataque definitivo de la casa —\n"
    f"\t\t\tmás LAS TRES COPIAS DE PRUEBA de la pila de capas (A la\n"
    f"\t\t\tequilibrada · B el titán · C el sueño): el original queda\n"
    f"\t\t\tintacto y las copias se ajustan hasta el look correcto.\n"
    f"\t\t\t'''\n"
    f"\t}}\n"
)
assert texto.count(viejo_bolsa) == 1, f"ancla bolsa: {texto.count(viejo_bolsa)}"
texto = texto.replace(viejo_bolsa, nuevo_bolsa)

with io.open(RUTA, "w", encoding="utf-8", newline="") as f:
    f.write(texto)

print("OK: 3 armas + proyectil + bolsa en es-ES")
