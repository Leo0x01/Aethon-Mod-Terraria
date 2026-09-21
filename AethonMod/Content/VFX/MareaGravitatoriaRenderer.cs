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
    /// MareaGravitatoriaRenderer — v6.26 — EL BASTÓN DE LA MAREA GRAVITATORIA.
    ///
    /// LA OLA DE LUZ dibujada 100% por código (~46 px de cresta, cuerpo de
    /// ~70 px), ROTADA por la pendiente del terreno (p.rotation del
    /// proyectil — la ola se inclina al subir/bajar colinas):
    ///
    ///   · EL CUERPO DE AGUA (AlphaBlend PRIMERO): masa translúcida de
    ///     BrumaFX.Puff/Cloud azul — el agua de luz que OCLUYE un poco y
    ///     cae por delante de la cresta (la lengua de la ola).
    ///   · LA CRESTA (Additive): el lomo brillante — LumenLib.Bloom + Flare
    ///     blancos-azules, más un CORAZÓN de espuma.
    ///   · LA ESPUMA (Additive): chispas blancas nerviosas con el patrón
    ///     StormLib (FlickTick/IsLit — el parpadeo 15 Hz de la casa) sobre
    ///     la cresta + gotas saltando delante.
    ///   · LOS CHARCOS (AlphaBlend): detrás de la ola, la bruma de los
    ///     puntos de contacto que el proyectil va dejando (cada uno SECA
    ///     con la edad — encharcan el camino).
    ///   · LA ESTELA: la cresta deja rastro de agua (Ribbon Comet de
    ///     EstelaLib sobre el track del proyectil).
    ///
    /// LA OLA MENOR (DrawMinor): la versión 0.4× para las hijas del
    /// rompeolas — misma anatomía en miniatura.
    ///
    /// CONTRATO DE BATCH: Draw() exige el SpriteBatch CERRADO y lo deja
    /// CERRADO (contrato v6.10). MASA en AlphaBlend PRIMERO, brillos en
    /// Additive.
    /// </summary>
    public static class MareaGravitatoriaRenderer
    {
        // ==================================================================
        //  LA ANATOMÍA DE LA OLA (px)
        // ==================================================================

        /// <summary>La altura de la cresta sobre la base (px).</summary>
        private const float CrestaH = 46f;

        /// <summary>La semianchura del cuerpo de la ola (px).</summary>
        private const float HalfW = 34f;

        // ==================================================================
        //  LAS PALETAS DE LA MAREA (agua de luz azul)
        // ==================================================================

        private static readonly Color AguaProfunda = new(30, 90, 150);    // la masa
        private static readonly Color AguaCuerpo = new(60, 150, 220);     // el cuerpo
        private static readonly Color CrestaColor = new(160, 225, 255);   // el lomo
        private static readonly Color EspumaBlanca = new(235, 250, 255);  // las chispas
        private static readonly Color CharcoColor = new(40, 80, 130);     // los charcos

        // ==================================================================
        //  EL PUNTO DE ENTRADA — LA OLA GRANDE
        // ==================================================================

        /// <summary>Dibuja la marea completa. Batch CERRADO → CERRADO.</summary>
        public static void Draw(MareaGravitatoriaProjectile p, float age, int seed)
        {
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 center = p.Projectile.Center - Main.screenPosition;
                float rot = p.Projectile.rotation;   // LA PENDIENTE del terreno

                // === 1. LOS CHARCOS (AlphaBlend — detrás de todo) ===
                DrawCharcos(p, age, time, seed);

                // === 2. EL CUERPO DE AGUA (AlphaBlend — la masa) ===
                BrumaFX.BeginMass();
                // La bola de agua que empuja la ola (girada por la pendiente).
                for (int i = 0; i < 5; i++)
                {
                    float f = i / 4f;   // 0 = retaguardia, 1 = frente
                    Vector2 local = new(-HalfW * 0.55f + f * HalfW * 0.9f,
                        -CrestaH * 0.28f * (1f - f * 0.35f));
                    Vector2 pos = center + Rotar(local, rot);
                    BrumaFX.Puff(pos, 30f - 9f * f, AguaCuerpo, seed + 17 + i * 7,
                        time * 0.5f + i * 0.31f, alpha: 0.20f + 0.06f * f,
                        quality: 0.8f);
                }
                // LA LENGUA: el agua que cae por delante (la ola muerde el suelo).
                Vector2 frente = center + Rotar(new Vector2(HalfW * 0.62f, 8f), rot);
                BrumaFX.Puff(frente, 22f, AguaProfunda, seed + 53, time * 0.66f,
                    alpha: 0.22f, quality: 0.7f);
                Main.spriteBatch.End();

                // === 3. LA ESTELA DE LA CRESTA (Additive — el rastro) ===
                DrawEstela(p.Projectile, time, seed);

                // === 4. LA CRESTA + LA ESPUMA (Additive) ===
                BeginAdditive();
                DrawCresta(center, rot, time, seed, age);
                Main.spriteBatch.End();

                // === 5. LA LUZ DEL AGUA (muestreada a lo largo) ===
                if (Main.netMode != NetmodeID.Server)
                {
                    Vector2 a = p.Projectile.Center + Rotar(new Vector2(-HalfW, 0f), rot);
                    Vector2 b = p.Projectile.Center + Rotar(new Vector2(HalfW, 0f), rot);
                    LumenLib.LightAlong(a, b, AguaCuerpo, 0.55f, 40f);
                }
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>Dibuja una OLA MENOR (la hija del rompeolas) — la
        /// anatomía completa al 0.4×. Batch CERRADO → CERRADO.</summary>
        public static void DrawMinor(MareaChicaProjectile p, float age, int seed)
        {
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 center = p.Projectile.Center - Main.screenPosition;
                float scale = 0.4f;
                float fade = MathHelper.Clamp(
                    Math.Min(age / 10f, (VidaRestante(p) / 20f)), 0f, 1f);

                // LA MASA (AlphaBlend).
                BrumaFX.BeginMass();
                BrumaFX.Puff(center, 20f * scale + 6f, AguaCuerpo, seed + 11,
                    time * 0.6f, alpha: 0.24f * fade, quality: 0.7f);
                Main.spriteBatch.End();

                // LA MINI-CRESTA (Additive).
                BeginAdditive();
                LumenLib.Bloom(Main.spriteBatch, center, 16f * scale + 4f,
                    CrestaColor, 0.5f * fade, 2);
                LumenLib.Flare(Main.spriteBatch, center, 12f * scale,
                    EspumaBlanca, 0.4f * fade, time * 2.2f);
                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>El timeLeft envuelto (para el fade de salida).</summary>
        private static float VidaRestante(MareaChicaProjectile p) =>
            MathF.Max(0f, p.Projectile.timeLeft);

        // ==================================================================
        //  LA CRESTA — el lomo brillante + la espuma
        // ==================================================================

        private static void DrawCresta(Vector2 center, float rot, float time, int seed, float age)
        {
            // El PULSO de la marea (la ola respira al avanzar).
            float pulso = VFXCore.Breathe(time, 2.6f, 0f, 0.10f);

            // === EL LOMO: la línea curva de la cresta (de atrás hacia el
            //     frente, alzándose — el perfil clásico de la ola). ===
            int segs = 7;
            for (int i = 0; i < segs; i++)
            {
                float f = i / (float)(segs - 1);
                float x = -HalfW + f * HalfW * 2f;
                // LA CURVA de la cresta: baja atrás, se alza al frente.
                float y = -CrestaH * (0.35f + 0.65f * f * f) * pulso;
                Vector2 a = center + Rotar(new Vector2(x, y), rot);
                float x2 = -HalfW + (f + 1f / segs) * HalfW * 2f;
                float y2 = -CrestaH * (0.35f + 0.65f * (f + 1f / segs) * (f + 1f / segs)) * pulso;
                Vector2 b = center + Rotar(new Vector2(x2, y2), rot);
                CapsulaWorld(a, b, 9f, Tint(AguaCuerpo, 0.30f));
                // LA VENA BLANCA de la cresta (el filo del agua).
                CapsulaWorld(a, b, 4f, Tint(CrestaColor, 0.55f));
            }

            // === EL CORAZÓN DE LA CRESTA: el punto más alto delante ===
            Vector2 cumbre = center + Rotar(new Vector2(HalfW * 0.72f, -CrestaH * 0.94f * pulso), rot);
            LumenLib.Bloom(Main.spriteBatch, cumbre, 34f * pulso, CrestaColor, 0.55f, 3);
            LumenLib.Flare(Main.spriteBatch, cumbre, 22f, EspumaBlanca, 0.55f, time * 1.8f);

            // === LA ESPUMA: chispas blancas con el PARPADEO de StormLib
            //     (FlickTick/IsLit 15 Hz — nerviosa como espuma de verdad). ===
            int flick = StormLib.FlickTick(time, 15f);
            for (int k = 0; k < 10; k++)
            {
                if (!StormLib.IsLit(seed + 91 + k * 13, flick, 0.55f)) continue;
                float f = VFXCore.Hash01(seed, k, 21);
                float x = -HalfW * 0.9f + f * HalfW * 1.8f;
                float y = -CrestaH * (0.35f + 0.65f * f * f) * pulso
                          - 3f * VFXCore.Hash01(seed, k, 23);
                // EL SALTO de la gota de espuma (viven sobre la cresta).
                float salto = MathF.Abs(MathF.Sin(time * 5.5f + k * 1.7f)) * 7f;
                Vector2 pos = center + Rotar(new Vector2(x, y - salto), rot);
                Quad(Glow, pos, new Vector2(3.4f, 3.4f), 0f, Tint(EspumaBlanca, 0.6f));
            }

            // === EL ROCÍO DELANTERO: gotas que la ola lanza al frente ===
            for (int k = 0; k < 4; k++)
            {
                float h1 = VFXCore.Hash01(seed, 60 + k, 29);
                float fase = (time * 0.9f + h1) % 1f;   // vuelo determinista
                Vector2 local = new(HalfW * (0.8f + 0.3f * h1) + fase * 14f,
                    -CrestaH * 1.1f + fase * fase * 34f);
                Vector2 pos = center + Rotar(local, rot);
                float a = (1f - fase) * 0.45f;
                Quad(Glow, pos, new Vector2(2.6f, 2.6f), 0f, Tint(CrestaColor, a));
            }
        }

        // ==================================================================
        //  LOS CHARCOS — el rastro encharcado (se seca con la edad)
        // ==================================================================

        private static void DrawCharcos(MareaGravitatoriaProjectile p, float age, float time, int seed)
        {
            bool any = false;
            for (int i = 0; i < p.Charcos.Length; i++)
            {
                if (p.CharcoEdad[i] < 0f) continue;
                any = true;
                break;
            }
            if (!any) return;

            BrumaFX.BeginMass();
            for (int i = 0; i < p.Charcos.Length; i++)
            {
                if (p.CharcoEdad[i] < 0f) continue;
                float vejez = (age - p.CharcoEdad[i]) / 220f;   // 3.7 s secándose
                if (vejez >= 1f) continue;
                float alfa = 0.20f * (1f - vejez) * (1f - vejez * 0.4f);
                // El charco: achatado (ancho y bajo — agua en el suelo).
                BrumaFX.Puff(p.Charcos[i] - Main.screenPosition + new Vector2(0f, 4f),
                    14f + 6f * (1f - vejez), CharcoColor, seed + 130 + i * 17,
                    time * 0.24f + i * 0.5f, alpha: alfa, quality: 0.6f);
            }
            Main.spriteBatch.End();
        }

        // ==================================================================
        //  LA ESTELA — el rastro de la cresta (Ribbon Comet)
        // ==================================================================

        private static void DrawEstela(Projectile p, float time, int seed)
        {
            Vector2[] camino = EstelaLib.Track(p.whoAmI, 18).Points();
            if (camino == null || camino.Length < 3) return;

            Vector2[] pantalla = new Vector2[camino.Length];
            for (int i = 0; i < camino.Length; i++)
                pantalla[i] = camino[i] - Main.screenPosition;

            BeginAdditive();
            EstelaLib.Ribbon(Main.spriteBatch, pantalla, 10f, EstelaProfile.Comet,
                AguaCuerpo, 0.34f, seed + 7, time, head: false);
            Main.spriteBatch.End();
        }

        // ==================================================================
        //  LOS HELPERS DE PINTADO (los de la casa)
        // ==================================================================

        private static Asset<Texture2D> _glow;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        /// <summary>Rotación 2D de un vector local (la pendiente).</summary>
        private static Vector2 Rotar(Vector2 v, float rot)
        {
            float c = MathF.Cos(rot), s = MathF.Sin(rot);
            return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
        }

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
            // v6.50.3 — FIX (sonda IL contra el FNA real): BlendState.Additive
            // de FNA es (SourceAlpha, One) — el alfa GATEA el aporte. El Tint
            // premultiplicado v6.25 atenuaba DOS VECES (intensidad real f²:
            // el halo 0.30 salía a 0.09). RGB intacto, alfa=f: LINEAL.
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
