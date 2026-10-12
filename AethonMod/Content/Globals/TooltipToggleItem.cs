using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// TooltipToggleItem — GlobalItem que detecta clicks derechos en el Grimorio
    /// mientras está en el inventario y cicla las ventanas de estadísticas.
    ///
    /// v5.7: Migrado al patrón del SeerOrb (que funciona correctamente).
    /// Usa un flag ESTÁTICO (no por-item) en vez de modificar ShardLevelItem.
    /// Esto evita el error 134124 que ocurría en v5.6 anterior cuando se
    /// modificaba player.inventory[i] dentro de ModifyTooltips.
    ///
    /// v6.50.110 — TRES VENTANAS (la letra: «la segunda ventana de info del
    /// grimorio tiene demasiada informacion, crea una nueva ventana de
    /// informacion para repartir mejor la informacion»): la vista completa
    /// de la .108 se PARTE en dos — el clic derecho cicla
    /// BÁSICA → EL ARMA → EL FESTÍN → BÁSICA:
    ///   · 0 BÁSICA   — la caja compacta de la .108 (daño, lore, progresión).
    ///   · 1 EL ARMA  — DAÑO · RECURSOS · PROYECTIL · BONUS (el poder).
    ///   · 2 EL FESTÍN — LOS OJOS DEL ETERNO · PROGRESIÓN · LA PRIMERA CENA
    ///     (los minion y la vida del libro).
    /// </summary>
    public class TooltipToggleItem : GlobalItem
    {
        public override bool InstancePerEntity => false;

        // Flag estático — NO se persiste por-item (igual que SeerOrbToggle)
        /// <summary>La ventana activa: 0 = básica · 1 = EL ARMA · 2 = EL FESTÍN.</summary>
        public static int VistaEstadisticas = 0;

        /// <summary>Compat v6.50.108: ¿alguna ventana de estadísticas abierta?</summary>
        public static bool ShowExtendedTooltip => VistaEstadisticas != 0;

        private static bool _rightMouseLast = false;

        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            try
            {
                return item.type == ModContent.ItemType<Weapons.GrimoireEternal>();
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

            // Guard: Main.LocalPlayer puede ser null en pantalla de selección
            if (Main.LocalPlayer == null) return;

            // Detectar flanco de subida del click derecho
            bool rightMouseNow = Main.mouseRight;
            if (!rightMouseNow || _rightMouseLast)
            {
                _rightMouseLast = rightMouseNow;
                return;
            }
            _rightMouseLast = rightMouseNow;

            // Ciclar la ventana (NO toca el inventario): básica → arma → festín → básica
            VistaEstadisticas = (VistaEstadisticas + 1) % 3;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen);
        }
    }
}
