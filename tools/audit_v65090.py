#!/usr/bin/env python3
"""
v6.50.90 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65090.py [ruta-al-.tmod]

Novedades .90 — EL TERCER ASIENTO CAMBIA DE DUEÑO: EL GRIMORIO NERVIOSO
(la letra del usuario: «el tercer libro borralo, y crea otro con las
mismas caracteristicas, temblor, glisheado, y nervioso»):
(1) EL TEMBLOR — receta .87 INTACTA heredada del difunto Tembloroso
   (dos senos por eje, 9,3/17,3 Hz X · 11,9/19,1 Hz Y, 0,2→1,3 px);
(2) EL GLITCH — receta .88 INTACTA (tiras 2-4, ±3,3→±6 px, ráfagas
   12-24 t cada 15 s→2,1 s, gate 12 %) con máquina PROPIA;
(3) EL NERVIOSO (nuevo, dos capas): el OJO ANSIOSO (EstadoGrimorio.
   Nervioso: dwell 2,4 s→0,42 s, LERP 0,22, 1/3 al centro, 1/2 de las
   demás en dardo lateral) y EL SOBRESALTO (6,5 s→1,8 s con el hambre,
   brinco 1,2→2,2 px que se asienta en 7 ticks; con hambre ≥12 % el
   susto trae ráfaga corta 8-14 t — EL SUSTO ROMPE EL LIBRO).
SIN aura roja — la firma del Inestable no se hereda.
ENTRADAS: 388 de la .89 − 1 (rawimg del Tembloroso) + 1 (rawimg del
Nervioso) = 388. El blob diff permite dll/pdb/Info + los 3 hjson (la
clave nueva + el tooltip del Inestable sin la cita al difunto) + el
rawimg del Nervioso entrante y el del Tembloroso saliente — NADA MÁS.
Errático/Inestable/base: el dump IL de sus clases debe ser IDÉNTICO al
de la .89. EstadoGrimorio CAMBIA (documentado: la bandera Nervioso +
sus 3 ramas — con la bandera apagada la secuencia de Main.rand y la
matemática son las de la .89). El tooltip del Inestable pierde la cita
«como el Tembloroso» (el libro ya no existe).
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
PREV = '/home/sync/AethonMod-v6.50.89.tmod'
CECIL = '/home/z/tmp_scripts/cecil_check/bin/Debug/net8.0/cecil_check.dll'
DOTNET = '/tmp/sdk/dotnet'
DLLTMP = '/tmp/audit_v65090.dll'
DLLPREV = '/tmp/audit_v65090_prev.dll'

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
    r = subprocess.run([DOTNET, CECIL, dll, clase],
                       capture_output=True, text=True, env={**os.environ, 'DOTNET_ROLL_FORWARD': 'Minor'})
    return r.stdout if r.returncode == 0 else f'ERROR {r.returncode}: {r.stderr[:300]}'


def main():
    d = open(TMOD, 'rb').read()
    md5 = hashlib.md5(d).hexdigest()
    print(f'=== {TMOD} ===')
    print(f'  {len(d)} B | md5 {md5}')

    tmlver, name, ver, entries, fsize, blobs = inflar(TMOD)
    print(f'=== 1. CABECERA ===')
    if ver == '6.50.90' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.90)')

    print(f'=== 2. EOF EXACTO (doble fórmula) ===')
    total_comp = sum(c for _, _, c in entries)
    tend = parse(TMOD)[4]
    if tend + total_comp == fsize:
        ok(f'table_end {tend} + sum(comp) {total_comp} == {fsize}')
    else:
        mal(f'EOF: {tend}+{total_comp} != {fsize}')

    print(f'=== 3. ENTRADAS vs .89 — SALE el rawimg del Tembloroso, ENTRA el del Nervioso ===')
    _, _, verprev, eprev, _, blobs_prev = inflar(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    fuera = set_prev - set_now
    dentro = set_now - set_prev
    if len(entries) == 388:
        ok(f'{len(entries)} entradas (388 de .89 − 1 Tembloroso + 1 Nervioso)')
    else:
        mal(f'{len(entries)} entradas (esperaba 388)')
    if fuera == {'Content/Weapons/GrimorioHambrientoTembloroso.rawimg'} \
            and dentro == {'Content/Weapons/GrimorioHambrientoNervioso.rawimg'}:
        ok('SET: sale SOLO el rawimg del Tembloroso, entra SOLO el del Nervioso')
    else:
        for p in sorted(fuera):
            mal(f'  SOLO en .89 (borrado): {p}')
        for p in sorted(dentro):
            mal(f'  SOLO en .90 (nueva): {p}')

    print(f'=== 4. BLOB DIFF vs .89 — lo que DEBE cambiar y nada más ===')
    WHITELIST = {'Info', 'AethonMod.pdb', 'AethonMod.dll',
                 'Localization/en-US_Mods.AethonMod.hjson',
                 'Localization/es-MX_Mods.AethonMod.hjson',
                 'Localization/es-ES_Mods.AethonMod.hjson',
                 'Content/Weapons/GrimorioHambrientoTembloroso.rawimg',
                 'Content/Weapons/GrimorioHambrientoNervioso.rawimg'}
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
        ok('TODO lo demás byte-idéntico a .89 (arte, sonidos — la .90 solo cambia de dueño)')
    else:
        for p in dif_inesperado:
            mal(f'entrada diverge de .89 SIN permiso: {p}')

    print(f'=== 5. EL ICONO DEL NERVIOSO — copia byte a byte de la base ===')
    b_nueva = blobs.get('Content/Weapons/GrimorioHambrientoNervioso.rawimg')
    b_base = blobs.get('Content/Weapons/GrimorioHambriento.rawimg')
    if b_nueva is None:
        mal('FALTA el rawimg del Nervioso')
    elif b_base is None:
        mal('no se encontró el rawimg base')
    elif b_nueva == b_base:
        ok('rawimg del Nervioso == base (36×49, la convención de la familia: los efectos viven en el código)')
    else:
        mal('el rawimg del Nervioso DIFIERE de la base')

    print(f'=== 6. HJSON — parse + espejo es + claves simétricas + el difunto fuera ===')
    import hjson
    import json as _json
    textos = {}
    for clave in ['es-MX', 'es-ES', 'en-US']:
        p = f'Localization/{clave}_Mods.AethonMod.hjson'
        try:
            j = hjson.loads(blobs[p].decode('utf-8'))
            textos[clave] = j
            ok(f'{clave} PARSEA ({len(blobs[p])} B)')
        except Exception as e:
            mal(f'{clave} NO PARSEA: {e}')
    if 'es-MX' in textos and 'es-ES' in textos:
        if _json.dumps(textos['es-MX'], sort_keys=True) == _json.dumps(textos['es-ES'], sort_keys=True):
            ok('es-ES ESPEJO EXACTO de es-MX (contenido)')
        else:
            mal('es-ES diverge de es-MX')
    for idi in ['es-MX', 'en-US']:
        items = (textos.get(idi) or {}).get('Items') or {}
        if 'GrimorioHambrientoNervioso' in items and 'GrimorioHambrientoTembloroso' not in items:
            ok(f'{idi}: el Nervioso vive, el Tembloroso NO (claves)')
        else:
            mal(f'{idi}: claves del tercer asiento mal (Nervioso={("GrimorioHambrientoNervioso" in items)}, '
                f'Tembloroso={("GrimorioHambrientoTembloroso" in items)})')
    if 'es-MX' in textos and 'en-US' in textos:
        kmx = set((textos['es-MX'].get('Items') or {}).get('GrimorioHambrientoNervioso', {}))
        ken = set((textos['en-US'].get('Items') or {}).get('GrimorioHambrientoNervioso', {}))
        if kmx == ken and kmx:
            ok(f'claves simétricas Nervioso es↔en: {sorted(kmx)}')
        else:
            mal(f'claves asimétricas: es {sorted(kmx)} vs en {sorted(ken)}')
    raw_en = blobs['Localization/en-US_Mods.AethonMod.hjson'].decode('utf-8')
    if '\t' in raw_en and raw_en.count('\t') > 4000:
        ok(f'en-US con TABS ({raw_en.count(chr(9))}) — el árbol, no la variante de espacios del incidente .87')
    else:
        mal('en-US perdió los TABS')

    print(f'=== 7. CECIL — la receta del Nervioso (glitch .88 + temblor .87 + nervios nuevo) ===')
    open(DLLTMP, 'wb').write(blobs['AethonMod.dll'])
    open(DLLPREV, 'wb').write(blobs_prev['AethonMod.dll'])
    dump = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.GrimorioHambrientoNervioso')
    if 'CLASE NO ENCONTRADA' in dump:
        mal('la clase GrimorioHambrientoNervioso NO está en la DLL')
        print(dump)
    else:
        ok('la clase vive en la DLL del paquete')

        def consts(metodo):
            for linea in dump.split('\n'):
                if linea.startswith(f'M {metodo} |'):
                    return linea
            return ''
        pm, lay, tb = consts('PasoMaquina'), consts('Layout'), consts('Temblor')
        # PasoMaquina: el glitch .88 (15f/2.1f/12/%13/60/0.12) + el sobresalto NUEVO
        # (6.5f/1.8f/1.2f/2.2f) + el susto-que-rompe (8+%7 + la sal 0x2545F491).
        # OJO (lección .88 reloaded): las sales hex van al IL como Ldc_I4
        # DECIMAL — 0x2545F491 = 625341585 y 0x68E31DA4 = 1759714724.
        for s in ['15', '2.1', '12', '13', '60', '0.12']:
            if s in pm:
                ok(f'PasoMaquina lleva {s} (el glitch .88)')
            else:
                mal(f'PasoMaquina SIN {s} — la receta del glitch cambió')
        for s in ['6.5', '1.8', '1.2', '2.2', '625341585', '1759714724', '8']:
            if s in pm:
                ok(f'PasoMaquina lleva {s} (el sobresalto nuevo)')
            else:
                mal(f'PasoMaquina SIN {s} — el sobresalto cambió')
        if '120' in dump:
            ok('120 presente (la primera ráfaga a los 2 s)')
        else:
            mal('la primera ráfaga perdió su 120 (2 s)')
        if '300' in dump:
            ok('300 presente (el primer sobresalto a los 5 s)')
        else:
            mal('el primer sobresalto perdió su 300 (5 s)')
        # Layout: 2-4 tiras (%3), alto 4-12 (%9), y inicial %22, gap %5, tope 46, 0.55/0.45
        for s in ['2', '3', '4', '9', '22', '5', '46', '0.55', '0.45']:
            if s in lay:
                ok(f'Layout lleva {s} (tiras .88)')
            else:
                mal(f'Layout SIN {s} — el layout del glitch cambió')
        # Temblor: 0.2/1.1 amplitud, senos 9.3/17.3/11.9/19.1, fases 1.7/0.6, pesos 0.8/0.25
        for s in ['0.2', '1.1', '9.3', '17.3', '11.9', '19.1', '1.7', '0.6', '0.8', '0.25']:
            if s in tb:
                ok(f'Temblor lleva {s} (la receta .87 del difunto)')
            else:
                mal(f'Temblor SIN {s} — el temblor cambió')
        # SIN aura: la firma del Inestable NO se hereda
        if '1.55' in dump:
            mal('el Nervioso lleva 1.55 — ¡la constante del AURA del Inestable!')
        else:
            ok('SIN constantes del aura (1.55 ausente) — la firma roja es del Inestable')
        # El iris viaja con su tira (offOjo): los 3 draws
        for m in ['PostDrawInInventory', 'PostDrawInWorld', 'ModifyItemDraw']:
            if consts(m):
                ok(f'{m} presente')
            else:
                mal(f'{m} AUSENTE')

    print(f'=== 8. CECIL — Errático/Inestable/base INTACTOS vs .89 + el difunto AUSENTE ===')
    for clase in ['AethonMod.Content.Weapons.GrimorioHambrientoErratico',
                  'AethonMod.Content.Weapons.GrimorioHambrientoInestable',
                  'AethonMod.Content.Weapons.GrimorioHambriento']:
        nuevo = cecil_dump(DLLTMP, clase)
        viejo = cecil_dump(DLLPREV, clase)
        if nuevo == viejo and 'ERROR' not in nuevo:
            ok(f'{clase.split(".")[-1]}: IL IDÉNTICO a la .89 (la .90 no lo toca)')
        else:
            mal(f'{clase.split(".")[-1]}: el IL cambió vs .89')
    # el difunto: la clase NO debe existir en la DLL nueva (el dumper sale
    # con código 2 y el wrapper lo envuelve en "ERROR 2" — las dos formas
    # cuentan como AUSENTE)
    difunto = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.GrimorioHambrientoTembloroso')
    if 'CLASE NO ENCONTRADA' in difunto or difunto.startswith('ERROR 2'):
        ok('GrimorioHambrientoTembloroso: AUSENTE de la DLL (borrado de verdad)')
    else:
        mal('la clase del Tembloroso SIGUE en la DLL')
    # EstadoGrimorio: CAMBIA (documentado) — la bandera + las 3 ramas del nerviosismo
    est_nuevo = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.EstadoGrimorio')
    est_viejo = cecil_dump(DLLPREV, 'AethonMod.Content.Weapons.EstadoGrimorio')
    if est_nuevo == est_viejo:
        mal('EstadoGrimorio NO cambió — ¿dónde está la bandera Nervioso?')
    else:
        ok('EstadoGrimorio CAMBIA (esperado: la bandera Nervioso + sus ramas)')
        # OJO (lección .88 reloaded): Roslyn PLEGÓ las constantes — 2.4f*60f
        # vive como 144 y 0.42f*60f como 25.2 en el IL
        for s in ['0.22', '144', '25.2']:
            if s in est_nuevo:
                ok(f'EstadoGrimorio lleva {s} (la mirada nerviosa — 2,4 s·60 y 0,42 s·60 plegados)')
            else:
                mal(f'EstadoGrimorio SIN {s} — la mirada nerviosa incompleta')
        # la bandera apagada = secuencia de Main.rand intacta: el 10 (Next(10) del centro) sigue
        for s in ['10', '3', '2']:
            if s in est_nuevo:
                ok(f'EstadoGrimorio lleva {s} (el patrón al-centro/lateral)')
            else:
                mal(f'EstadoGrimorio SIN {s}')
    # la entrega: la bolsa cita al Nervioso y NO al difunto (nombres en el heap de metadata)
    dll_bytes = blobs['AethonMod.dll']
    if b'GrimorioHambrientoNervioso' in dll_bytes:
        ok('la DLL cita GrimorioHambrientoNervioso (la bolsa del probador lo entrega)')
    else:
        mal('la DLL NO cita al Nervioso — ¿falta el registro?')
    if b'GrimorioHambrientoTembloroso' in dll_bytes:
        mal('la DLL aún cita al Tembloroso (referencia zombie)')
    else:
        ok('la DLL ya NO cita al Tembloroso (sin referencias zombies)')

    print(f'=== 9. RESUMEN ===')
    if FALLOS:
        print(f'  ✗ {len(FALLOS)} FALLOS')
        for f in FALLOS:
            print(f'    - {f}')
        sys.exit(1)
    print('  ✓ AUDITORÍA COMPLETA: 0 fallos')


if __name__ == '__main__':
    main()
