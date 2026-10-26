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
    //  v6.50.66 — LA BOLSA DE LAS ARMAS DEFINITIVAS («El Veredicto de
    //  Aethon»): DOCE sentencias nacidas de la investigación profunda de
    //  los jefes y armas finales de los mods más populares y hermosos
    //  (Calamity, Fargo's Souls, el dios antiguo/HOTOG, Thorium, The Stars Above).
    //
    //  Este archivo: LAS CUATRO PRIMERAS.
    // ======================================================================

    /// <summary>
    /// ARMA 1 — EL OJO DEL JUICIO: EL RAYO CONCENTRADO. El ataque
    /// definitivo del mod, hijo directo del RAYO DE FUSIÓN DE EL DIOS ANTIGUO
    /// (estudiado en su código fuente): el ojo del grimorio se abre sobre
    /// el portador, SUCCIONA la luz de la sala (partículas convergentes,
    /// halos apilados) y escupe un CONO DE LUZ PRIMORDIAL de 2400 px con
    /// ANCHO CRECIENTE que BARRE el campo de lado a lado — núcleo blanco,
    /// gradiente animado oro→ámbar→violeta→cian, temblor de cámara
    /// constante y vuelo infinito mientras dura el veredicto.
    /// </summary>
    public class OjoDelJuicio : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 250;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;                       // CANALIZADO: el rayo vive mientras sostienes
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<OjoDelJuicioProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 40;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74;
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<OjoDelJuicioProjectile>())
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
    /// ARMA 2 — EL PRISMA PRIMORDIAL: siete haces de luz que CONVERGEN en
    /// uno colosal mientras canalizas (la receta del «Yharim's Crystal» de
    /// Calamity, la evolución del Last Prism de vanilla): los haces
    /// pequeños brotan alrededor del portador girando y se van fundiendo
    /// en el HAZ CENTRAL que crece hasta los 2400 px — los siete colores
    /// del prisma rotando por su firma.
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
    /// ARMA 3 — LA LLUVIA DE VERSOS: PARES de hojas de luz que NACEN en
    /// lados opuestos del cielo, SE BUSCAN entre sí y se ATRAVIESAN al
    /// converger (la receta del «Dance of Light» de Calamity): cada doce
    /// pares cruzados, EL DESTELLO — una nova que cubre la pantalla y
    /// borra lo que quede de pie.
    /// </summary>
    public class LluviaDeVersos : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 140;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LluviaDeVersosProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 60;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(0.2f);
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<LluviaDeVersosProjectile>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA PRESA: primero jefes, luego chusma (el contrato de la casa)
            NPC presa = DefinitivasLib.BuscarPresa(player.Center, true) ??
                        DefinitivasLib.BuscarPresa(player.Center, false);
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type,
                damage, knockback, player.whoAmI, presa != null ? presa.whoAmI + 1 : 0);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>
    /// ARMA 4 — EL FILO CONSTELADO: la espada de las CUATRO
    /// CONSTELACIONES (la receta de la «Galaxia» de Calamity): clic
    /// derecho para cambiar de constelación — EL SOL (la onda solar que
    /// barre), LA ESTRELLA POLAR (los dardos estelares que persiguen),
    /// ANDRÓMEDA (la galaxia espiral que engulle) y ARIES (la cadena de
    /// nodos estelares que golpea a 650 px). Cada una con SU paleta, SU
    /// firma y SU sonido.
    /// </summary>
    public class FiloConstelado : ModItem
    {
        public static readonly string[] NombresAtavio =
        {
            "EL SOL", "LA ESTRELLA POLAR", "ANDRÓMEDA", "ARIES"
        };

        public override void SetDefaults()
        {
            Item.damage = 260;
            Item.DamageType = DamageClass.Melee;
            Item.width = 44;
            Item.height = 44;
            Item.useTime = 26;
            Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<FiloConsteladoProjectile>();
            Item.shootSpeed = 12f;
            Item.mana = 0;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item1.WithPitchOffset(-0.35f);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            // CLIC DERECHO: girar la constelación (sin golpear)
            if (player.altFunctionUse == 2)
            {
                byte a = DefinitivasLib.AtavioFilo[player.whoAmI];
                DefinitivasLib.AtavioFilo[player.whoAmI] = (byte)((a + 1) % 4);
                DefinitivasLib.Sonar(SoundID.Item4.WithPitchOffset(0.3f), player.Center);
                if (player.whoAmI == Main.myPlayer)
                    Main.NewText($"[c/FFD678:El filo canta:] [c/B0A0FF:{NombresAtavio[DefinitivasLib.AtavioFilo[player.whoAmI]]}]");
                return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // el atavío viaja en ai[1]; la dirección del swing es la del cursor
            Vector2 rumbo = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX);
            Projectile.NewProjectile(source, player.MountedCenter + rumbo * 30f, rumbo * 12f,
                type, damage, knockback, player.whoAmI, 0f, DefinitivasLib.AtavioFilo[player.whoAmI]);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
