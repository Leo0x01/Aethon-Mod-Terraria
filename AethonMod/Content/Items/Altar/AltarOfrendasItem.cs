using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Altar
{
    /// <summary>
    /// AltarOfrendasItem — v6.86 — EL ITEM DEL ALTAR DE OFRENDAS (la letra:
    /// «investiga el altar del mod de referencia... este es el tipo de
    /// altar que necesitamos para que el grimorio pueda consumir cosas
    /// que no sean las que necesita y no esten corruptas»).
    ///
    /// LA CADENA DE OBTENCIÓN (el calco de la de la referencia §2.2 —
    /// «crafteo barato sin estación»: 20 piedra + 2 estrellas fugaces +
    /// 1 gema): cualquiera puede levantar un altar — lo que cuesta es
    /// alimentarlo.
    ///
    /// v6.137 — LA LEY DEL ALTAR ÚNICO (la letra: «en el mundo solo
    /// puede haber un altar colocado en el mundo»): el ítem REBOTA si
    /// ya vive un altar — el mensaje lo dice claro. Tampoco se coloca
    /// DENTRO del mundo interior («en el mundo interior no se generen
    /// grimorios ni altares, solo estara el pilar de prisma»).
    /// </summary>
    public class AltarOfrendasItem : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Altar/AltarOfrendasItem";

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType
                <global::AethonMod.Content.Tiles.AltarOfrendasTile>());
            Item.width = 34;
            // v6.100 — la caja del ítem mide el marco nuevo (34×50, el
            // altar asentado con las velas vivas).
            Item.height = 50;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.maxStack = 99;
        }

        // ==============================================================
        //  v6.137 — LA LEY DEL ALTAR ÚNICO + LA PUREZA DEL INTERIOR
        // ==============================================================
        public override bool CanUseItem(Player player)
        {
            if (global::AethonMod.Content.Systems.SubmundoGrimorio.Activo)
            {
                Main.NewText(Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Altar.NoInterior"), new Microsoft.Xna.Framework.Color(190, 160, 220));
                return false;
            }
            if (global::AethonMod.Content.Systems.AltarUnicoSistema.HayActivo)
            {
                Main.NewText(Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Altar.YaHayUno"), new Microsoft.Xna.Framework.Color(190, 160, 220));
                return false;
            }
            return true;
        }

        public override void AddRecipes()
        {
            // LA RECETA DE ENTRADA (§2.2 de R5: barata, sin estación —
            // la misma economía que el Arsenal Altar: 25 piedra + 3
            // estrellas; la nuestra: 20 piedra + 2 estrellas + 1 diamante).
            CreateRecipe()
                .AddIngredient(ItemID.StoneBlock, 20)
                .AddIngredient(ItemID.FallenStar, 2)
                .AddIngredient(ItemID.Diamond, 1)
                .Register();
        }
    }
}
