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

        /// <summary>True si el Codex de Memoria ha sido desbloqueado (al conseguir un arma magica/invocacion).</summary>
        public bool CodexUnlocked = false;

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
            if (!IsImprinted) return; // El fragmento no gana XP hasta imprimirse.
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
            // Notificacion visual de nivel subido.
            Main.NewText($"✦ Fragmento Genesis ha alcanzado el nivel {ShardLevel}!",
                new Microsoft.Xna.Framework.Color(245, 196, 81));
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item4);

            // Explosion de particulas doradas alrededor del jugador.
            for (int i = 0; i < 40; i++)
            {
                Dust.NewDustPerfect(Player.Center, Terraria.ID.DustID.GoldFlame,
                    new Microsoft.Xna.Framework.Vector2(Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-6, 6)),
                    100, new Microsoft.Xna.Framework.Color(245, 196, 81), 1.5f);
            }
            // Estrellas brillantes.
            for (int i = 0; i < 20; i++)
            {
                Dust.NewDustPerfect(Player.Center, Terraria.ID.DustID.YellowStarDust,
                    new Microsoft.Xna.Framework.Vector2(Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, 5)),
                    150, default, 1.3f);
            }

            // Hitos cosmicos con efectos especiales.
            if (ShardLevel == 10)
            {
                Main.NewText("✦ Hito: Primer despertar. El fragmento se solidifica.", new Microsoft.Xna.Framework.Color(179, 136, 255));
                for (int i = 0; i < 60; i++)
                    Dust.NewDustPerfect(Player.Center, Terraria.ID.DustID.PurpleTorch,
                        new Microsoft.Xna.Framework.Vector2(Main.rand.NextFloat(-8, 8), Main.rand.NextFloat(-8, 8)),
                        100, new Microsoft.Xna.Framework.Color(179, 136, 255), 2f);
            }
            if (ShardLevel == 25)
            {
                Main.NewText("✦ Hito: Lluvia de Luz Estelar activada.", new Microsoft.Xna.Framework.Color(179, 136, 255));
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Thunder);
            }
            if (ShardLevel == 50)
            {
                Main.NewText("✦ Hito: El Sagrario Hueco se extiende.", new Microsoft.Xna.Framework.Color(179, 136, 255));
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Roar);
            }
            if (ShardLevel == 100)
            {
                Main.NewText("✦✦ HITO CRITICO: Aethon se agita. Zona de Ascendancy desbloqueada. ✦✦", new Microsoft.Xna.Framework.Color(245, 196, 81));
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.DD2_EtherianPortalOpen);
                // Explosion masiva de particulas.
                for (int i = 0; i < 100; i++)
                    Dust.NewDustPerfect(Player.Center, Terraria.ID.DustID.GoldFlame,
                        new Microsoft.Xna.Framework.Vector2(Main.rand.NextFloat(-12, 12), Main.rand.NextFloat(-12, 12)),
                        100, new Microsoft.Xna.Framework.Color(245, 196, 81), 3f);
            }
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

        /// <summary>Puntos de habilidad gastados (suma de costos de nodos asignados).</summary>
        public int SpentSkillPoints()
        {
            int spent = 0;
            var tree = Systems.PoETreeCatalog.GetTree(ActiveBranch);
            foreach (var nodeId in AllocatedNodes)
            {
                var node = tree.Nodes.Find(n => n.Id == nodeId);
                if (node != null) spent += node.Cost;
            }
            return spent;
        }

        /// <summary>Puntos de habilidad disponibles (total ganado - gastado).</summary>
        public int AvailableSkillPoints()
        {
            return CumulativeSkillPoints() - SpentSkillPoints();
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
            tag["codexUnlocked"] = CodexUnlocked;
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
            CodexUnlocked = tag.GetBool("codexUnlocked");
        }

        // --- Aplicar efectos pasivos cada tick ---
        public override void PostUpdateEquips()
        {
            Systems.NodeEffectSystem.ApplyPassiveEffects(Player);
        }

        // --- Manejar daño entrante (escudo de maná) ---
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            // Escudo de maná: reducir el daño ANTES de que se aplique (no curar después).
            if (Systems.NodeEffectSystem.HasManaShield(Player) && Player.statMana > 0)
            {
                // Calcular cuanto maná podemos absorber
                float absorbPct = 0.5f;
                if (Systems.NodeEffectSystem.HasNode(Player, "barrier-keystone"))
                    absorbPct = 0.8f;

                // El daño base antes de modificadores
                float baseDamage = modifiers.SourceDamage.ApplyTo(0);
                if (baseDamage <= 0) baseDamage = 1;

                int manaAbsorb = (int)(baseDamage * absorbPct);
                manaAbsorb = System.Math.Min(Player.statMana, manaAbsorb);

                if (manaAbsorb > 0)
                {
                    // Drenar maná
                    Player.statMana -= manaAbsorb;
                    // Reducir el daño (antes de que se aplique)
                    modifiers.SourceDamage -= manaAbsorb;
                    // Particulas visuales
                    for (int i = 0; i < 8; i++)
                        Dust.NewDustPerfect(Player.Center, Terraria.ID.DustID.Enchanted_Pink,
                            new Microsoft.Xna.Framework.Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                            100, default, 1.2f);
                }
            }
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            // El escudo de maná ahora se maneja en ModifyHurt (antes del daño).
            // Aqui solo efectos visuales si el jugador tiene el escudo.
            if (Systems.NodeEffectSystem.HasManaShield(Player))
            {
                // Polvo adicional al recibir daño con escudo activo
                for (int i = 0; i < 4; i++)
                    Dust.NewDustPerfect(Player.Center, Terraria.ID.DustID.Enchanted_Pink,
                        new Microsoft.Xna.Framework.Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2)),
                        80, default, 1f);
            }
        }
    }
}
