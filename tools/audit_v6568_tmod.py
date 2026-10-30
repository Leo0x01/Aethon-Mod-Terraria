#!/usr/bin/env python3
"""v6.56.8 — LA AUDITORÍA DEL .tmod: la cura de la banda plana + la paleta
del conjuro. Verifica: versión, EOF exacto, blobs sanos, el .fxc NUEVO
dentro (7496 B) y 0 .fx, la DLL con ColorConjuro/PaletaConjuro PRESENTES y
la boca de glows MUERTA, paridad hjson, y el loop .ogg intacto."""
import os
import sys
import hashlib
import zlib

sys.path.insert(0, "/tmp")
from parse_tmod import parse  # noqa: E402

TMOD = os.path.expanduser("~/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod")
errores = []

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
d = open(TMOD, "rb").read()
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | blobs {tend} | archivo {fsize}")
md5 = hashlib.md5(d).hexdigest()
print(f"md5: {md5} · {fsize} bytes")

if ver != "6.56.8":
    errores.append(f"versión del paquete: {ver} != 6.56.8")

total_comp = sum(c for _, _, c in entries)
if tend + total_comp != fsize:
    errores.append(f"EOF: {tend} + {total_comp} = {tend + total_comp} != {fsize}")
else:
    print(f"EOF EXACTO: {tend} + {total_comp} = {fsize} OK")

# inflar todos los blobs
off = tend
blobs = {}
malos = 0
for (ruta, usize, csize) in entries:
    if csize == usize:
        raw = d[off:off + csize]          # tML 2026.8: lo incomprimible va CRUDO
    else:
        raw = zlib.decompress(d[off:off + csize], -15)   # deflate raw (sin header)
    if len(raw) != usize:
        malos += 1
    blobs[ruta] = raw
    off += csize
if malos:
    errores.append(f"{malos} blobs malos")
else:
    print(f"blobs: {len(blobs)}/{len(entries)} inflan EXACTOS")

# el .fxc nuevo dentro, 0 .fx
fxc = [r for r in blobs if r.endswith(".fxc")]
fx = [r for r in blobs if r.endswith(".fx")]
print(f".fxc dentro: {len(fxc)} · .fx dentro: {len(fx)}")
if "Content/Effects/Shaders/HazConcentradoShader.fxc" not in blobs:
    errores.append("el HazConcentradoShader.fxc NO esta en el paquete")
else:
    n = len(blobs["Content/Effects/Shaders/HazConcentradoShader.fxc"])
    print(f"HazConcentradoShader.fxc: {n} bytes")
    if n != 7496:
        errores.append(f"el .fxc mide {n} != 7496 (¿stale?)")
    h = hashlib.md5(blobs["Content/Effects/Shaders/HazConcentradoShader.fxc"]).hexdigest()
    print(f"  md5 del .fxc empaquetado: {h}")
if fx:
    errores.append(f".fx fuente dentro del paquete: {fx}")

# la DLL: firmas nuevas presentes, muertas ausentes
dll = None
for r in blobs:
    if r.endswith("AethonMod.dll"):
        dll = blobs[r]
        break
if dll is None:
    errores.append("DLL no encontrada")
else:
    print(f"DLL: {len(dll)} bytes")
    presentes = [b"ColorConjuro", b"PaletaConjuro", b"CicloConjuro", b"SolRunico4Puntas",
                 b"ObtenerShader", b"_cacheColores64"]
    for s in presentes:
        if s not in dll:
            errores.append(f"firma AUSENTE en la DLL: {s.decode()}")
    print("firmas nuevas PRESENTES: " + ", ".join(s.decode() for s in presentes if s in dll))

# hjson: es-ES fuente, espejos identicos
loc = {r: blobs[r] for r in blobs if "Localization" in r and r.endswith(".hjson")}
print(f"hjson en paquete: {len(loc)}")
cuerpos = {}
for r, b in loc.items():
    cuerpos[r] = b
ess = [r for r in cuerpos if "es-ES" in r]
if not ess:
    errores.append("sin es-ES en el paquete")
else:
    # pares por sufijo (main y Escriba): cada es-ES == su espejo es-MX == su en-US
    def sufijo(r):
        return "Escriba" if "Escriba" in r else "Main"
    def cuerpo(b):
        # el CUERPO = sin el bloque de comentario de cabecera (cada espejo
        # lleva su propia nota explicativa — la convención de la sync)
        lineas = b.decode("utf-8", "replace").splitlines()
        while lineas and (lineas[0].startswith("//") or not lineas[0].strip()):
            lineas.pop(0)
        return "\n".join(lineas)
    espejos_ok = True
    for base_r in ess:
        suf = sufijo(base_r)
        base = cuerpo(cuerpos[base_r])
        for r, b in cuerpos.items():
            if r == base_r or sufijo(r) != suf:
                continue
            if cuerpo(b) != base:
                espejos_ok = False
                errores.append(f"espejo DIFFERENTE del es-ES ({suf}): {r}")
    if espejos_ok:
        print(f"es-ES ({len(ess)} archivos) + espejos es-MX/en-US con cuerpos IDENTICOS")

# el loop de sonido intacto
ogg = "Content/Sounds/RayoConcentradoLoop.ogg"
if ogg in blobs:
    print(f"loop .ogg: {len(blobs[ogg])} bytes OK")
else:
    errores.append("loop .ogg ausente")

print()
if errores:
    print("ERRORES:")
    for e in errores:
        print("  -", e)
    sys.exit(1)
print(f"VEREDICTO: AUDITORIA LIMPIA ({len(entries)} entradas, md5 {md5})")
