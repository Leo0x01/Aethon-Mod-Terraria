#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# ======================================================================
# hjson_v6510.py — LA LOCALIZACIÓN DE LA LLEGADA Y EL DESPERTAR (×3)
#
#  · NUEVO: la sección Despertar.* (subidas, voz, requisitos, hitos,
#    cerrojo, escupidizo, condición de recetas) · GrimorioDormido.Numero
#    (el rótulo ∞ · ????? de la letra) · Items.EcoDelDespertar.
#  · LA LLEGADA: todos los textos de Caida.* que hablaban de cometa/
#    cráter/estrella negra ahora hablan de bruma y aparecimiento.
#  · LA BRUMA DEPRECADA (vestigio): BrumaDelCrater + el MapEntry con la
#    clave CORREGIDA (Tiles.BrumaAltarTile — el bug de la .130: la clave
#    era BrumaAltar y el mapa caía al fallback crudo).
#  · EL AGUJERO NEGRO NORMAL MUERE (Items.BlackHoleStaff +
#    Projectiles.BlackHoleProjectile.DisplayName).
#  · es-MX = espejo EXACTO de es-ES (la regla de la casa).
# ======================================================================
import io
import re

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"

def leer(nombre):
    return io.open(BASE + nombre, encoding="utf-8").read()

def escribir(nombre, contenido):
    io.open(BASE + nombre, "w", encoding="utf-8").write(contenido)

def reemplazar(s, viejo, nuevo, ruta, veces=1):
    n = s.count(viejo)
    assert n == veces, f"[{ruta}] {viejo[:60]!r}: esperado {veces}, hallado {n}"
    return s.replace(viejo, nuevo)

# ======================================================================
# LA SECCIÓN DESPERTAR (es / en)
# ======================================================================
DESPERTAR_ES = """
// v6.51.0 — EL DESPERTAR DEL GRIMORIO: la nueva progresión (nivel 0..50)
// — las tres ofrendas por nivel (esencias corruptas + dinero + el arma
// corrupta del peldaño de Terraria), los poderes que desbloquea y la
// voz del libro al despertar.
Despertar: {
	Subida: ✦ El Grimorio DESPIERTA un poco más: nivel de despertar {0}.
	Voz1: Mmm… otra página que se escribe sola. Sigue alimentándome, portador.
	Voz2: Siento los anillos, portador. TODOS los anillos. Falta poco.
	Voz3: Cada despertar me recuerda lo que era antes de la rotura. Casi todo.
	Voz4: (el libro se estremece de gusto) Eso. MÁS de eso.
	FaltaEsencia: ✦ El grimorio aparta el plato: le faltan {0} esencia(s) corrupta(s)… y {1}.
	FaltaDinero: ✦ El grimorio aparta el plato: le faltan {0}.
	OjosCerrados: ✦ El escuadrón aún duerme: el grimorio necesita NIVEL {0} para invocar su primer ojo.
	MejorasSelladas: ✦ Las mejoras del escuadrón nacen con EL DESPERTAR (nivel 5 la primera, 15 la segunda, 30 la tercera, 40 la última). Despertar actual: {0}.
	NuevoOjo: ✧ ¡El grimorio puede invocar un OJO NUEVO! (el ojo número {0} del escuadrón)
	CerrojoEsbirros: ✦ El grimorio no comparte su cielo: mientras sus ojos velan, ningún otro esbirro responde.
	CondicionRecetas: Requiere el Grimorio del Eterno en el inventario
	HitoMejora1: ✧ ¡DESPERTAR 5: la MEJORA 1 de los ojos desbloqueada! (la invocación de más ahora FORTALECE)
	HitoSol: ✧ ¡DESPERTAR 7: DOS OJOS con mejora pueden crear un SOL RÚNICO de 1 anillo y lanzárselo al enemigo! (2 por minuto, al azar)
	HitoAgujeros: ✧ ¡DESPERTAR 10: CINCO OJOS con mejora pueden abrir un AGUJERO NEGRO RÚNICO al azar sobre el enemigo! (1 por minuto)
	HitoMejora2: ✧ ¡DESPERTAR 15: la MEJORA 2 de los ojos desbloqueada!
	HitoSol2: ✧ ¡DESPERTAR 17: el sol rúnico gana su SEGUNDO anillo y nace 3 veces por minuto!
	HitoMascota: ✧ ¡DESPERTAR 20: EL GRIMORIO TE ESCUPE AL MINIGRIMORIO como mascota!
	HitoAscendidos: ✧ ¡DESPERTAR 25: los agujeros negros combinados ascienden — hacen AÚN MÁS daño!
	HitoMejora3: ✧ ¡DESPERTAR 30: la MEJORA 3 de los ojos desbloqueada!
	HitoSol3: ✧ ¡DESPERTAR 35: el sol rúnico gana su TERCER anillo y nace 4 veces por minuto!
	HitoMejora4: ✧ ¡DESPERTAR 40: la MEJORA FINAL de los ojos desbloqueada!
	HitoSupremo: ✧ ¡DESPERTAR 45: los agujeros negros son 2 por minuto… y OCHO OJOS con la MEJORA 4 pueden abrir el AGUJERO NEGRO SUPREMO o el SUPREMO AURORA! (1 por minuto)
	HitoMaximo: ✧✧ ¡DESPERTAR 50: TODO el ataque de los ojos y sus proyectiles, MÁS FUERTE y MÁS RÁPIDO! (máximo por ahora)
	Escupidizo:
		'''
		Ya somos tres, portador: tú, yo… y ÉL.
		Una parte de mí siempre quiso volver al mundo pequeño — el de los
		hombros y las travesuras. Toma: te lo escupo como mascota.
		Aliméntalo con silencio y curiosidad. Es lo único que comemos los dos.
		'''
}
"""

