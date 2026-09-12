using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// v5.99 — NebulaLightning — EL RAYO NEBULAR (ataque de LA MEDUSA).
    ///
    /// Petición del usuario: "sus proyectiles son aburridos, es mejor que el
    /// proyectil que usa la medusa sean rayos, ya sabes, los rayos que caen
    /// del cielo". ADIÓS agujas de luz: cuando la campana se contrae junto
    /// a una víctima, la medusa DESCARGA un rayo cósmico QUE CAE DEL CIELO
    /// sobre el objetivo.
    ///
    /// EL RAYO: una descarga vertical de plasma frío aqua-blanco con núcleo
    /// incandescente, zigzag CHISPEANTE (regenerado cada pocos ticks — vive),
    /// ramas laterales cortas y un frente brillante que baja a toda
    /// velocidad. Al clavarse: destello de impacto + chispas de hielo.
    /// Aplica QUEMADURA DE HIELO (Frostburn — la quemadura fría del vacío,
    /// la firma de la medusa).
    ///
    /// Determinista: el zigzag se deriva de (semilla, tick, segmento) con
    /// hash puro → todas las máquinas ven el MISMO rayo sin sincronizar nada.
    /// Campos: ai[0] = profundidad del impacto (Y del objetivo) ·
    /// ai[1] = semilla del zigzag · localAI[0] = Y de nacimiento.
    /// </summary>
    public class NebulaLightning : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 26;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.light = 1.0f;
            Projectile.extraUpdates = 2;   // baja a ~90 px/t: un rayo ES rápido
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            // la Y de nacimiento (para dibujar el trazo del cielo a aquí)
            if (Projectile.localAI[0] == 0f)
                Projectile.localAI[0] = Projectile.Center.Y;

            // el rayo CAE: vertical, sin dudas
            Projectile.velocity = new Vector2(0f, 30f);

            // chispas descendentes alrededor del frente (la estela del rayo)
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center +
                    new Vector2(Main.rand.NextFloat(-10f, 10f), Main.rand.NextFloat(-8f, 4f)),
                    DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), 1.6f),
                    180, Main.rand.NextBool(3)
                        ? new Color(140, 245, 255)
                        : new Color(255, 170, 230), 0.55f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // llegada a la profundidad del objetivo → IMPACTO
            if (Projectile.Center.Y >= Projectile.ai[0])
                Projectile.Kill();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // la quemadura fría del vacío (firma de la medusa)
            try { target.AddBuff(BuffID.Frostburn, 300); } catch { }
            if (Main.netMode == NetmodeID.Server) return;

            for (int i = 0; i < 7; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-2.6f, 2.6f), Main.rand.NextFloat(-2.6f, 0.8f)),
                    210, new Color(170, 245, 255), 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // EL IMPACTO: trueno eléctrico + destello de hielo en el suelo
            try
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item12,
                    Projectile.Center);
            }
            catch { }

            for (int i = 0; i < 22; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float spd = Main.rand.NextFloat(1.5f, 5.5f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang) * 0.7f) * spd,
                    220, Main.rand.NextBool(2)
                        ? new Color(160, 245, 255)
                        : new Color(255, 180, 235), 0.75f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        // ================================================================
        //  RENDER — el rayo dentado cayendo del cielo
        // ================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Texture2D star = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/Star").Value;

                Vector2 head = Projectile.Center - Main.screenPosition;
                float spawnY = Projectile.localAI[0];
                if (spawnY <= 0f) return false;
                Vector2 top = new Vector2(head.X, spawnY - Main.screenPosition.Y);

                // el rayo NO baja de más arriba de lo que ha vivido: nace donde
                // nació y su cola lo sigue (un trazo, no una columna infinita)
                float len = head.Y - top.Y;
                if (len < 12f) len = 12f;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // vida del rayo (se apaga al final)
                float lifeT = 1f - Projectile.timeLeft / 26f;
                float fade = 1f - lifeT * lifeT * 0.7f;

                // === el ZIGZAG: nace del hash (semilla, flick, segmento) ===
                int flick = (int)(Main.GlobalTimeWrappedHourly * 20f); // ~3 ticks
                int seed = (int)Projectile.ai[1];
                int segs = Math.Max(3, (int)(len / 24f) + 1);

                Vector2 prev = top;
                for (int s = 0; s < segs; s++)
                {
                    float t0 = s / (float)segs;
                    float t1 = (s + 1) / (float)segs;
                    // el zigzag se AMPLIA en el medio y afina al final (un rayo
                    // real tiene la parte media más ramificada)
                    float amp = 20f * (float)Math.Sin(t0 * Math.PI);
                    float jx = (Hash01(seed, flick, s) - 0.5f) * 2f * amp;
                    float jx1 = (Hash01(seed, flick, s + 1) - 0.5f) * 2f *
                                20f * (float)Math.Sin(t1 * Math.PI);
                    Vector2 p0 = top + new Vector2(jx, t0 * len);
                    Vector2 p1 = top + new Vector2(jx1, t1 * len);

                    // segmento como glow estirado (halo aqua + núcleo blanco)
                    DrawSegment(glow, p0, p1, 7.5f * fade,
                        new Color(90, 220, 255, (byte)(120 * fade)));
                    DrawSegment(glow, p0, p1, 3.0f * fade,
                        new Color(235, 252, 255, (byte)(235 * fade)));

                    // RAMA lateral corta (una de cada dos segmentos, aleatoria)
                    if (s > 0 && s < segs - 1 && Hash01(seed, flick, s + 91) > 0.62f)
                    {
                        float side = Hash01(seed, flick, s + 37) > 0.5f ? 1f : -1f;
                        Vector2 bEnd = p0 + new Vector2(side * 26f, 18f) *
                            (0.6f + 0.4f * Hash01(seed, flick, s + 53));
                        DrawSegment(glow, p0, bEnd, 3.2f * fade,
                            new Color(130, 235, 255, (byte)(110 * fade)));
                        DrawSegment(glow, p0, bEnd, 1.4f * fade,
                            new Color(230, 250, 255, (byte)(170 * fade)));
                    }

                    prev = p1;
                }

                // === el FRENTE: la cabeza del rayo brilla mientras cae ===
                Main.spriteBatch.Draw(glow, head, null,
                    new Color(200, 250, 255, (byte)(210 * fade)), 0f,
                    glow.Size() * 0.5f, (34f * fade) / (glow.Width * 0.5f),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(star, head, null,
                    new Color(240, 255, 255, (byte)(255 * fade)), 0f,
                    star.Size() * 0.5f, (12f * fade) / (star.Width * 0.5f),
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

        /// <summary>Dibuja un segmento como glow estirado/rotado entre dos puntos.</summary>
        private static void DrawSegment(Texture2D glow, Vector2 a, Vector2 b,
            float thickness, Color col)
        {
            Vector2 delta = b - a;
            float l = delta.Length();
            if (l < 0.5f) return;
            float rot = (float)Math.Atan2(delta.Y, delta.X);
            Main.spriteBatch.Draw(glow, a, null, col, rot,
                new Vector2(0f, glow.Height * 0.5f),
                new Vector2(l / (glow.Width * 0.5f), thickness / (glow.Height * 0.5f)),
                SpriteEffects.None, 0f);
        }

        /// <summary>Hash determinista [0,1) — el MISMO rayo en todas las máquinas.</summary>
        private static float Hash01(int seed, int flick, int seg)
        {
            float h = (float)Math.Abs(Math.Sin(
                seed * 12.9898f + seg * 78.233f + flick * 37.719f) * 43758.5453f);
            return h - (float)Math.Floor(h);
        }
    }
}
