using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// v5.97 — Buff de LA MEDUSA NEBULAR (minion invocador).
    ///
    /// Aparece en la zona de buffs del jugador mientras la medusa siga
    /// viva (patrón del CosmicOrbBuff): el buff se elimina a sí mismo si
    /// ya no queda ninguna medusa invocada, y la medusa lo mantiene fresco
    /// (su IA lo refresca cada tick — ver NebulaJellyfishMinion).
    /// </summary>
    public class NebulaJellyfishBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName y Description se cargan desde Localization
            Main.buffNoTimeDisplay[Type] = true; // no muestra el timer (como los minions)
            Main.buffNoSave[Type] = true;        // no se guarda al salir del mundo
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // El buff vive mientras la medusa esté activa.
            if (player.ownedProjectileCounts[
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.NebulaJellyfishMinion>()] == 0)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                // Resetear el timer del buff (que no se agote).
                player.buffTime[buffIndex] = 18000;
            }
        }

        // ================================================================
        //  v6.51.12 — LA DESPEDIDA LEGÍTIMA (la letra del usuario: «solo
        //  se deben desactivar si el jugador los quita usando su buff de
        //  minion»): el CLIC DERECHO en ESTE icono despide al escuadrón
        //  entero (se disuelven en tinta). Es la ÚNICA despedida que
        //  queda — la barra de buffs llena ya NO los mata (ver
        //  EscuadronUtil: la cura de la desinvocación silenciosa).
        // ================================================================
        public override bool RightClick(int buffIndex)
        {
            try
            {
                global::AethonMod.Content.Systems.EscuadronUtil.Despedir(
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.NebulaJellyfishMinion>());
            }
            catch { }
            return true;   // y el buff se cancela (vanilla lo retira)
        }
    }
}
