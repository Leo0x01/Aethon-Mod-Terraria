#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# v6.50.127 — LOCALIZATION SURGERY (es-MX): espejo del es-ES (la convención
# de la .66) + sus 3 purgas propias de «niños» + Reclamos con el sabor MX.
import io, sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from hjson_v650127_es import MENOR, DORMIDO, GENERICOS_M4_NUEVA

RUTA = "AethonMod/Localization/es-MX_Mods.AethonMod.hjson"

# Reclamos es-MX: el ritmo local, las mordidas del LIBRO (no de la cría de luz)
RECLAMOS = {
"Guide": "¿Sí? Ah, eres tú. Tu Grimorio Menor me mordió el tobillo mientras revisaba mis mapas. Dos veces. La segunda dolió MÁS, que quede constancia. Qué cosa tan... decidida.",
"Merchant": "Antes de hablar de precios: el Grimorio Menor me debe CINCO monedas por concepto de mordiscos. Nada personal. Aquí todo se cobra. Hasta la tinta de tu libro se factura.",
"Nurse": "¡Tú, exacto! Si el Grimorio Menor me vuelve a morder en plena curación, la próxima inyección la pones TÚ. Conmigo de tutora. Y sin anestesia. ¿Quedamos?",
"ArmsDealer": "Mirá, pibe. Tu mini libro me mordió mientras recargaba. Si mi escopeta se hubiera asustado, el pueblo estrena agujero nuevo y la culpa es tuya. Ponle freno.",
"Dryad": "La naturaleza perdona; yo, casi. Pero el Grimorio Menor me mordisqueó el brazo y mis hierbas se RIERON. ¿Las plantas se ríen? Antes de hoy, no.",
"Demolitionist": "¡El Grimorio Menor me mordió mientras sostenía dinamita! ¡DINAMITA! ¡Un estornudo y volamos el barrio entero! ¡Enséñale modales o le enseño balística, ja, es broma! ...¡Es broma!",
"Clothier": "Ay, muchacho... a mi edad los huesos ya crujen solos, sin ayuda de nadie. El Grimorio Menor me mordisqueó la pierna. Dile que este sastre ya no tiene... ¿cómo se dice? ¡Ah, sí! Juguito.",
"Goblin": "Escuchame bien, humano: si el Grimorio Menor vuelve a entrar a mi taller, lo refactoreo a MORDISCOS. Con garantía. Sin garantía. ¡SIN garantía, me lees la ira?!",
"Wizard": "*suspiro* Cuando yo era aprendiz, los grimorios respetaban a sus mayores... y sus pantorrillas. El tuyo, el Menor, probó las mías. Salado el mago viejo, ¿verdad, libro descarado?",
"Mechanic": "El Grimorio Menor se echó en mi mesa de trabajo. ENCIMA del circuito nuevo. ¿Sabes lo que hace la tinta de página en el cobre? Pues yo tampoco, ¡y ODIO no saber! ¡ODIO NO SABER!",
"Santa": "Jo, jo... jo. No. Nada de regalos para ese libro hasta enero. Y mira que lo pesqué masticando MI lista de traviesos. ¡La MÍA! ¡Con lo que cuesta el papel bueno!",
"Truffle": "¡Me olfatea como plato principal y ahora también MORDERÍA, amigo! ¡Los hongos hablantes NO se comen! ¡La palabra clave es HAB-LAN-TE! ¡Enséñasela!",
"Steampunker": "Tu Menor jugó con mis engranajes y ahora uno gira al revés. Al REVÉS, y sonriendo. ¿Sonríen los engranajes? ESE sí. Y no me deja desmontarlo. ¡Es MÍO, es de MI caja de herramientas!",
"DyeTrader": "¡El Grimorio Menor volteó mis tintes! ¡Morado el suelo, morado el gato, MORADO YO! ¿Y sabes qué? Me resalta los ojos. ¡PERO ESO NO IMPORTA: eran CUATRO barriles!",
"PartyGirl": "Amo a tu mini libro, ¿sabes? Lo AMO. Pero me mordió EN PLENA FIESTA. ¡Las luces se apagaron de la vergüenza, y las luces ni siquiera tienen CARA! ¡Ni idea de cómo lo hizo!",
"Cyborg": "Registré diez impactos de 1 unidad de daño. DIEZ. Precisión de datos: diez. Mi protocolo de agravios está ACTIVO, hermano. El mantenimiento de mi buen humor te lo facturo a TI.",
"Painter": "El Grimorio Menor cruzó mi estudio y mis lienzos amanecieron con... ¿dientecitos de tinta? Es arte. Arte MOLESTO, pero arte. ¡La próxima vez que se inscriba de musa, como todo el mundo!",
"WitchDoctor": "Los espíritus se rieron cuando el Menor me mordió. Se RIERON, muchacho. ¿Sabes lo que es que los MUERTOS se rían de ti en tu propia cara? Tampoco quiero que lo sepas. Paga la ofrenda.",
"Pirate": "¡Ningún cañón me ha cañoneado como tu Grimorio Menor! ¡Diez mordiscos, los conté como cuento mis monedas! ¡Y NINGUNO en el buen sentido! ¡Yaaar... de rabia!",
"Stylist": "¡Me mordió a mitad de un corte de pelo! ¡Del CLIENTE! El cliente no vuelve, el peinado quedó perfecto (obra mía, obvio) y el pánico me lo dejaron de propina. ¡PARA MÍ!",
"Angler": "¡El Grimorio Menor me mordió y me caí al lago! ¡YO! ¡Que nunca me caigo al lago, el lago se caía CONMIGO! ¡Glu! ¡Eso no se olvida, libro traidor!",
"Tavernkeep": "¡Ese Grimorio Menor tuyo me vació una jarra a mordiscos! ¿Sabes lo que es eso? ¡Ni en la guerra eterna se tocaban las jarras, compadre! ¡Allí se moría por MENOS!",
"Golfer": "¡El Grimorio Menor robó mi bola en el hoyo 9! ¡EN PLENO SWING! ¿Sabes lo que es un birdie perdido? ¿SABES LO QUE ES? ¡Ni el viento me ha hecho eso, y el viento es MI ENEMIGO!",
"Zoologist": "Anoté diez mordiscos del Menor en mi bestiario. DIEZ. Con hora y todo, ¡que soy PROFESIONAL! Más te vale que su página no cambie de sección, ¿me explico?",
"Princess": "Como princesa debo ser amable, así que lo digo con muchísimo cariño: controla a tu Grimorio Menor, ¿sí? ¡Porfa! ...Dicho esto con TODO el afecto diplomático disponible. ¡Que es mucho!",
"Genericos": "Ah, eres tú. Mira, sobre el Grimorio Menor... dile que el barrio ya empezó a contar. SABEMOS lo que hizo. Solo estamos siendo educados al respecto. Por ahora.",
}

