#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v65111_tmod.py — LA AUDITORÍA DEL PAQUETE .tmod de la v6.51.11.

(1) Cabecera: AethonMod 6.51.11 · 401 entradas (las mismas de la .51.10:
    murió un .cs — DevoracionInventarioFX — pero los .cs no viajan: solo
    dll/pdb/Info/build.txt/LICENSE/png/rawimg/hjson/fx).
(2) EOF exacto: Σcomp + tabla = fsize.
(3) TODAS las entradas inflan a su tamaño declarado.
(4) hjson ×3: el paquete == el árbol, BYTE A BYTE + las 8 claves nuevas
    de LA MESA DEL SUELO + EL FESTÍN DEL PORTADOR dentro + las 6 muertas
    fuera + Migracion en Despertar.
(5) LA DLL — LOS VIVOS DE LA .51.11 (símbolos nuevos por nombre exacto
    del heap de metadatos).
(6) LOS MUERTOS de la .51.10→.51.11 (AutoDevorarOfrenda, la FX de la
    mochila, los contadores de inventario).
(7) LOS LITERALES UTF-16LE del heap #US (los anuncios de los bocados).
"""
import hashlib
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = sys.argv[1] if len(sys.argv) > 1 else \
    '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
ARBOL = 'AethonMod'

FALLOS = []


def ok(msg):
    print(f'  ✓ {msg}')


def mal(msg):
    print(f'  ✗ {msg}')
    FALLOS.append(msg)


def inflar(path):
    d = open(path, 'rb').read()
    tmlver, name, ver, entries, tend, fsize = parse(path)
    blobs, off = {}, tend
    for p, raw, comp in entries:
        data = d[off:off + comp]
        off += comp
        blobs[p] = zlib.decompress(data, -15) if raw != comp else data
    return tmlver, name, ver, entries, tend, fsize, blobs


VIVOS = [
    # LA MESA DEL SUELO
    'ServirLaMesa', 'LaMesaEstaCompleta', 'EscupirCambio', 'MigrarViejoPoder',
    'SincronizarDrop', 'PolvosDelBocado', 'EsDelPeldano', 'ValorDeMoneda',
    # LA MESA EN EL ALMA (contadores parciales)
    'OfrendaEsencias', 'OfrendaMonedas', 'OfrendaMineral', 'OfrendaArma',
    # EL FESTÍN DEL PORTADOR
    'EsenciasConsumidas', 'EsenciasTope', 'BonusDanoEsencias',
    # EL TOPE DE VELOCIDAD
    'FactorVelocidadTotal', 'MinTicksUso',
    # EL COLOR DEL NOMBRE (la rareza viste el peldaño)
    'ColorDelLibro',
    # LA REFORJA EN LA VENTANA BÁSICA
    'ReforjaBasica',
]
MUERTOS = [
    # LA DEVORACIÓN DE LA MOCHILA (la .51.10 — el bug de la cadena)
    'AutoDevorarOfrenda', 'DevoracionInventarioFX',
    'ConsumirEsenciasConVictimas', 'ConsumirMineralesConVictimas',
    'SlotArmaDelPeldano',
    # LOS CONTADORES DE INVENTARIO (la mochila es sagrada)
    'ContarMonedas', 'EsenciasEnManos', 'MineralesEnManos',
    'LlevaCorrupta', 'LlevaMineralCompleto', 'NombreOfrenda',
]
CLAVES_NUEVAS = [
    'BocadoEsencias', 'BocadoMonedas', 'BocadoMineral', 'BocadoArma',
    'MigracionPoder', 'TopeEsencias', 'EsenciaTopeTooltip',
    'EsenciaProgresoTooltip',
]
CLAVES_MUERTAS = [
    'EsenciaDevorada:', 'DevoraInventario:', 'DevoradoTooltipOjos:',
    'DevoradoCuenta:', 'ArmaCorrompidaAviso:',
]


def main():
    d = open(TMOD, 'rb').read()
    md5 = hashlib.md5(d).hexdigest()
    print(f'=== {TMOD} ===')
    print(f'  {len(d)} B | md5 {md5}')

    tmlver, name, ver, entries, tend, fsize, blobs = inflar(TMOD)

    print('=== 1. CABECERA / ENTRADAS / EOF ===')
    ok(f'nombre {name}') if name == 'AethonMod' else mal(f'nombre {name}')
    ok(f'versión {ver}') if ver == '6.51.11' else mal(f'versión {ver} (esperaba 6.51.11)')
    ok(f'{len(entries)} entradas') if len(entries) == 401 else mal(f'{len(entries)} entradas (esperaba 401)')
    if tend + sum(c for _, _, c in entries) == fsize == len(d):
        ok(f'EOF exacto (tabla {tend} + Σcomp = {fsize})')
    else:
        mal('EOF NO exacto')

    print('=== 2. TODAS INFLAN ===')
    malas = sum(1 for p, raw, comp in entries if len(blobs[p]) != raw)
    if malas == 0:
        ok(f'{len(entries)}/{len(entries)} inflan a su tamaño declarado')
    else:
        mal(f'{malas} entradas no inflan')

    print('=== 3. hjson ×3 PAQUETE == ÁRBOL + CLAVES .51.11 ===')
    for idioma in ['en-US', 'es-ES', 'es-MX']:
        clave = f'Localization/{idioma}_Mods.AethonMod.hjson'
        ruta = f'{ARBOL}/Localization/{idioma}_Mods.AethonMod.hjson'
        if clave not in blobs:
            mal(f'{clave} NO viaja')
            continue
        fuente = open(ruta, 'rb').read()
        if blobs[clave] == fuente:
            ok(f'{clave} == fuente ({len(fuente)} B byte a byte)')
        else:
            mal(f'{clave} DIFIERE del fuente')
        texto = blobs[clave].decode('utf-8')
        faltan = [k for k in CLAVES_NUEVAS if k + ':' not in texto]
        if not faltan:
            ok(f'{clave}: las 8 claves nuevas dentro')
        else:
            mal(f'{clave}: faltan {faltan}')
        vivas_muertas = [k for k in CLAVES_MUERTAS if '\t' + k in texto]
        if not vivas_muertas:
            ok(f'{clave}: las 5 muertas fuera')
        else:
            mal(f'{clave}: aún viven {vivas_muertas}')
        if '\tMigracion:' in texto.split('\nDespertar: {')[1]:
            ok(f'{clave}: Migracion vive en Despertar')
        else:
            mal(f'{clave}: Migracion NO está en Despertar')

    print('=== 4. LA DLL — LOS VIVOS ===')
    dll = blobs.get('AethonMod.dll')
    if dll is None:
        mal('AethonMod.dll NO viaja')
        return
    # heap de metadatos: nombres por longitud + UTF-16LE
    heap = dll
    texto_us = dll.decode('utf-16-le', 'ignore')
    for sim in VIVOS:
        if sim.encode('utf-8') in heap or sim.encode('utf-16-le') in heap:
            ok(f'VIVO: {sim}')
        else:
            mal(f'VIVO AUSENTE: {sim}')

    print('=== 5. LA DLL — LOS MUERTOS ===')
    for sim in MUERTOS:
        if sim.encode('utf-8') in heap or sim.encode('utf-16-le') in heap:
            mal(f'MUERTO PRESENTE: {sim}')
        else:
            ok(f'muerto ausente: {sim}')

    print('=== 6. LITERALES #US (los anuncios) ===')
    for lit in ['DevoradoTooltip', 'TopeEsencias', 'BocadoEsencias', 'Reforja (']:
        if lit.encode('utf-16-le') in heap:
            ok(f'literal: {lit}')
        else:
            mal(f'literal ausente: {lit}')

    print()
    if FALLOS:
        print(f'✗✗ {len(FALLOS)} FALLOS')
        sys.exit(1)
    print('✓✓ AUDITORÍA LIMPIA')


if __name__ == '__main__':
    main()
