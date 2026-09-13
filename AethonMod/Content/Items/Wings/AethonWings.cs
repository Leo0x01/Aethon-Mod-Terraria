using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Wings
{
    /// <summary>
    /// AethonWings — v6.06 — LA BASE DE LAS ALAS CON SPRITESHEET.
    ///
    /// Las 8 alas "de tira" usan el flujo VANILLA de Terraria: textura de
    /// equipo de 4 frames verticales (0 plegadas · 1 aleteo arriba ·
    /// 2 extendidas · 3 aleteo abajo) que el juego anima solo (reacción
    /// nativa a volar / saltar / caer / planear / reposo, con su sonido de
    /// aleteo). Cada subclase solo define SUS estadísticas, su dust de
    /// vuelo y su tooltip de color.
    ///
    /// Todas son de PRUEBA end-game: nivel Solar Wings o superior.
    /// </summary>
    public abstract class AethonWings : ModItem
    {
        /// <summary>Ticks de vuelo (Solar = 180).</summary>
        public virtual int FlyTime => 180;

        /// <summary>Velocidad de vuelo (Solar = 9).</summary>
        public virtual float FlySpeed => 9f;

        /// <summary>Multiplicador de aceleración (Solar = 2.5).</summary>
        public virtual float FlyAccel => 2.5f;

        /// <summary>Dust que sueltan al volar.</summary>
        public virtual int FlightDust => DustID.Torch;

        /// <summary>Color del dust (default = el color propio del dust).</summary>
        public virtual Color FlightDustColor => default;

        /// <summary>Escala del dust.</summary>
        public virtual float FlightDustScale => 1.2f;

        public override void SetStaticDefaults()
        {
            // Estadísticas end-game por el camino oficial.
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(FlyTime, FlySpeed, FlyAccel);
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 24;
            Item.value = Item.buyPrice(gold: 12);
            Item.rare = ItemRarityID.Red;
            Item.accessory = true;
        }

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling,
            ref float ascentWhenRising, ref float maxCanAscendMultiplier,
            ref float maxAscentMultiplier, ref float constantAscend)
        {
            // Personalidad vertical por defecto (los valores del ExampleMod).
            ascentWhenFalling = 0.85f;
            ascentWhenRising = 0.15f;
            maxCanAscendMultiplier = 1f;
            maxAscentMultiplier = 3f;
            constantAscend = 0.135f;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // Ascuas de vuelo (no en vanidad oculta).
            if (hideVisual) return;
            if (player.controlJump && player.wingTime > 0f && player.jump == 0
                && player.velocity.Y != 0f && Main.rand.NextBool(2))
            {
                float side = Main.rand.NextBool() ? -1f : 1f;
                Dust d = Dust.NewDustDirect(
                    player.position + new Vector2(
                        player.width * 0.5f + side * Main.rand.NextFloat(6f, 34f),
                        player.height * 0.5f - 12f),
                    8, 8, FlightDust, 0f, 0f, 120, FlightDustColor,
                    Main.rand.NextFloat(FlightDustScale * 0.8f, FlightDustScale * 1.25f));
                d.noGravity = true;
                d.velocity *= 0.3f;
                d.fadeIn = 1.15f;
            }
        }

        public override void AddRecipes()
        {
            // Receta barata de pruebas: que nunca se pierdan (como las coronas).
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
