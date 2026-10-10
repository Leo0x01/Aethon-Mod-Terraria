#!/usr/bin/env python3
"""
v6.50.91 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65091.py [ruta-al-.tmod]

Novedades .91 — EL NERVIOSO CAZA POR SU CUENTA + LA FUGA + EL CÓDICE VIVO
MUERE (la letra del usuario): (1) «no es que este nervioso por tener miedo,
esta mas bien intranquilo, hambriento, enojado, inquieto, quiero comer y
tiene hambre» — el temperamento se relee: APETITO CON CUERPO; (2) LA CAZA:
con hambre < 50% y el jugador quieto 5 s (valor de PRUEBA) el libro sale
flotando encima de él MIRÁNDOLO (GrimorioNerviosoFlotante) hasta que algo
SE MUEVA cerca (≤480 px, todas menos los NPC de casas) — del libro sale
LA SOMBRA DE LA PÁGINA (SombraPaginaCaza) mordiendo 10% de la vida MÁXIMA
por golpe hasta matarla y ABSORBERLA: sin loot, sin gore, sin crédito del
jugador; la XP SÍ cobra; −1% de hambre por presa, caza hasta el 0% (el
reloj de hambre se congela mientras patrulla: PasoSinHambre); (3) ≥ 50%:
EL IMPACIENTE — efectos visuales desde CERO al 50% (HambreEfectos), el
libro HABLA (FrasesDeHambre) y comienzan las PROBABILIDADES de oleadas
(4%→20% cada 5 s); (4) al 100%: LA FUGA — el libro se escapa flotando
(modo fuga del espíritu, homenaje a la levitación del Códice Vivo);
(5) EL CÓDICE VIVO BORRADO POR COMPLETO (ítem + 2 proyectiles + 2 rawimg
+ registraciones + localización).
ENTRADAS: 388 de la .90 − 2 (rawimg del Códice Vivo y del proyectil) = 386.
El blob diff permite dll/pdb/Info + los 3 hjson + los 2 rawimg salientes —
NADA MÁS. Errático/Inestable: dump IL IDÉNTICO a la .90. La base
(EstadoGrimorio + GrimorioHambriento) CAMBIA documentado (NerviosoActivo +
PasoSinHambre + las 3 ramas del ojo). El Nervioso gana la región entera de
las dos vidas.
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
PREV = '/home/sync/AethonMod-v6.50.90.tmod'
CECIL = '/home/z/tmp_scripts/cecil_check/bin/Debug/net8.0/cecil_check.dll'
DOTNET = '/tmp/sdk/dotnet'
DLLTMP = '/tmp/audit_v65091.dll'
DLLPREV = '/tmp/audit_v65091_prev.dll'

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
    if ver == '6.50.91' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.91)')

    print(f'=== 2. EOF EXACTO (doble fórmula) ===')
    total_comp = sum(c for _, _, c in entries)
    tend = parse(TMOD)[4]
    if tend + total_comp == fsize:
        ok(f'table_end {tend} + sum(comp) {total_comp} == {fsize}')
    else:
        mal(f'EOF: {tend}+{total_comp} != {fsize}')

    print(f'=== 3. ENTRADAS vs .90 — SALEN los 2 rawimg del Códice Vivo ===')
    _, _, verprev, eprev, _, blobs_prev = inflar(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    fuera = sorted(set_prev - set_now)
    dentro = sorted(set_now - set_prev)
    print(f'    .90: {len(eprev)} entradas | .91: {len(entries)} entradas')
    esperado = len(eprev) - 2
    if len(entries) == esperado:
        ok(f'{len(eprev)} − 2 = {len(entries)}')
    else:
        mal(f'entradas: {len(entries)} (esperaba {esperado})')
    for p in fuera:
        if 'CodiceVivo' in p:
            ok(f'SALE: {p} (el difunto)')
        else:
            mal(f'SALE inesperado: {p}')
    for p in dentro:
        mal(f'ENTRA inesperado: {p}')

    print(f'=== 4. BLOB DIFF — whitelist exacta ===')
    cambios = []
    for p in sorted(set_prev & set_now):
        if blobs_prev[p] != blobs[p]:
            cambios.append(p)
    permitidos = ('.dll', '.pdb', 'Info', 'hjson', 'CodiceVivo')
    for p in cambios:
        if any(s in p for s in permitidos):
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

    print(f'=== 5. CECIL — la DLL de la .91 vs la de la .90 ===')
    open(DLLTMP, 'wb').write(blobs['AethonMod.dll'])
    open(DLLPREV, 'wb').write(blobs_prev['AethonMod.dll'])

    # (a) los muertos AUSENTES
    for clase in ('CodiceVivo', 'CodiceVivoProjectile', 'CodiceVivoChispa'):
        dmp = cecil_dump(DLLTMP, clase)
        if dmp.startswith('ERROR'):
            ok(f'{clase}: AUSENTE (borrado por completo)')
        else:
            mal(f'{clase} VIVE en la DLL — la purga falló')

    # (b) los nuevos PRESENTES con su firma (los const viven como FIELD)
    for clase, firma in (
        ('GrimorioNerviosoFlotante', 'MODO_CAZA'),
        ('SombraPaginaCaza', 'CADENCIA_MORDIDA'),
    ):
        dmp = cecil_dump(DLLTMP, clase)
        if not dmp.startswith('ERROR') and f' {firma}' in dmp:
            ok(f'{clase}: presente (con {firma})')
        else:
            mal(f'{clase}: no encontrado o sin {firma}')

    # (c) Errático/Inestable: IL IDÉNTICO a la .90
    for clase in ('GrimorioHambrientoErratico', 'GrimorioHambrientoInestable'):
        a, b = cecil_dump(DLLPREV, clase), cecil_dump(DLLTMP, clase)
        if a == b and not a.startswith('ERROR'):
            ok(f'{clase}: IL IDÉNTICO a la .90')
        else:
            mal(f'{clase}: cambió respecto de la .90')

    # (d) el Nervioso: la región de las DOS VIDAS
    dmp = cecil_dump(DLLTMP, 'GrimorioHambrientoNervioso')
    nuevas = ['PasoCaza', 'AbsorberPresa', 'HambreEfectos', 'FrasesDeHambre',
              'DecirLocal', 'CobrarXPLibro', 'LibroFuera', 'CaceriaActiva',
              'UpdateInventory', 'ModifyTooltips', 'get_EstadoCompartido']
    for m in nuevas:
        if f'::{m}(' in dmp or f'::{m} (' in dmp:
            ok(f'Nervioso::{m} presente')
        else:
            mal(f'Nervioso::{m} AUSENTE')
    # constantes plegadas (Roslyn pliega los const): 0.5 y 300 y 0.01
    for lit, etiqueta in (('ldc.r4 0.5', 'UMBRAL_IMPACIENCIA 0.5'),
                          ('ldc.i4 300', 'TICKS_QUIETUD 300'),
                          ('ldc.r4 0.01', '−1% por presa (0.01)')):
        if lit in dmp:
            ok(f'constante plegada: {etiqueta}')
        else:
            mal(f'constante {etiqueta} no encontrada en el IL')

    # (e) la base: NerviosoActivo + PasoSinHambre (documentado) — y la
    #     CLASE DEL ÍTEM (GrimorioHambriento) INTACTA vs .90 (solo cambió
    #     EstadoGrimorio, su vecina de archivo)
    dmp_estado = cecil_dump(DLLTMP, 'EstadoGrimorio')
    for m in ('NerviosoActivo', 'PasoSinHambre'):
        if f'::{m}(' in dmp_estado:
            ok(f'EstadoGrimorio::{m} presente (cambio documentado)')
        else:
            mal(f'EstadoGrimorio::{m} AUSENTE')
    base_n = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.GrimorioHambriento')
    base_v = cecil_dump(DLLPREV, 'AethonMod.Content.Weapons.GrimorioHambriento')
    if base_n == base_v and 'ERROR' not in base_n:
        ok('GrimorioHambriento (la clase del ítem): IL IDÉNTICO a la .90')
    else:
        mal('GrimorioHambriento (la clase del ítem) cambió vs .90 — no declarado')
    est_n = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.EstadoGrimorio')
    est_v = cecil_dump(DLLPREV, 'AethonMod.Content.Weapons.EstadoGrimorio')
    if est_n != est_v:
        ok('EstadoGrimorio CAMBIA (esperado: NerviosoActivo + PasoSinHambre + las 3 ramas)')
    else:
        mal('EstadoGrimorio NO cambió — ¿dónde está el umbral del 50%?')

    print(f'=== 6. HJSON — parse + espejo es (contenido) + claves nuevas/muertas + tabs ===')
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

    # el espejo es-ES == es-MX por CONTENIDO (tML re-serializa el paquete)
    if 'es-MX' in textos and 'es-ES' in textos:
        if _json.dumps(textos['es-MX'], sort_keys=True) == _json.dumps(textos['es-ES'], sort_keys=True):
            ok('es-ES ESPEJO EXACTO de es-MX (contenido)')
        else:
            mal('es-ES diverge de es-MX (contenido)')

    # las claves nuevas del Nervioso VIVEN y las del Códice Vivo NO
    for idioma in ('es-MX', 'es-ES', 'en-US'):
        if idioma not in textos:
            continue
        hambre = (textos[idioma].get('Hambre') or {})
        nerv = hambre.get('Nervioso') or {}
        proyectiles = (textos[idioma].get('Projectiles') or {})
        nuevas_ok = (len(nerv) == 13 and
                     'SaleACazar' in nerv and 'Escapa' in nerv and 'Absorbe' in nerv and
                     'GrimorioNerviosoFlotante.DisplayName' in proyectiles and
                     'SombraPaginaCaza.DisplayName' in proyectiles)
        muertas_ok = ('CodiceVivo' not in (textos[idioma].get('Items') or {}) and
                      'CodiceVivoProjectile.DisplayName' not in proyectiles and
                      'CodiceVivoChispa.DisplayName' not in proyectiles)
        if nuevas_ok and muertas_ok:
            ok(f'{idioma}: Hambre.Nervioso (13 claves) + 2 proyectiles vivos, Códice Vivo purgado')
        else:
            mal(f'{idioma}: nuevas={nuevas_ok} purga={muertas_ok}')

    # simetría de claves es-MX ↔ en-US (la familia entera)
    if 'es-MX' in textos and 'en-US' in textos:

        def hojas(d, pref=''):
            s = set()
            for k, v in d.items():
                if isinstance(v, dict):
                    s |= hojas(v, f'{pref}{k}.')
                else:
                    s.add(f'{pref}{k}')
            return s

        kmx, ken = hojas(textos['es-MX']), hojas(textos['en-US'])
        if kmx == ken:
            ok(f'claves simétricas es-MX ↔ en-US ({len(kmx)} hojas)')
        else:
            mal(f'asimetría es↔en: solo-es {sorted(kmx - ken)[:4]} solo-en {sorted(ken - kmx)[:4]}')

    # el en-US del paquete conserva SUS TABS (la marca del árbol de la casa)
    raw_en = blobs['Localization/en-US_Mods.AethonMod.hjson'].decode('utf-8')
    if '\t' in raw_en and raw_en.count('\t') > 4000:
        ok(f'en-US con TABS ({raw_en.count(chr(9))}) — sin normalizar')
    else:
        mal(f'en-US con {raw_en.count(chr(9))} tabs — ¿normalizado por una herramienta?')

    print()
    if FALLOS:
        print(f'✗✗✗ {len(FALLOS)} FALLOS:')
        for f in FALLOS:
            print(f'  - {f}')
        sys.exit(1)
    print(f'✓ AUDITORÍA v6.50.91 COMPLETA — 0 fallos '
          f'({len(d)} B, md5 {md5})')


if __name__ == '__main__':
    main()
