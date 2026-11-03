#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.89 — AUDITORÍA DEL .tmod: la casa exige la entrega VERIFICADA.
  (0) versión 6.89 + entradas contadas (sin número mágico: la tabla manda)
  (1) tabla + EOF exacto (blobs íntegros, zlib OK / planas OK)
  (2) EL HIGHLIGHT DEL ALTAR + LAS TEXTURAS NUEVAS (BloomFlare/GlowAlpha
      del Relicario + el Códice restaurado)
  (3) LAS DOS MÚSICAS DEL FESTÍN (OggS, >1.5 MB cada una)
  (4) LA DLL: las firmas v6.89 VIVAS (las tablas del despertar, el precio
      de la corrupción, la rareza, la entrega del libro, la tecla B, el
      velo de Perlin, el aura del relicario, el rayo de Solyn) y las
      firmas MUERTAS AUSENTES (las armas nuevas borradas + el Libro Azul
      y su nivel)
  (5) LOS HJSON ×3 DENTRO: simétricos (es-ES == es-MX == en-US)
  (6) LA BOLSA: los ítems de las investigaciones VIVOS (códice dentro,
      armas muertas fuera)
"""
import sys, zlib

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
assert ver == "6.89", f"¡VERSIÓN {ver} ≠ 6.89!"

todos, off, malas = blobs(d, tend, entries)
print(f"(1) blobs: {len(todos)}/{len(entries)} íntegros, malas={malas}, EOF={off} == {fsize}: {'OK' if off == fsize else '¡ROMPO!'}")
assert malas == 0 and off == fsize

# (2) highlight + texturas nuevas
hl = [p for p in todos if 'AltarOfrendasTile_Highlight' in p]
assert any('.rawimg' in p or p.endswith('.png') for p in hl), "¡FALTA EL HIGHLIGHT!"
print(f"(2) highlight del altar: OK ({hl[0]})")
for tex in ('Relicario_BloomFlare', 'Relicario_GlowAlpha', 'CodiceHabilidades'):
    hit = [p for p in todos if tex in p and (p.endswith('.rawimg') or p.endswith('.png'))]
    assert hit, f"¡FALTA {tex}!"
    print(f"    textura {tex}: OK ({len(todos[hit[0]]):,} B)")

# (3) las dos músicas del festín
ogs = [p for p in todos if p.endswith('.ogg') and len(todos[p]) > 1_500_000]
print(f"(3) músicas del festín (>1.5 MB): {len(ogs)} — {[(p.split('/')[-1], f'{len(todos[p]):,}B', todos[p][:4]==b'OggS') for p in ogs]}")
assert len(ogs) >= 2 and all(todos[p][:4] == b'OggS' for p in ogs), "¡las DOS músicas, OggS!"

# (4) LA DLL: firmas v6.89 vivas + muertas ausentes
dll = next(v for k, v in todos.items() if k.endswith('.dll'))
s = dll.decode('utf-8', 'ignore') + dll.decode('utf-16-le', 'ignore')
print("(4) LA DLL:")
vivas = ['MaterialesDelNivel', 'CantidadesMateriales', 'CosteCorrupcion',
         'VestirLaRareza', 'RarityCorrupta', 'RevisarEntregaDelLibro',
         'MascotasTeclaB', 'DetourQuickBuff', 'TexPerlinConducto', 'ColorPerlin',
         'RelicarioAuraSistema', 'AnchoLaser', 'ColorLaser', 'CodiceHabilidades']
for f in vivas:
    ok = f in s
    print(f"    viva {f}: {'OK' if ok else '¡AUSENTE!'}")
    assert ok, f"¡firma v6.89 {f} ausente!"
muertas = ['AgujeroAbsolutoBlackHoleProjectile', 'RequiemDelPulsarStaff',
           'RequiemPulsarProjectile', 'SolEnMiniaturaStaff', 'SolEnMiniaturaBuff',
           'ColmilloDoradoStaff', 'AureumSkyStrike', 'LibroAzulDescarga',
           'NivelLibroAzul', 'RegistrarBajaLibroAzul', 'LlevaLibroAzul']
for f in muertas:
    # ojo: 'LibroAzul' a secas vive en COMENTOS de la DLL? no: los comentarios
    # no viajan. Pero 'LibroAzul' es prefijo de 'LibroAzulDescarga' — se
    # comprueba con el nombre largo.
    mal = f in s
    print(f"    muerta {f}: {'¡AÚN VIVE!' if mal else 'ausente ✓'}")
    assert not mal, f"¡la firma muerta {f} sigue viva!"

# (5) HJSON ×3 simétricos
hjsons = [p for p in todos if p.endswith('.hjson')]
print(f"(5) hjson dentro: {len(hjsons)} archivos")
assert len(hjsons) == 6, "3 idiomas × 2 (normal + Escriba)"
def cuerpo(p):
    t = todos[p].decode('utf-8')
    i = t.find('{')
    return t[i:]
es = [p for p in hjsons if 'es-ES' in p and 'Escriba' not in p][0]
mx = [p for p in hjsons if 'es-MX' in p and 'Escriba' not in p][0]
en = [p for p in hjsons if 'en-US' in p and 'Escriba' not in p][0]
sim = cuerpo(es) == cuerpo(mx) == cuerpo(en)
print(f"    es-ES == es-MX == en-US: {'SIMÉTRICOS ✓' if sim else '¡DIFIEREN!'}")
assert sim
# el Cielo dice CIEN peldaños y la esencia se llama del Eterno
txt = todos[es].decode('utf-8')
assert "los 100 peldaños" in txt, "¡el Cielo debe decir los 100 peldaños!"
assert "Esencia del Eterno" in txt, "¡la esencia debe llamarse del Eterno!"
assert "los 50 peldaños" not in txt, "¡el 50 peldaños rancio aún vive!"
print("    «los 100 peldaños» + «Esencia del Eterno» + sin «50 peldaños»: OK")

# (6) LA BOLSA: la entrega v6.89 (el códice DENTRO, las armas muertas fuera)
print("(6) LA BOLSA (entrega v6.89):")
entrega = ['RelicarioDelAvispon', 'MiradaSolyn', 'GrilletesDeAndromeda',
           'AltarOfrendasItem', 'CodiceHabilidades', 'CompasRunico', 'FuriaSolarClicker',
           'BolsaMaestra', 'BolsaInvestigaciones', 'BolsaDefinitivas',
           'RayoPruebaC', 'TestingPlayer', 'LibroCorrupto']
for f in entrega:
    ok = f in s
    print(f"    entrega {f}: {'OK' if ok else '¡AUSENTE!'}")
    assert ok, f"¡entrega {f} ausente!"

print("\n== v6.89: .tmod AUDITADO — TODO VERIFICADO ==")
