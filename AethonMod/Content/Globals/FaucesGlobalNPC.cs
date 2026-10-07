using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// FAUCESGLOBALNPC — v6.50.62 — LA MECÁNICA DE LA DEVORACIÓN.
    ///
    /// «Cuando el jefe llega a 1 punto de vida, su animación original de
    /// muerte se DETIENE y no avanza; en su lugar, la nueva animación: la
    /// sombra sale del libro y lo DEVORA.»
    ///
    /// LA RECETA (investigación 2-b, verificada contra Fargo Mutant, TSA
    /// Tsukiyomi, CalamityOverhaul y el propio AethonBoss de la casa —
    /// que usa EXACTAMENTE este patrón desde hace 60 versiones):
    ///
    ///   CheckDead (1ª pasada) → life=1 · dontTakeDamage · damage=0 ·
    ///   fase=DEVORADO · netUpdate · return false  → la muerte vanilla
    ///   NUNCA ARRANCA (ni gore, ni sonido, ni loot: NADA).
    ///   PreAI (congelado)     → return false: ni la IA ni la animación
    ///   del jefe corren — queda POSADO en 1 HP mientras lo comen.
    ///   Final del festín      → life=0 · fase=MUERTE_REAL ·
    ///   checkDead() (2ª llamada) → TODO el pipeline vanilla: loot,
    ///   bestiario, banners, flags downed*, OnKill de terceros. El jefe
    ///   muere DE VERDAD, con su botín completo.
    ///
    /// AethonBoss (el jefe del mod) NO pasa por aquí: su CheckDead de
    /// ModNPC corre ANTES que este GlobalNPC y devuelve false siempre —
    /// su cine de muerte propia es sagrado.
    ///
    /// MULTIPLAYER: la lógica autoritativa (drain, kill final) corre en
    /// server/SP (guard Main.netMode != MultiplayerClient); los campos
    /// viajan en SendExtraAI (se sincronizan con cada netUpdate) y los
    /// clientes congelan su simulación local en PreAI por su cuenta.
    /// </summary>
    public class FaucesGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        // === EL ESTADO (por NPC) ===
        /// <summary>0 = normal · 1 = DEVORADO (congelado en 1 HP) · 2 = muerte real ya ejecutada.</summary>
        public byte DevorarEstado;
        /// <summary>3 = sombra de la página (la base) · 11 = LA MANO DEL ESCRIBA · 12 = LAS TIJERAS DE LA PÁGINA · 13 = LA PÁGINA ARRANCADA (v6.50.71 — los útiles del escriba).</summary>
        public byte DevorarEstilo;
        /// <summary>El reloj del festín (el motor lo alimenta en server; el cliente lo cuenta local).</summary>
        public ushort DevorarTick;

        /// <summary>Duración total de la devoración (ticks) — 3.5 s de festín.</summary>
        public const int LargoFestin = 210;

        // === LAS MARCAS (golpe mortal de nuestras armas) ===
        // whoAmI del jefe → tick de caducidad. La marca vive 5 s: si otra
        // cosa lo mata después, la muerte es de esa otra cosa.
        private static readonly Dictionary<int, int> _marcas = new();
        /// <summary>whoAmI → estilo del arma que marcó.</summary>
        private static readonly Dictionary<int, byte> _marcasEstilo = new();

        // ==================================================================
        //  LA MARCA — la llamadan las armas cuando SU golpe es mortal
        // ==================================================================

        /// <summary>
        /// Marca un jefe para la devoración: su PRÓXIMA muerte (la que
        /// está pasando AHORA, vida ≤ 0) se intercepta. Los worms con
        /// pool compartido (realLife) marcan al DUEÑO del pool — el que
        /// de verdad corre checkDead.
        /// </summary>
        public static void Marcar(NPC npc, byte estilo)
        {
            if (npc == null || !npc.active || !EsJefe(npc)) return;
            NPC dueño = DueñoDelPool(npc);
            if (dueño == null || !dueño.active) return;

            if (Main.netMode == NetmodeID.MultiplayerClient) return; // la autoridad decide

            _marcas[dueño.whoAmI] = 300;              // caduca en 5 s
            _marcasEstilo[dueño.whoAmI] = estilo;
            dueño.netUpdate = true;
        }

        /// <summary>El jefe de verdad: con pool compartido (realLife), el dueño; si no, él mismo.</summary>
        public static NPC DueñoDelPool(NPC npc)
        {
            if (npc == null || !npc.active) return null;
            if (npc.realLife >= 0 && npc.realLife != npc.whoAmI && npc.realLife < Main.npc.Length)
            {
                NPC d = Main.npc[npc.realLife];
                return d != null && d.active ? d : null;
            }
            return npc;
        }

        /// <summary>¿Es un jefe devorable? (AethonBoss se excluye: su CheckDead propio corre antes).</summary>
        public static bool EsJefe(NPC npc)
        {
            if (npc == null || !npc.active) return false;
            if (npc.type == ModContent.NPCType<NPCs.AethonBoss>()) return false;
            return npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type];
        }

        // ==================================================================
        //  EL INICIO DEL FESTÍN — desde el drain del arma (vida→1 directo)
        // ==================================================================

        /// <summary>
        /// Arranca la devoración SIN pasar por checkDead (la llamadan los
        /// drains de las armas cuando la vida tocaría el suelo): clava la
        /// vida en 1, congela, suelta la marca y cede el mando al motor.
        /// </summary>
        public static void Iniciar(NPC npc, byte estilo)
        {
            if (npc == null || !npc.active) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            NPC dueño = DueñoDelPool(npc);
            if (dueño == null || !dueño.active) return;
            if (!dueño.TryGetGlobalNPC<FaucesGlobalNPC>(out var g)) return;
            if (g.DevorarEstado != 0) return;                     // ya se lo están comiendo

            _marcas.Remove(dueño.whoAmI);
            _marcasEstilo.Remove(dueño.whoAmI);

            g.DevorarEstado = 1;
            g.DevorarEstilo = estilo;
            g.DevorarTick = 0;
            g.FaseFestin = 0;

            dueño.life = 1;
            dueño.active = true;
            dueño.dontTakeDamage = true;
            dueño.damage = 0;
            dueño.netUpdate = true;

            // LA ANIMACIÓN: un proyectil-festín por cabeza (viaja solo por
            // la red de proyectiles — el cliente lo ve llegar solo).
            if (dueño.whoAmI >= 0 && Main.projectile.Length > 0)
            {
                int p = Projectile.NewProjectile(dueño.GetSource_FromAI(),
                    dueño.Center, Vector2.Zero, ModContent.ProjectileType<FauceDevoradorProjectile>(),
                    0, 0f, Main.myPlayer, dueño.whoAmI, estilo);
                if (p >= 0) Main.projectile[p].timeLeft = LargoFestin + 90;
            }
        }

        // ==================================================================
        //  LOS HOOKS
        // ==================================================================

        public override bool CheckDead(NPC npc)
        {
            // 2ª pasada (o muerte post-festín): la muerte REAL de vanilla.
            if (DevorarEstado == 2) return true;

            // ¿Marca viva de nuestras armas? (el golpe mortal fue NUESTRO)
            bool marcado = _marcas.TryGetValue(npc.whoAmI, out int caduca)
                && caduca > 0;
            if (!marcado) return true;                            // muerte ajena: normal

            // v6.50.66 — el estilo 1 (la pinza del gif) murió con su arma:
            // el festín por defecto es la sombra (3).
            byte estilo = _marcasEstilo.TryGetValue(npc.whoAmI, out byte e) ? e : (byte)3;
            _marcas.Remove(npc.whoAmI);
            _marcasEstilo.Remove(npc.whoAmI);

            // === LA INTERCEPCIÓN (1ª pasada) ===
            DevorarEstado = 1;
            DevorarEstilo = estilo;
            DevorarTick = 0;
            FaseFestin = 0;

            npc.life = 1;
            npc.active = true;
            npc.dontTakeDamage = true;
            npc.damage = 0;
            npc.netUpdate = true;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int p = Projectile.NewProjectile(npc.GetSource_Death(),
                    npc.Center, Vector2.Zero, ModContent.ProjectileType<FauceDevoradorProjectile>(),
                    0, 0f, Main.myPlayer, npc.whoAmI, estilo);
                if (p >= 0) Main.projectile[p].timeLeft = LargoFestin + 90;
            }
            return false;   // ← SU MUERTE ORIGINAL NUNCA ARRANCA
        }

        public override bool PreAI(NPC npc)
        {
            // congelar TAMBIÉN a los segmentos de un worm devorado
            bool devorado = DevorarEstado == 1;
            if (!devorado && npc.realLife >= 0 && npc.realLife != npc.whoAmI)
            {
                NPC dueño = Main.npc[npc.realLife];
                if (dueño != null && dueño.active &&
                    dueño.TryGetGlobalNPC<FaucesGlobalNPC>(out var gDueño) &&
                    gDueño.DevorarEstado == 1)
                    devorado = true;
            }
            if (!devorado) return true;

            // === EL CONGELADO: ni IA ni animación (la muerte detenida) ===
            // las primeras 12 t frena SUAVE (no un telonazo físico)
            if (DevorarTick < 12) npc.velocity *= 0.82f;
            else npc.velocity *= 0f;
            if (npc.lifeRegen < 0) npc.lifeRegen = 0;

            // v6.50.69 — LA FASE VISUAL DEL FESTÍN (0 manifestación ·
            // 1 envolver · 2 EL TRAGADO · 3 la disipación): la calcula el
            // motor (server/SP) y viaja por SendExtraAI — el PreDraw de
            // ABAJO la usa para retirar el sprite a su tiempo y el
            // proyectil-festín para sincronizar su animación en MP
            FaseFestin = DevorarTick < 30 ? (byte)0
                : DevorarTick < 60 ? (byte)1
                : DevorarTick < 160 ? (byte)2
                : (byte)3;

            // === EL MOTOR DEL FESTÍN ===
            if (DevorarTick < ushort.MaxValue - 2) DevorarTick++;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Los SONIDOS del festín (solo donde hay oídos)
                if (DevorarTick == 1)
                    Sonar(SoundID.Roar.WithPitchOffset(-0.45f).WithVolumeScale(0.9f), npc.Center);
                if (DevorarTick == 60 || DevorarTick == 110 || DevorarTick == 160)
                    Sonar(SoundID.Item122.WithPitchOffset(-0.30f).WithVolumeScale(0.55f), npc.Center);

                // redes: refresco periódico del estado (los clientes animan local)
                if (DevorarTick % 15 == 0) npc.netUpdate = true;

                // === EL FINAL: la muerte REAL (2ª llamada a checkDead) ===
                if (DevorarTick >= LargoFestin)
                {
                    DevorarEstado = 2;
                    npc.life = 0;
                    npc.dontTakeDamage = false;
                    npc.netUpdate = true;
                    npc.checkDead();   // → gore bajo la bruma + LOOT COMPLETO + bestiario + downed*
                }
                else if (DevorarTick > LargoFestin + 60)
                {
                    // RED DE SEGURIDAD: jamás un jefe zombi atascado
                    DevorarEstado = 2;
                    npc.life = 0;
                    npc.checkDead();
                }
            }
            return false;   // ← NI IA NI ANIMACIÓN: POSADO
        }

        /// <summary>CheckActive: false = NO despawnee durante el festín (bool en este tML).</summary>
        public override bool CheckActive(NPC npc)
        {
            if (DevorarEstado == 1) return false;
            return base.CheckActive(npc);
        }

        /// <summary>
        /// v6.50.69 — LA BRUMA ES LA ANIMACIÓN (la letra del usuario:
        /// «toda esa bruma que se traga al jefe debe animarse para que
        /// sustituya cualquier animación»): desde la fase 2 (la cobertura
        /// YA es total) el sprite del jefe SE RETIRA del mundo — lo que
        /// se ve es SOLO la masa de bruma tragándoselo. La fase viaja por
        /// la red (FaseFestin en SendExtraAI): todos los clientes retiran
        /// el sprite al mismo tick. Si el festín se interrumpe, el estado
        /// vuelve a 0 y el sprite REGRESA (nada queda invisible y vivo).
        /// </summary>
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (DevorarEstado == 1 && FaseFestin >= 2) return false;
            return true;
        }

        /// <summary>Los DoT no pueden matarlo durante el festín.</summary>
        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (DevorarEstado == 1) damage = 0;
        }

        // ==================================================================
        //  SINCRONIZACIÓN (multiplayer)
        // ==================================================================

        /// <summary>Fase visual del festín (para el render del proyectil-festín).</summary>
        public byte FaseFestin;

        public override void SendExtraAI(NPC npc, Terraria.ModLoader.IO.BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            // EL BIT: ¿este NPC lleva festín? (1 bit por paquete — barato
            // para TODOS los NPCs del mundo; los bytes solo viajan si toca)
            bitWriter.WriteBit(DevorarEstado != 0);
            if (DevorarEstado != 0)
            {
                binaryWriter.Write(DevorarEstado);
                binaryWriter.Write(DevorarEstilo);
                binaryWriter.Write(FaseFestin);
            }
        }

        public override void ReceiveExtraAI(NPC npc, Terraria.ModLoader.IO.BitReader bitReader, BinaryReader binaryReader)
        {
            if (!bitReader.ReadBit()) return;

            byte estado = binaryReader.ReadByte();
            DevorarEstilo = binaryReader.ReadByte();
            FaseFestin = binaryReader.ReadByte();

            if (estado == 1 && DevorarEstado == 0)
            {
                // el cliente aprende del festín: congela su simulación local
                DevorarEstado = 1;
                DevorarTick = 0;
            }
            else DevorarEstado = estado;
        }

        // ==================================================================
        //  LIMPIEZA (el dict estático no puede podrirse)
        // ==================================================================

        /// <summary>Podrido de las marcas (lo llama el ModSystem cada segundo).</summary>
        public static void PodarMarcas()
        {
            if (_marcas.Count == 0) return;
            List<int> muertas = null;
            foreach (KeyValuePair<int, int> kv in _marcas)
            {
                int restante = kv.Value - 60;   // 1 s por llamada
                if (restante <= 0) { muertas ??= new List<int>(); muertas.Add(kv.Key); }
                else _marcas[kv.Key] = restante;
            }
            if (muertas != null)
                for (int i = 0; i < muertas.Count; i++)
                {
                    _marcas.Remove(muertas[i]);
                    _marcasEstilo.Remove(muertas[i]);
                }
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
