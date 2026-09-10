using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Custom
{
    /// <summary>
    /// SkyLightningStrike — relámpago que cae del cielo hacia el cursor.
    /// Se dibuja como un rayo vertical con ramificaciones + flash de impacto.
    /// El proyectil se mueve rápidamente hacia abajo y explota al impactar.
    /// </summary>
    public class SkyLightningStrike : ModProjectile
    {
        private float StrikeTimer { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private bool HasImpacted { get => Projectile.ai[1] == 1f; set => Projectile.ai[1] = value ? 1f : 0f; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1; // atraviesa todo
            Projectile.timeLeft = 60;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 2;
        }

        public override void AI()
        {
            StrikeTimer += 1f;

            // Caída rápida vertical
            Projectile.velocity.Y = 20f;
            Projectile.velocity.X = 0f;

            // Estela eléctrica (chispas azules-blancas)
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-1f, 1f)),
                    200, new Color(150, 200, 255), 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Chispas amarillas
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-2f, 2f)),
                    180, new Color(255, 255, 200), 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Luz de relámpago intensa
            float pulse = 0.9f + 0.1f * (float)Math.Sin(StrikeTimer * 0.5f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.6f * pulse, 0.8f * pulse, 1f * pulse));

            // Verificar impacto con suelo o enemigos
            if (!HasImpacted)
            {
                Tile tile = Framing.GetTileSafely((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f));
                if (tile != null && tile.HasTile && Main.tileSolid[tile.TileType])
                {
                    ImpactEffect();
                    HasImpacted = true;
                }
            }
            else
            {
                // Después del impacto, mantener el rayo visible pero reducir alpha
                if (StrikeTimer > 30f)
                    Projectile.Kill();
            }
        }

        private void ImpactEffect()
        {
            // Flash de impacto masivo
            for (int i = 0; i < 30; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float speed = Main.rand.NextFloat(3f, 8f);
                Vector2 dir = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed - 2f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 220, new Color(255, 255, 200), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Onda expansiva eléctrica
            for (int i = 0; i < 20; i++)
            {
                float angle = (MathHelper.TwoPi / 20) * i;
                Vector2 dir = new Vector2((float)Math.Cos(angle) * 5f, (float)Math.Sin(angle) * 5f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    dir, 200, new Color(150, 200, 255), 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Sonido de trueno
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Thunder, Projectile.Center);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!HasImpacted)
            {
                ImpactEffect();
                HasImpacted = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                float t = Main.GameUpdateCount;
                float pulse = 0.8f + 0.2f * (float)Math.Sin(t * 0.4f);

                // === 1. RAYO PRINCIPAL (con flickering) ===
                // Dibujar el sprite de LightningStrike vertical con parpadeo
                float flicker = Main.rand.NextFloat(0.8f, 1.2f);
                DrawAdditive("AethonMod/Content/Effects/Custom/LightningStrike",
                    Projectile.Center, 1.0f * flicker, new Color(200, 220, 255, 200), 0f);

                // === 2. GLOW BLANCO-CIAN ALREDEDOR ===
                DrawAdditive("AethonMod/Content/Effects/GlowOrbWhite",
                    Projectile.Center, 0.5f * pulse, new Color(255, 255, 255, 200), 0f);

                DrawAdditive("AethonMod/Content/Effects/GlowCircleCyan",
                    Projectile.Center, 0.8f * pulse, new Color(150, 200, 255, 150), 0f);

                // === 3. RAMIFICACIONES (beam cian a los lados) ===
                for (int i = 0; i < 3; i++)
                {
                    float angle = (MathHelper.Pi / 4) * (i - 1); // -45, 0, +45 grados
                    DrawAdditive("AethonMod/Content/Effects/BeamCyan",
                        Projectile.Center + new Vector2(0, i * 15f - 15f), 0.6f * pulse,
                        new Color(150, 220, 255, 100), angle + (float)Math.Sin(t * 0.3f + i) * 0.2f);
                }

                // === 4. FLASH DE IMPACTO ===
                if (HasImpacted)
                {
                    float flashScale = 2.0f + (float)Math.Sin(StrikeTimer * 0.5f) * 0.5f;
                    DrawAdditive("AethonMod/Content/Effects/GlowCircleWhite",
                        Projectile.Center, flashScale, new Color(255, 255, 255, 180), 0f);
                    DrawAdditive("AethonMod/Content/Effects/Custom/Shockwave",
                        Projectile.Center, 1.5f, new Color(200, 220, 255, 150), 0f);
                }
            }
            catch { }
            return false;
        }

        private void DrawAdditive(string path, Vector2 pos, float scale, Color color, float rotation)
        {
            try
            {
                Texture2D tex = ModContent.Request<Texture2D>(path).Value;
                if (tex == null) return;
                Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
                Vector2 drawPos = pos - Main.screenPosition;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                Main.spriteBatch.Draw(tex, drawPos, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
        }
    }
}
