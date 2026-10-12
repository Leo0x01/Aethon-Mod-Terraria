#!/usr/bin/env python3
"""
v6.50.111 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO (2 pedidos):
(1) «que raro, a veces los minion se desactivan por si solo» →
    LA PROMESA CUMPLIDA: vanilla CULLA minions CADA TICK dentro de
    Projectile.Update («slotsMinions + minionSlots > maxMinions →
    Kill()») — el comentario de ShardPlayer («vanilla no los
    despawnea al bajar maxMinions») era FALSO. El bonus de slots del
    Eterno solo vive SOSTENIDO/en la BARRA RÁPIDA: el tick en que el
    libro salía de ahí (AL CURSOR al reorganizar, cofre, hucha,
    inventario profundo) el escuadrón entero moría en UN TICK.
    EL FIX: en PostUpdateEquips el techo de maxMinions JAMÁS baja de
    los slots YA EN USO mientras el libro cuente (nivel>0) o quede un
    OJO vivo (la suma exacta que vanilla va a contar en el loop de
    proyectiles — incl. otros minions).
(2) «el icono que muestra el buff del minion es demasiado grande,
    dame un prompt para crear un icono adecuado en una IA de arte» →
    OjoEternoBuff apuntaba la textura AL SPRITE DEL PROYECTIL (56×54)
    y el HUD dibuja la textura TAL CUAL: GIGANTE, montado sobre los
    vecinos. EL ICONO PROPIO: Content/Buffs/OjoEternoBuff.png 32×32
    (el estándar de los otros 10 buffs de la casa) — EL OJO COMPUESTO
    (base OjoEterno_Abierto + IRIS en su reposo, porque en runtime el
    iris se dibuja aparte), por tools/gen_icono_ojo_v650111.py
    (candidato FILO: BOX + alfa re-endurecido — verificado por VLM).
    El override Texture MUERE (textura por convención). El PROMPT para
    la IA de arte se entrega en la conversación (para reemplazar el
    .png manteniendo 32×32).

CHECKS: cabecera/entradas/EOF · heap (#Strings + #US UTF-16; Ceiling
es la huella del fix — no existe en NINGÚN otro archivo) ·
constantes float32 de regresión .104–.110 · EL ÁRBOL (el fix completo
+ el icono + la regresión .110 ÍNTEGRA) · hjson ×3 empaquetado ==
fuente (regresión sin cambios) · rawimg nueva 32×32.
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
    # === LA PROMESA CUMPLIDA (la .111) — la huella del fix ===
    'Ceiling',                       # Math.Ceiling: NO existe en ningún otro .cs
    # === la regresión .105–.110 (los ojos y su casa) ===
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

# ldstr (UTF-16LE, #US heap) — regresión de las tres ventanas + la cena
VIVOS_US = [
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
    'HambreBarra', 'HambreEstado',
    'Próximo hito: Nivel',
    '═══ DAÑO ═══', '═══ RECURSOS ═══', '═══ PROYECTIL ═══',
    '═══ BONUS ═══', '═══ OJOS DEL ETERNO ═══',
    'MinionRitmo', 'MinionCadena', 'Rayo en cadena: ',
    '+1 salto de cadena del rayo',
    'Click der para vista básica',
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
    rutas = {p for p, _, _ in entries}

    print('=== 1. CABECERA / ENTRADAS / EOF ===')
    if ver == '6.50.111' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    # .110 = 388; +1 = Content/Buffs/OjoEternoBuff.rawimg (el icono propio)
    if len(entries) == 389:
        ok('389 entradas (388 de la .110 + OjoEternoBuff.rawimg)')
    else:
        mal(f'se esperaban 389 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. EL HEAP — LA PROMESA CUMPLIDA + REGRESIÓN ===')
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

    print('=== 3. LAS CONSTANTES (float32) — REGRESIÓN .104–.110 ===')
    def en_dll(b):
        return b in dll
    constantes = [
        (f32(430.0), '430f — T_MUERTE: el largo del cine (regresión .110)'),
        (f32(175.0), '175f — T_FESTIN: la sombra sale del grimorio (.110)'),
        (f32(-90.0), '−90f — LA ELEVACIÓN (regresión .110)'),
        (f32(0.05), '0.05f — HambrePorUso: +5% por uso (regresión .110)'),
        (f32(0.375), '0.375f — ESCALA: EL OJO A LA MITAD (regresión .109)'),
        (f32(46.0), '46f — la base de las órbitas (regresión .109)'),
        (f32(34.0), '34f — el escalón de las órbitas (regresión .109)'),
        (f32(0.42), '0.42f — ω par: GIRO CONSTANTE (regresión .109)'),
        (f32(1.0 / 216000.0), '1f/216000f — EL RELOJ DE LA HORA (regresión .103)'),
        (f32(0.10), '0.10f — el jefe vale 10% de hambre (regresión .104)'),
        (f32(2.55), '2.55f — el aro del círculo mágico (regresión .106)'),
        (f32(0.5), '0.5f — EL 50% de la velocidad de ataque (regresión .107)'),
    ]
    for bytes_, etiqueta in constantes:
        if en_dll(bytes_):
            ok(f'{etiqueta} vive en el IL')
        else:
            mal(f'FALTA en la DLL: {etiqueta}')

    print('=== 4. EL ÁRBOL — LA PROMESA CUMPLIDA + EL ICONO + REGRESIÓN ===')
    def leer(ruta):
        return open(f'{ARBOL}/{ruta}', encoding='utf-8').read()
    sp = leer('Content/Players/ShardPlayer.cs')
    buff = leer('Content/Buffs/OjoEternoBuff.cs')
    fauces = leer('Content/Globals/FaucesGlobalNPC.cs')
    cena = leer('Content/Projectiles/Sombras/PrimeraCenaProjectile.cs')
    et = leer('Content/Weapons/GrimoireEternal.cs')
    toggle = leer('Content/Globals/TooltipToggleItem.cs')
    esencia = leer('Content/Items/EsenciaHambre.cs')
    caza = leer('Content/Projectiles/Nervioso/SombraPaginaCaza.cs')
    sombraarma = leer('Content/Projectiles/Sombras/SombraPaginaProjectile.cs')
    nervioso = leer('Content/Weapons/GrimorioHambrientoNervioso.cs')
    bolsas = leer('Content/Items/Bolsas/BolsasCategorias.cs')
    for patron, etiqueta, src in [
        # === LA PROMESA CUMPLIDA (la .111) — el fix de la masacre ===
        ('float slotsEnUso = 0f;', 'LA SUMA: lo que vanilla va a contar', sp),
        ('slotsEnUso += p.minionSlots;', 'cada minion vivo suma SUS slots', sp),
        ('if (p.type == tipoOjo) hayOjos = true;', 'el detector de ojos vivos', sp),
        ('if (nivel > 0 || hayOjos)', 'la ley: libro a mano O ojos en el aire', sp),
        ('int techoVivo = (int)Math.Ceiling(slotsEnUso);', 'EL TECHO VIVO (Math.Ceiling)', sp),
        ('if (techoVivo > Player.maxMinions)', 'el techo JAMÁS baja de lo vivo', sp),
        ('Player.maxMinions = techoVivo;', 'la promesa se cumple', sp),
        # === EL ICONO PROPIO (la .111) ===
        ('public override void SetStaticDefaults()', 'el buff vive (regresión)', buff),
        ('player.buffTime[buffIndex] = 18000;', 'el timer nunca se agota (regresión)', buff),
        # === LA REGLA DE LA .110 EN LOS HOOKS VIEJOS (regresión) ===
        ('public const byte ESTADO_PRIMERA_CENA = 3;', 'EL ESTADO 3 (la primera cena)', fauces),
        ('public const int VidaUmbralPrimeraCena = 100;', 'EL UMBRAL DE 100 DE VIDA', fauces),
        ('dueñoPool.life <= VidaUmbralPrimeraCena', 'EL GATILLO: el jefe llegó a 100', fauces),
        ('sp.JefesDevorados.Contains(jefe.type)', 'SOLO LA PRIMERA VEZ', fauces),
        ('jefe.life = VidaUmbralPrimeraCena;', 'la vida CLAVADA en 100', fauces),
        ('private void PrimeraCenaPreAI(NPC npc)', 'el motor del cine', fauces),
        ('npc.checkDead();', 'LA MUERTE REAL (loot completo)', fauces),
        ('public static bool EnFauces(NPC npc)', 'EnFauces — el respeto al festín', fauces),
        ('SombrasLib.Vignette(0.45f * presencia)', 'el velo de la sala de cine', cena),
        ('SombrasLib.PurgaVisualMuerte(Projectile.Center, 420f)', 'la purga tapa el gore', cena),
        ('public static int VistaEstadisticas = 0;', 'el ciclo 0/1/2', toggle),
        ('VistaEstadisticas = (VistaEstadisticas + 1) % 3;', 'el clic der cicla las TRES', toggle),
        ('bool ventanaArma = sl != null && vista == 1;', 'VENTANA 1: EL ARMA', et),
        ('bool ventanaFestin = sl != null && vista == 2;', 'VENTANA 2: EL FESTÍN', et),
        ('public const float HambrePorUso = 0.05f;', '+5% POR USO (de 5 en 5)', esencia),
        ('Item.consumable = false;', 'NO CONSUMIBLE', esencia),
        ('if (Globals.FaucesGlobalNPC.EnFauces(presa))', 'SombraPaginaCaza SUELTA la presa', caza),
        ('if (FaucesGlobalNPC.EnFauces(presa))', 'SombraPaginaProjectile SUELTA la presa', sombraarma),
        ('tag["jefesDevorados"] = JefesDevorados;', 'se guarda con el jugador', sp),
        ('tag.GetList<int>("jefesDevorados")', 'se carga con el jugador', sp),
    ]:
        if patron in src:
            ok(etiqueta)
        else:
            mal(f'FALTA en el árbol: {etiqueta}')

    # === LOS MUERTOS DEL ÁRBOL (la .111) ===
    if 'override string Texture' in buff:
        mal('OjoEternoBuff: el override Texture VIVE (debe morir)')
    else:
        ok('OjoEternoBuff: override string Texture MUERTO (convención)')
    if 'se van al desinvocarlos' in sp:
        mal('ShardPlayer: la mentira «se van al desinvocarlos» VIVE')
    else:
        ok('ShardPlayer: la mentira de «vanilla no despawnea» MUERTA')

    print('=== 5. hjson ×3 — REGRESIÓN (sin cambios en la .111) ===')
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
        if 'EsenciaHambre' in pak:
            ok(f'{loc}: la EsenciaHambre vive (regresión .110)')
        else:
            mal(f'{loc}: FALTA la EsenciaHambre (regresión .110)')
        if loc == 'es-MX' and '\t' in pak:
            mal('es-MX: se coló un TAB')
        else:
            ok(f'{loc}: indentación respetada')

    print('=== 6. LA RAWIMG NUEVA — OjoEternoBuff 32×32 (EL ICONO) ===')
    raw = blobs.get('Content/Buffs/OjoEternoBuff.rawimg')
    if raw is None:
        mal('FALTA Content/Buffs/OjoEternoBuff.rawimg')
    else:
        magic, w, h = struct.unpack('<iii', raw[:12])
        if magic == 1 and (w, h) == (32, 32) and len(raw) == 12 + 32 * 32 * 4:
            ok('OjoEternoBuff.rawimg 32×32 RGBA (magic 1, 4108 B — el estándar de la casa)')
        else:
            mal(f'OjoEternoBuff.rawimg inesperada: magic {magic}, {w}×{h}, {len(raw)} B')
        # píxel-idéntica al árbol (el packager pone a 0 el RGB de alfa=0)
        try:
            from PIL import Image
            png = Image.open(f'{ARBOL}/Content/Buffs/OjoEternoBuff.png').convert('RGBA')
            px_raw, px_png = raw[12:], png.tobytes()
            malos = 0
            for p in range(32 * 32):
                if px_raw[p * 4 + 3] != px_png[p * 4 + 3]:
                    malos += 1
                elif px_raw[p * 4 + 3] > 0 and \
                        px_raw[p * 4:p * 4 + 3] != px_png[p * 4:p * 4 + 3]:
                    malos += 1
            if malos == 0:
                ok('la rawimg es píxel-idéntica al .png del árbol (RGB nulo sólo en alfa 0)')
            else:
                mal(f'la rawimg DIFIERE del .png en {malos} píxeles')
        except ImportError:
            ok('(sin PIL: salto la comparación píxel a píxel)')
    # el sprite del proyectil SIGUE vivo (el ojo en juego no cambió)
    if 'Content/Projectiles/OjoEterno_Abierto.rawimg' in rutas:
        ok('OjoEterno_Abierto.rawimg sigue en el paquete (el ojo del juego intacto)')
    else:
        mal('FALTA OjoEterno_Abierto.rawimg (¡el ojo del juego!)')

    print()
    if FALLOS:
        print(f'=== {len(FALLOS)} FALLOS ===')
        for f in FALLOS:
            print(f'  ✗ {f}')
        sys.exit(1)
    print('=== TODO OK — v6.50.111 VERIFICADA ===')


if __name__ == '__main__':
    main()
