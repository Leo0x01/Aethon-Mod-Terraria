#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v650127_tmod.py — LA AUDITORÍA DEL PAQUETE .tmod de la v6.50.127.

(1) Cabecera: AethonMod 6.50.127 · MISMAS 391 entradas que la .126
    (ningún archivo nuevo ni muerto: esta versión es SOLO código y hjson).
(2) EOF exacto: Σcomp + tabla = fsize.
(3) TODAS las entradas inflan a su tamaño declarado.
(4) hjson ×3: el paquete == el árbol, BYTE A BYTE.
(5) EL HEAP #Strings de la DLL: los vivos de la .127 (TieneMascotaLuz,
    TieneGrimorioMenor, NoTownNPCHappiness, el prefijo «GrimorioMenor»,
    la lista «Caida.Dormido») y el muerto de la .126 (TieneMascota).
"""
import hashlib
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = sys.argv[1] if len(sys.argv) > 1 else \
    '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
VIEJO = sys.argv[2] if len(sys.argv) > 2 else \
    '/home/sync/AethonMod-v6.50.126.tmod'
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
    if ver == '6.50.127' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')

    _, _, vver, ventries, _, _, _ = inflar(VIEJO)
    vrutas = {p for p, _, _ in ventries}
    if vver != '6.50.126':
        print(f'  (aviso: el respaldo viejo es {vver})')
    if rutas == vrutas and len(entries) == len(ventries):
        ok(f'MISMAS {len(entries)} entradas que la .126 '
           f'(ni nuevas ni muertas — solo código y hjson cambiados)')
    else:
        solo127 = rutas - vrutas
        solo126 = vrutas - rutas
        mal(f'conjuntos distintos: +{sorted(solo127)} −{sorted(solo126)} '
            f'({len(entries)} vs {len(ventries)})')

    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. TODAS INFLAN ===')
    malas = []
    for p, raw, comp in entries:
        if len(blobs[p]) != raw:
            malas.append((p, raw, len(blobs[p])))
    if malas:
        for p, raw, got in malas[:8]:
            mal(f'{p}: declarado {raw}, inflado {got}')
    else:
        ok(f'{len(entries)}/{len(entries)} entradas inflan a su tamaño declarado')
    planas = sum(1 for p, raw, comp in entries if raw == comp)
    ok(f'{planas} entradas van planas (sin compresión)')

    print('=== 3. hjson ×3 — PAQUETE == ÁRBOL, BYTE A BYTE ===')
    for idioma in ('es-ES', 'es-MX', 'en-US'):
        ruta_paq = f'Localization/{idioma}_Mods.AethonMod.hjson'
        ruta_fuente = f'{ARBOL}/Localization/{idioma}_Mods.AethonMod.hjson'
        a = blobs.get(ruta_paq)
        b = open(ruta_fuente, 'rb').read()
        if a is None:
            mal(f'{ruta_paq} NO ESTÁ en el paquete')
        elif a == b:
            ok(f'{idioma}: paquete == fuente ({len(b)} B)')
        else:
            mal(f'{idioma}: paquete != fuente ({len(a) if a else 0} vs {len(b)} B)')

    print('=== 4. EL HEAP #Strings — VIVOS .127 / MUERTOS .126 ===')
    dll = blobs['AethonMod.dll']

    def en_heap(s):
        return s.encode('utf-8') + b'\x00' in dll

    def en_literal(s):
        # los ldstr viven en el heap #US — UTF-16LE (lección de la .127)
        return s.encode('utf-16-le') in dll

    vivos = [
        'TieneMascotaLuz',          # chismes: la cría de luz, partida
        'TieneGrimorioMenor',       # chismes: el Menor, SU buff
        'NoTownNPCHappiness',       # dormido: el botón de felicidad APAGADO
        'SeleccionGrimorioSistema', # el vagabundo (regresión .126)
        'ParejaCerebro',            # el cerebro (regresión)
        'ArrancarPlaga',            # la plaga (regresión)
        'DibujarLaHorda',           # la horda (regresión)
        'PreDrawVistaPrevista',     # la vista previa (regresión)
        'DibujarSobreElMenu',       # el hook del vagabundo (regresión)
    ]
    literales = [
        'GrimorioMenor',                 # el prefijo del hjson (ldstr)
        'Mods.AethonMod.Caida.Dormido',  # la lista exclusiva del dormido (ldstr)
        'Mods.AethonMod.Pueblo.Chismes.',  # el chisme compuesto (regresión)
    ]
    muertos = [
        'TieneMascota',             # la fusión de la .126 (chismes)
    ]
    for s in vivos:
        if en_heap(s):
            ok(f'VIVO en el heap: {s}')
        else:
            mal(f'FALTA en el heap: {s}')
    for s in literales:
        if en_literal(s):
            ok(f'VIVO en el heap #US (ldstr): {s}')
        else:
            mal(f'FALTA en el heap #US: {s}')
    for s in muertos:
        if en_heap(s):
            mal(f'el muerto VIVE en el heap: {s}')
        else:
            ok(f'muerto ausente: {s}')

    print(f'\n{len(FALLOS)} ✗')
    if FALLOS:
        for f in FALLOS:
            print('  -', f)
        sys.exit(1)
    print('AUDIT .tmod v6.50.127 TODO OK')


if __name__ == '__main__':
    main()
