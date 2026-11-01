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
                if (puerta <= 0) return;

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
                NPCID.Deerclops            => 15,
                NPCID.WallofFlesh          => 20,
                NPCID.QueenSlimeBoss       => 23,
                NPCID.Retinazer           => 25,   // los Gemelos: cada lente…
                NPCID.Spazmatism          => 25,   // …cierra la MISMA puerta
                NPCID.TheDestroyer         => 28,
                NPCID.SkeletronPrime       => 30,
                NPCID.Plantera             => 32,  // la puerta ALTA de la letra
                NPCID.Golem                => 35,
                NPCID.DukeFishron          => 38,
                NPCID.HallowBoss           => 40,  // la Emperatriz de la Luz
                NPCID.CultistBoss          => 45,  // el Cultista Lunático (v6.75 — la antesala de la cima)
                NPCID.MoonLordCore         => 50,  // el corazón del Señor de la Luna — LA CIMA (v6.75)
                NPCID.MoonLordHead         => 50,  // (la cabeza cuenta si el núcleo voló raro)
                _ => 0,
            };
        }
    }
}
