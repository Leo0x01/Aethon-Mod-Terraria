#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""purga_v6177_en_fosiles.py — LA PURGA DE LOS FÓSILES DEL EN-US (2ª).

v6.177 (la continuación de la purga v6.176): el en-US publicado en la
v6.176 llevaba 181 claves FÓSILES (tooltips de bolsas que el es-ES ya
no pide — texto muerto de una era vieja) + la sesión nueva sumó las
suyas por una doble inserción ya curada. LA POLÍTICA de la v6.176 sigue
viva: «las no reclamadas se purgan del EN». El loop (R20) marca la
regresión y esta purga la devuelve a cero.
"""
import re
import hjson

R = 'AethonMod/Localization/en-US_Mods.AethonMod.hjson'


def aplanar(d, prefijo=''):
    pares = {}
    for k, v in d.items():
        clave = (prefijo + '.' + k) if prefijo else k
        if isinstance(v, dict):
            pares.update(aplanar(v, clave))
        else:
            pares[clave] = v
    return pares


en_texto = open(R, encoding='utf-8').read()
en = aplanar(hjson.loads(en_texto))
es = aplanar(hjson.loads(open('AethonMod/Localization/es-ES_Mods.AethonMod.hjson',
                              encoding='utf-8').read()))
fosiles = sorted(k for k in en if k not in es)
print(f'fósiles EN-sin-ES: {len(fosiles)}')

# la purga TEXTUAL (sin re-dump: los comentarios y el orden viven)
lineas = en_texto.split('\n')
# índice por (nombre, indent) de cada bloque abierto y su línea de
# declaración — para ubicar la línea de cada fósil
salida = []
eliminadas = 0
# apilar: (nombre, indent, indice_en_salida_del_"{")
pila = []
i = 0
# pre-computar: para cada línea con "Clave: {" o "Clave: valor" su clave completa
# (recorrido lineal con la pila; los ''' multilinea se saltan por completo)
contenido = []
prof = 0
claves_linea = {}   # clave_plana -> índice de línea en contenido
multi = False
for idx, ln in enumerate(lineas):
    if multi:
        contenido.append(ln)
        if "'''" in ln:
            multi = False
        continue
    if ln.strip().startswith('//'):
        contenido.append(ln)
        continue
    m = re.match(r'^(\t*)[A-Za-z0-9_]+', ln)
    if m:
        indent = len(m.group(1))
        nombre = ln.strip().split(':')[0].strip()
        # cerrar bloques más profundos
        while pila and pila[-1][1] >= indent:
            pila.pop()
        if ln.strip().endswith('{'):
            pila.append((nombre, indent, idx))
            contenido.append(ln)
        elif "'''" in ln and ': ' in ln and ln.strip().endswith("'''") is False:
            # valor multilinea EMPIEZA (puede ser "Tooltip:" solo o "Tooltip: '''algo")
            ruta = '.'.join([n for n, _, _ in pila] + [nombre])
            claves_linea[ruta] = idx
            contenido.append(ln)
            multi = True
        else:
            ruta = '.'.join([n for n, _, _ in pila] + [nombre])
            claves_linea[ruta] = idx
            contenido.append(ln)
    else:
        contenido.append(ln)

# las líneas a borrar (la del fósil + su ''' de cierre si multilinea)
borrar = set()
for f in fosiles:
    if f in claves_linea:
        idx = claves_linea[f]
        ln = lineas[idx]
        borrar.add(idx)
        if "'''" in ln and ln.strip().count("'''") == 1:
            # multilinea: hasta el siguiente '''
            j = idx + 1
            while j < len(lineas):
                borrar.add(j)
                if "'''" in lineas[j]:
                    break
                j += 1
    else:
        print('  (no ubicado textual — raro):', f)

salida = [ln for i2, ln in enumerate(lineas) if i2 not in borrar]
eliminadas = len(borrar)
open(R, 'w', encoding='utf-8', newline='\n').write('\n'.join(salida))
print(f'purgadas {eliminadas} líneas · {len(fosiles)} claves fósiles')

# verificación: la paridad vuelve a calzar y el archivo sigue parseando
en2 = aplanar(hjson.loads(open(R, encoding='utf-8').read()))
print('en-US ahora:', len(en2), '· EN-sin-ES:', sum(1 for k in en2 if k not in es))
print('ES-sin-EN:', sum(1 for k in es if k not in en2))
