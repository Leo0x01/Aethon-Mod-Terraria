#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
loc_v6114.py — LA LOCALIZACIÓN DE LA v6.114 (los 3 espejos):
  · EL TOOLTIP DE LAS ALAS DEL VACÍO reescrito (la membrana + el arco:
    la cura real de la sierra).
  · LA FORMA ETERNA (el cosmético definitivo) + SU BOLSA.
  · LA CEREMONIA DE LA LLAMADA (el anuncio del altar + el grimorio).
  · EL SUBMUNDO DEL GRIMORIO (los mensajes del mundo interior).
"""

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization"

ALAS_VACIO = {
"es-MX": """\
			'''
			        La copia mejorada del eclipse, SIN SIERRA: la MEMBRANA une
			        las plumas y el ARCO exterior borda el ala de punta a punta —
			        UNA curva continua, sin un solo pico. Simétrica izquierda y
			        derecha, del COLOR DEL VACÍO (la rampa del aura) y con SUS
			        efectos: el anillo de runas violeta en las puntas, la niebla
			        que corre por dentro y las migas que caen de las alas.
			        Vuelo 240 · sin daño de caída · se pliega ante alas reales.
			'''""",
"es-ES": """\
			'''
			        La copia mejorada del eclipse, SIN SIERRA: la MEMBRANA une
			        las plumas y el ARCO exterior borda el ala de punta a punta —
			        UNA curva continua, sin un solo pico. Simétrica izquierda y
			        derecha, del COLOR DEL VACÍO (la rampa del aura) y con SUS
			        efectos: el anillo de runas violeta en las puntas, la niebla
			        que corre por dentro y las migas que caen de las alas.
			        Vuelo 240 · sin daño de caída · se pliega ante alas reales.
			'''""",
"en-US": """\
			'''
			        The eclipse's improved copy, SAW-FREE: the MEMBRANE joins
			        the feathers and the outer ARC hems the wing from tip to
			        tip — ONE continuous curve, not a single spike. Left/right
			        symmetric, in the COLOR OF THE VOID (the aura's ramp) and
			        with ITS effects: the violet rune ring at the tips, the
			        mist running through it and the crumbs falling from it.
			        Flight 240 · no fall damage · folds before real wings.
			'''""",
}

ITEMS_NUEVOS = {
"es-MX": """
	// v6.114 — EL COSMÉTICO DEFINITIVO (la letra: «es momento de crear
	// el item cosmetico definitivo… un unico accesorio»).
	FormaEternaItem: {
		DisplayName: La Forma Eterna
		Tooltip:
			'''
			        ✦ EL COSMÉTICO DEFINITIVO — TODO EL PANTEÓN EN UN SOLO CUERPO.
			        VISTE LOS OCHO: la Forma Ascendida y LAS OFANIM (las dos
			        luces vivas + el VUELO INFINITO), la Brasa del Eclipse, el
			        Anillo Rúnico Dorsal y el Brillo de los Soles Rúnicos.
			        El Sello del Génesis y los Anillos del Sol Rúnico (+40 maná,
			        +8% mágico, +8% melee, +3 defensa) y EL AURA DEL VACÍO: el
			        campo invisible que viste a los impíos.
			        «Todo lo que el libro ha dado, en un solo cuerpo — la forma
			        que no se apaga.»
			'''
	}

	BolsaFormaEterna: {
		// v6.114 — LA BOLSA NUEVA (la letra: «los accesorios mencionados
		// se deben guardar en una nueva bolsa junto con el accesorio
		// definitivo»).
		DisplayName: La Bolsa de la Forma Eterna
		Tooltip:
			'''
			        LOS OCHO DEL PANTEÓN + LA FORMA ETERNA: la brasa del eclipse,
			        el anillo rúnico dorsal, el sello del génesis, los anillos del
			        sol rúnico, el brillo de los soles rúnicos, la forma ascendida,
			        las ofanim y el aura del vacío… y el UNO que los viste todos.
			'''
	}
