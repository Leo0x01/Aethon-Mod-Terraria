// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

using AethonMod.Content.GoozmaPort.Common.Systems.Particles;
using AethonMod.Content.GoozmaPort.Common.Utilities;
using AethonMod.Content.GoozmaPort.Content.NPCs.Bosses.GoozmaBoss;
using AethonMod.Content.GoozmaPort.Content.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.GoozmaPort.Content.Buffs
{
    public class FusionBurn : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<FusionBurnPlayer>().active = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<FusionBurnNPC>().active = true;
        }
    }

    public class FusionBurnPlayer : ModPlayer
    {
        public bool active;

        public override void UpdateBadLifeRegen()
        {
            if (active) {
                Player.lifeRegen = -48;
                Player.lifeRegenTime = 0;

                if (Main.rand.NextBool(5)) {
                    Dust dust = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.Sand, Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-3f, -1f), 0, Color.Black, 0.1f + Main.rand.NextFloat());
                    dust.noGravity = true;
                }

                Rectangle box = new Rectangle((int)Player.position.X, (int)Player.position.Y, Player.width, Player.height);
                Color glowColor = new GradientColor(SlimeUtils.GoozColors, 0.2f, 0.2f).Value; //goozma's main color

                if (Main.rand.NextBool(5)) {
                    GoozmaPortHub.Particles.SpawnParticle<ChromaticEnergyDust>(particle => {
                        particle.Position = Main.rand.NextVector2FromRectangle(box);
                        particle.Velocity = -Vector2.UnitY.RotatedByRandom(1f) * Main.rand.NextFloat(3f);
                        particle.Scale = new Vector2(Main.rand.NextFloat(0.5f, 1.5f));
                        particle.Color = glowColor;
                    });
                }

                GoozmaPortHub.Particles.SpawnParticle<FusionFlameParticle>(particle => {
                    particle.Position = Main.rand.NextVector2FromRectangle(box);
                    particle.Velocity = -Vector2.UnitY.RotatedByRandom(1f) * Main.rand.NextFloat(2f);
                    particle.Scale = new Vector2(Main.rand.NextFloat(1f, 3f));
                    particle.maxTime = Main.rand.Next(15, 20);
                    particle.Color = glowColor;
                    particle.fadeColor = glowColor * 0.6f;
                });
            }
        }

        public override void ResetEffects()
        {
            active = false;
        }
    }

    public class FusionBurnNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool active;

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (active) {
                npc.lifeRegenCount -= 120;
                npc.lifeRegen = -80;
                damage = 40;

                if (Main.rand.NextBool(5)) {
                    Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Sand, Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-3f, -1f), 0, Color.Black, 0.3f + Main.rand.NextFloat());
                    dust.noGravity = true;
                }

                Color glowColor = new GradientColor(SlimeUtils.GoozOilColors, 0.2f, 0.2f).Value with { A = 0 }; //oil, bc not from goozma

                if (Main.rand.NextBool(3)) {
                    GoozmaPortHub.Particles.SpawnParticle<ChromaticEnergyDust>(particle => {
                        particle.Position = Main.rand.NextVector2FromRectangle(npc.Hitbox);
                        particle.Velocity = -Vector2.UnitY.RotatedByRandom(1f) * Main.rand.NextFloat(3f);
                        particle.Scale = new Vector2(Main.rand.NextFloat(0.5f, 1.5f));
                        particle.Color = glowColor;
                    });
                }

                GoozmaPortHub.Particles.SpawnParticle<FusionFlameParticle>(particle => {
                    particle.Position = Main.rand.NextVector2FromRectangle(npc.Hitbox);
                    particle.Velocity = -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(1f, 5f);
                    particle.Rotation = particle.Velocity.ToRotation();
                    particle.Scale = new Vector2(Main.rand.NextFloat(1f, 3f));
                    particle.maxTime = Main.rand.Next(15, 20);
                    particle.Color = glowColor * 2f;
                    particle.fadeColor = glowColor * 0.4f;
                });
            }
        }

        public override void ResetEffects(NPC npc)
        {
            active = false;
        }
    }
}
