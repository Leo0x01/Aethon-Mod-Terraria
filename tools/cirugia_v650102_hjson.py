#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
CIRUGÍA hjson v6.50.102 — LA ELEGIDA + EL ETERNO NERVIOSO.

La letra del usuario: «creo que me quedo con el Equinoccio, puedes borrar
las otras y también borra el grimorio sellado, borra también tinta viva»
+ «usa el proyectil del Equinoccio para el grimorio del eterno».

MUEREN (los 3 idiomas):
  - Items: GrimorioEstrellado, EcoEmperatriz, VeredictoEmperatriz,
    GrimorioSellado, TintaViva
  - Projectiles (líneas planas): EstelaAethonProjectile,
    TrazoEmperatrizProjectile, FragmentoAethon, ExplosionAethon
  - Buffs: Observado (el sistema Observado entero murió con el Sellado)
  - Sección Desborde (los textos del sistema muerto)

NACEN/SE REESCRIBEN:
  - Equinoccio: LA ELEGIDA — sin bruma (la letra: «que ya no use bruma»)
  - BolsaEmperatriz: la bolsa de LA ELEGIDA
  - GrimoireEternal: EL NERVIOSO JUGABLE — hambre de 15 min, pareja
    día/noche por disparo, sombra que caza, XP del festín

