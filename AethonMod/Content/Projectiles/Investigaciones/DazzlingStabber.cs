using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Investigaciones
{
    // ======================================================================
    //  DAZZLING STABBER — v6.124 — LA COPIA DE INVESTIGACIÓN (la letra:
    //  «copia el arma Dazzling Stabber Staff de Calamity Mod y crea una
    //  version con su codigo, sprite y textura al 100% como
    //  investigacion»).
    //
    //  EL CÓDIGO: calco del original de Calamity Mod
    //  (CalamityTeam/CalamityModPublic — Projectiles/Summon/
    //  DazzlingStabber.cs, rama 1.4.4) — la máquina COMPLETA:
    //    · LOS TRES CUCHILLOS: ai[2] = 1 CRISTAL (Orchid, extraUpdates
    //      1, daño ×0,5, 20 penetración de armadura — ataca rápido y
    //      descansa tras 6 golpes), 2 ROCA (OrangeRed, extraUpdates 2,
    //      daño ×3 — embiste y RETROCEDE) y 3 LLAMA (Orange, daño ×1 —
    //      barre en arco amplio).
    //    · EL SLICE (SliceTarget) con su AttackDelay, la aceleración
    //      1.03/0.93 por cercanía, el giro 0.15 y la carga de la llama
    //      (36 px + AttackDelay 15) — BYTE A BYTE.
    //    · EL DESCANSO (ReturnToRestingPosition): el abanico orbital a
    //      220 px con el seno del original.
    //    · EL ON-HIT (justHit): los efectos de golpe por tipo.
    //  LAS ADAPTACIONES (SOLO donde Calamity es propietario):
    //    · MinionHoming(1100f) → el buscador local de la misma ley (la
    //      presa más cercana en 1100 px que pueda ser perseguida).
    //    · LightDust/CustomSpark/GlowSparkParticle/CustomPulse → los
    //      polvos y las partículas de la casa (misma intención visual:
    //      chispas y blooms del color del cuchillo).
    //    · Owner.Calamity().providenciaStabber → el patrón de minion de
    //      la casa (el buff refresca 18000 mientras viva la escuadra —
    //      StellarCometBuff, la regla de la v6.51.12).
    //    · HolyFlames (Calamity) → LLAMAS SAGRADAS de la casa (el DoT
    //      sagrado local, 3 s — mismo efecto, icono propio).
    //    · DrawProjectileWithBackglow (Calamity) → el backglow pintado a
    //      mano (la tinta aditiva detrás del sprite — misma receta).
    //  LOS SPRITES: los TRES PNG ORIGINALES de Calamity (DazzlingStabber
    //  1/2/3), copiados tal cual — investigación de la casa.
    // ======================================================================
    public class DazzlingStabber : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];

        /// <summary>(Calamity: Projectile.SafeDirectionTo — la misma ley en
        /// casa: hacia el punto, normalizada segura jamás NaN.)</summary>
        private static Vector2 DireccionSegura(Projectile p, Vector2 destino, Vector2 respaldo = default)
        {
            Vector2 v = destino - p.Center;
            return v.LengthSquared() > 0.0001f ? Vector2.Normalize(v) : respaldo;
        }
        public ref float AttackDelay => ref Projectile.ai[0];
        public ref float RestOffsetAngle => ref Projectile.ai[1];
        public ref float KnifeType => ref Projectile.ai[2]; // 1 - Crystal, 2 - Stone, 3 - Fire
        public Color bladeColor = Color.White;
        public int attackCooldown = 0;
        public int attacksDone = 0;
        private bool justHit = false;
        public NPC potentialTarget;
        public float moveSmoothing = 50;
        public float damageMult = 1;
        public Vector2 storedPos;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Type] = false; // To prevent blade type desync
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 50;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 1f;
            Projectile.timeLeft = 18000;   // (Calamity: 90000 — la casa: el patrón del escuadrón)
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 28;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(attackCooldown);
            writer.Write(attacksDone);
            writer.Write(justHit);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            attackCooldown = reader.ReadInt32();
            attacksDone = reader.ReadInt32();
            justHit = reader.ReadBoolean();
        }

        public override void AI()
        {
            switch (KnifeType)
            {
                case 1: // Crystal
                    bladeColor = Color.Orchid;
                    Projectile.extraUpdates = 1;
                    damageMult = 0.5f;
                    Projectile.ArmorPenetration = 20;
                    break;
                case 2: // Rock
                    bladeColor = Color.OrangeRed;
                    Projectile.extraUpdates = 2;
                    damageMult = 3;
                    break;
                default: // Flame
                    bladeColor = Color.Orange;
                    damageMult = 1;
                    break;
            }

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3());

            ApplyPlayerBuffs();

            potentialTarget = FindTarget(1100f, Owner);

            if (potentialTarget != null && attackCooldown == 0 && AttackDelay <= 0)
            {
                storedPos = potentialTarget.Center;
            }

            // On-hit visuals
            if (justHit)
            {
                if (KnifeType == 1)
                {
                    SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Volume = 0.7f, Pitch = attacksDone * 0.05f }, Projectile.Center);
                    if (attacksDone >= 6)
                    {
                        // (Calamity: 9 LightDust + 4 CustomSpark — la casa: la
                        // misma lluvia de chispas ORQUÍDEA con los polvos.)
                        for (int i = 0; i < 9; i++)
                        {
                            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink, Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.5f) * Main.rand.NextFloat(10f, 20f));
                            dust.noGravity = true;
                            dust.scale = Main.rand.NextFloat(1.25f, 1.75f);
                            dust.color = Color.Orchid;
                        }
                        for (int i = 0; i < 4; i++)
                        {
                            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink,
                                Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.6f) * Main.rand.NextFloat(15f, 20f));
                            dust.noGravity = true;
                            dust.scale = Main.rand.NextFloat(1.35f, 1.6f);
                            dust.color = Color.Orchid;
                        }
                    }
                }
                if (KnifeType == 2)
                {
                    // (Calamity: el slam de Ravager + 5 sparks de roca — la
                    // casa: el golpe sordo + el chisporroteo pétreo.)
                    SoundEngine.PlaySound(SoundID.DD2_BetsyFlameBreath with { Volume = 0.18f, Pitch = Main.rand.NextFloat(-0.75f, -0.65f), MaxInstances = -1 }, Projectile.Center);
                    for (int i = 0; i < 5; i++)
                    {
                        Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Stone,
                            Vector2.One.RotatedByRandom(100) * Main.rand.NextFloat(1f, 5f));
                        dust.noGravity = true;
                        dust.scale = Main.rand.NextFloat(0.75f, 1.2f);
                        dust.color = Color.White;
                    }
                }
                justHit = false;
            }

            if (potentialTarget != null)
            {
                if (KnifeType == 1 && attacksDone > 0 && Utils.Distance(Projectile.Center, storedPos) < 150 && attackCooldown == 0)
                {
                    // (Calamity: GlowSparkParticle — la casa: el rastro tenue
                    // del color del cuchillo.)
                    if (Main.rand.NextBool(2))
                    {
                        Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink, -Projectile.velocity * 0.1f);
                        dust.noGravity = true;
                        dust.scale = 0.9f;
                        dust.color = bladeColor * 0.4f;
                    }
                }
                if (KnifeType == 2 && attackCooldown == 0)
                {
                    if (Main.rand.NextBool(4))
                    {
                        Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10, 10), DustID.Stone,
                            -Projectile.velocity * Main.rand.NextFloat(0.1f, 0.8f));
                        dust.noGravity = true;
                        dust.scale = Main.rand.NextFloat(0.55f, 1.2f);
                        dust.color = Color.White;
                    }
                    else
                    {
                        bool memesHatesLists = Main.rand.NextBool(5);
                        Dust dust = Dust.NewDustPerfect(Projectile.Center, memesHatesLists ? 278 : DustID.Enchanted_Pink, -Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.4f) * Main.rand.NextFloat(1f, 4f));
                        dust.noGravity = !memesHatesLists;
                        dust.scale = Main.rand.NextFloat(0.55f, 1.1f);
                        dust.color = bladeColor;
                    }
                }
                if (KnifeType == 3)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink, Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.4f) * Main.rand.NextFloat(2f, 8f));
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.65f, 1f);
                    dust.color = Color.Goldenrod;

                    // (Calamity: CustomSpark dorado — la casa: la chispa.)
                    if (Main.rand.NextBool(2))
                    {
                        Dust dust2 = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                            Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.5f) * Main.rand.NextFloat(1f, 5f));
                        dust2.noGravity = true;
                        dust2.scale = Main.rand.NextFloat(0.9f, 1.2f);
                        dust2.color = Color.Gold;
                    }
                }
                if (attackCooldown == 0)
                    SliceTarget(potentialTarget);
                else
                {
                    manageAttackCooldown();
                }
            }
            else
            {
                manageAttackCooldown();
                ReturnToRestingPosition();
            }
        }

        /// <summary>(Calamity: MinionHoming(1100f, Owner) — la misma ley en
        /// casa: la presa más cercana en 1100 px que pueda perseguirse.)</summary>
        private static NPC FindTarget(float maxRange, Player owner)
        {
            NPC best = null;
            float bestDist = maxRange * maxRange;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float d = n.Center.DistanceSQ(owner.Center);
                if (d < bestDist) { bestDist = d; best = n; }
            }
            return best;
        }

        public void manageAttackCooldown()
        {
            if (attackCooldown > 0)
            {
                attackCooldown--;
                if (KnifeType == 1)
                    ReturnToRestingPosition();
                else if (KnifeType == 2)
                    Projectile.velocity *= 0.96f;
                if (attackCooldown == 0)
                {
                    attacksDone = 0;

                    if (KnifeType == 1)
                    {
                        // (Calamity: el BloomRing + SmallBloom de la pausa del
                        // cristal — la casa: el pulso de anillo + el polvo.)
                        Particles.ParticlePresets.RingPulse(Projectile.Center, 46f, Color.Orchid, 10);
                        for (int i = 0; i < 6; i++)
                        {
                            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink, Vector2.One.RotatedByRandom(100) * Main.rand.NextFloat(4f, 8f));
                            dust.noGravity = true;
                            dust.scale = Main.rand.NextFloat(0.65f, 1.15f);
                            dust.color = Color.Orchid;
                        }
                    }
                }
            }
        }

        public void ApplyPlayerBuffs()
        {
            // (Calamity: el bool del modplayer mantiene viva la daga — la
            // casa: el patrón del escuadrón, el buff que se refresca
            // mientras la daga viva — StellarCometBuff.)
            Owner.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.Investigaciones.DazzlingStabberBuff>(), 3600);
            if (Owner.dead)
                Projectile.Kill();
        }

        public void SliceTarget(NPC target)
        {
            moveSmoothing = 50;
            // Don't do anything if the attack delay is still passing.
            if (AttackDelay > -60f)
            {
                AttackDelay--;
                if (AttackDelay > 0f)
                {
                    if (KnifeType == 1)
                    {
                        if (!Projectile.WithinRange(storedPos, 150f))
                            Projectile.velocity = Projectile.velocity.RotatedBy(0.1f);
                        else
                            Projectile.velocity *= 1.004f;
                    }
                    return;
                }
            }

            // Reset the velocity to fly upward if there is very little motion to ensure
            // that the summon does not get stuck.
            if (Projectile.velocity.Length() < 3f)
                Projectile.velocity = Vector2.UnitY * -12f;

            // If close to the target, slow down dramatically.
            if (Projectile.velocity.Length() > 5f && Projectile.WithinRange(storedPos, 50f))
                Projectile.velocity *= 0.93f;

            // Otherwise, if not close, speed up dramatically.
            else if (Projectile.velocity.Length() < (KnifeType == 2 ? 15 : 40f))
                Projectile.velocity *= 1.03f;

            float angularTurnSpeed = 0.15f;
            float angleToTargetCoords = Projectile.AngleTo(storedPos);

            if (KnifeType == 3)
            {
                if (!Projectile.WithinRange(storedPos, 50f))
                    Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(angleToTargetCoords, angularTurnSpeed).ToRotationVector2() * Projectile.velocity.Length();
            }
            if (KnifeType == 2)
            {
                Projectile.velocity = DireccionSegura(Projectile, storedPos) * 15f;
            }
            if (KnifeType == 1)
            {
                if (!Projectile.WithinRange(storedPos, 400f))
                    Projectile.velocity = DireccionSegura(Projectile, storedPos) * 20f;
            }
            // If not super close to the target but the target is very much in the line of sight of the summon, charge.
            if (!Projectile.WithinRange(storedPos, 75f) && Vector2.Dot(DireccionSegura(Projectile, storedPos), Projectile.velocity.SafeNormalize(Vector2.Zero)) > 0.99f && KnifeType == 3)
            {
                Projectile.velocity = DireccionSegura(Projectile, storedPos) * 36f;
                AttackDelay = 15f;

                Projectile.netUpdate = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public void ReturnToRestingPosition()
        {
            if (moveSmoothing > 2)
                moveSmoothing--;
            Projectile.rotation = Projectile.rotation.AngleTowards(RestOffsetAngle + Main.GlobalTimeWrappedHourly, 0.25f);

            float sine = Math.Abs((float)Math.Sin((Main.GlobalTimeWrappedHourly + RestOffsetAngle * 1.8f) * 8f / MathHelper.Pi));

            // Rapidly approach the resting position.
            Vector2 destination = Owner.Center + Vector2.UnitY.RotatedBy(RestOffsetAngle + Main.GlobalTimeWrappedHourly) * -220f * (MathHelper.Lerp(sine, 0.8f, 0.8f));
            Projectile.velocity = (destination - Projectile.Center) / (potentialTarget == null ? moveSmoothing : 18);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            justHit = true;
            if (KnifeType == 1)
            {
                AttackDelay = 15;
                attacksDone++;
                if (attacksDone >= 6)
                    attackCooldown = Main.rand.Next(80, 95 + 1);
            }
            if (KnifeType == 2)
            {
                Projectile.velocity = (-Projectile.velocity.SafeNormalize(Vector2.UnitX) * 15).RotatedByRandom(0.7f);
                attackCooldown = Main.rand.Next(45, 60 + 1);
            }
            if (KnifeType == 3)
            {
                if (attackCooldown == 0)
                    attackCooldown = 5;
                // (Calamity: HolyFlames 180 — la casa: LLAMAS SAGRADAS, el
                // DoT sagrado local, misma duración de 3 s.)
                target.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.Investigaciones.LlamasSagradas>(), 180);
            }
            Projectile.netUpdate = true;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.SourceDamage *= damageMult;

        public override bool? CanDamage() => (attackCooldown == 0 ? base.CanDamage() : false);

        public override bool PreDraw(ref Color lightColor)
        {
            // (Calamity: DrawProjectileWithBackglow con su textura por tipo
            // — la casa: la MISMA receta a mano: el backglow aditivo del
            // color del cuchillo DETRÁS + el sprite ENCIMA.)
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            if (KnifeType == 1)
                tex = ModContent.Request<Texture2D>("AethonMod/Content/Projectiles/Investigaciones/DazzlingStabber").Value;
            if (KnifeType == 2)
                tex = ModContent.Request<Texture2D>("AethonMod/Content/Projectiles/Investigaciones/DazzlingStabber2").Value;
            if (KnifeType == 3)
                tex = ModContent.Request<Texture2D>("AethonMod/Content/Projectiles/Investigaciones/DazzlingStabber3").Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origen = tex.Size() * 0.5f;
            try
            {
                // EL BACKGLOW (la tinta aditiva detrás — el ×3.5 del original).
                VFXCore.AbrirAdditive();
                try
                {
                    Color glow = bladeColor with { A = 0 };
                    Main.spriteBatch.Draw(tex, drawPos, null, glow * 0.28f,
                        Projectile.rotation, origen, 3.5f, SpriteEffects.None, 0f);
                }
                finally { VFXCore.RestaurarLoteVanilla(); }
            }
            catch { }

            // EL SPRITE (el pase normal, intacto).
            Main.spriteBatch.Draw(tex, drawPos, null, lightColor, Projectile.rotation, origen, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
