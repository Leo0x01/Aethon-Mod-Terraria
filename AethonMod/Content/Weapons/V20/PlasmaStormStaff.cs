using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Projectiles.V20;

namespace AethonMod.Content.Weapons.V20
{
    /// <summary>
    /// PlasmaStormStaff — bastón que invoca una tormenta de 5 orbes de plasma
    /// con posiciones y velocidades aleatorias. Los orbes cercanos entre sí
    /// generan arcos eléctricos (BeamCyan).
    ///
    /// Especificaciones:
    ///   - damage = 40 (DamageType.Magic)
    ///   - useTime = useAnimation = 35
    ///   - shootSpeed = 10f
    ///   - mana = 0
    ///   - useStyle = HoldUp, autoReuse = true, noMelee = true
    ///   - Quest rarity, SoundID.Item8, receta 5 Wood
    ///
    /// Dispara PlasmaStormProjectile (5 orbes con arcos entre ellos).
    /// </summary>
    public class PlasmaStormStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 40;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 28;
            Item.useTime = 35;
            Item.useAnimation = 35;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ModContent.ProjectileType<PlasmaStormProjectile>();
            Item.shootSpeed = 10f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Spawn 5 proyectiles en posiciones aleatorias dentro de 100px del jugador,
            // con velocidades dispersas.
            Vector2 baseDir = velocity.SafeNormalize(new Vector2(1f, 0f));
            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = new Vector2(
                    Main.rand.NextFloat(-100f, 100f),
                    Main.rand.NextFloat(-100f, 100f));
                Vector2 spawnPos = position + offset;
                float spread = Main.rand.NextFloat(-0.35f, 0.35f);
                Vector2 vel = baseDir.RotatedBy(spread) * velocity.Length() *
                              Main.rand.NextFloat(0.7f, 1.3f);
                Projectile.NewProjectile(source, spawnPos, vel, type, damage, knockback,
                    player.whoAmI, Main.rand.NextFloat(0f, 6.28f));
            }
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "PS_Title",
                "[c/FF00FF:═══ TORMENTA DE PLASMA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "PS_Desc",
                "[c/D040FF:Cinco orbes de plasma que se arquean eléctricamente entre sí]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
