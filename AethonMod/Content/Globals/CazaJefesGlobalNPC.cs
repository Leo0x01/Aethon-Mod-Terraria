using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  v6.67 — EL TESTIGO DE LA CAZA: cuando un JEFE PRINCIPAL de
    //  Terraria muere, TODOS los jugadores vivos anotan SU puerta en el
    //  registro del cazador (CazaJefesPlayer — la letra: «estamos en
    //  nivel 1 y matamos a plantera… cuando lleguemos al nivel de
    //  despertar que la pide entonces este ya estaria marcado»).
    //
    //  El mapa NPC→puerta es el ESPEJO EXACTO de
    //  DespertarGrimorio.LosJefesDelDespertar (la escalera de la caza):
    //  el Rey Gelatina paga la puerta del 3 · Plantera la del 32 · el
    //  Señor de la Luna LA DEL 50 — LA CIMA (v6.75: la letra pidió al
    //  dios final como requisito del nivel máximo del despertar).
    //  Los segmentos del Devorador y los dos Gemelos comparten puerta
    //  (la pareja ES el jefe — la casa de EcoSistema ya los trataba
    //  igual).
    //
    //  (La banda del mundo de vanilla — NPC.downed* — se escribe sola:
    //  es la OTRA fuente del crédito, la que ya cubría las muertes de
    //  ANTES del mod. Este global cubre el registro personal: lo que el
    //  cazador carga a otros mundos.)
    // ======================================================================
    public class CazaJefesGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            try
            {
                int puerta = PuertaDe(npc.type);
                if (puerta > 0)
                {
                    // LA PUERTA SE PAGA PARA TODAS LAS PANTALLAS VIVAS (una
                    // cacería es de quien la ve — y del mundo, vía la bandera
                    // de vanilla, que ya se escribió sola).
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p == null || !p.active || p.dead) continue;
                        p.GetModPlayer<Players.CazaJefesPlayer>()?.Pagar(puerta);
                    }
                }

                // ============================================================
                //  v6.86 — LOS CONTADORES DE LA IRA (la letra de la cima:
                //  «venciendo a moon lord al menos 5 veces, y venciendo al
                //  fragmento de un recuerdo lejano Aethon 1 vez»): el
                //  despertar 100 ya NO basta — estos relojes los paga el
                //  OnKill (todas las pantallas vivas, como las puertas).
                // ============================================================
                if (npc.type == NPCID.MoonLordCore || npc.type == NPCID.MoonLordHead)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p == null || !p.active || p.dead) continue;
                        var cazador = p.GetModPlayer<Players.CazaJefesPlayer>();
                        if (cazador != null) cazador.VecesMoonLord++;
                    }
                }
                if (npc.type == ModContent.NPCType<NPCs.AethonBoss>() ||
                    npc.type == ModContent.NPCType<NPCs.AethonBossOscuro>() ||
                    npc.type == ModContent.NPCType<NPCs.AethonBossFusionado>())
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p == null || !p.active || p.dead) continue;
                        var cazador = p.GetModPlayer<Players.CazaJefesPlayer>();
                        if (cazador == null) continue;
                        cazador.VecesAethon++;
                        cazador.Pagar(59);   // LA PUERTA DEL RECUERDO LEJANO
                    }
                }

                // ============================================================
                //  v6.86 — LA MURALLA SUELTA EL LIBRO (la letra del
                //  peldaño 20): «o que directamente lo suelte la muralla
                //  de carne con una probabilidad del 50%» — el LIBRO
                //  CORRUPTO cae al suelo del mundo (lo levanta quien no
                //  tenga el suyo: CanPickup filtra al resto — 1 por
                //  cazador, para siempre).
                // ============================================================
                if (npc.type == NPCID.WallofFlesh && Main.rand.NextBool(2))
                {
                    int idx = Item.NewItem(npc.GetSource_Loot(), npc.Hitbox,
                        ModContent.ItemType<Items.Altar.LibroCorrupto>(), 1);
                    if (idx >= 0 && Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx);
                }
            }
            catch { }
        }

        /// <summary>LA PUERTA QUE PAGA ESTE NPC (0 = ninguno de la
        /// escalera): el espejo de LosJefesDelDespertar, por tipo.</summary>
        private static int PuertaDe(int tipo)
        {
            return tipo switch
            {
                NPCID.KingSlime            => 3,
                NPCID.EyeofCthulhu         => 5,
                NPCID.EaterofWorldsHead    => 8,   // el mal del mundo: la serpiente…
                NPCID.EaterofWorldsBody    => 8,   // …cualquier segmento que muere…
                NPCID.EaterofWorldsTail    => 8,   // …cierra la puerta igual
                NPCID.BrainofCthulhu       => 8,   // …o su primo carmesí
                NPCID.QueenBee             => 10,
                NPCID.SkeletronHead        => 13,
                // v6.86 — Deerclops y el Duque Fishron MURIERON como puerta
                // (la letra: «el jefe Deerclop no lo incluyas en la
                // progresion» + «el jefe duke fishron no necesitas
                // incluirlo») — sus líneas ya no existen.
                NPCID.WallofFlesh          => 20,
                NPCID.QueenSlimeBoss       => 23,
                NPCID.Retinazer           => 25,   // los Gemelos: cada lente…
                NPCID.Spazmatism          => 25,   // …cierra la MISMA puerta
                NPCID.TheDestroyer         => 28,
                NPCID.SkeletronPrime       => 30,
                NPCID.Plantera             => 32,  // la puerta ALTA de la letra
                NPCID.Golem                => 35,
                NPCID.HallowBoss           => 38,  // la Emperatriz de la Luz — v6.86: SU puerta (era la 40)
                NPCID.CultistBoss          => 45,  // el Cultista Lunático (la antesala de la cima)
                NPCID.MoonLordCore         => 50,  // el corazón del Señor de la Luna — LA CIMA de vanilla
                NPCID.MoonLordHead         => 50,  // (la cabeza cuenta si el núcleo voló raro)
                // Aethon (59) NO vive aquí: su OnKill de arriba lo paga
                // (ModContent no entra en un switch de constantes).
                _ => 0,
            };
        }
    }
}
