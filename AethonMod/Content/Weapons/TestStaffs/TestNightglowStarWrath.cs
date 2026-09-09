using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// TestNightglowStarWrath — bastón especial que recrea el efecto de la imagen.
    /// Dispara Nightglow (#931) y genera el efecto completo "Star Wrath":
    ///   1. Esfera de impacto central aditiva (blanco/cian/azul real)
    ///   2. Estrellas cayendo del cielo (4-6 estrellas con estela)
    ///   3. Destellos ambientales (sparkles)
    ///   4. Rayos de luz radiantes
    ///   5. Anillo mágico dorado secundario
    /// </summary>
    public class TestNightglowStarWrath : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 80;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item68; // sonido de estrella
            Item.autoReuse = true;
            Item.shoot = 931; // Nightglow
            Item.shootSpeed = 14f;
            Item.mana = 0; // bastón de prueba: sin costo de mana
            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // === EFECTO COMPLETO STAR WRATH en el cursor ===
            Vector2 target = Main.MouseWorld;
            CosmicEffects.SpawnStarWrathEffect(target);

            // Sonido adicional de impacto cósmico
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, target);
            return true;
        }

        public override void HoldItem(Player player)
        {
            // Mientras sostiene: destellos sutiles alrededor del jugador
            if (Main.rand.NextBool(8))
            {
                CosmicEffects.SpawnSparkles(player.Center, count: 1, spread: 20f);
            }
        }
    }
}
