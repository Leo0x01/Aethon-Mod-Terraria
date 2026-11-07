#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6113_tmod.py — LA AUDITORÍA DEL .tmod v6.113 — LAS CINCO ALAS DE
LA CASA + LOS MUROS DEL JUICIO + EL FARO DEL VEREDICTO.

LA LETRA: «guardar las siguientes alas: alas runicas del eclipse,
corolas del alba, alas runicas del vacio, alas runicas solares, alas
runicas del juicio — el resto de alas las puedes borrar / toma las alas
runicas del eclipse y has una copia llamada alas runicas del vacio, a
esta copia debes mejorar las alas, quitandole los picos y añadiendo
afectos, ademas de que las alas deben ser iguales tanto izquierda como
derecha o sea me refiero a no tener picos, su color debe ser el color
del vacio / algunos ataques del jefe se ven planos o con texturas
raras, por ejemplo los 3 pilares no deben bajar deben ocupar toda la
pantalla, asi que debes mejorar los ataques del jefe, los visuales y
toda la coreografia / mejora los hazes del jefe, y añade nuevos hazes
y mas seguido, asegurate que los ataques sean vistosos».

Verifica: versión · blobs íntegros/EOF · firmas VIVAS/MUERTAS en la DLL
· las cirugías en la fuente · hjson ×3 (espejo es-MX + frases nuevas +
frases muertas fuera) · los 5 iconos de alas VIVOS EXACTOS y los 18
muertos AUSENTES · 0 CJK / 0 IP en los .cs.
"""
import hashlib
import importlib.util
import os
import re
import struct
import zlib
import sys

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
RAIZ = os.path.join(REPO, 'AethonMod')
TMOD = os.path.expanduser('~/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod')

MD5_ESPERADO = 'e59c52fcb4964d0c36aee237235fef39'
TAMANO_ESPERADO = 11486715

spec = importlib.util.spec_from_file_location('parse_tmod', os.path.join(REPO, 'tools', 'parse_tmod.py'))
mp = importlib.util.module_from_spec(spec); spec.loader.exec_module(mp)


def blobs(d, tend, entries):
    out, off, malas = {}, tend, 0
    for p, raw, comp in entries:
        trozo = d[off:off + comp]
        if len(trozo) != comp: malas += 1; break
        try: data = trozo if comp == raw else zlib.decompress(trozo, -15)
        except zlib.error: malas += 1; break
        if len(data) != raw: malas += 1; break
        out[p] = data; off += comp
    return out, off, malas


d = open(TMOD, 'rb').read()
tmlver, name, ver, entries, tend, fsize = mp.parse(TMOD)
print(f'(0) {name} v{ver} · tML {tmlver} · {len(entries)} entradas · {fsize:,} B')
md5_real = hashlib.md5(d).hexdigest()
print(f'    md5 {md5_real}')
assert ver == '6.113', f'¡VERSIÓN {ver} ≠ 6.113!'
assert md5_real == MD5_ESPERADO, f'¡MD5 {md5_real} ≠ {MD5_ESPERADO}!'
assert fsize == TAMANO_ESPERADO, f'¡TAMAÑO {fsize} ≠ {TAMANO_ESPERADO}!'
todos, off, malas = blobs(d, tend, entries)
print(f'    blobs: {len(todos)}/{len(entries)} íntegros, malas={malas}, '
      f'EOF={off} == {fsize}: {"OK" if off == fsize else "¡ROMPO!"}')
assert malas == 0 and off == fsize

dll = todos['AethonMod.dll']
assert dll, 'sin DLL'

def vive(s):
    return s.encode() in dll or s.encode('utf-16-le') in dll

# ===================== (1) LAS FIRMAS =====================
# (las DLL solo conservan MÉTODOS/tipos/literales — los locales viven
#  en la FUENTE: la auditoría mira los DOS reinos, cada firma en el suyo)
fuente_boss = open(os.path.join(RAIZ, 'Content', 'NPCs', 'AethonBoss.cs'), encoding='utf-8').read()
fuente_proyectil = open(os.path.join(RAIZ, 'Content', 'Projectiles', 'Jefes', 'AtaqueJefeProjectile.cs'), encoding='utf-8').read()
fuente_haces = open(os.path.join(RAIZ, 'Content', 'Projectiles', 'Jefes', 'JefeHacesProjectiles.cs'), encoding='utf-8').read()
fuente_halo = open(os.path.join(RAIZ, 'Content', 'Projectiles', 'Cosmetic', 'AlasAuroraHalo.cs'), encoding='utf-8').read()
fuente_estilo = open(os.path.join(RAIZ, 'Content', 'VFX', 'AlasEstilo.cs'), encoding='utf-8').read()

vivas_dll = [
    # v6.113 — EL FARO DEL VEREDICTO (el haz nuevo)
    'JefeVeredictoProj',     # la clase del faro
    'CorazonFaro',           # la tercera capa (el hilo cegador)
    'VerticeVeredicto',      # el vértice que hierve
    'TinteVeloVeredicto',    # el velo dorado
    # v6.113 — LAS CINCO ALAS viven (las clases que quedaron)
    'AlasRunicasDelVacio',   # LA COPIA MEJORADA DEL ECLIPSE (rediseñada)
    'AlasRunicasSolares',
    'AlasRunicasDelEclipse',
    'AlasRunicasDelJuicio',
    'CorolasDelAlba',
]
muertas_dll = [
    # v6.113 — LA PURGA DE LAS ALAS: la Aurora de Tinta original,
    # LAS QUINCE del prisma, las Agujas y las Cuchillas
    'AuroraDeTinta',
    'AlasDeTintaSerena', 'AlasDePlasma', 'AlasDeNebulosa',
    'AlasDeCometario', 'AlasDeMedianoche', 'AlasDelPrisma',
    'AlasDeFuegoFrio', 'AlasDeTormenta', 'AlasDeMarioneta',
    'AlasDeAuroraArtica', 'AlasDeOrquidea', 'AlasDelVacio',
    'AlasSolaresRunicas', 'AlasDeSangreDeTinta',
    'AgujasDelVacio', 'CuchillasDelEclipse',
]
for s in vivas_dll:
    assert vive(s), f'falta la firma VIVA en la DLL: {s}'
for s in muertas_dll:
    assert not vive(s), f'la firma MUERTA sigue viva en la DLL: {s}'

vivas_fuente = [
    # el jefe — el compás y el faro
    'EL COMPÁS DE LOS HACES',               # el compás (boss)
    'EL HAZ DEL VEREDICTO',                 # el estado nuevo
    'LOS MUROS DE LUZ (v6.113)',            # la ley nueva del juicio
    # el proyectil — la pared de luz
    'LA PARED DE LUZ',                      # la autocuración del muro
    'EL MURO DEL JUICIO',                   # el caso de la AI
    # el haz — el radar y el corazón
    'EL RADAR',                             # el telégrafo del barrido
    # el halo — las migas del vacío
    'LAS MIGAS DEL VACÍO',                  # el efecto de las alas 2
    # el estilo — la copia mejorada
    'LA COPIA MEJORADA DEL ECLIPSE',        # el catálogo renumerado
    'LA PURGA DE LAS ALAS',                 # la lápida en el catálogo
    # la nube con volumen
    'LA NUBE CON VOLUMEN',                  # el fix de los planos
]
muertas_fuente = [
    # el descenso del pilar viejo (la letra: «no deben bajar»)
    'new Vector2(0f, 7f)',                  # la caída lenta del telégrafo
    'new Vector2(0f, 52f)',                 # la aceleración del descenso
    # el catálogo viejo
    'new EstiloAlas[24]',                   # el catálogo de las 23+1
    # la nube plana
    'LAS FLORES DE HUMO',                   # el comentario del plano
    # el caso especial del halo (la Aurora muerta)
    'alaEstilo == 1 || alaEstilo == 0',
]
for s in vivas_fuente:
    assert (s in fuente_boss) or (s in fuente_proyectil) or (s in fuente_haces) \
        or (s in fuente_halo) or (s in fuente_estilo), \
        f'falta en la fuente: {s}'
for s in muertas_fuente:
    assert (s not in fuente_boss) and (s not in fuente_proyectil) and (s not in fuente_haces) \
        and (s not in fuente_halo) and (s not in fuente_estilo), \
        f'la MUERTA sigue en la fuente: {s}'
print(f'(1) firmas: {len(vivas_dll)} VIVAS en la DLL · {len(muertas_dll)} MUERTAS ausentes '
      f'· {len(vivas_fuente)} lápidas/cirugías VIVAS · {len(muertas_fuente)} MUERTAS de fuente ausentes ✔')

# ===================== (2) EL HJSON ×3 =====================
def cuerpo(ruta):
    """El cuerpo SIN su bloque de cabecera (// líneas + blancos) — la
    MISMA definición del sync (la cabecera del espejo es DISTINTA por
    diseño: la CABECERA_ESPEJO del sync)."""
    lineas = open(ruta, encoding='utf-8').read().split('\n')
    i = 0
    while i < len(lineas) and (lineas[i].startswith('//') or lineas[i].strip() == ''):
        i += 1
    assert i < len(lineas), f'{ruta}: archivo sin cuerpo'
    return [l for l in lineas[i:] if l.strip()]


es = os.path.join(RAIZ, 'Localization', 'es-ES_Mods.AethonMod.hjson')
mx = os.path.join(RAIZ, 'Localization', 'es-MX_Mods.AethonMod.hjson')
en = os.path.join(RAIZ, 'Localization', 'en-US_Mods.AethonMod.hjson')
assert cuerpo(es) == cuerpo(mx), 'es-MX ≠ es-ES (correr el sync)'
print('(2) hjson ×3: es-MX espejo EXACTO del es-ES ✔ · en-US respetado ✔')

# (2b) el hjson EMPAQUETADO == la FUENTE byte a byte (la lección v6.50.63:
#      la auditoría v6.110 lo exigía; el -build de tML auto-añade claves
#      de localización que faltan — si cambia algo tras el build, ROMPE)
for carpeta, nombre in [('es-ES', 'es-ES'), ('es-MX', 'es-MX'), ('en-US', 'en-US')]:
    entrada = f'Localization/{nombre}_Mods.AethonMod.hjson'
    assert entrada in todos, f'falta en el .tmod: {entrada}'
    with open(os.path.join(RAIZ, 'Localization', f'{nombre}_Mods.AethonMod.hjson'), 'rb') as f:
        fuente_b = f.read()
    assert todos[entrada] == fuente_b, \
        f'{entrada}: el empaquetado ≠ la fuente (re-build tras el último cambio)'
print('(2b) hjson ×3 empaquetado == fuente BYTE A BYTE ✔')

es_txt = open(es, encoding='utf-8').read()
en_txt = open(en, encoding='utf-8').read()
nuevas_es = [
    'muros de luz a pantalla completa: muévete',      # el juicio nuevo
    'el faro del dios barre la arena',                # el veredicto nuevo
    'La copia mejorada del eclipse, YA SIN PICOS',    # las alas del vacío
    'Las CINCO alas de código de la casa',            # la bolsa de las alas
]
muertas_es = [
    # la purga de las alas (los tooltips muertos)
    'Veintidós alas de código',                       # la bolsa vieja
    'Violeta-cian: las runas del VACÍO',              # el tooltip viejo de las del vacío
    'Las alas creadas por código: diez plumas',       # la Aurora de Tinta
    'El lila de la página en calma',                  # Tinta Serena
    'El morado profundo del cielo tapado',            # Eclipse (la del prisma)
    'Cuatro plumas-aguja cóncavas',                   # Agujas del Vacío
    'Media luna de cuchilla',                         # Cuchillas del Eclipse
    # el juicio viejo (la letra: «no deben bajar»)
    'columnas de luz caen del cielo',
]
muertas_en = [
    'Veintidós alas de código',
    'Violeta-cian: las runas del VACÍO',
]
for s in nuevas_es:
    assert s in es_txt, f'falta la frase NUEVA en es-ES: {s}'
for s in muertas_es:
    assert s not in es_txt, f'la frase MUERTA sigue en es-ES: {s}'
for s in muertas_en:
    assert s not in en_txt, f'la frase MUERTA sigue en en-US: {s}'
print(f'(3) las {len(nuevas_es)} frases nuevas VIVAS · las {len(muertas_es) + len(muertas_en)} muertas AUSENTES (es-ES + en-US) ✔')

# ===================== (4) LOS ICONOS: LAS 5 ALAS VIVAS, LOS 18 MUERTOS =====================
from PIL import Image
import io

vivas_iconos = ['AlasRunicasSolares', 'AlasRunicasDelVacio',
                'AlasRunicasDelEclipse', 'AlasRunicasDelJuicio', 'CorolasDelAlba']
for ic in vivas_iconos:
    entrada = f'Content/Items/Alas/{ic}.rawimg'
    assert entrada in todos, f'falta el icono: {entrada}'
    raw = todos[entrada]
    w, h = struct.unpack('<ii', raw[4:12])
    assert (w, h) == (30, 30), f'{ic}: {w}x{h} ≠ 30x30'
    paquete = raw[12:]
    fuente_png = Image.open(os.path.join(RAIZ, 'Content', 'Items', 'Alas', ic + '.png')).convert('RGBA').tobytes()
    assert paquete == fuente_png, f'{ic}: el rawimg ≠ el PNG fuente'

muertos = [
    # las 17 de la carpeta Alas/
    'AlasDeTintaSerena', 'AlasDeEclipse', 'AlasDePlasma', 'AlasDeNebulosa',
    'AlasDeCometario', 'AlasDeMedianoche', 'AlasDelPrisma', 'AlasDeFuegoFrio',
    'AlasDeTormenta', 'AlasDeMarioneta', 'AlasDeAuroraArtica', 'AlasDeOrquidea',
    'AlasDelVacio', 'AlasSolaresRunicas', 'AlasDeSangreDeTinta',
    'AgujasDelVacio', 'CuchillasDelEclipse',
    # y la Aurora de Tinta (vive en Items/)
    'AuroraDeTinta',
]
for m in muertos:
    for carpeta in ('Alas', '..'):
        ruta_raw = f'Content/Items/{carpeta}/{m}.rawimg' if carpeta == 'Alas' else f'Content/Items/{m}.rawimg'
        assert ruta_raw not in todos, f'el icono MUERTO sigue en el .tmod: {ruta_raw}'
    base_png = os.path.join(RAIZ, 'Content', 'Items', 'Alas', m + '.png')
    base_png2 = os.path.join(RAIZ, 'Content', 'Items', m + '.png')
    assert not (os.path.exists(base_png) or os.path.exists(base_png2)), \
        f'el PNG MUERTO sigue en disco: {m}'
print(f'(4) los {len(vivas_iconos)} iconos de alas VIVOS 30×30 EXACTOS · los {len(muertos)} iconos muertos AUSENTES ✔')

# ===================== (5) 0 CJK / 0 IP (la ley de la casa) =====================
# («IP» = PROPIEDAD INTELECTUAL: nombres con copyright que NUNCA — la
#  lista de la casa; NO direcciones de red: las versiones «23.10.0.0»
#  de los comentarios son versiones, no IPs)
ips_cs = re.compile(r're:?zero|ousagi|daphne|subaru|roswaal', re.I)
pat_cjk = re.compile('[\u4e00-\u9fff\u3040-\u30ff\uac00-\ud7af]')
n_cs = 0
malos = []
for root, _, fs in os.walk(RAIZ):
    for f in fs:
        if f.endswith(('.cs', '.hjson')):
            n_cs += 1
            txt = open(os.path.join(root, f), encoding='utf-8', errors='replace').read()
            if ips_cs.search(txt) or pat_cjk.search(txt):
                malos.append(f)
assert not malos, malos
print(f'(5) 0 IP (propiedad intelectual) y 0 CJK en {n_cs} .cs+hjson ✔')

print()
print(f'AUDIT v6.113 TODO VERIFICADO — {len(todos)} entradas, EOF exacto {fsize:,} B')
print(f'md5 {md5_real}')
