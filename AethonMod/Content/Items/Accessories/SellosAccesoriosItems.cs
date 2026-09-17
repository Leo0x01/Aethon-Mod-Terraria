using System.Collections.Generic;
using Terraria;
using Terraria.ID;
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
                "[c/FFD54F:═══ EL SELLO DEL GÉNESIS ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/FFC107:Los signos mágicos del sol te rodean: el aro doble con las ocho runas girando,\ny dentro el sello azul-estelar contrarrotando con el glifo del astro]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/9FA8DA:+40 de maná máximo, +8% de daño mágico y +4% de crítico mágico]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:La escritura solar vive en huecos de accesorio o de vanidad.\nFirmada por las librerías de signos mágicos de la casa]"));
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
                "[c/FFB74D:═══ LOS ANILLOS DEL SOL RÚNICO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/FFD180:Siete aros rúnicos orbitan tu cuerpo en planos alternos:\npar horario, impar antihorario, precesando vivos con sus runas a lomos]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/A7FFEB:+2 de regeneración de vida, +8% de daño melé, +3% de crítico melé y +3 de defensa]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:Cada tercer aro viste el azul-estelar frío de los soles de tier alto.\nEl sistema vive en huecos de accesorio o de vanidad]"));
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

    /// <summary>
    /// AnillosHorizonteItem — LOS ANILLOS DEL HORIZONTE DE SUCESOS (v6.35).
    ///
    /// EL ACCESORIO DE LA LIBRERÍA DEL VACÍO: los anillos del agujero
    /// negro RODEAN AL JUGADOR — el disco de acreción con sus veinte
    /// bandas viajando (la fórmula fiel del shader, mitad trasera
    /// tenue y mitad delantera incandescente), los ecos en resonancia,
    /// el aro fino del horizonte, los fotones corriendo el vórtice y
    /// las ondas de distorsión pulsando. Y EL MUNDO SE DOBLA: la lente
    /// gravitacional de GravLens curva el fondo alrededor del portador
    /// (el signature de la casa — caminas y la realidad se estrecha
    /// contigo).
    ///
    /// MECÁNICA: la gravedad del vacío — +10% de velocidad de
    /// movimiento y +5% de daño universal; y LA SUCCIÓN: los enemigos
    /// cercanos (140 px) se deslizan imperceptiblemente hacia ti (los
    /// jefes no se dejan arrastrar).
    /// </summary>
    public class AnillosHorizonteItem : ModItem
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
            player.moveSpeed += 0.10f;
            player.GetDamage(DamageClass.Generic) += 0.05f;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FF7043:═══ LOS ANILLOS DEL HORIZONTE DE SUCESOS ═══]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/FFAB91:El disco de acreción te orbita: veinte bandas incandescentes viajando,\nlos fotones corriendo el vórtice y el aro fino del horizonte]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FF8A65:LA LENTE: el fondo se curva a tu alrededor · LA SUCCIÓN: los enemigos cercanos se deslizan hacia ti]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/80CBC4:+10% de velocidad y +5% de daño universal.\nLos anillos viven en huecos de accesorio o de vanidad]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.ChlorophyteBar, 20)
                .AddIngredient(ItemID.SoulofNight, 20)
                .AddIngredient(ItemID.Obsidian, 15)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
