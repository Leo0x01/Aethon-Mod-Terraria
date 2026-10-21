#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# hjson_v6511.py — LA LOCALIZACIÓN v6.51.1 (×3):
#  · Textos CORTOS (la letra: «algunos textos son largos o están mal
#    recortados») — Voz1-4 y el Escupidizo a UNA/DOS líneas.
#  · Claves nuevas: EsenciaDevorada, ReqArmaLimpia, NaceCorrupta.
#  · ArmaCorrupta: la etiqueta con el +5% a TODAS las estadísticas.
#  · La rareza: Rarities.LegendarioSupremo.
#  · es-MX == es-ES (espejo).
import io, re

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"
def leer(n): return io.open(BASE + n, encoding="utf-8").read()
def escribir(n, s): io.open(BASE + n, "w", encoding="utf-8").write(s)

# ==================== ESPAÑOL ====================
VOZ_ES = """\tSubida: ✦ El Grimorio DESPIERTA un poco más: nivel de despertar {0}.
\tVoz1: Mmm… otra página que se escribe sola.
\tVoz2: Siento los anillos, portador. TODOS los anillos.
\tVoz3: Cada despertar me recuerda lo que era antes.
\tVoz4: (el libro se estremece de gusto) Eso. MÁS de eso.
\tFaltaEsencia: ✦ El grimorio aparta el plato: le faltan {0} esencia(s)… y {1}.
\tFaltaDinero: ✦ El grimorio aparta el plato: le faltan {0}.
\tOjosCerrados: ✦ El escuadrón aún duerme: se necesita NIVEL {0} para el primer ojo.
\tMejorasSelladas: ✦ Las mejoras nacen con EL DESPERTAR (5/15/30/40). Actual: {0}.
\tNuevoOjo: ✧ ¡El grimorio puede invocar un OJO NUEVO! (el número {0} del escuadrón)
\tCerrojoEsbirros: ✦ El grimorio no comparte su cielo: con sus ojos velando, ningún otro esbirro responde.
\tEsenciaDevorada: ✦ El Eterno devora {0} × {1}: +{2}% de daño permanente.
\tReqArmaLimpia: Requiere un arma SIN corromper
\tNaceCorrupta: Nace CORRUPTA: +5% a TODAS sus estadísticas · sin etiquetas ni reforge
\tCondicionRecetas: Requiere el Grimorio del Eterno en el inventario"""

ESCUPIDIZO_ES = """\tEscupidizo:
\t\t'''
\t\tYa somos tres, portador: tú, yo… y ÉL.
\t\tToma: te lo escupo como mascota. Aliméntalo de silencio.
\t\t'''"""

HITOS_ES = """\tHitoMejora1: ✧ ¡DESPERTAR 5: la MEJORA 1 de los ojos desbloqueada!
\tHitoSol: ✧ ¡DESPERTAR 7: DOS OJOS mejorados crean un SOL RÚNICO de 1 anillo! (2/min)
\tHitoAgujeros: ✧ ¡DESPERTAR 10: CINCO OJOS mejorados abren un AGUJERO RÚNICO al azar! (1/min)
\tHitoMejora2: ✧ ¡DESPERTAR 15: la MEJORA 2 de los ojos desbloqueada!
\tHitoSol2: ✧ ¡DESPERTAR 17: el sol gana su SEGUNDO anillo y nace 3 veces por minuto!
\tHitoMascota: ✧ ¡DESPERTAR 20: EL GRIMORIO TE ESCUPE AL MINIGRIMORIO como mascota!
\tHitoAscendidos: ✧ ¡DESPERTAR 25: los agujeros ASCIENDES — aún más daño!
\tHitoMejora3: ✧ ¡DESPERTAR 30: la MEJORA 3 de los ojos desbloqueada!
\tHitoSol3: ✧ ¡DESPERTAR 35: el sol de TRES anillos, 4 veces por minuto!
\tHitoMejora4: ✧ ¡DESPERTAR 40: la MEJORA FINAL de los ojos!
\tHitoSupremo: ✧ ¡DESPERTAR 45: agujeros 2/min… y OCHO OJOS M4 abren el SUPREMO o el SUPREMO AURORA!
\tHitoMaximo: ✧✧ ¡DESPERTAR 50: TODO el ataque del escuadrón MÁS FUERTE y MÁS RÁPIDO!"""

ARMA_ES = """\tArmaCorruptaTag: Corrupto
\tArmaCorrupta1: +5% a TODAS sus estadísticas — daña más, empuja más, pega más rápido.
\tArmaCorrupta2: Ya no acepta prefijos ni reforge — el yunque le está cerrado.
\tArmaCorrupta3: Suéltala con el grimorio EN LA MANO: el Eterno la devorará."""

