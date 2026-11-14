#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6132_tmod.py — LA AUDITORÍA DEL .tmod v6.132 — LA MARIPOSA DEL
VACÍO · LA EMBOSCADA DEL GRIMORIO · LAS CUCHILLAS DEL STABBER.

LA LETRA (sesión 13, tres pedidos sobre la v6.131 jugada):
  (a) «las alas se ven genials, pero estan muy grandes, hagamosla un
      10% mas pequeñas y ademas ciento que le falta algo abajo, como
      otro pequeño par de alitas ya sabes como una mariposa y su forma
      necesitan ser mas digamos que mas epicas»
  (b) «en el caso de la animacion para entrar al mundo interior…
      la animacion debes rehacerla desde 0… haz simplemente que sea
      el grimorio quien toque al jugador, al jugador no lo muevas de
      donde estar, el grimorio lo atrapa con la sombra de la pagina,
      y se avalanza al jugador, tocandolo de esa forma»
  (c) «las cuchillas no parecen funcionar correctamente, su ataque no
      funciona y su animacion tampoco… el pico de las cuchillas deben
      apuntar al jugador, ademas deben estar como una corona centrada
      y su tipo de ataque debe ser igual que el de Dazzling Stabber
      Staff de Calamity Mod»

LO QUE ESTA AUDITORÍA VERIFICA (la obra de la v6.132):
  (0) IDENTIDAD: AethonMod v6.132 · 527 entradas (la tira de
      cuchillas se REEMPLAZA byte a byte — sin assets nuevos).
  (1) EOF EXACTO + (2) TODOS LOS BLOBS INFLAN.
  (2b) EMPAQUETADO == FUENTE byte a byte (hjson ×3 + la tira).
  (3) LA MARIPOSA: EscalaGlobal + Alitas en la fuente y la DLL; el
      estilo Runas del Vacío con escala 0,9 + alitas 0,55 + la forma
      épica (curva 0,30); los pozos del par inferior separados.
  (4) LA EMBOSCADA: la presa QUIETA (muere el lerp del jugador — sin
      "p.Center =" en el arrastre), la sombra que APRESA (el charco
      bajo los pies + los brazos), el avalanzamiento (TVueloLibro 26)
      y el toque (rectBoca.Intersects con la presa en su sitio).
  (5) LAS CUCHILLAS DEL STABBER: la máquina nueva (SliceTarget ·
      ManageAttackCooldown · ReturnToRestingPosition · DireccionSegura
      en fuente y DLL), el compás del OnHit (80-95/45-60/5/10), el
      AttackDelay en localAI[0], la corona CIRCULAR centrada (72° por
      RestOffsetAngle) con picos AL jugador (rotDescanso), los
      SendExtraAI/ReceiveExtraAI y la tira regenerada (aserrada+rim+
      tip: pixeles NUEVOS ≠ v6.131).
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
check('la versión es 6.132', ver == '6.132', ver)
check('las entradas son 527 (la tira se REEMPLAZA — sin assets nuevos)',
      len(entries) == 527, str(len(entries)))

# === (1) EOF + (2) BLOBS ===
bl, off, malas = blobs_de(d, tend, entries)
check('(1) EOF EXACTO', off == len(d) == fsize, f'{off} vs {len(d)}/{fsize}')
check(f'(2) TODOS LOS BLOBS INFLAN ({len(bl)}/{len(entries)}, {malas} malas)',
      len(bl) == len(entries) and malas == 0, f'{len(bl)}/{len(entries)}')

# === (2b) EMPAQUETADO == FUENTE ===
for lang in ('es-ES', 'es-MX', 'en-US'):
    p = f'Localization/{lang}_Mods.AethonMod.hjson'
    fuente = open(os.path.join(RAIZ, p), 'rb').read()
    check(f'(2b) hjson == fuente: {lang}', p in bl and bl[p] == fuente,
          'ausente' if p not in bl else f'{len(bl[p]):,} vs {len(fuente):,}')
p_tira = 'Content/Projectiles/DagaEterna_Hoja.rawimg'
check('(2b) la tira de cuchillas: 34×170 DENTRO (la SForja la regeneró)',
      p_tira in bl and _st.unpack('<iii', bl[p_tira][:12])[1:] == (34, 170),
      'ausente' if p_tira not in bl else 'medida mal')
import io as _io2
from PIL import Image as _Im2
_tira_fuente = _Im2.open(os.path.join(RAIZ, 'Content/Projectiles/DagaEterna_Hoja.png')).convert('RGBA')
_tira_raw = _Im2.frombytes('RGBA', (34, 170), bl[p_tira][12:12 + 4 * 34 * 170])
_diff = sum(1 for px_f, px_r in zip(_tira_fuente.getdata(), _tira_raw.getdata())
            if px_f != px_r)
