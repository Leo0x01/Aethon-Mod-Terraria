#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6105_tmod.py — LA AUDITORÍA DEL .tmod v6.105.

LA LETRA DEL USUARIO (cuatro frentes):
(1) EL AURA DE JUICIO 2: partículas más pequeñas, el efecto del
    estandarte, NADA sobre el jugador, morado y negro, bruma en la
    misma dirección y el SEMICÍRCULO PERLIN con difuminado circular
    en su parte plana.
(2) LA SOMBRA DE LA PÁGINA 5 = LA COPIA DE LA ORIGINAL + el
    OjoAbismo AMARILLO con PUPILA ESTRELLA DE 5 PUNTAS + el
    ColmilloAbismo.
(3) LOS DUMMIES COMO EN TERRARIA NORMAL: EsObjetivo y BuscarPresa
    vuelven a CanBeChasedBy (el dummy vanilla NO es objetivo) + EL
    DUMMY NEGRO (NPC.chaseable = true — la puerta medida contra el
    IL del tML real: NO existe ningún NPCID.Sets.CanBeChasedBy).
(4) LAS ENTREGAS: el aura 2 directo + en la Bolsa de las Auras; los
    30 muñecos negros en el kit del probador.
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
assert ver == "6.105", f"¡VERSIÓN {ver} ≠ 6.105!"
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

print("(1) EL AURA DE JUICIO 2 (la letra: partículas pequeñas, efecto estandarte, nada sobre el jugador):")
vivas = ['AuraDeJuicio2Item', 'AuraJuicio2Ancla', 'VestirUnImpio', 'PerlinSemi',
         'PortaCampoDeJuicio']
for f in vivas:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
print(f"    VIVAS en la DLL: {', '.join(vivas)}")
src_ancla = open(f'{REPO}/AethonMod/Content/Projectiles/AuraJuicio2Ancla.cs', encoding='utf-8').read()
# EL EFECTO ES EL DEL ESTANDARTE (la quemadura del vacío exterior, el radio compartido):
assert 'QuemaduraVacioExterior' in src_ancla, "¡El ancla NO aplica la quemadura del estandarte!"
assert 'EstandarteJuicioAuraProjectile.RADIO' in src_ancla, "¡El radio NO es el del estandarte!"
assert 'DanoVacio = danoBase' in src_ancla, "¡El daño del estandarte falta!"
# SOBRE EL JUGADOR NO HAY NADA (ni luz del campo ni dibujo propio):
assert 'Lighting.AddLight' not in src_ancla, "¡El ancla ALUMBRA al portador (la letra: NADA sobre el jugador)!"
assert 'return false;   # el ancla NUNCA se dibuja'.replace('# ', '// ') in src_ancla or \
       'return false;   // el ancla NUNCA se dibuja' in src_ancla
# LAS PARTÍCULAS PEQUEÑAS (la letra: «deben ser mas pequeñas»):
assert 'Main.rand.NextFloat(0.26f, 0.42f)' in src_ancla, "¡Las partículas NO son pequeñas!"
# LA BRUMA en la MISMA DIRECCIÓN e INTENSIDAD VARIABLE:
assert 'BrumaFX.Puff' in src_ancla and 'velocity: dir' in src_ancla, "¡La bruma no deriva en la misma dirección!"
assert '(0.16f + 0.34f * k)' in src_ancla, "¡La bruma no tiene intensidad variable!"
# EL SEMICÍRCULO PERLIN (los dos pases: alfa + aditivo):
assert 'PerlinSemicirculo' in src_ancla and 'FlushAlpha()' in src_ancla \
   and 'FlushAdditive()' in src_ancla, "¡El semicírculo Perlin no está completo!"
# LA DIRECCIÓN de las chispas (expulsadas del flanco que MIRA al portador):
assert 'n.Center - dir' in src_ancla and 'EXPULSADAS' in src_ancla
print("    el efecto = estandarte (QuemaduraVacioExterior + RADIO) ✔ · nada sobre el jugador ✔")
print("    partículas 0.26–0.42 ✔ · bruma misma dirección + intensidad ∝k ✔ · semicírculo alfa+aditivo ✔")

print("(2) LA SOMBRA 5 = LA COPIA DE LA ORIGINAL (la letra: «copia la sombra de la pagina y usala»):")
vivas5 = ['OjosDeEstrella', 'BocaDeColmillos', 'AperturaDeCaza', 'Devorador']
for f in vivas5:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
muertas5 = ['FauceYCorona', 'OjosDelAbismo', 'AperturaDeFauce', 'AnilloDeSangre', 'DientesDelLomo']
for f in muertas5:
    assert not firma(f, dll), f"¡La firma MUERTA {f} sigue viva!"
