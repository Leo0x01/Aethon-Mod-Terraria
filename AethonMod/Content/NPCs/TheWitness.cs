using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.NPCs
{
    /// <summary>
    /// El Testigo — NPC cósmico errante.
    /// No hostil por defecto: narra lore según el nivel del fragmento del jugador,
    /// y vende Fragmentos de Resonancia + Runas de Memoria.
    /// Si es atacado: superboss opcional de 3 fases (TODO: implementar combate).
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
            NPC.damage = 0; // No hostil.
            NPC.defense = 999; // Esencialmente invulnerable sin ser atacado.
            NPC.lifeMax = 200_000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.friendly = true;
            NPC.townNPC = false;
            NPC.npcSlots = 1f;
            NPC.aiStyle = 0; // Sin movimiento.
            NPC.immortal = true; // No puede morir normalmente.
        }

        public override bool? CanBeHitByItem(Player player, Item item)
        {
            // Permite ser atacado (activa superboss opcional — TODO: implementar fases).
            return true;
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            return true;
        }

        public override string GetChat()
        {
            var sp = Main.LocalPlayer.GetModPlayer<Players.ShardPlayer>();
            int level = sp?.ShardLevel ?? 0;

            // Diálogo de lore escalado por nivel del fragmento.
            return level switch
            {
                0 => "Te he observado. Aún no has reclamado el Fragmento Génesis. Busca el Sagrario Hueco bajo tierra.",
                < 25 => $"Tu fragmento brilla con nivel {level}. Aún es débil. Sigue combatiendo.",
                < 50 => $"Nivel {level}... El fragmento empieza a recordar su origen. La Lluvia de Luz Estelar se acerca.",
                < 75 => $"Nivel {level}. El Sagrario Hueco responde a tu poder. Rifts dimensionales acechan.",
                < 100 => $"Nivel {level}. Aethon se agita en sueños. Los Ecos de portadores anteriores vendrán por ti.",
                < 150 => $"Nivel {level}. Aethon está a punto de despertar. Prepárate para el reconocimiento.",
                _ => $"Nivel {level}. Aethon te espera. Ve al Sagrario Hueco y llama su nombre.",
            };
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = "Tienda";
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                shopName = "Testigo";
            }
        }

        public override void SetupShop(string shop, ref bool[] shopLocked, params object[] args)
        {
            // Vende Fragmentos de Resonancia (caros).
            // TODO: añadir Runas de Memoria específicas.
            var sp = Main.LocalPlayer.GetModPlayer<Players.ShardPlayer>();
            int level = sp?.ShardLevel ?? 0;

            // Fragmentos de Resonancia: disponible desde nivel 50.
            if (level >= 50)
            {
                Main.instance.StoreItems.Add(ModContent.ItemType<Items.ResonanceShard>(), 10);
            }
        }
    }
}
