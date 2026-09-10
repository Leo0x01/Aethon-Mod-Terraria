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
    /// AbyssalTentacleStaff — bastón que invoca un tentáculo abisal que
    /// se extiende hacia los enemigos.
    ///
    /// Especificaciones:
    ///   - damage = 50 (DamageClass.Magic)
    ///   - useTime = useAnimation = 38
    ///   - shootSpeed = 10f
    ///   - mana = 0
    ///   - useStyle = HoldUp, autoReuse = true, noMelee = true
    ///   - Quest rarity, SoundID.Item8, receta 5 Wood
    /// </summary>
    public class AbyssalTentacleStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 50;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 38;
            Item.useAnimation = 38;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ModContent.ProjectileType<AbyssalTentacleProjectile>();
            Item.shootSpeed = 10f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 spawnPos = position + new Vector2(0f, -16f);
            Projectile.NewProjectile(source, spawnPos, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "AT_Title",
                "[c/2E8B57:═══ TENTÁCULO ABISAL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "AT_Desc",
                "[c/5577AA:Miembro oscuro que persigue enemigos y se extiende desde el jugador]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
