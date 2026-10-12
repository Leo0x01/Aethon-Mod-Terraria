#!/usr/bin/env python3
"""
v6.50.109 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO: «reduce el tamaño de los minion en un 50% y
mejora la animacion, que sea mas parecida a Ruinous_Staff en el momento
en el que estan alrededor del libro» + «añadir una estadistica para el
grimorio eterno que mejore a los minion».

LAS TRES CIRUGÍAS:
(1) EL TAMAÑO A LA MITAD: ESCALA 0,75 → 0,375 (el ojo 56×54 → 21×20
    px en pantalla); hitbox 25×15 → 12×8; sello ESCALA·16→ESCALA·20
    (6→7,5 px); aliento a la ESCALA; corona 56+2,8n tope 88 → 44+2,4n
    tope 70; órbitas 62+42k → 46+34k.
(2) LA FORMACIÓN GIRATORIA DEL RUINOUS (la wiki del Forbidden Ruin:
    «they form a spinning orbital formation» + «continuously adjusts
    its position to stay along the line segment between the player and
    its target»): MUERE la elipse kepleriana de la .107 (2ª ley:
    aceleración de perigeo = tirones) y MUERE el freno en seco de la
    carga (velocity *= 0,75); NACE el GIRO CONSTANTE por anillo
    (ω par +0,42 / impar −0,36 rad/s), el radio que RESPIRA (±3,5%),
    la micro-deriva propia (1,6 px 2D) y el PLANEO de la carga hacia
    la línea ancla↔presa (una LEVANTADA hacia la presa).
(3) LA IRA EN CADENA (la estadística NUEVA que mejora a los minion):
    los rayos de los ojos saltan a +1 enemigo cada 20 niveles del
    grimorio — base 2, tope 7 en nivel 100 (WeaponScaling.
    MinionChainCount + TruenoPerlinProjectile.Strike en modo ojo +
    la línea «Rayo en cadena» en la segunda ventana + «+1 salto de
    cadena del rayo» en los hitos 20/40/60/80/100). El CETRO propio
    conserva sus 2 saltos.

CHECKS:
(1) cabecera 6.50.109 · 387 entradas (los mismos archivos) · EOF.
(2) EL HEAP: el repertorio .105–.108 VIVO + MinionChainCount /
    NivelDelOjo / MaxCadenasOjo NUEVOS; el getter Cadencia de la .106
    y el Orbe Cósmico MUERTOS.
(3) LAS CONSTANTES de la .109 (mitad + Ruinous + cadena) y la
    regresión .102–.108 en el IL (float32).
(4) EL ÁRBOL: Kepler y el freno en seco MUERTOS; ESCALA 0,375, el
    giro constante, el planeo de la carga, la cadena por nivel, la
    línea del tooltip y el hito VIVOS; localAI[3] AUSENTE; el ORDEN
    barra→estado y la segunda ventana INTACTOS (.108).
(5) hjson ×3: paquete == árbol + el LORE vivo (intacto de la .107).
(6) Regresión .102–.108 + las rawimg 56×54 píxel-idénticas.
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
    # los ojos (la .105/.106/.107)
    'OjoEternoMinion', 'OjoEternoBuff', 'DrawElSello', 'DrawElOjo',
    'SlotDeFormacion', 'ObjetivoDelOjo', 'SalirDeFuego', 'FocoVivo',
    'Disparar', 'IRIS_REST',
    # el tamaño del libro y el ritmo propio (la .107)
    'ESCALA', 'CADENCIA_BASE', 'VelocidadNivel', 'RitmoPropio',
    'CadenciaPersonal',
    # la luna (la .107) — y SU CASA NUEVA (la .108: el campo de
    # instancia que reemplaza al localAI[3] fuera de rango)
    'PasoOrbita', 'FaseOrbital', 'AnilloDe', '_faseOrbital',
    # la .109 — LA MITAD, EL RUINOUS Y LA IRA EN CADENA
    'NivelDelOjo', 'MinionChainCount', 'MaxCadenasOjo',
    # el modo ojo del cetro (la .106, afinado en la .107)
    'TruenoPerlinProjectile', 'DrawArcoOjo', 'CanalOjo', 'BocaDelOjo',
    'PresaDelAncla', 'OjoHalo', 'OjoNegro',
    # el ojo del icono (la .106)
    'PasoOjoIcono', 'SnapDireccionIcono', '_iconoDesp',
    # la familia viva (regresión .102–.107)
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

# ldstr (UTF-16LE, #US heap) — LA SEGUNDA VENTANA VUELVE (la .108):
# los ldstr que la .107 mató, VIVOS de nuevo
VIVOS_US = [
    'HambreBarra', 'HambreEstado',
    'Próximo hito: Nivel',
    '═══ DAÑO ═══',
    '═══ RECURSOS ═══',
    '═══ PROYECTIL ═══',
    '═══ BONUS ═══',
    'Click der para vista completa',
    'Click der para vista básica',
    'MinionRitmo',
    # la .109 — LA IRA EN CADENA (el tooltip y el hito)
    'MinionCadena',
    'Rayo en cadena: ',
    '+1 salto de cadena del rayo',
]

# ldstr muertos: ninguno nuevo (la .108 no mata strings — los devuelve)


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
    if ver == '6.50.109' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    if len(entries) == 387:
        ok('387 entradas (los mismos archivos de la .108, nuevos bytes)')
    else:
        mal(f'se esperaban 387 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. EL HEAP #Strings — LA MITAD, EL RUINOUS Y LA IRA EN CADENA ===')
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
            ok(f'ldstr vivo (UTF-16, #US): {s}')
        else:
            mal(f'FALTA el ldstr (UTF-16): {s}')

    print('=== 3. LAS CONSTANTES DE LA .109 + REGRESIÓN .102–.108 (float32) ===')
    def en_dll(b):
        return b in dll
    constantes = [
        # (1) EL TAMAÑO A LA MITAD (la .109)
        (f32(0.375), '0.375f — ESCALA: EL OJO A LA MITAD (21×20 px)'),
        # (2) LA FORMACIÓN GIRATORIA DEL RUINOUS (la .109)
        (f32(46.0), '46f — la base de las órbitas (era 62: más cerca del libro)'),
        (f32(34.0), '34f — el escalón de las órbitas (era 42)'),
        (f32(0.94), '0.94f — el anillo casi circular (la RUEDA del Ruinous)'),
        (f32(0.42), '0.42f — ω par: GIRO CONSTANTE (una vuelta ~15 s)'),
        (f32(-0.36), '-0.36f — ω impar: la contrarrotación'),
        (f32(0.035), '0.035f — el radio RESPIRA ±3,5% (la formación viva)'),
        (f32(1.6), '1.6f — la micro-deriva propia (2D, lenta)'),
        # EL PLANEO DE LA CARGA (el Ruinous: la línea ancla↔presa)
        (f32(6.0), '6f — el vmax del planeo de la carga (el movimiento no muere)'),
        (f32(0.12), '0.12f — el controlador proporcional de la carga'),
        (f32(0.18), '0.18f — el lerp del planeo de la carga'),
        (f32(220.0), '220f — el alcance del punto de tiro sobre la línea'),
        # la corona (la .109: apretada a la mitad)
        (f32(44.0), '44f — la base del radio de la corona (era 56)'),
        (f32(2.4), '2.4f — el paso del radio por ojo (era 2.8)'),
        (f32(70.0), '70f — el tope del radio de la corona (era 88)'),
        # el sello y el aliento a la ESCALA (const-fold: 0.375·16=6, 0.375·20=7.5)
        (f32(6.0), '6f — el sello r=ESCALA·16 (≈6 px, la mitad)'),
        (f32(7.5), '7.5f — el sello inhala a ESCALA·20 (≈7,5 px)'),
        # (2) EL RITMO PROPIO — la regresión .107 INTACTA
        (f32(0.5), '0.5f — EL 50% de la letra (1.0× nivel 1 → 1.5× nivel 100)'),
        (f32(0.85), '0.85f — el piso del ritmo personal'),
        (f32(0.30), '0.30f — el ancho del ritmo (0.85–1.15)'),
        (f32(0.35), '0.35f — el desfase de nacimiento: piso (y la fracción de la línea de tiro)'),
        (f32(0.65), '0.65f — el desfase de nacimiento: techo (y el ancla fina)'),
        (f32(0.37), '0.37f — la fase natal (jamás 0: no renace)'),
        (f32(1.63), '1.63f — el vaivén propio de la corona (5,7 s por ciclo)'),
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
        # NOTA .109: 0.9f (tilt), 0.07f (periodo de la precesión), 62f/42f
        # (la elipse) y 0.32f/-0.27f (ω keplerianas) MURIERON con la luna —
        # sus ausencias se comprueban en el ÁRBOL (sección 4): en el IL los
        # float32 sueltos no son únicos (0.9 vive en la mirada del icono).
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

    print('=== 4. EL ÁRBOL — LA MITAD, EL RUINOUS, LA CADENA + REGRESIÓN .108 ===')
    ojo = open(f'{ARBOL}/Content/Projectiles/OjoEternoMinion.cs', encoding='utf-8').read()
    cetro = open(f'{ARBOL}/Content/Projectiles/Cosmic/TruenoPerlinProjectile.cs',
                  encoding='utf-8').read()
    furia = open(f'{ARBOL}/Content/Systems/GrimorioFuriaSistema.cs', encoding='utf-8').read()
    ws = open(f'{ARBOL}/Content/Systems/WeaponScaling.cs', encoding='utf-8').read()
    gh = open(f'{ARBOL}/Content/Weapons/GrimorioHambriento.cs', encoding='utf-8').read()
    et = open(f'{ARBOL}/Content/Weapons/GrimoireEternal.cs', encoding='utf-8').read()
    for patron, etiqueta, src in [
        # (1) EL FIX DEL CRASH — la fase en el campo de instancia
        ('private float _faseOrbital;', 'EL CAMPO _faseOrbital (la casa nueva de la fase)', ojo),
        ('if (_faseOrbital <= 0f)', 'FaseOrbital lee el CAMPO (no localAI[3])', ojo),
        ('_faseOrbital = fase;', 'PasoOrbita escribe el CAMPO (no localAI[3])', ojo),
        ('return _faseOrbital;', 'FaseOrbital devuelve el CAMPO', ojo),
        # (2) EL RITMO PROPIO (regresión .107)
        ('private const int CADENCIA_BASE = 110', 'la base 110 (1,83 s — el Forbidden Ruin)', ojo),
        ('Math.Min(Level - 1, 99) / 99f', 'la VELOCIDAD DE NIVEL: 1.0× → 1.5× (nivel 100)', ojo),
        ('0.85f + 0.30f * VFXCore.Hash01(Projectile.whoAmI, 577', 'EL RITMO PROPIO por ojo (hash)', ojo),
        ('CADENCIA_BASE / (VelocidadNivel * RitmoPropio)', 'la cadencia de ESTE ojo', ojo),
        ('0.35f + 0.65f * VFXCore.Hash01(Projectile.whoAmI, 811', 'EL DESFASE DE NACIMIENTO (35–100%)', ojo),
        # (3) LA FORMACIÓN GIRATORIA DEL RUINOUS (la .109)
        ('private void PasoOrbita(Vector2? libro)', 'PasoOrbita — el reloj del anillo', ojo),
        ('PasoOrbita(libro);', 'la AI lo llama UNA vez por tick', ojo),
        ('private static void AnilloDe(int idx', 'AnilloDe — la escalera 5, 6, 7…', ojo),
        ('private float FaseOrbital(int enAnillo', 'FaseOrbital — la fase natal', ojo),
        ('float omega = (k % 2 == 0) ? 0.42f : -0.36f;', 'EL GIRO CONSTANTE del Ruinous (ω fija)', ojo),
        ('float dFase = omega / 60f;', 'el paso por tick — SIN Kepler (liso)', ojo),
        ('float radio = (46f + 34f * k)', 'las órbitas apretadas 46+34k (era 62+42k)', ojo),
        ('radio * 0.94f', 'el anillo casi circular — LA RUEDA', ojo),
        ('* 1.6f;', 'la micro-deriva propia (1,6 px, 2D)', ojo),
        ('Math.Min(44f + n * 2.4f, 70f)', 'la corona apretada a la mitad (era 56+2,8n/88)', ojo),
        ('Math.Min(dP * 0.35f, 220f)', 'EL PLANEO DE LA CARGA: el punto sobre la línea', ojo),
        ('6f * WeaponScaling.MinionSpeedMult(nivel)', 'el vmax del planeo de la carga', ojo),
        ('Projectile.width = 12;', 'la hitbox a la mitad (12×8)', ojo),
        ('Projectile.height = 8;', 'la hitbox a la mitad (12×8)', ojo),
        ('ESCALA * 16f', 'el sello a la ESCALA (r 6 → 7,5 en la carga)', ojo),
        ('ESCALA * 3.33f', 'el aliento a la ESCALA (la mitad)', ojo),
        # LA IRA EN CADENA (la estadística NUEVA de la .109)
        ('private const float ESCALA = 0.375f', 'ESCALA 0,375 — LA MITAD EXACTA (la letra)', ojo),
        ('public static int MinionChainCount(int level)', 'MinionChainCount — LA IRA EN CADENA', ws),
        ('MinionChainCount(level - 5) < MinionChainCount(level)', 'el hito sólo cuando el salto SUBE', ws),
        ('private int NivelDelOjo()', 'NivelDelOjo — el nivel del ojo que dispara', cetro),
        ('MaxCadenasOjo = 7', 'los arrays de cadena a 7 (el tope)', cetro),
        ('WeaponScaling.MinionChainCount(NivelDelOjo())', 'el MODO OJO lee la cadena del NIVEL', cetro),
        ('MinionCadena', 'la línea «Rayo en cadena» de la segunda ventana', et),
        # el repertorio heredado (regresión .106)
        ('alSlot * 0.25f', 'la amortiguación crítica (el ojo SIGUE al hueco)', ojo),
        ('dist * 0.22f', 'el arranque suave por distancia', ojo),
        ('atrás * 3.2f', 'EL RETROCESO y la reincorporación', ojo),
        ('VFXCore.Hash01(Projectile.whoAmI, 911', 'el parpadeo INDEPENDIENTE por ojo (hash)', ojo),
        ('fase > 7f || fase < 4f', 'la convención de fases del libro (la .86)', ojo),
        ('OrbitaLib.SigiloErosionado', 'EL CÍRCULO MÁGICO de los agujeros negros', ojo),
        ('TruenoPerlinProjectile', 'el minion dispara EL PROYECTIL DEL CETRO', ojo),
        ('VFXCore.ReabrirLoteVanilla()', 'el contrato del lote (la .101)', ojo),
        # (2) EL ORDEN DE LA LETRA — la barra y el estado DEBAJO
        ('if (barraHambre != null) tooltips.Add(barraHambre)',
         'la BARRA de hambre se añade', et),
        ('if (estadoHambre != null) tooltips.Add(estadoHambre)',
         'el estado del hambre se añade', et),
        # (3) LA SEGUNDA VENTANA
        ('Globals.TooltipToggleItem.ShowExtendedTooltip', 'el conmutador de la segunda ventana', et),
        ('bool completa = sl != null', 'la bandera de la vista completa', et),
        ('velocidad de ataque (tope +50% en nivel 100)', 'la línea del ritmo de los ojos', et),
        # (4) LAS ESTADÍSTICAS DEL PRÓXIMO NIVEL
        ('Próximo hito: Nivel', 'el próximo hito', et),
        ('WeaponScaling.MilestoneRewards(nextMilestoneNum)', 'la escalera de mejoras del hito', et),
        ('sl.XPForNextLevel()', 'la barra de EXP', et),
        ('═══ PROGRESIÓN ═══', 'la sección PROGRESIÓN', et),
        ('daño de invocación', 'la línea de daño de invocación', et),
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

    # EL ORDEN DE LA LETRA: la BARRA primero, el estado JUSTO DEBAJO —
    # la línea de la barra debe aparecer ANTES que la del estado
    pos_barra = et.find('tooltips.Add(barraHambre)')
    pos_estado = et.find('tooltips.Add(estadoHambre)')
    if 0 <= pos_barra < pos_estado:
        ok('EL ORDEN: la barra de hambre PRIMERO y el estado DEBAJO (la letra)')
    else:
        mal(f'el ORDEN al revés: barra en {pos_barra}, estado en {pos_estado}')

    # EL CRASH: Projectile.localAI[3] AUSENTE del código (los comentarios
    # hablan de él, pero el CÓDIGO jamás lo toca)
    import re as _re
    codigo_ojo = '\n'.join(l for l in ojo.splitlines()
                           if not l.strip().startswith('//') and '///' not in l)
    if 'localAI[3]' in codigo_ojo:
        mal('el CÓDIGO aún toca localAI[3] (el crash de la .107)')
    else:
        ok('localAI[3] AUSENTE del código (el crash murió)')

    # los restos de la .106 deben seguir AUSENTES — los del cetro se
    # comprueban SOLO en el trozo del MODO OJO (DrawArcoOjo…CanalOjo)
    ojo_ini = cetro.index('private void DrawArcoOjo()')
    ojo_fin = cetro.index('private static Vector2[] CanalOjo')
    cetro_ojo = cetro[ojo_ini:ojo_fin]
    ausencias = [
        # la .109: LA LUNA KEPLERIANA DE LA .107 MUERE (el Ruinous manda)
        ('omega / 60f * (a * a) / r2', 'KEPLER dθ ∝ a²/r² (la .107 — leía a tirones)', ojo),
        ('a = 62f + 42f * k', 'la elipse 62+42k de la .107', ojo),
        ('b = a * 0.80f', 'el semieje menor 0,8a de la .107', ojo),
        ('0.18f * MathF.Sin(time * 0.07f', 'la precesión apsidal de la .107', ojo),
        ('punto.RotatedBy(tilt)', 'el tilt de la .107', ojo),
        ('Math.Min(56f + n * 2.8f, 88f)', 'la corona de la .107', ojo),
        ('Projectile.velocity *= 0.75f', 'EL FRENO EN SECO de la carga (la .108)', ojo),
        ('private const float ESCALA = 0.75f', 'la ESCALA del tamaño del libro (la .107)', ojo),
        ('Lerp(12f, 15f', 'el sello del ojo grande (la .107)', ojo),
        ('new Vector2(2.5f, 2.1f)', 'el aliento sin escala (la .107)', ojo),
        ('Projectile.width = 25', 'la hitbox del ojo grande (la .107)', ojo),
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
        ('DrawElRayo', 'el rayo propio del minion (la .105)', ojo),
        ('CanalPerlin', 'el canal propio del minion (la .105)', ojo),
        ('CosmicOrbMinion', 'el Orbe Cósmico (la .105)', et),
    ]
    for patron, etiqueta, src in ausencias:
        if patron in src:
            mal(f'el resto de la .106/.105 VIVE: {etiqueta} ({patron!r})')
        else:
            ok(f'resto ausente: {etiqueta}')

    print('=== 5. hjson ×3: PAQUETE == ÁRBOL + EL LORE VIVO (intacto .107) ===')
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
            ok(f'{h}: paquete == árbol ({len(arbol)} B) + LORE vivo')
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
        print(f'✗✗✗ {len(FALLOS)} FALLOS — la .109 NO está lista')
        for f in FALLOS:
            print(f'  - {f}')
        sys.exit(1)
    print('✓✓✓ TODO OK — v6.50.109 AUDITADA')


if __name__ == '__main__':
    main()
