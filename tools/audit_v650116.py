#!/usr/bin/env python3
"""
v6.50.116 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO (5 pedidos):
(1) «en vez de 10 minion que sean 11, así se ve mejor en la animación
    de cuando está el libro fuera» → EL ESCUADRÓN ES 11 (la banda
    completa cierra LAS ÓRBITAS EXACTO: anillo interior de 5 + exterior
    de 6 — DOS RUEDAS llenas); la 12.ª invocación FORTALECE (la
    compuerta lee EscuadronEterno — TechoOjosDe).
(2) «debes mejorar la animación sobre el jugador ya que se quedan muy
    juntos uno de otros cuando están encima del jugador, dame ideas
    para animación en esa fase» → LA CORONA DOBLE (SlotDeFormacion):
    DOS ARCOS ENTRELAZADOS a lo LADRILLO (la mitad fuera en el arco
    exterior, la mitad dentro 23 px más cerca DESFASADA medio paso —
    el DOBLE de cielo por ojo), el arco se ABRE con la banda (±9°),
    el radio RESPIRA (±4%, cada capa a su reloj) y cada ojo CABECEA
    a su propio tempo (±3 px).
(3) «algo importante es que los jefes de las oleadas no deben contar
    para los cambios del mundo… todos los jefes de oleadas son de
    oleadas y están separados de los logros de sus versiones
    originales así que no afectan la progresión del juego, solo
    afectan al grimorio como tal y su propia progresión así como al
    hambre entre otras cosas del grimorio» → EL AISLAMIENTO
    (OleadaNPC.AislarJefeDeOleada, llamado por FaucesGlobalNPC.
    CheckDead en TODA salida donde la muerte de vanilla PROSIGUE):
    el jefe del festín muere con SU identidad en el sello
    (TipoReservado/JefeReservado) y el NPC entregado con type/netID
    en 0 y boss=false — NI downedBoss3 (el esqueletrón de la letra
    ya NO abre la mazmorra), NI meteorito, NI logros
    (CountKillForAchievements→NotifyNPCKilled con netID 0), NI
    celebración, NI loot vanilla (el solver cae por netID). El mod
    cobra TODO por el sello verdadero: la esencia
    (EsenciaDeJefeItem.DeNPC(TipoReservado)), la XP de jefe
    (ShardLevelSystem.XPForNPC lee JefeVerdadero), el hambre del 10%
    (HambrePorPresa), la crónica (CronicaMarcar(TipoVerdadero)), la
    voz (ClaveDeVoz/ColorDeVoz) y la victoria del Juicio
    (FaseJefe acepta TipoReservado==_tipoJefeOleada). El Fragmento
    Génesis NO cae de jefes de oleada (es del jefe DE VERDAD).
    VERIFICADO AL IL DE ESTE tML: NPCLoader.OnKill corre ANTES de
    DoDeathEvents; el netID 0 TIENE entrada en el diccionario del
    bestiario (Initialize arranca en −65: RegisterKill(0) no truena).
(4) «si se realizó la animación sobre el guardián pero salieron más
    guardianes justo al instante, debes bloquear la aparición de
    guardianes a solo 1 antes de ser devorado y después de ser
    devorado por al menos 2 minutos, o sea el jugador llega, el
    guardián se acerca y antes de tocar al jugador es devorado por el
    grimorio, luego hay 2 minutos de gracia antes de que vuelvan a
    aparecer» → LA PUERTA DEL POOL (FaucesGlobalNPC.EditSpawnPool):
    (a) LA GRACIA: ShardPlayer.GuardianGracia (7200 t = 2 min,
    arranca EN el mordisco — cubre TODA la cena) vacía al guardián
    del pool; (b) UNO SOLO: con el pacto PENDIENTE (portador con el
    Eterno a mano sin GuardianMazmorraDevorado), no nace un SEGUNDO
    guardián si ya hay uno vivo. Para quien no carga el libro, la
    mazmorra sigue siendo la de siempre.
(5) «en el momento en el que el grimorio está devorando este
    entrecierra los ojos, en ese momento el iris está por encima del
    ojo, cuando el grimorio es pequeño no se nota, pero cuando se
    hace más grande para devorar a un jefe se nota, deberías crear un
    recorte del iris para evitar eso, por ejemplo recortar el iris a
    la mitad, y que la mitad superior se haga invisible cuando el
    ojo esté medio cerrado» → EL RECORTES DEL IRIS
    (PrimeraCenaProjectile.DibujarElLibro, la rama «comiendo»): el
    iris se dibuja con RECTÁNGULO FUENTE RECORTADO DESDE ABAJO (la
    mitad inferior es la que vive) y el origen compensado para que
    el BORDE INFERIOR quede clavado donde siempre (la pupila no
    salta); la banda visible respira con el masticar (apertura
    0,45+0,50·muerde: al MORDER queda a medias — la mitad superior
    invisible, la letra — y al ABRIR el bocado afloja hasta 0,95).

CHECKS: cabecera/entradas/EOF (390) · heap + ldstr + constantes
.104–.116 · EL ÁRBOL (las 5 piezas + regresión .110–.115 ÍNTEGRA) ·
hjson ×3 empaquetado == fuente (CON cambios esta versión:
OjosAlMaximo diez→once en los 3 idiomas) · iconos SIN cambios.
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
    # === la regresión .105–.114 (los ojos, la cena, la promesa y el perezoso) ===
    'PrimeraCenaProjectile', 'ArrancarPrimeraCena', 'PrimeraCenaPendiente',
    'PrimeraCenaPreAI', 'EnFauces', 'JefesDevorados', 'EsenciaHambre',
    'ESTADO_PRIMERA_CENA', 'VidaUmbralPrimeraCena', 'LargoPrimeraCena',
    'TieneEternoVisible', '_posCenaBase',
    'VistaEstadisticas', 'HambrePorUso',
    'OjoEterneMinion' if False else 'OjoEternoMinion', 'OjoEternoBuff', 'DrawElSello', 'DrawElOjo',
    'ESCALA', 'CADENCIA_BASE', 'VelocidadNivel', 'RitmoPropio',
    'CadenciaPersonal', 'PasoOrbita', 'FaseOrbital', 'AnilloDe',
    '_faseOrbital', 'MinionChainCount', 'MaxCadenasOjo',
    'TruenoPerlinProjectile', 'DrawArcoOjo', 'CanalOjo', 'BocaDelOjo',
    'PasoOjoIcono', 'SnapDireccionIcono', '_iconoDesp',
    'GrimoireEternal', 'GrimorioHambrientoNervioso', 'EstadoGrimorio',
    'EstadoDeLibro', 'EstadoEterno', 'LanzarPareja', 'TicksQuietudSalida',
    'TasaHambre', 'GrimorioNerviosoFlotante', 'SombraPaginaCaza',
    'SombraPaginaProjectile', 'FauceDevoradorProjectile',
    'FaucesGlobalNPC', 'Equinoccio', 'AbsorberPresa',
    'HambrePorPresa',            # SIGUE VIVO: lo usa GlobalNPCXP.OnKill
    'ReduccionHambre', 'EstrellasDe',
    'SigiloErosionado', 'GolpeMotor', 'Bruma', 'IRIS_ESC',
    'CarnadaDelGrimorio', 'BolsaProbador', 'PaginaEnBlanco',
    'AethonMenorBuff', 'AethonMenorPet',
    'Ceiling',                    # la huella del fix .111
    # === LA .114 (regresión) ===
    'TechoOjosEterno', 'TechoOjosDe', '_techoOjos', '_techoTick',
    'equipoInvocador',
    'DibujarElLibro', 'T_LIBRO', 'T_LIBRO_LISTO',
    'T_CIERRE', 'anclaLibro',
    '_retroceso', '_dirRetroceso',
    # === LA .115 (regresión) — la corona, la sombra, el guardián y las mejoras ===
    'AnclaSobrePortador', '_cenaPortador', 'PosicionVivaDelLibro',
    'IntentarDetenerGuardian', 'GuardianPaso', 'GuardianMazmorraDevorado',
    'MejorasDe', 'Fortalecer', 'MultMejoras', 'MejorasMax',
    'EscuadronEterno',
    # === LA .116 — el escuadrón de 11, la corona doble, el aislamiento
    #     y la gracia del guardián ===
    'AislarJefeDeOleada', 'TipoReservado', 'JefeReservado',
    'TipoVerdadero', 'JefeVerdadero',
    'GuardianGracia', 'GuardianGraciaTicks', 'EditSpawnPool',
]

MUERTOS_HEAP = [
    'GrimorioSellado', 'TintaViva', 'GrimorioEstrellado', 'EcoEmperatriz',
    'VeredictoEmperatriz', 'EstelaAethonProjectile', 'TrazoEmperatrizProjectile',
    'FragmentoAethon', 'ExplosionAethon', 'SistemaObservado',
    'CosmicOrbMinion', 'CosmicOrbBuff', 'CosmicOrbBolt',
    'DrawElRayo', 'CanalPerlin', 'get_Cadencia',
    # === LA .115 — los muertos (regresión) ===
    'AnilloFotones',          # los 3 anillos de fotones RGB MURIERON
    'CentroDePantalla',       # el centro de pantalla MUERE como destino
    'BonusMinionSlots',       # la escalera de nivel del escuadrón MUERE
]

VIVOS_US = [
    # === la regresión .110–.113 ===
    'Mods.AethonMod.PaginaEnBlanco.Reset',
    'Mods.AethonMod.PaginaEnBlanco.Nada',
    'Click der para ver EL ARMA',
    'Click der para ver EL FESTÍN',
    '═══ LA PRIMERA CENA ═══',
    'probabilidad de crítico mágico',
    'penetración de armadura mágica',
    'daño mágico (este libro)',
    'Sostenido: crítico · penetración · regeneración · reducción',
    'Barra rápida: vida/maná (tope +100) · slots de minion',
    'Los ojos leen el nivel con el que fueron invocados',
    'HambreBarra', 'HambreEstado',
    'Próximo hito: Nivel',
    '═══ DAÑO ═══', '═══ RECURSOS ═══', '═══ PROYECTIL ═══',
    '═══ BONUS ═══', '═══ OJOS DEL ETERNO ═══',
    'MinionRitmo', 'MinionCadena', 'Rayo en cadena: ',
    '+1 salto de cadena del rayo',
    'Click der para vista básica',
    # === LA .115 (regresión) — la voz del guardián, el fortalecimiento ===
    'Mods.AethonMod.Armas.GuardianDetenido',
    'Mods.AethonMod.Armas.OjoFortalecido',
    'Mods.AethonMod.Armas.OjosAlMaximo',
    'guardianDevorado',            # la llave TagCompound de la marca (ldstr)
    'jefesDevorados',              # la llave TagCompound de la lista (ldstr, regresión)
    # === LA .116 — el escuadrón de ONCE ===
    'Escuadrón de 11 ojos · lleno el escuadrón, cada invocación FORTALECE a un ojo (+50% daño · 4 mejoras)]',
    '[c/9B59FF:Al primer jefe de cada tipo: los anillos rúnicos del agujero]',
    '[c/9B59FF:negro y los soles lo alzan sobre tu cabeza; el libro, a un]',
    '[c/9B59FF:costado, lo mira fijo y LA SOMBRA DE LA PÁGINA lo devora. UNA vez.]',
]

MUERTOS_US = [
    # === LA .116 — el escuadrón de 10 MUERE (la letra: «que sean 11») ===
    'Escuadrón de 10 ojos · lleno el escuadrón',
    # === LA .114/.115 — los textos muertos (regresión) ===
    'Mods.AethonMod.Armas.SlotsMinionLlenos',
    '[c/78FF96:Escuadrón de 20 ojos · +',
    ' por nivel · +equipo de invocador]',
    '[c/9B59FF:Al primer jefe de cada tipo: los anillos del agujero negro]',
    '[c/9B59FF:y los soles rúnicos lo aprisionan, lo alzan al centro de la]',
    '[c/9B59FF:pantalla y EL LIBRO lo absorbe entero — botín completo. UNA vez.]',
    '[c/9B59FF:Al primer jefe de cada tipo: los sellos lo aprisionan,]',
    '[c/9B59FF:lo alzan al centro de la pantalla y LA SOMBRA DE LA PÁGINA]',
    '[c/9B59FF:lo devora entero — con botín completo. Solo UNA vez.]',
    ' slot(s) de minion (máx +10)]',
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
    rutas = {p for p, _, _ in entries}

    print('=== 1. CABECERA / ENTRADAS / EOF ===')
    if ver == '6.50.116' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    # .116 == .115 en NOMBRES (390): solo cambia CÓDIGO, cero altas
    if len(entries) == 390:
        ok('390 entradas (los mismos nombres de la .115: solo cambia el código)')
    else:
        mal(f'se esperaban 390 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. EL HEAP — REGRESIÓN .104–.115 + LA .116 ===')
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

    print('=== 3. LAS CONSTANTES (float32) — REGRESIÓN + LA .116 ===')
    def en_dll(b):
        return b in dll
    constantes = [
        (f32(0.01), '0.01f — el PLANO del autóctono (.113) + EL PASO de las mejoras (.115)'),
        (f32(0.009), '0.009f — el tirón del perezoso (regresión .112)'),
        (f32(0.88), '0.88f — el freno nuevo (regresión .112)'),
        (f32(10.0), '10f — el tope CERCA del dueño (.112)'),
        (f32(11.0), '11f — EL ESCUADRÓN DE ONCE (.116)'),
        (f32(20.0), '20f — el tope LEJOS (regresión .112) + el anillo VIOLETA (.115)'),
        (f32(0.32), '0.32f — el empuje del planeo de vuelta (regresión .112)'),
        (f32(12.0), '12f — el tope del planeo de vuelta (regresión .112)'),
        (f32(430.0), '430f — T_MUERTE: el largo del cine (regresión .110)'),
        (f32(-90.0), '−90f — LA ELEVACIÓN (regresión .110)'),
        (f32(0.05), '0.05f — HambrePorUso: +5% por uso (regresión .110)'),
        (f32(0.375), '0.375f — ESCALA: EL OJO A LA MITAD (regresión .109)'),
        (f32(0.42), '0.42f — ω par: GIRO CONSTANTE (regresión .109)'),
        (f32(1.0 / 216000.0), '1f/216000f — EL RELOJ DE LA HORA (regresión .103)'),
        (f32(0.10), '0.10f — el jefe vale 10% (la dieta VIVA en OnKill, regresión .104)'),
        (f32(2.55), '2.55f — el aro del círculo mágico (regresión .106)'),
        (f32(0.5), '0.5f — EL 50% de velocidad de ataque (.107) + EL 50% POR MEJORA (.115)'),
        (f32(195.0), '195f — T_FESTIN/T_LIBRO_LISTO (regresión .114)'),
        (f32(400.0), '400f — T_CIERRE: EL LIBRO SE CIERRA de golpe (regresión .114)'),
        (f32(5.6), '5.6f — LA PATADA del retroceso (regresión .114)'),
        (f32(14.0), '14f — el LARGO del retroceso (regresión .114)'),
        (f32(2.6), '2.6f — el libro CRECE ×2,6 (regresión .114) + la PENDIENTE de la corona (.116)'),
        (f32(0.30), '0.30f — LA PRECESIÓN (.114) + LA INCLINACIÓN del libro al preso (.115)'),
        # === LA .115 (regresión) ===
        (f32(250.0), '250f — GUARDIANPASO: la detención a un paso del portador (.115)'),
        (f32(0.35), '0.35f — el UMBRAL del rumbo: el guardián carga contra él (.115)'),
        (f32(120.0), '120f — el MARGEN sobre la cabeza del portador (AnclaSobrePortador) (.115)'),
        (f32(118.0), '118f — el LIBRO A UN COSTADO (radioJefe+118) (.115)'),
        (f32(0.62), '0.62f — el medio jefe del ancla (regresión radioJefe/AnclaSobrePortador)'),
        (f32(1.10), '1.10f — la corona NACE grande (rBase lerpea 1,10→0,46) (.115)'),
        (f32(0.46), '0.46f — la corona SE CIÑE al preso (.115)'),
        (f32(2.1), '2.1f — GLYPHMUL de la corona: las runas SE LEEN a escala de jefe (.115)'),
        (f32(15.0), '15f — el anillo DORADO de la mejora 1 (.115)'),
        (f32(25.0), '25f — EL ANILLO DEL SOL de la mejora 3 (.115)'),
        (f32(30.0), '30f — EL SEGUNDO SOL de la mejora 4 (.115)'),
        (f32(-0.55), '−0.55f — la inclinación del sol dorado (RingTilt(0) de la casa) (.115)'),
        # === LA .116 ===
        (f32(0.45), '0.45f — EL RECORTES DEL IRIS: la mitad inferior vive (.116)'),
        (f32(46.0), '46f — LA BASE del radio de la corona doble (.116)'),
        (f32(23.0), '23f — EL ARCO INTERIOR de la corona doble (.116)'),
        (f32(80.0), '80f — EL TOPE del radio de la corona doble (.116)'),
        (f32(0.92), '0.92f — EL APLASTADO vertical de la corona doble (.116)'),
    ]
    for bytes_, etiqueta in constantes:
        if en_dll(bytes_):
            ok(f'{etiqueta} vive en el IL')
        else:
            mal(f'FALTA en la DLL: {etiqueta}')

    print('=== 4. EL ÁRBOL — LAS 5 PIEZAS + REGRESIÓN .110–.115 ===')
    def leer(ruta):
        return open(f'{ARBOL}/{ruta}', encoding='utf-8').read()
    nerv = leer('Content/Weapons/GrimorioHambrientoNervioso.cs')
    base = leer('Content/Weapons/GrimorioHambriento.cs')
    esencia = leer('Content/Items/EsenciaHambre.cs')
    xp = leer('Content/Globals/GlobalNPCXP.cs')
    esp = leer('Content/Projectiles/Nervioso/GrimorioNerviosoFlotante.cs')
    sp = leer('Content/Players/ShardPlayer.cs')
    fauces = leer('Content/Globals/FaucesGlobalNPC.cs')
    toggle = leer('Content/Globals/TooltipToggleItem.cs')
    caza = leer('Content/Projectiles/Nervioso/SombraPaginaCaza.cs')
    sombraarma = leer('Content/Projectiles/Sombras/SombraPaginaProjectile.cs')
    cena = leer('Content/Projectiles/Sombras/PrimeraCenaProjectile.cs')
    eter = leer('Content/Weapons/GrimoireEternal.cs')
    ojo = leer('Content/Projectiles/OjoEternoMinion.cs')
    ws = leer('Content/Systems/WeaponScaling.cs')
    pagina = leer('Content/Items/PaginaEnBlanco.cs')
    oleada = leer('Content/Globals/OleadaNPC.cs')
    furia = leer('Content/Systems/GrimorioFuriaSistema.cs')
    shardlevel = leer('Content/Systems/ShardLevelSystem.cs')
    eco = leer('Content/Systems/EcoSistema.cs')
    for patron, etiqueta, src in [
        # === LA .116 — (1) EL ESCUADRÓN DE ONCE ===
        ('public const int EscuadronEterno = 11;',
         'EL ESCUADRÓN ES 11 (la letra: «en vez de 10 minion que sean 11»)', ws),
        ('return EscuadronEterno;', 'TechoOjosEterno devuelve el ONCE llano', ws),
        ('Escuadrón de 11 ojos · lleno el escuadrón',
         'EL TOOLTIP DEL ESCUADRÓN DE ONCE', eter),
        ('LA 12.ª INVOCACIÓN FORTALECE', 'la compuerta: con 11 puestos, la 12.ª fortalece', eter),
        # === LA .116 — (2) LA CORONA DOBLE (sin apiñamiento) ===
        ('int capaFuera = (n + 1) / 2;',
         'LA CORONA DOBLE: la banda se parte en DOS ARCOS', ojo),
        ('if (!fuera) angC += paso * 0.5f;',
         'EL DESFASE DEL LADRILLO: el arco interior a MEDIAS de paso', ojo),
        ('if (!fuera) radioC -= 23f;',
         'EL ARCO INTERIOR: 23 px más cerca del portador', ojo),
        ('float radioBase = Math.Min(46f + n * 2.6f, 80f);',
         'EL RADIO QUE CRECE CON LA BANDA (tope 80)', ojo),
        ('MathF.Sin(time * 0.23f + (fuera ? 0f : 1.2f))',
         'EL RADIO RESPIRA: cada capa a SU reloj', ojo),
        ('pos.Y += MathF.Sin(time * 0.9f + i * 1.9f) * 3f;',
         'EL CABECEO PROPIO de cada ojo (±3 px)', ojo),
        # === LA .116 — (3) EL AISLAMIENTO DE LOS JEFES DE OLEADA ===
        ('public static void AislarJefeDeOleada(NPC npc)',
         'LA PUERTA DEL AISLAMIENTO (la letra: no cuentan para el mundo)', oleada),
        ('sello.TipoReservado = npc.type;',
         'LA IDENTIDAD RESERVADA en el sello', oleada),
        ('npc.type = 0;', 'el NPC se entrega con type 0', oleada),
        ('npc.netID = 0;', 'el NPC se entrega con netID 0 (SIN loot vanilla)', oleada),
        ('npc.boss = false;', 'el NPC se entrega SIN flag boss (sin celebración)', oleada),
        ('public static int TipoVerdadero(NPC npc)',
         'TipoVerdadero — el mod cobra por SU especie', oleada),
        ('public static bool JefeVerdadero(NPC npc)',
         'JefeVerdadero — el mod cobra por SU rango', oleada),
        ('OleadaNPC.AislarJefeDeOleada(npc);',
         'FaucesGlobalNPC.CheckDead AISLA en la salida de muerte real', fauces),
        ('TipoReservado > 0 ? TipoReservado : npc.type);',
         'LA ESENCIA se paga con la especie VERDADERA', oleada),
        ('sello.TipoReservado == _tipoJefeOleada);',
         'LA VICTORIA DEL JUICIO acepta el sello reservado (DerrotaOleada10)', furia),
        ('if (npc.boss || Globals.OleadaNPC.JefeVerdadero(npc))',
         'LA XP DE JEFE vive para el aislado (ShardLevelSystem)', shardlevel),
        ('if (presa.boss || Globals.OleadaNPC.JefeVerdadero(presa))',
         'EL HAMBRE DEL 10% vive para el aislado (HambrePorPresa)', eter),
        ('(npc.boss || OleadaNPC.JefeVerdadero(npc)) &&',
         'LA CRÓNICA vive para el aislado (GlobalNPCXP)', xp),
        ('OleadaNPC.TipoVerdadero(npc)) == true',
         'la crónica se escribe con la especie VERDADERA', xp),
        ('Globals.OleadaNPC.JefeVerdadero(npc))) return;',
         'LA VOZ DEL JEFE vive para el aislado (EcoSistema)', eco),
        ('ClaveDeVoz(Globals.OleadaNPC.TipoVerdadero(npc))',
         'la voz dice SU nombre verdadero', eco),
        ('if (npc.boss || OleadaNPC.JefeVerdadero(npc))\n                    EcoSistema.AnunciarJefeMuerto',
         'la llamada a la voz acepta al aislado (GlobalNPCXP)', xp),
        ('lee el type VIVO a propósito',
         'EL FRAGMENTO GÉNESIS NO cae de jefes de oleada (leer type vivo)', xp),
        # === LA .116 — (4) EL GUARDIÁN: UNO SOLO + LOS 2 MINUTOS ===
        ('private const int GuardianGraciaTicks = 7200;',
         'LA GRACIA: 7200 ticks = 2 MINUTOS', fauces),
        ('sp.GuardianGracia = GuardianGraciaTicks;',
         'la gracia arranca EN el mordisco (cubre TODA la cena)', fauces),
        ('public int GuardianGracia = 0;',
         'el reloj de la gracia vive en ShardPlayer', sp),
        ('if (GuardianGracia > 0 &&', 'el reloj corre hacia atrás en PostUpdate', sp),
        ('public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)',
         'LA PUERTA DEL POOL (el guardián no se amontona)', fauces),
        ('pool.Remove(NPCID.DungeonGuardian); return; }',
         'el guardián sale del pool (gracia o uno-vivo)', fauces),
        ('if (sp.GuardianGracia > 0) gracia = true;',
         'LA TREGUA: la gracia de cualquier portador vacía el pool', fauces),
        ('if (!sp.GuardianMazmorraDevorado && TieneEternoVisible(p))',
         'UNO SOLO: el pacto pendiente + un guardián vivo = no nace otro', fauces),
        # === LA .116 — (5) EL RECORTES DEL IRIS ===
        ('float apertura = 0.45f + 0.50f * muerde;',
         'EL RECORTES: la banda visible respira con el masticar', cena),
        ('var recorte = new Rectangle(0, corte, iris.Width, iris.Height - corte);',
         'EL RECTÁNGULO FUENTE recortado DESDE ABAJO', cena),
        ('iris.Height * 0.5f - corte);',
         'EL ORIGEN COMPENSADO: el borde inferior queda clavado', cena),
        ('la mitad superior se haga',
         'el recorte es LA LETRA (media mitad superior invisible)', cena),
        # === LA .115 (regresión) — la corona de conjuro, no los fotones ===
        ('OrbitaLib.CoronaConjuro(posPantalla, rCorona, tiempo * giro,',
         'LA TRIPLE CORONA DE CONJURO (regresión .115)', cena),
        ('SigiloLib.SistemaAnillos(centroAros, rRunico, tiempo * spinUp, semilla',
         'LOS ANILLOS DE LOS SOLES SE QUEDAN (regresión .115)', cena),
        ('private Vector2 AnclaSobrePortador(NPC npc)',
         'EL ANCLA: el preso cuelga ENCIMA DEL PORTADOR (regresión .115)', fauces),
        ('Vector2 anclaLibro = centroJefe + new Vector2(lado * (radioJefe + 118f)',
         'el ancla del libro: radioJefe+118 al costado (regresión .115)', cena),
        ('private Vector2 PosicionVivaDelLibro(',
         'LA POSICIÓN VIVA COMPARTIDA (regresión .115)', cena),
        ('SombrasLib.ColumnaViva(raiz, destino, tiempo, semilla, 22, 0.20f, gancho: 0.45f)',
         'LA SOMBRA DE LA PÁGINA SALE DEL LIBRO (regresión .115)', cena),
        ('private static void IntentarDetenerGuardian(NPC npc)',
         'LA DETENCIÓN del guardián (regresión .115)', fauces),
        ('sp.GuardianMazmorraDevorado = true;', 'LA MARCA: solo UNA vez (regresión .115)', fauces),
        ('sl.SubirNivelDirecto(inv, 1);', '+1 NIVEL COMPLETO al Eterno (regresión .115)', fauces),
        ('ArrancarPrimeraCena(npc, p);', 'la detención ES la cena completa (regresión .115)', fauces),
        ('tag["guardianDevorado"] = GuardianMazmorraDevorado;',
         'la marca persiste en el .plr (regresión .115)', sp),
        ('sp.GuardianMazmorraDevorado = false;',
         'PaginaEnBlanco también borra el pacto (regresión .115)', pagina),
        ('OjoEterneMinion' if False else 'OjoEternoMinion.MejorasDe(p);',
         'el ojo con MENOS mejoras primero (regresión .115)', eter),
        ('OjoEternoMinion.Fortalecer(objetivo);', 'LA 12.ª FORTALECE (regresión .115)', eter),
        ('public static int MejorasDe(Projectile p)', 'MejorasDe (regresión .115)', ojo),
        ('public static float MultMejoras(Projectile p) => 1f + 0.5f * MejorasDe(p);',
         'EL +50% POR MEJORA (regresión .115)', ojo),
        ('SigiloLib.AnilloRunico(centroMundo, 25f, 25f * 0.34f, -0.55f,',
         'MEJORA 3: EL ANILLO DEL SOL (regresión .115)', ojo),
        ('SigiloLib.AnilloRunico(centroMundo, 30f, 30f * 0.41f, 0.35f,',
         'MEJORA 4: el segundo sol AZUL-ESTELAR (regresión .115)', ojo),
        ('p.ai[2] = nivel + m * 0.01f;', 'las mejoras viajan en la centésima de ai[2] (regresión .115)', ojo),
        # === LA .114 (regresión) — el fix masivo y el libro ===
        ('PrimeraCenaPendiente(dueñoCena, out Player portadorCena)',
         'CheckDead intercepta la muerte de un golpe (regresión .114)', fauces),
        ('LA CENA TIENE PRIORIDAD SOBRE EL FESTÍN', 'Iniciar ENTREGA a la cena (regresión .114)', fauces),
        ('private void DibujarElLibro', 'EL LIBRO (regresión .114)', cena),
        ('GrimorioHambriento_Cerrado', 'EL CIERRE a los 400 (regresión .114)', cena),
        ('RotatedBy(-omega * dt)', 'la estela fantasma (regresión .114)', ojo),
        ('_retroceso = 14f;', 'EL ESTADO de retroceso 14 t (regresión .114)', ojo),
        ('Projectile.velocity = atrás * 5.6f;', 'LA PATADA 5,6 (regresión .114)', ojo),
        ('bool enTirón = _retroceso > 0f && _retroceso > 6f;', 'el tirón VIVE 8 ticks (regresión .114)', ojo),
        ('float incl = 0.30f * MathF.Sin(time * 0.45f + k * 1.9f)',
         'LA PRECESIÓN ±0,30 rad (regresión .114)', ojo),
        ('public static int TechoOjosDe(Player p, int nivel)', 'el cache por tick (regresión .114)', sp),
        ('Players.ShardPlayer.TechoOjosDe(player, level)',
         'LA COMPUERTA lee el techo del cache (regresión .114)', eter),
        ('if (Player.maxMinions < techoOjos)', 'el techo es PISO (la promesa .111, regresión)', sp),
        # === LA .113 (regresión) — el plano + el decimal ===
        ('internal static float ReduccionHambre(NPC presa, int tipoLibro)',
         'ReduccionHambre vive (regresión .113)', nerv),
        ('return 0.01f;', 'EL PLANO: 1% fijo (regresión .113)', nerv),
        ('float quita = Weapons.GrimoireEternal.HambrePorPresa(npc);',
         'la dieta por rareza en OnKill (regresión .104/.113)', xp),
        ('{(h * 100f).ToString("0.0")}', 'LA BARRA: un decimal SIEMPRE (regresión .113)', base),
        ('(et.Hambre * 100f).ToString("0.0")', 'LA ESENCIA: la voz con decimal (regresión .113)', esencia),
        # === LA .110/.111/.112 (regresión) — los hooks ===
        ('float slotsEnUso = 0f;', 'LA SUMA: lo que vanilla va a contar (regresión)', sp),
        ('if (p.type == tipoOjo) hayOjos = true;', 'el detector de ojos vivos (regresión)', sp),
        ('int techoVivo = (int)Math.Ceiling(slotsEnUso);', 'EL TECHO VIVO (.111 — la promesa)', sp),
        ('Player.maxMinions = techoVivo;', 'la promesa se cumple (.111)', sp),
        ('public const byte ESTADO_PRIMERA_CENA = 3;', 'EL ESTADO 3 (regresión .110)', fauces),
        ('public const int VidaUmbralPrimeraCena = 100;', 'EL UMBRAL DE 100 (regresión .110)', fauces),
        ('dueñoPool.life <= VidaUmbralPrimeraCena', 'EL GATILLO PreAI (regresión .110)', fauces),
        ('sp.JefesDevorados.Contains(jefe.type)', 'SOLO LA PRIMERA VEZ (regresión .110)', fauces),
        ('npc.checkDead();', 'LA MUERTE REAL (regresión .110)', fauces),
        ('public static bool EnFauces(NPC npc)', 'EnFauces — el respeto al festín (regresión)', fauces),
        ('public const float HambrePorUso = 0.05f;', '+5% POR USO (regresión)', esencia),
        ('if (Globals.FaucesGlobalNPC.EnFauces(presa))', 'SombraPaginaCaza SUELTA la presa (regresión)', caza),
        ('if (FaucesGlobalNPC.EnFauces(presa))', 'SombraPaginaProjectile SUELTA la presa (regresión)', sombraarma),
        ('tag["jefesDevorados"] = JefesDevorados;', 'se guarda con el jugador (regresión)', sp),
        ('tag.GetList<int>("jefesDevorados")', 'se carga con el jugador (regresión)', sp),
        ('Projectile.velocity += (destino - Projectile.Center) * 0.009f;',
         'EL TIRÓN DEL PEREZOSO (regresión .112)', esp),
        ('Vector2.Distance(destino, Projectile.Center) * 0.01f, 10f, 20f);',
         'EL TOPE GRADUADO (regresión .112)', esp),
        ('Projectile.velocity += hacia * 0.32f;', 'LA VUELTA: empuje 0.32 (regresión .112)', esp),
    ]:
        if patron in src:
            ok(etiqueta)
        else:
            mal(f'FALTA en el árbol: {etiqueta}')

    # === LOS MUERTOS DEL ÁRBOL (la .116 + regresión) ===
    if 'Escuadrón de 10 ojos' in eter:
        mal('el TOOLTIP VIEJO del escuadrón de 10 VIVE (la letra: 11)')
    else:
        ok('el tooltip viejo del escuadrón de 10 MUERTO (ahora son ONCE)')
    if 'public const int EscuadronEterno = 10;' in ws:
        mal('EL ESCUADRÓN sigue en 10')
    else:
        ok('el 10 MUERTO: el escuadrón es 11')
    # la corona vieja de UN arco (el apiñamiento) — el radio viejo 44+2,4
    if 'Math.Min(44f + n * 2.4f, 70f)' in ojo:
        mal('LA CORONA VIEJA de un solo arco VIVE (el apiñamiento)')
    else:
        ok('la corona vieja de UN arco MUERTA (nació la doble)')
    if 'float f = (i + 0.5f) / n;' in ojo:
        mal('el reparto de UNA fila VIVE (todos al mismo arco)')
    else:
        ok('el reparto de una sola fila MUERTO (dos arcos entrelazados)')
    # el iris ENTERO sobre el párpado Medio — el bug de la letra
    if 'Draw(iris, posOjo, null, luz * alfaLibro, rot,\n                        iris.Size() * 0.5f, esc * Weapons.GrimorioHambrientoNervioso.IrisEscala,\n                        SpriteEffects.None, 0f);\n                }\n                else if (cerrado)' in cena:
        mal('EL IRIS ENTERO sigue dibujándose sobre el párpado Medio')
    else:
        ok('el iris ENTERO sobre el párpado Medio MUERTO (el recorte manda)')
    # los muertos de la .115 (regresión)
    if 'private static void AnilloFotones' in cena:
        mal('LOS ANILLOS DE FOTONES RGB VIVEN (regresión .115)')
    else:
        ok('los anillos de fotones RGB MUERTOS (regresión .115)')
    if 'CentroDePantalla' in fauces:
        mal('EL CENTRO DE PANTALLA VIVE como destino (regresión .115)')
    else:
        ok('el centro de pantalla MUERTO como destino (regresión .115)')
    if 'BonusMinionSlots' in ws:
        mal('LA ESCALERA BonusMinionSlots VIVE (regresión .115)')
    else:
        ok('la escalera BonusMinionSlots MUERTA (regresión .115)')
    if 'SlotsMinionLlenos' in eter:
        mal('el aviso VIEJO de espacios llenos VIVE (regresión .115)')
    else:
        ok('el aviso viejo de espacios llenos MUERTO (regresión .115)')
    if 'HambrePorPresa(presa)' in nerv:
        mal('la dieta por rareza VIVE en la sombra (regresión .113)')
    else:
        ok('la sombra YA NO llama a HambrePorPresa (regresión .113)')
    if '(int)(h * 100f)' in base:
        mal('LA BARRA: el (int) truncador VIVE (regresión .113)')
    else:
        ok('la BARRA sin truncador (regresión .113)')

    print('=== 5. hjson ×3 — CON CAMBIOS (.116: OjosAlMaximo diez→once) ===')
    import re as _re

    def pares(texto):
        fuera = []
        en_ml = False
        for ln in texto.splitlines():
            s = ln.strip()
            if "'''" in s:
                en_ml = not en_ml
                continue
            if en_ml or not s or s.startswith('//') or s in ('}', '{'):
                continue
            m = _re.match(r"^([A-Za-z0-9_.]+):\s*(.+)$", s)
            if m:
                fuera.append((m.group(1), m.group(2).strip()))
        return set(fuera)

    for loc in ('es-ES', 'en-US', 'es-MX'):
        ruta_p = f'Localization/{loc}_Mods.AethonMod.hjson'
        ruta_a = f'{ARBOL}/{ruta_p}'
        if ruta_p not in rutas:
            mal(f'{loc}: no está en el paquete')
            continue
        pak = blobs[ruta_p].decode('utf-8')
        src = open(ruta_a, encoding='utf-8').read()
        if loc == 'es-MX':
            if blobs[ruta_p] == open(ruta_a, 'rb').read():
                ok('es-MX: empaquetado == fuente (byte a byte)')
            else:
                mal('es-MX: DIFIERE del árbol')
        faltan = pares(src) - pares(pak)
        sobran = pares(pak) - pares(src)
        if not faltan and not sobran:
            ok(f'{loc}: {len(pares(src))} pares llave:valor idénticos (semántico)')
        else:
            for k in list(faltan)[:5]:
                mal(f'{loc}: el par se PERDIÓ en el paquete: {k}')
            for k in list(sobran)[:5]:
                mal(f'{loc}: el par SOBRA en el paquete: {k}')
        # EL ONCE de la .116 vive en el paquete
        if loc == 'en-US':
            if 'All eleven eyes carry their four upgrades' in pak:
                ok(f'{loc}: OjosAlMaximo con ELEVEN (la .116)')
            else:
                mal(f'{loc}: OjosAlMaximo SIN el eleven')
            if 'All ten eyes' in pak:
                mal(f'{loc}: el TEN muerto VIVE')
            else:
                ok(f'{loc}: el ten MUERTO')
        else:
            if 'Los once ojos ya llevan sus cuatro mejoras' in pak:
                ok(f'{loc}: OjosAlMaximo con ONCE (la .116)')
            else:
                mal(f'{loc}: OjosAlMaximo SIN el once')
            if 'Los diez ojos' in pak:
                mal(f'{loc}: el DIEZ muerto VIVE')
            else:
                ok(f'{loc}: el diez MUERTO')
        for clave in ('GuardianDetenido', 'OjoFortalecido', 'OjosAlMaximo'):
            if clave in pak:
                ok(f'{loc}: Armas.{clave} viva en el paquete (regresión .115)')
            else:
                mal(f'{loc}: Armas.{clave} FALTA en el paquete (regresión .115)')
        if 'SlotsMinionLlenos' in pak:
            mal(f'{loc}: la clave MUERTA SlotsMinionLlenos viaja aún (regresión)')
        else:
            ok(f'{loc}: la clave muerta SlotsMinionLlenos AUSENTE (regresión)')
        for clave in ('PaginaEnBlanco', 'EsenciaHambre', 'Hambre'):
            if clave in pak:
                ok(f'{loc}: {clave} vive en el paquete (regresión)')
            else:
                mal(f'{loc}: {clave} FALTA en el paquete (regresión)')

    print('=== 6. LOS ICONOS DEL USUARIO — SIN CAMBIOS (.113) ===')
    icon_png = blobs.get('icon.png')
    if icon_png is not None and icon_png[:8] == b'\x89PNG\r\n\x1a\n':
        import struct as _s
        w, h = _s.unpack('>II', icon_png[16:24])
        if (w, h) == (80, 80):
            ok('icon.png PNG crudo 80×80 EXACTO (el del menú Mods)')
        else:
            mal(f'icon.png: {w}×{h} (debe ser 80×80)')
    else:
        mal('icon.png no es PNG crudo')
    icon_small = blobs.get('icon_small.png')
    if icon_small is not None and icon_small[:8] == b'\x89PNG\r\n\x1a\n':
        import struct as _s
        w, h = _s.unpack('>II', icon_small[16:24])
        if (w, h) == (30, 30):
            ok('icon_small.png PNG crudo 30×30 EXACTO')
        else:
            mal(f'icon_small.png: {w}×{h} (debe ser 30×30)')
    else:
        mal('icon_small.png no es PNG crudo')

    # === RESOLUCIÓN ===
    print()
    if FALLOS:
        print(f'✗✗✗ {len(FALLOS)} FALLOS:')
        for f in FALLOS:
            print(f'  - {f}')
        sys.exit(1)
    print('✓ TODO OK — v6.50.116 verificada')


if __name__ == '__main__':
    main()