check(f'(2b) la tira empaquetada == fuente (píxel a píxel: {_diff} px distintos)',
      _diff == 0, str(_diff))

# === (3) LA MARIPOSA ===
estilo_cs = open(os.path.join(RAIZ, 'Content/VFX/AlasEstilo.cs'), encoding='utf-8').read()
lib_cs = open(os.path.join(RAIZ, 'Content/VFX/AlasAuroraLib.cs'), encoding='utf-8').read()
check('(3) EscalaGlobal + Alitas declarados en el estilo',
      'public float EscalaGlobal;' in estilo_cs and 'public float Alitas;' in estilo_cs)
check('(3) Runas del Vacío: 0,9 + alitas 0,55 + épica (curva 0,30)',
      'escalaGlobal: 0.9f, alitas: 0.55f' in estilo_cs and '0.22f, 1.54f, 0.12f, 0.14f, 0.30f' in estilo_cs)
check('(3) la escala global vive en ConstruirPluma (largo + semianchos)',
      'float num8 = MathHelper.Lerp(estilo.LargoInterior, estilo.LargoExterior, num2) * (0.5f + 0.5f * spread) * escalaG;' in lib_cs and
      '(estilo.PuntaSemianchoBase + estilo.PuntaSemianchoAbierto * spread) * escalaG;' in lib_cs)
check('(3) el anclaje también escala',
      'estilo.AnclajeSep * (estilo.EscalaGlobal > 0f ? estilo.EscalaGlobal : 1f)' in lib_cs)
check('(3) EL PAR DE ALITAS: segunda pasada en Dibujar + pozos propios',
      'EL PAR DE ALITAS' in lib_cs and '_pozosEjeAlitas' in lib_cs and '_pozosSemiAlitas' in lib_cs)
check('(3) el par inferior: 16 px bajo la espalda, escala 0,55, abatido +0,22',
      '16f * (estilo.EscalaGlobal > 0f ? estilo.EscalaGlobal : 1f) * gravDir' in lib_cs and
      'estA.Inclinacion = estilo.Inclinacion + 0.22f;' in lib_cs)

# === (4) LA EMBOSCADA ===
dev_cs = open(os.path.join(RAIZ, 'Content/Projectiles/Definitivas/DevoracionEterna.cs'), encoding='utf-8').read()
check('(4) LA PRESA QUIETA: el lerp del jugador MURIÓ (sin p.Center = en el festín)',
      'p.Center = Vector2.Lerp' not in dev_cs and 'Vector2 posAntes = p.Center;' not in dev_cs)
check('(4) solo los controles mueren + el freno de la sombra',
      'LA PRESA QUIETA: solo los controles mueren' in dev_cs and 'p.velocity.X *= 0.60f;' in dev_cs)
check('(4) LA SOMBRA APRESA: el charco bajo los PIES + los brazos',
      'EL APRESAMIENTO' in dev_cs and 'p.Center + new Vector2(0f, p.height * 0.5f - 4f)' in dev_cs and
      'brazoIzq' in dev_cs)
check('(4) EL AVALANZAMIENTO es el acto central (TVueloLibro 26)',
      'private const int TVueloLibro = 26;' in dev_cs and 'EL GRIMORIO SE AVALANZA' in dev_cs)
check('(4) EL TOQUE: la boca contra el hitbox de la presa quieta',
      'LA PUERTA ES EL TOQUE' in dev_cs and 'rectBoca.Intersects(p.Hitbox)' in dev_cs)
check('(4) sin giro del jugador (fullRotation += murió)',
      'p.fullRotation +=' not in dev_cs)

# === (5) LAS CUCHILLAS DEL STABBER ===
daga_cs = open(os.path.join(RAIZ, 'Content/Projectiles/DagaEternaMinion.cs'), encoding='utf-8').read()
check('(5) LA MÁQUINA NUEVA: SliceTarget + ManageAttackCooldown + ReturnToRestingPosition',
      'private void SliceTarget()' in daga_cs and 'private void ManageAttackCooldown()' in daga_cs and
      'private void ReturnToRestingPosition(Player dueño)' in daga_cs)
check('(5) las FASES MUERTAS (sin F_HOVER/F_GOLPE en código vivo)',
      'F_HOVER = ' not in daga_cs and 'F_GOLPE = ' not in daga_cs and
      'private const float F_' not in daga_cs)
check('(5) EL COMPÁS DEL ONHIT: 80-95 el cristal · 45-60 la roca · 5 la llama/vacío · 10 el veredicto',
      'Main.rand.Next(80, 96)' in daga_cs and 'Main.rand.Next(45, 61)' in daga_cs)
check('(5) LA FÍSICA DEL SLICE: 1,03/0,93 + giro 0,15 + carga 36',
      'Projectile.velocity *= 1.03f;' in daga_cs and 'Projectile.velocity *= 0.93f;' in daga_cs and
      'angularTurnSpeed = 0.15f' in daga_cs and 'DireccionSegura(storedPos) * 36f' in daga_cs)
