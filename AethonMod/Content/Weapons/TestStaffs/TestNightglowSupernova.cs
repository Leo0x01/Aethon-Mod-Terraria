using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// TestNightglowSupernova — Nightglow que genera una supernova cósmica
    /// completa (4 colores + blanco) en cada disparo.
    /// El efecto más espectacular de la estela cósmica.
    /// </summary>
    public class TestNightglowSupernova : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 50;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(0, 3, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item14; // explosión
            Item.autoReuse = true;
            Item.shoot = 931;
            Item.shootSpeed = 12f;
            Item.mana = 0; // bastón de prueba: sin costo de mana
            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Disparar el proyectil manualmente (como el remote)
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);

            // Supernova cósmica completa en el cursor
            CosmicEffects.SpawnSupernova(Main.MouseWorld, scale: 1.5f);
            // Esfera de impacto adicional para más impacto visual
            CosmicEffects.SpawnImpactSphere(Main.MouseWorld, 1f);
            return false;
        
        }
    }
}
