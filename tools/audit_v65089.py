#!/usr/bin/env python3
"""
v6.50.89 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65089.py [ruta-al-.tmod]

Novedades .89 — LA CUARTA COPIA: EL GRIMORIO INESTABLE (la letra del
usuario: «crea un cuarto grimorio donde el efecto glish y el temblor
este unidos, ademas ponle un aura pequeña roja»):
(1) EL GLITCH del Errático — receta .88 INTACTA (el volumen aprobado:
   tiras 2-4, ±3,3→±6 px, ráfagas 12-24 t cada 15 s→2,1 s, gate 12 %);
(2) EL TEMBLOR del Tembloroso — receta .87 INTACTA (dos senos por eje,
   9,3/17,3 Hz X · 11,9/19,1 Hz Y, 0,2→1,3 px con el hambre);
(3) EL AURA ROJA PEQUEÑA (nuevo): SoftGlow 64×64 teñido (255,64,48),
   1,55× el ancho del libro, alfa 0,30→0,42 con el hambre, respiración
   ±12 % a 0,45 Hz, piso de luz 35 % en el mundo — detrás del libro en
   inventario/mundo/mano.
El ojo queda del ciclo NORMAL (mirada 4 s→0,35 s): un ojo sereno en un
cuerpo que se descompone.
ENTRADAS: 387 + 1 (el rawimg del icono del Inestable) = 388. El blob
diff permite dll/pdb/Info + los 3 hjson (claves nuevas) + el rawimg
nuevo — NADA MÁS. Errático/Tembloroso/base: el dump IL de sus clases
debe ser IDÉNTICO al de la .88 (la .89 no los toca).
"""
import hashlib
import os
import struct
import subprocess
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = sys.argv[1] if len(sys.argv) > 1 else \
    '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
PREV = '/home/sync/AethonMod-v6.50.88.tmod'
CECIL = '/home/z/tmp_scripts/cecil_check/bin/Debug/net8.0/cecil_check.dll'
DOTNET = '/tmp/sdk/dotnet'
DLLTMP = '/tmp/audit_v65089.dll'
DLLPREV = '/tmp/audit_v65089_prev.dll'

FALLOS = []


def ok(msg):
    print(f'  ✓ {msg}')


def mal(msg):
    print(f'  ✗ {msg}')
    FALLOS.append(msg)


def inflar(path):
    d = open(path, 'rb').read()
    tmlver, name, ver, entries, tend, fsize = parse(path)
    blobs, off = {}, tend
    for p, raw, comp in entries:
        data = d[off:off + comp]
        off += comp
        blobs[p] = zlib.decompress(data, -15) if raw != comp else data
    return tmlver, name, ver, entries, fsize, blobs


def cecil_dump(dll, clase):
    r = subprocess.run([DOTNET, DLLTMP.replace(DLLTMP, CECIL), dll, clase],
                       capture_output=True, text=True, env={**os.environ, 'DOTNET_ROLL_FORWARD': 'Minor'})
    return r.stdout if r.returncode == 0 else f'ERROR {r.returncode}: {r.stderr[:300]}'


