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
    /// SolarFlareStaff — bastón que detona una ráfaga radial de luz solar.
    ///
    /// Especificaciones:
    ///   - damage = 80 (DamageType.Magic)
    ///   - useTime = useAnimation = 40
    ///   - shootSpeed = 8f
    ///   - mana = 0
    ///   - useStyle = HoldUp, autoReuse = true, noMelee = true
    ///
    /// Dispara SolarFlareProjectile (ráfaga radial: 6 rayos BeamGold a 60°,
    /// GlowCircleGold central pulsante, dust GoldFlame hacia afuera).
    /// </summary>
    public class SolarFlareStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 80;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 28;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ModContent.ProjectileType<SolarFlareProjectile>();
            Item.shootSpeed = 8f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 spawnPos = position + new Vector2(0f, -16f);
            Projectile.NewProjectile(source, spawnPos, Vector2.Zero, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "SolarFlare_Title",
                "[c/FF8C00:═══ SOLAR FLARE ═══]"));
            tooltips.Add(new TooltipLine(Mod, "SolarFlare_Desc",
                "[c/FFB347:Ráfaga radial de luz solar que abrasa a los enemigos cercanos]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
