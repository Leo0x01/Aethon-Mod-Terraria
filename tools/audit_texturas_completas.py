#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.97.1 — EL GUARDIÁN DE LAS TEXTURAS COMPLETAS.

LA LECCIÓN (v6.87 → v6.97): el servidor headless NO carga texturas — solo
el CLIENTE las exige. Dos versiones ya salieron con clases ModItem /
ModProjectile cuya textura PNG nunca entró al .tmod y el mod entero se
desactivó en la máquina del usuario («Disabling Mod: AethonMod»).

ESTE GUARDIÁN cierra la clase de bug PARA SIEMPRE:

  (1) FUENTE → DISCO: toda clase de contenido (ModItem / ModProjectile /
      ModNPC / ModDust / ModTile / ModWall / ModBuff) debe tener su PNG
      en su ruta de verdad — LA PROPIA (misma carpeta del .cs, el mismo
      nombre de la clase) o LA COMPARTIDA (el «override string Texture»
      de la casa: los FX toman prestada una textura de Effects/ u otra
      hermana — el guardián la sigue hasta su PNG).
  (2) DLL → .tmod: toda clase de contenido viva en la DLL compilada
      debe tener su rawimg DENTRO del .tmod publicado (la propia o la
      compartida — la que el override diga).
  (3) .tmod → DLL: (inversa) todo rawimg de contenido debe existir
      como clase o como textura prestada (las de Effects/ no cuentan:
      las pide el código a mano).

USO:  python3 tools/audit_texturas_completas.py [.tmod]
      (sin argumento: usa el .tmod instalado del tML local)
