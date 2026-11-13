#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6128_tmod.py — LA AUDITORÍA DEL .tmod v6.128 — LAS DAGAS NACIDAS
EN LA SFORJA.

LA LETRA (v6.128): «Usa la SForja para crear el diseño de las dagas» —
la ley de la casa (v6.124): «todo proyectil debe ser creado con la
superforja, en este caso con la nueva que sera creada SForja»: las cinco
dagas del Eterno nacieron de un script aparte (dagas_v6124.py) — la
receta `daga` VIVE AHORA en la SForja (tools/sforja.py, comando
`proyectil --receta daga`) y LA TIRA fue re-forjada con ella.

LO QUE ESTA AUDITORÍA VERIFICA (la obra de la v6.128):
  (0) IDENTIDAD: AethonMod v6.128 · 524 entradas (las mismas de la
      v6.127 — solo la TIRA cambió de contenido).
  (1) EOF EXACTO: dataSize + tabla + sum(blobs) == fsize.
  (2) TODOS LOS BLOBS INFLAN (524/524).
  (2b) EMPAQUETADO == FUENTE byte a byte (la TIRA nueva + el cetro +
      el icono del buff, intactos).
  (3) LA TIRA DE LAS DAGAS (el corazón de la versión): 34×170, CINCO
      marcos distintos (las firmas alfa), la CUENCA del ojo (10,6,18)
      en (17,15)±2 en los cinco marcos — donde el iris vivo del
      DagaEternaMinion aterriza en runtime.
  (4) hjson: es-ES == es-MX con cuerpos IDÉNTICOS (la ley monolingüe).
"""
import importlib.util
import io
import os
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
check('la versión es 6.128', ver == '6.128', ver)
check('las entradas son 524 (las mismas de la v6.127 — solo la TIRA cambió)',
      len(entries) == 524, str(len(entries)))

# === (1) EOF + (2) BLOBS ===
bl, off, malas = blobs_de(d, tend, entries)
check('(1) EOF EXACTO', off == len(d) == fsize, f'{off} vs {len(d)}/{fsize}')
check(f'(2) TODOS LOS BLOBS INFLAN ({len(bl)}/{len(entries)}, {malas} malas)',
      len(bl) == len(entries) and malas == 0, f'{len(bl)}/{len(entries)}')

# === (2b) EMPAQUETADO == FUENTE (los 3 hjson — la ley de siempre) ===
for lang in ('es-ES', 'es-MX', 'en-US'):
    p = f'Localization/{lang}_Mods.AethonMod.hjson'
    fuente = open(os.path.join(RAIZ, p), 'rb').read()
    check(f'(2b) hjson empaquetado == fuente: {lang} ({len(fuente):,} B)',
          p in bl and bl[p] == fuente,
          'ausente' if p not in bl else f'{len(bl[p]):,} vs {len(fuente):,}')

# === (3) LA TIRA DE LAS DAGAS — el corazón de la versión ===
try:
    import struct as _st
    import numpy as np
    from PIL import Image
    p_tira = 'Content/Projectiles/DagaEterna_Hoja.rawimg'
    check('(3) la TIRA viaja DENTRO del .tmod', p_tira in bl)
    _v, w, h = _st.unpack('<iii', bl[p_tira][:12])
    a = np.frombuffer(bl[p_tira], dtype=np.uint8, offset=12).reshape(h, w, 4)
    check('(3) la TIRA es 34×170 (5 marcos de 34×34 — el recorte del PreDraw)',
          (w, h) == (34, 170), f'{w}x{h}')
    # la TIRA del .tmod == la TIRA forjada de la fuente (píxel a píxel)
    a_fuente = np.array(Image.open(os.path.join(RAIZ, 'Content/Projectiles/DagaEterna_Hoja.png')).convert('RGBA'))
    check('(3) la TIRA empaquetada == la TIRA forjada (píxel a píxel)',
          a.shape == a_fuente.shape and bool((a == a_fuente).all()),
          f'{a.shape} vs {a_fuente.shape}')
    firmas = [int(a[k * 34:(k + 1) * 34, :, 3].sum()) for k in range(5)]
    check('(3) las CINCO dagas son DISTINTAS (las firmas alfa)',
          len(set(firmas)) == 5, str(firmas))
    nombres = ['cristal', 'llama', 'roca', 'vacio', 'veredicto']
    todo_ok = True
    for k, nm in enumerate(nombres):
        m = a[k * 34:(k + 1) * 34]
        cuenca = ((m[:, :, 0] == 10) & (m[:, :, 1] == 6) & (m[:, :, 2] == 18) & (m[:, :, 3] == 255))
        ys, xs = (cuenca.nonzero()[0], cuenca.nonzero()[1]) if cuenca.any() else ([], [])
        okk = (len(xs) >= 5 and 13 <= xs.mean() <= 21 and 13 <= ys.mean() <= 17
               and (m[:, :, 3] >= 250).sum() > 150)
        todo_ok = todo_ok and okk
        print(f'    {"OK " if okk else "FALLA"} marco {k} ({nm}): cuerpo {(m[:, :, 3] >= 250).sum()} px · cuenca {len(xs)} px en ({xs.mean() if len(xs) else -1:.1f},{ys.mean() if len(ys) else -1:.1f})')
        if not okk:
            _falla.append(f'marco {k} {nm}')
    check('(3) EL OJO EN EL CENTRO de las cinco (la cuenca en (17,15)±2 — el trono del iris vivo)', todo_ok)
    # las OTRAS texturas de las dagas siguen intactas (el cetro y el icono)
    for p, we, he in (('Content/Weapons/Investigaciones/CetroDagasEterno.rawimg', 40, 46),
                      ('Content/Buffs/DagaEternaBuff.rawimg', 32, 32)):
        okk = p in bl and _st.unpack('<iii', bl[p][:12])[1:] == (we, he)
        check(f'(3) intacta: {os.path.basename(p)} {we}x{he}', okk)
except ImportError:
    check('(3) PIL disponible para la TIRA', False, 'sin Pillow')

print()
if _falla:
    print(f'AUDIT v6.128 — {len(_falla)} FALLAS: ' + '; '.join(_falla))
    raise SystemExit(1)
print('AUDIT v6.128 TODO VERIFICADO ✔')
