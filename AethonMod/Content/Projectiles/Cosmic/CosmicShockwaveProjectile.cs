using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Effects;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// CosmicShockwaveProjectile — onda expansiva con daño real por frente de onda.
    ///
    /// v5.88 — Fix del MissingResourceException: textura propia añadida
    /// (InvisiblePixel 1x1 — el dibujado es 100% manual vía DrawWaveVisual)
    /// y NewInstance con array de golpes fresco por onda.
    ///
    /// v5.86 — Los tres frentes de onda del arsenal cósmico:
    ///
    ///   ESTILO 0 — ONDA CROMÁTICA (explosión del agujero negro):
    ///     Anillo RGB (aberración cromática real: los canales R/G/B se separan
    ///     radialmente) que se expande desde el centro. Se registra como fuente
    ///     del BlackHoleLensSystem → el FONDO del juego se distorsiona a su paso
    ///     ("distorsiona un poco"). Daña a cada NPC cuando el frente lo alcanza.
    ///
    ///   ESTILO 1 — ONDA CROMÁTICA INVERSA (implosión del agujero negro):
    ///     Nace en el radio máximo y CONVERGE hacia el centro (el frente barre
    ///     el daño hacia dentro). El desfase de color está invertido (azul por
    ///     delante de rojo) y también distorsiona el fondo al pasar.
    ///
    ///   ESTILO 2 — ONDA DE FUEGO (nova final del sol):
    ///     Triple anillo ardiente (rojo/naranja/amarillo) + llamas a lo largo
    ///     del frente. Cada onda hace daño y aplica QUEMADURA (OnFire).
    ///
    /// Campos AI:
    ///   ai[0] = edad (negativa = retardo escalonado aún activo)
    ///   ai[1] = estilo (0 cromática / 1 cromática inversa / 2 fuego)
    ///   ai[2] = radio máximo en píxeles
    ///   duración = derivada del radio (maxR/20 ticks ≈ frente de ~40 px/tick)
    ///   (la API de NewProjectile solo acepta 3 slots de ai: la duración se
    ///   deriva de forma determinista para que todas las máquinas coincidan)
    ///
    /// RENDER: los estilos 0/1 se dibujan ENCIMA de la lente gravitacional
    /// (el BlackHoleLensSystem los pinta tras compositar la distorsión), de
    /// modo que la lente nunca deforma sus propios anillos. El estilo 2 se
    /// dibuja en el pase normal del mundo.
    /// </summary>
    public class CosmicShockwaveProjectile : ModProjectile
    {
        /// <summary>Estilo: onda cromática expansiva.</summary>
        public const float StyleChromatic = 0f;

        /// <summary>Estilo: onda cromática inversa (convergente).</summary>
        public const float StyleChromaticInverse = 1f;

        /// <summary>Estilo: onda de fuego (con quemadura).</summary>
        public const float StyleFire = 2f;

        /// <summary>
        /// Marca de NPCs ya golpeados por esta onda (una sola vez cada uno).
        /// v5.88 — UNA por proyectil: tML crea cada proyectil clonando el
        /// prototipo (MemberwiseClone), así que un array inicializado en el
        /// campo se COMPARTIRÍA entre todas las ondas simultáneas del mismo
        /// tipo (las 3 ondas inversas de la implosión se borrarían las marcas
        /// de daño unas a otras al nacer escalonadas). NewInstance le da a
        /// cada onda su propio array.
        /// </summary>
        private bool[] _hitNPCs = new bool[Main.maxNPCs];

        /// <summary>
        /// v5.88 — Cada proyectil nuevo recibe un array de marcas FRESCO.
        /// Sin esto, MemberwiseClone haría que todas las ondas activas del
        /// mismo tipo compartieran el MISMO array (bug de daño en cascada:
        /// cada onda nueva borraba las marcas de sus hermanas y estas podían
        /// golpear varias veces a los mismos NPC).
        /// </summary>
        public override ModProjectile NewInstance(Projectile entity)
        {
            CosmicShockwaveProjectile inst = (CosmicShockwaveProjectile)base.NewInstance(entity);
            inst._hitNPCs = new bool[Main.maxNPCs];
            return inst;
        }

        private float Age { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private float Style => Projectile.ai[1];
        private float MaxRadius => Projectile.ai[2];

        /// <summary>Duración derivada del radio: frente de ~40 px/tick de pico.</summary>
        private float Duration => Math.Max(MaxRadius / 20f, 10f);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.tileCollide = false;
            // Daño manual por frente de onda (SimpleStrikeNPC): sin colisión vanilla.
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;

            // tML reutiliza instancias clonadas del prototipo: además del
            // array fresco de NewInstance, se limpia por si el clon se recicla.
            Array.Clear(_hitNPCs, 0, _hitNPCs.Length);
        }

        public override bool? CanCutTiles() => false;
        public override bool? CanDamage() => false;

        // ================================================================
        //  AI — frente de onda, daño y soporte visual
        // ================================================================
        public override void AI()
        {
            try
            {
                float age = Age;
                Age += 1f;

                // Retardo escalonado (ondas en secuencia): invisible e inofensiva.
                if (age < 0f)
                    return;

                float progress = MathHelper.Clamp(age / Math.Max(Duration, 1f), 0f, 1f);
                float front = FrontRadius(age, Style, MaxRadius);

                // === DAÑO POR FRENTE DE ONDA (solo autoridad) ===
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    ApplyWaveDamage(front);

                // === SOPORTE VISUAL (solo cliente) ===
                if (Main.netMode != NetmodeID.Server)
                {
                    if (Style == StyleFire)
                        SpawnFireFrontDusts(front, progress);
                    else
                        SpawnChromaticFrontSparks(front, progress);
                }

                // === LUZ ===
                float alpha = WaveAlpha(age, Math.Max(Duration, 1f));
                if (Style == StyleFire)
                    Lighting.AddLight(Projectile.Center,
                        new Vector3(1f, 0.55f, 0.2f) * 1.8f * alpha);
                else
                    Lighting.AddLight(Projectile.Center,
                        new Vector3(0.6f, 0.5f, 1f) * 0.9f * alpha);

                // Frente completado → la onda se disipa.
                if (age >= Duration)
                    Projectile.Kill();
            }
            catch { }
        }

        // ================================================================
        //  FRENTE DE ONDA (compartido con el sistema de lente)
        // ================================================================

        /// <summary>Radio del frente en píxeles, o -1 si aún retrasada/inactiva.</summary>
        public static float GetFrontRadius(Projectile p)
        {
            if (p == null || !p.active) return -1f;
            float age = p.ai[0];
            if (age < 0f) return -1f;
            return FrontRadius(age, p.ai[1], p.ai[2]);
        }

        /// <summary>Progreso 0..1 del frente (o -1 si retrasada).</summary>
        public static float GetProgress(Projectile p)
        {
            if (p == null || !p.active) return -1f;
            float age = p.ai[0];
            if (age < 0f) return -1f;
            return MathHelper.Clamp(age / DurationOf(p.ai[2]), 0f, 1f);
        }

        /// <summary>Duración del frente derivada del radio máximo (determinista).</summary>
        private static float DurationOf(float maxR)
        {
            return Math.Max(maxR / 20f, 10f);
        }

        private static float FrontRadius(float age, float style, float maxR)
        {
            float duration = DurationOf(maxR);
            float p = MathHelper.Clamp(age / duration, 0f, 1f);
            if (style == StyleChromaticInverse)
            {
                // Convergencia acelerada: nace en maxR y colapsa hacia el centro.
                return maxR * (1f - p * p);
            }
            // Expansión ease-out: arranque veloz, frenado al final.
            return maxR * (1f - (1f - p) * (1f - p));
        }

        /// <summary>Envolvente de alpha: aparición rápida + desvanecimiento final.</summary>
        private static float WaveAlpha(float age, float duration)
        {
            float fadeIn = Utils.GetLerpValue(0f, duration * 0.15f, age, true);
            float fadeOut = 1f - Utils.GetLerpValue(duration * 0.7f, duration, age, true);
            return Math.Min(fadeIn, fadeOut);
        }

        // ================================================================
        //  DAÑO — el frente barre a los NPC una única vez por onda
        // ================================================================
        private void ApplyWaveDamage(float front)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (_hitNPCs[i]) continue;
                NPC npc = Main.npc[i];
                if (npc == null || !npc.active || !npc.CanBeChasedBy()) continue;

                Vector2 toCenter = Projectile.Center - npc.Center;
                float dist = toCenter.Length();

                bool crossed;
                if (Style == StyleChromaticInverse)
                {
                    // Onda convergente: golpea cuando el frente pasa hacia dentro
                    // (y solo a quien estaba dentro del radio inicial).
                    crossed = dist <= MaxRadius * 1.02f && dist >= front;
                }
                else
                {
                    // Onda expansiva: golpea cuando el frente le alcanza.
                    crossed = dist <= front;
                }

                if (!crossed) continue;
                _hitNPCs[i] = true;

                // Dirección del empuje: hacia fuera en expansivas, hacia el
                // centro en la inversa (la implosión arrastra hacia dentro).
                int dir;
                float knockBack;
                if (Style == StyleChromaticInverse)
                {
                    dir = npc.Center.X < Projectile.Center.X ? 1 : -1;
                    knockBack = -4f;
                }
                else
                {
                    dir = npc.Center.X < Projectile.Center.X ? -1 : 1;
                    knockBack = Style == StyleFire ? 5f : 6f;
                }

                npc.SimpleStrikeNPC(Projectile.damage, dir, false, knockBack, DamageClass.Magic);

                // La onda de fuego aplica QUEMADURA.
                if (Style == StyleFire)
                    npc.AddBuff(BuffID.OnFire, 300);
            }
        }

        // ================================================================
        //  DUSTS DE APOYO
        // ================================================================

        /// <summary>LLamas vivas a lo largo del frente de la onda de fuego.</summary>
        private void SpawnFireFrontDusts(float front, float progress)
        {
            int count = 4;
            for (int i = 0; i < count; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * front,
                    (float)Math.Sin(angle) * front);
                Vector2 vel = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(1.5f, 3.5f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(1.5f, 3.5f));
                Color color = Main.rand.NextBool(2)
                    ? new Color(255, 170, 60)
                    : new Color(255, 100, 30);
                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, vel, 220, color, 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Chispas blancas escasas sobre el frente cromático.</summary>
        private void SpawnChromaticFrontSparks(float front, float progress)
        {
            if (!Main.rand.NextBool(2)) return;
            float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            Vector2 pos = Projectile.Center + new Vector2(
                (float)Math.Cos(angle) * front,
                (float)Math.Sin(angle) * front);
            Dust d = Dust.NewDustPerfect(pos, DustID.Enchanted_Gold,
                Vector2.Zero, 200, new Color(230, 220, 255), 0.6f);
            d.noGravity = true;
            d.fadeIn = 0.2f;
        }

        // ================================================================
        //  RENDER
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                if (Age < 0f) return false;

                // Las ondas cromáticas las pinta el sistema de lente ENCIMA de la
                // distorsión (para que la lente no las deforme a ellas). Si la
                // lente no está activa, caemos al dibujado normal del mundo.
                if ((Style == StyleChromatic || Style == StyleChromaticInverse) &&
                    BlackHoleLensSystem.LensActive)
                    return false;

                // Pase del mundo: el spriteBatch del juego está abierto → cerrarlo
                // antes de nuestros pases (el sistema de lente lo llama en batch
                // ya cerrado, por eso el parámetro).
                DrawWaveVisual(Projectile, true);
            }
            catch { }

            // Restaurar el SpriteBatch al estado que tML espera tras PreDraw.
            // (End defensivo: si un error interno dejó un Begin abierto, se cierra
            // antes de restaurar; si no había nada abierto, se ignora.)
            try { Main.spriteBatch.End(); } catch { }
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        /// <summary>
        /// Dibuja la onda completa (anillos con aberración cromática o triple
        /// anillo de fuego). Reutilizable desde el pase del mundo (PreDraw) y
        /// desde el pase posterior a la lente (BlackHoleLensSystem).
        /// <param name="endActiveBatch">true cuando existe un Begin del juego
        /// activo (pase del mundo); false en el hook de la lente (batch cerrado).</param>
        /// </summary>
        public static void DrawWaveVisual(Projectile p, bool endActiveBatch)
        {
            try
            {
                if (p == null || !p.active || p.ai[0] < 0f) return;

                float age = p.ai[0];
                float style = p.ai[1];
                float duration = DurationOf(p.ai[2]);
                float front = FrontRadius(age, style, p.ai[2]);
                if (front <= 1f) return;

                float progress = MathHelper.Clamp(age / duration, 0f, 1f);
                float alpha = WaveAlpha(age, duration);

                Texture2D ring = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/Ring").Value;
                Vector2 drawPos = p.Center - Main.screenPosition;
                float ringUnit = ring.Width / 2f; // radio del anillo a escala 1

                if (endActiveBatch)
                    Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                if (style == StyleFire)
                {
                    // === TRIPLE ANILLO DE FUEGO ===
                    DrawRing(ring, drawPos, front, ringUnit,
                        new Color(220, 50, 10, (byte)(alpha * 200f)));
                    DrawRing(ring, drawPos, front * 0.93f, ringUnit,
                        new Color(255, 130, 30, (byte)(alpha * 220f)));
                    DrawRing(ring, drawPos, front * 0.86f, ringUnit,
                        new Color(255, 230, 130, (byte)(alpha * 230f)));
                    DrawRing(ring, drawPos, front * 0.8f, ringUnit,
                        new Color(255, 255, 220, (byte)(alpha * 120f)));
                }
                else
                {
                    // === ANILLO CROMÁTICO (aberración RGB real) ===
                    // La separación de canales crece con la edad (dispersión)
                    // y se INVERTIEn en la onda inversa (azul por delante).
                    float fringe = (2.5f + 4.5f * progress) *
                                   (style == StyleChromaticInverse ? -1f : 1f);
                    byte a = (byte)(alpha * 230f);

                    DrawRing(ring, drawPos, front + fringe, ringUnit, new Color(255, 40, 40, a));
                    DrawRing(ring, drawPos, front, ringUnit, new Color(60, 255, 90, a));
                    DrawRing(ring, drawPos, front - fringe, ringUnit, new Color(70, 130, 255, a));
                    // Núcleo blanco que unifica los tres canales.
                    DrawRing(ring, drawPos, front, ringUnit,
                        new Color(255, 255, 255, (byte)(alpha * 150f)));
                }

                Main.spriteBatch.End();
            }
            catch { }
        }

        /// <summary>Dibuja un anillo centrado en drawPos con el radio dado en píxeles.</summary>
        private static void DrawRing(Texture2D ring, Vector2 drawPos, float radiusPx,
            float ringUnit, Color color)
        {
            if (radiusPx <= 0.5f || color.A == 0) return;
            float scale = radiusPx / ringUnit;
            Main.spriteBatch.Draw(ring, drawPos, null, color, 0f,
                new Vector2(ring.Width * 0.5f, ring.Height * 0.5f), scale,
                SpriteEffects.None, 0f);
        }
    }
}
