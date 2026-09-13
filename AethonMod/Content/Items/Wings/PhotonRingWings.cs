using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Wings
{
    /// <summary>
    /// PhotonRingWings — v6.06 — LAS ALAS DEL ANILLO DE FOTONES.
    ///
    /// Segunda ala de luz 100% PROCEDURAL (técnica de las coronas, PNG de
    /// equipo en blanco + renderer de la biblioteca):
    ///
    ///   - Una MICRO-SINGULARIDAD negra por lado como raíz.
    ///   - CINCO HOJAS DE LUZ por lado (cadenas de cuentas pálidas curvadas
    ///     hacia arriba) que se COMPRIMEN en reposo y se abren en abanico
    ///     al volar.
    ///   - PULSOS DE FOTONES: cuentas blancas que VIAJAN hacia fuera por
    ///     cada hoja — la luz escapando del horizonte.
    ///   - MINI-ANILLOS de Einstein rotando en las puntas.
    ///   - Animación por MUELLES (WingAnimPlayer), aleteo de onda continua.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class PhotonRingWings : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Récord de aceleración del set.
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(200, 9f, 3.2f);
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
            ascentWhenFalling = 0.80f;   // cae un poco más lento: fotonas ligeras
            ascentWhenRising = 0.18f;
            maxCanAscendMultiplier = 1f;
            maxAscentMultiplier = 3.2f;
            constantAscend = 0.15f;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FF1738:═══ ALAS DEL ANILLO DE FOTONES ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FF66AA:Cinco hojas de luz pálida brotando de dos micro-singularidades,\ncon pulsos de fotones viajando hacia las puntas]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FF9BD2:Mini-anillos de Einstein rotando en cada punta]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/78788C:Ala de luz 100% procedural (técnica de las coronas):\nse pliega en reposo · se despliega al caer · aleteo de onda continua al volar]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Tiempo de vuelo end-game: 200 ticks · velocidad 9 · aceleración ×3.2]"));
        }
    }
}
