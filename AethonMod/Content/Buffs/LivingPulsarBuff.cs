using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// v5.99 — Buff de EL PÚLSAR VIVO (minion invocador).
    ///
    /// Patrón del arsenal (CosmicOrb/NebulaJellyfishBuff): aparece mientras
    /// el púlsar siga vivo; se elimina solo si ya no queda ninguno, y el
    /// propio púlsar lo refresca cada tick.
    /// </summary>
    public class LivingPulsarBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.LivingPulsarMinion>()] == 0)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
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
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.LivingPulsarMinion>());
            }
            catch { }
            return true;   // y el buff se cancela (vanilla lo retira)
        }
    }
}
