#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.88 — versión por RANGOS (el texto exacto traía unicode ilegible)."""
import io

# ---------- (1) OjoEternoMinion: por rango de líneas ----------
RUTA = "AethonMod/Content/Projectiles/OjoEternoMinion.cs"
with io.open(RUTA, "r", encoding="utf-8") as f:
    L = f.readlines()

i0 = next(i for i, l in enumerate(L) if "¿EL DAÑO DE ESTE OJO nació ya" in l)
ib = next(i for i, l in enumerate(L) if "public static float Bonus50Vivo" in l)
# el cierre de la función: la línea "        }" después de "catch { return 1f; }"
ic = next(i for i, l in enumerate(L) if i > ib and l.strip() == "catch { return 1f; }")
i1 = next(i for i, l in enumerate(L) if i > ic and l.strip() == "}")

bloque = '''        /// <summary>v6.88 — EL HITO DEL ESCUADRÓN QUE VIVE (la letra:
        /// «todo el ataque del escuadron… deberia decirlo en porcentaje
        /// y deberia de haber un aumento cada 20 niveles»): el
        /// multiplicador VIVO de MultEscuadron (los hitos cada 20
        /// niveles: +10/+15/+20/+25/+30 %) del dueño de ESTE ojo. Las
        /// consumidoras del daño del ojo (el rayo, el sol, los agujeros,
        /// el supremo) lo multiplican por AQUÍ — una sola vez, en todas
        /// partes. (El ×1,25 del 50 horneado de la v6.51.9 MURIÓ: se
        /// cobraba UNA vez al nacer; el hito VIVO crece con el despertar
        /// del dueño — el escuadrón entero sube con él, hito a hito.)</summary>
        public static float BonusHitoVivo(Projectile p)
        {
            try
            {
                if (p == null || !p.active) return 1f;
                if (!(p.ModProjectile is OjoEternoMinion ojo)) return 1f;
                Player dueño = Main.player[p.owner];
                if (dueño == null || !dueño.active) return 1f;
                return Content.Systems.DespertarGrimorio.MultEscuadron(
                    Content.Systems.DespertarGrimorio.NivelDe(dueño));
            }
            catch { return 1f; }
        }
'''
L = L[:i0] + [bloque] + L[i1+1:]
with io.open(RUTA, "w", encoding="utf-8") as f:
    f.writelines(L)
print(f"OK — OjoEternoMinion: BonusHitoVivo reemplaza a Bonus50Vivo+Horneado (líneas {i0}..{i1})")

# el rayo lee la nueva
with io.open(RUTA, "r", encoding="utf-8") as f:
    t = f.read()
viejo = "                float bonusFinal = Bonus50Vivo(Projectile);"
nuevo = "                float bonusFinal = BonusHitoVivo(Projectile);"
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)
viejo = """                // v6.51.0 — EL NIVEL 50 DEL DESPERTAR (la letra: «mejora
                // TODO el ataque de los minions y sus proyectiles así
                // como la cadencia de disparo»): el rayo pega ×1,25 —
                // v6.51.9 — PERO SOLO SI NO LO LLEVA HORNEADO (la
                // auditoría: antes se multiplicaba DOS veces en los ojos
                // invocados ya en el 50)."""
nuevo = """                // v6.88 — EL HITO DEL ESCUADRÓN (la letra: «todo el
                // ataque del escuadron… deberia decirlo en porcentaje y
                // deberia de haber un aumento cada 20 niveles»): el rayo
                // pega por el MULTIPLICADOR VIVO del despertar del dueño
                // (+10/+15/+20/+25/+30 % — BonusHitoVivo, UNA sola vez
                // en toda la cadena: nada de hornear ni de duplicar)."""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)
with io.open(RUTA, "w", encoding="utf-8") as f:
    f.write(t)
print("OK — el rayo del ojo lee BonusHitoVivo")

# ---------- (2) GrimoireEternal ----------
RUTA = "AethonMod/Content/Weapons/GrimoireEternal.cs"
with io.open(RUTA, "r", encoding="utf-8") as f:
    t = f.read()
viejo = """                // v6.51.9 — LA AUDITORÍA DEL ×1,25: si el daño nació con el
                // bonus del DESPERTAR 50 HORNEADO (DanoOjosDelEterno lo
                // hornea), se marca — las consumidoras (el rayo del ojo,
                // los ataques combinados) NO vuelven a multiplicarlo.
                if (ojoIdx >= 0 && Main.projectile[ojoIdx].ModProjectile
                        is global::AethonMod.Content.Projectiles.OjoEternoMinion ojoNuevo)
                    ojoNuevo.Bonus50Horneado =
                        DespertarGrimorio.NivelDe(player) >= DespertarGrimorio.NivelMax;
"""
nuevo = """                // v6.88 — EL HORNEADO DEL ×1,25 MURIÓ: el hito del
                // escuadrón ahora es VIVO (BonusHitoVivo lee el
                // despertar del dueño CADA tick — nada que marcar al
                // nacer: el ojo sube con su dueño, hito a hito).
"""
assert t.count(viejo) == 1, "horneado GrimoireEternal no encontrado"
t = t.replace(viejo, nuevo)
with io.open(RUTA, "w", encoding="utf-8") as f:
    f.write(t)
print("OK — GrimoireEternal: horneado MUERTO")

