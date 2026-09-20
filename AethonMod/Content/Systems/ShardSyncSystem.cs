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
        /// v6.49 — EcoRed (voces/hambre/latido al portador) se enruta a su
        /// propio receptor: la doble puerta vive allá.
        /// </summary>
        public static void HandlePacket(BinaryReader reader)
        {
            try
            {
                byte msgType = reader.ReadByte();
                switch (msgType)
                {
                    case SyncResonance:
                        // TODO: implementar sync de resonancia.
                        break;

                    case EcoRed.MsgVoz:
                    case EcoRed.MsgHambre:
                    case EcoRed.MsgLatidoXp:
                        // rewind 1 byte: EcoRed lee el TIPO de nuevo (su
                        // propio switch lo necesita para el filtro).
                        reader.BaseStream.Position -= 1L;
                        EcoRed.Recibir(reader);
                        break;
                }
            }
            catch { }
        }
    }
}
