#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.98 — AUDITORÍA INDEPENDIENTE DEL .tmod.

LA SESIÓN DEL CORTE Y LAS CAPAS:
(0) versión 6.98 + entradas + tabla/EOF exacto
(1) LA LEY NUEVA — TRANSPARENCIA: las 13 armas recortadas + el altar
    + el proyectil base TIENEN fondo transparente (alfa 0 real en el
    rawimg); los 2 grimorios INTACTOS por decisión de la casa (su
    vacío estrellado es diseño).
(2) EL ALTAR DEL CORO: lámina 36×216 (4 marcos) transparente +
    highlight 36×54 + el ITEM 36×50 (fidelidad 1:1).
(3) EL ESTANDARTE DEL JUICIO (el aura que daña): item 30×40 + buff
    JuicioDelAlba 24×24 + la tira del banner 26×160 (4 marcos).
(4) DLL — firmas v6.98 vivas (el vestido por capas ×10 · el
    estandarte · el juicio) + históricas + muertas ausentes.
(5) hjson ×3 + claves nuevas + es-ES == es-MX + en-US traducido.
(6) LICENSE (la propietaria) dentro.
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
assert ver == "6.98", f"¡VERSIÓN {ver} ≠ 6.98!"
todos, off, malas = blobs(d, tend, entries)
print(f"(0) blobs: {len(todos)}/{len(entries)} íntegros, malas={malas}, EOF={off} == {fsize}: {'OK' if off == fsize else '¡ROMPO!'}")
assert malas == 0 and off == fsize

def dims(b):
    if b[:8] == b'\x89PNG\r\n\x1a\n': return struct.unpack('>II', b[16:24])
    return struct.unpack('<ii', b[4:12])

def pct_transparente(b):
    """El % de píxeles FULL transparentes de un rawimg/PNG (la ley v6.98)."""
    if b[:8] == b'\x89PNG\r\n\x1a\n':
        from PIL import Image
        import io
        im = Image.open(io.BytesIO(b)).convert('RGBA')
        px = im.load()
        n = sum(1 for y in range(im.height) for x in range(im.width) if px[x, y][3] == 0)
        return 100.0 * n / (im.width * im.height)
    w, h = struct.unpack('<ii', b[4:12])
    datos = b[12:12 + w * h * 4]
    n = sum(1 for i in range(3, len(datos), 4) if datos[i] == 0)
    return 100.0 * n / (w * h)

print("(1) LA LEY DE LA TRANSPARENCIA — EL CORTE de la superforja:")
recortadas = {   # las 13 armas que tenían fondo oscuro #183078/#101A4A
    'Weapons/TresCasas/CadenasDelOlvido', 'Weapons/TresCasas/CampanaDelSantuario',
    'Weapons/TresCasas/CetroDelAlba', 'Weapons/TresCasas/EspinasNocturnas',
    'Weapons/TresCasas/GeometriaImposible', 'Weapons/TresCasas/HambrePrimigenia',
    'Weapons/TresCasas/JuicioDelCielo', 'Weapons/TresCasas/LetaniaNebular',
    'Weapons/TresCasas/PupilaDelAbismo', 'Weapons/TresCasas/SemillaDeEstrellas',
    'Weapons/TresCasas/SusurroDelVacio', 'Weapons/TresCasas/LamparaDelPeregrino',
    'Weapons/TresCasas/CoroDeLosProfundos',
}
for tex in recortadas:
    hit = [p for p in todos if tex in p and (p.endswith('.rawimg') or p.endswith('.png'))]
    assert hit, f"¡FALTA {tex}!"
    t = pct_transparente(todos[hit[0]])
    assert t >= 8.0, f"¡{tex} SIN fondo transparente ({t:.1f}% — volvió el rectángulo)!"
    print(f"    TRANSPARENTE {tex.split('/')[-1]}: {t:.1f}% alfa-0")
# el proyectil base también fue cortado
hit = [p for p in todos if 'TresCasas/TresCasasProjectile' in p and (p.endswith('.rawimg') or p.endswith('.png'))]
assert hit, "¡FALTA TresCasasProjectile!"
tp = pct_transparente(todos[hit[0]])
assert tp >= 8.0, f"¡el proyectil base sin transparencia ({tp:.1f}%)!"
print(f"    TRANSPARENTE TresCasasProjectile: {tp:.1f}% alfa-0")
print(f"    → {len(recortadas) + 1} sprites cortados con EL CORTE, fondo GONE")

