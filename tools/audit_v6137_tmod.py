#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6137_tmod.py — LA AUDITORÍA DEL .tmod v6.137 — EL PILAR DEL
CUERPO DEL DIOS: LA ARENA INTERIOR.

LO QUE ESTA AUDITORÍA VERIFICA (la letra de la sesión 18 completa):
  (0) IDENTIDAD: v6.137 · las entradas que manda el build (521 + 2 png
      de alas nuevas = 523).
  (1) EOF EXACTO + (2) TODOS LOS BLOBS INFLAN + hjson == fuente ×3.
  (3) LAS ALAS: catálogo 14 (12 + las DOS copias de la Valquiria),
      estilos 12 (SIN plumas: plumas=2) y 13 (LISA: lisas:true), items
      + bolsa (13 alas) + PNG en disco y .tmod.
  (4) LAS CUCHILLAS-COLMILLOS: el dibujado usa ColmilloAbismo (no la
      hoja vieja) y la máquina sigue viva.
  (5) EL MUÑECO: receta 5+5 · bruma BrumaFX (NO SoftGlow plano) · los
      999 en el regalo.
  (6) EL ALTAR ÚNICO: el sistema + el rebote del ítem + el libro nace
      ENCIMA (cima−40) + el interior limpio (CanUseItem niega dentro).
  (7) EL PILAR: la ventana nueva (selector · LANZAR · jefes con
      nivel de poder) + la arena (JefeInteriorGlobalNPC: sello · aura ·
      +25% · IA por niveles · kills persistentes · MundoLibre ve la
      arena) + la coreografía prestada.
  (8) LA PARIDAD hjson ×3.
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
print(f'(0) {name} v{ver} · tML {tmlver} · {len(entries)} entradas · {fsize:,} B')
check('la versión es 6.137', ver == '6.137', ver)
check(f'las entradas son 523 (521 de la v6.136 + 2 alas)', len(entries) == 523, len(entries))

# === (1) EOF + (2) BLOBS (el patrón del v6.136) ===
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
      len(bl) == len(entries) and malas == 0)
for lang in ('es-ES', 'es-MX', 'en-US'):
    p = f'Localization/{lang}_Mods.AethonMod.hjson'   # sin prefijo — así viaja
    fuente = open(os.path.join(RAIZ, p), 'rb').read()
    check(f'(2b) hjson == fuente: {lang}', p in bl and bl[p] == fuente)

dll = bl.get('AethonMod.dll', b'')
check('(2c) la DLL viaja DENTRO', len(dll) > 100000, f'{len(dll):,} B')

# === (3) LAS ALAS — LAS DOS COPIAS DE LA VALQUIRIA ===
estilo = open(os.path.join(RAIZ, 'Content/VFX/AlasEstilo.cs'), encoding='utf-8').read()
check('(3) el catálogo es de 14 estilos', 'EstiloAlas[14]' in estilo)
check('(3) los const nuevos (12 y 13)', 'ValquiriaSinPlumas = 12' in estilo and 'ValquiriaLisa = 13' in estilo)
check('(3) la copia SIN PLUMAS: plumas=2 (el mínimo estructural)',
      'Ala("Valquiria Sin Plumas"' in estilo and '"Ruido_13", 2, 92f' in estilo)
check('(3) la copia LISA: lisas:true (la vela continua)',
      'Ala("Valquiria Lisa"' in estilo and '"Ruido_13", 7, 92f' in estilo)
items = open(os.path.join(RAIZ, 'Content/Items/Alas/AlasMitologicasV6135.cs'), encoding='utf-8').read()
check('(3) los items AlasValquiriaSinPlumas + AlasValquiriaLisa',
      'class AlasValquiriaSinPlumas' in items and 'class AlasValquiriaLisa' in items)
bolsa = open(os.path.join(RAIZ, 'Content/Items/Bolsas/BolsasCategorias.cs'), encoding='utf-8').read()
check('(3) la bolsa lleva las 13 alas (una de cada, sin duplicar)',
      bolsa.count('AlasValquiriaSinPlumas>(), 1') == 1 and bolsa.count('AlasValquiriaLisa>(), 1') == 1
      and 'AlasQuetzalEmplumado>(), 1' in bolsa)
