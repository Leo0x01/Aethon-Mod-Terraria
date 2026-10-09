#!/usr/bin/env python3
"""v6.50.82 — La cura del espejo es-ES (incidente #8): el cuerpo de es-ES se
regenera BYTE-IDÉNTICO al cuerpo de es-MX (el invariante .77). La cabecera
del espejo (ESPEJO GENERADO — NO EDITAR) se conserva."""
import io

MX = 'AethonMod/Localization/es-MX_Mods.AethonMod.hjson'
ES = 'AethonMod/Localization/es-ES_Mods.AethonMod.hjson'

mx = io.open(MX, encoding='utf-8', newline='').read()
es = io.open(ES, encoding='utf-8', newline='').read()


def cuerpo(t):
    lineas = t.split('\n')
    i = 0
    while i < len(lineas) and (lineas[i].startswith('//') or not lineas[i].strip()):
        i += 1
    return '\n'.join(lineas[i:]), i


cuerpo_mx, _ = cuerpo(mx)
cuerpo_es, _ = cuerpo(es)
if cuerpo_es == cuerpo_mx:
    print('espejo YA correcto — nada que hacer')
    raise SystemExit(0)

# ¿el cuerpo viejo del es-ES traía los inserts de la .82?
print('es-ES viejo:', 'con GrimorioHambriento' if 'GrimorioHambriento' in es else 'SIN GrimorioHambriento (revert total)')
print('es-ES viejo:', 'con tabs' if '\t' in cuerpo_es[:2000] else 'sin tabs')

# El nuevo es-ES: la MISMA cabecera de espejo + el cuerpo de es-MX tal cual
lineas_es = es.split('\n')
i = 0
while i < len(lineas_es) and (lineas_es[i].startswith('//') or not lineas_es[i].strip()):
    i += 1
cabecera = '\n'.join(lineas_es[:i])
nuevo = cabecera + '\n' + cuerpo_mx
io.open(ES, 'w', encoding='utf-8', newline='').write(nuevo)
print(f'espejo REGENERADO: cabecera {len(cabecera)} B + cuerpo es-MX {len(cuerpo_mx)} B')
assert 'GrimorioHambriento' in nuevo and 'Hambre {' in nuevo
print('verificado: lleva GrimorioHambriento + sección Hambre')
