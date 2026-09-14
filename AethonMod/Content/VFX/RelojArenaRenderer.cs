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
    /// RelojArenaRenderer — v6.26 — EL BASTÓN DEL RELOJ DE ARENA CÓSMICO.
    ///
    /// EL RELOJ DE ARENA VIVO dibujado 100% por código (~50 px de alto):
    ///
    ///   · LA MASA (AlphaBlend PRIMERO): una aura de polvo cósmico dorado
    ///     (BrumaFX.Cloud tenue) — el reloj flota en su propia atmósfera.
    ///   · EL CUADRO: dos bulbos trapezoidales de vidrio de luz (cápsulas
    ///     VFXCore doradas: rieles laterales curvados + tapas + collar) —
    ///     el marco entero girado por la rotación de INVERSIÓN.
    ///   · LA ARENA: 26 MOTAS DE LUZ (quads VFXCore) con física derivada
    ///     de la edad (determinista — cero estado): montículo cónico
    ///     arriba, CORRIENTE visible en el cuello (granos acelerando +
    ///     destello LumenLib.Flare en el cuello), montículo CRECIENTE
    ///     abajo (grano a grano, fila a fila).
    ///   · LA INVERSIÓN: mientras dura el giro, un anillo OndaLib.Pulse
    ///     dorado expande del reloj (la onda del tiempo) y las motas
    ///     salpican.
    ///   · EL TIEMPO LENTO: los enemigos dentro del radio del reloj llevan
    ///     AFTERIMAGES FANTASMA — sus propios sprites re-dibujados tras su
    ///     rastro de velocidad con tinte dorado (la lectura visual del
    ///     "tiempo pesado"; la mecánica real es el daño+peso del proyectil).
    ///
    /// CONTRATO DE BATCH: Draw() exige el SpriteBatch CERRADO y lo deja
    /// CERRADO (el llamador restaura el batch de tML — contrato v6.10).
    /// La MASA va en AlphaBlend PRIMERO; los brillos en Additive.
    /// </summary>
    public static class RelojArenaRenderer
    {
        // ==================================================================
        //  LA GEOMETRÍA DEL RELOJ (px, marco LOCAL — y+ hacia abajo)
        // ==================================================================

        /// <summary>Media altura del reloj: 27 px → ~54 px de alto.</summary>
        public const float HalfH = 27f;

        /// <summary>Media anchura de las TAPAS (el extremo ancho).</summary>
        private const float HalfW = 13f;

        /// <summary>Media anchura del CUELLO (el extremo estrecho).</summary>
        private const float NeckW = 3.2f;

        /// <summary>La altura del cuello (donde viven las tapas interiores).</summary>
        private const float NeckY = 4f;

        // ==================================================================
        //  LA LÍNEA DE TIEMPO (la MISMA del proyectil — fuente única)
        // ==================================================================

        private const int Caida = RelojArenaProjectile.CaidaTicks;
        private const int Giro = RelojArenaProjectile.GiroTicks;
        private const int Ciclo = RelojArenaProjectile.CicloTicks;
        private const int Granos = RelojArenaProjectile.Granos;
        private const int Cruce = RelojArenaProjectile.CruceTicks;

        // ==================================================================
        //  LAS PALETAS DEL RELOJ (la identidad dorada-cósmica)
        // ==================================================================

        private static readonly Color VidrioOro = new(255, 214, 120);      // el marco
        private static readonly Color VidrioAlma = new(255, 244, 200);     // el corazón del marco
        private static readonly Color Arena = new(255, 232, 150);          // las motas
        private static readonly Color ArenaCaliente = new(255, 250, 215);  // el grano vivo
        private static readonly Color AuraMasa = new(168, 138, 82);        // el polvo cósmico
        private static readonly Color PulsoOnda = new(255, 224, 130);      // la inversión

        // ==================================================================
        //  EL PUNTO DE ENTRADA
        // ==================================================================

        /// <summary>Dibuja el reloj completo. El batch llega CERRADO y se
        /// devuelve CERRADO (contrato v6.10).</summary>
        public static void Draw(Projectile p, float age, int seed)
        {
            _current = p;   // para los fantasmas (el rango del tiempo lento)
            try
            {
                float time = Main.GlobalTimeWrappedHourly;

                // === LA FLOTACIÓN (visual — el reloj de relojería respira) ===
                float bob = MathF.Sin(age * 0.045f) * 5f;
                Vector2 center = p.Center - Main.screenPosition + new Vector2(0f, bob);

                // === LA FASE DEL CICLO (determinista por edad) ===
                float ciclo = age % Ciclo;
                bool cayendo = ciclo < Caida;
                float rot = p.rotation;   // la INVERSIÓN acumulada (n·π)

                // === 1. LA MASA (AlphaBlend PRIMERO — el polvo que OCLUYE) ===
                BrumaFX.BeginMass();
                BrumaFX.Cloud(center, 46f, AuraMasa, seed + 11, time * 0.22f,
                    puffs: 5, alpha: 0.13f);
                Main.spriteBatch.End();

                // === 2. LOS AFTERIMAGES DEL TIEMPO LENTO (los fantasmas de
                //     los enemigos cercanos — sus sprites re-dibujados) ===
                DrawFantasmas();

                // === 3. EL RELOJ (Additive: marco + arena + corriente) ===
                BeginAdditive();
                DrawMarco(center, rot, time, seed, cayendo);
                int vueltas = (int)(age / Ciclo);
                DrawArena(center, rot, ciclo, cayendo, time, seed, vueltas);
                Main.spriteBatch.End();

                // === 4. LA ONDA DE LA INVERSIÓN (el anillo del tiempo) ===
                if (!cayendo)
                {
                    float g = (ciclo - Caida) / (float)Giro;
                    BeginAdditive();
                    OndaLib.Pulse(Main.spriteBatch, center, g * 0.9f, 190f,
                        PulsoOnda, 0.8f, seed);
                    Main.spriteBatch.End();
                }

                // === 5. LA LUZ DEL MUNDO (dorada, puntual) ===
                if (Main.netMode != NetmodeID.Server)
                    Lighting.AddLight(p.Center, 0.55f, 0.42f, 0.16f);
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error (contrato v6.10).
                try { Main.spriteBatch.End(); } catch { }
            }
            finally
            {
                _current = null;   // sin estado entre proyectiles
            }
        }

        // ==================================================================
        //  EL MARCO — dos bulbos de vidrio de luz
        // ==================================================================

        private static void DrawMarco(Vector2 center, float rot, float time, int seed, bool cayendo)
        {
            // La respiración del vidrio (vivo, no rígido).
            float breathe = VFXCore.Breathe(time, 2.0f, 0f, 0.03f);

            // TAPA superior / inferior (barras horizontales en los extremos).
            CapsulaLocal(center, rot, new Vector2(0f, -HalfH), new Vector2(0f, -HalfH),
                (HalfW * 2f + 4f) * breathe, 2.6f, Tint(VidrioOro, 0.55f));
            CapsulaLocal(center, rot, new Vector2(0f, HalfH), new Vector2(0f, HalfH),
                (HalfW * 2f + 4f) * breathe, 2.6f, Tint(VidrioOro, 0.55f));

            // LOS RIELES laterales (dos segmentos por lado: la curvatura del
            // trapecio del bulbo — arriba y abajo del cuello).
            for (int lado = -1; lado <= 1; lado += 2)
            {
                float x = lado * HalfW;
                float xn = lado * NeckW;
                // Bulbo SUPERIOR (se estrecha hacia el cuello).
                CapsulaLocal(center, rot, new Vector2(x, -HalfH), new Vector2(xn, -NeckY),
                    2.0f, 0f, Tint(VidrioOro, 0.42f));
                // Bulbo INFERIOR (se ensancha hacia la tapa).
                CapsulaLocal(center, rot, new Vector2(xn, NeckY), new Vector2(x, HalfH),
                    2.0f, 0f, Tint(VidrioOro, 0.42f));
                // El reflejo del riel (la línea fina blanca interior).
                CapsulaLocal(center, rot, new Vector2(x * 0.82f, -HalfH * 0.86f),
                    new Vector2(xn * 0.8f, -NeckY * 0.8f), 0.9f, 0f, Tint(VidrioAlma, 0.22f));
            }

            // EL COLLAR del cuello (la cintura del reloj).
            CapsulaLocal(center, rot, new Vector2(0f, 0f), new Vector2(0f, 0f),
                NeckW * 2f + 7f, 2.2f, Tint(VidrioAlma, 0.5f));

            // EL CORAZÓN del marco: bloom dorado pequeño en el cuello (la
            // corriente VIVE — más brillo mientras cae arena).
            float corazon = cayendo ? 1f : 0.4f;
            LumenLib.Bloom(Main.spriteBatch, center, 16f * breathe * corazon + 6f,
                VidrioOro, 0.35f * corazon, 2);

            // LAS TRES PERLAS del marco (la firma de la relojería cósmica:
            // dos en las tapas, una arriba — orbitan lento el eje).
            for (int k = 0; k < 3; k++)
            {
                float ang = time * 0.7f + k * MathHelper.TwoPi / 3f;
                float ry = (k == 2 ? 0f : (k == 0 ? -HalfH : HalfH)) + MathF.Sin(ang) * 2f;
                float rx = MathF.Cos(ang) * (HalfW + 5f);
                Vector2 local = new(rx, ry);
                Vector2 world = Transform(center, local, rot);
                Quad(Glow, world, new Vector2(4.5f, 4.5f), 0f, Tint(VidrioAlma, 0.5f));
            }
        }

        // ==================================================================
        //  LA ARENA — 26 motas de luz con física derivada de la edad
        // ==================================================================

        private static void DrawArena(Vector2 center, float rot, float ciclo, bool cayendo,
            float time, int seed, int vueltas)
        {
            // === LA PARIDAD DE LAS CÁMARAS: con cada inversión el reloj queda
            //     al revés — la cámara que era ARRIBA pasa a ser ABAJO. La
            //     arena siempre apila en la cámara que ESTÁ ARRIBA (así el
            //     giro y el ciclo son CONTINUOS: nada teletransporta). ===
            float sArriba = (vueltas % 2 == 0) ? -1f : 1f;   // signo Y de la cámara alta
            float sAbajo = -sArriba;                          // la baja

            // El INSTANTE congelado durante la inversión (toda la arena ya
            // cayó — el montículo de abajo LLENO, el de arriba VACÍO).
            float c = MathF.Min(ciclo, Caida);

            int enVuelo = 0;

            for (int i = 0; i < Granos; i++)
            {
                float inicio = i * Caida / (float)Granos;

                // === ESTADO del grano i (determinista por c) ===
                if (c < inicio)
                {
                    // TODAVÍA ARRIBA: apilado en el montículo cónico del
                    // bulbo superior (los granos caen POR ORDEN de índice:
                    // el hueco crece desde el cuello hacia arriba).
                    int caidos = (int)(c * Granos / Caida);   // ya partieron
                    int rank = Math.Max(0, i - caidos);        // profundidad

                    float y = sArriba * (NeckY + 2.2f + rank * 2.3f);
                    float half = HalfAnchoEn(MathF.Abs(y));
                    float x = (VFXCore.Hash01(seed, i, 17) - 0.5f) * 2f * half * 0.8f;
                    Vector2 world = Transform(center, new Vector2(x, y), rot);
                    Quad(Glow, world, new Vector2(2.4f, 2.4f), 0f, Tint(Arena, 0.55f));
                }
                else if (c < inicio + Cruce)
                {
                    // EN VUELO: acelerando por el cuello (p² — arena de
                    // verdad: sale lenta y aterriza rápido).
                    enVuelo++;
                    float pr = MathHelper.Clamp((c - inicio) / Cruce, 0f, 1f);
                    float eased = pr * pr;
                    Vector2 aterrizaje = PosMonticulo(i, seed, sAbajo);
                    Vector2 cuello = new(0f, sArriba * (NeckY - 0.5f));
                    Vector2 local = Vector2.Lerp(cuello, aterrizaje, eased);
                    local.X += MathF.Sin(pr * MathF.PI) * 1.3f *
                        (VFXCore.Hash01(seed, i, 29) - 0.5f) * 2f;
                    Vector2 world = Transform(center, local, rot);
                    // El grano en vuelo es el MÁS BRILLANTE (la corriente).
                    Quad(Glow, world, new Vector2(3.0f, 3.0f), 0f, Tint(ArenaCaliente, 0.8f));
                }
                else
                {
                    // YA ABAJO: su sitio fijo del montículo creciente.
                    Vector2 world = Transform(center, PosMonticulo(i, seed, sAbajo), rot);
                    Quad(Glow, world, new Vector2(2.4f, 2.4f), 0f, Tint(Arena, 0.6f));
                }
            }

            // === LA CORRIENTE VISIBLE: el hilo brillante del cuello cuando
            //     hay arena en vuelo (la caída se LEE desde lejos). ===
            if (enVuelo > 0)
            {
                Vector2 cuelloTop = Transform(center, new Vector2(0f, sArriba * NeckY), rot);
                Vector2 cuelloBot = Transform(center, new Vector2(0f, sAbajo * NeckY), rot);
                CapsulaWorld(cuelloTop, cuelloBot, 2.2f, Tint(ArenaCaliente, 0.5f));
                // EL DESTELLO del cuello (la boca de la corriente).
                LumenLib.Flare(Main.spriteBatch, cuelloTop, 14f, Arena, 0.5f, time * 1.4f);
                // El respirar del montículo lleno: brillo creciente abajo.
                Vector2 monticulo = Transform(center, new Vector2(0f, sAbajo * (HalfH - 4f)), rot);
                LumenLib.Bloom(Main.spriteBatch, monticulo, 10f, Arena, 0.28f, 2);
            }

            // === EL VAIVÉN de todo el polvo (la vida sutil): una mota
            //     errante flotando alrededor del reloj (2, deterministas). ===
            for (int k = 0; k < 2; k++)
            {
                float ang = time * (0.32f + 0.14f * k) * ((k & 1) == 0 ? 1f : -1f)
                            + VFXCore.Hash01(seed, 3 + k, 5) * MathHelper.TwoPi;
                Vector2 world = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.55f)
                    * (HalfW + 14f + 6f * MathF.Sin(time * 0.5f + k));
                Quad(Glow, world, new Vector2(2.0f, 2.0f), 0f, Tint(Arena, 0.35f));
            }
        }

        /// <summary>La media anchura INTERIOR del bulbo en la altura y (el
        /// trapecio: estrecho en el cuello, ancho en las tapas).</summary>
        private static float HalfAnchoEn(float y)
        {
            // t: 0 en la tapa superior, 1 en el cuello.
            float t = MathHelper.Clamp((y + HalfH) / (HalfH - NeckY), 0f, 1f);
            float w = MathHelper.Lerp(HalfW - 2f, NeckW - 1.2f, t);
            return MathF.Max(w, 1.4f);
        }

        /// <summary>La POSICIÓN fija del grano i dentro del montículo de
        /// abajo (filas de 5 desde el fondo del bulbo inferior) — en la
        /// cámara con signo sAbajo.</summary>
        private static Vector2 PosMonticulo(int i, int seed, float sAbajo)
        {
            int fila = i / 5;
            int col = i % 5;
            float y = sAbajo * (HalfH - 2.6f - fila * 3.0f);
            float half = MathF.Max(HalfW - 4f - fila * 1.1f, 2.0f);
            float x = (col / 4f - 0.5f) * 2f * half * 0.9f
                      + (VFXCore.Hash01(seed, i, 41) - 0.5f) * 2.2f;
            return new Vector2(x, y);
        }

        // ==================================================================
        //  LOS FANTASMAS DEL TIEMPO LENTO (los enemigos cercanos)
        // ==================================================================

        /// <summary>Los AFTERIMAGES dorados de los enemigos en 160 px: sus
        /// sprites re-dibujados tras su rastro de velocidad (AlphaBlend
        /// bajo — la lectura de "tiempo pesado"). Solo cliente.</summary>
        private static void DrawFantasmas()
        {
            if (Main.netMode == NetmodeID.Server) return;
            Projectile p = CurrentProjectile;
            if (p == null) return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            try
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    if ((npc.Center - p.Center).Length() > 160f) continue;

                    var tex = Terraria.GameContent.TextureAssets.Npc[npc.type].Value;
                    if (tex == null) continue;

                    // TRES fantasmas detrás del rastro de velocidad.
                    for (int g = 1; g <= 3; g++)
                    {
                        Vector2 pos = npc.Center - Main.screenPosition
                                      - npc.velocity * (2.2f * g);
                        float a = 0.16f - g * 0.035f;
                        Color col = new Color(255, 226, 150) * a;
                        Main.spriteBatch.Draw(tex, pos, npc.frame, col, npc.rotation,
                            npc.frame.Size() * 0.5f, npc.scale,
                            npc.spriteDirection == -1
                                ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
                    }
                }
            }
            catch { }

            Main.spriteBatch.End();
        }

        /// <summary>El proyectil que llama al Draw actual (pasa por el
        /// set de PreDraw — el renderer es estático y sin estado).</summary>
        private static Projectile _current;

        /// <summary>Registra el proyectil llamador (lo usan los fantasmas
        /// para el rango — se limpia al salir del Draw).</summary>
        private static Projectile CurrentProjectile => _current;

        // ==================================================================
        //  LOS HELPERS DE PINTADO (los de la casa)
        // ==================================================================

        private static Asset<Texture2D> _glow;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        /// <summary>Transforma un punto LOCAL del reloj a pantalla (con la
        /// rotación de la inversión acumulada).</summary>
        private static Vector2 Transform(Vector2 center, Vector2 local, float rot)
        {
            float c = MathF.Cos(rot), s = MathF.Sin(rot);
            return center + new Vector2(local.X * c - local.Y * s, local.X * s + local.Y * c);
        }

        /// <summary>Cápsula entre dos puntos LOCALES del marco.</summary>
        private static void CapsulaLocal(Vector2 center, float rot, Vector2 a, Vector2 b,
            float width, float extra, Color tint)
        {
            Vector2 wa = Transform(center, a, rot);
            Vector2 wb = Transform(center, b, rot);
            Vector2 mid = (wa + wb) * 0.5f;
            Vector2 d = wb - wa;
            float len = d.Length();
            if (len < 0.1f) len = width;
            float r = MathF.Atan2(d.Y, d.X);
            Quad(Glow, mid, new Vector2(len + width, width * 1.9f), r, tint);
        }

        /// <summary>Cápsula entre dos puntos de PANTALLA (la corriente).</summary>
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
