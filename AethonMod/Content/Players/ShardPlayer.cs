using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// Datos por-jugador del Fragmento Génesis: nivel, XP, rama activa,
    /// nodos del árbol asignados, runas memorizadas, etc.
    /// Persistido en el save del jugador (SaveData/LoadData).
    /// </summary>
    public class ShardPlayer : ModPlayer
    {
        // --- Estado del fragmento ---
        /// <summary>Nivel actual del fragmento (1-indexado, infinito).</summary>
        public int ShardLevel = 1;

        /// <summary>XP acumulada hacia el siguiente nivel.</summary>
        public int ShardXP = 0;

        /// <summary>Rama principal del fragmento (None hasta que se imprima).</summary>
        public BranchType ActiveBranch = BranchType.None;

        /// <summary>Sub-forma del arma elegida dentro de la rama.</summary>
        public WeaponSubForm SubForm = WeaponSubForm.None;

        /// <summary>Contador de kills por clase de daño (para detectar la rama).</summary>
        public int DistanceKills = 0;
        public int MeleeKills = 0;
        public int MagicKills = 0;
        public const int KILLS_TO_IMPRINT = 20;

        /// <summary>True si el fragmento se ha imprimido (ya tiene rama).</summary>
        public bool IsImprinted => ActiveBranch != BranchType.None;

        // --- Árbol de habilidades ---
        /// <summary>IDs de nodos asignados en el árbol.</summary>
        public HashSet<string> AllocatedNodes = new();

        /// <summary>Seed del árbol procedural (0 = pendiente de generar).</summary>
        public int SkillTreeSeed = 0;

        // --- Códex de memoria (capstone) ---
        /// <summary>IDs de armas memorizadas (Runas de Memoria equipadas).</summary>
        public List<string> MemorizedRunes = new();

        // --- Monedas ---
        /// <summary>Fragmentos de Resonancia (moneda secundaria).</summary>
        public int ResonanceShards = 0;

        // --- Métodos de nivel ---

        /// <summary>XP necesaria para subir del nivel actual al siguiente.</summary>
        public int XPForNextLevel()
        {
            return (int)(80 * System.Math.Pow(ShardLevel, 1.5));
        }

        /// <summary>Otorga XP al fragmento. Si pasa el umbral, sube de nivel (recursivo).</summary>
        public void GrantXP(int amount)
        {
            if (!IsImprinted) return; // El fragmento no gana XP hasta imprprimirse.
            ShardXP += amount;
            while (ShardXP >= XPForNextLevel())
            {
                ShardXP -= XPForNextLevel();
                ShardLevel++;
                OnLevelUp();
            }
        }

        /// <summary>Llamado cuando el fragmento sube de nivel.</summary>
        private void OnLevelUp()
        {
            // TODO: notificación visual (texto flotante), evento cósmico por hito.
            // TODO: desbloquear slots de runa por nivel (runeSlotsForLevel).
        }

        /// <summary>Puntos de habilidad acumulados según el nivel (tabla por tramos).</summary>
        public int CumulativeSkillPoints()
        {
            // Mismo cálculo que el sitio web.
            int total = 0;
            int[][] tiers = {
                new[] { 1, 10, 1 },
                new[] { 11, 20, 2 },
                new[] { 21, 30, 3 },
                new[] { 31, 40, 4 },
                new[] { 41, 50, 5 },
                new[] { 51, 60, 6 },
                new[] { 61, 70, 7 },
                new[] { 71, 80, 8 },
                new[] { 81, 90, 9 },
                new[] { 91, 100, 10 },
            };
            foreach (var tier in tiers)
            {
                int min = tier[0], max = tier[1], perLevel = tier[2];
                int upper = System.Math.Min(ShardLevel, max);
                if (upper >= min)
                    total += (upper - min + 1) * perLevel;
                if (ShardLevel <= max) break;
            }
            // Post-100: +10 por nivel.
            if (ShardLevel > 100)
                total += (ShardLevel - 100) * 10;
            return total;
        }

        /// <summary>Slots de Runa de Memoria disponibles según el nivel.</summary>
        public int RuneSlots()
        {
            if (ShardLevel < 50) return 0;
            if (ShardLevel < 75) return 1;
            if (ShardLevel < 100) return 2;
            if (ShardLevel < 125) return 3;
            if (ShardLevel < 150) return 4;
            return 5;
        }

        // --- Persistencia ---

        public override void SaveData(TagCompound tag)
        {
            tag["shardLevel"] = ShardLevel;
            tag["shardXP"] = ShardXP;
            tag["activeBranch"] = (int)ActiveBranch;
            tag["subForm"] = (int)SubForm;
            tag["distanceKills"] = DistanceKills;
            tag["meleeKills"] = MeleeKills;
            tag["magicKills"] = MagicKills;
            tag["skillTreeSeed"] = SkillTreeSeed;
            tag["resonanceShards"] = ResonanceShards;
            tag["allocatedNodes"] = new List<string>(AllocatedNodes);
            tag["memorizedRunes"] = MemorizedRunes;
        }

        public override void LoadData(TagCompound tag)
        {
            ShardLevel = tag.GetInt("shardLevel");
            if (ShardLevel < 1) ShardLevel = 1;
            ShardXP = tag.GetInt("shardXP");
            ActiveBranch = (BranchType)tag.GetInt("activeBranch");
            SubForm = (WeaponSubForm)tag.GetInt("subForm");
            DistanceKills = tag.GetInt("distanceKills");
            MeleeKills = tag.GetInt("meleeKills");
            MagicKills = tag.GetInt("magicKills");
            SkillTreeSeed = tag.GetInt("skillTreeSeed");
            ResonanceShards = tag.GetInt("resonanceShards");
            AllocatedNodes = new HashSet<string>(tag.GetList<string>("allocatedNodes"));
            MemorizedRunes = new List<string>(tag.GetList<string>("memorizedRunes"));
        }

        // --- Aplicar efectos pasivos cada tick ---
        public override void PostUpdateEquips()
        {
            Systems.NodeEffectSystem.ApplyPassiveEffects(Player);
        }

        // --- Manejar daño entrante (escudo de maná) ---
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            // convert-0: Escudo de maná — el daño drena maná antes que HP
            if (Systems.NodeEffectSystem.HasManaShield(Player) && Player.statMana > 0)
            {
                int manaAbsorb = System.Math.Min(Player.statMana, modifiers.FinalDamage.Value.Round());
                Player.statMana -= manaAbsorb;
                modifiers.FinalDamage -= manaAbsorb;
            }
        }
    }
}