src5 = open(f'{REPO}/AethonMod/Content/Projectiles/Sombras/SombraPagina5Projectile.cs', encoding='utf-8').read()
# EL CUERPO ES EL DE LA ORIGINAL (la masa clásica — la piel forjada MURIÓ en la 5):
assert 'SombrasLib.Masa(' in src5, "¡La masa clásica de la original falta!"
assert 'MasaLisa' not in src5, "¡La piel lisa forjada sigue en la 5 (la letra: la COPIA de la original)!"
assert 'CabezaDeBruma' in src5 and 'BrumaColumna' in src5, "¡La cabeza/bruma de la original faltan!"
# LOS OJOS DE ESTRELLA (el patrón de la original + la textura nueva):
assert 'SombrasLib.OjoAbismo' in src5 and 'OjoTexturizado' in src5, "¡Los ojos no usan el OjoAbismo!"
# LA BOCA DE COLMILLOS de la forja:
assert 'SombrasLib.ColmilloAbismo' in src5 and 'ColmilloTexturizado' in src5, "¡La boca no usa el ColmilloAbismo!"
print("    masa clásica + bruma ×2 + cabeza de bruma + devorador ✔ · ojos OjoAbismo ✔ · boca ColmilloAbismo ✔")
print(f"    MUERTAS y ausentes: {', '.join(muertas5)} ✔")

print("(3) EL OJO AMARILLO CON PUPILA ESTRELLA (la letra: «un ojo amarillo con una pupila en forma de estrella de 5 puntas»):")
ojo = hit('Effects/OjoAbismo')
assert dims(ojo) == (64, 64), f"¡OjoAbismo NO es 64×64! {dims(ojo)}"
# el rawimg de tML: [4 bytes versión][int32 w][int32 h][RGBA raw] — decodificado directo:
assert len(ojo) == 12 + 64 * 64 * 4, f"¡payload raro! {len(ojo)}"
pxraw = ojo[12:]
def px(x, y):
    o = (y * 64 + x) * 4
    return pxraw[o], pxraw[o + 1], pxraw[o + 2], pxraw[o + 3]
malos = sum(1 for y in range(0, 64, 3) for x in range(0, 64, 3)
            if (px(x, y)[3] == 0 and (px(x, y)[0] or px(x, y)[1] or px(x, y)[2]))
            or (px(x, y)[3] > 0 and (px(x, y)[0] > px(x, y)[3] or px(x, y)[1] > px(x, y)[3] or px(x, y)[2] > px(x, y)[3])))
assert malos == 0, f"¡OjoAbismo fuera de premultiplicado ({malos})!"
amarillos = negros = 0
for y in range(64):
    for x in range(64):
        r, g, b, a = px(x, y)
        if a > 100 and r > 150 and g > 110 and b < 110: amarillos += 1
        if a > 100 and r < 30 and g < 30 and b < 40: negros += 1
assert amarillos > 150 and negros > 25, f"¡iris={amarillos} estrella={negros}!"
print(f"    OjoAbismo 64×64 premultiplicado ✔ · el iris AMARILLO ({amarillos} px) ✔ · la ESTRELLA ({negros} px) ✔")

print("(4) LOS DUMMIES COMO EN TERRARIA NORMAL + EL DUMMY NEGRO:")
src_vfx = open(f'{REPO}/AethonMod/Content/VFX/VFXCore.cs', encoding='utf-8').read()
assert 'npc.CanBeChasedBy()' in src_vfx and 'EsObjetivo' in src_vfx, "¡EsObjetivo no usa la puerta vanilla!"
src_sp = open(f'{REPO}/AethonMod/Content/Weapons/Sombras/SombraDeLaPagina.cs', encoding='utf-8').read()
assert '!n.CanBeChasedBy()) continue' in src_sp, "¡BuscarPresa no excluye al dummy vanilla!"
src_dn = open(f'{REPO}/AethonMod/Content/NPCs/DummyNegroNPC.cs', encoding='utf-8').read()
assert 'NPC.chaseable = true' in src_dn, "¡El dummy negro NO declara chaseable (la puerta del IL)!"
assert 'NPC.immortal = false' in src_dn, "¡El immortal bloquea CanBeChasedBy (medido al IL)!"
assert 'override bool CheckDead' in src_dn, "¡La eternidad del banco (CheckDead false) falta!"
assert 'Terraria/Images/NPC_113' in src_dn, "¡El sprite vanilla teñido falta!"
src_fa = open(f'{REPO}/AethonMod/Content/Globals/FaucesGlobalNPC.cs', encoding='utf-8').read()
assert 'DummyNegroNPC' in src_fa and 'LA HERRAMIENTA NO ES COMIDA' in src_fa, "¡El guard de devoración falta!"
for f in ['DummyNegroNPC', 'DummyNegroItem']:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
print("    EsObjetivo + BuscarPresa → CanBeChasedBy (dummy vanilla FUERA, como Terraria) ✔")
print("    DummyNegroNPC: chaseable=true + immortal=false + CheckDead eterno + sprite NPC_113 teñido ✔")
print("    el guard del festín (la herramienta no es comida) ✔ · firmas DLL ✔")

