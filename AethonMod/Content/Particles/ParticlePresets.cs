using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.Particles
{
    /// <summary>
    /// Efectos pre-construidos sobre la librería de partículas.
    /// </summary>
    public static class ParticlePresets
    {
        /// <summary>
        /// Explosión: ráfaga radial con interpolación de color núcleo→borde,
        /// anillo expansivo de shockwave y chispas con desaceleración.
        /// </summary>
        public static void Explosion(Vector2 center, float radius, int count,
            Color coreColor, Color edgeColor, int duration = 45)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;

            // Ráfaga radial — ColorShift núcleo → borde + FadeOut + ScaleDown
            var template = new ParticleData
            {
                Velocity = Vector2.Zero,
                Scale = Vector2.One * Main.rand.NextFloat(0.9f, 1.6f),
                PackedColor = ParticleManager.PackColor(coreColor),
                PackedStartColor = ParticleManager.PackColor(coreColor),
                PackedEndColor = ParticleManager.PackColor(edgeColor),
                TimeLeft = duration,
                Duration = duration,
                TextureId = ParticleTex.SoftGlow,
                BlendMode = 1, // Additive
                LayerPriority = LayerPriorities.BeforeProjectiles,
            };
            template.EnableComponent(ComponentFlag.FadeOut);
            template.EnableComponent(ComponentFlag.ScaleDown);
            template.EnableComponent(ComponentFlag.ColorShift);

            for (int i = 0; i < count; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(radius * 0.2f, radius);
                ParticleData p = template;
                p.Position = center + new Vector2(
                    (float)System.Math.Cos(angle) * dist,
                    (float)System.Math.Sin(angle) * dist);
                // Velocidad radial con desaceleración integrada (Gravity no; fricción via ScaleDown visual)
                p.Velocity = new Vector2(
                    (float)System.Math.Cos(angle) * Main.rand.NextFloat(0.5f, 2f),
                    (float)System.Math.Sin(angle) * Main.rand.NextFloat(0.5f, 2f));
                ParticleManager.Spawn(p);
            }

            // Anillo de shockwave — Ring con ScaleUp expansivo
            var ring = new ParticleData
            {
                Position = center,
                Velocity = Vector2.Zero,
                Scale = Vector2.One,
                PackedColor = ParticleManager.PackColor(edgeColor * 0.85f),
                PackedStartColor = ParticleManager.PackColor(edgeColor),
                PackedEndColor = ParticleManager.PackColor(Color.Transparent),
                TimeLeft = duration,
                Duration = duration,
                TextureId = ParticleTex.Ring,
                BlendMode = 1,
                LayerPriority = LayerPriorities.BeforeProjectiles,
            };
            // v5.94 — Ring.png pasó de 64px a 1024px (textura HD de v5.93):
            // la escala final se calculaba como radio/64 → anillos 16× más
            // grandes de lo pedido. Ahora con el radio REAL de la textura
            // (mitad de 1024 = 512): escala = radio/512 → el anillo visible
            // queda a ~0.92·radio (núcleo del Ring a 0.92 de su radio).
            ring.UserData1 = radius / 512f;  // escala final X (Ring HD = 1024px)
            ring.UserData2 = radius / 512f;  // escala final Y
            ring.EnableComponent(ComponentFlag.ScaleUp);
            ring.EnableComponent(ComponentFlag.FadeOut);
            ParticleManager.Spawn(ring);

            // Chispas — Star con rotación y vida corta
            var spark = new ParticleData
            {
                Velocity = Vector2.Zero,
                Scale = Vector2.One * Main.rand.NextFloat(0.5f, 1.1f),
                PackedColor = ParticleManager.PackColor(Color.White),
                PackedStartColor = ParticleManager.PackColor(coreColor),
                PackedEndColor = ParticleManager.PackColor(edgeColor),
                TimeLeft = duration * 2 / 3,
                Duration = duration * 2 / 3,
                TextureId = ParticleTex.Star,
                BlendMode = 1,
                RotationSpeed = Main.rand.NextFloat(-0.3f, 0.3f),
                LayerPriority = LayerPriorities.BeforeProjectiles,
            };
            spark.EnableComponent(ComponentFlag.FadeOut);
            ParticleManager.SpawnShape(center, ShapeDescriptor.Circle(radius * 1.2f), count / 3, spark);
        }

        /// <summary>
        /// Implosión: partículas convergen en espiral hacia el centro
        /// (colapso gravitatorio del agujero negro).
        /// </summary>
        public static void Implosion(Vector2 center, float radius, int count, Color color, int duration = 30)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;

            for (int i = 0; i < count; i++)
            {
                float angle = (MathHelper.TwoPi / count) * i + Main.rand.NextFloat(-0.15f, 0.15f);
                float dist = Main.rand.NextFloat(radius * 0.6f, radius);
                Vector2 spawnPos = center + new Vector2(
                    (float)System.Math.Cos(angle) * dist,
                    (float)System.Math.Sin(angle) * dist);

                // Velocidad: radial hacia el centro + componente tangencial → espiral
                Vector2 toCenter = center - spawnPos;
                if (toCenter.LengthSquared() < 0.01f) continue;
                toCenter.Normalize();
                Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X);

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = toCenter * (dist / duration) + tangent * (dist / duration) * 0.6f,
                    Scale = Vector2.One * Main.rand.NextFloat(0.7f, 1.4f),
                    PackedColor = ParticleManager.PackColor(color),
                    PackedStartColor = ParticleManager.PackColor(color),
                    PackedEndColor = ParticleManager.PackColor(Color.White),
                    TimeLeft = duration,
                    Duration = duration,
                    TextureId = ParticleTex.SoftGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.ColorShift);
                p.EnableComponent(ComponentFlag.ScaleDown);
                ParticleManager.Spawn(p);
            }
        }

        /// <summary>
        /// Pulso de anillo: onda circular que se expande y desvanece
        /// (aura, shockwave de impacto).
        /// </summary>
        public static void RingPulse(Vector2 center, float maxRadius, Color color, int duration = 36)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;

            var ring = new ParticleData
            {
                Position = center,
                Velocity = Vector2.Zero,
                Scale = Vector2.One,
                PackedColor = ParticleManager.PackColor(color),
                PackedStartColor = ParticleManager.PackColor(color),
                TimeLeft = duration,
                Duration = duration,
                TextureId = ParticleTex.Ring,
                BlendMode = 1,
                LayerPriority = LayerPriorities.BeforeProjectiles,
            };
            // v5.94 — fix del 16× (Ring.png 64→1024px): radio REAL de la textura.
            ring.UserData1 = maxRadius / 512f;
            ring.UserData2 = maxRadius / 512f;
            ring.EnableComponent(ComponentFlag.ScaleUp);
            ring.EnableComponent(ComponentFlag.FadeOut);
            ParticleManager.Spawn(ring);
        }

        /// <summary>
        /// Vórtice: partículas en brazos espirales alrededor del centro
        /// (portal, succión gravitatoria).
        /// </summary>
        public static void VortexSwirl(Vector2 center, float radius, int count, Color innerColor, Color outerColor, int duration = 50)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;

            var template = new ParticleData
            {
                Velocity = Vector2.Zero,
                Scale = Vector2.One * 0.8f,
                PackedColor = ParticleManager.PackColor(outerColor),
                PackedStartColor = ParticleManager.PackColor(outerColor),
                PackedEndColor = ParticleManager.PackColor(innerColor),
                TimeLeft = duration,
                Duration = duration,
                TextureId = ParticleTex.SoftGlow,
                BlendMode = 1,
                LayerPriority = LayerPriorities.BeforeProjectiles,
            };
            template.EnableComponent(ComponentFlag.FadeOut);
            template.EnableComponent(ComponentFlag.ColorShift);

            ParticleManager.SpawnShape(center, ShapeDescriptor.Vortex(radius, 2f), count, template);
        }
    }
}
