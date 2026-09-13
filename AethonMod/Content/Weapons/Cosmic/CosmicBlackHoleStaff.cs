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
    /// CosmicBlackHoleStaff — v6.16 — EL BASTÓN DEL AGUJERO NEGRO CÓSMICO.
    ///
    /// EL 5to agujero negro del mod, NACIDO DEL SCRIPT UNITY del usuario
    /// (CosmicBlackHole.cs + shader "Custom/CosmicRing"): anillo
    /// energético MAGENTA (1.0, 0.2, 0.8) donde VEINTE BANDAS DE BRILLO
    /// recorren el vórtice a la velocidad exacta del shader
    /// (sin(uv·20 + t·5)), rotación de 20°/s, rayos eléctricos
    /// (lightningParticles), runas doradas flotando (runeParticles),
    /// distorsión sinusoidal global y núcleo de negro absoluto
    /// (coreSphere) — más todo lo que faltaba: aura oscura, nebulosas,
    /// ecos del anillo, corredores de fotones, destellos polares, ondas
    /// de distorsión y partículas radiales. 100% CÓDIGO.
    ///
    /// Hermano del BlackHoleStaff, Crimson, Fusión y Olvido — todos
    /// quedan INTACTOS.
    /// </summary>
    public class CosmicBlackHoleStaff : ModItem
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
            Item.shoot = ModContent.ProjectileType<CosmicBlackHoleProjectile>();
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
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FF33CC:═══ AGUJERO NEGRO CÓSMICO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/FF66D9:100% creado por código — nacido del script Unity: anillo energético MAGENTA con las VEINTE BANDAS del shader sin(uv·20 + t·5) recorriéndolo, rotando a 20°/s]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:El núcleo es negro absoluto mientras RAYOS ELÉCTRICOS violeta danzan dentro y magenta escapan del anillo — y RUNAS DORADAS de circuito flotan orbitando, entre ecos de resonancia, corredores de fotones, destellos polares y ondas de distorsión sinusoidal]"));
            tooltips.Add(new TooltipLine(Mod, "D3", "[c/78788C:GIGANTE: esfera de 50px, arte de ~7R. Atrae enemigos en un radio de 450px; su aura abraza el anillo y machaca más rápido cerca del centro]"));
            tooltips.Add(new TooltipLine(Mod, "D4", "[c/FF33CC:Devora las balas enemigas al cruzar el horizonte — y al final nace el ANILLO DE EINSTEIN que curva el fondo del juego]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
