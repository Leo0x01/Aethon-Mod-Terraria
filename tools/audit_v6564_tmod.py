#!/usr/bin/env python3
"""v6.56.4 — LA AUDITORÍA DEL .tmod (sobre el parser de la casa, tools/parse_tmod.py).

La letra de esta versión: (1) las alas simétricas + flip direccional,
(2) los picos curados por el CAMINO GPU del RC2 (HazConcentradoLib),
(3) el RC2: aguja→destellos, círculo plano→portal de agujero negro,
franjas negras→lote aditivo, sonidos grave+carga, temblor,
(4) localización MONOLINGÜE: es-ES fuente + espejo es-MX, en-US retirado."""
import os
import sys
import hashlib
import zlib

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, "/tmp")
from parse_tmod import parse  # noqa: E402

TMOD = os.environ.get("TMOD", "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod")
errores = []

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
d = open(TMOD, "rb").read()
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | blobs {tend} | archivo {fsize}")
md5 = hashlib.md5(d).hexdigest()
print(f"md5: {md5} · {fsize} bytes")

if ver != "6.56.4":
    errores.append(f"versión del paquete: {ver} ≠ 6.56.4 (el build.txt no viajó)")

total_comp = sum(c for _, _, c in entries)
if tend + total_comp != fsize:
    errores.append(f"EOF: {tend} + {total_comp} = {tend + total_comp} ≠ {fsize}")
else:
    print(f"EOF EXACTO: {tend} + {total_comp} = {fsize} ✓")

# inflar todos los blobs (los almacenados van tal cual — el truco de la casa)
off = tend
blobs = {}
for p, raw, comp in entries:
    b = d[off:off + comp]
    off += comp
    try:
        blobs[p] = b if comp == raw else zlib.decompress(b, -15)
    except Exception as e:
        errores.append(f"{p}: blob no infla ({e})")
        blobs[p] = b

pngs = [r for r in blobs if r.endswith(".png")]
rawimgs = [r for r in blobs if r.endswith(".rawimg")]
hjsons = sorted(r for r in blobs if r.endswith(".hjson"))
fx = [r for r in blobs if r.endswith(".fx")]
fxc = [r for r in blobs if r.endswith(".fxc")]
oggs = sorted(r for r in blobs if r.endswith(".ogg"))
print(f"png {len(pngs)} · rawimg {len(rawimgs)} · hjson {len(hjsons)} · fx {len(fx)} · fxc {len(fxc)} · ogg {len(oggs)}")

# ---- (1) LA LOCALIZACIÓN MONOLINGÜE: en-US AUSENTE, es-ES == es-MX espejo
enus = [r for r in hjsons if "en-US_" in r]
if enus:
    errores.append(f"en-US VIVO: {enus}")
print(f"hjson en-US: {len(enus)} (0 = monolingüe ✓)")


def cuerpo_hjson(raw):
    lineas = raw.decode("utf-8-sig").splitlines()
    i = 0
    while i < len(lineas) and (lineas[i].startswith("//") or not lineas[i].strip()):
        i += 1
    return "\n".join(lineas[i:])


for paquete, prefijo in [("main", "Localization/"), ("escriba", "Localization/Escriba/")]:
    f = prefijo + "es-ES_Mods.AethonMod.hjson"
    m = prefijo + "es-MX_Mods.AethonMod.hjson"
    if f not in blobs or m not in blobs:
        errores.append(f"{paquete}: falta {f} o {m}")
        continue
    if cuerpo_hjson(blobs[f]) == cuerpo_hjson(blobs[m]):
        print(f"{paquete}: es-ES == es-MX (cuerpo idéntico) ✓")
    else:
        errores.append(f"{paquete}: el espejo es-MX NO coincide con el es-ES")

# ---- (2) EL DLL: los nuevos PRESENTES, los muertos AUSENTES
dll = blobs.get("AethonMod.dll", b"")
nuevos = [b"HazConcentradoLib", b"DestellosDeCarga", b"DestellosDeEstallido",
          b"AberracionCromatica", b"DiscoNegro", b"ObtenerShader"]
for s in nuevos:
    ok = s in dll
    print(f"  {s.decode()}: {'✓' if ok else 'AUSENTE ✗'}")
    if not ok:
        errores.append(f"simbolo nuevo ausente: {s.decode()}")
# la ruta del sonido de carga (UTF-16 en el heap de strings del DLL)
u16 = "RayoConcentradoCarga".encode("utf-16-le")
if u16 in dll:
    print("  RayoConcentradoCarga (ruta UTF-16 en DLL): ✓")
else:
    errores.append("la ruta RayoConcentradoCarga no vive en el DLL")
muertos = [b"HazFusionEspina", b"EmitirQuadTexBorde", b"ApagarColor",
           b"TestamentoAethonProjectile", b"SentenciaActual"]
# (ArraySemis NO está en la lista: el del Partidor murió, pero el del RC2
#  — la variante cero-GC de 3 args de su fallback ×3 — sigue VIVO por diseño.)
for s in muertos:
    vivo = s in dll
    print(f"  MUERTO {s.decode()}: {'VIVO ✗' if vivo else 'ausente ✓'}")
    if vivo:
        errores.append(f"simbolo muerto vivo: {s.decode()}")

# ---- (3) LOS SONIDOS: el loop + la carga
for o in ["Content/Sounds/RayoConcentradoLoop.ogg", "Content/Sounds/RayoConcentradoCarga.ogg"]:
    if o not in blobs:
        errores.append(f"falta {o}")
    else:
        print(f"  {o}: {len(blobs[o])} bytes ✓")
if not any(p.endswith("RayoConcentradoCarga.ogg") for p in oggs):
    errores.append("el one-shot de carga no está en los ogg")

# ---- (4) LA PURGA DEL TESTAMENTO (regresión)
test = [r for r in blobs if "Testamento" in r]
if test:
    errores.append(f"TESTAMENTO VIVO: {test}")
print(f"entradas 'Testamento': {len(test)} (0 = purga completa)")

# veredicto
print()
if errores:
    print(f"=== {len(errores)} ERROR(ES) ===")
    for e in errores:
        print(" ✗", e)
    sys.exit(1)
print("=== AUDITORÍA LIMPIA — el paquete está sano ===")
