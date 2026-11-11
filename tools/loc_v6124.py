#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
loc_v6124.py — v6.124→v6.125 — LA LOCALIZACIÓN DE LA SESIÓN (×3 idiomas):
  · Mueren TajoOscuro y AgujasVacio; nace EL TAJO DEL VACÍO.
  · El Juicio de los Dos Soles: el tooltip de los SOLES RÚNICOS.
  · La Bolsa de la Superforja: las dos armas.
  · EL SET DE DIÁLOGOS EXCLUSIVOS DEL MUNDO INTERIOR (Eco.Interior1..8).
  · LAS TRES ARMAS DE INVESTIGACIÓN: DazzlingStabberStaff (Calamity),
    ArbitradorII y ElPrincipioYElFin (Calamity Entropy) + sus buffs.
USO: python3 tools/loc_v6124.py
"""
import re
import sys

RAIZ = "AethonMod/Localization"

# ======================================================================
#  LOS TEXTOS (es-MX · es-ES · en-US)
# ======================================================================

TAJO_ES = '''	TajoDelVacio: {
		// v6.124 — THE VOID SLASH: the beam-teardrop of the letter
		// ("usa un pequeño haz usando nuestros hazes, el inicio y final
		// del tajo tipo haz sera muy fino en el centro se vuelve mas
		// grueso... negro en el centro, morado en los bordes").
		DisplayName: El Tajo del Vacío
		Tooltip:
			\'\'\'
			EL TAJO-HAZ: cada uso clava HASTA TRES TAJOS cerca de tus enemigos
			(los más cercanos al cursor) — pequeños haces FINOS en el inicio y el
			final y GRUESOS en el centro, con el CORAZÓN NEGRO y los BORDES
			MORADOS que arden: la lágrima de vacío que se clava como aguja y
			atraviesa a todo el que toca.
			\'\'\'
	}
'''

JUICIO_ES = '''	JuicioDosSoles: {
		// v6.124 — REBUILT ON THE RUNE SUNS of the house (the letter: "no
		// usan los soles runicos que tenemos como base para sus soles").
		DisplayName: El Juicio de los Dos Soles
		Tooltip:
			\'\'\'
			EL ATAQUE DEFINITIVO DE LUZ Y OSCURIDAD: mantén el clic y los DOS SOLES
			RÚNICOS nacen — el SOL BLANCO con su paleta de oro y el SOL NEGRO con
			sus anillos violeta — y vuelan al punto que miras. Se funden en EL SOL
			FUSIONADO, más grande y devastador, que se hincha cargando la
			sentencia… y ESTALLA en LA NOVA: el doble círculo de bruma dorada, la
			lluvia de runas de eco y el anillo de choque que golpea a todo lo que
			viva en 460 px.
			\'\'\'
	}
'''

BOLSA_ES = '''	BolsaSuperforja: {
		// v6.124 — THE SUPERFORGE BAG: the two weapons of the letter
		// (the Tajo de Oscuridad and the Agujas del Vacío died; the
		// Tajo del Vacío — the beam-slash — was born).
		DisplayName: La Bolsa de la Superforja
		Tooltip:
			\'\'\'
			LAS ARMAS DEFINITIVAS DE LA SUPERFORJA: El Juicio de los Dos Soles
			(los dos soles rúnicos que se funden en el sol más grande y devastador
			y estallan en la nova de bruma y runas) y El Tajo del Vacío (los
			tajos-haz finos en los extremos y gruesos al centro — negro el
			corazón, morado el filo — que se clavan cerca de tus enemigos y los
			atraviesan).
			\'\'\'
	}
'''

INTERIOR_ES = '''	// v6.124 — EL SET EXCLUSIVO DEL MUNDO INTERIOR (la letra: «el
	// grimorio tendra un nuevo set de dialogos solo dentro del mundo
	// interior, esto hara que los dialogos anteriores no se activen y
	// solo se activen los nuevos dialogos exclusivos del mundo
	// interior»): DENTRO, el libro NO susurra el hambre, NO opina de
	// las armas, NO celebra a los jefes — SOLO habla ESTE repertorio
	// (la voz del Eterno soñando su propia panza, cada ~25 s y en cada
	// momento que antes hablaba otra voz).
	Interior: {
		Interior1: Estás dentro… ¿lo notas? Aquí hasta el silencio tiene sabor a tinta.
		Interior2: Todo lo que comí vive en estas paredes. Camínalas con respeto.
		Interior3: Los tentáculos del cielo son mi respiración. No te apures: no muerden a su dueño.
		Interior4: Si escuchas páginas girar… no es el viento. Soy yo, soñando lo que devoré.
		Interior5: Este llano fue una biblioteca entera. Ahora es UNA página. Aprendí a plegar.
		Interior6: Las brumas de aquí recuerdan a cada jefe. La que te roza… esa es del que más me costó.
		Interior7: ¿Quieres salir? El pilar es mi única puerta. Yo también tengo una columna, de otro tipo.
		Interior8: Quédate el tiempo que quieras… pero no te duermas: los sueños, aquí dentro, se me contagian.
	}

'''

DAZZLING_ES = '''	DazzlingStabberStaff: {
		// v6.124 — THE RESEARCH COPY of Calamity Mod's Dazzling Stabber
		// Staff (code + sprite 100% from the public repo — drop of
		// Providence, the Profaned Goddess).
		DisplayName: Dazzling Stabber Staff
		Tooltip:
			\'\'\'
			INVESTIGACIÓN · CALAMITY MOD: el bastón de las TRES DAGAS SAGRADAS.
			Invoca 3 dagas por uso (ocupa 3 ranuras de esbirro): la daga de
			CRISTAL ataca rápido y descansa (50% del daño, 20 de penetración),
			la daga de ROCA embiste y retrocede (¡300% del daño!) y la daga de
			FUEGO barre en arco amplio y prende las LLAMAS SAGRADAS.
			¡En garde!
			\'\'\'
	}
'''

ARBITRADOR_ES = '''	ArbitradorII: {
		// v6.124 — THE RESEARCH COPY of Calamity Entropy's Arbitrator II
		// (the official wiki sheet — javelin that pierces walls).
		DisplayName: El Arbitrador II
		Tooltip:
			\'\'\'
			INVESTIGACIÓN · CALAMITY ENTROPY: la JABALINA que ATRAVIESA MUROS.
			250 daño · uso lento · 16% crítico. Al golpear, ESTALLA y daña a todo
			lo que viva en el área. Con el SIGILO cargado (2,5 s quieto) se CLAVA
			en la presa… y a los 1 segundo libera la EXPLOSIÓN EN X, cuatro
			brazos de mayor alcance que barren a todo lo que cruzan.
			\'\'\'
		Titulo: ═══ EL ARBITRADOR II — CALAMITY ENTROPY ═══
		Linea1: La javalina del arbitraje: atraviesa MUROS y estalla al golpear.
		Linea2: SIGILOSO (2,5 s quieto): se CLAVA en la presa…
		Linea3: …y a los 1 segundo, la EXPLOSIÓN EN X de mayor alcance.
		Linea4: 250 daño · uso 32 · empuje 1 · 16% crítico.
		Receta: La receta del original: 4× Velo de Sombra Solar + 6× Lingote de Escoria + 1× Bote de Células de Peste (yunque de mitril). Aquí: madera (investigación).
	}
'''

PRINCIPIO_ES = '''	ElPrincipioYElFin: {
		// v6.124 — THE RESEARCH COPY of Calamity Entropy's The Beginning
		// and The End (the official wiki sheet — the gold and blue knives).
		DisplayName: El Principio y el Fin
		Tooltip:
			\'\'\'
			INVESTIGACIÓN · CALAMITY ENTROPY: las DAGAS DEL CANJE. 2500 daño ·
			100 penetración · atraviesan muros. IZQUIERDA: la daga de ORO marca
			LA MARCA DEL PRINCIPIO. DERECHA: la daga AZUL clava LA MIRADA DEL
			FIN. Ambas acumulan hasta 8 CAPAS… y golpear con el OTRO color
			LIBERA la marca: cada capa suelta su ALMA (las de eco persiguen a
			tus enemigos con ondas que atraviesan; las de luz inferior te curan
			y te sirven). SIGILOSO: dos dagas rastreadoras que revientan en
			almas.
			\'\'\'
		Titulo: ═══ EL PRINCIPIO Y EL FIN — CALAMITY ENTROPY ═══
		Linea1: IZQUIERDA: la daga de ORO — LA MARCA DEL PRINCIPIO (8 capas).
		Linea2: DERECHA: la daga AZUL — LA MIRADA DEL FIN (8 capas).
		Linea3: Golpear con el OTRO color LIBERA la marca en ALMAS: las de ECO
		Linea4: persiguen a tus enemigos con ondas que ATRAVIESAN; las de LUZ
		Linea5: INFERIOR te curan y te SIRVEN (+daño, +velocidad, +crítico).
		Receta: La receta del original: 12× Colmillo de Dragón Fantasma en el Altar del Abismo. Aquí: madera (investigación).
	}
'''

BUFFS_INV_ES = '''	// v6.124 — LOS BUFFS DE LAS INVESTIGACIONES (la copia local de los
	// efectos de Calamity y Calamity Entropy).
	DazzlingStabberBuff: {
		DisplayName: Dazzling Stabber
		Description: ¡En garde! Las dagas sagradas velan por ti
	}
	LlamasSagradas: {
		// (Holy Flames de Calamity — la daga de FUEGO la prende, 3 s)
		DisplayName: Llamas sagradas
		Description: La luz sagrada te disuelve
	}
	MarcaDelPrincipio: {
		// (冥煌烙印 — la daga de ORO; hasta 8 capas)
		DisplayName: La Marca del Principio
		Description: La marca de la luz inferior arde en ti — las capas esperan su canje
	}
	MiradaDelFin: {
		// (虚渊凝视 — la daga AZUL; hasta 8 capas)
		DisplayName: La Mirada del Fin
		Description: El abismo vacío te mira — las capas esperan su canje
	}
	SupresionVital: {
		// (生命压制 — ambas dagas, 10 s)
		DisplayName: Supresión vital
		Description: Has visto tu muerte
	}
	Servidumbre: {
		// (使役 — el alma de luz inferior al portador, 3 s)
		DisplayName: Servidumbre
		Description: El alma sirviente: +daño, +velocidad de uso y +crítico
	}
'''

# ======================================================================
#  EN-US (las mismas llaves, el inglés de la casa)
# ======================================================================

TAJO_EN = TAJO_ES.replace(
    "DisplayName: El Tajo del Vacío",
    "DisplayName: The Void Slash").replace(
    "EL TAJO-HAZ: cada uso clava HASTA TRES TAJOS cerca de tus enemigos\n\t\t\t(los más cercanos al cursor) — pequeños haces FINOS en el inicio y el\n\t\t\tfinal y GRUESOS en el centro, con el CORAZÓN NEGRO y los BORDES\n\t\t\tMORADOS que arden: la lágrima de vacío que se clava como aguja y\n\t\t\tatraviesa a todo el que toca.",
    "THE BEAM-SLASH: each use stabs UP TO THREE SLASHES near your enemies\n\t\t\t(the closest to your cursor) — small beams THIN at both ends and THICK\n\t\t\tin the middle, with the BLACK core and the burning PURPLE edges: the\n\t\t\tvoid teardrop that stabs like a needle and pierces everything it touches.")

JUICIO_EN = '''	JuicioDosSoles: {
		DisplayName: The Judgment of the Two Suns
		Tooltip:
			\'\'\'
			THE DEFINITIVE ATTACK OF LIGHT AND DARKNESS: hold the click and the
			TWO RUNE SUNS are born — the WHITE SUN with its golden palette and
			the BLACK SUN with its violet rings — and they fly to the point you
			look at. They fuse into THE FUSED SUN, bigger and more devastating,
			swelling as it charges the sentence… and it BURSTS into THE NOVA:
			the double ring of golden mist, the rain of echo runes and the
			shockwave that strikes everything alive within 460 px.
			\'\'\'
	}
'''

BOLSA_EN = '''	BolsaSuperforja: {
		DisplayName: The Superforge Bag
		Tooltip:
			\'\'\'
			THE SUPERFORGE'S DEFINITIVE WEAPONS: The Judgment of the Two Suns (the
			two rune suns that fuse into the bigger, devastating sun and burst into
			the mist-and-rune nova) and The Void Slash (the beam-slashes, thin at
			the ends and thick in the middle — black core, burning purple edge —
			that stab near your enemies and pierce through them).
			\'\'\'
	}
'''

INTERIOR_EN = '''	// v6.124 — THE INNER WORLD'S EXCLUSIVE DIALOGUE SET.
	Interior: {
		Interior1: You are inside… do you notice? Even the silence here tastes of ink.
		Interior2: Everything I ever ate lives in these walls. Walk them with respect.
		Interior3: The sky's tentacles are my breathing. Don't worry: they don't bite their owner.
		Interior4: If you hear pages turning… it is not the wind. It's me, dreaming what I devoured.
		Interior5: This plain was once a whole library. Now it is ONE page. I learned to fold.
		Interior6: The mists here remember every boss. The one brushing you… that one cost me the most.
		Interior7: Want to leave? The pillar is my only door. I also have a spine, of another kind.
		Interior8: Stay as long as you wish… but don't fall asleep: dreams, in here, are contagious to me.
	}

'''

DAZZLING_EN = '''	DazzlingStabberStaff: {
		DisplayName: Dazzling Stabber Staff
		Tooltip:
			\'\'\'
			RESEARCH · CALAMITY MOD: the staff of the THREE HOLY DAGGERS. Summons
			3 daggers per use (takes 3 minion slots): the CRYSTAL dagger strikes
			fast then rests (50% damage, 20 armor penetration), the ROCK dagger
			lunges and recoils (300% damage!) and the FLAME dagger sweeps in a
			wide arc and ignites the HOLY FLAMES. En garde!
			\'\'\'
	}
'''

ARBITRADOR_EN = '''	ArbitradorII: {
		DisplayName: The Arbitrator II
		Tooltip:
			\'\'\'
			RESEARCH · CALAMITY ENTROPY: the JAVELIN that PIERCES WALLS. 250
			damage · slow use · 16% crit. On hit it BURSTS, damaging everything
			alive in the area. With STEALTH charged (2.5 s still) it STICKS into
			the prey… and 1 second later releases the X BURST, four arms of
			greater range that sweep everything they cross.
			\'\'\'
		Titulo: ═══ THE ARBITRATOR II — CALAMITY ENTROPY ═══
		Linea1: The javelin of arbitration: pierces WALLS and bursts on hit.
		Linea2: STEALTHED (2.5 s still): it STICKS into the prey…
		Linea3: …and 1 second later, the greater-range X BURST.
		Linea4: 250 damage · use 32 · knockback 1 · 16% crit.
		Receta: The original recipe: 4× Solar Veil + 6× Slag Ingot + 1× Plague Cell Vat (mythril anvil). Here: wood (research).
	}
'''

PRINCIPIO_EN = '''	ElPrincipioYElFin: {
		DisplayName: The Beginning and The End
		Tooltip:
			\'\'\'
			RESEARCH · CALAMITY ENTROPY: the DAGGERS OF THE EXCHANGE. 2500
			damage · 100 armor penetration · they pierce walls. LEFT: the GOLD
			dagger brands THE BRAND OF THE BEGINNING. RIGHT: the BLUE dagger
			pins THE GAZE OF THE END. Both stack up to 8 LAYERS… and striking
			with the OTHER color RELEASES the mark: every layer spawns its SOUL
			(the echo souls hunt your enemies with piercing waves; the
			netherlight souls heal and serve you). STEALTHED: two homing daggers
			that burst into souls.
			\'\'\'
		Titulo: ═══ THE BEGINNING AND THE END — CALAMITY ENTROPY ═══
		Linea1: LEFT: the GOLD dagger — THE BRAND OF THE BEGINNING (8 layers).
		Linea2: RIGHT: the BLUE dagger — THE GAZE OF THE END (8 layers).
		Linea3: Striking with the OTHER color RELEASES the mark into SOULS: the
		Linea4: ECHO ones hunt your enemies with PIERCING waves; the
		Linea5: NETHERLIGHT ones heal you and SERVE you (+damage, +speed, +crit).
		Receta: The original recipe: 12× Phantom Dragon Tooth at the Abyssal Altar. Here: wood (research).
	}
'''

BUFFS_INV_EN = '''	// v6.124 — THE RESEARCH BUFFS (local copies of Calamity's effects).
	DazzlingStabberBuff: {
		DisplayName: Dazzling Stabber
		Description: En garde! The holy daggers watch over you
	}
	LlamasSagradas: {
		DisplayName: Holy Flames
		Description: Dissolving from holy light
	}
	MarcaDelPrincipio: {
		DisplayName: Brand of the Beginning
		Description: The netherlight brand burns in you — its layers await the exchange
	}
	MiradaDelFin: {
		DisplayName: Gaze of the End
		Description: The void abyss watches you — its layers await the exchange
	}
	SupresionVital: {
		DisplayName: Life Suppression
		Description: You have seen your death
	}
	Servidumbre: {
		DisplayName: Servitude
		Description: The serving soul: +damage, +use speed and +crit
	}
'''

# ======================================================================
#  LA APLICACIÓN (por idioma, indentación-agnóstica)
# ======================================================================

def reemplazar_bloque(s, nombre, nuevo, obligatorio=True):
    """Reemplaza el bloque `Nombre: { ... }` (o lo INSERTA si no existe)."""
    pat = re.compile(r'^[ \t]*' + re.escape(nombre) + r': \{.*?\n[ \t]*\}\n',
                     re.S | re.M)
    m = pat.search(s)
    if m:
        return s[:m.start()] + nuevo.rstrip('\n') + '\n' + s[m.end():]
    if obligatorio:
        raise SystemExit(f"BLOQUE NO ENCONTRADO: {nombre}")
    return s


def borrar_bloque(s, nombre):
    pat = re.compile(r'^[ \t]*' + re.escape(nombre) + r': \{.*?\n[ \t]*\}\n(?:\n)?',
                     re.S | re.M)
    return pat.sub('', s)


def aplicar(archivo, lang):
    s = open(archivo, encoding='utf-8').read()
    es = lang.startswith('es')

    # 1) LAS DOS ARMAS MUERTAS → EL TAJO DEL VACÍO
    s = borrar_bloque(s, 'TajoOscuro')
    s = borrar_bloque(s, 'AgujasVacio')
    # (la línea comentada del DisplayName del proyectil muerto, si vive)
    s = re.sub(r'^[ \t]*// TajoOscuroProj\.DisplayName:.*\n', '', s,
               flags=re.M)

    # 2) EL JUICIO + LA BOLSA (tooltips nuevos)
    s = reemplazar_bloque(s, 'JuicioDosSoles', JUICIO_ES if es else JUICIO_EN)
    s = reemplazar_bloque(s, 'BolsaSuperforja', BOLSA_ES if es else BOLSA_EN)
    # el tajo nuevo entra donde vivió el viejo (después del Juicio)
    s = reemplazar_bloque(s, 'JuicioDosSoles',
                          (JUICIO_ES if es else JUICIO_EN).rstrip('\n') + '\n\n' +
                          (TAJO_ES if es else TAJO_EN).rstrip('\n') + '\n')

    # 3) LAS ARMAS DE INVESTIGACIÓN (después de la bolsa nueva)
    s = reemplazar_bloque(
        s, 'BolsaSuperforja',
        (BOLSA_ES if es else BOLSA_EN).rstrip('\n') + '\n\n' +
        (DAZZLING_ES if es else DAZZLING_EN).rstrip('\n') + '\n\n' +
        (ARBITRADOR_ES if es else ARBITRADOR_EN).rstrip('\n') + '\n\n' +
        (PRINCIPIO_ES if es else PRINCIPIO_EN).rstrip('\n') + '\n')

    # 4) EL SET DE DIÁLOGOS DEL INTERIOR — al final de la sección Eco
    #    (antes de la llave que la cierra: buscamos el bloque Eco {...}
    #    y lo insertamos justo antes de su cierre)
    if 'Eco.Interior1' not in s and '\tInterior: {' not in s:
        m = re.search(r'^Eco: \{.*?^\}\n', s, re.S | re.M)
        if not m:
            raise SystemExit("SECCIÓN Eco NO ENCONTRADA en " + archivo)
        eco = m.group(0)
        eco_nuevo = eco.rstrip('\n')
        # insertar antes de la última llave de cierre
        idx = eco_nuevo.rfind('\n}')
        eco_nuevo = eco_nuevo[:idx] + '\n\n' + \
            (INTERIOR_ES if es else INTERIOR_EN).rstrip('\n') + eco_nuevo[idx:]
        s = s[:m.start()] + eco_nuevo + '\n' + s[m.end():]

    # 5) LOS BUFFS DE LAS INVESTIGACIONES — al final de la sección Buffs
    if 'LlamasSagradas: {' not in s:
        m = re.search(r'^Buffs: \{.*?^\}\n', s, re.S | re.M)
        if not m:
            raise SystemExit("SECCIÓN Buffs NO ENCONTRADA en " + archivo)
        buffs = m.group(0)
        idx = buffs.rstrip('\n').rfind('\n}')
        buffs_nuevo = buffs.rstrip('\n')[:idx] + '\n\n' + \
            (BUFFS_INV_ES if es else BUFFS_INV_EN).rstrip('\n') + \
            buffs.rstrip('\n')[idx:]
        s = s[:m.start()] + buffs_nuevo + '\n' + s[m.end():]

    open(archivo, 'w', encoding='utf-8').write(s)
    print(f"  ✓ {archivo}")


print("LA LOCALIZACIÓN v6.124:")
aplicar(f"{RAIZ}/es-MX_Mods.AethonMod.hjson", "es-MX")
aplicar(f"{RAIZ}/es-ES_Mods.AethonMod.hjson", "es-ES")
aplicar(f"{RAIZ}/en-US_Mods.AethonMod.hjson", "en-US")
print("LOCALIZACIÓN COMPLETA (×3).")
