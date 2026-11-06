#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6107_tmod.py — LA AUDITORÍA DEL .tmod v6.107.

LA LETRA DEL USUARIO (la corrección del aura de juicio 2): «tanto las
particulas como la medialuna en los enemigos al usar el aura del
juicio, esta mal, intenta darle una rotacion vertical de 90 grados, o
sea darle la vuelte y las particulas deben nacer del centro de los
enemigos y luego dispersarce hacia fuera pero siempre en la direccion
opuesta al jugador».
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
assert ver == "6.107", f"¡VERSIÓN {ver} ≠ 6.107!"
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

dll = todos['AethonMod.dll']
def firma(s, donde):
    return s.encode() in donde or s.encode('utf-16-le') in donde

print("(1) LA CORRECCIÓN v6.107 — LA CÚPULA VERTICAL + LA EXPULSIÓN DEL CORAZÓN:")
vivas = ['AuraDeJuicio2Item', 'AuraJuicio2Ancla', 'VestirUnImpio', 'PerlinSemi',
         'PortaCampoDeJuicio', 'PolvoExpulsionJuicio', 'RadioDomo', 'DistMax']
for f in vivas:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
print(f"    VIVAS en la DLL: {', '.join(vivas)}")

src_ancla = open(f'{REPO}/AethonMod/Content/Projectiles/AuraJuicio2Ancla.cs', encoding='utf-8').read()
src_dust = open(f'{REPO}/AethonMod/Content/Dusts/PolvoExpulsionJuicio.cs', encoding='utf-8').read()

# EL EFECTO SIGUE SIENDO EL DEL ESTANDARTE (la quemadura, el radio, el pago):
assert 'QuemaduraVacioExterior' in src_ancla and 'EstandarteJuicioAuraProjectile.RADIO' in src_ancla
# SOBRE EL JUGADOR NO HAY NADA (el ancla NO alumbra al portador):
assert 'Lighting.AddLight' not in src_ancla, "¡El ancla ALUMBRA al portador!"

# LA MEDIALUNA VERTICAL — la rotación vertical de 90° de la letra: el
# bamboleo YA NO cuelga de dir.ToRotation()+PI/2 (la inclinación
# horizontal de la v6.106 MURIÓ): la cúpula queda DE PIE, la parte
# plana disuelta ABAJO sobre el corazón y la concha coronando ARRIBA:
assert 'float rot = 0.05f * MathF.Sin(t * 0.8f' in src_ancla, "¡La cúpula NO está de pie (rot vertical)!"
assert 'dir.ToRotation() + MathHelper.PiOver2' not in src_ancla, "¡La inclinación horizontal de la v6.106 sigue VIVA!"
assert '(0.40f + 0.26f * k)' in src_ancla and 'FlushAlpha()' in src_ancla, "¡El pase alfa de la cúpula falta!"
assert 'tamEco' in src_ancla and 'FlushAdditive()' in src_ancla, "¡El pase aditivo + eco falta!"

# LAS CHISPAS NACEN DEL CENTRO y se dispersan hacia fuera SIEMPRE en la
# dirección OPUESTA al jugador (el abanico alrededor de portador→impío):
assert 'Vector2 cuna = n.Center' in src_ancla, "¡Las chispas NO nacen del centro del impío!"
assert 'dir.RotatedBy(abanico)' in src_ancla and 'Main.rand.NextFloat(-0.85f, 0.85f)' in src_ancla, \
    "¡El abanico de fuga OPUESTO al jugador falta!"
assert 'customData = chispa' in src_ancla, "¡Las chispas NO viajan liberadas!"
assert 'Centro = cuna' in src_ancla and 'Dir = rumbo' in src_ancla, "¡La geometría congelada al nacer falta!"
assert 'float acel = 0.020f + 0.018f * k' in src_ancla, "¡La aceleración de las chispas falta!"
assert 'Main.rand.NextFloat(0.24f, 0.38f)' in src_ancla, "¡Las chispas NO son menudas!"
assert 'RadioDomo(n, k)' in src_ancla and src_ancla.count('RadioDomo(n, k)') >= 3, \
    "¡El teatro NO comparte la misma cúpula (RadioDomo)!"

# EL POLVO DE LA EXPULSIÓN — el vuelo recto del corazón, aceleración viva:
assert 'dust.position = c.Centro + c.Dir * c.Dist' in src_dust, "¡El polvo NO vuela recto del corazón!"
assert 'MathF.Min(c.Vel * (1f + c.Acel)' in src_dust, "¡El polvo de la expulsión NO acelera!"
assert 'dust.rotation = c.Dir.ToRotation()' in src_dust, "¡La estela NO apunta por donde vuela!"
assert 'return false;   // TODO manual' in src_dust, "¡La física vanilla no está desactivada!"

