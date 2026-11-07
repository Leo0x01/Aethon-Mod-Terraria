#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6109_tmod.py — LA AUDITORÍA DEL .tmod v6.110 — EL AURA DEL
VACÍO + LOS FANTASMAS QUE ARDEN + LA BRUMA DE VERDAD + LAS DOS LUCES
NUEVAS (El Escriba de la Luz y La Estrella Viva).

La letra del usuario: «ya puedes borrar los otros 2 estandarte y
renombrar a EL AURA DE JUICIO 2 como Aura de Vacio, pero todavia falta
algo EL AURA DE JUICIO 2 aunque esta bien no veo bruma le falta eso» +
«al parecer con el enemigo que es un fantasma el aura no funciona es
curioso jajajaja» + «ahora debes mejorar la forma ascendida 5 y crear
otras las forma ascendidas, intenta ser creativo y puedes tomar como
ejemplo a auras en internet y a los mismos angeles».
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
assert ver == "6.110", f"¡VERSIÓN {ver} ≠ 6.110!"
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

# =====================================================================
print("(1) EL AURA DEL VACÍO — el renacer (los otros 2 estandartes MURIERON):")
vivas = ['AuraDeJuicio2Item', 'AuraJuicio2Ancla', 'VestirUnImpio', 'PerlinSemi',
         'PortaCampoDeJuicio', 'PolvoExpulsionJuicio', 'RadioDomo', 'QuemaduraVacioExterior']
for f in vivas:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
print(f"    VIVAS en la DLL: {', '.join(vivas)}")

muertas = ['AuraDeJuicioItem', 'EstandarteDelJuicioItem', 'JuicioPlayer',
           'AuraJuicioGlobalNPC', 'AuraJuicioMetroSistema', 'LlamasDelJuicio',
           'PolvoLuzLuciernaga', 'EstandarteBannerDrawLayer', 'AuraJuicioLuzProjectile',
           'AuraJuicioDanoProjectile', 'EstandarteJuicioAuraProjectile']
for f in muertas:
    assert not firma(f, dll), f"¡{f} SIGUE VIVA en la DLL!"
print(f"    MUERTAS y ausentes de la DLL: {', '.join(muertas)}")

# las texturas de los muertos TAMBIÉN fuera del paquete:
for tex in ('AuraDeJuicioItem', 'EstandarteDelJuicioItem', 'EstandarteBannerTrasero',
            'CampoVacio', 'FrenteVacio', 'LlamasDelJuicio', 'PolvoLuzLuciernaga'):
    h = [p for p in todos if tex in p]
    assert not h, f"¡La textura de {tex} sigue en el paquete: {h}!"
print("    sus TEXTURAS también fuera del paquete ✔")

src_ancla = open(f'{REPO}/AethonMod/Content/Projectiles/AuraJuicio2Ancla.cs', encoding='utf-8').read()
src_dust = open(f'{REPO}/AethonMod/Content/Dusts/PolvoExpulsionJuicio.cs', encoding='utf-8').read()
src_vacio = open(f'{REPO}/AethonMod/Content/Globals/VacioGlobalNPC.cs', encoding='utf-8').read()
src_lib = open(f'{REPO}/AethonMod/Content/VFX/AuraLib.cs', encoding='utf-8').read()
src_cp = open(f'{REPO}/AethonMod/Content/Players/CosmeticPlayer.cs', encoding='utf-8').read()
src_halo = open(f'{REPO}/AethonMod/Content/Projectiles/Cosmetic/AuraPortadorHalo.cs', encoding='utf-8').read()
src_item2 = open(f'{REPO}/AethonMod/Content/Items/Accessories/AuraDeJuicio2Item.cs', encoding='utf-8').read()

# EL RADIO ya es del ancla (el estandarte murió y se lo llevó su constante):
assert 'public const float RADIO = 300f' in src_ancla, "¡El RADIO no vive en el ancla!"
assert 'EstandarteJuicioAuraProjectile.RADIO' not in src_ancla.replace(
    'vivía en el EstandarteJuicioAuraProjectile, que MURIÓ', ''), "¡El RADIO viejo sigue referenciado!"
