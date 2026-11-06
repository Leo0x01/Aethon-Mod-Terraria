#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.102 — AUDITORÍA INDEPENDIENTE DEL .tmod.

LA SESIÓN DE LA FORJA, LA QUINTA LUZ Y LA RECETA:
(0) versión 6.102 + entradas + tabla/EOF exacto.
(1) EL TENTÁCULO FORJADO (TentaculoAbismo.png 128×512: LA PLUMA IA +
    EL MOLDE + LA SASTRERÍA — máscara U, espejo periódico C0 en V,
    premultiplicado del lote alfa) + las texturas de la receta
    (CampoVacio 256×256 + FrenteVacio 256×32) + el icono de la Forma 5.
(2) LA QUINTA LUZ en la DLL: AuraPerfil.Turbulencia/Hebras (la capa
    de turbulencia SOLO del aura), FormaAscendidaCinco (la fábrica),
    el modo 8 del portador y la piel forjada (MasaAbismo +
    VFXCore.TentaculoAbismo).
(3) LA RECETA en la DLL: VestirLaReceta (CalorLib + el polvo) y
    DibujarElFrenteYElDestello (la capa 2 y la 4) — y la v6.101
    (DibujarCapaDeAblacion) MUERTA-AUSENTE.
(4) LA ENTREGA de la Forma 5 (la regla permanente: directo al jugador).
(5) hjson ×3 + es-ES == es-MX + FormaAscendidaCincoItem por idioma.
"""
import sys, zlib, struct, hashlib, re, glob as globmod
sys.path.insert(0, '/home/z/my-project/download/Aethon-Mod-Terraria/tools')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
REPO = "/home/z/my-project/download/Aethon-Mod-Terraria"

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
assert ver == "6.102", f"¡VERSIÓN {ver} ≠ 6.102!"
todos, off, malas = blobs(d, tend, entries)
print(f"(0) blobs: {len(todos)}/{len(entries)} íntegros, malas={malas}, EOF={off} == {fsize}: {'OK' if off == fsize else '¡ROMPO!'}")
assert malas == 0 and off == fsize

def dims(b):
    if b[:8] == b'\x89PNG\r\n\x1a\n': return struct.unpack('>II', b[16:24])
    return struct.unpack('<ii', b[4:12])

def abrir(b):
    """Abre una textura del paquete como PIL RGBA (PNG o rawimg)."""
    if b[:8] == b'\x89PNG\r\n\x1a\n':
        return Image.open(io.BytesIO(b)).convert('RGBA')
    w, h = struct.unpack('<ii', b[4:12])
    return Image.frombytes('RGBA', (w, h), b[12:12 + w * h * 4])

def hit(tex):
    h = [p for p in todos if tex in p and (p.endswith('.rawimg') or p.endswith('.png'))]
    assert h, f"¡FALTA {tex}!"
    return todos[h[0]]

print("(1) EL TENTÁCULO FORJADO + LAS TEXTURAS DE LA RECETA + EL ICONO:")
piel = hit('Effects/TentaculoAbismo')
assert dims(piel) == (128, 512), f"¡La piel NO es 128×512! {dims(piel)}"
# EL ESPEJO PERIÓDICO: la fila 511 == la fila 0 (la costura C0 en V)
from PIL import Image
import io
im = abrir(piel)
px = im.load()
fila0 = [px[x, 0] for x in range(128)]
fila511 = [px[x, 511] for x in range(128)]
assert fila0 == fila511, "¡La costura del espejo NO es C0 (fila 0 ≠ fila 511)!"
# LA MÁSCARA U: los bordes se desvanecen (columna 2 y 126 alfa 0)
assert px[2, 256][3] == 0 and px[125, 256][3] == 0, "¡La máscara U no respira en los bordes!"
# EL PREMULTIPLICADO: en el lote alfa One/InverseSourceAlpha el RGB ya
# viene multiplicado (un pixel de alfa medio NUNCA tiene RGB pleno)
sospechosos = sum(1 for y in range(0, 512, 8) for x in range(20, 108, 6)
                  if 0 < px[x, y][3] < 250 and px[x, y][0] > px[x, y][3])
assert sospechosos <= 3, f"¡{sospechosos} píxeles NO premultiplicados!"
print(f"    LA PIEL 128×512: espejo C0 ✔ · máscara U ✔ · premultiplicado ✔ ({sospechosos} sospechosos)")
campo = hit('Effects/CampoVacio')
assert dims(campo) == (256, 256), f"¡CampoVacio NO es 256×256! {dims(campo)}"
imc = abrir(campo)
pc = imc.load()
assert pc[128, 128][3] == 0, "¡El centro del campo NO está limpio!"
assert pc[128, 40][3] > 100, "¡La banda del campo NO arde!"
print(f"    CampoVacio 256×256: centro limpio ✔ · banda encendida ✔")
frente = hit('Effects/FrenteVacio')
assert dims(frente) == (256, 32), f"¡FrenteVacio NO es 256×32! {dims(frente)}"
imf = abrir(frente)
pf = imf.load()
a_origen = sum(pf[x, 16][3] for x in range(0, 32)) / 32
a_final = sum(pf[x, 16][3] for x in range(224, 256)) / 32
assert a_origen > a_final * 1.5, "¡La estela NO es densa en el origen!"
print(f"    FrenteVacio 256×32: densa en el origen ({a_origen:.0f}) → dispersa al final ({a_final:.0f}) ✔")
icono = hit('Items/Cosmetics/FormaAscendidaCincoItem')
assert dims(icono) == (30, 30), f"¡El icono NO es 30×30! {dims(icono)}"
print(f"    FormaAscendidaCincoItem 30×30 ✔")

print("(2) LA QUINTA LUZ + LA PIEL FORJADA en la DLL:")
dll = todos['AethonMod.dll']
def firma(s, donde):
    return s.encode() in donde or s.encode('utf-16-le') in donde
v6_102 = [
    # LA CAPA DE TURBULENCIA (AuraPerfil) + su lectura en Emitir
    'Turbulencia', 'Hebras',
    # LA FÁBRICA + EL ÍTEM + EL MODO 8
    'FormaAscendidaCinco', 'PerfilAscendidaCinco',
    # LA PIEL FORJADA (VFXCore + SombrasLib + el proyectil la viste)
    'TentaculoAbismo', 'MasaAbismo',
]
for f in v6_102:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
print(f"    VIVAS: {len(v6_102)} de la v6.102 (la turbulencia · la quinta luz · la piel forjada)")

print("(3) LA RECETA en la DLL:")
v6_102b = ['VestirLaReceta', 'DibujarElFrenteYElDestello']
for f in v6_102b:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
muertas_f = ['DibujarCapaDeAblacion']   # la v6.101 murió en v6.102
for f in muertas_f:
    assert not firma(f, dll), f"¡{f} SIGUE EN LA DLL!"
print(f"    VIVAS: {len(v6_102b)} · MUERTAS-AUSENTE: {len(muertas_f)} (la capa de resistencia v6.101 murió)")

print("(4) LA ENTREGA DE LA FORMA 5 (la regla permanente — directo al jugador):")
assert firma('FormaAscendidaCincoItem', dll)
src = open(f'{REPO}/AethonMod/Content/Players/ShardPlayer.cs', encoding='utf-8').read()
assert 'Cosmetics.FormaAscendidaCincoItem' in src, "¡La entrega de la Forma 5 NO está registrada!"
print("    VIVA la entrega en EntregarRegaloDePruebas (al entrar al mundo, si no la lleva)")

print("(4a) PAQUETE + FUENTE — 0 IP y 0 CJK (la ley de la casa):")
ips_pkg = re.compile(r're:?zero|ousagi|daphne|subaru|roswaal|calamity|warbanner', re.I)
cjk = re.compile('[\u4e00-\u9fff\u3040-\u30ff\uac00-\ud7af]')
sucios = [p for p in todos if p.endswith('.hjson') and
          (ips_pkg.search(todos[p].decode('utf-8', 'replace')) or cjk.search(todos[p].decode('utf-8', 'replace')))]
assert not sucios, sucios
archivos = globmod.glob(f'{REPO}/AethonMod/**/*.cs', recursive=True) + \
           globmod.glob(f'{REPO}/AethonMod/**/*.hjson', recursive=True)
malos = []
for f in archivos:
    t = open(f, encoding='utf-8', errors='replace').read()
    if cjk.search(t): malos.append(('CJK', f))
    if ips_pkg.search(t): malos.append(('IP', f))
assert not malos, malos
print(f"    OK: 0 IP y 0 CJK en {len(archivos)} archivos y en los hjson empaquetados")

print("(5) hjson ×3:")
hj = {p: todos[p] for p in todos if p.endswith('.hjson')}
assert len(hj) == 3, hj.keys()
es = [b for p, b in hj.items() if 'es-ES' in p][0].decode('utf-8')
mx = [b for p, b in hj.items() if 'es-MX' in p][0].decode('utf-8')
en = [b for p, b in hj.items() if 'en-US' in p][0].decode('utf-8')
def cuerpo(t):
    lineas = t.split('\n')
    i = 0
    while i < len(lineas) and (lineas[i].strip() == '' or lineas[i].lstrip().startswith('//')):
        i += 1
    return lineas[i:]
# LA REGLA v6.101 (la convención de la casa desde la .101): el es-MX ya
# no es el espejo byte a byte del es-ES — lleva sus entradas vivas donde
# la sesión las dejó (el Coro y la Pluma MOVIDAS con su contenido real).
# La regla es: PAQUETE == FUENTE byte a byte + PARIDAD DE CLAVES por
# idioma (la red de seguridad: nada falta en ningún idioma).
import re as _re
fuente_es = open(f'{REPO}/AethonMod/Localization/es-ES_Mods.AethonMod.hjson', 'rb').read()
fuente_mx = open(f'{REPO}/AethonMod/Localization/es-MX_Mods.AethonMod.hjson', 'rb').read()
fuente_en = open(f'{REPO}/AethonMod/Localization/en-US_Mods.AethonMod.hjson', 'rb').read()
assert es.encode('utf-8') == fuente_es, "¡es-ES paquete ≠ fuente!"
assert mx.encode('utf-8') == fuente_mx, "¡es-MX paquete ≠ fuente!"
assert en.encode('utf-8') == fuente_en, "¡en-US paquete ≠ fuente!"
print("    OK: los 3 hjson paquete == fuente BYTE A BYTE")
claves = {idioma: set(_re.findall(r'^\t([A-Za-z0-9_]+):', t, _re.M))
          for idioma, t in (('es-ES', es), ('es-MX', mx), ('en-US', en))}
a, b, c = claves['es-ES'], claves['es-MX'], claves['en-US']
assert a == b, f"¡es-ES y es-MX divergen en claves! {a ^ b}"
assert a == c, f"¡es-ES y en-US divergen en claves! {a ^ c}"
print(f"    OK: paridad de claves ×3 ({len(a)} claves por idioma)")
for clave in ['FormaAscendidaCincoItem', 'SombraDeLaPagina5']:
    for idioma, texto in (('es-ES', es), ('es-MX', mx), ('en-US', en)):
        assert f'{clave}:' in texto, f"¡FALTA {clave} en {idioma}!"
print("    OK: FormaAscendidaCincoItem ×3 + el tomo V ×3")

print("(6) LICENSE (la propietaria) dentro:")
lic = todos.get('LICENSE') or todos.get('LICENSE.txt')
assert lic is not None, "¡LICENSE no viaja!"
assert 'TODOS LOS DERECHOS RESERVADOS' in lic.decode('utf-8', 'replace')
print("    OK: propietaria, sin atribuciones")
print("\nAUDIT v6.102: TODO VERIFICADO ✔")
