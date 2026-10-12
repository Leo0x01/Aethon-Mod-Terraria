#!/usr/bin/env python3
"""
v6.50.112 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO (2 pedidos):
(1) «el libro cuando esta fuera se mueve muy rapido, debe ser mas
    lento» → EL MUELLE DE LA .111 (tirón 0.02, freno 0.90, SIN TOPE
    en MODO_CAZA) pegaba el libro a la cabeza con tirón al 27% más
    rápido que la carrera y lo convertía en MISIL al teletransportarse
    (127 px/t medidos en tools/sim_libro_v650112.py). EL MUELLE NUEVO:
    tirón 0.009 (menos de la mitad — el libro se queda ATRÁS: 42 px al
    caminar, 127 al esprintar), freno 0.88 y EL TOPE GRADUADO
    Clamp(dist*0.01, 10, 20) — 10 px/t cerca del dueño, 20 sólo si
    quedó muy lejos. LA VUELTA (MODO_VOLVER) deja de ser misil:
    empuje 0.32 (era 0.90), tope 12 (era 22). Y el espíritu perdedor
    de SoyDuplicado ahora se DISUELVE EN POLVO (con la vuelta lenta es
    más fácil que pierda contra un cazador nuevo).
(2) «necesito un item que reinicie el contador de los jefes para
    verificar como devora los jefes, este iten no debe ser
    consumible» → LA PÁGINA EN BLANCO (Items/PaginaEnBlanco.cs): borra
    ShardPlayer.JefesDevorados del portador (la «solo la primera vez»
    de LA PRIMERA CENA de la .110) — cada tipo de jefe vuelve a
    estrenar su cena completa. NO CONSUMIBLE, pila 1, la voz con el
    conteo (Reset/Nada), textura 24×24 (la hermana de la Esencia de
    Hambre: el pliego con la tinta BORRÁNDOSE y las motas alzándose).

CHECKS: cabecera/entradas/EOF (390) · heap (#Strings + #US UTF-16;
PaginaEnBlanco + las constantes del muelle nuevo) · constantes float32
de regresión .104–.111 · EL ÁRBOL (el muelle nuevo + los muertos del
viejo + el ítem + el registro en la bolsa + la regresión .111 ÍNTEGRA)
· hjson ×3 empaquetado == fuente + las claves nuevas · rawimg nueva
24×24 píxel-idéntica + las rawimg de regresión.
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
    # === LA .112 — el perezoso y la página en blanco ===
    'PaginaEnBlanco',
    # === la regresión .105–.111 (los ojos, la cena y la promesa) ===
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
    'Ceiling',                       # la huella del fix .111 (no existe en otro .cs)
]

MUERTOS_HEAP = [
    'GrimorioSellado', 'TintaViva', 'GrimorioEstrellado', 'EcoEmperatriz',
    'VeredictoEmperatriz', 'EstelaAethonProjectile', 'TrazoEmperatrizProjectile',
    'FragmentoAethon', 'ExplosionAethon', 'SistemaObservado',
    'CosmicOrbMinion', 'CosmicOrbBuff', 'CosmicOrbBolt',
    'DrawElRayo', 'CanalPerlin', 'get_Cadencia',
]

# ldstr (UTF-16LE, #US heap) — la .112 + la regresión de las tres
# ventanas y la cena
VIVOS_US = [
    # === LA .112 — las claves de la voz del olvido ===
    'Mods.AethonMod.PaginaEnBlanco.Reset',
    'Mods.AethonMod.PaginaEnBlanco.Nada',
    # === la regresión ===
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
    if ver == '6.50.112' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    # .111 = 389; +1 = Content/Items/PaginaEnBlanco.rawimg (el ítem nuevo)
    if len(entries) == 390:
        ok('390 entradas (389 de la .111 + PaginaEnBlanco.rawimg)')
    else:
        mal(f'se esperaban 390 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. EL HEAP — EL PEREZOSO + LA PÁGINA + REGRESIÓN ===')
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

    print('=== 3. LAS CONSTANTES (float32) — EL MUELLE NUEVO + REGRESIÓN ===')
    def en_dll(b):
        return b in dll
    constantes = [
        # === LA .112 — EL MUELLE DEL PEREZOSO ===
        (f32(0.009), '0.009f — el tirón a MENOS de la mitad (era 0.02)'),
        (f32(0.88), '0.88f — el freno nuevo (era 0.90)'),
        (f32(0.01), '0.01f — la pendiente del TOPE GRADUADO'),
        (f32(10.0), '10f — el tope CERCA del dueño'),
        (f32(20.0), '20f — el tope LEJOS (que no se pierda)'),
        (f32(0.32), '0.32f — el empuje del planeo de vuelta (era 0.90)'),
        (f32(12.0), '12f — el tope del planeo de vuelta (era 22)'),
        # === la regresión .104–.111 ===
        (f32(430.0), '430f — T_MUERTE: el largo del cine (regresión .110)'),
        (f32(175.0), '175f — T_FESTIN: la sombra sale del grimorio (.110)'),
        (f32(-90.0), '−90f — LA ELEVACIÓN (regresión .110)'),
        (f32(0.05), '0.05f — HambrePorUso: +5% por uso (regresión .110)'),
        (f32(0.375), '0.375f — ESCALA: EL OJO A LA MITAD (regresión .109)'),
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

    print('=== 4. EL ÁRBOL — EL MUELLE NUEVO + LOS MUERTOS + EL ÍTEM ===')
    def leer(ruta):
        return open(f'{ARBOL}/{ruta}', encoding='utf-8').read()
    esp = leer('Content/Projectiles/Nervioso/GrimorioNerviosoFlotante.cs')
    pagina = leer('Content/Items/PaginaEnBlanco.cs')
    sp = leer('Content/Players/ShardPlayer.cs')
    buff = leer('Content/Buffs/OjoEternoBuff.cs')
    fauces = leer('Content/Globals/FaucesGlobalNPC.cs')
    et = leer('Content/Weapons/GrimoireEternal.cs')
    toggle = leer('Content/Globals/TooltipToggleItem.cs')
    esencia = leer('Content/Items/EsenciaHambre.cs')
    caza = leer('Content/Projectiles/Nervioso/SombraPaginaCaza.cs')
    sombraarma = leer('Content/Projectiles/Sombras/SombraPaginaProjectile.cs')
    cena = leer('Content/Projectiles/Sombras/PrimeraCenaProjectile.cs')
    bolsas = leer('Content/Items/Bolsas/BolsasCategorias.cs')
    for patron, etiqueta, src in [
        # === EL MUELLE DEL PEREZOSO (la .112) ===
        ('Projectile.velocity += (destino - Projectile.Center) * 0.009f;',
         'EL TIRÓN NUEVO: 0.009 (menos de la mitad)', esp),
        ('Projectile.velocity *= 0.88f;', 'EL FRENO NUEVO: 0.88', esp),
        ('MathHelper.Clamp(', 'el tope graduado usa MathHelper.Clamp', esp),
        ('Vector2.Distance(destino, Projectile.Center) * 0.01f, 10f, 20f);',
         'EL TOPE GRADUADO: clamp(dist*0.01, 10, 20)', esp),
        ('if (velCaza > tope) Projectile.velocity *= tope / velCaza;',
         'el tope SE APLICA (ya nadie vuela sin límite)', esp),
        ('Projectile.velocity += hacia * 0.32f;', 'LA VUELTA: empuje 0.32 (era 0.90)', esp),
        ('if (vel > 12f) Projectile.velocity *= 12f / vel;',
         'LA VUELTA: tope 12 (era 22)', esp),
        ('LA DISOLUCIÓN CON POLVO', 'el perdedor del duplicado se disuelve', esp),
        # === LA PÁGINA EN BLANCO (la .112) ===
        ('sp.JefesDevorados.Clear();', 'EL OLVIDO: la lista queda VACÍA', pagina),
        ('Item.consumable = false;', 'NO CONSUMIBLE', pagina),
        ('Item.maxStack = 1;', 'pila 1 (el patrón de la casa)', pagina),
        ('Mods.AethonMod.PaginaEnBlanco.Reset', 'la voz del olvido: Reset', pagina),
        ('Mods.AethonMod.PaginaEnBlanco.Nada', 'la voz del olvido: Nada', pagina),
        ('l.Add((ModContent.ItemType<Items.PaginaEnBlanco>(), 1));',
         'registrada en la BOLSA DEL PROBADOR (la lección v6.50.63)', bolsas),
        # === LA REGLA DE LA .110/.111 EN LOS HOOKS (regresión) ===
        ('float slotsEnUso = 0f;', 'LA SUMA: lo que vanilla va a contar', sp),
        ('if (p.type == tipoOjo) hayOjos = true;', 'el detector de ojos vivos', sp),
        ('if (nivel > 0 || hayOjos)', 'la ley: libro a mano O ojos en el aire', sp),
        ('int techoVivo = (int)Math.Ceiling(slotsEnUso);', 'EL TECHO VIVO (.111)', sp),
        ('Player.maxMinions = techoVivo;', 'la promesa se cumple (.111)', sp),
        ('player.buffTime[buffIndex] = 18000;', 'el timer nunca se agota (regresión)', buff),
        ('public const byte ESTADO_PRIMERA_CENA = 3;', 'EL ESTADO 3 (la primera cena)', fauces),
        ('public const int VidaUmbralPrimeraCena = 100;', 'EL UMBRAL DE 100 DE VIDA', fauces),
        ('dueñoPool.life <= VidaUmbralPrimeraCena', 'EL GATILLO: el jefe llegó a 100', fauces),
        ('sp.JefesDevorados.Contains(jefe.type)', 'SOLO LA PRIMERA VEZ', fauces),
        ('jefe.life = VidaUmbralPrimeraCena;', 'la vida CLAVADA en 100', fauces),
        ('npc.checkDead();', 'LA MUERTE REAL (loot completo)', fauces),
        ('public static bool EnFauces(NPC npc)', 'EnFauces — el respeto al festín', fauces),
        ('SombrasLib.Vignette(0.45f * presencia)', 'el velo de la sala de cine', cena),
        ('public static int VistaEstadisticas = 0;', 'el ciclo 0/1/2', toggle),
        ('VistaEstadisticas = (VistaEstadisticas + 1) % 3;', 'el clic der cicla las TRES', toggle),
        ('bool ventanaArma = sl != null && vista == 1;', 'VENTANA 1: EL ARMA', et),
        ('bool ventanaFestin = sl != null && vista == 2;', 'VENTANA 2: EL FESTÍN', et),
        ('public const float HambrePorUso = 0.05f;', '+5% POR USO (de 5 en 5)', esencia),
        ('if (Globals.FaucesGlobalNPC.EnFauces(presa))', 'SombraPaginaCaza SUELTA la presa', caza),
        ('if (FaucesGlobalNPC.EnFauces(presa))', 'SombraPaginaProjectile SUELTA la presa', sombraarma),
        ('tag["jefesDevorados"] = JefesDevorados;', 'se guarda con el jugador', sp),
        ('tag.GetList<int>("jefesDevorados")', 'se carga con el jugador', sp),
    ]:
        if patron in src:
            ok(etiqueta)
        else:
            mal(f'FALTA en el árbol: {etiqueta}')

    # === LOS MUERTOS DEL ÁRBOL (la .112: el muelle viejo) ===
    if 'Projectile.velocity += (destino - Projectile.Center) * 0.02f;' in esp:
        mal('el TIRÓN VIEJO (0.02) VIVE — el misil no murió')
    else:
        ok('el tirón viejo 0.02 MUERTO')
    if 'Projectile.velocity *= 0.90f;' in esp:
        mal('el FRENO VIEJO (0.90 en velocity) VIVE')
    else:
        ok('el freno viejo 0.90 (en velocity) MUERTO')
    if 'Projectile.velocity += hacia * 0.90f;' in esp:
        mal('el MISIL de vuelta (0.90) VIVE')
    else:
        ok('el misil de vuelta (empuje 0.90) MUERTO')
    if 'vel > 22f' in esp:
        mal('el TOPE VIEJO de vuelta (22) VIVE')
    else:
        ok('el tope viejo de vuelta (22) MUERTO')
    # el override Texture del buff sigue muerto (regresión .111)
    if 'override string Texture' in buff:
        mal('OjoEternoBuff: el override Texture VIVE (debe morir)')
    else:
        ok('OjoEternoBuff: override Texture MUERTO (regresión .111)')

    print('=== 5. hjson ×3 — LAS CLAVES NUEVAS + REGRESIÓN ===')
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
        # las claves nuevas de la .112 en el PAQUETE
        marca_tooltip = 'FIRST DINNER' if loc == 'en-US' else 'SU PRIMERA CENA'
        if 'PaginaEnBlanco' in pak and marca_tooltip in pak:
            ok(f'{loc}: la Página en Blanco vive en el paquete')
        else:
            mal(f'{loc}: FALTA la Página en Blanco')
        if ('PaginaEnBlanco.Reset' in pak or _re.search(r'(?m)^PaginaEnBlanco: \{', pak)) \
                and 'Reset:' in pak and 'Nada:' in pak:
            ok(f'{loc}: las claves Reset/Nada viven en el paquete')
        else:
            mal(f'{loc}: FALTAN las claves Reset/Nada')
        if 'EsenciaHambre' in pak:
            ok(f'{loc}: la EsenciaHambre vive (regresión .110)')
        else:
            mal(f'{loc}: FALTA la EsenciaHambre (regresión .110)')
        if loc == 'es-MX' and '\t' in pak:
            mal('es-MX: se coló un TAB')
        else:
            ok(f'{loc}: indentación respetada')

    print('=== 6. LA RAWIMG NUEVA — PaginaEnBlanco 24×24 (EL ÍTEM) ===')
    raw = blobs.get('Content/Items/PaginaEnBlanco.rawimg')
    if raw is None:
        mal('FALTA Content/Items/PaginaEnBlanco.rawimg')
    else:
        magic, w, h = struct.unpack('<iii', raw[:12])
        if magic == 1 and (w, h) == (24, 24) and len(raw) == 12 + 24 * 24 * 4:
            ok('PaginaEnBlanco.rawimg 24×24 RGBA (magic 1, 2316 B — el formato de la Esencia)')
        else:
            mal(f'PaginaEnBlanco.rawimg inesperada: magic {magic}, {w}×{h}, {len(raw)} B')
        try:
            from PIL import Image
            png = Image.open(f'{ARBOL}/Content/Items/PaginaEnBlanco.png').convert('RGBA')
            px_raw, px_png = raw[12:], png.tobytes()
            malos = 0
            for p in range(24 * 24):
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
    # las rawimg de regresión
    for ruta, etiqueta in [
        ('Content/Items/EsenciaHambre.rawimg', 'la Esencia de Hambre (regresión .110)'),
        ('Content/Buffs/OjoEternoBuff.rawimg', 'el icono del buff (regresión .111)'),
        ('Content/Projectiles/OjoEterno_Abierto.rawimg', 'el ojo del juego (regresión)'),
    ]:
        if ruta in rutas:
            ok(f'{ruta} sigue en el paquete ({etiqueta})')
        else:
            mal(f'FALTA {ruta} ({etiqueta})')

    print()
    if FALLOS:
        print(f'=== {len(FALLOS)} FALLOS ===')
        for f in FALLOS:
            print(f'  ✗ {f}')
        sys.exit(1)
    print('=== TODO OK — v6.50.112 VERIFICADA ===')


if __name__ == '__main__':
    main()
