using Microsoft.Xna.Framework;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// ALASESTILO — v6.54.3 — EL CATÁLOGO DE ALAS PROCEDURALES.
    ///
    /// La letra del usuario: «las alas son las mismas solo que con
    /// color diferente... usa texturas diferentes» — LA QUEJA CIERTA:
    /// la v6.54.0 solo variaba la paleta; los ÁNGULOS del abanico y el
    /// CONTEO de plumas eran CONSTANTES de la librería (todas las alas
    /// eran EL MISMO ABANICO recolorizado).
    ///
    /// LA CURA: la forma completa del ala vive en su estilo — el
    /// NÚMERO de plumas (4..7), los ángulos plegado/abierto del
    /// abanico, LA CURVA de la pluma (la punta se abre en garra — el
    /// ala de murciélago CURVA, la de hada es suave, la rígida no
    /// curva), la separación de las raíces, los anchos de las capas y
    /// el flujo del ruido. Quince alas = QUINCE ANATOMÍAS.
    ///
    /// LA AURORA DE TINTA (índice 1) es la entrada [1] con los
    /// valores históricos EXACTOS en los campos nuevos (Plumas=5,
    /// los ángulos de las constantes, Curva=0, las capas 0.30/0.62):
    /// su dibujado NO cambia ni un píxel (la promesa de «deja esas
    /// alas»).
    /// </summary>
    public struct EstiloAlas
    {
        /// <summary>La paleta que corre por el abanico (muestreo por hue
        /// con crossfade — el mismo contrato del ColorPrisma).</summary>
        public Color[] Paleta;

        /// <summary>El tinte de la forma diurna (la aurora funde hacia
        /// blanco-oro; cada ala funde hacia SU luz).</summary>
        public Color ColorDia;

        /// <summary>La mezcla diurna (0.62 en la aurora) — cuánto funde
        /// el día hacia ColorDia.</summary>
        public float FuerzaDia;

        // === LA FORMA DEL ABANICO (v6.54.3: TODO vive en el estilo) ===
        public int Plumas;             // 4..7 plumas por ala (la anatomía)
        public float LargoInterior;
        public float LargoExterior;
        public float RaizSemiancho;
        public float PuntaSemianchoBase;
        public float PuntaSemianchoAbierto;
        public float PlegadoMin;       // el abanico PLEGADO (vertical)
        public float PlegadoMax;
        public float AbiertoMin;       // el abanico ABIERTO (horizontal)
        public float AbiertoMax;
        public float Inclinacion;
        public float Barrido;
        public float Curva;            // la punta se abre en garra (px × largo)
        public float AnclajeSep;       // la separación de las raíces del par

        // === EL COLOR EN MOVIMIENTO ===
        public float HueEntre;      // salto de hue pluma a pluma
        public float HueLargo;      // dispersión a lo largo de la pluma
        public float HueTiempo;     // velocidad del arcoíris

        // === EL TEMPO ===
        public float FlapVel;       // multiplicador del aleteo
        public float Desgarre;      // amplitud del desgarre de puntas

        // === LAS INTENSIDADES Y ANCHOS DE LAS 3 CAPAS (0..1) ===
        public float NucleoInten;
        public float NucleoFrac;    // el ancho del núcleo (0.30 histórico)
        public float CuerpoInten;
        public float CuerpoFrac;    // el ancho del cuerpo (0.62 histórico)
        public float HaloInten;

        // === EL RUIDO QUE FLUYE ===
        public string Ruido;
        public float RuidoInten;
        public float RuidoEscala;   // el tamaño de las vetas (× las 2 pasadas)
        public float RuidoFlujo;    // la velocidad del flujo (× las 2 pasadas)

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

            // === 1 — LA AURORA DE TINTA (histórica — NO TOCAR: los
            //     campos nuevos llevan los valores EXACTOS de las
            //     constantes históricas de la librería) ===
            t[1] = new EstiloAlas
            {
                Paleta = null,              // null = el prisma completo de la casa
                ColorDia = new Color(255, 243, 214),
                FuerzaDia = 0.62f,
                Plumas = 5,
                LargoInterior = 88f, LargoExterior = 48f,
                RaizSemiancho = 3.5f,
                PuntaSemianchoBase = 14f, PuntaSemianchoAbierto = 8f,
                PlegadoMin = 0.50f, PlegadoMax = 0.92f,
                AbiertoMin = 0.22f, AbiertoMax = 1.54f,
                Inclinacion = 0.12f, Barrido = 0.14f,
                Curva = 0f, AnclajeSep = 5f,
                HueEntre = 0.085f, HueLargo = 0.16f, HueTiempo = 0.018f,
                FlapVel = 1f, Desgarre = 1f,
                NucleoInten = 0.92f, NucleoFrac = 0.30f,
                CuerpoInten = 0.34f, CuerpoFrac = 0.62f,
                HaloInten = 0.13f,
                Ruido = PrefijoRuido + "Noise",
                RuidoInten = 1f, RuidoEscala = 1f, RuidoFlujo = 1f,
                Luz = 1f,
            };

            // === 2..16 — LAS QUINCE (v6.54.3: QUINCE ANATOMÍAS — la
            //     letra: «las alas son las mismas solo que con color
            //     diferente»; ahora NINGUNA se parece a otra) ===

            // 2 — TINTA SERENA: SEIS plumas líquidas curvadas suaves —
            //     el abanico estrecho y elegante del hada de tinta.
            t[2] = Ala("Tinta Serena",
                paleta: new[] { new Color(201, 166, 255), new Color(140, 60, 210), new Color(75, 30, 122) },
                dia: new Color(232, 214, 255), ruido: "Ruido_7",
                plumas: 6, largoInt: 96f, largoExt: 52f, curva: 0.22f,
                plegadoMin: 0.52f, plegadoMax: 0.98f,
                abiertoMin: 0.18f, abiertoMax: 1.05f,
                nucleo: 0.85f, cuerpo: 0.30f, halo: 0.14f);

            // 3 — ECLIPSE: CUATRO plumas anchas en abanico AMPLIO con
            //     garra curva — la penumbra que se despliega ancha.
            t[3] = Ala("Eclipse",
                paleta: new[] { new Color(157, 92, 232), new Color(75, 18, 119), new Color(20, 6, 31) },
                dia: new Color(140, 100, 190), ruido: "Ruido_4",
                plumas: 4, largoInt: 86f, largoExt: 46f, punta: 18f, barrido: 0.22f,
                curva: 0.30f, plegadoMin: 0.46f, plegadoMax: 0.90f,
                abiertoMin: 0.30f, abiertoMax: 1.70f,
                nucleo: 0.80f, cuerpo: 0.38f, halo: 0.16f);

            // 4 — PLASMA: SIETE plumas finas curvadas HACIA ATRÁS — los
            //     arcos voltaicos que chisporrotean rápido.
            t[4] = Ala("Plasma",
                paleta: new[] { new Color(255, 61, 219), new Color(199, 36, 184), new Color(122, 47, 209) },
                dia: new Color(255, 214, 246), ruido: "Ruido_5",
                plumas: 7, largoInt: 70f, largoExt: 40f, punta: 10f,
                curva: -0.18f, plegadoMin: 0.54f, plegadoMax: 0.96f,
                abiertoMin: 0.20f, abiertoMax: 1.48f,
                flap: 1.35f, desgarre: 1.3f,
                nucleo: 1f, cuerpo: 0.42f, halo: 0.18f, ruidoInten: 1.2f,
                ruidoFlujo: 1.6f);

            // 5 — NEBULOSA: SEIS plumas larguísimas de flujo LENTO y
            //     vetas GRANDES — la nube estelar que se arrastra.
            t[5] = Ala("Nebulosa",
                paleta: new[] { new Color(184, 127, 232), new Color(209, 92, 255), new Color(110, 123, 232) },
                dia: new Color(240, 220, 255), ruido: "Investigacion/Espacio_1",
                plumas: 6, largoInt: 100f, largoExt: 56f, curva: 0.12f,
                nucleo: 0.82f, cuerpo: 0.40f, halo: 0.20f,
                ruidoInten: 1.25f, ruidoEscala: 1.4f, ruidoFlujo: 0.7f, luz: 1.15f);

            // 6 — COMETARIO: CINCO plumas con fuerte conicidad y leve
            //     garra atrás — la cola del cometa.
            t[6] = Ala("Cometario",
                paleta: new[] { new Color(255, 243, 214), new Color(125, 232, 255), new Color(92, 59, 209) },
                dia: new Color(255, 250, 240), ruido: "Ruido_12",
                plumas: 5, largoInt: 96f, largoExt: 44f, curva: -0.10f,
                hueLargo: 0.22f,
                nucleo: 1f, cuerpo: 0.30f, halo: 0.12f);

            // 7 — MEDIANOCHE: CUATRO plumas ANCHAS con la GARRA fuerte
            //     del murciélago — la membrana nocturna.
            t[7] = Ala("Medianoche",
                paleta: new[] { new Color(245, 240, 255), new Color(48, 26, 96), new Color(14, 8, 30) },
                dia: new Color(120, 110, 160), ruido: "Ruido_2",
                plumas: 4, largoInt: 92f, largoExt: 50f,
                punta: 20f, puntaAbierta: 12f, curva: 0.38f,
                plegadoMin: 0.48f, plegadoMax: 0.94f,
                abiertoMin: 0.28f, abiertoMax: 1.62f,
                nucleo: 0.70f, cuerpo: 0.22f, halo: 0.10f,
                ruidoInten: 0.9f, luz: 0.8f);

            // 8 — PRISMA: SIETE plumas finas CORTAS y rectas — el
            //     racimo cristalino que gira rápido.
            t[8] = Ala("Prisma",
                paleta: new[] {                          // el arcoíris del dios (ColorPrisma)
                    new Color(174, 23, 189), new Color(237, 128, 60),
                    new Color(247, 255, 101), new Color(176, 234, 85),
                    new Color(102, 219, 249) },
                dia: new Color(255, 243, 214), ruido: "Ruido_11",
                plumas: 7, largoInt: 72f, largoExt: 42f, punta: 11f,
                plegadoMin: 0.52f, plegadoMax: 0.95f,
                abiertoMin: 0.22f, abiertoMax: 1.44f,
                hueEntre: 0.12f, hueTiempo: 0.026f,
                nucleo: 0.95f, cuerpo: 0.38f, halo: 0.16f,
                ruidoInten: 1.1f, luz: 1.2f);

            // 9 — FUEGO FRÍO: SEIS plumas de hielo con curva suave y
            //     flujo lento — la escarcha que brilla.
            t[9] = Ala("Fuego Frío",
                paleta: new[] { new Color(234, 246, 255), new Color(185, 166, 255), new Color(125, 209, 232) },
                dia: new Color(250, 252, 255), ruido: "Ruido_8",
                plumas: 6, largoInt: 90f, largoExt: 54f, curva: 0.16f,
                punta: 16f, hueLargo: 0.24f, flap: 0.85f,
                nucleo: 0.88f, cuerpo: 0.30f, halo: 0.15f);

            // 10 — TORMENTA: CINCO plumas desgarradas barridas hacia
            //     atrás — el chubasco eléctrico.
            t[10] = Ala("Tormenta",
                paleta: new[] { new Color(240, 230, 255), new Color(178, 76, 232), new Color(61, 26, 92) },
                dia: new Color(226, 216, 246), ruido: "Ruido_9",
                plumas: 5, largoInt: 82f, largoExt: 46f, punta: 11f,
                curva: -0.22f, barrido: 0.22f,
                flap: 1.5f, desgarre: 1.5f,
                nucleo: 1f, cuerpo: 0.36f, halo: 0.14f, ruidoInten: 1.15f);

            // 11 — MARIONETA: CUATRO plumas larguísimas COLGANTES en
            //     garra cerrada — los hilos que caen.
            t[11] = Ala("Marioneta",
                paleta: new[] { new Color(227, 92, 133), new Color(138, 26, 61), new Color(26, 10, 20) },
                dia: new Color(210, 140, 160), ruido: "Ruido_15",
                plumas: 4, largoInt: 84f, largoExt: 38f, punta: 9f,
                inclinacion: 0.2f, curva: 0.50f,
                plegadoMin: 0.56f, plegadoMax: 1.02f,
                abiertoMin: 0.24f, abiertoMax: 1.30f,
                nucleo: 0.85f, cuerpo: 0.34f, halo: 0.12f, luz: 0.9f);

            // 12 — AURORA ÁRTICA: SIETE plumas LARGUÍSIMAS en abanico
            //     vertical — la cortina de la aurora.
            t[12] = Ala("Aurora Ártica",
                paleta: new[] { new Color(125, 232, 154), new Color(125, 232, 224), new Color(185, 166, 255) },
                dia: new Color(235, 255, 250), ruido: "Ruido_0",
                plumas: 7, largoInt: 112f, largoExt: 64f, curva: 0.06f,
                plegadoMin: 0.44f, plegadoMax: 0.84f,
                abiertoMin: 0.20f, abiertoMax: 1.35f,
                hueLargo: 0.28f, flap: 0.75f,
                nucleo: 0.80f, cuerpo: 0.32f, halo: 0.18f, luz: 1.1f);

            // 13 — ORQUÍDEA: CINCO plumas ANCHÍSIMAS redondeadas — los
            //     pétalos que se abren.
            t[13] = Ala("Orquídea",
                paleta: new[] { new Color(255, 138, 209), new Color(232, 79, 199), new Color(166, 61, 232) },
                dia: new Color(255, 224, 242), ruido: "Ruido_18",
                plumas: 5, largoInt: 88f, largoExt: 54f,
                punta: 22f, puntaAbierta: 14f, curva: 0.20f,
                hueEntre: 0.1f,
                nucleo: 0.92f, cuerpo: 0.36f, halo: 0.16f, ruidoInten: 1.1f);

            // 14 — VACÍO: TRES plumas ENORMES tenues — el vacío no
            //     necesita más que tres zarpas.
            t[14] = Ala("Vacío",
                paleta: new[] { new Color(107, 47, 160), new Color(40, 14, 66), new Color(8, 3, 13) },
                dia: new Color(70, 50, 100), ruido: "Ruido_10",
                plumas: 3, largoInt: 120f, largoExt: 70f, curva: 0.28f,
                punta: 17f, anclajeSep: 6f,
                flap: 0.65f, desgarre: 0.7f,
                nucleo: 0.55f, cuerpo: 0.20f, halo: 0.08f,
                ruidoInten: 0.85f, luz: 0.6f);

            // 15 — SOLARES RÚNICAS: SEIS plumas RÍGIDAS sin curva — la
            //     llama solar grabada que no se dobla.
            t[15] = Ala("Solares Rúnicas",
                paleta: new[] { new Color(255, 246, 220), new Color(232, 166, 61), new Color(255, 232, 154) },
                dia: new Color(255, 252, 240), ruido: "Ruido_1",
                plumas: 6, largoInt: 90f, largoExt: 48f,
                curva: 0.02f, desgarre: 0.7f, hueTiempo: 0.014f,
                nucleo: 1f, cuerpo: 0.34f, halo: 0.15f, luz: 1.25f);

            // 16 — SANGRE DE TINTA: CINCO plumas PUNTIAGUDAS garras y
            //     goteando — la firma de la tinta oscura.
            t[16] = Ala("Sangre de Tinta",
                paleta: new[] { new Color(209, 47, 166), new Color(122, 26, 77), new Color(20, 6, 20) },
                dia: new Color(224, 130, 190), ruido: "Ruido_17",
                plumas: 5, largoInt: 86f, largoExt: 46f,
                punta: 10f, puntaAbierta: 5f, curva: 0.34f,
                desgarre: 1.2f,
                nucleo: 0.9f, cuerpo: 0.38f, halo: 0.14f, ruidoInten: 1.2f);

            return t;
        }

        /// <summary>El constructor con los valores de la casa por defecto
        /// (los de la Aurora histórica) — cada ala solo declara su alma;
        /// v6.54.3: los parámetros de la ANATOMÍA (plumas, ángulos del
        /// abanico, curva, anclaje, anchos de capa, flujo del ruido).</summary>
        private static EstiloAlas Ala(string _,
            Color[] paleta, Color dia, string ruido,
            int plumas = 5,
            float largoInt = 88f, float largoExt = 48f,
            float raiz = 3.5f, float punta = 14f, float puntaAbierta = 8f,
            float plegadoMin = 0.50f, float plegadoMax = 0.92f,
            float abiertoMin = 0.22f, float abiertoMax = 1.54f,
            float inclinacion = 0.12f, float barrido = 0.14f,
            float curva = 0f, float anclajeSep = 5f,
            float hueEntre = 0.085f, float hueLargo = 0.16f, float hueTiempo = 0.018f,
            float flap = 1f, float desgarre = 1f,
            float nucleo = 0.92f, float nucleoFrac = 0.30f,
            float cuerpo = 0.34f, float cuerpoFrac = 0.62f,
            float halo = 0.13f,
            float ruidoInten = 1f, float ruidoEscala = 1f, float ruidoFlujo = 1f,
            float luz = 1f)
        {
            return new EstiloAlas
            {
                Paleta = paleta,
                ColorDia = dia,
                FuerzaDia = 0.62f,
                Plumas = plumas,
                LargoInterior = largoInt, LargoExterior = largoExt,
                RaizSemiancho = raiz,
                PuntaSemianchoBase = punta, PuntaSemianchoAbierto = puntaAbierta,
                PlegadoMin = plegadoMin, PlegadoMax = plegadoMax,
                AbiertoMin = abiertoMin, AbiertoMax = abiertoMax,
                Inclinacion = inclinacion, Barrido = barrido,
                Curva = curva, AnclajeSep = anclajeSep,
                HueEntre = hueEntre, HueLargo = hueLargo, HueTiempo = hueTiempo,
                FlapVel = flap, Desgarre = desgarre,
                NucleoInten = nucleo, NucleoFrac = nucleoFrac,
                CuerpoInten = cuerpo, CuerpoFrac = cuerpoFrac,
                HaloInten = halo,
                Ruido = PrefijoRuido + ruido,
                RuidoInten = ruidoInten,
                RuidoEscala = ruidoEscala, RuidoFlujo = ruidoFlujo,
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
