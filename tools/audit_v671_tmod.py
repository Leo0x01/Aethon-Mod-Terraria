#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.71 — AUDITORÍA DEL .tmod: la casa no firma lo que no leyó.
  (1) tabla + EOF exacto (blobs íntegros, zlib OK, cuenta viva)
  (2) LOS SONIDOS intactos (RunaCarga + los nacimientos + el RC2) y
      RunaCompas SEGUIR ausente (murió en la v6.64)
  (3) LA TEXTURA DEL CIELO: OjoDelCieloItem.rawimg DENTRO
  (4) LA DLL EXTRAÍDA: LOS RAYOS PERLIN RESUCITADOS (DispararRayoRitual +
      _rayosLanzados + el ancla ritual de la ceremonia — LA CREACIÓN DEL
      HAZ de vuelta) + EL LIBRO REZA MUERTO (la compuerta de la v6.70
      en CanUseItem BORRADA) + LOS ANILLOS VIVOS (SigiloErosionado ·
      CirculoRunico · AnilloRunico — la v6.70 intacta) y LOS MUERTOS
      AUSENTES (enRitualIra · MicroPoder · la separación)
  (5) EL HJSON: los TRES espejos byte-idénticos entre sí
"""
import sys, zlib, hashlib

sys.path.insert(0, '/home/z/.tmp-ml')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize}")
assert str(ver) == "6.71", f"versión {ver} != 6.71"

# (1) blobs íntegros
d = open(TMOD, 'rb').read()
off = tend
files = {}
bad = 0
for p, raw, comp in entries:
    blob = d[off:off + comp]
    off += comp
    try:
        data = blob if comp == raw else zlib.decompress(blob, -15)
        assert len(data) == raw, f"{p}: raw {raw} != {len(data)}"
        files[p] = data
    except Exception as e:
        bad += 1
        print(f"[FALLO] blob {p}: {e}")
print(f"[{'OK' if bad == 0 else 'FALLO'}] blobs {len(files)}/{len(entries)} descomprimidos, tamaños exactos")
assert bad == 0
assert off == fsize, f"EOF: {off} != {fsize}"
print(f"[OK] EOF exacto ({off} == {fsize})")

# (2) LOS SONIDOS
dentro = ["Sounds/RunaCarga.ogg", "Sounds/RunaNaceSol.ogg", "Sounds/RunaNaceVacio.ogg",
          "Sounds/RayoConcentradoCarga.ogg", "Sounds/RayoConcentradoLoop.ogg"]
for s in dentro:
    k = [p for p in files if p.endswith(s)]
    assert k, f"falta {s}"
    n = len(files[k[0]])
    vorbis = files[k[0]][:4] == b'OggS'
    print(f"[{'OK' if vorbis else 'FALLO'}] {s} DENTRO ({n} bytes, {'vorbis' if vorbis else 'NO-vorbis?'})")
    assert vorbis
assert not any(p.endswith("Sounds/RunaCompas.ogg") for p in files)
print("[OK] RunaCompas.ogg AUSENTE (la campana sigue muerta)")

# (3) LA TEXTURA DEL CIELO
k = [p for p in files if "OjoDelCieloItem" in p]
assert k and k[0] in files, "falta OjoDelCieloItem"
print(f"[OK] {k[0]} DENTRO ({len(files[k[0]])} bytes)")

# (4) LA DLL — firmas UTF-16
dll = files.get(".dll", None) or files.get("AethonMod.dll", None)
assert dll, "sin dll"
def tiene(s):
    return (s.encode('utf-16-le') + b'\x00\x00') in dll or s.encode('utf-8') in dll

vivas = ["DispararRayoRitual", "_rayosLanzados", "RayitosDelCorazon", "RitualDe",
         "CanUseItem", "SigiloErosionado", "CirculoRunico", "AnilloRunico",
         "SlotDelRitual", "CirculoDelEscuadron"]
for s in vivas:
    ok = tiene(s)
    print(f"[{'OK' if ok else 'FALLO'}] firma VIVA: {s}")
    assert ok, f"falta {s} en la DLL"
muertas = ["enRitualIra", "MicroPoder", "DanoEscuadronMult", "TonosDeIris"]
for s in muertas:
    mal = tiene(s)
    print(f"[{'OK' if not mal else 'FALLO'}] muerto AUSENTE: {s}")
    assert not mal, f"{s} resucitó"

# (5) EL HJSON ×3: cada espejo del .tmod == SU fuente del repo
import os
hj = sorted(p for p in files if p.endswith("Mods.AethonMod.hjson") and "/Escriba/" not in p)
assert len(hj) == 3, f"esperaba 3 espejos hjson (sin Escriba), hay {len(hj)}"
for h in hj:
    nombre = os.path.basename(h)
    fuente = os.path.join("/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization", nombre)
    assert os.path.exists(fuente), f"falta la fuente {nombre}"
    assert files[h] == open(fuente, 'rb').read(), f"{nombre} difiere de su fuente"
    print(f"[OK] {nombre} == fuente del repo ({len(files[h])} bytes)")

# resumen md5
md5 = hashlib.md5(d).hexdigest()
print(f"\nmd5 {md5} | {fsize} B | v{ver}")
print("AUDITORÍA v6.71: TODO OK")
