using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// GalaxyStarProjectile — LAS SEMILLAS ESTELARES de LA GALAXIA VIVIENTE.
    ///
    /// v6.00 — Las estrellas que los BRAZOS de la galaxia sueltan al girar
    /// (sembra cada 24 ticks) y las 14 que estallan en la EXPLOSIÓN final.
    /// Estrellas de 4 puntas girando sobre sí mismas, con halo suave y una
    /// estela de eco — cada una con el COLOR de su origen: AZUL (brazos de
    /// estrellas jóvenes), ORO (bulbo viejo) o ROSA (nudo HII de formación
    /// estelar).
    ///
    /// Campos: ai[0] = índice de color (0 azul / 1 oro / 2 rosa) ·
    /// ai[1] = giro propio.
    /// </summary>
    public class GalaxyStarProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.light = 0.65f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            // giro propio (la estrella rueda por el espacio)
            Projectile.ai[1] += 0.28f;

            // vuelo recto y limpio — una semilla estelar no duda
            // (el leve arco: gravedad nula, solo deriva digna)
            Projectile.velocity *= 0.995f;

            // estela de polvo
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    -Projectile.velocity * 0.1f +
                        new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-0.3f, 0.3f)),
                    160, StarColor(Projectile.ai[0]), 0.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f)),
                    200, StarColor(Projectile.ai[0]), 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 8; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float spd = Main.rand.NextFloat(1f, 3.5f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * spd,
                    210, StarColor(Projectile.ai[0]), 0.65f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>El color de la estrella según su origen en la galaxia.</summary>
        private static Color StarColor(float idx)
        {
            return ((int)idx % 3) switch
            {
                0 => new Color(170, 215, 255),   // azul: brazos jóvenes
                1 => new Color(255, 230, 170),   // oro: bulbo viejo
                _ => new Color(255, 175, 215),   // rosa: nudo HII
            };
        }

        // ================================================================
        //  RENDER — la semilla estelar (4 puntas girando + halo + eco)
        // ================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Texture2D star = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/Star").Value;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float spin = Projectile.ai[1];
                Color col = StarColor(Projectile.ai[0]);

                // vida (se apaga al final)
                float fade = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // el ECO (estela tenue detrás)
                Vector2 echo = drawPos - Projectile.velocity * 0.09f;
                Main.spriteBatch.Draw(star, echo, null,
                    new Color(col.R, col.G, col.B, (byte)(70 * fade)), spin,
                    star.Size() * 0.5f, 0.55f, SpriteEffects.None, 0f);

                // el HALO
                Main.spriteBatch.Draw(glow, drawPos, null,
                    new Color(col.R, col.G, col.B, (byte)(130 * fade)), 0f,
                    glow.Size() * 0.5f, 26f / (glow.Width * 0.5f),
                    SpriteEffects.None, 0f);

                // la ESTRELLA de 4 puntas (núcleo blanco + tinte del origen)
                Main.spriteBatch.Draw(star, drawPos, null,
                    new Color(255, 255, 255, (byte)(255 * fade)), spin,
                    star.Size() * 0.5f, 13f / (star.Width * 0.5f),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(star, drawPos, null,
                    new Color(col.R, col.G, col.B, (byte)(160 * fade)), spin,
                    star.Size() * 0.5f, 20f / (star.Width * 0.5f),
                    SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                    null, Main.GameViewMatrix.TransformationMatrix);
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                    null, Main.GameViewMatrix.TransformationMatrix);
            }
            return false;
        }
    }
}
