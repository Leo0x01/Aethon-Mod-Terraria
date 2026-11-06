#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6108_tmod.py — LA AUDITORÍA DEL .tmod v6.108 — EL CALENTAMIENTO
AERODINÁMICO.

La letra del usuario: «la parte abombada de la medialuna tiene que
mirarme a mi en todo momento, deben nacer del centro de la medialuna en
la parte abombada y extenderse en todas las direcciones pero siempre
alejandose de mi, a mayor cercania mas particulas y la medialuna cambia
de color volviendose ligeramente morada negra, y la bruma debe hacer lo
mismo que las particulas» — el portador es el PLANETA, su aura la
ATMÓSFERA y el impío cae en REENTRADA.
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
assert ver == "6.108", f"¡VERSIÓN {ver} ≠ 6.108!"
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

print("(1) EL CALENTAMIENTO AERODINÁMICO v6.108 — EL ARCO DE CHOQUE + EL PLASMA:")
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

# LA MEDIALUNA = EL ARCO DE CHOQUE — la CONCHA ABOMBADA MIRA AL PORTADOR
# EN TODO MOMENTO (rot = dir − π/2: el impío siempre «cae hacia ti»):
assert 'float rot = dir.ToRotation() - MathHelper.PiOver2' in src_ancla, \
    "¡LA CONCHA NO MIRA AL PORTADOR (rot = dir − π/2)!"
assert 'dir.ToRotation() + MathHelper.PiOver2' not in src_ancla, "¡La inclinación v6.106 sigue VIVA!"
assert 'Vector2 pos = n.Center - baseTex' in src_ancla, "¡El centro del semicírculo NO cae sobre el corazón!"
# EL COLOR — la medialuna SE ENFRÍA EN SOMBRA con la cercanía (morada negra):
assert 'Color moradaNegra = new(46, 20, 78)' in src_ancla, "¡El morado negro falta!"
assert 'Color.Lerp(negroVioleta, moradaNegra, k)' in src_ancla, "¡El pase alfa NO se oscurece con la cercanía!"
assert 'Color.Lerp(violeta, moradaNegra, 0.85f * k)' in src_ancla, "¡El brillo NO se apaga hacia el morado negro!"
assert 'colorAlfa * ((0.42f + 0.30f * k) * vive)' in src_ancla and 'FlushAlpha()' in src_ancla
assert 'colorLuz * ((0.26f + 0.26f * k) * vive)' in src_ancla and 'FlushAdditive()' in src_ancla
assert 'tamEco' in src_ancla, "¡El eco del choque falta!"

# EL PLASMA NACE DEL PUNTO DE ESTANCAMIENTO (el tope abombado — el punto
# del arco que MIRA al portador) y se derrama hacia ATRÁS en TODAS las
# direcciones de fuga (el abanico trasero COMPLETO ±90°):
assert 'Vector2 cuna = n.Center - dir * (RadioDomo(n, k) * 0.95f)' in src_ancla, \
    "¡Las chispas NO nacen del punto de estancamiento (el tope abombado)!"
assert 'Main.rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2)' in src_ancla, \
    "¡El abanico de fuga ±90° (todas las direcciones que se alejan) falta!"
assert 'dir.RotatedBy(abanico)' in src_ancla, "¡El rumbo de fuga falta!"
assert 'customData = chispa' in src_ancla, "¡Las chispas NO viajan liberadas!"
assert 'Centro = cuna' in src_ancla and 'Dir = rumbo' in src_ancla, "¡La geometría congelada al nacer falta!"
# A MAYOR CERCANÍA, MÁS PARTÍCULAS (la densidad de la atmósfera):
assert 'MathHelper.Clamp(6f - k * 5f, 1f, 6f)' in src_ancla, "¡El ritmo NO escala 6→1 con la cercanía!"
assert 'int rafaga = 1 + (k > 0.4f ? 1 : 0)' in src_ancla, "¡Las ráfagas NO crecen con la cercanía!"
assert 'Main.rand.NextFloat(1.6f, 2.6f)' in src_ancla, "¡La estela de la reentrada (1.6–2.6) falta!"
assert 'Main.rand.NextFloat(0.24f, 0.38f)' in src_ancla, "¡Las chispas NO son menudas!"
assert src_ancla.count('RadioDomo(n, k)') >= 5, "¡El teatro NO comparte la misma cúpula (RadioDomo)!"

