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
                    case EcoRed.MsgPrepararOleadas:
                    case EcoRed.MsgAmbienteOleada:
                    case EcoRed.MsgTomarCaida:
                    // v6.50.129 — LA CURA DEL ROUTER COJO (cazada al
                    // auditar la sync de zonas): MsgCambiarHorario (13),
                    // MsgForzarCaida (15) y MsgDevoracion (16) NUNCA
                    // estuvieron en este switch — el server los enviaba
                    // y el router del cliente los TIRABA en silencio (el
                    // poder devorado, el giro del reloj del Escriba y la
                    // Tinta del Cometa jamás llegaban en MP). MsgAltarZonas
                    // (17) estrena con ellos en la lista.
                    case EcoRed.MsgCambiarHorario:
                    case EcoRed.MsgForzarCaida:
                    case EcoRed.MsgDevoracion:
                    case EcoRed.MsgAltarZonas:
                        // v6.50.1 — Rewind 1 byte: EcoRed lee el TIPO de
                        // nuevo (su propio switch lo necesita para el filtro).
                        // v6.50.2 — MsgPrepararOleadas (10) en el mismo grupo
                        // (el router de EcoRed lo cubre).
                        // v6.50.117 — LA PURGA DEL GÉNESIS: el case de
                        // MsgPedirFragmento (9) murió con el Altar — el
                        // paquete huérfano ya no tiene router ni receptor.
                        reader.BaseStream.Position -= 1L;
                        EcoRed.Recibir(reader, whoAmI);
                        break;
                }
            }
            catch { }
        }
    }
}
