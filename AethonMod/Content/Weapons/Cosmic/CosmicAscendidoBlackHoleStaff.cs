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
    /// CosmicAscendidoBlackHoleStaff — v6.18 — EL BASTÓN DEL CÓSMICO ASCENDIDO.
    ///
    /// La copia MEJORADA del bastón del Cósmico (CosmicBlackHoleStaff
    /// queda INTACTO): lanza el agujero negro COSMICO ASCENDIDO — el
    /// Cósmico elevado con la librería de rayos LightningCore: TORMENTA
    /// de 4-6 rayos ramificados escapando del anillo (doble tira
    /// cuerpo rojo-naranja + núcleo ámbar), CORONAS DE DESCARGA
    /// abrazando el horizonte (~11 Hz), DOBLE ANILLO DE BANDAS (el eco
    /// interior a 0.62× contrarrotando), DOBLE CÍRCULO DE RUNAS (8
    /// doradas + 5 ámbar) y JETS POLARES con rayo interior. Paleta
    /// ROJO-NARANJA incandescente.
    ///
    /// Daño 200 (el Cósmico base es 150). Tooltip CORTO v6.18.
    /// </summary>
    public class CosmicAscendidoBlackHoleStaff : ModItem
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
            Item.shoot = ModContent.ProjectileType<CosmicAscendidoBlackHoleProjectile>();
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
            tooltips.Add(new TooltipLine(Mod, "D", "[c/FF7A55:El Cósmico elevado — tormenta de rayos ramificados, doble anillo de bandas y jets polares]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Atrae enemigos en 450px · devora balas · muere en un Anillo de Einstein]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
