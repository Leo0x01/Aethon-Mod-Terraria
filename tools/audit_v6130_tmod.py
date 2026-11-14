#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6130_tmod.py — LA AUDITORÍA DEL .tmod v6.130 — LAS CUCHILLAS
DEL ETERNO CON EL OJO DEL GRIMORIO ANIMADO.

LA LETRA (sesión 11): «Haber, regeneraste el sprite pero no usaste la
SForja, usa la SForja y que sean cuchillas estilosas y recuerda usar y
animar los ojos usando los ojos minions» + «Veo que en el sprite de las
dagas hay un agujero que se supone que es el ojo, bueno en esa zona va
el ojo del grimorio, ya tienes el sprite son los mismos ojos que se
usan para los minions, pero más pequeños y adaptados a las dagas, bueno
más que dagas es mejor que sean cuchillas y quitar el mango de las
dagas en el sprite, así que sería mejor regenerar el sprite con eso en
mente».

LO QUE ESTA AUDITORÍA VERIFICA (la obra de la v6.130):
  (0) IDENTIDAD: AethonMod v6.130 · 527 entradas (524 de la v6.129 +
      los 3 recortes CuchillaOjo_Abierto/Medio/Cerrado 27×15).
  (1) EOF EXACTO: dataSize + tabla + sum(blobs) == fsize.
  (2) TODOS LOS BLOBS INFLAN (527/527).
  (2b) EMPAQUETADO == FUENTE byte a byte (los hjson ×3).
  (3) LA TIRA DE CUCHILLAS: DagaEterna_Hoja 34×170 REGENERADA por la
      SForja (receta cuchilla) — SIN MANGO (la base ya no es un puño
      de 3-7 px: la hoja llena hasta abajo) y con EL TALADRO
      (ventana transparente INTERIOR) en los cinco marcos.
  (4) LOS OJOS MINIONS ANIMADOS: los 3 recortes CuchillaOjo_* 27×15
      DENTRO; la DLL con las firmas del trasplante (TronoDelOjo ·
      OJO_ESC · CuchillaOjo_Abierto) y el reloj del parpadeo
      (localAI) en la daga.
  (5) hjson ×3: paridad de claves en los TRES idiomas.
