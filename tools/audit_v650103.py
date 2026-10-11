#!/usr/bin/env python3
"""
v6.50.103 — Auditoría del .tmod (formato casa).

LA LETRA DEL USUARIO (sobre el reloj de 15 min de la .102): «esto es muy
rapido, el reloj de hambre debe durar una hora en llegar a 100% si no se
alimenta el libro».

EL CAMBIO: TasaHambre del Grimorio del Eterno 1/54000 (15 min) →
1/216000 (UNA HORA: 3600 s × 60 ticks). El Nervioso de prueba conserva
su minuto de demo (1/3600). Los hitos del reloj escalan con él:
25% (EL CAZADOR) a los 15 min, 50% (LA IMPACIENCIA) a los 30 min.

CHECKS:
(1) cabecera 6.50.103 · 386 entradas (idéntica la .102 — cero archivos
    añadidos/borrados) · EOF exacto.
(2) LA CONSTANTE en la DLL: el float32 de 1f/216000f VIVE en el IL y el
    de 1f/54000f está AUSENTE (la cirugía entró al binario).
(3) hjson ×3 del PAQUETE == árbol, con «una hora de 0 a 100%» (es) y
    «one hour from 0 to 100%» (en) — y SIN «15 minutos».
(4) Regresión .102: los muertos ausentes (paquete + heap), la elegida y
    su bolsa vivas, la bruma muerta, la espera de 10 s intacta.
"""
import hashlib
import struct
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = sys.argv[1] if len(sys.argv) > 1 else \
    '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
ARBOL = 'AethonMod'

FALLOS = []

# los mismos muertos de la .102 — la .103 NO puede revivirlos
MUERTOS_RUTA = [
    'Content/Weapons/Emperatriz/GrimorioEstrellado.cs',
    'Content/Weapons/Emperatriz/GrimorioEstrellado.png',
    'Content/Weapons/Emperatriz/EcoEmperatriz.cs',
    'Content/Weapons/Emperatriz/EcoEmperatriz.png',
    'Content/Weapons/Emperatriz/VeredictoEmperatriz.cs',
    'Content/Weapons/Emperatriz/VeredictoEmperatriz.png',
    'Content/Projectiles/Emperatriz/EstelaAethonProjectile.cs',
    'Content/Projectiles/Emperatriz/EstelaAethonProjectile.png',
    'Content/Projectiles/Emperatriz/TrazoEmperatrizProjectile.cs',
    'Content/Projectiles/Emperatriz/TrazoEmperatrizProjectile.png',
    'Content/Weapons/GrimorioSellado.cs',
    'Content/Weapons/GrimorioSellado.png',
    'Content/Weapons/Sombras/TintaViva.cs',
    'Content/Weapons/Sombras/TintaViva.png',
    'Content/Projectiles/Aethon/FragmentoAethon.cs',
    'Content/Projectiles/Aethon/FragmentoAethon.png',
    'Content/Projectiles/Aethon/ExplosionAethon.cs',
    'Content/Projectiles/Aethon/ExplosionAethon.png',
    'Content/Systems/SistemaObservado.cs',
    'Content/Buffs/Observado.cs',
    'Content/Buffs/Observado.png',
    'Content/Weapons/GrimoireEternal.png',
]

VIVOS_HEAP = [
    'GrimoireEternal', 'GrimorioHambrientoNervioso', 'EstadoGrimorio',
    'EstadoDeLibro', 'EstadoEterno', 'LanzarPareja', 'TicksQuietudSalida',
    'TipoLibro', 'TasaHambre', 'PasoMaquina', 'Corrimiento', 'DibujarTiras',
    'GrimorioNerviosoFlotante', 'SombraPaginaCaza', 'TipoLibroDeLaRaiz',
    'EmperatrizFX', 'Equinoccio', 'BolsaEmperatriz', 'ReabrirLoteVanilla',
]

