#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""fijar_decomp.py — normaliza el output de ilspycmd 9.1 a C# válido.

Artefactos que cura (en orden):
  1. (T)(ref <expr>) → <expr>          [con balance de paréntesis consciente de strings]
  2. TYPE x = default(TYPE); (x)._002Ector(args);  →  TYPE x = new TYPE(args);
  3. orig_X/hook_X sin calificar → On_Main.orig_X / On_Main.hook_X (delegados MonoMod)
  4. HitInfo sin calificar → NPC.HitInfo
  5. TileActionAttempt sin calificar → Utils.TileActionAttempt

Aplicar SIEMPRE antes de usar el árbol decompilado (splice o verificación).
"""
import re, sys, os

def saltar_string(texto, k):
    """k apunta a la comilla de apertura; devuelve índice tras la comilla de cierre."""
    q = texto[k]; k += 1
    while k < len(texto):
        if texto[k] == '\\': k += 2; continue
        if texto[k] == q: return k + 1
        k += 1
    return k

def match_paren(texto, open_pos):
    """índice del ')' que cierra el '(' en open_pos (consciente de strings y //)."""
    prof = 0; k = open_pos
    while k < len(texto):
        c = texto[k]
        if c == '"' or c == "'":
            k = saltar_string(texto, k); continue
        if c == '/' and k + 1 < len(texto) and texto[k+1] == '/':
            nl = texto.find('\n', k)
            if nl < 0: return -1
            k = nl + 1; continue
        if c == '(':
            prof += 1
        elif c == ')':
            prof -= 1
            if prof == 0: return k
        k += 1
    return -1

REF_ART = re.compile(r'\([A-Za-z_][A-Za-z0-9_]*\)\(ref\s')

def fix_ref(text):
    out = []; i = 0; n = len(text); fixes = 0
    while i < n:
        m = REF_ART.match(text, i)
        if m:
            open_pos = text.rfind('(', m.start(), m.end())
            close = match_paren(text, open_pos)
            if close < 0:
                out.append(text[i]); i += 1; continue
            inner = text[open_pos + 4:close].strip()  # tras "(ref" hasta close
            out.append(inner)
            i = close + 1
            fixes += 1
        else:
            out.append(text[i]); i += 1
    return ''.join(out), fixes

CTOR_ART = re.compile(
    r'^([ \t]*)([A-Za-z_][\w.<>\[\], ]*?)\s+([A-Za-z_]\w*)\s*=\s*default\(\2\);[ \t]*\n'
    r'[ \t]*\(\3\)\._002Ector\((.*)\);[ \t]*$',
    re.M)

def fix_ctor(text):
    def rep(m):
        ind, tipo, nombre, args = m.groups()
        return f'{ind}{tipo} {nombre} = new {tipo}({args});'
    return CTOR_ART.subn(rep, text)

def fix_hooks(text):
    return re.subn(r'(?<![.\w])(orig_UpdateAudio|hook_UpdateAudio)\b', r'On_Main.\1', text)

def fix_hitinfo(text):
    return re.subn(r'(?<![.\w])HitInfo\b', 'NPC.HitInfo', text)

def fix_taa(text):
    return re.subn(r'(?<![.\w])TileActionAttempt\b', 'Utils.TileActionAttempt', text)

def fix_sets(text):
    # ILSpy emite `Sets.X` sin el ProjectileID. (imprecisión de resolución)
    return re.subn(r'(?<![.\w])Sets\.', 'ProjectileID.Sets.', text)

def fix_strip(text):
    # delegados anidados en Terraria.Graphics.VertexStrip
    text, a = re.subn(r'(?<![.\w])StripColorFunction\b', 'VertexStrip.StripColorFunction', text)
    text, b = re.subn(r'(?<![.\w])StripHalfWidthFunction\b', 'VertexStrip.StripHalfWidthFunction', text)
    return text, a + b

def procesar(text):
    text, n_ref = fix_ref(text)
    text, n_ctor = fix_ctor(text)
    text, n_h = fix_hooks(text)
    text, n_hi = fix_hitinfo(text)
    text, n_taa = fix_taa(text)
    text, n_sets = fix_sets(text)
    text, n_strip = fix_strip(text)
    return text, (n_ref, n_ctor, n_h, n_hi, n_taa, n_sets, n_strip)

if __name__ == '__main__':
    raiz = sys.argv[1]
    tot = [0, 0, 0, 0, 0, 0, 0]
    restantes_ctor = 0
    for root, _, files in os.walk(raiz):
        for fn in files:
            if not fn.endswith('.cs'): continue
            p = os.path.join(root, fn)
            t = open(p, encoding='utf-8').read()
            t2, st = procesar(t)
            if t2 != t:
                open(p, 'w', encoding='utf-8').write(t2)
            for k in range(7): tot[k] += st[k]
            if '_002Ector' in t2:
                restantes_ctor += t2.count('_002Ector')
    print(f'ref={tot[0]} ctor={tot[1]} hooks={tot[2]} hitinfo={tot[3]} taa={tot[4]} sets={tot[5]} strip={tot[6]}')
    if restantes_ctor:
        print(f'¡OJO! {restantes_ctor} _002Ector sin fusionar (revisar manualmente)')
