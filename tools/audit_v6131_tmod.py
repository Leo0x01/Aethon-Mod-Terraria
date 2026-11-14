#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6131_tmod.py — LA AUDITORÍA DEL .tmod v6.131 — LAS ALAS
REPARADAS · LA CORONA DE CUCHILLAS · EL JUGADOR COMPLETO DEVORADO.

LA LETRA (sesión 12, tres pedidos):
  (a) «las alas estan mal, solo se ven las alas de la izquierda y
      ademas parece que en algun punto la textura de las alas se vuelve
      cuadrada o rectangular, eso esta mal, intenta arreglar las alas»
  (b) «las cuchillas son muy pequeñas, que sean un poco mas grande y
      ademas deben estar ubicadas como una corona encima de los ojos»
  (c) «sobre la animacion donde devoran al jugador, lo que sucede es
      que el jugador se mueve hacia arriba, pero todos sus items
      cosmeticos quedan en la misma posicion, es como que dentro del
      codigo estas tomando solo al jugador, pero no a lo que lleva
      equipado, debes tomar al jugador completo para la animacion de
      devorar e ir al mundo interior»

LO QUE ESTA AUDITORÍA VERIFICA (la obra de la v6.131):
  (0) IDENTIDAD: AethonMod v6.131 · 527 entradas (las mismas de la
      v6.130 — la v6.131 es CIRUGÍA de código, sin assets nuevos).
  (1) EOF EXACTO: dataSize + tabla + sum(blobs) == fsize.
  (2) TODOS LOS BLOBS INFLAN (527/527).
  (2b) EMPAQUETADO == FUENTE byte a byte (los hjson ×3).
  (3) LA CURA DE LAS ALAS — LAS TRES FIRMAS en AlasAuroraLib.cs y en
      la DLL: EL SNAPSHOT de EstirarVelaLisa (la copia de la
      armadura: `ejesViejos`), EL N DE LOS EMISORES (`nEmisores`) y
      EL ESPEJO COMPLETO (`nEspejo`).
  (4) LA CORONA DE CUCHILLAS — las firmas en DagaEternaMinion.cs y la
      DLL: `RadioTiara` (la ley del despeje), `ESCALA_TIARA` (×1,4) y
      la escala viva `Projectile.scale` en SetDefaults.
  (5) EL PEGUE DEL FESTÍN — las firmas en DevoracionEterna.cs y la
      DLL: `DeltaDelFestin` + `PegarAlFestin` + `ArrastrandoA`, y los
      TRES consumidores (OjoEternoMinion, DagaEternaMinion,
      BrilloSolarHalo con el delta + AnilloRunicoDorsalHalo con la
      fullRotation).
  (6) hjson ×3: paridad de claves en los TRES idiomas.
