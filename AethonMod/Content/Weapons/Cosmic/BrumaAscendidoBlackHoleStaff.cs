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
    /// BrumaAscendidoBlackHoleStaff — v6.18 — EL BASTÓN DE LA BRUMA ASCENDIDA.
    ///
    /// LA COPIA MEJORADA del bastón de la Bruma (que queda INTACTO): el
    /// vacío gelido, ELEVADO — CORONAS DE ESCARCHA ELÉCTRICA parpadeando
    /// alrededor del horizonte (StormLib.Arc cian a ~10 Hz, la firma
    /// visual), RAYOS GELIDOS ramificados escapando del anillo de humo,
    /// CINCO volutas serpenteando con curl reforzado, CRISTALES de hielo
    /// flotando y chimeneas polares más altas. 100% CÓDIGO con las
    /// librerías del proyecto (BrumaFX + StormLib).
    ///
    /// Física idéntica a la de la Bruma (aura un 15% más rápida, muerte
    /// más rica). Hermano de todos los agujeros del mod — intactos.
    /// </summary>
    public class BrumaAscendidoBlackHoleStaff : ModItem
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
            Item.shoot = ModContent.ProjectileType<BrumaAscendidoBlackHoleProjectile>();
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
            tooltips.Add(new TooltipLine(Mod, "D", "[c/8FDCEF:La Bruma elevada — coronas de escarcha eléctrica y cinco volutas disolviéndose en el núcleo]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Atrae enemigos en 450px · devora balas · muere en un Anillo de Einstein]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
