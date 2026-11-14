#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6133_tmod.py — LA AUDITORÍA DEL .tmod v6.133 — LA COPIA EXACTA
DEL ECLIPSE · LA PURGA DEL STABBER.

LA LETRA (sesión 14):
  (a) «buenos, las alas se ven horrible por algun motivo no me gusta
      su forma, intenta copiar el codigo exacto de las alas runicas
      del eclipse y luego aplica la correccion para convertirlas en
      unas nuevas alas runicas de vacio»
  (b) «la animacion de devorar ya esta bien, como el jugador no se
      mueve, no hay errores, asi que eso es algo que esta listo»
  (c) «con el comportamiento ya creado de las cuchillas, puedes
      borrar Dazzling Stabber Staff de Calamity Mod del codigo y
      cualquier referencia»

LO QUE ESTA AUDITORÍA VERIFICA:
  (0) IDENTIDAD: AethonMod v6.133 · 516 entradas (527 − 11 del
      stabber).
  (1) EOF EXACTO + (2) TODOS LOS BLOBS INFLAN + hjson == fuente.
  (3) LA COPIA EXACTA: los 31 parámetros geométricos de Runas del
      Vacío == Runas del Eclipse (parseo del .cs real), SOLO las
      correcciones del vacío difieren (paleta · Ruido_6 · runas 2 ·
      escalaGlobal 0,9 · alitas 0,55), y el Eclipse INTACTO.
  (4) LA DEVORACIÓN CONGELADA: la coreografía v6.132 intacta (el
      jugador quieto, sin lerp, el toque por colisión) — la letra
      dice que ya está lista.
  (5) LA PURGA DEL STABBER: NINGÚN archivo Dazzling/LlamasSagradas
      en el paquete NI en el árbol, cero claves hjson, cero
      referencias .cs (salvo las dos citas históricas de la letra del
      usuario), la bolsa 28 sin el staff, y la MÁQUINA de las
      cuchillas VIVA (SliceTarget & co. en fuente y DLL).
  (6) LA PARIDAD hjson ×3.
