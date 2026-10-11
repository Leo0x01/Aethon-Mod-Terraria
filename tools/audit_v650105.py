#!/usr/bin/env python3
"""
v6.50.105 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO: «también debemos cambiar los minions, los minion
que use el grimorio será su propio ojo […] es tiempo de animar al
minion, yo te daré los sprite del ojo usando código […] el ojo se mueve
en la direccion en la que mira, o sea para moverse primero mira y luego
se mueve […] se posicionan encima del jugador como una corona, y si el
grimorio esta fuera comienzan a orbitar al grimorio en 2 circulos uno
de 5 minion, otro de 6 y asi […] cada minion mira a su objetivo y usa
el rayo con ruido de perlin ademas de que este rayo usado por los
minion es negro, con bordes morado y con bruma en la punta que toca al
monstruo, cada minion debe tener un pequeño circulo magico igual que el
de los agujeros negros […] cuando el minion dispara tiene un pequeño
retroceso y luego se reincorpora, los ojos de cada minion son
independientes de cada uno de ellos y del libro y no se ven afectados
por la animacion de hambre del grimorio».

LOS OJOS DEL ETERNO: el Orbe Cósmico muere entero (minion + bolt +
buff) y nacen LOS OJOS — el recorte del sprite exacto del usuario
(44×37 ×3 estados), el iris de la .84 como capa, la corona/órbitas,
el rayo Perlin NEGRO de bordes morados con bruma en la punta, el
círculo mágico de los agujeros negros y el parpadeo independiente.

CHECKS:
(1) cabecera 6.50.105 · 387 entradas (386 − 2 CosmicOrb + 3 ojos) · EOF.
(2) EL RAYO Y LA MÁQUINA en el heap: OjoEternoMinion/OjoEternoBuff/
    DrawElSello/DrawElRayo/CanalPerlin/SlotDeFormacion + los muertos
    CosmicOrb* AUSENTES.
(3) LAS CONSTANTES de la letra en el IL (float32): la mirada 7,5/3,5,
    el giro 0,22, el retroceso 3.0, el iris 0.52, la corona 3.49/5.93,
    las órbitas 58/42, el sello 2.55.
(4) EL ÁRBOL: los archivos tocados con sus patrones de la letra.
(5) hjson ×3: paquete == árbol + Ojos del Eterno vivos / Orbe Cósmico
    muerto.
(6) Regresión .102/.103/.104: el reloj 1/216000, la dieta, la pareja
    Equinoccio, la bolsa — y los muertos históricos ausentes.
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
    # los muertos históricos (la .102) — SIGUEN ausentes
    'Content/Weapons/Emperatriz/GrimorioEstrellado.cs',
    'Content/Weapons/Emperatriz/GrimorioEstrellado.rawimg',
    'Content/Weapons/Emperatriz/EcoEmperatriz.cs',
    'Content/Weapons/Emperatriz/EcoEmperatriz.rawimg',
    'Content/Weapons/Emperatriz/VeredictoEmperatriz.cs',
    'Content/Weapons/Emperatriz/VeredictoEmperatriz.rawimg',
    'Content/Projectiles/Emperatriz/EstelaAethonProjectile.cs',
    'Content/Projectiles/Emperatriz/EstelaAethonProjectile.rawimg',
    'Content/Projectiles/Emperatriz/TrazoEmperatrizProjectile.cs',
    'Content/Projectiles/Emperatriz/TrazoEmperatrizProjectile.rawimg',
    'Content/Weapons/GrimorioSellado.cs',
    'Content/Weapons/GrimorioSellado.rawimg',
    'Content/Weapons/Sombras/TintaViva.cs',
    'Content/Weapons/Sombras/TintaViva.rawimg',
    'Content/Projectiles/Aethon/FragmentoAethon.cs',
    'Content/Projectiles/Aethon/FragmentoAethon.rawimg',
    'Content/Projectiles/Aethon/ExplosionAethon.cs',
    'Content/Projectiles/Aethon/ExplosionAethon.rawimg',
    'Content/Systems/SistemaObservado.cs',
    'Content/Buffs/Observado.cs',
    'Content/Buffs/Observado.rawimg',
    'Content/Weapons/GrimoireEternal.rawimg',
    # los muertos de la .105 — EL ORBE CÓSMICO ENTERO
    'Content/Projectiles/CosmicOrbMinion.cs',
    'Content/Projectiles/CosmicOrbMinion.rawimg',
    'Content/Projectiles/CosmicOrbBolt.cs',
    'Content/Projectiles/CosmicOrbBolt.rawimg',
    'Content/Buffs/CosmicOrbBuff.cs',
]

VIVOS_HEAP = [
    # los ojos nuevos (la .105)
    'OjoEternoMinion', 'OjoEternoBuff', 'DrawElSello', 'DrawElRayo',
    'DrawElOjo', 'CanalPerlin', 'SlotDeFormacion', 'ObjetivoDelOjo',
    'SalirDeFuego', 'FocoVivo',
    # la familia viva (regresión .102/.103/.104)
    'GrimoireEternal', 'GrimorioHambrientoNervioso', 'EstadoGrimorio',
    'EstadoDeLibro', 'EstadoEterno', 'LanzarPareja', 'TicksQuietudSalida',
    'TasaHambre', 'GrimorioNerviosoFlotante', 'SombraPaginaCaza',
    'EmperatrizFX', 'Equinoccio', 'BolsaEmperatriz', 'AbsorberPresa',
    'HambrePorPresa', 'ReduccionHambre', 'EstrellasDe',
    'SigiloErosionado', 'GolpeMotor', 'Bruma',
]

MUERTOS_HEAP = [
    'GrimorioSellado', 'TintaViva', 'GrimorioEstrellado', 'EcoEmperatriz',
    'VeredictoEmperatriz', 'EstelaAethonProjectile', 'TrazoEmperatrizProjectile',
    'FragmentoAethon', 'ExplosionAethon', 'SistemaObservado',
    # el orbe entero (la .105)
    'CosmicOrbMinion', 'CosmicOrbBuff', 'CosmicOrbBolt',
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
    if ver == '6.50.105' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    if len(entries) == 387:
        ok('387 entradas (386 − 2 del Orbe + 3 de los OJOS)')
    else:
        mal(f'se esperaban 387 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. EL HEAP #Strings — LOS OJOS VIVOS, EL ORBE MUERTO ===')
    dll = blobs['AethonMod.dll']

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

    print('=== 3. LAS CONSTANTES DE LA LETRA — EN EL IL (float32) ===')
    def en_dll(b):
        return b in dll
    # NOTA (la lección de la .104): Roslyn sólo pliega subexpresiones
    # 100% constantes; estas son ldc.r4 DIRECTOS de asignaciones y
    # llamadas — no hay aritmética plegable que los difiera.
    constantes = [
        (f32(0.52), '0.52f — la escala del IRIS del ojo minion'),
        (f32(7.5), '7.5f — la mirada horizontal (la almendra)'),
        (f32(3.5), '3.5f — la mirada vertical'),
        (f32(0.22), '0.22f — el giro del iris por tick'),
        (f32(3.0), '3.0f — LA PATADA DEL RETROCESO'),
        (f32(3.49), '3.49f — la corona 200°'),
        (f32(5.93), '5.93f — la corona 340°'),
        (f32(58.0), '58f — el radio de la 1ª órbita'),
        (f32(42.0), '42f — el escalón de las órbitas'),
        (f32(2.55), '2.55f — el aro del círculo mágico (la casa)'),
        (f32(1.0 / 216000.0), '1f/216000f — EL RELOJ DE LA HORA (regresión .103)'),
        (f32(0.10), '0.10f — el jefe vale 10% (regresión .104)'),
    ]
    for bytes_, etiqueta in constantes:
        if en_dll(bytes_):
            ok(f'{etiqueta} vive en el IL')
        else:
            mal(f'FALTA en la DLL: {etiqueta}')

    print('=== 4. EL ÁRBOL — LOS ARCHIVOS DE LA LETRA ===')
    ojo = open(f'{ARBOL}/Content/Projectiles/OjoEternoMinion.cs', encoding='utf-8').read()
    et = open(f'{ARBOL}/Content/Weapons/GrimoireEternal.cs', encoding='utf-8').read()
    for patron, etiqueta, src in [
        ('GolpeMotor.Golpear(Projectile, presa', 'el golpe por EL MOTOR (la .105)', ojo),
        ('OrbitaLib.SigiloErosionado', 'EL CÍRCULO MÁGICO de los agujeros negros', ojo),
        ('SombrasLib.Bruma(punta', 'LA BRUMA en la punta que toca al monstruo', ojo),
        ('RayoStrip.Filamento(canal', 'la funda morada del rayo', ojo),
        ('BlendState.AlphaBlend', 'el NÚCLEO NEGRO en AlphaBlend', ojo),
        ('StormLib.Chaikin(pts, 1)', 'el meandro Perlin (el canal)', ojo),
        ('StormLib.Fbm1D(t * 2.8f', 'el fBm 3 octavas — EL RUIDO DE PERLIN', ojo),
        ('MODO_APUNTANDO', 'la carga del disparo', ojo),
        ('atrás * 3.0f', 'EL RETROCESO y la reincorporación', ojo),
        ('VFXCore.Hash01(Projectile.whoAmI, 911', 'el parpadeo INDEPENDIENTE por ojo (hash)', ojo),
        ('fase > 7f || fase < 4f', 'la convención de fases del libro (la .86)', ojo),
        ('Projectile.minionPos', 'el reparto de la formación (la corona/órbitas)', ojo),
        ('0.34f : -0.27f', 'la contrarrotación de los anillos', ojo),
        ('VFXCore.ReabrirLoteVanilla()', 'el contrato del lote (la .101)', ojo),
        ('OjoEternoMinion', 'el Eterno invoca LOS OJOS', et),
        ('OjosFormacion', 'el tooltip de la corona/órbitas', et),
        ('OJOS DEL ETERNO ═══', 'la sección del tooltip', et),
    ]:
        if patron in src:
            ok(f'{etiqueta}')
        else:
            mal(f'FALTA en el árbol: {patron}')
    # el spawn VIEJO debe estar AUSENTE
    if 'CosmicOrbMinion' in et:
        mal('GrimoireEternal aún invoca al Orbe Cósmico')
    else:
        ok('el Orbe Cósmico murió en el Eterno')

    print('=== 5. hjson ×3: PAQUETE == ÁRBOL + LOS OJOS VIVOS ===')
    for h, dentro, fuera in [
        ('es-MX', ['Ojos del Eterno', 'El Ojo del Eterno', 'rayo negro de bruma',
                   'invoca los Ojos del Eterno'],
         ['Orbe Cósmico', 'Bolt Cósmico']),
        ('es-ES', ['Ojos del Eterno', 'El Ojo del Eterno', 'rayo negro de bruma',
                   'invoca los Ojos del Eterno'],
         ['Orbe Cósmico', 'Bolt Cósmico']),
        ('en-US', ['Eyes of the Eternal', 'The Eye of the Eternal', 'misty black ray',
                   'Eyes of the Eternal (crown/orbits'],
         ['Cosmic Orb', 'Cosmic Bolt']),
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
            ok(f'{h}: paquete == árbol ({len(arbol)} B) + Ojos vivos, Orbe muerto')
        else:
            mal(f'{h}: faltan {faltan} / sobran {sobran}')

    print('=== 6. REGRESIÓN — LOS MUERTOS AUSENTES DEL PAQUETE ===')
    for ruta in MUERTOS_RUTA:
        if ruta in rutas:
            mal(f'el muerto sigue en el paquete: {ruta}')
    for ruta in ['Content/Weapons/Emperatriz/Equinoccio.rawimg',
                 'Content/Items/Bolsas/BolsaEmperatriz.rawimg',
                 'Content/Projectiles/OjoEterno_Abierto.rawimg',
                 'Content/Projectiles/OjoEterno_Medio.rawimg',
                 'Content/Projectiles/OjoEterno_Cerrado.rawimg']:
        if ruta in rutas:
            ok(f'vive en el paquete: {ruta}')
        else:
            mal(f'FALTA del paquete: {ruta}')
    # las rawimg de los ojos: EL FORMATO DE LA CASA (12 B de cabecera +
    # RGBA puro — la .100: 3.612 B = 30×30×4+12) y los PÍXELES == el PNG
    # del árbol decodificado (la LANCZOS del gen, píxel sobre píxel)
    import struct as _st
    import numpy as _np
    from PIL import Image as _Img
    for nombre in ('OjoEterno_Abierto', 'OjoEterno_Medio', 'OjoEterno_Cerrado'):
        ruta = f'Content/Projectiles/{nombre}.rawimg'
        if ruta not in rutas:
            mal(f'FALTA {ruta}'); continue
        raw = blobs[ruta]
        fmt, w, h = _st.unpack_from('<3i', raw, 0)
        if len(raw) != 12 + w * h * 4 or (fmt, w, h) != (1, 44, 37):
            mal(f'{nombre}: rawimg malformada ({len(raw)} B, {fmt}×{w}×{h})')
            continue
        png = _np.array(_Img.open(f'{ARBOL}/Content/Projectiles/{nombre}.png').convert('RGBA'))
        paquete = _np.frombuffer(raw[12:], dtype=_np.uint8).reshape(h, w, 4)
        if _np.array_equal(png, paquete):
            ok(f'{nombre}.rawimg 44×37 · píxel-idéntica al árbol ({len(raw)} B)')
        else:
            dif = int((png != paquete).any(axis=2).sum())
            mal(f'{nombre}.rawimg DIFF del árbol ({dif} px distintos)')
    # los .cs del árbol no son entradas (la lección .102) — las fuentes
    # borradas no cuentan: sólo lo EMPAQUETADO. CosmicOrb*.cs/ruta fuera.

    print()
    if FALLOS:
        print(f'✗✗✗ {len(FALLOS)} FALLOS — la .105 NO está lista')
        for f in FALLOS:
            print(f'  - {f}')
        sys.exit(1)
    print('✓✓✓ TODO OK — v6.50.105 AUDITADA')


if __name__ == '__main__':
    main()
