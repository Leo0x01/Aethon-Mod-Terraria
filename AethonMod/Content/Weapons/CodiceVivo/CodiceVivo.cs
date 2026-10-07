using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.CodiceVivo;

namespace AethonMod.Content.Weapons.CodiceVivo
{
    /// <summary>
    /// CODICEVIVO — v6.50.72 — EL ARMA DE LA PRUEBA DEL USUARIO (completamente
    /// a parte de las demás: la petición literal).
    ///
    /// «Tengo un archivo png, y también tengo esa misma imagen creada con
    /// código, quiero que tomes la versión de código y la conviertas en el
    /// sprite de una nueva arma, esto como prueba y quiero que animes ese
    /// sprite.»
    ///
    /// v6.50.72 — LA CURA de «solo es la imagen fija subiendo en vertical
    /// sin animaciones ni nada… no tiene ningún ataque»:
    ///   · EL SPRITE nace OTRA VEZ de la matriz de codigo.txt (313×313, 26
    ///     colores) pero animado de verdad (tools/gen_codice_vivo_v65072.py):
    ///     EL ALIENTO (todo el libro respira), LA ONDA (anillo de tinta
    ///     violeta viajando desde el ojo) y EL PARPADEO COMPLETO (5/6/7:
    ///     media, rendija, media).
    ///   · EL ATAQUE SIEMPRE SE VE: al usarlo, el códice VUELA AL PUNTO DE
    ///     LA MIRA (donde clicaste: ahí se queda velando, ya no pasa de
    ///     largo hacia arriba) y escupa CUATRO ANDANADAS de TRES chispas
    ///     autoguiadas — con enemigo o SIN él (sin presa, dispara hacia la
    ///     mira) — y luego vuelve como bumerán.
    ///
    /// Entrega: LA BOLSA DEL PROBADOR del kit de pruebas (lección
    /// v6.14.2/.63 — cada arma nueva se registra en DOS sitios) + receta
    /// de 5 madera como vía alternativa.
    /// </summary>
    public class CodiceVivo : ModItem
    {
        public override void SetStaticDefaults()
        {
            // EL SPRITE ANIMADO: el item se comporta como las almas de
            // vanilla — ANIMA en el mundo (tirado) y en el inventario (la
            // animación se registra en AethonMod.PostSetupContent)
            ItemID.Sets.AnimatesAsSoul[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 85;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;   // pose de lanzar: la mira MANDA
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<CodiceVivoProjectile>();
            Item.shootSpeed = 17f;
            Item.mana = 14;
            Item.noMelee = true;
            Item.noUseGraphic = true;   // el códice VUELA de tus manos: el sprite vivo es el proyectil
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA MIRA: el ángulo hacia el cursor viaja en ai[0] — el códice
            // vuela AL PUNTO donde clicaste (a 280 px de ti en esa
            // dirección) y AHÍ se queda velando y disparando.
            Vector2 mira = Main.MouseWorld - player.MountedCenter;
            float ang = mira.LengthSquared() > 4f ? mira.ToRotation() : -MathHelper.PiOver2;
            Projectile.NewProjectile(source, player.MountedCenter, ang.ToRotationVector2() * 4f,
                type, damage, knockback, player.whoAmI, ang, 0f, 0f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
