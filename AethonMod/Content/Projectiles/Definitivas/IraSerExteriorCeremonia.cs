using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.Projectiles.Definitivas
{
    // =====================================================================
    //  v6.59 — LA CEREMONIA DE LA IRA DE UN SER EXTERIOR (el despertar 50
    //  del grimorio — LO que consigue al final del camino).
    //
    //  LA LETRA, punto por punto:
    //  «independientemente de donde esten los minions ya sean sobre el
    //  jugador o sobre el grimorio, estos deben comenzar a girar en
    //  circulos, o sea el circulo que forman debe cerrarse en caso de
    //  que este en el jugador al inicio de la animacion, se deben elevar
    //  un poco si estan en el jugador, si estan en el libro no es
    //  necesario elevarse, entonces una vez que esten girando en circulos
    //  estos deben lanzar su ataque de rayos de perlin justo al centro
    //  del circulo, ahi se debe forma una bruma blanca y morada, se deben
    //  formas los circulos runicos, y es cuando el ataque comienza a
    //  cargar, en el momento de disparo los minion lanzan el haz hacia
    //  el enemigo al que estan apuntando en ese momento, si hay un jefe
    //  presente este siempre tendra prioridad por sobre todas las cosas»
    //
    //  LA LÍNEA DE TIEMPO (450 ticks — 7,5 segundos de ceremonia):
    //    0 ── 149  EL CÍRCULO SE CIERRA: los ojos (donde estén — el
    //              abanico del jugador SE CIERRA en corona completa,
    //              elevándose sobre el dueño; junto al libro ya giraban:
    //              el centro ES el libro) giran alrededor del centro del
    //              ritual con la VUELTA DEL RITUAL (ω 0,30→1,05 rad/s —
    //              arranca como la órbita del libro y ACELERA). La bruma
    //              blanca y morada respira y crece; los círculos rúnicos
    //              se encienden. TODO EN SILENCIO (v6.61: la creación
    //              del haz NO lleva sonidos de Terraria — ni el zap de
    //              los rayos rituales ni el compás: la ceremonia YA
    //              tiene su propia voz, la carga del RC2).
    //   12 ── 112  LOS RAYOS DE PERLIN: cada ojo del escuadrón, uno a
    //              uno (uno cada 10 ticks — el orden del escuadrón),
    //              dispara SU RAYO RITUAL (TruenoPerlin modo ritual: la
    //              boca SIGUE al ojo que gira, la punta clavada al
    //              centro — sostenido TODO el ritual, MUDO en la ira).
    //    · ── 450  LOS ARCOS DEL ESCUADRÓN (v6.61): TODO el ritual, los
    //              rayitos eléctricos van DE UN OJO A OTRO del círculo
    //              (uno al azar y su OPUESTO — la cuerda CRUZA EL
    //              MEDIO) — los gatilla la MISMA animación de la órbita.
    //  150 ── 449  LA CARGA: el sonido amado de la carga del RC2 suena
    //              entero (5,15 s), la implosión de soles rúnicos NACE
    //              LEJOS y cae al centro y TODO se CIÑE en los últimos
    //              45 ticks (la contracción de la casa — el parto se
    //              anuncia).
    //  450 ──────  EL DISPARO: la ceremonia MUERE y nace EL HAZ (el rayo
    //              de prueba C — EL SUEÑO, el ganador) YA EN el momento
    //              del disparo (Time = ChargeTime: la ceremonia FUE su
    //              carga), apuntando AL CURSOR del dueño y siguiéndolo
    //              TODO el lanzamiento (v6.60.2 — la letra nueva). El
    //              jugador queda LIBRE: la ira es del Ser Exterior. Y
    //              el círculo SIGUE: los ojos no vuelven a su puesto
    //              hasta que el haz MUERE (v6.60.2).
    //
    //  EL DAÑO (la letra, exacta): «el daño base del rayo que es 1000,
    //  mas el calculo correspondiente para el aumento de daño magico,
    //  mas el 50% del total del daño de invocacion» — lo calcula
    //  AtaquesCombinadosSistema.DanoDeLaIra (la cadena mágica EXACTA del
    //  clic izquierdo del Eterno sobre la base 1000 + el 50 % del rayo
    //  del ojo más despierto) y viaja en ai[1].
    //
    //  EL LORE: el grimorio ES un dios — pero no de este mundo. Es una
    //  entidad exterior, alguien que viene de fuera y puede ver otros
    //  mundos. Cuando el escuadrón entero cierra el círculo y reza al
    //  centro, lo que responde no es un arma: es la IRA de ese Alguien
    //  mirando hacia adentro — el rayo por el que el mundo entero es
    //  solo una página más.
    //
    //  SP-FIRST (el patrón de la casa): la IA completa corre en la
    //  máquina del dueño de los ojos (AtaquesCombinadosSistema.Tick solo
    //  corre en su pantalla); la ceremonia es netImportant — las demás
    //  pantallas ven el cine sincronizado.
    // =====================================================================
    public class IraSerExteriorCeremoniaProjectile : ModProjectile
    {
        /// <summary>LA TEXTURA DE CARGA (el patrón de la casa — el mismo
        /// SoftGlow del RC2): la ceremonia es 100 % procedimental (su
        /// PreDraw devuelve false: NUNCA dibuja esta textura) pero tML
        /// EXIGE una textura resuelta para cada ModProjectile al cargar
        /// — sin este override el mod entero muere en el cliente con
        /// «Recurso esperado no encontrado» (el headless no lo caza:
        /// los servers NO cargan texturas — la lección v6.60.1).</summary>
        public override string Texture =>
            "AethonMod/Content/Effects/Procedural/SoftGlow";

        /// <summary>LOS TIEMPOS: cerrar el círculo + rayos (150) · la
        /// carga (300 — la misma del RC2) · al cumplir, EL HAZ.</summary>
        private const int T_CEREMONIA = 150;
        private const int T_CARGA = 300;
        private const int T_TOTAL = T_CEREMONIA + T_CARGA;

        // v6.60.2 — LA PROPIEDAD Blanco MURIÓ con la mira al cursor:
        // ai[0] queda libre (nadie la escribe, nadie la lee).

        /// <summary>EL DAÑO DEL HAZ (ai[1] — calculado por el sistema: la
        /// base 1000 + la cadena mágica + el 50 % de invocación).</summary>
        public ref float Dano => ref Projectile.ai[1];

        /// <summary>LA EDAD del ritual (ai[2] — sincronizada: los demás
        /// cables dibujan la ceremonia con SU progreso real).</summary>
        public ref float Edad => ref Projectile.ai[2];

        // v6.60.2 — LA ÚLTIMA MIRADA MURIÓ (los campos también): el
        // rumbo de nacimiento es el CURSOR del dueño — siempre existe,
        // no hace falta recordar a nadie.

        // EL ESCUADRÓN: los whoAmI de los ojos vivos al nacer el ritual
        // (disparan sus rayos uno a uno, en orden de escuadrón).
        private readonly List<int> _ojos = new List<int>(12);
        private int _rayosLanzados = 0;

        // EL NÚCLEO BLANCO-VIOLETA de los soles del ritual (el mismo del haz).
        private static readonly Color CorazonRunico = new Color(240, 228, 255);

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;      // la ceremonia no daña: EL HAZ daña
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = T_TOTAL + 30;
            Projectile.netImportant = true;   // la ceremonia es de todos los cables
        }

        // ================================================================
        //  EL RITUAL VIVO — el punto que los ojos del escuadrón orbitan
        //  (la cache por frame de la casa, el patrón PosicionDelLibro).
        // ================================================================
        private static int _frameCache = -1;
        private static int _ownerCache = -1;
        private static Projectile _ritualCache = null;

        /// <summary>EL ANCLA VIVA del dueño (o null): los ojos orbitan en
        /// corona cerrada el punto que ella marca — es SU círculo.
        /// v6.60.2 — LA VUELTA SIGUE EN EL DISPARO (la letra: «estos
        /// deben seguir dando vueltas en circulos mientras el haz esta
        /// siendo lanzado y cuando termine el lanzamiento es que regresan
        /// a su posicion normal»): mientras vive, el ancla es LA
        /// CEREMONIA; muerta ella, el ancla es EL HAZ DE LA IRA mientras
        /// dispare (su boca ES el centro del círculo — el mismo punto que
        /// el escuadrón gestó). Cuando el haz muere, el ancla muere y los
        /// ojos vuelven a su formación de siempre.</summary>
        internal static Projectile RitualDe(int ownerWhoAmI)
        {
            if (_frameCache == (int)Main.GameUpdateCount && _ownerCache == ownerWhoAmI)
                return _ritualCache;
            _frameCache = (int)Main.GameUpdateCount;
            _ownerCache = ownerWhoAmI;
            _ritualCache = null;
            int tipo = ModContent.ProjectileType<IraSerExteriorCeremoniaProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.type == tipo && p.owner == ownerWhoAmI)
                {
                    _ritualCache = p;
                    break;
                }
            }
            // v6.60.2 — EL HAZ ES EL ANCLA EN EL DISPARO: la ceremonia
            // murió pero EL HAZ vive — el círculo NO se abre hasta que el
            // lanzamiento termine (la ceremonia nueva, si nace, manda: el
            // escaneo de arriba la encuentra PRIMERO).
            if (_ritualCache == null)
            {
                int tipoHaz = ModContent.ProjectileType<RayosPruebaProjectile>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || p.type != tipoHaz ||
                        p.owner != ownerWhoAmI) continue;
                    if (p.ModProjectile is RayosPruebaProjectile haz && haz.EsIra)
                    {
                        _ritualCache = p;
                        break;
                    }
                }
            }
            return _ritualCache;
        }

        // ================================================================
        //  LA IA
        // ================================================================
        public override void AI()
        {
            float edad = Edad;
            Projectile.velocity = Vector2.Zero;

            Player dueno = Main.player[Projectile.owner];
            bool duenoVivo = dueno != null && dueno.active && !dueno.dead;

            // === EL CENTRO DEL CÍRCULO (la letra): si el LIBRO está fuera
            //     cazando, el ritual vive EN el libro (los ojos ya giraban
            //     ahí — «si estan en el libro no es necesario elevarse»);
            //     si está en casa, el círculo se forma ELEVADO sobre el
            //     jugador (−100 px — «se deben elevar un poco si estan en
            //     el jugador») y el abanico trasero SE CIERRA en corona.
            //     El dueño caído: el ritual flota en su último punto — un
            //     hechizo empezado se cumple (v6.51.5).
            if (duenoVivo)
            {
                Vector2? libro = global::AethonMod.Content.Projectiles.OjoEternoMinion
                    .PosicionDelLibro(dueno);
                Vector2 objetivo = libro is Vector2 c
                    ? c
                    : dueno.MountedCenter + new Vector2(0f, -100f);
                Projectile.Center = Vector2.Lerp(Projectile.Center, objetivo, 0.18f);
            }

            // v6.60.2 — LA ÚLTIMA MIRADA MURIÓ: el rumbo del haz YA no
            // es el enemigo del escuadrón — es EL CURSOR del dueño (la
            // letra: «el haz debe apuntar a donde vaya el cursor y
            // siempre seguir el cursor»); no hay nada que recordar.

            // === EL ESCUADRÓN se reúne al nacer el ritual (los que estén
            //     — «independientemente de donde esten los minions»).
            if (edad < 2f)
            {
                _ojos.Clear();
                _rayosLanzados = 0;
                int tipoOjo = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.OjoEternoMinion>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p != null && p.active && p.type == tipoOjo &&
                        p.owner == Projectile.owner)
                        _ojos.Add(i);
                }
            }

            // === LOS RAYOS DE PERLIN, UNO A UNO (la letra: «una vez que
            //     esten girando en circulos estos deben lanzar su ataque
            //     de rayos de perlin justo al centro del circulo»): cada
            //     ojo dispara SU rayo ritual — la boca SIGUE al ojo que
            //     gira, la punta vive clavada al centro — sostenidos
            //     TODO el ritual (el ancla es ESTA ceremonia).
            if (Projectile.owner == Main.myPlayer && edad >= 12f && edad < 130f)
            {
                int tocados = (int)((edad - 12f) / 10f) + 1;
                while (_rayosLanzados < tocados && _rayosLanzados < _ojos.Count)
                {
                    DispararRayoRitual(_ojos[_rayosLanzados]);
                    _rayosLanzados++;
                }
            }

            float prog = MathHelper.Clamp(edad / (float)T_TOTAL, 0f, 1f);

            // === LA CARGA COMIENZA (edad 150): el SONIDO AMADO de la
            //     carga del RC2 suena ENTERO (5,15 s — calza con los 300
            //     ticks de la carga: la ceremonia SUENA como el haz que
            //     fue siempre).
            if (!Main.dedServ && edad == (float)T_CEREMONIA)
            {
                try
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        new Terraria.Audio.SoundStyle(
                            "AethonMod/Content/Sounds/RayoConcentradoCarga",
                            Terraria.Audio.SoundType.Sound)
                        { MaxInstances = 1, Volume = 1f },
                        Projectile.Center);
                }
                catch { }
            }

            // === LA LUZ: la bruma blanca y morada ALUMBRA (violeta
            //     incandescente creciendo con el ritual).
            if (!Main.dedServ)
                Lighting.AddLight(Projectile.Center,
                    (0.30f + 0.45f * prog) , (0.22f + 0.32f * prog), (0.55f + 0.45f * prog));

            // === EL MUNDO LE SOPA PARTÍCULAS (blancas y moradas
            //     convergentes — la carga del ritual de la casa).
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                float lejos = Main.rand.NextFloat(90f, 260f);
                Vector2 posM = Projectile.Center +
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * lejos;
                Vector2 hacia = Projectile.Center - posM;
                bool blanca = Main.rand.NextBool(3);
                Dust d = Dust.NewDustPerfect(posM,
                    blanca ? DustID.PurpleTorch : DustID.Shadowflame,
                    hacia.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2.5f, 6f),
                    140, blanca ? new Color(230, 220, 255) : new Color(130, 70, 200), 0.9f);
                d.noGravity = true;
            }

            // v6.61 — EL COMPÁS DEL RITUAL MURIÓ (la letra: «la
            //     creacion del haz no necesita sonidos de terraria, ya
            //     tiene su propio sonido»): el tic-tac de Item122 era
            //     UN SONIDO DE TERRARIA sonando TODO el nacimiento del
            //     haz — calla entero. La ceremonia queda con SU voz
            //     propia: el SILENCIO del círculo que cierra (0-150) y
            //     LA CARGA DEL RC2 (150-450, 5,15 s — el sonido amado
            //     que calza exacto con los 300 ticks de la carga);
            //     después, el bucle del disparo. Nada de Terraria.

            // === LA EDAD SINCRONIZADA: cada medio segundo el ritual
            //     avisa SU edad (los demás cables dibujan el progreso).
            if (edad % 30f == 0f)
                Projectile.netUpdate = true;

            // === EL CUMPLIMIENTO: EL HAZ (la ceremonia FUE la carga — el
            //     haz nace YA DISPARANDO, apuntando AL CURSOR del dueño
            //     — v6.60.2, la letra nueva — y el círculo SIGUE hasta
            //     que el haz muera).
            if (edad >= (float)T_TOTAL)
            {
                LanzarElHaz();
                Projectile.Kill();
                return;
            }

            Edad = edad + 1f;
        }

        // v6.60.2 — BlancoVivo MURIÓ: nadie lee el blanco de la
        // ceremonia ya (el rumbo es del cursor — la letra nueva).

        /// <summary>EL RAYO RITUAL DE UN OJO (el modo ritual del
        /// TruenoPerlin — el patrón de LosCelebrantesRezan: la boca sigue
        /// al ojo vivo, la punta vive clavada al centro de ESTA
        /// ceremonia, sin golpe: es la oración, no el ataque).</summary>
        private void DispararRayoRitual(int idxOjo)
        {
            Projectile ojo = Main.projectile[idxOjo];
            if (ojo == null || !ojo.active) return;
            try
            {
                Projectile.NewProjectile(ojo.GetSource_FromAI(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Cosmic.TruenoPerlinProjectile>(),
                    0, 0f, Projectile.owner,
                    2f,                      // EL MODO: RITUAL (sostenido, sin golpe)
                    ojo.whoAmI * 1f,         // la boca: ESTE ojo
                    Projectile.whoAmI * 1f); // el ancla: ESTA ceremonia
            }
            catch { }
        }

        /// <summary>EL DISPARO — EL HAZ DE LA IRA: la ceremonia muere y
        /// nace EL RAYO DE PRUEBA C (EL SUEÑO — el ganador, intacto) YA EN
        /// el momento del disparo (Time = ChargeTime: su carga FUE la
        /// ceremonia), en el MODO IRA: boca fija en el centro del círculo,
        /// apuntando AL CURSOR del dueño (v6.60.2 — la letra nueva: «el
        /// haz debe apuntar a donde vaya el cursor y siempre seguir el
        /// cursor, creo que asi sera mejor») y el portador LIBRE. La ira
        /// se DISPARA cuando el escuadrón caza, pero se APUNTA con la
        /// mano del portador — y la persigue TODO el lanzamiento.
        /// «este ataque se considera ataque magico» — su tipo de daño es
        /// MÁGICO.</summary>
        private void LanzarElHaz()
        {
            // SOLO LA MÁQUINA DUEÑA resuelve el lanzamiento (el patrón de
            // la casa): el dueño engendra y NewProjectile sincroniza.
            if (Projectile.owner != Main.myPlayer) return;

            // v6.60.2 — EL RUMBO DE NACIMIENTO ES EL CURSOR (la letra:
            // «el haz debe apuntar a donde vaya el cursor y siempre
            // seguir el cursor, creo que asi sera mejor»): nace apuntando
            // a DONDE APUNTA EL JUGADOR y lo persigue TODO el lanzamiento
            // (el blanco-persa de la v6.59 murió con la letra nueva — la
            // ira se DISPARA cuando el escuadrón caza, pero se APUNTA con
            // la mano del portador). Esto corre SOLO en la máquina del
            // dueño (la guarda de arriba): el cursor es SU ratón.
            Vector2 rumbo = Main.MouseWorld - Projectile.Center;
            if (rumbo.LengthSquared() < 1f) rumbo = -Vector2.UnitY;
            rumbo.Normalize();

            int dano = Math.Max(1, (int)Dano);

            // EL HAZ — LA RECETA C (2 = EL SUEÑO) con Time YA EN el
            // disparo (ChargeTime): el estallido de soles MURIÓ con la
            // letra nueva (v6.60.2: nada de estrellitas en el disparo) —
            // la transición ceremonia→conducto se lee con LA IGNICIÓN del
            // segundo tick (el hitstop y el temblor: MOVIMIENTO, no luz).
            Projectile haz = Projectile.NewProjectileDirect(
                Projectile.GetSource_FromAI(),
                Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<RayosPruebaProjectile>(),
                dano, 4f, Projectile.owner,
                RayosPruebaProjectile.ChargeTime * 1f,  // Time = ChargeTime: YA DISPARA
                2f,                                      // ai[1] — legado del slot de receta (la C, la única)
                Projectile.owner * 1f);                  // el dueño

            // EL MODO IRA (viaja por SendExtraAI en el paquete del spawn):
            // boca fija en el centro del ritual + el blanco perseguido.
            if (haz.ModProjectile is RayosPruebaProjectile rp)
            {
                rp.EsIra = true;
                rp.CentroIra = Projectile.Center;
                rp.targetRotation = rumbo.ToRotation();
            }
            haz.rotation = rumbo.ToRotation();
            haz.DamageType = DamageClass.Magic;   // «se considera ataque magico»
            haz.netUpdate = true;
        }

        // ================================================================
        //  EL DIBUJO — vive en HazSobreElMundoSistema (encima de TODO el
        //  mundo, debajo de la interfaz): la bruma blanca y morada, los
        //  círculos rúnicos, los arcos del escuadrón, la implosión de
        //  soles y la semilla del haz que crece. El PreDraw NO dibuja
        //  nada.
        // ================================================================
        public override bool PreDraw(ref Color lightColor) => false;

        // ================================================================
        //  v6.61 — LOS ARCOS DEL ESCUADRÓN: los rayitos eléctricos son
        //  DE LOS MINIONS (la letra: «los pequeños rayos electricos
        //  deben concentrarce en el centro del circulo formado por los
        //  minions, la forma para gatillar la creacion de pequeños
        //  rayos electrico es la misma animacion de los minions,
        //  ademas cada rayito debe ir de un minion a otro de forma
        //  aleatorio mientras el haz se carga y se lanza»). Cada arco
        //  une DOS ojos del círculo que gira — uno AL AZAR y su
        //  OPUESTO del otro lado (la cuerda CRUZA EL MEDIO: la
        //  electricidad se CONCENTRA en el centro del círculo) — y los
        //  gatilla LA MISMA ANIMACIÓN de la órbita: viven mientras el
        //  escuadrón gira, en la CEREMONIA (la carga) y en el DISPARO
        //  (los ojos siguen el círculo alrededor de la boca del haz).
        // ================================================================

        // LOS POZOS cero-GC de la casa (las posiciones y ángulos de los
        // ojos vivos, reusados cada frame — nunca un new por frame).
        private static readonly Vector2[] _arcosPos = new Vector2[16];
        private static readonly float[] _arcosAng = new float[16];
        private static readonly int[] _arcosOp = new int[16];

        /// <summary>LOS RAYITOS DE UN MINION A OTRO: dibuja 2-5 arcos
        /// (60-100 % de alfa según la intensidad) entre ojos AL AZAR y
        /// sus OPUESTOS del círculo. La llama la ceremonia (su carga) y
        /// el haz de la ira (su disparo) — el MISMO traje en las dos
        /// fases, como manda la letra. Es puro cine de cliente: el par
        /// lo elige el reloj del juego de cada pantalla (nada que
        /// sincronizar).</summary>
        internal static void DibujarArcosDelEscuadron(Vector2 centroMundo,
            int owner, float intensidad, float relojColor)
        {
            if (Main.dedServ) return;

            // EL ESCUADRÓN VIVO: los ojos del dueño, donde estén — la
            // órbita del ritual los pone en el círculo alrededor del
            // centro (SlotDeFormacion: la MISMA animación que gatilla
            // los arcos).
            int tipoOjo = ModContent.ProjectileType
                <global::AethonMod.Content.Projectiles.OjoEternoMinion>();
            int n = 0;
            for (int i = 0; i < Main.maxProjectiles && n < 16; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != tipoOjo) continue;
                if (p.owner != owner) continue;
                _arcosPos[n] = p.Center;
                _arcosAng[n] = (p.Center - centroMundo).ToRotation();
                n++;
            }
            if (n < 2) return;

            float intens = MathHelper.Clamp(intensidad, 0f, 1f);
            int flick = (int)(Main.GlobalTimeWrappedHourly * 60f) & 0x3FF;
            int cuenta = 2 + (int)(intens * 3.99f);

            for (int i = 0; i < cuenta; i++)
            {
                // EL PAR CAMBIA cada 8 ticks («de forma aleatoria»): el
                // reloj se parte en módulo 960 ANTES de escalar (el
                // GameUpdateCount crudo pierde precisión en float tras
                // horas de partida) — cada cliente dibuja SU par (es
                // puro cine: nada que sincronizar).
                int seed = (int)((Main.GameUpdateCount % 960ul) * 0.125f) * 131 + i * 297;
                int a = (int)(VFXCore.Hash01(seed, 11, 7) * n);
                if (a >= n) a = n - 1;

                // SU OPUESTO DEL CÍRCULO: los ojos del OTRO LADO — la
                // cuerda CRUZA EL MEDIO (si el par saliera del mismo
                // lado, el arco abrazaría el BORDE y el centro quedaría
                // vacío — justo lo que la letra prohibe). El umbral de
                // 2,4 rad (137°) deja SOLO los pares casi-diametrales
                // — con el escuadrón de 11 (32,7° por puesto) son los
                // ±5/±6 puestos (164°): la cuerda pasa a ~10 px del
                // CENTRO exacto. Sin opuestos (un círculo muy chico),
                // el más lejano del sorteo.
                float angA = _arcosAng[a];
                int nOp = 0;
                for (int j = 0; j < n; j++)
                {
                    if (j == a) continue;
                    if (Math.Abs(MathHelper.WrapAngle(_arcosAng[j] - angA)) >= 2.4f)
                        _arcosOp[nOp++] = j;
                }
                int b;
                if (nOp > 0)
                {
                    int ele = (int)(VFXCore.Hash01(seed, 23, 13) * nOp);
                    if (ele >= nOp) ele = nOp - 1;
                    b = _arcosOp[ele];
                }
                else
                    b = (a + Math.Max(1, n / 2)) % n;

                Vector2 start = _arcosPos[a] - Main.screenPosition;
                Vector2 end = _arcosPos[b] - Main.screenPosition;
                StormLib.Bolt(Main.spriteBatch, start, end, seed, flick, 3.2f,
                    RayosPruebaProjectile.ColorConjuro(relojColor + i * 8f),
                    DefinitivasLib.BlancoCalido,
                    0.60f + 0.40f * intens, 5, 12f);
            }
        }

        /// <summary>EL DIBUJO COMPLETO DEL RITUAL — lo llama
        /// HazSobreElMundoSistema desde su capa de interfaz, ENCIMA de
        /// todo el mundo. Contrato de lote: entra con un lote de mundo
        /// ABIERTO y sale dejando el lote en estado vanilla válido.</summary>
        internal void DibujarRitual()
        {
            if (Main.dedServ) return;

            float edad = Edad;
            float prog = MathHelper.Clamp(edad / (float)T_TOTAL, 0f, 1f);
            // LA CONTRACCIÓN (la firma de la casa): los últimos 45 ticks
            // TODO se ciñe — el parto se anuncia, no estalla de golpe.
            float contraccion = Utils.GetLerpValue(T_TOTAL - 45f, T_TOTAL, edad, true);
            float time = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 17 + 3;
            Vector2 centro = Projectile.Center;
            Vector2 posPantalla = centro - Main.screenPosition;

            // ============ LA BRUMA BLANCA Y MORADA (la letra) ============
            // BRUMAFX DE LA CASA (puffs fBm que se desgarran — jamás
            // discos lisos): una nube BLANCA (la luz del exterior) y su
            // sombra MORADA (el conjuro), creciendo con el ritual y
            // ciñéndose en la contracción.
            try
            {
                float radioCuna = MathHelper.Lerp(30f, 95f, prog)
                    * (1f - 0.55f * contraccion);
                VFXCore.CerrarLoteSiAbierto();
                BrumaFX.BeginMass();
                BrumaFX.Cloud(posPantalla, radioCuna, new Color(205, 195, 238),
                    semilla, time,
                    Math.Max(4, (int)(7 * prog) + 3),
                    alpha: 0.24f + 0.38f * prog + 0.14f * contraccion);
                BrumaFX.Cloud(posPantalla + new Vector2(0f, -radioCuna * 0.2f),
                    radioCuna * 0.62f, new Color(86, 44, 150),
                    semilla + 29, time * 0.85f, 4,
                    alpha: 0.20f + 0.28f * prog + 0.10f * contraccion);
            }
            catch { }
            finally
            {
                try { Main.spriteBatch.End(); } catch { }
                VFXCore.CerrarLoteSiAbierto();
            }

            // ============ LOS CÍRCULOS RÚNICOS (la letra) ============
            // LA CORONA DEL HAZ (CoronaConjuro con la MISMA paleta del
            // conducto — la cura de la transparencia de la v6.59: nace al
            // 55 % y madura al 100 %, VISIBLE desde el primer tick).
            {
                float numP = MathF.Pow(prog, 0.8f);
                float breathe = 1f + 0.04f * MathF.Sin(time * 5.1f);
                float rCorona = MathHelper.Lerp(70f, 26f, numP) * breathe;
                try
                {
                    OrbitaLib.AbrirAdditive();
                    OrbitaLib.CoronaConjuro(posPantalla, rCorona, time,
                        alphaMul: 0.55f + 0.45f * prog, glyphMul: 1.8f,
                        blanco: Color.Lerp(
                            RayosPruebaProjectile.ColorConjuro(semilla + 6f),
                            new Color(255, 246, 255), 0.58f),
                        dorado: RayosPruebaProjectile.ColorConjuro(semilla + 18f),
                        violeta: RayosPruebaProjectile.ColorConjuro(semilla + 30f));
                    OrbitaLib.CerrarBatch();
                }
                catch
                {
                    VFXCore.CerrarLoteSiAbierto();
                }
                VFXCore.ReabrirLoteVanilla();
            }

            // ============ v6.61 — LOS ARCOS DEL ESCUADRÓN ============
            // (la letra: «los pequeños rayos electricos deben
            // concentrarce en el centro del circulo formado por los
            // minions, la forma para gatillar la creacion de pequeños
            // rayos electrico es la misma animacion de los minions,
            // ademas cada rayito debe ir de un minion a otro de forma
            // aleatorio mientras el haz se carga y se lanza»): los
            // rayitos YA NO nacen alrededor del centro — nacen DE LOS
            // OJOS QUE GIRAN: cada arco une DOS ojos del círculo (uno
            // al azar y su OPUESTO del otro lado — la cuerda CRUZA EL
            // MEDIO: la electricidad se CONCENTRA en el centro) y los
            // gatilla LA MISMA ANIMACIÓN de la órbita: viven desde que
            // el círculo gira, TODO el ritual (ceremonia y carga) — y
            // en el disparo los hereda el haz (su boca ES el centro).
            if (prog > 0.04f && edad % 5f < 3f)
            {
                DibujarArcosDelEscuadron(Projectile.Center, Projectile.owner,
                    prog, semilla);
            }

            // ============ LA IMPLOSIÓN DE SOLES DE LA CARGA ============
            // (desde que el círculo cerró: los soles rúnicos NACEN LEJOS y
            // caen al centro — LA CARGA AMADA del RC2, con el núcleo
            // blanco-violeta que aviva sin leerse amarillo.)
            VFXCore.CerrarLoteSiAbierto();
            VFXCore.Begin();
            try
            {
                if (edad > 100f)
                {
                    float progCarga = Utils.GetLerpValue(100f, T_TOTAL, edad, true);
                    HazConcentradoLib.SolesRunicosCarga(centro,
                        progCarga, time,
                        radioLejos: 320f, radioVar: 200f, corazon: 0.5f,
                        colorCorazon: CorazonRunico);
                }

                // LA SEMILLA DEL HAZ: la estrella de 4 puntas que CRECE en
                // el centro — lo que la ceremonia está gestando, lista
                // para el estallido del disparo.
                float progSemilla = Utils.GetLerpValue(30f, T_TOTAL, edad, true);
                if (progSemilla > 0.05f)
                {
                    float late = 0.60f + 0.40f * MathF.Sin(time * 9.3f);
                    HazConcentradoLib.SolRunico4Puntas(centro,
                        8f + 22f * progSemilla * progSemilla,
                        late * (0.35f + 0.65f * progSemilla),
                        corazon: 0.5f, colorCorazon: CorazonRunico);
                }
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.FlushAdditive();
            VFXCore.ReabrirLoteVanilla();

            VFXCore.RestaurarLoteVanilla();
        }
    }
}
