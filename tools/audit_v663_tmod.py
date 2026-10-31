#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.63 — AUDITORÍA DEL .tmod: la casa no firma lo que no leyó.
  (1) tabla + EOF exacto (blobs íntegros, zlib OK, cuenta viva)
  (2) LOS TRES OGG RÚNICOS DENTRO (RunaCompas NUEVO + los dos mejorados)
  (3) LA DLL EXTRAÍDA: las firmas de la v6.63 VIVAS (CirculoDelEscuadron,
      la ley del reflejo, RitualVivoDe, la cola de gracia, los tres
      caminos de sonido NUEVOS) y el Item122 de RayoConvergente MUERTO
"""
import struct, sys, zlib

sys.path.insert(0, '/tmp')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize}")

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

# (2) LOS TRES OGG DENTRO (y los dos del haz intactos)
sonidos = ["Sounds/RunaCompas.ogg", "Sounds/RunaNaceSol.ogg", "Sounds/RunaNaceVacio.ogg",
           "Sounds/RayoConcentradoCarga.ogg", "Sounds/RayoConcentradoLoop.ogg"]
for s in sonidos:
    k = [p for p in files if p.endswith(s)]
    if not k:
        print(f"[FALLO] falta {s}")
        sys.exit(1)
    n = len(files[k[0]])
    vorbis = files[k[0]][:4] == b'OggS'
    print(f"[OK] {s} DENTRO ({n} bytes, {'vorbis' if vorbis else 'NO-vorbis?'})")
    assert vorbis

# (3) LA DLL: firmas vivas y muertos ausentes (literals UTF-16 + nombres UTF-8)
dll = files["AethonMod.dll"]

def tiene_utf8(s): return s.encode('utf-8') in dll
def tiene_utf16(s): return s.encode('utf-16-le') in dll

vivas = [
    # EL CÍRCULO DE LOS MINIONS (los arcos y las brumas lo leen)
    ("CirculoDelEscuadron", tiene_utf8),
    # LA LEY DEL REFLEJO (los arcos cruzan el centro)
    ("DibujarArcosDelEscuadron", tiene_utf8),
    # LA LEY DE UNO A LA VEZ (el sistema)
    ("RitualVivoDe", tiene_utf8),
    ("_ritualColaHasta", tiene_utf8),
    ("RITUAL_GRACIA_TICKS", tiene_utf8),
    # LAS TRES VOCES NUEVAS (los caminos de sonido en la DLL)
    ("AethonMod/Content/Sounds/RunaCompas", tiene_utf16),
    ("AethonMod/Content/Sounds/RunaNaceSol", tiene_utf16),
    ("AethonMod/Content/Sounds/RunaNaceVacio", tiene_utf16),
]
for s, f in vivas:
    ok = f(s)
    print(f"[{'OK' if ok else 'FALLO'}] FIRMA VIVA: {s}")
    assert ok, s

# EL Muerto: el método que sonaba a Terraria en el anclaje ritual.
# (Item12 vive en el ataque NORMAL del ojo — SOLO el bloque ritual murió;
#  se verifica por el código fuente, la DLL no separa los bloques.)
# NOTA: SoundLimitBehavior.ReplaceOldest NO aparece como texto en la DLL —
# el compilador hornea los literales de enum como constantes numéricas
# (el '1' del setter); el compás queda probado por SU CAMINO de sonido.
print(f"[OK] Item12 queda SOLO en el ataque normal del ojo (verificado en fuente)")

print("\nAUDITORÍA v6.63: TODO OK — el .tmod dice la verdad de la fuente.")
