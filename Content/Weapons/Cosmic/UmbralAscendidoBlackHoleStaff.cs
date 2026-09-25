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
    /// UmbralAscendidoBlackHoleStaff — v6.18 — EL BASTÓN DEL UMBRAL ASCENDIDO.
    ///
    /// LA COPIA MEJORADA del bastón del Umbral (que queda INTACTO): el
    /// agujero de la referencia, ELEVADO — Doppler extremo (el lado que se
    /// acerca ARDE a blanco incandescente y grueso; el lejano se hunde en
    /// rojo profundo y fino), LLUVIA DE RAYOS naranjas ramificados cayendo
    /// del círculo de runas (StormLib), ARCO DORADO eléctrico girando
    /// alrededor del horizonte, DOBLE anillo rúnico contrarrotante y brasas
    /// de estelas largas. 100% CÓDIGO.
    ///
    /// Física idéntica a la del Umbral (aura un 15% más rápida, muerte más
    /// rica). Hermano de todos los agujeros del mod — intactos.
    /// </summary>
    public class UmbralAscendidoBlackHoleStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 200;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 50; Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<UmbralAscendidoBlackHoleProjectile>();
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
            tooltips.Add(new TooltipLine(Mod, "D", "[c/FF9966:El Umbral elevado — Doppler extremo, lluvia de rayos ramificados y doble anillo rúnico]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Atrae enemigos en 450px · devora balas · muere en un Anillo de Einstein]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
