using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Weapons.Sombras
{
    /// <summary>
    /// PAGINAARRANCADA — v6.50.71 — ARMA NUEVA 3 (la que ocupa el lugar de
    /// la Cría: conceptos COMPLETAMENTE distintos, nada de tentáculos).
    ///
    /// «Lo que el libro escribe, el libro lo arranca.» LA PÁGINA
    /// ARRANCADA: un MARCO de sombra se traza alrededor del enemigo —con
    /// SUS RENGLONES y su MARGEN ROJO, como una hoja de cuaderno— y CADA
    /// TIEMBLA se ciñe más: un arranque, un desgarro, un bocado. Cuando
    /// la hoja ya no puede encogerse, lo que quedó dentro ES PÁRRAFO: la
    /// página se arranca y se lo lleva al libro. A 1 de vida: LA
    /// DEVORACIÓN — el festín estilo 13, EL ARREBATO.
    ///
    /// ARMA DE PRUEBA: LA BOLSA DE LAS SOMBRAS + receta de 5 madera
    /// (protocolo v6.14.2). 100% código: ni un sprite.
    /// </summary>
    public class PaginaArrancada : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 230;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 55;
            Item.useAnimation = 55;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PaginaArrancadaProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74;
        }

        public override bool CanUseItem(Player player)
        {
            // UNA sola página viva por portador (el libro arranca de a una hoja)
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<PaginaArrancadaProjectile>())
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
