using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Weapons.Cosmic
{
    public class BlackHoleStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 100;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 60; Item.useAnimation = 60;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BlackHoleProjectile>();
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
            tooltips.Add(new TooltipLine(Mod, "T", "[c/9600FF:═══ AGUJERO NEGRO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Lensing gravitacional real de 75 pasos + lente que distorsiona el propio fondo del juego alrededor del horizonte de sucesos (la lente va siempre DETRÁS del agujero y sus efectos)]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Disco de acreción de estelas orbitando + succión espiral con partículas de colores + devora el polvo del entorno]"));
            tooltips.Add(new TooltipLine(Mod, "D3", "[c/78788C:Atrae enemigos en un radio de 450px; su AURA DE DAÑO crece con él (35%→65% del daño a lo largo de su vida) y al explotar lanza una onda expansiva cromática que distorsiona el espacio]"));
            tooltips.Add(new TooltipLine(Mod, "D4", "[c/9600FF:Al FINAL de la explosión nace el ANILLO DE EINSTEIN: un lente gravitacional anular que se expande curvando el fondo del juego]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    public class SunStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 80;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 50; Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SunProjectile>();
            Item.shootSpeed = 6f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FFD700:═══ SOL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Estrella de plasma de 10 segundos con GLOW CORONAL que nace tenue y CRECE LENTO, sincronizado con su ciclo de vida y su tamaño (gigante roja ×1.85)]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Corona de plasma orbitando + viento solar radial + prominencias periódicas + destellos luminosos + supernova que carga desde el segundo 7]"));
            tooltips.Add(new TooltipLine(Mod, "D3", "[c/78788C:Su AURA DE DAÑO se extiende por fuera de la estrella y CRECE con ella (inflama con quemadura que dobla en gigante); atrae a los rivales con su gravedad]"));
            tooltips.Add(new TooltipLine(Mod, "D4", "[c/FF7000:La explosión final libera 3 ondas expansivas de fuego + una onda de lente gravitacional con RGB ligero — cada una hace daño por cada 0.1 s]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
