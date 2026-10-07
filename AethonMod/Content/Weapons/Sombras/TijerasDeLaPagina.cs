using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Weapons.Sombras
{
    /// <summary>
    /// TIJERASDELAPAGINA — v6.50.71 — ARMA NUEVA 2 (la que ocupa el lugar
    /// de la Mordida: conceptos COMPLETAMENTE distintos, nada de
    /// tentáculos).
    ///
    /// «Cada destino es una hoja: estas son las tijeras.» LAS TIJERAS DE
    /// LA PÁGINA: dos hojas curvas de sombra con FILO DE HUESO vuelan
    /// ABIERTAS al enemigo y TIJERETEAN — cada cierre es un TAJO BLANCO
    /// que corta un pedazo de la realidad y lo disuelve en bruma. Cortan
    /// lento pero hondo: cada tijeretada se lleva un bocado GORDO de vida.
    /// A 1 de vida: LA DEVORACIÓN — el festín estilo 12, EL CORTE FINAL.
    ///
    /// ARMA DE PRUEBA: LA BOLSA DE LAS SOMBRAS + receta de 5 madera
    /// (protocolo v6.14.2). 100% código: ni un sprite.
    /// </summary>
    public class TijerasDeLaPagina : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 190;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 38;
            Item.useAnimation = 38;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TijerasPaginaProjectile>();
            Item.shootSpeed = 16f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74;
        }

        public override bool CanUseItem(Player player)
        {
            // UN solo par de tijeras vivo por portador (el corte es un arte individual)
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<TijerasPaginaProjectile>())
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

            Projectile.NewProjectile(source, player.MountedCenter, rumbo * 16f, type, damage, knockback,
                player.whoAmI, presa != null ? presa.whoAmI + 1 : 0, 0);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
