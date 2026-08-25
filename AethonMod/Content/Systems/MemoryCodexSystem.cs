using AethonMod.Content.Players;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Sistema del Códex de Memoria — el capstone de Absorción de Lore.
    /// Permite al jugador "memorizar" el comportamiento de armas del juego base
    /// y equiparlas como Runas de Memoria.
    ///
    /// Cada rama tiene su propia lista de armas absorbibles:
    /// - Distance: 12 arcos + 16 armas de munición
    /// - Melee: 16 espadas/lanzas/yoyos
    /// - Magic: 16 armas mágicas + 12 de invocación
    /// </summary>
    public static class MemoryCodexSystem
    {
        /// <summary>
        /// Una entrada del códex: un arma del juego base que se puede memorizar.
        /// </summary>
        public class CodexEntry
        {
            public int ItemId;          // Terraria ItemID
            public string Name;
            public BranchType Branch;   // a qué rama pertenece
            public string Signature;    // comportamiento memorizado (descripción)
            public int ResonanceCost;   // Fragmentos de Resonancia para memorizar
        }

        /// <summary>
        /// Catálogo de armas absorbibles por rama.
        /// </summary>
        public static List<CodexEntry> GetCodexForBranch(BranchType branch)
        {
            var list = new List<CodexEntry>();
            switch (branch)
            {
                case BranchType.Distance:
                    // Arcos
                    list.Add(New(ItemID.WoodenBow, "Arco de Madera", "Disparo básico", 3));
                    list.Add(New(ItemID.DemonBow, "Arco Demoníaco", "+15% knockback, perfora 1", 8));
                    list.Add(New(ItemID.MoltenFury, "Furia Fundida", "Flechas se inflaman", 12));
                    list.Add(New(ItemID.BeesKnees, "Las Rodillas de la Abeja", "Abejas homing al impacto", 18));
                    list.Add(New(ItemID.HellwingBow, "Arco Ala Infernal", "Murciélagos flaming", 20));
                    list.Add(New(ItemID.DaedalusStormbow, "Arco Tormenta de Dédalo", "Flechas llueven del cielo", 30));
                    list.Add(New(ItemID.Tsunami, "Tsunami", "5 flechas en abanico", 40));
                    list.Add(New(ItemID.Phantasm, "Fantasma", "Flechas fantasma apilables", 45));
                    // Armas de munición
                    list.Add(New(ItemID.Minishark, "Minishark", "33% sin consumir munición", 12));
                    list.Add(New(ItemID.Megashark, "Megashark", "50% sin consumir, muy rápido", 28));
                    list.Add(New(ItemID.Shotgun, "Escopeta", "4 perdigones en abanico", 18));
                    list.Add(New(ItemID.SniperRifle, "Rifle Francotirador", "Zoom, crit masivo", 30));
                    list.Add(New(ItemID.ChainGun, "Ametralladora", "Cadencia extrema", 38));
                    list.Add(New(ItemID.SDMG, "Space Dolphin Machine Gun", "+15% daño, +5% crit", 45));
                    list.Add(New(ItemID.OnyxBlaster, "Blaster Ónix", "Virote ónix perforante + 2 perdigones", 26));
                    list.Add(New(ItemID.VortexBeater, "Vortex Beater", "Munición homing + cohete alt", 45));
                    break;

                case BranchType.Melee:
                    list.Add(New(ItemID.WoodenSword, "Espada de Madera", "+10% velocidad swing", 3));
                    list.Add(New(ItemID.BladeofGrass, "Hoja de Hierba", "Envenena 5s", 10));
                    list.Add(New(ItemID.Muramasa, "Muramasa", "Auto-swing +20% velocidad", 12));
                    list.Add(New(ItemID.FieryGreatsword, "Gran Espada Ardiente", "Incendia enemigos", 14));
                    list.Add(New(ItemID.BreakerBlade, "Hoja Rompedora", "+50% tamaño, +30% knockback", 20));
                    list.Add(New(ItemID.IceSickle, "Guadaña de Hielo", "Onda de escarcha perforante", 25));
                    list.Add(New(ItemID.TerraBlade, "Hoja Terra", "Rayo verde perforante", 35));
                    list.Add(New(ItemID.InfluxWaver, "Onda Influx", "Rayo se divide en 2 al impacto", 40));
                    list.Add(New(ItemID.BladedGlove, "Hoja del Jinete", "Calabazas homing flaming", 38));
                    list.Add(New(ItemID.Seedler, "Sembrador", "Hojas al impacto", 38));
                    list.Add(New(ItemID.StarWrath, "Ira Estelar", "Lluvia de estrellas al swing", 42));
                    list.Add(New(ItemID.Zenith, "Zenith", "Tormenta de todas las espadas", 50));
                    break;

                case BranchType.Magic:
                    // Magia
                    list.Add(New(ItemID.MagicDagger, "Daga Mágica", "Daga giratoria que retorna", 5));
                    list.Add(New(ItemID.DemonScythe, "Guadaña Demoníaca", "Gira perforando y retorna", 8));
                    list.Add(New(ItemID.AquaScepter, "Cetro de Agua", "Chorro continuo", 8));
                    list.Add(New(ItemID.FlowerofFire, "Flor de Fuego", "Bola de fuego rebotante", 10));
                    list.Add(New(ItemID.SpaceGun, "Pistola Espacial", "Láser perforante (bajo maná)", 10));
                    list.Add(New(ItemID.MagicMissile, "Misil Mágico", "Orbe guiado por cursor", 12));
                    list.Add(New(ItemID.BookofSkulls, "Libro de Calaveras", "Calaveras rebotantes", 18));
                    list.Add(New(ItemID.SkyFracture, "Fractura del Cielo", "3 esquirlas homing", 22));
                    list.Add(New(ItemID.MagnetSphere, "Esfera Magnética", "Orbe que dispara láseres", 24));
                    list.Add(New(ItemID.Razorpine, "Pino Navaja", "Ráfaga de hojas homing", 24));
                    list.Add(New(ItemID.RazorbladeTyphoon, "Tifón de Cuchillas", "Anillos homing rebotantes", 35));
                    list.Add(New(ItemID.LunarFlareBook, "Destello Lunar", "Lluvia lunar en cursor", 40));
                    list.Add(New(ItemID.LastPrism, "Último Prisma", "6 rayos convergentes", 45));
                    list.Add(New(ItemID.NebulaBlaze, "Destello Nebulosa", "Bolts alternos pequeños/grandes", 40));
                    list.Add(New(ItemID.BlizzardStaff, "Bastón de Ventura", "Lluvia de hielo", 35));
                    // Invocación
                    list.Add(New(ItemID.TempestStaff, "Bastón Tempestad", "Tiburones homing minions", 30));
                    list.Add(New(ItemID.StardustDragonStaff, "Bastón Dragón Stardust", "Dragón largo perforante", 40));
                    list.Add(New(ItemID.EmpressBlade, "Terraprisma", "Espadas aladas minion", 42));
                    break;
            }
            return list;
        }

        private static CodexEntry New(int itemId, string name, string signature, int cost)
        {
            return new CodexEntry
            {
                ItemId = itemId,
                Name = name,
                Branch = BranchType.None, // set by caller
                Signature = signature,
                ResonanceCost = cost,
            };
        }

        /// <summary>
        /// Memoriza un arma: consume Fragmentos de Resonancia y añade la runa al jugador.
        /// Devuelve true si tuvo éxito.
        /// </summary>
        public static bool Memorize(Player player, CodexEntry entry)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return false;
            if (sp.ResonanceShards < entry.ResonanceCost) return false;
            if (sp.MemorizedRunes.Count >= sp.RuneSlots()) return false;
            if (sp.MemorizedRunes.Contains(entry.Name)) return false;

            sp.ResonanceShards -= entry.ResonanceCost;
            sp.MemorizedRunes.Add(entry.Name);
            Main.NewText($"Memorizaste: {entry.Name} — {entry.Signature}", new Microsoft.Xna.Framework.Color(245, 196, 81));
            return true;
        }

        /// <summary>
        /// Olvida una runa (reembolso parcial de resonancia).
        /// </summary>
        public static void Forget(Player player, string runeName)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return;
            if (sp.MemorizedRunes.Remove(runeName))
            {
                sp.ResonanceShards += 5; // reembolso parcial
                Main.NewText($"Olvidaste: {runeName}", new Microsoft.Xna.Framework.Color(150, 150, 180));
            }
        }
    }
}
