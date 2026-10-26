using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Players;

namespace AethonMod.Content.Items.Cosmetics.Investigacion
{
    /// <summary>
    /// ESTELADELCOSMOS — v6.54.0 — EL ANILLO SATURNO DEL JUGADOR.
    ///
    /// La letra: «goozma tambien tiene varias texturas interesantes
    /// como SpaceTrail.png y SpaceTrailGlow.png estas creo que son
    /// usadas como una especia de aura de colores que rodea a goozma
    /// de todas formas revisa como es que la usa y crea un item en
    /// concecuencia para el jugador que sea cosmetico, pero no uses
    /// las mismas texturas crea tu propia SpaceTrail.png centrada en
    /// el color morado y negro ya sabes los colores de nuestro mod».
    ///
    /// LA INVESTIGACIÓN (Task 74-a): es el ANILLO tipo Saturno del
    /// StellarGeliath (el esbirro estelar — no de Goozma mismo): DOS
    /// SEMIANILLOS de primitivas (el trasero se dibuja antes del
    /// cuerpo, el delantero después → el anillo CRUZA al portador),
    /// radio ondulante, squash Y ×0.1, y el shader StellarRing hace la
    /// AUTO-INTERFERENCIA: la MISMA banda sampleada a 2 velocidades y
    /// MULTIPLICADA — flujo líquido con una sola textura; los canales
    /// RGB son MÁSCARAS (G=cuerpo, B=línea del borde, R=aura).
    ///
    /// LA VERSIÓN CASA (100 % nueva, MORADO y NEGRO): EstelaEspacial.png
    /// (la banda-máscara con los canales de la casa: G=lila caliente,
    /// R=violeta cuerpo, B=azul-violeta del borde — generada por
    /// tools/gen_investigacion_v6540.py) + EstelaEspacialBrillo.png
    /// (la barra glow 1D), dibujadas por FajaCPU con la
    /// auto-interferencia resuelta por nodo. El anillo entero vive
    /// DETRÁS del cuerpo (el pase de proyectiles — el patrón halo de
    /// la casa).
    /// </summary>
    public class EstelaDelCosmos : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;      // huecos funcionales Y de vanidad
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(gold: 5);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var mp = player.GetModPlayer<InvestigacionCosmeticosPlayer>();
            mp.EstelaCosmos = true;
            mp.EstelaCosmosOculta = hideVisual;
        }

        public override void UpdateVanity(Player player)
        {
            var mp = player.GetModPlayer<InvestigacionCosmeticosPlayer>();
            mp.EstelaCosmos = true;
            mp.EstelaCosmosVanity = true;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
            => true;    // siempre equipable: es un adorno

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "C",
                Language.GetTextValue("Mods.AethonMod.Items.EstelaDelCosmos.Titulo"))
            { OverrideColor = new Microsoft.Xna.Framework.Color(212, 190, 255) });
            tooltips.Add(new TooltipLine(Mod, "D",
                Language.GetTextValue("Mods.AethonMod.Items.EstelaDelCosmos.Linea1")));
            tooltips.Add(new TooltipLine(Mod, "D2",
                Language.GetTextValue("Mods.AethonMod.Items.EstelaDelCosmos.Linea2")));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>
    /// TINTAVIVA — v6.54.0 — EL CORDÓN LÍQUIDO DEL JUGADOR.
    ///
    /// La letra (la parte LiquidTrail): «otra textura interesante es
    /// LiquidTrail.png … investiga como las usa y crea tus propias
    /// texturas basadas en esas … y crea un item en consecuencia para
    /// el jugador».
    ///
    /// LA INVESTIGACIÓN (Task 74-b): es el CORDÓN UMBILICAL que Goozma
    /// arrastra detrás — una cola de 50 nodos (curva + onda seno ±20
    /// px, ancho máx 18) dibujada a un render target con el shader
    /// GoozmaCordMap: DOS CAPAS de la MISMA textura LiquidTrail a
    /// escalas UV ×(3,1) y ×(2,1) y velocidades uTime y 3·uTime
    /// MULTIPLICADAS → el encaje de agujeros de goo; el canal G decide
    /// la fila clara/oscura de la LUT ColorMap_0 y el canal R desplaza
    /// el tono — el color nace del LUT.
    ///
    /// LA VERSIÓN CASA: TintaViva.png (alpha binaria, vetas y metaballs
    /// — generada nueva) + MapaColorTinta.png (la LUT de 2 bandas con
    /// LA RAMA MORADA de la casa: negro #07030D → morado #4B1E7A → lila
    /// #C9A6FF) — la cola sigue el RASTRO REAL del jugador (las últimas
    /// posiciones vivas) y fluye por FajaCPU.
    /// </summary>
    public class TintaViva : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(gold: 5);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var mp = player.GetModPlayer<InvestigacionCosmeticosPlayer>();
            mp.TintaViva = true;
            mp.TintaVivaOculta = hideVisual;
        }

        public override void UpdateVanity(Player player)
        {
            var mp = player.GetModPlayer<InvestigacionCosmeticosPlayer>();
            mp.TintaViva = true;
            mp.TintaVivaVanity = true;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
            => true;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "C",
                Language.GetTextValue("Mods.AethonMod.Items.TintaViva.Titulo"))
            { OverrideColor = new Microsoft.Xna.Framework.Color(212, 190, 255) });
            tooltips.Add(new TooltipLine(Mod, "D",
                Language.GetTextValue("Mods.AethonMod.Items.TintaViva.Linea1")));
            tooltips.Add(new TooltipLine(Mod, "D2",
                Language.GetTextValue("Mods.AethonMod.Items.TintaViva.Linea2")));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
