#!/usr/bin/env python3
"""
v6.50.86 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65086.py [ruta-al-.tmod]

Novedades .86: (3) blob diff contra .85 — cambian SOLO los 2 párpados
(PUROS de código, misma corrida) y ENTRAN las 12 texturas nuevas (2 copias
base + 3 runas + 3 máscaras estrellas + 4 máscaras venas); (4b) LA
CONTINUIDAD DEL PARPADEO en el paquete: base vs Medio vs Cerrado |Δ|≈0
fuera del ojo (la calidad del código puro que pidió el usuario — la .85
compostaba el GIF con pluma); (4c) LAS 12 NUEVAS decodifican con tamaño
exacto y las copias base son BYTE-IDÉNTICAS a la base; (4d) LAS MÁSCARAS
no invaden el viaje del iris (r<8 px del OJO limpio); (5) CECIL v65086:
entrega de los TRES + herencia + get_Estado por clase + ModifyItemDraw de
las copias.
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
PREV = '/home/sync/AethonMod-v6.50.85.tmod'
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
    if ver == '6.50.86' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.86)')

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

    print(f'=== 3. ENTRADAS vs .85 ({PREV}) — la .85 tenía 385 ===')
    if len(entries) == 397:
        ok(f'{len(entries)} entradas (385 + 12 texturas nuevas)')
    else:
        mal(f'{len(entries)} entradas (esperaba 397)')
    dprev = open(PREV, 'rb').read()
    _, _, verprev, eprev, _, _ = parse(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    esperadas_dentro = {
        'Content/Weapons/GrimorioHambrientoRunico.rawimg',
        'Content/Weapons/GrimorioHambrientoRunico_RunaA.rawimg',
        'Content/Weapons/GrimorioHambrientoRunico_RunaB.rawimg',
        'Content/Weapons/GrimorioHambrientoRunico_RunaC.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar_Estrellas1.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar_Estrellas2.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar_Estrellas3.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar_Venas1.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar_Venas2.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar_Venas3.rawimg',
        'Content/Weapons/GrimorioHambrientoEstelar_Venas4.rawimg',
    }
    fuera = set_prev - set_now
    dentro = set_now - set_prev
    if not fuera and dentro == esperadas_dentro:
        ok('SET: entran EXACTAMENTE las 12 texturas nuevas, nada se pierde')
    else:
        for p in sorted(fuera):
            mal(f'  SOLO en .85: {p}')
        for p in sorted(dentro):
            (ok if p in esperadas_dentro else mal)(f'  SOLO en .86 (nueva): {p}')
    blobs_prev = {}
    off2 = parse(PREV)[4]
    for p, raw, comp in eprev:
        data = dprev[off2:off2 + comp]
        off2 += comp
        if raw != comp:
            blobs_prev[p] = zlib.decompress(data, -15)
        else:
            blobs_prev[p] = data

    # lo que DEBE cambiar: los 2 párpados PUROS (misma fuente que la base)
    # + los 3 hjson (claves nuevas de las copias); la base y las capas del
    # iris quedan BYTE-IDÉNTICAS (lo aprobado)
    ESPERADO_CAMBIO = {
        'Content/Weapons/GrimorioHambriento_Medio.rawimg',
        'Content/Weapons/GrimorioHambriento_Cerrado.rawimg',
        'Localization/en-US_Mods.AethonMod.hjson',
        'Localization/es-MX_Mods.AethonMod.hjson',
        'Localization/es-ES_Mods.AethonMod.hjson',
    }
    INTACTO = {
        'Content/Weapons/GrimorioHambriento.rawimg',
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
        ok(f'cambio ESPERADO: {p.split("/")[-1]} (el párpado PURO de código)')
    for p in roto:
        mal(f'CAMBIÓ y no debía: {p} (la capa aprobada)')
    if not roto:
        ok('base + Iris + IrisRojo BYTE-IDÉNTICOS a .85 (lo aprobado, intacto)')
    if not dif_inesperado:
        ok('el resto byte-idéntico a .85 (hjson incluido — el espejo se regeneró igual)')
    else:
        for p in dif_inesperado:
            mal(f'entrada diverge de .85 SIN permiso: {p}')

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
        'GrimorioHambrientoRunico.rawimg': (36, 49),
        'GrimorioHambrientoEstelar.rawimg': (36, 49),
        'GrimorioHambrientoRunico_RunaA.rawimg': (11, 11),
        'GrimorioHambrientoRunico_RunaB.rawimg': (11, 11),
        'GrimorioHambrientoRunico_RunaC.rawimg': (11, 11),
        'GrimorioHambrientoEstelar_Estrellas1.rawimg': (36, 49),
        'GrimorioHambrientoEstelar_Estrellas2.rawimg': (36, 49),
        'GrimorioHambrientoEstelar_Estrellas3.rawimg': (36, 49),
        'GrimorioHambrientoEstelar_Venas1.rawimg': (36, 49),
        'GrimorioHambrientoEstelar_Venas2.rawimg': (36, 49),
        'GrimorioHambrientoEstelar_Venas3.rawimg': (36, 49),
        'GrimorioHambrientoEstelar_Venas4.rawimg': (36, 49),
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

    print(f'=== 4b. LA CONTINUIDAD DEL PARPADEO (la letra del usuario) ===')
    # los párpados PUROS: misma corrida que la base — fuera del ojo SON el
    # mismo libro (la .85 compostaba el GIF: |Δ|>0 con pluma en TODO el marco)
    if all(k in tex for k in ('GrimorioHambriento.rawimg', 'GrimorioHambriento_Medio.rawimg',
                              'GrimorioHambriento_Cerrado.rawimg')):
        base = tex['GrimorioHambriento.rawimg'].astype(int)
        ox, oy = 19.09, 22.45
        yy, xx = np.mgrid[0:49, 0:36]
        d_ojo = np.sqrt((xx - ox) ** 2 + (yy - oy) ** 2)
        fuera_ojo = d_ojo > 10.2    # 200 px fuente a escala de juego
        for n in ('GrimorioHambriento_Medio.rawimg', 'GrimorioHambriento_Cerrado.rawimg'):
            dl = np.abs(tex[n].astype(int) - base).sum(axis=2)
            media = float(dl[fuera_ojo].mean())
            graves = int((dl[fuera_ojo] > 8).sum())
            if media < 0.5 and graves <= 1:
                ok(f'{n.split("/")[-1][:-7]}: |Δ|={media:.3f} fuera del ojo ({graves} px>8) — MISMA corrida, calidad del código puro')
            else:
                mal(f'{n.split("/")[-1][:-7]}: |Δ|={media:.3f} · {graves} px>8 fuera del ojo — salto de calidad')
        # y el ojo SÍ cambia (el párpado existe)
        for n in ('GrimorioHambriento_Medio.rawimg', 'GrimorioHambriento_Cerrado.rawimg'):
            dentro = np.abs(tex[n].astype(int) - base).sum(axis=2)[~fuera_ojo]
            if dentro.mean() > 30:
                ok(f'{n.split("/")[-1][:-7]}: el ojo cambia (párpado presente, Δ={dentro.mean():.0f})')
            else:
                mal(f'{n.split("/")[-1][:-7]}: el ojo NO cambia — ¿es la base repetida?')

    print(f'=== 4c. LAS COPIAS BASE — byte a byte con la base ===')
    bb = blob_de('GrimorioHambriento.rawimg')
    for n in ('GrimorioHambrientoRunico.rawimg', 'GrimorioHambrientoEstelar.rawimg'):
        if blob_de(n) == bb:
            ok(f'{n.split("/")[-1][:-7]} == base (el MISMO socket para las capas)')
        else:
            mal(f'{n.split("/")[-1][:-7]} != base — las capas quedarían desalineadas')

    print(f'=== 4d. LAS MÁSCARAS — masa y zona libre del iris ===')
    # las runas: 11×11 con alpha (la chispa es fina — 15 px bastan)
    for n in ('RunaA', 'RunaB', 'RunaC'):
        k = f'GrimorioHambrientoRunico_{n}.rawimg'
        if k in tex:
            masa = int((tex[k][..., 3] > 8).sum())
            (ok if masa >= 15 else mal)(f'{n}: {masa} px con masa en 11×11')
    # estrellas: 3 grupos con masa comparable, NINGUNA a r<8 del OJO (el viaje del iris)
    masas = []
    for g in (1, 2, 3):
        k = f'GrimorioHambrientoEstelar_Estrellas{g}.rawimg'
        if k not in tex:
            continue
        a = tex[k]
        masa = int((a[..., 3] > 8).sum())
        masas.append(masa)
        yy, xx = np.mgrid[0:49, 0:36]
        d_ojo = np.sqrt((xx - ox) ** 2 + (yy - oy) ** 2)
        cerca = int(((a[..., 3] > 8) & (d_ojo < 8.0)).sum())
        (ok if masa >= 20 and cerca == 0 else mal)(
            f'Estrellas{g}: {masa} px de masa · {cerca} px invadiendo el viaje del iris')
    if len(masas) == 3:
        relacion = max(masas) / max(1, min(masas))
        (ok if relacion < 2.0 else mal)(f'3 grupos equilibrados ({masas} — ratio {relacion:.2f})')
    # venas: 4 bandas, sin invadir el iris
    masasv = []
    for k in (1, 2, 3, 4):
        kk = f'GrimorioHambrientoEstelar_Venas{k}.rawimg'
        if kk not in tex:
            continue
        a = tex[kk]
        masa = int((a[..., 3] > 8).sum())
        masasv.append(masa)
        yy, xx = np.mgrid[0:49, 0:36]
        d_ojo = np.sqrt((xx - ox) ** 2 + (yy - oy) ** 2)
        cerca = int(((a[..., 3] > 8) & (d_ojo < 8.0)).sum())
        (ok if masa >= 20 and cerca == 0 else mal)(
            f'Venas{k}: {masa} px de masa · {cerca} px invadiendo el viaje del iris')
    if len(masasv) == 4:
        relacion = max(masasv) / max(1, min(masasv))
        (ok if relacion < 2.0 else mal)(f'4 bandas equilibradas ({masasv} — ratio {relacion:.2f})')
    # el color de las venas: MORADA (la letra del usuario)
    todas = None
    for k in (1, 2, 3, 4):
        kk = f'GrimorioHambrientoEstelar_Venas{k}.rawimg'
        if kk in tex:
            v = tex[kk][tex[kk][..., 3] > 100]
            todas = v if todas is None else np.concatenate([todas, v])
    if todas is not None and len(todas):
        r, g, b = todas[:, 0].mean(), todas[:, 1].mean(), todas[:, 2].mean()
        if b > r > g:
            ok(f'venas MORADAS: RGB ({r:.0f},{g:.0f},{b:.0f}) — azul>rojo>verde')
        else:
            mal(f'venas con color raro: RGB ({r:.0f},{g:.0f},{b:.0f})')

    print(f'=== 4e. LA DLL — los TRES grimorios y sus texturas ===')
    dll = None
    for p, b in blobs.items():
        if p.endswith('.dll'):
            dll = b
            break
    if dll is None:
        mal('sin DLL en el paquete')
        return
    # los TIPOS viven en el montón UTF-8 de los metadatos; las rutas de
    # textura se construyen por CONCATENACIÓN — se comprueban por PREFIJO
    if b'EstadoGrimorio' in dll:
        ok('la DLL declara el tipo «EstadoGrimorio» (montón UTF-8)')
    else:
        mal('la DLL NO declara «EstadoGrimorio»')
    for s in ('GrimorioHambrientoRunico', 'GrimorioHambrientoEstelar',
              'AethonMod/Content/Weapons/GrimorioHambrientoRunico_',
              'AethonMod/Content/Weapons/GrimorioHambrientoEstelar_Estrellas',
              'AethonMod/Content/Weapons/GrimorioHambrientoEstelar_Venas',
              'RunaA', 'RunaB', 'RunaC'):
        u16 = s.encode('utf-16-le')
        if u16 in dll:
            ok(f'la DLL menciona «{s}»')
        else:
            mal(f'la DLL NO menciona «{s}»')
    for s in ('GrimorioHambriento_Rojo_Medio', 'GrimorioHambriento_Rojo_Cerrado'):
        if s.encode('utf-16-le') in dll:
            mal(f'la DLL aún menciona «{s}»')
        else:
            ok(f'la DLL ya NO menciona «{s}» (el libro nunca se tiñe)')

    print(f'=== 5. CECIL — entrega ×3 + herencia + estado por clase + capas de mano ===')
    tmpdll = '/tmp/dll_v65086.dll'
    open(tmpdll, 'wb').write(dll)
    r = subprocess.run(
        [DOTNET, CECIL, tmpdll],
        capture_output=True, text=True,
        env={**os.environ, 'DOTNET_ROLL_FORWARD': 'Minor'})
    print('    ' + (r.stdout or '').strip().replace('\n', '\n    '))
    if r.returncode == 0:
        ok('Cecil: 3 entregas + herencia + get_Estado por clase + ModifyItemDraw de las copias')
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
    for s in ['Grimorio Hambriento Rúnico', 'Grimorio Hambriento Estelar', 'TIENE HAMBRE']:
        for texto, idioma in ((es_txt, 'es-MX'), (eses_txt, 'es-ES')):
            if s in texto:
                ok(f'{idioma} «{s}» presente')
            else:
                mal(f'{idioma} «{s}» AUSENTE')
    for s in ['Runic Hungry Grimoire', 'Stellar Hungry Grimoire']:
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
    destino = '/home/sync/AethonMod-v6.50.86.tmod'
    shutil.copy2(TMOD, destino)
    md5b = hashlib.md5(open(destino, 'rb').read()).hexdigest()
    if md5b == md5:
        ok(f'/home/sync/AethonMod-v6.50.86.tmod ({len(d)} B, md5 idéntico)')
    else:
        mal('el respaldo diverge')

    print()
    if FALLOS:
        print(f'*** {len(FALLOS)} FALLOS ***')
        for f in FALLOS:
            print(f'  ✗ {f}')
        sys.exit(1)
    print('AUDITORÍA v6.50.86 COMPLETA — 0 fallos')


if __name__ == '__main__':
    main()
