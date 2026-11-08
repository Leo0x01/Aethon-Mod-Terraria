#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6116_tmod.py — LA AUDITORÍA DEL .tmod v6.116 — LA MÁSCARA DEL
PRISMA (la cura del «hay errores» del client.log).

LA LETRA: el usuario cargó la v6.115 y EL MOD SE DESACTIVÓ SOLO —
    ReLogic.Content.AssetLoadException: Asset could not be found:
    "Content\\Tiles\\AltarPrismaTile_Highlight"
    → Terraria.ModLoader.ModTile.SetupContent() línea 145:
       if (TileID.Sets.HasOutlines[Type]) → pide {Texture}_Highlight.
El prisma encendió HasOutlines (calco del altar de la casa) pero nació
sin su máscara — y el headless NO lo caza (el servidor no carga
texturas). LA CURA: la máscara 54×54 del Taller (el contorno del
cuerpo del prisma en el blanco cálido de las páginas) + EL GUARDIÁN
CURADO (la pasada (4) nueva: HasOutlines ⇒ _Highlight, en disco y en
el .tmod — la lección v6.87 vuelve al gate PARA SIEMPRE) + de regalo
el aviso del clon del banco muerto (AlmaEternoGlobalItem.Clone).

  (0) IDENTIDAD: AethonMod v6.116 · tML 2026.9.3.1 · 492 entradas.
  (1) EOF EXACTO: dataSize + tabla + sum(blobs) == fsize.
  (2) TODOS LOS BLOBS INFLAN (492/492).
  (2b) EMPAQUETADO == FUENTE byte a byte (los 3 hjson).
  (3) LAS FIRMAS VIVEN en la DLL: las de la v6.114/v6.115 (el mundo
      interior con su prisma y su ceremonia, la membrana, la forma
      eterna) + el CLON DEL BANCO (AlmaEternoGlobalItem.Clone en la
      FUENTE — la cura del aviso del cargador).
  (4) LA MÁSCARA NUEVA: AltarPrismaTile_Highlight.rawimg 54×54 (el
      arte del cursor inteligente del prisma) + los 2 iconos de la
      v6.114 siguen 30×30.
  (5) LOS TEXTOS de la v6.114/v6.115 VIVOS en los 3 espejos.
  (6) LOS KEYSETS ×3 IDÉNTICOS (es-MX == es-ES == en-US).
  (7) CADENA DE CUSTODIA: repo == ModSources en los .cs/.hjson/.png.
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
check('la versión es 6.116', ver == '6.116', ver)
check('las entradas son 492 (491 de la v6.115 + la máscara del prisma)', len(entries) == 492, str(len(entries)))

# === (1) EOF + (2) BLOBS ===
bl, off, malas = blobs_de(d, tend, entries)
check('EOF EXACTO', off == len(d), f'{off:,} == {len(d):,}')
check('TODOS LOS BLOBS INFLAN (492/492)', malas == 0 and len(bl) == 492, f'{len(bl)}/492 · malas={malas}')

# === (2b) EMPAQUETADO == FUENTE (los 3 hjson) ===
for lang in ('es-MX', 'es-ES', 'en-US'):
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    src = open(os.path.join(RAIZ, key), 'rb').read()
    check(f'empaquetado == fuente: {key}', key in bl and bl[key] == src)

# === (3) LAS FIRMAS EN LA DLL + EL CLON EN LA FUENTE ===
dll = bl.get('AethonMod.dll', b'')
firmas_vivas = [
    # v6.114 — el mundo interior + la ceremonia + la membrana + la forma eterna
    b'SubmundoGrimorio', b'CeremoniaLlamado', b'FormaEternaItem', b'BolsaFormaEterna',
    b'EmitirMembrana', b'EmitirMembranaRuido',
    # v6.115 — el trago y el eclipse del interior + la sierra muerta
    b'MundoInteriorEternoItem', b'DevoracionEterna', b'AltarPrismaTile', b'PrismaInteriorUI',
    b'EmitirBordeMaestro', b'BrumaFaro', b'PedirSolEclipse', b'UpdateVanity',
    # v6.116 — el alma del banco (la clase que aprendió a clonarse)
    b'AlmaEternoGlobalItem',
]
for f in firmas_vivas:
    check(f'DLL VIVA: {f.decode()}', f in dll)

fuente_alma = open(os.path.join(RAIZ, 'Content', 'Globals', 'AlmaEternoGlobalItem.cs'),
                   encoding='utf-8').read()
check('FUENTE VIVA: AlmaEternoGlobalItem.Clone (la cura del aviso del cargador)',
      'public override GlobalItem Clone(Item from, Item to)' in fuente_alma
      and 'copia.BancoItems = new Dictionary<int, long>(BancoItems)' in fuente_alma)

# === (4) LA MÁSCARA NUEVA + LOS ICONOS (tML los empaqueta como .rawimg:
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

# === (7) CADENA DE CUSTODIA: repo == ModSources ===
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
    print(f'*** AUDIT v6.116: {len(fallos)} FALLOS ***')
    sys.exit(1)
print('*** AUDIT v6.116 TODO VERIFICADO ***')
