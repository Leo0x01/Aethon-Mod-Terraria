using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Effects.Bruma;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// CoroEspectralRenderer — v6.26 — EL BASTÓN DEL CORO ESPECTRAL.
    ///
    /// EL CORO dibujado 100% por código:
    ///
    ///   · LA BRUMA ESPECTRAL (AlphaBlend PRIMERO): una niebla dorada
    ///     tenue (BrumaFX.Cloud) alrededor del ancla — el aire del coro.
    ///   · LAS ÓRBITAS: 6 elipses fantasma finísimas (VFXCore.Ring con
    ///     squash vertical + alpha 0.06) — las pista por donde cantan.
    ///   · LAS NOTAS: FIGURAS DE NOTA MUSICAL dibujadas por código:
    ///     CABEZA circular (GlowOrb pequeño), MÁSTIL (cápsula vertical) y
    ///     BANDERA ondulante (3 quads curvándose con sin(t·3) — la bandera
    ///     VIVE al son del coro). Cada nota con su COLOR DE ESCALA (dorada,
    ///     ámbar, bronce, cobre, óxido, ceniza) + doble pasada fantasmal
    ///     (cuerpo aditivo + halo blando).
    ///   · EL CANTO: el anillo de la nota que acaba de emitir (OndaLib.
    ///     Pulse expandiéndose a 260 px, color de la nota) + EL ECO: un
    ///     segundo anillo RETRASADO (progreso −8%) y desvanecido (×0.35)
    ///     que persigue al primero — cada anillo deja su eco tenue.
    ///   · EL DIRECTOR: una luz pequeña en el ancla (LumenLib.Bloom) que
    ///     late al ritmo del ciclo (48 ticks).
    ///
    /// CONTRATO DE BATCH: Draw() exige el SpriteBatch CERRADO y lo deja
    /// CERRADO (contrato v6.10). MASA en AlphaBlend PRIMERO, brillos en
    /// Additive.
    /// </summary>
    public static class CoroEspectralRenderer
    {
        // ==================================================================
        //  LAS PALETAS (la escala dorada — el coro de fantasmas)
        // ==================================================================

        private static readonly Color BrumaColor = new(120, 96, 60);
        private static readonly Color DirectorColor = new(255, 232, 170);

        // ==================================================================
        //  EL PUNTO DE ENTRADA
        // ==================================================================

        /// <summary>Dibuja el coro completo. Batch CERRADO → CERRADO.</summary>
        public static void Draw(CoroEspectralProjectile p, float age, int seed)
        {
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 ancla = p.Projectile.Center - Main.screenPosition;

                // === 1. LA BRUMA ESPECTRAL (AlphaBlend PRIMERO) ===
                BrumaFX.BeginMass();
                BrumaFX.Cloud(ancla, 70f, BrumaColor, seed + 11, time * 0.18f,
                    puffs: 5, alpha: 0.10f);
                Main.spriteBatch.End();

                // === 2. EL CORO (Additive: órbitas + notas + anillos) ===
                BeginAdditive();

                DrawOrbitas(p, ancla, time);
                DrawNotas(p, time, seed);
                DrawAnillos(p, time);
                DrawDirector(ancla, age, time);

                Main.spriteBatch.End();

                // === 3. LA LUZ (tenue, dorada) ===
                if (Main.netMode != NetmodeID.Server)
                    Lighting.AddLight(p.Projectile.Center, 0.34f, 0.27f, 0.14f);
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ==================================================================
        //  LAS ÓRBITAS — las pistas del coro
        // ==================================================================

        private static void DrawOrbitas(CoroEspectralProjectile p, Vector2 ancla, float time)
        {
            for (int i = 0; i < CoroEspectralProjectile.Notas; i++)
            {
                // La elipse de la nota i: radio propio, aplastada 0.55,
                // desplazada por su altura — el anillo girando MUY lento.
                Vector2 size = VFXCore.RingQuadSize(p.NotaRadio(i));
                size.Y *= 0.55f;
                Vector2 pos = ancla + new Vector2(0f, p.NotaAltura(i) * 0.4f);
                float rot = time * 0.05f * ((i & 1) == 0 ? 1f : -1f);
                Color c = CoroEspectralProjectile.ColorDe(i);
                Quad(Ring, pos, size, rot, Tint(c, 0.08f));
            }
        }

        // ==================================================================
        //  LAS NOTAS — figuras musicales dibujadas por código
        // ==================================================================

        private static void DrawNotas(CoroEspectralProjectile p, float time, int seed)
        {
            for (int i = 0; i < CoroEspectralProjectile.Notas; i++)
            {
                float brillo = p.NotaBrillo(i);
                if (brillo <= 0.02f) continue;   // ya se apagó

                Color c = CoroEspectralProjectile.ColorDe(i);
                Color blanca = Color.Lerp(c, new Color(255, 250, 235), 0.5f);

                // LA NOTA oscila al cantar (las que cantan ÚLTIMO se mecen
                // más — el vaivén del solista).
                float fase = p.NotaFase(i);
                Vector2 pos = p.NotaPos(i) - Main.screenPosition;
                float mecer = MathF.Sin(time * 2.2f + i * 1.3f) * 2.2f;
                pos += new Vector2(mecer * 0.4f, MathF.Abs(mecer) * 0.6f);

                // === LA CABEZA (circular, con el color de la escala) ===
                Quad(Orb, pos, new Vector2(11f, 11f), fase * 0.3f, Tint(c, 0.75f * brillo));
                Quad(Glow, pos, new Vector2(16f, 16f), 0f, Tint(c, 0.25f * brillo));

                // === EL MÁSTI (cápsula vertical hacia arriba, 15 px) ===
                Vector2 mastilTop = pos + new Vector2(3.2f, -15f);
                Vector2 mastilBot = pos + new Vector2(2.0f, -3f);
                CapsulaWorld(mastilBot, mastilTop, 1.7f, Tint(blanca, 0.65f * brillo));

                // === LA BANDERA ONDULANTE (3 quads curvándose a la derecha
                //     desde la cima del mástil — VIVE con sin(t·3+nota)) ===
                Vector2 curva = mastilTop;
                for (int k = 0; k < 3; k++)
                {
                    float onda = MathF.Sin(time * 3f + i * 2.1f + k * 1.2f) * 1.6f;
                    Vector2 siguiente = curva + new Vector2(3.4f - k * 0.7f, 2.6f + onda * 0.4f);
                    CapsulaWorld(curva, siguiente, 1.4f,
                        Tint(c, (0.55f - k * 0.12f) * brillo));
                    curva = siguiente;
                }

                // === EL HALO FANTASMAL (la doble pasada espectral) ===
                LumenLib.Bloom(Main.spriteBatch, pos, 14f, c, 0.30f * brillo, 2);
            }
        }

        // ==================================================================
        //  LOS ANILLOS DEL CANTO — y su ECO
        // ==================================================================

        private static void DrawAnillos(CoroEspectralProjectile p, float time)
        {
            foreach (CoroEspectralProjectile.Anillo a in p.AnillosLista)
            {
                float prog = a.Edad / CoroEspectralProjectile.AnilloTicks;
                float maxR = a.Eco
                    ? CoroEspectralProjectile.RadioAnillo * 0.5f   // el eco: corto
                    : CoroEspectralProjectile.RadioAnillo;

                // === EL ANILLO PRINCIPAL (OndaLib.Pulse — el frente) ===
                OndaLib.Pulse(Main.spriteBatch, a.Origen - Main.screenPosition,
                    prog, maxR, a.Color, a.Eco ? 0.35f : 0.8f, a.Semilla);

                // === EL ECO TENUE (retrasado y desvanecido — persigue al
                //     anillo como una cola de sonido) ===
                if (!a.Eco && prog > 0.08f)
                {
                    OndaLib.Pulse(Main.spriteBatch, a.Origen - Main.screenPosition,
                        prog - 0.08f, maxR * 0.94f, a.Color, 0.28f, a.Semilla + 7);
                }
            }
        }

        // ==================================================================
        //  EL DIRECTOR — la luz del ancla (late al ritmo del coro)
        // ==================================================================

        private static void DrawDirector(Vector2 ancla, float age, float time)
        {
            // EL LATIDO al ciclo del coro (48 ticks: el compás).
            float compas = (age % CoroEspectralProjectile.CicloTicks)
                / CoroEspectralProjectile.CicloTicks;
            float latido = 0.75f + 0.25f * MathF.Sin(compas * MathHelper.TwoPi);
            LumenLib.Bloom(Main.spriteBatch, ancla, 22f * latido, DirectorColor,
                0.45f * latido, 2);
            // EL ANILLO del compás (pequeño, girando — el metrónomo).
            Quad(Ring, ancla, VFXCore.RingQuadSize(15f * latido),
                time * 1.4f, Tint(DirectorColor, 0.35f * latido));
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
