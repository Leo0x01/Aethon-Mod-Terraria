#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""audit_v6155_tmod.py — AUDITORÍA DEL .TMOD v6.155 (el patrón de la casa).

Con el parser canónico parse_tmod.py: identidad, EOF, blobs, las FIRMAS
VIVAS de las 8 curas en la DLL, las muertas ausentes, hjson ×3.
"""
import importlib.util
import os
import struct
import sys

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
TMOD = os.environ.get(
    "TMOD",
    os.path.expanduser("~/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"))

mp_spec = importlib.util.spec_from_file_location(
    "parse_tmod", os.path.join(REPO, "tools", "parse_tmod.py"))
mp = importlib.util.module_from_spec(mp_spec)
mp_spec.loader.exec_module(mp)

import zlib

d = open(TMOD, "rb").read()
tmlver, name, ver, entries, tend, fsize = mp.parse(TMOD)
print(f"(0) {name} v{ver} · tML {tmlver} · {len(entries)} entradas · {fsize:,} B")

bl, off = {}, tend
for path, raw, comp in entries:
    trozo = d[off:off + comp]
    blob = trozo if comp == raw else zlib.decompress(trozo, -15)
    bl[path] = blob
    off += comp
print(f"(1) EOF exacto: {'SI' if off == len(d) else 'NO'} ({off} vs {len(d)})")
print(f"(2) blobs: {len(bl)}/{len(entries)}")

dll = next(b for k, b in bl.items() if k.endswith(".dll"))
assert dll[:2] == b"MZ"

firmas = [
    "SistemaAnillos",       # cura 8: anillos de soles de verdad
    "NPCShopDatabase",      # cura 4: cosecha de tiendas ajenas
    "ActsLikeTownNPC",      # cura 4: filtro town NPC de la API real
    "AnillosSolRunico",     # cadena de la cura 8
    "PolvoVacioInvertido",  # cura 6: la mota suave de la bruma menuda
    "TravellingMerchant",   # cura 4: el ambulante cuenta como tienda
]
for f in firmas:
    ok = f.encode("utf-16-le") in dll or f.encode() in dll
    print(f"  firma {f}: {'VIVE' if ok else 'AUSENTE (MAL)'}")

patron20 = struct.pack("<f", 0.20)
print(f"  firma 0.20f (umbral hambre 20%): "
      f"{'VIVE' if patron20 in dll else 'AUSENTE (MAL)'}")

muertas = ["IsTownNPC", "GetNPCShopArray", "PolvoExpulsionJuicio"]
for m in muertas:
    ok = m.encode("utf-16-le") in dll or m.encode() in dll
    print(f"  muerta {m}: {'AUN VIVE (revisar)' if ok else 'muerta/ausente'}")

hjsons = [k for k in bl if k.endswith(".hjson")]
print(f"hjson: {len(hjsons)} {sorted(hjsons)}")
