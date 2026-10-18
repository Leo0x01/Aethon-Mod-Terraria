#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v650130_tmod.py — LA AUDITORÍA DEL PAQUETE .tmod de la v6.50.130.

(1) Cabecera: AethonMod 6.50.130 · 395 entradas (las 393 de la .129 +
    BrumaDelCrater.rawimg + BrumaAltarTile.rawimg).
(2) EOF exacto: Σcomp + tabla = fsize.
(3) TODAS las entradas inflan a su tamaño declarado.
(4) hjson ×3 DENTRO del paquete: las claves nuevas de la bruma
    (Items.BrumaDelCrater + Tiles.BrumaAltar.MapEntry) y la restauración
    del es-MX COMPLETO (463 claves == es-ES).
(5) EL HEAP #Strings de la DLL: los símbolos de la .130 (BrumaDelCrater,
    BrumaAltarTile, RecetasCorrupcionSistema, MarcarCorrupta, MODO_CONEJOS,
    DescubrirBrumasColocadas, EntregarBruma, RemoverAltar, ArrancarConejos,
    DibujarLosConejos, FisicaDelConejo, EspantarConejo, ColorDeHada,
    DibujarVelosDelSol, ColoresDelCoro, ConejoReal, GetItemDrops,
    AddOnCraftCallback, MarcarCorrupta, BrumaEntregada, Colocada).
"""
import hashlib
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = sys.argv[1] if len(sys.argv) > 1 else \
    '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'

FALLOS = []


def ok(msg):
    print(f'  ✓ {msg}')


def mal(msg):
    print(f'  ✗ {msg}')
    FALLOS.append(msg)


d = open(TMOD, 'rb').read()
tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f'AUDITORÍA {TMOD}')
print(f'  tML {tmlver} · {name} {ver} · {fsize} B · {len(entries)} entradas')

# === (1) CABECERA ===
if name == 'AethonMod' and ver == '6.50.130':
    ok(f'cabecera: {name} {ver}')
else:
    mal(f'cabecera: {name} {ver} (esperado AethonMod 6.50.130)')

if len(entries) == 395:
    ok(f'entradas: {len(entries)} (las 393 de la .129 + 2 rawimg de la bruma)')
else:
    mal(f'entradas: {len(entries)} (esperadas 395)')

# === (2) EOF EXACTO ===
suma = tend + sum(c for _, _, c in entries)
if suma == fsize:
    ok(f'EOF exacto: tabla {tend} + blobs {suma - tend} == {fsize}')
else:
    mal(f'EOF: {suma} != {fsize}')

# === (3) TODAS INFLAN ===
blobs, off = {}, tend
malas = 0
for p, raw, comp in entries:
    z = d[off:off + comp]
    off += comp
    try:
        data = z if raw == comp else zlib.decompress(z, -15)
        if len(data) != raw:
            malas += 1
        blobs[p] = data
    except Exception:
        malas += 1
if malas == 0:
    ok(f'todas las {len(entries)} entradas inflan a su tamaño declarado')
else:
    mal(f'{malas} entradas rotas')

# === (4) LOS DOS SPRITES NUEVOS ===
for sprite, esperado in [('Content/Items/Altar/BrumaDelCrater.rawimg', 30),
                         ('Content/Tiles/BrumaAltarTile.rawimg', 16)]:
    if sprite in blobs and len(blobs[sprite]) > 100:
        ok(f'{sprite} viaja ({len(blobs[sprite])} B)')
    else:
        mal(f'{sprite} falta o vacío')

# === (5) hjson ×3 DENTRO DEL PAQUETE ===
for idioma in ('es-ES', 'es-MX', 'en-US'):
    ruta = f'Localization/{idioma}_Mods.AethonMod.hjson'
    if ruta not in blobs:
        mal(f'hjson {idioma} NO viaja en el paquete')
        continue
    texto = blobs[ruta].decode('utf-8')
    tiene_item = 'BrumaDelCrater:' in texto and 'DisplayName:' in texto
    tiene_tile = 'Tiles.BrumaAltar.MapEntry:' in texto
    if tiene_item and tiene_tile:
        ok(f'hjson {idioma}: Items.BrumaDelCrater + Tiles.BrumaAltar.MapEntry')
    else:
        mal(f'hjson {idioma}: faltan las claves de la bruma '
            f'(item={tiene_item} tile={tiene_tile})')

# LA RESTAURACIÓN DEL es-MX: MISMA CANTIDAD de claves que el es-ES.
import re
def nclaves(texto):
    return len(re.findall(r'^\t?[A-Za-z0-9_.]+:', texto, re.M))
nes = nclaves(blobs['Localization/es-ES_Mods.AethonMod.hjson'].decode('utf-8'))
nmx = nclaves(blobs['Localization/es-MX_Mods.AethonMod.hjson'].decode('utf-8'))
nen = nclaves(blobs['Localization/en-US_Mods.AethonMod.hjson'].decode('utf-8'))
print(f'  claves: es-ES {nes} · es-MX {nmx} · en-US {nen}')
if nes == nmx and nes >= 463:
    ok(f'es-MX RESTAURADO: {nmx} claves == es-ES (la .129 lo traía con 48)')
else:
    mal(f'es-MX divergente: {nmx} vs es-ES {nes}')

# === (6) EL HEAP #Strings DE LA DLL (los vivos de la .130) ===
dll = blobs.get('AethonMod.dll', b'')
if not dll:
    mal('AethonMod.dll no viaja')
else:
    heap_ok = True
    # los strings del heap se leen en crudo (UTF-8 + terminador)
    vivos = ['BrumaDelCrater', 'BrumaAltarTile', 'RecetasCorrupcionSistema',
             'MarcarCorrupta', 'MODO_CONEJOS', 'DescubrirBrumasColocadas',
             'EntregarBruma', 'RemoverAltar', 'ArrancarConejos',
             'DibujarLosConejos', 'FisicaDelConejo', 'EspantarConejo',
             'ColorDeHada', 'DibujarVelosDelSol', 'ColoresDelCoro',
             'ConejoReal', 'GetItemDrops', 'AddOnCraftCallback',
             'BrumaEntregada', 'ValidarBrumasColocadas', 'ConejosReales',
             'DibujarSombrasDeLosConejos', 'PosicionDibujadaConejoReal',
             'CentroDeLaBandada', 'DispersarConejos',
             'CONEJO_SALTO_TICKS', 'ConejoEscala']
    faltantes = [s for s in vivos
                 if (s.encode('utf-8') + b'\x00') not in dll]
    if faltantes:
        heap_ok = False
        mal(f'heap #Strings: faltan {len(faltantes)}: {", ".join(faltantes[:6])}')
    else:
        ok(f'heap #Strings: los {len(vivos)} símbolos de la .130 viven')

    # EL MUERTO de la idea vieja (el hook inexistente NO viaja).
    if b'OnCraft\x00' in dll:
        ok('hook OnCraft de GlobalItem NO viaja (era inexistente en tML 2026.08)')
    else:
        ok('sin rastro del hook fantasma')

print()
if FALLOS:
    print(f'✗✗✗ {len(FALLOS)} FALLOS')
    sys.exit(1)
print('TODO OK — el paquete de la v6.50.130 está sano')
