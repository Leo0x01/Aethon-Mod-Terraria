using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using AethonMod.Content.Players;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// TIPOS DE NODOS (estilo Path of Exile):
    /// - Small: bono menor (ej: +5 vida, +2 daño)
    /// - Notable: bono mediano potente (ej: +15% daño de fuego, +10% velocidad)
    /// - Keystone: cambia la build completamente (ej: lifesteal, regen de mana)
    /// - Cluster: grupo de small+notable alrededor de un nodo central
    /// - Ascendancy: potenciadores procedurales post-nivel 100
    /// </summary>
    public enum NodeType { Small, Notable, Keystone, Cluster, Ascendancy }

    /// <summary>
    /// Nodo del árbol de habilidades estilo PoE.
    /// En PoE, los nodos forman un GRAFO DIRIGIDO (no radial).
    /// Cada nodo tiene conexiones (aristas) a otros nodos.
    /// El jugador debe seguir caminos desde su punto de partida.
    /// </summary>
    public class PoESkillNode
    {
        public string Id;
        public string Name;
        public string Branch;       // sub-rama temática
        public NodeType Type;
        public int Cost;             // puntos de habilidad
        public string Effect;        // descripción del efecto
        public List<string> Connections = new(); // IDs de nodos adyacentes
        // Posición en el canvas (coordenadas absolutas, no normalizadas)
        public float X;
        public float Y;
        // Tamaño visual según tipo
        public float Radius => Type switch
        {
            NodeType.Small => 8f,
            NodeType.Notable => 14f,
            NodeType.Keystone => 22f,
            NodeType.Cluster => 12f,
            NodeType.Ascendancy => 18f,
            _ => 8f,
        };
    }

    /// <summary>
    /// Sub-rama temática del árbol (cluster de PoE).
    /// Cada cluster tiene un grupo de small nodes + 1 notable + opcionalmente 1 keystone.
    /// </summary>
    public class PoECluster
    {
        public string Id;
        public string Name;
        public string Icon;
        public string Description;
        public bool HasKeystone;
    }

    /// <summary>
    /// Árbol de habilidades estilo Path of Exile.
    /// Estructura: grafo dirigido con:
    /// - Punto de partida central (uno por rama)
    /// - Clusters temáticos radiando desde el centro
    /// - Cada cluster: 3-5 small nodes → 1 notable → opcional keystone
    /// - Caminos entre clusters
    /// - Zona de Ascendancy (potenciadores procedurales) post-nivel 100
    /// </summary>
    public class PoESkillTree
    {
        public BranchType Branch;
        public List<PoECluster> Clusters = new();
        public List<PoESkillNode> Nodes = new();
        public string StartNodeId; // nodo inicial (punto de partida)
    }

    /// <summary>
    /// Catalogo de arboles de habilidades estilo PoE.
    /// Cada rama tiene un arbol extenso y complejo.
    /// Los arboles se construyen una sola vez y se cachean (son read-only).
    /// </summary>
    public static class PoETreeCatalog
    {
        // Cache de arboles construidos (son read-only despues de Build)
        private static PoESkillTree? _distanceTree;
        private static PoESkillTree? _meleeTree;
        private static PoESkillTree? _magicTree;
        private static readonly object _lock = new();

        public static PoESkillTree GetTree(BranchType branch)
        {
            lock (_lock)
            {
                return branch switch
                {
                    BranchType.Distance => _distanceTree ??= BuildDistanceTree(),
                    BranchType.Melee => _meleeTree ??= BuildMeleeTree(),
                    BranchType.Magic => _magicTree ??= BuildMagicTree(),
                    _ => new PoESkillTree { Branch = branch },
                };
            }
        }

        // ====================================================================
        // DISTANCIA — Lumina, la Arcoestelar
        // 12 clusters + zona de Ascendancy
        // ====================================================================
        private static PoESkillTree BuildDistanceTree()
        {
            var tree = new PoESkillTree { Branch = BranchType.Distance };
            float cx = 4000f, cy = 3000f; // centro del canvas

            // Nodo inicial central
            var start = new PoESkillNode
            {
                Id = "start", Name = "Fragmento Génesis", Branch = "Núcleo",
                Type = NodeType.Keystone, Cost = 0,
                Effect = "Punto de partida. El fragmento despierta.",
                X = cx, Y = cy,
            };
            tree.StartNodeId = "start";
            tree.Nodes.Add(start);

            // 12 clusters temáticos (estilo PoE)
            var clusterDefs = new (string id, string name, string icon, string desc, bool keystone, float angle, float dist)[]
            {
                ("proj-power", "Poder de Proyectiles", "➶", "Daño y velocidad de proyectiles", true, 0f, 500f),
                ("quiver", "Maestría de Carcaj", "🎒", "Munición y multi-disparo", true, 30f, 480f),
                ("hunter", "Cazador", "◎", "Crítico y marca", true, 60f, 520f),
                ("celestial", "Disparos Celestiales", "✦", "Meteoros y supernovas", true, 90f, 550f),
                ("phantom", "Carcaj Fantasma", "◈", "Fase y ricochet", true, 120f, 490f),
                ("velocity", "Velocidad", "⚡", "Cadencia y movilidad", false, 150f, 450f),
                ("piercing", "Perforación", "→", "Perforar enemigos y terrain", true, 180f, 510f),
                ("elemental", "Daño Elemental", "🔥", "Fuego, hielo, veneno", true, 210f, 530f),
                ("range", "Alcance", "↔", "Distancia y área de efecto", false, 240f, 470f),
                ("ammo", "Munición Especial", "⊕", "Tipos de munición única", true, 270f, 500f),
                ("survival", "Supervivencia", "❤", "Vida y defensa del arquero", true, 300f, 460f),
                ("absorption", "Absorción de Lore", "⭐", "Capstone: memorizar armas", true, 330f, 600f),
            };

            foreach (var (id, name, icon, desc, keystone, angle, dist) in clusterDefs)
            {
                tree.Clusters.Add(new PoECluster
                {
                    Id = id, Name = name, Icon = icon, Description = desc, HasKeystone = keystone,
                });
                BuildCluster(tree, id, name, icon, desc, keystone, angle, dist, cx, cy, BranchType.Distance);
            }

            // Conexiones entre clusters adyacentes (caminos del grafo)
            for (int i = 0; i < clusterDefs.Length - 1; i++)
            {
                ConnectClusters(tree, clusterDefs[i].id, clusterDefs[i + 1].id);
            }
            // Conexión circular (último al primero)
            ConnectClusters(tree, clusterDefs[^1].id, clusterDefs[0].id);

            // === CONECTAR START A CADA CLUSTER (para que el arbol sea accesible) ===
            foreach (var (id, _, _, _, _, _, _) in clusterDefs)
            {
                ConnectBidirectionalTree(tree, "start", $"{id}-entry");
            }

            // Zona de Ascendancy (potenciadores procedurales post-100)
            BuildAscendancyZone(tree, cx, cy - 800f, BranchType.Distance);

            return tree;
        }

        // ====================================================================
        // CUERPO A CUERPO — Solbrand, Filo del Alba
        // ====================================================================
        private static PoESkillTree BuildMeleeTree()
        {
            var tree = new PoESkillTree { Branch = BranchType.Melee };
            float cx = 4000f, cy = 3000f;

            var start = new PoESkillNode
            {
                Id = "start", Name = "Fragmento Génesis", Branch = "Núcleo",
                Type = NodeType.Keystone, Cost = 0,
                Effect = "Punto de partida. El fragmento despierta.",
                X = cx, Y = cy,
            };
            tree.StartNodeId = "start";
            tree.Nodes.Add(start);

            var clusterDefs = new (string id, string name, string icon, string desc, bool keystone, float angle, float dist)[]
            {
                ("blade", "Génesis de Hoja", "⚔", "Tipos de corte y ondas", true, 0f, 500f),
                ("combo", "Combo", "🔥", "Medidor de combo y remates", true, 30f, 510f),
                ("solar", "Ira Solar", "☀", "Quemadura y eclipse", true, 60f, 520f),
                ("aegis", "Égida del Alba", "🛡", "Parry y defensa", true, 90f, 530f),
                ("weight", "Peso de Estrellas", "★", "Golpes pesados y ondas de choque", true, 120f, 490f),
                ("berserk", "Furia", "💢", "Velocidad de ataque y daño", true, 150f, 480f),
                ("vampire", "Vampírico", "🩸", "Lifesteal y regeneración", true, 180f, 550f),
                ("whirlwind", "Torbellino", "🌀", "AoE y ataques giratorios", true, 210f, 500f),
                ("thrust", "Estocada", "➹", "Alcance y perforación", false, 240f, 460f),
                ("ground", "Tierra", "⛰", "Slam y terremotos", true, 270f, 520f),
                ("survival", "Supervivencia", "❤", "Vida y defensa", true, 300f, 470f),
                ("absorption", "Absorción de Lore", "⭐", "Capstone: memorizar armas", true, 330f, 600f),
            };

            foreach (var (id, name, icon, desc, keystone, angle, dist) in clusterDefs)
            {
                tree.Clusters.Add(new PoECluster
                {
                    Id = id, Name = name, Icon = icon, Description = desc, HasKeystone = keystone,
                });
                BuildCluster(tree, id, name, icon, desc, keystone, angle, dist, cx, cy, BranchType.Melee);
            }

            for (int i = 0; i < clusterDefs.Length - 1; i++)
                ConnectClusters(tree, clusterDefs[i].id, clusterDefs[i + 1].id);
            ConnectClusters(tree, clusterDefs[^1].id, clusterDefs[0].id);

            // === CONECTAR START A CADA CLUSTER (para que el arbol sea accesible) ===
            foreach (var (id, _, _, _, _, _, _) in clusterDefs)
            {
                ConnectBidirectionalTree(tree, "start", $"{id}-entry");
            }

            BuildAscendancyZone(tree, cx, cy - 800f, BranchType.Melee);

            return tree;
        }

        // ====================================================================
        // ARTES MÁGICAS — Grimorio del Eterno
        // ====================================================================
        private static PoESkillTree BuildMagicTree()
        {
            var tree = new PoESkillTree { Branch = BranchType.Magic };
            float cx = 4000f, cy = 3000f;

            var start = new PoESkillNode
            {
                Id = "start", Name = "Fragmento Génesis", Branch = "Núcleo",
                Type = NodeType.Keystone, Cost = 0,
                Effect = "Punto de partida. El fragmento despierta.",
                X = cx, Y = cy,
            };
            tree.StartNodeId = "start";
            tree.Nodes.Add(start);

            var clusterDefs = new (string id, string name, string icon, string desc, bool keystone, float angle, float dist)[]
            {
                ("mana", "Flujo de Maná", "💧", "Maná máximo y regen", true, 0f, 500f),
                ("element", "Génesis Elemental", "🔥", "Fuego, hielo, tormenta", true, 25f, 510f),
                ("proj", "Evolución de Proyectiles", "✺", "Split, homing, chain", true, 50f, 520f),
                ("convert", "Conversión Arcana", "☯", "Maná↔HP, lifesteal", true, 75f, 530f),
                ("cosmic", "Hechizos Cósmicos", "🌌", "Agujero negro, supernova", true, 100f, 540f),
                ("summon", "Maestría de Invocación", "🐉", "Minions del fragmento", true, 125f, 500f),
                ("cast-speed", "Velocidad de Lanzamiento", "⚡", "Cadencia y costo", false, 150f, 470f),
                ("area", "Área de Efecto", "◉", "Radio y penetración", true, 175f, 490f),
                ("debuff", "Debilitamiento", "☠", "Veneno, maldiciones", true, 200f, 510f),
                ("barrier", "Barrera Arcana", "🛡", "Escudo de maná y defensa", true, 225f, 530f),
                ("survival", "Supervivencia", "❤", "Vida y regeneración", true, 250f, 480f),
                ("vampire", "Vampírico Arcano", "🩸", "Lifesteal mágico", true, 275f, 550f),
                ("critical", "Crítico Mágico", "✦", "Crítico y daño crítico", true, 300f, 500f),
                ("absorption", "Absorción de Lore", "⭐", "Capstone: memorizar armas", true, 330f, 600f),
            };

            foreach (var (id, name, icon, desc, keystone, angle, dist) in clusterDefs)
            {
                tree.Clusters.Add(new PoECluster
                {
                    Id = id, Name = name, Icon = icon, Description = desc, HasKeystone = keystone,
                });
                BuildCluster(tree, id, name, icon, desc, keystone, angle, dist, cx, cy, BranchType.Magic);
            }

            for (int i = 0; i < clusterDefs.Length - 1; i++)
                ConnectClusters(tree, clusterDefs[i].id, clusterDefs[i + 1].id);
            ConnectClusters(tree, clusterDefs[^1].id, clusterDefs[0].id);

            // === CONECTAR START A CADA CLUSTER (para que el arbol sea accesible) ===
            foreach (var (id, _, _, _, _, _, _) in clusterDefs)
            {
                ConnectBidirectionalTree(tree, "start", $"{id}-entry");
            }

            BuildAscendancyZone(tree, cx, cy - 800f, BranchType.Magic);

            return tree;
        }

        // ====================================================================
        // HELPER: Construir un cluster completo (estilo PoE)
        // Cada cluster tiene: 3-5 small nodes → 1 notable → opcional keystone
        // ====================================================================
        private static void BuildCluster(
            PoESkillTree tree, string clusterId, string clusterName, string icon,
            string desc, bool hasKeystone, float angleDeg, float dist,
            float cx, float cy, BranchType branch)
        {
            float rad = angleDeg * (float)Math.PI / 180f;
            float clusterX = cx + (float)Math.Cos(rad) * dist;
            float clusterY = cy + (float)Math.Sin(rad) * dist;

            // Helper local para añadir bidireccional (con null check)
            void ConnectBidirectional(string fromId, string toId)
            {
                var fromNode = tree.Nodes.Find(n => n.Id == fromId);
                var toNode = tree.Nodes.Find(n => n.Id == toId);
                if (fromNode != null && toNode != null)
                {
                    if (!fromNode.Connections.Contains(toId)) fromNode.Connections.Add(toId);
                    if (!toNode.Connections.Contains(fromId)) toNode.Connections.Add(fromId);
                }
            }

            // Nodo de entrada del cluster (conectado al camino principal)
            string entryId = $"{clusterId}-entry";
            var entryNode = new PoESkillNode
            {
                Id = entryId, Name = $"{clusterName} (Entrada)", Branch = clusterName,
                Type = NodeType.Small, Cost = 1,
                Effect = GetSmallEffect(clusterId, branch, 0),
                X = clusterX - 40f, Y = clusterY,
            };
            tree.Nodes.Add(entryNode);

            // 3 small nodes en línea
            string prevId = entryId;
            for (int i = 0; i < 3; i++)
            {
                string nodeId = $"{clusterId}-small-{i}";
                float nx = clusterX + (i - 1) * 40f;
                float ny = clusterY + 30f;
                var node = new PoESkillNode
                {
                    Id = nodeId, Name = $"{clusterName} (Nodo {i+1})", Branch = clusterName,
                    Type = NodeType.Small, Cost = 1,
                    Effect = GetSmallEffect(clusterId, branch, i + 1),
                    X = nx, Y = ny,
                };
                node.Connections.Add(prevId);
                ConnectBidirectional(prevId, nodeId);
                tree.Nodes.Add(node);
                prevId = nodeId;
            }

            // Notable del cluster
            string notableId = $"{clusterId}-notable";
            var notable = new PoESkillNode
            {
                Id = notableId, Name = GetNotableName(clusterId, branch),
                Branch = clusterName, Type = NodeType.Notable, Cost = 2,
                Effect = GetNotableEffect(clusterId, branch),
                X = clusterX + 20f, Y = clusterY + 80f,
            };
            notable.Connections.Add(prevId);
            ConnectBidirectional(prevId, notableId);
            tree.Nodes.Add(notable);

            // Keystone del cluster (opcional)
            if (hasKeystone)
            {
                string keystoneId = $"{clusterId}-keystone";
                var keystone = new PoESkillNode
                {
                    Id = keystoneId, Name = GetKeystoneName(clusterId, branch),
                    Branch = clusterName, Type = NodeType.Keystone, Cost = 3,
                    Effect = GetKeystoneEffect(clusterId, branch),
                    X = clusterX + 60f, Y = clusterY + 130f,
                };
                keystone.Connections.Add(notableId);
                ConnectBidirectional(notableId, keystoneId);
                tree.Nodes.Add(keystone);
            }
        }

        // Conectar dos clusters (camino entre ellos)
        private static void ConnectClusters(PoESkillTree tree, string fromCluster, string toCluster)
        {
            string fromId = $"{fromCluster}-entry";
            string toId = $"{toCluster}-entry";
            ConnectBidirectionalTree(tree, fromId, toId);
        }

        /// <summary>Conecta dos nodos bidireccionalmente (null-safe, evita duplicados).</summary>
        private static void ConnectBidirectionalTree(PoESkillTree tree, string fromId, string toId)
        {
            var from = tree.Nodes.Find(n => n.Id == fromId);
            var to = tree.Nodes.Find(n => n.Id == toId);
            if (from == null || to == null) return;
            if (!from.Connections.Contains(toId)) from.Connections.Add(toId);
            if (!to.Connections.Contains(fromId)) to.Connections.Add(fromId);
        }

        // ====================================================================
        // ZONA DE ASCENDANCY — Potenciadores procedurales post-nivel 100
        // ====================================================================
        private static void BuildAscendancyZone(PoESkillTree tree, float cx, float cy, BranchType branch)
        {
            // Nodo de entrada a la zona de Ascendancy
            string ascendEntryId = "ascend-entry";
            var ascendEntry = new PoESkillNode
            {
                Id = ascendEntryId, Name = "Ascendencia (Lv 100+)", Branch = "Ascendancy",
                Type = NodeType.Keystone, Cost = 0,
                Effect = "Desbloquea potenciadores procedurales. Genera nuevos nodos cada nivel.",
                X = cx, Y = cy,
            };
            tree.Nodes.Add(ascendEntry);
            // Conectar al nodo inicial (null-safe)
            ConnectBidirectionalTree(tree, "start", ascendEntryId);

            // 6 potenciadores procedurales base (se generan más al subir de nivel)
            string[] ascendNames = {
                "Potenciador Estelar I", "Potenciador Estelar II", "Potenciador Estelar III",
                "Potenciador Cósmico I", "Potenciador Cósmico II", "Potenciador Cósmico III",
            };
            string[] ascendEffects = GetAscendancyEffects(branch);

            string prevAscendId = ascendEntryId;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * (float)Math.PI / 180f;
                string nodeId = $"ascend-{i}";
                var node = new PoESkillNode
                {
                    Id = nodeId, Name = ascendNames[i], Branch = "Ascendancy",
                    Type = NodeType.Ascendancy, Cost = 5,
                    Effect = ascendEffects[i],
                    X = cx + (float)Math.Cos(angle) * 200f,
                    Y = cy + (float)Math.Sin(angle) * 200f,
                };
                node.Connections.Add(prevAscendId);
                ConnectBidirectionalTree(tree, prevAscendId, nodeId);
                tree.Nodes.Add(node);
                prevAscendId = nodeId;
            }

            // KEYSTONE DE REGENERACIÓN DE SALUD (nivel 100+)
            // Distancia y Melee: regenera 0.01% del daño causado como salud
            // Magia: igual pero las invocaciones NO cuentan
            string regenKeystoneId = "ascend-regen-keystone";
            string regenEffect = branch == BranchType.Magic
                ? "REGENERACIÓN VITAL: Regenera el 0.01% de tu salud máxima por cada punto de daño mágico causado. El daño de invocaciones NO cuenta para esta regeneración."
                : "REGENERACIÓN VITAL: Regenera el 0.01% de tu salud máxima por cada punto de daño causado al enemigo. Lifesteal pasivo permanente.";

            var regenKeystone = new PoESkillNode
            {
                Id = regenKeystoneId, Name = "Regeneración Vital (Keystone)",
                Branch = "Ascendancy", Type = NodeType.Keystone, Cost = 10,
                Effect = regenEffect,
                X = cx + 100f, Y = cy + 250f,
            };
            regenKeystone.Connections.Add(prevAscendId);
            ConnectBidirectionalTree(tree, prevAscendId, regenKeystoneId);
            tree.Nodes.Add(regenKeystone);

            // Potenciadores adicionales que se generan proceduralmente
            // (el sistema generará más nodos en runtime cuando el jugador suba de nivel post-100)
            for (int i = 6; i < 12; i++)
            {
                float angle = i * 60f * (float)Math.PI / 180f;
                string nodeId = $"ascend-proc-{i}";
                var node = new PoESkillNode
                {
                    Id = nodeId, Name = $"Potenciador Procedural {i-5}",
                    Branch = "Ascendancy", Type = NodeType.Ascendancy, Cost = 5,
                    Effect = GetProceduralEffect(branch, i),
                    X = cx + (float)Math.Cos(angle) * 300f,
                    Y = cy + (float)Math.Sin(angle) * 300f,
                };
                node.Connections.Add(prevAscendId);
                ConnectBidirectionalTree(tree, prevAscendId, nodeId);
                tree.Nodes.Add(node);
                prevAscendId = nodeId;
            }
        }

        // ====================================================================
        // GENERADORES DE EFECTOS
        // ====================================================================

        private static string GetSmallEffect(string cluster, BranchType branch, int index)
        {
            return (cluster, branch) switch
            {
                ("proj-power", _) => $"+{5 + index * 2}% daño de proyectiles",
                ("quiver", _) => $"+{3 + index}% velocidad de disparo",
                ("hunter", _) => $"+{4 + index * 2}% probabilidad de crítico",
                ("celestial", _) => $"+{5 + index * 3}% daño solar",
                ("phantom", _) => $"+{3 + index}% probabilidad de fase",
                ("velocity", _) => $"+{5 + index * 2}% velocidad de movimiento",
                ("piercing", _) => $"+1 perforación de enemigos",
                ("elemental", _) => $"+{5 + index * 3}% daño elemental",
                ("range", _) => $"+{10 + index * 5} tiles de alcance",
                ("ammo", _) => $"+{5 + index * 2}% probabilidad de no consumir munición",
                ("survival", _) => $"+{10 + index * 5} vida máxima",
                ("absorption", _) => $"+1 slot de runa de memoria",

                ("blade", _) => $"+{5 + index * 2}% daño de corte",
                ("combo", _) => $"+{3 + index}% velocidad de combo",
                ("solar", _) => $"+{5 + index * 3}% daño solar",
                ("aegis", _) => $"+{3 + index}% probabilidad de parry",
                ("weight", _) => $"+{10 + index * 5}% knockback",
                ("berserk", _) => $"+{5 + index * 2}% velocidad de ataque",
                ("vampire", _) => $"+{1 + index}% lifesteal",
                ("whirlwind", _) => $"+{5 + index * 3}% daño AoE",
                ("thrust", _) => $"+{5 + index * 2} tiles de alcance",
                ("ground", _) => $"+{5 + index * 3}% daño de slam",

                ("mana", _) => $"+{20 + index * 10} maná máximo",
                ("element", _) => $"+{5 + index * 3}% daño elemental",
                ("proj", _) => $"+{3 + index}% velocidad de proyectil mágico",
                ("convert", _) => $"+{2 + index}% eficiencia de conversión",
                ("cosmic", _) => $"+{5 + index * 3}% daño cósmico",
                ("summon", _) => $"+1 slot de minion",
                ("cast-speed", _) => $"+{3 + index}% velocidad de lanzamiento",
                ("area", _) => $"+{5 + index * 2} tiles de AoE",
                ("debuff", _) => $"+{5 + index * 3}% duración de debuffs",
                ("barrier", _) => $"+{10 + index * 5} escudo de maná",
                ("critical", _) => $"+{4 + index * 2}% crítico mágico",
                _ => $"+{5}% bono",
            };
        }

        private static string GetNotableName(string cluster, BranchType branch) =>
            cluster switch
            {
                "proj-power" => "Poder Letal de Proyectiles",
                "quiver" => "Carcaj Infinito",
                "hunter" => "Ojo de Halcón",
                "celestial" => "Lluvia de Meteoros",
                "phantom" => "Forma Etérea",
                "piercing" => "Perforación Total",
                "elemental" => "Maestría Elemental",
                "ammo" => "Munición Cósmica",
                "survival" => "Constitución del Arquero",
                "absorption" => "Códex de Memoria",
                "blade" => "Corte de Realidad",
                "combo" => "Filo Infinito",
                "solar" => "Corona Solar",
                "aegis" => "Guardia Eterna",
                "weight" => "Pozo de Gravedad",
                "berserk" => "Furia Sangrienta",
                "vampire" => "Sangre de Aethon",
                "whirlwind" => "Torbellino Estelar",
                "ground" => "Terremoto Cósmico",
                "mana" => "Reserva Inagotable",
                "element" => "Convergencia Elemental",
                "proj" => "Multilanzamiento",
                "convert" => "Ciclo Eterno",
                "cosmic" => "Desgarro de Realidad",
                "summon" => "Enjambre Estelar",
                "cast-speed" => "Velocidad Arcana",
                "area" => "Expansión Cósmica",
                "barrier" => "Barrera Absoluta",
                "critical" => "Toque Crítico",
                _ => "Notable",
            };

        private static string GetNotableEffect(string cluster, BranchType branch) =>
            cluster switch
            {
                "proj-power" => "+25% daño de proyectiles, +10% velocidad de proyectil",
                "quiver" => "No consume munición base, +1 proyectil por disparo",
                "hunter" => "+20% crítico, golpes marcan enemigos (+10% daño)",
                "celestial" => "Cada disparo cargado genera meteoros, +50% daño solar",
                "phantom" => "Proyectiles atraviesan terreno, +2 rebotes",
                "piercing" => "Proyectiles perforan 5 enemigos, +15% daño",
                "elemental" => "+30% daño de fuego/hielo/veneno",
                "ammo" => "Genera munición cósmica única, +20% daño de munición",
                "survival" => "+50 vida, +5 regen de vida/seg",
                "absorption" => "+2 slots de runa de memoria",
                "blade" => "Ondas de corte perforan terreno, +30% daño de corte",
                "combo" => "Combo sin tope, +5% daño por combo acumulado",
                "solar" => "Aura de quemadura 3 tiles, enemigos quemados explotan",
                "aegis" => "Parry x2 ventana, refleja proyectiles +50% daño",
                "weight" => "Golpes al suelo atraen enemigos, +50% knockback",
                "berserk" => "+30% velocidad de ataque cuando HP < 50%",
                "vampire" => "+5% lifesteal de todo daño cuerpo a cuerpo",
                "whirlwind" => "Remolino golpea 360°, +40% daño AoE",
                "ground" => "Slam genera terremoto 10 tiles, +35% daño de slam",
                "mana" => "+60 maná, lanzar con <20 maná es gratis",
                "element" => "Bolts rotan elementos, todos activos simultáneamente",
                "proj" => "+3 bolts por lanzamiento gratis",
                "convert" => "Matar restaura 30% maná + 10% HP",
                "cosmic" => "Cargado abre portal, bolts salen de 2º portal",
                "summon" => "+3 slots de minion, minions disparan bolts",
                "cast-speed" => "+25% velocidad de lanzamiento, -20% costo de maná",
                "area" => "+8 tiles de AoE, +20% daño de área",
                "barrier" => "Escudo de maná absorbe 50% del daño",
                "critical" => "+15% crítico mágico, +50% daño crítico",
                _ => "+15% bono general",
            };

        private static string GetKeystoneName(string cluster, BranchType branch) =>
            cluster switch
            {
                "proj-power" => "DESTRUCCIÓN TOTAL",
                "quiver" => "CARCAJ OMNISCIENTE",
                "hunter" => "MARCA LETAL",
                "celestial" => "SUPERNOVA",
                "phantom" => "EXISTENCIA ETÉREA",
                "piercing" => "PERFORACIÓN ABSOLUTA",
                "elemental" => "APOCALIPSIS ELEMENTAL",
                "ammo" => "MUNICIÓN INFINITA",
                "survival" => "INMORTALIDAD DEL ARQUERO",
                "absorption" => "ARSENAL DEL ETERNO",
                "blade" => "CORTE DE REALIDAD",
                "combo" => "COMBO INFINITO",
                "solar" => "EXPLOSIÓN SOLAR",
                "aegis" => "BULWARK ETERNO",
                "weight" => "POZO GRAVITACIONAL",
                "berserk" => "FURIA DE SANGRE",
                "vampire" => "SANGRE DE AETHON",
                "whirlwind" => "TORMENTA ESTELAR",
                "ground" => "CATACLISMO",
                "mana" => "MANA INFINITO",
                "element" => "CONVERGENCIA TOTAL",
                "proj" => "TORMENTA DE BOLTS",
                "convert" => "CICLO ETERNO",
                "cosmic" => "DESGARRO DE REALIDAD",
                "summon" => "ENJAMBRE ESTELAR",
                "cast-speed" => "VELOCIDAD ARCANA",
                "barrier" => "BARRERA ABSOLUTA",
                "critical" => "TOQUE DE LA MUERTE",
                _ => "KEYSTONE",
            };

        private static string GetKeystoneEffect(string cluster, BranchType branch) =>
            cluster switch
            {
                "proj-power" => "Cada proyectil genera 2 mini-estrellas al impactar. +50% daño total de proyectiles.",
                "quiver" => "Equipa 5 Runas simultáneamente. Fusiona dos tipos de munición en una.",
                "hunter" => "Matar enemigos marcados resetea todos los cooldowns. +40% crítico en enemigos marcados.",
                "celestial" => "Disparo cargado: supernova de 12 tiles. Ciegga + quema a todos en pantalla.",
                "phantom" => "Todos los proyectiles fasan terreno Y perforan 5 enemigos. Inmunidad a proyectiles enemigos.",
                "piercing" => "Proyectiles perforan TODOS los enemigos. +25% daño por cada enemigo perforado.",
                "elemental" => "Cada proyectil aplica TODOS los elementos simultáneamente. +40% daño total.",
                "ammo" => "Nunca se acaba la munición. Genera munición cósmica única cada 10s.",
                "survival" => "+100 vida. Inmune a veneno y fuego. 5% regen de vida/seg.",
                "absorption" => "Memoriza cualquier arma de distancia del juego base + mods. 5 Runas equipables.",
                "blade" => "Cortes atraviesan terreno y enemigos. Ondas de corte +100% daño.",
                "combo" => "Sin tope de combo. Cada combo acumulado: +5% daño y +2% velocidad de ataque.",
                "solar" => "Corte cargado: explosión solar 10 tiles. Enemigos quemados explotan en cadena.",
                "aegis" => "Parry window x3. Refleja proyectiles Y daño cuerpo a cuerpo. Invuln al parry.",
                "weight" => "Golpe al suelo: pozos gravitacionales múltiples. Atrae enemigos desde 20 tiles.",
                "berserk" => "Velocidad de ataque +50%. Daño +100% cuando HP < 30%. Sin penalización.",
                "vampire" => "LIFESTEAL PERMANENTE: +10% de todo daño causado se convierte en salud. Regen 5 HP/seg.",
                "whirlwind" => "Remolino continuo: golpea todo a 360° cada 0.5s. +60% daño AoE.",
                "ground" => "Slam: terremoto 15 tiles que dura 10s. Grietas que dañan enemigos que las cruzan.",
                "mana" => "+100 maná. Lanzar con <20 maná es gratis. Regen 10 maná/seg siempre.",
                "element" => "Cada lanzamiento aplica TODOS los elementos. +50% daño elemental total.",
                "proj" => "+5 bolts por lanzamiento gratis. Bolts se dividen al impactar 3 veces.",
                "convert" => "Matar: restaura 50% maná + 20% HP. Maná nunca baja de 10%.",
                "cosmic" => "Portal permanente: bolts salen de 2 portales simultáneamente. Agujero negro pasivo.",
                "summon" => "+5 slots de minion. Minions heredan +30% daño del grimorio. Minions disparan bolts.",
                "cast-speed" => "+50% velocidad de lanzamiento. -50% costo de maná. Sin cooldown entre hechizos.",
                "barrier" => "Escudo de maná absorbe 80% del daño. Regen de maná x2 cuando recibes daño.",
                "critical" => "+30% crítico mágico. +100% daño crítico. Cada crítico genera un bolt extra gratis.",
                _ => "Efecto poderoso.",
            };

        // Efectos de la zona de Ascendancy
        private static string[] GetAscendancyEffects(BranchType branch) =>
            branch switch
            {
                BranchType.Distance => new[]
                {
                    "+50% daño de proyectiles (Ascendancy)",
                    "Proyectiles buscan enemigos automáticamente (Ascendancy)",
                    "+3 proyectiles por disparo (Ascendancy)",
                    "Cada disparo genera una supernova menor (Ascendancy)",
                    "Proyectiles perforan TODO (Ascendancy)",
                    "+100% velocidad de disparo (Ascendancy)",
                },
                BranchType.Melee => new[]
                {
                    "+50% daño cuerpo a cuerpo (Ascendancy)",
                    "Cada golpe genera onda de choque (Ascendancy)",
                    "+3 combo por golpe (Ascendancy)",
                    "Cada golpe genera explosión solar (Ascendancy)",
                    "Parry automático cada 5s (Ascendancy)",
                    "+100% velocidad de ataque (Ascendancy)",
                },
                BranchType.Magic => new[]
                {
                    "+50% daño mágico (Ascendancy)",
                    "Bolts se dividen al impactar (Ascendancy)",
                    "+5 bolts por lanzamiento (Ascendancy)",
                    "Cada lanzamiento genera supernova (Ascendancy)",
                    "Maná infinito (Ascendancy)",
                    "+100% velocidad de lanzamiento (Ascendancy)",
                },
                _ => new[] { "Bono (Ascendancy)" },
            };

        private static string GetProceduralEffect(BranchType branch, int seed) =>
            branch switch
            {
                BranchType.Distance => (seed % 6) switch
                {
                    0 => "+15% daño de proyectiles (procedural)",
                    1 => "+10% velocidad de disparo (procedural)",
                    2 => "+5% crítico de proyectiles (procedural)",
                    3 => "+1 proyectil por disparo (procedural)",
                    4 => "+20% perforación (procedural)",
                    _ => "+25% daño solar (procedural)",
                },
                BranchType.Melee => (seed % 6) switch
                {
                    0 => "+15% daño cuerpo a cuerpo (procedural)",
                    1 => "+10% velocidad de ataque (procedural)",
                    2 => "+5% crítico cuerpo a cuerpo (procedural)",
                    3 => "+1 combo por golpe (procedural)",
                    4 => "+20% knockback (procedural)",
                    _ => "+25% daño solar (procedural)",
                },
                BranchType.Magic => (seed % 6) switch
                {
                    0 => "+15% daño mágico (procedural)",
                    1 => "+10% velocidad de lanzamiento (procedural)",
                    2 => "+5% crítico mágico (procedural)",
                    3 => "+1 bolt por lanzamiento (procedural)",
                    4 => "+20 maná máximo (procedural)",
                    _ => "+25% daño cósmico (procedural)",
                },
                _ => "+10% bono (procedural)",
            };
    }
}
