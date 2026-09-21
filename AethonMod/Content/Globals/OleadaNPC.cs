using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.DataStructures;
using AethonMod.Content.VFX;
using AethonMod.Content.Items.Esencias;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// OleadaNPC — EL SELLO DE LAS OLEADAS DEL GRIMORIO. Todo monstruo o
    /// jefe que el libro hambriento CONVOCA lleva esta marca:
    ///
    /// - STATS ENFURECIDAS (v6.48, LA LETRA DEL USUARIO): la oleada k
    ///   multiplica la vida Y el ataque por ×(k+1) — la 1 golpea ×2 y la
    ///   10 ×11, monstruos Y jefes; LA OLEADA ESPECIAL (11) los viste a
    ///   ×15. La defensa sigue escalando aparte (chusma +2k, jefes +6k).
    /// - AGRESIÓN REAL (v6.48, CRECIENTE): la chusma re-objetiva y
    ///   empuja hacia la presa MÁS FUERTE con cada oleada (empuje
    ///   0.16+0.02k, techo 10+0.5k); los JEFES ya no son sagrados:
    ///   re-objetivo cada 20 ticks, los que surcan tiles HOMING hacia la
    ///   presa y los embistes (el lunge de la furia), y TODOS disparan
    ///   sus DIENTES (AtaqueOleadaProjectile — los ataques nuevos de
    ///   librería) con cadencia que crece con la oleada.
    /// - EL AURA (AuraLib, la octava librería): capa trasera ANTES del
    ///   cuerpo (PreDraw) y el VELO frontal al 6% DESPUÉS (PostDraw) —
    ///   gris-blanca, en la 10 gris-negra con bordes rojo oscuro y en la
    ///   ESPECIAL el JUICIO (negra, rojo intenso, chispas carmesí).
    ///   Las partículas corren por Actualizar (AI, no render).
    /// - LA XP DE LA OLEADA: GlobalNPCXP multiplica el cobro por
    ///   MultiplicadorXP() — oleada k paga ×(k+1), la ESPECIAL ×15.
    /// - EL PAGO EN METALES (v6.48): cada monstruo muerto suelta k
    ///   MONEDAS DE ORO y cada jefe de oleada k MONEDAS DE PLATINO (la
    ///   especial: 15) — la furia del libro paga lo que come. Los jefes
    ///   además dejan caer SU ESENCIA (EsenciaDeJefeItem — un nivel
    ///   completo para el libro).
    ///
    /// PROPAGACIÓN: los segmentos que los jefes-gusano y sus sirvientes
    /// engendran a su lado HEREDAN el sello al nacer (OnSpawn) — el
    /// Devorador entero y los Creepers del Cerebro visten el aura y la
    /// furia de su convocador. (Consecuencia querida y documentada: la
    /// marca también salta a los spawns NATURALES que caigan cerca de
    /// una oleada — el hambre del libro es contagiosa, y paga más XP Y
    /// más peligro: el sello es el sello.)
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
        /// <summary>El número de oleada que lo convocó (1..11).</summary>
        public int Oleada = 0;
        /// <summary>¿Es la VERSIÓN ESPECIAL del jefe de la oleada?</summary>
        public bool EsJefeDeOleada = false;
        /// <summary>
        /// v6.48 — ¿Es de LA OLEADA ESPECIAL (11 — El Juicio)? Todos los
        /// jefes juntos a ×15: stats, XP y aura del JUICIO.
        /// </summary>
        public bool EsEspecial = false;
        /// <summary>El perfil del aura que viste (null = sin aura).</summary>
        public AuraPerfil Aura = null;

        private int _tickAggro = 0;
        private int _tickAtaque = 0;   // el tempo de los dientes
        private int _tickLunge = 0;    // el tempo de los embites

        // ==================================================================
        //  EL SELLADO
        // ==================================================================

        /// <summary>
        /// Marca este NPC como criatura de la oleada k (especial=true →
        /// la 11, EL JUICIO) y aplica TODAS las consecuencias: stats
        /// enfurecidos, aura y knockback resistido. Llamado por
        /// GrimorioFuriaSistema JUSTO DESPUÉS de NPC.NewNPC (OnSpawn
        /// corre DENTRO de NewNPC — todavía no existía la marca).
        /// </summary>
        public void Marcar(NPC npc, int oleada, bool jefe, bool especial = false)
        {
            try
            {
                EsDeOleada = true;
                EsEspecial = especial;
                Oleada = especial ? 11 : (oleada < 1 ? 1 : (oleada > 10 ? 10 : oleada));
                EsJefeDeOleada = jefe;

                // === STATS: LA LETRA DEL USUARIO (v6.48) ===
                // La oleada k: vida Y daño ×(k+1) — la 1 ×2, la 10 ×11,
                // para chusma Y jefes. LA ESPECIAL: ×15.
                float mult = MultiplicadorStats;

                int nuevaVida = (int)(npc.lifeMax * mult);
                if (nuevaVida < 1) nuevaVida = 1;
                npc.lifeMax = nuevaVida;
                npc.life = nuevaVida;
                if (npc.damage > 0)
                    npc.damage = (int)(npc.damage * mult);
                npc.defense += jefe ? 6 * Oleada : 2 * Oleada;
                npc.knockBackResist *= 0.35f; // la furia no se interrumpe

                // === EL AURA ===
                Aura = AuraPerfil.OleadaGrimorio(Oleada);
                Aura.Radio = RadioSegun(npc, jefe);

                npc.netUpdate = true; // MP: mejor esfuerzo de la casa (SP-first)
            }
            catch { EsDeOleada = false; }
        }

        /// <summary>
        /// EL MULTIPLICADOR DE STATS: ×(oleada+1) — la 1 ×2 … la 10 ×11;
        /// la OLEADA ESPECIAL ×15 (la letra del usuario).
        /// </summary>
        public float MultiplicadorStats => EsEspecial ? 15f : Oleada + 1f;

        /// <summary>
        /// EL MULTIPLICADOR DE XP (lo lee GlobalNPCXP): ×(oleada+1) — la
        /// 1 paga ×2 … la 10 ×11; la ESPECIAL ×15.
        /// </summary>
        public int MultiplicadorXP => EsEspecial ? 15 : Oleada + 1;

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
                    Marcar(npc, sello.Oleada, false, sello.EsEspecial);
                    break;
                }
            }
            catch { }
        }

        // ==================================================================
        //  EL VIAJE DEL SELLO POR LA RED (v6.50.1 — flags con el NPC)
        // ==================================================================

        /// <summary>
        /// v6.50.1 — FIX: los flags de oleada viajan con el NPC (el aura del
        /// JUICIO se dibuja en los clientes de MP — antes solo el server los
        /// tenía: los GlobalNPC de instancia NO viajan solos). Corre cuando
        /// el NPC se sincroniza (MessageID.SyncNPC: netUpdate, creación y
        /// jugadores que entran a media oleada). ESCRITURA SIMÉTRICA
        /// EXACTA con ReceiveExtraAI (mismo orden y tipos: 3 bits + 1 byte).
        /// </summary>
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
        {
            bitWriter.WriteBit(EsDeOleada);
            bitWriter.WriteBit(EsEspecial);
            bitWriter.WriteBit(EsJefeDeOleada);
            writer.Write((byte)Oleada);
        }

        /// <summary>
        /// v6.50.1 — FIX (el simétrico de SendExtraAI): el cliente asigna los
        /// flags de instancia con los datos leídos Y reconstruye el aura —
        /// PreDraw/PostDraw leen Aura en el cliente y sin reconstruirla el
        /// sello llegaba pero el JUICIO seguía invisible.
        /// </summary>
        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
        {
            EsDeOleada = bitReader.ReadBit();
            EsEspecial = bitReader.ReadBit();
            EsJefeDeOleada = bitReader.ReadBit();
            Oleada = reader.ReadByte();

            if (EsDeOleada && Aura == null)
            {
                Aura = AuraPerfil.OleadaGrimorio(Oleada);
                Aura.Radio = RadioSegun(npc, EsJefeDeOleada);
            }
        }

        // ==================================================================
        //  LA AGRESIÓN (chusma Y jefes — la furia crece con la oleada)
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
        /// PostAI (después de la AI de vanilla): LA AGRESIÓN v6.48 —
        ///
        /// · La CHUSMA: empuje hacia la presa más fuerte con cada oleada
        ///   (0.16+0.02k, techo 10+0.5k). Gusanos (aiStyle 6) exentos
        ///   (la física de segmentos se rompe).
        /// · Los JEFES (ya no sagrados): re-objetivo cada 20 ticks; los
        ///   que surcan tiles HOMING suave (la furia los pega a la
        ///   presa) + EL EMBITE (lunge periódico que crece con la
        ///   oleada); y TODOS escupen SUS DIENTES — los ataques nuevos
        ///   de librería (AtaqueOleadaProjectile) con cadencia
        ///   300−18k (tope 60; especial 50).
        ///
        /// Las PARTÍCULAS del aura corren aquí para todos (chusma Y
        /// jefes — en AI, no en render).
        /// </summary>
        public override void PostAI(NPC npc)
        {
            try
            {
                // las partículas del aura: SIEMPRE (también los jefes de
                // la oleada visten chispas)
                if (Aura != null)
                    AuraLib.Actualizar(npc, Aura);

                if (!EsDeOleada) return;

                Player presa = Main.player[npc.target];
                bool presaValida = presa != null && presa.active && !presa.dead;

                // === LA CHUSMA: el empuje creciente ===
                if (!npc.boss)
                {
                    if (npc.aiStyle == 6) return; // gusanos: física sagrada
                    if (!presaValida) return;
                    Vector2 dir = presa.Center - npc.Center;
                    float d = dir.Length();
                    if (d > 1500f || d < 1f) return;
                    npc.velocity += dir / d * (0.16f + 0.02f * Oleada);
                    float techo = 10f + 0.5f * Oleada;
                    float vel = npc.velocity.Length();
                    if (vel > techo)
                        npc.velocity = npc.velocity * (techo / vel);
                    return;
                }

                // === LOS JEFES: la furia de verdad ===
                if (!EsJefeDeOleada) return; // solo los convocados por el libro

                // EL RE-OBJETIVO (cada 20 ticks — nunca se distraen).
                _tickAggro++;
                if (_tickAggro >= 20)
                {
                    _tickAggro = 0;
                    npc.TargetClosest(false);
                    presa = Main.player[npc.target];
                    presaValida = presa != null && presa.active && !presa.dead;
                }

                // EL HOMING + EL EMBITE: solo los que surcan tiles (los
                // demás ya persiguen por su cuenta — su AI usa el suelo).
                if (presaValida && npc.noTileCollide)
                {
                    Vector2 dir = presa.Center - npc.Center;
                    float d = dir.Length();
                    if (d > 60f && d < 2200f)
                        npc.velocity += dir / d * (0.05f + 0.008f * Oleada);

                    _tickLunge++;
                    int cadenciaLunge = EsEspecial ? 90 : Math.Max(120, 300 - 18 * Oleada);
                    if (_tickLunge >= cadenciaLunge)
                    {
                        _tickLunge = 0;
                        Vector2 embite = (presa.Center - npc.Center).SafeNormalize(Vector2.Zero);
                        npc.velocity += embite * (2f + 0.35f * Oleada);
                    }
                }

                // LOS DIENTES: los ataques nuevos de librería, con cadencia
                // que crece con la oleada (la 10 y la especial, sin pausa).
                if (presaValida)
                {
                    _tickAtaque++;
                    int cadencia = EsEspecial ? 50 : Math.Max(60, 300 - 18 * Oleada);
                    if (_tickAtaque >= cadencia)
                    {
                        _tickAtaque = 0;
                        EscupirDientes(npc, presa);
                    }
                }
            }
            catch { }
        }

        // ==================================================================
        //  LOS DIENTES — el ataque nuevo de cada guardián (librerías)
        // ==================================================================

        /// <summary>El estilo de diente que corresponde a cada guardián.</summary>
        private static int EstiloDe(NPC npc)
        {
            if (npc.type == NPCID.KingSlime) return Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloReyGelatina;
            if (npc.type == NPCID.EyeofCthulhu) return Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloOjo;
            if (npc.type == NPCID.Deerclops) return Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloDeerclops;
            if (npc.type == NPCID.QueenBee) return Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloAbeja;
            if (npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody ||
                npc.type == NPCID.EaterofWorldsTail)
                return Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloDevorador;
            if (npc.type == NPCID.BrainofCthulhu) return Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloCerebro;
            if (npc.type == NPCID.SkeletronHead)
                return Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloSkeletron;
            // guardián sin diente propio (no debería pasar): el estallido
            return Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloCerebro;
        }

        /// <summary>
        /// Escupe UN RACIÓN de dientes del estilo del guardián (los patrones
        /// completos de AtaqueOleadaProjectile — cada grupo es el "ataque").
        /// </summary>
        private void EscupirDientes(NPC npc, Player presa)
        {
            try
            {
                int estilo = EstiloDe(npc);
                int danio = (int)(16f * MultiplicadorStats);
                var src = npc.GetSource_FromAI();

                switch (estilo)
                {
                    case Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloReyGelatina:
                    {
                        // EL SELLO DEL TRONO: ocho cuentas de la corona.
                        for (int i = 0; i < 8; i++)
                        {
                            float ang = i * MathHelper.TwoPi / 8f;
                            Vector2 pos = npc.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 70f;
                            // v6.50.1 — FIX (MP ×N+1): la IA del NPC corre en
                            // server Y clientes — sin gate cada máquina escupía
                            // su ración de dientes y NewProjectile la
                            // auto-difundía. Solo la autoridad escupe.
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(src, pos, Vector2.Zero,
                                    ModContent.ProjectileType<Projectiles.Oleadas.AtaqueOleadaProjectile>(),
                                    danio, 2f, Main.myPlayer, estilo, ang, i * 31 + Oleada);
                            }
                        }
                        break;
                    }

                    case Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloOjo:
                    {
                        // LOS TAJOS DEL VIGÍA: dos marcas sobre la presa.
                        for (int i = 0; i < 2; i++)
                        {
                            Vector2 pos = presa.Center + (i == 0 ? Vector2.Zero : new Vector2(90f, -40f));
                            float dir = Main.rand.NextFloat(MathHelper.TwoPi);
                            // v6.50.1 — FIX (MP ×N+1): solo la autoridad escupe.
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(src, pos, Vector2.Zero,
                                    ModContent.ProjectileType<Projectiles.Oleadas.AtaqueOleadaProjectile>(),
                                    danio, 2f, Main.myPlayer, estilo, dir, Oleada * 7 + i);
                            }
                        }
                        break;
                    }

                    case Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloDeerclops:
                    {
                        // LOS LÁTIGOS DE ESCARCHA: tres espinas del cielo.
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 pos = presa.Center + new Vector2(
                                (i - 1) * 130f + Main.rand.NextFloat(-40f, 40f), -420f);
                            // v6.50.1 — FIX (MP ×N+1): solo la autoridad escupe.
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(src, pos, new Vector2(0f, 4f),
                                    ModContent.ProjectileType<Projectiles.Oleadas.AtaqueOleadaProjectile>(),
                                    danio, 2f, Main.myPlayer, estilo, 0f, Oleada * 11 + i);
                            }
                        }
                        break;
                    }

                    case Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloAbeja:
                    {
                        // EL ABANICO DE AGUIJONES: cinco con corrección.
                        Vector2 baseDir = (presa.Center - npc.Center).SafeNormalize(Vector2.UnitY);
                        for (int i = -2; i <= 2; i++)
                        {
                            Vector2 vel = baseDir.RotatedBy(i * 0.16f) * 11f;
                            // v6.50.1 — FIX (MP ×N+1): solo la autoridad escupe.
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(src, npc.Center, vel,
                                    ModContent.ProjectileType<Projectiles.Oleadas.AtaqueOleadaProjectile>(),
                                    danio, 2f, Main.myPlayer, estilo, 0f, Oleada * 13 + i);
                            }
                        }
                        break;
                    }

                    case Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloDevorador:
                    {
                        // LAS FAUCES CORRUPTAS: tres bocas que curvan.
                        Vector2 baseDir = (presa.Center - npc.Center).SafeNormalize(Vector2.UnitX);
                        for (int i = -1; i <= 1; i++)
                        {
                            Vector2 vel = baseDir.RotatedBy(i * 0.35f) * 8f;
                            // v6.50.1 — FIX (MP ×N+1): solo la autoridad escupe.
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(src, npc.Center, vel,
                                    ModContent.ProjectileType<Projectiles.Oleadas.AtaqueOleadaProjectile>(),
                                    danio, 2f, Main.myPlayer, estilo, 0f, Oleada * 17 + i);
                            }
                        }
                        break;
                    }

                    case Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloCerebro:
                    {
                        // EL ESTALLIDO CARMESÍ: ocho reflejos radiales.
                        for (int i = 0; i < 8; i++)
                        {
                            float ang = i * MathHelper.TwoPi / 8f;
                            Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 9f;
                            // v6.50.1 — FIX (MP ×N+1): solo la autoridad escupe.
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(src, npc.Center, vel,
                                    ModContent.ProjectileType<Projectiles.Oleadas.AtaqueOleadaProjectile>(),
                                    danio, 2f, Main.myPlayer, estilo, 0f, Oleada * 19 + i);
                            }
                        }
                        break;
                    }

                    case Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloSkeletron:
                    {
                        // LAS CALAVERAS EN ÓRBITA: tres cráneos que se lanzan.
                        for (int i = 0; i < 3; i++)
                        {
                            float ang = i * MathHelper.TwoPi / 3f;
                            Vector2 pos = npc.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 92f;
                            // v6.50.1 — FIX (MP ×N+1): solo la autoridad escupe.
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(src, pos, Vector2.Zero,
                                    ModContent.ProjectileType<Projectiles.Oleadas.AtaqueOleadaProjectile>(),
                                    danio, 2f, Main.myPlayer, estilo, ang, Oleada * 23 + i);
                            }
                        }
                        break;
                    }
                }

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, npc.Center);
            }
            catch { }
        }

        // ==================================================================
        //  LA MUERTE PAGA — monedas y esencias
        // ==================================================================

        /// <summary>
        /// v6.48 — EL PAGO EN METALES Y ESENCIAS: cada monstruo de la
        /// oleada k suelta k MONEDAS DE ORO; cada JEFE de la oleada k
        /// MONEDAS DE PLATINO + SU ESENCIA (la especial: 15). El festín
        /// del libro paga en los dos metales.
        /// </summary>
        public override void OnKill(NPC npc)
        {
            if (!EsDeOleada) return;
            try
            {
                int monedas = EsEspecial ? 15 : Oleada;
                var src = npc.GetSource_Loot();

                if (EsJefeDeOleada)
                {
                    // EL PLATINO DEL GUARDIÁN (k monedas — la 10: 10).
                    if (monedas > 0)
                        Item.NewItem(src, npc.Center, ItemID.PlatinumCoin, monedas);

                    // SU ESENCIA (un alma por guardián — el ítem que sube
                    // un nivel COMPLETO al libro).
                    int esencia = EsenciaDeJefeItem.DeNPC(npc.type);
                    if (esencia > 0)
                        Item.NewItem(src, npc.Center, esencia, 1);
                }
                else
                {
                    // EL ORO DE LA CHUSMA (k monedas — la 10: 10).
                    if (monedas > 0)
                        Item.NewItem(src, npc.Center, ItemID.GoldCoin, monedas);
                }
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
