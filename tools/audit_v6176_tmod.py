#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""audit_v6176_tmod.py — AUDITORÍA DEL .TMOD v6.176 (el patrón de la casa).

Con el parser canónico parse_tmod.py: identidad, EOF exacto, blobs, las
FIRMAS VIVAS de la versión (la cura de los crashes del lote · el gusano
sellado entero + la barra del cuerpo · el congelado de la aspiradora ·
la aspiradora reescrita · las tijeras del libro · el arma del plano ·
la bruma de la base · el azar sembrado), las muertas ausentes, hjson ×3,
la PARIDAD PERFECTA de claves (1849=1849) y la PURGA del en-US.
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
ok_hasta_aqui = (ver == "6.176")
if not ok_hasta_aqui:
    print("VERSIÓN MAL — no es la 6.176")
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


# === LAS FIRMAS VIVAS DE LA v6.176 ===
firmas = [
    # LA CURA DE LOS CRASHES DEL LOT (R21 — el client.log de la sesión)
    "AbrirAdditive",              # el PreDraw abre SU lote
    "RestaurarLoteVanilla",       # y sale con la restauración
    # EL GUSANO SELLADO ENTERO + LA BARRA DEL CUERPO
    "EsSegmento",                # el sello de las piezas
    "EsGusanoSeccionadoONo",      # la barra suma TODAS las piezas
    "EsPiezaDeGusano",            # la adopción en SetDefaults
    "BarraJefeInterior",          # la ModBossBar propia
    # EL CONGELADO DE LA ASPIRADORA (los jefes devorados callan)
    "CongeladoPorAspiradora",     # el flag del cine
    "DescongelarAspiradora",      # la red anti-zombi
    # LA ASPIRADORA REESCRITA (espera 5 s · flotación · trago a la vez)
    "AspiradoraDevoradorProjectile",
    "T_ESPERA",                   # la vigilias de la letra
    "_posicionesTrago",           # el trago sin salto (el sim lo cazó)
    "HablarAlPortador",           # el asco EN LA MENTE
    # LAS TIJERAS DEL LIBRO (nada desborda el papel)
    "Tijeras",                    # el rasterizador con recorte
    # EL ARMA DEL HAZ
    "ProyeccionDelel plano",       # el ítem
    "Proyeccionel planoRayo",      # el rayo del ojo
    "el planoEntregado",           # la entrega directa
    # LA BRUMA DE LA BASE de los tentáculos
    "BrumaFX",                    # (ya vivía — la base la usa)
    # EL AZAR SEMBRADO (R22)
    "Hash01",                     # la semilla por entidad·edad
    # LO QUE VIVÍA (no-regresión)
    "RegistroSistema", "MotorDeCombate", "DispararVolea",
    "JefeInteriorVoleaProjectile", "ContarSombras",
    "SakugaDelCielo", "el haz concentrado", "EcoProyectado",
    "FaroEntregado", "GeoTrio",
]
todas_vivas = True
for f in firmas:
    ok = vive(f)
    todas_vivas = todas_vivas and ok
    print(f"  firma {f}: {'VIVE' if ok else 'AUSENTE (MAL)'}")

# === LAS MUERTAS (la purga de la v6.175 sigue muerta) ===
muertas = ["LineaDelCielo"]
todas_muertas = True
for m in muertas:
    ok = vive(m)
    todas_muertas = todas_muertas and (not ok)
    print(f"  muerta {m}: {'AUN VIVE (revisar)' if ok else 'muerta ✔'}")
png_viejo = [k for k in bl if "LineaDelCielo" in k]
print(f"  png LineaDelCielo en el .tmod: {len(png_viejo)} (deben ser 0)")
todas_muertas = todas_muertas and len(png_viejo) == 0

# === LOS SPRITES NUEVOS DENTRO ===
sprites = ["el haz concentrado.rawimg", "EcoProyectado.rawimg",
           "ProyeccionDelel plano.rawimg", "FaroDelEclipse.rawimg"]
for s in sprites:
    ok = any(k.endswith(s) for k in bl)
    print(f"  sprite {s}: {'DENTRO' if ok else 'FALTA (MAL)'}")
    todas_vivas = todas_vivas and ok

# === HJSON ×3: las claves nuevas viven en los tres idiomas ===
hjsons = sorted(k for k in bl if k.endswith(".hjson"))
print(f"hjson: {len(hjsons)} {hjsons}")
claves_nuevas = [b"Fallo:", b"Asco:", b"Grito:", b"FaroEntregado",
                 b"el planoEntregado", b"el haz concentrado", b"EcoProyectado",
                 b"ProyeccionDelel plano", b"BolsaMaestra"]
for k in hjsons:
    texto = bl[k]
    es_idioma = "en-US" not in k
    valores = [b"asqueroso", "ME PROYECTO".encode(), "Proyección del C".encode()] if es_idioma \
        else [b"disgusting", b"PROJECT", b"the beam weapon"]
    for c in claves_nuevas + valores:
        ok = c in texto
        print(f"  {os.path.basename(k)} · {c.decode()}: {'VIVE' if ok else 'FALTA (MAL)'}")
        todas_vivas = todas_vivas and ok

# === LA PURGA DEL EN-US (la paridad perfecta: 1849 = 1849) ===
en = next(k for k in hjsons if "en-US" in k)
es = next(k for k in hjsons if "es-ES" in k)

# v6.176 (lección del build): tML REESCRIBE los hjson al compilar (los
# normaliza y ordena) — el conteo por LÍNEAS era frágil; la paridad se
# mide con el PARSEO REAL de hjson
import hjson as _hjson


def aplanar(dd, prefijo=""):
    pares = {}
    for k, v in dd.items():
        clave = (prefijo + "." + k) if prefijo else k
        if isinstance(v, dict):
            pares.update(aplanar(v, clave))
        else:
            pares[clave] = v
    return pares


es_k = aplanar(_hjson.loads(bl[es].decode("utf-8")))
en_k = aplanar(_hjson.loads(bl[en].decode("utf-8")))
print(f"(5) claves reales EN {len(en_k)} · ES {len(es_k)} · "
      f"EN-sin-ES {sum(1 for k in en_k if k not in es_k)} · ES-sin-EN {sum(1 for k in es_k if k not in en_k)}")
paridad = len(en_k) == len(es_k) and all(k in es_k for k in en_k)
print(f"    paridad del en-US purgado: {'OK' if paridad else 'MAL — la purga no calza'}")
# LOS FÓSILES no vuelven: los placeholders humanos de proyectil MURIERON
fosil = b"Corona De Plumas Projectile"
ok_fosil = fosil not in bl[en]
print(f"    fósil «Corona De Plumas Projectile» en EN: {'PURGADO ✔' if ok_fosil else 'VIVE (MAL)'}")
todas_muertas = todas_muertas and ok_fosil

veredicto = ok_hasta_aqui and off == len(d) and todas_vivas and todas_muertas and paridad
print("AUDITORÍA v6.176:", "TODO VERIFICADO ✔" if veredicto else "HAY FALLOS — NO PUBLICAR")
sys.exit(0 if veredicto else 1)
