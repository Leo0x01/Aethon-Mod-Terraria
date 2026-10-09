#!/usr/bin/env python3
"""v6.50.83 — La cura del espejo es-ES (incidente #9): el COMMIT de la .82
(3eba7a5) capturó la variante REVERTIDA (tabs, sin cabecera, 81.359 B) — el
sandbox golpeó entre el build final auditado y el git add; el .tmod .82
publicado sí lleva el espejo correcto (111.265 B) y la .83 construyó el
fuente malo. La cura: cabecera canónica EXPLÍCITA (nunca heredada del
archivo actual, que puede no tenerla) + cuerpo byte-idéntico al de es-MX."""
import io

MX = 'AethonMod/Localization/es-MX_Mods.AethonMod.hjson'
ES = 'AethonMod/Localization/es-ES_Mods.AethonMod.hjson'

CABECERA = (
    '// ESPEJO GENERADO de es-MX_Mods.AethonMod.hjson \u2014 NO EDITAR A MANO.\n'
    '// v6.50.71 \u2014 todas las variantes de espa\u00f1ol leen este espejo del cuerpo\n'
    '// de es-MX (invariante .77: cuerpo byte-id\u00e9ntico; regenerado por\n'
    '// tools/sync_es_es_v65083.py \u2014 incidente #9 del espejo, v6.50.83).\n'
    '\n'
)

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
if cuerpo_es == cuerpo_mx and es.startswith(CABECERA.split('\n')[0]):
    print('espejo YA correcto — nada que hacer')
    raise SystemExit(0)

print('es-ES viejo:', 'con GrimorioHambriento' if 'GrimorioHambriento' in es else 'SIN GrimorioHambriento (revert total)')
print('es-ES viejo:', 'con tabs' if '\t' in cuerpo_es[:2000] else 'sin tabs')
print('es-ES viejo:', 'con cabecera de espejo' if es.startswith('// ESPEJO') else 'SIN cabecera (revert al pre-espejo)')

nuevo = CABECERA + cuerpo_mx
io.open(ES, 'w', encoding='utf-8', newline='').write(nuevo)
print(f'espejo REGENERADO: cabecera canonica {len(CABECERA)} B + cuerpo es-MX {len(cuerpo_mx)} B')
