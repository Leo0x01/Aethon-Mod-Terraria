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
    /// OlvidoBlackHoleStaff — v6.18 — EL BASTÓN DEL AGUJERO NEGRO DEL OLVIDO.
    ///
    /// El 4to agujero negro del mod, 100% CREADO POR CÓDIGO (directiva del
    /// usuario): vórtice MORADO-AZUL (recoloreado en v6.18) con brazos
    /// espirales, runas doradas orbitando, ondas de distorsión, nebulosas
    /// difusas y aura mística. El núcleo queda NEGRO ABSOLUTO y LIMPIO
    /// (las partículas interiores fueron quitadas en v6.18 — como en el
    /// agujero de la Bruma).
    ///
    /// Uno de los 4 agujeros DEFINITIVOS: UMBRAL · BRUMA · CÓSMICO · OLVIDO.
    /// </summary>
    public class OlvidoBlackHoleStaff : ModItem
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
            Item.shoot = ModContent.ProjectileType<OlvidoBlackHoleProjectile>();
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
            tooltips.Add(new TooltipLine(Mod, "D", "[c/A78BFF:100% por código — vórtice morado-azul con brazos espirales y tormenta eléctrica]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Atrae enemigos en 450px · devora balas · muere en un Anillo de Einstein]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
