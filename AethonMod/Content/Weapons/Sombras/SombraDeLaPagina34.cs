using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Weapons.Sombras
{
    /// <summary>
    /// SOMBRADELAPAGINA3 — v6.52.2 — ARMA: LA SOMBRA DE LA PÁGINA III —
    /// EL CORO DE IRISES (la letra: «crea un sombra de la pagina 3 y 4
    /// pero para estos solo usa el iris no el ojo completo»).
    ///
    /// El tercer tomo: la cuerda de sombra lleva SIETE IRISES flotando
    /// (SOLO el iris — sin esclerótica ni párpados), todos clavados en la
    /// presa; al morder, EL CORO CONVERGE sobre la herida. El mismo
    /// festín de siempre: caza al jefe a cualquier distancia, muerde y
    /// drena 3%/6t, a 1 HP la devoración.
    ///
    /// ARMA DE PRUEBA (la familia de la Bolsa de las Sombras).
    /// </summary>
    public class SombraDeLaPagina3 : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 175;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 44;
            Item.useAnimation = 44;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SombraPagina3Projectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<SombraPagina3Projectile>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            NPC presa = SombraDeLaPagina.BuscarPresa(player.Center, true) ??
                        SombraDeLaPagina.BuscarPresa(player.Center, false);
            Vector2 rumbo = presa != null
                ? (presa.Center - player.MountedCenter).SafeNormalize(velocity.LengthSquared() > 0.1f ? velocity : Vector2.UnitX)
                : velocity.LengthSquared() > 0.1f ? velocity.SafeNormalize(Vector2.UnitX) : Vector2.UnitX;

            Projectile.NewProjectile(source, player.MountedCenter, rumbo * 13f, type, damage, knockback,
                player.whoAmI, presa != null ? presa.whoAmI + 1 : 0, 0);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>
    /// SOMBRADELAPAGINA4 — v6.52.2 — ARMA: LA SOMBRA DE LA PÁGINA IV —
    /// EL IRIS COLOSAL (la letra: «solo usa el iris no el ojo completo»).
    ///
    /// El cuarto tomo: DOS NERVIOS ÓPTICOS de sombra cuelgan del portador
    /// y en su punta vive UN SOLO IRIS GIGANTE (el rojo) con SU PUPILA
    /// que se DILATA al morder — el iris ES la boca: filamentos de tinta
    /// corren de la presa a la pupila mientras drena 3%/6t, y a 1 HP la
    /// devoración (el jefe visto A TRAVÉS del iris).
    /// </summary>
    public class SombraDeLaPagina4 : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 190;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 46;
            Item.useAnimation = 46;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SombraPagina4Projectile>();
            Item.shootSpeed = 13f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<SombraPagina4Projectile>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            NPC presa = SombraDeLaPagina.BuscarPresa(player.Center, true) ??
                        SombraDeLaPagina.BuscarPresa(player.Center, false);
            Vector2 rumbo = presa != null
                ? (presa.Center - player.MountedCenter).SafeNormalize(velocity.LengthSquared() > 0.1f ? velocity : Vector2.UnitX)
                : velocity.LengthSquared() > 0.1f ? velocity.SafeNormalize(Vector2.UnitX) : Vector2.UnitX;

            Projectile.NewProjectile(source, player.MountedCenter, rumbo * 12f, type, damage, knockback,
                player.whoAmI, presa != null ? presa.whoAmI + 1 : 0, 0);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
