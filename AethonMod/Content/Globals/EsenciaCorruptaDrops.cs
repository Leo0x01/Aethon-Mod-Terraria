using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  LAS DROPS DE ESENCIA CORRUPTA (v6.51.0) — EL FILTRO ENTRE LAS
    //  REALIDADES (la letra del usuario):
    //
    //  «en el momento en el que el grimorio hace su aparición en el
    //  mundo, su poder se filtra entre las realidades, y este aunque
    //  inocuo para todos se pega a ellos, cada criatura tiene una
    //  probabilidad de 0.1% de soltar 1 esencia corrupta, cada jefe
    //  suelta de 1 a 10 esencias corruptas siendo 10 una probabilidad de
    //  1%, 3 esencia corrupta una probabilidad de 5% y 1 esencia
    //  corrupta una probabilidad de 90%, que el resto de 1 a 10 tomen su
    //  probabilidad de esa muestra».
    //
    //  LA PUERTA: nada cae ANTES de LA LLEGADA (ElPoderSeFiltra — el
    //  poder se pegó al mundo cuando la bruma lo tocó). LA CHUSMA Y LOS
    //  JEFES DE LAS OLEADAS DE HAMBRE no pasan por aquí: su probabilidad
    //  es MAYOR (2 % la chusma, 10 planos los jefes — lo paga
    //  OleadaNPC.OnKill, que también peló su loot).
    // ======================================================================
    public class EsenciaCorruptaDrops : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if (npc == null || !npc.active) return;

                // LA PUERTA: el poder del grimorio aún no se filtra aquí.
                if (!Content.Systems.DespertarGrimorioSistema.ElPoderSeFiltra())
                    return;

                // LAS OLEADAS TIENEN SU PROPIA MESA (2 % la chusma, 10 el
                // jefe — y su loot pelado: OleadaNPC.OnKill).
                var sello = npc.GetGlobalNPC<Content.Globals.OleadaNPC>();
                if (sello != null && sello.EsDeOleada) return;

                // EL GRIMORIO DORMIDO JAMÁS ES UNA PRESA.
                if (npc.type == ModContent.NPCType<Content.NPCs.GrimorioDormido>()) return;

                int tipoEsencia = ModContent.ItemType
                    <Content.Items.Altar.ItemCorrupto>();

                // === LOS JEFES: 1..10 (90 % → 1 · 5 % → 3 · 1 % → 10 ·
                //     el resto de la muestra repartido entre 2,4,5,6,7,8,9) ===
                bool esJefe = npc.boss ||
                    npc.type == NPCID.EaterofWorldsHead ||      // el Devorador no lleva flag
                    npc.type == NPCID.BrainofCthulhu;
                if (esJefe)
                {
                    int dado = Main.rand.Next(1000);
                    int cantidad;
                    if (dado < 900) cantidad = 1;               // 90 %
                    else if (dado < 950) cantidad = 3;          // 5 %
                    else if (dado < 960) cantidad = 10;         // 1 %
                    else
                    {
                        // EL RESTO DE LA MUESTRA: 4 % repartido entre
                        // 2,4,5,6,7,8,9 (cada uno ~0,57 %).
                        int[] resto = { 2, 4, 5, 6, 7, 8, 9 };
                        cantidad = resto[Main.rand.Next(resto.Length)];
                    }
                    Soltar(npc, tipoEsencia, cantidad);
                    return;
                }

                // === LAS CRIATURAS: 0,1 % → 1 esencia (la letra) ===
                if (Main.rand.Next(1000) == 0)
                    Soltar(npc, tipoEsencia, 1);
            }
            catch { }
        }

        /// <summary>EL DROP limpio: nace sobre el cuerpo y camina al cable.</summary>
        private static void Soltar(NPC npc, int tipo, int cantidad)
        {
            if (cantidad <= 0) return;
            int idx = Item.NewItem(npc.GetSource_Loot(),
                (int)npc.Center.X, (int)npc.Center.Y, 16, 24, tipo, cantidad);
            if (idx >= 0 && Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx);
        }
    }
}
