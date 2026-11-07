using Microsoft.Xna.Framework;

namespace AethonMod.Content.VFX
{
    // v6.56.0 — clase reconstruida del BINARIO verificado tras el wipe del sandbox:
    // la fuente original se perdió; este código viene del .tmod (md5 3b45a5fe…) y
    // se verificó idéntico al recompilar (ver CHANGES.md v6.56.0 y worklog Task 69).
    // Los comentarios de la fuente original no viajan dentro del .dll.
    public struct EstiloAlas
    {
        public Color[] Paleta;

        public Color ColorDia;

        public float FuerzaDia;

        public int Plumas;

        public float LargoInterior;

        public float LargoExterior;

        public float RaizSemiancho;

        public float PuntaSemianchoBase;

        public float PuntaSemianchoAbierto;

        public float PlegadoMin;

        public float PlegadoMax;

        public float AbiertoMin;

        public float AbiertoMax;

        public float Inclinacion;

        public float Barrido;

        public float Curva;

        public float AnclajeSep;

        public float Vientre;

        public float Afilada;

        public float Filosa;

        public byte Runas;

        public float RunasInten;

        public float HueEntre;

        public float HueLargo;

        public float HueTiempo;

        public float FlapVel;

        public float Desgarre;

        public float NucleoInten;

        public float NucleoFrac;

        public float CuerpoInten;

        public float CuerpoFrac;

        public float HaloInten;

        public string Ruido;

        public float RuidoInten;

        public float RuidoEscala;

        public float RuidoFlujo;

        public float Luz;
    }

    // v6.113 — LA PURGA DE LAS ALAS (la letra: «guardar las siguientes
    // alas: alas runicas del eclipse, corolas del alba, alas runicas del
    // vacio, alas runicas solares, alas runicas del juicio — el resto de
    // alas las puedes borrar»): el catálogo quedó en LAS CINCO DE LA CASA
    // (1 Solares · 2 Vacío · 3 Eclipse · 4 Juicio · 5 Corolas) — la Aurora
    // de Tinta original, LAS QUINCE del prisma (2-16), las Agujas del
    // Vacío y las Cuchillas del Eclipse MURIERON con sus ítems.
    //
    // LA 2 NUEVA — LAS RUNAS DEL VACÍO (la letra: «toma las alas runicas
    // del eclipse y has una copia llamada alas runicas del vacio, a esta
    // copia debes mejorar las alas, quitandole los picos y añadiendo
    // afectos, ademas de que las alas deben ser iguales tanto izquierda
    // como derecha o sea me refiero a no tener picos, su color debe ser
    // el color del vacio»): LA COPIA DEL ECLIPSE CURADA — el mismo abanico
    // de 5 plumas y el mismo tempo (0.95), pero SIN PICOS (afilada 0.5 +
    // puntas 20/14: el perfil redondeado, el taper suave), PERFECTAMENTE
    // SIMÉTRICA (filosa 0: los DOS bordes idénticos — el espejo de la
    // v6.56.4 hace el resto), CON EL COLOR DEL VACÍO de la casa (la rampa
    // del aura: 150,84,235 vivo · 122,78,188 hondo · 26,14,44 el fondo)
    // y CON SUS EFECTOS: el anillo de runas del vacío en las puntas
    // (Runas=2, las de OrbitaLib en violeta) + LAS MIGAS DEL VACÍO (las
    // motas que caen de las alas — viven en AlasAuroraHalo.AI).
    public static class AlasCatalogo
    {
        public const byte Solares = 1;
        public const byte Vacio = 2;
        public const byte Eclipse = 3;
        public const byte Juicio = 4;
        public const byte Corolas = 5;

        private const string PrefijoRuido = "AethonMod/Content/Effects/Procedural/";

        public static readonly EstiloAlas[] Todos = Crear();

