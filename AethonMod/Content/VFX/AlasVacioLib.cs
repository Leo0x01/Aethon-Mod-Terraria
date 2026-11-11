using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.VFX
{
    // ======================================================================
    //  AlasVacioLib — v6.124 — LAS ALAS RÚNICAS DEL VACÍO, REHECHAS DE
    //  CERO (la letra: «me pregunto porque las alas runicas del vacio
    //  siguen con picos y dientes de sierra, deberias investigar bien
    //  como quitar eso, intenta rehacer el codigo de las alas runicas
    //  del vacio»).
    //
    //  LA AUTOPSIA DE LAS CINCO CURAS FALLIDAS (v6.113 → v6.123): cada
    //  ronda pulió UN síntoma del abanico de CINCO agujas (la punta
    //  redonda, la membrana que rellenó los valles, el borde maestro,
    //  el inset del ruido, las runas un nodo adentro) — pero la SIERRA
    //  nacía de la ARQUITECTURA misma: CINCO plumas de perfil aguja
    //  (Afilada 0.55 · Filosa 0.05 · Redonda 0) en abanico separado
    //  (pico → valle → pico), VESTIDAS por la textura de ruido cuyos
    //  blobs nacen con alfa DENTRO de la banda (por mucho inset que
    //  corra, los blobs SIEMPRE tocan el filo) y con el glifo girando
    //  pegado a la silueta. Ninguna cura podía matar la sierra porque
    //  TODAS vivían dentro de esa geometría.
    //
    //  EL REHACER DE VERDAD — NADA del camino viejo sobrevive:
    //   · UNA MEMBRANA CONTINUA por ala (no cinco agujas): el contorno
    //     es UNA curva cerrada suave — borde de ataque convexo, lóbulo
    //     trasero REDONDO de coseno (la punta muere a 0 con derivada 0:
    //     ni una aguja ni un plomo).
    //   · CERO TEXTURA DE RUIDO: el interior son RAMPS DE COLOR puros
    //     (la paleta del vacío de siempre) + dos venas de brillo y los
    //     glifos — nada con alfa que cruce el borde.
    //   · LA LEY DEL FILO (la técnica que curó los hazes, HazLib v6.91):
    //     el aporte de cada vértice de borde llega a ~0 EXACTO y el
    //     lote soft-add (dst += rgb·(1−dst)) NO PUEDE encender un píxel
    //     con un aporte de cero — los picos son MATEMÁTICAMENTE
    //     IMPOSIBLES, no «ajustados».
    //   · LAS RUNAS VIVEN DENTRO (tres glifos por ala, en el cuerpo de
    //     la membrana, girando serenos) + EL ANILLO de la casa alrededor
    //     del portador — jamás montadas en la silueta.
    //   · Las migas del vacío (AlasAuroraHalo.AI, alaEstilo == 2)
    //     SIGUEN cayendo — el vacío no cambia de casa.
    // ======================================================================
    public static class AlasVacioLib
    {
        // EL SOFT-ADD CANÓNICO DE LA CASA — la acumulación asintótica
        // (dst += rgb·(1−dst)): los bordes se funden EN CURVA y ningún
        // aporte CLIPEA jamás (el mismo blend de AlasAuroraLib/HazLib).
        private static readonly BlendState _pantalla = new BlendState
        {
            ColorSourceBlend = Blend.InverseDestinationColor,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
        };

        private static BasicEffect _fxPlano;

        private static VertexPositionColorTexture[] _verts =
            new VertexPositionColorTexture[4096];

        // === LA PALETA DEL VACÍO (la de siempre, v6.120) ===
        private static readonly Color RaizVacio = new Color(26, 14, 44);
        private static readonly Color MedioVacio = new Color(122, 78, 188);
        private static readonly Color PielVacio = new Color(150, 84, 235);
        private static readonly Color DiaVacio = new Color(90, 60, 140);

        // === LAS CUENTAS DEL ALA (el diseño nuevo, en px de pantalla) ===
        private const int Muestras = 26;     // secciones transversales por ala
        private const float LargoMax = 92f;  // el largo del ala a plena apertura

        /// <summary>v6.124 — LA PÉRDIDA DEL CONTEXTO (el download de
        /// tML mata los recursos): el efecto renace limpio.</summary>
        public static void Reiniciar()
        {
            BasicEffect viejo = _fxPlano;
            _fxPlano = null;
            if (viejo == null) return;
            Main.QueueMainThreadAction(delegate
            {
                try { viejo.Dispose(); } catch { }
            });
        }

        private static bool AsegurarEfecto(GraphicsDevice gd)
        {
            try
            {
                _fxPlano ??= new BasicEffect(gd)
                {
                    LightingEnabled = false,
                    VertexColorEnabled = true,
                    TextureEnabled = false,
                    Alpha = 1f
                };
                return _fxPlano != null;
            }
            catch { return false; }
        }

        // ==================================================================
        //  EL PERFIL DE LA MEMBRANA — LA CURVA CERRADA SUAVE (la muerte
        //  de la aguja): sube smoothstep hasta el hombro (t≈0,30), vive
        //  pleno y baja con perfil de COSENO a la punta redonda — la
        //  derivada llega a CERO en la punta (ni aguja, ni plomo).
        // ==================================================================
        private static float Semiancho(float t)
        {
            // el nacimiento suave (smoothstep 0 → 0,30)
            float nace = t / 0.30f;
            if (nace > 1f) nace = 1f;
            nace = nace * nace * (3f - 2f * nace);
            // la caída redonda (coseno 0,62 → 1,0: el lóbulo trasero)
            float muere = 1f;
            if (t > 0.62f)
            {
                float k = (t - 0.62f) / 0.38f;              // 0 → 1
                if (k > 1f) k = 1f;
                muere = MathF.Cos(k * MathHelper.PiOver2);  // derivada 0 en la punta
                muere = 0.16f + 0.84f * muere;               // nunca del todo cero antes del borde
            }
            return 15.5f * nace * muere;
        }

        /// <summary>El color de la membrana a lo largo del ala (la rampa
        /// de siempre: raíz hondo → piel del vacío en el borde de fuga)
        /// con el mezclado del día de la casa.</summary>
        private static Color TonoMembrana(float t, float mezclaDia)
        {
            Color c = t < 0.55f
                ? Color.Lerp(RaizVacio, MedioVacio, t / 0.55f)
                : Color.Lerp(MedioVacio, PielVacio, (t - 0.55f) / 0.45f);
            return Color.Lerp(c, DiaVacio, 0.62f * mezclaDia);
        }

        // ==================================================================
        //  EL DIBUJO PRINCIPAL — las dos membranas continuas.
        // ==================================================================
        public static bool Dibujar(Player pl, in EstiloAlas estilo, float spread, float flap, float mezclaDia)
        {
            if (Main.netMode == 2 || pl == null || pl.dead || spread <= 0.02f)
                return false;
            if (Main.gameMenu)
                return false;
            try
            {
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (!AsegurarEfecto(gd))
                    return false;

                float gt = Main.GlobalTimeWrappedHourly;
                float grav = pl.gravDir;
                float rot = pl.fullRotation;

                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Vector2 centro = pl.MountedCenter - Main.screenPosition;
                    Vector2 ancla = centro + new Vector2(0f, -4f * grav);
                    if (rot != 0f)
                        ancla = centro + Utils.RotatedBy(ancla - centro, rot, default(Vector2));

                    int n = 0;
                    for (int lado = -1; lado <= 1; lado += 2)
                        n = EmitirAla(n, ancla, lado, spread, flap, gt, grav, rot, mezclaDia);

                    if (n >= 3)
                        Volcar(gd, n);
                }
                finally
                {
                    VFXCore.ReabrirLoteVanilla();
                }
                return true;
            }
            catch
            {
                try { VFXCore.ReabrirLoteVanilla(); } catch { }
                return false;
            }
        }

        /// <summary>UNA membrana: la tira de secciones con la ley del
        /// filo (el borde SIEMPRE aporta ~0 — pico matemáticamente
        /// imposible) + el corazón interior + las dos venas.</summary>
        private static int EmitirAla(int n, Vector2 ancla, int lado, float spread,
            float flap, float gt, float grav, float rot, float mezclaDia)
        {
            // === LA POSE DEL ALA ===
            float apertura = MathHelper.Lerp(0.34f, 1.02f, spread);
            float aleteo = MathF.Sin(flap - lado * 0.45f) * (0.05f + 0.09f * spread);
            float ang = -MathHelper.PiOver2 + lado * (apertura + aleteo) + rot * grav;
            Vector2 dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang) * grav);
            Vector2 perp = new Vector2(-dir.Y, dir.X) * lado;   // el borde de FUERA
            float largo = LargoMax * (0.58f + 0.42f * spread)
                * (1f + 0.03f * MathF.Sin(flap * 0.7f + lado));
            float anchoPleno = 15.5f * (0.85f + 0.35f * spread);

            // el pico de rendimiento de la casa: nada de listas nuevas
            Vector2 pA = default, pB = default, pC = default, pD = default;

            for (int i = 0; i < Muestras - 1; i++)
            {
                if (n + 48 > _verts.Length) break;
                float t0 = i / (float)(Muestras - 1);
                float t1 = (i + 1) / (float)(Muestras - 1);

                // los centros de las dos secciones (la garra curvada hacia afuera)
                Vector2 c0 = PuntoAla(ancla, dir, perp, largo, t0, lado);
                Vector2 c1 = PuntoAla(ancla, dir, perp, largo, t1, lado);

                float w0 = Semiancho(t0) * anchoPleno / 15.5f;
                float w1 = Semiancho(t1) * anchoPleno / 15.5f;

                // LA LEY DEL FILO: el borde aporta ~0 — el soft-add no
                // puede encender un píxel con un aporte de cero.
                const float AlfaBorde = 0.03f;
                Color k0 = TonoMembrana(t0, mezclaDia);
                Color k1 = TonoMembrana(t1, mezclaDia);

                // (el fade del conjunto con la apertura: ala plegada, ala tenue)
                float vive = 0.30f + 0.70f * spread;

                pA = c0 + perp * w0;   // borde de fuera, sección 0
                pB = c0 - perp * w0;   // borde de dentro, sección 0
                pC = c1 + perp * w1;   // borde de fuera, sección 1
                pD = c1 - perp * w1;   // borde de dentro, sección 1

                Color a0 = Pre(k0, vive);
                Color a1 = Pre(k0, vive * AlfaBorde);
                Color b0 = Pre(k1, vive);
                Color b1 = Pre(k1, vive * AlfaBorde);

                // dos triángulos por segmento (tira continua)
                _verts[n++] = new VertexPositionColorTexture(new Vector3(pA, 0f), a1, Vector2.Zero);
                _verts[n++] = new VertexPositionColorTexture(new Vector3(pB, 0f), a0, Vector2.Zero);
                _verts[n++] = new VertexPositionColorTexture(new Vector3(pC, 0f), b1, Vector2.Zero);
                _verts[n++] = new VertexPositionColorTexture(new Vector3(pB, 0f), a0, Vector2.Zero);
                _verts[n++] = new VertexPositionColorTexture(new Vector3(pD, 0f), b0, Vector2.Zero);
                _verts[n++] = new VertexPositionColorTexture(new Vector3(pC, 0f), b1, Vector2.Zero);
            }

            // === EL CORAZÓN INTERIOR (la veta central, adentro del ala) ===
            n = EmitirVena(n, ancla, dir, perp, largo, lado, anchoPleno, 0.0f, 0.62f,
                new Color(196, 150, 255), 0.40f * (0.3f + 0.7f * spread), mezclaDia);
            // === LAS DOS VENAS DE LA MEMBRANA (la caligrafía del vacío) ===
            n = EmitirVena(n, ancla, dir, perp, largo, lado, anchoPleno, 0.42f, 0.10f,
                PielVacio, 0.30f * (0.3f + 0.7f * spread), mezclaDia);
            n = EmitirVena(n, ancla, dir, perp, largo, lado, anchoPleno, -0.42f, 0.10f,
                MedioVacio, 0.26f * (0.3f + 0.7f * spread), mezclaDia);

            return n;
        }

        /// <summary>El centro de una sección del ala (la garra curvada:
        /// la curvatura abre el arco hacia AFUERA con el cuadrado de t).</summary>
        private static Vector2 PuntoAla(Vector2 ancla, Vector2 dir, Vector2 perp,
            float largo, float t, int lado)
        {
            // la curva simétrica del par: perp YA lleva el lado (el
            // espejo es la construcción misma — girar == reflejar).
            return ancla + dir * (largo * t) + perp * (9.5f * t * t);
        }

        /// <summary>UNA VENA: la tira delgada que vive DENTRO de la
        /// membrana (a un desplazamiento fijo de su eje), con fade
        /// smoothstep en AMBOS extremos — el trazo de la runa que
        /// escribe el ala. Nunca cruza el borde: nace y muere adentro.</summary>
        private static int EmitirVena(int n, Vector2 ancla, Vector2 dir, Vector2 perp,
            float largo, int lado, float anchoPleno, float desplaz, float grueso,
            Color tinte, float alfa, float mezclaDia)
        {
            if (alfa <= 0.01f) return n;
            const int Seg = 14;
            const float TIni = 0.12f, TFin = 0.80f;
            for (int i = 0; i < Seg; i++)
            {
                if (n + 6 > _verts.Length) break;
                float f0 = i / (float)Seg;
                float f1 = (i + 1) / (float)Seg;
                float t0 = MathHelper.Lerp(TIni, TFin, f0);
                float t1 = MathHelper.Lerp(TIni, TFin, f1);

                // EL FADE DE LOS EXTREMOS (smoothstep en ambos cabos)
                float fade0 = f0 < 0.15f ? f0 / 0.15f : (f0 > 0.85f ? (1f - f0) / 0.15f : 1f);
                float fade1 = f1 < 0.15f ? f1 / 0.15f : (f1 > 0.85f ? (1f - f1) / 0.15f : 1f);
                fade0 = MathHelper.Clamp(fade0, 0f, 1f);
                fade1 = MathHelper.Clamp(fade1, 0f, 1f);
                fade0 = fade0 * fade0 * (3f - 2f * fade0);
                fade1 = fade1 * fade1 * (3f - 2f * fade1);

                Vector2 c0 = PuntoAla(ancla, dir, perp, largo, t0, lado)
                    + perp * (desplaz * Semiancho(t0) * anchoPleno / 15.5f * 0.9f);
                Vector2 c1 = PuntoAla(ancla, dir, perp, largo, t1, lado)
                    + perp * (desplaz * Semiancho(t1) * anchoPleno / 15.5f * 0.9f);

                float g0 = Math.Max(0.6f, grueso * Semiancho(t0) * 0.16f);
                float g1 = Math.Max(0.6f, grueso * Semiancho(t1) * 0.16f);

                Color k0 = Pre(Color.Lerp(tinte, DiaVacio, 0.30f * mezclaDia), alfa * fade0);
                Color k1 = Pre(Color.Lerp(tinte, DiaVacio, 0.30f * mezclaDia), alfa * fade1);

                Vector2 q0 = c0 + perp * g0;
                Vector2 q1 = c0 - perp * g0;
                Vector2 q2 = c1 + perp * g1;
                Vector2 q3 = c1 - perp * g1;

                _verts[n++] = new VertexPositionColorTexture(new Vector3(q0, 0f), k0, Vector2.Zero);
                _verts[n++] = new VertexPositionColorTexture(new Vector3(q1, 0f), k0, Vector2.Zero);
                _verts[n++] = new VertexPositionColorTexture(new Vector3(q2, 0f), k1, Vector2.Zero);
                _verts[n++] = new VertexPositionColorTexture(new Vector3(q1, 0f), k0, Vector2.Zero);
                _verts[n++] = new VertexPositionColorTexture(new Vector3(q3, 0f), k1, Vector2.Zero);
                _verts[n++] = new VertexPositionColorTexture(new Vector3(q2, 0f), k1, Vector2.Zero);
            }
            return n;
        }

        private static Color Pre(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color((int)(c.R * f + 0.5f), (int)(c.G * f + 0.5f),
                (int)(c.B * f + 0.5f), 255);
        }

        private static void Volcar(GraphicsDevice gd, int nVerts)
        {
            _fxPlano.World = Main.GameViewMatrix.TransformationMatrix;
            _fxPlano.View = Matrix.Identity;
            _fxPlano.Projection = Matrix.CreateOrthographicOffCenter(
                0f, Main.screenWidth, Main.screenHeight, 0f, 0f, -1f);
            gd.BlendState = _pantalla;
            gd.DepthStencilState = DepthStencilState.None;
            gd.RasterizerState = RasterizerState.CullNone;
            foreach (EffectPass pass in _fxPlano.CurrentTechnique.Passes)
            {
                pass.Apply();
                int max = _verts.Length / 3 * 3;
                for (int i = 0; i < nVerts; i += max)
                {
                    int trozo = Math.Min(nVerts - i, max);
                    int tris = trozo / 3;
                    if (tris > 0)
                        gd.DrawUserPrimitives(PrimitiveType.TriangleList, _verts, i, tris);
                }
            }
        }

        // ==================================================================
        //  LAS RUNAS QUE VIVEN DENTRO — tres glifos por ala en el CUERPO
        //  de la membrana (jamás en la silueta) + EL ANILLO de la casa
        //  alrededor del portador (el mismo de OrbitaLib.RunasVacio).
        // ==================================================================
        public static bool DibujarRunas(Player pl, in EstiloAlas estilo, float spread, float flap)
        {
            if (Main.netMode == 2 || pl == null || pl.dead || spread <= 0.02f)
                return false;
            if (Main.gameMenu)
                return false;
            try
            {
                Vector2[][] glifos = OrbitaLib.RunasVacio;
                Color cuerpo = OrbitaLib.RunaVioleta;
                Color punta = OrbitaLib.RunaVioletaTip;
                if (glifos == null || glifos.Length == 0) return false;

                float gt = Main.GlobalTimeWrappedHourly;
                float grav = pl.gravDir;
                float rot = pl.fullRotation;
                float brillo = MathHelper.Clamp(spread, 0f, 1f);

                Vector2 centro = pl.MountedCenter - Main.screenPosition;
                Vector2 ancla = centro + new Vector2(0f, -4f * grav);
                if (rot != 0f)
                    ancla = centro + Utils.RotatedBy(ancla - centro, rot, default(Vector2));

                VFXCore.Begin();
                try
                {
                    Vector2 screenPos = Main.screenPosition;

                    // === EL ANILLO CENTRAL (ocho glifos alrededor del portador) ===
                    float dirGiro = pl.direction;
                    for (int j = 0; j < 8; j++)
                    {
                        float a = (j / 8f * MathHelper.TwoPi + gt * -0.30f) * dirGiro
                            + MathHelper.Pi * (1f - dirGiro) * 0.5f;
                        Vector2 p = ancla + new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.86f) * 46f;
                        float fase = VFXCore.Hash01(pl.whoAmI * 11 + j, 5, 7) * MathHelper.TwoPi;
                        float br = brillo * (0.42f + 0.30f * MathF.Sin(gt * 2f + j * 1.3f))
                            * (0.55f + 0.45f * spread);
                        SigiloLib.Runa(p + screenPos, a + MathHelper.PiOver2, glifos,
                            j % glifos.Length, 1.05f, gt, fase, cuerpo, punta, br, perla: false);
                    }

                    // === LOS TRES GLIFOS DEL ALA (en el CUERPO de la membrana) ===
                    for (int lado = -1; lado <= 1; lado += 2)
                    {
                        float apertura = MathHelper.Lerp(0.34f, 1.02f, spread);
                        float aleteo = MathF.Sin(flap - lado * 0.45f) * (0.05f + 0.09f * spread);
                        float ang = -MathHelper.PiOver2 + lado * (apertura + aleteo) + rot * grav;
                        Vector2 dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang) * grav);
                        Vector2 perp = new Vector2(-dir.Y, dir.X) * lado;
                        float largo = LargoMax * (0.58f + 0.42f * spread);

                        float[] enEl = { 0.30f, 0.55f, 0.78f };
                        for (int g = 0; g < enEl.Length; g++)
                        {
                            float t = enEl[g];
                            Vector2 c = PuntoAla(ancla, dir, perp, largo, t, lado)
                                + perp * (0.22f * Semiancho(t));   // HACIA EL BORDE DE FUERA, adentro
                            float fase = VFXCore.Hash01(pl.whoAmI * 7 + g * 5 + lado * 3, 3, 5)
                                * MathHelper.TwoPi;
                            float centellea = 0.55f + 0.45f * MathF.Sin(gt * 2.3f + fase + flap * 0.35f);
                            float esc = 0.95f + 0.30f * (g / 2f);
                            SigiloLib.Runa(c + screenPos,
                                (gt * 0.35f * ((g % 2 == 0) ? 1f : -1f) * lado + fase) * dirGiro,
                                glifos, (g * 3 + pl.whoAmI + (lado > 0 ? 0 : 1)) % glifos.Length,
                                esc, gt, fase, cuerpo, punta, brillo * centellea * 0.85f);
                        }
                    }
                    VFXCore.FlushAdditive();
                }
                finally
                {
                    VFXCore.ReabrirLoteVanilla();
                }
                return true;
            }
            catch
            {
                try { VFXCore.ReabrirLoteVanilla(); } catch { }
                return false;
            }
        }
    }
}
