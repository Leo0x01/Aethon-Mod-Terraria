#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.67 — AUDITORÍA DEL .tmod: la casa no firma lo que no leyó.
  (1) tabla + EOF exacto (blobs íntegros, zlib OK, cuenta viva)
  (2) LOS SONIDOS intactos (RunaCarga + los nacimientos + el RC2 — la
      voz aprobada) y RunaCompas SEGUIR ausente (murió en la v6.64)
  (3) LA TEXTURA DEL CIELO: OjoDelCieloItem.png DENTRO (el sprite de la
      vieja Ira del Cielo, resurrecto como la ventana de misiones)
  (4) LA DLL EXTRAÍDA: las firmas de la v6.67 VIVAS (la caza de jefes ·
      la ventana del Cielo · la separación del escuadrón · los
      micropoderes) y LOS MUERTOS ausentes (las funciones que el nivel
      del libro ya no alimenta)
  (5) EL HJSON: las claves nuevas de la caza y del cielo en los TRES
      espejos (es-ES == es-MX == en-US)
"""
import sys, zlib, hashlib

sys.path.insert(0, '/home/z/.tmp-ml')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize}")
assert str(ver) == "6.67", f"versión {ver} != 6.67"

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

# (3) LA TEXTURA DEL CIELO (la vieja Ira del Cielo, resurrecta — tML la
#     empaqueta como .rawimg: la textura convertida a datos crudos)
k = [p for p in files if p.endswith("OjoDelCieloItem.rawimg")]
assert k, "falta OjoDelCieloItem.rawimg (¡la textura del cielo!)"
print(f"[OK] OjoDelCieloItem.rawimg DENTRO ({len(files[k[0]])} bytes — la textura del cielo resurrecta)")

# (4) LA DLL: firmas vivas y los muertos ausentes
dll = files["AethonMod.dll"]

def tiene_utf8(s): return s.encode('utf-8') in dll
def tiene_utf16(s): return s.encode('utf-16-le') in dll

vivas = [
    # LA CAZA DEL DESPERTAR (v6.67 — los jefes de Terraria en la escalera)
    ("LosJefesDelDespertar", tiene_utf8),
    ("JefeYaCaido", tiene_utf8),
    ("BanderaDelMundo", tiene_utf8),
    ("CazaJefesPlayer", tiene_utf8),
    ("CazaJefesGlobalNPC", tiene_utf8),
    ("PuedePasarLaPuerta", tiene_utf8),
    ("PuertaDeCaza", tiene_utf16),
    # EL CIELO DEL DESPERTAR (la ventana de misiones + su ítem)
    ("CieloDespiertoUI", tiene_utf8),
    ("OjoDelCieloItem", tiene_utf8),
    ("CieloTitulo", tiene_utf16),
    # LA SEPARACIÓN DEL ESCUADRÓN (los buffs nacen del despertar)
    ("DanoEscuadronMult", tiene_utf8),
    ("ContactoEscuadronMult", tiene_utf8),
    ("VelocidadEscuadronMult", tiene_utf8),
    ("RangoEscuadron", tiene_utf8),
    ("CadenaEscuadron", tiene_utf8),
    # LOS MICROPODERES (los niveles llanos ya no son mudos)
    ("MicroPoderDe", tiene_utf8),
    ("MultVelaDelLibro", tiene_utf8),
    ("TonosDeIris", tiene_utf8),
    # LOS RAYITOS DEL CORAZÓN + EL ANCLA (v6.66 intacta)
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
    # LA SEPARACIÓN: las funciones del nivel del libro MURIERON
    "MinionContactDamageMult",
    "MinionSpeedMult",
    "MinionDetectionRange",
    "MinionChainCount",
    "MinionHitCooldown",
    "SummonDamageMult",
    "SummonDamageBonus",
    "SummonDamagePerLevel",
]
for s in muertos:
    if tiene_utf8(s):
        print(f"[FALLO] {s} vive en la DLL — tenía que estar AUSENTE")
        sys.exit(1)
    print(f"[OK] {s} AUSENTE (el nivel del libro ya no alimenta al escuadrón)")

# (5) EL HJSON: las claves nuevas en los TRES espejos
claves_necesarias = [
    "JefeReyGelatina", "JefePlantera", "JefeSenorDeLaLuna", "PuertaDeCaza",
    "CieloTitulo", "CieloLaEscalera", "CieloMicroIrisTono", "CieloSeExigeEn",
]
hjsons = {}
for p in files:
    if p.endswith(".hjson") and "Localization" in p and "Escriba" not in p:
        hjsons[p] = files[p].decode("utf-8")
assert len(hjsons) == 3, f"esperaba 3 hjson, hay {len(hjsons)}"
for p, texto in sorted(hjsons.items()):
    for c in claves_necesarias:
        assert c + ":" in texto, f"{p}: falta la clave {c}"
    print(f"[OK] {p}: las {len(claves_necesarias)} claves nuevas DENTRO")
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

print("\nAUDITORÍA v6.67: TODO OK — el .tmod dice la verdad de la fuente.")