"""
import importlib.util
import os
import struct as _st
import zlib

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
RAIZ = os.path.join(REPO, 'AethonMod')
TMOD = os.path.expanduser('~/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod')

_falla = []


def check(nombre, ok, det=''):
    print(('  OK ' if ok else '  FALLA ') + nombre + (f' [{det}]' if det and not ok else (f' ({det})' if det else '')))
    if not ok:
        _falla.append(nombre)


mp_spec = importlib.util.spec_from_file_location('parse_tmod', os.path.join(REPO, 'tools', 'parse_tmod.py'))
mp = importlib.util.module_from_spec(mp_spec)
mp_spec.loader.exec_module(mp)


def blobs_de(data, start, entries):
    out, off, malas = {}, start, 0
    for path, raw, comp in entries:
        trozo = data[off:off + comp]
        if len(trozo) != comp:
            malas += 1
            break
        try:
            blob = trozo if comp == raw else zlib.decompress(trozo, -15)
        except zlib.error:
            malas += 1
            break
        if len(blob) != raw:
            malas += 1
            break
        out[path] = blob
        off += comp
    return out, off, malas


d = open(TMOD, 'rb').read()

# === (0) IDENTIDAD ===
tmlver, name, ver, entries, tend, fsize = mp.parse(TMOD)
print(f'(0) {name} v{ver} · tML {tmlver} · {len(entries)} entradas · {fsize:,} B · archivo {len(d):,} B')
check('la versión es 6.131', ver == '6.131', ver)
check('las entradas son 527 (cirugía de código — sin assets nuevos)',
      len(entries) == 527, str(len(entries)))

# === (1) EOF + (2) BLOBS ===
bl, off, malas = blobs_de(d, tend, entries)
check('(1) EOF EXACTO', off == len(d) == fsize, f'{off} vs {len(d)}/{fsize}')
check(f'(2) TODOS LOS BLOBS INFLAN ({len(bl)}/{len(entries)}, {malas} malas)',
      len(bl) == len(entries) and malas == 0, f'{len(bl)}/{len(entries)}')

# === (2b) EMPAQUETADO == FUENTE (los 3 hjson) ===
for lang in ('es-ES', 'es-MX', 'en-US'):
    p = f'Localization/{lang}_Mods.AethonMod.hjson'
    fuente = open(os.path.join(RAIZ, p), 'rb').read()
    check(f'(2b) hjson empaquetado == fuente: {lang} ({len(fuente):,} B)',
          p in bl and bl[p] == fuente,
          'ausente' if p not in bl else f'{len(bl[p]):,} vs {len(fuente):,}')

# === (3) LA CURA DE LAS ALAS — las TRES firmas ===
alas_cs = open(os.path.join(RAIZ, 'Content/VFX/AlasAuroraLib.cs'), encoding='utf-8').read()
check('(3) EL SNAPSHOT en la fuente: ejesViejos (la copia de la armadura)',
      'ejesViejos' in alas_cs and 'Clone()' in alas_cs)
check('(3) EL N DE LOS EMISORES en la fuente: nEmisores',
      'int nEmisores = estilo.Lisas ? _geoN : num2 * 2;' in alas_cs)
check('(3) EL ESPEJO COMPLETO en la fuente: nEspejo (recorre _geoN)',
      'int nEspejo = estilo.Lisas ? _geoN : num3;' in alas_cs)
check('(3) los emisores usan nEmisores (5 llamadas)',
      alas_cs.count('in estilo, nEmisores);') == 5, str(alas_cs.count('in estilo, nEmisores);')))

dll = bl.get('AethonMod.dll', b'')
check('(3) la DLL viaja DENTRO del .tmod', len(dll) > 100000, f'{len(dll):,} B')


def vive_dll(firma):
    return firma.encode('utf-8') in dll or firma.encode('utf-16-le') in dll


# los nombres locales del snapshot viajan ofuscados en la DLL — las
# firmas ESTABLES de la cura son los métodos y campos que la sostienen
for firma in ('EstirarVelaLisa', 'PuntoCaminoLisa', 'Lisas'):
    check(f'(3) la firma del ala lisa VIVE en la DLL: {firma}', vive_dll(firma))

# === (4) LA CORONA DE CUCHILLAS ===
daga_cs = open(os.path.join(RAIZ, 'Content/Projectiles/DagaEternaMinion.cs'), encoding='utf-8').read()
check('(4) LA LEY DEL DESPEJE en la fuente: RadioTiara',
      'private static float RadioTiara(int nOjos) =>' in daga_cs and
      'MathF.Max(40f, 6.2f * nOjos) + 41f;' in daga_cs)
check('(4) LA ESCALA ×1,4 en la fuente: ESCALA_TIARA + Projectile.scale',
      'ESCALA_TIARA = 1.40f' in daga_cs and 'Projectile.scale = 1.40f;' in daga_cs)
check('(4) EL HITBOX GRANDE en la fuente: 26×26',
      'Projectile.width = 26;' in daga_cs and 'Projectile.height = 26;' in daga_cs)
check('(4) EL ARCO QUIETO encima de los ojos (aBorde, sin giro)',
      'aBorde' in daga_cs and 'MathHelper.TwoPi * idx / n' not in daga_cs)
check('(4) EL FILO HACIA AFUERA (rotCorona)',
      'rotCorona' in daga_cs)
for firma in ('RadioTiara', 'ESCALA_TIARA', 'SlotDelAnillo'):
    check(f'(4) la firma VIVE en la DLL: {firma}', vive_dll(firma))

# la tira de cuchillas INTACTA (la SForja v6.130 no se toca)
p = 'Content/Projectiles/DagaEterna_Hoja.rawimg'
okk = p in bl and _st.unpack('<iii', bl[p][:12])[1:] == (34, 170)
check('(4) la tira de cuchillas 34×170 INTACTA (la obra v6.130)', okk)

# === (5) EL PEGUE DEL FESTÍN ===
dev_cs = open(os.path.join(RAIZ, 'Content/Projectiles/Definitivas/DevoracionEterna.cs'), encoding='utf-8').read()
check('(5) EL DELTA en la fuente: DeltaDelFestin (estático público)',
      'public static Vector2 DeltaDelFestin' in dev_cs)
check('(5) EL PEGUE en la fuente: PegarAlFestin + ArrastrandoA',
      'public static bool PegarAlFestin' in dev_cs and
      'public static bool ArrastrandoA' in dev_cs)
check('(5) el delta se CALCULA en el arrastre (posAntes)',
      'Vector2 posAntes = p.Center;' in dev_cs and
      'DeltaDelFestin = p.Center - posAntes;' in dev_cs)
check('(5) el delta nace limpio cada tick y muere en OnKill',
      'DeltaDelFestin = Vector2.Zero;   // v6.131 — el delta nace limpio cada tick' in dev_cs and
      dev_cs.count('DeltaDelFestin = Vector2.Zero;') == 3)  # declaración + AI + OnKill

ojo_cs = open(os.path.join(RAIZ, 'Content/Projectiles/OjoEternoMinion.cs'), encoding='utf-8').read()
check('(5) LOS OJOS heredan el festín (PegarAlFestin en OjoEternoMinion)',
      '.PegarAlFestin(Projectile, Projectile.owner);' in ojo_cs)
check('(5) LAS CUCHILLAS heredan el festín (PegarAlFestin en DagaEternaMinion)',
      '.PegarAlFestin(Projectile, Projectile.owner);' in daga_cs)

brillo_cs = open(os.path.join(RAIZ, 'Content/Projectiles/Cosmetic/BrilloSolarHalo.cs'), encoding='utf-8').read()
check('(5) EL BRILLO SOLAR hereda el delta en el dibujado',
      '.ArrastrandoA(Projectile.owner)' in brillo_cs and 'DeltaDelFestin' in brillo_cs)

anillo_cs = open(os.path.join(RAIZ, 'Content/Projectiles/Cosmetic/AnilloRunicoDorsalHalo.cs'), encoding='utf-8').read()
check('(5) EL ANILLO DORSAL gira CON el cuerpo (fullRotation del festín)',
      '.ArrastrandoA(Projectile.owner)' in anillo_cs and
      'RotatedBy(duenio.fullRotation)' in anillo_cs)

for firma in ('DeltaDelFestin', 'PegarAlFestin', 'ArrastrandoA'):
    check(f'(5) la firma VIVE en la DLL: {firma}', vive_dll(firma))

# === (6) LA PARIDAD DE CLAVES hjson ×3 ===
import hjson


def claves(lang):
    dd = hjson.load(open(os.path.join(RAIZ, f'Localization/{lang}_Mods.AethonMod.hjson'), encoding='utf-8'))
    plano = set()

    def walk(pref, nodo):
        for k, v in nodo.items():
            if isinstance(v, dict):
                walk(pref + k + '.', v)
            else:
                plano.add(pref + k)
    walk('', dd)
    return plano


es, mx, en = claves('es-ES'), claves('es-MX'), claves('es-US' if False else 'en-US')
check(f'(6) la paridad de claves ×3 ({len(es)}/{len(mx)}/{len(en)})',
      es == mx == en, f'{len(es)}/{len(mx)}/{len(en)}')

print()
if _falla:
    print(f'AUDIT v6.131 — {len(_falla)} FALLAS: ' + '; '.join(_falla))
    raise SystemExit(1)
print('AUDIT v6.131 TODO VERIFICADO ✔')
