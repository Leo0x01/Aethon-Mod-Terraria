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
    /// Cosmic Orb Minion — orbe cósmico que orbita al jugador y dispara a enemigos hostiles.
    /// Sprite diseñado para combinar con el Grimorio del Eterno (galaxia espiral dorada,
    /// fondo índigo, destellos cian y gemas magenta).
    ///
    /// ANIMACIÓN DE 3 FRAMES (vertical):
    /// - Frame 0: Idle (órbita tranquila, brillo suave)
    /// - Frame 1: Cargando (brillo intenso, acumula partículas cian/magenta)
    /// - Frame 2: Disparando (flash dorado, libera el bolt cósmico)
    ///
    /// COMPORTAMIENTO DE CARGA:
    /// 1. Detecta enemigo hostil cercano
    /// 2. Fase de carga (~1 segundo): el minion se detiene, brilla intensamente,
    ///    rota más rápido, acumula partículas cósmicas hacia su centro
    /// 3. Fase de disparo: lanza el CosmicOrbBolt hacia el enemigo
    /// 4. Vuelve a fase idle hasta el próximo ciclo
    /// </summary>
    public class CosmicOrbMinion : ModProjectile
    {
        // === Timers de comportamiento ===
        private int shootTimer = 0;
        private int chargeTimer = 0;
        private float orbitAngle = 0f;

        // === Fases del comportamiento ===
        private enum MinionState { Idle, Charging, Shooting }
        private MinionState state = MinionState.Idle;

        // === Frame animation ===
        private int frameTimer = 0;
        private int currentFrame = 0;

        // === Constantes ===
        private const int ChargeDuration = 60;  // 1 segundo de carga
        private const int ShootCooldown = 60;   // 1 segundo entre disparos
        private const int FrameSpeed = 6;        // cada 6 ticks cambia de frame en idle

        public override void SetStaticDefaults()
        {
            // 3 frames verticales: idle, charging, shooting
            Main.projFrames[Projectile.type] = 3;
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

            // Si está cargando, orbita más lento (frena para concentrar energía)
            float lerpSpeed = state == MinionState.Charging ? 0.05f : 0.15f;
            Projectile.Center = Vector2.Lerp(Projectile.Center, targetPos, lerpSpeed);
            Projectile.velocity = Vector2.Zero;

            // === MÁQUINA DE ESTADOS DEL COMPORTAMIENTO ===
            switch (state)
            {
                case MinionState.Idle:
                    HandleIdle(owner);
                    break;
                case MinionState.Charging:
                    HandleCharging(owner);
                    break;
                case MinionState.Shooting:
                    HandleShooting(owner);
                    break;
            }

            // === EFECTOS VISUALES SEGÚN ESTADO ===
            UpdateVisualEffects();

            // === ANIMACIÓN DE FRAMES ===
            UpdateAnimation();

            // Luz (más intensa cuando carga)
            float lightIntensity = state == MinionState.Charging ? 1.5f : 1.0f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.8f * lightIntensity, 0.6f * lightIntensity, 1.0f * lightIntensity));
        }

        // ================================================================
        //  ESTADOS DEL COMPORTAMIENTO
        // ================================================================

        private void HandleIdle(Player owner)
        {
            NPC? target = FindHostileTarget(owner);
            if (target != null)
            {
                shootTimer++;
                int shootInterval = ShootCooldown;

                // Velocidad de disparo mejora con el nivel del Grimorio sostenido
                Item? held = owner.HeldItem;
                if (held != null && held.type == ModContent.ItemType<Weapons.GrimoireEternal>())
                {
                    try
                    {
                        var sl = held.GetGlobalItem<Globals.ShardLevelItem>();
                        if (sl != null)
                        {
                            int speedTier = sl.Level / 10;
                            int reduction = speedTier * 5;
                            if (reduction > 30) reduction = 30;
                            shootInterval -= reduction;
                        }
                    }
                    catch { }
                }

                if (shootTimer >= shootInterval)
                {
                    shootTimer = 0;
                    chargeTimer = 0;
                    state = MinionState.Charging;
                }
            }
            else
            {
                shootTimer = 0;
            }
        }

        private void HandleCharging(Player owner)
        {
            chargeTimer++;

            // === PARTÍCULAS DE CARGA ===
            // Partículas cian y magenta convergen hacia el centro del minion
            for (int i = 0; i < 3; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(20f, 35f);
                Vector2 offset = new Vector2(
                    (float)System.Math.Cos(angle) * dist,
                    (float)System.Math.Sin(angle) * dist);
                Vector2 vel = -offset.SafeNormalize(Vector2.Zero) * 2f;
                Dust.NewDustPerfect(Projectile.Center + offset,
                    DustID.BlueTorch, vel, 100, new Color(0, 255, 255), 1.0f);
            }
            for (int i = 0; i < 2; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(25f, 40f);
                Vector2 offset = new Vector2(
                    (float)System.Math.Cos(angle) * dist,
                    (float)System.Math.Sin(angle) * dist);
                Vector2 vel = -offset.SafeNormalize(Vector2.Zero) * 2.5f;
                Dust.NewDustPerfect(Projectile.Center + offset,
                    DustID.RainbowTorch, vel, 150, new Color(255, 0, 102), 1.0f);
            }

            // Partícula dorada central (núcleo de galaxia)
            if (Main.rand.NextBool(3))
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-0.5f, 0.5f)),
                    100, new Color(255, 217, 61), 0.8f);
            }

            // Sonido de carga (cada 15 frames)
            if (chargeTimer % 15 == 0)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item13, Projectile.Center);
            }

            // Cuando termina la carga, disparar
            if (chargeTimer >= ChargeDuration)
            {
                state = MinionState.Shooting;
            }
        }

        private void HandleShooting(Player owner)
        {
            NPC? target = FindHostileTarget(owner);
            if (target != null)
            {
                ShootAtTarget(target, owner);

                // Flash dorado al disparar
                for (int i = 0; i < 12; i++)
                {
                    Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                        new Vector2(Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4)),
                        150, new Color(255, 217, 61), 1.2f);
                }
                for (int i = 0; i < 8; i++)
                {
                    Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                        new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                        200, new Color(0, 255, 255), 1.0f);
                }

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, Projectile.Center);
            }

            // Volver a idle
            state = MinionState.Idle;
            shootTimer = 0;
            chargeTimer = 0;
        }

        // ================================================================
        //  EFECTOS VISUALES Y ANIMACIÓN
        // ================================================================

        private void UpdateVisualEffects()
        {
            // Partículas ambientales según el estado
            switch (state)
            {
                case MinionState.Idle:
                    // Partículas suaves doradas y cian
                    if (Main.rand.NextBool(5))
                    {
                        Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                            new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                            100, new Color(255, 217, 61), 0.6f);
                    }
                    if (Main.rand.NextBool(8))
                    {
                        Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                            new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                            100, new Color(0, 255, 255), 0.6f);
                    }
                    break;

                case MinionState.Charging:
                    // El minion rota más rápido cuando carga
                    Projectile.rotation += 0.3f;
                    break;
            }
        }

        private void UpdateAnimation()
        {
            // Frame según el estado
            int targetFrame;
            switch (state)
            {
                case MinionState.Idle:
                    // Animación idle: frame 0 (puede parpadear entre 0 y 0)
                    targetFrame = 0;
                    // Parpadeo ocasional en idle
                    frameTimer++;
                    if (frameTimer >= FrameSpeed)
                    {
                        frameTimer = 0;
                    }
                    break;
                case MinionState.Charging:
                    // Cargando: frame 1 (acumula energía)
                    // Progresión visual: frame 0 → 1 según progreso de carga
                    targetFrame = chargeTimer < ChargeDuration / 2 ? 0 : 1;
                    break;
                case MinionState.Shooting:
                    // Disparando: frame 2 (flash)
                    targetFrame = 2;
                    break;
                default:
                    targetFrame = 0;
                    break;
            }

            // Aplicar frame
            if (currentFrame != targetFrame)
            {
                currentFrame = targetFrame;
                Projectile.frame = currentFrame;
            }

            // Rotación suave en idle y charging
            if (state != MinionState.Charging)
            {
                Projectile.rotation += 0.05f;
            }
        }

        // ================================================================
        //  HELPERS
        // ================================================================

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

            // Bolts extra: segun nivel del Grimorio sostenido (hito Magia)
            Item? held2 = owner.HeldItem;
            if (held2 != null && held2.type == ModContent.ItemType<Weapons.GrimoireEternal>())
            {
                try
                {
                    var sl = held2.GetGlobalItem<Globals.ShardLevelItem>();
                    if (sl != null)
                    {
                        int extraBolts = WeaponScaling.ExtraProjectiles(sl.Level) / 2;
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
                }
                catch { }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Explosión cósmica al contacto (paleta del grimorio)
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDustPerfect(target.Center, Terraria.ID.DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                    100, new Color(255, 217, 61), 1f);
            }
            for (int i = 0; i < 6; i++)
            {
                Dust.NewDustPerfect(target.Center, Terraria.ID.DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                    150, new Color(0, 255, 255), 0.9f);
            }
        }
    }
}
