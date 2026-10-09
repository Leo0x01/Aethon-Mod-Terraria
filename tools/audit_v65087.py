#!/usr/bin/env python3
"""
v6.50.87 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65087.py [ruta-al-.tmod]

Novedades .87: el usuario devolvió las animaciones de la .86 («se ven
bastante mal y exageradas») — LOS EFECTOS VIEJOS SE BORRAN (runas, aura,
estrellas, venas: 10 rawimg fuera) y las dos copias RENACEN con efectos
SUAVES: Errático (glitch en ráfagas + mirada errática) y Tembloroso
(dos senos por eje, 0,2 → 1,3 px). (3) blob diff contra .86: SOLO
cambian los 3 hjson (claves nuevas) + dll/pdb/Info (whitelist) — NADA
visual cambia: base/Medio/Cerrado/Iris/IrisRojo y las copias quedan
BYTE-IDÉNTICAS (la .87 es 100 % código); (4c) las nuevas copias ==
base byte a byte; (4d) la DLL ya no menciona Runico/Estelar/Runa/
Estrellas/Venas; (5) CECIL v65087: entrega ×3 + Erratico=true + senos
del temblor + máquina de glitch + PreDraw false + el libro NORMAL
intacto (PostDrawInWorld → Core).
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
PREV = '/home/sync/AethonMod-v6.50.86.tmod'
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
    if ver == '6.50.87' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.87)')

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

    print(f'=== 3. ENTRADAS vs .86 ({PREV}) — la .86 tenía 397 ===')
    if len(entries) == 387:
        ok(f'{len(entries)} entradas (397 − 12 efectos viejos + 2 copias renombradas)')
    else:
        mal(f'{len(entries)} entradas (esperaba 387)')
    dprev = open(PREV, 'rb').read()
    _, _, verprev, eprev, _, _ = parse(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    # FUERA: los 12 efectos de la .86 (2 copias viejas + 10 texturas de efecto)
    esperadas_fuera = {
        'Content/Weapons/GrimorioHambrientoRunico.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar.rawimg',
        'Content/Weapons/GrimorioHambrientoRunico_RunaA.rawimg',
        'Content/Weapons/GrimorioHambrientoRunico_RunaB.rawimg',
        'Content/Weapons/GrimorioHambrientoRunico_RunaC.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar_Estrellas1.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar_Estrellas2.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar_Estrellas3.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar_Venas1.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar_Venas2.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar_Venas3.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar_Venas4.rawimg',
    }
    # DENTRO: las dos copias renacidas (mismo arte: los efectos son 100 % código)
    esperadas_dentro = {
        'Content/Weapons/GrimorioHambrientoErratico.rawimg',
        'Content/Weapons/GrimorioHambrientoTembloroso.rawimg',
    }
    fuera = set_prev - set_now
    dentro = set_now - set_prev
    if fuera == esperadas_fuera and dentro == esperadas_dentro:
        ok('SET: salen EXACTAMENTE los 12 efectos de la .86, entran las 2 copias nuevas')
    else:
        for p in sorted(fuera):
            (ok if p in esperadas_fuera else mal)(f'  SOLO en .86 (borrado): {p}')
        for p in sorted(dentro):
            (ok if p in esperadas_dentro else mal)(f'  SOLO en .87 (nueva): {p}')
    blobs_prev = {}
    off2 = parse(PREV)[4]
    for p, raw, comp in eprev:
        data = dprev[off2:off2 + comp]
        off2 += comp
        if raw != comp:
            blobs_prev[p] = zlib.decompress(data, -15)
        else:
            blobs_prev[p] = data

    # lo que DEBE cambiar: SOLO los 3 hjson (claves nuevas de las copias);
    # NADA visual cambia — la .87 es 100 % código: TODO el arte queda intacto
    ESPERADO_CAMBIO = {
        'Localization/en-US_Mods.AethonMod.hjson',
        'Localization/es-MX_Mods.AethonMod.hjson',
        'Localization/es-ES_Mods.AethonMod.hjson',
    }
    INTACTO = {
        'Content/Weapons/GrimorioHambriento.rawimg',
        'Content/Weapons/GrimorioHambriento_Medio.rawimg',
        'Content/Weapons/GrimorioHambriento_Cerrado.rawimg',
        'Content/Weapons/GrimorioHambriento_Iris.rawimg',
        'Content/Weapons/GrimorioHambriento_IrisRojo.rawimg',
    }
    WHITELIST = {'Info', 'AethonMod.pdb', 'AethonMod.dll'}  # metadatos/código de build
    dif_inesperado, cambio_ok, roto = [], [], []
    for p in sorted(set_prev & set_now):
        if p in WHITELIST:
            continue
        if blobs[p] != blobs_prev.get(p):
            if p in ESPERADO_CAMBIO:
                cambio_ok.append(p)
            elif p in INTACTO:
                roto.append(p)
            else:
                dif_inesperado.append(p)
    for p in cambio_ok:
        ok(f'cambio ESPERADO: {p.split("/")[-1]} (claves nuevas — el espejo se regeneró)')
    for p in roto:
        mal(f'CAMBIÓ y no debía: {p} (la capa aprobada)')
    if not roto:
        ok('base/Medio/Cerrado/Iris/IrisRojo BYTE-IDÉNTICOS a .86 — el libro NORMAL intacto')
    if not dif_inesperado:
        ok('el resto byte-idéntico a .86 (los efectos son 100 % código)')
    else:
        for p in dif_inesperado:
            mal(f'entrada diverge de .86 SIN permiso: {p}')

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

    print(f'=== 4c. LAS COPIAS NUEVAS — byte a byte con la base ===')
    bb = blob_de('GrimorioHambriento.rawimg')
    for n in ('GrimorioHambrientoErratico.rawimg', 'GrimorioHambrientoTembloroso.rawimg'):
        if blob_de(n) == bb:
            ok(f'{n.split("/")[-1][:-7]} == base (el MISMO socket — los efectos son 100 % código)')
        else:
            mal(f'{n.split("/")[-1][:-7]} != base — las capas quedarían desalineadas')

    print(f'=== 4d. LA DLL — los efectos viejos AUSENTES, los nuevos presentes ===')
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
    # ausentes: los efectos de la .86. Solo por PREFIJO DE RUTA completo —
    # las palabras sueltas viven en OTROS ítems legítimos (TopeEstrellas
    # del Telar, EstrellasCamino del RiftLib, CienpiesRunicoStaff del panteón)
    for s in ('GrimorioHambrientoRunico', 'GrimorioHambrientoEstelar',
              'AethonMod/Content/Weapons/GrimorioHambrientoRunico_',
              'AethonMod/Content/Weapons/GrimorioHambrientoEstelar_'):
        if s.encode('utf-16-le') in dll or s.encode('utf-8') in dll:
            mal(f'la DLL aún menciona «{s}»')
        else:
            ok(f'la DLL ya NO menciona «{s}» (borrado)')
    for s in ('RunaA', 'RunaB', 'RunaC', 'Estrellas', 'Venas'):
        ruta = f'AethonMod/Content/Weapons/GrimorioHambriento{"Runico_" if s.startswith("Runa") else "Estelar_"}{s}'
        if ruta.encode('utf-16-le') in dll:
            mal(f'la DLL aún menciona la ruta «{ruta}»')
        else:
            ok(f'la DLL ya NO menciona la ruta «{s}» (borrado)')

    print(f'=== 5. CECIL — entrega ×3 + efectos suaves + libro normal intacto ===')
    tmpdll = '/tmp/dll_v65087.dll'
    open(tmpdll, 'wb').write(dll)
    r = subprocess.run(
        [DOTNET, CECIL, tmpdll],
        capture_output=True, text=True,
        env={**os.environ, 'DOTNET_ROLL_FORWARD': 'Minor'})
    print('    ' + (r.stdout or '').strip().replace('\n', '\n    '))
    if r.returncode == 0:
        ok('Cecil: 3 entregas + Erratico=true + temblor + máquina de glitch + el libro normal intacto')
    else:
        mal(f'Cecil falló (rc={r.returncode}): {r.stderr.strip()[:200]}')

    print(f'=== 6. HJSON — espejo y claves nuevas ===')
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
    # los nombres viejos de la .86 deben ser AUSENTES (los ítems renacieron)
    for s in ['Grimorio Hambriento Rúnico', 'Grimorio Hambriento Estelar',
              'Runic Hungry Grimoire', 'Stellar Hungry Grimoire']:
        idioma = 'en-US' if s[0].isascii() and not s[0].isupper() or 'Hungry' in s else 'es'
        texto = en_txt if 'Runic' in s or 'Stellar' in s else (es_txt if 'Rúnico' in s or 'Estelar' in s else '')
        hallado_en = s in en_txt
        hallado_es = s in es_txt or s in eses_txt
        if hallado_en or hallado_es:
            mal(f'«{s}» SIGUE VIVO (debia morir con la .86)')
        else:
            ok(f'«{s}» AUSENTE (la copia renació con otro nombre)')
    for malo in ['ladrones', 'bandits']:
        if malo in es_txt and malo in en_txt:
            mal(f'«{malo}» REGRESÓ (invariante .77 rota)')
        else:
            ok(f'«{malo}» ausente (invariante .77)')

    print(f'=== 7. RESPALDO ===')
    destino = '/home/sync/AethonMod-v6.50.87.tmod'
    shutil.copy2(TMOD, destino)
    md5b = hashlib.md5(open(destino, 'rb').read()).hexdigest()
    if md5b == md5:
        ok(f'/home/sync/AethonMod-v6.50.87.tmod ({len(d)} B, md5 idéntico)')
    else:
        mal('el respaldo diverge')

    print()
    if FALLOS:
        print(f'*** {len(FALLOS)} FALLOS ***')
        for f in FALLOS:
            print(f'  ✗ {f}')
        sys.exit(1)
    print('AUDITORÍA v6.50.87 COMPLETA — 0 fallos')


if __name__ == '__main__':
    main()
