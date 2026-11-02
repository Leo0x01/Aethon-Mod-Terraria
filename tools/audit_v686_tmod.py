#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.86 — AUDITORÍA DEL .tmod: la casa exige la entrega VERIFICADA.
  (0) versión 6.86 + cuenta exacta de entradas (480 de la v6.85 + 6 assets)
  (1) tabla + EOF exacto (blobs íntegros, zlib OK / planas OK)
  (2) EL .fxc BYTE-IDÉNTICO a la v6.85 (cero riesgo de shader — la cura es C#)
  (3) LOS 6 ASSETS NUEVOS dentro (libro, altar ítem/tile, códice, cadena, nodo)
  (4) LA MÚSICA DEL FESTÍN intacta (OleadaHambre.ogg)
  (5) LA DLL: LAS 17 FIRMAS NUEVAS VIVAS (LibroCorrupto · AltarOfrendasTile ·
      AltarOfrendasUI · ArbolHabilidadesUI · SkillTreePlayer ·
      EscuadronPersistPlayer · CodiceHabilidades · IraContadoresTexto ·
      RegistrarOfrenda · AceptaRelleno · ProgresoOfrendas ·
      LibroCorruptoConsumido · CreditoPuntosEsencias · VecesMoonLord ·
      VecesAethon · LaRuletaDeLosCofres · PuertaLibro)
  (6) LOS HJSON ×3 DENTRO: simétricos (924=924=924) y con las claves nuevas
      (JefeAethon · HitoAgujeros2 · LibroEnInventario · EscuadronFortalecido
      · Altar.Titulo · Arbol.Titulo)
"""
import sys, zlib, struct, re

sys.path.insert(0, '/home/z/.tmp-ml')
from parse_tmod import parse

TMOD = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
VIEJO = "/home/sync/AethonMod-v6.85.tmod"


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


# (0) versión + cuenta
tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f"tML {tmlver} | {name} v{ver} | {len(entries)} entradas | tabla fin {tend} | archivo {fsize}")
assert str(ver) == "6.86", f"versión {ver} != 6.86"
ESPERADAS = 486   # 480 (v6.85) + 6 assets nuevos
assert len(entries) == ESPERADAS, f"esperaba {ESPERADAS} entradas, hay {len(entries)}"

# (1) blobs íntegros + EOF
d = open(TMOD, 'rb').read()
files, off, malas = blobs(d, tend, entries)
assert malas == 0, f"{malas} entradas corruptas"
assert off == fsize, f"EOF: el último blob termina en {off} != {fsize}"
print(f"(1) blobs OK: {len(files)} entradas íntegras · EOF exacto ({off}=={fsize})")

# (2) .fxc byte-idénticos a v6.85 (TODOS los shaders — cero riesgo de shader)
fx_nuevo = sorted(p for p in files if p.endswith('.fxc'))
assert len(fx_nuevo) >= 5, fx_nuevo
d_viejo = open(VIEJO, 'rb').read()
_, _, _, ent_v, tend_v, _ = parse(VIEJO)
files_v, _, _ = blobs(d_viejo, tend_v, ent_v)
fx_viejo = sorted(p for p in files_v if p.endswith('.fxc'))
assert fx_nuevo == fx_viejo, f"sets .fxc distintos: {set(fx_nuevo) ^ set(fx_viejo)}"
for p in fx_nuevo:
    assert files[p] == files_v[p], f"{p} DIFIERE de v6.85"
print(f"(2) {len(fx_nuevo)} .fxc BYTE-IDÉNTICOS a v6.85 ({fx_nuevo[0]}, …)")

# (3) los 6 assets nuevos dentro (como .rawimg — el formato del pipeline)
ASSETS = [
    "Content/Items/Altar/LibroCorrupto",
    "Content/Items/Altar/AltarOfrendasItem",
    "Content/Tiles/AltarOfrendasTile",
    "Content/Items/Investigaciones/CodiceHabilidades",
    "Content/UI/ArbolHabilidades_Cadena",
    "Content/UI/ArbolHabilidades_Nodo",
]
for a in ASSETS:
    hits = [p for p in files if p.startswith(a)]
    assert hits, f"asset ausente: {a}"
    # rawimg: [int32 1][int32 w][int32 h] + RGBA
    for h in hits:
        blob = files[h]
        if h.endswith('.rawimg'):
            magic, w, hh = struct.unpack_from('<iii', blob, 0)
            assert magic == 1, (h, magic)
            assert len(blob) == 12 + w * hh * 4, (h, len(blob), w, hh)
            print(f"    {h} rawimg {w}x{hh} EXACTO")
        elif h.endswith('.png'):
            assert blob[:8] == b'\x89PNG\r\n\x1a\n', h
            print(f"    {h} png ({len(blob)} B)")
print("(3) LOS 6 ASSETS DENTRO — dimensiones exactas")

# (4) la música del festín
mus = [p for p in files if 'OleadaHambre' in p]
assert mus and files[mus[0]][:4] == b'OggS', "música del festín rota"
print(f"(4) MÚSICA INTACTA: {mus[0]} ({len(files[mus[0]])} B, OggS)")

# (5) la DLL: las 17 firmas nuevas VIVAS
dlls = [p for p in files if p.endswith('.dll')]
assert dlls, "sin DLL en el .tmod"
dll = files[dlls[0]]
FIRMAS = [
    "LibroCorrupto", "AltarOfrendasTile", "AltarOfrendasUI",
    "ArbolHabilidadesUI", "SkillTreePlayer", "EscuadronPersistPlayer",
    "CodiceHabilidades", "IraContadoresTexto", "RegistrarOfrenda",
    "AceptaRelleno", "ProgresoOfrendas", "LibroCorruptoConsumido",
    "CreditoPuntosEsencias", "VecesMoonLord", "VecesAethon",
    "LaRuletaDeLosCofres", "PuertaLibro",
]
def viva(dll, f):
    """Una firma vive si aparece como identificador (UTF-8, heap #Strings)
    o como literal de cadena (UTF-16LE, heap #US) — la lectora de la casa."""
    return f.encode() in dll or f.encode('utf-16-le') in dll


vivas = [f for f in FIRMAS if viva(dll, f)]
muertas = [f for f in FIRMAS if not viva(dll, f)]
assert not muertas, f"firmas muertas: {muertas}"
print(f"(5) DLL: {len(vivas)}/{len(FIRMAS)} firmas nuevas VIVAS ({dlls[0]}, {len(dll)} B)")

# (6) hjson ×3 dentro del paquete: simetría + claves nuevas
def claves_de(texto):
    ks = set()
    for l in texto.splitlines():
        m = re.match(r"\s*([A-Za-z][A-Za-z0-9_.]*):", l)
        if m and not l.strip().startswith('//'):
            k = m.group(1)
            if '.' in k or k[0].isupper():
                ks.add(k)
    return ks

hjs = {}
for lang in ('es-MX', 'es-ES', 'en-US'):
    cand = [p for p in files if p == f'Localization/{lang}_Mods.AethonMod.hjson']
    assert len(cand) == 1, (lang, cand)
    hjs[lang] = claves_de(files[cand[0]].decode('utf-8'))
a, b, c = hjs['es-MX'], hjs['en-US'], hjs['es-ES']
assert a == b == c, f"asimetría: solo es-MX={sorted(a - b)[:4]} · solo en-US={sorted(b - a)[:4]}"
CLAVES_NUEVAS = ['JefeAethon', 'HitoAgujeros2', 'CaminoAgujeros2', 'LibroEnInventario',
                 'PuertaLibro', 'ReqLibroDevorado', 'CreditoPuntos', 'EscuadronFortalecido',
                 'AltarOfrendasTile.MapEntry', 'Nodo9', 'Desc9']
faltan = [k for k in CLAVES_NUEVAS if k not in a]
assert not faltan, f"claves ausentes: {faltan}"
# Las SECCIONES nuevas (Altar/Arbol) existen como texto en los 3 idiomas:
for lang in ('es-MX', 'es-ES'):
    cand = [p for p in files if p == f'Localization/{lang}_Mods.AethonMod.hjson']
    txt = files[cand[0]].decode('utf-8')
    assert 'EL ALTAR DE OFRENDAS' in txt and 'EL ÁRBOL DE HABILIDADES' in txt, lang
cand = [p for p in files if p == 'Localization/en-US_Mods.AethonMod.hjson']
txt_en = files[cand[0]].decode('utf-8')
assert 'THE OFFERING ALTAR' in txt_en and 'THE SKILL TREE' in txt_en, 'en-US'
print(f"(6) HJSON ×3 SIMÉTRICOS: {len(a)}={len(b)}={len(c)} claves · las {len(CLAVES_NUEVAS)} nuevas + las secciones Altar/Arbol presentes")

print("\nAUDIT v6.86: TODO OK — el .tmod está ÍNTEGRO")
