#!/usr/bin/env python3
"""
v6.50.110 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO (4 pedidos):
(1) «analisa si las estadisticas del grimorio eterno aplican realmente»
    → EL ANÁLISIS: las 24 líneas de la segunda ventana verificadas UNA
    A UNA contra su punto de aplicación (ModifyWeaponDamage /
    ShardPlayer.PostUpdateEquips / PostUpdateBuffs / ModifyHurt /
    OjoEternoMinion / TruenoPerlin / CosmicProjectileFX /
    GlobalNPCXP.OnHitBy*) — TODAS aplican; las etiquetas quedaron
    HONESTAS (crítico/penetración MÁGICOS, el «(este libro)» del daño
    mágico, las notas al pie de postura y del nivel de invocación).
(2) «la segunda ventana de info del grimorio tiene demasiada
    informacion, crea una nueva ventana de informacion para repartir
    mejor la informacion» → TRES VENTANAS (clic der cicla BÁSICA →
    EL ARMA → EL FESTÍN → BÁSICA): TooltipToggleItem.VistaEstadisticas
    (int 0/1/2) · EL ARMA = DAÑO+RECURSOS+PROYECTIL+BONUS+notas ·
    EL FESTÍN = OJOS+LA PRIMERA CENA+PROGRESIÓN.
(3) «crea un item que le de hambre al grimorio eterno para comprobar
    si funciona, que suba el hambre de 5 en 5 y que no sea consumible»
    → LA ESENCIA DE HAMBRE (EsenciaHambre.cs): +5% al Eterno por uso
    (HambrePorUso = 0.05f), pila 1, consumable = false, en la Bolsa
    Probador, textura 24×24 propia + hjson ×3.
(4) «asegurate de que el grimorio eterno tenga la sombra de la pagina
    para cuando sale a cazar y que ademas cuando se mata un jefe su
    animacion de muerte se detenga y sea deborado por el grimorio
    usando la sombra de la pagina… circulos magicos como los de los
    agujeros negros aprisionan al jefe, lo levantan y llevan al centro
    de la pantalla… la primera vez que se mate un jefe y solo la
    primera vez» → (a) la sombra de la caza VERIFICADA (la cadena
    UpdateInventory→PasoCaza→espíritu(ai[2]=Eterno)→SombraPaginaCaza
    vive intacta); (b) LA PRIMERA CENA: FaucesGlobalNPC estado 3 —
    gatillo a ≤100 de vida con portador peleado (playerInteraction) +
    Eterno visible + tipo SIN devorar (ShardPlayer.JefesDevorados,
    persiste en el .plr); vida clavada en 100, TRES sellos
    erosionados (OrbitaLib.SigiloErosionado) que aprisionan, elevan
    (−90 px) y llevan al CENTRO VIVO de la pantalla; a los 175 t sale
    LA SOMBRA DE LA PÁGINA de la mano del portador (PrimeraCena-
    Projectile: ColumnaViva+Masa fusiforme+Devorador+almas); a los
    430 t LA MUERTE REAL (2ª pasada de CheckDead: loot completo, la
    purga tapa el gore); los worms enroscan sus segmentos; las
    sombras de la página SUELTAN a cualquier presa en festín (EnFauces
    en SombraPaginaCaza + SombraPaginaProjectile + AbsorberPresa).

CHECKS: cabecera/entradas/EOF · heap (#Strings + #US UTF-16) ·
constantes float32 del IL · EL ÁRBOL (los 6 archivos tocados + los 2
nuevos) · hjson ×3 empaquetado == fuente · rawimg nuevas.
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
    # === LA PRIMERA CENA (la .110) ===
    'PrimeraCenaProjectile', 'ArrancarPrimeraCena', 'PrimeraCenaPendiente',
    'PrimeraCenaPreAI', 'EnFauces', 'JefesDevorados', 'EsenciaHambre',
    'ESTADO_PRIMERA_CENA', 'VidaUmbralPrimeraCena', 'LargoPrimeraCena',
    'TieneEternoVisible', 'CentroDePantalla', '_posCenaBase',
    # === LAS TRES VENTANAS (la .110) ===
    'VistaEstadisticas',
    # === LA ESENCIA DE HAMBRE (la .110) ===
    'HambrePorUso',
    # === la regresión .105–.109 (los ojos y su casa) ===
    'OjoEternoMinion', 'OjoEternoBuff', 'DrawElSello', 'DrawElOjo',
    'ESCALA', 'CADENCIA_BASE', 'VelocidadNivel', 'RitmoPropio',
    'CadenciaPersonal', 'PasoOrbita', 'FaseOrbital', 'AnilloDe',
    '_faseOrbital', 'NivelDelOjo', 'MinionChainCount', 'MaxCadenasOjo',
    'TruenoPerlinProjectile', 'DrawArcoOjo', 'CanalOjo', 'BocaDelOjo',
    'PasoOjoIcono', 'SnapDireccionIcono', '_iconoDesp',
    # === la familia viva ===
    'GrimoireEternal', 'GrimorioHambrientoNervioso', 'EstadoGrimorio',
    'EstadoDeLibro', 'EstadoEterno', 'LanzarPareja', 'TicksQuietudSalida',
    'TasaHambre', 'GrimorioNerviosoFlotante', 'SombraPaginaCaza',
    'SombraPaginaProjectile', 'FauceDevoradorProjectile',
    'FaucesGlobalNPC', 'Equinoccio', 'AbsorberPresa',
    'HambrePorPresa', 'ReduccionHambre', 'EstrellasDe',
    'SigiloErosionado', 'GolpeMotor', 'Bruma', 'IRIS_ESC',
    'CarnadaDelGrimorio', 'BolsaProbador',
]

MUERTOS_HEAP = [
    'GrimorioSellado', 'TintaViva', 'GrimorioEstrellado', 'EcoEmperatriz',
    'VeredictoEmperatriz', 'EstelaAethonProjectile', 'TrazoEmperatrizProjectile',
    'FragmentoAethon', 'ExplosionAethon', 'SistemaObservado',
    'CosmicOrbMinion', 'CosmicOrbBuff', 'CosmicOrbBolt',
    'DrawElRayo', 'CanalPerlin', 'get_Cadencia',
]

# ldstr (UTF-16LE, #US heap) — LAS TRES VENTANAS + LA PRIMERA CENA
VIVOS_US = [
    # las ventanas nuevas (.110)
    'Click der para ver EL ARMA',
    'Click der para ver EL FESTÍN',
    '═══ LA PRIMERA CENA ═══',
    'probabilidad de crítico mágico',
    'penetración de armadura mágica',
    'daño mágico (este libro)',
    'Sostenido: crítico · penetración · regeneración · reducción',
    'Barra rápida: vida/maná (tope +100) · slots de minion',
    'Los ojos leen el nivel con el que fueron invocados',
    'lo devora entero — con botín completo. Solo UNA vez.',
    # la regresión .108/.109 (el orden y la cadena)
    'HambreBarra', 'HambreEstado',
    'Próximo hito: Nivel',
    '═══ DAÑO ═══', '═══ RECURSOS ═══', '═══ PROYECTIL ═══',
    '═══ BONUS ═══', '═══ OJOS DEL ETERNO ═══',
    'MinionRitmo', 'MinionCadena', 'Rayo en cadena: ',
    '+1 salto de cadena del rayo',
    'Click der para vista básica',
]

# ldstr muertos: NINGUNO — «Click der para vista completa» vive en el
# SeerOrb (SU conmutador, no el del grimorio): no es un muerto.
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
    rutas = {p for p, _, _ in entries}

    print('=== 1. CABECERA / ENTRADAS / EOF ===')
    if ver == '6.50.110' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    # .109 = 387; +1 = EsenciaHambre.rawimg (los .cs viajan en la DLL)
    if len(entries) == 388:
        ok('388 entradas (387 de la .109 + EsenciaHambre.rawimg)')
    else:
        mal(f'se esperaban 388 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. EL HEAP — LA PRIMERA CENA, LAS TRES VENTANAS, LA ESENCIA ===')
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
    for s in MUERTOS_US:
        if u16(s) in dll:
            mal(f'el ldstr muerto VIVE: {s}')
        else:
            ok(f'ldstr muerto ausente: {s}')

    print('=== 3. LAS CONSTANTES DE LA .110 + REGRESIÓN (float32) ===')
    def en_dll(b):
        return b in dll
    constantes = [
        # === LA PRIMERA CENA (la .110) ===
        (f32(430.0), '430f — T_MUERTE: el largo del cine (LargoPrimeraCena)'),
        (f32(345.0), '345f — T_TRAGO: los sellos encienden y la elipse se inhala'),
        (f32(175.0), '175f — T_FESTIN: la sombra de la página sale del grimorio'),
        (f32(150.0), '150f — T_CENTRO: el preso queda colgado del centro vivo'),
        (f32(-90.0), '−90f — LA ELEVACIÓN (los sellos lo levantan)'),
        (f32(3.1), '3.1f — el GRAN SELLO (3,1× el radio del jefe)'),
        (f32(0.92), '0.92f — el gran sello se ciñe al exprimir (−92%)'),
        (f32(0.80), '0.80f — el sello íntimo del vice (0,80× el jefe)'),
        (f32(0.45), '0.45f — el velo de la sala de cine (Vignette)'),
        (f32(62.0), '62f — el grosor fusiforme del cuerpo de la sombra'),
        # === LA ESENCIA DE HAMBRE (la .110) ===
        (f32(0.05), '0.05f — HambrePorUso: +5% POR USO (la letra: «de 5 en 5»)'),
        # === la regresión .109 (la mitad, el Ruinous, la cadena) ===
        (f32(0.375), '0.375f — ESCALA: EL OJO A LA MITAD (regresión .109)'),
        (f32(46.0), '46f — la base de las órbitas (regresión .109)'),
        (f32(34.0), '34f — el escalón de las órbitas (regresión .109)'),
        (f32(0.42), '0.42f — ω par: GIRO CONSTANTE (regresión .109)'),
        (f32(1.0 / 216000.0), '1f/216000f — EL RELOJ DE LA HORA (regresión .103)'),
        (f32(0.10), '0.10f — el jefe vale 10% de hambre (regresión .104)'),
        # el repertorio heredado
        (f32(2.55), '2.55f — el aro del círculo mágico (regresión .106)'),
        (f32(0.5), '0.5f — EL 50% de la velocidad de ataque (regresión .107)'),
    ]
    for bytes_, etiqueta in constantes:
        if en_dll(bytes_):
            ok(f'{etiqueta} vive en el IL')
        else:
            mal(f'FALTA en la DLL: {etiqueta}')

    print('=== 4. EL ÁRBOL — LA PRIMERA CENA, LAS TRES VENTANAS, LA ESENCIA ===')
    def leer(ruta):
        return open(f'{ARBOL}/{ruta}', encoding='utf-8').read()
    fauces = leer('Content/Globals/FaucesGlobalNPC.cs')
    cena = leer('Content/Projectiles/Sombras/PrimeraCenaProjectile.cs')
    et = leer('Content/Weapons/GrimoireEternal.cs')
    toggle = leer('Content/Globals/TooltipToggleItem.cs')
    sp = leer('Content/Players/ShardPlayer.cs')
    esencia = leer('Content/Items/EsenciaHambre.cs')
    caza = leer('Content/Projectiles/Nervioso/SombraPaginaCaza.cs')
    sombraarma = leer('Content/Projectiles/Sombras/SombraPaginaProjectile.cs')
    nervioso = leer('Content/Weapons/GrimorioHambrientoNervioso.cs')
    bolsas = leer('Content/Items/Bolsas/BolsasCategorias.cs')
    for patron, etiqueta, src in [
        # === LA PRIMERA CENA — el gatillo y el registro ===
        ('public const byte ESTADO_PRIMERA_CENA = 3;', 'EL ESTADO 3 (la primera cena)', fauces),
        ('public const int VidaUmbralPrimeraCena = 100;', 'EL UMBRAL DE 100 DE VIDA (la letra)', fauces),
        ('public const int LargoPrimeraCena = 430;', 'EL LARGO DEL CINE (430 t ≈ 7,2 s)', fauces),
        ('dueñoPool.life <= VidaUmbralPrimeraCena', 'EL GATILLO: el jefe llegó a 100', fauces),
        ('PrimeraCenaPendiente(dueñoPool, out Player portador)', '¿le toca su primera cena?', fauces),
        ('sp.JefesDevorados.Contains(jefe.type)', 'SOLO LA PRIMERA VEZ (la lista del portador)', fauces),
        ('sp.JefesDevorados.Add(jefe.type)', 'el registro ANTES del espectáculo', fauces),
        ('jefe.life = VidaUmbralPrimeraCena;', 'la vida CLAVADA en 100', fauces),
        ('private static bool TieneEternoVisible(Player p)', 'el portador con el Eterno en la barra', fauces),
        ('jefe.playerInteraction[i]', 'el portador PELEÓ al jefe', fauces),
        # === LA PRIMERA CENA — la coreografía ===
        ('private void PrimeraCenaPreAI(NPC npc)', 'el motor del cine', fauces),
        ('Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f',
         'EL CENTRO VIVO DE LA PANTALLA', fauces),
        ('_posCenaBase + new Vector2(0f, -90f', 'LA ELEVACIÓN (−90 px)', fauces),
        ('if (seg.realLife != npc.whoAmI) continue;', 'LOS WORMS: enroscar los segmentos', fauces),
        ('npc.checkDead();', 'LA MUERTE REAL (2ª pasada — loot completo)', fauces),
        ('_cenaActiva = -1;', 'la mesa queda libre', fauces),
        ('public static bool EnFauces(NPC npc)', 'EnFauces — el respeto al festín ajeno', fauces),
        ('DevorarTick >= 260', 'el sprite se retira a mitad del festín', fauces),
        # === LA PRIMERA CENA — la película ===
        ('OrbitaLib.SigiloErosionado(posPantalla, rGrande * latido', 'EL GRAN SELLO erosionado', cena),
        ('OrbitaLib.SigiloErosionado(posPantalla, rMedio * latido', 'EL SELLO MEDIO (contrarrotando)', cena),
        ('OrbitaLib.SigiloErosionado(posPantalla, rIntimo * latido', 'EL SELLO ÍNTIMO (el vice)', cena),
        ('SombrasLib.Devorador(punta, jefe, intensidad', 'EL DEVORADOR v2 (la elipse completa)', cena),
        ('portador.MountedCenter + new Vector2(portador.direction * 12f, 6f)',
         'LA RAÍZ: la MANO del portador (el grimorio en ella)', cena),
        ('SombrasLib.Vignette(0.45f * presencia)', 'el velo de la sala de cine', cena),
        ('SombrasLib.Ojo(pos, 7f, b - pos, 1f)', 'las ALMAS vuelan al portador', cena),
        ('SombrasLib.PurgaVisualMuerte(Projectile.Center, 420f)', 'la purga tapa el gore (el loot limpio)', cena),
        # === LAS TRES VENTANAS ===
        ('public static int VistaEstadisticas = 0;', 'el ciclo 0/1/2 (TooltipToggleItem)', toggle),
        ('VistaEstadisticas = (VistaEstadisticas + 1) % 3;', 'el clic der cicla las TRES', toggle),
        ('bool ventanaArma = sl != null && vista == 1;', 'VENTANA 1: EL ARMA', et),
        ('bool ventanaFestin = sl != null && vista == 2;', 'VENTANA 2: EL FESTÍN', et),
        ('if (!ventanaArma)', 'la progresión es del festín (y de la básica)', et),
        # v6.50.110 — el indicador (dentro del switch del ModeIndicator)
        ('Click der para ver EL FESTÍN', 'el indicador dice a cuál vas (EL FESTÍN)', et),
        ('Click der para ver EL ARMA', 'el indicador dice a cuál vas (EL ARMA)', et),
        # === LA ESENCIA DE HAMBRE ===
        ('public const float HambrePorUso = 0.05f;', '+5% POR USO (de 5 en 5)', esencia),
        ('Item.consumable = false;', 'NO CONSUMIBLE (la letra)', esencia),
        ('GrimoireEternal.EstadoEterno', 'alimenta al ETERNO (sólo él)', esencia),
        ('EcoRed.SincronizarHambre(player);', 'en MP la barra viaja YA', esencia),
        ('l.Add((ModContent.ItemType<Items.EsenciaHambre>(), 1));', 'en la Bolsa Probador (la lección del kit)', bolsas),
        # === LAS SOMBRAS RESPETAN EL FESTÍN ===
        ('if (Globals.FaucesGlobalNPC.EnFauces(presa))', 'SombraPaginaCaza SUELTA la presa', caza),
        ('if (FaucesGlobalNPC.EnFauces(presa))', 'SombraPaginaProjectile SUELTA la presa', sombraarma),
        ('if (Globals.FaucesGlobalNPC.EnFauces(presa))\n                return;', 'AbsorberPresa RESPETA el festín', nervioso),
        # === LA PERSISTENCIA ===
        ('tag["jefesDevorados"] = JefesDevorados;', 'se guarda con el jugador', sp),
        ('tag.GetList<int>("jefesDevorados")', 'se carga con el jugador', sp),
        # === LA REGLA DE LA .110 EN LOS HOOKS VIEJOS ===
        ('DevorarEstado == 1 || DevorarEstado == ESTADO_PRIMERA_CENA', 'congelar/CheckActive/DoT cubren la cena', fauces),
    ]:
        if patron in src:
            ok(etiqueta)
        else:
            mal(f'FALTA en el árbol: {etiqueta}')

    print('=== 5. hjson ×3 — EL PAQUETE == EL ÁRBOL (semántico: el packager')
    print('             normaliza llaves planas ↔ bloques y borra comentarios) ===')
    import re as _re

    def pares(texto):
        """Extrae el conjunto «Llave: Valor» de un hjson (tolerante a
        bloques/planos y comentarios): sólo pares SIN ''' multilinea."""
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

    import os
    for loc in ('es-ES', 'en-US', 'es-MX'):
        ruta_p = f'Localization/{loc}_Mods.AethonMod.hjson'
        ruta_a = f'{ARBOL}/{ruta_p}'
        if ruta_p not in rutas:
            mal(f'{loc}: no está en el paquete')
            continue
        pak = blobs[ruta_p].decode('utf-8')
        src = open(ruta_a, encoding='utf-8').read()
        # el es-MX (ni cultura base ni fallback) viaja BYTE a byte
        if loc == 'es-MX':
            if blobs[ruta_p] == open(ruta_a, 'rb').read():
                ok('es-MX: empaquetado == fuente (byte a byte)')
            else:
                mal('es-MX: DIFIERE del árbol')
        # TODOS: el mismo conjunto de pares llave:valor
        faltan = pares(src) - pares(pak)
        sobran = pares(pak) - pares(src)
        if not faltan and not sobran:
            ok(f'{loc}: {len(pares(src))} pares llave:valor idénticos (semántico)')
        else:
            for k in list(faltan)[:5]:
                mal(f'{loc}: el par se PERDIÓ en el paquete: {k}')
            for k in list(sobran)[:5]:
                mal(f'{loc}: el par SOBRA en el paquete: {k}')
        # las claves nuevas
        cuerpo = pak
        if 'EsenciaHambre' in cuerpo and 'Sube:' in cuerpo:
            ok(f'{loc}: la EsenciaHambre vive (ítem + voz)')
        else:
            mal(f'{loc}: FALTA la EsenciaHambre')
        if loc == 'en-US' and 'Hunger Essence' in cuerpo:
            ok('en-US: Hunger Essence (el nombre)')
        if loc != 'en-US' and 'Esencia de Hambre' in cuerpo:
            ok(f'{loc}: Esencia de Hambre (el nombre)')
        if 'PrimeraCenaProjectile.DisplayName: La Primera Cena' in cuerpo or \
           'PrimeraCenaProjectile.DisplayName: The First Feast' in cuerpo:
            ok(f'{loc}: el nombre del proyectil del cine')
        else:
            mal(f'{loc}: FALTA el nombre del proyectil del cine')
        if loc == 'es-MX' and '\t' in cuerpo:
            mal('es-MX: se coló un TAB')
        else:
            ok(f'{loc}: indentación respetada')

    print('=== 6. LA RAWIMG NUEVA — EsenciaHambre 24×24 ===')
    # formato raw de la casa: int32 versión(1) + int32 ancho + int32 alto
    # + píxeles RGBA (el patrón de CarnadaDelGrimorio.rawimg: 2316 B)
    raw = blobs.get('Content/Items/EsenciaHambre.rawimg')
    if raw is None:
        mal('FALTA Content/Items/EsenciaHambre.rawimg')
    else:
        magic, w, h = struct.unpack('<iii', raw[:12])
        if magic == 1 and (w, h) == (24, 24) and len(raw) == 12 + 24 * 24 * 4:
            ok(f'EsenciaHambre.rawimg 24×24 RGBA (magic 1, {len(raw)} B — el formato de la casa)')
        else:
            mal(f'EsenciaHambre.rawimg inesperada: magic {magic}, {w}×{h}, {len(raw)} B')

    print()
    if FALLOS:
        print(f'=== {len(FALLOS)} FALLOS ===')
        for f in FALLOS:
            print(f'  ✗ {f}')
        sys.exit(1)
    print('=== TODO OK — v6.50.110 VERIFICADA ===')


if __name__ == '__main__':
    main()