for n in ('AlasValquiriaSinPlumas', 'AlasValquiriaLisa'):
    png = os.path.join(RAIZ, f'Content/Items/Alas/{n}.png')
    check(f'(3) PNG en disco: {n}.png', os.path.exists(png))
    check(f'(3) PNG en .tmod: {n}.rawimg', any(r.endswith(f'Items/Alas/{n}.rawimg') for r in bl))
# el check de la v6.135 sigue (el arcángel intacto)
check('(3) EL ARCÁNGEL intacto (coronas 0.72 en el Vacío)', 'coronas: 0.72f' in estilo)
check('(3) el catálogo de la v6.135 intacto (12 estilos base)',
      'EstiloAlas[12]' not in estilo and 'public const byte Serafin = 6' in estilo)

# === (4) LAS CUCHILLAS-COLMILLOS ===
minion = open(os.path.join(RAIZ, 'Content/Projectiles/DagaEternaMinion.cs'), encoding='utf-8').read()
check('(4) el colmillo de la forja vive en el dibujado',
      'ColmilloAbismo' in minion and 'ColmilloAbismo", ReLogic' in minion)
check('(4) la hoja vieja murió en el dibujado',
      '"AethonMod/Content/Projectiles/DagaEterna_Hoja", ReLogic' not in minion)
check('(4) la máquina de las cuchillas INTACTA',
      all(f in minion for f in ('SliceTarget', 'ManageAttackCooldown',
                                'ReturnToRestingPosition', 'DireccionSegura')))
check('(4) el ojo del grimorio sigue en el trono (la encía)', 'TronoDelOjo' in minion)

# === (5) EL MUÑECO DE LA CASA ===
ditem = open(os.path.join(RAIZ, 'Content/Items/Llamados/DummyNegroItem.cs'), encoding='utf-8').read()
check('(5) la receta 5 madera + 5 esencias (lote de 5)',
      'AddIngredient(ItemID.Wood, 5)' in ditem and 'ItemCorrupto>(), 5)' in ditem)
dnpc = open(os.path.join(RAIZ, 'Content/NPCs/DummyNegroNPC.cs'), encoding='utf-8').read()
check('(5) la bruma de la casa (BrumaFX.Puff)',
      'BrumaFX.Puff' in dnpc)
check('(5) el círculo plano MURIÓ (SoftGlow estirado fuera)',
      'glow.Size() * 0.5f, new Vector2(marco.Width * 2.6f' not in dnpc)
sp = open(os.path.join(RAIZ, 'Content/Players/ShardPlayer.cs'), encoding='utf-8').read()
check('(5) los 999 muñecos en el regalo', 'DummyNegroItem' in sp and 'tipoDummy, 999' in sp)

# === (6) EL ALTAR ÚNICO ===
sis = os.path.join(RAIZ, 'Content/Systems/AltarUnicoSistema.cs')
check('(6) AltarUnicoSistema.cs en disco', os.path.exists(sis))
sistema = open(sis, encoding='utf-8').read() if os.path.exists(sis) else ''
check('(6) la ley: registro + promoción + persistencia',
      all(f in sistema for f in ('public static void Registrar', 'PromoverSiHaceFalta',
                                 'SaveWorldData', 'LoadWorldData')))
aitem = open(os.path.join(RAIZ, 'Content/Items/Altar/AltarOfrendasItem.cs'), encoding='utf-8').read()
check('(6) el ítem rebota si ya hay altar (YaHayUno)',
      'Altar.YaHayUno' in aitem and aitem.count('public override bool CanUseItem') == 1)
check('(6) el ítem NIEGA dentro del interior (NoInterior)', 'Altar.NoInterior' in aitem)
atile = open(os.path.join(RAIZ, 'Content/Tiles/AltarOfrendasTile.cs'), encoding='utf-8').read()
check('(6) el tile apaga su luz cuando no es el activo', 'ModifyLight' in atile and 'EsElActivo' in atile)
check('(6) el cursor marca al DESACTIVADO (la técnica del pilar)', 'DesactivadoCorto' in atile)
check('(6) la promoción al picar el activo (CanDrop)', 'PromoverSiHaceFalta' in atile)
caida = open(os.path.join(RAIZ, 'Content/Systems/LaCaidaSistema.cs'), encoding='utf-8').read()
check('(6) el libro nace ENCIMA de la cima del altar (−40px)',
      'cimaAltar * 16f - 40f - 21f' in caida)
