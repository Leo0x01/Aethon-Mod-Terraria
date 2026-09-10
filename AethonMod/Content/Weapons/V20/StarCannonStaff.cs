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
    /// StarCannonStaff — bastón que invoca estrellas cayendo del cielo.
    ///
    /// Especificaciones:
    ///   - damage = 70 (DamageType.Magic)
    ///   - useTime = useAnimation = 40
    ///   - shootSpeed = 12f
    ///   - mana = 0
    ///   - useStyle = HoldUp, autoReuse = true, noMelee = true
    ///
    /// Override Shoot: genera 3 proyectiles a Y-300 del cursor cayendo hacia abajo.
    /// </summary>
    public class StarCannonStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 70;
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
            Item.shoot = ModContent.ProjectileType<StarCannonProjectile>();
            Item.shootSpeed = 12f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 cursorWorld = Main.MouseWorld;
            Vector2 spawnPos = new Vector2(cursorWorld.X, cursorWorld.Y - 300f);
            for (int i = 0; i < 3; i++)
            {
                Vector2 offset = new Vector2(Main.rand.NextFloat(-40f, 40f), -i * 20f);
                Vector2 vel = new Vector2(Main.rand.NextFloat(-1f, 1f), 6f);
                Projectile.NewProjectile(source, spawnPos + offset, vel, type, damage, knockback, player.whoAmI);
            }
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "StarCannon_Title",
                "[c/FFD700:═══ STAR CANNON ═══]"));
            tooltips.Add(new TooltipLine(Mod, "StarCannon_Desc",
                "[c/FFEC8B:Invoca estrellas que caen del cielo sobre el cursor]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
