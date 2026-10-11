#!/usr/bin/env python3
"""
v6.50.107 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO: «los minion son demasiado grande, deben ser mas
pequeño, el tamaño debe ser el mismo que el del grimorio, ademas ellos
atacan al mismo tiempo, cada ojo debe ser individual y deben tener la
libertad de atacar cuando quieran y a la velocidad que deseen ya que la
velocidad de ataque aumenta en cuanto mas nivel tengan el grimorio,
hasta un maximo de 50% mas rapido a nivel 100 del grimorio subiendo a
partir de nivel 1» + «el proyectil que usan los minion es muy grueso,
debe ser un poco mas fino» + «la forma en la que deben moverse al
rededor del libro es orbitar como una luna a un planeta no solo dar
vuelta en ciruclo» + «la caja de informacion del grimorio eterno es
demasiado grande con datos que no necesitan solo debe decir: daño
magico, daño de invocacion, datos y buff propios del juego, un pequeño
texto de lore donde explique un poco que es el libro sin decir mucho,
al menos 2 lineas, luego en progresion debe salir la barra de hambre y
la de exp».

LAS CINCO CIRUGÍAS:
(1) EL TAMAÑO: ESCALA 0.75 — el ojo 56×54 viste 42×41 px (el libro del
    espíritu es 36×49×1.15 = 41×56): la luna cabe en la sombra del
    planeta. Hitbox 25×15, sello r12→15, TODO el layout escala.
(2) EL RITMO PROPIO: 110 / (VelocidadNivel × RitmoPropio) — nivel 1
    = 1.0× subiendo LINEAL a 1.5× (+50%) en nivel 100 (tope clavado);
    ritmo personal 0.85–1.15 por hash de whoAmI; el primer disparo cae
    entre el 35% y el 100% del intervalo (el desfase de nacimiento).
(3) LA LUNA: elipse a=62+42k, b=0.8a, precesión apsidal lenta y LA
    SEGUNDA LEY DE KEPLER (dθ = ω·a²/r² — acelera en el perigeo, frena
    en el apogeo); la fase vive en localAI[3] y sólo avanza PasoOrbita
    (UNA vez por tick, desde la AI).
(4) EL RAYO FINO: funda 6.0 (era 7.5), cápsulas negras 0.235 = 15 px
    (era 19), cadenas 2.9, gorro 4.8, ancla 1.2/0.65, flash 38, bruma
    20 — UN POCO MÁS FINO, la letra.
(5) LA CAJA COMPACTA: el tooltip del Eterno pierde la vista completa
    de 25 líneas — sólo daño mágico (vanilla) + daño de invocación +
    datos/buff del juego (vanilla, sin tocarlos) + LORE de 2 líneas
    (hjson) + PROGRESIÓN (barra de EXP y barra de HAMBRE).

CHECKS:
(1) cabecera 6.50.107 · 387 entradas (los mismos archivos) · EOF.
(2) EL HEAP: los ojos + la luna + el ritmo propio + el modo ojo fino
    VIVOS; el getter Cadencia de la .106 (110−nivel/3) y el Orbe
    Cósmico MUERTOS.
(3) LAS CONSTANTES de la letra en el IL (float32): ESCALA 0.75, el
    nivel 0.5/99, el ritmo 0.85/0.30, el desfase 0.35/0.65, la elipse
    62/42/0.80/0.32/−0.27, la precesión 0.9/0.18/0.07, la fase natal
    0.37, la corona 56/2.8/88, el sello 12/15, el rayo 6.0/0.235/2.9/
    4.8/1.2/0.65/38/2.3/20 + la regresión completa.
(4) EL ÁRBOL: los archivos tocados con sus patrones de la letra.
(5) hjson ×3: paquete == árbol + el LORE vivo / el mega-tooltip y el
    «invoca los Ojos» MUERTOS.
(6) Regresión .102–.106 + las rawimg 56×54 píxel-idénticas.
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
    # los ojos (la .105/.106, vivos en la .107)
    'OjoEternoMinion', 'OjoEternoBuff', 'DrawElSello', 'DrawElOjo',
    'SlotDeFormacion', 'ObjetivoDelOjo', 'SalirDeFuego', 'FocoVivo',
    'Disparar', 'IRIS_REST',
    # el tamaño del libro y el ritmo propio (la .107)
    'ESCALA', 'CADENCIA_BASE', 'VelocidadNivel', 'RitmoPropio',
    'CadenciaPersonal',
    # la luna (la .107)
    'PasoOrbita', 'FaseOrbital', 'AnilloDe',
    # el modo ojo del cetro (la .106, afinado en la .107)
    'TruenoPerlinProjectile', 'DrawArcoOjo', 'CanalOjo', 'BocaDelOjo',
    'PresaDelAncla', 'OjoHalo', 'OjoNegro',
    # el ojo del icono (la .106)
    'PasoOjoIcono', 'SnapDireccionIcono', '_iconoDesp',
    # la caja compacta (la .107) — la pareja del hambre REUBICADA
    # (sus ldstr viven en #US UTF-16 — ver VIVOS_US abajo)
    # la familia viva (regresión .102–.106)
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
    # la cadencia de la .106 (110−nivel/3, piso 55) — MUERTA por la .107
    'get_Cadencia',
]

# ldstr (UTF-16LE, #US heap) — los restos de la vista completa de 25 líneas
# (el «Click der para vista» de SeerOrb NO va aquí: es SU toggle legítimo —
#  la ausencia del conmutador se comprueba en el ÁRBOL del Eterno)
MUERTOS_US = [
    'Próximo hito: Nivel',        # los hitos con su lista de premios
    '═══ DAÑO ═══',               # la sección de daño de la vista completa
    '═══ RECURSOS ═══',
    '═══ PROYECTIL ═══',
    '═══ BONUS ═══',
]

# ldstr (UTF-16LE, #US heap) — la pareja del hambre VIVA (los nombres de
# las líneas con que el Eterno las rescata y reubica a PROGRESIÓN)
VIVOS_US = ['HambreBarra', 'HambreEstado']


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
    if ver == '6.50.107' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    if len(entries) == 387:
        ok('387 entradas (los mismos archivos de la .106, nuevos bytes)')
    else:
        mal(f'se esperaban 387 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. EL HEAP #Strings — LA LUNA Y EL RITMO PROPIO VIVOS ===')
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
    for s in MUERTOS_US:
        if u16(s) in dll:
            mal(f'el ldstr de la vista completa VIVE: {s!r}')
        else:
            ok(f'ldstr muerto (UTF-16): {s!r}')
    for s in VIVOS_US:
        if u16(s) in dll:
            ok(f'ldstr vivo (UTF-16, #US): {s}')
        else:
            mal(f'FALTA el ldstr (UTF-16): {s}')

    print('=== 3. LAS CONSTANTES DE LA LETRA — EN EL IL (float32) ===')
    def en_dll(b):
        return b in dll
    constantes = [
        # (1) EL TAMAÑO DEL LIBRO
        (f32(0.75), '0.75f — ESCALA: el ojo al tamaño del libro (42×41)'),
        # (2) EL RITMO PROPIO — el nivel y el ritmo
        (f32(0.5), '0.5f — EL 50% de la letra (1.0× nivel 1 → 1.5× nivel 100)'),
        (f32(0.85), '0.85f — el piso del ritmo personal'),
        (f32(0.30), '0.30f — el ancho del ritmo (0.85–1.15)'),
        (f32(0.35), '0.35f — el desfase de nacimiento: piso del primer disparo'),
        (f32(0.65), '0.65f — el desfase de nacimiento: techo (y el ancla fina)'),
        # (3) LA LUNA — la elipse y Kepler
        (f32(62.0), '62f — el semieje mayor de la 1ª órbita'),
        (f32(42.0), '42f — el escalón de las órbitas'),
        (f32(0.80), '0.80f — el semieje menor (la órbita INCLINADA: b = 0.8a)'),
        (f32(0.32), '0.32f — ω par (contrarrotación)'),
        (f32(-0.27), '-0.27f — ω impar (contrarrotación)'),
        (f32(0.9), '0.9f — el tilt por anillo'),
        (f32(0.18), '0.18f — la amplitud de la precesión apsidal'),
        (f32(0.07), '0.07f — el periodo de la precesión'),
        (f32(0.37), '0.37f — la fase natal (jamás 0: no renace)'),
        (f32(1.63), '1.63f — el vaivén propio (5,7 s por ciclo)'),
        # la corona (recalibrada al ojo pequeño)
        (f32(56.0), '56f — la base del radio de la corona'),
        (f32(2.8), '2.8f — el paso del radio por ojo (y la mirada Y del icono)'),
        (f32(88.0), '88f — el tope del radio de la corona'),
        # el sello a la escala del libro
        (f32(12.0), '12f — el sello r12 (respira a 15 en la carga)'),
        (f32(15.0), '15f — el sello inhala'),
        # (4) EL RAYO FINO
        (f32(6.0), '6.0f — LA FUNDA (era 7.5: un poco más fina)'),
        (f32(0.235), '0.235f — LA CÁPSULA NEGRA de 15 px (era 19)'),
        (f32(2.9), '2.9f — las cadenas (era 3.6)'),
        (f32(4.8), '4.8f — el gorro del electrodo (era 6.0)'),
        (f32(1.2), '1.2f — el ancla exterior (era 1.5)'),
        (f32(38.0), '38f — el estallido de impacto (era 46)'),
        (f32(2.3), '2.3f — los arcos de corona (era 2.8)'),
        (f32(20.0), '20f — la bruma en la punta (era 24)'),
        # el repertorio heredado (regresión .106)
        (f32(0.41), '0.41f — la escala del iris'),
        (f32(7.5), '7.5f — el viaje horizontal de la mirada (MIRA_X)'),
        (f32(2.5), '2.5f — el viaje vertical de la mirada'),
        (f32(3.2), '3.2f — LA PATADA del retroceso'),
        (f32(2.55), '2.55f — el aro del círculo mágico'),
        (f32(0.55), '0.55f — el ancla pegada a la presa'),
        (f32(0.22), '0.22f — el giro del iris (y el tope de arranque)'),
        (f32(1.1), '1.1f — el vaivén propio de la corona'),
        (f32(0.23), '0.23f — la respiración del radio de la corona'),
        (f32(3.5), '3.5f — la mirada horizontal del ICONO'),
        # LA REGRESIÓN de la casa
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
        # (1) EL TAMAÑO DEL LIBRO
        ('private const float ESCALA = 0.75f', 'ESCALA 0.75 — el ojo al tamaño del libro', ojo),
        ('Projectile.width = 25', 'la hitbox 25×15 (la almendra a escala)', ojo),
        ('pos + (IRIS_REST + mirada) * ESCALA', 'el iris y la mirada escalados', ojo),
        ('IRIS_ESCALA * ESCALA', 'la escala del iris compuesta', ojo),
        # (2) EL RITMO PROPIO
        ('private const int CADENCIA_BASE = 110', 'la base 110 (1,83 s — el Forbidden Ruin)', ojo),
        ('Math.Min(Level - 1, 99) / 99f', 'la VELOCIDAD DE NIVEL: 1.0× → 1.5× (nivel 100)', ojo),
        ('0.85f + 0.30f * VFXCore.Hash01(Projectile.whoAmI, 577', 'EL RITMO PROPIO por ojo (hash)', ojo),
        ('CADENCIA_BASE / (VelocidadNivel * RitmoPropio)', 'la cadencia de ESTE ojo', ojo),
        ('0.35f + 0.65f * VFXCore.Hash01(Projectile.whoAmI, 811', 'EL DESFASE DE NACIMIENTO (35–100%)', ojo),
        ('Projectile.frame = CadenciaPersonal', 'SalirDeFuego arranca SU cadencia', ojo),
        # (3) LA LUNA
        ('private void PasoOrbita(Vector2? libro)', 'PasoOrbita — el reloj de la luna', ojo),
        ('PasoOrbita(libro);', 'la AI lo llama UNA vez por tick', ojo),
        ('private static void AnilloDe(int idx', 'AnilloDe — la escalera 5, 6, 7…', ojo),
        ('private float FaseOrbital(int enAnillo', 'FaseOrbital — la fase natal en localAI[3]', ojo),
        ('omega / 60f * (a * a) / r2', 'KEPLER: dθ ∝ a²/r² (área barrida constante)', ojo),
        ('a = 62f + 42f * k', 'el semieje mayor 62+42k', ojo),
        ('b = a * 0.80f', 'el semieje menor 0.8a (la órbita INCLINADA)', ojo),
        ('+ 0.18f * MathF.Sin(time * 0.07f + k * 1.3f)', 'la precesión apsidal lenta', ojo),
        ('punto.RotatedBy(tilt)', 'el punto de la elipse rotado por el tilt', ojo),
        ('Math.Min(56f + n * 2.8f, 88f)', 'la corona recalibrada al ojo pequeño', ojo),
        # el repertorio heredado (regresión .106)
        ('alSlot * 0.25f', 'la amortiguación crítica (el ojo SIGUE al hueco)', ojo),
        ('dist * 0.22f', 'el arranque suave por distancia', ojo),
        ('atrás * 3.2f', 'EL RETROCESO y la reincorporación', ojo),
        ('VFXCore.Hash01(Projectile.whoAmI, 911', 'el parpadeo INDEPENDIENTE por ojo (hash)', ojo),
        ('fase > 7f || fase < 4f', 'la convención de fases del libro (la .86)', ojo),
        ('OrbitaLib.SigiloErosionado', 'EL CÍRCULO MÁGICO de los agujeros negros', ojo),
        ('TruenoPerlinProjectile', 'el minion dispara EL PROYECTIL DEL CETRO', ojo),
        ('VFXCore.ReabrirLoteVanilla()', 'el contrato del lote (la .101)', ojo),
        # (4) EL RAYO FINO
        ('RayoStrip.Filamento(canal, 6.0f', 'LA FUNDA 6.0 (era 7.5)', cetro),
        ('new Vector2(0.235f, 0.235f)', 'LA CÁPSULA de 15 px (era 19)', cetro),
        ('RayoStrip.Filamento(canalC, 2.9f', 'las cadenas 2.9 (era 3.6)', cetro),
        ('EndCap(Main.spriteBatch, boca, 4.8f', 'el gorro 4.8 (era 6.0)', cetro),
        ('1.2f * pulso', 'el ancla exterior 1.2 (era 1.5)', cetro),
        ('0.65f, SpriteEffects.None', 'el ancla interior 0.65 (era 0.8)', cetro),
        ('38f * (0.6f + 0.4f * flashI)', 'el estallido 38 (era 46)', cetro),
        ('flick, 2.3f,', 'los arcos de corona 2.3 (era 2.8)', cetro),
        ('Bruma(Projectile.Center, 20f', 'la bruma 20 (era 24)', cetro),
        ('DrawArcoOjo', 'el dibujo del arco del ojo', cetro),
        ('BocaDelOjo', 'la boca del arco = EL OJO', cetro),
        ('PresaDelAncla', 'el ancla pegada a la presa', cetro),
        ('DamageClass.Summon', 'el daño de invocación (el ojo, no la magia)', cetro),
        # (5) LA CAJA COMPACTA
        ('═══ PROGRESIÓN ═══', 'la sección PROGRESIÓN', et),
        ('daño de invocación', 'la línea de daño de invocación', et),
        ('if (estadoHambre != null) tooltips.Add(estadoHambre)',
         'la barra de hambre DENTRO de progresión', et),
        ('if (barraHambre != null) tooltips.Add(barraHambre)',
         'el estado del hambre dentro de progresión', et),
        ('sl.XPForNextLevel()', 'la barra de EXP', et),
        # el icono de la oleada (regresión .106)
        ('PasoOjoIcono', 'la máquina del ojo del icono', furia),
        ('SnapDireccionIcono', 'el snap de 8 direcciones', furia),
        ('Main.MouseScreen - posOjoPantalla', 'el iris SIGUE AL CURSOR', furia),
        ('internal const float IRIS_ESC', 'IRIS_ESC internal (el icono la lee)', gh),
    ]:
        if patron in src:
            ok(f'{etiqueta}')
        else:
            mal(f'FALTA en el árbol: {patron}')

    # los restos de la .106 deben estar AUSENTES — los del cetro se
    # comprueban SOLO en el trozo del MODO OJO (DrawArcoOjo…CanalOjo):
    # DrawArco es el cetro PROPIO y conserva su grosor de siempre
    ojo_ini = cetro.index('private void DrawArcoOjo()')
    ojo_fin = cetro.index('private static Vector2[] CanalOjo')
    cetro_ojo = cetro[ojo_ini:ojo_fin]
    ausencias = [
        ('66f + 44f * k', 'las órbitas circulares de la .106', ojo),
        ('Projectile.frame = Cadencia;', 'la cadencia global de la .106', ojo),
        ('new Vector2(3.3f, 2.8f)', 'el aliento del ojo grande de la .106', ojo),
        ('Math.Min(64f + n * 3.2f, 98f)', 'la corona del ojo grande de la .106', ojo),
        ('Filamento(canal, 7.5f', 'la funda gruesa de la .106', cetro_ojo),
        ('new Vector2(0.30f, 0.30f)', 'la cápsula de 19 px de la .106', cetro_ojo),
        ('Filamento(canalC, 3.6f', 'las cadenas gruesas de la .106', cetro_ojo),
        ('boca, 6.0f,', 'el gorro grueso de la .106', cetro_ojo),
        ('1.5f * pulso', 'el ancla gruesa de la .106', cetro_ojo),
        ('46f * (0.6f', 'el estallido grueso de la .106', cetro_ojo),
        ('Bruma(Projectile.Center, 24f', 'la bruma gruesa de la .106', cetro_ojo),
        ('ShowExtendedTooltip', 'la vista completa de 25 líneas', et),
        ('Click der para vista', 'el conmutador de modos del ETERNO', et),
        ('Próximo hito', 'los hitos con lista de premios', et),
        ('MilestoneRewards', 'los premios del próximo hito', et),
        ('═══ DAÑO ═══', 'la sección daño de la vista completa', et),
        ('═══ RECURSOS ═══', 'la sección recursos de la vista completa', et),
        ('═══ PROYECTIL ═══', 'la sección proyectil de la vista completa', et),
        ('═══ BONUS ═══', 'la sección bonus de la vista completa', et),
        ('DrawElRayo', 'el rayo propio del minion (la .105)', ojo),
        ('CanalPerlin', 'el canal propio del minion (la .105)', ojo),
    ]
    for patron, etiqueta, src in ausencias:
        if patron in src:
            mal(f'el resto de la .106/.105 VIVE: {etiqueta} ({patron!r})')
        else:
            ok(f'resto ausente: {etiqueta}')
    if 'CosmicOrbMinion' in et:
        mal('GrimoireEternal aún invoca al Orbe Cósmico')
    else:
        ok('el Orbe Cósmico murió en el Eterno')

    print('=== 5. hjson ×3: PAQUETE == ÁRBOL + EL LORE VIVO ===')
    for h, dentro, fuera in [
        ('es-MX',
         ['Un libro viejo que devora lo que su portador mata',
          'Mantén sus páginas llenas',
          'Ojos del Eterno', 'El Ojo del Eterno', 'rayo negro de bruma'],
         ['EL HAMBRE (una hora', 'invoca los Ojos del Eterno',
          'Sin coste de maná', 'Orbe Cósmico', 'Bolt Cósmico']),
        ('es-ES',
         ['Un libro viejo que devora lo que su portador mata',
          'Mantén sus páginas llenas',
          'Ojos del Eterno', 'El Ojo del Eterno', 'rayo negro de bruma'],
         ['EL HAMBRE (una hora', 'invoca los Ojos del Eterno',
          'Sin coste de maná', 'Orbe Cósmico', 'Bolt Cósmico']),
        ('en-US',
         ['An old book that devours whatever its bearer kills',
          'Keep its pages full',
          'Eyes of the Eternal', 'The Eye of the Eternal', 'misty black ray'],
         ['THE HUNGER (one hour', 'Eyes of the Eternal (crown/orbits',
          'No mana cost', 'Cosmic Orb', 'Cosmic Bolt']),
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
            ok(f'{h}: paquete == árbol ({len(arbol)} B) + LORE vivo, mega-tooltip muerto')
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
        print(f'✗✗✗ {len(FALLOS)} FALLOS — la .107 NO está lista')
        for f in FALLOS:
            print(f'  - {f}')
        sys.exit(1)
    print('✓✓✓ TODO OK — v6.50.107 AUDITADA')


if __name__ == '__main__':
    main()
