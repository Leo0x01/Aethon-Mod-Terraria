#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.76 — AUDITORÍA DEL .tmod: la casa no firma lo que no leyó.
  (1) tabla + EOF exacto (blobs íntegros, zlib OK, cuenta viva)
  (2) LOS SONIDOS intactos y RunaCompas SEGUIR ausente (murió v6.64)
  (3) LA TEXTURA DEL CIELO: OjoDelCieloItem.rawimg DENTRO
  (4) LA DLL EXTRAÍDA — LO VIVO SIGUE VIVO (la creación perlin, la
      oración, el repertorio) Y LA LÍNEA NUEVA DE LA CAZA VIVE:
      «DespertarReqJefe» + «ReqJefeCaido» (la presa del peldaño en la
      lista de requisitos) — la bola del corazón SIGUE MUERTA.
  (5) LA ESCALERA DE LA CAZA: Cultista 45 + SEÑOR DE LA LUNA 50.
  (6) EL DAÑO INICIAL = 7 — LA MEDIA DE LAS PRIMERAS ARMAS (wiki:
      espada corta de cobre 5 + arco de madera 4 + vara de chispas 14
      + flor de Abigail 6 = 29/4 = 7,25 ≈ 7).
  (7) v6.76 EN LAS FUENTES: EL VIAJE SUAVE (k 0,10, tope 9) + LA
      VUELTA LENTA CONSTANTE (omega 0,50 — muere la hélice del ramp) +
      MÁS BRUMA (34·prog + el VIENTRE ×0,36) + LA FUENTE DEL
      LANZAMIENTO (1700 + i·53) + LAS DOS ESFERAS DEL NACIMIENTO
      (ceremonia 6→15 · boca del haz rE 13).
  (8) EL HJSON: los TRES espejos byte-idénticos a sus fuentes.
  (9) CADENA DE CUSTODIA: repo == ModSources en los editados.
