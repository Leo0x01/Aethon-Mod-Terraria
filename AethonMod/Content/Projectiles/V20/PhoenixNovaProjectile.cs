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
    /// PhoenixNovaProjectile — nova/llamarada solar centrada (llamarada cuando
    /// la invoca el SunProjectile; explosión estática con PhoenixNovaStaff).
    ///
    /// v5.95 — DETRÁS DEL SOL, DE VERDAD (petición del usuario: "sus efectos
    /// SupernovaStaff y PhoenixNovaStaff deben estar detrás de él… un extraño
    /// parpadeo que supongo es PhoenixNovaStaff el cual no está detrás del
    /// sol"). La v5.91 registraba la llamarada en drawCacheProjsBehindProjectiles
    /// vía DrawBehind, PERO nunca ponía hide=true — y decompilando tML
    /// (Main.DrawProjectiles) se comprobó que el bucle principal SOLO excluye a
    /// los proyectiles con hide: la llamarada se dibujaba DOS VECES por frame,
    /// una de ellas ENCIMA del sol con brillo aditivo duplicado → el "extraño
    /// parpadeo". Ahora: (1) la llamarada invocada por el sol (ai[2]=1) va con
    /// hide=true — ni tML ni ningún mod (Luminance incluida) la dibuja — y
    /// (2) EL SOL la dibuja él mismo ANTES de sus propias capas
    /// (SunProjectile.DrawStarVisuals → DrawFlareSprites): detrás del disco
    /// SIEMPRE, en cualquier entorno. El disco del SunShader (alpha≈1) la
    /// oculta en el centro y su brillo asoma por el limbo: una llamarada real
    /// ERUPCIONANDO POR DETRÁS de la estrella.
    ///
    /// v5.95 — TAMAÑO IGUAL AL DEL SOL (petición: "aumentar el tamaño de
    /// PhoenixNovaStaff y que iguale el tamaño del sol"): los sprites se
    /// dimensionan con starR, el RADIO VISUAL REAL de la estrella que la
    /// invocó (crece con la gigante roja): el núcleo caliente mide lo mismo
    /// que el disco solar y el halo lo envuelve como backlight. El FLASH del
    /// pico pasó de "pantalla blanca completa" (el parpadeo) a un RIM de luz
    /// suave alrededor del limbo (alpha ≤ 120 tras el sol).
    ///
    /// Visuales (60 frames total):
    ///   - Halo SoftGlow aditivo pulsante SUAVE (0.93±0.07 — el pulso fuerte
    ///     de la v5.91 contribuía al parpadeo) que se extiende con la edad.
    ///   - Núcleo caliente del tamaño de la estrella (oculto tras el disco).
    ///   - Frames 25-35: rim de luz (backlight del pico de la llamarada).
    ///   - Spawn continuo de DustID.Torch en todas las direcciones.
    ///
    /// Físicas:
    ///   - Velocidad cero (estático). penetrate = -1. timeLeft = 60 (1 s).
    ///   - tileCollide = false. Aplica OnFire a los NPCs golpeados.
    /// </summary>
    public class PhoenixNovaProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
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
                // v5.95 — HIJA DEL SOL: OCULTA. Nadie la dibuja salvo el propio
                // sol (la pinta DETRÁS de su disco en DrawStarVisuals). hide
                // solo afecta al RENDER: la llamarada sigue dañando igual.
                if (Projectile.ai[2] == 1f)
                    Projectile.hide = true;

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

        // ================================================================
        //  RENDER
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                // v5.95 — HIJA DEL SOL: el SOL la dibuja detrás de su disco
                // (DrawStarVisuals); con hide=true tML jamás la pinta. Este
                // PreDraw ni siquiera corre para ella (los proyectiles ocultos
                // sin DrawBehind no pasan por el bucle de dibujado).
                if (Projectile.ai[2] == 1f)
                    return false;

                // === STANDALONE (PhoenixNovaStaff): se dibuja a sí misma ===
                // Tamaño equivalente a una estrella tipo sol (petición del
                // usuario: "que iguale el tamaño del sol" — aquí no hay sol que
                // la oculte, así que conserva su destello de pico completo).
                Vector2 drawPos = Projectile.Center - Main.screenPosition;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                DrawFlareSprites(Projectile, drawPos, 56f, 0f, true);

                Main.spriteBatch.End();
            }
            catch
            {
                // Cierre defensivo solo si una excepción cortó un Begin a
                // medias (el path normal deja las capas balanceadas).
                try { Main.spriteBatch.End(); } catch { }
            }

            // v5.95 — Restauración EXACTA del estado que tML espera tras
            // PreDraw (FUERA del catch: el batch siempre vuelve a quedar
            // ABIERTO, incluso en el path de error).
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        // ================================================================
        //  SPRITES DE LA LLAMARADA (v5.95)
        // ================================================================

        /// <summary>
        /// v5.95 — Sprites de la llamarada: los dibuja EL SOL detrás de su
        /// propio disco (DrawStarVisuals, capa 0) o el propio proyectil cuando
        /// va suelto con PhoenixNovaStaff (PreDraw standalone). Requiere un
        /// batch ADITIVO ya abierto — lo gestiona el llamador.
        ///
        /// El TAMAÑO IGUALA AL DEL SOL (petición del usuario): starR es el
        /// radio visual REAL de la estrella invocadora — el núcleo caliente
        /// mide lo mismo que el disco (queda oculto tras él y asoma por el
        /// limbo) y el halo lo envuelve como backlight. redGiant tiñe la
        /// llamarada de ROJO cuando la estrella se hincha (paleta unificada).
        /// </summary>
        internal static void DrawFlareSprites(Projectile p, Vector2 drawPos, float starR,
            float redGiant, bool standalone)
        {
            Texture2D softGlow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            Texture2D glowCircleWhite = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/GlowCircleWhite").Value;
            if (softGlow == null || glowCircleWhite == null) return;

            float age = p.ai[0];
            float progress = MathHelper.Clamp(age / 60f, 0f, 1f);

            // Pulso SUAVE (v5.95: el pulso fuerte + el flash de pantalla de la
            // v5.91 eran el "extraño parpadeo" que el usuario veía SOBRE el sol).
            float pulse = 0.93f + 0.07f * (float)Math.Sin(age * 0.25f);
            float expand = 1f + progress * 0.9f; // la llamarada se extiende

            // Paleta cálida: naranja solar → ROJO de la gigante.
            Color halo = Color.Lerp(new Color(255, 150, 55, 195), new Color(255, 65, 25, 195), redGiant);
            Color core = Color.Lerp(new Color(255, 235, 170, 225), new Color(255, 135, 90, 225), redGiant);

            Vector2 glowOrigin = softGlow.Size() * 0.5f;

            // === HALO (backlight): envuelve a la estrella (≈2.6× su radio) ===
            float haloScale = starR * 2.6f * expand / (softGlow.Width * 0.5f);
            Main.spriteBatch.Draw(softGlow, drawPos, null, halo, 0f, glowOrigin,
                haloScale * pulse, SpriteEffects.None, 0f);

            // === NÚCLEO CALIENTE: IGUALA el tamaño del sol (oculto tras el
            // disco; asoma por el limbo como corona en erupción) ===
            float coreScale = starR * 1.05f * (1f + progress * 0.35f) / (softGlow.Width * 0.5f);
            Main.spriteBatch.Draw(softGlow, drawPos, null, core, 0f, glowOrigin,
                coreScale * pulse, SpriteEffects.None, 0f);

            // === DESTELLO DEL PICO (frames 25-35) ===
            // v5.95: ya NO es la "pantalla blanca" de la v5.91 (parpadeo):
            // detrás del sol se lee como un RIM de luz que ABRAZA el limbo de
            // la estrella (alpha 120); standalone (sin sol que la oculte)
            // conserva el destello pleno (alpha 235).
            if (age >= 25f && age <= 35f)
            {
                float flashStrength = 1f - Math.Abs(age - 30f) / 5f;
                flashStrength = MathHelper.Clamp(flashStrength, 0f, 1f);
                Vector2 whiteOrigin = glowCircleWhite.Size() * 0.5f;
                float rimScale = starR * 1.45f * (1f + 0.08f * flashStrength) /
                                 (glowCircleWhite.Width * 0.5f);
                byte flashAlpha = (byte)(flashStrength * (standalone ? 235f : 120f));
                Main.spriteBatch.Draw(glowCircleWhite, drawPos, null,
                    new Color(255, 240, 210, flashAlpha),
                    0f, whiteOrigin, rimScale, SpriteEffects.None, 0f);
            }
        }
    }
}
