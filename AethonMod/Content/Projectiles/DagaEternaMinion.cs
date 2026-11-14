using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles
{
    /// <summary>
    /// DAGA ETERNA MINION — v6.124 — LAS CINCO DAGAS DEL ETERNO (la letra
    /// del usuario: «quiero que esas dagas sean las siguiente invocacion
    /// despues de los 11 ojos, vienen las 5 dagas que tambien tendran un
    /// ojo en el centro jejejejej»).
    ///
    /// LA TÉCNICA ES 100 % COPIADA DEL DAZZLING STABBER STAFF (la letra:
    /// «toma lo que aprendiste del invocador de dagas del Eterno» — el
    /// comportamiento de cuchillas orbitantes nacido de una
    /// investigación de la wiki, hoy máquina 100 % de la casa):
    /// un escuadrón de DAGAS PERSISTENTES, cada una con SU técnica
    /// propia, que VUELAN alrededor del portador y se LANZAN contra las
    /// presas — y TODAS prenden la QUEMADURA DEL VACÍO (nuestra Holy
    /// Flames: el stabber prende Holy Flames al 100 % por 3 s — aquí es
    /// la quemadura del vacío al 100 % por 3 s, 180 t):
    ///   · DAGA DE CRISTAL (ai[0]=0) — «Crystal daggers rapidly slash
    ///     enemies before briefly pausing, dealing 50% base damage but
    ///     having an innate 20 armor penetration»: RÁFAGA de tres tajos
    ///     rápidos A TRAVÉS de la presa (×0,5 del daño, +20 de
    ///     perforación) y una PAUSA breve antes de volver — el compás
    ///     EXACTO del original.
    ///   · DAGA DE LLAMA (1) — «Flame daggers swipe at enemies in a wide
    ///     arcing motion»: un BARRIDO EN ARCO AMPLIO que cruza la zona
    ///     de la presa peinándolo todo (×1,0).
    ///   · DAGA DE ROCA (2) — «Rock daggers lunge at enemies and recoil
    ///     after hits, dealing 300% base damage»: la EMBESTIDA de
    ///     lejos, el golpe ×3,0 y el RECUDE hacia atrás antes de volver.
    ///   · DAGA DEL VACÍO (3) — la cuarta de la casa: se LANZA, SE CLAVA
    ///     en la presa y GOTEA el vacío (×0,35 cada 15 t mientras vive
    ///     clavada, 45 t — la técnica de Las Agujas del Vacío que el
    ///     dueño ama, viviendo en el bucle del stabber).
    ///   · DAGA DEL VEREDICTO (4) — la quinta de la casa: MARCA a la
    ///     presa (el anillo dorado del veredicto) y la SENTENCIA con un
    ///     golpe puntual ×1,5 y su estallido de oro.
    ///
    /// EL OJO EN EL CENTRO (la letra: «tambien tendran un ojo en el
    /// centro»): cada daga lleva la ALMENDRA en el corazón de la hoja y
    /// SU IRIS VIVE — dibujado aparte en runtime, MIRA a la presa (el
    /// iris que manda, la firma de los Ojos del Eterno): el escuadrón
    /// entero es la mirada del Eterno hecha filo.
    ///
    /// LA FORMACIÓN: el anillo ÍNTIMO del portador (radio 32, girando
    /// despacio — más adentro que los ojos: los ojos son la corona, las
    /// dagas son la guardia) — y cuando el espíritu del Eterno sale a
    /// cazar, el anillo entero SE TRASLADA al libro (la ley v6.51.7 de
    /// los ojos, en su círculo pequeño).
    ///
    /// LA LEY DEL DAÑO (v6.51: «el daño es del grimorio solamente, sin
    /// importar la armadura o accesorios»): nace con
    /// WeaponScaling.DanoOjosDelEterno — la MISMA cadena de los ojos.
    ///
    /// LA ESCALERA (la letra): las dagas son LA SIGUIENTE INVOCACIÓN
    /// después de los 11 ojos — con el escuadrón de ojos LLENO, el clic
    /// derecho del Grimorio invoca a la siguiente daga que falte (la
    /// secuencia del stabber: cristal → llama → roca → vacío →
    /// veredicto); con las CINCO vivas, la invocación vuelve a
    /// FORTALECER a los ojos (las mejoras de siempre).
    /// </summary>
    public class DagaEternaMinion : ModProjectile
    {
        // === LOS TIPOS (ai[0]) — la secuencia del stabber ===
        public const int TIPO_CRISTAL = 0;
        public const int TIPO_LLAMA = 1;
        public const int TIPO_ROCA = 2;
        public const int TIPO_VACIO = 3;
        public const int TIPO_VEREDICTO = 4;
        public const int TiposMax = 5;

        /// <summary>El tope del escuadrón de dagas (la letra: «vienen
        /// las 5 dagas»).</summary>
        public const int TechoDagas = 5;

        // v6.133 — LA MÁQUINA DEL STABBER ES DE LA CASA (la letra:
        // «con el comportamiento ya creado de las cuchillas, puedes
        // borrar Dazzling Stabber Staff de Calamity Mod del codigo y
        // cualquier referencia»): EL ARMA DE INVESTIGACIÓN murió con sus
        // sprites y su buff — la MÁQUINA VIVE porque ya es nuestra: la
        // casa la forjó en la v6.132 para curar el ataque muerto (LAS
        // FASES con el reloj en Projectile.frame que NADIE avanzaba en
        // F_GOLPE — las cuchillas congeladas a 70 px de la presa) y hoy
        // no debe NADA a nadie: VELOCIDAD (nada de lerp de posición —
        // el cuchillo ACELERA 1,03× y FRENA 0,93× cerca del objetivo,
        // el giro 0,15), EL DESCANSO (attackCooldown tras los golpes:
        // 80-95 el cristal a las 6 cuchilladas · 45-60 la roca tras su
        // embestida · 5 la llama), AttackDelay del empuje de la carga
        // y la vuelta al descanso orbital (moveSmoothing 50→2).
        // ai[1] ES el RestOffsetAngle: el ángulo del puesto de
        // descanso de ESTE cuchillo en el abanico — asignado al nacer,
        // JAMÁS una fase.
        private float RestOffsetAngle => Projectile.ai[1];

        /// <summary>EL DESCANSO del stabber (attackCooldown):
        /// los ticks sin atacar tras la última cuchillada — el cristal
        /// descansa 80-95 tras 6 golpes, la roca 45-60 tras la
        /// embestida, la llama 5 entre barridos.</summary>
        private int attackCooldown;

        /// <summary>Las cuchilladas de ESTA ronda (el cristal cuenta 6
        /// antes de descansar — la cadencia exacta de la casa).</summary>
        private int attacksDone;

        /// <summary>LA SUAVIZACIÓN DE LA VUELTA (moveSmoothing 50→2): el acercamiento al puesto de descanso cada vez más
        /// directo — arruja suave, aterriza directo.</summary>
        private float moveSmoothing = 50f;

        /// <summary>El multiplicador de daño por tipo (damageMult — 0,5 cristal con 20 de perforación · 3 roca · 1
        /// llama·vacío·veredicto).</summary>
        private float damageMult = 1f;

        /// <summary>EL OBJETIVO FIJADO (storedPos): la
        /// posición donde vuela el cuchillo — se fija al abrir la
        /// ronda y el cuchillo la PERSIGUE (el original congela el
        /// objetivo al arrancar: no re-aima cada tick).</summary>
        private Vector2 storedPos;

        /// <summary>¿Acaba de golpear? (los efectos del on-hit — justHit).</summary>
        private bool justHit;

        /// <summary>EL RETARDO DEL EMPUJE (AttackDelay en localAI[0]: ai[0] ES EL TIPO del alma): el
        /// cuchillo que acaba de cortar espera ANTES de volver a
        /// cargar — el empuje que lee el ojo.</summary>
        private float AttackDelay
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        // === LA PUERTA DE LAS DAGAS (la letra: «la siguiente invocación
        //     después de los 11 ojos») — la llama el Grimorio ===
        public static int DagasVivas(Player p)
        {
            int tipo = ModContent.ProjectileType<DagaEternaMinion>();
            int n = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr != null && pr.active && pr.owner == p.whoAmI && pr.type == tipo) n++;
            }
            return n;
        }

        /// <summary>¿Vive ya una daga de ESTE tipo? (la secuencia del
        /// stabber: se invoca la PRIMERA que falte).</summary>
        public static bool ViveTipo(Player p, int tipoDaga)
        {
            int tipo = ModContent.ProjectileType<DagaEternaMinion>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr != null && pr.active && pr.owner == p.whoAmI && pr.type == tipo &&
                    (int)pr.ai[0] == tipoDaga)
                    return true;
            }
            return false;
        }

        public override void SetStaticDefaults()
        {
            // LA TIRA DE LAS CINCO (el spritesheet de la casa: 5 marcos
            // de 34×34, uno por daga — el marco se elige por ai[0]).
            Main.projFrames[Type] = TiposMax;
            Main.projPet[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
        }

        // LA TIRA ES LA TEXTURA (el spritesheet DagaEterna_Hoja, 34×170 —
        // el guardián de la casa exige su PNG y el PreDraw recorta SU marco
        // de 34×34 por tipo).
        public override string Texture => "AethonMod/Content/Projectiles/DagaEterna_Hoja";

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            // v6.131 — LA CORONA GRANDE (la letra: «las cuchillas son
            // muy pequeñas, que sean un poco mas grande»): la hoja se
            // dibuja ×1,4 (47×47 — el filo SE LEE) y el CUERPO del
            // proyectil crece con ella (hitbox 26 — el golpe cubre la
            // hoja grande).
            Projectile.scale = 1.40f;
            Projectile.friendly = true;
            Projectile.minion = true;
            // v6.129 — LOS PUESTOS LIBERADOS (la letra: «haz que esta arma
            // no sea de invocador asi no se ven afectado los minions»): la
            // daga cobraba 1 puesto de minion — la maquinaria vanilla
            // (maxMinions) MATABA a los minions que no cabían: los del
            // jugador y los propios del escuadrón (¡16 puestos contra 1-11
            // del portador: moría TODO al nacer!). minionSlots 0: el
            // escuadrón tiene SU PROPIO contador (el cetro: 11 ojos + 5
            // dagas — nada de puestos de vanilla): no desplaza a NADIE y
            // nadie lo mata. (minion sigue true: el targeting derecho del
            // jugador y el minionPos del anillo VIVEN.)
            Projectile.minionSlots = 0f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.aiStyle = -1;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;   // el goteo del clavado: un golpe cada 15 t
            Projectile.light = 0.3f;
        }

        // EL FILO ES EL ATAQUE (a diferencia del ojo, que dispara su
        // rayo): la daga golpea CON SU CUERPO — el bucle del stabber.
        public override bool MinionContactDamage() => true;

        /// <summary>v6.132 — EL DESCANSO APAGA EL FILO (CanDamage =>
        /// attackCooldown == 0): la cuchilla que descansa
        /// NO corta de paso — solo la cuchilla en ronda.</summary>
        public override bool? CanDamage() => attackCooldown == 0 ? null : false;

        // ================================================================
        //  EL NIVEL (el patrón de los ojos: ai[2] desde el spawn)
        // ================================================================
        private int Level
        {
            get
            {
                int nivel = (int)Projectile.ai[2];
                if (nivel < 1) nivel = 1;
                return nivel;
            }
        }

        /// <summary>LA VELOCIDAD DE NIVEL (la ley de los ojos: 1,0× en
        /// nivel 1 subiendo LINEAL hasta 1,5× en el 100 — el compás del
        /// stabber se acelera con el libro).</summary>
        private float VelocidadNivel => 1f + 0.5f * Math.Min(Level - 1, 99) / 99f;

        /// <summary>EL RITMO PROPIO (la ley de los ojos: cada daga lleva
        /// SU velocidad personal 0,85–1,15 — el escuadrón JAMÁS ataca en
        /// bloque).</summary>
        private float RitmoPropio =>
            0.85f + 0.30f * VFXCore.Hash01(Projectile.whoAmI, 733, 149);

        /// <summary>El tipo de ESTA daga (ai[0], 0..4).</summary>
        private int Tipo => Math.Max(0, Math.Min(TiposMax - 1, (int)Projectile.ai[0]));

        // ================================================================
        //  LA IA — EL BUCLE DEL STABBER: anillo → anticipación → técnica
        //  → (clavada) → vuelta → anillo
        // ================================================================
        public override void AI()
        {
            Player dueño = Main.player[Projectile.owner];
            if (dueño == null || !dueño.active)
            {
                Projectile.Kill();
                return;
            }

            // EL SOSTÉN (la ley del escuadrón: el reloj nunca expira, el
            // icono vive, la única despedida es el clic derecho del buff).
            int tipoBuff = ModContent.BuffType<global::AethonMod.Content.Buffs.DagaEternaBuff>();
            global::AethonMod.Content.Systems.EscuadronUtil.Sostener(Projectile, dueño, tipoBuff);

            // === EL ALMA DE CADA CUCHILLO (el switch de KnifeType
            //     al abrir la IA — la ley de la casa): el color del filo, el
            //     extraUpdates (el cristal 1, la roca 2 — MÁS ticks de
            //     física por frame: la rapidéz del original) y el daño
            //     por tipo. El VACÍO y el VEREDICTO (los dos de la casa)
            //     cabalgan la máquina del stabber con SU firma: el vacío
            //     extraUpdates 1 con la QUEMADURA 100 % (la casa arde
            //     violeta), el veredicto ×1,5 de la sentencia.
            switch (Tipo)
            {
                case TIPO_CRISTAL:
                    damageMult = 0.5f;
                    Projectile.ArmorPenetration = 20;   // «innate 20 armor penetration»
                    Projectile.extraUpdates = 1;
                    break;
                case TIPO_LLAMA:
                    damageMult = 1f;
                    break;
                case TIPO_ROCA:
                    damageMult = 3f;                    // «300% base damage»
                    Projectile.extraUpdates = 2;
                    break;
                case TIPO_VACIO:
                    damageMult = 1f;
                    Projectile.extraUpdates = 1;
                    break;
                default:
                    damageMult = 1.5f;                  // la sentencia de la casa
                    break;
            }

            // LA LUZ DE LA CASA (el minion alumbra).
            if (!Main.dedServ)
            {
                Color luz = LuzDe(Tipo);
                Lighting.AddLight(Projectile.Center, luz.ToVector3() * 0.55f);
            }

            // EL PEGUE DEL FESTÍN (v6.131, intacto: la corona entera
            // hereda el movimiento del cuerpo COMPLETO mientras el
            // Eterno arrastra al dueño).
            global::AethonMod.Content.Projectiles.Definitivas.DevoracionEterna
                .PegarAlFestin(Projectile, Projectile.owner);

            // EL FOCO (el buscador de siempre de la casa): la presa más cercana al dueño.
            NPC potentialTarget = ObjetivoDaga(dueño);

            // EL OBJETIVO SE FIJA AL ABRIR LA RONDA (storedPos
            // solo cuando cooldown==0 y AttackDelay<=0 — el cuchillo no
            // re-apunta cada tick: persigue el punto donde ESTABA).
            // (AttackDelay vive en localAI[0] — NO en ai[0],
            // PERO aquí ai[0] ES EL TIPO del alma desde la v6.124 — el
            // reloj del empuje es CINE LOCAL: el daño lo cobra el
            // contacto del filo, no el reloj.)
            if (potentialTarget != null && attackCooldown == 0 && AttackDelay <= 0f)
            {
                storedPos = potentialTarget.Center;
            }
            // LA PRESA DE LA MIRADA (el iris la sigue viva).
            _presa = potentialTarget != null && potentialTarget.active ? potentialTarget.whoAmI : -1;

            // LOS EFECTOS DEL GOLPE (justHit — el flash y la firma de sonido de cada alma).
            if (justHit)
            {
                if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                {
                    if (Tipo == TIPO_CRISTAL && attacksDone >= 6)
                    {
                        for (int k = 0; k < 9; k++)
                        {
                            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink,
                                Vector2.One.RotatedByRandom(0.5) * Main.rand.NextFloat(10f, 20f));
                            dust.noGravity = true;
                            dust.scale = Main.rand.NextFloat(1.25f, 1.75f);
                            dust.color = LuzDe(Tipo);
                        }
                    }
                    if (Tipo == TIPO_ROCA)
                    {
                        for (int k = 0; k < 5; k++)
                        {
                            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Stone,
                                Vector2.One.RotatedByRandom(100) * Main.rand.NextFloat(1f, 5f));
                            dust.noGravity = true;
                            dust.scale = Main.rand.NextFloat(0.75f, 1.2f);
                        }
                    }
                    if (Tipo == TIPO_VEREDICTO)
                    {
                        for (int k = 0; k < 12; k++)
                        {
                            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                                Main.rand.NextVector2Circular(3.2f, 3.2f), 160, new Color(255, 214, 130), 1.0f);
                            dust.noGravity = true;
                        }
                    }
                    if (Tipo == TIPO_VACIO)
                    {
                        for (int k = 0; k < 6; k++)
                        {
                            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink,
                                Main.rand.NextVector2Circular(2.4f, 2.4f), 150, new Color(150, 84, 235), 0.9f);
                            dust.noGravity = true;
                        }
                    }
                }
                justHit = false;
            }

            // LA MÁQUINA (el if grande — objetivo presente:
            // SLICE o descanso; sin objetivo: la vuelta al descanso).
            if (potentialTarget != null)
            {
                if (attackCooldown == 0)
                    SliceTarget();
                else
                    ManageAttackCooldown();
            }
            else
            {
                ManageAttackCooldown();
                ReturnToRestingPosition(dueño);
            }
        }

        // ================================================================
        //  EL SLICE — EL ATAQUE DEL STABBER (la máquina de la casa:
        //  la aceleración 1,03/0,93, el giro 0,15 y la carga del
        //  empuje — adaptada a las CINCO almas: el cristal orbita y
        //  repite · la roca embiste recta y recula · la llama, el
        //  vacío y el veredicto persiguen curvando y CARGAN a 36 px/t)
        // ================================================================
        private void SliceTarget()
        {
            moveSmoothing = 50f;

            // EL RETARDO DEL EMPUJE (AttackDelay — cuenta hacia
            // abajo hasta −60; mientras >0 el cuchillo SOLO gira la
            // órbita del cristal).
            if (AttackDelay > -60f)
            {
                AttackDelay -= 1f;
                if (AttackDelay > 0f)
                {
                    if (Tipo == TIPO_CRISTAL)
                    {
                        if (!Projectile.WithinRange(storedPos, 150f))
                            Projectile.velocity = Projectile.velocity.RotatedBy(0.1);
                        else
                            Projectile.velocity *= 1.004f;
                    }
                    return;
                }
            }

            // EL DESPEGUE (sin movimiento → −12 en Y: el
            // cuchillo JAMÁS se queda clavado esperando).
            if (Projectile.velocity.Length() < 3f)
                Projectile.velocity = Vector2.UnitY * -12f;

            // CERCA: FRENA en seco (0,93 — el mordisco del corte).
            if (Projectile.velocity.Length() > 5f && Projectile.WithinRange(storedPos, 50f))
                Projectile.velocity *= 0.93f;
            // LEJOS: ACELERA (1,03 — el zumbido del vuelo).
            else if (Projectile.velocity.Length() < (Tipo == TIPO_ROCA ? 15f : 40f))
                Projectile.velocity *= 1.03f;

            float angularTurnSpeed = 0.15f;                       // el giro del original
            float angleToTargetCoords = Projectile.AngleTo(storedPos);

            // BLADE-3 (la llama, el vacío y el veredicto): PERSIGUE
            // curvando hacia el punto (la curva del barrido amplio).
            if (Tipo == TIPO_LLAMA || Tipo == TIPO_VACIO || Tipo == TIPO_VEREDICTO)
            {
                if (!Projectile.WithinRange(storedPos, 50f))
                    Projectile.velocity = Projectile.velocity.ToRotation()
                        .AngleTowards(angleToTargetCoords, angularTurnSpeed)
                        .ToRotationVector2() * Projectile.velocity.Length();
            }
            // BLADE-2 (la roca): la EMBESTIDA RECTA a 15 px/t.
            if (Tipo == TIPO_ROCA)
                Projectile.velocity = DireccionSegura(storedPos) * 15f;
            // BLADE-1 (el cristal): si se alejó MUCHO, vuelve directo.
            if (Tipo == TIPO_CRISTAL && !Projectile.WithinRange(storedPos, 400f))
                Projectile.velocity = DireccionSegura(storedPos) * 20f;

            // LA CARGA DEL EMPUJE (blade-3): a la vista del objetivo y
            // lejos (>75) — ZUMBIDO: 36 px/t rectos + AttackDelay 15
            // (el empuje que lee el ojo: el cuchillo TRUENA).
            if (!Projectile.WithinRange(storedPos, 75f) &&
                Vector2.Dot(DireccionSegura(storedPos), Projectile.velocity.SafeNormalize(Vector2.Zero)) > 0.99f &&
                (Tipo == TIPO_LLAMA || Tipo == TIPO_VACIO || Tipo == TIPO_VEREDICTO))
            {
                Projectile.velocity = DireccionSegura(storedPos) * 36f;
                AttackDelay = 15f;
                Projectile.netUpdate = true;
            }

            // LA ROTACIÓN: el filo APUNTA al vuelo (la punta hacia
            // donde va — la ley del sprite que apunta arriba).
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        /// <summary>DireccionSegura — la dirección hacia el punto con
        /// respaldo: JAMÁS NaN.</summary>
        private Vector2 DireccionSegura(Vector2 destino, Vector2 respaldo = default)
        {
            Vector2 v = destino - Projectile.Center;
            return v.LengthSquared() > 0.0001f ? Vector2.Normalize(v) : respaldo;
        }

        // ================================================================
        //  EL DESCANSO — manageAttackCooldown: el cristal vuelve a su
        //  puesto mientras cuenta; la roca desliza frenando (0,96); al
        //  terminar, la ronda renace (attacksDone=0 y el pulso del
        //  anillo del cristal).
        // ================================================================
        private void ManageAttackCooldown()
        {
            if (attackCooldown <= 0) return;
            attackCooldown--;
            if (Tipo == TIPO_CRISTAL)
                ReturnToRestingPosition(Main.player[Projectile.owner]);
            else if (Tipo == TIPO_ROCA)
                Projectile.velocity *= 0.96f;
            if (attackCooldown == 0)
            {
                attacksDone = 0;
                if (Tipo == TIPO_CRISTAL && !Main.dedServ && Main.netMode != NetmodeID.Server)
                {
                    // el BloomRing de la pausa — el pulso de anillo
                    // violeta del cristal que descansa.
                    for (int k = 0; k < 6; k++)
                    {
                        Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink,
                            Vector2.One.RotatedByRandom(100) * Main.rand.NextFloat(4f, 8f));
                        dust.noGravity = true;
                        dust.scale = Main.rand.NextFloat(0.65f, 1.15f);
                        dust.color = LuzDe(Tipo);
                    }
                }
            }
        }

        // ================================================================
        //  LA VUELTA AL DESCANSO — ReturnToRestingPosition: el abanico
        //  orbital SOBRE LA CORONA CENTRADA (el arco de la
        //  tiara v6.131, quieta sobre la cabeza del enjambre) con el
        //  seno del original (la respiración del vuelo) y el giro del
        //  sprite hacia el descanso (0,25 — la rotación que aterriza).
        // ================================================================
        private void ReturnToRestingPosition(Player dueño)
        {
            if (dueño == null || !dueño.active) return;
            if (moveSmoothing > 2f)
                moveSmoothing--;
            // v6.132 — LOS PICOS AL JUGADOR (la letra: «el pico de las
            // cuchillas deben apuntar al jugador»): al descansar, el
            // filo mira HACIA el centro (el jugador) — la corona de
            // puntas de verdad: el sprite apunta arriba → rotación =
            // dirección al centro + 90°.
            Vector2 centroCorona = CentroDeLaCorona(dueño);
            Vector2 radial = Projectile.Center - centroCorona;
            float rotDescanso = radial.LengthSquared() > 0.01f
                ? MathF.Atan2(radial.Y, radial.X) + MathHelper.PiOver2
                : -MathHelper.PiOver2;
            Projectile.rotation = Projectile.rotation.AngleTowards(rotDescanso, 0.25f);

            // EL PUESTO: la TIARA (SlotDelAnillo — el arco quieto sobre
            // la corona de ojos, la respiración ±3,5 % de la danza).
            Vector2 destino = SlotDelAnillo(dueño);

            // EL SENO DEL VUELO: el vaivén vertical vivo
            // del abanico — la corona RESPIRA.
            float sine = Math.Abs((float)Math.Sin(
                (Main.GlobalTimeWrappedHourly + RestOffsetAngle * 1.8f) * 8f / MathHelper.Pi));
            destino += new Vector2(0f, -6f * (MathHelper.Lerp(sine, 0.8f, 0.8f) - 0.8f));

            // EL ACECAMIENTO (velocidad = distancia /
            // moveSmoothing — 50 al arrancar: suave; 2 al final:
            // directo. La corona ATRAPA al cuchillo que vuelve).
            Projectile.velocity = (destino - Projectile.Center) / moveSmoothing;
        }

        /// <summary>EL CENTRO DE LA CORONA (el jugador — o el LIBRO
        /// mientras el espíritu caza: la ley v6.51.7 de los ojos).</summary>
        private Vector2 CentroDeLaCorona(Player dueño)
        {
            Vector2 centro = dueño.MountedCenter;
            try
            {
                Vector2? libro = PosicionDelLibro(dueño);
                if (libro is Vector2 lb) centro = lb;
            }
            catch { }
            return centro;
        }

        /// <summary>EL RESTO DEL TIEMPO (sin presa): el cuchillo vive en
        /// SU puesto de la tiara — la corona SIEMPRE centrada (la letra:
        // «deben estar como una corona centrada»).</summary>
        private void VivirEnLaCorona(Player dueño)
        {
            ReturnToRestingPosition(dueño);
        }

        // ================================================================
        //  LOS GOLPES — la letra del daño por tipo (la técnica del
        //  stabber: 50 %/20 pen el cristal · 100 % la llama · 300 % la
        //  roca) + LA QUEMADURA DEL VACÍO al 100 % por 3 s en TODAS
        // ================================================================
        // v6.132 — EL GOLPE DEL STABBER (damageMult — el
        // multiplicador vive en la IA y AQUÍ se aplica; la perforación
        // del cristal viaja en Projectile.ArmorPenetration como el
        // original).
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= damageMult;
            if (Tipo == TIPO_ROCA)
                modifiers.Knockback += 2f;               // el peso de la piedra
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // v6.132 — EL COMPÁS DEL STABBER (el OnHit por tipo):
            // el CRISTAL empuja (AttackDelay 15, cuenta la
            // racha y descansa 80-95 a las SEIS) · la ROCA RECUA del
            // golpe (rebote 15 px/t desviado + descanso 45-60) · la
            // LLAMA/VACÍO el latido corto (5) · el VEREDICTO la posa
            // breve (10 — la sentencia respira). Y LA QUEMADURA DEL
            // VACÍO prende en TODO golpe (nuestra Holy Flames: el
            // stabber arde violeta al 100 % por 3 s).
            justHit = true;
            switch (Tipo)
            {
                case TIPO_CRISTAL:
                    AttackDelay = 15f;
                    attacksDone++;
                    if (attacksDone >= 6)
                        attackCooldown = Main.rand.Next(80, 96);
                    break;
                case TIPO_ROCA:
                    Projectile.velocity = (-Projectile.velocity.SafeNormalize(Vector2.UnitX) * 15f).RotatedByRandom(0.7);
                    attackCooldown = Main.rand.Next(45, 61);
                    break;
                case TIPO_LLAMA:
                case TIPO_VACIO:
                    if (attackCooldown == 0)
                        attackCooldown = 5;
                    break;
                default:
                    if (attackCooldown == 0)
                        attackCooldown = 10;
                    break;
            }
            try
            {
                int quemadura = ModContent.BuffType
                    <global::AethonMod.Content.Buffs.QuemaduraVacioExterior>();
                target.AddBuff(quemadura, 180);
            }
            catch { }
            Projectile.netUpdate = true;
        }

        // ================================================================
        //  LA FORMACIÓN — v6.132 LA CORONA CENTRADA (la letra: «ademas
        //  deben estar como una corona centrada» + «el pico de las
        //  cuchillas deben apuntar al jugador»): la tiara en ARCO de la
        //  v6.131 (solo ARRIBA) pasa a ser LA CORONA CIRCULAR COMPLETA
        //  — el CÍRCULO ENTERO alrededor de la cabeza del enjambre,
        //  las CINCO repartidas a 72° (la corona de puntas de verdad:
        //  la de arriba corona, las de los lados ciñen las sienes, las
        //  de abajo escoltan la nuca), con la RESPIRACIÓN ÚNICA de la
        //  danza (el latido ±3,5 % de los ojos — el enjambre respira
        //  JUNTO) y los PICOS APUNTANDO AL JUGADOR (ReturnToRestingPosition
        //  gira cada filo hacia el centro: puntas adentro, la corona
        //  ciñe).
        // ================================================================

        // EL RADIO DE LA CORONA — LA LEY DEL DESPEJE (medida por el sim
        // sim_corona_v6131.py): la corona de ojos vive a max(40, 6,2·n)
        // y su borde exterior a +11 (el sprite del ojo); la cuchilla
        // ×1,4 tiene 23,8 px de semialto ANCLADOS AL CENTRO → el radio
        // de la corona es el borde de los ojos + 41 (semialto + 6 de
        // aire): a 11 ojos, ~109 px — la corona CIÑE la cabeza del
        // enjambre SIN pisar a un solo ojo, a CUALQUIER número de estos.
        private static float RadioTiara(int nOjos) =>
            MathF.Max(40f, 6.2f * nOjos) + 41f;

        // LA ESCALA DE LA CORONA (la letra: «las cuchillas son muy
        // pequeñas, que sean un poco mas grande»): la hoja de 34×34
        // viaja ×1,4 — el filo se LEE (47×47 en pantalla), el ojo del
        // taladro acompaña (OJO_ESC×1,4).
        private const float ESCALA_TIARA = 1.40f;

        private Vector2 SlotDelAnillo(Player dueño)
        {
            // EL CENTRO: el jugador — o el LIBRO mientras el espíritu
            // caza (la ley v6.51.7 de los ojos: los ojos son la corona
            // del libro, las cuchillas SU CORONA de puntas).
            Vector2 centro = CentroDeLaCorona(dueño);

            // LA CORONA CIRCULAR COMPLETA (v6.132): las CINCO repartidas
            // a 72° exactos por RestOffsetAngle (el ángulo del abanico
            // de descanso que el cetro siembra al nacer: k/5·2π − π/2 —
            // el CÍRCULO ENTERO, nadie apilado, la corona CENTRADA de
            // la letra), QUIETA (una corona se lleva puesta: no gira),
            // con la RESPIRACIÓN ÚNICA de la danza (el latido ±3,5 % de
            // los ojos — el enjambre respira JUNTO) y el cabeceo mínimo.
            float time = Main.GlobalTimeWrappedHourly;
            int idx = Math.Max(0, (int)Projectile.minionPos);
            int tipoOjo = ModContent.ProjectileType<OjoEternoMinion>();
            int nOjos = Math.Max(0, dueño.ownedProjectileCounts[tipoOjo]);
            float ang = RestOffsetAngle;
            float radio = RadioTiara(nOjos) * (1f + 0.035f * MathF.Sin(time * 0.5f));
            Vector2 slot = centro + ang.ToRotationVector2() * radio;
            slot.Y += MathF.Sin(time * 0.9f + idx * 1.9f) * 1.5f;   // el cabeceo de siempre
            return slot;
        }

        private float AnguloHacia(Vector2 destino)
        {
            Vector2 d = destino - Projectile.Center;
            if (d.LengthSquared() < 0.0001f) return -MathHelper.PiOver2;
            return d.ToRotation() + MathHelper.PiOver2;
        }

        /// <summary>LA POSICIÓN DEL LIBRO cuando el espíritu caza (el
        /// caché de los ojos, reutilizado vivo: una sola copia por
        /// tick para TODO el escuadrón).</summary>
        internal static Vector2? PosicionDelLibro(Player dueño) =>
            OjoEternoMinion.PosicionDelLibro(dueño);

        /// <summary>EL CAZA (el foco de los ojos): la presa más cercana
        /// al DUEÑO dentro del alcance del stabber.</summary>
        private NPC ObjetivoDaga(Player dueño)
        {
            NPC mejor = null;
            float mejorD = 640f * 640f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float d = (n.Center - dueño.MountedCenter).LengthSquared();
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        // ================================================================
        //  EL DIBUJO — LA CUCHILLA + EL OJO MINION ANIMADO (v6.130: la
        //  letra: «recuerda usar y animar los ojos usando los ojos
        //  minions» — el MISMO sistema del OjoEternoMinion: las tres
        //  texturas por capas Abierto/Medio/Cerrado para el PARPADEO
        //  independiente + el iris GrimorioHambriento_Iris dibujado en
        //  runtime que MIRA a la presa, TODO escalado a la cuenca de la
        //  cuchilla: el agujero ~7×5 del sprite recibe el ojo minion
        //  ~8×5 — la MISMA cara del escuadrón, en versión pocket)
        // ================================================================

        // EL OJO EN LA CUCHILLA (las constantes del trasplante: la
        // ALMENDRA del ojo minion recortada 27×15 — la esclerótica
        // blanca PURA que a 9 px SÍ se lee como el ojo del escuadrón;
        // el ojo entero 56×54 era TODO párpado marrón a esta escala)
        private const float OJO_ESC = 0.35f;    // 27×15 → ~9,5×5,3 px: el taladro lleno
        private const float IRIS_ESC = 0.115f;  // el iris 32×32 → ~3,7 px (sobre el blanco)
        private const float MIRA_X = 1.6f;       // el viaje del iris: media cuenca
        private const float MIRA_Y = 0.8f;

        /// <summary>EL TRONO DEL TALADRO por tipo (v6.130: la SForja
        /// taladró la hoja DONDE tenía masa — la curva y el eje MUEVEN
        /// el centro): el offset del ojo desde el centro del marco,
        /// medido píxel a píxel del sprite (flood-fill del taladro
        /// real de cada marco de la tira).</summary>
        private static readonly Vector2[] TronoDelOjo =
        {
            new(-4.5f, 0.5f),    // cristal (la curva luna lo baja a la izquierda)
            new(0.5f, -0.5f),    // llama (el serpenteo lo deja casi al centro)
            new(-5.5f, -1.5f),   // roca (el cincel ancho, arriba-izquierda)
            new(-2.5f, -1.5f),   // vacío (la puerta lo pone a la izquierda)
            new(-0.5f, 1.5f),    // veredicto (la balanza lo baja al pie)
        };

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                Texture2D tex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/DagaEterna_Hoja", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                int frameH = tex.Height / TiposMax;
                Rectangle rect = new Rectangle(0, Tipo * frameH, tex.Width, frameH);
                Vector2 pos = Projectile.Center - Main.screenPosition;
                Vector2 orig = new Vector2(tex.Width * 0.5f, frameH * 0.5f);
                float rot = Projectile.rotation;
                // v6.131 — LA CORONA GRANDE: la hoja ×1,4 (ESCALA_TIARA
                // vive en Projectile.scale — el golpe y el dibujado
                // crecen JUNTOS con la hoja).
                float esc = Projectile.scale;

                // LA ESTELA DEL GOLPE (el rastro de la casa): tres
                // fantasmas ATRÁS mientras VUELA al ataque — v6.132: la
                // máquina del stabber no tiene fases; la estela vive
                // cuando el cuchillo NO descansa (la ronda abierta).
                if (attackCooldown == 0 && Projectile.velocity.LengthSquared() > 25f)
                {
                    for (int j = 1; j <= 3; j++)
                    {
                        float a = (0.30f / j);
                        Main.spriteBatch.Draw(tex, pos - new Vector2(0f, j * 7f).RotatedBy(rot - MathHelper.PiOver2),
                            rect, LuzDe(Tipo) * a, rot, orig, esc - j * 0.17f, SpriteEffects.None, 0f);
                    }
                }

                // LA CUCHILLA (el marco de SU tipo del spritesheet — la
                // hoja SIN mango con el agujero taladrado al centro)
                Main.spriteBatch.Draw(tex, pos, rect, Color.White, rot, orig, esc, SpriteEffects.None, 0f);

                // ============== EL OJO DEL GRIMORIO (v6.130) ==============
                // LA LETRA: «en esa zona va el ojo del grimorio, ya
                // tienes el sprite son los mismos ojos que se usan para
                // los minions, pero más pequeños y adaptados a las
                // dagas» — EL TRASPLANTE del OjoEternoMinion: LA
                // ALMENDRA recortada del ojo del escuadrón (la
                // esclerótica blanca 27×15 — a esta escala el ojo
                // entero 56×54 era todo párpado marrón y NO se leía),
                // el párpado parpadea por capas (Medio/Cerrado/Medio —
                // localAI[1] 10→1, el reloj de la IA) y el IRIS VIVO
                // GrimorioHambriento_Iris MIRA a la presa (la firma
                // del escuadrón, en versión pocket).
                float fase = Projectile.localAI[1];
                bool abierto = fase <= 0f;
                bool medio = fase > 8f || fase < 3f;      // 10-9 y 2-1: el medio párpado (el QA: el iris ASOMA en 8-3)
                Texture2D ojoTex = ModContent.Request<Texture2D>(
                    abierto ? "AethonMod/Content/Projectiles/CuchillaOjo_Abierto"
                            : (medio ? "AethonMod/Content/Projectiles/CuchillaOjo_Medio"
                                     : "AethonMod/Content/Projectiles/CuchillaOjo_Cerrado"),
                    ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

                // EL CENTRO DEL TALADRO (v6.130: EL TRONO POR TIPO — la
                // SForja taladró cada hoja donde tenía masa; el offset
                // medido píxel a píxel del sprite, girando CON la hoja)
                Vector2 trono = TronoDelOjo[Tipo] * esc;
                Vector2 centroOjo = pos + trono.RotatedBy(rot);

                // EL OJO MINION (la esclerótica + el párpado que
                // respira): EL COLOR ES EL DEL OJO REAL DE LA CASA —
                // Lighting.GetColor (blanco en luz plena; la esclerótica
                // ES blanca, el QA del sim lo exigió: el tinte del alma
                // la volvía oscura y el ojo NO se leía como el de los
                // minions). Con el ojo CERRADO el brillo baja (duerme).
                Color colorOjo = abierto || medio
                    ? Lighting.GetColor(Projectile.Center.ToTileCoordinates())
                    : Lighting.GetColor(Projectile.Center.ToTileCoordinates()) * 0.55f;
                Main.spriteBatch.Draw(ojoTex, centroOjo, null, colorOjo, rot,
                    ojoTex.Size() * 0.5f, OJO_ESC * esc, SpriteEffects.None, 0f);

                // EL IRIS VIVO (solo con el ojo ABIERTO — la ley del
                // OjoEternoMinion: el párpado tapa la mirada): viaja
                // hacia la presa (el iris que manda, la firma de los
                // Ojos del Eterno — MIRA a lo que va a cortar).
                if (abierto)
                {
                    NPC presa = _presa >= 0 && _presa < Main.maxNPCs ? Main.npc[_presa] : null;
                    Vector2 mira = Vector2.Zero;
                    if (presa != null && presa.active)
                    {
                        Vector2 d = Vector2.Normalize(presa.Center - Projectile.Center);
                        if (d.LengthSquared() > 0.0001f) mira = d * 1.0f;
                    }
                    // la mirada en el MARCO DE LA HOJA (rot inversa: el
                    // iris se desplaza hacia la presa EN EL MUNDO, no en
                    // el marco girado — la geometría del trasplante)
                    Vector2 miraHoja = mira.RotatedBy(-rot);
                    Vector2 irisPos = centroOjo + new Vector2(miraHoja.X * MIRA_X * esc, miraHoja.Y * MIRA_Y * esc).RotatedBy(rot);
                    Texture2D iris = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Weapons/GrimorioHambriento_Iris",
                        ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                    // EL IRIS LLEVA EL ALMA (el oro de la casa sobre el
                    // blanco de la esclerótica — el QA del sim: el iris
                    // OSCURO no se leía; el del grimorio ES dorado vivo)
                    Color colorIris = IrisDe(Tipo);
                    Main.spriteBatch.Draw(iris, irisPos, null, colorIris, rot,
                        iris.Size() * 0.5f, IRIS_ESC * esc, SpriteEffects.None, 0f);
                }
                return false;
            }
            catch { return false; }
        }

        /// <summary>LA LUZ de cada daga (la casa alumbra).</summary>
        private static Color LuzDe(int tipo) => tipo switch
        {
            TIPO_CRISTAL => new Color(216, 190, 255),
            TIPO_LLAMA => new Color(150, 84, 235),
            TIPO_ROCA => new Color(96, 54, 170),
            TIPO_VACIO => new Color(122, 78, 188),
            _ => new Color(255, 214, 130),
        };

        /// <summary>EL IRIS de cada daga (el ojo en el centro): el
        /// cristal lila · la llama violeta · la roca amatista · el vacío
        /// ORO (la mirada del Eterno en la hoja negra) · el veredicto
        /// blanco-cálido.</summary>
        private static Color IrisDe(int tipo) => tipo switch
        {
            TIPO_CRISTAL => new Color(224, 200, 255),
            TIPO_LLAMA => new Color(178, 120, 255),
            TIPO_ROCA => new Color(150, 84, 235),
            TIPO_VACIO => new Color(255, 214, 130),
            _ => new Color(255, 244, 214),
        };

        // === EL ESTADO PRIVADO (visual/movimiento — la posición la
        //     sincroniza el owner como todo minion de vanilla) ===
        private int _presa = -1;

        /// <summary>v6.132 — LA RED DEL COMPÁS (SendExtraAI —
        /// el descanso y la racha viajan con el minion: los otros
        /// clientes ven la MISMA cadencia, no cuchillas fantasma
        /// cortando en vacío).</summary>
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(attackCooldown);
            writer.Write(attacksDone);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            attackCooldown = reader.ReadInt32();
            attacksDone = reader.ReadInt32();
        }
    }
}
