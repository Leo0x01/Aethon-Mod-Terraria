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
    /// CosmicBlackHoleStaff — v6.18 — EL BASTÓN DEL AGUJERO NEGRO CÓSMICO.
    ///
    /// Nacido del script Unity del usuario (CosmicBlackHole.cs + shader
    /// "Custom/CosmicRing"): anillo energético (hoy ROJO-NARANJA — recolor
    /// v6.18 para diferenciarlo del Olvido) donde VEINTE BANDAS DE BRILLO
    /// recorren el vórtice a la velocidad exacta del shader
    /// (sin(uv·20 + t·5)), rotación de 20°/s, rayos eléctricos escapando
    /// del anillo, runas doradas flotando, distorsión sinusoidal global y
    /// núcleo de negro absoluto LIMPIO (v6.18: sin partículas en el centro).
    ///
    /// Uno de los 4 agujeros DEFINITIVOS: UMBRAL · BRUMA · CÓSMICO · OLVIDO.
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
            // v6.18: TOOLTIP CORTO — la ventana de info ya no es un muro de
            // texto (el nombre ahora vive en la localización, arriba).
            tooltips.Add(new TooltipLine(Mod, "D", "[c/FF7A55:100% por código — anillo rojo-naranja de bandas vivas, runas doradas y tormenta eléctrica]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Atrae enemigos en 450px · devora balas · muere en un Anillo de Einstein]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
