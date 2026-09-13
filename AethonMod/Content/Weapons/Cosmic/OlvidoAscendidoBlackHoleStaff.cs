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
    /// OlvidoAscendidoBlackHoleStaff — v6.18 — EL BASTÓN DEL OLVIDO ASCENDIDO.
    ///
    /// La copia MEJORADA del bastón del Olvido (OlvidoBlackHoleStaff
    /// queda INTACTO): lanza el agujero negro OLVIDO ASCENDIDO — el
    /// Olvido elevado con la librería de rayos LightningCore: ARCOS DEL
    /// VACÍO (3 coronas eléctricas morado-azules alrededor del
    /// horizonte, ~9 Hz), RAYOS ESPIRALES que siguen los brazos y
    /// espiralan hacia el núcleo (anclas sobre la espiral + JitterPath),
    /// brazos espirales REFORZADOS (3×18, flujo rápido), nebulosa fBm
    /// más rica y fotones-rayo recorriendo el anillo. Paleta
    /// MORADO-AZUL eléctrico.
    ///
    /// Daño 200 (el Olvido base es 150). Tooltip CORTO v6.18.
    /// </summary>
    public class OlvidoAscendidoBlackHoleStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 200;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 50; Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<OlvidoAscendidoBlackHoleProjectile>();
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
            tooltips.Add(new TooltipLine(Mod, "D", "[c/A78BFF:El Olvido elevado — arcos del vacío y rayos espiralando hacia el núcleo morado-azul]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Atrae enemigos en 450px · devora balas · muere en un Anillo de Einstein]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
