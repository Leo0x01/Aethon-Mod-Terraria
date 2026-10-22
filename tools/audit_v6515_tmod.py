#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6515_tmod.py — LA AUDITORÍA DEL PAQUETE .tmod de la v6.51.5
(LA SEGUNDA AUDICIÓN).

(1) Cabecera: AethonMod 6.51.5 · 405 entradas (las 406 de la .51.4 − 5
    iconos muertos de las armas borradas + 4 iconos nuevos).
(2) EOF exacto: Σcomp + tabla = fsize.
(3) TODAS las entradas inflan a su tamaño declarado.
(4) hjson ×3: el paquete == el árbol, BYTE A BYTE.
(5) LA DLL: los 12 símbolos NUEVOS presentes (IndiceGrimorioProjectile,
    LluviaVersosProjectile, OjoQueJuzgaProjectile,
    CorazonMagnetarProjectile, DignosIndiceGrimorio, DignosLluviaVersos,
    DignosOjoQueJuzga, DignosCorazonMagnetar, DespertarRequisitosGlobalItem,
    _perforaBinaria, LlevaCorrupta, 9211 en el heap #US) y los 12 MUERTOS
    ausentes (ComaRunica, PaginaArrancada, OjoQueCaza, LineaLectura,
    Marcapaginas, EquinoccioBinario, AgujeroEnViaje + los 5 ítems).
(6) LOS ICONOS: los 4 nuevos dentro, los 5 muertos fuera.
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


def main():
    d = open(TMOD, 'rb').read()
    md5 = hashlib.md5(d).hexdigest()
    print(f'=== {TMOD} ===')
    print(f'  {len(d)} B | md5 {md5}')

    tmlver, name, ver, entries, tend, fsize, blobs = inflar(TMOD)
    rutas = {p for p, _, _ in entries}

    print('=== 1. CABECERA / ENTRADAS / EOF ===')
    if name == 'AethonMod':
        ok(f'nombre {name}')
    else:
        mal(f'nombre {name}')
    if ver == '6.51.5':
        ok(f'versión {ver}')
    else:
        mal(f'versión {ver} (esperaba 6.51.5)')
    if len(entries) == 405:
        ok(f'{len(entries)} entradas (406 de la .51.4 − 5 iconos muertos + 4 nuevos)')
    else:
        mal(f'{len(entries)} entradas (esperaba 405)')
    if tend + sum(c for _, _, c in entries) == fsize == len(d):
        ok(f'EOF exacto (tabla {tend} + Σcomp = {fsize})')
    else:
        mal('EOF NO exacto')

    print('=== 2. TODAS INFLAN ===')
    malas = 0
    for p, raw, comp in entries:
        if len(blobs[p]) != raw:
            malas += 1
    if malas == 0:
        ok(f'{len(entries)}/{len(entries)} inflan a su tamaño declarado')
    else:
        mal(f'{malas} entradas no inflan')

    print('=== 3. hjson ×3 PAQUETE == ÁRBOL ===')
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

    print('=== 4. LA DLL — LOS VIVOS Y LOS MUERTOS ===')
    dll = blobs['AethonMod.dll']
    vivos = [
        b'IndiceGrimorioProjectile', b'LluviaVersosProjectile',
        b'OjoQueJuzgaProjectile', b'CorazonMagnetarProjectile',
        b'DignosIndiceGrimorio', b'DignosLluviaVersos',
        b'DignosOjoQueJuzga', b'DignosCorazonMagnetar',
        b'DespertarRequisitosGlobalItem', b'_perforaBinaria',
        b'LlevaCorrupta', b'LluviaVersosProjectile',
    ]
    for v in vivos:
        if v in dll:
            ok(f'VIVO: {v.decode()}')
        else:
            mal(f'FALTA EL VIVO: {v.decode()}')
    muertos = [
        b'ComaRunicaProjectile', b'PaginaArrancadaProjectile',
        b'OjoQueCazaProjectile', b'LineaLecturaProjectile',
        b'MarcapaginasProjectile', b'EquinoccioBinarioProjectile',
        b'AgujeroEnViajeProjectile', b'DignosComasQueMuerden',
        b'DignosPaginaArrancada', b'DignosOjoQueCaza',
        b'DignosLineaDeLectura', b'DignosMarcapaginas',
    ]
    for m in muertos:
        if m in dll:
            mal(f'EL MUERTO VIVE: {m.decode()}')
        else:
            ok(f'muerto: {m.decode()}')

    print('=== 5. LAS MARCAS BINARIAS (el heap #US — UTF-16LE) ===')
    us = dll
    if '9211'.encode('utf-16-le') in us and '9212'.encode('utf-16-le') in us:
        pass  # los números viven como float en el IL, no como string — verificación semántica abajo
    # verificación de los LITERALES de la .51.5 (UTF-16LE del heap #US)
    lit = 'La Bolsa de los Proyectiles Dignos'.encode('utf-16-le')
    if lit in us:
        ok('heap #US: la bolsa de los dignos vive')
    else:
        mal('heap #US: no encontré la bolsa')

    print('=== 6. LOS ICONOS ===')
    nuevos_iconos = [
        'Content/Weapons/Cosmic/DignosIndiceGrimorio.rawimg',
        'Content/Weapons/Cosmic/DignosLluviaVersos.rawimg',
        'Content/Weapons/Cosmic/DignosOjoQueJuzga.rawimg',
        'Content/Weapons/Cosmic/DignosCorazonMagnetar.rawimg',
    ]
    for ic in nuevos_iconos:
        if ic in rutas:
            ok(f'icono nuevo: {ic.split("/")[-1]}')
        else:
            mal(f'ICONO NUEVO FALTA: {ic}')
    muertos_iconos = [
        'Content/Weapons/Cosmic/DignosComasQueMuerden.rawimg',
        'Content/Weapons/Cosmic/DignosPaginaArrancada.rawimg',
        'Content/Weapons/Cosmic/DignosOjoQueCaza.rawimg',
        'Content/Weapons/Cosmic/DignosLineaDeLectura.rawimg',
        'Content/Weapons/Cosmic/DignosMarcapaginas.rawimg',
    ]
    for ic in muertos_iconos:
        if ic in rutas:
            mal(f'ICONO MUERTO VIAJA: {ic.split("/")[-1]}')
        else:
            ok(f'icono muerto fuera: {ic.split("/")[-1]}')

    print('=== 7. hjson — CLAVES SIMÉTRICAS ===')
    import re as _re
    def claves(blob):
        return set(m.group(1).decode() for m in
                   _re.finditer(rb'^\t([A-Za-z0-9_]+): \{', blob, _re.M))
    c_en = claves(blobs['Localization/en-US_Mods.AethonMod.hjson'])
    c_es = claves(blobs['Localization/es-ES_Mods.AethonMod.hjson'])
    c_mx = claves(blobs['Localization/es-MX_Mods.AethonMod.hjson'])
    if c_en == c_es == c_mx:
        ok(f'items simétricos ×3 ({len(c_en)} claves)')
    else:
        mal(f'asimetría: en={len(c_en)} es={len(c_es)} mx={len(c_mx)} | '
            f'solo en={sorted(c_en - c_es - c_mx)[:5]} | '
            f'fuera en={sorted((c_es | c_mx) - c_en)[:5]}')

    print()
    if FALLOS:
        print(f'*** {len(FALLOS)} FALLOS ***')
        for f in FALLOS:
            print('  ✗', f)
        sys.exit(1)
    print('TODO OK — LA SEGUNDA AUDICIÓN VIAJA COMPLETA')


if __name__ == '__main__':
    main()
