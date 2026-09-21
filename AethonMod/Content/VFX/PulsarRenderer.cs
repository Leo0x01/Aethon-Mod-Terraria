using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// PulsarRenderer — v6.26 — EL PÚLSAR (EL FARO CÓSMICO).
    ///
    /// La estrella de neutrones GIRANDO con sus DOS HACES POLARES de
    /// luz barriendo el mundo como un faro (~1 rev/s). La firma visual:
    ///
    ///   · EL CUERPO — LA TÉCNICA DEL SOL ORIGINAL (v6.31, DrawSunBody)
    ///     (Effects/Procedural/), girando SOLIDARIO con el haz: la
    ///     estrella y su luz son UN solo cuerpo en rotación.
    ///   · LOS DOS HACES OPUESTOS — LumenLib.Ray de 400 px a lo largo
    ///     del eje magnético, con el GROSOR respirando al ritmo del
    ///     giro (el pulso vive EN el barrido, no encima).
    ///   · EL CONO DEL FARO ("lighthouse cone") — el abanico tenue que
    ///     el barrido deja ATRÁS: 5 Ray cortos en abanico ±10° detrás
    ///     de cada haz (el rastro del barrido).
    ///   · PULSOS SINCRONIZADOS DE LUZ — el bloom central late a la
    ///     frecuencia de giro (dos máximos por revolución: uno por haz).
    ///   · EL ANILLO DE EMISIÓN — un aro fino PERPENDICULAR al haz (el
    ///     plano del ecuador de emisión) con perlas corriendo.
    ///
    /// CONTRATO DE BATCH: Draw() exige el SpriteBatch CERRADO y lo deja
    /// CERRADO (el llamador restaura el batch de tML — contrato v6.10).
    /// </summary>
    public static class PulsarRenderer
    {
        /// <summary>Radio del cuerpo en px a escala 1 (v6.31: 28→33 — más grande).</summary>
        public const float BodyPx = 33f;

        /// <summary>Longitud del haz polar en px (el faro de 400 px).</summary>
        public const float BeamLength = 400f;

        /// <summary>Rotación del faro: ~1 rev/s (la especificación del púlsar).</summary>
        public const float SpinRate = MathHelper.TwoPi * 1f;

        // --- PALETA azul faro ---
        private static readonly Color BeamBlue = new(140, 185, 255);
        private static readonly Color BeamCyan = new(100, 220, 240);
        private static readonly Color WhiteHot = new(245, 250, 255);

        // --- PINCELES ---
        private static Asset<Texture2D> _glow;
        private static Asset<Texture2D> _ring;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;


        private static Texture2D Ring =>
            (_ring ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring")).Value;

        // ==================================================================
        //  EL RENDER PRINCIPAL — batch CERRADO → CERRADO
        // ==================================================================

        /// <summary>
        /// Dibuja el púlsar: EL CUERPO DEL SOL girando + los DOS haces
        /// polares opuestos + los conos del faro + el anillo de emisión.
        /// `beamAngle` = ángulo ACTUAL del haz principal (rad, determinista:
        /// se calcula igual en servidor y clientes), `lifeT` = 0..1 del
        /// ciclo, `seed` = semilla del disparo.
        /// </summary>
        public static void Draw(Projectile p, float beamAngle, float lifeT, int seed)
        {
            try
            {
                Vector2 drawPos = p.Center - Main.screenPosition;
                float scale = Math.Max(p.scale, 0.05f);
                float R = BodyPx * scale;
                float time = Main.GlobalTimeWrappedHourly;
                float fade = 1f - lifeT * 0.40f;

                // EL PULSO SINCRONIZADO: dos máximos por revolución (uno
                // por cada haz que pasa por delante). El grosor de los
                // rayos y el bloom viven de AQUÍ.
                float pulse = 0.5f + 0.5f * MathF.Sin(beamAngle * 2f);

                BeginAdditive();

                // === 1. EL RESPLANDOR DEL FARO (bloom latiendo al giro) ===
                LumenLib.Bloom(Main.spriteBatch, drawPos, R * (2.1f + 0.7f * pulse),
                    BeamBlue, (0.45f + 0.30f * pulse) * fade, 3);

                // === 2. EL ANILLO DE EMISIÓN (perpendicular al haz) ===
                DrawEmissionRing(drawPos, R, beamAngle, time, seed, fade);

                // === 3. LOS DOS HACES POLARES OPUESTOS + SUS CONOS ===
                for (int side = 0; side < 2; side++)
                {
                    Vector2 dir = AngleToDir(beamAngle + side * MathHelper.Pi);
                    DrawBeam(drawPos, dir, R, pulse, fade, seed + side * 17);
                }

                // === 4. LOS IMPACTOS DEL BARRIDO (visual determinista en el
                //     cliente: si un enemigo está EN el haz, chispea) ===
                DrawSweepFlashes(p, drawPos, beamAngle, R, time, seed);

                Main.spriteBatch.End();

                // === 5. EL CUERPO — LA TÉCNICA DEL SOL ORIGINAL (v6.31:
                //     DrawSunBody gestiona SUS lotes → DESPUÉS del aditivo;
                //     el disco azul-blanco gira SOLDADO AL HAZ — la estrella
                //     ES el giro; el PulsarCore de textura plana BORRADO) ===
                RuneSunRenderer.DrawSunBody(drawPos, R, beamAngle, time,
                    new Color(240, 248, 255),
                    new Color(126, 160, 235),
                    new Color(50, 80, 215),
                    new Color(150, 200, 255),
                    new Color(90, 120, 255),
                    new Color(198, 222, 255),
                    0.5f, fade * (0.80f + 0.20f * pulse));
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ==================================================================
        //  CAPA 2 — EL ANILLO DE EMISIÓN (el ecuador del faro)
        // ==================================================================

        private static void DrawEmissionRing(Vector2 pos, float R, float beamAngle,
            float time, int seed, float fade)
        {
            // Aro fino perpendicular al haz (girado al ángulo del haz).
            float rot = beamAngle + MathHelper.PiOver2;
            Vector2 size = VFXCore.RingQuadSize(R * 1.9f);
            Main.spriteBatch.Draw(Ring, pos, null,
                Tint(BeamCyan, 0.35f * fade), rot,
                Ring.Size() * 0.5f,
                size / Ring.Size(), SpriteEffects.None, 0f);

            // Perlas corriendo por el anillo (el material cayendo al disco).
            const int Pearls = 5;
            for (int i = 0; i < Pearls; i++)
            {
                float t = i / (float)Pearls * MathHelper.TwoPi + time * 1.4f;
                Vector2 local = new Vector2(MathF.Cos(t), MathF.Sin(t) * 0.32f) * (R * 1.9f);
                Vector2 pearl = pos + local.RotatedBy(rot);
                float tw = 0.6f + 0.4f * MathF.Sin(time * 6f + i * 2.1f);
                Main.spriteBatch.Draw(Glow, pearl, null,
                    Tint(WhiteHot, 0.55f * tw * fade), 0f,
                    Glow.Size() * 0.5f, ScaleOf(R * 0.30f), SpriteEffects.None, 0f);
            }
        }

        // ==================================================================
        //  CAPA 3 — EL HAZ POLAR + EL CONO DEL FARO
        // ==================================================================

        private static void DrawBeam(Vector2 pos, Vector2 dir, float R,
            float pulse, float fade, int seed)
        {
            // === 3a. EL CONO DEL FARO: el abanico tenue que el barrido
            //     deja ATRÁS (5 Ray cortos en ±10° tras el haz — el
            //     rastro del barrido de 1 rev/s). ===
            float beamRot = (float)Math.Atan2(dir.Y, dir.X);
            const int ConeRays = 5;
            for (int i = 0; i < ConeRays; i++)
            {
                float spread = (i / (float)(ConeRays - 1) - 0.5f) * 0.35f;   // ±10°
                Vector2 coneDir = AngleToDir(beamRot + spread);
                float coneLen = BeamLength * (0.30f + 0.22f * (1f - Math.Abs(spread) / 0.175f));
                LumenLib.Ray(Main.spriteBatch, pos + coneDir * (R * 0.7f), coneDir,
                    coneLen * (0.9f + 0.1f * pulse), R * 0.9f,
                    BeamBlue, 0.16f * fade, 0.3f + 0.3f * pulse);
            }

            // === 3b. EL HAZ PRINCIPAL: LumenLib.Ray de 400 px con el
            //     grosor respirando al pulso del giro. ===
            LumenLib.Ray(Main.spriteBatch, pos + dir * (R * 0.55f), dir,
                BeamLength, R * (1.5f + 0.8f * pulse),
                BeamCyan, (0.55f + 0.30f * pulse) * fade, pulse);

            // === 3c. LA BOCA DEL HAZ: el gorro de descarga polar. ===
            StormLib.EndCap(Main.spriteBatch, pos + dir * (R * 0.55f), R * 1.1f,
                Tint(BeamBlue, 0.6f * fade), Tint(WhiteHot, 0.9f * fade), fade);
        }

        // ==================================================================
        //  CAPA 4 — EL CUERPO (v6.31: LA TÉCNICA DEL SOL ORIGINAL vive en
        //  RuneSunRenderer.DrawSunBody, girando solidario al haz; el
        //  DrawCore de textura plana BORRADO)
        // ==================================================================

        // ==================================================================
        //  CAPA 5 — LOS IMPACTOS DEL BARRIDO (determinista, visual)
        // ==================================================================

        private static void DrawSweepFlashes(Projectile p, Vector2 drawPos,
            float beamAngle, float R, float time, int seed)
        {
            // Visual CLIENTE puro: releer los enemigos y chispear donde el
            // haz los está tocando (el daño real lo hace el servidor con
            // sus propios cooldowns — aquí solo se VE el barrido golpear).
            if (Main.netMode == NetmodeID.Server) return;

            for (int side = 0; side < 2; side++)
            {
                Vector2 dir = AngleToDir(beamAngle + side * MathHelper.Pi);
                Vector2 a = p.Center + dir * (R * 0.5f);
                Vector2 b = p.Center + dir * BeamLength;

                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if (DistToSegment(npc.Center, a, b) > R * 1.6f) continue;

                    Vector2 hit = npc.Center - Main.screenPosition;
                    // El destello late con el paso del haz (más brillante
                    // cuando el haz apunta DIRECTO al enemigo).
                    float aim = Vector2.Dot(Vector2.Normalize(npc.Center - p.Center), dir);
                    float strength = MathHelper.Clamp((aim - 0.75f) / 0.25f, 0f, 1f);
                    if (strength <= 0.05f) continue;
                    StormLib.ImpactFlash(Main.spriteBatch, hit, R * 1.2f,
                        BeamCyan, 0.55f * strength, time * 3.0f + side);
                }
            }
        }

        // ==================================================================
        //  HELPERS
        // ==================================================================

        /// <summary>Dirección unitaria de un ángulo.</summary>
        private static Vector2 AngleToDir(float ang) =>
            new Vector2(MathF.Cos(ang), MathF.Sin(ang));

        /// <summary>Distancia punto → segmento (el test del barrido del haz).</summary>
        private static float DistToSegment(Vector2 pt, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.LengthSquared();
            if (len2 < 0.001f) return (pt - a).Length();
            float t = MathHelper.Clamp(Vector2.Dot(pt - a, ab) / len2, 0f, 1f);
            return (pt - a - ab * t).Length();
        }

        /// <summary>Escala uniforme del pincel Glow para un diámetro dado.</summary>
        private static Vector2 ScaleOf(float diameter) =>
            new Vector2(diameter, diameter) / Glow.Size();

        /// <summary>Abre el lote ADITIVO de la casa (brillos).</summary>
        private static void BeginAdditive()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Tinte de intensidad LINEAL de la casa (v6.50.3 — el Additive
        /// de FNA es (SourceAlpha, One): el alfa GATEA; RGB intacto, alfa=f).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            // v6.50.3 — FIX (sonda IL contra el FNA real): BlendState.Additive
            // de FNA es (SourceAlpha, One) — el alfa GATEA el aporte. El Tint
            // premultiplicado v6.25 atenuaba DOS VECES (intensidad real f²:
            // el halo 0.30 salía a 0.09). RGB intacto, alfa=f: LINEAL.
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
