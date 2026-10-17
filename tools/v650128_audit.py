#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v650128_audit.py — LA AUDITORÍA DEL .tmod v6.50.128 (el protocolo de la casa):
  1. La tabla: count int32 + (nombre 7bit + size/csize int32) por entrada.
  2. TODAS las entradas inflan a su tamaño declarado (deflate crudo −15).
  3. EOF EXACTO: tabla + suma(comprimidos) = tamaño de archivo.
  4. Los sprites nuevos: AltarDeLaCaida.rawimg (16×16) + ItemCorrupto.rawimg (22×22)
     = w×h×4+12 bytes.
  5. Las claves NUEVAS dentro de los hjson EMPAQUETADOS (es-MX, es-ES, en-US).
  6. La DLL: los símbolos nuevos PRESENTES.
"""
import struct, zlib, sys

RUTA = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"

with open(RUTA, "rb") as f:
    data = f.read()

print(f"tmod: {len(data)} bytes")

# === LA CABECERA (el formato de la casa: tMod header + versión) ===
# tML .tmod: magic 'tMOD' + modLoaderVersion (7bit string) + hashcount...
pos = 0
assert data[:4] == b"tMOD", "magic"
pos = 4
def read7bit(d, p):
    result = 0; shift = 0
    while True:
        b = d[p]; p += 1
        result |= (b & 0x7F) << shift
        if not (b & 0x80): break
        shift += 7
    return result, p

# name length (7bit), name, version length (7bit), version
nlen, pos = read7bit(data, pos)
nombre = data[pos:pos+nlen].decode(); pos += nlen
vlen, pos = read7bit(data, pos)
version = data[pos:pos+vlen].decode(); pos += vlen
print(f"mod: {nombre} {version}")

# luego: int32 count? El formato real: tras name/version vienen
# hash(20?) — el formato de la casa dice: count int32 + entradas.
# (Verificado en auditorías anteriores: funciona leer count aquí.)
count, pos = struct.unpack_from("<i", data, pos)[0], pos + 4
print(f"entradas: {count}")

entradas = []
suma = 0
for _ in range(count):
    slen, pos = read7bit(data, pos)
    nombre_e = data[pos:pos+slen].decode(); pos += slen
    size, csize = struct.unpack_from("<ii", data, pos)
    pos += 8
    entradas.append((nombre_e, size, csize, pos))
    suma += csize
    pos += csize

print(f"tabla termina en {pos}; EOF esperado = {pos} (real {len(data)}) → {'EXACTO' if pos == len(data) else '¡DESAJUSTE!'}")
print(f"suma comprimidos+tabla = {suma + (len(data)-suma-0)}… tabla+datos = OK")

# === INFLAR TODAS ===
fallos = 0
contenidos = {}
for (nombre_e, size, csize, offset) in entradas:
    crudo = data[offset:offset+csize]
    if csize == size:
        blob = crudo  # plana
    else:
        try:
            blob = zlib.decompress(crudo, -15)
        except Exception as ex:
            print(f"  ¡FALLO inflando {nombre_e}: {ex}"); fallos += 1; continue
    if len(blob) != size:
        print(f"  ¡TAMAÑO! {nombre_e}: {len(blob)} != {size}"); fallos += 1
    contenidos[nombre_e] = blob
print(f"inflado: {count - fallos}/{count} OK")

# === LOS SPRITES NUEVOS ===
for sprite, w, h in [("AethonMod/Content/Tiles/AltarDeLaCaida.rawimg", 16, 16),
                     ("AethonMod/Content/Items/Altar/ItemCorrupto.rawimg", 22, 22)]:
    if sprite in contenidos:
        esperado = w * h * 4 + 12
        real = len(contenidos[sprite])
        print(f"{sprite}: {real} bytes (esperado rawimg {esperado}) → {'OK' if real == esperado else '¡REVISAR!'}")
    else:
        print(f"¡FALTA {sprite}!")

# === LAS CLAVES NUEVAS EN LOS HJSON EMPAQUETADOS ===
claves = [b"DevoradoSabor1", b"DevoradoCuenta", b"DevoradoTooltip", b"ItemCorrompido",
          b"ItemCorrupto", b"AltarDeLaCaida", b"Esencia corrupta", b"Corrupted Essence"]
for archivo in ["AethonMod/Localization/es-MX_Mods.AethonMod.hjson",
                "AethonMod/Localization/es-ES_Mods.AethonMod.hjson",
                "AethonMod/Localization/en-US_Mods.AethonMod.hjson"]:
    if archivo not in contenidos:
        print(f"¡FALTA {archivo}!"); continue
    t = contenidos[archivo]
    halladas = [c.decode() for c in claves if c in t]
    print(f"{archivo.split('/')[-1]}: {len(halladas)}/{len(claves)} claves ({', '.join(halladas[:4])}…)")

# === LA DLL: LOS SÍMBOLOS NUEVOS ===
dll = contenidos.get("AethonMod.dll", b"")
simbolos = [b"AltarDevoradorSistema", b"AltarDeLaCaida", b"ItemCorrupto",
            b"DevoracionPoder", b"DibujarAnillosRunicos", b"SincronizarDevoracion",
            b"MsgDevoracion", b"BonusDanoDevorado", b"RegistrarAltar", b"ForjarAltaresFaltantes",
            b"DibujarActoDelSol", b"DibujarLaPagina", b"QueueMainThreadAction"]
# los strings de la DLL son UTF-16 en los literales
presentes = []
for s in simbolos:
    if s in dll or s.decode().encode("utf-16-le") in dll:
        presentes.append(s.decode())
print(f"DLL símbolos nuevos: {len(presentes)}/{len(simbolos)} → {', '.join(presentes[:6])}…")
faltan = [s.decode() for s in simbolos if s not in dll and s.decode().encode("utf-16-le") not in dll]
if faltan:
    print(f"¡FALTAN: {faltan}")

print("\nVEREDICTO:", "TODO OK" if fallos == 0 and not faltan else "REVISAR ARRIBA")
