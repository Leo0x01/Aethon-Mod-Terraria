using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// TajoLib — v6.40 — LA ESCRITURA DEL FILORTE (los tajos del anime).
    ///
    /// LA LIBRERÍA DEL Tajo: la media luna blanca de los animes — las
    /// líneas curvas que aparecen DESPUÉS del corte, crecen de un extremo
    /// al otro y se disuelven — promovida a primitiva de la casa (la
    /// investigación v6.40: research/tajos_v640/INFORME_TAJOS.md).
    ///
    /// LA ANATOMÍA (medida de la investigación):
    ///   · LA FORMA: un CRECIENTE (arco de círculo) con el GROSOR
    ///     MODULADO a lo largo — máximo al centro, fino en las puntas
    ///     (el perfil de lente G(u) = sin(π·u)^0.55).
    ///   · EL PINCEL: LAS BANDAS UNIFORMES de v6.39 (BoltHalo/BoltCore —
    ///     uniformes A LO LARGO de la textura): los arcos quedan
    ///     CONTINUOS de punta a punta sin cuentas en las juntas (la
    ///     cápsula radial se desvanece en sus extremos y sembraba
    ///     collares de perlas — medido en el mock v6.40: depresiones de
    ///     24 px en cada junta; con banda + solape de 2px, continuo).
    ///   · LAS CAPAS: HALO (BoltHalo, ancho, dorado, PARPADEA — el
    ///     parpadeo vive SOLO aquí) + NÚCLEO (BoltCore, fino,
    ///     blanco-caliente, NUNCA parpadea — el filo es limpio) + el ECO
    ///     interior (el rastro fantasma a radio ×0.88).
    ///   · EL CRECIMIENTO: revelado DIRECCIONAL — los segmentos del arco
    ///     nacen por orden de parámetro (de la punta izquierda a la
    ///     derecha, o al revés si el llamador invierte los ángulos) con
    ///     el FRENTE DE REVELADO ardiendo en la punta del crecimiento.
    ///   · LA VIDA: la campana asimétrica del anime la lleva el
    ///     LLAMADOR (brillo); la librería solo pinta el instante.
    ///   · LAS PUNTAS: cuando el revelado termina, las dos puntas del
    ///     creciente prenden (puntos de luz + chispas que escapan).
    ///
    /// EL TINTE: RGB intacto, alfa = f (el de las librerías rúnicas): el
    /// lote aditivo REAL de FNA (SourceAlpha/One — verificado contra el
    /// IL del FNA.dll en v6.40) GATEA por el alfa → el brillo responde
    /// LINEAL a la intensidad. Las bandas llevan el perfil premultiplicado
    /// en su RGB (la convención v6.39) → los bordes del filo salen suaves.
    /// Cero Main.rand: todo el azar vive en Hash01.
    ///
    /// CONTRATO (Sección A de la casa): batch ABIERTO en aditivo →
    /// ABIERTO. Coordenadas de PANTALLA (el llamador resta
    /// Main.screenPosition; el batch lleva la GameViewMatrix).
    /// </summary>
    public static class TajoLib
    {
        // ==================================================================
        //  v6.49 — EL GOLPE DEL ARCO (la idea nº2 de la auditoría AUD-B:
        //  los tajos son ARCOS que golpeaban como RECTAS — la hitbox no
        //  acompañaba al dibujo). La primitiva espejo de RiftLib.LineaToca:
        //  subdivide el arco en segmentos y prueba la cápsula de cada uno
        //  contra la hitbox (el motor decide — la casa no inventa colisión).
        // ==================================================================

        /// <summary>
        /// ¿El ARCO toca la hitbox? (centro + radio + sector ang0→ang1 +
        /// ancho de filo). Subdivide el arco en N segmentos (N por la
        /// longitud: ~12 px por segmento, 4..24) y prueba cada segmento
        /// con Collision.CheckAABBvLineCollision sobre la hitbox INFLADA
        /// por el medio-ancho del filo — cápsula curva por segmentos
        /// rectos, la misma escuela de la casa.
        /// Corre en LÓGICA (el llamador la usa en su tick de daño, con su
        /// guard de netMode y su cooldown — la librería solo responde).
        /// </summary>
        /// <param name="centro">Centro del arco (coords de MUNDO).</param>
        /// <param name="radio">Radio del arco (px).</param>
        /// <param name="ang0">Ángulo inicial (radianes).</param>
        /// <param name="ang1">Ángulo final (radianes; el arco va de ang0 a ang1).</param>
        /// <param name="ancho">Ancho del filo (px).</param>
        /// <param name="hitbox">La hitbox a probar (la del NPC o jugador).</param>
        public static bool ArcoToca(Vector2 centro, float radio, float ang0, float ang1,
            float ancho, Rectangle hitbox)
        {
            if (radio < 1f || ancho <= 0f) return false;

            // EL ARCO NORMALIZADO: barre siempre hacia adelante (el
            // llamador puede invertir las puntas — el Tajo lo permite).
            float barrido = ang1 - ang0;
            while (barrido > MathHelper.TwoPi) barrido -= MathHelper.TwoPi;
            while (barrido < -MathHelper.TwoPi) barrido += MathHelper.TwoPi;

            // LA SUBDIVISIÓN: ~12 px de cuerda por segmento (4..24).
            float longitud = MathF.Abs(barrido) * radio;
            int segs = (int)MathHelper.Clamp(longitud / 12f, 4f, 24f);

            // Broad-phase: el AABB del arco entero contra la hitbox.
            float rTotal = radio + ancho * 0.5f;
            var caja = new Rectangle(
                (int)(centro.X - rTotal), (int)(centro.Y - rTotal),
                (int)(rTotal * 2f), (int)(rTotal * 2f));
            if (!caja.Intersects(hitbox)) return false;

            // Narrow-phase: cápsula por segmento (hitbox inflada por el
            // medio-filo, la fórmula de RiftLib.LineaToca).
            float r = MathF.Max(ancho * 0.5f, 4f);
            var box = new Rectangle(
                hitbox.X - (int)r, hitbox.Y - (int)r,
                hitbox.Width + (int)r * 2, hitbox.Height + (int)r * 2);
            var boxPos = new Vector2(box.X, box.Y);
            var boxTam = new Vector2(box.Width, box.Height);

            Vector2 prev = centro + new Vector2(MathF.Cos(ang0), MathF.Sin(ang0)) * radio;
            for (int i = 1; i <= segs; i++)
            {
                float a = ang0 + barrido * (i / (float)segs);
                Vector2 pt = centro + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radio;
                if (Collision.CheckAABBvLineCollision(boxPos, boxTam, prev, pt))
                    return true;
                prev = pt;
            }
            return false;
        }

        // ==================================================================
        //  EL PINCEL (las bandas uniformes v6.39 + el glow de puntos)
        // ==================================================================

        private static Asset<Texture2D> _banda;   // BoltHalo: el halo ancho
        private static Asset<Texture2D> _vena;    // BoltCore: el filo fino
        private static Asset<Texture2D> _glow;    // SoftGlow: los puntos

        private static Texture2D Banda =>
            (_banda ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/BoltHalo")).Value;

        private static Texture2D Vena =>
            (_vena ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/BoltCore")).Value;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        /// <summary>Tinte de intensidad (RGB intacto, alfa = f — el de las
        /// librerías rúnicas: el lote aditivo real GATEA por el alfa).</summary>
        public static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        // ==================================================================
        //  LA PRIMITIVA — EL Tajo (la media luna completa)
        // ==================================================================

        /// <summary>Segmentos del arco (20: curva suave con deltas de
        /// ángulo pequeños — los quads banda quedan continuos).</summary>
        public const int Segmentos = 20;

        /// <summary>
        /// UN Tajo — la media luna del anime en UN instante de su vida.
        ///
        /// CONTRATO: batch ABIERTO en aditivo → ABIERTO.
        /// </summary>
        /// <param name="centro">Centro del círculo del arco (coords de PANTALLA).</param>
        /// <param name="radio">Radio del arco (px — crece con la vida si el llamador lo escala).</param>
        /// <param name="ang0">Ángulo inicial (rad — el revelado EMPIEZA aquí).</param>
        /// <param name="ang1">Ángulo final (rad — el revelado TERMINA aquí: invierte la pareja para invertir el sentido).</param>
        /// <param name="prog">Progreso del revelado 0..1 (los segmentos nacen por orden).</param>
        /// <param name="brillo">La campana de vida 0..1 (la lleva el llamador).</param>
        /// <param name="ancho">Grosor VISIBLE del filo (px, máximo en el centro del arco).</param>
        /// <param name="halo">Color del halo (el color del tajo).</param>
        /// <param name="nucleo">Color del núcleo (blanco-caliente de la casa).</param>
        /// <param name="seed">Semilla determinista (puntas y parpadeo).</param>
        /// <param name="time">Tiempo animado (el parpadeo del halo).</param>
        public static void Tajo(Vector2 centro, float radio, float ang0, float ang1,
            float prog, float brillo, float ancho, Color halo, Color nucleo,
            int seed, float time)
        {
            if (brillo <= 0.02f || radio < 6f || ancho < 0.6f) return;
            prog = MathHelper.Clamp(prog, 0f, 1f);
            if (prog <= 0.01f) return;

            float span = ang1 - ang0;

            // --- 1. EL ECO FANTASMA (el rastro interior del anime: un
            //     segundo arco a radio ×0.88, finísimo, apagándose). ---
            for (int i = 0; i < Segmentos; i++)
            {
                float u0 = i / (float)Segmentos;
                if (u0 >= prog) break;
                float um = (u0 + (i + 1f) / Segmentos) * 0.5f;
                float gu = PerfilLente(um);
                SegmentoArco(Banda, centro, radio * 0.88f, ang0, span, u0, um,
                    ancho * 1.9f * gu,
                    Tint(nucleo, 0.14f * brillo * (0.4f + 0.6f * gu)));
            }

            // --- 2. EL HALO (el color: BoltHalo ancho, PARPADEA — el
            //     parpadeo vive SOLO aquí, nunca en el núcleo). ---
            for (int i = 0; i < Segmentos; i++)
            {
                float u0 = i / (float)Segmentos;
                if (u0 >= prog) break;
                float um = (u0 + (i + 1f) / Segmentos) * 0.5f;
                float gu = PerfilLente(um);
                float flick = 0.82f + 0.18f *
                    (float)Math.Sin(time * 38f + i * 2.7f + seed % 7);
                SegmentoArco(Banda, centro, radio, ang0, span, u0, um,
                    ancho * 6.2f * (0.45f + 0.55f * gu),
                    Tint(halo, 0.20f * brillo * flick * (0.35f + 0.65f * gu)));
            }

            // --- 3. EL NÚCLEO (el filo blanco-caliente: BoltCore fino,
            //     limpio, sin parpadeo, el grosor de la lente). ---
            for (int i = 0; i < Segmentos; i++)
            {
                float u0 = i / (float)Segmentos;
                if (u0 >= prog) break;
                float um = (u0 + (i + 1f) / Segmentos) * 0.5f;
                float gu = PerfilLente(um);
                SegmentoArco(Vena, centro, radio, ang0, span, u0, um,
                    ancho * 2.8f * (0.40f + 0.60f * gu),
                    Tint(nucleo, 0.95f * brillo * (0.40f + 0.60f * gu)));
            }

            // --- 4. EL FRENTE DEL REVELADO (la punta del crecimiento
            //     ardiendo: el punto + la cruz de luz — SoftGlow, el
            //     pincel CORRECTO para puntos). ---
            if (prog < 1f)
            {
                float angFront = ang0 + span * prog;
                Vector2 pFront = PuntoArco(centro, radio, angFront);
                Quad(Glow, pFront, new Vector2(ancho * 2.6f, ancho * 2.6f), 0f,
                    Tint(nucleo, 0.65f * brillo));
                Quad(Glow, pFront, new Vector2(ancho * 4.8f, ancho * 0.65f), 0f,
                    Tint(nucleo, 0.45f * brillo));
                Quad(Glow, pFront, new Vector2(ancho * 0.65f, ancho * 4.8f), 0f,
                    Tint(nucleo, 0.45f * brillo));
            }

            // --- 5. LAS PUNTAS (cuando el creciente está completo, las
            //     puntas prenden y sueltan su chispa). ---
            if (prog >= 0.999f)
            {
                PrenderPunta(centro, radio, ang0, ancho, halo, nucleo, brillo, seed);
                PrenderPunta(centro, radio, ang1, ancho, halo, nucleo, brillo, seed + 3);
            }
        }

        /// <summary>
        /// UN SEGMENTO del arco con la BANDA UNIFORME: el quad cubre el
        /// tramo u0→u1 del arco, girado a la tangente del punto medio, con
        /// el solape de 2 px que empalma los empalmes (la banda no se
        /// desvanece a lo largo — la costura no existe).
        /// </summary>
        private static void SegmentoArco(Texture2D tex, Vector2 centro, float radio,
            float ang0, float span, float u0, float um, float altoQuad, Color tint)
        {
            if (tint.A == 0) return;
            float aU = ang0 + span * u0;
            float aV = ang0 + span * (u0 + 1f / Segmentos);
            Vector2 pa = PuntoArco(centro, radio, aU);
            Vector2 pb = PuntoArco(centro, radio, aV);
            Vector2 mid = (pa + pb) * 0.5f;
            Vector2 delta = pb - pa;
            float len = delta.Length();
            if (len < 0.05f) return;
            float rot = (float)Math.Atan2(delta.Y, delta.X);

            // La tangente del punto medio (el quad se gira al filo REAL del
            // arco — el solape de 2px cubre la cuña del giro).
            _ = um;
            Main.spriteBatch.Draw(tex, mid, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                new Vector2((len + 2f) / tex.Width, altoQuad / tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>
        /// LA PUNTA del creciente completa: el punto de luz + la chispa
        /// que escapa del filo (determinista por Hash01).
        /// </summary>
        private static void PrenderPunta(Vector2 centro, float radio, float ang,
            float ancho, Color halo, Color nucleo, float brillo, int seed)
        {
            Vector2 p = PuntoArco(centro, radio, ang);
            Quad(Glow, p, new Vector2(ancho * 2.2f, ancho * 2.2f), 0f,
                Tint(halo, 0.55f * brillo));
            Quad(Glow, p, new Vector2(ancho * 1.0f, ancho * 1.0f), 0f,
                Tint(nucleo, 0.92f * brillo));

            // La chispa que escapa (hacia afuera del arco).
            float h = VFXCore.Hash01(seed, 17, 3);
            if (h > 0.4f)
            {
                Vector2 dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
                Vector2 sp = p + dir * (2f + 3f * h) * (ancho * 0.6f);
                Quad(Glow, sp, new Vector2(ancho * 1.4f, ancho * 1.4f), 0f,
                    Tint(nucleo, 0.5f * brillo * h));
            }
        }

        // ==================================================================
        //  LOS HELPERS (privados, el pincel de la casa)
        // ==================================================================

        /// <summary>El PERFIL DE LENTE del creciente: grosor máximo al
        /// centro, fino en las puntas (la media luna).
        /// v6.49 — POR LUT (hallazgo AUD-B): se llamaba ~60 veces por tajo
        /// por frame (20 segmentos × 3 capas) con Math.Pow cada vez — la
        /// curva completa es UNA tabla de 33 muestras, interpolada.</summary>
        public static float PerfilLente(float u)
        {
            u = MathHelper.Clamp(u, 0f, 1f) * (_lenteLutN - 1);
            int i0 = (int)u;
            int i1 = i0 + 1 < _lenteLutN ? i0 + 1 : i0;
            float k = u - i0;
            return _lenteLut[i0] + (_lenteLut[i1] - _lenteLut[i0]) * k;
        }

        private const int _lenteLutN = 33;
        private static readonly float[] _lenteLut = BuildLenteLut();

        private static float[] BuildLenteLut()
        {
            var lut = new float[_lenteLutN];
            for (int i = 0; i < _lenteLutN; i++)
            {
                float s = (float)Math.Sin(Math.PI * (i / (float)(_lenteLutN - 1)));
                lut[i] = (float)Math.Pow(Math.Max(s, 0.001f), 0.55f);
            }
            return lut;
        }

        /// <summary>Punto del arco (centro + radio + ángulo).</summary>
        private static Vector2 PuntoArco(Vector2 centro, float radio, float ang)
            => centro + new Vector2((float)Math.Cos(ang) * radio, (float)Math.Sin(ang) * radio);

        /// <summary>Quad centrado al batch actual (tamaño total = size px).</summary>
        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>
        /// LA CAMPANA DE VIDA del anime (asimétrica: subida rápida al
        /// 55%, caída larga al 45% restante — la medición de la
        /// investigación). t = 0..1 de la vida.
        /// </summary>
        public static float Campana(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            float subida = MathHelper.Clamp(t / 0.55f, 0f, 1f);
            float caida = MathHelper.Clamp((1f - t) / 0.45f, 0f, 1f);
            return subida * caida;
        }
    }
}
