using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Wings
{
    /// <summary>
    /// EventHorizonWings — v6.06 — LAS ALAS DEL HORIZONTE DE SUCESOS.
    ///
    /// Ala de luz 100% PROCEDURAL con la técnica de las coronas: su textura
    /// de equipo es un PNG EN BLANCO (no hay spritesheet) y TODO el dibujado
    /// lo hace EventHorizonWingRenderer desde la capa de dibujado del
    /// jugador, con la paleta del agujero negro carmesí:
    ///
    ///   - Un MINI HORIZONTE DE SUCESOS negro por lado, con su anillo de
    ///     fotones rosa pálido — la raíz del ala en la espalda.
    ///   - TRES ARCOS DE ACRECIÓN anidados por lado (la silueta del ala)
    ///     que respiran y se precesan.
    ///   - DOPPLER: el lado que avanza ARDE más (δ³, como el disco real).
    ///   - Cuentas de MATERIA orbitando los horizontes cuando vuelas.
    ///   - Animación por MUELLES (WingAnimPlayer): se pliegan en reposo,
    ///     se despliegan al saltar y aletean con onda continua al volar.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class EventHorizonWings : ModItem
    {
        public override void SetStaticDefaults()
        {
            // La joya del set: por encima de Solar Wings.
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(200, 9.5f, 3f);
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

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FF1738:═══ ALAS DEL HORIZONTE DE SUCESOS ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FF66AA:Arcos de acreción carmesí anidados que brotan de dos mini agujeros\nnegros en tu espalda, con anillos de fotones y materia orbitando]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FC0096:El lado que avanza ARDE por doppler relativista (δ³)]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/78788C:Ala de luz 100% procedural (técnica de las coronas):\nse pliega en reposo · se despliega al caer · aleteo de onda continua al volar]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Tiempo de vuelo end-game: 200 ticks · velocidad 9.5 · aceleración ×3]"));
        }
    }
}
