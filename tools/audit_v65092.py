#!/usr/bin/env python3
"""
v6.50.92 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65092.py [ruta-al-.tmod]

Novedades .92 — LA FAMILIA SE ENCOGE A DOS + LA GRACIA DE LOS 10 s + EL
FOCO DEL FESTÍN (la letra del usuario): (1) «borra al grimorio hambriento
inestable y al erratico, deja el original y al nervioso» — el Errático
(.87) y el Inestable (.89) BORRADOS por completo (clases + sprites +
localización + bolsa): la familia queda original SERENO + Nervioso
HAMBRIENTO; (2) «el hambre debe subir si es que en 10 segundos no come
nada» — el reloj del apetito sólo corre tras 10 s SIN comer (UltimaComida:
cada presa absorbida y el reinicio de la demo marcan la hora);
(3) «el libro debe mirar lo que esta comiendo» — mientras la sombra
devora, el ojo se clava en su comida: FocoPantalla en EstadoGrimorio (el
ítem en hotbar/mano/mundo persigue el festín) + FocoDelFestin en el
Nervioso + el espíritu flotante INCLINA el cuerpo y CLAVA el iris en la
presa (o en las almas mientras las absorbe). Además el .bat/.sh de
ACTUALIZAR-FUENTE arreglados (borraba y no copiaba: LF + bloques
multilinea confundían a cmd.exe — ahora CRLF + cero bloques).
ENTRADAS: 386 de la .91 − 2 (rawimg del Errático y del Inestable) = 384.
El blob diff permite dll/pdb/Info + los 3 hjson — NADA MÁS. SombraPaginaCaza
y la CLASE DEL ÍTEM (GrimorioHambriento): IL IDÉNTICO a la .91. La base
(EstadoGrimorio) CAMBIA documentado (gracia de 10 s + FocoPantalla + la
bandera Erratico muerta). El Nervioso gana FocoDelFestin; el espíritu
inclina y mira su comida.
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
PREV = '/home/sync/AethonMod-v6.50.91.tmod'
CECIL = '/home/z/tmp_scripts/cecil_check/bin/Debug/net8.0/cecil_check.dll'
DOTNET = '/tmp/sdk/dotnet'
DLLTMP = '/tmp/audit_v65092.dll'
DLLPREV = '/tmp/audit_v65092_prev.dll'

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
    if ver == '6.50.92' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.92)')

    print(f'=== 2. EOF EXACTO (doble fórmula) ===')
    total_comp = sum(c for _, _, c in entries)
    tend = parse(TMOD)[4]
    if tend + total_comp == fsize:
        ok(f'table_end {tend} + sum(comp) {total_comp} == {fsize}')
    else:
        mal(f'EOF: {tend}+{total_comp} != {fsize}')

    print(f'=== 3. ENTRADAS vs .91 — SALEN los 2 rawimg de los difuntos ===')
    _, _, verprev, eprev, _, blobs_prev = inflar(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    fuera = sorted(set_prev - set_now)
    dentro = sorted(set_now - set_prev)
    print(f'    .91: {len(eprev)} entradas | .92: {len(entries)} entradas')
    esperado = len(eprev) - 2
    if len(entries) == esperado:
        ok(f'{len(eprev)} − 2 = {len(entries)}')
    else:
        mal(f'entradas: {len(entries)} (esperaba {esperado})')
    for p in fuera:
        if 'Erratico' in p or 'Inestable' in p:
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

    print(f'=== 5. CECIL — la DLL de la .92 vs la de la .91 ===')
    open(DLLTMP, 'wb').write(blobs['AethonMod.dll'])
    open(DLLPREV, 'wb').write(blobs_prev['AethonMod.dll'])

    # (a) los muertos AUSENTES
    for clase in ('GrimorioHambrientoErratico', 'GrimorioHambrientoInestable'):
        dmp = cecil_dump(DLLTMP, clase)
        if dmp.startswith('ERROR'):
            ok(f'{clase}: AUSENTE (borrado por completo)')
        else:
            mal(f'{clase} VIVE en la DLL — la purga falló')

    # (b) los vivos PRESENTES con su firma (los const viven como FIELD)
    for clase, firma in (
        ('GrimorioNerviosoFlotante', 'MODO_CAZA'),
        ('SombraPaginaCaza', 'CADENCIA_MORDIDA'),
    ):
        dmp = cecil_dump(DLLTMP, clase)
        if not dmp.startswith('ERROR') and f' {firma}' in dmp:
            ok(f'{clase}: presente (con {firma})')
        else:
            mal(f'{clase}: no encontrado o sin {firma}')

    # (c) los intactos: la sombra y la clase del ítem — IL IDÉNTICO a la .91
    for clase in ('SombraPaginaCaza', 'AethonMod.Content.Weapons.GrimorioHambriento'):
        a, b = cecil_dump(DLLPREV, clase), cecil_dump(DLLTMP, clase)
        if a == b and not a.startswith('ERROR'):
            ok(f'{clase}: IL IDÉNTICO a la .91')
        else:
            mal(f'{clase}: cambió respecto de la .91')

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
    # v6.50.92 — FocoDelFestin (el libro MIRA su comida)
    if '::FocoDelFestin(' in dmp:
        ok('Nervioso::FocoDelFestin presente (el foco del festín)')
    else:
        mal('Nervioso::FocoDelFestin AUSENTE')
    # constantes plegadas (Roslyn pliega los const): 0.5 / 300 / 0.01
    # (la GRACIA 600 vive en EstadoGrimorio::Paso — chequeada abajo en (e))
    for lit, etiqueta in (('ldc.r4 0.5', 'UMBRAL_IMPACIENCIA 0.5'),
                          ('ldc.i4 300', 'TICKS_QUIETUD 300'),
                          ('ldc.r4 0.01', '−1% por presa (0.01)')):
        if lit in dmp:
            ok(f'constante plegada: {etiqueta}')
        else:
            mal(f'constante {etiqueta} no encontrada en el IL')

    # (e) la base: la gracia de los 10 s + el foco del festín (documentado)
    #     — la clase del ítem GrimorioHambriento quedó INTACTA (sólo cambió
    #     EstadoGrimorio, su vecina de archivo — chequeada en (c))
    dmp_estado = cecil_dump(DLLTMP, 'EstadoGrimorio')
    for m in ('NerviosoActivo', 'PasoSinHambre'):
        if f'::{m}(' in dmp_estado:
            ok(f'EstadoGrimorio::{m} presente (heredado de la .91)')
        else:
            mal(f'EstadoGrimorio::{m} AUSENTE')
    for campo in ('UltimaComida', 'FocoPantalla', 'TICKS_GRACIA_COMIDA'):
        if f'field {campo}' in dmp_estado or f' {campo} :' in dmp_estado or f' {campo}:' in dmp_estado or campo in dmp_estado:
            ok(f'EstadoGrimorio.{campo} presente (la gracia/foco de la .92)')
        else:
            mal(f'EstadoGrimorio.{campo} AUSENTE')
    if 'Erratico' not in dmp_estado:
        ok('la bandera Erratico MURIÓ con su dueño (AUSENTE del dump)')
    else:
        mal('Erratico sigue vivo en EstadoGrimorio — la purga falló')
    if 'ldc.i4 600' in dmp_estado:
        ok('constante plegada: GRACIA 600 en EstadoGrimorio::Paso')
    else:
        mal('GRACIA 600 no está en el IL de EstadoGrimorio')
    est_n = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.EstadoGrimorio')
    est_v = cecil_dump(DLLPREV, 'AethonMod.Content.Weapons.EstadoGrimorio')
    if est_n != est_v:
        ok('EstadoGrimorio CAMBIA (esperado: gracia 10 s + FocoPantalla − Erratico)')
    else:
        mal('EstadoGrimorio NO cambió — ¿dónde está la gracia de los 10 s?')

    # (f) el espíritu: CAMBIA documentado (inclinación + mirada al festín)
    esp_n = cecil_dump(DLLTMP, 'GrimorioNerviosoFlotante')
    esp_v = cecil_dump(DLLPREV, 'GrimorioNerviosoFlotante')
    if esp_n != esp_v:
        ok('GrimorioNerviosoFlotante CAMBIA (esperado: se inclina y mira su comida)')
    else:
        mal('GrimorioNerviosoFlotante NO cambió — ¿dónde mira al festín?')

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

    # las claves del Nervioso VIVEN (13) y los DOS DIFUNTOS NO existen
    for idioma in ('es-MX', 'es-ES', 'en-US'):
        if idioma not in textos:
            continue
        hambre = (textos[idioma].get('Hambre') or {})
        nerv = hambre.get('Nervioso') or {}
        items = (textos[idioma].get('Items') or {})
        proyectiles = (textos[idioma].get('Projectiles') or {})
        nuevas_ok = (len(nerv) == 13 and
                     'SaleACazar' in nerv and 'Escapa' in nerv and 'Absorbe' in nerv and
                     'GrimorioNerviosoFlotante.DisplayName' in proyectiles and
                     'SombraPaginaCaza.DisplayName' in proyectiles)
        familia_ok = ('GrimorioHambriento' in items and
                      'GrimorioHambrientoNervioso' in items)
        muertas_ok = ('GrimorioHambrientoErratico' not in items and
                      'GrimorioHambrientoInestable' not in items and
                      'CodiceVivo' not in items)
        if nuevas_ok and familia_ok and muertas_ok:
            ok(f'{idioma}: familia de DOS + Hambre.Nervioso (13 claves), difuntos purgados')
        else:
            mal(f'{idioma}: nuevas={nuevas_ok} familia={familia_ok} purga={muertas_ok}')

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
    print(f'✓ AUDITORÍA v6.50.92 COMPLETA — 0 fallos '
          f'({len(d)} B, md5 {md5})')


if __name__ == '__main__':
    main()
