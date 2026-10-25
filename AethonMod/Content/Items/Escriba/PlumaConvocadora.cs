using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Escriba
{
    /// <summary>
    /// PLUMACONVOCADORA — v6.52.0 — LA PLUMA CONVOCADORA: el ítem que
    /// invoca a EL ESCRIBA (el jefe demo de la coreografía). Úsala de
    /// noche… o de día — el Escriba no respeta horarios (jefe de PRUEBA).
    /// </summary>
    public class PlumaConvocadora : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName y Tooltip: Items.PlumaConvocadora.
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 13;
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.maxStack = 20;
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 40;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.Master;
            Item.value = Item.buyPrice(0, 0, 0, 0);
        }

        public override bool CanUseItem(Player player)
        {
            // UN Escriba a la vez (el patrón de todos los jefes).
            return !NPC.AnyNPCs(ModContent.NPCType<NPCs.Escriba.EscribaJefe>());
        }

        public override bool? UseItem(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return true;

            Vector2 pos = player.Center + new Vector2(0, -420);
            int n = NPC.NewNPC(player.GetSource_ItemUse(Item),
                (int)pos.X, (int)pos.Y,
                ModContent.NPCType<NPCs.Escriba.EscribaJefe>(),
                Target: player.whoAmI);
            if (n >= 0 && Main.netMode == NetmodeID.Server)
                Main.npc[n].netUpdate = true;
            return true;
        }

        public override void AddRecipes()
        {
            // LA VÍA DE PRUEBAS de la casa (la tradición de las armas y
            // jefes de prueba: la madera de siempre).
            CreateRecipe().AddIngredient(ItemID.Wood, 10)
                .Register();
        }
    }
}
