#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.75 — AUDITORÍA DEL .tmod: la casa no firma lo que no leyó.
  (1) tabla + EOF exacto (blobs íntegros, zlib OK, cuenta viva)
  (2) LOS SONIDOS intactos y RunaCompas SEGUIR ausente (murió v6.64)
  (3) LA TEXTURA DEL CIELO: OjoDelCieloItem.rawimg DENTRO
  (4) LA DLL EXTRAÍDA — LOS RAYITOS DEL CORAZÓN, MUERTOS EN TODOS LOS
      HACES: la firma «RayitosDelCorazon» AUSENTE de la DLL y del PDB
      (el método murió con sus TRES llamadas: ceremonia · haz Ira ·
      arma-C durante la Ira) — y LO VIVO SIGUE VIVO: los rayos perlin
      (DispararRayoRitual + _rayosLanzados + TruenoPerlin), la
      ANIMACIÓN NUEVA de la oración (EstaRezando + FactoresDeOracion +
      _oleadaRitual + _rayoRitualVivo), el resto del repertorio.
  (5) LA ESCALERA DE LA CAZA v6.75: el Cultista en el 45 y EL SEÑOR
      DE LA LUNA EN EL 50 (fuente + espejo del testigo + bandera).
  (6) EL DAÑO INICIAL DEL GRIMORIO: Item.damage = 1 (el arma inicial).
  (7) EL HJSON: los TRES espejos byte-idénticos a sus fuentes.
  (8) CADENA DE CUSTODIA: repo == ModSources en los cinco archivos.
