"""audit_v6524_tmod.py — auditoría del .tmod v6.52.4.

La cura del inventario muerto: HotbarLibreSistema (el IL hook que volvió
ItemSlot.LeftClick/RightClick en retorno incondicional) DEBÍA desaparecer
por completo del DLL. Este audit lo prueba por bytes + verifica el
paquete completo (entradas, EOF, hjson ×3, controles positivos).
"""
import sys, hashlib
sys.path.insert(0, '/home/z/my-project/download/Aethon-Mod-Terraria/tools')
from parse_tmod import parse
import zlib

PATH = sys.argv[1] if len(sys.argv) > 1 else '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'

tmlver, name, ver, entries, tend, fsize = parse(PATH)
data = open(PATH, 'rb').read()
print(f'== {name} v{ver} == tML {tmlver} | {len(entries)} entradas | tabla {tend} | archivo {fsize}')
assert name == 'AethonMod' and ver == '6.52.4', (name, ver)

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

# 3) EL CORAZÓN DEL AUDIT: el DLL sin el hook
dll = blobs['AethonMod.dll']
for MALO in (b'HotbarLibreSistema', b'QuitarRebote', b'DumpForenseSistema',
             b'IL_ItemSlot', b'MonoMod.Cil'):
    assert MALO not in dll, f'PRESENTE Y PROHIBIDO: {MALO!r}'
print('DLL LIMPIO: HotbarLibreSistema / QuitarRebote / DumpForenseSistema / IL_ItemSlot / MonoMod.Cil — AUSENTES')

for BUENO in (b'AlasAuroraLib', b'BarraConjuroUI', b'ArbolInscripcionesSistema',
              b'SombraDeLaPagina', b'GrimorioEternal', b'EscribaJefe'):
    assert BUENO in dll, f'FALTA CONTROL POSITIVO: {BUENO!r}'
print('Controles positivos OK (alas/haz-UI/sombras/grimorio/Escriba)')

# 4) hjson ×3 con paridad de claves
def claves(blob):
    ks = []
    for ln in blob.decode('utf-8').splitlines():
        s = ln.strip()
        if s.startswith('#') or not s:
            continue
        if '=' in s and (s.startswith(' ') or s.startswith('\t') or ':' not in s.split('=')[0]):
            pass
        if ':' in s and not s.startswith('-') and '=' not in s.split(':')[0]:
            ks.append(s.split(':')[0].strip())
    return ks

hj = {p: claves(blobs[p]) for p in blobs if p.endswith('.hjson') and 'Mods.AethonMod' in p}
assert len(hj) >= 3, f'hjson < 3: {list(hj)}'
mx, es, en = hj['Localization/es-MX_Mods.AethonMod.hjson'], hj['Localization/es-ES_Mods.AethonMod.hjson'], hj['Localization/en-US_Mods.AethonMod.hjson']
assert mx == es == en, f'paridad rota: mx={len(mx)} es={len(es)} en={len(en)}'
print(f'hjson ×3 paridad {len(mx)}=={len(es)}=={len(en)} claves  OK')

# 5) texturas clave
for png in ('Content/Weapons/Sombras/SombraDeLaPagina3.png', 'Content/Weapons/Sombras/SombraDeLaPagina4.png'):
    assert png in blobs, f'falta {png}'
print('Sombras III/IV png presentes  OK')

md5 = hashlib.md5(data).hexdigest()
print(f'\nTODO LIMPIO — md5 {md5} · {fsize} B')