print("(2) EL ALTAR DEL CORO (el Coro de los Profundos mejorado):")
alt = [p for p in todos if p.endswith('Tiles/AltarOfrendasTile.rawimg') or p.endswith('Tiles/AltarOfrendasTile.png')]
assert alt and dims(todos[alt[0]]) == (36, 216), f"¡EL ALTAR NO ES 36×216! {[dims(todos[p]) for p in alt]}"
ta = pct_transparente(todos[alt[0]])
assert ta >= 20.0, f"¡EL ALTAR SIGUE SIENDO UN RECTÁNGULO ({ta:.1f}% transparente)!"
print(f"    VIVO AltarOfrendasTile 36×216 (4 marcos) · {ta:.1f}% transparente (el rectángulo oscuro MURIÓ)")
alh = [p for p in todos if p.endswith('AltarOfrendasTile_Highlight.rawimg') or p.endswith('AltarOfrendasTile_Highlight.png')]
assert alh and dims(todos[alh[0]]) == (36, 54), "¡EL HIGHLIGHT DEL ALTAR MAL!"
print("    VIVO AltarOfrendasTile_Highlight 36×54")
ai = [p for p in todos if 'Items/Altar/AltarOfrendasItem' in p and (p.endswith('.rawimg') or p.endswith('.png'))]
assert ai and dims(todos[ai[0]]) == (36, 50), f"¡EL ITEM DEL ALTAR MAL! {[dims(todos[p]) for p in ai]}"
print("    VIVO AltarOfrendasItem 36×50 (el altar 1:1, sin reescalado)")

print("(3) EL ESTANDARTE DEL JUICIO (el aura que daña — v6.98):")
nuevas = {'Items/Accessories/EstandarteDelJuicioItem': (30, 40),
          'Buffs/JuicioDelAlba': (24, 24),
          'Projectiles/EstandarteJuicioAuraBanner': (26, 160)}
for tex, wh in nuevas.items():
    hit = [p for p in todos if tex in p and (p.endswith('.rawimg') or p.endswith('.png'))]
    assert hit, f"¡FALTA {tex}!"
    got = dims(todos[hit[0]])
    assert got == wh, f"{tex}: {got} ≠ {wh}"
    print(f"    VIVA {tex.split('/')[-1]}: {wh[0]}×{wh[1]}")

ogs = [p for p in todos if p.endswith('.ogg') and len(todos[p]) > 1_500_000]
print(f"(3b) músicas del festín: {len(ogs)} — OggS: {[todos[p][:4]==b'OggS' for p in ogs]}")
assert len(ogs) == 2 and all(todos[p][:4]==b'OggS' for p in ogs)

print("(4) DLL — firmas:")
dll = todos['AethonMod.dll']
v6_98 = [
    # EL VESTIDO POR CAPAS (las 10 Vestir* + el corazón negro)
    'VestirHaz', 'VestirCorona', 'VestirLluvia', 'VestirNova', 'VestirVortice',
    'VestirEnjambre', 'VestirEstela', 'VestirZona', 'VestirHoja', 'VestirLucero',
    'DibujarCorazonNegro', 'Ruido', 'Titilar', 'CapaSuelo',
    # EL ESTANDARTE DEL JUICIO
    'EstandarteJuicioAuraProjectile', 'EstandarteDelJuicioItem', 'JuicioDelAlba',
    'JuicioPlayer', 'EstandarteJuicio', 'EstandarteJuicioOculto',
    'Juzgar', 'ApplyDamageToNPC', 'MAX_BLANCOS',
]
v6_97 = ['DevorarMaterialParcial', 'LibroRecibido', 'RecibidoPor', 'JefeEnElChecklistActual',
         'CultistasGlobalNPC', 'ElMandatoDelAltar', 'ColocarElAltarDeLaLlegada',
         'ArmaTresCasas', 'TresCasasProjectile', 'BolsaTresCasas', 'FueraDeLaLista',
         'CetroDelAlba', 'CoroDeLosProfundos', 'PlumaDelEscribaSanto']
