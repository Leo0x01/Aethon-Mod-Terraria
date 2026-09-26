using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// v5.99 — Buff de EL COMETA ESTELAR (minion invocador).
    ///
    /// Patrón del arsenal (CosmicOrb/NebulaJellyfishBuff): aparece mientras
    /// el cometa siga vivo; se elimina solo si ya no queda ninguno, y el
    /// propio cometa lo refresca cada tick.
    /// </summary>
    public class StellarCometBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.StellarCometMinion>()] == 0)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;
            }
        }
    }
}
