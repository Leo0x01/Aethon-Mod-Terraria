
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// Buff del CosmicOrbMinion — aparece en la zona de buffs del jugador.
    /// Mientras este buff este activo, el minion cosmico permanece invocado.
    /// </summary>
    public class CosmicOrbBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName y Description se cargan desde Localization
            Main.buffNoTimeDisplay[Type] = true; // no muestra el timer (como los minions)
            Main.buffNoSave[Type] = true; // no se guarda al salir (se recrea al equipar el grimorio)
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // El buff se mantiene mientras el minion este activo
            if (player.ownedProjectileCounts[ModContent.ProjectileType<global::AethonMod.Content.Projectiles.CosmicOrbMinion>()] == 0)
            {
                // Si no hay minion activo, remover el buff
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                // Resetear el timer del buff (que no se agote)
                player.buffTime[buffIndex] = 18000;
            }
        }
    }
}
