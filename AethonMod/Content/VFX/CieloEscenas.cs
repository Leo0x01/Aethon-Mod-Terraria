using Microsoft.Xna.Framework;

namespace AethonMod.Content.VFX
{
    // ======================================================================
    //  v6.43 — LAS TRES ESCENAS DE DEMOSTRACIÓN de CieloLib (las que
    //  despliega el Prisma de Paisajes). Cada una es un PAISAJE COMPLETO:
    //  2-3 capas con su parallax, su deriva y su profundidad + los efectos
    //  de la escena (el tinte del cielo, el tinte de la luz de fondo y el
    //  brillo de la iluminación). Todas las texturas son 1024×256 sin
    //  costura horizontal (generadas por tools/gen_cielo_v643.py).
    //
    //  Lazy: la escena se construye la primera vez que se pide y NUNCA se
    //  tira (cero GC al ciclar el prisma).
    // ======================================================================

    /// <summary>El catálogo de escenas de la casa.</summary>
    public static class CieloEscenas
    {
        // --- LOS NOMBRES (el prisma cicla contra ellos) ---
        /// <summary>La primera escena: el eclipse carmesí y violeta.</summary>
        public const string NombreEclipse = "Eclipse Umbral";

        /// <summary>La segunda escena: la noche azul y su lluvia de estrellas.</summary>
        public const string NombreEstelar = "Lluvia Estelar";

        /// <summary>La tercera escena: el primer amanecer del mundo.</summary>
        public const string NombreAlba = "Amanecer Primordial";

        // --- LAS ESCENAS (lazy) ---
        private static EscenaDeCielo _eclipse;
        private static EscenaDeCielo _estelar;
        private static EscenaDeCielo _alba;

        /// <summary>EL ECLIPSE UMBRAL: la nebulosa carmesí detrás, las
        /// montañas violetas al medio y las siluetas oscuras de primer
        /// plano — el cielo se apaga y la luz baja a penumbra.</summary>
        public static EscenaDeCielo EclipseUmbral => _eclipse ??= new EscenaDeCielo(NombreEclipse)
        {
            // LAS CAPAS (el constructor las ordena por profundidad):
            Capas =
            {
                // La nebulosa del eclipse — BRILLANTE (suma su resplandor) y
                // derivando despacio: el telón de fondo del fenómeno.
                new Capa
                {
                    RutaTextura = "Content/Effects/Cielo/EclipseNebulosa",
                    Parallax = 0.08f, OffsetY = 60f, Escala = 0.95f, Alpha = 0.50f,
                    Tinte = new Color(255, 255, 255, 255),
                    ScrollX = 5f, Profundidad = 0.12f, Brillante = true,
                },
                // Las montañas violetas — la cordillera media del eclipse.
                new Capa
                {
                    RutaTextura = "Content/Effects/Cielo/EclipseMontanas",
                    Parallax = 0.30f, OffsetY = 40f, Escala = 0.68f, Alpha = 1f,
                    Tinte = new Color(255, 255, 255, 255),
                    ScrollX = 0f, Profundidad = 0.50f, Brillante = false,
                },
                // Las siluetas cercanas — casi negras, carmesí al filo.
                new Capa
                {
                    RutaTextura = "Content/Effects/Cielo/EclipseSiluetas",
                    Parallax = 0.58f, OffsetY = 12f, Escala = 0.55f, Alpha = 1f,
                    Tinte = new Color(255, 214, 232, 255),
                    ScrollX = 0f, Profundidad = 0.88f, Brillante = false,
                },
            },
            // LOS EFECTOS: el cielo carmesí oscuro, la luz de fondo violeta
            // y la penumbra del eclipse.
            TinteDelCielo = new Color(48, 6, 24),
            TinteDelFondo = new Color(96, 24, 64),
            Brillo = 0.86f,
            FadeInSeg = 1.6f,
            FadeOutSeg = 1.2f,
        };

