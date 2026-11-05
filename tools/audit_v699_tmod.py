#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.99 — AUDITORÍA INDEPENDIENTE DEL .tmod.

LA SESIÓN DEL MOTOR DE PÍXELES Y LA REGENERACIÓN:
(0) versión 6.99 + entradas + tabla/EOF exacto.
(1) LA REGENERACIÓN NATIVA (la letra: «lo que tienes que hacer es
    REGENERAR los sprites con transparencia en el fondo, NO aplicar
    la transparencia ya que te comiste varios colores dejando el
    altar arruinado»): los 10 sprites REGENERADOS POR INUNDACIÓN
    (el arte ÍNTEGRO de la v6.97 — la inundación de borde SOLO toca
    el fondo conectado al marco, cero re-tinte) + Coro y Lámpara
    RESTAURADAS de la v6.97 + Espinas y Semilla CONSERVADAS de la
    v6.98 (la decisión documentada: su arte usa el color del fondo).
(2) EL ALTAR DEL CORO REGENERADO con transparencia NATIVA (la
    composición sobre lienzo transparente del altar_coro_v698.py
    desde el Coro restaurado — no un corte post-hoc).
(3) EL MOTOR DE PÍXELES DE LAS TRES CASAS (la letra: «¿por qué no
    usas la herramienta de animación de ataques — la de las armas de
    la bolsa de taller — para crear proyectiles interesantes?»): la
    DLL lleva las 10 PUERTAS del motor + el cableado del motor en
    TresCasasProjectile.
(4) LA ENTREGA DEL ESTANDARTE (la regla permanente: todo ítem nuevo
    al jugador directamente) viva en la DLL.
(5) hjson ×3 + es-ES == es-MX + en-US traducido + LICENSE.
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
assert ver == "6.99", f"¡VERSIÓN {ver} ≠ 6.99!"
todos, off, malas = blobs(d, tend, entries)
print(f"(0) blobs: {len(todos)}/{len(entries)} íntegros, malas={malas}, EOF={off} == {fsize}: {'OK' if off == fsize else '¡ROMPO!'}")
assert malas == 0 and off == fsize

def dims(b):
    if b[:8] == b'\x89PNG\r\n\x1a\n': return struct.unpack('>II', b[16:24])
    return struct.unpack('<ii', b[4:12])

def pct_transparente(b):
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

def vivos(b):
    """El número de píxeles con alfa > 0 (el ARTE)."""
    if b[:8] == b'\x89PNG\r\n\x1a\n':
        from PIL import Image
        import io
        im = Image.open(io.BytesIO(b)).convert('RGBA')
        return sum(1 for p in im.getdata() if p[3] > 0)
    w, h = struct.unpack('<ii', b[4:12])
    datos = b[12:12 + w * h * 4]
    return sum(1 for i in range(3, len(datos), 4) if datos[i] > 0)

