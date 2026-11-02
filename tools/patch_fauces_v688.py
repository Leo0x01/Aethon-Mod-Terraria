#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.88 — LA DEVORACIÓN SAGRADA (FaucesGlobalNPC):
   (A) CheckDead: estado 1/3 JAMÁS muere a medias (la «muerte ajena» del
       segmento que revienta al WoF en plena cena — el bug del muro).
   (B) PreAI: los segmentos congelados ni golpean ni reciben.
   (C) La cena del WoF no teletransporta el cuerpo (el muro dibujado se
       queda en su sitio; los Hungry se enroscan con los ojos).
   (D) HitEffect: sin gores mientras dura el festín (la explosión del
       muro a pantalla completa muere).
   (E) CanHitPlayer: nada de la familia del devorado toca al jugador +
       HayJefeDevoradoCerca para el nuevo FaucesGlobalProjectile."""
import io

R = "AethonMod/Content/Globals/FaucesGlobalNPC.cs"
t = io.open(R, encoding="utf-8").read()

# ---------- (A) el festín es sagrado ----------
viejo = """            // ¿Marca viva de nuestras armas? (el golpe mortal fue NUESTRO)
            bool marcado = _marcas.TryGetValue(npc.whoAmI, out int caduca)
                && caduca > 0;
            if (!marcado)
            {
                // muerte ajena: normal (y el jefe de oleada, aislado)
                OleadaNPC.AislarJefeDeOleada(npc);
                return true;
            }
"""
nuevo = """            // === v6.88 — EL FESTÍN ES SAGRADO (la letra: «todos los
            //     jefes mientras son devorados por el grimorio, estos
            //     siguen atacando… en el momento en el que comienza la
            //     animacion de devorar, los jefes deben dejar de atacar y
            //     deternerse» + «al matar al muro de carne la accion de
            //     devorar del grimorio no se ejecuta bien ya que el muro
            //     explota y el grimorio no lo puede devorar»): mientras el
            //     festín o la CENA viven (estado 1 o 3), NINGÚN golpe lo
            //     mata a medias — ni el propio ni el AJENO. La v6.86
            //     dejaba pasar la «muerte ajena» (un segmento del pool —
            //     los OJOS del WoF — transfirió el daño al dueño y llamó
            //     checkDead SIN marca: vanilla REVIENTA al jefe en plena
            //     devoración, el WoF con su rejilla de cadáver a pantalla
            //     completa). El jefe SOLO muere cuando el motor del
            //     festín lo decreta (estado 2): NADIE interrumpe la
            //     comida. ===
            if (DevorarEstado == 1 || DevorarEstado == ESTADO_PRIMERA_CENA)
            {
                if (npc.life <= 0) npc.life = 1;
                npc.active = true;
                npc.dontTakeDamage = true;
                npc.damage = 0;
                npc.netUpdate = true;
                return false;   // el festín manda
            }

            // ¿Marca viva de nuestras armas? (el golpe mortal fue NUESTRO)
            bool marcado = _marcas.TryGetValue(npc.whoAmI, out int caduca)
                && caduca > 0;
            if (!marcado)
            {
                // muerte ajena: normal (y el jefe de oleada, aislado)
                OleadaNPC.AislarJefeDeOleada(npc);
                return true;
            }
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# ---------- (B) los segmentos callan ----------
viejo = """            if (!devorado) return true;
"""
nuevo = """            // v6.88 — EL SEGMENTO TAMBIÉN CALLA: congelado por el dueño
            //     devorado, ni GOLPEA (damage 0: los ojos del WoF enredados
            //     en el portador con su daño 50) ni RECIBE
            //     (dontTakeDamage: el daño a un segmento transfiere al
            //     pool y podía matar al jefe en pleno festín — el agujero
            //     de la muerte temprana que el festín sagrado ya tapa).
            if (devorado && npc.realLife >= 0 && npc.realLife != npc.whoAmI)
            {
                npc.damage = 0;
                npc.dontTakeDamage = true;
            }
            if (!devorado) return true;
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# ---------- (C) la muralla no viaja ----------
viejo = """            // === v6.50.115 — LA ELEVACIÓN SOBRE EL PORTADOR (la letra:
"""
nuevo = """            // === v6.88 — LA MURALLA NO VIAJA (la letra: «el grimorio
            //     debe asegurarse de devorar visualmente al muro de carne,
            //     por ejemplo atrapando su boca al morir»): el CUERPO del
            //     WoF es quien manda el muro dibujado (DrawWOFBody sigue
            //     su position.X) — al teletransportarlo sobre el portador,
            //     vanilla REDIBUJABA el muro entero en la nueva X y la
            //     escena se leía rota (el muro «explotaba» en un sitio y
            //     reaparecía en otro). El WoF cena DONDE ESTÁ: se eleva
            //     −90 px (la elevación del cine de siempre) pero NO viaja
            //     — el muro queda clavado en su columna del infierno, los
            //     OJOS y los HUNGRY se enroscan alrededor (abajo) y la
            //     fauce los atrapa en el sitio: devoración de verdad. ===
            bool esMuralla = npc.type == NPCID.WallofFlesh;

            // === v6.50.115 — LA ELEVACIÓN SOBRE EL PORTADOR (la letra:
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

