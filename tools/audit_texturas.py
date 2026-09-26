import os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from parse_tmod import parse

SRC = '/home/z/my-project/AethonMod/AethonMod'
MODROOT = 'AethonMod'

TEXTURED_BASES = {'ModNPC','ModProjectile','ModItem','ModDust','ModBuff','ModTile','ModWall','ModPylon','ModTree','ModPlant','ModMount'}

# ---------- packed entries ----------
packed = set()
if len(sys.argv) > 1 and sys.argv[1] not in ('-', '-v'):
    _, name, ver, entries, _, _ = parse(sys.argv[1])
    packed = {p for p, _, _ in entries}
    print(f'[i] packed: {name} {ver} ({len(packed)} entries)')

# ---------- filesystem ----------
disk = set()
for root, dirs, files in os.walk(SRC):
    for f in files:
        disk.add(os.path.relpath(os.path.join(root, f), SRC).replace(os.sep, '/'))

cls_re  = re.compile(r'((?:(?:public|internal|private|protected|static|sealed|abstract|partial)\s+)*)class\s+(\w+)\s*:\s*([^{]+)')
ns_re   = re.compile(r'^\s*namespace\s+([\w.]+)', re.M)
tex_re  = re.compile(r'override\s+string\s+Texture\s*=>\s*"([^"]+)"')
req_re  = re.compile(r'Request<[^<>]*Texture2D>\(\s*"([^"]+)"(?=\s*[,)])')

all_classes = []
tex_overrides = []
req_paths = []

for dp, _, fns in os.walk(SRC):
    for fn in fns:
        if not fn.endswith('.cs'):
            continue
        path = os.path.join(dp, fn)
        rel = os.path.relpath(path, SRC).replace(os.sep, '/')
        txt = open(path, encoding='utf-8', errors='replace').read()
        fns_decls = [(m.start(), m.group(1)) for m in ns_re.finditer(txt)]
        for m in cls_re.finditer(txt):
            cname = m.group(2)
            mods = m.group(1) or ''
            bases = [b.strip().split('.')[-1].split('<')[0].strip() for b in m.group(3).split(',')]
            is_abstract = 'abstract' in mods  # tML no autoloada abstract: no pide textura
            ns = MODROOT
            for pos, n in fns_decls:
                if pos < m.start():
                    ns = n
            all_classes.append({'name': cname, 'ns': ns, 'bases': bases, 'file': rel, 'pos': m.start(), 'abstract': is_abstract})
        for m in tex_re.finditer(txt):
            tex_overrides.append((m.start(), m.group(1), rel))
        for m in req_re.finditer(txt):
            req_paths.append((m.group(1), rel))

by_key = {(c['ns'], c['name']): c for c in all_classes}

def resolve_base(basename, from_ns, seen):
    if basename in TEXTURED_BASES:
        return True
    if basename in seen:
        return False
    seen = seen | {basename}
    c = by_key.get((from_ns, basename))
    if c is None:
        cands = [x for x in all_classes if x['name'] == basename]
        if len(cands) == 1:
            c = cands[0]
        elif cands:
            return any(resolve_base(b, x['ns'], seen) for x in cands for b in x['bases'])
    if c is None:
        return False
    return any(resolve_base(b, c['ns'], seen) for b in c['bases'])

def exists(path_noext, where):
    if where == 'disk':
        return (path_noext + '.png') in disk or (path_noext + '.rawimg') in disk or (path_noext + '.jpg') in disk
    return (path_noext + '.rawimg') in packed or (path_noext + '.png') in packed

def strip_mod(path):
    if path.startswith(MODROOT + '/'):
        return path[len(MODROOT) + 1:]
    return path

def owner_of(pos, rel):
    best = None
    for c in all_classes:
        if c['file'] == rel and c['pos'] < pos:
            if best is None or c['pos'] > best['pos']:
                best = c
    return best

problems = []
audit_lines = []

tex_by_class = {}
for pos, path, rel in tex_overrides:
    o = owner_of(pos, rel)
    if o:
        tex_by_class[(o['ns'], o['name'])] = path

need_tex = []
for c in all_classes:
    if c['abstract']:
        continue
    if any(resolve_base(b, c['ns'], {c['name']}) for b in c['bases']):
        need_tex.append(c)

for c in sorted(need_tex, key=lambda x: (x['ns'], x['name'])):
    key = (c['ns'], c['name'])
    fq = c['ns'] + '.' + c['name']
    if key in tex_by_class:
        path = strip_mod(tex_by_class[key])
        ok_disk = exists(path, 'disk')
        ok_packed = (not packed) or exists(path, 'packed')
        audit_lines.append(f'[{"OK" if ok_disk and (not packed or ok_packed) else "MISS"}] {fq} -> override {path} (disk={ok_disk} packed={ok_packed})')
        if not ok_disk:
            problems.append(('LOAD', f'{fq}: override Texture "{tex_by_class[key]}" - el archivo NO existe en la fuente'))
        elif packed and not ok_packed:
            problems.append(('LOAD', f'{fq}: override Texture "{tex_by_class[key]}" - NO esta en el .tmod'))
    else:
        path = strip_mod(c['ns'].replace('.', '/') + '/' + c['name'])
        ok_disk = exists(path, 'disk')
        ok_packed = (not packed) or exists(path, 'packed')
        audit_lines.append(f'[{"OK" if ok_disk and (not packed or ok_packed) else "MISS"}] {fq} -> default {path} (disk={ok_disk} packed={ok_packed})')
        if not ok_disk:
            problems.append(('LOAD', f'{fq}: textura por defecto {path}.png NO existe -> MissingResourceException AL CARGAR'))
        elif packed and not ok_packed:
            problems.append(('LOAD', f'{fq}: textura por defecto {path} NO esta en el .tmod'))

for path, rel in req_paths:
    p = strip_mod(path)
    ok_disk = exists(p, 'disk')
    ok_packed = (not packed) or exists(p, 'packed')
    audit_lines.append(f'[{"OK" if ok_disk and (not packed or ok_packed) else "MISS"}] Request "{path}" ({rel}) disk={ok_disk} packed={ok_packed}')
    if not ok_disk:
        problems.append(('RUNTIME', f'Request "{path}" ({rel}) - el archivo NO existe'))
    elif packed and not ok_packed:
        problems.append(('RUNTIME', f'Request "{path}" ({rel}) - NO esta en el .tmod'))

print(f'[i] clases con textura: {len(need_tex)} (abstract excluidas) | overrides Texture: {len(tex_overrides)} | Requests literales: {len(req_paths)}')
if problems:
    print(f'\n!!!! {len(problems)} PROBLEMAS !!!!')
    for kind, msg in problems:
        print(f'  [{kind}] {msg}')
else:
    print('\n[OK] AUDITORIA LIMPIA: toda clase con textura y todo Request literal resuelve')
if '-v' in sys.argv:
    print('\n--- detalle ---')
    for l in audit_lines:
        print(l)
