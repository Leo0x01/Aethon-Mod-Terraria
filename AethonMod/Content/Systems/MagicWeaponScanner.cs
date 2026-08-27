using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Escanea TODAS las armas del juego (vanilla + mods) por DamageType.
    /// En vez de hardcodear ItemIDs, iteramos ItemID y verificamos:
    ///   - item.DamageType == DamageClass.Magic (armas mágicas)
    ///   - item.DamageType == DamageClass.Summon (armas de invocación)
    ///   - item.damage > 0 (es un arma real, no un objeto decorativo)
    /// 
    /// El resultado se cachea al cargar el mod (solo se calcula una vez).
    /// </summary>
    public static class MagicWeaponScanner
    {
        public class WeaponInfo
        {
            public int ItemId;
            public string Name;
            public string Description;  // descripción breve del comportamiento
            public int ResonanceCost;    // costo en fragmentos de resonancia
            public bool IsSummon;        // true si es arma de invocación
        }

        private static List<WeaponInfo>? _magicWeapons;
        private static List<WeaponInfo>? _summonWeapons;
        private static List<WeaponInfo>? _rangedWeapons;
        private static List<WeaponInfo>? _meleeWeapons;

        /// <summary>Lista de armas mágicas (DamageClass.Magic) cacheada.</summary>
        public static List<WeaponInfo> GetMagicWeapons()
        {
            if (_magicWeapons != null) return _magicWeapons;
            _magicWeapons = ScanWeapons(false, true, false);
            return _magicWeapons;
        }

        /// <summary>Lista de armas de invocación (DamageClass.Summon) cacheada.</summary>
        public static List<WeaponInfo> GetSummonWeapons()
        {
            if (_summonWeapons != null) return _summonWeapons;
            _summonWeapons = ScanWeapons(false, false, true);
            return _summonWeapons;
        }

        /// <summary>Lista de armas a distancia (DamageClass.Ranged) cacheada.</summary>
        public static List<WeaponInfo> GetRangedWeapons()
        {
            if (_rangedWeapons != null) return _rangedWeapons;
            _rangedWeapons = ScanWeapons(true, false, false);
            return _rangedWeapons;
        }

        /// <summary>Lista de armas cuerpo a cuerpo (DamageClass.Melee) cacheada.</summary>
        public static List<WeaponInfo> GetMeleeWeapons()
        {
            if (_meleeWeapons != null) return _meleeWeapons;
            _meleeWeapons = ScanWeapons(false, false, false, true);
            return _meleeWeapons;
        }

        /// <summary>Escanea todas las armas del juego por DamageType.</summary>
        private static List<WeaponInfo> ScanWeapons(bool ranged, bool magic, bool summon, bool melee = false)
        {
            var result = new List<WeaponInfo>();
            var seen = new HashSet<int>();

            for (int i = 0; i < ItemLoader.ItemCount; i++)
            {
                if (seen.Contains(i)) continue;
                seen.Add(i);

                try
                {
                    Item item = ContentSamples.ItemsByType[i];
                    if (item == null) continue;

                    // Debe ser un arma (damage > 0)
                    if (item.damage <= 0) continue;

                    // Verificar DamageType
                    bool matches = false;
                    if (ranged && item.CountsAsClass(DamageClass.Ranged)) matches = true;
                    if (magic && item.CountsAsClass(DamageClass.Magic)) matches = true;
                    if (summon && item.CountsAsClass(DamageClass.Summon)) matches = true;
                    if (melee && item.CountsAsClass(DamageClass.Melee) && !ranged && !magic && !summon) matches = true;

                    if (!matches) continue;

                    // Excluir items triviales (no armas reales)
                    if (item.consumable) continue;
                    if (item.createTile > 0) continue;  // coloca tiles, no es arma
                    if (item.createWall > 0) continue;
                    if (item.ammo > 0 && item.useAmmo == 0 && !ranged) continue;  // es munición

                    // Calcular costo de resonancia basado en el daño y rareza
                    int cost = CalculateResonanceCost(item);

                    string desc = GenerateDescription(item);

                    result.Add(new WeaponInfo
                    {
                        ItemId = i,
                        Name = item.Name,
                        Description = desc,
                        ResonanceCost = cost,
                        IsSummon = summon,
                    });
                }
                catch { /* ignorar items problemáticos */ }
            }

            // Ordenar por costo (ascendente) para que las baratas salgan primero
            result.Sort((a, b) => a.ResonanceCost.CompareTo(b.ResonanceCost));

            // NO limitar — mostrar TODAS las armas disponibles
            return result;
        }

        /// <summary>Calcula el costo de resonancia basado en el daño y rareza del item.</summary>
        private static int CalculateResonanceCost(Item item)
        {
            int baseCost = 5;
            // Daño contribuye al costo
            baseCost += item.damage / 10;
            // Rareza contribuye al costo
            if (item.rare >= ItemRarityID.Purple) baseCost += 30;
            else if (item.rare >= ItemRarityID.Lime) baseCost += 20;
            else if (item.rare >= ItemRarityID.LightRed) baseCost += 10;
            else if (item.rare >= ItemRarityID.Blue) baseCost += 5;
            // Hardmode items cuestan más
            if (item.value > Item.buyPrice(0, 1, 0, 0)) baseCost += 10;
            return System.Math.Max(3, System.Math.Min(baseCost, 50));
        }

        /// <summary>Genera una descripción breve del arma basada en sus propiedades.</summary>
        private static string GenerateDescription(Item item)
        {
            var parts = new List<string>();
            if (item.useTime < 15) parts.Add("rapida");
            else if (item.useTime > 35) parts.Add("lenta");
            if (item.knockBack > 5) parts.Add("knockback alto");
            if (item.crit > 0) parts.Add($"+{item.crit}% crit");
            if (item.shoot > 0) parts.Add("dispara proyectil");
            if (item.mana > 0) parts.Add($"mana {item.mana}");
            if (item.autoReuse) parts.Add("auto-uso");
            if (parts.Count == 0) parts.Add($"dano {item.damage}");
            return string.Join(", ", parts);
        }
    }
}