        private static EstiloAlas[] Crear()
        {
            //IL_002b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0030: Unknown result type (might be due to invalid IL or missing references)
            //IL_01e1: Unknown result type (might be due to invalid IL or missing references)
            //IL_01e6: Unknown result type (might be due to invalid IL or missing references)
            //IL_01f9: Unknown result type (might be due to invalid IL or missing references)
            //IL_01fe: Unknown result type (might be due to invalid IL or missing references)
            //IL_020b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0210: Unknown result type (might be due to invalid IL or missing references)
            //IL_0224: Unknown result type (might be due to invalid IL or missing references)
            //IL_02f0: Unknown result type (might be due to invalid IL or missing references)
            //IL_02f5: Unknown result type (might be due to invalid IL or missing references)
            //IL_0302: Unknown result type (might be due to invalid IL or missing references)
            //IL_0307: Unknown result type (might be due to invalid IL or missing references)
            //IL_0313: Unknown result type (might be due to invalid IL or missing references)
            //IL_0318: Unknown result type (might be due to invalid IL or missing references)
            //IL_0329: Unknown result type (might be due to invalid IL or missing references)
            //IL_03f5: Unknown result type (might be due to invalid IL or missing references)
            //IL_03fa: Unknown result type (might be due to invalid IL or missing references)
            //IL_040d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0412: Unknown result type (might be due to invalid IL or missing references)
            //IL_0422: Unknown result type (might be due to invalid IL or missing references)
            //IL_0427: Unknown result type (might be due to invalid IL or missing references)
            //IL_043b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0507: Unknown result type (might be due to invalid IL or missing references)
            //IL_050c: Unknown result type (might be due to invalid IL or missing references)
            //IL_051f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0524: Unknown result type (might be due to invalid IL or missing references)
            //IL_0534: Unknown result type (might be due to invalid IL or missing references)
            //IL_0539: Unknown result type (might be due to invalid IL or missing references)
            //IL_054d: Unknown result type (might be due to invalid IL or missing references)
            //IL_061c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0621: Unknown result type (might be due to invalid IL or missing references)
            //IL_0634: Unknown result type (might be due to invalid IL or missing references)
            //IL_0639: Unknown result type (might be due to invalid IL or missing references)
            //IL_0649: Unknown result type (might be due to invalid IL or missing references)
            //IL_064e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0662: Unknown result type (might be due to invalid IL or missing references)
            //IL_0731: Unknown result type (might be due to invalid IL or missing references)
            //IL_0736: Unknown result type (might be due to invalid IL or missing references)
            //IL_0743: Unknown result type (might be due to invalid IL or missing references)
            //IL_0748: Unknown result type (might be due to invalid IL or missing references)
            //IL_0754: Unknown result type (might be due to invalid IL or missing references)
            //IL_0759: Unknown result type (might be due to invalid IL or missing references)
            //IL_0767: Unknown result type (might be due to invalid IL or missing references)
            //IL_0833: Unknown result type (might be due to invalid IL or missing references)
            //IL_0838: Unknown result type (might be due to invalid IL or missing references)
            //IL_084b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0850: Unknown result type (might be due to invalid IL or missing references)
            //IL_0863: Unknown result type (might be due to invalid IL or missing references)
            //IL_0868: Unknown result type (might be due to invalid IL or missing references)
            //IL_087b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0880: Unknown result type (might be due to invalid IL or missing references)
            //IL_0893: Unknown result type (might be due to invalid IL or missing references)
            //IL_0898: Unknown result type (might be due to invalid IL or missing references)
            //IL_08ac: Unknown result type (might be due to invalid IL or missing references)
            //IL_097c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0981: Unknown result type (might be due to invalid IL or missing references)
            //IL_0997: Unknown result type (might be due to invalid IL or missing references)
            //IL_099c: Unknown result type (might be due to invalid IL or missing references)
            //IL_09af: Unknown result type (might be due to invalid IL or missing references)
            //IL_09b4: Unknown result type (might be due to invalid IL or missing references)
            //IL_09c8: Unknown result type (might be due to invalid IL or missing references)
            //IL_0a98: Unknown result type (might be due to invalid IL or missing references)
            //IL_0a9d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0ab0: Unknown result type (might be due to invalid IL or missing references)
            //IL_0ab5: Unknown result type (might be due to invalid IL or missing references)
            //IL_0ac2: Unknown result type (might be due to invalid IL or missing references)
            //IL_0ac7: Unknown result type (might be due to invalid IL or missing references)
            //IL_0adb: Unknown result type (might be due to invalid IL or missing references)
            //IL_0ba8: Unknown result type (might be due to invalid IL or missing references)
            //IL_0bad: Unknown result type (might be due to invalid IL or missing references)
            //IL_0bbd: Unknown result type (might be due to invalid IL or missing references)
            //IL_0bc2: Unknown result type (might be due to invalid IL or missing references)
            //IL_0bcf: Unknown result type (might be due to invalid IL or missing references)
            //IL_0bd4: Unknown result type (might be due to invalid IL or missing references)
            //IL_0be8: Unknown result type (might be due to invalid IL or missing references)
            //IL_0cb5: Unknown result type (might be due to invalid IL or missing references)
            //IL_0cba: Unknown result type (might be due to invalid IL or missing references)
            //IL_0ccd: Unknown result type (might be due to invalid IL or missing references)
            //IL_0cd2: Unknown result type (might be due to invalid IL or missing references)
            //IL_0ce8: Unknown result type (might be due to invalid IL or missing references)
            //IL_0ced: Unknown result type (might be due to invalid IL or missing references)
            //IL_0d01: Unknown result type (might be due to invalid IL or missing references)
            //IL_0dd1: Unknown result type (might be due to invalid IL or missing references)
            //IL_0dd6: Unknown result type (might be due to invalid IL or missing references)
            //IL_0de9: Unknown result type (might be due to invalid IL or missing references)
            //IL_0dee: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e01: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e06: Unknown result type (might be due to invalid IL or missing references)
            //IL_0e1a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0ee4: Unknown result type (might be due to invalid IL or missing references)
            //IL_0ee9: Unknown result type (might be due to invalid IL or missing references)
            //IL_0ef6: Unknown result type (might be due to invalid IL or missing references)
            //IL_0efb: Unknown result type (might be due to invalid IL or missing references)
            //IL_0f06: Unknown result type (might be due to invalid IL or missing references)
            //IL_0f0b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0f16: Unknown result type (might be due to invalid IL or missing references)
            //IL_0fe6: Unknown result type (might be due to invalid IL or missing references)
            //IL_0feb: Unknown result type (might be due to invalid IL or missing references)
            //IL_0ffe: Unknown result type (might be due to invalid IL or missing references)
            //IL_1003: Unknown result type (might be due to invalid IL or missing references)
            //IL_1019: Unknown result type (might be due to invalid IL or missing references)
            //IL_101e: Unknown result type (might be due to invalid IL or missing references)
            //IL_1032: Unknown result type (might be due to invalid IL or missing references)
            //IL_10ff: Unknown result type (might be due to invalid IL or missing references)
            //IL_1104: Unknown result type (might be due to invalid IL or missing references)
            //IL_1111: Unknown result type (might be due to invalid IL or missing references)
            //IL_1116: Unknown result type (might be due to invalid IL or missing references)
            //IL_1122: Unknown result type (might be due to invalid IL or missing references)
            //IL_1127: Unknown result type (might be due to invalid IL or missing references)
            //IL_113b: Unknown result type (might be due to invalid IL or missing references)
            //IL_11fc: Unknown result type (might be due to invalid IL or missing references)
            //IL_1201: Unknown result type (might be due to invalid IL or missing references)
            //IL_1217: Unknown result type (might be due to invalid IL or missing references)
            //IL_121c: Unknown result type (might be due to invalid IL or missing references)
            //IL_1223: Unknown result type (might be due to invalid IL or missing references)
            //IL_1228: Unknown result type (might be due to invalid IL or missing references)
            //IL_122d: Unknown result type (might be due to invalid IL or missing references)
            //IL_12fa: Unknown result type (might be due to invalid IL or missing references)
            //IL_12ff: Unknown result type (might be due to invalid IL or missing references)
            //IL_130f: Unknown result type (might be due to invalid IL or missing references)
            //IL_1314: Unknown result type (might be due to invalid IL or missing references)
            //IL_1327: Unknown result type (might be due to invalid IL or missing references)
            //IL_132c: Unknown result type (might be due to invalid IL or missing references)
            //IL_1340: Unknown result type (might be due to invalid IL or missing references)
            //IL_1401: Unknown result type (might be due to invalid IL or missing references)
            //IL_1406: Unknown result type (might be due to invalid IL or missing references)
            //IL_1416: Unknown result type (might be due to invalid IL or missing references)
            //IL_141b: Unknown result type (might be due to invalid IL or missing references)
            //IL_1428: Unknown result type (might be due to invalid IL or missing references)
            //IL_142d: Unknown result type (might be due to invalid IL or missing references)
            //IL_143b: Unknown result type (might be due to invalid IL or missing references)
            //IL_14fc: Unknown result type (might be due to invalid IL or missing references)
            //IL_1501: Unknown result type (might be due to invalid IL or missing references)
            //IL_1508: Unknown result type (might be due to invalid IL or missing references)
            //IL_150d: Unknown result type (might be due to invalid IL or missing references)
            //IL_1514: Unknown result type (might be due to invalid IL or missing references)
            //IL_1519: Unknown result type (might be due to invalid IL or missing references)
            //IL_1520: Unknown result type (might be due to invalid IL or missing references)
            //IL_1525: Unknown result type (might be due to invalid IL or missing references)
            //IL_152c: Unknown result type (might be due to invalid IL or missing references)
            //IL_1531: Unknown result type (might be due to invalid IL or missing references)
            //IL_1536: Unknown result type (might be due to invalid IL or missing references)
            //IL_1603: Unknown result type (might be due to invalid IL or missing references)
            //IL_1608: Unknown result type (might be due to invalid IL or missing references)
            //IL_1618: Unknown result type (might be due to invalid IL or missing references)
            //IL_161d: Unknown result type (might be due to invalid IL or missing references)
            //IL_1630: Unknown result type (might be due to invalid IL or missing references)
            //IL_1635: Unknown result type (might be due to invalid IL or missing references)
            //IL_1646: Unknown result type (might be due to invalid IL or missing references)
            //IL_1713: Unknown result type (might be due to invalid IL or missing references)
            //IL_1718: Unknown result type (might be due to invalid IL or missing references)
            //IL_171f: Unknown result type (might be due to invalid IL or missing references)
            //IL_1724: Unknown result type (might be due to invalid IL or missing references)
            //IL_1730: Unknown result type (might be due to invalid IL or missing references)
            //IL_1735: Unknown result type (might be due to invalid IL or missing references)
            //IL_1746: Unknown result type (might be due to invalid IL or missing references)
            //IL_1807: Unknown result type (might be due to invalid IL or missing references)
            //IL_180c: Unknown result type (might be due to invalid IL or missing references)
            //IL_1813: Unknown result type (might be due to invalid IL or missing references)
            //IL_1818: Unknown result type (might be due to invalid IL or missing references)
            //IL_182e: Unknown result type (might be due to invalid IL or missing references)
            //IL_1833: Unknown result type (might be due to invalid IL or missing references)
            //IL_1847: Unknown result type (might be due to invalid IL or missing references)
            // v6.113 — EL CATÁLOGO COMPACTO: [0] default · [1] Runas
            // Solares · [2] Runas del Vacío (LA COPIA MEJORADA DEL ECLIPSE)
            // · [3] Runas del Eclipse · [4] Runas del Juicio · [5] Corolas
            // del Alba. Las QUINCE del prisma, la Aurora de Tinta, las
            // Agujas y las Cuchillas cayeron con sus ítems (la purga).
            return new EstiloAlas[6]
            {
                default(EstiloAlas),
                Ala("Runas Solares", (Color[])(object)new Color[3]
                {
                    DefinitivasLib.Oro,
                    new Color(255, 214, 130),
                    DefinitivasLib.BlancoCalido
                }, DefinitivasLib.BlancoCalido, "Ruido_13", 5, 92f, 50f, 3.5f, 14f, 8f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.34f, 5f, 0.3f, 1f, 0.35f, 0.05f, 0.1f, 0.018f, 0.85f, 1f, 0.95f, 0.3f, 0.34f, 0.62f, 0.16f, 1f, 1f, 1f, 1.2f, 1, 1f),
                // v6.113 — LA 2: LA COPIA DEL ECLIPSE SIN PICOS, SIMÉTRICA
                // y DEL COLOR DEL VACÍO (la rampa del aura de la casa).
                // Mismo abanico/tempo que la 3; filosa 0 (bordes gemelos),
                // afilada 0.5 + puntas 20/14 (redondez), Runas=2 (el anillo
                // violeta del vacío en las puntas).
                Ala("Runas del Vacío", (Color[])(object)new Color[3]
                {
                    new Color(150, 84, 235),
                    new Color(122, 78, 188),
                    new Color(26, 14, 44)
                }, new Color(90, 60, 140), "Ruido_6", 5, 90f, 52f, 3.5f, 20f, 14f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.18f, 5f, 0.45f, 0.5f, 0f, 0.085f, 0.16f, 0.018f, 0.95f, 1f, 0.88f, 0.3f, 0.36f, 0.62f, 0.15f, 1f, 1f, 1f, 1.05f, 2, 1.05f),
                Ala("Runas del Eclipse", (Color[])(object)new Color[3]
                {
                    DefinitivasLib.Ambar,
                    new Color(158, 78, 22),
                    new Color(64, 28, 10)
                }, new Color(180, 120, 70), "Ruido_14", 5, 90f, 52f, 3.5f, 19f, 12f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.18f, 5f, 0.45f, 0.55f, 0.05f, 0.085f, 0.16f, 0.018f, 0.95f, 1f, 0.88f, 0.3f, 0.36f, 0.62f, 0.15f, 1f, 1f, 1f, 1.05f, 3, 1f),
                Ala("Runas del Juicio", (Color[])(object)new Color[5]
                {
                    DefinitivasLib.Oro,
                    DefinitivasLib.Ambar,
                    DefinitivasLib.BlancoCalido,
                    DefinitivasLib.Violeta,
                    DefinitivasLib.Cian
                }, DefinitivasLib.BlancoCalido, "Ruido_16", 5, 94f, 50f, 3.5f, 14f, 8f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.26f, 5f, 0.32f, 1.2f, 0.55f, 0.16f, 0.2f, 0.024f, 1.1f, 1f, 0.95f, 0.3f, 0.36f, 0.62f, 0.16f, 1f, 1f, 1f, 1.25f, 4, 1.1f),
                Ala("Corolas del Alba", (Color[])(object)new Color[3]
                {
                    DefinitivasLib.BlancoCalido,
                    DefinitivasLib.Oro,
                    new Color(255, 226, 160)
                }, new Color(255, 250, 240), "Ruido_6", 7, 86f, 54f, 3.5f, 16f, 11f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.14f, 9f, 0.4f, 0.5f, 0f, 0.085f, 0.16f, 0.018f, 0.7f, 0.6f, 0.9f, 0.3f, 0.34f, 0.62f, 0.17f, 1f, 1f, 1f, 1.1f, 0)
            };
        }

