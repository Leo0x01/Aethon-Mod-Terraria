using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Definitivas;

namespace AethonMod.Content.Weapons.Definitivas
{
    // ======================================================================
    //  v6.56.9 — RAYO DE PRUEBA C — EL SUEÑO (la receta 2 de la pila de
    //  capas): el DESENFOQUE MÁXIMO — bruma ×3.6 al 20%, DOBLE glow
    //  (×2.5 al 24% + ×1.9 al 12%), núcleo suave ×0.28 al 65% y 6 soles
    //  conducto. El conducto onírico, todo fundido. Ver RayoPruebaA para
    //  el contrato.
    // ======================================================================
    public class RayoPruebaC : ModItem
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
            // LA RECETA C (2): EL SUEÑO.
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type,
                damage, knockback, player.whoAmI, 0f, 2f, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
