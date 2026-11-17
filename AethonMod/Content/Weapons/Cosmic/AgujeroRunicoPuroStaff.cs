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
    /// AgujeroRunicoPuroStaff — v6.147 — EL BASTÓN DEL AGUJERO RÚNICO PURO.
    ///
    /// La letra del usuario: «el Agujero Rúnico Puro debes darselo al
    /// jugador en forma de arma no lo veo» + «lo quiero como baston al
    /// igual que estan los otros bastones, solo tienes que darselo al
    /// jugador, metelo en la bolsa de agujeros negros runicos»: el
    /// agujero PURO de la v6.146 (el anillo de 12 runas vivas en
    /// contrarrotación, SIN polvo) vivía SOLO dentro del Rayo
    /// Convergente (la ruleta de la fusión, su opción 4) — el jugador
    /// no podía lanzarlo. ESTE es su bastón: el patrón exacto de los
    /// diez hermanos de la bolsa (HoldUp · disparo único · autoReuse).
    /// La receta de 5 maderas es el respaldo de siempre — la bolsa es
    /// la vía del kit.
    /// </summary>
    public class AgujeroRunicoPuroStaff : ModItem
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
            Item.shoot = ModContent.ProjectileType<AgujeroRunicoPuroProjectile>();
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
            // TOOLTIP CORTO (la regla de la casa desde v6.18).
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B48AFF:El agujero PURO — el anillo de 12 runas vivas en contrarrotación, sin polvo]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Atrae enemigos en 450px · devora balas · muere en un Anillo de Einstein]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
