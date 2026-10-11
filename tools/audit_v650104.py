#!/usr/bin/env python3
"""
v6.50.104 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO (dos pedidos):
(1) «el hambre en el grimorio del eterno no baja, el hambre deberia
    bajar usando la rareza de las criaturas como fuente, cada criatura
    de 1 estrella solo quita el 0.1% de hambre los jefes quitan 10% de
    hambre, y el hambre se reduce de 0.1% a 5% dependiendo de la
    rareza del monstruo» — LA DIETA POR RAREZA: cada baja acreditada
    (GlobalNPCXP.OnKill, xp>0) Y cada presa devorada por la sombra
    (AbsorberPresa) bajan el hambre del Eterno según las ESTRELLAS del
    bestiario (la misma fuente que la XP): 1★=0,1% … 5★=5% LINEAL,
    jefes 10%. El Nervioso de prueba conserva su −1% plano de demo.
(2) «el sprite del libro que aparece como icono de la oleada no tiene
    animacion, deberia tener animacion» — EL ICONO VIVO: la cajita del
    título de la oleada ahora RESPIRA + FLOTA + SE INCLINA y cada 3,5 s
    sufre EL SOBRESALTO nervioso (determinista, sin RNG).

CHECKS:
(1) cabecera 6.50.104 · 386 entradas · EOF exacto.
(2) LA DIETA EN EL BINARIO: los float32 de la escalera (0.1 jefe,
    0.001 piso, 0.01225 el paso lineal) VIVEN en la DLL + los
    símbolos HambrePorPresa/ReduccionHambre en el heap.
(3) EL ICONO VIVO y LA DIETA en el árbol (los 4 archivos tocados).
(4) hjson ×3: paquete == árbol + los textos nuevos («SEGÚN SU
    RAREZA»/«BY RARITY», «Absorbe: −{0}%») y los viejos ausentes.
(5) Regresión .102/.103: el reloj 1/216000 intacto en la DLL, los
    muertos ausentes, la pareja Equinoccio, la bolsa, la bruma muerta.
"""
import hashlib
import struct
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

VIVOS_HEAP = [
    # la dieta nueva
    'HambrePorPresa', 'ReduccionHambre', 'EstrellasDe',
    # la familia viva (regresión .102)
    'GrimoireEternal', 'GrimorioHambrientoNervioso', 'EstadoGrimorio',
    'EstadoDeLibro', 'EstadoEterno', 'LanzarPareja', 'TicksQuietudSalida',
    'TasaHambre', 'GrimorioNerviosoFlotante', 'SombraPaginaCaza',
    'EmperatrizFX', 'Equinoccio', 'BolsaEmperatriz', 'AbsorberPresa',
]

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


def f32(x):
    return struct.pack('<f', x)


