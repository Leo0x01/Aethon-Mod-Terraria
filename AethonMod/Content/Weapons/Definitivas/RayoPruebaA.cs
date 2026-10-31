using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Definitivas;

namespace AethonMod.Content.Weapons.Definitivas
{
    // ======================================================================
    //  v6.56.9 — LAS COPIAS DEL RC2 (la letra: «copia ese rc2 en varias
    //  armas con los cambios diseñados asi no dañamos el codigo original
    //  y cambiamos las copias hasta tener la correcta»). TRES armas de
    //  prueba, cada una disparando la MISMA pila de capas con SU receta:
    //    · RAYO DE PRUEBA A — LA EQUILIBRADA: bruma ×2.6 al 9%, glow
    //      ×1.75 al 20%, núcleo ×0.34 al 80% y 5 soles conducto. El
    //      diseño base, elegante.
    //    · RAYO DE PRUEBA B — EL TITÁN: todo al máximo (bruma ×3.1 al
    //      14%, glow ×2.1 al 28%, núcleo ×0.42 al 95%, 8 soles). El haz
    //      más GORDO y dramático.
    //    · RAYO DE PRUEBA C — EL SUEÑO: el desenfoque máximo (bruma
    //      ×3.6 al 20%, DOBLE glow ×2.5+×1.9, núcleo suave ×0.28 al
    //      65%, 6 soles). El conducto onírico.
    //  La receta viaja en ai[1] del proyectil. El RC2 ORIGINAL queda
    //  INTACTO — cuando el usuario elija la ganadora, la casa la copia.
    // ======================================================================
    public class RayoPruebaA : ModItem
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
            // UN solo rayo de prueba a la vez (cualquier receta).
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
            // LA RECETA A (0): EL DUEÑO en ai[2], la receta en ai[1].
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type,
                damage, knockback, player.whoAmI, 0f, 0f, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
