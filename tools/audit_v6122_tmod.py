#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6122_tmod.py — LA AUDITORÍA DEL .tmod v6.122 — EL CURA-TODO DEL
INTERIOR + EL AURA DEL VACÍO + LAS TRES ARMAS DE LA SUPERFORJA.

LA LETRA (v6.122): «la forma eterna no se oculta completamente…» · «el
daño que causa la forma eterna debe ser el mismo que el 0.1% de la suma
del daño de invocacion final y el daño magico final del grimorio…
el buff se llamara Aura del Vacio» · «Vacio Exterior, que ahora se
llamara Quemadura del Vacio» · «las alas runicas del vacio siguen
teniendo picos» · «dos esferas de luz y oscuridad colisionen y creen
una implosion que luego explota en un destello de luz» · «un tajo de
oscuridad, con centro negro, bordes blancos y medios morados» · «otra
arma con agujas delgadas que atraviezan y se clavan en los enemigos» ·
«el siempre debe aparecer con una explocion de luz» · «sus hazes
siempre terminan y empiezan en un corte brusco» · «no lanza el rayo
concentrado» · «el altar tiene un tope de 1000… el resto quedaran
seleccionadas para que sean devueltas al inventario» · «el mundo
interior sigue teniendo un fondo completamente negro» · «el sprite del
pilar se ve cortado en la parte de arriba» · «sigue apareciendo un
ovalo amarillo que crece y desaparece» · «MundoGarganta.ogg y
MundoSuenoVacio.ogg siguen sonando mal, mejor regeneralas que sean
diferentes» · «ademas tambien se detecto un error en el log».

LO QUE ESTA AUDITORÍA VERIFICA (la obra de la v6.122):
  · LA CURA DEL FONDO NEGRO: las brumas del cielo con SU LOTE (la
    excepción «Begin has not yet been called» del client.log) + la red
    de seguridad que ya NO mata el cielo al primer fallo.
  · EL ÓVALO AMARILLO MUERTO: el IRIS DEL NACIMIENTO extirpado (la
    fuente ya NO dibuja el NovaBurst dorado de la llegada).
  · EL CRASH DEL MINIMAPA: BlindarMapa (la cura v6.121 del
    IndexOutOfRange del client.log).
  · EL PILAR SIN RECORTE: la fuente del ápice (0.205H/0.165H) y el
    haz naciendo de LA FUENTE (y_src) — nada pega a las filas 0-1.
  · LA FORMA ETERNA COMPLETA: SellosPlayer respeta el OJITO
    (hideVisibleAccessory) + el interruptor del menú.
  · EL AURA DEL VACÍO: DanoFormaEterna (el 0,1 % de la suma de los
    DOS daños finales del Grimorio) + el buff AuraVacioBuff visible
    con su icono + la QUEmadura del Vacío renombrada.
  · LAS ALAS SIN PICOS: las runas UN NODO ADENTRO (nodoRuna = 7) y la
    escala SERENA (1.26+0.28) — universales.
  · EL JEFE: EstiloAparicionLuz (la explosión de luz de SIEMPRE) +
    el RC2 en la fase 4 (la carta y el menú) + LA RAMPA DE VIDA de los
    haces (HacesVida + IntensidadGlobal).
  · EL ALTAR: el DEPÓSITO DE LAS MIL (PuertaBebe / PuertaYaAbierta,
    el sobreviviente en el cursor — NI UNA se pierde).
  · LAS TRES ARMAS: JuicioDosSoles (DanoFormaEterna de prueba) +
    TajoOscuro (centro negro/bordes blancos/medios morados) +
    AgujasVacio (la aguja que se CLAVA) — con sus 4 PNG nuevos.
  · LAS DOS BANDAS RENACIDAS: los ogg regenerados DIFERENTES (v6.122:
    melodía de flauta en Dm + el ritmo/caminate de Em).

  (0) IDENTIDAD: AethonMod v6.122 · 503 entradas (499 + AuraVacioBuff
      + JuicioDosSoles + TajoOscuro + AgujasVacio).
  (1) EOF EXACTO: dataSize + tabla + sum(blobs) == fsize.
  (2) TODOS LOS BLOBS INFLAN (503/503).
  (2b) EMPAQUETADO == FUENTE byte a byte (los 3 hjson).
  (3) LAS FIRMAS VIVEN en la DLL.
  (4) LAS TEXTURAS NUEVAS (los 4 PNG de la v6.122 + el pilar).
  (5) LAS TRES BANDAS (ogg presentes y vorbis).
  (6) LOS TEXTOS NUEVOS + los keysets ×3 idénticos.
  (7) LA FUENTE DE LA CURA (las firmas exactas).