# EL POLVO DEL PLASMA — el vuelo recto del estancamiento, aceleración viva:
assert 'dust.position = c.Centro + c.Dir * c.Dist' in src_dust, "¡El polvo NO vuela recto!"
assert 'MathF.Min(c.Vel * (1f + c.Acel)' in src_dust, "¡El polvo del plasma NO acelera!"
assert 'dust.rotation = c.Dir.ToRotation()' in src_dust, "¡La estela NO apunta por donde vuela!"
assert 'return false;   // TODO manual' in src_dust, "¡La física vanilla no está desactivada!"

# LA BRUMA HACE LO MISMO QUE LAS PARTÍCULAS — nace del PUNTO DE
# ESTANCAMIENTO, abanico de fuga ±90° y MÁS Densa con la cercanía:
assert 'Vector2 pos = n.Center - dir * (RadioDomo(n, k) * 0.95f) + rumbo * avance' in src_ancla, \
    "¡La bruma NO nace del punto de estancamiento!"
assert '(Frac(f0 * 13.7f) * 2f - 1f) * MathHelper.PiOver2' in src_ancla, "¡La bruma NO usa el abanico de fuga ±90°!"
assert 'velocity: rumbo *' in src_ancla, "¡La bruma NO se derrama por el rumbo de fuga!"
assert 'int puffs = 2 + (int)(k * 3f)' in src_ancla, "¡La bruma NO crece 2→5 con la cercanía!"
assert '(0.16f + 0.34f * k)' in src_ancla, "¡La bruma no tiene intensidad variable!"

# LO VIEJO MUERTO (la cúpula vertical fija y la expulsión del corazón):
assert 'NextFloat(-0.85f, 0.85f)' not in src_ancla, "¡El abanico ±0.85 de la v6.107 sigue VIVO!"
assert '* 0.85f)\n' not in src_ancla.replace('(Frac(f0 * 13.7f) * 2f - 1f) * 0.85f', ''), "¡Restos del abanico viejo!"
assert 'int puffs = 3 + (k > 0.6f ? 1 : 0)' not in src_ancla, "¡Los puffs viejos siguen VIVOS!"
assert 'Clamp(4f - k * 2f, 1f, 4f)' not in src_ancla, "¡El ritmo viejo sigue VIVO!"
assert 'Vector2 pos = n.Center + rumbo * avance' not in src_ancla, "¡La bruma del corazón sigue VIVA!"
assert 'NextFloat(1.35f, 2.1f)' not in src_ancla, "¡La estela corta vieja sigue VIVA!"
assert 'PolvoArcoJuicio' not in src_ancla and not firma('PolvoArcoJuicio', dll), "¡El polvo del arco viejo VIVE!"
assert 'phi - MathHelper.PiOver2' not in src_ancla, "¡Los cuernos del arco siguen vivos!"
assert 'AngFin' not in src_ancla and 'VelAng' not in src_ancla, "¡La geometría angular del arco sigue viva!"

print("    medialuna: EL ARCO DE CHOQUE con la concha MIRANDO al portador ✔ (rot = dir − π/2)")
print("    medialuna: SE ENFRÍA en MORADA NEGRA con la cercanía ✔ (alfa + aditivo + eco)")
print("    chispas: NACEN DEL PUNTO DE ESTANCAMIENTO (el tope abombado) ✔")
print("    chispas: abanico de fuga COMPLETO ±90° (todas las que se alejan) ✔ · liberadas ✔")
print("    chispas: DENSIDAD ∝ cercanía (ritmo 6→1, ráfagas 1→3) ✔ · estela 1.6–2.6 ✔")
print("    bruma: LO MISMO que las chispas ✔ (estancamiento + ±90° + puffs 2→5 + intensidad ∝k)")
print("    MUERTAS y ausentes: el abanico ±0.85, el ritmo 4→2, los puffs 3+1, la bruma del corazón ✔")

