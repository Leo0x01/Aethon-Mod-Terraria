using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using AethonMod.Content.Effects.Bruma;

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

            // v6.50.65 — EL RIM VIOLETA (la cura del «no se ve nada»): la
            // masa negra sobre fondo negro era INVISIBLE — un borde
            // aditivo violeta tenue (1.35× el grosor) le da a la silueta
            // un aura de sombra mágica que se lee en CUALQUIER fondo.
            VFXCore.Begin();
            for (int i = 1; i < n; i++)
            {
                float f = i / (float)(n - 1);
                float g = MathHelper.Lerp(grosorRaiz, grosorPunta, f) * 1.35f;
                g *= 0.9f + 0.1f * MathF.Sin(tiempo * 6.2f + f * 7f + semille(semilla));
                VFXCore.Line(col[i - 1], col[i], Alfa(Violeta, alfa * 0.16f), g);
            }
            VFXCore.FlushAdditive();

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

        /// <summary>El violeta del RIM de la masa y las venas de bruma: la
        /// sombra mágica se LEE sobre cualquier fondo (v6.50.65).</summary>
        public static readonly Color Violeta = new(118, 74, 190);

        /// <summary>El humo base de la bruma negra: negro con un fulgor
        /// violáceo (v6.50.65 — el negro puro era invisible).</summary>
        public static readonly Color HumoNegro = new(18, 10, 28);

        /// <summary>El color al que SE ENFRÍA la bruma al disolverse: el
        /// borde violáceo-gris que hace visible cada puff (v6.50.65).</summary>
        public static readonly Color HumoVioleta = new(64, 42, 96);

        /// <summary>
        /// UN OJO: esclerótica blanca elíptica (aditivo), pupila roja
        /// desplazada hacia <paramref name="mirada"/>. El tamaño late
        /// con <paramref name="abierto"/> (0 = cerrado, 1 = bien abierto):
        /// los ojos de Pride NO parpadean suaves — SE ABREN de golpe, y
        /// la apertura se anima con un ease-out violento en el llamador.
        /// v6.50.65 — EL HALO: un resplandor blanco suave detrás de la
        /// esclerótica para que el ojo PRENDA en cualquier fondo.
        /// </summary>
        public static void Ojo(Vector2 pos, float tamaño, Vector2 mirada, float abierto, bool pupila = true)
        {
            if (abierto <= 0.03f || tamaño <= 0.5f) return;
            float a = MathHelper.Clamp(abierto, 0f, 1f);
            // EL HALO (v6.50.65): el ojo BRILLA antes de existir
            VFXCore.Quad(pos, Alfa(Blanco, 0.20f * a), new Vector2(tamaño * 2.1f, tamaño * 1.5f));
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
        //  v6.50.65 — LA BRUMA DE VERDAD: la bruma de la .64 (quads
        //  SoftGlow casi negros al 30%) era INVISIBLE — negro sobre
        //  negro, puffs minúsculos, cero textura. AHORA usa BrumaFX (la
        //  librería de humo de la casa: flipbooks de ruido fBm horneados,
        //  sub-blobs que respiran, invariancia de escala) en el lote de
        //  MASA (AlphaBlend — humo que OCLUYE) con la RAMPA DE
        //  ENFRIAMIENTO violácea: cada puff NACE negro, se DESGARRA y se
        //  disuelve en gris-violeta — el borde SIEMPRE se lee — más una
        //  segunda pasada aditiva de ALIENTO VIOLETA entre los puffs
        //  (la magia de la sombra PRENDE en cualquier fondo).
        // ==================================================================

        /// <summary>
        /// EL PATRÓN COMPARTIDO: abre el lote de masa de BrumaFX, dibuja
        /// los puffs que el callback pida (en coords de PANTALLA) y lo
        /// cierra — blindado con try/finally para no dejar jamás un Begin
        /// vivo (la lección de la .58).
        /// </summary>
        private static void LoteDeBruma(Action dibujar)
        {
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                BrumaFX.BeginMass();
                dibujar();
            }
            catch { }
            finally
            {
                try { Main.spriteBatch.End(); } catch { }
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        /// <summary>
        /// EL Aliento violeta (la segunda capa): puffs aditivos violáceos
        /// MUY tenues entre la bruma — la lectura garantizada de noche y
        /// en cueva. Va por VFXCore (lote aditivo de la casa).
        /// </summary>
        private static void AlientoVioleta(Vector2 pos, float radio, float alfa, float tiempo, int semilla, int k)
        {
            float f0 = Frac(semille(semilla) * 0.61f + k * 0.618034f);
            float late = 0.7f + 0.3f * MathF.Sin(tiempo * (1.1f + f0) + k * 2.4f);
            VFXCore.Quad(pos + new Vector2(MathF.Sin(tiempo * 0.8f + k * 2.1f) * radio * 0.5f,
                                           -radio * 0.35f * f0),
                Alfa(Violeta, alfa * 0.11f * late), new Vector2(radio * 1.9f, radio * 1.5f));
        }

        /// <summary>
        /// LA NUBE DE BRUMA que desintegra (la muerte del festín): racimo
        /// BrumaFX.Cloud (masa que ocluye + rampa violácea) + aliento
        /// violeta. Coordenadas de MUNDO.
        /// </summary>
        public static void Bruma(Vector2 pos, float radio, float alfa, float tiempo, int semilla, Vector2 deriva = default)
        {
            if (alfa <= 0.02f || radio <= 1f) return;
            if (Main.netMode == NetmodeID.Server) return;

            Vector2 o = Main.screenPosition;
            LoteDeBruma(() =>
            {
                BrumaFX.Cloud(pos - o, radio, HumoNegro, semilla,
                    tiempo, Math.Max(3, (int)(radio / 30f)), alpha: 0.62f * alfa);
                BrumaFX.Cloud(pos - o + deriva * 0.5f, radio * 0.66f, HumoVioleta, semilla + 31,
                    tiempo * 0.85f, 3, alpha: 0.30f * alfa);
            });

            // la lectura nocturna: el aliento violeta
            VFXCore.Begin();
            for (int k = 0; k < 4; k++)
                AlientoVioleta(pos, radio * 0.55f, alfa, tiempo, semilla + 5, k);
            VFXCore.FlushAdditive();
        }

        // ==================================================================
        //  v6.50.65 — LA BRUMA NEGRA CONTINUA (BrumaFX) + LAS GARRAS
        // ==================================================================

        /// <summary>
        /// LA BRUMA VIVA DEL CUERPO: puﬀs de humo fBm (flipbook que se
        /// DESGARRA de verdad) que nacen PEGADOS a la masa del tentáculo,
        /// crecen ×1.5, derivan hacia fuera y hacia arriba y se enfrían
        /// de negro a violeta-gris al morir — la exhalación continua de
        /// la sombra. Cada puﬀ ancla a un punto de la columna (reparto
        /// áureo), su vida es CÍCLICA y todo determinista por semilla.
        /// Funciona con CUALQUIER polilínea — tentáculo, rastro o anillo.
        /// </summary>
        /// <param name="col">La polilínea que respira (columna, rastro, anillo).</param>
        /// <param name="grosor">Grosor de la masa que la exhala (escala los puﬀs).</param>
        /// <param name="alfa">0..1 — cuánta bruma hay.</param>
        public static void BrumaColumna(Vector2[] col, float grosor, float alfa, float tiempo, int semilla, int cantidad = 9)
        {
            if (alfa <= 0.02f || col == null || col.Length < 3) return;
            if (Main.netMode == NetmodeID.Server) return;
            int n = col.Length;
            Vector2 o = Main.screenPosition;

            LoteDeBruma(() =>
            {
                for (int k = 0; k < cantidad; k++)
                {
                    float f0 = Frac(semille(semilla) * 0.83f + k * 0.618034f);  // el ancla en la columna
                    float ritmo = 0.10f + 0.09f * Frac(f0 * 9.7f);              // cada puﬀ respira a su ritmo
                    float edad = Frac(tiempo * ritmo + f0 * 4.1f + k * 0.233f); // vida cíclica 0→1
                    int idx = (int)(f0 * (n - 1));
                    // la dirección local de la columna (para derivar DE LADO)
                    Vector2 seg = col[Math.Min(idx + 1, n - 1)] - col[Math.Max(idx - 1, 0)];
                    Vector2 perp = seg.LengthSquared() < 0.01f ? Vector2.UnitY
                        : new Vector2(-seg.Y, seg.X) * (1f / MathF.Sqrt(seg.LengthSquared()));
                    float lado = Frac(f0 * 13.7f) > 0.5f ? 1f : -1f;
                    float deriva = edad * (26f + 34f * Frac(f0 * 5.9f));
                    Vector2 pos = col[idx]
                        + perp * (lado * (grosor * 0.3f + deriva * 0.5f))
                        + new Vector2(0f, -14f * edad);                          // sube, como humo frío
                    float r = grosor * (0.5f + 0.85f * edad);                    // nace chico, crece ×1.35
                    // LA ENVOLVENTE y LA RAMPA (a mano — BrumaFX.Puff expone quality):
                    float a = alfa * MathF.Sin(edad * MathF.PI) * (0.75f + 0.25f * Frac(f0 * 7.3f));
                    Color c = edad > 0.55f
                        ? Color.Lerp(HumoNegro, HumoVioleta, (edad - 0.55f) / 0.45f)
                        : HumoNegro;
                    BrumaFX.Puff(pos - o, r, c, semilla * 31 + k, tiempo + f0 * 7f,
                        MathHelper.Clamp(a, 0.04f, 0.85f), quality: 0.5f,
                        velocity: perp * (lado * deriva * 0.4f));
                }
            });

            // el aliento violeta entre puffs (lectura nocturna)
            VFXCore.Begin();
            for (int k = 0; k < Math.Min(cantidad, 5); k++)
            {
                float f0 = Frac(semille(semilla) * 0.83f + k * 0.618034f);
                int idx = (int)(f0 * (n - 1));
                AlientoVioleta(col[idx], grosor * 0.9f, alfa, tiempo, semilla + k * 3, k);
            }
            VFXCore.FlushAdditive();
        }

        /// <summary>
        /// EL ALIENTO DE LA BOCA: la bruma que EXHALA una fauce abierta —
        /// sale de la garganta hacia <paramref name="dir"/> en abanico,
        /// crece y se enfría violáceo al disolverse. Gateada por
        /// <paramref name="apertura"/>: la boca cerrada no respira.
        /// </summary>
        public static void BrumaBoca(Vector2 garganta, Vector2 dir, float apertura, float alfa, float tiempo, int semilla, int puffs = 5)
        {
            if (alfa <= 0.02f || apertura <= 0.1f) return;
            if (Main.netMode == NetmodeID.Server) return;
            Vector2 d = dir.LengthSquared() < 0.01f ? Vector2.UnitX : Vector2.Normalize(dir);
            Vector2 perp = new(-d.Y, d.X);
            Vector2 o = Main.screenPosition;

            LoteDeBruma(() =>
            {
                for (int k = 0; k < puffs; k++)
                {
                    float f0 = Frac(semille(semilla) * 0.47f + k * 0.618034f);
                    float edad = Frac(tiempo * (0.16f + 0.10f * f0) + f0 * 5.3f);
                    float alcance = (18f + 56f * f0) * (0.35f + 0.65f * apertura);
                    float lado = (Frac(f0 * 11.3f) > 0.5f ? 1f : -1f) * (0.3f + 0.7f * edad);
                    Vector2 pos = garganta + d * (alcance * (0.3f + 0.7f * edad))
                                + perp * (lado * alcance * 0.4f)
                                + new Vector2(0f, -10f * edad);
                    float r = (18f + 26f * f0) * (0.55f + 0.8f * edad) * (0.4f + 0.6f * apertura);
                    float a = alfa * apertura * MathF.Sin(edad * MathF.PI) * 0.85f;
                    Color c = edad > 0.55f
                        ? Color.Lerp(HumoNegro, HumoVioleta, (edad - 0.55f) / 0.45f)
                        : HumoNegro;
                    BrumaFX.Puff(pos - o, r, c, semilla * 17 + k * 3, tiempo + f0 * 9f,
                        MathHelper.Clamp(a, 0.04f, 0.8f), quality: 0.5f,
                        velocity: d * (alcance * 0.25f));
                }
            });

            // la lectura nocturna del aliento
            VFXCore.Begin();
            for (int k = 0; k < 3; k++)
                AlientoVioleta(garganta + d * (30f + k * 26f) * apertura, 26f * apertura,
                    alfa * apertura, tiempo, semilla + k * 5, k);
            VFXCore.FlushAdditive();
        }

        /// <summary>
        /// EL VELO DE LA SALA (la anticipación de cine): oscurece los
        /// CUATRO BORDES con doble velo (uno ancho y denso + otro corto y
        /// profundo) que RESPIRA lento — la penumbra de Pride hecha sala
        /// de proyección. v6.50.65: ×2 capas + pulso — antes era un velo
        /// tan tenue que nadie lo veía.
        /// </summary>
        public static void Vignette(float alfa)
        {
            if (alfa <= 0.02f) return;
            if (Main.netMode == NetmodeID.Server) return;
            float w = Main.screenWidth, h = Main.screenHeight;
            Vector2 o = Main.screenPosition;
            float pulso = 0.90f + 0.10f * MathF.Sin(Main.GlobalTimeWrappedHourly * 1.7f);

            // CAPA 1 — el velo ancho (cubre media pantalla hacia adentro)
            Color c1 = Alfa(Negro, alfa * 0.62f * pulso);
            VFXCore.Begin();
            VFXCore.Quad(new Vector2(o.X - w * 0.34f, o.Y + h * 0.5f), c1, new Vector2(w * 1.1f, h * 2.4f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 1.34f, o.Y + h * 0.5f), c1, new Vector2(w * 1.1f, h * 2.4f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y - h * 0.36f), c1, new Vector2(w * 2.4f, h * 1.1f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y + h * 1.36f), c1, new Vector2(w * 2.4f, h * 1.1f), 0f, VFXCore.SoftGlow);
            VFXCore.FlushAlpha();

            // CAPA 2 — el velo profundo de la esquina (más corto, más negro)
            Color c2 = Alfa(Negro, alfa * 0.80f * pulso);
            VFXCore.Begin();
            VFXCore.Quad(new Vector2(o.X - w * 0.22f, o.Y + h * 0.5f), c2, new Vector2(w * 0.8f, h * 1.9f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 1.22f, o.Y + h * 0.5f), c2, new Vector2(w * 0.8f, h * 1.9f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y - h * 0.24f), c2, new Vector2(w * 1.9f, h * 0.8f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y + h * 1.24f), c2, new Vector2(w * 1.9f, h * 0.8f), 0f, VFXCore.SoftGlow);
            VFXCore.FlushAlpha();
        }

        /// <summary>
        /// UNA GARRA (v6.50.65 — la cura de las «púas rectangulares»):
        /// zarpo AFILADO de tres segmentos que se ESTRECHA de la base a
        /// la punta y se CURVA en gancho hacia <paramref name="gancho"/>
        /// (−1/−1 izquierda, +1 derecha…) — con la punta BLANCA de hueso.
        /// Se lee como garra de sombra, no como barra.
        /// </summary>
        /// <param name="basePos">La base gruesa de la garra (la más alejada del jefe).</param>
        /// <param name="hacia">Dirección de la punta (hacia el jefe).</param>
        /// <param name="largo">Largo total en px.</param>
        /// <param name="alfa">0..1.</param>
        /// <param name="gancho">−1..1 — cuánto y hacia dónde se curva.</param>
        public static void Garra(Vector2 basePos, Vector2 hacia, float largo, float alfa, float gancho = 0f)
        {
            if (largo <= 3f || alfa <= 0.02f) return;
            if (hacia.LengthSquared() < 0.01f) hacia = -Vector2.UnitY;
            Vector2 dir = Vector2.Normalize(hacia);
            Vector2 perp = new(-dir.Y, dir.X);

            // tres segmentos: grueso → medio → aguja, cada uno MÁS corto
            // y desviado hacia el gancho (la curva del zarpo)
            float g1 = largo * 0.30f, g2 = largo * 0.20f, g3 = largo * 0.11f;
            Vector2 p1 = basePos + dir * (largo * 0.40f) + perp * (gancho * largo * 0.06f);
            Vector2 p2 = p1 + dir * (largo * 0.38f) + perp * (gancho * largo * 0.14f);
            Vector2 p3 = p2 + dir * (largo * 0.22f) + perp * (gancho * largo * 0.20f);

            VFXCore.Begin();
            VFXCore.Line(basePos, p1, Alfa(Negro, alfa), g1);
            VFXCore.Line(p1, p2, Alfa(Negro, alfa), g2);
            VFXCore.Line(p2, p3, Alfa(Negro, alfa), g3);
            VFXCore.FlushAlpha(VFXCore.Pixel);

            // el rim violeta de la garra (lectura nocturna)
            VFXCore.Begin();
            VFXCore.Line(basePos, p1, Alfa(Violeta, alfa * 0.14f), g1 * 1.5f);
            VFXCore.Line(p1, p2, Alfa(Violeta, alfa * 0.14f), g2 * 1.5f);
            VFXCore.Line(p2, p3, Alfa(Violeta, alfa * 0.14f), g3 * 1.5f);
            VFXCore.FlushAdditive();

            // LA PUNTA DE HUESO — blanca, afilada, brillando
            VFXCore.Begin();
            VFXCore.Line(p2, p3 + dir * (largo * 0.10f), Alfa(Blanco, 0.85f * alfa), g3 * 0.45f);
            VFXCore.FlushAdditive(VFXCore.Pixel);
        }

        /// <summary>
        /// LA ONDA DE CHOQUE (v6.50.64 — el «juice» del impacto): anillo
        /// doble que se expande y muere — el CRUJIDO del golpe hecho
        /// imagen. En rojo visceral (la luz que mata la sombra) o en
        /// blanco de hueso.
        /// </summary>
        public static void OndaChoque(Vector2 pos, float radio, float alfa, bool roja = true)
        {
            if (alfa <= 0.02f || radio < 4f) return;
            VFXCore.Begin();
            Color c = roja ? Rojo : Blanco;
            VFXCore.Quad(pos, Alfa(c, alfa), VFXCore.RingQuadSize(radio), 0f, VFXCore.Ring);
            VFXCore.Quad(pos, Alfa(c, alfa * 0.45f), VFXCore.RingQuadSize(radio * 0.74f), 0f, VFXCore.Ring);
            VFXCore.FlushAdditive();
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
