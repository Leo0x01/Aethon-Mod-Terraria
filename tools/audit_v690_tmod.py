#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.90 — AUDITORÍA DEL .tmod: la casa exige la entrega VERIFICADA.
  (0) versión 6.90 + entradas contadas
  (1) tabla + EOF exacto (blobs íntegros)
  (2) LOS PNGs NUEVOS (las 3 armas del taller + la bolsa)
  (3) LAS DOS MÚSICAS DEL FESTÍN (OggS)
  (4) LA DLL: firmas v6.90 VIVAS (el taller de efectos, la ley de la
      consagración, arma-o-material, la tecla B desde la bolsa, el
      relicario desde la bolsa, el hito de invocación, el libro de las
      criaturas, el hitbox del haz) y las CLAVES MUERTAS ausentes
  (5) LOS HJSON ×3 simétricos + los textos nuevos (consagrado, daño de
      invocación, uno de los dos)
  (6) LA ENTREGA: las armas del taller + la bolsa nueva
"""
import sys, zlib, os

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
assert ver == "6.90", f"¡VERSIÓN {ver} ≠ 6.90!"

todos, off, malas = blobs(d, tend, entries)
print(f"(1) blobs: {len(todos)}/{len(entries)} íntegros, malas={malas}, EOF={off} == {fsize}: {'OK' if off == fsize else '¡ROMPO!'}")
assert malas == 0 and off == fsize

# (2) LOS PNGs NUEVOS DEL TALLER
print("(2) LOS PNGs NUEVOS:")
for tex in ('Weapons/Investigaciones/SigiloDelEterno', 'Weapons/Investigaciones/LlamasDeSombra',
            'Weapons/Investigaciones/EclipseVivo', 'Items/Bolsas/BolsaTallerDeEfectos'):
    hit = [p for p in todos if tex in p and (p.endswith('.rawimg') or p.endswith('.png'))]
    assert hit, f"¡FALTA {tex}!"
    # el formato rawimg: [int32 1][int32 w][int32 h][RGBA] — 30×30
    b = todos[hit[0]]
    if hit[0].endswith('.rawimg'):
        import struct
        w, h = struct.unpack('<ii', b[4:12])
        assert (w, h) == (30, 30), f"{tex}: {w}x{h} ≠ 30x30"
    print(f"    {tex.split('/')[-1]}: OK ({len(b):,} B)")

# (3) las dos músicas del festín
ogs = [p for p in todos if p.endswith('.ogg') and len(todos[p]) > 1_500_000]
print(f"(3) músicas del festín (>1.5 MB): {len(ogs)} — OggS: {[todos[p][:4]==b'OggS' for p in ogs]}")
assert len(ogs) >= 2 and all(todos[p][:4] == b'OggS' for p in ogs)

# (4) LA DLL: firmas v6.90 vivas
dll = next(v for k, v in todos.items() if k.endswith('.dll'))
# v6.90 — LAS DOS ALINEACIONES del heap UTF-16 (una literal en offset
# impar se pierde con un solo decode: la lección de esta auditoría).
s = (dll.decode('utf-8', 'ignore')
     + dll.decode('utf-16-le', 'ignore')
     + dll[1:].decode('utf-16-le', 'ignore'))
print("(4) LA DLL:")
vivas = [
    # EL TALLER DE EFECTOS (las 3 armas + la bolsa)
    'TallerDeEfectos', 'SigiloDelEterno', 'LlamasDeSombra', 'EclipseVivo',
    'BolsaTallerDeEfectos', 'TexRuidoLlamas', 'ElTrazo', 'LaDetonacion',
    'RadioAhora',
    # LA LEY DE LA CONSAGRACIÓN (arma O materiales + solo lo consagrado)
    'AvisoConCalma', 'NoConsagrado', 'EsOfrendaConsagrible', 'EsArmaDeVerdad',
    'ConsagradaHechaArma', 'ConsagradaHechaMaterial', 'PrecioVivo',
    'ResultadoArma', 'ResultadoMaterial', 'LeyendaSello',
    # EL LIBRO DE LAS CRIATURAS + LOS COFRES DE TODA LA VIDA
    'LibroDeLasCriaturasNPC', 'LaRuletaDeLosCofres',
    # EL RELICARIO DESDE LA BOLSA + EL AURA ADITIVA
    'LlevaElRelicario',
    # LA TECLA B DESDE LA BOLSA
    'MascotasTeclaB', 'DetourQuickBuff',
    # EL HITO DE INVOCACIÓN (daño de invocación)
    'BonusHitoVivo', 'MultEscuadron',
    # EL HITBOX DEL HAZ (la silueta)
    'StripWidth',
    # EL VELO DE PERLIN CON EL PERFIL V (la cura del ancho)
    'TexPerlinConducto', 'perfil',
    # LA MIRADA DE SOLYN (la cura de los picos)
    'ColorBloom', 'AnchoLaser', '_rampa',
]
for f in vivas:
    ok = f in s
    print(f"    viva {f}: {'OK' if ok else '¡AUSENTE!'}")
    assert ok, f"¡firma v6.90 {f} ausente!"

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
for frase in ("(Consagrado)", "EL ALTAR DE LA CONSAGRACIÓN", "CONSAGRAR",
              "daño de invocación del escuadrón", "La Bolsa del Taller de Efectos",
              "El Sigilo del Eterno", "Las Llamas de Sombra", "El Eclipse Vivo",
              "Sin MI sello no la pruebo"):
    assert frase in txt, f"¡falta el texto «{frase}»!"
    print(f"    texto «{frase}»: OK")
# LAS CLAVES MUERTAS: ya nadie las lee
for vieja in ("TODO el ataque del escuadrón", "EL ALTAR DE LA CORRUPCIÓN",
              "\tCorromper:", "\tCorruptaHecha:", "\tLista:"):
    assert vieja not in txt, f"¡la clave rancia «{vieja}» aún vive!"
print("    claves rancias ausentes: OK")

# (6) LA ENTREGA COMPLETA (la cadena del kit + la bolsa nueva)
print("(6) LA ENTREGA:")
entrega = ['RelicarioDelAvispon', 'MiradaSolyn', 'GrilletesDeAndromeda',
           'AltarOfrendasItem', 'CodiceHabilidades', 'CompasRunico', 'FuriaSolarClicker',
           'BolsaMaestra', 'BolsaInvestigaciones', 'BolsaTallerDeEfectos',
           'BolsaDefinitivas', 'RayoPruebaC', 'TestingPlayer', 'LibroCorrupto',
           'SigiloDelEterno', 'LlamasDeSombra', 'EclipseVivo']
for f in entrega:
    ok = f in s
    print(f"    entrega {f}: {'OK' if ok else '¡AUSENTE!'}")
    assert ok, f"¡entrega {f} ausente!"

print("\n== v6.90: .tmod AUDITADO — TODO VERIFICADO ==")
