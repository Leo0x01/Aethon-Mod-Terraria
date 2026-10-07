using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Weapons.Sombras
{
    /// <summary>
    /// MANODELESCRIBA — v6.50.71 — ARMA NUEVA 1 (la que ocupa el lugar del
    /// Azote: el usuario jubiló a la familia de tentáculos .69 — «las 3
    /// nuevas armas se ven mal» — y pidió conceptos COMPLETAMENTE distintos).
    ///
    /// «Todo libro necesita una mano que escriba.» LA MANO DEL ESCRIBA:
    /// una garra de sombra COLOSAL nace de la sombra del portador y CAMINA
    /// sobre sus cinco dedos —como araña, como escriba— hasta el enemigo;
    /// la palma lleva UN OJO que no parpadea, y al alcanzarlo los dedos lo
    /// ENCIERRAN, lo APRIETAN y lo empujan al CHARCO que se abre bajo sus
    /// pies. A 1 de vida: LA DEVORACIÓN — el festín estilo 11,
    /// EL PUÑO DEL ESCRIBA.
    ///
    /// ARMA DE PRUEBA (no toca el grimorio): se entrega en LA BOLSA DE LAS
    /// SOMBRAS del kit de pruebas (lección v6.14.2/.63 — cada arma nueva se
    /// registra en DOS sitios: aquí y en la bolsa); la receta de 5 madera
    /// queda como vía alternativa. 100% código: ni un sprite.
    /// </summary>
    public class ManoDelEscriba : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 110;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ManoEscribaProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool CanUseItem(Player player)
        {
            // UNA sola mano viva por portador (el escriba no tiene dos)
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<ManoEscribaProjectile>())
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
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
