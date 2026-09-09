using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// TestNightglowSparkleTrail — Nightglow que deja una estela de sparkles.
    /// Genera destellos ambientales constantes en la posición del jugador
    /// y una ráfaga de sparkles en el cursor al disparar.
    /// </summary>
    public class TestNightglowSparkleTrail : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 32;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item9;
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

            CosmicEffects.SpawnSparkles(Main.MouseWorld, count: 20, spread: 50f);
            return false;
        
        }

        public override void HoldItem(Player player)
        {
            // Estela de sparkles detrás del jugador mientras se mueve/sostiene
            if (Main.rand.NextBool(3))
            {
                CosmicEffects.SpawnSparkles(player.Center + new Vector2(0, -10f), count: 2, spread: 25f);
            }
        }
    }
}
