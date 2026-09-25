using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;

namespace AethonMod.Content.NPCs
{
    /// <summary>
    /// El Testigo — NPC cósmico errante. No hostil.
    ///
    /// v6.48 — EL CRONISTA Y EL MERCADER DE ALMAS:
    /// · LA CRÓNICA: cada jefe que EL LIBRO devora con su portador queda
    ///   apuntado (ShardPlayer.CronicaJefes, lo marca GlobalNPCXP). El
    ///   Testigo cuenta SU versión HUMANA de esa misma derrota — dos
    ///   narradores, un mismo hecho: el libro dice "comí" y el Testigo
    ///   dice "yo lo vi caer". Cada charla nueva revela LA SIGUIENTE
    ///   página del cuento (el cursor CronicaNarrada persiste).
    /// · LA TIENDA DE ESENCIAS: vende las almas de los SIETE guardianes
    ///   de las oleadas (EsenciaDeJefeItem — un nivel completo por alma,
    ///   10 monedas de platino cada una)… a quien SOBREVIVIÓ a la
    ///   oleada 10 de la furia (DerrotaOleada10). El botón siempre está
    ///   a la vista (el probador lo quiere a mano); el Testigo solo
    ///   abre el cajón al que aguantó el festín completo.
    ///
    /// v6.47 — LAS BURBUJAS: habla por EcoLib al acercarte (violeta,
    /// sin rugido). El chat clásico (GetChat) cuenta la crónica y el
    /// nivel del grimorio — TODO localizado en hjson (la casa no
    /// hardcodea diálogos).
    /// </summary>
    public class TheWitness : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 30;
            NPC.height = 48;
            NPC.damage = 0;
            NPC.defense = 999;
            NPC.lifeMax = 200_000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.friendly = true;
            NPC.townNPC = true; // v5.59: debe ser townNPC=true para que GetChat/SetChatButtons/OnChatButtonClicked funcionen
            NPC.npcSlots = 1f;
            NPC.aiStyle = 0;
            NPC.immortal = true;
        }

        // ==================================================================
        //  LA TIENDA DE ESENCIAS (el cajón de las almas)
        // ==================================================================

        /// <summary>
        /// EL CAJÓN DE LAS ALMAS: las SIETE esencias de los guardianes de
        /// las oleadas — cada una sube UN NIVEL COMPLETO al Grimorio
        /// (EsenciaDeJefeItem, precio 10 de platino en su SetDefaults).
        /// La CONDICIÓN de la casa: solo las compra quien derrotó la
        /// oleada 10 de la furia (ShardPlayer.DerrotaOleada10).
        /// </summary>
        public override void AddShops()
        {
            var tienda = new NPCShop(Type, "Esencias");
            var desbloqueada = new Condition("Mods.AethonMod.Conditions.TiendaEsencias",
                () => Main.LocalPlayer != null && Main.LocalPlayer.active &&
                      Main.LocalPlayer.GetModPlayer<Players.ShardPlayer>()?.DerrotaOleada10 == true);
            foreach (int esencia in Items.Esencias.EsenciaDeJefeItem.Todas())
                tienda.Add(esencia, desbloqueada);
            tienda.Register();
        }

        public override void AI()
        {
            // Flota suavemente sin moverse.
            NPC.velocity.X *= 0.8f;
            NPC.velocity.Y *= 0.8f;
            // Brillo violeta.
            Lighting.AddLight(NPC.Center, new Vector3(0.4f, 0.2f, 0.6f));

            // ================================================================
            //  v6.47 — LAS BURBUJAS DRAMÁTICAS DEL TESTIGO (EcoLib).
            //  La biblioteca de diálogos con cola es genérica: el Testigo
            //  la usa como segundo usuario (después del grimorio) para
            //  hablar POR BURBUJA DRAMÁTICA cuando te acercas — susurros
            //  violetas sin rugido, en vez de chat plano. Solo cliente.
            // ================================================================
            if (Main.netMode != NetmodeID.Server)
            {
                bool alguienCerca = false;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player pl = Main.player[i];
                    if (pl != null && pl.active && !pl.dead &&
                        pl.Distance(NPC.Center) < 300f)
                    {
                        alguienCerca = true;
                        break;
                    }
                }

                NPC.localAI[0]++;
                if (alguienCerca && NPC.localAI[0] >= 840f) // ~14 s entre burbujas
                {
                    NPC.localAI[0] = 0f;
                    NPC.localAI[1]++; // la línea rota deterministamente
                    int idx = 1 + ((int)NPC.localAI[1]) % 3;
                    EcoLib.Hablar(
                        Language.GetTextValue("Mods.AethonMod.Testigo.Ambiente" + idx),
                        new Color(196, 150, 255),
                        rugido: false, escala: 0.5f);
                }
                else if (!alguienCerca && NPC.localAI[0] < 700f)
                {
                    NPC.localAI[0] = 700f; // pre-calentado: habla pronto al acercarse
                }
            }
        }

        // ==================================================================
        //  EL CHAT — la crónica primero, el nivel después
        // ==================================================================

        /// <summary>
        /// EL NIVEL DEL GRIMORIO del jugador local (la misma lectura de
        /// siempre: el libro SOSTENIDO).
        /// </summary>
        private static int NivelDelGrimorio()
        {
            int level = 0;
            Item held = Main.LocalPlayer.HeldItem;
            if (held != null && held.type == ModContent.ItemType<Weapons.GrimoireEternal>())
            {
                try { var sl = held.GetGlobalItem<Globals.ShardLevelItem>(); if (sl != null) level = sl.Level; }
                catch { }
            }
            return level;
        }

        public override string GetChat()
        {
            // ==============================================================
            //  v6.48 — LA CRÓNICA: si hay páginas sin contar, ESTA charla
            //  es la página siguiente (el libro devoró un jefe; el Testigo
            //  lo vio caer con ojos de humano). Se consume de una en una.
            // ==============================================================
            var sp = Main.LocalPlayer.GetModPlayer<Players.ShardPlayer>();
            if (sp != null && sp.CronicaJefes != null && sp.CronicaNarrada < sp.CronicaJefes.Count)
            {
                int jefe = sp.CronicaJefes[sp.CronicaNarrada];
                string clave = EcoSistema.ClaveDeVoz(jefe);
                string linea = Language.GetTextValue("Mods.AethonMod.Testigo.Cronica." + clave);
                if (string.IsNullOrEmpty(linea) || linea.StartsWith("Mods.AethonMod"))
                    linea = Language.GetTextValue("Mods.AethonMod.Testigo.Cronica.Desconocido");
                sp.CronicaNarrada++;
                return linea;
            }

            // El escalado del grimorio, como siempre (ahora localizado).
            int level = NivelDelGrimorio();
            if (level <= 0)
                return Language.GetTextValue("Mods.AethonMod.Testigo.Chat.SinLibro");
            if (level < 25)
                return Language.GetTextValue("Mods.AethonMod.Testigo.Chat.Debil", level);
            if (level < 50)
                return Language.GetTextValue("Mods.AethonMod.Testigo.Chat.Recuerda", level);
            if (level < 75)
                return Language.GetTextValue("Mods.AethonMod.Testigo.Chat.Sagrario", level);
            if (level < 100)
                return Language.GetTextValue("Mods.AethonMod.Testigo.Chat.Ecos", level);
            if (level < 150)
                return Language.GetTextValue("Mods.AethonMod.Testigo.Chat.Despierta", level);
            return Language.GetTextValue("Mods.AethonMod.Testigo.Chat.Espera", level);
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("Mods.AethonMod.Testigo.BotonResonancia");

            // v6.48 — EL CAJÓN DE LAS ESENCIAS: siempre a la vista (el
            // probador lo quiere a mano); el Testigo decide al abrirlo.
            button2 = Language.GetTextValue("Mods.AethonMod.Testigo.BotonEsencias");
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            var sp = Main.LocalPlayer.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return;

            // === BOTÓN 2 — LA TIENDA DE ESENCIAS (el cajón de las almas) ===
            if (!firstButton)
            {
                if (!sp.DerrotaOleada10)
                {
                    // El Testigo NO abre el cajón: el festín no está pagado.
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.Testigo.EsenciasBloqueadas"),
                        new Color(150, 150, 180));
                    return;
                }
                shopName = "Esencias";
                return;
            }

            // === BOTÓN 1 — LA RESONANCIA DE SIEMPRE (nivel 50+) ===
            int level = NivelDelGrimorio();
            if (level >= 50 && Main.LocalPlayer.BuyItem(Item.buyPrice(0, 0, 10, 0)))
            {
                // v6.50.10 — FIX (regla de la casa — el drop fantasma del
                // Testigo): Item.NewItem en el CLIENTE nace local y sin
                // difusión — en MP el comprador pagaba y el fragmento
                // nunca existía (el mismo anti-patrón del Altar v6.50).
                // SP: lo crea aquí (el proceso ES la autoridad). MP: se
                // pide al server (EcoRed revalida el nivel 50 del libro y
                // lo spawn-ea él — vanilla lo difunde).
                if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
                    EcoRed.PedirResonancia();
                else
                    Item.NewItem(
                        Main.LocalPlayer.GetSource_GiftOrReward(),
                        Main.LocalPlayer.Center,
                        ModContent.ItemType<Items.ResonanceShard>());
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Testigo.ResonanciaEntregada"),
                    new Color(245, 196, 81));
            }
            else if (level < 50)
            {
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Testigo.ResonanciaBloqueada"),
                    new Color(150, 150, 180));
            }
        }
    }
}
