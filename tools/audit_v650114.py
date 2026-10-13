#!/usr/bin/env python3
"""
v6.50.114 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO (4 pedidos):
(1) «cuando el daño que recibe el jefe es masivo no se aplica eso de
    que su primera muerte sea absorbido por el grimorio» → EL DAÑO
    MASIVO TAMBIÉN CENA: el gatillo de PreAI exige vida > 0 — un golpe
    que cruza de lleno a ≤ 0 sin pasar por las 100 robaba la cena. La
    interceptación vive AHORA en CheckDead (antes de la marca: la cena
    tiene PRIORIDAD sobre el festín clásico) y en Iniciar (el drain
    masivo ENTREGA a la cena).
(2) «el jefe debe ser rodeado por anillos iguales a los de los
    proyectiles agujeros negro y además también poner anillos como los
    de los soles runicos, ser llevado al centro de la pantalla y ser
    absorbido por el libro» → LA PELÍCULA NUEVA: TRES ANILLOS DE
    FOTONES (Ring + aberración cromática RGB + el aro blanco caliente
    — el patrón exacto del BlackHoleProjectile) + LOS ANILLOS RÚNICOS
    de los soles (SigiloLib.SistemaAnillos, el emisor compartido),
    elevación al centro (FaucesGlobalNPC — intacta) y EL LIBRO ABSORBE:
    el Eterno sale de la mano del portador, CRECE ×2,6, cuelga DEBAJO
    del preso, se ABRE (párpado Medio, iris clavado), come (almas y
    bruma a SU boca), y a los 400 SE CIERRA de golpe (Cerrado + onda)
    y VUELVE a la mano. La sombra de la página MURIÓ en la cena.
(3) «al parecer los minion no tienen limite… deberian tener un limite
    de 10, pero actualiza a que tengan un limite de 20 con el libro y
    mas con items y armadura de invocador» → EL TECHO DE LOS OJOS: el
    BUG era el techo vivo de la .111 (cada ojo vivo subía maxMinions →
    la compuerta JAMÁS se cerraba). AHORA: WeaponScaling.TechoOjosEterno
    (20 + equipo de invocador + escalera por nivel), cacheado por tick
    en ShardPlayer._techoOjos; la compuerta del Eterno lee
    ShardPlayer.TechoOjosDe y NO maxMinions. La promesa .111 (lo vivo
    no se culla) SIGUE EN PIE.
(4) «mejora la animación en cuanto estan alrededor del grimorio en su
    fase de caza… al atacar tenga un pequeño retroceso» → LA PRECESIÓN
    (el plano del anillo cabecea ±0,30 rad, Saturno) + LA ESTELA (tres
    fantasmas en las posiciones pasadas del anillo) + EL RETROCESO DE
    VERDAD (patada 5,6 + estado de 14 t que la ley del planeo RESPETA
    8 ticks + squash/pop en el dibujo — la patada de 3,2 de la .109
    moría en 2-3 t y no se veía).

CHECKS: cabecera/entradas/EOF (390) · heap + ldstr + constantes
.104–.114 · EL ÁRBOL (las 4 piezas + regresión .110–.113 ÍNTEGRA) ·
hjson ×3 empaquetado == fuente (SIN cambios esta versión) ·
icon.png/icon_small.png PNG crudos byte a byte + las dos rawimg de
buff píxel-idénticas (los iconos del usuario SIN cambios).
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
    # === la regresión .105–.113 (los ojos, la cena, la promesa y el perezoso) ===
    'PrimeraCenaProjectile', 'ArrancarPrimeraCena', 'PrimeraCenaPendiente',
    'PrimeraCenaPreAI', 'EnFauces', 'JefesDevorados', 'EsenciaHambre',
    'ESTADO_PRIMERA_CENA', 'VidaUmbralPrimeraCena', 'LargoPrimeraCena',
    'TieneEternoVisible', 'CentroDePantalla', '_posCenaBase',
    'VistaEstadisticas', 'HambrePorUso',
    'OjoEternoMinion', 'OjoEternoBuff', 'DrawElSello', 'DrawElOjo',
    'ESCALA', 'CADENCIA_BASE', 'VelocidadNivel', 'RitmoPropio',
    'CadenciaPersonal', 'PasoOrbita', 'FaseOrbital', 'AnilloDe',
    '_faseOrbital', 'NivelDelOjo', 'MinionChainCount', 'MaxCadenasOjo',
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
    # === LA .114 — el techo, la película nueva y el retroceso ===
    'TechoOjosEterno', 'TechoOjosDe', '_techoOjos', '_techoTick',
    'equipoInvocador',
    'AnilloFotones', 'DibujarElLibro', 'T_LIBRO', 'T_LIBRO_LISTO',
    'T_CIERRE', 'anclaLibro',
    '_retroceso', '_dirRetroceso',
]

MUERTOS_HEAP = [
    'GrimorioSellado', 'TintaViva', 'GrimorioEstrellado', 'EcoEmperatriz',
    'VeredictoEmperatriz', 'EstelaAethonProjectile', 'TrazoEmperatrizProjectile',
    'FragmentoAethon', 'ExplosionAethon', 'SistemaObservado',
    'CosmicOrbMinion', 'CosmicOrbBuff', 'CosmicOrbBolt',
    'DrawElRayo', 'CanalPerlin', 'get_Cadencia',
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
    # === LA .114 — el tooltip del escuadrón y la cena nueva ===
    # (OJO: los strings INTERPOLADOS viajan PARTIDOS en fragmentos por el
    #  InterpolatedStringHandler de C# — el formato completo {0} NUNCA es
    #  un solo ldstr; se verifica por TROZOS literales)
    '[c/78FF96:Escuadrón de 20 ojos · +',
    ' por nivel · +equipo de invocador]',
    '[c/9B59FF:Al primer jefe de cada tipo: los anillos del agujero negro]',
    '[c/9B59FF:y los soles rúnicos lo aprisionan, lo alzan al centro de la]',
    '[c/9B59FF:pantalla y EL LIBRO lo absorbe entero — botín completo. UNA vez.]',
]

MUERTOS_US = [
    # === LA .114 — los textos viejos que MURIERON (por TROZOS: el
    #  interpolado viejo tampoco era UN ldstr — su sufijo es la huella) ===
    ' slot(s) de minion (máx +10)]',
    '[c/9B59FF:Al primer jefe de cada tipo: los sellos lo aprisionan,]',
    '[c/9B59FF:lo alzan al centro de la pantalla y LA SOMBRA DE LA PÁGINA]',
    '[c/9B59FF:lo devora entero — con botín completo. Solo UNA vez.]',
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
    if ver == '6.50.114' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    # .114 == .113 en NOMBRES (390): solo cambia CÓDIGO, cero altas
    if len(entries) == 390:
        ok('390 entradas (los mismos nombres de la .113: solo cambia el código)')
    else:
        mal(f'se esperaban 390 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. EL HEAP — REGRESIÓN .104–.113 + LA .114 ===')
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

    print('=== 3. LAS CONSTANTES (float32) — REGRESIÓN + LA .114 ===')
    def en_dll(b):
        return b in dll
    constantes = [
        (f32(0.01), '0.01f — el PLANO del autóctono (.113) + la pendiente del tope (.112)'),
        (f32(0.009), '0.009f — el tirón del perezoso (regresión .112)'),
        (f32(0.88), '0.88f — el freno nuevo (regresión .112)'),
        (f32(10.0), '10f — el tope CERCA del dueño (regresión .112)'),
        (f32(20.0), '20f — el tope LEJOS (regresión .112) + EL ESCUADRÓN DE 20 (.114)'),
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
        (f32(0.5), '0.5f — EL 50% de la velocidad de ataque (regresión .107)'),
        # === LA .114 ===
        (f32(195.0), '195f — T_FESTIN/T_LIBRO_LISTO: la boca lista debajo (.114)'),
        (f32(400.0), '400f — T_CIERRE: EL LIBRO SE CIERRA de golpe (.114)'),
        (f32(5.6), '5.6f — LA PATACA del retroceso (antes 3,2 invisible) (.114)'),
        (f32(14.0), '14f — el LARGO del retroceso (el estado) (.114)'),
        (f32(2.6), '2.6f — el libro CRECE ×2,6 (.114)'),
        (f32(0.30), '0.30f — LA PRECESIÓN del anillo ±0,30 rad (.114)'),
    ]
    for bytes_, etiqueta in constantes:
        if en_dll(bytes_):
            ok(f'{etiqueta} vive en el IL')
        else:
            mal(f'FALTA en la DLL: {etiqueta}')

    print('=== 4. EL ÁRBOL — LAS 4 PIEZAS + REGRESIÓN .110–.113 ===')
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
    for patron, etiqueta, src in [
        # === LA .114 — (1) EL DAÑO MASIVO TAMBIÉN CENA ===
        ('EL DAÑO MASIVO TAMBIÉN CENA', 'el comentario del fix masivo', fauces),
        ('PrimeraCenaPendiente(dueñoCena, out Player portadorCena)',
         'CheckDead intercepta la muerte de un golpe (la cena ANTES que la marca)', fauces),
        ('LA CENA TIENE PRIORIDAD SOBRE EL FESTÍN', 'Iniciar ENTREGA a la cena (drain masivo)', fauces),
        # === LA .114 — (2) LA PELÍCULA NUEVA ===
        ('private static void AnilloFotones', 'LOS ANILLOS DE FOTONES (los del agujero negro)', cena),
        ('SigiloLib.SistemaAnillos', 'LOS ANILLOS RÚNICOS (los de los soles)', cena),
        ('private void DibujarElLibro', 'EL LIBRO ABSORBE (el dibujo del Eterno gigante)', cena),
        ('ModContent.ItemType<Weapons.GrimoireEternal>()', 'el sprite REAL del Eterno', cena),
        ('GrimorioHambriento_Cerrado', 'EL CIERRE: el párpado Cerrado a los 400', cena),
        ('RotatedBy(-omega * dt)', 'la estela fantasma (regresión del giro)', ojo),
        # === LA .114 — (3) EL TECHO DE LOS OJOS ===
        ('public static int TechoOjosEterno(int equipoInvocador, int nivel)',
         'WeaponScaling.TechoOjosEterno (20 + equipo + nivel)', ws),
        ('int equipoInvocador = Player.maxMinions - 1;',
         'el EQUIPO LIMPIO capturado ANTES de tocar maxMinions', sp),
        ('public static int TechoOjosDe(Player p, int nivel)', 'el cache por tick (TechoOjosDe)', sp),
        ('Players.ShardPlayer.TechoOjosDe(player, level)',
         'LA COMPUERTA lee el techo nuevo (ya NO maxMinions)', eter),
        ('if (Player.maxMinions < techoOjos)', 'el techo es PISO (la promesa .111 intacta)', sp),
        # === LA .114 — (4) EL RETROCESO Y LA ÓRBITA ===
        ('_retroceso = 14f;', 'EL ESTADO de retroceso (14 t)', ojo),
        ('Projectile.velocity = atrás * 5.6f;', 'LA PATADA 5,6 (antes 3,2 invisible)', ojo),
        ('bool enTirón = _retroceso > 0f && _retroceso > 6f;', 'el tirón VIVE 8 ticks (la ley respeta)', ojo),
        ('float incl = 0.30f * MathF.Sin(time * 0.45f + k * 1.9f)',
         'LA PRECESIÓN: el plano cabecea ±0,30 rad', ojo),
        ('LA ESTELA DE LA ÓRBITA', 'la estela de fantasmas del anillo', ojo),
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

    # === LOS MUERTOS DEL ÁRBOL (la .114 + regresión) ===
    if 'int maxMinions = player.maxMinions;' in eter:
        mal('LA COMPUERTA VIEJA lee maxMinions (el runaway VIVE)')
    else:
        ok('LA COMPUERTA VIEJA MUERTA (ya no lee maxMinions)')
    if 'Player.maxMinions += WeaponScaling.BonusMinionSlots(nivel);' in sp:
        mal('el += viejo de slots VIVE en ShardPlayer')
    else:
        ok('el += viejo de slots MUERTO (ahora es PISO del techo nuevo)')
    if 'Projectile.velocity = atrás * 3.2f;' in ojo:
        mal('LA PATADA VIEJA (3,2) VIVE')
    else:
        ok('la patada vieja 3,2 MUERTA (la invisible de la .109)')
    if 'SombrasLib.ColumnaViva' in cena:
        mal('el CUERPO DE LA SOMBRA vive en la cena (el LIBRO lo absorbe ahora)')
    else:
        ok('el cuerpo de la sombra MUERTO en la cena (el libro es el devorador)')
    if 'SombrasLib.CabezaDeBruma' in cena:
        mal('la CABEZA DE BRUMA vive en la cena')
    else:
        ok('la cabeza de bruma MUERTA en la cena')
    if 'OrbitaLib.SigiloErosionado' in cena:
        mal('los SIGILOS EROSIONADOS viven en la cena (la letra pidió agujero negro + rúnicos)')
    else:
        ok('los sigilos erosionados MUERTOS en la cena (anillos de fotones + rúnicos)')
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

    print('=== 5. hjson ×3 — SIN CAMBIOS (.114 no toca textos) ===')
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
        for clave in ('PaginaEnBlanco', 'EsenciaHambre', 'Hambre'):
            if clave in pak:
                ok(f'{loc}: {clave} vive en el paquete (regresión)')
            else:
                mal(f'{loc}: FALTA {clave} (regresión)')
        if loc == 'es-MX' and '\t' in pak:
            mal('es-MX: se coló un TAB')
        else:
            ok(f'{loc}: indentación respetada')

    print('=== 6. LOS ICONOS DEL USUARIO — PNG CRUDOS BYTE A BYTE (SIN cambios) ===')
    png_magic = bytes([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])
    for entrada, tam, etiqueta in [
        ('icon.png', 80, 'EL ICONO DEL MOD (menú Mods — GetModIcon exige 80×80)'),
        ('icon_small.png', 30, 'EL HERMANO PEQUEÑO (vistas compactas — 30×30)'),
    ]:
        if entrada not in rutas:
            mal(f'FALTA {entrada} en el paquete')
            continue
        data = blobs[entrada]
        tree = open(f'{ARBOL}/{entrada}', 'rb').read()
        if data == tree:
            ok(f'{entrada}: byte a byte == árbol ({len(data)} B)')
        else:
            mal(f'{entrada}: DIFIERE del árbol')
        if data[:8] == png_magic:
            ok(f'{entrada}: PNG crudo (la exención de ContentConverters — viaja SIN rawimg)')
        else:
            mal(f'{entrada}: no es PNG crudo')
        try:
            from PIL import Image
            import io as _io
            im = Image.open(_io.BytesIO(data))
            if im.size == (tam, tam):
                ok(f'{entrada}: {tam}×{tam} EXACTOS (GetModIcon)')
            else:
                mal(f'{entrada}: {im.size} — se esperaba {tam}×{tam}')
        except ImportError:
            pass

    print('=== 7. LAS RAWIMG DE BUFF — LOS ICONOS DEL USUARIO (píxel a píxel) ===')
    for entrada, tam, nombre_png in [
        ('Content/Buffs/OjoEternoBuff.rawimg', 32, 'Content/Buffs/OjoEternoBuff.png'),
        ('Content/Buffs/AethonMenorBuff.rawimg', 32, 'Content/Buffs/AethonMenorBuff.png'),
    ]:
        if entrada not in rutas:
            mal(f'FALTA {entrada}')
            continue
        raw = blobs[entrada]
        magic, w, h = struct.unpack('<iii', raw[:12])
        if magic == 1 and (w, h) == (tam, tam) and len(raw) == 12 + tam * tam * 4:
            ok(f'{entrada} {tam}×{tam} RGBA (magic 1, {len(raw)} B)')
        else:
            mal(f'{entrada} inesperada: magic {magic}, {w}×{h}, {len(raw)} B')
        try:
            from PIL import Image
            png = Image.open(f'{ARBOL}/{nombre_png}').convert('RGBA')
            px_raw, px_png = raw[12:], png.tobytes()
            malos = 0
            for p in range(tam * tam):
                if px_raw[p * 4 + 3] != px_png[p * 4 + 3]:
                    malos += 1
                elif px_raw[p * 4 + 3] > 0 and \
                        px_raw[p * 4:p * 4 + 3] != px_png[p * 4:p * 4 + 3]:
                    malos += 1
            if malos == 0:
                ok(f'{entrada}: píxel-idéntica al .png del árbol (SIN cambios)')
            else:
                mal(f'{entrada}: DIFIERE del .png en {malos} píxeles')
        except ImportError:
            ok('(sin PIL: salto la comparación píxel a píxel)')
    for ruta, etiqueta in [
        ('Content/Items/EsenciaHambre.rawimg', 'la Esencia de Hambre (regresión .110)'),
        ('Content/Items/PaginaEnBlanco.rawimg', 'la Página en Blanco (regresión .112)'),
        ('Content/Projectiles/OjoEterno_Abierto.rawimg', 'el ojo EN JUEGO no cambió (regresión)'),
        ('Content/Items/Llamados/AethonMenorItem.rawimg', 'el ítem de la mascota no cambió (regresión)'),
    ]:
        if ruta in rutas:
            ok(f'{ruta} sigue en el paquete ({etiqueta})')
        else:
            mal(f'FALTA {ruta} ({etiqueta})')

    print()
    if FALLOS:
        print(f'✗✗✗ {len(FALLOS)} FALLOS — NO PUBLICAR')
        for f in FALLOS:
            print(f'  ✗ {f}')
        sys.exit(1)
    print('✓ TODO OK — LA .114 ESTÁ LISTA')


if __name__ == '__main__':
    main()
