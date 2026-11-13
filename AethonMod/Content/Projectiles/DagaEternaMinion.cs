using System;
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
    /// «toma lo que aprendiste de Dazzling Stabber Staff» — el invocador
    /// de dagas de Providence del Calamity, investigado en la wiki real):
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

        // === LAS FASES (ai[1]) ===
        private const float F_HOVER = 0f;     // el anillo: espera y mira
        private const float F_VIENTO = 1f;    // la anticipación: se echa atrás
        private const float F_GOLPE = 2f;     // la técnica de SU tipo
        private const float F_CLAVADA = 3f;   // (solo Vacío) clavada goteando
        private const float F_VUELTA = 4f;    // el regreso al anillo

        // === EL COMPÁS (la cadencia en el anillo, por tipo — el ritmo
        //     propio del stabber: cada daga ataca a SU velocidad) ===
        private static int CadenciaDe(int tipo) => tipo switch
        {
            TIPO_CRISTAL => 130,
            TIPO_LLAMA => 170,
            TIPO_ROCA => 220,
            TIPO_VACIO => 240,
            _ => 160,
        };

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
            Projectile.width = 20;
            Projectile.height = 20;
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

            // EL FOCO: la presa más cercana al dueño (el caza de los ojos).
            NPC presa = ObjetivoDaga(dueño);

            float fase = Projectile.ai[1];
            // Projectile.frame ES el reloj de fase (el truco de los ojos:
            // campo sincronizado, cero netcode).
            int t = Projectile.frame;

            if (fase == F_HOVER)
            {
                // === EL ANILLO ÍNTIMO: la guardia del portador ===
                Vector2 slot = SlotDelAnillo(dueño);
                float suaviza = Math.Min(1f, t / 22f);   // el nacimiento: se acomoda suave
                Projectile.Center = Vector2.Lerp(Projectile.Center, slot, 0.18f + 0.10f * suaviza);
                Projectile.velocity = Vector2.Zero;

                // el reloj del compás: cuenta ABAJO hasta el ataque
                if (t <= 0)
                {
                    // EL DESFASE DE NACIMIENTO (la ley de los ojos): el
                    // primer ataque cae entre el 35 % y el 100 % del
                    // intervalo propio — las cinco JAMÁS se lanzan en bloque.
                    int cadencia = Math.Max(30, (int)(CadenciaDe(Tipo) / (VelocidadNivel * RitmoPropio)));
                    Projectile.frame = (int)(cadencia *
                        (0.35f + 0.65f * VFXCore.Hash01(Projectile.whoAmI, 811, 233)));
                }
                else if (t <= 1)
                {
                    if (presa != null)
                    {
                        Projectile.ai[1] = F_VIENTO;
                        Projectile.frame = 0;
                        Projectile.netUpdate = true;
                        // LA MIRADA COLECTIVA: el iris se clava en la presa
                        if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                        {
                            Dust d = Dust.NewDustPerfect(Projectile.Center,
                                DustID.Enchanted_Pink, -Vector2.Normalize(presa.Center - Projectile.Center) * 1.6f,
                                130, new Color(200, 160, 255), 0.7f);
                            d.noGravity = true;
                        }
                    }
                    else
                    {
                        // sin presa: el compás respira corto (vuelve a mirar)
                        Projectile.frame = 50;
                    }
                }
                else
                {
                    Projectile.frame = t - 1;
                }
            }
            else if (fase == F_VIENTO)
            {
                // === LA ANTICIPACIÓN (el telegráfo del stabber): la daga
                //     se ECHA ATRÁS apuntando a la presa — el resorte ===
                int viento = Tipo == TIPO_ROCA ? 16 : 12;
                float p = Math.Min(1f, t / (float)viento);
                if (presa != null && presa.active)
                {
                    _presa = presa.whoAmI;
                    Vector2 dir = Vector2.Normalize(presa.Center - Projectile.Center);
                    if (dir.LengthSquared() < 0.0001f) dir = -Vector2.UnitY;
                    Projectile.Center -= dir * (2.6f * (1f - p * 0.4f));
                    Projectile.rotation = dir.ToRotation() + MathHelper.PiOver2;
                    // LA MARCA DEL VEREDICTO: el anillo dorado sobre la presa
                    if (Tipo == TIPO_VEREDICTO && !Main.dedServ && Main.rand.NextBool(3))
                    {
                        Dust d = Dust.NewDustPerfect(presa.Center + Main.rand.NextVector2Circular(22f, 22f),
                            DustID.GoldFlame, Vector2.Zero, 160, new Color(255, 214, 130), 0.8f);
                        d.noGravity = true;
                    }
                }
                if (t >= viento)
                {
                    Projectile.ai[1] = F_GOLPE;
                    Projectile.frame = 0;
                    Projectile.netUpdate = true;
                }
                else Projectile.frame = t + 1;
            }
            else if (fase == F_GOLPE)
            {
                CorrerTecnica(presa, t);
            }
            else if (fase == F_CLAVADA)
            {
                // === (VACÍO) LA DAGA VIVE DENTRO DE LA PRESA: cabalga el
                //     cuerpo donde entró y GOTEA el vacío — cada 15 t un
                //     golpe de ×0,35 (el contacto lo cobra el hitbox
                //     acoplado) — hasta disolverse en motas violetas ===
                NPC clavada = _presa >= 0 && _presa < Main.maxNPCs ? Main.npc[_presa] : null;
                if (clavada == null || !clavada.active || clavada.life <= 0)
                {
                    // la presa murió: la daga sigue su vuelo (busca la
                    // siguiente tela — la ley de Las Agujas del Vacío)
                    Projectile.ai[1] = F_VUELTA;
                    Projectile.frame = 0;
                    Projectile.netUpdate = true;
                }
                else
                {
                    Projectile.Center = clavada.Center + _offsetClavada;
                    Projectile.rotation = _angClavada;
                    if (t >= 45)
                    {
                        // la disolución en motas violetas
                        if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                            for (int k = 0; k < 10; k++)
                            {
                                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink,
                                    Main.rand.NextVector2Circular(2.4f, 2.4f), 140,
                                    new Color(190, 150, 240), 0.9f);
                                d.noGravity = true;
                            }
                        Projectile.ai[1] = F_VUELTA;
                        Projectile.frame = 0;
                        Projectile.netUpdate = true;
                    }
                    else Projectile.frame = t + 1;
                }
            }
            else // F_VUELTA
            {
                // === EL REGRESO (el compás del stabber: golpe → pausa →
                //     anillo — el CRISTAL vuelve MÁS DESPACIO: su pausa
                //     breve de la letra) ===
                int vuelta = Tipo == TIPO_CRISTAL ? 46 : 24;
                Vector2 slot = SlotDelAnillo(dueño);
                float p = Math.Min(1f, t / (float)vuelta);
                float ease = p * p * (3f - 2f * p);
                Projectile.Center = Vector2.Lerp(_posVuelta, slot, ease);
                if (presa != null && presa.active)
                    Projectile.rotation = MathHelper.Lerp(Projectile.rotation,
                        AnguloHacia(presa.Center), 0.12f);
                if (t >= vuelta)
                {
                    Projectile.ai[1] = F_HOVER;
                    int cadencia = Math.Max(30, (int)(CadenciaDe(Tipo) / (VelocidadNivel * RitmoPropio)));
                    Projectile.frame = cadencia;
                    Projectile.netUpdate = true;
                }
                else Projectile.frame = t + 1;
            }

            // LA QUEMADURA PRENDE EN CADA GOLPE (ModifyHitNPC) — aquí
            // solo vive la LUZ de la daga (la casa: el minion alumbra).
            if (!Main.dedServ)
            {
                Color luz = LuzDe(Tipo);
                Lighting.AddLight(Projectile.Center, luz.ToVector3() * 0.55f);
            }
        }

        // ================================================================
        //  LAS TÉCNICAS — el repertorio del stabber, una por daga
        // ================================================================
        private void CorrerTecnica(NPC presa, int t)
        {
            switch (Tipo)
            {
                case TIPO_CRISTAL: TecnicaCristal(presa, t); break;
                case TIPO_LLAMA: TecnicaLlama(presa, t); break;
                case TIPO_ROCA: TecnicaRoca(presa, t); break;
                case TIPO_VACIO: TecnicaVacio(presa, t); break;
                default: TecnicaVeredicto(presa, t); break;
            }
        }

        /// <summary>CRISTAL — «rapidly slash enemies before briefly
        /// pausing, dealing 50% base damage with 20 innate armor
        /// penetration»: TRES tajos rápidos A TRAVÉS de la presa (12 t
        /// cada uno), cada uno naciendo del lado opuesto — la ráfaga
        /// EXACTA del original.</summary>
        private void TecnicaCristal(NPC presa, int t)
        {
            if (presa == null || !presa.active) { IrAVuelta(); return; }
            int tajo = t / 12;                     // 0..2: la ráfaga de tres
            float p = (t % 12) / 12f;
            if (tajo > 2) { IrAVuelta(); return; }
            // cada tajo nace 70 px DETRÁS de la presa (alternando el
            // lado — la ráfaga la cruza en zigzag) y la ATRAVIESA
            Vector2 dir = tajo % 2 == 0
                ? new Vector2(1f, -0.35f)
                : new Vector2(-1f, -0.35f);
            dir.Normalize();
            Vector2 desde = presa.Center - dir * 70f;
            Vector2 hasta = presa.Center + dir * 70f;
            float ease = p * p * (3f - 2f * p);
            Projectile.Center = Vector2.Lerp(desde, hasta, ease);
            Projectile.rotation = dir.ToRotation() + MathHelper.PiOver2;
            // el rastro del tajo (las esquirlas de cristal)
            if (!Main.dedServ && Main.netMode != NetmodeID.Server && p < 0.7f && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink,
                    Main.rand.NextVector2Circular(1.2f, 1.2f), 130, new Color(220, 190, 255), 0.7f);
                d.noGravity = true;
            }
        }

        /// <summary>LLAMA — «swipe at enemies in a wide arcing motion»:
        /// un BARRIDO en arco amplio alrededor de la presa (de −70° a
        /// +70° por encima, peinando la zona, ×1,0 a TODO lo que el
        /// arco toque).</summary>
        private void TecnicaLlama(NPC presa, int t)
        {
            if (presa == null || !presa.active) { IrAVuelta(); return; }
            const int T_BARRIDO = 30;
            if (t > T_BARRIDO) { IrAVuelta(); return; }
            float p = t / (float)T_BARRIDO;
            float a0 = -MathHelper.PiOver2 - 1.25f;
            float a1 = -MathHelper.PiOver2 + 1.25f;
            float ang = MathHelper.Lerp(a0, a1, p);
            float radio = 84f;
            Projectile.Center = presa.Center + ang.ToRotationVector2() * radio;
            Projectile.rotation = ang + MathHelper.Pi;   // la tangente del arco + el sprite que apunta arriba
            // las lenguas del barrido (la llama negra de la casa)
            if (!Main.dedServ && Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center,
                    Main.rand.NextBool(2) ? DustID.Shadowflame : DustID.Enchanted_Pink,
                    -ang.ToRotationVector2() * Main.rand.NextFloat(0.8f, 2.2f), 140,
                    Main.rand.NextBool(2) ? new Color(150, 84, 235) : new Color(96, 54, 170), 0.9f);
                d.noGravity = true;
            }
        }

        /// <summary>ROCA — «lunge at enemies and recoil after hits,
        /// dealing 300% base damage»: la EMBESTIDA desde el anillo
        /// (nacimos lejos: el lunge de verdad) que ATRAVIESA a la presa
        /// y el RECUDE — el rebote hacia atrás del golpe.</summary>
        private void TecnicaRoca(NPC presa, int t)
        {
            if (presa == null || !presa.active) { IrAVuelta(); return; }
            const int T_LUNGE = 16;
            const int T_RECUDE = 12;
            if (t < T_LUNGE)
            {
                // LA EMBESTIDA: de donde nací (guardado en _posVuelta)
                // hasta MÁS ALLÁ de la presa (la atraviesa entera)
                float p = t / (float)T_LUNGE;
                float ease = p * p * p;                 // acelera como una piedra
                Vector2 hasta = _posVuelta + (presa.Center - _posVuelta) * 1.65f;
                Projectile.Center = Vector2.Lerp(_posVuelta, hasta, ease);
                Projectile.rotation = AnguloHacia(presa.Center);
                if (!Main.dedServ && Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                        Main.rand.NextVector2Circular(1.6f, 1.6f), 130, new Color(96, 54, 170), 0.9f);
                    d.noGravity = true;
                }
            }
            else if (t < T_LUNGE + T_RECUDE)
            {
                // EL RECUDE: el rebote hacia atrás del golpe (el
                // retroceso EXACTO del original)
                float p = (t - T_LUNGE) / (float)T_RECUDE;
                Vector2 dir = Vector2.Normalize(presa.Center - _posVuelta);
                if (dir.LengthSquared() < 0.0001f) dir = -Vector2.UnitY;
                Projectile.Center += -dir * 4.2f * (1f - p);
            }
            else IrAVuelta();
        }

        /// <summary>VACÍO — la cuarta de la casa: el LANZAMIENTO que SE
        /// CLAVA (la daga vive DENTRO de la presa 45 t goteando ×0,35
        /// cada 15 t — F_CLAVADA cobra el goteo por contacto).</summary>
        private void TecnicaVacio(NPC presa, int t)
        {
            if (presa == null || !presa.active) { IrAVuelta(); return; }
            const int T_VUELO = 14;
            if (t > T_VUELO)
            {
                // LA ENTRADA: el punto del clavado (offset dentro del
                // cuerpo — la herida vive donde entró)
                _offsetClavada = Projectile.Center - presa.Center;
                _angClavada = Projectile.rotation;
                Projectile.ai[1] = F_CLAVADA;
                Projectile.frame = 0;
                Projectile.netUpdate = true;
                if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                {
                    for (int k = 0; k < 8; k++)
                    {
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink,
                            Main.rand.NextVector2Circular(2.2f, 2.2f), 150, new Color(150, 84, 235), 0.9f);
                        d.noGravity = true;
                    }
                }
                return;
            }
            float p = t / (float)T_VUELO;
            float ease = p * p;                        // acelera: la sed
            Vector2 hasta = presa.Center;
            Projectile.Center = Vector2.Lerp(_posVuelta, hasta, ease);
            Projectile.rotation = AnguloHacia(presa.Center);
        }

        /// <summary>VEREDICTO — la quinta de la casa: el golpe PUNTUAL
        /// ×1,5 sobre la presa marcada, con su estallido de oro (la
        /// sentencia pequeña — el lenguaje del Juicio).</summary>
        private void TecnicaVeredicto(NPC presa, int t)
        {
            if (presa == null || !presa.active) { IrAVuelta(); return; }
            const int T_GOLPE = 12;
            const int T_POSA = 14;
            if (t < T_GOLPE)
            {
                // LA SENTENCIA: una línea recta rápida, de punta
                float p = t / (float)T_GOLPE;
                float ease = 1f - (1f - p) * (1f - p) * (1f - p);
                Vector2 hasta = presa.Center;
                Projectile.Center = Vector2.Lerp(_posVuelta, hasta, Math.Min(1f, ease * 1.15f));
                Projectile.rotation = AnguloHacia(presa.Center);
            }
            else if (t < T_GOLPE + T_POSA)
            {
                // LA POSA: la daga respira un instante donde cayó la
                // sentencia (con su lluvia de oro)
                if (t == T_GOLPE && !Main.dedServ && Main.netMode != NetmodeID.Server)
                {
                    for (int k = 0; k < 12; k++)
                    {
                        Dust d = Dust.NewDustPerfect(presa.Center, DustID.GoldFlame,
                            Main.rand.NextVector2Circular(3.2f, 3.2f), 160, new Color(255, 214, 130), 1.0f);
                        d.noGravity = true;
                    }
                }
            }
            else IrAVuelta();
        }

        private void IrAVuelta()
        {
            _posVuelta = Projectile.Center;
            Projectile.ai[1] = F_VUELTA;
            Projectile.frame = 0;
            Projectile.netUpdate = true;
        }

        // ================================================================
        //  LOS GOLPES — la letra del daño por tipo (la técnica del
        //  stabber: 50 %/20 pen el cristal · 100 % la llama · 300 % la
        //  roca) + LA QUEMADURA DEL VACÍO al 100 % por 3 s en TODAS
        // ================================================================
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            switch (Tipo)
            {
                case TIPO_CRISTAL:
                    modifiers.SourceDamage *= 0.5f;      // «50% base damage»
                    modifiers.ArmorPenetration += 20f;   // «innate 20 armor penetration»
                    break;
                case TIPO_ROCA:
                    modifiers.SourceDamage *= 3f;        // «300% base damage»
                    modifiers.Knockback += 2f;           // el peso de la piedra
                    break;
                case TIPO_VEREDICTO:
                    modifiers.SourceDamage *= 1.5f;      // la sentencia
                    break;
                case TIPO_VACIO:
                    if (Projectile.ai[1] == F_CLAVADA)
                        modifiers.SourceDamage *= 0.35f; // el goteo del clavado
                    break;
                // LLAMA: ×1,0 — el barrido completo
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // LA QUEMADURA DEL VACÍO (nuestra Holy Flames: el stabber
            // prende Holy Flames al 100 % por 3 s — la casa arde violeta)
            try
            {
                int quemadura = ModContent.BuffType
                    <global::AethonMod.Content.Buffs.QuemaduraVacioExterior>();
                target.AddBuff(quemadura, 180);
            }
            catch { }
        }

        // ================================================================
        //  LA FORMACIÓN — EL ANILLO ÍNTIMO (la guardia del portador)
        // ================================================================
        private Vector2 SlotDelAnillo(Player dueño)
        {
            // EL CENTRO: el jugador — o el LIBRO mientras el espíritu
            // caza (la ley v6.51.7 de los ojos, en su círculo pequeño:
            // los ojos son la corona del libro, las dagas su guardia).
            Vector2 centro = dueño.MountedCenter;
            try
            {
                Vector2? libro = PosicionDelLibro(dueño);
                if (libro is Vector2 lb) centro = lb;
            }
            catch { }

            // EL ANILLO: radio 32 (íntimo — los ojos viven a 62+), giro
            // lento 0,45 rad/s, el puesto por minionPos (la cuenta
            // ordenada de vanilla), reloj GlobalTime (determinista).
            float time = Main.GlobalTimeWrappedHourly;
            int idx = Math.Max(0, (int)Projectile.minionPos);
            int n = Math.Max(1, DagasVivas(dueño));
            float ang = time * 0.45f + MathHelper.TwoPi * idx / n;
            return centro + ang.ToRotationVector2() * 32f +
                new Vector2(0f, MathF.Sin(time * 2.2f + idx * 1.9f) * 2.5f);
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
        //  EL DIBUJO — LA HOJA + EL IRIS VIVO (el ojo en el centro)
        // ================================================================
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
                float time = Main.GlobalTimeWrappedHourly;

                // LA ESTELA DEL GOLPE (el rastro de la casa): tres
                // fantasmas ATRÁS durante la técnica — la daga que vuela
                // deja su firma en el aire.
                if (Projectile.ai[1] == F_GOLPE)
                {
                    for (int j = 1; j <= 3; j++)
                    {
                        float a = (0.30f / j);
                        Main.spriteBatch.Draw(tex, pos - new Vector2(0f, j * 7f).RotatedBy(rot - MathHelper.PiOver2),
                            rect, LuzDe(Tipo) * a, rot, orig, 1f - j * 0.12f, SpriteEffects.None, 0f);
                    }
                }

                // LA HOJA (el marco de SU tipo del spritesheet)
                Main.spriteBatch.Draw(tex, pos, rect, Color.White, rot, orig, 1f, SpriteEffects.None, 0f);

                // EL IRIS VIVO (la letra: «también tendrán un ojo en el
                // centro»): la almendra de la hoja RECIBE su iris en
                // runtime — y MIRA: viaja hacia la presa (el iris que
                // manda, la firma de los Ojos del Eterno).
                NPC presa = _presa >= 0 && _presa < Main.maxNPCs ? Main.npc[_presa] : null;
                Vector2 mira = Vector2.Zero;
                if (presa != null && presa.active)
                {
                    Vector2 d = Vector2.Normalize(presa.Center - Projectile.Center);
                    if (d.LengthSquared() > 0.0001f) mira = d * 2.2f;
                }
                Vector2 irisPos = pos + mira.RotatedBy(rot - MathHelper.PiOver2) + new Vector2(0f, -1f).RotatedBy(rot);
                Color irisColor = IrisDe(Tipo);
                if (VFXCore.SoftGlow != null)
                {
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, irisPos, null,
                        irisColor * 0.95f, 0f, VFXCore.SoftGlow.Size() * 0.5f,
                        0.16f, SpriteEffects.None, 0f);
                    // la pupila: el punto oscuro que da la mirada
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, irisPos, null,
                        new Color(12, 7, 20) * 0.9f, 0f, VFXCore.SoftGlow.Size() * 0.5f,
                        0.07f, SpriteEffects.None, 0f);
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
        private Vector2 _offsetClavada = Vector2.Zero;
        private float _angClavada = -MathHelper.PiOver2;
        private Vector2 _posVuelta = Vector2.Zero;
    }
}