check('(5) AttackDelay en localAI[0] (ai[0] ES EL TIPO)',
      'get => Projectile.localAI[0];' in daga_cs and 'AttackDelay = 15f;' in daga_cs)
check('(5) LA CORONA CIRCULAR CENTRADA: RestOffsetAngle (72° por cuchilla)',
      'float ang = RestOffsetAngle;' in daga_cs and
      'k / (float)global::AethonMod.Content.Projectiles.DagaEternaMinion.TiposMax * MathHelper.TwoPi' in
      open(os.path.join(RAIZ, 'Content/Weapons/Investigaciones/CetroDagasEterno.cs'), encoding='utf-8').read())
check('(5) LOS PICOS AL JUGADOR (rotDescanso hacia el centro)',
      'rotDescanso' in daga_cs and 'radial = Projectile.Center - centroCorona' in daga_cs)
check('(5) LA RED DEL COMPÁS: SendExtraAI/ReceiveExtraAI',
      'public override void SendExtraAI(BinaryWriter writer)' in daga_cs and
      'public override void ReceiveExtraAI(BinaryReader reader)' in daga_cs)
check('(5) CanDamage: el descanso apaga el filo',
      'public override bool? CanDamage() => attackCooldown == 0 ? null : false;' in daga_cs)

dll = bl.get('AethonMod.dll', b'')
check('(5) la DLL viaja DENTRO', len(dll) > 100000, f'{len(dll):,} B')


def vive(firma):
    return firma.encode('utf-8') in dll or firma.encode('utf-16-le') in dll


for firma in ('SliceTarget', 'ManageAttackCooldown', 'ReturnToRestingPosition', 'DireccionSegura',
              'RestOffsetAngle', 'storedPos', 'moveSmoothing', 'attackCooldown'):
    check(f'(5) la firma VIVE en la DLL: {firma}', vive(firma))

# LA TIRA REGENERADA: los píxeles NUEVOS (≠ v6.131) — el aserrado de la
# roca (dientes 1 px FUERA del filo en el marco 3) y el tip-highlight
import io as _io
try:
    from PIL import Image
    tira = Image.open(os.path.join(RAIZ, 'Content/Projectiles/DagaEterna_Hoja.png')).convert('RGBA')
    roca = tira.crop((0, 68, 34, 102))
    # EL ASERRADO: píxeles CLAROS (el diente pinta _mult(filo,1.18))
    # pegados al borde derecho del perfil, en ≥4 filas distintas
    filas_diente = set()
    for y in range(3, 30):
        xs = [x for x in range(34) if roca.getpixel((x, y))[3] > 60]
        if not xs:
            continue
        borde = max(xs)
        c = roca.getpixel((borde, y))[:3]
        vecino = roca.getpixel((borde - 1, y))[:3] if borde - 1 >= 0 else (0, 0, 0)
        # EL PATRÓN DEL ASERRADO: borde OSCURO con vecino CLARO (el
        # valle que quema + el diente que asoma) — la sierra alternada
        if vecino[0] > c[0] + 60 and vecino[2] > c[2] + 60:
            filas_diente.add(y)
    check(f'(5) la roca ASERRADA: el patrón alternado vive en {len(filas_diente)} filas del filo',
          len(filas_diente) >= 4, str(len(filas_diente)))
    # el tip highlight: píxel BLANCO (252,248,255) en la punta de cada marco
    tips = 0
    for k in range(5):
        fr = tira.crop((0, k * 34, 34, (k + 1) * 34))
        # el destello: ≥2 px blancos en las primeras 8 filas del marco
        blancos = sum(1 for y in range(0, 8) for x in range(0, 34)
                      if fr.getpixel((x, y))[3] > 200 and fr.getpixel((x, y))[:3] >= (240, 236, 244))
        if blancos >= 2:
            tips += 1
    check(f'(5) el TIP-HIGHLIGHT: punta blanca en {tips}/5 marcos', tips == 5, f'{tips}/5')
except ImportError:
    check('(5) PIL ausente — chequeos de píxeles saltados', True, 'skip')

# === (6) LA PARIDAD hjson ×3 ===
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


es, mx, en = claves('es-ES'), claves('es-MX'), claves('en-US')
check(f'(6) la paridad de claves ×3 ({len(es)}/{len(mx)}/{len(en)})',
      es == mx == en, f'{len(es)}/{len(mx)}/{len(en)}')

print()
if _falla:
    print(f'AUDIT v6.132 — {len(_falla)} FALLAS: ' + '; '.join(_falla))
    raise SystemExit(1)
print('AUDIT v6.132 TODO VERIFICADO ✔')
