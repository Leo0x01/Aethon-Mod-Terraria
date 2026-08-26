using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.NPCs
{
    /// <summary>
    /// Eco de la Arquera Estelar — fantasma de una arquera que intentó derribar a Aethon.
    /// Dispara flechas homing que fasan terreno, pone minas de luz, y llama Starfall Storm.
    /// </summary>
    public class EchoArcher : ModNPC
    {
        private int AttackTimer = 0;
        private int MineTimer = 0;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 28;
            NPC.height = 44;
            NPC.damage = 45;
            NPC.defense = 18;
            NPC.lifeMax = 160_000;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.05f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.npcSlots = 12f;
            NPC.aiStyle = -1;
            Music = MusicID.Boss1;
        }

        public override void AI()
        {
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                NPC.TargetClosest(false);
                return;
            }

            bool phase2 = (float)NPC.life / NPC.lifeMax < 0.50f;

            // Mantener distancia: alejarse si el jugador está cerca.
            Vector2 toTarget = target.Center - NPC.Center;
            float dist = toTarget.Length();
            if (dist < 300f)
            {
                NPC.velocity = -toTarget.SafeNormalize(Vector2.Zero) * 4f;
            }
            else if (dist > 500f)
            {
                NPC.velocity = toTarget.SafeNormalize(Vector2.Zero) * 4f;
            }
            else
            {
                NPC.velocity *= 0.95f;
            }

            // Ataque: flechas homing cada 60 ticks (40 en fase 2).
            AttackTimer++;
            int arrowInterval = phase2 ? 40 : 60;
            if (AttackTimer >= arrowInterval)
            {
                AttackTimer = 0;
                FireHomingArrows(target);
            }

            // Minas de luz cada 180 ticks.
            MineTimer++;
            if (MineTimer >= 180)
            {
                MineTimer = 0;
                LayLightMines(target);
            }

            // Fase 2: Starfall Storm cada 360 ticks.
            if (phase2 && MineTimer % 360 == 0)
            {
                StarfallStorm(target);
            }

            Lighting.AddLight(NPC.Center, new Vector3(0.5f, 0.4f, 0.1f));
        }

        private void FireHomingArrows(Player target)
        {
            Vector2 vel = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 12f;
            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                NPC.Center,
                vel,
                ProjectileID.VortexBeaterRocket,
                35,
                2f,
                Main.myPlayer);
        }

        private void LayLightMines(Player target)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = target.Center + new Vector2(
                    Main.rand.NextFloat(-200, 200),
                    Main.rand.NextFloat(-200, 200));
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    pos,
                    Vector2.Zero,
                    ProjectileID.Bullet, // mina de luz visual
                    30,
                    2f,
                    Main.myPlayer,
                    0, 120); // 2 segundos de vida
            }
        }

        private void StarfallStorm(Player target)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 pos = target.Center + new Vector2(
                    Main.rand.NextFloat(-300, 300),
                    -400f);
                Vector2 vel = new Vector2(0, 8f);
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    pos, vel,
                    ProjectileID.StarCannonStar,
                    40, 2f,
                    Main.myPlayer);
            }
        }

        public override void OnKill()
        {
            // Otorgar resonancia al jugador que mato al NPC (no a LocalPlayer — bug en MP).
            int killerWho = NPC.lastInteraction;
            if (killerWho < 0 || killerWho >= Main.player.Length)
            {
                // Fallback: buscar primer jugador que interactuo.
                for (int i = 0; i < Main.player.Length; i++)
                {
                    if (Main.player[i] != null && Main.player[i].active && NPC.playerInteraction[i])
                    {
                        killerWho = i;
                        break;
                    }
                }
            }
            if (killerWho < 0 || killerWho >= Main.player.Length) return;
            Player player = Main.player[killerWho];
            if (player == null || !player.active) return;

            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null)
            {
                sp.ResonanceShards += 110;
                Main.NewText($"Has absorbido 110 fragmentos de resonancia de {NPC.FullName}!", new Color(245, 196, 81));
            }
        }
    }
}