DESPERTAR_EN = """
// v6.51.0 — THE GRIMOIRE'S AWAKENING: the new progression (level 0..50)
// — three offerings per level (corrupt essences + coins + the corrupted
// weapon of Terraria's ladder), the powers it unlocks, and the book's
// voice as it wakes.
Despertar: {
	Subida: ✦ The Grimoire AWAKENS a little more: awakening level {0}.
	Voz1: Mmm… another page writing itself. Keep feeding me, bearer.
	Voz2: I can feel the rings, bearer. ALL the rings. We're close.
	Voz3: Every awakening reminds me what I was before the breaking. Most of it.
	Voz4: (the book shivers with delight) Yes. MORE of that.
	FaltaEsencia: ✦ The grimoire pushes the dish away: it needs {0} more corrupt essence(s)… and {1}.
	FaltaDinero: ✦ The grimoire pushes the dish away: it needs {0}.
	OjosCerrados: ✦ The squad still sleeps: the grimoire needs LEVEL {0} to summon its first eye.
	MejorasSelladas: ✦ The squad's upgrades are born with THE AWAKENING (level 5 the first, 15 the second, 30 the third, 40 the last). Current awakening: {0}.
	NuevoOjo: ✧ The grimoire can summon a NEW EYE! (squad eye number {0})
	CerrojoEsbirros: ✦ The grimoire does not share its sky: while its eyes watch, no other minion answers.
	CondicionRecetas: Requires the Eternal Grimoire in the inventory
	HitoMejora1: ✧ AWAKENING 5: eye UPGRADE 1 unlocked! (extra summons now EMPOWER)
	HitoSol: ✧ AWAKENING 7: TWO UPGRADED EYES can forge a 1-RING RUNIC SUN and hurl it at the enemy! (2 per minute, at random)
	HitoAgujeros: ✧ AWAKENING 10: FIVE UPGRADED EYES can open a random RUNIC BLACK HOLE over the enemy! (1 per minute)
	HitoMejora2: ✧ AWAKENING 15: eye UPGRADE 2 unlocked!
	HitoSol2: ✧ AWAKENING 17: the runic sun gains its SECOND ring and is born 3 times per minute!
	HitoMascota: ✧ AWAKENING 20: THE GRIMOIRE SPITS THE MINI-GRIMOIRE out as your pet!
	HitoAscendidos: ✧ AWAKENING 25: the combined black holes ASCEND — dealing even MORE damage!
	HitoMejora3: ✧ AWAKENING 30: eye UPGRADE 3 unlocked!
	HitoSol3: ✧ AWAKENING 35: the runic sun gains its THIRD ring and is born 4 times per minute!
	HitoMejora4: ✧ AWAKENING 40: the FINAL eye upgrade unlocked!
	HitoSupremo: ✧ AWAKENING 45: black holes twice per minute… and EIGHT EYES with UPGRADE 4 can open the SUPREME BLACK HOLE or the SUPREME AURORA! (1 per minute)
	HitoMaximo: ✧✧ AWAKENING 50: ALL the eyes' attacks and projectiles, STRONGER and FASTER! (max for now)
	Escupidizo:
		'''
		There are three of us now, bearer: you, me… and HIM.
		A part of me always wanted to return to the small world — the one
		with shoulders and mischief. Here: I spit him out as your pet.
		Feed him silence and curiosity. It's the only thing we both eat.
		'''
}
"""

