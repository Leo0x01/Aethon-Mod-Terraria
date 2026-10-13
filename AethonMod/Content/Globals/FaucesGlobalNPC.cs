using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// FAUCESGLOBALNPC — v6.50.62 — LA MECÁNICA DE LA DEVORACIÓN.
    ///
    /// «Cuando el jefe llega a 1 punto de vida, su animación original de
    /// muerte se DETIENE y no avanza; en su lugar, la nueva animación: la
    /// sombra sale del libro y lo DEVORA.»
    ///
    /// LA RECETA (investigación 2-b, verificada contra Fargo Mutant, TSA
    /// Tsukiyomi, CalamityOverhaul y el propio AethonBoss de la casa —
    /// que usa EXACTAMENTE este patrón desde hace 60 versiones):
    ///
    ///   CheckDead (1ª pasada) → life=1 · dontTakeDamage · damage=0 ·
    ///   fase=DEVORADO · netUpdate · return false  → la muerte vanilla
    ///   NUNCA ARRANCA (ni gore, ni sonido, ni loot: NADA).
    ///   PreAI (congelado)     → return false: ni la IA ni la animación
    ///   del jefe corren — queda POSADO en 1 HP mientras lo comen.
    ///   Final del festín      → life=0 · fase=MUERTE_REAL ·
    ///   checkDead() (2ª llamada) → TODO el pipeline vanilla: loot,
    ///   bestiario, banners, flags downed*, OnKill de terceros. El jefe
    ///   muere DE VERDAD, con su botín completo.
    ///
    /// AethonBoss (el jefe del mod) NO pasa por aquí: su CheckDead de
    /// ModNPC corre ANTES que este GlobalNPC y devuelve false siempre —
    /// su cine de muerte propia es sagrado.
    ///
    /// MULTIPLAYER: la lógica autoritativa (drain, kill final) corre en
    /// server/SP (guard Main.netMode != MultiplayerClient); los campos
    /// viajan en SendExtraAI (se sincronizan con cada netUpdate) y los
    /// clientes congelan su simulación local en PreAI por su cuenta.
    ///
    /// =================================================================
    /// v6.50.110 — ESTADO 3: LA PRIMERA CENA (el festín del primer jefe).
    /// v6.50.114 — LA LETRA NUEVA: «el jefe debe ser rodeado por anillos
    /// iguales a los de los proyectiles agujeros negro y además también
    /// poner anillos como los de los soles runicos, ser llevado al centro
    /// de la pantalla y ser absorbido por el libro» — y EL DAÑO MASIVO
    /// TAMBIÉN CENA (CheckDead intercepta la muerte de un golpe: la
    /// primera muerte ES la cena, venga como venga).
    /// La letra del usuario: «cuando se mata un jefe su animacion de
    /// muerte se detiene y sea deborado por el grimorio usando la sombra
    /// de la pagina, el jugador recibe loot ya que cuenta como una muerte
    /// normal, lo que sucede es que el jugador ataca al jefe y cuando
    /// llegue a 100 de vida el jefe se detiene circulos magicos como los
    /// de los agujeros negros aprisionan al jefe, lo levantan y llevan
    /// al centro de la pantalla y luego del grimorio sale la sombra de
    /// la pagina y devora por completo al jefe, que esto suceda la
    /// primera vez que se mate un jefe y solo la primera vez, ya
    /// despues de la primera vez el jefe muere de forma normal».
    ///
    /// EL GATILLO (PreAI, autoridad): jefe ≤ 100 de vida + portador con
    /// el Eterno VISIBLE + tipo de jefe SIN devorar (ShardPlayer.
    /// JefesDevorados — se marca AL ARRANCAR: un tipo, UNA cena).
    /// LA COREOGRAFÍA (LargoPrimeraCena = 430 t ≈ 7,2 s):
    ///   0-40   fase 0 · EL SELLO — los círculos mágicos se materializan
    ///          ALREDEDOR del jefe detenido (vida clavada en 100).
    ///   40-60  fase 1 · LA CONTRACCIÓN — los aros se cierran y lo
    ///          APRISIONAN.
    ///   60-150 fase 2 · LA ELEVACIÓN — los sellos LO LEVANTAN (−90 px)
    ///          y lo LLEVAN al CENTRO DE LA PANTALLA (vive: si el
    ///          portador camina, el jefe lo sigue colgado del centro).
    ///   150-175 fase 3 · LA PRESENTACIÓN — el sello íntimo se cierra
    ///          del todo (el vice).
    ///   175-345 fase 4 · EL FESTÍN — DEL GRIMORIO (la mano del
    ///          portador) sale LA SOMBRA DE LA PÁGINA y lo devora
    ///          (la cobertura crece, muerde con pulso, las almas vuelan).
    ///   345-430 fase 5 · EL TRAGO — los sellos se ENCIENEN en el jefe
    ///          (radio → 0), la elipse entera SE INHALA hacia la sombra
    ///          y a los 430 t llega LA MUERTE REAL (2ª pasada de
    ///          CheckDead): loot completo, sin verse el gore (la purga
    ///          de la bruma lo tapa).
    /// Los worms (realLife): el dueño del pool viaja al centro y los
    /// segmentos se ENROSCAN a su alrededor (determinista por whoAmI).
    /// </summary>
    public class FaucesGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        // === EL ESTADO (por NPC) ===
        /// <summary>0 = normal · 1 = DEVORADO (congelado en 1 HP) · 2 = muerte real ya ejecutada · 3 = LA PRIMERA CENA (el festín del primer jefe).</summary>
        public byte DevorarEstado;
        /// <summary>3 = sombra de la página (v6.50.72) · 4 = LA PRIMERA CENA de la .110.</summary>
        public byte DevorarEstilo;
        /// <summary>El reloj del festín (el motor lo alimenta en server; el cliente lo cuenta local).</summary>
        public ushort DevorarTick;

        /// <summary>Duración total de la devoración clásica (ticks) — 3.5 s de festín.</summary>
        public const int LargoFestin = 210;

        // === v6.50.110 — LA PRIMERA CENA: constantes de la coreografía ===
        /// <summary>El estado de LA PRIMERA CENA (el festín del primer jefe).</summary>
        public const byte ESTADO_PRIMERA_CENA = 3;
        /// <summary>La vida donde el jefe SE DETIENE (la letra: «cuando llegue a 100 de vida el jefe se detiene»).</summary>
        public const int VidaUmbralPrimeraCena = 100;
        /// <summary>Duración total de LA PRIMERA CENA (ticks): sello 40 · contracción 20 · elevación 90 · presentación 25 · festín 170 · trago 85.</summary>
        public const int LargoPrimeraCena = 430;

        /// <summary>El centro de donde partía el jefe al detenerse (el ancla de la elevación — memoria del server).</summary>
        private Vector2 _posCenaBase;
        /// <summary>¿Ya se capturó el ancla de la cena? (la posición se congela al arrancar)</summary>
        private bool _posCenaLista;

        // === LA CENA ÚNICA: UNA primera cena a la vez en el mundo ===
        /// <summary>whoAmI del jefe en LA PRIMERA CENA activa (-1 = ninguna): los gemelos esperan su turno.</summary>
        private static int _cenaActiva = -1;

        // === LAS MARCAS (golpe mortal de nuestras armas) ===
        // whoAmI del jefe → tick de caducidad. La marca vive 5 s: si otra
        // cosa lo mata después, la muerte es de esa otra cosa.
        private static readonly Dictionary<int, int> _marcas = new();
        /// <summary>whoAmI → estilo del arma que marcó.</summary>
        private static readonly Dictionary<int, byte> _marcasEstilo = new();

        // ==================================================================
        //  LA MARCA — la llaman las armas cuando SU golpe es mortal
        // ==================================================================

        /// <summary>
        /// Marca un jefe para la devoración: su PRÓXIMA muerte (la que
        /// está pasando AHORA, vida ≤ 0) se intercepta. Los worms con
        /// pool compartido (realLife) marcan al DUEÑO del pool — el que
        /// de verdad corre checkDead.
        /// </summary>
        public static void Marcar(NPC npc, byte estilo)
        {
            if (npc == null || !npc.active || !EsJefe(npc)) return;
            NPC dueño = DueñoDelPool(npc);
            if (dueño == null || !dueño.active) return;

            if (Main.netMode == NetmodeID.MultiplayerClient) return; // la autoridad decide

            _marcas[dueño.whoAmI] = 300;              // caduca en 5 s
            _marcasEstilo[dueño.whoAmI] = estilo;
            dueño.netUpdate = true;
        }

        /// <summary>El jefe de verdad: con pool compartido (realLife), el dueño; si no, él mismo.</summary>
        public static NPC DueñoDelPool(NPC npc)
        {
            if (npc == null || !npc.active) return null;
            if (npc.realLife >= 0 && npc.realLife != npc.whoAmI && npc.realLife < Main.npc.Length)
            {
                NPC d = Main.npc[npc.realLife];
                return d != null && d.active ? d : null;
            }
            return npc;
        }

        /// <summary>¿Es un jefe devorable? (AethonBoss se excluye: su CheckDead propio corre antes).</summary>
        public static bool EsJefe(NPC npc)
        {
            if (npc == null || !npc.active) return false;
            if (npc.type == ModContent.NPCType<NPCs.AethonBoss>()) return false;
            return npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type];
        }

        /// <summary>v6.50.110 — ¿ESTA presa está en un festín de las Fauces
        /// (la devoración clásica o LA PRIMERA CENA)? Las sombras de la
        /// página la SUELTAN (el festín en curso la termina con SU cine
        /// y SU loot) y AbsorberPresa la respeta.</summary>
        public static bool EnFauces(NPC npc)
        {
            NPC dueño = DueñoDelPool(npc);
            if (dueño == null || !dueño.active) return false;
            return dueño.TryGetGlobalNPC<FaucesGlobalNPC>(out var g) && g.DevorarEstado != 0;
        }

        // ==================================================================
        //  v6.50.110 — LA PRIMERA CENA: ¿le toca a ESTE jefe?
        // ==================================================================

        /// <summary>¿El jugador lleva un Grimorio del Eterno en la BARRA RÁPIDA (slots 0-9)?</summary>
        private static bool TieneEternoVisible(Player p)
        {
            int tipo = ModContent.ItemType<Weapons.GrimoireEternal>();
            for (int i = 0; i < 10; i++)
            {
                Item inv = p.inventory[i];
                if (inv != null && inv.type == tipo) return true;
            }
            return false;
        }

        /// <summary>
        /// ¿ESTE jefe estrena su primera cena? Busca al portador que lo
        /// peleó (playerInteraction) con el Eterno visible y SIN este
        /// tipo en su lista de devorados (la «solo la primera vez» de la
        /// letra — ShardPlayer.JefesDevorados).
        /// </summary>
        private static bool PrimeraCenaPendiente(NPC jefe, out Player portador)
        {
            portador = null;
            // la cena única: si YA hay una en marcha, el próximo jefe espera
            if (_cenaActiva >= 0)
            {
                NPC activo = _cenaActiva < Main.npc.Length ? Main.npc[_cenaActiva] : null;
                if (activo != null && activo.active &&
                    activo.TryGetGlobalNPC<FaucesGlobalNPC>(out var gAct) &&
                    gAct.DevorarEstado == ESTADO_PRIMERA_CENA)
                    return false;
                _cenaActiva = -1;   // la anterior ya terminó: la mesa está libre
            }
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.dead) continue;
                if (!jefe.playerInteraction[i]) continue;       // el que lo peleó
                if (!TieneEternoVisible(p)) continue;           // con el libro a mano
                var sp = p.GetModPlayer<Players.ShardPlayer>();
                if (sp == null) continue;
                if (sp.JefesDevorados.Contains(jefe.type)) continue;  // ya se lo comió
                portador = p;
                return true;
            }
            return false;
        }

        /// <summary>
        /// ARRANCA LA PRIMERA CENA: el jefe se DETIENE en 100 de vida,
        /// la lista se marca YA (un tipo, UNA cena) y el cine toma el
        /// mando (PreAI mueve, el proyectil dibuja).
        /// </summary>
        public static void ArrancarPrimeraCena(NPC jefe, Player portador)
        {
            if (jefe == null || !jefe.active || portador == null) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (!jefe.TryGetGlobalNPC<FaucesGlobalNPC>(out var g) || g.DevorarEstado != 0) return;

            // EL REGISTRO ANTES DEL ESPECTÁCULO (la «solo la primera vez»):
            // aunque el cine se interrumpiera, este tipo ya NO repite
            try
            {
                var sp = portador.GetModPlayer<Players.ShardPlayer>();
                if (sp != null && !sp.JefesDevorados.Contains(jefe.type))
                    sp.JefesDevorados.Add(jefe.type);
            }
            catch { }

            g.DevorarEstado = ESTADO_PRIMERA_CENA;
            g.DevorarEstilo = 4;
            g.DevorarTick = 0;
            g.FaseFestin = 0;
            g._posCenaBase = jefe.Center;
            g._posCenaLista = true;
            _cenaActiva = jefe.whoAmI;

            jefe.life = VidaUmbralPrimeraCena;    // se detiene EN 100
            jefe.active = true;
            jefe.dontTakeDamage = true;
            jefe.damage = 0;
            jefe.netUpdate = true;

            // LA PELÍCULA: el proyectil del cine (netImportant: viaja solo)
            if (Main.projectile.Length > 0)
            {
                int p = Projectile.NewProjectile(jefe.GetSource_FromAI(),
                    jefe.Center, Vector2.Zero, ModContent.ProjectileType<PrimeraCenaProjectile>(),
                    0, 0f, Main.myPlayer, jefe.whoAmI, portador.whoAmI);
                if (p >= 0) Main.projectile[p].timeLeft = LargoPrimeraCena + 120;
            }
        }

        // ==================================================================
        //  EL INICIO DEL FESTÍN — desde el drain del arma (vida→1 directo)
        // ==================================================================

        /// <summary>
        /// Arranca la devoración SIN pasar por checkDead (la llaman los
        /// drains de las armas cuando la vida tocaría el suelo): clava la
        /// vida en 1, congela, suelta la marca y cede el mando al motor.
        /// </summary>
        public static void Iniciar(NPC npc, byte estilo)
        {
            if (npc == null || !npc.active) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            NPC dueño = DueñoDelPool(npc);
            if (dueño == null || !dueño.active) return;

            // === v6.50.114 — LA CENA TIENE PRIORIDAD SOBRE EL FESTÍN: el
            //     drain que salta de lleno al suelo (daño masivo del arma)
            //     no puede devorarse él solo al jefe — si a ESTE jefe aún
            //     le estrena su primera cena, el drain lo ENTREGA a la
            //     cena (la letra: la primera muerte es del LIBRO) ===
            if (PrimeraCenaPendiente(dueño, out Player portadorCena))
            {
                _marcas.Remove(dueño.whoAmI);
                _marcasEstilo.Remove(dueño.whoAmI);
                ArrancarPrimeraCena(dueño, portadorCena);
                return;
            }

            if (!dueño.TryGetGlobalNPC<FaucesGlobalNPC>(out var g)) return;
            if (g.DevorarEstado != 0) return;                     // ya se lo están comiendo

            _marcas.Remove(dueño.whoAmI);
            _marcasEstilo.Remove(dueño.whoAmI);

            g.DevorarEstado = 1;
            g.DevorarEstilo = estilo;
            g.DevorarTick = 0;
            g.FaseFestin = 0;

            dueño.life = 1;
            dueño.active = true;
            dueño.dontTakeDamage = true;
            dueño.damage = 0;
            dueño.netUpdate = true;

            // LA ANIMACIÓN: un proyectil-festín por cabeza (viaja solo por
            // la red de proyectiles — el cliente lo ve llegar solo).
            if (dueño.whoAmI >= 0 && Main.projectile.Length > 0)
            {
                int p = Projectile.NewProjectile(dueño.GetSource_FromAI(),
                    dueño.Center, Vector2.Zero, ModContent.ProjectileType<FauceDevoradorProjectile>(),
                    0, 0f, Main.myPlayer, dueño.whoAmI, estilo);
                if (p >= 0) Main.projectile[p].timeLeft = LargoFestin + 90;
            }
        }

        // ==================================================================
        //  LOS HOOKS
        // ==================================================================

        public override bool CheckDead(NPC npc)
        {
            // 2ª pasada (o muerte post-festín): la muerte REAL de vanilla.
            if (DevorarEstado == 2) return true;

            // === v6.50.114 — EL DAÑO MASIVO TAMBIÉN CENA (la letra: «cuando
            //     el daño que recibe el jefe es masivo no se aplica eso de
            //     que su primera muerte sea absorbido por el grimorio»):
            //     el gatillo de PreAI exige vida > 0 — un golpe que cruza
            //     de lleno a ≤ 0 SIN pasar por las 100 robaba la cena (el
            //     jefe moría normal). AHORA la muerte también se
            //     intercepta AQUÍ: si a este jefe le estrena su primera
            //     cena, la PRIMERA MUERTE ES LA CENA — venga como venga
            //     (masiva, drenada o a cuotas). La cena tiene PRIORIDAD
            //     sobre el festín clásico de las marcas. ===
            if (DevorarEstado == 0 && npc.life <= 0 &&
                Main.netMode != NetmodeID.MultiplayerClient && EsJefe(npc))
            {
                NPC dueñoCena = DueñoDelPool(npc);
                if (dueñoCena != null && dueñoCena.active &&
                    PrimeraCenaPendiente(dueñoCena, out Player portadorCena))
                {
                    _marcas.Remove(dueñoCena.whoAmI);
                    _marcasEstilo.Remove(dueñoCena.whoAmI);
                    ArrancarPrimeraCena(dueñoCena, portadorCena);
                    return false;   // la muerte original NUNCA ARRANCA: primero la cena
                }
            }

            // ¿Marca viva de nuestras armas? (el golpe mortal fue NUESTRO)
            bool marcado = _marcas.TryGetValue(npc.whoAmI, out int caduca)
                && caduca > 0;
            if (!marcado) return true;                            // muerte ajena: normal

            // v6.50.66 — el estilo 1 (la pinza del gif) murió con su arma:
            // el festín por defecto es la sombra (3).
            byte estilo = _marcasEstilo.TryGetValue(npc.whoAmI, out byte e) ? e : (byte)3;
            _marcas.Remove(npc.whoAmI);
            _marcasEstilo.Remove(npc.whoAmI);

            // === LA INTERCEPCIÓN (1ª pasada) ===
            DevorarEstado = 1;
            DevorarEstilo = estilo;
            DevorarTick = 0;
            FaseFestin = 0;

            npc.life = 1;
            npc.active = true;
            npc.dontTakeDamage = true;
            npc.damage = 0;
            npc.netUpdate = true;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int p = Projectile.NewProjectile(npc.GetSource_Death(),
                    npc.Center, Vector2.Zero, ModContent.ProjectileType<FauceDevoradorProjectile>(),
                    0, 0f, Main.myPlayer, npc.whoAmI, estilo);
                if (p >= 0) Main.projectile[p].timeLeft = LargoFestin + 90;
            }
            return false;   // ← SU MUERTE ORIGINAL NUNCA ARRANCA
        }

        public override bool PreAI(NPC npc)
        {
            // === v6.50.110 — EL GATILLO DE LA PRIMERA CENA: el jefe llegó
            //     a 100 de vida y hay portador con derecho a su primera
            //     cena de ESTE tipo → el jefe SE DETIENE y arranca el cine
            if (DevorarEstado == 0 && !npc.dontTakeDamage && npc.life > 0 &&
                Main.netMode != NetmodeID.MultiplayerClient && EsJefe(npc))
            {
                NPC dueñoPool = DueñoDelPool(npc);
                if (dueñoPool != null && dueñoPool.active && dueñoPool.life > 0 &&
                    dueñoPool.life <= VidaUmbralPrimeraCena &&
                    dueñoPool.TryGetGlobalNPC<FaucesGlobalNPC>(out var gPool) &&
                    gPool.DevorarEstado == 0 &&
                    PrimeraCenaPendiente(dueñoPool, out Player portador))
                {
                    ArrancarPrimeraCena(dueñoPool, portador);
                }
            }

            // congelar TAMBIÉN a los segmentos de un worm devorado
            bool devorado = DevorarEstado == 1 || DevorarEstado == ESTADO_PRIMERA_CENA;
            if (!devorado && npc.realLife >= 0 && npc.realLife != npc.whoAmI)
            {
                NPC dueño = Main.npc[npc.realLife];
                if (dueño != null && dueño.active &&
                    dueño.TryGetGlobalNPC<FaucesGlobalNPC>(out var gDueño) &&
                    (gDueño.DevorarEstado == 1 || gDueño.DevorarEstado == ESTADO_PRIMERA_CENA))
                    devorado = true;
            }
            if (!devorado) return true;

            // === v6.50.110 — LA PRIMERA CENA: el motor propio (la
            //     elevación al centro de la pantalla + el final) ===
            if (DevorarEstado == ESTADO_PRIMERA_CENA)
            {
                PrimeraCenaPreAI(npc);
                return false;   // ni IA ni animación: el cine manda
            }

            // === EL CONGELADO: ni IA ni animación (la muerte detenida) ===
            // las primeras 12 t frena SUAVE (no un telonazo físico)
            if (DevorarTick < 12) npc.velocity *= 0.82f;
            else npc.velocity *= 0f;
            if (npc.lifeRegen < 0) npc.lifeRegen = 0;

            // v6.50.69 — LA FASE VISUAL DEL FESTÍN (0 manifestación ·
            // 1 envolver · 2 EL TRAGADO · 3 la disipación): la calcula el
            // motor (server/SP) y viaja por SendExtraAI — el PreDraw de
            // ABAJO la usa para retirar el sprite a su tiempo y el
            // proyectil-festín para sincronizar su animación en MP
            FaseFestin = DevorarTick < 30 ? (byte)0
                : DevorarTick < 60 ? (byte)1
                : DevorarTick < 160 ? (byte)2
                : (byte)3;

            // === EL MOTOR DEL FESTÍN ===
            if (DevorarTick < ushort.MaxValue - 2) DevorarTick++;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Los SONIDOS del festín (solo donde hay oídos)
                if (DevorarTick == 1)
                    Sonar(SoundID.Roar.WithPitchOffset(-0.45f).WithVolumeScale(0.9f), npc.Center);
                if (DevorarTick == 60 || DevorarTick == 110 || DevorarTick == 160)
                    Sonar(SoundID.Item122.WithPitchOffset(-0.30f).WithVolumeScale(0.55f), npc.Center);

                // redes: refresco periódico del estado (los clientes animan local)
                if (DevorarTick % 15 == 0) npc.netUpdate = true;

                // === EL FINAL: la muerte REAL (2ª llamada a checkDead) ===
                if (DevorarTick >= LargoFestin)
                {
                    DevorarEstado = 2;
                    npc.life = 0;
                    npc.dontTakeDamage = false;
                    npc.netUpdate = true;
                    npc.checkDead();   // → gore bajo la bruma + LOOT COMPLETO + bestiario + downed*
                }
                else if (DevorarTick > LargoFestin + 60)
                {
                    // RED DE SEGURIDAD: jamás un jefe zombi atascado
                    DevorarEstado = 2;
                    npc.life = 0;
                    npc.checkDead();
                }
            }
            return false;   // ← NI IA NI ANIMACIÓN: POSADO
        }

        // ==================================================================
        //  v6.50.110 — EL MOTOR DE LA PRIMERA CENA (PreAI del estado 3)
        // ==================================================================

        /// <summary>El centro VIVO de la pantalla en coords de mundo (el jefe cuelga de ÉL).</summary>
        private static Vector2 CentroDePantalla =>
            Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;

        private void PrimeraCenaPreAI(NPC npc)
        {
            // el reloj (el cliente también lo cuenta: congela su simulación)
            if (DevorarTick < ushort.MaxValue - 2) DevorarTick++;
            int t = DevorarTick;

            // LA FASE VISUAL (idéntica en server y cliente: viaja además
            // por SendExtraAI para los que llegan a media cena)
            FaseFestin = t < 40 ? (byte)0       // EL SELLO
                : t < 60 ? (byte)1              // LA CONTRACCIÓN
                : t < 150 ? (byte)2             // LA ELEVACIÓN AL CENTRO
                : t < 175 ? (byte)3             // LA PRESENTACIÓN (el vice)
                : t < 345 ? (byte)4             // EL FESTÍN DE LA SOMBRA
                : (byte)5;                      // EL TRAGO

            // === EL CONGELADO: ni IA ni animación (la muerte detenida) ===
            if (t < 12) npc.velocity *= 0.80f;
            else npc.velocity *= 0f;
            if (npc.lifeRegen < 0) npc.lifeRegen = 0;

            // v6.50.110 — EL CLAVO: la vida se queda EN 100 (la letra:
            // «cuando llegue a 100 de vida el jefe se detiene») — ninguna
            // vía (un residuo, un drain tardío) la baja mientras cena
            if (npc.life < VidaUmbralPrimeraCena && npc.life > 0)
                npc.life = VidaUmbralPrimeraCena;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;   // el cliente congela y cuenta; el MOVIMIENTO llega por la red

            // el ancla de la elevación (la posición del detenido)
            if (!_posCenaLista)
            {
                _posCenaBase = npc.Center;
                _posCenaLista = true;
            }

            // === LOS SONIDOS DEL CINE (solo donde hay oídos) ===
            if (t == 1) Sonar(SoundID.Item122.WithPitchOffset(-0.55f).WithVolumeScale(0.7f), npc.Center);   // el sello
            if (t == 40) Sonar(SoundID.Item122.WithPitchOffset(-0.35f).WithVolumeScale(0.55f), npc.Center);  // la contracción
            if (t == 60) Sonar(SoundID.Roar.WithPitchOffset(-0.45f).WithVolumeScale(0.85f), npc.Center);      // se resiste
            if (t == 150) Sonar(SoundID.Item122.WithPitchOffset(0.10f).WithVolumeScale(0.6f), npc.Center);   // el vice
            if (t == 175) Sonar(SoundID.Item122.WithPitchOffset(-0.30f).WithVolumeScale(0.6f), npc.Center);  // EL LIBRO DESPIERTA (.114)
            if (t == 215 || t == 255 || t == 295 || t == 330)
                Sonar(SoundID.NPCHit9.WithPitchOffset(-0.10f).WithVolumeScale(0.5f), npc.Center);             // mastica
            if (t == 345) Sonar(SoundID.Item122.WithPitchOffset(0.34f).WithVolumeScale(0.6f), npc.Center);   // el trago
            if (t == 400) Sonar(SoundID.Item122.WithPitchOffset(0.48f).WithVolumeScale(0.8f), npc.Center);   // v6.50.114: EL LIBRO SE CIERRA

            // === LA ELEVACIÓN AL CENTRO DE LA PANTALLA (la letra: los
            //     círculos «lo levantan y llevan al centro de la
            //     pantalla»): sube −90 px, VIAJA al centro y de allí
            //     CUELGA del centro vivo (si el portador camina, el
            //     preso lo sigue) con el vaivén del aprisionado ===
            if (t >= 60)
            {
                float subir = MathHelper.Clamp((t - 60) / 40f, 0f, 1f);
                float viajar = MathHelper.Clamp((t - 100) / 50f, 0f, 1f);
                Vector2 levantado = _posCenaBase + new Vector2(0f, -90f * MathHelper.SmoothStep(0f, 1f, subir));
                Vector2 destino = t >= 150 ? CentroDePantalla
                    : Vector2.Lerp(levantado, CentroDePantalla, MathHelper.SmoothStep(0f, 1f, viajar));
                float vaivén = t < 175 ? 1f : 0.4f;
                destino += new Vector2(MathF.Sin(t * 0.31f) * 3f, MathF.Sin(t * 0.23f) * 4f) * vaivén;
                npc.Center = destino;

                // LOS SEGMENTOS DEL WORM: se ENROSCAN alrededor del centro
                // (determinista por whoAmI — la serpiente se hace un nudo)
                int idx = 0;
                for (int i = 0; i < Main.npc.Length; i++)
                {
                    NPC seg = Main.npc[i];
                    if (seg == null || !seg.active || i == npc.whoAmI) continue;
                    if (seg.realLife != npc.whoAmI) continue;      // no es mío
                    float ang = idx * 0.7f + t * 0.02f;
                    Vector2 metaSeg = destino + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.6f)
                        * (46f + 7f * idx);
                    seg.Center = Vector2.Lerp(seg.Center, metaSeg, 0.25f);
                    seg.netUpdate = true;
                    idx++;
                }
            }

            // redes: refresco periódico (los clientes animan local)
            if (t % 10 == 0) npc.netUpdate = true;

            // === EL FINAL: LA MUERTE REAL (2ª llamada a checkDead) —
            //     loot completo, bestiario, banners, downed* ===
            if (t >= LargoPrimeraCena)
            {
                DevorarEstado = 2;
                _cenaActiva = -1;
                npc.life = 0;
                npc.dontTakeDamage = false;
                npc.netUpdate = true;
                npc.checkDead();
            }
            else if (t > LargoPrimeraCena + 60)
            {
                // RED DE SEGURIDAD: jamás un jefe zombi colgado del centro
                DevorarEstado = 2;
                _cenaActiva = -1;
                npc.life = 0;
                npc.checkDead();
            }
        }

        /// <summary>CheckActive: false = NO despawnee durante el festín (bool en este tML).</summary>
        public override bool CheckActive(NPC npc)
        {
            if (DevorarEstado == 1 || DevorarEstado == ESTADO_PRIMERA_CENA) return false;
            return base.CheckActive(npc);
        }

        /// <summary>
        /// v6.50.69 — LA BRUMA ES LA ANIMACIÓN (la letra del usuario:
        /// «toda esa bruma que se traga al jefe debe animarse para que
        /// sustituya cualquier animación»): desde la fase 2 (la cobertura
        /// YA es total) el sprite del jefe SE RETIRA del mundo — lo que
        /// se ve es SOLO la masa de bruma tragándoselo. La fase viaja por
        /// la red (FaseFestin en SendExtraAI): todos los clientes retiran
        /// el sprite al mismo tick. Si el festín se interrumpe, el estado
        /// vuelve a 0 y el sprite REGRESA (nada queda invisible y vivo).
        /// v6.50.110 — LA PRIMERA CENA se retira MÁS TARDE: el preso se
        /// VE colgado de los sellos hasta la mitad del festín (t ≥ 260);
        /// la bruma de la devoración lo tapa desde entonces.
        /// </summary>
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (DevorarEstado == 1 && FaseFestin >= 2) return false;
            if (DevorarEstado == ESTADO_PRIMERA_CENA && DevorarTick >= 260) return false;
            return true;
        }

        /// <summary>Los DoT no pueden matarlo durante el festín.</summary>
        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (DevorarEstado == 1 || DevorarEstado == ESTADO_PRIMERA_CENA) damage = 0;
        }

        // ==================================================================
        //  SINCRONIZACIÓN (multiplayer)
        // ==================================================================

        /// <summary>Fase visual del festín (para el render del proyectil-festín).</summary>
        public byte FaseFestin;

        public override void SendExtraAI(NPC npc, Terraria.ModLoader.IO.BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            // EL BIT: ¿este NPC lleva festín? (1 bit por paquete — barato
            // para TODOS los NPCs del mundo; los bytes solo viajan si toca)
            bitWriter.WriteBit(DevorarEstado != 0);
            if (DevorarEstado != 0)
            {
                binaryWriter.Write(DevorarEstado);
                binaryWriter.Write(DevorarEstilo);
                binaryWriter.Write(FaseFestin);
            }
        }

        public override void ReceiveExtraAI(NPC npc, Terraria.ModLoader.IO.BitReader bitReader, BinaryReader binaryReader)
        {
            if (!bitReader.ReadBit()) return;

            byte estado = binaryReader.ReadByte();
            DevorarEstilo = binaryReader.ReadByte();
            FaseFestin = binaryReader.ReadByte();

            if (estado == 1 && DevorarEstado == 0)
            {
                // el cliente aprende del festín: congela su simulación local
                DevorarEstado = 1;
                DevorarTick = 0;
            }
            else if (estado == ESTADO_PRIMERA_CENA && DevorarEstado == 0)
            {
                // v6.50.110 — el cliente aprende de LA PRIMERA CENA: congela
                // su simulación local y cuenta el cine con su propio reloj
                DevorarEstado = ESTADO_PRIMERA_CENA;
                DevorarTick = 0;
            }
            else DevorarEstado = estado;
        }

        // ==================================================================
        //  LIMPIEZA (el dict estático no puede pudrirse)
        // ==================================================================

        /// <summary>Podrido de las marcas (lo llama el ModSystem cada segundo).</summary>
        public static void PodarMarcas()
        {
            if (_marcas.Count == 0) return;
            List<int> muertas = null;
            foreach (KeyValuePair<int, int> kv in _marcas)
            {
                int restante = kv.Value - 60;   // 1 s por llamada
                if (restante <= 0) { muertas ??= new List<int>(); muertas.Add(kv.Key); }
                else _marcas[kv.Key] = restante;
            }
            if (muertas != null)
                for (int i = 0; i < muertas.Count; i++)
                {
                    _marcas.Remove(muertas[i]);
                    _marcasEstilo.Remove(muertas[i]);
                }
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
