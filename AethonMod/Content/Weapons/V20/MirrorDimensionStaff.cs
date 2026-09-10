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
    /// MirrorDimensionStaff — crea un proyectil y su duplicado reflejado a
    /// través del cursor. Ambos se conectan con sparkles cian.
    /// </summary>
    public class MirrorDimensionStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 65;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 35;
            Item.useAnimation = 35;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ModContent.ProjectileType<MirrorDimensionProjectile>();
            Item.shootSpeed = 12f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 playerPos = player.Center;
            Vector2 cursor = Main.MouseWorld;
            // Mirrored position: player reflected through cursor.
            Vector2 mirrored = 2f * cursor - playerPos;

            int origId = Projectile.NewProjectile(source, playerPos, velocity,
                type, damage, knockback, player.whoAmI, 0f, 0f);
            int mirrorId = Projectile.NewProjectile(source, mirrored, velocity,
                type, damage, knockback, player.whoAmI, 0f, 0f);

            // Cross-link partners: ai[1] = partner whoAmI + 1 (0 means unset)
            if (origId >= 0 && origId < Main.projectile.Length)
            {
                Main.projectile[origId].ai[1] = mirrorId + 1;
                Main.projectile[origId].localAI[0] = 0f;
            }
            if (mirrorId >= 0 && mirrorId < Main.projectile.Length)
            {
                Main.projectile[mirrorId].ai[1] = origId + 1;
                Main.projectile[mirrorId].localAI[0] = 1f; // mark as mirror
            }
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "MD_Title",
                "[c/40D0FF:═══ DIMENSIÓN ESPEJO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "MD_Desc",
                "[c/80E0FF:Proyectil y su duplicado reflejado conectados por sparkles]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
