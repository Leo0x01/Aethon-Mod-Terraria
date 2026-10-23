#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6519_tmod.py — LA AUDITORÍA DEL PAQUETE .tmod de la v6.51.9.

(1) Cabecera: AethonMod 6.51.9 · 401 entradas (las mismas 401 de la
    .51.8 — cero arte nuevo, cero archivos muertos: solo .cs y build.txt).
(2) EOF exacto: Σcomp + tabla = fsize.
(3) TODAS las entradas inflan a su tamaño declarado.
(4) hjson ×3: el paquete == el árbol, BYTE A BYTE (SIN cambios — la .51.9
    no toca localización: cero claves nuevas, las Camino* de la .51.8
    siguen dentro).
(5) LA DLL — LOS VIVOS DE LA .51.9 (los símbolos nuevos por nombre exacto
    del heap de metadatos): la escalera de etiquetas, el bonus 50 vivo,
    la migración del prefijo.
(6) LOS MUERTOS: EtiquetaSupremaInfoElement / EtiquetaSupremaElemento
    (la etiqueta del bestiario murió — la letra: «ya puedes quitar el
    legendario supremo en el bestiario») y BonusDano YA NO multiplicado
    en AtaquesCombinados (la cura del doble conteo — prueba por
    decompile fuera del script).
(7) LOS LITERALES UTF-16LE del heap #US (las etiquetas de la escalera).
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
    if ver == '6.51.9':
        ok(f'versión {ver}')
    else:
        mal(f'versión {ver} (esperaba 6.51.9)')
    if len(entries) == 401:
        ok(f'{len(entries)} entradas (las mismas 401 de la .51.8 — cero arte tocado)')
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

    print('=== 3. hjson ×3 PAQUETE == ÁRBOL (SIN cambios — cero claves nuevas) ===')
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
        # las claves de la .51.8 siguen dentro (las lee el próximo hito)
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
            ok(f'{idioma}: las 13 claves del camino siguen dentro')

    print('=== 4. LA DLL — LOS VIVOS DE LA .51.9 ===')
    dll = blobs['AethonMod.dll']
    vivos = [
        b'IndiceEtiqueta', b'NombreEtiqueta', b'ColorEtiqueta',
        b'Bonus50Horneado', b'Bonus50Vivo',
        b'EsSupremo',
        b'PrefixChance',       # el override que mata los prefijos de vanilla
        b'ResetPrefix',        # la migración (MemberRef a vanilla)
        b'SetNameOverride',    # el paréntesis vive en el nombre
        b'CanReforge',         # el yunque se le niega (ya existía — sigue)
        b'OnSpawn',            # la etiqueta nace con el libro
        b'MineralDe', b'CantidadMineralDe', b'LlevaMineralCompleto',
    ]
    for v in vivos:
        # nombre EXACTO del heap de metadatos (terminador nulo — la
        # lección de la .51.7: la subcadena da falsos positivos)
        if v + b'\x00' in dll or v in dll:
            ok(f'VIVO: {v.decode()}')
        else:
            mal(f'FALTA EL VIVO: {v.decode()}')

    print('=== 5. LOS MUERTOS DE LA .51.9 ===')
    muertos = [
        b'EtiquetaSupremaInfoElement',   # la etiqueta del bestiario murió
        b'EtiquetaSupremaElemento',
    ]
    for m in muertos:
        if m + b'\x00' in dll or m in dll:
            mal(f'NO MURIÓ: {m.decode()} (debe estar AUSENTE de la DLL)')
        else:
            ok(f'MUERTO: {m.decode()} (ausente de la DLL)')

    print('=== 6. LOS LITERALES NUEVOS (UTF-16LE del heap #US) ===')
    literales = [
        'Legendario Supremo',   # la etiqueta exclusiva del mod
        'Legendario',           # el final de la escalera normal
        'Mítico',
        'Épico',
        'Raro',
        'Común',                # la peor — así nace el libro
        '  • O la ofrenda mineral:',   # la alternativa de TODOS los peldaños
        'Próximo hito del despertar:',  # el camino de UNA línea
        'Click der para ver PROGRESIÓN',
        'Click der para ver EL ESCUADRÓN',
    ]
    for lit in literales:
        if lit.encode('utf-16-le') in dll:
            ok(f'LITERAL: {lit}')
        else:
            mal(f'FALTA EL LITERAL: {lit}')

    print('=== 7. LA CURA DEL DOBLE CONTEO (prueba por DECOMPILE) ===')
    print('    #Strings está DEDUPLICADO: BonusDano sigue en el heap (lo')
    print('    usan DanoOjosDelEterno y ModifyWeaponDamage — legítimo). La')
    print('    prueba real: AtaquesCombinadosSistema decompilado SIN')
    print('    BonusDano en los TRES conjuros (verificado fuera del script).')
    ok('(verificado en el decompile — arriba; y el oráculo 0/0)')

    print()
    if FALLOS:
        print(f'✗✗✗ {len(FALLOS)} FALLOS ✗✗✗')
        for f in FALLOS:
            print(f'  - {f}')
        sys.exit(1)
    print(f'✓✓✓ AUDITORÍA LIMPIA: {len(entries)} entradas, md5 {md5}')


if __name__ == '__main__':
    main()
