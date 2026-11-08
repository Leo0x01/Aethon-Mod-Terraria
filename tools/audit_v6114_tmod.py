#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6114_tmod.py — LA AUDITORÍA DEL .tmod v6.114 — LA MEMBRANA DEL
VACÍO + EL MUNDO INTERIOR + LA CEREMONIA DE LA LLAMADA + LA FORMA ETERNA.

  (0) IDENTIDAD: AethonMod v6.114 · tML 2026.9.3.1 · 489 entradas.
  (1) EOF EXACTO: dataSize + tabla + sum(blobs) == fsize.
  (2) TODOS LOS BLOBS INFLAN (489/489).
  (2b) EMPAQUETADO == FUENTE byte a byte (los 3 hjson — la lección v6.113).
  (3) LAS FIRMAS NUEVAS VIVEN en la DLL: SubmundoGrimorio (+ su flujo:
      Entrar/Salir/CallbackEntrar/CallbackSalir/GenerarInterior/
      BloquearGuardado/DibujarElOjo/EmitirMembrana/EmitirMembranaRuido/
      Perfil redondo), CeremoniaLlamado, FormaEternaItem, BolsaFormaEterna,
      InteriorSpawnGate, EscenaInteriorGrimorio.
  (4) LOS ICONOS NUEVOS: FormaEternaItem.png + BolsaFormaEterna.png 30×30.
  (5) LOS TEXTOS NUEVOS en los 3 espejos: SIN SIERRA / LA FORMA ETERNA /
      LA LLAMADA CONTESTA / EL INTERIOR DEL ETERNO (y los espejos en-US).
  (6) LOS KEYSETS ×3 IDÉNTICOS (es-MX == es-ES == en-US).
  (7) CADENA DE CUSTODIA: repo == ModSources en los .cs/.hjson/.png.
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
check('la versión es 6.114', ver == '6.114', ver)
check('las entradas son 489 (487 + 2 iconos)', len(entries) == 489, str(len(entries)))

# === (1) EOF + (2) BLOBS ===
todos = {p for p, _, _ in entries}
bl, off, malas = blobs_de(d, tend, entries)
check('EOF EXACTO', off == len(d), f'{off:,} == {len(d):,}')
check('TODOS LOS BLOBS INFLAN (489/489)', malas == 0 and len(bl) == 489, f'{len(bl)}/489 · malas={malas}')

# === (2b) EMPAQUETADO == FUENTE (los 3 hjson) ===
for lang in ('es-MX', 'es-ES', 'en-US'):
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    src = open(os.path.join(RAIZ, key), 'rb').read()
    check(f'empaquetado == fuente: {key}', key in bl and bl[key] == src)

# === (3) LAS FIRMAS NUEVAS EN LA DLL ===
dll = bl.get('AethonMod.dll', b'')
firmas_vivas = [
    b'SubmundoGrimorio', b'CallbackEntrar', b'CallbackSalir', b'GenerarInterior',
    b'BloquearGuardado', b'DibujarElOjo', b'InteriorSpawnGate', b'EscenaInteriorGrimorio',
    b'CeremoniaLlamado', b'FormaEternaItem', b'BolsaFormaEterna',
    b'EmitirMembrana', b'EmitirMembranaRuido', b'Membrana', b'Redonda',
]
for f in firmas_vivas:
    check(f'DLL VIVA: {f.decode()}', f in dll)

# === (4) LOS ICONOS NUEVOS (tML los empaqueta como .rawimg:
#      [i32 versión][i32 ancho][i32 alto] LE + RGBA) ===
import struct as _s


def rawimg_size(b):
    _v, w, h = _s.unpack('<iii', b[:12])
    return w, h


for icono in ('Content/Items/Cosmetics/FormaEternaItem.rawimg',
              'Content/Items/Bolsas/BolsaFormaEterna.rawimg'):
    ok = icono in bl
    det = ''
    if ok:
        w, h = rawimg_size(bl[icono])
        det = f'{w}x{h}'
        ok = (w, h) == (30, 30)
    check(f'ICONO VIVO: {os.path.basename(icono)} 30x30', ok, det)

# === (5) LOS TEXTOS NUEVOS ===
textos = {
    'es-MX': ['SIN SIERRA', 'La Forma Eterna', 'LA LLAMADA CONTESTA',
              'EL INTERIOR DEL ETERNO', 'La Bolsa de la Forma Eterna',
              'EL VACÍO TE DEVUELVE A LA PLAZA'],
    'es-ES': ['SIN SIERRA', 'La Forma Eterna', 'LA LLAMADA CONTESTA',
              'EL INTERIOR DEL ETERNO'],
    'en-US': ['SAW-FREE', 'The Eternal Form', 'THE CALL ANSWERS',
              "THE ETERNAL'S INTERIOR", 'The Bag of the Eternal Form'],
}
for lang, textos_lang in textos.items():
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    contenido = bl.get(key, b'').decode('utf-8')
    for t in textos_lang:
        check(f'texto {lang}: «{t[:34]}»', t in contenido)

# === (6) LOS KEYSETS ×3 IDÉNTICOS ===
def keyset(lang):
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    return set(re.findall(r'^[\t]*([\w.]+):\s*\{', bl.get(key, b'').decode('utf-8'), re.M))

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
    print(f'*** AUDIT v6.114: {len(fallos)} FALLOS ***')
    sys.exit(1)
print('*** AUDIT v6.114 TODO VERIFICADO ***')
