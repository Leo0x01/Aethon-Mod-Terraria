using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  v6.97 — LOS CULTISTAS DE LA MAZMORRA (la letra del usuario): «el
    //  grimorio debe tener un dialogo especial cuando los cultistas sean
    //  atacados o vencidos, algo asi como que son tontos por rezarles a
    //  un dios falso, o tambien como que deberian rendirle tributo a él
    //  y que por no hacerlo moriran, que el grimorio nada mas vea a los
    //  primeros NPC cultistas en la mazmorra que tenga una probabilidad
    //  de devorarlos desde el inicio, aunque seria una probabilidad y no
    //  siempre, y que se burle del jefe cultistas cuando aparezca».
    //
    //  LOS TRES TEATROS:
    //   · ATACADO  — el Devoto recibe un golpe del portador del Eterno:
    //     la burla (tontos por rezarle a un dios falso / rindan tributo
    //     o mueran) — con calma (un grito cada 10 s, no cada espada).
    //   · VENCIDO  — el Devoto cae: la carcajada + LA PROBABILIDAD de
    //     devoración (20%: no siempre — la letra) con su animación de
    //     bocado y su voz; el festín alimenta el hambre como toda presa.
    //   · EL JEFE  — el Cultista Lunático APARECE: la burla del falso
    //     profeta a TODO portador con el Eterno encima (el que vio la
    //     escena del ritual).
    //
    //  SOLO LOS PRIMEROS: el Devoto arrodillado de la entrada de la
    //  mazmorra (NPCID.CultistDevote) — el grimoire NO ve a los arqueros
    //  azul/blanco del hardmode (la letra: «nada mas vea a los primeros
    //  NPC cultistas en la mazmorra»).
    // ======================================================================
    public class CultistasGlobalNPC : GlobalNPC
    {
        /// <summary>LA PROBABILIDAD DE LA LETRA: 1 de cada 5 devotos
        /// vencidos acaba en la boca del Eterno («aunque seria una
        /// probabilidad y no siempre»).</summary>
        private const int CARAS_DEVORO = 5;

        /// <summary>EL COMPÁS DE LA BURLA por portador (un grito cada
        /// 10 s: la espada golpea rápido, el libro habla con calma).</summary>
        private static readonly Dictionary<int, ulong> _ultimaBurla = new();

        // ================================================================
        //  TEATRO 1 — ATACADO: la burla del dios falso (con calma).
        // ================================================================

        public override void OnHitByItem(NPC npc, Player player, Item item,
            NPC.HitInfo hit, int damageDone)
        {
            BurlarAlDevoto(npc, player);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile,
            NPC.HitInfo hit, int damageDone)
        {
            Player player = projectile.owner >= 0 && projectile.owner < Main.maxPlayers
                ? Main.player[projectile.owner] : null;
            BurlarAlDevoto(npc, player);
        }

        /// <summary>¿ESTE portador lleva el Eterno encima? (los 59
        /// slots — el libro burla desde la bolsa, no hace falta la
        /// mano).</summary>
        private static bool LlevaElEterno(Player p)
        {
            if (p == null || !p.active) return false;
            int eterno = ModContent.ItemType
                <global::AethonMod.Content.Weapons.GrimoireEternal>();
            for (int k = 0; k < 59; k++)
            {
                Item it = p.inventory[k];
                if (it != null && !it.IsAir && it.type == eterno) return true;
            }
            return false;
        }

        private static void BurlarAlDevoto(NPC npc, Player player)
        {
            try
            {
                if (Main.gameMenu) return;
                if (npc == null || !npc.active || npc.type != NPCID.CultistDevote) return;
                if (player == null || !player.active || player.dead) return;
                if (!LlevaElEterno(player)) return;
                if (Main.netMode == NetmodeID.MultiplayerClient) return; // la AUTORIDAD burla

                ulong ahora = Main.GameUpdateCount;
                if (_ultimaBurla.TryGetValue(player.whoAmI, out ulong ultimo) &&
                    ahora - ultimo < 600ul) return;   // con calma: 10 s
                _ultimaBurla[player.whoAmI] = ahora;

                // 3 variantes: el dios falso · el tributo · la muerte.
                Systems.EcoRed.HablarVarianteAlPortador(player,
                    "Mods.AethonMod.Cultistas.Burla", 3,
                    new Color(178, 110, 236), rugido: true, escala: 0.60f);
            }
            catch { }
        }

        // ================================================================
        //  TEATRO 2 — VENCIDO: la carcajada + LA PROBABILIDAD de devorar.
        // ================================================================

        public override void OnKill(NPC npc)
        {
            try
            {
                if (Main.gameMenu) return;
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if (npc == null || !npc.active) return;
                if (npc.type != NPCID.CultistDevote) return;

                // ¿Quién lo venció? (el último golpe).
                Player p = npc.lastInteraction >= 0 && npc.lastInteraction < Main.maxPlayers
                    ? Main.player[npc.lastInteraction] : null;
                if (p == null || !p.active || !LlevaElEterno(p)) return;

                // LA CARCAJADA (la voz del vencido).
                Systems.EcoRed.HablarVarianteAlPortador(p,
                    "Mods.AethonMod.Cultistas.Vencido", 2,
                    new Color(196, 138, 255), rugido: true, escala: 0.62f);

                // LA PROBABILIDAD DE DEVORARLOS DESDE EL INICIO (la
                //     letra: no siempre — 1 de 5).
                if (!Main.rand.NextBool(CARAS_DEVORO)) return;

                // LA ANIMACIÓN DEL BOCADO: el tentáculo del Fauce a
                // escala de plato envuelve al devoto caído y se lo lleva
                // (ai[0] = -1: sin item del suelo — el ancla es el propio
                // devoto vencido, en las localAI).
                int pr = Projectile.NewProjectile(npc.GetSource_FromAI(),
                    npc.Center, Vector2.Zero,
                    ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Sombras.BocadoDelSueloProjectile>(),
                    0, 0f, p.whoAmI, -1, 4f);
                if (pr >= 0)
                {
                    var mp = Main.projectile[pr];
                    mp.localAI[1] = npc.Center.X;
                    mp.localAI[2] = npc.Center.Y;
                    mp.localAI[0] = npc.netID;
                    mp.netUpdate = true;
                }

                // EL TEATRILLO: polvos + el trago (el hambre ya cobró la
                // presa por la vía de siempre — GlobalNPCXP).
                if (!Main.dedServ)
                {
                    for (int k = 0; k < 14; k++)
                    {
                        Dust d = Dust.NewDustPerfect(npc.Center, DustID.Shadowflame,
                            new Vector2(Main.rand.NextFloat(-2.2f, 2.2f),
                                Main.rand.NextFloat(-3f, 0.4f)), 150,
                            new Color(110, 40, 170), 0.9f);
                        d.noGravity = true;
                    }
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item113.WithPitchOffset(-0.45f), npc.Center);
                }

                // LA PALABRA DEL FESTÍN.
                Systems.EcoRed.HablarAlPortador(p, "Mods.AethonMod.Cultistas.Devorado",
                    new Color(178, 110, 236), rugido: true, escala: 0.62f);
            }
            catch { }
        }

        // ================================================================
        //  TEATRO 3 — EL JEFE APARECE: la burla al falso profeta.
        // ================================================================

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            try
            {
                if (Main.gameMenu) return;
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if (npc == null || !npc.active) return;
                if (npc.type != NPCID.CultistBoss) return;

                // LA BURLA a todo portador vivo del Eterno (el que está
                // viendo nacer al falso profeta).
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p == null || !p.active || p.dead) continue;
                    if (!LlevaElEterno(p)) continue;
                    Systems.EcoRed.HablarAlPortador(p, "Mods.AethonMod.Cultistas.ElJefe",
                        new Color(178, 110, 236), rugido: true, escala: 0.70f,
                        prioridad: true);
                }
            }
            catch { }
        }
    }
}
