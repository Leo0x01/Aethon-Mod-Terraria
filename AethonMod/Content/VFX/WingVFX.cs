using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// WingVFX — v6.08 — EL NÚCLEO DEL SISTEMA DE ALAS DE LUZ.
    ///
    /// v6.06 tuvo 2 alas "de técnica coronas" y 8 de spritesheet; el usuario
    /// pidió que DESDE AHORA TODAS sean de luz procedural y con animaciones
    /// mejores. Este archivo define las tres piezas del sistema unificado:
    ///
    ///   1. <see cref="WingDrawContext"/>: TODO lo que un renderizador de
    ///      ala necesita saber del jugador en un solo struct (anclaje,
    ///      apertura, fase/amuente de aleteo, dirección, gravedad, luz,
    ///      VELOCIDAD para el sweep aerodinámico).
    ///   2. <see cref="WingMotionProfile"/>: la PERSONALIDAD de vuelo de cada
    ///      estilo (la mariposa aletea LENTO y profundo con golpe asimétrico,
    ///      el hada vibra RÁPIDO y superficial, el cometa ondea...) — la
    ///      animación ya no es una sola onda sine para todos.
    ///   3. <see cref="WingStyles"/>: el registro estilo→renderizador que la
    ///      capa de dibujado y el WingAnimPlayer consultan por índice.
    ///
    /// Los renderizadores viven en archivos propios (BlackHoleWings.cs,
    /// ButterflyWings.cs, FairyWings.cs, SolarCoronaWings.cs, NebulaWings.cs,
    /// EclipseWings.cs, CometWings.cs) y pintan cuadros en el buffer de
    /// VFXCore — el mismo camino de las coronas.
    /// </summary>

    /// <summary>Todo lo que un renderizador de alas necesita del jugador.</summary>
    public struct WingDrawContext
    {
        /// <summary>Anclaje en la espalda ALTA (mundo): nace del hombro.</summary>
        public Vector2 Back;

        /// <summary>Apertura 0..1+ (0 plegada al cuerpo, 1 extendida).</summary>
        public float Open;

        /// <summary>Fase del ciclo de aleteo (rad, avanza al volar).</summary>
        public float FlapPhase;

        /// <summary>Amplitud del aleteo 0..1 (0 reposo, 1 volando).</summary>
        public float FlapAmp;

        /// <summary>Main.GlobalTimeWrappedHourly.</summary>
        public float Time;

        /// <summary>Dirección del jugador (-1/1).</summary>
        public int Direction;

        /// <summary>Gravedad (1/-1): invierte el plano del ala.</summary>
        public float GravDir;

        /// <summary>Luz del mundo 0..1 (mult. global de brillo).</summary>
        public float Alpha;

        /// <summary>Velocidad HORIZONTAL del jugador (px/tick, con signo):
        /// las alas se BARREN hacia atrás al correr/volar rápido.</summary>
        public float SpeedX;

        /// <summary>Velocidad VERTICAL normalizada (-1 subiendo .. 1 cayendo):
        /// controla el diedro (alas más planas al subir, más recogidas al caer).</summary>
        public float Rise;
    }

    /// <summary>
    /// La personalidad de vuelo de un estilo de ala: frecuencias, aperturas
    /// objetivo por estado y el GOLPE de aleteo (asimétrico o no).
    /// </summary>
    public struct WingMotionProfile
    {
        /// <summary>Avance de fase por tick VOLANDO (rad): mariposa 0.16
        /// (lento y majestuoso), hada 0.55 (vibración de colibrí).</summary>
        public float FlapSpeedFlying;

        /// <summary>Avance de fase por tick PLANEANDO (rad).</summary>
        public float FlapSpeedGliding;

        /// <summary>Amplitud DE FORMA del aleteo (1 = golpe completo).</summary>
        public float FlapShape;

        /// <summary>Asimetría del golpe: 1 = sine simétrico; &gt;1 = bajada
        /// más rápida que la subida (vuelo real de mariposa: el golpe hacia
        /// abajo da el empuje y la recuperación es lenta).</summary>
        public float StrokeAsymmetry;

        /// <summary>Apertura objetivo en REPOSO (plegado contra la espalda).</summary>
        public float FoldOpen;

        /// <summary>Apertura objetivo al CAER (alas extendidas sin aletear).</summary>
        public float FallOpen;

        /// <summary>Apertura objetivo al PLANEAR.</summary>
        public float GlideOpen;

        /// <summary>Apertura objetivo al VOLAR.</summary>
        public float FlyOpen;

        /// <summary>Rigidez del muelle de apertura (mayor = respuesta más viva).</summary>
        public float SpringStiffness;

        /// <summary>Amortiguación del muelle (menor = más overshoot).</summary>
        public float SpringDamping;

        /// <summary>¿El aleteo NUNCA para del todo en reposo? (alas de hada
        /// que vibran suavemente incluso paradas).</summary>
        public bool AlwaysFlutter;

        /// <summary>Dust de vuelo (id vanilla).</summary>
        public int DustId;

        /// <summary>Color del dust de vuelo.</summary>
        public Color DustColor;

        /// <summary>¿Sonar de aleteo en CADA ciclo? (el hada suena cada 2).</summary>
        public bool SoundEveryCycle;
    }

    /// <summary>Un renderizador de alas: vuelca cuadros al buffer de VFXCore.</summary>
    public delegate void WingRenderer(ref WingDrawContext ctx);

    /// <summary>
    /// El puente slot-de-equipo → estilo de ala: los ítems de alas de luz
    /// registran su textura de equipo (PNG en blanco) y ESTA tabla dice a
    /// qué estilo de render pertenece cada slot. La comparten el animador
    /// (WingAnimPlayer) y la capa de dibujado (VFXWingsDrawLayer).
    /// </summary>
    public static class VFXWingSlots
    {
        /// <summary>Los NOMBRES DE ÍTEM en orden de estilo (índice = estilo).</summary>
        public static readonly string[] ItemNames =
        {
            "EventHorizonWings",
            "PhotonRingWings",
            "CosmicButterflyWings",
            "StardustFairyWings",
            "SolarCoronaWings",
            "LivingNebulaWings",
            "TotalEclipseWings",
            "CrimsonCometWings",
        };

        /// <summary>Cache slot → estilo (-1 = no es de este sistema).</summary>
        private static int[] _slotToStyle = new int[WingStyles.Count];

        /// <summary>Estilo de ala para un slot de equipo (-1 si no es nuestro).</summary>
        public static int StyleFromSlot(Mod mod, int slot)
        {
            if (slot < 0) return -1;

            // Resolver la caché la primera vez.
            if (_slotToStyle[0] == 0 && _slotToStyle[WingStyles.Count - 1] == 0)
            {
                for (int s = 0; s < WingStyles.Count; s++)
                {
                    int eq = EquipLoader.GetEquipSlot(mod, ItemNames[s], EquipType.Wings);
                    _slotToStyle[s] = eq;
                }
            }

            for (int s = 0; s < WingStyles.Count; s++)
                if (_slotToStyle[s] == slot) return s;
            return -1;
        }
    }

    /// <summary>El registro de estilos de alas de luz (v6.08: los 8).</summary>
    public static class WingStyles
    {
        /// <summary>Cuantos estilos hay (los índices son ESTABLES: se usan
        /// desde los ítems, el animador y la capa de dibujado).</summary>
        public const int Count = 8;

        /// <summary>0 — Horizonte de Sucesos.</summary>
        public const int EventHorizon = 0;

        /// <summary>1 — Anillo de Fotones.</summary>
        public const int PhotonRing = 1;

        /// <summary>2 — Mariposa Cósmica.</summary>
        public const int Butterfly = 2;

        /// <summary>3 — Hada de Polvo Estelar.</summary>
        public const int Fairy = 3;

        /// <summary>4 — Corona Solar.</summary>
        public const int SolarCorona = 4;

        /// <summary>5 — Nebulosa Viva.</summary>
        public const int Nebula = 5;

        /// <summary>6 — Eclipse Total.</summary>
        public const int Eclipse = 6;

        /// <summary>7 — Cometa Carmesí.</summary>
        public const int Comet = 7;

        private static WingRenderer[] _renderers;
        private static WingMotionProfile[] _motions;

        /// <summary>Acceso al renderizador de un estilo (carga perezosa).</summary>
        public static WingRenderer Renderer(int style)
        {
            EnsureLoaded();
            return _renderers[style];
        }

        /// <summary>Acceso a la personalidad de vuelo de un estilo.</summary>
        public static WingMotionProfile Motion(int style)
        {
            EnsureLoaded();
            return _motions[style];
        }

        private static void EnsureLoaded()
        {
            if (_renderers != null) return;

            _renderers = new WingRenderer[Count];
            _motions = new WingMotionProfile[Count];

            // ----------------------------------------------------------------
            //  0 — HORIZONTE DE SUCESOS: rastros de acreción barriendo el aire
            // ----------------------------------------------------------------
            _renderers[EventHorizon] = BlackHoleWings.RenderEventHorizon;
            _motions[EventHorizon] = new WingMotionProfile
            {
                FlapSpeedFlying = 0.24f,
                FlapSpeedGliding = 0.05f,
                FlapShape = 1.0f,
                StrokeAsymmetry = 1.0f,
                FoldOpen = 0.18f,
                FallOpen = 0.95f,
                GlideOpen = 0.85f,
                FlyOpen = 1.0f,
                SpringStiffness = 0.020f,
                SpringDamping = 0.88f,
                AlwaysFlutter = false,
                DustId = Terraria.ID.DustID.PinkTorch,
                DustColor = new Color(255, 60, 190),
                SoundEveryCycle = true,
            };

            // ----------------------------------------------------------------
            //  1 — ANILLO DE FOTONES: órbitas de luz acelerando al volar
            // ----------------------------------------------------------------
            _renderers[PhotonRing] = BlackHoleWings.RenderPhotonRing;
            _motions[PhotonRing] = new WingMotionProfile
            {
                FlapSpeedFlying = 0.20f,
                FlapSpeedGliding = 0.04f,
                FlapShape = 0.8f,
                StrokeAsymmetry = 1.0f,
                FoldOpen = 0.30f,
                FallOpen = 0.95f,
                GlideOpen = 0.85f,
                FlyOpen = 1.0f,
                SpringStiffness = 0.016f,
                SpringDamping = 0.86f,
                AlwaysFlutter = false,
                DustId = Terraria.ID.DustID.PinkCrystalShard,
                DustColor = new Color(255, 170, 215),
                SoundEveryCycle = true,
            };

            // ----------------------------------------------------------------
            //  2 — MARIPOSA CÓSMICA: golpe LENTO y PROFUNDO, asimétrico
            //     (bajada rápida que da empuje, recuperación lenta — como
            //     vuela una mariposa de verdad)
            // ----------------------------------------------------------------
            _renderers[Butterfly] = ButterflyWings.Render;
            _motions[Butterfly] = new WingMotionProfile
            {
                FlapSpeedFlying = 0.155f,
                FlapSpeedGliding = 0.035f,
                FlapShape = 1.15f,
                StrokeAsymmetry = 1.7f,
                FoldOpen = 0.30f,
                FallOpen = 1.0f,
                GlideOpen = 0.9f,
                FlyOpen = 1.0f,
                SpringStiffness = 0.014f,
                SpringDamping = 0.90f,
                AlwaysFlutter = false,
                DustId = Terraria.ID.DustID.RainbowRod,
                DustColor = new Color(255, 120, 200),
                SoundEveryCycle = true,
            };

            // ----------------------------------------------------------------
            //  3 — HADA DE POLVO ESTELAR: vibración RÁPIDA y superficial
            //     (colibrí) que nunca cesa del todo
            // ----------------------------------------------------------------
            _renderers[Fairy] = FairyWings.Render;
            _motions[Fairy] = new WingMotionProfile
            {
                FlapSpeedFlying = 0.52f,
                FlapSpeedGliding = 0.10f,
                FlapShape = 0.45f,
                StrokeAsymmetry = 1.0f,
                FoldOpen = 0.42f,
                FallOpen = 0.9f,
                GlideOpen = 0.8f,
                FlyOpen = 1.0f,
                SpringStiffness = 0.026f,
                SpringDamping = 0.84f,
                AlwaysFlutter = true,
                DustId = Terraria.ID.DustID.GoldFlame,
                DustColor = new Color(255, 226, 160),
                SoundEveryCycle = false,
            };

            // ----------------------------------------------------------------
            //  4 — CORONA SOLAR: prominencias que se estiran al empujar
            // ----------------------------------------------------------------
            _renderers[SolarCorona] = SolarCoronaWings.Render;
            _motions[SolarCorona] = new WingMotionProfile
            {
                FlapSpeedFlying = 0.26f,
                FlapSpeedGliding = 0.05f,
                FlapShape = 1.05f,
                StrokeAsymmetry = 1.25f,
                FoldOpen = 0.20f,
                FallOpen = 0.95f,
                GlideOpen = 0.85f,
                FlyOpen = 1.0f,
                SpringStiffness = 0.018f,
                SpringDamping = 0.87f,
                AlwaysFlutter = false,
                DustId = Terraria.ID.DustID.SolarFlare,
                DustColor = new Color(255, 160, 40),
                SoundEveryCycle = true,
            };

            // ----------------------------------------------------------------
            //  5 — NEBULOSA VIVA: nubes que respiran, deriva orgánica
            // ----------------------------------------------------------------
            _renderers[Nebula] = NebulaWings.Render;
            _motions[Nebula] = new WingMotionProfile
            {
                FlapSpeedFlying = 0.19f,
                FlapSpeedGliding = 0.04f,
                FlapShape = 0.85f,
                StrokeAsymmetry = 1.0f,
                FoldOpen = 0.28f,
                FallOpen = 0.95f,
                GlideOpen = 0.9f,
                FlyOpen = 1.0f,
                SpringStiffness = 0.012f,
                SpringDamping = 0.90f,
                AlwaysFlutter = true,
                DustId = Terraria.ID.DustID.PurpleTorch,
                DustColor = new Color(230, 120, 255),
                SoundEveryCycle = true,
            };

            // ----------------------------------------------------------------
            //  6 — ECLIPSE TOTAL: majestad lenta, corona ondeando al viento
            // ----------------------------------------------------------------
            _renderers[Eclipse] = EclipseWings.Render;
            _motions[Eclipse] = new WingMotionProfile
            {
                FlapSpeedFlying = 0.17f,
                FlapSpeedGliding = 0.04f,
                FlapShape = 0.9f,
                StrokeAsymmetry = 1.0f,
                FoldOpen = 0.22f,
                FallOpen = 0.95f,
                GlideOpen = 0.88f,
                FlyOpen = 1.0f,
                SpringStiffness = 0.013f,
                SpringDamping = 0.89f,
                AlwaysFlutter = false,
                DustId = Terraria.ID.DustID.Shadowflame,
                DustColor = new Color(200, 210, 255),
                SoundEveryCycle = true,
            };

            // ----------------------------------------------------------------
            //  7 — COMETA CARMESÍ: la cola ondea más rápido cuanta más prisa
            // ----------------------------------------------------------------
            _renderers[Comet] = CometWings.Render;
            _motions[Comet] = new WingMotionProfile
            {
                FlapSpeedFlying = 0.30f,
                FlapSpeedGliding = 0.06f,
                FlapShape = 0.7f,
                StrokeAsymmetry = 1.0f,
                FoldOpen = 0.25f,
                FallOpen = 0.9f,
                GlideOpen = 0.8f,
                FlyOpen = 1.0f,
                SpringStiffness = 0.020f,
                SpringDamping = 0.85f,
                AlwaysFlutter = false,
                DustId = Terraria.ID.DustID.Torch,
                DustColor = new Color(255, 120, 60),
                SoundEveryCycle = true,
            };
        }
    }
}