        private static EstiloAlas Ala(string _, Color[] paleta, Color dia, string ruido, int plumas = 5, float largoInt = 88f, float largoExt = 48f, float raiz = 3.5f, float punta = 14f, float puntaAbierta = 8f, float plegadoMin = 0.5f, float plegadoMax = 0.92f, float abiertoMin = 0.22f, float abiertoMax = 1.54f, float inclinacion = 0.12f, float barrido = 0.14f, float curva = 0f, float anclajeSep = 5f, float vientre = 0.35f, float afilada = 1f, float filosa = 0.3f, float hueEntre = 0.085f, float hueLargo = 0.16f, float hueTiempo = 0.018f, float flap = 1f, float desgarre = 1f, float nucleo = 0.92f, float nucleoFrac = 0.3f, float cuerpo = 0.34f, float cuerpoFrac = 0.62f, float halo = 0.13f, float ruidoInten = 1f, float ruidoEscala = 1f, float ruidoFlujo = 1f, float luz = 1f, byte runas = 0, float runasInten = 0f)
        {
            //IL_0012: Unknown result type (might be due to invalid IL or missing references)
            //IL_0013: Unknown result type (might be due to invalid IL or missing references)
            return new EstiloAlas
            {
                Paleta = paleta,
                ColorDia = dia,
                FuerzaDia = 0.62f,
                Plumas = plumas,
                LargoInterior = largoInt,
                LargoExterior = largoExt,
                RaizSemiancho = raiz,
                PuntaSemianchoBase = punta,
                PuntaSemianchoAbierto = puntaAbierta,
                PlegadoMin = plegadoMin,
                PlegadoMax = plegadoMax,
                AbiertoMin = abiertoMin,
                AbiertoMax = abiertoMax,
                Inclinacion = inclinacion,
                Barrido = barrido,
                Curva = curva,
                AnclajeSep = anclajeSep,
                Vientre = vientre,
                Afilada = afilada,
                Filosa = filosa,
                HueEntre = hueEntre,
                HueLargo = hueLargo,
                HueTiempo = hueTiempo,
                FlapVel = flap,
                Desgarre = desgarre,
                NucleoInten = nucleo,
                NucleoFrac = nucleoFrac,
                CuerpoInten = cuerpo,
                CuerpoFrac = cuerpoFrac,
                HaloInten = halo,
                Ruido = "AethonMod/Content/Effects/Procedural/" + ruido,
                RuidoInten = ruidoInten,
                RuidoEscala = ruidoEscala,
                RuidoFlujo = ruidoFlujo,
                Luz = luz,
                Runas = runas,
                RunasInten = runasInten
            };
        }

        public static ref readonly EstiloAlas De(byte indice)
        {
            if (indice <= 0 || indice >= Todos.Length)
            {
                return ref Todos[1];
            }
            return ref Todos[indice];
        }
    }
}
