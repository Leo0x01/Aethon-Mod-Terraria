#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""audit_v6178_tmod.py — AUDITORÍA DEL .TMOD v6.178 (el patrón de la casa).

Con el parser canónico parse_tmod.py: identidad, EOF exacto, blobs, las
FIRMAS VIVAS de la versión (EL RAYO DE VERDAD de HazProyectadoLib con sus
cuatro texturas de la casa · LA CURA DEL client.log (Opcionales con sus
TRES argumentos) · LA NOCHE PRESTADA UNIVERSAL (todos los jefes se
quedan en el interior) · LA VIDA CONTADA de las voleas (180 ticks y
fuera) · EL VIGÍA BORRADO por la página en blanco · LA BRUMA PEQUEÑA
del paisaje (la cura de los 5 fps) · EL NOMBRE COSIDO bajo las ranuras
del trío · EL FARO CON MUCHOS VFX (estela · campanada · choque de
punta)), las muertas ausentes (LA TUMBA DE NIEBLA · el arma VIEJA del
el plano), hjson ×3, la paridad del en-US y los split de los gigantes
vivos.
"""
import importlib.util
import os
import sys

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
import pathlib as _pl
REPO = _pl.Path(REPO)
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
ok_hasta_aqui = (ver == "6.178")
if not ok_hasta_aqui:
    print("VERSIÓN MAL — no es la 6.178")
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


# === LAS FIRMAS VIVAS DE LA v6.178 ===
firmas = [
    # v6.178 — EL RAYO DE VERDAD (la técnica del arma real, de la casa):
    "HazProyectadoLib",             # la librería de las seis capas
    "TramosAlineados",              # el tiling sin costura
    "BrilloBordes",                 # el filo que arde
    "FranjaSuave",                  # las envolventes del tubo
    "TexturasVivas",                # la puerta con fallback
    # v6.178 — LA CURA DEL client.log (Opcionales: {0}/{1}/{2})
    "TotalOpcionales",              # el total que faltaba ({1})
    # v6.178 — EL BANCO DE PRUEBAS de la persistencia
    "DiagnosticoPersistenciaSistema",
    # v6.178 — EL FARO CON MUCHOS VFX
    "_hastaElMuro",                 # la punta contra el mundo (el nombre vive en la DLL como campo)
    "RingQuad",                     # la campanada de cada vuelta
    # LO QUE VIVÍA (no-regresión — la v6.177 entera)
    "AbrirAdditive", "RestaurarLoteVanilla",
    "EsSegmento", "EsGusanoSeccionadoONo", "EsPiezaDeGusano", "BarraJefeInterior",
    "CongeladoPorAspiradora", "DescongelarAspiradora",
    "AspiradoraDevoradorProjectile", "T_ESPERA", "_posicionesTrago", "HablarAlPortador",
    "Tijeras", "AuraPerfil",
    "ExtensionDevoradorPendiente", "CancelarExtensionDevorador",
    "PurgarProyectilesDelJefe", "InteriorInvocaGlobalItem",
    "_saltoTele", "_dashDoble", "_volea2Cd", "ProyectilTematico",
    "BrumaFX",
    "VisorDelProyectado", "RayoOpticoProyectado", "RayoProyectado",
    "ProyectadoPlayer", "ProyectadoSistema", "ProyectadoDropsNPC",
    "CapaVisorProyectado", "RecuperacionProyectada",
    "FaroDelEclipseHaz",
    "RegistroSistema", "MotorDeCombate", "DispararVolea",
    "JefeInteriorVoleaProjectile", "ContarSombras",
    "SakugaDelCielo", "el haz concentrado", "EcoProyectado",
    "FaroEntregado", "GeoTrio", "Hash01", "TrioDelEterno",
    "MarcarInvocacionInterior",     # el vigía (ahora BORRABLE por la página)
    "PaginaEnBlanco",
]
todas_vivas = True
for f in firmas:
    ok = vive(f)
    todas_vivas = todas_vivas and ok
    print(f"  firma {f}: {'VIVE' if ok else 'AUSENTE (MAL)'}")

# === LAS MUERTAS ===
muertas = ["LineaDelel plano", "LineaDelCielo",
           "ProyeccionDelel plano",   # el arma vieja sigue muerta
           "Proyeccionel planoRayo",
           "el planoEntregado"]
todas_muertas = True
for m in muertas:
    ok = vive(m)
    todas_muertas = todas_muertas and (not ok)
    print(f"  muerta {m}: {'AUN VIVE (revisar)' if ok else 'muerta ✔'}")
png_viejo = [k for k in bl if "LineaDelCielo" in k or "ProyeccionDelel plano" in k
             or "Proyeccionel planoRayo" in k]
print(f"  png muertos: {len(png_viejo)} (deben ser 0)")
todas_muertas = todas_muertas and len(png_viejo) == 0

# === LA TUMBA MURIÓ EN LA FUENTE (la cura de los 5 fps) ===
cielo = (REPO / "AethonMod" / "Content" / "Effects" / "CieloInteriorEterno.cs").read_text(encoding="utf-8")
tumba_muerta = "radioTumba" not in cielo
print(f"  fuente CieloInteriorEterno sin LA TUMBA (radioTumba): {'MUERTA ✔' if tumba_muerta else 'VIVE (MAL)'}")
todas_muertas = todas_muertas and tumba_muerta
brumas_peq = cielo.count("quality: 0.16f") + cielo.count("quality: 0.18f")
print(f"  brumas PEQUEÑAS (quality 0.16/0.18): {brumas_peq} (deben ser 3)")
todas_vivas = todas_vivas and brumas_peq == 3

# === LOS SPRITES DENTRO (los nuevos del rayo + lo que vivía) ===
sprites = ["ProyeccionHaz.rawimg", "ProyeccionBorde.rawimg",
           "ProyeccionCuerpo.rawimg", "ProyeccionFlujo.rawimg",
           "el haz concentrado.rawimg", "EcoProyectado.rawimg",
           "FaroDelEclipse.rawimg",
           "VisorDelProyectado.rawimg", "RecuperacionProyectada.rawimg",
           "CartaQueNuncaLeDiste.rawimg", "CapturaDelVisto.rawimg",
           "NoEresTuSoyYo.rawimg", "ProyectorDelSalon.rawimg",
           "ProyectorDeCine.rawimg", "ProyectorImax.rawimg",
           "FloresAmarillas.rawimg", "YaLoSupere.rawimg",
           "HayMasPeces.rawimg", "LunaQueTeIbaABajar.rawimg"]
for s in sprites:
    ok = any(k.endswith(s) for k in bl)
    print(f"  sprite {s}: {'DENTRO' if ok else 'FALTA (MAL)'}")
    todas_vivas = todas_vivas and ok

# === LA CURA DEL client.log EN LA FUENTE (los TRES argumentos) ===
visor = (REPO / "AethonMod" / "Content" / "Items" / "Investigaciones"
         / "VisorDelProyectado.cs").read_text(encoding="utf-8")
cura_ok = "TotalOpcionales,\n                        ProyectadoPlayer.BonoOpcionales() * 100f" in visor
print(f"  VisorDelProyectado Opcionales con TRES argumentos: {'CURADO ✔' if cura_ok else 'AÚN ROTO (MAL)'}")
todas_vivas = todas_vivas and cura_ok

# === LA NOCHE PRESTADA UNIVERSAL EN LA FUENTE ===
jefe_g = (REPO / "AethonMod" / "Content" / "Globals" / "JefeInteriorGlobalNPC.cs").read_text(encoding="utf-8")
noche_universal = "if (Systems.SubmundoGrimorio.Activo && Main.dayTime)" in jefe_g
print(f"  la noche prestada UNIVERSAL (todo NPC): {'VIVA ✔' if noche_universal else 'AUSENTE (MAL)'}")
todas_vivas = todas_vivas and noche_universal

# === LA VIDA CONTADA (timeLeft 180 en las voleas) ===
vida_contada = jefe_g.count("Main.projectile[_npx].timeLeft = vidaUtil")
print(f"  voleas con la vida contada: {vida_contada} (deben ser 4)")
todas_vivas = todas_vivas and vida_contada == 4

# === EL VIGÍA BORRABLE (la página en blanco) ===
pagina = (REPO / "AethonMod" / "Content" / "Items" / "PaginaEnBlanco.cs").read_text(encoding="utf-8")
vigia_ok = "InvocacionesInteriorTipos?.Clear()" in pagina
print(f"  la página en blanco borra EL VIGÍA: {'SI ✔' if vigia_ok else 'NO (MAL)'}")
todas_vivas = todas_vivas and vigia_ok

# === HJSON ×3: las claves viven en los tres idiomas ===
hjsons = sorted(k for k in bl if k.endswith(".hjson"))
print(f"hjson: {len(hjsons)} {hjsons}")
claves = [b"Fallo:", b"VisorDelProyectado", b"Proyectado", b"RecuperacionProyectada",
          b"LunaQueTeIbaABajar", b"RayoProyectado", b"BolsaMaestra", b"FaroEntregado"]
for k in hjsons:
    texto = bl[k]
    for c in claves:
        ok = c in texto
        print(f"  {os.path.basename(k)} · {c.decode()}: {'VIVE' if ok else 'FALTA (MAL)'}")
        todas_vivas = todas_vivas and ok

# === LA PARIDAD del en-US (los fósiles no vuelven) ===
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


en = next(k for k in hjsons if "en-US" in k)
es = next(k for k in hjsons if "es-ES" in k)
es_k = aplanar(_hjson.loads(bl[es].decode("utf-8")))
en_k = aplanar(_hjson.loads(bl[en].decode("utf-8")))
print(f"(5) claves reales EN {len(en_k)} · ES {len(es_k)} · "
      f"EN-sin-ES {sum(1 for k in en_k if k not in es_k)} · ES-sin-EN {sum(1 for k in es_k if k not in en_k)}")
paridad = len(en_k) == len(es_k) and all(k in es_k for k in en_k)
print(f"    paridad del en-US: {'OK' if paridad else 'MAL'}")
todas_vivas = todas_vivas and paridad
fosil = b"Corona De Plumas Projectile"
ok_fosil = fosil not in bl[en]
print(f"    fósil «Corona De Plumas Projectile» en EN: {'PURGADO ✔' if ok_fosil else 'VIVE (MAL)'}")
todas_muertas = todas_muertas and ok_fosil

# === EL PARTIDO DE LOS GIGANTES sigue vivo en la fuente ===
FUENTE = REPO / "AethonMod" / "Content"
split_archivos = [
    "VFX/AuraPerfil.cs", "VFX/AuraLibDibujo.cs", "VFX/AuraLib.cs",
    "NPCs/AethonBossDibujo.cs", "NPCs/AethonBoss.cs",
    "Projectiles/Jefes/AtaqueJefeDibujo.cs", "Projectiles/Jefes/AtaqueJefeProjectile.cs",
    "VFX/HazProyectadoLib.cs",
]
for f in split_archivos:
    existe = (FUENTE / f).exists()
    print(f"  split {f}: {'VIVE' if existe else 'FALTA (MAL)'}")
    todas_vivas = todas_vivas and existe
for f in ("NPCs/AethonBoss.cs", "Projectiles/Jefes/AtaqueJefeProjectile.cs", "VFX/AuraLib.cs"):
    texto = (FUENTE / f).read_text(encoding="utf-8")
    parcial = "partial class" in texto
    print(f"  split {f} partial: {'SI' if parcial else 'NO (MAL)'}")
    todas_vivas = todas_vivas and parcial

veredicto = ok_hasta_aqui and off == len(d) and todas_vivas and todas_muertas and paridad
print("AUDITORÍA v6.178:", "TODO VERIFICADO ✔" if veredicto else "HAY FALLOS — NO PUBLICAR")
sys.exit(0 if veredicto else 1)
