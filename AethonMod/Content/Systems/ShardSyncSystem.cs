using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Sistema de sync multi-jugador.
    ///
    /// NOTA: El nivel/XP del arma ahora vive por-item (ShardLevelItem) y se persiste
    /// via TagCompound del item. El estado del jugador (rama, kills, resonancia)
    /// sigue siendo per-jugador y se sincroniza aqui.
    /// </summary>
    public class ShardSyncSystem : ModSystem
    {
        public const byte SyncResonance = 2;

        public override void NetSend(BinaryWriter writer)
        {
            // No necesario por ahora.
        }

        public override void NetReceive(BinaryReader reader)
        {
            // No necesario por ahora.
        }

        /// <summary>
        /// Procesa un paquete recibido.
        /// </summary>
        public static void HandlePacket(BinaryReader reader)
        {
            byte msgType = reader.ReadByte();
            switch (msgType)
            {
                case SyncResonance:
                    // TODO: implementar sync de resonancia.
                    break;
            }
        }
    }
}
