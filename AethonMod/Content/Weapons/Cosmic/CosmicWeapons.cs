using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Weapons.Cosmic
{
    // ==================================================================
    //  v6.51.0 — EL AGUJERO NEGRO NORMAL MURIÓ (la letra del usuario:
    //  «quiero que borres al agujero negro normal del paquete de armas,
    //  solo deben estar los agujeros negros rúnicos para evitar
    //  confusión, el agujero negro normal fue el primero que se creó,
    //  el agujero negro realista por así decirlo»). EL SOL VIVE.
    // ==================================================================

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