# ======================================================================
# LAS TRADUCCIONES DIFERENTES (es / en)
# ======================================================================
NUMERO_ES = "\t\tNumero: ∞ · Nº ????? · FUERA DEL CATÁLOGO\n"
NUMERO_EN = "\t\tNumero: ∞ · No. ????? · OUT OF THE CATALOGUE\n"

BESTIARIO_ES = "\t\tBestiario: Un grimorio que un día llegó envuelto en un mar de bruma — nadie vio cómo. Duerme flotando en el mismo aire… a la espera de la mano correcta.\n"
BESTIARIO_EN = "\t\tBestiario: A grimoire that one day arrived wrapped in a sea of mist — nobody saw how. It sleeps floating on the very air… waiting for the right hand.\n"

ECO_ES = """	EcoDelDespertar: {
		DisplayName: Eco del Despertar
		Tooltip:
			'''
			LA LLAVE DE PRUEBAS DE EL DESPERTAR (no consumible): cada uso
			despierta al grimorio UN NIVEL MÁS (con toda la ceremonia — y en
			el 20, la película de la mascota).
			El camino real son las tres ofrendas: esencias corruptas,
			dinero y el arma corrupta del peldaño… pero el probador es del
			probador.
			'''
	}
"""
ECO_EN = """	EcoDelDespertar: {
		DisplayName: Echo of Awakening
		Tooltip:
			'''
			THE TESTING KEY OF THE AWAKENING (non-consumable): each use
			wakes the grimoire ONE MORE LEVEL (full ceremony included — and
			at 20, the pet's little movie).
			The real path is the three offerings: corrupt essences, coins
			and the corrupted weapon of the rung… but a tester's key
			belongs to the tester.
			'''
	}
"""