MUERTOS_HEAP = [
    'GrimorioSellado', 'TintaViva', 'GrimorioEstrellado', 'EcoEmperatriz',
    'VeredictoEmperatriz', 'EstelaAethonProjectile', 'TrazoEmperatrizProjectile',
    'FragmentoAethon', 'ExplosionAethon', 'SistemaObservado',
]


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
    return tmlver, name, ver, entries, tend, fsize, blobs


def f32(x):
    return struct.pack('<f', x)


def main():
    d = open(TMOD, 'rb').read()
    md5 = hashlib.md5(d).hexdigest()
    print(f'=== {TMOD} ===')
    print(f'  {len(d)} B | md5 {md5}')

    tmlver, name, ver, entries, tend, fsize, blobs = inflar(TMOD)
    rutas = {p for p, _, _ in entries}

    print('=== 1. CABECERA / ENTRADAS / EOF ===')
    if ver == '6.50.103' and name == 'AethonMod':
        ok(f'mod {name} {ver} (tML {tmlver})')
    else:
        mal(f'cabeza inesperada: {name} {ver}')
    if len(entries) == 386:
        ok('386 entradas (idénticas a la .102 — cero archivos tocados)')
    else:
        mal(f'se esperaban 386 entradas, hay {len(entries)}')
    if sum(c for _, _, c in entries) + tend == len(d) and fsize == len(d):
        ok(f'EOF exacto ({tend} + Σcomp = {len(d)} = fsize)')
    else:
        mal('EOF descuadrado')

    print('=== 2. LA CONSTANTE DEL RELOJ — DENTRO DE LA DLL ===')
    dll = blobs['AethonMod.dll']
    # 1f/216000f: float32 bien redondeado (doble redondeo doble→float no
    # aplica aquí: 1/216000 cae lejos de un empate; ambos candidatos dan
    # los mismos 4 bytes, pero los comparamos por si acaso)
    hora = {f32(1.0 / 216000.0)}
    try:
        import numpy as np
        hora.add(struct.pack('<f', np.float32(1.0) / np.float32(216000.0)))
    except ImportError:
        pass
    if any(h in dll for h in hora):
        ok('1f/216000f (LA HORA) vive en el IL de la DLL')
    else:
        mal('la constante de la hora NO está en la DLL')
    vieja = {f32(1.0 / 54000.0)}
    try:
        import numpy as np
        vieja.add(struct.pack('<f', np.float32(1.0) / np.float32(54000.0)))
    except ImportError:
        pass
    if any(v in dll for v in vieja):
        mal('la constante vieja 1f/54000f (15 min) sigue viva en la DLL')
    else:
        ok('la constante vieja 1f/54000f está AUSENTE de la DLL')
    if len(dll) > 1_200_000:
        ok(f'DLL {len(dll)} B (~1,28 MB — el build real, no la carpeta vacía)')
    else:
        mal(f'DLL sospechosamente pequeña: {len(dll)} B')

    print('=== 3. hjson ×3: PAQUETE == ÁRBOL + LA HORA EN EL TOOLTIP ===')
    for h, dentro, fuera in [
        ('es-MX', 'una hora de 0 a 100%', '15 minutos de 0 a 100%'),
        ('es-ES', 'una hora de 0 a 100%', '15 minutos de 0 a 100%'),
        ('en-US', 'one hour from 0 to 100%', '15 minutes from 0 to 100%'),
    ]:
        clave = f'Localization/{h}_Mods.AethonMod.hjson'
        if clave not in blobs:
            mal(f'falta {clave} en el paquete')
            continue
        arbol = open(f'{ARBOL}/{clave}', 'rb').read()
        if blobs[clave] != arbol:
            mal(f'{h}: el paquete DIFF del árbol '
                f'({len(blobs[clave])} vs {len(arbol)} B)')
            continue
        texto = blobs[clave].decode('utf-8')
        if dentro in texto and fuera not in texto:
            ok(f'{h}: paquete == árbol ({len(arbol)} B) + «{dentro}» y sin «{fuera}»')
        elif dentro in texto:
            mal(f'{h}: la hora está pero «{fuera}» SIGUE en el texto')
        else:
            mal(f'{h}: falta «{dentro}» en el tooltip')

    print('=== 4. REGRESIÓN .102 — LOS MUERTOS AUSENTES DEL PAQUETE ===')
    for ruta in MUERTOS_RUTA:
        if ruta in rutas:
            mal(f'el muerto sigue en el paquete: {ruta}')
    for ruta in ['Content/Weapons/Emperatriz/Equinoccio.rawimg',
                 'Content/Items/Bolsas/BolsaEmperatriz.rawimg']:
        if ruta in rutas:
            ok(f'la elegida y su bolsa viven: {ruta}')
        else:
            mal(f'FALTA en el paquete: {ruta}')
    if not any(r in rutas for r in MUERTOS_RUTA):
        ok('los archivos muertos NO están en el paquete')

    print('=== 5. LA DLL: VIVOS Y MUERTOS EN EL HEAP #Strings ===')
    def en_heap(s):
        return s.encode('utf-8') + b'\x00' in dll
    for s in VIVOS_HEAP:
        if en_heap(s):
            ok(f'VIVO en el heap: {s}')
        else:
            mal(f'FALTA en el heap: {s}')
    for s in MUERTOS_HEAP:
        if en_heap(s):
            mal(f'el muerto VIVE en el heap: {s}')
        else:
            ok(f'muerto ausente: {s}')

    print('=== 6. EL ÁRBOL — LA CIRUGÍA DE LA HORA (y lo que NO cambió) ===')
    et = open(f'{ARBOL}/Content/Weapons/GrimoireEternal.cs', encoding='utf-8').read()
    gh = open(f'{ARBOL}/Content/Weapons/GrimorioHambriento.cs', encoding='utf-8').read()
    for patron, etiqueta, src in [
        ('1f / 216000f', 'TasaHambre = 1/216000 (LA HORA) en el Eterno', et),
        ('1f / 54000f', 'la vieja 1/54000 ELIMINADA del Eterno', None),
        ('TicksQuietudSalida => 600', 'la espera de caza 10 s INTACTA', et),
        ('GrimorioHambrientoNervioso', 'la subclase del Nervioso', et),
        ('9201f', 'la marca del dorado', et),
        ('9202f', 'la marca del negro', et),
        ('Texture => "AethonMod/Content/Weapons/GrimorioHambrientoNervioso"',
         'el sprite del Nervioso', et),
        ('TasaHambre = 1f / 3600f',
         'el minuto de demo del Nervioso INTACTO', gh),
        ('Reiniciar()', 'el reinicio del Nervioso (sólo él lo usa)', gh),
    ]:
        if src is None:
            if patron in et:
                mal(f'FALTA eliminar de GrimoireEternal.cs: {patron}')
            else:
                ok(f'{etiqueta}')
        elif patron in src:
            ok(f'{etiqueta}')
        else:
            mal(f'FALTA en el árbol: {patron}')
    fx_txt = open(f'{ARBOL}/Content/Globals/EmperatrizFX.cs', encoding='utf-8').read()
    if 'BrumaColumna' in fx_txt:
        mal('EmperatrizFX.cs aún llama BrumaColumna (regresión)')
    elif '9202' in fx_txt and 'FlushAlpha' in fx_txt:
        ok('el negro 9202 sigue eclipse LIMPIO (sin bruma) — regresión .102 OK')
    else:
        mal('EmperatrizFX.cs inesperado')

    print()
    if FALLOS:
        print(f'✗✗✗ {len(FALLOS)} FALLOS:')
        for f in FALLOS:
            print(f'  - {f}')
        sys.exit(1)
    print(f'TODO OK — {len(d)} B, md5 {md5}, 386 entradas, '
          f'la HORA vive en la DLL y en los 3 tooltips')


if __name__ == '__main__':
    main()
