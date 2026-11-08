using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles;

namespace AethonMod.Content.Items.Cosmetics
{
    // ======================================================================
    //  FormaEternaItem — v6.114 — LA FORMA ETERNA: EL COSMÉTICO DEFINITIVO.
    //
    //  LA LETRA DEL USUARIO: «es momento de crear el item cosmetico
    //  definitivo para este se tiene que usar los siguiente accesorios…
    //  la brasa del eclipse · el anillo runico dorsal · el sello del
    //  genesis · los anillos del sol runico · el brillo de los soles
    //  runicos · la forma ascendida · la forma ascendida 8 · aura del
    //  vacio… con esto tienes que hacer un unico accesorio».
    //
    //  EL UNO QUE VISTE LOS OCHO: LA FORMA ETERNA porta TODO el panteón
    //  de la casa EN UN SOLO CUERPO —
    //   · LAS DOS LUCES (la Forma Ascendida original y LAS OFANIM, la
    //     octava): el aura del portador con SUS dos patrones + el VUELO
    //     INFINITO de las formas.
    //   · LA BRASA DEL ECLIPSE (el aura de humo negro y oro).
    //   · EL ANILLO RÚNICO DORSAL (la triple corona de conjuro a la
    //     espalda).
    //   · EL BRILLO DE LOS SOLES RÚNICOS (los destellos de 4 puntas).
    //   · EL SELLO DEL GÉNESIS + LOS ANILLOS DEL SOL RÚNICO (las dos
    //     escrituras que rodean al portador + sus stats de sabio y de
    //     guerrero).
    //   · EL AURA DEL VACÍO (el campo invisible que viste a los impíos
    //     con la quemadura del vacío exterior).
    //  Los ocho originales VIAJAN con ella en LA BOLSA DE LA FORMA
    //  ETERNA (por si el portador quiere vestirlos por separado).
    // ======================================================================
    public class FormaEternaItem : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 1;
            Item.accessory = true;      // funciona puesto Y en vanidad (el escaneo de la casa ve ambos)
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(platinum: 1);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // ==========================================================
            //  EL STATS DEL PANTEÓN — lo que los sellos daban, juntos:
            //   · EL SELLO DEL GÉNESIS (el sabio): +40 maná, +8% daño
            //     mágico, +4% crítico mágico.
            //   · LOS ANILLOS DEL SOL RÚNICO (el guerrero): +2 regen,
            //     +8% daño cuerpo a cuerpo, +3% crítico melee, +3 defensa.
            //  (Los visuales viven por el ESCANEO: CosmeticPlayer y
            //  SellosPlayer encienden SUS banderas al ver la Forma
            //  Eterna en cualquier hueco — puesto o vanidad.)
            // ==========================================================
            player.statManaMax2 += 40;
            player.GetDamage(DamageClass.Magic) += 0.08f;
            player.GetCritChance(DamageClass.Magic) += 4f;
            player.lifeRegen += 2;
            player.GetDamage(DamageClass.Melee) += 0.08f;
            player.GetCritChance(DamageClass.Melee) += 3f;
            player.statDefense += 3;

            // ==========================================================
            //  EL AURA DEL VACÍO (la octava gracia): el campo invisible
            //  que viste a los impíos — el ancla nace igual que en el
            //  ítem original (AuraJuicio2Ancla, el efecto del viejo
            //  estandarte: nada sobre el portador, el teatro vive en
            //  los enemigos).
            // ==========================================================
            int tipoAncla = ModContent.ProjectileType<AuraJuicio2Ancla>();
            if (player.ownedProjectileCounts[tipoAncla] < 1 &&
                !player.dead && !Main.gameMenu)
            {
                Projectile.NewProjectile(player.GetSource_FromThis(),
                    player.MountedCenter, Vector2.Zero, tipoAncla, 0, 0f,
                    player.whoAmI);
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "FE1",
                Language.GetTextValue("Mods.AethonMod.Items.FormaEternaItem.Titulo")));
            tooltips.Add(new TooltipLine(Mod, "FE2",
                Language.GetTextValue("Mods.AethonMod.Items.FormaEternaItem.Linea1")));
            tooltips.Add(new TooltipLine(Mod, "FE3",
                Language.GetTextValue("Mods.AethonMod.Items.FormaEternaItem.Linea2")));
            tooltips.Add(new TooltipLine(Mod, "FE4",
                Language.GetTextValue("Mods.AethonMod.Items.FormaEternaItem.Linea3")));
            tooltips.Add(new TooltipLine(Mod, "FE5",
                Language.GetTextValue("Mods.AethonMod.Items.FormaEternaItem.Linea4")));
            tooltips.Add(new TooltipLine(Mod, "FE6",
                Language.GetTextValue("Mods.AethonMod.Items.FormaEternaItem.Linea5")));
        }
    }
}