        /// <summary>LA LLUVIA ESTELAR: el campo de estrellas fijo en el
        /// cielo, las nubes azules que derivan y las montañas de la noche
        /// — el fondo se tiñe de azul profundo y el brillo apenas baja.</summary>
        public static EscenaDeCielo LluviaEstelar => _estelar ??= new EscenaDeCielo(NombreEstelar)
        {
            Capas =
            {
                // Las estrellas — BRILLANTES y casi fijas (el parallax mínimo
                // del cielo lejano) con su deriva lenta de bóveda.
                new Capa
                {
                    RutaTextura = "Content/Effects/Cielo/EstelarEstrellas",
                    Parallax = 0.05f, OffsetY = 30f, Escala = 1.00f, Alpha = 1f,
                    Tinte = new Color(255, 255, 255, 255),
                    ScrollX = 8f, Profundidad = 0.10f, Brillante = true,
                },
                // Las nubes azules de la noche — translúcidas, viajando.
                new Capa
                {
                    RutaTextura = "Content/Effects/Cielo/EstelarNubes",
                    Parallax = 0.30f, OffsetY = 50f, Escala = 0.78f, Alpha = 0.65f,
                    Tinte = new Color(255, 255, 255, 255),
                    ScrollX = 16f, Profundidad = 0.45f, Brillante = false,
                },
                // Las montañas de la noche — azul oscuro, primer plano.
                new Capa
                {
                    RutaTextura = "Content/Effects/Cielo/EstelarMontanas",
                    Parallax = 0.55f, OffsetY = 10f, Escala = 0.55f, Alpha = 1f,
                    Tinte = new Color(255, 255, 255, 255),
                    ScrollX = 0f, Profundidad = 0.85f, Brillante = false,
                },
            },
            TinteDelCielo = new Color(10, 14, 44),
            TinteDelFondo = new Color(36, 52, 120),
            Brillo = 0.94f,
            FadeInSeg = 1.4f,
            FadeOutSeg = 1.0f,
        };

        /// <summary>EL AMANECER PRIMORDIAL: la nebulosa dorada que
        /// resplandece en el horizonte, la bruma naranja que cruza y las
        /// colinas doradas de primer plano — el cielo arde en dorado suave
        /// y la iluminación sube.</summary>
        public static EscenaDeCielo AmanecerPrimordial => _alba ??= new EscenaDeCielo(NombreAlba)
        {
            Capas =
            {
                // La nebulosa dorada — BRILLANTE: el resplandor del alba.
                new Capa
                {
                    RutaTextura = "Content/Effects/Cielo/AlbaNebulosa",
                    Parallax = 0.07f, OffsetY = 70f, Escala = 0.90f, Alpha = 0.60f,
                    Tinte = new Color(255, 235, 200, 255),
                    ScrollX = 4f, Profundidad = 0.10f, Brillante = true,
                },
                // La bruma naranja — derivando despacio, translúcida.
                new Capa
                {
                    RutaTextura = "Content/Effects/Cielo/AlbaBruma",
                    Parallax = 0.28f, OffsetY = 40f, Escala = 0.72f, Alpha = 0.60f,
                    Tinte = new Color(255, 255, 255, 255),
                    ScrollX = 12f, Profundidad = 0.46f, Brillante = false,
                },
                // Las colinas doradas — el primer plano del amanecer.
                new Capa
                {
                    RutaTextura = "Content/Effects/Cielo/AlbaColinas",
                    Parallax = 0.55f, OffsetY = 8f, Escala = 0.55f, Alpha = 1f,
                    Tinte = new Color(255, 236, 200, 255),
                    ScrollX = 0f, Profundidad = 0.88f, Brillante = false,
                },
            },
            TinteDelCielo = new Color(255, 178, 92),
            TinteDelFondo = new Color(255, 204, 130),
            Brillo = 1.10f,
            FadeInSeg = 1.8f,
            FadeOutSeg = 1.4f,
        };
    }
}
