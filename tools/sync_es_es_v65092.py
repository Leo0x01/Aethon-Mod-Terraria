#!/usr/bin/env python3
"""v6.50.92 — regeneracion del espejo es-ES: cabecera canonica EXPLICITA +
cuerpo byte-identico al de es-MX (el invariante de la casa desde la .71;
regenerado en cada version que toca es-MX — esta vez: la familia se encoge
a DOS libros y los tooltips del hambre con la gracia de los 10 s)."""
import io

MX = 'AethonMod/Localization/es-MX_Mods.AethonMod.hjson'
ES = 'AethonMod/Localization/es-ES_Mods.AethonMod.hjson'

CABECERA = (
    '// ESPEJO GENERADO de es-MX_Mods.AethonMod.hjson \u2014 NO EDITAR A MANO.\n'
    '// v6.50.71 \u2014 todas las variantes de espa\u00f1ol leen este espejo del cuerpo\n'
    '// de es-MX (invariante .77: cuerpo byte-id\u00e9ntico; regenerado por\n'
    '// tools/sync_es_es_v65092.py \u2014 familia de DOS libros, v6.50.92).\n'
    '\n'
)

def cuerpo(t):
    lineas = t.split('\n')
    i = 0
    while i < len(lineas) and (lineas[i].startswith('//') or not lineas[i].strip()):
        i += 1
    return '\n'.join(lineas[i:])

mx = io.open(MX, encoding='utf-8', newline='').read()
cuerpo_mx = cuerpo(mx)
nuevo = CABECERA + cuerpo_mx
io.open(ES, 'w', encoding='utf-8', newline='').write(nuevo)

es = io.open(ES, encoding='utf-8', newline='').read()
assert cuerpo(es) == cuerpo_mx, 'el cuerpo del espejo NO coincide con es-MX'
print('es-ES regenerado:', len(es), 'bytes — cuerpo == es-MX byte a byte')
