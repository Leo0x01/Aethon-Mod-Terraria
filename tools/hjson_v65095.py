#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.50.95 — CIRUGÍA hjson ×3 (la casa: strings EXACTOS con aserciones de
conteo — los TABS del en-US NUNCA se tocan; el 19º incidente no se repite).

La letra del usuario:
  (1) EL COMPÁS MUERE: «quitar la restriccion del compás de la sombra —
      tus dos reglas a la vez […] creo que mejor es dejarlo atacar
      cuando quiera» — el tooltip del Nervioso ya no promete los 10 s
      ni los 2 s: ataca cuando QUIERE.
  (2) LA DESCARGA PRESTADA MUERE: el clic izq (del original Y del
      Nervioso) YA NO dispara el Nightglow prestado — escupe LA LÁGRIMA
      DE TINTA (el disparo propio, el sprite del usuario).
  (3) NUEVA: LA TINTA VIVA — el arma de la lágrima (hijo del sprite del
      usuario, vive en la Bolsa de las Sombras).
  (4) BUG .94 CAZADO: el tooltip del ORIGINAL seguía prometiendo la
      GRACIA de los 10 s y el «(y lo calma 10 s)» — la gracia MURIÓ en
      la .94 (el reloj suma SIEMPRE) y el clic der ya no calma nada.
