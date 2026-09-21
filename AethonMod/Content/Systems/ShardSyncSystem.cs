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
        /// v6.50 — los LIBROS y la CRÓNICA caminan por el mismo cauce (y el
        /// PEDIDO de entrada llega del CLIENTE: el whoAmI del remitente
        /// decide a quién contesta el servidor).
        /// v6.50.2 — MsgPrepararOleadas (10) se suma al mismo cauce: la
        /// selección de la Carnada viaja al server por aquí.
        /// </summary>
        public static void HandlePacket(BinaryReader reader, int whoAmI = -1)
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
                    case EcoRed.MsgLibro:
                    case EcoRed.MsgCronica:
                    case EcoRed.MsgPedirLibros:
                    case EcoRed.MsgPedirFragmento:
                    case EcoRed.MsgPrepararOleadas:
                        // v6.50.1 — FIX: MsgPedirFragmento (9) no estaba en
                        // el switch — el paquete del Altar moría en silencio
                        // y el Fragmento Génesis era inobtenible en MP (el
                        // cliente veía el mensaje de reclamado sin que nada
                        // naciera). Rewind 1 byte: EcoRed lee el TIPO de
                        // nuevo (su propio switch lo necesita para el filtro).
                        // v6.50.2 — FIX: MsgPrepararOleadas (10) en el mismo
                        // grupo (el router de EcoRed lo cubre — sin case, la
                        // selección de la carnada moriría igual que el
                        // fragmento en su día).
                        reader.BaseStream.Position -= 1L;
                        EcoRed.Recibir(reader, whoAmI);
                        break;
                }
            }
            catch { }
        }
    }
}
