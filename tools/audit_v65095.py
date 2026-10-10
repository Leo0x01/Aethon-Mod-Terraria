#!/usr/bin/env python3
"""
v6.50.95 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65095.py [ruta-al-.tmod]

Novedades .95 — EL COMPÁS MURIÓ + LA LÁGRIMA DE TINTA (el disparo propio):
(1) «quitar la restriccion del compás de la sombra — tus dos reglas a la
    vez […] creo que mejor es dejarlo atacar cuando quiera» —
    TICKS_ENTRE_ATAQUES/TICKS_TRAS_TERMINAR/_proximoAtaque/MarcarAtaque/
    AtaqueListo/SombraTerminada/OnKill ELIMINADOS de SombraPaginaCaza; el
    espíritu lanza la sombra en cuanto hay presa (sin relojes, una presa
    por sombra).
(2) «te dare un sprite para un disparo […] seas creativo, lo animes y
    crees una nueva arma con el proyectil» — LA LÁGRIMA DE TINTA
    (LagrimaDeTintaProjectile: 4 frames respirando, homing que acelera,
    goteo, estelas, halo aditivo, salpicadura, LA MANCHA) + LA TINTA VIVA
    (TintaViva, el arma que la escupe — bolsa de sombras) + la DESCARGA
    PRESTADA MUERTA (el 931 Nightglow ya no vive en el Shoot del libro:
    escupe la lágrima).
ENTRADAS: 386 (384 de .94 + LagrimaDeTintaProjectile.rawimg +
TintaViva.rawimg). Whitelist del blob diff: dll/pdb/Info + 3 hjson + los
2 rawimg nuevos. CAMBIAN (documentado): SombraPaginaCaza, espíritu,
GrimorioHambriento, Nervioso, BolsasCategorias + LAS DOS CLASES NUEVAS.
IDÉNTICOS esperados: GrimorioFuriaSistema, OleadaNPC.
"""
import hashlib
import os
import subprocess
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = sys.argv[1] if len(sys.argv) > 1 else \
    '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
