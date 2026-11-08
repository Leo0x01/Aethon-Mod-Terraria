#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6117_tmod.py — LA AUDITORÍA DEL .tmod v6.117 — EL TRAGO DE
VERDAD (la cura del «hay errores» + «arregla la animacion del
grimorio devorando al jugador» del client.log de la v6.116).

LA AUTOPSIA DEL client.log (DOS errores, ambos de la casa):
  (A) LA EXCEPCIÓN SILENCIOSA — SubmundoGrimorio.DibujarVelo:747
      «InvalidOperationException: Begin has been called before calling
      End» — el velo llamaba Begin DENTRO del lote que la capa de
      interfaz YA tenía abierto (el IL de GameInterfaceLayer.Draw:
      la capa abre Begin(Deferred, null, null, null, null, null,
      Matrix) y lo cierra al volver) → el velo NUNCA se dibujó y la
      sonda lo mató PARA TODA LA SESIÓN: ni revelación, ni ojo, NI LA
      INUNDACIÓN de la devoración (la animación se veía ROTA).
  (B) EL CRASH FATAL — Main.DrawToMap_Section:
      «IndexOutOfRangeException: Index was outside the bounds of the
      array» justo tras entrar al interior (800×400): las secciones
      del minimapa son de 200×150 tiles, los mundos vanilla son TODOS
      múltiplos exactos (1200/1800/2400 de alto) y el interior de
      400 NO lo era → la fila de secciones y=2 lee Map[x, 400..449]
      con el indexador CRUDO → Main engine crash.

LA CURA v6.117 (verificada contra el IL del tML 2026.9.3.1 real):
  · Alto 400→450 (3×150 EXACTO) + ReiniciarMiniMapa() en ambas
    direcciones (MapHelper.ResetMapData + updateMap/mapMin/mapMax/
    refreshMap/clearMap — DrawToMap NO clampea mapMaxX/mapMaxY);
  · EL VELO SIN Begin/End (dibuja en el lote abierto de la capa);
  · EL FARO MUDADO A Main.OnPostDraw (el único punto que pinta
    TAMBIÉN en gameMenu — la pantalla de carga queda TRAGADA por la
    inundación: un trago continuo sin huecos);
  · LA ANIMACIÓN DE VERDAD: el libro se PARTE EN DOS TAPAS que
    giran sobre el lomo (las fauces EXPUESTAS — antes el sprite
    entero tapaba su propia boca), el portador SUBE EN ESPIRAL
    GIRANDO (Player.fullRotation) y al ser tragado DESAPARECE
    (hideEntirePlayer por ModifyDrawInfo) dentro del estallido.

  (0) IDENTIDAD: AethonMod v6.117 · tML 2026.9.3.1 · 492 entradas.
  (1) EOF EXACTO: dataSize + tabla + sum(blobs) == fsize.
  (2) TODOS LOS BLOBS INFLAN (492/492).
  (2b) EMPAQUETADO == FUENTE byte a byte (los 3 hjson).
  (3) LAS FIRMAS VIVEN en la DLL: las de siempre + LAS NUEVAS de la
      cura (ReiniciarMiniMapa, PintarFaroDevoracion, OnPostDraw,
      ResetMapData, hideEntirePlayer, fullRotation, Tragado, OnKill).
  (4) LA MÁSCARA Y LOS ICONOS de la v6.116 siguen intactos.
  (5) LOS TEXTOS de la v6.114/v6.115 VIVOS en los 3 espejos.
  (6) LOS KEYSETS ×3 IDÉNTICOS (es-MX == es-ES == en-US).
  (7) LA FUENTE DE LA CURA: Alto=450 · sin Begin en DibujarVelo ·
      las dos tapas (srcArriba/srcAbajo) · el giro · el ocultado.
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
check('la versión es 6.117', ver == '6.117', ver)
check('las entradas son 492 (las 492 de la v6.116 — la cura es toda .cs)',
      len(entries) == 492, str(len(entries)))

# === (1) EOF + (2) BLOBS ===
bl, off, malas = blobs_de(d, tend, entries)
check('EOF EXACTO', off == len(d), f'{off:,} == {len(d):,}')
check('TODOS LOS BLOBS INFLAN (492/492)', malas == 0 and len(bl) == 492, f'{len(bl)}/492 · malas={malas}')

# === (2b) EMPAQUETADO == FUENTE (los 3 hjson) ===
for lang in ('es-MX', 'es-ES', 'en-US'):
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    src = open(os.path.join(RAIZ, key), 'rb').read()
    check(f'empaquetado == fuente: {key}', key in bl and bl[key] == src)

# === (3) LAS FIRMAS EN LA DLL ===
dll = bl.get('AethonMod.dll', b'')
firmas_vivas = [
    # v6.114/v6.115/v6.116 — el mundo interior y su gente
    b'SubmundoGrimorio', b'CeremoniaLlamado', b'FormaEternaItem', b'BolsaFormaEterna',
    b'MundoInteriorEternoItem', b'DevoracionEterna', b'AltarPrismaTile', b'PrismaInteriorUI',
    b'BrumaFaro', b'PedirSolEclipse', b'AlmaEternoGlobalItem',
    # v6.117 — LA CURA DEL TRAGO: el minimapa, el faro del post-draw,
    # el giro de la presa, su ocultado y la red de seguridad
    b'ReiniciarMiniMapa', b'PintarFaroDevoracion',
    b'OnPostDraw', b'ResetMapData', b'hideEntirePlayer',
    b'fullRotation', b'Tragado', b'OnKill', b'ModifyDrawInfo',
]
for f in firmas_vivas:
    check(f'DLL VIVA: {f.decode()}', f in dll)

