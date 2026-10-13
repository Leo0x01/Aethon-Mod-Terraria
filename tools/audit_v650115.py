#!/usr/bin/env python3
"""
v6.50.115 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO (4 pedidos):
(1) «La sombra de la página es necesaria ya que es la forma en la que
    el grimorio debe comerse a los jefes… en esta nueva animación de
    muerte primero quita los 3 anillos de fotones, eso no es de lo que
    estaba hablando, yo lo que me refería era a los anillos de los
    agujeros rúnicos, eran esos anillos los que tenías que poner más
    los anillos de los soles que sí están bien, estos anillos de
    agujero negro rúnico deben brillar» → LOS ANILLOS DE FOTONES RGB
    DE LA .114 MUEREN; nace LA TRIPLE CORONA DE CONJURO
    (OrbitaLib.CoronaConjuro: el círculo dorado de 8 + el violeta de 6
    contrarrotando + el blanco íntimo — el CÍRCULO LITERAL de los
    agujeros negros) ABRILLANTADA (doble resplandor dorado+violeta
    pulsante tras la corona) y LOS ANILLOS DE LOS SOLES SE QUEDAN.
(2) «la mayor parte de la animación de devorar a un jefe está bien,
    solo que no está en el centro de la pantalla, se centra en el
    jugador, creo que en este caso es mejor ubicar al jefe encima del
    jugador y que el grimorio se ponga a un lado para devorar al jefe,
    la misma animación pero con la sombra de la página saliendo del
    libro y con el libro mirando fijamente a su víctima» → EL MOTOR
    (FaucesGlobalNPC.AnclaSobrePortador): el preso cuelga ENCIMA DEL
    PORTADOR (margen = 0,62·jefe + 120 px, lo sigue si camina); EL
    LIBRO A UN COSTADO (lado del portador, radioJefe+118, inclinado
    -0,30·lado hacia su víctima, iris CLAVADO); LA SOMBRA DE LA
    PÁGINA VUELVE y sale DEL LIBRO (ColumnaViva+Masa+bruma+OjosDeMasa+
    CabezaDeBruma+Devorador v2 — el patrón .113 enraizado en el libro,
    PosicionVivaDelLibro compartida).
(3) «crea una animación especial para el guardián de la mazmorra en su
    fase de guardián… el guardia intenta atacar al jugador pero antes
    de que toque al jugador el grimorio lo detiene y hace la animación
    de devorarlo, esto solo lo hace una vez y el grimorio lo dice…
    esta acción con el guardián le da 1 nivel completo» → EL EASTER
    EGG: IntentarDetenerGuardian (PreAI del DungeonGuardian, a 250 px
    del portador cargando contra él): lo DETIENE (ArrancarPrimeraCena
    — LA CENA completa), LA VOZ lo anuncia (Armas.GuardianDetenido),
    +1 NIVEL COMPLETO (SubirNivelDirecto al Eterno del inventario) y
    ShardPlayer.GuardianMazmorraDevorado (persiste en .plr, PaginaEnBlanco
    lo borra); el guardián NO pasa por la cena normal
    (PrimeraCenaPendiente lo excluye).
(4) «sobre los minion… 30 son demasiado… cuando llegue a 11 minions
    del grimorio en la siguiente invocación no invoca minion, lo que
    hace es fortalecer a uno que ya existe con un 50% de daño y una
    mejora visual en su proyectil… 2 anillos rúnicos de agujero negro
    cada uno con un color diferente y además 2 anillos rúnicos de
    sol… 4 mejoras, cada mejora aumenta el ataque en un 50%» → EL
    ESCUADRÓN DE 10 LLANO (WeaponScaling.EscuadronEterno=10;
    BonusMinionSlots MUERE) y la compuerta cuenta SOLO OJOS; la 11.ª
    invocación FORTALECE (OjoEternoMinion.Fortalecer/MejorasDe/
    MultMejoras — mejoras en la centésima de ai[2]; el ojo con MENOS
    mejoras primero; +50% daño por mejora hasta ×3): anillo dorado de
    agujero negro (mejora 1) + anillo VIOLETA contrarrotante (mejora
    2) + el anillo del SOL dorado (mejora 3, SigiloLib.AnilloRunico)
    + el segundo sol AZUL-ESTELAR (mejora 4) + el resplandor que
    crece con cada mejora.

CHECKS: cabecera/entradas/EOF (390) · heap + ldstr + constantes
.104–.115 · EL ÁRBOL (las 4 piezas + regresión .110–.114 ÍNTEGRA) ·
hjson ×3 empaquetado == fuente (CON cambios esta versión: 3 claves
nuevas, SlotsMinionLlenos muerta) · iconos del usuario SIN cambios.
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
    'OjoEternoMinion', 'OjoEternoBuff', 'DrawElSello', 'DrawElOjo',
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
    # === LA .115 — la corona, la sombra, el guardián y las mejoras ===
    'AnclaSobrePortador', '_cenaPortador', 'PosicionVivaDelLibro',
    'IntentarDetenerGuardian', 'GuardianPaso', 'GuardianMazmorraDevorado',
    'MejorasDe', 'Fortalecer', 'MultMejoras', 'MejorasMax',
    'EscuadronEterno',
]

MUERTOS_HEAP = [
    'GrimorioSellado', 'TintaViva', 'GrimorioEstrellado', 'EcoEmperatriz',
    'VeredictoEmperatriz', 'EstelaAethonProjectile', 'TrazoEmperatrizProjectile',
    'FragmentoAethon', 'ExplosionAethon', 'SistemaObservado',
    'CosmicOrbMinion', 'CosmicOrbBuff', 'CosmicOrbBolt',
    'DrawElRayo', 'CanalPerlin', 'get_Cadencia',
    # === LA .115 — los muertos nuevos ===
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
    # === LA .115 — la voz del guardián, el fortalecimiento y la cena nueva ===
    'Mods.AethonMod.Armas.GuardianDetenido',
    'Mods.AethonMod.Armas.OjoFortalecido',
    'Mods.AethonMod.Armas.OjosAlMaximo',
    'guardianDevorado',            # la llave TagCompound de la marca (ldstr)
    'jefesDevorados',              # la llave TagCompound de la lista (ldstr, regresión)
    'Escuadrón de 10 ojos · lleno el escuadrón, cada invocación FORTALECE a un ojo (+50% daño · 4 mejoras)]',
    '[c/9B59FF:Al primer jefe de cada tipo: los anillos rúnicos del agujero]',
    '[c/9B59FF:negro y los soles lo alzan sobre tu cabeza; el libro, a un]',
    '[c/9B59FF:costado, lo mira fijo y LA SOMBRA DE LA PÁGINA lo devora. UNA vez.]',
]

MUERTOS_US = [
    # === LA .114 — los textos que MURIERON en la .115 ===
    'Mods.AethonMod.Armas.SlotsMinionLlenos',
    '[c/78FF96:Escuadrón de 20 ojos · +',
    ' por nivel · +equipo de invocador]',
    '[c/9B59FF:Al primer jefe de cada tipo: los anillos del agujero negro]',
    '[c/9B59FF:y los soles rúnicos lo aprisionan, lo alzan al centro de la]',
    '[c/9B59FF:pantalla y EL LIBRO lo absorbe entero — botín completo. UNA vez.]',
    # === LA .110 — el texto viejo de la cena (regresión) ===
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
    if ver == '6.50.115' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    # .115 == .114 en NOMBRES (390): solo cambia CÓDIGO, cero altas
    if len(entries) == 390:
        ok('390 entradas (los mismos nombres de la .114: solo cambia el código)')
    else:
        mal(f'se esperaban 390 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. EL HEAP — REGRESIÓN .104–.114 + LA .115 ===')
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

    print('=== 3. LAS CONSTANTES (float32) — REGRESIÓN + LA .115 ===')
    def en_dll(b):
        return b in dll
    constantes = [
        (f32(0.01), '0.01f — el PLANO del autóctono (.113) + EL PASO de las mejoras (.115: ai[2]+m·0,01)'),
        (f32(0.009), '0.009f — el tirón del perezoso (regresión .112)'),
        (f32(0.88), '0.88f — el freno nuevo (regresión .112)'),
        (f32(10.0), '10f — el tope CERCA del dueño (.112) + EL ESCUADRÓN DE 10 (.115)'),
        (f32(20.0), '20f — el tope LEJOS (regresión .112)'),
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
        (f32(2.6), '2.6f — el libro CRECE ×2,6 (regresión .114)'),
        (f32(0.30), '0.30f — LA PRECESIÓN (.114) + LA INCLINACIÓN del libro al preso (.115)'),
        # === LA .115 ===
        (f32(250.0), '250f — GUARDIANPASO: la detención a un paso del portador (.115)'),
        (f32(0.35), '0.35f — el UMBRAL del rumbo: el guardián carga contra él (.115)'),
        (f32(120.0), '120f — el MARGEN sobre la cabeza del portador (AnclaSobrePortador) (.115)'),
        (f32(118.0), '118f — el LIBRO A UN COSTADO (radioJefe+118) (.115)'),
        (f32(0.62), '0.62f — el medio jefe del ancla (regresión radioJefe/AnclaSobrePortador)'),
        (f32(1.10), '1.10f — la corona NACE grande (rBase lerpea 1,10→0,46) (.115)'),
        (f32(0.46), '0.46f — la corona SE CIÑE al preso (.115)'),
        (f32(2.1), '2.1f — GLYPHMUL de la corona: las runas SE LEEN a escala de jefe (.115)'),
        (f32(15.0), '15f — el anillo DORADO de la mejora 1 (.115)'),
        (f32(20.0), '20f — el anillo VIOLETA de la mejora 2 (.115)'),
        (f32(25.0), '25f — EL ANILLO DEL SOL de la mejora 3 (.115)'),
        (f32(30.0), '30f — EL SEGUNDO SOL de la mejora 4 (.115)'),
        (f32(-0.55), '−0.55f — la inclinación del sol dorado (RingTilt(0) de la casa) (.115)'),
    ]
    for bytes_, etiqueta in constantes:
        if en_dll(bytes_):
            ok(f'{etiqueta} vive en el IL')
        else:
            mal(f'FALTA en la DLL: {etiqueta}')

    print('=== 4. EL ÁRBOL — LAS 4 PIEZAS + REGRESIÓN .110–.114 ===')
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
    for patron, etiqueta, src in [
        # === LA .115 — (1) LA CORONA DE CONJURO, NO LOS FOTONES ===
        ('OrbitaLib.CoronaConjuro(posPantalla, rCorona, tiempo * giro,',
         'LA TRIPLE CORONA DE CONJURO: los anillos rúnicos del agujero negro', cena),
        ('deben brillar', 'EL ABRILLANTADO (la letra: deben brillar)', cena),
        ('SigiloLib.SistemaAnillos(centroAros, rRunico, tiempo * spinUp, semilla',
         'LOS ANILLOS DE LOS SOLES SE QUEDAN (los que sí estaban bien)', cena),
        # === LA .115 — (2) EL PUESTO NUEVO: PRESO ENCIMA, LIBRO AL COSTADO,
        #     LA SOMBRA SALE DEL LIBRO ===
        ('private Vector2 AnclaSobrePortador(NPC npc)',
         'EL ANCLA NUEVA: el preso cuelga ENCIMA DEL PORTADOR', fauces),
        ('_cenaPortador = portador.whoAmI;', 'el motor sabe QUIÉN peleó al jefe', fauces),
        ('int lado = portador != null && portador.active && portador.direction != 0',
         'EL LIBRO A UN COSTADO (el lado que mira el portador)', cena),
        ('Vector2 anclaLibro = centroJefe + new Vector2(lado * (radioJefe + 118f)',
         'el ancla del libro: radioJefe+118 al costado', cena),
        ('private Vector2 PosicionVivaDelLibro(',
         'LA POSICIÓN VIVA COMPARTIDA (el dibujo y la sombra respiran igual)', cena),
        ('SombrasLib.ColumnaViva(raiz, destino, tiempo, semilla, 22, 0.20f, gancho: 0.45f)',
         'LA SOMBRA DE LA PÁGINA VUELVE: el cuerpo vivo', cena),
        ('SombrasLib.OjosDeMasa(col, centroJefe', 'los OJOS de la masa miran a la víctima', cena),
        ('SombrasLib.CabezaDeBruma(punta, rumbo, vigor', 'LA CABEZA ES BRUMA (la boca)', cena),
        ('SombrasLib.Devorador(punta, jefe, intensidad', 'EL DEVORADOR v2 (la elipse completa)', cena),
        ('Vector2 raiz = posLibro + rumboRaiz * 16f;', 'la RAÍZ de la sombra ES EL LIBRO', cena),
        ('0.09f * MathF.Sin(tiempo * 1.25f) - lado * 0.30f',
         'el libro SE INCLINA hacia su víctima', cena),
        # === LA .115 — (3) EL EASTER EGG DEL GUARDIÁN ===
        ('private static void IntentarDetenerGuardian(NPC npc)',
         'LA DETENCIÓN del guardián de la mazmorra', fauces),
        ('if (d > GuardianPaso) continue;', 'a UN PASO del portador (250 px)', fauces),
        ('sp.GuardianMazmorraDevorado = true;', 'LA MARCA: solo UNA vez', fauces),
        ('"Mods.AethonMod.Armas.GuardianDetenido"', 'LA VOZ del grimorio lo anuncia', fauces),
        ('sl.SubirNivelDirecto(inv, 1);', '+1 NIVEL COMPLETO al Eterno', fauces),
        ('if (jefe.type == NPCID.DungeonGuardian) return false;',
         'el guardián NO pasa por la cena normal (su festín es el easter egg)', fauces),
        ('ArrancarPrimeraCena(npc, p);', 'la detención ES la cena completa', fauces),
        ('public bool GuardianMazmorraDevorado = false;', 'la marca vive en ShardPlayer', sp),
        ('tag["guardianDevorado"] = GuardianMazmorraDevorado;', 'la marca persiste en el .plr', sp),
        ('tag.GetBool("guardianDevorado")', 'la marca se carga del .plr', sp),
        ('sp.GuardianMazmorraDevorado = false;', 'PaginaEnBlanco también borra el pacto', pagina),
        # === LA .115 — (4) EL ESCUADRÓN DE 10 + EL FORTALECIMIENTO ===
        ('public const int EscuadronEterno = 10;', 'EL ESCUADRÓN SON 10 (llano)', ws),
        ('return EscuadronEterno;', 'TechoOjosEterno YA NO suma equipo ni nivel', ws),
        ('p.type == tipoOjo)', 'LA COMPUERTA cuenta SOLO OJOS (no los minions vanilla)', eter),
        ('OjoEternoMinion.MejorasDe(p);', 'el ojo con MENOS mejoras primero', eter),
        ('OjoEternoMinion.Fortalecer(objetivo);', 'LA 11.ª INVOCACIÓN FORTALECE', eter),
        ('"Mods.AethonMod.Armas.OjoFortalecido"', 'el aviso del fortalecimiento', eter),
        ('"Mods.AethonMod.Armas.OjosAlMaximo"', 'el aviso de las 4 mejoras completas', eter),
        ('public static int MejorasDe(Projectile p)', 'MejorasDe (la centésima de ai[2])', ojo),
        ('public static void Fortalecer(Projectile p)', 'Fortalecer (+1 mejora)', ojo),
        ('public static float MultMejoras(Projectile p) => 1f + 0.5f * MejorasDe(p);',
         'EL +50% POR MEJORA (hasta ×3 con las 4)', ojo),
        ('WeaponScaling.MinionContactDamageMult(nivel) * MultMejoras(Projectile)',
         'EL RAYO escala con nivel Y mejoras', ojo),
        ('OrbitaLib.CirculoRunico(pos, 15f, time, 1f, 8, 0.10f,',
         'MEJORA 1: el anillo DORADO de agujero negro', ojo),
        ('OrbitaLib.CirculoRunico(pos, 20f, time, 1f, 6, -0.075f,',
         'MEJORA 2: el anillo VIOLETA contrarrotante (OTRO color)', ojo),
        ('SigiloLib.AnilloRunico(centroMundo, 25f, 25f * 0.34f, -0.55f,',
         'MEJORA 3: EL ANILLO DEL SOL dorado', ojo),
        ('SigiloLib.AnilloRunico(centroMundo, 30f, 30f * 0.41f, 0.35f,',
         'MEJORA 4: el segundo sol AZUL-ESTELAR', ojo),
        ('p.ai[2] = nivel + m * 0.01f;', 'las mejoras viajan en la centésima de ai[2]', ojo),
        # === LA .114 — regresión (el fix masivo y el libro) ===
        ('PrimeraCenaPendiente(dueñoCena, out Player portadorCena)',
         'CheckDead intercepta la muerte de un golpe (regresión .114)', fauces),
        ('LA CENA TIENE PRIORIDAD SOBRE EL FESTÍN', 'Iniciar ENTREGA a la cena (regresión .114)', fauces),
        ('private void DibujarElLibro', 'EL LIBRO (el dibujo del Eterno gigante, regresión .114)', cena),
        ('GrimorioHambriento_Cerrado', 'EL CIERRE: el párpado Cerrado a los 400 (regresión .114)', cena),
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
        # === LA .113 — el plano + el decimal (regresión) ===
        ('internal static float ReduccionHambre(NPC presa, int tipoLibro)',
         'ReduccionHambre vive (el camino de la sombra)', nerv),
        ('return 0.01f;', 'EL PLANO: 1% fijo por presa devorada por sí mismo', nerv),
        ('float quita = Weapons.GrimoireEternal.HambrePorPresa(npc);',
         'GlobalNPCXP.OnKill SIGUE con la dieta por rareza (la .104 no se toca)', xp),
        ('{(h * 100f).ToString("0.0")}', 'LA BARRA: un decimal SIEMPRE (47,3%)', base),
        ('(et.Hambre * 100f).ToString("0.0")', 'LA ESENCIA: la voz con decimal', esencia),
        # === LA REGLA DE LA .110/.111/.112 EN LOS HOOKS (regresión) ===
        ('float slotsEnUso = 0f;', 'LA SUMA: lo que vanilla va a contar', sp),
        ('if (p.type == tipoOjo) hayOjos = true;', 'el detector de ojos vivos', sp),
        ('if (nivel > 0 || hayOjos)', 'la ley: libro a mano O ojos en el aire', sp),
        ('int techoVivo = (int)Math.Ceiling(slotsEnUso);', 'EL TECHO VIVO (.111 — la promesa)', sp),
        ('Player.maxMinions = techoVivo;', 'la promesa se cumple (.111)', sp),
        ('public const byte ESTADO_PRIMERA_CENA = 3;', 'EL ESTADO 3 (la primera cena)', fauces),
        ('public const int VidaUmbralPrimeraCena = 100;', 'EL UMBRAL DE 100 DE VIDA', fauces),
        ('dueñoPool.life <= VidaUmbralPrimeraCena', 'EL GATILLO PreAI: el jefe llegó a 100', fauces),
        ('sp.JefesDevorados.Contains(jefe.type)', 'SOLO LA PRIMERA VEZ', fauces),
        ('npc.checkDead();', 'LA MUERTE REAL (loot completo)', fauces),
        ('public static bool EnFauces(NPC npc)', 'EnFauces — el respeto al festín', fauces),
        ('public static int VistaEstadisticas = 0;', 'el ciclo 0/1/2', toggle),
        ('VistaEstadisticas = (VistaEstadisticas + 1) % 3;', 'el clic der cicla las TRES', toggle),
        ('public const float HambrePorUso = 0.05f;', '+5% POR USO (de 5 en 5)', esencia),
        ('if (Globals.FaucesGlobalNPC.EnFauces(presa))', 'SombraPaginaCaza SUELTA la presa', caza),
        ('if (FaucesGlobalNPC.EnFauces(presa))', 'SombraPaginaProjectile SUELTA la presa', sombraarma),
        ('tag["jefesDevorados"] = JefesDevorados;', 'se guarda con el jugador', sp),
        ('tag.GetList<int>("jefesDevorados")', 'se carga con el jugador', sp),
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

    # === LOS MUERTOS DEL ÁRBOL (la .115 + regresión) ===
    if 'private static void AnilloFotones' in cena:
        mal('LOS ANILLOS DE FOTONES RGB VIVEN en la cena (la letra los quitó)')
    else:
        ok('LOS ANILLOS DE FOTONES RGB MUERTOS (la corona de conjuro los reemplaza)')
    if 'new Color(255, 0, 0, 80)' in cena:
        mal('LA ABERRACIÓN CROMÁTICA RGB vive en la cena')
    else:
        ok('la aberración cromática RGB MUERTA en la cena')
    if 'CentroDePantalla' in fauces:
        mal('EL CENTRO DE PANTALLA VIVE como destino (la letra: encima del jugador)')
    else:
        ok('el centro de pantalla MUERTO como destino (encima del portador)')
    if 'BonusMinionSlots' in ws:
        mal('LA ESCALERA BonusMinionSlots VIVE (el escuadrón es llano)')
    else:
        ok('la escalera BonusMinionSlots MUERTA (el escuadrón de 10 es llano)')
    if 'SlotsMinionLlenos' in eter:
        mal('el aviso VIEJO de espacios llenos VIVE (ahora se fortalece)')
    else:
        ok('el aviso viejo de espacios llenos MUERTO (la 11.ª fortalece)')
    if 'Escuadrón de 20 ojos' in eter:
        mal('el TOOLTIP VIEJO del escuadrón de 20 VIVE')
    else:
        ok('el tooltip viejo del escuadrón de 20 MUERTO')
    if 'int maxMinions = player.maxMinions;' in eter:
        mal('LA COMPUERTA VIEJA lee maxMinions (el runaway VIVE)')
    else:
        ok('LA COMPUERTA VIEJA MUERTA (ya no lee maxMinions)')
    if 'Main.projectile[i].minion)' in eter:
        mal('la compuerta cuenta TODOS los minions (debe contar SOLO ojos)')
    else:
        ok('la compuerta YA NO cuenta los minions vanilla (solo ojos del Eterno)')
    if 'Projectile.velocity = atrás * 3.2f;' in ojo:
        mal('LA PATADA VIEJA (3,2) VIVE')
    else:
        ok('la patada vieja 3,2 MUERTA (la invisible de la .109)')
    if 'OrbitaLib.SigiloErosionado' in cena:
        mal('los SIGILOS EROSIONADOS viven en la cena')
    else:
        ok('los sigilos erosionados MUERTOS en la cena (la corona de conjuro)')
    if 'HambrePorPresa(presa)' in nerv:
        mal('ReduccionHambre: la dieta por rareza VIVE en la sombra (debe ser PLANA)')
    else:
        ok('la sombra YA NO llama a HambrePorPresa (el plano del autóctono)')
    if '(int)(h * 100f)' in base:
        mal('LA BARRA: el (int) truncador VIVE')
    else:
        ok('LA BARRA: el (int) truncador MUERTO')
    if '(int)(et.Hambre * 100f)' in esencia:
        mal('LA ESENCIA: el (int) truncador VIVE')
    else:
        ok('LA ESENCIA: el (int) truncador MUERTO')

    print('=== 5. hjson ×3 — CON CAMBIOS (.115: 3 claves nuevas, 1 muerta) ===')
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
        # LAS CLAVES NUEVAS de la .115 viven en el paquete
        for clave in ('GuardianDetenido', 'OjoFortalecido', 'OjosAlMaximo'):
            if clave in pak:
                ok(f'{loc}: Armas.{clave} viva en el paquete')
            else:
                mal(f'{loc}: Armas.{clave} FALTA en el paquete')
        if 'SlotsMinionLlenos' in pak:
            mal(f'{loc}: la clave MUERTA SlotsMinionLlenos viaja aún')
        else:
            ok(f'{loc}: la clave muerta SlotsMinionLlenos AUSENTE')
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
    print('✓ TODO OK — v6.50.115 verificada')


if __name__ == '__main__':
    main()
