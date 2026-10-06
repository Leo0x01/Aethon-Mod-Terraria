using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Weapons.Sombras
{
    /// <summary>
    /// AZOTEDELAPAGINA — v6.50.69 — ARMA NUEVA 1: EL AZOTE DE LA PÁGINA.
    ///
    /// «Esas tres armas nuevas se ven y funcionan horrible» — la Pluma
    /// murió. La familia vuelve al ADN que el usuario AMA (el tentáculo
    /// de carne de La Sombra) con OFICIOS distintos. EL AZOTE es el
    /// látigo: LARGO, fino, CRUJA atravesando TODO en línea — el alcance
    /// y la rabia de la página. A 1 de vida de un jefe: LA DEVORACIÓN
    /// (el festín estilo 8, EL LATIGAZO TRIPLE).
    ///
    /// Arma de prueba: llega en LA BOLSA DE LAS SOMBRAS (receta de 5
    /// madera como alternativa — protocolo v6.14.2). 100% código.
    /// </summary>
    public class AzoteDeLaPagina : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 90;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<AzotePaginaProjectile>();
            Item.shootSpeed = 30f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL RUMBO: al cursor (la velocidad solo trae la dirección —
            // el azote mide su alcance él mismo)
            float rumbo = velocity.LengthSquared() > 0.1f
                ? velocity.ToRotation()
                : (Main.MouseWorld - player.MountedCenter).ToRotation();
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type,
                damage, knockback, player.whoAmI, rumbo, 0f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
