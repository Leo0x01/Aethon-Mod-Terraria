#!/usr/bin/env python3
"""
v6.50.98 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65098.py [ruta-al-.tmod]

LA LETRA DEL USUARIO: «te dare el codigo para un arma nueva de prueba,
recuerda darcela al jugador, este sera el proyectil y su efecto».

(1) EL ARMA NUEVA: EL GRIMORIO SELLADO (GrimorioSellado) — dispara EL
    FRAGMENTO DE AETHON (FragmentoAethon), que al morir abre LA APERTURA
    (ExplosionAethon), aplica el debuff OBSERVADO (Observado) y cuenta
    sus stacks en el SISTEMA (SistemaObservado). 5 disparos = DESBORDE
    (250% de daño, 5% de vida al dueño — sin matar jamás).
(2) LA ENTREGA: en la Bolsa del Probador (lección .83) + receta doble
    (la temática del usuario con SpellTome + la madera del protocolo).
(3) LOS ERRORES DEL BORRADOR, TODOS MUERTOS (el build REAL los cazó):
    paréntesis sin cerrar en el desborde, DustID.GoldFlare (no existe —
    GoldFlame), ItemID.Spellbook (no existe — SpellTome), Kill→OnKill
    (CS0619), las firmas de golpe de esta tML (OnHitNPC con HitInfo,
    ModifyHitNPC con HitModifiers), GetSource_FromThis, y el clamp de
    vida que no mata.
(4) NADA MÁS CAMBIA: el set de la .97 + 4 rawimg nuevos (390); la
    grieta, la mancha, el libro y toda la familia IL-IDÉNTICOS a la .97.
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
PREV = '/home/sync/AethonMod-v6.50.97.tmod'
CECIL = '/home/z/tmp_scripts/cecil_check/bin/Release/net8.0/cecil_check.dll'
DOTNET = '/tmp/sdk/dotnet'
DLLTMP = '/tmp/audit_v65098.dll'
DLLPREV = '/tmp/audit_v65097_prev.dll'

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
    if ver == '6.50.98' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.98)')

    print(f'=== 2. EOF EXACTO (doble fórmula) ===')
    total_comp = sum(c for _, _, c in entries)
    tend = parse(TMOD)[4]
    if tend + total_comp == fsize:
        ok(f'table_end {tend} + sum(comp) {total_comp} == {fsize}')
    else:
        mal(f'EOF: {tend}+{total_comp} != {fsize}')

    print(f'=== 3. ENTRADAS vs .97 — el set de la .97 + los 4 rawimg nuevos ===')
    _, _, verprev, eprev, _, blobs_prev = inflar(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    fuera = sorted(set_prev - set_now)
    dentro = sorted(set_now - set_prev)
    print(f'    .97: {len(eprev)} entradas | .98: {len(entries)} entradas')
    esperados = {
        'Content/Buffs/Observado.rawimg',
        'Content/Projectiles/Aethon/ExplosionAethon.rawimg',
        'Content/Projectiles/Aethon/FragmentoAethon.rawimg',
        'Content/Weapons/GrimorioSellado.rawimg',
    }
    if len(entries) == 390 and not fuera and set(dentro) == esperados:
        ok('390 entradas — la .97 + EXACTAMENTE los 4 rawimg del arma nueva')
    else:
        for p in fuera:
            mal(f'SALE inesperado: {p}')
        for p in dentro:
            if p not in esperados:
                mal(f'ENTRA inesperado: {p}')
        if len(entries) != 390:
            mal(f'entradas: {len(entries)} (esperaba 390)')
        else:
            ok('390 entradas con los 4 rawimg correctos')

    # EL SPRITE DE LA GRIETA SIN TOCAR: rawimg .98 == rawimg .97
    g = 'Content/Projectiles/Sombras/GrietaDeTintaProjectile.rawimg'
    if g in blobs and g in blobs_prev and blobs[g] == blobs_prev[g]:
        ok(f'la GRIETA sin tocar ({len(blobs[g])} B byte-idénticos a la .97)')
    else:
        mal('el sprite de la grieta cambió — la letra NO lo pedía')

    # dims exactas de los 4 rawimg nuevos (W×H×4 + 12 de cabecera)
    dims = {
        'Content/Weapons/GrimorioSellado.rawimg': (28, 30),
        'Content/Projectiles/Aethon/FragmentoAethon.rawimg': (24, 192),
        'Content/Projectiles/Aethon/ExplosionAethon.rawimg': (48, 384),
        'Content/Buffs/Observado.rawimg': (32, 32),
    }
    for p, (w, h) in dims.items():
        if p in set_now:
            raw = next(r for q, r, _ in entries if q == p)
            if raw == w * h * 4 + 12:
                ok(f'{p.split("/")[-1]}: {w}×{h} exactos (raw {raw} B)')
            else:
                mal(f'{p}: raw {raw} != {w}×{h}×4+12')
        else:
            mal(f'FALTA el rawimg: {p}')

    print(f'=== 4. BLOB DIFF vs .97 — whitelist: código + hjson (textos nuevos) ===')
    cambios = []
    for p in sorted(set_prev & set_now):
        if blobs_prev[p] != blobs[p]:
            cambios.append(p)
    permitidos = ('.dll', '.pdb', 'Info',
                  'Localization/es-MX_Mods.AethonMod.hjson',
                  'Localization/es-ES_Mods.AethonMod.hjson',
                  'Localization/en-US_Mods.AethonMod.hjson')
    for p in cambios:
        if any(s in p for s in permitidos):
            ok(f'cambia (permitido): {p.split("/")[-1]} {len(blobs_prev[p])}→{len(blobs[p])} B')
        else:
            mal(f'cambia FUERA de whitelist: {p}')
    arte_prev = {p for p in set_prev & set_now if p not in cambios}
    identicos = all(blobs_prev[p] == blobs[p] for p in arte_prev)
    if identicos:
        ok(f'arte byte-idéntico en {len(arte_prev)} entradas restantes')
    else:
        mal('arte con cambios no declarados')
    hjsons_cambiados = [p for p in cambios if '.hjson' in p]
    if len(hjsons_cambiados) == 3:
        ok('los 3 hjson cambian (los textos del arma nueva)')
    elif cambios:
        for p in cambios:
            if '.hjson' not in p:
                mal(f'un hjson no cambió: esperaba los 3')

    print(f'=== 5. CECIL — LAS CLASES NUEVAS en el IL ===')
    open(DLLTMP, 'wb').write(blobs['AethonMod.dll'])
    open(DLLPREV, 'wb').write(blobs_prev['AethonMod.dll'])

    # (a) FRAGMENTO DE AETHON — el disparo: la IA, la mordida, el colapso
    dmp_f = cecil_dump(DLLTMP, 'AethonMod.Content.Projectiles.Aethon.FragmentoAethon')
    if dmp_f.startswith('ERROR') or not dmp_f.strip():
        mal('la clase FragmentoAethon NO EXISTE')
        return fin(md5, d)
    ok('FragmentoAethon EXISTE')
    for marca, nombre in (
            ('::AI(', 'la IA del vuelo'),
            ('::OnHitNPC(', 'LA MORDIDA'),
            ('::OnKill(', 'EL COLAPSO'),
            ('::ModifyHitNPC(', 'LA REVELACIÓN (5 stacks)'),
            ('NewDustDirect', 'las chispas doradas'),
            ('NewDustPerfect', 'el rayo violeta'),
            ('Lighting::AddLight', 'LA LUZ DORADA que late'),
            ('get_ActiveNPCs', 'EL HOMING (Main.ActiveNPCs)'),
            ('ToRotation', 'la rotación al vuelo'),
            ('NPC::AddBuff', 'el debuff OBSERVADO aplicado'),
            ('SistemaObservado::AddStack', 'el stack en el sistema'),
            ('SistemaObservado::GetStacks', 'la cuenta del «Conocido»'),
            ('GetSource_FromThis', 'el source de esta tML (el GetProjectileSource del borrador murió)'),
            ('ExplosionAethon', 'LA APERTURA al morir'),
            ('SoundEngine::PlaySound', 'el sonido del impacto')):
        if marca in dmp_f:
            ok(f'{nombre}')
        else:
            mal(f'FALTA: {nombre} ({marca})')
    mod_il = bloque_metodo(dmp_f, 'ModifyHitNPC')
    if 'SetCrit' in mod_il and 'FinalDamage' in mod_il:
        ok('ModifyHitNPC: SetCrit() + FinalDamage ×1.5 (crítico garantizado de esta tML)')
    else:
        mal('ModifyHitNPC sin SetCrit/FinalDamage')
    ai_il = bloque_metodo(dmp_f, 'AI')
    if 'Vector2::Lerp' in ai_il:
        ok('el homing suave (Lerp 0.1) en la IA')
    else:
        mal('el homing perdió el Lerp')
    ssd_f = bloque_metodo(dmp_f, 'SetStaticDefaults')
    if 'ldc.i4.8' in ssd_f:
        ok('Main.projFrames = 8 (la respiración del fragmento)')
    else:
        mal('projFrames != 8 en FragmentoAethon')

    # (b) LA APERTURA — la explosión
    dmp_e = cecil_dump(DLLTMP, 'AethonMod.Content.Projectiles.Aethon.ExplosionAethon')
    if dmp_e.startswith('ERROR') or not dmp_e.strip():
        mal('la clase ExplosionAethon NO EXISTE')
        return fin(md5, d)
    ok('ExplosionAethon EXISTE')
    ssd_e = bloque_metodo(dmp_e, 'SetStaticDefaults')
    if 'ldc.i4.8' in ssd_e:
        ok('projFrames = 8 (se abre violentamente → se sella)')
    else:
        mal('projFrames != 8 en ExplosionAethon')
    ai_e = bloque_metodo(dmp_e, 'AI')
    for marca, nombre in (('NewDustPerfect', 'los rayos violetas radiales + la onda expansiva'),
                           ('Lighting::AddLight', 'la luz que se apaga al sellarse')):
        if marca in ai_e:
            ok(nombre)
        else:
            mal(f'la APERTURA perdió: {nombre}')

    # (c) EL SISTEMA — quien cuenta los 5 golpes
    dmp_s = cecil_dump(DLLTMP, 'AethonMod.Content.Systems.SistemaObservado')
    if dmp_s.startswith('ERROR') or not dmp_s.strip():
        mal('la clase SistemaObservado NO EXISTE')
        return fin(md5, d)
    ok('SistemaObservado EXISTE')
    for marca, nombre in (
            ('::AddStack(', 'AddStack'),
            ('::GetStacks(', 'GetStacks'),
            ('::ClearStacks(', 'ClearStacks'),
            ('::PostUpdateWorld(', 'el reloj de los 5 segundos'),
            ('::ClearWorld(', 'la limpieza al cerrar el mundo'),
            ('NewDustPerfect', 'el destello del «Conocido»')):
        if marca in dmp_s:
            ok(nombre)
        else:
            mal(f'SistemaObservado FALTA: {nombre} ({marca})')

    # (d) EL DEBUFF — la presencia
    dmp_b = cecil_dump(DLLTMP, 'AethonMod.Content.Buffs.Observado')
    if dmp_b.startswith('ERROR') or not dmp_b.strip():
        mal('la clase Observado (buff) NO EXISTE')
        return fin(md5, d)
    ok('Observado EXISTE')
    upd_b = bloque_metodo(dmp_b, 'Update')
    if 'NewDustDirect' in upd_b:
        ok('el buff: las chispas doradas de la mirada')
    else:
        mal('el buff sin chispas')

    # (e) EL ARMA — el grimorio sellado y su desborde
    dmp_w = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.GrimorioSellado')
    if dmp_w.startswith('ERROR') or not dmp_w.strip():
        mal('la clase GrimorioSellado NO EXISTE')
        return fin(md5, d)
    ok('GrimorioSellado EXISTE')
    shoot_il = bloque_metodo(dmp_w, 'Shoot')
    for marca, nombre in (
            ('ldc.r4 2.5', 'el desborde al 250% (la const plegada del ERROR de sintaxis, muerto)'),
            ('ldc.i4.s 120', 'el cooldown de 2 s'),
            ('NewProjectile', 'el rayo masivo'),
            ('NewProjectileDirect', 'el disparo normal (con ai[1] del homing)'),
            ('Math::Max', 'el clamp que NO mata (5% de vida sin morir)'),
            ('NewDustPerfect', 'las 30 chispas del desborde'),
            ('SoundEngine::PlaySound', 'el Item88 del desborde'),
            ('NewText', '«¡Aethon se desborda!»')):
        if marca in shoot_il:
            ok(nombre)
        else:
            mal(f'el Shoot FALTA: {nombre} ({marca})')
    if '::CanUseItem(' in dmp_w and '::UpdateInventory(' in dmp_w:
        ok('CanUseItem + UpdateInventory (el cooldown vive)')
    else:
        mal('faltan CanUseItem/UpdateInventory')
    rec_il = bloque_metodo(dmp_w, 'AddRecipes')
    if rec_il.count('CreateRecipe') >= 2:
        ok('DOS recetas (SpellTome temática + la madera del protocolo)')
    else:
        mal(f'recetas: {rec_il.count("CreateRecipe")} (esperaba 2)')
    if 'Item88' in dmp_w or 'PlaySound' in dmp_w:
        pass  # el sonido vive en el Shoot (arriba)

    # (f) LA ENTREGA — en la Bolsa del Probador
    dmp_p = cecil_dump(DLLTMP, 'BolsaProbador')
    if 'GrimorioSellado' in dmp_p:
        ok('LA ENTREGA: GrimorioSellado en la Bolsa del Probador (lección .83)')
    else:
        mal('GrimorioSellado NO está en la Bolsa del Probador')

    print(f'=== 6. LO NO TOCADO — IL idéntico a la .97 ===')
    for clase in ('GrietaDeTintaProjectile',
                  'AethonMod.Content.Projectiles.Sombras.ManchaDeTinta',
                  'GrimorioFuriaSistema', 'OleadaNPC',
                  'SombraPaginaCaza', 'GrimorioNerviosoFlotante',
                  'AethonMod.Content.Weapons.Sombras.TintaViva',
                  'AethonMod.Content.Weapons.GrimorioHambriento'):
        n = cecil_dump(DLLTMP, clase)
        v = cecil_dump(DLLPREV, clase)
        if n == v and n.strip():
            ok(f'{clase.split(".")[-1]} IL IDÉNTICO a la .97 (intacto)')
        else:
            mal(f'{clase} CAMBIA — la .98 no lo tocaba')
    dmp_base = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.GrimorioHambriento')
    shoot_libro = bloque_metodo(dmp_base, 'Shoot')
    if 'GrietaDeTintaProjectile' in shoot_libro:
        ok('el libro ABRE LA GRIETA (Shoot)')
    else:
        mal('la grieta NO aparece en el Shoot del libro')

    print(f'=== 7. HJSON ×3 — los textos del arma nueva ===')
    for idioma in ('es-MX', 'es-ES', 'en-US'):
        p = f'Localization/{idioma}_Mods.AethonMod.hjson'
        raw = blobs[p].decode('utf-8')
        es = idioma.startswith('es')
        item = 'DisplayName: Grimorio Sellado' if es else 'DisplayName: Sealed Grimoire'
        tip = 'Contiene a Aethon' if es else 'Contains Aethon'
        frag = 'FragmentoAethon.DisplayName: Fragmento de Aethon' if es else 'FragmentoAethon.DisplayName: Fragment of Aethon'
        aper = 'ExplosionAethon.DisplayName: Apertura de Aethon' if es else 'ExplosionAethon.DisplayName: Opening of Aethon'
        obs = 'Aethon te está mirando' if es else 'Aethon is watching you'
        for v in (f'GrimorioSellado: {{', item, tip, frag, aper, f'Observado: {{', obs):
            if v in raw:
                ok(f'{idioma}: «{v[:44]}»')
            else:
                mal(f'{idioma}: falta «{v[:44]}»')

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

    # el en-US del paquete conserva SUS TABS (4320 exactos)
    raw_en = blobs['Localization/en-US_Mods.AethonMod.hjson'].decode('utf-8')
    if raw_en.count('\t') == 4320:
        ok(f'en-US con {raw_en.count(chr(9))} TABS exactos (4288+32)')
    else:
        mal(f'en-US con {raw_en.count(chr(9))} tabs (esperaba 4320)')

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
    print(f'✓ AUDITORÍA v6.50.98 COMPLETA — 0 fallos '
          f'({len(d)} B, md5 {md5})')


if __name__ == '__main__':
    main()
