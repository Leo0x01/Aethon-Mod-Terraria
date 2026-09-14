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
    /// EclipsePrimordialStaff — v6.26 — EL SOL DE LOS 20 ANILLOS.
    ///
    /// v6.26 — LA ORDEN DEL USUARIO: "el bastón del eclipse primordial
    /// cámbialo, esta nueva versión será el sol de 20 anillos, y la
    /// mezcla de todos los agujeros negros rúnicos".
    ///
    /// EL SOL DE LOS 20 ANILLOS — LA MEZCLA DE TODOS LOS AGUJEROS NEGROS
    /// RÚNICOS: el núcleo ya no es una luna negra — es el Sol XX ENTERO
    /// (cuerpo solar + 20 anillos + gran sellado + cometa, ×1.30) con
    /// TODAS las firmas orbitando por fuera:
    ///   · del SUPREMO   → los TRES círculos rúnicos concéntricos de pie
    ///                      con perlas (blanco/dorado/violeta) entrelazados
    ///                      con el anillo 20 del sol.
    ///   · del CÓSMICO   → el anillo de bandas (20 zonas de brillo viajando).
    ///   · del OLVIDO    → los brazos espirales con flujo hacia adentro,
    ///                      ahora ALIMENTANDO al sol.
    ///   · de la BRUMA   → el halo de nubes + el aliento + las volutas
    ///                      cayendo al cuerpo solar (acreción invertida).
    ///   · del UMBRAL    → el anillo de fotones + corredores.
    ///   · del AURORA    → el gradiente morado→azul→dorado exterior.
    ///   · de STORMLIB   → la corona de descarga + rayos fugitivos.
    ///   · de LUMENLIB   → la luz prismática radiando.
    ///
    /// La muerte es LA NOVA DEL ECLIPSE: el anillo de Einstein + la nova
    /// rúnica del sol ×1.6 + la nova visual con TODAS las librerías.
    /// </summary>
    public class EclipsePrimordialStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 700;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 55; Item.useAnimation = 55;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<EclipsePrimordialProjectile>();
            Item.shootSpeed = 6f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item20;
            Item.value = Item.buyPrice(gold: 50);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/FFD080:EL SOL DE LOS 20 ANILLOS — la fusión del Sol XX con TODOS los agujeros negros rúnicos]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Tres círculos rúnicos entrelazados · anillo de bandas · brazos que alimentan al sol · halo de bruma · gradiente aurora · corona de descarga · luz prismática]"));
            tooltips.Add(new TooltipLine(Mod, "D3",
                "[c/78788C:Atracción gravitacional de 600px · muere en LA NOVA DEL ECLIPSE (Anillo de Einstein + nova rúnica ×1.6)]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
