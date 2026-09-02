using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Projectiles
{
    /// <summary>
    /// Cosmic Orb Minion — pequeña esfera de luz que rodea al jugador.
    /// Dispara proyectiles mágicos a los enemigos HOSTILES cercanos.
    /// Aparece en la zona de efectos (buff slot) como cualquier minion.
    ///
    /// ESCALADO (via Grimorio + WeaponScaling):
    /// - Slots de minion: +1 cada 5 niveles del fragmento (Grimorio los otorga)
    /// - Bolts extra por lanzamiento: +1 cada 10 niveles (hito Magia)
    /// </summary>
    public class CosmicOrbMinion : ModProjectile
    {
        private int shootTimer = 0;
        private float orbitAngle = 0f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18000;
            // Daño HÍBRIDO: tanto Magic como Summon
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.aiStyle = -1;
            Projectile.light = 1.0f;
        }

        public override bool? CanCutTiles() => false;

        public override bool MinionContactDamage() => true;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            // Verificar que el owner tiene el buff del minion
            CheckMinionBuff(owner);

            // === ORBITAR ALREDEDOR DEL JUGADOR ===
            orbitAngle += 0.04f;
            float orbitRadius = 60f + Projectile.minionPos * 20f;
            Vector2 orbitOffset = new Vector2(
                (float)System.Math.Cos(orbitAngle + Projectile.minionPos * 1.5f) * orbitRadius,
                (float)System.Math.Sin(orbitAngle + Projectile.minionPos * 1.5f) * orbitRadius * 0.6f
            );
            Vector2 targetPos = owner.Center + orbitOffset;

            Projectile.Center = Vector2.Lerp(Projectile.Center, targetPos, 0.15f);
            Projectile.velocity = Vector2.Zero;

            // === BUSCAR ENEMIGO HOSTIL MAS CERCANO ===
            NPC? target = FindHostileTarget(owner);
            if (target != null)
            {
                shootTimer++;
                int shootInterval = 60; // cada 1 segundo

                // Velocidad de disparo mejora con nivel del fragmento (cada 10 niveles, -10 frames)
                var sp = owner.GetModPlayer<ShardPlayer>();
                if (sp != null && sp.IsImprinted && sp.ActiveBranch == BranchType.Magic)
                {
                    int speedTier = sp.ShardLevel / 10;
                    int reduction = speedTier * 5;
                    if (reduction > 30) reduction = 30;
                    shootInterval -= reduction;
                }

                if (shootTimer >= shootInterval)
                {
                    shootTimer = 0;
                    ShootAtTarget(target, owner);
                }
            }

            // === EFECTOS VISUALES ===
            if (Main.rand.NextBool(5))
            {
                Dust.NewDustPerfect(Projectile.Center, Terraria.ID.DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    100, new Color(245, 196, 81), 0.8f);
            }
            if (Main.rand.NextBool(8))
            {
                Dust.NewDustPerfect(Projectile.Center, Terraria.ID.DustID.PurpleTorch,
                    new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    100, new Color(179, 136, 255), 0.7f);
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.8f, 0.6f, 1.0f));
        }

        private void CheckMinionBuff(Player owner)
        {
            int buffType = ModContent.BuffType<global::AethonMod.Content.Buffs.CosmicOrbBuff>();
            bool hasBuff = false;
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                if (owner.buffType[i] == buffType && owner.buffTime[i] > 0)
                {
                    hasBuff = true;
                    break;
                }
            }

            if (!hasBuff)
            {
                Projectile.Kill();
            }
            else
            {
                owner.AddBuff(buffType, 18000);
            }
        }

        /// <summary>
        /// Busca el enemigo HOSTIL mas cercano.
        /// IMPORTANTE: Solo ataca NPCs hostiles (no critters, no town NPCs, no NPCs amistosos).
        /// </summary>
        private NPC? FindHostileTarget(Player owner)
        {
            NPC? closest = null;
            float closestDist = 600f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active) continue;
                if (npc.friendly) continue;
                if (npc.townNPC) continue;
                if (npc.dontTakeDamage) continue;
                if (npc.aiStyle == 7) continue;
                if (npc.catchItem > 0) continue;
                if (npc.realLife >= 0 && npc.realLife != npc.whoAmI) continue;
                if (npc.immortal) continue;
                if (!npc.CanBeChasedBy()) continue;

                float dist = Vector2.Distance(npc.Center, owner.Center);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = npc;
                }
            }
            return closest;
        }

        private void ShootAtTarget(NPC target, Player owner)
        {
            Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
            if (direction == Vector2.Zero) direction = new Vector2(1, 0);

            int damage = Projectile.damage;
            float knockback = 2f;

            int projType = ModContent.ProjectileType<CosmicOrbBolt>();
            Projectile.NewProjectile(
                Projectile.GetSource_FromAI(),
                Projectile.Center,
                direction * 12f,
                projType, damage, knockback, owner.whoAmI);

            // Bolts extra: +1 cada 10 niveles del fragmento (hito Magia)
            var sp = owner.GetModPlayer<ShardPlayer>();
            if (sp != null && sp.IsImprinted && sp.ActiveBranch == BranchType.Magic)
            {
                int extraBolts = WeaponScaling.ExtraProjectiles(BranchType.Magic, sp.ShardLevel) / 2;
                for (int i = 0; i < extraBolts; i++)
                {
                    float angle = (i + 1) * 0.25f * (i % 2 == 0 ? 1f : -1f);
                    Vector2 perturbed = direction.RotatedBy(angle);
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromAI(),
                        Projectile.Center,
                        perturbed * 12f,
                        projType, damage, knockback, owner.whoAmI);
                }
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, Projectile.Center);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDustPerfect(target.Center, Terraria.ID.DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                    100, new Color(245, 196, 81), 1f);
            }
        }
    }
}
