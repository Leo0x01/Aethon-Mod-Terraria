using Microsoft.Xna.Framework;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// ALASESTILO — v6.54.0 — EL CATÁLOGO DE ALAS PROCEDURALES.
    ///
    /// La letra del usuario: «las alas la aurora de tinta están bien pero
    /// pueden ser mejor, deja esas alas, y crea otras 15 alas usando el
    /// mismo sistema». La técnica de AlasAuroraLib (el abanico de plumas
    /// de QUADS, la onda de aleteo en cadena, el ruido que fluye, el
    /// desgarre de punta) queda INTACTA — lo nuevo es que cada par de
    /// alas lee SU ESTILO: la paleta que corre por las plumas, la forma
    /// del abanico, el tempo del aleteo, las intensidades de las capas y
    /// LA TEXTURA DE RUIDO propia (cada ala fluye con una de las 20
    /// texturas Ruido_N nacidas de la investigación de las texturas de
    /// efecto de Goozma — familias fBm / wisps / turbulencia / starfield
    /// / seda / vermiforme / ridged / células / plasma).
    ///
    /// LA AURORA DE TINTA (índice 1) es la entrada [0] del catálogo con
    /// los valores históricos exactos: su dibujado NO cambia ni un
    /// píxel (la promesa de «deja esas alas»).
    /// </summary>
    public struct EstiloAlas
    {
        /// <summary>La paleta que corre por el abanico (muestreo por hue
        /// con crossfade — el mismo contrato del ColorGoozma).</summary>
        public Color[] Paleta;

        /// <summary>El tinte de la forma diurna (la aurora funde hacia
        /// blanco-oro; cada ala funde hacia SU luz).</summary>
        public Color ColorDia;

        /// <summary>La mezcla diurna (0.62 en la aurora) — cuánto funde
        /// el día hacia ColorDia.</summary>
        public float FuerzaDia;

        // === LA FORMA DEL ABANICO (los valores históricos de la casa) ===
        public float LargoInterior;
        public float LargoExterior;
        public float RaizSemiancho;
        public float PuntaSemianchoBase;
        public float PuntaSemianchoAbierto;
        public float Inclinacion;
        public float Barrido;

        // === EL COLOR EN MOVIMIENTO ===
        public float HueEntre;      // salto de hue pluma a pluma
        public float HueLargo;      // dispersión a lo largo de la pluma
        public float HueTiempo;     // velocidad del arcoíris

        // === EL TEMPO ===
        public float FlapVel;       // multiplicador del aleteo
        public float Desgarre;      // amplitud del desgarre de puntas

        // === LAS INTENSIDADES DE LAS 3 CAPAS (0..1) ===
        public float NucleoInten;
        public float CuerpoInten;
        public float HaloInten;

        // === EL RUIDO QUE FLUYE ===
        public string Ruido;
        public float RuidoInten;

        /// <summary>La escala de la luz que emite el ala.</summary>
        public float Luz;
    }

    /// <summary>
    /// EL CATÁLOGO — el índice ES el byte que vive en AlasAuroraPlayer
    /// (1 = la Aurora de Tinta histórica; 2..16 = las quince nuevas).
    /// </summary>
    public static class AlasCatalogo
    {
        public const byte Aurora = 1;

        private const string PrefijoRuido =
            "AethonMod/Content/Effects/Procedural/";

        public static readonly EstiloAlas[] Todos = Crear();

        private static EstiloAlas[] Crear()
        {
            var t = new EstiloAlas[17];     // [0] se salta (1-indexado)

            // === 1 — LA AURORA DE TINTA (histórica — NO TOCAR) ===
            t[1] = new EstiloAlas
            {
                Paleta = null,              // null = el prisma completo de la casa
                ColorDia = new Color(255, 243, 214),
                FuerzaDia = 0.62f,
                LargoInterior = 88f, LargoExterior = 48f,
                RaizSemiancho = 3.5f,
                PuntaSemianchoBase = 14f, PuntaSemianchoAbierto = 8f,
                Inclinacion = 0.12f, Barrido = 0.14f,
                HueEntre = 0.085f, HueLargo = 0.16f, HueTiempo = 0.018f,
                FlapVel = 1f, Desgarre = 1f,
                NucleoInten = 0.92f, CuerpoInten = 0.34f, HaloInten = 0.13f,
                Ruido = PrefijoRuido + "Noise",
                RuidoInten = 1f,
                Luz = 1f,
            };

            // === 2..16 — LAS QUINCE NUEVAS (la letra del 27/10) ===
            t[2] = Ala("Tinta Serena",
                paleta: new[] { new Color(201, 166, 255), new Color(140, 60, 210), new Color(75, 30, 122) },
                dia: new Color(232, 214, 255), ruido: "Ruido_7",
                largoInt: 96f, largoExt: 52f, flap: 0.8f,
                nucleo: 0.85f, cuerpo: 0.30f, halo: 0.14f);

            t[3] = Ala("Eclipse",
                paleta: new[] { new Color(157, 92, 232), new Color(75, 18, 119), new Color(20, 6, 31) },
                dia: new Color(140, 100, 190), ruido: "Ruido_4",
                largoInt: 84f, largoExt: 44f, punta: 18f, barrido: 0.22f,
                nucleo: 0.80f, cuerpo: 0.38f, halo: 0.16f);

            t[4] = Ala("Plasma",
                paleta: new[] { new Color(255, 61, 219), new Color(199, 36, 184), new Color(122, 47, 209) },
                dia: new Color(255, 214, 246), ruido: "Ruido_5",
                largoInt: 70f, largoExt: 40f, flap: 1.35f, desgarre: 1.3f,
                nucleo: 1f, cuerpo: 0.42f, halo: 0.18f, ruidoInten: 1.2f);

            t[5] = Ala("Nebulosa",
                paleta: new[] { new Color(184, 127, 232), new Color(209, 92, 255), new Color(110, 123, 232) },
                dia: new Color(240, 220, 255), ruido: "Investigacion/Espacio_1",
                largoInt: 100f, largoExt: 56f, flap: 0.9f,
                nucleo: 0.82f, cuerpo: 0.40f, halo: 0.20f, ruidoInten: 1.25f, luz: 1.15f);

            t[6] = Ala("Cometario",
                paleta: new[] { new Color(255, 243, 214), new Color(125, 232, 255), new Color(92, 59, 209) },
                dia: new Color(255, 250, 240), ruido: "Ruido_12",
                largoInt: 92f, largoExt: 60f, hueLargo: 0.22f,
                nucleo: 1f, cuerpo: 0.30f, halo: 0.12f);

            t[7] = Ala("Medianoche",
                paleta: new[] { new Color(245, 240, 255), new Color(48, 26, 96), new Color(14, 8, 30) },
                dia: new Color(120, 110, 160), ruido: "Ruido_2",
                largoInt: 94f, largoExt: 50f, nucleo: 0.70f,
                cuerpo: 0.22f, halo: 0.10f, ruidoInten: 0.9f, luz: 0.8f);

            t[8] = Ala("Prisma",
                paleta: new[] {                          // el arcoíris del dios (ColorGoozma)
                    new Color(174, 23, 189), new Color(237, 128, 60),
                    new Color(247, 255, 101), new Color(176, 234, 85),
                    new Color(102, 219, 249) },
                dia: new Color(255, 243, 214), ruido: "Ruido_11",
                hueEntre: 0.12f, hueTiempo: 0.026f,
                nucleo: 0.95f, cuerpo: 0.38f, halo: 0.16f, ruidoInten: 1.1f, luz: 1.2f);

            t[9] = Ala("Fuego Frío",
                paleta: new[] { new Color(234, 246, 255), new Color(185, 166, 255), new Color(125, 209, 232) },
                dia: new Color(250, 252, 255), ruido: "Ruido_8",
                largoInt: 90f, largoExt: 54f, hueLargo: 0.24f, flap: 0.85f,
                nucleo: 0.88f, cuerpo: 0.30f, halo: 0.15f);

            t[10] = Ala("Tormenta",
                paleta: new[] { new Color(240, 230, 255), new Color(178, 76, 232), new Color(61, 26, 92) },
                dia: new Color(226, 216, 246), ruido: "Ruido_9",
                largoInt: 82f, largoExt: 46f, flap: 1.5f, desgarre: 1.5f,
                nucleo: 1f, cuerpo: 0.36f, halo: 0.14f, ruidoInten: 1.15f);

            t[11] = Ala("Marioneta",
                paleta: new[] { new Color(227, 92, 133), new Color(138, 26, 61), new Color(26, 10, 20) },
                dia: new Color(210, 140, 160), ruido: "Ruido_15",
                largoInt: 80f, largoExt: 42f, punta: 9f, inclinacion: 0.2f,
                nucleo: 0.85f, cuerpo: 0.34f, halo: 0.12f, luz: 0.9f);

            t[12] = Ala("Aurora Ártica",
                paleta: new[] { new Color(125, 232, 154), new Color(125, 232, 224), new Color(185, 166, 255) },
                dia: new Color(235, 255, 250), ruido: "Ruido_0",
                largoInt: 104f, largoExt: 58f, hueLargo: 0.28f, flap: 0.75f,
                nucleo: 0.80f, cuerpo: 0.32f, halo: 0.18f, luz: 1.1f);

            t[13] = Ala("Orquídea",
                paleta: new[] { new Color(255, 138, 209), new Color(232, 79, 199), new Color(166, 61, 232) },
                dia: new Color(255, 224, 242), ruido: "Ruido_18",
                largoInt: 88f, largoExt: 50f, hueEntre: 0.1f,
                nucleo: 0.92f, cuerpo: 0.36f, halo: 0.16f, ruidoInten: 1.1f);

            t[14] = Ala("Vacío",
                paleta: new[] { new Color(107, 47, 160), new Color(40, 14, 66), new Color(8, 3, 13) },
                dia: new Color(70, 50, 100), ruido: "Ruido_10",
                largoInt: 114f, largoExt: 62f, flap: 0.65f, desgarre: 0.7f,
                nucleo: 0.55f, cuerpo: 0.20f, halo: 0.08f, ruidoInten: 0.85f, luz: 0.6f);

            t[15] = Ala("Solares Rúnicas",
                paleta: new[] { new Color(255, 246, 220), new Color(232, 166, 61), new Color(255, 232, 154) },
                dia: new Color(255, 252, 240), ruido: "Ruido_1",
                largoInt: 90f, largoExt: 48f, hueTiempo: 0.014f,
                nucleo: 1f, cuerpo: 0.34f, halo: 0.15f, luz: 1.25f);

            t[16] = Ala("Sangre de Tinta",
                paleta: new[] { new Color(209, 47, 166), new Color(122, 26, 77), new Color(20, 6, 20) },
                dia: new Color(224, 130, 190), ruido: "Ruido_17",
                largoInt: 86f, largoExt: 46f, desgarre: 1.2f,
                nucleo: 0.9f, cuerpo: 0.38f, halo: 0.14f, ruidoInten: 1.2f);

            return t;
        }

        /// <summary>El constructor con los valores de la casa por defecto
        /// (los de la Aurora histórica) — cada ala solo declara su alma.</summary>
        private static EstiloAlas Ala(string _,
            Color[] paleta, Color dia, string ruido,
            float largoInt = 88f, float largoExt = 48f,
            float raiz = 3.5f, float punta = 14f, float puntaAbierta = 8f,
            float inclinacion = 0.12f, float barrido = 0.14f,
            float hueEntre = 0.085f, float hueLargo = 0.16f, float hueTiempo = 0.018f,
            float flap = 1f, float desgarre = 1f,
            float nucleo = 0.92f, float cuerpo = 0.34f, float halo = 0.13f,
            float ruidoInten = 1f, float luz = 1f)
        {
            return new EstiloAlas
            {
                Paleta = paleta,
                ColorDia = dia,
                FuerzaDia = 0.62f,
                LargoInterior = largoInt, LargoExterior = largoExt,
                RaizSemiancho = raiz,
                PuntaSemianchoBase = punta, PuntaSemianchoAbierto = puntaAbierta,
                Inclinacion = inclinacion, Barrido = barrido,
                HueEntre = hueEntre, HueLargo = hueLargo, HueTiempo = hueTiempo,
                FlapVel = flap, Desgarre = desgarre,
                NucleoInten = nucleo, CuerpoInten = cuerpo, HaloInten = halo,
                Ruido = PrefijoRuido + ruido,
                RuidoInten = ruidoInten,
                Luz = luz,
            };
        }

        /// <summary>El estilo seguro (fuera de rango → la Aurora).</summary>
        public static ref readonly EstiloAlas De(byte indice)
        {
            if (indice <= 0 || indice >= Todos.Length)
                return ref Todos[1];
            return ref Todos[indice];
        }
    }
}
