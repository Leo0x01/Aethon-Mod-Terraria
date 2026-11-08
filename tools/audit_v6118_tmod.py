#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6118_tmod.py — LA AUDITORÍA DEL .tmod v6.118 — EL CUERPO DEL DIOS
(el rework del MUNDO INTERIOR + el calco del festín + las curas del ala).

LA LETRA (v6.118): «el nuevo mundo no es lo que pedí, primero tiene que
estar en la superficie, luego debe estar en eclipse permanente y debe ser
el de las oleadas de hambre, luego no debe tener circulos planos… el
fondo del mundo debe ser un mundo de tentaculos por capas y oscuridad por
capas, ademas de brumas y particulas» · «la forma eterna no debe volar,
eso es algo que tienen que hacer las alas» · «las alas runicas del vacio
siguen con picos de sierra, investiga por que» · «usa la misma animacion
que el grimorio usa para devorar a los jefes y solo enviar al jugador al
nuevo mundo cuando el jugador entre a la boca del grimorio».

LA CURA v6.118 (lo que esta auditoría verifica):
  · EL MUNDO: terreno CONTINUO de superficie (GenerarInterior nuevo,
    BrumaNoise), las VETAS de LA PÁGINA DEL ETERNO (el tile forjado),
    EL CORAZÓN de amatista con su LATIDO (PulsoCorazon), LAS VENTANAS
    (_vents), EL CICLO ETERNO de oleadas (Provocar + DetenerFestin) y
    EL CERCO del spawn gate abierto para el festín.
  · EL CIELO: CieloInteriorEterno (el CustomSky de las TRES CAPAS de
    tentáculos con la Piel del Eterno) + CieloInteriorSistema (el
    registro + LA BRUMA DEL SOL del eclipse).
  · EL VELO Y EL FARO: BrumaFX de verdad (MistBand/Cloud/Puff — cero
    rects planos), la revelación con AnimatedPuff + NovaBurst, y EL OJO
    COLOSAL MUERTO (el cuestionario: FUERA).
  · LAS PARTÍCULAS: SpawnMotaDorada (ParticleManager — los Dust de
    vanilla MURIERON).
  · LA DEVORACIÓN: EL CALCO DEL FESTÍN (GrimorioHambriento_Medio/
    _Cerrado/_Iris + la cadena SombrasLib + Devorador(Player)) y LA
    PUERTA VIVE EN LA BOCA (RectBoca + Intersects — jamás un timer).
  · LAS ALAS: el soft-add canónico (ColorDestinationBlend = Blend.One
    — la cura de la sierra que dos curas geométricas no podían matar),
    el ensoglado del ruido, las runas un nodo adentro y el balance de
    la vela (0.78/0.42).
  · LA FORMA ETERNA: FormaAscendidaVisual/FormaAscendidaOchoVisual
    (viste SIN volar — VueloDivino solo lee las banderas reales).

  (0) IDENTIDAD: AethonMod v6.118 · tML 2026.9.3.1 · 494 entradas.
  (1) EOF EXACTO: dataSize + tabla + sum(blobs) == fsize.
  (2) TODOS LOS BLOBS INFLAN (494/494).
  (2b) EMPAQUETADO == FUENTE byte a byte (los 3 hjson).
  (3) LAS FIRMAS VIVEN en la DLL (las nuevas + las de siempre).
  (4) LAS TEXTURAS DE LA FORJA: PielDelEterno 128×512 + el tile 54×18.
  (5) LOS TEXTOS NUEVOS + los de siempre en los 3 espejos.
  (6) LOS KEYSETS ×3 IDÉNTICOS.
  (7) LA FUENTE DE LA CURA (las firmas exactas del rework).
  (8) CADENA DE CUSTODIA: repo == ModSources en .cs/.hjson/.png.
