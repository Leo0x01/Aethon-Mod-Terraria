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
    //  EL CRONISTA — v6.152 — EL COMERCIANTE DEL GRIMORIO (la letra del
    //  usuario: «quiero agregar un NPC no hostil como comerciante…
    //  investiga como los mods populares agregan NPC no hostiles como
    //  por ejemplo thorium… y luego tu mismo añade uno que siempre
    //  aparezca cuando el jugador posea el grimorio y mientras el
    //  jugador tenga el grimorio este NPC estara ahi»).
    //
    //  LA INVESTIGACIÓN (la casa — la guía de facto de tML + el patrón
    //  de los town NPCs de BossesAsNPCs/Thorium): un ModNPC con
    //  [AutoloadHead] + NPC.townNPC=true es un ALDEANO de verdad
    //  (vivienda, tienda, felicidad) — el gate de llegada es
    //  CanTownNPCSpawn (el patrón del Cobbler de Thorium: «un jugador
    //  lleva el ítem») y la PARTIDA se controla en PreAI (si NADIE
    //  tiene el grimorio, el Cronista SE VA — «mientras el jugador
    //  tenga el grimorio este NPC estara ahi»).
    //
    //  QUIÉN ES: EL CRONISTA — el escriba que sobrevivió a la primer
    //  devoración. Vende LOS MATERIALES DEL DESPERTAR (los que el
    //  festín consume: esencias menores, monedas de paso, el
    //  equipamiento del portador novato). No vende PODER: vende lo
    //  que el libro COME.
    // ======================================================================
    [AutoloadHead]
    public class ElCronista : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // LA ANIMACIÓN prestada del Guide (el spritesheet 25 frames
            // de vanilla — el patrón de los town NPCs de la casa)
            Main.npcFrameCount[Type] = 25;
            NPCID.Sets.ExtraFramesCount[Type] = 9;
            NPCID.Sets.AttackFrameCount[Type] = 4;
            NPCID.Sets.DangerDetectRange[Type] = 700;
            NPCID.Sets.AttackType[Type] = 0;          // lanzamiento
            NPCID.Sets.AttackTime[Type] = 90;
            NPCID.Sets.AttackAverageChance[Type] = 30;
            NPCID.Sets.NoTownNPCHappiness[Type] = true;   // precio fijo (la casa no cobra cariño)

            // EL BESTIARIO mínimo (la entrada del aldeano)
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
            NPC.knockBackResist = 0.5f;
            AnimationType = NPCID.Guide;
        }

        // ================================================================
        //  LA LLEGADA (la letra: «que siempre aparezca cuando el jugador
        //  posea el grimorio»): el patrón del Cobbler de Thorium — un
        //  jugador CUALQUIERA con el Grimorio del Eterno en su
        //  inventario LO LLAMA. Sin el libro no llega.
        // ================================================================
        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            try
            {
                int eterno = ModContent.ItemType
                    <global::AethonMod.Content.Weapons.GrimoireEternal>();
                foreach (Player p in Main.ActivePlayers)
                {
                    for (int i = 0; i < 58; i++)
                        if (p.inventory[i] != null && p.inventory[i].type == eterno)
                            return true;
                }
            }
            catch { }
            return false;
        }

        // ================================================================
        //  LA PARTIDA (la letra: «mientras el jugador tenga el grimorio
        //  este NPC estara ahi»): si NADIE lleva ya el grimorio, el
        //  Cronista SE DESPIDE Y SE VA (despawn activo — el patrón
        //  PreAI de los mods grandes: CanTownNPCSpawn solo gatea la
        //  llegada, no la partida).
        // ================================================================
        public override bool PreAI()
        {
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
                    // LA DESPEDIDA: se disuelve en tinta (el teatro de la
                    // casa — sus páginas vuelven al libro que las llama)
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

        // ================================================================
        //  EL NOMBRE — una sola identidad (el Cronista ES el Cronista)
        // ================================================================
        public override List<string> SetNPCNameList()
        {
            return new List<string> { "El Cronista" };
        }

        public override string GetChat()
        {
            // EL DIÁLOGO DEL ESCRIBA: sabes del festín, de la furia, del
            // despertar — y de las facturas del portador
            string[] lineas =
            {
                "Language.GetTextValue placeholder",
            };
            int dado = Main.rand.Next(6);
            return dado switch
            {
                0 => "(cierra el plumín) Cada página que come tu libro, yo la escribí primero. No preguntes cuánto cobré.",
                1 => "(te mira los bolsillos) El hambre sube aunque no mires la barra. Cuando la cabeza te dé vueltas, ya sabes qué pasó.",
                2 => "Las esencias no se venden solas, portador. Alguien tiene que clasificar los restos de los jefes… yo.",
                3 => "Si el libro se pone furioso, corre hacia una puerta cerrada. A él le gustan las habitaciones con una sola salida.",
                4 => "Yo vi la primer devoración. Por eso escribo con tinta violeta: la negra se me acabó aquel día.",
                _ => "¿Monedas? Acepto. El grimorio paga con algo mejor cuando está de buen humor… y con algo peor cuando no.",
            };
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

        public override void AddShops()
        {
            // ============================================================
            //  LA TIENDA DEL CRONISTA — LO QUE EL LIBRO COME (materiales
            //  del despertar y del portador novato — NADA de poder
            //  directo: la casa no vende la victoria)
            // ============================================================
            var tienda = new NPCShop(Type, "Cronista")
                // LAS ESENCIAS MENORES (la moneda del altar — 3 de oro)
                // LA ESENCIA DEL REY GELATINA (la más menuda del festín —
                // el peldaño del despertar 5 en la mesa del altar)
                .Add(new Item(ModContent.ItemType
                    <global::AethonMod.Content.Items.Esencias.EsenciaDelReyGelatina>()) { shopCustomPrice = Item.buyPrice(gold: 5) })
                // LAS ANTORCHAS (la luz del camino — el pack de 10)
                .Add(new Item(ItemID.Torch) { shopCustomPrice = Item.buyPrice(silver: 10), stack = 10 })
                // EL PAPEL DEL PORTADOR (el material de la página en
                // blanco — la casa la usa para recetas de despertar)
                .Add(new Item(ModContent.ItemType
                    <global::AethonMod.Content.Items.PaginaEnBlanco>()) { shopCustomPrice = Item.buyPrice(gold: 1) })
                // LA CURA DE SIEMPRE (poción de maná — el coste del clic
                // derecho está en el hambre, pero el portador novato
                // necesita maná para el Equinoccio)
                .Add(new Item(ItemID.ManaPotion) { shopCustomPrice = Item.buyPrice(silver: 25) })
                .Add(new Item(ItemID.HealingPotion) { shopCustomPrice = Item.buyPrice(silver: 25) })
                // LA CASA DE SIEMPRE (una antorcha del vacío para el
                // camino de vuelta del mundo interior)
                .Add(new Item(ItemID.RecallPotion) { shopCustomPrice = Item.buyPrice(silver: 15) })
                ;
            tienda.Register();
        }
    }
}
