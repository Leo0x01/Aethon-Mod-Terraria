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

    // v6.56.0 — clase reconstruida del BINARIO verificado tras el wipe del sandbox:
    // la fuente original se perdió; este código viene del .tmod (md5 3b45a5fe…) y
    // se verificó idéntico al recompilar (ver CHANGES.md v6.56.0 y worklog Task 69).
    // Los comentarios de la fuente original no viajan dentro del .dll.
    public static class AlasCatalogo
    {
        public const byte Aurora = 1;

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
            return new EstiloAlas[24]
            {
                default(EstiloAlas),
                new EstiloAlas
                {
                    Paleta = null,
                    ColorDia = new Color(255, 243, 214),
                    FuerzaDia = 0.62f,
                    Plumas = 5,
                    LargoInterior = 88f,
                    LargoExterior = 48f,
                    RaizSemiancho = 3.5f,
                    PuntaSemianchoBase = 14f,
                    PuntaSemianchoAbierto = 8f,
                    PlegadoMin = 0.5f,
                    PlegadoMax = 0.92f,
                    AbiertoMin = 0.22f,
                    AbiertoMax = 1.54f,
                    Inclinacion = 0.12f,
                    Barrido = 0.14f,
                    Curva = 0f,
                    AnclajeSep = 5f,
                    Vientre = 0.34f,
                    Afilada = 1.1f,
                    Filosa = 0.25f,
                    HueEntre = 0.085f,
                    HueLargo = 0.16f,
                    HueTiempo = 0.018f,
                    FlapVel = 1f,
                    Desgarre = 1f,
                    NucleoInten = 0.92f,
                    NucleoFrac = 0.3f,
                    CuerpoInten = 0.34f,
                    CuerpoFrac = 0.62f,
                    HaloInten = 0.13f,
                    Ruido = "AethonMod/Content/Effects/Procedural/Noise",
                    RuidoInten = 1f,
                    RuidoEscala = 1f,
                    RuidoFlujo = 1f,
                    Luz = 1f
                },
                Ala("Tinta Serena", (Color[])(object)new Color[3]
                {
                    new Color(201, 166, 255),
                    new Color(140, 60, 210),
                    new Color(75, 30, 122)
                }, new Color(232, 214, 255), "Ruido_7", 6, 96f, 52f, 3.5f, 14f, 8f, 0.52f, 0.98f, 0.18f, 1.05f, 0.12f, 0.14f, 0.22f, 5f, 0.42f, 0.7f, 0.35f, 0.085f, 0.16f, 0.018f, 1f, 1f, 0.85f, 0.3f, 0.3f, 0.62f, 0.14f, 1f, 1f, 1f, 1f, 0),
                Ala("Eclipse", (Color[])(object)new Color[3]
                {
                    new Color(157, 92, 232),
                    new Color(75, 18, 119),
                    new Color(20, 6, 31)
                }, new Color(140, 100, 190), "Ruido_4", 4, 86f, 46f, 3.5f, 18f, 8f, 0.46f, 0.9f, 0.3f, 1.7f, 0.12f, 0.22f, 0.3f, 5f, 0.38f, 0.9f, 0.3f, 0.085f, 0.16f, 0.018f, 1f, 1f, 0.8f, 0.3f, 0.38f, 0.62f, 0.16f, 1f, 1f, 1f, 1f, 0),
                Ala("Plasma", (Color[])(object)new Color[3]
                {
                    new Color(255, 61, 219),
                    new Color(199, 36, 184),
                    new Color(122, 47, 209)
                }, new Color(255, 214, 246), "Ruido_5", 7, 70f, 40f, 3.5f, 10f, 8f, 0.54f, 0.96f, 0.2f, 1.48f, 0.12f, 0.14f, -0.18f, 5f, 0.18f, 1.9f, 0.85f, 0.085f, 0.16f, 0.018f, 1.35f, 1.3f, 1f, 0.3f, 0.42f, 0.62f, 0.18f, 1.2f, 1f, 1.6f, 1f, 0),
                Ala("Nebulosa", (Color[])(object)new Color[3]
                {
                    new Color(184, 127, 232),
                    new Color(209, 92, 255),
                    new Color(110, 123, 232)
                }, new Color(240, 220, 255), "Investigacion/Espacio_1", 6, 100f, 56f, 3.5f, 14f, 8f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.12f, 5f, 0.3f, 1f, 0.15f, 0.085f, 0.16f, 0.018f, 1f, 1f, 0.82f, 0.3f, 0.4f, 0.62f, 0.2f, 1.25f, 1.4f, 0.7f, 1.15f, 0),
                Ala("Cometario", (Color[])(object)new Color[3]
                {
                    new Color(255, 243, 214),
                    new Color(125, 232, 255),
                    new Color(92, 59, 209)
                }, new Color(255, 250, 240), "Ruido_12", 5, 96f, 44f, 3.5f, 14f, 8f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, -0.1f, 5f, 0.25f, 1.5f, 0.7f, 0.085f, 0.22f, 0.018f, 1f, 1f, 1f, 0.3f, 0.3f, 0.62f, 0.12f, 1f, 1f, 1f, 1f, 0),
                Ala("Medianoche", (Color[])(object)new Color[3]
                {
                    new Color(245, 240, 255),
                    new Color(48, 26, 96),
                    new Color(14, 8, 30)
                }, new Color(120, 110, 160), "Ruido_2", 4, 92f, 50f, 3.5f, 20f, 12f, 0.48f, 0.94f, 0.28f, 1.62f, 0.12f, 0.14f, 0.38f, 5f, 0.5f, 0.65f, 0.2f, 0.085f, 0.16f, 0.018f, 1f, 1f, 0.7f, 0.3f, 0.22f, 0.62f, 0.1f, 0.9f, 1f, 1f, 0.8f, 0),
                Ala("Prisma", (Color[])(object)new Color[5]
                {
                    new Color(174, 23, 189),
                    new Color(237, 128, 60),
                    new Color(247, 255, 101),
                    new Color(176, 234, 85),
                    new Color(102, 219, 249)
                }, new Color(255, 243, 214), "Ruido_11", 7, 72f, 42f, 3.5f, 11f, 8f, 0.52f, 0.95f, 0.22f, 1.44f, 0.12f, 0.14f, 0f, 5f, 0.2f, 1.8f, 0.6f, 0.12f, 0.16f, 0.026f, 1f, 1f, 0.95f, 0.3f, 0.38f, 0.62f, 0.16f, 1.1f, 1f, 1f, 1.2f, 0),
                Ala("Fuego Frío", (Color[])(object)new Color[3]
                {
                    new Color(234, 246, 255),
                    new Color(185, 166, 255),
                    new Color(125, 209, 232)
                }, new Color(250, 252, 255), "Ruido_8", 6, 90f, 54f, 3.5f, 16f, 8f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.16f, 5f, 0.4f, 0.9f, 0.3f, 0.085f, 0.24f, 0.018f, 0.85f, 1f, 0.88f, 0.3f, 0.3f, 0.62f, 0.15f, 1f, 1f, 1f, 1f, 0),
                Ala("Tormenta", (Color[])(object)new Color[3]
                {
                    new Color(240, 230, 255),
                    new Color(178, 76, 232),
                    new Color(61, 26, 92)
                }, new Color(226, 216, 246), "Ruido_9", 5, 82f, 46f, 3.5f, 11f, 8f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.22f, -0.22f, 5f, 0.24f, 1.7f, 0.8f, 0.085f, 0.16f, 0.018f, 1.5f, 1.5f, 1f, 0.3f, 0.36f, 0.62f, 0.14f, 1.15f, 1f, 1f, 1f, 0),
                Ala("Marioneta", (Color[])(object)new Color[3]
                {
                    new Color(227, 92, 133),
                    new Color(138, 26, 61),
                    new Color(26, 10, 20)
                }, new Color(210, 140, 160), "Ruido_15", 4, 84f, 38f, 3.5f, 9f, 8f, 0.56f, 1.02f, 0.24f, 1.3f, 0.2f, 0.14f, 0.5f, 5f, 0.35f, 1.3f, 0.4f, 0.085f, 0.16f, 0.018f, 1f, 1f, 0.85f, 0.3f, 0.34f, 0.62f, 0.12f, 1f, 1f, 1f, 0.9f, 0),
                Ala("Aurora Ártica", (Color[])(object)new Color[3]
                {
                    new Color(125, 232, 154),
                    new Color(125, 232, 224),
                    new Color(185, 166, 255)
                }, new Color(235, 255, 250), "Ruido_0", 7, 112f, 64f, 3.5f, 14f, 8f, 0.44f, 0.84f, 0.2f, 1.35f, 0.12f, 0.14f, 0.06f, 5f, 0.32f, 1f, 0.1f, 0.085f, 0.28f, 0.018f, 0.75f, 1f, 0.8f, 0.3f, 0.32f, 0.62f, 0.18f, 1f, 1f, 1f, 1.1f, 0),
                Ala("Orquídea", (Color[])(object)new Color[3]
                {
                    new Color(255, 138, 209),
                    new Color(232, 79, 199),
                    new Color(166, 61, 232)
                }, new Color(255, 224, 242), "Ruido_18", 5, 88f, 54f, 3.5f, 22f, 14f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.2f, 5f, 0.5f, 0.6f, 0.05f, 0.1f, 0.16f, 0.018f, 1f, 1f, 0.92f, 0.3f, 0.36f, 0.62f, 0.16f, 1.1f, 1f, 1f, 1f, 0),
                Ala("Vacío", (Color[])(object)new Color[3]
                {
                    new Color(107, 47, 160),
                    new Color(40, 14, 66),
                    new Color(8, 3, 13)
                }, new Color(70, 50, 100), "Ruido_10", 3, 120f, 70f, 3.5f, 17f, 8f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.28f, 6f, 0.36f, 1.2f, 0.5f, 0.085f, 0.16f, 0.018f, 0.65f, 0.7f, 0.55f, 0.3f, 0.2f, 0.62f, 0.08f, 0.85f, 1f, 1f, 0.6f, 0),
                Ala("Solares Rúnicas", (Color[])(object)new Color[3]
                {
                    new Color(255, 246, 220),
                    new Color(232, 166, 61),
                    new Color(255, 232, 154)
                }, new Color(255, 252, 240), "Ruido_1", 6, 90f, 48f, 3.5f, 14f, 8f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.02f, 5f, 0.28f, 1f, 0.45f, 0.085f, 0.16f, 0.014f, 1f, 0.7f, 1f, 0.3f, 0.34f, 0.62f, 0.15f, 1f, 1f, 1f, 1.25f, 0),
                Ala("Sangre de Tinta", (Color[])(object)new Color[3]
                {
                    new Color(209, 47, 166),
                    new Color(122, 26, 77),
                    new Color(20, 6, 20)
                }, new Color(224, 130, 190), "Ruido_17", 5, 86f, 46f, 3.5f, 10f, 5f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.34f, 5f, 0.26f, 2.2f, 0.9f, 0.085f, 0.16f, 0.018f, 1f, 1.2f, 0.9f, 0.3f, 0.38f, 0.62f, 0.14f, 1.2f, 1f, 1f, 1f, 0),
                Ala("Runas Solares", (Color[])(object)new Color[3]
                {
                    DefinitivasLib.Oro,
                    new Color(255, 214, 130),
                    DefinitivasLib.BlancoCalido
                }, DefinitivasLib.BlancoCalido, "Ruido_13", 5, 92f, 50f, 3.5f, 14f, 8f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.34f, 5f, 0.3f, 1f, 0.35f, 0.05f, 0.1f, 0.018f, 0.85f, 1f, 0.95f, 0.3f, 0.34f, 0.62f, 0.16f, 1f, 1f, 1f, 1.2f, 1, 1f),
                Ala("Runas del Vacío", (Color[])(object)new Color[3]
                {
                    new Color(150, 92, 255),
                    new Color(96, 52, 190),
                    new Color(96, 200, 255)
                }, new Color(150, 170, 230), "Ruido_6", 5, 88f, 44f, 3.5f, 10f, 8f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, -0.15f, 5f, 0.22f, 1.6f, 0.75f, 0.085f, 0.16f, 0.018f, 0.7f, 0.8f, 0.85f, 0.3f, 0.32f, 0.62f, 0.14f, 1f, 1f, 1f, 0.9f, 2, 1.05f),
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
                Ala("Agujas del Vacío", (Color[])(object)new Color[3]
                {
                    new Color(130, 74, 220),
                    new Color(84, 40, 150),
                    new Color(110, 216, 255)
                }, new Color(140, 120, 200), "Ruido_19", 4, 96f, 40f, 3.5f, 9f, 5f, 0.5f, 0.92f, 0.22f, 1.54f, 0.2f, 0.14f, 0.42f, 5f, 0.24f, 2.4f, 0.55f, 0.085f, 0.16f, 0.018f, 1.15f, 1.2f, 1f, 0.3f, 0.3f, 0.62f, 0.13f, 1f, 1f, 1f, 1f, 0),
                Ala("Cuchillas del Eclipse", (Color[])(object)new Color[3]
                {
                    new Color(255, 196, 110),
                    DefinitivasLib.Ambar,
                    new Color(70, 26, 8)
                }, new Color(230, 180, 120), "Ruido_3", 5, 90f, 48f, 3.5f, 22f, 13f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0.1f, 5f, 0.18f, 1.35f, 1f, 0.085f, 0.16f, 0.018f, 1f, 1f, 0.92f, 0.3f, 0.38f, 0.62f, 0.15f, 1.1f, 1f, 1f, 1f, 0),
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
