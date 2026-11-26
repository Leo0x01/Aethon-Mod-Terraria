using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Players;

namespace AethonMod.Content.Items.Investigaciones
{
    // ======================================================================
    //  LAS DIEZ MEJORAS DEL PROYECTADO — v6.177 — «CON NOMBRES QUE
    //  DUELEN» (el plano del meme, recreado por la casa).
    //
    //  Cada jefe de Terraria suelta su consumible de UN SOLO USO que
    //  sube la ENERGÍA PROYECTADA máxima (de 100 hasta 300) y acelera la
    //  recarga +10%. La del Señor de la Luna es LA LUNA QUE TE IBA A
    //  BAJAR: la PROYECCIÓN INFINITA (el rayo ya no gasta energía).
    //
    //  EL ÍNDICE de cada una (el orden del plano — vive en el bitmask
    //  del jugador):
    //   0 la carta que nunca le diste        (Ojo de Cthulhu)
    //   1 la captura del visto               (Cerebro de Cthulhu)
    //   2 el «no eres tú, soy yo»            (Devorador de Mundos)
    //   3 el proyector del salón            (el 1er jefe mecánico caído)
    //   4 el proyector de cine             (el 2º jefe mecánico caído)
    //   5 el proyector IMAX                (el 3er jefe mecánico caído)
    //   6 las flores amarillas              (Plantera)
    //   7 el «ya lo superé»                 (Gólem)
    //   8 «hay más peces en el mar»        (Duque Pez Dragón)
    //   9 la luna que te ibas a bajar       (Señor de la Luna — INFINITA)
    // ======================================================================
    public abstract class MejoraProyectada : ModItem
    {
        public abstract int Indice { get; }

        public override void SetStaticDefaults() { Item.ResearchUnlockCount = 1; }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.consumable = true;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(0, 0, 50, 0);
            Item.UseSound = SoundID.Item4;
        }

        public override bool CanUseItem(Player player)
            => !player.GetModPlayer<ProyectadoPlayer>().Usado(Indice);

        public override bool? UseItem(Player player)
        {
            if (!player.GetModPlayer<ProyectadoPlayer>().AplicarMejora(Indice))
                return false;
            if (player.whoAmI == Main.myPlayer)
            {
                string texto = Indice == 9
                    ? Language.GetTextValue("Mods.AethonMod.Proyectado.InfinitaAnuncio")
                    : Language.GetTextValue("Mods.AethonMod.Proyectado.MasEnergia", ProyectadoPlayer.PuntosMejora[Indice]);
                CombatText.NewText(player.Hitbox, new Color(255, 70, 50), texto, Indice == 9);
            }
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Color vivo = new(255, 90, 70);
            tooltips.Add(new TooltipLine(Mod, "ProyectadoEfecto", Indice == 9
                ? Language.GetTextValue("Mods.AethonMod.Proyectado.EfectoInfinito")
                : Language.GetTextValue("Mods.AethonMod.Proyectado.Efecto", ProyectadoPlayer.PuntosMejora[Indice]))
            { OverrideColor = vivo });
            tooltips.Add(new TooltipLine(Mod, "ProyectadoRecarga",
                Language.GetTextValue("Mods.AethonMod.Proyectado.EfectoRecarga", 10)) { OverrideColor = vivo });
            bool usada = Main.LocalPlayer.GetModPlayer<ProyectadoPlayer>().Usado(Indice);
            tooltips.Add(new TooltipLine(Mod, "ProyectadoUso", usada
                ? Language.GetTextValue("Mods.AethonMod.Proyectado.YaSuperada")
                : Language.GetTextValue("Mods.AethonMod.Proyectado.UnSoloUso"))
            { OverrideColor = usada ? Color.Gray : new Color(200, 200, 200) });
        }
    }

    public class CartaQueNuncaLeDiste : MejoraProyectada { public override int Indice => 0; }
    public class CapturaDelVisto : MejoraProyectada { public override int Indice => 1; }
    public class NoEresTuSoyYo : MejoraProyectada { public override int Indice => 2; }
    public class ProyectorDelSalon : MejoraProyectada { public override int Indice => 3; }
    public class ProyectorDeCine : MejoraProyectada { public override int Indice => 4; }
    public class ProyectorImax : MejoraProyectada { public override int Indice => 5; }
    public class FloresAmarillas : MejoraProyectada { public override int Indice => 6; }
    public class YaLoSupere : MejoraProyectada { public override int Indice => 7; }
    public class HayMasPeces : MejoraProyectada { public override int Indice => 8; }
    public class LunaQueTeIbaABajar : MejoraProyectada { public override int Indice => 9; }
}
