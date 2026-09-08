using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// GlobalItem — guarda el nivel y XP de CADA arma Aethon individualmente.
    ///
    /// Esto permite que cada arma tenga su propio progreso independiente:
    /// - El SolbrandEdge que el jugador elige sube de nivel con kills.
    /// - Las otras 2 armas (Lumina/Grimorio) NO suben hasta que sean elegidas.
    /// - Si el jugador consigue otra copia del mismo item, esa copia empieza en nivel 1.
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
        /// Solo aplica a las 3 armas Aethon + el FragmentoGenesis.
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
        /// Nivel 1→2: 1 XP (cualquier kill).
        /// Nivel 2+: 80 * nivel^1.5.
        /// </summary>
        public int XPForNextLevel()
        {
            return (int)(80 * System.Math.Pow(Level, 1.5));
        }

        /// <summary>
        /// Otorga XP a ESTE item específico. Si sube de nivel, dispara OnLevelUp.
        /// DEFENSIVO: envuelto en try/catch para que un error NUNCA corrompa el kill.
        /// </summary>
        public void GrantXP(Item item, int amount)
        {
            try
            {
                // Solo sube de nivel si es un arma (no el FragmentoGenesis base).
                if (item.type == ModContent.ItemType<Items.GenesisShard>()) return;

                XP += amount;
                while (XP >= XPForNextLevel())
                {
                    XP -= XPForNextLevel();
                    Level++;
                    OnLevelUp(item);
                }
            }
            catch
            {
                // Silenciar: nunca lanzar desde GrantXP.
            }
        }

        /// <summary>
        /// Se llama cuando ESTE item sube de nivel.
        /// DEFENSIVO: envuelto en try/catch para que un error NUNCA corrompa el kill.
        /// </summary>
        private void OnLevelUp(Item item)
        {
            try
            {
                // Efectos visuales en la posición del jugador (mantenidos por request del usuario):
                // - mensaje dorado con el nivel alcanzado
                // - sonido corto
                // - partículas doradas en torno al jugador
                // (Los eventos cinematográficos — temblor de pantalla, grano, time-skip, lore —
                //  fueron eliminados por request del usuario. Ver commit de eliminación de
                //  LevelUpEventSystem.)
                Player? owner = Main.LocalPlayer;
                if (owner != null)
                {
                    Main.NewText($"✦ {item.Name} alcanzó el nivel {Level}!",
                        new Microsoft.Xna.Framework.Color(245, 196, 81));
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item4);
                    for (int i = 0; i < 40; i++)
                        Dust.NewDustPerfect(owner.Center, Terraria.ID.DustID.GoldFlame,
                            new Microsoft.Xna.Framework.Vector2(Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-6, 6)),
                            100, new Microsoft.Xna.Framework.Color(245, 196, 81), 1.5f);
                }

                // Hito especial cada 50 niveles (infinito)
                if (Level % 50 == 0)
                {
                    Main.NewText($"✦✦ Hito nivel {Level}! {item.Name} resuena con poder. ✦✦",
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

        public override void SaveData(Item item, TagCompound tag)
        {
            if (Level > 1 || XP > 0)
            {
                tag["aethonLevel"] = Level;
                tag["aethonXP"] = XP;
            }
        }

        public override void LoadData(Item item, TagCompound tag)
        {
            try
            {
                Level = tag.GetInt("aethonLevel");
                if (Level < 1) Level = 1;
                XP = tag.GetInt("aethonXP");
            }
            catch
            {
                Level = 1;
                XP = 0;
            }
        }
    }
}
