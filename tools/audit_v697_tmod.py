#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.97 — AUDITORÍA INDEPENDIENTE DEL .tmod PUBLICADO EN GITHUB.
(0) versión 6.97 + entradas
(1) tabla + EOF exacto
(2) PNGs: las 20 armas de LAS TRES CASAS + el ALTAR animado 4 marcos (36×216)
    + la BOLSA DE POCIONES AUSENTE
(3) las dos músicas del festín
(4) DLL: firmas v6.97 vivas (devoración parcial · libro 1 vez · jefes por
    checklist · cultistas · mandato · llegada con altar · tres casas) +
    muertas ausentes
(5) hjson ×3 simétricos + claves nuevas (FueraDeLaLista · Consagrado)
(6) LICENSE (la propietaria) dentro
"""
import sys, zlib, struct, hashlib, re, glob as globmod
sys.path.insert(0, '/home/z/my-project/download/Aethon-Mod-Terraria/tools')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"  # (o el asset de la release v6.97 — md5 6cbf57e3527fd114bfe0752af2f411b8)
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
assert ver == "6.97", f"¡VERSIÓN {ver} ≠ 6.97!"
todos, off, malas = blobs(d, tend, entries)
print(f"(1) blobs: {len(todos)}/{len(entries)} íntegros, malas={malas}, EOF={off} == {fsize}: {'OK' if off == fsize else '¡ROMPO!'}")
assert malas == 0 and off == fsize

def dims(b):
    if b[:8] == b'\x89PNG\r\n\x1a\n': return struct.unpack('>II', b[16:24])
    return struct.unpack('<ii', b[4:12])

print("(2) PNGs — LAS TRES CASAS + EL ALTAR ANIMADO + LA BOLSA MUERTA:")
casas = {
    # SAGRADA (7)
    'Weapons/TresCasas/CetroDelAlba': (36, 40),
    'Weapons/TresCasas/CoronaDeSerafines': (38, 34),
    'Weapons/TresCasas/JuicioDelCielo': (36, 44),
    'Weapons/TresCasas/CampanaDelSantuario': (32, 38),
    'Weapons/TresCasas/LamparaDelPeregrino': (30, 40),
    'Weapons/TresCasas/PlumaDelEscribaSanto': (34, 40),
    'Weapons/TresCasas/ReliquiaDelAmanecer': (36, 36),
    # HORROR CÓSMICO (7)
    'Weapons/TresCasas/PupilaDelAbismo': (36, 42),
    'Weapons/TresCasas/GeometriaImposible': (36, 36),
    'Weapons/TresCasas/MordidaDelEspacio': (36, 36),
    'Weapons/TresCasas/LetaniaNebular': (36, 44),
    'Weapons/TresCasas/SusurroDelVacio': (34, 38),
    'Weapons/TresCasas/SemillaDeEstrellas': (32, 38),
    'Weapons/TresCasas/CoroDeLosProfundos': (34, 46),
    # OSCURIDAD (6)
    'Weapons/TresCasas/HambrePrimigenia': (36, 40),
    'Weapons/TresCasas/GuadanaDelVerdugo': (44, 44),
    'Weapons/TresCasas/CadenasDelOlvido': (38, 36),
    'Weapons/TresCasas/VeloDeCeniza': (30, 38),
    'Weapons/TresCasas/EspinasNocturnas': (36, 34),
    'Weapons/TresCasas/EclipseInterior': (36, 36),
}
for tex, wh in casas.items():
    hit = [p for p in todos if tex in p and (p.endswith('.rawimg') or p.endswith('.png'))]
    assert hit, f"¡FALTA {tex}!"
    assert dims(todos[hit[0]]) == wh, f"{tex}: {dims(todos[hit[0]])} ≠ {wh}"
    print(f"    VIVA {tex.split('/')[-1]}: {wh[0]}x{wh[1]}")
print(f"    → {len(casas)} armas de LAS TRES CASAS DENTRO")
# EL ALTAR ANIMADO: 4 marcos verticales (36×216 = 4×54)
alt = [p for p in todos if p.endswith('Tiles/AltarOfrendasTile.rawimg') or p.endswith('Tiles/AltarOfrendasTile.png')]
assert alt and dims(todos[alt[0]]) == (36, 216), f"¡EL ALTAR NO ES 36×216 (4 marcos)! {alt}: {[dims(todos[p]) for p in alt]}"
print(f"    VIVA Tiles/AltarOfrendasTile: 36x216 (4 marcos animados, SIN libro)")
alh = [p for p in todos if p.endswith('AltarOfrendasTile_Highlight.rawimg') or p.endswith('AltarOfrendasTile_Highlight.png')]
assert alh and dims(todos[alh[0]]) == (36, 54), f"¡EL HIGHLIGHT DEL ALTAR MAL! {alh}"
print(f"    VIVA Tiles/AltarOfrendasTile_Highlight: 36x54")
# LA BOLSA DE POCIONES MURIÓ
assert not [p for p in todos if 'BolsaDePociones' in p], "¡LA BOLSA DE POCIONES SIGUE DENTRO!"
print("    MUERTA-AUSENTE Items/Bolsas/BolsaDePociones")
# las texturas de la casa históricas siguen vivas
for tex, wh in {'Effects/Procedural/Investigacion/DestelloEstelar': (512, 512),
                'Projectiles/Cosmetic/VastagoDelEternoPet': (40, 384),
                'Weapons/Definitivas/RenglonFinal': (44, 44)}.items():
    hit = [p for p in todos if tex in p and (p.endswith('.rawimg') or p.endswith('.png'))]
    assert hit and dims(todos[hit[0]]) == wh, f"¡FALTA {tex}!"
    print(f"    VIVA {tex.split('/')[-1]}: {wh[0]}x{wh[1]}")
ogs = [p for p in todos if p.endswith('.ogg') and len(todos[p]) > 1_500_000]
print(f"(3) músicas del festín: {len(ogs)} — OggS: {[todos[p][:4]==b'OggS' for p in ogs]}")
assert len(ogs) == 2 and all(todos[p][:4]==b'OggS' for p in ogs)

print("(4) DLL — firmas:")
dll = todos['AethonMod.dll']
v6_97 = ['DevorarMaterialParcial',       # (2) las cantidades parciales
         'LibroRecibido', 'RecibidoPor', # (3) el libro cae UNA vez
         'JefeEnElChecklistActual',      # (4) los jefes por checklist
         'CultistasGlobalNPC',           # (5) los cultistas
         'ElMandatoDelAltar',            # (7) la nota la dice el Eterno
         'ColocarElAltarDeLaLlegada',    # (9) la llegada con su altar
         'ArmaTresCasas', 'TresCasasProjectile', 'BolsaTresCasas',  # (12) las 20 armas
         'CetroDelAlba', 'CoronaDeSerafines', 'JuicioDelCielo', 'CampanaDelSantuario',
         'LamparaDelPeregrino', 'PlumaDelEscribaSanto', 'ReliquiaDelAmanecer',
         'PupilaDelAbismo', 'GeometriaImposible', 'MordidaDelEspacio', 'LetaniaNebular',
         'SusurroDelVacio', 'SemillaDeEstrellas', 'CoroDeLosProfundos',
         'HambrePrimigenia', 'GuadanaDelVerdugo', 'CadenasDelOlvido', 'VeloDeCeniza',
         'EspinasNocturnas', 'EclipseInterior',
         'FueraDeLaLista']               # (1) el aviso del plato apartado
v6_96 = ['ArrojadoTirosSistema', 'MarcarPorRed', 'AvisarTiroDelPortador',
         'MsgTiro', 'HuellaDelTiro', 'LaHuellaVive', 'BancoMonedasDisponibles']
v6_95 = ['JefeRenglonesProj', 'JefeRC2Proj', 'JefeMiradaProj',
         'RefrescarPara', 'SemianchoDeCasa',
         'EstadoRenglones', 'EstadoRC2', 'EstadoMirada']
v6_93 = ['ArrojadoGlobalItem', 'ContarOpcionesPagadas',
         'BancoGuardarMonedas', 'BancoCobrarMonedas', 'BancoGrupoDisponible',
         'HazLib', 'MiradaDelEterno', 'RenglonFinal', 'DespertarDevorarA',
         'DespertarBarra', 'DespertarHitoDano', 'ComoEstatuas',
         'EntitySource_DropAsItem']
def firma(s, donde):
    return s.encode() in donde or s.encode('utf-16-le') in donde
for f in v6_97 + v6_96 + v6_95 + v6_93:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
print(f"    VIVAS: {len(v6_97)} de la v6.97 + {len(v6_96)+len(v6_95)+len(v6_93)} históricas")
muertas_f = ['MiradaSolyn', 'SolynTagTeamBeam', 'RelicarioDelAvispon', 'GrilletesDeAndromeda',
             'WHitoEscuadron', 'EntitySource_ItemDrop', 'BolsaDePociones']
for f in muertas_f:
    assert not firma(f, dll), f"¡{f} SIGUE EN LA DLL!"
print(f"    MUERTAS-AUSENTE: {len(muertas_f)}")

print("(4b) FUENTE — SIN chino ni IP (la ley de la casa):")
cjk = re.compile('[\u4e00-\u9fff\u3040-\u30ff\uac00-\ud7af]')
ips = re.compile(r're:?zero|ousagi|daphne|subaru|roswaal', re.I)
malos = []
archivos = globmod.glob(f'{REPO}/AethonMod/**/*.cs', recursive=True) + \
           globmod.glob(f'{REPO}/AethonMod/**/*.hjson', recursive=True)
for f in archivos:
    t = open(f, encoding='utf-8', errors='replace').read()
    if cjk.search(t): malos.append(('CJK', f))
    if ips.search(t): malos.append(('IP', f))
assert not malos, malos
print(f"    OK: 0 CJK y 0 IP en {len(archivos)} archivos")

print("(4c) FUENTE — la letra de la v6.97:")
grim = open(f'{REPO}/AethonMod/Content/Weapons/GrimoireEternal.cs', encoding='utf-8').read()
assert 'Devorar Armas o Materiales Consagrados' in grim, "¡la etiqueta nueva no vive!"
assert '(Consagrado)' in grim, "¡la etiqueta por item no vive!"
assert '(A) {nombreArma}' not in grim and '(M) {cuota}' not in grim, "¡los (A)/(M) viven!"
assert 'BonusDano(despertar) * 100f' in grim, "¡el daño no lee el acumulado!"
print("    OK: etiqueta Consagrados + (Consagrado) por item + sin (A)/(M) + daño acumulado")

print("(5) hjson ×3:")
hj = {p: todos[p] for p in todos if p.endswith('.hjson')}
assert len(hj) == 6, hj.keys()
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
    lineas = t.split('\n')
    i = 0
    while i < len(lineas) and (lineas[i].strip() == '' or lineas[i].lstrip().startswith('//')):
        i += 1
    return lineas[i:]
les, lmx, len_ = cuerpo(es), cuerpo(mx), cuerpo(en)
assert les == lmx, "¡es-ES y es-MX divergen!"
print("    OK: es-ES == es-MX en el cuerpo (el espejo)")
# v6.97: el en-US lleva TRADUCCIÓN REAL de TODO el contenido nuevo (las 20
# armas Tres Casas · los diálogos de cultistas · ElMandatoDelAltar ·
# FueraDeLaLista) — mejora sobre el fallback puro de la .96. Las diferencias
# deben caer TODAS dentro de esas claves nuevas y el resto seguir siendo el
# fallback español exacto.
import difflib
claves_nuevas = ['CetroDelAlba', 'CoronaDeSerafines', 'JuicioDelCielo', 'CampanaDelSantuario',
                 'LamparaDelPeregrino', 'PlumaDelEscribaSanto', 'ReliquiaDelAmanecer',
                 'PupilaDelAbismo', 'GeometriaImposible', 'MordidaDelEspacio', 'LetaniaNebular',
                 'SusurroDelVacio', 'SemillaDeEstrellas', 'CoroDeLosProfundos',
                 'HambrePrimigenia', 'GuadanaDelVerdugo', 'CadenasDelOlvido', 'VeloDeCeniza',
                 'EspinasNocturnas', 'EclipseInterior',
                 'BolsaTresCasas', 'Cultistas', 'Burla', 'Vencido', 'Devorado', 'ElJefe',
                 'ElMandatoDelAltar', 'FueraDeLaLista']
fuera = []
for h in [l for l in difflib.unified_diff(les, len_, lineterm='', n=0) if l.startswith('@@')]:
    m = re.match(r'@@ -(\d+)', h)
    if m:
        # el contexto del hunk: subir hasta la clave de bloque más cercana
        n = int(m.group(1)) - 1
        contexto = '\n'.join(les[max(0, n - 400):n + 8])
        if not any(c in contexto[-2200:] for c in claves_nuevas):
            fuera.append(n + 1)
assert not fuera, f"¡el en-US diverge FUERA del contenido nuevo de la v6.97 en {len(fuera)} hunks: {fuera}!"
print("    OK: en-US = fallback español EXACTO salvo el contenido NUEVO v6.97 traducido al inglés")
# y las traducciones SÍ viven (no es una copia rancia del español)
cuerpo_en = '\n'.join(len_)
for s in ['The Scepter of Dawn', 'FALSE god', "That is NOT on this step's list",
          'Listen to me, bearer']:
    assert s in cuerpo_en, f"¡FALTA la traducción: {s}!"
print("    OK: traducciones reales vivas (armas · cultistas · FueraDeLaLista · ElMandato)")
for clave in ['FueraDeLaLista', 'Consagrado', 'No es apto', 'ComoEstatuas',
              'el dinero simplemente se deja caer']:
    assert clave in es, f"¡FALTA la clave {clave}!"
    print(f"    CLAVE VIVA {clave}")

print("(6) LICENSE (la propietaria) dentro:")
lic = todos.get('LICENSE') or todos.get('LICENSE.txt')
assert lic is not None, "¡LICENSE no viaja!"
texto = lic.decode('utf-8', 'replace')
assert 'TODOS LOS DERECHOS RESERVADOS' in texto, texto[:200]
print("    OK: propietaria, sin atribuciones")
print("\nAUDIT v6.97: TODO VERIFICADO ✔")
