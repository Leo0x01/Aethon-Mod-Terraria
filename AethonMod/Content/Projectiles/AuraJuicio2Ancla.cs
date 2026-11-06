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
    //  AuraJuicio2Ancla — v6.105 → v6.106 — EL ANCLA INVISIBLE DEL
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
    //  LA CURA v6.106 — TODO EL TEATRO COMPARTE EL MISMO ARCO:
    //   · LA MEDIALUNA — la textura REFORJADA disuelve la parte recta
    //     HACIA ADENTRO (isopletas circulares DENTRO de la cúpula; el
    //     rabo de la v6.105 que salía bajo el diámetro MURIÓ): solo
    //     vive la CONCHA del arco — una medialuna de Perlin morada y
    //     negra cuya parte PLANA (la disuelta) mira AL PORTADOR y cuyo
    //     arco se abomba EN DIRECCIÓN OPUESTA a él (el jugador a la
    //     derecha → la medialuna abraza la espalda del impío abriéndose
    //     a la IZQUIERDA).
    //   · LAS CHISPAS DEL ARCO — cada una nace LIBERADA (su centro y su
    //     radio quedan congelados al nacer) y RECORRE el mismo
    //     semicírculo: parte del cuerno de arranque ALEJÁNDOSE del
    //     portador, envuelve la cúpula por la espalda y llega al cuerno
    //     de llegada ACELERANDO — cada vez más rápida en el tramo que
    //     VUELVE hacia el jugador, donde se disuelve.
    //   · LA BRUMA — el MISMO recorrido del arco y la MISMA dirección
    //     (nace lenta en el cuerno de arranque y acelera por la cúpula),
    //     con INTENSIDAD VARIABLE (alfa, tamaño y número de puffs crecen
    //     con la cercanía del impío).
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
    //      solo existe en el enemigo): LA MEDIALUNA PERLIN + LAS
    //      CHISPAS QUE LA RECORREN + LA BRUMA QUE LA ENVUELVE — todo
    //      sobre EL MISMO ARCO (ver LA CURA v6.106 arriba).
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
            //  2) LAS PARTÍCULAS PEQUEÑAS + LA BRUMA (solo cliente — el
            //     teatro de los impíos; la MISMA DIRECCIÓN que las
            //     partículas originales del aura: expulsadas del flanco
            //     que MIRA al portador, ALEJÁNDOSE de él).
            // ==============================================================
            if (Main.netMode != NetmodeID.Server && !Main.dedServ && !Main.gameMenu)
                VestirALosImpios(dueno);
        }

        // ================================================================
        //  EL TEATRO DE LOS IMPIOS — LAS PARTÍCULAS PEQUEÑAS Y LA BRUMA
        //  (fase de UPDATE: el spawn de polvos; la bruma y el semicírculo
        //  viven en el render).
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
                // nacen en el flanco que MIRA al portador y salen
                // EXPULSADAS alejándose (la misma dirección de las
                // partículas originales del aura de juicio).
                Vector2 hacia = n.Center - dueno.MountedCenter;
                float largo = hacia.Length();
                if (largo < 1f)
                    continue;
                Vector2 dir = hacia * (1f / largo);

                // ----------------------------------------------------------
                //  v6.106 — LAS CHISPAS DEL ARCO: «las particulas deben
                //  moverse en la direccion opuesto al jugador, o sea el
                //  jugador esta a la derecha del enemigo y las
                //  particulas del enemigo se mueven a la izquierda, asi
                //  para todas las direcciones, ademas cada particula se
                //  debe liberar y deben tener el mismo recorrido que el
                //  semcirculo, la velocidad de las particulas debe ir
                //  cada vez mas rapido a medida que se acercan al
                //  jugador».
                //
                //  CADA CHISPA NACE LIBERADA con su arco propio — EL
                //  MISMO RECORRIDO del semicírculo Perlin: parte del
                //  cuerno de arranque ALEJÁNDOSE del portador (jugador a
                //  la derecha → nace por el costado y corre a la
                //  IZQUIERDA), envuelve la cúpula por la espalda del
                //  impío y llega al cuerno de llegada ACELERANDO (el
                //  tramo que VUELVE hacia el jugador es el MÁS RÁPIDO —
                //  allí se disuelve). Cuanto más pegado arde el impío,
                //  más rápida nace y más denso hierve el arco.
                // ----------------------------------------------------------
                int ritmo = (int)MathHelper.Clamp(4f - k * 2f, 1f, 4f);  // 1 de cada 4 lejos… de cada 2 pegado
                if ((Projectile.ai[0] + i) % ritmo == 0f && n.Center.DistanceSQ(dueno.Center) < 640f * 640f)
                {
                    // LA GEOMETRÍA DEL SEMICÍRCULO — el mismo arco que
                    // dibuja la medialuna: el cuerno de arranque cae al
                    // costado del impío y el barrido (ángulo CRECIENTE)
                    // sale ALEJÁNDOSE del portador, pasa por la espalda
                    // (phi+PI) y vuelve al cuerno de llegada.
                    float phi = dir.ToRotation();               // portador → impío
                    float radioDomo = RadioDomo(n, k);          // LA MISMA cúpula de la medialuna
                    float a0 = phi - MathHelper.PiOver2;        // el cuerno de ARRANQUE
                    float a1 = phi + MathHelper.PiOver2;        // el cuerno de LLEGADA

                    int rafaga = k > 0.55f && Main.rand.NextFloat() < 0.5f ? 2 : 1;
                    for (int s = 0; s < rafaga; s++)
                    {
                        // escalonadas por el arranque del arco (la
                        // corriente parte del costado y envuelve)
                        float ang = a0 + Main.rand.NextFloat(0f, 0.20f);
                        float radio = radioDomo * Main.rand.NextFloat(0.68f, 0.95f);

                        // LA VELOCIDAD — arranca suave y ACELERA SIN
                        // FRENO (cada vez más rápido hacia el cuerno de
                        // llegada); la furia escala con la cercanía.
                        float vBase = (0.045f + 0.038f * k) * Main.rand.NextFloat(0.85f, 1.2f);
                        float acel = vBase * (0.055f + 0.05f * k);

                        // LIBERADA — el centro queda congelado AL NACER:
                        // aunque el impío camine, la chispa termina SU
                        // semicírculo sola (PolvoArcoJuicio manda).
                        var arco = new PolvoArcoJuicio.Arco
                        {
                            Centro = n.Center,
                            Ang0 = a0,
                            Ang = ang,
                            AngFin = a1,
                            VelAng = vBase,
                            Acel = acel,
                            Radio = radio
                        };

                        // EL COLOR DE LA CASA — el halo violeta: más
                        // CLARO cuanto más cerca arde (la temperatura).
                        Color halo = Color.Lerp(
                            new Color(150, 84, 235), new Color(224, 176, 255), k);
                        Vector2 posChispa = n.Center
                            + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                        Dust d = Dust.NewDustPerfect(posChispa,
                            ModContent.DustType<PolvoArcoJuicio>(), Vector2.Zero,
                            0, halo, Main.rand.NextFloat(0.24f, 0.38f));   // MENUDAS (la letra)
                        d.customData = arco;
                        d.noGravity = true;
                        d.noLight = false;
                    }
                }
            }
            Projectile.ai[0] += 1f;
        }

        // ================================================================
        //  EL RENDER — NADA sobre el jugador: SOLO los impíos. LA BRUMA
        //  (los puffs de BrumaFX en la misma dirección e intensidad
        //  variable) y EL SEMICÍRCULO PERLIN (la media cúpula morada y
        //  negra con el difuminado circular en su parte plana).
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

        /// <summary>EL VESTIDO DE UN IMPIO — la bruma (misma dirección,
        /// intensidad variable) y el SEMICÍRCULO PERLIN.</summary>
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
            //  v6.106 — LA BRUMA RECORRE EL MISMO ARCO — «en la misma
            //  direccion e intensidad variable como las particulas
            //  iniciales»: cada puff NACE en el cuerno de arranque y
            //  ACELERA por el semicírculo (prog^2.3: nace lenta, cada
            //  vez más rápida — la misma aceleración de las chispas),
            //  envolviendo la cúpula ALEJÁNDOSE del portador y volviendo
            //  por su espalda hasta el cuerno de llegada. INTENSIDAD
            //  VARIABLE: el alfa, el tamaño y el NÚMERO de puffs crecen
            //  con la cercanía del impío (cuanto más pegado arde, más
            //  bruma respira el arco).
            // ==============================================================
            Vector2 o = Main.screenPosition;
            try
            {
                BrumaFX.BeginMass();
                float phi = dir.ToRotation();
                float radioDomo = RadioDomo(n, k);
                float a0 = phi - MathHelper.PiOver2;        // EL MISMO cuerno de arranque de las chispas
                int puffs = 3 + (k > 0.6f ? 1 : 0);         // INTENSIDAD VARIABLE: más puffs pegado
                for (int p = 0; p < puffs; p++)
                {
                    float f0 = Frac(semilla * 0.83f + p * 0.618034f);
                    float ritmo = 0.085f + 0.075f * Frac(f0 * 9.7f);
                    float edad = Frac(t * ritmo + f0 * 4.1f + p * 0.233f);   // vida cíclica

                    // EL MISMO RECORRIDO DEL SEMICÍRCULO — la posición es
                    // la del ARCO (el mismo que las chispas y la medialuna)
                    // y el progreso ACELERA (empieza lento, termina volando).
                    float prog = MathF.Pow(edad, 2.3f);
                    float ang = a0 + prog * MathHelper.Pi;
                    Vector2 pos = n.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang))
                        * (radioDomo * (1.00f + 0.24f * f0));

                    float radio = (6f + 9f * f0) * (0.7f + 0.8f * edad)
                        * (0.8f + 0.5f * k);                 // …y del TAMAÑO de su cuerpo
                    float a = vive * MathF.Sin(edad * MathF.PI)
                        * (0.16f + 0.34f * k);               // INTENSIDAD VARIABLE (×k)
                    Color c = edad > 0.55f
                        ? Color.Lerp(new Color(30, 16, 50), new Color(96, 54, 150),
                            (edad - 0.55f) / 0.45f)
                        : new Color(30, 16, 50);
                    // LA TANGENTE del barrido — el humo corre por donde
                    // corre el arco (la MISMA dirección de las chispas).
                    Vector2 tangente = new Vector2(-MathF.Sin(ang), MathF.Cos(ang));
                    BrumaFX.Puff(pos - o, radio, c, semilla * 31 + p, t + f0 * 7f,
                        MathHelper.Clamp(a, 0.04f, 0.55f), quality: 0.5f,
                        velocity: tangente * (14f + 18f * f0));
                }
            }
            catch { }
            finally
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            // ==============================================================
            //  v6.106 — LA MEDIALUNA PERLIN — «el semicirculo esta al
            //  revez […] el semicirculo debe tener la parte recta mas
            //  difuminada algo asi como una cupula o medialuna». LA
            //  CURA: la textura REFORJADA disuelve la parte recta HACIA
            //  ADENTRO (isopletas circulares DENTRO de la cúpula — el
            //  rabo de la v6.105 que goteaba bajo el diámetro hacia el
            //  jugador MURIÓ): solo vive la CONCHA del arco, una
            //  MEDIALUNA de Perlin morada y negra. Y LA INCLINACIÓN:
            //  la parte PLANA (la disuelta) mira AL PORTADOR y el arco
            //  se abomba EN DIRECCIÓN OPUESTA a él — el jugador a la
            //  derecha → la medialuna abraza la espalda del impío
            //  abriéndose a la IZQUIERDA, y las chispas y la bruma
            //  RECORREN ese mismo arco alejándose del jugador.
            // ==============================================================
            Texture2D perlin = PerlinSemi;
            if (perlin != null && !perlin.IsDisposed)
            {
                float late = 0.94f + 0.06f * MathF.Sin(t * 7f + semilla * 1.7f);

                // EL TAMAÑO — EL RADIO DEL ARCO QUE CHISPAS Y BRUMA
                // RECORREN (TODO el teatro comparte LA MISMA cúpula:
                // RadioDomo) y crece con la cercanía (el fuego pegado
                // lo ENVUELVE), con la respiración de la casa.
                float ancho = RadioDomo(n, k) * 2f * late;
                float esc = ancho / 192f;                    // el semicírculo mide 2R=192 en la textura

                // LA INCLINACIÓN — la parte plana disuelta AL PORTADOR,
                // el arco ALEJÁNDOSE de él… y EL BAMBOLEO lento (la
                // medialuna respira sobre la espalda del impío).
                float rot = dir.ToRotation() + MathHelper.PiOver2
                    + 0.05f * MathF.Sin(t * 0.8f + semilla * 0.7f);

                // LA BASE EN EL CUERPO — el diámetro de la textura vive a
                // 22 px bajo su centro (y=150 del lienzo de 256): la
                // PARTE PLANA DISUELTA cae SOBRE el corazón del impío y
                // la concha abraza su espalda.
                Vector2 baseTex = new Vector2(0f, 22f * esc).RotatedBy(rot);
                Vector2 pos = n.Center - baseTex;

                Vector2 tamPx = new Vector2(256f * esc, 256f * esc);
                Vector2 tamEco = new Vector2(288f * esc, 288f * esc);   // EL ECO (más grande, más tenue)

                Color negroVioleta = new(26, 12, 44);
                Color violeta = new(150, 84, 235);

                // (1) EL PASO ALFA — el negro-violeta que OSCURECE: la
                //     medialuna se lee TAMBIÉN DE DÍA (la lección de la
                //     .103: el aditivo se lava con el sol).
                VFXCore.Begin();
                VFXCore.Quad(pos, negroVioleta * ((0.40f + 0.26f * k) * vive),
                    tamPx, rot, perlin);
                VFXCore.FlushAlpha();

                // (2) EL PASO ADITIVO — la energía violeta que SUMA: las
                //     vetas del Perlin prenden en la oscuridad…
                VFXCore.Begin();
                VFXCore.Quad(pos, violeta * ((0.26f + 0.32f * k) * vive),
                    tamPx, rot, perlin);
                //     …Y SU ECO — una segunda medialuna más GRANDE y
                //     TENUE (la aura que gotea del arco hacia afuera).
                VFXCore.Quad(pos, violeta * ((0.12f + 0.16f * k) * vive),
                    tamEco, rot, perlin);
                VFXCore.FlushAdditive();
            }
        }

        /// <summary>v6.106 — EL RADIO DEL SEMICÍRCULO — LA MISMA CÚPULA
        /// para TODO el teatro: la medialuna dibujada, las chispas que la
        /// recorren y la bruma que la envuelve. Crece con la cercanía del
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
