#!/usr/bin/env python3
"""
v6.50.97 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65097.py [ruta-al-.tmod]

Novedad .97 — LA LECCIÓN DEL client.log («hay errores mira», 16:53:24):
System.InvalidOperationException: «End was called, but Begin has not yet
been called» en GrietaDeTintaProjectile.DrawTodo:349 → PreDraw:324. Un frame
llegó al PreDraw con el lote del juego YA CERRADO (un End ajeno sin Begin)
y el End pelado que abría el lote aditivo del ribbon LANZÓ: tML lo registró
como «Excepción silenciosa» (first-chance) aunque el catch del PreDraw lo
tragó — la grieta se volvía invisible ese frame. El fix: la entrada del
lote del ribbon ahora usa la SONDA de la casa (VFXCore.CerrarLoteSiAbierto)
en vez del End pelado: si hay un Begin vivo lo cierra como siempre, y si
el frame llegó envenenado NO lanza y la grieta dibuja IGUAL.

(1) EL FIX EN EL IL (DrawTodo): la secuencia de instrucciones debe ser
    EstelaLib::Track → VFXCore::CerrarLoteSiAbierto → SpriteBatch::Begin
    (Immediate) … y SOLO UN SpriteBatch::End() pelado en TODO DrawTodo (el
    de la .96 tenía DOS: el de entrada que lanzaba + el de cierre).
    PreDraw conserva su try/catch (la red que curó la .96 en producción).
(2) NADA MÁS CAMBIA: entradas 386 (el MISMO set de la .96), el rawimg de la
    grieta byte-idéntico, los 3 hjson del paquete IDÉNTICOS a la .96 (esta
    versión NO toca textos — si un hjson cambia, es FALLO), y el blob diff
    vs .96 admite SOLO dll/pdb/Info.
(3) LO NO TOCADO — IL idéntico a la .96: ManchaDeTinta (vive en el mismo
    .cs pero no se tocó), GrimorioFuriaSistema, OleadaNPC, SombraPaginaCaza,
    GrimorioNerviosoFlotante, TintaViva, GrimorioHambriento.
(4) El stack de 15 efectos SIGUE COMPLETO en el IL (la .97 no le quitó
    ni una capa a la grieta — sólo la volvió inmune al veneno ajeno).
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
PREV = '/home/sync/AethonMod-v6.50.96.tmod'
CECIL = '/home/z/tmp_scripts/cecil_check/bin/Release/net8.0/cecil_check.dll'
DOTNET = '/tmp/sdk/dotnet'
DLLTMP = '/tmp/audit_v65097.dll'
DLLPREV = '/tmp/audit_v65096_prev.dll'

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
    if ver == '6.50.97' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.97)')

    print(f'=== 2. EOF EXACTO (doble fórmula) ===')
    total_comp = sum(c for _, _, c in entries)
    tend = parse(TMOD)[4]
    if tend + total_comp == fsize:
        ok(f'table_end {tend} + sum(comp) {total_comp} == {fsize}')
    else:
        mal(f'EOF: {tend}+{total_comp} != {fsize}')

    print(f'=== 3. ENTRADAS vs .96 — el MISMO set, NADA renombrado ===')
    _, _, verprev, eprev, _, blobs_prev = inflar(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    fuera = sorted(set_prev - set_now)
    dentro = sorted(set_now - set_prev)
    print(f'    .96: {len(eprev)} entradas | .97: {len(entries)} entradas')
    if len(entries) == 386 and not fuera and not dentro:
        ok('386 entradas — el set EXACTO de la .96 (cero entra, cero sale)')
    else:
        for p in fuera:
            mal(f'SALE inesperado: {p}')
        for p in dentro:
            mal(f'ENTRA inesperado: {p}')
        if len(entries) != 386:
            mal(f'entradas: {len(entries)} (esperaba 386)')

    # EL SPRITE SIN TOCAR: el rawimg de la .97 == el de la .96
    g = 'Content/Projectiles/Sombras/GrietaDeTintaProjectile.rawimg'
    if g in blobs and g in blobs_prev and blobs[g] == blobs_prev[g]:
        ok(f'el sprite del usuario SIN TOCAR ({len(blobs[g])} B byte-idénticos a la .96)')
    else:
        mal('el sprite cambió — la letra NO lo pedía')

    print(f'=== 4. BLOB DIFF vs .96 — whitelist ESTRICTA (sólo código) ===')
    cambios = []
    for p in sorted(set_prev & set_now):
        if blobs_prev[p] != blobs[p]:
            cambios.append(p)
    permitidos = ('.dll', '.pdb', 'Info')
    for p in cambios:
        if any(s in p for s in permitidos):
            ok(f'cambia (permitido): {p} {len(blobs_prev[p])}→{len(blobs[p])} B')
        else:
            # la .97 NO toca textos: un hjson que cambia es FALLO
            mal(f'cambia FUERA de whitelist: {p}')
    arte_prev = {p for p in set_prev & set_now if p not in cambios}
    identicos = all(blobs_prev[p] == blobs[p] for p in arte_prev)
    if identicos:
        ok(f'arte byte-idéntico en {len(arte_prev)} entradas restantes')
    else:
        mal('arte con cambios no declarados')
    for idioma in ('es-MX', 'es-ES', 'en-US'):
        p = f'Localization/{idioma}_Mods.AethonMod.hjson'
        if p in blobs and p in blobs_prev and blobs[p] == blobs_prev[p]:
            ok(f'{idioma}: hjson IDÉNTICO a la .96 (la .97 no toca textos)')

    print(f'=== 5. CECIL — EL FIX de la .97 en el IL ===')
    open(DLLTMP, 'wb').write(blobs['AethonMod.dll'])
    open(DLLPREV, 'wb').write(blobs_prev['AethonMod.dll'])

    dmp_g = cecil_dump(DLLTMP, 'GrietaDeTintaProjectile')
    if dmp_g.startswith('ERROR') or not dmp_g.strip():
        mal('la clase GrietaDeTintaProjectile NO EXISTE')
        return fin(md5, d)
    ok('la clase GrietaDeTintaProjectile EXISTE')

    draw_il = bloque_metodo(dmp_g, 'DrawTodo')
    if not draw_il.strip():
        mal('DrawTodo NO EXISTE')
        return fin(md5, d)

    # (a) EL PATRÓN DEL FIX: Track → CerrarLoteSiAbierto → Begin → … → End
    ins = [ln.strip() for ln in draw_il.splitlines() if ln.strip()]
    idx_track = next((i for i, ln in enumerate(ins)
                      if 'EstelaLib::Track' in ln), -1)
    idx_cerrar = next((i for i, ln in enumerate(ins)
                       if 'VFXCore::CerrarLoteSiAbierto' in ln), -1)
    idx_begin = next((i for i, ln in enumerate(ins)
                      if 'SpriteBatch::Begin(' in ln), -1)
    idx_end = next((i for i, ln in enumerate(ins)
                    if 'SpriteBatch::End()' in ln), -1)
    if idx_track >= 0 and idx_cerrar > idx_track and idx_begin > idx_cerrar \
            and idx_end > idx_begin:
        ok('DrawTodo: Track → CerrarLoteSiAbierto → Begin(ribbon) → … → End '
           '(la sonda ANTES del lote aditivo)')
    else:
        mal(f'DrawTodo: orden roto (track {idx_track}, cerrar {idx_cerrar}, '
            f'begin {idx_begin}, end {idx_end})')

    # (b) EL END PELADO DE ENTRADA MURIÓ: queda UNO solo (el de cierre)
    n_end = draw_il.count('SpriteBatch::End()')
    if n_end == 1:
        ok('UN solo SpriteBatch::End() pelado en DrawTodo (el de cierre del '
           'lote del ribbon; el de entrada que lanzaba MURIÓ)')
    else:
        mal(f'DrawTodo tiene {n_end} End() pelados (esperaba 1)')
    n_cerrar = draw_il.count('VFXCore::CerrarLoteSiAbierto')
    if n_cerrar == 1:
        ok('UNA llamada a la sonda en DrawTodo (la entrada del ribbon)')
    else:
        mal(f'DrawTodo tiene {n_cerrar} llamadas a CerrarLoteSiAbierto (esperaba 1)')

    # (c) PreDraw conserva el try/catch (la red que curó la .96)
    pre_il = bloque_metodo(dmp_g, 'PreDraw')
    if 'DrawTodo(Microsoft.Xna.Framework.Color)' in pre_il \
            and 'leave.s' in pre_il \
            and pre_il.count('VFXCore::CerrarLoteSiAbierto') >= 2 \
            and 'ReabrirLoteVanilla' in pre_il:
        ok('PreDraw: try/catch + CerrarLoteSiAbierto + ReabrirLoteVanilla '
           '(la red de la .96 intacta)')
    else:
        mal('PreDraw perdió el try/catch o el Reabrir')

    # (d) EL STACK DE 15 — la .97 no le quitó ni una capa
    stack = [
        ('RiftLib::Star', 'la ESTRELLA DE RUPTURA del nacimiento'),
        ('RiftLib::ChispasAnomalia', 'LAS CHISPAS DE ANOMALÍA'),
        ('RiftLib::EcoGlitch', 'EL GLITCH DE TAJOS'),
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
    for marca, nombre in stack:
        if marca in dmp_g:
            ok(f'{nombre}')
        else:
            mal(f'FALTA: {nombre} ({marca})')
    ssd = bloque_metodo(dmp_g, 'SetStaticDefaults')
    if 'ldc.i4.4' in ssd:
        ok('Main.projFrames = 4 (la respiración del strip)')
    else:
        mal('projFrames != 4')
    for plegado, donde, nombre in (
            ('ldc.r4 34', draw_il, 'EL GLITCH cada 34 t (const float PLEGADA)'),
            ('ldc.r4 7', draw_il, 'EL GLITCH dura 7 t (const float PLEGADA)')):
        if plegado in donde:
            ok(f'{nombre} (en el IL)')
        else:
            mal(f'{nombre} — literal {plegado} ausente')

    # (e) EL COLAPSO y LA MANCHA siguen intactos
    kill_il = bloque_metodo(dmp_g, 'OnKill')
    if 'NewDustPerfect' in kill_il:
        ok('OnKill: implosión + estallido (el colapso intacto)')
    else:
        mal('OnKill sin polvo — ¿dónde quedó el colapso?')
    if 'ManchaDeTinta' in kill_il:
        ok('OnKill deja LA MANCHA')
    else:
        mal('OnKill NO deja la mancha')

    print(f'=== 6. LO NO TOCADO — IL idéntico a la .96 ===')
    for clase in ('AethonMod.Content.Projectiles.Sombras.ManchaDeTinta',
                  'GrimorioFuriaSistema', 'OleadaNPC',
                  'SombraPaginaCaza', 'GrimorioNerviosoFlotante'):
        n = cecil_dump(DLLTMP, clase)
        v = cecil_dump(DLLPREV, clase)
        if n == v and n.strip():
            ok(f'{clase.split(".")[-1]} IL IDÉNTICO a la .96 (intacto)')
        else:
            mal(f'{clase} CAMBIA — la .97 no lo tocaba')
    # el libro y el arma siguen disparando la grieta
    dmp_base = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.GrimorioHambriento')
    shoot_il = bloque_metodo(dmp_base, 'Shoot')
    if 'GrietaDeTintaProjectile' in shoot_il:
        ok('el libro ABRE LA GRIETA (Shoot)')
    else:
        mal('la grieta NO aparece en el Shoot del libro')
    if 'ldc.i4 931' in shoot_il or 'ldc.i4.s 931' in shoot_il:
        mal('el Shoot del libro dispara el 931 (Nightglow)')
    dmp_t = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.Sombras.TintaViva')
    if 'GrietaDeTintaProjectile' in dmp_t:
        ok('TintaViva dispara LA GRIETA')
    else:
        mal('TintaViva no dispara la grieta')

    print(f'=== 7. HJSON ×3 — sin cirugía esta vez ===')
    for idioma in ('es-MX', 'es-ES', 'en-US'):
        p = f'Localization/{idioma}_Mods.AethonMod.hjson'
        raw = blobs[p].decode('utf-8')
        if raw.count('TintaViva: {') == 1:
            ok(f'{idioma}: TintaViva presente')
        else:
            mal(f'{idioma}: TintaViva ausente')
        es = idioma.startswith('es')
        muertos = ('Escupe LÁGRIMAS DE TINTA',) if es else ('Spits INK TEARS',)
        vivos = ('Abre GRIETAS DE TINTA', 'OJO RASGADO') if es else \
              ('Opens INK RIFTS', 'TORN EYE')
        for m in muertos:
            if m in raw:
                mal(f'{idioma}: «{m}» — la lágrima vive')
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
    print(f'✓ AUDITORÍA v6.50.97 COMPLETA — 0 fallos '
          f'({len(d)} B, md5 {md5})')


if __name__ == '__main__':
    main()