assert 'AuraJuicio2Ancla.RADIO' in src_vacio, "¡VacioGlobalNPC no lee el RADIO del ancla!"
# el vestido del estandarte MURIÓ con él (el motor común queda limpio):
assert 'DibujarElFrenteYElDestello' not in src_vacio, "¡El vestido del estandarte sigue VIVO!"
assert 'EstandarteJuicio' not in src_vacio.replace('el estandarte y el aura de juicio', ''), \
    "¡VacioGlobalNPC sigue mirando a JuicioPlayer!"
# el tooltip y el título del ítem renacieron:
assert 'EL AURA DEL VACÍO' in src_item2 and 'Aura de Juicio II' not in src_item2
# SOBRE EL JUGADOR NO HAY NADA (el ancla NO alumbra al portador):
assert 'Lighting.AddLight' not in src_ancla, "¡El ancla ALUMBRA al portador!"
print("    RADIO propio ✔ · VacioGlobalNPC limpio ✔ · el ítem renacido ✔ · el ancla sigue callada ✔")

# =====================================================================
print("(2) EL FANTASMA TAMBIÉN ARDE (la curiosidad resuelta):")
assert 'bool esEspiritu = n.aiStyle == 23 || n.type == NPCID.Ghost' in src_ancla, \
    "¡La familia de los espíritus NO entra al campo!"
assert 'n.dontTakeDamage) && !esEspiritu' in src_ancla, "¡La excepción espiritual no está cableada!"
assert 'CanBeChasedBy' in src_ancla, "¡La fórmula vanilla documentada falta!"
# la lentitud también pisa a los espíritus (el motor común, sin exclusiones nuevas):
assert 'LA LENTITUD DE VERDAD' in src_vacio or 'el freno del vacío pisa DESPUÉS' in src_vacio
print("    aiStyle 23 + NPCID.Ghost entran al campo ✔ · la puerta vanilla documentada ✔")

# =====================================================================
print("(3) LA BRUMA DE VERDAD (la letra: «no veo bruma le falta eso»):")
assert 'int puffs = 3 + (int)(k * 4f)' in src_ancla, "¡La bruma NO crece 3→7 con la cercanía!"
assert '(0.40f + 0.34f * k)' in src_ancla, "¡El alfa de la bruma NO es de verdad!"
assert 'MathHelper.Clamp(a, 0.14f, 0.80f)' in src_ancla, "¡El rango del alfa tímido sigue!"
assert '(122, 78, 188)' in src_ancla, "¡La rampa violácea clara falta!"
assert 'quality: 0.75f' in src_ancla, "¡La calidad de los puffs no subió!"
assert 'Color violetaAliento' in src_ancla and 'VFXCore.SoftGlow' in src_ancla, \
    "¡EL ALIENTO VIOLETA (la segunda capa aditiva) falta!"
assert 'LA LLUVIA DE FOTONES' not in src_ancla, "¡Sangrado de la forma 7!"
# lo viejo MUERTO:
assert 'int puffs = 2 + (int)(k * 3f)' not in src_ancla, "¡Los puffs viejos siguen VIVOS!"
assert '(0.16f + 0.34f * k)' not in src_ancla, "¡El alfa tímido sigue VIVO!"
assert 'quality: 0.5f' not in src_ancla, "¡La calidad vieja sigue VIVA!"
print("    puffs 3→7 ✔ · alfa 0.14..0.80 ✔ · rampa violácea ✔ · EL ALIENTO VIOLETA aditivo ✔")

# =====================================================================
print("(4) LA FORMA 5 MEJORADA (el vórtice + las alas + las ráfagas):")
assert 'Presupuesto(780)' in src_lib, "¡El presupuesto de la forma 5 no creció!"
assert 'EL VÓRTICE' in src_lib and 'espiral de Arquímedes' in src_lib, "¡El vórtice falta!"
assert 'LAS ALAS DE VIENTO' in src_lib, "¡Las alas de viento faltan!"
assert 'LAS RÁFAGAS' in src_lib, "¡Las ráfagas faltan!"
assert 'LA TURBULENCIA (LA FIRMA v6.102 — INTACTA' in src_lib, "¡La firma de la forma 5 murió!"
print("    el presupuesto 780 ✔ · el vórtice ✔ · las alas ✔ · las ráfagas ✔ · la turbulencia intacta ✔")

