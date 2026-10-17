#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
hjson_v650126.py — LA LOCALIZACIÓN DE LA v6.50.126 (3 idiomas):
  + Items.TintaDelCometa (el activador de LA CAÍDA — item de pruebas)
  + Caida.TintaYaEspera / Caida.TintaEnVuelo (los avisos de la tinta)
La casa: los hjson JAMÁS se tocan con la herramienta Edit (normaliza
tabs) — solo script Python con inserción de texto EXACTO por archivo
(es-MX espacios 8/16/24 · es-ES/en-US tabs 1/2/3).
"""
import io
import sys

BASE = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/'

# ============================================================
#  LOS TEXTOS (es = es-MX y es-ES idénticos; en = en-US)
# ============================================================

ITEM_ES = '''{i}TintaDelCometa: {{
{i2}DisplayName: Tinta del Cometa
{i2}Tooltip:
{i3}\'\'\'
{i3}EL ACTIVADOR DE LA CAÍDA (item de pruebas — la Bolsa de la Cría lo trae).
{i3}Rompe el frasco y el cielo RECUERDA: el cometa del Grimorio
{i3}del Eterno cruza otra vez hacia ti — de día o de noche.
{i3}Los caminos automáticos siguen su ley (la primera noche de
{i3}cada jugador en cada mundo + el primer vecino que no sea el
{i3}Guía); ESTA tinta es el camino MANUAL: el evento NUNCA se
{i3}pierde… y jamás duplica tu grimorio (si ya te espera o ya
{i3}vuela hacia ti, el cielo no insiste).
{i3}\'\'\'
{i}}}
'''

ITEM_EN = '''{i}TintaDelCometa: {{
{i2}DisplayName: Comet Ink
{i2}Tooltip:
{i3}\'\'\'
{i3}THE FALL ACTIVATOR (test item — delivered by the Brood Pouch).
{i3}Break the vial and the sky REMEMBERS: the Eternal Grimoire's
{i3}comet crosses again toward you — day or night.
{i3}The automatic paths keep their law (each player's first night in
{i3}each world + the first neighbor that isn't the Guide); THIS ink
{i3}is the MANUAL path: the event is NEVER lost… and it never
{i3}duplicates your grimoire (if it already awaits you or already
{i3}flies toward you, the sky doesn't insist).
{i3}\'\'\'
{i}}}
'''

CAIDA_ES = '''{i}TintaYaEspera: ✦ El cielo no insiste: TU GRIMORIO YA TE ESPERA en su cráter.
{i}TintaEnVuelo: ✦ Tu estrella negra YA VIAJA hacia ti — espera su beso con la tierra.
'''

CAIDA_EN = '''{i}TintaYaEspera: ✦ The sky doesn't insist: YOUR GRIMOIRE ALREADY AWAITS in its crater.
{i}TintaEnVuelo: ✦ Your black star is ALREADY FLYING toward you — wait for its kiss with the earth.
'''

# ============================================================
#  LA INSERCIÓN
# ============================================================

def insertar(nombre, item, caida, i, i2, i3, ancla_item, ancla_caida):
    ruta = BASE + nombre
    with io.open(ruta, 'r', encoding='utf-8') as f:
        src = f.read()

    if 'TintaDelCometa:' in src:
        print(f'  {nombre}: TintaDelCometa YA EXISTE (skip)')
        return False

    # 1) EL ÍTEM — justo antes de MedusaNebularStaff (el vecino alfabético
    #    de GrimorioMenorItem en la sección Items).
    assert src.count(ancla_item) == 1, f'{nombre}: ancla item != 1'
    bloque = item.format(i=i, i2=i2, i3=i3)
    src = src.replace(ancla_item, bloque + ancla_item)

    # 2) LAS CLAVES DE LA CAÍDA — justo después de Devorandose (la última
    #    clave del bloque Caida).
    assert src.count(ancla_caida) == 1, f'{nombre}: ancla caida != 1'
    extra = caida.format(i=i)
    src = src.replace(ancla_caida, ancla_caida + extra)

    with io.open(ruta, 'w', encoding='utf-8', newline='\n') as f:
        f.write(src)
    print(f'  {nombre}: +TintaDelCometa +2 claves Caida OK')
    return True


def main():
    print('HJSON v6.50.126:')
    ok = True

    # es-MX (ESPACIOS: 8/16/24)
    ok &= insertar(
        'es-MX_Mods.AethonMod.hjson', ITEM_ES, CAIDA_ES,
        ' ' * 8, ' ' * 16, ' ' * 24,
        '        MedusaNebularStaff: {',
        '        Devorandose: (el libro tiembla… algo lo está devorando — ya no puede hablar)')

    # es-ES (TABS: 1/2/3)
    ok &= insertar(
        'es-ES_Mods.AethonMod.hjson', ITEM_ES, CAIDA_ES,
        '\t', '\t\t', '\t\t\t',
        '\tMedusaNebularStaff: {',
        '\tDevorandose: (el libro tiembla… algo lo está devorando — ya no puede hablar)')

    # en-US (TABS: 1/2/3)
    ok &= insertar(
        'en-US_Mods.AethonMod.hjson', ITEM_EN, CAIDA_EN,
        '\t', '\t\t', '\t\t\t',
        '\tMedusaNebularStaff: {',
        '\tDevorandose: (the book trembles… something is devouring it — it can no longer speak)')

    if not ok:
        sys.exit(1)
    print('TODO OK')


if __name__ == '__main__':
    main()
