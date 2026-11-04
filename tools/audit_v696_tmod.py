#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.96 — AUDITORÍA DEL .tmod: LA CURA DEL HAMBRE MUERTA.
(0) versión 6.95 + entradas
(1) tabla + EOF exacto
(2) PNGs: las texturas de la casa VIVAS + las copiadas AUSENTES + EL
    ALTAR NUEVO de la Forja (36×54 con su highlight)
(3) las dos músicas del festín
(4) DLL: firmas v6.93/94/95 vivas + muertas ausentes + SIN chino ni IP
(5) hjson ×3 simétricos + las claves nuevas (Renglones/RC2/Mirada/dinero)
(6) LICENSE (la propietaria) dentro
"""
import sys, zlib, struct
sys.path.insert(0, '/home/z/bombus-repo/tools')
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
assert ver == "6.96", f"¡VERSIÓN {ver} ≠ 6.96!"
todos, off, malas = blobs(d, tend, entries)
print(f"(1) blobs: {len(todos)}/{len(entries)} íntegros, malas={malas}, EOF={off} == {fsize}: {'OK' if off == fsize else '¡ROMPO!'}")
assert malas == 0 and off == fsize

def dims(b):
    if b[:8] == b'\x89PNG\r\n\x1a\n': return struct.unpack('>II', b[16:24])
    return struct.unpack('<ii', b[4:12])

print("(2) PNGs — LA LIMPIEZA + EL ALTAR DE LA FORJA:")
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
    # v6.95 — EL ALTAR DE LA FORJA (la pluma + el molde + el alma)
    'Tiles/AltarOfrendasTile': (36, 54),
    'Tiles/AltarOfrendasTile_Highlight': (36, 54),
    'Items/Altar/AltarOfrendasItem': (30, 44),
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
v6_96 = ['ArrojadoTirosSistema', 'MarcarPorRed', 'AvisarTiroDelPortador',  # LA CURA DEL HAMBRE
         'MsgTiro', 'HuellaDelTiro', 'LaHuellaVive', 'BancoMonedasDisponibles']
v6_95 = ['JefeRenglonesProj', 'JefeRC2Proj', 'JefeMiradaProj',   # LOS TRES HACES DEL DIOS
         'RefrescarPara', 'SemianchoDeCasa',                       # las leyes de la Mirada prestadas
         'EstadoRenglones', 'EstadoRC2', 'EstadoMirada',           # los estados del jefe
         'DespertarReq7a', 'DespertarReq7f']                       # la nota cortada en 6 líneas
def firma(s, donde):
    return s.encode() in donde or s.encode('utf-16-le') in donde
for f in v6_93 + v6_95 + v6_96:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
    print(f"    VIVA {f}")
muertas_f = ['MiradaSolyn', 'SolynTagTeamBeam', 'RelicarioDelAvispon', 'GrilletesDeAndromeda',
             'WHitoEscuadron', 'EntitySource_ItemDrop']
for f in muertas_f:
    assert not firma(f, dll), f"¡{f} SIGUE EN LA DLL!"
    print(f"    MUERTA-AUSENTE {f}")

print("(4b) FUENTE — las constantes inline (ItemID no viaja en la dll):")
src = open('/home/z/bombus-repo/AethonMod/Content/Systems/DespertarGrimorio.cs', encoding='utf-8').read()
assert 'ItemID.WandofSparking' in src and 'ItemID.FinchStaff' not in src
print("    OK: nivel 1 = SlimeStaff + WandofSparking (FinchStaff ausente)")
print("(4c-bis) FUENTE — v6.96: los (A)/(M) muertos + el daño +1% + el ✓ del banco:")
grim = open('/home/z/bombus-repo/AethonMod/Content/Weapons/GrimoireEternal.cs', encoding='utf-8').read()
assert '(A) {nombreArma}' not in grim and '(M) {cuota}' not in grim, '¡los (A)/(M) viven!'
assert 'BonusDano(despertar) * 100f' in grim, '¡el daño no lee el acumulado!'
req = open('/home/z/bombus-repo/AethonMod/Content/Globals/DespertarRequisitosGlobalItem.cs', encoding='utf-8').read()
assert 'BancoMonedasDisponibles(almaReq)' in req, '¡el ✓ del dinero no lee el banco!'
print('    OK: sin (A)/(M) · daño = BonusDano (acumulado) · ✓ del dinero por el banco')
print("(4c) FUENTE — SIN texto en chino ni IP externa (la ley de la casa):")
import re, glob as globmod
cjk = re.compile('[\u4e00-\u9fff\u3040-\u30ff\uac00-\ud7af]')
ips = re.compile(r're:?zero|大兎|ousagi|daphne|subaru|roswaal', re.I)
malos = []
for f in globmod.glob('/home/z/bombus-repo/AethonMod/**/*.cs', recursive=True) + \
         globmod.glob('/home/z/bombus-repo/AethonMod/**/*.hjson', recursive=True):
    t = open(f, encoding='utf-8', errors='replace').read()
    if cjk.search(t): malos.append(('CJK', f))
    if ips.search(t): malos.append(('IP', f))
assert not malos, malos
print(f"    OK: 0 caracteres CJK y 0 menciones IP en {len(globmod.glob('/home/z/bombus-repo/AethonMod/**/*.cs', recursive=True))} .cs + hjson")

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
mx = [b for p, b in hj.items() if 'es-MX' in p and 'Escriba' not in p][0].decode('utf-8')
en = [b for p, b in hj.items() if 'en-US' in p and 'Escriba' not in p][0].decode('utf-8')
def cuerpo(t):
    """El hjson SIN su bloque de cabecera (los comentarios de diseño de
    cada maleta difieren POR DISEÑO: fuente única vs espejo vs fallback)."""
    lineas = t.split('\n')
    i = 0
    while i < len(lineas) and (lineas[i].strip() == '' or lineas[i].lstrip().startswith('//')):
        i += 1
    return '\n'.join(lineas[i:])
assert cuerpo(es) == cuerpo(mx), "¡es-ES y es-MX divergen en el CUERPO (el espejo de la casa)!"
assert cuerpo(es) == cuerpo(en), "¡el en-US no lleva el español del fallback!"
print("    OK: es-ES == es-MX == en-US en el cuerpo (el espejo + el fallback español)")
for clave in ['ComoEstatuas', 'Estatua:', 'ReqJefeCaido', 'ReqJefeViva', 'YaConsagrada',
              'Renglones:', 'RayoConcentrado:', 'Mirada:',
              'el dinero simplemente se deja caer', 'se comen SIN sello', 'No es apto']:
    assert clave in es, f"¡FALTA la clave {clave}!"
    print(f"    CLAVE VIVA {clave}")

print("(6) LICENSE (la propietaria) dentro:")
lic = todos.get('LICENSE') or todos.get('LICENSE.txt')
assert lic is not None, "¡LICENSE no viaja!"
texto = lic.decode('utf-8', 'replace')
assert 'TODOS LOS DERECHOS RESERVADOS' in texto, texto[:200]
assert 'pixelfx' not in texto.lower() and 'luminance' not in texto.lower()
print("    OK: propietaria, sin atribuciones")
print("\nAUDIT v6.96: TODO VERIFICADO ✔")
