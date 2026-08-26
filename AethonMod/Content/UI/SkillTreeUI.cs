using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// Arbol de habilidades como PANTALLA COMPLETA (estilo mapa de Terraria).
    /// Se abre con K, pausa el juego, y se cierra con K o Esc.
    /// Todo se dibuja directamente en DrawFullscreen().
    /// </summary>
    public class SkillTreeUIState : UIState
    {
        public bool IsVisible;
        public PoESkillTree? CurrentTree;

        private float _zoom = 1f;
        private Vector2 _panOffset = Vector2.Zero;
        private bool _isDragging = false;
        private Vector2 _dragStart;
        private int _lastScrollValue = 0;
        private bool _mouseLeftPressed = false;

        private List<PoESkillNode> _allNodes = new();
        private List<(int fromIdx, int toIdx)> _allConnections = new();
        private Dictionary<string, int> _nodeIndex = new();
        private float _minX, _maxX, _minY, _maxY, _rangeX, _rangeY;

        public override void OnInitialize()
        {
        }

        public void BuildPoETree(PoESkillTree tree)
        {
            CurrentTree = tree;
            _allNodes = tree.Nodes;
            _allConnections.Clear();
            _nodeIndex.Clear();
            for (int i = 0; i < _allNodes.Count; i++)
                _nodeIndex[_allNodes[i].Id] = i;
            foreach (var node in _allNodes)
            {
                int fromIdx = _nodeIndex[node.Id];
                foreach (var connId in node.Connections)
                {
                    if (_nodeIndex.TryGetValue(connId, out int j))
                        _allConnections.Add((fromIdx, j));
                }
            }
            _minX = float.MaxValue; _maxX = float.MinValue;
            _minY = float.MaxValue; _maxY = float.MinValue;
            foreach (var node in _allNodes)
            {
                _minX = System.Math.Min(_minX, node.X);
                _maxX = System.Math.Max(_maxX, node.X);
                _minY = System.Math.Min(_minY, node.Y);
                _maxY = System.Math.Max(_maxY, node.Y);
            }
            _rangeX = _maxX - _minX + 1;
            _rangeY = _maxY - _minY + 1;
        }

        public void Show()
        {
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted) return;
            var tree = PoETreeCatalog.GetTree(sp.ActiveBranch);
            BuildPoETree(tree);
            _zoom = 1f;
            _panOffset = Vector2.Zero;
            IsVisible = true;
        }

        public void Hide()
        {
            IsVisible = false;
        }

        private Vector2 NodeToScreen(PoESkillNode node)
        {
            float cx = Main.screenWidth / 2f;
            float cy = Main.screenHeight / 2f;
            float scale = System.Math.Min(Main.screenWidth / _rangeX, Main.screenHeight / _rangeY) * 0.5f * _zoom;
            float x = (node.X - _minX) * scale - (_rangeX * scale / 2f) + cx + _panOffset.X;
            float y = (node.Y - _minY) * scale - (_rangeY * scale / 2f) + cy + _panOffset.Y;
            return new Vector2(x, y);
        }

        private PoESkillNode? FindHoveredNode()
        {
            Vector2 mouse = new Vector2(Main.mouseX, Main.mouseY);
            PoESkillNode? closest = null;
            float closestDist = float.MaxValue;
            foreach (var node in _allNodes)
            {
                Vector2 pos = NodeToScreen(node);
                float dist = Vector2.Distance(pos, mouse);
                float radius = node.Radius * 2.2f * _zoom + 8f;
                if (dist < radius && dist < closestDist)
                {
                    closestDist = dist;
                    closest = node;
                }
            }
            return closest;
        }

        /// <summary>
        /// Dibuja el arbol a pantalla completa. Llamado directamente desde UISystem.
        /// </summary>
        public void DrawFullscreen(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null || CurrentTree == null) return;

            // --- FONDO ---
            // Pantalla completa negra
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                new Color(6, 4, 14, 250));

            // Estrellas
            for (int i = 0; i < 120; i++)
            {
                int seed = i * 73856093;
                float baseX = (seed % Main.screenWidth);
                float baseY = ((seed * 19349663) % Main.screenHeight);
                float sx = (baseX + _panOffset.X * 0.2f) % Main.screenWidth;
                float sy = (baseY + _panOffset.Y * 0.2f) % Main.screenHeight;
                if (sx < 0) sx += Main.screenWidth;
                if (sy < 0) sy += Main.screenHeight;
                int bright = 30 + ((seed * 83492791) % 40);
                int sz = 1 + ((seed * 1299721) % 2);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)sx, (int)sy, sz, sz),
                    new Color(bright, bright, bright + 10, bright + 15));
            }

            // Vignette radial
            for (int r = 350; r > 0; r -= 25)
            {
                int alpha = (350 - r) / 12;
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(Main.screenWidth / 2 - r, Main.screenHeight / 2 - r, r * 2, r * 2),
                    new Color(20, 12, 40, alpha));
            }

            // --- CONEXIONES ---
            foreach (var (fromIdx, toIdx) in _allConnections)
            {
                var fromNode = _allNodes[fromIdx];
                var toNode = _allNodes[toIdx];
                Vector2 from = NodeToScreen(fromNode);
                Vector2 to = NodeToScreen(toNode);
                bool bothActive = sp.AllocatedNodes.Contains(fromNode.Id) && sp.AllocatedNodes.Contains(toNode.Id);
                Color lineColor = bothActive ? new Color(245, 196, 81, 220) : new Color(50, 45, 75, 70);
                float thickness = bothActive ? 3f : 1.5f;
                DrawLine(spriteBatch, from, to, lineColor, thickness);
            }

            // --- NODOS ---
            var hoveredNode = FindHoveredNode();
            foreach (var node in _allNodes)
            {
                Vector2 pos = NodeToScreen(node);
                bool allocated = sp.AllocatedNodes.Contains(node.Id);
                bool canAllocate = false;
                if (!allocated)
                {
                    if (node.Id == "start" || node.Cost == 0) canAllocate = true;
                    else foreach (var aid in sp.AllocatedNodes)
                        if (node.Connections.Contains(aid)) { canAllocate = true; break; }
                }
                bool isHovered = hoveredNode != null && hoveredNode.Id == node.Id;
                DrawNode(spriteBatch, pos, node, allocated, canAllocate, isHovered);
            }

            // --- INPUT ---
            // Pan con click derecho
            if (Main.mouseRight)
            {
                if (!_isDragging)
                {
                    _isDragging = true;
                    _dragStart = new Vector2(Main.mouseX, Main.mouseY);
                }
                else
                {
                    _panOffset += new Vector2(Main.mouseX, Main.mouseY) - _dragStart;
                    _dragStart = new Vector2(Main.mouseX, Main.mouseY);
                }
            }
            else _isDragging = false;

            // Zoom con rueda (usar Main.scrollDelta que es el delta acumulado de scroll)
            // En tModLoader/FNA, el scroll se maneja via Main.mouseState.ScrollValue
            // Simplificado: usar un campo estatico que UISystem actualiza
            // Por ahora, sin zoom (el pan con click derecho funciona)
            // TODO: implementar zoom cuando encontremos la API correcta de scroll

            // Click izquierdo para asignar
            if (Main.mouseLeft && !_mouseLeftPressed && hoveredNode != null && !_isDragging)
            {
                _mouseLeftPressed = true;
                bool allocated = sp.AllocatedNodes.Contains(hoveredNode.Id);
                if (allocated)
                {
                    sp.AllocatedNodes.Remove(hoveredNode.Id);
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
                }
                else
                {
                    bool canAlloc = false;
                    if (hoveredNode.Id == "start" || hoveredNode.Cost == 0) canAlloc = true;
                    else foreach (var aid in sp.AllocatedNodes)
                        if (hoveredNode.Connections.Contains(aid)) { canAlloc = true; break; }
                    if (canAlloc)
                    {
                        sp.AllocatedNodes.Add(hoveredNode.Id);
                        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
                    }
                }
            }
            if (!Main.mouseLeft) _mouseLeftPressed = false;

            // --- TOOLTIP ---
            if (hoveredNode != null)
            {
                string typeStr = hoveredNode.Type switch
                {
                    NodeType.Small => "[Small]",
                    NodeType.Notable => "[Notable]",
                    NodeType.Keystone => "[KEYSTONE]",
                    NodeType.Ascendancy => "[Ascendancy]",
                    _ => "",
                };
                bool alloc = sp.AllocatedNodes.Contains(hoveredNode.Id);
                string status = alloc ? "(Asignado)" : "(Disponible)";
                Main.instance.MouseText($"{typeStr} {hoveredNode.Name}\n{hoveredNode.Effect}\nCoste: {hoveredNode.Cost} pts {status}");
            }

            // --- UI OVERLAY ---
            int total = sp.CumulativeSkillPoints();
            int spent = sp.AllocatedNodes.Count;
            int available = total - spent;

            // Barra superior
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(0, 0, Main.screenWidth, 50),
                new Color(10, 8, 20, 230));
            Utils.DrawBorderString(spriteBatch, "ARBOL DE HABILIDADES",
                new Vector2(Main.screenWidth / 2f, 15), new Color(245, 196, 81), 1.2f, 0.5f, 0.5f);
            Utils.DrawBorderString(spriteBatch, $"Puntos: {available} | Asignados: {spent}/{total}",
                new Vector2(Main.screenWidth / 2f, 35), new Color(179, 136, 255), 0.9f, 0.5f, 0.5f);

            // Ayuda abajo
            Utils.DrawBorderString(spriteBatch,
                "Click izq: asignar | Click der: mover | Rueda: zoom | K/Esc: cerrar",
                new Vector2(Main.screenWidth / 2f, Main.screenHeight - 15),
                new Color(100, 95, 120), 0.8f, 0.5f, 0.5f);

            // Cerrar con Escape
            if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                Hide();
        }

        private void DrawNode(SpriteBatch sb, Vector2 pos, PoESkillNode node,
            bool allocated, bool canAllocate, bool hovered)
        {
            int radius = (int)(node.Radius * 2.0f * _zoom);
            radius = System.Math.Max(6, radius);

            Color baseColor = node.Type switch
            {
                NodeType.Small => new Color(90, 90, 120),
                NodeType.Notable => new Color(60, 120, 210),
                NodeType.Keystone => new Color(245, 196, 81),
                NodeType.Ascendancy => new Color(150, 60, 230),
                _ => Color.White,
            };

            if (!allocated && !canAllocate) baseColor *= 0.12f;

            // Glow si asignado
            if (allocated)
                for (int i = 3; i > 0; i--)
                {
                    int gr = radius + i * 5;
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(245, 196, 81, 10 - i * 2));
                }

            // Glow hover
            if (hovered && (canAllocate || allocated))
                for (int i = 2; i > 0; i--)
                {
                    int gr = radius + i * 4;
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(255, 255, 255, 6 - i * 2));
                }

            // Relleno
            Color fill = allocated ? new Color(255, 225, 140) : (canAllocate ? baseColor : baseColor * 0.08f);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)pos.X - radius, (int)pos.Y - radius, radius * 2, radius * 2), fill);

            // Borde
            Color border = allocated ? new Color(255, 240, 190) : hovered ? Color.White : new Color(baseColor.R + 30, baseColor.G + 30, baseColor.B + 30);
            int b = 2;
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)pos.X - radius, (int)pos.Y - radius, radius * 2, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)pos.X - radius, (int)pos.Y + radius - b, radius * 2, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)pos.X - radius, (int)pos.Y - radius, b, radius * 2), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)pos.X + radius - b, (int)pos.Y - radius, b, radius * 2), border);

            // Punto interior small
            if (node.Type == NodeType.Small)
            {
                int dr = radius / 3;
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)pos.X - dr, (int)pos.Y - dr, dr * 2, dr * 2),
                    allocated ? new Color(255, 245, 200) : new Color(baseColor.R + 50, baseColor.G + 50, baseColor.B + 50));
            }

            // Costo
            if (node.Cost > 0)
                Utils.DrawBorderString(sb, node.Cost.ToString(),
                    new Vector2(pos.X, pos.Y + radius + 5),
                    allocated ? new Color(255, 225, 140) : new Color(120, 110, 140), 0.65f, 0.5f, 0f);
        }

        private void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)System.Math.Atan2(edge.Y, edge.X);
            float length = edge.Length();
            if (length < 1f) return;
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)start.X, (int)start.Y, (int)length, (int)thickness),
                null, color, angle, new Vector2(0, thickness / 2f), SpriteEffects.None, 0);
        }
    }
}
