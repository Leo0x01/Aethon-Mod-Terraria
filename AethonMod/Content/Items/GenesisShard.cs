using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// El Fragmento Génesis — arma de luz y material de crafteo.
    /// - Dropeado por King Slime o Eye of Cthulhu.
    /// - Arma de luz que dispara proyectiles autoguiados.
    /// - Se usa como material para craftear el Grimorio del Eterno.
    /// </summary>
    public class GenesisShard : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 18;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28;
            Item.height = 28;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item9;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<Weapons.Projectiles.GenesisLight>();
            Item.shootSpeed = 14f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            for (int i = 0; i < 2; i++)
            {
                float angle = (i - 0.5f) * 0.15f;
                Vector2 perturbed = velocity.RotatedBy(angle);
                Projectile.NewProjectile(source, position, perturbed, type, damage, knockback, player.whoAmI);
            }
            return false;
        }

        public override bool? UseItem(Player player) => null;

        public override void AddRecipes()
        {
            // Grimorio del Eterno — se craftea con Fragmento Génesis + lingotes
            Recipe.Create(ModContent.ItemType<Weapons.GrimoireEternal>())
                .AddIngredient<GenesisShard>(1)
                .AddIngredient(ItemID.GoldBar, 5)
                .Register();

            Recipe.Create(ModContent.ItemType<Weapons.GrimoireEternal>())
                .AddIngredient<GenesisShard>(1)
                .AddIngredient(ItemID.PlatinumBar, 5)
                .Register();
        }
    }
}