check('(6) la llegada REGISTRA su altar como el activo', 'AltarUnicoSistema.Registrar' in caida)
uia = open(os.path.join(RAIZ, 'Content/UI/AltarOfrendasUI.cs'), encoding='utf-8').read()
check('(6) el aire en los títulos de sección (v6.137)', 'cy += 8f;   // el respiro' in uia)

# === (7) EL PILAR + LA ARENA ===
prisma = open(os.path.join(RAIZ, 'Content/UI/PrismaInteriorUI.cs'), encoding='utf-8').read()
check('(7) el selector de nivel (1..FuriaNivel)', 'FuriaNivel' in prisma and '_geoMenos' in prisma)
check('(7) LANZAR LA OLEADA (natural:false — probar no es progresar)',
      'LanzarLaOleada' in prisma and 'natural: false' in prisma)
check('(7) la lista de jefes devorados con su nivel de poder',
      'JefesDevorados' in prisma and 'KillsInteriorDe' in prisma)
check('(7) el panel MEDIDO (la cura del desborde: la altura del contenido)',
      'altoContenido' in prisma)
check('(7) el scroll de la lista (la rueda de la casa)', 'ScrollWheelValue' in prisma)
arena_p = os.path.join(RAIZ, 'Content/Globals/JefeInteriorGlobalNPC.cs')
check('(7) JefeInteriorGlobalNPC.cs en disco', os.path.exists(arena_p))
arena = open(arena_p, encoding='utf-8').read() if os.path.exists(arena_p) else ''
check('(7) el sello de la arena (Sellar)', 'public static void Sellar' in arena)
check('(7) el +25% por muerte (Multiplicador)', '1f + 0.25f *' in arena)
check('(7) el AURA de la oleada (OleadaGrimorio)', 'AuraLib.OleadaGrimorio(3)' in arena)
check('(7) la IA por niveles (préstamo maestro 6+ · coreografía 8+ · combos 10)',
      '_overrideForExpertMode' in arena and 'CoreografiaPrestada' in arena and 'NivelIA >= 10 ? 36 : 60' in arena)
check('(7) los jefes del mod: SOLO stats+experto (EsVanilla gatea la IA)',
      'EsVanilla(npc.type)' in arena)
check('(7) los kills interiores persisten en ShardPlayer',
      'MuertesInteriorTipos' in sp and 'MarcarKillInterior' in sp)
oleada = open(os.path.join(RAIZ, 'Content/Globals/OleadaNPC.cs'), encoding='utf-8').read()
check('(7) la coreografía PRESTADA (el wrapper público)', 'public static void CoreografiaPrestada' in oleada)
furia = open(os.path.join(RAIZ, 'Content/Systems/GrimorioFuriaSistema.cs'), encoding='utf-8').read()
check('(7) el festín ESPERA a los jefes de la arena (MundoLibre)',
      'JefeInteriorGlobalNPC' in furia and 'ji.EsInterior' in furia)
# los que ya funcionaban SIGUEN (la regla de la casa)
check('(7) VOLVER + INVOCAR AETHON intactos',
      'BotonVolver' in prisma and 'InvocarLaLlamada' in prisma)

# === (8) LA PARIDAD hjson ×3 ===
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
check(f'(8) la paridad de claves ×3 ({len(es)}/{len(mx)}/{len(en)})', es == mx == en,
      f'{len(es)}/{len(mx)}/{len(en)}')

# y las claves nuevas EXISTEN (en las tres)
for k in ('PrismaInterior.OleadaTitulo', 'PrismaInterior.OleadaNivel', 'PrismaInterior.OleadaPista',
          'PrismaInterior.OleadaLanzar', 'PrismaInterior.OleadaCorre', 'PrismaInterior.JefesTitulo',
          'PrismaInterior.JefesVacio', 'PrismaInterior.JefePoder', 'PrismaInterior.PoderCrece',
          'Altar.YaHayUno', 'Altar.Desactivado', 'Altar.DesactivadoCorto', 'Altar.Promovido', 'Altar.NoInterior',
          'Items.AlasValquiriaSinPlumas.DisplayName', 'Items.AlasValquiriaLisa.DisplayName'):
    check(f'(8b) la clave vive ×3: {k}', k in es and k in mx and k in en)

print()
if _falla:
    print(f'AUDIT v6.137 — {len(_falla)} FALLAS: ' + '; '.join(_falla))
    raise SystemExit(1)
print('AUDIT v6.137 TODO VERIFICADO ✔')
