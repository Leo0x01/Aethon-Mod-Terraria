using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Custom;

namespace AethonMod.Content.Weapons.Custom
{
    /// <summary>
    /// SkyLightningStaff — bastón que invoca relámpagos desde el cielo.
    /// El proyectil cae verticalmente desde arriba del cursor con rayos,
    /// ramificaciones eléctricas y flash de impacto.
    /// </summary>
    public class SkyLightningStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 55;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SkyLightningStrike>();
            Item.shootSpeed = 20f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item93;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Spawn el relámpago desde arriba del cursor (400px arriba)
            Vector2 target = Main.MouseWorld;
            Vector2 spawnPos = new Vector2(target.X, target.Y - 400f);
            Vector2 downward = new Vector2(0f, 20f);
            Projectile.NewProjectile(source, spawnPos, downward, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/00C8FF:═══ RELÁMPAGO CELESTIAL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Invoca relámpagos desde el cielo hacia el cursor]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Flash de impacto + onda eléctrica expansiva]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
