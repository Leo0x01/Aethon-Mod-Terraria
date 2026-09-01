using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.Projectiles
{
    /// <summary>
    /// Cosmic Orb Minion — pequeña esfera de luz que rodea al jugador.
    /// Dispara proyectiles mágicos a los enemigos HOSTILES cercanos.
    /// Aparece en la zona de efectos (buff slot) como cualquier minion.
    ///
    /// Mejoras del árbol de habilidades (NodeEffectSystem):
    /// - summon-notable: +1 slot de minion
    /// - summon-keystone: +5 slots de minion, minions disparan bolts extra
    /// - ascend-3 (Magic): +5 bolts por lanzamiento
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
            // Asociar el buff del minion (aparece en la zona de buffs)
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
            // Usamos Summon como base, pero el grimorio aplica bonus de ambos
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

            // Verificar que el owner tiene el buff del minion (aparece en zona de efectos)
            CheckMinionBuff(owner);

            // === ORBITAR ALREDEDOR DEL JUGADOR ===
            orbitAngle += 0.04f;
            float orbitRadius = 60f + Projectile.minionPos * 20f;
            Vector2 orbitOffset = new Vector2(
                (float)System.Math.Cos(orbitAngle + Projectile.minionPos * 1.5f) * orbitRadius,
                (float)System.Math.Sin(orbitAngle + Projectile.minionPos * 1.5f) * orbitRadius * 0.6f
            );
            Vector2 targetPos = owner.Center + orbitOffset;

            // Moverse hacia la posicion orbital
            Projectile.Center = Vector2.Lerp(Projectile.Center, targetPos, 0.15f);
            Projectile.velocity = Vector2.Zero;

            // === BUSCAR ENEMIGO HOSTIL MAS CERCANO ===
            NPC? target = FindHostileTarget(owner);
            if (target != null)
            {
                shootTimer++;
                int shootInterval = 60; // cada 1 segundo

                // Mejora: si tiene summon-keystone, disparar mas rapido
                if (false)
                    shootInterval = 40;

                if (shootTimer >= shootInterval)
                {
                    shootTimer = 0;
                    ShootAtTarget(target, owner);
                }
            }

            // === EFECTOS VISUALES ===
            // Polvo cosmico
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

            // Luz
            Lighting.AddLight(Projectile.Center, new Vector3(0.8f, 0.6f, 1.0f));
        }

        private void CheckMinionBuff(Player owner)
        {
            // Verificar que el jugador tenga el buff del minion activo
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

            // El minion SOLO se elimina si el buff fue cancelado (click derecho en el icono del buff)
            // NO se elimina al cambiar de arma — persiste hasta que el jugador cancele el buff.
            if (!hasBuff)
            {
                Projectile.Kill();
            }
            else
            {
                // Renovar el buff (que no se agote)
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
            float closestDist = 600f; // rango de deteccion
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active) continue;

                // === FILTROS PARA NO ATACAR NPCs NO HOSTILES ===
                // NPCs amistosos (town NPCs, etc.)
                if (npc.friendly) continue;
                // Town NPCs (mercaderes, etc.)
                if (npc.townNPC) continue;
                // NPCs que no reciben daño (inmunes)
                if (npc.dontTakeDamage) continue;
                // Critters (conejos, pajaros, etc.) — no atacarlos (aiStyle 7 = Bunny/Critter)
                if (npc.aiStyle == 7) continue;
                if (npc.catchItem > 0) continue; // capturable con red
                // NPCs que son partes de un jefe (no objetivos reales)
                if (npc.realLife >= 0 && npc.realLife != npc.whoAmI) continue;
                // NPCs tipo proyectil (no son enemigos reales)
                if (npc.immortal) continue;
                // Verificar que sea realmente hostil
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

            // Proyectil principal
            int projType = ModContent.ProjectileType<CosmicOrbBolt>();
            Projectile.NewProjectile(
                Projectile.GetSource_FromAI(),
                Projectile.Center,
                direction * 12f,
                projType, damage, knockback, owner.whoAmI);

            // Mejora: si tiene summon-keystone, disparar bolts extra
            if (false)
            {
                for (int i = 0; i < 2; i++)
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

            // Mejora: ascend-3 (Magic) +5 bolts
            if (false)
            {
                for (int i = 0; i < 5; i++)
                {
                    float angle = i * 0.15f - 0.3f;
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
            // Polvo dorado al impactar
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDustPerfect(target.Center, Terraria.ID.DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                    100, new Color(245, 196, 81), 1f);
            }
        }
    }
}