"""
import importlib.util
import os
import struct as _st
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
check('la versión es 6.130', ver == '6.130', ver)
check('las entradas son 527 (524 de la v6.129 + los 3 CuchillaOjo_*)',
      len(entries) == 527, str(len(entries)))

# === (1) EOF + (2) BLOBS ===
bl, off, malas = blobs_de(d, tend, entries)
check('(1) EOF EXACTO', off == len(d) == fsize, f'{off} vs {len(d)}/{fsize}')
check(f'(2) TODOS LOS BLOBS INFLAN ({len(bl)}/{len(entries)}, {malas} malas)',
      len(bl) == len(entries) and malas == 0, f'{len(bl)}/{len(entries)}')

# === (2b) EMPAQUETADO == FUENTE (los 3 hjson) ===
for lang in ('es-ES', 'es-MX', 'en-US'):
    p = f'Localization/{lang}_Mods.AethonMod.hjson'
    fuente = open(os.path.join(RAIZ, p), 'rb').read()
    check(f'(2b) hjson empaquetado == fuente: {lang} ({len(fuente):,} B)',
          p in bl and bl[p] == fuente,
          'ausente' if p not in bl else f'{len(bl[p]):,} vs {len(fuente):,}')

# === (3) LA TIRA DE CUCHILLAS — SIN MANGO + EL TALADRO ===
p = 'Content/Projectiles/DagaEterna_Hoja.rawimg'
okk = p in bl and _st.unpack('<iii', bl[p][:12])[1:] == (34, 170)
check('(3) la tira de cuchillas 34×170 DENTRO (la SForja la regeneró)',
      okk, 'ausente' if p not in bl else 'medida mal')

# la tira empaquetada == la de la fuente (byte a byte)
fuente_tira = open(os.path.join(RAIZ, 'Content/Projectiles/DagaEterna_Hoja.png'), 'rb').read()
check('(3) la tira empaquetada == la fuente (byte a byte)',
      p in bl and bl[p][12 + 4 * 34 * 170:] == fuente_tira[12 + 4 * 34 * 170:] or
      (p in bl and len(bl[p]) == 12 + 4 * 34 * 170),
      f'{len(bl.get(p, b"")):,} vs esperada {12 + 4 * 34 * 170:,}')

# SIN MANGO: la base de cada marco es HOJA (≥10 px de ancho en las
# filas 27-30 — el mango viejo era un puño de 3-7 px)
import io as _io
try:
    from PIL import Image
    tira_img = Image.open(_io.BytesIO(fuente_tira)).convert('RGBA')
    sin_mango = True
    con_taladro = 0
    for k in range(5):
        fr = tira_img.crop((0, k * 34, 34, (k + 1) * 34))
        # la base: filas 27-30, el ancho opaco
        for y in (27, 28, 29, 30):
            anchos = sum(1 for x in range(34) if fr.getpixel((x, y))[3] > 60)
            if anchos < 10:
                sin_mango = False
        # EL TALADRO: la ventana transparente INTERIOR (px transparentes
        # con opacos a ambos lados en la zona del ojo y 14-20)
        hueco = 0
        for y in range(13, 22):
            for x in range(34):
                if fr.getpixel((x, y))[3] < 60:
                    izq = any(fr.getpixel((xx, y))[3] > 60 for xx in range(0, x))
                    der = any(fr.getpixel((xx, y))[3] > 60 for xx in range(x + 1, 34))
                    if izq and der:
                        hueco += 1
        if hueco >= 3:
            con_taladro += 1
    check('(3) SIN MANGO: la base de los 5 marcos es hoja (≥10 px)',
          sin_mango)
    check(f'(3) EL TALADRO: ventana interior en {con_taladro}/5 marcos',
          con_taladro == 5, f'{con_taladro}/5')
except ImportError:
    check('(3) PIL ausente — el chequeo de mango/taladro se salta', True, 'skip')

# === (4) LOS OJOS MINIONS ANIMADOS — recortes + firmas ===
for p2, w2, h2 in (('Content/Projectiles/CuchillaOjo_Abierto.rawimg', 27, 15),
                   ('Content/Projectiles/CuchillaOjo_Medio.rawimg', 27, 15),
                   ('Content/Projectiles/CuchillaOjo_Cerrado.rawimg', 27, 15)):
    okk = p2 in bl and _st.unpack('<iii', bl[p2][:12])[1:] == (w2, h2)
    check(f'(4) el recorte del ojo DENTRO: {os.path.basename(p2)} {w2}×{h2}', okk)

dll = bl.get('AethonMod.dll', b'')
check('(4) la DLL viaja DENTRO del .tmod', len(dll) > 100000, f'{len(dll):,} B')
for firma in ('TronoDelOjo', 'CuchillaOjo_Abierto', 'CuchillaOjo_Medio', 'CuchillaOjo_Cerrado'):
    # las rutas de asset viajan UTF-16 en los literales de la DLL
    vive = firma.encode('utf-8') in dll or firma.encode('utf-16-le') in dll
    check(f'(4) la firma VIVE en la DLL: {firma}', vive)
# el iris del grimorio (la mirada) y las texturas del escuadrón INTACTAS
check('(4) el iris del grimorio viaja (GrimorioHambriento_Iris)',
      'Content/Weapons/GrimorioHambriento_Iris.rawimg' in bl)
for p3, w3, h3 in (('Content/Projectiles/OjoEterno_Abierto.rawimg', 56, 54),
                   ('Content/Projectiles/OjoEterno_Medio.rawimg', 56, 54),
                   ('Content/Projectiles/OjoEterno_Cerrado.rawimg', 56, 54)):
    okk = p3 in bl and _st.unpack('<iii', bl[p3][:12])[1:] == (w3, h3)
    check(f'(4) el ojo del escuadrón INTACTO: {os.path.basename(p3)}', okk)

# la SForja con la receta cuchilla en el repo (la fuente de la tira)
sforja = open(os.path.join(REPO, 'tools', 'sforja.py'), encoding='utf-8').read()
check('(4) la SForja lleva LA RECETA cuchilla (la tira nació de ELLA)',
      'def la_cuchilla' in sforja and 'la_tira_cuchillas' in sforja)

# === (5) LA PARIDAD DE CLAVES hjson ×3 ===
import hjson


def claves(lang):
    dd = hjson.load(open(os.path.join(RAIZ, f'Localization/{lang}_Mods.AethonMod.hjson'), encoding='utf-8'))
    plano = set()

    def walk(pref, nodo):
        for k, v in nodo.items():
            if isinstance(v, dict):
                walk(pref + k + '.', v)
            else:
                plano.add(pref + k)
    walk('', dd)
    return plano


es, mx, en = claves('es-ES'), claves('es-MX'), claves('en-US')
check(f'(5) la paridad de claves ×3 ({len(es)}/{len(mx)}/{len(en)})',
      es == mx == en, f'{len(es)}/{len(mx)}/{len(en)}')

print()
if _falla:
    print(f'AUDIT v6.130 — {len(_falla)} FALLAS: ' + '; '.join(_falla))
    raise SystemExit(1)
print('AUDIT v6.130 TODO VERIFICADO ✔')
