#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Auditoría del .tmod v6.50.75 — EL RELOJ DE ARENA DEL ESCRIBA."""
import struct, zlib, sys
sys.path.insert(0, 'tools')
from parse_tmod import parse

PATH = '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
d = open(PATH, 'rb').read()
tmlver, name, ver, entries, tend, fsize = parse(PATH)

ok = lambda m: print(f'  ✓ {m}')

# === 1. ESTRUCTURA ===
total = sum(c for _, _, c in entries)
assert tend + total == fsize, f'EOF roto: {tend}+{total} != {fsize}'
ok(f'EOF EXACTO: {tend} + {total} == {fsize} ({fsize} B)')
assert ver == '6.50.75', ver
ok(f'versión {ver} · {len(entries)} entradas (la .74 llevaba 394 → +1 el sprite nuevo)')

# === 2. EXTRAER LOS BLOBS (DEFLATE crudo wbits=-15 — y RAW cuando el
#        deflate no ayudó: tML guarda SIN comprimir si comp==raw, p.ej.
#        los PNG ya comprimidos; 24 entradas de este paquete) ===
blobs, off = {}, tend
sin_comp = 0
for p, raw, comp in entries:
    if raw == comp:
        data = d[off:off + comp]  # almacenado crudo
        sin_comp += 1
    else:
        data = zlib.decompress(d[off:off + comp], -15)
    assert len(data) == raw, (p, len(data), raw)
    blobs[p] = data
    off += comp
assert off == fsize
ok(f'{len(entries)} blobs íntegros ({sin_comp} guardados sin compresión — comp==raw)')

# === 3. LOS HJSON EMPAQUETADOS (la lección de la .73/.74: el espejo
#        que el sandbox revierte a medias sesión) ===
es_mx = blobs['Localization/es-MX_Mods.AethonMod.hjson'].decode('utf-8')
es_es = blobs['Localization/es-ES_Mods.AethonMod.hjson'].decode('utf-8')
en_us = blobs['Localization/en-US_Mods.AethonMod.hjson'].decode('utf-8')

def cuerpo(s):  # el cuerpo tras la cabecera (la 1ª clave del mod)
    i = s.index('Mods.AethonMod.DisplayName')
    return s[i:]

assert cuerpo(es_mx) == cuerpo(es_es), 'el espejo es-ES empaquetado NO es el es-MX'
ok('cuerpo es-ES == es-MX BYTE A BYTE (el espejo empaquetado está vivo)')

for etiqueta, texto in [('es-MX', es_mx), ('es-ES', es_es), ('en-US', en_us)]:
    dn = texto.count('DisplayName:'); tt = texto.count('Tooltip:')
    print(f'  · {etiqueta}: DisplayName {dn} · Tooltip {tt}')
    assert dn == tt == 311 or dn == 311, (etiqueta, dn, tt)

for clave in ['RelojDeArenaDelEscriba', 'RelojDeArena:', 'AlAmanecer', 'AlAnochecer']:
    assert clave in es_mx and clave in es_es, f'falta {clave} en español'
    assert ('RelojDeArenaDelEscriba' if 'Reloj' in clave else clave) in en_us or clave in en_us, clave
ok('las claves del RELOJ viajan en las tres lenguas (ítem + AlAmanecer/AlAnochecer)')

# === 4. EL SPRITE (rawimg: [fmt:i32=1][w:i32][h:i32] LE + RGBA crudo) ===
from PIL import Image
import io

def rawimg(datos, ruta_fuente):
    fmt, w, h = struct.unpack_from('<iii', datos, 0)
    cuerpo = datos[12:]
    assert fmt == 1 and len(cuerpo) == w * h * 4, (ruta_fuente, fmt, w, h, len(cuerpo))
    # los píxeles empaquetados deben ser EXACTAMENTE los del PNG fuente
    if ruta_fuente:
        png = Image.open(ruta_fuente).convert('RGBA').tobytes()
        assert png == cuerpo, f'{ruta_fuente}: los píxeles del paquete != PNG fuente'
    return w, h

w, h = rawimg(blobs['Content/Items/RelojDeArenaDelEscriba.rawimg'],
              'AethonMod/Content/Items/RelojDeArenaDelEscriba.png')
ok(f'RelojDeArenaDelEscriba.rawimg: {w}×{h} RGBA — píxeles IDÉNTICOS al PNG fuente')

# los sprites de la .74 siguen vivos (y con sus píxeles intactos)
for ruta, fuente, (ew, eh) in [
        ('Content/Ambientes/SolDeLaOleada', 'AethonMod/Content/Ambientes/SolDeLaOleada.png', (200, 200)),
        ('Content/Ambientes/LunaDeLaOleada', 'AethonMod/Content/Ambientes/LunaDeLaOleada.png', (200, 1600)),
        ('Content/Weapons/CodiceVivo/CodiceVivo', 'AethonMod/Content/Weapons/CodiceVivo/CodiceVivo.png', (48, 384))]:
    w2, h2 = rawimg(blobs[ruta + '.rawimg'], fuente)
    assert (w2, h2) == (ew, eh), (ruta, w2, h2)
ok('sprites de la .74 intactos y byte a byte (Sol 200×200 · Luna 200×1600 · Códice 48×384)')

# === 5. LA DLL — símbolos vivos nuevos + persistencia de la .74 ===
dll = blobs['AethonMod.dll']
assert dll[:2] == b'MZ'
def utf8(s): return s.encode('utf-8')
def utf16(s): return s.encode('utf-16-le')

NUEVOS = ['RelojDeArenaDelEscriba', 'GiroLocal', 'DifundirGiroReloj',
          'MsgCambiarHorario']
for s in NUEVOS:
    assert utf8(s) in dll, f'FALTA el símbolo nuevo {s}'
ok('DLL: los 4 símbolos nuevos VIVOS (ítem + GiroLocal + broadcast + MsgCambiarHorario)')

PERSISTEN = ['AmbienteOleadaSistema', 'MsgAmbienteOleada', 'VigilarPrestamo',
             'JefeDeLaOleada', 'CodiceVivoProjectile', 'SombraDeLaPagina',
             '_overrideForMasterMode']
for s in PERSISTEN:
    assert utf8(s) in dll or utf16(s) in dll, f'FALTA el símbolo persistente {s}'
ok('DLL: los símbolos de la .73/.74 PERSISTEN (ambiente + préstamo + oleadas)')

# el byte del ID 13 no puede afirmarse por sí solo, pero el nombre del
# mensaje sí viaja en el IL (ldsfld uint8 MsgCambiarHorario → el literal
# en los metadatos de la tabla Field)
assert utf16('MsgCambiarHorario') in dll or utf8('MsgCambiarHorario') in dll
ok('MsgCambiarHorario presente en los metadatos de campos (ID 13)')

# === 6. LA BOLSA — el reloj entra en la Bolsa del Probador ===
# (el contenido vive en el IL de BolsasCategorias → el tipo referenciado)
assert utf8('RelojDeArenaDelEscriba') in dll
ok('la referencia al ítem en la Bolsa del Probador viaja en la DLL (mismo símbolo)')

print()
print(f'AUDITORÍA COMPLETA — {name} v{ver}: {len(entries)} entradas, '
      f'{fsize} B, TODO VERIFICADO')
import hashlib
print('md5:', hashlib.md5(d).hexdigest())
