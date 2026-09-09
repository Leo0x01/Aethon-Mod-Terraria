using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;
using AethonMod.Content.Buffs;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// TestNightglowEmpower — Nightglow que concede Empoderamiento Cósmico mientras se sostiene.
    /// El jugador recibe +10% daño, +5% crit, +5% atk speed, 1% lifesteal al sostenerlo.
    /// </summary>
    public class TestNightglowEmpower : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 33;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 2, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;
            Item.shoot = 931;
            Item.shootSpeed = 12f;
            Item.mana = 0; // bastón de prueba: sin costo de mana
            Item.noMelee = true;
        }

        public override void HoldItem(Player player)
        {
            // Conceder el buff CosmicEmpowerment mientras sostenga el bastón
            player.AddBuff(ModContent.BuffType<CosmicEmpowermentBuff>(), 60);

            // Aura dorada visual
            if (Main.rand.NextBool(7))
            {
                Dust d = Dust.NewDustPerfect(player.Center, DustID.GoldFlame,
                    new Vector2(
                        Main.rand.NextFloat(-1f, 1f),
                        Main.rand.NextFloat(-2f, -0.5f)),
                    180, CosmicEffects.Gold, 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }
}