# =====================================================================
print("(5) LAS DOS LUCES NUEVAS — el Escriba y la Estrella:")
vivas2 = ['FormaAscendidaSeisItem', 'FormaAscendidaSieteItem',
          'FormaAscendidaSeis', 'FormaAscendidaSiete',
          'EmitirDivino6', 'EmitirDivino7', 'DibujarDivino6', 'DibujarDivino7',
          'Divino6', 'Divino7', 'FormaAscendidaCinco']
for f in vivas2:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
print(f"    VIVAS en la DLL: {', '.join(vivas2)}")
# EL ESCRIBA — la geometría sagrada:
for s in ('EL CUBO DE METATRÓN', 'EL HEXAGRAMA DOBLE', 'EL HALO DE LOS GLIFOS',
          'LA LLUVIA DE VERSOS', 'EL SELLO PERFECTO'):
    assert s in src_lib, f"¡{s} falta!"
# LA ESTRELLA — las prominencias:
for s in ('LAS PROMINENCIAS', 'LA CORONA SOLAR', 'EL NÚCLEO BLANCO',
          'EL CUERPO DE PLASMA', 'EL PARPADEO ESTELAR'):
    assert s in src_lib, f"¡{s} falta!"
assert 'el desgarre' in src_ancla.lower() or 'EL DESGARRE' in src_lib, "¡El desgarre de prominencias falta!"
# los despachos y los modos:
assert 'PatronAura.Divino6' in src_lib and 'PatronAura.Divino7' in src_lib, "¡Los patrones no se despachan!"
assert 'case 9:' in src_halo and 'case 10:' in src_halo, "¡Los modos 9/10 no viven en el portador!"
assert 'FormaAscendidaSeis && !EspiarPortador(9)' in src_cp, "¡La forma 6 no invoca su portador!"
assert 'FormaAscendidaSiete && !EspiarPortador(10)' in src_cp, "¡La forma 7 no invoca su portador!"
assert 'FormaAscendidaSeis || FormaAscendidaSiete' in src_cp.replace(
    'FormaAscendidaCuatro || FormaAscendidaCinco || FormaAscendidaSeis ||\n            FormaAscendidaSiete', 'X') or \
    'FormaAscendidaSeis ||\n            FormaAscendidaSiete' in src_cp or 'FormaAscendidaSeis || FormaAscendidaSiete' in src_cp, \
    "¡El vuelo divino no creció!"
assert 'new Vector3(1.62f, 1.30f, 0.92f)' in src_cp, "¡La luz de la estrella falta!"
# los iconos nuevos en el paquete (30×30):
for tex in ('FormaAscendidaSeisItem', 'FormaAscendidaSieteItem'):
    dd = dims(hit(f'Cosmetics/{tex}'))
    assert dd == (30, 30), f"¡{tex} no es 30×30: {dd}!"
print("    El Escriba: cubo + hexagrama + glifos + versos + sello ✔")
print("    La Estrella: prominencias + corona + núcleo + plasma + parpadeo ✔")
print("    modos 9/10 ✔ · vuelo divino ×7 ✔ · la luz nueva ✔ · iconos 30² ✔")

# =====================================================================
print("(6) LA MEDIALUNA PERLIN EN EL PAQUETE (intacta — la firma v6.106):")
perlin = hit('Effects/PerlinSemicirculo')
assert dims(perlin) == (256, 256), f"¡PerlinSemicirculo NO es 256²! {dims(perlin)}"
praw = perlin[12:]
def ppx(x, y):
    o = (y * 256 + x) * 4
    return praw[o], praw[o + 1], praw[o + 2], praw[o + 3]
vivos_abajo = sum(1 for y in range(152, 256, 2) for x in range(0, 256, 2) if ppx(x, y)[3] > 4)
assert vivos_abajo == 0, f"¡EL RABO VIVE: {vivos_abajo} px bajo el diámetro!"
assert ppx(128, 142)[3] <= 12, "¡LA PARTE RECTA MACIZA!"
assert ppx(128, 70)[3] >= 130, "¡LA MEDIALUNA VACÍA!"
print("    nada bajo el diámetro ✔ · corazón disuelto ✔ · concha viva ✔")

