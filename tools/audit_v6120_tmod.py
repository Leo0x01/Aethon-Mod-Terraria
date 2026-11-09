#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6120_tmod.py — LA AUDITORÍA DEL .tmod v6.120 — EL LLANO DE
OBSIDIANA + EL PILAR DE LA SALIDA + LAS TRES BANDAS.

LA LETRA (v6.120): «una vez dentro del mundo interior no puedo salir,
necesitas crear un pilar tipo prisma altar que permita salir… el pilar
debe aparecer ya colocado en el mundo interior, y el mundo interior debe
ser plano, solo un mundo plano de obsidiana, el fondo deben ser capas
de tentaculos en loop y al menos 3 capas» · «crea una banda sonora
exclusiva para el mundo interior, crea 3 banda sonoras que sonaran
aleatoriamente» · «la forma eterna debe poder activarse y ocultarse con
las opciones del juego» · «las esencias del eterno solo llegan hasta
pilas de 999… hacer que puedan acumularse hasta 9999» · «vuelve a copiar
el codigo de las alas runicas del eclipse para reparar las alas runicas
del vacio» · «asegurarte de que cuando se cumpla esto, solo se active
siempre que se consagran 1000 esencias».

LO QUE ESTA AUDITORÍA VERIFICA (la cura y la obra de la v6.120):
  · EL PILAR PRISMA: PilarPrismaTile (la salida «tipo prisma altar») +
    su tira 54×516 + su MÁSCARA 54×86 (la convención HasOutlines).
  · EL LLANO: GenerarInterior NUEVO (solo TileID.Obsidian, sin colinas
    ni geoda ni vetas) y EL PILAR COLOCADO DESPUÉS DEL TERRENO (la cura
    del «no puedo salir»: el ancla de Style2xX valida sólido debajo).
  · EL LOOP DEL CIELO: CieloInteriorEterno con EL MÓDULO DEL LOOP (Mod)
    y las TRES capas periódicas (900/760/640 px).
  · LAS TRES BANDAS: MundoSuenoVacio.ogg · MundoGarganta.ogg ·
    MundoCorazon.ogg (los 3 en el .tmod) y EscenaInteriorGrimorio con
    la moneda (AlEntrar + el re-tiro de ~2,5 min sin repetir).
  · LA FORMA ETERNA A LA VISTA: AethonConfig.MostrarFormaEterna +
    FormaEternaVisible + el respeto del OJO de vanilla
    (hideVisibleAccessory) en el escaneo de CosmeticPlayer.
  · LAS ESENCIAS 9999: ItemCorrupto.maxStack == 9999 (y el drop y la
    cuenta del altar INTACTOS — la puerta SOLO a las 1000 consagradas).
  · LAS ALAS DEL VACÍO: LA COPIA EXACTA DEL ECLIPSE (el Ala() del vacío
    con los parámetros del eclipse — membrana 0, redonda 0, ruido 14,
    puntas 19/12, afilada 0.55, filosa 0.05, núcleo 0.88, cuerpo 0.36).

  (0) IDENTIDAD: AethonMod v6.120 · 499 entradas (494 + pilar ×2 + 3 ogg).
  (1) EOF EXACTO: dataSize + tabla + sum(blobs) == fsize.
  (2) TODOS LOS BLOBS INFLAN (499/499).
  (2b) EMPAQUETADO == FUENTE byte a byte (los 3 hjson).
  (3) LAS FIRMAS VIVEN en la DLL (las nuevas + las de siempre).
  (4) LAS TEXTURAS DEL PILAR (la tira y su máscara, medidas exactas).
  (5) LAS TRES BANDAS (ogg presentes y vorbis).
  (6) LOS TEXTOS NUEVOS + los keysets ×3 idénticos.
  (7) LA FUENTE DE LA CURA (las firmas exactas del rework).
