using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// CriaEstelarBuff — v6.38 — EL BUFF DE LA CRÍA ESTELAR.
    ///
    /// Aparece en la zona de buffs del jugador mientras la camada viva.
    /// Mientras esté activo, la cría (o las crías) permanece invocada;
    /// el propio minion lo sostiene (el patrón de la medusa nebulosa:
    /// su IA lo refresca cada tick mientras viva).
    /// </summary>
    public class CriaEstelarBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName y Description se cargan desde Localization.
            Main.buffNoTimeDisplay[Type] = true;   // sin timer (como los minions)
            Main.buffNoSave[Type] = true;          // no se guarda (se recrea al invocar)
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // El buff vive mientras haya crías activas.
            if (player.ownedProjectileCounts[
                ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.CriaEstelarMinion>()] == 0)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                // El timer nunca se agota (la cría lo sostiene).
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
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.CriaEstelarMinion>());
            }
            catch { }
            return true;   // y el buff se cancela (vanilla lo retira)
        }
    }
}
