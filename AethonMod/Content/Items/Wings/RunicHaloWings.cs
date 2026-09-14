using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Wings
{
    /// <summary>
    /// RunicHaloWings — v6.22 — EL ANILLO RÚNICO ESTELAR (ALAS).
    ///
    /// Petición del usuario: "crea un cosmético de un anillo rúnico en la
    /// espalda del jugador que funcione como ALAS y HALO en la espalda;
    /// cuando el jugador va a volar, este anillo rúnico BRILLA CON
    /// INTENSIDAD".
    ///
    /// EL PATRÓN DE LAS ALAS DE LUZ (v6.12, el camino probado):
    ///   · [AutoloadEquip(EquipType.Wings)] reserva el slot de equipo y
    ///     da las estadísticas de vuelo (ArmorIDs.Wing.Sets.Stats).
    ///   · El PNG RunicHaloWings_Wings.png es un 8×8 TOTALMENTE
    ///     TRANSPARENTE (el truco del sprite en blanco): vanilla no
    ///     dibuja NADA — ni sprite ni caja ni animación.
    ///   · TODO el dibujado lo hace RunicHaloWingsDrawLayer (detrás del
    ///     cuerpo, tras la capa vanilla de alas): el GRAN ANILLO RÚNICO
    ///     vertical tras la espalda + su contraro + glifos + el corazón
    ///     de luz — que SE ENCIENDE al volar (RunicHaloPlayer).
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class RunicHaloWings : ModItem
    {
        /// <summary>Ticks de vuelo (end-game: como las alas solares).</summary>
        public virtual int FlyTime => 180;

        /// <summary>Velocidad de vuelo.</summary>
        public virtual float FlySpeed => 9f;

        /// <summary>Multiplicador de aceleración.</summary>
        public virtual float FlyAccel => 2.5f;

        public override void SetStaticDefaults()
        {
            // Estadísticas end-game por el camino oficial.
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] =
                new WingStats(FlyTime, FlySpeed, FlyAccel);
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 24;
            Item.value = Item.buyPrice(gold: 15);
            Item.rare = ItemRarityID.Red;
            Item.accessory = true;
        }

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling,
            ref float ascentWhenRising, ref float maxCanAscendMultiplier,
            ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 0.85f;
            ascentWhenRising = 0.15f;
            maxCanAscendMultiplier = 1f;
            maxAscentMultiplier = 3f;
            constantAscend = 0.135f;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FFC86B:═══ ANILLO RÚNICO ESTELAR ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2",
                "[c/FFE8A8:Un gran anillo rúnico de luz dorada flota tras tu espalda:\nel halo y las alas en UN solo sello]"));
            tooltips.Add(new TooltipLine(Mod, "W3",
                "[c/FFD700:AL VOLAR el anillo SE ENCIENDE: el corazón arde, nace la cruz de luz,\nlos rayos radiales del halo se avivan y las runas brillan al blanco]"));
            tooltips.Add(new TooltipLine(Mod, "W5",
                "[c/78788C:Vuelo end-game: 180 ticks · velocidad 9 · aceleración ×2.5]"));
        }

        public override void AddRecipes()
        {
            // Receta barata de pruebas: que nunca se pierda (como las coronas).
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
