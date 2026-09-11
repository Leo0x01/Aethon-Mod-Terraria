using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Projectiles
{
    /// <summary>
    /// Cosmic Orb Minion — minion con IA tipo Terraprisma usando sprite custom.
    ///
    /// Comportamiento (como Terraprisma):
    /// - Idle: orbita al jugador
    /// - Ataque: vuela hacia el enemigo más cercano, ataca por contacto
    /// - Sin proyectiles — daño por contacto directo
    ///
    /// Sprite: minion cosmico.png del usuario.
    /// </summary>
    public class CosmicOrbMinion : ModProjectile
    {
        private float orbitAngle = 0f;
        private bool attacking = false;
        private NPC currentTarget = null;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18000;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            // Hit cooldown que mejora con el nivel: 15 frames base, -1 cada 10 niveles, mínimo 1.
            // Nivel 1-9: 15 frames, nivel 10-19: 14, ..., nivel 140+: 1 frame.
            Projectile.localNPCHitCooldown = 15;
            Projectile.aiStyle = -1;
            Projectile.light = 0.8f;
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

            CheckMinionBuff(owner);

            // v5.29: Obtener nivel del Grimorio cacheado en ai[2].
            // Si ai[2] > 0, usar el nivel cacheado (no depende de HeldItem).
            // Si ai[2] == 0 (minion viejo o invocado sin cache), intentar leer HeldItem.
            int level = (int)Projectile.ai[2];
            if (level < 1)
            {
                // Fallback: leer HeldItem (comportamiento legacy)
                Item held = owner.HeldItem;
                if (held != null && held.type == ModContent.ItemType<Weapons.GrimoireEternal>())
                {
                    try
                    {
                        var sl = held.GetGlobalItem<Globals.ShardLevelItem>();
                        if (sl != null) level = sl.Level;
                    }
                    catch { }
                }
                if (level < 1) level = 1;
            }

            // === ACTUALIZAR HIT COOLDOWN SEGÚN NIVEL CACHEADO ===
            Projectile.localNPCHitCooldown = WeaponScaling.MinionHitCooldown(level);

            // === BUSCAR ENEMIGO ===
            NPC target = FindHostileTarget(owner, level);
            attacking = target != null;
            currentTarget = target;

            if (attacking && currentTarget != null)
            {
                // === MODO ATAQUE: volar hacia el enemigo ===
                Vector2 toTarget = currentTarget.Center - Projectile.Center;
                float dist = toTarget.Length();

                if (dist > 0.1f)
                {
                    // v5.29: Velocidad escala con nivel cacheado (no HeldItem)
                    float baseSpeed = 16f;
                    float speedMult = WeaponScaling.MinionSpeedMult(level);
                    Vector2 desiredVel = toTarget.SafeNormalize(Vector2.Zero) * (baseSpeed * speedMult);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVel, 0.25f);
                }

                // Rotación hacia la dirección de movimiento
                if (Projectile.velocity.Length() > 1f)
                    Projectile.rotation = Projectile.velocity.ToRotation();

                // Partículas doradas al atacar
                if (Main.rand.NextBool(4))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                        new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-0.4f, 0.4f)),
                        250, new Color(255, 217, 61), 0.15f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
            else
            {
                // === MODO IDLE: orbitar al jugador CERCA ===
                // v5.2: radio reducido y compacto para muchos minions.
                // Antes: 45 + minionPos*22 = enorme con muchos minions.
                // Ahora: radio fijo pequeño + separación angular uniforme.
                orbitAngle += 0.05f;

                // Radio compacto: 30px base + 4px por minion (tope ~80px con 12+ minions)
                float orbitRadius = 30f + System.Math.Min(Projectile.minionPos * 4f, 50f);

                // Separación angular uniforme entre minions (360° / numMinions)
                float angleOffset = orbitAngle + (Projectile.minionPos * MathHelper.TwoPi / 8f);

                Vector2 orbitOffset = new Vector2(
                    (float)System.Math.Cos(angleOffset) * orbitRadius,
                    (float)System.Math.Sin(angleOffset) * orbitRadius * 0.5f
                );
                Vector2 targetPos = owner.Center + orbitOffset;

                Vector2 direction = targetPos - Projectile.Center;
                float distToOrbit = direction.Length();
                if (distToOrbit > 0.1f)
                {
                    // Movimiento más responsivo (lerp 0.2 en vez de 0.1)
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction * 0.3f, 0.2f);
                }
                else
                {
                    Projectile.velocity *= 0.8f;
                }

                // Rotación suave en idle — el minion gira sobre sí mismo
                Projectile.rotation += 0.06f;
                // Rotación adicional para que "miren" hacia donde se mueven
                if (Projectile.velocity.Length() > 0.5f)
                    Projectile.rotation = Projectile.velocity.ToRotation();

                // Partículas suaves en idle (más frecuentes para mejor efecto visual)
                if (Main.rand.NextBool(5))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                        new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-0.3f, 0.3f)),
                        200, new Color(255, 217, 61), 0.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                // Partícula cian ocasional
                if (Main.rand.NextBool(12))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                        new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-0.3f, 0.3f)),
                        200, new Color(0, 255, 255), 0.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Luz
            Lighting.AddLight(Projectile.Center, new Vector3(0.8f, 0.6f, 0.3f));
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

            if (!hasBuff) Projectile.Kill();
            else owner.AddBuff(buffType, 18000);
        }

        private NPC FindHostileTarget(Player owner, int level)
        {
            // v5.29: Usar nivel cacheado (pasado como parámetro) en vez de leer HeldItem
            float detectionRange = WeaponScaling.MinionDetectionRange(level);

            NPC closest = null;
            float closestDist = detectionRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active) continue;
                if (npc.friendly || npc.townNPC) continue;
                if (npc.dontTakeDamage) continue;
                if (npc.aiStyle == 7) continue;
                if (npc.catchItem > 0) continue;
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

        /// <summary>
        /// v5.29: Apply MinionContactDamageMult — multiplica el daño de contacto
        /// del minion según el nivel cacheado del Grimorio.
        /// +50% cada 15 niveles (tope +500%).
        /// </summary>
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            int level = (int)Projectile.ai[2];
            if (level < 1) level = 1;
            modifiers.SourceDamage *= WeaponScaling.MinionContactDamageMult(level);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2)),
                    250, new Color(255, 217, 61), 0.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            for (int i = 0; i < 4; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2)),
                    250, new Color(0, 255, 255), 0.15f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        // Nota: el override PreDraw fue eliminado porque causaba que el minion
        // fuera invisible (alpha=0 → transparente). Con el sprite redimensionado
        // a 32x32 (cuadrado), el draw default de tModLoader ya rota alrededor
        // del centro correctamente. El issue del "gira por arriba" era por el
        // tamaño gigante del sprite (713x664), no por el pivot.
    }
}