def main():
    d = open(TMOD, 'rb').read()
    md5 = hashlib.md5(d).hexdigest()
    print(f'=== {TMOD} ===')
    print(f'  {len(d)} B | md5 {md5}')

    tmlver, name, ver, entries, tend, fsize, blobs = inflar(TMOD)
    rutas = {p for p, _, _ in entries}

    print('=== 1. CABECERA / ENTRADAS / EOF ===')
    if ver == '6.50.104' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    if len(entries) == 386:
        ok('386 entradas (las mismas de la .102/.103 — cero archivos tocados)')
    else:
        mal(f'se esperaban 386 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. LA DIETA POR RAREZA — DENTRO DE LA DLL ===')
    dll = blobs['AethonMod.dll']

    def en_dll(b):
        return b in dll

    # la escalera de la letra: jefe 0.10 · piso 0.001 · la resta plegada
    # 0.05−0.001 (Roslyn pliega SÓLO la resta — la /4 aplica al producto
    # con la variable; el plegado es ARITMÉTICA FLOAT POR OPERACIÓN, no
    # el double 0.049 redondeado: difieren 1 ulp — verificado con un
    # build de prueba del SDK)
    try:
        import numpy as np
        RESTA_PLEGADA = struct.pack('<f',
            float(np.float32(0.05) - np.float32(0.001)))
    except ImportError:
        RESTA_PLEGADA = bytes.fromhex('3ab4483d')
    escalera = [
        (f32(0.10), '0.10f — EL JEFE VALE 10%'),
        (f32(0.001), '0.001f — el piso de la 1★ (0,1%)'),
        (RESTA_PLEGADA, '0.05f−0.001f — la resta plegada de la escalera'),
        (f32(0.05), '0.05f — el tope de la 5★ (5%)'),
    ]
    for bytes_, etiqueta in escalera:
        if en_dll(bytes_):
            ok(f'{etiqueta} vive en el IL')
        else:
            mal(f'FALTA en la DLL: {etiqueta}')
    # la demo del Nervioso (−1%) SIGUE viva — es SU valor, no el del Eterno
    if en_dll(f32(0.01)):
        ok('0.01f vivo (el −1% de la DEMO del Nervioso — ahora sóLO suyo)')
    else:
        mal('el −1% de la demo del Nervioso desapareció (regresión)')
    # el reloj de la hora (.103) intacto
    if en_dll(f32(1.0 / 216000.0)):
        ok('1f/216000f — EL RELOJ DE LA HORA intacto (regresión .103)')
    else:
        mal('el reloj de la hora se perdió (regresión .103)')

    print('=== 3. LA DLL: VIVOS Y MUERTOS EN EL HEAP #Strings ===')
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

    print('=== 4. EL ÁRBOL — LA DIETA Y EL ICONO VIVO ===')
    et = open(f'{ARBOL}/Content/Weapons/GrimoireEternal.cs', encoding='utf-8').read()
    gh = open(f'{ARBOL}/Content/Weapons/GrimorioHambriento.cs', encoding='utf-8').read()
    nv = open(f'{ARBOL}/Content/Weapons/GrimorioHambrientoNervioso.cs', encoding='utf-8').read()
    xp = open(f'{ARBOL}/Content/Globals/GlobalNPCXP.cs', encoding='utf-8').read()
    fu = open(f'{ARBOL}/Content/Systems/GrimorioFuriaSistema.cs', encoding='utf-8').read()
    for patron, etiqueta, src in [
        ('HambrePorPresa', 'el helper de la dieta (Eterno)', et),
        ('ShardLevelSystem.EstrellasDe', 'la fuente: las estrellas del bestiario', et),
        ('presa.boss', 'el jefe vale 10% entero', et),
        ('TasaHambre = 1f / 216000f', 'el reloj de la hora INTACTO', et),
        ('TicksQuietudSalida => 600', 'la espera de caza 10 s INTACTA', et),
        ('TasaHambre = 1f / 3600f', 'el minuto de demo del Nervioso INTACTO', gh),
        ('ReduccionHambre', 'la dieta POR LIBRO (Nervioso demo / Eterno rareza)', nv),
        ('string.Format(Language.GetTextValue("Mods.AethonMod.Hambre.Nervioso.Absorbe")',
         'el Absorbe DINÁMICO (la cifra real del bocado)', nv),
        ('GrimoireEternal.HambrePorPresa(npc)', 'OnKill alimenta el reloj del Eterno', xp),
        ('EstadoEterno', 'OnKill baja el estado DEL ETERNO', xp),
        ('EL ICONO VIVO', 'el comentario del icono animado', fu),
        ('pasoSobresalto', 'EL SOBRESALTO del icono (el nervio de la familia)', fu),
        ('escIcono', 'la RESPIRACIÓN del icono (escala latiendo)', fu),
        ('bobIcono', 'la FLOTACIÓN del icono (vaivén)', fu),
        ('sb.Draw(icono, centroIcono', 'el draw ANIMADO del icono', fu),
    ]:
        if patron in src:
            ok(f'{etiqueta}')
        else:
            mal(f'FALTA en el árbol: {patron}')
    # el draw VIEJO (estático, origin left) debe estar AUSENTE
    if 'new Vector2(0f, icono.Height / 2)' in fu:
        mal('el icono aún tiene el draw estático viejo')
    else:
        ok('el draw estático del icono murió — ahora es animado')
    fx_txt = open(f'{ARBOL}/Content/Globals/EmperatrizFX.cs', encoding='utf-8').read()
    if 'BrumaColumna' in fx_txt:
        mal('EmperatrizFX.cs aún llama BrumaColumna (regresión .102)')
    else:
        ok('el negro 9202 sigue eclipse LIMPIO (regresión .102 OK)')

    print('=== 5. hjson ×3: PAQUETE == ÁRBOL + LOS TEXTOS DE LA DIETA ===')
    for h, dentro, fuera in [
        ('es-MX', ['SEGÚN SU RAREZA', 'Absorbe: −{0}% hambre'],
         ['(−1% por presa)', 'Absorbe: −1% hambre']),
        ('es-ES', ['SEGÚN SU RAREZA', 'Absorbe: −{0}% hambre'],
         ['(−1% por presa)', 'Absorbe: −1% hambre']),
        ('en-US', ['BY RARITY', 'Absorbe: −{0}% hunger'],
         ['(−1% each)', 'Absorbe: −1% hunger']),
    ]:
        clave = f'Localization/{h}_Mods.AethonMod.hjson'
        if clave not in blobs:
            mal(f'falta {clave} en el paquete')
            continue
        arbol = open(f'{ARBOL}/{clave}', 'rb').read()
        if blobs[clave] != arbol:
            mal(f'{h}: el paquete DIFF del árbol '
                f'({len(blobs[clave])} vs {len(arbol)} B)')
            continue
        texto = blobs[clave].decode('utf-8')
        faltan = [x for x in dentro if x not in texto]
        sobran = [x for x in fuera if x in texto]
        if not faltan and not sobran:
            ok(f'{h}: paquete == árbol ({len(arbol)} B) + dieta nueva, sin restos')
        else:
            mal(f'{h}: faltan {faltan} / sobran {sobran}')

    print('=== 6. REGRESIÓN — LOS MUERTOS AUSENTES DEL PAQUETE ===')
    for ruta in MUERTOS_RUTA:
        if ruta in rutas:
            mal(f'el muerto sigue en el paquete: {ruta}')
    for ruta in ['Content/Weapons/Emperatriz/Equinoccio.rawimg',
                 'Content/Items/Bolsas/BolsaEmperatriz.rawimg']:
        if ruta in rutas:
            ok(f'la elegida y su bolsa viven: {ruta}')
        else:
            mal(f'FALTA en el paquete: {ruta}')
    if not any(r in rutas for r in MUERTOS_RUTA):
        ok('los archivos muertos NO están en el paquete')

    print()
    if FALLOS:
        print(f'✗✗✗ {len(FALLOS)} FALLOS:')
        for f in FALLOS:
            print(f'  - {f}')
        sys.exit(1)
    print(f'TODO OK — {len(d)} B, md5 {md5}, 386 entradas, la DIETA POR '
          f'RAREZA vive en la DLL y el ICONO DE LA OLEADA ya es VIVO')


if __name__ == '__main__':
    main()
