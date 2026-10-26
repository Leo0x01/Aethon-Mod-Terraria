#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_texturas.py — LA LECCIÓN DEL client.log v6.50.99 (permanente) +
LA LECCIÓN DEL client.log v6.54.0 (v6.54.1).

La letra del usuario (v6.50.99): «hay errores… justo ahora no veo nada en
github la .99 no esta subida» → el client.log: tML DESACTIVÓ el mod entero
con TRES MissingResourceException (BolsaEmperatriz, EstelaAethonProjectile,
TrazoEmperatrizProjectile — las tres clases nuevas SIN su .png).

La letra del usuario (v6.54.0): «hay errores» → el client.log: OTRA VEZ,
TRES bolsas nuevas SIN su .png (BolsaDeAlasPrisma, BolsaInvestigacion,
BolsaMaestra). LA CAUSA DE LA REINCIDENCIA: desde v6.51 el mod vive en
AethonMod/Content (no en Content/) y este script caminaba una carpeta
INEXISTENTE → cero clases → «OK» FALSO. Además el chequeo del
`override string Texture` buscaba HASTA EL FINAL DEL ARCHIVO: una clase
posterior que lo declarara marcaba a TODAS las anteriores como «con
textura» (falso negativo).

NI el -build NI el servidor headless validan texturas: SOLO el cliente
las pide (AssetRepository.Request en AutoStaticDefaults). Este script es
el gate que faltaba: TODA clase cargable que herede (transitivamente) de
un tipo con textura por convención DEBE tener su .png junto al .cs —
salvo que la clase sea abstracta, genérica, o declare (ella o un ancestro
del propio mod) `override string Texture`.

Se corre ANTES de cada build. Sale con código 1 si falta alguna.
"""
import os
import re
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
os.chdir(ROOT)

# v6.54.1 — AUTO-DETECCIÓN DE LA RAÍZ DEL MOD (la lección de la .54):
# el layout migró de Content/ (raíz) a AethonMod/Content (v6.51+). Se
# auditan TODAS las raíces que existan — caminar una carpeta muerta y
# decir «OK» es exactamente el falso positivo que publicó la .54.
RAICES = [r for r in ('Content', 'AethonMod/Content') if os.path.isdir(r)]
if not RAICES:
    print('!!! No encontré ninguna raíz de contenido (¿dónde vive el mod?)')
    sys.exit(1)

CLASS_RE = re.compile(
    r'(?:^|\n)[ \t]*(?:public|internal)?[ \t]*(?:sealed |abstract |partial |static )*'
    r'(?:class|struct)\s+(\w+)\s*(?::\s*([^{]+))?\s*\{')

# Tipos de tML con textura por convención (los que el mod usa)
MOD_BASES = {'ModItem', 'ModProjectile', 'ModNPC', 'ModBuff', 'ModDust',
             'ModTile', 'ModWall', 'ModPrefix', 'ModMount', 'ModBossHead',
             'ModPylon'}

info = {}
for raiz in RAICES:
    for dirpath, _, files in os.walk(raiz):
        for f in files:
            if not f.endswith('.cs'):
                continue
            cs = os.path.join(dirpath, f)
            src = open(cs, encoding='utf-8', errors='replace').read()
            matches = list(CLASS_RE.finditer(src))
            for i, m in enumerate(matches):
                name = m.group(1)
                bases = [b.strip() for b in (m.group(2) or '').split(',') if b.strip()]
                line_start = src.rfind('\n', 0, m.start()) + 1
                header = src[line_start:m.end()]
                # v6.54.1 — EL SCOPE CORRECTO: el override de Texture vale
                # SOLO si está en EL CUERPO DE ESTA clase (de su llave de
                # apertura a la siguiente declaración de clase del archivo),
                # no «en cualquier parte después de mí».
                cuerpo_fin = matches[i + 1].start() if i + 1 < len(matches) else len(src)
                cuerpo = src[m.end():cuerpo_fin]
                tex_override = re.search(r'override\s+string\s+Texture', cuerpo)
                info[name] = {'bases': bases, 'file': cs,
                              'abstract': 'abstract ' in header,
                              'tex': bool(tex_override),
                              'generic': bool(bases) and '<' in bases[0]}


def base_requerida(name, seen=None):
    """La base con textura, siguiendo la herencia transitiva del propio mod."""
    if seen is None:
        seen = set()
    if name in seen or name not in info:
        return None
    seen.add(name)
    for b in info[name]['bases']:
        b0 = b.split('<')[0].strip()
        if b0 in MOD_BASES:
            return b0
        r = base_requerida(b0, seen)
        if r:
            return r
    return None


def textura_heredada(name, seen=None):
    """v6.54.1 — ¿algún ANCESTRO del propio mod declara `override string
    Texture`? Sus herederos NO necesitan .png propio (heredan la ruta)."""
    if seen is None:
        seen = set()
    if name in seen or name not in info:
        return False
    seen.add(name)
    if info[name]['tex']:
        return True
    return any(textura_heredada(b.split('<')[0].strip(), seen)
               for b in info[name]['bases'])


def ruta_textura(cs_file, name):
    """La ruta del .png de una clase: junto al .cs."""
    return os.path.join(os.path.dirname(cs_file), name + '.png')


faltan = []
for name, d in sorted(info.items()):
    if d['abstract'] or d['generic'] or d['tex'] or textura_heredada(name):
        continue
    base = base_requerida(name)
    if base is None:
        continue
    tex = ruta_textura(d['file'], name)
    if not os.path.exists(tex):
        faltan.append((base, name, d['file']))

print(f'Audit de texturas — raíces: {", ".join(RAICES)} · {len(info)} clases inspeccionadas')
if faltan:
    print('!!! FALTAN TEXTURAS (el cliente desactivará el mod):')
    for base, name, f in faltan:
        print(f'  [{base}] {name}  <-  {f}  (falta {name}.png)')
    sys.exit(1)

print('OK — toda clase cargable tiene su .png (gate de la lección v6.50.99 + v6.54.1)')
