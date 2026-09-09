using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// TestNightglowLightBeams — Nightglow + rayos de luz radiantes en el cursor.
    /// Genera el efecto SpawnLightBeams (6 rayos de luz cian/blanca radiando).
    /// </summary>
    public class TestNightglowLightBeams : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 38;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 26;
            Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item12;
            Item.autoReuse = true;
            Item.shoot = 931;
            Item.shootSpeed = 14f;
            Item.mana = 6;
            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            CosmicEffects.SpawnLightBeams(Main.MouseWorld, count: 8, length: 120f);
            return true;
        }
    }
}
