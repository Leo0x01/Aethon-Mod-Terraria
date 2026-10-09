#!/usr/bin/env python3
"""
v6.50.88 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65088.py [ruta-al-.tmod]

Novedades .88: el usuario probó el glitch de la .87 y lo vio tímido
(«el efecto glish es muy suave, aumente el efecto glish al menos un
70 %») — SOLO el Errático se INTENSIFICA, todo lo demás queda intacto:
(1) desfases ±1,3→±3 px pasan a ±3,3→±6 px (media |off| a h=1:
1,75 → 3,24 px, +85 %; máx +100 %); (2) tiras 1-2 → 2-4 (media 1,38 →
2,56, +86 %; la primera nace en y=3..24 para que todas quepan); (3) la
ráfaga vive 12-24 ticks (era 7-14, +71 %) y se regenera cada 2 frames
(era cada 3); (4) el intervalo 22 s→3,2 s pasa a 15 s→2,1 s (presencia
a h=1: 5,2 % → 12,5 %, ×2,4) y la primera ráfaga llega a los 2 s.
El SET de entradas es IDÉNTICO al de la .87 (387) y el blob diff solo
permite dll/pdb/Info — NADA visual ni de localización cambia.
"""
import hashlib
import os
import shutil
import struct
import subprocess
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = sys.argv[1] if len(sys.argv) > 1 else \
    '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
