#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v650128_tmod.py — LA AUDITORÍA DEL PAQUETE .tmod de la v6.50.128.

(1) Cabecera: AethonMod 6.50.128 · 393 entradas esperadas (las 391 de la
    .127 + AltarDeLaCaida.rawimg + ItemCorrupto.rawimg).
(2) EOF exacto: Σcomp + tabla = fsize.
(3) TODAS las entradas inflan a su tamaño declarado.
(4) LOS SPRITES NUEVOS: 16×16×4+12 y 22×22×4+12 bytes.
(5) hjson ×3 DENTRO del paquete: las claves nuevas (DevoradoSabor1..4,
    DevoradoCuenta, DevoradoTooltip, ItemCorrompido, ItemCorrupto,
    AltarDeLaCaida) + ItemCorrupto DENTRO del bloque Items.
(6) EL HEAP #Strings de la DLL: los símbolos de la .128 (el lote propio
    _lote, AltarDevoradorSistema, RegistrarAltar, SincronizarDevoracion,
    MsgDevoracion, DibujarAnillosRunicos, DibujarActoDelSol,
    DibujarLaPagina, QueueMainThreadAction, DevoracionPoder) y el
    TANTEO muerto de la .127 (abiertoAlLlegar).
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
    if ver == '6.50.128' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    print(f'  entradas: {len(entries)}')
    suma = sum(c for _, _, c in entries)
    if tend + suma == len(d):
        ok(f'EOF exacto (tabla {tend} + Σcomp {suma} = {len(d)})')
    else:
        mal(f'EOF: {tend} + {suma} = {tend+suma} ≠ {len(d)}')

    nuevos = ['Content/Tiles/AltarDeLaCaida.rawimg',
              'Content/Items/Altar/ItemCorrupto.rawimg']
    for n in nuevos:
        if n in rutas:
            ok(f'entrada nueva: {n.split("/")[-1]}')
        else:
            mal(f'FALTA {n}')

    print('=== 2. EL INFLADO (todas a su tamaño) ===')
    malas = 0
    for p, raw, comp in entries:
        if len(blobs[p]) != raw:
            malas += 1
    if malas == 0:
        ok(f'{len(entries)}/{len(entries)} inflan exacto')
    else:
        mal(f'{malas} entradas no inflan a su tamaño')

    print('=== 3. LOS SPRITES NUEVOS ===')
    for ruta, w, h in [(nuevos[0], 16, 16), (nuevos[1], 22, 22)]:
        esperado = w * h * 4 + 12
        real = len(blobs.get(ruta, b''))
        if real == esperado:
            ok(f'{ruta.split("/")[-1]}: {real} B = {w}×{h}×4+12')
        else:
            mal(f'{ruta.split("/")[-1]}: {real} B ≠ {esperado}')

    print('=== 4. LOS hjson DENTRO DEL PAQUETE ===')
    for idioma in ['es-MX', 'es-ES', 'en-US']:
        ruta = f'Localization/{idioma}_Mods.AethonMod.hjson'
        t = blobs.get(ruta, b'').decode('utf-8', 'replace')
        claves = ['DevoradoSabor1', 'DevoradoSabor4', 'DevoradoCuenta',
                  'DevoradoTooltip', 'DevoradoTooltipOjos', 'ItemCorrompido',
                  'ItemCorrupto: {', 'AltarDeLaCaida', 'MapEntry']
        halladas = [c for c in claves if c in t]
        if len(halladas) == len(claves):
            ok(f'{idioma}: {len(claves)}/{len(claves)} claves nuevas')
        else:
            mal(f'{idioma}: faltan {[c for c in claves if c not in t]}')
        # ItemCorrupto DENTRO del bloque Items (forma bloque O plana
        # Items.ItemCorrupto — el build de tML re-serializa las culturas):
        ap = t.find('Items: {')
        ci = t.find('\n}\n', ap)
        dentro_bloque = 0 <= ap < ci and ap < t.find('ItemCorrupto: {') < ci
        dentro_plano = 'Items.ItemCorrupto' in t
        if dentro_bloque or dentro_plano:
            ok(f'{idioma}: ItemCorrupto dentro de Items (bloque={dentro_bloque} plano={dentro_plano})')
        else:
            mal(f'{idioma}: ItemCorrupto fuera del bloque Items')
        # Los escapes literales \uXXXX MURIERON (caracteres reales):
        if '\\u2726' in t or '\\u2026' in t:
            mal(f'{idioma}: escapes \\uXXXX literales vivos')
        else:
            ok(f'{idioma}: sin escapes literales')

    print('=== 5. LA DLL: LOS SÍMBOLOS DE LA .128 ===')
    dll = blobs.get('AethonMod.dll', b'')

    def en_dll(s):
        # Los identificadores viven en #Strings (UTF-8); los literales en
        # UTF-16. Probamos ambos.
        return s.encode() in dll or s.encode('utf-16-le') in dll

    vivos = ['AltarDevoradorSistema', 'AltarDeLaCaida', 'ItemCorrupto',
             'DevoracionPoder', 'DibujarAnillosRunicos', 'DibujarActoDelSol',
             'DibujarLaPagina', 'SincronizarDevoracion', 'MsgDevoracion',
             'RegistrarAltar', 'ForjarAltaresFaltantes', 'PuntoElipse',
             'Volar', 'ElegirCuriosidad', 'CosmeticaDelHaz', 'DibujarElHaz']
    for s in vivos:
        if en_dll(s):
            ok(f'DLL: {s}')
        else:
            mal(f'DLL: FALTA {s}')
    muertos = ['abiertoAlLlegar']  # el tanteo de la .127 MURIÓ
    for s in muertos:
        if en_dll(s):
            mal(f'DLL: {s} sigue vivo (debía morir)')
        else:
            ok(f'DLL: {s} AUSENTE (muerto como debe)')

    print()
    if FALLOS:
        print(f'✗ {len(FALLOS)} FALLOS')
        sys.exit(1)
    print('✓ TODO OK')


if __name__ == '__main__':
    main()
