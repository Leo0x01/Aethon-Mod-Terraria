using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Weapons.Sombras
{
    /// <summary>
    /// HOJADELAPAGINA — v6.50.68 — ARMA NUEVA 2: LA HOJA DE LA PÁGINA.
    ///
    /// La letra del usuario: «cambia los otros bastones por conceptos
    /// diferentes». LA HOJA reemplaza a La Mirada de la Página — el
    /// juego de palabras de la casa: «hoja» es página Y filo. CUATRO
    /// hojas-filo orbitan al portador acelerando (el molino) y salen
    /// disparadas en abanico hacia el cursor. Su festín: EL MOLINO DE
    /// FILOS (estilo 9, el círculo que se ciñe).
    ///
    /// ARMA DE PRUEBA (no toca el grimorio): se entrega en LA BOLSA DE
    /// LAS SOMBRAS del kit de pruebas; la receta de 5 madera queda como
    /// vía alternativa (protocolo v6.14.2).
    /// </summary>
    public class HojaDeLaPagina : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 105;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 42;
            Item.useAnimation = 42;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<HojaPaginaProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool CanUseItem(Player player)
        {
            // UN solo molino vivo por portador (el festín es un arte individual)
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<HojaPaginaProjectile>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // el rumbo del abanico: al cursor (la velocity del uso)
            Vector2 rumbo = velocity.LengthSquared() > 0.1f
                ? Vector2.Normalize(velocity) : Vector2.UnitX;
            Projectile.NewProjectile(source, player.MountedCenter, rumbo * 14f,
                type, damage, knockback, player.whoAmI, rumbo.ToRotation(), 0f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
