using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Definitivas;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Definitivas
{
    // ======================================================================
    //  v6.54.6 — LA BOLSA DE LAS ARMAS DEFINITIVAS, LA REFORMA. Este
    //  archivo: LAS SENTENCIAS 3 Y 4 (el réquiem y la ira).
    // ======================================================================

    /// <summary>
    /// ARMA 3 — EL RÉQUIEM ESTELAR (la receta del arma de anillos del
    /// género, en SIETE capas): LA CAMPANA DE LUZ — tres
    /// anillos dorados que CRUZAN EL MUNDO expandiéndose desde el
    /// portador (1400 px, uno cada medio segundo), ahora CERRADOS DE
    /// VERDAD (la costura del loop enterrada bajo segmentos vivos — la
    /// cura del agujero), con LAS NOTAS cabalgando la circunferencia, EL
    /// BADAJO interior girando en contra, EL ECO del tercer anillo, el
    /// LATIDO central sincronizado al tañido, la palabra de cada campana
    /// (flash + onda + temblor) y LA LLUVIA DE ESTRELLAS del cierre.
    /// </summary>
    public class RequiemEstelar : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 220;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 70;
            Item.useAnimation = 70;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RequiemEstelarProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 10;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item27.WithPitchOffset(-0.2f);
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<RequiemEstelarProjectile>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>
    /// ARMA 4 — LA IRA DEL CIELO (el filamento de rayos de la casa en
    /// SIETE capas): la tormenta dura tres segundos — EL CIELO SE CUBRE
    /// con nubes vivas y bruma dorada, LOS RELÁMPAGOS AMBIENTALES
    /// recorren el cielo entre juicio y juicio, cada uno de LOS OCHO
    /// JUICIOS se ANUNCIA con una línea que desciende antes de caer con
    /// su canal doble y su columna de tres fundas + nova + anillo de
    /// choque, LAS RAMAS saltan a los vecinos chispeando… y el ÚLTIMO
    /// es LA COLUMNA FINAL: dos venas EN ESPIRAL alrededor del eje, con
    /// flash y el temblor mayor.
    /// </summary>
    public class IraDelCielo : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 280;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 110;
            Item.useAnimation = 110;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<IraDelCieloProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 10;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122.WithPitchOffset(-0.45f);
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<IraDelCieloProjectile>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
