using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.Projectiles.Definitivas
{
    // =====================================================================
    //  v6.56.9 — LAS COPIAS DEL RC2: LA PILA DE CAPAS GPU-UNIVERSAL.
    // =====================================================================
    //
    //  v6.57 — LA VUELTA DEL HAZ C (el ganador de las tres copias, a
    //  pedido del usuario). LA LETRA, punto por punto:
    //  (0) LA IGNICIÓN A CIEGAS (v6.58 — la letra: «justo en el primer
    //      segundo de lanzar el rayo hay un pequeño destello blanco…
    //      eso no debería pasar… es solo de iluminación»): DOS fuentes
    //      de luz blanca morían en el arranque — el baño de AddLight +
    //      flash de pantalla del GolpeDeCine de la ignición (blanco
    //      255,248,235 ×2.75) y EL DESTELLO sprite (la estrella
    //      DestelloFinal a 0.9 de alfa durante 8 ticks en la boca).
    //      AMBAS MUEREN: la ignición va con destello: false (ImpactoLib
    //      — quedan el temblor suave y el hitstop) y el bloque del
    //      sprite se borra ENTERO. El momento se lee por el MOVIMIENTO
    //      (temblor + hitstop + estallido de soles), no por la luz.
    //  (0.b) EL HAZ SOBRE TODO EL MUNDO (v6.58 — la letra: «algunas
    //      sprites del juego como las hojitas que caen de los árboles
    //      se ven sobre el ataque y eso no debe ser así, el ataque debe
    //      estar sobre todo»): los proyectiles se dibujan ANTES del
    //      polvo/gore/hojas de vanilla — TODO lo que cae después del
    //      pase de proyectiles tapa el conducto. LA CURA: el dibujado
    //      entero (boca + pila) se muda a RayosPruebaEncimaSistema
    //      (PostDrawInterface — ENCIMA de todo el mundo, el patrón
    //      PantallaSistema/OcasoSystem de la casa); este PreDraw ya no
    //      dibuja NADA. Misma geometría, mismo volumen, otro punto del
    //      frame: cero costo nuevo, cero picos nuevos.
    //  (1) LOS CÍRCULOS AMARILLOS («son como circulos amarillos que
    //      aparecen a veces y son demasiado grandes, no deben
    //      aparecer»): era EL CORAZÓN DORADO del sol rúnico — un Quad
    //      de glow radial a ×1.5 del tamaño: a escala de haz (boca
    //      ~57 px, márgenes ~60 px) se lee como un círculo amarillo
    //      gigante. LA LEY DEL CORAZÓN DORADO: los haces dibujan sus
    //      soles con corazon: 0 — queda la estrella blanca de 4 puntas
    //      pura (el accesorio BrilloSolarHalo conserva SU corazón a SU
    //      escala; el RC2 original tampoco se toca).
    //  (2) EL ALMA — LA TEXTURA BLANCA A TODO EL ANCHO («veo una especia
    //      de textura blanca en el centro… debe tener otra textura como
    //      esa pero que ocupe todo el haz en ancho»): la MISMA RampaGlow
    //      gaussiana del núcleo, abierta a ×0.88..0.96 del cuerpo — el
    //      haz entero viste la tela blanca incandescente y la vena del
    //      núcleo arde ENCIMA. La pila ahora son SIETE capas.
    //  (3) LOS SOLES NACEN MÁS LEJOS («debe aparecer mas lejos del
    //      jugador, y a medida que se carga el ataque, estos caen hacia
    //      el centro»): la banda de nacimiento pasa de 150..240 px a
    //      320..520 px — la implosión cae desde el borde lejano del
    //      conjuro (y sigue cayendo recta, sin dar vueltas).
    //  (4) EL TEMBLOR SUAVE DEL LANZAMIENTO («al lanzar el haz la
    //      pantalla debe temblar un poco… un temblor de unos pocos
    //      pixeles y con movimiento suaves»): la ignición va por LA VÍA
    //      DULCE de ImpactoLib (Pantalla.Sacudir — 3.2 px de DOBLES
    //      SENOS suaves, nada de kick seco) y el temblor sostenido
    //      baja la voz (6.5 → 2.6 px) para que el momento del disparo
    //      SE LEA.
    //  (5) LA CORONA VISTE EL HAZ («el color de los circulos runicos
    //      debe ser el mismo que el del haz C»): las tres vueltas de la
    //      corona viajan LA PALETA DEL CONJURO (la misma del conducto:
    //      violeta arcano → morado profundo → rojo sangre → magenta),
    //      cada aro en su fase del ciclo — antes iban blanco/dorado/
    //      violeta fijos y se leían ajenas al haz.
    //  (6) LA AGUJA Y LA ESTRANGULACIÓN («al final del haz este como que
    //      se hace estrecho pero no lo suficiente… al terminar de
    //      lanzar el haz este debe hacerse estrecho y luego
    //      desaparecer»): la punta se afila de ×0.32 a ×0.06 (LA AGUJA)
    //      y en la ventana de muerte TODO el conducto colapsa a ×0.07
    //      antes de apagarse (LA ESTRANGULACIÓN) — el haz se cierra
    //      como una boca y muere. Curvas monótonas suaves: cero picos
    //      por construcción (misma polilínea, mismo pipeline vanilla).
    //  LA LETRA del usuario: «lo que le faltan son varias capas de
    //  texturas con mas difuminado, glow entre demas efectos visuales…
    //  copia ese rc2 en varias armas con los cambios diseñados asi no
    //  dañamos el codigo original y cambiamos las copias hasta tener la
    //  correcta».
    //
    //  EL DIAGNÓSTICO (forense sobre la captura del jugador, píxel a
    //  píxel): el RC2 de la v6.56.8 dibuja TODO su universo visual en UN
    //  SOLO strip — cuerpo, venas, núcleo y halo viven PER-PIXEL dentro
    //  del mismo polígono. La medición: el núcleo saturado ocupa el 99 %
    //  del halo medido (núcleo 412 px vs halo 424 px en la boca) — NO
    //  EXISTE envolvente luminosa fuera de la banda. Por eso el ojo lee
    //  «una sola capa»: por más riqueza per-pixel que tenga el shader,
    //  la SILUETA del haz es una banda sola de borde firme. El shader NO
    //  está roto — le falta MUNDO alrededor.
    //
    //  LA CURA — LA PILA DE CAPAS: la MISMA polilínea de 1500 puntos de
    //  la técnica 607df36 se dibuja SIETE veces, de atrás hacia adelante,
    //  cada una con su propio ancho y su propio color:
    //    (1) LA BRUMA — ×2.6..3.6, RampaGlow, violeta profundo al 9-20%:
    //        el resplandor AMBIENTAL que tiñe el fondo (el bloom).
    //    (2) EL GLOW — ×1.75..2.5, RampaGlow, la paleta del conjuro
    //        VIAJANDO al 20-28%: la envolvente difusa del conducto.
    //    (3) EL GLOW² (receta C) — una segunda envolvente interior.
    //    (4) EL CUERPO — el strip del HazConcentradoShader EXACTO del
    //        RC2 (el esqueleto 607df36, INTACTO): el plasma que hierve.
    //    (5) EL ALMA (v6.57) — ×0.88..0.96, la MISMA RampaGlow del
    //        núcleo ABIERTA A TODO EL ANCHO: la tela blanca del haz.
    //    (6) EL NÚCLEO — ×0.28..0.42, RampaGlow, blanco-violeta al
    //        65-95%: la vena ardiente ENCIMA del alma.
    //    (7) LOS SOLES CONDUCTO — los destellos de 4 puntas del
    //        accesorio corriendo por las márgenes del haz hacia la punta
    //        (v6.57: SIN corazón dorado — la ley del corazón).
    //
    //  POR QUÉ NO PUEDE PRODUCIR PICOS (la ley §5.4 del códice): las
    //  capas 1, 2, 3 y 5 se dibujan con el SHADER VANILLA de sprites
    //  (Main.pixelShader) + RampaGlow — el perfil gaussiano de la rampa
    //  (0 en el borde, pico al centro, medido: 24/255 a V=0.25) funde
    //  cada borde en CURVA por píxel. Todas las capas siguen la MISMA
    //  polilínea (mismos arrays de 1500 puntos, solo cambia el factor de
    //  ancho) — no hay esquinas poligonales duras (los picos de la
    //  v6.56.6 nacieron de los QUADS de RayoStrip y el remolino binario
    //  visto crudo, jamás del strip).
    //
    //  GPU-UNIVERSAL (la letra: «necesito que cualquier gpu incluso los
    //  que no son dedicadas deben ver el shader»): las capas 1, 2, 3, 5,
    //  6 y 7 usan el pipeline de sprites VANILLA — el mismo que dibuja
    //  todo Terraria, presente en CUALQUIER GPU (integrada, vieja, lo
    //  que sea). El .fx del cuerpo es un PLUS: si carga, el plasma
    //  hierve; si no carga, el cuerpo cae a una tira de CarnePlasma por
    //  el shader vanilla y LA PILA SE VE IGUAL — siete capas visibles en
    //  cualquier máquina, shader o no shader.
    //
    //  LAS TRES RECETAS (la letra: «cambiamos las copias hasta tener la
    //  correcta»): A = LA EQUILIBRADA (el diseño base), B = EL TITÁN
    //  (bruma y glow al máximo, el haz más gordo), C = EL SUEÑO (el
    //  desenfoque máximo, doble glow, núcleo suave). La receta viaja en
    //  ai[1] (la lección de la .56.6: Projectile.ai solo tiene 3 slots
    //  en este tML).
    //
    //  LO DEMÁS ES EL RC2 (conducta intacta): 300 de carga + 800 de
    //  láser, el cono de 3500, el barrido cosenoidal, el temblor
    //  sostenido, la música al 30 %, el vuelo infinito, los pozos
    //  cero-GC y la implosión de soles de la carga (v6.56.9: los soles
    //  NACEN LEJOS y CAEN RECTOS al centro, sin dar vueltas).
    // =====================================================================
    public class RayosPruebaProjectile : ModProjectile
    {
        // LOS TIEMPOS DEL ATAQUE (idénticos al RC2). v6.59 — ChargeTime
        // es internal: la CEREMONIA de la Ira lo usa para engendrar el
        // haz YA EN el disparo (Time = ChargeTime: la ceremonia FUE la
        // carga; el haz nace disparando).
        internal static readonly int ChargeTime = 300;
        private static readonly int LaserDuration = 800;
        private const float LargoCono = 3500f;

        // v6.59 — EL NÚCLEO BLANCO-VIOLETA de los soles del haz (la letra:
        // «los brillitos ahora casi no se ven»): el corazón que AVIVA el
        // centro de cada sol sin leerse como el círculo amarillo de la
        // v6.57 — blanco-violáceo incandescente, a media escala.
        private static readonly Color CorazonRunico = new Color(240, 228, 255);

        public float targetRotation;
        public float targetSize;
        public float realSize;

        // v6.55.1 — LA BANDERA DE FALLO DEL SHADER (el contrato del RC2).
        private static bool _shaderFallo;
        private static Effect _shaderCache;

        internal SonidoEnLoop raySound;
        public float volume;
        public float pitch;

        private ReLogic.Utilities.SlotId _cargaSlot;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        // ================================================================
        //  v6.59.1 — LA RECETA ÚNICA — C: EL SUEÑO (la letra: «me quedare
        //  con rayo de prueba C el sueño, el resto los puedes borrar»):
        //  las recetas A (LA EQUILIBRADA) y B (EL TITÁN) MURIERON con sus
        //  armas — sus columnas se BORRAN de los planos. Estos son LOS
        //  VALORES DE LA C, exactos, como constantes únicas.
        // ================================================================
        private const float KBruma = 3.60f;
        private const float AlfaBruma = 0.20f;
        private const float KGlow = 2.50f;
        private const float AlfaGlow = 0.24f;
        private const float KGlow2 = 1.90f;
        private const float AlfaGlow2 = 0.12f;
        private const float KNucleo = 0.28f;
        private const float AlfaNucleo = 0.65f;
        // v6.60.2 — SolesConducto (6) MURIÓ con los soles del conducto
        // (la letra: las estrellitas solo en la fase de carga).

        // v6.57 — EL ALMA (la letra: «debe tener otra textura como esa
        // pero que ocupe todo el haz en ancho»): la MISMA RampaGlow
        // gaussiana del núcleo (la «textura blanca» que el usuario ve en
        // el centro) ABIERTA A TODO EL ANCHO del conducto — el haz
        // entero viste la tela blanca incandescente y la vena arde
        // ENCIMA. Misma polilínea, mismo shader vanilla: cero picos.
        private const float KAlma = 0.96f;
        private const float AlfaAlma = 0.30f;

        // LA PALETA DEL CONJURO (la del RC2, intacta): violeta arcano →
        // morado profundo (el «negro») → rojo sangre → magenta, viajando.
        private static readonly Color[] PaletaConjuro =
        {
            new(150, 62, 235),    // violeta arcano
            new(104, 34, 182),    // morado profundo — el «negro» del conjuro
            new(208, 46, 88),     // rojo sangre
            new(178, 52, 214),    // magenta del conjuro
        };

        private const float CicloConjuro = 48f;

        public static Color ColorConjuro(float reloj)
        {
            float t = reloj % CicloConjuro;
            if (t < 0f) t += CicloConjuro;
            float seg = t / 12f;
            int i = (int)seg % PaletaConjuro.Length;
            int j = (i + 1) % PaletaConjuro.Length;
            return Color.Lerp(PaletaConjuro[i], PaletaConjuro[j], seg - (int)seg);
        }

        public ref float Time => ref Projectile.ai[0];
        public ref float Owner => ref Projectile.ai[2];

        // v6.59.1 — ai[1] (LA RECETA) MURIÓ: las recetas A y B se
        // borraron con sus armas y la C es LA ÚNICA — el slot viaja en
        // los engendros como legado (nadie lo lee) y YA NO se serializa
        // en el ExtraAI.

        // ================================================================
        //  v6.59 — EL MODO IRA DE UN SER EXTERIOR: cuando EL GRIMORIO
        //  dispara este haz (la ceremonia del despertar 50), el conducto
        //  NO vive en la boca del jugador — vive en EL CENTRO DEL CÍRCULO
        //  que el escuadrón formó y NO encadena al portador (el jugador
        //  queda LIBRE: la ira es del Ser Exterior, no del que lo porta).
        //  v6.60.2 — EL RUMBO ES DEL JUGADOR (la letra: «el haz debe
        //  apuntar a donde vaya el cursor y siempre seguir el cursor,
        //  creo que asi sera mejor»): la boca queda FIJA en el centro,
        //  pero el conducto SIGUE AL CURSOR del dueño TODO el disparo —
        //  el blanco-perseguido de la v6.59 murió con la letra nueva
        //  (BlancoIra ya no viaja: el rumbo se sincroniza solo).
        //  Viaja por SendExtraAI (el paquete del spawn).
        // ================================================================
        internal bool EsIra;
        internal Vector2 CentroIra = Vector2.Zero;

        /// <summary>v6.59 — EL BLANCO DE LA IRA (la letra: «si hay un
        /// jefe presente este siempre tendra prioridad por sobre todas
        /// las cosas»): el JEFE más cercano manda sobre todo; sin
        /// jefes, el enemigo válido más cercano. Lo usan la ceremonia
        /// (al elegir presa) y el haz (al apuntar y re-adquirir).</summary>
        internal static NPC BuscarBlancoIra(Vector2 desde, float radio)
        {
            NPC mejorJefe = null;
            float dJ = radio * radio;
            NPC mejor = null;
            float dM = radio * radio;
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n == null || !n.active) continue;
                if (!VFXCore.EsObjetivo(n)) continue;
                // v6.50.116 — EL SELLO VERDADERO: los jefes de oleada
                // mueren AISLADOS (tipo/netID en 0) pero siguen siendo
                // jefes para TODO lo que importa.
                bool esJefe = n.boss || Globals.OleadaNPC.JefeVerdadero(n);
                float d = n.Center.DistanceSQ(desde);
                if (esJefe)
                {
                    if (d < dJ) { dJ = d; mejorJefe = n; }
                }
                else if (d < dM) { dM = d; mejor = n; }
            }
            return mejorJefe ?? mejor;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 5000;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
            Projectile.manualDirectionChange = true;
            Projectile.netImportant = true;
            raySound = null;
            _cargaSlot = default;
        }

        // LOS POZOS DEL RC2 (cero-GC): la MISMA polilínea de 1500 puntos
        // alimenta TODAS las capas de la pila.
        private static readonly Vector2[] _cachePuntos = new Vector2[1500];
        private static readonly float[] _cacheRotas = new float[1500];
        private static readonly VertexStrip _strip = new VertexStrip();

        // EL ESTADO DE FRAME que leen los delegados de cada capa.
        private float _laserActivo;
        private float _kCapa;

        private static Effect ObtenerShader()
        {
            if (_shaderFallo)
                return null;
            if (_shaderCache != null && !_shaderCache.IsDisposed)
                return _shaderCache;
            try
            {
                _shaderCache = ModContent.Request<Effect>(
                    "AethonMod/Content/Effects/Shaders/HazConcentradoShader",
                    ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                return _shaderCache;
            }
            catch
            {
                _shaderFallo = true;
                return null;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.direction == 1);
            writer.Write(targetRotation);
            // v6.59 — EL MODO IRA: la bandera y el centro del ritual viajan
            // en el paquete del spawn. v6.60.2 — el blanco YA NO viaja: el
            // rumbo es del cursor y targetRotation se sincroniza sola.
            writer.Write(EsIra);
            writer.Write(CentroIra.X);
            writer.Write(CentroIra.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.direction = reader.ReadBoolean() ? 1 : -1;
            targetRotation = reader.ReadSingle();
            // v6.59 — EL MODO IRA (el mismo orden del Send).
            EsIra = reader.ReadBoolean();
            CentroIra = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        // ================================================================
        //  EL CORAZÓN — la IA del haz (idéntica al RC2).
        // ================================================================
        public override void AI()
        {
            if (Owner < 0 || Owner >= Main.maxPlayers)
            {
                CortarCarga();
                Projectile.Kill();
                return;
            }
            Player dueno = Main.player[(int)Owner];
            // v6.59 — LA IRA NO MUERE CON SU PORTADOR: un hechizo empezado
            // se cumple (la regla de la casa de la v6.51.5) — la ira es del
            // Ser Exterior, no del que lo porta. El arma de siempre sigue
            // muriendo con su dueño (conducta intacta).
            if (dueno == null || !dueno.active || dueno.dead)
            {
                if (!EsIra)
                {
                    CortarCarga();
                    Projectile.Kill();
                    return;
                }
            }

            if (EsIra)
            {
                // ============================================================
                //  v6.62 — LA BOCA VIAJA CON SU PORTADOR (la letra: «otro
                //  detalle importante es que cuando se lanza el haz por
                //  los minions, si el personaje se mueve los minions no lo
                //  siguen, el jugador se mueve y los minions se quedan
                //  quieto en su sitio disparando el haz sin moverse con
                //  el jugador»): la boca del Ser Exterior YA no queda
                //  CLAVADA al punto donde nació — SIGUE AL DUEÑO con el
                //  MISMO lerp de la ceremonia (0,18/tick: el círculo
                //  entero, que orbita ESTE punto — RitualDe cae a él —,
                //  viaja con el jugador y el conducto nace de donde el
                //  círculo ESTÁ, no de donde estaba). El portador sigue
                //  LIBRE (nada de cadenas ni empujones): solo su CÍRCULO
                //  lo acompaña. El dueño caído: la boca flota en su
                //  último punto (un hechizo empezado se cumple — la regla
                //  de la casa de la v6.51.5).
                // ============================================================
                bool duenoVivo = dueno != null && dueno.active && !dueno.dead;
                if (duenoVivo)
                {
                    Vector2? libro = global::AethonMod.Content.Projectiles.OjoEternoMinion
                        .PosicionDelLibro(dueno);
                    Vector2 objetivo = libro is Vector2 c
                        ? c
                        : dueno.MountedCenter + new Vector2(0f, -100f);
                    CentroIra = Vector2.Lerp(CentroIra, objetivo, 0.18f);
                }
                Projectile.Center = CentroIra;
            }
            else
            {
                dueno.direction = Math.Sign(Projectile.rotation.ToRotationVector2().X);
                dueno.itemLocation = dueno.MountedCenter;

                Projectile.Center = dueno.MountedCenter + new Vector2(13 * dueno.direction, -20f);

                dueno.velocity += Projectile.rotation.ToRotationVector2() * -0.1f;
                dueno.velocity *= 0.98f;
            }

            if (!Main.dedServ)
            {
                if (Time == 2f)
                {
                    SoundStyle carga = new SoundStyle("AethonMod/Content/Sounds/RayoConcentradoCarga", SoundType.Sound)
                    {
                        MaxInstances = 1,
                        Volume = 1f
                    };
                    try
                    {
                        _cargaSlot = SoundEngine.PlaySound(carga, Projectile.Center);
                    }
                    catch { }
                }

                if (Time > ChargeTime && Time < ChargeTime + LaserDuration)
                {
                    // v6.57 — EL TEMBLOR SOSTENIDO BAJA LA VOZ (6.5 → 2.6
                    // px): antes la pantalla temblaba fuerte DURANTE todo
                    // el disparo y el momento del LANZAMIENTO no se leía
                    // — ahora la conversación sostenida es un susurro y
                    // el golpe suave de la ignición ES el momento.
                    Pantalla.Sacudir(2.6f, 0.4f, "RC2P-sostenido");
                }
                if (Time == ChargeTime + 1)
                {
                    // v6.58 — LA IGNICIÓN A CIEGAS (la letra: «justo en el
                    // primer segundo de lanzar el rayo hay un pequeño
                    // destello blanco… no debería estar ahí… es solo de
                    // iluminación»): el golpe de cine SIN destello — ni
                    // el flash blanco de pantalla ni el baño de AddLight
                    // (los tres focos blancos ×2.75 que iluminaban el
                    // mundo un frame). QUEDAN el temblor suave de la vía
                    // dulce (3.2 px de dobles senos) y el hitstop de
                    // TiemposLib: el momento del disparo se lee por el
                    // MOVIMIENTO, no por la luz.
                    ImpactoLib.GolpeDeCine(Projectile.Center, 1.25f, 6, suave: true, destello: false);
                }
            }

            float totalOffRot = (float)Math.Cos((Time - ChargeTime) / LaserDuration * MathHelper.TwoPi) * 2.5f
                * Utils.GetLerpValue(ChargeTime * 0.8f, ChargeTime + LaserDuration * 0.1f, Time, true)
                * Utils.GetLerpValue(ChargeTime + LaserDuration, ChargeTime + LaserDuration * 0.9f, Time, true);

            // v6.61 — LA IRA NO ECHA MONEDAS AL AIRE (la letra: «el haz
            // debe seguir al cursor siempre, pero actualmente solo da
            // vueltas aleatoriamente lo cual esta mal»): la DIRECCIÓN AL
            // AZAR que se sorteaba cada 15 ticks era UNO de los dos
            // tiranos del vaivén (el otro: el totalOffRot de ±2,5 rad —
            // ±143° — del arma, ABAJO) — la ira NO la sortea: su rumbo
            // es SOLO el cursor. El sync del rumbo viaja cada 15 ticks
            // por el canal de siempre (los demás cables ven la vuelta).
            if (EsIra)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && Time % 15f == 0f)
                    Projectile.netUpdate = true;
            }
            else if (Main.netMode != NetmodeID.MultiplayerClient && Time % 15f == 0f
                && Time < ChargeTime + LaserDuration * 0.6f)
            {
                Projectile.direction = Main.rand.NextBool() ? -1 : 1;
                Projectile.netUpdate = true;
            }

            // ============================================================
            //  v6.60.2 — EL RUMBO ES DEL JUGADOR (la letra: «el haz debe
            //  apuntar a donde vaya el cursor y siempre seguir el cursor,
            //  creo que asi sera mejor»): LA IRA YA NO PERSIGUE BLANCOS —
            //  su conducto SIGUE AL CURSOR del dueño TODO el disparo. La
            //  boca de la ira queda FIJA en el centro del círculo; el
            //  RUMBO es la mano del portador. Corre SOLO en la máquina
            //  del dueño (el cursor es SU ratón — los demás cables ven
            //  el rumbo en el sync de siempre).
            // ============================================================
            if (Main.netMode != NetmodeID.Server && Main.myPlayer == Projectile.owner)
            {
                if (EsIra)
                {
                    // ========================================================
                    //  v6.61 — CLAVADO AL CURSOR, SIN TITEREADAS: la cura
                    //  de la v6.60.2 dejó DOS tiranos vivos — el VAIVÉN
                    //  del arma (totalOffRot ±2,5 rad × la dirección que
                    //  se SORTEABA cada 15 ticks: el conducto BAILABA
                    //  alrededor del cursor hasta ±143°) y la persecución
                    //  LENTA (tasas 0,04→0,01: tardaba SEGUNDOS en
                    //  alcanzar al ratón). AHORA: el rumbo PERSIGUE al
                    //  cursor a MEDIA VUELTA por frame (llega en 2-3
                    //  frames y se CLAVA al llegar) y la rotación ES el
                    //  rumbo — SIN vaivén, SIN azar: donde está el
                    //  cursor, está el haz. (La guarda de los 10 px: con
                    //  el cursor ENCIMA de la boca el ángulo no existe —
                    //  el haz conserva su última mirada en vez de dar
                    //  bandazos de 180°.)
                    // ========================================================
                    Vector2 alCursor = Main.MouseWorld - Projectile.Center;
                    if (alCursor.LengthSquared() > 100f)
                    {
                        float objetivo = alCursor.ToRotation();
                        targetRotation = targetRotation.AngleLerp(objetivo, 0.55f);
                        if (Math.Abs(MathHelper.WrapAngle(objetivo - targetRotation)) < 0.02f)
                            targetRotation = objetivo;
                    }
                }
                else
                {
                    targetRotation = targetRotation.AngleLerp(Projectile.AngleTo(Main.MouseWorld),
                        Utils.GetLerpValue(ChargeTime + LaserDuration * 0.1f, ChargeTime, Time, true) * 0.04f
                        + Utils.GetLerpValue(ChargeTime + LaserDuration * 0.3f, ChargeTime + LaserDuration * 0.6f, Time, true) * 0.01f);
                }
            }

            targetSize = (float)Math.Pow(
                Utils.GetLerpValue(ChargeTime - 20, ChargeTime + 60, Time, true) *
                Utils.GetLerpValue(ChargeTime + LaserDuration + 60, ChargeTime + LaserDuration, Time, true), 3f);

            float waveChange = MathHelper.SmoothStep(0f, 1f,
                Utils.GetLerpValue(ChargeTime * 0.8f, ChargeTime + 20, Time, true) *
                Utils.GetLerpValue(ChargeTime + LaserDuration * 0.9f, ChargeTime + LaserDuration * 0.5f, Time, true));
            // v6.61 — LA IRA NO VAGUEA: el conducto ES el rumbo del
            // cursor — el vaivén (totalOffRot × dirección × ola) es la
            // firma del ARMA de siempre, NO de la ira.
            Projectile.rotation = EsIra
                ? targetRotation
                : targetRotation + totalOffRot * Projectile.direction * waveChange;

            realSize = MathHelper.Lerp(realSize, targetSize, 0.1f);
            Projectile.velocity = Projectile.rotation.ToRotationVector2();

            Projectile.localAI[0] += 1f;

            if (!Main.dedServ && Time > ChargeTime - 10 && Time < ChargeTime + LaserDuration)
            {
                Color cLuz = ColorConjuro(Projectile.localAI[0]);
                float k = realSize;
                Vector2 dir = Projectile.rotation.ToRotationVector2();
                Lighting.AddLight(Projectile.Center,
                    cLuz.R / 255f * 1.8f * k, cLuz.G / 255f * 1.7f * k, cLuz.B / 255f * 1.5f * k);
                Lighting.AddLight(Projectile.Center + dir * 1100f,
                    cLuz.R / 255f * 1.4f * k, cLuz.G / 255f * 1.3f * k, cLuz.B / 255f * 1.1f * k);
                Lighting.AddLight(Projectile.Center + dir * 2400f,
                    cLuz.R / 255f * 1.0f * k, cLuz.G / 255f * 0.9f * k, cLuz.B / 255f * 0.8f * k);
            }

            if (Time == ChargeTime + LaserDuration + 20)
            {
                DefinitivasLib.Sonar(SoundID.Item34.WithPitchOffset(-0.4f), Projectile.Center);
                OndaLib.Kick(6f, 10);
            }

            Time++;
            Projectile.timeLeft = 10;

            HandleSound();

            if (Time > ChargeTime + LaserDuration + 50)
            {
                raySound?.StopSound();
                Projectile.Kill();
                return;
            }

            // v6.59 — LA IRA NO REGALA EL VUELO: el vuelo infinito es la
            // experiencia del ARMA (el conjuro del jugador); el Ser
            // Exterior no sostiene a su portador — él vuela solo.
            if (!EsIra)
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    Player p = Main.player[(int)Owner];
                    if (p != null && p.active && !p.dead)
                        p.wingTime = p.wingTimeMax;
                }
                else if (Main.LocalPlayer != null && Main.LocalPlayer.whoAmI == (int)Owner)
                {
                    Main.LocalPlayer.wingTime = Main.LocalPlayer.wingTimeMax;
                }
            }
        }

        public void HandleSound()
        {
            if (Main.dedServ)
                return;
            volume = 1.3f * Utils.GetLerpValue(0f, 0.05f, realSize, true);
            pitch = MathHelper.Lerp(-0.22f, -0.7f, 1f - Utils.GetLerpValue(ChargeTime + LaserDuration + 60, ChargeTime + LaserDuration, Time, true));

            raySound ??= new SonidoEnLoop(
                new SoundStyle("AethonMod/Content/Sounds/RayoConcentradoLoop") { MaxInstances = 0 },
                new ProjectileAudioTracker(Projectile).IsActiveAndInGame);
            raySound.PlaySound(() => Projectile.Center, () => volume, () => pitch);
        }

        private void CortarCarga()
        {
            try
            {
                if (_cargaSlot.IsValid && SoundEngine.TryGetActiveSound(_cargaSlot, out ActiveSound sonidoActivo) && sonidoActivo != null)
                    sonidoActivo.Stop();
            }
            catch { }
            _cargaSlot = default;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time > ChargeTime + 20 && Time < ChargeTime + LaserDuration + 10)
            {
                float grow = (float)Math.Pow(
                    Utils.GetLerpValue(ChargeTime - 20, ChargeTime + 40, Time, true) *
                    Utils.GetLerpValue(ChargeTime + LaserDuration + 60, ChargeTime + LaserDuration, Time, true), 3f);
                return DefinitivasLib.Cono(targetHitbox, Projectile.Center, LargoCono * (0.5f + grow * 0.5f),
                    Projectile.rotation, 0.28f * Math.Max(grow, 0.02f));
            }
            return false;
        }

        // LA MÚSICA −30 % MIENTRAS VIVE (el contrato del RC2).
        public override void Load()
        {
            On_Main.UpdateAudio += CallarMusica;
        }

        public override void Unload()
        {
            On_Main.UpdateAudio -= CallarMusica;
        }

        private void CallarMusica(On_Main.orig_UpdateAudio orig, Main self)
        {
            orig(self);

            Projectile projectile = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i] != null && Main.projectile[i].active && Main.projectile[i].type == Type)
                {
                    projectile = Main.projectile[i];
                    break;
                }
            }
            if (projectile == null) return;

            for (int i = 0; i < Main.musicFade.Length; i++)
            {
                float grow = Utils.GetLerpValue(ChargeTime - 15, ChargeTime + 40, projectile.ai[0], true)
                    * Utils.GetLerpValue(ChargeTime + LaserDuration + 50, ChargeTime + LaserDuration, projectile.ai[0], true);

                float vol = Main.musicFade[i] * Main.musicVolume * (1f - grow * 0.3f);
                float tempFade = Main.musicFade[i];
                Main.audioSystem.UpdateCommonTrackTowardStopping(i, vol, ref tempFade, Main.musicFade[i] > 0.15f);
                Main.musicFade[i] = tempFade;
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
            => overPlayers.Add(index);

        // ================================================================
        //  EL DIBUJO — LA BOCA DEL CONJURO (idéntica al RC2: implosión de
        //  soles + corona rúnica + micro-arcos + estrella conducto) y LA
        //  PILA DE CAPAS en el conducto.
        //
        //  v6.58 — ESTE CUERPO VIVE ENCAPSULADO EN DibujarAtaque(): lo
        //  llama RayosPruebaEncimaSistema desde PostDrawInterface (encima
        //  de TODO el mundo — hojas, polvos y gore ya no pueden tapar el
        //  conducto). El PreDraw de abajo NO dibuja nada.
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            // v6.58 — EL HAZ VIVE ENCIMA DE TODO (la letra: «el ataque debe
            // estar sobre todo»): el pase de proyectiles corre ANTES del
            // polvo/gore/hojas de vanilla y esas sprites caían ENCIMA del
            // conducto. Ya no se dibuja aquí NADA — todo vive en
            // RayosPruebaEncimaSistema.PostDrawInterface, sobre el mundo
            // entero, con la MISMA matemática y el MISMO volumen de
            // dibujado (cero costo nuevo, cero picos nuevos).
            return false;
        }

        /// <summary>EL DIBUJO COMPLETO DEL ATAQUE (la boca + la pila de
        /// capas) — lo llama RayosPruebaEncimaSistema desde su
        /// PostDrawInterface, ENCIMA de todo el mundo. Contrato de lote:
        /// entra con un lote de mundo ABIERTO y sale dejando el lote en
        /// estado vanilla válido (RestaurarLoteVanilla).</summary>
        internal void DibujarAtaque()
        {
            Texture2D texture = DefinitivasLib.TexRuido;
            Texture2D textureBits = DefinitivasLib.TexBits;
            Texture2D textureGlow = DefinitivasLib.TexRampa;
            Texture2D textureSecond = DefinitivasLib.TexRemolino;
            Texture2D ring = VFXCore.Ring;

            if (textureGlow == null || ring == null)
                return;

            float laserCharge = Utils.GetLerpValue(50, ChargeTime, Time, true)
                * Utils.GetLerpValue(ChargeTime + LaserDuration + 50, ChargeTime + LaserDuration + 20, Time, true);

            Vector2 posPantalla = Projectile.Center - Main.screenPosition;

            // ============ v6.63 — LA BRUMA DEL DISPARO: LA CINTA QUE NO
            //            TERMINA · v6.64 — LA CUNA QUE VISTE LOS
            //            CÍRCULOS + EL VELO DEL LANZAMIENTO ============
            // (la letra v6.63: «en el momento de naciemiento del haz la
            // bruma colapsa en el centro, pero una vez es lanzado el haz
            // ya no hay mas bruma y la bruma deberia seguir colapsando
            // hacia el centro y ademas un poco de bruma tambien debe
            // cubrir el centro de naciemiento del haz, y en todo momento
            // mas bruma colapsa hacia el centro, toda la bruma solo puede
            // estar dentro del circulo formado por los minions al igual
            // que los pequeños rayos electricos»):
            //   · LA CUNA (v6.64 REFORZADA — la letra: «le falta bruma en
            //     los circulos del centro»): la cuna era un susurro de
            //     19 px — MÁS CHICA que los círculos rúnicos que vestía
            //     (corona 26 px + aro 36 px con el anillo a ~56 px).
            //     AHORA VISTE los círculos (×0,60 del anillo, puffs 5→7,
            //     alfa 0,20→0,30) y lleva EL CORAZÓN: una capa interior
            //     más blanca que DENSIFICA el medio, TODO el disparo.
            //   · EL VELO DEL LANZAMIENTO (v6.64 — la letra: «la bruma
            //     que esta en el centro debe cubrir el punto de
            //     nacimiento en el momento en el que el haz es
            //     lanzado»): los primeros 55 ticks, el punto donde el
            //     haz NACE queda CUBIERTO — el velo grande que se cierra
            //     sobre la boca + el corazón blanco encima del punto
            //     exacto — y se DISUELVE en la cuna de siempre.
            //   · LA CINTA CONTINUA — en TODO momento nace bruma nueva
            //     dentro del anillo y CAE al centro (succión que se apura
            //     t²; el alfa se entrega EN LA LLEGADA 1−t³: la bruma SE
            //     VUELVE el haz) — un bucle sin fin con el reloj de cada
            //     pantalla.
            //   · EL TRAGO DEL NACIMIENTO — los primeros 48 ticks: la
            //     MITAD colapsa y la otra mitad SE DISUELVE EN SU SITIO
            //     (la v6.62 la echaba a volar FUERA del círculo — la ley
            //     nueva la detiene: NADA asoma del anillo).
            // Y LA LEY DEL CÍRCULO: el anillo lo pone EL PROPIO ESCUADRÓN
            // (CirculoDelEscuadron): las bocanadas NACEN dentro del
            // círculo de los minions y mueren en LA BOCA del haz (y el
            // velo, en su pico ×0,75 del anillo, alcanza ~0,80 del
            // círculo REAL — el margen del 0,82 lo cubre: jamás asoma).
            if (EsIra && Time >= ChargeTime && Time < ChargeTime + LaserDuration)
            {
                // EL CÍRCULO DE LOS MINIONS (la ley: la bruma vive DENTRO).
                IraSerExteriorCeremoniaProjectile.CirculoDelEscuadron(
                    (int)Owner, out Vector2 centroMin, out float radioMin, out int nMin);
                float anillo = Math.Max(40f, radioMin) * 0.82f;   // el techo del anillo
                Vector2 boca = posPantalla;                        // el centro de nacimiento
                Vector2 centroMinScr = nMin > 0 ? centroMin - Main.screenPosition : boca;

                // LA RESPIRACIÓN GLOBAL: nace con el disparo, se apaga
                // SUAVE en los últimos 90 ticks (nada de corte en seco).
                float aliento = Utils.GetLerpValue(ChargeTime, ChargeTime + 20f, Time, true)
                    * Utils.GetLerpValue(ChargeTime + LaserDuration,
                        ChargeTime + LaserDuration - 90f, Time, true);

                float tCol = Utils.GetLerpValue(ChargeTime, ChargeTime + 48f, Time, true);
                bool naciendo = Time < ChargeTime + 48f;
                // v6.64 — EL RELOJ DEL VELO: la ventana del lanzamiento
                // (55 ticks — un poco más larga que la del trago: el
                // velo ENTREGA la cobertura del nacimiento y se disuelve
                // suave en la cuna).
                float tVelo = Utils.GetLerpValue(ChargeTime, ChargeTime + 55f, Time, true);

                try
                {
                    VFXCore.CerrarLoteSiAbierto();
                    BrumaFX.BeginMass();

                    // ——— LA CUNA QUE VISTE LOS CÍRCULOS (v6.64 — la
                    //      letra: «le falta bruma en los circulos del
                    //      centro»): ×0,60 del anillo (≈34 px con el
                    //      escuadrón de 11 — VISTE la corona de 26 px y
                    //      su aro de 36 px), 7 puffs y alfa 0,30 TODO el
                    //      disparo (respirando, blanco-violeta) ———
                    BrumaFX.Cloud(boca, anillo * 0.60f, new Color(214, 205, 244),
                        77, Main.GlobalTimeWrappedHourly * 0.8f,
                        puffs: 7,
                        alpha: 0.30f * aliento);
                    // ——— EL CORAZÓN DE LA CUNA: la capa interior MÁS
                    //      BLANCA (×0,34) que DENSIFICA el medio de los
                    //      círculos — el centro HIERVE de bruma ———
                    BrumaFX.Cloud(boca, anillo * 0.34f, new Color(228, 220, 250),
                        83, Main.GlobalTimeWrappedHourly * 1.1f,
                        puffs: 4,
                        alpha: 0.24f * aliento);

                    // ——— EL VELO DEL LANZAMIENTO (v6.64 — la letra:
                    //      «la bruma que esta en el centro debe cubrir
                    //      el punto de nacimiento en el momento en el
                    //      que el haz es lanzado»): JUSTO al lanzarse,
                    //      el punto donde el haz nace queda CUBIERTO —
                    //      el VELO grande (×0,75→×0,55 cerrándose sobre
                    //      la boca, 9 puffs) y EL CORAZÓN BLANCO encima
                    //      del punto exacto (×0,40→×0,28) — y en ~0,9 s
                    //      se DISUELVE en la cuna de siempre. ———
                    if (tVelo < 1f)
                    {
                        float retirada = 1f - tVelo;
                        BrumaFX.Cloud(boca,
                            MathHelper.Lerp(anillo * 0.75f, anillo * 0.55f, tVelo),
                            new Color(218, 208, 246),
                            91, Main.GlobalTimeWrappedHourly * 1.4f,
                            puffs: 9,
                            alpha: 0.50f * retirada * (1f - 0.35f * tVelo) * aliento);
                        BrumaFX.Cloud(boca,
                            MathHelper.Lerp(anillo * 0.40f, anillo * 0.28f, tVelo),
                            new Color(232, 226, 252),
                            97, Main.GlobalTimeWrappedHourly * 1.7f,
                            puffs: 5,
                            alpha: 0.55f * retirada * retirada * aliento);
                    }

                    // ——— LA CINTA CONTINUA: diez bocanadas en bucle —
                    //      cada una NACE dentro del anillo de los
                    //      minions y CAE a la boca del haz (la succión
                    //      se apura: t²) — el alfa se entrega EN LA
                    //      LLEGADA (1−t³): la bruma SE VUELVE el haz.
                    //      El reloj de cada pantalla (nada que
                    //      sincronizar). ———
                    const int ciclo = 72;   // 1,2 s por bocanada
                    for (int i = 0; i < 10; i++)
                    {
                        int h = i * 37 + 11;
                        float desfase = VFXCore.Hash01(h, 5, 3) * ciclo;
                        float t = ((Main.GameUpdateCount % (ulong)ciclo) + desfase)
                            % ciclo / (float)ciclo;
                        float ang = VFXCore.Hash01(h, 7, 11) * MathHelper.TwoPi;
                        float r0 = anillo * (0.55f + 0.30f * VFXCore.Hash01(h, 13, 17));
                        Vector2 naceEn = centroMinScr +
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * r0;
                        Vector2 pos = Vector2.Lerp(naceEn, boca, t * t);
                        float radio = (13f + 9f * VFXCore.Hash01(h, 19, 23)) * (1f - 0.62f * t);
                        float nace = Utils.GetLerpValue(0f, 0.12f, t, true);
                        BrumaFX.Cloud(pos, Math.Max(5f, radio),
                            i % 2 == 0 ? new Color(212, 202, 244) : new Color(138, 86, 214),
                            h, Main.GlobalTimeWrappedHourly,
                            puffs: 2,
                            alpha: 0.42f * nace * (1f - t * t * t) * aliento);
                    }

                    if (naciendo)
                    {
                        // ——— LA MITAD QUE COLAPSA (el trago): ocho
                        //      bocanadas nacen en el anillo y caen a la
                        //      boca — el alfa se entrega EN LA LLEGADA
                        //      (1−t³). ———
                        for (int i = 0; i < 8; i++)
                        {
                            int h = i * 41 + 211;
                            float ang = VFXCore.Hash01(h, 5, 3) * MathHelper.TwoPi;
                            float r0 = anillo * (0.55f + 0.35f * VFXCore.Hash01(h, 7, 11));
                            Vector2 naceEn = centroMinScr +
                                new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * r0;
                            Vector2 pos = Vector2.Lerp(naceEn, boca, tCol * tCol);
                            float radio = (18f + 12f * VFXCore.Hash01(h, 13, 17))
                                * (1f - 0.68f * tCol);
                            BrumaFX.Cloud(pos, Math.Max(6f, radio),
                                i % 2 == 0 ? new Color(212, 202, 244) : new Color(138, 86, 214),
                                h, Main.GlobalTimeWrappedHourly,
                                puffs: 2 + (i & 1),
                                alpha: 0.50f * (1f - tCol * tCol * tCol));
                        }
                        // ——— LA MITAD QUE SE DISIPA: siete bocanadas SE
                        //      DISUELVEN EN SU SITIO (adentro del anillo
                        //      — la ley del círculo: nada sale volando).
                        for (int i = 0; i < 7; i++)
                        {
                            int h = 900 + i * 43 + 29;
                            float ang = VFXCore.Hash01(h, 5, 3) * MathHelper.TwoPi;
                            float r0 = anillo * (0.50f + 0.30f * VFXCore.Hash01(h, 7, 11));
                            Vector2 pos = centroMinScr +
                                new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * r0
                                * (1f - 0.18f * tCol);
                            float radio = (16f + 11f * VFXCore.Hash01(h, 13, 17))
                                * (1f + 0.35f * tCol);
                            BrumaFX.Cloud(pos, radio,
                                i % 2 == 0 ? new Color(196, 186, 232) : new Color(120, 72, 196),
                                h, Main.GlobalTimeWrappedHourly * 0.9f,
                                puffs: 2,
                                alpha: 0.34f * (1f - tCol) * (1f - tCol));
                        }
                    }
                }
                catch { }
                finally
                {
                    try { Main.spriteBatch.End(); } catch { }
                    VFXCore.CerrarLoteSiAbierto();
                }
            }

            // ============ LA IMPLOSIÓN DE SOLES DE LA CARGA ============
            // (v6.56.9 — la letra: «aparecer lejos y contraerse hacia el
            // centro pero sin dar vueltas»: cada sol nace LEJOS en su
            // ángulo FIJO y cae RECTO a la boca del arma).
            VFXCore.CerrarLoteSiAbierto();
            VFXCore.Begin();
            try
            {
                if (Time < ChargeTime)
                {
                    // v6.57 — LA IMPLOSIÓN NACE MÁS LEJOS (la letra: «el
                    // pequeño brillo de soles runicos debe aparecer mas
                    // lejos del jugador»): 320..520 px (antes 150..240).
                    // v6.59 — EL CORAZÓN QUE AVIVA SIN SER AMARILLO: un
                    // núcleo blanco-violeta a media escala (la cura de los
                    // brillitos apagados — la letra: «los brillitos ahora
                    // casi no se ven»; el círculo AMARILLO grande no vuelve).
                    HazConcentradoLib.SolesRunicosCarga(Projectile.Center,
                        Utils.GetLerpValue(20f, ChargeTime, Time, true),
                        Main.GlobalTimeWrappedHourly,
                        radioLejos: 320f, radioVar: 200f, corazon: 0.5f,
                        colorCorazon: CorazonRunico);
                }
                // v6.60.2 — EL ESTALLIDO DE SOLES MUERE (la letra: «las
                // estrellitas o brillo solo deben aparecer en la fase de
                // carga, cuando el haz es lanzado no necesita cosas como
                // las estrellitas o brillo»): la implosión de la carga
                // (arriba) es la ÚLTIMA vez que se ven soles — el
                // lanzamiento es LIMPIO.
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.FlushAdditive();
            VFXCore.ReabrirLoteVanilla();

            // ============ LOS CÍRCULOS RÚNICOS (la corona del RC2) ============
            {
                float numP = MathF.Pow(laserCharge, 0.8f);
                float breathe = 1f + 0.04f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5.1f);
                float rCorona = MathHelper.Lerp(70f, 26f, numP) * breathe;
                try
                {
                    // v6.57 — LA CORONA VISTE EL COLOR DEL HAZ (la letra:
                    // «el color de los circulos runicos debe ser el mismo
                    // que el del haz C»): las tres vueltas viajan LA
                    // MISMA PALETA DEL CONJURO del conducto (violeta
                    // arcano → morado profundo → rojo sangre → magenta),
                    // cada aro en su fase del ciclo — la íntima caliente
                    // (mezclada al blanco-violeta del núcleo), la media y
                    // la externa desfasadas. Los aros finos ya iban con
                    // ColorConjuro; ahora la corona entera canta con el
                    // haz.
                    // v6.59 — LA CURA DE LA TRANSPARENCIA (la letra:
                    // «los circulos runicos se ven mas transparente lo
                    // cual no debe ser asi»): el alfa piso sube del 28 %
                    // al 55 % (antes la corona nacía casi invisible y solo
                    // maduraba al final) y la íntima viaja MÁS BLANCA
                    // (0,40 → 0,58 de mezcla) para LEERSE desde el primer
                    // tick.
                    OrbitaLib.AbrirAdditive();
                    OrbitaLib.CoronaConjuro(posPantalla, rCorona, Main.GlobalTimeWrappedHourly,
                        alphaMul: 0.55f + 0.45f * laserCharge, glyphMul: 1.8f,
                        blanco: Color.Lerp(ColorConjuro(Projectile.localAI[0] + 6f), new Color(255, 246, 255), 0.58f),
                        dorado: ColorConjuro(Projectile.localAI[0] + 18f),
                        violeta: ColorConjuro(Projectile.localAI[0] + 30f));
                    OrbitaLib.CerrarBatch();
                }
                catch
                {
                    VFXCore.CerrarLoteSiAbierto();
                }
                VFXCore.ReabrirLoteVanilla();

                Vector2 tamAnillo = VFXCore.RingQuadSize(rCorona);
                Main.EntitySpriteDraw(ring, posPantalla, null,
                    DefinitivasLib.Alfa(ColorConjuro(Projectile.localAI[0] + 6f), 0.80f * laserCharge),
                    Main.GlobalTimeWrappedHourly * 0.7f, ring.Size() * 0.5f, tamAnillo, SpriteEffects.None, 0f);
                Vector2 tamAnillo2 = VFXCore.RingQuadSize(rCorona * 1.38f);
                Main.EntitySpriteDraw(ring, posPantalla, null,
                    DefinitivasLib.Alfa(ColorConjuro(Projectile.localAI[0] + 18f), 0.50f * laserCharge),
                    -Main.GlobalTimeWrappedHourly * 0.5f + 2.1f, ring.Size() * 0.5f, tamAnillo2, SpriteEffects.None, 0f);
            }

            // ============ v6.61 — LOS ARCOS: DEL ESCUADRÓN (IRA) O DE LA
            // BOCA (ARMA) ============
            // LA IRA (la letra: «los pequeños rayos electricos deben
            // concentrarce en el centro del circulo formado por los
            // minions… cada rayito debe ir de un minion a otro de forma
            // aleatorio mientras el haz se carga y se lanza»): su boca NO
            // lleva micro-arcos propios — su electricidad son LOS OJOS
            // QUE LA ORBITAN (el círculo sigue vivo TODO el disparo,
            // v6.60.2): arco a arco, de un ojo a su OPUESTO, cruzando el
            // centro. EL ARMA de siempre conserva SUS micro-arcos de
            // boca (la firma del RC2 — la cura de la v6.59: el 60 % del
            // ciclo, dos a cinco, al 60-100 %, cruzando el medio).
            if (EsIra)
            {
                // EL ANCLA VIVA (el círculo ES de quien el escuadrón
                // orbita HOY): solo el haz que SOSTIENE el círculo viste
                // los arcos — en el solape de la prueba (la ceremonia
                // nueva ya nació), los arcos se van CON ELLA (el círculo
                // es suyo) y el haz viejo muere limpio.
                Projectile anclaCirculo = IraSerExteriorCeremoniaProjectile
                    .RitualDe((int)Owner);
                if (anclaCirculo == Projectile && laserCharge > 0.05f && Time % 5f < 3f)
                {
                    IraSerExteriorCeremoniaProjectile.DibujarArcosDelEscuadron(
                        (int)Owner, laserCharge, Projectile.localAI[0]);
                }
            }
            else if (laserCharge > 0.05f && Time % 5f < 3f)
            {
                float progreso = Utils.GetLerpValue(0f, ChargeTime, Time, true);
                int flick = (int)(Main.GlobalTimeWrappedHourly * 60f) & 0x3FF;
                int cuenta = 2 + (int)(progreso * 3.99f);
                for (int i = 0; i < cuenta; i++)
                {
                    int seed = (int)(Time * 0.017f) * 131 + i * 297;
                    float ang = VFXCore.Hash01(seed, i * 7 + 1, 3) * MathHelper.TwoPi;
                    // v6.60.2 — CENTRADOS EN EL MEDIO (la letra: «los
                    // pequeños rayos electricos no estan centrados en el
                    // medio de los circulos runicos»): antes nacían al
                    // borde del centro (r0 10-16) y corrían HACIA FUERA
                    // (r1 36-76) — un anillo de radios con el medio
                    // VACÍO. AHORA cada arco CRUZA EL CENTRO: nace en un
                    // lado del círculo y muere en el OPUESTO (con ±0,4 rad
                    // de vaivén orgánico) — el MEDIO de los círculos
                    // rúnicos HIERVE de electricidad.
                    float rArco = 16f + 6f * progreso;
                    float angOpuesto = ang + MathHelper.Pi
                        + (VFXCore.Hash01(seed, i * 13 + 5, 9) - 0.5f) * 0.8f;
                    Vector2 start = posPantalla + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rArco;
                    Vector2 end = posPantalla + new Vector2(MathF.Cos(angOpuesto), MathF.Sin(angOpuesto)) * rArco;
                    StormLib.Bolt(Main.spriteBatch, start, end, seed, flick, 3.2f,
                        ColorConjuro(Projectile.localAI[0] + i * 8f),
                        DefinitivasLib.BlancoCalido,
                        0.60f + 0.40f * progreso, 5, 12f);
                }
            }

            // v6.60.2 — LA ESTRELLA DE LA BOCA MUERE (la letra: «las
            // estrellitas o brillo solo deben aparecer en la fase de
            // carga»): vivía SOLO durante el disparo (Time > ChargeTime)
            // — exactamente lo que la letra nueva prohíbe. La boca del
            // disparo queda con SU corona rúnica, sus aros y sus arcos
            // (que ahora cruzan EL CENTRO) — sin brillo prestado.

            // ================================================================
            //  ============ LA PILA DE CAPAS DEL CONDUCTO ============
            //  La MISMA polilínea de 1500 puntos, SIETE pasadas: bruma →
            //  glow → (glow²) → CUERPO (el shader del RC2, o su fallback
            //  vanilla) → ALMA → núcleo → soles conducto.
            // ================================================================
            if (Time >= ChargeTime - 20)
            {
                _laserActivo = Utils.GetLerpValue(ChargeTime - 30, ChargeTime + 70, Time, true)
                    * Utils.GetLerpValue(ChargeTime + LaserDuration + 60, ChargeTime + LaserDuration, Time, true);

                Vector2[] positions = _cachePuntos;
                float[] rotations = _cacheRotas;
                for (int i = 0; i < 1500; i++)
                {
                    positions[i] = Projectile.Center + new Vector2(LargoCono * (i / 1500f) * (0.5f + _laserActivo * 0.5f), 0f).RotatedBy(Projectile.rotation);
                    rotations[i] = Projectile.rotation;
                }

                VFXCore.CerrarLoteSiAbierto();
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                gd.BlendState = BlendState.Additive;
                try
                {
                    // (1) LA BRUMA — el resplandor AMBIENTAL: violeta
                    // profundo casi disuelto, la banda más ancha de todas
                    // (×2.6..3.6). Es el «bloom» que tiñe el fondo alrededor
                    // del conducto.
                    CapaRampa(gd, textureGlow, KBruma, ColorBruma, positions, rotations);

                    // (2) EL GLOW — la envolvente DIFUSA: la paleta del
                    // conjuro VIAJANDO (violeta→morado→rojo→magenta, de
                    // boca a punta) al 20-28%.
                    CapaRampa(gd, textureGlow, KGlow, ColorGlow, positions, rotations);

                    // (3) EL GLOW² (EL SUEÑO): la segunda envolvente
                    // interior, más caliente y más suave.
                    CapaRampa(gd, textureGlow, KGlow2, ColorGlow2, positions, rotations);

                    // (4) EL CUERPO — el strip del HazConcentradoShader del
                    // RC2 (el esqueleto 607df36 INTACTO). Si el .fxc no
                    // cargara, el cuerpo cae a una tira de CarnePlasma por
                    // el shader vanilla — LA PILA SE VE IGUAL (GPU-universal).
                    Effect lightningEffect = (texture != null && textureBits != null && textureSecond != null)
                        ? ObtenerShader() : null;
                    if (lightningEffect != null)
                    {
                        lightningEffect.Parameters["uTransformMatrix"].SetValue(Main.GameViewMatrix.NormalizedTransformationmatrix);
                        lightningEffect.Parameters["uTexturaCarne"].SetValue(texture);
                        lightningEffect.Parameters["uTexturaSegunda"].SetValue(textureSecond);
                        lightningEffect.Parameters["uTexturaRampa"].SetValue(textureGlow);
                        lightningEffect.Parameters["uTexturaBits"].SetValue(textureBits);
                        lightningEffect.Parameters["uTime"].SetValue(-Projectile.localAI[0] * 0.025f);
                        lightningEffect.Parameters["uEscalaV"].SetValue(MathHelper.Clamp(realSize, 0.18f, 1f));
                        lightningEffect.CurrentTechnique.Passes[0].Apply();
                        _strip.PrepareStripWithProceduralPadding(positions, rotations, StripColor, StripWidth,
                            -Main.screenPosition, true);
                        _strip.DrawTrail();
                        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
                    }
                    else
                    {
                        // EL CUERPO DE RESPALDO — CarnePlasma (la textura
                        // anti-picos de la casa, envolvente suave en V) con
                        // la paleta viajando: plasma texturizado SIN .fx.
                        // (texture no puede ser null aquí: el guard del
                        // shader ya lo exigió — pero el respaldo es a prueba
                        // de todo: si faltara la carne, la rampa.)
                        CapaRampa(gd, texture ?? textureGlow, 1.0f, ColorCuerpo, positions, rotations);
                    }

                    // (5) EL ALMA (v6.57) — LA TEXTURA BLANCA A TODO EL
                    // ANCHO (la letra: «debe tener otra textura como esa
                    // pero que ocupe todo el haz en ancho»): la MISMA
                    // RampaGlow del núcleo (la textura blanca que el
                    // usuario ve en el centro) abierta hasta la silueta
                    // del cuerpo — el haz entero viste la tela blanca.
                    CapaRampa(gd, textureGlow, KAlma, ColorAlma, positions, rotations);

                    // (6) EL NÚCLEO — la vena ardiente: blanco-violeta al
                    // 65-95% en la banda más estrecha (×0.28..0.42). El
                    // «láser dentro de la explosión de energía» — arde
                    // ENCIMA del alma.
                    CapaRampa(gd, textureGlow, KNucleo, ColorNucleo, positions, rotations);
                }
                finally
                {
                    gd.BlendState = BlendState.AlphaBlend;
                    VFXCore.RestaurarLoteVanilla();
                }

                // v6.60.2 — (7) LOS SOLES CONDUCTO MUEREN (la letra:
                // «las estrellitas o brillo solo deben aparecer en la fase
                // de carga»): corrían por las márgenes SOLO durante el
                // disparo — el conducto lanzado queda LIMPIO: la pila de
                // capas y su boca rúnica, nada más.
            }

            VFXCore.RestaurarLoteVanilla();
        }

        /// <summary>UNA CAPA DE LA PILA: el shader VANILLA de sprites
        /// (Main.pixelShader — el mismo que dibuja todo Terraria, presente
        /// en cualquier GPU) + RampaGlow (perfil gaussiano: 0 en el borde,
        /// pico al centro) → el borde de la capa se funde EN CURVA por
        /// píxel. Cero picos por construcción.</summary>
        private void CapaRampa(GraphicsDevice gd, Texture2D rampa, float k,
            VertexStrip.StripColorFunction colorFn, Vector2[] positions, float[] rotations)
        {
            if (rampa == null) return;
            _kCapa = k;
            gd.Textures[0] = rampa;
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            _strip.PrepareStripWithProceduralPadding(positions, rotations, colorFn, AnchoCapa,
                -Main.screenPosition, true);
            _strip.DrawTrail();
        }

        // ============ LOS COLORES DE CADA CAPA (por nodo, la convención
        // de la casa: rgb premultiplicado por el alfa de la capa) ============

        private Color ColorBruma(float p)
        {
            Color c = ColorConjuro(Projectile.localAI[0] - p * CicloConjuro);
            // OSCURECIDA — el «negro» del conjuro: la bruma es la sombra
            // luminosa (violeta profundo con un pulso de rojo al final).
            Color sombra = new Color((byte)(c.R * 0.42f), (byte)(c.G * 0.34f), (byte)(c.B * 0.52f));
            return sombra * (AlfaBruma * _laserActivo) * ColaSuave(p);
        }

        private Color ColorGlow(float p)
        {
            Color c = ColorConjuro(Projectile.localAI[0] - p * CicloConjuro);
            return c * (AlfaGlow * _laserActivo) * ColaSuave(p);
        }

        private Color ColorGlow2(float p)
        {
            Color c = ColorConjuro(Projectile.localAI[0] - p * CicloConjuro);
            // más caliente: medio tono hacia el blanco-violeta.
            Color caliente = Color.Lerp(c, new Color(255, 246, 255), 0.28f);
            return caliente * (AlfaGlow2 * _laserActivo) * ColaSuave(p);
        }

        private Color ColorCuerpo(float p)
        {
            Color c = ColorConjuro(Projectile.localAI[0] - p * CicloConjuro);
            return c * (0.62f * _laserActivo) * ColaSuave(p);
        }

        private Color ColorAlma(float p)
        {
            // LA TELA BLANCA (v6.57) — la misma mezcla blanco-violeta del
            // núcleo, un tono más violeta (la vena de encima es la que
            // llega al blanco puro): la textura blanca A TODO EL ANCHO.
            Color alma = Color.Lerp(new Color(255, 250, 255), new Color(222, 196, 255), 0.42f);
            return alma * (AlfaAlma * _laserActivo) * ColaSuave(p);
        }

        private Color ColorNucleo(float p)
        {
            // BLANCO-VIOLETA incandescente — la vena del conducto.
            Color nucleo = Color.Lerp(new Color(255, 246, 255), new Color(214, 170, 255), 0.35f);
            return nucleo * (AlfaNucleo * _laserActivo) * ColaSuave(p);
        }

        /// <summary>EL ANCHO DE LA CAPA ACTIVA — el perfil del RC2 (p^0.8 ·
        /// cbrt · realSize · 3500 · respiración) por el factor de la capa:
        /// TODAS las capas respiran JUNTAS (cohesión del conjunto).</summary>
        private float AnchoCapa(float p) => StripWidth(p) * _kCapa;

        // v6.60.2 — LOS SOLES CONDUCTO MURIERON ENTEROS (la letra: «las
        // estrellitas o brillo solo deben aparecer en la fase de carga,
        // cuando el haz es lanzado no necesita cosas como las estrellitas
        // o brillo»): su método y su constante murieron con la llamada —
        // el conducto lanzado es la pila de capas y su boca rúnica, LIMPIO.

        /// <summary>EL COLOR DEL CUERPO (el strip del shader): la paleta
        /// del conjuro que VIAJA a lo largo — idéntico al RC2, con LA
        /// COLA (v6.62): el cuerpo también se entrega al final.</summary>
        public Color StripColor(float progress)
        {
            Color color = ColorConjuro(Projectile.localAI[0] - progress * CicloConjuro);
            color.A = 0;
            return color * realSize * ColaSuave(progress);
        }

        // ================================================================
        //  v6.62 — LA COLA QUE SE DISUELVE: el final del conducto NO se
        //  corta en seco — las texturas LLEGAN hasta el final y SE
        //  DIFUMINAN hasta perderse al llegar al final del recorrido
        //  (la letra: «el haz deberia tener un final suave, o sea si
        //  estas en multijugador un jugador veria el final del haz con
        //  el final cortado como en la segunda imagen, debe tener un
        //  final donde las texturas llegen hasta el final y se
        //  difuminen hasta perderse al llegar al final del recorrido»).
        //  La AGUJA de la v6.57 afila el ANCHO de la punta; la COLA
        //  entrega el ALFA: el último 22 % del conducto se disuelve en
        //  curva suave — pleno hasta el 78 %, un susurro al 92 %, NADA
        //  en la punta. La llevan TODAS las capas (la misma polilínea:
        //  cero esquinas nuevas, cero picos por construcción).
        // ================================================================
        private static float ColaSuave(float p)
        {
            if (p <= 0.78f) return 1f;
            float t = Utils.GetLerpValue(0.78f, 1f, p, true);
            // ease-in 1,7: se mantiene casi pleno hasta bien entrado el
            // tramo y se entrega en los últimos píxeles — ni corte, ni
            // escalón: la punta se disuelve, no se amputa.
            return 1f - (float)Math.Pow(t, 1.7);
        }

        /// <summary>EL ANCHO DEL STRIP — el perfil del RC2 (607df36) más
        /// LAS DOS AFILADAS de la v6.57: p^0.8 · cbrt · AGUJA · CIERRE ·
        /// realSize · 3500 · (1.1 + cos·(0.08 − p·0.06)).</summary>
        public float StripWidth(float progress)
        {
            float start = (float)Math.Pow(progress, 0.8f);
            float cap = (float)Math.Cbrt(Utils.GetLerpValue(1.01f, 0.7f, progress, true));

            // LA AGUJA DE LA PUNTA (v6.57 — la letra: «al final del haz
            // este como que se hace estrecho pero no lo suficiente»): el
            // cap del 607df36 solo afilaba a ×0.32 — la punta se leía
            // ROMA. El último 16 % del conducto se afila ahora hasta
            // ×0.06: punta de aguja. Curva MONÓTONA suave (sin saltos):
            // cero picos por construcción — misma polilínea.
            float aguja = MathHelper.Lerp(1f, 0.06f,
                (float)Math.Pow(Utils.GetLerpValue(0.84f, 1f, progress, true), 1.35f));

            // LA ESTRANGULACIÓN FINAL (v6.57 — la letra: «al terminar de
            // lanzar el haz este debe hacerse estrecho y luego
            // desaparecer»): en la ventana de muerte (últimos ~45
            // ticks) TODO el conducto colapsa a una aguja (×0.07,
            // ease-in ²) ANTES de apagarse — TODAS las capas comparten
            // este ancho: el haz entero se cierra como una boca y muere.
            float cierre01 = Utils.GetLerpValue(ChargeTime + LaserDuration,
                ChargeTime + LaserDuration + 45f, Time, true);
            float cierre = MathHelper.Lerp(1f, 0.07f, cierre01 * cierre01);

            return start * cap * aguja * cierre * realSize * 3500f
                * (1.1f + (float)Math.Cos(Time) * (0.08f - progress * 0.06f));
        }
    }
}