"""
import importlib.util
import os
import struct
import sys
import zlib

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
RAIZ = os.path.join(REPO, 'AethonMod')
TMOD = os.path.expanduser('~/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod')
MSRC = os.path.expanduser('~/.local/share/Terraria/tModLoader/ModSources/AethonMod')

spec = importlib.util.spec_from_file_location('parse_tmod', os.path.join(REPO, 'tools', 'parse_tmod.py'))
mp = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mp)

fallos = []


def check(nombre, ok, detalle=''):
    print(f'  [{"OK" if ok else "FALLO"}] {nombre}' + (f' — {detalle}' if detalle else ''))
    if not ok:
        fallos.append(nombre)


def blobs_de(d, tend, entries):
    out, off, malas = {}, tend, 0
    for p, raw, comp in entries:
        trozo = d[off:off + comp]
        if len(trozo) != comp:
            malas += 1
            break
        try:
            data = trozo if comp == raw else zlib.decompress(trozo, -15)
        except zlib.error:
            malas += 1
            break
        if len(data) != raw:
            malas += 1
            break
        out[p] = data
        off += comp
    return out, off, malas


d = open(TMOD, 'rb').read()

# === (0) IDENTIDAD ===
tmlver, name, ver, entries, tend, fsize = mp.parse(TMOD)
print(f'(0) {name} v{ver} · tML {tmlver} · {len(entries)} entradas · {fsize:,} B · archivo {len(d):,} B')
check('la versión es 6.118', ver == '6.118', ver)
check('las entradas son 494 (las 492 de la v6.117 + PielDelEterno.png + PaginaDelEterno_Tile.png)',
      len(entries) == 494, str(len(entries)))

# === (1) EOF + (2) BLOBS ===
bl, off, malas = blobs_de(d, tend, entries)
check('EOF EXACTO', off == len(d), f'{off:,} == {len(d):,}')
check('TODOS LOS BLOBS INFLAN (494/494)', malas == 0 and len(bl) == 494, f'{len(bl)}/494 · malas={malas}')

# === (2b) EMPAQUETADO == FUENTE (los 3 hjson) ===
for lang in ('es-MX', 'es-ES', 'en-US'):
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    src = open(os.path.join(RAIZ, key), 'rb').read()
    check(f'empaquetado == fuente: {key}', key in bl and bl[key] == src)

# === (3) LAS FIRMAS EN LA DLL ===
dll = bl.get('AethonMod.dll', b'')
firmas_vivas = [
    # v6.114–v6.117 — el mundo interior y su gente (LO QUE SIGUE VIVO)
    b'SubmundoGrimorio', b'FormaEternaItem', b'MundoInteriorEternoItem',
    b'DevoracionEterna', b'AltarPrismaTile', b'PrismaInteriorUI',
    b'BrumaFaro', b'PedirSolEclipse', b'ReiniciarMiniMapa',
    b'PintarFaroDevoracion', b'OnPostDraw', b'hideEntirePlayer',
    b'Tragado', b'OnKill', b'ModifyDrawInfo',
    # v6.118 — EL CUERPO DEL DIOS: el mundo, el cielo, la vida
    b'CieloInteriorEterno', b'CieloInteriorSistema', b'PaginaDelEternoTile',
    b'PulsoCorazon', b'SpawnMotaDorada', b'DetenerFestin',
    b'GenerarInterior', b'ModifySunLightColor', b'PostDrawTiles',
    b'BrumaDelSolInterior', b'DibujarTira', b'DibujarOjo',
    # v6.118 — LA DEVORACIÓN DE VERDAD (el calco del festín)
    b'BocaDelLibro', b'RectBoca', b'Intersects', b'CicloMasticar',
    b'DibujarElLibro', b'DevoradorNucleo',
    # v6.118 — LA FORMA ETERNA SIN VUELO + las alas
    b'FormaAscendidaVisual', b'FormaAscendidaOchoVisual', b'VueloDivino',
    b'noFallDmg', b'EmitirMembrana', b'EmitirBordeMaestro',
]
for f in firmas_vivas:
    check(f'DLL VIVA: {f.decode()}', f in dll)

# LOS LITERALES DE RUTA (los párpados y el iris del festín) viven en el
# heap #US como UTF-16LE — no como identificadores UTF-8 (la lección de
# esta auditoría: la casa los pide con ModContent.Request<string>).
for lit in ('GrimorioHambriento_Medio', 'GrimorioHambriento_Cerrado', 'GrimorioHambriento_Iris'):
    check(f'DLL VIVA (literal UTF-16): {lit}', lit.encode('utf-16-le') in dll)

# === (4) LAS TEXTURAS DE LA FORJA (tML las empaqueta como .rawimg:
#      [i32 versión][i32 ancho][i32 alto] LE + RGBA) ===
def rawimg_size(b):
    _v, w, h = struct.unpack('<iii', b[:12])
    return w, h


for tex, w_esperado, h_esperado in (
        ('Content/Effects/PielDelEterno.rawimg', 128, 512),
        ('Content/Tiles/PaginaDelEterno_Tile.rawimg', 54, 18)):
    ok = tex in bl
    det = ''
    if ok:
        w, h = rawimg_size(bl[tex])
        det = f'{w}x{h}'
        ok = (w, h) == (w_esperado, h_esperado)
    check(f'TEXTURA DE LA FORJA: {os.path.basename(tex)} {w_esperado}x{h_esperado}', ok, det)

# LA MÁSCARA DEL PRISMA sigue viva (la puerta de vuelta)
mascara = 'Content/Tiles/AltarPrismaTile_Highlight.rawimg'
ok = mascara in bl
det = ''
if ok:
    w, h = rawimg_size(bl[mascara])
    det = f'{w}x{h}'
    ok = (w, h) == (54, 54)
check('MÁSCARA VIVA: AltarPrismaTile_Highlight.rawimg 54x54', ok, det)

# === (5) LOS TEXTOS VIVOS ===
import re as _re
textos = {
    'es-MX': ['La Página del Eterno', 'el cielo es cosa de las alas', 'La Forma Eterna'],
    'es-ES': ['La Página del Eterno', 'el cielo es cosa de las alas', 'La Forma Eterna'],
    'en-US': ['The Page of the Eternal', 'the sky belongs to the wings', 'The Eternal Form'],
}
for lang, textos_lang in textos.items():
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    contenido = bl.get(key, b'').decode('utf-8')
    for t in textos_lang:
        check(f'texto {lang}: «{t[:40]}»', t in contenido)

# LA PROMESA MUERTA DEL VUELO (la Forma Eterna ya NO canta vuelo infinito)
for lang, muerto in (('es-MX', 'EL VUELO INFINITO'), ('es-ES', 'EL VUELO INFINITO'),
                     ('en-US', 'INFINITE FLIGHT')):
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    contenido = bl.get(key, b'').decode('utf-8')
    # las Formas REALES (2/3/4/8) siguen cantando su vuelo — solo la de la
    # Forma Eterna murió: la clave vive en su bloque
    bloque = contenido.split('FormaEternaItem: {')[1].split('\n\t}')[0] if 'FormaEternaItem: {' in contenido else ''
    check(f'promesa MUERTA {lang}: «{muerto}» fuera del bloque de la Forma Eterna',
          muerto not in bloque)

# === (6) LOS KEYSETS ×3 IDÉNTICOS ===
def keyset(lang):
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    return set(_re.findall(r'^[\t ]*([\w.]+):\s*\{', bl.get(key, b'').decode('utf-8'), _re.M))


k_mx, k_es, k_us = keyset('es-MX'), keyset('es-ES'), keyset('en-US')
check('keysets es-MX == es-ES', k_mx == k_es, f'{len(k_mx)} vs {len(k_es)}')
check('keysets es-MX == en-US', k_mx == k_us, f'{len(k_mx)} vs {len(k_us)}')

# === (7) LA FUENTE DE LA CURA (el diff v6.117→v6.118) ===
f_sub = open(os.path.join(RAIZ, 'Content', 'Systems', 'SubmundoGrimorio.cs'), encoding='utf-8').read()
f_cielo = open(os.path.join(RAIZ, 'Content', 'Effects', 'CieloInteriorEterno.cs'), encoding='utf-8').read()
f_dev = open(os.path.join(RAIZ, 'Content', 'Projectiles', 'Definitivas', 'DevoracionEterna.cs'), encoding='utf-8').read()
f_alas = open(os.path.join(RAIZ, 'Content', 'VFX', 'AlasAuroraLib.cs'), encoding='utf-8').read()
f_estilo = open(os.path.join(RAIZ, 'Content', 'VFX', 'AlasEstilo.cs'), encoding='utf-8').read()
f_prisma = open(os.path.join(RAIZ, 'Content', 'Items', 'Alas', 'AlasPrismaItems.cs'), encoding='utf-8').read()
f_forma = open(os.path.join(RAIZ, 'Content', 'Items', 'Cosmetics', 'FormaEternaItem.cs'), encoding='utf-8').read()
f_sombra = open(os.path.join(RAIZ, 'Content', 'VFX', 'SombrasLib.cs'), encoding='utf-8').read()
f_furia = open(os.path.join(RAIZ, 'Content', 'Systems', 'GrimorioFuriaSistema.cs'), encoding='utf-8').read()

# EL MUNDO
check('FUENTE: LA SUPERFICIE CONTINUA (PlazaY=120, Superficie=150, Roca=270)',
      'private const int PlazaY = 120;' in f_sub and 'private const int Superficie = 150;' in f_sub
      and 'private const int Roca = 270;' in f_sub)
check('FUENTE: la LEY DE LAS SECCIONES intacta (800×450)',
      'private const int Ancho = 800;' in f_sub and 'private const int Alto = 450;' in f_sub)
check('FUENTE: el terreno usa BrumaNoise de la casa',
      'BrumaNoise.WarpedFbm' in f_sub)
check('FUENTE: LAS VETAS del tile forjado + EL CORAZÓN + LAS VENTANAS',
      'PaginaDelEternoTile' in f_sub and '_vents' in f_sub and 'EL CORAZÓN' in f_sub)
check('FUENTE: EL CICLO ETERNO de oleadas (Provocar dentro)',
      'GrimorioFuriaSistema.Provocar' in f_sub and 'CalmaTicks' in f_sub)
check('FUENTE: EL LATIDO del corazón (PulsoCorazon)',
      'public static float PulsoCorazon;' in f_sub and 'OndaLib.Kick(2)' in f_sub)
check('FUENTE: el spawn gate ABIERTO para el festín',
      'SubmundoGrimorio.Activo && !GrimorioFuriaSistema.Activo' in f_sub)
check('FUENTE: EL OJO COLOSAL MUERTO (DibujarElOjo ausente)',
      'DibujarElOjo' not in f_sub)
check('FUENTE: el velo con BrumaFX (cero rects de banda plana)',
      'BrumaFX.Puff' in f_sub and 'BrumaFX.Cloud' in f_sub and 'BrumaFX.AnimatedPuff' in f_sub)
check('FUENTE: la revelación con el NovaBurst (el iris sancionado)',
      'VFXCore.NovaBurst' in f_sub)
check('FUENTE: las partículas de casa (ParticleManager, cero DustID en el interior)',
      'SpawnMotaDorada' in f_sub and 'Dust.NewDustPerfect' not in f_sub)
check('FUENTE: el tinte CEDE al eclipse de la oleada',
      '_tinteCedido' in f_sub and 'AmbienteOleadaSistema.FestinVisible' in f_sub)

# EL CIELO
check('FUENTE: el CustomSky con las TRES CAPAS + la Piel del Eterno',
      'CieloInteriorEterno : CustomSky' in f_cielo and 'PielDelEterno' in f_cielo
      and 'SemillaLejos' in f_cielo and 'SemillaMedio' in f_cielo and 'SemillaCerca' in f_cielo)
check('FUENTE: los ojos del cielo MIRAN al portador (OjoAbismo)',
      'OjoAbismo' in f_cielo and 'DibujarOjo' in f_cielo)
check('FUENTE: la bruma del sol del eclipse (la posición EXACTA de vanilla)',
      'DrawSunAndMoon' in f_cielo and 'BrumaDelSolInterior' in f_cielo)
check('FUENTE: el cielo respira con el latido',
      'SubmundoGrimorio.PulsoCorazon' in f_cielo)
check('FUENTE: el cielo NO se cuela bajo la roca',
      'Main.rockLayer' in f_cielo)

# LA DEVORACIÓN
check('FUENTE: LAS DOS TAPAS MURIERON (srcArriba/srcAbajo/angMand ausentes)',
      'srcArriba' not in f_dev and 'srcAbajo' not in f_dev and 'angMand' not in f_dev)
check('FUENTE: EL CALCO DEL FESTÍN (párpado Medio/Cerrado + iris)',
      '_texMedio' in f_dev and '_texCerrado' in f_dev and '_texIris' in f_dev
      and 'CicloMasticar' in f_dev)
check('FUENTE: la cadena SombrasLib del festín',
      'SombrasLib.ColumnaViva' in f_dev and 'SombrasLib.Devorador' in f_dev
      and 'SombrasLib.OjosDeMasa' in f_dev and 'SombrasLib.CabezaDeBruma' in f_dev)
check('FUENTE: LA PUERTA VIVE EN LA BOCA (colisión, jamás timer)',
      'rectBoca.Intersects(p.Hitbox)' in f_dev and 'TEntrar' not in f_dev)
check('FUENTE: LA RED DE SEGURIDAD viva (OnKill libera)',
      'public override void OnKill(int timeLeft)' in f_dev and 'Tragado = false;' in f_dev)
check('FUENTE: la sobrecarga Devorador(Player) de SombrasLib',
      'Devorador(Vector2 punta, Player presa' in f_sombra and 'DevoradorNucleo' in f_sombra)

# LAS ALAS
check('FUENTE: EL SOFT-ADD CANÓNICO (Dest=One — la cura de la sierra)',
      'ColorDestinationBlend = Blend.One' in f_alas and '(Blend)0' not in f_alas)
check('FUENTE: el ruido ENSOGLADO (la membrana manda en el filo)',
      'float inset = estilo.Membrana > 0f' in f_alas)
check('FUENTE: las runas UN NODO ADENTRO en la membrana',
      'int nodoRuna = estilo.Membrana > 0f ? 7 : 8;' in f_alas)
check('FUENTE: EL BALANCE DE LA VELA (nucleo 0.78 · cuerpo 0.42)',
      '0.78f, 0.3f, 0.42f' in f_estilo)
check('FUENTE: la inmunidad de caída reforzada en el tick del accesorio',
      'player.noFallDmg = true;' in f_prisma)

# LA FORMA ETERNA
check('FUENTE: la Forma Eterna enciende SOLO-VISUAL (las banderas nuevas)',
      'FormaAscendidaVisual = true' in f_forma and 'FormaAscendidaOchoVisual = true' in f_forma)
check('FUENTE: VueloDivino SOLO lee las banderas reales',
      'FormaAscendida || FormaAscendidaDos || FormaAscendidaTres ||' in
      open(os.path.join(RAIZ, 'Content', 'Players', 'CosmeticPlayer.cs'), encoding='utf-8').read())

# EL FESTÍN
check('FUENTE: DetenerFestin público (la puerta del interior)',
      'public static void DetenerFestin()' in f_furia)

# === (8) CADENA DE CUSTODIA: repo == ModSources ===
if os.path.isdir(MSRC):
    drift = []
    for raiz_dir, _, archivos in os.walk(RAIZ):
        for a in archivos:
            if not a.endswith(('.cs', '.hjson', '.png', '.txt')):
                continue
            rel = os.path.relpath(os.path.join(raiz_dir, a), RAIZ)
            destino = os.path.join(MSRC, rel)
            if not os.path.exists(destino):
                drift.append(rel)
                continue
            if open(os.path.join(raiz_dir, a), 'rb').read() != open(destino, 'rb').read():
                drift.append(rel)
    check('CADENA DE CUSTODIA repo == ModSources', len(drift) == 0, f'{len(drift)} drift: {drift[:4]}')
else:
    print('  [SALTO] CADENA DE CUSTODIA: ModSources no existe en este sandbox (la copia del playtest no corrió)')

print()
if fallos:
    print(f'*** AUDIT v6.118: {len(fallos)} FALLOS ***')
    sys.exit(1)
print('*** AUDIT v6.118 TODO VERIFICADO ***')
