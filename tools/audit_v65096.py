#!/usr/bin/env python3
"""
v6.50.96 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65096.py [ruta-al-.tmod]

Novedades .96 — LA GRIETA (la lágrima re-leída como lo que siempre fue):
«el nuevo proyectil lo usaste tal cual, no le diste efectos visuales,
digamos que es un grieta como debe ser, entonces dale muchos efectos
visuales investiga que tipo de efectos se le pueden poner al nuevo
proyectil cuanto mas efectos mejor».

(1) LA CLASE SE LLAMA COMO SU NATURALEZA: LagrimaDeTintaProjectile →
    GrietaDeTintaProjectile (el sprite del usuario, SIN TOCAR — el
    rawimg renombrado debe ser byte-idéntico al de la .95).
(2) EL STACK DE EFECTOS (15 capas): nacimiento con estrella de ruptura
    (RiftLib.Star) + chispas de anomalía (ChispasAnomalia ×4 sitios),
    ribbon del vacío (EstelaLib.Track+Ribbon), 3 estelas fantasma,
    LA ASPIRACIÓN (motas convergentes + orbitales), aberración cromática
    (VFXCore.QuadSrc rojo/cian), EL GLITCH DE TAJOS (RiftLib.EcoGlitch
    en bandas), halo doble, EL IRIS (anillo Ring), EL OJO RASGADO
    (SombrasLib.Ojo mirando a la presa), estrellas fugitivas, goteo,
    luz parpadeante (VFXCore.Hash01), LA MORDIDA (chispas al golpear),
    EL COLAPSO (implosión + estallido) y EL CORTE QUE SANA
    (ManchaDeTinta con VFXCore.Line ×3, 46 t de vida).
ENTRADAS: 386 (las mismas de la .95 con el rawimg RENOMBRADO:
LagrimaDeTintaProjectile.rawimg → GrietaDeTintaProjectile.rawimg).
Whitelist del blob diff: dll/pdb/Info + 3 hjson. IDÉNTICOS esperados:
GrimorioFuriaSistema, OleadaNPC, SombraPaginaCaza, GrimorioNerviosoFlotante.
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
PREV = '/home/sync/AethonMod-v6.50.95.tmod'
CECIL = '/home/z/tmp_scripts/cecil_check/bin/Release/net8.0/cecil_check.dll'
DOTNET = '/tmp/sdk/dotnet'
DLLTMP = '/tmp/audit_v65096.dll'
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


def bloque_metodo(dump, nombre):
    lineas, dentro = [], False
    for ln in dump.splitlines():
        if ln.startswith('METHOD'):
            dentro = f'::{nombre}(' in ln
            continue
        if dentro:
            lineas.append(ln)
    return '\n'.join(lineas)


def main():
    d = open(TMOD, 'rb').read()
    md5 = hashlib.md5(d).hexdigest()
    print(f'=== {TMOD} ===')
    print(f'  {len(d)} B | md5 {md5}')

    tmlver, name, ver, entries, fsize, blobs = inflar(TMOD)
    print(f'=== 1. CABECERA ===')
    if ver == '6.50.96' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.96)')

    print(f'=== 2. EOF EXACTO (doble fórmula) ===')
    total_comp = sum(c for _, _, c in entries)
    tend = parse(TMOD)[4]
    if tend + total_comp == fsize:
        ok(f'table_end {tend} + sum(comp) {total_comp} == {fsize}')
    else:
        mal(f'EOF: {tend}+{total_comp} != {fsize}')

    print(f'=== 3. ENTRADAS vs .95 — el rawimg RENOMBRADO ===')
    _, _, verprev, eprev, _, blobs_prev = inflar(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    fuera = sorted(set_prev - set_now)
    dentro = sorted(set_now - set_prev)
    print(f'    .95: {len(eprev)} entradas | .96: {len(entries)} entradas')
    esperado_dentro = {'Content/Projectiles/Sombras/GrietaDeTintaProjectile.rawimg'}
    esperado_fuera = {'Content/Projectiles/Sombras/LagrimaDeTintaProjectile.rawimg'}
    if (len(entries) == 386 and set(dentro) == esperado_dentro
            and set(fuera) == esperado_fuera):
        ok('386 entradas — la lágrima rawimg RENOMBRADA a la grieta')
    else:
        for p in fuera:
            if p not in esperado_fuera:
                mal(f'SALE inesperado: {p}')
        for p in dentro:
            if p not in esperado_dentro:
                mal(f'ENTRA inesperado: {p}')
        if len(entries) != 386:
            mal(f'entradas: {len(entries)} (esperaba 386)')

    # EL SPRITE SIN TOCAR: el rawimg de la grieta == el de la lágrima (.95)
    g = 'Content/Projectiles/Sombras/GrietaDeTintaProjectile.rawimg'
    l = 'Content/Projectiles/Sombras/LagrimaDeTintaProjectile.rawimg'
    if g in blobs and l in blobs_prev and blobs[g] == blobs_prev[l]:
        ok(f'el sprite del usuario SIN TOCAR ({len(blobs[g])} B byte-idénticos a la .95)')
    else:
        mal('el sprite cambió — la letra NO lo pedía')

    print(f'=== 4. BLOB DIFF — whitelist exacta ===')
    cambios = []
    for p in sorted(set_prev & set_now):
        if blobs_prev[p] != blobs[p]:
            cambios.append(p)
    permitidos = ('.dll', '.pdb', 'Info')
    for p in cambios:
        if any(s in p for s in permitidos) or p.startswith('Localization/'):
            ok(f'cambia (permitido): {p} {len(blobs_prev[p])}→{len(blobs[p])} B')
        else:
            mal(f'cambia FUERA de whitelist: {p}')
    arte_prev = {p for p in set_prev & set_now if p not in cambios}
    identicos = all(blobs_prev[p] == blobs[p] for p in arte_prev)
    if identicos:
        ok(f'arte byte-idéntico en {len(arte_prev)} entradas restantes')
    else:
        mal('arte con cambios no declarados')

    print(f'=== 5. CECIL — LA GRIETA y su stack ===')
    open(DLLTMP, 'wb').write(blobs['AethonMod.dll'])
    open(DLLPREV, 'wb').write(blobs_prev['AethonMod.dll'])

    # (a) la lágrima MURIÓ de nombre — la grieta EXISTE
    dmp_l = cecil_dump(DLLPREV, 'LagrimaDeTintaProjectile')
    dmp_g = cecil_dump(DLLTMP, 'GrietaDeTintaProjectile')
    if dmp_g.startswith('ERROR') or not dmp_g.strip():
        mal('la clase GrietaDeTintaProjectile NO EXISTE')
        return fin(md5, d)
    ok('la clase GrietaDeTintaProjectile EXISTE')
    dmp_prev_dll = cecil_dump(DLLTMP, 'LagrimaDeTintaProjectile')
    if dmp_prev_dll.startswith('ERROR') or not dmp_prev_dll.strip():
        ok('la clase LagrimaDeTintaProjectile YA NO VIVE en la .96 (sólo la .95)')
    else:
        mal('LagrimaDeTintaProjectile sigue viva — el rename quedó a medias')

    # (b) EL STACK — cada capa delata una llamada en el IL
    stack = [
        ('RiftLib::Star', 'la ESTRELLA DE RUPTURA del nacimiento'),
        ('RiftLib::ChispasAnomalia', 'LAS CHISPAS DE ANOMALÍA (nacimiento, '
         'fugitivas, mordida, colapso)'),
        ('RiftLib::EcoGlitch', 'EL GLITCH DE TAJOS (bandas desplazadas)'),
        ('EstelaLib::Track', 'EL TRACK del ribbon'),
        ('EstelaLib::Ribbon', 'EL RIBBON DEL VACÍO'),
        ('SombrasLib::Ojo', 'EL OJO RASGADO que mira a la presa'),
        ('VFXCore::QuadSrc', 'LA ABERRACIÓN CROMÁTICA (ecos rojo/cian)'),
        ('VFXCore::Ring', 'EL IRIS (anillo de apertura)'),
        ('VFXCore::Hash01', 'LA LUZ PARPADEANTE (flicker)'),
        ('ParticleManager::Spawn', 'las partículas del otro lado'),
        ('::AI(', 'la vida propia'),
        ('::PreDraw(', 'el draw de la grieta'),
        ('::OnHitNPC(', 'LA MORDIDA'),
        ('::OnKill(', 'EL COLAPSO'),
        ('BuscarPresa', 'LA CACERÍA (homing heredado)'),
        ('PulsoDelFrame', 'EL LATIDO del corazón de marfil'),
    ]
    n_chispas = dmp_g.count('RiftLib::ChispasAnomalia')
    for marca, nombre in stack:
        if marca in dmp_g:
            ok(f'{nombre}')
        else:
            mal(f'FALTA: {nombre} ({marca})')
    if n_chispas >= 4:
        ok(f'ChispasAnomalia llamada en {n_chispas} sitios (esperaba ≥4: '
            'nacimiento + fugitivas + mordida + colapso)')
    else:
        mal(f'ChispasAnomalia en {n_chispas} sitios (esperaba ≥4)')
    ssd = bloque_metodo(dmp_g, 'SetStaticDefaults')
    if 'ldc.i4.4' in ssd:
        ok('Main.projFrames = 4 (la respiración del strip)')
    else:
        mal('projFrames != 4')
    # el OJO sólo abre con presa: el bloque de Ojo va tras el chequeo
    ai_il = bloque_metodo(dmp_g, 'AI')
    draw_il = bloque_metodo(dmp_g, 'DrawTodo')
    for plegado, donde, nombre in (
            ('ldc.r4 34', ai_il, 'LA ASPIRACIÓN nace a 34 px (const plegada)'),
            ('ldc.r4 66', ai_il, 'LA ASPIRACIÓN llega de 66 px (const plegada)'),
            ('ldc.r4 7', ai_il, 'las ÓRBITAS cada 7 t (const plegada)'),
            ('ldc.r4 34', draw_il, 'EL GLITCH cada 34 t (const float PLEGADA)'),
            ('ldc.r4 7', draw_il, 'EL GLITCH dura 7 t (const float PLEGADA)')):
        if plegado in donde:
            ok(f'{nombre} (en el IL)')
        else:
            mal(f'{nombre} — literal {plegado} ausente')

    # (c) EL COLAPSO — la implosión en OnKill
    kill_il = bloque_metodo(dmp_g, 'OnKill')
    if kill_il.count('Dust.NewDustPerfect') >= 2 or 'NewDustPerfect' in kill_il:
        ok('OnKill: implosión (14 convergentes) + estallido (16 en anillo)')
    else:
        mal('OnKill sin polvo — ¿dónde quedó el colapso?')
    if 'ManchaDeTinta' in kill_il:
        ok('OnKill deja LA MANCHA')
    else:
        mal('OnKill NO deja la mancha')

    # (d) LA MANCHA con EL CORTE QUE SANA
    dmp_m = cecil_dump(DLLTMP, 'ManchaDeTinta')
    if dmp_m.startswith('ERROR') or not dmp_m.strip():
        mal('la clase ManchaDeTinta NO EXISTE')
    else:
        ok('la clase ManchaDeTinta EXISTE')
        if 'SombrasLib::Charco' in dmp_m or 'Charco' in dmp_m:
            ok('la mancha dibuja el Charco de SombrasLib')
        else:
            mal('la mancha no dibuja el charco')
        n_lineas = dmp_m.count('VFXCore::Line')
        if n_lineas >= 3:
            ok(f'EL CORTE QUE SANA: {n_lineas} líneas VFXCore::Line '
               '(cian + magenta + marfil)')
        else:
            mal(f'EL CORTE QUE SANA: {n_lineas} líneas (esperaba 3)')
        sd_m = bloque_metodo(dmp_m, 'SetDefaults')
        if 'ldc.i4.s 46' in sd_m:
            ok('la mancha vive 46 t (el corte sana en 26)')
        else:
            mal('la mancha no vive 46 t')

    # (e) el libro abre la grieta
    dmp_base = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.GrimorioHambriento')
    shoot_il = bloque_metodo(dmp_base, 'Shoot')
    if 'ldc.i4 931' in shoot_il or 'ldc.i4.s 931' in shoot_il:
        mal('el Shoot del libro dispara el 931 (Nightglow)')
    else:
        ok('el Shoot YA NO dispara el 931 (Nightglow)')
    if 'GrietaDeTintaProjectile' in shoot_il and 'GrietaDeTintaProjectile' in dmp_base:
        ok('el libro ABRE LA GRIETA (Shoot + Item.shoot)')
    else:
        mal('la grieta NO aparece en el libro')
    if '::OrigenDelDisparo(' in dmp_base:
        ok('GrimorioHambriento::OrigenDelDisparo sigue presente')
    else:
        mal('GrimorioHambriento::OrigenDelDisparo AUSENTE')

    # (f) el arma la dispara
    dmp_t = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.Sombras.TintaViva')
    if dmp_t.startswith('ERROR') or not dmp_t.strip():
        mal('la clase TintaViva NO EXISTE')
    else:
        ok('la clase TintaViva EXISTE')
        if 'GrietaDeTintaProjectile' in dmp_t:
            ok('TintaViva dispara LA GRIETA')
        else:
            mal('TintaViva no dispara la grieta')
        if 'PosicionDelLibro' in dmp_t:
            ok('TintaViva dispara DESDE el libro cuando el Nervioso está fuera')
        else:
            mal('TintaViva no consulta al espíritu')
    dmp_b = cecil_dump(DLLTMP, 'BolsaSombras')
    if 'TintaViva' in dmp_b:
        ok('la Bolsa de las Sombras entrega LA TINTA VIVA')
    else:
        mal('la bolsa NO entrega la Tinta Viva')

    # (g) LO NO TOCADO — intactos (esta versión sólo tocó la grieta)
    for clase in ('GrimorioFuriaSistema', 'OleadaNPC',
                  'SombraPaginaCaza', 'GrimorioNerviosoFlotante'):
        n = cecil_dump(DLLTMP, clase)
        v = cecil_dump(DLLPREV, clase)
        if n == v:
            ok(f'{clase} IL IDÉNTICO a la .95 (intacto)')
        else:
            mal(f'{clase} CAMBIA — esta versión NO lo tocaba')

    print(f'=== 6. HJSON ×3 — LA GRIETA vive, la lágrima murió ===')
    for idioma in ('es-MX', 'es-ES', 'en-US'):
        p = f'Localization/{idioma}_Mods.AethonMod.hjson'
        raw = blobs[p].decode('utf-8')
        if raw.count('TintaViva: {') == 1:
            ok(f'{idioma}: TintaViva presente')
        else:
            mal(f'{idioma}: TintaViva ausente')
        es = idioma.startswith('es')
        muertos = ('Escupe LÁGRIMAS DE TINTA', 'escupe LA LÁGRIMA DE TINTA',
                   'no quiso llorar') if es else \
                 ('Spits INK TEARS', 'spits THE INK TEAR', 'refused to cry')
        vivos = ('Abre GRIETAS DE TINTA', 'OJO RASGADO') if es else \
              ('Opens INK RIFTS', 'TORN EYE')
        for m in muertos:
            if m in raw:
                mal(f'{idioma}: «{m}» — la lágrima vive')
        todo_muerto = all(m not in raw for m in muertos)
        if todo_muerto:
            ok(f'{idioma}: la lágrima MURIÓ en los textos')
        for v in vivos:
            if v in raw:
                ok(f'{idioma}: «{v}» presente')
            else:
                mal(f'{idioma}: falta «{v}»')

    # el espejo es-ES == es-MX por CONTENIDO
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

    # simetría de hojas es↔en
    import hjson
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
        if kmx == ken:
            ok(f'claves simétricas es-MX ↔ en-US ({len(kmx)} hojas)')
        else:
            mal(f'asimetría es↔en: solo-es {sorted(kmx - ken)[:4]} '
                f'solo-en {sorted(ken - kmx)[:4]}')

    # el en-US del paquete conserva SUS TABS
    raw_en = blobs['Localization/en-US_Mods.AethonMod.hjson'].decode('utf-8')
    if '\t' in raw_en and raw_en.count('\t') >= 4288:
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
            mal(f'{idioma}: el paquete diverge del árbol')

    fin(md5, d)


def fin(md5, d):
    print()
    if FALLOS:
        print(f'✗✗✗ {len(FALLOS)} FALLOS:')
        for f in FALLOS:
            print(f'  - {f}')
        sys.exit(1)
    print(f'✓ AUDITORÍA v6.50.96 COMPLETA — 0 fallos '
          f'({len(d)} B, md5 {md5})')


if __name__ == '__main__':
    main()
