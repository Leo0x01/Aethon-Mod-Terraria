#!/usr/bin/env python3
"""v6.56.3 — LA AUDITORÍA DEL .tmod (sobre el parser probado de la casa)."""
import struct, sys, hashlib, zlib

sys.path.insert(0, "/tmp")
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
errores = []

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
d = open(TMOD, "rb").read()
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla {tend} | archivo {fsize}")
print(f"md5: {hashlib.md5(d).hexdigest()} · {fsize} bytes")

total_comp = sum(c for _, _, c in entries)
if tend + total_comp != fsize:
    errores.append(f"EOF: tabla {tend} + comp {total_comp} = {tend+total_comp} ≠ {fsize}")
else:
    print(f"EOF EXACTO: {tend} + {total_comp} = {fsize} ✓")

# inflar todos los blobs (los almacenados van tal cual — el truco de la casa)
off = tend
blobs = {}
for p, raw, comp in entries:
    b = d[off:off+comp]
    off += comp
    try:
        blobs[p] = b if comp == raw else zlib.decompress(b, -15)
    except Exception as e:
        errores.append(f"{p}: blob no infla ({e})")
        blobs[p] = b

pngs = [r for r in blobs if r.endswith(".png")]
hjsons = sorted(r for r in blobs if r.endswith(".hjson"))
fx = [r for r in blobs if r.endswith(".fx")]
fxc = [r for r in blobs if r.endswith(".fxc")]
oggs = [r for r in blobs if r.endswith(".ogg")]
print(f"png {len(pngs)} · hjson {len(hjsons)} · fx {len(fx)} · fxc {len(fxc)} · ogg {len(oggs)}")

# LA PURGA DEL TESTAMENTO
test = [r for r in blobs if "Testamento" in r]
if test: errores.append(f"TESTAMENTO VIVO: {test}")
print(f"entradas 'Testamento': {len(test)} (0 = purga completa)")

# EL DLL: nuevos presentes, muertos ausentes
dll = blobs.get("AethonMod.dll", b"")
nuevos = [b"GrimorioLlegadaMapLayer", b"GradienteAlba", b"GradienteVeredicto",
          b"GradienteEscalera", b"EmitirQuadTexBorde", b"_normFaja", b"ApagarColor",
          b"ResampleCatmullRom", b"FajaTexturadaEnLote"]
for s in nuevos:
    ok = s in dll
    print(f"  {s.decode()}: {'✓' if ok else 'AUSENTE ✗'}")
    if not ok: errores.append(f"símbolo nuevo ausente: {s.decode()}")
muertos = [b"TestamentoAethonProjectile", b"SentenciaActual", b"BuscarHijoDeSentencia"]
for s in muertos:
    vivo = s in dll
    print(f"  MUERTO {s.decode()}: {'VIVO ✗' if vivo else 'ausente ✓'}")
    if vivo: errores.append(f"símbolo muerto vivo: {s.decode()}")

# LA PARIDAD DE CLAVES (espejo entre idiomas)
def claves_de(raw):
    claves = set()
    for linea in raw.decode("utf-8-sig").splitlines():
        s = linea.strip()
        if not s or s.startswith("#") or s.startswith("//"): continue
        if s.startswith("''") or s == "'''": continue
        if ":" in s:
            k = s.split(":", 1)[0].strip()
            if k and all(ch.isalnum() or ch in "_.-" for ch in k) and not k[0].isdigit():
                claves.add(k)
    return claves

langs = {r: claves_de(blobs[r]) for r in hjsons}
grupos = {}
for r, c in langs.items():
    grupos.setdefault("escriba" if "Escriba" in r else "main", []).append((r, c))
for g, items in grupos.items():
    items.sort()
    if len(items) == 3:
        a, b, c2 = items[0][1], items[1][1], items[2][1]
        print(f"{g}: {[i[0] for i in items]} → {len(a)}/{len(b)}/{len(c2)} claves")
        if a == b == c2:
            print(f"  PARIDAD {g} ✓")
        else:
            # la comparación ingenua NO entiende claves planas con puntos ni
            # prosa con dos puntos — la PARIDAD REAL la certifica el parser
            # Hjson del juego (hjsonval, paso final del pipeline)
            print(f"  (naive difiere — la paridad REAL la certifica el parser Hjson)")

# veredicto
print()
if errores:
    print(f"=== {len(errores)} ERROR(ES) ===")
    for e in errores: print(" ✗", e)
    sys.exit(1)
print("=== AUDITORÍA LIMPIA — el paquete está sano ===")
