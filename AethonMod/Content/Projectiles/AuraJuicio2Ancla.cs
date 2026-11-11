using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Buffs;
using AethonMod.Content.Dusts;
using AethonMod.Content.Effects.Bruma;
using AethonMod.Content.Globals;
using AethonMod.Content.Items.Accessories;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles
{
    // ======================================================================
    //  AuraJuicio2Ancla — v6.105 → v6.106 → v6.107 → v6.108 → v6.109 →
    //  v6.112 — EL ANCLA INVISIBLE DEL AURA DEL VACÍO (nacida como «Aura
    //  de Juicio 2» — la copia silenciosa: el efecto del viejo
    //  ESTANDARTE, el teatro SOLO en los enemigos; v6.109: los otros dos
    //  estandartes MURIERON y esta queda como LA ÚNICA — renombrada EL
    //  AURA DEL VACÍO).
    //
    //  LA LETRA DEL USUARIO (v6.112): «en el aura del vacio la bruma es
    //  muy larga, mejor quitemosla de esa forma y dejemos solo pequeñas
    //  particulas» — EL MANTO LARGO (la estela de 2.2-3.4 cúpulas de la
    //  v6.111) y EL ALIENTO VIOLETA (sus tres velos) MURIERON: solo
    //  quedan LAS PEQUEÑAS PARTÍCULAS del humo (motas de 4-8 px que
    //  nacen del punto de estancamiento, dan unos pasos por el
    //  hemisferio de fuga y se disuelven) — más las chispas y el arco
    //  de choque de siempre.
    //
    //  LA LETRA DEL USUARIO (v6.109): «ya puedes borrar los otros 2
    //  estandarte y renombrar a EL AURA DE JUICIO 2 como Aura de Vacio,
    //  pero todavia falta algo EL AURA DE JUICIO 2 aunque esta bien no
    //  veo bruma le falta eso» + «al parecer con el enemigo que es un
    //  fantasma el aura no funciona es curioso jajajaja».
    //
    //  LA CURA v6.109 — DOS COSAS:
    //   · LA BRUMA DE VERDAD: la v6.108 exhalaba puffs con alfas tímidos
    //     (pico ~0.2 típicos, colores casi negros y radios chicos — la
    //     LECCIÓN v6.50.65 otra vez: bruma oscura con alfa bajo = INVISIBLE).
    //     AHORA la receta de la casa: MÁS puffs (3→7 por cercanía), MÁS
    //     GRANDES, con ALFA de verdad (0.14..0.80), la RAMPA DE
    //     ENFRIAMIENTO violácea (nace oscura y se disuelve en
    //     violeta-claro — el borde SIEMPRE se lee) Y LA SEGUNDA CAPA: EL
    //     ALIENTO VIOLETA aditivo derramándose por la estela de fuga
    //     (la lectura garantizada de día Y de noche).
    //   · EL FANTASMA TAMBIÉN ARDE: el aura es QUEMADURA (debuff +
    //     lifeRegen), no un golpe — la puerta vanilla (CanBeChasedBy +
    //     dontTakeDamage) dejaba FUERA a los espíritus que atraviesan
    //     paredes (los fantasmAS de la mazmorra son intangibles dentro
    //     de los bloques). AHORA la familia de los espíritus (aiStyle 23)
    //     ENTRA al campo como cualquier impío — hasta un fantasma sufre
    //     el calentamiento de la reentrada.
    //
    //  LA LETRA DEL USUARIO (v6.105): «su efecto debe ser el mismo que
    //  el de estandarte del juicio, ademas sobre el jugador no necesita
    //  haber nada, el efecto solo se ve al acercarce los enemigos con
    //  particulas mas pequeñas y con los colores de la casa, ya sabes
    //  principalmente morado y negro, ademas tambien agrega particulas
    //  de bruma en la misma direccion e intensidad variable como las
    //  particulas iniciales y sobre los enemigos agrega un efecto que
    //  sea una textura de perlin circular y que solo sea un cemirculo
    //  con un difuminado y transparencia en forma de cirulo en su
    //  parte plana».
    //
    //  LA CORRECCIÓN DE LA LETRA (v6.106): «el semicirculo esta al
    //  revez y las particulas deben moverse en la direccion opuesta al
    //  jugador, ademas el semicirculo debe tener la parte recta mas
    //  difuminada algo asi como una cupula o medialuna, las particulas
    //  deben moverse en la direccion opuesto al jugador, o sea el
    //  jugador esta a la derecha del enemigo y las particulas del
    //  enemigo se mueven a la izquierda, asi para todas las direcciones,
    //  ademas cada particula se debe liberar y deben tener el mismo
    //  recorrido que el semcirculo, la velocidad de las particulas debe
    //  ir cada vez mas rapido a medida que se acercan al jugador».
    //
    //  LA CORRECCIÓN DE LA LETRA (v6.107): «tanto las particulas como
    //  la medialuna en los enemigos al usar el aura del juicio, esta
    //  mal, intenta darle una rotacion vertical de 90 grados, o sea
    //  darle la vuelte y las particulas deben nacer del centro de los
    //  enemigos y luego dispersarce hacia fuera pero siempre en la
    //  direccion opuesta al jugador».
    //
    //  LA CORRECCIÓN DE LA LETRA (v6.108): «la parte abombada de la
    //  medialuna tiene que mirarme a mi en todo momento, deben nacer
    //  del centro de la medialuna en la parte abombada y extenderse
    //  en todas las direcciones pero siempre alejandose de mi, a
    //  mayor cercania mas particulas y la medialuna cambia de color
    //  volviendose ligeramente morada negra, y la bruma debe hacer
    //  lo mismo que las particulas. el objetivo es que parezca
    //  calentamiento aerodinamico igual que las cosas que entran a
    //  la atmosfera del planeta, pero en este caso yo seria el
    //  planeta y todo lo que entre en mi atmosfera que seria al aura
    //  del juicio, sufriria de calentamiento aerodinamico, entiendes
    //  el concepto».
    //
    //  EL CONCEPTO v6.108 — EL CALENTAMIENTO AERODINÁMICO: el
    //  portador es EL PLANETA, su aura LA ATMÓSFERA y cada impío un
    //  objeto CAYENDO EN REENTRADA — el ARCO DE CHOQUE (la medialuna)
    //  lo envuelve POR DELANTE con la CONCHA ABOMBADA MIRANDO AL
    //  PLANETA (en todo momento: el impío siempre «cae hacia ti»), y
    //  del PUNTO DE ESTANCAMIENTO (el tope abombado — donde el choque
    //  arde más) brota el PLASMA: chispas y bruma que se derraman
    //  hacia ATRÁS en TODAS las direcciones de fuga (siempre
    //  alejándose del portador), MÁS DENSAS cuanto más hondo cae el
    //  impío (a mayor cercanía, más partículas)… y el arco MISMO se
    //  enfría en sombra: la medialuna se vuelve LIGERAMENTE MORADA
    //  NEGRA con la cercanía.
    //
    //  LA CURA v6.107 — EL TEATRO SE PONE DE PIE Y EXPULSA DEL CORAZÓN:
    //   · LA MEDIALUNA VERTICAL (LA CÚPULA) — LA ROTACIÓN VERTICAL DE
    //     90° de la letra: la v6.106 ACOSTABA el arco en horizontal (la
    //     parte plana mirando al portador, el domo abombándose lejos de
    //     él) — AHORA la parte PLANA (la disuelta) cae ABAJO, sobre el
    //     corazón del impío, y la concha se YERGUE cubriéndolo desde
    //     arriba: LA CÚPULA DEL JUICIO que pesa sobre su cabeza (la
    //     medialuna YA NO gira con el portador — solo el bamboleo lento
    //     la mece).
    //   · LAS CHISPAS DEL CORAZÓN (PolvoExpulsionJuicio — el polvo
    //     NUEVO): cada una NACE EN EL CORAZÓN del impío y sale
    //     EXPULSADA hacia FUERA, SIEMPRE en la dirección OPUESTA al
    //     portador (jugador a la derecha → nacen del pecho y vuelan a
    //     la IZQUIERDA, abriéndose en abanico — así para TODAS las
    //     direcciones), LIBERADA (la cuna congelada al nacer) y
    //     ACELERANDO suavecito hasta disolverse al final del camino —
    //     la furia escala con la cercanía.
    //   · LA BRUMA DEL CORAZÓN — cada puff NACE en el corazón del
    //     impío y se derrama HACIA FUERA por el MISMO abanico de fuga
    //     (la MISMA dirección de las chispas), con INTENSIDAD VARIABLE
    //     (alfa, tamaño y número de puffs crecen con la cercanía).
    //
    //  EL ANCLA ES INVISIBLE — NADA se dibuja sobre el portador (ni
    //  bloom, ni luciérnagas, ni luz del campo). Lo que hace:
    //
    //   1) MARCA EL CAMPO — EL EFECTO DEL ESTANDARTE (la copia del
    //      bloque del EstandarteJuicioAuraProjectile): el radio
    //      pequeño de 300 px, LA QUEMADURA DEL VACÍO EXTERIOR con su
    //      timer de 180, LA INTENSIDAD ×1 (borde) → ×3 (pegado) más
    //      LA RAMPA del tiempo ardiendo — el dot, la lentitud y el
    //      pago del buff viven en el motor compartido
    //      (QuemaduraVacioExterior + VacioGlobalNPC).
    //
    //   2) VISTE A LOS IMPIOS (solo cuando se ACERCAN — el efecto
    //      solo existe en el enemigo): EL ARCO DE CHOQUE (la
    //      medialuna con la concha abombada MIRANDO al portador) +
    //      EL PLASMA del punto de estancamiento (chispas y bruma
    //      derramándose hacia atrás — ver EL CONCEPTO v6.108 arriba).
    // ======================================================================
    public class AuraJuicio2Ancla : ModProjectile
    {
        /// <summary>v6.109 — EL RADIO DEL VACÍO (px — 18-19 tiles: un
        /// aura ÍNTIMA, la letra del estandarte original). Público: lo
        /// lee VacioGlobalNPC para la lentitud y la cercanía (antes
        /// vivía en el EstandarteJuicioAuraProjectile, que MURIÓ).</summary>
        public const float RADIO = 300f;

        /// <summary>LA DISTANCIA ÍNTIMA del campo (a 40 px del impío, el
        /// fuego pleno).</summary>
        private const float DIST_INTIMA = 40f;

        /// <summary>v6.123 — CUÁNTO ARDE FUERA DEL AURA (la letra:
        /// «cuando aura del vacio deja de afectar al enemigo, la
        /// quemadura del vacio sobre el enemigo continua por 5 segundos
        /// mas haciendo daño»): 300 ticks = 5 segundos EXACTOS de fuego
        /// que sigue COBRANDO tras dejar el campo (antes eran 180).</summary>
        private const int ARDE_FUERA = 300;

        /// <summary>La intensidad máxima por cercanía (×3 pegado).</summary>
        private const float INTENSIDAD_CERCA = 3f;

        /// <summary>LOS IMPÍOS vestidos a la vez (el presupuesto de la
        /// casa: los más cercanos primero).</summary>
        private const int MAX_VESTIDOS = 8;

        // EL RECURSO NUEVO — el semicírculo Perlin (null-safe, el
        // patrón de la casa: Asset diferido, IsLoaded, catch).
        private static Asset<Texture2D> _perlinSemi;

        /// <summary>EL SEMICÍRCULO PERLIN (256×256 — premultiplicado).
        /// Null-safe.</summary>
        private static Texture2D PerlinSemi
        {
            get
            {
                try
                {
                    _perlinSemi ??= ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/PerlinSemicirculo");
                    return _perlinSemi.IsLoaded ? _perlinSemi.Value : null;
                }
                catch { return null; }
            }
        }

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;          // es un CAMPO, no un arma
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;       // el campo del portador: todos lo ven
            Projectile.aiStyle = -1;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player dueno = Main.player[Projectile.owner];

            // EL VÍNCULO — mientras el dueño viva y PORTÉ el aura 2,
            // el ancla vive; si la suelta o cae, muere.
            if (dueno == null || !dueno.active || dueno.dead ||
                !AuraDeJuicio2Item.LlevaElAura(dueno))
            {
                Projectile.Kill();
                return;
            }

            // EL ANCLA: clavada al pecho del portador, inmortal mientras
            // el vínculo viva (INVISIBLE — nada de luz, nada de dibujo).
            Projectile.Center = dueno.MountedCenter;
            Projectile.timeLeft = 2;

            // ==============================================================
            //  1) LA MARCA DEL CAMPO — EL EFECTO DEL ESTANDARTE (solo el
            //     dueño manda: la quemadura viaja dentro del propio
            //     debuff — la copia del bloque del estandarte).
            //     v6.122 → v6.124 — EL DAÑO DEL AURA (la letra nueva:
            //     «el daño va de 1 a 10 por cada tick de daño» + el
            //     bono del 10 % del maná sobre 200): la vieja escala del
            //     despertar (20 + 6×nivel) murió en la v6.122, el 0,1 %
            //     de los daños del Grimorio muere AHORA — el rango del
            //     aura vive del MANÁ del portador (DanoAuraMax y sus
            //     hermanas, aquí al lado — la única fuente).
            // ==============================================================
            if (Projectile.owner == Main.myPlayer && !Main.gameMenu)
            {
                // ======================================================
                //  v6.124 — EL BUFF SIEMPRE PRESENTE (la letra: «el buff
                //  del Aura del Vacio siempre debe estar aplicado
                //  mientras se use La Forma Eterna ya que es un efecto
                //  activo de La Forma Eterna»): el Aura del Vacío es un
                //  EFECTO ACTIVO del portador — viva o no viva una presa
                //  en el campo, el espejo luce SU PODER: la ventana
                //  cuenta el rango EXACTO que el vacío está cobrando
                //  (1 en el borde exterior → {DanoMaximo} pegado al
                //  portador). Se refresca cada tick que el ancla vive
                //  (30 ticks de cortesía — el mismo patrón del pago del
                //  Vacío Exterior).
                // ======================================================
                int danoMaximo = DanoAuraMax(dueno);
                Content.Buffs.AuraVacioBuff.DanoMaximoVivo = danoMaximo;
                dueno.AddBuff(ModContent.BuffType<Content.Buffs.AuraVacioBuff>(), 30, true);

                bool marcoAlMenosUno = false;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active)
                        continue;

                    // ------------------------------------------------------
                    //  v6.109 — EL FANTASMA TAMBIÉN ARDE (la letra: «con
                    //  el enemigo que es un fantasma el aura no funciona
                    //  es curioso jajajaja» — la curiosidad resuelta):
                    //  el daño del aura es QUEMADURA (debuff + lifeRegen),
                    //  NO un golpe — así que la puerta vanilla ya no
                    //  decide quién arde. LA FÓRMULA REAL de
                    //  NPC.CanBeChasedBy (verificada contra el fuente del
                    //  tML 2026.09.3.1): «active && chaseable &&
                    //  lifeMax > 5 && !dontTakeDamage && !friendly» — el
                    //  FANTASMA del cementerio (NPCID.Ghost) se vuelve
                    //  INTANGIBLE dentro de los bloques y la puerta lo
                    //  dejaba FUERA. AHORA LA FAMILIA DE LOS ESPÍRITUS
                    //  (aiStyle 23 — espectros, poltergeists, segadores —
                    //  y el Ghost por ID, por si su AI cambia) ENTRA al
                    //  campo como cualquier impío: hasta un fantasma
                    //  sufre el calentamiento de la reentrada. Todo lo
                    //  demás SIGUE fuera: los inmortales, los amigos,
                    //  los pueblos, la chusma menuda (lifeMax <= 5) y
                    //  los segmentos de gusano (chaseable = false).
                    // ------------------------------------------------------
                    bool esEspiritu = n.aiStyle == 23 || n.type == NPCID.Ghost;
                    if ((n.friendly || n.townNPC || n.immortal ||
                         !n.CanBeChasedBy() || n.dontTakeDamage) && !esEspiritu)
                        continue;

                    VacioGlobalNPC vacio = n.GetGlobalNPC<VacioGlobalNPC>();

                    // FUERA DEL RADIO: si arde por NOSOTROS, el fuego se
                    // enfría poco a poco (el debuff muere solo a los 5 s
                    // — la persistencia de la letra).
                    Vector2 borde = Vector2.Clamp(Projectile.Center, n.TopLeft, n.BottomRight);
                    float dist = Vector2.Distance(Projectile.Center, borde);
                    float perdon = Math.Max(n.width, n.height) * 0.5f + 40f;
                    if (dist > RADIO + perdon)
                    {
                        // v6.124 — ya NO está dentro: la quemadura que
                        // queda es EL FUEGO QUE ARDE SOLO (1% de la vida
                        // máxima por medio segundo, QuemaduraVacioExterior).
                        vacio.DentroDelCampo = false;
                        if (vacio.DuenoVacio == dueno.whoAmI + 1 && vacio.IntensidadQuemadura > 0f)
                            vacio.IntensidadQuemadura *= 0.995f;
                        continue;
                    }

                    // DENTRO — LA MARCA: la Quemadura del Vacío Exterior
                    // (se refresca mientras permanezca; fuera, sus últimos
                    // 300 ticks de fuego — los 5 segundos de la letra).
                    n.AddBuff(ModContent.BuffType<QuemaduraVacioExterior>(), ARDE_FUERA, false);

                    // LA CUENTA DEL DUEÑO (quién cobra el buff del vacío).
                    vacio.DuenoVacio = dueno.whoAmI + 1;
                    vacio.DanoVacio = danoMaximo;
                    // v6.124 — DENTRO DEL CAMPO: el daño por CERCANÍA de
                    // la letra (lo lee QuemaduraVacioExterior.Update).
                    vacio.DentroDelCampo = true;
                    marcoAlMenosUno = true;

                    // LA INTENSIDAD — la técnica Remap del original:
                    // ×1 en el borde → ×3 PEGADO, más LA RAMPA DEL TIEMPO
                    // (cada segundo ardiendo lo enciende, hasta ×1.5).
                    float cercania = Remap(dist, RADIO + perdon,
                        DIST_INTIMA, 0f, 1f);
                    float rampaTiempo = 1f + 0.5f * Math.Min(1f, vacio.VisualQuemadura);
                    vacio.IntensidadQuemadura = (1f + (INTENSIDAD_CERCA - 1f) * cercania)
                        * rampaTiempo;
                }

                // ==========================================================
                //  v6.122 → v6.124 — EL AURA DEL VACÍO HECHA BUFF: mientras
                //  el campo marque a UN solo impío, la ventana cuenta
                //  también SI está cobrando (el marcado — un texto corto
                //  de trabajo). El buff en sí YA vive ARRIBA (siempre
                //  presente con la Forma Eterna — la letra nueva).
                // ==========================================================
                if (marcoAlMenosUno)
                {
                    Content.Buffs.AuraVacioBuff.Trabajando = true;
                }
                else
                {
                    Content.Buffs.AuraVacioBuff.Trabajando = false;
                }
            }

            // ==============================================================
            //  2) EL PLASMA DE LA REENTRADA (solo cliente — el teatro
            //     de los impíos: NACE del PUNTO DE ESTANCAMIENTO — el
            //     tope abombado de la medialuna, donde el choque arde
            //     más — y se derrama hacia ATRÁS en TODAS las
            //     direcciones de fuga, SIEMPRE alejándose del
            //     portador — la MISMA fuga de la bruma).
            // ==============================================================
            if (Main.netMode != NetmodeID.Server && !Main.dedServ && !Main.gameMenu)
                VestirALosImpios(dueno);
        }

        // ================================================================
        //  v6.124 — EL DAÑO DEL AURA DEL VACÍO, LA LETRA NUEVA Y SIMPLE
        //  (la única fuente de verdad). LA LETRA: «para hacerlo simple
        //  con el calculo de daño, dejemos que el daño del aura del vacio
        //  sea 1 de daño en los bordes exteriores del efecto del aura,
        //  y a mas cerca del jugador este el enemigo mas daño hace y
        //  mas tick de daño hace por segundo, el daño va de 1 a 10 por
        //  cada tick de daño, ademas tendra un calculo de daño igual
        //  al 10% de mana superior a 200 puntos, o sea, cuando el jugador
        //  tenga mas de 200 puntos de mana, cada punto de mana extra por
        //  encima de 200 se cuenta para el calculo de daño, los primeros
        //  200 puntos de mana no cuentan nunca para el calculo de daño»
        //  + «el daño siempre se suma al daño maximo que puede hacer el
        //  aura del vacio y este siempre es el daño que se hace cuando
        //  el enemigo esta cerca del jugador».
        //
        //  LA ARITMÉTICA (el ejemplo de la letra, verificado): con 300
        //  de maná → 100 por encima del límite → el 10% = 10 → el rango
        //  queda 1 a 20 por tick (el bono SIEMPRE al MÁXIMO — el daño
        //  pegado al portador). El DAÑO por tick a cercanía c:
        //      D(c) = 1 + (máx − 1)·c          (c = 0 en el borde → 1)
        //  y EL RITMO de ticks también escala con la cercanía:
        //      I(c) = 30 − 20·c  ticks entre golpe (2/s en el borde →
        //                          6/s pegado al cuerpo)
        //  TODO se cobra por la convención vanilla del DoT:
        //  lifeRegen = −120·D/I (un golpe de D cada I ticks EXACTOS) y
        //  lifeRegenExpectedLossPerSecond = D (el número que el combate
        //  enseña). El viejo 0,1 % de los daños del Grimorio MURIÓ: el
        //  bono vive del MANÁ (y el reclamo de la letra — «aumente el
        //  daño del grimorio y no se vio reflejado» — ya no aplica:
        //  el rango se mueve con el MANÁ del portador, no con el libro).
        // ================================================================

        /// <summary>v6.124 — EL MANÁ QUE CUENTA: el 10 % de los puntos
        /// de maná POR ENCIMA de 200 (los primeros 200 NUNCA cuentan —
        /// la letra, palabra por palabra).</summary>
        public static int BonusMana(Player p)
        {
            try
            {
                if (p == null || !p.active) return 0;
                int extra = p.statManaMax2 - 200;
                if (extra <= 0) return 0;
                return (int)(extra * 0.10f);
            }
            catch { return 0; }
        }

        /// <summary>v6.124 — EL MÁXIMO DEL AURA: 10 + el bono de maná.
        /// Es el daño por tick que sufre el enemigo PEGADO al portador
        /// (el bono SIEMPRE se suma al máximo — la letra).</summary>
        public static int DanoAuraMax(Player p)
        {
            try { return 10 + BonusMana(p); }
            catch { return 10; }
        }

        /// <summary>v6.124 — EL DAÑO POR TICK a una cercanía dada
        /// (0 = borde exterior → 1 = pegado al cuerpo).</summary>
        public static int DanoAuraPorCercania(Player p, float cercania)
        {
            float c = MathHelper.Clamp(cercania, 0f, 1f);
            float dano = 1f + (DanoAuraMax(p) - 1f) * c;
            return Math.Max(1, (int)Math.Round(dano));
        }

        /// <summary>v6.124 — EL INTERVALO de ticks entre golpes a una
        /// cercanía dada: 30 en el borde (2 golpes/s) → 10 pegado
        /// (6 golpes/s) — «mas tick de daño hace por segundo», la letra.</summary>
        public static int IntervaloAura(float cercania)
        {
            float c = MathHelper.Clamp(cercania, 0f, 1f);
            return (int)MathHelper.Lerp(30f, 10f, c);
        }

        // ================================================================
        //  EL TEATRO DE LOS IMPIOS — EL PLASMA DE LA REENTRADA (fase de
        //  UPDATE: el spawn de polvos naciendo del PUNTO DE
        //  ESTANCAMIENTO — el tope abombado de la medialuna; la bruma y
        //  el arco de choque viven en el render).
        // ================================================================
        private void VestirALosImpios(Player dueno)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active)
                    continue;
                if (!VacioGlobalNPC.TieneQuemadura(n))
                    continue;
                VacioGlobalNPC vacio = n.GetGlobalNPC<VacioGlobalNPC>();
                if (vacio.DuenoVacio != dueno.whoAmI + 1)
                    continue;                              // arde por OTRO campo
                if (vacio.VisualQuemadura <= 0.05f)
                    continue;

                float vive = vacio.VisualQuemadura;
                float k = MathHelper.Clamp(vacio.CercaniaLocal, 0f, 1f);   // 0 borde → 1 pegado
                float intensidad = MathHelper.Clamp(vacio.IntensidadQuemadura, 1f, 4.5f);

                // LA DIRECCIÓN DE LA CASA — portador → impío: las chispas
                // NACEN DEL CORAZÓN del impío y salen EXPULSADAS hacia
                // fuera, SIEMPRE en la dirección OPUESTA al portador
                // (la MISMA fuga de la bruma y de las originales).
                Vector2 hacia = n.Center - dueno.MountedCenter;
                float largo = hacia.Length();
                if (largo < 1f)
                    continue;
                Vector2 dir = hacia * (1f / largo);

                // ----------------------------------------------------------
                //  v6.108 — EL PLASMA DEL PUNTO DE ESTANCAMIENTO:
                //  «deben nacer del centro de la medialuna en la parte
                //  abombada y extenderse en todas las direcciones pero
                //  siempre alejandose de mi, a mayor cercania mas
                //  particulas».
                //
                //  CADA CHISPA NACE EN EL TOPE ABOMBADO de la medialuna
                //  (EL PUNTO DE ESTANCAMIENTO — el punto del arco que
                //  MIRA al portador, donde el choque comprime y arde)
                //  y se derrama hacia ATRÁS en TODAS las direcciones de
                //  fuga (el abanico trasero COMPLETO ±90°: el plasma
                //  rodea el cuerpo y deja la estela — así para TODAS
                //  las direcciones, SIEMPRE alejándose del portador).
                //  Cuanto más hondo cae el impío (más cercanía), MÁS
                //  DENSA hierve la reentrada.
                // ----------------------------------------------------------
                // LA DENSIDAD DE LA ATMÓSFERA — a mayor cercanía, MÁS
                // PARTÍCULAS (el impío cae más hondo: cada 6 ticks en
                // el borde… CADA TICK pegado, en ráfagas).
                int ritmo = (int)MathHelper.Clamp(6f - k * 5f, 1f, 6f);
                if ((Projectile.ai[0] + i) % ritmo == 0f && n.Center.DistanceSQ(dueno.Center) < 640f * 640f)
                {
                    int rafaga = 1 + (k > 0.4f ? 1 : 0)
                        + (k > 0.75f && Main.rand.NextFloat() < 0.5f ? 1 : 0);
                    for (int s = 0; s < rafaga; s++)
                    {
                        // EL RUMBO — EL HEMISFERIO DE FUGA COMPLETO: el
                        // plasma se derrama en TODAS las direcciones que
                        // SE ALEJAN del portador (el abanico trasero
                        // ±90° alrededor de la fuga — el flujo del
                        // choque rodeando el cuerpo en su caída).
                        float abanico = Main.rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);
                        Vector2 rumbo = dir.RotatedBy(abanico);

                        // LA CUNA — EL PUNTO DE ESTANCAMIENTO: el tope
                        // ABOMBADO de la medialuna (el punto del arco
                        // que MIRA al portador — allí el choque comprime
                        // y arde, y de allí nace el plasma), con un
                        // temblor mínimo para que no nazcan todas del
                        // mismo pixel.
                        Vector2 cuna = n.Center - dir * (RadioDomo(n, k) * 0.95f) + new Vector2(
                            Main.rand.NextFloat(-1f, 1f) * n.width * 0.08f,
                            Main.rand.NextFloat(-1f, 1f) * n.height * 0.08f);

                        // LA VELOCIDAD — la furia del vacío escala con la
                        // cercanía, y ACELERA sin freno durante el vuelo
                        // (la rabia crece al escapar).
                        float vBase = (0.9f + 1.1f * k) * Main.rand.NextFloat(0.85f, 1.2f);
                        float acel = 0.020f + 0.018f * k;

                        // LIBERADA — la cuna y el rumbo quedan congelados
                        // AL NACER: aunque el impío camine, la chispa
                        // termina SU vuelo sola (PolvoExpulsionJuicio
                        // manda). El camino: del punto de estancamiento
                        // hacia ATRÁS — la ESTELA de la reentrada, larga
                        // y desgarrada (hasta ~2.6 cúpulas).
                        float distMax = RadioDomo(n, k) * Main.rand.NextFloat(1.6f, 2.6f);
                        var chispa = new PolvoExpulsionJuicio.Chispa
                        {
                            Centro = cuna,
                            Dir = rumbo,
                            Vel = vBase,
                            Acel = acel,
                            DistMax = distMax
                        };

                        // EL COLOR DE LA CASA — el halo violeta: más
                        // CLARO cuanto más cerca arde (la temperatura).
                        Color halo = Color.Lerp(
                            new Color(150, 84, 235), new Color(224, 176, 255), k);
                        Dust d = Dust.NewDustPerfect(cuna,
                            ModContent.DustType<PolvoExpulsionJuicio>(), Vector2.Zero,
                            0, halo, Main.rand.NextFloat(0.24f, 0.38f));   // MENUDAS (la letra)
                        d.customData = chispa;
                        d.noGravity = true;
                        d.noLight = false;
                    }
                }
            }
            Projectile.ai[0] += 1f;
        }

        // ================================================================
        //  EL RENDER — NADA sobre el jugador: SOLO los impíos. LAS
        //  PEQUEÑAS PARTÍCULAS DEL HUMO (v6.112: el manto largo murió
        //  — motas de BrumaFX naciendo del punto de estancamiento,
        //  unos pasos por el hemisferio de fuga y se disuelven) y EL
        //  ARCO DE CHOQUE (la medialuna morada y negra con la concha
        //  abombada MIRANDO al portador).
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.gameMenu || Main.netMode == NetmodeID.Server)
                return false;

            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active)
                return false;

            try
            {
                VFXCore.CerrarLoteSiAbierto();
                float t = Main.GlobalTimeWrappedHourly;

                // LA LISTA de los impíos que arden por ESTE campo (los
                // más cercanos primero — el presupuesto de la casa).
                int vestidos = 0;
                for (int i = 0; i < Main.maxNPCs && vestidos < MAX_VESTIDOS; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active)
                        continue;
                    VacioGlobalNPC vacio;
                    try { vacio = n.GetGlobalNPC<VacioGlobalNPC>(); }
                    catch { continue; }
                    if (vacio.DuenoVacio != Projectile.owner + 1)
                        continue;
                    if (!VacioGlobalNPC.TieneQuemadura(n) || vacio.VisualQuemadura <= 0.05f)
                        continue;
                    // FUERA DE PANTALLA: nadie lo ve (el margen del
                    // semicírculo más su inclinación — 340 px de colchón)
                    Vector2 enPantalla = n.Center - Main.screenPosition;
                    if (enPantalla.X < -340f || enPantalla.X > Main.screenWidth + 340f ||
                        enPantalla.Y < -340f || enPantalla.Y > Main.screenHeight + 340f)
                        continue;

                    vestidos++;
                    VestirUnImpio(n, vacio, dueno, t);
                }
            }
            catch
            {
                try { VFXCore.CerrarLoteSiAbierto(); } catch { }
            }
            VFXCore.ReabrirLoteVanilla();
            return false;   // el ancla NUNCA se dibuja
        }

        /// <summary>EL VESTIDO DE UN IMPIO — las pequeñas partículas del
        /// humo (v6.112: motas naciendo del punto de estancamiento, unos
        /// pasos de fuga y se disuelven) y EL ARCO DE CHOQUE (la
        /// medialuna con la concha abombada mirando al portador).</summary>
        private static void VestirUnImpio(NPC n, VacioGlobalNPC vacio, Player dueno, float t)
        {
            float vive = vacio.VisualQuemadura;
            float k = MathHelper.Clamp(vacio.CercaniaLocal, 0f, 1f);
            int semilla = n.whoAmI * 13 + 7;

            Vector2 hacia = n.Center - dueno.MountedCenter;
            float largo = hacia.Length();
            if (largo < 1f)
                return;
            Vector2 dir = hacia * (1f / largo);   // portador → impío

            // ==============================================================
            //  v6.112 — LA BRUMA MURIÓ DISUELTA EN PARTÍCULAS (la letra:
            //  «en el aura del vacio la bruma es muy larga, mejor
            //  quitemosla de esa forma y dejemos solo pequeñas
            //  particulas»). LA AUTOPSIA: la v6.111 la había hecho
            //  HUIR (la estela viajaba 2.2-3.4 cúpulas + 110 px — un
            //  MANTO LARGO tras el impío) y el aliento violeta
            //  derramaba TRES VELOS enteros por la misma fuga: la
            //  bruma era una ESTELA, no un aura. LA CURA: el manto y
            //  los velos MURIERON — solo quedan LAS PEQUEÑAS
            //  PARTÍCULAS del humo: motas DIMINUTAS (4-8 px) que
            //  nacen del PUNTO DE ESTANCAMIENTO, dan UNOS PASOS por
            //  el hemisferio de fuga (medio cuerpo del impío, nada
            //  más) y se disuelven — la misma rampa de enfriamiento
            //  violácea de la casa (nace oscura, muere en VIOLETA
            //  CLARO: el borde SIEMPRE se lee) a escala de PARTÍCULA.
            //  (las chispas de PolvoExpulsionJuicio — la otra familia
            //  de partículas — siguen viviendo en el update).
            // ==============================================================
            Vector2 o = Main.screenPosition;
            try
            {
                BrumaFX.BeginMass();
                int motas = 5 + (int)(k * 4f);              // 5 lejos → 9 pegado (más densidad cuanto más cerca)
                for (int p = 0; p < motas; p++)
                {
                    float f0 = Frac(semilla * 0.83f + p * 0.618034f);
                    float ritmo = 0.16f + 0.12f * Frac(f0 * 9.7f);   // la vida de una mota: BREVE
                    float edad = Frac(t * ritmo + f0 * 4.1f + p * 0.233f);   // vida cíclica

                    // EL RUMBO — el MISMO abanico de fuga de las
                    // chispas (el hemisferio trasero ±90°, determinista
                    // por mota): las partículas también se alejan del
                    // portador — pero SOLO UNOS PASOS.
                    float abanico = (Frac(f0 * 13.7f) * 2f - 1f) * MathHelper.PiOver2;
                    Vector2 rumbo = dir.RotatedBy(abanico);

                    // EL VIAJE CORTO — la letra pidió FIN de la estela
                    // larga: la mota nace en el punto de
                    // estancamiento y muere a MEDIO CUERPO del impío
                    // (0.25-0.55 cúpulas — partícula de aura, no
                    // estela de reentrada).
                    float avance = edad * RadioDomo(n, k) * (0.25f + 0.30f * f0);
                    Vector2 pos = n.Center - dir * (RadioDomo(n, k) * 0.95f) + rumbo * avance;

                    // EL TAMAÑO DE PARTÍCULA — la mota DIMINUTA: 4-8 px
                    // (la v6.111 criaba mantos de 10-25 px).
                    float radio = (3.5f + 4.5f * f0) * (0.8f + 0.5f * edad);
                    // EL ALFA (×k — la intensidad variable de siempre,
                    // acotada abajo para que la mota sea mota).
                    float a = vive * MathF.Sin(edad * MathF.PI)
                        * (0.34f + 0.30f * k);
                    // LA RAMPA DE ENFRIAMIENTO (la de la casa): nace
                    // oscura y se disuelve en VIOLETA CLARO.
                    Color c = edad > 0.45f
                        ? Color.Lerp(new Color(26, 14, 44), new Color(122, 78, 188),
                            (edad - 0.45f) / 0.55f)
                        : new Color(26, 14, 44);
                    BrumaFX.Puff(pos - o, radio, c, semilla * 31 + p, t + f0 * 7f,
                        MathHelper.Clamp(a, 0.12f, 0.60f), quality: 0.6f,
                        velocity: rumbo * (14f + 20f * f0) * (0.5f + 0.5f * edad));
                }
            }
            catch { }
            finally
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            // ==============================================================
            //  v6.112 — EL ALIENTO VIOLETA MURIÓ (la letra: «la bruma es
            //  muy larga, mejor quitemosla… dejemos solo pequeñas
            //  particulas»): sus TRES VELOS aditivos derramándose por
            //  la estela eran la mitad LARGA del manto — cayeron con
            //  él. La lectura nocturna queda en las motas (la rampa
            //  violácea al disolverse) y en el arco de choque.
            // ==============================================================

            // ==============================================================
            //  v6.108 — LA MEDIALUNA = EL ARCO DE CHOQUE — «la parte
            //  abombada de la medialuna tiene que mirarme a mi en todo
            //  momento… la medialuna cambia de color volviendose
            //  ligeramente morada negra»: EL CALENTAMIENTO
            //  AERODINÁMICO — el portador es el PLANETA, su aura la
            //  ATMÓSFERA y el impío el objeto que CAE EN REENTRADA: el
            //  arco de choque lo envuelve POR DELANTE con la CONCHA
            //  ABOMBADA APUNTANDO SIEMPRE AL PORTADOR (en todo momento
            //  — si lo rodeas, el arco gira contigo), la parte plana
            //  disuelta abraza su corazón por detrás… y cuanto más
            //  HONDO cae (más cerca del portador), más MORADA NEGRA se
            //  vuelve (el choque se enfría en la sombra del vacío).
            // ==============================================================
            Texture2D perlin = PerlinSemi;
            if (perlin != null && !perlin.IsDisposed)
            {
                float late = 0.94f + 0.06f * MathF.Sin(t * 7f + semilla * 1.7f);

                // EL TAMAÑO — EL RADIO DE LA CÚPULA (TODO el teatro
                // comparte la MISMA: RadioDomo) y crece con la cercanía
                // (el fuego pegado lo ENVUELVE), con la respiración de
                // la casa.
                float ancho = RadioDomo(n, k) * 2f * late;
                float esc = ancho / 192f;                    // el semicírculo mide 2R=192 en la textura

                // LA ROTACIÓN DEL CHOQUE — LA CONCHA ME MIRA: el impío
                // «cae» hacia el portador (el planeta) y el arco de
                // choque lo envuelve POR DELANTE — la parte ABOMBADA
                // apunta SIEMPRE al portador (EN TODO MOMENTO: si lo
                // rodeas, el arco gira contigo)… y EL BAMBOLEO lento
                // (la concha respira en el viento de la reentrada).
                float rot = dir.ToRotation() - MathHelper.PiOver2
                    + 0.05f * MathF.Sin(t * 0.8f + semilla * 0.7f);

                // LA BASE EN EL CUERPO — el diámetro de la textura vive a
                // 22 px bajo su centro (y=150 del lienzo de 256): el
                // CENTRO del semicírculo cae SOBRE el corazón del impío
                // (la parte plana disuelta lo abraza por detrás) y la
                // CONCHA SE ABOMBA hacia el portador hasta ~RadioDomo
                // de su pecho — el choque se planta DELANTE del cuerpo.
                Vector2 baseTex = new Vector2(0f, 22f * esc).RotatedBy(rot);
                Vector2 pos = n.Center - baseTex;

                Vector2 tamPx = new Vector2(256f * esc, 256f * esc);
                Vector2 tamEco = new Vector2(288f * esc, 288f * esc);   // EL ECO (más grande, más tenue)

                // EL COLOR DEL CHOQUE — v6.108: «la medialuna cambia de
                // color volviendose ligeramente morada negra»: cuanto
                // más HONDO cae el impío en la atmósfera del portador,
                // el arco SE ENFRÍA EN SOMBRA — el brillo violeta se
                // apaga hacia el MORADO NEGRO del vacío (de lejos es la
                // medialuna violeta de siempre; pegado, una concha
                // morada-negra).
                Color negroVioleta = new(26, 12, 44);
                Color violeta = new(150, 84, 235);
                Color moradaNegra = new(46, 20, 78);
                Color colorAlfa = Color.Lerp(negroVioleta, moradaNegra, k);
                Color colorLuz = Color.Lerp(violeta, moradaNegra, 0.85f * k);

                // (1) EL PASO ALFA — la sombra que OSCURECE (y se ahonda
                //     con la cercanía): la medialuna se lee TAMBIÉN DE
                //     DÍA (la lección de la .103: el aditivo se lava con
                //     el sol).
                VFXCore.Begin();
                VFXCore.Quad(pos, colorAlfa * ((0.42f + 0.30f * k) * vive),
                    tamPx, rot, perlin);
                VFXCore.FlushAlpha();

                // (2) EL PASO ADITIVO — la energía que SUMA se APAGA
                //     hacia el morado negro cuando el impío cae hondo
                //     (las vetas del Perlin se enfrían en sombra)…
                VFXCore.Begin();
                VFXCore.Quad(pos, colorLuz * ((0.26f + 0.26f * k) * vive),
                    tamPx, rot, perlin);
                //     …Y SU ECO — una segunda medialuna más GRANDE y
                //     TENUE (la aura que gotea del choque hacia afuera).
                VFXCore.Quad(pos, colorLuz * ((0.12f + 0.14f * k) * vive),
                    tamEco, rot, perlin);
                VFXCore.FlushAdditive();
            }
        }

        /// <summary>v6.106 → v6.108 — EL RADIO DEL ARCO DE CHOQUE — LA
        /// MISMA cúpula para TODO el teatro: la medialuna dibujada, el
        /// punto de estancamiento donde nace el plasma (chispas y
        /// bruma) y la estela que dejan. Crece con la cercanía del
        /// impío (el fuego pegado lo ENVUELVE) y viste su cuerpo ENTERO.</summary>
        private static float RadioDomo(NPC n, float k) =>
            (MathF.Max(n.width, n.height) * 0.5f + 17f) * (0.85f + 0.45f * k);

        /// <summary>El Remap acotado del estandarte.</summary>
        private static float Remap(float v, float in1, float in2, float out1, float out2)
        {
            float kk = (v - in1) / (in2 - in1);
            if (kk < 0f) kk = 0f;
            else if (kk > 1f) kk = 1f;
            return out1 + (out2 - out1) * kk;
        }

        /// <summary>El Frac determinista de la casa.</summary>
        private static float Frac(float v)
        {
            float f = v - MathF.Floor(v);
            return f < 0f ? f + 1f : f;
        }
    }
}
