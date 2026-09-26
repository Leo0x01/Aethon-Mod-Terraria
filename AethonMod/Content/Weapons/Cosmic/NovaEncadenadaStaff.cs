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
    /// NovaEncadenadaStaff — LA NOVA ENCADENADA.
    ///
    /// Dispara una ESFERA DE NOVA lenta hacia el cursor; al tocar un
    /// enemigo o el suelo ESTALLA en una nova dorada (anillo de choque,
    /// radio 140) y ENCADENA: lanza 3 novas hijas hacia los 3 enemigos
    /// más cercanos aún sin golpear; cada hija estalla en nova ×0.65 y
    /// lanza 2 nietas ×0.42 — hasta 3 generaciones. Cero azar: los
    /// objetivos se eligen por distancia.
    /// </summary>
    public class NovaEncadenadaStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 200;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 40; Item.useAnimation = 40;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<NovaEncadenadaProjectile>();
            Item.shootSpeed = 7f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8.WithPitchOffset(0.1f);
            Item.value = 24000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FFD76B:LA NOVA ENCADENADA]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/FFF0C4:Una esfera de nova lenta que estalla al tocar enemigo o suelo]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FFB545:Cada nova encadena 3 hijas ×0.65 y 2 nietas ×0.42 — hasta 3 generaciones]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:El frente del anillo es lo que golpea · cero azar: presas por distancia · sin maná]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA ESFERA MADRE (generación 0) viaja LENTA hacia el cursor.
            Vector2 dir = velocity.LengthSquared() > 0.01f
                ? Vector2.Normalize(velocity)
                : Vector2.Normalize(Main.MouseWorld - position);

            Projectile.NewProjectile(source, position + dir * 24f, dir * 7f,
                type, damage, knockback, player.whoAmI, -1f, 0f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
