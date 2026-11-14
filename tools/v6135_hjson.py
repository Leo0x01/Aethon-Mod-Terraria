#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.135 — EL HJSON ×3: DisplayName + Tooltip de las seis mitológicas
en es-ES, es-MX y en-US (paridad de claves de la casa).
"""
import re

BLOQUES = {
    'es-ES': {
        'AlasSerafinDelVacio': ('Alas del Serafín del Vacío', '''EL SERAFÍN: TRES PARES de alas de fuego violeta que no quema —
			        el par mayor sobre los hombros, el de vuelo en la espalda y el
			        inferior caído. La llama se encorva como el fuego de un trono y
			        el alfabeto del vacío arde en el borde de cada vela.
			        Vuelo 240 · sin daño de caída · se pliega ante alas reales.'''),
        'AlasValquiriaRunica': ('Alas de la Valquiria Rúnica', '''LA VALQUIRIA: SIETE PLUMAS DE LANZA por lado — rectas y filosas,
			        la plata del cielo de batalla sobre el acero y la noche. El
			        futhark solar de la casa graba cada pluma: las alas que eligen
			        a los caídos.
			        Vuelo 240 · sin daño de caída · se pliega ante alas reales.'''),
        'AlasQuetzalEmplumado': ('Alas del Quetzal Emplumado', '''LA SERPIENTE EMPLUMADA: siete plumas de jade por lado que ONDEAN
			        en tres niveles — el par alto, el de vuelo y el inferior. La
			        rampa turquesa de la selva viva y los glifos del vacío en el
			        borde de cada nivel.
			        Vuelo 240 · sin daño de caída · se pliega ante alas reales.'''),
        'AlasFenixDelRenacer': ('Alas del Fénix del Renacer', '''EL FÉNIX: TRES LLAMAS ANIDADAS — núcleo blanco, fuego vivo y
			        brasa hondo, el par alto alzado sobre los hombros. El tono ARDE
			        a lo largo del ala y el alfabeto solar corona cada punta: las
			        alas que renacen de su propia ceniza.
			        Vuelo 240 · sin daño de caída · se pliega ante alas reales.'''),
        'AlasTenguDelCrepusculo': ('Alas del Tengu del Crepúsculo', '''EL CUERVO DE LAS MONTAÑAS: siete plumas primarias de tinta
			        azul-negra, rectas y separadas como el ala del ave que las
			        escribe. Los glifos ámbar del abismo arden entre pluma y pluma
			        — el crepúsculo del demonio lector.
			        Vuelo 240 · sin daño de caída · se pliega ante alas reales.'''),
        'AlasDragonDelVacio': ('Alas del Dragón del Vacío', '''EL DRAGÓN: la membrana de DOS DEDOS por lado — la vela gorda del
			        murciélago ancestral en seis velas de escamas rojo-vino. Los
			        glifos ámbar del abismo recorren el borde de cada vela y las
			        venas laten como corazón de cueva.
			        Vuelo 240 · sin daño de caída · se pliega ante alas reales.'''),
    },
    'es-MX': None,   # se copia de es-ES (la casa usa textos idénticos es-ES/es-MX)
    'en-US': {
        'AlasSerafinDelVacio': ('Void Seraph Wings', '''THE SERAPH: THREE PAIRS of violet-fire wings that do not burn —
			        the greater pair over the shoulders, the flight pair on the
			        back, and the lower folded pair. The flame curls like throne
			        fire, and the void alphabet burns along every sail's edge.
			        Flight 240 · no fall damage · folds before real wings.'''),
        'AlasValquiriaRunica': ('Runic Valkyrie Wings', '''THE VALKYRIE: SEVEN SPEAR-FEATHERS per side — straight and keen,
			        the silver of the battle sky over steel and night. The house's
			        solar futhark is engraved on every feather: the wings that
			        choose the fallen.
			        Flight 240 · no fall damage · folds before real wings.'''),
        'AlasQuetzalEmplumado': ('Feathered Quetzal Wings', '''THE FEATHERED SERPENT: seven jade feathers per side that ripple
			        across three tiers — the high pair, the flight pair, and the
			        lower one. The living jungle's turquoise ramp with void glyphs
			        on every tier's edge.
			        Flight 240 · no fall damage · folds before real wings.'''),
        'AlasFenixDelRenacer': ('Rebirth Phoenix Wings', '''THE PHOENIX: THREE NESTED FLAMES — white core, living fire and
			        deep ember, the high pair raised over the shoulders. The hue
			        BURNS along the wing and the solar alphabet crowns every tip:
			        the wings reborn from their own ash.
			        Flight 240 · no fall damage · folds before real wings.'''),
        'AlasTenguDelCrepusculo': ('Dusk Tengu Wings', '''THE MOUNTAIN CROW: seven primary feathers of blue-black ink,
			        straight and parted like the wing of the bird that writes
			        them. Abyssal amber glyphs burn between feather and feather —
			        the reading demon's dusk.
			        Flight 240 · no fall damage · folds before real wings.'''),
        'AlasDragonDelVacio': ('Void Dragon Wings', '''THE DRAGON: the TWO-FINGER membrane per side — the ancestral
			        bat's fat sail across six wine-red scale veils. Abyssal amber
			        glyphs run along every sail's edge, and the veins beat like a
			        cave heart.
			        Flight 240 · no fall damage · folds before real wings.'''),
    },
}

BASE = '/home/z/my-project/mod/AethonMod/Localization/'
BLOQUES['es-MX'] = BLOQUES['es-ES']

ORDEN = ['AlasSerafinDelVacio', 'AlasValquiriaRunica', 'AlasQuetzalEmplumado',
         'AlasFenixDelRenacer', 'AlasTenguDelCrepusculo', 'AlasDragonDelVacio']

for lang, mapa in BLOQUES.items():
    P = f'{BASE}{lang}_Mods.AethonMod.hjson'
    s = open(P, encoding='utf-8').read()
    # el ancla: el bloque de CorolasDelAlba cierra el set de alas
    m = re.search(r'\tCorolasDelAlba: \{\n\t\tDisplayName: [^\n]+\n\t\tTooltip:\n\t\t\t\'\'\'\n.*?\n\t\t\t\'\'\'\n\t\}', s, re.S)
    assert m, f"ancla CorolasDelAlba no hallada en {lang}"
    bloques = []
    for k in ORDEN:
        nombre, tip = mapa[k]
        bloques.append(f'''
\t{k}: {{
\t\tDisplayName: {nombre}
\t\tTooltip:
\t\t\t\'\'\'
{tip}
\t\t\t\'\'\'
\t}}''')
    ins = m.group(0) + '\n' + '\n'.join(bloques)
    s = s[:m.start()] + ins + s[m.end():]
    open(P, 'w', encoding='utf-8').write(s)
    print(f'{lang}: 6 bloques insertados tras CorolasDelAlba')
print("hjson ×3 listo")