Sale 0 si TODO vive; lista CADA ausencia si no. EL GATE DE PUBLICACIÓN:
ningún build se publica si este guardián no dice TODO VIVE.
"""
import os
import re
import sys
import zlib

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.join(AQUI, '..', 'AethonMod')
sys.path.insert(0, AQUI)
try:
    from parse_tmod import parse
except ImportError:
    parse = None

TMOD = sys.argv[1] if len(sys.argv) > 1 else \
    os.path.expanduser('~/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod')

# === (1) FUENTE → DISCO ===
# LA CASA tiene clases intermedias (BolsaCategoria, ArmaTresCasas…) — la
# herencia se resuelve en TRANSITIVO: si la cadena de bases llega a un
# ModItem/ModProjectile/…, la clase ES contenido.
CLASES = re.compile(
    r'(?:public\s+)?(?:internal\s+)?(?:abstract\s+)?(?:sealed\s+)?(?:partial\s+)?class\s+(\w+)\s*:\s*([\w<>,\s.]+?)\s*(?:\{|where|//)')
MODS = {'ModItem', 'ModProjectile', 'ModNPC', 'ModDust', 'ModTile',
        'ModWall', 'ModBuff', 'ModPetal'}
PRESTADA = re.compile(r'override\s+string\s+Texture\s*=>\s*"([^"]+)"')


def ruta_png(ruta_asset):
    """'AethonMod/Content/X/Y' → (la ruta del PNG en el árbol, la entrada del .tmod)"""
    if ruta_asset.startswith('AethonMod/'):
        ruta_asset = ruta_asset[len('AethonMod/'):]
    png = os.path.join(RAIZ, ruta_asset + '.png')
    return png, ruta_asset


# PASADA A: el mapa de herencia del árbol entero
herencia, tipo_directo, posiciones = {}, {}, {}
for dirpath, _, archivos in os.walk(RAIZ):
    for a in archivos:
        if not a.endswith('.cs'):
            continue
        ruta = os.path.join(dirpath, a)
        texto = open(ruta, encoding='utf-8', errors='replace').read()
        for m in CLASES.finditer(texto):
            clase = m.group(1)
            bases = [b.strip().split('<')[0].split('.')[-1]
                     for b in m.group(2).split(',')]
            herencia[clase] = bases
            for b in bases:
                if b in MODS:
                    tipo_directo[clase] = b
            posiciones[clase] = (ruta, texto, m.start())

TIPOS = {}          # clase → el ModX de su cadena


def tipo_de(clase, visto=None):
    if clase in TIPOS:
        return TIPOS[clase]
    visto = visto or set()
    if clase in visto or clase not in herencia:
        return None
    visto.add(clase)
    for b in herencia[clase]:
        if b in MODS:
            TIPOS[clase] = b
            return b
        t = tipo_de(b, visto)
        if t:
            TIPOS[clase] = t
            return t
    return None


encontradas = {}      # clase → dict(tipo, cs, abstracta, png, entrada, modo)
for clase, (ruta, texto, ini) in posiciones.items():
    tipo = tipo_de(clase)
    if not tipo:
        continue
    abstracta = 'abstract ' in texto[max(0, ini - 20):ini + 90]
    marcas = sorted(p[2] for p in posiciones.values() if p[0] == ruta)
    i = marcas.index(ini)
    cuerpo = texto[ini:marcas[i + 1] if i + 1 < len(marcas) else len(texto)]
    rel = os.path.relpath(ruta, RAIZ)
    prestamo = PRESTADA.search(cuerpo)
    if prestamo:
        png, entrada = ruta_png(prestamo.group(1))
        modo = 'prestada'
    else:
        # LA PROPIA: la convención de tML — la carpeta del .cs (la casa
        # mantiene namespace == carpeta) + el nombre de la clase
        carpeta = os.path.dirname(ruta)[len(RAIZ.rstrip('/')) + 1:]
        png = os.path.join(RAIZ, carpeta, clase + '.png')
        entrada = carpeta + '/' + clase
        modo = 'propia'
    encontradas[clase] = {'tipo': tipo, 'cs': rel, 'abstracta': abstracta,
                          'png': png, 'entrada': entrada, 'modo': modo}

faltan_disco, total = [], 0
for clase, info in encontradas.items():
    if info['abstracta']:
        continue
    total += 1
    if not os.path.exists(info['png']):
        faltan_disco.append((info['tipo'], clase, info['modo'], info['cs'], info['png']))
print(f'(1) FUENTE → DISCO: {total} clases de contenido (herencia transitiva)')
if faltan_disco:
    for tipo, clase, modo, cs, png in faltan_disco:
        print(f'    ¡FALTA EL PNG ({modo})! {clase} ({tipo}) — {cs}')
        print(f'        esperado: {os.path.relpath(png, RAIZ)}')
else:
    print('    OK: toda clase tiene su PNG (la propia o la prestada del override)')

# === (2)+(3) DLL ↔ .tmod ===
if not parse or not os.path.exists(TMOD):
    print(f'(2) .tmod no disponible ({TMOD}) — la pasada del .tmod se salta')
    sys.exit(1 if faltan_disco else 0)

d = open(TMOD, 'rb').read()
tmlver, nombre, ver, entradas, tend, fsize = parse(TMOD)
todos, off = {}, tend
for p, raw, comp in entradas:
    trozo = d[off:off + comp]
    todos[p] = trozo if comp == raw else zlib.decompress(trozo, -15)
    off += comp
print(f'(2) {nombre} v{ver}: {len(todos)} entradas, EOF {"OK" if off == fsize else "¡ROMPO!"}')

dentro = {p.rsplit('/', 1)[-1].rsplit('.', 1)[0]: p for p in todos if p.endswith('.rawimg')}
dll = todos.get('AethonMod.dll', b'')


def vive_en_dll(s):
    return s.encode() in dll or s.encode('utf-16-le') in dll


faltan_tmod = []
prestadas_usadas = set()
for clase, info in encontradas.items():
    if info['abstracta'] or not vive_en_dll(clase):
        continue          # la abstracta o la muerta no cuentan
    entrada = info['entrada']
    if not any(p == entrada + '.rawimg' for p in todos):
        faltan_tmod.append((info['tipo'], clase, info['modo'], entrada))
    else:
        prestadas_usadas.add(entrada)
print(f'(2) DLL → .tmod:')
if faltan_tmod:
    for tipo, clase, modo, entrada in faltan_tmod:
        print(f'    ¡FALTA DENTRO DEL .tmod ({modo})! {clase} ({tipo}) → {entrada}.rawimg')
else:
    print('    OK: toda clase viva en la DLL tiene su rawimg DENTRO (propio o prestado)')

# (3) la inversa: rawimgs de contenido SIN clase y SIN préstamo (los de
# Effects/ no cuentan — los pide el código a mano; y los *_Highlight son
# la CONVENCIÓN tML del HasOutlines — la lección de la v6.87)
sueltos = []
for p in todos:
    if not p.endswith('.rawimg') or '/Effects/' in p or '/Localization/' in p:
        continue
    if p.endswith('_Highlight.rawimg'):
        continue          # el compañero del resaltado (exigido por tML)
    base = p.rsplit('/', 1)[-1].rsplit('.', 1)[0]
    ruta_asset = p[:-len('.rawimg')]
    if ruta_asset not in prestadas_usadas and not vive_en_dll(base):
        sueltos.append(p)
print(f'(3) .tmod → DLL (los sueltos):')
if sueltos:
    for p in sueltos:
        print(f'    rawimg sin clase: {p}')
else:
    print('    OK: ningún rawimg de contenido huérfano')

if faltan_disco or faltan_tmod or sueltos:
    print('\nGUARDIÁN DE TEXTURAS: ¡FALTA! — el build NO se publica hasta curar esto')
    sys.exit(1)
print('\nGUARDIÁN DE TEXTURAS: TODO VIVE ✔')
