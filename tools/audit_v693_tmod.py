#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.93 — AUDITORÍA DEL .tmod: LA LIMPIEZA Y LA LISTA NUEVA.
(0) versión 6.93 + entradas
(1) tabla + EOF exacto
(2) PNGs: las 5 texturas de la casa VIVAS + las 7 copiadas AUSENTES + heredadas
(3) las dos músicas del festín
(4) DLL: firmas v6.93 vivas + muertas ausentes
(5) hjson ×3 simétricos + ComoEstatuas/Estatua vivas
(6) LICENSE (la propietaria) dentro
"""
import sys, zlib, struct
sys.path.insert(0, '/home/z/my-project/download/Aethon-Mod-Terraria/tools')
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
assert ver == "6.93", f"¡VERSIÓN {ver} ≠ 6.93!"
todos, off, malas = blobs(d, tend, entries)
print(f"(1) blobs: {len(todos)}/{len(entries)} íntegros, malas={malas}, EOF={off} == {fsize}: {'OK' if off == fsize else '¡ROMPO!'}")
assert malas == 0 and off == fsize

def dims(b):
    if b[:8] == b'\x89PNG\r\n\x1a\n': return struct.unpack('>II', b[16:24])
    return struct.unpack('<ii', b[4:12])

print("(2) PNGs — LA LIMPIEZA:")
vivas = {
    'Effects/Procedural/Investigacion/DestelloEstelar': (512, 512),
    'Effects/Procedural/Investigacion/ResplandorChico': (200, 200),
    'Effects/Procedural/Investigacion/RuidoDendritico': (512, 512),
    'Effects/Textures/MapaDistorsion': (256, 256),
    'Effects/Textures/ResplandorChico': (200, 200),
    'Effects/Textures/RuidoDendritico': (256, 256),
    'Effects/Textures/RuidoManchas': (256, 256),
    'Projectiles/Cosmetic/VastagoDelEternoPet': (40, 384),
    'Items/Bolsas/BolsaDePociones': (30, 30),
    'Weapons/Investigaciones/MiradaDelEterno': (34, 40),
    'Weapons/Definitivas/RenglonFinal': (44, 44),
}
for tex, wh in vivas.items():
    hit = [p for p in todos if tex in p and (p.endswith('.rawimg') or p.endswith('.png'))]
    assert hit, f"¡FALTA {tex}!"
    assert dims(todos[hit[0]]) == wh, f"{tex}: {dims(todos[hit[0]])} ≠ {wh}"
    print(f"    VIVA {tex.split('/')[-1]}: {wh[0]}x{wh[1]}")
muertas = ['DendriticNoiseZoomedOut', 'ShineFlare', 'BloomCircleSmall', 'WavyBlotchNoise', 'PsychedelicWingTextureOffsetMap', 'MiradaSolyn']
for tex in muertas:
    assert not [p for p in todos if tex in p], f"¡LA COPIADA {tex} SIGUE DENTRO!"
    print(f"    MUERTA-AUSENTE {tex}")
ogs = [p for p in todos if p.endswith('.ogg') and len(todos[p]) > 1_500_000]
print(f"(3) músicas del festín: {len(ogs)} — OggS: {[todos[p][:4]==b'OggS' for p in ogs]}")
assert len(ogs) == 2 and all(todos[p][:4]==b'OggS' for p in ogs)

print("(4) DLL — firmas:")
dll = todos['AethonMod.dll']
v6_93 = ['ArrojadoGlobalItem', 'JefeEnElChecklistActual', 'ContarOpcionesPagadas',
         'BancoGuardarMonedas', 'BancoCobrarMonedas', 'BancoGrupoDisponible',
         'HazLib', 'MiradaDelEterno', 'RenglonFinal', 'DespertarDevorarA',
         'DespertarBarra', 'DespertarHitoDano', 'ComoEstatuas',
         'EntitySource_DropAsItem']
def firma(s, donde):
    return s.encode() in donde or s.encode('utf-16-le') in donde
for f in v6_93:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
    print(f"    VIVA {f}")
muertas_f = ['MiradaSolyn', 'SolynTagTeamBeam', 'RelicarioDelAvispon', 'GrilletesDeAndromeda',
             'WHitoEscuadron', 'EntitySource_ItemDrop']
for f in muertas_f:
    assert not firma(f, dll), f"¡{f} SIGUE EN LA DLL!"
    print(f"    MUERTA-AUSENTE {f}")

print("(4b) FUENTE — las constantes inline (ItemID no viaja en la dll):")
src = open('/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Systems/DespertarGrimorio.cs', encoding='utf-8').read()
assert 'ItemID.WandofSparking' in src and 'ItemID.FinchStaff' not in src
print("    OK: nivel 1 = SlimeStaff + WandofSparking (FinchStaff ausente)")

print("(5) hjson ×3:")
hj = {p: todos[p] for p in todos if p.endswith('.hjson')}
assert len(hj) == 6, hj.keys()
import hashlib
grupos = {}
for p, b in hj.items():
    for idioma in ['es-ES', 'es-MX', 'en-US']:
        if idioma in p:
            clave = ('Escriba' if 'Escriba' in p else 'Mods')
            grupos.setdefault((idioma, clave), []).append(hashlib.md5(b).hexdigest())
assert len(grupos) == 6, grupos.keys()
for (idioma, clave), hashes in sorted(grupos.items()):
    assert len(hashes) == 1 or len(set(hashes)) == 1, f"{idioma}/{clave} asimétrico"
    print(f"    {idioma}/{clave}: {'==' if len(set(hashes))==1 else '≠'} ")
es = [b for p, b in hj.items() if 'es-ES' in p and 'Escriba' not in p][0].decode('utf-8')
for clave in ['ComoEstatuas', 'Estatua:', 'ReqJefeCaido', 'ReqJefeViva', 'YaConsagrada']:
    assert clave in es, f"¡FALTA la clave {clave}!"
    print(f"    CLAVE VIVA {clave}")

print("(6) LICENSE (la propietaria) dentro:")
lic = todos.get('LICENSE') or todos.get('LICENSE.txt')
assert lic is not None, "¡LICENSE no viaja!"
texto = lic.decode('utf-8', 'replace')
assert 'TODOS LOS DERECHOS RESERVADOS' in texto, texto[:200]
assert 'pixelfx' not in texto.lower() and 'luminance' not in texto.lower()
print("    OK: propietaria, sin atribuciones")
print("\nAUDIT v6.93: TODO VERIFICADO ✔")
