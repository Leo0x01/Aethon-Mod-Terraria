using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.CodiceVivo;

namespace AethonMod.Content.Weapons.CodiceVivo
{
    /// <summary>
    /// CODICEVIVO — v6.50.71 — EL ARMA DE LA PRUEBA DEL USUARIO (completamente
    /// a parte de las demás: la petición literal).
    ///
    /// «Tengo un archivo png, y también tengo esa misma imagen creada con
    /// código, quiero que tomes la versión de código y la conviertas en el
    /// sprite de una nueva arma, esto como prueba y quiero que animes ese
    /// sprite.»
    ///
    /// EL SPRITE NACE DE CÓDIGO: la matriz de píxeles del usuario
    /// (codigo.txt — 313×313, 26 colores) se renderizó y se ANIMÓ por
    /// código (tools/gen_codice_vivo_v65071.py): 8 frames con EL PULSO DE
    /// ENERGÍA (una onda de brillo recorre la rampa violeta del grimorio)
    /// y EL PARPADEO (el ojo central cierra y abre). Esos frames viven en
    /// DOS sprites animados:
    ///
    ///   · EL ITEM (48×384, 8 frames): registrado con
    ///     Main.RegisterItemAnimation + AnimatesAsSoul — el icono ANIMA EN
    ///     EL INVENTARIO (la tinta late, el ojo parpadea) y el objeto
    ///     tirado también (como las almas de vanilla).
    ///   · EL PROYECTIL (128×1024, 8 frames): el códice VUELA.
    ///
    /// EL ARMA: al usarlo, el códice SALTA de tus manos, vuela al cursor,
    /// VELA flotando sobre el campo (su ojo buscando presas) y dispara
    /// TRES VOLAS de chispas violetas autoguiadas a los enemigos cercanos;
    /// luego VUELVE a ti como un bumerán. Arma mágica de la familia
    /// NINGUNA: es la prueba del sprite-de-código, aparte de las demás.
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
            Item.useTime = 34;
            Item.useAnimation = 34;
            Item.useStyle = ItemUseStyleID.HoldUp;
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
            // al cursor (o al rumbo del disparo): el códice despega de las manos
            Vector2 rumbo = velocity.LengthSquared() > 0.1f
                ? velocity.SafeNormalize(Vector2.UnitX)
                : (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX);
            Projectile.NewProjectile(source, player.MountedCenter, rumbo * 17f, type,
                damage, knockback, player.whoAmI, 0f, 0f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
