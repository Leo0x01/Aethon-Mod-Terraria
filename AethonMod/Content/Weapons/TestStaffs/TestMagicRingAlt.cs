using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// TestMagicRingAlt — versión ALTERNATIVA del TestMagicRing del remote.
    /// NO toca el original del remote (namespace Weapons.TestMagicRing).
    /// Esta versión usa el helper CosmicEffects.SpawnMagicRing.
    /// Dispara Nightglow (#931) y genera un anillo mágico dorado en cada uso.
    /// </summary>
    public class TestMagicRingAlt : ModItem
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
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.FairyQueenMagicItemShot; // Nightglow
            Item.shootSpeed = 12f;
            Item.mana = 0; // bastón de prueba: sin costo de mana
            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Disparar el proyectil manualmente (como el remote)
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            // Anillo mágico dorado en la posición del cursor
            Vector2 target = Main.MouseWorld;
            CosmicEffects.SpawnMagicRing(target, CosmicEffects.Gold, particleCount: 24, radius: 60f, speed: 3f);
            return false; // nosotros disparamos el proyectil
        }
    }
}
