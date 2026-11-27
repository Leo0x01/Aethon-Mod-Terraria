#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""audit_v6179_tmod.py — AUDITORÍA DEL .TMOD v6.179 (el patrón de la casa).

Con el parser canónico parse_tmod.py: identidad, EOF exacto, blobs, las
FIRMAS VIVAS de la versión (EL TRÍO MÁS ARMADO que el altar lee —la cura
de las ranuras en blanco— · LA PUERTA VIVA con su TRAGO de las mil
esencias la primera vez · LA VENTANA QUE SE LEE (textos grandes y celda
50) · EL SLOT MUDO (nada de texto en el cuadro del trío) · EL FESTÍN DE
LA LLAMADA (solo las invocaciones DEL PORTADOR cuentan — la marca
pendiente) · LA CIUDAD VACÍA del interior (la cura del crash
AI_007_FindGoodRestingSpot del client.log) · EL ARMA MÁXIMA del
Proyectado (energía infinita 300, sin mejoras) con SUS DOS FASES
cambiables (tecla X o clic derecho) · EL BARRENDERO cuenta los ítems como
anclas — la cura del warn de memoria), las muertas ausentes (LAS DIEZ
MEJORAS · EL CÍCLOPE · SU ECO · SUS DROPS · EL BUFF DE RECARGA), hjson
×3, la paridad del en-US y los split de los gigantes vivos.
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
ok_hasta_aqui = (ver == "6.179")
if not ok_hasta_aqui:
    print("VERSIÓN MAL — no es la 6.179")
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


# === LAS FIRMAS VIVAS DE LA v6.179 ===
firmas = [
    # v6.179 — LA PUERTA VIVA (el cerrojo permanente + el trago de las mil)
    "PuertaViva", "TragarLaPuerta",
    # v6.179 — EL FESTÍN DE LA LLAMADA (solo las invocaciones del portador)
    "InvocacionDevoradorPendiente", "MarcarInvocacionInterior",
    # v6.179 — EL ARMA MÁXIMA con sus DOS FASES
    "CambiarFase", "EnergiaTope", "MultiplicadorProyeccion",
    # LO QUE VIVÍA (no-regresión — la v6.177/v6.178 entera que sigue viva)
    "HazProyectadoLib", "TramosAlineados", "TexturasVivas",
    "TotalOpcionales",
    "DiagnosticoPersistenciaSistema",
    "AbrirAdditive", "RestaurarLoteVanilla",
    "CongeladoPorAspiradora", "DescongelarAspiradora",
    "AspiradoraDevoradorProjectile", "_posicionesTrago", "HablarAlPortador",
    "ExtensionDevoradorPendiente", "CancelarExtensionDevorador",
    "InteriorInvocaGlobalItem",
    "BrumaFX",
    "VisorDelProyectado", "RayoOpticoProyectado", "RayoProyectado",
    "ProyectadoPlayer", "ProyectadoSistema", "CapaVisorProyectado",
    "FaroDelEclipseHaz",
    "RegistroSistema", "JefeInteriorVoleaProjectile",
    "GeoTrio", "TrioDelEterno", "PaginaEnBlanco",
    "SelectorFlotante",
]
todas_vivas = True
for f in firmas:
    ok = vive(f)
    todas_vivas = todas_vivas and ok
    print(f"  firma {f}: {'VIVE' if ok else 'AUSENTE (MAL)'}")

# === LAS MUERTAS (las de siempre + LAS NUEVAS de la cirugía del arma) ===
muertas = ["LineaDelCiclope", "LineaDelCielo",
           "ProyeccionDelCiclope", "ProyeccionCiclopeRayo",
           # v6.179 — LA CIRUGÍA DEL PROYECTADO (solo el arma):
           "ProyectadoDropsNPC", "CiclopeProyectado", "EcoProyectado",
           "RecuperacionProyectada",
           "CartaQueNuncaLeDiste", "CapturaDelVisto", "NoEresTuSoyYo",
           "ProyectorDelSalon", "ProyectorDeCine", "ProyectorImax",
           "FloresAmarillas", "YaLoSupere", "HayMasPeces",
           "LunaQueTeIbaABajar",
           # las entrañas muertas del arma vieja de mejoras
           "ObjetosUsados", "PuntosMejora", "AplicarMejora",
           "EnfriamientoEspecial", "RecuperacionProyectada"]
todas_muertas = True
for m in muertas:
    ok = vive(m)
    todas_muertas = todas_muertas and (not ok)
    print(f"  muerta {m}: {'AUN VIVE (revisar)' if ok else 'muerta ✔'}")
png_viejo = [k for k in bl if "LineaDelCielo" in k or "ProyeccionDelCiclope" in k
             or "ProyeccionCiclopeRayo" in k
             or "CiclopeProyectado" in k or "EcoProyectado" in k
             or "RecuperacionProyectada" in k
             or "CartaQueNuncaLeDiste" in k or "CapturaDelVisto" in k
             or "NoEresTuSoyYo" in k or "ProyectorDelSalon" in k
             or "ProyectorDeCine" in k or "ProyectorImax" in k
             or "FloresAmarillas" in k or "YaLoSupere" in k
             or "HayMasPeces" in k or "LunaQueTeIbaABajar" in k]
