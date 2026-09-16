using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// NeutronStarRenderer — v6.26 — LA ESTRELLA DE NEUTRONES.
    ///
    /// El cadáver ultradenso de un sol masivo: un NÚCLEO MINÚSCULO
    /// (~12 px) blanco-azulado cegador cuya firma visual es triple:
    ///
    ///   · SUPERFICIE CHISPEANTE — destellos de 4 puntas a 20 Hz
    ///     (LumenLib.Flare sobre posiciones de superficie fijas por
    ///     semilla, encendidas por StormLib.IsLit/FlickTick).
    ///   · LÍNEAS DE CAMPO MAGNÉTICO DIPOALRES — bucles cerrados de
    ///     polo a polo con la ecuación REAL del dipolo (r = L·sen²θ),
    ///     en arcos elipse concéntricos con tinte de aurora (cian →
    ///     azul → violeta por bucle) — y co-rotando con la estrella.
    ///   · ROTACIÓN RÁPIDA VISIBLE — el campo entero gira a 1.5 rev/s
    ///     y el ecuador lleva arcos de velocidad que lo delatan.
    ///   · STARQUAKE — cuando el proyectil lo señala (quakeT &gt; 0):
    ///     aberración R/B del núcleo (el truco de los 3 draws de canal
    ///     puro) + anillo de onda OndaLib.Pulse expandiéndose.
    ///
    /// CONTRATO DE BATCH: Draw() exige el SpriteBatch CERRADO y lo deja
    /// CERRADO (el llamador restaura el batch de tML — contrato v6.10).
    /// </summary>
    public static class NeutronStarRenderer
    {
        /// <summary>Radio del cuerpo en px a escala 1 (v6.31: 22→26 — más grande).</summary>
        public const float BodyPx = 26f;

        /// <summary>Rotación visible: 1.5 rev/s (el campo co-rota con la estrella).</summary>
        public const float SpinRate = MathHelper.TwoPi * 1.5f;

        // --- PALETA blanco-azul cegador + aurora del campo ---
        private static readonly Color WhiteBlinding = new(240, 246, 255);
        private static readonly Color CoreBlue = new(150, 190, 255);
        private static readonly FieldArc[] Field = new FieldArc[]
        {
            new(1.65f, new Color(110, 235, 235), new Color(190, 250, 255), 0.42f),
            new(2.35f, new Color(120, 205, 255), new Color(210, 240, 255), 0.34f),
            new(3.15f, new Color(140, 150, 255), new Color(200, 220, 255), 0.28f),
            new(4.05f, new Color(150, 120, 240), new Color(190, 180, 255), 0.22f),
        };

        /// <summary>Un bucle de campo dipolar: L (×R), color cuerpo, color punta, alpha.</summary>
        private readonly struct FieldArc
        {
            public readonly float L; public readonly Color Body; public readonly Color Tip; public readonly float Alpha;
            public FieldArc(float l, Color body, Color tip, float alpha)
            { L = l; Body = body; Tip = tip; Alpha = alpha; }
        }

        // --- PINCELES ---
        private static Asset<Texture2D> _glow;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        // ==================================================================
        //  EL RENDER PRINCIPAL — batch CERRADO → CERRADO
        // ==================================================================

        /// <summary>
        /// Dibuja la estrella de neutrones: núcleo cegador + chispas de
        /// superficie a 20 Hz + campo dipolar co-rotante + starquake.
        /// `lifeT` = 0..1 del ciclo de vida, `quakeT` = 0..1 del
        /// starquake activo (0 = ninguno), `seed` = semilla del disparo.
        /// </summary>
        public static void Draw(Projectile p, float lifeT, float quakeT, int seed)
        {
            try
            {
                Vector2 drawPos = p.Center - Main.screenPosition;
                float scale = Math.Max(p.scale, 0.05f);
                float R = BodyPx * scale;
                float time = Main.GlobalTimeWrappedHourly;
                float spin = time * SpinRate;
                int flick20 = StormLib.FlickTick(time, 20f);   // la superficie a 20 Hz
                float fade = 1f - lifeT * 0.45f;

                BeginAdditive();

                // === 1. EL RESPLANDOR PROFUNDO (el halo del ultradenso) ===
                DrawGlow(drawPos, R, time, fade);

                // === 2. EL CAMPO DIPOLAR (los bucles r = L·sen²θ, aurora) ===
                for (int k = 0; k < Field.Length; k++)
                    DrawDipoleLoop(drawPos, R, spin, seed, k, time, fade);

                // === 3. EL ECUADOR EN ROTACIÓN (arcos de velocidad — la
                //     rotación se VE, no se explica) ===
                DrawEquatorStreaks(drawPos, R, spin, time, fade);

                // === 4. LA SUPERFICIE CHISPEANTE (4 puntas a 20 Hz) ===
                DrawSurfaceSparkles(drawPos, R, spin, seed, flick20, fade);

                // === 5. EL STARQUAKE (onda + aberración del núcleo) ===
                if (quakeT > 0f)
                    DrawStarquake(drawPos, R, quakeT, time, seed);

                Main.spriteBatch.End();

                // === 6. EL CUERPO — LA TÉCNICA DEL SOL ORIGINAL (v6.31: el disco
                //     de plasma SunShader azul-blanco con granulación dendrítica,
                //     girando RÁPIDO — la superficie DURA de verdad, no un orbe;
                //     el jitter del starquake lo hace TIEMBLAR) ===
                Vector2 jitter = quakeT > 0f
                    ? new Vector2(MathF.Sin(time * 63f), MathF.Cos(time * 71f)) * R * 0.06f * quakeT
                    : Vector2.Zero;
                RuneSunRenderer.DrawSunBody(drawPos + jitter, R, p.rotation, time,
                    new Color(235, 242, 255),
                    new Color(118, 148, 222),
                    new Color(45, 75, 205),
                    new Color(150, 190, 255),
                    new Color(82, 112, 255),
                    new Color(192, 216, 255),
                    2.5f, fade);
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error (contrato v6.10).
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ==================================================================
        //  CAPA 1 — EL RESPLANDOR
        // ==================================================================

        private static void DrawGlow(Vector2 pos, float R, float time, float fade)
        {
            // Halo frío amplio (el "peso" de la estrella en el mundo).
            Color halo = Tint(CoreBlue, 0.30f * fade);
            Main.spriteBatch.Draw(Glow, pos, null, halo, 0f,
                Glow.Size() * 0.5f, ScaleOf(R * 4.2f), SpriteEffects.None, 0f);
            // Corona interna intensa.
            Color cor = Tint(WhiteBlinding, 0.45f * fade);
            Main.spriteBatch.Draw(Glow, pos, null, cor, 0f,
                Glow.Size() * 0.5f, ScaleOf(R * 2.0f), SpriteEffects.None, 0f);
        }

        // ==================================================================
        //  CAPA 2 — EL CAMPO DIPOLAR (r = L·sen²θ, curvándose polo a polo)
        // ==================================================================

        private static void DrawDipoleLoop(Vector2 pos, float R, float spin,
            int seed, int k, float time, float fade)
        {
            FieldArc arc = Field[k];
            float L = arc.L * R;
            const int Segs = 34;

            Vector2 prev = DipolePoint(pos, L, spin, 0f);
            for (int s = 1; s <= Segs; s++)
            {
                float t = s / (float)Segs * MathHelper.TwoPi;
                Vector2 pt = DipolePoint(pos, L, spin, t);
                Vector2 mid = (prev + pt) * 0.5f;
                Vector2 delta = pt - prev;
                float len = delta.Length();
                if (len > 0.5f)
                {
                    float rot = (float)Math.Atan2(delta.Y, delta.X);
                    // El brillo VIVE cerca de los polos (sen²θ → máximo
                    // ecuador en r, pero la DENSIDAD de líneas aprieta
                    // los polos): alpha por cercanía al eje de giro.
                    float polAx = Math.Abs(MathF.Sin(t));
                    float breathe = 0.75f + 0.25f * MathF.Sin(time * 2.6f + k * 1.7f + s * 0.4f);
                    Color c = Color.Lerp(arc.Body, arc.Tip, 0.35f + 0.35f * polAx);
                    Capsule(mid, len, Math.Max(1.4f, R * 0.10f) * (1f + 0.3f * polAx),
                        rot, Tint(c, arc.Alpha * breathe * fade));
                }
                prev = pt;
            }

            // Las PERLAS POLARES: dos puntos de aurora en el eje de giro.
            Vector2 axis = new Vector2(MathF.Cos(spin + MathHelper.PiOver2),
                                       MathF.Sin(spin + MathHelper.PiOver2));
            float pearlR = R * 1.1f;
            for (int side = 0; side < 2; side++)
            {
                Vector2 pearl = pos + axis * (side == 0 ? pearlR : -pearlR);
                float pulse = 0.65f + 0.35f * MathF.Sin(time * 3.1f + side * MathHelper.Pi + k);
                Main.spriteBatch.Draw(Glow, pearl, null,
                    Tint(arc.Tip, 0.55f * arc.Alpha * pulse * fade * 2f), 0f,
                    Glow.Size() * 0.5f, ScaleOf(R * 0.55f), SpriteEffects.None, 0f);
            }
        }

        /// <summary>Punto del bucle dipolar r = L·sen²θ girado por `rot`.</summary>
        private static Vector2 DipolePoint(Vector2 center, float L, float rot, float t)
        {
            float r = L * MathF.Sin(t) * MathF.Sin(t);
            if (r < 0.02f) r = 0.02f;   // pegado al polo: cierra el bucle
            Vector2 local = new Vector2(MathF.Cos(t) * r, MathF.Sin(t) * r);
            return center + local.RotatedBy(rot);
        }

        // ==================================================================
        //  CAPA 3 — EL ECUADOR EN ROTACIÓN (la rotación VISIBLE)
        // ==================================================================

        private static void DrawEquatorStreaks(Vector2 pos, float R, float spin,
            float time, float fade)
        {
            // 3 arcos cortos sobre el ecuador (R·1.02) avanzando con el
            // giro: al girar tan rápido se leen como "estelas" de superficie.
            for (int a = 0; a < 3; a++)
            {
                float baseA = spin * 1.15f + a * (MathHelper.TwoPi / 3f);
                const float Span = 0.5f;
                const int Segs = 8;
                Vector2 prev = pos + new Vector2(MathF.Cos(baseA), MathF.Sin(baseA)) * (R * 1.02f);
                for (int s = 1; s <= Segs; s++)
                {
                    float ang = baseA + Span * s / Segs;
                    Vector2 pt = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (R * 1.02f);
                    Vector2 mid = (prev + pt) * 0.5f;
                    Vector2 d = pt - prev;
                    float len = d.Length();
                    if (len > 0.4f)
                        Capsule(mid, len, R * 0.085f, (float)Math.Atan2(d.Y, d.X),
                            Tint(WhiteBlinding, 0.50f * fade * (1f - s / (float)(Segs + 2))));
                    prev = pt;
                }
            }
        }

        // ==================================================================
        //  CAPA 4 — EL CUERPO (v6.31: LA TÉCNICA DEL SOL ORIGINAL vive en
        //  RuneSunRenderer.DrawSunBody — el disco de plasma SunShader
        //  azul-blanco; el DrawCore de orbe+blob BORRADO)
        // ==================================================================

        // ==================================================================
        //  CAPA 5 — LA SUPERFICIE CHISPEANTE (4 puntas a 20 Hz)
        // ==================================================================

        private static void DrawSurfaceSparkles(Vector2 pos, float R, float spin,
            int seed, int flick20, float fade)
        {
            const int Count = 6;
            for (int k = 0; k < Count; k++)
            {
                if (!StormLib.IsLit(seed + 311 + k * 53, flick20, 0.55f)) continue;

                // Posición de superficie FIJA por semilla, co-rotando.
                float ang = VFXCore.Hash01(seed, 71 + k, 3) * MathHelper.TwoPi + spin;
                float rr = R * (0.35f + 0.65f * VFXCore.Hash01(seed, 83 + k, 7));
                Vector2 sPos = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;

                LumenLib.Flare(Main.spriteBatch, sPos, R * (0.85f + 0.5f * VFXCore.Hash01(seed, 97 + k, 11)),
                    CoreBlue, 0.85f * fade, spin * 0.30f + k * 0.9f);
            }
        }

        // ==================================================================
        //  CAPA 6 — EL STARQUAKE (la corteza se rompe)
        // ==================================================================

        private static void DrawStarquake(Vector2 pos, float R, float quakeT,
            float time, int seed)
        {
            // 1. EL ANILLO DE ONDA expandiéndose (OndaLib.Pulse — el eco
            //    sísmico de la corteza rota).
            OndaLib.Pulse(Main.spriteBatch, pos, quakeT, R * (2.2f + 2.6f * quakeT),
                CoreBlue, 0.55f * (1f - quakeT * 0.5f), seed);

            // 2. LA ABERRACIÓN R/B del núcleo (el truco de los 3 draws
            //    con tinte de canal puro — la lección de la investigación interna "Thanks
            //    spirit"): el núcleo se DESDOZA durante el temblor.
            float off = R * 0.14f * quakeT;
            Main.spriteBatch.Draw(Glow, pos - new Vector2(off, 0f), null,
                Tint(new Color(255, 60, 60), 0.55f * quakeT), 0f,
                Glow.Size() * 0.5f, ScaleOf(R * 0.8f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Glow, pos + new Vector2(off, 0f), null,
                Tint(new Color(60, 90, 255), 0.55f * quakeT), 0f,
                Glow.Size() * 0.5f, ScaleOf(R * 0.8f), SpriteEffects.None, 0f);

            // 3. EL DESTELLO CENTRAL del temblor (el latido del terremoto).
            LumenLib.Flare(Main.spriteBatch, pos, R * 1.9f * (1f - quakeT * 0.4f),
                WhiteBlinding, 0.60f * (1f - quakeT * 0.5f), time * 2.2f);
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

        /// <summary>Abre el lote ADITIVO de la casa (brillos).</summary>
        private static void BeginAdditive()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Tinte premultiplicado de la casa (v6.25).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(
                (byte)(int)(c.R * f), (byte)(int)(c.G * f), (byte)(int)(c.B * f),
                (byte)(int)(255f * f));
        }
    }
}
