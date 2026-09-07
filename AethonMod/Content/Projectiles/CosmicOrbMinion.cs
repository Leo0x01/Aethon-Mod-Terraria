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
        private NPC? currentTarget = null;

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

            // === ACTUALIZAR HIT COOLDOWN SEGÚN NIVEL DEL GRIMORIO ===
            // Base 15, -1 cada 10 niveles, mínimo 1.
            Item? held = owner.HeldItem;
            if (held != null && held.type == ModContent.ItemType<Weapons.GrimoireEternal>())
            {
                try
                {
                    var sl = held.GetGlobalItem<Globals.ShardLevelItem>();
                    if (sl != null)
                    {
                        Projectile.localNPCHitCooldown = WeaponScaling.MinionHitCooldown(sl.Level);
                    }
                }
                catch { }
            }

            // === BUSCAR ENEMIGO ===
            NPC? target = FindHostileTarget(owner);
            attacking = target != null;
            currentTarget = target;

            if (attacking && currentTarget != null)
            {
                // === MODO ATAQUE: volar hacia el enemigo ===
                Vector2 toTarget = currentTarget.Center - Projectile.Center;
                float dist = toTarget.Length();

                if (dist > 0.1f)
                {
                    // Velocidad escala con nivel del Grimorio (base 16, +50% cada 30 niveles, tope 3x)
                    // Nota: 'held' ya fue declarado arriba en AI() — lo reutilizamos (fix CS0136).
                    float baseSpeed = 16f;
                    float speedMult = 1f;
                    if (held != null && held.type == ModContent.ItemType<Weapons.GrimoireEternal>())
                    {
                        try
                        {
                            var sl = held.GetGlobalItem<Globals.ShardLevelItem>();
                            if (sl != null)
                                speedMult = WeaponScaling.MinionSpeedMult(sl.Level);
                        }
                        catch { }
                    }
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
                // === MODO IDLE: orbitar al jugador ===
                orbitAngle += 0.04f;
                float orbitRadius = 45f + Projectile.minionPos * 22f;
                Vector2 orbitOffset = new Vector2(
                    (float)System.Math.Cos(orbitAngle + Projectile.minionPos * 1.5f) * orbitRadius,
                    (float)System.Math.Sin(orbitAngle + Projectile.minionPos * 1.5f) * orbitRadius * 0.4f
                );
                Vector2 targetPos = owner.Center + orbitOffset;

                Vector2 direction = targetPos - Projectile.Center;
                float distToOrbit = direction.Length();
                if (distToOrbit > 0.1f)
                {
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction * 0.08f, 0.1f);
                }
                else
                {
                    Projectile.velocity *= 0.8f;
                }

                // Rotación suave en idle
                Projectile.rotation += 0.04f;

                // Partículas suaves en idle
                if (Main.rand.NextBool(8))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                        new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), Main.rand.NextFloat(-0.2f, 0.2f)),
                        250, new Color(255, 217, 61), 0.15f);
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

        private NPC? FindHostileTarget(Player owner)
        {
            // Rango de detección escala con nivel del Grimorio (base 500, +100 cada 10 niveles, tope 1500)
            float detectionRange = 500f;
            Item? held = owner.HeldItem;
            if (held != null && held.type == ModContent.ItemType<Weapons.GrimoireEternal>())
            {
                try
                {
                    var sl = held.GetGlobalItem<Globals.ShardLevelItem>();
                    if (sl != null)
                        detectionRange = WeaponScaling.MinionDetectionRange(sl.Level);
                }
                catch { }
            }

            NPC? closest = null;
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
