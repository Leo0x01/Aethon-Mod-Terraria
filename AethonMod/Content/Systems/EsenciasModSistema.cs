using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AethonMod.Content.Items.Esencias;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// EsenciasModSistema — v6.49 — EL CONTADOR DE ALMAS DEL MUNDO.
    ///
    /// LA LETRA DEL USUARIO: "cada jefe de mod debe tener su esencia…
    /// pero para evitar el farmeo de niveles, solo podrán dar hasta 10
    /// esencias por mundo de juego cada jefe; los jefes de oleadas NO
    /// tienen límite" (con la oleada la dificultad escala — el farmeo
    /// se paga en riesgo; con el jefe invocable de prueba no: por eso
    /// el MUNDO lleva la cuenta).
    ///
    /// CÓMO: cada jefe del mod llama SoltarEsencia(NPC) en su OnKill —
    /// este sistema decide si el cofre del mundo aún paga. El contador
    /// por jefe se persiste en el SaveWorldData (TagCompound): 10 por
    /// mundo y NI UNA MÁS — el undécimo intento cae en silencio (el
    /// aviso del "cofre seco" se dice UNA vez por jefe, la primera vez
    /// que se agota).
    ///
    /// MP: OnKill de NPC corre en el servidor; el drop de items y el
    /// conteo viven ahí — el item viaja solo (vanilla lo sincroniza).
    /// </summary>
    public class EsenciasModSistema : ModSystem
    {
        /// <summary>El límite de la casa: 10 almas por jefe por mundo.</summary>
        public const int LimitePorMundo = 10;

        /// <summary>Las esencias ya pagadas por cada jefe (0..4 = Titán, Rift, Arquera, Portador, Aethon).</summary>
        private int[] _dadas = new int[5];

        /// <summary>Ya se anunció el "cofre seco" de cada jefe (una sola vez).</summary>
        private bool[] _avisadoSeco = new bool[5];

        /// <summary>Las esencias pagadas por jefe (lectura de diagnóstico; -1 si índice inválido).</summary>
        public static int Dadas(int jefeIdx)
        {
            var sys = ModContent.GetInstance<EsenciasModSistema>();
            if (sys == null || jefeIdx < 0 || jefeIdx >= 5) return -1;
            return sys._dadas[jefeIdx];
        }

        /// <summary>
        /// EL PAGO DEL JEFE: suelta SU esencia si el mundo todavía debe
        /// almas de ese jefe. Lo llama el OnKill de los cinco — una
        /// línea por jefe, la decisión completa vive aquí.
        /// </summary>
        public static void SoltarEsencia(NPC npc)
        {
            try
            {
                if (npc == null || Main.netMode == NetmodeID.MultiplayerClient) return;

                int tipo = EsenciasJefesModMapa.DeNPC(npc.type);
                if (tipo <= 0) return;
                int idx = EsenciasJefesModMapa.IndiceDe(npc.type);
                if (idx < 0) return;

                var sys = ModContent.GetInstance<EsenciasModSistema>();
                if (sys == null) return;

                if (sys._dadas[idx] >= LimitePorMundo)
                {
                    // EL COFRE SECO: una sola vez por jefe, al mundo entero
                    // (el festín de las almas de ESTE jefe ya está pagado).
                    if (!sys._avisadoSeco[idx])
                    {
                        sys._avisadoSeco[idx] = true;
                        EcoRed.AnunciarMundo("Mods.AethonMod.Esencia.CofreSeco",
                            new Microsoft.Xna.Framework.Color(150, 150, 180), npc.FullName);
                    }
                    return;
                }

                sys._dadas[idx]++;
                Item.NewItem(npc.GetSource_Loot(), npc.Center, tipo, 1);

                // EL AVISO DE LA ÚLTIMA: cuando el cofre queda seco con
                // esta alma, el mundo lo sabe (diez páginas, cuento
                // cerrado — la undécima ya no cae).
                if (sys._dadas[idx] >= LimitePorMundo)
                    EcoRed.AnunciarMundo("Mods.AethonMod.Esencia.UltimaAlma",
                        new Microsoft.Xna.Framework.Color(196, 150, 255), npc.FullName);
            }
            catch { }
        }

        // === LA PERSISTENCIA (la cuenta vive con el mundo) ===

        public override void SaveWorldData(TagCompound tag)
        {
            tag["esenciasModDadas"] = _dadas;
            tag["esenciasModAvisadas"] = _avisadoSeco;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            _dadas = new int[5];
            _avisadoSeco = new bool[5];
            try
            {
                var dadas = tag.GetList<int>("esenciasModDadas");
                for (int i = 0; i < dadas.Count && i < 5; i++) _dadas[i] = dadas[i];
                var avisadas = tag.GetList<bool>("esenciasModAvisadas");
                for (int i = 0; i < avisadas.Count && i < 5; i++) _avisadoSeco[i] = avisadas[i];
            }
            catch { }
        }

        public override void OnWorldUnload()
        {
            _dadas = new int[5];
            _avisadoSeco = new bool[5];
        }
    }
}