# =====================================================================
print("(7) LO DE LA v6.105 SIGUE INTACTO (ojo estrella, dummies, entregas):")
for f in ['OjosDeEstrella', 'BocaDeColmillos', 'DummyNegroNPC', 'DummyNegroItem']:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
src_shard = open(f'{REPO}/AethonMod/Content/Players/ShardPlayer.cs', encoding='utf-8').read()
src_bolsas = open(f'{REPO}/AethonMod/Content/Items/Bolsas/BolsasCategorias.cs', encoding='utf-8').read()
assert 'Accessories.AuraDeJuicio2Item' in src_shard, "¡El aura ya no se entrega!"
assert 'Accessories.AuraDeJuicio2Item' in src_bolsas, "¡El aura salió de su bolsa!"
assert 'Cosmetics.FormaAscendidaSeisItem' in src_bolsas and 'Cosmetics.FormaAscendidaSieteItem' in src_bolsas, \
    "¡Las formas nuevas no viajan en la bolsa!"
assert 'Accessories.EstandarteDelJuicioItem' not in src_bolsas, "¡El estandarte muerto sigue en la bolsa!"
assert 'Accessories.AuraDeJuicioItem' not in src_bolsas, "¡El aura muerta sigue en la bolsa!"
print("    firmas ✔ · el aura en kit+bolsa ✔ · las formas 6/7 en la bolsa ✔ · los muertos fuera ✔")

# =====================================================================
print("(8) hjson ×3 — LA REGLA DEL SYNC DE LA CASA: es-MX = espejo del es-ES · en-US ⊇ es-ES:")
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

# EL RENACIMIENTO ×3: el aura se llama AURA DEL VACÍO:
for idioma, archivo, aguja in (
    ('es-ES', 'es-ES_Mods.AethonMod.hjson', 'DisplayName: Aura del Vacío'),
    ('es-MX', 'es-MX_Mods.AethonMod.hjson', 'DisplayName: Aura del Vacío'),
    ('en-US', 'en-US_Mods.AethonMod.hjson', 'DisplayName: Aura of the Void')):
    fuente = open(f'{REPO}/AethonMod/Localization/{archivo}', encoding='utf-8').read()
    assert aguja in fuente, f"¡El renombre falta en {idioma}!"
    # el nombre viejo NO puede vivir en LÍNEAS ACTIVAS (los comentarios
    # históricos no viajan al juego):
    activas = '\n'.join(l for l in fuente.split('\n') if not l.lstrip().startswith('//'))
    assert 'Aura de Juicio II' not in activas and 'DisplayName: Aura de Juicio' not in activas, \
        f"¡El nombre viejo vive en líneas activas de {idioma}!"
# LOS FANTASMAS en el tooltip ×3:
for idioma, archivo, aguja in (
    ('es-ES', 'es-ES_Mods.AethonMod.hjson', 'hasta los FANTASMAS'),
    ('es-MX', 'es-MX_Mods.AethonMod.hjson', 'hasta los FANTASMAS'),
    ('en-US', 'en-US_Mods.AethonMod.hjson', 'even GHOSTS')):
    fuente = open(f'{REPO}/AethonMod/Localization/{archivo}', encoding='utf-8').read()
    assert aguja in fuente, f"¡Los fantasmas no arden en el tooltip de {idioma}!"
# LAS DOS LUCES ×3:
for idioma, archivo, seis, siete in (
    ('es-ES', 'es-ES_Mods.AethonMod.hjson', 'EL ESCRIBA DE LA LUZ', 'LA ESTRELLA VIVA'),
    ('es-MX', 'es-MX_Mods.AethonMod.hjson', 'EL ESCRIBA DE LA LUZ', 'LA ESTRELLA VIVA'),
    ('en-US', 'en-US_Mods.AethonMod.hjson', 'THE SCRIBE OF LIGHT', 'THE LIVING STAR')):
    fuente = open(f'{REPO}/AethonMod/Localization/{archivo}', encoding='utf-8').read()
    assert seis in fuente, f"¡La forma 6 falta en {idioma}!"
    assert siete in fuente, f"¡La forma 7 falta en {idioma}!"
