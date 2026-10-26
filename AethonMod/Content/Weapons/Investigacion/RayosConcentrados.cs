using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Investigacion;

namespace AethonMod.Content.Weapons.Investigacion
{
    /// <summary>
    /// RAYOSCONCENTRADOS — v6.54.0 — EL RAYO DE FUSIÓN DEL JUGADOR.
    ///
    /// La letra del usuario: «creamos nuestros propios FusionRay.png
    /// basandote en esos, los nuestros se llamaran rayos concentrados
    /// y crea un item llamado rayos concentrados».
    ///
    /// LA TÉCNICA (la investigación Task 74-a — el FusionRay del jefe,
    /// re-implementada 100 % casa): CUATRO TEXTURAS PROPIAS
    /// (RayoConcentrado_0..3.png — cuerpo ruidoso, bits de chispas,
    /// perfil de brillo, núcleo+falda) multiplicadas por VÉRTICE en
    /// CPU (RayosConcentradosLib — tML 2026.08 no carga .fx crudos),
    /// con el fade en lente, el wobble sinusoidal, el núcleo
    /// blanco-caliente por umbral, las chispas de doble muestreo hacia
    /// la punta y el ARCOÍRIS VIAJERO del ColorPrisma a lo largo del
    /// haz — la rebanada móvil del dios.
    ///
    /// EL CICLO DEL JEFE, COMPLETO: clic → LA CARGA (45 t: los
    /// resplandores de los soles rúnicos creciendo en la boca + la
    /// aguja telegráfica) → EL HAZ (150 t: el rayo gordo de 2000 px
    /// que respira, con daños por segmentos y sacudida de cámara) →
    /// la despedida.
    ///
    /// Las chispas: la letra manda — «se puede omitir y sustituir por
    /// los brillos de los soles runicos que es incluso superior a
    /// FusionRaySpark.png» (que era HUÉRFANA incluso en el original).
    /// </summary>
    public class RayosConcentrados : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName/Tooltip: Items.RayosConcentrados (hjson).
        }

        public override void SetDefaults()
        {
            Item.damage = 180;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.mana = 40;
            Item.autoReuse = false;         // UN ciclo por clic (la carga es un acto)
            Item.shoot = ModContent.ProjectileType<RayosConcentradosHeld>();
            Item.shootSpeed = 1f;
            Item.rare = ItemRarityID.Master;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = null;           // el zumbido lo lleva el Held
        }

        public override bool CanUseItem(Player player)
        {
            // UN solo rayo por portador (el patrón de la casa).
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<RayosConcentradosHeld>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 mira = Vector2.Normalize(Main.MouseWorld - player.MountedCenter);
            if (float.IsNaN(mira.X) || float.IsNaN(mira.Y)) mira = -Vector2.UnitY;
            Projectile.NewProjectile(source, player.MountedCenter, mira,
                type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            // LA VÍA DE REPUESTO de la casa (la tradición de las armas de
            // prueba): la madera de siempre — la concentración se practica gratis.
            CreateRecipe().AddIngredient(ItemID.Wood, 10)
                .Register();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine linea = new(Mod, "RayosConcentradosPrueba",
                Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Items.RayosConcentrados.Prueba"))
            {
                OverrideColor = new Color(255, 240, 180),
            };
            tooltips.Add(linea);
        }
    }
}
