using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  ProyectadoDropsNPC — v6.177 — LAS MEJORAS QUE SUOLTAN LOS JEFES.
    //
    //  El plano del meme: cada jefe de Terraria suelta SU consumible de
    //  un solo uso (la energía proyectada sube y la recarga acelera):
    //   · el OJO DE CTHULHU → la CARTA QUE NUNCA LE DISTE
    //   · ESQUELETRÓN → la CAPTURA DEL VISTO
    //   · el MURO CARNOSO → el «NO ERES TÚ, SOY YO» (la más gruesa: +30)
    //   · los MECÁNICOS → los TRES PROYECTORES (salón · cine · IMAX) en
    //     el ORDEN en que caen (el primero que cae suelta el del salón…)
    //   · PLANTERA → las FLORES AMARILLAS
    //   · GÓLEM → el «YA LO SUPERÉ»
    //   · el DUQUE PEZ DRAGÓN → el «HAY MÁS PECES EN EL MAR» (+10)
    //   · el SEÑOR DE LA LUNA → LA LUNA QUE TE IBAS A BAJAR (¡INFINITA!)
    // ======================================================================
    public class ProyectadoDropsNPC : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            switch (npc.type)
            {
                case NPCID.EyeofCthulhu:
                    npcLoot.Add(ItemDropRule.Common(ModContent.ItemType
                        <global::AethonMod.Content.Items.Investigaciones.CartaQueNuncaLeDiste>(), 1));
                    break;
                case NPCID.SkeletronHead:
                    npcLoot.Add(ItemDropRule.Common(ModContent.ItemType
                        <global::AethonMod.Content.Items.Investigaciones.CapturaDelVisto>(), 1));
                    break;
                case NPCID.WallofFlesh:
                    npcLoot.Add(ItemDropRule.Common(ModContent.ItemType
                        <global::AethonMod.Content.Items.Investigaciones.NoEresTuSoyYo>(), 1));
                    break;
                case NPCID.Retinazer:
                case NPCID.Spazmatism:
                case NPCID.SkeletronPrime:
                case NPCID.TheDestroyer:
                    // LOS PROYECTORES por ORDEN de caída (el plano: el
                    // primero suelta el del salón, el segundo el de cine,
                    // el tercero el IMAX)
                    npcLoot.Add(ItemDropRule.ByCondition(new CondicionMecanico(1),
                        ModContent.ItemType<global::AethonMod.Content.Items.Investigaciones.ProyectorDelSalon>(), 1));
                    npcLoot.Add(ItemDropRule.ByCondition(new CondicionMecanico(2),
                        ModContent.ItemType<global::AethonMod.Content.Items.Investigaciones.ProyectorDeCine>(), 1));
                    npcLoot.Add(ItemDropRule.ByCondition(new CondicionMecanico(3),
                        ModContent.ItemType<global::AethonMod.Content.Items.Investigaciones.ProyectorImax>(), 1));
                    break;
                case NPCID.Plantera:
                    npcLoot.Add(ItemDropRule.Common(ModContent.ItemType
                        <global::AethonMod.Content.Items.Investigaciones.FloresAmarillas>(), 1));
                    break;
                case NPCID.Golem:
                    npcLoot.Add(ItemDropRule.Common(ModContent.ItemType
                        <global::AethonMod.Content.Items.Investigaciones.YaLoSupere>(), 1));
                    break;
                case NPCID.DukeFishron:
                    npcLoot.Add(ItemDropRule.Common(ModContent.ItemType
                        <global::AethonMod.Content.Items.Investigaciones.HayMasPeces>(), 1));
                    break;
                case NPCID.MoonLordCore:
                    npcLoot.Add(ItemDropRule.Common(ModContent.ItemType
                        <global::AethonMod.Content.Items.Investigaciones.LunaQueTeIbaABajar>(), 1));
                    break;
            }
        }

        // ================================================================
        //  LA CONDICIÓN DEL MECÁNICO (el plano): ¿es tu primer/segundo/
        //  tercer jefe mecánico? — cuenta los ya caídos + el que cae
        //  ahora (los gemelos caen JUNTOS: cuentan como uno).
        // ================================================================
        private class CondicionMecanico : IItemDropRuleCondition
        {
            private readonly int _orden;
            public CondicionMecanico(int orden) { _orden = orden; }

            public bool CanDrop(DropAttemptInfo info)
            {
                NPC npc = info.npc;
                if (npc == null) return false;
                // los gemelos caen juntos: solo cuenta la pareja una vez
                if (npc.type == NPCID.Retinazer && NPC.AnyNPCs(NPCID.Spazmatism)) return false;
                if (npc.type == NPCID.Spazmatism && NPC.AnyNPCs(NPCID.Retinazer)) return false;
                int n = 0;
                if (NPC.downedMechBoss1 || npc.type == NPCID.TheDestroyer) n++;
                if (NPC.downedMechBoss2 || npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism) n++;
                if (NPC.downedMechBoss3 || npc.type == NPCID.SkeletronPrime) n++;
                return n == _orden;
            }

            public bool CanShowItemDropInUI() => true;

            public string GetConditionDescription()
            {
                // v6.177 — la clave se arma en variable: el loop cuenta
                // los literales y esta es DINÁMICA (Condicion1/2/3 viven)
                string clave = "Mods.AethonMod.Proyectado.Condicion" + _orden.ToString();
                return Language.GetTextValue(clave);
            }
        }
    }
}
