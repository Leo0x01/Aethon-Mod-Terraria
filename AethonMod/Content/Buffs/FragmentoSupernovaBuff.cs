using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// FragmentoSupernovaBuff — v6.41 — EL BUFF DEL FRAGMENTO.
    ///
    /// Aparece en la zona de buffs del jugador mientras la singularidad
    /// alada viva a su lado. El propio minion lo sostiene (el patrón de
    /// la cría estelar: su IA lo refresca cada tick).
    /// </summary>
    public class FragmentoSupernovaBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName y Description se cargan desde Localization.
            Main.buffNoTimeDisplay[Type] = true;   // sin timer (como los minions)
            Main.buffNoSave[Type] = true;          // no se guarda (se recrea al invocar)
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // El buff vive mientras haya fragmentos activos.
            if (player.ownedProjectileCounts[
                ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.FragmentoSupernovaMinion>()] == 0)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                // El timer nunca se agota (el fragmento lo sostiene).
                player.buffTime[buffIndex] = 18000;
            }
        }
    }
}
