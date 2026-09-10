using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// AbyssalTentacleProjectile — tentáculo que se extiende hacia los enemigos.
    ///
    /// Visuales:
    ///   - Cadena de SoftGlow desde el jugador al proyectil (10 puntos interpolados)
    ///   - Colores púrpura-verde oscuro
    ///   - Dust púrpura en trail
    ///
    /// Físicas:
    ///   - Movimiento lento, homing hacia NPC más cercano
    ///   - timeLeft = 150, penetrate = 3
    /// </summary>
    public class AbyssalTentacleProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 150;
            Projectile.light = 0.3f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 1f;
                Projectile.rotation += 0.05f;

                // Damping — movimiento lento
                Projectile.velocity *= 0.96f;

                // Homing hacia el NPC más cercano
                NPC target = FindNearestNPC(450f);
                if (target != null)
                {
                    Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                    float homingStrength = 0.35f;
                    Projectile.velocity += toTarget * homingStrength;

                    // Velocidad máxima
                    float maxSpeed = 9f;
                    if (Projectile.velocity.Length() > maxSpeed)
                        Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * maxSpeed;
                }

                // Dust púrpura en trail
                if (Main.rand.NextBool(4))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                        -Projectile.velocity * 0.2f + new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-0.5f, 0.5f)),
                        150, new Color(120, 60, 180), 1.0f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Lighting púrpura-verde
                Lighting.AddLight(Projectile.Center, new Vector3(0.15f, 0.30f, 0.20f));
            }
            catch { }
        }

        private NPC FindNearestNPC(float maxDist)
        {
            try
            {
                NPC nearest = null;
                float nearestDist = maxDist;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active) continue;
                    if (npc.friendly || npc.townNPC) continue;
                    if (npc.dontTakeDamage) continue;
                    if (!npc.CanBeChasedBy()) continue;

                    float dist = Vector2.Distance(npc.Center, Projectile.Center);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = npc;
                    }
                }
                return nearest;
            }
            catch { return null; }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                for (int i = 0; i < 12; i++)
                {
                    float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float s = Main.rand.NextFloat(2f, 6f);
                    Vector2 v = new Vector2((float)Math.Cos(a) * s, (float)Math.Sin(a) * s);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, v, 200, new Color(120, 60, 180), 1.1f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
            catch { }
        }

        public override void Kill(int timeLeft)
        {
            try
            {
                for (int i = 0; i < 18; i++)
                {
                    float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float s = Main.rand.NextFloat(3f, 7f);
                    Vector2 v = new Vector2((float)Math.Cos(a) * s, (float)Math.Sin(a) * s);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, v, 210, new Color(120, 60, 180), 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (softGlow == null) return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 origin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                float t = Main.GameUpdateCount;

                // Origen del tentáculo: posición del jugador dueño
                Player owner = Main.player[Projectile.owner];
                Vector2 ownerCenter = owner != null && owner.active ? owner.Center : Projectile.Center;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === CADENA DE TENTÁCULO desde player al proyectil ===
                // 10 puntos interpolados, color púrpura-verde oscuro, pulsa
                int segments = 10;
                Vector2 start = ownerCenter;
                Vector2 end = Projectile.Center;
                for (int i = 0; i <= segments; i++)
                {
                    float progress = (float)i / (float)segments;
                    Vector2 basePos = Vector2.Lerp(start, end, progress);

                    // Wiggle sinusoidal para que el tentáculo parezca "vivo"
                    float wiggleAmp = 12f;
                    Vector2 dir = (end - start);
                    if (dir.Length() > 0.1f)
                    {
                        dir = dir.SafeNormalize(Vector2.Zero);
                        Vector2 perp = new Vector2(-dir.Y, dir.X);
                        float wiggle = (float)Math.Sin(t * 0.08f + i * 0.6f) * wiggleAmp * (1f - Math.Abs(progress - 0.5f) * 2f);
                        basePos += perp * wiggle;
                    }

                    Vector2 segDrawPos = basePos - Main.screenPosition;

                    // Color interpola de púrpura (player) a verde oscuro (projectile)
                    Color purpleEnd = new Color(110, 30, 160);
                    Color greenEnd = new Color(40, 110, 70);
                    Color c = new Color(
                        (byte)(purpleEnd.R * (1f - progress) + greenEnd.R * progress),
                        (byte)(purpleEnd.G * (1f - progress) + greenEnd.G * progress),
                        (byte)(purpleEnd.B * (1f - progress) + greenEnd.B * progress),
                        200);
                    // Pulso de brillo
                    float pulse = 0.9f + (float)Math.Sin(t * 0.12f + i * 0.4f) * 0.1f;

                    // Escala decrece desde el jugador al proyectil (base ancha, punta fina)
                    float scale = (1.4f - progress * 0.7f) * pulse;

                    Main.spriteBatch.Draw(softGlow, segDrawPos, null,
                        c, 0f, origin, scale * 0.4f, SpriteEffects.None, 0f);
                }

                // === CABEZA del tentáculo (en Projectile.Center) ===
                // Glow principal púrpura-verde
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(140, 50, 180, 220),
                    0f, origin, 0.9f, SpriteEffects.None, 0f);

                // Glow secundario verde
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(60, 140, 90, 180),
                    Projectile.rotation, origin, 0.7f, SpriteEffects.None, 0f);

                // Núcleo brillante
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(200, 150, 220, 160),
                    0f, origin, 0.4f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
