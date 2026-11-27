#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""audit_v6182_tmod.py — AUDITORÍA DEL .TMOD v6.182 (el patrón de la casa).

Las FIRMAS VIVAS de la versión:
  · EL BUFF MUNDO INTERIOR (la puerta hecha icono: el botón del altar murió
    — paisaje plano + bruma + tentáculos, clic derecho VIAJA)
  · LA SELECCIÓN DEL TRÍO (clic izquierdo en la ranura del altar: 1, 2 o
    las 3 — la casilla «las tres a la vez» murió)
  · LA MARCA DE LA LLAMADA DEL DEVORAMUNDOS (solo la invocación REAL arma
    el pedacito — los guardianes de las oleadas ya no lo queman)
  · EL JEFE A LA PROPORCIÓN DEL ARMA (iris a IRIS_ESC 0,2773) Y SIN
    CÍRCULOS PLANOS (el halo SoftGlow murió: anillo rúnico solar + la
    corona de minions a nivel máximo con sus 4 anillos rúnicos)
  · EL TEXTO CENTRADO DEL PILAR (el ancla de LibroLineas.Texto escala)
  · EL 1% DEL MANÁ MÁXIMO en el tope del Aura de Vacío
  · EL GUARDIÁN DEL HOGAR (homeTile clamped dentro del interior — la cura
    del crash AI_007 del client.log)
