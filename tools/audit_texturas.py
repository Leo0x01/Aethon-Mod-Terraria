#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_texturas.py — LA LECCIÓN DEL client.log v6.50.99 (permanente).

La letra del usuario: «hay errores… justo ahora no veo nada en github la
.99 no esta subida» → el client.log: tML DESACTIVÓ el mod entero con
TRES MissingResourceException (BolsaEmperatriz, EstelaAethonProjectile,
TrazoEmperatrizProjectile — las tres clases nuevas SIN su .png).

NI el -build NI el servidor headless validan texturas: SOLO el cliente
las pide (AssetRepository.Request en AutoStaticDefaults). Este script es
el gate que faltaba: TODA clase cargable que herede (transitivamente) de
un tipo con textura por convención DEBE tener su .png junto al .cs —
salvo que la clase sea abstracta o declare `override string Texture`.

Se corre ANTES de cada build. Sale con código 1 si falta alguna.
"""
import os
import re
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
os.chdir(ROOT)

if len(sys.argv) > 1 and os.path.isdir(sys.argv[1]):
    os.chdir(sys.argv[1])

CLASS_RE = re.compile(
    r'(?:^|\n)[ \t]*(?:public|internal)?[ \t]*(?:sealed |abstract |partial |static )*'
    r'(?:class|struct)\s+(\w+)\s*(?::\s*([^{]+))?\s*\{')

# Tipos de tML con textura por convención (los que el mod usa)
MOD_BASES = {'ModItem', 'ModProjectile', 'ModNPC', 'ModBuff', 'ModDust',
             'ModTile', 'ModWall', 'ModPrefix', 'ModMount', 'ModBossHead',
             'ModPylon'}

info = {}
for dirpath, _, files in os.walk('Content'):
    for f in files:
        if not f.endswith('.cs'):
            continue
        cs = os.path.join(dirpath, f)
        src = open(cs, encoding='utf-8', errors='replace').read()
        for m in CLASS_RE.finditer(src):
            name = m.group(1)
            bases = [b.strip() for b in (m.group(2) or '').split(',') if b.strip()]
            line_start = src.rfind('\n', 0, m.start()) + 1
            header = src[line_start:m.end()]
            tex_override = re.search(r'override\s+string\s+Texture', src[m.start():])
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


def ruta_textura(cs_file, name):
    """La ruta del .png de una clase: junto al .cs, o REDIRIGIDA para el
    puerto de Goozma (el SmartContentSource del mod mapea
    Content/GoozmaPort/{Content,Common}/** → Assets/Textures/** — la
    convención de CalamityHunt, v6.53). v6.53.1: el puerto VIVE en
    research/GoozmaPort (fuera del mod, nunca se compila) — este redirect
    quedó INERTE y se conserva como documentación histórica."""
    d = os.path.dirname(cs_file)
    for prefijo in ('Content/GoozmaPort/Content', 'Content/GoozmaPort/Common'):
        norm = d.replace(os.sep, '/')
        if norm.startswith(prefijo):
            resto = norm[len(prefijo):].lstrip('/')
            return os.path.join('Content/GoozmaPort/Assets/Textures', resto, name + '.png')
    return os.path.join(d, name + '.png')

faltan = []
for name, d in sorted(info.items()):
    if d['abstract'] or d['tex'] or d['generic']:
        continue
    base = base_requerida(name)
    if base is None:
        continue
    tex = ruta_textura(d['file'], name)
    if not os.path.exists(tex):
        faltan.append((base, name, d['file']))

if faltan:
    print('!!! FALTAN TEXTURAS (el cliente desactivará el mod):')
    for base, name, f in faltan:
        print(f'  [{base}] {name}  <-  {f}  (falta {name}.png)')
    sys.exit(1)

print('OK — toda clase cargable tiene su .png (gate de la lección v6.50.99)')
