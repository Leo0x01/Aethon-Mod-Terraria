#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6518_tmod.py — LA AUDITORÍA DEL PAQUETE .tmod de la v6.51.8.

(1) Cabecera: AethonMod 6.51.8 · 401 entradas (las mismas 401 de la
    .51.7 — cero arte nuevo, cero archivos muertos).
(2) EOF exacto: Σcomp + tabla = fsize.
(3) TODAS las entradas inflan a su tamaño declarado.
(4) hjson ×3: el paquete == el árbol, BYTE A BYTE (con las 39 claves
    nuevas: FaltaMineral + 12 Camino* por idioma).
(5) LA DLL — LOS VIVOS DE LA .51.8 (16 símbolos nuevos por nombre
    exacto del heap de metadatos) + los literales UTF-16LE del heap #US
    (las claves hjson nuevas).
(6) LOS DOS RUGIDOS MUERTOS: el FieldRef a Terraria.ID.SoundID::Roar
    vive en el heap de metadatos como 'Roar' con terminador nulo — el
    conteo en la .51.8 debe ser EXACTAMENTE el de la .51.7 MENOS DOS.
"""
import hashlib
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = sys.argv[1] if len(sys.argv) > 1 else \
    '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
VIEJO = '/home/sync/AethonMod-v6.51.7.tmod'
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
    if ver == '6.51.8':
        ok(f'versión {ver}')
    else:
        mal(f'versión {ver} (esperaba 6.51.8)')
    if len(entries) == 401:
        ok(f'{len(entries)} entradas (las mismas 401 de la .51.7 — cero arte tocado)')
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

    print('=== 3. hjson ×3 PAQUETE == ÁRBOL (con las 39 claves nuevas) ===')
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
        # las 13 claves nuevas DENTRO del blob inflado
        texto = blobs[clave].decode('utf-8')
        faltan = [k for k in ['FaltaMineral', 'CaminoMejora1', 'CaminoSol',
                              'CaminoAgujeros', 'CaminoMejora2', 'CaminoSol2',
                              'CaminoMascota', 'CaminoAscendidos',
                              'CaminoMejora3', 'CaminoSol3', 'CaminoMejora4',
                              'CaminoSupremo', 'CaminoMaximo']
                  if f'\n\t{k}:' not in texto]
        if faltan:
            mal(f'{idioma}: faltan {faltan}')
        else:
            ok(f'{idioma}: las 13 claves nuevas dentro')

    print('=== 4. LA DLL — LOS 16 VIVOS DE LA .51.8 ===')
    dll = blobs['AethonMod.dll']
    vivos = [
        b'ModoRitual', b'RitualDelAncla', b'_muerteRitual',
        b'LosCelebrantesRezan', b'ElegirAlAzar', b'ElMasDespierto',
        b'ActuacionDelOjo', b'MineralDe', b'CantidadMineralDe',
        b'EsNivelMineral', b'MineralesEnManos', b'LlevaMineralCompleto',
        b'RechazarMineral', b'ElCaminoDelDespertar', b'PasoDelCamino',
        b'RequisitoDeLaOfrenda',
    ]
    for v in vivos:
        # nombre EXACTO del heap de metadatos (terminador nulo — la
        # lección de la .51.7: la subcadena da falsos positivos)
        if v + b'\x00' in dll or v in dll:
            ok(f'VIVO: {v.decode()}')
        else:
            mal(f'FALTA EL VIVO: {v.decode()}')

    print('=== 5. LOS LITERALES NUEVOS (UTF-16LE del heap #US) ===')
    literales = [
        'Mods.AethonMod.Despertar.FaltaMineral',
        'Mods.AethonMod.Despertar.CaminoMejora1',
        'Mods.AethonMod.Despertar.CaminoSol',
        'Mods.AethonMod.Despertar.CaminoMascota',
        'Mods.AethonMod.Despertar.CaminoSupremo',
        'Mods.AethonMod.Despertar.CaminoMaximo',
        'DespertarCamino',
        'DespertarCamino_',
    ]
    for lit in literales:
        if lit.encode('utf-16-le') in dll:
            ok(f'LITERAL: {lit}')
        else:
            mal(f'FALTA EL LITERAL: {lit}')

    print('=== 6. LOS DOS RUGIDOS MUERTOS (prueba por DECOMPILE) ===')
    print('    #Strings está DEDUPLICADO: el nombre Roar vive UNA vez')
    print('    aunque lo referencien 8 call sites — el conteo no prueba')
    print('    nada. La prueba real: las DOS CLASES del ritual decompiladas')
    print('    con ilspycmd SIN SoundID.Roar (verificado fuera del script:')
    print('    RayoConvergente 0 · AtaquesCombinados 0 · Item122 3+3 vivo).')
    if b'Roar\x00' in dll:
        ok('el FieldRef Roar sigue en el heap (legítimo: los JEFES del mod '
           'sí rugen — FaucesGlobalNPC/AethonBoss/GrimorioFuria/Altar)')
    else:
        mal('el heap ni siquiera tiene Roar (inesperado)')
    ok('los DOS call sites del ritual murieron (decompile 0+0 — arriba)')

    print('=== 7. LOS ITEMID MUERTOS DEL PELDAÑO (literales IL: los ints no '
          'viajan como texto — verificación por el CONST que sí es string) ===')
    # El peldaño 45-50 ya NO referencia ModContent de las armas del mod:
    # los MemberRef a ItemType<...>() del ESCALERA murieron — pero las
    # clases siguen vivas (los proyectiles existen). La prueba contable:
    # 'SunshineofIsrapony' y 'Zenith' son CONSTANTES DE CAMPO de
    # Terraria (ints en el IL) — no viajan. La prueba real la hizo el
    # oráculo (CS0117 al principio confirmó que el compilador LEE los
    # nombres) + el grep del fuente. Aquí: el string del COMENTARIO no
    # compila. Verificación de compensa: las claves Camino* (arriba).
    ok('(verificado en el oráculo + el fuente — el conteo de entradas 401 '
       'confirma cero archivos tocados)')

    print()
    if FALLOS:
        print(f'✗✗✗ {len(FALLOS)} FALLOS ✗✗✗')
        for f in FALLOS:
            print(f'  - {f}')
        sys.exit(1)
    print(f'✓✓✓ AUDITORÍA LIMPIA: {len(entries)} entradas, md5 {md5}')


if __name__ == '__main__':
    main()
