using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// PenduloJuicioRenderer — v6.26 — EL BASTÓN DEL PÉNDULO DEL JUICIO.
    ///
    /// EL PÉNDULO DE LUZ dibujado 100% por código:
    ///
    ///   · EL ANCLA: un halo dorado pequeño (LumenLib.Bloom) con su ANILLO
    ///     de sujeción (VFXCore.Ring) y una runa de clavado — el punto del
    ///     que cuelga la sentencia. Se queda dibujada un rato tras el corte
    ///     (el hueco del hilo).
    ///   · EL HILO: la línea del ancla a la maza — CÁPSULA dorada cuyo
    ///     BRILLO crece con la energía (alpha y grosor ∝ amplitud: la
    ///     sentencia se VE abrirse) + vena blanca interior.
    ///   · LA MAZA: bola de 26 px (GlowOrb con núcleo sólido) con
    ///     RUNAS GRABADAS — 8 marcas radiales oscuras sobre el cuerpo + un
    ///     aro de runas de pie (el estilo de la casa) + Flare al pasar por
    ///     el fondo (la velocidad máxima se lee).
    ///   · EL ARCO: la ESTELA del barrido (EstelaLib.Ribbon Comet sobre el
    ///     track de la maza — el rastro dorado de la sentencia) + FANTASMAS
    ///     del arco (3 posiciones anteriores de la maza, ghost-quads).
    ///   · EL CORTE: en el desprendimiento, chispas StormLib (MultiBolt
    ///     corto del ancla a la maza + ImpactFlash en el punto de corte) y
    ///     la maza vuela con su estela.
    ///
    /// CONTRATO DE BATCH: Draw() exige el SpriteBatch CERRADO y lo deja
    /// CERRADO (contrato v6.10). Brillos en Additive.
    /// </summary>
    public static class PenduloJuicioRenderer
    {
        // ==================================================================
        //  LA ANATOMÍA (px)
        // ==================================================================

        /// <summary>El radio de la MAZA (26 px de diámetro).</summary>
        private const float MazaR = 13f;

        // ==================================================================
        //  LAS PALETAS DEL JUICIO (el dorado de la sentencia)
        // ==================================================================

        private static readonly Color Oro = new(255, 205, 105);
        private static readonly Color OroCaliente = new(255, 240, 190);
        private static readonly Color RunaOscura = new(120, 70, 20);
        private static readonly Color HiloColor = new(255, 226, 150);

        // ==================================================================
        //  EL PUNTO DE ENTRADA
        // ==================================================================

        /// <summary>Dibuja el péndulo completo. Batch CERRADO → CERRADO.</summary>
        public static void Draw(PenduloJuicioProjectile p, float age, int seed)
        {
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 ancla = p.AnclaPublica - Main.screenPosition;
                Vector2 maza = p.Projectile.Center - Main.screenPosition;
                float energia = p.EnergiaPublica;

                // === 1. LA ESTELA DEL ARCO (el rastro del barrido/vuelo) ===
                DrawEstela(p, time, seed);

                // === 2. TODO EL CUERPO (Additive) ===
                BeginAdditive();

                if (!p.Suelto)
                {
                    // === EL HILO (brilla con la energía de la sentencia) ===
                    DrawHilo(ancla, maza, energia, time);

                    // === EL ARCO FANTASMA (3 posiciones hacia atrás por la
                    //     TANGENTE del movimiento — el rastro inmediato del
                    //     barrido, en el sentido CONTRARIO al vaivén) ===
                    Vector2 tang = new(MathF.Cos(p.Theta), -MathF.Sin(p.Theta));
                    if (p.Omega < 0f) tang *= -1f;
                    for (int g = 1; g <= 3; g++)
                    {
                        Vector2 ghost = maza - tang * (11f * g);
                        Quad(Glow, ghost, new Vector2(MazaR * 1.5f, MazaR * 1.5f),
                            0f, Tint(Oro, 0.14f * (4 - g)));
                    }
                }
                else
                {
                    // === EL CORTE: el rayo del hilo rompiéndose (StormLib) ===
                    int flick = StormLib.FlickTick(time, 15f);
                    if (age < PenduloJuicioProjectile.JuicioTicks + 14f &&
                        StormLib.IsLit(seed + 31, flick, 0.6f))
                    {
                        StormLib.MultiBolt(Main.spriteBatch, ancla, maza,
                            seed + 31, flick, 5f, Oro, HiloColor, OroCaliente,
                            alpha: 0.7f, amp: 10f);
                    }
                    // EL ANCLA deshilachada (se queda un rato tras el corte).
                    if (age < PenduloJuicioProjectile.JuicioTicks + 40f)
                        DrawAncla(ancla, time, seed, 0.6f);
                }

                if (!p.Suelto)
                    DrawAncla(ancla, time, seed, 1f);

                // === LA MAZA (con sus runas grabadas) ===
                DrawMaza(p, maza, energia, time, seed);

                Main.spriteBatch.End();

                // === 3. LA LUZ (dorada, entre el ancla y la maza) ===
                if (Main.netMode != NetmodeID.Server)
                {
                    float e = 0.6f + 0.4f * energia;
                    LumenLib.LightAlong(p.AnclaPublica, p.Projectile.Center,
                        HiloColor, 0.5f * e, 60f);
                }
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ==================================================================
        //  EL ANCLA — el punto del que cuelga la sentencia
        // ==================================================================

        private static void DrawAncla(Vector2 ancla, float time, int seed, float alpha)
        {
            // EL HALO pequeño (la punta clavada).
            LumenLib.Bloom(Main.spriteBatch, ancla, 18f, Oro, 0.5f * alpha, 2);
            // EL ANILLO de sujeción (girando lento).
            Quad(Ring, ancla, VFXCore.RingQuadSize(16f), time * 0.8f,
                Tint(Oro, 0.4f * alpha));
            // LA RUNA de clavado (una marca vertical + dos diagonales).
            CapsulaWorld(ancla + new Vector2(0f, -6f), ancla + new Vector2(0f, 6f),
                1.6f, Tint(OroCaliente, 0.5f * alpha));
            CapsulaWorld(ancla + new Vector2(-4f, -3f), ancla + new Vector2(4f, 3f),
                1.1f, Tint(OroCaliente, 0.35f * alpha));
            CapsulaWorld(ancla + new Vector2(4f, -3f), ancla + new Vector2(-4f, 3f),
                1.1f, Tint(OroCaliente, 0.35f * alpha));
        }

        // ==================================================================
        //  EL HILO — la línea de la sentencia (brilla con la energía)
        // ==================================================================

        private static void DrawHilo(Vector2 ancla, Vector2 maza, float energia, float time)
        {
            // El GROSOR crece con la energía (1.4 → 4.2 px).
            float grosor = 1.4f + 2.8f * energia;
            // El VAIVÉN sutil del hilo tensado (2 Hz, ±0.6 px).
            Vector2 mid = (ancla + maza) * 0.5f;
            Vector2 d = maza - ancla;
            float len = d.Length();
            if (len < 1f) return;
            Vector2 normal = new Vector2(-d.Y, d.X) / len;
            float vaiven = MathF.Sin(time * 2.1f) * 0.6f * (1f - energia * 0.5f);
            mid += normal * vaiven;

            // EL CUERPO del hilo (dorado, opacidad con energía).
            CapsulaWorld(ancla, mid, grosor, Tint(HiloColor, 0.35f + 0.35f * energia));
            CapsulaWorld(mid, maza, grosor, Tint(HiloColor, 0.35f + 0.35f * energia));
            // LA VENA blanca (el núcleo tenso).
            CapsulaWorld(ancla, maza, grosor * 0.35f, Tint(OroCaliente, 0.5f + 0.3f * energia));
        }

        // ==================================================================
        //  LA MAZA — la bola de runas grabadas
        // ==================================================================

        private static void DrawMaza(PenduloJuicioProjectile p, Vector2 maza,
            float energia, float time, int seed)
        {
            float rot = p.Projectile.rotation;

            // === EL CUERPO (GlowOrb — núcleo sólido dorado) ===
            Color cuerpo = Color.Lerp(Oro, OroCaliente, 0.35f + 0.3f * energia);
            Quad(Orb, maza, new Vector2(MazaR * 2f, MazaR * 2f), rot,
                Tint(cuerpo, 0.85f));
            // EL RIM brillante (más energía = más borde).
            Quad(Ring, maza, VFXCore.RingQuadSize(MazaR * 1.12f), -rot * 2f,
                Tint(OroCaliente, 0.4f + 0.4f * energia));

            // === LAS RUNAS GRABADAS (8 marcas radiales oscuras — giran con
            //     la maza; sobre el cuerpo sólido se leen como grabado) ===
            for (int k = 0; k < 8; k++)
            {
                float ang = k / 8f * MathHelper.TwoPi + rot;
                Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang));
                Vector2 a = maza + dir * (MazaR * 0.42f);
                Vector2 b = maza + dir * (MazaR * 0.80f);
                // La marca oscura (grabado) + su filo claro (el cincelado).
                Quad(Glow, (a + b) * 0.5f, new Vector2(5.5f, 2.2f), ang + MathHelper.PiOver2,
                    Tint(RunaOscura, 0.55f));
                Quad(Glow, b + dir * 1.2f, new Vector2(2.4f, 2.4f), 0f,
                    Tint(OroCaliente, 0.5f));
            }

            // === EL ARO DE RUNAS DE PIE (el estilo de la casa: 4 perlas
            //     orbitando el ecuador de la maza) ===
            for (int k = 0; k < 4; k++)
            {
                float ang = -time * 1.1f + k * MathHelper.PiOver2;
                Vector2 pos = maza + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.35f)
                    * (MazaR * 1.45f);
                Quad(Glow, pos, new Vector2(3.4f, 5.2f), ang,
                    Tint(OroCaliente, 0.55f));
            }

            // === EL BLOOM de la maza (la masa de luz alrededor) ===
            LumenLib.Bloom(Main.spriteBatch, maza, MazaR * 1.7f, Oro,
                0.35f + 0.3f * energia, 2);

            // === EL DESTELLO del fondo (cuando la maza pasa por abajo va a
            //     MÁXIMA velocidad — ahí el destello, no siempre) ===
            float cercaDelFondo = 1f - MathHelper.Clamp(MathF.Abs(p.Theta) / 0.9f, 0f, 1f);
            if (cercaDelFondo > 0.25f)
            {
                LumenLib.Flare(Main.spriteBatch, maza, 20f * cercaDelFondo, OroCaliente,
                    0.5f * cercaDelFondo, time * 1.2f);
            }
        }

        // ==================================================================
        //  LA ESTELA — el rastro del arco (Ribbon Comet dorado)
        // ==================================================================

        private static void DrawEstela(PenduloJuicioProjectile p, float time, int seed)
        {
            Vector2[] camino = EstelaLib.Track(p.Projectile.whoAmI, 16).Points();
            if (camino == null || camino.Length < 3) return;

            Vector2[] pantalla = new Vector2[camino.Length];
            for (int i = 0; i < camino.Length; i++)
                pantalla[i] = camino[i] - Main.screenPosition;

            BeginAdditive();
            EstelaLib.Ribbon(Main.spriteBatch, pantalla, 9f, EstelaProfile.Comet,
                Oro, 0.40f, seed + 7, time, head: false);
            Main.spriteBatch.End();
        }

        // ==================================================================
        //  LOS HELPERS DE PINTADO (los de la casa)
        // ==================================================================

        private static Asset<Texture2D> _glow;
        private static Asset<Texture2D> _orb;
        private static Asset<Texture2D> _ring;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        private static Texture2D Orb =>
            (_orb ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/GlowOrb")).Value;

        private static Texture2D Ring =>
            (_ring ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/Ring")).Value;

        /// <summary>Cápsula de luz entre dos puntos de PANTALLA.</summary>
        private static void CapsulaWorld(Vector2 a, Vector2 b, float width, Color tint)
        {
            Vector2 mid = (a + b) * 0.5f;
            Vector2 d = b - a;
            float len = d.Length();
            if (len < 0.1f) return;
            float r = MathF.Atan2(d.Y, d.X);
            Quad(Glow, mid, new Vector2(len + width, width * 1.9f), r, tint);
        }

        /// <summary>Abre el lote ADITIVO de la casa (Immediate).</summary>
        private static void BeginAdditive()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Un quad de luz centrado (el pincel universal).</summary>
        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tex == null || tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>Tinte PREMULTIPLICADO de la casa (v6.25 — RGB×f).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(
                (byte)(int)(c.R * f), (byte)(int)(c.G * f), (byte)(int)(c.B * f),
                (byte)(int)(255f * f));
        }
    }
}