# LO VIEJO MUERTO (el polvo del arco y su geometría de la v6.106):
assert 'PolvoArcoJuicio' not in src_ancla, "¡El ancla sigue usando el polvo del arco viejo!"
assert not firma('PolvoArcoJuicio', dll), "¡PolvoArcoJuicio VIVE en la DLL!"
assert 'phi - MathHelper.PiOver2' not in src_ancla, "¡Los cuernos del arco siguen vivos!"
assert 'AngFin' not in src_ancla and 'VelAng' not in src_ancla, "¡La geometría angular del arco sigue viva!"
assert 'MathF.Pow(edad, 2.3f)' not in src_ancla, "¡La bruma del arco (prog^2.3) sigue viva!"
assert 'velocity: tangente' not in src_ancla, "¡La tangente del barrido sigue viva!"

# LA BRUMA DEL CORAZÓN — nace del corazón y se derrama por el MISMO
# rumbo de fuga (dirección opuesta al jugador) + intensidad variable:
assert 'Vector2 pos = n.Center + rumbo * avance' in src_ancla, "¡La bruma NO nace del corazón!"
assert 'velocity: rumbo *' in src_ancla, "¡La bruma NO se derrama por el rumbo de fuga!"
assert '(0.16f + 0.34f * k)' in src_ancla, "¡La bruma no tiene intensidad variable!"
assert 'int puffs = 3 + (k > 0.6f ? 1 : 0)' in src_ancla, "¡La bruma no crece con la cercanía!"

print("    cúpula: DE PIE (rot sin inclinación) + alfa+aditivo+eco+bamboleo ✔")
print("    chispas: NACEN DEL CORAZÓN ✔ · abanico SIEMPRE opuesto al jugador ✔ · liberadas ✔")
print("    chispas: aceleración ✔ · menudas 0.24–0.38 ✔ · estela por el rumbo ✔")
print("    bruma: NACE DEL CORAZÓN ✔ · MISMO rumbo de fuga ✔ · intensidad ∝k ✔")
print("    MUERTAS y ausentes: PolvoArcoJuicio, los cuernos, AngFin/VelAng, prog^2.3, la tangente ✔")

print("(2) LA MEDIALUNA PERLIN EN EL PAQUETE (la parte recta DISUELTA, nada bajo el diámetro):")
perlin = hit('Effects/PerlinSemicirculo')
assert dims(perlin) == (256, 256), f"¡PerlinSemicirculo NO es 256²! {dims(perlin)}"
assert len(perlin) == 12 + 256 * 256 * 4, f"¡payload raro! {len(perlin)}"
praw = perlin[12:]
def ppx(x, y):
    o = (y * 256 + x) * 4
    return praw[o], praw[o + 1], praw[o + 2], praw[o + 3]
import math as m
# (a) NADA bajo el diámetro (el rabo de la v6.105 MURIÓ):
vivos_abajo = sum(1 for y in range(152, 256, 2) for x in range(0, 256, 2) if ppx(x, y)[3] > 4)
assert vivos_abajo == 0, f"¡EL RABO VIVE: {vivos_abajo} px bajo el diámetro!"
# (b) LA PARTE RECTA DISUELTA (el corazón de la cúpula es transparencia):
corazon = ppx(128, 142)[3]
assert corazon <= 12, f"¡LA PARTE RECTA MACIZA: alfa del corazón = {corazon}!"
# (c) LA CONCHA VIVA (la medialuna se lee):
concha = ppx(128, 70)[3]
assert concha >= 130, f"¡LA MEDIALUNA VACÍA: alfa de la concha = {concha}!"
# (d) LAS ISOPLETAS CIRCULARES (el alfa sube con el radio por el rayo de 50°):
def alfa_rayo(dd, grados=50):
    th = m.radians(grados)
    return ppx(int(round(128 + dd * m.cos(th))), int(round(150 - dd * m.sin(th))))[3]
a40, a75, a86 = alfa_rayo(40), alfa_rayo(75), alfa_rayo(86)
assert a40 < a75 <= a86 + 8, f"¡ISOPLETAS: d40={a40} d75={a75} d86={a86}!"
# (e) LOS CUERNOS (la concha se ahusa hacia la línea base):
def alfa_pol(dd, grados):
    th = m.radians(grados)
    return ppx(int(round(128 + dd * m.cos(th))), int(round(150 - dd * m.sin(th))))[3]
