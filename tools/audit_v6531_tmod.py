"""audit_v6531_tmod.py — auditoría del .tmod v6.53.1 — LA SEPARACIÓN.

La letra: «esta parte de Goozma es solo para investigación, nunca lo
usaré realmente y todo su material debería ser borrado antes del
lanzamiento». Este audit PRUEBA que el paquete quedó 100 % limpio:
cero entradas del puerto, cero símbolos del puerto en el DLL, y el
trazo nuevo (material de la casa) presente. + verificación completa
del paquete (EOF, hjson ×3, controles positivos).
"""
import sys, hashlib
sys.path.insert(0, '/home/z/my-project/download/Aethon-Mod-Terraria/tools')
from parse_tmod import parse
import zlib

PATH = sys.argv[1] if len(sys.argv) > 1 else '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'

tmlver, name, ver, entries, tend, fsize = parse(PATH)
data = open(PATH, 'rb').read()
print(f'== {name} v{ver} == tML {tmlver} | {len(entries)} entradas | tabla {tend} | archivo {fsize}')
assert name == 'AethonMod' and ver == '6.53.1', (name, ver)

# 1) EOF exacto
total_comp = sum(c for _, _, c in entries)
assert tend + total_comp == fsize, f'EOF ROTO: {tend}+{total_comp} != {fsize}'
print(f'EOF exacto: {tend}+{total_comp} == {fsize}  OK')

# 2) blobs: inflar por orden
o = tend
blobs = {}
for p, raw, comp in entries:
    b = data[o:o+comp]; o += comp
    blobs[p] = b if comp == raw else zlib.decompress(b, -15)
assert o == fsize, 'blobs no consumen el archivo'

# 3) EL CORAZÓN: CERO material del puerto
puerto = [p for p in blobs if 'GoozmaPort' in p]
assert not puerto, f'ENTRADAS DEL PUERTO PRESENTES: {puerto[:5]} (total {len(puerto)})'
loc = [p for p in blobs if p.startswith('Localization/Goozma')]
assert not loc, f'LOCALIZACIÓN DEL PUERTO PRESENTE: {loc}'
fx = [p for p in blobs if p.endswith('.fx') and 'GoozmaPort' in p]
assert not fx
print('PAQUETE LIMPIO: 0 entradas GoozmaPort · 0 Localization/Goozma (el puerto vive SOLO en research/)')

# 4) EL DLL: símbolos del puerto AUSENTES, material de la casa PRESENTE
dll = blobs['AethonMod.dll']
for MALO in (b'GoozmaPort', b'PluripotentSpawn', b'SmartContentSource',
             b'GradientColor', b'SlimeUtils', b'AssetUtilities'):
    assert MALO not in dll, f'PRESENTE Y PROHIBIDO: {MALO!r}'
print('DLL LIMPIO: GoozmaPort / PluripotentSpawn / SmartContentSource / GradientColor / SlimeUtils — AUSENTES')

for BUENO in (b'PrimerTrazoFajo', b'ColorGoozma', b'FajaTexturadaEnLote',
              b'RayoDeFusion', b'BolsaDelDiosAntiguo', b'ALoLargo'):
    assert BUENO in dll, f'FALTA CONTROL POSITIVO: {BUENO!r}'
print('Controles positivos OK (trazo nuevo + paleta de la casa + armas Goozma propias + bolsa)')

# 5) hjson ×3 con paridad de claves — CON PARSER REAL (bun+hjson).
# v6.53.1 — LA LECCIÓN: el heurístico de líneas daba FALSA paridad desde la
# .52.4 (las claves comentadas y el texto multilínea con ':' engañaban al
# regex — en-US llevaba 24 claves vivas que el español no tenía). Desde
# ahora el gate parsea los hjson de verdad.
import subprocess, tempfile, os
with tempfile.TemporaryDirectory() as td:
    for c in ('es-MX', 'es-ES', 'en-US'):
        open(os.path.join(td, c + '.hjson'), 'wb').write(blobs[f'Localization/{c}_Mods.AethonMod.hjson'])
    js = f'''
import hjson from 'hjson';
import {{ readFileSync }} from 'fs';
const td = '{td}';
const planas = (o, p = '') => {{ const r = []; for (const [k, v] of Object.entries(o)) {{ const ruta = p ? p + '.' + k : k;
  if (v && typeof v === 'object' && !Array.isArray(v)) r.push(...planas(v, ruta)); else r.push(ruta); }} return r; }};
const sets = {{}};
for (const c of ['es-MX', 'es-ES', 'en-US']) sets[c] = new Set(planas(hjson.parse(readFileSync(td + '/' + c + '.hjson', 'utf8'))));
const dif = (a, b) => [...sets[a]].filter(k => !sets[b].has(k));
console.log(JSON.stringify({{ mx: sets['es-MX'].size, es: sets['es-ES'].size, en: sets['en-US'].size,
  d1: dif('es-MX','es-ES'), d2: dif('es-MX','en-US'), d3: dif('en-US','es-MX') }}));
'''
    ruta_js = os.path.join(td, 'paridad.mjs')
    open(ruta_js, 'w').write(js)
    out = subprocess.run(['bun', ruta_js], capture_output=True, text=True, cwd='/tmp')
    assert out.returncode == 0, f'parser hjson fallo: {out.stderr}'
    import json
    r = json.loads(out.stdout.strip().splitlines()[-1])
    assert not r['d1'] and not r['d2'] and not r['d3'], f"paridad ROTA: {r}"
    print(f"hjson ×3 paridad REAL {r['mx']}=={r['es']}=={r['en']} claves  OK")

# 6) las armas Goozma de la CASA (.52.2) siguen dentro
for png in ('Content/Weapons/Goozma/RayoDeFusion.rawimg',
            'Content/Weapons/Goozma/LaserArcoiris.rawimg',
            'Content/Weapons/Goozma/DestructorDePrismas.rawimg',
            'Content/Weapons/Goozma/TruenoDeGelatina.rawimg',
            'Content/Weapons/Goozma/MasaCromatica.rawimg'):
    assert png in blobs, f'falta {png}'
print('Las 5 armas Goozma de la casa presentes  OK')

# 7) las del Escriba purgado siguen AUSENTES (regresión)
for MUERTO in (b'EscribaJefe', b'PlumaConsciente', b'PlumaConvocadora', b'BolsaDelEscriba'):
    assert MUERTO not in dll, f'REGRESIÓN: {MUERTO!r} volvió al DLL'
print('Purga del Escriba intacta (sin regresión)  OK')

md5 = hashlib.md5(data).hexdigest()
print(f'\nTODO LIMPIO — md5 {md5} · {fsize} B · {len(entries)} entradas')
