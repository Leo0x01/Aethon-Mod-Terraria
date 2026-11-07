#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6111_tmod.py — LA AUDITORÍA DEL .tmod v6.111 — EL HAZ MUERTO +
LAS CINCO RENACIDAS + EL ALTAR DEL OJO + LA BRUMA QUE HUYE.

LA LETRA: «a la forma ascendida 1 quitale el haz de luz que tiene /
la forma ascendida 6 tiene texturas que son cuadrados planos, debes
arreglar eso y ademas crear la regenera y vuelve a crear la forma
ascendida 5, 7, 8, 9, 10 / tambien debes cambiar las texturas del
altar, regenera la textura para crear un altar digno de una entidad
cosmica, dale nueva tematica al altar que sea diferente / ademas la
bruma de el aura de vacio sobre los enemigos tambien debe alejarse
del jugador».

Verifica: versión · blobs íntegros/EOF · firmas VIVAS/MUERTAS en la
DLL · hjson ×3 (espejo es-MX + frases nuevas + frases muertas fuera)
· los 6 iconos rawimg EXACTOS · EL ALTAR DEL OJO (3 archivos) ·
0 CJK / 0 IP en los .cs.
"""
import hashlib
import importlib.util
import os
import re
import struct
import zlib
import sys

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
RAIZ = os.path.join(REPO, 'AethonMod')
TMOD = os.path.expanduser('~/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod')

spec = importlib.util.spec_from_file_location('parse_tmod', os.path.join(REPO, 'tools', 'parse_tmod.py'))
mp = importlib.util.module_from_spec(spec); spec.loader.exec_module(mp)


def blobs(d, tend, entries):
    out, off, malas = {}, tend, 0
    for p, raw, comp in entries:
        trozo = d[off:off + comp]
        if len(trozo) != comp: malas += 1; break
        try: data = trozo if comp == raw else zlib.decompress(trozo, -15)
        except zlib.error: malas += 1; break
        if len(data) != raw: malas += 1; break
        out[p] = data; off += comp
    return out, off, malas


d = open(TMOD, 'rb').read()
tmlver, name, ver, entries, tend, fsize = mp.parse(TMOD)
print(f'(0) {name} v{ver} · tML {tmlver} · {len(entries)} entradas · {fsize:,} B')
print(f'    md5 {hashlib.md5(d).hexdigest()}')
assert ver == '6.111', f'¡VERSIÓN {ver} ≠ 6.111!'
todos, off, malas = blobs(d, tend, entries)
print(f'    blobs: {len(todos)}/{len(entries)} íntegros, malas={malas}, '
      f'EOF={off} == {fsize}: {"OK" if off == fsize else "¡ROMPO!"}')
assert malas == 0 and off == fsize

dll = todos['AethonMod.dll']
assert dll, 'sin DLL'

def vive(s):
    return s.encode() in dll or s.encode('utf-16-le') in dll

# ===================== (1) LAS FIRMAS =====================
# (las DLL solo conservan MÉTODOS/tipos/literales — los locales viven
#  en la FUENTE: la auditoría mira los DOS reinos, cada firma en el suyo)
fuente_auralib = open(os.path.join(RAIZ, 'Content', 'VFX', 'AuraLib.cs'), encoding='utf-8').read()
fuente_ancla = open(os.path.join(RAIZ, 'Content', 'Projectiles', 'AuraJuicio2Ancla.cs'), encoding='utf-8').read()

vivas_dll = [
    'EmitirDivino5', 'EmitirDivino6', 'EmitirDivino7',
    'EmitirDivino8', 'EmitirDivino9', 'EmitirDivino10',
    'DibujarDivino5', 'DibujarDivino10',
    'UnAro',            # 8 — la llanta común de las ruedas
    'UnAroVertical',     # 8 — la rueda de pie que precesa
    'UnOjo',             # 8 — el ojo de la rueda
]
muertas_dll = []   # (los locales muertos jamás estuvieron en la DLL: se
                    #  comprueban en la FUENTE — abajo)
for s in vivas_dll:
    assert vive(s), f'falta la firma VIVA en la DLL: {s}'
for s in muertas_dll:
    assert not vive(s), f'la firma MUERTA sigue viva en la DLL: {s}'

vivas_fuente = [
    'apriete',           # 5 — la bandada que se aprieta al atacar
    'achat',             # 7 — el disco inclinado de la galaxia
    'posLamparas',       # 9 — el coro de las siete
    'angOrilla',         # 10 — el trueno del cenit a la orilla
    'LA FUGA DE LA BRUMA',            # la bruma que huye (la lápida viva)
    'velocity: rumbo * (30f + 42f * f0)',   # el smear estirado por la fuga
    'EL ALIENTO TAMBIÉN HUYE',       # el aliento con su edad cíclica
]
muertas_fuente = [
    '640f + 110f',       # LA COLUMNA DE LA FORMA 1 (su altoCol único —
                         #  la forma 2 conserva SU columna: 760f+130f VIVE)
    'LA PUERTA: el charco de luz donde la columna ABRAZA',  # el charco de la 1
]
for s in vivas_fuente:
    assert (s in fuente_auralib) or (s in fuente_ancla), f'falta en la fuente: {s}'
for s in muertas_fuente:
    assert s not in fuente_auralib, f'la MUERTA sigue en la fuente: {s}'
# LA LÁPIDA del haz (el comentario que cuenta la muerte):
assert 'LA COLUMNA DEL CIELO + LOS RAYOS DIVINOS — MUERTOS' in fuente_auralib
print(f'(1) firmas: {len(vivas_dll)} VIVAS en la DLL + {len(vivas_fuente)} VIVAS en la fuente '
      f'+ {len(muertas_fuente)} MUERTAS ausentes + la lápida del haz ✔')

# ===================== (2) EL HJSON ×3 =====================
def cuerpo(ruta):
    """El cuerpo SIN su bloque de cabecera (// líneas + blancos) — la
    MISMA definición del sync (la cabecera del espejo es DISTINTA por
    diseño: la CABECERA_ESPEJO del sync)."""
    lineas = open(ruta, encoding='utf-8').read().split('\n')
    i = 0
    while i < len(lineas) and (lineas[i].startswith('//') or lineas[i].strip() == ''):
        i += 1
    assert i < len(lineas), f'{ruta}: archivo sin cuerpo'
    return [l for l in lineas[i:] if l.strip()]


es = os.path.join(RAIZ, 'Localization', 'es-ES_Mods.AethonMod.hjson')
mx = os.path.join(RAIZ, 'Localization', 'es-MX_Mods.AethonMod.hjson')
en = os.path.join(RAIZ, 'Localization', 'en-US_Mods.AethonMod.hjson')
assert cuerpo(es) == cuerpo(mx), 'es-MX ≠ es-ES (correr el sync)'
print('(2) hjson ×3: es-MX espejo EXACTO del es-ES ✔ · en-US respetado ✔')

es_txt = open(es, encoding='utf-8').read()
en_txt = open(en, encoding='utf-8').read()
nuevas_es = [
    'Sin alas y sin haz de luz',                 # la forma 1 v6.111
    'EL RÍO DE PLUMAS', 'LA BANDADA DE LUZ',     # la 5 renacida
    'EL OJO DE LA GALAXIA',                      # la 7 renacida
    'LAS OFANIM', 'RUEDAS DENTRO DE RUEDAS',     # la 8 renacida
    'EL CÁNTICO DE LAS LÁMPARAS',                # la 9 renacida
    'EL TRONO', 'MAR DE CRISTAL',                # la 10 renacida
    'LA MANDORLA',                               # la 10, la firma
]
muertas_es = [
    'EL VIENTO DEL ESPÍRITU',        # la 5 muerta
    'LA ESTRELLA VIVA',              # la 7 muerta
    'LA ESPADA DEL JUICIO',          # la 8 muerta
    'LA MÚSICA DE LAS ESFERAS',      # la 9 muerta
    'EL ÁRBOL DE LA VIDA',           # la 10 muerta
    'La columna del cielo abierta',  # el haz muerto de la 1
]
for s in nuevas_es:
    assert s in es_txt, f'falta la frase NUEVA en es-ES: {s}'
for s in muertas_es:
    assert s not in es_txt, f'la frase MUERTA sigue en es-ES: {s}'
    assert s not in en_txt, f'la frase MUERTA sigue en en-US: {s}'
print(f'(3) las {len(nuevas_es)} frases nuevas VIVAS · las {len(muertas_es)} muertas AUSENTES (es-ES + en-US) ✔')

# ===================== (4) LOS ICONOS EXACTOS =====================
def ppx(x, y):
    return struct.pack('<II', x, y)

def dims(b):
    raw = zlib.decompress(b, -15) if b[:2] != b'\x89P' else b
    return struct.unpack('<II', raw[:8]) if len(raw) >= 8 else (0, 0)

iconos = ['FormaAscendidaCincoItem', 'FormaAscendidaSeisItem',
          'FormaAscendidaSieteItem', 'FormaAscendidaOchoItem',
          'FormaAscendidaNueveItem', 'FormaAscendidaDiezItem']
from PIL import Image
import io
for ic in iconos:
    entrada = f'Content/Items/Cosmetics/{ic}.rawimg'
    assert entrada in todos, f'falta el icono: {entrada}'
    raw = todos[entrada]
    w, h = struct.unpack('<ii', raw[4:12])
    assert (w, h) == (30, 30), f'{ic}: {w}x{h} ≠ 30x30'
    paquete = raw[12:]
    fuente = Image.open(os.path.join(RAIZ, 'Content', 'Items', 'Cosmetics', ic + '.png')).convert('RGBA').tobytes()
    assert paquete == fuente, f'{ic}: el rawimg ≠ el PNG fuente'
print(f'(4) los {len(iconos)} iconos 30×30 EXACTOS (rawimg == fuente) ✔')

# ===================== (5) EL ALTAR DEL OJO =====================
archivos = [
    ('Content/Tiles/AltarOfrendasTile.png', (34, 300)),
    ('Content/Tiles/AltarOfrendasTile_Highlight.png', (34, 50)),
    ('Content/Items/Altar/AltarOfrendasItem.png', (34, 50)),
]
for ruta, (w, h) in archivos:
    im = Image.open(os.path.join(RAIZ, ruta.replace('Content/', 'Content/')))
    assert im.size == (w, h), f'{ruta}: {im.size} ≠ {w}x{h}'
    disco = os.path.join(RAIZ, ruta)
    assert os.path.getmtime(disco) > os.path.getmtime(os.path.join(REPO, 'tools', 'altar_v6110.py')), \
        f'{ruta}: más viejo que la v6.110 — ¿se regeneró con la v6.111?'
assert os.path.exists(os.path.join(REPO, 'tools', 'altar_v6111.py')), 'falta el generador v6111'
print('(5) EL ALTAR DEL OJO: tile 34×300 + highlight 34×50 + ítem 34×50, REGENERADO ✔')

# ===================== (6) 0 CJK / 0 IP (la ley de la casa) =====================
# («IP» = PROPIEDAD INTELECTUAL: nombres con copyright que NUNCA — la
#  lista de la casa; NO direcciones de red: las versiones «23.10.0.0»
#  de los comentarios son versiones, no IPs)
ips_cs = re.compile(r're:?zero|ousagi|daphne|subaru|roswaal', re.I)
pat_cjk = re.compile('[\u4e00-\u9fff\u3040-\u30ff\uac00-\ud7af]')
n_cs = 0
malos = []
for root, _, fs in os.walk(RAIZ):
    for f in fs:
        if f.endswith(('.cs', '.hjson')):
            n_cs += 1
            txt = open(os.path.join(root, f), encoding='utf-8', errors='replace').read()
            if ips_cs.search(txt) or pat_cjk.search(txt):
                malos.append(f)
assert not malos, malos
print(f'(6) 0 IP (propiedad intelectual) y 0 CJK en {n_cs} .cs+hjson ✔')

print()
print(f'AUDIT v6.111 TODO VERIFICADO — {len(todos)} entradas, EOF exacto {fsize:,} B')
print(f'md5 {hashlib.md5(d).hexdigest()}')
