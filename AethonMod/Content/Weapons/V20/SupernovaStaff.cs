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
    /// SupernovaStaff — bastón que invoca una estrella que colapsa durante 3
    /// segundos y luego estalla en una supernova masiva (v5.85 mejorada).
    ///
    /// Especificaciones:
    ///   - damage = 90 (DamageType.Magic)
    ///   - useTime = useAnimation = 45
    ///   - shootSpeed = 6f (proyectil lento, carga 180 frames = 3 s)
    ///   - mana = 0
    ///   - useStyle = HoldUp, autoReuse = true, noMelee = true
    ///   - Quest rarity, SoundID.Item8, receta 5 Wood
    ///
    /// Dispara SupernovaProjectile (carga con atracción creciente → explosión
    /// masiva con 3 ondas expansivas de fuego que dañan y queman, flash y AoE
    /// de 340px).
    /// El Sol (SunProjectile) lo invoca centrado en su segundo 7 y ambos
    /// explotan sincronizados en el segundo 10.
    /// </summary>
    public class SupernovaStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 90;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;
            Item.knockBack = 6f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ModContent.ProjectileType<SupernovaProjectile>();
            Item.shootSpeed = 6f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 spawnPos = position + new Vector2(0f, -32f);
            Projectile.NewProjectile(source, spawnPos, velocity * 0.3f, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "SN_Title",
                "[c/FFD700:═══ SUPERNOVA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "SN_Desc",
                "[c/FFE0A0:Una estrella que colapsa hacia el blanco calor durante 3 segundos y estalla en 3 ondas expansivas de fuego]"));
            tooltips.Add(new TooltipLine(Mod, "SN_Desc2",
                "[c/78788C:Cada onda hace daño al pasar y provoca quemadura (5 s) a los enemigos alcanzados]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
