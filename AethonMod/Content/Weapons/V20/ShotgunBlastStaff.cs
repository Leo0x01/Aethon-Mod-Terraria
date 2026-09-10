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
    /// ShotgunBlastStaff — bastón que dispara 6 proyectiles en abanico.
    ///
    /// Especificaciones:
    ///   - damage = 40 (DamageClass.Generic)
    ///   - useTime = useAnimation = 20
    ///   - shootSpeed = 12f
    ///   - mana = 0
    ///   - useStyle = HoldUp, autoReuse = true, noMelee = true
    ///   - Quest rarity, SoundID.Item8, receta 5 Wood
    ///
    /// Override de Shoot: genera 6 proyectiles a distintos ángulos
    /// (spread tipo escopeta).
    /// </summary>
    public class ShotgunBlastStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 40;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ModContent.ProjectileType<ShotgunBlastProjectile>();
            Item.shootSpeed = 12f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Spread de 6 proyectiles en un cono de ~30 grados
            int numShots = 6;
            float spread = MathHelper.ToRadians(30f);
            Vector2 spawnPos = position + new Vector2(0f, -16f);
            float baseAngle = velocity.ToRotation();
            Vector2 baseVel = velocity.SafeNormalize(Vector2.Zero) * Item.shootSpeed;

            for (int i = 0; i < numShots; i++)
            {
                // Distribución uniforme entre -spread/2 y +spread/2
                float t = numShots > 1 ? (float)i / (numShots - 1) - 0.5f : 0f;
                float angleOffset = t * spread;
                Vector2 shotVel = baseVel.RotatedBy(angleOffset);
                Projectile.NewProjectile(source, spawnPos, shotVel, type, damage, knockback, player.whoAmI);
            }
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "SB_Title",
                "[c/FF8C00:═══ DESCARGA EN ABANICO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "SB_Desc",
                "[c/B388FF:Dispara 6 proyectiles en abanico, generando chispas al impacto]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
