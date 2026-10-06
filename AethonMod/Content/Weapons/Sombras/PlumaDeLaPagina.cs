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
    /// PLUMADELAPAGINA — v6.50.68 — ARMA NUEVA 1: LA PLUMA DE LA PÁGINA.
    ///
    /// La letra del usuario: «los bastones están interesantes, pero me
    /// gusta más el aspecto de La Sombra de la Página… cambia los otros
    /// bastones por conceptos diferentes». LA PLUMA reemplaza a La
    /// Marea de la Página — ya no es un tentáculo: es la pluma del
    /// escriba, el TRAZO de tinta que atraviesa (agujas de hueso con
    /// rastro de tinta, homing suave, salpicadura al clavarse). Su
    /// festín: la LLUVIA DE TINTA (estilo 8).
    ///
    /// ARMA DE PRUEBA (no toca el grimorio): se entrega en LA BOLSA DE
    /// LAS SOMBRAS del kit de pruebas; la receta de 5 madera queda como
    /// vía alternativa (protocolo v6.14.2).
    /// </summary>
    public class PlumaDeLaPagina : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 95;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PlumaPaginaProjectile>();
            Item.shootSpeed = 24f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL TRAZO TRIPLE: tres agujas en abanico estrecho (la pluma
            // escribe con presión — tres trazos por firmada)
            Vector2 rumbo = velocity.LengthSquared() > 0.1f
                ? Vector2.Normalize(velocity) : Vector2.UnitX;
            for (int i = -1; i <= 1; i++)
            {
                Vector2 vel = rumbo.RotatedBy(i * 0.10f) * 24f;
                Projectile.NewProjectile(source, player.MountedCenter + rumbo * 18f, vel,
                    type, damage, knockback, player.whoAmI, 0f, 0f);
            }
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
