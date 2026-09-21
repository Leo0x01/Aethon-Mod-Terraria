using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// PlasmaStormProjectile — un orbe de plasma púrpura-magenta pulsante.
    /// Cuando dos orbes se acercan (menos de 50px) dibuja un BeamCyan entre ellos.
    ///
    /// Visuales:
    ///   - SoftGlow con color púrpura-magenta, pulsante.
    ///   - Trail de PurpleTorch dust.
    ///   - Busca otros PlasmaStormProjectile cercanos y dibuja BeamCyan entre ellos.
    ///
    /// penetrate = 2, timeLeft = 90, extraUpdates = 1.
    /// </summary>
    public class PlasmaStormProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 90;
            Projectile.light = 0.5f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 1f;
                Projectile.rotation += 0.1f;

                // Mild homing
                NPC target = null;
                float minDist = 350f;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    float d = (npc.Center - Projectile.Center).Length();
                    if (d < minDist) { minDist = d; target = npc; }
                }
                if (target != null)
                {
                    Vector2 toTarget = (target.Center - Projectile.Center);
                    if (toTarget.Length() > 0.1f)
                    {
                        toTarget.Normalize();
                        Projectile.velocity += toTarget * 0.1f;
                        if (Projectile.velocity.Length() > 10f)
                            Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 10f;
                    }
                }

                // Trail of PurpleTorch dust
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                {
                    Vector2 dustPos = Projectile.Center + new Vector2(
                        Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f));
                    Vector2 dustVel = -Projectile.velocity * 0.1f + new Vector2(
                        Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-0.5f, 0.5f));
                    Dust d = Dust.NewDustPerfect(dustPos, DustID.PurpleTorch,
                        dustVel, 150, new Color(200, 80, 230), 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                Lighting.AddLight(Projectile.Center, new Vector3(0.7f, 0.2f, 0.9f));
            }
            catch { }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                target.AddBuff(BuffID.Frostburn, 120); // plasma burn
                for (int i = 0; i < 6; i++)
                {
                    float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float s = Main.rand.NextFloat(2f, 5f);
                    Vector2 v = new Vector2((float)Math.Cos(a) * s, (float)Math.Sin(a) * s);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                        v, 200, new Color(220, 100, 255), 1.0f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            Texture2D beamCyan = ModContent.Request<Texture2D>("AethonMod/Content/Effects/BeamCyan").Value;
            if (softGlow == null || beamCyan == null) return false;

            // v6.50.3 — BLINDAJE (hallazgo V-1): el End+restore vivía DENTRO del
            // try con catch vacío — una excepción a mitad de draw dejaba el lote
            // aditivo ABIERTO el resto del pase del frame. El restore ahora
            // vive en finally (como los hermanos PhoenixNova/Supernova).
            try
            {
                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                float t = Projectile.ai[0];
                float pulse = 0.7f + 0.3f * (float)Math.Sin(t * 0.25f);

                Main.spriteBatch.End();
                // v6.50.3 — FIX (contrato de lote de la casa — el único archivo
                // V20 que lo incumplía): 2 args = SIN matriz (con zoom≠100%
                // el orbe se dibujaba desplazado) y SIN sampler/rasterizer;
                // el restore en Identity + AlphaBlend pelado dejaba el RESTO
                // del pase de proyectiles del frame dibujándose sin zoom.
                // Ahora: additive con el sampler/rasterizer del pase de
                // entidades + Main.Transform, y el RESTORE con el patrón
                // exacto de v6.50.2 (AuraLib.ReabrirLoteVanilla, medido en IL).
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.Transform);

                // === Plasma orb (purple-magenta pulsing) ===
                // Outer magenta halo
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(220, 60, 220, 180) * pulse,
                    0f, glowOrigin, 1.2f * pulse, SpriteEffects.None, 0f);
                // Inner purple
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(180, 80, 255, 220) * pulse,
                    0f, glowOrigin, 0.7f * pulse, SpriteEffects.None, 0f);
                // White-hot core
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 240, 255, 230),
                    0f, glowOrigin, 0.3f * pulse, SpriteEffects.None, 0f);

                // === Electric arcs to nearby plasma orbs (< 50px) ===
                // Scan all projectiles of same type
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile other = Main.projectile[i];
                    if (other == null || !other.active) continue;
                    if (other.type != Projectile.type) continue;
                    if (other.whoAmI == Projectile.whoAmI) continue;
                    if (other.owner != Projectile.owner) continue;

                    Vector2 diff = other.Center - Projectile.Center;
                    float dist = diff.Length();
                    if (dist > 50f || dist < 1f) continue;

                    // Draw a BeamCyan between us
                    float arcAlpha = 1f - dist / 50f;
                    Vector2 beamOrigin = new Vector2(0f, beamCyan.Height / 2f);
                    float beamRotation = (float)Math.Atan2(diff.Y, diff.X);
                    float beamScale = dist / (float)beamCyan.Width;

                    Main.spriteBatch.Draw(beamCyan, drawPos, null,
                        new Color(180, 220, 255, (byte)(220 * arcAlpha)),
                        beamRotation, beamOrigin,
                        new Vector2(beamScale, 1.0f + (float)Math.Sin(t * 0.5f) * 0.3f),
                        SpriteEffects.None, 0f);
                }
            }
            catch { }
            finally
            {
                try { Main.spriteBatch.End(); } catch { }
                try { AuraLib.ReabrirLoteVanilla(); } catch { } // restore del pase de entidades (v6.50.2)
            }
            return false;
        }
    }
}
