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
    /// OlvidoBlackHoleStaff — v6.14 — EL BASTÓN DEL AGUJERO NEGRO DEL OLVIDO.
    ///
    /// El 4to agujero negro del mod (petición del usuario): "100% exacto a
    /// la referencia". Su arte fue EXTRAÍDO píxel a píxel de la imagen de
    /// referencia original (Ancients Awakened — Regicide, Oblivion God of
    /// the Void) tras 10 rondas de validación visual y 130 rondas de
    /// optimización automatizada — el vórtice entero, la esfera con su
    /// estrella y su rayo, el vacío rojizo que lo envuelve.
    ///
    /// Hermano del BlackHoleStaff (base) y del CrimsonBlackHoleStaff (el
    /// agujero del vacío) — ambos quedan INTACTOS.
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
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FF0044:═══ AGUJERO NEGRO DEL OLVIDO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/FF4488:100% EXACTO a la referencia: su arte fue extraído píxel a píxel de la imagen original — el vórtice de plasma carmesí con su anillo de fotones, sus brazos espirales y su aguja, tal cual]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Esfera de NEGRO PROFUNDO con la estrella rosa y el rayo púrpura en su interior, envuelta en el vacío rojizo de la referencia — y VIVA: pulsos de fotones recorriendo el anillo, llamaradas del hotspot, chispas cayendo y velos girando]"));
            tooltips.Add(new TooltipLine(Mod, "D3", "[c/78788C:GIGANTE: 811px de envergadura. Atrae enemigos en un radio de 450px; su aura abraza el disco y machaca más rápido cerca del centro]"));
            tooltips.Add(new TooltipLine(Mod, "D4", "[c/FF0044:Devora las balas enemigas al cruzar el horizonte — y al final nace el ANILLO DE EINSTEIN que curva el fondo del juego]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
