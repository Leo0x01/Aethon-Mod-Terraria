#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v650129_tmod.py — LA AUDITORÍA DEL PAQUETE .tmod de la v6.50.129.

(1) Cabecera: AethonMod 6.50.129 · 393 entradas esperadas (las 393 de la
    .128 — la .129 NO añade sprites: CorruptoGlobalItem y
    AlmasDelFestinProjectile son globales sin textura).
(2) EOF exacto: Σcomp + tabla = fsize.
(3) TODAS las entradas inflan a su tamaño declarado.
(4) hjson ×3 DENTRO del paquete: las claves nuevas de la corrupción de
    armas (ArmaCorrompidaAviso, ArmaDevorada, AlmaDespertada, las
    Stats y las Almas, la línea del Duende, la etiqueta Corrupto) +
    la re-voz del haz muerto (bruma).
(5) EL HEAP #Strings de la DLL: los símbolos de la .129
    (CorruptoGlobalItem, AlmasDelFestinProjectile, DevorarArma,
    CorromperArma, MsgAltarZonas, SincronizarZonas, MODO_CURIOSO,
    TICKS_CURIOSIDAD, MoverHacia, ExpPorRareza) y los muertos de la
    .128 (PonerAltar, BarrerAltaresLocales, DibujarElHaz,
    TICKS_AFK, _bocaAbierta).
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

    print('=== 1. CABECERA / ENTRADAS / EOF ===')
    if ver == '6.50.129' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    print(f'  entradas: {len(entries)}')
    if len(entries) == 393:
        ok('393 entradas (las 393 de la .128 — sin sprites nuevos)')
    else:
        mal(f'entradas: {len(entries)} (esperadas 393)')
    suma = sum(c for _, _, c in entries)
    if tend + suma == len(d):
        ok(f'EOF exacto (tabla {tend} + Σcomp {suma} = {len(d)})')
    else:
        mal(f'EOF: {tend} + {suma} = {tend+suma} ≠ {len(d)}')

    print('=== 2. INFLADO ===')
    malas = []
    for p, raw, comp in entries:
        if len(blobs[p]) != raw:
            malas.append(p)
    if not malas:
        ok(f'{len(entries)}/{len(entries)} entradas inflan a su tamaño')
    else:
        mal(f'entradas rotas: {malas[:5]}')

    print('=== 3. hjson ×3 — LAS CLAVES DE LA CORRUPCIÓN ===')
    claves_es = ['Caida.ArmaCorrompidaAviso', 'Caida.ArmaDevorada',
                 'Caida.AlmaDespertada', 'Caida.StatFuerza', 'Caida.StatPrisa',
                 'Caida.StatMirada', 'Caida.StatCorona', 'Caida.StatMordida',
                 'Caida.AlmaIgnea', 'Caida.AlmaSalvaje', 'Caida.AlmaVampira',
                 'Caida.CorazonEstelar', 'Caida.AlmaVeloz', 'Caida.PunoTitan',
                 'Items.ArmaCorruptaTag', 'Items.ArmaCorrupta1',
                 'Items.ArmaCorrupta2', 'Items.ArmaCorrupta3',
                 'Duende.ArmaCorrompida']
    for cult in ('es-ES', 'es-MX', 'en-US'):
        ruta = f'Localization/{cult}_Mods.AethonMod.hjson'
        if ruta not in blobs:
            mal(f'hjson ausente: {ruta}')
            continue
        texto = blobs[ruta].decode('utf8', 'replace')
        # El build de tML RE-SERIALIZA los hjson: las claves planas
        # anidan dentro de sus bloques (Caida.ArmaX -> \tArmaX:). El
        # NOMBRE único con dos puntos basta en cualquiera de las dos.
        faltan = [c for c in claves_es if c.split('.')[-1] + ':' not in texto]
        if not faltan:
            ok(f'{cult}: las {len(claves_es)} claves nuevas + etiquetas')
        else:
            mal(f'{cult} faltan: {faltan}')
        # LA RE-VOZ: el haz murió — la bruma vive.
        if cult.startswith('es'):
            if 'bruma del cráter PUDRE' in texto:
                ok(f'{cult}: la re-voz del haz muerto (bruma)')
            else:
                mal(f'{cult}: la re-voz bruma no llegó')
        else:
            if "crater's mist ROTS" in texto:
                ok('en-US: la re-voz del haz muerto (mist)')
            else:
                mal('en-US: la re-voz mist no llegó')

    print('=== 4. EL HEAP #Strings DE LA DLL ===')
    dll = blobs.get('AethonMod.dll')
    if dll is None:
        mal('sin DLL en el paquete')
        return
    vivos = ['CorruptoGlobalItem', 'AlmasDelFestinProjectile', 'DevorarArma',
             'CorromperArma', 'MsgAltarZonas', 'SincronizarZonas',
             'DifundirZonas', 'RecibirZonas', 'MODO_CURIOSO',
             'TICKS_CURIOSIDAD', 'MoverHacia', 'ExpPorRareza',
             'DespertarAlma', 'DevoracionExp', 'AlmaIgnea', 'PunoTitan',
             'DibujarLaBruma', 'DibujarElAliento', 'BonusDanoArmasDevoradas',
             'ArmaCorrompidaAviso', 'PlagaTiradasFallidas']
    muertos = ['PonerAltar', 'BarrerAltaresLocales', 'DibujarElHaz',
               'TICKS_AFK', '_bocaAbierta', '_altaresLocales']
    for s in vivos:
        if s.encode('utf8') in dll or s.encode('utf-16-le') in dll:
            ok(f'símbolo vivo: {s}')
        else:
            mal(f'símbolo VIVO ausente: {s}')
    for s in muertos:
        if s.encode('utf8') not in dll:
            ok(f'símbolo muerto: {s}')
        else:
            mal(f'símbolo MUERTO que aún vive: {s}')

    print()
    if FALLOS:
        print(f'✗ {len(FALLOS)} FALLOS')
        sys.exit(1)
    print('✓ AUDITORÍA v6.50.129 TODO OK')


main()
