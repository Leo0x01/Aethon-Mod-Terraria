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
    //  AuraJuicio2Ancla — v6.105 → v6.106 → v6.107 → v6.108 — EL ANCLA INVISIBLE DEL
    //  AURA DE JUICIO 2 (la copia silenciosa: el efecto del
    //  ESTANDARTE, el teatro SOLO en los enemigos).
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
        /// <summary>LA DISTANCIA ÍNTIMA del estandarte (a 40 px del
        /// impío, el fuego pleno).</summary>
        private const float DIST_INTIMA = 40f;

        /// <summary>Ticks que el impío sigue ARDIENDO ya fuera del
        /// campo (el timer 180 del estandarte).</summary>
        private const int ARDE_FUERA = 180;

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
            // ==============================================================
            if (Projectile.owner == Main.myPlayer && !Main.gameMenu)
            {
                int nivelDespertar = dueno.GetModPlayer<ShardPlayer>().NivelDespertar;
                int danoBase = 20 + 6 * nivelDespertar;   // HP/s a intensidad ×1

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy() || n.dontTakeDamage)
                        continue;

                    VacioGlobalNPC vacio = n.GetGlobalNPC<VacioGlobalNPC>();

                    // FUERA DEL RADIO: si arde por NOSOTROS, el fuego se
                    // enfría poco a poco (el debuff muere solo a los 180).
                    Vector2 borde = Vector2.Clamp(Projectile.Center, n.TopLeft, n.BottomRight);
                    float dist = Vector2.Distance(Projectile.Center, borde);
                    float perdon = Math.Max(n.width, n.height) * 0.5f + 40f;
                    if (dist > EstandarteJuicioAuraProjectile.RADIO + perdon)
                    {
                        if (vacio.DuenoVacio == dueno.whoAmI + 1 && vacio.IntensidadQuemadura > 0f)
                            vacio.IntensidadQuemadura *= 0.995f;
                        continue;
                    }

                    // DENTRO — LA MARCA: la Quemadura del Vacío Exterior
                    // (se refresca mientras permanezca; fuera, sus últimos
                    // 180 ticks de fuego — el timer del estandarte).
                    n.AddBuff(ModContent.BuffType<QuemaduraVacioExterior>(), ARDE_FUERA, false);

                    // LA CUENTA DEL DUEÑO (quién cobra el buff del vacío).
                    vacio.DuenoVacio = dueno.whoAmI + 1;
                    vacio.DanoVacio = danoBase;

                    // LA INTENSIDAD — la técnica Remap del estandarte:
                    // ×1 en el borde → ×3 PEGADO, más LA RAMPA DEL TIEMPO
                    // (cada segundo ardiendo lo enciende, hasta ×1.5).
                    float cercania = Remap(dist, EstandarteJuicioAuraProjectile.RADIO + perdon,
                        DIST_INTIMA, 0f, 1f);
                    float rampaTiempo = 1f + 0.5f * Math.Min(1f, vacio.VisualQuemadura);
                    vacio.IntensidadQuemadura = (1f + (INTENSIDAD_CERCA - 1f) * cercania)
                        * rampaTiempo;
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
        //  EL RENDER — NADA sobre el jugador: SOLO los impíos. LA BRUMA
        //  DE LA REENTRADA (los puffs de BrumaFX naciendo del punto de
        //  estancamiento y derramándose hacia atrás, en la misma
        //  dirección e intensidad variable que las chispas) y EL ARCO
        //  DE CHOQUE (la medialuna morada y negra con la concha
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

        /// <summary>EL VESTIDO DE UN IMPIO — la bruma de la reentrada
        /// (nace del punto de estancamiento, misma dirección e
        /// intensidad variable que las chispas) y EL ARCO DE CHOQUE (la
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
            //  v6.108 — LA BRUMA DE LA REENTRADA — «la bruma debe hacer
            //  lo mismo que las particulas»: cada puff NACE en el PUNTO
            //  DE ESTANCAMIENTO (el tope abombado de la medialuna) y se
            //  derrama hacia ATRÁS en TODAS las direcciones de fuga
            //  (el MISMO abanico ±90° de las chispas — SIEMPRE
            //  alejándose del portador). INTENSIDAD VARIABLE: el alfa,
            //  el tamaño y el NÚMERO de puffs crecen con la cercanía
            //  del impío (cuanto más hondo cae, más densa la estela).
            // ==============================================================
            Vector2 o = Main.screenPosition;
            try
            {
                BrumaFX.BeginMass();
                int puffs = 2 + (int)(k * 3f);              // LA DENSIDAD: 2 lejos → 5 pegado (más bruma cuanto más cerca)
                for (int p = 0; p < puffs; p++)
                {
                    float f0 = Frac(semilla * 0.83f + p * 0.618034f);
                    float ritmo = 0.085f + 0.075f * Frac(f0 * 9.7f);
                    float edad = Frac(t * ritmo + f0 * 4.1f + p * 0.233f);   // vida cíclica

                    // EL RUMBO DEL PUFF — el MISMO abanico de fuga de
                    // las chispas: TODAS las direcciones que se ALEJAN
                    // del portador (el hemisferio trasero ±90°,
                    // determinista por puff).
                    float abanico = (Frac(f0 * 13.7f) * 2f - 1f) * MathHelper.PiOver2;
                    Vector2 rumbo = dir.RotatedBy(abanico);

                    // NACE EN EL PUNTO DE ESTANCAMIENTO (el tope abombado
                    // de la medialuna) y se derrama HACIA ATRÁS por su
                    // rumbo (la MISMA estela de las chispas).
                    float avance = edad * RadioDomo(n, k) * (1.3f + 0.6f * f0);
                    Vector2 pos = n.Center - dir * (RadioDomo(n, k) * 0.95f) + rumbo * avance;

                    float radio = (6f + 9f * f0) * (0.7f + 0.8f * edad)
                        * (0.8f + 0.5f * k);                 // …y del TAMAÑO de su cuerpo
                    float a = vive * MathF.Sin(edad * MathF.PI)
                        * (0.16f + 0.34f * k);               // INTENSIDAD VARIABLE (×k)
                    Color c = edad > 0.55f
                        ? Color.Lerp(new Color(30, 16, 50), new Color(96, 54, 150),
                            (edad - 0.55f) / 0.45f)
                        : new Color(30, 16, 50);
                    // LA DERIVA — el humo se derrama por donde vuelan
                    // las chispas (la MISMA dirección de la fuga).
                    BrumaFX.Puff(pos - o, radio, c, semilla * 31 + p, t + f0 * 7f,
                        MathHelper.Clamp(a, 0.04f, 0.55f), quality: 0.5f,
                        velocity: rumbo * (10f + 16f * f0));
                }
            }
            catch { }
            finally
            {
                try { Main.spriteBatch.End(); } catch { }
            }

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
