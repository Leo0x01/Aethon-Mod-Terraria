using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// PhoenixNovaProjectile — nova/explosión centrada en el jugador (llamarada
    /// solar cuando la invoca el SunProjectile).
    ///
    /// v5.91 — DETRÁS DEL SOL (DrawBehind): el Sol invoca esta nova cada 2 s como
    /// LLAMARADA SOLAR. En DrawProjectiles tML dibuja los proyectiles en orden
    /// ASCENDENTE de índice, así que la llamarada (invocada DESPUÉS del sol, índice
    /// mayor) quedaba pintada ENCIMA del cuerpo de la estrella. Ahora el hook
    /// DrawBehind la registra en drawCacheProjsBehindProjectiles: tML dibuja esa
    /// cache ANTES de DrawProjectiles() → la llamarada queda DETRÁS del sol (y
    /// por delante de los NPC): el disco de la estrella tapa el NÚCLEO de los
    /// anillos y estos se abren alrededor de la silueta — una llamarada real
    /// ERUPCIONANDO POR DETRÁS de la estrella (petición del usuario). El destello
    /// blanco queda como un backlight dramático alrededor del disco.
    ///
    /// v5.91 — FIX CRÍTICO DEL SPRITEBATCH (bug presente desde v5.88): este
    /// PreDraw abría/cerraba el batch SIN Main.GameViewMatrix.TransformationMatrix
    /// (y sin sampler/rasterizer) — sus anillos se dibujaban sin el transform del
    /// mundo (mal posicionados con zoom ≠ 1) y el Begin(Deferred) final dejaba el
    /// batch SIN TRANSFORMAR para TODOS los proyectiles vanilla posteriores del
    /// frame: dibujados en coordenadas de mundo sin la vista → "círculos que
    /// subían" flotando por la pantalla (el reporte del usuario en v5.89). Ahora
    /// todos los Begin llevan el transform del mundo y el estado exacto que tML
    /// espera (Deferred, AlphaBlend, DefaultSamplerState, CullCounterClockwise).
    ///
    /// Visuales (60 frames total):
    ///   - Se dibuja múltiple Ring.png a escalas crecientes con colores
    ///     naranja-rojo (de naranja brillante a rojo profundo).
    ///   - En frame 30: flash blanco con GlowCircleWhite.
    ///   - Spawn continuo de DustID.Torch en todas las direcciones.
    ///
    /// Físicas:
    ///   - Velocidad cero (estático).
    ///   - penetrate = -1 (atraviesa todo en el área).
    ///   - timeLeft = 60 (1 segundo).
    ///   - tileCollide = false.
    ///   - Aplica OnFire a los NPCs golpeados.
    /// </summary>
    public class PhoenixNovaProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        /// <summary>
        /// v5.91 — LA LLAMARADA VA DETRÁS DEL SOL: registrarse en la cache
        /// drawCacheProjsBehindProjectiles hace que tML dibuje este proyectil
        /// ANTES del pase principal de proyectiles (verificado decompilando
        /// Main.DrawCachedProjs: se llama justo antes de DrawProjectiles) →
        /// queda detrás del cuerpo del sol y de cualquier proyectil posterior.
        /// </summary>
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.light = 0f;
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
                float age = Projectile.ai[0];
                Projectile.ai[0] += 1f;

                // v5.78: spawn dust solo en cliente (no en server)
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        SpawnTorchDust(age);
                    }
                }

                // Iluminación cálida intensa (más intensa en el pico frame 30)
                float intensity = age < 30f ? (age / 30f) : (1f - (age - 30f) / 30f);
                intensity = MathHelper.Clamp(intensity, 0f, 1f);
                Lighting.AddLight(Projectile.Center,
                    new Vector3(1f * intensity, 0.55f * intensity, 0.15f * intensity));
            }
            catch { }
        }

        // ================================================================
        //  Spawn DustID.Torch en direcciones radiales
        // ================================================================
        private void SpawnTorchDust(float age)
        {
            float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            // Velocidad creciente con la edad (la nova se expande)
            float speed = 2f + age * 0.18f + Main.rand.NextFloat(-1f, 1f);
            Vector2 vel = new Vector2((float)Math.Cos(angle) * speed,
                                       (float)Math.Sin(angle) * speed);
            Color color = Main.rand.NextBool(2)
                ? new Color(255, 180, 60)
                : new Color(255, 100, 30);
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                vel, 180, color, 1.3f);
            d.noGravity = true;
            d.fadeIn = 0f;
        }

        // ================================================================
        //  OnHitNPC — aplica OnFire debuff
        // ================================================================
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                target.AddBuff(BuffID.OnFire, 300); // 5 segundos de OnFire
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                // v5.94 — EL ANILLO SE ELIMINÓ: los anillos son parte de la ONDA
                // EXPANSIVA final (petición del usuario: "solo deben salir al
                // final"). Además dibujaba Ring.png (1024px desde v5.93) con
                // escalas fijas de hasta 4.6× → anillos de 4710px en cada
                // llamarada. La llamarada queda como EXPLOSIÓN de brillo:
                // halo pulsante + núcleo + flash blanco al pico.
                Texture2D glowCircleWhite = ModContent.Request<Texture2D>("AethonMod/Content/Effects/GlowCircleWhite").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                if (glowCircleWhite == null || softGlow == null)
                    return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float age = Projectile.ai[0];
                float progress = age / 60f; // 0..1

                // v5.91 — Begin CON el transform del mundo (Main.GameViewMatrix):
                // antes iba SIN matrix → el dibujado quedaba en coords de
                // pantalla puras (mal con zoom ≠ 1) y el restore final dejaba el
                // batch corrupto para los proyectiles vanilla posteriores.
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === Halo central SoftGlow naranja (pulsa y CRECE con la edad) ===
                float pulse = 0.9f + (float)Math.Sin(age * 0.3f) * 0.15f;
                float expand = 1f + progress * 1.2f; // la llamarada se extiende
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 160, 60, 200),
                    0f, glowOrigin, 1.5f * pulse * expand, SpriteEffects.None, 0f);
                // Núcleo blanco-amarillo central
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 240, 180, 230),
                    0f, glowOrigin, 0.8f * pulse * (1f + progress * 0.5f), SpriteEffects.None, 0f);

                // === FLASH BLANCO en frame 30 (±5 frames) ===
                if (age >= 25f && age <= 35f)
                {
                    // Intensidad máxima en frame 30, decrece simétricamente
                    float flashStrength = 1f - Math.Abs(age - 30f) / 5f;
                    flashStrength = MathHelper.Clamp(flashStrength, 0f, 1f);
                    Vector2 whiteOrigin = new Vector2(glowCircleWhite.Width / 2f, glowCircleWhite.Height / 2f);
                    // GlowCircleWhite pulsante
                    float flashScale = 2.5f * flashStrength + 0.5f;
                    Main.spriteBatch.Draw(glowCircleWhite, drawPos, null,
                        new Color(255, 255, 255, (byte)(flashStrength * 255f)),
                        0f, whiteOrigin, flashScale, SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();
                // v5.91 — Restauración EXACTA del estado que tML espera tras
                // PreDraw (igual que el resto de proyectiles del mod): Deferred,
                // AlphaBlend, DefaultSamplerState, CullCounterClockwise y el
                // TRANSFORM DEL MUNDO. El Begin(Deferred, AlphaBlend) pelado de
                // v5.88 corrompía el dibujado de todo proyectil posterior.
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                    null, Main.GameViewMatrix.TransformationMatrix);
            }
            catch
            {
                // v5.90/v5.91 — cierre defensivo solo si una excepción cortó el Begin
                // (path de error exclusivamente: el path normal deja el batch
                // balanceado — sin excepciones first-chance por frame).
                try { Main.spriteBatch.End(); } catch { }
            }
            return false;
        }
    }
}
