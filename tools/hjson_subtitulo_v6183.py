#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.183 — LA CIRUGÍA DEL SUBTÍTULO DEL PERGAMINO (la letra: «hay
texto en crudo... coloca lo que debe ir en esos textos»).

La página informativa del libro dibuja Titulo + Subtitulo — la clave
Subtitulo NUNCA existió (el código hasta se defendía escondiéndola:
`!sub.StartsWith("Mods.")`). Esta cirugía le da vida en los 3 idiomas,
justo debajo de Titulo dentro del bloque Pergamino.

Cirugía con asserts de la casa: si el patrón no está, MUERE.
"""
import io

RAIZ = '/home/z/my-project/mod/AethonMod/Localization'

INSERTOS = {
    'es-MX_Mods.AethonMod.hjson':
        '\tTitulo: Páginas informativas del grimorio\n'
        '\t// v6.183 — EL SUBTÍTULO COSIDO (la letra: el texto en crudo '
        'de la página del libro): el rótulo que viaja junto al título.\n'
        '\tSubtitulo: cosido al lomo por el Cronista\n',
    'es-ES_Mods.AethonMod.hjson':
        '\tTitulo: Páginas informativas del grimorio\n'
        '\t// v6.183 — EL SUBTÍTULO COSIDO (la letra: el texto en crudo '
        'de la página del libro): el rótulo que viaja junto al título.\n'
        '\tSubtitulo: cosido al lomo por el Cronista\n',
    'en-US_Mods.AethonMod.hjson':
        '\tTitulo: Informational pages of the grimoire\n'
        '\t// v6.183 — THE STITCHED SUBTITLE (the letter: the raw text '
        'on the book page): the tagline that rides beside the title.\n'
        '\tSubtitulo: stitched into the spine by the Chronicler\n',
}

for archivo, bloque in INSERTOS.items():
    ruta = f'{RAIZ}/{archivo}'
    with io.open(ruta, encoding='utf-8') as f:
        s = f.read()
    # ── asserts: la cirugía solo toca lo que reconoce ────────────
    primera = bloque.split('\n')[0]
    assert '\tSubtitulo:' not in s.split('Pergamino: {', 1)[1][:800], \
        f'{archivo}: Subtitulo ya vive en Pergamino — nada que hacer'
    assert primera in s, f'{archivo}: falta el ancla {primera!r}'
    assert 'Pergamino: {' in s, f'{archivo}: falta el bloque Pergamino'
    # la primera línea de bloque REEMPLAZA al ancla (quedan iguales) y
    # añade el resto debajo — el patrón de la casa
    s = s.replace(primera, bloque, 1)
    with io.open(ruta, 'w', encoding='utf-8') as f:
        f.write(s)
    print(f'  ✔ {archivo}: Subtitulo nace en Pergamino')

print('CIRUGÍA DEL SUBTÍTULO: 3/3 idiomas vivos')
