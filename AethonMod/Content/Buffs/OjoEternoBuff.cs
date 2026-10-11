using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// OjoEternoBuff — v6.50.105 — el buff de LOS OJOS DEL ETERNO.
    ///
    /// La letra del usuario: «los minion que use el grimorio será su
    /// propio ojo» — los minions del Grimorio del Eterno ya no son el
    /// Orbe Cósmico: son los OJOS del libro, que flotan en corona sobre
    /// el dueño (u orbitan el libro flotante en anillos concéntricos) y
    /// disparan el rayo Perlin negro de bordes morados.
    ///
    /// El patrón del Orbe Cósmico (v6.50.3: el timeLeft nunca expira) y
    /// la textura del buff es EL OJO ABIERTO (el mismo sprite del minion).
    /// </summary>
    public class OjoEternoBuff : ModBuff
    {
        public override string Texture => "AethonMod/Content/Projectiles/OjoEterno_Abierto";

        public override void SetStaticDefaults()
        {
            // DisplayName y Description se cargan desde Localization
            Main.buffNoTimeDisplay[Type] = true; // no muestra el timer (como los minions)
            Main.buffNoSave[Type] = true;        // no se guarda al salir (se recrea al invocar)
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // El buff se mantiene mientras haya ojos activos
            if (player.ownedProjectileCounts[
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.OjoEternoMinion>()] == 0)
            {
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