CAIDA_CAMBIOS_ES = [
    # (viejo, nuevo)
    ("""\tPresentacion:
\t\t'''
\t\t…despierto.
\t\tLlevo eones esperando una mano como la tuya — el cielo me dejó caer solo para TI.
""",
     """\tPresentacion:
\t\t'''
\t\t…despierto.
\t\tLlevo eones esperando una mano como la tuya — la bruma me trajo hasta AQUÍ, solo para TI.
"""),
    ("\tDormido1: …zZz. ¿Ya llegaste?… No. Todavía no. Que siga cayendo el cielo. zZz.",
     "\tDormido1: …zZz. ¿Ya llegaste?… No. Todavía no. Que siga hilando la bruma. zZz."),
    ("\tDormido3: …el cráter está tibio. Los cráteres de los otros libros eran fríos. Este se parece un poquito a mí. zZz.",
     "\tDormido3: …el aire de la bruma está tibio. Las llegadas de los otros libros eran frías. Esta se parece un poquito a mí. zZz."),
    ("\tDormido5: …llevo eones contando caídas. Esta es la primera que huele a alguien. Las demás olían a nadie. zZz.",
     "\tDormido5: …llevo eones contando llegadas. Esta es la primera que huele a alguien. Las demás olían a nadie. zZz."),
    ("\tCometaCae: ✦ Una estrella negra se desgarra en el cielo… y cae hacia ti.",
     "\tCometaCae: ✦ El aire se espesa: UNA BRUMA INMENSA está naciendo cerca de ti… y algo llega dentro."),
    ("\tImpacto: ✦ El cielo calló donde la estrella besó la tierra: TU GRIMORIO TE ESPERA.",
     "\tImpacto: ✦ La bruma se disipa… y algo flota en su lugar: TU GRIMORIO TE ESPERA."),
    ("\tTintaYaEspera: ✦ El cielo no insiste: TU GRIMORIO YA TE ESPERA en su cráter.",
     "\tTintaYaEspera: ✦ El cielo no insiste: TU GRIMORIO YA TE ESPERA entre la bruma."),
    ("\tTintaEnVuelo: ✦ Tu estrella negra YA VIAJA hacia ti — espera su beso con la tierra.",
     "\tTintaEnVuelo: ✦ TU BRUMA YA NACE frente a ti — espera a que se disipe."),
    ("\tArmaCorrompidaAviso: ✦ La bruma del cráter ABRAZA a {0}… queda CORRUPTA.",
     "\tArmaCorrompidaAviso: ✦ La corrupción del grimorio ABRAZA a {0}… queda CORRUPTA."),
    ("\tArmaCorrupta1: La CAÍDA la ha mancillado: la bruma del cráter vive en ella.",
     "\tArmaCorrupta1: LA LLEGADA la ha mancillado: la bruma del despertar vive en ella."),
    ("\tArmaCorrupta3: El Grimorio del Eterno puede devorarla en el lugar de la caída.",
     "\tArmaCorrupta3: Suéltala con el grimorio EN LA MANO: el Eterno la devorará."),
]

CAIDA_CAMBIOS_EN = [
    ("""\tPresentacion:
\t\t'''
\t\t…awake.
\t\tI have waited eons for a hand like yours — the sky dropped me here for YOU alone.
""",
     """\tPresentacion:
\t\t'''
\t\t…awake.
\t\tI have waited eons for a hand like yours — the mist carried me HERE, for you alone.
"""),
    ("\tCometaCae: ✦ A black star tears open the sky… and it falls toward you.",
     "\tCometaCae: ✦ The air thickens: A VAST MIST is being born near you… and something arrives within."),
    ("\tImpacto: ✦ The sky went silent where the star kissed the earth: YOUR GRIMOIRE AWAITS.",
     "\tImpacto: ✦ The mist dissipates… and something floats in its place: YOUR GRIMOIRE AWAITS."),
]

BRUMA_ITEM_ES = """	BrumaDelCrater: {
\t\tDisplayName: Bruma del cráter (vestigio)
\t\tTooltip:
\t\t\t'''
\t\t\tUn VESTIGIO de la vieja caída: la bruma que una vez fue altar.
\t\t\tLA LLEGADA ya no la necesita — la corrupción vive en el apartado
\t\t\tde creación mientras cargues el grimorio, y el Eterno devora las
\t\t\tarmas corruptas que sueltes con él en la mano.
\t\t\t'''
\t}
"""
BRUMA_ITEM_EN = """	BrumaDelCrater: {
\t\tDisplayName: Crater mist (relic)
\t\tTooltip:
\t\t\t'''
\t\t\tA RELIC of the old fall: the mist that was once an altar.
\t\t\tTHE ARRIVAL no longer needs it — corruption lives in your crafting
\t\t\tmenu while you carry the grimoire, and the Eternal devours any
\t\t\tcorrupted weapon you drop with it in hand.
\t\t\t'''
\t}
"""

