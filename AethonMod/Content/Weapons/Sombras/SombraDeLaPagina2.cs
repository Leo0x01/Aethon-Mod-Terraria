using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Weapons.Sombras
{
    /// <summary>
    /// SOMBRADELAPAGINA2 — v6.51.12 — ARMA 3-BIS: LA SOMBRA DE LA PÁGINA II
    /// (la letra del usuario: «tambien quiero que crees un arma que copia
    /// a la sombra de la pagina, crea la sombra de la pagina 2 y a esta
    /// segunda version quiero que le mejores las textura, usa nuestras
    /// librerias y nuestros iris, tambien hacerlo lo mas detallado
    /// posible»).
    ///
    /// LA MISMA SOMBRA, EL SEGUNDO TOMO: el mismo festín de la Sombra de
    /// la Página (emerge del portador → caza al jefe a cualquier
    /// distancia → muerde y drena → a 1 HP el motor de devoración,
    /// estilo 3)… con EL CUERPO DOBLE Y DETALLADO DE VERDAD:
    ///   · LA DOBLE HÉLICE — DOS tentáculos de tinta trenzados alrededor
    ///     de una espina central (la página DOBLADA: la sombra del libro
    ///     que aprendió a cerrarse en dos).
    ///   · LOS IRIS REALES — los ojos del cuerpo ya no son puntos de
    ///     quad: son LOS SPRITES DEL ESCUADRÓN (el Ojo Eterno con SU
    ///     iris que MIRA y SUS párpados — «nuestros iris») repartidos
    ///     por las dos hebras.
    ///   · LOS RAYOS PERLIN — descargas del Trueno Perlin real
    ///     (StormLib) chispeando ENTRE las dos hebras mientras cazan.
    ///   · LAS GARRAS — la boca es un ANILLO DE GARRAS de 3 segmentos
    ///     (SombrasLib.Garra) que se abre y CIERRA DE GOLPE al morder.
    ///   · Y al morder: EL DEVORADOR v2 con la CORONA DE 9 GARRAS
    ///     (el borde dentado del festín del arma 4) + la herida doble.
    /// TODO con las librerías de la casa (SombrasLib + StormLib +
    /// VFXCore + BrumaFX) — ni un sprite propio: puro código, máximo
    /// detalle.
    /// </summary>
    public class SombraDeLaPagina2 : ModItem
    {
        public override string Texture => "AethonMod/Content/Weapons/Sombras/SombraDeLaPagina";

        public override void SetStaticDefaults()
        {
            // DisplayName y Tooltip viven en la localización (la regla de
            // la casa) — Items.SombraPagina2.
        }

        public override void SetDefaults()
        {
            Item.damage = 260;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 46;
            Item.useAnimation = 46;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SombraPagina2Projectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool CanUseItem(Player player)
        {
            // UNA sola fauce viva por portador (el festín es un arte individual)
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    (p.type == ModContent.ProjectileType<SombraPagina2Projectile>() ||
                     p.type == ModContent.ProjectileType<SombraPaginaProjectile>()))
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA PRESA: el jefe más cercano (a CUALQUIER distancia); sin
            // jefes, el enemigo más cercano en 1800 px (la chusma también cena)
            NPC presa = SombraDeLaPagina.BuscarPresa(player.Center, true)
                ?? SombraDeLaPagina.BuscarPresa(player.Center, false);
            Vector2 rumbo = presa != null
                ? (presa.Center - player.MountedCenter).SafeNormalize(velocity.LengthSquared() > 0.1f ? velocity : Vector2.UnitX)
                : velocity.LengthSquared() > 0.1f ? velocity.SafeNormalize(Vector2.UnitX) : Vector2.UnitX;

            Projectile.NewProjectile(source, player.MountedCenter, rumbo * 14f, type, damage, knockback,
                player.whoAmI, presa != null ? presa.whoAmI + 1 : 0, 0);
            return false;
        }

        public override void AddRecipes()
        {
            // LA EVOLUCIÓN DEL TOMO (la vía de pruebas de la casa): el
            // primer tomo + la madera de siempre — la página se DOBLA.
            CreateRecipe().AddIngredient(ModContent.ItemType<SombraDeLaPagina>(), 1)
                .AddIngredient(ItemID.Wood, 10)
                .Register();
        }
    }
}
