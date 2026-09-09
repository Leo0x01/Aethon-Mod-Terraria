using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// TestNightglowLifesteal — Nightglow + 5% lifesteal mientras se sostiene.
    /// Aplica el flag HasCosmicEmpowerment (mismo que el Sello de Aethon)
    /// pero con un lifesteal mayor (5% en vez de 1%) para pruebas.
    /// </summary>
    public class TestNightglowLifesteal : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 30;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.FairyQueenMagicItemShot; // Nightglow
            Item.shootSpeed = 12f;
            Item.mana = 0; // bastón de prueba: sin costo de mana
            Item.noMelee = true;
        }

        public override void HoldItem(Player player)
        {
            // Activar lifesteal mientras sostiene este bastón
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null)
            {
                sp.HasCosmicEmpowerment = true; // activa lifesteal 1% (del buff)
                sp.HasEnhancedLifesteal = true; // activa lifesteal extra 4% (total 5%)
            }

            // Aura visual de lifesteal (partículas rojas/magenta alrededor del jugador)
            if (Main.rand.NextBool(6))
            {
                Vector2 offset = new Vector2(
                    Main.rand.NextFloat(-20f, 20f),
                    Main.rand.NextFloat(-30f, 0f));
                Dust d = Dust.NewDustPerfect(player.Center + offset, DustID.RainbowTorch,
                    new Vector2(0, -0.5f),
                    150, CosmicEffects.Magenta, 0.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // v5.59: añadido Shoot override para consistencia con los demás bastones
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            // Aura magenta en el cursor al disparar
            CosmicEffects.SpawnMagicRing(Main.MouseWorld, CosmicEffects.Magenta, 16, 50f, 3f);
            return false;
        }
    }
}
