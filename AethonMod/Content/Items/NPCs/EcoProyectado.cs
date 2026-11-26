using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.NPCs
{
    /// <summary>
    /// ECO PROYECTADO — v6.175 — LA LLAMADA DEL CÍCLOPE (la letra: «quieres
    /// decir que no puedes recrear el Cíclope Proyectado con sus efectos»
    /// — el vídeo «Me Proyecte» de Chopilas): el portador grita
    /// «¡ME PROYECTO!» y su ESENCIA sube por el haz — del otro lado baja
    /// el Cíclope: la proyección pálida del ojo único, naciendo del haz
    /// violeta-magenta (la coreografía completa del vídeo: el grito en
    /// letras rojas, el haz, la barra de jefe rosa).
    ///
    /// PROTOCOLO de la casa: arma de prueba con receta de 5 maderas + va
    /// en la bolsa del probador.
    /// </summary>
    public class EcoProyectado : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 30;
            Item.maxStack = 20;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool CanUseItem(Player player)
        {
            // UN Cíclope a la vez (la proyección es única)
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && n.type == ModContent.NPCType
                    <global::AethonMod.Content.NPCs.CiclopeProyectado>())
                    return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return null;
            // EL NACIMIENTO: el haz aparece y de él baja el Cíclope (el
            // grito del vídeo va en el chat, la animación del haz es del NPC)
            int idx = NPC.NewNPC(player.GetSource_FromThis(),
                (int)player.Center.X, (int)player.Center.Y - 420,
                ModContent.NPCType<global::AethonMod.Content.NPCs.CiclopeProyectado>(),
                0, 0f, player.whoAmI);
            if (idx >= 0 && idx < Main.maxNPCs)
            {
                Main.npc[idx].netUpdate = true;
                if (player.whoAmI == Main.myPlayer)
                    Main.NewText(Terraria.Localization.Language.GetTextValue(
                            "Mods.AethonMod.Ciclope.Grito"),
                        new Color(255, 70, 70));
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
