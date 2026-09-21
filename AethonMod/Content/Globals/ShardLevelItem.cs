using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// GlobalItem — guarda el nivel y XP de CADA arma Aethon individualmente.
    ///
    /// Esto permite que cada arma tenga su propio progreso independiente:
    /// - El Grimorio del Eterno del jugador sube de nivel comiendo XP.
    /// - El Fragmento Génesis base NO sube (es material, no arma).
    /// - Si el jugador consigue otra copia del mismo item, esa copia
    ///   empieza en nivel 1 con su propio progreso.
    /// </summary>
    public class ShardLevelItem : GlobalItem
    {
        public override bool InstancePerEntity => true;

        /// <summary>Nivel actual del arma (1 = sin XP todavía).</summary>
        public int Level = 1;
        /// <summary>XP acumulada hacia el próximo nivel.</summary>
        public int XP = 0;

        /// <summary>
        /// v5.2: Flag que alterna la vista del tooltip del Grimorio.
        /// false = tooltip básico (solo nivel + XP + próximo hito)
        /// true  = tooltip completo (todas las estadísticas)
        /// Se alterna haciendo click derecho en el item dentro del inventario.
        /// Como la bolsa de vacio (Void Bag) que abre/cierra con click derecho.
        /// </summary>
        public bool ShowExtendedTooltip = false;

        /// <summary>
        /// v6.47: ¿ya se comió este libro su primera criatura 5★? La
        /// primera merece su línea en EcoLib ("Lo más raro que ha
        /// comido jamás") — una sola vez en la vida del libro.
        /// </summary>
        public bool PrimeraCincoEstrellas = false;

        /// <summary>
        /// Solo aplica a las 2 armas Aethon (Grimorio + Fragmento Génesis).
        /// DEFENSIVO: envuelto en try/catch porque se llama durante la carga del mod
        /// y ModContent.ItemType puede fallar si los items aún no están registrados.
        /// </summary>
        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            try
            {
                return item.type == ModContent.ItemType<Weapons.GrimoireEternal>() ||
                       item.type == ModContent.ItemType<Items.GenesisShard>();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// XP necesaria para subir al próximo nivel.
        /// v6.45: coste inicial 100 y sube desde ahí — 100 * nivel^1.5.
        /// Nivel 1→2: 100 XP. Nivel 10→11: ~3.162 XP. Nivel 20→21: ~8.944 XP.
        /// La XP ahora es REAL (rareza del bestiario, no kills): un jefe
        /// pre-hardmode (5.000+) paga ~un tercio del camino al nivel 20.
        /// </summary>
        public int XPForNextLevel()
        {
            return (int)(100 * System.Math.Pow(Level, 1.5));
        }

        /// <summary>
        /// Otorga XP a ESTE item específico. Si sube de nivel, dispara
        /// OnLevelUp UNA VEZ con el total de niveles ganados.
        /// DEFENSIVO: envuelto en try/catch para que un error NUNCA corrompa el kill.
        /// </summary>
        public void GrantXP(Item item, int amount)
        {
            try
            {
                // Solo sube de nivel si es un arma (no el FragmentoGenesis base).
                if (item.type == ModContent.ItemType<Items.GenesisShard>()) return;

                XP += amount;
                int nivelesGanados = 0;
                bool cruzoHito = false;
                int nivelHito = 0;
                while (XP >= XPForNextLevel())
                {
                    XP -= XPForNextLevel();
                    Level++;
                    nivelesGanados++;
                    if (Level % 50 == 0) { cruzoHito = true; nivelHito = Level; }
                }

                // v6.46: una sola derrota de jefe a nivel bajo salta 10–15
                // niveles de golpe — TODA la subida se anuncia en UN
                // mensaje condensado (el spam de 15 "alcanzó el nivel N"
                // seguidos era ruido, no información).
                if (nivelesGanados > 0)
                    OnLevelUp(item, nivelesGanados, cruzoHito, nivelHito);
            }
            catch
            {
                // Silenciar: nunca lanzar desde GrantXP.
            }
        }

        /// <summary>
        /// Se llama cuando ESTE item sube de nivel (v6.46: UNA vez por
        /// cobro de XP, con el total de niveles ganados).
        /// DEFENSIVO: envuelto en try/catch para que un error NUNCA corrompa el kill.
        /// v6.46: respeta las banderas de la config
        /// (ShowLevelUpNotifications / ShowMilestoneNotifications) — antes
        /// eran promesas muertas: existían y nadie las leía.
        /// v6.50: los FX viven SOLO donde hay pantalla (en MP el server
        /// sube SU copia y EcoRed.MsgLibro trae el delta al portador,
        /// que celebra en SU cliente — Main.LocalPlayer ya no se usa en
        /// contexto de servidor).
        /// </summary>
        private void OnLevelUp(Item item, int nivelesGanados, bool cruzoHito, int nivelHito)
        {
            try
            {
                if (Main.netMode == NetmodeID.Server) return; // sin pantalla

                var config = ModContent.GetInstance<Content.AethonConfig>();
                bool notificar = config == null || config.ShowLevelUpNotifications;
                bool notificarHitos = config == null || config.ShowMilestoneNotifications;

                // Efectos visuales en la posición del jugador (mantenidos por request del usuario):
                // - mensaje dorado con el nivel alcanzado (CONDENSADO si hubo salto múltiple)
                // - sonido corto
                // - partículas doradas en torno al jugador
                // (Los eventos cinematográficos — temblor de pantalla, grano, time-skip, lore —
                //  fueron eliminados por request del usuario. Ver commit de eliminación de
                //  LevelUpEventSystem.)
                Player owner = Main.LocalPlayer;
                if (owner != null && notificar && nivelesGanados > 0)
                {
                    string salto = nivelesGanados > 1 ? $" (+{nivelesGanados} niveles de golpe)" : "";
                    Main.NewText($"✦ {item.Name} alcanzó el nivel {Level}{salto}!",
                        new Microsoft.Xna.Framework.Color(245, 196, 81));
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item4);
                    for (int i = 0; i < 40; i++)
                        Dust.NewDustPerfect(owner.Center, Terraria.ID.DustID.GoldFlame,
                            new Microsoft.Xna.Framework.Vector2(Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-6, 6)),
                            100, new Microsoft.Xna.Framework.Color(245, 196, 81), 1.5f);
                }

                // Hito especial cada 50 niveles (infinito) — v6.46: el hito
                // también se anuncia si la subida múltiple LO CRUZÓ sin
                // aterrizar exactamente en él (nivelHito = el cruzado, no
                // el nivel final del salto).
                if (cruzoHito && notificarHitos && nivelHito > 0)
                {
                    Main.NewText($"✦✦ Hito nivel {nivelHito}! {item.Name} resuena con poder. ✦✦",
                        new Microsoft.Xna.Framework.Color(245, 196, 81));
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.DD2_EtherianPortalOpen);
                }
            }
            catch
            {
                // Silenciar: nunca lanzar desde OnLevelUp.
            }
        }

        // === Persistencia ===

        /// <summary>
        /// v6.50.1 — LA RED: el nivel/XP del libro viaja con el ITEM cuando
        /// vanilla sincroniza el inventario (al entrar sin SSC el cliente
        /// manda sus slots al server con ItemIO — que solo transporta datos
        /// de mod si el GlobalItem implementa esto). Sin él, la copia del
        /// SERVER nacía fresca (nivel 1) en cada sesión y EcoRed.MsgLibro
        /// pisoteaba la progresión real del cliente. Con esto la autoridad
        /// cuenta desde los datos verdaderos.
        /// </summary>
        public override void NetSend(Item item, BinaryWriter writer)
        {
            try
            {
                writer.Write(Level);
                writer.Write(XP);
                writer.Write(PrimeraCincoEstrellas);
            }
            catch { }
        }

        public override void NetReceive(Item item, BinaryReader reader)
        {
            try
            {
                Level = System.Math.Max(1, reader.ReadInt32());
                XP = reader.ReadInt32();
                PrimeraCincoEstrellas = reader.ReadBoolean();
            }
            catch
            {
                Level = 1;
                XP = 0;
                PrimeraCincoEstrellas = false;
            }
        }

        /// <summary>
        /// v6.50 — LA CELEBRACIÓN DEL DELTA: cuando EcoRed.MsgLibro trae el
        /// nivel nuevo del servidor, el cliente aplica la copia local y
        /// celebra SOLO lo subido (mismo mensaje condensado, mismo sonido,
        /// mismas partículas doradas de la subida local — una sola fiesta
        /// aunque el server y el cliente cuenten por separado).
        /// </summary>
        public void CelebrarSubida(Item item, int nivelesGanados)
        {
            try
            {
                if (nivelesGanados <= 0) return;
                bool cruzoHito = false;
                int nivelHito = 0;
                for (int l = Level - nivelesGanados + 1; l <= Level; l++)
                    if (l % 50 == 0) { cruzoHito = true; nivelHito = l; }
                OnLevelUp(item, nivelesGanados, cruzoHito, nivelHito);
            }
            catch { }
        }

        /// <summary>
        /// v6.48 — LA ESENCIA DEL GUARDIÁN: sube UN nivel COMPLETO al libro
        /// (la vía directa de las esencias de los jefes de oleada — no
        /// pasa por la XP: el alma ES el nivel). Anuncia como una subida
        /// normal (condensada, con hitos y config respetadas).
        /// </summary>
        public void SubirNivelDirecto(Item item, int niveles = 1)
        {
            try
            {
                if (niveles <= 0) return;
                if (item.type == ModContent.ItemType<Items.GenesisShard>()) return;
                Level += niveles;
                bool cruzoHito = false;
                int nivelHito = 0;
                int limite = Level;
                for (int l = Level - niveles + 1; l <= limite; l++)
                {
                    if (l % 50 == 0) { cruzoHito = true; nivelHito = l; }
                }
                OnLevelUp(item, niveles, cruzoHito, nivelHito);
            }
            catch { }
        }

        public override void SaveData(Item item, TagCompound tag)
        {
            if (Level > 1 || XP > 0)
            {
                tag["aethonLevel"] = Level;
                tag["aethonXP"] = XP;
            }
            if (PrimeraCincoEstrellas)
                tag["aethonPrimera5"] = true;
        }

        public override void LoadData(Item item, TagCompound tag)
        {
            try
            {
                Level = tag.GetInt("aethonLevel");
                if (Level < 1) Level = 1;
                XP = tag.GetInt("aethonXP");
                PrimeraCincoEstrellas = tag.GetBool("aethonPrimera5");
            }
            catch
            {
                Level = 1;
                XP = 0;
                PrimeraCincoEstrellas = false;
            }
        }
    }
}
