#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""audit_v6180_tmod.py — AUDITORÍA DEL .TMOD v6.180 (el patrón de la casa).

Las FIRMAS VIVAS de la versión:
  · LOS ICONOS CARGADOS DE VERDAD (GetItemDrawFrame — la causa raíz de los
    slots eternos en blanco: las texturas vanilla son LAZY)
  · EL PEDACITO DEL DEVORAMUNDOS (una sección, la cola, la primera vez —
    la aspiradora murió)
  · EL LORE ENTRELAZADO (las 19 secciones, TODO sellado menos el Compendio)
    + EL CRONISTA NARRADOR (botón LORE + la primera vez + su pensamiento)
  · EL SELLADO PEQUEÑO (el grimorio normal + su ojo + los once ojos +
    las sombras 1→10 + LAS DOS IRAS que persiguen lentas)
  · LA EXPLOSIÓN LIMPIA DEL RECUERDO (EstrellaLimpia — cero raster en el
    arsenal del jefe)
Las MUERTAS: el arma proyectada entera · la aspiradora · el LibroV3 · las
ChatLore viejas · las claves fósiles.
"""
import importlib.util
import os
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
if ver != "6.180":
    print("VERSIÓN MAL — no es la 6.180")
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
#  LAS FIRMAS VIVAS DE LA v6.180
# ════════════════════════════════════════════════════════════════
firmas = [
    # LOS ICONOS CARGADOS (la cura de los slots en blanco)
    "GetItemDrawFrame",
    # EL PEDACITO DEL DEVORAMUNDOS
    "DevoramundosPedacito", "ServirElPedacito", "TerminarElPedacito",
    "PedacitoVivo", "PedacitoEspera",
    # EL LORE ENTRELAZADO + EL CRONISTA NARRADOR
    "UmbralesLore", "LorePrimeraVez", "PrimeraVezCronista",
    # EL SELLADO PEQUEÑO + SUS DOS IRAS
    "IraSelladoProjectile", "SombrasMeta", "BarraAethonSellado",
    "PosicionDelOjo", "TeletransporteEnBruma",
    # LA EXPLOSIÓN LIMPIA DEL RECUERDO
    "EstrellaLimpia",
    # LO QUE VIVE DE SIEMPRE (no-regresión)
    "HazProyectadoLib", "TexturasVivas", "FaroDelEclipseHaz",
    "PuertaViva", "TragarLaPuerta",
    "DiagnosticoPersistenciaSistema", "RegistroSistema",
    "TrioDelEterno", "SelectorFlotante", "PaginaEnBlanco",
    "InteriorInvocaGlobalItem", "JefeInteriorVoleaProjectile",
    "FaucesGlobalNPC", "FauceDevoradorProjectile",
    "ElCronista", "GrimorioNerviosoFlotante",
]
todas_vivas = True
for f in firmas:
    ok = vive(f)
    todas_vivas = todas_vivas and ok
    print(f"  firma {f}: {'VIVE' if ok else 'AUSENTE (MAL)'}")

# ════════════════════════════════════════════════════════════════
#  LAS MUERTAS DE LA v6.180 (y las de siempre)
# ════════════════════════════════════════════════════════════════
muertas = [
    # EL ARMA PROYECTADA ENTERA (la letra: «ya puedes borrar el arma…»)
    "VisorDelProyectado", "RayoProyectado", "RayoOpticoProyectado",
    "ProyectadoPlayer", "ProyectadoSistema", "CapaVisorProyectado",
    "EnergiaTope", "SinVisor", "FaseCambioTip",
    # LA ASPIRADORA (la animación que nunca ocurría)
    "AspiradoraDevoradorProjectile", "CongeladoPorAspiradora",
    "DescongelarAspiradora", "InvocacionDevoradorPendiente",
    "MarcarInvocacionInterior", "InvocacionesInteriorDe",
    # EL LIBROV3 (el cuerpo gigante del Sellado viejo)
    "LibroV3",
    # EL LORE VIEJO (la narración automática)
    "ChatLore",
]
todas_muertas = True
for m in muertas:
    ok = vive(m)
    todas_muertas = todas_muertas and (not ok)
    print(f"  muerta {m}: {'AUN VIVE (revisar)' if ok else 'muerta ✔'}")

pngs_muertos = [k for k in bl if any(s in k for s in (
    "LibroV3", "AethonSellado.png", "AethonSelladoOjo",
    "VisorDelProyectado", "AspiradoraDevorador"))]
print(f"  png/rawimg muertos: {len(pngs_muertos)} (deben ser 0)"
      f"{'' if not pngs_muertos else ' — ' + str(pngs_muertos)}")
todas_muertas = todas_muertas and len(pngs_muertos) == 0

# ════════════════════════════════════════════════════════════════
#  EL hjson ×3: LAS CLAVES NUEVAS VIVEN, LAS VIEJAS MUEREN, LA PARIDAD
# ════════════════════════════════════════════════════════════════
FUENTE = REPO / "AethonMod"
FUENTE_CS = REPO / "AethonMod" / "Content"
import re

def claves_hjson(ruta):
    t = ruta.read_text(encoding="utf-8")
    claves = set()
    for m in re.finditer(r"\n\t?([A-Za-z0-9_.]+):", t):
        claves.add(m.group(1))
    return claves, t

idiomas = {}
for idi in ("es-ES", "es-MX", "en-US"):
    k, txt = claves_hjson(FUENTE / "Localization" / f"{idi}_Mods.AethonMod.hjson")
    idiomas[idi] = k

# (a) LAS CLAVES NUEVAS viven en los TRES (por presencia en el texto)
def textos():
    return {idi: (FUENTE / "Localization" / f"{idi}_Mods.AethonMod.hjson").read_text(encoding="utf-8")
            for idi in ("es-ES", "es-MX", "en-US")}
TXT = textos()
nuevas = [f"LoreHabla{i}:" for i in range(1, 19)] + [
    "LoreBoton:", "LorePrimeraVez:", "LoreTemedor:", "LoreCompleto:",
    "Devorador.Pedacito:", "IraSellado1:", "IraSellado2:"]
for idi, txt in TXT.items():
    faltan = [c for c in nuevas if c not in txt]
    print(f"  [{idi}] claves nuevas: {len(nuevas) - len(faltan)}/{len(nuevas)}"
          f"{'' if not faltan else ' — FALTAN: ' + str(faltan)}")
    todas_vivas = todas_vivas and not faltan

# (b) LAS MUERTAS mueren en los TRES
muertas_hjson = ["SinVisor", "FaseCambio", "ChatLore1", "ChatLore2", "ChatLore3",
                 "ChatLore4", "ChatLore5", "CiclopeEntregado", "Asco"]
for idi, txt in TXT.items():
    viven = [c for c in muertas_hjson if (c + ":") in txt or (c + " ") in txt]
    print(f"  [{idi}] claves muertas: {len(muertas_hjson) - len(viven)}/{len(muertas_hjson)}"
          f"{'' if not viven else ' — VIVEN: ' + str(viven)}")
    todas_muertas = todas_muertas and not viven

# (c) LA PARIDAD (es-ES ↔ es-MX EXACTA — parser real de hjson)
import hjson as _hj
def claves_reales(ruta):
    txt = re.sub(r"//[^\n]*", "", ruta.read_text(encoding="utf-8"))
    d = _hj.loads(txt)
    cs = set()
    def hoja(pref, obj):
        for k, v in obj.items():
            kk = f"{pref}.{k}" if pref else k
            if isinstance(v, dict): hoja(kk, v)
            else: cs.add(kk)
    hoja("", d)
    return cs
es_r = claves_reales(FUENTE / "Localization" / "es-ES_Mods.AethonMod.hjson")
mx_r = claves_reales(FUENTE / "Localization" / "es-MX_Mods.AethonMod.hjson")
dif_es_mx = es_r.symmetric_difference(mx_r)
print(f"  paridad REAL es-ES ↔ es-MX: {len(es_r)} = {len(mx_r)} · "
      f"{'PERFECTA ✔' if not dif_es_mx else 'ROTA: ' + str(sorted(dif_es_mx)[:6])}")
print(f"  en-US va ADELANTE (la brecha EN-es histórica: los Tooltips del"
      f" inglés que el español aún no estrena — documentada)")
es, mx, en = idiomas["es-ES"], idiomas["es-MX"], idiomas["en-US"]
solo_es_mx = es.symmetric_difference(mx)
print(f"  paridad es-ES ↔ es-MX: {len(es)} vs {len(mx)} · "
      f"{'PERFECTA ✔' if not solo_es_mx else 'ROTA: ' + str(sorted(solo_es_mx)[:6])}")
print(f"  en-US={len(en)} (la brecha EN-es es HISTÓRICA: los DisplayName"
      f" cosméticos del inglés van ADELANTE — documentada desde la v6.135)")
paridad_ok = not dif_es_mx

# (d) LOS TEXTOS DE LA LETRA (el botón y el aviso)
_, es_txt = claves_hjson(FUENTE / "Localization" / "es-ES_Mods.AethonMod.hjson")
boton_ok = "BotonConsagrar: Consagrar" in es_txt
aviso_ok = "TrioSinConsagradas: No hay armas disponibles" in es_txt
pedacito_ok = "Un pedacito es suficiente." in es_txt
print(f"  EL BOTÓN dice «Consagrar»: {'✔' if boton_ok else 'MAL'}")
print(f"  EL AVISO dice «No hay armas disponibles»: {'✔' if aviso_ok else 'MAL'}")
print(f"  EL PEDACITO dice «un pedacito es suficiente»: {'✔' if pedacito_ok else 'MAL'}")
todas_vivas = todas_vivas and boton_ok and aviso_ok and pedacito_ok

# ════════════════════════════════════════════════════════════════
#  LAS CURAS EN LA FUENTE
# ════════════════════════════════════════════════════════════════
# (a) LOS ICONOS CARGADOS en las TRES ventanas
libro = (FUENTE_CS / "UI" / "LibroDelEternoUI.cs").read_text(encoding="utf-8")
selector = (FUENTE_CS / "UI" / "SelectorFlotante.cs").read_text(encoding="utf-8")
cielo = (FUENTE_CS / "UI" / "CieloDespiertoUI.cs").read_text(encoding="utf-8")
iconos = libro.count("GetItemDrawFrame")
iconos_ok = iconos >= 2 and "GetItemDrawFrame" in selector and "GetItemDrawFrame" in cielo
print(f"  LOS ICONOS CARGADOS (libro×{iconos} + selector + cielo): "
      f"{'VIVOS ✔' if iconos_ok else 'ROTOS (MAL)'}")
todas_vivas = todas_vivas and iconos_ok

# (b) LA PUERTA SUBE DE PUESTO (el botón jamás fuera del papel)
idx_mundo = libro.find('Titulo(sb, At("MundoTitulo")')
idx_gana = libro.find('Titulo(sb, At("GanaTitulo")')
puerta_ok = 0 < idx_mundo < idx_gana
print(f"  LA PUERTA SUBE DE PUESTO (antes que GANA): {'✔' if puerta_ok else 'MAL'}")
todas_vivas = todas_vivas and puerta_ok

# (c) LOS BOTONES CON EL TEXTO COMPLETO (piso 0.62, sin forzar 1.0)
piso_ok = libro.count("Math.Max(esc, 0.62f)") == 2 and "1.2f, 202f" not in libro
print(f"  EL PISO DE LOS BOTONES (0.62, texto completo): {'✔' if piso_ok else 'MAL'}")
todas_vivas = todas_vivas and piso_ok

# (d) EL PEDACITO: la cola, la primera vez, el reset con la página
fauces = (FUENTE_CS / "Globals" / "FaucesGlobalNPC.cs").read_text(encoding="utf-8")
pedacito = ("UltimaSeccionDelGusano(cabeza)" in fauces
            and "Devorador.Pedacito" in fauces
            and "FauceDevoradorProjectile" in fauces)
pagina = (FUENTE_CS / "Items" / "PaginaEnBlanco.cs").read_text(encoding="utf-8")
reset_ok = "DevoramundosPedacito = false" in pagina
print(f"  EL PEDACITO (la cola + el cine de siempre): {'VIVO ✔' if pedacito else 'MAL'}")
print(f"  LA PÁGINA EN BLANCO lo devuelve a la mesa: {'✔' if reset_ok else 'MAL'}")
todas_vivas = todas_vivas and pedacito and reset_ok

# (e) EL LORE ENTRELAZADO: las 19 secciones en SU orden (el archivo
#     viejo entrelazado en 4·7·10·13·16) + TODO sellado menos [0]
orden_ok = (libro.find('"LoreSecI"') < libro.find('"CodexIITit"')
            and libro.find('"LoreSecII"') < libro.find('"CodexIVTit"')
            and libro.find('"LoreSecIII"') < libro.find('"CodexVITit"')
            and libro.find('"LoreSecIV"') < libro.find('"CodexVIIITit"')
            and libro.find('"LoreSecV"') < libro.find('"CodexXTit"'))
sello_total = "if (!esIzq && idxSec >= 1)" in libro
print(f"  EL ENTRELAZADO (las 5 historias en 4·7·10·13·16): {'✔' if orden_ok else 'MAL'}")
print(f"  TODO SELLADO menos el Compendio [0]: {'✔' if sello_total else 'MAL'}")
todas_vivas = todas_vivas and orden_ok and sello_total

# (f) EL CRONISTA NARRADOR (botón LORE + primera vez + su pensamiento)
cronista = (FUENTE_CS / "NPCs" / "ElCronista.cs").read_text(encoding="utf-8")
cronista_ok = ("Cronista.LoreBoton" in cronista
               and "LorePrimeraVez" in cronista
               and "LoreTemedor" in cronista and "LoreCompleto" in cronista
               and "UmbralesLore" in cronista)
print(f"  EL CRONISTA NARRADOR (botón + primera vez + pensamiento): "
      f"{'VIVO ✔' if cronista_ok else 'MAL'}")
todas_vivas = todas_vivas and cronista_ok

# (g) EL SELLADO PEQUEÑO: el grimorio normal + su ojo + las sombras 1→10
sellado = (FUENTE_CS / "NPCs" / "AethonSellado.cs").read_text(encoding="utf-8")
sellado_ok = ("NPC.width = 44" in sellado
              and "GrimorioHambriento_Iris" in sellado
              and "private const int OJOS = 11" in sellado
              and "Math.Min(10, 1 + (int)(Furia * 10f))" in sellado
              and "IraSelladoProjectile" in sellado
              and "Lentitud" not in sellado.replace("LENTITUD", ""))
ira_ok = ("LENTITUD = 2.3f" in sellado and "T_CEREMONIA" in sellado)
print(f"  EL SELLADO PEQUEÑO (44px + ojo + 11 ojos + sombras 1→10): "
      f"{'VIVO ✔' if sellado_ok else 'MAL'}")
print(f"  LAS IRAS LENTAS (2.3 px/t + ceremonia): {'VIVAS ✔' if ira_ok else 'MAL'}")
todas_vivas = todas_vivas and sellado_ok and ira_ok

# (h) LA EXPLOSIÓN LIMPIA (cero raster en el arsenal del Recuerdo)
dibujo = (FUENTE_CS / "Projectiles" / "Jefes" / "AtaqueJefeDibujo.cs").read_text(encoding="utf-8")
limpio = "VFXCore.DestelloFinal" not in dibujo.replace("La textura DestelloFinal", "")
# la única mención permitida es el comentario del helper
limpio = "EstrellaLimpia(" in dibujo and dibujo.count("VFXCore.DestelloFinal") == 0
print(f"  LA EXPLOSIÓN LIMPIA (cero DestelloFinal en el dibujo del jefe): "
      f"{'✔' if limpio else 'MAL'}")
todas_vivas = todas_vivas and limpio

# ════════════════════════════════════════════════════════════════
#  EL VEREDICTO
# ════════════════════════════════════════════════════════════════
print()
if todas_vivas and todas_muertas and paridad_ok:
    print("AUDIT v6.180: TODO VERIFICADO ✔")
else:
    print("AUDIT v6.180: HAY PROBLEMAS — revisar arriba")
    sys.exit(1)
