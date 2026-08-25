using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.NPCs
{
    /// <summary>
    /// El Guardián del Rift — jefe cósmico de nivel 75.
    /// Existe mitad en Terraria, mitad en el vacío entre mundos.
    /// Teleporta a través de rifts, dispara virotes de vacío, y a 30% HP sella los rifts.
    /// </summary>
    public class RiftKeeper : ModNPC
    {
        private int AttackTimer = 0;
        private int TeleportTimer = 0;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 50;
            NPC.damage = 55;
            NPC.defense = 25;
            NPC.lifeMax = 95_000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.npcSlots = 10f;
            NPC.aiStyle = -1;
            Music = MusicID.Boss3;
            SceneEffectPriority = SceneEffectPriority.BossHigh;
        }

        public override void AI()
        {
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

            // Fase 2 a 30% HP: sella los rifts (arena cerrada).
            bool phase2 = (float)NPC.life / NPC.lifeMax < 0.30f;

            // Movimiento: teleport cada 180 ticks (o 120 en fase 2).
            TeleportTimer++;
            int teleportInterval = phase2 ? 120 : 180;
            if (TeleportTimer >= teleportInterval)
            {
                TeleportTimer = 0;
                TeleportNear(target);
            }

            // Entre teleports: flotar y perseguir suavemente.
            Vector2 toTarget = target.Center - NPC.Center;
            NPC.velocity = Vector2.Lerp(NPC.velocity, toTarget.SafeNormalize(Vector2.Zero) * 4f, 0.05f);

            // Ataque: virotes de vacío cada 75 ticks.
            AttackTimer++;
            if (AttackTimer >= 75)
            {
                AttackTimer = 0;
                FireVoidLances(target);
            }

            Lighting.AddLight(NPC.Center, new Vector3(0.2f, 0.4f, 0.5f));
        }

        private void TeleportNear(Player target)
        {
            // Teleporta a un punto aleatorio cerca del jugador.
            Vector2 offset = new Vector2(
                Main.rand.NextFloat(-300f, 300f),
                Main.rand.NextFloat(-200f, 200f));
            NPC.Center = target.Center + offset;
            NPC.velocity = Vector2.Zero;
            // Efecto visual de rift.
            for (int i = 0; i < 30; i++)
            {
                Dust.NewDust(NPC.Center, 30, 30, DustID.PurpleTorch, 0, 0, 100, default, 1.5f);
            }
        }

        private void FireVoidLances(Player target)
        {
            Vector2 vel = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 10f;
            for (int i = -1; i <= 1; i++)
            {
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    vel.RotatedBy(i * 0.15),
                    ProjectileID.DeathLaser,
                    40,
                    2f,
                    Main.myPlayer);
            }
        }

        public override void OnKill()
        {
            var sp = Main.LocalPlayer.GetModPlayer<Players.ShardPlayer>();
            if (sp != null)
            {
                sp.ResonanceShards += 45;
            }
        }
    }
}
