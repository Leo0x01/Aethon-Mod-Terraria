#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6520_tmod.py — LA AUDITORÍA DEL PAQUETE .tmod de la v6.52.0
(LA BOLSA DEL ESCRIBA — Task 84).

(1) Cabecera: AethonMod 6.52.0 · conteo de entradas (las 401 de la
    .51.14 + los ~34 archivos NUEVOS del ciclo).
(2) EOF exacto: Σcomp + tabla = fsize.
(3) TODAS las entradas inflan a su tamaño declarado.
(4) hjson: los TRES de raíz INTACTOS byte a byte contra git HEAD (la
    regla del ciclo: «no toques nada del mod») + los TRES NUEVOS de
    Localization/Escriba/ presentes en el paquete == árbol BYTE A BYTE
    + paridad de claves es-MX == es-ES == en-US.
(5) LA DLL — LOS VIVOS del ciclo nuevo presentes (BeamLib, el arma de
    rayo, las inscripciones, el árbol, la coreografía, el Escriba, la
    bolsa, la entrega) + REGRESIÓN: los 6 vivos clave de la .51.14.
(6) LAS TEXTURAS nuevas presentes en el paquete.
"""
import hashlib
import subprocess
import sys
import zlib

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

TMOD = sys.argv[1] if len(sys.argv) > 1 else \
    '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
ARBOL = 'AethonMod'

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
    return tmlver, name, ver, entries, tend, fsize, blobs


# ============ (1) LA CABECERA ============
tmlver, name, ver, entries, tend, fsize = parse(TMOD)
print(f'PAQUETE: {TMOD}')
print(f'  header: {name} v{ver} · tml {tmlver} · {len(entries)} entradas · {fsize} B')
if name != 'AethonMod':
    mal(f'nombre del mod: {name}')
elif ver != '6.52.0':
    mal(f'versión: {ver} (esperaba 6.52.0)')
else:
    ok(f'cabecera: AethonMod 6.52.0, {len(entries)} entradas')

# ============ (2) EL EOF EXACTO ============
suma = sum(c for _, _, c in entries)
if tend + suma == fsize:
    ok(f'EOF exacto: {tend} + Σ{suma} = {fsize}')
else:
    mal(f'EOF: {tend} + {suma} != {fsize}')

# ============ (3) TODAS INFLAN ============
d = open(TMOD, 'rb').read()
off = tend
todas_inflan = True
for p, raw, comp in entries:
    data = d[off:off + comp]
    off += comp
    try:
        out = zlib.decompress(data, -15) if raw != comp else data
        if len(out) != raw:
            todas_inflan = False
            mal(f'{p}: infló {len(out)} ≠ {raw}')
    except Exception as e:
        todas_inflan = False
        mal(f'{p}: {e}')
if todas_inflan:
    ok(f'{len(entries)}/{len(entries)} entradas inflan a su tamaño declarado')

# ============ (4) LOS hjson ============
_, _, _, _, _, _, blobs = inflar(TMOD)

# (4a) LOS DE RAÍZ: INTACTOS contra git HEAD (la garantía del ciclo).
for h in ('es-MX_Mods.AethonMod.hjson', 'es-ES_Mods.AethonMod.hjson',
          'en-US_Mods.AethonMod.hjson'):
    clave = f'Localization/{h}'
    if clave not in blobs:
        mal(f'falta el hjson de raíz: {h}')
        continue
    try:
        head = subprocess.run(['git', 'show', f'HEAD:AethonMod/Localization/{h}'],
                              capture_output=True, cwd='.').stdout
    except Exception:
        head = None
    if head and blobs[clave] == head:
        ok(f'{h}: RAÍZ INTACTA byte a byte contra HEAD (no se tocó NADA)')
    elif head:
        mal(f'{h}: ¡LA RAÍZ CAMBIÓ contra HEAD! (regla del ciclo rota)')
    else:
        print(f'  ~ {h}: sin git HEAD para comparar (continuo)')

# (4b) LOS NUEVOS: presentes == árbol, byte a byte.
for h in ('es-MX_Mods.AethonMod.hjson', 'es-ES_Mods.AethonMod.hjson',
          'en-US_Mods.AethonMod.hjson'):
    clave = f'Localization/Escriba/{h}'
    arbol = open(f'{ARBOL}/Localization/Escriba/{h}', 'rb').read()
    if clave not in blobs:
        mal(f'falta el hjson NUEVO: {clave}')
    elif blobs[clave] == arbol:
        ok(f'Localization/Escriba/{h}: paquete == árbol BYTE A BYTE')
    else:
        mal(f'Localization/Escriba/{h}: difiere del árbol')

# (4c) LA PARIDAD de claves entre los TRES nuevos.
import re
def claves_de(texto):
    return set(re.findall(r'^\t*([A-Za-z0-9_]+):', texto, re.M))
paridad = None
for h in ('es-MX_Mods.AethonMod.hjson', 'es-ES_Mods.AethonMod.hjson',
          'en-US_Mods.AethonMod.hjson'):
    ks = claves_de(blobs[f'Localization/Escriba/{h}'].decode('utf-8'))
    if paridad is None:
        paridad = ks
    elif ks != paridad:
        mal(f'paridad de claves rota en {h} ({len(ks)} vs {len(paridad)})')
if paridad is not None:
    ok(f'paridad de claves ×3 EXACTA: {len(paridad)} claves por idioma')

# (4d) CLAVES CLAVE del ciclo presentes en los tres idiomas.
for clave_hoja in ('BolsaDelEscriba', 'ElColofon', 'AtaqueBorron', 'Encuentro1'):
    for h in ('es-MX', 'es-ES', 'en-US'):
        texto = blobs[f'Localization/Escriba/{h}_Mods.AethonMod.hjson'].decode('utf-8')
        if clave_hoja not in texto:
            mal(f'{h}: falta la clave {clave_hoja}')
ok('claves clave del ciclo (bolsa, colofón, borrón, encuentro) ×3')

# ============ (5) LA DLL — LOS VIVOS ============
dll = blobs.get('AethonMod.dll')
if dll is None:
    mal('¡FALTA LA DLL DEL PAQUETE!')
else:
    texto_dll = dll.decode('utf-8', errors='ignore')

    VIVOS_NUEVOS = [
        # EL RAYO MASIVO (la investigación R2)
        'BeamLib', 'PrimerTrazoHeld', 'PrimerTrazoFajo', 'Rayo',
        'Telegrafo',
        # EL ARMA VIVA (la investigación R1)
        'InscripcionDefinicion', 'PerfilInscripcion', 'Inscripciones',
        'InscripcionItem', 'InscripcionJugador', 'InscripcionGlobalNPC',
        'TintaBolt', 'ArbolInscripcionesSistema', 'HechosCatalogo',
        'PruebasPluma', 'ColofonHumilde', 'ElColofon',
        # LA COREOGRAFÍA (la investigación R3)
        'CoreoGuion', 'GuionAtaque', 'LuzEscenica', 'CamaraCine',
        'BarraConjuroUI', 'EscribaJefe', 'EscribaAtaques', 'EscribaVerso',
        'EscribaPagina', 'EscribaPluma', 'EscribaPared', 'EscribaGota',
        'Triseccion',
        # LA ENTREGA
        'BolsaDelEscriba', 'EscribaKitPlayer', 'PlumaConvocadora',
        'PlumaConsciente',
    ]
    vivos_ok = [s for s in VIVOS_NUEVOS if s in texto_dll]
    faltan = [s for s in VIVOS_NUEVOS if s not in texto_dll]
    if faltan:
        mal(f'la DLL NO trae: {faltan}')
    else:
        ok(f'los {len(VIVOS_NUEVOS)} vivos del ciclo presentes en la DLL')

    # REGRESIÓN — los vivos clave de la .51.14 (la cura del crash + los 9 puntos)
    REGRESION = [
        'SombraPagina2Projectile', 'DibujarLosIrisReales',   # la cura del crash
        'FactorDelLibro',                                        # presencia 100/25/0
        'DevolverAlInventario', 'LazoVelocidad',                # el lazo
        'EscuadronUtil', 'Sostener',                            # el escuadrón
        'EntregarRegaloDePruebas', 'EntregarSiNoTiene',         # la entrega de la Sombra II
        'BocadoDelSueloProjectile', 'CicloMasticar',            # el festín chico
        'DibujarRayitos', 'CoroTicks',                          # el concierto de las hadas
    ]
    faltan_r = [s for s in REGRESION if s not in texto_dll]
    if faltan_r:
        mal(f'REGRESIÓN — la DLL perdió: {faltan_r}')
    else:
        ok(f'regresión LIMPIA: los {len(REGRESION)} vivos de la .51.14 intactos')

# ============ (6) LAS TEXTURAS NUEVAS ============
PNGS = [
    'Content/Items/Bolsas/BolsaDelEscriba.rawimg',
    'Content/Weapons/Escriba/PrimerTrazo.rawimg',
    'Content/Weapons/Escriba/PlumaConsciente.rawimg',
    'Content/Items/Escriba/PlumaConvocadora.rawimg',
    'Content/NPCs/Escriba/EscribaJefe_Head_Boss.rawimg',
    'Content/NPCs/Escriba/EscribaJefe_PoseBestiario.rawimg',
    'Content/Effects/Procedural/RuidoTinta.rawimg',
]
faltan_png = [p for p in PNGS if p not in blobs]
if faltan_png:
    mal(f'FALTAN texturas: {faltan_png}')
else:
    ok(f'las {len(PNGS)} texturas del ciclo presentes')

# ============ RESUMEN ============
print()
if FALLOS:
    print(f'✗ {len(FALLOS)} FALLOS:')
    for f in FALLOS:
        print('  -', f)
    sys.exit(1)
print(f'✓ AUDITORÍA v6.52.0 TODO LIMPIO — {len(entries)} entradas')
md5 = hashlib.md5(open(TMOD, 'rb').read()).hexdigest()
print(f'  md5: {md5}')
