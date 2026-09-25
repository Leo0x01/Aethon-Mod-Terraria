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
    /// SupremoAuroraBlackHoleStaff — v6.20 — EL BASTÓN DEL AGUJERO NEGRO
    /// SUPREMO AURORA.
    ///
    /// PETICIÓN DEL USUARIO (v6.20): "en el agujero negro supremo, cambiar
    /// el color — centro negro, morado cerca del centro, azul y dorado en
    /// los bordes. Guardar una copia del original y crear uno nuevo con
    /// estos cambios". EL ORIGINAL QUEDA INTACTO: el Bastón del Agujero
    /// Negro Supremo (v6.18) sigue existiendo tal cual — este es el
    /// HERMANO AURORA.
    ///
    /// La MISMA física suprema (esfera de 55px, atracción en 550px, devora
    /// balas, anillo de Einstein) con el GRADIENTE AURORA: el núcleo
    /// negro, el rim y el anillo de fotones MORADOS, los brazos y runas
    /// interiores AZULES, el exterior del anillo de bandas, las puntas de
    /// los jets y el círculo exterior de runas DORADOS — el gradiente
    /// hecho vórtice.
    /// </summary>
    public class SupremoAuroraBlackHoleStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 300;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 50; Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SupremoAuroraBlackHoleProjectile>();
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
            // v6.20: TOOLTIP CORTO — 2 líneas (el gradiente del alba polar).
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B46CFF:El Supremo vestido de aurora — centro negro, morado junto al núcleo, azul y dorado en los bordes]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:La misma física suprema · el original dorado queda intacto · muere en un Anillo de Einstein]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
