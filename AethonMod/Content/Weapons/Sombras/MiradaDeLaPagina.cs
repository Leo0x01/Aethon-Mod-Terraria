using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Weapons.Sombras
{
    /// <summary>
    /// MIRADADELAPAGINA — v6.50.66 — LA MIRADA DE LA PÁGINA (hermana 2).
    ///
    /// v6.50.66: el usuario jubiló a las 3 armas viejas y pidió COPIAR
    /// a La Sombra de la Página con «más personalidad, más efectos,
    /// más sabor, más alma, más de todo».
    ///
    /// LA MIRADA es la página que TE MIRA: una pared de catorce ojos
    /// que parpadean en oleadas, líneas de mirada hasta la presa, el
    /// OJO COLOSAL que se abre al morder, lágrimas de tinta y la
    /// viñeta de la sentencia. Ocupa el puesto de El Tajo de las
    /// Sombras (la arma 2 original). 100% código, ni un sprite.
    /// </summary>
    public class MiradaDeLaPagina : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 165;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 44;
            Item.useAnimation = 44;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<MiradaPaginaProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool CanUseItem(Player player)
        {
            // UNA sola mirada viva por portador (el festín es un arte individual)
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<MiradaPaginaProjectile>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            NPC presa = SombraDeLaPagina.BuscarPresa(player.Center, true)
                     ?? SombraDeLaPagina.BuscarPresa(player.Center, false);
            Vector2 rumbo = presa != null
                ? (presa.Center - player.MountedCenter).SafeNormalize(velocity.LengthSquared() > 0.1f ? velocity : Vector2.UnitX)
                : velocity.LengthSquared() > 0.1f ? velocity.SafeNormalize(Vector2.UnitX) : Vector2.UnitX;

            Projectile.NewProjectile(source, player.MountedCenter, rumbo * 14f, type, damage, knockback,
                player.whoAmI, presa != null ? presa.whoAmI + 1 : 0, 0);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
