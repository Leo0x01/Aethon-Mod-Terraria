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
    /// FiloHorizonteStaff — EL FILO DEL HORIZONTE.
    ///
    /// Lanza un HORIZONTE DE SUCESOS portátil: un disco negro girando a
    /// toda velocidad con anillo de acreción ámbar-violeta y una línea
    /// de fotón en el ecuador. Atraviesa enemigos perdiendo velocidad,
    /// curva suave hacia el más cercano y VUELVE como bumerán — al
    /// volver, se puede volver a lanzar. Cada golpe deja una MARCA que
    /// implosiona 30 ticks después (×0.7 + arrastre de 6 px).
    /// </summary>
    public class FiloHorizonteStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 230;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 38; Item.useAnimation = 38;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<FiloHorizonteProjectile>();
            Item.shootSpeed = 15f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8.WithPitchOffset(-0.5f);
            Item.value = 23000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/C9A0FF:EL FILO DEL HORIZONTE]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/EBE0FF:Lanza un disco negro — un horizonte de sucesos — que atraviesa TODO y vuelve a ti]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/B48CFF:Corte ×1.0 que pierde velocidad con cada golpe · la MARCA implosiona a los 30 ticks: ×0.7 + arrastre]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:Curva suave hacia el enemigo más cercano · línea de fotón en el ecuador · sin maná]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 dir = velocity.LengthSquared() > 0.01f
                ? Vector2.Normalize(velocity)
                : Vector2.Normalize(Main.MouseWorld - position);

            // EL SENTIDO DEL SPIN: alternado por casteo — determinista,
            // sin Main.rand (viaja en ai[0]: el mismo disco en todos).
            float sentido = (Main.GameUpdateCount / 35) % 2 == 0 ? 1f : -1f;

            Projectile.NewProjectile(source, player.MountedCenter + dir * 36f, dir * 15f,
                type, damage, knockback, player.whoAmI, sentido, 0f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
