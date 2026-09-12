using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// StylizedVoidRenderer — v6.03 — EL AGUJERO NEGRO DE LA REFERENCIA.
    ///
    /// Render ESTILIZADO por capas que reproduce la GEOMETRÍA MEDIDA por
    /// píxel en las dos imágenes de referencia del usuario (elipse plana
    /// brillante + centro negro pequeño), SIN la corona (que es hoy un
    /// cosmético del jugador):
    ///
    /// Estructura medida (recorte del agujero de la referencia 1):
    ///   - UNA LENTE BRILLANTE: elipse plana de aspecto ~2.4:1 — un CUERPO
    ///     LLENO de fucsia con banda de borde CONTINUA (la referencia pica
    ///     en magenta puro en los extremos; NO es una puntura de puntos).
    ///   - El VACÍO central: negro absoluto con penumbra (hueco medido
    ///     ~40×32 px sobre 148 de lente — redondeado, no plano).
    ///   - Un ARO BLANCO-CÁLIDO CONTINUO abrazando la silueta del vacío
    ///     (el anillo brillante que lo define).
    ///   - Una MEDIA LUNA inferior: el destello frontal intenso que cruza
    ///     justo por debajo del negro (pico medido 255,26,255).
    ///   - Encima: glow + rayos sutiles (la corona de runas/perlas fue
    ///     RETIRADA: hoy son cosméticos) y relámpagos en los flancos.
    ///
    /// Capas del render (de atrás a delante):
    ///   1. Halo púrpura-carmesí compacto que respira (la lente domina).
    ///   2. RELLENO de la lente: tres glows elípticos (fucsia→caliente→brasa).
    ///   3. BANDA de la lente: la textura Ring ESTIRADA a la elipse — un
    ///      aro CONTINUO y grueso (cuerpo fucsia + filo caliente) — más un
    ///      TEMBLOR de materia fluyendo encima (grumos sutiles, no estructura).
    ///   4. EL VACÍO: 4 elipses negras apiladas (2 GlowOrb de núcleo sólido
    ///      + 2 SoftGlow de penumbra) — negro absoluto, hueco redondeado.
    ///   5. ARO BLANCO-CÁLIDO CONTINUO sobre la silueta del vacío.
    ///   6. MEDIA LUNA inferior + temblor frontal de materia.
    ///   7. ONCE RAYOS sutiles en el hemisferio superior (los del portal).
    ///   8. DOS RELÁMPAGOS de los flancos de la lente (zigzag determinista
    ///      que se regenera — la tormenta estática del vacío).
    ///
    /// Todo se construye con la biblioteca (VFXCore + BoltRenderer +
    /// VFXPalettes.VoidQueen) — cero texturas externas, cero shader:
    /// el look de la referencia es luz PURA por capas.
    /// </summary>
    public static class StylizedVoidRenderer
    {
        /// <summary>Semieje mayor de la lente (× R): la lente DOMINA.</summary>
        private const float LensA = 1.92f;

        /// <summary>Semieje menor (aspecto ~2:1 medido: 1.82 en el recorte).</summary>
        private const float LensB = 0.95f;

        /// <summary>Semieje mayor del trazo interior del gradiente (× R):
        /// el blanco-cálido cae a 0.47× el semieje, como en la referencia.</summary>
        private const float RimA = 0.90f;

        /// <summary>Semieje menor del trazo interior del gradiente.</summary>
        private const float RimB = 0.45f;

        /// <summary>Radio del horizonte en px (misma fórmula del proyectil).</summary>
        public static float GetHorizonPx(Projectile p)
        {
            return 0.3f * p.width * Math.Max(p.scale, 0.08f);
        }

        /// <summary>
        /// Dibuja el agujero completo. Contrato de batch idéntico al render
        /// viejo: si <paramref name="endActiveBatch"/> es true se cierra el
        /// batch activo antes; al terminar el batch queda CERRADO (el
        /// llamador lo restaura — PreDraw o el pase de la lente).
        /// </summary>
        public static void Draw(Projectile p, bool endActiveBatch)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;

            try
            {
                Vector2 center = p.Center;
                float R = GetHorizonPx(p);
                if (R < 2f) return;

                float time = Main.GlobalTimeWrappedHourly;
                // Respiración global (95%..105%): inestabilidad dimensional.
                float breathe = VFXCore.Breathe(time, 0.9f, 0f, 0.045f);

                // ==============================================================
                //  PASO A — halo + RELLENO + BANDA CONTINUA de la lente
                //  (blending aditivo: la luz de fondo del vacío)
                // ==============================================================
                VFXCore.Begin();

                // 1. Halo púrpura-carmesí compacto (la LENTE domina la
                //    figura, como en la referencia) + aura fucsia.
                VFXCore.Quad(center, new Color(26, 11, 46, 46) * breathe,
                    new Vector2(4.4f * R, 4.4f * R));
                VFXCore.Quad(center, new Color(61, 17, 87, 34) * breathe,
                    new Vector2(3.2f * R, 3.2f * R));
                VFXCore.Quad(center, VFXPalettes.VoidQueen.Disk * (0.08f * breathe),
                    new Vector2(2.8f * R, 2.8f * R));

                // 2. RELLENO SÓLIDO de la lente (GlowOrb — núcleo macizo a
                //    diferencia de SoftGlow): la referencia es una lente
                //    LLENA y SATURADA (23k px brillantes en el recorte), no
                //    un aro tenue. Tres cuerpos anidados: fucsia → caliente
                //    → brasa clara hacia el centro.
                VFXCore.Quad(center, VFXPalettes.VoidQueen.Disk * (0.80f * breathe),
                    new Vector2(2f * LensA * R, 2f * LensB * R), VFXCore.GlowOrb);
                VFXCore.Quad(center, VFXPalettes.VoidQueen.Hot * (0.60f * breathe),
                    new Vector2(1.61f * LensA * R, 1.61f * LensB * R), VFXCore.GlowOrb);
                VFXCore.Quad(center, VFXPalettes.VoidQueen.Pale * (0.45f * breathe),
                    new Vector2(1.15f * LensA * R, 1.15f * LensB * R), VFXCore.GlowOrb);

                // 3. EL GRADIENTE CONTINUO: DIEZ anillos de Ring ESTIRADOS
                //    cuyos trazos (0.90..0.98 del semieje) SE SOLAPAN en
                //    progresión geométrica (ratio ~1.089) — una banda
                //    CONTINUA del aro blanco-cálido (0.47× el semieje, como
                //    la referencia) al borde saturado (1.0×), con el color
                //    interpolado blanco→caliente→fucsia. Nada de huecos.
                Color whiteHot = Color.Lerp(VFXPalettes.VoidQueen.Pale,
                                             VFXPalettes.VoidQueen.Spark, 0.55f);
                const int Steps = 10;
                for (int s = 0; s < Steps; s++)
                {
                    float f = s / (float)(Steps - 1);            // 0 aro → 1 borde
                    float semiA = MathHelper.Lerp(RimA, LensA, f);
                    float semiB = MathHelper.Lerp(RimB, LensB, f);
                    Color col = f < 0.35f
                        ? Color.Lerp(whiteHot, VFXPalettes.VoidQueen.Hot, f / 0.35f)
                        : Color.Lerp(VFXPalettes.VoidQueen.Hot, VFXPalettes.VoidQueen.Disk,
                                     (f - 0.35f) / 0.65f);
                    float intensity = MathHelper.Lerp(2.0f, 1.55f, f);
                    DrawBand(center, R, semiA, semiB, col, intensity * breathe);
                }

                // 3b. TEMBLOR de materia fluyendo (mitad trasera, tenue).
                ComputeShimmer(center, R, LensA, LensB, time, front: false, alpha: 0.28f);

                VFXCore.FlushAdditive(null, endActiveBatch);

                // ==============================================================
                //  PASO B — EL VACÍO (blending ALPHA: la nada come luz, no
                //  la suma). Cuatro elipses negras apiladas (dos GlowOrb de
                //  NÚCLEO SÓLIDO + dos SoftGlow de penumbra): negro ABSOLUTO
                //  al centro y hueco oscuro redondeado hasta ~0.8R.
                // ==============================================================
                Texture2D orb = VFXCore.GlowOrb;
                Texture2D glow = VFXCore.SoftGlow;
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Vector2 screen = Main.screenPosition;
                Vector2 cPos = center - screen;
                // EL VACÍO — proporciones de la referencia: zona oscura solo
                // hasta ±0.23× el semieje (±0.44R) y negro PURO diminuto
                // (~5% de la lente). Penumbra → núcleo → centro negro.
                Main.spriteBatch.Draw(glow, cPos, null, new Color(0, 0, 0, 232), 0f,
                    glow.Size() * 0.5f, new Vector2(1.05f * R, 0.75f * R) / glow.Size(), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(glow, cPos, null, new Color(0, 0, 0, 245), 0f,
                    glow.Size() * 0.5f, new Vector2(0.80f * R, 0.56f * R) / glow.Size(), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(orb, cPos, null, new Color(0, 0, 0, 255), 0f,
                    orb.Size() * 0.5f, new Vector2(0.55f * R, 0.40f * R) / orb.Size(), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(orb, cPos, null, new Color(0, 0, 0, 255), 0f,
                    orb.Size() * 0.5f, new Vector2(0.38f * R, 0.28f * R) / orb.Size(), SpriteEffects.None, 0f);

                Main.spriteBatch.End();

                // ==============================================================
                //  PASO C — ARO DEL VACÍO + MEDIA LUNA + materia + rayos
                //  (aditivo: la luz que cruza POR DELANTE de la nada)
                // ==============================================================
                VFXCore.Begin();

                // 5. CRUCE FRONTAL: los TRES anillos interiores del gradiente
                //    (los blanco-cálidos) redibujados POR DELANTE de la nada —
                //    así el gradiente CRUZA el vacío por abajo, como en la
                //    referencia (la banda frontal mide 255,26,255 ahí).
                for (int s = 0; s < 3; s++)
                {
                    float f = s / 9f;
                    float semiA = MathHelper.Lerp(RimA, LensA, f);
                    float semiB = MathHelper.Lerp(RimB, LensB, f);
                    Color col = Color.Lerp(whiteHot, VFXPalettes.VoidQueen.Hot, f / 0.35f);
                    DrawBand(center, R, semiA, semiB, col, 1.5f * breathe);
                }

                // 6. MEDIA LUNA inferior: el destello sólido del tercio
                //    inferior (la referencia lo pica en 255,26,255) —
                //    PROPORCIONAL: abraza el vacío pequeño.
                VFXCore.Quad(center + new Vector2(0f, 0.26f * R),
                    VFXPalettes.VoidQueen.Hot * (0.85f * breathe),
                    new Vector2(1.15f * R, 0.46f * R));
                VFXCore.Quad(center + new Vector2(0f, 0.36f * R),
                    Color.Lerp(VFXPalettes.VoidQueen.Pale, VFXPalettes.VoidQueen.Spark, 0.5f) * (0.70f * breathe),
                    new Vector2(0.75f * R, 0.30f * R));

                // 6b. TEMBLOR de materia fluyendo (mitad frontal).
                ComputeShimmer(center, R, LensA, LensB, time, front: true, alpha: 0.36f);

                // 7. ONCE RAYOS sutiles del hemisferio superior (los del portal).
                ComputeRays(center, R, time);

                // 8. LOS DOS RELÁMPAGOS de los flancos de la lente.
                ComputeBolts(p, center, R, time);

                VFXCore.FlushAdditive(null, false);
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error.
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ------------------------------------------------------------------
        //  LA BANDA CONTINUA — la textura Ring estirada a la elipse
        // ------------------------------------------------------------------

        /// <summary>
        /// Un aro CONTINUO sobre una elipse: la textura Ring (trazo a ~0.92
        /// del semiancho) dibujada ESTIRADA (escala no uniforme) cae justo
        /// en la elipse pedida — banda lisa y densa, nada de puntos.
        /// </summary>
        /// <param name="center">Centro (mundo).</param>
        /// <param name="R">Radio del horizonte.</param>
        /// <param name="semiMajor">Semieje mayor del aro (× R).</param>
        /// <param name="semiMinor">Semieje menor del aro (× R).</param>
        /// <param name="color">Color de la banda.</param>
        /// <param name="intensity">Multiplicador de brillo (puede ser &gt;1).</param>
        private static void DrawBand(Vector2 center, float R, float semiMajor, float semiMinor,
            Color color, float intensity)
        {
            // El trazo visible de Ring cae a ~0.92 del semiancho: para que
            // caiga en el semieje pedido, el sprite mide ~2.174× el semieje.
            Vector2 size = new Vector2(semiMajor * R * 2.174f, semiMinor * R * 2.174f);
            VFXCore.Quad(center, color * intensity, size, VFXCore.Ring);
        }

        // ------------------------------------------------------------------
        //  EL TEMBLOR — materia fluyendo SOBRE la banda (brillo, no estructura)
        // ------------------------------------------------------------------

        /// <summary>
        /// Temblor de materia viva sobre la banda continua: grumos tenues
        /// estirados a lo largo de la tangente de la elipse, orbitando en
        /// sentido ANTIHORARIO. Es un brillo que FLUYE (alpha bajo: la
        /// estructura la pone la banda continua, no esto).
        /// </summary>
        private static void ComputeShimmer(Vector2 center, float R, float semiMajor, float semiMinor,
            float time, bool front, float alpha)
        {
            float a = semiMajor * R;
            float b = semiMinor * R;
            int count = 22;
            float rotSpeed = 0.34f;   // rad/s, antihorario visual
            float clumpWidth = 0.42f * R;
            int seed = 0x77E2;

            float perim = MathHelper.Pi * (3f * (a + b) -
                (float)Math.Sqrt((3f * a + b) * (a + 3f * b)));
            float clumpLen = perim / count * 1.7f;

            for (int i = 0; i < count; i++)
            {
                // Órbita ANTIHORARIA (en pantalla el ángulo DECRECE).
                float t = i / (float)count * MathHelper.TwoPi - time * rotSpeed;

                bool isFront = Math.Sin(t) > 0f;
                if (isFront != front) continue;

                Vector2 pos = VFXCore.Ellipse(center, a, b, 0f, t);
                Vector2 tang = new Vector2(-a * (float)Math.Sin(t), b * (float)Math.Cos(t));
                float rot = (float)Math.Atan2(tang.Y, tang.X);

                // Doppler suave (izquierda caliente — medido en la referencia).
                float dop_t = (float)Math.Cos(t) * 0.5f + 0.5f;   // 0 izq → 1 der
                Color col = VFXPalettes.VoidQueen.DiskDoppler(1f - dop_t);
                float dopplerBoost = 1.12f - 0.28f * dop_t;

                float striation = 0.78f + 0.22f * (float)Math.Abs(Math.Sin(t * 5f + i * 1.7f));
                float hashJitter = 0.86f + 0.14f * VFXCore.Hash01(seed, i, 3);

                Color finalCol = col * (alpha * dopplerBoost * striation * hashJitter);
                float width = clumpWidth * (0.78f + 0.5f * striation);

                VFXCore.Quad(pos, finalCol, new Vector2(clumpLen, width), rot);
            }
        }

        // ------------------------------------------------------------------
        //  LOS RAYOS — once temblores de luz en el hemisferio superior
        // ------------------------------------------------------------------

        private static void ComputeRays(Vector2 center, float R, float time)
        {
            const int Rays = 11;
            int seed = 0x5A35;

            for (int i = 0; i < Rays; i++)
            {
                // Ángulo: -168° → -12° (hemisferio superior completo).
                float angle = -MathHelper.Pi * 0.933f +
                              i / (float)(Rays - 1) * MathHelper.Pi * 0.866f;

                // Largo desigual determinista y CONTENIDO (sutiles).
                float len = R * (0.42f + 0.62f * VFXCore.Hash01(seed, i, 7));
                // Pulsación individual (viva, no un abanico estático).
                float pulse = 0.65f + 0.35f * (float)Math.Sin(time * 2.2f + i * 2.1f);

                // Nacen del BORDE de la lente (siguen su silueta elíptica).
                float dirX = (float)Math.Cos(angle);
                float dirY = (float)Math.Sin(angle);
                float denom = (float)Math.Sqrt(LensA * LensA * dirY * dirY +
                                               LensB * LensB * dirX * dirX);
                float edgeT = 1f / Math.Max(denom, 0.0001f);
                float startR = edgeT * 1.04f;

                Vector2 mid = center + new Vector2(dirX, dirY) * (startR + len * 0.5f);

                Color col = Color.Lerp(VFXPalettes.VoidQueen.Ray,
                                       VFXPalettes.VoidQueen.RayEdge,
                                       VFXCore.Hash01(seed, i, 11));
                col *= 0.28f * pulse;

                float thick = R * (0.10f + 0.10f * VFXCore.Hash01(seed, i, 17));

                VFXCore.Quad(mid, col, new Vector2(len, thick), angle);
            }
        }

        // ------------------------------------------------------------------
        //  LOS RELÁMPAGOS — tormenta estática en los flancos de la lente
        // ------------------------------------------------------------------

        private static void ComputeBolts(Projectile p, Vector2 center, float R, float time)
        {
            // El flick se renueva cada ~9.6 ticks: el zigzag VIVE.
            int flick = (int)(time / 0.16f);
            // Semilla determinista por proyectil: todas las máquinas ven la
            // MISMA tormenta sin sincronizar nada.
            int seed = unchecked((int)(p.whoAmI * 2654435761u) + 0x9E37);

            // Flancos izquierdo y derecho de la lente.
            Vector2 leftEdge = center + new Vector2(-LensA * R, 0f);
            Vector2 rightEdge = center + new Vector2(LensA * R, 0f);

            BoltRenderer.ComputeQuads(
                leftEdge, leftEdge + new Vector2(-1.45f * R, -1.0f * R),
                seed, flick, 0.14f * R,
                VFXPalettes.VoidQueen.Bolt, VFXPalettes.VoidQueen.Spark, 0.85f, 1.1f);

            BoltRenderer.ComputeQuads(
                rightEdge, rightEdge + new Vector2(1.45f * R, -1.0f * R),
                seed + 1013, flick, 0.14f * R,
                VFXPalettes.VoidQueen.Bolt, VFXPalettes.VoidQueen.Spark, 0.85f, 1.1f);
        }
    }
}