Cada archivo conserva SU formato (es-MX espacios, es-ES/en-US tabs).
Idempotente: los bloques muertos que ya no están se saltan; los
reemplazos reemplazan.
"""

import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
LOC = REPO / "AethonMod" / "Localization"

ARCHIVOS = {
    "es": LOC / "es-MX_Mods.AethonMod.hjson",   # el PRIMARIO español
    "es-es": LOC / "es-ES_Mods.AethonMod.hjson",
    "en": LOC / "en-US_Mods.AethonMod.hjson",
}

# =====================================================================
#  LOS NUEVOS BLOQUES (por idioma; {I}=indent de la clave, se rellena)
# =====================================================================

EQUINOCCIO_ES = """\
{I}// v6.50.99/.102 — LA CUARTA ARMA — OPCIÓN B DUAL (el día + la noche) — LA ELEGIDA
{I}Equinoccio: {
{I2}DisplayName: El Equinoccio
{I2}Tooltip:
{I3}\'\'\'
{I3}«Mitad aurora, mitad eclipse.»
{I3}LA ELEGIDA — OPCIÓN B DUAL: dos Nightglow vanilla por disparo.
{I3}EL DORADO sale con LUZ de sol: halo dorado pulsante y ocho rayos
{I3}giratorios. EL NEGRO es un eclipse limpio: disco oscuro con rim
{I3}violeta pulsante y chispas de llama de sombra (la bruma murió).
{I3}El día y la noche se separan sobre tu cabeza… y luego cazan.
{I3}Sin maná (arma de pruebas).
{I3}\'\'\'
{I}}"""

EQUINOCCIO_EN = """\
{I}// v6.50.99/.102 — THE FOURTH WEAPON — DUAL OPTION B — THE CHOSEN ONE
{I}Equinoccio: {
{I2}DisplayName: The Equinox
{I2}Tooltip:
{I3}\'\'\'
{I3}"Half dawn, half eclipse."
{I3}THE CHOSEN ONE — DUAL OPTION B: two vanilla Nightglows per shot.
{I3}THE GOLDEN one leaves with SUNLIGHT: pulsing golden halo and eight
{I3}spinning rays. THE BLACK one is a clean eclipse: dark disc with a
{I3}pulsing violet rim and shadowflame sparks (the mist is gone).
{I3}Day and night part above your head... then they hunt.
{I3}No mana (test weapon).
{I3}\'\'\'
{I}}"""

BOLSA_ES = """\
{I}// v6.50.99/.102 — LA BOLSA DE LA EMPERATRIZ — bolsa 20 (la elegida del estudio)
{I}BolsaEmperatriz: {
{I2}DisplayName: Bolsa de la Emperatriz
{I2}Tooltip:
{I3}\'\'\'
{I3}La elegida del estudio del proyectil de la Emperatriz de la Luz:
{I3}EL EQUINOCCIO — el día y la noche en un libro. Las otras tres
{I3}pruebas (el clon cósmico, el eco y el veredicto) cumplieron su
{I3}misión y se retiraron.
{I3}Clic derecho para desplegarla. Solo entrega lo que te falte.
{I3}\'\'\'
{I}}"""

BOLSA_EN = """\
{I}// v6.50.99/.102 — THE EMPRESS BAG — bag 20 (the chosen one)
{I}BolsaEmperatriz: {
{I2}DisplayName: Empress Bag
{I2}Tooltip:
{I3}\'\'\'
{I3}The chosen one of the Empress of Light projectile study:
{I3}THE EQUINOX — day and night in a single book. The other three
{I3}trials (the cosmic clone, the echo and the verdict) served their
{I3}purpose and were retired.
{I3}Right-click to deploy it. Only delivers what you're missing.
{I3}\'\'\'
{I}}"""

ETENO_ES = """\
{I}GrimoireEternal: {
{I2}DisplayName: Grimorio del Eterno
{I2}Tooltip:
{I3}\'\'\'
{I3}Aethon, atado en forma de libro: el arma que come ES el jefe final
{I3}— y ahora VISTE EL CUERPO DEL NERVIOSO: su ojo, su temblor, su
{I3}glitch y su hambre, con reloj de verdad.
{I3}EL HAMBRE (15 minutos de 0 a 100%): debajo del 25% calla; del 25%
{I3}sale a cazar si te ausentas (quieto 10 s o 30 s sin darle uso); del
{I3}50% LA IMPACIENCIA — voz, temblor, glitch y las oleadas de hambre.
{I3}Sólo la presa comida lo baja (−1% por presa): aliméntalo CAZANDO.
{I3}Clic izq: EL EQUINOCCIO — el día (orbe dorado con luz de sol) y la
{I3}noche (eclipse con rim violeta) se separan sobre tu cabeza; cada 3
{I3}niveles, una pareja MÁS. Con el espíritu fuera, dispara el LIBRO.
{I3}Clic der: invoca el Orbe Cósmico.
{I3}Gana XP REAL con cada baja (bestiario 0–5★, primera baja de cada
{I3}especie ×3, modo difícil ×2, jefes con fórmula fija) — y su Sombra
{I3}de la Página devora presas ENTERAS (botín incluido) cuya XP es DEL
{I3}LIBRO: el que come es el que sube de nivel.
{I3}LOS TRES ESTADOS: sostenido = todo el poder; en la barra rápida =
{I3}sigue comiendo XP, conserva los espacios de esbirro y vida/maná
{I3}(tope +100); guardado = nada (los esbirros ya invocados permanecen).
{I3}Sin coste de maná — el hambre ES el coste.
{I3}\'\'\'
{I}}"""

ETENO_EN = """\
{I}GrimoireEternal: {
{I2}DisplayName: Grimoire of the Eternal
{I2}Tooltip:
{I3}\'\'\'
{I3}Aethon, bound in book form: the weapon that eats IS the final
{I3}boss — and now it WEARS THE NERVOUS BODY: its eye, its tremor,
{I3}its glitch and its hunger, on a real clock.
{I3}THE HUNGER (15 minutes from 0 to 100%): below 25% it stays quiet;
{I3}from 25% it goes hunting if you go absent (still for 10 s or 30 s
{I3}without using it); from 50% THE IMPATIENCE — voice, tremor, glitch
{I3}and the hunger waves. Only eaten prey lowers it (−1% each): FEED
{I3}IT BY LETTING IT HUNT.
{I3}Left click: THE EQUINOX — day (golden orb with sunlight) and night
{I3}(eclipse with violet rim) part above your head; every 3 levels, one
{I3}MORE pair. With the spirit out, the BOOK fires. Right click:
{I3}summons the Cosmic Orb.
{I3}Earns REAL XP from every kill (bestiary rarity 0–5 stars, first
{I3}kill of each species ×3, hardmode ×2, bosses with a fixed formula)
{I3}— and its Page Shadow devours WHOLE prey (loot included) whose XP
{I3}IS THE BOOK'S: the one that eats is the one that levels up.
{I3}THREE STATES: held = full power; in the hotbar = still eats XP,
{I3}keeps minion slots and life/mana (capped at +100); stored = nothing
{I3}(already-summoned minions remain).
{I3}No mana cost — the hunger IS the cost.
{I3}\'\'\'
{I}}"""

NUEVOS = {
    "es": {"Equinoccio": EQUINOCCIO_ES, "BolsaEmperatriz": BOLSA_ES, "GrimoireEternal": ETENO_ES},
    "es-es": {"Equinoccio": EQUINOCCIO_ES, "BolsaEmperatriz": BOLSA_ES, "GrimoireEternal": ETENO_ES},
    "en": {"Equinoccio": EQUINOCCIO_EN, "BolsaEmperatriz": BOLSA_EN, "GrimoireEternal": ETENO_EN},
}

BLOQUES_MUERTOS = [
    "GrimorioEstrellado", "EcoEmperatriz", "VeredictoEmperatriz",
    "GrimorioSellado", "TintaViva", "Observado", "Desborde",
]

LINEAS_MUERTAS = [
    "EstelaAethonProjectile.DisplayName",
    "TrazoEmperatrizProjectile.DisplayName",
    "FragmentoAethon.DisplayName",
    "ExplosionAethon.DisplayName",
]


def quitar_bloque(text: str, key: str, comer_comentarios: bool = True):
    """Quita el bloque `KEY: { ... }` (con su indent exacto) y, si
    comer_comentarios, los comentarios // PEGADOS arriba (sin blanco de
    por medio). Devuelve (texto, se_quitó)."""
    m = re.search(r"^([ \t]*)" + re.escape(key) + r": \{[ \t]*\n", text, re.M)
    if not m:
        return text, False
    ind = m.group(1)
    endm = re.search(r"^" + re.escape(ind) + r"\}[ \t]*\n", text[m.end():], re.M)
    if not endm:
        raise RuntimeError(f"bloque {key} sin cierre al indent {ind!r}")
    start, end = m.start(), m.end() + endm.end()
    if comer_comentarios:
        arriba = text[:start].split("\n")
        j = len(arriba) - 2          # la línea justo encima del KEY
        while j >= 0 and arriba[j].strip().startswith("//"):
            j -= 1
        if j + 1 <= len(arriba) - 2:
            start = len("\n".join(arriba[: j + 1])) + 1
    return text[:start] + text[end:], True


def reemplazar_bloque(text: str, key: str, nuevo: str):
    """Reemplaza el bloque `KEY: { ... }` (sin tocar comentarios de
    arriba) por `nuevo` — {I}/{I2}/{I3} se rellenan con el indent real."""
    m = re.search(r"^([ \t]*)" + re.escape(key) + r": \{[ \t]*\n", text, re.M)
    if not m:
        raise RuntimeError(f"no encontré el bloque {key} para reemplazar")
    ind = m.group(1)
    endm = re.search(r"^" + re.escape(ind) + r"\}[ \t]*\n", text[m.end():], re.M)
    if not endm:
        raise RuntimeError(f"bloque {key} sin cierre")
    start, end = m.start(), m.end() + endm.end()
    cuerpo = nuevo.replace("{I2}", ind * 2).replace("{I3}", ind * 3).replace("{I}", ind)
    return text[:start] + cuerpo + "\n" + text[end:]


def main():
    total_muertos = 0
    for etiqueta, ruta in ARCHIVOS.items():
        texto = ruta.read_text(encoding="utf-8")
        original = texto

        # 1) los bloques muertos
        for key in BLOQUES_MUERTOS:
            texto, ok = quitar_bloque(texto, key)
            total_muertos += ok
            print(f"[{etiqueta}] {'MURIÓ' if ok else 'ya no estaba':>12}  {key}")

        # 2) las líneas planas
        for flat in LINEAS_MUERTAS:
            nuevo, n = re.subn(r"^[ \t]*" + re.escape(flat) + r":[^\n]*\n", "", texto, flags=re.M)
            texto = nuevo
            total_muertos += n
            print(f"[{etiqueta}] {'MURIÓ' if n else 'ya no estaba':>12}  {flat}")

        # 3) los reemplazos (la elegida + el eterno nuevo)
        for key, nuevo in NUEVOS[etiqueta].items():
            texto = reemplazar_bloque(texto, key, nuevo)
            print(f"[{etiqueta}] {'NACIÓ':>12}  {key}")

        # 4) colapsar blancos triples que hayan quedado en las costuras
        texto = re.sub(r"\n{3,}", "\n\n", texto)

        # 5) sanidad: balance de '''  par
        if texto.count("'''") % 2 != 0:
            raise RuntimeError(f"[{etiqueta}] ''' desbalanceado — ABORTAR")

        # 6) sanidad: ningún muerto vivo
        for fantasma in ["GrimorioSellado", "TintaViva", "GrimorioEstrellado",
                         "EcoEmperatriz", "VeredictoEmperatriz", "EstelaAethon",
                         "TrazoEmperatriz", "FragmentoAethon", "ExplosionAethon"]:
            if re.search(r"^[ \t]*" + fantasma + r"[.:]", texto, re.M):
                raise RuntimeError(f"[{etiqueta}] el fantasma {fantasma} sigue vivo")

        if texto != original:
            ruta.write_text(texto, encoding="utf-8", newline="\n")
            print(f"[{etiqueta}] ESCRITO  ({len(original)} → {len(texto)} bytes)\n")
        else:
            print(f"[{etiqueta}] SIN CAMBIOS\n")

    print(f"TOTAL piezas muertas: {total_muertos}")


if __name__ == "__main__":
    sys.exit(main())
