#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6168_tmod.py — AUDITORÍA DEL .tmod DE LA v6.168 (con el parser de la casa).
"""
import sys, zlib, struct, hashlib
sys.path.insert(0, '/home/z/my-project/mod/tools')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"

def blobs(d, tend, entries):
    out, off, malas = {}, tend, 0
    for p, raw, comp in entries:
        trozo = d[off:off + comp]
        if len(trozo) != comp: malas += 1; break
        try: data = trozo if comp == raw else zlib.decompress(trozo, -15)
        except zlib.error: malas += 1; break
        if len(data) != raw: malas += 1; break
        out[p] = data; off += comp
    return out, off, malas

d = open(TMOD, 'rb').read()
tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla {tend} | {fsize:,} B")
print(f"md5 {hashlib.md5(d).hexdigest()}")
assert ver == "6.168", f"¡VERSIÓN {ver} ≠ 6.168!"
todos, off, malas = blobs(d, tend, entries)
print(f"blobs: {len(todos)}/{len(entries)} íntegros, malas={malas}, EOF={off} == {fsize}: {'OK' if off == fsize else '¡ROMPO!'}")
assert malas == 0 and off == fsize

dll = todos['AethonMod.dll']
def firma(s): return s.encode() in dll

print("\n(1) FIRMAS NUEVAS v6.168 (deben estar):")
nuevas = ['SelectorFlotante', 'ClickerDelUmbral', 'MarcaDeTinta', 'LaReglaDelCielo',
          'LineaDelCielo', 'AdoptarItem', '_scrollJefes', 'JefesCuenta',
          'DevueltaAlSuelo', 'SelectorTrioTitulo']
ok = True
for f in nuevas:
    esta = firma(f); ok &= esta
    print(f"  {'✔' if esta else '✘ FALTA'} {f}")

print("\n(2) FIRMAS MUERTAS (deben faltar):")
muertas = ['BolsaClickers', 'PlumaDelCronista', 'CompasRunico', 'FuriaSolarClicker',
           'SelloDeTinta', 'ParpadeoDelEterno', 'MareaDePlata', 'CompasDeLosSilos',
           'CorazonDelLibro', 'RelojDeArenaDelFin' if False else 'VeredictoRunico']
for f in muertas:
    esta = firma(f); ok &= not esta
    print(f"  {'✔ ausente' if not esta else '✘ PRESENTE'} {f}")

print("\n(3) RECURSOS clickers muertos (deben faltar):")
for patron in ['Clicker/PlumaDelCronista', 'Clicker/CompasRunico', 'Clicker/SelloDeTinta',
               'Clicker/MareaDePlata', 'Clicker/CorazonDelLibro']:
    hay = any(patron in p for p in todos)
    ok &= not hay
    print(f"  {'✔ ausente' if not hay else '✘ PRESENTE'} {patron}")

print("\n(4) hjson:")
for p in sorted(todos):
    if p.endswith('.hjson'): print(f"  {p} ({len(todos[p]):,} B)")

print("\n(5) LA CAZA NUEVA: el tope de 10 y la caza sin alma")
import re
src_alma = open('/home/z/my-project/mod/AethonMod/Content/Globals/AlmaEternoGlobalItem.cs', encoding='utf-8').read()
assert 'Math.Min(10, 1 + NivelDespertar / 20' in src_alma, "¡El tope de 10 no está!"
src_flot = open('/home/z/my-project/mod/AethonMod/Content/Projectiles/Nervioso/GrimorioNerviosoFlotante.cs', encoding='utf-8').read()
assert 'vivasCaza < topeSombra' in src_flot, "¡La caza sin alma no está!"
print("  ✔ SombrasCap tope 10 + caza sin depender del alma")

print("\nAUDITORÍA", "OK ✔" if ok else "FALLÓ ✘")
sys.exit(0 if ok else 1)
