#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.83 — AUDITORÍA DE ESTADO (post-recuperación de contexto): verifica que
el .tmod instalado es EXACTAMENTE la v6.83 publicada y que todo el repertorio
de las creaciones v6.82 vive dentro (Absoluto, clickers, púlsar, anillo Bruma,
el calor completo). Rápida y contundente."""
import sys, zlib, os, subprocess

sys.path.insert(0, '/home/z/.tmp-ml')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"

tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize}")
assert str(ver) == "6.83", f"versión {ver} != 6.83"
assert len(entries) == 466, f"esperaba 466 entradas, hay {len(entries)}"

# blobs íntegros + mapa de archivos
d = open(TMOD, 'rb').read()
off = tend
files = {}
bad = 0
for p, raw, comp in entries:
    blob = d[off:off+comp]
    try:
        data = blob if comp == raw else zlib.decompress(blob, -15)
        assert len(data) == raw, f"tamaño {p}: {len(data)} != {raw}"
        files[p] = data
    except Exception as e:
        bad += 1
        print(f"  BLOB ROTO: {p}: {e}")
    off += comp
assert off == fsize, f"EOF: {off} != {fsize}"
print(f"(1) blobs {len(files)}/{len(entries)} íntegros · EOF exacto ({off} == {fsize})")
assert bad == 0

# (2) el repertorio vivo: ASSETS clave de v6.82/83 DENTRO del .tmod
esperados = [
    "Content/Effects/Shaders/CalorDistorsionShader.fxc",
    "Content/Effects/Procedural/RuidoPlasmaCalor.rawimg",
]
faltan = [e for e in esperados if e not in files]
if faltan:
    print("  FALTAN:", faltan); sys.exit(1)
print(f"(2) los assets del calor DENTRO ({len(esperados)}/{len(esperados)})")

# (2.b) las FUENTES de las creaciones v6.82 en el REPO
REPO = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"
fuentes = [
    "Content/Projectiles/Cosmic/AgujeroAbsolutoBlackHoleProjectile.cs",
    "Content/Weapons/Cosmic/AgujeroAbsolutoStaff.cs",
    "Content/Weapons/Clicker/ClickerCasa.cs",
    "Content/Weapons/Clicker/CompasRunico.cs",
    "Content/Weapons/Clicker/FuriaSolarClicker.cs",
    "Content/Projectiles/Clicker/ClickerGolpeProjectile.cs",
    "Content/Projectiles/Cosmic/RequiemPulsarProjectile.cs",
    "Content/Weapons/Cosmic/RequiemDelPulsarStaff.cs",
]
fsrc = [f for f in fuentes if not os.path.exists(os.path.join(REPO, f))]
if fsrc:
    print("  FUENTES QUE FALTAN EN EL REPO:", fsrc); sys.exit(1)
print(f"(2.b) las 8 fuentes de las creaciones v6.82 en el repo ✓")

fxc = files["Content/Effects/Shaders/CalorDistorsionShader.fxc"]
print(f"(3) el .fxc del calor: {len(fxc)} B (v6.82 reconstruido = 5250)")
assert len(fxc) == 5250, "el .fxc NO es el reconstruido de v6.82"

# (4) la DLL — firmas VIVAS de las curas y las creaciones
dll = None
for p, data in files.items():
    if p.endswith(".dll"):
        dll = data; break
assert dll and dll[:2] == b"MZ", "DLL ausente"
firmas = [
    b"PaseRetro", b"HayCalorVivo", b"EvaluarActivacion", b"TerminarFrameRetro",
    b"AgujeroAbsoluto", b"ClickerCasa", b"CompasRunico", b"FuriaSolarClicker",
    b"RequiemPulsar", b"ConCuartoAnillo", b"DrawRuneCircle",
    b"EstiloConstelacion", b"PrepararPaseDeCalor",
    "AethonCalor".encode("utf-16-le"),      # constante → heap #US (UTF-16)
]
vivas = [f.decode() for f in firmas if f in dll]
muertas = [f.decode() for f in firmas if f not in dll]
print(f"(4) DLL {len(dll)} B — firmas VIVAS: {', '.join(vivas)}")
if muertas:
    print("  ¡FIRMAS MUERTAS:", muertas, "!"); sys.exit(1)

# (5) los PNG de los 4 ítems nuevos
pngs = [p for p in files if p.endswith(".png") and any(k in p for k in
        ("AgujeroAbsoluto", "CompasRunico", "FuriaSolar", "Requiem"))]
print(f"(5) PNG de las creaciones: {len(pngs)} → {[os.path.basename(p) for p in pngs]}")

print("\nTODO VERIFICADO — el .tmod instalado ES la v6.83 completa.")