PREV = '/home/sync/AethonMod-v6.50.94.tmod'
CECIL = '/home/z/tmp_scripts/cecil_check/bin/Release/net8.0/cecil_check.dll'
DOTNET = '/tmp/sdk/dotnet'
DLLTMP = '/tmp/audit_v65095.dll'
DLLPREV = '/tmp/audit_v65095_prev.dll'

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
    if ver == '6.50.95' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.95)')

    print(f'=== 2. EOF EXACTO (doble fórmula) ===')
    total_comp = sum(c for _, _, c in entries)
    tend = parse(TMOD)[4]
    if tend + total_comp == fsize:
        ok(f'table_end {tend} + sum(comp) {total_comp} == {fsize}')
    else:
        mal(f'EOF: {tend}+{total_comp} != {fsize}')

    print(f'=== 3. ENTRADAS vs .94 — +2 (la lágrima y su arma) ===')
    _, _, verprev, eprev, _, blobs_prev = inflar(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    fuera = sorted(set_prev - set_now)
    dentro = sorted(set_now - set_prev)
    print(f'    .94: {len(eprev)} entradas | .95: {len(entries)} entradas')
    esperado_dentro = {
        'Content/Projectiles/Sombras/LagrimaDeTintaProjectile.rawimg',
        'Content/Weapons/Sombras/TintaViva.rawimg',
    }
    if len(entries) == 386 and set(dentro) == esperado_dentro and not fuera:
        ok('386 entradas — las 384 de la .94 + LA LÁGRIMA y SU ARMA')
    else:
        for p in fuera:
            mal(f'SALE inesperado: {p}')
        for p in dentro:
            if p not in esperado_dentro:
                mal(f'ENTRA inesperado: {p}')
        if len(entries) != 386:
            mal(f'entradas: {len(entries)} (esperaba 386)')

    print(f'=== 4. BLOB DIFF — whitelist exacta ===')
    cambios = []
    for p in sorted(set_prev & set_now):
        if blobs_prev[p] != blobs[p]:
            cambios.append(p)
    permitidos = ('.dll', '.pdb', 'Info')
    for p in cambios:
        if any(s in p for s in permitidos) or p.startswith('Localization/'):
            ok(f'cambia (permitido): {p} '
               f'{len(blobs_prev[p])}→{len(blobs[p])} B')
        else:
            mal(f'cambia FUERA de whitelist: {p}')
    arte_prev = {p for p in set_prev & set_now if p not in cambios}
    identicos = all(blobs_prev[p] == blobs[p] for p in arte_prev)
    if identicos:
        ok(f'arte byte-idéntico en {len(arte_prev)} entradas restantes')
    else:
        mal('arte con cambios no declarados')

    print(f'=== 5. CECIL — la DLL de la .95 vs la de la .94 ===')
    open(DLLTMP, 'wb').write(blobs['AethonMod.dll'])
    open(DLLPREV, 'wb').write(blobs_prev['AethonMod.dll'])

    # (a) EL COMPÁS MURIÓ — SombraPaginaCaza sin relojes
    dmp = cecil_dump(DLLTMP, 'SombraPaginaCaza')
    for muerto in ('TICKS_ENTRE_ATAQUES', 'TICKS_TRAS_TERMINAR',
                   '_proximoAtaque', 'MarcarAtaque', 'AtaqueListo',
                   'SombraTerminada', '::OnKill('):
        if muerto in dmp:
            mal(f'SombraPaginaCaza.{muerto} VIVE — el compás tenía que morir')
        else:
            ok(f'SombraPaginaCaza.{muerto} ELIMINADO')
    for vivo in ('CADENCIA_MORDIDA', '::AI(', '::PreDraw(', '::AbsorberPresa'):
        if vivo in dmp:
            ok(f'SombraPaginaCaza{vivo} sigue presente')
        else:
            mal(f'SombraPaginaCaza{vivo} AUSENTE')
    dmp_prev = cecil_dump(DLLPREV, 'SombraPaginaCaza')
    if dmp != dmp_prev:
        ok('SombraPaginaCaza CAMBIA (esperado: sin compás)')
    else:
        mal('SombraPaginaCaza NO cambió — ¿dónde murió el compás?')

    # (b) EL ESPÍRITU lanza sin relojes pero calla mientras el dueño dispara
    dmp = cecil_dump(DLLTMP, 'GrimorioNerviosoFlotante')
    for muerto in ('AtaqueListo', 'MarcarAtaque'):
        if muerto in dmp:
            mal(f'el espíritu sigue llamando a {muerto}')
        else:
            ok(f'el espíritu YA NO llama a {muerto}')
    for vivo in ('SombraActiva', 'JugadorAtacoReciente', 'PresaCercana'):
        if vivo in dmp:
            ok(f'el espíritu conserva {vivo}')
        else:
            mal(f'el espíritu perdió {vivo}')

    # (c) LA DESCARGA PRESTADA MURIÓ — el libro escupe la lágrima
    dmp_base = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.GrimorioHambriento')

    def bloque_metodo(dump, nombre):
        lineas, dentro = [], False
        for ln in dump.splitlines():
            if ln.startswith('METHOD'):
                dentro = f'::{nombre}(' in ln
                continue
            if dentro:
                lineas.append(ln)
        return '\n'.join(lineas)

    shoot_il = bloque_metodo(dmp_base, 'Shoot')
    if 'ldc.i4 931' in shoot_il or 'ldc.i4.s 931' in shoot_il:
        mal('el Shoot del libro sigue disparando el 931 (Nightglow)')
    else:
        ok('el Shoot YA NO dispara el 931 (Nightglow)')
    if 'LagrimaDeTintaProjectile' in shoot_il and 'LagrimaDeTintaProjectile' in dmp_base:
        ok('el libro escupe LA LÁGRIMA DE TINTA (Shoot + Item.shoot)')
    else:
        mal('la lágrima NO aparece en el libro')
    # el OrigenDelDisparo virtual sigue (la letra .93)
    if '::OrigenDelDisparo(' in dmp_base:
        ok('GrimorioHambriento::OrigenDelDisparo sigue presente')
    else:
        mal('GrimorioHambriento::OrigenDelDisparo AUSENTE')

    # (d) LA LÁGRIMA — la clase nueva con toda su máquina
    dmp_l = cecil_dump(DLLTMP, 'LagrimaDeTintaProjectile')
    if dmp_l.startswith('ERROR') or not dmp_l.strip():
        mal('la clase LagrimaDeTintaProjectile NO EXISTE')
    else:
        ok('la clase LagrimaDeTintaProjectile EXISTE')
        for m in ('::AI(', '::PreDraw(', '::OnHitNPC(', '::OnKill(',
                  'BuscarPresa', 'TICKS_POR_FRAME', 'RADIO_BUSCA'):
            if m in dmp_l:
                ok(f'la lágrima trae {m}')
            else:
                mal(f'la lágrima NO trae {m}')
        ssd = bloque_metodo(dmp_l, 'SetStaticDefaults')
        if 'ldc.i4.4' in ssd:
            ok('Main.projFrames = 4 (la respiración del strip)')
        else:
            mal('projFrames != 4')

    # (e) LA MANCHA — el recuerdo del disparo
    dmp_m = cecil_dump(DLLTMP, 'ManchaDeTinta')
    if dmp_m.startswith('ERROR') or not dmp_m.strip():
        mal('la clase ManchaDeTinta NO EXISTE')
    else:
        ok('la clase ManchaDeTinta EXISTE')
        if 'Charco' in dmp_m:
            ok('la mancha dibuja el Charco de SombrasLib')
        else:
            mal('la mancha no dibuja el charco')

    # (f) EL ARMA — TintaViva con su Shoot y la bolsa
    dmp_t = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.Sombras.TintaViva')
    if dmp_t.startswith('ERROR') or not dmp_t.strip():
        mal('la clase TintaViva NO EXISTE')
    else:
        ok('la clase TintaViva EXISTE')
        for m in ('::Shoot(', '::SetDefaults(', '::AddRecipes('):
            if m in dmp_t:
                ok(f'TintaViva trae {m}')
            else:
                mal(f'TintaViva NO trae {m}')
        if 'PosicionDelLibro' in dmp_t:
            ok('TintaViva dispara DESDE el libro cuando el Nervioso está fuera')
        else:
            mal('TintaViva no consulta al espíritu')
    dmp_b = cecil_dump(DLLTMP, 'BolsaSombras')
    if 'TintaViva' in dmp_b:
        ok('la Bolsa de las Sombras entrega LA TINTA VIVA')
    else:
        mal('la bolsa NO entrega la Tinta Viva')

    # (g) LO NO TOCADO — el sistema de oleadas y el sello, intactos
    for clase in ('GrimorioFuriaSistema', 'OleadaNPC'):
        n = cecil_dump(DLLTMP, clase)
        v = cecil_dump(DLLPREV, clase)
        if n == v:
            ok(f'{clase} IL IDÉNTICO a la .94 (intacto)')
        else:
            mal(f'{clase} CAMBIA — esta versión NO lo tocaba')

    print(f'=== 6. HJSON ×3 — la lágrima presente, el compás muerto ===')
    for idioma in ('es-MX', 'es-ES', 'en-US'):
        p = f'Localization/{idioma}_Mods.AethonMod.hjson'
        raw = blobs[p].decode('utf-8')
        if raw.count('TintaViva: {') == 1:
            ok(f'{idioma}: TintaViva presente')
        else:
            mal(f'{idioma}: TintaViva ausente')
        if 'cada 10 s (y nunca antes' in raw or \
                'every 10 s (and never sooner' in raw:
            mal(f'{idioma}: el compás sigue prometido en el tooltip')
        else:
            ok(f'{idioma}: el compás ya no se promete')
        if 'calma 10 s' in raw or 'calms it 10 s' in raw:
            mal(f'{idioma}: la gracia .92 sigue en el texto')
        else:
            ok(f'{idioma}: la gracia muerta ya no se promete')

    # el espejo es-ES == es-MX por CONTENIDO (la lección del 18º incidente)
    mx = blobs['Localization/es-MX_Mods.AethonMod.hjson'].decode('utf-8')
    es = blobs['Localization/es-ES_Mods.AethonMod.hjson'].decode('utf-8')

    def cuerpo(t):
        lineas = t.split('\n')
        i = 0
        while i < len(lineas) and (lineas[i].startswith('//') or not lineas[i].strip()):
            i += 1
        return '\n'.join(lineas[i:])

    if cuerpo(mx) == cuerpo(es):
        ok('es-ES espejo de es-MX por CONTENIDO (cabecera propia)')
    else:
        mal('es-ES diverge de es-MX')

    # simetría de hojas es↔en (la salud del árbol localizado — el parse
    # de la casa: hjson.loads, JAMÁS líneas con ':')
    import hjson
    import json as _json
    textos = {}
    for idioma in ('es-MX', 'es-ES', 'en-US'):
        p = f'Localization/{idioma}_Mods.AethonMod.hjson'
        try:
            textos[idioma] = hjson.loads(blobs[p].decode('utf-8'))
            ok(f'{idioma} PARSEA ({len(blobs[p])} B)')
        except Exception as e:
            mal(f'{idioma} NO PARSEA: {e}')

    def hojas(d, pref=''):
        s = set()
        for k, v in d.items():
            if isinstance(v, dict):
                s |= hojas(v, f'{pref}{k}.')
            else:
                s.add(f'{pref}{k}')
        return s

    if 'es-MX' in textos and 'en-US' in textos:
        kmx, ken = hojas(textos['es-MX']), hojas(textos['en-US'])
        tintas_mx = [h for h in kmx if 'TintaViva' in h]
        tintas_en = [h for h in ken if 'TintaViva' in h]
        if tintas_mx == tintas_en and len(tintas_mx) == 2:
            ok(f'TintaViva localizada es↔en ({tintas_mx})')
        else:
            mal(f'TintaViva mal localizada: es {tintas_mx} en {tintas_en}')
        if kmx == ken:
            ok(f'claves simétricas es-MX ↔ en-US ({len(kmx)} hojas)')
        else:
            mal(f'asimetría es↔en: solo-es {sorted(kmx - ken)[:4]} '
                f'solo-en {sorted(ken - kmx)[:4]}')

    # el en-US del paquete conserva SUS TABS
    raw_en = blobs['Localization/en-US_Mods.AethonMod.hjson'].decode('utf-8')
    if '\t' in raw_en and raw_en.count('\t') > 4200:
        ok(f'en-US con TABS ({raw_en.count(chr(9))}) — sin normalizar')
    else:
        mal(f'en-US con {raw_en.count(chr(9))} tabs — ¿normalizado?')

    # el hjson del PAQUETE == el del ÁRBOL (byte a byte)
    for idioma in ('es-MX', 'es-ES', 'en-US'):
        p = f'Localization/{idioma}_Mods.AethonMod.hjson'
        arbol = open(f'AethonMod/{p}', 'rb').read()
        if arbol == blobs[p]:
            ok(f'{idioma}: paquete == árbol (byte a byte)')
        else:
            mal(f'{idioma}: el paquete diverge del árbol '
                '(¿el -build re-serializó el es-ES? — restaurar y re-empaquetar)')

    print()
    if FALLOS:
        print(f'✗✗✗ {len(FALLOS)} FALLOS:')
        for f in FALLOS:
            print(f'  - {f}')
        sys.exit(1)
    print(f'✓ AUDITORÍA v6.50.95 COMPLETA — 0 fallos '
          f'({len(d)} B, md5 {md5})')


if __name__ == '__main__':
    main()