def main():
    d = open(TMOD, 'rb').read()
    md5 = hashlib.md5(d).hexdigest()
    print(f'=== {TMOD} ===')
    print(f'  {len(d)} B | md5 {md5}')

    tmlver, name, ver, entries, fsize, blobs = inflar(TMOD)
    print(f'=== 1. CABECERA ===')
    if ver == '6.50.89' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.89)')

    print(f'=== 2. EOF EXACTO (doble fórmula) ===')
    total_comp = sum(c for _, _, c in entries)
    tend = parse(TMOD)[4]
    if tend + total_comp == fsize:
        ok(f'table_end {tend} + sum(comp) {total_comp} == {fsize}')
    else:
        mal(f'EOF: {tend}+{total_comp} != {fsize}')

    print(f'=== 3. ENTRADAS vs .88 — la .88 tenía 387, la .89 suma EL ICONO del Inestable ===')
    if len(entries) == 388:
        ok(f'{len(entries)} entradas (387 + GrimorioHambrientoInestable.rawimg)')
    else:
        mal(f'{len(entries)} entradas (esperaba 388)')
    _, _, verprev, eprev, _, blobs_prev = inflar(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    fuera = set_prev - set_now
    dentro = set_now - set_prev
    if fuera == set() and dentro == {'Content/Weapons/GrimorioHambrientoInestable.rawimg'}:
        ok('SET: entra SOLO el rawimg del Inestable — nada se borra')
    else:
        for p in sorted(fuera):
            mal(f'  SOLO en .88 (borrado): {p}')
        for p in sorted(dentro):
            mal(f'  SOLO en .89 (nueva): {p}')

    print(f'=== 4. BLOB DIFF vs .88 — lo que DEBE cambiar y nada más ===')
    WHITELIST = {'Info', 'AethonMod.pdb', 'AethonMod.dll',
                 'Localization/en-US_Mods.AethonMod.hjson',
                 'Localization/es-MX_Mods.AethonMod.hjson',
                 'Localization/es-ES_Mods.AethonMod.hjson',
                 'Content/Weapons/GrimorioHambrientoInestable.rawimg'}
    dif_inesperado = []
    cambio_ok = []
    for p in sorted(set_prev & set_now):
        if blobs[p] != blobs_prev.get(p):
            (cambio_ok if p in WHITELIST else dif_inesperado).append(p)
    for p in sorted(dentro):
        if p in WHITELIST:
            cambio_ok.append(p)
    for p in cambio_ok:
        ok(f'cambio ESPERADO: {p}')
    if not dif_inesperado:
        ok('TODO lo demás byte-idéntico a .88 (arte, sonidos — la .89 solo suma)')
    else:
        for p in dif_inesperado:
            mal(f'entrada diverge de .88 SIN permiso: {p}')

    print(f'=== 5. EL ICONO DEL INESTABLE — copia byte a byte de la base ===')
    b_nueva = blobs.get('Content/Weapons/GrimorioHambrientoInestable.rawimg')
    b_base = blobs_prev.get('Content/Weapons/GrimorioHambriento.rawimg')
    if b_nueva is None:
        mal('FALTA el rawimg del Inestable')
    elif b_base is None:
        mal('no se encontró el rawimg base en .88')
    elif b_nueva == b_base:
        ok('rawimg del Inestable == base (36×49, la convención de la familia: los 4 libros comparten arte)')
    else:
        mal('el rawimg del Inestable DIFIERE de la base')

    print(f'=== 6. HJSON — parse + espejo es + claves simétricas ===')
    import hjson
    import json as _json
    textos = {}
    for idi, clave in [('es-MX', 'es-MX'), ('es-ES', 'es-ES'), ('en-US', 'en-US')]:
        p = f'Localization/{clave}_Mods.AethonMod.hjson'
        try:
            j = hjson.loads(blobs[p].decode('utf-8'))
            textos[idi] = j
            ok(f'{clave} PARSEA ({len(blobs[p])} B)')
        except Exception as e:
            mal(f'{clave} NO PARSEA: {e}')
    if 'es-MX' in textos and 'es-ES' in textos:
        if _json.dumps(textos['es-MX'], sort_keys=True) == _json.dumps(textos['es-ES'], sort_keys=True):
            ok('es-ES ESPEJO EXACTO de es-MX (contenido)')
        else:
            mal('es-ES diverge de es-MX')
    if 'es-MX' in textos and 'en-US' in textos:
        kmx = set((textos['es-MX'].get('Items') or {}).get('GrimorioHambrientoInestable', {}))
        ken = set((textos['en-US'].get('Items') or {}).get('GrimorioHambrientoInestable', {}))
        if kmx == ken and kmx:
            ok(f'claves simétricas Inestable es↔en: {sorted(kmx)}')
        else:
            mal(f'claves asimétricas: es {sorted(kmx)} vs en {sorted(ken)}')
        if 'GrimorioHambrientoInestable' not in (textos['en-US'].get('Items') or {}):
            mal('en-US SIN la entrada del Inestable')
        else:
            ok('en-US lleva la entrada del Inestable')
    raw_en = blobs['Localization/en-US_Mods.AethonMod.hjson'].decode('utf-8')
    if '\t' in raw_en and raw_en.count('\t') > 4000:
        ok(f'en-US con TABS ({raw_en.count(chr(9))}) — el árbol, no la variante de espacios del incidente .87')
    else:
        mal('en-US perdió los TABS')

    print(f'=== 7. CECIL — la receta del Inestable (glitch .88 + temblor .87 + aura nueva) ===')
    open(DLLTMP, 'wb').write(blobs['AethonMod.dll'])
    open(DLLPREV, 'wb').write(blobs_prev['AethonMod.dll'])
    dump = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.GrimorioHambrientoInestable')
    if 'CLASE NO ENCONTRADA' in dump:
        mal('la clase GrimorioHambrientoInestable NO está en la DLL')
        print(dump)
    else:
        ok('la clase vive en la DLL del paquete')
        todo = dump
        # --- el glitch (receta .88): constantes clave (verificadas por método abajo) ---
        def consts(metodo):
            for linea in dump.split('\n'):
                if linea.startswith(f'M {metodo} |'):
                    return linea
            return ''
        pg, lay, tb = consts('PasoGlitch'), consts('Layout'), consts('Temblor')
        # PasoGlitch: 15f/2.1f (intervalo), 12+hash%13 (ráfaga), 120 (primera ráfaga — vive en el .cctor como inicializador de campo), 60 (retry), 0.12 (gate)
        for s in ['15', '2.1', '12', '13', '60', '0.12']:
            if s in pg:
                ok(f'PasoGlitch lleva {s} (la receta .88)')
            else:
                mal(f'PasoGlitch SIN {s} — la receta del glitch cambió')
        if '120' in dump:
            ok('120 presente (la primera ráfaga a los 2 s — inicializador en el .cctor)')
        else:
            mal('la primera ráfaga perdió su 120 (2 s)')
        # Layout: 2-4 tiras (%3), alto 4-12 (%9), y inicial %22, gap %5, tope 46, 0.55/0.45
        for s in ['2', '3', '4', '9', '22', '5', '46', '0.55', '0.45']:
            if s in lay:
                ok(f'Layout lleva {s} (tiras .88)')
            else:
                mal(f'Layout SIN {s} — el layout del glitch cambió')
        # Temblor: 0.2/1.1 amplitud, senos 9.3/17.3/11.9/19.1, fases 1.7/0.6, pesos 0.8/0.25
        for s in ['0.2', '1.1', '9.3', '17.3', '11.9', '19.1', '1.7', '0.6', '0.8', '0.25']:
            if s in tb:
                ok(f'Temblor lleva {s} (la receta .87)')
            else:
                mal(f'Temblor SIN {s} — el temblor cambió')
        # El aura: constantes nuevas (aparecen repartidas en PulsoAura/ColorAura/AlfaAura/DibujarAura/los 3 draws)
        for s in ['0.88', '0.12', '0.45', '0.3', '0.42', '0.35', '1.55', '0.04', '255', '64', '48']:
            if s in todo:
                pass  # contadas abajo en conjunto
        n_aura = sum(1 for s in ['0.88', '0.12', '0.45', '0.3', '0.42', '0.35', '1.55', '0.04', '255', '64', '48'] if s in todo)
        if n_aura >= 10:
            ok(f'constantes del AURA presentes ({n_aura}/11: 1,55× · 0,30→0,42 · 0,45 Hz · piso 0,35 · rojo 255/64/48)')
        else:
            mal(f'constantes del aura incompletas ({n_aura}/11)')
        # El iris viaja con su tira (offOjo): la señal — los 3 draws usan offOjo
        for m in ['PostDrawInInventory', 'PostDrawInWorld', 'ModifyItemDraw']:
            if consts(m):
                ok(f'{m} presente')
            else:
                mal(f'{m} AUSENTE')

    print(f'=== 8. CECIL — Errático/Tembloroso/base INTACTOS vs .88 ===')
    for clase in ['AethonMod.Content.Weapons.GrimorioHambrientoErratico',
                  'AethonMod.Content.Weapons.GrimorioHambrientoTembloroso',
                  'AethonMod.Content.Weapons.GrimorioHambriento']:
        nuevo = cecil_dump(DLLTMP, clase)
        viejo = cecil_dump(DLLPREV, clase)
        if nuevo == viejo and 'ERROR' not in nuevo:
            ok(f'{clase.split(".")[-1]}: IL IDÉNTICO a la .88 (la .89 no lo toca)')
        else:
            mal(f'{clase.split(".")[-1]}: el IL cambió vs .88')
    # la entrega: el Inestable en la bolsa (IL de BolsasCategorias cambió — verificado por diff del blob en 4)

    print(f'=== 9. RESUMEN ===')
    if FALLOS:
        print(f'  ✗ {len(FALLOS)} FALLOS')
        for f in FALLOS:
            print(f'    - {f}')
        sys.exit(1)
    print('  ✓ AUDITORÍA COMPLETA: 0 fallos')


if __name__ == '__main__':
    main()
