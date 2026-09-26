using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Weapons.Cosmic
{
    /// <summary>
    /// EclipsePrimordialStaff — v6.28 — EL BASTÓN DEL ECLIPSE TOTAL (rediseñado).
    ///
    /// v6.28 — LA ORDEN DEL USUARIO: "rediseña el bastón de eclipse
    /// primordial". EL NUEVO CONCEPTO es el nombre del arma: UN ECLIPSE
    /// SOLAR TOTAL — el disco de la noche se desliza sobre el sol
    /// primordial (EL CRECIENTE), el día MUERE (el mundo se apaga con el
    /// sesgo violeta de la casa), la CORONA streamerea blanca desde
    /// detrás, la CROMOSFERA arde roja al limbo con sus perlas de Baily,
    /// LAS PROMINENCIAS lamen desde la cara oculta y EL ANILLO DE
    /// DIAMANTE viaja por el borde. LOS TRES CÍRCULOS RÚNICOS contienen
    /// la noche.
    ///
    /// Física: atracción gravitacional 600 px + devora proyectiles
    /// enemigos + el aura de la corona quema. La muerte es EL RETORNO DE
    /// LA LUZ: el disco implode, la corona explota soplada y el Anillo
    /// de Einstein + la nova ×1.6 estallan mientras el día VUELVE.
    /// </summary>
    public class EclipsePrimordialStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 700;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 55; Item.useAnimation = 55;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<EclipsePrimordialProjectile>();
            Item.shootSpeed = 6f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item20;
            Item.value = Item.buyPrice(gold: 50);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/FFD080:EL ECLIPSE TOTAL — el disco de la noche devora al Sol Primordial]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/E6E0FF:El creciente mengua... y el día MUERE: corona de streamers · cromosfera roja · prominencias · el ANILLO DE DIAMANTE acelera al final]"));
            tooltips.Add(new TooltipLine(Mod, "D3",
                "[c/78788C:Tres círculos rúnicos contienen la noche · atracción 600px · la muerte es EL RETORNO DE LA LUZ (Einstein + nova ×1.6)]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
