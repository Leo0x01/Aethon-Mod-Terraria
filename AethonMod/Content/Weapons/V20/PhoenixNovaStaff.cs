using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Projectiles.V20;

namespace AethonMod.Content.Weapons.V20
{
    /// <summary>
    /// PhoenixNovaStaff — bastón que genera una nova de fénix centrada en el jugador.
    ///
    /// Especificaciones:
    ///   - damage = 70 (DamageType.Magic)
    ///   - useTime = useAnimation = 50 (cooldown largo por nova)
    ///   - shootSpeed = 0f (sin velocidad: explosion estática)
    ///   - mana = 0
    ///   - useStyle = HoldUp, autoReuse = true, noMelee = true
    ///
    /// Override Shoot para crear el proyectil en la posición del jugador con
    /// velocidad cero. El proyectil se expande radialmente en 60 frames,
    /// dibujando múltiples Ring.png a escalas crecientes con colores
    /// naranja-rojo. En frame 30, flash con GlowCircleWhite. Aplica OnFire.
    /// </summary>
    public class PhoenixNovaStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 70;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 50;
            Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;
            Item.knockBack = 6f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ModContent.ProjectileType<PhoenixNovaProjectile>();
            Item.shootSpeed = 0f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Spawn en el centro exacto del jugador, velocidad cero
            Vector2 spawnPos = player.Center;
            Projectile.NewProjectile(source, spawnPos, Vector2.Zero, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "PN_Title",
                "[c/FF6400:═══ NOVA DE FÉNIX ═══]"));
            tooltips.Add(new TooltipLine(Mod, "PN_Desc",
                "[c/B388FF:Explosión radial que envuelve al jugador en llamas]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
