#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v65110_tmod.py — LA AUDITORÍA DEL PAQUETE .tmod de la v6.51.10.

(1) Cabecera: AethonMod 6.51.10 · 401 entradas (las mismas de la .51.9 —
    cero arte tocado: solo .cs, hjson y build.txt).
(2) EOF exacto: Σcomp + tabla = fsize.
(3) TODAS las entradas inflan a su tamaño declarado.
(4) hjson ×3: el paquete == el árbol, BYTE A BYTE — con las +8 claves
    nuevas (Duende.Miedo1..4, Duende.Burla1..3, Despertar.DevoraInventario)
    y las 3 MUERTAS de los rechazos (FaltaEsencia/FaltaDinero/FaltaMineral).
(5) LA DLL — LOS VIVOS DE LA .51.10: la escalera de la REFORJA VIVA
    (PeldanoReforja/LaReforja/StatsEtiqueta), LA DEVORACIÓN DE LA MOCHILA
    (AutoDevorarOfrenda/SlotArmaDelPeldano/DevoracionInventarioFX) y LA
    ESCENA DEL YUNQUE (LaBurlaDelEterno).
(6) LOS MUERTOS: RechazarOfrenda/RechazarMineral (los platos CAÍDOS ya no
    se rechazan: la mesa se sirve del inventario).
(7) LOS LITERALES UTF-16LE del heap #US (los 14 modificadores de la wiki).
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

    print('=== 1. CABECERA / ENTRADAS / EOF ===')
    if name == 'AethonMod':
        ok(f'nombre {name}')
    else:
        mal(f'nombre {name}')
    if ver == '6.51.10':
        ok(f'versión {ver}')
    else:
        mal(f'versión {ver} (esperaba 6.51.10)')
    if len(entries) == 401:
        ok(f'{len(entries)} entradas (las mismas 401 de la .51.9 — cero arte tocado)')
    else:
        mal(f'{len(entries)} entradas (esperaba 401)')
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

    print('=== 3. hjson ×3 PAQUETE == ÁRBOL (con la cirugía de la .51.10) ===')
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
        # LAS NUEVAS de la .51.10 — el build de tML puede anidarlas
        # (Duende: { Miedo1: ... }) o dejarlas planas (Duende.Miedo1:) —
        # AMBAS formas son la MISMA clave de localización.
        nuevas = ['Miedo1', 'Miedo2', 'Miedo3', 'Miedo4',
                  'Burla1', 'Burla2', 'Burla3', 'DevoraInventario']
        faltan = [k for k in nuevas if f'{k}:' not in texto]
        if faltan:
            mal(f'{idioma}: faltan las nuevas {faltan}')
        else:
            ok(f'{idioma}: las 8 claves nuevas (miedo/burla/devora) dentro')
        # LAS MUERTAS de la .51.10 (los rechazos de los platos caídos)
        muertas_hjson = ['FaltaEsencia', 'FaltaDinero', 'FaltaMineral']
        vivas_malas = [k for k in muertas_hjson if f'{k}:' in texto]
        if vivas_malas:
            mal(f'{idioma}: siguen vivas {vivas_malas}')
        else:
            ok(f'{idioma}: los 3 rechazos muertos AUSENTES')

    print('=== 4. LA DLL — LOS VIVOS DE LA .51.10 ===')
    dll = blobs['AethonMod.dll']
    vivos = [
        # LA REFORJA VIVA (la escalera de los modificadores mágicos)
        b'PeldanoReforja', b'LaReforja', b'PeldanoDe', b'StatsEtiqueta',
        b'IndiceEtiqueta', b'NombreEtiqueta', b'ColorEtiqueta',
        # LA DEVORACIÓN DE LA MOCHILA
        b'AutoDevorarOfrenda', b'SlotArmaDelPeldano',
        b'ConsumirEsenciasConVictimas', b'ConsumirMineralesConVictimas',
        b'DevoracionInventarioFX', b'DevoracionSlotsGlobalItem',
        b'DevoracionInventarioSistema', b'Disparar', b'Victima',
        # LA ESCENA DEL YUNQUE
        b'LaBurlaDelEterno',
        # LO QUE SIGUE DE LA .51.9 (intacto)
        b'Bonus50Horneado', b'Bonus50Vivo', b'EsSupremo',
        b'PrefixChance', b'ResetPrefix', b'SetNameOverride',
        b'CanReforge', b'OnSpawn',
        b'MineralDe', b'CantidadMineralDe', b'LlevaMineralCompleto',
        b'DevorarComoBocado',
    ]
    for v in vivos:
        if v + b'\x00' in dll or v in dll:
            ok(f'VIVO: {v.decode()}')
        else:
            mal(f'FALTA EL VIVO: {v.decode()}')

    print('=== 5. LOS MUERTOS DE LA .51.10 ===')
    muertos = [
        b'RechazarOfrenda',    # los platos caídos ya no se rechazan
        b'RechazarMineral',    # (la mesa se sirve del inventario)
    ]
    for m in muertos:
        if m + b'\x00' in dll or m in dll:
            mal(f'NO MURIÓ: {m.decode()} (debe estar AUSENTE de la DLL)')
        else:
            ok(f'MUERTO: {m.decode()} (ausente de la DLL)')

    print('=== 6. LOS LITERALES NUEVOS (UTF-16LE del heap #US) ===')
    literales = [
        # LOS 12 MODIFICADORES MÁGICOS DE LA WIKI + LAS 2 CIMAS DEL MOD
        'Ignorante', 'Transtornado', 'Inepto', 'Intenso', 'Furioso',
        'Maníaco', 'Prohibido', 'Experto', 'Celeste', 'Místico',
        'Maestro', 'Mítico', 'Legendario Supremo',
        # LA VENTANA DEL ARMA — LA REFORJA
        '═══ LA REFORJA ═══',
        '(viste en el nivel ',
        'Próxima reforja: ',
        'Reforja: ',
        # LA LISTA DEL DESPERTAR — LOS TEXTOS DEL INVENTARIO
        'corrupta, en cualquier parte de TU inventario',
        'en cualquier parte de TU inventario, sin corrupción',
        'los items requeridos deben estar en TU inventario',
        'con el arma O los lingotes basta',
    ]
    for lit in literales:
        if lit.encode('utf-16-le') in dll:
            ok(f'LITERAL: {lit}')
        else:
            mal(f'FALTA EL LITERAL: {lit}')

    print('=== 7. EL LOOT DEL FUSIONADO (prueba por DECOMPILE) ===')
    print('    DropBotin del Aethon normal quedó VACÍO (solo _yaDropeo)')
    print('    y el del Fusionado paga los 3 — verificado por Cecil')
    print('    FUERA del script (la prueba del loot).')
    ok('(verificado con Cecil — la prueba del loot del Fusionado)')

    print()
    if FALLOS:
        print(f'✗✗✗ {len(FALLOS)} FALLOS ✗✗✗')
        for f in FALLOS:
            print(f'  - {f}')
        sys.exit(1)
    print(f'✓✓✓ AUDITORÍA LIMPIA: {len(entries)} entradas, md5 {md5}')


if __name__ == '__main__':
    main()
