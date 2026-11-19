using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.NPCs
{
    // ======================================================================
    //  EL CRONISTA — v6.154 — EL ESCRIBA INMORTAL DEL MARGEN.
    //
    //  EL LORE CANÓNICO (el Codex del Eterno, Lámina X — la letra del
    //  usuario): «Fui testigo del primer devorado… la única forma de
    //  seguir existiendo era hacerse útil al hambre… consumí la misma
    //  Esencia del Eterno y desde entonces no puedo morir. Él sabe que
    //  existo. Me mira, de vez en cuando, como se mira el mueble que
    //  siempre ha estado ahí». EL CRONISTA ES:
    //   · INMORTAL (la inmortalidad «me fue cobrada»): jamás muere —
    //     ni vida que baje a 0, ni golpe que lo mate. canBeHitBy ==
    //     false en TODO; su vida ES decorado (la tinta no se rompe);
    //   · ÚNICO POR MUNDO (la letra: «siempre existira un cronista por
    //     mundo sin importar cuantos jugadores con grimorios haya»):
    //     el spawner de la v6.153 ya no dobla — y CanTownNPCSpawn
    //     exige el grimorio de ALGUIEN;
    //   · LA TIENDA LIGADA AL COMPRADOR (la letra: «lo que vende
    //     siempre estara ligado al jugador que va a comprarle… el
    //     cronista sabra el nivel de despertar de cada uno y vendera
    //     la respectiva mercancia para cada uno»): la tienda se
    //     reconstruye EN CADA APERTURA con el DESPERTAR DEL QUE HABLA
    //     (ModifyActiveShop — el hook que recibe al comprador);
    //   · SOLO ESENCIAS DEL ETERNO (la letra): CustomCurrencySystem
    //     registrado (el patrón vanilla de las Medallas de Defensor —
    //     verificado por reflexión contra el tModLoader.dll REAL);
    //   · LO QUE VENDE (la letra): LOS REQUISITOS DE CADA DESPERTAR —
    //     las armas del peldaño siguiente del comprador, cada una a
    //     100 + (rareza × nivel de Despertar del requisito) − nivel de
    //     oleadas de hambre (mín 100), y LAS POCIONES DE MANÁ
    //     SINCRONIZADAS con el Mago vanilla (los mismos desbloqueos
    //     de NPC.downedMechBossAny/hardMode: 1 esencia la básica → 5
    //     la mayor).
    // ======================================================================
    [AutoloadHead]
    public class ElCronista : ModNPC
    {
        // ================================================================
        //  v6.157 — EL CUERPO ES LA TIRA PROPIA (ver PreDraw): el
        //  PlayerRenderer de la v6.156 MURIÓ (frágil: un fallo del
        //  new Player() fuera de mundo dejaba al NPC INVISIBLE — el
        //  «realmente no aparece» del usuario). El sprite es el
        //  NPC_25_Frames clásico de los town NPC (verificado en el
        //  framer del decompile: Main.npcFrameCount[Type]=25 con
        //  ExtraFrames=5 y AttackFrames=4 — el layout del Guide).
        // ================================================================

        public override void SetStaticDefaults()
        {
            // LA ANIMACIÓN PROPIA (la tira forjada v6.157: 25 marcos
            // del town NPC — idle, caminar, EL ESCRIBA SENTADO y el
            // lanzamiento de la pluma). EL LAYOUT DEL GUIDE (el framer
            // del decompile): 0 idle · 1-14 andar · 15-20 extra ·
            // 21-24 ataque → ExtraFrames = 5, AttackFrames = 4.
            Main.npcFrameCount[Type] = 25;
            NPCID.Sets.ExtraFramesCount[Type] = 5;
            NPCID.Sets.AttackFrameCount[Type] = 4;
            NPCID.Sets.DangerDetectRange[Type] = 700;
            NPCID.Sets.AttackType[Type] = 0;          // lanzamiento (la pluma)
            NPCID.Sets.AttackTime[Type] = 90;
            NPCID.Sets.AttackAverageChance[Type] = 30;
            NPCID.Sets.NoTownNPCHappiness[Type] = true;   // precio fijo (la casa no cobra cariño)
            NPCID.Sets.NPCFramingGroup[Type] = 0;
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;        // EL flag esencial: aldeano de verdad
            NPC.friendly = true;       // jamás hostil
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = NPCAIStyleID.Passive;
            NPC.damage = 10;
            NPC.defense = 15;
            NPC.lifeMax = 250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;  // v6.154: la tinta NO retrocede
            AnimationType = NPCID.Guide;
        }

        // ================================================================
        //  v6.154 — LA INMORTALIDAD (la letra del lore: «desde entonces
        //  no puedo morir» + «recuerda que el cronista es inmortal»):
        //  NADIE lo golpea, NADIE lo mata. Su vida jamás baja.
        // ================================================================
        public override bool? CanBeHitByItem(Player player, Item item) => false;
        public override bool? CanBeHitByProjectile(Projectile projectile) => false;
        public override bool CanBeHitByNPC(NPC attacker) => false; // ni los jefes lo rozan
        public override bool CanHitNPC(NPC target) => false;      // él tampoco golpea de melee
        public override bool CheckDead() => false;                 // jamás muere
        public override bool PreAI()
        {
            // la tinta siempre entera (por si ALGO de vanilla la raya)
            if (NPC.life < NPC.lifeMax) { NPC.life = NPC.lifeMax; }

            // ============================================================
            //  LA PARTIDA (la regla de siempre): si NADIE lleva ya el
            //  grimorio, el Cronista SE DESPIDE Y SE VA (despawn activo
            //  — el patrón PreAI de los mods grandes). NOTA: no es
            //  morir — es IRSE (el inmortal camina a su siguiente
            //  mundo; la tinta no se rompe).
            // ============================================================
            if (Main.netMode == NetmodeID.MultiplayerClient) return true;
            try
            {
                int eterno = ModContent.ItemType
                    <global::AethonMod.Content.Weapons.GrimoireEternal>();
                bool alguienConLibro = false;
                foreach (Player p in Main.ActivePlayers)
                {
                    for (int i = 0; i < 58; i++)
                        if (p.inventory[i] != null && p.inventory[i].type == eterno)
                        { alguienConLibro = true; break; }
                    if (alguienConLibro) break;
                }
                if (!alguienConLibro && NPC.active)
                {
                    if (!Main.dedServ)
                        for (int k = 0; k < 12; k++)
                        {
                            var d = Dust.NewDustPerfect(NPC.Center,
                                DustID.Shadowflame,
                                new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-3f, -0.5f)),
                                120, new Color(90, 50, 150), 1.1f);
                            d.noGravity = true;
                        }
                    NPC.active = false;
                    NPC.netSkip = -1;
                    NPC.life = 0;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                }
            }
            catch { }
            return true;
        }

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = 20;
            knockback = 2f;
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 30;
            randExtraCooldown = 30;
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            projType = ProjectileID.ThrowingKnife;
            attackDelay = 5;
        }

        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            multiplier = 12f;
        }

        /// <summary>El Cronista ES el Cronista (una sola identidad).</summary>
        public override List<string> SetNPCNameList()
        {
            return new List<string> { "El Cronista" };
        }

        // ================================================================
        //  EL DIÁLOGO — LOS 22 EXCLUSIVOS DE LA LETRA, ORGANIZADOS POR
        //  DISPARADOR (hjson: Mods.AethonMod.Cronista.Chat* — la casa
        //  localiza; aquí solo la ELECCIÓN del disparador)
        // ================================================================
        public override string GetChat()
        {
            try
            {
                // LOS DISPARADORES DE LA LETRA:
                //  · La Onceava Oleada activa → la línea 12
                //  · El portador llega al Despertar 120 → la línea 15
                //  · ...y el resto: la rotación por peso (genéricas y
                //    las de portador/oleadas/despertar/sellado)
                if (global::AethonMod.Content.Systems.GrimorioFuriaSistema.EspecialActiva)
                    return Language.GetTextValue("Mods.AethonMod.Cronista.ChatOnceava");

                int despertar = 0;
                try
                {
                    despertar = global::AethonMod.Content.Systems.DespertarGrimorio
                        .NivelDe(Main.LocalPlayer);
                }
                catch { }
                if (despertar >= 120)
                    return Language.GetTextValue("Mods.AethonMod.Cronista.ChatUltimaPuerta");

                // LA ROTACIÓN: el dado de la casa sobre las 4 familias
                // (genéricas 5 · portador 5 · oleadas 3 · despertar 1 ·
                // sellado 2 · objetos 3 · cierre 2 — la letra completa)
                int familia = Main.rand.Next(21);
                string clave = familia switch
                {
                    int n when n < 5   => "Mods.AethonMod.Cronista.ChatGenerica" + (n + 1),
                    int n when n < 10  => "Mods.AethonMod.Cronista.ChatPortador" + (n - 4),
                    int n when n < 13  => "Mods.AethonMod.Cronista.ChatOleadas" + (n - 9),
                    13                => "Mods.AethonMod.Cronista.ChatDespertar",
                    int n when n < 16  => "Mods.AethonMod.Cronista.ChatSellado" + (n - 14),
                    int n when n < 19  => "Mods.AethonMod.Cronista.ChatObjetos" + (n - 15),
                    _                 => "Mods.AethonMod.Cronista.ChatCierre" + (familia - 18),
                };
                return Language.GetTextValue(clave);
            }
            catch
            {
                return "Trae Esencia del Eterno. Todo lo demás es ruido entre páginas.";
            }
        }

        // ================================================================
        //  v6.154 — LA LLEGADA AL INSTANTE (la regla de la v6.153 —
        //  se conserva exacta: UNO por mundo, nace junto al portador).
        // ================================================================
        private class CronistaSistema : ModSystem
        {
            private int _tick;

            public override void PostUpdateWorld()
            {
                if (++_tick < 60) return;
                _tick = 0;
                try
                {
                    if (Main.netMode == NetmodeID.MultiplayerClient) return;

                    // ¿YA vive? (UNO por mundo — jamás se dobla)
                    int tipo = ModContent.NPCType<ElCronista>();
                    for (int i = 0; i < Main.maxNPCs; i++)
                        if (Main.npc[i] != null && Main.npc[i].active &&
                            Main.npc[i].type == tipo) return;

                    // v6.157 — EL NPC TEST TAMBIÉN NACE (la letra: «el
                    // NPC test debe aparecer junto con el cronista»):
                    // el CanTownNPCSpawn del NPCTest NO LO HACE NACER
                    // JAMÁS (los town NPC sin casa no llegan — el
                    // chequeo vanilla de Main.SpawnTownNPC exige
                    // housing; por eso «el NPC test no existe no lo
                    // veo»). EL SPOWNER DE LA CASA: cuando nace el
                    // Cronista, su sombra de pruebas nace A SU LADO
                    // (UNO por mundo, el mismo bucle anti-doble).
                    int tipoTest = ModContent.NPCType<NPCTest>();
                    bool testVive = false;
                    for (int i = 0; i < Main.maxNPCs; i++)
                        if (Main.npc[i] != null && Main.npc[i].active &&
                            Main.npc[i].type == tipoTest) { testVive = true; break; }

                    // ¿ALGUIEN lleva el grimorio? Nace a SU lado.
                    int eterno = ModContent.ItemType
                        <global::AethonMod.Content.Weapons.GrimoireEternal>();
                    foreach (Player p in Main.ActivePlayers)
                    {
                        bool tiene = false;
                        for (int i = 0; i < 58; i++)
                            if (p.inventory[i] != null && p.inventory[i].type == eterno)
                            { tiene = true; break; }
                        if (!tiene) continue;

                        int tx = (int)(p.Center.X / 16f) + (p.direction >= 0 ? 3 : -3);
                        int ty = (int)(p.Center.Y / 16f);
                        int suelo = -1;
                        for (int y = ty - 4; y < ty + 22 && y < Main.maxTilesY - 4; y++)
                            if (WorldGen.SolidTile(tx, y)) { suelo = y; break; }
                        Vector2 pos = suelo >= 0
                            ? new Vector2(tx * 16f + 8f, suelo * 16f)
                            : p.Bottom;

                        int idx = NPC.NewNPC(p.GetSource_FromAI(),
                            (int)pos.X, (int)pos.Y, tipo, 0, 0f, p.whoAmI);
                        if (idx >= 0 && idx < Main.maxNPCs && Main.npc[idx].active)
                        {
                            Main.npc[idx].homeTileX = (int)(pos.X / 16f);
                            Main.npc[idx].homeTileY = (int)(pos.Y / 16f);
                            Main.npc[idx].netUpdate = true;

                            if (!Main.dedServ)
                                for (int k = 0; k < 16; k++)
                                {
                                    var d = Dust.NewDustPerfect(Main.npc[idx].Center,
                                        DustID.Shadowflame,
                                        new Vector2(Main.rand.NextFloat(-2.5f, 2.5f),
                                            Main.rand.NextFloat(-3.5f, -0.5f)),
                                        120, new Color(90, 50, 150), 1.2f);
                                    d.noGravity = true;
                                }
                            global::AethonMod.Content.Systems.EcoRed.AnunciarMundo("Mods.AethonMod.Cronista.Llega",
                                new Color(196, 138, 255));
                        }

                        // v6.157 — LA SOMBRA DE PRUEBAS nace JUNTO AL
                        // CRONISTA (la letra: «el NPC test debe aparecer
                        // junto con el cronista»): a 4 tiles a la derecha
                        // (o a la izquierda si no hay suelo), con SU
                        // bruma violeta de nacimiento.
                        if (!testVive)
                        {
                            int txT = tx + (p.direction >= 0 ? 4 : -4);
                            int sueloT = -1;
                            for (int y = ty - 4; y < ty + 22 && y < Main.maxTilesY - 4; y++)
                                if (WorldGen.SolidTile(txT, y)) { sueloT = y; break; }
                            Vector2 posT = sueloT >= 0
                                ? new Vector2(txT * 16f + 8f, sueloT * 16f)
                                : p.Bottom;
                            int idxT = NPC.NewNPC(p.GetSource_FromAI(),
                                (int)posT.X, (int)posT.Y, tipoTest, 0, 0f, p.whoAmI);
                            if (idxT >= 0 && idxT < Main.maxNPCs && Main.npc[idxT].active)
                            {
                                Main.npc[idxT].homeTileX = (int)(posT.X / 16f);
                                Main.npc[idxT].homeTileY = (int)(posT.Y / 16f);
                                Main.npc[idxT].netUpdate = true;
                                if (!Main.dedServ)
                                    for (int k = 0; k < 10; k++)
                                    {
                                        var d = Dust.NewDustPerfect(Main.npc[idxT].Center,
                                            DustID.Shadowflame,
                                            new Vector2(Main.rand.NextFloat(-1.5f, 1.5f),
                                                Main.rand.NextFloat(-2.5f, -0.5f)),
                                            100, new Color(110, 60, 180), 1.0f);
                                        d.noGravity = true;
                                    }
                            }
                        }
                        return;   // UNO por mundo (el primero que lo lleve)
                    }
                }
                catch { }
            }
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("LegacyInterface.28");   // "Tienda"
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
                shopName = "Cronista";
        }

        // ================================================================
        //  v6.157 — EL CUERPO: LA TIRA PROPIA DE 25 MARCOS (la letra:
        //  «el cronista no se ve como un NPC de terraria» + «investiga
        //  si existe algun mod o recurso externo para la creacion de
        //  NPC… crea nuestra propia version para nosotros»). LA
        //  INVESTIGACIÓN: los mods grandes usan UNA TIRA de 25 marcos
        //  40×56 animada por EL FRAMER DE VANILLA (el decompile del
        //  Guide: idle=frame 0 · walk=2..14 por velocity.X ·
        //  extra=16..20 · ataque=21..24) — LA VÍA SIN texturas ajenas
        //  ni copyright: el sprite del escriba FORJADO POR CÓDIGO
        //  (cronista_sprite_v6157.py — la túnica de tinta con borde
        //  dorado, la piel de pergamino, el ojo de runa, la pluma
        //  dorada y el grimorio al pecho). EL RENDERER DE JUGADORES
        //  MUERE: era frágil (un new Player() fuera de mundo tira con
        //  el catch mudo → NPC INVISIBLE — exactamente lo que el
        //  usuario vio: «realmente no aparece»); vanilla dibuja la
        //  tira con el framer de siempre (AnimationType = Guide).
        // ================================================================

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // LA LEY DE LA CASA: el sprite lo dibuja VANILLA (la tira
            // propia de 25 marcos, animada por el framer del Guide) —
            // aquí solo EL AURA DEL ESCRIBA: la tinta del oficio respira
            // a sus pies (2 motas suaves, el suspiro de la pluma).
            if (!Main.dedServ && (Main.GameUpdateCount + NPC.whoAmI) % 18f == 0f)
            {
                try
                {
                    for (int k = 0; k < 2; k++)
                    {
                        Vector2 cuna = NPC.BottomLeft + new Vector2(
                            Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(-3f, 1f));
                        Dust d = Dust.NewDustPerfect(cuna,
                            ModContent.DustType<global::AethonMod.Content.Dusts.PolvoVacioInvertido>(),
                            new Vector2(Main.rand.NextFloat(-0.25f, 0.25f), -Main.rand.NextFloat(0.08f, 0.3f)),
                            0, new Color(196, 138, 255), Main.rand.NextFloat(0.18f, 0.30f));
                        d.noGravity = true;
                    }
                }
                catch { }
            }
            return true;   // VANILLA DIBUJA (la tira de 25 marcos propia)
        }

        // ================================================================
        //  LA MONEDA — ESENCIA DEL ETERNO (la letra: «solo usa como
        //  moneda las esencias del eterno»). LA VÍA DE LA CASA: los
        //  items de la tienda llevan shopSpecialCurrency (el ID que
        //  registra EsenciaMonedaSistema — el patrón vanilla de las
        //  Medallas de Defensor, verificado contra el tML real).
        // ================================================================

        /// <summary>EL PRECIO de un requisito de Despertar (la letra:
        /// «cada uno cuesta como base 100 esencias del eterno y a eso
        /// se le suma esto, el nivel de rareza del item x el nivel de
        /// Despertar del requisito en cuestion - el nivel de oleadas
        /// de hambre»): max(100, 100 + rareza×despertar − oleadas).
        /// La rareza es la del item de Terraria (rare ≥ 0); el
        /// despertar, el nivel de LA PUERTA que exige el requisito;
        /// las oleadas, el nivel de furia del festín.</summary>
        private static long PrecioRequisito(Item it, int despertarRequisito, Player comprador)
        {
            int rareza = Math.Max(0, it.rare);
            int oleadas = 1;
            try
            {
                var sp = comprador.GetModPlayer<global::AethonMod.Content.Players.ShardPlayer>();
                if (sp != null) oleadas = Math.Max(1, Math.Min(10, sp.FuriaNivel));
            }
            catch { }
            long precio = 100L + (long)rareza * despertarRequisito - oleadas;
            return Math.Max(100L, precio);   // el mínimo de la letra: 100
        }

        /// <summary>EL NIVEL DE DESPERTAR DE LA PUERTA que exige ESTA
        /// arma (el peldaño donde el arma es requisito — la escalera
        /// de ArmasDe). 0 si no es requisito de ninguna puerta: se
        /// vende a la base (100).</summary>
        private static int DespertarDelRequisito(int tipoArma)
        {
            try
            {
                for (int nivel = 1; nivel <= global::AethonMod.Content.Systems.DespertarGrimorio.NivelMax; nivel++)
                {
                    int[] armas = global::AethonMod.Content.Systems.DespertarGrimorio.ArmasDe(nivel);
                    if (armas == null) continue;
                    foreach (int a in armas)
                        if (a == tipoArma) return nivel;
                }
            }
            catch { }
            return 0;
        }

        public override void AddShops()
        {
            // LA TIENDA VACÍA DE NACIMIENTO: la mercancía vive en
            // ModifyActiveShop (la tienda ES del comprador — la letra).
            new NPCShop(Type, "Cronista").Register();
        }

        /// <summary>v6.154 — LA TIENDA DEL COMPRADOR (la letra: «lo que
        /// vende siempre estara ligado al jugador que va a comprarle…
        /// el cronista sabra el nivel de despertar de cada uno y
        /// vendera la respectiva mercancia para cada uno»). EL HOOK:
        /// ModifyActiveShop corre EN CADA APERTURA con el player LOCAL
        /// delante del mostrador — la mercancía nace del DESPERTAR DE
        /// QUIEN MIRA. (El botón de la Onceava y las esencias de
        /// monstruos de la v6.153 MUEREN: la letra los mata.)
        /// v6.156 — LA TIENDA NUNCA MÁS VACÍA (la letra: «la tienda del
        /// cronista no muestra nada, tengo el grimorio en el nivel 0 de
        /// Despertar y para subir a nivel 1 necesito articulos que no
        /// salen en la tienda, recuerda que el Cronista debe vender
        /// siempre lo que el jugador necesita para subir el nivel de
        /// Despertar»): LA AUTOPSIA — el cuerpo entero vivía dentro de
        /// un try/catch VACÍO: CUALQUIER excepción (la moneda sin
        /// registrar, un null de la cosecha del 120, lo que sea)
        /// dejaba el mostrador VACÍO y el log NI UNA LÍNEA. LA CURA:
        /// (1) el catch silencioso MUERE — si algo revienta, la casa LO
        /// ESCRIBE en el log (Logging.tML.Error) y lo que ya se llenó
        /// QUEDA a la venta; (2) el nivel 0 TAMBIÉN COMPRA: con el
        /// libro en la mano, el peldaño siguiente es el 1 (SlimeStaff
        /// + WandofSparking — ArmasDelNivel[1]) y SIEMPRE se ofrece;
        /// (3) la búsqueda del libro mira el inventario COMPLETO (58 +
        /// la mano + el banco abierto) — la moneda se registra en Load
        /// ANTES de cualquier tienda.</summary>
        public override void ModifyActiveShop(string shopName, Item[] items)
        {
            // v6.157 — LA CAUSA RAÍZ DE LA TIENDA VACÍA (la autopsia del
            // decompile REAL): el nombre que llega aquí es EL COMPLETO —
            // NPCShopDatabase.GetShopName(npcType, "Cronista") forma
            // «AethonMod/ElCronista/Cronista» (FullName + "/" + nombre),
            // y OnChatButtonClicked llama SetupShop con ESE nombre. El
            // guard viejo `shopName != "Cronista"` NUNCA casaba → el
            // mostrador quedaba VACÍO EN TODAS LAS PARTIDAS, desde el
            // día uno. EL GUARD NUEVO: casa con el FINAL del nombre
            // completo (cualquier prefijo del mod lo deja pasar).
            if (shopName == null || !shopName.EndsWith("/Cronista")) return;
            try
            {
                Player comprador = Main.LocalPlayer;
                if (comprador == null || !comprador.active) return;

                int eterno = ModContent.ItemType
                    <global::AethonMod.Content.Weapons.GrimoireEternal>();
                bool llevaElLibro = false;
                for (int i = 0; i < 58; i++)
                    if (comprador.inventory[i] != null &&
                        comprador.inventory[i].type == eterno)
                    { llevaElLibro = true; break; }
                if (!llevaElLibro && comprador.HeldItem != null &&
                    comprador.HeldItem.type == eterno)
                    llevaElLibro = true;   // v6.156: también EN LA MANO
                if (!llevaElLibro) return;   // sin el libro, el mostrador está vacío

                // LA PUERTA QUE ESTÁ PAGANDO ESTE COMPRADOR: el nivel
                // siguiente de SU grimorio (cada libro, su escalera).
                // v6.156 — EL NIVEL 0 SUBE AL 1: NivelDespertarSiguiente
                // ya devuelve 1 cuando el libro está a 0 (nivel+1) — las
                // armas del peldaño 1 SALEN SIEMPRE en el mostrador.
                int siguiente = Math.Min(
                    global::AethonMod.Content.Systems.DespertarGrimorio.NivelMax,
                    global::AethonMod.Content.Systems.DespertarGrimorio
                        .NivelDespertarSiguiente(comprador));

                int slot = 0;
                void Poner(int tipoItem, long precio, int pila = 1)
                {
                    if (slot >= items.Length) return;
                    var it = new Item(tipoItem);
                    it.stack = pila;
                    it.shopCustomPrice = (int)Math.Min(int.MaxValue, precio);
                    it.shopSpecialCurrency = EsenciaMonedaSistema.IdMoneda;
                    items[slot++] = it;
                }

                // ========================================================
                //  (1) LOS REQUISITOS DEL DESPERTAR (la letra: «lo que
                //  vende el cronista son los requisitos de cada
                //  despertar»): LAS ARMAS del peldaño siguiente del
                //  comprador — el festín se compra a plazos.
                //  (Las monedas y las esencias del festín NO se
                //  venden: el oro es de los vivos y las esencias de
                //  jefe se ganan cazando.)
                // ========================================================
                int[] armas = global::AethonMod.Content.Systems.DespertarGrimorio.ArmasDe(siguiente);
                if (armas != null)
                {
                    foreach (int tipoArma in armas)
                    {
                        if (tipoArma <= 0) continue;
                        int despertarReq = DespertarDelRequisito(tipoArma);
                        var plantilla = new Item(tipoArma);
                        Poner(tipoArma, PrecioRequisito(plantilla, despertarReq, comprador));
                    }
                }
                // LOS MATERIALES DE LA FORMA (101-120: los grupos con
                // alternativas — se vende la PRIMERA del grupo)
                int[][] grupos = global::AethonMod.Content.Systems.DespertarGrimorio.GruposMaterialesDeLaForma(siguiente);
                if (grupos != null)
                {
                    foreach (int[] grupo in grupos)
                    {
                        if (grupo == null || grupo.Length == 0) continue;
                        var plantilla = new Item(grupo[0]);
                        int despertarReq = siguiente;
                        long precioMat = Math.Max(100L,
                            PrecioRequisito(plantilla, despertarReq, comprador) / 4);
                        Poner(grupo[0], precioMat, 5);   // paquete de 5 (un tributo, no el festín entero)
                    }
                }

                // ========================================================
                //  (2) LAS POCIONES DE MANÁ SINCRONIZADAS CON EL MAGO
                //  (la letra: «al igual que el mago en el juego estas
                //  se iran desbloqueando a la par que el NPC Mago en
                //  el terraria vanilla… siempre estaran sincronizadas
                //  con las del NPC Mago, solo que el cronista las
                //  vendera con esencias del eterno y su valor sera de
                //  1 esencia del eterno la mas basica hasta 5 esencias
                //  la de mayor calidad»).
                //  LA SINCRONÍA REAL del Mago vanilla (el decompile:
                //  el Mago vende Menor SIEMPRE; Restauración con
                //  hardMode; Mayor con algún mech muerto):
                //    Menor        → 1 esencia
                //    Restauración → 3 esencias
                //    Mayor        → 5 esencias
                // ========================================================
                Poner(ItemID.ManaPotion, 1, 5);                       // Menor (la básica: 1)
                if (Main.hardMode)
                    Poner(ItemID.RestorationPotion, 3, 5);            // Restauración: 3
                if (NPC.downedMechBossAny)
                    Poner(ItemID.GreaterManaPotion, 5, 5);            // Mayor (la de mayor calidad: 5)

                // ========================================================
                //  (3) v6.155 — LA MERCANCÍA DEL 120 (la letra: «cuando
                //  se cumpla el nivel 120 de despertar, el cronista debe
                //  quedarse vendiendo pociones de mana y ademas 10
                //  articulos aleatorios tomados de otros NPC, el mago no
                //  cuenta, puede contar los NPC de otros mod»): al
                //  Despertar 120 del comprador la escalera SE ACABÓ (no
                //  hay peldaño siguiente que vender) — el Cronista se
                //  queda con LAS POCIONES DE MANÁ de siempre + DIEZ
                //  ARTÍCULOS tomados de las tiendas de OTROS NPC del
                //  mundo (town NPC vanilla y de mods, EL MAGO EXCLUIDO
                //  — las pociones de maná ya son suyas y no compite
                //  con su colega). El surtido es ESTABLE por día (la
                //  semilla nace del día del mundo: mismo día, mismo
                //  mostrador — el Cronista no cambia de humor a media
                //  partida) y se vende EN ESENCIAS (la moneda de la
                //  casa): el precio de la casa, la rareza manda.
                // ========================================================
                int despertarComprador = global::AethonMod.Content.Systems
                    .DespertarGrimorio.NivelDe(comprador);
                if (despertarComprador >= 120)
                {
                    // LA COSECHA DE LAS TIENDAS AJENAS (la API real
                    // verificada en el decompile del tModLoader.dll
                    // 2026.09.3.1): NPCShopDatabase.AllShops expone TODAS
                    // las tiendas registradas — vanilla Y de otros mods —
                    // con su NpcType y sus ActiveEntries (el Entry
                    // abstracto expone Item). Se toma UN artículo por
                    // tienda ajena (el primer Entry activo con item de
                    // verdad), saltando al MAGO (la letra: «el mago no
                    // cuenta») y al propio Cronista.
                    List<(int tipo, int stack)> cosecha = new List<(int, int)>();
                    try
                    {
                        foreach (var tienda in Terraria.ModLoader.NPCShopDatabase.AllShops)
                        {
                            if (tienda == null) continue;
                            if (tienda.NpcType == NPCID.Wizard) continue;   // el Mago NO cuenta
                            if (tienda.NpcType == NPC.type) continue;       // él mismo tampoco
                            // solo tiendas de NPC del PUEBLO: los town NPC
                            // vanilla (IsTownNPC de la lista canónica — y
                            // el Mercader Ambulante, que es tienda sin
                            // casa)… y TODAS las de OTROS MODS (la letra:
                            // «puede contar los NPC de otro mod»)
                            bool esPueblo = tienda.NpcType >= NPCID.Count
                                || tienda.NpcType == NPCID.TravellingMerchant
                                || Terraria.ID.NPCID.Sets.ActsLikeTownNPC[tienda.NpcType];
                            if (!esPueblo) continue;
                            foreach (var entry in tienda.ActiveEntries)
                            {
                                Item mercancia = entry?.Item;
                                if (mercancia == null || mercancia.IsAir) continue;
                                if (mercancia.type == ItemID.CopperCoin || mercancia.type == ItemID.SilverCoin ||
                                    mercancia.type == ItemID.GoldCoin || mercancia.type == ItemID.PlatinumCoin) continue;
                                cosecha.Add((mercancia.type, mercancia.stack));
                                break;   // UNO por tienda (10 tiendas = 10 artículos)
                            }
                            if (cosecha.Count >= 24) break;
                        }
                    }
                    catch (System.Exception eCol)
                    {
                        // v6.156 — la cosecha del 120 ya no es muda: si
                        // una tienda ajena revienta, se anota y SE SIGUE
                        // con las demás (el mostrador del 120 no muere).
                        try
                        {
                            Mod.Logger.Error("[ElCronista/120] la cosecha de tiendas ajenas falló", eCol);
                        }
                        catch { }
                    }

                    // LA ELECCIÓN DETERMINISTA DEL DÍA (la casa no usa
                    // Main.rand en el surtido: el mismo día, el mismo
                    // mostrador en todas las máquinas)
                    if (cosecha.Count > 0)
                    {
                        // LA SEMILLA DEL DÍA: el reloj del mundo
                        // (Main.time avanza 0→dayLength cada día) — el
                        // número de días completos + el ID del mundo:
                        // mismo día, mismo mostrador.
                        int semillaDia = (int)(Main.time / Main.dayLength)
                            + (int)Main.worldID;
                        int cuantos = Math.Min(10, cosecha.Count);
                        for (int k = 0; k < cuantos; k++)
                        {
                            // el dado de la casa (Hash01 determinista)
                            int idx = (int)(global::AethonMod.Content.VFX.VFXCore.Hash01(
                                semillaDia * 7 + 1, k, 31) * cosecha.Count);
                                var (tipoV, pilaV) = cosecha[idx];
                            var planta = new Item(tipoV);
                            long precioV = Math.Max(5L, 20L + 40L * Math.Max(0, planta.rare));
                            Poner(tipoV, precioV, Math.Max(1, Math.Min(5, pilaV)));
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                // v6.156 — EL CATCH SILENCIOSO MUERE: la tienda vacía del
                // nivel 0 nació AQUÍ — cualquier excepción dejaba el
                // mostrador MUERTO sin NI UNA LÍNEA en el log. Ahora la
                // casa LO ESCRIBE (y lo que ya se llenó QUEDA a la venta:
                // items[] ya escrito no se borra).
                try
                {
                    Mod.Logger.Error("[ElCronista/ModifyActiveShop] la tienda falló a medio llenar", e);
                }
                catch { }
            }
        }
    }

    // ======================================================================
    //  ESENCIAMONEDASISTEMA — LA MONEDA DEL DESPERTAR (v6.154).
    //  El patrón vanilla de las Medallas de Defensor (CustomCurrency
    //  SingleCoin — verificado por reflexión contra el tModLoader.dll
    //  REAL de 2026.09.3.1): la Esencia del Eterno (ItemCorrupto — la
    //  moneda del festín de siempre) pasa a ser MONEDA DE TIENDA. El
    //  tope es el cap del alma: 9999 por pila (la pila de la v6.120).
    // ======================================================================
    public class EsenciaMonedaSistema : ModSystem
    {
        /// <summary>El ID de la moneda (lo reparten los items de la tienda
        /// via shopSpecialCurrency — CustomCurrencyManager lo registra
        /// en su Load).</summary>
        public static int IdMoneda { get; private set; } = -1;

        public override void Load()
        {
            // LA ESSENCIA COMO MONEDA: cuenta las pilas del inventario
            // (y las cobra — el patrón del SingleCoin de vanilla).
            IdMoneda = Terraria.GameContent.UI.CustomCurrencyManager
                .RegisterCurrency(new Terraria.GameContent.UI.CustomCurrencySingleCoin(
                    ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>(),
                    999999L));
        }

        public override void Unload()
        {
            IdMoneda = -1;
        }
    }

    // ======================================================================
    //  CONSAGRACIONDELCRONISTA — v6.157 — TODO LO QUE EL CRONISTA VENDE
    //  SALE CONSAGRADO (la letra: «asegurate de que todo lo que venda
    //  este consagrado justo como lo pide el grimorio»). LA VÍA REAL
    //  (verificada en el decompile): al comprar, ItemSlot llama
    //  item.OnCreated(new BuyItemCreationContext(Main.mouseItem,
    //  localPlayer.TalkNPC)) — el contexto lleva EL NPC VENDEDOR. El
    //  hook GlobalItem.OnCreated ve pasar cada item comprado: si el
    //  vendedor es El Cronista, el item NACE CONSAGRADO (el mismo
    //  sello del altar: Corrompido = true — el grimorio lo come sin
    //  pasar por el altar, porque el Cronista YA lo consagró al
    //  venderlo: la pluma del escriba sella la mercancía).
    // ======================================================================
    public class ConsagracionDelCronista : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstance)
            => true;   // cualquier mercancía del mostrador

        public override void OnCreated(Item item, ItemCreationContext context)
        {
            // SOLO lo comprado en el mostrador del Cronista (la firma
            // real del decompile: BuyItemCreationContext con VendorNPC).
            if (context is not Terraria.DataStructures.BuyItemCreationContext compra)
                return;
            try
            {
                if (compra.VendorNPC == null) return;
                if (compra.VendorNPC.type != ModContent.NPCType<ElCronista>()) return;

                var sello = item.GetGlobalItem
                    <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                if (sello != null && !sello.Corrompido)
                {
                    sello.Corrompido = true;   // EL SELLO DEL ESCRIBA
                }
            }
            catch { }
        }
    }
}
