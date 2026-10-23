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
    /// BÁSICA → EL ARMA → EL FESTÍN → BÁSICA.
    ///
    /// v6.51.9 — CUATRO VENTANAS (la letra: «poner en una nueva ventana de
    /// informacion todo el apartado de Progresion, y que la ventana de
    /// progresion sea la segunda ventana, primero la ventana normal, luego
    /// la de progresion, luego la de las estadisticas del arma y por ultimo
    /// la ventana de festin, la ventana de festin deberia tener otro nombre
    /// ya que lo que muestra es informacion del escuadron y sus ataques
    /// combinados»): el apartado PROGRESIÓN se muda a SU PROPIA ventana y
    /// la vieja ventana del festín se llama EL ESCUADRÓN (eso es lo que
    /// enseña: el escuadrón y sus ataques combinados). El clic derecho cicla
    ///   · 0 BÁSICA       — la caja compacta (daño, lore).
    ///   · 1 PROGRESIÓN   — nivel, despertar, requisitos, hambre, hitos.
    ///   · 2 EL ARMA      — DAÑO · RECURSOS · PROYECTIL · BONUS (el poder).
    ///   · 3 EL ESCUADRÓN — LOS OJOS DEL ETERNO · LO DEVORADO · LOS
    ///     ATAQUES COMBINADOS (el escuadrón del libro y su festín).
    /// </summary>
    public class TooltipToggleItem : GlobalItem
    {
        public override bool InstancePerEntity => false;

        // Flag estático — NO se persiste por-item (igual que SeerOrbToggle)
        /// <summary>La ventana activa: 0 = básica · 1 = PROGRESIÓN ·
        /// 2 = EL ARMA · 3 = EL ESCUADRÓN.</summary>
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

            // Ciclar la ventana (NO toca el inventario):
            // básica → progresión → arma → escuadrón → básica
            VistaEstadisticas = (VistaEstadisticas + 1) % 4;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen);
        }
    }
}
