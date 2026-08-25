using System.Collections.Generic;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Estructura de un nodo del árbol de habilidades.
    /// </summary>
    public class SkillNodeData
    {
        public string Id;
        public string Name;
        public string Branch;      // sub-rama (ej: "Mana Flow")
        public NodeRarity Rarity;
        public int Cost;            // puntos de habilidad
        public string Effect;       // descripción
        public string? PrereqId;    // nodo requerido antes
        // Coords para UI (0..1 normalizadas)
        public float X;
        public float Y;
    }

    public enum NodeRarity { Common, Rare, Legendary }

    /// <summary>
    /// Sub-rama del árbol (6 por rama, 7 para Magic).
    /// </summary>
    public class SkillBranchData
    {
        public string Id;
        public string Name;
        public string Icon;         // carácter/emoji
        public string Description;
        public bool Capstone;      // rama de absorción de lore
    }

    /// <summary>
    /// Datos completos del árbol de una rama principal.
    /// </summary>
    public class SkillTreeData
    {
        public BranchType Branch;
        public List<SkillBranchData> Branches;
        public List<SkillNodeData> Nodes;
    }

    /// <summary>
    /// Catálogo estático de los 3 árboles de habilidades.
    /// Generado a partir del documento de diseño (DISEÑO_DEL_MOD.md §5-§7).
    /// </summary>
    public static class SkillTreeCatalog
    {
        /// <summary>
        /// Devuelve el árbol de habilidades completo para una rama.
        /// </summary>
        public static SkillTreeData GetTree(BranchType branch)
        {
            return branch switch
            {
                BranchType.Distance => BuildDistanceTree(),
                BranchType.Melee => BuildMeleeTree(),
                BranchType.Magic => BuildMagicTree(),
                _ => new SkillTreeData { Branch = branch, Branches = new(), Nodes = new() },
            };
        }

        // ====================================================================
        // DISTANCIA — Lumina, la Arcoestelar (6 sub-ramas × 5 nodos = 30)
        // ====================================================================
        private static SkillTreeData BuildDistanceTree()
        {
            var branches = new List<SkillBranchData>
            {
                new() { Id = "arrow",    Name = "Génesis de Proyectiles",  Icon = "➶", Description = "Nuevos tipos de munición/proyectil.", Capstone = false },
                new() { Id = "quiver",   Name = "Maestría de Carcaj",      Icon = "🎒", Description = "Más proyectiles, tiro rápido, sin munición.", Capstone = false },
                new() { Id = "mark",     Name = "Marca del Cazador",       Icon = "◎", Description = "Etiqueta, crit stacking, punto débil.", Capstone = false },
                new() { Id = "celestial",Name = "Disparos Celestiales",    Icon = "✦", Description = "Meteoros, Destello solar, Eclipse.", Capstone = false },
                new() { Id = "phantom",  Name = "Carcaj Fantasma",         Icon = "◈", Description = "Proyectiles que fasan terreno, ricochet.", Capstone = false },
                new() { Id = "absorb",   Name = "Absorción de Lore",       Icon = "⭐", Description = "Capstone: memoriza armas de distancia.", Capstone = true },
            };
            var nodes = new List<SkillNodeData>();
            int idx = 0;
            // Rama 1: Génesis de Proyectiles
            AddBranch(nodes, branches[0], idx++, "Flecha estelar", NodeRarity.Common, 1, "Ignora 5 de defensa.", null, 0);
            AddBranch(nodes, branches[0], idx++, "Virote de vacío", NodeRarity.Rare, 1, "+30% daño, drena 4 maná.", "arrow-0");
            AddBranch(nodes, branches[0], idx++, "Disparo dividido", NodeRarity.Rare, 1, "2 proyectiles en abanico.", "arrow-1");
            AddBranch(nodes, branches[0], idx++, "Estrella guiada", NodeRarity.Rare, 1, "Proyectiles curvan al enemigo.", "arrow-2");
            AddBranch(nodes, branches[0], idx++, "Salva triple", NodeRarity.Legendary, 3, "Cada 3er disparo: 3 virotes buscadores.", "arrow-3");
            // Rama 2: Maestría de Carcaj
            AddBranch(nodes, branches[1], idx++, "Tiro rápido", NodeRarity.Common, 1, "-15% tiempo de tensión.", null, 1);
            AddBranch(nodes, branches[1], idx++, "Cuerda doble", NodeRarity.Rare, 1, "+1 proyectil por disparo.", "quiver-0");
            AddBranch(nodes, branches[1], idx++, "Carcaj infinito", NodeRarity.Rare, 1, "No consume munición.", "quiver-1");
            AddBranch(nodes, branches[1], idx++, "Ráfaga", NodeRarity.Rare, 1, "Mantén: dispara 6 proyectiles.", "quiver-2");
            AddBranch(nodes, branches[1], idx++, "Tormenta de estrellas", NodeRarity.Legendary, 4, "Cargado: llueve 12 proyectiles.", "quiver-3");
            // Rama 3: Marca del Cazador
            AddBranch(nodes, branches[2], idx++, "Etiqueta", NodeRarity.Common, 1, "Golpes marcan: +10% daño.", null, 2);
            AddBranch(nodes, branches[2], idx++, "Acumulación de crítico", NodeRarity.Rare, 1, "+4% crítico por golpe (máx 40%).", "mark-0");
            AddBranch(nodes, branches[2], idx++, "Punto débil", NodeRarity.Rare, 1, "Enemigos marcados: siempre crítico.", "mark-1");
            AddBranch(nodes, branches[2], idx++, "Foco del cazador", NodeRarity.Rare, 1, "Quieto 1s: doble crítico.", "mark-2");
            AddBranch(nodes, branches[2], idx++, "Marca letal", NodeRarity.Legendary, 3, "Matar marcado: resetea cooldowns.", "mark-3");
            // Rama 4: Disparos Celestiales
            AddBranch(nodes, branches[3], idx++, "Salva de meteoros", NodeRarity.Rare, 1, "Alt-fuego: 3 meteoros.", null, 3);
            AddBranch(nodes, branches[3], idx++, "Destello solar", NodeRarity.Rare, 1, "Proyectiles incendian.", "celestial-0");
            AddBranch(nodes, branches[3], idx++, "Eclipse", NodeRarity.Rare, 1, "Cargado: ciega + quema (10s cd).", "celestial-1");
            AddBranch(nodes, branches[3], idx++, "Cascada estelar", NodeRarity.Rare, 1, "Cada golpe: 2 mini-estrellas.", "celestial-2");
            AddBranch(nodes, branches[3], idx++, "Supernova", NodeRarity.Legendary, 5, "Cargado: supernova 12 tiles.", "celestial-3");
            // Rama 5: Carcaj Fantasma
            AddBranch(nodes, branches[4], idx++, "Disparo fase", NodeRarity.Rare, 1, "Atraviesa 3 tiles de terreno.", null, 4);
            AddBranch(nodes, branches[4], idx++, "Ricochet", NodeRarity.Rare, 1, "Rebota en paredes 2 veces.", "phantom-0");
            AddBranch(nodes, branches[4], idx++, "Betty rebotadora", NodeRarity.Rare, 1, "Fallos: mina de luz estacionaria.", "phantom-1");
            AddBranch(nodes, branches[4], idx++, "Cadena fantasma", NodeRarity.Rare, 1, "Rebota entre 3 enemigos.", "phantom-2");
            AddBranch(nodes, branches[4], idx++, "Forma etérea", NodeRarity.Legendary, 4, "Todo fasar + perfora 5.", "phantom-3");
            // Rama 6: Absorción de Lore (capstone)
            AddBranch(nodes, branches[5], idx++, "Códex de memoria I", NodeRarity.Rare, 1, "Slot de Runa 1.", null, 5);
            AddBranch(nodes, branches[5], idx++, "Códex de memoria II", NodeRarity.Rare, 1, "Slot de Runa 2.", "absorb-0");
            AddBranch(nodes, branches[5], idx++, "Códex de memoria III", NodeRarity.Rare, 1, "Slot de Runa 3.", "absorb-1");
            AddBranch(nodes, branches[5], idx++, "Afinación de resonancia", NodeRarity.Legendary, 5, "Memoriza arma de distancia.", "absorb-2");
            AddBranch(nodes, branches[5], idx++, "Carcaj omnisciente", NodeRarity.Legendary, 8, "5 Runas; fusiona dos.", "absorb-3");
            return new SkillTreeData { Branch = BranchType.Distance, Branches = branches, Nodes = nodes };
        }

        // ====================================================================
        // CUERPO A CUERPO — Solbrand, Filo del Alba (6 sub-ramas × 5 = 30)
        // ====================================================================
        private static SkillTreeData BuildMeleeTree()
        {
            var branches = new List<SkillBranchData>
            {
                new() { Id = "blade",  Name = "Génesis de Hoja",   Icon = "⚔", Description = "Onda, rayo, remolino, estocada.", Capstone = false },
                new() { Id = "combo",  Name = "Maestría de Combo", Icon = "🔥", Description = "Medidor de combo, remates.", Capstone = false },
                new() { Id = "solar",  Name = "Ira Solar",          Icon = "☀", Description = "Quemadura, eclipse, corona.", Capstone = false },
                new() { Id = "aegis",  Name = "Égida del Alba",    Icon = "🛡", Description = "Parry, reflexión, dash.", Capstone = false },
                new() { Id = "weight", Name = "Peso de Estrellas", Icon = "★", Description = "Golpes pesados, ondas de choque.", Capstone = false },
                new() { Id = "absorb", Name = "Absorción de Lore", Icon = "⭐", Description = "Capstone: memoriza armas melee.", Capstone = true },
            };
            var nodes = new List<SkillNodeData>();
            int idx = 0;
            AddBranch(nodes, branches[0], idx++, "Onda de corte", NodeRarity.Common, 1, "Onda frontal 6 tiles.", null, 0);
            AddBranch(nodes, branches[0], idx++, "Corte de rayo", NodeRarity.Rare, 1, "Cargado: rayo 12 tiles.", "blade-0");
            AddBranch(nodes, branches[0], idx++, "Remolino", NodeRarity.Rare, 1, "Alt-fuego: gira golpeando.", "blade-1");
            AddBranch(nodes, branches[0], idx++, "Combo de estocada", NodeRarity.Rare, 1, "3 golpes: estocada perforante.", "blade-2");
            AddBranch(nodes, branches[0], idx++, "Corte de realidad", NodeRarity.Legendary, 3, "Cargado corta terreno+enemigos.", "blade-3");
            AddBranch(nodes, branches[1], idx++, "Medidor de combo", NodeRarity.Common, 1, "Golpes consecutivos: combo máx 10.", null, 1);
            AddBranch(nodes, branches[1], idx++, "Remate", NodeRarity.Rare, 1, "Combo 10: +200% daño.", "combo-0");
            AddBranch(nodes, branches[1], idx++, "Impulso", NodeRarity.Rare, 1, "Combo no decae al moverse.", "combo-1");
            AddBranch(nodes, branches[1], idx++, "Parry-Riposte", NodeRarity.Rare, 1, "Bloqueo+contra: resetea combo.", "combo-2");
            AddBranch(nodes, branches[1], idx++, "Filo infinito", NodeRarity.Legendary, 4, "Sin tope; +5%/combo.", "combo-3");
            AddBranch(nodes, branches[2], idx++, "Corte de destello solar", NodeRarity.Rare, 1, "Golpes incendian.", null, 2);
            AddBranch(nodes, branches[2], idx++, "Corte de eclipse", NodeRarity.Rare, 1, "Oscurece pantalla +50% daño.", "solar-0");
            AddBranch(nodes, branches[2], idx++, "Aura de corona", NodeRarity.Rare, 1, "Aura quemadura 3 tiles.", "solar-1");
            AddBranch(nodes, branches[2], idx++, "Ignición solar", NodeRarity.Rare, 1, "Quemados explotan al morir.", "solar-2");
            AddBranch(nodes, branches[2], idx++, "Golpe de supernova", NodeRarity.Legendary, 5, "Remate: supernova 10 tiles.", "solar-3");
            AddBranch(nodes, branches[3], idx++, "Frames de parry", NodeRarity.Common, 1, "Primeros 4 frames: invuln.", null, 3);
            AddBranch(nodes, branches[3], idx++, "Reflexión de daño", NodeRarity.Rare, 1, "Parry: rebota +50% daño.", "aegis-0");
            AddBranch(nodes, branches[3], idx++, "Dash del alba", NodeRarity.Rare, 1, "Dash con i-frames (3s cd).", "aegis-1");
            AddBranch(nodes, branches[3], idx++, "Baluarte", NodeRarity.Rare, 1, "Quieto 1s: +20 defensa.", "aegis-2");
            AddBranch(nodes, branches[3], idx++, "Guardia eterna", NodeRarity.Legendary, 4, "Parry x2; refleja melee.", "aegis-3");
            AddBranch(nodes, branches[4], idx++, "Golpes pesados", NodeRarity.Common, 1, "+50% knockback.", null, 4);
            AddBranch(nodes, branches[4], idx++, "Golpe al suelo", NodeRarity.Rare, 1, "Onda de choque.", "weight-0");
            AddBranch(nodes, branches[4], idx++, "Cráter", NodeRarity.Rare, 1, "Cráter dañino 5s.", "weight-1");
            AddBranch(nodes, branches[4], idx++, "Caída de meteorito", NodeRarity.Rare, 1, "Aéreo: meteoros.", "weight-2");
            AddBranch(nodes, branches[4], idx++, "Pozo de gravedad", NodeRarity.Legendary, 5, "Golpe atrae enemigos.", "weight-3");
            AddBranch(nodes, branches[5], idx++, "Códex de memoria I", NodeRarity.Rare, 1, "Slot de Runa 1.", null, 5);
            AddBranch(nodes, branches[5], idx++, "Códex de memoria II", NodeRarity.Rare, 1, "Slot de Runa 2.", "absorb-0");
            AddBranch(nodes, branches[5], idx++, "Códex de memoria III", NodeRarity.Rare, 1, "Slot de Runa 3.", "absorb-1");
            AddBranch(nodes, branches[5], idx++, "Afinación de resonancia", NodeRarity.Legendary, 5, "Memoriza arma melee.", "absorb-2");
            AddBranch(nodes, branches[5], idx++, "Alma del maestro de hojas", NodeRarity.Legendary, 8, "5 Runas; fusiona golpes.", "absorb-3");
            return new SkillTreeData { Branch = BranchType.Melee, Branches = branches, Nodes = nodes };
        }

        // ====================================================================
        // ARTES MÁGICAS — Grimorio del Eterno (7 sub-ramas × 5 = 35)
        // ====================================================================
        private static SkillTreeData BuildMagicTree()
        {
            var branches = new List<SkillBranchData>
            {
                new() { Id = "mana",    Name = "Flujo de Maná",           Icon = "💧", Description = "Maná máximo, regen, coste.", Capstone = false },
                new() { Id = "element", Name = "Génesis Elemental",      Icon = "🔥", Description = "Fuego, escarcha, tormenta, vacío.", Capstone = false },
                new() { Id = "proj",    Name = "Evolución de Proyectiles",Icon = "✺", Description = "Split, homing, chain, familiar.", Capstone = false },
                new() { Id = "convert", Name = "Conversión Arcana",       Icon = "☯", Description = "Maná↔HP, lifesteal, desesperación.", Capstone = false },
                new() { Id = "cosmic",  Name = "Hechizos Cósmicos",       Icon = "🌌", Description = "Agujero negro, supernova, dilatación.", Capstone = false },
                new() { Id = "summon",   Name = "Maestría de Invocación", Icon = "🐉", Description = "Minions del fragmento.", Capstone = false },
                new() { Id = "absorb",   Name = "Absorción de Lore",       Icon = "⭐", Description = "Capstone: memoriza armas mágicas+invoc.", Capstone = true },
            };
            var nodes = new List<SkillNodeData>();
            int idx = 0;
            AddBranch(nodes, branches[0], idx++, "Reserva de maná", NodeRarity.Common, 1, "+40 maná máximo.", null, 0);
            AddBranch(nodes, branches[0], idx++, "Maná fluyente", NodeRarity.Rare, 1, "Regen +50%.", "mana-0");
            AddBranch(nodes, branches[0], idx++, "Lanzamiento eficiente", NodeRarity.Rare, 1, "-25% coste maná.", "mana-1");
            AddBranch(nodes, branches[0], idx++, "Pozo de maná", NodeRarity.Rare, 1, "Quieto: +5 maná/seg.", "mana-2");
            AddBranch(nodes, branches[0], idx++, "Reserva inagotable", NodeRarity.Legendary, 3, "Lanzar con <20 maná gratis.", "mana-3");
            AddBranch(nodes, branches[1], idx++, "Bolt de fuego", NodeRarity.Common, 1, "Bolts incendian.", null, 1);
            AddBranch(nodes, branches[1], idx++, "Esquirla de escarcha", NodeRarity.Rare, 1, "Ralentiza 40%.", "element-0");
            AddBranch(nodes, branches[1], idx++, "Arco de tormenta", NodeRarity.Rare, 1, "Salta a 2 enemigos.", "element-1");
            AddBranch(nodes, branches[1], idx++, "Virote de vacío", NodeRarity.Rare, 1, "Perfora 3, +30% daño.", "element-2");
            AddBranch(nodes, branches[1], idx++, "Convergencia elemental", NodeRarity.Legendary, 4, "Rota elementos; todos a la vez.", "element-3");
            AddBranch(nodes, branches[2], idx++, "Bolt dividido", NodeRarity.Common, 1, "Se divide en 2 al impactar.", null, 2);
            AddBranch(nodes, branches[2], idx++, "Chispa guiada", NodeRarity.Rare, 1, "Homing al más cercano.", "proj-0");
            AddBranch(nodes, branches[2], idx++, "Cadena de lanzamiento", NodeRarity.Rare, 1, "Salta entre 3 enemigos.", "proj-1");
            AddBranch(nodes, branches[2], idx++, "Orbe familiar", NodeRarity.Rare, 1, "Familiar orbital (3 bolts/seg).", "proj-2");
            AddBranch(nodes, branches[2], idx++, "Multilanzamiento", NodeRarity.Legendary, 5, "+3 bolts gratis por lanzamiento.", "proj-3");
            AddBranch(nodes, branches[3], idx++, "Escudo de maná", NodeRarity.Common, 1, "Daño drena maná antes que HP.", null, 3);
            AddBranch(nodes, branches[3], idx++, "Maná→HP", NodeRarity.Rare, 1, "Lanzar cura 2 HP/10 maná.", "convert-0");
            AddBranch(nodes, branches[3], idx++, "HP→Maná", NodeRarity.Rare, 1, "5 HP → 30 maná.", "convert-1");
            AddBranch(nodes, branches[3], idx++, "Desesperación", NodeRarity.Rare, 1, "Daño escala con maná faltante.", "convert-2");
            AddBranch(nodes, branches[3], idx++, "Ciclo eterno", NodeRarity.Legendary, 4, "Matar: 30% maná + 10% HP.", "convert-3");
            AddBranch(nodes, branches[4], idx++, "Agujero negro", NodeRarity.Rare, 1, "Cargado: pozo gravitacional 4s.", null, 4);
            AddBranch(nodes, branches[4], idx++, "Supernova", NodeRarity.Rare, 1, "Cargado: explosión 10 tiles.", "cosmic-0");
            AddBranch(nodes, branches[4], idx++, "Dilatación temporal", NodeRarity.Rare, 1, "Cargado: -60% velocidad enemigos.", "cosmic-1");
            AddBranch(nodes, branches[4], idx++, "Lluvia de estrellas", NodeRarity.Rare, 1, "Estrella cae cada 3s.", "cosmic-2");
            AddBranch(nodes, branches[4], idx++, "Desgarro de realidad", NodeRarity.Legendary, 6, "Portal: bolts de 2º portal.", "cosmic-3");
            AddBranch(nodes, branches[5], idx++, "Minion base", NodeRarity.Common, 1, "Invoca minion estrella orbital.", null, 5);
            AddBranch(nodes, branches[5], idx++, "Minion +1", NodeRarity.Rare, 1, "+1 slot de minion.", "summon-0");
            AddBranch(nodes, branches[5], idx++, "Minion empoderado", NodeRarity.Rare, 1, "+10% daño del grimorio.", "summon-1");
            AddBranch(nodes, branches[5], idx++, "Minion torre", NodeRarity.Rare, 1, "Minions en modo defensivo.", "summon-2");
            AddBranch(nodes, branches[5], idx++, "Enjambre estelar", NodeRarity.Legendary, 5, "+3 slots; minions disparan.", "summon-3");
            AddBranch(nodes, branches[6], idx++, "Códex de memoria I", NodeRarity.Rare, 1, "Slot de Runa 1.", null, 6);
            AddBranch(nodes, branches[6], idx++, "Códex de memoria II", NodeRarity.Rare, 1, "Slot de Runa 2.", "absorb-0");
            AddBranch(nodes, branches[6], idx++, "Códex de memoria III", NodeRarity.Rare, 1, "Slot de Runa 3.", "absorb-1");
            AddBranch(nodes, branches[6], idx++, "Afinación de resonancia", NodeRarity.Legendary, 5, "Memoriza arma mágica/invoc.", "absorb-2");
            AddBranch(nodes, branches[6], idx++, "Grimorio omnisciente", NodeRarity.Legendary, 8, "5 Runas; tormenta simultanea.", "absorb-3");
            return new SkillTreeData { Branch = BranchType.Magic, Branches = branches, Nodes = nodes };
        }

        // Helper para añadir un nodo a una sub-rama con coords polares.
        private static void AddBranch(
            List<SkillNodeData> nodes,
            SkillBranchData branch,
            int indexInBranch,
            string name,
            NodeRarity rarity,
            int cost,
            string effect,
            string? prereq,
            int branchIndex)
        {
            int totalBranches = 7; // max para magic; los demás se acomodan
            float angle = (branchIndex / (float)totalBranches) * System.MathF.PI * 2 - System.MathF.PI / 2;
            float r = 0.18f + indexInBranch * 0.13f;
            float jitter = (branchIndex % 2 == 0 ? 0.04f : -0.04f);
            float x = 0.5f + System.MathF.Cos(angle + jitter * indexInBranch) * r * 1.15f;
            float y = 0.5f + System.MathF.Sin(angle + jitter * indexInBranch) * r * 1.0f;
            nodes.Add(new SkillNodeData
            {
                Id = $"{branch.Id}-{indexInBranch}",
                Name = name,
                Branch = branch.Name,
                Rarity = rarity,
                Cost = cost,
                Effect = effect,
                PrereqId = prereq,
                X = x,
                Y = y,
            });
        }
    }
}
