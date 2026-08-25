using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.NPCs
{
    /// <summary>
    /// Aethon, la Luz Primordial — jefe final del mod.
    /// 5 fases (Stardust → Nebula → Gravity → Black Hole → Acknowledgment).
    ///
    /// ESQUELETO: AI básica de fase 1 (espiral de pernos estelares).
    /// Las demás fases se implementarán en iteraciones posteriores.
    /// Desbloqueo: el Fragmento Génesis del jugador alcanza nivel 150.
    /// </summary>
    public class AethonBoss : ModNPC
    {
        private int Phase = 1;
        private int AttackTimer = 0;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 120;
            NPC.height = 120;
            NPC.damage = 80;
            NPC.defense = 40;
            NPC.lifeMax = 2_400_000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.npcSlots = 30f;
            NPC.aiStyle = -1; // AI custom
            Music = MusicID.LunarPillar;
            SceneEffectPriority = SceneEffectPriority.BossHigh;
        }

        public override void AI()
        {
            // AI de jefe: flotar y perseguir al jugador, espiral de proyectiles.
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                NPC.TargetClosest(false);
                target = Main.player[NPC.target];
                if (!target.active || target.dead)
                {
                    NPC.life = 0;
                    return;
                }
            }

            // Movimiento: perseguir al jugador manteniendo distancia.
            Vector2 toTarget = target.Center - NPC.Center;
            float dist = toTarget.Length();
            if (dist > 400f)
            {
                NPC.velocity = toTarget.SafeNormalize(Vector2.Zero) * 8f;
            }
            else
            {
                NPC.velocity *= 0.95f;
            }

            // Ataque: espiral de pernos estelares cada 60 ticks.
            AttackTimer++;
            if (AttackTimer >= 60)
            {
                AttackTimer = 0;
                FireSpiral();
            }

            // Transición de fases basada en HP.
            float hpPct = (float)NPC.life / NPC.lifeMax;
            if (hpPct < 0.8f) Phase = 2;
            if (hpPct < 0.6f) Phase = 3;
            if (hpPct < 0.4f) Phase = 4;
            if (hpPct < 0.2f) Phase = 5;

            // Brillo.
            Lighting.AddLight(NPC.Center, new Vector3(0.6f, 0.4f, 0.8f));
        }

        private void FireSpiral()
        {
            // Dispara 8 pernos en espiral.
            for (int i = 0; i < 8; i++)
            {
                float angle = (System.MathF.PI * 2 / 8) * i + NPC.ai[0] * 0.1f;
                Vector2 vel = new Vector2(System.MathF.Cos(angle), System.MathF.Sin(angle)) * 6f;
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    vel,
                    ProjectileID.CultistBossLightningOrbArc,
                    45,
                    2f,
                    Main.myPlayer);
            }
            NPC.ai[0]++;
        }

        public override void OnKill()
        {
            // Drops: Fragmentos de Resonancia + acceso a New Game+.
            var sp = Main.LocalPlayer.GetModPlayer<Players.ShardPlayer>();
            if (sp != null)
            {
                sp.ResonanceShards += 250;
            }
            // TODO: drop de Forma Ascendida (cosmético).
        }
    }
}
