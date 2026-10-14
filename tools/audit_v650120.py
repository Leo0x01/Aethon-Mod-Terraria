#!/usr/bin/env python3
"""
v6.50.120 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO (Task 77):
(1) «los jefes se alejan mucho del jugador» → LA CORREA DEL DUELO:
    la órbita de reposo bajó de 650-1100 px (×2.5 del cuerpo gigante) a
    300-420 px absolutos + altura −300, y NINGÚN estado deja al dios a
    más de CORREA_DUENO=820 px de la presa (el empuje vive en AI, tras
    el switch de estados — no aplica en órbita/fusión/muerte/llegada).
(2) «no se activa la mecanica, pongamosla como una fase, cuando tengan
    cierta cantidad de vida se activa y ambos se fucionen haciendose
    mas grandes, mas epicos» → LA FUSIÓN ES UNA FASE: el umbral de los
    1000 ABSOLUTOS exigía vaciar el 99,96% de 4,8 M de vida combinada —
    AHORA, cuando la vida COMBINADA del par baja al 50% (o la red .119
    atrapa un overkill), LA FUSIÓN SE DECRETA con anuncio épico mundial
    (Jefe.Aethon.Fusion) y ambos pasan a la cinemática intocables.
    Y EL RECUERDO COMPLETO ES UN JEFE FINAL DE VERDAD: EscalaCuerpo
    3.40 (los gemelos 2.50), hitbox 770 (48 tiles), 3_000_000 de vida,
    LA NOCHE DEL ECLIPSE (Ilumina=false + penumbra violeta + relámpagos
    cada 3-5 s + cada fase ABRE con trueno) y TRES ARMAS INVESTIGADAS
    en los finales de Terraria, destapadas con la furia de la fase:
    EL JUICIO FANTASMAL (P5, Moon Lord: dos ruedas de rayos cruzados),
    EL CONCILIO DE ESPEJOS (P3+, Lunatic Cultist: círculos de pernos
    que cargan quietos y convergen donde estabas — estilo NUEVO
    EstiloRitualConvergente) y LA TORMENTA DEL ABISMO (P4+, Duke
    Fishron: tres vórtices que acechan escupiendo espirales).
(3) «ademas no tengo las bolsa, no olvides que debes darcelo al
    jugador» → LA BOLSA A LA MANO: el 20% de azar MURIÓ; la primera
    victoria de la familia (gemelo final o Recuerdo Completo) ENTREGA
    LA BOLSA DE LA CRÍA directamente al inventario del verdugo — una
    sola vez por jugador para siempre (flag BolsaCriaEntregada
    persistido en el .plr), con fanfarria de chispas doradas/moradas y
    el anuncio Jefe.BolsaCria.Entregada.

CHECKS: cabecera/entradas/EOF (387 — sin altas: la .120 solo modifica
.cs/.hjson existentes) · heap + ldstr + constantes (.104–.120) · EL
ÁRBOL (los 3 fixes + la regresión .117-.119 intacta) · hjson ×3
empaquetado == fuente (CON las 5 claves nuevas ×3 idiomas).
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
    # === la regresión .104–.119 (lo esencial vivo) ===
    'GrimoireEternal', 'GrimorioNerviosoFlotante', 'LaCaidaSistema',
    'CaidaCometaProjectile', 'GrimorioDormido',
    'AethonBossOscuro', 'AethonBossFusionado', 'PasoFusionPrevia',
    'EsFusion', 'EstadoFusionado', 'DropBotin',
    'ElegirVictima', 'EmoteMiedo', 'EsRocaNatural', 'ExcavarCrater',
    'EscalaCuerpo', 'BolsaDeLaCria', 'CanRightClick',
    'AethonMenorItem', 'ParejaCerebro', 'PuebloChismesNPC',
    # === LA .120 — la correa, la fase, la bolsa a la mano, el coloso ===
    'BolsaCriaEntregada',       # el FLAG per-player persistido (campo público)
    '_anclasTormenta',          # los TRES vórtices del Duque
    '_tormentaSembrada',        # el sembrado único del estado
    '_angTormenta',             # la espiral que gira (+0.62/ráfaga)
    '_tickRelampago',           # el cielo del eclipse (relámpagos 3-5 s)
    'RelampagoFondo',           # la vena dorada + el trueno grave
    'JuicioFantasmal',          # Moon Lord: las dos ruedas cruzadas
    'ConcilioEspejos',          # el Cultista: el círculo que converge
    'TormentaAbismo',           # el Duque: los vórtices que acechan
    'RitualTick',               # el perno del concilio (carga 55 t → vuela)
]

MUERTOS_HEAP = [
    # === muertos históricos (regresión) ===
    'PuntoFueraDelPueblo', '_vecinoVigilado',
    'AnilloFotones', 'CentroDePantalla', 'BonusMinionSlots',
    'FragmentoGenesis', 'AltarAntiguoTile',
]

VIVOS_US = [
    # === LA .120 — las claves nuevas del jefe final ===
    'Mods.AethonMod.Jefe.Aethon.Fusion',
    'Mods.AethonMod.Jefe.BolsaCria.Entregada',
    'Mods.AethonMod.Jefe.Aethon.Rayo',
    'Mods.AethonMod.Jefe.Aethon.Ritual',
    'Mods.AethonMod.Jefe.Aethon.Tormenta',
    'bolsaCriaEntregada',       # el tag del .plr
    # === la regresión .117-.119 (claves vivas) ===
    'Mods.AethonMod.Caida.CometaCae',
    'Mods.AethonMod.Items.BolsaDeLaCria.Abierta',
    'Mods.AethonMod.Jefe.Aethon.Decreto',
]

MUERTOS_US = []


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
    if ver == '6.50.120' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    # .120 = .119 SIN altas ni bajas (solo .cs/.hjson modificados)
    if len(entries) == 387:
        ok('387 entradas (igual que la .119 — sin altas: la .120 solo modifica)')
    else:
        mal(f'se esperaban 387 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')
    if any(p == 'Content/Items/Llamados/BolsaDeLaCria.rawimg' for p, _, _ in entries):
        ok('BolsaDeLaCria.rawimg sigue viajando (la .119 intacta)')
    else:
        mal('FALTA BolsaDeLaCria.rawimg')

    print('=== 2. EL HEAP — REGRESIÓN + LA .120 ===')
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

    print('=== 3. LAS CONSTANTES — REGRESIÓN + LA .120 ===')
    def en_dll(b):
        return b in dll
    constantes = [
        # === la regresión .109–.119 (lo esencial) ===
        (f32(0.05), '0.05f — SaciedadVuelta (.117)'),
        (f32(550.0), '550f — la hitbox gemela gigante (.119)'),
        (f32(2.50), '2.50f — EscalaCuerpo de los gemelos (.119)'),
        (f32(480.0), '480f — La Caída al anochecer (.119)'),
        (f32(850.0), '850f — LA CORREA del grimorio en el pueblo (.118)'),
        (f32(32.0), '32f — el cometa CERCA del jugador (.118)'),
        # === LA .120 — la correa, la fase, el coloso, las armas ===
        (f32(820.0), '820f — CORREA_DUENO: el duelo jamás a media pantalla'),
        (f32(0.50), '0.50f — LA FASE: la vida combinada del par al 50%'),
        (f32(3.40), '3.40f — EscalaCuerpo del RECUERDO COMPLETO (el coloso)'),
        (f32(2.2), '2.2f — el avance de los vórtices de la tormenta'),
        (f32(0.62), '0.62f — el giro de la espiral de ráfagas'),
        (f32(11.5), '11.5f — la velocidad del perno del concilio'),
        (f32(640.0), '640f — la altura del eclipse gemelo (coloso)'),
        (f32(680.0), '680f — la altura del sol moribundo/concilio (coloso)'),
        (f32(700.0), '700f — la altura de la rejilla/juicio (coloso)'),
        (f32(600.0), '600f — el cerco de la tormenta jamás cierra a <600'),
        (struct.pack('<i', 770), '770 (i4) — width/height del coloso'),
        (struct.pack('<i', 3000000), '3_000_000 (i4) — lifeMax del coloso'),
    ]
    for bytes_, etiqueta in constantes:
        if en_dll(bytes_):
            ok(f'{etiqueta} vive en el IL')
        else:
            mal(f'FALTA en la DLL: {etiqueta}')

    print('=== 4. EL ÁRBOL — LOS 3 FIXES + LA REGRESIÓN .117-.119 ===')
    def leer(ruta):
        return open(f'{ARBOL}/{ruta}', encoding='utf-8').read()
    jefe = leer('Content/NPCs/AethonBoss.cs')
    fusion = leer('Content/NPCs/AethonBossFusionado.cs')
    fusproj = leer('Content/Projectiles/Jefes/AtaqueFusionProjectile.cs')
    shard = leer('Content/Players/ShardPlayer.cs')

    # === (1) LA CORREA DEL DUELO ===
    if 'CORREA_DUENO = 820f' in jefe and 'distDuelo > CORREA_DUENO' in jefe:
        ok('LA CORREA: 820 px máx + el empuje tras el switch de estados')
    else:
        mal('FALTA la correa del duelo')
    if 'Furia ? 300f : (Phase >= 4 ? 360f : 420f)' in jefe:
        ok('la órbita de reposo: 300/360/420 absolutos (era 650-1100)')
    else:
        mal('FALTA la órbita de reposo cercana')

    # === (2) LA FUSIÓN ES UNA FASE ===
    if 'vidaPar <= maxPar * 0.50f' in jefe and 'bool rescatado' in jefe:
        ok('LA FASE: vida combinada ≤50% (o la red .119) DECRETA la fusión')
    else:
        mal('FALTA la fase de fusión al 50% combinado')
    if 'Jefe.Aethon.Fusion' in jefe:
        ok('el ANUNCIO ÉPICO de la fusión (la clave .120)')
    else:
        mal('FALTA el anuncio de la fusión')
    if jefe.find('if (FusionDecretada)') < jefe.find('if (NPC.life > VidaFusion) return;'):
        ok('el DECRETO manda sobre la órbita (orden: fase → cine → red)')
    else:
        mal('el orden del decreto está mal')

    # === (2b) EL COLOSO Y LAS ARMAS DEL FINAL ===
    if 'EscalaCuerpo => 3.40f' in fusion and 'NPC.width = 770' in fusion \
       and 'lifeMax = 3_000_000' in fusion:
        ok('EL COLOSO: 3.40 / 770 / 3M (más grande y más fuerte)')
    else:
        mal('FALTA el coloso del Recuerdo Completo')
    if 'Ilumina => false' in fusion and 'RelampagoFondo' in fusion:
        ok('LA NOCHE DEL ECLIPSE: sin sol + relámpagos de fondo')
    else:
        mal('FALTA la noche del eclipse')
    if 'F_RAYOS = 36' in fusion and 'F_RITUAL = 37' in fusion \
       and 'F_TORMENTA = 38' in fusion:
        ok('LAS TRES ARMAS DEL FINAL: Juicio/Concilio/Tormenta')
    else:
        mal('FALTAN los estados de las armas nuevas')
    if 'EstiloRitualConvergente = 3' in fusproj and 'RitualTick' in fusproj:
        ok('el PERNO DEL CONCILIO: estilo nuevo (carga → converge)')
    else:
        mal('FALTA el estilo del perno convergente')
    if fusion.count('case F_RAYOS') == 1 and fusion.count('case F_RITUAL') == 1 \
       and fusion.count('case F_TORMENTA') == 1:
        ok('los TRES casos despachan en EstadoFusionado')
    else:
        mal('FALTA el despacho de las armas nuevas')

    # === (3) LA BOLSA A LA MANO ===
    if '!sp.BolsaCriaEntregada' in jefe and 'aLaMano' in jefe:
        ok('LA ENTREGA: la bolsa DIRECTO al inventario del verdugo')
    else:
        mal('FALTA la entrega de la bolsa a la mano')
    if 'NextFloat() < 0.20f' not in jefe.split('v6.50.120 — EL 20% DE AZAR MURIÓ')[0][-4000:]:
        ok('el 20% de azar MURIÓ (la letra: «debes dárselo al jugador»)')
    else:
        mal('el 20% de azar sigue vivo ANTES del bloque .120')
    if 'public bool BolsaCriaEntregada = false;' in shard \
       and 'tag["bolsaCriaEntregada"]' in shard:
        ok('el FLAG persistido en el .plr (una vez por jugador)')
    else:
        mal('FALTA el flag persistido de la bolsa')

    # === la regresión .117-.119 intacta ===
    if 'EL RESCATE DE LA FUSIÓN' in jefe and 'NPC.life = VidaFusion;' in jefe:
        ok('regresión: el RESCATE de CheckDead intacto (la red .119)')
    else:
        mal('FALTA el rescate de la .119')
    if 'hermano.ai[0] != EST_MURIENDO' in jefe:
        ok('regresión: el guard del cine del hermano')
    else:
        mal('FALTA el guard del hermano en el rescate')
    if fusion.count('EstiloSolMoribundo') >= 1 and 'EstiloRejillaLaser' in fusion:
        ok('regresión: el moveset clásico del fusionado intacto')
    else:
        mal('FALTA el moveset clásico')

    print('=== 5. HJSON ×3 — EMPAQUETADO == FUENTE (con las claves .120) ===')
    hjsons = [
        ('Localization/es-MX_Mods.AethonMod.hjson', 'es-MX'),
        ('Localization/es-ES_Mods.AethonMod.hjson', 'es-ES'),
        ('Localization/en-US_Mods.AethonMod.hjson', 'en-US'),
    ]
    claves120 = ['Fusion:', 'Rayo:', 'Ritual:', 'Tormenta:', 'Entregada:']
    for ruta, idioma in hjsons:
        fuente = open(f'{ARBOL}/{ruta}', 'rb').read()
        clave = f'Localization/{idioma}_Mods.AethonMod.hjson'
        if clave not in blobs:
            mal(f'{idioma}: no viaja en el .tmod')
            continue
        if blobs[clave] == fuente:
            ok(f'{idioma}: empaquetado == fuente BYTE A BYTE')
        else:
            mal(f'{idioma}: el hjson empaquetado DIFIERE de la fuente')
        texto = fuente.decode('utf-8')
        faltan = [c for c in claves120 if c not in texto]
        if not faltan:
            ok(f'{idioma}: las 5 claves .120 presentes')
        else:
            mal(f'{idioma}: FALTAN {faltan}')

    print('=== 6. MD5 FINAL ===')
    print(f'  md5: {md5}')

    print()
    if FALLOS:
        print(f'TODO MAL: {len(FALLOS)} fallos')
        for f in FALLOS:
            print(f'  ✗ {f}')
        sys.exit(1)
    print('TODO OK')


if __name__ == '__main__':
    main()
