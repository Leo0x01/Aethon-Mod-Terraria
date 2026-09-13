using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// NebulaLightning — EL LÁTIGO ELÉCTRICO DE LA MEDUSA (v6.00).
    ///
    /// Petición del usuario: "en cuanto a la medusa el rayo debe salir de
    /// medusa no del cielo, y debe tener mas brillo". ADIÓS columna vertical
    /// del cielo: el rayo SALE DE LA MEDUSA — un LÁTIGO de plasma frío que
    /// se desenrosca de la campana y CRUZA el espacio hasta la víctima,
    /// zigzagueando perpendicular a su marcha.
    ///
    /// EL RAYO (más brillante que nunca):
    ///   - TRES capas aditivas (halo aqua ANCHO + funda azul-blanco + NÚCLEO
    ///     blanco puro a 255) — y luz real proyectada cada paso (1.4/1.7/1.9)
    ///   - ZIGZAG dentado regenerado cada pocos ticks (VIVE) con ramas cortas
    ///     laterales + frente de 4 puntas + destello de DESCARGA en la
    ///     medusa (el origen) — la campana chispea al soltarlo
    ///   - Al clavarse: TRUENO + estallido de chispas de hielo, y el trazo
    ///     LIGERA un instante fundiéndose mientras chisporrotea
    ///
    /// Determinista: el zigzag se deriva de (semilla, tick, segmento) con
    /// hash puro → todas las máquinas ven el MISMO rayo sin sincronizar nada.
    /// Campos: ai[0] = distancia restante al objetivo · ai[1] = semilla del
    /// zigzag · localAI[0]/[1] = origen (la medusa) · localAI[2] = llegó.
    /// </summary>
    public class NebulaLightning : ModProjectile
    {
        private const float BoltSpeed = 14f;      // px por update
        private const int LingerUpdates = 16;     // chisporroteo tras clavarse

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
            Projectile.timeLeft = 48;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.light = 1.6f;              // v6.00 — MÁS BRILLO
            Projectile.extraUpdates = 1;         // 14 × 2 = 28 px/t: un látigo
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            // el origen (LA MEDUSA) se captura al nacer — de ahí sale todo
            if (Projectile.localAI[0] == 0f && Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[0] = Projectile.Center.X;
                Projectile.localAI[1] = Projectile.Center.Y;
            }

            bool arrived = Projectile.localAI[2] > 0f;

            if (!arrived)
            {
                // vuela hacia el objetivo consumiendo la distancia restante
                Projectile.ai[0] -= BoltSpeed;

                // chispas a lo largo del trazo (la estela eléctrica)
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center +
                        new Vector2(Main.rand.NextFloat(-8f, 8f), Main.rand.NextFloat(-8f, 8f)),
                        DustID.BlueTorch,
                        new Vector2(Main.rand.NextFloat(-0.7f, 0.7f), Main.rand.NextFloat(-0.7f, 0.7f)),
                        220, Main.rand.NextBool(3)
                            ? new Color(160, 250, 255)
                            : new Color(255, 190, 240), 0.6f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // llegada al objetivo → IMPACTO (y el rayo se queda chisporroteando)
                if (Projectile.ai[0] <= 0f)
                {
                    Projectile.localAI[2] = 1f;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.friendly = false;       // ya no daña: solo brilla
                    Projectile.timeLeft = LingerUpdates;
                    ImpactFX();
                }
            }
            else
            {
                // clavado: chisporrotea en el sitio mientras se funde
                Projectile.velocity = Vector2.Zero;
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center +
                        new Vector2(Main.rand.NextFloat(-14f, 14f), Main.rand.NextFloat(-14f, 14f)),
                        DustID.BlueTorch,
                        new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-1.6f, 0.4f)),
                        230, Main.rand.NextBool(2)
                            ? new Color(180, 250, 255)
                            : new Color(255, 200, 245), 0.65f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // v6.00 — MÁS BRILLO: luz real BLANCO-CIAN potente en cada paso
            Lighting.AddLight(Projectile.Center, new Vector3(1.30f, 1.55f, 1.75f));
        }

        /// <summary>El CLAVADO: trueno + estallido de hielo en el objetivo.</summary>
        private void ImpactFX()
        {
            if (Main.netMode == NetmodeID.Server) return;

            try
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item12,
                    Projectile.Center);
            }
            catch { }

            for (int i = 0; i < 18; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float spd = Main.rand.NextFloat(1.5f, 5.0f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * spd,
                    230, Main.rand.NextBool(2)
                        ? new Color(170, 250, 255)
                        : new Color(255, 190, 240), 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
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
                    220, new Color(180, 250, 255), 0.65f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        // ================================================================
        //  RENDER — el látigo dentado DE LA MEDUSA AL OBJETIVO
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
                Vector2 origin = new Vector2(Projectile.localAI[0], Projectile.localAI[1]) -
                                 Main.screenPosition;
                if (Projectile.localAI[0] == 0f && Projectile.localAI[1] == 0f) return false;

                Vector2 delta = head - origin;
                float len = delta.Length();
                if (len < 8f) len = 8f;
                Vector2 dir = delta / len;
                Vector2 perp = new Vector2(-dir.Y, dir.X);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // vida del rayo: pleno en vuelo, fundido tras clavarse
                float lifeT = Projectile.localAI[2] > 0f
                    ? 1f - Projectile.timeLeft / (float)LingerUpdates
                    : 0f;
                float fade = Projectile.localAI[2] > 0f
                    ? 1f - lifeT * lifeT * 0.85f
                    : 1f;
                // micro-parpadeo vivo (el látigo chisporrotea SIEMPRE)
                float flicker = 0.86f + 0.14f *
                    (float)Math.Sin(Main.GlobalTimeWrappedHourly * 61f);

                // === el ZIGZAG: nace del hash (semilla, flick, segmento) ===
                int flick = (int)(Main.GlobalTimeWrappedHourly * 20f); // ~3 ticks
                int seed = (int)Projectile.ai[1];
                int segs = Math.Max(4, (int)(len / 26f) + 1);

                for (int s = 0; s < segs; s++)
                {
                    float t0 = s / (float)segs;
                    float t1 = (s + 1) / (float)segs;
                    // el zigzag se AMPLIA en el medio (un rayo real tiene la
                    // parte media más ramificada) y afina al llegar a la punta
                    float amp = 16f * (float)Math.Sin(t0 * Math.PI);
                    float j0 = (Hash01(seed, flick, s) - 0.5f) * 2f * amp;
                    float j1 = (Hash01(seed, flick, s + 1) - 0.5f) * 2f *
                               16f * (float)Math.Sin(t1 * Math.PI);
                    Vector2 p0 = origin + dir * (t0 * len) + perp * j0;
                    Vector2 p1 = origin + dir * (t1 * len) + perp * j1;

                    // v6.00 — TRES capas: halo ANCHO + funda + NÚCLEO 255
                    float f = fade * flicker;
                    DrawSegment(glow, p0, p1, 11f * f,
                        new Color(70, 210, 255, (byte)(150 * f)));
                    DrawSegment(glow, p0, p1, 4.6f * f,
                        new Color(170, 240, 255, (byte)(225 * f)));
                    DrawSegment(glow, p0, p1, 1.9f * f,
                        new Color(255, 255, 255, (byte)(255 * f)));

                    // RAMA lateral corta (una de cada dos segmentos, aleatoria)
                    if (s > 0 && s < segs - 1 && Hash01(seed, flick, s + 91) > 0.60f)
                    {
                        float side = Hash01(seed, flick, s + 37) > 0.5f ? 1f : -1f;
                        Vector2 bEnd = p0 + (perp * side - dir * 0.45f) *
                            (20f + 14f * Hash01(seed, flick, s + 53));
                        DrawSegment(glow, p0, bEnd, 4.5f * f,
                            new Color(120, 230, 255, (byte)(140 * f)));
                        DrawSegment(glow, p0, bEnd, 1.8f * f,
                            new Color(240, 252, 255, (byte)(200 * f)));
                    }
                }

                // === la PUNTA: el frente del látigo brilla mientras vuela ===
                Main.spriteBatch.Draw(glow, head, null,
                    new Color(210, 250, 255, (byte)(235 * fade)), 0f,
                    glow.Size() * 0.5f, (40f * fade) / (glow.Width * 0.5f),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(star, head, null,
                    new Color(255, 255, 255, 255), 0f,
                    star.Size() * 0.5f, (15f * fade) / (star.Width * 0.5f),
                    SpriteEffects.None, 0f);

                // === v6.00 — el ORIGEN: la DESCARGA en la propia medusa ===
                Main.spriteBatch.Draw(glow, origin, null,
                    new Color(190, 245, 255, (byte)(200 * fade * flicker)), 0f,
                    glow.Size() * 0.5f, (30f * fade) / (glow.Width * 0.5f),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(star, origin, null,
                    new Color(235, 252, 255, (byte)(235 * fade)), 0f,
                    star.Size() * 0.5f, (10f * fade) / (star.Width * 0.5f),
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
