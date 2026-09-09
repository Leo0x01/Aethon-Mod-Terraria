using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// TestNightglowRainbowTrail — Nightglow con estela arcoíris cambiante.
    /// El color de la estela cambia constantemente según el tiempo (efecto HSL).
    /// </summary>
    public class TestNightglowRainbowTrail : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 34;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;
            Item.shoot = 931;
            Item.shootSpeed = 12f;
            Item.mana = 5;
            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Ráfaga de estela arcoíris en el disparo
            for (int i = 0; i < 5; i++)
            {
                CosmicEffects.SpawnRainbowTrail(position, velocity);
            }
            return true;
        }
    }
}
