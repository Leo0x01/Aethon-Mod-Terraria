using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// TestSparkle — bastón de prueba PROTEGIDO (no modificar).
    /// Dispara Nightglow (#931) y genera destellos ambientales (sparkles)
    /// alrededor del cursor en cada uso.
    /// Efecto: demostración del SpawnSparkles básico.
    /// </summary>
    public class TestSparkle : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 28;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;
            Item.shoot = 931; // Nightglow
            Item.shootSpeed = 12f;
            Item.mana = 4;
            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Destellos ambientales alrededor del cursor
            CosmicEffects.SpawnSparkles(Main.MouseWorld, count: 12, spread: 40f);
            return true;
        }

        public override void HoldItem(Player player)
        {
            // Destellos constantes alrededor del jugador mientras sostiene el bastón
            if (Main.rand.NextBool(5))
            {
                CosmicEffects.SpawnSparkles(player.Center, count: 2, spread: 30f);
            }
        }
    }
}
