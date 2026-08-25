using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.NPCs
{
    /// <summary>
    /// El Titán Hueco — mini-jefe del bioma Sagrario Hueco.
    /// Coloso cristalino guardián del altar. Primer jefe al que se enfrenta el jugador.
    /// </summary>
    public class HollowTitan : ModNPC
    {
        private int AttackTimer = 0;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 60;
            NPC.height = 80;
            NPC.damage = 30;
            NPC.defense = 15;
            NPC.lifeMax = 42_000;
            NPC.HitSound = SoundID.NPCHit41; // cristal
            NPC.DeathSound = SoundID.NPCDeath43;
            NPC.knockBackResist = 0.05f;
            NPC.noGravity = false;
            NPC.boss = true;
            NPC.npcSlots = 5f;
            NPC.aiStyle = 2; // Fighter AI
            Music = MusicID.Boss2;
        }

        public override void AI()
        {
            AttackTimer++;
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                NPC.TargetClosest(false);
                return;
            }

            // Enrage a 50% HP: doble velocidad de ataque.
            float speedMult = (float)NPC.life / NPC.lifeMax < 0.5f ? 2f : 1f;

            // Ataque: cristales homing cada 90 ticks.
            if (AttackTimer >= 90 / speedMult)
            {
                AttackTimer = 0;
                FireCrystalShards();
            }

            NPC.ai[0]++;
        }

        private void FireCrystalShards()
        {
            Player target = Main.player[NPC.target];
            for (int i = 0; i < 3; i++)
            {
                Vector2 vel = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero);
                vel = vel.RotatedByRandom(0.3) * 8f;
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    vel,
                    ProjectileID.CrystalBullet,
                    25,
                    2f,
                    Main.myPlayer);
            }
        }

        public override void OnKill()
        {
            var sp = Main.LocalPlayer.GetModPlayer<Players.ShardPlayer>();
            if (sp != null)
            {
                sp.ResonanceShards += 8;
            }
        }
    }
}