# ==================== INGLÉS ====================
VOZ_EN = """\tSubida: ✦ The Grimoire AWAKENS a little more: awakening level {0}.
\tVoz1: Mmm… another page writing itself.
\tVoz2: I can feel the rings, bearer. ALL the rings.
\tVoz3: Every awakening reminds me what I was.
\tVoz4: (the book shivers with delight) Yes. MORE of that.
\tFaltaEsencia: ✦ The grimoire pushes the dish away: it needs {0} more essence(s)… and {1}.
\tFaltaDinero: ✦ The grimoire pushes the dish away: it needs {0}.
\tOjosCerrados: ✦ The squad still sleeps: LEVEL {0} is needed for the first eye.
\tMejorasSelladas: ✦ Upgrades are born with THE AWAKENING (5/15/30/40). Current: {0}.
\tNuevoOjo: ✧ The grimoire can summon a NEW EYE! (squad number {0})
\tCerrojoEsbirros: ✦ The grimoire does not share its sky: while its eyes watch, no other minion answers.
\tEsenciaDevorada: ✦ The Eternal devours {0} × {1}: +{2}% permanent damage.
\tReqArmaLimpia: Requires an UNCORRUPTED weapon
\tNaceCorrupta: Born CORRUPT: +5% to ALL its stats · no prefixes or reforging
\tCondicionRecetas: Requires the Eternal Grimoire in the inventory"""

ESCUPIDIZO_EN = """\tEscupidizo:
\t\t'''
\t\tThere are three of us now, bearer: you, me… and HIM.
\t\tHere: I spit him out as your pet. Feed him silence.
\t\t'''"""

HITOS_EN = """\tHitoMejora1: ✧ AWAKENING 5: eye UPGRADE 1 unlocked!
\tHitoSol: ✧ AWAKENING 7: TWO UPGRADED EYES forge a 1-RING RUNIC SUN! (2/min)
\tHitoAgujeros: ✧ AWAKENING 10: FIVE UPGRADED EYES open a random RUNIC BLACK HOLE! (1/min)
\tHitoMejora2: ✧ AWAKENING 15: eye UPGRADE 2 unlocked!
\tHitoSol2: ✧ AWAKENING 17: the sun gains its SECOND ring, 3 times per minute!
\tHitoMascota: ✧ AWAKENING 20: THE GRIMORIO SPITS THE MINI-GRIMOIRE out as your pet!
\tHitoAscendidos: ✧ AWAKENING 25: the black holes ASCEND — even more damage!
\tHitoMejora3: ✧ AWAKENING 30: eye UPGRADE 3 unlocked!
\tHitoSol3: ✧ AWAKENING 35: the THREE-ring sun, 4 times per minute!
\tHitoMejora4: ✧ AWAKENING 40: the FINAL eye upgrade!
\tHitoSupremo: ✧ AWAKENING 45: holes 2/min… and EIGHT M4 EYES open the SUPREME or SUPREME AURORA!
\tHitoMaximo: ✧✧ AWAKENING 50: ALL the squad's attacks STRONGER and FASTER!"""

ARMA_EN = """\tArmaCorruptaTag: Corrupt
\tArmaCorrupta1: +5% to ALL its stats — more damage, more knockback, faster swings.
\tArmaCorrupta2: It no longer accepts prefixes or reforging — the anvil is closed to it.
\tArmaCorrupta3: Drop it with the grimoire IN HAND: the Eternal will devour it."""

RAREZA_ES = "\tLegendarioSupremo: Legendario Supremo\n"
RAREZA_EN = "\tLegendarioSupremo: Supreme Legendary\n"

def operar(nombre, es_en):
    s = leer(nombre)
    # (1) LA SECCIÓN DESPERTAR: reemplazar el cuerpo completo
    patron = re.compile(r"^Despertar: \{$.*?^\}$", re.S | re.M)
    m = patron.search(s)
    assert m, f"[{nombre}] sección Despertar no hallada"
    voz = VOZ_EN if es_en else VOZ_ES
    escupiz = ESCUPIDIZO_EN if es_en else ESCUPIDIZO_ES
    hitos = HITOS_EN if es_en else HITOS_ES
    nueva = "Despertar: {\n" + voz + "\n" + hitos + "\n" + escupiz + "\n}\n"
    s = s[:m.start()] + nueva + s[m.end():]

    # (2) ARMAS CORRUPTAS: las líneas con el +5%
    patron_arma = re.compile(r"\tArmaCorruptaTag: [^\n]*\n\tArmaCorrupta1: [^\n]*\n\tArmaCorrupta2: [^\n]*\n\tArmaCorrupta3: [^\n]*\n")
    m = patron_arma.search(s)
    assert m, f"[{nombre}] ArmaCorrupta no hallada"
    s = s[:m.start()] + (ARMA_EN if es_en else ARMA_ES) + "\n" + s[m.end():]

    # (3) LA RAREZA (sección Rarities nueva al final)
    if "LegendarioSupremo" not in s:
        s = s.rstrip("\n") + "\n\n// v6.51.1 — LA RAREZA CÓSMICA DEL ETERNO\nRarities: {\n" + \
            (RAREZA_EN if es_en else RAREZA_ES) + "}\n"

    escribir(nombre, s)
    print("OK —", nombre)

operar("es-ES_Mods.AethonMod.hjson", False)
operar("es-MX_Mods.AethonMod.hjson", False)
operar("en-US_Mods.AethonMod.hjson", True)

# LA VERIFICACIÓN DE LA CASA
es = leer("es-ES_Mods.AethonMod.hjson")
mx = leer("es-MX_Mods.AethonMod.hjson")
def claves(s):
    return [l.strip() for l in s.splitlines()
            if re.match(r"^\s*[A-Za-z0-9_.]+:", l) and not l.strip().startswith("//")]
ces, cmx = claves(es), claves(mx)
assert ces == cmx, f"es-MX != es-ES: {set(ces) ^ set(cmx)}"
print(f"VERIFICADO: es-MX == es-ES ({len(ces)} claves)")