print(f"  png/rawimg muertos: {len(png_viejo)} (deben ser 0){'' if not png_viejo else ' — ' + str(png_viejo)}")
todas_muertas = todas_muertas and len(png_viejo) == 0

# === LOS SPRITES DENTRO (el arma y su técnica) ===
sprites = ["ProyeccionHaz.rawimg", "ProyeccionBorde.rawimg",
           "ProyeccionCuerpo.rawimg", "ProyeccionFlujo.rawimg",
           "FaroDelEclipse.rawimg",
           "VisorDelProyectado.rawimg"]
for s in sprites:
    ok = any(k.endswith(s) for k in bl)
    print(f"  sprite {s}: {'DENTRO' if ok else 'FALTA (MAL)'}")
    todas_vivas = todas_vivas and ok

# === v6.179 — LAS CURAS EN LA FUENTE ===
FUENTE = REPO / "AethonMod" / "Content"

# (a) EL TRÍO MÁS ARMADO (la cura de las ranuras en blanco)
trio = (FUENTE / "Weapons" / "Investigaciones" / "TrioDelEterno.cs").read_text(encoding="utf-8")
trio_ok = "mejorArmadas" in trio and "it.ModItem is not TrioDelEterno trio" in trio
print(f"  EL TRÍO MÁS ARMADO en Del(): {'VIVO ✔' if trio_ok else 'AUSENTE (MAL)'}")
todas_vivas = todas_vivas and trio_ok

# (b) LA PUERTA VIVA + EL TRAGO en los TRES caminos del altar
submundo = (FUENTE / "Systems" / "SubmundoGrimorio.cs").read_text(encoding="utf-8")
puerta_ok = ("public static bool PuertaAbierta => PuertaViva ||" in submundo
             and "tag[\"puertaViva\"] = PuertaViva" in submundo
             and "PuertaViva = tag.GetBool(\"puertaViva\");" in submundo)
print(f"  LA PUERTA VIVA (flag persistente + PuertaAbierta con cerrojo): {'VIVA ✔' if puerta_ok else 'AUSENTE (MAL)'}")
todas_vivas = todas_vivas and puerta_ok
altar = (FUENTE / "UI" / "AltarOfrendasUI.cs").read_text(encoding="utf-8")
tragos = altar.count("Submundo.TragarLaPuerta();")
print(f"  EL TRAGO en los caminos del altar: {tragos} (deben ser 3)")
todas_vivas = todas_vivas and tragos == 3

# (c) LA CIUDAD VACÍA (la cura del crash AI_007 del client.log)
ciudad_ok = "viejo.active = false" in submundo and "LA CIUDAD VACÍA" in submundo
print(f"  LA CIUDAD VACÍA al entrar al interior (crash AI_007): {'VIVA ✔' if ciudad_ok else 'AUSENTE (MAL)'}")
todas_vivas = todas_vivas and ciudad_ok

# (d) EL FESTÍN DE LA LLAMADA (solo invocaciones del portador)
fauces = (FUENTE / "Globals" / "FaucesGlobalNPC.cs").read_text(encoding="utf-8")
fauces_ok = "sp.InvocacionDevoradorPendiente" in fauces
print(f"  EL VIGÍA gasta SOLO la marca de la llamada: {'VIVO ✔' if fauces_ok else 'AUSENTE (MAL)'}")
todas_vivas = todas_vivas and fauces_ok
pilar = (FUENTE / "UI" / "PrismaInteriorUI.cs").read_text(encoding="utf-8")
pilar_ok = "InvocacionDevoradorPendiente = true" in pilar
print(f"  EL PILAR marca la llamada: {'VIVO ✔' if pilar_ok else 'AUSENTE (MAL)'}")
todas_vivas = todas_vivas and pilar_ok
invoca = (FUENTE / "Globals" / "InteriorInvocaGlobalItem.cs").read_text(encoding="utf-8")
invoca_ok = invoca.count("InvocacionDevoradorPendiente = true") == 2
print(f"  LOS ÍTEMS de invocación marcan (dentro Y fuera): {'VIVOS ✔' if invoca_ok else 'ROTOS (MAL)'}")
todas_vivas = todas_vivas and invoca_ok
pagina = (FUENTE / "Items" / "PaginaEnBlanco.cs").read_text(encoding="utf-8")
pagina_ok = "InvocacionDevoradorPendiente = false" in pagina
print(f"  LA PÁGINA EN BLANCO borra la marca: {'VIVA ✔' if pagina_ok else 'AUSENTE (MAL)'}")
todas_vivas = todas_vivas and pagina_ok

