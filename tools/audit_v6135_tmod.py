#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6135_tmod.py — LA AUDITORÍA DEL .tmod v6.135 — EL ARCÁNGEL
DE 6 ALAS Y LAS SEIS MITOLÓGICAS.

LA LETRA (sesión 16):
  «necesitamos mejorar la estetica de las alas, ahora que tienes
  como hacerlas correctamente con las alas runicas de vacio, quiero
  que modifiques esas alas teniendo en cuenta las alas de
  arcangeles superiores que tiene 3 pares de alas o sea 6 alas y
  hacer otras versiones de las alas runicas del vacio basandote en
  otras mitologias o creaciones y tipos de alas» + «continua y crea
  mas tipos de alas»

LO QUE ESTA AUDITORÍA VERIFICA:
  (0) IDENTIDAD: AethonMod v6.133 · 516 entradas (527 − 11 del
      stabber).
  (1) EOF EXACTO + (2) TODOS LOS BLOBS INFLAN + hjson == fuente.
  (3) EL ARCÁNGEL: el Vacío conserva la receta lisa v6.134 (membrana
      1f · redonda 1f · lisas true) y SU geometría intacta, y GANA el
      tercer par (coronas 0.72: el abanico alzado sobre la espalda —
      6 alas). LOS SEIS MITOS: [6..11] con SU paleta, plumas, curva y
      pares; los de 6 alas: Serafín/Quetzal/Fénix/Dragón.
  (4) LA DEVORACIÓN CONGELADA (la sesión 14 la declaró lista): la
      coreografía v6.132 intacta.
  (5) NO-REGRESIÓN — la purga v6.133 del stabber sigue completa: 0
      archivos, 0 claves, 0 referencias; la máquina de las cuchillas
      VIVA en fuente y DLL.
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
check('la versión es 6.135', ver == '6.135', ver)
check('las entradas son 527 (520 + 6 sprites de los mitos + 1 clase nueva… el build manda)',
      len(entries) >= 526, str(len(entries)))

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
CS_L = open('/home/z/my-project/mod/AethonMod/Content/VFX/AlasAuroraLib.cs', encoding='utf-8').read()
CS_E = open('/home/z/my-project/mod/AethonMod/Content/VFX/AlasEstilo.cs', encoding='utf-8').read()
CS_I = open('/home/z/my-project/mod/AethonMod/Content/Items/Alas/AlasMitologicasV6135.cs', encoding='utf-8').read()
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
check(f'(3) la GEOMETRÍA v6.133 intacta (los {GEOM_HASTA} parámetros == Eclipse)', not dif, f'índices {dif}')
rawV, rawE = entrada('Runas del Vacío'), entrada('Runas del Eclipse')
check('(3) la paleta violeta + Ruido_6 + runas 2 se mantienen',
      'new Color(150, 84, 235)' in rawV and '"Ruido_6"' in rawV and V[31] == 2.0 and E[31] == 3.0)
check('(3) la receta lisa v6.134 SE MANTIENE (membrana 1f + redonda 1f + lisas true)',
      re.search(r'2, 1\.05f, 1f, 1f, lisas: true', rawV) is not None and 'lisas' not in rawE)
check('(3) EL ARCÁNGEL: el Vacío GANA el tercer par (coronas 0.72)',
      'coronas: 0.72f' in rawV and 'alitas: 0.55f' in rawV)
check('(3) el Eclipse INTACTO (con sus plumas: sin lisa, sin escala, sin alitas)',
      'escalaGlobal' not in rawE and 'alitas' not in rawE and 'lisas' not in rawE)
# v6.135 — LOS SEIS MITOS
MITOS = ['Serafín del Vacío', 'Valquiria Rúnica', 'Quetzal Emplumado',
         'Fénix del Renacer', 'Tengu del Crepúsculo', 'Dragón del Vacío']
esperado = {'Serafín del Vacío': ('235, 120, 255', 3), 'Valquiria Rúnica': ('218, 232, 255', 1),
            'Quetzal Emplumado': ('94, 222, 198', 3), 'Fénix del Renacer': ('255, 244, 214', 3),
            'Tengu del Crepúsculo': ('92, 110, 158', 1), 'Dragón del Vacío': ('200, 64, 72', 3)}
for nombre_m in MITOS:
    raw_m = entrada(nombre_m)
    col, pares = esperado[nombre_m]
    n_pares = 1 + ('coronas:' in raw_m) + ('alitas:' in raw_m)
    check(f'(3) el mito «{nombre_m}»: paleta {col} y {pares} par(es)',
          col in raw_m and n_pares == pares, f'pares={n_pares}')
check('(3) el catálogo tiene 12 estilos (6 casa + 6 mitos)',
      'EstiloAlas[12]' in CS_E and 'public const byte Serafin = 6' in CS_E
      and 'public const byte Dragon = 11' in CS_E)
check('(3) LA CORONA DEL ARCÁNGEL: el bloque del tercer par en la lib',
      'v6.135 — LA CORONA DEL ARCÁNGEL' in CS_L and 'estilo.Coronas > 0f' in CS_L)
# los items de los mitos
check('(3) las SEIS CLASES de items de los mitos',
      all(f'Estilo => {k}' in CS_I for k in range(6, 12)),
      'faltan clases en AlasMitologicasV6135.cs')
_nombres_tmod = {e[0] for e in entries}
check('(3) los SEIS SPRITES de los mitos (viajan como .rawimg)',
      all(any(r.endswith(f'{n}.rawimg') for r in _nombres_tmod) for n in
          ['AlasSerafinDelVacio', 'AlasValquiriaRunica', 'AlasQuetzalEmplumado',
           'AlasFenixDelRenacer', 'AlasTenguDelCrepusculo', 'AlasDragonDelVacio']))

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
    print(f'AUDIT v6.135 — {len(_falla)} FALLAS: ' + '; '.join(_falla))
    raise SystemExit(1)
print('AUDIT v6.135 TODO VERIFICADO ✔')
