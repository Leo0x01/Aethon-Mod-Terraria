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
            //  v6.118 — LA BANDERA EN VIVO, PERO SOLO-VISUAL (la letra:
            //  «la forma eterna no debe volar, eso es algo que tienen
            //  que hacer las alas»): el mismo patrón del fix v6.115 (las
            //  banderas se encienden DENTRO de Player.UpdateEquips para
            //  que los hooks las vean EN ESTE tick) — pero ahora la
            //  Forma Eterna enciende las banderas VISUALES de las dos
            //  luces (FormaAscendidaVisual/FormaAscendidaOchoVisual):
            //  el panteón VISTE, el VUELO NO (VueloDivino solo lee las
            //  banderas reales — el cielo es cosa de las alas).
            // ==========================================================
            var visuales = player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>();
            visuales.FormaAscendidaVisual = true;   // LA PRIMERA LUZ (el aura, sin vuelo)
            visuales.FormaAscendidaOchoVisual = true; // LAS OFANIM (la octava luz, sin vuelo)
            visuales.AnilloDorsal = true;         // la triple corona de conjuro
            visuales.BrasaDelEclipse = true;      // el aura de humo negro y oro
            visuales.BrilloSolar = true;          // los destellos de 4 puntas

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
            //  los enemigos). (v6.115 — LlevaElAura ya la RECONOCE:
            //  antes el ancla MORÍA al instante porque su vínculo solo
            //  sabía ver el ítem original — el aura jamás encendía.)
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

        public override void UpdateVanity(Player player)
        {
            // v6.118 — EL PANTEÓN TAMBIÉN EN VANIDAD, SOLO-VISUAL (la regla
            // de la casa: «un cosmético es un cosmético viva donde lo
            // pongas» — y la letra nueva: NINGUNA de las dos luces de la
            // Forma Eterna inyecta vuelo, ni del hueco funcional ni del de
            // vanidad: el cielo es cosa de las alas).
            var visuales = player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>();
            visuales.FormaAscendidaVisual = true;
            visuales.FormaAscendidaOchoVisual = true;
            visuales.AnilloDorsal = true;
            visuales.BrasaDelEclipse = true;
            visuales.BrilloSolar = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // v6.115 — EL PERGAMINO COMPLETO (el fix de la ventana de
            // información: en la v6.114 estas claves NO existían en el
            // hjson y la ventana mostraba las claves CRUDAS — «Mods.
            // AethonMod.Items.FormaEternaItem.Titulo» línea por línea.
            // Ahora las claves viven ×3 idiomas y, si alguna faltara,
            // el respaldo la calla en vez de enseñar la llave).
            void Linea(string clave, string respaldo)
            {
                string texto = Language.GetTextValue("Mods.AethonMod.Items.FormaEternaItem." + clave);
                if (string.IsNullOrEmpty(texto) || texto.StartsWith("Mods."))
                    texto = respaldo;
                if (!string.IsNullOrEmpty(texto))
                    tooltips.Add(new TooltipLine(Mod, clave, texto));
            }
            Linea("Titulo", "[c/B96EEA:═══ LA FORMA ETERNA ═══]");
            Linea("Linea1", "");
            Linea("Linea2", "");
            Linea("Linea3", "");
            Linea("Linea4", "");
            Linea("Linea5", "");
        }
    }
}