# Purgas es-MX: Genericos.Mascota4 + Zoologist «desde niña» + Angler «a un niño»
GEN_M4_MX = ("Tu lucecita juega a las escondidas con los espantapájaros del sembradío. Nunca la encuentran: "
"se esconde en sus SOMBRAS, ¿sabes? Las sombras se quedan muy quietitas cuando ella está dentro. "
"¡Qué juego tan bonito!... ¿Verdad? ...¿Verdad?")

def leer():
    with io.open(RUTA, "r", encoding="utf-8-sig") as f:
        return f.read().split("\n")

def escribir(lineas):
    with io.open(RUTA, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lineas))
    print("ESCRITO:", RUTA)

def indent_of(linea):
    return linea[:len(linea) - len(linea.lstrip())] if linea.strip() else ""

def main():
    lineas = leer()
    n0 = len(lineas)

    # --- (1) GrimorioMenor1-4 (espejo del es-ES) tras el Grimorio4 ---
    for clave, textos in MENOR.items():
        idx_bloque = None
        for i, l in enumerate(lineas):
            if l.strip() == f"{clave}: {{":
                idx_bloque = i
                break
        if idx_bloque is None:
            raise SystemExit(f"BLOQUE NO ENCONTRADO: {clave}")
        fin = None
        for j in range(idx_bloque + 1, len(lineas)):
            if lineas[j].strip() == "}":
                fin = j
                break
        ancla = None
        for j in range(idx_bloque + 1, fin):
            if lineas[j].strip().startswith("Grimorio4:"):
                ancla = j
                break
        if ancla is None:
            raise SystemExit(f"Grimorio4 NO ENCONTRADO en {clave}")
        ind = indent_of(lineas[ancla])
        nuevas = [f"{ind}GrimorioMenor{k+1}: {t}" for k, t in enumerate(textos)]
        lineas = lineas[:ancla+1] + nuevas + lineas[ancla+1:]
    print("(1) GrimorioMenor1-4 x", len(MENOR), "bloques: OK")

    # --- (2) Reclamos es-MX ---
    idx_recl = None
    for i, l in enumerate(lineas):
        if l.strip() == "Reclamos: {":
            idx_recl = i
            break
    if idx_recl is None:
        raise SystemExit("BLOQUE Reclamos NO ENCONTRADO")
    fin_recl = None
    for j in range(idx_recl + 1, len(lineas)):
        if lineas[j].strip() == "}":
            fin_recl = j
            break
    for clave, texto in RECLAMOS.items():
        hecho = False
        for j in range(idx_recl + 1, fin_recl):
            if lineas[j].strip().startswith(f"{clave}: "):
                ind = indent_of(lineas[j])
                lineas[j] = f"{ind}{clave}: {texto}"
                hecho = True
                break
        if not hecho:
            raise SystemExit(f"RECLAMO NO ENCONTRADO: {clave}")
    print("(2) Reclamos x", len(RECLAMOS), ": OK")

    # --- (3) purgas de niños (3 en es-MX) ---
    for i, l in enumerate(lineas):
        s = l.strip()
        if s.startswith("Mascota4:") and "escondidas con los niños del pueblo" in s:
            ind = indent_of(l)
            lineas[i] = f"{ind}Mascota4: {GEN_M4_MX}"
            print("(3a) Genericos.Mascota4 purgada (linea", i+1, ")")
        if s.startswith("Grimorio2:") and "se me ha resistido desde niña" in s:
            ind = indent_of(l)
            nueva = l.replace("se me ha resistido desde niña", "se me ha resistido desde hace años y años")
            lineas[i] = nueva
            print("(3b) Zoologist.Grimorio2 «desde niña» purgada (linea", i+1, ")")
        if s.startswith("Mascota3:") and "¿Quién le cree eso a un niño?" in s:
            nueva = l.replace(" ¡Doblar un anzuelo! ¿Quién le cree eso a un niño?",
                              " ¡Doblar un anzuelo! ¡Ni en el mejor cuento!")
            lineas[i] = nueva
            print("(3c) Angler.Mascota3 «a un niño» purgada (linea", i+1, ")")
    resto = [i+1 for i, l in enumerate(lineas) if "niños" in l or "niñas" in l or "niño " in l]
    if resto:
        raise SystemExit(f"QUEDAN REFERENCIAS A NIÑOS en lineas {resto}")

    # --- (4) Caida.Dormido1-8 (espejo) tras BotonTomar ---
    hecho = False
    for i, l in enumerate(lineas):
        if l.strip().startswith("BotonTomar:"):
            ind = indent_of(l)
            nuevas = [f"{ind}Dormido{k+1}: {t}" for k, t in enumerate(DORMIDO)]
            lineas = lineas[:i+1] + nuevas + lineas[i+1:]
            hecho = True
            break
    if not hecho:
        raise SystemExit("BotonTomar NO ENCONTRADO")
    print("(4) Caida.Dormido1-8: OK")

    escribir(lineas)
    print("LINEAS:", n0, "->", len(lineas))

if __name__ == "__main__":
    main()
