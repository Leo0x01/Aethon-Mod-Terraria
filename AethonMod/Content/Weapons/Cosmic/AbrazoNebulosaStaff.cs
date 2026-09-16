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
    /// AbrazoNebulosaStaff — EL ABRAZO DE LA NEBULOSA.
    ///
    /// Dispara una nebulosa que se instala en el punto del cursor
    /// (300 ticks): nube de bruma magenta-cian-violeta que crece de 40
    /// a 170 px y deriva lenta. Los enemigos dentro reciben un mordisco
    /// constante ×0.06 cada 3 ticks y quedan LENTOS. En su seno nacen
    /// estrellitas que revientan en mini-pops ×0.8. Al morir se
    /// disipa en polvo de estrellas. Una sola nebulosa: la nueva se
    /// come a la vieja.
    /// </summary>
    public class AbrazoNebulosaStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 120;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 45; Item.useAnimation = 45;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.shoot = ModContent.ProjectileType<AbrazoNebulosaProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8.WithPitchOffset(0.2f);
            Item.value = 21000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FF9BE8:EL ABRAZO DE LA NEBULOSA]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/F6E2FF:Instala una nebulosa en el cursor: 300 ticks de nube magenta-cian-violeta]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/C79BFF:Los de dentro reciben ×0.06 cada 3 ticks y quedan LENTOS · las estrellitas nacen y revientan ×0.8]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:El radio crece de 40 a 170 px · al morir se disipa en polvo de estrellas · sin maná]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL PUNTO donde instalarse (clamp de tejido: 640 px).
            Vector2 target = Main.MouseWorld;
            Vector2 delta = target - player.MountedCenter;
            if (delta.Length() > 640f)
                target = player.MountedCenter + Vector2.Normalize(delta) * 640f;

            // EL TOPE DE 1: una sola nebulosa — la nueva libera a la vieja.
            MatarNebulosaVieja(player);

            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Normalize(delta) * 6f,
                type, damage, knockback, player.whoAmI, target.X, target.Y);
            return false;
        }

        /// <summary>EL TOPE DE 1: mata a la nebulosa del mismo owner (si vive alguna).</summary>
        private static void MatarNebulosaVieja(Player player)
        {
            int tipo = ModContent.ProjectileType<AbrazoNebulosaProjectile>();
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
