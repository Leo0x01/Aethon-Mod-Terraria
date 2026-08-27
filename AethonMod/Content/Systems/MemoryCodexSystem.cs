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
        /// Catalogo de armas absorbibles por rama.
        /// Usa MagicWeaponScanner para encontrar TODAS las armas del juego por DamageType
        /// (no hardcodeado — incluye vanilla + mods automaticamente).
        /// </summary>
        public static List<CodexEntry> GetCodexForBranch(BranchType branch)
        {
            var list = new List<CodexEntry>();
            var weapons = branch switch
            {
                BranchType.Distance => MagicWeaponScanner.GetRangedWeapons(),
                BranchType.Melee => MagicWeaponScanner.GetMeleeWeapons(),
                BranchType.Magic => CombineMagicAndSummon(),
                _ => new List<MagicWeaponScanner.WeaponInfo>(),
            };

            foreach (var w in weapons)
            {
                list.Add(new CodexEntry
                {
                    ItemId = w.ItemId,
                    Name = w.Name,
                    Branch = branch,
                    Signature = w.Description,
                    ResonanceCost = w.ResonanceCost,
                });
            }
            return list;
        }

        /// <summary>Combina armas mágicas + de invocación para la rama Magic (sin límite).</summary>
        private static List<MagicWeaponScanner.WeaponInfo> CombineMagicAndSummon()
        {
            var result = new List<MagicWeaponScanner.WeaponInfo>();
            result.AddRange(MagicWeaponScanner.GetMagicWeapons());
            result.AddRange(MagicWeaponScanner.GetSummonWeapons());
            // Ordenar por costo (sin límite — mostrar todas)
            result.Sort((a, b) => a.ResonanceCost.CompareTo(b.ResonanceCost));
            return result;
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
