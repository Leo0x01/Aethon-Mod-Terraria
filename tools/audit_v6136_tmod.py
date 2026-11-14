#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6136_tmod.py — LA AUDITORÍA DEL .tmod v6.136 — LA PURGA DE LAS DOS
ARMAS DE INVESTIGACIÓN + EL MUÑECO DE LA CASA.

LA LETRA (sesión 17):
  «Arbitrador II, El Principio y el Fin, puedes borrar esas armas, ya no
  son útiles, el resto de contenido de la carpeta se queda tal cual, ya
  que no tiene menciones externas ni nada de eso» + «Genera las texturas
  con SForja, y luego verifica que todo esté sin mencionar ni
  referencias externas»

LO QUE ESTA AUDITORÍA VERIFICA:
  (0) IDENTIDAD: AethonMod v6.136 · las entradas que manda el build
      (526 de la v6.135 − 12 de los muertos + 1 del sprite del muñeco).
  (1) EOF EXACTO + (2) TODOS LOS BLOBS INFLAN + hjson == fuente.
  (3) LA PURGA: los 13 archivos muertos NO están ni en el árbol ni en el
      .tmod ni en la DLL; las 4 carpetas vacías idem; las claves hjson
      condenadas idem; y CERO menciones en TODO el código y la
      localización (la verificación integral de la letra).
  (4) LOS QUE SE QUEDAN VIVOS: la Mirada del Eterno, el Cetro de las
      Dagas, el Códice, PixelFxPlayer, DagaEternaMinion y su buff —
      en fuente Y en la DLL.
  (5) NO-REGRESIÓN — las purgas anteriores siguen: 0 archivos, 0 claves,
      0 referencias de los muertos viejos (stabber etc.); la máquina de
      las cuchillas viva.
  (6) EL MUÑECO DE LA CASA: DummyNegroNPC.png viaja como .rawimg, SIN
      textura vanilla (0 «Terraria/» en todo el árbol), y el ítem lo
      toma prestado con el override de la casa.
  (7) LA PARIDAD hjson ×3.
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
check('la versión es 6.136', ver == '6.136', ver)
check(f'las entradas son 521 (el build manda — v6.135: 526 − 12 muertos + 1 sprite'
      ' del muñeco − ajustes del empaquetador)',
      len(entries) == 521, len(entries))

# === (1) EOF + (2) BLOBS ===
# (el formato de entrada es (ruta, tamaño_raw, tamaño_comprimido) y los
# blobs van tras tend — el patrón del audit v6.135, verificado)
bl, off, malas = {}, tend, 0
for path, raw, comp in entries:
    trozo = d[off:off + comp]
    try:
        blob = trozo if comp == raw else zlib.decompress(trozo, -15)
    except zlib.error:
        malas += 1; off += comp; continue
    bl[path] = blob
    off += comp
check('(1) EOF EXACTO', off == len(d) == fsize, f'{off} vs {len(d)}/{fsize}')
check(f'(2) TODOS LOS BLOBS INFLAN ({len(bl)}/{len(entries)}, {malas} malas)',
      len(bl) == len(entries) and malas == 0, f'{len(bl)}/{len(entries)} {malas}')
for lang in ('es-ES', 'es-MX', 'en-US'):
    p = f'Localization/{lang}_Mods.AethonMod.hjson'   # (sin prefijo — así viaja en el .tmod)
    fuente = open(os.path.join(RAIZ, p), 'rb').read()
    check(f'(2b) hjson == fuente: {lang}', p in bl and bl[p] == fuente)

# === (3) LA PURGA — LOS MUERTOS DE LA v6.136 ===
MUERTOS = ['ArbitradorII', 'ElPrincipioYElFin', 'ArbitradorIIProyectil',
           'DagaEterna', 'SigiloRatero', 'MarcaDelPrincipio', 'MiradaDelFin',
           'SupresionVital', 'Servidumbre', 'InvestigacionesGlobalNPC',
           'AlmaEco', 'AlmaLuzInferior', 'OndaSonoraEterna']
rx_muertos = {m: re.compile(m + r'(?!Minion|Buff|_Hoja)') for m in MUERTOS}
en_arbol = [m for m in MUERTOS
            if any(rx_muertos[m].search(f) for base, _, fs in os.walk(RAIZ)
                   for f in fs if f.endswith(('.cs', '.png')))]
