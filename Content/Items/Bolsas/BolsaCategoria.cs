using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Bolsas
{
    /// <summary>
    /// BolsaCategoria — v6.29 — LA BASE DE LAS BOLSAS POR CATEGORÍA.
    ///
    /// Petición del usuario: "crea varias bolsas para todas las armas que me
    /// tienes que dar no solo una y separalas por categorías, una categoría
    /// por bolsa".
    ///
    /// La bolsa única del Arsenal (v6.27) se retira: en su lugar, UNA BOLSA
    /// POR CATEGORÍA del arsenal. Todas comparten la semántica de la casa:
    ///
    ///   · PERMANENTES (no se consumen): reábrelas cuando pierdas un arma.
    ///   · GARANTÍA: solo entregan lo que FALTE (reabrir repone lo perdido
    ///     sin duplicar el resto).
    ///   · CLIC DERECHO para desplegar su categoría.
    ///
    /// La clase es abstracta (tML no la autoledea): cada categoría concreta
    /// declara su Contenido(), su color y sus líneas de tooltip.
    /// </summary>
    public abstract class BolsaCategoria : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 1;
            Item.consumable = false;        // PERMANENTE: se reabre cuantas veces haga falta
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(0, 50, 0, 0);
            Item.expert = false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "B",
                Language.GetTextValue("Mods.AethonMod.Items.BolsaCategoria.Titulo",
                    ColorHex(ColorFiesta), Titulo.ToUpper())));
            tooltips.Add(new TooltipLine(Mod, "B2",
                Language.GetTextValue("Mods.AethonMod.Items.BolsaCategoria.Linea1")));
            tooltips.Add(new TooltipLine(Mod, "B3",
                Language.GetTextValue("Mods.AethonMod.Items.BolsaCategoria.Linea2")));
            if (!string.IsNullOrEmpty(Nota))
                tooltips.Add(new TooltipLine(Mod, "B4",
                    Language.GetTextValue("Mods.AethonMod.Items.BolsaCategoria.Nota", Nota)));
        }

        public override bool CanRightClick() => true;

        public override void RightClick(Player player)
        {
            // v6.50.2 — FIX (las 16 bolsas NO entregaban NADA en MP):
            // RightClick corre SOLO en el cliente que clica (ItemSlot lo
            // llama desde la UI — el server JAMAS lo ejecuta), así que el
            // guard "MPClient → return" mataba la entrega en la ÚNICA
            // máquina que la hacía. El inventario ES client-authoritative
            // en vanilla (se sincroniza solo): Dar() escribe slots y el
            // juego los difunde. El guard server viejo cubría un caso que
            // no existe.
            int entregados = 0;
            foreach ((int tipo, int pila) in Contenido())
            {
                if (!Tiene(player, tipo))
                {
                    Dar(player, tipo, pila);
                    entregados++;
                }
            }

            // === LA APERTURA (el momento de la casa: FX + texto) ===
            // (host incluido: Main.dedServ es "sin pantalla", netMode 1
            // en listen server SÍ la tiene)
            if (!Main.dedServ)
            {
                // 24 chispas del color de la categoría alrededor del jugador.
                for (int i = 0; i < 24; i++)
                {
                    float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(player.Center,
                        i % 3 == 0 ? DustID.GoldFlame : DustID.Torch,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                            Main.rand.NextFloat(1.5f, 4.0f),
                        180, new Color(ColorFiesta.R, ColorFiesta.G, ColorFiesta.B), 0.8f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                Lighting.AddLight(player.Center,
                    ColorFiesta.R / 255f * 0.9f, ColorFiesta.G / 255f * 0.7f,
                    ColorFiesta.B / 255f * 1.1f);
            }

            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item4.WithPitchOffset(-0.25f), player.Center);

            if (player.whoAmI == Main.myPlayer)
            {
                // v6.50.3 — FIX (strings→hjson, la regla v6.50.2): los dos
                // mensajes de la apertura viajan por localización.
                Main.NewText(entregados > 0
                    ? Language.GetTextValue("Mods.AethonMod.Items.BolsaCategoria.Abierta",
                        NombreCorto, entregados)
                    : Language.GetTextValue("Mods.AethonMod.Items.BolsaCategoria.Completa",
                        NombreCorto),
                    new Color(230, 196, 255));
            }
        }

        // ==================================================================
        //  LO QUE CADA CATEGORÍA DECLARA
        // ==================================================================

        /// <summary>El título de la bolsa (línea destacada del tooltip).</summary>
        protected abstract string Titulo { get; }

        /// <summary>El nombre corto para los mensajes de apertura.</summary>
        protected abstract string NombreCorto { get; }

        /// <summary>El color de la fiesta de la apertura.</summary>
        protected abstract Color ColorFiesta { get; }

        /// <summary>La nota de la cuarta línea (opcional).</summary>
        protected virtual string Nota => "";

        /// <summary>EL CONTENIDO de la categoría — la lista de la verdad.</summary>
        protected abstract List<(int tipo, int pila)> Contenido();

        // ==================================================================
        //  LOS HELPERS (la semántica de "garantía" de la casa)
        // ==================================================================

        /// <summary>¿El jugador tiene este ítem en el inventario (58 slots)?</summary>
        protected static bool Tiene(Player player, int itemType)
        {
            for (int i = 0; i < 58; i++)
            {
                if (player.inventory[i] != null &&
                    player.inventory[i].type == itemType)
                    return true;
            }
            return false;
        }

        /// <summary>Entrega el ítem en la primera ranura libre (o al suelo).</summary>
        protected static void Dar(Player player, int itemType, int stack)
        {
            for (int i = 0; i < 58; i++)
            {
                if (player.inventory[i] == null ||
                    player.inventory[i].type == ItemID.None)
                {
                    player.inventory[i].SetDefaults(itemType);
                    player.inventory[i].stack = stack;
                    return;
                }
            }
            // v6.50.2 — nota honesta: inventario LLENO → el ítem cae al
            // suelo. En SP/server el drop es real; en un cliente MP es
            // un fallback local (Item.NewItem no difunde desde clientes —
            // caso raro: la bolsa solo entrega lo que FALTA).
            int drop = Item.NewItem(player.GetSource_GiftOrReward(),
                player.Center, itemType, stack);
            if (drop >= 0 && drop < Main.item.Length)
                Main.item[drop].noGrabDelay = 0;
        }

        /// <summary>Color → "RRGGBB" para las etiquetas de tooltip.</summary>
        protected static string ColorHex(Color c)
            => $"{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
