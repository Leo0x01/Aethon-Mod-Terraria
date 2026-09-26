using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Weapons.Cosmic
{
    /// <summary>
    /// RayoGammaStaff — EL RAYO GAMMA.
    ///
    /// LA TÉCNICA DEL JUICIO DIFERIDO: al disparar, un haz fino hitscan
    /// de 700 px cruza TODO al instante — flash blanco-cian con
    /// aberración cromática — pero no hace daño aún: cada enemigo
    /// tocado queda MARCADO con un contorno brillante. Tras 30 ticks,
    /// TODAS las marcas liquidan a la vez: clang + destello + daño ×1.0
    /// + quemadura cósmica 5 s.
    /// </summary>
    public class RayoGammaStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 300;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 70; Item.useAnimation = 70;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.shoot = ModContent.ProjectileType<RayoGammaProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8.WithPitchOffset(0.65f);
            Item.value = 25000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/BFF3FF:EL RAYO GAMMA]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/EAFBFF:Un haz fino de 700 px cruza TODO al instante... pero aún no hace daño]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/7FDFFF:Tras 30 ticks, TODAS las marcas LIQUIDAN a la vez: clang + daño ×1.0 + quemadura cósmica 5 s]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:Si un marcado muere antes, su marca se apaga en silencio · sin maná]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA DIRECCIÓN del juicio (viaja en ai[0]).
            Vector2 dir = velocity.LengthSquared() > 0.01f
                ? Vector2.Normalize(velocity)
                : Vector2.Normalize(Main.MouseWorld - position);

            Projectile.NewProjectile(source, player.MountedCenter + dir * 24f, Vector2.Zero,
                type, damage, knockback, player.whoAmI, dir.ToRotation(), 0f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
