using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

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
        public override void SetStaticDefaults()
        {
            // LA ANIMACIÓN PROPIA (la tira forjada: 25 marcos del
            // town NPC — idle, caminar, atacar y EL REPOSO DEL ESCRIBA)
            Main.npcFrameCount[Type] = 25;
            NPCID.Sets.ExtraFramesCount[Type] = 9;
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
        /// monstruos de la v6.153 MUEREN: la letra los mata.)</summary>
        public override void ModifyActiveShop(string shopName, Item[] items)
        {
            if (shopName != "Cronista") return;
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
                if (!llevaElLibro) return;   // sin el libro, el mostrador está vacío

                // LA PUERTA QUE ESTÁ PAGANDO ESTE COMPRADOR: el nivel
                // siguiente de SU grimorio (cada libro, su escalera).
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
            }
            catch { }
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
        /// <summary>El ID de la moneda (lo reparten los items de la
        /// tienda via shopSpecialCurrency — CustomCurrencyManager lo
        /// registra en su Load).</summary>
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
}