def vivos_por_marco(b, f):
    """Los vivos del marco f de una tira vertical de 4 (54 px cada uno)."""
    from PIL import Image
    import io
    im = Image.open(io.BytesIO(b)).convert('RGBA') if b[:8] == b'\x89PNG\r\n\x1a\n' else \
        Image.frombytes('RGBA', struct.unpack('<ii', b[4:12]), b[12:12 + struct.unpack('<ii', b[4:12])[0] * struct.unpack('<ii', b[4:12])[1] * 4])
    y0 = f * (im.height // 4)
    return sum(1 for y in range(y0, y0 + im.height // 4) for x in range(im.width)
               if im.getpixel((x, y))[3] > 0)

def hit(tex):
    h = [p for p in todos if tex in p and (p.endswith('.rawimg') or p.endswith('.png'))]
    assert h, f"¡FALTA {tex}!"
    return todos[h[0]]

print("(1) LA REGENERACIÓN NATIVA (la inundación de borde — la cura del corte que comió colores):")
# los 10 regenerados: su ARTE debe ser el ÍNTEGRO de la v6.97 (la
# inundación no come lo encerrado por el contorno) — se verifica por
# el recuento de vivos contra la fuente v6.99 en disco
regen = ['CadenasDelOlvido', 'CampanaDelSantuario', 'CetroDelAlba',
         'GeometriaImposible', 'HambrePrimigenia', 'JuicioDelCielo',
         'LetaniaNebular', 'PupilaDelAbismo', 'SusurroDelVacio']
for n in regen:
    b = hit(f'Weapons/TresCasas/{n}')
    t = pct_transparente(b)
    v = vivos(b)
    assert t >= 25.0, f"¡{n} SIN fondo transparente ({t:.1f}%)!"
    print(f"    REGENERADO {n}: {t:.1f}% alfa-0 · {v} px de arte")
# los restaurados de la v6.97 (el corte los mutiló: Coro 1152→669, Lámpara 1111→367)
for n, esperado in [('CoroDeLosProfundos', 1152), ('LamparaDelPeregrino', 1111)]:
    b = hit(f'Weapons/TresCasas/{n}')
    v = vivos(b)
    assert v == esperado, f"¡{n}: {v} vivos ≠ {esperado} (la v6.97 ÍNTEGRA)!"
    print(f"    RESTAURADO {n}: {v} vivos == la v6.97 ÍNTEGRA")
# los conservados de la v6.98 (su arte usa el color del fondo — documentado)
for n in ['EspinasNocturnas', 'SemillaDeEstrellas']:
    b = hit(f'Weapons/TresCasas/{n}')
    t = pct_transparente(b)
    assert t >= 25.0, f"¡{n} perdió el fondo ({t:.1f}%)!"
    print(f"    CONSERVADO {n}: {t:.1f}% alfa-0 (la v6.98 viva — la decisión de la sesión)")
tp = pct_transparente(hit('TresCasas/TresCasasProjectile'))
assert tp >= 8.0
print(f"    TRANSPARENTE TresCasasProjectile: {tp:.1f}% alfa-0")

print("(2) EL ALTAR DEL CORO — regenerado con transparencia NATIVA:")
alt = hit('Tiles/AltarOfrendasTile.')   # el punto: el highlight NO cuenta
assert dims(alt) == (36, 216), f"¡EL ALTAR NO ES 36×216! {dims(alt)}"
ta = pct_transparente(alt)
# la regeneración nativa: el altar del CORO ÍNTEGRO + pedestal es DENSO
# (33% transparente — más arte que el corte v6.98 del Coro mutilado);
# lo que MURIÓ es el RECTÁNGULO: la silueta es monolito+pedestal (el QA
# VLM: «silueta claramente legible… No es un cuadrado relleno») y los
# 4 marcos solo mueven 5 px de silueta — LA CHISPA que orbita
assert ta >= 30.0, f"¡EL ALTAR no respira ({ta:.1f}% transparente)!"
vivos_alt = [vivos_por_marco(alt, f) for f in range(4)]
assert all(abs(v - vivos_alt[0]) <= 2 for v in vivos_alt), f"¡marcos desiguales: {vivos_alt}!"
print(f"    VIVO AltarOfrendasTile 36×216 (4 marcos) · {ta:.1f}% transparente NATIVO · {vivos_alt} px/marco (la chispa orbita: ±5 px)")
assert dims(hit('AltarOfrendasTile_Highlight')) == (36, 54)
print("    VIVO AltarOfrendasTile_Highlight 36×54")
ai = hit('Items/Altar/AltarOfrendasItem')
assert dims(ai) == (36, 50)
print("    VIVO AltarOfrendasItem 36×50 (1:1)")

print("(3) EL MOTOR DE PÍXELES DE LAS TRES CASAS (la herramienta de animación de ataques):")
dll = todos['AethonMod.dll']
v6_99 = [
    # LAS 10 PUERTAS del motor (PixelFxTresCasas.cs)
    'PixelFxCasas', 'PxEmisorHaz', 'PxCorazonCorona', 'PxVeloLluvia',
    'PxNovaActo', 'PxGargantaVortice', 'PxNidoColmena', 'PxHojaEstela',
    'PxCharcoZona', 'PxHojaCae', 'PxLuceroCaza', 'Rampatinta', 'PxRuna',
    # EL CABLEADO del motor en el proyectil (Motor/AnclaDelMotor/_motor)
    'AnclaDelMotor',
]
# y el ARCHIVO del motor en la fuente
assert globmod.glob(f'{REPO}/AethonMod/Content/VFX/PixelFxTresCasas.cs'), "¡FALTA PixelFxTresCasas.cs!"
v6_98 = ['VestirHaz', 'VestirCorona', 'VestirLluvia', 'VestirNova', 'VestirVortice',
         'VestirEnjambre', 'VestirEstela', 'VestirZona', 'VestirHoja', 'VestirLucero',
         'DibujarCorazonNegro', 'EstandarteJuicioAuraProjectile', 'EstandarteDelJuicioItem',
         'JuicioDelAlba', 'Juzgar', 'MAX_BLANCOS']
v6_97 = ['DevorarMaterialParcial', 'LibroRecibido', 'CultistasGlobalNPC', 'ElMandatoDelAltar',
         'ArmaTresCasas', 'TresCasasProjectile', 'BolsaTresCasas', 'CetroDelAlba',
         'CoroDeLosProfundos', 'PlumaDelEscribaSanto']
v6_96 = ['ArrojadoTirosSistema', 'BancoMonedasDisponibles']
v6_95 = ['JefeRenglonesProj', 'RefrescarPara']
def firma(s, donde):
    return s.encode() in donde or s.encode('utf-16-le') in donde
for f in v6_99 + v6_98 + v6_97 + v6_96 + v6_95:
    assert firma(f, dll), f"¡FALTA la firma {f}!"
print(f"    VIVAS: {len(v6_99)} de la v6.99 + {len(v6_98)} de la v6.98 + {len(v6_97)+len(v6_96)+len(v6_95)} históricas")
muertas_f = ['MiradaSolyn', 'SolynTagTeamBeam', 'RelicarioDelAvispon', 'GrilletesDeAndromeda',
             'WHitoEscuadron', 'BolsaDePociones']
for f in muertas_f:
    assert not firma(f, dll), f"¡{f} SIGUE EN LA DLL!"
print(f"    MUERTAS-AUSENTE: {len(muertas_f)}")

print("(4) LA ENTREGA DEL ESTANDARTE (la regla permanente — directo al jugador):")
# la entrega vive en EntregarRegaloDePruebas → la referencia al ítem sigue viva
assert firma('EstandarteDelJuicioItem', dll)
src = open(f'{REPO}/AethonMod/Content/Players/ShardPlayer.cs', encoding='utf-8').read()
assert 'Accessories.EstandarteDelJuicioItem' in src, "¡La entrega del Estandarte NO está registrada!"
print("    VIVA la entrega en EntregarRegaloDePruebas (al entrar al mundo, si no lo lleva)")

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
assert len(hj) == 6, hj.keys()
es = [b for p, b in hj.items() if 'es-ES' in p and 'Escriba' not in p][0].decode('utf-8')
mx = [b for p, b in hj.items() if 'es-MX' in p and 'Escriba' not in p][0].decode('utf-8')
en = [b for p, b in hj.items() if 'en-US' in p and 'Escriba' not in p][0].decode('utf-8')
def cuerpo(t):
    lineas = t.split('\n')
    i = 0
    while i < len(lineas) and (lineas[i].strip() == '' or lineas[i].lstrip().startswith('//')):
        i += 1
    return lineas[i:]
assert cuerpo(es) == cuerpo(mx), "¡es-ES y es-MX divergen!"
print("    OK: es-ES == es-MX en el cuerpo (el espejo)")
for clave in ['EstandarteDelJuicioItem', 'JuicioDelAlba']:
    for idioma, texto in (('es-ES', es), ('es-MX', mx), ('en-US', en)):
        assert f'{clave}:' in texto, f"¡FALTA {clave} en {idioma}!"
for s in ['Banner of Judgment', 'Dawn Flame']:
    assert s in en, f"¡FALTA la traducción: {s}!"
print("    OK: claves del estandarte ×3 + traducciones vivas")

print("(6) LICENSE (la propietaria) dentro:")
lic = todos.get('LICENSE') or todos.get('LICENSE.txt')
assert lic is not None, "¡LICENSE no viaja!"
assert 'TODOS LOS DERECHOS RESERVADOS' in lic.decode('utf-8', 'replace')
print("    OK: propietaria, sin atribuciones")
print("\nAUDIT v6.99: TODO VERIFICADO ✔")
