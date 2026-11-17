using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// v6.149 — EL RENDERER DEL AGUJERO RÚNICO PURO (la letra: «dale otro
    /// estilo al agujero negro runico puro»). El Puro ya NO viste el
    /// renderer del Cósmico (el vórtice rojo-naranja con bandas): su
    /// identidad nueva es EL ANILLO DE EINSTEIN RÚNICO — el agujero negro
    /// de la física de verdad visto casi de canto:
    ///
    ///   · EL HORIZONTE — un disco negro ABSOLUTO (BlackDisk) rodeado de
    ///     un filo blanco fino: la luz del anillo que se curva sobre el
    ///     borde (el «photon ring» de las fotos del M87).
    ///   · EL DISCO DE ACRECIÓN — UNA elipse MUY ACHATADA (casi de canto,
    ///     como Gargantua en Interstellar): blanca incandescente en el
    ///     borde interior, fundiéndose a dorado y muriendo en un ámbar
    ///     tenue hacia afuera. Fina, elegante, SIN bandas de shader.
    ///   · EL ARCO SUPERIOR — el anillo se curva SOBRE el horizonte (la
    ///     lente gravitatoria dobla la luz del disco por encima del
    ///     agujero): el mismo arco achatado, desplazado hacia arriba y
    ///     detrás, abrazando el núcleo negro.
    ///   · LAS RUNAS — ocho glifos dorados orbitando EN el propio borde
    ///     del disco (tallados en la acreción): nacen y mueren con el
    ///     flujo, sin polvo — dibujadas como cápsulas finas alineadas a
    ///     la tangente de la elipse.
    ///   · EL ALIENTO VIOLETA — el aura de lente más suave de la casa
    ///     (violeta profundo, apenas 10 %): el vacío alrededor se sabe
    ///     curvado.
    ///
    /// CONTRATO DE LOTE: el batch llega CERRADO y queda CERRADO (el mismo
    /// contrato del renderer del Cósmico — lo llama el PreDraw del Puro y
    /// el pase posterior a la lente).
    /// </summary>
    public static class AgujeroPuroRenderer
    {
        /// <summary>Radio del horizonte en px a escala 1 (mismo tamaño físico
        /// que el Cósmico: el arma, el daño y la lente no cambian).</summary>
        public const float SpherePx = 50f;

        // === LA GEOMETRÍA DEL DISCO (casi de canto — Gargantua) ===
        private const float DiscoA = 2.30f;   // semieje mayor (×R): ancho
        private const float DiscoB = 0.52f;   // semieje menor (×R): FINO
        private const float DiscoTilt = -0.10f; // inclinación sutil

        // === EL ARCO SUPERIOR (la lente dobla el disco sobre el agujero) ===
        private const float ArcoA = 1.55f;
        private const float ArcoB = 1.18f;
        private const float ArcoSube = 0.42f;  // ×R hacia arriba

        // === LAS RUNAS (ocho glifos tallados en el borde del disco) ===
        private const int RunaCount = 8;
        private const float RunaOrbit = 0.16f; // rad/s — la acreción fluye

        // === LA PALETA (blanco incandescente → oro → ámbar; lente violeta) ===
        private static readonly Color FiloBlanco = new(255, 250, 238);
        private static readonly Color AcrecionInt = new(255, 244, 214);
        private static readonly Color AcrecionMid = new(255, 196, 92);
        private static readonly Color AcrecionExt = new(170, 110, 40);
        private static readonly Color RunaOro = new(255, 226, 150);
        private static readonly Color LenteViol = new(96, 52, 168);

        // === PINCELES (los procedurales de la casa) ===
        private static Asset<Texture2D> _glow;
        private static Asset<Texture2D> _ring;
        private static Asset<Texture2D> _disk;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;
        private static Texture2D Ring =>
            (_ring ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring")).Value;
        private static Texture2D Disk =>
            (_disk ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/BlackDisk")).Value;

        // ==================================================================
        //  EL DRAW
        // ==================================================================
        public static void Draw(Vector2 center, float scale, float time, int seed)
        {
            float r = SpherePx * Math.Max(scale, 0.02f);
            if (r < 2f) return;

            try
            {
                float respira = 1f + 0.02f * MathF.Sin(time * 0.9f);

                // ============ 0. EL ALIENTO DE LENTE (violeta, suave) ============
                VFXCore.AbrirAdditive();
                Quad(Glow, center, new Vector2(6.4f * r, 6.4f * r), 0f,
                    Tint(LenteViol, 0.10f + 0.03f * MathF.Sin(time * 1.3f)));
                Main.spriteBatch.End();

                // ============ 1. EL DISCO TRASERO (el arco que nace detrás) ============
                // La parte del anillo que vive DETRÁS del horizonte: se dibuja
                // antes del disco negro para que el núcleo la tape a medias —
                // la profundidad del casi-de-canto.
                VFXCore.AbrirAdditive();
                try { DibujarAcrecion(center, r * respira, time, seed, frente: false); }
                catch { }

                // ============ 2. EL HORIZONTE (negro absoluto + filo de fotones) ============
                Main.spriteBatch.End();
                VFXCore.AbrirAlpha();
                Quad(Disk, center, new Vector2(2.06f * r, 2.06f * r), 0f, Color.White);
                Main.spriteBatch.End();
                VFXCore.AbrirAdditive();
                // EL FILO — el aro de fotones: un Ring blanco fino pegado al
                // horizonte, el borde donde la luz se rinde.
                AnilloFino(center, 1.005f * r, time * 0.10f, Tint(FiloBlanco, 0.55f + 0.10f * MathF.Sin(time * 2.0f)));

                // ============ 3. EL DISCO DELANTERO (la acreción que cruza delante) ============
                try { DibujarAcrecion(center, r * respira, time, seed, frente: true); }
                catch { }

                // ============ 4. EL ARCO SUPERIOR (la lente dobla el disco) ============
                try { DibujarArcoSuperior(center, r * respira, time); }
                catch { }

                // ============ 5. LAS RUNAS TALLADAS EN EL BORDE ============
                try { DibujarRunas(center, r * respira, time, seed); }
                catch { }
            }
            finally
            {
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        // ==================================================================
        //  EL DISCO DE ACRECIÓN — cápsulas finas sobre la elipse, con el
        //  gradiente térmico (blanco dentro → oro → ámbar fuera) y la
        //  MITAD trasera/delantera para la profundidad del horizonte.
        // ==================================================================
        private static void DibujarAcrecion(Vector2 c, float r, float time, int seed, bool frente)
        {
            int pasos = 26;
            float t0 = frente ? 0f : MathHelper.Pi;
            float span = MathHelper.Pi;
            float brilloBase = frente ? 1.0f : 0.62f;

            for (int i = 0; i < pasos; i++)
            {
                float u = i / (float)(pasos - 1);            // 0..1 a lo largo de la mitad
                float t = t0 + u * span + time * RunaOrbit * 0.35f; // el disco fluye lento

                // EL PUNTO sobre la elipse (casi de canto)
                float ca = MathF.Cos(t), sa = MathF.Sin(t);
                float x = c.X + (ca * DiscoA * r) * MathF.Cos(DiscoTilt) - (sa * DiscoB * r) * MathF.Sin(DiscoTilt);
                float y = c.Y + (ca * DiscoA * r) * MathF.Sin(DiscoTilt) + (sa * DiscoB * r) * MathF.Cos(DiscoTilt);
                Vector2 pos = new(x, y);

                // LA TANGENTE (para orientar la cápsula)
                float tx = -MathF.Sin(t) * DiscoA, ty = MathF.Cos(t) * DiscoB;
                Vector2 tang = new(
                    tx * MathF.Cos(DiscoTilt) - ty * MathF.Sin(DiscoTilt),
                    tx * MathF.Sin(DiscoTilt) + ty * MathF.Cos(DiscoTilt));
                float rot = tang.ToRotation();

                // EL GRADIENTE TÉRMICO — el borde INTERIOR (|sin| bajo = cerca
                // del eje mayor = pegado al horizonte) ARDE BLANCO; hacia las
                // puntas de la elipse el disco se enfría a ámbar.
                float frio = MathF.Abs(sa);                     // 0 = pegado al horizonte
                Color termico = Color.Lerp(AcrecionInt, AcrecionExt, frio * frio);
                // el pico blanco en el borde interior directo
                if (frio < 0.35f && frente)
                    termico = Color.Lerp(FiloBlanco, termico, frio / 0.35f);

                float largo = (2f * MathF.PI * ((DiscoA + DiscoB) * 0.5f) * r) / pasos * 1.25f;
                float grueso = (7f + 5f * frio) * brilloBase * (frente ? 1f : 0.85f);
                float alfa = (0.42f + 0.30f * (1f - frio)) * brilloBase;

                Capsule(pos, largo, grueso, rot, Tint(termico, alfa));
            }
        }

        // ==================================================================
        //  EL ARCO SUPERIOR — la imagen lensada del disco: el mismo anillo,
        //  más chico y desplazado hacia arriba, curvándose SOBRE el
        //  horizonte. Es lo que hace que el agujero «abrace» su anillo.
        // ==================================================================
        private static void DibujarArcoSuperior(Vector2 c, float r, float time)
        {
            int pasos = 18;
            for (int i = 0; i < pasos; i++)
            {
                float u = i / (float)(pasos - 1);
                // la mitad SUPERIOR de una elipse vertical (π..2π en sin<0)
                float t = MathHelper.Pi + u * MathHelper.Pi + time * RunaOrbit * 0.22f;
                float ca = MathF.Cos(t), sa = MathF.Sin(t);
                Vector2 pos = c + new Vector2(ca * ArcoA * r, sa * ArcoB * r - ArcoSube * r);

                float tx = -MathF.Sin(t) * ArcoA, ty = MathF.Cos(t) * ArcoB;
                float rot = new Vector2(tx, ty).ToRotation();

                // el tope del arco brilla más (la luz lensada se concentra arriba)
                float cima = 1f - MathF.Abs(sa);
                Color cArco = Color.Lerp(AcrecionMid, FiloBlanco, cima * 0.7f);
                float largo = (2f * MathF.PI * ((ArcoA + ArcoB) * 0.5f) * r) / pasos * 1.2f;
                Capsule(pos, largo, 5f + 3f * cima, rot, Tint(cArco, 0.20f + 0.22f * cima));
            }
        }

        // ==================================================================
        //  LAS RUNAS — ocho glifos orbitando EN el borde EXTERIOR del disco,
        //  alineados a la tangente: cápsulas doradas finas con punta
        //  brillante, naciendo y muriendo con el flujo (SIN polvo de Dust:
        /// dibujo puro de la casa).
        // ==================================================================
        private static void DibujarRunas(Vector2 c, float r, float time, int seed)
        {
            for (int k = 0; k < RunaCount; k++)
            {
                float fase = k / (float)RunaCount;
                float t = time * RunaOrbit + fase * MathHelper.TwoPi;

                // sobre la MISMA elipse del disco, un pelo más afuera
                float ca = MathF.Cos(t), sa = MathF.Sin(t);
                float factor = 1.10f;
                float x = c.X + (ca * DiscoA * factor * r) * MathF.Cos(DiscoTilt) - (sa * DiscoB * factor * r) * MathF.Sin(DiscoTilt);
                float y = c.Y + (ca * DiscoA * factor * r) * MathF.Sin(DiscoTilt) + (sa * DiscoB * factor * r) * MathF.Cos(DiscoTilt);
                Vector2 pos = new(x, y);

                float tx = -MathF.Sin(t) * DiscoA, ty = MathF.Cos(t) * DiscoB;
                float rot = new Vector2(tx, ty).ToRotation();

                // EL PULSO DE NACIMIENTO — cada runa respira con su fase
                float aliento = 0.5f + 0.5f * MathF.Sin(time * 1.6f + k * 2.1f);
                // las runas del lado trasero se atenúan (profundidad)
                float profundo = sa > 0f ? 0.45f : 1f;

                // el glifo: trazo doble (cuerpo + punta brillante)
                Capsule(pos, 10f + 4f * aliento, 3.2f, rot, Tint(RunaOro, (0.35f + 0.40f * aliento) * profundo));
                Capsule(pos + tang(rot) * (5f + 2f * aliento), 4.5f, 2.4f, rot,
                    Tint(FiloBlanco, (0.30f + 0.35f * aliento) * profundo));
            }
        }

        private static Vector2 tang(float rot) => new(MathF.Cos(rot), MathF.Sin(rot));

        // ==================================================================
        //  HELPERS (el dialecto del renderer del Cósmico)
        // ==================================================================
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        private static void Capsule(Vector2 mid, float len, float width, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Texture2D tex = Glow;
            Main.spriteBatch.Draw(tex, mid, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                new Vector2(len + width, width * 1.9f) / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        private static void AnilloFino(Vector2 pos, float radio, float rot, Color tint)
        {
            if (tint.A == 0) return;
            float s = radio * 2.174f;
            Quad(Ring, pos, new Vector2(s, s), rot, tint);
        }
    }
}
