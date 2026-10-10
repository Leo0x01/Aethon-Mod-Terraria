#!/usr/bin/env python3
"""
v6.50.93 — Auditoría del .tmod (formato casa).
Uso: python3 tools/audit_v65093.py [ruta-al-.tmod]

Novedades .93 — LA OLEADA ES OTRA ENTIDAD + EL APETITO SIEMPRE CORRE +
LOS ATAQUES DEL LIBRO SAEN DEL LIBRO + UN ATAQUE POR CRIATURA:
(1) «en la oleada el devorador de mundo aparecio y se fue, debes
asegurarte de que los jefes y monstruos invocados en las oleadas no se
vean afectados por los parametros de biomas o climas que sus versiones
originales» — LA CAUSA RAÍZ (decompile): al PARTIR al gusano, vanilla
muta el cuerpo en CABEZA (SetDefaultsKeepPlayerInteraction) y
NPC.SetDefaults DESTRUYE los globals (_globals=null): el sello moría con
la instancia y la cabeza nueva, SIN sello, era enterrada por su IA
(«¿nadie en la Corrupción?») hasta APAGAR la cadena (active=false
directo). CURA DOBLE: EL REGISTRO (SelloVivo por whoAmI + re-adopción en
SetDefaults) + EL PRÉSTAMO DE ZONA A TODA LA MESA (sin target: TODOS los
vivos reciben la zona del guardián durante su AI — y los DevourerHead
escupidos también leen la Corrupción prestada).
(2) «el libro solo sale a cazar de 25% de hambre en adelante» —
UMBRAL_CACERIA 0.25 (antes 0–50%); con el libro FUERA y el hambre al
100%, el espíritu rompe y HUYE ÉL solo (IrAFuga).
(3) «si el jugador ataque y el libro esta fuera sus ataques salen del
libro no del jugador […] el jugador tenia una copia del libro en la
mano» — OrigenDelDisparo virtual (la descarga nace del ESPÍRITU flotante)
+ LA MANO VACÍA (ModifyItemDraw no dibuja NADA mientras el libro está
fuera).
(4) «el nivel de hambre debe subir independientemente el libro case o
no […] su hambre se congela, el hambre no debe congelarse» —
PasoSinHambre ELIMINADO: el reloj corre SIEMPRE (tras la gracia de 10 s).
(5) «su ataque vuelve a lanzarce justo cuando mata a la criatura […]
solo se debe lanzar una vez su ataque hasta que la criatura muera» — LA
DIGESTIÓN: MarcarDigestion/DigestionLista (TICKS_DIGESTION 120) — tras
comer una presa, el espíritu espera antes de lanzar la siguiente sombra.
ENTRADAS: 384 (las mismas de la .92 — sin archivos nuevos ni muertos).
Whitelist del blob diff: dll/pdb/Info + los 3 hjson — NADA MÁS.
GrimorioFuriaSistema: IL IDÉNTICO a la .92 (el sistema de oleadas no se
tocó — sólo su sello). CAMBIAN (documentado): OleadaNPC,
EstadoGrimorio+clase-ítem (mismo archivo), Nervioso, espíritu y sombra.
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
PREV = '/home/sync/AethonMod-v6.50.92.tmod'
CECIL = '/home/z/tmp_scripts/cecil_check/bin/Debug/net8.0/cecil_check.dll'
DOTNET = '/tmp/sdk/dotnet'
DLLTMP = '/tmp/audit_v65093.dll'
DLLPREV = '/tmp/audit_v65093_prev.dll'

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
    if ver == '6.50.93' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.93)')

    print(f'=== 2. EOF EXACTO (doble fórmula) ===')
    total_comp = sum(c for _, _, c in entries)
    tend = parse(TMOD)[4]
    if tend + total_comp == fsize:
        ok(f'table_end {tend} + sum(comp) {total_comp} == {fsize}')
    else:
        mal(f'EOF: {tend}+{total_comp} != {fsize}')

    print(f'=== 3. ENTRADAS vs .92 — EL MISMO SET (384) ===')
    _, _, verprev, eprev, _, blobs_prev = inflar(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    fuera = sorted(set_prev - set_now)
    dentro = sorted(set_now - set_prev)
    print(f'    .92: {len(eprev)} entradas | .93: {len(entries)} entradas')
    if len(entries) == len(eprev) and not fuera and not dentro:
        ok(f'{len(entries)} entradas — el MISMO set de la .92')
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

    print(f'=== 5. CECIL — la DLL de la .93 vs la de la .92 ===')
    open(DLLTMP, 'wb').write(blobs['AethonMod.dll'])
    open(DLLPREV, 'wb').write(blobs_prev['AethonMod.dll'])

    # (a) LA BASE: PasoSinHambre MUERE, la gracia de los 10 s VIVE
    dmp_estado = cecil_dump(DLLTMP, 'EstadoGrimorio')
    if '::PasoSinHambre(' in dmp_estado:
        mal('EstadoGrimorio::PasoSinHambre VIVE — la congelación no murió')
    else:
        ok('EstadoGrimorio::PasoSinHambre ELIMINADO (el reloj corre siempre)')
    for campo in ('UltimaComida', 'FocoPantalla', 'TICKS_GRACIA_COMIDA'):
        if campo in dmp_estado:
            ok(f'EstadoGrimorio.{campo} presente (la gracia/foco de la .92)')
        else:
            mal(f'EstadoGrimorio.{campo} AUSENTE')
    if 'ldc.i4 600' in dmp_estado:
        ok('constante plegada: GRACIA 600 sigue en EstadoGrimorio::Paso')
    else:
        mal('GRACIA 600 no está en el IL de EstadoGrimorio')
    est_n = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.EstadoGrimorio')
    est_v = cecil_dump(DLLPREV, 'AethonMod.Content.Weapons.EstadoGrimorio')
    if est_n != est_v:
        ok('EstadoGrimorio CAMBIA (esperado: PasoSinHambre fuera)')
    else:
        mal('EstadoGrimorio NO cambió — ¿dónde murió el paso sin hambre?')

    # (b) LA CLASE DEL ÍTEM: el disparo nace de un ORIGEN virtual
    dmp_item = cecil_dump(DLLTMP, 'AethonMod.Content.Weapons.GrimorioHambriento')
    if '::OrigenDelDisparo(' in dmp_item:
        ok('GrimorioHambriento::OrigenDelDisparo presente (el origen virtual)')
    else:
        mal('GrimorioHambriento::OrigenDelDisparo AUSENTE')
    item_n = dmp_item
    item_v = cecil_dump(DLLPREV, 'AethonMod.Content.Weapons.GrimorioHambriento')
    if item_n != item_v:
        ok('la clase-ítem GrimorioHambriento CAMBIA (esperado: OrigenDelDisparo)')
    else:
        mal('la clase-ítem NO cambió — ¿dónde está el origen del disparo?')

    # (c) EL NERVIOSO: 25% + mano vacía + disparo del libro + digestión
    dmp = cecil_dump(DLLTMP, 'GrimorioHambrientoNervioso')
    for m in ('PasoCaza', 'AbsorberPresa', 'LibroFuera', 'CaceriaActiva',
              'FocoDelFestin', 'UpdateInventory', 'ModifyTooltips',
              'PosicionDelLibro', 'OrigenDelDisparo', 'ModifyItemDraw'):
        if f'::{m}(' in dmp or f'::{m} (' in dmp:
            ok(f'Nervioso::{m} presente')
        else:
            mal(f'Nervioso::{m} AUSENTE')
    if 'UMBRAL_CACERIA' in dmp:
        ok('Nervioso.UMBRAL_CACERIA presente (el umbral del 25%)')
    else:
        mal('Nervioso.UMBRAL_CACERIA AUSENTE')
    # constantes plegadas (Roslyn pliega los const): 0.25 / 300 / 0.01
    for lit, etiqueta in (('ldc.r4 0.25', 'UMBRAL_CACERIA 0.25'),
                          ('ldc.i4 300', 'TICKS_QUIETUD 300'),
                          ('ldc.r4 0.01', '−1% por presa (0.01)')):
        if lit in dmp:
            ok(f'constante plegada: {etiqueta}')
        else:
            mal(f'constante {etiqueta} no encontrada en el IL')
    if 'ldc.r4 0.5' in dmp:
        ok('constante plegada: UMBRAL_IMPACIENCIA 0.5 (sigue en pie)')
    else:
        mal('UMBRAL_IMPACIENCIA 0.5 no está en el IL del Nervioso')
    # la fase nueva del tooltip
    if 'Hambriento' in dmp:
        ok('la fase «Hambriento» vive en el IL del tooltip')
    else:
        mal('la fase «Hambriento» no aparece en el IL')
    # la digestión se marca al absorber
    if 'MarcarDigestion' in dmp:
        ok('AbsorberPresa llama a MarcarDigestion (la pausa entre presas)')
    else:
        mal('AbsorberPresa NO marca la digestión')
    # el PASO SIN HAMBRE no se llama jamás
    if 'PasoSinHambre' in dmp:
        mal('el Nervioso sigue llamando a PasoSinHambre')
    else:
        ok('el Nervioso ya NO llama a PasoSinHambre (reloj siempre vivo)')
    nerv_n = cecil_dump(DLLTMP, 'GrimorioHambrientoNervioso')
    nerv_v = cecil_dump(DLLPREV, 'GrimorioHambrientoNervioso')
    if nerv_n != nerv_v:
        ok('GrimorioHambrientoNervioso CAMBIA (esperado: 25%+mano+digestión)')
    else:
        mal('GrimorioHambrientoNervioso NO cambió')

    # (d) LA SOMBRA: la digestión (reloj por dueño)
    dmp_sombra = cecil_dump(DLLTMP, 'SombraPaginaCaza')
    for m in ('MarcarDigestion', 'DigestionLista'):
        if f'::{m}(' in dmp_sombra or f'::{m} (' in dmp_sombra:
            ok(f'SombraPaginaCaza::{m} presente')
        else:
            mal(f'SombraPaginaCaza::{m} AUSENTE')
    if 'TICKS_DIGESTION' in dmp_sombra and ('ldc.i4.s 120' in dmp_sombra or 'ldc.i4 120' in dmp_sombra):
        ok('SombraPaginaCaza.TICKS_DIGESTION presente + 120 plegado (ldc.i4.s)')
    else:
        mal('TICKS_DIGESTION/120 no están en el IL de la sombra')
    som_n = dmp_sombra
    som_v = cecil_dump(DLLPREV, 'SombraPaginaCaza')
    if som_n != som_v:
        ok('SombraPaginaCaza CAMBIA (esperado: el reloj de la digestión)')
    else:
        mal('SombraPaginaCaza NO cambió')

    # (e) EL ESPÍRITU: la digestión respeta + la fuga en vivo
    dmp_esp = cecil_dump(DLLTMP, 'GrimorioNerviosoFlotante')
    if '::IrAFuga(' in dmp_esp:
        ok('GrimorioNerviosoFlotante::IrAFuga presente (la caza se harta)')
    else:
        mal('GrimorioNerviosoFlotante::IrAFuga AUSENTE')
    if 'DigestionLista' in dmp_esp:
        ok('el espíritu respeta DigestionLista antes de lanzar sombra')
    else:
        mal('el espíritu NO consulta la digestión')
    esp_v = cecil_dump(DLLPREV, 'GrimorioNerviosoFlotante')
    if dmp_esp != esp_v:
        ok('GrimorioNerviosoFlotante CAMBIA (esperado: IrAFuga + digestión)')
    else:
        mal('GrimorioNerviosoFlotante NO cambió')

    # (f) EL SELLO DE LAS OLEADAS: registro + re-adopción + mesa prestada
    dmp_ole = cecil_dump(DLLTMP, 'OleadaNPC')
    for token, etiqueta in (
        ('SelloVivo', 'la clase del registro'),
        ('_sellados', 'el diccionario del registro'),
        ('TTL_SELLO', 'el TTL del sello'),
        ('_presasDeZonas', 'la MESA de zonas prestadas'),
    ):
        if token in dmp_ole:
            ok(f'OleadaNPC: {etiqueta} presente')
        else:
            mal(f'OleadaNPC: {etiqueta} AUSENTE')
    if 'ldc.i4 900' in dmp_ole:
        ok('constante plegada: TTL_SELLO 900 en OleadaNPC')
    else:
        mal('TTL 900 no está en el IL de OleadaNPC')
    # EL DEVORADOR ESCUPIDO (NPCID.DevourerHead = 7) llega plegado al IL:
    # dentro del CUERPO de PreAI deben convivir los literales del chequeo
    # de corrupción (13 = EaterofWorldsHead, 7 = DevourerHead — ldc.i4.7
    # es la forma corta del 7)
    def bloque_metodo(dump, nombre):
        lineas, dentro = [], False
        for ln in dump.splitlines():
            if ln.startswith('METHOD'):
                dentro = f'::{nombre}(' in ln
                continue
            if dentro:
                lineas.append(ln)
        return '\n'.join(lineas)
    preai = bloque_metodo(dmp_ole, 'PreAI')
    if 'ldc.i4.s 13' in preai and 'ldc.i4.7' in preai:
        ok('PreAI presta la Corrupción también al Devorador escupido (7) y al jefe (13)')
    else:
        mal('el chequeo 7|13 del préstamo de corrupción no está en el IL de PreAI')
    ole_v = cecil_dump(DLLPREV, 'OleadaNPC')
    if dmp_ole != ole_v:
        ok('OleadaNPC CAMBIA (esperado: registro + zonas a toda la mesa)')
    else:
        mal('OleadaNPC NO cambió — ¿dónde está el registro?')

    # (g) EL SISTEMA DE OLEADAS: INTACTO (sólo cambió su sello)
    fur_n = cecil_dump(DLLTMP, 'GrimorioFuriaSistema')
    fur_v = cecil_dump(DLLPREV, 'GrimorioFuriaSistema')
    if fur_n == fur_v and not fur_n.startswith('ERROR'):
        ok('GrimorioFuriaSistema IL IDÉNTICO a la .92 (intacto)')
    else:
        mal('GrimorioFuriaSistema cambió — esta versión NO lo tocaba')

    print(f'=== 6. HJSON — parse + espejo es (contenido) + claves nuevas + tabs ===')
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

    # las claves del Nervioso VIVEN (14 con la fase Hambriento)
    for idioma in ('es-MX', 'es-ES', 'en-US'):
        if idioma not in textos:
            continue
        hambre = (textos[idioma].get('Hambre') or {})
        nerv = hambre.get('Nervioso') or {}
        items = (textos[idioma].get('Items') or {})
        proyectiles = (textos[idioma].get('Projectiles') or {})
        nuevas_ok = (len(nerv) == 14 and 'Hambriento' in nerv and
                     'Cazando' in nerv and 'Escapa' in nerv and 'Absorbe' in nerv and
                     'GrimorioNerviosoFlotante.DisplayName' in proyectiles and
                     'SombraPaginaCaza.DisplayName' in proyectiles)
        familia_ok = ('GrimorioHambriento' in items and
                      'GrimorioHambrientoNervioso' in items)
        muertas_ok = ('GrimorioHambrientoErratico' not in items and
                      'GrimorioHambrientoInestable' not in items and
                      'CodiceVivo' not in items)
        if nuevas_ok and familia_ok and muertas_ok:
            ok(f'{idioma}: familia de DOS + Hambre.Nervioso (14 claves con Hambriento), difuntos purgados')
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
    print(f'✓ AUDITORÍA v6.50.93 COMPLETA — 0 fallos '
          f'({len(d)} B, md5 {md5})')


if __name__ == '__main__':
    main()
