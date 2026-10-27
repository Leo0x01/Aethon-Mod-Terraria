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
    //  v6.54.6 — LA BOLSA DE LAS ARMAS DEFINITIVAS, LA REFORMA: LAS
    //  CINCO SENTENCIAS que el usuario eligió (el prisma, el partidor, el
    //  réquiem, la ira… y el testamento que las dicta), cada una con SUS
    //  SIETE CAPAS de efectos. Este archivo: LAS DOS PRIMERAS.
    // ======================================================================

    /// <summary>
    /// ARMA 1 — EL PRISMA PRIMORDIAL: EL ATAQUE DEFINITIVO DEL MOD. La
    /// evolución del «Yharim's Crystal» de Calamity con LA RECETA COMPLETA
    /// del rayo de fusión estudiado en la fuente: siete haces del
    /// espectro brotan alrededor del portador girando y CONVERGEN en un
    /// foco que se acerca a medida que el prisma se calienta — del foco
    /// sale EL HAZ CENTRAL de 2400 px con ANCHO CRECIENTE (la fórmula
    /// exacta: nace fino, engorda hacia el fondo y RESPIRA), cuatro
    /// fundas + la vena blanca, tres pulsos que viajan, la lluvia
    /// cromática con offset de color por distancia, el velo prismático,
    /// los cuatro halos apilados del foco y el resplandor de la punta.
    /// </summary>
    public class PrismaPrimordial : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 160;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 14;
            Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PrismaPrimordialProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 14;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item13;
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<PrismaPrimordialProjectile>())
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
    /// ARMA 2 — EL PARTIDOR DE MUNDOS (la receta del «Universe Splitter»
    /// de Calamity: un clic = un pilar, hecha ritual en SIETE capas): el
    /// velo de la contención, el telegrafo con sus cinco runas y sus
    /// chispas convergentes, LA COLUMNA de 260 px con sus cuatro fundas,
    /// el cráter de luz con los halos apilados, LAS TRES ONDAS de
    /// choque, LAS GRIETAS que se abren en el suelo a los lados y la
    /// hemorragia de chispas con gravedad de verdad.
    /// </summary>
    public class PartidorDeMundos : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 600;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 90;
            Item.useAnimation = 90;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<PartidorDeMundosProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 80;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122.WithPitchOffset(-0.3f);
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<PartidorDeMundosProjectile>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // el pilar cae sobre el cursor (el proyectil vuela hasta él y ancla)
            Vector2 destino = Main.MouseWorld;
            Vector2 rumbo = destino - player.MountedCenter;
            float dist = rumbo.Length();
            rumbo = dist > 1f ? rumbo / dist : Vector2.UnitX;
            Projectile.NewProjectile(source, player.MountedCenter, rumbo * MathHelper.Clamp(dist, 6f, 900f),
                type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
