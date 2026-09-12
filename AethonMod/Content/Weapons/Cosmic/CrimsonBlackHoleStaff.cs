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
    /// CrimsonBlackHoleStaff — v6.05 — EL AGUJERO NEGRO DEL VACÍO.
    ///
    /// Hermano del BlackHoleStaff (que queda INTACTO): misma física
    /// gravitacional y el MISMO render de marcha de luz del agujero
    /// funcional (RealBlackHoleShader, copia exacta) con los parámetros
    /// recalibrados según la referencia — disco de acreción carmesí-fucsia
    /// MÁS GRANDE, horizonte un poco más pequeño, Doppler beaming (el lado
    /// que se acerca brilla más), anillo de fotones rosa pálido y la
    /// animación más viva (el plasma hierve más rápido y el plano del
    /// disco precesiona).
    /// </summary>
    public class CrimsonBlackHoleStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 110;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 50; Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<CrimsonBlackHoleProjectile>();
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
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FF0055:═══ AGUJERO NEGRO DEL VACÍO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/FF66AA:EXACTAMENTE como la referencia: lente que distorsiona el propio fondo del juego alrededor del horizonte de sucesos]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Disco de acreción CARMESÍ-FUCSIA de materia viva con DOPPLER BEAMING (el lado que se acerca brilla ~δ³ más) + anillo de fotones ROSA PÁLIDO + lente que distorsiona el fondo alrededor del horizonte]"));
            tooltips.Add(new TooltipLine(Mod, "D3", "[c/78788C:Atrae enemigos en un radio de 450px; su AURA DE DAÑO crece con él (35%→65% del daño) y machaca más rápido cuanto más cerca del centro]"));
            tooltips.Add(new TooltipLine(Mod, "D4", "[c/FF0055:Devora las balas enemigas al cruzar el horizonte — y al final nace el ANILLO DE EINSTEIN que curva el fondo del juego]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
