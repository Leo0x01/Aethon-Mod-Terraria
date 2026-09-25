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
    /// VozCuasarStaff — LA VOZ DEL CUÁSAR.
    ///
    /// Invoca un CUÁSAR (disco de acreción compacto blanco-azul con un
    /// anillo fino inclinado) al lado del lanzador. Carga 40 ticks —
    /// el anillo se tensa, el disco se aprieta, un pitido sube — y
    /// luego SUELTA UN HAZ CONTINUO de 460 px que perfora TODO, con
    /// DOPPLER cromático: el borde delantero más azul, la cola más
    /// roja. Si el haz toca una pared, REBOTA una vez. Muere con un
    /// destello.
    /// </summary>
    public class VozCuasarStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 260;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 55; Item.useAnimation = 55;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<VozCuasarProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8.WithPitchOffset(0.45f);
            Item.value = 25000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/BFE8FF:LA VOZ DEL CUÁSAR]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/E8F6FF:Invoca un cuásar que carga 40 ticks y suelta un HAZ CONTINUO de 460 px]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/6FC4FF:El haz PERFORA TODO · DOPPLER: el borde delantero azul, la cola roja · rebota una vez en las paredes]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:El anillo se tensa mientras carga · muere con un destello · sin maná]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL CUÁSAR nace al lado del jugador, mirando hacia el disparo.
            Vector2 dir = velocity.LengthSquared() > 0.01f
                ? Vector2.Normalize(velocity)
                : Vector2.Normalize(Main.MouseWorld - position);

            Projectile.NewProjectile(source, player.MountedCenter + dir * 46f, dir * 2f,
                type, damage, knockback, player.whoAmI, dir.ToRotation(), 0f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
