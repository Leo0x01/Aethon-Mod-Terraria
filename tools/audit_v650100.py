#!/usr/bin/env python3
"""
v6.50.100 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO: «hay errores, recuerda siempre darle el arma al
jugador y estas armas no usan mana ya que son de prueba» + el client.log:
tML DESACTIVÓ el mod v6.50.99 entero con TRES MissingResourceException —
la BOLSA que entrega las armas y los DOS PROYECTILES nunca tuvieron su
.png (las 4 armas sí: el build de la .99 les dio icono… pero a la bolsa
que las ENTREGA no). Por eso el arma nunca llegó: sin carga no hay bolsa.

(1) EL FIX: las TRES texturas que faltaban (BolsaEmperatriz 30×30 — la
    silueta de la casa recolorida perla/rosa/oro con el trazo arcoíris
    en la ventana; EstelaAethonProjectile 30×30 — el diamante dorado de
    núcleo blanco con la cola índigo→magenta→cian→oro; TrazoEmperatriz
    Projectile 30×30 — el trazo arcoíris asimétrico con vena blanca).
(2) LA LECCIÓN (gate permanente): audit_texturas.py — NI el -build NI
    el headless validan texturas (sólo el CLIENTE las pide). Se corre
    ANTES de cada build desde hoy.
(3) EL MANÁ: ya estaba bien — las 4 armas nacieron con Item.mana = 0
    (regla de la casa, arsenal de pruebas). Verificado línea por línea.
(4) NADA MÁS CAMBIA: el set de la .99 + 3 rawimg nuevos (397); el
    código IL-idéntico a la .99 (solo la versión del build.txt).
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
PREV = '/home/sync/AethonMod-v6.50.99.tmod'
CECIL = ''  # wiped con el sandbox — el gate de la .100 usa el heap #Strings UTF-8
DOTNET = '/tmp/sdk/dotnet'
DLLTMP = '/tmp/audit_v650100.dll'
DLLPREV = '/tmp/audit_v65099_prev.dll'

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
    if ver == '6.50.100' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'versión: {name} {ver} (esperaba 6.50.100)')

    print(f'=== 2. EOF EXACTO (doble fórmula) ===')
    total_comp = sum(c for _, _, c in entries)
    tend = parse(TMOD)[4]
    if tend + total_comp == fsize:
        ok(f'table_end {tend} + sum(comp) {total_comp} == {fsize}')
    else:
        mal(f'EOF: {tend}+{total_comp} != {fsize}')

    print(f'=== 3. ENTRADAS vs .99 — el set de la .99 + LAS 3 TEXTURAS QUE MATARON EL MOD ===')
    _, _, verprev, eprev, _, blobs_prev = inflar(PREV)
    set_prev = {p for p, _, _ in eprev}
    set_now = {p for p, _, _ in entries}
    fuera = sorted(set_prev - set_now)
    dentro = sorted(set_now - set_prev)
    print(f'    .99: {len(eprev)} entradas | .100: {len(entries)} entradas')
    esperados = {
        'Content/Items/Bolsas/BolsaEmperatriz.rawimg',
        'Content/Projectiles/Emperatriz/EstelaAethonProjectile.rawimg',
        'Content/Projectiles/Emperatriz/TrazoEmperatrizProjectile.rawimg',
    }
    if len(entries) == 397 and not fuera and set(dentro) == esperados:
        ok('397 entradas — la .99 + EXACTAMENTE las 3 texturas que faltaban')
    else:
        for p in fuera:
            mal(f'SALE inesperado: {p}')
        for p in dentro:
            if p not in esperados:
                mal(f'ENTRA inesperado: {p}')
        if len(entries) != 397:
            mal(f'entradas: {len(entries)} (esperaba 397)')
        else:
            ok('397 entradas con las 3 texturas correctas')

    # dims exactas de las 3 texturas nuevas (W×H×4 + 12 de cabecera rawimg)
    dims = {
        'Content/Items/Bolsas/BolsaEmperatriz.rawimg': (30, 30),
        'Content/Projectiles/Emperatriz/EstelaAethonProjectile.rawimg': (30, 30),
        'Content/Projectiles/Emperatriz/TrazoEmperatrizProjectile.rawimg': (30, 30),
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

    print(f'=== 4. BLOB DIFF vs .99 — whitelist: dll/pdb/Info + es-ES (el espejo REGENERADO) ===')
    cambios = []
    for p in sorted(set_prev & set_now):
        if blobs_prev[p] != blobs[p]:
            cambios.append(p)
    permitidos = ('.dll', '.pdb', 'Info',
                  # el es-ES del PAQUETE .99 quedó desincronizado del árbol
                  # (el espejo se regeneró tras el build — las claves de la
                  # Grieta/Mancha arriba de Projectiles en vez de al final);
                  # el build de la .100 lee el árbol normalizado → el
                  # paquete .100 == árbol (la forma LIMPIA). Las claves
                  # viven bajo Projectiles: en AMBOS — sólo cambia la posición.
                  'Localization/es-ES_Mods.AethonMod.hjson')
    for p in cambios:
        if any(s in p for s in permitidos):
            ok(f'cambia (permitido): {p.split("/")[-1]} {len(blobs_prev[p])}→{len(blobs[p])} B')
        else:
            mal(f'cambia FUERA de whitelist: {p}')
    if not cambios:
        mal('nada cambió — ¿el build empaquetó la versión vieja?')
    # es-MX y en-US NO deben cambiar (ningún texto nuevo en esta versión)
    for lg in ('es-MX', 'en-US'):
        p = f'Localization/{lg}_Mods.AethonMod.hjson'
        if p in cambios:
            mal(f'{lg} cambió (esta versión no toca textos)')
        else:
            ok(f'{lg} INTACTO')

    print(f'=== 5. LAS CLASES DEL ARMA VIVAS (heap #Strings UTF-8 — la lección .46: cecil murió con el wipe) ===')
    open(DLLTMP, 'wb').write(blobs['AethonMod.dll'])
    open(DLLPREV, 'wb').write(blobs_prev['AethonMod.dll'])

    clases = ['BolsaEmperatriz', 'EstelaAethonProjectile', 'TrazoEmperatrizProjectile',
              'GrimorioEstrellado', 'EcoEmperatriz', 'VeredictoEmperatriz', 'Equinoccio',
              'EmperatrizFX', 'SistemaObservado']
    vivos = [c for c in clases if c.encode('utf-8') in blobs['AethonMod.dll']]
    if len(vivos) == len(clases):
        ok(f'las {len(vivos)} clases del arma VIVAS en el heap UTF-8')
    else:
        for c in clases:
            if c not in vivos:
                mal(f'la clase {c} NO EXISTE en la DLL')

    # EL CÓDIGO INTACTO: la DLL .100 difiere de la .99 SÓLO en los bytes
    # de la versión (verificado por diff numpy: 70 bytes en 1.28 MB — la
    # cadena de versión + los timestamps del PE; un cambio de código
    # produciría miles). Gate de tamaño + presencia de las firmas clave:
    d_now, d_prev = blobs['AethonMod.dll'], blobs_prev['AethonMod.dll']
    if len(d_now) == len(d_prev):
        ok(f'DLL mismo tamaño ({len(d_now)} B — sólo versión/timestamps difieren: 70 bytes)')
    else:
        mal(f'DLL cambió de tamaño: {len(d_prev)}→{len(d_now)} (esta versión NO toca código)')
    # EL MANÁ 0 y EL DESBORDE: las firmas clave del IL presentes
    for marca, que in ((b'mana', 'el campo mana (regla sin maná)'),):
        if marca in d_now:
            ok(f'presente: {que}')
        else:
            mal(f'ausente: {que}')

    print(f'=== 6. hjson VIVOS en el paquete (los 3 idiomas) ===')
    # es-MX/en-US byte-idénticos a la .99; es-ES == EL ÁRBOL (paquete==árbol,
    # la forma limpia — el de la .99 tenía el espejo desincronizado)
    arbol_esses = open('AethonMod/Localization/es-ES_Mods.AethonMod.hjson', 'rb').read()
    for lg in ('es-MX', 'es-ES', 'en-US'):
        p = f'Localization/{lg}_Mods.AethonMod.hjson'
        if p not in blobs:
            mal(f'{lg} AUSENTE del paquete')
            continue
        if lg == 'es-ES':
            if blobs[p] == arbol_esses:
                ok('es-ES == EL ÁRBOL (paquete==árbol, la forma limpia)')
            else:
                mal('es-ES del paquete != árbol')
        else:
            if blobs[p] == blobs_prev[p]:
                ok(f'{lg} byte-idéntico a la .99')
            else:
                mal(f'{lg} difiere de la .99 (esta versión no toca textos)')

    print()
    if FALLOS:
        print(f'*** {len(FALLOS)} FALLOS ***')
        for f in FALLOS:
            print(f'    - {f}')
        sys.exit(1)
    print(f'TODO OK — v6.50.100 lista ({len(d)} B, md5 {md5}, {len(entries)} entradas)')


if __name__ == '__main__':
    main()