print("(2) LA MEDIALUNA PERLIN EN EL PAQUETE (la parte recta DISUELTA, nada bajo el diámetro):")
perlin = hit('Effects/PerlinSemicirculo')
assert dims(perlin) == (256, 256), f"¡PerlinSemicirculo NO es 256²! {dims(perlin)}"
assert len(perlin) == 12 + 256 * 256 * 4, f"¡payload raro! {len(perlin)}"
praw = perlin[12:]
def ppx(x, y):
    o = (y * 256 + x) * 4
    return praw[o], praw[o + 1], praw[o + 2], praw[o + 3]
import math as m
vivos_abajo = sum(1 for y in range(152, 256, 2) for x in range(0, 256, 2) if ppx(x, y)[3] > 4)
assert vivos_abajo == 0, f"¡EL RABO VIVE: {vivos_abajo} px bajo el diámetro!"
corazon = ppx(128, 142)[3]
assert corazon <= 12, f"¡LA PARTE RECTA MACIZA: alfa del corazón = {corazon}!"
concha = ppx(128, 70)[3]
assert concha >= 130, f"¡LA MEDIALUNA VACÍA: alfa de la concha = {concha}!"
def alfa_rayo(dd, grados=50):
    th = m.radians(grados)
    return ppx(int(round(128 + dd * m.cos(th))), int(round(150 - dd * m.sin(th))))[3]
a40, a75, a86 = alfa_rayo(40), alfa_rayo(75), alfa_rayo(86)
assert a40 < a75 <= a86 + 8, f"¡ISOPLETAS: d40={a40} d75={a75} d86={a86}!"
def alfa_pol(dd, grados):
    th = m.radians(grados)
    return ppx(int(round(128 + dd * m.cos(th))), int(round(150 - dd * m.sin(th))))[3]
assert alfa_pol(80, 15) < alfa_pol(80, 60) - 25, "¡LOS CUERNOS no se ahusan!"
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
print("    byte a byte del es-ES · en-US RESPETADO pero ⊇ es-ES · paquete == fuente:")
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

# EL TOOLTIP NUEVO DEL AURA 2 ×3 (el calentamiento aerodinámico):
for idioma, archivo, aguja in (
    ('es-ES', 'es-ES_Mods.AethonMod.hjson', 'CALENTAMIENTO AERODINÁMICO'),
    ('es-MX', 'es-MX_Mods.AethonMod.hjson', 'CALENTAMIENTO AERODINÁMICO'),
    ('en-US', 'en-US_Mods.AethonMod.hjson', 'AERODYNAMIC HEATING')):
    fuente = open(f'{REPO}/AethonMod/Localization/{archivo}', encoding='utf-8').read()
    assert aguja in fuente, f"¡El tooltip del calentamiento aerodinámico falta en {idioma}!"
for idioma, archivo in (('es-ES', 'es-ES_Mods.AethonMod.hjson'),
                        ('es-MX', 'es-MX_Mods.AethonMod.hjson'),
                        ('en-US', 'en-US_Mods.AethonMod.hjson')):
    fuente = open(f'{REPO}/AethonMod/Localization/{archivo}', encoding='utf-8').read()
    assert 'NACEN DE SU CORAZÓN' not in fuente and 'BORN FROM ITS HEART' not in fuente, \
        f"¡El tooltip viejo de la cúpula vertical vive en {idioma}!"
    assert 'ARCO DE CHOQUE' in fuente or 'SHOCK ARC' in fuente, f"¡El arco de choque falta en {idioma}!"
print("    OK: el tooltip del CALENTAMIENTO AERODINÁMICO vive ×3 (y el viejo murió ×3)")

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
assert len(archivos) == 453, f"¡{len(archivos)} archivos (esperados 453)!"
malos_ip = [f for f in archivos if ips_cs.search(open(f, encoding='utf-8', errors='replace').read())]
assert not malos_ip, malos_ip
malos_cjk = [f for f in archivos if cjk.search(open(f, encoding='utf-8', errors='replace').read())]
assert not malos_cjk, malos_cjk
print(f"    0 IP en {len(archivos)} archivos del árbol · 0 CJK · paquete limpio")

print()
print("=" * 64)
print(f"AUDIT v6.108 TODO VERIFICADO — {len(todos)} entradas, EOF exacto {fsize:,} B")
print(f"md5 {hashlib.md5(d).hexdigest()}")
print("=" * 64)
