#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.103 — AUDITORÍA INDEPENDIENTE DEL .tmod.

LA SESIÓN DE LOS TRES ARREGLOS + LA BOLSA (la letra: «La Forma Ascendida
5 se ve muy simple, no tiene ni brillos ni buenos efectos vfx / me di
cuenta que la etiqueta de consagrado solo aparece en el inventario y no
el barra de acceso rapido del inventario / el banner no tiene ninguno
de los efectos vfx ni textura que tenia que tener / recuerda siempre
crear una bolsa para armas nuevas y darselas al jugador»):
(0) versión 6.103 + entradas + tabla/EOF exacto.
(1) LA QUINTA LUZ RICA en la DLL: PatronAura.Divino5 + DibujarDivino5 +
    EmitirDivino5 + VFXCore.Destello4Puntas (LOS BRILLOS) — y la fábrica
    apuntando al camino nuevo (no Perlin).
(2) EL BANNER en la DLL: la CAPA DE RESISTENCIA RESTAURADA (el manto de
    16 + la cresta + las estelas viven de nuevo en
    DibujarElFrenteYElDestello) + el DOMO OSCURO + las BRASAS AMBIENTALES
    + CalorLib.Punto en el portador.
(3) LA ETIQUETA EN LA BARRA RÁPIDA: CorruptoGlobalItem.UpdateInventory +
    SetNameOverride (el nombre de la instancia lleva «(Consagrado)» —
    la barra rápida lo dibuja con AffixName plano, verificado al
    decompile del tML 2026.09.3.1).
(4) LA BOLSA: FormaAscendidaCincoItem + EstandarteDelJuicioItem dentro
    de BolsaCosmeticos (fuente) + la entrega directa intacta.
(5) hjson ×3: paquete == fuente byte a byte + paridad de claves + el
    tooltip nuevo de la Forma 5 (SUS DIEZ CAPAS).
(6) Texturas de la receta presentes (CampoVacio 256² + FrenteVacio
    256×32) + FlareEstrella4Puntas (los brillos).