# LAS CLAVES MUERTAS fuera de los tres:
for idioma, archivo in (('es-ES', 'es-ES_Mods.AethonMod.hjson'),
                        ('es-MX', 'es-MX_Mods.AethonMod.hjson'),
                        ('en-US', 'en-US_Mods.AethonMod.hjson')):
    fuente = open(f'{REPO}/AethonMod/Localization/{archivo}', encoding='utf-8').read()
    for muerto in ('EstandarteDelJuicioItem:', 'AuraDeJuicioItem:', 'LlamasDelJuicio:',
                   'EstandarteJuicioAuraProjectile.DisplayName'):
        assert muerto not in fuente, f"¡La clave muerta {muerto} vive en {idioma}!"
print("    el renombre ×3 ✔ · los fantasMAS del tooltip ×3 ✔ · las dos luces ×3 ✔ · las claves muertas fuera ✔")

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

# =====================================================================
print("(9) LA LEY DE LA CASA (0 IP y 0 CJK):")
ips_pkg = re.compile(r're:?zero|ousagi|daphne|subaru|roswaal|calamity|warbanner', re.I)
ips_cs = re.compile(r're:?zero|ousagi|daphne|subaru|roswaal', re.I)
cjk = re.compile('[\u4e00-\u9fff\u3040-\u30ff\uac00-\ud7af]')
sucios = [p for p in todos if p.endswith('.hjson') and
          (ips_pkg.search(todos[p].decode('utf-8', 'replace')) or cjk.search(todos[p].decode('utf-8', 'replace')))]
assert not sucios, sucios
archivos = globmod.glob(f'{REPO}/AethonMod/**/*.cs', recursive=True) + \
           globmod.glob(f'{REPO}/AethonMod/**/*.hjson', recursive=True)
assert len(archivos) == 447, f"¡{len(archivos)} archivos (esperados 447: 444 de la v6.109 + 3 ítems nuevos de las luces 8/9/10)!"
malos_ip = [f for f in archivos if ips_cs.search(open(f, encoding='utf-8', errors='replace').read())]
assert not malos_ip, malos_ip
malos_cjk = [f for f in archivos if cjk.search(open(f, encoding='utf-8', errors='replace').read())]
assert not malos_cjk, malos_cjk
print(f"    0 IP en {len(archivos)} archivos del árbol · 0 CJK · paquete limpio")

print()
print("=" * 64)
print(f"AUDIT v6.110 TODO VERIFICADO — {len(todos)} entradas, EOF exacto {fsize:,} B")
print(f"md5 {hashlib.md5(d).hexdigest()}")
print("=" * 64)

# ===================== v6.110 — LA DÉCADA COMPLETA =====================
print("(9) v6.110 — LA FORMA 1 SIN ALAS (la letra: «a la forma ascendida 1 quitale las alas»):")
fuente_auralib = open(REPO + '/AethonMod/Content/VFX/AuraLib.cs', encoding='utf-8').read()
assert 'EL BANCO PRIMARIO (8 plumas' not in fuente_auralib, '¡el banco primario de alas VIVE!'
assert 'EL BANCO SECUNDARIO (4 plumas cortas' not in fuente_auralib, '¡el banco secundario VIVE!'
assert 'MUERTAS en la v6.110' in fuente_auralib, 'falta la lápida de las alas'
print("    los DOS bancos de alas MUERTOS · la lápida viva ✔")

print("(10) v6.110 — LAS TRES LUCES NUEVAS (8 · 9 · 10):")
for firma_v in ('FormaAscendidaOchoItem', 'FormaAscendidaNueveItem', 'FormaAscendidaDiezItem',
                'FormaAscendidaOcho', 'FormaAscendidaNueve', 'FormaAscendidaDiez',
                'EmitirDivino8', 'EmitirDivino9', 'EmitirDivino10',
                'DibujarDivino8', 'DibujarDivino9', 'DibujarDivino10',
                'Divino8', 'Divino9', 'Divino10'):
    assert firma(firma_v, dll), f'¡{firma_v} NO vive en la DLL!'
