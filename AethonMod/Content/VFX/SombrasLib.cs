using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// SOMBRASLIB — v6.50.62 — LA LIBRERÍA DE LAS SOMBRAS DEVORADORAS.
    ///
    /// El lenguaje visual de Pride (Selim Bradley, FMA:B) traducido a
    /// primitivas de la casa: masa NEGRA que TAPA (lote alfa — el negro
    /// aditivo es invisible, para oscurecer hay que salir del aditivo),
    /// OJOS blancos que se abren de golpe y MIRAN (lote aditivo), DIENTES
    /// blancos triangulares, CHARCOS de sombra en el suelo y BRUMA que
    /// se come la luz. Todo determinista (semillas por whoAmI) y sin
    /// sprites: Pixel + SoftGlow + GlowOrb, los tres pinceles del motor.
    ///
    /// CONTRATO DE LOTES (la lección de la .58): cada método de dibujo
    /// abre y CIERRA sus propios lotes vía VFXCore.Flush* — el llamador
    /// (un PreDraw de la casa) hace CerrarLoteSiAbierto() antes y
    /// ReabrirLoteVanilla() al final. Aquí dentro jamás queda un Begin
    /// vivo: los vuelcos son atómicos por diseño.
    /// </summary>
    public static class SombrasLib
    {
        // ==================================================================
        //  LA PALETA DE PRIDE — negro absoluto, blanco de hueso, rojo
        //  visceral (la luz que MATA la sombra es la única con color).
        // ==================================================================

        /// <summary>El negro de la masa (casi absoluto: 8/4/7, un pelo de violeta para que se lea sobre fondo negro).</summary>
        public static readonly Color Negro = new(8, 4, 7);

        /// <summary>El blanco de los ojos y los dientes (hueso cálido, no clínico).</summary>
        public static readonly Color Blanco = new(250, 248, 244);

        /// <summary>El rojo de las pupilas y las fauces abiertas.</summary>
        public static readonly Color Rojo = new(198, 18, 24);

        /// <summary>Rojo profundo para el interior de las bocas (garganta).</summary>
        public static readonly Color RojoGarganta = new(120, 8, 12);

        // ==================================================================
        //  EL CUERPO — la polilínea orgánica (la columna del tentáculo)
        // ==================================================================

        /// <summary>
        /// LA COLUMNA VERTEBRAL DEL TENTÁCULO: N puntos de la RAÍZ a la
        /// CABEZA siguiendo una Bézier cuadrática whose control point
        /// ondula con el tiempo — el flujo líquido de Pride: la masa NUNCA
        /// va recta, SIEMPRE serpentea (y serpentea MÁS cuanto más larga,
        /// la física del látigo). Determinista por semilla.
        /// </summary>
        /// <param name="raiz">Nacimiento (el jugador / su sombra).</param>
        /// <param name="cabeza">La boca (el jefe).</param>
        /// <param name="tiempo">GlobalTimeWrappedHourly.</param>
        /// <param name="semilla">Semilla determinista (whoAmI·k).</param>
        /// <param name="puntos">Cantidad de puntos (18 por defecto).</param>
        /// <param name="amplitud">0..1 — cuánto serpentrea (0.22 = vivo).</param>
        public static Vector2[] Columna(Vector2 raiz, Vector2 cabeza, float tiempo, int semilla,
            int puntos = 18, float amplitud = 0.22f)
        {
            puntos = Math.Max(puntos, 2);
            Vector2[] col = new Vector2[puntos];

            float dist = Vector2.Distance(raiz, cabeza);
            // el látigo largo ondea MÁS (la amplitud vive con la longitud)
            float escala = Math.Min(dist / 420f, 2.2f);
            Vector2 medio = (raiz + cabeza) * 0.5f;
            Vector2 perp = new Vector2(-(cabeza - raiz).Y, (cabeza - raiz).X);
            perp = perp.LengthSquared() < 1f ? Vector2.UnitY : Vector2.Normalize(perp);

            // DOS ondas superpuestas (lenta + viva): el vaivén orgánico
            float onda = MathF.Sin(tiempo * 1.35f + semilla * 0.71f) * 0.62f
                       + MathF.Sin(tiempo * 3.1f + semilla * 1.37f) * 0.38f;
            // la fase lenta DESPLAZA el punto de control, la viva lo tiembla
            float vivo = 1f + 0.18f * MathF.Sin(tiempo * 5.3f + semilla);
            Vector2 ctrl = medio + perp * (onda * dist * amplitud * escala * vivo * 0.5f);

            for (int i = 0; i < puntos; i++)
            {
                float t = i / (float)(puntos - 1);
                // Bézier cuadrática raíz→ctrl→cabeza
                float u = 1f - t;
                col[i] = u * u * raiz + 2f * u * t * ctrl + t * t * cabeza;
            }
            return col;
        }

        /// <summary>
        /// DIBUJA LA MASA: cada segmento de la columna es un LOTE ALFA de
        /// negro sólido (Pixel) con grosor decreciente raíz→punta, y una
        /// segunda pasada SoftGlow oscura ×1.7 con alpha baja — el borde
        /// SUAVE de la sombra (la masa no termina en cuchillo: respira).
        /// La punta (últimos 15%) lleva un abanicado de PICOS: el borde
        /// DENTADO de Pride (la sombra afilada como sierra).
        /// </summary>
        public static void Masa(Vector2[] col, float grosorRaiz, float grosorPunta, float alfa, int semilla, float tiempo)
        {
            if (col == null || col.Length < 2 || alfa <= 0.02f) return;

            VFXCore.Begin();
            int n = col.Length;
            for (int i = 1; i < n; i++)
            {
                float f = i / (float)(n - 1);           // 0 raíz → 1 punta
                float g = MathHelper.Lerp(grosorRaiz, grosorPunta, f);
                // el latido: la masa RESPIRA a lo largo (la ondulación de energía viva)
                g *= 0.9f + 0.1f * MathF.Sin(tiempo * 6.2f + f * 7f + semille(semilla));
                VFXCore.Line(col[i - 1], col[i], Alfa(Negro, alfa), g);
            }
            VFXCore.FlushAlpha(VFXCore.Pixel);

            // EL BORDE SUAVE (velo oscuro ×1.7)
            VFXCore.Begin();
            for (int i = 1; i < n; i++)
            {
                float f = i / (float)(n - 1);
                float g = MathHelper.Lerp(grosorRaiz, grosorPunta, f) * 1.7f;
                VFXCore.Line(col[i - 1], col[i], Alfa(Negro, alfa * 0.35f), g);
            }
            VFXCore.FlushAlpha();

            // LOS PICOS DE LA SIERRA en la mitad final de la columna
            VFXCore.Begin();
            for (int i = 1; i < n - 2; i++)
            {
                float f = i / (float)(n - 1);
                if (f < 0.45f) continue;
                // picos alternando lado, deterministas
                float fase = MathF.Sin(i * 2.399f + Frac(semille(semilla) * 5.9f) * MathHelper.TwoPi);
                if (fase < 0.35f) continue;
                Vector2 dir = col[i + 1] - col[i - 1];
                if (dir.LengthSquared() < 0.01f) continue;
                dir = Vector2.Normalize(dir);
                Vector2 perp = new(-dir.Y, dir.X);
                float g = MathHelper.Lerp(grosorRaiz, grosorPunta, f) * 0.5f;
                Vector2 pico = col[i] + perp * g * (fase > 0 ? 1f : -1f);
                VFXCore.Line(col[i], pico, Alfa(Negro, alfa * 0.9f), g * 0.35f);
            }
            VFXCore.FlushAlpha(VFXCore.Pixel);
        }

        /// <summary>El charco de sombra del que nace todo (el ancla en el suelo).</summary>
        public static void Charco(Vector2 pos, float radio, float alfa, float tiempo, int semilla)
        {
            if (alfa <= 0.02f) return;
            VFXCore.Begin();
            // tres elipses solapadas ondulando: el charco VIVO
            for (int k = 0; k < 3; k++)
            {
                float w = radio * (1.05f + 0.12f * MathF.Sin(tiempo * 1.7f + k * 2.4f + semille(semilla)));
                float h = w * (0.34f + 0.05f * MathF.Sin(tiempo * 2.3f + k));
                VFXCore.Quad(pos + new Vector2((k - 1) * radio * 0.22f, 0f), Alfa(Negro, alfa * (k == 1 ? 1f : 0.7f)),
                    new Vector2(w, h), VFXCore.GlowOrb);
            }
            VFXCore.FlushAlpha();
        }

        // ==================================================================
        //  LOS OJOS — blancos, se abren DE GOLPE y MIRAN
        // ==================================================================

        /// <summary>
        /// UN OJO: esclerótica blanca elíptica (aditivo), pupila roja
        /// desplazada hacia <paramref name="mirada"/>. El tamaño late
        /// con <paramref name="abierto"/> (0 = cerrado, 1 = bien abierto):
        /// los ojos de Pride NO parpadean suaves — SE ABREN de golpe, y
        /// la apertura se anima con un ease-out violento en el llamador.
        /// </summary>
        public static void Ojo(Vector2 pos, float tamaño, Vector2 mirada, float abierto, bool pupila = true)
        {
            if (abierto <= 0.03f || tamaño <= 0.5f) return;
            float a = MathHelper.Clamp(abierto, 0f, 1f);
            VFXCore.Quad(pos, Alfa(Blanco, 0.92f * a), new Vector2(tamaño, tamaño * 0.62f));
            if (pupila)
            {
                Vector2 m = mirada.LengthSquared() < 0.01f ? Vector2.Zero : Vector2.Normalize(mirada) * tamaño * 0.18f;
                VFXCore.Quad(pos + m, Alfa(Rojo, 0.95f * a), new Vector2(tamaño * 0.34f, tamaño * 0.40f));
            }
        }

        /// <summary>
        /// LOS OJOS DE LA MASA: repartidos por la columna con semilla
        /// fija, mirando TODOS a la misma presa (la firma de Pride: la
        /// masa entera TE MIRA). Cada ojo abre con retraso escalonado
        /// (aparecen de golpe, uno tras otro) y late.
        /// </summary>
        /// <param name="presencia">0..1 — cuántos ojos hay abiertos ya.</param>
        public static void OjosDeMasa(Vector2[] col, Vector2 presa, float presencia, int semilla, float tiempo, int cantidad = 5)
        {
            if (presencia <= 0.02f || col == null || col.Length < 4) return;
            VFXCore.Begin();
            int n = col.Length;
            for (int k = 0; k < cantidad; k++)
            {
                float t01 = 0.24f + 0.66f * ((k * 0.618034f + 0.13f) % 1f);   // áureo: reparto orgánico
                int idx = (int)(t01 * (n - 1));
                Vector2 pos = col[idx];
                // apertura escalonada + el latido del ojo
                float local = presencia * cantidad * 0.9f - k * 0.9f;
                float abierto = MathHelper.Clamp(local, 0f, 1f);
                abierto *= 0.85f + 0.15f * MathF.Sin(tiempo * 3.3f + k * 2.1f + semille(semilla));
                if (abierto <= 0.05f) continue;
                float tamaño = 16f + 9f * Frac(semille(semilla) * 0.173f + k * 0.37f);
                Ojo(pos, tamaño, presa - pos, abierto);
            }
            VFXCore.FlushAdditive();
        }

        // ==================================================================
        //  LAS FAUCES — mandíbulas con dientes (100% procedural)
        // ==================================================================

        /// <summary>
        /// UNA BOCA PROCEDURAL: dos mandíbulas negras en cuña que se
        /// abren en ángulo alrededor de <paramref name="dir"/> (la
        /// apertura 0 = cerrada, 1 = bien abierta), con colmillos
        /// blancos triangulares apuntando al interior y la GARGANTA roja
        /// al fondo. La boca de Pride: se abre DE GOLPE en cualquier
        /// punto de la sombra.
        /// </summary>
        /// <param name="centro">El gozne de la mandíbula.</param>
        /// <param name="dir">Rumbo de la boca (adónde muerde).</param>
        /// <param name="apertura">0..1.</param>
        /// <param name="largo">Largo de las mandíbulas en px.</param>
        public static void Fauces(Vector2 centro, Vector2 dir, float apertura, float largo, int semilla)
        {
            if (largo <= 2f) return;
            apertura = MathHelper.Clamp(apertura, 0f, 1f);
            float ang = dir.LengthSquared() < 0.01f ? 0f : MathF.Atan2(dir.Y, dir.X);
            float sep = apertura * 0.85f + 0.06f;   // ángulo de cada mandíbula

            // --- LA GARGANTA (el fondo rojo, solo si está abierta) ---
            if (apertura > 0.15f)
            {
                VFXCore.Begin();
                VFXCore.Quad(centro + dir * 0f, Alfa(RojoGarganta, 0.55f * apertura),
                    new Vector2(largo * 0.85f, largo * 0.9f), ang, VFXCore.SoftGlow);
                VFXCore.FlushAdditive();
            }

            // --- LAS MANDÍBULAS (dos cuñas negras gruesas) ---
            VFXCore.Begin();
            for (int lado = -1; lado <= 1; lado += 2)
            {
                float aMand = ang + lado * sep;
                Vector2 rumbo = new(MathF.Cos(aMand), MathF.Sin(aMand));
                // la cuña: dos líneas desde el goyne divergentes + relleno central
                Vector2 punta = centro + rumbo * largo;
                Vector2 perp = new Vector2(-rumbo.Y, rumbo.X) * (largo * 0.30f * lado);
                VFXCore.Line(centro, punta + perp, Alfa(Negro, 0.97f), largo * 0.34f);
                VFXCore.Line(centro, punta, Alfa(Negro, 0.97f), largo * 0.22f);
                VFXCore.Line(centro, punta - perp * 0.4f, Alfa(Negro, 0.97f), largo * 0.16f);
            }
            VFXCore.FlushAlpha(VFXCore.Pixel);

            // --- LOS COLMILLOS (blanco hueso, aditivo): 4 arriba, 4 abajo ---
            VFXCore.Begin();
            for (int lado = -1; lado <= 1; lado += 2)
            {
                float aMand = ang + lado * sep;
                Vector2 rumbo = new(MathF.Cos(aMand), MathF.Sin(aMand));
                for (int k = 0; k < 4; k++)
                {
                    float t = 0.30f + k * 0.21f;
                    float tamaño = largo * (0.20f - k * 0.028f);
                    if (tamaño < 3f) continue;
                    Vector2 baseD = centro + rumbo * (largo * t);
                    // el colmillo apunta hacia el interior de la boca (contra el lado)
                    Vector2 haciaDentro = new(MathF.Cos(ang - lado * 0.24f), MathF.Sin(ang - lado * 0.24f));
                    Vector2 puntaD = baseD + haciaDentro * tamaño;
                    // dibujado como línea gruesa blanca rotada (el triángulo del colmillo)
                    VFXCore.Line(baseD, puntaD, Alfa(Blanco, 0.95f), tamaño * 0.42f);
                }
            }
            VFXCore.FlushAdditive(VFXCore.Pixel);
        }

        // ==================================================================
        //  LA BRUMA Y LAS ALMAS
        // ==================================================================

        /// <summary>Nube de bruma oscura que TAPA (lote alfa): la desintegración.</summary>
        public static void Bruma(Vector2 pos, float radio, float alfa, float tiempo, int semilla, Vector2 deriva = default)
        {
            if (alfa <= 0.02f || radio <= 1f) return;
            VFXCore.Begin();
            int nubes = 3;
            for (int k = 0; k < nubes; k++)
            {
                float f = Frac(semille(semilla) * 0.31f + k * 0.443f);
                float ang = f * MathHelper.TwoPi + tiempo * (0.14f + f * 0.12f);
                float d = radio * (0.25f + 0.55f * Frac(f * 7.3f));
                Vector2 p = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.6f) * d + deriva * f;
                float r = radio * (0.42f + 0.4f * Frac(f * 11.7f));
                VFXCore.Quad(p, Alfa(Negro, alfa * (0.5f + 0.5f * Frac(f * 5.1f))), new Vector2(r * 1.3f, r), ang, VFXCore.SoftGlow);
            }
            VFXCore.FlushAlpha();
        }

        /// <summary>Un alma (partícula blanca pequeña, aditivo): lo que vuela de la presa al portador.</summary>
        public static void Alma(Vector2 pos, float tamaño, float alfa)
        {
            if (alfa <= 0.02f) return;
            VFXCore.Begin();
            VFXCore.Quad(pos, Alfa(Blanco, alfa * 0.75f), new Vector2(tamaño, tamaño));
            VFXCore.Quad(pos, Alfa(Blanco, alfa), new Vector2(tamaño * 0.4f, tamaño * 0.4f));
            VFXCore.FlushAdditive();
        }

        // ==================================================================
        //  PEQUEÑOS AJUSTADORES
        // ==================================================================

        /// <summary>Color con alpha escalada (Color·f de la casa).</summary>
        public static Color Alfa(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color((byte)(c.R * f), (byte)(c.G * f), (byte)(c.B * f), (byte)(255f * f));
        }

        /// <summary>Hash determinista [0,1).</summary>
        public static float Frac(float x) => x - MathF.Floor(x);

        /// <summary>La semilla sana (entero → [0,1) determinista, nunca negativa).</summary>
        public static float semille(int s) => Frac(MathF.Sin(s * 12.9898f) * 43758.5453f);

        /// <summary>
        /// EL ACELERADOR DE APERTURA de los ojos/bocas de Pride: nada,
        /// nada… y DE GOLPE. Ease-out explosivo (k=4): 0→1 con casi todo
        /// el salto en el primer 25% del tiempo.
        /// </summary>
        public static float DeGolpe(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return 1f - MathF.Pow(1f - t, 4f);
        }
    }
}