v6_96 = ['ArrojadoTirosSistema', 'MarcarPorRed', 'BancoMonedasDisponibles']
v6_95 = ['JefeRenglonesProj', 'RefrescarPara', 'SemianchoDeCasa']
v6_93 = ['ArrojadoGlobalItem', 'HazLib', 'DespertarBarra', 'ComoEstatuas']
def firma(s, donde):
    return s.encode() in donde or s.encode('utf-16-le') in donde
for f in v6_98 + v6_97 + v6_96 + v6_95 + v6_93:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
print(f"    VIVAS: {len(v6_98)} de la v6.98 + {len(v6_97)+len(v6_96)+len(v6_95)+len(v6_93)} históricas")
muertas_f = ['MiradaSolyn', 'SolynTagTeamBeam', 'RelicarioDelAvispon', 'GrilletesDeAndromeda',
             'WHitoEscuadron', 'EntitySource_ItemDrop', 'BolsaDePociones']
for f in muertas_f:
    assert not firma(f, dll), f"¡{f} SIGUE EN LA DLL!"
print(f"    MUERTAS-AUSENTE: {len(muertas_f)}")

print("(4a) PAQUETE — los hjson DENTRO del .tmod sin IP:")
ips_pkg = re.compile(r're:?zero|ousagi|daphne|subaru|roswaal|calamity|warbanner', re.I)
cjk_pkg = re.compile('[\u4e00-\u9fff\u3040-\u30ff\uac00-\ud7af]')
sucios = [p for p in todos if p.endswith('.hjson') and
          (ips_pkg.search(todos[p].decode('utf-8', 'replace')) or cjk_pkg.search(todos[p].decode('utf-8', 'replace')))]
assert not sucios, sucios
print(f"    OK: 0 IP y 0 CJK en los {len([p for p in todos if p.endswith('.hjson')])} hjson empaquetados")

print("(4b) FUENTE — SIN chino ni IP (la ley de la casa):")
cjk = re.compile('[\u4e00-\u9fff\u3040-\u30ff\uac00-\ud7af]')
ips = re.compile(r're:?zero|ousagi|daphne|subaru|roswaal|calamity|warbanner', re.I)
malos = []
archivos = globmod.glob(f'{REPO}/AethonMod/**/*.cs', recursive=True) + \
           globmod.glob(f'{REPO}/AethonMod/**/*.hjson', recursive=True)
for f in archivos:
    t = open(f, encoding='utf-8', errors='replace').read()
    if cjk.search(t): malos.append(('CJK', f))
    if ips.search(t): malos.append(('IP', f))
assert not malos, malos
print(f"    OK: 0 CJK y 0 IP en {len(archivos)} archivos")

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
# las claves nuevas viven en los TRES idiomas
for clave in ['EstandarteDelJuicioItem', 'JuicioDelAlba', 'EstandarteJuicioAuraProjectile']:
    for idioma, texto in (('es-ES', es), ('es-MX', mx), ('en-US', en)):
        assert f'{clave}:' in texto, f"¡FALTA {clave} en {idioma}!"
print("    OK: EstandarteDelJuicioItem + JuicioDelAlba + EstandarteJuicioAuraProjectile en ×3")
# la traducción real del inglés
for s in ['Banner of Judgment', 'Dawn Flame', 'Aura of Judgment']:
    assert s in en, f"¡FALTA la traducción: {s}!"
print("    OK: traducciones reales (Banner of Judgment · Dawn Flame · Aura of Judgment)")
# la letra de la v6.97 sigue viva
for s in ['The Scepter of Dawn', 'Listen to me, bearer']:
    assert s in en, f"¡FALTA la traducción histórica: {s}!"

print("(6) LICENSE (la propietaria) dentro:")
lic = todos.get('LICENSE') or todos.get('LICENSE.txt')
assert lic is not None, "¡LICENSE no viaja!"
texto = lic.decode('utf-8', 'replace')
assert 'TODOS LOS DERECHOS RESERVADOS' in texto, texto[:200]
print("    OK: propietaria, sin atribuciones")
print("\nAUDIT v6.98: TODO VERIFICADO ✔")
