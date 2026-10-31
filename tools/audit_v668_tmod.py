#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.68 — AUDITORÍA DEL .tmod: la casa no firma lo que no leyó.
  (1) tabla + EOF exacto (blobs íntegros, zlib OK, cuenta viva)
  (2) LOS SONIDOS intactos (RunaCarga + los nacimientos + el RC2 — la
      voz aprobada) y RunaCompas SEGUIR ausente (murió en la v6.64)
  (3) LA TEXTURA DEL CIELO: OjoDelCieloItem.rawimg DENTRO (la ventana de
      misiones del Ojo del Cielo SIGUE viva — la caza no se toca)
  (4) LA DLL EXTRAÍDA: la VUELTA A CASA — el nivel del libro VIVE de
      nuevo (MinionContactDamageMult · MinionSpeedMult ·
      MinionDetectionRange · MinionChainCount · SummonDamageMult ·
      NivelDelOjo) y LOS MUERTOS DE LA v6.67 AUSENTES (la separación
      completa + los micropoderes + los rayos de plegaria de la
      ceremonia + su escuadrón lanzador)
  (5) EL HJSON: las claves de la caza en los TRES espejos y las
      CieloMicro* AUSENTES (los micropoderes murieron)
"""
import sys, zlib, hashlib

sys.path.insert(0, '/home/z/.tmp-ml')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize}")
assert str(ver) == "6.68", f"versión {ver} != 6.68"

# (1) blobs íntegros (el parse ya probó el EOF; aquí cada blob, zlib OK)
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

# (2) LOS SONIDOS: la voz aprobada, intacta
dentro = ["Sounds/RunaCarga.ogg", "Sounds/RunaNaceSol.ogg", "Sounds/RunaNaceVacio.ogg",
          "Sounds/RayoConcentradoCarga.ogg", "Sounds/RayoConcentradoLoop.ogg"]
for s in dentro:
    k = [p for p in files if p.endswith(s)]
    assert k, f"falta {s}"
    n = len(files[k[0]])
    vorbis = files[k[0]][:4] == b'OggS'
    print(f"[{'OK' if vorbis else 'FALLO'}] {s} DENTRO ({n} bytes, {'vorbis' if vorbis else 'NO-vorbis?'})")
    assert vorbis
if any(p.endswith("Sounds/RunaCompas.ogg") for p in files):
    print("[FALLO] RunaCompas.ogg resucitó")
    sys.exit(1)
print("[OK] RunaCompas.ogg AUSENTE (la campana sigue muerta)")

# (3) LA TEXTURA DEL CIELO (la ventana de misiones SIGUE viva — la caza
#     no se toca en la v6.68: solo murió la separación y los micros)
k = [p for p in files if p.endswith("OjoDelCieloItem.rawimg")]
assert k, "falta OjoDelCieloItem.rawimg"
print(f"[OK] OjoDelCieloItem.rawimg DENTRO ({len(files[k[0]])} bytes)")

# (4) LA DLL: firmas vivas y los muertos ausentes
dll = files["AethonMod.dll"]

def tiene_utf8(s): return s.encode('utf-8') in dll
def tiene_utf16(s): return s.encode('utf-16-le') in dll

vivas = [
    # LA CAZA DEL DESPERTAR (v6.67 INTACTA — los jefes no se tocan)
    ("LosJefesDelDespertar", tiene_utf8),
    ("JefeYaCaido", tiene_utf8),
    ("BanderaDelMundo", tiene_utf8),
    ("CazaJefesPlayer", tiene_utf8),
    ("CazaJefesGlobalNPC", tiene_utf8),
    ("PuedePasarLaPuerta", tiene_utf8),
    ("PuertaDeCaza", tiene_utf16),
    # EL CIELO DEL DESPERTAR (la ventana + su ítem — intactos)
    ("CieloDespiertoUI", tiene_utf8),
    ("OjoDelCieloItem", tiene_utf8),
    ("CieloTitulo", tiene_utf16),
    ("EsNivelDeCaza", tiene_utf8),
    # v6.68 — LA VUELTA A CASA: el nivel del libro VIVE de nuevo
    ("MinionContactDamageMult", tiene_utf8),
    ("MinionSpeedMult", tiene_utf8),
    ("MinionDetectionRange", tiene_utf8),
    ("MinionChainCount", tiene_utf8),
    ("MinionHitCooldown", tiene_utf8),
    ("SummonDamageMult", tiene_utf8),
    ("SummonDamagePerLevel", tiene_utf8),
    ("NivelDelOjo", tiene_utf8),
    # LOS RAYITOS DEL CORAZÓN + EL ANCLA (v6.66/67 intactas)
    ("RayitosDelCorazon", tiene_utf8),
    ("CirculoDelEscuadron", tiene_utf8),
    ("SlotDelRitual", tiene_utf8),
    ("RitualVivoDe", tiene_utf8),
]
for s, f in vivas:
    ok = f(s)
    print(f"[{'OK' if ok else 'FALLO'}] FIRMA VIVA: {s}")
    assert ok, s

muertos = [
    # v6.68 — LA SEPARACIÓN MURIÓ ENTERA
    "DanoEscuadronMult",
    "ContactoEscuadronMult",
    "VelocidadEscuadronMult",
    "RangoEscuadron",
    "CadenaEscuadron",
    # v6.68 — LOS MICROPODERES MURIERON ENTEROS
    "MicroPoderDe",
    "ContarMicro",
    "MultVelaDelLibro",
    "TonosDeIris",
    "EsNivelLlano",
    # v6.68 — LOS RAYOS DE PLEGARIA MURIERON (los rayitos de la parte
    #         inferior derecha del círculo — «esos quitalos de ahi»)
    "DispararRayoRitual",
    "_rayosLanzados",
]
for s in muertos:
    if tiene_utf8(s):
        print(f"[FALLO] {s} vive en la DLL — tenía que estar AUSENTE")
        sys.exit(1)
    print(f"[OK] {s} AUSENTE (murió de raíz en la v6.68)")

# (5) EL HJSON: las claves de la caza en los TRES espejos y las
#     CieloMicro* AUSENTES (los micropoderes murieron)
claves_necesarias = [
    "JefeReyGelatina", "JefePlantera", "JefeSenorDeLaLuna", "PuertaDeCaza",
    "CieloTitulo", "CieloLaEscalera", "CieloSeExigeEn",
]
claves_muertas = ["CieloMicroOjoAgudo", "CieloMicroVelaViva", "CieloMicroIrisTono"]
hjsons = {}
for p in files:
    if p.endswith(".hjson") and "Localization" in p and "Escriba" not in p:
        hjsons[p] = files[p].decode("utf-8")
assert len(hjsons) == 3, f"esperaba 3 hjson, hay {len(hjsons)}"
for p, texto in sorted(hjsons.items()):
    for c in claves_necesarias:
        assert c + ":" in texto, f"{p}: falta la clave {c}"
    for c in claves_muertas:
        assert c + ":" not in texto, f"{p}: la clave muerta {c} resucitó"
    print(f"[OK] {p}: {len(claves_necesarias)} claves DENTRO + {len(claves_muertas)} muertas AUSENTES")
# los cuerpos de los tres espejos IDÉNTICOS (el invariante de la casa)
cuerpos = []
for p, texto in sorted(hjsons.items()):
    lineas = texto.split("\n")
    i = 0
    while i < len(lineas) and (lineas[i].startswith("//") or not lineas[i].strip()):
        i += 1
    cuerpos.append("\n".join(lineas[i:]))
assert cuerpos[0] == cuerpos[1] == cuerpos[2], "los espejos hjson NO son idénticos"
print("[OK] es-ES == es-MX == en-US (cuerpos byte-idénticos — el invariante)")

md5 = hashlib.md5(d).hexdigest()
print(f"md5 {md5} | {fsize} bytes")

print("\nAUDITORÍA v6.68: TODO OK — el .tmod dice la verdad de la fuente.")
