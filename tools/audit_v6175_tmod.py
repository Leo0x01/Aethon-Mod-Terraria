#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""audit_v6175_tmod.py — AUDITORÍA DEL .TMOD v6.175 (el patrón de la casa).

Con el parser canónico parse_tmod.py: identidad, EOF exacto, blobs, las
FIRMAS VIVAS de la versión (el loop de retroalimentación · el motor de
combate de la arena · la barra del jefe interior · el coro de sombras ·
la aspiradora del Devorador · el tajo sakuga · el el plano del haz),
las muertas ausentes (LineaDelCielo purge), hjson ×3 y las claves
nuevas en los tres idiomas.
"""
import importlib.util
import os
import sys

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
TMOD = os.environ.get(
    "TMOD",
    os.path.expanduser("~/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"))

mp_spec = importlib.util.spec_from_file_location(
    "parse_tmod", os.path.join(REPO, "tools", "parse_tmod.py"))
mp = importlib.util.module_from_spec(mp_spec)
mp_spec.loader.exec_module(mp)

import zlib

d = open(TMOD, "rb").read()
tmlver, name, ver, entries, tend, fsize = mp.parse(TMOD)
print(f"(0) {name} v{ver} · tML {tmlver} · {len(entries)} entradas · {fsize:,} B")
ok_hasta_aqui = (ver == "6.175")
if not ok_hasta_aqui:
    print("VERSIÓN MAL — no es la 6.175")
    sys.exit(1)

bl, off = {}, tend
for path, raw, comp in entries:
    trozo = d[off:off + comp]
    blob = trozo if comp == raw else zlib.decompress(trozo, -15)
    bl[path] = blob
    off += comp
print(f"(1) EOF exacto: {'SI' if off == len(d) else 'NO'} ({off} vs {len(d)})")

dll = next(b for k, b in bl.items() if k.endswith(".dll"))
assert dll[:2] == b"MZ"

def vive(txt):
    return txt.encode("utf-16-le") in dll or txt.encode() in dll

# === LAS FIRMAS VIVAS DE LA v6.175 ===
firmas = [
    # EL LOOP DE RETROALIMENTACIÓN (la causa de los botones muertos v6.173)
    "RegistroSistema",            # el registro de la casa
    "Fallo",                      # su núcleo (catch → Registro.Fallo)
    # EL MOTOR DE COMBATE DE LA ARENA (jefes interiores potenciados)
    "MotorDeCombate",             # dash + voleas + combos
    "EjecutarPasoDeCombo",        # los tres pasos del combo
    "DispararVolea",              # la volea temática
    "TelegraphActivo",            # el aviso del dash
    "JefeInteriorVoleaProjectile",# el proyectil temático (nuevo)
    # LA BARRA DEL JEFE INTERIOR (el desborde + el número total)
    "BarraJefeInterior",          # la ModBossBar propia
    # EL CORO DE SOMBRAS (devorar con N sombras)
    "ContarSombras",              # el conteo nivel+despertar
    # LA ASPIRADORA DEL DEVORADOR (la 2ª invocación)
    "AspiradoraDevoradorProjectile",  # el cine espacial
    "MarcarInvocacionInterior",   # el contador de invocaciones
    "EsGusanoInvocado",           # la devoración normal desactivada
    # EL TAJO SAKUGA (el arma recreada)
    "SakugaDelCielo",             # el proyectil nuevo
    # EL HAZ CONCENTRADO (la recreación de la técnica)
    "el haz concentrado",          # el jefe nuevo
    "EcoProyectado",              # su ítem de llamada
    # LA ENTREGA DEL FARO
    "FaroEntregado",              # la clave del aviso de entrega
    # LAS VENTANAS PEGADAS + EL CHECKLIST SIMPLE
    "GeoTrio",                    # el selector anclado a la ranura
]
todas_vivas = True
for f in firmas:
    ok = vive(f)
    todas_vivas = todas_vivas and ok
    print(f"  firma {f}: {'VIVE' if ok else 'AUSENTE (MAL)'}")

# === LAS MUERTAS (la purga de la v6.175) ===
muertas = ["LineaDelCielo"]      # el tajo viejo murió (recreado como SakugaDelCielo)
todas_muertas = True
for m in muertas:
    ok = vive(m)
    todas_muertas = todas_muertas and (not ok)
    print(f"  muerta {m}: {'AUN VIVE (revisar)' if ok else 'muerta ✔'}")

# el PNG viejo del tajo tampoco puede viajar
png_viejo = [k for k in bl if "LineaDelCielo" in k]
print(f"  png LineaDelCielo en el .tmod: {len(png_viejo)} (deben ser 0)")
todas_muertas = todas_muertas and len(png_viejo) == 0

# === LOS SPRITES NUEVOS DENTRO (los que TIENEN PNG propio — SakugaDelCielo
#     dibuja con SoftGlow procedural: NO lleva rawimg, su firma vive arriba) ===
sprites = ["el haz concentrado.rawimg", "EcoProyectado.rawimg"]
for s in sprites:
    ok = any(k.endswith(s) for k in bl)
    print(f"  sprite {s}: {'DENTRO' if ok else 'FALTA (MAL)'}")
    todas_vivas = todas_vivas and ok

# === HJSON ×3: las claves nuevas viven en los tres idiomas ===
hjsons = sorted(k for k in bl if k.endswith(".hjson"))
print(f"hjson: {len(hjsons)} {hjsons}")
# NOTA: el hjson viaja ANIDADO (Registro: { Fallo: ... }) — se buscan los
# NOMBRES de las claves nuevas; los VALORES distintivos se buscan EN SU
# IDIOMA (es-*: «asqueroso»/«ME PROYECTO» · en-US: «disgusting»/«PROJECT»)
claves_nuevas = [b"Fallo:", b"Asco:", b"Grito:", b"FaroEntregado",
                 b"el haz concentrado", b"EcoProyectado"]
for k in hjsons:
    texto = bl[k]
    es_idioma = "en-US" not in k
    valores = [b"asqueroso", "ME PROYECTO".encode()] if es_idioma else [b"disgusting", b"PROJECT"]
    for c in claves_nuevas + valores:
        ok = c in texto
        print(f"  {os.path.basename(k)} · {c.decode()}: {'VIVE' if ok else 'FALTA (MAL)'}")
        todas_vivas = todas_vivas and ok

veredicto = ok_hasta_aqui and off == len(d) and todas_vivas and todas_muertas
print("AUDITORÍA v6.175:", "TODO VERIFICADO ✔" if veredicto else "HAY FALLOS — NO PUBLICAR")
sys.exit(0 if veredicto else 1)
