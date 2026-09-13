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
    /// BrumaBlackHoleStaff — v6.17 — EL BASTÓN DEL AGUJERO NEGRO DE LA BRUMA.
    ///
    /// La DEMOSTRACIÓN de la LIBRERÍA DE HUMO/NIEBLA/BRUMA PROCEDURAL
    /// 100% PROPIA del proyecto (Content/Effects/Bruma): un vacío GELIDO
    /// envuelto en bruma nebular fría teal/cian/violeta — el HALO son
    /// nubes (BrumaFX.Cloud), el anillo son FUMARELITOS (BrumaFX.Puff con
    /// texturas fBm horneadas 100% en runtime), las VOLUTAS espiralan al
    /// núcleo (BrumaFX.Tendril) y por los polos escapan CHIMENEAS de
    /// bruma (BrumaFX.Column). Todo por código, a CUALQUIER escala.
    ///
    /// Uno de los 4 agujeros DEFINITIVOS: UMBRAL · BRUMA · CÓSMICO · OLVIDO.
    /// </summary>
    public class BrumaBlackHoleStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 150;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 50; Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BrumaBlackHoleProjectile>();
            Item.shootSpeed = 6f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item20;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // v6.18: TOOLTIP CORTO — la ventana de info ya no es un muro de
            // texto (el nombre ahora vive en la localización, arriba).
            tooltips.Add(new TooltipLine(Mod, "D", "[c/8FDCEF:100% por código con la librería de bruma — humo y niebla procedural a cualquier escala]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Atrae enemigos en 450px · devora balas · muere en un Anillo de Einstein]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
