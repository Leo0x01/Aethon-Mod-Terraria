#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6112_tmod.py — LA AUDITORÍA DEL .tmod v6.112 — LAS CINCO LUCES
MUERTAS + LAS RUNAS DE LAS OFANIM + EL ARO PLANTADO + LA BRUMA DISUELTA
EN PARTÍCULAS.

LA LETRA: «la forma 1 y 8 se quedan / las otras formas 5, 6, 7, 9, 10
pueden ser borradas / en la forma 8 hay algunas texturas pequeñas que
son solo rectangulos, arregla eso convirtiendo esos rectangulos pequeños
en runas / en la forma 8 hay un circulo vertical que gira alrededor del
jugador, ese circulo eliminalo o has que no gire / en el aura del vacio
la bruma es muy larga, mejor quitemosla de esa forma y dejemos solo
pequeñas particulas».

Verifica: versión · blobs íntegros/EOF · firmas VIVAS/MUERTAS en la DLL
· las lápidas y las cirugías en la fuente · hjson ×3 (espejo es-MX +
frases nuevas + frases muertas fuera) · el icono de la 8 EXACTO y los
CINCO íconos muertos AUSENTES · 0 CJK / 0 IP en los .cs.
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
assert ver == '6.112', f'¡VERSIÓN {ver} ≠ 6.112!'
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
fuente_cosmeticos = open(os.path.join(RAIZ, 'Content', 'Players', 'CosmeticPlayer.cs'), encoding='utf-8').read()

vivas_dll = [
    'EmitirDivino8', 'DibujarDivino8', 'FormaAscendidaOcho',
    'UnAro',            # 8 — la llanta común de las ruedas
    'UnAroVertical',    # 8 — la rueda de pie (PLANTADA v6.112)
    'UnOjo',            # 8 — el ojo de la rueda
    'UnaRuna',          # 8 — LA RUNA DE LA RUEDA (v6.112 — la cura del rectángulo)
    'RunasOfanim',      # 8 — EL ALFABETO DE LAS OFANIM (6 glifos de trazos)
]
muertas_dll = [
    'EmitirDivino5', 'EmitirDivino6', 'EmitirDivino7',
    'EmitirDivino9', 'EmitirDivino10',
    'DibujarDivino5', 'DibujarDivino6', 'DibujarDivino7',
    'DibujarDivino9', 'DibujarDivino10',
    'FormaAscendidaCincoItem', 'FormaAscendidaSeisItem',
    'FormaAscendidaSieteItem', 'FormaAscendidaNueveItem',
    'FormaAscendidaDiezItem',
]
for s in vivas_dll:
    assert vive(s), f'falta la firma VIVA en la DLL: {s}'
for s in muertas_dll:
    assert not vive(s), f'la firma MUERTA sigue viva en la DLL: {s}'

vivas_fuente = [
    'AzimutFijo',                        # 8 — el plano plantado (const viva)
    'EL PLANO PLANTADO',                 # 8 — la lápida de la precesión
    'LAS RUNAS DE LA FRICCIÓN',          # 8 — la cura del rectángulo
    'EL ALFABETO DE LAS OFANIM',         # 8 — los 6 patrones del futhark
    'LA BRUMA MURIÓ DISUELTA EN PARTÍCULAS',   # el aura del vacío (la lápida)
    'EL ALIENTO VIOLETA MURIÓ',          # los 3 velos cayeron con el manto
    'EL VIAJE CORTO',                    # la mota muere a medio cuerpo
    'LAS LUCES 5, 6, 7, 9 Y 10 MURIERON',      # la lápida en CosmeticPlayer
    'LOS PATRONES DIVINOS 5, 6, 7, 9 Y 10 MURIERON',  # la lápida del enum
]
muertas_fuente_auralib = [
    'float azimut = t * 0.10f',          # la precesión del aro vertical
    'new Vector2(16f, 26f), ang, ruido', # EL RECTÁNGULO de la lengüeta
    'apriete',                           # 5 — la bandada muerta
    'posLamparas',                       # 9 — el coro muerto
    'angOrilla',                         # 10 — la orilla muerta
]
muertas_fuente_ancla = [
    'LA FUGA DE LA BRUMA',               # la estela larga de la v6.111
    'EL ALIENTO TAMBIÉN HUYE',           # el velo con edad cíclica
    'velocity: rumbo * (30f + 42f * f0)',        # el smear ×2.4 del manto
    'RadioDomo(n, k) * 2.2f',            # el viaje de 2.2-3.4 cúpulas
    'RadioDomo(n, k) * 2.1f',            # la deriva del aliento
]
for s in vivas_fuente:
    assert (s in fuente_auralib) or (s in fuente_ancla) or (s in fuente_cosmeticos), \
        f'falta en la fuente: {s}'
