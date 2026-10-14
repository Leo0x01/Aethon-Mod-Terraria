#!/usr/bin/env python3
"""
v6.50.118 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO (4 pedidos):
(1) «Asegúrate de que el jugador pueda conseguir las mascotas» → LA
    CRÍA GEMELA GARANTIZADA: el Recuerdo Completo (AethonBossFusionado.
    DropBotin) deja al AethonMenorItem SIEMPRE — y el 20% de azar del
    jefe de luz queda GATEADO con !EsFusion (sin el gate caerían DOS).
    El mismo ítem engendra a la pareja (luz + gemela oscura).
(2) «el cometa de tinta caiga cerca del jugador» → PuntoDeImpacto
    ANCLADO AL JUGADOR: 32–64 tiles a un lado (la .117 lo medía desde
    el SPAWN, ~30 pantallas — un jugador con base lejos se perdía la
    caída); superficie de esa columna y fallback la altura DEL JUGADOR.
(3) «el cráter debe dejar algún rastro de algún bloque oscuro, en el
    juego de terraria hay métodos para eso, no necesita bloques nuevos
    se pueden reutilizar los bloques existentes» → EL RASTRO en
    ExcavarCrater: PIEDRA DE ÉBANO (80%, el bloque oscuro de la
    Corrupción — TileID.Ebonstone=25 verificado al IL de ESTE tML) y
    OBSIDIANA (20%, TileID.Obsidian=56), esparcido con gradiente hacia
    el fondo (0.26+0.34·dentro), el corazón limpio (d≥2.5: el libro
    duerme ahí) y EL ARO TINTADO (los bordes que no se abrieron
    amanecen ébano al 35% — solo sobre roca natural). Todo con
    SquareTileFrame y el SendTileSquare de siempre.
(4) «eso de que el grimorio sale del pueblo esta mal, el grimorio no
    debe salir del área del jugador, siempre debe estar cerca del
    jugador, lo que debe hacer es que si el jugador está en el pueblo,
    el grimorio sale como si fuera a cazar pero lo que hace es solo
    vigilar a los npc y acosarlos aterradormente» → EL ACECHO DEL
    PUEBLO (GrimorioNerviosoFlotante): EL REFUGIO de la .117 (Punto-
    FueraDelPueblo, ~900px FUERA, dirección opuesta al vecino) MURIÓ.
    Ahora: LA VÍCTIMA (ElegirVictima rota cada TURNO_VIGILANCIA=900 t,
    ~15 s por vecino — el terror se reparte), LA RONDA DE VIGILANCIA
    (merodeo en elipse achatada a 104 px, deriva 0.012 rad/t), LA
    INTIMIDACIÓN (cada 360 t, moneda determinista Hash01<0.55: se
    planta a 44 px mirando FIJO 300 t con el EMOTE DEL SUSTO en el
    vecino — EmoteBubble/EmoteID.EmoteFear de Terraria.GameContent.UI)
    y LA CORREA (el punto de la ronda JAMÁS a más de 850 px del dueño).
    Sin sombras en el pueblo (solo vigilar), la mirada/iris clavados
    en la víctima (AI y PreDraw) y la víctima se limpia al salir del
    pueblo o al volverse (MODO_VOLVER).

CHECKS: cabecera/entradas/EOF (386, los mismos nombres de la .117:
solo cambia código) · heap + ldstr + constantes (.104–.118) · EL
ÁRBOL (los 4 fixes + la regresión .117 intacta: SaciedadVuelta, el
ladrillo, el guardián) · hjson ×3 empaquetado == fuente (CERO textos
nuevos esta versión) · iconos SIN cambios.
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
    'SaciedadVuelta', 'SaciedadVuelta',      # public const → vive en el heap si se referencia
    'AethonBossOscuro', 'AethonBossFusionado', 'PasoFusionPrevia',
    'EsFusion', 'EstadoFusionado', 'DropBotin',
    'AethonMenorOscuroPet', 'ParejaCerebro', 'TicksMolestado',
    'PuebloChismesNPC',
    # === LA .118 — EL ACECHO, EL COMETA CERCA, EL RASTRO, LA CRÍA ===
    'ElegirVictima', 'VecinoCercano', 'EmoteMiedo',
    '_victima', '_tickVictima', '_finIntimidacion', '_anguloMerodeo',
    'EsRocaNatural', 'ExcavarCrater', 'PuntoDeImpacto',
    'DispararCometa', 'CasaDelJugador',
]

MUERTOS_HEAP = [
    # === LA .118 — EL REFUGIO DE LA .117 MURIÓ (el libro YA NO sale del pueblo) ===
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
]

MUERTOS_US = [
    # === LA .118 — CERO textos nuevos: nada que muera en #US ===
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
    if ver == '6.50.118' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    # .118 == .117 en NOMBRES (386): solo cambia CÓDIGO, cero altas/bajas
    if len(entries) == 386:
        ok('386 entradas (los mismos nombres de la .117: solo cambia el código)')
    else:
        mal(f'se esperaban 386 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. EL HEAP — REGRESIÓN .104–.117 + LA .118 ===')
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

    print('=== 3. LAS CONSTANTES (float32) — REGRESIÓN + LA .118 ===')
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
        # === LA .118 — EL COMETA CERCA ===
        (f32(32.0), '32f — el MÍNIMO de tiles CERCA DEL JUGADOR (el cometa)'),
        (f32(64.0), '64f — el MÁXIMO de tiles CERCA DEL JUGADOR (el cometa)'),
        # === LA .118 — EL RASTRO DEL CRÁTER ===
        (f32(2.5), '2.5f — el corazón LIMPIO del cráter (el libro duerme ahí)'),
        (f32(0.26), '0.26f — el rastro: la BASE de la probabilidad'),
        (f32(0.34), '0.34f — el rastro: el GRADIENTE hacia el fondo'),
        (f32(0.80), '0.80f — el rastro: 80% ÉBANO / 20% OBSIDIANA'),
        (f32(0.35), '0.35f — el ARO TINTADO al 35% (solo roca natural)'),
        # === LA .118 — LA CRÍA GARANTIZADA ===
        (f32(0.20), '0.20f — el 20% del jefe de luz (GATEADO con !EsFusion)'),
        # === LA .118 — EL ACECHO DEL PUEBLO ===
        (f32(800.0), '800f — RADIO_PUEBLO: vecinos a ≤800px del dueño'),
        (f32(850.0), '850f — LA CORREA: jamás lejos del dueño (la letra)'),
        (f32(104.0), '104f — DISTANCIA_VIGILANCIA: la ronda de vigilancia'),
        (f32(44.0), '44f — DISTANCIA_INTIMIDACION: el susto de cerca'),
        (f32(900.0), '900f — TURNO_VIGILANCIA (~15 s por vecino; DISTANCIA_ESPERA murió)'),
        (f32(300.0), '300f — DURACION_INTIMIDACION (~5 s mirando fijo)'),
        (f32(360.0), '360f — EL COMPÁS de la intimidación (cada ~6 s)'),
        (f32(0.55), '0.55f — LA MONEDA determinista de la intimidación'),
        (f32(1.25), '1.25f — el margen de rotación de la víctima'),
        (f32(0.012), '0.012f — la DERIVA del merodeo (rad/t)'),
        (f32(0.004), '0.004f — la deriva INTIMANDO (casi quieto)'),
    ]
    for bytes_, etiqueta in constantes:
        if en_dll(bytes_):
            ok(f'{etiqueta} vive en el IL')
        else:
            mal(f'FALTA en la DLL: {etiqueta}')

    print('=== 4. EL ÁRBOL — LOS 4 FIXES + LA REGRESIÓN .117 ===')
    def leer(ruta):
        return open(f'{ARBOL}/{ruta}', encoding='utf-8').read()
    caida = leer('Content/Systems/LaCaidaSistema.cs')
    cometa = leer('Content/Projectiles/CaidaCometaProjectile.cs')
    nerv = leer('Content/Projectiles/Nervioso/GrimorioNerviosoFlotante.cs')
    jefe = leer('Content/NPCs/AethonBoss.cs')
    fusion = leer('Content/NPCs/AethonBossFusionado.cs')

    # (1) LA CRÍA GARANTIZADA
    if 'if (!EsFusion && Main.rand.NextFloat() < 0.20f)' in jefe:
        ok('el 20% del jefe de luz GATEADO con !EsFusion (sin duplicados)')
    else:
        mal('FALTA el gate !EsFusion del 20%')
    if fusion.count('AethonMenorItem') >= 1 and 'LA CRÍA GEMELA — SIEMPRE' in fusion:
        ok('el Recuerdo Completo deja LA CRÍA SIEMPRE (vía canónica de la mascota)')
    else:
        mal('FALTA la cría garantizada del fusionado')

    # (2) EL COMETA CERCA
    if 'PuntoDeImpacto(Player p)' in caida and 'p.Center.X + lado * distTiles * 16f' in caida:
        ok('el punto de impacto ANCLADO AL JUGADOR (32–64 tiles)')
    else:
        mal('FALTA el anclaje del cometa al jugador')
    if '30f * 1920f' not in caida and 'spawn + dir' not in caida:
        ok('las ~30 pantallas del SPAWN murieron')
    else:
        mal('el destino del spawn viejo VIVE')
    if 'destino.Y = p.Center.Y;' in caida:
        ok('el fallback de la superficie es la altura DEL JUGADOR')
    else:
        mal('FALTA el fallback del jugador')

    # (3) EL RASTRO DEL CRÁTER
    if 'TileID.Ebonstone' in cometa and 'TileID.Obsidian' in cometa:
        ok('EL RASTRO: ébano + obsidiana (bloques EXISTENTES, sin nuevos)')
    else:
        mal('FALTA el rastro de bloques oscuros')
    if 'WorldGen.SquareTileFrame(x, y, true)' in cometa:
        ok('el rastro enmarca con SquareTileFrame (el método de vanilla)')
    else:
        mal('FALTA el SquareTileFrame del rastro')
    if 'EsRocaNatural' in cometa and 'EsCasaDelJugador' in cometa:
        ok('la casa del jugador intacta + el aro solo sobre roca natural')
    else:
        mal('FALTAN los guards del rastro')

    # (4) EL ACECHO DEL PUEBLO
    if 'ElegirVictima' in nerv and 'EmoteMiedo' in nerv and 'EmoteID.EmoteFear' in nerv:
        ok('el ACECHO: víctima elegida + el emote del susto (EmoteFear)')
    else:
        mal('FALTA el acecho completo')
    if 'PuntoFueraDelPueblo' in nerv or '_vecinoVigilado' in nerv:
        mal('el REFUGIO de la .117 sigue VIVO en el árbol')
    else:
        ok('el REFUGIO de la .117 murió (el libro ya NO sale del pueblo)')
    if 'CORREA_DUENO' in nerv and 'largo > CORREA_DUENO' in nerv:
        ok('LA CORREA: el destino clampado al área del jugador')
    else:
        mal('FALTA la correa del dueño')
    if 'if (!enPueblo && autoridad' in nerv:
        ok('SIN sombras en el pueblo (solo vigilar — la letra)')
    else:
        mal('FALTA el gate de caza del pueblo')
    if 'VFXCore.Hash01' in nerv:
        ok('la moneda determinista de la intimidación (Hash01, no Main.rand)')
    else:
        mal('FALTA la moneda determinista')
    if 'SaciedadVuelta = 0.05f' in nerv and 'hambre <= SaciedadVuelta' in nerv:
        ok('la regresión .117 intacta: SaciedadVuelta decide la vuelta')
    else:
        mal('SaciedadVuelta ROTA')

    print('=== 5. HJSON ×3 EMPAQUETADO == FUENTE (CERO textos nuevos) ===')
    for nombre in ('es-MX', 'en-US', 'es-ES'):
        ruta = f'Localization/{nombre}_Mods.AethonMod.hjson'
        fuente = open(f'{ARBOL}/{ruta}', 'rb').read()
        if ruta in blobs:
            if blobs[ruta] == fuente:
                ok(f'{ruta}: empaquetado == fuente (byte a byte)')
            else:
                mal(f'{ruta}: el empaquetado DIFIERE de la fuente')
        else:
            # tML puede normalizar a .json? No: viaja .hjson (lección .55/.117)
            mal(f'{ruta} NO VIAJA en el .tmod')

    print('=== 6. LOS ICONOS DEL USUARIO — SIN CAMBIOS ===')
    if blobs.get('icon.png') == open(f'{ARBOL}/icon.png', 'rb').read() and \
       blobs.get('icon_small.png') == open(f'{ARBOL}/icon_small.png', 'rb').read():
        ok('icon.png 80×80 + icon_small.png 30×30 intactos (PNG crudos)')
    else:
        mal('los iconos del usuario cambiaron')

    print()
    if FALLOS:
        print(f'=== AUDITORÍA v6.50.118: {len(FALLOS)} FALLOS ===')
        for f in FALLOS:
            print(f'  ✗ {f}')
        sys.exit(1)
    print('=== AUDITORÍA v6.50.118: TODO OK ===')


if __name__ == '__main__':
    main()