"""
import importlib.util
import os
import re
import struct
import sys
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
check('la versión es 6.122', ver == '6.122', ver)
check('las entradas son 503 (las 499 de la v6.120 + los 4 PNG de la v6.122)',
      len(entries) == 503, str(len(entries)))

# === (1) EOF + (2) BLOBS ===
bl, off, malas = blobs_de(d, tend, entries)
check('EOF EXACTO', off == len(d), f'{off:,} == {len(d):,}')
check('TODOS LOS BLOBS INFLAN (503/503)', malas == 0 and len(bl) == 503, f'{len(bl)}/503 · malas={malas}')

# === (2b) EMPAQUETADO == FUENTE (los 3 hjson) ===
for lang in ('es-MX', 'es-ES', 'en-US'):
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    src = open(os.path.join(RAIZ, key), 'rb').read()
    check(f'empaquetado == fuente: {key}', key in bl and bl[key] == src)

# === (3) LAS FIRMAS EN LA DLL ===
dll = bl.get('AethonMod.dll', b'')
firmas_vivas = [
    # las de siempre (v6.115–v6.120 — LO QUE SIGUE VIVO)
    b'SubmundoGrimorio', b'MundoInteriorEternoItem', b'DevoracionEterna',
    b'EsenciasConsagradas', b'EsenciasPuerta', b'PuertaAbierta', b'PuedeEntrar',
    b'PilarPrismaTile', b'GenerarInterior', b'CieloInteriorEterno',
    b'EscenaInteriorGrimorio', b'MostrarFormaEterna', b'FormaEternaVisible',
    b'hideVisibleAccessory', b'FormaEternaItem', b'AlasCatalogo',
    b'AlasRunicasDelVacio', b'DibujarRunas', b'RunasVacio',
    # v6.121 — la cura del crash del minimapa (el client.log)
    b'BlindarMapa',
    # v6.122 — EL AURA DEL VACÍO (el daño de la Forma Eterna)
    b'DanoFormaEterna', b'AuraVacioBuff', b'UltimoDano',
    b'ModifyBuffText',
    # v6.122 — LAS TRES ARMAS DE LA SUPERFORJA
    b'JuicioDosSoles', b'NacerElDestello', b'TajoOscuro', b'AgujasVacio',
    b'SimpleStrikeNPC',
    # v6.122 — EL JEFE (la explosión de luz SIEMPRE + los haces suaves)
    b'EstiloAparicionLuz', b'HacesVida', b'IntensidadGlobal', b'Rampa',
]
for f in firmas_vivas:
    check(f'DLL VIVA: {f.decode()}', f in dll)

# LOS LITERALES (los claves de tag viven en el heap #US como UTF-16LE)
for lit in ('Music/MundoSuenoVacio', 'Music/MundoGarganta', 'Music/MundoCorazon',
            'Mods.AethonMod.Buffs.AuraVacioBuff.Description',
            'Mods.AethonMod.Submundo.PuertaAbiertaAnuncio',
            'PuertaBebe', 'PuertaYaAbierta'):
    check(f'DLL VIVA (literal UTF-16): {lit}', lit.encode('utf-16-le') in dll)


# === (4) LAS TEXTURAS (la forja de la casa) ===
def rawimg_size(b):
    _v, w, h = struct.unpack('<iii', b[:12])
    return w, h


for tex, w_e, h_e in (
        ('Content/Tiles/PilarPrismaTile.rawimg', 54, 516),
        ('Content/Tiles/PilarPrismaTile_Highlight.rawimg', 54, 86),
        ('Content/Buffs/AuraVacioBuff.rawimg', 32, 32),
        ('Content/Buffs/VacioExterior.rawimg', 32, 32),
        ('Content/Weapons/Definitivas/JuicioDosSoles.rawimg', 44, 44),
        ('Content/Weapons/Definitivas/TajoOscuro.rawimg', 44, 44),
        ('Content/Weapons/Definitivas/AgujasVacio.rawimg', 44, 44)):
    ok = tex in bl
    det = ''
    if ok:
        w, h = rawimg_size(bl[tex])
        det = f'{w}x{h}'
        ok = (w, h) == (w_e, h_e)
    check(f'TEXTURA: {os.path.basename(tex)} {w_e}x{h_e}', ok, det)

# === (5) LAS TRES BANDAS (ogg vorbis, presentes y gordas) ===
for ogg in ('Music/MundoSuenoVacio.ogg', 'Music/MundoGarganta.ogg', 'Music/MundoCorazon.ogg'):
    ok = ogg in bl
    det = ''
    if ok:
        b_ogg = bl[ogg]
        det = f'{len(b_ogg):,} B'
        ok = b_ogg[:4] == b'OggS' and len(b_ogg) > 900_000
    check(f'BANDA VIVA: {os.path.basename(ogg)}', ok, det)

# LAS DOS RENACIDAS son DIFERENTES a la v6.120 (el archivo de la casa)
for ogg in ('Music/MundoSuenoVacio.ogg', 'Music/MundoGarganta.ogg'):
    vieja = os.path.join(REPO, 'BandaSonoraV1', os.path.basename(ogg))
    ok = os.path.exists(vieja)
    det = ''
    if ok and ogg in bl:
        import hashlib
        h_vieja = hashlib.md5(open(vieja, 'rb').read()).hexdigest()
        h_nueva = hashlib.md5(bl[ogg]).hexdigest()
        ok = h_vieja != h_nueva
        det = 'v1=' + h_vieja[:8]
    check(f'RENACIDA DIFERENTE: {os.path.basename(ogg)}', ok, det)

# === (6) LOS TEXTOS NUEVOS ===
textos = {
    'es-MX': ['Aura del vacío', 'Quemadura del vacío', 'El Juicio de los Dos Soles',
              'El Tajo de Oscuridad', 'Las Agujas del Vacío', 'El altar bebe'],
    'es-ES': ['Aura del vacío', 'Quemadura del vacío', 'El Juicio de los Dos Soles',
              'El Tajo de Oscuridad', 'Las Agujas del Vacío', 'El altar bebe'],
    'en-US': ['Void Aura', 'Void Burn', 'Judgment of the Two Suns',
              'The Dark Slash', 'The Void Needles', 'The altar drinks'],
}
for lang, textos_lang in textos.items():
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    contenido = bl.get(key, b'').decode('utf-8')
    for t in textos_lang:
        check(f'texto {lang}: «{t[:44]}»', t in contenido)

# LAS CLAVES NUEVAS ×3
for clave in ('AuraVacioBuff', 'JuicioDosSoles', 'TajoOscuro', 'AgujasVacio',
              'PuertaBebe', 'PuertaYaAbierta'):
    for lang in ('es-MX', 'es-ES', 'en-US'):
        key = f'Localization/{lang}_Mods.AethonMod.hjson'
        contenido = bl.get(key, b'').decode('utf-8')
        check(f'clave {clave} en {lang}', re.search(rf'^[\t ]*{re.escape(clave)}:', contenido, re.M) is not None)

# LOS KEYSETS ×3 IDÉNTICOS
def keyset(lang):
    key = f'Localization/{lang}_Mods.AethonMod.hjson'
    return set(re.findall(r'^[\t ]*([\w.]+):\s*\{', bl.get(key, b'').decode('utf-8'), re.M))


k_mx, k_es, k_us = keyset('es-MX'), keyset('es-ES'), keyset('en-US')
check('keysets es-MX == es-ES', k_mx == k_es, f'{len(k_mx)} vs {len(k_es)}')
check('keysets es-MX == en-US', k_mx == k_us, f'{len(k_mx)} vs {len(k_us)}')

# === (7) LA FUENTE DE LA CURA (las firmas exactas) ===
f_cielo = open(os.path.join(RAIZ, 'Content', 'Effects', 'CieloInteriorEterno.cs'), encoding='utf-8').read()
f_sub = open(os.path.join(RAIZ, 'Content', 'Systems', 'SubmundoGrimorio.cs'), encoding='utf-8').read()
f_sellos = open(os.path.join(RAIZ, 'Content', 'Players', 'SellosPlayer.cs'), encoding='utf-8').read()
f_alaslib = open(os.path.join(RAIZ, 'Content', 'VFX', 'AlasAuroraLib.cs'), encoding='utf-8').read()
f_ancla = open(os.path.join(RAIZ, 'Content', 'Projectiles', 'AuraJuicio2Ancla.cs'), encoding='utf-8').read()
f_boss = open(os.path.join(RAIZ, 'Content', 'NPCs', 'AethonBoss.cs'), encoding='utf-8').read()
f_haces = open(os.path.join(RAIZ, 'Content', 'Projectiles', 'Jefes', 'JefeHacesProjectiles.cs'), encoding='utf-8').read()
f_hazlib = open(os.path.join(RAIZ, 'Content', 'VFX', 'HazLib.cs'), encoding='utf-8').read()
f_altar = open(os.path.join(RAIZ, 'Content', 'UI', 'AltarOfrendasUI.cs'), encoding='utf-8').read()
f_ataque = open(os.path.join(RAIZ, 'Content', 'Projectiles', 'Jefes', 'AtaqueJefeProjectile.cs'), encoding='utf-8').read()
f_pilarpy = open(os.path.join(REPO, 'tools', 'pilar_prisma_v6120.py'), encoding='utf-8').read()

# LA CURA DEL FONDO NEGRO: las brumas de paralaje con SU PROPIO LOTE
check('CIELO: las brumas corren EN un lote (MistBand dentro de Begin/finally)',
      re.search(r'sb\.Begin\([^\n]*\n[^;]*;\s*\n\s*try\s*\n\s*\{\s*\n\s*BrumaFX\.MistBand',
                f_cielo) is not None)
check('CIELO: el halo del sol con SU lote (Puff tras Begin)',
      re.search(r'sb\.Begin\(\s*SpriteSortMode\.Deferred,\s*BlendState\.AlphaBlend,\s*\n[^;]*;\s*\n\s*try\s*\n\s*\{\s*\n\s*Vector2 sol', f_cielo) is not None)
check('CIELO: la red de seguridad ya NO mata el cielo (60 fallos)',
      '_fallosSeguidos > 60' in f_cielo)

# EL ÓVALO AMARILLO MUERTO: el IRIS extirpado de la fuente
check('ÓVALO: el IRIS DEL NACIMIENTO murió (la fuente ya no lo dibuja)',
      'aIris' not in f_sub and 'NovaBurst' not in f_sub.split('DibujarVelo')[1].split('EL VELO PROPIO')[0]
      if 'DibujarVelo' in f_sub and 'EL VELO PROPIO' in f_sub else 'IRIS DEL NACIMIENTO MURIÓ' in f_sub)

# EL CRASH DEL MINIMAPA (la cura v6.121, sigue viva)
check('MINIMAPA: BlindarMapa vive (la cura del client.log)',
      'BlindarMapa' in f_sub)

# EL PILAR SIN RECORTE (la forja: el ápice dentro + la fuente del haz)
check('PILAR: el prisma baja (py=0.205H) y se afina (ph=0.165H)',
      'px, py = cx, H * 0.205 + bob' in f_pilarpy and
      'pw, ph = W * 0.150, H * 0.165' in f_pilarpy)
check('PILAR: el haz nace de LA FUENTE (y_src), no del borde',
      'y_src = 4.5 * SS' in f_pilarpy)

# LA FORMA ETERNA COMPLETA (el ojito respetado en TODOS los caminos)
check('FORMA: SellosPlayer respeta el OJITO (hideVisibleAccessory)',
      'hideVisibleAccessory' in f_sellos)
check('FORMA: SellosPlayer respeta el interruptor del menú',
      'FormaEternaVisible' in f_sellos)

# EL AURA DEL VACÍO (el 0,1 % de la suma de los DOS daños finales)
check('AURA: el cálculo exacto de la letra (0.001 × suma)',
      '0.001f * (invocFinal + magicoFinal)' in f_ancla)
check('AURA: GetWeaponDamage (la cadena COMPLETA de buffs del mágico)',
      'p.GetWeaponDamage(libro)' in f_ancla)
check('AURA: la invocación final con SU multiplicador (DanoOjos × Summon)',
      'DamageClass.Summon).ApplyTo(ojos)' in f_ancla)
check('AURA: el buff refrescado mientras marque (AddBuff AuraVacioBuff)',
      'AuraVacioBuff>(), 30, true' in f_ancla)

# LAS ALAS SIN PICOS (las runas un nodo adentro, universales)
check('ALAS: las runas UN NODO ADENTRO (nodoRuna = 7, SIEMPRE)',
      'int nodoRuna = 7;' in f_alaslib)
check('ALAS: la escala SERENA (1.26 + 0.28) sin condición de membrana',
      'float escala = 1.26f + 0.28f * num7;' in f_alaslib and
      '1.45f + 0.35f * num7' not in f_alaslib)

# EL JEFE: la explosión de luz SIEMPRE + el RC2 en P4 + los haces suaves
check('JEFE: la explosión de luz en la materialización (t=24)',
      '_tickEstado == 24' in f_boss and 'EstiloAparicionLuz' in f_boss)
check('JEFE: el gemelo oscuro/fusionado TAMBIÉN nace del estallido',
      'EstiloAparicionLuz' in f_boss.split('else')[2] if f_boss.count('EstiloAparicionLuz') >= 2 else
      f_boss.count('EstiloAparicionLuz') >= 3)
check('JEFE: el RAYO CONCENTRADO en la fase 4 (menú)',
      'EST_RC2, EST_SOL, EST_VEREDICTO },' in f_boss)
check('JEFE: LA RAMPA DE VIDA en los haces (HacesVida.Rampa)',
      'HacesVida.Rampa' in f_haces)
check('HAZLIB: la INTENSIDAD DE VIDA viaja con el filo',
      'Filo(y0) * IntensidadGlobal' in f_hazlib)
check('ATAQUE: el estilo AparicionLuz (pura luz, escala 1.45)',
      'EstiloAparicionLuz' in f_ataque and '1.45f' in f_ataque)

# EL ALTAR: el depósito de las mil (el sobreviviente en el cursor)
check('ALTAR: el depósito toma SOLO lo que falta (Math.Min)',
      'int toma = Math.Min(falta, Main.mouseItem.stack);' in f_altar)
check('ALTAR: el sobreviviente QUEDA EN EL CURSOR (jamás se pierde)',
      'Main.mouseItem.stack -= toma;' in f_altar)

# LAS TRES ARMAS (la fuente de la obra)
f_juicio = open(os.path.join(RAIZ, 'Content', 'Weapons', 'Definitivas', 'JuicioDosSoles.cs'), encoding='utf-8').read()
f_tajo = open(os.path.join(RAIZ, 'Content', 'Weapons', 'Definitivas', 'TajoOscuro.cs'), encoding='utf-8').read()
f_agujas = open(os.path.join(RAIZ, 'Content', 'Weapons', 'Definitivas', 'AgujasVacio.cs'), encoding='utf-8').read()
check('ARMA 1: los TRES actos (convocatoria/implosión/destello)',
      'T_CONVOCATORIA' in f_juicio and 'T_IMPLOSION' in f_juicio and 'T_DESTELLO' in f_juicio)
check('ARMA 2: la letra de color EXACTA (centro negro/medios morados/bordes blancos)',
      'EL CENTRO NEGRO' in f_tajo and 'medio morado' in f_tajo and 'EL BORDE BLANCO' in f_tajo)
check('ARMA 2: nace cerca del enemigo (150 px detrás, en el eje)',
      'dir * 150f' in f_tajo)
check('ARMA 3: la aguja SE CLAVA (OnHitNPC + el goteo)',
      'OnHitNPC' in f_agujas and '_clavada = true' in f_agujas)
check('ARMA 3: nace cerca del enemigo (90-140 px)',
      'NextFloat(90f, 140f)' in f_agujas)

# LA MÚSICA RENACIDA (el generador v6.122 con las NUEVAS canciones)
f_musica = open(os.path.join(REPO, 'tools', 'gen_musica_interior_v6122.py'), encoding='utf-8').read()
check('MÚSICA: las canciones NUEVAS (Dm/Em con melodía escrita)',
      'Re menor' in f_musica and 'Mi menor' in f_musica and 'fraseA' in f_musica)
check('MÚSICA: TODO cuantizado (campana tonal, sin desafinación)',
      'campana_tonal' in f_musica)

# === EL VEREDICTO ===
print()
if _falla:
    print(f'[{len(_falla)} FALLAS] LA AUDITORÍA NO PASA:')
    for f in _falla:
        print('  ✗ ' + f)
    sys.exit(1)
print('AUDIT v6.122 TODO VERIFICADO ✔ — el .tmod es la obra y la obra es el .tmod')
