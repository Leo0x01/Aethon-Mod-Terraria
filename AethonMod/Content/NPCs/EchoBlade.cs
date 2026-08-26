using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.NPCs
{
    /// <summary>
    /// Eco del Primer Portador — sombra del primer alma en bondéate a un fragmento.
    /// Mimica un árbol de Melee completo. Parry tus ataques con i-frames.
    /// A 40% HP: desata Corte de Realidad relentamente.
    /// </summary>
    public class EchoBlade : ModNPC
    {
        private int AttackTimer = 0;
        private int ParryCooldown = 0;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 30;
            NPC.height = 48;
            NPC.damage = 50;
            NPC.defense = 20;
            NPC.lifeMax = 180_000;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.02f;
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

            bool phase2 = (float)NPC.life / NPC.lifeMax < 0.40f;
            float speed = phase2 ? 7f : 5f;

            // Perseguir al jugador.
            Vector2 toTarget = target.Center - NPC.Center;
            NPC.velocity = Vector2.Lerp(NPC.velocity,
                toTarget.SafeNormalize(Vector2.Zero) * speed, 0.08f);

            // Parry: si recibe daño cuerpo a cuerpo, chance de i-frame.
            if (ParryCooldown > 0) ParryCooldown--;

            // Ataque: Corte de Realidad cada 90 ticks (60 en fase 2).
            AttackTimer++;
            int attackInterval = phase2 ? 60 : 90;
            if (AttackTimer >= attackInterval)
            {
                AttackTimer = 0;
                FireSlash(target);
            }

            Lighting.AddLight(NPC.Center, new Vector3(0.5f, 0.3f, 0.1f));
        }

        private void FireSlash(Player target)
        {
            Vector2 vel = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 14f;
            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                NPC.Center,
                vel,
                ProjectileID.SolarWhipSword,
                50,
                4f,
                Main.myPlayer);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1, NPC.Center);
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            // 20% chance de parry (inmune al golpe) con cooldown de 120 ticks.
            // Usamos ModifyIncomingHit en vez de revertir daño en OnHitByItem (que era buggy).
            if (ParryCooldown <= 0 && Main.rand.NextBool(5))
            {
                ParryCooldown = 120;
                modifiers.Null(); // anular el golpe por completo (i-frame)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item37, NPC.Center);
                for (int i = 0; i < 15; i++)
                    Dust.NewDust(NPC.Center, 20, 20, DustID.YellowStarDust);
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            // El parry ahora se maneja en ModifyIncomingHit.
            // Aqui solo efectos visuales secundarios.
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
                sp.ResonanceShards += 120;
                Main.NewText($"Has absorbido 120 fragmentos de resonancia de {NPC.FullName}!", new Color(245, 196, 81));
            }
        }
    }
}
