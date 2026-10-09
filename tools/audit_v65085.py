#!/usr/bin/env python3
"""
v6.50.85 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65085.py [ruta-al-.tmod]

Novedades .85: (3) el blob diff contra .84 espera cambiar SOLO las 3 texturas
del libro (base limpia sin iris + párpados compuestos) — las 2 capas del iris
quedan BYTE-IDÉNTICAS (el disco aprobado); el SET pierde EXACTAMENTE las 2
texturas rojas del libro; (4b) BLANCURA DEL SOCKET empaquetado (la letra del
usuario: sin sombra del iris); (4c) EL ROJO SOLO EN EL IRIS — la DLL ya no
menciona Rojo_Medio/Rojo_Cerrado y las constantes OJO nuevas están en el IL.
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
PREV = '/home/sync/AethonMod-v6.50.84.tmod'
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
    if ver == '6.50.85' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.85)')

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

    print(f'=== 3. ENTRADAS vs .84 ({PREV}) — la .84 tenía 387 ===')
    if len(entries) == 385:
        ok(f'{len(entries)} entradas (387 − 2 texturas rojas del libro)')
    else:
        mal(f'{len(entries)} entradas (esperaba 385)')
    dprev = open(PREV, 'rb').read()
    _, _, verprev, eprev, _, _ = parse(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    esperadas_fuera = {
        'Content/Weapons/GrimorioHambriento_Rojo_Medio.rawimg',
        'Content/Weapons/GrimorioHambriento_Rojo_Cerrado.rawimg',
    }
    fuera = set_prev - set_now
    dentro = set_now - set_prev
    if fuera == esperadas_fuera and not dentro:
        ok('SET: SOLO desaparecen Rojo_Medio/Rojo_Cerrado (el libro queda normal)')
    else:
        for p in sorted(fuera):
            (ok if p in esperadas_fuera else mal)(f'  SOLO en .84: {p}')
        for p in sorted(dentro):
            mal(f'  SOLO en .85 (nueva): {p}')
    # re-inflar .84
    blobs_prev = {}
    off2 = parse(PREV)[4]
    for p, raw, comp in eprev:
        data = dprev[off2:off2 + comp]
        off2 += comp
        if raw != comp:
            blobs_prev[p] = zlib.decompress(data, -15)
        else:
            blobs_prev[p] = data

    # lo que DEBE cambiar en la .85: base (socket limpio) + 2 párpados compuestos
    ESPERADO_CAMBIO = {
        'Content/Weapons/GrimorioHambriento.rawimg',
        'Content/Weapons/GrimorioHambriento_Medio.rawimg',
        'Content/Weapons/GrimorioHambriento_Cerrado.rawimg',
    }
    IRIS_INTACTO = {
        'Content/Weapons/GrimorioHambriento_Iris.rawimg',
        'Content/Weapons/GrimorioHambriento_IrisRojo.rawimg',
    }
    WHITELIST = {'Info', 'AethonMod.pdb'}  # metadatos de build (siempre cambian)
    dif_inesperado, cambio_ok, iris_roto = [], [], []
    for p in sorted(set_prev & set_now):
        if p.endswith('.dll') or p in WHITELIST:
            continue
        if blobs[p] != blobs_prev.get(p):
            if p in ESPERADO_CAMBIO:
                cambio_ok.append(p)
            elif p in IRIS_INTACTO:
                iris_roto.append(p)
            else:
                dif_inesperado.append(p)
    for p in cambio_ok:
        ok(f'cambio ESPERADO: {p} (la base limpia / el párpado compuesto)')
    for p in iris_roto:
        mal(f'EL IRIS CAMBIÓ y no debía: {p} (el disco .84 aprobado)')
    if not iris_roto:
        ok('Iris/IrisRojo BYTE-IDÉNTICOS a .84 (la capa aprobada, intacta)')
    if not dif_inesperado:
        ok('el resto byte-idéntico a .84 (hjson, TODO lo demás)')
    else:
        for p in dif_inesperado:
            mal(f'entrada diverge de .84 SIN permiso: {p}')

    print(f'=== 4. LAS 5 TEXTURAS DEL GRIMORIO HAMBRIENTO ===')
    nuevas = [
        'GrimorioHambriento', 'GrimorioHambriento_Iris', 'GrimorioHambriento_IrisRojo',
        'GrimorioHambriento_Medio', 'GrimorioHambriento_Cerrado',
    ]
    rutas = {}
    for n in nuevas:
        hit = [p for p in blobs if p.endswith(f'/{n}.rawimg') or p == f'{n}.rawimg']
        if hit:
            rutas[n] = hit[0]
            ok(f'{hit[0]} ({len(blobs[hit[0]])} B inflado)')
        else:
            mal(f'FALTA {n}.rawimg')
    for n in ('GrimorioHambriento_Rojo_Medio', 'GrimorioHambriento_Rojo_Cerrado'):
        if any(p.endswith(f'/{n}.rawimg') for p in blobs):
            mal(f'{n} sigue en el paquete — debía desaparecer')
        else:
            ok(f'{n} AUSENTE (el libro no se tiñe de rojo)')

    print(f'=== 4b. LA BLANCURA DEL SOCKET EMPAQUETADO (la letra del usuario) ===')
    import numpy as np

    def rawimg_rgba(b):
        """rawimg tML: 12 B (versión, ancho, alto) + RGBA crudo."""
        v, w, h = struct.unpack('<III', b[:12])
        assert v == 1, f'versión rawimg {v}'
        a = np.frombuffer(b[12:12 + w * h * 4], dtype=np.uint8).reshape(h, w, 4)
        assert len(b) == 12 + w * h * 4, f'{len(b)} != {12 + w * h * 4}'
        return a, w, h

    if 'GrimorioHambriento' in rutas:
        base, w, h = rawimg_rgba(blobs[rutas['GrimorioHambriento']])
        if (w, h) == (36, 49):
            ok(f'base 36×49')
        else:
            mal(f'base {w}×{h} (esperaba 36×49)')
        # OJO = (19.09, 22.45): el socket debe ser BLANCO y sin sombra
        ox, oy = 19.09, 22.45
        yy, xx = np.mgrid[0:h, 0:w]
        dist = np.sqrt((xx - ox) ** 2 + (yy - oy) ** 2)
        vis = base[..., 3] > 0
        zona = (dist < 3.0) & vis
        lum = base[..., :3].astype(int).mean(axis=2)
        if zona.sum():
            media = float(lum[zona].mean())
            oscuros = int((lum[zona] < 150).sum())
            if media >= 190:
                ok(f'socket BLANCO: luminancia media {media:.0f} en r<3 del ojo')
            else:
                mal(f'socket con sombra: luminancia {media:.0f} (la .82 daba ~165 con ruido σ54)')
            if oscuros == 0:
                ok(f'0 píxeles oscuros (<150) en el socket (sin sombra del iris)')
            else:
                mal(f'{oscuros} píxeles oscuros en el socket — la sombra sigue ahí')
        # comparación directa contra la base .84 (la sombra de antes)
        rprev = [p for p in blobs_prev if p.endswith('/GrimorioHambriento.rawimg')]
        if rprev:
            b84, _, _ = rawimg_rgba(blobs_prev[rprev[0]])
            lum84 = b84[..., :3].astype(int).mean(axis=2)
            zona84 = (dist < 3.0) & (b84[..., 3] > 0)
            m84 = float(lum84[zona84].mean())
            ok(f'referencia .84 (el relleno ruidoso): luminancia {m84:.0f} → ahora {media:.0f}'
               if zona.sum() else 'sin zona')
    # el iris: circularidad heredada de la .84 (intacto)
    for n in ('GrimorioHambriento_Iris', 'GrimorioHambriento_IrisRojo'):
        if n not in rutas:
            continue
        a, w, h = rawimg_rgba(blobs[rutas[n]])
        alfa = a[..., 3]
        esquinas = int(alfa[0, 0]) + int(alfa[0, -1]) + int(alfa[-1, 0]) + int(alfa[-1, -1])
        if w == 32 and h == 32 and esquinas == 0:
            ok(f'{n}: 32×32, esquinas α=0 (disco circular intacto)')
        else:
            mal(f'{n}: {w}×{h} esquinas α={esquinas}')

    print(f'=== 4c. EL ROJO SOLO EN EL IRIS (la DLL) ===')
    dll = None
    for p, b in blobs.items():
        if p.endswith('.dll'):
            dll = b
            break
    if dll is None:
        mal('sin DLL en el paquete')
        return
    for s in ('GrimorioHambriento_Rojo_Medio', 'GrimorioHambriento_Rojo_Cerrado'):
        u16 = s.encode('utf-16-le')
        if u16 in dll:
            mal(f'la DLL aún menciona «{s}» — el libro seguía tiñéndose')
        else:
            ok(f'la DLL ya NO menciona «{s}»')
    if 'GrimorioHambriento_IrisRojo'.encode('utf-16-le') in dll:
        ok('la DLL mantiene «GrimorioHambriento_IrisRojo» (la capa del iris rojo)')
    else:
        mal('la DLL perdió «GrimorioHambriento_IrisRojo»')
    # constantes OJO nuevas: 19.09f y 22.45f como ldc.r4 en el IL
    for fval, etiqueta in ((19.09, 'OJO.X=19.09f'), (22.45, 'OJO.Y=22.45f')):
        patrón = struct.pack('<f', fval)
        if patrón in dll:
            ok(f'constante {etiqueta} presente en el IL')
        else:
            mal(f'constante {etiqueta} AUSENTE en el IL')
    for fval, etiqueta in ((19.27, 'OJO viejo 19.27f'), (22.70, 'OJO viejo 22.70f')):
        patrón = struct.pack('<f', fval)
        if patrón not in dll:
            ok(f'{etiqueta} ya no está (recentrado)')
        else:
            print(f'  · {etiqueta} aún aparece en el IL (puede ser otra constante — revisar)')

    print(f'=== 5. DLL — ENTREGA + IRIS EN TODAS PARTES (Cecil) ===')
    import os
    tmpdll = '/tmp/dll_v65085.dll'
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
    destino = '/home/sync/AethonMod-v6.50.85.tmod'
    shutil.copy2(TMOD, destino)
    md5b = hashlib.md5(open(destino, 'rb').read()).hexdigest()
    if md5b == md5:
        ok(f'/home/sync/AethonMod-v6.50.85.tmod ({len(d)} B, md5 idéntico)')
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