# (e) EL SLOT MUDO + LOS TEXTOS GRANDES
libro = (FUENTE / "UI" / "LibroDelEternoUI.cs").read_text(encoding="utf-8")
mudo_ok = "la ranura VACÍA queda MUDA" in libro and "EL NOMBRE COSIDO MUERE" not in libro
print(f"  EL SLOT MUDO (sin textos en el cuadro del trío): {'VIVO ✔' if mudo_ok else 'AUSENTE (MAL)'}")
todas_vivas = todas_vivas and mudo_ok
botones_12 = libro.count("1.2f, 202f")
print(f"  LOS BOTONES a escala base 1.2: {botones_12} (deben ser 4)")
todas_vivas = todas_vivas and botones_12 == 4
selector = (FUENTE / "UI" / "SelectorFlotante.cs").read_text(encoding="utf-8")
ventana_ok = "private const int Celda = 50;" in selector and "EscalaQueCabe(_titulo, 1.3f" in selector
print(f"  LA VENTANA QUE SE LEE (celda 50 + título 1.3): {'VIVA ✔' if ventana_ok else 'AUSENTE (MAL)'}")
todas_vivas = todas_vivas and ventana_ok

# (f) EL ARMA MÁXIMA con SUS DOS FASES
player = (FUENTE / "Players" / "ProyectadoPlayer.cs").read_text(encoding="utf-8")
arma_ok = ("Infinito => true" in player and "EnergiaTope = 300f" in player
           and "public void CambiarFase()" in player and "tag[\"fase\"] = Fase" in player)
print(f"  EL ARMA MÁXIMA (infinite 300) con fases guardadas: {'VIVA ✔' if arma_ok else 'AUSENTE (MAL)'}")
todas_vivas = todas_vivas and arma_ok
visor = (FUENTE / "Items" / "Investigaciones" / "VisorDelProyectado.cs").read_text(encoding="utf-8")
visor_ok = "MultiplicadorProyeccion = 3f" in visor and "Fase == 2" in visor
print(f"  EL VISOR dispara LA PROYECCIÓN en fase 2: {'VIVO ✔' if visor_ok else 'AUSENTE (MAL)'}")
todas_vivas = todas_vivas and visor_ok

# (g) EL BARRENDERO cuenta los ítems (la cura del warn de memoria)
raiz_mod = (REPO / "AethonMod" / "AethonMod.cs").read_text(encoding="utf-8")
barrido_ok = "typeof(Terraria.Item).IsAssignableFrom(t)" in raiz_mod
print(f"  EL BARRENDERO ancla los ítems (warn de memoria): {'VIVO ✔' if barrido_ok else 'AUSENTE (MAL)'}")
todas_vivas = todas_vivas and barrido_ok

# === HJSON ×3: las claves viven en los tres idiomas ===
hjsons = sorted(k for k in bl if k.endswith(".hjson"))
print(f"hjson: {len(hjsons)} {hjsons}")
claves = [b"Fase1:", b"Fase2:", b"FaseViva:", b"FaseCambioTip:", b"EnergiaInfinita:",
          b"VisorDelProyectado", b"Proyectado", b"RayoProyectado", b"FaroEntregado",
          b"TrioAlbedrio"]
for k in hjsons:
    texto = bl[k]
    for c in claves:
        ok = c in texto
        print(f"  {os.path.basename(k)} · {c.decode()}: {'VIVE' if ok else 'FALTA (MAL)'}")
        todas_vivas = todas_vivas and ok
muertas_hjson = [b"HudEnfriamiento", b"MasEnergia", b"InfinitaAnuncio:", b"UnSoloUso:",
                 b"CiclopeProyectado", b"EcoProyectado", b"RecuperacionProyectada",
                 b"LunaQueTeIbaABajar"]
for k in hjsons:
    for m in muertas_hjson:
        mal = m in bl[k]
        print(f"  {os.path.basename(k)} · muerta {m.decode()}: {'AUN VIVE (MAL)' if mal else 'muerta ✔'}")
        todas_muertas = todas_muertas and not mal

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
solo_es = [k for k in es_k if k not in en_k]
if solo_es:
    print(f"    claves ES-sin-EN: {solo_es}")

# === EL PARTIDO DE LOS GIGANTES sigue vivo en la fuente ===
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

# === LA NOCHE PRESTADA UNIVERSAL sigue viva (no-regresión v6.178) ===
jefe_g = (FUENTE / "Globals" / "JefeInteriorGlobalNPC.cs").read_text(encoding="utf-8")
noche_universal = "if (Systems.SubmundoGrimorio.Activo && Main.dayTime)" in jefe_g
print(f"  la noche prestada UNIVERSAL (todo NPC): {'VIVA ✔' if noche_universal else 'AUSENTE (MAL)'}")
todas_vivas = todas_vivas and noche_universal
vida_contada = jefe_g.count("Main.projectile[_npx].timeLeft = vidaUtil")
print(f"  voleas con la vida contada: {vida_contada} (deben ser 4)")
todas_vivas = todas_vivas and vida_contada == 4

veredicto = ok_hasta_aqui and off == len(d) and todas_vivas and todas_muertas and paridad
print("AUDITORÍA v6.179:", "TODO VERIFICADO ✔" if veredicto else "HAY FALLOS — NO PUBLICAR")
sys.exit(0 if veredicto else 1)