""",
"es-ES": """
	// v6.114 — EL COSMÉTICO DEFINITIVO (la letra: «es momento de crear
	// el item cosmetico definitivo… un unico accesorio»).
	FormaEternaItem: {
		DisplayName: La Forma Eterna
		Tooltip:
			'''
			        ✦ EL COSMÉTICO DEFINITIVO — TODO EL PANTEÓN EN UN SOLO CUERPO.
			        VISTE LOS OCHO: la Forma Ascendida y LAS OFANIM (las dos
			        luces vivas + el VUELO INFINITO), la Brasa del Eclipse, el
			        Anillo Rúnico Dorsal y el Brillo de los Soles Rúnicos.
			        El Sello del Génesis y los Anillos del Sol Rúnico (+40 maná,
			        +8% mágico, +8% melee, +3 defensa) y EL AURA DEL VACÍO: el
			        campo invisible que viste a los impíos.
			        «Todo lo que el libro ha dado, en un solo cuerpo — la forma
			        que no se apaga.»
			'''
	}

	BolsaFormaEterna: {
		// v6.114 — LA BOLSA NUEVA (la letra: «los accesorios mencionados
		// se deben guardar en una nueva bolsa junto con el accesorio
		// definitivo»).
		DisplayName: La Bolsa de la Forma Eterna
		Tooltip:
			'''
			        LOS OCHO DEL PANTEÓN + LA FORMA ETERNA: la brasa del eclipse,
			        el anillo rúnico dorsal, el sello del génesis, los anillos del
			        sol rúnico, el brillo de los soles rúnicos, la forma ascendida,
			        las ofanim y el aura del vacío… y el UNO que los viste todos.
			'''
	}
""",
"en-US": """
	// v6.114 — THE DEFINITIVE COSMETIC (the letter: "time to create the
	// definitive cosmetic item… a single accessory").
	FormaEternaItem: {
		DisplayName: The Eternal Form
		Tooltip:
			'''
			        ✦ THE DEFINITIVE COSMETIC — THE WHOLE PANTHEON IN ONE BODY.
			        IT WEARS THE EIGHT: the Ascended Form and THE OPHANIM (the
			        two living lights + INFINITE FLIGHT), the Eclipse Ember, the
			        Dorsal Runic Ring and the Runic Suns' Shine.
			        The Seal of Genesis and the Runic Sun Rings (+40 mana, +8%
			        magic, +8% melee, +3 defense) and THE VOID AURA: the
			        invisible field that dresses the unholy.
			        "All the book ever gave, in one body — the form that
			        never fades."
			'''
	}

	BolsaFormaEterna: {
		// v6.114 — THE NEW BAG (the letter: "the mentioned accessories
		// must be kept in a new bag together with the definitive
		// accessory").
		DisplayName: The Bag of the Eternal Form
		Tooltip:
			'''
			        THE EIGHT OF THE PANTHEON + THE ETERNAL FORM: the eclipse
			        ember, the dorsal runic ring, the seal of genesis, the runic
			        sun rings, the runic suns' shine, the ascended form, the
			        ophanim and the void aura… and the ONE wearing them all.
			'''
	}
""",
}

LLAMADA = {
"es-MX": "\t\t// v6.114 — LA CEREMONIA DE LA LLAMADA: el altar se alza y el\n\t\t// grimorio del eterno flota JUSTO ENCIMA (la letra).\n\t\tLlamadaCeremonia: LA LLAMADA CONTESTA — el altar se alza y el grimorio se abre: EL LIBRO HA ESCUCHADO SU NOMBRE.\n",
"es-ES": "\t\t// v6.114 — LA CEREMONIA DE LA LLAMADA: el altar se alza y el\n\t\t// grimorio del eterno flota JUSTO ENCIMA (la letra).\n\t\tLlamadaCeremonia: LA LLAMADA CONTESTA — el altar se alza y el grimorio se abre: EL LIBRO HA ESCUCHADO SU NOMBRE.\n",
"en-US": "\t\t// v6.114 — THE CALL'S CEREMONY: the altar rises and the eternal\n\t\t// grimoire floats RIGHT ABOVE it (the letter).\n\t\tLlamadaCeremonia: THE CALL ANSWERS — the altar rises and the grimoire opens: THE BOOK HAS HEARD ITS NAME.\n",
}

SUBMUNDO = {
"es-MX": """
// v6.114 — EL MUNDO INTERIOR DEL GRIMORIO ETERNO (la letra: «necesito
// crear un mundo nuevo que sera el mundo interior del grimorio del
// eterno, estara lleno de bruma y de momento se puede entrar usando el
// altar»).
Submundo: {
	Nombre: El Interior del Eterno
	SoloSP: El interior del libro solo se abre en mundos de UN JUGADOR — por ahora.
	YaDentro: El libro ya está abierto — el interior te rodea.
	Cargando: El libro se abre…
	Cerrando: El libro se cierra…
	Bienvenida: EL INTERIOR DEL ETERNO — la bruma lo es todo. El altar de la plaza es la vuelta a casa.
	Despedida: El libro se cierra — el mundo real te devuelve.
	RedCaida: EL VACÍO TE DEVUELVE A LA PLAZA — nadie cae dentro del libro.
}

