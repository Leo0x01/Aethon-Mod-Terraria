#!/usr/bin/env python3
"""
v6.50.108 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO: «el texto que dice lo que hará el libro con el
hambre, por ejemplo este: Fuera de Caza - flota sobre ti buscando su
propia comida, bueno ese texto debe ir justo debajo del hambre, no
encima» + «la segunda ventana de informacion del grimorio desaprecio,
recuerda que el grimorio necesita una segunda ventana de info para
visualisar sus estadisticas» + «en progresion quitaste las estadisticas
del proximo nivel, vuelve a ponerlas» + «ademas, hay errores los minion
no aparecen» (client.log: IndexOutOfRangeException en
OjoEternoMinion.FaseOrbital línea 415 — Projectile.localAI[3]).

LAS CUATRO CIRUGÍAS:
(1) EL CRASH DE LOS OJOS: localAI es float[3] en tML (índices 0–2) —
    la .107 guardaba la fase orbital en localAI[3] y la AI tiraba
    IndexOutOfRange en cada tick (los ojos no aparecían). AHORA la fase
    vive en el CAMPO DE INSTANCIA _faseOrbital (una fase POR OJO,
    determinista en todos los clientes).
(2) EL ORDEN DEL HAMBRE: la BARRA primero y el estado JUSTO DEBAJO
    («ese texto debe ir justo debajo del hambre, no encima»).
(3) LA SEGUNDA VENTANA VUELVE: la vista completa por el conmutador de
    siempre (clic der sobre el libro) con TODAS las estadísticas —
    DAÑO / RECURSOS / PROYECTIL / OJOS DEL ETERNO / BONUS + la nueva
    línea de la velocidad de ataque de los ojos (tope +50% nivel 100).
(4) LAS ESTADÍSTICAS DEL PRÓXIMO NIVEL: el PRÓXIMO HITO y su escalera
    de mejoras (MilestoneRewards), de vuelta dentro de PROGRESIÓN.

CHECKS:
(1) cabecera 6.50.108 · 387 entradas (los mismos archivos) · EOF.
(2) EL HEAP: _faseOrbital y el repertorio de la .107 VIVOS; el getter
    Cadencia de la .106 y el Orbe Cósmico MUERTOS.
(3) LAS CONSTANTES de la .107 (regresión íntegra) en el IL (float32).
(4) EL ÁRBOL: Projectile.localAI[3] AUSENTE del código (el crash), el
    ORDEN barra→estado, la segunda ventana y el próximo hito VIVOS.
(5) hjson ×3: paquete == árbol + el LORE vivo (intacto de la .107).
(6) Regresión .102–.107 + las rawimg 56×54 píxel-idénticas.
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
    if ver == '6.50.108' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    if len(entries) == 387:
        ok('387 entradas (los mismos archivos de la .107, nuevos bytes)')
    else:
        mal(f'se esperaban 387 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. EL HEAP #Strings — EL FIX DE LOS OJOS + LA SEGUNDA VENTANA ===')
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

    print('=== 3. LAS CONSTANTES DE LA .107 — REGRESIÓN ÍNTEGRA (float32) ===')
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

    print('=== 4. EL ÁRBOL — EL FIX, EL ORDEN, LA SEGUNDA VENTANA ===')
    ojo = open(f'{ARBOL}/Content/Projectiles/OjoEternoMinion.cs', encoding='utf-8').read()
    cetro = open(f'{ARBOL}/Content/Projectiles/Cosmic/TruenoPerlinProjectile.cs',
                  encoding='utf-8').read()
    furia = open(f'{ARBOL}/Content/Systems/GrimorioFuriaSistema.cs', encoding='utf-8').read()
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
        # (3) LA LUNA (regresión .107)
        ('private void PasoOrbita(Vector2? libro)', 'PasoOrbita — el reloj de la luna', ojo),
        ('PasoOrbita(libro);', 'la AI lo llama UNA vez por tick', ojo),
        ('private static void AnilloDe(int idx', 'AnilloDe — la escalera 5, 6, 7…', ojo),
        ('private float FaseOrbital(int enAnillo', 'FaseOrbital — la fase natal', ojo),
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
        print(f'✗✗✗ {len(FALLOS)} FALLOS — la .108 NO está lista')
        for f in FALLOS:
            print(f'  - {f}')
        sys.exit(1)
    print('✓✓✓ TODO OK — v6.50.108 AUDITADA')


if __name__ == '__main__':
    main()
