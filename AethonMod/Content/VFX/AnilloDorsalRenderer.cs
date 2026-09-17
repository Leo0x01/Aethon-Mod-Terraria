using Microsoft.Xna.Framework;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// AnilloDorsalRenderer — v6.36 — EL ANILLO RÚNICO DE LA ESPALDA.
    ///
    /// EL FACHADA DEL ANILLO RÚNICO DORSAL: la gran firma mágica que
    /// el jugador lleva EN LA ESPALDA — un anillo elíptico de pie
    /// detrás del cuerpo (visto a escorzo, como las alas de un círculo
    /// mágico), con LAS OCHO RUNAS cabalgando su tangente (la
    /// escritura de SigiloLib), el ANILLO DE FOTONES interior
    /// contrarrotando (la firma de los agujeros negros de la casa —
    /// LOS ANILLOS RÚNICOS DEL VACÍO, no su disco de acreción), los
    /// CUATRO NODOS cardinales y el polvo rúnico orbitando el conjunto.
    ///
    /// La paleta es la del vacío: el aro fucsia-magenta con las runas
    /// de punta rosa pálida — la misma familia del sello del agujero
    /// negro, colgada de tu espalda.
    ///
    /// Uso (biblioteca): VFXCore.Begin() → ComputeQuads(...) →
    /// VFXCore.AppendToPlayerDraw(...) — el camino oficial de las
    /// capas de jugador (coords de MUNDO, como SigiloLib).
    /// </summary>
    public static class AnilloDorsalRenderer
    {
        /// <summary>Las runas del anillo (la ley de los sellos de la casa).</summary>
        public const int RunasCuenta = 8;

        /// <summary>El cuerpo del aro y de las runas (fucsia del vacío).</summary>
        public static readonly Color CuerpoVacio = new(255, 92, 158);

        /// <summary>La punta pálida de las runas (rosa casi blanco).</summary>
        public static readonly Color PuntaVacio = new(255, 230, 244);

        /// <summary>El color del anillo de fotones (magenta del abismo).</summary>
        public static readonly Color FotonesVacio = new(255, 64, 208);

        /// <summary>
        /// Calcula el anillo rúnico dorsal completo como cuadros de luz
        /// en el buffer de VFXCore (coordenadas de MUNDO).
        /// </summary>
        /// <param name="anchor">El centro de la espalda (mundo — típicamente
        /// el centro del jugador apenas por encima del pecho).</param>
        /// <param name="altura">La altura del sprite del jugador (humano
        /// ≈ 42 px — el anillo escala con él).</param>
        /// <param name="time">Tiempo animado (Main.GlobalTimeWrappedHourly).</param>
        /// <param name="alpha">Multiplicador global de intensidad (0..1).</param>
        public static void ComputeQuads(Vector2 anchor, float altura, float time, float alpha = 1f)
        {
            if (alpha <= 0.02f || altura < 8f) return;

            // === LA GEOMETRÍA COMPARTIDA (el anillo a escorzo: ancho de
            //     lado a lado, achatado en Y — de pie DETRÁS del cuerpo). ===
            float a = altura * 1.05f;
            float b = altura * 0.62f;
            float tilt = -0.16f + 0.05f * VFXCore.Sway(time, 0.5f);   // precesión viva
            float spin = time * 0.22f;                                   // la cadencia de la casa

            // La respiración conjunta (el anillo está VIVO).
            float aliento = 0.80f + 0.08f * (float)System.Math.Sin(time * 1.30f);

            // --- 1. EL ANILLO RÚNICO: el aro con profundidad + las OCHO
            //     RUNAS cabalgando la tangente (SigiloLib.AnilloRunico). ---
            SigiloLib.AnilloRunico(anchor, a, b, tilt, spin,
                RunasCuenta, 0.95f, time, 3, CuerpoVacio, PuntaVacio,
                aliento * alpha);

            // --- 2. EL ANILLO DE FOTONES interior (la firma del vacío):
            //     el aro fino CONTRARROTANDO dentro del anillo rúnico. ---
            float pulso = 0.30f + 0.14f * (float)System.Math.Sin(time * 1.7f);
            VFXCore.Quad(anchor,
                SigiloLib.Tint(FotonesVacio, pulso * alpha),
                new Vector2(a * 0.80f * 2.174f, b * 0.86f * 2.174f),
                tilt - time * 0.06f, VFXCore.Ring);

            // --- 3. LOS CUATRO NODOS CARDINALES: las perlas anclas en los
            //     cuatro puntos del anillo (cabalgando despacio). ---
            for (int k = 0; k < 4; k++)
            {
                Vector2 pos = SigiloLib.EllipsePoint(anchor, a, b, tilt,
                    k * MathHelper.PiOver2 + time * 0.05f);
                SigiloLib.NodoCardinal(pos, 6.5f, time, k * 1.6f,
                    CuerpoVacio, PuntaVacio, 0.80f * alpha);
            }

            // --- 4. EL POLVO RÚNICO: motas orbitando el conjunto. ---
            SigiloLib.PolvoRunico(anchor, a * 0.85f, 10, time, 77,
                CuerpoVacio, 0.45f * alpha);
        }

        /// <summary>
        /// Posición mundial de la runa g del anillo (para las chispas que
        /// escapan de la escritura — el patrón de las coronas).
        /// </summary>
        public static Vector2 RunaWorld(Vector2 anchor, float altura, float time, int g)
        {
            float a = altura * 1.05f;
            float b = altura * 0.62f;
            float tilt = -0.16f + 0.05f * VFXCore.Sway(time, 0.5f);
            float spin = time * 0.22f;
            return SigiloLib.EllipsePoint(anchor, a, b, tilt,
                g / (float)RunasCuenta * MathHelper.TwoPi + spin);
        }
    }
}
