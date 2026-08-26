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
    /// Arbol de habilidades estilo Path of Exile.
    ///
    /// Caracteristicas visuales (basado en analisis de imagenes de PoE/PoE2/Grim Dawn):
    /// - Fondo: vacio oscuro con estrellas
    /// - Nodos: circulos con borde, icono de costo, colores por tipo
    /// - Lineas: rectas, finas grises (inactivas), doradas brillantes (activas)
    /// - Hover: glow blanco + tooltip
    /// - Click izquierdo: asignar/desasignar
    /// - Click derecho + arrastrar: pan
    /// - Rueda: zoom (usando Main.scrollWheelDelta)
    /// </summary>
    public class SkillTreeUIState : UIState
    {
        public bool IsVisible;
        public PoESkillTree? CurrentTree;

        private float _zoom = 1f;
        private Vector2 _panOffset = Vector2.Zero;
        private bool _isDragging = false;
        private Vector2 _dragStart;

        private UIText _titleText = null!;
        private UIText _pointsText = null!;
        private UITextPanel<string> _closeButton = null!;
        private UIPanel _bgPanel = null!;

        // Datos precalculados para render
        private List<PoESkillNode> _allNodes = new();
        private List<(int fromIdx, int toIdx)> _allConnections = new();
        private Dictionary<string, int> _nodeIndex = new();
        private float _minX, _maxX, _minY, _maxY, _rangeX, _rangeY;

        public override void OnInitialize()
        {
            // Panel de fondo (oscuro, estilo PoE)
            _bgPanel = new UIPanel();
            _bgPanel.Width.Set(0f, 1f);
            _bgPanel.Height.Set(0f, 1f);
            _bgPanel.BackgroundColor = new Color(8, 6, 16, 250);
            _bgPanel.BorderColor = Color.Transparent;
            Append(_bgPanel);

            _titleText = new UIText("Arbol de Habilidades", 1.2f);
            _titleText.HAlign = 0.5f;
            _titleText.Top.Set(8, 0f);
            _titleText.TextColor = new Color(245, 196, 81);
            _bgPanel.Append(_titleText);

            _pointsText = new UIText("Puntos: 0", 0.85f);
            _pointsText.HAlign = 0.5f;
            _pointsText.Top.Set(32, 0f);
            _pointsText.TextColor = new Color(179, 136, 255);
            _bgPanel.Append(_pointsText);

            _closeButton = new UITextPanel<string>("Cerrar (K)");
            _closeButton.Width.Set(100, 0f);
            _closeButton.Height.Set(26, 0f);
            _closeButton.HAlign = 1f;
            _closeButton.Top.Set(8, 0f);
            _closeButton.Left.Set(-8, 0f);
            _closeButton.BackgroundColor = new Color(40, 20, 30, 220);
            _closeButton.BorderColor = new Color(180, 80, 80, 150);
            _closeButton.OnLeftClick += (evt, el) => Hide();
            _bgPanel.Append(_closeButton);
        }

        public void BuildPoETree(PoESkillTree tree)
        {
            CurrentTree = tree;
            _allNodes = tree.Nodes;
            _allConnections.Clear();
            _nodeIndex.Clear();

            // Indexar nodos
            for (int i = 0; i < _allNodes.Count; i++)
                _nodeIndex[_allNodes[i].Id] = i;

            // Calcular conexiones
            for (int i = 0; i < _allNodes.Count; i++)
            {
                foreach (var connId in _allNodes[i].Connections)
                {
                    if (_nodeIndex.TryGetValue(connId, out int j))
                        _allConnections.Add((i, j));
                }
            }

            // Calcular limites
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

            UpdatePointsPoE();
        }

        public void UpdatePointsPoE()
        {
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null) return;
            int total = sp.CumulativeSkillPoints();
            int spent = sp.AllocatedNodes.Count;
            int available = total - spent;
            _pointsText.SetText($"Puntos: {available}    Asignados: {spent}/{total}");
        }

        public void Show()
        {
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted) return;
            var tree = PoETreeCatalog.GetTree(sp.ActiveBranch);
            BuildPoETree(tree);
            IsVisible = true;
        }

        public void Hide()
        {
            IsVisible = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!IsVisible) return;

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
                    Vector2 delta = new Vector2(Main.mouseX, Main.mouseY) - _dragStart;
                    _panOffset += delta;
                    _dragStart = new Vector2(Main.mouseX, Main.mouseY);
                }
            }
            else _isDragging = false;
        }

        // Transformar coords del nodo a coords de pantalla
        private Vector2 NodeToScreen(PoESkillNode node)
        {
            float cx = Main.screenWidth / 2f;
            float cy = Main.screenHeight / 2f;
            float scale = System.Math.Min(Main.screenWidth / _rangeX, Main.screenHeight / _rangeY) * 0.6f * _zoom;
            float x = (node.X - _minX) * scale - (_rangeX * scale / 2f) + cx + _panOffset.X;
            float y = (node.Y - _minY) * scale - (_rangeY * scale / 2f) + cy + _panOffset.Y;
            return new Vector2(x, y);
        }

        // Encontrar nodo bajo el cursor
        private PoESkillNode? FindHoveredNode()
        {
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null) return null;
            Vector2 mouse = new Vector2(Main.mouseX, Main.mouseY);
            PoESkillNode? closest = null;
            float closestDist = float.MaxValue;
            foreach (var node in _allNodes)
            {
                Vector2 pos = NodeToScreen(node);
                float dist = Vector2.Distance(pos, mouse);
                float radius = node.Radius * 2.5f * _zoom + 5f;
                if (dist < radius && dist < closestDist)
                {
                    closestDist = dist;
                    closest = node;
                }
            }
            return closest;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            // Fondo oscuro estrellado (estilo PoE)
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                new Color(8, 6, 16, 250));

            // Estrellas
            DrawStars(spriteBatch);

            // Vignette radial
            for (int r = 400; r > 0; r -= 30)
            {
                int alpha = (400 - r) / 15;
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(Main.screenWidth / 2 - r, Main.screenHeight / 2 - r, r * 2, r * 2),
                    new Color(25, 15, 45, alpha));
            }

            // Dibujar conexiones PRIMERO (detras de nodos)
            foreach (var (fromIdx, toIdx) in _allConnections)
            {
                var fromNode = _allNodes[fromIdx];
                var toNode = _allNodes[toIdx];
                Vector2 from = NodeToScreen(fromNode);
                Vector2 to = NodeToScreen(toNode);
                bool bothActive = sp.AllocatedNodes.Contains(fromNode.Id) && sp.AllocatedNodes.Contains(toNode.Id);

                Color lineColor = bothActive
                    ? new Color(245, 196, 81, 220)
                    : new Color(70, 60, 100, 80);
                float thickness = bothActive ? 3f : 1.5f;
                DrawLine(spriteBatch, from, to, lineColor, thickness);
            }

            // Encontrar nodo bajo hover
            var hoveredNode = FindHoveredNode();

            // Dibujar nodos
            foreach (var node in _allNodes)
            {
                Vector2 pos = NodeToScreen(node);
                bool allocated = sp.AllocatedNodes.Contains(node.Id);
                bool canAllocate = false;
                if (!allocated)
                {
                    if (node.Id == "start" || node.Cost == 0)
                        canAllocate = true;
                    else
                        foreach (var aid in sp.AllocatedNodes)
                            if (node.Connections.Contains(aid)) { canAllocate = true; break; }
                }
                bool isHovered = hoveredNode != null && hoveredNode.Id == node.Id;

                DrawNode(spriteBatch, pos, node, allocated, canAllocate, isHovered);
            }

            // Procesar click izquierdo
            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                if (hoveredNode != null && !_isDragging)
                {
                    bool allocated = sp.AllocatedNodes.Contains(hoveredNode.Id);
                    if (allocated)
                    {
                        sp.AllocatedNodes.Remove(hoveredNode.Id);
                        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
                    }
                    else
                    {
                        bool canAllocate = false;
                        if (hoveredNode.Id == "start" || hoveredNode.Cost == 0)
                            canAllocate = true;
                        else
                            foreach (var aid in sp.AllocatedNodes)
                                if (hoveredNode.Connections.Contains(aid)) { canAllocate = true; break; }

                        if (canAllocate)
                        {
                            sp.AllocatedNodes.Add(hoveredNode.Id);
                            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
                        }
                    }
                    UpdatePointsPoE();
                }
            }

            // Tooltip del nodo hover
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

            // Texto de ayuda abajo
            Utils.DrawBorderString(spriteBatch,
                "Click izq: asignar | Click der: mover | K: cerrar",
                new Vector2(Main.screenWidth / 2f, Main.screenHeight - 20),
                new Color(120, 110, 140), 0.85f, 0.5f, 0.5f);

            // Zoom con rueda
            if (true)
            {
                _zoom += true ? 0.1f : -0.1f;
                _zoom = System.Math.Clamp(_zoom, 0.3f, 3f);
            }
        }

        private void DrawStars(SpriteBatch sb)
        {
            for (int i = 0; i < 150; i++)
            {
                int seed = i * 73856093;
                float baseX = (seed % 1920);
                float baseY = ((seed * 19349663) % 1080);
                float sx = (baseX + _panOffset.X * 0.3f) % Main.screenWidth;
                float sy = (baseY + _panOffset.Y * 0.3f) % Main.screenHeight;
                if (sx < 0) sx += Main.screenWidth;
                if (sy < 0) sy += Main.screenHeight;
                int bright = 40 + ((seed * 83492791) % 50);
                int sz = 1 + ((seed * 1299721) % 2);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)sx, (int)sy, sz, sz),
                    new Color(bright, bright, bright + 15, bright + 20));
            }
        }

        private void DrawNode(SpriteBatch sb, Vector2 pos, PoESkillNode node,
            bool allocated, bool canAllocate, bool hovered)
        {
            int radius = (int)(node.Radius * 2.2f * _zoom);
            radius = System.Math.Max(6, radius);

            // Color base por tipo
            Color baseColor = node.Type switch
            {
                NodeType.Small => new Color(100, 100, 130),
                NodeType.Notable => new Color(70, 130, 220),
                NodeType.Keystone => new Color(245, 196, 81),
                NodeType.Ascendancy => new Color(160, 70, 240),
                _ => Color.White,
            };

            // Dim si no disponible
            if (!allocated && !canAllocate)
                baseColor *= 0.15f;

            // Glow si asignado (estilo PoE: nodos activos brillan)
            if (allocated)
            {
                for (int i = 3; i > 0; i--)
                {
                    int gr = radius + i * 5;
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(245, 196, 81, 12 - i * 3));
                }
            }

            // Glow al hover
            if (hovered && (canAllocate || allocated))
            {
                for (int i = 2; i > 0; i--)
                {
                    int gr = radius + i * 4;
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(255, 255, 255, 8 - i * 3));
                }
            }

            // Relleno del nodo
            Color fill = allocated
                ? new Color(255, 230, 150)
                : (canAllocate ? baseColor : baseColor * 0.1f);

            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)pos.X - radius, (int)pos.Y - radius, radius * 2, radius * 2),
                fill);

            // Borde
            Color border = allocated
                ? new Color(255, 245, 200)
                : hovered ? Color.White
                : new Color(baseColor.R + 40, baseColor.G + 40, baseColor.B + 40);
            int b = 2;
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)pos.X - radius, (int)pos.Y - radius, radius * 2, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)pos.X - radius, (int)pos.Y + radius - b, radius * 2, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)pos.X - radius, (int)pos.Y - radius, b, radius * 2), border);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)pos.X + radius - b, (int)pos.Y - radius, b, radius * 2), border);

            // Punto interior para small nodes
            if (node.Type == NodeType.Small)
            {
                int dr = radius / 3;
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)pos.X - dr, (int)pos.Y - dr, dr * 2, dr * 2),
                    allocated ? new Color(255, 245, 200) : new Color(baseColor.R + 60, baseColor.G + 60, baseColor.B + 60));
            }

            // Costo debajo del nodo
            if (node.Cost > 0)
            {
                Utils.DrawBorderString(sb, node.Cost.ToString(),
                    new Vector2(pos.X, pos.Y + radius + 6),
                    allocated ? new Color(255, 230, 150) : new Color(130, 120, 150),
                    0.7f, 0.5f, 0f);
            }
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
