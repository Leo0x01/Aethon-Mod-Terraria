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
    //  SENTENCIAS 5 A 8.
    // ======================================================================

    /// <summary>
    /// ARMA 5 — EL PARTIDOR DE MUNDOS (la receta del «Universe Splitter»
    /// de Calamity: un clic = un pilar). La pantalla CONTIENE la respiración
    /// (el telegrafo), el cielo se parte y UNA COLUMNA DE LUZ VERTICAL de
    /// 260 px de ancho CAE sobre el cursor dividiendo el mundo — destello
    /// blanco, doble onda de choque desde la base y el temblor más fuerte
    /// de la bolsa.
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

    /// <summary>
    /// ARMA 6 — LA GUADAÑA DEL VIEJO DIOS (la receta de la «Scythe of the
    /// Old God» de el dios antiguo): el COMBO DE LOS TRES TAJOS — media luna,
    /// media luna espejada MÁS GRANDE, y la CRUZ GIGANTE que remata con
    /// onda de choque y almas. Cada arco de la guadaña viaja media
    /// pantalla cortando TODO lo que toca.
    /// </summary>
    public class GuadanaViejoDios : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 300;
            Item.DamageType = DamageClass.Melee;
            Item.width = 44;
            Item.height = 44;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<GuadanaViejoDiosProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 0;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item71.WithPitchOffset(-0.4f);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // el combo vive en ai[1] (0 → 1 → 2 → 0…): el tercer tajo ES la cruz
            Vector2 rumbo = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX);
            Projectile.NewProjectile(source, player.MountedCenter, rumbo * 6f, type, damage, knockback,
                player.whoAmI, 0f, 0f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>
    /// ARMA 7 — EL SISTEMA DE AETHON (la receta de la «el dios antiguoga» de el dios antiguo
    /// + el «Ultima Thule» de Stars Above): tu PROPIO sistema solar —
    /// CINCO cuerpos en órbita (el Sol, la Luna, el Cometa, la Estrella y
    /// el Vacío) que bombardean solos a los enemigos cercanos. CLIC
    /// DERECHO: EL BOMBARDEO — los cinco se lanzan al cursor y DETONAN.
    /// </summary>
    public class SistemaAethon : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 80;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SistemaAethonProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 30;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item44;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<SistemaAethonProjectile>())
                {
                    // CLIC DERECHO con el sistema vivo: LA ORDEN DE BOMBARDEO
                    if (player.altFunctionUse == 2 && p.ai[1] == 0f)
                    {
                        p.ai[1] = 1f;
                        p.netUpdate = true;
                        DefinitivasLib.Sonar(SoundID.Item122.WithPitchOffset(0.25f), player.Center);
                    }
                    return false;
                }
            }
            return player.altFunctionUse != 2;      // clic derecho sin sistema: nada
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
    /// ARMA 8 — LA CONSTELACIÓN DEL JUICIO (la receta de los «Star Signs»
    /// del Stellar Geliath de el dios antiguo): SIEMBRA hasta SIETE estrellas
    /// doradas donde cliques — se CONECTAN solas con hilos de luz y,
    /// cuando la figura cierra (la séptima o un clic más), CADA ESTRELLA
    /// recibe su rayo del cielo y los arcos saltan de estrella a estrella
    /// juzgando todo lo que la figura abraza.
    /// </summary>
    public class ConstelacionJuicio : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 350;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ConstelacionJuicioProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 25;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item4.WithPitchOffset(0.15f);
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<ConstelacionJuicioProjectile>())
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
