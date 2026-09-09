using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// TestMagicRing — bastón de prueba PROTEGIDO (no modificar).
    /// Dispara Nightglow (#931) y genera un anillo mágico dorado en cada uso.
    /// Efecto: demostración del SpawnMagicRing básico.
    /// </summary>
    public class TestMagicRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName cargado desde Localization
        }

        public override void SetDefaults()
        {
            Item.damage = 30;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;
            Item.shoot = 931; // Nightglow
            Item.shootSpeed = 12f;
            Item.mana = 5;
            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Anillo mágico dorado en la posición del cursor
            Vector2 target = Main.MouseWorld;
            CosmicEffects.SpawnMagicRing(target, CosmicEffects.Gold, particleCount: 24, radius: 60f, speed: 3f);
            return true; // tModLoader dispara el Nightglow vanilla
        }
    }
}
