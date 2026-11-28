#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""audit_v6185_tmod.py — AUDITORÍA DEL .TMOD v6.185 (el patrón de la casa).

Las FIRMAS VIVAS de la versión:
  · LA ESTRELLA DEL GRIMORIO (el ataque básico por nivel de despertar 0-120:
    EstrellaGrimorioProjectile + las tablas puras de DespertarGrimorio +
    las herencias: TormentaNebular (ramificación + implosión), la Enana
    Blanca y la Estrella Muerta (seguimiento heredado), la Sombra de la
    Página (daño heredado + quemadura))
  · LAS ESTRELLAS FUGACES CONSAGRADAS (el ítem, la mesa que las come, la
    tienda del Cronista, la línea del tooltip/Cielo)
  · EL ATAQUE BASE (Flat en ModifyWeaponDamage + el 70 % en DanoOjosDelEterno)
  · EL FESTÍN DEL DEVORAMUNDO POR LA CENA DE LOS DEMÁS (EsGusanoInvocado solo
    Destructor · HaySegmentoDelDevoramundoDanado · InteractuoConElPreso ·
    soloPrimeraVez)
  · EL LIBRO CONSAGRADO EN TODO JEFE (EsJefeConBotin · EsLaUltimaPieza ·
    FamiliaDelJefe · ElLibroPuedeCaerle)
  · LA LLEGADA SOLO SIN GRIMORIO (los dos gates + la Tinta código 3)
  · LA CELDA DEL BUFF SAGRADA (MarcoLuz 2 px · anillo radio 18)
  · EL NACIMIENTO INMEDIATO DEL RAYO C (ai[1] = -3 · owner == Main.myPlayer)