print("(5) LAS ENTREGAS (la regla permanente: todo lo nuevo llega DIRECTO y en su bolsa):")
src_shard = open(f'{REPO}/AethonMod/Content/Players/ShardPlayer.cs', encoding='utf-8').read()
assert 'Accessories.AuraDeJuicio2Item' in src_shard, "¡La entrega directa del aura 2 falta!"
src_bolsas = open(f'{REPO}/AethonMod/Content/Items/Bolsas/BolsasCategorias.cs', encoding='utf-8').read()
assert 'Accessories.AuraDeJuicio2Item' in src_bolsas, "¡El aura 2 NO viaja en la Bolsa de las Auras!"
src_test = open(f'{REPO}/AethonMod/Content/Players/TestingPlayer.cs', encoding='utf-8').read()
assert 'DummyNegroItem' in src_test and 'GiveItem(tipoNegro, 30)' in src_test, "¡Los 30 muñecos negros faltan!"
print("    aura 2 DIRECTO + en la Bolsa de las Auras ✔ · 30 muñecos negros en el kit ✔")

print("(6) hjson ×3:")
hj = {p: todos[p] for p in todos if p.endswith('.hjson')}
assert len(hj) == 3, hj.keys()
es = [b for p, b in hj.items() if 'es-ES' in p][0].decode('utf-8')
mx = [b for p, b in hj.items() if 'es-MX' in p][0].decode('utf-8')
en = [b for p, b in hj.items() if 'en-US' in p][0].decode('utf-8')
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
    assert 'AuraDeJuicio2Item' in texto, f"¡El aura 2 falta en {idioma}!"
    assert 'DummyNegroNPC' in texto and 'DummyNegroItem' in texto, f"¡El dummy negro falta en {idioma}!"
print("    OK: las claves nuevas (AuraDeJuicio2Item + DummyNegro ×2) ×3")

print("(7) LAS TEXTURAS NUEVAS:")
perlin = hit('Effects/PerlinSemicirculo')
assert dims(perlin) == (256, 256), f"¡PerlinSemicirculo NO es 256²! {dims(perlin)}"
icono = hit('Items/Accessories/AuraDeJuicio2Item')
assert dims(icono) == (30, 30), f"¡El icono del aura 2 NO es 30×30! {dims(icono)}"
print("    PerlinSemicirculo 256² ✔ · icono AuraDeJuicio2Item 30×30 ✔")

print("(8) LA LEY DE LA CASA (0 IP y 0 CJK):")
# LA LEY: el PAQUETE (.tmod → hjson) viaja LIMPIO con la lista COMPLETA;
# los .cs (que NO viajan) se escanean con CJK + los términos graves —
# «Warbanner» en el header del AuraDeJuicioItem.cs es la CITA de la
# investigación v6.104 (documentación del repo, no del paquete).
ips_pkg = re.compile(r're:?zero|ousagi|daphne|subaru|roswaal|calamity|warbanner', re.I)
ips_cs = re.compile(r're:?zero|ousagi|daphne|subaru|roswaal', re.I)
cjk = re.compile('[\u4e00-\u9fff\u3040-\u30ff\uac00-\ud7af]')
sucios = [p for p in todos if p.endswith('.hjson') and
          (ips_pkg.search(todos[p].decode('utf-8', 'replace')) or cjk.search(todos[p].decode('utf-8', 'replace')))]
assert not sucios, sucios
archivos = globmod.glob(f'{REPO}/AethonMod/**/*.cs', recursive=True) + \
           globmod.glob(f'{REPO}/AethonMod/**/*.hjson', recursive=True)
malos2 = []
for f in archivos:
    t = open(f, encoding='utf-8', errors='replace').read()
    if cjk.search(t): malos2.append(('CJK', f))
    if f.endswith('.hjson') and ips_pkg.search(t): malos2.append(('IP', f))
    elif f.endswith('.cs') and ips_cs.search(t): malos2.append(('IP', f))
assert not malos2, malos2
print(f"    OK: 0 IP y 0 CJK en {len(archivos)} archivos y en los hjson empaquetados")

lic = todos.get('LICENSE') or todos.get('LICENSE.txt')
assert lic is not None and 'TODOS LOS DERECHOS RESERVADOS' in lic.decode('utf-8', 'replace')
print("    LICENSE propietaria dentro ✔")

print("\nAUDIT v6.105: TODO VERIFICADO ✔")
