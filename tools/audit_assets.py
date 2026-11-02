#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Auditoría de assets: toda clase que herede (directa o transitivamente) de un
ModType con textura automática (ModItem/ModProjectile/ModNPC/ModDust/ModBuff/
ModTile/ModWall/ModMount) y cuya cadena NO defina `override string Texture`
debe tener su PNG en {namespace}/{Clase}.png. Reporta los que falten."""
import os, re, sys

ROOT = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"
AUTOTEX_BASES = {"ModItem", "ModProjectile", "ModNPC", "ModDust", "ModBuff",
                 "ModTile", "ModWall", "ModMount", "ModTileEntity"}

cls_re = re.compile(
    r'^[ \t]*(?:public|internal|private|protected)?[ \t]*'
    r'(?:sealed[ \t]+|abstract[ \t]+|static[ \t]+|partial[ \t]+)*'
    r'class[ \t]+(\w+)(?:<[^(]*>)?[ \t]*:[ \t]*([^\{]+?)\s*$',
    re.M)

def blocks(src):
    """Itera (nombre, bases, cuerpo) por clase con balance de llaves."""
    out = []
    for m in cls_re.finditer(src):
        name, bases = m.group(1), m.group(2)
        start = src.find('{', m.end())
        if start < 0:
            continue
        depth, i = 0, start
        while i < len(src):
            c = src[i]
            if c == '{': depth += 1
            elif c == '}':
                depth -= 1
                if depth == 0: break
            i += 1
        out.append((name, [b.strip() for b in bases.split(',')],
                    src[start:i + 1]))
    return out

# Pass 1: recopilar todas las clases del mod: nombre → (namespace, bases, body)
allcls = {}
for dirpath, dirs, files in os.walk(ROOT):
    dirs[:] = [d for d in dirs if d not in ('obj', 'bin', 'research', 'tools', 'tests', '_masters')]
    for f in files:
        if not f.endswith('.cs'): continue
        p = os.path.join(dirpath, f)
        try: src = open(p, encoding='utf-8-sig', errors='replace').read()
        except Exception as e:
            print(f"LEE-ERROR {p}: {e}"); continue
        ns = None
        nsm = re.search(r'^\s*namespace\s+([\w.]+)', src, re.M)
        if nsm: ns = nsm.group(1)
        for name, bases, body in blocks(src):
            allcls[name] = (ns, bases, body, p)

# clases abstract: no las registra tModLoader → no requieren PNG
abstract_classes = set()
cls_decl_re = re.compile(
    r'(?:public|internal|private|protected)?[ \t]*'
    r'((?:sealed|abstract|static|partial)[ \t]+)*class[ \t]+(\w+)', re.M)
for dirpath, dirs, files in os.walk(ROOT):
    dirs[:] = [d for d in dirs if d not in ('obj', 'bin', 'research', 'tools', 'tests', '_masters')]
    for f in files:
        if not f.endswith('.cs'): continue
        src = open(os.path.join(dirpath, f), encoding='utf-8-sig', errors='replace').read()
        for m in cls_decl_re.finditer(src):
            mods = (m.group(1) or '').split()
            if 'abstract' in mods:
                abstract_classes.add(m.group(2))

def defines_texture(name, seen=None):
    """¿La cadena de herencia define override string Texture?"""
    if seen is None: seen = set()
    if name in seen or name not in allcls: return None
    seen.add(name)
    ns, bases, body, p = allcls[name]
    if re.search(r'override\s+string\s+Texture\s*=>', body) or \
       re.search(r'override\s+string\s+Texture\s*\{', body) or \
       re.search(r'override\s+string\s+Texture\s*\(', body):
        return True
    for b in bases:
        b = b.split('<')[0].strip()
        if defines_texture(b, seen) is True:
            return True
    return False

missing, errors = [], []
for name, (ns, bases, body, p) in sorted(allcls.items()):
    if name in abstract_classes: continue  # abstract → sin registro → sin PNG
    # ¿hereda (transitivamente) de un AUTOTEX_BASE?
    stack, seen, root_hit = list(bases), set(), False
    while stack:
        b = stack.pop(0).split('<')[0].strip()
        if b in seen or b in stack: continue
        seen.add(b)
        if b in AUTOTEX_BASES: root_hit = True; break
        if b in allcls:
            stack.extend(allcls[b][1])
    if not root_hit: continue
    # ¿alguien en la cadena define Texture?
    if defines_texture(name) is True: continue
    # requiere PNG
    if not ns:
        errors.append(f"{p}: clase {name} sin namespace"); continue
    rel = ns[len('AethonMod.'):] if ns.startswith('AethonMod.') else ns
    png = os.path.join(ROOT, rel.replace('.', os.sep), name + '.png')
    if not os.path.exists(png):
        missing.append((name, ns, p))

print(f"Clases escaneadas: {len(allcls)}")
if errors:
    print("\n== ERRORES DE PARSEO ==")
    for e in errors: print(" ", e)
if missing:
    print(f"\n== PNG FALTANTES: {len(missing)} ==")
    for name, ns, p in missing:
        print(f"  {ns}/{name}.png   ({os.path.relpath(p, ROOT)})")
    sys.exit(1)
else:
    print("\n== 0 PNG FALTANTES — TODO ASSET PRESENTE ==")

# ======================================================================
# v6.87 — LA LECCIÓN v6.86: ASSETS POR CONVENCIÓN + RUTAS LITERALES.
# El -build empaqueta sin validar assets de contenido; el headless NO
# carga texturas (server) → el ÚNICO sitio que caza un MissingResource
# es EL JUEGO DEL USUARIO (v6.84: PNG de proyectil; v6.86: _Highlight
# del altar — el mod entero desactivado). ESTAS reglas cierran la caja:
#   A) ModTile con TileID.Sets.HasOutlines[Type]=true → exige
#      {Texture}_Highlight.png (salvo override HighlightTexture).
#   B) Toda ruta literal de asset (Request<T>/GetTexture/SoundStyle/
#      MusicLoader/Effect) debe existir en disco.
# ======================================================================
print("\n== v6.87: AUDITORÍA DE CONVENCIONES ==")
faltantes_conv = []

# --- A) _Highlight de tiles con outlines ---
highlight_re = re.compile(r'TileID\.Sets\.HasOutlines\s*\[\s*Type\s*\]\s*=\s*true')
htex_re = re.compile(r'override\s+string\s+HighlightTexture')
for name, (ns, bases, body, p) in sorted(allcls.items()):
    if not highlight_re.search(body): continue
    if htex_re.search(body): continue
    rel = ns[len('AethonMod.'):] if ns and ns.startswith('AethonMod.') else (ns or '')
    base_png = os.path.join(ROOT, rel.replace('.', os.sep), name + '.png')
    hl = os.path.join(ROOT, rel.replace('.', os.sep), name + '_Highlight.png')
    if not os.path.exists(hl):
        faltantes_conv.append(f"{ns}/{name}_Highlight.png  (tile con HasOutlines)")

# --- B) rutas literales de assets en código ---
ruta_re = re.compile(r'"(AethonMod/[\w/\-]+)"')
exts = ('.png', '.ogg', '.wav', '.mp3', '.fx', '.fxc', '.xnb')
rutas_vistas = {}
def sin_comentarios(cs):
    cs = re.sub(r'/\*.*?\*/', '', cs, flags=re.S)
    return re.sub(r'//[^\n]*', '', cs)
for dirpath, dirs, files in os.walk(ROOT):
    dirs[:] = [d for d in dirs if d not in ('obj', 'bin', 'research', 'tools', 'tests', '_masters')]
    for f in files:
        if not f.endswith('.cs'): continue
        src = sin_comentarios(open(os.path.join(dirpath, f), encoding='utf-8-sig', errors='replace').read())
        for m in ruta_re.finditer(src):
            r = m.group(1)
            # sufijos dinámicos (p.ej. GrimorioHambriento_ + algo): probar el prefijo
            rutas_vistas.setdefault(r, os.path.join(dirpath, f))

for ruta, csfile in sorted(rutas_vistas.items()):
    fs = os.path.join(ROOT, ruta[len('AethonMod/'):])
    if os.path.exists(fs): continue
    # ¿es prefijo dinámico? (la ruta base + '_' + variable) → verificar que
    # exista ALGÚN archivo que empiece por el prefijo
    prefijo = os.path.basename(fs)
    carpeta = os.path.dirname(fs)
    dyn = False
    if os.path.isdir(carpeta):
        for f in os.listdir(carpeta):
            if prefijo.endswith('_'):
                if f.startswith(prefijo): dyn = True; break
            elif f.startswith(prefijo + '_'): dyn = True; break
    if dyn: continue
    # ¿tiene extensión de asset conocida?
    if any(fs.endswith(e) for e in exts):
        faltantes_conv.append(f"{ruta}  (rutas literales en {os.path.relpath(csfile, ROOT)})")
    else:
        # sin extensión → el juego añade .png/.ogg según el loader: probar
        if not (os.path.exists(fs + '.png') or os.path.exists(fs + '.ogg')
                or os.path.exists(fs + '.wav') or os.path.exists(fs + '.fxc')
                or os.path.exists(fs + '.fx')):
            faltantes_conv.append(f"{ruta}  (ruta sin asset en {os.path.relpath(csfile, ROOT)})")

if faltantes_conv:
    print(f"== FALTANTES DE CONVENCIÓN: {len(faltantes_conv)} ==")
    for f in faltantes_conv: print("  " + f)
    sys.exit(1)
else:
    print("== 0 FALTANTES DE CONVENCIÓN (highlights + rutas literales) ==")
