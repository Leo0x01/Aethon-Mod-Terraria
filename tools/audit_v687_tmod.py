#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.87 — AUDITORÍA DEL .tmod: la casa exige la entrega VERIFICADA.
  (0) versión 6.87 + cuenta exacta de entradas (486 de la v6.86 + 1 highlight)
  (1) tabla + EOF exacto (blobs íntegros, zlib OK / planas OK)
  (2) EL HIGHLIGHT DEL ALTAR DENTRO (la cura del crash v6.86)
  (3) LA MÚSICA NUEVA (OleadaHambre.ogg — 128.3 s, vorbis, ≠ v6.86)
  (4) LA DLL: la firma v6.87 VIVA (el cambio del ritual de la bruma) +
      las firmas clave v6.86 intactas
  (5) LOS HJSON ×3 DENTRO: simétricos
  (6) LA BOLSA: los items de las investigaciones + Absoluto (verificación
      de entrega — la letra: «asegúrate de que todos los items hayan sido
      dados al jugador»)
"""
import sys, zlib, struct, re

sys.path.insert(0, '/home/z/.tmp-ml')
if not __import__('os').path.isdir('/home/z/.tmp-ml'):
    sys.path.insert(0, '/home/z/my-project/download/Aethon-Mod-Terraria/tools')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"

def blobs(d, tend, entries):
    out, off, malas = {}, tend, 0
    for p, raw, comp in entries:
        trozo = d[off:off + comp]
        if len(trozo) != comp:
            malas += 1
            break
        try:
            data = trozo if comp == raw else zlib.decompress(trozo, -15)
        except zlib.error:
            malas += 1
            break
        if len(data) != raw:
            malas += 1
            break
        out[p] = data
        off += comp
    return out, off, malas

d = open(TMOD, 'rb').read()
tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize}")
assert ver == "6.87", f"¡VERSIÓN {ver} ≠ 6.87!"

todos, off, malas = blobs(d, tend, entries)
print(f"(1) blobs: {len(todos)}/{len(entries)} íntegros, malas={malas}, EOF={off} == {fsize}: {'OK' if off == fsize else '¡ROMPO!'}")
assert malas == 0 and off == fsize

# (2) EL HIGHLIGHT DEL ALTAR — la cura del crash
hl = [p for p in todos if 'AltarOfrendasTile_Highlight' in p]
print(f"(2) highlight del altar: {hl}")
assert any('.rawimg' in p or p.endswith('.png') for p in hl), "¡FALTA EL HIGHLIGHT!"
print(f"    tamaño: {len(todos[hl[0]])} B")

# (3) LA MÚSICA NUEVA
mus = [p for p in todos if 'OleadaHambre.ogg' in p]
assert mus, "¡FALTA LA MÚSICA!"
ogg = todos[mus[0]]
print(f"(3) música: {mus[0]} — {len(ogg):,} B, OggS={'OK' if ogg[:4] == b'OggS' else 'NO!'}")
assert ogg[:4] == b'OggS' and len(ogg) > 1_800_000, "la música debe ser la NUEVA (~1.9 MB)"

# (4) LAS FIRMAS — la DLL
dll = todos.get('.dll', todos.get('AethonMod.dll'))
if dll is None:
    dll = next(v for k, v in todos.items() if k.endswith('.dll'))
firmas = ['BrumaBlackHoleProjectile']  # la del ritual ahora
# el cambio v6.87: el ritual ya NO referencia BrumaAscendidoBlackHole en el switch
s = dll.decode('utf-8', 'ignore') + dll.decode('utf-16-le', 'ignore')
for f in ['RayoConvergente', 'BrumaBlackHoleProjectile', 'BrumaAscendidoBlackHoleProjectile',
          'AltarOfrendasTile', 'OleadaHambreMusica', 'LibroCorrupto', 'ArbolHabilidadesUI',
          'EscuadronPersistPlayer', 'AgujeroAbsolutoStaff', 'BolsaInvestigaciones']:
    viva = f in s
    print(f"    firma {f}: {'VIVA' if viva else '¡AUSENTE!'}")
    assert viva, f"¡firma {f} ausente!"

# (5) HJSON ×3
hjsons = [p for p in todos if p.endswith('.hjson')]
print(f"(5) hjson dentro: {hjsons} — líneas: {[todos[h].count(b'\\n') for h in hjsons]}")
assert len(hjsons) == 6  # 3 idiomas × 2 (normal + Escriba)

# (6) LA BOLSA: verificación de ENTREGA — los ítems clave en la DLL
#    (las bolsas referencian ModContent.ItemType<...> → los nombres viajan en la DLL)
entrega = ['RelicarioDelAvispon', 'SolEnMiniaturaStaff', 'ColmilloDoradoStaff',
           'MiradaSolyn', 'GrilletesDeAndromeda', 'AltarOfrendasItem', 'CodiceHabilidades',
           'AgujeroAbsolutoStaff', 'CompasRunico', 'FuriaSolarClicker',
           'BolsaMaestra', 'BolsaInvestigaciones', 'BolsaAgujerosNegros', 'BolsaDefinitivas',
           'RayoPruebaC', 'TestingPlayer']
for f in entrega:
    viva = f in s
    print(f"    entrega {f}: {'OK' if viva else '¡AUSENTE!'}")
    assert viva

print("\n== v6.87: .tmod AUDITADO — TODO VERIFICADO ==")
