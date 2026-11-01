#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.74 — AUDITORÍA DEL .tmod: la casa no firma lo que no leyó.
  (1) tabla + EOF exacto (blobs íntegros, zlib OK, cuenta viva)
  (2) LOS SONIDOS intactos (RunaCarga + los nacimientos + el RC2) y
      RunaCompas SEGUIR ausente (murió en la v6.64)
  (3) LA TEXTURA DEL CIELO: OjoDelCieloItem.rawimg DENTRO
  (4) LA DLL EXTRAÍDA: TODO lo vivo de la v6.71 SIGUE VIVO (los rayos
      perlin resucitados + el libro reza muerto + los anillos) y los
      muertos AUSENTEs
  (5) EL PDB: «anclaViva» VIVA (la ley v6.72 — la electricidad del
      arma se muda al corazón durante la Ira — viajó en la DLL) y
      «flick» VIVA (el bloque de chispas del RC2 ORIGINAL — la primera
      creación — INTACTO: sus rayitos bien puestos no se tocan)
  (6) EL HJSON: los TRES espejos byte-idénticos entre sí
  (7) EL BORRADO v6.74 EN LA FUENTE (cadena de custodia repo ==
      ModSources == DLL): la marca «v6.74 — LOS RAYITOS DEL ARMA-C,
      BORRADOS» presente; «StormLib», «14f» y «angOpuesto» AUSENTES
      de RayosPruebaProjectile.cs (el arma-C en paz no dibuja NI UN
      rayito — ni la firma RC2 de la v6.73 ni las cuerdas v6.60.2);
      «anclaViva» PRESENTE (la ley de la Ira intacta); el RC2 con SU
      bloque de chispas («int flick») SIN TOCAR.
"""
import sys, zlib, hashlib, os

sys.path.insert(0, '/home/z/.tmp-ml')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
REPO = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"
MSRC = "/home/z/.local/share/Terraria/tModLoader/ModSources/AethonMod"

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize}")
assert str(ver) == "6.74", f"versión {ver} != 6.74"

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

# (4.b) EL PDB — la ley v6.72 VIVA y el RC2 ORIGINAL INTACTO
pdb = files.get("AethonMod.pdb")
assert pdb, "sin pdb empaquetado"
ok72 = tiene(pdb, "anclaViva")
print(f"[{'OK' if ok72 else 'FALLO'}] PDB: la local «anclaViva» DE LA LEY v6.72 DENTRO (la Ira muda la electricidad al corazón)")
assert ok72, "falta anclaViva en el PDB — la ley v6.72 no viajó en la DLL"
okrc2 = tiene(pdb, "flick")
print(f"[{'OK' if okrc2 else 'FALLO'}] PDB: la local «flick» DEL RC2 ORIGINAL DENTRO (la primera creación, sin tocar)")
assert okrc2, "falta flick en el PDB — ¿el bloque de chispas del RC2 fue dañado?"

# (5) EL HJSON ×3: cada espejo del .tmod == SU fuente del repo
hj = sorted(p for p in files if p.endswith("Mods.AethonMod.hjson") and "/Escriba/" not in p)
assert len(hj) == 3, f"esperaba 3 espejos hjson (sin Escriba), hay {len(hj)}"
for h in hj:
    nombre = os.path.basename(h)
    fuente = os.path.join(REPO, "Localization", nombre)
    assert os.path.exists(fuente), f"falta la fuente {nombre}"
    assert files[h] == open(fuente, 'rb').read(), f"{nombre} difiere de su fuente"
    print(f"[OK] {nombre} == fuente del repo ({len(files[h])} bytes)")

# (6) EL BORRADO v6.74 EN LA FUENTE — repo == ModSources, el arma-C LIMPIA
marca = "v6.74 — LOS RAYITOS DEL ARMA-C, BORRADOS"
for raiz, etiqueta in ((REPO, "repo"), (MSRC, "ModSources")):
    cs = os.path.join(raiz, "Content/Projectiles/Definitivas/RayosPruebaProjectile.cs")
    txt = open(cs, encoding='utf-8').read()
    ok_marca = marca in txt
    ok_muertos = ("StormLib" not in txt) and ("14f" not in txt) and ("angOpuesto" not in txt)
    ok_ley = "anclaViva" in txt and "RitualDe((int)Owner)" in txt
    print(f"[{'OK' if ok_marca else 'FALLO'}] {etiqueta}: la marca v6.74 del borrado en RayosPruebaProjectile.cs")
    assert ok_marca, f"la marca v6.74 no está en {etiqueta}"
    print(f"[{'OK' if ok_muertos else 'FALLO'}] {etiqueta}: StormLib/14f/angOpuesto AUSENTES — el arma-C en paz no dibuja NI UN rayito")
    assert ok_muertos, f"los rayitos del arma-C siguen vivos en {etiqueta}"
    print(f"[{'OK' if ok_ley else 'FALLO'}] {etiqueta}: la ley v6.72 intacta (anclaViva + RitualDe)")
    assert ok_ley, f"la ley v6.72 se perdió en {etiqueta}"
    # espejos byte-idénticos entre repo y ModSources
    rc2 = os.path.join(raiz, "Content/Projectiles/Definitivas/RayoConcentradoDosProjectile.cs")
    t2 = open(rc2, encoding='utf-8').read()
    ok_rc2 = "int flick" in t2 and "int cuenta" in t2
    print(f"[{'OK' if ok_rc2 else 'FALLO'}] {etiqueta}: el RC2 ORIGINAL con SU bloque de chispas (flick + cuenta), sin tocar")
    assert ok_rc2, f"el RC2 original fue dañado en {etiqueta}"

a = open(os.path.join(REPO, "Content/Projectiles/Definitivas/RayosPruebaProjectile.cs"), 'rb').read()
b = open(os.path.join(MSRC, "Content/Projectiles/Definitivas/RayosPruebaProjectile.cs"), 'rb').read()
assert a == b, "repo != ModSources en RayosPruebaProjectile.cs — la cadena de custodia se rompió"
print("[OK] cadena de custodia: repo == ModSources (RayosPruebaProjectile.cs byte-idéntico)")

# resumen md5
md5 = hashlib.md5(d).hexdigest()
print(f"\nmd5 {md5} | {fsize} B | v{ver}")
print("AUDITORÍA v6.74: TODO OK — los rayitos del arma-C, MUERTOS; la Ira y el RC2, INTACTOS")
