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
    /// SinfoniaPrimordialStaff — v6.24 — EL BASTÓN DE LA SINFONÍA PRIMORDIAL.
    ///
    /// LA PRIMERA DE LAS TRES ARMAS DE LAS LIBRERÍAS (petición del
    /// usuario: "crea un arma que use todas nuestras librerías, sé
    /// creativo") — cada nota de la sinfonía es una librería:
    ///
    ///   · LUMENLIB (la luz)  → el corazón: bloom prismático de drift +
    ///     destello de 4 puntas + aurora de bandas + rayos de sol radiando.
    ///   · BRUMAFX (el humo)  → la estela de bruma viva tras la chispa +
    ///     la NUBE de la detonación final.
    ///   · STORMLIB (el rayo) → los arcos de corona alrededor de la chispa,
    ///     las cadenas a los enemigos cercanos y LOS SEIS RAYOS RADIALES
    ///     del estallido.
    ///   · LAS RUNAS (la firma) → seis glifos orbitando la chispa (la forma
    ///     de los agujeros) que REVIENTAN hacia afuera en la detonación.
    ///
    /// Dispara la CHISPA DE LA CREACIÓN: un corazón de luz lento que se
    /// abre paso encadenando rayos a los enemigos que roza — y al morir,
    /// LA SINFONÍA: estallido en área + 6 rayos radiales + nube de bruma
    /// + aurora + las runas volando.
    ///
    /// v6.24 — SIN MANA (regla del usuario: todos los bastones del mod
    /// son de prueba).
    /// </summary>
    public class SinfoniaPrimordialStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 120;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 32; Item.useAnimation = 32;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SinfoniaPrimordialProjectile>();
            Item.shootSpeed = 9.5f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item12.WithPitchOffset(0.15f);
            Item.value = 12000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/BFE8FF:Dispara la Chispa de la Creación: luz + bruma + rayos + runas]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Encadena rayos al vuelo · estalla en área con 6 rayos radiales · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