Las MUERTAS: ClicEnMarcaTres · ModoDisparo · SiguienteConArma · NIVEL_OJOS
· el halo plano (2,4×ESCALA y 3,6×) · MundoPuerta · TrioMarcaTexto ·
TrioPista · TrioModoTres/TrioModoTurnos (+Tips) · la aspiradora de marcas.
"""
import importlib.util
import os
import re
import sys
import zlib

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

d = open(TMOD, "rb").read()
tmlver, name, ver, entries, tend, fsize = mp.parse(TMOD)
print(f"(0) {name} v{ver} · tML {tmlver} · {len(entries)} entradas · {fsize:,} B")
if ver != "6.182":
    print("VERSIÓN MAL — no es la 6.182")
    sys.exit(1)

bl, off = {}, tend
for path, raw, comp in entries:
    trozo = d[off:off + comp]
    blob = trozo if comp == raw else zlib.decompress(trozo, -15)
    bl[path] = blob
    off += comp
print(f"(1) EOF exacto: {'SI' if off == len(d) else 'NO'} ({off} vs {len(d)})")
if off != len(d):
    sys.exit(1)

dll = next(b for k, b in bl.items() if k.endswith(".dll"))
assert dll[:2] == b"MZ"

def vive(txt):
    return txt.encode("utf-16-le") in dll or txt.encode() in dll

# ════════════════════════════════════════════════════════════════
#  LAS FIRMAS VIVAS DE LA v6.182
# ════════════════════════════════════════════════════════════════
firmas = [
    # EL BUFF MUNDO INTERIOR (la puerta hecha icono)
    "MundoInteriorBuff", "ViajarAlInterior", "RightClick",
    "MarcarLlamadaDevoramundos", "ConsumirLlamadaDevoramundos",
    # LA SELECCIÓN DEL TRÍO
    "ClicEnSeleccionRanura", "PonerSeleccion", "Seleccionada",
    # EL JEFE A LA PROPORCIÓN DEL ARMA + LOS ANILLOS RÚNICOS
    "DibujarLaCorona", "DibujarElCuerpo", "EscalaDelOjo",
    # EL 1% DEL MANÁ MÁXIMO + EL GUARDIÁN DEL HOGAR
    "DanoAuraMax", "pedacitoV2",
    # LO QUE VIVE DE SIEMPRE (no-regresión)
    "DevoramundosPedacito", "ServirElPedacito", "TerminarElPedacito",
    "IraSelladoProjectile", "PuertaViva", "TragarLaPuerta",
    "DiagnosticoPersistenciaSistema", "RegistroSistema",
    "TrioDelEterno", "SelectorFlotante", "PaginaEnBlanco",
    "InteriorInvocaGlobalItem", "FaucesGlobalNPC", "FauceDevoradorProjectile",
    "ElCronista", "GrimorioHambriento", "OjoEternoMinion",
    "LibroDelEternoUI", "AltarOfrendasUI", "SubmundoGrimorio",
]
todas_vivas = True
for f in firmas:
    ok = vive(f)
    todas_vivas = todas_vivas and ok
    print(f"  firma {f}: {'VIVE' if ok else 'AUSENTE (MAL)'}")

# ════════════════════════════════════════════════════════════════
#  LAS MUERTAS DE LA v6.182
# ════════════════════════════════════════════════════════════════
muertas = [
    # LA CASILLA «LAS TRES A LA VEZ» + SU MODO
    "ClicEnMarcaTres", "SiguienteConArma",
    # LA HINCHAZÓN DE LA v6.180
    "NIVEL_OJOS",
]
todas_muertas = True
for m in muertas:
    ok = vive(m)
    todas_muertas = todas_muertas and (not ok)
    print(f"  muerta {m}: {'AUN VIVE (revisar)' if ok else 'muerta ✔'}")

pngs_muertos = [k for k in bl if any(s in k for s in ("LibroV3", "AspiradoraDevorador"))]
print(f"  png/rawimg muertos: {len(pngs_muertos)} (deben ser 0)")
todas_muertas = todas_muertas and len(pngs_muertos) == 0

# ════════════════════════════════════════════════════════════════
#  EL hjson ×3: LAS CLAVES NUEVAS VIVEN, LAS VIEJAS MUEREN, LA PARIDAD
# ════════════════════════════════════════════════════════════════
FUENTE = REPO / "AethonMod"
FUENTE_CS = FUENTE / "Content"

def textos():
    return {idi: (FUENTE / "Localization" / f"{idi}_Mods.AethonMod.hjson").read_text(encoding="utf-8")
            for idi in ("es-ES", "es-MX", "en-US")}
TXT = textos()

nuevas = ["MundoInteriorBuff:", "TrioMarcaPuesta:", "TrioMarcaQuitada:",
          "TrioMarcadasTip:", "TrioSinMarca:"]
for idi, txt in TXT.items():
    faltan = [c for c in nuevas if c not in txt]
    print(f"  [{idi}] claves nuevas: {len(nuevas) - len(faltan)}/{len(nuevas)}"
          f"{'' if not faltan else ' — FALTAN: ' + str(faltan)}")
    todas_vivas = todas_vivas and not faltan

muertas_hjson = ["MundoPuerta:", "TrioMarcaTexto:", "TrioPista:",
                 "TrioModoTres:", "TrioModoTurnos:",
                 "TrioModoTresTip:", "TrioModoTurnosTip:"]
for idi, txt in TXT.items():
    viven = [c for c in muertas_hjson if c in txt]
    print(f"  [{idi}] claves muertas: {len(muertas_hjson) - len(viven)}/{len(muertas_hjson)}"
          f"{'' if not viven else ' — VIVEN: ' + str(viven)}")
    todas_muertas = todas_muertas and not viven

import hjson as _hj
def claves_reales(ruta):
    txt = re.sub(r"//[^\n]*", "", ruta.read_text(encoding="utf-8"))
    dd = _hj.loads(txt)
    cs = set()
    def hoja(pref, obj):
        for k, v in obj.items():
            kk = f"{pref}.{k}" if pref else k
            if isinstance(v, dict): hoja(kk, v)
            else: cs.add(kk)
    hoja("", dd)
    return cs
es_r = claves_reales(FUENTE / "Localization" / "es-ES_Mods.AethonMod.hjson")
mx_r = claves_reales(FUENTE / "Localization" / "es-MX_Mods.AethonMod.hjson")
dif_es_mx = es_r.symmetric_difference(mx_r)
print(f"  paridad REAL es-ES ↔ es-MX: {len(es_r)} = {len(mx_r)} · "
      f"{'PERFECTA ✔' if not dif_es_mx else 'ROTA: ' + str(sorted(dif_es_mx)[:6])}")
paridad_ok = not dif_es_mx

# LOS TEXTOS DE LA LETRA (exactos)
es_txt = TXT["es-ES"]
texto_vacio = "El Fuego de Vacío reconoce a su dueño y da una pequeña mejora" in es_txt
texto_pedacito = ("Esto es asqueroso, un pedacito es suficiente." in es_txt
                  and "prefiero comerme al Rey Slime que a esta porquería" in es_txt)
texto_buff = re.search(r"MundoInteriorBuff: \{\n\t\tDisplayName: Mundo Interior\n\t\}", es_txt)
buff_solo_nombre = texto_buff is not None and "Description" not in (
    es_txt[texto_buff.start():texto_buff.end()])
print(f"  LA DESCRIPCIÓN DEL VACÍO EXTERIOR (la letra exacta): {'✔' if texto_vacio else 'MAL'}")
print(f"  EL DIÁLOGO DEL PEDACITO (la letra exacta): {'✔' if texto_pedacito else 'MAL'}")
print(f"  EL BUFF SOLO DICE «Mundo Interior» (sin descripción): "
      f"{'✔' if buff_solo_nombre else 'MAL'}")
todas_vivas = todas_vivas and texto_vacio and texto_pedacito and buff_solo_nombre

# ════════════════════════════════════════════════════════════════
#  LAS CURAS EN LA FUENTE
# ════════════════════════════════════════════════════════════════
# (a) EL JEFE: el iris a LA ESCALA DEL ARMA + los anillos rúnicos + SIN halo plano
sellado = (FUENTE_CS / "NPCs" / "AethonSellado.cs").read_text(encoding="utf-8")
iris_ok = ("GrimorioHambriento.IRIS_ESC" in sellado
           and "escIris = escVivo * Weapons.GrimorioHambriento.IRIS_ESC" in sellado)
corona_ok = ("ESCALA_MINION = 0.375f" in sellado
             and "OrbitaLib.SigiloErosionado" in sellado
             and "OrbitaLib.CirculoRunico" in sellado
             and "SigiloLib.AnilloRunico" in sellado
             and "SigiloLib.CuerpoOro" in sellado and "SigiloLib.CuerpoAzul" in sellado)
halo_muerto = ("2.4f * ESCALA" not in sellado
               and "hinchazon * 3.6f" not in sellado
               and "CoronaConjuro" in sellado)
print(f"  EL IRIS A LA ESCALA DEL ARMA (IRIS_ESC ×1,30 ≈ 11,6 px): {'✔' if iris_ok else 'MAL'}")
print(f"  LA CORONA = MINIONS A NIVEL MÁXIMO (4 anillos rúnicos c/u): "
      f"{'✔' if corona_ok else 'MAL'}")
print(f"  EL HALO PLANO MURIÓ (anillo solar + corona de conjuro): "
      f"{'✔' if halo_muerto else 'MAL'}")
todas_vivas = todas_vivas and iris_ok and corona_ok and halo_muerto

# (b) EL ANCLA DEL PILAR: el texto centrado a CUALQUIER escala
libro = (FUENTE_CS / "UI" / "LibroDelEternoUI.cs").read_text(encoding="utf-8")
ancla_ok = "fuente.MeasureString(texto) * esc * new Vector2(ax, ay)" in libro
print(f"  EL ANCLA ESCALA CON EL TEXTO (el pilar centrado): {'✔' if ancla_ok else 'MAL'}")
todas_vivas = todas_vivas and ancla_ok

# (c) LA SELECCIÓN DEL TRÍO en el altar + el hueco de las etiquetas
trio_ui_ok = ("ClicEnSeleccionRanura(i)" in libro
              and "trio.Seleccionada(i)" in libro
              and "GeoMarcaTres" not in libro
              and "xCasilla - 14f" in libro
              and "At(\"TrioPista\")" not in libro)
boton_muerto = 'At("MundoPuerta")' not in libro
print(f"  EL CLIC IZQUIERDO SELECCIONA + EL HUECO DE LAS ETIQUETAS: "
      f"{'✔' if trio_ui_ok else 'MAL'}")
print(f"  EL BOTÓN «IR AL MUNDO INTERIOR» MURIÓ: {'✔' if boton_muerto else 'MAL'}")
todas_vivas = todas_vivas and trio_ui_ok and boton_muerto

# (d) EL TRÍO: disparan SOLO las marcadas
trio = (FUENTE_CS / "Weapons" / "Investigaciones" / "TrioDelEterno.cs").read_text(encoding="utf-8")
trio_ok = ("!RanuraArmada(idx) || !Seleccionada(idx)" in trio
           and "TrioSinMarca" in trio and "trioSel" in trio
           and "TrioMarcadasTip" in trio
           and "ModoDisparo" not in trio.replace("«modo»", ""))
print(f"  EL TRÍO DISPARA SOLO LAS MARCADAS (persistencia + red + aviso): "
      f"{'✔' if trio_ok else 'MAL'}")
todas_vivas = todas_vivas and trio_ok

# (e) LA MARCA DE LA LLAMADA (el festín solo con invocación REAL)
fauces = (FUENTE_CS / "Globals" / "FaucesGlobalNPC.cs").read_text(encoding="utf-8")
marca_ok = ("ConsumirLlamadaDevoramundos()" in fauces
            and "MarcarLlamadaDevoramundos" not in fauces)
guardian_hogar = ("EL GUARDIÁN DEL HOGAR" in fauces
                  and "npc.homeTileX = Math.Clamp((int)(npc.Center.X / 16f)" in fauces)
interior = (FUENTE_CS / "Globals" / "InteriorInvocaGlobalItem.cs").read_text(encoding="utf-8")
marca_interior = "MarcarLlamadaDevoramundos()" in interior
prisma = (FUENTE_CS / "UI" / "PrismaInteriorUI.cs").read_text(encoding="utf-8")
marca_pilar = "MarcarLlamadaDevoramundos()" in prisma
print(f"  EL FESTÍN SOLO CON INVOCACIÓN REAL (la marca se consume): "
      f"{'✔' if marca_ok else 'MAL'}")
print(f"  LA COMIDA DE GUSANO MARCA: {'✔' if marca_interior else 'MAL'} · "
      f"EL PILAR MARCA: {'✔' if marca_pilar else 'MAL'}")
print(f"  EL GUARDIÁN DEL HOGAR (homeTile clamped en el interior): "
      f"{'✔' if guardian_hogar else 'MAL'}")
todas_vivas = todas_vivas and marca_ok and marca_interior and marca_pilar and guardian_hogar

# (f) EL AURA DE VACÍO: el 1% del maná máximo al tope
aura = (FUENTE_CS / "Projectiles" / "AuraJuicio2Ancla.cs").read_text(encoding="utf-8")
aura_ok = "statManaMax2 * 0.01f" in aura
print(f"  EL 1% DEL MANÁ MÁXIMO EN EL TOPE DEL AURA: {'✔' if aura_ok else 'MAL'}")
todas_vivas = todas_vivas and aura_ok

# (g) EL BUFF DE LA PUERTA: fijo + clic derecho viaja + sin descripción
buff = (FUENTE_CS / "Buffs" / "MundoInteriorBuff.cs").read_text(encoding="utf-8")
submundo = (FUENTE_CS / "Systems" / "SubmundoGrimorio.cs").read_text(encoding="utf-8")
buff_ok = ("LocalizedText.Empty" in buff and "ViajarAlInterior()" in buff
           and "buffNoTimeDisplay" in buff)
refresco_ok = "MundoInteriorBuff" in submundo and "PuertaAbierta" in submundo
icono_ok = (FUENTE_CS / "Buffs" / "MundoInteriorBuff.png").exists()
print(f"  EL BUFF (sin descripción + clic derecho viaja): {'✔' if buff_ok else 'MAL'}")
print(f"  EL REFRESCO PERMANENTE (nace con las mil esencias): "
      f"{'✔' if refresco_ok else 'MAL'}")
print(f"  EL ICONO 32×32 (el paisaje con sus tentáculos): {'✔' if icono_ok else 'MAL'}")
todas_vivas = todas_vivas and buff_ok and refresco_ok and icono_ok

# ════════════════════════════════════════════════════════════════
#  EL VEREDICTO
# ════════════════════════════════════════════════════════════════
print()
if todas_vivas and todas_muertas and paridad_ok:
    print("AUDIT v6.182: TODO VERIFICADO ✔")
else:
    print("AUDIT v6.182: HAY PROBLEMAS — revisar arriba")
    sys.exit(1)
