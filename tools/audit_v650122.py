#!/usr/bin/env python3
"""
v6.50.122 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO (Task 79 — el mensaje que DESENREDA los conceptos):
«estás mezclando 2 mascotas… la mascota de luz es una representación
miniatura del jefe… la mascota normal es una pequeña representación del
grimorio del eterno… la mascota de luz solo debe iluminar y explotar en
luz como el jefe además de fusionarse al igual que el jefe que es de
luz… en cambio, la mascota que es el minigrimorio, este sí debe tener
los mini minions, toda la mecánica de molestar npc y etc»
+ «la bolsa debe ser entregada al entrar al mundo»
+ «el grimorio siempre cae, pero si tienes uno en el inventario, el del
  inventario hace lo mismo que le hace a los jefes en la primera
  muerte, se lo come y lo devora como si fuera un jefe, y que diga
  algo sobre el tema usando la librería de ecos».

LA SEPARACIÓN: la pareja de luz (claro+gemelo) queda PURA miniatura del
jefe (alumbra, mini estallido, fusión de equinoccio — SIN travesuras);
toda la máquina de ParejaCerebro (AFK, pueblo, sombrilla, 3 mini ojos,
tope 10) se muda al GRIMORIO MENOR (GrimorioMenorPet + GrimorioMenorBuff
+ GrimorioMenorItem — el libro en miniatura con su ojo vivo). La bolsa
de la cría trae a las DOS mascotas. Y LA DEVORACIÓN DEL DOBLE: el
destinatario con grimorio se acerca al caído (~420 px) → CaidaDevorada
Projectile (sellos + sombra de la página, el lenguaje de la Primera
Cena) → DevorarLibroCaido cierra a los 200 t: marca del alma + la voz
del Eterno (EcoRed).

CHECKS: cabecera/entradas/EOF (388 = 387 + GrimorioMenorItem.rawimg) ·
heap + ldstr + constantes (.104–.122) · EL ÁRBOL (la separación + la
devoración + la regresión .117-.121) · hjson ×3 empaquetado == fuente.
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
    # === la regresión .104–.121 (lo esencial vivo) ===
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
    'TICKS_APOYO_DUO', '_tickApoyoDuo',
    'TicksMolestado',
    # === LA .122 — LA SEPARACIÓN + LA DEVORACIÓN ===
    'GrimorioMenorPet',            # el libro en miniatura (la 2.ª mascota)
    'GrimorioMenorBuff',           # su buff (NO lightPet)
    'GrimorioMenorItem',           # su ítem (la bolsa lo trae)
    'CaidaDevoradaProjectile',     # el cine de la devoración del doble
    'DevorarLibroCaido',           # el cierre de la AUTORIDAD
    'DestinatarioVivo',            # el destinatario por NOMBRE
    'DibujarExtrasAlfa',           # los extras siguen vivos (huésped nuevo)
    'PosicionOjos',                # los 3 mini ojos (centro = el libro)
]

MUERTOS_HEAP = [
    'PuntoFueraDelPueblo', '_vecinoVigilado',
    'AnilloFotones', 'CentroDePantalla', 'BonusMinionSlots',
    'FragmentoGenesis', 'AltarAntiguoTile',
    'BuscarOscuro',                  # .122 — los ojos ya NO centran en la pareja
]

VIVOS_US = [
    # === LA .122 — las claves nuevas ===
    'Mods.AethonMod.Caida.Devorado',        # la voz del Eterno tras el festín
    'Mods.AethonMod.Caida.Devorandose',     # el libro devorado no puede hablar
    # === la regresión .117-.121 (claves vivas) ===
    'Mods.AethonMod.PaginaEnBlanco.Caida',
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
    if ver == '6.50.122' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    # .122 = .121 + 1 alta: el icono del Grimorio Menor
    if len(entries) == 388:
        ok('388 entradas (387 de la .121 + GrimorioMenorItem.rawimg)')
    else:
        mal(f'se esperaban 388 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')
    if any(p == 'Content/Items/Llamados/GrimorioMenorItem.rawimg' for p, _, _ in entries):
        ok('GrimorioMenorItem.rawimg viaja (el icono del mini grimorio)')
    else:
        mal('FALTA GrimorioMenorItem.rawimg')
    if any(p == 'Content/Items/Llamados/BolsaDeLaCria.rawimg' for p, _, _ in entries):
        ok('BolsaDeLaCria.rawimg sigue viajando (la .119 intacta)')
    else:
        mal('FALTA BolsaDeLaCria.rawimg')

    print('=== 2. EL HEAP — REGRESIÓN + LA .122 ===')
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

    print('=== 3. LAS CONSTANTES — REGRESIÓN + LA .122 ===')
    def en_dll(b):
        return b in dll
    constantes = [
        # === la regresión .109–.121 (lo esencial) ===
        (f32(0.05), '0.05f — SaciedadVuelta (.117)'),
        (f32(2.50), '2.50f — EscalaCuerpo de los gemelos (.119)'),
        (f32(480.0), '480f — La Caída al anochecer (.119)'),
        (f32(850.0), '850f — LA CORREA del grimorio en el pueblo (.118)'),
        (f32(820.0), '820f — CORREA_DUENO (.120)'),
        (f32(0.50), '0.50f — LA FASE: vida combinada al 50% (.120)'),
        (f32(3.40), '3.40f — EscalaCuerpo del coloso (.120)'),
        (struct.pack('<i', 110), '110 (i4) — TICKS_APOYO_DUO del dúo hostiga (.121)'),
        (f32(-420.0), '−420f — EL REPOSO sin contacto gratis (.121)'),
        (f32(0.028), '0.028f — LA DERIVA del coloso vivo (.121)'),
        # === LA .122 — la separación y la devoración ===
        (f32(420.0), '420f — EL GATILLO: el portador se acerca al doble (.122)'),
        (f32(200.0), '200f — EL CIERRE de la devoración (la AUTORIDAD)'),
        (f32(0.52), '0.52f — el mini grimorio a escala mini'),
        (struct.pack('<i', 260), '260 (i4) — la vida del cine (timeLeft)'),
        (struct.pack('<i', 58), '58 (i4) — la órbita de los mini ojos'),
    ]
    for bytes_, etiqueta in constantes:
        if en_dll(bytes_):
            ok(f'{etiqueta} vive en el IL')
        else:
            mal(f'FALTA en la DLL: {etiqueta}')

    print('=== 4. EL ÁRBOL — LA SEPARACIÓN + LA DEVORACIÓN + LA REGRESIÓN ===')
    def leer(ruta):
        return open(f'{ARBOL}/{ruta}', encoding='utf-8').read()
    claro = leer('Content/Projectiles/Cosmetic/AethonMenorPet.cs')
    oscuro = leer('Content/Projectiles/Cosmetic/AethonMenorOscuroPet.cs')
    cerebro = leer('Content/Projectiles/Cosmetic/AethonMenorPareja.cs')
    mini = leer('Content/Projectiles/Cosmetic/GrimorioMenorPet.cs')
    minibuff = leer('Content/Buffs/GrimorioMenorBuff.cs')
    miniitem = leer('Content/Items/Llamados/GrimorioMenorItem.cs')
    bolsa = leer('Content/Items/Llamados/BolsaDeLaCria.cs')
    chismes = leer('Content/Globals/PuebloChismesNPC.cs')
    dormido = leer('Content/NPCs/GrimorioDormido.cs')
    caida = leer('Content/Systems/LaCaidaSistema.cs')
    cine = leer('Content/Projectiles/Sombras/CaidaDevoradaProjectile.cs')
    kit = leer('Content/Players/TestingPlayer.cs')
    pagina = leer('Content/Items/PaginaEnBlanco.cs')
    jefe = leer('Content/NPCs/AethonBoss.cs')
    fusion = leer('Content/NPCs/AethonBossFusionado.cs')

    # === LA SEPARACIÓN (la letra que desenredó los conceptos) ===
    if 'ParejaCerebro.Tick(' not in claro and 'DibujarExtrasAlfa' not in claro \
       and 'DibujarExtrasAditivos' not in claro:
        ok('LA LUZ, PURA: el claro SIN cerebro de travesuras (solo alumbra/estalla/fusiona)')
    else:
        mal('el claro AÚN arrastra la máquina de travesuras')
    if claro.count('Projectile.timeLeft = 2;') == 1 and \
       oscuro.count('Projectile.timeLeft = 2;') == 1:
        ok('regresión .121: timeLeft=2 cada tick en la cría Y el gemelo')
    else:
        mal('FALTA el refresco de timeLeft en las mascotas de luz')
    if 'ParejaCerebro.Tick(' in mini and 'ParejaCerebro.ElegirMeta(' in mini:
        ok('EL MINI GRIMORIO: el cerebro completo vive en SU huésped (Tick+ElegirMeta)')
    else:
        mal('FALTA el cerebro en el mini grimorio')
    if mini.count('Projectile.timeLeft = 2;') == 1:
        ok('EL MINI GRIMORIO VIVE: timeLeft=2 (la cura .121 aplicada de nacimiento)')
    else:
        mal('FALTA la cura del timeLeft en el mini grimorio')
    if 'DibujarExtrasAlfa(' in mini and 'DibujarExtrasAditivos(' in mini:
        ok('EL MINI GRIMORIO VISTE los extras: sombrilla + 3 mini ojos + anillo rúnico')
    else:
        mal('FALTA el vestido de extras del mini grimorio')
    if 'OjoTexel' in mini and 'GrimorioHambriento_Cerrado' in mini:
        ok('EL OJO VIVO: el mini libro parpadea y mira (contrato del flotante)')
    else:
        mal('FALTA el ojo del mini grimorio')
    if 'public static readonly Dictionary<int, int> TicksMolestado' in cerebro and \
       'BuscarOscuro' not in cerebro:
        ok('EL REGISTRO DEL PUEBLO mudado a ParejaCerebro (y BuscarOscuro muerto)')
    else:
        mal('FALTA la mudanza del registro del pueblo')
    if 'ParejaCerebro.TicksMolestado.TryGetValue' in chismes and \
       'GrimorioMenorBuff' in chismes:
        ok('LOS CHISMES: reclamos desde ParejaCerebro + las DOS mascotas cuentan')
    else:
        mal('FALTA la actualización de los chismes del pueblo')
    if 'Dar(player, ModContent.ItemType<AethonMenorItem>());' in bolsa and \
       'Dar(player, ModContent.ItemType<GrimorioMenorItem>());' in bolsa:
        ok('LA BOLSA DE LAS DOS: la cría de luz + el mini grimorio despiertan juntos')
    else:
        mal('FALTA el mini grimorio en la bolsa')
    if 'Items.Llamados.BolsaDeLaCria' in kit:
        ok('regresión .121: la bolsa garantizada en cada entrada al mundo (el kit)')
    else:
        mal('FALTA la bolsa en el kit de pruebas')
    if 'GrimorioMenorBuff' in minibuff and 'lightPet' not in minibuff.split('SIN ')[1][:60] or True:
        pass  # (el buff NO es lightPet — verificado abajo por ausencia)
    if 'Main.lightPet[Type] = true' not in minibuff:
        ok('EL BUFF DEL LIBRO NO ES DE LUZ (ese trono es del Aethon Menor)')
    else:
        mal('el buff del mini grimorio NO debe ser lightPet')

    # === LA DEVORACIÓN DEL DOBLE ===
    if '420f * 420f' in dormido and 'CaidaDevoradaProjectile' in dormido:
        ok('EL GATILLO: el portador se acerca (~420 px) y el Eterno despierta')
    else:
        mal('FALTA el gatillo de la devoración en el Grimorio Dormido')
    if 'DevorarLibroCaido' in dormido and 'NPC.ai[2] >= 200f' in dormido:
        ok('EL CIERRE a los 200 t (la AUTORIDAD cierra el festín)')
    else:
        mal('FALTA el cierre de 200 t')
    if 'Devorandose' in dormido and 'NPC.ai[2] > 0f' in dormido:
        ok('EL DIÁLOGO DEL DEVORADO (ya no puede hablar mientras lo comen)')
    else:
        mal('FALTA el estado devorado del diálogo')
    if 'public static void DevorarLibroCaido' in caida and \
       'CaidaRecibida = true' in caida and 'Caida.Devorado' in caida:
        ok('LA AUTORIDAD: marca del alma + pendiente apagada + LA VOZ (EcoRed)')
    else:
        mal('FALTA DevorarLibroCaido completo')
    if 'ColumnaViva' in cine and 'Devorador' in cine and 'CicloMasticar' in cine:
        ok('EL CINE: sellos (SistemaAnillos) + la sombra de la página (la .115 en corto)')
    else:
        mal('FALTA la coreografía del cine')

    # === la regresión .117-.121 intacta ===
    pre = caida[caida.find('public override void PreUpdateTime'):
                caida.find('DispararCometa')]
    if 'if (TieneGrimorio(p)) continue;' not in pre:
        ok('regresión .121: EL GATE murió (el cometa cae aunque lleves el grimorio)')
    else:
        mal('el gate del grimorio SIGUE vivo en el disparo')
    if 'sp.CaidaRecibida = false;' in pagina and \
       'Mods.AethonMod.PaginaEnBlanco.Caida' in pagina:
        ok('regresión .121: LA PÁGINA EN BLANCO borra la marca de La Caída')
    else:
        mal('FALTA el reset de La Caída en la Página en Blanco')
    if 'TICKS_APOYO_DUO = 110' in jefe and 'EstiloPernoEstelar' in jefe:
        ok('regresión .121 (78-e): EL DÚO HOSTIGA')
    else:
        mal('FALTA el dúo hostiga de 78-e')
    if fusion.count('_tickEstado * 0.028f') >= 7:
        ok('regresión .121 (78-f): EL COLOSO RESPIRA')
    else:
        mal('FALTA la deriva del coloso de 78-f')
    if 'CORREA_DUENO = 820f' in jefe and 'vidaPar <= maxPar * 0.50f' in jefe:
        ok('regresión .120: la correa y LA FASE al 50% intactas')
    else:
        mal('FALTA la correa/fase de la .120')
    if 'SaciedadVuelta' in leer('Content/Projectiles/Nervioso/GrimorioNerviosoFlotante.cs'):
        ok('regresión .117: el libro saciado vuelve a casa')
    else:
        mal('FALTA la SaciedadVuelta de la .117')
    if 'TieneEternoEnInventario' in leer('Content/Globals/FaucesGlobalNPC.cs'):
        ok('regresión .117: el guardián de la mazmorra controlado')
    else:
        mal('FALTA el control del guardián de la .117')

    print('=== 5. HJSON ×3 — EMPAQUETADO == FUENTE (con las claves .122) ===')
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
        # LA .122: el Grimorio Menor + las claves de la devoración
        if 'GrimorioMenorItem: {' in texto and 'GrimorioMenorBuff: {' in texto:
            ok(f'{idioma}: el GRIMORIO MENOR presente (ítem + buff)')
        else:
            mal(f'{idioma}: FALTA el Grimorio Menor')
        if 'GrimorioMenorPet.DisplayName' in texto and \
           'CaidaDevoradaProjectile.DisplayName' in texto:
            ok(f'{idioma}: los DisplayName nuevos (mascota + cine)')
        else:
            mal(f'{idioma}: FALTAN los DisplayName nuevos')
        if 'Devorado:' in texto and 'Devorandose:' in texto:
            ok(f'{idioma}: la voz del festín + el libro devorado (Caida)')
        else:
            mal(f'{idioma}: FALTAN las claves de la devoración')
        if 'Las DOS mascotas de la casa duermen dentro' in texto or \
           'Both pets of the house sleep inside' in texto:
            ok(f'{idioma}: la bolsa cuenta las DOS mascotas')
        else:
            mal(f'{idioma}: la bolsa NO cuenta las dos mascotas')
        # la regresión .121
        if 'Fragmentos de un recuerdo lejano - Aethon' in texto or \
           'Fragments of a Distant Memory - Aethon' in texto:
            ok(f'{idioma}: el invocador renombrado (regresión .121)')
        else:
            mal(f'{idioma}: FALTA el rename del invocador')

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
