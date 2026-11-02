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
    /// AgujeroAbsolutoStaff — v6.82 — EL BASTÓN DEL AGUJERO ABSOLUTO.
    ///
    /// LA LETRA: «crea un arma nueva de agujero negro runico con lo que
    /// creas que sea bueno, este debe ser mas fuerte que los supremos
    /// asi que tiene que tener 4 anillos runicos de agujero negro».
    ///
    /// EL MÁS ALLÁ DE LOS SUPREMOS: el motor completo del Supremo (la
    /// fusión v6.80 — disco Doppler del Umbral, anillo de bandas del
    /// Cósmico, brazos espirales del Olvido, humo de la Bruma y corona
    /// de rayos de StormLib) viste EL PERFIL ABSOLUTO — el gradiente
    /// del más allá (violeta del vacío → cian estelar → oro blanco),
    /// rim cian, blanco platino — y LA CUARTA CORONA RÚNICA: 10 runas
    /// blanco-cian a 3,95·R en contrarroto lento, MÁS AFUERA que
    /// cualquier círculo Supremo. EL ÚNICO agujero con CUATRO anillos
    /// rúnicos.
    ///
    /// MÁS FUERTE QUE LOS SUPREMOS: daño 480 (vs 300), atracción en
    /// 680px (vs 550), esfera ×1,14, aura que devora MÁS RÁPIDO
    /// (ticks 4-18 vs 6-24) y MÁS FUERTE (42-76% vs 35-65%), Einstein
    /// final de 640px.
    /// </summary>
    public class AgujeroAbsolutoStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 480;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 50; Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<AgujeroAbsolutoBlackHoleProjectile>();
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
            // TOOLTIP CORTO — 2 líneas (el nombre vive en la localización).
            tooltips.Add(new TooltipLine(Mod, "D", "[c/50DCFF:El más allá de los Supremos — el gradiente del vacío, el cian estelar y LA CUARTA CORONA RÚNICA]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Atrae enemigos en 680px · devora balas · aura absoluta · muere en un Anillo de Einstein de 640px]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