""",
"es-ES": """
// v6.114 — EL MUNDO INTERIOR DEL GRIMORIO ETERNO (la letra: «necesito
// crear un mundo nuevo que sera el mundo interior del grimorio del
// eterno, estara lleno de bruma y de momento se puede entrar usando el
// altar»).
Submundo: {
	Nombre: El Interior del Eterno
	SoloSP: El interior del libro solo se abre en mundos de UN JUGADOR — por ahora.
	YaDentro: El libro ya está abierto — el interior te rodea.
	Cargando: El libro se abre…
	Cerrando: El libro se cierra…
	Bienvenida: EL INTERIOR DEL ETERNO — la bruma lo es todo. El altar de la plaza es la vuelta a casa.
	Despedida: El libro se cierra — el mundo real te devuelve.
	RedCaida: EL VACÍO TE DEVUELVE A LA PLAZA — nadie cae dentro del libro.
}

""",
"en-US": """
// v6.114 — THE ETERNAL GRIMOIRE'S INNER WORLD (the letter: "I need to
// create a new world that will be the eternal grimoire's inner world,
// full of mist, and for now you enter it using the altar").
Submundo: {
	Nombre: The Eternal's Interior
	SoloSP: The book's interior only opens in SINGLE PLAYER worlds — for now.
	YaDentro: The book is already open — the interior surrounds you.
	Cargando: The book opens…
	Cerrando: The book closes…
	Bienvenida: THE ETERNAL'S INTERIOR — the mist is everything. The plaza's altar is the way home.
	Despedida: The book closes — the real world takes you back.
	RedCaida: THE VOID RETURNS YOU TO THE PLAZA — no one falls inside the book.
}

""",
}


def parchar(lang):
    ruta = f"{BASE}/{lang}_Mods.AethonMod.hjson"
    src = open(ruta, encoding="utf-8").read()

    # (1) EL TOOLTIP DE LAS ALAS DEL VACÍO — reemplazo del bloque '''...'''
    marca_ini = "	AlasRunicasDelVacio: {\n"
    i = src.index(marca_ini)
    j = src.index("'''", i)           # apertura del tooltip
    k = src.index("'''", j + 3) + 3   # cierre del tooltip
    bloque = src[i:k]
    nuevo_alas = marca_ini + "\t\tDisplayName: Alas Rúnicas del Vacío\n\t\tTooltip:\n" + ALAS_VACIO[lang]
    if lang == "en-US":
        nuevo_alas = marca_ini + "\t\tDisplayName: Void Runic Wings\n\t\tTooltip:\n" + ALAS_VACIO[lang]
    src = src[:i] + nuevo_alas + src[k:]

    # (2) LOS ÍTEMS NUEVOS — justo tras el bloque BolsaAuras
    marca_bolsa = "	BolsaAuras: {"
    b = src.index(marca_bolsa)
    # el cierre del bloque: la línea "\t}" tras el tooltip
    c = src.index("\n\t}\n", b) + len("\n\t}\n")
    src = src[:c] + ITEMS_NUEVOS[lang].lstrip("\n") + "\n" + src[c:]

    # (3) LA LLAMADA — dentro del bloque Aethon
    marca_aethon = "\tAethon: {\n"
    a = src.index(marca_aethon) + len(marca_aethon)
    src = src[:a] + LLAMADA[lang] + src[a:]

    # (4) EL SUBMUNDO — antes de Rarities.LegendarioSupremo
    marca_rar = "\nRarities.LegendarioSupremo"
    r = src.index(marca_rar)
    src = src[:r] + "\n" + SUBMUNDO[lang].rstrip("\n") + "\n" + src[r:]

    open(ruta, "w", encoding="utf-8").write(src)
    print(f"  {lang}: OK")


print("LOC v6.114:")
for lang in ("es-MX", "es-ES", "en-US"):
    parchar(lang)
print("LOS 3 ESPEJOS LISTOS")
