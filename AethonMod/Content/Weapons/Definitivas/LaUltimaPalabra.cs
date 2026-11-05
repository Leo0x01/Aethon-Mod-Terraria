using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Definitivas;

namespace AethonMod.Content.Weapons.Definitivas
{
    // ======================================================================
    //  LaUltimaPalabra — v6.100 — EL ARMA DEFINITIVA.
    //
    //  LA LETRA DEL USUARIO (v6.100): «crea 1 arma definitiva con
    //  todas las nuevas herramientas y tecnicas vfx» — TODAS significa
    //  TODAS: la forja (el sprite con transparencia nativa), el motor
    //  de píxeles (la nova doble del juicio), el motor de capas (el
    //  rayo de tres fundas con su velo de ruido), HazLib (la columna
    //  que cruza el mapa), OndaLib (la onda de choque), el charco que
    //  queda hirviendo y LAS ALMAS que vuelan.
    //
    //  EL RITUAL EN TRES ACTOS (la carga → el decreto → el juicio):
    //   · LA CARGA: mantén el uso — el libro se abre sobre el hombro
    //     y el corazón de la corona LATE creciendo (2,5 s al máximo).
    //   · EL DECRETO: al soltar, LA PALABRA cruza el mapa — el rayo
    //     de tres capas (núcleo blanco + funda dorada + halo) con su
    //     velo de ruido procedural y su núcleo de píxeles.
    //   · EL JUICIO: donde cae — la NOVA DOBLE (la flor de fuego de
    //     OndaLib + el acto del motor de píxeles), la ONDA DE CHOQUE,
    //     EL CHARCO que queda hirviendo 5 segundos y LAS ALMAS.
    // ======================================================================
    public class LaUltimaPalabra : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 320;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 34;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = false;
            Item.channel = true;             // LA CARGA: vive mientras sostengas
            Item.shoot = ModContent.ProjectileType<LaUltimaPalabraProjectile>();
            Item.shootSpeed = 0f;
            Item.mana = 0;                   // SIN COSTE DE MANÁ
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.UseSound = SoundID.Item74;  // el susurro del libro al abrirse
        }

        public override bool CanUseItem(Player player)
        {
            // UN solo decreto a la vez (la palabra no se atropella).
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == Item.shoot)
                    return false;
            }
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Palabra1",
                "[c/FFE9B0:LA CARGA: mantén el uso — el libro se abre y el corazón de la corona late creciendo]"));
            tooltips.Add(new TooltipLine(Mod, "Palabra2",
                "[c/FFE9B0:EL DECRETO: al soltar, LA PALABRA cruza el mapa — el rayo de tres capas con su velo de ruido]"));
            tooltips.Add(new TooltipLine(Mod, "Palabra3",
                "[c/FFE9B0:EL JUICIO: la nova doble (píxeles + flor de fuego), la onda de choque, el charco que queda hirviendo y las almas que vuelan]"));
            tooltips.Add(new TooltipLine(Mod, "Palabra4",
                "[c/D8B45A:(la palabra que cierra el libro · TODAS las herramientas · SIN COSTE DE MANÁ)]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
