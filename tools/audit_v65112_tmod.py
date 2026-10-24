#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v65112_tmod.py — LA AUDITORÍA DEL PAQUETE .tmod de la v6.51.12.

(1) Cabecera: AethonMod 6.51.12 · 401 entradas (las mismas de la .51.11:
    los 4 .cs nuevos no viajan — BocadoDelSueloProjectile, EscuadronUtil,
    SombraDeLaPagina2, SombraPagina2Projectile — y ningún .png nuevo:
    Sombra 2 reutiliza el sprite del tomo 1 por override de Texture).
(2) EOF exacto: Σcomp + tabla = fsize.
(3) TODAS las entradas inflan a su tamaño declarado.
(4) hjson ×3: el paquete == el árbol, BYTE A BYTE + las claves nuevas de
    LA PRESENCIA DEL LIBRO (EsenciaLigada*, LazoDevuelto, SombraPagina2).
(5) LA DLL — LOS VIVOS DE LA .51.12 (FactorDelLibro, el LAZO, el
    ESCUADRÓN que no muere, el FESTÍN CHICO, el CONCIERTO DE LAS 2 HORAS,
    LOS RAYITOS PERLIN, LA SOMBRA II con su hélice y sus iris reales).
(6) LOS MUERTOS (regresión: la devoración de la mochila sigue muerta).
(7) LOS LITERALES UTF-16LE del heap #US.
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
    # LA PRESENCIA DEL LIBRO (el factor 100/25/0)
    'FactorDelLibro',
    # EL LAZO DE LA SOMBRA DE LA PÁGINA (anti-caída)
    'DevolverAlInventario', 'LazoUmbral', 'LazoVelocidad',
    # EL ESCUADRÓN QUE NO MUERE (la cura de la desinvocación)
    'EscuadronUtil', 'Sostener', 'Despedir',
    # EL FESTÍN CHICO (la animación de devorar los platos)
    'BocadoDelSueloProjectile', 'AnimarElBocado', 'DibujarElFantasma',
    'SendExtraAI',
    # EL CONCIERTO DE LAS DOS HORAS (las hadas)
    'TicksSinCoro', 'CoroTicks', 'CoroQuieto',
    # LOS RAYITOS PERLIN DE LOS MINIMINIONS
    'DibujarRayitos', 'RayitoHalo',
    # LA SOMBRA DE LA PÁGINA II (la hélice, los iris, las garras)
    'SombraDeLaPagina2', 'SombraPagina2Projectile', 'Hebra', 'Espina',
    'DibujarLosIrisReales', 'DibujarOjoReal', 'CicloMasticar',
    # LA PARIDAD DEL DAÑO DE INVOCACIÓN (misma fórmula del mágico)
    'DanoOjosDelEterno', 'MagicDamageMult',
    # LO QUE SIGUE VIVO DE LA .51.11 (regresión)
    'ServirLaMesa', 'LaMesaEstaCompleta', 'EscupirCambio',
    'EsenciasConsumidas', 'FactorVelocidadTotal', 'MinTicksUso',
    'ColorDelLibro', 'ReforjaBasica',
]
MUERTOS = [
    # REGRESIÓN: la devoración de la mochila sigue muerta
    'AutoDevorarOfrenda', 'DevoracionInventarioFX',
    'ConsumirEsenciasConVictimas', 'ConsumirMineralesConVictimas',
    'SlotArmaDelPeldano', 'ContarMonedas', 'EsenciasEnManos',
]
CLAVES_NUEVAS = [
    'EsenciaLigadaMano', 'EsenciaLigadaBolsa', 'EsenciaLigadaNada',
    'LazoDevuelto', 'SombraPagina2',
]
CLAVES_REESCRITAS = [
    'EsenciaTopeTooltip', 'EsenciaProgresoTooltip',
]


def main():
    d = open(TMOD, 'rb').read()
    md5 = hashlib.md5(d).hexdigest()
    print(f'=== {TMOD} ===')
    print(f'  {len(d)} B | md5 {md5}')

    tmlver, name, ver, entries, tend, fsize, blobs = inflar(TMOD)

    print('=== 1. CABECERA / ENTRADAS / EOF ===')
    ok(f'nombre {name}') if name == 'AethonMod' else mal(f'nombre {name}')
    ok(f'versión {ver}') if ver == '6.51.12' else mal(f'versión {ver} (esperaba 6.51.12)')
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

    print('=== 3. hjson ×3 PAQUETE == ÁRBOL + CLAVES .51.12 ===')
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
        faltan = [k for k in CLAVES_NUEVAS if k + ':' not in texto and k + ': {' not in texto]
        if not faltan:
            ok(f'{clave}: las 5 claves nuevas dentro')
        else:
            mal(f'{clave}: faltan {faltan}')
        faltan2 = [k for k in CLAVES_REESCRITAS if k + ':' not in texto]
        if not faltan2:
            ok(f'{clave}: las 2 reescritas presentes')
        else:
            mal(f'{clave}: faltan {faltan2}')

    print('=== 4. LA DLL — LOS VIVOS ===')
    dll = blobs.get('AethonMod.dll')
    if dll is None:
        mal('AethonMod.dll NO viaja')
        return
    heap = dll
    for sim in VIVOS:
        if sim.encode('utf-8') in heap or sim.encode('utf-16-le') in heap:
            ok(f'VIVO: {sim}')
        else:
            mal(f'VIVO AUSENTE: {sim}')

    print('=== 5. LA DLL — LOS MUERTOS (regresión) ===')
    for sim in MUERTOS:
        if sim.encode('utf-8') in heap or sim.encode('utf-16-le') in heap:
            mal(f'MUERTO PRESENTE: {sim}')
        else:
            ok(f'muerto ausente: {sim}')

    print('=== 6. LITERALES #US (los avisos) ===')
    for lit in ['EsenciaProgresoTooltip', 'LazoDevuelto', 'MesaDelEterno', 'EsenciaLigadaNada']:
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