assert alfa_pol(80, 15) < alfa_pol(80, 60) - 25, "¡LOS CUERNOS no se ahusan!"
# (f) premultiplicado (muestreo):
malos = sum(1 for y in range(0, 256, 4) for x in range(0, 256, 4)
            if (ppx(x, y)[3] == 0 and (ppx(x, y)[0] or ppx(x, y)[1] or ppx(x, y)[2]))
            or (ppx(x, y)[3] > 0 and (ppx(x, y)[0] > ppx(x, y)[3] or ppx(x, y)[1] > ppx(x, y)[3] or ppx(x, y)[2] > ppx(x, y)[3])))
assert malos == 0, f"¡FUERA DE PREMULTIPLICADO ({malos})!"
print(f"    nada bajo el diámetro ✔ · corazón disuelto ({corazon}) ✔ · concha viva ({concha}) ✔")
print(f"    isopletas circulares ({a40}<{a75}≤{a86}) ✔ · cuernos ahusados ✔ · premultiplicado ✔")

print("(3) LO DE LA v6.105 SIGUE INTACTO (sombra 5, ojo estrella, dummies, entregas):")
for f in ['OjosDeEstrella', 'BocaDeColmillos', 'DummyNegroNPC', 'DummyNegroItem']:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
src_dn = open(f'{REPO}/AethonMod/Content/NPCs/DummyNegroNPC.cs', encoding='utf-8').read()
assert 'NPC.chaseable = true' in src_dn and 'NPC.immortal = false' in src_dn
src_vfx = open(f'{REPO}/AethonMod/Content/VFX/VFXCore.cs', encoding='utf-8').read()
assert 'npc.CanBeChasedBy()' in src_vfx
ojo = hit('Effects/OjoAbismo')
assert dims(ojo) == (64, 64)
src_shard = open(f'{REPO}/AethonMod/Content/Players/ShardPlayer.cs', encoding='utf-8').read()
assert 'Accessories.AuraDeJuicio2Item' in src_shard
src_bolsas = open(f'{REPO}/AethonMod/Content/Items/Bolsas/BolsasCategorias.cs', encoding='utf-8').read()
assert 'Accessories.AuraDeJuicio2Item' in src_bolsas
print("    firmas ✔ · dummy negro chaseable+no-inmortal ✔ · CanBeChasedBy ✔ · ojo 64² ✔ · entregas ✔")

print("(4) hjson ×3 — LA REGLA DEL SYNC DE LA CASA (v6.97.1): es-MX = espejo")
print("    byte a byte del es-ES · en-US RESPETADO pero ⊇ es-ES (toda clave")
print("    del es-ES existe en el en-US) · paquete == fuente byte a byte:")
hj = {p: todos[p] for p in todos if p.endswith('.hjson')}
assert len(hj) == 3, hj.keys()
for idioma, archivo in (('es-ES', 'es-ES_Mods.AethonMod.hjson'),
                        ('es-MX', 'es-MX_Mods.AethonMod.hjson'),
                        ('en-US', 'en-US_Mods.AethonMod.hjson')):
    enpkg = [b for p, b in hj.items() if idioma in p][0]
    fuente = open(f'{REPO}/AethonMod/Localization/{archivo}', 'rb').read()
    assert enpkg == fuente, f"¡{idioma} paquete ≠ fuente!"
print("    OK: los 3 hjson paquete == fuente BYTE A BYTE")

def cuerpo(ruta):
    lineas = open(ruta, encoding='utf-8').read().split('\n')
    i = 0
    while i < len(lineas) and (lineas[i].startswith('//') or lineas[i].strip() == ''):
        i += 1
    return lineas[i:]

c_es = cuerpo(f'{REPO}/AethonMod/Localization/es-ES_Mods.AethonMod.hjson')
c_mx = cuerpo(f'{REPO}/AethonMod/Localization/es-MX_Mods.AethonMod.hjson')
assert c_es == c_mx, "¡el es-MX NO es el espejo byte a byte del es-ES!"
print(f"    OK: es-MX espejo exacto del es-ES ({len(c_es)} líneas de cuerpo)")

# EL TOOLTIP NUEVO DEL AURA 2 ×3 (la cúpula de pie + las chispas del corazón):
for idioma, archivo, aguja in (
    ('es-ES', 'es-ES_Mods.AethonMod.hjson', 'NACEN DE SU CORAZÓN'),
    ('es-MX', 'es-MX_Mods.AethonMod.hjson', 'NACEN DE SU CORAZÓN'),
    ('en-US', 'en-US_Mods.AethonMod.hjson', 'BORN FROM ITS HEART')):
    fuente = open(f'{REPO}/AethonMod/Localization/{archivo}', encoding='utf-8').read()
    assert aguja in fuente, f"¡El tooltip nuevo del aura 2 falta en {idioma}!"
    assert 'flanco que te mira' not in fuente or idioma == 'en-US', f"¡El tooltip viejo vive en {idioma}!"
