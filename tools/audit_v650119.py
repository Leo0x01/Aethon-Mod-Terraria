#!/usr/bin/env python3
"""
v6.50.119 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO (4 pedidos):
(1) «nunca se fusionaron los jefes ni se hizo todo eso de quedar con
    1000 de vida para una fusion y comenzar una nueva fase» → EL
    RESCATE DE LA FUSIÓN en AethonBoss.CheckDead: la .117 solo veía la
    ventana si un tick de la IA pillaba la vida en (0,1000] — el golpe
    de gracia saltaba de >1000 a ≤0 en el MISMO impacto y el gemelo
    moría limpio (y el otro, sin hermano, tampoco fusionaba jamás).
    AHORA: mientras el hermano viva, el golpe de gracia NO mata — el
    fragmento QUEDA A MIL y entra en órbita; la fusión se decreta al
    tick siguiente. + EL RESET de los estáticos del par al invocar
    (FusionDecretada/TurnoAtaque: el estado huérfano insta-fusionaba a
    la pareja siguiente). + EL GUARD DE LA PRESA MUERTA en la órbita
    (el jugador muerto dejaba a los fragmentos girando eternos).
(2) «ademas los jefes son pequeños, hacer que sean mas grande, al
    menos un 150% mas grande» → EL CUERPO GIGANTE: hitbox 220 → 550
    (×2.5 = un 150% MÁS grande) + TODO el sol de código crece con
    EscalaCuerpo=2.50 (núcleo 130→325, velo 540→1350, halo, coronas,
    chispas, rayos) + el disco del eclipse y las siluetas del ÚLTIMO
    ALIENTO a escala + TODAS las órbitas del duelo escaladas (la de
    flotación, los flancos de los ataques, el círculo veloz, el decreto
    y la del hermano 240→600) — sin envolver a la presa en el reposo.
    Los gemelos nacen a ±600 (los ±300 los dejaba ENCIMADOS).
(3) «crea una bolsa donde esten las mascotas, las 2» → LA BOLSA DE LA
    CRÍA (BolsaDeLaCria): el Recuerdo Completo la suelta SIEMPRE y el
    jefe de luz un 20% — se abre con CLIC DERECHO y entrega al
    AethonMenorItem, el ítem único que engendra a la PAREJA (la cría
    de luz + su gemela oscura: las DOS mascotas). Textura 30×30
    procedural (mitad oro / mitad violeta, la costura del centro).
(4) «el evento de caída no parece activarse, debe activarse al caer
    la primera noche» → LA CAÍDA al ANOCHECER: la ventana pasó de
    time>12000 (pasada la medianoche — el jugador esperaba toda la
    primera mitad de la noche sin ver nada) a time>480 (8 s tras el
    ocaso, el cielo ya es de noche y el cometa cruza AHORA).

CHECKS: cabecera/entradas/EOF (387 = 386 de la .118 + BolsaDeLaCria.
rawimg — los .cs compilan a la DLL, los PNG viajan decodificados como
rawimg: header int32(1)+w+h y píxeles RGBA) · heap + ldstr + constantes
(.104–.119) · EL ÁRBOL (los 4 fixes + la regresión .117/.118 intacta)
· hjson ×3 empaquetado == fuente (CON textos nuevos: BolsaDeLaCria ×3)
· la textura de la bolsa empaquetada == fuente (rawimg byte a byte) ·
iconos SIN cambios.
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

VIVOS_HEAP = [
    # === la regresión .104–.116 (los ojos, la cena, el perezoso, la corona) ===
    'PrimeraCenaProjectile', 'ArrancarPrimeraCena', 'PrimeraCenaPendiente',
    'JefesDevorados', 'EsenciaHambre',
    'OjoEternoMinion', 'OjoEternoBuff',
    'GrimoireEternal', 'GrimorioHambrientoNervioso', 'EstadoGrimorio',
    'EstadoDeLibro', 'TasaHambre', 'GrimorioNerviosoFlotante',
    'SombraPaginaCaza', 'FaucesGlobalNPC', 'AbsorberPresa',
    'HambrePorPresa', 'ReduccionHambre',
    'AethonMenorBuff', 'AethonMenorPet', 'AethonMenorItem',
    'EscuadronEterno', 'TechoOjosEterno', 'TechoOjosDe',
    'GuardianMazmorraDevorado', 'GuardianGracia', 'EditSpawnPool',
    'AislarJefeDeOleada', 'TipoReservado', 'JefeVerdadero',
    # === LA .117 (regresión — La Caída, el gemelo, la fusión) ===
    'CaidaCometaProjectile', 'LaCaidaSistema', 'GrimorioDormido',
    'CaidasPendientes', 'TomarElLibro', 'RegistrarImpacto',
    'SaciedadVuelta',
    'AethonBossOscuro', 'AethonBossFusionado', 'PasoFusionPrevia',
    'EsFusion', 'EstadoFusionado', 'DropBotin',
    'AethonMenorOscuroPet', 'ParejaCerebro', 'TicksMolestado',
    'PuebloChismesNPC',
    # === LA .118 — el acecho, el cometa cerca, el rastro, la cría ===
    'ElegirVictima', 'VecinoCercano', 'EmoteMiedo',
    '_victima', '_tickVictima', '_finIntimidacion', '_anguloMerodeo',
    'EsRocaNatural', 'ExcavarCrater', 'PuntoDeImpacto',
    'DispararCometa', 'CasaDelJugador',
    # === LA .119 — el rescate, el gigante, la bolsa, el anochecer ===
    'EscalaCuerpo',          # la propiedad del sol gigante (×2.5)
    'BolsaDeLaCria',          # la clase de la bolsa
    'CanRightClick',          # el clic derecho de la apertura
]

MUERTOS_HEAP = [
    # === LA .118 — el REFUGIO de la .117 murió (regresión) ===
    'PuntoFueraDelPueblo',
    '_vecinoVigilado',
    # === muertos históricos (regresión) ===
    'AnilloFotones', 'CentroDePantalla', 'BonusMinionSlots',
    'FragmentoGenesis', 'AltarAntiguoTile',
]

VIVOS_US = [
    # === LA .117 (regresión) — las claves de La Caída siguen vivas ===
    'Mods.AethonMod.Caida.CometaCae',
    'Mods.AethonMod.Caida.Impacto',
    'Mods.AethonMod.Caida.PrimerPensamiento',
    'Mods.AethonMod.Mensajes.RegaloPruebas',
    # === LA .116 (regresión) — la voz del escuadrón de once ===
    'Mods.AethonMod.Armas.GuardianDetenido',
    'Mods.AethonMod.Armas.OjosAlMaximo',
    'guardianDevorado', 'jefesDevorados',
    # === LA .119 — el canto de la bolsa abierta ===
    'Mods.AethonMod.Items.BolsaDeLaCria.Abierta',
]

MUERTOS_US = [
    # === LA .118 (regresión) ===
    'Escuadrón de 10 ojos · lleno el escuadrón',
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


def u16(s):
    return s.encode('utf-16-le')


def main():
    d = open(TMOD, 'rb').read()
    md5 = hashlib.md5(d).hexdigest()
    print(f'=== {TMOD} ===')
    print(f'  {len(d)} B | md5 {md5}')

    tmlver, name, ver, entries, tend, fsize, blobs = inflar(TMOD)

    print('=== 1. CABECERA / ENTRADAS / EOF ===')
    if ver == '6.50.119' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    # .119 = .118 + 1 ALTA: BolsaDeLaCria.rawimg (la textura decodificada;
    # el .cs compila dentro de AethonMod.dll — nunca viaja como entrada)
    if len(entries) == 387:
        ok('387 entradas (386 de la .118 + BolsaDeLaCria.rawimg)')
    else:
        mal(f'se esperaban 387 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')
    if any(p == 'Content/Items/Llamados/BolsaDeLaCria.rawimg' for p, _, _ in entries):
        ok('el ALTA de la .119 viaja (BolsaDeLaCria.rawimg — la textura decodificada)')
    else:
        mal('FALTA el alta de la .119 en la tabla de entradas')

    print('=== 2. EL HEAP — REGRESIÓN .104–.118 + LA .119 ===')
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
    for s in VIVOS_US:
        if u16(s) in dll:
            ok(f'ldstr vivo (UTF-16, #US): {s[:60]}')
        else:
            mal(f'FALTA el ldstr (UTF-16): {s[:60]}')
    for s in MUERTOS_US:
        if u16(s) in dll:
            mal(f'el ldstr muerto VIVE: {s[:60]}')
        else:
            ok(f'ldstr muerto ausente: {s[:60]}')

    print('=== 3. LAS CONSTANTES (float32) — REGRESIÓN + LA .119 ===')
    def en_dll(b):
        return b in dll
    constantes = [
        # === la regresión .109–.116 ===
        (f32(0.009), '0.009f — el tirón del perezoso (regresión .112)'),
        (f32(0.88), '0.88f — el freno nuevo (regresión .112)'),
        (f32(10.0), '10f — el tope CERCA + EL TOPE DEL ACECHO (regresión .112 + .118)'),
        (f32(20.0), '20f — el tope LEJOS (regresión .112)'),
        (f32(0.32), '0.32f — el empuje del planeo de vuelta (regresión .112)'),
        (f32(12.0), '12f — el tope del planeo de vuelta (regresión .112)'),
        (f32(0.05), '0.05f — SaciedadVuelta (.117) + HambrePorUso (.110)'),
        (f32(11.0), '11f — EL ESCUADRÓN DE ONCE (regresión .116)'),
        (f32(0.10), '0.10f — el jefe vale 10% (regresión .104)'),
        (f32(430.0), '430f — el cine de la Primera Cena (regresión .110)'),
        # === LA .118 — el cometa cerca, el rastro, la cría, el acecho ===
        (f32(32.0), '32f — el MÍNIMO de tiles CERCA DEL JUGADOR (el cometa)'),
        (f32(64.0), '64f — el MÁXIMO de tiles CERCA DEL JUGADOR (el cometa)'),
        (f32(2.5), '2.5f — el corazón LIMPIO del cráter (el libro duerme ahí)'),
        (f32(0.26), '0.26f — el rastro: la BASE de la probabilidad'),
        (f32(0.34), '0.34f — el rastro: el GRADIENTE hacia el fondo'),
        (f32(0.80), '0.80f — el rastro: 80% ÉBANO / 20% OBSIDIANA'),
        (f32(0.35), '0.35f — el ARO TINTADO al 35% (solo roca natural)'),
        (f32(0.20), '0.20f — el 20% del jefe de luz (GATEADO con !EsFusion)'),
        (f32(800.0), '800f — RADIO_PUEBLO: vecinos a ≤800px del dueño'),
        (f32(850.0), '850f — LA CORREA: jamás lejos del dueño (la letra)'),
        (f32(104.0), '104f — DISTANCIA_VIGILANCIA: la ronda de vigilancia'),
        (f32(44.0), '44f — DISTANCIA_INTIMIDACION: el susto de cerca'),
        (f32(900.0), '900f — TURNO_VIGILANCIA (~15 s por vecino)'),
        (f32(300.0), '300f — DURACION_INTIMIDACION (~5 s mirando fijo)'),
        (f32(360.0), '360f — EL COMPÁS de la intimidación (cada ~6 s)'),
        (f32(0.55), '0.55f — LA MONEDA determinista de la intimidación'),
        (f32(1.25), '1.25f — el margen de rotación de la víctima'),
        (f32(0.012), '0.012f — la DERIVA del merodeo (rad/t)'),
        (f32(0.004), '0.004f — la deriva INTIMANDO (casi quieto)'),
        # === LA .119 — el gigante, el rescate, la bolsa, el anochecer ===
        (f32(550.0), '550f — LA HITBOX GIGANTE (220 → 550: ×2.5, un 150% MÁS grande)'),
        (f32(2.50), '2.50f — EscalaCuerpo: TODO el sol de código crece ×2.5'),
        (f32(480.0), '480f — LA CAÍDA al caer la primera noche (8 s tras el ocaso)'),
        (f32(600.0), '600f — los gemelos nacen a ±600 + la órbita del hermano 240·2.5'),
        (f32(100.0), '100f — el jitter del nacimiento (±600 ±100)'),
    ]
    for bytes_, etiqueta in constantes:
        if en_dll(bytes_):
            ok(f'{etiqueta} vive en el IL')
        else:
            mal(f'FALTA en la DLL: {etiqueta}')

    print('=== 4. EL ÁRBOL — LOS 4 FIXES + LA REGRESIÓN .117/.118 ===')
    def leer(ruta):
        return open(f'{ARBOL}/{ruta}', encoding='utf-8').read()
    caida = leer('Content/Systems/LaCaidaSistema.cs')
    nerv = leer('Content/Projectiles/Nervioso/GrimorioNerviosoFlotante.cs')
    jefe = leer('Content/NPCs/AethonBoss.cs')
    fusion = leer('Content/NPCs/AethonBossFusionado.cs')
    llamado = leer('Content/Items/Llamados/LlamadosJefesItems.cs')
    bolsa = leer('Content/Items/Llamados/BolsaDeLaCria.cs')
    oscuro = leer('Content/NPCs/AethonBossOscuro.cs')

    # === (1) EL RESCATE DE LA FUSIÓN (la letra: «quedar con 1000 de vida») ===
    if 'EL RESCATE DE LA FUSIÓN' in jefe and \
       'hermano.ai[0] != EST_MURIENDO' in jefe and \
       'NPC.life = VidaFusion;          // queda a MIL, no muere' in jefe:
        ok('EL RESCATE en CheckDead: el golpe de gracia NO mata — queda a MIL')
    else:
        mal('FALTA el rescate de la fusión en CheckDead')
    if 'LA PRESA MUERTA TAMBIÉN ROMPE LA ÓRBITA' in jefe:
        ok('la órbita se rompe con la presa muerta (no más girando eternos)')
    else:
        mal('FALTA el guard de la presa muerta en la órbita')
    if 'AethonBoss.FusionDecretada = false;' in llamado and \
       'AethonBoss.TurnoAtaque = -1;' in llamado:
        ok('el RESET de los estáticos del par al invocar (el estado huérfano murió)')
    else:
        mal('FALTA el reset de FusionDecretada/TurnoAtaque en el llamado')

    # === (2) EL CUERPO GIGANTE (×2.5 = un 150% MÁS grande) ===
    if 'NPC.width = 550;' in jefe and 'NPC.height = 550;' in jefe:
        ok('la hitbox 550×550 (220 → 550: un 150% más grande)')
    else:
        mal('FALTA la hitbox gigante')
    if 'EscalaCuerpo => 2.50f' in jefe:
        ok('EscalaCuerpo 2.50 (TODO el sol de código ×2.5)')
    else:
        mal('FALTA EscalaCuerpo')
    if '(240f * EscalaCuerpo)' in jefe:
        ok('la órbita del hermano escalada (240 → 600: el pre-abrazo)')
    else:
        mal('FALTA la órbita del hermano escalada')
    if '540f * EscalaCuerpo' in jefe and '130f * EscalaCuerpo' in jefe:
        ok('el velo y el núcleo del sol escalados (540→1350, 130→325)')
    else:
        mal('FALTA el escalado del cuerpo en el PreDraw')
    if '150f * EscalaCuerpo' in fusion and '96f * EscalaCuerpo' in fusion:
        ok('el disco del eclipse del Recuerdo Completo escalado')
    else:
        mal('FALTA el escalado del disco del eclipse')
    if oscuro.count('base.SetDefaults()') >= 1 and 'lifeMax = 1_400_000' in oscuro:
        ok('el gemelo oscuro hereda el cuerpo gigante (base.SetDefaults)')
    else:
        mal('el gemelo oscuro NO hereda')

    # === (3) LA BOLSA DE LA CRÍA (las DOS mascotas adentro) ===
    if 'ModContent.ItemType<Items.Llamados.BolsaDeLaCria>()' in fusion:
        ok('el Recuerdo Completo suelta LA BOLSA SIEMPRE')
    else:
        mal('FALTA la bolsa en el botín del fusionado')
    if 'ModContent.ItemType<Items.Llamados.AethonMenorItem>()' not in fusion:
        ok('el drop directo del ítem crudo MURIÓ en el fusionado (ahora es la bolsa)')
    else:
        mal('el fusionado aún suelta el ítem crudo (doble vía)')
    if 'ModContent.ItemType<Items.Llamados.BolsaDeLaCria>()' in jefe:
        ok('el 20% del jefe de luz también viaja EN SU BOLSA')
    else:
        mal('FALTA la bolsa en el 20% del jefe de luz')
    if 'CanRightClick() => true' in bolsa and \
       'ModContent.ItemType<AethonMenorItem>()' in bolsa:
        ok('la bolsa abre con CLIC DERECHO y entrega al ítem de la PAREJA')
    else:
        mal('FALTA la apertura de la bolsa')

    # === (4) LA CAÍDA AL CAER LA PRIMERA NOCHE ===
    if 'Main.time > 480.0' in caida and 'NOCHE RECÉN CAÍDA' in caida:
        ok('el cometa cruza 8 s tras el ocaso (time > 480, la medianoche murió)')
    else:
        mal('FALTA la ventana del anochecer')
    if 'Main.time > 12000.0' not in caida:
        ok('la medianoche de la .117 murió')
    else:
        mal('la ventana vieja time>12000 sigue VIVA')

    # === LA REGRESIÓN .117/.118 intacta ===
    if 'if (!EsFusion && Main.rand.NextFloat() < 0.20f)' in jefe:
        ok('el 20% del jefe de luz GATEADO con !EsFusion (regresión .118)')
    else:
        mal('FALTA el gate !EsFusion del 20%')
    if 'PuntoDeImpacto(Player p)' in caida and 'p.Center.X + lado * distTiles * 16f' in caida:
        ok('el cometa ANCLADO AL JUGADOR 32–64 tiles (regresión .118)')
    else:
        mal('FALTA el anclaje del cometa al jugador')
    if 'TileID.Ebonstone' in leer('Content/Projectiles/CaidaCometaProjectile.cs'):
        ok('el rastro del cráter con ébano/obsidiana (regresión .118)')
    else:
        mal('FALTA el rastro del cráter')
    if 'ElegirVictima' in nerv and 'EmoteID.EmoteFear' in nerv and 'CORREA_DUENO' in nerv:
        ok('el acecho del pueblo intacto (regresión .118)')
    else:
        mal('FALTA el acecho del pueblo')
    if 'SaciedadVuelta = 0.05f' in nerv:
        ok('SaciedadVuelta intacta (regresión .117)')
    else:
        mal('SaciedadVuelta ROTA')

    print('=== 5. HJSON ×3 EMPAQUETADO == FUENTE (con los textos nuevos) ===')
    for nombre in ('es-MX', 'en-US', 'es-ES'):
        ruta = f'Localization/{nombre}_Mods.AethonMod.hjson'
        fuente = open(f'{ARBOL}/{ruta}', 'rb').read()
        if ruta in blobs:
            if blobs[ruta] == fuente:
                ok(f'{ruta}: empaquetado == fuente (byte a byte)')
            else:
                mal(f'{ruta}: el empaquetado DIFIERE de la fuente')
        else:
            mal(f'{ruta} NO VIAJA en el .tmod')
        if 'BolsaDeLaCria' in fuente.decode('utf-8'):
            ok(f'{nombre}: la entrada BolsaDeLaCria vive en la fuente')
        else:
            mal(f'{nombre}: FALTA la entrada BolsaDeLaCria')

    print('=== 6. LA TEXTURA DE LA BOLSA — EMPAQUETADA == FUENTE ===')
    from PIL import Image
    png_ruta = 'Content/Items/Llamados/BolsaDeLaCria.png'
    raw_ruta = 'Content/Items/Llamados/BolsaDeLaCria.rawimg'
    img = Image.open(f'{ARBOL}/{png_ruta}').convert('RGBA')
    esperado = struct.pack('<iii', 1, img.size[0], img.size[1]) + img.tobytes()
    if blobs.get(raw_ruta) == esperado:
        ok(f'{raw_ruta}: empaquetada == fuente ({len(esperado)} B, header 1+w+h + RGBA, byte a byte)')
    else:
        mal(f'{raw_ruta}: no viaja o difiere de la fuente')

    print('=== 7. LOS ICONOS DEL USUARIO — SIN CAMBIOS ===')
    if blobs.get('icon.png') == open(f'{ARBOL}/icon.png', 'rb').read() and \
       blobs.get('icon_small.png') == open(f'{ARBOL}/icon_small.png', 'rb').read():
        ok('icon.png 80×80 + icon_small.png 30×30 intactos (PNG crudos)')
    else:
        mal('los iconos del usuario cambiaron')

    print()
    if FALLOS:
        print(f'=== AUDITORÍA v6.50.119: {len(FALLOS)} FALLOS ===')
        for f in FALLOS:
            print(f'  ✗ {f}')
        sys.exit(1)
    print('=== AUDITORÍA v6.50.119: TODO OK ===')


if __name__ == '__main__':
    main()
