#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6129_tmod.py — LA AUDITORÍA DEL .tmod v6.129 — EL ALA LISA SIN
PLUMAS + EL CETRO QUE INVOCABA + LA DEVORACIÓN DE LA PRIMERA VEZ.

LA LETRA (sesión 10): «las alas siguen con picos, diente de sierra,
supongo que esos dientes de sierra son supuestas plumas, esas plumas las
puedes eliminar» + «el arma cetro de las dagas del eterno no invoca nada,
mejora eso y revisa porque no invoca nada, haz que esta arma no sea de
invocador asi no se ven afectado los minions» + «quita eso ya que no hace
diferencias y todo lo que el jugador usa está separado del jugador cuando
es devorado… la primera vez que se puso la mecanica de que el grimorio del
eterno devore al jugador esta animacion estaba correcta, asi que
investiga como funcionaba la primera vez».

LO QUE ESTA AUDITORÍA VERIFICA (la obra de la v6.129):
  (0) IDENTIDAD: AethonMod v6.129 · 524 entradas (las mismas de la
      v6.128 — solo los .cs y los hjson cambiaron de contenido).
  (1) EOF EXACTO: dataSize + tabla + sum(blobs) == fsize.
  (2) TODOS LOS BLOBS INFLAN (524/524).
  (2b) EMPAQUETADO == FUENTE byte a byte (los hjson ×3).
  (3) EL ALA LISA (la DLL con la geometría nueva): EstirarVelaLisa +
      PuntoCaminoLisa + PADLisa VIVEN en la DLL; la TIRA de las dagas
      y las demás texturas INTACTAS.
  (4) LA DEVORACIÓN DE LA PRIMERA VEZ: DevoracionCosmeticosSistema y
      la ventana «Arrastrando» MURIERON (sus nombres JAMÁS en la DLL);
      Tragado VIVE (la ocultación del trago — hideEntirePlayer).
  (5) hjson ×3: el tooltip del ALA LISA SIN PLUMAS en es-ES == es-MX
      y la paridad de claves en los TRES idiomas.
"""
import importlib.util
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
check('la versión es 6.129', ver == '6.129', ver)
check('las entradas son 524 (las mismas de la v6.128 — solo .cs y hjson cambiaron)',
      len(entries) == 524, str(len(entries)))

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

# === (3) EL ALA LISA — las firmas de la geometría nueva en la DLL ===
dll = bl.get('AethonMod.dll', b'')
check('(3) la DLL viaja DENTRO del .tmod', len(dll) > 100000, f'{len(dll):,} B')
for firma in ('EstirarVelaLisa', 'PuntoCaminoLisa', 'PADLisa', 'RNaceLisa'):
    check(f'(3) la firma VIVE en la DLL: {firma}', firma.encode('utf-8') in dll)
check('(3) el campo LISAS del catálogo vive (Lisas: el ala lisa del vacío)',
      b'Lisas' in dll)

# la TIRA de las dagas y las demás texturas de la v6.128 INTACTAS
import struct as _st
for p, we, he in (('Content/Projectiles/DagaEterna_Hoja.rawimg', 34, 170),
                  ('Content/Weapons/Investigaciones/CetroDagasEterno.rawimg', 40, 46),
                  ('Content/Buffs/DagaEternaBuff.rawimg', 32, 32)):
    okk = p in bl and _st.unpack('<iii', bl[p][:12])[1:] == (we, he)
    check(f'(3) intacta: {os.path.basename(p)} {we}x{he}', okk)

# === (4) LA DEVORACIÓN DE LA PRIMERA VEZ — los muertos y los vivos ===
check('(4) DevoracionCosmeticosSistema MURIÓ (su nombre JAMÁS en la DLL)',
      b'DevoracionCosmeticosSistema' not in dll)
check('(4) la ventana «Arrastrando» MURIÓ (el campo público ya no existe)',
      b'Arrastrando' not in dll)
check('(4) Tragado VIVE (la ocultación del trago — hideEntirePlayer)',
      b'Tragado' in dll)
check('(4) Devorando VIVE (el festín consulta el grimorio flotante)',
      b'Devorando' in dll)

# === (5) EL TOOLTIP DEL ALA LISA ×3 + LA PARIDAD DE CLAVES ===
import hjson
texto_es = ('EL ALA LISA DEL VACÍO, SIN PLUMAS').encode('utf-8')
for lang in ('es-ES', 'es-MX'):
    p = f'Localization/{lang}_Mods.AethonMod.hjson'
    check(f'(5) el tooltip del ala lisa en {lang}',
          p in bl and texto_es in bl[p])

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
    print(f'AUDIT v6.129 — {len(_falla)} FALLAS: ' + '; '.join(_falla))
    raise SystemExit(1)
print('AUDIT v6.129 TODO VERIFICADO ✔')
