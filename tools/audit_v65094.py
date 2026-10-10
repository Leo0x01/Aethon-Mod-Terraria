#!/usr/bin/env python3
"""
v6.50.94 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65094.py [ruta-al-.tmod]

Novedades .94 — EL RELOJ SIEMPRE + EL COMPÁS DE LA SOMBRA + LA FUGA MUERTA
+ EL FESTÍN EGOÍSTA + EL ESPÍRITU ÚNICO:
(1) «el hambre se pausa» — LA GRACIA DE LOS 10 s DE LA .92 MURIÓ:
    EstadoGrimorio sin UltimaComida ni TICKS_GRACIA_COMIDA — Paso() suma
    SIEMPRE (1/3600 por tick). El bocado sólo RESTA (−1%), no congela.
(2) «su ataque sigue relanzandose al matar a una criatura» — EL COMPÁS:
    un ataque cada 10 s (MarcarAtaque al NACER la sombra) y NADA antes de
    2 s de TERMINAR la anterior (SombraTerminada en OnKill). La digestión
    de la .93 (marcada en la ABSORCIÓN, a mitad del ataque) murió.
(3) «la fuga simplemente la quitamos el libro no se fuga, solo queda
    flotando cerca del jugador cazando por si mismo» — MODO_FUGA/IrAFuga/
    spawn de fuga/frases Escapa/Fugitivo ELIMINADOS. Al 100% sigue cazando.
(4) «esto de la caza por su cuenta es cuando el jugador esta en afk o
    simplemente lleva mucho tiempo sin atacar criaturas con el libro» —
    TICKS_SIN_ATAQUE_LIBRO 1800 (30 s sin disparar con el libro TAMBIÉN
    lo saca) + TICKS_MARGEN_ATAQUE 300 (mientras el jugador dispara, el
    espíritu calla sus sombras — JugadorAtacoReciente).
(5) «el festin compartido no tiene sentido […] el libro debe ser egoista
    y comer toda la criatura loot incluido por eso el libro recibe la exp
    y el jugador nada» — CobrarXPLibro cobra al NERVIOSO (ShardLevelItem
    ahora le aplica); el Grimorio del Eterno, el latido y el jugador NO
    reciben NADA.
(6) LA COPIA DEL SPRITE — EL ESPÍRITU ÚNICO: SoyDuplicado() disuelve al
    segundo espíritu del mismo dueño (gana el MODO_CAZA; a igual modo, el
    más antiguo) — jamás dos libros flotando, los ataques salen del que
    es.
ENTRADAS: 384 (las mismas de la .93). Whitelist del blob diff:
dll/pdb/Info + los 3 hjson. CAMBIAN (documentado): EstadoGrimorio,
Nervioso, espíritu, sombra y ShardLevelItem. IDÉNTICOS esperados:
GrimorioHambriento (clase-ítem), OleadaNPC, GrimorioFuriaSistema.
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
PREV = '/home/sync/AethonMod-v6.50.93.tmod'
CECIL = '/home/z/tmp_scripts/cecil_check/bin/Debug/net8.0/cecil_check.dll'
DOTNET = '/tmp/sdk/dotnet'
DLLTMP = '/tmp/audit_v65094.dll'
DLLPREV = '/tmp/audit_v65094_prev.dll'

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
    if ver == '6.50.94' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.94)')

    print(f'=== 2. EOF EXACTO (doble fórmula) ===')
    total_comp = sum(c for _, _, c in entries)
    tend = parse(TMOD)[4]
    if tend + total_comp == fsize:
        ok(f'table_end {tend} + sum(comp) {total_comp} == {fsize}')
    else:
        mal(f'EOF: {tend}+{total_comp} != {fsize}')

    print(f'=== 3. ENTRADAS vs .93 — EL MISMO SET (384) ===')
    _, _, verprev, eprev, _, blobs_prev = inflar(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    fuera = sorted(set_prev - set_now)
    dentro = sorted(set_now - set_prev)
    print(f'    .93: {len(eprev)} entradas | .94: {len(entries)} entradas')
    if len(entries) == len(eprev) and not fuera and not dentro:
        ok(f'{len(entries)} entradas — el MISMO set de la .93')
    else:
        for p in fuera:
            mal(f'SALE inesperado: {p}')
        for p in dentro:
            mal(f'ENTRA inesperado: {p}')

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

    print(f'=== 5. CECIL — la DLL de la .94 vs la de la .93 ===')
    open(DLLTMP, 'wb').write(blobs['AethonMod.dll'])
    open(DLLPREV, 'wb').write(blobs_prev['AethonMod.dll'])

    # (a) LA BASE: LA GRACIA MUERE, EL RELOJ SUMA SIEMPRE
    dmp_estado = cecil_dump(DLLTMP, 'EstadoGrimorio')
    for campo in ('UltimaComida', 'TICKS_GRACIA_COMIDA', 'PasoSinHambre'):
        if campo in dmp_estado:
            mal(f'EstadoGrimorio.{campo} VIVE — tenía que morir')
        else:
            ok(f'EstadoGrimorio.{campo} ELIMINADO')
    for vivo in ('FocoPantalla', '::Paso(', '::Reiniciar('):
        if vivo in dmp_estado:
            ok(f'EstadoGrimorio{vivo} presente')
        else:
            mal(f'EstadoGrimorio{vivo} AUSENTE')
    # la GRACIA 600 ya NO vive en el IL de Paso (la suma 1/3600 sí)
    def bloque_metodo(dump, nombre):
        lineas, dentro = [], False
        for ln in dump.splitlines():
            if ln.startswith('METHOD'):
                dentro = f'::{nombre}(' in ln
                continue
            if dentro:
                lineas.append(ln)
        return '\n'.join(lineas)
    paso_il = bloque_metodo(dmp_estado, 'Paso')
    if 'ldc.i4 600' in paso_il or 'ldc.i4.s 600' in paso_il:
        mal('la GRACIA 600 sigue viva en el IL de Paso')
    else:
        ok('Paso SIN la gracia de 600 — el reloj suma SIEMPRE')
    # la tasa 1/3600 llega PLEGADA por Roslyn (ldc.r4 0.00027777778 — la
    # lección de la .93: los const/literales nunca viajan como se escriben)
    if 'ldc.r4 0.0002777' in paso_il:
        ok('Paso con la tasa 1/3600 plegada (0→100% en un minuto)')
    else:
        mal('la tasa 1/3600 (plegada) no aparece en el IL de Paso')
    est_n = dmp_estado
    est_v = cecil_dump(DLLPREV, 'AethonMod.Content.Weapons.EstadoGrimorio')
    if est_n != est_v:
        ok('EstadoGrimorio CAMBIA (esperado: la gracia fuera)')
    else:
        mal('EstadoGrimorio NO cambió — ¿dónde murió la gracia?')

    # (b) LA CLASE-ÍTEM BASE: INTACTA (la .94 no la tocó)
    dmp_item = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.GrimorioHambriento')
    item_v = cecil_dump(DLLPREV, 'AethonMod.Content.Weapons.GrimorioHambriento')
    if '::OrigenDelDisparo(' in dmp_item:
        ok('GrimorioHambriento::OrigenDelDisparo sigue presente')
    else:
        mal('GrimorioHambriento::OrigenDelDisparo AUSENTE')
    if dmp_item == item_v:
        ok('la clase-ítem GrimorioHambriento IL IDÉNTICA a la .93 (intacta)')
    else:
        mal('la clase-ítem cambió — esta versión NO la tocaba')

    # (c) EL NERVIOSO: Shoot + caza del ausente + sin fuga + nivel en tooltip
    dmp = cecil_dump(DLLTMP, 'GrimorioHambrientoNervioso')
    for m in ('PasoCaza', 'AbsorberPresa', 'LibroFuera', 'CaceriaActiva',
              'FocoDelFestin', 'UpdateInventory', 'ModifyTooltips',
              'PosicionDelLibro', 'OrigenDelDisparo', 'ModifyItemDraw',
              '::Shoot(', 'JugadorAtacoReciente', 'CobrarXPLibro'):
        if f'::{m}(' in dmp or f'::{m} (' in dmp or m in dmp:
            ok(f'Nervioso: {m} presente')
        else:
            mal(f'Nervioso: {m} AUSENTE')
    for muerto in ('MarcarDigestion', 'DigestionLista', 'Fugitivo', 'Escapa',
                   'MODO_FUGA', 'UltimaComida'):
        if muerto in dmp:
            mal(f'Nervioso: {muerto} VIVE — tenía que morir')
        else:
            ok(f'Nervioso: {muerto} ELIMINADO')
    # constantes plegadas (Roslyn pliega los const)
    for lit, etiqueta in (('ldc.r4 0.25', 'UMBRAL_CACERIA 0.25'),
                          ('ldc.i4 1800', 'TICKS_SIN_ATAQUE_LIBRO 1800 (30 s)'),
                          ('ldc.i4 300', 'TICKS_QUIETUD/MARGEN 300'),
                          ('ldc.r4 0.01', '−1% por presa (0.01)'),
                          ('ldc.r4 0.5', 'UMBRAL_IMPACIENCIA 0.5')):
        if lit in dmp:
            ok(f'constante plegada: {etiqueta}')
        else:
            mal(f'constante {etiqueta} no encontrada en el IL')
    # el tooltip muestra el nivel del libro (la XP egoísta)
    if 'NivelNervioso' in dmp and 'Nervioso.Nivel' in dmp:
        ok('el tooltip del Nervioso muestra SU nivel (Hambre.Nervioso.Nivel)')
    else:
        mal('la línea de nivel del Nervioso no está en el IL')
    nerv_v = cecil_dump(DLLPREV, 'GrimorioHambrientoNervioso')
    if dmp != nerv_v:
        ok('GrimorioHambrientoNervioso CAMBIA (esperado: la letra .94)')
    else:
        mal('GrimorioHambrientoNervioso NO cambió')

    # (d) LA SOMBRA: el compás (10 s al nacer + 2 s al morir)
    dmp_sombra = cecil_dump(DLLTMP, 'SombraPaginaCaza')
    for m in ('MarcarAtaque', 'AtaqueListo', 'SombraTerminada', 'OnKill'):
        if f'::{m}(' in dmp_sombra or f'::{m} (' in dmp_sombra:
            ok(f'SombraPaginaCaza::{m} presente')
        else:
            mal(f'SombraPaginaCaza::{m} AUSENTE')
    for muerto in ('MarcarDigestion', 'DigestionLista', 'TICKS_DIGESTION'):
        if muerto in dmp_sombra:
            mal(f'SombraPaginaCaza: {muerto} VIVE — la digestión murió')
        else:
            ok(f'SombraPaginaCaza: {muerto} ELIMINADO')
    for lit, etiqueta in (('ldc.i4 600', 'TICKS_ENTRE_ATAQUES 600 (10 s)'),
                          ('ldc.i4.s 120', 'TICKS_TRAS_TERMINAR 120 (2 s)')):
        if lit in dmp_sombra:
            ok(f'constante plegada: {etiqueta}')
        else:
            mal(f'constante {etiqueta} no está en el IL de la sombra')
    som_v = cecil_dump(DLLPREV, 'SombraPaginaCaza')
    if dmp_sombra != som_v:
        ok('SombraPaginaCaza CAMBIA (esperado: el compás del ataque)')
    else:
        mal('SombraPaginaCaza NO cambió')

    # (e) EL ESPÍRITU: SIN FUGA + ÚNICO + compás respetado
    dmp_esp = cecil_dump(DLLTMP, 'GrimorioNerviosoFlotante')
    for muerto in ('IrAFuga', 'MODO_FUGA', 'DigestionLista'):
        if muerto in dmp_esp:
            mal(f'GrimorioNerviosoFlotante: {muerto} VIVE — la fuga murió')
        else:
            ok(f'GrimorioNerviosoFlotante: {muerto} ELIMINADO')
    for vivo in ('SoyDuplicado', 'MarcarAtaque', 'AtaqueListo',
                 'JugadorAtacoReciente', 'MODO_VOLVER', 'IrAVolver'):
        if vivo in dmp_esp:
            ok(f'GrimorioNerviosoFlotante: {vivo} presente')
        else:
            mal(f'GrimorioNerviosoFlotante: {vivo} AUSENTE')
    esp_v = cecil_dump(DLLPREV, 'GrimorioNerviosoFlotante')
    if dmp_esp != esp_v:
        ok('GrimorioNerviosoFlotante CAMBIA (esperado: fuga fuera + único)')
    else:
        mal('GrimorioNerviosoFlotante NO cambió')

    # (f) EL NIVEL DEL NERVIOSO: ShardLevelItem lo adopta
    dmp_sli = cecil_dump(DLLTMP, 'ShardLevelItem')
    if 'GrimorioHambrientoNervioso' in dmp_sli:
        ok('ShardLevelItem adopta al Nervioso (la XP es del libro que come)')
    else:
        mal('ShardLevelItem NO referencia al Nervioso')
    sli_v = cecil_dump(DLLPREV, 'ShardLevelItem')
    if dmp_sli != sli_v:
        ok('ShardLevelItem CAMBIA (esperado: el Nervioso entra al nivel)')
    else:
        mal('ShardLevelItem NO cambió')

    # (g) OLEADAS Y FURIA: INTACTAS
    ole_n = cecil_dump(DLLTMP, 'OleadaNPC')
    ole_v = cecil_dump(DLLPREV, 'OleadaNPC')
    if ole_n == ole_v and not ole_n.startswith('ERROR'):
        ok('OleadaNPC IL IDÉNTICO a la .93 (intacto)')
    else:
        mal('OleadaNPC cambió — esta versión NO lo tocaba')
    fur_n = cecil_dump(DLLTMP, 'GrimorioFuriaSistema')
    fur_v = cecil_dump(DLLPREV, 'GrimorioFuriaSistema')
    if fur_n == fur_v and not fur_n.startswith('ERROR'):
        ok('GrimorioFuriaSistema IL IDÉNTICO a la .93 (intacto)')
    else:
        mal('GrimorioFuriaSistema cambió — esta versión NO lo tocaba')

    print(f'=== 6. HJSON — parse + espejo es (contenido) + claves .94 + tabs ===')
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

    # las claves del Nervioso .94: 13 (sin Fugitivo ni Escapa, con Nivel)
    for idioma in ('es-MX', 'es-ES', 'en-US'):
        if idioma not in textos:
            continue
        hambre = (textos[idioma].get('Hambre') or {})
        nerv = hambre.get('Nervioso') or {}
        items = (textos[idioma].get('Items') or {})
        proyectiles = (textos[idioma].get('Projectiles') or {})
        nuevas_ok = (len(nerv) == 13 and 'Hambriento' in nerv and
                     'Cazando' in nerv and 'Absorbe' in nerv and 'Nivel' in nerv and
                     'Fugitivo' not in nerv and 'Escapa' not in nerv and
                     'GrimorioNerviosoFlotante.DisplayName' in proyectiles and
                     'SombraPaginaCaza.DisplayName' in proyectiles)
        familia_ok = ('GrimorioHambriento' in items and
                      'GrimorioHambrientoNervioso' in items)
        muertas_ok = ('GrimorioHambrientoErratico' not in items and
                      'GrimorioHambrientoInestable' not in items and
                      'CodiceVivo' not in items)
        if nuevas_ok and familia_ok and muertas_ok:
            ok(f'{idioma}: Nervioso 13 claves (con Nivel, sin fuga) + familia de DOS')
        else:
            mal(f'{idioma}: nuevas={nuevas_ok} familia={familia_ok} purga={muertas_ok} '
                f'claves={sorted(nerv)}')

    # el tooltip del ítem NO promete fuga NI gracia
    if 'es-MX' in textos:
        tt = ((textos['es-MX'].get('Items') or {}).get('GrimorioHambrientoNervioso') or {}).get('Tooltip', '')
        if 'NUNCA se escapa' in tt and 'cada 10 s' in tt and 'SIEMPRE' in tt:
            ok('el tooltip es-MX canta la letra .94 (nunca escapa / 10 s / siempre sube)')
        else:
            mal('el tooltip es-MX no refleja la letra .94')
        if '10 segundos sin comer' in tt or 'calma 10 s' in tt:
            mal('el tooltip es-MX aún promete la gracia de los 10 s')
        else:
            ok('el tooltip es-MX sin la gracia de los 10 s (murió)')

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

    # el hjson del PAQUETE == el del ÁRBOL (byte a byte)
    for idioma in ('es-MX', 'es-ES', 'en-US'):
        p = f'Localization/{idioma}_Mods.AethonMod.hjson'
        arbol = open(f'AethonMod/{p}', 'rb').read()
        if arbol == blobs[p]:
            ok(f'{idioma}: paquete == árbol (byte a byte)')
        else:
            mal(f'{idioma}: el paquete diverge del árbol')

    print()
    if FALLOS:
        print(f'✗✗✗ {len(FALLOS)} FALLOS:')
        for f in FALLOS:
            print(f'  - {f}')
        sys.exit(1)
    print(f'✓ AUDITORÍA v6.50.94 COMPLETA — 0 fallos '
          f'({len(d)} B, md5 {md5})')


if __name__ == '__main__':
    main()
