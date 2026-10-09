#!/usr/bin/env python3
"""
v6.50.82 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65082.py [ruta-al-.tmod]
"""
import hashlib
import shutil
import struct
import subprocess
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = sys.argv[1] if len(sys.argv) > 1 else \
    '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'

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
    if ver == '6.50.82' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.82)')

    print(f'=== 2. EOF EXACTO (doble fórmula) ===')
    total_comp = sum(c for _, _, c in entries)
    if tend + total_comp == fsize:
        ok(f'table_end {tend} + sum(comp) {total_comp} == {fsize}')
    else:
        mal(f'EOF: {tend}+{total_comp} != {fsize}')

    print(f'=== 3. ENTRADAS ({len(entries)}) — .81 tenía 380; +7 PNGs = 387 ===')
    if len(entries) == 387:
        ok(f'{len(entries)} entradas (380 + 7)')
    else:
        mal(f'{len(entries)} entradas (esperaba 387)')

    # inflar TODAS + reconstruir blobs por offset
    off = tend
    blobs = {}
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

    print(f'=== 4. LAS 7 TEXTURAS NUEVAS ===')
    nuevas = [
        'GrimorioHambriento', 'GrimorioHambriento_Iris', 'GrimorioHambriento_IrisRojo',
        'GrimorioHambriento_Medio', 'GrimorioHambriento_Rojo_Medio',
        'GrimorioHambriento_Cerrado', 'GrimorioHambriento_Rojo_Cerrado',
    ]
    for n in nuevas:
        hit = [p for p in blobs if p.endswith(f'/{n}.rawimg') or p == f'{n}.rawimg'
               or p.endswith(f'/{n}.rawimg'.lower())]
        if hit:
            ok(f'{hit[0]} ({len(blobs[hit[0]])} B inflado)')
        else:
            mal(f'FALTA {n}.rawimg')
            print('       paths con Grimorio:', [p for p in blobs if 'ambrient' in p])

    print(f'=== 5. DLL — símbolos del Grimorio Hambriento ===')
    dll = None
    for p, b in blobs.items():
        if p.endswith('.dll'):
            dll = b
            break
    if dll is None:
        mal('sin DLL en el paquete')
        return
    simbolos_utf8 = ['GrimorioHambriento', 'ElegirMirada', 'SnapDireccion', 'NivelRojo',
                     'UpdateInventory', 'PostDrawInInventory', 'PostDrawInWorld',
                     'ModifyTooltips', 'AddRecipes']
    for s in simbolos_utf8:
        if s.encode('utf-8') in dll:
            ok(f'UTF-8 «{s}» presente (metadata)')
        else:
            mal(f'UTF-8 «{s}» AUSENTE')
    literales_utf16 = [
        'Mods.AethonMod.Hambre.', 'Calmado', 'Inquieto', 'Furioso', 'Maximo',
        'Mods.AethonMod.Hambre.Reinicio',
        'AethonMod/Content/Weapons/GrimorioHambriento_Iris',
        'AethonMod/Content/Weapons/GrimorioHambriento_IrisRojo',
        'AethonMod/Content/Weapons/GrimorioHambriento_Medio',
        'AethonMod/Content/Weapons/GrimorioHambriento_Cerrado',
        'AethonMod/Content/Weapons/GrimorioHambriento_Rojo_Medio',
        'AethonMod/Content/Weapons/GrimorioHambriento_Rojo_Cerrado',
    ]
    for s in literales_utf16:
        if utf16(s) in dll:
            ok(f'UTF-16 «{s[:52]}» presente (#US)')
        else:
            mal(f'UTF-16 «{s[:52]}» AUSENTE')

    print(f'=== 6. HJSON — espejos e invariantes ===')
    h = {}
    for p, b in blobs.items():
        if p.endswith('.hjson'):
            h[p.split('/')[-1].split('_')[0]] = b

    def cuerpo(b):
        """El invariante .77 es CUERPO byte-idéntico: el espejo es-ES lleva una
        cabecera propia (ESPEJO GENERADO — NO EDITAR) que NO cuenta."""
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
    destino = '/home/sync/AethonMod-v6.50.82.tmod'
    shutil.copy2(TMOD, destino)
    md5b = hashlib.md5(open(destino, 'rb').read()).hexdigest()
    if md5b == md5:
        ok(f'/home/sync/AethonMod-v6.50.82.tmod ({len(d)} B, md5 idéntico)')
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