viejo = """                float vaivén = t < 175 ? 1f : 0.4f;
                destino += new Vector2(MathF.Sin(t * 0.31f) * 3f, MathF.Sin(t * 0.23f) * 4f) * vaivén;
                npc.Center = destino;
"""
nuevo = """                float vaivén = t < 175 ? 1f : 0.4f;
                destino += new Vector2(MathF.Sin(t * 0.31f) * 3f, MathF.Sin(t * 0.23f) * 4f) * vaivén;
                // v6.88 — LA MURALLA se queda en su columna (solo se
                //     eleva): los demás jefes viajan al ancla del portador.
                if (esMuralla)
                    destino = _posCenaBase + new Vector2(0f, -90f * MathHelper.SmoothStep(0f, 1f, subir));
                npc.Center = destino;
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# los Hungry se enroscan con los ojos (la familia del WoF)
viejo = """                    if (seg == null || !seg.active || i == npc.whoAmI) continue;
                    if (seg.realLife != npc.whoAmI) continue;      // no es mío
"""
nuevo = """                    if (seg == null || !seg.active || i == npc.whoAmI) continue;
                    // v6.88 — LA FAMILIA DE LA MURALLA: los OJOS ya son
                    //     míos por realLife; los HUNGRY (115/116 — sin
                    //     realLife en vanilla) también se enroscan cuando
                    //     la muralla cena (la boca del muro entera en el
                    //     nudo, como la letra la pidió).
                    bool esMio = seg.realLife == npc.whoAmI ||
                        (esMuralla && (seg.type == 115 || seg.type == 116));
                    if (!esMio) continue;                          // no es mío
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# ---------- (D+E) HitEffect + CanHitPlayer + helper, tras CheckActive ----------
viejo = """        /// <summary>CheckActive: false = NO despawnee durante el festín (bool en este tML).</summary>
        public override bool CheckActive(NPC npc)
        {
            if (DevorarEstado == 1 || DevorarEstado == ESTADO_PRIMERA_CENA) return false;
            return base.CheckActive(npc);
        }
"""
nuevo = """        /// <summary>CheckActive: false = NO despawnee durante el festín (bool en este tML).</summary>
        public override bool CheckActive(NPC npc)
        {
            if (DevorarEstado == 1 || DevorarEstado == ESTADO_PRIMERA_CENA) return false;
            return base.CheckActive(npc);
        }

        // ================================================================
        //  v6.88 — LA ZONA DEL FESTÍN ES SAGRADA (la letra: «todos los
        //  jefes mientras son devorados por el grimorio, estos siguen
        //  atacando… los jefes deben dejar de atacar y detenerse»):
        //  tres cerrojos nuevos sobre la familia del devorado.
        // ================================================================

        /// <summary>¿ESTE NPC es parte de un jefe devorado (o lo está él
        /// mismo)? El propio (estado 1/3), los segmentos del pool
        /// (realLife) y CUALQUIER satélite/hostil a ≤700 px de un
        /// devorado vivo (las manos de Skeletron/Prime, los puños del
        /// Gólem, las tres piezas del Señor de la Luna, The Hungry… sin
        /// enumerar IDs de vanilla: la distancia los cubre a todos).</summary>
        public static bool ParteDeJefeDevorado(NPC npc)
        {
            try
            {
                if (npc == null || !npc.active) return false;
                if (npc.TryGetGlobalNPC<FaucesGlobalNPC>(out var g) &&
                    (g.DevorarEstado == 1 || g.DevorarEstado == ESTADO_PRIMERA_CENA))
                    return true;
                if (npc.realLife >= 0 && npc.realLife != npc.whoAmI)
                {
                    NPC dueño = Main.npc[npc.realLife];
                    if (dueño != null && dueño.active &&
                        dueño.TryGetGlobalNPC<FaucesGlobalNPC>(out var gd) &&
                        (gd.DevorarEstado == 1 || gd.DevorarEstado == ESTADO_PRIMERA_CENA))
                        return true;
                }
                return HayJefeDevoradoCerca(npc.Center, 700f);
            }
            catch { return false; }
        }

        /// <summary>¿HAY un jefe devorado VIVO a «radio» de «pos»? (el
        /// rango cubre a los satélites sin realLife y a los proyectiles
        /// hostiles ya disparados — el nuevo FaucesGlobalProjectile los
        /// apaga en vuelo).</summary>
        public static bool HayJefeDevoradoCerca(Vector2 pos, float radio)
        {
            try
            {
                float r2 = radio * radio;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active) continue;
                    if (!n.TryGetGlobalNPC<FaucesGlobalNPC>(out var g)) continue;
                    if (g.DevorarEstado != 1 && g.DevorarEstado != ESTADO_PRIMERA_CENA)
                        continue;
                    if (n.Center.DistanceSQ(pos) <= r2) return true;
                }
                return false;
            }
            catch { return false; }
        }

        /// <summary>v6.88 — NADA de la familia del devorado TOCA al
        /// jugador (la letra: los jefes y sus piezas dejan de atacar
        /// desde el primer tick de la devoración — la v6.86 solo congelaba
        /// la IA: las manos/brazos/Hungry/ojos conservaban su daño de
        /// contacto y golpeaban durante todo el cine).</summary>
        public override bool CanHitPlayer(NPC npc, Player target)
        {
            if (ParteDeJefeDevorado(npc)) return false;
            return base.CanHitPlayer(npc, target);
        }

        /// <summary>v6.88 — SIN GORES DURANTE EL FESTÍN (la letra: «el
        /// muro explota y el grimorio no lo puede devorar… la accion no
        /// ocurre pero el efecto si»): la explosión del WoF nacía en
        /// VanillaHitEffect (StrikeNPC la dispara ANTES de que CheckDead
        /// pudiera frenar nada — 50 dust + 4-6 gores POR PARTE y el
        /// CUERPO además escupe la REJILLA DE CADÁVER a columna completa
        /// del infierno). La cura: mientras el festín vive, la vida se
        /// queda en 1 — VanillaHitEffect solo escupe gore con life ≤ 0:
        /// el jefe MUERTO A MEDIAS ya no revienta: se lo come la fauce.</summary>
        public override void HitEffect(NPC npc, int hitDirection, double damage)
        {
            try
            {
                if (ParteDeJefeDevorado(npc) && npc.life <= 0)
                    npc.life = 1;   // sin gore, sin rejilla: el festín sigue
            }
            catch { }
        }
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

io.open(R, "w", encoding="utf-8").write(t)
print("OK — FaucesGlobalNPC: festín sagrado (A-E)")

# ---------- el GlobalProjectile nuevo ----------
NUEVO = '''using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  FaucesGlobalProjectile — v6.88 — LA ZONA DEL FESTÍN (el aire).
    //
    //  LA LETRA: «todos los jefes mientras son devorados por el grimorio,
    //  estos siguen atacando… en el momento en el que comienza la
    //  animacion de devorar, los jefes deben dejar de atacar y
    //  detenerse» — la v6.86 congelaba la IA del jefe PERO sus
    //  PROYECTILES hostiles ya disparados (láseres, andanadas, bombas)
    //  seguían volando durante todo el cine. LA CURA: mientras un jefe
    //  devorado vive a ≤700 px, TODO proyectil hostil de la zona se
    //  apaga en vuelo (Kill silencioso, sin dust de explosion): la
    //  comidad del festín es sagrada. El chequeo corre cada 30 ticks por
    //  proyectil (localAI[1] de contador: barato, solo durante el
    //  festín — 3,5-7 s de vida).
    // ======================================================================
    public class FaucesGlobalProjectile : GlobalProjectile
    {
        public override bool? CanHitPlayer(Projectile projectile, Player target)
        {
            if (projectile.hostile && !projectile.friendly &&
                global::AethonMod.Content.Globals.FaucesGlobalNPC
                    .HayJefeDevoradoCerca(projectile.Center, 700f))
                return false;
            return base.CanHitPlayer(projectile, target);
        }

        public override void AI(Projectile projectile)
        {
            try
            {
                if (!projectile.hostile || projectile.friendly) return;
                // cada 30 ticks por proyectil — el escaneo es barato y
                // solo vive mientras el festín dura.
                projectile.localAI[1] += 1f;
                if (projectile.localAI[1] < 30f) return;
                projectile.localAI[1] = 0f;
                if (global::AethonMod.Content.Globals.FaucesGlobalNPC
                        .HayJefeDevoradoCerca(projectile.Center, 700f))
                {
                    projectile.active = false;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.TrySendData(MessageID.SyncProjectile,
                            -1, -1, null, projectile.whoAmI);
                }
            }
            catch { }
        }
    }
}
'''
with io.open("AethonMod/Content/Globals/FaucesGlobalProjectile.cs", "w", encoding="utf-8") as f:
    f.write(NUEVO)
print("OK — FaucesGlobalProjectile.cs NACIDO (los proyectiles se apagan)")
