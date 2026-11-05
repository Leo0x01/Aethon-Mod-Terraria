using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Players;

namespace AethonMod.Content.Items.Accessories
{
    /// <summary>
    /// EstandarteDelJuicioItem — v6.98 — EL ESTANDARTE DEL JUICIO.
    ///
    /// LA LETRA: «tienes que tomar este item […] un accesorio que
    /// crea un aura alrededor del jugador que daña a los enemigos si
    /// se acercan y cuanto mas se acerquen mas se dañan […] crea algo
    /// similar o incluso superior al concepto original con mejores
    /// VFX y texturas» (el estandarte de guerra de la referencia).
    ///
    /// EL CONCEPTO ES LA IDEA (el aura de daño por proximidad); TODO
    /// lo demás es DE LA CASA: el nombre, las cifras, el debuff, el
    /// banner y sus DIEZ capas de VFX (ver
    /// <see cref="Projectiles.EstandarteJuicioAuraProjectile"/>). El
    /// sprite lo forjó LA SUPERFORJA (la cadena completa con QA) y la
    /// tira del banner ondea en la espalda del portador.
    ///
    /// MECÁNICA: al equiparlo, el ANCLA DEL JUICIO vive pegada al
    /// portador — cada 12 ticks juzga a los 8 impíos más cercanos
    /// (24 tiles): el daño crece con la proximidad (hasta ×2,5), se
    /// arma mientras el enemigo permanezca (+8%/s, hasta +80%), la
    /// base escala con el nivel de despertar (40 + 12×nivel), quema
    /// con la LLAMA DEL ALBA (40 HP/s) y repele al osado.
    /// </summary>
    public class EstandarteDelJuicioItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 40;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(gold: 30);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            JuicioPlayer jp = player.GetModPlayer<JuicioPlayer>();
            jp.EstandarteJuicio = true;
            jp.EstandarteJuicioOculto = hideVisual;   // la mecánica vive, el Vestido se apaga
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "EJTitulo",
                Language.GetTextValue("Mods.AethonMod.Items.EstandarteDelJuicioItem.Titulo")));
            tooltips.Add(new TooltipLine(Mod, "EJL1",
                Language.GetTextValue("Mods.AethonMod.Items.EstandarteDelJuicioItem.Linea1")));
            tooltips.Add(new TooltipLine(Mod, "EJL2",
                Language.GetTextValue("Mods.AethonMod.Items.EstandarteDelJuicioItem.Linea2")));
            tooltips.Add(new TooltipLine(Mod, "EJL3",
                Language.GetTextValue("Mods.AethonMod.Items.EstandarteDelJuicioItem.Linea3")));
            tooltips.Add(new TooltipLine(Mod, "EJL4",
                Language.GetTextValue("Mods.AethonMod.Items.EstandarteDelJuicioItem.Linea4")));
        }

        public override void AddRecipes()
        {
            // LA FORJA POST-LUNAR: el estandarte se confecciona con la
            // luz de la derrota del pilares del cielo (la Casa Sagrada).
            CreateRecipe().AddIngredient(ItemID.HallowedBar, 20)
                .AddIngredient(ItemID.SoulofLight, 20)
                .AddIngredient(ItemID.FragmentSolar, 12)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