TINTA_ES = """\tTintaDelCometa: {
\t\tDisplayName: Tinta de la Llegada
\t\tTooltip:
\t\t\t'''
\t\t\tEL ACTIVADOR DE LA LLEGADA (item de pruebas — la Bolsa de la Cría lo trae).
\t\t\tRompe el frasco y el mundo RECUERDA: la bruma del Grimorio
\t\t\tdel Eterno nace otra vez frente a ti — de día o de noche.
\t\t\tLos caminos automáticos siguen su ley (la primera noche de
\t\t\tcada jugador en cada mundo + el primer vecino que no sea el
\t\t\tGuía); ESTA tinta es el camino MANUAL: el evento NUNCA se
\t\t\tpierde… y jamás duplica tu grimorio (si ya te espera o su
\t\t\tbruma ya nació, el cielo no insiste).
\t\t\t'''
\t}
"""
TINTA_EN = """\tTintaDelCometa: {
\t\tDisplayName: Ink of the Arrival
\t\tTooltip:
\t\t\t'''
\t\t\tTHE ARRIVAL'S ACTIVATOR (testing item — the Hatchling's Bag carries it).
\t\t\tBreak the vial and the world REMEMBERS: the Eternal Grimoire's
\t\t\tmist is born again before you — day or night.
\t\t\tThe automatic paths keep their law (each player's first night in
\t\t\teach world + the first neighbor who isn't the Guide); THIS ink
\t\t\tis the MANUAL path: the event is NEVER lost… and it never
\t\t\tduplicates your grimoire (if it already awaits you, the sky
\t\t\tdoesn't insist).
\t\t\t'''
\t}
"""

MAPENTRY_ES = "Tiles.BrumaAltarTile.MapEntry: Bruma (vestigio de la vieja caída)\n"
MAPENTRY_EN = "Tiles.BrumaAltarTile.MapEntry: Mist (relic of the old fall)\n"
DUENDE_ES = "Duende.ArmaCorrompida: Esa arma está corrompida… no puedo hacer nada con ella. Ni reforge, ni prefijo, ni magia de taller: lo que la bruma del despertar tocó ya no es de este mundo.\n"
DUENDE_EN = "Duende.ArmaCorrompida: That weapon is corrupted… there's nothing I can do with it. No reforging, no prefixes, no workshop magic: what the awakening's mist touched is no longer of this world.\n"


