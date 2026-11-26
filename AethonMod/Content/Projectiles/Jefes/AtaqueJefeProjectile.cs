using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.VFX;
using AethonMod.Content.Projectiles.Cosmic;
using AethonMod.Content.NPCs; // v6.50.59 — el reloj permanente pregunta la FASE del jefe

namespace AethonMod.Content.Projectiles.Jefes
{
    /// <summary>
    /// AtaqueJefeProjectile — v6.48 — LOS DIENTES DE LOS CINCO.
    ///
    /// v6.50.46 — LA LEY DE ORO (la que despertó tres ataques muertos):
    /// EL ESTILO VIVE EN ai[0] Y NADIE LO TOCA. El coro, el telar y el
    /// decreto de la v6.50.45 NUNCA funcionaron: cada uno sobrescribía
    /// ai[0] con sus propios datos (ancla, centro, radio) en sus
    /// primeros ticks — el estilo moría, la IA y el render dejaban de
    /// matchear y el ataque quedaba INVISIBLE E INERTE. El ancla ES el
    /// cuerpo del proyectil; el radio se COMPUTA de la edad. Y la ronda:
    /// el reloj ×5.2 en anillos de cuatro que caen del cielo, el telar
    /// clava sus estrellas DONDE PASA EL JEFE (la figura de estrella que
    /// él mismo traza) y las runas disparan abanicos de pernos (LOS
    /// TAJOS DEL JEFE MURIERON — «el ataque de tajos que hace el jefe es
    /// feo, quitalo»).
    ///
    /// EL ARSENAL COMPLETO de los jefes del MOD (Aethon · el Titán Hueco
    /// · el Guardián del Rift · la Arquera · el Primer Portador): cada
    /// jefe v6.48 pelea con LAS LIBRERÍAS DE LA CASA — nada de
    /// proyectiles vanilla de prestado (los CultistBossLightningOrbArc
    /// recoloreados y las "minas" que eran ProjectileID.Bullet estáticos
    /// murieron aquí). Un diente por estilo, TODOS telegrafiados o
    /// esquivables, TODOS con la física del motor:
    ///
    ///   0  PÚA DEL SAGRARIO (Titán): espina de cristal en arco que SE
    ///      CLAVA, respira y DETONA en esquirlas (OndaLib.Pulse).
    ///   1  CORO DE CRISTAL (Titán, furia): esquirla en órbita alrededor
    ///      del coloso que SE LANZA a la presa cuando el coro canta.
    ///   2  VIROTE DE VACÍO (Rift): lanza entre-mundos que PARPADEA entre
    ///      fases (medio dentro del desgarro, medio fuera).
    ///   3  FLECHA ESTELAR (Arquera): flecha con corrección de rumbo y
    ///      ESTELA DE FANTASMAS (EspectroLib — la cola historia).
    ///   4  MINA ESTELAR (Arquera): LA MINA DE VERDAD — se ARMA con
    ///      pulso de aviso (OndaLib) y DETONA en un ARCO VOLTAICO a la
    ///      presa (StormLib.ChainBolt + ImpactFlash) — ya no una bala
    ///      quieta.
    ///   5  ESTRELLA FUGAZ (Arquera, furia): estrella que cae con MARCA
    ///      de suelo telegrafiada antes del impacto (OndaLib.GroundVisual;
    ///      v6.80 — antes decía OndaLib.Ground, purgado por muerto).
    ///   6  TAJO DEL PORTADOR (Portador): el corte DIFERIDO de la casa —
    ///      marca que respira (Telegrafo) y FLORECE en arco TajoLib.
    ///   7  CUCHILLA EN ÓRBITA (Portador): cuchilla girando en anillo
    ///      alrededor del duelista que se DISPARA al cierre.
    ///   8  CORTE DE REALIDAD (Portador, furia): la PARED DE DESGARRO
    ///      que AVanza lenta (RiftLib.Tear con CaminoDesgarro) — el
    ///      campo que parte el suelo en dos.
    ///   9  PERNO ESTELAR (Aethon): el perno de polvo estelar con halo
    ///      y latido (Bloom pulsante).
    ///   10 NUBE DE NEBULOSA (Aethon): zona que QUEMA por contacto —
    ///      flores de humo violeta girando (quads SoftGlow + anillos).
    ///   11 RUNA MEMORIZADA (Aethon, fase 5): LA RUNA QUE TE RECUERDA —
    ///      el sigilo dorado orbita a Aethon y DISPARA TUS PROPIOS
    ///      movimientos (tajos y pernos de la casa, aprendidos de ti).
    ///   12 ESTALLIDO DE MINA (Arquera): el cuerpo del estallido (20 t
    ///      de radio honesto — daño por colisión del motor).
    ///
    /// REGLAS DE LA CASA: daño por COLISIÓN del motor (cero daño
    /// manual), cero Main.rand en el render (Hash01 y semillas por
    /// ai[2]), SIN hide (la lección v6.35), el contrato de lote
    /// cerrado→cerrado (VFXCore búfer → lote aditivo de pantalla →
    /// lote del pase reabierto TAL CUAL).
    /// </summary>
    public partial class AtaqueJefeProjectile : ModProjectile
    {
        // === LOS ESTILOS (ai[0]) ===
        public const int EstiloPuaSagrario = 0;
        public const int EstiloCoroCristal = 1;
        public const int EstiloViroteVacio = 2;
        public const int EstiloFlechaEstelar = 3;
        public const int EstiloMinaEstelar = 4;
        public const int EstiloEstrellaFugaz = 5;
        public const int EstiloTajoPortador = 6;
        public const int EstiloCuchillaOrbit = 7;
        public const int EstiloCorteRealidad = 8;
        public const int EstiloPernoEstelar = 9;
        public const int EstiloNubeNebulosa = 10;
        public const int EstiloRunaMemorizada = 11;
        public const int EstiloEstallidoMina = 12;
        public const int EstiloColumnaJuicio = 13;   // v6.50.36 — Aethon, LA LUZ
        public const int EstiloPilarAparicion = 14;  // v6.50.44 — EL PILAR DEL CIELO (cosmético: la entrada de la emperatriz)
        public const int EstiloRelojGigante = 15;    // v6.50.45 — EL RELOJ DE ARENA GIGANTE del bastón, ×2.6
        public const int EstiloCoroJefe = 16;        // v6.50.45 — EL CORO ESPECTRAL alrededor de la presa
        public const int EstiloTelarJefe = 17;       // v6.50.45 — EL TELAR DE CONSTELACIONES (la trampa)
        public const int EstiloDecretoJefe = 18;     // v6.50.45 — EL DECRETO DEL ECLIPSE (cambio de fase)
        public const int EstiloEstallidoRadiante = 19; // v6.50.53 — EL ESTALLIDO RADIANTE (la imagen del usuario: el punto de luz que ARDE y lo arrasa TODO — núcleo blanco-oro, anillo SEGMENTADO de emisores, 44 rayos en 360° de largo variable, la cruz anamórfica, las chispas que vuelan y el destello que inunda la pantalla)
        public const int EstiloDanzaSolar = 20;     // v6.50.54 — LA DANZA SOLAR (Sun Dance — la rueda de SEIS rayos girando que cabalga con el jefe)
        public const int EstiloLanzaEterna = 21;    // v6.50.54 — LA LANZA ETERNA (Ethereal Lance — telegrafiada translúcida y hostil solo al volar)
        public const int EstiloCoronaEterna = 22;   // v6.50.54 — LA CORONA ETERNA (Everlasting Rainbow — 14 plumas prismáticas espiralando)
        // (v6.50.81 — el estilo 23, LA BOLA FINAL, RETIRADO: la letra
        //  «el sol explota, pero se sigue disparando una bola de luz hacia
        //  algun lado, esa bola de luz dura varios segundos antes de
        //  desaparecer» — la muerte del dios es EL ESTALLIDO y desaparecer,
        //  NADA MÁS; el número queda retirado como los de camiseta)
        public const int EstiloSolJefe = 24;       // v6.50.57 — EL SOL DEL DIOS (el ataque especial: el jefe SE CONVIERTE en sol — la asunción cabalgándolo — y luego LO LANZA a la presa: la persigue LENTO, GRAVITA, CRECE hasta la GIGANTE ROJA (×6) y estalla en LUZ, BRUMA y FORMAS que hacen daño)
        public const int EstiloMuerteFinal = 25;  // v6.50.80 — LA MUERTE DEL DIOS (la letra: «debe hacer una gran explosion de luz y desaparecer, asi es como debe ser su muerte»): el Estallido Radiante en SU versión final — la MISMA luz que el dios usaba como ataque, pero a ESCALA 1.85 (la más grande de toda la pelea) y PURA LUZ: jamás daña (la victoria no se cobra con un golpe de gracia barato a quemarropa)
        public const int EstiloAparicionLuz = 26; // v6.122 — LA EXPLOSIÓN DE LUZ DE LA APARICIÓN (la letra: «el siempre debe aparecer con una explocion de luz»): el Estallido Radiante en su versión de NACIMIENTO — ESCALA 1.45 (más grande que cualquier ataque de combate, más contenida que la muerte) y PURA LUZ: jamás daña (la bienvenida no se cobra con un golpe barato). Se dispara en EstadoNaciendo al completarse la materialización — SIEMPRE, en cada aparición.

        /// <summary>El estilo del diente (ai[0]).</summary>
        private int Estilo => (int)Projectile.ai[0];
        /// <summary>Parámetro libre (ángulo base / fase / modo).</summary>
        private float Par => Projectile.ai[1];
        /// <summary>Semilla determinista (ai[2]).</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[2] % 9973);

        // === v6.50.54 — EL ESTALLIDO ESCALADO: la FASE viaja en ai[2]
        //     empaquetada por el jefe (fase·8192 + seed — Seed sigue siendo
        //     ai[2]%9973 y la fase se lee ai[2]/8192; 0 si no vino) —
        //     «un poco más grande» con la ira del dios ===
        private int FaseEstallido => (int)(Projectile.ai[2] / 8192f);
        // v6.50.80 — LA ESCALA DE LA MUERTE: la última luz es LA MÁS GRANDE
        //     de la pelea — 1.85 plano (el estallido de combate llega a 1.27
        //     en fase 3: la muerte es un 45 % más grande que su máximo ataque).
        //     v6.122 — LA ESCALA DEL NACIMIENTO: 1.45 — la bienvenida del dios
        //     ARDE más que cualquier ataque de combate, sin robarle el trono
        //     a su última luz.
        private float EscalaEstallido => Estilo == EstiloMuerteFinal
            ? 1.85f
            : Estilo == EstiloAparicionLuz
                ? 1.45f
                : 1.15f + 0.06f * Math.Max(0, FaseEstallido - 1);

        // === EL ESTADO (por estilo) ===
        private float _edad;
        private Vector2 _centroOrbita;   // el anillo del coro / la runa
        private bool _lanzado;           // esquirla/cuchilla ya disparada
        private bool _clavada;           // la púa ya se hundió en el suelo
        private Vector2 _posAnterior;    // la cola del virote
        private int _ticksDisolver;      // v6.50.44 — el fade del pilar de la aparición

        // === v6.50.60 — EL SOL DEL DIOS, LA FUSIÓN DE PROXIMIDAD (la
        //     letra: «el ataque del sol que debe lanzar y perseguirme no
        //     lo hace realmente» — el sol ya NO explota en el vacío a los
        //     380 t lejísimos de la presa: cuando la CAZA lo pone a menos
        //     de 240 px, LA GIGANTE EMPIEZA YA — la estrella se hincha
        //     CERCA y DETONA CERCA; la fusión por tiempo queda como red
        //     de seguridad) ===
        private bool _solGigante;        // la gigante empezó (espolote o reloj)
        private int _tickGigante;        // el compás de la gigante (60 t de hinchazón)
        private bool _solExploto;        // la detonación ya sonó

        // === v6.50.58 — LOS SHADERS DEL DISCO DEL SOL DEL DIOS (los del
        //     arma SunProjectile — el cacheo de la casa: Request UNA vez,
        //     flag si el asset no está y jamás volver a tocarlo) ===
        private static Ref<Effect> _solDiscoShader;
        private static bool _solDiscoFallo;
        private static Ref<Effect> _solAuraShader;
        private static bool _solAuraFallo;