"""
import importlib.util
import os
import re
import struct
import sys
import zlib

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
RAIZ = os.path.join(REPO, 'AethonMod')
TMOD = os.path.expanduser('~/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod')

_falla = []


def check(nombre, ok, det=''):
    print(('  OK ' if ok else '  FALLA ') + nombre + (f' [{det}]' if det and not ok else (f' ({det})' if det else '')))
    if not ok:
        _falla.append(nombre)


mp_spec = importlib.util.spec_from_file_location('parse_tmod', os.path.join(REPO, 'tools', 'parse_tmod.py'))
mp = importlib.util.module_from_spec(mp_spec)
mp_spec.loader.exec_module(mp)


def blobs_de(data, start, entries):
    # (la casa: parse_tmod entrega (path, raw, comp) — el tamaño COMPRIMIDO
    # es el que camina el archivo, el RAW es el inflado esperado)
    out, off, malas = {}, start, 0
    for path, raw, comp in entries:
        trozo = data[off:off + comp]
        if len(trozo) != comp:
            malas += 1
            break
        try:
            blob = trozo if comp == raw else zlib.decompress(trozo, -15)
        except zlib.error:
            malas += 1
            break
        if len(blob) != raw:
            malas += 1
            break
        out[path] = blob
        off += comp
    return out, off, malas


d = open(TMOD, 'rb').read()

# === (0) IDENTIDAD ===
tmlver, name, ver, entries, tend, fsize = mp.parse(TMOD)
print(f'(0) {name} v{ver} · tML {tmlver} · {len(entries)} entradas · {fsize:,} B · archivo {len(d):,} B')
check('la versión es 6.120', ver == '6.120', ver)
check('las entradas son 499 (las 494 de la v6.119 + PilarPrismaTile.png + '
      'PilarPrismaTile_Highlight.png + las 3 bandas)',
      len(entries) == 499, str(len(entries)))

# === (1) EOF + (2) BLOBS ===
bl, off, malas = blobs_de(d, tend, entries)
check('EOF EXACTO', off == len(d), f'{off:,} == {len(d):,}')
check('TODOS LOS BLOBS INFLAN (499/499)', malas == 0 and len(bl) == 499, f'{len(bl)}/499 · malas={malas}')

# === (2b) EMPAQUETADO == FUENTE (los 3 hjson) ===
for lang in ('es-MX', 'es-ES', 'en-US'):
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    src = open(os.path.join(RAIZ, key), 'rb').read()
    check(f'empaquetado == fuente: {key}', key in bl and bl[key] == src)

# === (3) LAS FIRMAS EN LA DLL ===
dll = bl.get('AethonMod.dll', b'')
firmas_vivas = [
    # v6.115–v6.119 — las puertas y la devoración (LO QUE SIGUE VIVO)
    b'SubmundoGrimorio', b'MundoInteriorEternoItem', b'DevoracionEterna',
    b'AltarPrismaTile', b'PrismaInteriorUI', b'EsenciasConsagradas',
    b'EsenciasPuerta', b'PuertaAbierta', b'PuedeEntrar',
    b'BrumaFaro', b'Revelado', b'PintarFaroDevoracion',
    b'ReiniciarMiniMapa', b'CerrarLoteSiAbierto', b'PostDrawTiles',
    b'noFallDmg',
    # v6.120 — EL PILAR DE LA SALIDA + EL LLANO
    b'PilarPrismaTile', b'LlanoY', b'SpawnX', b'GenerarInterior',
    # v6.120 — EL LOOP DEL CIELO
    b'CieloInteriorEterno', b'DibujarTira', b'DibujarOjo', b'PulsoCorazon',
    # v6.120 — LAS TRES BANDAS
    b'EscenaInteriorGrimorio', b'AlEntrar', b'GetMusicSlot',
    # v6.120 — LA FORMA ETERNA A LA VISTA
    b'MostrarFormaEterna', b'FormaEternaVisible', b'hideVisibleAccessory',
    b'FormaEternaItem',
    # v6.120 — LAS ALAS (la copia del eclipse)
    b'AlasCatalogo', b'AlasRunicasDelVacio', b'AlasRunicasDelEclipse',
    b'EmitirMembrana', b'DibujarRunas', b'RunasVacio',
]
for f in firmas_vivas:
    check(f'DLL VIVA: {f.decode()}', f in dll)

# LOS LITERALES (las claves de tag y las rutas del MusicLoader viven en
# el heap #US como UTF-16LE — la lección de la casa)
for lit in ('Music/MundoSuenoVacio', 'Music/MundoGarganta', 'Music/MundoCorazon',
            'esenciasConsagradasAltar', 'Mods.AethonMod.Submundo.PuertaAbiertaAnuncio'):
    check(f'DLL VIVA (literal UTF-16): {lit}', lit.encode('utf-16-le') in dll)

# === (4) LAS TEXTURAS DEL PILAR (la forja de la casa) ===
def rawimg_size(b):
    _v, w, h = struct.unpack('<iii', b[:12])
    return w, h


for tex, w_e, h_e in (
        ('Content/Tiles/PilarPrismaTile.rawimg', 54, 516),
        ('Content/Tiles/PilarPrismaTile_Highlight.rawimg', 54, 86)):
    ok = tex in bl
    det = ''
    if ok:
        w, h = rawimg_size(bl[tex])
        det = f'{w}x{h}'
        ok = (w, h) == (w_e, h_e)
    check(f'TEXTURA DEL PILAR: {os.path.basename(tex)} {w_e}x{h_e}', ok, det)

# LA MÁSCARA DEL PRISMA VIEJO sigue viva (la ventana de dentro)
mascara = 'Content/Tiles/AltarPrismaTile_Highlight.rawimg'
ok = mascara in bl
det = ''
if ok:
    w, h = rawimg_size(bl[mascara])
    det = f'{w}x{h}'
    ok = (w, h) == (54, 54)
check('MÁSCARA VIVA: AltarPrismaTile_Highlight.rawimg 54x54', ok, det)

# === (5) LAS TRES BANDAS (ogg vorbis, presentes y gordas) ===
for ogg in ('Music/MundoSuenoVacio.ogg', 'Music/MundoGarganta.ogg', 'Music/MundoCorazon.ogg'):
    ok = ogg in bl
    det = ''
    if ok:
        b_ogg = bl[ogg]
        det = f'{len(b_ogg):,} B'
        ok = b_ogg[:4] == b'OggS' and len(b_ogg) > 900_000
    check(f'BANDA VIVA: {os.path.basename(ogg)}', ok, det)

# === (6) LOS TEXTOS NUEVOS ===
textos = {
    'es-MX': ['El Pilar Prisma', 'Mostrar la Forma Eterna', 'la salida del llano',
              'LA COPIA EXACTA DEL ECLIPSE'],
    'es-ES': ['El Pilar Prisma', 'Mostrar la Forma Eterna', 'la salida del llano',
              'LA COPIA EXACTA DEL ECLIPSE'],
    'en-US': ['The Prism Pillar', 'Show the Eternal Form', "the plain's way out",
              'THE EXACT COPY OF THE ECLIPSE'],
}
for lang, textos_lang in textos.items():
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    contenido = bl.get(key, b'').decode('utf-8')
    for t in textos_lang:
        check(f'texto {lang}: «{t[:44]}»', t in contenido)

# LAS CLAVES NUEVAS ×3
for clave in ('PilarPrismaTile.MapEntry', 'NombrePilar', 'MostrarFormaEterna'):
    for lang in ('es-MX', 'es-ES', 'en-US'):
        key = f'Localization/{lang}_Mods.AethonMod.hjson'
        contenido = bl.get(key, b'').decode('utf-8')
        check(f'clave {clave} en {lang}', re.search(rf'^[\t ]*{re.escape(clave)}:', contenido, re.M) is not None)

# LOS KEYSETS ×3 IDÉNTICOS
def keyset(lang):
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    return set(re.findall(r'^[\t ]*([\w.]+):\s*\{', bl.get(key, b'').decode('utf-8'), re.M))


k_mx, k_es, k_us = keyset('es-MX'), keyset('es-ES'), keyset('en-US')
check('keysets es-MX == es-ES', k_mx == k_es, f'{len(k_mx)} vs {len(k_es)}')
check('keysets es-MX == en-US', k_mx == k_us, f'{len(k_mx)} vs {len(k_us)}')

# === (7) LA FUENTE DE LA CURA (las firmas exactas del rework) ===
f_sub = open(os.path.join(RAIZ, 'Content', 'Systems', 'SubmundoGrimorio.cs'), encoding='utf-8').read()
f_cielo = open(os.path.join(RAIZ, 'Content', 'Effects', 'CieloInteriorEterno.cs'), encoding='utf-8').read()
f_alas = open(os.path.join(RAIZ, 'Content', 'VFX', 'AlasEstilo.cs'), encoding='utf-8').read()
f_item = open(os.path.join(RAIZ, 'Content', 'Items', 'Altar', 'ItemCorrupto.cs'), encoding='utf-8').read()
f_forma = open(os.path.join(RAIZ, 'Content', 'Items', 'Cosmetics', 'FormaEternaItem.cs'), encoding='utf-8').read()
f_cfg = open(os.path.join(RAIZ, 'Content', 'AethonConfig.cs'), encoding='utf-8').read()
f_pilar = open(os.path.join(RAIZ, 'Content', 'Tiles', 'PilarPrismaTile.cs'), encoding='utf-8').read()
f_cos = open(os.path.join(RAIZ, 'Content', 'Players', 'CosmeticPlayer.cs'), encoding='utf-8').read()
f_drops = open(os.path.join(RAIZ, 'Content', 'Globals', 'EsenciaCorruptaDrops.cs'), encoding='utf-8').read()

# EL LLANO: SOLO obsidiana, y EL PILAR DESPUÉS del terreno (la cura del ancla)
check('FUENTE: EL LLANO ES SOLO OBSIDIANA (TileID.Obsidian, sin Amatista/GoldBrick en GenerarInterior)',
      'TileID.Obsidian' in f_sub and 'AmethystGemspark' not in f_sub.split('GenerarInterior')[1].split('LO QUE VIVE')[0].split('private static void')[0] + f_sub.split('private static void GenerarInterior')[1].split('// (2)')[0] and 'GoldBrick' not in f_sub.split('private static void GenerarInterior')[1].split('// (2)')[0])
check('FUENTE: EL PILAR SE COLOCA DESPUÉS DEL LLANO (PlaceObject tras el for del terreno)',
      f_sub.split('private static void GenerarInterior')[1].find('WorldGen.PlaceObject') >
      f_sub.split('private static void GenerarInterior')[1].find('TileID.Obsidian'))
check('FUENTE: EL PILAR ES PilarPrismaTile (la salida nueva)',
      'PilarPrismaTile' in f_sub.split('private static void GenerarInterior')[1])
check('FUENTE: LA GEODA Y LAS VETAS MURIERON (PaginaDelEternoTile ya no se genera)',
      'PaginaDelEternoTile' not in f_sub.split('private static void GenerarInterior')[1])
check('FUENTE: LA CUENTA DEL ALTAR VIVA (EsenciasConsagradas += coste SOLO en la consagración)',
      'EsenciasConsagradas += coste' in open(os.path.join(RAIZ, 'Content', 'UI', 'AltarOfrendasUI.cs'), encoding='utf-8').read())
check('FUENTE: LA PUERTA SOLO A LAS MIL (EsenciasPuerta = 1000, PuertaAbierta >=)',
      'EsenciasPuerta = 1000' in f_sub and 'EsenciasConsagradas >= EsenciasPuerta' in f_sub)
check('FUENTE: EL DROP INTACTO (0,1 % criaturas · jefes 90/5/1 — la mesa de la v6.93)',
      'Main.rand.Next(1000) == 0' in f_drops and 'dado < 900' in f_drops and 'dado < 960' in f_drops)

# EL LOOP DEL CIELO
check('FUENTE: EL MÓDULO DEL LOOP (Mod) en CieloInteriorEterno',
      'private static float Mod(' in f_cielo and 'Mod(camX' in f_cielo)
check('FUENTE: LAS TRES CAPAS EN LOOP (perLejos 900 · perMedio 760 · perCerca 640)',
      'perLejos = 900f' in f_cielo and 'perMedio = 760f' in f_cielo and 'perCerca = 640f' in f_cielo)

# LAS ALAS: LA COPIA EXACTA DEL ECLIPSE
def ala_de(txt, nombre):
    # la llamada ENTERA: desde su Al@("Nombre" hasta la siguiente Ala("
    # (el repertorio es una sola expresión larga — se corta por la SIGUIENTE
    # llamada o por el final del catálogo)
    marca = f'Ala("{nombre}"'
    i = txt.find(marca)
    if i < 0: return ''
    j = txt.find('Ala("', i + 5)
    if j < 0: j = txt.find('};', i)
    return txt[i:j if j > 0 else len(txt)]


ala_vacio = ala_de(f_alas, 'Runas del Vacío')
ala_eclipse = ala_de(f_alas, 'Runas del Eclipse')
check('FUENTE: LAS ALAS DEL VACÍO Y DEL ECLIPSE EXISTEN', ala_vacio != '' and ala_eclipse != '')
# los parámetros estructurales IDÉNTICOS (todo menos paleta, ColorDia y runas):
# desde la textura de ruido hasta el cierre — sin la paleta ni el anillo
def params(ala):
    i = ala.find('"Ruido_')
    cuerpo = ala[i:] if i >= 0 else ''
    # se corta en la runa (lo ÚLTIMO propio: 2 el vacío · 3 el eclipse)
    m = re.search(r', [23], 1f\)', cuerpo)
    return cuerpo[:m.start()] if m else cuerpo
check('FUENTE: EL REPERTORIO ESTRUCTURAL DEL VACÍO == ECLIPSE (ruido, plumas, puntas, tempo…)',
      params(ala_vacio) == params(ala_eclipse),
      f'{params(ala_vacio)[:60]} vs {params(ala_eclipse)[:60]}')
check('FUENTE: EL VACÍO USA EL RUIDO DEL ECLIPSE (Ruido_14)',
      '"Ruido_14"' in ala_vacio and '"Ruido_6"' not in ala_vacio)
check('FUENTE: LA MEMBRANA MURIÓ EN EL VACÍO (la llamada cierra en ", 2, 1f)" — sin membrana ni redonda)',
      re.search(r', 2, 1f\)\s*,?\s*$', ala_vacio.rstrip()) is not None or ala_vacio.rstrip().endswith('2, 1f),'))
check('FUENTE: EL ANILLO VIOLETA ES DEL VACÍO (Runas=2)', ', 2, 1f' in ala_vacio)
check('FUENTE: EL ECLIPSE CONSERVA SU ANILLO (Runas=3)', ', 3, 1f' in ala_eclipse)

# LAS ESENCIAS
check('FUENTE: LAS ESENCIAS APILAN A 9999', 'Item.maxStack = 9999' in f_item)
check('FUENTE: EL TOPE DEL FESTÍN INTACTO (EsenciasTope — sin cambio)',
      'EsenciasTope' in open(os.path.join(RAIZ, 'Content', 'Players', 'ShardPlayer.cs'), encoding='utf-8').read())

# LA FORMA ETERNA A LA VISTA
check('FUENTE: LA OPCIÓN DEL JUEGO (MostrarFormaEterna en AethonConfig)',
      'MostrarFormaEterna' in f_cfg and 'FormaEternaVisible' in f_cfg)
check('FUENTE: EL OJO DE VANILLA RESPETADO EN EL ÍTEM (hideVisual)',
      'hideVisual' in f_forma.split('UpdateAccessory')[1].split('public override void UpdateVanity')[0])
check('FUENTE: EL ESCANEO RESPETA LA OPCIÓN Y EL OJO (hideVisibleAccessory en CosmeticPlayer)',
      'hideVisibleAccessory' in f_cos and 'FormaEternaVisible()' in f_cos)

# EL PILAR
check('FUENTE: EL PILAR ES «TIPO PRISMA ALTAR» (la ventana del prisma)',
      'PrismaInteriorUI.Alternar' in f_pilar)
check('FUENTE: EL PILAR NO SE ROMPE (MinPick 9999 · CanDrop false)',
      'MinPick = 9999' in f_pilar and 'CanDrop(int i, int j) => false' in f_pilar)

# === VEREDICTO ===
print()
if _falla:
    print(f'AUDIT v6.120: {len(_falla)} FALLAS')
    for f in _falla:
        print('  - ' + f)
    sys.exit(1)
print('AUDIT v6.120: TODO VERIFICADO ✔')
