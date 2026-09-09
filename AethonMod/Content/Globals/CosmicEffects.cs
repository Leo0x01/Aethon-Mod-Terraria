using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// CosmicEffects — Helper estático con todos los efectos visuales cósmicos
    /// reutilizables por los bastones de prueba.
    ///
    /// Efectos disponibles:
    /// - SpawnCosmicTrail: estela dorada/cian/magenta/índigo (la del Grimorio)
    /// - SpawnMagicRing: anillo expansivo de partículas
    /// - SpawnMagicRingMulti: múltiples anillos de colores
    /// - SpawnSparkles: destellos ambientales aleatorios
    /// - SpawnLightBeams: rayos de luz radiando hacia afuera
    /// - SpawnStarfall: estrellas cayendo del cielo (efecto Star Wrath)
    /// - SpawnImpactSphere: esfera de impacto aditiva blanca/cian
    /// - SpawnSupernova: explosión cósmica completa (4 colores + blanco)
    /// - SpawnStarWrathEffect: el efecto COMPLETO de la imagen (todos los anteriores combinados)
    /// </summary>
    public static class CosmicEffects
    {
        // ================================================================
        //  PALETA CÓSMICA
        // ================================================================
        public static readonly Color Gold      = new Color(255, 217, 61);
        public static readonly Color Cyan      = new Color(0, 255, 255);
        public static readonly Color Magenta   = new Color(255, 0, 102);
        public static readonly Color Indigo    = new Color(75, 0, 130);
        public static readonly Color PureWhite = new Color(255, 255, 255);
        public static readonly Color DeepBlue  = new Color(0, 50, 200);
        public static readonly Color RoyalBlue = new Color(0, 0, 128);

        // ================================================================
        //  ESTELA CÓSMICA (la del Grimorio)
        // ================================================================
        public static void SpawnCosmicTrail(Vector2 center, Vector2 velocity, float scale = 1f)
        {
            // Dorado — cada frame
            Dust d1 = Dust.NewDustPerfect(center, DustID.GoldFlame,
                -velocity * 0.05f + new Vector2(
                    Main.rand.NextFloat(-1f, 1f),
                    Main.rand.NextFloat(-1f, 1f)) * scale,
                150, Gold, 0.7f * scale);
            d1.noGravity = true;
            d1.fadeIn = 0f;

            // Cian — cada 2 frames
            if (Main.rand.NextBool(2))
            {
                Dust d2 = Dust.NewDustPerfect(center, DustID.BlueTorch,
                    -velocity * 0.1f + new Vector2(
                        Main.rand.NextFloat(-1.5f, 1.5f),
                        Main.rand.NextFloat(-1.5f, 1.5f)) * scale,
                    180, Cyan, 0.6f * scale);
                d2.noGravity = true;
                d2.fadeIn = 0f;
            }

            // Magenta — cada 3 frames
            if (Main.rand.NextBool(3))
            {
                Dust d3 = Dust.NewDustPerfect(center, DustID.RainbowTorch,
                    -velocity * 0.08f + new Vector2(
                        Main.rand.NextFloat(-1.5f, 1.5f),
                        Main.rand.NextFloat(-1.5f, 1.5f)) * scale,
                    200, Magenta, 0.5f * scale);
                d3.noGravity = true;
                d3.fadeIn = 0f;
            }

            // Índigo — cada 4 frames
            if (Main.rand.NextBool(4))
            {
                Dust d4 = Dust.NewDustPerfect(center, DustID.PurpleTorch,
                    new Vector2(
                        Main.rand.NextFloat(-1f, 1f),
                        Main.rand.NextFloat(-1f, 1f)) * scale,
                    120, Indigo, 0.5f * scale);
                d4.noGravity = true;
                d4.fadeIn = 0f;
            }
        }

        // ================================================================
        //  ANILLO MÁGICO EXPANSIVO
        // ================================================================
        public static void SpawnMagicRing(Vector2 center, Color color, int particleCount = 24, float radius = 60f, float speed = 3f)
        {
            for (int i = 0; i < particleCount; i++)
            {
                float angle = (MathHelper.TwoPi / particleCount) * i;
                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                // v5.60: usar radius para spawn dusts en center + dir*radius (anillo visual real)
                Vector2 spawnPos = center + dir * radius;
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                    dir * speed,
                    180, color, 0.9f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            // Luz central
            Lighting.AddLight(center, new Vector3(color.R / 255f, color.G / 255f, color.B / 255f));
        }

        /// <summary>
        /// Múltiples anillos de colores cósmicos (mejorado).
        /// </summary>
        public static void SpawnMagicRingMulti(Vector2 center, float speed = 3f)
        {
            SpawnMagicRing(center, Gold, 24, 60f, speed);
            SpawnMagicRing(center, Cyan, 20, 50f, speed * 0.85f);
            SpawnMagicRing(center, Magenta, 16, 40f, speed * 0.7f);
            SpawnMagicRing(center, Indigo, 12, 30f, speed * 0.55f);
        }

        // ================================================================
        //  DESTELLOS AMBIENTALES (Sparkles)
        // ================================================================
        public static void SpawnSparkles(Vector2 center, int count, float spread = 30f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = new Vector2(
                    Main.rand.NextFloat(-spread, spread),
                    Main.rand.NextFloat(-spread, spread));
                // Alternar colores
                Color c;
                switch (i % 4)
                {
                    case 0: c = PureWhite; break;
                    case 1: c = Cyan; break;
                    case 2: c = Gold; break;
                    default: c = Magenta; break;
                }
                Dust d = Dust.NewDustPerfect(center + offset, DustID.Enchanted_Gold,
                    new Vector2(
                        Main.rand.NextFloat(-0.5f, 0.5f),
                        Main.rand.NextFloat(-1.5f, -0.2f)), // flotan hacia arriba
                    200, c, 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        // ================================================================
        //  RAYOS DE LUZ RADIANTES (Light Beams)
        // ================================================================
        public static void SpawnLightBeams(Vector2 center, int count = 6, float length = 80f)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (MathHelper.TwoPi / count) * i + Main.rand.NextFloat(-0.2f, 0.2f);
                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                // Línea de dust desde el centro hacia afuera
                for (int j = 0; j < 8; j++)
                {
                    Vector2 pos = center + dir * (length * j / 8f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.BlueTorch,
                        dir * 0.5f,
                        100 - j * 10, // alpha bajo = muy transparente
                        new Color(200, 230, 255, 80), 0.4f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
        }

        // ================================================================
        //  ESTRELLAS CAYENDO DEL CIELO (Starfall — efecto Star Wrath)
        // ================================================================
        public static void SpawnStarfall(Vector2 target, int count = 4, float spread = 80f)
        {
            for (int i = 0; i < count; i++)
            {
                // Spawn arriba del target, con algo de spread horizontal
                Vector2 spawnPos = new Vector2(
                    target.X + Main.rand.NextFloat(-spread, spread),
                    target.Y - Main.rand.NextFloat(300, 500)); // 300-500px arriba

                // Velocidad hacia abajo (hacia el target)
                Vector2 velocity = new Vector2(
                    (target.X - spawnPos.X) * 0.02f,
                    8f); // caída rápida

                // Dust en forma de estrella (4 puntos) usando GoldFlame
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                    velocity,
                    200, PureWhite, 1.2f);
                d.noGravity = false; // cae con gravedad
                d.fadeIn = 0f;

                // Estela cian detrás de la estrella
                Dust trail = Dust.NewDustPerfect(spawnPos, DustID.BlueTorch,
                    -velocity * 0.1f,
                    150, Cyan, 0.8f);
                trail.noGravity = true;
                trail.fadeIn = 0f;
            }
        }

        // ================================================================
        //  ESFERA DE IMPACTO ADITIVA (central burst blanco/cian)
        // ================================================================
        public static void SpawnImpactSphere(Vector2 center, float intensity = 1f)
        {
            // Núcleo blanco brillante
            for (int i = 0; i < 12; i++)
            {
                Vector2 dir = new Vector2(
                    Main.rand.NextFloat(-1f, 1f),
                    Main.rand.NextFloat(-1f, 1f));
                if (dir.Length() > 0.1f) dir.Normalize();
                Dust d = Dust.NewDustPerfect(center, DustID.Silver,
                    dir * (2f * intensity),
                    255, PureWhite, 1.2f * intensity);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Anillo cian interno
            for (int i = 0; i < 16; i++)
            {
                float angle = (MathHelper.TwoPi / 16) * i;
                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Dust d = Dust.NewDustPerfect(center, DustID.BlueTorch,
                    dir * (3f * intensity),
                    200, Cyan, 1.0f * intensity);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Halo azul real externo
            for (int i = 0; i < 20; i++)
            {
                float angle = (MathHelper.TwoPi / 20) * i;
                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Dust d = Dust.NewDustPerfect(center, DustID.PurpleTorch,
                    dir * (4f * intensity),
                    150, DeepBlue, 0.8f * intensity);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Luz intensa (v5.60: clamp a <=1.0 — valores >1 se truncan a blanco plano)
            float lr = Math.Min(1f, 1.5f * intensity);
            float lg = Math.Min(1f, 1.8f * intensity);
            float lb = Math.Min(1f, 2.5f * intensity);
            Lighting.AddLight(center, new Vector3(lr, lg, lb));
        }

        // ================================================================
        //  SUPERNOVA (explosión cósmica completa)
        // ================================================================
        public static void SpawnSupernova(Vector2 center, float scale = 1f)
        {
            // Núcleo dorado
            for (int i = 0; i < 12; i++)
            {
                Dust d = Dust.NewDustPerfect(center, DustID.GoldFlame,
                    new Vector2(
                        Main.rand.NextFloat(-5f, 5f),
                        Main.rand.NextFloat(-5f, 5f)) * scale,
                    180, Gold, 0.9f * scale);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Destellos cian
            for (int i = 0; i < 10; i++)
            {
                Dust d = Dust.NewDustPerfect(center, DustID.BlueTorch,
                    new Vector2(
                        Main.rand.NextFloat(-4f, 4f),
                        Main.rand.NextFloat(-4f, 4f)) * scale,
                    200, Cyan, 0.8f * scale);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Fragmentos magenta
            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustPerfect(center, DustID.RainbowTorch,
                    new Vector2(
                        Main.rand.NextFloat(-4f, 4f),
                        Main.rand.NextFloat(-4f, 4f)) * scale,
                    220, Magenta, 0.6f * scale);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Halo índigo
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(center, DustID.PurpleTorch,
                    new Vector2(
                        Main.rand.NextFloat(-3f, 3f),
                        Main.rand.NextFloat(-3f, 3f)) * scale,
                    150, Indigo, 0.6f * scale);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Supernova blanca central
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustPerfect(center, DustID.Enchanted_Gold,
                    new Vector2(
                        Main.rand.NextFloat(-2f, 2f),
                        Main.rand.NextFloat(-2f, 2f)),
                    255, PureWhite, 0.6f); // v5.60: default → PureWhite (default tiene alpha 0 = invisible)
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        // ================================================================
        //  EFECTO COMPLETO STAR WRATH (recrea la imagen)
        //  Combina: esfera de impacto + starfall + sparkles + light beams
        // ================================================================
        public static void SpawnStarWrathEffect(Vector2 center)
        {
            // 1. Esfera de impacto central (aditiva blanco/cian/azul)
            SpawnImpactSphere(center, 1.5f);

            // 2. Estrellas cayendo del cielo (4-6 estrellas)
            SpawnStarfall(center, count: Main.rand.Next(4, 7), spread: 100f);

            // 3. Destellos ambientales (sparkles)
            SpawnSparkles(center, count: 15, spread: 50f);

            // 4. Rayos de luz radiantes
            SpawnLightBeams(center, count: 6, length: 100f);

            // 5. Anillo mágico dorado secundario
            SpawnMagicRing(center, Gold, 24, 60f, 4f);
        }

        // ================================================================
        //  ESTELA ARCOÍRIS (variante colorida)
        // ================================================================
        public static void SpawnRainbowTrail(Vector2 center, Vector2 velocity)
        {
            // Ciclar colores del arcoíris según el tiempo (sin usar HSL para evitar issues de API)
            Color[] rainbow = {
                new Color(255, 0, 0),      // Rojo
                new Color(255, 127, 0),    // Naranja
                new Color(255, 255, 0),    // Amarillo
                new Color(0, 255, 0),      // Verde
                new Color(0, 255, 255),    // Cian
                new Color(0, 127, 255),    // Azul
                new Color(139, 0, 255),    // Violeta
            };
            int idx = (int)(Main.time * 0.05) % rainbow.Length;
            Color c = rainbow[idx];

            Dust d = Dust.NewDustPerfect(center, DustID.RainbowTorch,
                -velocity * 0.1f + new Vector2(
                    Main.rand.NextFloat(-1f, 1f),
                    Main.rand.NextFloat(-1f, 1f)),
                200, c, 0.8f);
            d.noGravity = true;
            d.fadeIn = 0f;
        }
    }
}