        // === v6.50.45 — EL CORO ESPECTRAL DEL JEFE (sub-entidades lógicas,
        //     el patrón de la casa: NADA de proyectiles extra) ===
        private sealed class AnilloCoro
        {
            public Vector2 Origen;
            public float Edad;
            public int Nota;
        }
        private readonly Vector2[] _coroNotas = new Vector2[6];
        private readonly float[] _coroFase = new float[6];
        private readonly float[] _coroRadio = new float[6];
        private readonly float[] _coroVel = new float[6];
        private readonly float[] _coroAlto = new float[6];
        private readonly System.Collections.Generic.List<AnilloCoro> _coroAnillos = new(10);
        private bool _coroSembrada;
        private int _coroNotaActual;

        // === v6.50.45 — EL TELAR DEL JEFE (las estrellas clavadas) ===
        private readonly Vector2[] _estrellas = new Vector2[7];
        private readonly bool[] _estrellaClavada = new bool[7];
        private float _telarFlash;

        /// <summary>
        /// v6.50.46 — LA PUNTA i DE LA FIGURA DEL TELAR: la MISMA fórmula
        /// que usa el jefe para trazar la estrella (el contrato para que
        /// ambos dibujen la MISMA figura: el jefe vuela a estas puntas, el
        /// telar clava las estrellas en ellas).
        /// </summary>
        internal static Vector2 PuntaTelar(Vector2 centro, int i, int n, float semilla)
        {
            float ang = semilla * 0.013f + i * MathHelper.TwoPi / n;
            return centro + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.82f) * 430f;
        }

        /// <summary>
        /// v6.50.46 — LAS ESTRELLAS CLAVADAS EN ORDEN DE ÍNDICE (el orden
        /// del polígono — el jefe las clava por el SALTO de la estrella,
        /// pero la JAULA conecta las puntas en su orden natural).
        /// </summary>
        private int EstrellasClavadas(Vector2[] destino)
        {
            int puntas = Par > 0.5f ? 7 : 5;
            int n = 0;
            for (int i = 0; i < puntas; i++)
                if (_estrellaClavada[i]) destino[n++] = _estrellas[i];
            return n;
        }

        // === LAS PALETAS DE LOS CINCO ===
        private static readonly Color TealCristal = new(168, 232, 255);
        private static readonly Color TealVacio = new(96, 224, 220);
        private static readonly Color VioletaVacio = new(140, 60, 220);
        private static readonly Color AmbarEstelar = new(255, 178, 96);
        private static readonly Color EmberPortador = new(255, 120, 60);
        private static readonly Color LuzPrimordial = new(196, 150, 255);
        private static readonly Color OroGrimorio = new(245, 196, 81);
        private static readonly Color BlancoCaliente = new(255, 240, 190);
        private static readonly Color NebulosaNube = new(55, 42, 71); // violeta oscuro premezclado (XNA Color no define +)
        private static readonly Color ColorOroEclipse = new(255, 214, 110);  // v6.50.45 — el oro del decreto
        private static readonly Color ColorVioletaEclipse = new(150, 80, 255); // v6.50.45 — el fuego oscuro del decreto

        /// <summary>v6.50.54 — EL COLOR DEL PRISMA (el mismo espectro de la
        /// Forma 3 — la banda de siete franjas: h ∈ [0,1) recorre rojo →
        /// violeta) para LAS PLUMAS DE LA CORONA.</summary>
        private static Color ColorPrismaLocal(float h)
        {
            h -= MathF.Floor(h);
            float paso = h * 7f;
            int i = (int)paso % 7;
            float f = paso - MathF.Floor(paso);
            Color[] franjas =
            {
                new Color(255, 60, 60), new Color(255, 150, 50),
                new Color(255, 235, 60), new Color(90, 255, 110),
                new Color(80, 190, 255), new Color(120, 100, 255),
                new Color(210, 90, 255),
            };
            return Color.Lerp(franjas[i], franjas[(i + 1) % 7], f);
        }

        // ================================================================
        //  v6.104 — LAS LEYES DE HAZ DE LA FASE 1.5 (la muerte de las
        //  texturas planas): el estado estático que leen los delegados
        //  (cero-GC — el patrón de toda la casa) y LAS CUATRO LEYES: el
        //  tinte del velo, el tinte del núcleo, el vértice del núcleo
        //  (el grumo de ruido + el brillo 1/d) y el vértice del rayo
        //  del estallido (el núcleo blanco y el halo CALIENTE en UNA
        //  pasada — el degradé blanco→oro→ámbar→brasa de la imagen
        //  vive en el VÉRTICE, no en quads apilados).
        // ================================================================
        private static float _hazAlfa = 1f;
        private static Color _hazTinte = Color.White;
        private static float _hazAncho = 8f;

        private static readonly HazLib.LeyNodo LeyTinteHazVelo = TinteHazVelo;
        private static readonly HazLib.LeyNodo LeyTinteHazNucleo = TinteHazNucleo;
        private static readonly HazLib.LeyVertice LeyVerticeHazNucleo = VerticeHazNucleo;
        private static readonly HazLib.LeyVertice LeyVerticeRayoEstallido = VerticeRayoEstallido;

        private static Color TinteHazVelo(float p)
            => _hazTinte * (MathHelper.Clamp(p / 0.10f, 0f, 1f)
                * MathHelper.Clamp((1f - p) / 0.12f, 0f, 1f) * (_hazAlfa * 0.40f));

        private static Color TinteHazNucleo(float p)
            => _hazTinte * (MathHelper.Clamp(p / 0.06f, 0f, 1f)
                * MathHelper.Clamp((1f - p) / 0.10f, 0f, 1f) * (_hazAlfa * 0.95f));

