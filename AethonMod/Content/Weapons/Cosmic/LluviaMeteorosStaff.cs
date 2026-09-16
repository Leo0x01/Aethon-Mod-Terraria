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
    /// LluviaMeteorosStaff — LA LLUVIA DE METEOROS.
    ///
    /// Marca la zona del cursor con un telégrafo circular fino ámbar
    /// (30 ticks, radio 220) y luego caen 8 METEOROS escalonados (uno
    /// cada 4 ticks) en posiciones deterministas dentro de la zona:
    /// cada uno cae en diagonal con estela de fuego y al tocar suelo o
    /// enemigo EXPLOTA (×1.0, radio 70) con quemadura cósmica. Un solo
    /// director en campo: el nuevo se come al viejo.
    /// </summary>
    public class LluviaMeteorosStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 170;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 50; Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LluviaMeteorosProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8.WithPitchOffset(-0.35f);
            Item.value = 22000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FFD9A0:LA LLUVIA DE METEOROS]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/FFF3DC:Marca la zona del cursor: 30 ticks de telégrafo ámbar y caen 8 meteoros]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FFB96F:Cada meteoro cae en diagonal con estela de fuego y estalla ×1.0 en radio 70 · quemadura cósmica]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:Una piedra cada 4 ticks · las posiciones son deterministas · sin maná]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA ZONA: el punto del cursor (clamp de tejido: 760 px).
            Vector2 target = Main.MouseWorld;
            Vector2 delta = target - player.MountedCenter;
            if (delta.Length() > 760f)
                target = player.MountedCenter + Vector2.Normalize(delta) * 760f;

            // EL TOPE DE 1: un solo director — el nuevo libera al viejo.
            MatarDirectorViejo(player);

            Projectile.NewProjectile(source, target, Vector2.Zero,
                type, damage, knockback, player.whoAmI, 0f, 0f);
            return false;
        }

        /// <summary>EL TOPE DE 1: mata al director del mismo owner (si vive alguno).</summary>
        private static void MatarDirectorViejo(Player player)
        {
            int tipo = ModContent.ProjectileType<LluviaMeteorosProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != tipo || p.owner != player.whoAmI)
                    continue;
                p.Kill();
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
