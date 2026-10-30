#!/usr/bin/env python3
"""v6.56.5 — LA AUDITORÍA DEL .tmod (sobre el parser de la casa, tools/parse_tmod.py).

La letra de esta versión: (1) EL PITIDO de la carga muere (seno puro de
1376 Hz → soplo de presión sin pitch, fade 60 ms), (2) LA AUDITORÍA DE LOS
4 AGENTES aplicada: temblor sostenido perceptible (6.5/9), GravLens sin
monopolio (vida 0.06), horizonte del portal a tamaño real (2.28×), la
carga muere con su dueño (CortarCarga), espina de la Ira/Partidor
invertida (el extremo del juicio brilla), el giro de runas espejado con
la paridad correcta + direction + gemelas compartiendo semilla/glifo,
(3) empaquetado: *.fx fuera del .tmod (peso muerto), description.txt ES."""
import os
import sys
import hashlib
import zlib

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, "/tmp")
from parse_tmod import parse  # noqa: E402

TMOD = os.environ.get("TMOD", "/tmp/tmlhome/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod")
errores = []

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
d = open(TMOD, "rb").read()
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | blobs {tend} | archivo {fsize}")
md5 = hashlib.md5(d).hexdigest()
print(f"md5: {md5} · {fsize} bytes")

if ver != "6.56.5":
    errores.append(f"versión del paquete: {ver} ≠ 6.56.5 (el build.txt no viajó)")

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

# ---- (1) EL buildIgnore *.fx: los .fx FUERA, los .fxc DENTRO (¡la crítica!)
if fx:
    errores.append(f"los .fx siguen DENTRO (buildIgnore no funcionó): {fx}")
else:
    print("los 4 .fx fuente: FUERA del paquete ✓ (16.5 KB de peso muerto menos)")
if len(fxc) != 4:
    errores.append(f"los .fxc compilados: {len(fxc)} ≠ 4 — ¡EL GLOB *.fx SE COMIÓ LOS SHADERS!"
                   " (revertir el buildIgnore y reconstruir)")
else:
    print(f"los 4 .fxc compilados: DENTRO ✓ ({', '.join(os.path.basename(r) for r in sorted(fxc))})")

# ---- (2) LA LOCALIZACIÓN MONOLINGÜE: en-US AUSENTE, es-ES == es-MX espejo
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
    # el comentario huérfano del Escriba murió en la fuente — que no viva en el espejo
    if b"ENGLISH (en-US)" in blobs[f]:
        errores.append("Escriba es-ES: el comentario huérfano '// ENGLISH (en-US).' sigue vivo")

# ---- (3) EL DLL: los nuevos PRESENTES
dll = blobs.get("AethonMod.dll", b"")
nuevos = [b"CortarCarga", b"_cargaSlot"]  # v6.56.5: la carga muere con su dueño
for s in nuevos:
    ok = s in dll
    print(f"  {s.decode()}: {'✓' if ok else 'AUSENTE ✗'}")
    if not ok:
        errores.append(f"simbolo nuevo ausente: {s.decode()}")
# los que SIGUEN vivos de la .56.4 (regresión del porte GPU)
sobrevivientes = [b"HazConcentradoLib", b"DestellosDeCarga", b"DestellosDeEstallido",
                  b"DiscoNegro", b"GravLens", b"Pantalla"]
for s in sobrevivientes:
    if s not in dll:
        errores.append(f"simbolo de la .56.4 desaparecido: {s.decode()}")
print(f"  los {len(sobrevivientes)} simbolos de la .56.4: todos presentes ✓"
      if not any(s not in dll for s in sobrevivientes) else "  (ver errores)")
muertos = [b"HazFusionEspina", b"EmitirQuadTexBorde", b"ApagarColor",
           b"TestamentoAethonProjectile", b"SentenciaActual"]
for s in muertos:
    vivo = s in dll
    print(f"  MUERTO {s.decode()}: {'VIVO ✗' if vivo else 'ausente ✓'}")
    if vivo:
        errores.append(f"simbolo muerto vivo: {s.decode()}")

# ---- (4) LOS SONIDOS: el loop INTACTO (byte a byte con la .56.4) + la carga nueva
LOOP_MD5_564 = "a645221b0cecc96e06a138227a933447"  # verificado contra la .56.4 publicada
for o in ["Content/Sounds/RayoConcentradoLoop.ogg", "Content/Sounds/RayoConcentradoCarga.ogg"]:
    if o not in blobs:
        errores.append(f"falta {o}")
        continue
    h = hashlib.md5(blobs[o]).hexdigest()
    if o.endswith("Loop.ogg"):
        ok = h == LOOP_MD5_564
        print(f"  {o}: {len(blobs[o])} bytes md5 {h[:8]}… {'== .56.4 INTACTO ✓' if ok else 'CAMBIÓ ✗'}")
        if not ok:
            errores.append("el loop v3 cambió — esta versión NO lo toca")
    else:
        print(f"  {o}: {len(blobs[o])} bytes md5 {h[:8]}… (la cura del pitido) ✓")
        if len(blobs[o]) == 32535:
            errores.append("la carga pesa igual que la .56.4 — ¿no se regeneró?")

# ---- (5) LA PURGA DEL TESTAMENTO (regresión)
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
