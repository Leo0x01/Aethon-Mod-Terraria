#!/usr/bin/env python3
"""
v6.50.106 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO: «no debes hacer recorte ya que arruinaste el recorte
que yo mismo hice, solo debes acomodar el iris para que pueda realmente
funcionar, los rayos que lanzan los ojos son demasiado gruesos y no hacen
daño, ademas la animacion de la corona no existen, se mueven erraticamente
de arriba hacia abajo, ademas la animacion que necesita de movimienot es la
misma que este invocador Ruinous_Staff […] el icono del grimorio en el
evento de oleada no tiene animacion en el ojo […] el proyecril que debe
usar el minion debe ser el mismo que Cetro de Trueno de Perlin pero teñido
de negro en el centro y morado en los bordes con bruma en la punta».

LOS TRES CIRUJÍAS:
(1) EL SPRITE: el recorte del usuario (512×500, bbox opaco del archivo)
    SIN tocar → 56×54; el iris ACOMODADO en el centro de la almendra
    medida (fracción 0.513/0.543, escala 0.41, viaje ±7,5/±2,5).
(2) EL MOVIMIENTO: la ley del Ruinous Staff (glide continuo con
    amortiguación crítica) + la corona VIVA (balanceo/vaivén/respiración)
    + EL DISPARO = el proyectil DEL CETRO DEL TRUENO PERLIN (TrunoPerlin-
    Projectile modo ojo: ai[0]=1 — el golpe del cetro, radio+cadena) con
    el núcleo negro de cápsulas de 19 px (la .105 las ponía a 352 px:
    SoftGlow escala 5,5 — el rayo leía como neblina gorda).
(3) EL ICONO DE LA OLEADA: el OJO del libro de la cajita SIGUE AL CURSOR
    + parpadea (la convención de fases de la .86) + escapa en el
    sobresalto — determinista, cero RNG.

CHECKS:
(1) cabecera 6.50.106 · 387 entradas (los mismos archivos, nuevos bytes) ·
    EOF.
(2) EL HEAP: los ojos + el modo ojo del cetro + el ojo del icono VIVOS;
    DrawElRayo/CanalPerlin (la .105, mudados al cetro) y el Orbe Cósmico
    MUERTOS.
(3) LAS CONSTANTES de la letra en el IL (float32): el iris 0.41/±7,5/±2,5,
    IRIS_REST 0.7/2.3, la ley del planeo 0.15/0.35/0.22, el balanceo 0.20,
    el vaivén 1.1, las órbitas 66/44, la patada 3.2, el sello 16, LA
    CÁPSULA NEGRA 0.30 (19 px — la lección de la .105), el ancla pegada
    0.55, la mirada del icono 3.5/2.8.
(4) EL ÁRBOL: los archivos tocados con sus patrones de la letra.
(5) hjson ×3: paquete == árbol (sin cirugía esta versión — cero riesgo
    de tabs) + los Ojos vivos / el Orbe muerto.
(6) Regresión .102/.103/.104/.105 + las rawimg 56×54 píxel-idénticas.
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
    # los ojos (la .105, vivos en la .106)
    'OjoEternoMinion', 'OjoEternoBuff', 'DrawElSello', 'DrawElOjo',
    'SlotDeFormacion', 'ObjetivoDelOjo', 'SalirDeFuego', 'FocoVivo',
    'Disparar', 'IRIS_REST',
    # el modo ojo del cetro (la .106)
    'TruenoPerlinProjectile', 'DrawArcoOjo', 'CanalOjo', 'BocaDelOjo',
    'PresaDelAncla', 'OjoHalo', 'OjoNegro',
    # el ojo del icono (la .106)
    'PasoOjoIcono', 'SnapDireccionIcono', '_iconoDesp',
    # la familia viva (regresión .102/.103/.104/.105)
    'GrimoireEternal', 'GrimorioHambrientoNervioso', 'EstadoGrimorio',
    'EstadoDeLibro', 'EstadoEterno', 'LanzarPareja', 'TicksQuietudSalida',
    'TasaHambre', 'GrimorioNerviosoFlotante', 'SombraPaginaCaza',
    'EmperatrizFX', 'Equinoccio', 'BolsaEmperatriz', 'AbsorberPresa',
    'HambrePorPresa', 'ReduccionHambre', 'EstrellasDe',
    'SigiloErosionado', 'GolpeMotor', 'Bruma', 'IRIS_ESC',
]

MUERTOS_HEAP = [
    'GrimorioSellado', 'TintaViva', 'GrimorioEstrellado', 'EcoEmperatriz',
    'VeredictoEmperatriz', 'EstelaAethonProjectile', 'TrazoEmperatrizProjectile',
    'FragmentoAethon', 'ExplosionAethon', 'SistemaObservado',
    # el orbe entero (la .105)
    'CosmicOrbMinion', 'CosmicOrbBuff', 'CosmicOrbBolt',
    # los mudados de la .105 (la .106 los movió al cetro)
    'DrawElRayo', 'CanalPerlin',
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
    if ver == '6.50.106' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    if len(entries) == 387:
        ok('387 entradas (los mismos archivos de la .105, nuevos bytes)')
    else:
        mal(f'se esperaban 387 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. EL HEAP #Strings — LOS OJOS Y EL MODO OJO VIVOS ===')
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
    constantes = [
        # EL SPRITE — el iris acomodado
        (f32(0.41), '0.41f — la escala del iris (caja 13 px, disco ~11)'),
        (f32(7.5), '7.5f — el viaje horizontal (y EL GROSOR de la funda del cetro)'),
        (f32(2.5), '2.5f — el viaje vertical'),
        (f32(0.7), '0.7f — IRIS_REST.X (el centro de la almendra)'),
        (f32(2.3), '2.3f — IRIS_REST.Y'),
        # EL MOVIMIENTO — la ley del planeo (el Ruinous)
        (f32(0.15), '0.15f — la inercia lejos del hueco'),
        (f32(0.35), '0.35f — la amortiguación crítica cerca'),
        (f32(0.25), '0.25f — la velocidad proporcional al hueco'),
        (f32(0.22), '0.22f — el tope de arranque por distancia (y el giro del iris)'),
        (f32(13.0), '13f — la velocidad de crucero del ojo'),
        # LA CORONA VIVA
        (f32(0.20), '0.20f — EL BALANCEO de la corona (±11°)'),
        (f32(1.1), '1.1f — el vaivén propio (5,7 s por ciclo)'),
        (f32(0.23), '0.23f — la respiración del radio'),
        # LAS ÓRBITAS y EL RETROCESO
        (f32(66.0), '66f — el radio de la 1ª órbita'),
        (f32(44.0), '44f — el escalón de las órbitas'),
        (f32(3.2), '3.2f — LA PATADA DEL RETROCESO'),
        # EL SELLO
        (f32(16.0), '16f — el sello r16 (respira a 20 en la carga)'),
        (f32(2.55), '2.55f — el aro del círculo mágico (la casa)'),
        # EL RAYO — el modo ojo del cetro
        (f32(0.30), '0.30f — LA CÁPSULA NEGRA de 19 px (la .105: 352 px — la lección)'),
        (f32(0.55), '0.55f — el ancla pegada a la presa (la bruma lo sigue)'),
        (f32(24.0), '24f — el radio de la bruma en la punta'),
        # EL ICONO — la mirada del ojo de la cajita
        (f32(3.5), '3.5f — la mirada horizontal del ICONO (el modelo del libro)'),
        (f32(2.8), '2.8f — la mirada vertical del icono'),
        # LA REGRESIÓN
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
    cetro = open(f'{ARBOL}/Content/Projectiles/Cosmic/TruenoPerlinProjectile.cs',
                  encoding='utf-8').read()
    furia = open(f'{ARBOL}/Content/Systems/GrimorioFuriaSistema.cs', encoding='utf-8').read()
    gh = open(f'{ARBOL}/Content/Weapons/GrimorioHambriento.cs', encoding='utf-8').read()
    et = open(f'{ARBOL}/Content/Weapons/GrimoireEternal.cs', encoding='utf-8').read()
    for patron, etiqueta, src in [
        # EL OJO — la ley del planeo y la corona viva
        ('alSlot * 0.25f', 'la amortiguación crítica (el ojo SIGUE al hueco)', ojo),
        ('dist * 0.22f', 'el arranque suave por distancia', ojo),
        ('MinionSpeedMult(nivel)', 'la velocidad por nivel', ojo),
        ('MathF.Sin(time * 0.30f) * 0.20f', 'EL BALANCEO del arco de la corona', ojo),
        ('MathF.Sin(time * 1.1f + i * 1.63f) * 3f', 'el vaivén propio de la corona', ojo),
        ('MathF.Sin(time * 0.23f) * 3f', 'el radio de la corona RESPIRA', ojo),
        ('0.32f : -0.26f', 'la contrarrotación de los anillos', ojo),
        ('Projectile.minionPos', 'el reparto de la formación (la corona/órbitas)', ojo),
        # EL DISPARO — el proyectil del cetro
        ('TruenoPerlinProjectile', 'el minion dispara EL PROYECTIL DEL CETRO', ojo),
        ('Projectile.GetSource_FromAI()', 'la fuente del spawn', ojo),
        ('atrás * 3.2f', 'EL RETROCESO y la reincorporación', ojo),
        ('presa.whoAmI', 'el ancla nace en la presa (ai[2])', ojo),
        # EL IRIS y el parpadeo
        ('IRIS_REST', 'el iris en el centro de la almendra', ojo),
        ('VFXCore.Hash01(Projectile.whoAmI, 911', 'el parpadeo INDEPENDIENTE por ojo (hash)', ojo),
        ('fase > 7f || fase < 4f', 'la convención de fases del libro (la .86)', ojo),
        ('OrbitaLib.SigiloErosionado', 'EL CÍRCULO MÁGICO de los agujeros negros', ojo),
        ('VFXCore.ReabrirLoteVanilla()', 'el contrato del lote (la .101)', ojo),
        # EL MODO OJO del cetro
        ('DrawArcoOjo', 'el dibujo del arco del ojo', cetro),
        ('CanalOjo', 'el canal (la matemática del PerlinBolt VERBATIM)', cetro),
        ('BocaDelOjo', 'la boca del arco = EL OJO', cetro),
        ('PresaDelAncla', 'el ancla pegada a la presa', cetro),
        ('RayoStrip.Filamento(canal, 7.5f', 'la funda morada al GROSOR DEL CETRO', cetro),
        ('new Vector2(0.30f, 0.30f)', 'LA CÁPSULA NEGRA de 19 px', cetro),
        ('OjoNegro', 'el núcleo que oscurece', cetro),
        ('SombrasLib.Bruma(Projectile.Center', 'LA BRUMA en la punta pegada al monstruo', cetro),
        ('StormLib.EndCap(Main.spriteBatch, boca', 'el gorro del electrodo del ojo', cetro),
        ('DamageClass.Summon', 'el daño de invocación (el ojo, no la magia)', cetro),
        ('DustID.Shadowflame', 'las chispas de SOMBRA violeta', cetro),
        # EL ICONO de la oleada — el ojo vivo
        ('PasoOjoIcono', 'la máquina del ojo del icono', furia),
        ('SnapDireccionIcono', 'el snap de 8 direcciones (el modelo del libro)', furia),
        ('Main.MouseScreen - posOjoPantalla', 'el iris SIGUE AL CURSOR', furia),
        ('GrimorioHambriento_Medio', 'el párpado del icono (la .86)', furia),
        ('GrimorioHambriento_Iris', 'el iris del icono', furia),
        ('IRIS_ESC', 'la MISMA escala del iris del libro', furia),
        # el libro abre la constante al icono
        ('internal const float IRIS_ESC', 'IRIS_ESC internal (el icono la lee)', gh),
        # la regresión del Eterno
        ('OjoEternoMinion', 'el Eterno invoca LOS OJOS', et),
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
    # la .105 arruinada debe estar AUSENTE del minion
    if 'DrawElRayo' in ojo or 'CanalPerlin' in ojo:
        mal('el minion aún dibuja su propio rayo (la .105)')
    else:
        ok('el minion ya no dibuja el rayo: LO DISPARA (el cetro)')

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

    print('=== 6. REGRESIÓN — LOS MUERTOS AUSENTES + LAS RAWIMG 56×54 ===')
    for ruta in MUERTOS_RUTA:
        if ruta in rutas:
            mal(f'el muerto sigue en el paquete: {ruta}')
    for ruta in ['Content/Weapons/Emperatriz/Equinoccio.rawimg',
                 'Content/Items/Bolsas/BolsaEmperatriz.rawimg',
                 'Content/Projectiles/OjoEterno_Abierto.rawimg',
                 'Content/Projectiles/OjoEterno_Medio.rawimg',
                 'Content/Projectiles/OjoEterno_Cerrado.rawimg',
                 'Content/Projectiles/Cosmic/TruenoPerlinProjectile.rawimg']:
        if ruta in rutas:
            ok(f'vive en el paquete: {ruta}')
        else:
            mal(f'FALTA del paquete: {ruta}')
    # las rawimg de los ojos: EL FORMATO DE LA CASA (12 B de cabecera +
    # RGBA puro) y los PÍXELES == el PNG del árbol decodificado
    import struct as _st
    import numpy as _np
    from PIL import Image as _Img
    for nombre in ('OjoEterno_Abierto', 'OjoEterno_Medio', 'OjoEterno_Cerrado'):
        ruta = f'Content/Projectiles/{nombre}.rawimg'
        if ruta not in rutas:
            mal(f'FALTA {ruta}'); continue
        raw = blobs[ruta]
        fmt, w, h = _st.unpack_from('<3i', raw, 0)
        if len(raw) != 12 + w * h * 4 or (fmt, w, h) != (1, 56, 54):
            mal(f'{nombre}: rawimg malformada ({len(raw)} B, {fmt}×{w}×{h})')
            continue
        png = _np.array(_Img.open(f'{ARBOL}/Content/Projectiles/{nombre}.png').convert('RGBA'))
        paquete = _np.frombuffer(raw[12:], dtype=_np.uint8).reshape(h, w, 4)
        if _np.array_equal(png, paquete):
            ok(f'{nombre}.rawimg 56×54 · píxel-idéntica al árbol ({len(raw)} B)')
        else:
            dif = int((png != paquete).any(axis=2).sum())
            mal(f'{nombre}.rawimg DIFF del árbol ({dif} px distintos)')

    print()
    if FALLOS:
        print(f'✗✗✗ {len(FALLOS)} FALLOS — la .106 NO está lista')
        for f in FALLOS:
            print(f'  - {f}')
        sys.exit(1)
    print('✓✓✓ TODO OK — v6.50.106 AUDITADA')


if __name__ == '__main__':
    main()
