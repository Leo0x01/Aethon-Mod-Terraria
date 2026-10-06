using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Weapons.Sombras
{
    /// <summary>
    /// MORDIDADELAPAGINA — v6.50.69 — ARMA NUEVA 2: LA MORDIDA DE LA PÁGINA.
    ///
    /// La Hoja murió («se ven y funcionan horrible»). LA MORDIDA es el
    /// glotón de la familia: tentáculo CORTO Y GORDO con cabeza de bruma
    /// ENORME y TRES GARRAS DE HUESO que cierran de golpe — se cuelga de
    /// UNA presa y le da MORDIDAS de verdad cada 16 ticks. A un jefe le
    /// queda una mordida de vida → LA DEVORACIÓN (festín estilo 9, LA
    /// MASTICACIÓN GIGANTE). Arma de prueba: LA BOLSA DE LAS SOMBRAS
    /// (receta de 5 madera — protocolo v6.14.2). 100% código.
    /// </summary>
    public class MordidaDeLaPagina : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 235;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 42;
            Item.useAnimation = 42;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<MordidaPaginaProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool CanUseItem(Player player)
        {
            // UNA sola mordida viva por portador
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<MordidaPaginaProjectile>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA PRESA: el jefe más cercano a CUALQUIER distancia; sin
            // jefes, el enemigo más cercano en 1800 px (buscador de La
            // Sombra — el glotón cena igual)
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
