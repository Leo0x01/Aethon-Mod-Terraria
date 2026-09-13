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
    /// FusionBlackHoleStaff — v6.14 — EL BASTÓN DEL AGUJERO NEGRO DE FUSIÓN.
    ///
    /// El 3er agujero negro del mod (petición del usuario): "la fusión del
    /// agujero negro del vacío con el agujero negro base". Ambos renders
    /// originales componen en el mismo centro: el Gargantua de marcha de
    /// luz del BASE (detrás, con su disco naranja lensado) y el VÓRTICE
    /// OBLIVION del VACÍO con sus 7 capas de personalidad (delante a
    /// 0.68×) — FUEGO y VACÍO en un solo cuerpo.
    /// </summary>
    public class FusionBlackHoleStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 130;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 50; Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<FusionBlackHoleProjectile>();
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
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FF5522:═══ AGUJERO NEGRO DE FUSIÓN ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/FFAA66:LA FUSIÓN LITERAL: el Gargantua de marcha de luz del agujero BASE (detrás, con su disco naranja lensado por el RealBlackHoleShader de 75 pasos) y el VÓRTICE OBLIVION del agujero DEL VACÍO con sus 7 capas de personalidad (delante) — FUEGO y VACÍO en un solo cuerpo]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Esfera de negro profundo abrazada por el anillo naranja lensado del Gargantua, envuelta en las hojas de plasma carmesí del vacío: ondas de espacio-tiempo, pulsos de fotones, chorros relativistas, corrientes de materia, llamaradas y arcos de Einstein]"));
            tooltips.Add(new TooltipLine(Mod, "D3", "[c/78788C:GIGANTE. Atrae enemigos en un radio de 480px (la suma de ambas masas); su aura abraza el disco y machaca más rápido cerca del centro]"));
            tooltips.Add(new TooltipLine(Mod, "D4", "[c/FF5522:Devora las balas enemigas al cruzar el horizonte — y al final nace el ANILLO DE EINSTEIN que curva el fondo del juego]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