        /// <summary>EL VÉRTICE del núcleo: el grumo de ruido diagonal (la
        /// luz que HIERVE) y el brillo interior 1/d — el mismo idioma de
        /// los haces de las armas.</summary>
        private static Color VerticeHazNucleo(float x, float y, Color tinte)
        {
            float tt = Main.GlobalTimeWrappedHourly;
            float ruido = FracHaz(x * 2f - tt * 2.6f) * FracHaz(y * 2f + x * 4f - tt * 2.6f);
            float opacidad = ruido * 0.65f + 0.40f;
            float d = MathF.Abs(y - 0.5f);
            float brillo = d <= 0.0001f ? 1f : MathHelper.Clamp(0.30f / d, 0f, 1f);
            brillo *= 1f - MathHelper.Clamp((d - 0.10f) / 0.22f, 0f, 1f);
            Vector4 suma = tinte.ToVector4() * opacidad + new Vector4(brillo * 0.8f);
            return new Color(
                (byte)Math.Clamp((int)(suma.X * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(suma.Y * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(suma.Z * 255f + 0.5f), 0, 255), 255);
        }

        /// <summary>EL VÉRTICE del rayo del estallido: el CENTRO de la
        /// banda es el núcleo BLANCO y el borde es el HALO CALIENTE (oro →
        /// ámbar → brasa — el degradé de la imagen) — núcleo y halo en UNA
        /// pasada, con SU ruido vivo.</summary>
        private static Color VerticeRayoEstallido(float x, float y, Color tinte)
        {
            float tt = Main.GlobalTimeWrappedHourly;
            float ruido = FracHaz(x * 2.4f - tt * 3f) * 0.5f;
            float d = MathF.Abs(y - 0.5f) * 2f;      // 0 = centro, 1 = borde
            Color caliente = Color.Lerp(OroGrimorio, AmbarEstelar, 0.45f);
            Color c = Color.Lerp(BlancoCaliente, caliente, MathF.Pow(d, 0.65f));
            Vector4 v = c.ToVector4() * (0.95f - 0.45f * d + ruido * 0.30f);
            return new Color(
                (byte)Math.Clamp((int)(v.X * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(v.Y * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(v.Z * 255f + 0.5f), 0, 255), 255);
        }

        private static float FracHaz(float x) => x - MathF.Floor(x);

        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 420;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.5f;
            // v6.50.1 — los que entren a media pelea reciben las paredes/minas
            // (vanilla lo hace con los proyectiles de jefe).
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            _posAnterior = Projectile.Center;
            _centroOrbita = Projectile.Center;

            // La púa es la única que RESPETA el terreno (se clava).
            Projectile.tileCollide = Estilo == EstiloPuaSagrario ||
                                     Estilo == EstiloEstrellaFugaz;

            // Vidas propias por estilo.
            switch (Estilo)
            {
                case EstiloPuaSagrario: Projectile.timeLeft = 300; break;
                case EstiloCoroCristal: Projectile.timeLeft = 330; break;
                case EstiloViroteVacio: Projectile.timeLeft = 200; break;
                case EstiloFlechaEstelar: Projectile.timeLeft = 240; break;
                case EstiloMinaEstelar: Projectile.timeLeft = 480; break;
                case EstiloEstrellaFugaz: Projectile.timeLeft = 300; break;
                case EstiloTajoPortador: Projectile.timeLeft = 90; break;
                case EstiloCuchillaOrbit: Projectile.timeLeft = 360; break;
                case EstiloCorteRealidad: Projectile.timeLeft = 330; break;
                case EstiloPernoEstelar: Projectile.timeLeft = 220; break;
                case EstiloNubeNebulosa: Projectile.timeLeft = 360; break;
                case EstiloRunaMemorizada: Projectile.timeLeft = 600; break;
                case EstiloColumnaJuicio: Projectile.timeLeft = 210; break;
                case EstiloEstallidoMina: Projectile.timeLeft = 20; break;
                case EstiloPilarAparicion:
                    // v6.50.44 — EL PILAR: cosmético puro (daño 0, jamás
                    // hostil) — cae del cielo, sostiene el descenso del
                    // jefe y se disuelve cuando la luz llega a su puesto.
                    Projectile.timeLeft = 340;
                    Projectile.hostile = false;
                    Projectile.damage = 0;
                    break;

                // === v6.50.45 — LAS ARMAS DEL MOD EN MANOS DEL JEFE ===
                case EstiloRelojGigante:
                    // EL RELOJ GIGANTE (v6.50.54 — LA CATEDRAL DEL TIEMPO:
                    // ×7.5 — UNO SOLO, nacido en el jefe y cabalgando con
                    // él) + el peso de la arena en 560 px. La autocuración
                    // de la talla vive en AI (el msg 27 no lleva OnSpawn).
                    Projectile.timeLeft = 640;
                    Projectile.width = 300;
                    Projectile.height = 380;
                    break;

                case EstiloDanzaSolar:
                    // v6.50.54 — LA DANZA: los rayos son TODO el daño (el
                    // corte honesto por sector — el cuerpo no toca a nadie).
                    Projectile.timeLeft = 560;
                    Projectile.hostile = false;
                    break;

                case EstiloLanzaEterna:
                    // v6.50.54 — LA LANZA: daño 100% MANUAL (ai[3] lo lleva:
                    // el telegrafo es INOFENSIVO — la regla de legibilidad de
                    // la casa — y solo el VUELO corta).
                    Projectile.timeLeft = 150;
                    Projectile.hostile = false;
                    break;

                case EstiloCoronaEterna:
                    // v6.50.54 — LA CORONA: las plumas son TODO el daño.
                    Projectile.timeLeft = 660;
                    Projectile.hostile = false;
                    break;

                case EstiloCoroJefe:
                    // EL CORO: las notas y sus anillos son TODO el daño.
                    Projectile.timeLeft = 640;
                    Projectile.hostile = false;
                    break;

                case EstiloTelarJefe:
                    // EL TELAR: las estrellas y la ignición son TODO el daño.
                    Projectile.timeLeft = 490;
                    Projectile.hostile = false;
                    break;

                case EstiloDecretoJefe:
                    // EL DECRETO: el círculo y sus ejecuciones son TODO el daño.
                    Projectile.timeLeft = 470;
                    Projectile.hostile = false;
                    break;

                case EstiloEstallidoRadiante:
                    // v6.50.53 — EL ESTALLIDO: daño SOLO en la ventana de
                    // la ONDA (los primeros 12 t — la autocuración de la IA
                    // lo reconstruye cada tick: el msg 27 no lleva este
                    // OnSpawn) y 152 t de espectáculo derritiéndose (los
                    // rayos de la imagen viven hasta el final).
                    Projectile.timeLeft = 152;
                    break;

                case EstiloMuerteFinal:
                    // v6.50.80 — LA MUERTE DEL DIOS: mismo espectáculo (152 t
                    // derritiéndose) pero PURA LUZ — hostil y daño a CERO
                    // desde el nacimiento (la autocuración de la IA lo
                    // mantiene así en TODAS las máquinas: el msg 27 no lleva
                    // este OnSpawn).
                    Projectile.timeLeft = 152;
                    Projectile.hostile = false;
                    Projectile.damage = 0;
                    break;

                case EstiloAparicionLuz:
                    // v6.122 — LA EXPLOSIÓN DE LUZ DE LA APARICIÓN: la MUERTE
                    // en su versión de nacimiento — mismo espectáculo, PURA
                    // LUZ (hostil y daño a CERO: la ventana de la onda del
                    // estilo de combate NO corre para este estilo — el cauce
                    // la filtra por Estilo == EstiloEstallidoRadiante).
                    Projectile.timeLeft = 152;
                    Projectile.hostile = false;
                    Projectile.damage = 0;
                    break;

                case EstiloSolJefe:
                    // v6.50.57 — EL SOL DEL DIOS: nace EN el jefe (LA
                    // ASUNCIÓN — lo cabalga mientras carga), se LANZA a la
                    // presa, la persigue LENTO con GRAVEDAD, CRECE hasta la
                    // GIGANTE ROJA (×6) y estalla en LUZ, BRUMA y FORMAS
                    // (la autocuración de la IA reconstruye talla/daño: el
                    // msg 27 no lleva este OnSpawn).
                    Projectile.timeLeft = 520;
                    Projectile.width = 96;
                    Projectile.height = 96;
                    break;
            }
        }

        /// <summary>
        /// v6.50.2 — FIX (LA PÚA MORÍA AL CLAVARSE): el motor mata al
        /// proyectil en la PRIMERA colisión de tile (OnTileCollide default
        /// → true → Kill del else final del cauce) — la IA corría ANTES de
        /// la colisión y jamás veía velocity≈0: _clavada nunca se ponía, las
        /// 90 t de "respiración" y DetonarPua eran código muerto (en SP y
        /// server igual). AHORA la colisión ES el clavado: velocidad a cero,
        /// sin rebote y SIN muerte (return false — la IA detona a su tiempo).
        /// La estrella fugaz recibía el mismo disparo del motor cuando su
        /// chequeo manual de suelo perdía la carrera contra un tick rápido.
        /// Corre en TODAS las máquinas: la simulación local de cada pantalla
        /// queda idéntica (la autoridad detona; el visual acompaña).
        /// </summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Estilo == EstiloPuaSagrario || Estilo == EstiloEstrellaFugaz)
            {
                _clavada = true;
                Projectile.velocity = Vector2.Zero;
                return false; // ni muerte ni rebote: clavada
            }
            return true;
        }

        /// <summary>
        /// v6.50.2 — FIX (hitbox honesta del estallido): el cuerpo de la
        /// mina dibuja ~130 px pero su caja de colisión era la de
        /// SetDefaults (14×14) — solo dañaba al pisar el centro exacto.
        /// Inflada a la talla del visual (golpea lo que SE VE que golpea).
        /// </summary>
        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            if (Estilo == EstiloEstallidoMina)
            {
                const int inflar = 96; // 14 → 110 px de cuerpo
                hitbox.X -= inflar / 2;
                hitbox.Y -= inflar / 2;
                hitbox.Width += inflar;
                hitbox.Height += inflar;
            }

            // v6.50.53/54 — EL ESTALLIDO RADIANTE: la ONDA golpea en el radio
            // TELEGRAFEADO (600·ESCALA px — el aro que el jefe pintó durante
            // toda la carga): la hitbox honesta de la mina, pero de
            // 1200·ESCALA y SOLO durante la ventana de la onda (los
            // primeros 12 t) — después el cuerpo es puro espectáculo.
            if (Estilo == EstiloEstallidoRadiante && _edad <= 12f)
            {
                int inflarE = (int)(1186f * EscalaEstallido); // 14 → 1200·esc px de onda
                hitbox.X -= inflarE / 2;
                hitbox.Y -= inflarE / 2;
                hitbox.Width += inflarE;
                hitbox.Height += inflarE;
            }

        }

        /// <summary>La presa más cercana.</summary>
        private Player Presa()
        {
            Player mejor = null;
            float mejorD = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.dead) continue;
                float d = Vector2.DistanceSquared(p.Center, Projectile.Center);
                if (d < mejorD) { mejorD = d; mejor = p; }
            }
            return mejor;
        }


        public override void AI()
        {
            _edad++;

            // v6.50.2 — FIX (MP: el estado que OnSpawn no lleva): OnSpawn
            // NO corre en los clientes que reciben el proyectil por red
            // (msg 27 — payload sin tileCollide/timeLeft) → la púa
            // ATRAVESABA el suelo en las pantallas remotas y el
            // _centroOrbita quedaba (0,0) (coro/cuchilla/runa teleportados
            // a la esquina del mundo — sus ataques jamás amenazaban a los
            // remotos). Autocuración al inicio de cada IA: el estado se
            // reconstruye desde lo que SÍ viaja (posición + ai[]).
            if (_centroOrbita == Vector2.Zero) _centroOrbita = Projectile.Center;
            Projectile.tileCollide = Estilo == EstiloPuaSagrario ||
                                     Estilo == EstiloEstrellaFugaz;

            // v6.113 — LA PARED DE LUZ (la letra: «los 3 pilares no deben
            // bajar deben ocupar toda la pantalla»): el pilar YA NO baja —
            // nace PARADO y mide de ARRIBA A ABAJO (1.800 px: más alto que
            // cualquier pantalla). La autocuración de la casa reconstruye
            // la forma desde lo que SÍ viaja.
            if (Estilo == EstiloColumnaJuicio && Projectile.height < 1000)
            {
                Vector2 c = Projectile.Center;
                Projectile.width = 76;
                Projectile.height = 1800;
                Projectile.Center = c;
            }

            // v6.50.44 — EL PILAR DE LA APARICIÓN: cosmético (la
            // autocuración también — hostile/damage no viajan enteros por
            // el msg 27: jamás una columna de luz que golpea).
            if (Estilo == EstiloPilarAparicion)
            {
                Projectile.hostile = false;
                Projectile.damage = 0;
            }

            // v6.50.45 — LAS ARMAS SIN CONTACTO (coro/telar/decreto): la
            // misma autocuración MP del pilar — el msg 27 no lleva el
            // OnSpawn, y sin esto los remotos evaluaban colisión hostil.
            // v6.50.54 — LA DANZA, LA LANZA y LA CORONA también (sus daños
            // son 100% manuales — la casa de HerirJugador).
            if (Estilo == EstiloCoroJefe || Estilo == EstiloTelarJefe ||
                Estilo == EstiloDecretoJefe || Estilo == EstiloDanzaSolar ||
                Estilo == EstiloLanzaEterna || Estilo == EstiloCoronaEterna)
            {
                Projectile.hostile = false;
            }

            // v6.50.54 — LOS CABALGADORES (el reloj, el decreto y la danza
            // SIGUEN AL JEFE — ai[2] = su whoAmI: la goma suave de Lerp).
            if (Estilo == EstiloRelojGigante || Estilo == EstiloDecretoJefe ||
                Estilo == EstiloDanzaSolar)
            {
                int quienJ = (int)Projectile.ai[2];
                if (quienJ >= 0 && quienJ < Main.maxNPCs && Main.npc[quienJ] != null &&
                    Main.npc[quienJ].active)
                {
                    Vector2 hacia = Main.npc[quienJ].Center;
                    if (Vector2.DistanceSquared(hacia, Projectile.Center) > 4f)
                        Projectile.Center = Vector2.Lerp(Projectile.Center, hacia, 0.30f);
                }
            }

            // v6.50.46/54 — EL RELOJ GIGANTE ×7.5: la autocuración de la
            // TALLA también (el msg 27 no lleva el OnSpawn — sin esto los
            // remotos colisionaban con una caja de 14×14 en vez de la
            // CATEDRAL de tiempo de 300×380).
            if (Estilo == EstiloRelojGigante && Projectile.width < 300)
            {
                Vector2 cR = Projectile.Center;
                Projectile.width = 300;
                Projectile.height = 380;
                Projectile.Center = cR;
            }

            // v6.50.53 — EL ESTALLIDO RADIANTE: la autocuración de la casa
            // (el msg 27 no lleva el OnSpawn) — hostil/daño se reconstruyen
            // de lo que SÍ viaja (ai[0] el estilo · ai[1] el daño) y el
            // cuerpo queda CLAVADO donde nació: el estallido no se muda.
            if (Estilo == EstiloEstallidoRadiante)
            {
                Projectile.velocity = Vector2.Zero;
                bool ventana = _edad <= 12f;
                Projectile.hostile = ventana;
                Projectile.damage = ventana ? Math.Max(1, (int)Projectile.ai[1]) : 0;
            }

            // v6.50.80 — LA MUERTE DEL DIOS: la autocuración de la casa para
            // el estallido final — PURA LUZ SIEMPRE, en TODAS las máquinas
            // (hostil NUNCA, daño NUNCA: el que mató al dios se lleva el
            // espectáculo entero, no un golpe de gracia a quemarropa.
            // v6.50.81 — y ya NO HAY nada tras el estallido: LA BOLA FINAL
            // retirada — el adiós es la explosión y el silencio).
            if (Estilo == EstiloMuerteFinal)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.hostile = false;
                Projectile.damage = 0;
            }

            // v6.50.57 — EL SOL DEL DIOS: la autocuración de la casa (talla
            // + daño — ai[1] lleva el daño que el jefe le sopló al nacer; la
            // HOSTILIDAD la administra la IA por fase: la carga abraza, el
            // vuelo y la gigante queman, la explosión es AoE honesto).
            if (Estilo == EstiloSolJefe)
            {
                if (Projectile.width < 96)
                {
                    Vector2 cS = Projectile.Center;
                    Projectile.width = 96;
                    Projectile.height = 96;
                    Projectile.Center = cS;
                }
                Projectile.damage = Math.Max(1, (int)Projectile.ai[1]);
            }

            Player presa = Presa();

            switch (Estilo)
            {
                // =============================================================
                //  LA PÚA DEL SAGRARIO — arco, clavado, detona en esquirlas
                // =============================================================
                case EstiloPuaSagrario:
                {
                    if (!_clavada)
                    {
                        // EL ARCO: gravedad del coloso.
                        Projectile.velocity.Y += 0.30f;
                        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                        // ¿CLAVADA? (suelo o techo — el terreno la detiene)
                        if (Projectile.velocity.LengthSquared() < 0.01f) _clavada = true;
                    }
                    else
                    {
                        Projectile.velocity = Vector2.Zero;
                        // LA DETONACIÓN: respira 90 t y estalla.
                        if (_edad > 210)
                        {
                            DetonarPua();
                            Projectile.Kill();
                        }
                    }
                    break;
                }

                // =============================================================
                //  EL CORO DE CRISTAL — órbita y lanza al canto
                // =============================================================
                case EstiloCoroCristal:
                {
                    if (!_lanzado)
                    {
                        // LA ÓRBITA: el anillo del coro (el centro a la deriva
                        // lenta hacia la presa — el coro se acerca cantando).
                        if (presa != null && _edad % 10 == 0)
                        {
                            Vector2 dir = (presa.Center - _centroOrbita).SafeNormalize(Vector2.Zero);
                            _centroOrbita += dir * 2.2f;
                        }
                        float ang = Par + _edad * 0.050f;
                        Projectile.Center = _centroOrbita +
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 118f;
                        Projectile.rotation = ang + MathHelper.PiOver2;

                        // EL CANTO: a los 150 t (o 60 en el coro rápido de la
                        // furia — Par marca la cadencia) TODAS se lanzan.
                        if (_edad >= (Par < 0f ? 60 : 150))
                        {
                            _lanzado = true;
                            if (presa != null)
                                Projectile.velocity = (presa.Center - Projectile.Center)
                                    .SafeNormalize(Vector2.UnitY) * 13f;
                        }
                    }
                    else if (_edad > 330) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  EL VIROTE DE VACÍO — recto, parpadeando entre fases
                // =============================================================
                case EstiloViroteVacio:
                {
                    _posAnterior = Projectile.Center - Projectile.velocity * 1.6f;
                    if (_edad > 200) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  LA FLECHA ESTELAR — corrección de rumbo y estela
                // =============================================================
                case EstiloFlechaEstelar:
                {
                    if (presa != null && _edad < 46)
                    {
                        Vector2 deseada = (presa.Center - Projectile.Center)
                            .SafeNormalize(Vector2.UnitY) * 14f;
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, deseada, 0.055f);
                    }
                    Projectile.rotation = Projectile.velocity.ToRotation();
                    if (_edad > 240) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  LA MINA ESTELAR — se arma, avisa, DETONA en arco
                // =============================================================
                case EstiloMinaEstelar:
                {
                    Projectile.velocity *= 0.90f; // se asienta donde cae
                    float distPresa = presa != null
                        ? Vector2.Distance(presa.Center, Projectile.Center) : 9999f;

                    // LA DETONACIÓN: presa en el radio del arco (160 px) o
                    // vida agotada (el suelo queda sembrado un rato).
                    if ((distPresa < 160f && _edad > 40) || _edad > 440)
                    {
                        DetonarMina(presa);
                        Projectile.Kill();
                    }
                    break;
                }

                // =============================================================
                //  LA ESTRELLA FUGAZ — cae sobre la marca
                // =============================================================
                case EstiloEstrellaFugaz:
                {
                    if (!_clavada)
                    {
                        Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.42f, 15f);
                        Projectile.rotation += 0.22f;

                        // ¿IMPACTO? (terreno — tileCollide activo para esta)
                        Point tile = Projectile.Center.ToTileCoordinates();
                        if (tile.X > 5 && tile.X < Main.maxTilesX - 5 &&
                            tile.Y > 5 && tile.Y < Main.maxTilesY - 5)
                        {
                            Tile tl = Main.tile[tile.X, tile.Y + 1];
                            if (tl != null && tl.HasTile && Main.tileSolid[tl.TileType])
                            {
                                _clavada = true;
                                DetonarFugaz();
                            }
                        }
                    }
                    else { DetonarFugaz(); }
                    if (_edad > 300) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  EL TAJO DEL PORTADOR — marca que respira, corte que florece
                // =============================================================
                case EstiloTajoPortador:
                {
                    if (_edad < 24)
                    {
                        // LA MARCA: el proyectil ya voló a su sitio (la fija
                        // el duelista) y la tensión crece.
                        Projectile.velocity *= 0.82f;
                    }
                    else if (_edad == 24)
                    {
                        // EL FLORECIMIENTO: arranque violento en la dirección
                        // de la marca (Par = ángulo del corte).
                        Projectile.velocity = new Vector2(MathF.Cos(Par), MathF.Sin(Par)) * 15f;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71, Projectile.Center);
                    }
                    if (_edad > 72) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  LA CUCHILLA EN ÓRBITA — gira y se dispara al cierre
                // =============================================================
                case EstiloCuchillaOrbit:
                {
                    if (!_lanzado)
                    {
                        float ang = Par + _edad * 0.065f;
                        Projectile.Center = _centroOrbita +
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 104f;
                        Projectile.rotation = ang + MathHelper.PiOver2;

                        // LA PRESA ENTRA AL ANILLO → disparo.
                        if (presa != null && _edad > 30)
                        {
                            float d = Vector2.Distance(presa.Center, _centroOrbita);
                            if (d < 170f)
                            {
                                _lanzado = true;
                                Projectile.velocity = (presa.Center - Projectile.Center)
                                    .SafeNormalize(Vector2.UnitY) * 15f;
                            }
                        }
                    }
                    else if (_edad > 360) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  EL CORTE DE REALIDAD — la pared que avanza
                // =============================================================
                case EstiloCorteRealidad:
                {
                    // AVANZA LENTA (la pared que parte el suelo: no persigue,
                    // OBLIGA a moverse — el campo del duelista).
                    Projectile.velocity = new Vector2(MathF.Cos(Par), MathF.Sin(Par)) * 3.4f;
                    if (_edad > 330) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  LA COLUMNA DEL JUICIO — la luz que cae del cielo (v6.50.36)
                // =============================================================
                case EstiloColumnaJuicio:
                {
                    // v6.113 — EL MURO DEL JUICIO (la letra: «los 3 pilares
                    // no deben bajar deben ocupar toda la pantalla»): el
                    // pilar YA NO DESCIENDE — NASCE PARADO y ocupa TODA la
                    // pantalla de arriba a abajo. TRES tiempos por ai[1]:
                    //   0 · 26 t — EL TELEGRAFO: la línea fina que vibra
                    //              (SIN daño: hostile apagado).
                    //   1 · 16 t — LA ERUPCIÓN: la pared SE ENCIENDE con
                    //              SU hitbox de 1.800 px (76 de ancho):
                    //              castiga al que no se movió de la línea.
                    //   2 · 30 t — LA DISOLUCIÓN: la luz se apaga hacia
                    //              arriba (hostile apagado otra vez).
                    float edad = _edad;
                    if (edad < 26f)
                    {
                        Projectile.ai[1] = 0f;
                        Projectile.velocity = Vector2.Zero;
                        Projectile.hostile = false;
                        if (edad == 1f && !Main.dedServ)
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item122.WithPitchOffset(0.35f), Projectile.Center);
                        // LAS CHISPAS DEL TELÉGRAFO: motas doradas cayendo por
                        // la línea (la casa: Hash01, jamás Main.rand).
                        if (!Main.dedServ && ((int)edad % 6) == 0)
                        {
                            for (int ch = 0; ch < 3; ch++)
                            {
                                float yy = Projectile.Center.Y - 700f + ch * 700f;
                                Dust d = Dust.NewDustPerfect(
                                    new Vector2(Projectile.Center.X, yy), DustID.GoldFlame,
                                    new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(0.3f, 1.1f)),
                                    130, new Color(255, 224, 150), 0.7f);
                                d.noGravity = true;
                            }
                        }
                    }
                    else if (edad < 42f)
                    {
                        if (Projectile.ai[1] < 1f)
                        {
                            Projectile.ai[1] = 1f;
                            // LA PARED ENCIENDE: el golpe del juicio.
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, Projectile.Center);
                            OndaLib.Kick(9f, 18);
                        }
                        Projectile.hostile = true;   // 16 t de veredicto
                        // LAS CHISPAS DE LA ERUPCIÓN a lo largo del muro.
                        if (!Main.dedServ && ((int)edad % 3) == 0)
                        {
                            float yy = Projectile.Center.Y - 800f +
                                ((edad * 97f) % 1600f);
                            Dust d = Dust.NewDustPerfect(
                                new Vector2(Projectile.Center.X + Main.rand.NextFloat(-30f, 30f), yy),
                                DustID.GoldFlame,
                                new Vector2(Main.rand.NextFloat(-1.6f, 1.6f), -Main.rand.NextFloat(0.5f, 2.2f)),
                                165, new Color(255, 236, 170), 0.9f);
                            d.noGravity = true;
                        }
                    }
                    else
                    {
                        if (Projectile.ai[1] < 2f)
                            Projectile.ai[1] = 2f;
                        Projectile.hostile = false;  // la disolución no golpea
                    }
                    if (edad > 72f)
                        Projectile.Kill();
                    break;
                }

                // =============================================================
                //  EL PERNO ESTELAR — recto con latido (o GUIADO: ai[1]=1,
                //  las balas del eclipse que te persiguen)
                // =============================================================
                case EstiloPernoEstelar:
                {
                    Projectile.rotation = Projectile.velocity.ToRotation();

                    // v6.50.44 — EL PERNO GUIADO (ai[1]=1 — las balas del
                    // ECLIPSE): una curva SUAVE hacia la presa (≤0.055
                    // rad/t, solo si está a <900 px) y una aceleración
                    // siniestra al acercarse — la oscuridad no dispara a un
                    // punto: TE BUSCA. Los pernos comunes (ai[1]=0) siguen
                    // siendo balística honesta.
                    if ((Par == 1f || Par == 3f) && _edad > 8 && presa != null &&
                        Vector2.Distance(presa.Center, Projectile.Center) < 900f)
                    {
                        float deseado = (presa.Center - Projectile.Center).ToRotation();
                        float giro = MathHelper.WrapAngle(deseado - Projectile.rotation);
                        if (Math.Abs(giro) > 0.055f) giro = Math.Sign(giro) * 0.055f;
                        Projectile.velocity = Projectile.velocity.RotatedBy(giro);
                        if (Projectile.velocity.Length() < 9.5f)
                            Projectile.velocity *= 1.014f;
                        Projectile.rotation = Projectile.velocity.ToRotation();
                    }

                    if (_edad > 220) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  LA NUBE DE NEBULOSA — la zona que quema (por contacto)
                // =============================================================
                case EstiloNubeNebulosa:
                {
                    // Deriva lenta hacia la presa (la nube persigue sin prisa).
                    if (presa != null)
                    {
                        Vector2 dir = (presa.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * 1.4f, 0.01f);
                    }
                    if (_edad > 360) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  LA RUNA MEMORIZADA — orbita a Aethon y dispara TUS trucos
                // =============================================================
                case EstiloRunaMemorizada:
                {
                    // LA ÓRBITA a Aethon (ai[1] = fase; el centro a la deriva
                    // hacia la presa lento — la memoria se acerca).
                    if (presa != null && _edad % 12 == 0)
                    {
                        Vector2 dir = (presa.Center - _centroOrbita).SafeNormalize(Vector2.Zero);
                        _centroOrbita += dir * 2.6f;
                    }
                    float ang = Par + _edad * 0.038f;
                    Projectile.Center = _centroOrbita +
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 150f;
                    Projectile.rotation = ang;

                    // EL DISPARO (v6.50.46 — LOS TAJOS MUEREN: la petición
                    // fue LITERAL — «el ataque de tajos que hace el jefe es
                    // feo, quitalo»): cada 110 t la runa RECUERDA tu estilo
                    // con un ABANICO de pernos — tres líneas de luz que se
                    // leen y se esquivan (nada de media lunas feas).
                    if (_edad % 110 == 0 && _edad > 0 && presa != null)
                    {
                        float dirBase = (presa.Center - Projectile.Center).ToRotation();
                        for (int l = 0; l < 3; l++)
                        {
                            float angF = dirBase + (l - 1) * 0.17f;
                            Vector2 vel = new Vector2(MathF.Cos(angF), MathF.Sin(angF)) * 10.5f;
                            // v6.50.1 — FIX (MP ×N+1): solo la autoridad spawnnea.
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                                    Projectile.Center, vel,
                                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                                    (int)(Projectile.damage * 0.7f), 2f, Main.myPlayer,
                                    EstiloPernoEstelar, 0f, Seed + 17 + l);
                            }
                        }
                    }
                    if (_edad > 600) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  EL ESTALLIDO DE MINA — el cuerpo del boom (20 t honestos)
                // =============================================================
                case EstiloEstallidoMina:
                {
                    Projectile.velocity = Vector2.Zero;
                    // v6.50.2 — FIX (el fantasma de 7 s en clientes MP): el
                    // timeLeft=20 se fija en OnSpawn, que NO corre al recibir
                    // el msg 27 → las pantallas remotas mantenían la zona 420 t
                    // (colisión hostil evaluada EN cada cliente local). La edad
                    // SÍ corre en todas las máquinas: muerte por edad,
                    // idempotente con el timeLeft de la autoridad.
                    if (_edad > 20) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  EL PILAR DE LA APARICIÓN — la columna de luz que cae del
                //  cielo (v6.50.44 — la entrada de la EMPERATRIZ: el
                //  destello desde arriba y el jefe bajando por él)
                // =============================================================
                case EstiloPilarAparicion:
                {
                    Projectile.velocity = Vector2.Zero;

                    // ¿El jefe sigue BAJANDO? (ai[2] = su whoAmI). Cuando la
                    // luz llega a su órbita (o muere/se va), el pilar se
                    // DISUELVE (ai[1]=1 viaja por el cable: todas las
                    // pantallas se enteran).
                    if (Projectile.ai[1] < 1f)
                    {
                        bool jefeVivo = false;
                        int who = (int)Projectile.ai[2];
                        if (who >= 0 && who < Main.maxNPCs)
                        {
                            NPC jefe = Main.npc[who];
                            // EST_NACIENDO (ai[0]=0) con aparición (ai[1]=10
                            // — v6.50.52, la llegada en DOS actos) — el pilar
                            // vive mientras la llegada no haya terminado.
                            jefeVivo = jefe != null && jefe.active &&
                                jefe.ai[0] == 0f &&
                                jefe.ai[1] == 10f;
                        }
                        if (!jefeVivo) Projectile.ai[1] = 1f;   // DISOLVERSE
                    }
                    else
                    {
                        // disuelto: 40 t de fade y fuera.
                        if (_edad > 240 || _ticksDisolver >= 40) Projectile.Kill();
                        _ticksDisolver++;
                    }

                    // LA LLUVIA DEL VELO: chispas doradas cayendo por el
                    // cuerpo del pilar (la luz que se derrama).
                    if (!Main.dedServ && Main.rand.NextBool(2))
                    {
                        float p = Math.Min(1f, _edad / 40f);
                        float y = Projectile.Center.Y - 900f * Main.rand.NextFloat(0.05f, 0.98f * p);
                        Dust d = Dust.NewDustPerfect(
                            new Vector2(Projectile.Center.X + Main.rand.NextFloat(-55f, 55f), y),
                            DustID.GoldFlame,
                            new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(1f, 2.6f)),
                            170, OroGrimorio, 1.1f);
                        d.noGravity = true;
                    }

                    // LA LUZ del cielo abierto (la columna ilumina la arena).
                    if (!Main.dedServ)
                    {
                        Lighting.AddLight(Projectile.Center, new Vector3(0.85f, 0.72f, 0.42f));
                        Lighting.AddLight(Projectile.Center - new Vector2(0f, 450f),
                            new Vector3(0.55f, 0.46f, 0.26f));
                    }
                    break;
                }

                // =============================================================
                //  v6.50.45 — EL RELOJ DE ARENA GIGANTE (la petición: «el
                //  proyectil de bastón del reloj de arena cósmica pero en
                //  GIGANTE»): la MISMA línea de tiempo del bastón (290 t
                //  de caída + 26 de inversión, 26 granos, giros de π
                //  acumulados) — v6.50.46 a ×5.2 y en ANILLOS DE CUATRO
                //  que CAEN del cielo alrededor de la presa — y el PESO
                //  invertido: la arena aplasta a los JUGADORES de abajo
                //  (daño cada 10 t + empuje hacia ABAJO — el castigo del
                //  volador) y cada INVERSIÓN es el pulso que HUNDE.
                //  v6.50.46 — LA CAÍDA: los primeros 26 t caen firmes
                //  (13 px/t — vienen del cielo), después frenan y quedan
                //  FLOTANDO en su puesto del anillo; el peso solo pesa
                //  YA ATERRIZADO (nadie paga por un reloj que aún viaja).
                // =============================================================
                case EstiloRelojGigante:
                {
                    // v6.50.54 — LA CATEDRAL DEL TIEMPO: YA NO CAE del cielo
                    // ni aterriza — NACE EN EL JEFE (el cabalgador de arriba
                    // lo pega a él) y su arena corre DESDE EL TICK CERO.
                    Projectile.velocity = Vector2.Zero;

                    // LA LÍNEA DE TIEMPO del bastón (la MISMA: la fuente
                    // única de la casa — 290 + 26, giro π suave) — corriendo
                    // desde que el reloj nace.
                    float edadArena = _edad;
                    float cicloR = edadArena % 316f;
                    int vueltasR = (int)(edadArena / 316f);
                    if (cicloR >= 290f)
                    {
                        float g = (cicloR - 290f) / 26f;
                        float suave = g * g * (3f - 2f * g);
                        Projectile.rotation = (vueltasR + suave) * MathHelper.Pi;
                    }
                    else
                        Projectile.rotation = vueltasR * MathHelper.Pi;

                    // EL PESO DE LA ARENA (mientras cae, cada 10 t — alrededor
                    // DEL JEFE: radio 560, la sombra de la catedral entera).
                    bool cayendoR = cicloR < 290f;
                    if (cayendoR && edadArena >= 10f && ((int)edadArena % 10) == 0)
                        PesarJugadores(0.35f, 2.6f, 560f);

                    // LA INVERSIÓN: el pulso que HUNDE.
                    if (cicloR >= 290f && cicloR < 291.5f && edadArena > 26f)
                    {
                        PesarJugadores(0.90f, 6.0f, 600f);
                        if (Main.netMode != NetmodeID.Server)
                        {
                            OndaLib.Kick(4f, 12);
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item45.WithPitchOffset(-0.35f), Projectile.Center);
                            for (int i = 0; i < 10; i++)
                            {
                                float ang = i / 10f * MathHelper.TwoPi + Seed * 0.13f;
                                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                                    Main.rand.NextFloat(1.2f, 3.2f),
                                    150, new Color(255, 220, 130), 1.0f);
                                d.noGravity = true;
                            }
                        }
                    }

                    // v6.50.59 — EL RELOJ DE TODA LA FASE 2 (la letra:
                    // «el ataque reloj de arena del jefe es muy corto,
                    // deberia durar al menos toda la fase 2 completa»):
                    // nacido con ai[1]=2 (el jefe lo lanza en fase 2), el
                    // reloj es INMORTAL mientras SU jefe siga vivo y en
                    // fase 2 — la catedral del tiempo cabalga con él por
                    // TODA la fase (el resto de platos sigue saliendo con
                    // el reloj encima). El día que la fase acaba (fase 3+ o
                    // el dios muerto) el reloj SE DISUELVE en 40 t: dura
                    // exacto toda la fase 2, ni un tick menos ni uno más.
                    if (Projectile.ai[1] >= 2f)
                    {
                        bool fase2Viva = false;
                        int dueno = (int)Projectile.ai[2];
                        if (dueno >= 0 && dueno < Main.maxNPCs && Main.npc[dueno] != null &&
                            Main.npc[dueno].active &&
                            Main.npc[dueno].ModNPC is AethonBoss jefeReloj)
                            fase2Viva = jefeReloj.FasePublica == 2;

                        if (fase2Viva)
                        {
                            // INMORTAL: el timeLeft jamás baja del minuto.
                            if (Projectile.timeLeft < 3600) Projectile.timeLeft = 3600;
                        }
                        else
                        {
                            // LA FASE ACABÓ — la disolución final (40 t).
                            if (Projectile.timeLeft > 40) Projectile.timeLeft = 40;
                            if (_edad > 620f) Projectile.Kill();
                        }
                    }
                    else if (_edad > 620f) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  v6.50.45 — EL CORO ESPECTRAL (la petición: «el proyectil
                //  de bastón del coro espectral»): seis notas de luz
                //  orbitan al ANCLA (que deriva hacia la presa — el coro
                //  te sigue cantando) y cada ciclo LA NOTA SIGUIENTE emite
                //  su anillo: el FRENTE del anillo corta a quien atraviesa
                //  (|dist − radio| < 16). Daño bajo pero FRECUENTE: el
                //  canto castiga al que se queda quieto.
                //  v6.50.46 — EL FIX QUE LO DESPERTÓ: este proyectil NUNCA
                //  funcionó — escribía su ancla en ai[0]/ai[1] y el
                //  ESTILO vive en ai[0] (el estilo moría en el tick 2 y
                //  el coro quedaba invisible e inerte para siempre). EL
                //  ANCLA AHORA ES EL PROYECTIL MISMO: el cuerpo ES el
                //  ancla (deriva moviéndose a sí mismo — la posición ya
                //  viaja por el cable con netImportant) y ai[0] queda
                //  INTACTO para siempre.
                // =============================================================
                case EstiloCoroJefe:
                {
                    Projectile.velocity = Vector2.Zero;
                    Player presaC = Presa();

                    // EL ANCLA (el cuerpo del proyectil) deriva hacia la presa.
                    if (presaC != null)
                    {
                        Vector2 dir = (presaC.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                        Projectile.Center += dir * 0.9f;
                    }
                    Vector2 ancla = Projectile.Center;

                    // LA SIEMBRA (órbitas propias: radio, velocidad y
                    // sentido por nota — el coro nunca se alinea).
                    if (!_coroSembrada)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            _coroFase[i] = VFXCore.Hash01(Seed, i, 71) * MathHelper.TwoPi;
                            _coroRadio[i] = 200f + 130f * VFXCore.Hash01(Seed, i, 73);
                            _coroVel[i] = (0.018f + 0.014f * VFXCore.Hash01(Seed, i, 77))
                                * ((i & 1) == 0 ? 1f : -1f);
                            _coroAlto[i] = -40f + 80f * VFXCore.Hash01(Seed, i, 79);
                            _coroNotas[i] = ancla;
                        }
                        _coroSembrada = true;
                    }

                    // EL VUELO de las notas (elipses propias).
                    for (int i = 0; i < 6; i++)
                    {
                        _coroFase[i] += _coroVel[i];
                        _coroNotas[i] = ancla + new Vector2(
                            MathF.Cos(_coroFase[i]) * _coroRadio[i],
                            MathF.Sin(_coroFase[i]) * _coroRadio[i] * 0.6f + _coroAlto[i]);
                    }

                    // EL CANTO: cada ciclo (48 t — 36 en furia) la nota
                    // siguiente EMITE su anillo.
                    int cicloC = Par > 0.5f ? 36 : 48;
                    if (((int)_edad % cicloC) == 0 && _edad > 8f && _edad < 540f)
                    {
                        int nota = _coroNotaActual++ % 6;
                        _coroAnillos.Add(new AnilloCoro
                        { Origen = _coroNotas[nota], Edad = 0f, Nota = nota });
                        if (Main.netMode != NetmodeID.Server)
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item70.WithPitchOffset(-0.35f + nota * 0.13f),
                                _coroNotas[nota]);
                    }

                    // LOS ANILLOS crecen y su FRENTE corta (autoridad).
                    float radioFinalC = Par > 0.5f ? 300f : 260f;
                    for (int a = _coroAnillos.Count - 1; a >= 0; a--)
                    {
                        AnilloCoro anillo = _coroAnillos[a];
                        anillo.Edad += 1f;
                        float radio = radioFinalC * (anillo.Edad / 44f);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int pl = 0; pl < Main.maxPlayers; pl++)
                            {
                                Player p = Main.player[pl];
                                if (p == null || !p.active || p.dead) continue;
                                float dP = Vector2.Distance(p.Center, anillo.Origen);
                                if (Math.Abs(dP - radio) < 16f)
                                    HerirJugador(p, (int)(Projectile.damage * 0.6f), anillo.Origen);
                            }
                        }
                        if (anillo.Edad > 44f) _coroAnillos.RemoveAt(a);
                    }

                    if (_edad > 620f) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  v6.50.46 — EL TELAR DE CONSTELACIONES — LA FIGURA QUE
                //  DIBUJA EL JEFE (la corrección LITERAL: «el jefe debe
                //  moverse formando una figura de tantas puntas como
                //  proyectiles se puedan lanzar al rededor del jugador
                //  y rapidamente»). EL TELAR YA NO CLAVA SOLO: el jefe
                //  TRAZA LA ESTRELLA de punta en punta (pentagrama de 5
                //  · heptagrama de 7 en furia) y cada vez que PASA por
                //  una punta, SU ESTRELLA se clava ahí — la figura que
                //  dibuja el jefe ES la jaula. Con ≥4 clavadas la figura
                //  se ENCIENDE cada 30 t y TODO jugador DENTRO del
                //  polígono paga el corte (ray-casting, como el bastón).
                //  EL FIX QUE LO DESPERTÓ: este proyectil NUNCA funcionó
                //  — escribía su centro en ai[0]/ai[1] y el ESTILO vive
                //  en ai[0] (las estrellas jamás se clavaron: la IA murió
                //  en el tick 20 y el jefe corría el círculo para nadie).
                //  AHORA el centro ES el proyectil (clavado donde nació)
                //  y ai[0] queda INTACTO para siempre.
                // =============================================================
                case EstiloTelarJefe:
                {
                    Projectile.velocity = Vector2.Zero;
                    int puntasT = Par > 0.5f ? 7 : 5;

                    // EL JEFE traza la figura (ai[2] = su whoAmI): cuando
                    // pasa cerca de una punta SIN clavar, LA ESTRELLA se
                    // clava ahí — la jaula nace del TRAZO de la luz.
                    int quienT = (int)Projectile.ai[2];
                    NPC jefeT = (quienT >= 0 && quienT < Main.maxNPCs)
                        ? Main.npc[quienT] : null;
                    bool jefeVivo = jefeT != null && jefeT.active;
                    for (int i = 0; i < puntasT; i++)
                    {
                        if (_estrellaClavada[i]) continue;
                        Vector2 puntaT = PuntaTelar(Projectile.Center, i, puntasT, Seed);
                        if (jefeVivo && Vector2.Distance(jefeT.Center, puntaT) < 110f)
                        {
                            _estrellas[i] = puntaT;
                            _estrellaClavada[i] = true;
                            if (Main.netMode != NetmodeID.Server)
                                Terraria.Audio.SoundEngine.PlaySound(
                                    SoundID.Item4.WithPitchOffset(0.3f + i * 0.06f), puntaT);
                        }
                    }

                    // EL FLASH de la ignición se apaga.
                    if (_telarFlash > 0.003f) _telarFlash *= 0.85f;
                    else _telarFlash = 0f;

                    // LA IGNICIÓN (≥4 estrellas, cada 30 t): el corte a
                    // TODO jugador DENTRO del polígono (el telar ATRAPA).
                    // v6.50.46 — LA JAULA en ORDEN DE ÍNDICE: el jefe clava
                    // las estrellas por el SALTO de la estrella (0,2,4,1,3…)
                    // pero el polígono que ATRAPA conecta las puntas en su
                    // orden natural — el veredicto y los hilos dibujan la
                    // MISMA jaula.
                    Vector2[] jaula = new Vector2[7];
                    int clavadas = EstrellasClavadas(jaula);
                    if (clavadas >= 4 && ((int)_edad % 30) == 0 && _edad > 30f)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int pl = 0; pl < Main.maxPlayers; pl++)
                            {
                                Player p = Main.player[pl];
                                if (p == null || !p.active || p.dead) continue;
                                if (PuntoEnPoligono(p.Center, jaula, clavadas))
                                    HerirJugador(p, (int)(Projectile.damage * 0.65f),
                                        Projectile.Center);
                            }
                        }
                        _telarFlash = 1f;
                        if (clavadas >= puntasT && Main.netMode != NetmodeID.Server)
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item122.WithPitchOffset(0.4f), Projectile.Center);
                    }

                    if (_edad > 560f) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  v6.50.46 — EL DECRETO DEL ECLIPSE (la petición: «el
                //  proyectil el decreto del eclipse EN EL CAMBIO DE FASES y
                //  POR CADA FASE QUE SEA MÁS GRANDE, al usarlo el jefe queda
                //  INMÓVIL así le da tiempo al jugador a escapar»):
                //  NUNCA FUNCIONÓ — el círculo escribía su radio en ai[0]
                //  (¡donde vive el ESTILO!) en su PRIMER tick: la IA y el
                //  render morían al instante y quedaba un círculo invisible
                //  congelado (el jefe CLAVADO decretando para nadie — «el
                //  jefe no lo usa al cambio de fase»). EL FIX: el radio se
                //  COMPUTA de la edad (determinista, la misma en todas las
                //  máquinas) y ai[0] queda INTACTO. El círculo nace a 80 px
                //  sobre el jugador, CRECE +2 px/t hasta su radio máximo
                //  (P2 600 → P5 960 — ai[1] = la fase) y cada 15 t LA
                //  EJECUCIÓN corta a TODO jugador del círculo. El jefe
                //  inmóvil en su estado ES la ventana de escape — y la
                //  última ejecución (círculo lleno) ES LA SENTENCIA: paga
                //  doble.
                // =============================================================
                case EstiloDecretoJefe:
                {
                    // v6.50.54 — EL CÍRCULO CABALGA CON EL DIOS: el
                    // cabalgador de la AI lo pega al jefe (ai[2] = su
                    // whoAmI) — el decreto se lee EN MOVIMIENTO.
                    Projectile.velocity = Vector2.Zero;

                    // EL RADIO: 80 + 2·edad, tope LA FASE (computado — NUNCA
                    // escrito en ai[0]: el estilo vive ahí).
                    int faseD = Math.Max(2, (int)Par);
                    float radioMaxD = 600f + (faseD - 2) * 120f;
                    float radio = Math.Min(80f + 2f * _edad, radioMaxD);

                    // LA EJECUCIÓN CÍCLICA (cada 15 t — el beat del bastón).
                    if (((int)_edad % 15) == 0 && _edad >= 15f &&
                        Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        bool sentencia = radio >= radioMaxD - 4f;   // la FINAL paga doble
                        for (int pl = 0; pl < Main.maxPlayers; pl++)
                        {
                            Player p = Main.player[pl];
                            if (p == null || !p.active || p.dead) continue;
                            if (Vector2.Distance(p.Center, Projectile.Center) <= radio)
                                HerirJugador(p, (int)(Projectile.damage *
                                    (sentencia ? 1.1f : 0.55f)), Projectile.Center);
                        }
                        if (Main.netMode != NetmodeID.Server)
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item122.WithPitchOffset(-0.3f), Projectile.Center);
                    }

                    // La vida: el círculo terminó de crecer + un compás.
                    float vidaD = (radioMaxD - 80f) / 2f + 25f;
                    if (_edad > vidaD) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  v6.50.53 — EL ESTALLIDO RADIANTE (LA IMAGEN DEL USUARIO:
                //  «te envié una imagen, crea por código un ataque del jefe
                //  que sea igual que la imagen» — el punto de luz potente:
                //  la explosión radiante con anillo segmentado, rayos en
                //  360°, chispas y el destello que inunda)
                // =============================================================
                case EstiloEstallidoRadiante:
                case EstiloMuerteFinal:
                case EstiloAparicionLuz:
                {
                    // v6.50.80 — LA MUERTE comparte el cauce del ataque (el
                    // estallido del dios muriendo ES su propio ataque, en su
                    // versión más grande) — solo el NACIMIENTO cambia de
                    // talla: más chispas, más fuerte, el estruendo final.
                    // v6.122 — y LA APARICIÓN comparte el mismo cauce: la
                    // bienvenida del dios ES su propia luz.
                    bool esLaMuerte = Estilo == EstiloMuerteFinal;
                    bool esAparicion = Estilo == EstiloAparicionLuz;

                    // CLAVADO donde nació (estalló donde estaba el jefe) —
                    // la autocuración del inicio ya puso hostil/daño de la
                    // ventana de la ONDA (los primeros 12 t: el radio 600
                    // TELEGRAFEADO por el aro de la carga). LA MUERTE no
                    // tiene ventana: es pura luz, jamás daña.
                    Projectile.velocity = Vector2.Zero;

                    // LA LUZ QUE INUNDA EL MUNDO (el flash de la imagen: la
                    // escena entera queda BAÑADA en blanco-oro — 2,5 s de
                    // día dentro de la pelea) — VIVO, no el 0.5 plano del
                    // SetDefaults: la luz de un dios que se enciende (y que
                    // CRECE con la fase — la escala de la .54; en la muerte,
                    // la escala 1.85 la hace LA INUNDACIÓN definitiva).
                    float luzE = Math.Max(0f, 1f - _edad / 150f);
                    Lighting.AddLight(Projectile.Center,
                        new Vector3(2.4f, 2.1f, 1.5f) * luzE * EscalaEstallido);

                    // EL NACIMIENTO (t=1): el estampido + LAS CHISPAS QUE
                    // VUELAN (los streaks radiales de la imagen — cada
                    // cliente ve las suyas: es polvo, pura decoración). EN
                    // LA MUERTE: 80 chispas al doble de velocidad — la
                    // despedida arrasa.
                    if (_edad <= 1f)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, Projectile.Center);
                        OndaLib.Kick(esLaMuerte ? 20f : esAparicion ? 16f : 13f,
                                     esLaMuerte ? 40 : esAparicion ? 32 : 26);
                        int rachas = esLaMuerte ? 80 : esAparicion ? 56 : 42;
                        for (int i = 0; i < rachas; i++)
                        {
                            float ang = i * MathHelper.TwoPi / rachas +
                                Main.rand.NextFloat(-0.06f, 0.06f);
                            float v = esLaMuerte
                                ? Main.rand.NextFloat(9f, 24f)
                                : esAparicion
                                    ? Main.rand.NextFloat(8f, 21f)
                                    : Main.rand.NextFloat(6f, 17f);
                            Dust d = Dust.NewDustPerfect(Projectile.Center,
                                DustID.GoldFlame,
                                new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * v);
                            d.noGravity = true;
                            d.scale = Main.rand.NextFloat(1.1f, esLaMuerte ? 2.2f : 1.9f);
                        }
                    }
                    break;
                }

                // =============================================================
                //  v6.50.54 — LA DANZA SOLAR (Sun Dance): la rueda de SEIS
                //  rayos que gira LENTO y cabalga con el jefe — TRES tandas
                //  de 175 t (cada tanda desfasada en el reloj), cada rayo
                //  con SU hitbox de barra honesta (dot/cruz contra los SEIS
                //  rumbos — la geometría de la Espada Zenia). Los primeros
                //  25 t de cada tanda los rayos son TRANSLÚCIDOS e
                //  inofensivos (la regla de la casa: lo que aún no daña se ve
                //  translúcido — se ENCIENDEN al matar).
                // =============================================================
                case EstiloDanzaSolar:
                {
                    Projectile.velocity = Vector2.Zero;

                    // LA TANDA y su fase local.
                    int tanda = (int)(_edad / 175f);
                    float tEdad = _edad % 175f;
                    bool ardiendo = tEdad > 25f && tanda < 3 && _edad < 520f;

                    if (ardiendo && Main.netMode != NetmodeID.MultiplayerClient &&
                        ((int)_edad % 5) == 0)
                    {
                        float giroDan = _edad * 0.0125f + tanda * 0.35f;
                        for (int pl = 0; pl < Main.maxPlayers; pl++)
                        {
                            Player p = Main.player[pl];
                            if (p == null || !p.active || p.dead) continue;
                            Vector2 rel = p.Center - Projectile.Center;
                            for (int r = 0; r < 6; r++)
                            {
                                float angR = giroDan + r * MathHelper.TwoPi / 6f;
                                Vector2 dirR = new Vector2(MathF.Cos(angR), MathF.Sin(angR));
                                float dot = Vector2.Dot(rel, dirR);
                                if (dot < 50f || dot > 920f) continue;
                                float cruz = MathF.Abs(rel.X * dirR.Y - rel.Y * dirR.X);
                                if (cruz < 24f)
                                {
                                    HerirJugador(p, (int)(Projectile.damage * 0.9f),
                                        Projectile.Center);
                                    break;
                                }
                            }
                        }
                    }

                    // EL LATIDO de la tanda (el sonido del compás — cada
                    // tanda nueva suena a sol).
                    if (tEdad < 1f && tanda > 0 && Main.netMode != NetmodeID.Server)
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item122.WithPitchOffset(-0.2f), Projectile.Center);

                    if (_edad > 555f) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  v6.50.54 — LA LANZA ETERNA (Ethereal Lance): 50 t de
                //  TELEGRAFO (la línea fina translúcida de origen a destino
                //  — inofensiva) y EL VUELO (15 px/t a su destino — el daño
                //  manual viaja en ai[3]). El destino viaja en ai[1]/ai[2].
                // =============================================================
                case EstiloLanzaEterna:
                {
                    Vector2 destinoL = new Vector2(Projectile.ai[1], Projectile.ai[2]);

                    if (_edad < 50f)
                    {
                        // EL TELEGRAFO: quieta, translúcida, inofensiva.
                        Projectile.velocity = Vector2.Zero;
                        Projectile.rotation = (destinoL - Projectile.Center).ToRotation();
                    }
                    else
                    {
                        // EL VUELO: 15 px/t hacia su destino — el corte.
                        if (Projectile.velocity.LengthSquared() < 0.01f)
                        {
                            Vector2 dirL = destinoL - Projectile.Center;
                            if (dirL.LengthSquared() > 1f) dirL.Normalize();
                            Projectile.velocity = dirL * 15f;
                            Projectile.rotation = dirL.ToRotation();
                            if (Main.netMode != NetmodeID.Server)
                                Terraria.Audio.SoundEngine.PlaySound(
                                    SoundID.Item4.WithPitchOffset(0.4f), Projectile.Center);
                        }
                        // EL CORTE MANUAL (hostil=false siempre — el cauce de
                        // la casa: HerirJugador respeta iframes; el daño
                        // llegó en el spawn — en SP/servidor vive entero).
                        if (Main.netMode != NetmodeID.MultiplayerClient &&
                            ((int)_edad % 4) == 0)
                        {
                            int danoL = Math.Max(1, Projectile.damage);
                            for (int pl = 0; pl < Main.maxPlayers; pl++)
                            {
                                Player p = Main.player[pl];
                                if (p == null || !p.active || p.dead) continue;
                                if (Vector2.Distance(p.Center, Projectile.Center) < 30f)
                                    HerirJugador(p, danoL, Projectile.Center);
                            }
                        }
                    }

                    // el vuelo termina al alejarse de su destino.
                    if (_edad > 60f &&
                        Vector2.DistanceSquared(Projectile.Center, destinoL) < 90f * 90f)
                        Projectile.Kill();
                    if (_edad > 145f) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  v6.50.54 — LA CORONA ETERNA (Everlasting Rainbow): el
                //  ancla DERIVA hacia la presa (0.5 px/t — el coro de la
                //  casa) y CATORCE plumas prismáticas espiralan alrededor:
                //  el radio ABRE (240→640 en 300 t) y CIERRA (640→240) —
                //  la jaula que respira, girando siempre (0.024 rad/t).
                //  Cada pluma corta por contacto (radio 26 — manual).
                // =============================================================
                case EstiloCoronaEterna:
                {
                    Projectile.velocity = Vector2.Zero;
                    Player presaC = Presa();
                    if (presaC != null)
                    {
                        Vector2 dirC = (presaC.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                        Projectile.Center += dirC * 0.5f;
                    }

                    // EL RADIO DE LA CORONA (abre y cierra — 300 t cada lado).
                    float cicloC = _edad % 600f;
                    float radioC = cicloC < 300f
                        ? MathHelper.Lerp(240f, 640f, cicloC / 300f)
                        : MathHelper.Lerp(640f, 240f, (cicloC - 300f) / 300f);
                    float giroC = _edad * 0.024f;

                    // EL CORTE de las plumas (cada 4 t — autoridad).
                    if (Main.netMode != NetmodeID.MultiplayerClient && ((int)_edad % 4) == 0)
                    {
                        for (int pl = 0; pl < Main.maxPlayers; pl++)
                        {
                            Player p = Main.player[pl];
                            if (p == null || !p.active || p.dead) continue;
                            for (int f = 0; f < 14; f++)
                            {
                                float angF = giroC + f * MathHelper.TwoPi / 14f;
                                Vector2 pluma = Projectile.Center + new Vector2(
                                    MathF.Cos(angF) * radioC,
                                    MathF.Sin(angF) * radioC * 0.85f);
                                if (Vector2.Distance(p.Center, pluma) < 26f)
                                {
                                    HerirJugador(p, (int)(Projectile.damage * 0.95f), pluma);
                                    break;
                                }
                            }
                        }
                    }

                    if (_edad > 655f) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  v6.50.57/.60 — EL SOL DEL DIOS (el ataque especial pedido:
                //  «debe convertirse en solo y luego lanzar ese sol al
                //  jugador… el sol que lanza el jefe debe perseguir
                //  lentamente al jugador, tener gravedad y que cresca al
                //  menos 5 a 10 veces su tamañao al convertirse en gigante
                //  roja y explotar en luz, bruma y formas, la cual hace
                //  daño»). v6.50.60 — LA CURA DEL «NO LO HACE REALMENTE»
                //  (tres rondas de letra: el sol salía flojo, perdía a la
                //  presa y explotaba en el vacío): el cañonazo VUELVE con
                //  rabia (11 px/t), la caza es IMPARABLE (giro 0.11/t —
                //  converge en ~0.4 s — y crucero 9.5: MÁS RÁPIDO que
                //  cualquier carrera, con EL RELEVO a 12 px/t si algo lo
                //  dejó lejos) y LA ESPOLETA DE PROXIMIDAD: a menos de
                //  240 px de la presa LA GIGANTE EMPIEZA YA — el sol se
                //  hincha ×6 CERCA de ti y DETONA CERCA de ti. CUATRO
                //  ACTOS:
                //
                //  ACTO 1 · LA ASUNCIÓN (0-50 t): el sol NACE EN el jefe
                //  (ai[2] = su whoAmI) y lo VISTE — crece de una chispa a
                //  un sol entero blanco-dorado MIENTRAS el dios se detiene:
                //  el jefe SE CONVIERTE en sol. Inofensivo (es su propio
                //  nacimiento) y TODAS las pantallas lo ven cabalgar.
                //
                //  ACTO 2 · EL LANZAMIENTO (t=51): el sol se DESPRENDE del
                //  dios rumbo a la presa A TODO (11 px/t — el cañonazo de
                //  luz de verdad: «que debe lanzar» se LANZA) con el
                //  estampido — el jefe retrocede por el retroceso.
                //
                //  ACTO 3 · EL VUELO (51-380 t o hasta la espolote): CAZA
                //  IMPARABLE (giro Lerp 0.11/t, crucero 9.5 px/t — por
                //  ENCIMA de la carrera máxima con botas; el RELEVO 12
                //  px/t a >700 px) y GRAVITA como comba menor (+0.008/t
                //  — el peso se SIENTE sin frenar la caza). Tocarlo quema
                //  (hostil, contacto honesto). LA ESPOLETE: <240 px de la
                //  presa → LA GIGANTE, YA.
                //
                //  ACTO 4 · LA GIGANTE ROJA (60 t) y LA EXPLOSIÓN: el sol
                //  se HINCHA ×6 (96 → ~590 px de núcleo), BLANCO-DORADO →
                //  ROJO BRASA, temblor de rayos y trueno grave — y DETONA:
                //  LA LUZ que inunda (4.2/2.4/1.4), LA BRUMA (120 nubes en
                //  tres capas + el goteo residual), LAS FORMAS (estrellas
                //  giratorias y esquirlas que vuelan) y EL DAÑO honesto en
                //  640 px (HerirJugador — el cauce del motor: iframes y
                //  escudos respetados). Tras la detonación: 50 t de bruma
                //  muriendo y Kill.
                // =============================================================
                case EstiloSolJefe:
                {
                    // === ACTO 4 · LA GIGANTE ROJA (por espolote o reloj) ===
                    if (_solGigante)
                    {
                        _tickGigante++;
                        // v6.50.60 — LA HINCHAZÓN ES CORTA Y PERSIGUE: 30 t
                        // (una media segunda — la simulación de la casa lo
                        // probó: con 60 t el corredor escapaba del radio de
                        // la explosión; con 30 t y la deriva hacia la presa
                        // el estallido PEGA a toda velocidad de juego real).
                        float g = MathHelper.Clamp(_tickGigante / 30f, 0f, 1f);
                        float ease = g * g * (3f - 2f * g);   // suave al hincharse

                        // EL HINCHAZÓN ×6 (96 → ~590 px de núcleo: «5 a 10
                        // veces su tamaño» — la talla del motor sigue con
                        // honestidad: el NÚCLEO que quema, no el velo).
                        float esc = 1f + 5.0f * ease;
                        Projectile.scale = esc;
                        int talla = Math.Min(300, (int)(96f * esc));
                        if (talla != Projectile.width)
                        {
                            Vector2 cG = Projectile.Center;
                            Projectile.Resize(talla, talla);
                            Projectile.Center = cG;
                        }

                        // LA GIGANTE SE ALZA PERO NO SE RINDE: deriva suave
                        // hacia la presa mientras se hincha (el sol moribundo
                        // SE ACERCA para morir — jamás estalla en el vacío).
                        Player presaG = Presa();
                        if (presaG != null)
                        {
                            Vector2 haciaG = (presaG.Center - Projectile.Center)
                                .SafeNormalize(Vector2.UnitY);
                            Vector2 objetivoG = haciaG * 4f + new Vector2(0f, -0.6f);
                            Projectile.velocity = Vector2.Lerp(Projectile.velocity, objetivoG, 0.08f);
                        }
                        else
                        {
                            Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                                new Vector2(0f, -1.1f), 0.06f);
                        }
                        Projectile.rotation += 0.028f;
                        Projectile.hostile = g < 1f;   // quema mientras crece

                        // EL TRUENO DEL HINCHAZÓN (una vez).
                        if (_tickGigante == 1 && !Main.dedServ)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item117.WithPitchOffset(-0.45f), Projectile.Center);
                            OndaLib.Kick(7f, 26);
                        }

                        // LA LUZ SE ENROJECE (blanco-dorado → brasa).
                        Lighting.AddLight(Projectile.Center,
                            new Vector3(
                                2.4f + 1.8f * ease,
                                1.9f - 0.6f * ease,
                                1.3f - 0.7f * ease));

                        // LA EXPLOSIÓN (a los 30 t de gigante): LUZ + BRUMA
                        // + FORMAS + DAÑO — CERCA DE LA PRESA (la espolote
                        // lo garantizó: nació a <240 px de ella).
                        if (_tickGigante >= 30 && !_solExploto)
                        {
                            _solExploto = true;
                            // EL ESTALLIDO (una sola vez, en TODAS las
                            // máquinas — visual) + EL DAÑO (solo la
                            // autoridad — el cauce honesto de la casa).
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item74, Projectile.Center);
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item122.WithPitchOffset(0.35f), Projectile.Center);
                            OndaLib.Kick(13f, 28);
                            if (!Main.dedServ)
                            {
                                for (int dX = 0; dX < 90; dX++)
                                {
                                    float angX = Main.rand.NextFloat(MathHelper.TwoPi);
                                    Dust dX2 = Dust.NewDustPerfect(Projectile.Center,
                                        dX % 3 == 0 ? DustID.Torch : DustID.GoldFlame,
                                        new Vector2(MathF.Cos(angX), MathF.Sin(angX)) *
                                        Main.rand.NextFloat(4f, 13f),
                                        220,
                                        dX % 3 == 0
                                            ? new Color(255, 120, 50)
                                            : new Color(255, 235, 190), 1.6f);
                                    dX2.noGravity = true;
                                }

                                // v6.50.58 — LA BRUMA DE LA ESTRELLA (la
                                // letra: «necesitan mucha bruma al explotar
                                // ya que la explocion de una estrella libera
                                // mucha energie y polvo»): 120 NUBES en
                                // TRES CAPAS — el VELO interior (denso y
                                // cálido, deriva lenta), el FRENTE medio (el
                                // muro que empuja la onda) y el POLVO
                                // exterior (la ceniza que vuela) — la
                                // estrella se DESHACE en humo.
                                for (int nB = 0; nB < 120; nB++)
                                {
                                    int capaB = nB % 3;
                                    float angN = Main.rand.NextFloat(MathHelper.TwoPi);
                                    float velN = capaB == 0
                                        ? Main.rand.NextFloat(0.6f, 2.0f)
                                        : capaB == 1
                                            ? Main.rand.NextFloat(2.2f, 5.5f)
                                            : Main.rand.NextFloat(5.5f, 10.5f);
                                    float escN = capaB == 0
                                        ? Main.rand.NextFloat(3.6f, 5.8f)
                                        : capaB == 1
                                            ? Main.rand.NextFloat(2.4f, 4.2f)
                                            : Main.rand.NextFloat(1.3f, 2.6f);
                                    Color cN = capaB == 0
                                        ? new Color(255, 196, 138)
                                        : capaB == 1
                                            ? new Color(232, 128, 78)
                                            : new Color(176, 128, 108);
                                    Dust nube = Dust.NewDustPerfect(Projectile.Center,
                                        DustID.Smoke,
                                        new Vector2(MathF.Cos(angN), MathF.Sin(angN)) * velN,
                                        Main.rand.Next(80, 130), cN, escN);
                                    nube.noGravity = true;
                                }
                            }
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float radioX = 640f;
                                int danoX = Math.Max(1, (int)(Projectile.damage * 1.15f));
                                for (int ip = 0; ip < Main.maxPlayers; ip++)
                                {
                                    Player pX = Main.player[ip];
                                    if (pX == null || !pX.active || pX.dead) continue;
                                    if (Vector2.Distance(pX.Center, Projectile.Center) > radioX)
                                        continue;
                                    HerirJugador(pX, danoX, Projectile.Center);
                                }
                            }
                            Projectile.hostile = false;
                            Projectile.netUpdate = true;
                        }

                        if (_solExploto)
                        {
                            // LA BRUMA RESIDUAL a RÍO (v6.50.58: 3 nubes por
                            // tick — la nube que sigue goteando mientras la
                            // luz muere: la estrella DESHACIÉNDOSE en polvo).
                            if (!Main.dedServ)
                            {
                                for (int rB = 0; rB < 3; rB++)
                                {
                                    float angB2 = Main.rand.NextFloat(MathHelper.TwoPi);
                                    Dust bruma = Dust.NewDustPerfect(
                                        Projectile.Center + new Vector2(
                                            MathF.Cos(angB2), MathF.Sin(angB2)) *
                                            Main.rand.NextFloat(60f, 420f),
                                        DustID.Smoke,
                                        new Vector2(MathF.Cos(angB2), MathF.Sin(angB2)) * 1.1f,
                                        120, new Color(255, 150, 80), 2.6f);
                                    bruma.noGravity = true;
                                }
                            }
                            // LA LUZ DE LA EXPLOSIÓN muriendo en grande (50 t
                            // de agonía — y el Kill: la estrella ya pagó).
                            float apX = Math.Max(0f,
                                (80 - _tickGigante) / 50f);
                            Lighting.AddLight(Projectile.Center,
                                new Vector3(4.2f, 2.4f, 1.4f) * apX);
                            if (_tickGigante >= 80) Projectile.Kill();
                        }
                        break;
                    }

                    // === ACTO 1 · LA ASUNCIÓN (0-50 t): cabalga y crece ===
                    if (_edad <= 50f)
                    {
                        int quienSol = (int)Projectile.ai[2];
                        if (quienSol >= 0 && quienSol < Main.maxNPCs &&
                            Main.npc[quienSol] != null && Main.npc[quienSol].active)
                        {
                            Projectile.Center = Main.npc[quienSol].Center;
                            Projectile.velocity = Vector2.Zero;
                        }
                        Projectile.hostile = false;   // su nacimiento no quema
                        Projectile.scale = 0.30f + 0.70f * (_edad / 50f);

                        // LA LUZ QUE SUBE mientras el dios se convierte.
                        float asc = _edad / 50f;
                        Lighting.AddLight(Projectile.Center,
                            new Vector3(1.2f, 1.05f, 0.75f) * (0.5f + asc));

                        if (_edad >= 49f && !Main.dedServ)
                        {
                            // EL ESTAMPIDO DEL CAÑONAZO.
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item122.WithPitchOffset(-0.35f), Projectile.Center);
                            OndaLib.Kick(10f, 20);
                        }
                        break;
                    }

                    // === ACTO 2/3 · EL LANZAMIENTO y LA CAZA IMPARABLE ===
                    Projectile.hostile = true;

                    // EL LANZAMIENTO (una sola vez, determinista por edad):
                    // v6.50.60 — EL CAÑONAZO DE VERDAD (la letra: «que debe
                    // lanzar»): 11 px/t rumbo a la presa — el disparo que SE
                    // VE (la .59 lo ablandó a 4.8 y el usuario siguió viendo
                    // «que no lanza»; la caza firme se encarga del resto).
                    if (_edad <= 51f)
                    {
                        Vector2 rumbo = new Vector2(0f, -1f);
                        Player presaS = Presa();
                        if (presaS != null)
                            rumbo = (presaS.Center - Projectile.Center)
                                .SafeNormalize(Vector2.UnitY);
                        Projectile.velocity = rumbo * 11f;
                        Projectile.netUpdate = true;
                    }

                    // v6.50.60 — LA CAZA QUE NO PIERDE, POR FIN: giro al
                    // DOBLE del .59 (Lerp 0.11/t — converge en ~0.4 s),
                    // crucero 10.5 px/t (POR ENCIMA de la carrera máxima con
                    // botas: la línea recta NO te salva), EL RELEVO 14 px/t
                    // a >500 px (montura/gancho/asfalto: remonta SIEMPRE)
                    // y el peso como comba menor (+0.008: se siente, no
                    // frena) — SIMULADO por la casa: botas 7 px/t → el sol
                    // estalla a 270 px de tu espalda; 9 px/t → 330; 11 px/t
                    // (montura veloz) → el filo del radio. La espolote de
                    // proximidad (<240 px) enciende la gigante — el sol
                    // NUNCA más estalla en el vacío.
                    Player presaV = Presa();
                    Vector2 dirSol = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
                    if (presaV != null)
                    {
                        float distSol = Vector2.Distance(presaV.Center, Projectile.Center);
                        if (distSol < 240f)
                        {
                            // LA ESPOLETE: la gigante, YA (se hincha CERCA y
                            // estalla CERCA — el ataque se SIENTE).
                            _solGigante = true;
                            _tickGigante = 0;
                            Projectile.netUpdate = true;
                            break;
                        }
                        Vector2 haciaSol = (presaV.Center - Projectile.Center)
                            .SafeNormalize(dirSol);
                        dirSol = Vector2.Lerp(dirSol, haciaSol, 0.11f);  // EL GIRO FIRME
                        dirSol.Y += 0.008f;                              // EL PESO (comba menor)
                        dirSol = dirSol.SafeNormalize(dirSol);

                        // LOS TRES RITMOS DE LA CAZA.
                        float objetivoSol = distSol > 500f ? 14.0f   // EL RELEVO: remonta
                            : distSol < 220f ? 7.5f                   // CERCA: pesa, se esquiva
                            : 10.5f;                                  // EL CRUCERO: caza
                        float rapidoSol = MathHelper.Lerp(
                            Projectile.velocity.Length(), objetivoSol, 0.12f);
                        Projectile.velocity = dirSol * rapidoSol;
                    }
                    else
                    {
                        dirSol.Y += 0.05f;   // sin presa: el peso manda
                        dirSol = dirSol.SafeNormalize(dirSol);
                        Projectile.velocity = dirSol * Math.Max(
                            2f, Projectile.velocity.Length() - 0.05f);
                    }
                    Projectile.rotation += 0.012f;

                    // LA ESTELA DE FUEGO BLANCO.
                    if (!Main.dedServ && Main.rand.NextBool(2))
                    {
                        Dust dS = Dust.NewDustPerfect(
                            Projectile.Center + new Vector2(
                                Main.rand.NextFloat(-26f, 26f),
                                Main.rand.NextFloat(-26f, 26f)),
                            DustID.GoldFlame,
                            new Vector2(Main.rand.NextFloat(-0.6f, 0.6f),
                                Main.rand.NextFloat(-1.2f, -0.3f)),
                            190, new Color(255, 248, 220), 0.8f);
                        dS.noGravity = true;
                    }

                    // LA LUZ DEL SOL EN VUELO.
                    Lighting.AddLight(Projectile.Center,
                        new Vector3(2.1f, 1.9f, 1.35f));

                    // MP: la estrella respira por el cable (cada 15 t —
                    // la caza se ve VIVA en las pantallas remotas).
                    if ((Projectile.timeLeft % 15) == 0) Projectile.netUpdate = true;

                    // LA RED DE SEGURIDAD DEL RELOJ: si la caza se eternizó
                    // (380 t de vuelo), la gigante por tiempo — igual que
                    // la espolote, cerca o lejos: la estrella MUERE explotando.
                    if (_edad >= 380f)
                    {
                        _solGigante = true;
                        _tickGigante = 0;
                        Projectile.netUpdate = true;
                    }
                    break;
                }
            }

            // La luz del diente (el color de su dueño).
            Vector3 luz = Estilo switch
            {
                EstiloPuaSagrario or EstiloCoroCristal => new Vector3(0.20f, 0.42f, 0.55f),
                EstiloViroteVacio => new Vector3(0.14f, 0.30f, 0.32f),
                EstiloFlechaEstelar or EstiloMinaEstelar or EstiloEstrellaFugaz
                    or EstiloEstallidoMina => new Vector3(0.48f, 0.30f, 0.10f),
                EstiloTajoPortador or EstiloCuchillaOrbit or EstiloCorteRealidad
                    => new Vector3(0.45f, 0.16f, 0.07f),
                EstiloNubeNebulosa => new Vector3(0.20f, 0.10f, 0.34f),
                EstiloRunaMemorizada or EstiloPernoEstelar => (Par >= 2f
                    ? new Vector3(0.12f, 0.03f, 0.20f)   // v6.50.117 — el perno del GEMELO OSCURO: brasa violeta tenue (su sol no alumbra)
                    : new Vector3(0.38f, 0.28f, 0.10f)),
                EstiloPilarAparicion => Vector3.Zero,   // su luz la pone SU caso (la columna doble)
                EstiloRelojGigante => Vector3.Zero,      // su luz la pone SU renderer (la del bastón)
                EstiloCoroJefe => new Vector3(0.42f, 0.32f, 0.14f),
                EstiloTelarJefe => new Vector3(0.26f, 0.26f, 0.44f),
                EstiloDecretoJefe => Vector3.Zero,       // su caso pone la suya (el eclipse)
                EstiloEstallidoRadiante => Vector3.Zero, // v6.50.53 — su caso pone la suya (el flash que inunda)
                EstiloMuerteFinal => Vector3.Zero,        // v6.50.80 — su caso pone la suya (LA INUNDACIÓN de la muerte)
                EstiloDanzaSolar => new Vector3(1.05f, 0.95f, 0.62f),   // v6.50.54 — la rueda alumbra
                EstiloLanzaEterna => new Vector3(0.50f, 0.45f, 0.24f),  // v6.50.54 — el filo dorado
                EstiloCoronaEterna => new Vector3(0.55f, 0.50f, 0.30f), // v6.50.54 — el prisma suave
                EstiloSolJefe => Vector3.Zero,       // v6.50.57 — su caso pone la suya (el sol arde por actos)
                _ => new Vector3(0.3f, 0.3f, 0.3f),
            };
            Lighting.AddLight(Projectile.Center, luz);
        }

        // ==================================================================
        //  LAS DETONACIONES (lógica del juego — polvos y cuerpos del motor)
        // ==================================================================

        // ==================================================================
        //  v6.50.45 — LOS CORTES HONESTOS A JUGADORES (el cauce del motor:
        //  Hurt respeta iframes, escudos y esquiva — nada de daño crudo).
        //  Solo la AUTORIDAD hiere (SP/servidor); los remotos simulan el
        //  visual con su propia IA (netImportant, el patrón del coro del
        //  bastón).
        // ==================================================================

        /// <summary>El corte a un jugador (desde un punto — la dirección
        /// del golpe sale sola).</summary>
        private void HerirJugador(Player p, int dmg, Vector2 desde)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (p == null || !p.active || p.dead || dmg <= 0) return;
            int dir = p.Center.X < desde.X ? -1 : 1;
            // tML 2026.07: ByProjectile(int projectile, int damage) — el
            // daño entra en la RAZÓN de la muerte (lo que dice el log).
            p.Hurt(Terraria.DataStructures.PlayerDeathReason.ByProjectile(
                Projectile.whoAmI, dmg), dmg, dir);
        }

        /// <summary>EL PESO DEL RELOJ: daño + HUNDIR a los jugadores en el
        /// radio (la gravedad aumentada del tiempo).</summary>
        private void PesarJugadores(float mult, float hundimiento, float radio)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.dead) continue;
                if (Vector2.Distance(p.Center, Projectile.Center) > radio) continue;
                HerirJugador(p, (int)(Projectile.damage * mult), Projectile.Center);
                p.velocity.Y += hundimiento;
            }
        }

        /// <summary>Punto-en-polígono por ray-casting (el veredicto del
        /// telar — el MISMO algoritmo del bastón).</summary>
        private static bool PuntoEnPoligono(Vector2 punto, Vector2[] vertices, int n)
        {
            if (n < 3) return false;
            bool dentro = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((vertices[i].Y > punto.Y) != (vertices[j].Y > punto.Y)) &&
                    (punto.X < (vertices[j].X - vertices[i].X) * (punto.Y - vertices[i].Y) /
                        (vertices[j].Y - vertices[i].Y) + vertices[i].X))
                    dentro = !dentro;
            }
            return dentro;
        }

        /// <summary>La púa estalla en ESQUIRLAS radiales (el coloso las escupe).</summary>
        private void DetonarPua()
        {
            for (int i = 0; i < 5; i++)
            {
                float ang = i * MathHelper.TwoPi / 5f + Seed * 0.01f;
                Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 7f;
                // v6.50.1 — FIX (MP ×N+1): solo la autoridad spawnnea
                // (los clientes corren esta IA — la esquirla se difunde sola).
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                        Projectile.Center, vel,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(Projectile.damage * 0.55f), 2f, Main.myPlayer,
                        EstiloPernoEstelar, 0f, Seed + i * 7);
                }
            }
            for (int d = 0; d < 8; d++)
            {
                int idx = Dust.NewDust(Projectile.Center, 12, 12, DustID.IceTorch,
                    -Main.rand.NextFloat(2f, 5f), -Main.rand.NextFloat(1f, 4f));
                Main.dust[idx].noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item30, Projectile.Center);
        }

        /// <summary>
        /// LA MINA DETONA: el ARCO VOLTAICO a la presa (visual de
        /// StormLib) + el CUERPO del estallido (proyectil con hitbox
        /// honesta — el daño lo pone el motor por colisión, no a mano).
        /// </summary>
        private void DetonarMina(Player presa)
        {
            // EL CUERPO: 20 t de radio honesto.
            // v6.50.1 — FIX (MP ×N+1): solo la autoridad spawnnea
            // (los clientes corren esta IA — el cuerpo se difunde solo).
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                    Projectile.damage, 4f, Main.myPlayer,
                    EstiloEstallidoMina, 0f, Seed);
            }

            for (int d = 0; d < 10; d++)
            {
                int idx = Dust.NewDust(Projectile.Center, 14, 14, DustID.YellowStarDust,
                    Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 1f));
                Main.dust[idx].noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item94, Projectile.Center);
        }

        /// <summary>La estrella fugaz revienta en polvo estelar.</summary>
        private void DetonarFugaz()
        {
            for (int d = 0; d < 8; d++)
            {
                int idx = Dust.NewDust(Projectile.Center, 12, 12, DustID.YellowStarDust,
                    Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-3f, 1f));
                Main.dust[idx].noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Projectile.Kill();
        }


    }
}
