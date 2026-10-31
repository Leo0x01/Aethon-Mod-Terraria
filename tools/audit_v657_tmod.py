#!/usr/bin/env python3
"""v6.57 — LA AUDITORÍA DEL .tmod: LA VUELTA DEL HAZ C (siete capas +
aguja + estrangulación + ley del corazón). Verifica: versión 6.57.0,
EOF exacto, blobs sanos, los 3 iconos RayoPrueba DENTRO, muertos
AUSENTES, 0 .fx, la DLL con las firmas vivas (ColorAlma NUEVA), paridad
hjson de espejos y el loop ogg intacto."""
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

if ver != "6.57.0":
    errores.append(f"versión del paquete: {ver} != 6.57.0")

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
        if csize == usize:
            raw = d[off:off + csize]
        else:
            raw = zlib.decompress(d[off:off + csize], -15)
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

# LAS FIRMAS VIVAS EN LA DLL (ColorAlma = LA CAPA NUEVA v6.57)
dll = blobs.get("AethonMod.dll", b"")
if not dll.startswith(b"MZ"):
    errores.append("AethonMod.dll no empieza con MZ")
firmas_vivas = ["RayosPruebaProjectile", "RayoPruebaA", "RayoPruebaB", "RayoPruebaC",
                "TiemposLib", "TiemposSistema", "TiemposGlobalNPC", "ImpactoLib",
                "GolpeDeCine", "SolesRunicosCarga", "CapaRampa", "SolesDelConducto",
                "ColorBruma", "ColorNucleo", "ColorAlma", "AnchoCapa", "StripWidth"]
for f in firmas_vivas:
    if f.encode() not in dll:
        errores.append(f"DLL sin la firma VIVA: {f}")
print(f"DLL: {len(dll)} B · firmas vivas {'TODAS PRESENTES' if not any('VIVA' in e for e in errores) else '❌ FALTAN'}")

# LOS MUERTOS AUSENTES
firmas_muertas = [b"BeamLib", b"BoltRenderer", b"RegisterLayer", b"VoidQueen",
                  b"SeekArc", b"FractalBolt", b"ForkTree", b"EmberField",
                  b"TelegrafoAnillo", b"AbrirVeredicto", b"GradienteEscalera",
                  b"ArcoToca", b"DrawOrbitalSystem", b"BloomTriple", b"GhostTransforms",
                  b"EsAleta", b"PuntoImpacto", b"PresetGolpeSeco", b"ConCantidad",
                  b"AberracionCromatica"]
muertos_presentes = [f.decode() for f in firmas_muertas if f in dll]
if muertos_presentes:
    errores.append(f"DLL con MUERTOS presentes: {muertos_presentes}")
print(f"DLL muertos: {'AUSENTES ✓' if not muertos_presentes else '❌ ' + str(muertos_presentes)}")

# LOS 3 ICONOS DENTRO
for icono in ["Content/Weapons/Definitivas/RayoPruebaA.rawimg",
              "Content/Weapons/Definitivas/RayoPruebaB.rawimg",
              "Content/Weapons/Definitivas/RayoPruebaC.rawimg"]:
    if icono not in nombres:
        errores.append(f"falta el icono {icono}")
print(f"iconos RayoPrueba: {sum(1 for i in ['A','B','C'] if f'Content/Weapons/Definitivas/RayoPrueba{i}.rawimg' in nombres)}/3 DENTRO")

# .fx FUERA, .fxc DENTRO
fx = [n for n in nombres if n.endswith(".fx")]
fxc = [n for n in nombres if n.endswith(".fxc")]
if fx:
    errores.append(f".fx dentro del paquete: {fx}")
print(f".fx: {'NINGUNO ✓' if not fx else '❌ ' + str(fx)} · .fxc: {len(fxc)}")

# hjson: paridad de espejos
def cuerpo_sin_cabecera(ruta):
    raw = blobs.get(ruta)
    if raw is None:
        return None
    lineas = raw.decode("utf-8", "replace").splitlines()
    while lineas and (lineas[0].startswith("//") or not lineas[0].strip()):
        lineas.pop(0)
    return "\n".join(lineas)

es = cuerpo_sin_cabecera("Localization/es-ES_Mods.AethonMod.hjson")
mx = cuerpo_sin_cabecera("Localization/es-MX_Mods.AethonMod.hjson")
en = cuerpo_sin_cabecera("Localization/en-US_Mods.AethonMod.hjson")
if es is None or mx is None or en is None:
    errores.append("hjson ausente en el paquete")
else:
    if mx != es:
        errores.append("es-MX != es-ES (cuerpo)")
    if en != es:
        errores.append("en-US != es-ES (cuerpo)")
    if "RayoPruebaA" not in es:
        errores.append("es-ES sin RayoPruebaA")
    print(f"hjson: es-ES {len(es)} B · espejos {'IDÉNTICOS ✓' if mx == es and en == es else '❌ DIFIEREN'} · RayoPrueba {'DENTRO ✓' if 'RayoPruebaA' in es else '❌'}")

# el loop del sonido intacto
loop = blobs.get("Content/Sounds/RayoConcentradoLoop.ogg")
print(f"loop ogg: {len(loop)} B {'presente ✓' if loop else '❌'}")

print()
if errores:
    print(f"ERRORES ({len(errores)}):")
    for e in errores:
        print("  ·", e)
    sys.exit(1)
print(f"AUDITORÍA .tmod v6.57.0 LIMPIA — md5 {md5}")
