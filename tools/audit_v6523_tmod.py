#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v6522_tmod.py — LA AUDITORÍA DEL .tmod v6.52.3 (el protocolo de la casa).
EL FORMATO TMOD v2 (decodificado del binario — tML 2026.08):
  "TMOD" + 7bit tMLversion + hash(20) + firma(256) + int32 dataLength
  + 7bit nombre + 7bit versión + int32 count
  + TABLA: count × { 7bit nombre, int32 size, int32 csize }
  + BLOB: los datos concatenados EN EL ORDEN DE LA TABLA.
"""
import struct, zlib, sys

RUTA = "/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"

with open(RUTA, "rb") as f:
    data = f.read()
print(f"tmod: {len(data)} bytes")

def r7(d, p):
    r = 0; s = 0
    while True:
        b = d[p]; p += 1
        r |= (b & 0x7F) << s
        if not (b & 0x80): break
        s += 7
    return r, p

assert data[:4] == b"TMOD", "magic"
p = 4
vlen, p = r7(data, p)
tmlver = data[p:p+vlen].decode(); p += vlen
p += 20 + 256    # hash + firma
dataLen, = struct.unpack_from("<i", data, p); p += 4
nlen, p = r7(data, p)
nombre = data[p:p+nlen].decode(); p += nlen
mvl, p = r7(data, p)
version = data[p:p+mvl].decode(); p += mvl
count, = struct.unpack_from("<i", data, p); p += 4
print(f"mod: {nombre} {version} (tML {tmlver}, dataLen {dataLen}, {count} entradas)")
assert version == "6.52.3", f"versión equivocada: {version}"

# LA TABLA COMPLETA primero
tabla = []
for _ in range(count):
    slen, p = r7(data, p)
    ne = data[p:p+slen].decode(); p += slen
    size, csize = struct.unpack_from("<ii", data, p)
    p += 8
    tabla.append((ne, size, csize))
tablaFin = p
print(f"tabla: {count} entradas ({tablaFin} B) — el blob de datos sigue")

# EL BLOB: datos concatenados en el orden de la tabla
fallos = 0
contenidos = {}
off = tablaFin
for (ne, size, csize) in tabla:
    crudo = data[off:off+csize]
    if csize == size:
        blob = crudo
    else:
        try:
            blob = zlib.decompress(crudo, -15)
        except Exception as ex:
            print(f"  ¡FALLO inflando {ne}: {ex}"); fallos += 1; off += csize; continue
    if len(blob) != size:
        print(f"  ¡TAMAÑO! {ne}: {len(blob)} != {size}"); fallos += 1
    contenidos[ne] = blob
    off += csize
print(f"blob termina en {off} de {len(data)} → {'EXACTO' if off == len(data) else '¡DESAJUSTE!'}")
assert off == len(data) and fallos == 0

errores = 0
almacenadas = sum(1 for (_, s, c) in tabla if s == c)
comprimidas = count - almacenadas
print(f"compresión: {comprimidas} deflate + {almacenadas} planas")

# === 1. LOS SPRITES NUEVOS (6) + RuidoTinta V2 ===
# EL FORMATO rawimg (v2): int32 bandera + int32 ancho + int32 alto + BGRA
for clave, (we, he) in [("BolsaDelDiosAntiguo", (30, 30)), ("RayoDeFusion", (30, 30)),
                        ("LaserArcoiris", (30, 30)), ("DestructorDePrismas", (30, 30)),
                        ("TruenoDeGelatina", (30, 30)), ("MasaCromatica", (22, 22))]:
    hits = [ne for ne in contenidos if clave in ne and ne.endswith(".rawimg")]
    if not hits:
        print(f"  ¡FALTA el sprite {clave}!"); errores += 1
    else:
        blob = contenidos[hits[0]]
        bandera, w, h = struct.unpack_from("<iii", blob, 0)
        ok = w == we and h == he and len(blob) == 12 + w * h * 4
        print(f"  {'OK ' if ok else '¡MAL!'} {clave}: {hits[0]} ({w}x{h}, {len(blob)} B)")
        if not ok: errores += 1

rt = [ne for ne in contenidos if "RuidoTinta" in ne]
if not rt:
    print("  ¡FALTA RuidoTinta!"); errores += 1
else:
    blob = contenidos[rt[0]]
    bandera, w, h = struct.unpack_from("<iii", blob, 0)
    px = blob[12:]
    borde = px[0]
    centro = px[(h // 2) * w * 4 + (w // 2) * 4]
    print(f"  OK RuidoTinta V2: {w}x{h}, borde V R={borde} ({'CERO ✓' if borde < 8 else '¡NO CERO!'}), centro R={centro}")
    if borde >= 8: errores += 1

# === 2. LAS CLAVES NUEVAS en los hjson EMPAQUETADOS ===
claves = ["SombraDeLaPagina3: {", "SombraDeLaPagina4: {", "RayoDeFusion: {",
          "LaserArcoiris: {", "DestructorDePrismas: {", "TruenoDeGelatina: {",
          "MasaCromatica: {"]
for lang in ("es-MX", "es-ES", "en-US"):
    ruta_hjson = f"Localization/{lang}_Mods.AethonMod.hjson"
    if ruta_hjson not in contenidos:
        print(f"  ¡FALTA {ruta_hjson}!"); errores += 1; continue
    texto = contenidos[ruta_hjson].decode("utf-8")
    faltan = [c for c in claves if c not in texto]
    viejo = "ALTAR DE LA CAÍDA" in texto or "FALLING ALTAR" in texto
    if faltan or viejo:
        print(f"  ¡{lang}: faltan {faltan} / viejo={'SÍ' if viejo else 'no'}!"); errores += 1
    else:
        print(f"  OK {lang}: 7 claves nuevas + tooltip viejo muerto")

# === 3. LA DLL: los símbolos nuevos PRESENTES ===
dll = contenidos.get("AethonMod.dll")
if dll is None:
    print("  ¡FALTA la DLL!"); errores += 1
else:
    simbolos = [b"HotbarLibreSistema", b"ColorGoozma", b"SombraPagina3Projectile",
                b"SombraPagina4Projectile", b"SombraDeLaPagina3", b"SombraDeLaPagina4",
                b"RayoFusionProjectile", b"LaserArcoirisProjectile",
                b"PrismaDestructorProjectile", b"TruenoGelatinaProjectile",
                b"GolpeFusion", b"BolsaDelDiosAntiguo", b"FajaTexturadaEnLote",
                b"InclinacionPar", b"LazoPaciencia", b"QuitarReboteIzquierdo"]
    faltan = [s.decode() for s in simbolos if s not in dll]
    if faltan:
        for f in faltan: print(f"  ¡FALTA símbolo {f}!")
        errores += len(faltan)
    else:
        print(f"  OK los 16 símbolos nuevos ({len(dll)} B de DLL)")

print()
if errores == 0:
    print(f"AUDITORÍA v6.52.3: TODO LIMPIO ({count} entradas, {len(data)} B)")
else:
    print(f"AUDITORÍA v6.52.3: {errores} ERRORES")
    sys.exit(1)
