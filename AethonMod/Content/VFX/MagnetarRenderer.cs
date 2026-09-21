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
    /// MagnetarRenderer — v6.26 — EL MAGNETAR.
    ///
    /// La estrella de neutrones EXTREMA: el campo magnético más violento
    /// del universo, estilizado en VIOLETA. La firma visual:
    ///
    ///   · LÍNEAS DE CAMPO INTENSAS Y RETORCIDAS — 6 bucles dipolares
    ///     (r = L·sen²θ, la ecuación de la casa) donde cada punto SUFRE
    ///     una torsión extra (RotatedBy: θ' = θ + k·sen 2θ) — el campo
    ///     del magnetar no es un dipolo limpio: está ESTRANGULADO.
    ///   · LA TORMENTA VIOLETA — StormLib.Bolt descargando entre puntos
    ///     del propio campo (las líneas se CORTAN entre sí) al ritmo del
    ///     flick de 14 Hz de la casa, en paleta violeta.
    ///   · LAS CHISPAS DE RECONEXIÓN — puntos blancos ESTALLANDO sobre
    ///     las líneas (StormLib.ImpactFlash): el momento exacto en que
    ///     dos líneas se tocan y reconectan soltando su energía.
    ///   · LA ARRITMIA — el latido del núcleo con fase CAÓTICA pero
    ///     determinista (seno de seno: irregular SIEMPRE IGUAL).
    ///
    /// CONTRATO DE BATCH: Draw() exige el SpriteBatch CERRADO y lo deja
    /// CERRADO (el llamador restaura el batch de tML — contrato v6.10).
    /// </summary>
    public static class MagnetarRenderer
    {
        /// <summary>Radio del cuerpo en px a escala 1 (v6.31: 26→31 — más grande).</summary>
        public const float BodyPx = 31f;

        /// <summary>Alcance de las CADENAS DE RAYO automáticas (px).</summary>
        public const float ChainRange = 240f;

        /// <summary>Periodo de la descarga de cadenas (ticks).</summary>
        public const int ChainPeriod = 20;

        /// <summary>Rotación visible (el campo co-rota, nervioso).</summary>
        public const float SpinRate = MathHelper.TwoPi * 1.2f;

        // --- PALETA violeta violenta ---
        private static readonly Color VioletCore = new(170, 110, 240);
        private static readonly Color VioletWhite = new(255, 250, 255);
        private static readonly Color VioletDeep = new(120, 70, 200);
        private static readonly Color VioletPink = new(230, 150, 255);

        // --- LOS SEIS BUCLES DEL CAMPO: L (×R), torsión, colores, alpha ---
        private static readonly (float L, float Twist, float Alpha)[] Field =
        {
            (1.55f, 0.22f, 0.42f),
            (2.10f, -0.34f, 0.36f),
            (2.70f, 0.47f, 0.32f),
            (3.35f, -0.55f, 0.28f),
            (4.05f, 0.68f, 0.24f),
            (4.80f, -0.80f, 0.20f),
        };

        // --- PINCELES ---
        private static Asset<Texture2D> _glow;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;


        // ==================================================================
        //  EL RENDER PRINCIPAL — batch CERRADO → CERRADO
        // ==================================================================

        /// <summary>
        /// Dibuja el magnetar: núcleo violeta con ARRITMIA + las líneas de
        /// campo retorcidas + la tormenta de rayos violeta + las chispas
        /// de reconexión. `lifeT` = 0..1, `seed` = semilla del disparo.
        /// </summary>
        public static void Draw(Projectile p, float lifeT, int seed)
        {
            try
            {
                Vector2 drawPos = p.Center - Main.screenPosition;
                float scale = Math.Max(p.scale, 0.05f);
                float R = BodyPx * scale;
                float time = Main.GlobalTimeWrappedHourly;
                float spin = time * SpinRate;
                int flick = StormLib.FlickTick(time, 14f);
                float fade = 1f - lifeT * 0.40f;

                // LA ARRITMIA: fase caótica DETERMINISTA (seno de seno —
                // el latido nunca se repite igual pero SIEMPRE es el mismo:
                // un corazón enfermo y fiable a la vez).
                float arrhythmia = Arrhythmia(time, seed);
                float pulse = 0.55f + 0.45f * arrhythmia;

                BeginAdditive();

                // === 1. EL RESPLANDOR MAGNÉTICO (el aura violeta arrítmico) ===
                Main.spriteBatch.Draw(Glow, drawPos, null,
                    Tint(VioletDeep, 0.34f * pulse * fade), 0f,
                    Glow.Size() * 0.5f, ScaleOf(R * 4.4f * (0.9f + 0.2f * pulse)), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(Glow, drawPos, null,
                    Tint(VioletCore, 0.42f * pulse * fade), 0f,
                    Glow.Size() * 0.5f, ScaleOf(R * 2.1f * (0.9f + 0.2f * pulse)), SpriteEffects.None, 0f);

                // === 2. LAS LÍNEAS DE CAMPO RETORCIDAS (6 bucles) ===
                for (int k = 0; k < Field.Length; k++)
                    DrawTwistedLoop(drawPos, R, spin, k, time, fade);

                // === 3. LA TORMENTA VIOLETA (rayos entre líneas del campo) ===
                DrawFieldStorm(drawPos, R, spin, seed, flick, time, fade);

                // === 4. LAS CHISPAS DE RECONEXIÓN (puntos blancos estallando) ===
                DrawReconnectionSparks(drawPos, R, spin, seed, flick, time, fade);

                // === 5. LAS CADENAS A LOS ENEMIGOS (la tormenta que DAÑA:
                //     el mismo test que hace el servidor, releído aquí en
                //     visual determinista — las cadenas se VEN donde pegan) ===
                DrawChainStrikes(p, drawPos, R, seed, flick, time, fade);

                Main.spriteBatch.End();

                // === 6. EL NÚCLEO (violeta-blanco) — v6.31: LA TÉCNICA DEL SOL
                //     ORIGINAL (DrawSunBody gestiona SUS PROPIOS lotes → va
                //     DESPUÉS de cerrar el aditivo; el orbe+blob BORRADOS) ===
                RuneSunRenderer.DrawSunBody(drawPos, R, p.rotation, time,
                    new Color(238, 224, 255),
                    new Color(128, 96, 190),
                    new Color(70, 25, 130),
                    new Color(168, 120, 255),
                    new Color(108, 70, 220),
                    new Color(205, 175, 255),
                    1.1f, fade * (0.75f + 0.25f * pulse));
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ==================================================================
        //  LA ARRITMIA — fase caótica determinista
        // ==================================================================

        /// <summary>
        /// El latido IRREGULAR del magnetar: seno de seno con la semilla —
        /// caótico a ojos del jugador, EXACTAMENTE reproducible para la
        /// máquina (la casa no tolera estado no determinista en el render).
        /// </summary>
        private static float Arrhythmia(float time, int seed) =>
            MathF.Sin(time * 6.3f + MathF.Sin(time * 2.17f + seed * 0.7f) * 2.4f + seed);

        /// <summary>La tanda aditiva de la casa (v6.26 — el helper que faltaba).</summary>
        private static void BeginAdditive()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        // ==================================================================
        //  CAPA 2 — LAS LÍNEAS DE CAMPO RETORCIDAS
        // ==================================================================

        private static void DrawTwistedLoop(Vector2 pos, float R, float spin,
            int k, float time, float fade)
        {
            (float L, float twist, float alpha) = Field[k];
            float loopL = L * R;
            const int Segs = 36;

            // El color del bucle: violeta profundo afuera, rosa adentro.
            Color body = Color.Lerp(VioletDeep, VioletCore, k / (float)Field.Length);
            Color tip = Color.Lerp(VioletCore, VioletPink, 0.5f);

            Vector2 prev = TwistedDipolePoint(pos, loopL, spin, twist, 0f);
            for (int s = 1; s <= Segs; s++)
            {
                float t = s / (float)Segs * MathHelper.TwoPi;
                Vector2 pt = TwistedDipolePoint(pos, loopL, spin, twist, t);
                Vector2 mid = (prev + pt) * 0.5f;
                Vector2 delta = pt - prev;
                float len = delta.Length();
                if (len > 0.5f)
                {
                    float rot = (float)Math.Atan2(delta.Y, delta.X);
                    float polAx = Math.Abs(MathF.Sin(t));
                    // El TEMBLOR del campo: la torsión VIVE (0.8 Hz nervioso).
                    float quake = 0.80f + 0.20f * MathF.Sin(time * 5.1f + k * 1.9f + s * 0.5f);
                    Color c = Color.Lerp(body, tip, 0.30f + 0.40f * polAx);
                    Capsule(mid, len, Math.Max(1.3f, R * 0.085f) * (1f + 0.35f * polAx),
                        rot, Tint(c, alpha * quake * fade));
                }
                prev = pt;
            }
        }

        /// <summary>
        /// Punto del bucle dipolar RETORCIDO: r = L·sen²θ con la dirección
        /// deformada (θ' = θ + twist·sen 2θ) y todo girado por `rot` —
        /// el campo del magnetar ESTRANGULADO sobre sí mismo.
        /// </summary>
        private static Vector2 TwistedDipolePoint(Vector2 center, float L,
            float rot, float twist, float t)
        {
            float r = L * MathF.Sin(t) * MathF.Sin(t);
            if (r < 0.02f) r = 0.02f;
            float tt = t + twist * MathF.Sin(2f * t);
            Vector2 local = new Vector2(MathF.Cos(tt) * r, MathF.Sin(tt) * r);
            return center + local.RotatedBy(rot);
        }

        // ==================================================================
        //  CAPA 3 — LA TORMENTA VIOLETA (rayos entre líneas del campo)
        // ==================================================================

        private static void DrawFieldStorm(Vector2 pos, float R, float spin,
            int seed, int flick, float time, float fade)
        {
            // DOS DESCARGAS por flick: cada rayo une DOS puntos del campo
            // (líneas distintas que se CORTAN — la descarga de la tensión).
            for (int b = 0; b < 2; b++)
            {
                if (!StormLib.IsLit(seed + 611 + b * 71, flick, 0.42f)) continue;

                // Punto A: bucle interior; Punto B: bucle exterior.
                int kA = (int)(VFXCore.Hash01(seed, 613 + b, flick) * 3f);
                int kB = 3 + (int)(VFXCore.Hash01(seed, 617 + b, flick) * 3f);
                float tA = VFXCore.Hash01(seed, 619 + b, flick) * MathHelper.TwoPi;
                float tB = VFXCore.Hash01(seed, 623 + b, flick) * MathHelper.TwoPi;
                Vector2 a = TwistedDipolePoint(pos, Field[kA].L * R, spin, Field[kA].Twist, tA);
                Vector2 b2 = TwistedDipolePoint(pos, Field[kB].L * R, spin, Field[kB].Twist, tB);

                StormLib.Bolt(Main.spriteBatch, a, b2, seed + 631 + b * 37, flick,
                    Math.Max(2.0f, R * 0.14f),
                    Tint(VioletDeep, 0.85f * fade), Tint(VioletWhite, 0.95f * fade),
                    0.9f * fade, 8, 8f + R * 0.3f);
            }
        }

        // ==================================================================
        //  CAPA 4 — LAS CHISPAS DE RECONEXIÓN
        // ==================================================================

        private static void DrawReconnectionSparks(Vector2 pos, float R, float spin,
            int seed, int flick, float time, float fade)
        {
            // TRES chispas por flick: puntos blancos ESTALLANDO sobre las
            // líneas (donde el campo se reconecta, la energía sale BLANCA).
            for (int s = 0; s < 3; s++)
            {
                if (!StormLib.IsLit(seed + 711 + s * 53, flick, 0.50f)) continue;

                int k = (int)(VFXCore.Hash01(seed, 713 + s, flick) * Field.Length);
                float t = VFXCore.Hash01(seed, 717 + s, flick) * MathHelper.TwoPi;
                Vector2 spark = TwistedDipolePoint(pos, Field[k].L * R, spin,
                    Field[k].Twist, t);

                // La intensidad NACE y MUERE dentro del flick (el estallido).
                float life = VFXCore.Hash01(seed, 719 + s, flick + 1);
                float intensity = 4f * life * (1f - life);   // campana 0..1
                StormLib.ImpactFlash(Main.spriteBatch, spark, R * (0.5f + 0.5f * intensity),
                    VioletPink, 0.65f * intensity * fade, time * 4f + s);
            }
        }

        // ==================================================================
        //  CAPA 6 — LAS CADENAS A LOS ENEMIGOS (la tormenta que DAÑA)
        // ==================================================================

        private static void DrawChainStrikes(Projectile p, Vector2 pos, float R,
            int seed, int flick, float time, float fade)
        {
            // Visual CLIENTE puro: releer los 3 enemigos más cercanos en
            // ChainRange (el mismo criterio del daño del servidor) y
            // descargar las cadenas sobre ellos. El FLASH se abre en la
            // primera ventana de cada periodo de 20 ticks (3 Hz — el
            // mismo ritmo al que el servidor reparte el daño).
            if (Main.netMode == NetmodeID.Server) return;

            // La ventana de descarga: los primeros 6 ticks del periodo.
            uint g = Main.GameUpdateCount;
            float window = (g % ChainPeriod) / (float)ChainPeriod;
            if (window > 0.30f) return;
            float strikeA = 1f - window / 0.30f;   // el golpe MUERE en 6 ticks

            int found = 0;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (found >= 3) break;
                if (!VFXCore.EsObjetivo(npc)) continue;
                float dist = (npc.Center - p.Center).Length();
                if (dist > ChainRange || dist < R * 2f) continue;

                Vector2 target = npc.Center - Main.screenPosition;
                StormLib.ChainBolt(Main.spriteBatch, pos, target,
                    seed + 811 + found * 41, flick,
                    Math.Max(2.4f, R * 0.16f),
                    Tint(VioletDeep, 0.85f * strikeA * fade),
                    Tint(VioletWhite, 0.95f * strikeA * fade),
                    strikeA * fade, 8, 10f + R * 0.25f);

                // El gorro de impacto sobre el enemigo golpeado.
                StormLib.EndCap(Main.spriteBatch, target, R * 0.9f,
                    Tint(VioletPink, 0.6f * strikeA * fade),
                    Tint(VioletWhite, 0.9f * strikeA * fade), strikeA * fade);
                found++;
            }
        }

        // ==================================================================
        //  PRIMITIVAS INTERNAS
        // ==================================================================

        /// <summary>Cápsula de luz orientada (el trazo de las líneas de campo).</summary>
        private static void Capsule(Vector2 mid, float len, float width, float rot, Color tint)
        {
            if (tint.A == 0 || width < 0.5f) return;
            Main.spriteBatch.Draw(Glow, mid, null, tint, rot,
                Glow.Size() * 0.5f,
                new Vector2(len + width, width * 1.9f) / Glow.Size(),
                SpriteEffects.None, 0f);
        }

        /// <summary>Escala uniforme del pincel Glow para un diámetro dado.</summary>
        private static Vector2 ScaleOf(float diameter) =>
            new Vector2(diameter, diameter) / Glow.Size();

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