# ---------- (3) EscuadronPersistPlayer ----------
RUTA = "AethonMod/Content/Players/EscuadronPersistPlayer.cs"
with io.open(RUTA, "r", encoding="utf-8") as f:
    t = f.read()
viejo = """                    if (idx >= 0)
                    {
                        // v6.51.9 — EL ×1,25 DEL DESPERTAR MÁXIMO: si YA
                        // está en el 100 al volver, el ojo NACE horneado.
                        var ojo = Main.projectile[idx].ModProjectile
                            as global::AethonMod.Content.Projectiles.OjoEternoMinion;
                        if (ojo != null && despertar >= Systems.DespertarGrimorio.NivelMax)
                            ojo.Bonus50Horneado = true;
                    }
"""
nuevo = """                    // v6.88 — NADA QUE HORNEAR: el hito del escuadrón
                    //     es VIVO (BonusHitoVivo lee el despertar del
                    //     dueño cada tick — el ojo resucita y sube solo).
"""
assert t.count(viejo) == 1, "horneado EscuadronPersist no encontrado"
t = t.replace(viejo, nuevo)

viejo = """        public override void SaveData(TagCompound tag)
        {
            try
            {
"""
nuevo = """        /// <summary>v6.88 — ¿ESTE personaje LLEVA su grimorio? (la
        /// letra: «los minions se guardan y aparecen en cualquier
        /// personaje tenga o no tenga el grimorio, los minions solo se
        /// deben guardar para el personaje que tenga el grimorio y los
        /// invocó, no para cualquier personaje que yo use y entre a un
        /// mundo»): LA GUARDIA DEL DUEÑO — el retrato SOLO se fotografía
        /// y SOLO resucita si el grimorio VIVE en el inventario de ESTE
        /// personaje (el que lo invocó). Sin Eterno encima: retrato
        /// vacío = nada vuelve (el .plr lo recuerda, pero jamás revive
        /// mientras no cargue su libro).</summary>
        private bool LlevaSuGrimorio()
        {
            try
            {
                int tipoGrimorio = ModContent.ItemType
                    <global::AethonMod.Content.Weapons.GrimoireEternal>();
                for (int i = 0; i < 58; i++)
                    if (Player.inventory[i] != null && !Player.inventory[i].IsAir
                        && Player.inventory[i].type == tipoGrimorio)
                        return true;
                return Player.HeldItem != null && !Player.HeldItem.IsAir
                    && Player.HeldItem.type == tipoGrimorio;
            }
            catch { return false; }
        }

        public override void SaveData(TagCompound tag)
        {
            try
            {
                // v6.88 — LA GUARDIA DEL DUEÑO: sin grimorio NO se
                // fotografía NADA (el retrato viejo queda huérfano y no
                // revive — el escuadrón es de quien lo invocó CON su
                // libro puesto).
                if (!LlevaSuGrimorio())
                {
                    Niveles.Clear(); MejorasList.Clear(); Danos.Clear();
                    return;
                }
"""
assert t.count(viejo) == 1, "SaveData EscuadronPersist no encontrado"
t = t.replace(viejo, nuevo)

viejo = """                if (Main.netMode == NetmodeID.Server) return;
                if (Player.whoAmI != Main.myPlayer) return;
                if (Niveles.Count == 0) return;   // retrato vacío: despedidos o jamás invocados
"""
nuevo = """                if (Main.netMode == NetmodeID.Server) return;
                if (Player.whoAmI != Main.myPlayer) return;
                if (Niveles.Count == 0) return;   // retrato vacío: despedidos o jamás invocados
                // v6.88 — LA GUARDIA DEL DUEÑO: sin el grimorio puesto
                //     (inventario o mano) el escuadrón NO resucita —
                //     aunque el .plr traiga un retrato viejo.
                if (!LlevaSuGrimorio()) return;
"""
assert t.count(viejo) == 1, "OnEnterWorld guard no encontrado"
t = t.replace(viejo, nuevo)

with io.open(RUTA, "w", encoding="utf-8") as f:
    f.write(t)
print("OK — EscuadronPersistPlayer: guardia del dueño + horneado MUERTO")

# ---------- (4) AtaquesCombinados ----------
RUTA = "AethonMod/Content/Systems/AtaquesCombinadosSistema.cs"
with io.open(RUTA, "r", encoding="utf-8") as f:
    t = f.read()
n = t.count("OjoEternoMinion.Bonus50Vivo(lider)")
t = t.replace("OjoEternoMinion.Bonus50Vivo(lider)",
              "OjoEternoMinion.BonusHitoVivo(lider)")
t = t.replace("×1,25 del 50 se cobra UNA vez vía Bonus50Vivo",
              "v6.88: el HITO del escuadrón (cada 20 niveles) se cobra UNA vez vía BonusHitoVivo")
with io.open(RUTA, "w", encoding="utf-8") as f:
    f.write(t)
print(f"OK — AtaquesCombinados: {n} consumos → BonusHitoVivo")

# ---------- (5) verificación global ----------
import subprocess
r = subprocess.run(["grep", "-rn", "Bonus50", "AethonMod/Content/"],
                   capture_output=True, text=True)
print("=== Bonus50 restantes (debe ser VACÍO) ===")
print(r.stdout if r.stdout else "(ninguno — MUERTO de raíz)")