print("    VIVAS en la DLL: los 15 símbolos de las 3 luces ✔")
fuente_al = open(REPO + '/AethonMod/Content/VFX/AuraLib.cs', encoding='utf-8').read()
for firma_t in ('LA GRAN ESPADA DE LUZ', 'LAS DOS BALANZAS DEL JUICIO',
                'LAS ONDAS DEL LLAMADO', 'LA CORTINA DE AURORA',
                'EL ÁRBOL DE LA VIDA', 'LA SECUENCIA DE LA EMANACI',
                'KETER — LA CORONA'):
    assert firma_t in fuente_al, f'¡la firma {firma_t} falta en AuraLib!'
print("    las firmas de texto (espada+balanzas · ondas+aurora · árbol+keter) ✔")

fuente_cp = open(REPO + '/AethonMod/Content/Players/CosmeticPlayer.cs', encoding='utf-8').read()
for marca in ('FormaAscendidaOcho', 'FormaAscendidaNueve', 'FormaAscendidaDiez',
              'whoAmI, 11f', 'whoAmI, 12f', 'whoAmI, 13f'):
    assert marca in fuente_cp, f'¡{marca} falta en CosmeticPlayer!'
print('    el wiring del portador (modos 11/12/13 + vuelo divino ×10) ✔')
fuente_ap = open(REPO + '/AethonMod/Content/Projectiles/Cosmetic/AuraPortadorHalo.cs', encoding='utf-8').read()
assert 'case 11:' in fuente_ap and 'case 12:' in fuente_ap and 'case 13:' in fuente_ap
print('    los modos del portador en el halo ✔')
fuente_sp = open(REPO + '/AethonMod/Content/Players/ShardPlayer.cs', encoding='utf-8').read()
assert 'FormaAscendidaOchoItem' in fuente_sp and 'FormaAscendidaDiezItem' in fuente_sp
print('    la entrega directa de las 3 ✔')
fuente_bolsa = open(REPO + '/AethonMod/Content/Items/Bolsas/BolsasCategorias.cs', encoding='utf-8').read()
assert fuente_bolsa.count('FormaAscendidaOchoItem') == 2
print('    la bolsa ×2 ✔')
for icono in ('FormaAscendidaCincoItem', 'FormaAscendidaSeisItem', 'FormaAscendidaSieteItem',
              'FormaAscendidaOchoItem', 'FormaAscendidaNueveItem', 'FormaAscendidaDiezItem'):
    hit('Cosmetics/' + icono)
    w, h = dims(todos['Content/Items/Cosmetics/' + icono + '.rawimg'])
    assert (w, h) == (30, 30), f'¡{icono} mide {w}x{h}!'
print('    los 6 iconos 30×30 en el paquete ✔')

print("(11) v6.110 — EL TOOLTIP CORTO del Aura del Vacío:")
for archivo in ('es-ES', 'es-MX'):
    t = open(REPO + f'/AethonMod/Localization/{archivo}_Mods.AethonMod.hjson', encoding='utf-8').read()
    assert 'MIRÁNDOTE EN TODO MOMENTO' not in t, '¡la oración eterna VIVE!'
    assert 'la medialuna de choque te mira' in t
t = open(REPO + '/AethonMod/Localization/en-US_Mods.AethonMod.hjson', encoding='utf-8').read()
assert 'FACING YOU AT ALL TIMES' not in t
assert 'the shock crescent faces you' in t
print('    la oración extendida MUERTA ×3 · la versión corta viva ×3 ✔')

print("(12) v6.110 — EL ALTAR CÓSMICO (la regeneración digna):")
w, h = dims(todos['Content/Tiles/AltarOfrendasTile.rawimg'])
assert (w, h) == (34, 300), f'¡el tile mide {w}x{h}!'
w2, h2 = dims(todos['Content/Items/Altar/AltarOfrendasItem.rawimg'])
assert (w2, h2) == (34, 50), f'¡el ítem mide {w2}x{h2}!'
w3, h3 = dims(todos['Content/Tiles/AltarOfrendasTile_Highlight.rawimg'])
assert (w3, h3) == (34, 50), f'¡el highlight mide {w3}x{h3}!'
fuente_gen = open(REPO + '/tools/altar_v6110.py', encoding='utf-8').read()
assert 'EL GLOW EMISIVO' in fuente_gen and 'LA LUZ DE LAS VELAS' in fuente_gen
print('    el tile 34×300 (6 marcos) + ítem + highlight + la forja cósmica ✔')

print()
print("AUDIT v6.110 — TODO VERIFICADO.")