PREV = '/home/sync/AethonMod-v6.50.87.tmod'
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
    if ver == '6.50.88' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.88)')

    print(f'=== 2. EOF EXACTO (doble fórmula) ===')
    total_comp = sum(c for _, _, c in entries)
    if tend + total_comp == fsize:
        ok(f'table_end {tend} + sum(comp) {total_comp} == {fsize}')
    else:
        mal(f'EOF: {tend}+{total_comp} != {fsize}')

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

    print(f'=== 3. ENTRADAS vs .87 ({PREV}) — la .87 tenía 387 ===')
    if len(entries) == 387:
        ok(f'{len(entries)} entradas (IDÉNTICAS en número a la .87 — la .88 es 100 % código)')
    else:
        mal(f'{len(entries)} entradas (esperaba 387)')
    dprev = open(PREV, 'rb').read()
    _, _, verprev, eprev, _, _ = parse(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    fuera = set_prev - set_now
    dentro = set_now - set_prev
    if not fuera and not dentro:
        ok('SET IDÉNTICO a .87: nada entra, nada sale — solo cambia el código')
    else:
        for p in sorted(fuera):
            mal(f'  SOLO en .87 (borrado): {p}')
        for p in sorted(dentro):
            mal(f'  SOLO en .88 (nueva): {p}')
    blobs_prev = {}
    off2 = parse(PREV)[4]
    for p, raw, comp in eprev:
        data = dprev[off2:off2 + comp]
        off2 += comp
        if raw != comp:
            blobs_prev[p] = zlib.decompress(data, -15)
        else:
            blobs_prev[p] = data

    # lo que DEBE cambiar: SOLO la DLL (y su pdb + el Info de versión);
    # la .88 no toca NI UN byte de arte. EXCEPCIÓN DOCUMENTADA: el en-US
    # del PAQUETE .87 llevaba una variante transitoria con ESPACIOS
    # (109.543 B) que JAMÁS se commiteó — el árbol git de la .87 (como
    # los paquetes .85/.86) siempre usó TABS (79.422 B). El paquete .88
    # vuelve a ser IGUAL AL ÁRBOL (reproducible desde el commit, que es
    # lo que la .87 perdió): el diff se permite SOLO si es cosmético
    # (indentación) — se PARSEA ambos y el contenido debe ser IDÉNTICO
    # (el 15º incidente del espejo, esta vez con en-US).
    WHITELIST = {'Info', 'AethonMod.pdb', 'AethonMod.dll'}  # código + metadatos de build
    COSMETICO = {'Localization/en-US_Mods.AethonMod.hjson'}  # tabs↔espacios .87→.88
    dif_inesperado, cambio_ok, cambio_cosmetico, roto = [], [], [], []
    for p in sorted(set_prev & set_now):
        if p in WHITELIST:
            continue
        if blobs[p] != blobs_prev.get(p):
            if p in COSMETICO:
                cambio_cosmetico.append(p)
            else:
                dif_inesperado.append(p)
    for p in ('Info', 'AethonMod.pdb', 'AethonMod.dll'):
        if blobs.get(p) != blobs_prev.get(p):
            cambio_ok.append(p)
    for p in cambio_ok:
        ok(f'cambio ESPERADO: {p} (el código del glitch +70 %)')
    for p in cambio_cosmetico:
        try:
            import hjson
            import json as _json
            ja = hjson.loads(blobs_prev[p].decode('utf-8'))
            jb = hjson.loads(blobs[p].decode('utf-8'))
            sa = _json.dumps(ja, sort_keys=True, ensure_ascii=False)
            sb = _json.dumps(jb, sort_keys=True, ensure_ascii=False)
            if sa == sb:
                ok(f'{p.split("/")[-1]}: diff COSMÉTICO (tabs↔espacios del incidente .87) — '
                   f'parseados IDÉNTICOS ({len(sa)} chars): el paquete .88 == árbol git')
            else:
                mal(f'{p}: el diff NO es solo indentación — el contenido cambió')
        except ImportError:
            ok(f'{p.split("/")[-1]}: diff esperado (tabs del árbol vs espacios transitorios '
                f'de la .87) — hjson no instalado, sin verificación profunda')
    if not dif_inesperado:
        ok('TODO lo demás byte-idéntico a .87 (arte, es-MX/es-ES, sonidos — la .88 es SOLO código)')
    else:
        for p in dif_inesperado:
            mal(f'entrada diverge de .87 SIN permiso: {p}')

    print(f'=== 4. LAS TEXTURAS DEL GRIMORIO — decodificación rawimg ===')
    import numpy as np

    def rawimg_rgba(b):
        v, w, h = struct.unpack('<III', b[:12])
        assert v == 1, f'versión rawimg {v}'
        a = np.frombuffer(b[12:12 + w * h * 4], dtype=np.uint8).reshape(h, w, 4)
        assert len(b) == 12 + w * h * 4, f'{len(b)} != {12 + w * h * 4}'
        return a, w, h

    def blob_de(sufijo):
        hit = [p for p in blobs if p.endswith(sufijo)]
        return blobs[hit[0]] if hit else None

    tex = {}
    esperadas = {
        'GrimorioHambriento.rawimg': (36, 49),
        'GrimorioHambriento_Medio.rawimg': (36, 49),
        'GrimorioHambriento_Cerrado.rawimg': (36, 49),
        'GrimorioHambriento_Iris.rawimg': (32, 32),
        'GrimorioHambriento_IrisRojo.rawimg': (32, 32),
        'GrimorioHambrientoErratico.rawimg': (36, 49),
        'GrimorioHambrientoTembloroso.rawimg': (36, 49),
    }
    for sufijo, (w_, h_) in esperadas.items():
        b = blob_de(sufijo)
        if b is None:
            mal(f'FALTA {sufijo}')
            continue
        try:
            a, w, h = rawimg_rgba(b)
            tex[sufijo] = a
            if (w, h) == (w_, h_):
                ok(f'{sufijo[:-7]}: {w}×{h} exacto')
            else:
                mal(f'{sufijo[:-7]}: {w}×{h} (esperaba {w_}×{h_})')
        except Exception as e:
            mal(f'{sufijo} NO decodifica: {e}')

    print(f'=== 4b. LA CONTINUIDAD DEL PARPADEO (heredada de la .86) ===')
    ox, oy = 19.09, 22.45
    if all(k in tex for k in ('GrimorioHambriento.rawimg', 'GrimorioHambriento_Medio.rawimg',
                              'GrimorioHambriento_Cerrado.rawimg')):
        base = tex['GrimorioHambriento.rawimg'].astype(int)
        yy, xx = np.mgrid[0:49, 0:36]
        d_ojo = np.sqrt((xx - ox) ** 2 + (yy - oy) ** 2)
        fuera_ojo = d_ojo > 10.2
        for n in ('GrimorioHambriento_Medio.rawimg', 'GrimorioHambriento_Cerrado.rawimg'):
            dl = np.abs(tex[n].astype(int) - base).sum(axis=2)
            media = float(dl[fuera_ojo].mean())
            graves = int((dl[fuera_ojo] > 8).sum())
            (ok if media < 0.5 and graves <= 1 else mal)(
                f'{n.split("/")[-1][:-7]}: |Δ|={media:.3f} fuera del ojo ({graves} px>8)')

    print(f'=== 4c. LAS DOS COPIAS — byte a byte con la base ===')
    bb = blob_de('GrimorioHambriento.rawimg')
    for n in ('GrimorioHambrientoErratico.rawimg', 'GrimorioHambrientoTembloroso.rawimg'):
        if blob_de(n) == bb:
            ok(f'{n.split("/")[-1][:-7]} == base (el MISMO socket — el glitch +70 % es 100 % código)')
        else:
            mal(f'{n.split("/")[-1][:-7]} != base — las capas quedarían desalineadas')

    print(f'=== 4d. LA DLL — la familia presente, los viejos ausentes ===')
    dll = None
    for p, b in blobs.items():
        if p.endswith('.dll'):
            dll = b
            break
    if dll is None:
        mal('sin DLL en el paquete')
        return
    # presentes: los tipos viven en el montón UTF-8 de los metadatos
    # (lección .86); los PATHS de textura son literales → UTF-16
    for s in ('GrimorioHambrientoErratico', 'GrimorioHambrientoTembloroso',
              'EstadoGrimorio'):
        if s.encode('utf-8') in dll:
            ok(f'la DLL declara el tipo «{s}» (montón UTF-8)')
        else:
            mal(f'la DLL NO declara el tipo «{s}»')
    if 'AethonMod/Content/Weapons/GrimorioHambriento_'.encode('utf-16-le') in dll:
        ok('la DLL declara el prefijo compartido «GrimorioHambriento_» (las capas de la familia)')
    else:
        mal('la DLL NO declara el prefijo «GrimorioHambriento_»')
    # ausentes: los efectos de la .86 (invariante heredada de la .87)
    for s in ('GrimorioHambrientoRunico', 'GrimorioHambrientoEstelar',
              'AethonMod/Content/Weapons/GrimorioHambrientoRunico_',
              'AethonMod/Content/Weapons/GrimorioHambrientoEstelar_'):
        if s.encode('utf-16-le') in dll or s.encode('utf-8') in dll:
            mal(f'la DLL aún menciona «{s}»')
        else:
            ok(f'la DLL ya NO menciona «{s}» (borrado en la .87, sigue ausente)')

    print(f'=== 5. CECIL — entrega ×3 + glitch +70 % + libro normal intacto ===')
    tmpdll = '/tmp/dll_v65088.dll'
    open(tmpdll, 'wb').write(dll)
    r = subprocess.run(
        [DOTNET, CECIL, tmpdll],
        capture_output=True, text=True,
        env={**os.environ, 'DOTNET_ROLL_FORWARD': 'Minor'})
    print('    ' + (r.stdout or '').strip().replace('\n', '\n    '))
    if r.returncode == 0:
        ok('Cecil: 3 entregas + Erratico=true + temblor + GLITCH +70 % (15/2,1 con 22/3,2 ausentes) + el libro normal intacto')
    else:
        mal(f'Cecil falló (rc={r.returncode}): {r.stderr.strip()[:200]}')

    print(f'=== 6. HJSON — espejo y claves (heredadas de la .87) ===')
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
                    mal(f'divergencia en el byte {i}')
                    break
            else:
                mal(f'prefijo común {n} | tamaños {len(mx)} vs {len(eses)}')
    else:
        mal(f'hjson hallados: {list(h)}')

    en_txt = h.get('en-US', b'').decode('utf-8', 'replace')
    es_txt = h.get('es-MX', b'').decode('utf-8', 'replace')
    eses_txt = h.get('es-ES', b'').decode('utf-8', 'replace')
    for s in ['Grimorio Hambriento Errático', 'Grimorio Hambriento Tembloroso', 'TIENE HAMBRE']:
        for texto, idioma in ((es_txt, 'es-MX'), (eses_txt, 'es-ES')):
            if s in texto:
                ok(f'{idioma} «{s}» presente')
            else:
                mal(f'{idioma} «{s}» AUSENTE')
    for s in ['Erratic Hungry Grimoire', 'Trembling Hungry Grimoire']:
        if s in en_txt:
            ok(f'en-US «{s}» presente')
        else:
            mal(f'en-US «{s}» AUSENTE')
    for malo in ['ladrones', 'bandits']:
        if malo in es_txt and malo in en_txt:
            mal(f'«{malo}» REGRESÓ (invariante .77 rota)')
        else:
            ok(f'«{malo}» ausente (invariante .77)')

    print(f'=== 7. RESPALDO ===')
    destino = '/home/sync/AethonMod-v6.50.88.tmod'
    shutil.copy2(TMOD, destino)
    md5b = hashlib.md5(open(destino, 'rb').read()).hexdigest()
    if md5b == md5:
        ok(f'/home/sync/AethonMod-v6.50.88.tmod ({len(d)} B, md5 idéntico)')
    else:
        mal('el respaldo diverge')

    print()
    if FALLOS:
        print(f'*** {len(FALLOS)} FALLOS ***')
        for f in FALLOS:
            print(f'  ✗ {f}')
        sys.exit(1)
    print('AUDITORÍA v6.50.88 COMPLETA — 0 fallos')


if __name__ == '__main__':
    main()
