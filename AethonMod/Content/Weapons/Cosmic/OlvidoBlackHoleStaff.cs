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
    /// OlvidoBlackHoleStaff — v6.15 — EL BASTÓN DEL AGUJERO NEGRO DEL OLVIDO.
    ///
    /// El 4to agujero negro del mod, ahora 100% CREADO POR CÓDIGO
    /// (directiva del usuario: "el agujero negro no puede ser creado por
    /// sprite"): anillo de plasma púrpura/rosa con hotspot incandescente y
    /// turbulencia viva, brazos espirales, rayos eléctricos violeta dentro
    /// del vacío y rosa emergiendo del anillo, runas doradas orbitando en
    /// círculo, ondas de distorsión, nebulosas difusas y aura mística.
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
            tooltips.Add(new TooltipLine(Mod, "T", "[c/9C4DFF:═══ AGUJERO NEGRO DEL OLVIDO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B870FF:100% creado por código — anillo de plasma púrpura/rosa con zonas incandescentes y sombras vivas, brazos espirales de vórtice y corredores de fotones orbitando]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:El vacío absoluto devora la luz mientras rayos eléctricos violeta danzan en su interior — y un círculo de RUNAS DORADAS gira lento a su alrededor, entre ondas de distorsión, nebulosas difusas y partículas emanando del poder arcano]"));
            tooltips.Add(new TooltipLine(Mod, "D3", "[c/78788C:GIGANTE: esfera de 52px, arte de ~7R. Atrae enemigos en un radio de 450px; su aura abraza el disco y machaca más rápido cerca del centro]"));
            tooltips.Add(new TooltipLine(Mod, "D4", "[c/9C4DFF:Devora las balas enemigas al cruzar el horizonte — y al final nace el ANILLO DE EINSTEIN que curva el fondo del juego]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
