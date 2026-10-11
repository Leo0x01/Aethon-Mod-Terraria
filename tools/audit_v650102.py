#!/usr/bin/env python3
"""
v6.50.102 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO: «creo que me quedo con el Equinoccio, puedes borrar
las otras y también borra el grimorio sellado, borra también tinta viva»
+ «usa el proyectil del Equinoccio para el grimorio del eterno […] el
grimorio del eterno tendrá las mecánicas del grimorio hambriento nervioso
pero esta vez será jugable no de prueba».

(1) LOS MUERTOS: 3 armas de la Emperatriz (Estrellado/Eco/Veredicto) +
    sus 2 proyectiles (Estela/Trazo) + Grimorio Sellado + Tinta Viva +
    LA CASCADA: FragmentoAethon/ExplosionAethon/SistemaObservado/buff
    Observado (huérfanos al morir su dueño) + GrimoireEternal.png (el
    Eterno viste el sprite del Nervioso por Texture override).
    22 entradas menos que la .101: 397 → 375.
(2) EL ETERNO NERVIOSO: subclase de GrimorioHambrientoNervioso — sprite
    del Nervioso, hambre 15 min (1/54000), pareja Equinoccio por disparo
    (dorado 9201 + negro 9202 SIN bruma), minion en clic der, XP del
    festín al libro que come (ai[2] tipo).
(3) LA BRUMA MURIÓ: el negro del Equinoccio ya no exhala BrumaColumna.
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

MUERTOS_RUTA = [
    'Content/Weapons/Emperatriz/GrimorioEstrellado.cs',
    'Content/Weapons/Emperatriz/GrimorioEstrellado.png',
    'Content/Weapons/Emperatriz/EcoEmperatriz.cs',
    'Content/Weapons/Emperatriz/EcoEmperatriz.png',
    'Content/Weapons/Emperatriz/VeredictoEmperatriz.cs',
    'Content/Weapons/Emperatriz/VeredictoEmperatriz.png',
    'Content/Projectiles/Emperatriz/EstelaAethonProjectile.cs',
    'Content/Projectiles/Emperatriz/EstelaAethonProjectile.png',
    'Content/Projectiles/Emperatriz/TrazoEmperatrizProjectile.cs',
    'Content/Projectiles/Emperatriz/TrazoEmperatrizProjectile.png',
    'Content/Weapons/GrimorioSellado.cs',
    'Content/Weapons/GrimorioSellado.png',
    'Content/Weapons/Sombras/TintaViva.cs',
    'Content/Weapons/Sombras/TintaViva.png',
    'Content/Projectiles/Aethon/FragmentoAethon.cs',
    'Content/Projectiles/Aethon/FragmentoAethon.png',
    'Content/Projectiles/Aethon/ExplosionAethon.cs',
    'Content/Projectiles/Aethon/ExplosionAethon.png',
    'Content/Systems/SistemaObservado.cs',
    'Content/Buffs/Observado.cs',
    'Content/Buffs/Observado.png',
    'Content/Weapons/GrimoireEternal.png',
]

# símbolos que DEBEN vivir en el heap #Strings de la DLL (UTF-8 crudo)
VIVOS_HEAP = [
    'GrimoireEternal', 'GrimorioHambrientoNervioso', 'EstadoGrimorio',
    'EstadoDeLibro', 'EstadoEterno', 'LanzarPareja', 'TicksQuietudSalida',
    'TipoLibro', 'TasaHambre', 'PasoMaquina', 'Corrimiento', 'DibujarTiras',
    'GrimorioNerviosoFlotante', 'SombraPaginaCaza', 'TipoLibroDeLaRaiz',
    'EmperatrizFX', 'Equinoccio', 'BolsaEmperatriz', 'ReabrirLoteVanilla',
]

# símbolos que DEBEN estar AUSENTES del heap (los muertos)
MUERTOS_HEAP = [
    'GrimorioSellado', 'TintaViva', 'GrimorioEstrellado', 'EcoEmperatriz',
    'VeredictoEmperatriz', 'EstelaAethonProjectile', 'TrazoEmperatrizProjectile',
    'FragmentoAethon', 'ExplosionAethon', 'SistemaObservado',
]


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

    print(f'=== 1. CABECERA ===')
    if ver == '6.50.102' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')

    print(f'=== 2. ENTRADAS ({len(entries)}) ===')
    # los .cs NO son entradas (viven en la DLL): sólo las 11 texturas
    # muertas salen del paquete — 397 − 11 = 386
    if len(entries) == 386:
        ok('386 entradas (397 de la .101 − 11 rawimg muertos)')
    else:
        mal(f'se esperaban 386 entradas, hay {len(entries)}')

    print(f'=== 3. EOF EXACTO ===')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print(f'=== 4. LOS MUERTOS AUSENTES DEL PAQUETE ===')
    for ruta in MUERTOS_RUTA:
        if ruta in rutas:
            mal(f'el muerto sigue en el paquete: {ruta}')
    vivos_esperados = [
        'Content/Weapons/Emperatriz/Equinoccio.rawimg',
        'Content/Items/Bolsas/BolsaEmperatriz.rawimg',
    ]
    for ruta in vivos_esperados:
        if ruta in rutas:
            ok(f'la elegida y su bolsa viven: {ruta}')
        else:
            mal(f'FALTA en el paquete: {ruta}')
    if not any(r in rutas for r in MUERTOS_RUTA):
        ok('los archivos muertos NO están en el paquete')

    print(f'=== 5. hjson ×3: PAQUETE == ÁRBOL ===')
    for h in ['es-MX', 'es-ES', 'en-US']:
        clave = f'Localization/{h}_Mods.AethonMod.hjson'
        if clave not in blobs:
            mal(f'falta {clave} en el paquete')
            continue
        arbol = open(f'{ARBOL}/{clave}', 'rb').read()
        if blobs[clave] == arbol:
            ok(f'{h}: paquete == árbol ({len(arbol)} B)')
        else:
            mal(f'{h}: el paquete DIFF del árbol '
                f'({len(blobs[clave])} vs {len(arbol)} B)')

    print(f'=== 6. LA DLL: VIVOS Y MUERTOS EN EL HEAP #Strings ===')
    dll = blobs['AethonMod.dll']
    # el heap #Strings de metadata .NET es UTF-8 con terminadores \0
    def en_heap(s):
        return s.encode('utf-8') + b'\x00' in dll
    for s in VIVOS_HEAP:
        if en_heap(s):
            ok(f'VIVO en el heap: {s}')
        else:
            mal(f'FALTA en el heap: {s}')
    for s in MUERTOS_HEAP:
        if en_heap(s):
            mal(f'el muerto VIVE en el heap: {s}')
        else:
            ok(f'muerto ausente: {s}')

    print(f'=== 7. LA BRUMA MURIÓ EN EL EQUINOCCIO (del árbol — el .cs vive en la DLL) ===')
    fx_txt = open(f'{ARBOL}/Content/Globals/EmperatrizFX.cs', encoding='utf-8').read()
    if 'BrumaColumna' in fx_txt:
        mal('EmperatrizFX.cs aún llama BrumaColumna')
    elif '9101' in fx_txt:
        mal('EmperatrizFX.cs aún tiene el caso del Eco (9101)')
    elif '9202' in fx_txt and 'FlushAlpha' in fx_txt:
        ok('el negro 9202 conserva el disco de eclipse (FlushAlpha) SIN BrumaColumna')
    else:
        mal('EmperatrizFX.cs inesperado')

    print(f'=== 8. EL ETERNO EN EL ÁRBOL (los parámetros del merge) ===')
    et = open(f'{ARBOL}/Content/Weapons/GrimoireEternal.cs', encoding='utf-8').read()
    for patron, etiqueta in [
        ('1f / 54000f', 'la tasa del hambre jugable (15 min)'),
        ('GrimorioHambrientoNervioso', 'la subclase del Nervioso'),
        ('9201f', 'la marca del dorado'),
        ('9202f', 'la marca del negro'),
        ('TicksQuietudSalida => 600', 'la espera jugable (10 s)'),
        ('CosmicOrbMinion', 'el Orbe Cósmico del clic der'),
        ('Texture => "AethonMod/Content/Weapons/GrimorioHambrientoNervioso"', 'el sprite del Nervioso'),
    ]:
        if patron in et:
            ok(f'{etiqueta}')
        else:
            mal(f'FALTA en GrimoireEternal.cs: {patron}')

    print(f'=== 9. LA BOLSA DE LA EMPERATRIZ: SÓLO LA ELEGIDA (del árbol) ===')
    bolsa = open(f'{ARBOL}/Content/Items/Bolsas/BolsasCategorias.cs', encoding='utf-8').read()
    if 'GrimorioSellado' in bolsa or 'GrimorioEstrellado' in bolsa or 'TintaViva' in bolsa:
        mal('la bolsa aún referencia muertos')
    elif bolsa.count('Equinoccio') >= 2:
        ok('la bolsa entrega al Equinoccio y nada más (los muertos fuera)')
    else:
        mal('la bolsa no entrega al Equinoccio')

    print()
    if FALLOS:
        print(f'✗ {len(FALLOS)} FALLOS')
        return 1
    print('TODO OK — v6.50.102 auditada')
    return 0


if __name__ == '__main__':
    sys.exit(main())
