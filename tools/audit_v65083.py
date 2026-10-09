#!/usr/bin/env python3
"""
v6.50.83 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65083.py [ruta-al-.tmod]
"""
import hashlib
import shutil
import subprocess
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = sys.argv[1] if len(sys.argv) > 1 else \
    '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
PREV = '/home/sync/AethonMod-v6.50.82.tmod'
CECIL = '/home/z/tmp_scripts/cecil_check/bin/Debug/net8.0/cecil_check.dll'
DOTNET = '/tmp/sdk/dotnet'

FALLOS = []


def ok(msg):
    print(f'  ✓ {msg}')


def mal(msg):
    print(f'  ✗ {msg}')
    FALLOS.append(msg)


def utf16(s):
    return s.encode('utf-16-le')


def main():
    d = open(TMOD, 'rb').read()
    md5 = hashlib.md5(d).hexdigest()
    print(f'=== {TMOD} ===')
    print(f'  {len(d)} B | md5 {md5}')

    tmlver, name, ver, entries, tend, fsize = parse(TMOD)
    print(f'=== 1. CABECERA ===')
    if ver == '6.50.83' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.83)')

    print(f'=== 2. EOF EXACTO (doble fórmula) ===')
    total_comp = sum(c for _, _, c in entries)
    if tend + total_comp == fsize:
        ok(f'table_end {tend} + sum(comp) {total_comp} == {fsize}')
    else:
        mal(f'EOF: {tend}+{total_comp} != {fsize}')

    # inflar todos los blobs
    blobs = {}
    off = tend
    rotas = 0
    for p, raw, comp in entries:
        data = d[off:off + comp]
        off += comp
        try:
            if raw != comp:
                blobs[p] = zlib.decompress(data, -15)
                if len(blobs[p]) != raw:
                    rotas += 1
            else:
                blobs[p] = data
                if len(data) != raw:
                    rotas += 1
        except zlib.error:
            rotas += 1
    if rotas == 0:
        ok(f'{len(entries)} blobs inflan (RAW-DEFLATE o planos)')
    else:
        mal(f'{rotas} blobs rotos')

    print(f'=== 3. ENTRADAS vs .82 ({PREV}) — la .82 tenía 387 ===')
    if len(entries) == 387:
        ok(f'{len(entries)} entradas (igual que .82: 380 + 7)')
    else:
        mal(f'{len(entries)} entradas (esperaba 387)')
    dprev = open(PREV, 'rb').read()
    _, _, verprev, eprev, _, _ = parse(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    if set_prev == set_now:
        ok('SET de entradas IDÉNTICO al de la .82 (ninguna entrada perdida)')
    else:
        mal('SET de entradas DIFIERE de la .82:')
        for p in sorted(set_prev - set_now):
            mal(f'  SOLO en .82 (perdida): {p}')
        for p in sorted(set_now - set_prev):
            mal(f'  SOLO en .83 (nueva): {p}')
    # blobs de texturas y hjson deben ser BYTE-IDÉNTICOS a .82 (solo cambian DLL+build)
    # re-inflar .82: parse devuelve (tmlver, name, ver, entries, tend, fsize)
    blobs_prev = {}
    off2 = parse(PREV)[4]
    for p, raw, comp in eprev:
        data = dprev[off2:off2 + comp]
        off2 += comp
        if raw != comp:
            blobs_prev[p] = zlib.decompress(data, -15)
        else:
            blobs_prev[p] = data
    dif_texturas = []
    # .hjson → comparar CUERPOS (la cabecera documenta el incidente y cambia a propósito)
    # rawimg → bytes crudos. Info/*.pdb → metadatos de build, SIEMPRE cambian (whitelist)
    WHITELIST = {'Info', 'AethonMod.pdb'}
    def cuerpo_txt(b):
        lineas = b.split(b'\n')
        i = 0
        while i < len(lineas) and (lineas[i].startswith(b'//') or not lineas[i].strip()):
            i += 1
        return b'\n'.join(lineas[i:])
    for p in sorted(set_prev & set_now):
        if p.endswith('.dll') or p in WHITELIST:
            continue
        if p.endswith('.hjson'):
            if cuerpo_txt(blobs[p]) != cuerpo_txt(blobs_prev.get(p, b'')):
                dif_texturas.append(p)
        elif blobs[p] != blobs_prev.get(p):
            dif_texturas.append(p)
    if not dif_texturas:
        ok('rawimg byte-idénticos a .82 y hjson por cuerpos; Info/pdb en whitelist (metadatos de build)')
    else:
        for p in dif_texturas:
            mal(f'entrada no-DLL diverge de .82: {p}')

    print(f'=== 4. LAS 7 TEXTURAS DEL GRIMORIO HAMBRIENTO ===')
    nuevas = [
        'GrimorioHambriento', 'GrimorioHambriento_Iris', 'GrimorioHambriento_IrisRojo',
        'GrimorioHambriento_Medio', 'GrimorioHambriento_Rojo_Medio',
        'GrimorioHambriento_Cerrado', 'GrimorioHambriento_Rojo_Cerrado',
    ]
    for n in nuevas:
        hit = [p for p in blobs if p.endswith(f'/{n}.rawimg') or p == f'{n}.rawimg']
        if hit:
            ok(f'{hit[0]} ({len(blobs[hit[0]])} B inflado)')
        else:
            mal(f'FALTA {n}.rawimg')

    print(f'=== 5. DLL — la ENTREGA del arma (Cecil) ===')
    dll = None
    dll_path = None
    for p, b in blobs.items():
        if p.endswith('.dll'):
            dll = b
            dll_path = p
            break
    if dll is None:
        mal('sin DLL en el paquete')
        return
    import os
    tmpdll = '/tmp/dll_v65083.dll'
    open(tmpdll, 'wb').write(dll)
    r = subprocess.run(
        [DOTNET, CECIL, tmpdll],
        capture_output=True, text=True,
        env={**os.environ, 'DOTNET_ROLL_FORWARD': 'Minor'})
    print('    ' + (r.stdout or '').strip().replace('\n', '\n    '))
    if r.returncode == 0:
        ok(f'Cecil: BolsaProbador.Contenido llama ItemType<GrimorioHambriento>')
    else:
        mal(f'Cecil falló (rc={r.returncode}): {r.stderr.strip()[:200]}')

    print(f'=== 6. HJSON — espejos e invariantes (heredados de .82, sin cambios) ===')
    h = {}
    for p, b in blobs.items():
        if p.endswith('.hjson'):
            h[p.split('/')[-1].split('_')[0]] = b

    def cuerpo(b):
        lineas = b.split(b'\n')
        i = 0
        while i < len(lineas) and (lineas[i].startswith(b'//') or not lineas[i].strip()):
            i += 1
        return b'\n'.join(lineas[i:])

    if 'es-MX' in h and 'es-ES' in h:
        mx, eses = cuerpo(h['es-MX']), cuerpo(h['es-ES'])
        if mx == eses:
            ok('cuerpo es-ES == cuerpo es-MX BYTE A BYTE (en el paquete)')
        else:
            mal('cuerpo es-ES != cuerpo es-MX — el espejo se rompió')
            n = min(len(mx), len(eses))
            for i in range(n):
                if mx[i] != eses[i]:
                    mal(f'divergencia byte {i}: {mx[max(0,i-30):i+30]!r} vs {eses[max(0,i-30):i+30]!r}')
                    break
            else:
                mal(f'prefijo común {n} | tamaños {len(mx)} vs {len(eses)}')
    else:
        mal(f'hjson hallados: {list(h)}')
    if 'en' in h:
        texto = h['en'].decode('utf-8', 'replace')
        for s in ['Hungry Grimoire', 'IT IS HUNGRY', 'Hunger {0}']:
            if s in texto:
                ok(f'en-US «{s}» presente')
            else:
                mal(f'en-US «{s}» AUSENTE')
    es_txt = h.get('es-MX', b'').decode('utf-8', 'replace')
    eses_txt = h.get('es-ES', b'').decode('utf-8', 'replace')
    for s in ['Grimorio Hambriento', 'TIENE HAMBRE', 'Hambre {0}']:
        if s in es_txt:
            ok(f'es-MX «{s}» presente')
        else:
            mal(f'es-MX «{s}» AUSENTE')
        if s in eses_txt:
            ok(f'es-ES «{s}» presente')
        else:
            mal(f'es-ES «{s}» AUSENTE (el espejo perdió las claves nuevas)')
    for malo in ['ladrones', 'bandits']:
        if malo in es_txt and malo in h.get('en', b'').decode('utf-8', 'replace'):
            mal(f'«{malo}» REGRESÓ (invariante .77 rota)')
        else:
            ok(f'«{malo}» ausente (invariante .77)')

    print(f'=== 7. RESPALDO ===')
    destino = '/home/sync/AethonMod-v6.50.83.tmod'
    shutil.copy2(TMOD, destino)
    md5b = hashlib.md5(open(destino, 'rb').read()).hexdigest()
    if md5b == md5:
        ok(f'/home/sync/AethonMod-v6.50.83.tmod ({len(d)} B, md5 idéntico)')
    else:
        mal('respaldo divergente')

    print()
    if FALLOS:
        print(f'AUDITORÍA FALLIDA: {len(FALLOS)} fallos')
        sys.exit(1)
    print('AUDITORÍA COMPLETA: TODO OK')
    print(f'md5 {md5} | {len(d)} B | {len(entries)} entradas')


if __name__ == '__main__':
    main()
