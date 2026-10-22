#!/usr/bin/env python3
# v6.51.4 — en-US: insertar los 7 DisplayNames tras el bloque BolsaCategoria
# (replace literal — la regex fallaba por los {0} dentro del bloque).
ruta = 'AethonMod/Localization/en-US_Mods.AethonMod.hjson'
with open(ruta, encoding='utf-8') as f:
    t = f.read()
if 'DignosEquinoccioBinario' in t:
    print('YA ESTABA'); raise SystemExit
ANCLAJE = ('\tBolsaCategoria: {\n'
           '\t\tTitulo: "[c/{0}:{1}]"\n'
           '\t\tLinea1: "[c/E6B0FF:Right-click to deploy this category of the arsenal]"\n'
           '\t\tLinea2: "[c/FFD66B:Only delivers what you\'re missing \xe2\x80\x94 reopen it whenever you lose a weapon]"\n'.encode('latin-1').decode('utf-8') +
           '\t\tNota: "[c/78788C:{0}]"\n'
           '\t\tAbierta: The {0} opens: {1} new items.\n'
           '\t\tCompleta: The {0} is already complete with you.\n'
           '\t}\n')
if ANCLAJE not in t:
    # fallback: anclar tras la línea Completa
    i = t.find('\t\tCompleta: The {0} is already complete with you.\n')
    if i < 0: raise SystemExit('ANCLAJE NO HALLADO')
    fin = t.find('\n', i + len('\t\tCompleta: The {0} is already complete with you.\n'))  # fin de línea
    punto = i + len('\t\tCompleta: The {0} is already complete with you.\n') + 1  # tras el \n
    # avanzar hasta la llave de cierre del bloque
    punto = t.find('}', punto) + 2  # } + \n
else:
    punto = t.find(ANCLAJE) + len(ANCLAJE)
NUEVO = ('\n\t// v6.51.4 \xe2\x80\x94 THE SIX WORTHY CANDIDATES of the Grimoire (one weapon\n'.encode('latin-1').decode('utf-8') +
         '\t// per idea: the binary, the commas, the page, the eye, the line and\n'
         '\t// the bookmark) + THE BAG that delivers them.\n'
         '\tDignosEquinoccioBinario: {\n\t\tDisplayName: The Binary Equinox\n\t}\n'
         '\tDignosComasQueMuerden: {\n\t\tDisplayName: The Biting Commas\n\t}\n'
         '\tDignosPaginaArrancada: {\n\t\tDisplayName: The Torn Page\n\t}\n'
         '\tDignosOjoQueCaza: {\n\t\tDisplayName: The Hunting Eye\n\t}\n'
         '\tDignosLineaDeLectura: {\n\t\tDisplayName: The Reading Line\n\t}\n'
         '\tDignosMarcapaginas: {\n\t\tDisplayName: The Bookmark\n\t}\n'
         '\tBolsaProyectilesDignos: {\n\t\tDisplayName: The Bag of Worthy Projectiles\n\t}\n')
t = t[:punto] + NUEVO + t[punto:]
with open(ruta, 'w', encoding='utf-8', newline='\n') as f:
    f.write(t)
print('OK en-US — claves añadidas en', punto)
