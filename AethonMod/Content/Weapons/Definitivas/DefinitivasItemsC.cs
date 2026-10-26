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
    //  v6.50.66 — LA BOLSA DE LAS ARMAS DEFINITIVAS. Este archivo: LAS
    //  SENTENCIAS 9 A 12.
    // ======================================================================

    /// <summary>
    /// ARMA 9 — EL VÓRTICE DEVORADOR (la cara SOMBRAS de la bolsa — la
    /// receta del «Subsuming Vortex» de Calamity y el «Goomoire» de
    /// el dios antiguo): una semilla de tinta vuela al cursor, GERMINA en un
    /// vórtice negro-violeta que SUCCIONA a los enemigos hacia su
    /// garganta mientras sus zarpas los desgarran… y al colapsar, la
    /// IMPLOSIÓN: las almas de lo comido estallan hacia afuera.
    /// </summary>
    public class VorticeDevorador : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 60;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 60;
            Item.useAnimation = 60;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<VorticeDevoradorProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 70;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(-0.5f);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 rumbo = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX);
            Projectile.NewProjectile(source, player.MountedCenter, rumbo * 14f, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>
    /// ARMA 10 — EL RÉQUIEM ESTELAR (la receta del «The Set» de Thorium +
    /// los anillos de Fargo's): LA CAMPANA DE LUZ — tres anillos dorados
    /// que CRUZAN EL MUNDO expandiéndose desde el portador (1400 px, uno
    /// cada medio segundo, cada uno iluminando todo a su paso); el
    /// tercero es DOBLE y siembra la lluvia de estrellas del cierre.
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
            Item.mana = 60;
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
    /// ARMA 11 — LA IRA DEL CIELO (la receta del «BurstLightning» y el
    /// «GooLightning» de el dios antiguo, sobre el filamento de rayos de la casa):
    /// el cielo se CUBRE durante tres segundos y LOS OCHO JUICIOS caen —
    /// un rayo del cielo sobre cada enemigo cercano, con sus ramas
    /// chispeando a los vecinos; el ÚLTIMO es LA COLUMNA, el rayo doble
    /// que remata al más grande de todos.
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
            Item.mana = 75;
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

    /// <summary>
    /// ARMA 12 — EL TESTAMENTO DE AETHON: LA CORONA DE LA BOLSA (la
    /// receta del patrón FUSIÓN de Fargo's — el «Soul of Eternity» — y
    /// del «Dragon Pow» de Calamity: todas las armas de la familia
    /// fundidas en una). El libro abre sus páginas… y DICTA la sentencia
    /// que toca: CADA USO dispara una de las ONCE sentencias anteriores
    /// con su poder acrecentado (×1.3), rotando para siempre. Once
    /// arsenales en un solo objeto.
    /// </summary>
    public class TestamentoAethon : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 300;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 50;
            Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TestamentoAethonProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 50;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(-0.3f);
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<TestamentoAethonProjectile>())
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
