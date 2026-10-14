#!/usr/bin/env python3
"""
v6.50.121 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO (Task 78 — el mensaje grande, 5 bugs + el re-listado):
(a) «no existe la mascota mini-grimorio con sus mini minions y
    mecánicas» + (e) «la mascota de luz se desactiva después de un
    tiempo» → LA CURA DEL TIMELEFT: NI la cría de luz NI su gemelo
    oscuro refrescaban Projectile.timeLeft — el patrón vanilla de TODA
    mascota (Wisp, Corazón Carmesí…) es timeLeft=2 A CADA TICK (la
    muerte la decide SOLO el buff): sin él, el reloj por defecto del
    proyectil expiraba solo y la pareja se apagaba a los pocos minutos
    (por eso el usuario jamás vio las mecánicas: la mascota moría
    antes de conocerla).
(b) «la bolsa debe entregarse al jugador de la misma forma que todas
    las demás bolsas del mod» → EL KIT DE PRUEBAS: TODAS las bolsas del
    mod viven garantizadas en TestingPlayer.OnEnterWorld — la Bolsa de
    la Cría entra al kit como la hermana 21 (la entrega por victoria
    de la .120 SIGUE viva: la vía canónica).
(c) «el ítem para invocar a los jefes no cambia su nombre» → EL RENAME
    COMPLETO: el invocador es «Fragmentos de un recuerdo lejano -
    Aethon» (×3 idiomas, tooltip nuevo de los gemelos) — Y EL FANTASMA
    QUE LO COMÍA: el hjson es-MX tenía la clave AethonBoss.DisplayName
    DUPLICADA (la vieja pisaba el rename de la .117 — hjson: la última
    gana) y es-ES/en-US jamás recibieron el DisplayName nuevo del jefe
    de luz: los TRES quedan limpios.
(d) «La Caída nunca se activa al usar el ítem de hacer de noche y
    luego de día» → EL GATE DEL GRIMORIO MURIÓ: el disparo exigía NO
    llevar el Grimorio del Eterno… pero el kit entrega la Bolsa
    Fundacional CON el grimorio dentro: TODO jugador de pruebas lo
    llevaba y el cometa JAMÁS caía. Ahora cae SIEMPRE en la primera
    noche (la regla del alma vive en LA TOMA — TomarElLibro rechaza al
    portador). Y LA PÁGINA EN BLANCO también borra la marca de La
    Caída (el probador de primeras veces las borra TODAS — la clave
    nueva PaginaEnBlanco.Caida ×3).

+ LA LETRA DE LAS MECÁNICAS («todas las mecanicas de su combate se
  sienten mal» — agentes 78-e/78-f, ya en el árbol):
  LA CORREA-VS-FLANCOS terminó (620-720 absolutos), MENOS TIEMPO
  MUERTO (70/55/45/35/28), EL DÚO HOSTIGA (TICKS_APOYO_DUO=110: el
  gemelo sin batón suelta pernos de apoyo), DERIVA VIVA ±60/±80 en
  TODOS los estados plantados, EL REPOSO SIN CONTACTO GRATIS (−420) y
  PERSIGUE (catch-up (distF−700)·0.02), APERTURA ÁGIL (75/285/40) y
  EL COLOSO RESPIRA (recortes 300/390/570).

CHECKS: cabecera/entradas/EOF (387 — sin altas) · heap + ldstr +
constantes (.104–.121) · EL ÁRBOL (los 5 fixes + 78-e/78-f + la
regresión .117-.120) · hjson ×3 empaquetado == fuente (con las claves
nuevas ×3 y SIN la duplicada es-MX).
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
    # === la regresión .104–.120 (lo esencial vivo) ===
    'GrimoireEternal', 'GrimorioNerviosoFlotante', 'LaCaidaSistema',
    'CaidaCometaProjectile', 'GrimorioDormido',
    'AethonBossOscuro', 'AethonBossFusionado', 'PasoFusionPrevia',
    'EsFusion', 'EstadoFusionado', 'DropBotin',
    'ElegirVictima', 'EmoteMiedo', 'EsRocaNatural', 'ExcavarCrater',
    'EscalaCuerpo', 'BolsaDeLaCria', 'CanRightClick',
    'AethonMenorItem', 'ParejaCerebro', 'PuebloChismesNPC',
    'BolsaCriaEntregada', 'TestingPlayer',
    'JuicioFantasmal', 'ConcilioEspejos', 'TormentaAbismo',
    'RitualTick', 'SaciedadVuelta',
    # === LA .121 ===
    'TICKS_APOYO_DUO',           # 78-e: el gemelo sin batón hostiga
    '_tickApoyoDuo',             # 78-e: el reloj de la ráfaga de apoyo
    'TicksMolestado',            # (regresión mascota — el registro del pueblo)
]

MUERTOS_HEAP = [
    'PuntoFueraDelPueblo', '_vecinoVigilado',
    'AnilloFotones', 'CentroDePantalla', 'BonusMinionSlots',
    'FragmentoGenesis', 'AltarAntiguoTile',
]

VIVOS_US = [
    # === LA .121 — las claves nuevas ===
    'Mods.AethonMod.PaginaEnBlanco.Caida',
    # === la regresión .117-.120 (claves vivas) ===
    'Mods.AethonMod.Jefe.Aethon.Fusion',
    'Mods.AethonMod.Jefe.BolsaCria.Entregada',
    'Mods.AethonMod.Caida.CometaCae',
    'Mods.AethonMod.Items.BolsaDeLaCria.Abierta',
    'Mods.AethonMod.Jefe.Aethon.Decreto',
    'Mods.AethonMod.PaginaEnBlanco.Reset',
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
    if ver == '6.50.121' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    # .121 = .120 SIN altas ni bajas (solo .cs/.hjson modificados)
    if len(entries) == 387:
        ok('387 entradas (igual que la .120 — sin altas: la .121 solo modifica)')
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

    print('=== 2. EL HEAP — REGRESIÓN + LA .121 ===')
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

    print('=== 3. LAS CONSTANTES — REGRESIÓN + LA .121 ===')
    def en_dll(b):
        return b in dll
    constantes = [
        # === la regresión .109–.120 (lo esencial) ===
        (f32(0.05), '0.05f — SaciedadVuelta (.117)'),
        (f32(550.0), '550f — la hitbox gemela gigante (.119)'),
        (f32(2.50), '2.50f — EscalaCuerpo de los gemelos (.119)'),
        (f32(480.0), '480f — La Caída al anochecer (.119)'),
        (f32(850.0), '850f — LA CORREA del grimorio en el pueblo (.118)'),
        (f32(820.0), '820f — CORREA_DUENO (.120)'),
        (f32(0.50), '0.50f — LA FASE: vida combinada al 50% (.120)'),
        (f32(3.40), '3.40f — EscalaCuerpo del coloso (.120)'),
        (struct.pack('<i', 770), '770 (i4) — width/height del coloso'),
        (struct.pack('<i', 3000000), '3_000_000 (i4) — lifeMax del coloso'),
        # === LA .121 — 78-e: el dúo, la paciencia, la apertura ===
        (struct.pack('<i', 110), '110 (i4) — TICKS_APOYO_DUO del dúo hostiga'),
        (f32(-420.0), '−420f — EL REPOSO sin contacto gratis (.121)'),
        (f32(0.02), '0.02f — EL REPOSO PERSIGUE (catch-up .121)'),
        (struct.pack('<i', 70), '70 (i4) — paciencia fase 1 (.121)'),
        (struct.pack('<i', 28), '28 (i4) — paciencia fase 5 (.121)'),
        (struct.pack('<i', 75), '75 (i4) — apertura ágil: mínimo (.121)'),
        (struct.pack('<i', 285), '285 (i4) — apertura ágil: paracaídas (.121)'),
        # === LA .121 — 78-f: el coloso respira ===
        (f32(0.028), '0.028f — LA DERIVA del coloso vivo (.121)'),
    ]
    for bytes_, etiqueta in constantes:
        if en_dll(bytes_):
            ok(f'{etiqueta} vive en el IL')
        else:
            mal(f'FALTA en la DLL: {etiqueta}')

    print('=== 4. EL ÁRBOL — LOS 5 FIXES + 78-e/78-f + LA REGRESIÓN ===')
    def leer(ruta):
        return open(f'{ARBOL}/{ruta}', encoding='utf-8').read()
    jefe = leer('Content/NPCs/AethonBoss.cs')
    fusion = leer('Content/NPCs/AethonBossFusionado.cs')
    pet = leer('Content/Projectiles/Cosmetic/AethonMenorPet.cs')
    oscuro = leer('Content/Projectiles/Cosmetic/AethonMenorOscuroPet.cs')
    kit = leer('Content/Players/TestingPlayer.cs')
    caida = leer('Content/Systems/LaCaidaSistema.cs')
    pagina = leer('Content/Items/PaginaEnBlanco.cs')

    # === (e) LA CURA DEL TIMELEFT (la mascota vivía reloj de prestado) ===
    if pet.count('Projectile.timeLeft = 2;') == 1 and \
       oscuro.count('Projectile.timeLeft = 2;') == 1:
        ok('LA CURA: timeLeft=2 cada tick en la cría Y el gemelo (vanilla)')
    else:
        mal('FALTA el refresco de timeLeft en las mascotas')

    # === (b) LA BOLSA AL KIT (la misma forma que todas) ===
    if 'Items.Llamados.BolsaDeLaCria' in kit and 'LA MISMA FORMA QUE TODAS' in kit:
        ok('EL KIT: la Bolsa de la Cría garantizada en cada entrada (hermana 21)')
    else:
        mal('FALTA la bolsa en el kit de pruebas')

    # === (c) EL GATE DE LA CAÍDA MURIÓ + LA PÁGINA BORRA LA MARCA ===
    pre = caida[caida.find('public override void PreUpdateTime'):
                caida.find('DispararCometa')]
    if 'if (TieneGrimorio(p)) continue;' not in pre:
        ok('EL GATE MURIÓ: el cometa cae aunque lleves el grimorio (el kit lo trae)')
    else:
        mal('el gate del grimorio SIGUE vivo en el disparo')
    if 'LA REGLA DEL ALMA VIVE EN LA TOMA' in caida or 'la regla del alma vive en LA TOMA' in caida:
        ok('la regla del alma documentada en LA TOMA (TomarElLibro)')
    else:
        mal('FALTA la nota de la regla del alma en la toma')
    if 'sp.CaidaRecibida = false;' in pagina and \
       'Mods.AethonMod.PaginaEnBlanco.Caida' in pagina:
        ok('LA PÁGINA EN BLANCO también borra la marca de La Caída (+ la voz)')
    else:
        mal('FALTA el reset de La Caída en la Página en Blanco')

    # === 78-e: el dúo hostiga + la paciencia + la correa-vs-flancos ===
    if 'TICKS_APOYO_DUO = 110' in jefe and 'EstiloPernoEstelar' in jefe:
        ok('78-e: EL DÚO HOSTIGA (el gemelo sin batón suelta pernos)')
    else:
        mal('FALTA el dúo hostiga de 78-e')
    if '1 => 70, 2 => 55, 3 => 45, 4 => 35, _ => 28' in jefe:
        ok('78-e: MENOS TIEMPO MUERTO (70/55/45/35/28)')
    else:
        mal('FALTA la paciencia nueva de 78-e')
    if '-420f + MathF.Sin(_angOrbita) * 90f' in jefe:
        ok('78-e: EL REPOSO sin contacto gratis (−420+sin·90)')
    else:
        mal('FALTA el reposo a −420 de 78-e')
    if '((distF - 700f) * 0.02f)' in jefe:
        ok('78-e: EL REPOSO PERSIGUE (catch-up en el muelle)')
    else:
        mal('FALTA el catch-up de 78-e')
    if '(t >= 75 && solCentro) || t >= 285' in jefe:
        ok('78-e: LA APERTURA ÁGIL (75/285)')
    else:
        mal('FALTA la apertura ágil de 78-e')

    # === 78-f: el coloso respira ===
    if fusion.count('_tickEstado * 0.028f') >= 7:
        ok(f'78-f: EL COLOSO RESPIRA (deriva viva ×{fusion.count("_tickEstado * 0.028f")} estados)')
    else:
        mal('FALTA la deriva del coloso de 78-f')
    if '570' in fusion and '390' in fusion and '300' in fusion:
        ok('78-f: los recortes de contemplaciones (300/390/570)')
    else:
        mal('FALTA los recortes de 78-f')

    # === la regresión .117-.120 intacta ===
    if 'CORREA_DUENO = 820f' in jefe and 'vidaPar <= maxPar * 0.50f' in jefe:
        ok('regresión: la correa .120 y LA FASE al 50% intactas')
    else:
        mal('FALTA la correa/fase de la .120')
    if 'EL RESCATE DE LA FUSIÓN' in jefe and 'NPC.life = VidaFusion;' in jefe:
        ok('regresión: el RESCATE de CheckDead intacto (la red .119)')
    else:
        mal('FALTA el rescate de la .119')
    if 'SaciedadVuelta' in leer('Content/Projectiles/Nervioso/GrimorioNerviosoFlotante.cs'):
        ok('regresión: el libro saciado vuelve a casa (.117)')
    else:
        mal('FALTA la SaciedadVuelta de la .117')
    if 'bool fuera = (i % 2) == 0' in leer('Content/Projectiles/OjoEternoMinion.cs'):
        ok('regresión: la corona doble intercalada (.117)')
    else:
        mal('FALTA el ladrillo intercalado de la .117')
    if 'TieneEternoEnInventario' in leer('Content/Globals/FaucesGlobalNPC.cs'):
        ok('regresión: el guardián de la mazmorra controlado (.117)')
    else:
        mal('FALTA el control del guardián de la .117')

    print('=== 5. HJSON ×3 — EMPAQUETADO == FUENTE (con las claves .121) ===')
    hjsons = [
        ('Localization/es-MX_Mods.AethonMod.hjson', 'es-MX'),
        ('Localization/es-ES_Mods.AethonMod.hjson', 'es-ES'),
        ('Localization/en-US_Mods.AethonMod.hjson', 'en-US'),
    ]
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
        # (c) el rename del invocador ×3
        if 'Fragmentos de un recuerdo lejano - Aethon' in texto or \
           'Fragments of a Distant Memory - Aethon' in texto:
            ok(f'{idioma}: el invocador renombrado (DisplayName)')
        else:
            mal(f'{idioma}: FALTA el rename del invocador')
        # (d) la clave nueva de la página
        if 'Caida:' in texto:
            ok(f'{idioma}: la clave PaginaEnBlanco.Caida presente')
        else:
            mal(f'{idioma}: FALTA PaginaEnBlanco.Caida')
        # el jefe de luz renombrado en los 3 (el fantasma de la duplicada)
        viejo_bloque = '\t\tDisplayName: Aethon, el Grimorio Eterno'
        viejo_bloque_en = '\t\tDisplayName: Aethon, the Eternal Grimoire'
        nuevo_es = texto.count('\t\tDisplayName: Fragmentos de un recuerdo lejano - Aethon')
        nuevo_en = texto.count('\t\tDisplayName: Fragments of a Distant Memory - Aethon')
        if idioma == 'es-MX':
            n_luz = texto.count('AethonBoss.DisplayName: Fragmentos de un recuerdo lejano')
            viejo_mx = texto.count('AethonBoss.DisplayName: Aethon, el Grimorio Eterno')
            if n_luz == 1 and viejo_mx == 0:
                ok('es-MX: el jefe de luz renombrado y la DUPLICADA muerta')
            else:
                mal(f'es-MX: rename {n_luz} (esperado 1) + duplicada viva: {viejo_mx}')
        else:
            viejo = viejo_bloque if idioma == 'es-ES' else viejo_bloque_en
            nuevo = nuevo_es if idioma == 'es-ES' else nuevo_en
            # 3 apariciones legítimas: invocador + jefe de luz + gemelo oscuro
            if nuevo >= 2 and viejo not in texto:
                ok(f'{idioma}: el jefe de luz renombrado (el que faltaba) y el viejo muerto')
            else:
                mal(f'{idioma}: FALTA el DisplayName nuevo del jefe de luz (nuevo {nuevo}, viejo {viejo in texto})')

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
