#!/usr/bin/env python3
"""
v6.50.113 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO (3 pedidos):
(1) «si el libro se alimenta por si solo, cada muerte que el libro
    haga por si mismo debe reducir 1 de hambre, actualmente solo lo
    hace en 0%» → LA SOMBRA COME PLANO: ReduccionHambre (el camino del
    festín autóctono — AbsorberPresa, la sombra de la página devorando
    ENTERA) devuelve 0.01f PLANO (1%) para el Eterno y el Nervioso por
    igual; la dieta por rareza de la .104 (1★=0,1% — la chusma común
    no movía el medidor) muere en ESTE camino pero SIGUE VIVA en la
    baja acreditada del jugador (GlobalNPCXP.OnKill → HambrePorPresa).
(2) «tambien debes permitir que visualizar el medidor de hambre tenga
    decimales» → EL MEDIDOR CON UN DECIMAL: la barra del tooltip
    (GrimorioHambriento) y la voz de la Esencia (EsenciaHambre)
    formatean "0.0" (47,3%) — el (int) truncador murió.
(3) «el icono que debes usar para el buff cuando sale el minion es
    este… / para el icono del mod o sea lo que se ve cuando instala el
    mod es este… / ahora para el icono de la mascota de luz es este…»
    → LOS TRES ICONOS DEL USUARIO (SPRITE_EXACTO 2048×2048 decodificado,
    VLM eligió el filtro): OjoEternoBuff.png 32×32 (NEAREST — el buff
    del minion), AethonMenorBuff.png 32×32 (NEAREST — el buff de la
    mascota), icon.png 80×80 (LANCZOS — el menú de Mods) y su hermano
    pequeño icon_small.png 30×30 (NEAREST — vistas compactas; el viejo
    desde la .13 mostraba el arte viejo). Decompile verificado:
    ContentConverters EXIME icon.png del rawimg (viaja PNG crudo) y
    GetModIcon exige 80×80/30×30 EXACTOS; el stream-position de
    AddResource hace que icon_small.png SIEMPRE viaje PNG crudo.

CHECKS: cabecera/entradas/EOF (390 — mismos nombres que la .112: los
iconos son REEMPLAZOS) · heap + ldstr + constantes .104–.112 · EL
ÁRBOL (el plano del autóctono + el decimal + los iconos + regresión
.110–.112 ÍNTEGRA) · hjson ×3 empaquetado == fuente (SIN cambios esta
versión) · icon.png/icon_small.png PNG crudos byte a byte + las dos
rawimg de buff píxel-idénticas.
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
    # === la regresión .105–.112 (los ojos, la cena, la promesa y el perezoso) ===
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
]

MUERTOS_HEAP = [
    'GrimorioSellado', 'TintaViva', 'GrimorioEstrellado', 'EcoEmperatriz',
    'VeredictoEmperatriz', 'EstelaAethonProjectile', 'TrazoEmperatrizProjectile',
    'FragmentoAethon', 'ExplosionAethon', 'SistemaObservado',
    'CosmicOrbMinion', 'CosmicOrbBuff', 'CosmicOrbBolt',
    'DrawElRayo', 'CanalPerlin', 'get_Cadencia',
]

VIVOS_US = [
    # === la regresión .110–.112 ===
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
    if ver == '6.50.113' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    # .113 == .112 en NOMBRES (390): los cuatro .png (icon.png,
    # icon_small.png y los dos buffs → rawimg) son REEMPLAZOS, no altas
    if len(entries) == 390:
        ok('390 entradas (los mismos nombres de la .112: los iconos son REEMPLAZOS)')
    else:
        mal(f'se esperaban 390 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. EL HEAP — REGRESIÓN .104–.112 ===')
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

    print('=== 3. LAS CONSTANTES (float32) — REGRESIÓN .104–.112 ===')
    def en_dll(b):
        return b in dll
    constantes = [
        (f32(0.01), '0.01f — el PLANO del autóctono (.113) + la pendiente del tope (.112)'),
        (f32(0.009), '0.009f — el tirón del perezoso (regresión .112)'),
        (f32(0.88), '0.88f — el freno nuevo (regresión .112)'),
        (f32(10.0), '10f — el tope CERCA del dueño (regresión .112)'),
        (f32(20.0), '20f — el tope LEJOS (regresión .112)'),
        (f32(0.32), '0.32f — el empuje del planeo de vuelta (regresión .112)'),
        (f32(12.0), '12f — el tope del planeo de vuelta (regresión .112)'),
        (f32(430.0), '430f — T_MUERTE: el largo del cine (regresión .110)'),
        (f32(175.0), '175f — T_FESTIN: la sombra sale del grimorio (.110)'),
        (f32(-90.0), '−90f — LA ELEVACIÓN (regresión .110)'),
        (f32(0.05), '0.05f — HambrePorUso: +5% por uso (regresión .110)'),
        (f32(0.375), '0.375f — ESCALA: EL OJO A LA MITAD (regresión .109)'),
        (f32(0.42), '0.42f — ω par: GIRO CONSTANTE (regresión .109)'),
        (f32(1.0 / 216000.0), '1f/216000f — EL RELOJ DE LA HORA (regresión .103)'),
        (f32(0.10), '0.10f — el jefe vale 10% (la dieta VIVA en OnKill, regresión .104)'),
        (f32(2.55), '2.55f — el aro del círculo mágico (regresión .106)'),
        (f32(0.5), '0.5f — EL 50% de la velocidad de ataque (regresión .107)'),
    ]
    for bytes_, etiqueta in constantes:
        if en_dll(bytes_):
            ok(f'{etiqueta} vive en el IL')
        else:
            mal(f'FALTA en la DLL: {etiqueta}')

    print('=== 4. EL ÁRBOL — EL PLANO + EL DECIMAL + LOS ICONOS + REGRESIÓN ===')
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
    for patron, etiqueta, src in [
        # === LA .113 — EL PLANO DEL AUTÓCTONO ===
        ('internal static float ReduccionHambre(NPC presa, int tipoLibro)',
         'ReduccionHambre vive (el camino de la sombra)', nerv),
        ('return 0.01f;', 'EL PLANO: 1% fijo por presa devorada por sí mismo', nerv),
        ('dieta POR RAREZA sigue viva en el otro camino',
         'la dieta de la .104 SIGUE en la baja acreditada (el comentario lo jura)', nerv),
        ('float quita = Weapons.GrimoireEternal.HambrePorPresa(npc);',
         'GlobalNPCXP.OnKill SIGUE con la dieta por rareza (la .104 no se toca)', xp),
        # === LA .113 — EL MEDIDOR CON DECIMALES ===
        ('{(h * 100f).ToString("0.0")}', 'LA BARRA: un decimal SIEMPRE (47,3%)', base),
        ('(et.Hambre * 100f).ToString("0.0")', 'LA ESENCIA: la voz con decimal', esencia),
        # === LA .113 — LOS ICONOS (presencia en el paquete va en 6/7) ===
        # === LA REGLA DE LA .110/.111/.112 EN LOS HOOKS (regresión) ===
        ('float slotsEnUso = 0f;', 'LA SUMA: lo que vanilla va a contar', sp),
        ('if (p.type == tipoOjo) hayOjos = true;', 'el detector de ojos vivos', sp),
        ('if (nivel > 0 || hayOjos)', 'la ley: libro a mano O ojos en el aire', sp),
        ('int techoVivo = (int)Math.Ceiling(slotsEnUso);', 'EL TECHO VIVO (.111)', sp),
        ('Player.maxMinions = techoVivo;', 'la promesa se cumple (.111)', sp),
        ('public const byte ESTADO_PRIMERA_CENA = 3;', 'EL ESTADO 3 (la primera cena)', fauces),
        ('public const int VidaUmbralPrimeraCena = 100;', 'EL UMBRAL DE 100 DE VIDA', fauces),
        ('dueñoPool.life <= VidaUmbralPrimeraCena', 'EL GATILLO: el jefe llegó a 100', fauces),
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

    # === LOS MUERTOS DEL ÁRBOL (la .113 + regresión .112) ===
    if 'HambrePorPresa(presa)' in nerv:
        mal('ReduccionHambre: la dieta por rareza VIVE en la sombra (debe ser PLANA)')
    else:
        ok('la sombra YA NO llama a HambrePorPresa (el plano del autóctono)')
    if 'return GrimoireEternal.HambrePorPresa(presa);' in nerv:
        mal('el return de la dieta VIVE en ReduccionHambre')
    else:
        ok('el return de la dieta MUERTO en ReduccionHambre')
    if '(int)(h * 100f)' in base:
        mal('LA BARRA: el (int) truncador VIVE')
    else:
        ok('LA BARRA: el (int) truncador MUERTO')
    if '(int)(et.Hambre * 100f)' in esencia:
        mal('LA ESENCIA: el (int) truncador VIVE')
    else:
        ok('LA ESENCIA: el (int) truncador MUERTO')
    if 'Projectile.velocity += (destino - Projectile.Center) * 0.02f;' in esp:
        mal('el TIRÓN VIEJO (0.02) VIVE — el misil no murió')
    else:
        ok('el tirón viejo 0.02 MUERTO (regresión .112)')
    if 'Projectile.velocity *= 0.90f;' in esp:
        mal('el FRENO VIEJO (0.90 en velocity) VIVE')
    else:
        ok('el freno viejo 0.90 (en velocity) MUERTO (regresión .112)')

    print('=== 5. hjson ×3 — SIN CAMBIOS (.113 no toca textos) ===')
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

    print('=== 6. LOS ICONOS DEL USUARIO — PNG CRUDOS BYTE A BYTE ===')
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
        from PIL import Image
        import io as _io
        im = Image.open(_io.BytesIO(data))
        if im.size == (tam, tam):
            ok(f'{entrada}: {tam}×{tam} EXACTOS (GetModIcon)')
        else:
            mal(f'{entrada}: {im.size} — se esperaba {tam}×{tam}')

    print('=== 7. LAS RAWIMG DE BUFF — LOS DOS ICONOS NUEVOS (píxel a píxel) ===')
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
                ok(f'{entrada}: píxel-idéntica al .png del árbol (EL ARTE NUEVO del usuario)')
            else:
                mal(f'{entrada}: DIFIERE del .png en {malos} píxeles')
        except ImportError:
            ok('(sin PIL: salto la comparación píxel a píxel)')
    # las rawimg de regresión
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
        print(f'=== {len(FALLOS)} FALLOS ===')
        for f in FALLOS:
            print(f'  ✗ {f}')
        sys.exit(1)
    print('=== TODO OK — v6.50.113 VERIFICADA ===')


if __name__ == '__main__':
    main()
