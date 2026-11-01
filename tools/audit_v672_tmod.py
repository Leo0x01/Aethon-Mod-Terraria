#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.72 — AUDITORÍA DEL .tmod: la casa no firma lo que no leyó.
  (1) tabla + EOF exacto (blobs íntegros, zlib OK, cuenta viva)
  (2) LOS SONIDOS intactos (RunaCarga + los nacimientos + el RC2) y
      RunaCompas SEGUIR ausente (murió en la v6.64)
  (3) LA TEXTURA DEL CIELO: OjoDelCieloItem.rawimg DENTRO
  (4) LA DLL EXTRAÍDA: TODO lo vivo de la v6.71 SIGUE VIVO (los rayos
      perlin resucitados + el libro reza muerto + los anillos) y los
      muertos AUSENTES — más LA FIRMA v6.72 EN EL PDB: «anclaViva», el
      corazón del cure del SOSPECHOSO B (los micro-arcos del arma-C se
      mudan al corazón del círculo mientras la Ira vive) — los nombres
      de las locales viven en el PDB empaquetado
  (5) EL HJSON: los TRES espejos byte-idénticos entre sí
  (6) EL CURE EN LA FUENTE: el bloque v6.72 presente en el .cs del repo
      Y en el espejo de ModSources (cadena de custodia del build)
"""
import sys, zlib, hashlib, os

sys.path.insert(0, '/home/z/.tmp-ml')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
REPO = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"
MSRC = "/home/z/.local/share/Terraria/tModLoader/ModSources/AethonMod"

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize}")
assert str(ver) == "6.72", f"versión {ver} != 6.72"

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

# (4) LA DLL — firmas UTF-16 (lo vivo de la v6.71 ENTERO)
dll = files.get(".dll", None) or files.get("AethonMod.dll", None)
assert dll, "sin dll"
def tiene(buf, s):
    return (s.encode('utf-16-le') + b'\x00\x00') in buf or s.encode('utf-8') in buf

vivas = ["DispararRayoRitual", "_rayosLanzados", "RayitosDelCorazon", "RitualDe",
         "CanUseItem", "SigiloErosionado", "CirculoRunico", "AnilloRunico",
         "SlotDelRitual", "CirculoDelEscuadron"]
for s in vivas:
    ok = tiene(dll, s)
    print(f"[{'OK' if ok else 'FALLO'}] firma VIVA: {s}")
    assert ok, f"falta {s} en la DLL"
muertas = ["enRitualIra", "MicroPoder", "DanoEscuadronMult", "TonosDeIris"]
for s in muertas:
    mal = tiene(dll, s)
    print(f"[{'OK' if not mal else 'FALLO'}] muerto AUSENTE: {s}")
    assert not mal, f"{s} resucitó"

# (4.b) LA FIRMA v6.72 EN EL PDB — «anclaViva», la local del cure
pdb = files.get("AethonMod.pdb")
assert pdb, "sin pdb empaquetado"
ok72 = tiene(pdb, "anclaViva")
print(f"[{'OK' if ok72 else 'FALLO'}] PDB: la local «anclaViva» DEL CURE v6.72 DENTRO")
assert ok72, "falta anclaViva en el PDB — el cure no viajó en la DLL"

# (5) EL HJSON ×3: cada espejo del .tmod == SU fuente del repo
hj = sorted(p for p in files if p.endswith("Mods.AethonMod.hjson") and "/Escriba/" not in p)
assert len(hj) == 3, f"esperaba 3 espejos hjson (sin Escriba), hay {len(hj)}"
for h in hj:
    nombre = os.path.basename(h)
    fuente = os.path.join(REPO, "Localization", nombre)
    assert os.path.exists(fuente), f"falta la fuente {nombre}"
    assert files[h] == open(fuente, 'rb').read(), f"{nombre} difiere de su fuente"
    print(f"[OK] {nombre} == fuente del repo ({len(files[h])} bytes)")

# (6) EL CURE EN LA FUENTE — cadena de custodia repo == ModSources == bloque v6.72
marca = "v6.72 — LA BOCA DEL ARMA-C, BIEN UBICADA"
for raiz, etiqueta in ((REPO, "repo"), (MSRC, "ModSources")):
    cs = os.path.join(raiz, "Content/Projectiles/Definitivas/RayosPruebaProjectile.cs")
    txt = open(cs, encoding='utf-8').read()
    ok = marca in txt and "anclaViva" in txt and "RitualDe((int)Owner)" in txt
    print(f"[{'OK' if ok else 'FALLO'}] {etiqueta}: el bloque v6.72 (marca + anclaViva + RitualDe) en RayosPruebaProjectile.cs")
    assert ok, f"el cure v6.72 no está en {etiqueta}"

# resumen md5
md5 = hashlib.md5(d).hexdigest()
print(f"\nmd5 {md5} | {fsize} B | v{ver}")
print("AUDITORÍA v6.72: TODO OK")
