using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Accessories
{
    /// <summary>
    /// SelloGenesisItem — EL SELLO DEL GÉNESIS (v6.35).
    ///
    /// EL PRIMER ACCESORIO DE LOS SIGNOS MÁGICOS: los sellos de
    /// SigiloLib RODEAN AL JUGADOR — el aro doble con las ocho runas de
    /// pie gira lento alrededor del cuerpo mientras un segundo sello
    /// interior azul-estelar contrarrotando (con el glifo del ASTRO en
    /// el centro) respira dentro del primero. La escritura solar te
    /// abraza: cada runa cabalga la tangente con su gradiente y su
    /// perla, y el polvo rúnico orbita el conjunto.
    ///
    /// MECÁNICA: el favorecido del sol — +40 de maná máximo, +8% de
    /// daño mágico y +4% de crítico mágico. Los sellos viven en huecos
    /// funcionales Y de vanidad (un signo es un signo viva donde lo
    /// lleves).
    /// </summary>
    public class SelloGenesisItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.buyPrice(gold: 12);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.statManaMax2 += 40;
            player.GetDamage(DamageClass.Magic) += 0.08f;
            player.GetCritChance(DamageClass.Magic) += 4f;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                Language.GetTextValue("Mods.AethonMod.Items.SelloGenesisItem.Titulo")));
            tooltips.Add(new TooltipLine(Mod, "S2",
                Language.GetTextValue("Mods.AethonMod.Items.SelloGenesisItem.Linea1")));
            tooltips.Add(new TooltipLine(Mod, "S3",
                Language.GetTextValue("Mods.AethonMod.Items.SelloGenesisItem.Linea2")));
            tooltips.Add(new TooltipLine(Mod, "S4",
                Language.GetTextValue("Mods.AethonMod.Items.SelloGenesisItem.Linea3")));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.GoldBar, 20)
                .AddIngredient(ItemID.SoulofLight, 20)
                .AddIngredient(ItemID.FallenStar, 10)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    /// <summary>
    /// AnillosSolRunicoItem — LOS ANILLOS DEL SOL RÚNICO (v6.35).
    ///
    /// EL ACCESORIO DE LA LIBRERÍA DEL SOL: el SISTEMA COMPLETO de
    /// anillos orbitales de los soles rúnicos RODEA AL JUGADOR — siete
    /// aros elípticos cada uno en SU plano (empaquetados, achatados,
    /// precesando vivo), con GIRO ALTERNO (par horario, impar
    /// antihorario) y las runas cabalgando cada tangente; cada tercer
    /// aro viste el azul-estelar frío de los tier altos. El jugador se
    /// convierte en el corazón de su propia constelación.
    ///
    /// MECÁNICA: la vitalidad del sol — +2 de regeneración de vida,
    /// +8% de daño melé, +3% de crítico melé y +3 de defensa.
    /// </summary>
    public class AnillosSolRunicoItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.buyPrice(gold: 12);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.lifeRegen += 2;
            player.GetDamage(DamageClass.Melee) += 0.08f;
            player.GetCritChance(DamageClass.Melee) += 3f;
            player.statDefense += 3;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                Language.GetTextValue("Mods.AethonMod.Items.AnillosSolRunicoItem.Titulo")));
            tooltips.Add(new TooltipLine(Mod, "S2",
                Language.GetTextValue("Mods.AethonMod.Items.AnillosSolRunicoItem.Linea1")));
            tooltips.Add(new TooltipLine(Mod, "S3",
                Language.GetTextValue("Mods.AethonMod.Items.AnillosSolRunicoItem.Linea2")));
            tooltips.Add(new TooltipLine(Mod, "S4",
                Language.GetTextValue("Mods.AethonMod.Items.AnillosSolRunicoItem.Linea3")));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.HallowedBar, 20)
                .AddIngredient(ItemID.SoulofSight, 15)
                .AddIngredient(ItemID.Amber, 8)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
