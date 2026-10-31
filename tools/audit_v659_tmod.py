#!/usr/bin/env python3
"""v6.59 — LA AUDITORÍA DEL .tmod: LA IRA DE UN SER EXTERIOR.
Verifica: versión 6.59.0, EOF exacto, blobs sanos, los archivos NUEVOS
dentro (IraSerExteriorCeremonia, HazSobreElMundoSistema), los muertos
AUSENTES (RayoPruebaA/B cs+png, RayosPruebaEncimaSistema), la DLL con
las firmas vivas de la Ira, paridad hjson de espejos y el loop ogg."""
import os
import sys
import hashlib

sys.path.insert(0, "/tmp")
from parse_tmod import parse  # noqa: E402

TMOD = os.path.expanduser("~/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod")
errores = []

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
d = open(TMOD, "rb").read()
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | blobs {tend} | archivo {fsize}")
md5 = hashlib.md5(d).hexdigest()
print(f"md5: {md5} · {fsize} bytes")

if ver != "6.59.0":
    errores.append(f"versión del paquete: {ver} != 6.59.0")

total_comp = sum(c for _, _, c in entries)
if tend + total_comp != fsize:
    errores.append(f"EOF: {tend} + {total_comp} = {tend + total_comp} != {fsize}")
else:
    print(f"EOF EXACTO: {tend} + {total_comp} = {fsize} OK")

off = tend
blobs = {}
malos = 0
for (ruta, usize, csize) in entries:
    try:
        raw = d[off:off + csize] if csize == usize else __import__("zlib").decompress(d[off:off + csize], -15)
        if len(raw) != usize:
            errores.append(f"{ruta}: inflado {len(raw)} != {usize}")
            malos += 1
        blobs[ruta] = raw
    except Exception as e:
        errores.append(f"{ruta}: {e}")
        malos += 1
    off += csize
print(f"blobs: {len(blobs) - malos}/{len(entries)} OK" + (f" · MALOS: {malos}" if malos else ""))

nombres = set(blobs.keys())

# LOS NUEVOS DENTRO — los .cs NO viajan en el paquete (viajan COMPILADOS
# en la DLL): lo que viaja es la DLL con sus firmas (verificado abajo) y
# los assets. Aquí se confirma que el paquete tiene la DLL y los hjson.
if "AethonMod.dll" not in nombres:
    errores.append("AethonMod.dll ausente del paquete")
hjson_paq = [n for n in nombres if n.endswith(".hjson")]
print(f"DLL en paquete: {'AethonMod.dll' in nombres} · hjson en paquete: {len(hjson_paq)}")

# LOS MUERTOS AUSENTES (los .cs viajan por buildIgnore NO... ojo: los .cs NO
# viajan — la DLL sí; los PNG de iconos sí viajaban)
muertos_paquete = [n for n in nombres if "RayoPruebaA" in n or "RayoPruebaB" in n
                   or "RayosPruebaEncimaSistema" in n]
if muertos_paquete:
    errores.append(f"MUERTOS presentes en el paquete: {muertos_paquete}")
print("muertos del paquete AUSENTES:", not muertos_paquete)

# LAS FIRMAS VIVAS EN LA DLL
dll = blobs.get("AethonMod.dll", b"")
if not dll.startswith(b"MZ"):
    errores.append("AethonMod.dll no empieza con MZ")
firmas_vivas = [
    # el núcleo de la .59 — LA IRA
    "IraSerExteriorCeremoniaProjectile", "HazSobreElMundoSistema",
    "DibujarRitual", "RitualDe", "LanzarElHaz", "DanoDeLaIra",
    "DispararLaIra", "BuscarBlancoIra", "IraDisponible",
    "EsIra", "BlancoIra", "CentroIra",
    # las curas de visibilidad
    "colorCorazon", "CorazonRunico",
    # lo vivo de la .58 (intacto)
    "RayosPruebaProjectile", "RayoPruebaC", "RayoConcentradoDosProjectile",
    "TiemposLib", "ImpactoLib", "GolpeDeCine", "SolesRunicosCarga",
    "DibujarAtaque", "CapaRampa", "ColorConjuro",
]
faltan = [f for f in firmas_vivas if f.encode() not in dll]
if faltan:
    errores.append(f"DLL sin las firmas VIVAS: {faltan}")
print(f"DLL: {len(dll)} B · firmas vivas {'TODAS PRESENTES' if not faltan else '❌ FALTAN: ' + str(faltan)}")

# LOS MUERTOS DE LA DLL
firmas_muertas = [b"RayoPruebaA", b"RayoPruebaB", b"RayosPruebaEncimaSistema"]
muertos_dll = [f.decode() for f in firmas_muertas if f in dll]
if muertos_dll:
    errores.append(f"DLL con MUERTOS presentes: {muertos_dll}")
print("DLL muertos AUSENTES:", not muertos_dll)

# PARIDAD HJSON (los espejos deben ser IDÉNTICOS en CONTENIDO — la única
# diferencia permitida es el comentario de cabecera que explica el rol de
# cada espejo, por diseño de la casa)
import re
loc = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/"
def cuerpo(p):
    s = open(p, "r", encoding="utf-8").read()
    # el contenido real empieza en la primera clave
    i = s.find("Mods.AethonMod.DisplayName")
    return s[i:] if i >= 0 else s
es = cuerpo(loc + "es-ES_Mods.AethonMod.hjson")
mx = cuerpo(loc + "es-MX_Mods.AethonMod.hjson")
en = cuerpo(loc + "en-US_Mods.AethonMod.hjson")
if not (es == mx == en):
    errores.append("hjson: los espejos NO son idénticos en contenido")
else:
    print(f"hjson espejos IDÉNTICOS en contenido ({len(es)} chars) OK")

# EL LOOP OGG INTACTO (byte a byte contra el fuente)
ogg_fuente = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Sounds/RayoConcentradoLoop.ogg"
if os.path.exists(ogg_fuente):
    ogg = open(ogg_fuente, "rb").read()
    ok = any(blobs.get(n, b"") == ogg for n in nombres if n.endswith(".ogg"))
    print("loop ogg intacto:", ok)
    if not ok:
        errores.append("loop ogg NO coincide byte a byte")

# RESUMEN
print()
if errores:
    print(f"❌ {len(errores)} ERRORES:")
    for e in errores:
        print("  -", e)
    sys.exit(1)
print("✅ AUDITORÍA v6.59 LIMPIA — TODO VERIFICADO")
