using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// TestMagicRingV2Alt — versión ALTERNATIVA del TestMagicRingV2 del remote.
    /// NO toca el original del remote (namespace Weapons.TestMagicRingV2).
    /// Esta versión usa el helper CosmicEffects.SpawnMagicRingMulti.
    /// Dispara Nightglow (#931) y genera MÚLTIPLES anillos cósmicos.
    /// </summary>
    public class TestMagicRingV2Alt : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 40;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;
            Item.shoot = 931; // Nightglow
            Item.shootSpeed = 12f;
            Item.mana = 0; // bastón de prueba: sin costo de mana
            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Disparar el proyectil manualmente (como el remote)
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);

            // Múltiples anillos de colores cósmicos en el cursor
            CosmicEffects.SpawnMagicRingMulti(Main.MouseWorld, speed: 3f);
            return false;
        
        }
    }
}
