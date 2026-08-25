using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Sistema de sync multi-jugador. Usa NetMessage para sincronizar el estado
    /// del fragmento de cada jugador entre cliente y servidor.
    ///
    /// Mensajes:
    /// - sync nivel/XP/rama del jugador (cliente -> servidor -> otros).
    /// - sync resonancia (cliente -> servidor).
    /// </summary>
    public class ShardSyncSystem : ModSystem
    {
        // IDs de paquetes de red.
        public const byte SyncShardState = 1;
        public const byte SyncResonance = 2;

        public override void NetSend(BinaryWriter writer)
        {
            // Enviar estado del servidor al cliente al conectarse.
            // (No necesario por ahora: cada jugador persiste su propio estado.)
        }

        public override void NetReceive(BinaryReader reader)
        {
            // Recibir estado del servidor.
        }

        /// <summary>
        /// Envia el estado del fragmento del jugador al servidor (y luego a otros clientes).
        /// Llamado cuando el nivel cambia.
        /// </summary>
        public static void SendShardState(Player player)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.SinglePlayer) return;
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return;

            var packet = ModContent.GetInstance<AethonMod>().GetPacket();
            packet.Write((byte)SyncShardState);
            packet.Write((byte)player.whoAmI);
            packet.Write(sp.ShardLevel);
            packet.Write(sp.ShardXP);
            packet.Write((int)sp.ActiveBranch);
            packet.Write(sp.ResonanceShards);
            packet.Send();
        }

        /// <summary>
        /// Procesa un paquete recibido.
        /// </summary>
        public static void HandlePacket(BinaryReader reader)
        {
            byte msgType = reader.ReadByte();
            switch (msgType)
            {
                case SyncShardState:
                    byte playerWho = reader.ReadByte();
                    int level = reader.ReadInt32();
                    int xp = reader.ReadInt32();
                    int branch = reader.ReadInt32();
                    int resonance = reader.ReadInt32();

                    Player p = Main.player[playerWho];
                    if (p != null && p.active)
                    {
                        var sp = p.GetModPlayer<Players.ShardPlayer>();
                        if (sp != null)
                        {
                            sp.ShardLevel = level;
                            sp.ShardXP = xp;
                            sp.ActiveBranch = (Players.BranchType)branch;
                            sp.ResonanceShards = resonance;
                        }
                    }
                    break;

                case SyncResonance:
                    // TODO: implementar sync de resonancia.
                    break;
            }
        }
    }
}
