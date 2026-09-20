using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// OleadaNPC — EL SELLO DE LAS OLEADAS DEL GRIMORIO. Todo monstruo o
    /// jefe que el libro hambriento CONVOCA lleva esta marca:
    ///
    /// - STATS ENFURECIDAS: vida, daño y defensa escalados por el número
    ///   de oleada (los jefes son VERSIONES ESPECIALES — su propio
    ///   multiplicador, más duro que el de la chusma).
    /// - AGRESIÓN REAL: re-objetivo constante y empuje hacia la presa
    ///   (los jefes y los gusanos se saltan el empuje — su AI ya manda).
    /// - EL AURA (AuraLib, la octava librería): capa trasera ANTES del
    ///   cuerpo (PreDraw) y el VELO frontal al 6% DESPUÉS (PostDraw) —
    ///   gris-blanca, y en la oleada 10 gris-negra con bordes rojo
    ///   oscuro. Las partículas corren por Actualizar (AI, no render).
    /// - LA XP DE LA OLEADA: GlobalNPCXP multiplica el cobro por
    ///   (oleada + 1) — la oleada 1 paga ×2 … la 10 paga ×11.
    ///
    /// PROPAGACIÓN: los segmentos que los jefes-gusano y sus sirvientes
    /// engendran a su lado HEREDAN el sello al nacer (OnSpawn) — el
    /// Devorador entero y los Creepers del Cerebro visten el aura y la
    /// furia de su convocador. (Consecuencia querida: la marca también
    /// salta a los spawns NATURALES que caigan cerca de una oleada —
    /// el hambre del libro es contagiosa.)
    ///
    /// LOTES (el contrato de la casa en PreDraw/PostDraw): AuraLib vuelca
    /// su lote aditivo cerrando el activo; aquí se REABRE el lote del
    /// sprite de vanilla justo después (Deferred/AlphaBlend/LinearClamp
    /// con la matriz del mundo — el estado del dibujado de NPCs).
    /// </summary>
    public class OleadaNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        /// <summary>¿Este NPC fue convocado por la furia del grimorio?</summary>
        public bool EsDeOleada = false;
        /// <summary>El número de oleada que lo convocó (1..10).</summary>
        public int Oleada = 0;
        /// <summary>¿Es la VERSIÓN ESPECIAL del jefe de la oleada?</summary>
        public bool EsJefeDeOleada = false;
        /// <summary>El perfil del aura que viste (null = sin aura).</summary>
        public AuraPerfil Aura = null;

        private int _tickAggro = 0;

        // ==================================================================
        //  EL SELLADO
        // ==================================================================

        /// <summary>
        /// Marca este NPC como criatura de la oleada k y aplica TODAS las
        /// consecuencias: stats enfurecidos, aura y knockback resistido.
        /// Llamado por GrimorioFuriaSistema JUSTO DESPUÉS de NPC.NewNPC
        /// (OnSpawn corre DENTRO de NewNPC — todavía no existía la marca).
        /// </summary>
        public void Marcar(NPC npc, int oleada, bool jefe)
        {
            try
            {
                EsDeOleada = true;
                Oleada = oleada < 1 ? 1 : (oleada > 10 ? 10 : oleada);
                EsJefeDeOleada = jefe;

                // === STATS: chusma y jefes escalan distinto ===
                // Chusma: vida ×(1+0.6(k-1)) → ×6.4 en la 10; daño ×(1+0.15(k-1)).
                // Jefes: vida ×(2.5+0.5(k-1)) → ×7 en la 10; daño ×(1.5+0.1(k-1)).
                float multVida = jefe ? 2.5f + 0.5f * (Oleada - 1) : 1f + 0.6f * (Oleada - 1);
                float multDanio = jefe ? 1.5f + 0.1f * (Oleada - 1) : 1f + 0.15f * (Oleada - 1);

                int nuevaVida = (int)(npc.lifeMax * multVida);
                if (nuevaVida < 1) nuevaVida = 1;
                npc.lifeMax = nuevaVida;
                npc.life = nuevaVida;
                if (npc.damage > 0)
                    npc.damage = (int)(npc.damage * multDanio);
                npc.defense += jefe ? 6 * Oleada : 2 * Oleada;
                npc.knockBackResist *= 0.35f; // la furia no se interrumpe

                // === EL AURA ===
                Aura = AuraPerfil.OleadaGrimorio(Oleada);
                Aura.Radio = RadioSegun(npc, jefe);

                npc.netUpdate = true; // MP: mejor esfuerzo de la casa (SP-first)
            }
            catch { EsDeOleada = false; }
        }

        /// <summary>El radio del aura según el tamaño del bicho (los jefes visten más grande).</summary>
        private static float RadioSegun(NPC npc, bool jefe)
        {
            float baseR = (npc.width + npc.height) * 0.45f + 26f;
            if (jefe) baseR *= 1.35f;
            return baseR;
        }

        /// <summary>
        /// LA PROPAGACIÓN: si nace un NPC hostil SIN sello a menos de 800px
        /// de una criatura de la oleada, hereda su furia (segmentos de
        /// gusano, Creepers del Cerebro, Sirvientes del Ojo… y los spawns
        /// naturales que caigan en medio del festín).
        /// </summary>
        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            try
            {
                if (EsDeOleada) return; // ya sellado por su convocador
                if (npc.friendly || npc.townNPC || npc.boss) return;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC otro = Main.npc[i];
                    if (otro == null || !otro.active || i == npc.whoAmI) continue;
                    var sello = otro.GetGlobalNPC<OleadaNPC>();
                    if (sello == null || !sello.EsDeOleada) continue;
                    if (Vector2.DistanceSquared(otro.Center, npc.Center) > 800f * 800f) continue;

                    // hereda la oleada del vecino (como chusma, no como jefe)
                    Marcar(npc, sello.Oleada, false);
                    break;
                }
            }
            catch { }
        }

        // ==================================================================
        //  LA AGRESIÓN
        // ==================================================================

        /// <summary>
        /// PreAI: la chusma de la oleada re-elige objetivo cada 30 ticks —
        /// no se distraen, van a por la comida del libro.
        /// </summary>
        public override bool PreAI(NPC npc)
        {
            if (EsDeOleada && !npc.boss)
            {
                _tickAggro++;
                if (_tickAggro >= 30)
                {
                    _tickAggro = 0;
                    npc.TargetClosest(false);
                }
            }
            return true;
        }

        /// <summary>
        /// PostAI (después de la AI de vanilla, que ya escribió su
        /// velocidad): EL EMPUJE de la furia hacia la presa — chusma solo,
        /// nunca jefes (su AI es sagrada) ni gusanos (aiStyle 6: la física
        /// de segmentos se rompe). Techo de velocidad: la furia no es
        /// teletransporte. Las PARTÍCULAS del aura corren aquí para todos
        /// (chusma Y jefes — en AI, no en render).
        /// </summary>
        public override void PostAI(NPC npc)
        {
            try
            {
                // las partículas del aura: SIEMPRE (también los jefes de
                // la oleada visten chispas)
                if (Aura != null)
                    AuraLib.Actualizar(npc, Aura);

                if (!EsDeOleada || npc.boss || npc.aiStyle == 6) return;

                Player t = Main.player[npc.target];
                if (t == null || !t.active || t.dead) return;

                Vector2 dir = t.Center - npc.Center;
                float d = dir.Length();
                if (d > 1500f || d < 1f) return;

                npc.velocity += dir / d * 0.16f;
                float vel = npc.velocity.Length();
                if (vel > 10f)
                    npc.velocity = npc.velocity * (10f / vel);
            }
            catch { }
        }

        // ==================================================================
        //  EL RENDER DEL AURA (PreDraw → trasera · PostDraw → velo)
        // ==================================================================

        /// <summary>
        /// La CAPA TRASERA del aura: se dibuja ANTES del sprite del NPC —
        /// el cuerpo TAPA el humo (la profundidad del look). AuraLib vuelca
        /// su lote aditivo cerrando el activo; se REABRE el lote del sprite
        /// de vanilla SOLO si se cerró de verdad (el bool del contrato).
        /// </summary>
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Aura == null) return true;
            try
            {
                if (AuraLib.DibujarNPC(npc, Aura, frontal: false))
                    AuraLib.ReabrirLoteVanilla();
            }
            catch
            {
                try { AuraLib.ReabrirLoteVanilla(); } catch { }
            }
            return true;
        }

        /// <summary>
        /// El VELO FRONTAL (la transparencia del 94%): la MISMA geometría
        /// pisando al cuerpo DESPUÉS del sprite — la criatura emite desde
        /// dentro. PostDraw es el último paso del dibujado de ESTE NPC: el
        /// lote se reabre para que el siguiente NPC dibuje normal.
        /// </summary>
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Aura == null) return;
            try
            {
                if (AuraLib.DibujarNPC(npc, Aura, frontal: true))
                    AuraLib.ReabrirLoteVanilla();
            }
            catch
            {
                try { AuraLib.ReabrirLoteVanilla(); } catch { }
            }
        }
    }
}
