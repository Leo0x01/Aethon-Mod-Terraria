#!/usr/bin/env python3
"""
v6.50.84 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65084.py [ruta-al-.tmod]

Novedades .84: (3) el blob diff contra .83 espera cambiar SOLO las 2 capas del
iris (ahora discos 32×32 recortados del sprite exacto) + dll/pdb/Info;
(4b) CIRCULARIDAD del iris empaquetado: 32×32, esquinas TRANSPARENTES,
centro OPACO, y el halo del borde con α intermedio.
"""
import hashlib
import io
import shutil
import subprocess
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = sys.argv[1] if len(sys.argv) > 1 else \
    '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
PREV = '/home/sync/AethonMod-v6.50.83.tmod'
CECIL = '/home/z/tmp_scripts/cecil_check/bin/Debug/net8.0/cecil_check.dll'
DOTNET = '/tmp/sdk/dotnet'

FALLOS = []


def ok(msg):
    print(f'  ✓ {msg}')


def mal(msg):
    print(f'  ✗ {msg}')
    FALLOS.append(msg)


def main():
    d = open(TMOD, 'rb').read()
    md5 = hashlib.md5(d).hexdigest()
    print(f'=== {TMOD} ===')
    print(f'  {len(d)} B | md5 {md5}')

    tmlver, name, ver, entries, tend, fsize = parse(TMOD)
    print(f'=== 1. CABECERA ===')
    if ver == '6.50.84' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.84)')

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

    print(f'=== 3. ENTRADAS vs .83 ({PREV}) — la .83 tenía 387 ===')
    if len(entries) == 387:
        ok(f'{len(entries)} entradas (igual que .83)')
    else:
        mal(f'{len(entries)} entradas (esperaba 387)')
    dprev = open(PREV, 'rb').read()
    _, _, verprev, eprev, _, _ = parse(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    if set_prev == set_now:
        ok('SET de entradas IDÉNTICO al de la .83')
    else:
        mal('SET de entradas DIFIERE de la .83:')
        for p in sorted(set_prev - set_now):
            mal(f'  SOLO en .83 (perdida): {p}')
        for p in sorted(set_now - set_prev):
            mal(f'  SOLO en .84 (nueva): {p}')
    # re-inflar .83
    blobs_prev = {}
    off2 = parse(PREV)[4]
    for p, raw, comp in eprev:
        data = dprev[off2:off2 + comp]
        off2 += comp
        if raw != comp:
            blobs_prev[p] = zlib.decompress(data, -15)
        else:
            blobs_prev[p] = data

    # lo que DEBE cambiar en la .84: las 2 capas del iris (disco circular nuevo)
    ESPERADO_CAMBIO = {
        'Content/Weapons/GrimorioHambriento_Iris.rawimg',
        'Content/Weapons/GrimorioHambriento_IrisRojo.rawimg',
    }
    WHITELIST = {'Info', 'AethonMod.pdb'}  # metadatos de build (siempre cambian)
    dif_inesperado, cambio_ok = [], []
    for p in sorted(set_prev & set_now):
        if p.endswith('.dll') or p in WHITELIST:
            continue
        if blobs[p] != blobs_prev.get(p):
            (cambio_ok if p in ESPERADO_CAMBIO else dif_inesperado).append(p)
    for p in cambio_ok:
        ok(f'cambio ESPERADO: {p} (el disco circular nuevo)')
    if not dif_inesperado:
        ok('el resto byte-idéntico a .83 (base, párpados, hjson, TODO lo demás)')
    else:
        for p in dif_inesperado:
            mal(f'entrada diverge de .83 SIN permiso: {p}')

    print(f'=== 4. LAS 7 TEXTURAS DEL GRIMORIO HAMBRIENTO ===')
    nuevas = [
        'GrimorioHambriento', 'GrimorioHambriento_Iris', 'GrimorioHambriento_IrisRojo',
        'GrimorioHambriento_Medio', 'GrimorioHambriento_Rojo_Medio',
        'GrimorioHambriento_Cerrado', 'GrimorioHambriento_Rojo_Cerrado',
    ]
    rutas = {}
    for n in nuevas:
        hit = [p for p in blobs if p.endswith(f'/{n}.rawimg') or p == f'{n}.rawimg']
        if hit:
            rutas[n] = hit[0]
            ok(f'{hit[0]} ({len(blobs[hit[0]])} B inflado)')
        else:
            mal(f'FALTA {n}.rawimg')

    print(f'=== 4b. CIRCULARIDAD DEL IRIS EMPAQUETADO (la letra del usuario) ===')
    import struct
    import numpy as np

    def rawimg_rgba(b):
        """rawimg tML: 12 B (versión, ancho, alto) + RGBA crudo."""
        v, w, h = struct.unpack('<III', b[:12])
        assert v == 1, f'versión rawimg {v}'
        a = np.frombuffer(b[12:12 + w * h * 4], dtype=np.uint8).reshape(h, w, 4)
        assert len(b) == 12 + w * h * 4, f'{len(b)} != {12 + w * h * 4}'
        return a, w, h

    for n in ('GrimorioHambriento_Iris', 'GrimorioHambriento_IrisRojo'):
        if n not in rutas:
            continue
        a, w, h = rawimg_rgba(blobs[rutas[n]])
        alfa = a[..., 3]
        esquinas = int(alfa[0, 0]) + int(alfa[0, -1]) + int(alfa[-1, 0]) + int(alfa[-1, -1])
        centro = int(alfa[h // 2, w // 2])
        if w == 32 and h == 32:
            ok(f'{n}: 32×32 (4× supermuestreo)')
        else:
            mal(f'{n}: {w}×{h} (esperaba 32×32)')
        if esquinas == 0:
            ok(f'{n}: esquinas TRANSPARENTES (α=0) — el recorte es CIRCULAR, no cuadrado')
        else:
            mal(f'{n}: α esquinas={esquinas} — ¡el recorte tiene esquinas!')
        if centro == 255:
            ok(f'{n}: centro OPACO (α=255)')
        else:
            mal(f'{n}: α centro={centro}')
        # halo del borde: entre r13 y r16 debe haber α intermedio (la pluma)
        cy, cx = h / 2, w / 2
        yy, xx = np.mgrid[0:h, 0:w]
        dist = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
        halo = alfa[(dist > 13) & (dist < 16)]
        if halo.size and 0 < int(halo.mean()) < 255:
            ok(f'{n}: borde con pluma (α media {halo.mean():.0f} en el anillo r13-16)')
        else:
            mal(f'{n}: sin pluma en el borde (α media {halo.mean() if halo.size else -1:.0f})')
        # el centro del disco es la PUPILA oscura; a r≈11 texels el ORO/ROJO
        c = a[h // 2, w // 2]        # pupila
        g_old = a[h // 2, w // 2 + 11]  # r≈11 a la derecha: el disco dorado/rojo
        if int(c[:3].sum()) < 150:
            ok(f'{n}: centro = PUPILA oscura RGB=({c[0]},{c[1]},{c[2]})')
        else:
            mal(f'{n}: centro RGB=({c[0]},{c[1]},{c[2]}) — la pupila no es oscura')
        if n.endswith('_Iris'):
            if g_old[0] > 200 and g_old[1] > 180 and g_old[2] < g_old[1] - 60:
                ok(f'{n}: disco DORADO RGB=({g_old[0]},{g_old[1]},{g_old[2]})')
            else:
                mal(f'{n}: RGB=({g_old[0]},{g_old[1]},{g_old[2]}) — no parece oro')
        else:
            if g_old[0] > 200 and g_old[0] > int(g_old[1]) + 80:
                ok(f'{n}: disco ROJO RGB=({g_old[0]},{g_old[1]},{g_old[2]})')
            else:
                mal(f'{n}: RGB=({g_old[0]},{g_old[1]},{g_old[2]}) — no parece rojo')

    print(f'=== 5. DLL — ENTREGA + IRIS EN TODAS PARTES (Cecil) ===')
    dll = None
    for p, b in blobs.items():
        if p.endswith('.dll'):
            dll = b
            break
    if dll is None:
        mal('sin DLL en el paquete')
        return
    import os
    tmpdll = '/tmp/dll_v65084.dll'
    open(tmpdll, 'wb').write(dll)
    r = subprocess.run(
        [DOTNET, CECIL, tmpdll],
        capture_output=True, text=True,
        env={**os.environ, 'DOTNET_ROLL_FORWARD': 'Minor'})
    print('    ' + (r.stdout or '').strip().replace('\n', '\n    '))
    if r.returncode == 0:
        ok('Cecil: entrega + ModifyItemDraw + PostDrawInWorld(Item.Bottom) + IRIS_ESC')
    else:
        mal(f'Cecil falló (rc={r.returncode}): {r.stderr.strip()[:200]}')

    print(f'=== 6. HJSON — espejos e invariantes (heredados, sin cambios) ===')
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
    destino = '/home/sync/AethonMod-v6.50.84.tmod'
    shutil.copy2(TMOD, destino)
    md5b = hashlib.md5(open(destino, 'rb').read()).hexdigest()
    if md5b == md5:
        ok(f'/home/sync/AethonMod-v6.50.84.tmod ({len(d)} B, md5 idéntico)')
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
