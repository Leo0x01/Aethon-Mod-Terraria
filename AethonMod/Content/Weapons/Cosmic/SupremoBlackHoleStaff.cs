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
    /// SupremoBlackHoleStaff — v6.18 — EL BASTÓN DEL AGUJERO NEGRO SUPREMO.
    ///
    /// EL 5to AGUJERO DEFINITIVO — LA FUSIÓN DE LOS 4 (petición del
    /// usuario): el disco Doppler del UMBRAL (blanco-oro → carmesí), el
    /// anillo de BANDAS del CÓSMICO (20 zonas de brillo viajando a
    /// 5 rad/s), los BRAZOS ESPIRALES del OLVIDO con flujo hacia
    /// adentro, el HUMO de la BRUMA (halo de nubes + volutas cayendo) —
    /// y la CORONA DE RAYOS de StormLib: 3 arcos eléctricos vibrando
    /// en el horizonte a ~10 Hz y 3 rayos fugitivos (2 carmesí + 1
    /// dorado), con jets polares oro/violeta y doble círculo de runas.
    ///
    /// La esfera más grande del mod (55px), atracción en 550px y el
    /// conjunto más REGIO: el oro/carmesí DOMINA, el violeta/azul
    /// ACENTÚA. Hermano del BlackHoleStaff, Crimson, Fusión, Olvido,
    /// Cósmico, Umbral y Bruma — todos quedan INTACTOS.
    /// </summary>
    public class SupremoBlackHoleStaff : ModItem
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
            Item.shoot = ModContent.ProjectileType<SupremoBlackHoleProjectile>();
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
            // v6.18: TOOLTIP CORTO — 2 líneas (el nombre vive en la
            // localización, arriba de la ventana de info).
            tooltips.Add(new TooltipLine(Mod, "D", "[c/FFD700:La fusión suprema de los 4 agujeros — Doppler, bandas, espirales, humo y rayos dorados]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Atrae enemigos en 550px · devora balas · muere en un Anillo de Einstein]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
