using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// El Fragmento Génesis — item central del mod. Un mote de luz pura
    /// que el jugador encuentra en el altar del Sagrario Hueco.
    ///
    /// Antes de imprimirse: es un item genérico de luz.
    /// Después de imprimirse: se transforma en el arma de la rama correspondiente
    /// (Lumina / Solbrand / Grimorio) — implementado en Fase 3.
    /// </summary>
    public class GenesisShard : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName y Tooltip se cargan desde Localization.
            // Reservamos los IDs para que tModLoader los use.
        }

        public override void SetDefaults()
        {
            Item.damage = 8;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28;
            Item.height = 28;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item4;
            Item.noMelee = true;
            Item.autoReuse = false;
        }

        public override bool? UseItem(Player player)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return null;

            if (!sp.IsImprinted)
            {
                // Aún no se ha imprimido: mostrar mensaje de guía.
                Main.NewText("El Fragmento Génesis aún no tiene forma. Combate enemigos para imprprimirlo.", new Color(180, 160, 220));
                return true;
            }

            // Si ya está imprimido, usar como arma (delegar al arma específica).
            // En Fase 3, este item se reemplazará por el arma correspondiente.
            return true;
        }

        public override void UpdateInventory(Player player)
        {
            // El fragmento otorga un brillo pasivo al portador.
            player.GetModPlayer<Players.ShardPlayer>(); // asegura que el ModPlayer exista
        }

        // Receta de debug para obtener el fragmento fácilmente durante desarrollo.
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 1)
                .Register();
        }
    }
}