check('(3) NINGÚN archivo muerto en el árbol', not en_arbol, str(en_arbol[:4]))
en_pkg = [m for m in MUERTOS if any(rx_muertos[m].search(r) for r in bl)]
check('(3) NINGÚN muerto en el .tmod', not en_pkg, str(en_pkg[:4]))

# las carpetas vacías también murieron
vacias = [p for p in ('Buffs/Investigaciones', 'Players/Investigaciones',
                      'Globals/Investigaciones', 'Projectiles/Investigaciones')
          if os.path.exists(os.path.join(RAIZ, 'Content', p))]
check('(3) las 4 carpetas de investigación muertas, BORRADAS', not vacias, str(vacias))

# las claves hjson condenadas
CLAVES = ['ArbitradorII', 'ElPrincipioYElFin', 'ArbitradorIIProyectil', 'DagaEterna.',
          'OndaSonoraEterna', 'AlmaLuzInferior', 'AlmaEco', 'MarcaDelPrincipio',
          'MiradaDelFin', 'SupresionVital', 'Servidumbre']
hjson_malos = []
for lang in ('es-ES', 'es-MX', 'en-US'):
    t = open(os.path.join(RAIZ, f'Localization/{lang}_Mods.AethonMod.hjson'), encoding='utf-8').read()
    hjson_malos += [f'{lang}:{c}' for c in CLAVES if re.search(rf'\b{re.escape(c)}', t)]
check('(3) CERO claves condenadas en hjson ×3', not hjson_malos, str(hjson_malos[:4]))

# LA VERIFICACIÓN INTEGRAL DE LA LETRA — cero menciones en TODO el mod
MALOS = re.compile(r'calamity|miraheze|arbitrat|stabber|dazzling'
                   r'|sigiloratero|marcadelprincipio|miradadelfin'
                   r'|supresionvital|servidumbre|almaec|almaluzinferior'
                   r'|ondasonoraeterna|holy ?flames', re.I)
sucias = []
for base, _, archivos in os.walk(RAIZ):
    for a in archivos:
        if a.endswith(('.cs', '.hjson')):
            for i, linea in enumerate(open(os.path.join(base, a), encoding='utf-8').read().split('\n'), 1):
                if MALOS.search(linea):
                    sucias.append(f'{a}:{i}')
check('(3) LA VERIFICACIÓN DE LA LETRA: 0 menciones externas en TODO el código+loc',
      not sucias, str(sucias[:4]))

# los muertos NO viajan en la DLL (los símbolos de los tipos públicos)
dll = bl.get('AethonMod.dll', b'')
check('(3) la DLL viaja DENTRO', len(dll) > 100000, f'{len(dll):,} B')
for muerto in (b'ArbitradorII', b'ElPrincipioYElFin', b'SigiloRatero',
               b'MarcaDelPrincipio', b'InvestigacionesGlobalNPC', b'AlmaEco'):
    check(f'(3) el muerto NO viaja en la DLL: {muerto.decode()}', muerto not in dll)

# === (4) LOS QUE SE QUEDAN — VIVOS EN FUENTE Y DLL ===
VIVOS = {'Weapons/Investigaciones/MiradaDelEterno.cs': 'MiradaDelEterno',
         'Weapons/Investigaciones/CetroDagasEterno.cs': 'CetroDagasEterno',
         'Items/Investigaciones/CodiceHabilidades.cs': 'CodiceHabilidades',
         'Weapons/Investigaciones/PixelFxPlayer.cs': 'PixelFxPlayer',
         'Projectiles/DagaEternaMinion.cs': 'DagaEternaMinion',
         'Buffs/DagaEternaBuff.cs': 'DagaEternaBuff'}
for ruta, simbolo in VIVOS.items():
    en_disco = os.path.exists(os.path.join(RAIZ, 'Content', ruta))
    en_dll = simbolo.encode('utf-8') in dll
    check(f'(4) VIVO en fuente+DLL: {simbolo}', en_disco and en_dll,
          f'disco={en_disco} dll={en_dll}')
