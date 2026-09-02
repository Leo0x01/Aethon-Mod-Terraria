using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.NPCs
{
    /// <summary>
    /// El Testigo — NPC cósmico errante.
    /// No hostil: narra lore según el nivel del fragmento del jugador,
    /// y vende Fragmentos de Resonancia.
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
            NPC.townNPC = false;
            NPC.npcSlots = 1f;
            NPC.aiStyle = 0;
            NPC.immortal = true;
        }

        public override void AI()
        {
            // Flota suavemente sin moverse.
            NPC.velocity.X *= 0.8f;
            NPC.velocity.Y *= 0.8f;
            // Brillo violeta.
            Lighting.AddLight(NPC.Center, new Microsoft.Xna.Framework.Vector3(0.4f, 0.2f, 0.6f));
        }

        public override string GetChat()
        {
            var sp = Main.LocalPlayer.GetModPlayer<Players.ShardPlayer>();
            int level = sp?.HeldWeaponLevel ?? 0;
            return level switch
            {
                0 => "Te he observado. Aun no has reclamado el Fragmento Genesis. Busca el Sagrario Hueco bajo tierra.",
                < 25 => $"Tu fragmento brilla con nivel {level}. Aun es debil. Sigue combatiendo.",
                < 50 => $"Nivel {level}... El fragmento empieza a recordar su origen. La Lluvia de Luz Estelar se acerca.",
                < 75 => $"Nivel {level}. El Sagrario Hueco responde a tu poder. Rifts dimensionales acechan.",
                < 100 => $"Nivel {level}. Aethon se agita en suenos. Los Ecos de portadores anteriores vendran por ti.",
                < 150 => $"Nivel {level}. Aethon esta a punto de despertar. Preparate para el reconocimiento.",
                _ => $"Nivel {level}. Aethon te espera. Ve al Sagrario Hueco y llama su nombre.",
            };
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            var sp = Main.LocalPlayer.GetModPlayer<Players.ShardPlayer>();
            int level = sp?.HeldWeaponLevel ?? 0;
            if (level >= 50)
                button = "Comprar Resonancia (10 monedas)";
            else
                button = "Hablar";
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (!firstButton) return;
            var sp = Main.LocalPlayer.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return;
            if (sp.HeldWeaponLevel >= 50 && Main.LocalPlayer.BuyItem(Item.buyPrice(0, 0, 10, 0)))
            {
                Item.NewItem(
                    Main.LocalPlayer.GetSource_GiftOrReward(),
                    Main.LocalPlayer.Center,
                    ModContent.ItemType<Items.ResonanceShard>());
                Main.NewText("El Testigo te da un Fragmento de Resonancia.", new Microsoft.Xna.Framework.Color(245, 196, 81));
            }
        }
    }
}