"""
import sys, zlib, hashlib, os

sys.path.insert(0, '/home/z/.tmp-ml')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
REPO = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"
MSRC = "/home/z/.local/share/Terraria/tModLoader/ModSources/AethonMod"

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize}")
assert str(ver) == "6.76", f"versión {ver} != 6.76"

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
    # v6.76: los NOMBRES DE MIEMBRO van al metadata en UTF-8 (\x00 terminado)
    # y los LITERALES DE STRING al stream #US en UTF-16-LE PURO (con byte
    # flag, SIN terminador null) — se buscan los TRES formatos.
    return ((s.encode('utf-16-le') + b'\x00\x00') in buf or
            s.encode('utf-16-le') in buf or
            s.encode('utf-8') in buf)

# LOS MUERTOS: la bola del corazón sigue muerta
muertas = ["RayitosDelCorazon"]
for s in muertas:
    for buf, etiqueta in ((dll, "DLL"), (pdb, "PDB")):
        mal = tiene(buf, s)
        print(f"[{'OK' if not mal else 'FALLO'}] {etiqueta}: «{s}» AUSENTE (la bola del corazón, muerta)")
        assert not mal, f"{s} resucitó en la {etiqueta}"

# LO VIVO: la creación del haz (perlin) + la oración + el repertorio
# + v6.76 — LA LÍNEA DE LA PRESA EN LA LISTA DE REQUISITOS
vivas = ["DispararRayoRitual", "_rayosLanzados", "TruenoPerlinProjectile",   # los rayos perlin (la creación, INTACTA)
         "EstaRezando", "FactoresDeOracion", "_oleadaRitual", "_rayoRitualVivo",  # la oración
         "RitualDe", "CanUseItem", "SigiloErosionado", "CirculoRunico", "AnilloRunico",
         "SlotDelRitual", "CirculoDelEscuadron",
         "DespertarReqJefe", "ReqJefeCaido", "ReqJefeViva"]                     # v6.76 — la presa del peldaño
for s in vivas:
    ok = tiene(dll, s)
    print(f"[{'OK' if ok else 'FALLO'}] firma VIVA: {s}")
    assert ok, f"falta {s} en la DLL"
muertos_viejos = ["enRitualIra", "MicroPoder", "DanoEscuadronMult", "TonosDeIris"]
for s in muertos_viejos:
    mal = tiene(dll, s)
    print(f"[{'OK' if not mal else 'FALLO'}] muerto viejo AUSENTE: {s}")
    assert not mal, f"{s} resucitó"

# (5) LA ESCALERA DE LA CAZA — la cima intacta
def lee(raiz, rel):
    return open(os.path.join(raiz, rel), encoding='utf-8').read()

for raiz, etiqueta in ((REPO, "repo"), (MSRC, "ModSources")):
    desp = lee(raiz, "Content/Systems/DespertarGrimorio.cs")
    ok_cima = ("JefeDelCamino(45, \"Mods.AethonMod.Despertar.JefeCultistaLunatico\"" in desp and
               "JefeDelCamino(50, \"Mods.AethonMod.Despertar.JefeSenorDeLaLuna\"" in desp and
               "50 => NPC.downedMoonlord" in desp)
    print(f"[{'OK' if ok_cima else 'FALLO'}] {etiqueta}: LA CIMA — Cultista 45 + SEÑOR DE LA LUNA 50 (escalera + bandera)")
    assert ok_cima, f"la cima de la caza no está en {etiqueta}"

    # (6) EL DAÑO INICIAL — LA MEDIA DE LAS PRIMERAS ARMAS
    grim = lee(raiz, "Content/Weapons/GrimoireEternal.cs")
    ok_dano = "Item.damage = 7;" in grim and "Item.damage = 1;" not in grim
    print(f"[{'OK' if ok_dano else 'FALLO'}] {etiqueta}: el daño inicial del grimorio EN 7 (la media de las primeras armas)")
    assert ok_dano, f"el daño inicial no está en 7 en {etiqueta}"

    # (7) v6.76 EN LAS FUENTES — LOS CINCO FRENTES
    ojo = lee(raiz, "Content/Projectiles/OjoEternoMinion.cs")
    ok_viaje = "Math.Min(dist * 0.10f, 9f)" in ojo and "kDirecto" not in ojo
    print(f"[{'OK' if ok_viaje else 'FALLO'}] {etiqueta}: EL VIAJE SUAVE vivo (k 0,10 · tope 9 — el teletransporte MUERTO)")
    assert ok_viaje, f"el viaje suave no está en {etiqueta}"
    ok_giro = "const float omega = 0.50f;" in ojo and "MathHelper.Lerp(0.30f, 1.05f" not in ojo
    print(f"[{'OK' if ok_giro else 'FALLO'}] {etiqueta}: LA VUELTA LENTA CONSTANTE (omega 0,50 — el ramp hélice MUERTO)")
    assert ok_giro, f"la vuelta lenta no está en {etiqueta}"
    ok_rezo = ("EstaRezando" in ojo and "FactoresDeOracion" in ojo and
               "_oleadaRitual" in ojo and "_rayoRitualVivo" in ojo)
    print(f"[{'OK' if ok_rezo else 'FALLO'}] {etiqueta}: LA ANIMACIÓN DE LA ORACIÓN sigue viva en el minion")
    assert ok_rezo, f"la oración murió en {etiqueta}"

    cere = lee(raiz, "Content/Projectiles/Definitivas/IraSerExteriorCeremonia.cs")
    ok_bruma = "(int)(34 * prog) + 16" in cere and "semilla + 57" in cere
    print(f"[{'OK' if ok_bruma else 'FALLO'}] {etiqueta}: MÁS BRUMA (16-50 puffs + EL VIENTRE ×0,36)")
    assert ok_bruma, f"la bruma nueva no está en {etiqueta}"
    ok_esfera_c = "MathHelper.Lerp(6f, 15f, prog * prog)" in cere
    print(f"[{'OK' if ok_esfera_c else 'FALLO'}] {etiqueta}: LA ESFERA DEL NACIMIENTO en la ceremonia (6→15 px, latiendo)")
    assert ok_esfera_c, f"la esfera de la ceremonia no está en {etiqueta}"

    haz = lee(raiz, "Content/Projectiles/Definitivas/RayosPruebaProjectile.cs")
    ok_fuente = "1700 + i * 53" in haz and "tFt" in haz
    print(f"[{'OK' if ok_fuente else 'FALLO'}] {etiqueta}: LA FUENTE DEL LANZAMIENTO (bruma que sube y CAE al centro)")
    assert ok_fuente, f"la fuente del lanzamiento no está en {etiqueta}"
    ok_esfera_h = "rE * 3.1f" in haz
    print(f"[{'OK' if ok_esfera_h else 'FALLO'}] {etiqueta}: LA ESFERA DEL NACIMIENTO en la boca del haz (TODO el disparo)")
    assert ok_esfera_h, f"la esfera del haz no está en {etiqueta}"
    ok_perlin = ("DispararRayoRitual" in lee(raiz, "Content/Projectiles/Definitivas/IraSerExteriorCeremonia.cs"))
    print(f"[{'OK' if ok_perlin else 'FALLO'}] {etiqueta}: los rayos PERLIN de la creación, VIVOS")
    assert ok_perlin, f"¡los rayos perlin murieron en {etiqueta}!"

    # LA PRESA DEL PELDAÑO — tooltip + ✓ + hjson
    ok_presa = "DespertarReqJefe" in grim
    print(f"[{'OK' if ok_presa else 'FALLO'}] {etiqueta}: LA PRESA DEL PELDAÑO en la lista de requisitos (tooltip)")
    assert ok_presa, f"la línea de la presa no está en {etiqueta}"
    gr = lee(raiz, "Content/Globals/DespertarRequisitosGlobalItem.cs")
    ok_check = '"DespertarReqJefe"' in gr
    print(f"[{'OK' if ok_check else 'FALLO'}] {etiqueta}: el ✓ latiente de la presa (PostDrawTooltipLine)")
    assert ok_check, f"el ✓ de la presa no está en {etiqueta}"
    for hjson in ["es-MX_Mods.AethonMod.hjson", "es-ES_Mods.AethonMod.hjson", "en-US_Mods.AethonMod.hjson"]:
        h = lee(raiz, "Localization/" + hjson)
        assert "ReqJefeCaido:" in h and "ReqJefeViva:" in h, f"faltan las claves de la presa en {hjson} ({etiqueta})"
    print(f"[OK] {etiqueta}: las claves ReqJefeCaido/ReqJefeViva en los TRES hjson")

# (8) EL HJSON ×3
hj = sorted(p for p in files if p.endswith("Mods.AethonMod.hjson") and "/Escriba/" not in p)
assert len(hj) == 3, f"esperaba 3 espejos hjson (sin Escriba), hay {len(hj)}"
for h in hj:
    nombre = os.path.basename(h)
    fuente = os.path.join(REPO, "Localization", nombre)
    assert os.path.exists(fuente), f"falta la fuente {nombre}"
    assert files[h] == open(fuente, 'rb').read(), f"{nombre} difiere de su fuente"
    print(f"[OK] {nombre} == fuente del repo ({len(files[h])} bytes)")

# (9) CADENA DE CUSTODIA — los archivos editados, byte-idénticos
editados = [
    "Content/Projectiles/Definitivas/RayosPruebaProjectile.cs",
    "Content/Projectiles/Definitivas/IraSerExteriorCeremonia.cs",
    "Content/Projectiles/OjoEternoMinion.cs",
    "Content/Weapons/GrimoireEternal.cs",
    "Content/Globals/DespertarRequisitosGlobalItem.cs",
    "Localization/es-MX_Mods.AethonMod.hjson",
    "Localization/es-ES_Mods.AethonMod.hjson",
    "Localization/en-US_Mods.AethonMod.hjson",
    "build.txt",
]
for rel in editados:
    a = open(os.path.join(REPO, rel), 'rb').read()
    b = open(os.path.join(MSRC, rel), 'rb').read()
    assert a == b, f"repo != ModSources en {rel}"
    print(f"[OK] cadena de custodia: {os.path.basename(rel)} byte-idéntico")

# resumen md5
md5 = hashlib.md5(d).hexdigest()
print(f"\nmd5 {md5} | {fsize} B | v{ver}")
print("AUDITORÍA v6.76: TODO OK — el VIAJE SUAVE, la VUELTA LENTA, la BRUMA GRUESA, la FUENTE y las ESFERAS vivas · la PRESA en la lista · el daño EN 7")
