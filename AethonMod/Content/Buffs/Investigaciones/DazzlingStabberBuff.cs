using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs.Investigaciones
{
    /// <summary>
    /// DazzlingStabberBuff — v6.124 — LA COPIA DE INVESTIGACIÓN del buff
    /// de Calamity Mod (DazzlingStabberBuff, «En garde!») — la marca que
    /// llevas mientras las dagas del Dazzling Stabber Staff velan por ti.
    ///
    /// (Calamity lo resuelve con su BaseSummonBuff + el bool del
    /// modplayer — la casa lo resuelve con su patrón de escuadrón
    /// (StellarCometBuff): el buff vive mientras quede UNA daga y muere
    /// con la última; el clic derecho en el icono DESPIDE a la
    /// escuadra — la regla de la v6.51.12 de la casa.)
    /// </summary>
    public class DazzlingStabberBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Investigaciones.DazzlingStabber>()] == 0)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;
            }
        }

        // LA DESPEDIDA LEGÍTIMA de la casa (v6.51.12): el clic derecho en
        // ESTE icono despide a la escuadra de dagas entera.
        public override bool RightClick(int buffIndex)
        {
            try
            {
                global::AethonMod.Content.Systems.EscuadronUtil.Despedir(
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Investigaciones.DazzlingStabber>());
            }
            catch { }
            return true;   // y el buff se cancela (vanilla lo retira)
        }
    }
}
