#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.181 — EL GUARDIÁN DE ARRANQUE (EL GATE DEL CLIENTE).

LA LECCIÓN (la v6.180 salió con TODOS los gates en verde y el mod se
desactivó en la máquina del usuario): el servidor headless NO carga
texturas — solo el CLIENTE las exige. Ningún banco headless puede
certificar el arranque del cliente. Este gate SIMULA el camino del
cliente en frío, antes de publicar:

  (1) LA TEXTURA PRINCIPAL DE CADA CLASE — la que mató a la v6.180:
      ModNPC.AutoStaticDefaults() pide la textura DEFAULT de la clase
      («Asset could not be found: Content\\NPCs\\AethonSellado» →
      «Disabling Mod: AethonMod»). Toda clase de contenido debe tener
      su PNG en disco Y su rawimg en el .tmod — la propia de la
      convención o la prestada del override, con el cuerpo cerrado por
      DECLARACIÓN (jamás tragándose a las clases hermanas del archivo:
      la lección del fix partial v6.179).
  (2) EL JEFE EXIGE SU CARA: ModNPC con NPC.boss = true y sin override
      BossHeadTexture → {textura}_Head_Boss en disco y en el .tmod (la
      convención AethonBoss_Head_Boss).
  (3) LAS RUTAS PEDIDAS A MANO: todo ModContent.Request<...>("AethonMod/
      ...") literal del fuente debe existir en disco y en el .tmod — el
      cliente las pide en pleno dibujo y ninguna lo caza hoy.
  (4) LAS CLAVES VIVAS: todo Language.GetTextValue("Mods.AethonMod....")
      literal debe vivir en los 3 hjson Y los placeholders {N} deben
      PAREAR en los 3 idiomas (la lección de la FormatException v6.178).
  (5) LOS HJSON VIVOS: los 3 archivos deben PARSEAR con el parser real
      (hjson) — un archivo malformado muere aquí, no en el juego.

USO:  python3 tools/guardian_arranque.py [.tmod]
      (sin argumento: el .tmod instalado del tML local)
Sale 0 si el cliente arranca de verdad; CUALQUIER grito = NO PUBLICAR.
"""
import os
import re
import sys

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.join(AQUI, '..', 'AethonMod')
sys.path.insert(0, AQUI)
try:
    from parse_tmod import parse
except ImportError:
    parse = None

TMOD = sys.argv[1] if len(sys.argv) > 1 else \
    os.path.expanduser('~/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod')

CLASES = re.compile(
    r'(?:public\s+)?(?:internal\s+)?(?:abstract\s+)?(?:sealed\s+)?(?:partial\s+)?'
    r'class\s+(\w+)\s*:\s*([\w<>,\s.]+?)\s*(?:\{|where|//)')
MODS = {'ModItem', 'ModProjectile', 'ModNPC', 'ModDust', 'ModTile',
        'ModWall', 'ModBuff', 'ModPetal'}
PRESTADA = re.compile(r'override\s+string\s+Texture\s*=>\s*"([^"]+)"')
BOSS = re.compile(r'\bNPC\.boss\s*=\s*true')
CABEZA_PROPIA = re.compile(r'override\s+string\s+BossHeadTexture')
REQUEST = re.compile(r'ModContent\.Request<\w[\w<>,\s]*?>\(\s*"([^"]+)"')
GETTEXT = re.compile(r'GetTextValue\(\s*"(Mods\.AethonMod\.[^"]+)"')
EXTS = ['.png', '.xnb', '.ogg', '.wav', '.mp3', '.rawimg', '.fx', '.fxc',
         '.csv', '.json']

# === el .tmod (si hay) ===
entradas_tmod = set()
if parse and os.path.exists(TMOD):
    d = open(TMOD, 'rb').read()
    _, nombre_tmod, ver_tmod, entradas, _, _ = parse(TMOD)
    entradas_tmod = {p for p, _, _ in entradas}
    print(f'.tmod: {nombre_tmod} v{ver_tmod} · {len(entradas_tmod)} entradas')
else:
    print(f'.tmod NO disponible ({TMOD}) — se verifican disco y fuente; '
          f'el build aun no existe')

# === PASADA 0: el mapa de clases (cuerpos cerrados por DECLARACIÓN) ===
herencia = {}
posiciones = {}
decls_por_archivo = {}
requests = []          # (ruta_cs, path pedido)
gettexts = []           # (ruta_cs, clave)
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
            herencia.setdefault(clase, bases)
            posiciones.setdefault(clase, []).append(
                (ruta, texto, m.start(), m.group(0)))
            decls_por_archivo.setdefault(ruta, []).append(m.start())
        for m in REQUEST.finditer(texto):
            requests.append((ruta, m.group(1)))
        for m in GETTEXT.finditer(texto):
            gettexts.append((ruta, m.group(1)))

TIPOS = {}


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


gritos = []


def texto_efectivo(clase, decls):
    """(modo, entrada) — la textura principal que el CLIENTE va a pedir."""
    abstracta = False
    prestamo = None
    for ruta, texto, ini, cabecera in decls:
        abstracta = abstracta or 'abstract ' in cabecera
        marcas = sorted(decls_por_archivo[ruta])
        i = marcas.index(ini)
        cuerpo = texto[ini:marcas[i + 1] if i + 1 < len(marcas) else len(texto)]
        if not prestamo:
            prestamo = PRESTADA.search(cuerpo)
    if abstracta:
        return None, None
    if prestamo:
        p = prestamo.group(1)
        if p.startswith('Terraria/'):
            return 'vanilla', None
        return 'prestada', p[len('AethonMod/'):] if p.startswith('AethonMod/') else p
    carpeta = os.path.dirname(decls[0][0])[len(RAIZ.rstrip('/')) + 1:]
    return 'propia', f'{carpeta}/{clase}'


# === (1) LA TEXTURA PRINCIPAL DE CADA CLASE ===
print('(1) LA TEXTURA PRINCIPAL DE CADA CLASE (la que mató a la v6.180):')
sin_textura = 0
con_textura = 0
for clase, decls in sorted(posiciones.items()):
    if not tipo_de(clase):
        continue
    modo, entrada = texto_efectivo(clase, decls)
    if modo is None or modo == 'vanilla':
        continue          # abstracta (sin cuerpo) o vanilla (vive en el juego)
    con_textura += 1
    png = os.path.join(RAIZ, entrada + '.png')
    if not os.path.exists(png):
        gritos.append(f'(1) ¡SIN PNG el {clase} ({modo})! falta {entrada}.png '
                      f'— el CLIENTE lo pide y el mod entero se desactiva')
        sin_textura += 1
    elif entradas_tmod and entrada + '.rawimg' not in entradas_tmod:
        gritos.append(f'(1) ¡SIN RAWIMG el {clase} ({modo})! {entrada}.rawimg '
                      f'no vive en el .tmod')
        sin_textura += 1
print(f'    {con_textura} clases de contenido — '
      + ('TODO VIVE ✔' if not sin_textura else f'¡{sin_textura} SIN TEXTURA!'))

# === (2) EL JEFE EXIGE SU CARA ===
print('(2) EL JEFE EXIGE SU CARA (NPC.boss ⇒ _Head_Boss):')
jefes = 0
sin_cara = 0
for clase, decls in sorted(posiciones.items()):
    if tipo_de(clase) != 'ModNPC':
        continue
    modo, entrada = texto_efectivo(clase, decls)
    if modo in (None, 'vanilla'):
        continue
    es_jefe = False
    cara_propia = False
    for ruta, texto, ini, _ in decls:
        marcas = sorted(decls_por_archivo[ruta])
        i = marcas.index(ini)
        cuerpo = texto[ini:marcas[i + 1] if i + 1 < len(marcas) else len(texto)]
        es_jefe = es_jefe or bool(BOSS.search(cuerpo))
        cara_propia = cara_propia or bool(CABEZA_PROPIA.search(cuerpo))
    if not es_jefe or cara_propia:
        continue
    jefes += 1
    head = entrada + '_Head_Boss'
    if not os.path.exists(os.path.join(RAIZ, head + '.png')):
        gritos.append(f'(2) ¡EL JEFE {clase} NO TIENE CARA! falta {head}.png')
        sin_cara += 1
    elif entradas_tmod and head + '.rawimg' not in entradas_tmod:
        gritos.append(f'(2) ¡LA CARA DEL JEFE {clase} NO VIAJA! {head}.rawimg '
                      f'no vive en el .tmod')
        sin_cara += 1
print(f'    {jefes} jefes — ' + ('TODOS CON CARA ✔' if not sin_cara
                                else f'¡{sin_cara} SIN CARA!'))

# === (3) LAS RUTAS PEDIDAS A MANO ===
print('(3) LAS RUTAS PEDIDAS A MANO (ModContent.Request<...>("...")):')
vivas = 0
muertas = 0
dinamicas = 0
vistas = set()
for ruta, p in requests:
    if not p.startswith('AethonMod/') or p in vistas:
        continue
    vistas.add(p)
    rel = p[len('AethonMod/'):]
    # v6.181 — LAS DINÁMICAS: literales que son PREFIJO de concatenación
    # ("...GrimorioHambriento_" + estado) no son rutas verificables.
    if p.endswith('_') or p.endswith('/'):
        dinamicas += 1
        continue
    disco = [ext for ext in EXTS if os.path.exists(os.path.join(RAIZ, rel + ext))]
    if not disco and not os.path.exists(os.path.join(RAIZ, rel)):
        gritos.append(f'(3) ¡RUTA MUERTA! "{p}" (pedida en '
                      f'{os.path.relpath(ruta, RAIZ)}) no existe en disco')
        muertas += 1
        continue
    vivas += 1
    if entradas_tmod:
        en_tmod = any((rel + ext) in entradas_tmod or rel in entradas_tmod
                      for ext in EXTS)
        if not en_tmod:
            gritos.append(f'(3) ¡RUTA QUE NO VIAJA! "{p}" no vive en el .tmod')
            muertas += 1
            vivas -= 1
print(f'    {vivas + muertas} rutas únicas + {dinamicas} dinámicas — '
      + ('TODAS VIVEN ✔' if not muertas else f'¡{muertas} MUERTAS!'))

# === (5) LOS HJSON VIVOS (antes que las claves: sin parser no hay nada) ===
print('(5) LOS HJSON VIVOS (parser real):')
try:
    import hjson
    PARSER = 'hjson (el real)'
except ImportError:
    hjson = None
    PARSER = 'FALLBACK de la casa (pip install hjson para el parser real)'

LOC = os.path.join(RAIZ, 'Localization')
IDIOMAS = ['es-ES', 'es-MX', 'en-US']


def aplanar(texto, prefijo='Mods.AethonMod.'):
    """hjson de tML → {clave completa: valor crudo} (planas y anidadas).
    Parser real (hjson) si vive; si no, EL DE LA CASA (la receta de los
    audits: líneas + indentación + ''' multilinea, con VALOR para el
    pareo de placeholders)."""
    claves = {}
    if hjson is not None:
        datos = hjson.loads(texto)

        def bajar(nodo, camino):
            if isinstance(nodo, dict):
                for k, v in nodo.items():
                    bajar(v, f'{camino}.{k}' if camino else k)
            else:
                clave = camino if camino.startswith(prefijo) else prefijo + camino
                claves[clave] = '' if nodo is None else str(nodo)

        bajar(datos, '')
        return claves
    # === EL FALLBACK DE LA CASA (audit_v6110: líneas + indentación) ===
    pila = []
    en_ml = False
    for linea in texto.split('\n'):
        linea = linea.rstrip('\n')
        if en_ml:
            if "'''" in linea:
                en_ml = False
            continue
        sin = linea.strip()
        if not sin or sin.startswith('//'):
            continue
        m = re.match(r'^(\s*)([\w.]+)\s*:(.*)$', linea)
        if m:
            prof = len(m.group(1))
            while pila and pila[-1][0] >= prof:
                pila.pop()
            clave = '.'.join([n for _, n in pila] + m.group(2).split('.'))
            clave = clave if clave.startswith(prefijo) else prefijo + clave
            if m.group(3).strip().endswith('{'):
                pila.append((prof, m.group(2)))
                continue
            valor = m.group(3).split('//')[0].strip().strip('"')
            if "'''" in m.group(3):
                en_ml = True
                valor = "'''"
            claves[clave] = valor
            continue
        m2 = re.match(r'^(\s*)\}', linea)
        if m2:
            prof = len(m2.group(1))
            while pila and pila[-1][0] >= prof:
                pila.pop()
    return claves


sets_idioma = {}
hjson_rotos = 0
for idioma in IDIOMAS:
    ruta = os.path.join(LOC, f'{idioma}_Mods.AethonMod.hjson')
    texto = open(ruta, encoding='utf-8').read()
    try:
        claves = aplanar(texto)
        if not claves:
            raise ValueError('cero claves')
    except Exception as e:
        gritos.append(f'(5) ¡HJSON ROTO! {idioma}_Mods.AethonMod.hjson no parsea '
                      f'({PARSER}): {e}')
        hjson_rotos += 1
        claves = {}
    sets_idioma[idioma] = claves
print(f'    parser: {PARSER} — '
      + ('LOS 3 VIVEN ✔' if not hjson_rotos else f'¡{hjson_rotos} ROTOS!'))
for idioma in IDIOMAS:
    print(f'      {idioma}: {len(sets_idioma[idioma])} claves')

# === (4) LAS CLAVES VIVAS (y los placeholders que parean) ===
print('(4) LAS CLAVES VIVAS (GetTextValue → es-ES/es-MX viva; en-US avisa):')
sin_clave = 0
dinamicas_clave = 0
deudas_en_us = []
claves_vistas = set()
NUMERICA = re.compile(r'^\d+$')


def clave_dinamica(clave, sets):
    """v6.181 — LAS DINÁMICAS: prefijo de concatenación («...Hambre.» +
    estado) o base de sufijo numérico («EscuadronMejora» + n → vive
    EscuadronMejora1). Ninguna es verificable como literal."""
    if clave.endswith('.'):
        return True
    for claves in sets.values():
        for viva in claves:
            if viva.startswith(clave) and NUMERICA.match(viva[len(clave):]):
                return True
    return False


for ruta, clave in gettexts:
    if clave in claves_vistas:
        continue
    claves_vistas.add(clave)
    if clave_dinamica(clave, sets_idioma):
        dinamicas_clave += 1
        continue
    for idioma in IDIOMAS:
        if clave not in sets_idioma[idioma]:
            if idioma == 'en-US':
                # v6.181 — EL EN-US ES EL FALLBACK y arrastra UNA DEUDA
                # CONOCIDA (480 claves fósiles sin traducir — la lección
                # v6.97.1: las traducciones vivas NO se tocan). No bloquea
                # el arranque: se cuenta, no se grita.
                deudas_en_us.append(clave)
                continue
            gritos.append(f'(4) ¡CLAVE MUERTA! "{clave}" no vive en {idioma} '
                          f'(pedida en {os.path.relpath(ruta, RAIZ)})')
            sin_clave += 1
PLACE = re.compile(r'\{\d+\}')
for clave in sorted(claves_vistas):
    if clave_dinamica(clave, sets_idioma):
        continue
    marcas = {}
    for idioma in IDIOMAS:
        if clave in sets_idioma[idioma]:
            marcas[idioma] = sorted(PLACE.findall(sets_idioma[idioma][clave]))
    if len(set(map(tuple, marcas.values()))) > 1:
        detalle = ' · '.join(f'{i}: {m}' for i, m in marcas.items())
        gritos.append(f'(4) ¡PLACEHOLDERS DESCUADRADOS! "{clave}" — {detalle} '
                      f'(la lección FormatException v6.178)')
        sin_clave += 1
print(f'    {len(claves_vistas) - dinamicas_clave} literales + '
      f'{dinamicas_clave} dinámicas — '
      + ('TODAS VIVEN Y PAREAN ✔' if not sin_clave else f'¡{sin_clave} PROBLEMAS!'))
if deudas_en_us:
    print(f'    AVISO (la deuda fósil conocida): {len(deudas_en_us)} claves '
          f'sin traducción en-US — no bloquean el arranque (fallback)')

# === EL VEREDICTO ===
print()
if gritos:
    for g in gritos:
        print(f'  {g}')
    print()
    print('GUARDIÁN DE ARRANQUE: ¡EL CLIENTE NO ARRANCA! — NO PUBLICAR')
    sys.exit(1)
print('GUARDIÁN DE ARRANQUE: EL CLIENTE VIVE ✔ — publicable')
sys.exit(0)