# === (4) LA MÁSCARA + LOS ICONOS (tML los empaqueta como .rawimg:
#      [i32 versión][i32 ancho][i32 alto] LE + RGBA) ===
def rawimg_size(b):
    _v, w, h = struct.unpack('<iii', b[:12])
    return w, h


mascara = 'Content/Tiles/AltarPrismaTile_Highlight.rawimg'
ok = mascara in bl
det = ''
if ok:
    w, h = rawimg_size(bl[mascara])
    det = f'{w}x{h}'
    ok = (w, h) == (54, 54)
check('MÁSCARA VIVA: AltarPrismaTile_Highlight.rawimg 54x54', ok, det)

for icono in ('Content/Items/Cosmetics/FormaEternaItem.rawimg',
              'Content/Items/Bolsas/BolsaFormaEterna.rawimg'):
    ok = icono in bl
    det = ''
    if ok:
        w, h = rawimg_size(bl[icono])
        det = f'{w}x{h}'
        ok = (w, h) == (30, 30)
    check(f'ICONO VIVO: {os.path.basename(icono)} 30x30', ok, det)

# === (5) LOS TEXTOS VIVOS ===
import re as _re
textos = {
    'es-MX': ['SIN SIERRA', 'La Forma Eterna', 'EL INTERIOR DEL ETERNO',
              'La Bolsa de la Forma Eterna'],
    'es-ES': ['SIN SIERRA', 'La Forma Eterna', 'EL INTERIOR DEL ETERNO'],
    'en-US': ['SAW-FREE', 'The Eternal Form', "THE ETERNAL'S INTERIOR"],
}
for lang, textos_lang in textos.items():
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    contenido = bl.get(key, b'').decode('utf-8')
    for t in textos_lang:
        check(f'texto {lang}: «{t[:34]}»', t in contenido)

# === (6) LOS KEYSETS ×3 IDÉNTICOS ===
def keyset(lang):
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    return set(_re.findall(r'^[\t ]*([\w.]+):\s*\{', bl.get(key, b'').decode('utf-8'), _re.M))

k_mx, k_es, k_us = keyset('es-MX'), keyset('es-ES'), keyset('en-US')
check('keysets es-MX == es-ES', k_mx == k_es, f'{len(k_mx)} vs {len(k_es)}')
check('keysets es-MX == en-US', k_mx == k_us, f'{len(k_mx)} vs {len(k_us)}')

# === (7) LA FUENTE DE LA CURA (el diff v6.116→v6.117 en 3 archivos) ===
fuente_sub = open(os.path.join(RAIZ, 'Content', 'Systems', 'SubmundoGrimorio.cs'),
                  encoding='utf-8').read()
check('FUENTE: Alto = 450 (LA LEY DE LAS SECCIONES — 3×150 exacto)',
      'private const int Alto = 450;' in fuente_sub)
check('FUENTE: ReiniciarMiniMapa vive y se llama en ENTRAR y SALIR',
      'private static void ReiniciarMiniMapa()' in fuente_sub
      and fuente_sub.count('ReiniciarMiniMapa();') == 2)
check('FUENTE: el faro suscrito a OnPostDraw (cubre la carga)',
      'Main.OnPostDraw += PintarFaroDevoracion;' in fuente_sub
      and 'Main.OnPostDraw -= PintarFaroDevoracion;' in fuente_sub)

# DibujarVelo SIN Begin/End: la función vieja abría su propio lote —
# la nueva dibuja en el de la capa (cero Begin/End en el cuerpo).
cuerpo_velo = fuente_sub.split('private static bool DibujarVelo()')[1].split('\n        }')[0]
check('FUENTE: DibujarVelo ya NO abre lote (la cura del Begin-sobre-Begin)',
      'sb.Begin' not in cuerpo_velo and 'sb.End' not in cuerpo_velo)
cuerpo_faro = fuente_sub.split('private static void PintarFaroDevoracion(GameTime gameTime)')[1].split('\n        }')[0]
check('FUENTE: el faro pinta en lote propio con BAILE (cerrar→Begin→End)',
      'CerrarLoteSiAbierto' in cuerpo_faro and 'spriteBatch.Begin' in cuerpo_faro
      and 'spriteBatch.End' in cuerpo_faro)

fuente_dev = open(os.path.join(RAIZ, 'Content', 'Projectiles', 'Definitivas',
                               'DevoracionEterna.cs'), encoding='utf-8').read()
check('FUENTE: las DOS TAPAS girando sobre el lomo (srcArriba/srcAbajo)',
      'srcArriba' in fuente_dev and 'srcAbajo' in fuente_dev
      and 'angMand' in fuente_dev)
check('FUENTE: el GIRO de la presa (fullRotation acelera con la cercanía)',
      'fullRotation +=' in fuente_dev and 'fullRotationOrigin' in fuente_dev)
check('FUENTE: LA RED DE SEGURIDAD (OnKill libera presa y bruma)',
      'public override void OnKill(int timeLeft)' in fuente_dev
      and 'Tragado = false;' in fuente_dev)

fuente_pl = open(os.path.join(RAIZ, 'Content', 'Players', 'ShardPlayer.cs'),
                 encoding='utf-8').read()
check('FUENTE: la presa tragada NO se dibuja (ModifyDrawInfo → hideEntirePlayer)',
      'public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)' in fuente_pl
      and 'drawInfo.hideEntirePlayer = true;' in fuente_pl)

# === (8) CADENA DE CUSTODIA: repo == ModSources ===
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

print()
if fallos:
    print(f'*** AUDIT v6.117: {len(fallos)} FALLOS ***')
    sys.exit(1)
print('*** AUDIT v6.117 TODO VERIFICADO ***')
