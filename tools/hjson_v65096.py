#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.50.96 — CIRUGÍA hjson (la casa: strings EXACTOS con aserciones de
conteo — los TABS del en-US NUNCA se tocan).

La letra del usuario: «el nuevo proyectil lo usaste tal cual, no le diste
efectos visuales, digamos que es un grieta como debe ser, entonces dale
muchos efectos visuales […] cuanto mas efectos mejor».

  (1) LA GRIETA: la «Lágrima de Tinta» es re-leída como lo que siempre
      fue — UNA GRIETA. Los tooltips del libro (original y Nervioso)
      ahora ABREN la grieta en vez de escupir la lágrima.
  (2) TINTA VIVA: el tooltip del arma cuenta el stack nuevo (estrella
      de ruptura, aspiración, ribbon del vacío, aberración cromática,
      glitch de tajas, el ojo rasgado, el corte que sana).

es-MX ya fue editado a mano con el MISMO texto que aquí se verifica;
este tool hace la cirugía en-US (tabs), regenera el espejo es-ES y
corre la sanidad ×3.
"""
import io

MX = 'AethonMod/Localization/es-MX_Mods.AethonMod.hjson'
EN = 'AethonMod/Localization/en-US_Mods.AethonMod.hjson'
ES = 'AethonMod/Localization/es-ES_Mods.AethonMod.hjson'


def reemplazar(texto, viejo, nuevo, archivo):
    n = texto.count(viejo)
    assert n == 1, f'{archivo}: el bloque viejo aparece {n} veces (quería 1)'
    return texto.replace(viejo, nuevo)


# ================= en-US (TABS — ni un tab se toca) =================
# (1) El clic izq del Nervioso (tooltip a columna 0)
CLIC_EN = """Left click: spits THE INK TEAR — the book's OWN
homing shot (testing, no mana).
Right click: resets the appetite.
Testing item — compare against the original."""
CLIC_EN_NUEVO = """Left click: opens THE INK RIFT — the book's OWN
homing shot (testing, no mana).
Right click: resets the appetite.
Testing item — compare against the original."""

# (1) El clic izq del ORIGINAL (tooltip con tabs \t\t\t)
CLIC_BASE_EN = """\t\t\tLeft click: spits THE INK TEAR — the book's OWN
\t\t\thoming shot (testing, no mana).
\t\t\tRight click: resets the appetite.
\t\t\tTesting item — remove before publishing."""
CLIC_BASE_EN_NUEVO = """\t\t\tLeft click: opens THE INK RIFT — the book's OWN
\t\t\thoming shot (testing, no mana).
\t\t\tRight click: resets the appetite.
\t\t\tTesting item — remove before publishing."""

# (2) El bloque TintaViva completo (tabs)
TINTA_EN_VIEJO = """\t// v6.50.95 — LIVING INK (the user's letter: "I want you to be
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
\t}"""
TINTA_EN_NUEVO = """\t// v6.50.95/.96 — LIVING INK (the user's letter: "I want you to be
\t// creative with this projectile, animate it and create a new weapon
\t// with the projectile"): the weapon that opens THE INK RIFT — the
\t// book's own shot, born from the user's EXACT sprite and re-read in
\t// .96 as what it always was: A RIFT.
\tTintaViva: {
\t\tDisplayName: Living Ink
\t\tTooltip:
\t\t\t'''
\t\t\t"The first wound the grimoire opened in the world."
\t\t\tOpens INK RIFTS — tears in the page with a heart of
\t\t\tivory: they're born with their rupture star, ASPIRE
\t\t\tthe world in flight, trail a ribbon of void and
\t\t\tghost echoes, shine with chromatic aberration,
\t\t\tglitch into slices, and their heart IS A TORN EYE
\t\t\tthat stares at the prey. They pass through walls
\t\t\tand, when they collapse, they implode leaving
\t\t\tTHE HEALING CUT and THE STAIN.
\t\t\tIf the Nervous one is OUT, the rift flies FROM THE BOOK.
\t\t\tThree prey per rift.
\t\t\t'''
\t}"""


def main():
    # === en-US (tabs intactos) ===
    en = io.open(EN, encoding='utf-8', newline='').read()
    tabs_antes = en.count('\t')
    en = reemplazar(en, CLIC_EN, CLIC_EN_NUEVO, 'EN clic Nervioso')
    en = reemplazar(en, CLIC_BASE_EN, CLIC_BASE_EN_NUEVO, 'EN clic base')
    en = reemplazar(en, TINTA_EN_VIEJO, TINTA_EN_NUEVO, 'EN TintaViva')
    io.open(EN, 'w', encoding='utf-8', newline='').write(en)
    tabs_despues = en.count('\t')
    delta = TINTA_EN_NUEVO.count('\t') - TINTA_EN_VIEJO.count('\t')
    assert tabs_antes + delta == tabs_despues, \
        f'tabs: {tabs_antes}+{delta} != {tabs_despues}'
    print(f'en-US: la grieta en ambos libros + TintaViva re-escrita '
          f'({len(en)} B, tabs {tabs_antes}→{tabs_despues}, delta {delta:+d})')

    # === es-ES: espejo (regenerado — jamás a mano) ===
    mx = io.open(MX, encoding='utf-8', newline='').read()
    CABECERA = (
        '// ESPEJO GENERADO de es-MX_Mods.AethonMod.hjson \u2014 NO EDITAR A MANO.\n'
        '// v6.50.71 \u2014 todas las variantes de espa\u00f1ol leen este espejo del cuerpo\n'
        '// de es-MX (invariante .77: cuerpo byte-id\u00e9ntico; regenerado por\n'
        '// tools/hjson_v65096.py \u2014 la l\u00e1grima es LA GRIETA + el stack de efectos, v6.50.96).\n'
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

    # === SANIDAD ×3: la grieta vive, la lágrima muerta ===
    for f, nom in ((MX, 'es-MX'), (EN, 'en-US'), (ES, 'es-ES')):
        t = io.open(f, encoding='utf-8', newline='').read()
        assert t.count('TintaViva: {') == 1, f'{nom}: falta TintaViva'
        assert 'Tres presas por grieta' in t or 'Three prey per rift' in t, \
            f'{nom}: falta la grieta en TintaViva'
    for f, nom in ((MX, 'es-MX'), (ES, 'es-ES')):
        t = io.open(f, encoding='utf-8', newline='').read()
        assert 'Escupe LÁGRIMAS DE TINTA' not in t, f'{nom}: la lágrima vive'
        assert 'escupe LA LÁGRIMA DE TINTA' not in t, f'{nom}: la lágrima vive'
        assert 'no quiso llorar' not in t, f'{nom}: la lágrima vive'
        assert 'Abre GRIETAS DE TINTA' in t, f'{nom}: falta la grieta del arma'
        assert 'abre LA GRIETA DE TINTA' in t, f'{nom}: falta la grieta del libro'
        assert 'OJO RASGADO' in t, f'{nom}: falta el ojo'
    en_txt = io.open(EN, encoding='utf-8', newline='').read()
    assert 'Spits INK TEARS' not in en_txt, 'en-US: la lágrima vive'
    assert 'spits THE INK TEAR' not in en_txt, 'en-US: la lágrima vive'
    assert 'refused to cry' not in en_txt, 'en-US: la lágrima vive'
    assert 'Opens INK RIFTS' in en_txt, 'en-US: falta la grieta del arma'
    assert 'opens THE INK RIFT' in en_txt, 'en-US: falta la grieta del libro'
    assert 'TORN EYE' in en_txt, 'en-US: falta el ojo'
    print('SANIDAD ×3: LA GRIETA vive en los tres · la lágrima muerta · el ojo presente — OK')


if __name__ == '__main__':
    main()
