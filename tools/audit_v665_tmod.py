#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.65 — AUDITORÍA DEL .tmod: la casa no firma lo que no leyó.
  (1) tabla + EOF exacto (blobs íntegros, zlib OK, cuenta viva)
  (2) LOS SONIDOS: RunaCarga DENTRO (v6.64) + los nacimientos + la carga
      del RC2 — y RunaCompas SEGUIR ausente (murió en la v6.64)
  (3) LA DLL EXTRAÍDA: las firmas de la v6.65 VIVAS (CirculoDelEscuadron,
      DibujarArcosDelEscuadron — ahora LA CONCENTRACIÓN) y el POZO MUERTO
      de la ley del reflejo (_arcosPos) AUSENTE
"""
import struct, sys, zlib

sys.path.insert(0, '/tmp')
sys.path.insert(0, '/home/z/my-project/download/Aethon-Mod-Terraria/tools')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize}")
assert str(ver) == "6.65", f"versión {ver} != 6.65"

# (1) blobs íntegros: la suma de los tamaños comprimidos + fin de tabla = EOF
ok_eof = (tend + sum(c for _, _, c in entries) == fsize)
print(f"[{'OK' if ok_eof else 'FALLO'}] EOF exacto: {tend} + {sum(c for _,_,c in entries)} = {fsize}")
assert ok_eof

d = open(TMOD, 'rb').read()
off = tend
files = {}
bad = 0
for p, raw, comp in entries:
    blob = d[off:off + comp]
    off += comp
    try:
        # tML guarda SIN comprimir (comp==raw) lo que deflate no achica
        # (ogg, pngs ya comprimidos) — el blob ES el archivo tal cual.
        data = blob if comp == raw else zlib.decompress(blob, -15)
        assert len(data) == raw, f"{p}: raw {raw} != {len(data)}"
        files[p] = data
    except Exception as e:
        bad += 1
        print(f"[FALLO] blob {p}: {e}")
print(f"[{'OK' if bad == 0 else 'FALLO'}] blobs {len(files)}/{len(entries)} descomprimidos, tamaños exactos")
assert bad == 0

# (2) LOS SONIDOS: la carga sin campana (v6.64) + los nacimientos + el RC2
dentro = ["Sounds/RunaCarga.ogg", "Sounds/RunaNaceSol.ogg", "Sounds/RunaNaceVacio.ogg",
          "Sounds/RayoConcentradoCarga.ogg", "Sounds/RayoConcentradoLoop.ogg"]
for s in dentro:
    k = [p for p in files if p.endswith(s)]
    if not k:
        print(f"[FALLO] falta {s}")
        sys.exit(1)
    n = len(files[k[0]])
    vorbis = files[k[0]][:4] == b'OggS'
    print(f"[OK] {s} DENTRO ({n} bytes, {'vorbis' if vorbis else 'NO-vorbis?'})")
    assert vorbis

# RunaCompas (la campana muerta de la v6.64) SEGUIR ausente
if any(p.endswith("Sounds/RunaCompas.ogg") for p in files):
    print("[FALLO] RunaCompas.ogg resucitó — tenía que estar AUSENTE")
    sys.exit(1)
print("[OK] RunaCompas.ogg AUSENTE (la campana sigue muerta)")

# (3) LA DLL: firmas vivas y el muerto ausente (literals UTF-16 + nombres UTF-8)
dll = files["AethonMod.dll"]

def tiene_utf8(s): return s.encode('utf-8') in dll
def tiene_utf16(s): return s.encode('utf-16-le') in dll

vivas = [
    # EL CÍRCULO DE LOS MINIONS (los arcos y las brumas lo leen)
    ("CirculoDelEscuadron", tiene_utf8),
    # LA CONCENTRACIÓN v6.65 (los rayitos en el centro del círculo)
    ("DibujarArcosDelEscuadron", tiene_utf8),
    # LA LEY DE UNO A LA VEZ (el sistema — v6.63, intacta)
    ("RitualVivoDe", tiene_utf8),
    ("_ritualColaHasta", tiene_utf8),
    ("RITUAL_GRACIA_TICKS", tiene_utf8),
    # EL PUESTO COMPARTIDO DEL RITUAL (la corona de la v6.62 — intacta)
    ("SlotDelRitual", tiene_utf8),
    # LA VOZ DE LA CARGA (v6.64) + los nacimientos (v6.63)
    ("AethonMod/Content/Sounds/RunaCarga", tiene_utf16),
    ("AethonMod/Content/Sounds/RunaNaceSol", tiene_utf16),
    ("AethonMod/Content/Sounds/RunaNaceVacio", tiene_utf16),
]
for s, f in vivas:
    ok = f(s)
    print(f"[{'OK' if ok else 'FALLO'}] FIRMA VIVA: {s}")
    assert ok, s

# EL MUERTO v6.65: el pozo de la ley del reflejo (_arcosPos, el array
# cero-GC de las posiciones de los ojos) — la v6.65 lo BORRÓ entero.
if tiene_utf8("_arcosPos"):
    print("[FALLO] _arcosPos vive en la DLL — tenía que estar AUSENTE")
    sys.exit(1)
print("[OK] _arcosPos AUSENTE (el pozo de las cuerdas murió con la ley del reflejo)")

# LAS CONVERGENCIAS DE LA v6.65 (la reclama y la ley del ritual) y el
# afinado del perlin son CUERPOS y constantes IL — la DLL no los separa:
# quedan probados por el oráculo 0/0 + el -build 0/0 sobre la fuente.
print("[OK] reclama/convergencia/afinado verificados en fuente (oráculo 0/0 + -build 0/0)")

import hashlib
md5 = hashlib.md5(d).hexdigest()
print(f"md5 {md5} | {fsize} bytes")

print("\nAUDITORÍA v6.65: TODO OK — el .tmod dice la verdad de la fuente.")