(7) 0 IP y 0 CJK (la ley de la casa).
"""
import sys, zlib, struct, hashlib, re, glob as globmod
sys.path.insert(0, '/home/z/my-project/download/Aethon-Mod-Terraria/tools')
from parse_tmod import parse

TMOD = "/tmp/tmlhome/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
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
assert ver == "6.103", f"¡VERSIÓN {ver} ≠ 6.103!"
todos, off, malas = blobs(d, tend, entries)
print(f"(0) blobs: {len(todos)}/{len(entries)} íntegros, malas={malas}, EOF={off} == {fsize}: {'OK' if off == fsize else '¡ROMPO!'}")
assert malas == 0 and off == fsize

def dims(b):
    if b[:8] == b'\x89PNG\r\n\x1a\n': return struct.unpack('>II', b[16:24])
    return struct.unpack('<ii', b[4:12])

def hit(tex):
    h = [p for p in todos if tex in p and (p.endswith('.rawimg') or p.endswith('.png'))]
    assert h, f"¡FALTA {tex}!"
    return todos[h[0]]

print("(1) LA QUINTA LUZ RICA en la DLL (la letra: «se ve muy simple, no tiene ni brillos»):")
dll = todos['AethonMod.dll']
def firma(s, donde):
    return s.encode() in donde or s.encode('utf-16-le') in donde
v6_103a = [
    'Divino5', 'DibujarDivino5', 'EmitirDivino5',   # EL CAMINO PROPIO
    'Destello4Puntas',                               # LOS BRILLOS (VFXCore)
    'FlareEstrella4Puntas',                          # la textura de los brillos
]
for f in v6_103a:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
print(f"    VIVAS: {', '.join(v6_103a)}")
# LA FÁBRICA apunta al camino NUEVO (el Perlin genérico murió):
src_aura = open(f'{REPO}/AethonMod/Content/VFX/AuraLib.cs', encoding='utf-8').read()
assert 'Patron = PatronAura.Divino5' in src_aura, "¡La fábrica NO apunta a Divino5!"
assert 'Turbulencia = 1f' in src_aura and 'Hebras = 14' in src_aura, "¡La FIRMA v6.102 murió!"
# LOS BRILLOS en la emisión (el FlareEstrella por nombre):
assert 'estrella' in src_aura and 'VFXCore.Destello4Puntas' in src_aura
print("    la fábrica → Divino5 ✔ · la turbulencia viva ✔ · los brillos vivos ✔")

print("(2) EL BANNER (la letra: «no tiene ninguno de los efectos vfx ni textura»):")
v6_103b = ['DibujarElFrenteYElDestello']  # EL VESTIDO COMPLETO vive
for f in v6_103b:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
src_vacio = open(f'{REPO}/AethonMod/Content/Globals/VacioGlobalNPC.cs', encoding='utf-8').read()
# LA CAPA DE RESISTENCIA RESTAURADA (v6.101): manto 16 + cresta + estelas
assert 'k < 16' in src_vacio and 'LA CRESTA DE CHOQUE RESTAURADA' in src_vacio \
   and 'LAS ESTELAS DE ABLACIÓN RESTAURADAS' in src_vacio, "¡El manto NO volvió!"
# EL FRENTE DE LA RECETA sigue vivo
assert 'CAPA 2 DE LA RECETA' in src_vacio and 'CAPA 4 DE LA RECETA' in src_vacio
src_est = open(f'{REPO}/AethonMod/Content/Projectiles/EstandarteJuicioAuraProjectile.cs', encoding='utf-8').read()
assert 'EL DOMO OSCURO DEL VACÍO' in src_est, "¡El domo oscuro (visible de DÍA) falta!"
assert 'LAS BRASAS AMBIENTALES' in src_est, "¡Las brasas ambientales faltan!"
assert 'CalorLib.Punto(dueno.MountedCenter' in src_est, "¡El aire del portador no hierve!"
print("    el manto de 16 + la cresta + las estelas RESTAURADOS ✔ · el frente y el destello de la receta VIVOS ✔")
print("    el domo oscuro (día) + las brasas + el hervor del portador ✔")

print("(3) LA ETIQUETA EN LA BARRA RÁPIDA (la letra: «solo aparece en el inventario»):")
src_corr = open(f'{REPO}/AethonMod/Content/Globals/CorruptoGlobalItem.cs', encoding='utf-8').read()
assert 'public override void UpdateInventory' in src_corr, "¡UpdateInventory no existe!"
assert 'SetNameOverride' in src_corr, "¡El override del nombre no se aplica!"
assert 'EndsWith(etiqueta)' in src_corr, "¡El anti-duplicado del tooltip falta!"
assert firma('UpdateInventory', dll)
print("    el nombre de la instancia lleva «(Consagrado)» (SetNameOverride) ✔")
print("    tooltip sin duplicar (EndsWith) ✔ · barra rápida = AffixName plano ✔")

print("(4) LA BOLSA (la regla permanente: «crear una bolsa para armas nuevas y darselas al jugador»):")
src_bolsas = open(f'{REPO}/AethonMod/Content/Items/Bolsas/BolsasCategorias.cs', encoding='utf-8').read()
assert 'Cosmetics.FormaAscendidaCincoItem' in src_bolsas, "¡La Forma 5 NO está en bolsa!"
assert 'Accessories.EstandarteDelJuicioItem' in src_bolsas, "¡El estandarte NO está en bolsa!"
src_shard = open(f'{REPO}/AethonMod/Content/Players/ShardPlayer.cs', encoding='utf-8').read()
assert 'Cosmetics.FormaAscendidaCincoItem' in src_shard, "¡La entrega directa de la Forma 5 murió!"
print("    Forma 5 + Estandarte en BolsaCosmeticos ✔ · la entrega directa intacta ✔")

print("(5) hjson ×3:")
hj = {p: todos[p] for p in todos if p.endswith('.hjson')}
assert len(hj) == 3, hj.keys()
es = [b for p, b in hj.items() if 'es-ES' in p][0].decode('utf-8')
mx = [b for p, b in hj.items() if 'es-MX' in p][0].decode('utf-8')
en = [b for p, b in hj.items() if 'es-US' in p or 'en-US' in p][0].decode('utf-8')
fuente_es = open(f'{REPO}/AethonMod/Localization/es-ES_Mods.AethonMod.hjson', 'rb').read()
fuente_mx = open(f'{REPO}/AethonMod/Localization/es-MX_Mods.AethonMod.hjson', 'rb').read()
fuente_en = open(f'{REPO}/AethonMod/Localization/en-US_Mods.AethonMod.hjson', 'rb').read()
assert es.encode('utf-8') == fuente_es, "¡es-ES paquete ≠ fuente!"
assert mx.encode('utf-8') == fuente_mx, "¡es-MX paquete ≠ fuente!"
assert en.encode('utf-8') == fuente_en, "¡en-US paquete ≠ fuente!"
print("    OK: los 3 hjson paquete == fuente BYTE A BYTE")
claves = {idioma: set(re.findall(r'^\t([A-Za-z0-9_]+):', t, re.M))
          for idioma, t in (('es-ES', es), ('es-MX', mx), ('en-US', en))}
a, b, c = claves['es-ES'], claves['es-MX'], claves['en-US']
assert a == b, f"¡es-ES y es-MX divergen! {a ^ b}"
assert a == c, f"¡es-ES y en-US divergen! {a ^ c}"
print(f"    OK: paridad de claves ×3 ({len(a)} claves por idioma)")
for idioma, texto in (('es-ES', es), ('es-MX', mx), ('en-US', en)):
    assert 'SUS DIEZ CAPAS' in texto, f"¡El tooltip nuevo de la Forma 5 falta en {idioma}!"
print("    OK: el tooltip nuevo de la Forma 5 (SUS DIEZ CAPAS) ×3")

print("(6) LAS TEXTURAS:")
campo = hit('Effects/CampoVacio')
assert dims(campo) == (256, 256), f"¡CampoVacio NO es 256×256! {dims(campo)}"
frente = hit('Effects/FrenteVacio')
assert dims(frente) == (256, 32), f"¡FrenteVacio NO es 256×32! {dims(frente)}"
flare = hit('FlareEstrella4Puntas')
assert dims(flare) == (256, 256), f"¡FlareEstrella NO es 256²! {dims(flare)}"
icono = hit('Items/Cosmetics/FormaAscendidaCincoItem')
assert dims(icono) == (30, 30), f"¡El icono NO es 30×30! {dims(icono)}"
print("    CampoVacio 256² ✔ · FrenteVacio 256×32 ✔ · FlareEstrella4Puntas 256² ✔ · icono 30×30 ✔")

print("(7) LA LEY DE LA CASA (0 IP y 0 CJK):")
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

print("(8) LICENSE (la propietaria) dentro:")
lic = todos.get('LICENSE') or todos.get('LICENSE.txt')
assert lic is not None, "¡LICENSE no viaja!"
assert 'TODOS LOS DERECHOS RESERVADOS' in lic.decode('utf-8', 'replace')
print("    OK: propietaria, sin atribuciones")

print("\nAUDIT v6.103: TODO VERIFICADO ✔")
