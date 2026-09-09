using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// TestNightglowStarfall — Nightglow + estrellas cayendo del cielo.
    /// Genera el efecto SpawnStarfall (estrellas cayendo hacia el cursor).
    /// Inspirado en la Star Wrath vanilla pero con paleta cósmica.
    /// </summary>
    public class TestNightglowStarfall : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 45;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 2, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item68; // sonido estrella
            Item.autoReuse = true;
            Item.shoot = 931;
            Item.shootSpeed = 12f;
            Item.mana = 10;
            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 5-7 estrellas cayendo del cielo
            CosmicEffects.SpawnStarfall(Main.MouseWorld, count: Main.rand.Next(5, 8), spread: 120f);
            return true;
        }
    }
}