"""
import importlib.util
import os
import re
import zlib

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
RAIZ = os.path.join(REPO, 'AethonMod')
TMOD = os.path.expanduser('~/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod')

_falla = []


def check(nombre, ok, det=''):
    print(('  OK ' if ok else '  FALLA ') + nombre + (f' [{det}]' if det and not ok else (f' ({det})' if det else '')))
    if not ok:
        _falla.append(nombre)


mp_spec = importlib.util.spec_from_file_location('parse_tmod', os.path.join(REPO, 'tools', 'parse_tmod.py'))
mp = importlib.util.module_from_spec(mp_spec)
mp_spec.loader.exec_module(mp)

d = open(TMOD, 'rb').read()

# === (0) IDENTIDAD ===
tmlver, name, ver, entries, tend, fsize = mp.parse(TMOD)
print(f'(0) {name} v{ver} · tML {tmlver} · {len(entries)} entradas · {fsize:,} B · archivo {len(d):,} B')
check('la versión es 6.133', ver == '6.133', ver)
check('las entradas son 520 (527 v6.132 − 7 rawimg del stabber — los .cs viven en la DLL)',
      len(entries) == 520, str(len(entries)))

# === (1) EOF + (2) BLOBS ===
bl, off, malas = {}, tend, 0
for path, raw, comp in entries:
    trozo = d[off:off + comp]
    try:
        blob = trozo if comp == raw else zlib.decompress(trozo, -15)
        if len(blob) == raw:
            bl[path] = blob
        else:
            malas += 1
    except zlib.error:
        malas += 1
    off += comp
check('(1) EOF EXACTO', off == len(d) == fsize, f'{off} vs {len(d)}/{fsize}')
check(f'(2) TODOS LOS BLOBS INFLAN ({len(bl)}/{len(entries)}, {malas} malas)',
      len(bl) == len(entries) and malas == 0, f'{len(bl)}/{len(entries)}')

for lang in ('es-ES', 'es-MX', 'en-US'):
    p = f'Localization/{lang}_Mods.AethonMod.hjson'
    fuente = open(os.path.join(RAIZ, p), 'rb').read()
    check(f'(2b) hjson == fuente: {lang}', p in bl and bl[p] == fuente)

# === (3) LA COPIA EXACTA ===
CS = open(os.path.join(RAIZ, 'Content/VFX/AlasEstilo.cs'), encoding='utf-8').read()


def entrada(nombre):
    i = CS.index(f'Ala("{nombre}"')
    prof = 0
    for k in range(i, len(CS)):
        if CS[k] == '(':
            prof += 1
        elif CS[k] == ')':
            prof -= 1
            if prof == 0:
                return CS[i:k + 1]
    return CS[i:]


def params(nombre):
    raw = entrada(nombre)
    tail = re.sub(r'"Ruido_\d+"', '', raw[raw.index('"Ruido_'):], count=1)
    out = []
    for tok in re.findall(r'-?\d+\.?\d*f?|true|false', tail):
        out.append(tok if tok in ('true', 'false') else float(tok.rstrip('f')))
    return out


E, V = params('Runas del Eclipse'), params('Runas del Vacío')
GEOM_HASTA = 31          # plumas..luz (todo lo de forma)
dif = [k for k in range(GEOM_HASTA) if k >= len(E) or k >= len(V) or E[k] != V[k]]
check(f'(3) los {GEOM_HASTA} parámetros geométricos del Vacío == Eclipse', not dif, f'índices {dif}')
rawV, rawE = entrada('Runas del Vacío'), entrada('Runas del Eclipse')
check('(3) las correcciones del vacío: paleta violeta + Ruido_6',
      'new Color(150, 84, 235)' in rawV and '"Ruido_6"' in rawV and 'DefinitivasLib.Ambar' in rawE)
check('(3) las runas del vacío (2) y no las del eclipse (3)', V[31] == 2.0 and E[31] == 3.0, f'V={V[31]} E={E[31]}')
check('(3) la escala 0,9 y las alitas 0,55 viven (lo que SÍ gustó)',
      'escalaGlobal: 0.9f' in rawV and 'alitas: 0.55f' in rawV)
check('(3) el Eclipse INTACTO (sin escala ni alitas)',
      'escalaGlobal' not in rawE and 'alitas' not in rawE)

# === (4) LA DEVORACIÓN CONGELADA ===
dev = open(os.path.join(RAIZ, 'Content/Projectiles/Definitivas/DevoracionEterna.cs'), encoding='utf-8').read()
check('(4) la devoración v6.132 INTACTA (quieta·apresa·avalanza·toca)',
      'LA PRESA QUIETA' in dev and 'EL APRESAMIENTO' in dev and
      'EL GRIMORIO SE AVALANZA' in dev and 'rectBoca.Intersects(p.Hitbox)' in dev and
      'p.Center = Vector2.Lerp' not in dev)
check('(4) el compás de la emboscada intacto (TVueloLibro 26)',
      'private const int TVueloLibro = 26;' in dev)

# === (5) LA PURGA DEL STABBER ===
en_arbol = []
for root, _dirs, files in os.walk(os.path.join(RAIZ, 'Content')):
    for f in files:
        if 'Dazzling' in f or 'LlamasSagradas' in f:
            en_arbol.append(f)
check('(5) NINGÚN archivo Dazzling/LlamasSagradas en el árbol', not en_arbol, str(en_arbol))
en_pkg = [p for p in bl if 'Dazzling' in p or 'LlamasSagradas' in p]
check('(5) NINGÚN Dazzling/LlamasSagradas en el .tmod', not en_pkg, str(en_pkg[:3]))

hjson_malos = []
for lang in ('es-ES', 'es-MX', 'en-US'):
    s = open(os.path.join(RAIZ, f'Localization/{lang}_Mods.AethonMod.hjson'), encoding='utf-8').read()
    if re.search(r'^\t.*(Dazzling|Stabber|LlamasSagradas)', s, re.M):
        hjson_malos.append(lang)
check('(5) CERO claves Dazzling/Stabber/LlamasSagradas en hjson ×3', not hjson_malos, str(hjson_malos))

daga = open(os.path.join(RAIZ, 'Content/Projectiles/DagaEternaMinion.cs'), encoding='utf-8').read()
menciones = [l.strip() for l in daga.split('\n')
             if re.search(r'Dazzling|Calamity', l) and 'borrar' not in l]
check('(5) DagaEternaMinion SIN referencias (solo la cita histórica de la letra)',
      len(menciones) == 0, str(menciones[:2]))
check('(5) la MÁQUINA de las cuchillas VIVA en fuente',
      all(f in daga for f in ('SliceTarget', 'ManageAttackCooldown', 'ReturnToRestingPosition', 'DireccionSegura')))

bolsa = open(os.path.join(RAIZ, 'Content/Items/Bolsas/BolsasCategorias.cs'), encoding='utf-8').read()
check('(5) la bolsa 28 SIN el staff (Arbitrador y Principio siguen)',
      'DazzlingStabberStaff' not in bolsa.replace('// borrar Dazzling Stabber Staff de Calamity Mod del codigo y', '') and
      'ArbitradorII>(), 1' in bolsa and 'ElPrincipioYElFin>(), 1' in bolsa)

dll = bl.get('AethonMod.dll', b'')
check('(5) la DLL viaja DENTRO', len(dll) > 100000, f'{len(dll):,} B')
for firma in ('SliceTarget', 'ManageAttackCooldown', 'ReturnToRestingPosition', 'RestOffsetAngle', 'attackCooldown'):
    check(f'(5) la máquina VIVE en la DLL: {firma}', firma.encode('utf-8') in dll)
for muerto in (b'DazzlingStabberStaff', b'LlamasSagradas', b'DazzlingStabberBuff'):
    check(f'(5) el muerto NO viaja en la DLL: {muerto.decode()}', muerto not in dll)

# === (6) LA PARIDAD hjson ×3 ===
import hjson


def claves(lang):
    dd = hjson.load(open(os.path.join(RAIZ, f'Localization/{lang}_Mods.AethonMod.hjson'), encoding='utf-8'))
    plano = set()

    def walk(pref, nodo):
        for k, v in nodo.items():
            if isinstance(v, dict):
                walk(pref + k + '.', v)
            else:
                plano.add(pref + k)
    walk('', dd)
    return plano


es, mx, en = claves('es-ES'), claves('es-MX'), claves('en-US')
check(f'(6) la paridad de claves ×3 ({len(es)}/{len(mx)}/{len(en)})', es == mx == en,
      f'{len(es)}/{len(mx)}/{len(en)}')

print()
if _falla:
    print(f'AUDIT v6.133 — {len(_falla)} FALLAS: ' + '; '.join(_falla))
    raise SystemExit(1)
print('AUDIT v6.133 TODO VERIFICADO ✔')