"""
import io

MX = 'AethonMod/Localization/es-MX_Mods.AethonMod.hjson'
EN = 'AethonMod/Localization/en-US_Mods.AethonMod.hjson'


def reemplazar(texto, viejo, nuevo, archivo):
    n = texto.count(viejo)
    assert n == 1, f'{archivo}: el bloque viejo aparece {n} veces (quería 1)'
    return texto.replace(viejo, nuevo)


# ================= es-MX (tooltips del Nervioso a columna 0) =================
NERV_VIEJO_MX = """flota cerca de ti y su Sombra de la Página devora a
la criatura que se mueva cerca — su ataque se lanza
UNA vez cada 10 s (y nunca antes de 2 s de terminar
el anterior)."""
NERV_NUEVO_MX = """flota cerca de ti y su Sombra de la Página devora a
la criatura que se mueva cerca — ataca cuando QUIERE:
en cuanto termina con una presa, sale tras la
siguiente que se mueva."""

CLIC_MX = """Clic izq: descarga perseguidora (pruebas, sin maná).
Clic der: reinicia el apetito.
Objeto de pruebas — comparar con el original."""
CLIC_MX_NUEVO = """Clic izq: escupe LA LÁGRIMA DE TINTA — la descarga
perseguidora PROPIA del libro (pruebas, sin maná).
Clic der: reinicia el apetito.
Objeto de pruebas — comparar con el original."""

BASE_VIEJO_MX = """El ojo del Códice Vivo: te mira, parpadea y pasa hambre.
                        Si pasan 10 segundos sin comer, el apetito crece de
                        cero a TOTAL en un minuto: el parpadeo se acelera
                        (de 10-20 s a cada 2 s), la mirada se agita y el
                        iris dorado se enciende ROJO.
                        Clic izq: descarga perseguidora (pruebas, sin maná).
                        Clic der: reinicia el apetito (y lo calma 10 s).
                        Objeto de pruebas — eliminar antes de publicar."""
BASE_NUEVO_MX = """El ojo del Códice Vivo: te mira, parpadea y pasa hambre.
                        Su apetito crece de cero a TOTAL en un minuto (tasa
                        de prueba): el parpadeo se acelera (de 10-20 s a
                        cada 2 s), la mirada se agita y el iris dorado se
                        enciende ROJO.
                        Clic izq: escupe LA LÁGRIMA DE TINTA — la descarga
                        perseguidora PROPIA del libro (pruebas, sin maná).
                        Clic der: reinicia el apetito.
                        Objeto de pruebas — eliminar antes de publicar."""

ANCLA_MX = """                        (Arma de prueba de las fauces — v2.)
                        '''
        }
"""
TINTA_MX = """
        // v6.50.95 — LA TINTA VIVA (la letra: «quiero que con este
        // proyectil seas creativo, lo animes y crees una nueva arma con
        // el proyectil»): el arma que escupe LA LÁGRIMA DE TINTA — el
        // disparo propio del libro, nacido del sprite EXACTO del usuario.
        TintaViva: {
                DisplayName: Tinta Viva
                Tooltip:
                        '''
                        «La primera lágrima que el grimorio no quiso llorar.»
                        Escupe LÁGRIMAS DE TINTA — la página doblada en
                        sombra con corazón de marfil: respiran en vuelo,
                        persiguen a tu presa (la tinta aprende a caer),
                        gotean estelas de sombra, atraviesan los muros y,
                        al romperse, salpican y dejan LA MANCHA.
                        Si el Nervioso anda FUERA, la tinta sale DEL LIBRO.
                        Tres presas por lágrima.
                        '''
        }
"""

# ================= en-US (TABS — ni un tab se toca) =================
NERV_VIEJO_EN = """Page Shadow devours whatever creature moves nearby —
its attack fires ONCE every 10 s (and never sooner
than 2 s after the previous one ends)."""
NERV_NUEVO_EN = """Page Shadow devours whatever creature moves nearby —
it strikes WHENEVER IT WANTS: the moment it finishes
one prey, it goes after the next one that moves."""

CLIC_EN = """Left click: homing bolt (testing, no mana).
Right click: resets the appetite.
Testing item — compare against the original."""
CLIC_EN_NUEVO = """Left click: spits THE INK TEAR — the book's OWN
homing shot (testing, no mana).
Right click: resets the appetite.
Testing item — compare against the original."""

BASE_VIEJO_EN = """The Living Codex's eye: it watches, blinks and gets hungry.
\t\t\tIf 10 seconds pass without eating, the appetite grows
\t\t\tfrom zero to FULL in one minute: blinking speeds up
\t\t\t(from 10-20 s to every 2 s), the gaze grows restless
\t\t\tand the golden iris ignites RED.
\t\t\tLeft click: homing bolt (testing, no mana).
\t\t\tRight click: resets the appetite (and calms it 10 s).
\t\t\tTesting item — remove before publishing."""
BASE_NUEVO_EN = """The Living Codex's eye: it watches, blinks and gets hungry.
\t\t\tIts appetite grows from zero to FULL in one minute
\t\t\t(test rate): blinking speeds up (from 10-20 s to
\t\t\tevery 2 s), the gaze grows restless and the golden
\t\t\tiris ignites RED.
\t\t\tLeft click: spits THE INK TEAR — the book's OWN
\t\t\thoming shot (testing, no mana).
\t\t\tRight click: resets the appetite.
\t\t\tTesting item — remove before publishing."""

ANCLA_EN = """\t\t\t(Maw test weapon — v2.)
\t\t\t'''
\t}
"""
TINTA_EN = """\t// v6.50.95 — LIVING INK (the user's letter: "I want you to be
\t// creative with this projectile, animate it and create a new weapon
\t// with the projectile"): the weapon that spits THE INK TEAR — the
\t// book's own shot, born from the user's EXACT sprite.
\tTintaViva: {
\t\tDisplayName: Living Ink
\t\tTooltip:
\t\t\t'''
\t\t\t"The first tear the grimoire refused to cry."
\t\t\tSpits INK TEARS — the page folded into shadow with
\t\t\ta heart of ivory: they breathe in flight, chase your
\t\t\tprey (the ink learns how to fall), drip trails of
\t\t\tshadow, pass through walls and, when they break,
\t\t\tthey splash and leave THE STAIN.
\t\t\tIf the Nervous one is OUT, the ink flies FROM THE BOOK.
\t\t\tThree prey per tear.
\t\t\t'''
\t}
"""


def main():
    # === es-MX ===
    mx = io.open(MX, encoding='utf-8', newline='').read()
    mx = reemplazar(mx, NERV_VIEJO_MX, NERV_NUEVO_MX, 'MX compás')
    mx = reemplazar(mx, CLIC_MX, CLIC_MX_NUEVO, 'MX clic Nervioso')
    mx = reemplazar(mx, BASE_VIEJO_MX, BASE_NUEVO_MX, 'MX base')
    assert mx.count(ANCLA_MX) == 1, 'MX: ancla SombraDeLaPagina no única'
    mx = mx.replace(ANCLA_MX, ANCLA_MX + TINTA_MX)
    io.open(MX, 'w', encoding='utf-8', newline='').write(mx)
    print(f'es-MX: compás fuera + lágrima dentro + base honesta + TintaViva '
          f'({len(mx)} B)')

    # === en-US (tabs intactos) ===
    en = io.open(EN, encoding='utf-8', newline='').read()
    tabs_antes = en.count('\t')
    en = reemplazar(en, NERV_VIEJO_EN, NERV_NUEVO_EN, 'EN compás')
    en = reemplazar(en, CLIC_EN, CLIC_EN_NUEVO, 'EN clic Nervioso')
    en = reemplazar(en, BASE_VIEJO_EN, BASE_NUEVO_EN, 'EN base')
    assert en.count(ANCLA_EN) == 1, 'EN: ancla SombraDeLaPagina no única'
    en = en.replace(ANCLA_EN, ANCLA_EN + '\n' + TINTA_EN)
    io.open(EN, 'w', encoding='utf-8', newline='').write(en)
    tabs_despues = en.count('\t')
    delta_base = BASE_NUEVO_EN.count('\t') - BASE_VIEJO_EN.count('\t')
    delta_esperado = delta_base + TINTA_EN.count('\t')
    assert tabs_antes + delta_esperado == tabs_despues, \
        f'tabs: {tabs_antes}+{delta_esperado} != {tabs_despues}'
    print(f'en-US: compás fuera + lágrima dentro + base honesta + TintaViva '
          f'({len(en)} B, tabs {tabs_antes}→{tabs_despues}, +{delta_esperado})')

    # === es-ES: espejo (regenerado — jamás a mano) ===
    ES = 'AethonMod/Localization/es-ES_Mods.AethonMod.hjson'
    CABECERA = (
        '// ESPEJO GENERADO de es-MX_Mods.AethonMod.hjson \u2014 NO EDITAR A MANO.\n'
        '// v6.50.71 \u2014 todas las variantes de espa\u00f1ol leen este espejo del cuerpo\n'
        '// de es-MX (invariante .77: cuerpo byte-id\u00e9ntico; regenerado por\n'
        '// tools/hjson_v65095.py \u2014 comp\u00e1s muerto + l\u00e1grima de tinta + base honesta, v6.50.95).\n'
        '\n'
    )

    def cuerpo(t):
        lineas = t.split('\n')
        i = 0
        while i < len(lineas) and (lineas[i].startswith('//') or not lineas[i].strip()):
            i += 1
        return '\n'.join(lineas[i:])

    cuerpo_mx = cuerpo(mx)
    io.open(ES, 'w', encoding='utf-8', newline='').write(CABECERA + cuerpo_mx)
    es = io.open(ES, encoding='utf-8', newline='').read()
    assert cuerpo(es) == cuerpo_mx, 'el cuerpo del espejo NO coincide con es-MX'
    print(f'es-ES: espejo regenerado ({len(es)} B — cuerpo == es-MX byte a byte)')

    # === SANIDAD: claves nuevas presentes ×3, difuntas ausentes ×3 ===
    for f, nom in ((MX, 'es-MX'), (EN, 'en-US'), (ES, 'es-ES')):
        t = io.open(f, encoding='utf-8', newline='').read()
        assert t.count('TintaViva: {') == 1, f'{nom}: falta TintaViva'
        assert 'cada 10 s (y nunca antes' not in t, f'{nom}: el compás vive'
        assert 'every 10 s (and never sooner' not in t, f'{nom}: el compás vive'
        assert 'calma 10 s' not in t, f'{nom}: la gracia .92 sigue en el texto'
        assert 'calms it 10 s' not in t, f'{nom}: la gracia .92 sigue en el texto'
    print('SANIDAD ×3: TintaViva presente · compás muerto · gracia muerta — OK')


if __name__ == '__main__':
    main()
