using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Definitivas;

namespace AethonMod.Content.Weapons.Definitivas
{
    // ======================================================================
    //  v6.56.9 — RAYO DE PRUEBA B — EL TITÁN (la receta 1 de la pila de
    //  capas): TODO al máximo — bruma ×3.1 al 14%, glow ×2.1 al 28%,
    //  núcleo ×0.42 al 95% y 8 soles conducto. El haz más GORDO y
    //  dramático de la familia. Ver RayoPruebaA para el contrato.
    // ======================================================================
    public class RayoPruebaB : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RayosPruebaProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 10;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(-0.3f);
        }

        public override bool CanUseItem(Player player)
        {
            int tipo = ModContent.ProjectileType<RayosPruebaProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI && p.type == tipo)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA RECETA B (1): EL TITÁN.
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type,
                damage, knockback, player.whoAmI, 0f, 1f, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