"""
import sys, zlib, hashlib, os

sys.path.insert(0, '/home/z/.tmp-ml')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
REPO = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"
MSRC = "/home/z/.local/share/Terraria/tModLoader/ModSources/AethonMod"

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize}")
assert str(ver) == "6.75", f"versión {ver} != 6.75"

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
    vorbis = files[k[0]][:4] == b'OggS'
    print(f"[{'OK' if vorbis else 'FALLO'}] {s} DENTRO ({len(files[k[0]])} bytes, {'vorbis' if vorbis else 'NO-vorbis?'})")
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
pdb = files.get("AethonMod.pdb")
assert pdb, "sin pdb empaquetado"
def tiene(buf, s):
    return (s.encode('utf-16-le') + b'\x00\x00') in buf or s.encode('utf-8') in buf

# LOS MUERTOS: la bola del corazón, muerta en todos los haces
muertas = ["RayitosDelCorazon"]
for s in muertas:
    for buf, etiqueta in ((dll, "DLL"), (pdb, "PDB")):
        mal = tiene(buf, s)
        print(f"[{'OK' if not mal else 'FALLO'}] {etiqueta}: «{s}» AUSENTE (la bola del corazón, muerta)")
        assert not mal, f"{s} resucitó en la {etiqueta}"

# LO VIVO: la creación del haz (perlin) + la animación de la oración + el repertorio
vivas = ["DispararRayoRitual", "_rayosLanzados", "TruenoPerlinProjectile",   # los rayos perlin (la creación, INTACTA)
         "EstaRezando", "FactoresDeOracion", "_oleadaRitual", "_rayoRitualVivo",  # v6.75 — la oración
         "RitualDe", "CanUseItem", "SigiloErosionado", "CirculoRunico", "AnilloRunico",
         "SlotDelRitual", "CirculoDelEscuadron"]
for s in vivas:
    ok = tiene(dll, s)
    print(f"[{'OK' if ok else 'FALLO'}] firma VIVA: {s}")
    assert ok, f"falta {s} en la DLL"
muertos_viejos = ["enRitualIra", "MicroPoder", "DanoEscuadronMult", "TonosDeIris"]
for s in muertos_viejos:
    mal = tiene(dll, s)
    print(f"[{'OK' if not mal else 'FALLO'}] muerto viejo AUSENTE: {s}")
    assert not mal, f"{s} resucitó"

# (5) LA ESCALERA DE LA CAZA v6.75 — en la fuente (repo == ModSources == lo compilado)
def lee(raiz, rel):
    return open(os.path.join(raiz, rel), encoding='utf-8').read()

for raiz, etiqueta in ((REPO, "repo"), (MSRC, "ModSources")):
    desp = lee(raiz, "Content/Systems/DespertarGrimorio.cs")
    ok_cima = ("JefeDelCamino(45, \"Mods.AethonMod.Despertar.JefeCultistaLunatico\"" in desp and
               "JefeDelCamino(50, \"Mods.AethonMod.Despertar.JefeSenorDeLaLuna\"" in desp and
               "50 => NPC.downedMoonlord" in desp)
    print(f"[{'OK' if ok_cima else 'FALLO'}] {etiqueta}: LA CIMA — Cultista 45 + SEÑOR DE LA LUNA 50 (escalera + bandera)")
    assert ok_cima, f"la cima de la caza no está en {etiqueta}"
    espejo = lee(raiz, "Content/Globals/CazaJefesGlobalNPC.cs")
    ok_espejo = "NPCID.MoonLordCore         => 50" in espejo and "NPCID.CultistBoss          => 45" in espejo
    print(f"[{'OK' if ok_espejo else 'FALLO'}] {etiqueta}: el ESPEJO del testigo (Cultista 45 · Luna 50)")
    assert ok_espejo, f"el espejo del testigo no está en {etiqueta}"

    # (6) EL DAÑO INICIAL
    grim = lee(raiz, "Content/Weapons/GrimoireEternal.cs")
    ok_dano = "Item.damage = 1;" in grim and "Item.damage = 11" not in grim
    print(f"[{'OK' if ok_dano else 'FALLO'}] {etiqueta}: el daño inicial del grimorio EN 1 (el arma inicial)")
    assert ok_dano, f"el daño inicial no está en 1 en {etiqueta}"

    # LOS RAYITOS DEL CORAZÓN, MUERTOS EN LAS FUENTES
    haz = lee(raiz, "Content/Projectiles/Definitivas/RayosPruebaProjectile.cs")
    cere = lee(raiz, "Content/Projectiles/Definitivas/IraSerExteriorCeremonia.cs")
    ok_muerte = ("RayitosDelCorazon(" not in haz and "RayitosDelCorazon(" not in cere and
                 "internal static void RayitosDelCorazon" not in cere)
    ok_perlin = ("DispararRayoRitual" in cere and "_rayosLanzados" in cere)
    print(f"[{'OK' if ok_muerte else 'FALLO'}] {etiqueta}: RayitosDelCorazon MUERTO en el haz Y la ceremonia (cero llamadas, cero método)")
    assert ok_muerte, f"la bola del corazón sigue viva en {etiqueta}"
    print(f"[{'OK' if ok_perlin else 'FALLO'}] {etiqueta}: los rayos PERLIN de la creación, VIVOS (DispararRayoRitual)")
    assert ok_perlin, f"¡los rayos perlin murieron en {etiqueta}!"

    # LA ANIMACIÓN DE LA ORACIÓN
    ojo = lee(raiz, "Content/Projectiles/OjoEternoMinion.cs")
    ok_rezo = ("EstaRezando" in ojo and "FactoresDeOracion" in ojo and
               "_oleadaRitual" in ojo and "_rayoRitualVivo" in ojo and
               "System.Collections.Generic" in ojo)
    print(f"[{'OK' if ok_rezo else 'FALLO'}] {etiqueta}: LA ANIMACIÓN DE LA ORACIÓN viva en el minion")
    assert ok_rezo, f"la animación de la oración no está en {etiqueta}"

# (7) EL HJSON ×3
hj = sorted(p for p in files if p.endswith("Mods.AethonMod.hjson") and "/Escriba/" not in p)
assert len(hj) == 3, f"esperaba 3 espejos hjson (sin Escriba), hay {len(hj)}"
for h in hj:
    nombre = os.path.basename(h)
    fuente = os.path.join(REPO, "Localization", nombre)
    assert os.path.exists(fuente), f"falta la fuente {nombre}"
    assert files[h] == open(fuente, 'rb').read(), f"{nombre} difiere de su fuente"
    print(f"[OK] {nombre} == fuente del repo ({len(files[h])} bytes)")

# (8) CADENA DE CUSTODIA — los archivos editados, byte-idénticos
editados = [
    "Content/Projectiles/Definitivas/RayosPruebaProjectile.cs",
    "Content/Projectiles/Definitivas/IraSerExteriorCeremonia.cs",
    "Content/Projectiles/OjoEternoMinion.cs",
    "Content/Systems/DespertarGrimorio.cs",
    "Content/Globals/CazaJefesGlobalNPC.cs",
    "Content/Weapons/GrimoireEternal.cs",
]
for rel in editados:
    a = open(os.path.join(REPO, rel), 'rb').read()
    b = open(os.path.join(MSRC, rel), 'rb').read()
    assert a == b, f"repo != ModSources en {rel}"
    print(f"[OK] cadena de custodia: {os.path.basename(rel)} byte-idéntico")

# resumen md5
md5 = hashlib.md5(d).hexdigest()
print(f"\nmd5 {md5} | {fsize} B | v{ver}")
print("AUDITORÍA v6.75: TODO OK — la bola del corazón MUERTA, la oración VIVA, la Luna EN LA CIMA, el daño EN 1")
