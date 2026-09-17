using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// NebulaLib — v6.34 — LAS NEBULOSAS VOLUMÉTRICAS DE LA CASA.
    ///
    /// Nubes de gas que NADAN: cada voluta es un quad grande de SoftGlow
    /// que sigue un CAMPO DE RUIDO — no orbita como un péndulo ni flota
    /// con un seno solitario, sino que el gas CIRCULA de verdad. La casa
    /// simula el volumen con CAPAS (10-24 velos superpuestos, como
    /// siempre: cero shaders nuevos, el SpriteBatch es el motor), y el
    /// movimiento sale de CURL NOISE: la derivada PERPENDICULAR de un
    /// ruido de valor — un flujo sin divergencia que jamás acumula las
    /// volutas en un punto; giran alrededor de los "ojos" del campo,
    /// exactamente como el gas de una nebulosa de verdad.
    ///
    /// (La casa ya tiene BrumaNoise para el humo; aquí el contrato es una
    /// librería AUTOCONTENIDA de quads: el ValueNoise2D+Curl viven AQUÍ,
    /// ~30 líneas a mano con smoothstep e interpolación bilinear, cero
    /// dependencias cruzadas — Bruma oculta, Nebula emite luz.)
    ///
    /// VELOS, NO FUEGOS: el alpha por voluta vive en 0.05-0.14 (se clampea
    /// — la bibliografía de la casa: un velo que se pasa a 0.3 ya es un
    /// incendio). Con 14 volutas superpuestas el CORAZÓN densifica solo,
    /// hasta ~1.0 efectivo en el centro, y los bordes mueren en nada.
    ///
    /// DETERMINISMO TOTAL (el contrato de BoltRenderer/StormLib): TODO se
    /// deriva de (time, seed) con VFXCore.Hash01 — la MISMA nebulosa en
    /// todas las máquinas, sin estado, sin red, sin GC por frame. El time
    /// va en SEGUNDOS (Main.GlobalTimeWrappedHourly es el estándar de la
    /// casa).
    ///
    /// USO (el patrón de dibujo de la casa — VFXCore.Quad como pincel):
    /// <code>
    ///   VFXCore.Begin();
    ///   NebulaLib.Nube(p.Center, 180f, Main.GlobalTimeWrappedHourly,
    ///       p.whoAmI, colCorazon, colBorde, 0.10f);
    ///   VFXCore.FlushAdditive();
    /// </code>
    /// (o varias llamadas ANIDADAS antes del Flush — el buffer es de la
    /// casa). Las nebulosas EMITEN luz → lote ADITIVO. La luz del MUNDO
    /// (AddLight) la pone el llamador: aquí la biblioteca solo dibuja,
    /// como BoltRenderer.
    /// </summary>
    public static class NebulaLib
    {
        // ==================================================================
        //  EL CAMPO — ValueNoise2D + Curl, a mano y sin librerías
        // ==================================================================

        /// <summary>
        /// Hash de ESQUINA para el ruido de valor: un [0,1) determinista
        /// por celda entera (el patrón VFXCore.Hash01 de la casa).
        /// </summary>
        private static float RuidoEsquina(int x, int y) => VFXCore.Hash01(9781, x, y);

        /// <summary>
        /// VALUE NOISE 2D a mano: valor aleatorio por cada esquina de la
        /// celda, suavizado con smoothstep e interpolado BILINEAL — la
        /// forma mínima honesta de un campo continuo determinista (~30
        /// líneas, cero dependencias).
        /// </summary>
        private static float ValueNoise2D(float x, float y)
        {
            int xi = (int)MathF.Floor(x);
            int yi = (int)MathF.Floor(y);

            // Smoothstep sobre la fracción de celda (la interpolación
            // suave que separa el ruido "cremoso" del "tablero de ajedrez").
            float u = Suave01(x - xi);
            float v = Suave01(y - yi);

            // Las cuatro esquinas de la celda (diferencias centrales del
            // Curl2D piden vecinos: Floor las alinea siempre).
            float a = RuidoEsquina(xi, yi);
            float b = RuidoEsquina(xi + 1, yi);
            float c = RuidoEsquina(xi, yi + 1);
            float d = RuidoEsquina(xi + 1, yi + 1);

            // Bilineal: primero en X, luego en Y.
            return Lerp(Lerp(a, b, u), Lerp(c, d, u), v);
        }

        /// <summary>
        /// CURL del campo escalar n: (∂n/∂y, −∂n/∂x) por diferencias
        /// centrales — la derivada PERPENDICULAR del ruido. Un flujo sin
        /// divergencia: lo que una voluta deja atrás lo ocupa otra, y el
        /// gas nunca colapsa en un punto (los campos incompresibles de la
        /// bibliografía de fluidos, reducidos a cuatro lecturas de ruido).
        /// </summary>
        private static Vector2 Curl2D(float x, float y)
        {
            const float e = 0.65f;   // paso de la derivada (celdas de ruido)
            float dndx = (ValueNoise2D(x + e, y) - ValueNoise2D(x - e, y)) / (2f * e);
            float dndy = (ValueNoise2D(x, y + e) - ValueNoise2D(x, y - e)) / (2f * e);
            return new Vector2(dndy, -dndx);
        }

        /// <summary>Smoothstep 0..1 (t²·(3−2t) con clamp).</summary>
        private static float Suave01(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        /// <summary>Lerp lineal (el de siempre, escrito para no encadenar llamadas).</summary>
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        // ==================================================================
        //  NUBE — la nebulosa esférica
        // ==================================================================

        /// <summary>
        /// DIBUJA UNA NEBULOSA: <paramref name="volutas"/> velos de SoftGlow
        /// orbitando LENTO alrededor del centro (velocidades distintas por
        /// voluta, algunas CW otras CCW — la nube jamás gira en bloque),
        /// ADVECTADAS por curl noise (el campo deriva con el tiempo: el gas
        /// circula), respirando ±20% con fases propias y mezclando
        /// colorA→colorB según su distancia al corazón.
        /// </summary>
        /// <param name="centro">Centro en coords de MUNDO.</param>
        /// <param name="radio">Radio de la nube en px.</param>
        /// <param name="time">Tiempo en SEGUNDOS (GlobalTimeWrappedHourly).</param>
        /// <param name="seed">Semilla determinista (misma = misma nube).</param>
        /// <param name="colorA">Color del CORAZÓN (cerca del centro).</param>
        /// <param name="colorB">Color del BORDE (lejos del centro).</param>
        /// <param name="alpha">Alpha POR Voluta — se clampea a 0.05-0.14
        /// (VELOS, no fuegos; el corazón densifica solo al superponerse).</param>
        /// <param name="volutas">Cuántos velos (4-24, por defecto 14).</param>
        public static void Nube(Vector2 centro, float radio, float time, int seed,
            Color colorA, Color colorB, float alpha, int volutas = 14)
        {
            // (v6.27 — la lección NaN de la casa: un radio/tiempo NaN aquí
            // haría quads en ninguna parte; se rechaza el dibujo entero).
            if (!float.IsFinite(radio) || !float.IsFinite(time) || radio < 2f) return;
            if (Main.netMode == NetmodeID.Server) return;   // cosmético de cliente

            volutas = Math.Clamp(volutas, 4, 24);
            alpha = MathHelper.Clamp(alpha, 0.05f, 0.14f);

            // LOD de la casa: con el presupuesto de cuadros apretado, MENOS
            // volutas (la nube respira igual y cuesta menos fill-rate).
            if (!VFXCore.Presupuesto(volutas))
                volutas = Math.Max(4, volutas / 2);

            for (int i = 0; i < volutas; i++)
            {
                // Los siete dados de la voluta: ángulo, distancia, tamaño,
                // velocidad, sentido, fase y alfa — TODO por (seed, i).
                float hAng = VFXCore.Hash01(seed, i, 11);
                float hDist = VFXCore.Hash01(seed, i, 22);
                float hTam = VFXCore.Hash01(seed, i, 33);
                float hVel = VFXCore.Hash01(seed, i, 44);
                float hDir = VFXCore.Hash01(seed, i, 55);
                float hFase = VFXCore.Hash01(seed, i, 66);
                float hAlfa = VFXCore.Hash01(seed, i, 77);

                // ÓRBITA LENTA: velocidades DISTINTAS por voluta y sentido
                // mixto — un enjambre de rotaciones, no una rueda.
                float sentido = hDir > 0.5f ? 1f : -1f;
                float ang = hAng * MathHelper.TwoPi + time * sentido * (0.05f + 0.11f * hVel);
                float dist = radio * (0.18f + 0.60f * hDist);
                Vector2 orbita = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * dist;

                // ADVECCIÓN: el voluta muestrea el campo en su propia
                // posición mientras el CAMPO deriva (dos velocidades
                // inconmensurables) — el gas circula, no oscila en sitio.
                Vector2 curl = Curl2D(
                    (centro.X + orbita.X) * 0.012f + time * 0.230f + hFase * 7f,
                    (centro.Y + orbita.Y) * 0.012f + time * 0.147f + hFase * 13f);

                // Refuerzo de dos senos INCONMENSURABLES (la regla de la
                // casa: un solo seno = péndulo).
                Vector2 vaiven = new Vector2(
                    MathF.Sin(time * 0.31f + hFase * MathHelper.TwoPi),
                    MathF.Sin(time * 0.53f + hAng * MathHelper.TwoPi)) * (radio * 0.06f);

                Vector2 pos = centro + orbita + curl * (radio * 0.22f) + vaiven;

                // RESPIRACIÓN: radio ±20%, fases DISTINTAS por voluta.
                float respira = 1f + 0.20f *
                    MathF.Sin(time * (0.45f + 0.55f * hTam) + hFase * MathHelper.TwoPi);
                float rVoluta = radio * (0.34f + 0.30f * hTam) * respira;

                // COLOR por DISTANCIA al corazón: colorA dentro → colorB en
                // el borde (la mezcla VIAJA con la voluta al orbitar).
                float mezcla = MathHelper.Clamp(
                    (pos - centro).Length() / MathF.Max(radio, 1f), 0f, 1f);
                Color c = Color.Lerp(colorA, colorB, mezcla);

                // Alpha de VELO con su ±20% por voluta (dentro del rango).
                float a = MathHelper.Clamp(alpha * (0.80f + 0.45f * hAlfa), 0.04f, 0.14f);

                VFXCore.Quad(pos, c * a, new Vector2(rVoluta * 2f, rVoluta * 2f));
            }
        }

        // ==================================================================
        //  COLUMNA — la nebulosa que asciende (géiseres, portales verticales)
        // ==================================================================

        /// <summary>
        /// DIBUJA UNA COLUMNA DE GAS que asciende desde
        /// <paramref name="base_"/>: 10-16 volutas apiladas que se elevan
        /// en BUCLE (y = (y0 + time·vel) % altura — nacen en la base,
        /// mueren en el techo y vuelven a nacer), ENSANCHÁNDOSE hacia
        /// arriba (el gas se expande al liberarse) y desvaneciéndose en el
        /// techo (nunca un corte en seco). Advección vertical por el bucle
        /// + curl horizontal cada vez más suelto con la altura. El color
        /// cuenta la historia térmica: colorA arde en la base → colorB se
        /// disipa arriba.
        /// </summary>
        /// <param name="base_">Pie de la columna en coords de MUNDO.</param>
        /// <param name="altura">Altura total en px.</param>
        /// <param name="ancho">Ancho en la BASE en px (ensancha hacia arriba).</param>
        /// <param name="time">Tiempo en SEGUNDOS (GlobalTimeWrappedHourly).</param>
        /// <param name="seed">Semilla determinista (misma = misma columna).</param>
        /// <param name="colorA">Color de la BASE (el fuego del géiser).</param>
        /// <param name="colorB">Color del TECHO (el gas disipándose).</param>
        /// <param name="alpha">Alpha POR Voluta — se clampea a 0.05-0.14
        /// (VELOS: la columna densifica por superposición, no por fuerza).</param>
        public static void Columna(Vector2 base_, float altura, float ancho, float time,
            int seed, Color colorA, Color colorB, float alpha)
        {
            // (v6.27 — la lección NaN de la casa, igual que Nube).
            if (!float.IsFinite(altura) || !float.IsFinite(ancho) || !float.IsFinite(time) ||
                altura < 8f || ancho < 2f) return;
            if (Main.netMode == NetmodeID.Server) return;   // cosmético de cliente

            // 10-16 volutas según la esbeltez de la columna.
            int volutas = Math.Clamp((int)(altura / (ancho * 0.85f)), 10, 16);

            alpha = MathHelper.Clamp(alpha, 0.05f, 0.14f);

            // LOD de la casa (mismo gate que Nube).
            if (!VFXCore.Presupuesto(volutas))
                volutas = Math.Max(6, volutas * 2 / 3);

            // Velocidad de ascenso INVARIANTE DE ESCALA (la casa: velocidades
            // constantes INDEPENDIENTES del tamaño — rotaba el humo a rad/s
            // fijos): la columna completa recicla en ~2.9 s, sea géiser
            // enano o surtidor de jefe final.
            float vel = altura * 0.35f;

            for (int i = 0; i < volutas; i++)
            {
                float h0 = VFXCore.Hash01(seed, i, 111);
                float h1 = VFXCore.Hash01(seed, i, 122);
                float h2 = VFXCore.Hash01(seed, i, 133);
                float h3 = VFXCore.Hash01(seed, i, 144);
                float hFase = VFXCore.Hash01(seed, i, 155);

                // EL BUCLE de ascenso: y0 determinista + time·vel, envuelto
                // en la altura — la voluta nace, sube, se disipa y RENACE.
                float yRel = (h0 * altura + time * vel) % altura;
                float frac = yRel / altura;      // 0 base → 1 techo
                float y = base_.Y - yRel;

                // ENSANCHA hacia arriba + DESVANECE en el techo + nace suave
                // en el pie (el wrap del bucle jamás se ve: arriba ya era
                // invisible, abajo entra en fundido).
                float anchoAqui = ancho * (0.55f + 0.80f * frac);
                float techo = 1f - Suave01((frac - 0.72f) / 0.28f);
                float pie = Suave01(frac / 0.08f);

                // Advección horizontal por CURL (más suelta cuanto más
                // alto: el gas liberado obedece menos al géiser) + dos
                // senos inconmensurables de vaivén.
                Vector2 curl = Curl2D(
                    base_.X * 0.012f + time * 0.230f + hFase * 11f,
                    (base_.Y - yRel) * 0.012f + time * 0.147f + hFase * 17f);
                float x = base_.X
                    + curl.X * ancho * (0.25f + 0.55f * frac)
                    + MathF.Sin(time * 0.33f + hFase * MathHelper.TwoPi) * ancho * 0.10f * (0.3f + frac)
                    + MathF.Sin(time * 0.71f + h1 * MathHelper.TwoPi) * ancho * 0.05f;

                // RESPIRACIÓN ±20% con fase y tamaño propios.
                float respira = 1f + 0.20f *
                    MathF.Sin(time * (0.5f + 0.6f * h2) + h3 * MathHelper.TwoPi);
                float rVoluta = anchoAqui * 0.5f * respira * (0.85f + 0.30f * h2);

                // La historia térmica: colorA abajo → colorB arriba.
                Color c = Color.Lerp(colorA, colorB, frac);

                float a = MathHelper.Clamp(alpha * (0.80f + 0.45f * h3), 0.04f, 0.14f)
                    * techo * pie;

                VFXCore.Quad(new Vector2(x, y), c * a, new Vector2(rVoluta * 2f, rVoluta * 2f));
            }
        }
    }
}
