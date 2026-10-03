using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.VFX;

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
    ///      de suelo telegrafiada antes del impacto (OndaLib.Ground).
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
    public class AtaqueJefeProjectile : ModProjectile
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
        public const int EstiloBolaFinal = 23;     // v6.50.56 — LA BOLA FINAL (la muerte del dios: su ÚLTIMO aliento — una bola de energía como el proyectil sol pero BLANCO-DORADA y con MUCHO brillo)

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
        private float EscalaEstallido => 1.15f + 0.06f * Math.Max(0, FaseEstallido - 1);

        // === EL ESTADO (por estilo) ===
        private float _edad;
        private Vector2 _centroOrbita;   // el anillo del coro / la runa
        private bool _lanzado;           // esquirla/cuchilla ya disparada
        private bool _clavada;           // la púa ya se hundió en el suelo
        private Vector2 _posAnterior;    // la cola del virote
        private int _ticksDisolver;      // v6.50.44 — el fade del pilar de la aparición

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
                    // Fargo's — y solo el VUELO corta).
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

                case EstiloBolaFinal:
                    // v6.50.56 — LA BOLA FINAL: el último aliento del dios
                    // — 7 s de fuego blanco-dorado persiguiendo a su
                    // asesino (la autocuración de la IA reconstruye talla/
                    // hostil/daño: el msg 27 no lleva este OnSpawn).
                    Projectile.timeLeft = 420;
                    Projectile.width = 88;
                    Projectile.height = 88;
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

            // v6.50.56 — LA BOLA FINAL: la caja YA nace a 88×88 (la talla del
            // corazón blanco) — cero inflado: golpea lo que SE VE.
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

        /// <summary>
        /// v6.50.47 — LAS ESTRUCTURAS DE CAMPO DEL JEFE DIBUJAN DETRÁS DE
        /// LAS CRIATURAS (el fix de «algunos ataques del jefe hacen que
        /// los NPC sean semitransparentes»). EL RELOJ GIGANTE, EL CORO,
        /// EL TELAR y EL DECRETO son EDIFICIOS y TRAMPAS que viven
        /// SEGUNDOS en pantalla: dibujados en el pase normal de
        /// proyectiles (DESPUÉS de los NPCs), sus velos gigantes —
        /// la MASA alpha-blend del reloj («el polvo que OCLUYE») y las
        /// capas aditivas de arena/anillos/jaula — cubrían a los NPC del
        /// pueblo y los volvían FANTASMAS (vistos a través del ataque).
        /// REGISTRADOS en behindNPCs dibujan en el pase de
        /// DrawCachedProjs(DrawCacheProjsBehindNPCs) — DESPUÉS de los
        /// tiles, ANTES de TODOS los NPCs y del jugador (verificado en el
        /// decompile del tML 2026.07.3.0): las criaturas SIEMPRE sólidas
        /// encima, las estructuras de luz debajo. Los proyectiles RÁPIDOS
        /// (pernos, flechas, tajos) siguen en el pase normal — pasan POR
        /// DELANTE como toda bala de Terraria.
        /// </summary>
        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            if (Estilo == EstiloRelojGigante || Estilo == EstiloCoroJefe ||
                Estilo == EstiloTelarJefe || Estilo == EstiloDecretoJefe ||
                Estilo == EstiloDanzaSolar)
                behindNPCs.Add(index);
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

            // v6.50.36 — LA COLUMNA DEL JUICIO: la forma LARGA (la
            // autocuración de la casa: OnSpawn no corre en los clientes
            // remotos — la forma se reconstruye desde lo que SÍ viaja).
            if (Estilo == EstiloColumnaJuicio && Projectile.height < 100)
            {
                Vector2 c = Projectile.Center;
                Projectile.width = 56;
                Projectile.height = 210;
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

            // v6.50.56 — LA BOLA FINAL: la autocuración de la casa (talla,
            // hostil y daño — el msg 27 no lleva el OnSpawn; ai[1] lleva el
            // daño que el jefe le sopló al morir).
            if (Estilo == EstiloBolaFinal)
            {
                if (Projectile.width < 88)
                {
                    Vector2 cB = Projectile.Center;
                    Projectile.width = 88;
                    Projectile.height = 88;
                    Projectile.Center = cB;
                }
                Projectile.hostile = true;
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
                    // 45 t de CAÍDA LENTA (la línea de luz descendiendo —
                    // EL TELEGRAPH del juicio) y luego LA ACELERACIÓN:
                    // 52 px/t atravesando el mundo hasta el fondo.
                    if (_edad < 45)
                    {
                        Projectile.velocity = new Vector2(0f, 7f);
                        Projectile.ai[1] = 0f;
                    }
                    else
                    {
                        if (Projectile.ai[1] < 1f)
                        {
                            Projectile.ai[1] = 1f;
                            Projectile.velocity = new Vector2(0f, 52f);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122,
                                Projectile.Center);
                        }
                        // LA ESTELA de la caída (el rastro de la columna).
                        if (!Main.dedServ && ((int)_edad % 4) == 0)
                        {
                            int idx = Dust.NewDust(Projectile.Center, 40, 40,
                                DustID.GoldFlame,
                                Main.rand.NextFloat(-2f, 2f), -Main.rand.NextFloat(1f, 4f));
                            Main.dust[idx].noGravity = true;
                        }
                    }
                    if (Projectile.Center.Y > (Main.maxTilesY - 30) * 16f ||
                        _edad > 200)
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
                    if (Par == 1f && _edad > 8 && presa != null &&
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

                    if (_edad > 620f) Projectile.Kill();
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
                {
                    // CLAVADO donde nació (estalló donde estaba el jefe) —
                    // la autocuración del inicio ya puso hostil/daño de la
                    // ventana de la ONDA (los primeros 12 t: el radio 600
                    // TELEGRAFEADO por el aro de la carga).
                    Projectile.velocity = Vector2.Zero;

                    // LA LUZ QUE INUNDA EL MUNDO (el flash de la imagen: la
                    // escena entera queda BAÑADA en blanco-oro — 2,5 s de
                    // día dentro de la pelea) — VIVO, no el 0.5 plano del
                    // SetDefaults: la luz de un dios que se enciende (y que
                    // CRECE con la fase — la escala de la .54).
                    float luzE = Math.Max(0f, 1f - _edad / 150f);
                    Lighting.AddLight(Projectile.Center,
                        new Vector3(2.4f, 2.1f, 1.5f) * luzE * EscalaEstallido);

                    // EL NACIMIENTO (t=1): el estampido + LAS CHISPAS QUE
                    // VUELAN (los streaks radiales de la imagen — cada
                    // cliente ve las suyas: es polvo, pura decoración).
                    if (_edad <= 1f)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, Projectile.Center);
                        OndaLib.Kick(13f, 26);
                        for (int i = 0; i < 42; i++)
                        {
                            float ang = i * MathHelper.TwoPi / 42f +
                                Main.rand.NextFloat(-0.06f, 0.06f);
                            float v = Main.rand.NextFloat(6f, 17f);
                            Dust d = Dust.NewDustPerfect(Projectile.Center,
                                DustID.GoldFlame,
                                new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * v);
                            d.noGravity = true;
                            d.scale = Main.rand.NextFloat(1.1f, 1.9f);
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
                //  inofensivos (la regla Fargo's: lo que aún no daña se ve
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
                //  v6.50.56 — LA BOLA FINAL (el último aliento del dios):
                //  la bola de energía como EL PROYECTIL SOL pero
                //  BLANCO-DORADA y con MUCHO brillo (la letra: «al final
                //  de la muerte del jefe… debe lanzar un ataque de brillo
                //  aun mayor creando una bola de energia similar al
                //  proyectil sol pero de color blanco dorado y con mucho
                //  brillo»). Nace del cuerpo del jefe al morir, persigue
                //  a su asesino con suavidad (se esquiva: es un adiós, no
                //  una ejecución), ALUMBRA como un pequeño sol y al
                //  apagarse suelta un ÚLTIMO destello.
                // =============================================================
                case EstiloBolaFinal:
                {
                    // LA CARRERA SERENA: deriva lenta + homing suave a la
                    // presa (el sol del bastón también gravita — tope 5).
                    Player presaB = Presa();
                    if (presaB != null)
                    {
                        Vector2 hacia = presaB.Center - Projectile.Center;
                        float d = hacia.Length();
                        if (d > 40f)
                            Projectile.velocity += hacia / d * 0.06f;
                    }
                    float velB = Projectile.velocity.Length();
                    if (velB > 5f)
                        Projectile.velocity *= 5f / velB;
                    Projectile.rotation += 0.015f;

                    // LA LUZ QUE INUNDA (más que el estallido — es SU
                    // ataque más brillante: el cielo entero se dora).
                    float apagon = Math.Min(1f, _edad / 30f) *
                        (Projectile.timeLeft < 60f ? Projectile.timeLeft / 60f : 1f);
                    Lighting.AddLight(Projectile.Center,
                        new Vector3(2.6f, 2.35f, 1.7f) * apagon);

                    // LA ESTELA DE FUEGO BLANCO (chispas doradas cayendo
                    // del corazón — puro adorno del cliente).
                    if (!Main.dedServ && Main.rand.NextBool(3))
                    {
                        Dust dB = Dust.NewDustPerfect(
                            Projectile.Center + new Vector2(Main.rand.NextFloat(-30f, 30f),
                                Main.rand.NextFloat(-30f, 30f)),
                            DustID.GoldFlame,
                            new Vector2(Main.rand.NextFloat(-0.8f, 0.8f),
                                Main.rand.NextFloat(-1.6f, -0.4f)),
                            190, new Color(255, 248, 220), 0.9f);
                        dB.noGravity = true;
                    }

                    // EL NACIMIENTO (el estampido del aliento) y LA MUERTE
                    // (el último destello: la bola SE APAGA encendida).
                    if (_edad <= 1f && !Main.dedServ)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item122.WithPitchOffset(-0.25f), Projectile.Center);
                        OndaLib.Kick(9f, 18);
                    }
                    if (Projectile.timeLeft <= 2 && !Main.dedServ)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item122.WithPitchOffset(0.3f), Projectile.Center);
                        for (int dF = 0; dF < 26; dF++)
                        {
                            float angF = dF * MathHelper.TwoPi / 26f;
                            Dust dF2 = Dust.NewDustPerfect(Projectile.Center,
                                DustID.GoldFlame,
                                new Vector2(MathF.Cos(angF), MathF.Sin(angF)) *
                                Main.rand.NextFloat(3f, 8f),
                                190, new Color(255, 250, 230), 1.2f);
                            dF2.noGravity = true;
                        }
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
                EstiloRunaMemorizada or EstiloPernoEstelar => new Vector3(0.38f, 0.28f, 0.10f),
                EstiloPilarAparicion => Vector3.Zero,   // su luz la pone SU caso (la columna doble)
                EstiloRelojGigante => Vector3.Zero,      // su luz la pone SU renderer (la del bastón)
                EstiloCoroJefe => new Vector3(0.42f, 0.32f, 0.14f),
                EstiloTelarJefe => new Vector3(0.26f, 0.26f, 0.44f),
                EstiloDecretoJefe => Vector3.Zero,       // su caso pone la suya (el eclipse)
                EstiloEstallidoRadiante => Vector3.Zero, // v6.50.53 — su caso pone la suya (el flash que inunda)
                EstiloDanzaSolar => new Vector3(1.05f, 0.95f, 0.62f),   // v6.50.54 — la rueda alumbra
                EstiloLanzaEterna => new Vector3(0.50f, 0.45f, 0.24f),  // v6.50.54 — el filo dorado
                EstiloCoronaEterna => new Vector3(0.55f, 0.50f, 0.30f), // v6.50.54 — el prisma suave
                EstiloBolaFinal => Vector3.Zero,     // v6.50.56 — su caso pone la suya (el cielo dorado)
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

        // ==================================================================
        //  EL RENDER — cada diente con SU librería. EL FLUJO DE LA CASA:
        //  (1) los quads al BÚFER (coords de MUNDO) y su volcado propio;
        //  (2) el lote aditivo NUESTRO para las librerías de coords de
        //  PANTALLA (OndaLib/StormLib/TajoLib/LumenLib/OrbitaLib/RiftLib);
        //  (3) el lote del pase reabierto TAL CUAL (cerrado→cerrado).
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            // v6.50.45/54 — EL RELOJ GIGANTE camina por SU camino (el renderer
            //     del BASTÓN escalado ×7.5 — LA CATEDRAL DEL TIEMPO: gestiona
            //     SUS lotes enteros — la masa, el reloj y la onda de la
            //     inversión — y devuelve el lote CERRADO, el contrato v6.10).
            if (Estilo == EstiloRelojGigante)
            {
                RelojArenaRenderer.Draw(Projectile, _edad, Seed, 7.5f);   // v6.50.54 — ×7.5: la edad de arena corre desde el nacimiento (la AI ya no le resta la caída)
                VFXCore.ReabrirLoteVanilla();
                return false;
            }

            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 pos = Projectile.Center - Main.screenPosition;

                // === FASE 1 — EL BÚFER DE QUADS (coords de MUNDO) ===
                switch (Estilo)
                {
                    case EstiloPuaSagrario:
                        VFXCore.Quad(Projectile.Center, TealCristal * 0.85f,
                            new Vector2(10f, 26f), Projectile.rotation);
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.7f,
                            new Vector2(5f, 12f), Projectile.rotation);
                        break;

                    case EstiloCoroCristal:
                        VFXCore.Quad(Projectile.Center, TealCristal * 0.9f,
                            new Vector2(16f, 7f), Projectile.rotation);
                        break;

                    case EstiloViroteVacio:
                        VFXCore.Quad(Projectile.Center, TealVacio * 0.9f,
                            new Vector2(30f, 6f), Projectile.velocity.ToRotation());
                        VFXCore.Quad(Projectile.Center, VioletaVacio * 0.5f,
                            new Vector2(18f, 10f), Projectile.velocity.ToRotation());
                        break;

                    case EstiloFlechaEstelar:
                        VFXCore.Quad(Projectile.Center, AmbarEstelar * 0.9f,
                            new Vector2(24f, 5f), Projectile.rotation);
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.65f,
                            new Vector2(10f, 3f), Projectile.rotation);
                        break;

                    case EstiloMinaEstelar:
                    {
                        float pulso = 0.9f + 0.1f * MathF.Sin(t * 8f + Seed);
                        VFXCore.Quad(Projectile.Center, AmbarEstelar * (0.75f * pulso),
                            new Vector2(18f, 18f));
                        VFXCore.Quad(Projectile.Center, OroGrimorio * 0.6f,
                            new Vector2(9f, 9f));
                        break;
                    }

                    case EstiloEstrellaFugaz:
                        VFXCore.Quad(Projectile.Center, OroGrimorio * 0.95f,
                            new Vector2(20f, 20f), Projectile.rotation);
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.8f,
                            new Vector2(10f, 10f), Projectile.rotation);
                        break;

                    case EstiloCuchillaOrbit:
                        VFXCore.Quad(Projectile.Center, EmberPortador * 0.9f,
                            new Vector2(22f, 6f), Projectile.rotation);
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.6f,
                            new Vector2(9f, 3f), Projectile.rotation);
                        break;

                    case EstiloCorteRealidad:
                        // El CUERPO del desgarro lo dibuja RiftLib (fase 2);
                        // aquí solo las chispas del borde.
                        break;

                    case EstiloPernoEstelar:
                        VFXCore.Quad(Projectile.Center, LuzPrimordial * 0.9f,
                            new Vector2(16f, 16f), Projectile.rotation);
                        VFXCore.Quad(Projectile.Center, OroGrimorio * 0.6f,
                            new Vector2(8f, 8f), Projectile.rotation);
                        break;

                    case EstiloColumnaJuicio:
                    {
                        // EL MANTO DE LA COLUMNA: el velo dorado vertical
                        // (coords de MUNDO — la columna entera, más ALTA
                        // que la hitbox: la luz siempre llega antes).
                        float caida = Projectile.ai[1] >= 1f ? 1f :
                            0.35f + 0.65f * (_edad / 45f);   // crece mientras telegrafea
                        VFXCore.Quad(Projectile.Center, OroGrimorio * (0.55f * caida),
                            new Vector2(56f, 1100f));
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * (0.75f * caida),
                            new Vector2(22f, 950f));
                        break;
                    }

                    case EstiloPilarAparicion:
                    {
                        // v6.50.44 — EL PILAR DEL CIELO: la columna de luz
                        // que CAE (0-40 t: la punta desciende del cielo a la
                        // base), SE SOSTIENE mientras el jefe baja por ella
                        // y se DISUELVE al final (fade por ai[1]). TRES
                        // velos apilados (el manto dorado ancho, el cuerpo
                        // blanco y el núcleo cegador) + el CHARCO DE LUZ en
                        // la base + la PUNTA brillante mientras cae.
                        float p = Math.Min(1f, _edad / 40f);          // el descenso
                        float disolver = Projectile.ai[1] >= 1f
                            ? Math.Max(0f, 1f - _ticksDisolver / 40f) : 1f;
                        float pulso = 0.85f + 0.15f * MathF.Sin(t * 5f + Seed);
                        const float altura = 900f;

                        // el cuerpo del pilar (anclado arriba, la punta baja).
                        float punta = Projectile.Center.Y - altura * (1f - p);
                        float centroY = (Projectile.Center.Y - altura + punta) * 0.5f;
                        Vector2 centro = new Vector2(Projectile.Center.X, centroY);
                        float alto = altura * p;

                        if (alto > 4f)
                        {
                            VFXCore.Quad(centro, OroGrimorio * (0.40f * disolver * pulso),
                                new Vector2(110f, alto));
                            VFXCore.Quad(centro, BlancoCaliente * (0.55f * disolver * pulso),
                                new Vector2(46f, alto));
                            VFXCore.Quad(centro, LuzPrimordial * (0.30f * disolver),
                                new Vector2(160f, alto * 0.92f));
                            VFXCore.Quad(centro, new Color(255, 253, 244) * (0.80f * disolver),
                                new Vector2(18f, alto));
                        }

                        // EL CHARCO DE LUZ en la base (donde la columna se
                        // posa — el escenario de la pelea queda BAÑADO).
                        if (p >= 1f)
                        {
                            VFXCore.Quad(Projectile.Center, OroGrimorio * (0.50f * disolver * pulso),
                                new Vector2(170f, 34f));
                            VFXCore.Quad(Projectile.Center, BlancoCaliente * (0.45f * disolver),
                                new Vector2(80f, 16f));
                        }

                        // LA PUNTA que cae (la cabeza del destello mientras
                        // desciende — lo que se ve venir del cielo).
                        if (p < 1f)
                        {
                            Vector2 tip = new Vector2(Projectile.Center.X, punta);
                            VFXCore.Quad(tip, OroGrimorio * 0.85f, new Vector2(90f, 40f));
                            VFXCore.Quad(tip, new Color(255, 253, 244) * 0.90f,
                                new Vector2(40f, 18f));
                        }
                        break;
                    }

                    case EstiloNubeNebulosa:
                    {
                        // LAS FLORES DE HUMO: 6 puffs orbitando con Hash01.
                        for (int i = 0; i < 6; i++)
                        {
                            float h = VFXCore.Hash01(Seed, i, (int)(_edad / 40f));
                            float ang = h * MathHelper.TwoPi + t * 0.4f;
                            float r = 30f + 26f * VFXCore.Hash01(Seed, i + 40, 0);
                            Vector2 off = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * r;
                            float fade = 1f - _edad / 360f;
                            VFXCore.Quad(Projectile.Center + off,
                                NebulosaNube * fade,
                                new Vector2(34f, 30f) * (0.8f + 0.2f * MathF.Sin(t * 2f + i)));
                        }
                        break;
                    }

                    case EstiloRunaMemorizada:
                    {
                        // EL SIGILO: pentágono de cápsulas doradas + núcleo.
                        for (int i = 0; i < 5; i++)
                        {
                            float a0 = Projectile.rotation + i * MathHelper.TwoPi / 5f;
                            float a1 = Projectile.rotation + (i + 1) * MathHelper.TwoPi / 5f;
                            Vector2 p0 = Projectile.Center + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * 26f;
                            Vector2 p1 = Projectile.Center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * 26f;
                            VFXCore.Line(p0, p1, OroGrimorio * 0.55f, 3f);
                        }
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.9f,
                            new Vector2(10f, 10f));
                        break;
                    }

                    case EstiloEstallidoMina:
                    {
                        // EL CUERPO DEL ESTALLIDO: la bola de chispas (20 t).
                        float prog = _edad / 20f;
                        float r = 66f * (0.3f + 0.7f * prog);
                        for (int i = 0; i < 8; i++)
                        {
                            float ang = i * MathHelper.TwoPi / 8f + Seed * 0.01f;
                            Vector2 off = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * r;
                            VFXCore.Quad(Projectile.Center + off,
                                AmbarEstelar * (0.8f * (1f - prog)),
                                new Vector2(14f, 14f) * (1f - prog * 0.5f));
                        }
                        break;
                    }

                    // === v6.50.45 — EL TELAR: LOS HILOS de la constelación
                    //     (coords de MUNDO — la figura que se dibuja sola,
                    //     estrella a estrella, cerrándose N → 1) ===
                    case EstiloTelarJefe:
                    {
                        // v6.50.46 — LOS HILOS en ORDEN DE ÍNDICE (la jaula
                        // conecta las puntas en su orden natural, aunque el
                        // jefe las haya clavado por el SALTO de la estrella).
                        int puntasV = Par > 0.5f ? 7 : 5;
                        Vector2[] clavadasV = new Vector2[7];
                        int nV = 0;
                        for (int i = 0; i < puntasV; i++)
                            if (_estrellaClavada[i]) clavadasV[nV++] = _estrellas[i];
                        if (nV >= 2)
                        {
                            float brillo = 0.35f + 0.65f * _telarFlash;
                            Color cHilo = Color.Lerp(new Color(150, 170, 255), new Color(255, 255, 255), _telarFlash);
                            for (int i = 0; i < nV; i++)
                            {
                                int sig = (i + 1) % nV;
                                // el último hilo CIERRA la jaula solo con TODAS
                                if (sig == 0 && nV < puntasV) sig = nV - 1;
                                if (i == sig) continue;
                                VFXCore.Line(clavadasV[i], clavadasV[sig],
                                    cHilo * (0.4f * brillo), 2.4f + 2.6f * _telarFlash);
                            }
                        }
                        break;
                    }

                    // === v6.50.45 — EL DECRETO: LOS GLIFOS rúnicos cabalgando
                    //     el anillo (24 cápsulas que se RE-ESCRIBEN en cada
                    //     ejecución — el contrato del bastón) ===
                    case EstiloDecretoJefe:
                    {
                        // v6.50.46 — el radio se COMPUTA de la edad (ai[0]
                        // es el ESTILO — jamás se escribe).
                        float radioD = Math.Min(80f + 2f * _edad, 960f);
                        int beatIdx = (int)(_edad / 15f);
                        for (int i = 0; i < 24; i++)
                        {
                            float ang = i * MathHelper.TwoPi / 24f + t * 0.10f;
                            Vector2 g = Projectile.Center +
                                new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.86f) * radioD;
                            float h = VFXCore.Hash01(Seed, i, 91 + (beatIdx % 7));
                            VFXCore.Quad(g, ColorOroEclipse * (0.5f + 0.4f * h),
                                new Vector2(5f + 5f * h, 2.2f), ang + MathHelper.PiOver2);
                        }
                        break;
                    }

                    // === v6.50.53 — EL ESTALLIDO RADIANTE, FASE MUNDO (LA
                    //     IMAGEN DEL USUARIO hecha quads — TODO determinista:
                    //     Hash01 hace que TODAS las pantallas dibujen EL
                    //     MISMO estallido): LOS 44 RAYOS de largo VARIABLE
                    //     (los hay cortos al 25% y los que CRUZAN la
                    //     pantalla — cada uno con SU línea núcleo BLANCA y
                    //     SU halo que se CALIENTA con la distancia: oro →
                    //     ámbar → brasa, el degradé de la imagen), EL ANILLO
                    //     SEGMENTADO (14
                    //     EMISORES brillantes creciendo hacia afuera — NO un
                    //     aro continuo: la firma de la imagen) y LA CRUZ
                    //     ANAMÓRFICA (el bloom que estira H y V — el
                    //     lens-flare del cine) ===
                    case EstiloEstallidoRadiante:
                    {
                        // EL TEMPO: crece (0-14 t), ARDE (14-55) y se
                        // disuelve (55-150) — la rotación LENTA de la
                        // imagen (~0.5 RPM).
                        float crec = Math.Min(1f, _edad / 14f);
                        float fade = _edad < 55f ? 1f : Math.Max(0f, 1f - (_edad - 55f) / 95f);
                        float giro = _edad * 0.0045f;
                        Vector2 cE = Projectile.Center;

                        // === (a) LOS RAYOS (el corazón de la imagen) ===
                        for (int i = 0; i < 44; i++)
                        {
                            float h0 = VFXCore.Hash01(Seed, i, 10);
                            float h1 = VFXCore.Hash01(Seed, i, 11);
                            float h2 = VFXCore.Hash01(Seed, i, 12);
                            float h3 = VFXCore.Hash01(Seed, i, 13);
                            float ang = giro + i * MathHelper.TwoPi / 44f +
                                (h0 - 0.5f) * (MathHelper.TwoPi / 44f) * 1.15f;
                            // el largo: de 250 (el rayo corto) a 980 (el que
                            // cruza la pantalla entera) — la VARIANZA de la
                            // imagen, no un abanico matemático (v6.50.54: ×ESCALA
                            // — «un poco más grande» con la fase).
                            float largo = (250f + 730f * h1) * crec * EscalaEstallido;
                            // la vida propia: cada rayo muere a SU tiempo.
                            float vidaR = 75f + 45f * h2;
                            float alfaR = fade * (_edad < vidaR ? 1f :
                                Math.Max(0f, 1f - (_edad - vidaR) / 22f));
                            if (alfaR <= 0.02f || largo < 8f) continue;
                            Vector2 dirR = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
                            // LA LÍNEA NÚCLEO (blanca — la doble línea de la
                            // imagen: cada rayo tiene su corazón fino).
                            VFXCore.Quad(cE + dirR * (46f + largo * 0.5f),
                                BlancoCaliente * (0.85f * alfaR),
                                new Vector2(largo, 3.2f + 2.6f * h3), ang);
                            // EL HALO (más corto, más ancho — y CALIENTE:
                            // el degradé blanco→oro→ámbar→brasa de la imagen).
                            Color cHalo = Color.Lerp(OroGrimorio,
                                Color.Lerp(AmbarEstelar, EmberPortador, h3 * 0.7f),
                                0.35f + 0.45f * h3);
                            VFXCore.Quad(cE + dirR * (34f + largo * 0.34f),
                                cHalo * (0.40f * alfaR),
                                new Vector2(largo * 0.68f, 12f + 9f * h0), ang);
                        }

                        // === (b) EL ANILLO SEGMENTADO (14 EMISORES creciendo
                        //     hacia afuera con su flicker — 150 → 760·ESCALA px) ===
                        float rAnillo = MathHelper.Lerp(150f, 760f,
                            Math.Min(1f, _edad / 55f)) * EscalaEstallido;
                        float alfaA = _edad < 70f ? 1f :
                            Math.Max(0f, 1f - (_edad - 70f) / 45f);
                        if (alfaA > 0.02f)
                        {
                            int beatE = (int)(_edad / 6f);   // el flicker segmentado
                            for (int i = 0; i < 14; i++)
                            {
                                float angE = giro * 1.6f + i * MathHelper.TwoPi / 14f;
                                float hE = VFXCore.Hash01(Seed, i, 60 + (beatE % 5));
                                Vector2 e = cE + new Vector2(MathF.Cos(angE), MathF.Sin(angE)) * rAnillo;
                                VFXCore.Quad(e,
                                    BlancoCaliente * (0.80f * alfaA * (0.7f + 0.3f * hE)),
                                    new Vector2(26f + 10f * hE, 26f + 10f * hE));
                                VFXCore.Quad(e, OroGrimorio * (0.45f * alfaA),
                                    new Vector2(58f, 58f));
                            }
                        }

                        // === (c) LA CRUZ ANAMÓRFICA (el bloom estirado en H
                        //     y V — vive solo en el AUGE, muere a los 55 t) ===
                        float alfaC = Math.Max(0f, 1f - _edad / 55f) * 0.55f;
                        if (alfaC > 0.02f)
                        {
                            VFXCore.Quad(cE, BlancoCaliente * alfaC,
                                new Vector2(1150f * crec * EscalaEstallido, 7f));
                            VFXCore.Quad(cE, BlancoCaliente * alfaC,
                                new Vector2(7f, 1150f * crec * EscalaEstallido));
                            VFXCore.Quad(cE, OroGrimorio * (alfaC * 0.6f),
                                new Vector2(760f * crec * EscalaEstallido, 16f));
                            VFXCore.Quad(cE, OroGrimorio * (alfaC * 0.6f),
                                new Vector2(16f, 760f * crec * EscalaEstallido));
                        }
                        break;
                    }

                    // === v6.50.54 — LA DANZA SOLAR (fase mundo): LA RUEDA —
                    //     seis rayos de 860 px (núcleo blanco + halo dorado —
                    //     las primitivas del Estallido) girando LENTO; cada
                    //     tanda nace TRANSLÚCIDA (alfa 0.35 — la regla Fargo)
                    //     y se ENCIENDE cuando mata ===
                    case EstiloDanzaSolar:
                    {
                        int tandaV = (int)(_edad / 175f);
                        float tEdadV = _edad % 175f;
                        float alfaT = tEdadV <= 25f
                            ? 0.35f
                            : Math.Min(1f, 0.35f + (tEdadV - 25f) / 20f * 0.65f);
                        float fadeV = _edad > 520f
                            ? Math.Max(0f, 1f - (_edad - 520f) / 35f) : 1f;
                        float giroV = _edad * 0.0125f + tandaV * 0.35f;
                        const float largoD = 860f;
                        for (int i = 0; i < 6; i++)
                        {
                            float angV = giroV + i * MathHelper.TwoPi / 6f;
                            Vector2 dirV = new Vector2(MathF.Cos(angV), MathF.Sin(angV));
                            // LA LÍNEA NÚCLEO (blanca) y EL HALO (dorado).
                            VFXCore.Quad(Projectile.Center + dirV * (60f + largoD * 0.5f),
                                BlancoCaliente * (0.70f * alfaT * fadeV),
                                new Vector2(largoD, 4.5f), angV);
                            VFXCore.Quad(Projectile.Center + dirV * (40f + largoD * 0.34f),
                                OroGrimorio * (0.30f * alfaT * fadeV),
                                new Vector2(largoD * 0.68f, 26f), angV);
                            // LA PUNTA (la cuenta de luz del borde).
                            VFXCore.Quad(Projectile.Center + dirV * (60f + largoD),
                                BlancoCaliente * (0.55f * alfaT * fadeV),
                                new Vector2(14f, 14f));
                        }
                        // EL CUBO de la rueda (el núcleo que la sostiene).
                        VFXCore.Quad(Projectile.Center,
                            BlancoCaliente * (0.55f * alfaT * fadeV),
                            new Vector2(95f, 95f));
                        VFXCore.Quad(Projectile.Center,
                            OroGrimorio * (0.30f * alfaT * fadeV),
                            new Vector2(190f, 190f));
                        break;
                    }

                    // === v6.50.54 — LA LANZA ETERNA (fase mundo): el
                    //     TELEGRAFO (la línea fina translúcida de origen a
                    //     destino + la punta pulsando) y EL VUELO (el filo
                    //     blanco largo + el halo) ===
                    case EstiloLanzaEterna:
                    {
                        Vector2 destinoV = new Vector2(Projectile.ai[1], Projectile.ai[2]);
                        Vector2 segV = destinoV - Projectile.Center;
                        float largoSeg = segV.Length();
                        if (largoSeg > 4f)
                        {
                            float angSeg = segV.ToRotation();
                            if (_edad < 50f)
                            {
                                // EL TELEGRAFO (translúcido — inofensivo).
                                float pulsoL = 0.25f + 0.15f * MathF.Sin(t * 6f + Seed);
                                float llenado = Math.Min(1f, _edad / 50f);
                                Vector2 medioSeg = Projectile.Center + segV * (0.5f * llenado);
                                VFXCore.Quad(medioSeg, OroGrimorio * pulsoL,
                                    new Vector2(largoSeg * llenado, 3f), angSeg);
                                // LA PUNTA (donde va a golpear — la marca).
                                VFXCore.Quad(destinoV, BlancoCaliente * (0.5f + 0.3f * MathF.Sin(t * 8f)),
                                    new Vector2(16f, 16f));
                                VFXCore.Quad(destinoV, OroGrimorio * 0.25f,
                                    new Vector2(34f, 34f));
                            }
                            else
                            {
                                // EL VUELO: el filo blanco largo + el halo.
                                VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.85f,
                                    new Vector2(46f, 5.5f), Projectile.rotation);
                                VFXCore.Quad(Projectile.Center, OroGrimorio * 0.35f,
                                    new Vector2(64f, 16f), Projectile.rotation);
                                VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.9f,
                                    new Vector2(12f, 12f));
                            }
                        }
                        break;
                    }

                    // === v6.50.54 — LA CORONA ETERNA (fase mundo): el ANILLO
                    //     guía translúcido + LAS CATORCE PLUMAS prismáticas
                    //     (cada una SU color del espectro + núcleo blanco +
                    //     la estela corta de la rotación) ===
                    case EstiloCoronaEterna:
                    {
                        float cicloCV = _edad % 600f;
                        float radioCV = cicloCV < 300f
                            ? MathHelper.Lerp(240f, 640f, cicloCV / 300f)
                            : MathHelper.Lerp(640f, 240f, (cicloCV - 300f) / 300f);
                        float giroCV = _edad * 0.024f;
                        float fadeCV = _edad > 610f
                            ? Math.Max(0f, 1f - (_edad - 610f) / 45f) : 1f;

                        // EL ANILLO GUÍA (la única advertencia — translúcido).
                        VFXCore.Quad(Projectile.Center, OroGrimorio * (0.14f * fadeCV),
                            new Vector2(radioCV * 2.174f, radioCV * 0.85f * 2.174f), 0f,
                            VFXCore.Ring);

                        // LAS PLUMAS.
                        for (int f = 0; f < 14; f++)
                        {
                            float angF = giroCV + f * MathHelper.TwoPi / 14f;
                            Vector2 dirF = new Vector2(MathF.Cos(angF), MathF.Sin(angF) * 0.85f);
                            Vector2 plumaF = Projectile.Center + dirF * radioCV;
                            Color cPrisma = ColorPrismaLocal(f / 14f + _edad * 0.003f);
                            // EL CUERPO de la pluma (SU color + el núcleo blanco).
                            VFXCore.Quad(plumaF, cPrisma * (0.85f * fadeCV),
                                new Vector2(20f, 20f));
                            VFXCore.Quad(plumaF, BlancoCaliente * (0.85f * fadeCV),
                                new Vector2(8f, 8f));
                            // LA ESTELA (la cola corta de la rotación).
                            Vector2 atrasF = new Vector2(dirF.Y, -dirF.X / 0.85f);
                            VFXCore.Quad(plumaF + atrasF * 22f, cPrisma * (0.35f * fadeCV),
                                new Vector2(34f, 7f), atrasF.ToRotation());
                        }
                        break;
                    }

                    // === v6.50.56 — LA BOLA FINAL, FASE MUNDO (EL SOL EN
                    //     MINIATURA BLANCO-DORADO: la rueda de 12 rayos
                    //     girando + las dos coronas de perlas — las MISMAS
                    //     secciones del sol del jefe, SU paleta) ===
                    case EstiloBolaFinal:
                    {
                        float nace = Math.Min(1f, _edad / 24f);
                        float latidoB = 0.86f + 0.14f * MathF.Sin(t * 1.9f);

                        // LA RUEDA DE RAYOS (12 — el sol de Aethon es una
                        // RUEDA DE LUZ: el rayo sale del centro a AMBOS lados).
                        float giroB = t * 0.16f;
                        for (int i = 0; i < 12; i++)
                        {
                            float angB = giroB + i * MathHelper.TwoPi / 12f;
                            float largoB = (120f + 60f * nace) *
                                (0.80f + 0.20f * MathF.Sin(t * 2.2f + i * 1.7f));
                            VFXCore.Quad(Projectile.Center, BlancoCaliente * (0.34f * nace),
                                new Vector2(largoB * 2f, 20f), angB, VFXCore.SoftGlow);
                        }

                        // LAS DOS CORONAS DE PERLAS (los anillos de Saturno
                        // del jefe — sentidos opuestos, a escala del aliento).
                        for (int anilloB = 0; anilloB < 2; anilloB++)
                        {
                            float rxB = (anilloB == 0 ? 118f : 158f) * nace;
                            float ryB = (anilloB == 0 ? 72f : 48f) * nace;
                            float wB = anilloB == 0 ? 0.55f : -0.38f;
                            Color cPB = anilloB == 0 ? BlancoCaliente : OroGrimorio;
                            for (int i = 0; i < 11; i++)
                            {
                                float angPB = t * wB + i * MathHelper.TwoPi / 11f;
                                Vector2 perlaB = Projectile.Center + new Vector2(
                                    MathF.Cos(angPB) * rxB, MathF.Sin(angPB) * ryB);
                                float twB = 0.5f + 0.5f * MathF.Sin(t * 3f + i * 2.1f + anilloB);
                                VFXCore.Quad(perlaB, cPB * (0.50f * nace * (0.5f + 0.5f * twB)),
                                    new Vector2(16f, 16f), 0f, VFXCore.SoftGlow);
                            }
                        }

                        // EL NÚCLEO (el corazón blanco que ARDE — latido).
                        float nucleoB = 84f * latidoB * nace;
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * (1.0f * nace),
                            new Vector2(nucleoB * 1.5f, nucleoB * 1.5f), 0f, VFXCore.SoftGlow);
                        VFXCore.Quad(Projectile.Center, OroGrimorio * (0.65f * nace),
                            new Vector2(nucleoB * 0.42f * 1.5f, nucleoB * 0.42f * 1.5f),
                            0f, VFXCore.SoftGlow);
                        break;
                    }
                }
                VFXCore.FlushAdditive(null, false);

                // === FASE 2 — EL LOTE ADITIVO DE LAS LIBRERÍAS (PANTALLA) ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                switch (Estilo)
                {
                    // === LA PÚA: el halo que respira clavada ===
                    case EstiloPuaSagrario:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 20f, TealCristal,
                            _clavada ? 0.55f : 0.8f);
                        if (_clavada)
                        {
                            float prog = (_edad - 150) / 60f;
                            if (prog > 0f && prog < 1f)
                                OndaLib.Pulse(Main.spriteBatch, pos, prog, 70f,
                                    TealCristal, 0.6f, Seed);
                        }
                        break;
                    }

                    // === EL CORO: la esquirla y su canto ===
                    case EstiloCoroCristal:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 18f, TealCristal,
                            _lanzado ? 0.9f : 0.6f);
                        if (!_lanzado)
                            OrbitaLib.AnilloFino(pos, 26f, t * 2.4f,
                                OrbitaLib.Tint(TealCristal, 0.30f));
                        break;
                    }

                    // === EL VIROTE: el parpadeo entre fases ===
                    case EstiloViroteVacio:
                    {
                        float fase = 0.55f + 0.45f * MathF.Sin(t * 11f + Seed);
                        LumenLib.Bloom(Main.spriteBatch, pos, 22f * fase, TealVacio, 0.75f * fase);
                        Vector2 cola = _posAnterior - Main.screenPosition;
                        StormLib.Bolt(Main.spriteBatch, cola, pos, Seed,
                            StormLib.FlickTick(t, 16f), 2.2f,
                            VioletaVacio, TealVacio, 0.55f * fase);
                        break;
                    }

                    // === LA FLECHA: la ESTELA DE FANTASMAS ===
                    case EstiloFlechaEstelar:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 16f, AmbarEstelar, 0.7f);
                        // La cola historia: 4 fantasmas detrás (deterministas).
                        for (int g = 1; g <= 4; g++)
                        {
                            Vector2 fantasma = pos - Projectile.velocity * (1.1f * g);
                            float alfa = 0.4f * (1f - g / 5f);
                            LumenLib.Bloom(Main.spriteBatch, fantasma, 12f,
                                AmbarEstelar, alfa, 2);
                        }
                        break;
                    }

                    // === LA MINA: el aviso de armado ===
                    case EstiloMinaEstelar:
                    {
                        float progArmado = MathHelper.Clamp(_edad / 40f, 0f, 1f);
                        OndaLib.Pulse(Main.spriteBatch, pos,
                            (_edad % 50f) / 50f, 120f, AmbarEstelar,
                            0.35f + 0.25f * progArmado, Seed);
                        LumenLib.BloomPulse(Main.spriteBatch, pos, 20f, OroGrimorio,
                            0.5f + 0.3f * progArmado, t, 3f);
                        break;
                    }

                    // === LA FUGAZ: la marca de suelo antes del impacto ===
                    case EstiloEstrellaFugaz:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 22f, OroGrimorio, 0.85f);
                        // La marca: un anillo fino en el SUELO bajo la estrella.
                        Vector2 suelo = pos;
                        Point tile = Projectile.Center.ToTileCoordinates();
                        for (int ty = 0; ty < 40; ty++)
                        {
                            Point abajo = new Point(tile.X, tile.Y + ty);
                            if (abajo.X > 5 && abajo.X < Main.maxTilesX - 5 &&
                                abajo.Y > 5 && abajo.Y < Main.maxTilesY - 5)
                            {
                                Tile tl = Main.tile[abajo.X, abajo.Y];
                                if (tl != null && tl.HasTile && Main.tileSolid[tl.TileType])
                                {
                                    suelo = new Vector2(abajo.X * 16f + 8f, abajo.Y * 16f) - Main.screenPosition;
                                    break;
                                }
                            }
                        }
                        // v6.49 — EL MEDIO-ANILLO DE SUELO que CRECE mientras la
                        // estrella cae (OndaLib.GroundVisual — la promesa del
                        // comentario por fin cumplida: la marca "respira" el
                        // impacto que viene, pegada al piso de verdad).
                        float progCaida = MathHelper.Clamp(_edad / 90f, 0f, 1f);
                        OndaLib.GroundVisual(Main.spriteBatch, suelo,
                            MathHelper.Min(progCaida * 1.3f, 1f), 74f,
                            AmbarEstelar, 0.55f);
                        OrbitaLib.AnilloFino(suelo, 46f, 0f,
                            OrbitaLib.Tint(AmbarEstelar, 0.4f));
                        break;
                    }

                    // === EL TAJO DEL PORTADOR: marca, luego el corte ===
                    case EstiloTajoPortador:
                    {
                        if (_edad <= 23)
                        {
                            // LA MARCA que se afila (Telegrafo corto).
                            Vector2 fin = pos + new Vector2(MathF.Cos(Par), MathF.Sin(Par)) * 60f;
                            OndaLib.Telegrafo(Main.spriteBatch, pos, fin,
                                _edad / 24f, EmberPortador, 3f);
                        }
                        else
                        {
                            float prog = MathF.Min(1f, (_edad - 24f) / 8f);
                            TajoLib.Tajo(pos, 56f, Par - 0.6f, Par + 0.6f,
                                prog, 1f - prog * 0.3f, 13f,
                                EmberPortador, BlancoCaliente, Seed, t);
                            LumenLib.Bloom(Main.spriteBatch, pos, 26f, EmberPortador,
                                0.7f * (1f - prog));
                        }
                        break;
                    }

                    // === LA CUCHILLA: el filo giratorio ===
                    case EstiloCuchillaOrbit:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 18f, EmberPortador, 0.75f);
                        if (!_lanzado)
                            OrbitaLib.AnilloFino(_centroOrbita - Main.screenPosition, 104f,
                                t * 1.8f, OrbitaLib.Tint(EmberPortador, 0.22f));
                        break;
                    }

                    // === EL CORTE DE REALIDAD: la PARED de desgarro ===
                    case EstiloCorteRealidad:
                    {
                        Vector2 dir = new Vector2(MathF.Cos(Par), MathF.Sin(Par));
                        float largo = 240f;
                        Vector2 origen = Projectile.Center - dir * (largo * 0.5f);
                        float prog = MathHelper.Clamp(_edad / 330f, 0f, 1f);
                        RiftLib.TearVacio(Main.spriteBatch,
                            origen - Main.screenPosition, dir, largo,
                            MathF.Min(1f, 0.25f + _edad / 60f), 34f, Seed, t);
                        break;
                    }

                    // === EL PERNO: el latido estelar ===
                    case EstiloPernoEstelar:
                    {
                        LumenLib.BloomPulse(Main.spriteBatch, pos, 20f, LuzPrimordial,
                            0.85f, t, 5f);
                        break;
                    }

                    // === LA COLUMNA: la punta que cae y el latido del juicio ===
                    case EstiloColumnaJuicio:
                    {
                        float intensidad = Projectile.ai[1] >= 1f ? 1f : 0.35f + 0.65f * (_edad / 45f);
                        // LA PUNTA: la cabeza de la columna (donde nace el manto).
                        LumenLib.BloomPulse(Main.spriteBatch, pos, 34f * intensidad,
                            BlancoCaliente, 0.9f * intensidad, t, 6f);
                        // EL LATIDO del juicio (el pulso que anuncia la caída).
                        if (Projectile.ai[1] < 1f)
                        {
                            OndaLib.Pulse(Main.spriteBatch, pos,
                                (_edad % 45f) / 45f, 120f * intensidad,
                                OroGrimorio, 0.5f, Seed);
                        }
                        break;
                    }

                    // === EL PILAR: el bloom de la base y de la punta viva ===
                    case EstiloPilarAparicion:
                    {
                        float p = Math.Min(1f, _edad / 40f);
                        float disolver = Projectile.ai[1] >= 1f
                            ? Math.Max(0f, 1f - _ticksDisolver / 40f) : 1f;
                        // LA BASE: el corazón del charco de luz.
                        LumenLib.BloomPulse(Main.spriteBatch, pos,
                            60f * disolver, OroGrimorio, 0.85f * disolver, t, 3.2f);
                        // LA PUNTA mientras cae: el cometa que desciende.
                        if (p < 1f)
                        {
                            Vector2 tip = pos - new Vector2(0f, 900f * (1f - p));
                            LumenLib.Bloom(Main.spriteBatch, tip, 44f,
                                BlancoCaliente, 0.95f, 2);
                        }
                        break;
                    }

                    // === LA NUBE: los anillos de la zona que quema ===
                    case EstiloNubeNebulosa:
                    {
                        float fade = 1f - _edad / 360f;
                        OrbitaLib.AnilloFino(pos, 54f, t * 0.8f,
                            OrbitaLib.Tint(LuzPrimordial, 0.30f * fade));
                        OrbitaLib.AnilloFino(pos, 40f, -t * 1.1f,
                            OrbitaLib.Tint(VioletaVacio, 0.22f * fade));
                        LumenLib.Bloom(Main.spriteBatch, pos, 30f, VioletaVacio, 0.35f * fade);
                        break;
                    }

                    // === LA RUNA MEMORIZADA: el sigilo dorado vivo ===
                    case EstiloRunaMemorizada:
                    {
                        LumenLib.BloomPulse(Main.spriteBatch, pos, 24f, OroGrimorio,
                            0.8f, t, 2.2f);
                        OrbitaLib.AnilloFino(pos, 30f, -Projectile.rotation,
                            OrbitaLib.Tint(OroGrimorio, 0.35f));
                        break;
                    }

                    // === EL ESTALLIDO DE MINA: la cruz de luz ===
                    case EstiloEstallidoMina:
                    {
                        float prog = _edad / 20f;
                        StormLib.ImpactFlash(Main.spriteBatch, pos, 40f,
                            AmbarEstelar, 1f - prog, t);
                        OndaLib.Pulse(Main.spriteBatch, pos, prog, 130f,
                            OroGrimorio, 0.7f * (1f - prog), Seed);
                        break;
                    }

                    // === v6.50.45 — EL CORO ESPECTRAL: las notas y sus anillos ===
                    case EstiloCoroJefe:
                    {
                        // LA ESCALA (dorada → ceniza — la del bastón).
                        Color[] escala =
                        {
                            new(255, 214, 110), new(255, 190, 92), new(225, 165, 96),
                            new(198, 140, 82), new(172, 112, 66), new(150, 96, 55),
                        };
                        // LAS NOTAS: orbes de luz con su latido propio.
                        for (int i = 0; i < 6; i++)
                        {
                            Vector2 np = _coroNotas[i] - Main.screenPosition;
                            float tw = 0.6f + 0.4f * MathF.Sin(t * 3f + i * 2.1f);
                            LumenLib.Bloom(Main.spriteBatch, np, 16f * tw,
                                escala[i], 0.8f * tw, 2);
                            LumenLib.Bloom(Main.spriteBatch, np, 6f,
                                new Color(255, 248, 220), 0.9f, 2);
                        }
                        // LOS ANILLOS: frentes de onda expandiéndose (y su ECO
                        // tenue detrás — la despedida del canto).
                        float radioFinalV = Par > 0.5f ? 300f : 260f;
                        for (int a = 0; a < _coroAnillos.Count; a++)
                        {
                            AnilloCoro anillo = _coroAnillos[a];
                            Vector2 rp = anillo.Origen - Main.screenPosition;
                            float prog = anillo.Edad / 44f;
                            OndaLib.Pulse(Main.spriteBatch, rp, prog, radioFinalV,
                                escala[anillo.Nota % 6], 0.65f * (1f - prog * 0.5f),
                                Seed + anillo.Nota * 13);
                            if (prog > 0.25f)
                                OndaLib.Pulse(Main.spriteBatch, rp,
                                    prog - 0.25f, radioFinalV,
                                    escala[anillo.Nota % 6], 0.22f, Seed + anillo.Nota * 7);
                        }
                        break;
                    }

                    // === v6.50.45 — EL TELAR: las ESTRELLAS clavadas (la figura
                    //     la dibujan los hilos de la FASE 1; aquí viven las
                    //     estrellas — destellos de 4 puntas con parpadeo propio) ===
                    case EstiloTelarJefe:
                    {
                        int puntasR = Par > 0.5f ? 7 : 5;
                        for (int i = 0; i < puntasR; i++)
                        {
                            if (!_estrellaClavada[i]) continue;
                            Vector2 sp = _estrellas[i] - Main.screenPosition;
                            float tw = 0.55f + 0.45f * MathF.Sin(t * 2.6f + i * 1.9f);
                            Color cE = LumenLib.Hue(VFXCore.Hash01(Seed, i, 87) * 0.86f,
                                0.62f, 0.95f);
                            LumenLib.Flare(Main.spriteBatch, sp, 26f + 14f * _telarFlash,
                                cE, (0.55f + 0.45f * _telarFlash) * tw, t * 0.7f + i);
                            LumenLib.Bloom(Main.spriteBatch, sp, 10f,
                                new Color(255, 255, 255), 0.85f * tw, 2);
                        }
                        // LA SEÑAL del centro (donde nació la figura — el
                        // compás que la presa debe leer).
                        OndaLib.Pulse(Main.spriteBatch, pos,
                            (_edad % 40f) / 40f, 120f,
                            new Color(150, 170, 255), 0.4f, Seed);
                        break;
                    }

                    // === v6.50.45 — EL DECRETO DEL ECLIPSE: el círculo, la onda
                    //     viajera, LA MARCA DEL OJO y el flash de la ejecución ===
                    case EstiloDecretoJefe:
                    {
                        int faseD = Math.Max(2, (int)Par);
                        float radioMaxD = 600f + (faseD - 2) * 120f;
                        float radio = Math.Min(80f + 2f * _edad, radioMaxD);
                        float cyc = _edad % 15f;
                        float flare = cyc < 5f ? 1f - cyc / 5f : 0f;

                        // EL ANILLO de fuego oscuro (dos aros contrarrotantes
                        // + el borde ámbar DESGARRADO del bastón).
                        OrbitaLib.AnilloFino(pos, radio * 0.99f, t * 0.22f,
                            OrbitaLib.Tint(ColorVioletaEclipse, 0.55f));
                        OrbitaLib.AnilloFino(pos, radio * 1.04f, -t * 0.16f,
                            OrbitaLib.Tint(ColorOroEclipse, 0.4f + 0.3f * flare));
                        LumenLib.Bloom(Main.spriteBatch, pos, radio * 0.5f,
                            ColorVioletaEclipse, 0.10f + 0.10f * flare, 2);

                        // LA ONDA VIAJERA: del centro al borde en los 8 t
                        // previos a la ejecución (el telégrafo del tajo).
                        if (cyc >= 7f)
                        {
                            float prog = (cyc - 7f) / 8f;
                            OndaLib.Pulse(Main.spriteBatch, pos, prog, radio,
                                ColorOroEclipse, 0.55f, Seed);
                        }

                        // LA MARCA DEL OJO sobre la presa más cercana dentro
                        // (esclerótica + iris + PUPILA VERTICAL que se
                        // CONTRAE los 6 t previos al tajo — el ojo del bastón).
                        Player marcado = Presa();
                        if (marcado != null &&
                            Vector2.Distance(marcado.Center, Projectile.Center) <= radio)
                        {
                            Vector2 ep = marcado.Center - Main.screenPosition
                                - new Vector2(0f, 58f);
                            float contraccion = cyc >= 9f ? (cyc - 9f) / 6f : 0f;
                            // esclerótica (aplanada — un ojo que FLOTa).
                            LumenLib.Bloom(Main.spriteBatch, ep, 30f,
                                new Color(255, 244, 214), 0.35f, 2);
                            // iris.
                            LumenLib.Bloom(Main.spriteBatch, ep, 14f,
                                ColorVioletaEclipse, 0.7f, 2);
                            // LA PUPILA VERTICAL: se cierra antes del corte.
                            float anchoP = MathHelper.Lerp(5.5f, 1.2f, contraccion);
                            Main.spriteBatch.Draw(VFXCore.SoftGlow, ep, null,
                                new Color(255, 250, 230) * 0.9f, 0f,
                                new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f,
                                new Vector2(anchoP / VFXCore.SoftGlow.Width,
                                            16f / VFXCore.SoftGlow.Height),
                                SpriteEffects.None, 0f);
                        }

                        // EL FLASH de la ejecución (la sobre-exposición).
                        if (flare > 0f)
                            LumenLib.Bloom(Main.spriteBatch, pos, radio * 1.1f,
                                new Color(255, 244, 214), 0.22f * flare, 2);
                        break;
                    }

                    // === v6.50.53 — EL ESTALLIDO RADIANTE, FASE PANTALLA (EL
                    //     PUNTO DE LUZ de la imagen): EL NÚCLEO que crece
                    //     hasta ~950 px y RESPIRA, LA ESTRELLA de 8 rayos
                    //     girando LENTA (el lens-flare del cine), LA ONDA
                    //     expansiva y LAS BOKEH (los puntos de luz dispersos
                    //     de la imagen — deterministas, como todo lo demás) ===
                    case EstiloEstallidoRadiante:
                    {
                        float crecS = Math.Min(1f, _edad / 16f);
                        float fadeS = _edad < 55f ? 1f :
                            Math.Max(0f, 1f - (_edad - 55f) / 95f);
                        float respira = 0.95f + 0.05f * MathF.Sin(t * 4.2f);
                        float escS = EscalaEstallido;   // v6.50.54 — «un poco más grande»

                        // EL NÚCLEO INUNDANDO (el blanco que crece hasta
                        // ~950·escala px — el corazón que se queda viendo).
                        LumenLib.Bloom(Main.spriteBatch, pos,
                            (170f + 780f * crecS) * respira * escS, BlancoCaliente,
                            0.95f * fadeS, 4);
                        LumenLib.BloomPulse(Main.spriteBatch, pos,
                            (150f + 190f * crecS) * escS, OroGrimorio,
                            0.55f * fadeS, t, 2.6f);

                        // LA ESTRELLA DE DESTELLO (el flare de 8 rayos + núcleo
                        // caliente — la rotación LENTA de la imagen).
                        Texture2D dest = VFXCore.DestelloFinal;
                        if (dest != null)
                        {
                            float esc = (1500f * crecS * escS) / dest.Width *
                                (0.92f + 0.08f * MathF.Sin(t * 3.4f));
                            Main.spriteBatch.Draw(dest, pos, null,
                                OrbitaLib.Tint(BlancoCaliente, 0.60f * fadeS),
                                t * 0.06f,
                                new Vector2(dest.Width, dest.Height) * 0.5f, esc,
                                SpriteEffects.None, 0f);
                        }

                        // LA ONDA EXPANSIVA (el frente que barre — una sola,
                        // clara: el golpe ya pasó, esto es la firma).
                        if (_edad < 80f)
                            OndaLib.Pulse(Main.spriteBatch, pos, _edad / 80f,
                                760f * escS, OroGrimorio, 0.50f * (1f - _edad / 80f), Seed);

                        // LAS BOKEH (los puntos de luz dispersos de la imagen:
                        // 26 chispas fijas que giran despacito y titilan).
                        for (int i = 0; i < 26; i++)
                        {
                            float h0 = VFXCore.Hash01(Seed, i, 30);
                            float h1 = VFXCore.Hash01(Seed, i, 31);
                            float tw = 0.35f + 0.65f *
                                (0.5f + 0.5f * MathF.Sin(t * 2.6f + i * 1.9f));
                            float rB = (90f + 640f * h1) * crecS * escS;
                            float angB = h0 * MathHelper.TwoPi + t * 0.05f;
                            Vector2 b = pos + new Vector2(MathF.Cos(angB), MathF.Sin(angB)) * rB;
                            LumenLib.Bloom(Main.spriteBatch, b, 8f + 10f * h1,
                                (i % 2 == 0) ? OroGrimorio : AmbarEstelar,
                                0.45f * fadeS * tw, 2);
                        }
                        break;
                    }

                    // === v6.50.56 — LA BOLA FINAL, FASE PANTALLA (EL
                    //     DESTELLO del aliento: la estrella de 8 rayos
                    //     girando + el bloom blanco-dorado que respira +
                    //     LAS CHISPAS ORBITANTES — el lens-flare del sol
                    //     del bastón, en SU idioma de color) ===
                    case EstiloBolaFinal:
                    {
                        float naceS = Math.Min(1f, _edad / 24f);
                        float fadeBS = Projectile.timeLeft < 45f
                            ? Projectile.timeLeft / 45f : 1f;
                        float respiraB = 0.95f + 0.05f * MathF.Sin(t * 4.2f);

                        // EL NÚCLEO BLANCO-DORADO (el corazón que crece
                        // hasta ~300 px y RESPIRA — mucho brillo).
                        LumenLib.Bloom(Main.spriteBatch, pos,
                            (120f + 180f * naceS) * respiraB, BlancoCaliente,
                            0.95f * naceS * fadeBS, 4);
                        LumenLib.BloomPulse(Main.spriteBatch, pos,
                            90f + 40f * naceS, OroGrimorio,
                            0.60f * naceS * fadeBS, t, 2.4f);

                        // LA ESTRELLA DE DESTELLO (el flare de 8 rayos —
                        // DOS: la blanca grande y la dorada al revés).
                        Texture2D destB = VFXCore.DestelloFinal;
                        if (destB != null)
                        {
                            float escB = (520f * naceS) / destB.Width *
                                (0.92f + 0.08f * MathF.Sin(t * 3.4f));
                            Main.spriteBatch.Draw(destB, pos, null,
                                OrbitaLib.Tint(BlancoCaliente, 0.60f * naceS * fadeBS),
                                t * 0.10f,
                                new Vector2(destB.Width, destB.Height) * 0.5f, escB,
                                SpriteEffects.None, 0f);
                            Main.spriteBatch.Draw(destB, pos, null,
                                OrbitaLib.Tint(OroGrimorio, 0.40f * naceS * fadeBS),
                                -t * 0.07f + 0.4f,
                                new Vector2(destB.Width, destB.Height) * 0.5f, escB * 0.6f,
                                SpriteEffects.None, 0f);
                        }

                        // LAS CHISPAS ORBITANTES (8 motas deterministas que
                        // giran despacito y titilan — el enjambre cercano).
                        for (int i = 0; i < 8; i++)
                        {
                            float h0B = VFXCore.Hash01(Seed, i, 40);
                            float twB = 0.35f + 0.65f *
                                (0.5f + 0.5f * MathF.Sin(t * 2.6f + i * 1.9f));
                            float rB2 = (60f + 130f * h0B) * naceS;
                            float angB2 = h0B * MathHelper.TwoPi + t * 0.22f;
                            Vector2 b2 = pos + new Vector2(MathF.Cos(angB2), MathF.Sin(angB2)) * rB2;
                            LumenLib.Bloom(Main.spriteBatch, b2, 7f + 8f * h0B,
                                (i % 2 == 0) ? BlancoCaliente : OroGrimorio,
                                0.50f * naceS * fadeBS * twB, 2);
                        }
                        break;
                    }
                }
                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            finally
            {
                // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
                // llegó cerrado por un mod ajeno, se cura — el restore condicional
                // devolvía el veneno y tML mataba al proyectil: active=false).
                VFXCore.ReabrirLoteVanilla();
            }
            return false;
        }
    }
}