for s in muertas_fuente_auralib:
    assert s not in fuente_auralib, f'la MUERTA sigue en AuraLib: {s}'
for s in muertas_fuente_ancla:
    assert s not in fuente_ancla, f'la MUERTA sigue en el ancla: {s}'
print(f'(1) firmas: {len(vivas_dll)} VIVAS en la DLL · {len(muertas_dll)} MUERTAS ausentes '
      f'· {len(vivas_fuente)} lápidas/cirugías VIVAS · {len(muertas_fuente_auralib) + len(muertas_fuente_ancla)} MUERTAS de fuente ausentes ✔')

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
    'PLANTADA en su sitio',                 # la 8 sin precesión (v6.112)
    'de la fricción ARDEN sobre los aros',  # la 8 con sus runas (v6.112)
    'como sus cuatro hermanas',             # la 8: quedan 5 formas
]
muertas_es = [
    'La Forma Ascendida 5', 'La Forma Ascendida 6', 'La Forma Ascendida 7',
    'La Forma Ascendida 9', 'La Forma Ascendida 10',
    'EL RÍO DE PLUMAS', 'EL ESCRIBA DE LA LUZ',
    'EL OJO DE LA GALAXIA', 'EL CÁNTICO DE LAS LÁMPARAS',
    'como sus siete hermanas',              # la 8 contaba 7 (la 9 y 10 vivían)
]
muertas_en = [
    'The Ascended Form 5', 'The Ascended Form 6', 'The Ascended Form 7',
    'The Ascended Form 9', 'The Ascended Form 10',
    'THE RIVER OF FEATHERS', 'THE SCRIBE OF LIGHT',
    'THE EYE OF THE GALAXY', 'THE CHANT OF THE LAMPS',
    'like its seven sisters',
]
for s in nuevas_es:
    assert s in es_txt, f'falta la frase NUEVA en es-ES: {s}'
for s in muertas_es:
    assert s not in es_txt, f'la frase MUERTA sigue en es-ES: {s}'
for s in muertas_en:
    assert s not in en_txt, f'la frase MUERTA sigue en en-US: {s}'
print(f'(3) las {len(nuevas_es)} frases nuevas VIVAS · las {len(muertas_es) + len(muertas_en)} muertas AUSENTES (es-ES + en-US) ✔')

# ===================== (4) LOS ICONOS: LA 8 VIVA, LAS 5 MUERTAS =====================
from PIL import Image
import io

ic = 'FormaAscendidaOchoItem'
entrada = f'Content/Items/Cosmetics/{ic}.rawimg'
assert entrada in todos, f'falta el icono: {entrada}'
raw = todos[entrada]
w, h = struct.unpack('<ii', raw[4:12])
assert (w, h) == (30, 30), f'{ic}: {w}x{h} ≠ 30x30'
paquete = raw[12:]
fuente_png = Image.open(os.path.join(RAIZ, 'Content', 'Items', 'Cosmetics', ic + '.png')).convert('RGBA').tobytes()
assert paquete == fuente_png, f'{ic}: el rawimg ≠ el PNG fuente'

muertos = ['FormaAscendidaCincoItem', 'FormaAscendidaSeisItem',
           'FormaAscendidaSieteItem', 'FormaAscendidaNueveItem',
           'FormaAscendidaDiezItem']
for m in muertos:
    assert f'Content/Items/Cosmetics/{m}.rawimg' not in todos, \
        f'el icono MUERTO sigue en el .tmod: {m}'
    assert not os.path.exists(os.path.join(RAIZ, 'Content', 'Items', 'Cosmetics', m + '.png')), \
        f'el PNG MUERTO sigue en disco: {m}'
print(f'(4) el icono de la 8 30×30 EXACTO (rawimg == fuente) · los {len(muertos)} iconos muertos AUSENTES ✔')

# ===================== (5) 0 CJK / 0 IP (la ley de la casa) =====================
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
print(f'(5) 0 IP (propiedad intelectual) y 0 CJK en {n_cs} .cs+hjson ✔')

print()
print(f'AUDIT v6.112 TODO VERIFICADO — {len(todos)} entradas, EOF exacto {fsize:,} B')
print(f'md5 {hashlib.md5(d).hexdigest()}')
