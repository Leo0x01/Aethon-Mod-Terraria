#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.91 — AUDITORÍA DEL .tmod: la casa exige la entrega VERIFICADA.
  (0) versión 6.91 + entradas contadas
  (1) tabla + EOF exacto (blobs íntegros)
  (2) LOS PNGs NUEVOS (la mascota con su atlas + la bolsa de pociones +
      las 3 texturas del original de la Mirada)
  (3) LAS DOS MÚSICAS DEL FESTÍN (OggS)
  (4) LA DLL: firmas v6.91 VIVAS (el motor pixelfx, la mascota, la
      bolsa de pociones, el libro con su contador individual, el altar
      con sus secciones medidas, el relicario con su tecla, la mirada
      portada, la tecla B solo equipada) y las CLAVES MUERTAS ausentes
  (5) LOS HJSON ×3 simétricos + los textos nuevos (GANA · CÓMO
      CONSEGUIR ESENCIAS · Consagrado · el Vástago)
  (6) LA LICENCIA viaja dentro (AethonMod/LICENSE)
"""
import sys, zlib, os, struct

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
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize:,} B")
assert ver == "6.91", f"¡VERSIÓN {ver} ≠ 6.91!"

todos, off, malas = blobs(d, tend, entries)
print(f"(1) blobs: {len(todos)}/{len(entries)} íntegros, malas={malas}, EOF={off} == {fsize}: {'OK' if off == fsize else '¡ROMPO!'}")
assert malas == 0 and off == fsize

# (2) LOS PNGs NUEVOS DE LA v6.91
print("(2) LOS PNGs NUEVOS:")
def dims(b, p):
    if p.endswith('.rawimg'):
        w, h = struct.unpack('<ii', b[4:12])
        return w, h
    if b[:8] == b'\x89PNG\r\n\x1a\n':
        w, h = struct.unpack('>II', b[16:24])
        return w, h
    return None

esperados = {
    'Projectiles/Cosmetic/VastagoDelEternoPet': (40, 384),   # EL ATLAS sprite-gen (8 frames)
    'Items/Llamados/VastagoDelEternoItem': (30, 30),
    'Buffs/VastagoDelEternoBuff': (30, 30),
    'Items/Bolsas/BolsaDePociones': (30, 30),
    'Effects/Procedural/Investigacion/DendriticNoiseZoomedOut': (512, 512),  # del original (Luminance/WoTG)
    'Effects/Procedural/Investigacion/ShineFlare': (512, 512),
    'Effects/Procedural/Investigacion/BloomCircleSmall': (200, 200),
}
for tex, (w0, h0) in esperados.items():
    hit = [p for p in todos if tex in p and (p.endswith('.rawimg') or p.endswith('.png'))]
    assert hit, f"¡FALTA {tex}!"
    w, h = dims(todos[hit[0]], hit[0])
    assert (w, h) == (w0, h0), f"{tex}: {w}x{h} ≠ {w0}x{h0}"
    print(f"    {tex.split('/')[-1]}: OK {w0}x{h0}")

# (3) las dos músicas del festín
ogs = [p for p in todos if p.endswith('.ogg') and len(todos[p]) > 1_500_000]
print(f"(3) músicas del festín (>1.5 MB): {len(ogs)} — OggS: {[todos[p][:4]==b'OggS' for p in ogs]}")
assert len(ogs) >= 2 and all(todos[p][:4] == b'OggS' for p in ogs)

# (4) LA DLL: firmas v6.91 vivas
dll = next(v for k, v in todos.items() if k.endswith('.dll'))
# LAS DOS ALINEACIONES del heap UTF-16 (la lección de la casa).
s = (dll.decode('utf-8', 'ignore')
     + dll.decode('utf-16-le', 'ignore')
     + dll[1:].decode('utf-16-le', 'ignore'))
print("(4) LA DLL:")
vivas = [
    # EL MOTOR PIXELFX PORTADO (1:1, verificado píxel a píxel)
    'PixelFxCore', 'PxFxInstance', 'PxHexSigil', 'PxShadowFlames', 'PxEclipse',
    'HexSigil', 'ShadowFlames', 'Eclipse', 'Composite', 'PxPainter',
    # LA MASCOTA NUEVA (el pipeline sprite-gen)
    'VastagoDelEternoItem', 'VastagoDelEternoBuff', 'VastagoDelEternoPet',
    # LA BOLSA DE LAS POCIONES
    'BolsaDePociones',
    # EL LIBRO CONSUAGRADO: el contador individual + la computa nueva
    'LibroUsado', 'ElLibroYaNoLeToca', 'ElAlmaDelPortador',
    'LaRuletaDeLosCofres', 'LibroDeLasCriaturasNPC',
    # EL ALTAR: las secciones medidas + la cura de ListaArma
    'LineaMedida', 'TituloSeccion', 'GanaTitulo', 'SelloCuesta', 'SelloFalta',
    'ComoTitulo', 'ComoCriaturas', 'ComoOleadas', 'EsOfrendaConsagrible',
    # EL RELICARIO: la tecla que nunca existió
    'RelicarioTeclaSistema', 'TeclasAsignadas', 'OnWorldLoad',
    # LA MIRADA DE SOLYN: el port de los shaders del original
    'ColorHaz', 'ColorBloomV', 'TrianguloColoresEnLote', 'VolcarTriangulos',
    # LA TECLA B SOLO EQUIPADAS
    'MascotasTeclaB', 'DetourQuickBuff',
    # LAS ESENCIAS (la mesa del altar dice la verdad)
    'EsenciaCorruptaDrops',
]
for f in vivas:
    ok = f in s
    print(f"    viva {f}: {'OK' if ok else '¡AUSENTE!'}")
    assert ok, f"¡firma v6.91 {f} ausente!"

# LAS CLAVES MUERTAS: ya nadie las lee (la cuenta se fue del chat, la
# ruta de la bolsa murió con la tecla B, el precio viejo del altar se
# fue con las secciones nuevas)
muertas = ['PrecioVivo', 'ResultadoArma', 'ResultadoMaterial', 'LlevaElRelicario']
for f in muertas:
    ok = f not in s
    print(f"    muerta {f}: {'OK (ausente)' if ok else '¡AÚN VIVE!'}")
    assert ok, f"¡la clave rancia {f} sigue en la DLL!"

# (5) HJSON ×3 simétricos + los textos nuevos
hjsons = [p for p in todos if p.endswith('.hjson')]
print(f"(5) hjson dentro: {len(hjsons)} archivos")
assert len(hjsons) == 6, "3 idiomas × 2 (normal + Escriba)"
def cuerpo(p):
    t = todos[p].decode('utf-8')
    return t[t.find('{'):]
es = [p for p in hjsons if 'es-ES' in p and 'Escriba' not in p][0]
mx = [p for p in hjsons if 'es-MX' in p and 'Escriba' not in p][0]
en = [p for p in hjsons if 'en-US' in p and 'Escriba' not in p][0]
sim = cuerpo(es) == cuerpo(mx) == cuerpo(en)
print(f"    es-ES == es-MX == en-US: {'SIMÉTRICOS ✓' if sim else '¡DIFIEREN!'}")
assert sim
txt = todos[es].decode('utf-8')
for frase in ("GANA", "CÓMO CONSEGUIR ESENCIAS",
              "OLEADAS DE HAMBRE suben la probabilidad",
              "El Vástago del Eterno", "Libro Consagrado",
              "La Bolsa de las Pociones",
              "el altar dice cuánto falta",
              "Arma → +5% a TODAS sus estadísticas"):
    assert frase in txt, f"¡falta el texto «{frase}»!"
    print(f"    texto «{frase}»: OK")
# LAS CLAVES MUERTAS del hjson
for vieja in ("te faltan {1}", "\tPrecioVivo:", "\tResultadoArma:", "\tResultadoMaterial:",
              "LIBRO CORRUPTO", "Libro Corrupto: DEVORADO"):
    assert vieja not in txt, f"¡la clave rancia «{vieja}» aún vive!"
print("    claves rancias: AUSENTES ✓")

# (6) LA LICENCIA viaja dentro del .tmod
lic = [p for p in todos if p.endswith('LICENSE')]
assert lic, "¡AethonMod/LICENSE no viaja en el .tmod!"
cuerpo_lic = todos[lic[0]].decode('utf-8')
assert 'CC BY-NC-SA' in cuerpo_lic and 'Leo0x01' in cuerpo_lic
print(f"(6) LICENSE dentro del .tmod: OK ({lic[0]}, {len(cuerpo_lic)} chars)")

print("\nAUDITORÍA v6.91: TODO VERIFICADO ✓")