def operar(nombre, es_ingles):
    s = leer(nombre)
    T = (lambda es, en: en) if es_ingles else (lambda es, en: es)

    # --- (1) el agujero negro normal muere (item + proyectil) ---
    patron_item = re.compile(
        r"\n\tBlackHoleStaff: \{\n\t\tDisplayName: [^\n]*\n\t\tTooltip: \"\"\n\t\}\n")
    s, n = patron_item.subn("\n", s)
    assert n == 1, f"[{nombre}] BlackHoleStaff: {n}"
    patron_proj = re.compile(r"\tBlackHoleProjectile\.DisplayName: [^\n]*\n")
    s, n = patron_proj.subn("", s)
    assert n == 1, f"[{nombre}] BlackHoleProjectile: {n}"

    # --- (2) GrimorioDormido: el número ∞ + el bestiario de la llegada ---
    s = reemplazar(s, "\t\tBestiario: Un grimorio caído del cielo en un cometa de tinta. Duerme flotando en su cráter… a la espera de la mano correcta.\n" if not es_ingles else "\t\tBestiario: A grimoire that fell from the sky inside an ink comet. It sleeps afloat in its crater… waiting for the right hand.\n",
                   T(NUMERO_ES, NUMERO_EN) + T(BESTIARIO_ES, BESTIARIO_EN), nombre)

    # --- (3) el Eco del Despertar (después de TintaDelCometa) ---
    patron_tinta = re.compile(r"\tTintaDelCometa: \{.*?\n\t\}\n", re.S)
    m = patron_tinta.search(s)
    assert m, f"[{nombre}] TintaDelCometa no hallado"
    s = s[:m.start()] + T(TINTA_ES, TINTA_EN) + T(ECO_ES, ECO_EN) + s[m.end():]

    # --- (4) BrumaDelCrater → vestigio ---
    patron_bruma = re.compile(r"\tBrumaDelCrater: \{.*?\n\t\}\n", re.S)
    m = patron_bruma.search(s)
    assert m, f"[{nombre}] BrumaDelCrater no hallado"
    s = s[:m.start()] + T(BRUMA_ITEM_ES, BRUMA_ITEM_EN) + s[m.end():]

    # --- (5) los textos de LA LLEGADA ---
    ok_faltan = []
    for viejo, nuevo in (CAIDA_CAMBIOS_EN if es_ingles else CAIDA_CAMBIOS_ES):
        if viejo in s:
            s = s.replace(viejo, nuevo)
        else:
            ok_faltan.append(viejo[:40])
    # SOLO los críticos (avisos y diálogos dormidos pueden faltar en en-US)
    criticos = ["CometaCae", "Impacto:", "TintaYaEspera", "TintaEnVuelo"]
    for v in ok_faltan:
        if any(c[:12] in v for c in criticos) and not es_ingles:
            raise AssertionError(f"[{nombre}] crítico no hallado: {v!r}")

    # --- (6) la sección DESPERTAR (después del cierre de Caida) ---
    # el cierre de la sección Caida: la línea "\tPunoTitan: ..." + "\n}\n"
    ancla = "\tPunoTitan: Puño titán (más empuje)\n}\n" if not es_ingles else None
    if es_ingles:
        # hallar el cierre de Caida en en-US: la llave }\n que sigue a PunoTitan
        m = re.search(r"\tPunoTitan: [^\n]*\n\}\n", s)
        assert m, f"[{nombre}] cierre de Caida no hallado"
        s = s[:m.end()] + DESPERTAR_EN + s[m.end():]
    else:
        assert ancla in s, f"[{nombre}] cierre de Caida no hallado"
        s = s.replace(ancla, ancla + DESPERTAR_ES, 1)

    # --- (7) el MapEntry corregido + el duende ---
    s = reemplazar(s, "Tiles.BrumaAltar.MapEntry: " + ("Crater Mist\n" if es_ingles else "Bruma del cráter\n"),
                   T(MAPENTRY_ES, MAPENTRY_EN), nombre)
    patron_duende = re.compile(r"Duende\.ArmaCorrompida: [^\n]*\n")
    s, n = patron_duende.subn(T(DUENDE_ES, DUENDE_EN), s)
    assert n == 1, f"[{nombre}] Duende: {n}"

    escribir(nombre, s)
    print("OK —", nombre, len(s.splitlines()), "líneas")


if "Despertar: {" in leer("es-ES_Mods.AethonMod.hjson"):
    print("SKIP — es-ES ya operada")
else:
    operar("es-ES_Mods.AethonMod.hjson", es_ingles=False)
if "Despertar: {" in leer("es-MX_Mods.AethonMod.hjson"):
    print("SKIP — es-MX ya operada")
else:
    operar("es-MX_Mods.AethonMod.hjson", es_ingles=False)
if "Despertar: {" in leer("en-US_Mods.AethonMod.hjson"):
    print("SKIP — en-US ya operada")
else:
    operar("en-US_Mods.AethonMod.hjson", es_ingles=True)

# ======================================================================
# LA VERIFICACIÓN DE LA CASA: es-MX == es-ES (espejo exacto)
# ======================================================================
es = leer("es-ES_Mods.AethonMod.hjson")
mx = leer("es-MX_Mods.AethonMod.hjson")

def claves(s):
    return [l.strip() for l in s.splitlines()
            if re.match(r"^\s*[A-Za-z0-9_.]+:", l) and not l.strip().startswith("//")]

ces, cmx = claves(es), claves(mx)
assert ces == cmx, f"es-MX != es-ES: faltan {set(ces) - set(cmx)} / sobran {set(cmx) - set(ces)}"
print(f"VERIFICADO: es-MX == es-ES ({len(ces)} claves)")
