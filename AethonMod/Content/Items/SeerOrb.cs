using System.Collections.Generic;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// Orbe del Vidente — item de PRUEBA para experimentar con el toggle de
    /// tooltip de 2 ventanas (básico/completo) sin tocar el Grimorio.
    ///
    /// v5.6: Item experimental. Si funciona bien el toggle, se puede migrar
    /// el mismo patrón al Grimorio. Si no funciona, no afecta al Grimorio.
    ///
    /// Comportamiento:
    /// - Click derecho en el inventario alterna entre vista básica y completa
    /// - Vista básica: solo nombre + descripción corta
    /// - Vista completa: nombre + descripción + todas las estadísticas de prueba
    /// - No se consume al hacer click derecho (usa TooltipToggleOrb GlobalItem)
    /// </summary>
    public class SeerOrb : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Blue;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Leer el flag del GlobalItem (SeerOrbToggle)
            bool showExtended = SeerOrbToggle.ShowExtendedTooltip;

            if (showExtended)
            {
                // === VISTA COMPLETA ===
                tooltips.Add(new TooltipLine(Mod, "Section1", "[c/78FF96:═══ ESTADÍSTICAS ═══]"));
                tooltips.Add(new TooltipLine(Mod, "Stat1", "[c/FF5555:+100% daño de prueba]"));
                tooltips.Add(new TooltipLine(Mod, "Stat2", "[c/55AAFF:+50 mana máximo]"));
                tooltips.Add(new TooltipLine(Mod, "Stat3", "[c/FFAA55:+10% probabilidad crítica]"));
                tooltips.Add(new TooltipLine(Mod, "Stat4", "[c/BE78FD:+2 slots de minion]"));
                tooltips.Add(new TooltipLine(Mod, "Stat5", "[c/FF5566:♥ Robo de vida: +5%]"));
                tooltips.Add(new TooltipLine(Mod, "Stat6", "[c/78FF96:★ Bonus: +20% velocidad]"));
                tooltips.Add(new TooltipLine(Mod, "Mode1",
                    "[c/78788C:Click der para vista básica]"));
            }
            else
            {
                // === VISTA BÁSICA ===
                tooltips.Add(new TooltipLine(Mod, "Desc",
                    "[c/B388FF:Orbe de prueba para experimentar con tooltip de 2 ventanas.]"));
                tooltips.Add(new TooltipLine(Mod, "Mode2",
                    "[c/78788C:Click der para vista completa]"));
            }
        }
    }

    /// <summary>
    /// GlobalItem que maneja el toggle de vista del SeerOrb.
    /// Es SEPARADO del TooltipToggleItem del Grimorio para no interferir.
    ///
    /// Enfoque: usar ModifyTooltips para detectar el click derecho mientras
    /// el tooltip está visible. NO escribe en el inventario (solo lee/modifica
    /// un flag estático), por lo que no debería corromper el estado del jugador.
    /// </summary>
    public class SeerOrbToggle : GlobalItem
    {
        public override bool InstancePerEntity => false;

        // Flag estático — NO se persiste por-item (más simple y seguro para pruebas)
        public static bool ShowExtendedTooltip = false;

        private static bool _rightMouseLast = false;

        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            try
            {
                return item.type == ModContent.ItemType<SeerOrb>();
            }
            catch
            {
                return false;
            }
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            // Solo en cliente, con inventario abierto
            if (Main.dedServ || !Main.playerInventory) return;

            // Detectar flanco de subida del click derecho
            bool rightMouseNow = Main.mouseRight;
            if (!rightMouseNow || _rightMouseLast)
            {
                _rightMouseLast = rightMouseNow;
                return;
            }
            _rightMouseLast = rightMouseNow;

            // Alternar el flag estático (NO toca el inventario)
            ShowExtendedTooltip = !ShowExtendedTooltip;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen);
        }
    }
}
