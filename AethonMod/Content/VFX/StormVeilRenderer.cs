using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// StormVeilRenderer — v6.23 — LA ENVOLTURA DE RAYOS (100% CÓDIGO).
    ///
    /// Petición del usuario: "de la misma forma que haces con el fuego,
    /// crea un item cosmético que sea una envoltura de rayos; en esta
    /// envoltura usa nuestras librerías para darle el toque especial".
    ///
    /// EL ANILLO ELÉCTRICO: la SILUETA del jugador (una elipse del tamaño
    /// exacto del cuerpo) es el CARRIL donde vive la tormenta:
    ///   · LOS CHISPAZOS — rayos de verdad cruzando entre dos anclas del
    ///     contorno: la geometría nace de STORMLIB (ZigPath + el REFINO
    ///     FRACTAL multi-escala — la MISMA matemática del rayo del cielo
    ///     aprobado, sin una sola línea nueva que inventar).
    ///   · LOS ARCOS — descargas ABRAZANDO el contorno (el patrón de
    ///     ArcRing: jitter radial por hash, la silueta respira chispas).
    ///   · LOS PELOS — filamentos caóticos finísimos lanzados hacia
    ///     afuera (el "hair" que rodea a las descargas reales).
    ///
    /// LA INTERACCIÓN CON EL MOVIMIENTO (el corazón, como el fuego):
    ///   · ENERGÍA — la velocidad acumula ENERGÍA (0..1, vive en el
    ///     jugador): quieto, dos arcos perezosos y un chispazo suelto;
    ///     CORRIENDO la tormenta SE ENCIENDE — chispazos vivos, pelos
    ///     eléctricos y flick nervioso.
    ///   · VIENTO — al correr las anclas de los chispazos nacen A CONTRA
    ///     de la marcha (la estela eléctrica tras de ti).
    ///   · INERCIA — al saltar los arcos CAEN hacia los pies; al caer
    ///     SUBEN hacia la cabeza (el desplazamiento vertical del carril).
    ///
    /// EL RENDER: cada segmento de filamento = TRES CAPAS por VFXCore
    /// (el lenguaje de StrandImpl de StormLib: halo ancho suave + cuerpo
    /// de color + núcleo BLANCO razor-fino) con las texturas de rayo
    /// procedurales propias (BoltHalo/BoltCore) — y de ahí al buffer de
    /// VFXCore (camino oficial AppendToPlayerDraw, el de las coronas).
    /// La paleta es LA DEL RAYO APROBADO (oro solar + azul estelar +
    /// blanco incandescente): concordancia total con el cetro del trueno.
    /// </summary>
    public static class StormVeilRenderer
    {
        // ------------------------------------------------------------------
        //  LA PALETA (la del rayo del cielo — la casa tiene UNA sola voz)
        // ------------------------------------------------------------------

        private static readonly Color GoldHalo = new(255, 195, 85);   // oro solar (halo)
        private static readonly Color GoldWarm = new(255, 225, 140);  // oro cálido (arcos)
        private static readonly Color StarBlue = new(150, 180, 255);  // azul estelar (acentos)
        private static readonly Color WhiteIncan = new(255, 250, 235);// blanco incandescente

        // --- LAS TEXTURAS de filamento (las de StormLib, pedidas directas) ---
        private static Asset<Texture2D> _haloTex;
        private static Asset<Texture2D> _coreTex;

        private static Texture2D HaloTex =>
            (_haloTex ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/BoltHalo")).Value;

        private static Texture2D CoreTex =>
            (_coreTex ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/BoltCore")).Value;

        // ------------------------------------------------------------------
        //  LOS CARRILES — cuánta tormenta vive en la silueta
        // ------------------------------------------------------------------

        /// <summary>Chispazos entre anclas (carriles disponibles).</summary>
        private const int BoltSlots = 5;

        /// <summary>Arcos abrazando el contorno (arriba / abajo).</summary>
        private const int ArcSlots = 2;

        // ==================================================================
        //  EL RENDER — la tormenta al buffer de VFXCore (coords de MUNDO)
        // ==================================================================

        /// <summary>
        /// Calcula la ENVOLTURA DE RAYOS como cuadros de luz en el buffer
        /// de VFXCore (coordenadas de MUNDO).
        /// </summary>
        /// <param name="center">Centro del cuerpo del jugador (mundo).</param>
        /// <param name="bodyW">Ancho del cuerpo (px) — el carril abraza la silueta.</param>
        /// <param name="bodyH">Alto del cuerpo (px).</param>
        /// <param name="vel">Velocidad del jugador (el viento y la inercia).</param>
        /// <param name="time">Tiempo animado (GlobalTimeWrappedHourly).</param>
        /// <param name="energy">LA ENERGÍA de la tormenta (0..1 — la velocidad).</param>
        /// <param name="alpha">Multiplicador global (0..1).</param>
        /// <param name="seed">Semilla de identidad (cada jugador su tormenta).</param>
        public static void ComputeQuads(Vector2 center, float bodyW, float bodyH,
            Vector2 vel, float time, float energy, float alpha = 1f, int seed = 7)
        {
            if (alpha <= 0.02f) return;

            // EL CARRIL: la elipse de la silueta, apenas MÁS GRANDE que el
            // cuerpo (la envoltura abraza, no inunda).
            float rx = Math.Max(bodyW * 0.75f, 13f) + 3f;
            float ry = bodyH * 0.55f;

            // EL FLICK nervioso de la descarga — más rápido con energía.
            int flick = StormLib.FlickTick(time, 9f + 7f * energy);

            // LA INERCIA vertical del carril: al saltar los arcos CAEN a
            // los pies; al caer SUBEN a la cabeza (el viento relativo).
            float vy = MathHelper.Clamp(vel.Y * 1.1f, -7f, 7f);
            // EL VIENTO horizontal: las anclas derivan A CONTRA de la marcha.
            float wind = MathHelper.Clamp(-vel.X * 0.45f, -8f, 8f);

            // === 1. LOS CHISPAZOS — rayos cruzando entre anclas del carril ===
            for (int i = 0; i < BoltSlots; i++)
            {
                // LA ENERGÍA abre la puerta: quieto casi ninguno vive.
                if (!StormLib.IsLit(seed + i * 97, flick, 0.22f + 0.50f * energy)) continue;

                // LAS ANCLAS: dos puntos del contorno, derivando con la
                // tormenta y arrastradas por el viento de tu carrera.
                float drift = time * (0.35f + 0.10f * i) + i * 2.4f;
                float a1 = drift + VFXCore.Hash01(seed, flick, i * 41 + 5) * 0.9f;
                float span = 1.1f + 0.9f * VFXCore.Hash01(seed, flick, i * 43 + 9);
                float a2 = a1 + span;

                Vector2 start = EllipsePt(center, rx, ry, a1, vy, wind);
                Vector2 end = EllipsePt(center, rx, ry, a2, vy, wind);

                // EL CHISPAZO DE VERDAD: ZigPath + REFINO FRACTAL (la
                // matemática del rayo del cielo, jaggedness multi-escala).
                float amp = 6f + 7f * energy;
                Vector2[] path = StormLib.Refine(
                    StormLib.ZigPath(start, end, seed + i * 7, flick, 7, amp),
                    seed + i * 7, flick, 0.8f);

                // La doble voz del rayo: ORO / AZUL-ESTELAR alternando.
                Color halo = (i % 2 == 0) ? GoldHalo : StarBlue;
                Strand(path, width: 1.7f + 1.1f * energy, halo, WhiteIncan,
                    (0.60f + 0.40f * energy) * alpha);
            }

            // === 2. LOS ARCOS — descargas abrazando el contorno ===
            for (int i = 0; i < ArcSlots; i++)
            {
                if (!StormLib.IsLit(seed + 300 + i * 53, flick, 0.45f + 0.40f * energy)) continue;

                // El arco recorre MEDIA silueta con JITTER radial hash (el
                // patrón ArcRing de StormLib — el contorno respira).
                bool topHalf = i == 0;
                float a1 = topHalf ? -MathHelper.Pi : 0f;
                float a2 = topHalf ? 0f : MathHelper.Pi;
                const int Count = 11;
                Vector2[] pts = new Vector2[Count + 1];
                for (int p = 0; p <= Count; p++)
                {
                    float t = p / (float)Count;
                    float ang = a1 + (a2 - a1) * t;
                    // El JITTER: ±16% del radio, callado en los extremos.
                    float j = 1f + (VFXCore.Hash01(seed + 7, flick, p * 17 + 3 + i * 97) - 0.5f)
                              * 0.32f * (float)Math.Sin(t * Math.PI);
                    pts[p] = center + new Vector2(
                        (float)Math.Cos(ang) * rx * j + wind * (float)Math.Sin(t * Math.PI),
                        (float)Math.Sin(ang) * ry * j - vy);
                }
                Strand(pts, width: 1.3f + 0.8f * energy, GoldWarm, WhiteIncan,
                    (0.50f + 0.30f * energy) * alpha);
            }

            // === 3. LOS PELOS — filamentos caóticos hacia afuera ===
            int hairs = 2 + (int)(2f * energy);
            for (int i = 0; i < hairs; i++)
            {
                if (!StormLib.IsLit(seed + 800 + i * 41, flick, 0.25f + 0.45f * energy)) continue;

                float ang = VFXCore.Hash01(seed, flick, i * 61 + 11) * MathHelper.TwoPi
                            + time * 0.5f;
                Vector2 root = EllipsePt(center, rx, ry, ang, vy, wind);
                Vector2 dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang) * 0.9f);
                dir.Normalize();
                float len = 7f + 8f * VFXCore.Hash01(seed, flick, i * 67 + 13);
                Vector2 tip = root + dir * len;

                Vector2[] path = StormLib.ZigPath(root, tip, seed + 900 + i * 13, flick, 4, 4f);
                Strand(path, width: 0.8f, StarBlue, WhiteIncan, 0.45f * alpha);
            }

            // === 4. EL CORAZÓN — un aura eléctrica tenue en el pecho (la
            //         tormenta se SIENTE aun entre chispazos) ===
            float heartA = (0.07f + 0.09f * energy) * alpha;
            VFXCore.Quad(center, Tint(StarBlue, heartA), new Vector2(30f, 36f));
        }

        // ------------------------------------------------------------------
        //  EL FILAMENTO — tres capas por segmento (el lenguaje StormLib)
        // ------------------------------------------------------------------

        /// <summary>
        /// Dibuja UN filamento por su lista de puntos como cuadros VFXCore:
        /// halo ancho suave + cuerpo de color con grietas + núcleo BLANCO
        /// razor-fino, y gorros de descarga en los extremos.
        /// </summary>
        private static void Strand(Vector2[] pts, float width, Color halo, Color core, float alpha)
        {
            if (pts == null || pts.Length < 2 || alpha <= 0.02f || width <= 0.05f) return;

            for (int i = 0; i < pts.Length - 1; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[i + 1];
                Vector2 seg = b - a;
                float len = seg.Length();
                if (len < 0.35f) continue;

                float rot = (float)Math.Atan2(seg.Y, seg.X);
                Vector2 mid = (a + b) * 0.5f;
                // La GRIETA: brillo por sub-segmento (el filamento vive).
                float crackle = 0.62f + 0.38f * VFXCore.Hash01(1013, 77 + i, 17);

                // 1) EL HALO — banda suave y ancha (el contenido del color).
                VFXCore.Quad(mid, Tint(halo, 0.30f * alpha * crackle),
                    new Vector2(len + width * 2.0f, width * 2.0f), rot, HaloTex);
                // 2) EL CUERPO — el filamento de color.
                VFXCore.Quad(mid, Tint(halo, 0.85f * alpha * crackle),
                    new Vector2(len + width, width), rot, CoreTex);
                // 3) EL NÚCLEO — la vena BLANCA razor-fina.
                VFXCore.Quad(mid, Tint(core, 1f * alpha),
                    new Vector2(len + width * 0.45f, width * 0.26f), rot, CoreTex);
            }

            // LOS GORROS de descarga en ambos extremos (doble-draw barato).
            EndCap(pts[0], width, halo, core, alpha);
            EndCap(pts[pts.Length - 1], width, halo, core, alpha);
        }

        /// <summary>El gorro de descarga de un extremo (glow a dos escalas).</summary>
        private static void EndCap(Vector2 pos, float width, Color halo, Color core, float alpha)
        {
            VFXCore.Quad(pos, Tint(halo, 0.42f * alpha), new Vector2(width * 3.2f, width * 3.2f));
            VFXCore.Quad(pos, Tint(core, 0.78f * alpha), new Vector2(width * 1.8f, width * 1.8f));
        }

        // ------------------------------------------------------------------
        //  HELPERS (patrón validado del proyecto)
        // ------------------------------------------------------------------

        /// <summary>Punto de la elipse-silueta con inercia y viento aplicados.</summary>
        private static Vector2 EllipsePt(Vector2 center, float rx, float ry,
            float ang, float vy, float wind)
            => center + new Vector2(
                (float)Math.Cos(ang) * rx + wind * 0.5f,
                (float)Math.Sin(ang) * ry - vy);

        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
