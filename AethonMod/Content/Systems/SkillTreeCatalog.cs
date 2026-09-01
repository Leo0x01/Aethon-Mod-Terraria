using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Sistema completo de árbol de habilidades — reescrito desde cero
    /// siguiendo el patrón de AnRPG (mrshinx/AnRPG-Edited).
    ///
    /// Un solo árbol compartido con posiciones X/Y explícitas y vecinos por índice.
    /// Los nodos se definen en un array estático (como JsonNodeList de AnRPG).
    /// </summary>

    // ====================================================================
    // Tipos de nodo (como NodeType de AnRPG)
    // ====================================================================
    public enum SkillNodeType
    {
        Start,      // Nodo inicial (gratis, ya activado)
        Damage,     // +X% daño de una clase
        Stat,       // +X a un stat
        Speed,      // +X% velocidad de uso
        Life,       // +X% lifesteal
        Mana,       // +X maná máximo
        Minion,     // +X slots de minion
        Keystone,   // Efecto especial poderoso
        Ascendancy,  // Post-nivel 100
    }

    // ====================================================================
    // Definición de un nodo (como JsonNode de AnRPG)
    // ====================================================================
    public class SkillNodeData
    {
        public string Name;
        public SkillNodeType Type;
        public float PosX;
        public float PosY;
        public int[] Neighbors;   // índices de nodos vecinos
        public string Effect;      // descripción del efecto
        public float ValuePerLevel; // valor del efecto por nivel
        public int MaxLevel;       // nivel máximo (1 = una sola vez)
        public int PointsPerLevel;  // puntos necesarios por nivel
        public int LevelReq;        // nivel del fragmento requerido
        public bool Unlocked;      // si empieza desbloqueado (start)
        public string SpecificType; // "Melee", "Ranged", "Magic", "Summon", "Speed", "Life", "Mana", "Minion", etc.

        public SkillNodeData(string name, SkillNodeType type, float x, float y, int[] neighbors,
            string effect, float valuePerLevel, int maxLevel, int pointsPerLevel, int levelReq = 1,
            bool unlocked = false, string specificType = "")
        {
            Name = name; Type = type; PosX = x; PosY = y; Neighbors = neighbors;
            Effect = effect; ValuePerLevel = valuePerLevel; MaxLevel = maxLevel;
            PointsPerLevel = pointsPerLevel; LevelReq = levelReq; Unlocked = unlocked;
            SpecificType = specificType;
        }
    }

    // ====================================================================
    // Estado de un nodo en runtime (nivel asignado)
    // ====================================================================
    public class SkillNodeState
    {
        public int Level = 0;
        public bool Activated => Level > 0;
        public bool CanActivate; // calculado cada frame
    }

    // ====================================================================
    // El árbol completo — una sola lista de nodos con posiciones y vecinos
    // ====================================================================
    public static class SkillTreeCatalog
    {
        // El árbol completo — 60 nodos con posiciones radiales
        // Igual que AnRPG.JsonNodeList: un array estático con todo definido
        public static SkillNodeData[] Nodes = new SkillNodeData[]
        {
            // === CENTRO ===
            // 0: Nodo inicial (gratis, ya activado)
            new("Fragmento Génesis", SkillNodeType.Start, 0, 0, new[]{1,2,3,4,5,6,7,8},
                "Punto de partida. El fragmento despierta.", 0, 1, 0, 1, true),

            // === RAMA DISTANCIA (derecha) ===
            // 1: Daño a distancia
            new("Daño a Distancia", SkillNodeType.Damage, 200, -100, new[]{0,9,10},
                "+2% daño a distancia por nivel", 0.02f, 15, 1, 1, false, "Ranged"),
            // 2: Velocidad de disparo
            new("Velocidad de Disparo", SkillNodeType.Speed, 300, -50, new[]{0,1,11},
                "+2% velocidad de disparo por nivel", 0.02f, 10, 1, 5, false, "Ranged"),
            // 3: Crítico a distancia
            new("Crítico a Distancia", SkillNodeType.Damage, 250, -200, new[]{0,9,12},
                "+1% prob. de crítico a distancia por nivel", 0.01f, 10, 1, 5, false, "Ranged"),

            // === RAMA CUERPO A CUERPO (izquierda) ===
            // 4: Daño cuerpo a cuerpo
            new("Daño Cuerpo a Cuerpo", SkillNodeType.Damage, -200, -100, new[]{0,13,14},
                "+2% daño cuerpo a cuerpo por nivel", 0.02f, 15, 1, 1, false, "Melee"),
            // 5: Velocidad de ataque
            new("Velocidad de Ataque", SkillNodeType.Speed, -300, -50, new[]{0,4,15},
                "+2% velocidad de ataque por nivel", 0.02f, 10, 1, 5, false, "Melee"),
            // 6: Defensa
            new("Defensa", SkillNodeType.Stat, -250, -200, new[]{0,13,16},
                "+5 defensa por nivel", 5f, 10, 1, 5, false, "Defense"),

            // === RAMA MÁGICA (arriba) ===
            // 7: Daño mágico
            new("Daño Mágico", SkillNodeType.Damage, 0, -250, new[]{0,17,18},
                "+2% daño mágico por nivel", 0.02f, 15, 1, 1, false, "Magic"),
            // 8: Daño de invocación
            new("Daño de Invocación", SkillNodeType.Damage, 100, -250, new[]{0,17,19},
                "+2% daño de invocación por nivel", 0.02f, 15, 1, 1, false, "Summon"),

            // === DISTANCIA — nodos avanzados ===
            // 9: Cadencia rápida
            new("Cadencia Rápida", SkillNodeType.Speed, 350, -150, new[]{1,2,20},
                "+5% cadencia de disparo", 0.05f, 5, 2, 10, false, "Ranged"),
            // 10: Perforación
            new("Perforación", SkillNodeType.Damage, 300, -250, new[]{1,3,21},
                "+10% daño, perfora enemigos", 0.10f, 5, 3, 15, false, "Ranged"),
            // 11: Multidisparo
            new("Multidisparo", SkillNodeType.Damage, 400, 0, new[]{2,9,22},
                "+1 proyectil por disparo", 1f, 3, 5, 20, false, "Ranged"),
            // 12: Disparo celestial
            new("Disparo Celestial", SkillNodeType.Keystone, 200, -350, new[]{3,10,23},
                "Los proyectiles generan meteoros al impactar. +50% daño solar.", 0.50f, 1, 8, 25, false, "Ranged"),

            // === CUERPO A CUERPO — nodos avanzados ===
            // 13: Combo
            new("Combo", SkillNodeType.Damage, -350, -150, new[]{4,5,24},
                "+3% daño por golpe en combo", 0.03f, 10, 1, 10, false, "Melee"),
            // 14: Golpe pesado
            new("Golpe Pesado", SkillNodeType.Damage, -300, -250, new[]{4,6,25},
                "+10% daño, +50% knockback", 0.10f, 5, 3, 15, false, "Melee"),
            // 15: Furia
            new("Furia", SkillNodeType.Speed, -400, 0, new[]{5,13,26},
                "+5% velocidad de ataque", 0.05f, 5, 2, 20, false, "Melee"),
            // 16: Guardia eterna
            new("Guardia Eterna", SkillNodeType.Keystone, -200, -350, new[]{6,14,27},
                "+50 defensa. Inmune a veneno y fuego.", 50f, 1, 8, 25, false, "Defense"),

            // === MÁGICA — nodos avanzados ===
            // 17: Flujo de maná
            new("Flujo de Maná", SkillNodeType.Mana, 50, -350, new[]{7,8,28},
                "+20 maná máximo por nivel", 20f, 5, 1, 10, false, "Mana"),
            // 18: Velocidad de lanzamiento
            new("Velocidad de Lanzamiento", SkillNodeType.Speed, -50, -350, new[]{7,17,29},
                "+3% velocidad de lanzamiento por nivel", 0.03f, 10, 1, 10, false, "Magic"),
            // 19: Slots de minion
            new("Slots de Minion", SkillNodeType.Minion, 150, -350, new[]{8,17,30},
                "+1 slot de minion por nivel", 1f, 3, 2, 15, false, "Summon"),

            // === KEYSTONES DE RAMA ===
            // 20: Lluvia de meteoros (Distancia)
            new("Lluvia de Meteoros", SkillNodeType.Keystone, 450, -200, new[]{9,11,31},
                "Cada disparo tiene 20% de generar meteoros.", 0.20f, 1, 5, 30, false, "Ranged"),
            // 21: Proyectiles fantasma (Distancia)
            new("Proyectiles Fantasma", SkillNodeType.Keystone, 350, -300, new[]{10,12,32},
                "Los proyectiles atraviesan terreno y perforan 5 enemigos.", 5f, 1, 5, 30, false, "Ranged"),
            // 22: Carcaj infinito (Distancia)
            new("Carcaj Infinito", SkillNodeType.Keystone, 450, 50, new[]{11,20,33},
                "No consume munición. +2 proyectiles por disparo.", 2f, 1, 5, 35, false, "Ranged"),
            // 23: Supernova (Distancia)
            new("Supernova", SkillNodeType.Keystone, 250, -400, new[]{12,21,34},
                "Disparo cargado: supernova de 12 tiles. +50% daño solar.", 0.50f, 1, 10, 40, false, "Ranged"),

            // 24: Filo infinito (Melee)
            new("Filo Infinito", SkillNodeType.Keystone, -450, -200, new[]{13,15,35},
                "Combo sin tope. +5% daño por combo acumulado.", 0.05f, 1, 5, 30, false, "Melee"),
            // 25: Ira solar (Melee)
            new("Ira Solar", SkillNodeType.Keystone, -350, -300, new[]{14,16,36},
                "Aura de quemadura 3 tiles. Enemigos quemados explotan.", 3f, 1, 5, 30, false, "Melee"),
            // 26: Furia de sangre (Melee)
            new("Furia de Sangre", SkillNodeType.Keystone, -450, 50, new[]{15,24,37},
                "+50% velocidad de ataque cuando HP < 30%.", 0.50f, 1, 5, 35, false, "Melee"),
            // 27: Vampírico (Melee)
            new("Vampírico", SkillNodeType.Keystone, -250, -400, new[]{16,25,38},
                "Lifesteal permanente: +10% del daño se convierte en salud.", 0.10f, 1, 8, 40, false, "Melee"),

            // 28: Maná infinito (Mágica)
            new("Maná Infinito", SkillNodeType.Keystone, 100, -400, new[]{17,18,39},
                "+100 maná. Lanzar con <20 maná es gratis.", 100f, 1, 5, 30, false, "Mana"),
            // 29: Tormenta de bolts (Mágica)
            new("Tormenta de Bolts", SkillNodeType.Keystone, 0, -400, new[]{18,28,40},
                "+5 bolts por lanzamiento gratis.", 5f, 1, 5, 30, false, "Magic"),
            // 30: Enjambre estelar (Mágica/Invocación)
            new("Enjambre Estelar", SkillNodeType.Keystone, 200, -400, new[]{19,28,41},
                "+5 slots de minion. Minions disparan bolts.", 5f, 1, 8, 35, false, "Summon"),

            // === NODOS DE VIDA/MANA UNIVERSALES ===
            // 31: Vida máxima
            new("Vida Máxima", SkillNodeType.Stat, 500, -150, new[]{20,22,42},
                "+20 vida máxima por nivel", 20f, 10, 1, 10, false, "Life"),
            // 32: Vida máxima II
            new("Constitución", SkillNodeType.Stat, 400, -350, new[]{21,23,43},
                "+50 vida máxima por nivel", 50f, 5, 3, 25, false, "Life"),
            // 33: Regeneración
            new("Regeneración", SkillNodeType.Life, 500, 100, new[]{22,31,44},
                "+1 regen de vida/seg por nivel", 1f, 5, 2, 20, false, "Life"),

            // 34: Lifesteal universal
            new("Lifesteal", SkillNodeType.Life, 300, -450, new[]{23,32,45},
                "+1% lifesteal de todo el daño", 0.01f, 5, 3, 30, false, "Life"),

            // 35: Defensa avanzada
            new("Defensa Avanzada", SkillNodeType.Stat, -500, -150, new[]{24,26,46},
                "+10 defensa por nivel", 10f, 5, 2, 15, false, "Defense"),
            // 36: Defensa maestra
            new("Defensa Maestra", SkillNodeType.Stat, -400, -350, new[]{25,27,47},
                "+30 defensa por nivel", 30f, 3, 5, 25, false, "Defense"),
            // 37: Velocidad de movimiento
            new("Velocidad de Movimiento", SkillNodeType.Speed, -500, 100, new[]{26,35,48},
                "+5% velocidad de movimiento por nivel", 0.05f, 5, 2, 15, false, "Speed"),

            // 38: Lifesteal mejorado
            new("Lifesteal Mejorado", SkillNodeType.Life, -300, -450, new[]{27,36,49},
                "+2% lifesteal adicional", 0.02f, 5, 5, 35, false, "Life"),

            // 39: Regen de maná
            new("Regen de Maná", SkillNodeType.Mana, 150, -450, new[]{28,29,50},
                "+2 regen de maná/seg por nivel", 2f, 5, 2, 20, false, "Mana"),
            // 40: Crítico mágico
            new("Crítico Mágico", SkillNodeType.Damage, 50, -450, new[]{29,39,51},
                "+2% crítico mágico por nivel", 0.02f, 10, 2, 20, false, "Magic"),

            // 41: Minion mejorado
            new("Minions Mejorados", SkillNodeType.Minion, 250, -450, new[]{30,39,52},
                "+10% daño de minions por nivel", 0.10f, 5, 3, 20, false, "Summon"),

            // === ASCENDANCIA (post-nivel 100) ===
            // 42: Ascendencia I
            new("Ascendencia I", SkillNodeType.Ascendancy, 550, -200, new[]{31,33,53},
                "+50% daño total. Requiere nivel 100.", 0.50f, 1, 10, 100, false, "All"),
            // 43: Ascendencia II
            new("Ascendencia II", SkillNodeType.Ascendancy, 450, -400, new[]{32,34,54},
                "Proyectiles buscan enemigos automáticamente. Requiere nivel 100.", 0f, 1, 10, 100, false, "All"),
            // 44: Ascendencia III
            new("Ascendencia III", SkillNodeType.Ascendancy, 550, 150, new[]{33,42,55},
                "+100% velocidad de todo. Requiere nivel 100.", 1.0f, 1, 10, 100, false, "Speed"),
            // 45: Regeneración vital (Ascendancy)
            new("Regeneración Vital", SkillNodeType.Ascendancy, 350, -500, new[]{34,43,56},
                "Lifesteal: +0.01% de salud máxima por punto de daño. Requiere nivel 100.", 0.0001f, 1, 10, 100, false, "Life"),

            // 46: Ascendencia IV (Melee)
            new("Ascendencia IV", SkillNodeType.Ascendancy, -550, -200, new[]{35,37,57},
                "+50% daño cuerpo a cuerpo. Requiere nivel 100.", 0.50f, 1, 10, 100, false, "Melee"),
            // 47: Ascendencia V (Defensa)
            new("Ascendencia V", SkillNodeType.Ascendancy, -450, -400, new[]{36,38,58},
                "Inmune a todo el daño durante 2 segundos al recibir golpe. Requiere nivel 100.", 0f, 1, 10, 100, false, "Defense"),
            // 48: Ascendencia VI (Velocidad)
            new("Ascendencia VI", SkillNodeType.Ascendancy, -550, 150, new[]{37,46,59},
                "+100% velocidad de movimiento. Requiere nivel 100.", 1.0f, 1, 10, 100, false, "Speed"),

            // 49: Lifesteal definitivo
            new("Lifesteal Definitivo", SkillNodeType.Ascendancy, -350, -500, new[]{38,47,60},
                "+20% lifesteal de todo el daño. Requiere nivel 100.", 0.20f, 1, 10, 100, false, "Life"),

            // 50: Maná definitivo
            new("Maná Definitivo", SkillNodeType.Ascendancy, 200, -500, new[]{39,41,61},
                "Maná infinito. Todos los hechizos son gratis. Requiere nivel 100.", 0f, 1, 10, 100, false, "Mana"),
            // 51: Crítico definitivo
            new("Crítico Definitivo", SkillNodeType.Ascendancy, 100, -500, new[]{40,50,62},
                "+30% crítico a todo. +100% daño crítico. Requiere nivel 100.", 0.30f, 1, 10, 100, false, "All"),

            // 52: Enjambre definitivo
            new("Enjambre Definitivo", SkillNodeType.Ascendancy, 300, -500, new[]{41,51,63},
                "+10 slots de minion. Minions heredan +50% daño. Requiere nivel 100.", 10f, 1, 10, 100, false, "Summon"),

            // === NODOS FINALES (centro exterior) ===
            // 53-63: conectan las ascendencias
            new("Maestría Total", SkillNodeType.Keystone, 600, -250, new[]{42,44,64},
                "+100% daño total. Todos los efectos duplicados.", 1.0f, 1, 20, 150, false, "All"),
            new("Visión Total", SkillNodeType.Keystone, 500, -450, new[]{43,45,64},
                "Todos los proyectiles son homing. +50% daño total.", 0.50f, 1, 20, 150, false, "All"),
            new("Velocidad Total", SkillNodeType.Keystone, 600, 200, new[]{44,53,64},
                "+200% velocidad de todo. Sin cooldowns.", 2.0f, 1, 20, 150, false, "Speed"),
            new("Inmortalidad", SkillNodeType.Keystone, 400, -550, new[]{45,49,64},
                "Inmune a todo el daño durante 5 segundos cada 30 segundos.", 0f, 1, 20, 150, false, "Life"),
            new("Fuerza Total", SkillNodeType.Keystone, -600, -250, new[]{46,48,64},
                "+100% daño cuerpo a cuerpo. +100 defensa.", 1.0f, 1, 20, 150, false, "Melee"),
            new("Baluarte Total", SkillNodeType.Keystone, -500, -450, new[]{47,49,64},
                "+200 defensa. Inmune a debuffs.", 200f, 1, 20, 150, false, "Defense"),
            new("Velocidad Total II", SkillNodeType.Keystone, -600, 200, new[]{48,46,64},
                "+200% velocidad de movimiento y ataque.", 2.0f, 1, 20, 150, false, "Speed"),
            new("Vampiro Total", SkillNodeType.Keystone, -400, -550, new[]{49,47,64},
                "Lifesteal: +50% del daño se convierte en salud.", 0.50f, 1, 20, 150, false, "Life"),
            new("Mente Total", SkillNodeType.Keystone, 250, -550, new[]{50,52,64},
                "Maná infinito. +100% daño mágico.", 1.0f, 1, 20, 150, false, "Magic"),
            new("Devastación", SkillNodeType.Keystone, 150, -550, new[]{51,50,64},
                "+100% crítico. +200% daño crítico.", 1.0f, 1, 20, 150, false, "All"),
            new("Enjambre Total", SkillNodeType.Keystone, 350, -550, new[]{52,41,64},
                "+20 slots de minion. Minions hacen +100% daño.", 20f, 1, 20, 150, false, "Summon"),

            // 64: Nodo final supremo
            new("Aethon, la Luz Primordial", SkillNodeType.Keystone, 0, -600,
                new[]{53,54,55,56,57,58,59,60,61,62,63},
                "Despierta el poder de Aethon. Todos los efectos x2. Requiere nivel 200.", 2.0f, 1, 50, 200, false, "All"),
        };

        // Estado de cada nodo (nivel asignado) — se guarda en ShardPlayer
        public static SkillNodeState[] GetInitialState()
        {
            var states = new SkillNodeState[Nodes.Length];
            for (int i = 0; i < Nodes.Length; i++)
            {
                states[i] = new SkillNodeState();
                if (Nodes[i].Unlocked) states[i].Level = 1; // start node
            }
            return states;
        }

        /// <summary>Verifica si un nodo puede ser activado (tiene un vecino activado).</summary>
        public static bool CanActivate(int nodeIdx, SkillNodeState[] states, int shardLevel, int availablePoints)
        {
            if (nodeIdx < 0 || nodeIdx >= Nodes.Length) return false;
            var node = Nodes[nodeIdx];
            var state = states[nodeIdx];

            // Si ya está al máximo
            if (state.Level >= node.MaxLevel) return false;

            // Si no tiene suficiente nivel del fragmento
            if (shardLevel < node.LevelReq) return false;

            // Si no tiene puntos suficientes
            if (availablePoints < node.PointsPerLevel) return false;

            // Si es el nodo inicial, siempre se puede
            if (node.Unlocked) return true;

            // Si algún vecino está activado
            foreach (int neighborIdx in node.Neighbors)
            {
                if (neighborIdx >= 0 && neighborIdx < states.Length && states[neighborIdx].Level > 0)
                    return true;
            }
            return false;
        }

        /// <summary>Calcula los puntos gastados.</summary>
        public static int SpentPoints(SkillNodeState[] states)
        {
            int total = 0;
            for (int i = 0; i < Nodes.Length; i++)
            {
                total += states[i].Level * Nodes[i].PointsPerLevel;
            }
            return total;
        }

        // ====================================================================
        // APLICAR EFECTOS — se llama cada tick
        // ====================================================================
        public static void ApplyEffects(Player player, SkillNodeState[] states)
        {
            float rangedDmg = 0, meleeDmg = 0, magicDmg = 0, summonDmg = 0;
            float rangedSpeed = 0, meleeSpeed = 0, magicSpeed = 0, moveSpeed = 0;
            float lifeMax = 0, manaMax = 0, defense = 0, lifesteal = 0;
            int minionSlots = 0;
            float rangedCrit = 0, magicCrit = 0;

            for (int i = 0; i < Nodes.Length; i++)
            {
                if (states[i].Level <= 0) continue;
                var node = Nodes[i];
                float val = node.ValuePerLevel * states[i].Level;

                switch (node.Type)
                {
                    case SkillNodeType.Damage:
                        switch (node.SpecificType)
                        {
                            case "Ranged": rangedDmg += val; break;
                            case "Melee": meleeDmg += val; break;
                            case "Magic": magicDmg += val; break;
                            case "Summon": summonDmg += val; break;
                        }
                        break;
                    case SkillNodeType.Speed:
                        switch (node.SpecificType)
                        {
                            case "Ranged": rangedSpeed += val; break;
                            case "Melee": meleeSpeed += val; break;
                            case "Magic": magicSpeed += val; break;
                            case "Speed": moveSpeed += val; break;
                        }
                        break;
                    case SkillNodeType.Stat:
                        switch (node.SpecificType)
                        {
                            case "Life": lifeMax += val; break;
                            case "Defense": defense += val; break;
                        }
                        break;
                    case SkillNodeType.Mana:
                        manaMax += val;
                        break;
                    case SkillNodeType.Life:
                        lifesteal += val;
                        break;
                    case SkillNodeType.Minion:
                        minionSlots += (int)val;
                        break;
                }
            }

            // Aplicar al jugador
            player.GetDamage(DamageClass.Ranged) += rangedDmg;
            player.GetDamage(DamageClass.Melee) += meleeDmg;
            player.GetDamage(DamageClass.Magic) += magicDmg;
            player.GetDamage(DamageClass.Summon) += summonDmg;
            player.GetCritChance(DamageClass.Ranged) += rangedCrit;
            player.GetCritChance(DamageClass.Magic) += magicCrit;
            player.statLifeMax2 += (int)lifeMax;
            player.statManaMax2 += (int)manaMax;
            player.statDefense += (int)defense;
            player.maxMinions += minionSlots;
            player.moveSpeed += moveSpeed;
        }
    }
}