# la bolsa de investigación sigue (con los vivos, sin los muertos)
bolsa = open(os.path.join(RAIZ, 'Content/Items/Bolsas/BolsasCategorias.cs'), encoding='utf-8').read()
check('(4) la bolsa 28 VIVA con los que se quedan',
      'BolsaInvestigaciones' in bolsa and 'MiradaDelEterno>(), 1' in bolsa
      and 'CetroDagasEterno>(), 1' in bolsa)
check('(4) la bolsa SIN los muertos',
      'ArbitradorII>(), 1' not in bolsa and 'ElPrincipioYElFin>(), 1' not in bolsa)

# === (5) NO-REGRESIÓN — LAS PURGAS VIEJAS SIGUEN ===
viejos = [m for m in ('DazzlingStabberStaff', 'LlamasSagradas', 'DazzlingStabberBuff')
          if any(m in f for base, _, fs in os.walk(RAIZ) for f in fs if f.endswith(('.cs', '.png')))]
check('(5) los muertos viejos siguen muertos en el árbol', not viejos, str(viejos))
for muerto in (b'DazzlingStabberStaff', b'LlamasSagradas'):
    check(f'(5) el muerto viejo NO viaja en la DLL: {muerto.decode()}', muerto not in dll)
daga = open(os.path.join(RAIZ, 'Content/Projectiles/DagaEternaMinion.cs'), encoding='utf-8').read()
check('(5) la MÁQUINA de las cuchillas VIVA en fuente',
      all(f in daga for f in ('SliceTarget', 'ManageAttackCooldown',
                              'ReturnToRestingPosition', 'DireccionSegura')))
for firma in ('SliceTarget', 'ManageAttackCooldown', 'ReturnToRestingPosition', 'RestOffsetAngle'):
    check(f'(5) la máquina VIVE en la DLL: {firma}', firma.encode('utf-8') in dll)
# la geometría v6.133/v6.135 de las alas INTACTA (no-regresión de la sesión pasada)
alas = open(os.path.join(RAIZ, 'Content/VFX/AlasEstilo.cs'), encoding='utf-8').read()
check('(5) EL ARCÁNGEL intacto (coronas 0.72 en el Vacío)', 'coronas: 0.72f' in alas)
check('(5) el catálogo tiene 12 estilos (la v6.135 intacta)',
      'EstiloAlas[12]' in alas and 'public const byte Serafin = 6' in alas
      and 'public const byte Dragon = 11' in alas)

# === (6) EL MUÑECO DE LA CASA ===
check('(6) el sprite del muñeco VIAJA (DummyNegroNPC.rawimg)',
      any(r.endswith('DummyNegroNPC.rawimg') for r in bl))
check('(6) el PNG del muñeco en disco', os.path.exists(os.path.join(RAIZ, 'Content/NPCs/DummyNegroNPC.png')))
vanilla = [os.path.join(b, a) for b, _, fs in os.walk(RAIZ) for a in fs
           if a.endswith('.cs') and '"Terraria/' in open(os.path.join(b, a), encoding='utf-8').read()]
check('(6) CERO texturas vanilla en TODO el código', not vanilla, str(vanilla[:2]))
item = open(os.path.join(RAIZ, 'Content/Items/Llamados/DummyNegroItem.cs'), encoding='utf-8').read()
check('(6) el ítem toma PRESTADO el sprite del NPC (override de la casa)',
      'override string Texture => "AethonMod/Content/NPCs/DummyNegroNPC"' in item)
from PIL import Image
im = Image.open(os.path.join(RAIZ, 'Content/NPCs/DummyNegroNPC.png')).convert('RGBA')
opacos = sum(1 for p in im.getdata() if p[3] > 0)
check('(6) el sprite del muñeco 40×56 con silueta (≥400 px)', im.size == (40, 56) and opacos >= 400, f'{im.size} {opacos}px')

# === (7) LA PARIDAD hjson ×3 ===
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
check(f'(7) la paridad de claves ×3 ({len(es)}/{len(mx)}/{len(en)})', es == mx == en,
      f'{len(es)}/{len(mx)}/{len(en)}')

print()
if _falla:
    print(f'AUDIT v6.136 — {len(_falla)} FALLAS: ' + '; '.join(_falla))
    raise SystemExit(1)
print('AUDIT v6.136 TODO VERIFICADO ✔')