Las MUERTAS: LAS CUATRO ARMAS (OcasoAethonStaff · DesgarroRealityStaff ·
VozCuasarStaff · RayoGammaStaff + sus proyectiles + el subsistema del Ocaso) ·
EL PEDACITO (campos, motor, OnSpawn, vetos, persistencia) · LANZARPAREJA.
"""
import importlib.util
import os
import re
import sys
import zlib
import pathlib as _pl

REPO = _pl.Path(_pl.Path(__file__).resolve().parent.parent)
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
if ver != "6.185":
    print("VERSIÓN MAL — no es la 6.185"); sys.exit(1)
if len(entries) < 500:
    print(f"¡CÁSCARA VACÍA! solo {len(entries)} entradas"); sys.exit(1)

bl, off = {}, tend
for path, raw, comp in entries:
    trozo = d[off:off + comp]
    blob = trozo if comp == raw else zlib.decompress(trozo, -15)
    bl[path] = blob
    off += comp
print(f"(1) EOF exacto: {'SI' if off == len(d) else 'NO'} ({off} vs {len(d)})")
if off != len(d): sys.exit(1)

dll = next(b for k, b in bl.items() if k.endswith(".dll"))
assert dll[:2] == b"MZ"
print(f"(2) la DLL del build vive ({len(dll):,} B)")

def vive(txt):
    return txt.encode("utf-16-le") in dll or txt.encode() in dll

TODAS = True
def chk(ok, etiqueta):
    global TODAS
    TODAS = TODAS and ok
    print(f"  {etiqueta}: {'✔' if ok else 'MAL'}")

# ════════════════════════════════════════════════════════════════
#  LAS FIRMAS VIVAS DE LA v6.185
# ════════════════════════════════════════════════════════════════
print("(3) LAS FIRMAS VIVAS:")
firmas = [
    # LA ESTRELLA DEL GRIMORIO
    "EstrellaGrimorioProjectile", "AtaqueBaseEstrella", "SeguimientoEstrella",
    "SaltosDeLosArcos", "ImplosionDeLaTormenta", "TormentaAlExplotar",
    "TormentaRamifica", "QuemaduraDelVacio", "ProbabilidadSombra",
    "SombrasDelDisparo", "VidaDeLasSombras", "NivelesDeLaEstrella",
    "DespertarHeredado", "DanoHeredado", "QuemaduraHeredada",
    # LAS ESTRELLAS FUGACES
    "EstrellaFugazConsagrada", "OfrendaEstrellas", "EstrellasPara",
    "TipoEstrellaFugaz", "EstrellasPorPeldano", "BocadoEstrellas",
    "CieloEstrellas", "DespertarReq3b",
    # EL FESTÍN DEL DEVORAMUNDO POR LA CENA
    "EsSegmentoDelDevoramundo", "HaySegmentoDelDevoramundoDanado",
    "InteractuoConElPreso", "soloPrimeraVez",
    # EL LIBRO EN TODO JEFE
    "EsJefeConBotin", "EsLaUltimaPieza", "FamiliaDelJefe", "ElCazadorDe",
    "ElLibroPuedeCaerle",
    # LA LLEGADA + LA CELDA DEL BUFF + EL RAYO C
    "TintaConGrimorio",
    # LO QUE VIVE DE SIEMPRE (no-regresión)
    "FaucesGlobalNPC", "ArrancarPrimeraCena", "PrimeraCenaPendiente",
    "FauceDevoradorProjectile", "PrimeraCenaProjectile",
    "DiagnosticoFestinSistema", "SocketFalso", "MiniEstallidoPet",
    "EstallidoLuzLib", "IraSerExteriorRecargaBuff", "GlowUILib",
    "LaCaidaSistema", "TintaDelCometa", "PaginaEnBlanco",
    "ElCronista", "GrimoireEternal", "OjoEternoMinion", "SinfoniaPrimordialProjectile",
    "TormentaNebularProjectile", "MagnetarProjectile", "WhiteDwarfProjectile",
    "DeadStarProjectile", "SombraPaginaProjectile", "RayosPruebaProjectile",
    "AlmaEternoGlobalItem", "DespertarGrimorioSistema", "WeaponScaling",
    "RegistroSistema", "GolpeMotor", "CazaJefesGlobalNPC", "LibroCorrupto",
]
faltan = [f for f in firmas if not vive(f)]
chk(not faltan, f"firmas vivas {len(firmas) - len(faltan)}/{len(firmas)}"
    + ("" if not faltan else " — AUSENTES: " + str(faltan)))

# ════════════════════════════════════════════════════════════════
#  LAS MUERTAS DE LA v6.185
# ════════════════════════════════════════════════════════════════
print("(4) LAS MUERTAS:")
muertas = [
    # LAS CUATRO ARMAS + sus proyectiles + el subsistema del Ocaso
    "OcasoAethonStaff", "OcasoShardProjectile", "OcasoBurstProjectile",
    "OcasoPlayer", "OcasoActivoBuff", "SobrecalentadoBuff", "OcasoSystem",
    "OcasoBurstFX", "DesgarroRealityStaff", "RealityTearProjectile",
    "VozCuasarStaff", "VozCuasarProjectile", "RayoGammaStaff", "RayoGammaProjectile",
    # EL PEDACITO
    "DevoramundosPedacito", "DevoramundosPedacitoMigrado", "PedacitoEspera",
    "PedacitoVivo", "PedacitoTick", "PedacitoPortador", "ServirElPedacito",
    "TerminarElPedacito", "pedacitoV2",
    # EL BÁSICO VIEJO DEL LIBRO
    "LanzarPareja",
]
viven = [m for m in muertas if vive(m)]
chk(not viven, f"muertas {len(muertas) - len(viven)}/{len(muertas)}"
    + ("" if not viven else " — AÚN VIVEN: " + str(viven)))

pngs_muertos = [k for k in bl if any(s in k for s in (
    "Ocaso", "RayoGamma", "VozCuasar", "RealityTear", "DesgarroReality"))]
chk(len(pngs_muertos) == 0, f"png/rawimg muertos: {len(pngs_muertos)} (deben ser 0)"
    + ("" if not pngs_muertos else " — " + str(pngs_muertos)))
pngs_nuevos = [k for k in bl if "EstrellaFugazConsagrada" in k and k.endswith(".rawimg")]
chk(len(pngs_nuevos) == 1, f"el PNG de la Estrella Fugaz vive en el .tmod: {len(pngs_nuevos)}/1")

# ════════════════════════════════════════════════════════════════
#  EL hjson ×3: LAS CLAVES NUEVAS VIVEN, LAS VIEJAS MUEREN, LA PARIDAD
# ════════════════════════════════════════════════════════════════
print("(5) el hjson ×3:")
FUENTE = REPO / "AethonMod"
TXT = {idi: (FUENTE / "Localization" / f"{idi}_Mods.AethonMod.hjson").read_text(encoding="utf-8")
       for idi in ("es-ES", "es-MX", "en-US")}
nuevas = ["EstrellaFugazConsagrada: {", "BocadoEstrellas:", "CieloEstrellas:",
          "Estrella: {", "N120:", "Maxima:", "TintaConGrimorio:"]
for idi, txt in TXT.items():
    faltan = [c for c in nuevas if c not in txt]
    chk(not faltan, f"[{idi}] claves nuevas {len(nuevas) - len(faltan)}/{len(nuevas)}"
        + ("" if not faltan else " — FALTAN: " + str(faltan)))
muertas_hjson = ["OcasoAethonStaff:", "DesgarroRealityStaff:", "VozCuasarStaff:",
                 "RayoGammaStaff:", "OcasoActivoBuff:", "SobrecalentadoBuff:"]
for idi, txt in TXT.items():
    viven_h = [c for c in muertas_hjson if c in txt]
    chk(not viven_h, f"[{idi}] claves muertas {len(muertas_hjson) - len(viven_h)}/{len(muertas_hjson)}"
        + ("" if not viven_h else " — VIVEN: " + str(viven_h)))

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
en_r = claves_reales(FUENTE / "Localization" / "en-US_Mods.AethonMod.hjson")
dif = es_r.symmetric_difference(mx_r)
chk(not dif, f"paridad REAL es-ES ↔ es-MX: {len(es_r)} = {len(mx_r)}"
    + (" PERFECTA ✔" if not dif else " ROTA: " + str(sorted(dif)[:6])))
faltan_en = [k for k in es_r if k not in en_r]
chk(not faltan_en, f"en-US sin faltantes frente a es-ES: {len(es_r) - len(faltan_en)}/{len(es_r)}"
    + ("" if not faltan_en else " — FALTAN: " + str(faltan_en[:6])))
# LOS TEXTOS DE LA LETRA (exactos)
es_txt = TXT["es-ES"]
chk("Esto es asqueroso, un pedacito es suficiente." in es_txt, "EL DIÁLOGO DEL PEDACITO (la letra exacta)")
chk("50% al vencer CUALQUIER JEFE" in es_txt, "EL TOOLTIP DEL LIBRO (todo jefe 50 %)")
chk("El Eterno ya te acompaña, portador" in es_txt, "LA TINTA CON GRIMORIO (la clave nueva)")

# ════════════════════════════════════════════════════════════════
#  LAS CURAS EN LA FUENTE
# ════════════════════════════════════════════════════════════════
print("(6) las curas en la fuente:")
def leo(rel): return (FUENTE / rel).read_text(encoding="utf-8")

fauces = leo("Content/Globals/FaucesGlobalNPC.cs")
chk("return npc.type == NPCID.TheDestroyer ||" in fauces
    and all(x not in fauces for x in ("PedacitoEspera", "PedacitoVivo", "PedacitoTick",
                                      "ServirElPedacito", "TerminarElPedacito", "PedacitoPortador")),
    "EL PEDACITO MURIÓ ENTERO de FaucesGlobalNPC (solo vive la FRASE aprobada)")
chk("HaySegmentoDelDevoramundoDanado(dueñoPool)" in fauces and "soloPrimeraVez: true" in fauces,
    "EL DEVORAMUNDO CENA AL <99 % (solo la primera vez)")
chk("InteractuoConElPreso(p, jefe)" in fauces, "LA INTERACCIÓN EN CADENA")

caza = leo("Content/Globals/CazaJefesGlobalNPC.cs")
chk("EsJefeConBotin(npc) && EsLaUltimaPieza(npc)" in caza and "Main.rand.NextBool(2)" in caza
    and "ElLibroPuedeCaerle" in caza, "EL LIBRO: 50 % EN TODO JEFE · última pieza · regla del inventario")

caida = leo("Content/Systems/LaCaidaSistema.cs")
chk(caida.count("if (TieneGrimorio(p)) continue;") == 2 and "if (TieneGrimorio(p)) return 3;" in caida,
    "LA LLEGADA: los DOS gates del grimorio + la Tinta (código 3)")
pag = leo("Content/Items/PaginaEnBlanco.cs")
chk("OlvidarCaidaDelMundo" not in pag, "LA PÁGINA YA NO OLVIDA EL CIELO DEL MUNDO")

ira = leo("Content/Buffs/IraSerExteriorRecargaBuff.cs")
chk("2f, 0.95f, 1f);" in ira and "1f + 1f * progreso" in ira and "float radio = 18f;" in ira
    and "return false;" in ira, "LA CELDA DEL BUFF (marco 2 px · anillo 18 · sin doble dibujo)")

rayo = leo("Content/Projectiles/Definitivas/RayosPruebaProjectile.cs")
chk("Projectile.owner != Main.myPlayer) return;" in rayo and "-3f, 1.5f);" in rayo,
    "EL NACIMIENTO DEL RAYO C (owner == myPlayer · modo -3 · ×1,5)")
mini = leo("Content/Projectiles/Cosmetic/MiniEstallidoPet.cs")
chk("Projectile.ai[1] == -3f ? 0f : 45f" in mini, "EL MODO NACIMIENTO INMEDIATO (Inicio 0)")

grim = leo("Content/Weapons/GrimoireEternal.cs")
chk("damage.Flat += Systems.DespertarGrimorio.AtaqueBaseEstrella(" in grim,
    "EL ATAQUE BASE SE SUMA AL FINAL (Flat)")
chk("ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Grimorio.EstrellaGrimorioProjectile>()" in grim
    and "LanzarPareja" not in grim, "EL BÁSICO ES LA ESTRELLA (LanzarPareja murió)")
chk("sombra.DanoHeredado = damage;" in grim and "ProbabilidadSombra(despertarBasico)" in grim,
    "LAS SOMBRAS DEL 119/120 (daño acumulado)")
chk("Despertar.Estrella.Actual" in grim, "EL TOOLTIP DE LA ESTRELLA (actual + próxima)")
ws = leo("Content/Systems/WeaponScaling.cs")
chk("d += 0.70f * Content.Systems.DespertarGrimorio.AtaqueBaseEstrella(despertar);" in ws,
    "EL 70 % AL DAÑO DE INVOCACIÓN")
dg = leo("Content/Systems/DespertarGrimorio.cs")
chk("return 1 + (despertar - 105) / 3;" in dg and "if (despertar >= 95) return 0.80f;" in dg,
    "LAS TABLAS PURAS (saltos 105 · seguimiento 95)")
mesa = leo("Content/Systems/DespertarGrimorioSistema.cs")
chk("alma.OfrendaEstrellas < DespertarGrimorio.EstrellasPara(siguiente)) return false;" in mesa,
    "LA MESA EXIGE LAS ESTRELLAS")
alma = leo("Content/Globals/AlmaEternoGlobalItem.cs")
chk('tag["almaOfrendaEstrellas"]' in alma and "writer.Write(OfrendaEstrellas);" in alma,
    "LA PERSISTENCIA DE LAS ESTRELLAS (.plr + red)")
cron = leo("Content/NPCs/ElCronista.cs")
chk("Poner(tipoEstrella, 100L, 10);" in cron and "Poner(tipoEstrella, 1000L, 100);" in cron,
    "EL CRONISTA VENDE ESTRELLAS (10 esencias c/u)")
tor = leo("Content/Projectiles/Cosmic/TormentaNebularProjectile.cs")
chk("ImplosionDeLaTormenta(DespertarHeredado" in tor and "timeLeft = 180;" in tor,
    "LA TORMENTA HEREDA (ramificación + implosión 3 s)")
wd = leo("Content/Projectiles/Cosmic/WhiteDwarfProjectile.cs")
ds = leo("Content/Projectiles/Cosmic/DeadStarProjectile.cs")
chk("heredada ? 600f : 420f" in wd and "if (Projectile.ai[2] > 0f)" in ds,
    "LA ENANA Y LA MUERTA SIGUEN (seguimiento heredado)")

# ════════════════════════════════════════════════════════════════
print()
if TODAS:
    print("AUDIT v6.185: TODO VERIFICADO ✔")
else:
    print("AUDIT v6.185: HAY COSAS MAL — revisar arriba")
    sys.exit(1)