print("    OK: el tooltip de la cúpula + las chispas del corazón vive ×3")

# EL APLANADOR DE RUTAS — soporta tabs Y espacios (el hjson de la casa
# mezcla estilos por sección), claves punteadas en cualquier nivel,
# valores inline y cadenas multilínea '''…'''.
import re as _re
def aplanar(ruta):
    rutas = set(); pila = []; en_ml = False
    for linea in open(ruta, encoding='utf-8'):
        linea = linea.rstrip('\n')
        if en_ml:
            if "'''" in linea: en_ml = False
            continue
        if "'''" in linea:
            en_ml = True
            m = _re.match(r'^(\s*)([\w.]+)\s*:', linea.split("'''")[0])
            if m: rutas.add('.'.join([n for _, n in pila] + m.group(2).split('.')))
            continue
        sin = linea.strip()
        if not sin or sin.startswith('//'): continue
        m = _re.match(r'^(\s*)([\w.]+)\s*:(.*)$', linea)
        if m:
            prof = len(m.group(1))
            while pila and pila[-1][0] >= prof: pila.pop()
            rutas.add('.'.join([n for _, n in pila] + m.group(2).split('.')))
            if m.group(3).strip().endswith('{'): pila.append((prof, m.group(2)))
            continue
        m2 = _re.match(r'^(\s*)\}', linea)
        if m2:
            prof = len(m2.group(1))
            while pila and pila[-1][0] >= prof: pila.pop()
    return rutas

p_es = aplanar(f'{REPO}/AethonMod/Localization/es-ES_Mods.AethonMod.hjson')
p_mx = aplanar(f'{REPO}/AethonMod/Localization/es-MX_Mods.AethonMod.hjson')
p_en = aplanar(f'{REPO}/AethonMod/Localization/en-US_Mods.AethonMod.hjson')
assert p_es == p_mx, f"¡es-ES ≠ es-MX en rutas! {sorted(p_es ^ p_mx)[:8]}"
faltan = p_es - p_en
assert not faltan, f"¡al en-US le faltan claves del es-ES! {sorted(faltan)[:8]}"
print(f"    OK: {len(p_es)} rutas resueltas · es-MX ≡ es-ES · en-US ⊇ es-ES")

# LAS 5 CUERDAS DEL HUD (la brecha cerrada en la v6.106) ×3:
for clave in ('HUD.Nivel', 'HUD.NivelHambre', 'HUD.XP', 'HUD.XPHM', 'HUD.Ganancia'):
    assert clave in p_es and clave in p_mx and clave in p_en, f"¡{clave} no vive ×3!"
print("    OK: las 5 cuerdas del HUD (la brecha v6.106) viven ×3")

print("(5) LA LEY DE LA CASA (0 IP y 0 CJK):")
ips_pkg = re.compile(r're:?zero|ousagi|daphne|subaru|roswaal|calamity|warbanner', re.I)
ips_cs = re.compile(r're:?zero|ousagi|daphne|subaru|roswaal', re.I)
cjk = re.compile('[\u4e00-\u9fff\u3040-\u30ff\uac00-\ud7af]')
sucios = [p for p in todos if p.endswith('.hjson') and
          (ips_pkg.search(todos[p].decode('utf-8', 'replace')) or cjk.search(todos[p].decode('utf-8', 'replace')))]
assert not sucios, sucios
archivos = globmod.glob(f'{REPO}/AethonMod/**/*.cs', recursive=True) + \
           globmod.glob(f'{REPO}/AethonMod/**/*.hjson', recursive=True)
assert len(archivos) == 453, f"¡{len(archivos)} archivos (esperados 453 — murió el arco, nació la expulsión)!"
malos_ip = [f for f in archivos if ips_cs.search(open(f, encoding='utf-8', errors='replace').read())]
assert not malos_ip, malos_ip
malos_cjk = [f for f in archivos if cjk.search(open(f, encoding='utf-8', errors='replace').read())]
assert not malos_cjk, malos_cjk
print(f"    0 IP en {len(archivos)} archivos del árbol · 0 CJK · paquete limpio")

print()
print("=" * 64)
print(f"AUDIT v6.107 TODO VERIFICADO — {len(todos)} entradas, EOF exacto {fsize:,} B")
print(f"md5 {hashlib.md5(d).hexdigest()}")
print("=" * 64)
