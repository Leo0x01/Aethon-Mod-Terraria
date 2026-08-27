using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// Vista del arbol de habilidades — UIElement que dibuja el fondo de estrellas,
    /// los nodos y las conexiones. Maneja pan/zoom y click en nodos.
    /// Se renderiza DENTRO del DraggablePanel (no a pantalla completa).
    /// </summary>
    public class SkillTreeView : UIElement
    {
        private PoESkillTree? _tree;
        private List<PoESkillNode> _allNodes = new();
        private List<(int fromIdx, int toIdx)> _allConnections = new();
        private Dictionary<string, int> _nodeIndex = new();
        private float _minX, _maxX, _minY, _maxY, _rangeX, _rangeY;

        private float _zoom = 1f;
        private Vector2 _panOffset = Vector2.Zero;
        private bool _isPanning = false;
        private Vector2 _lastMouse = Vector2.Zero;
        private int _lastScrollValue = 0;
        private float _time = 0f;

        // Estrellas de fondo
        private struct Star
        {
            public Vector2 Pos;
            public float Size;
            public float Twinkle;
            public float Phase;
            public Color Color;
        }
        private Star[]? _stars;

        private PoESkillNode? _hoveredNode;

        public SkillTreeView()
        {
            // Margin = despues de la barra de titulo (40px) + padding
            MarginTop = 44;
            MarginBottom = 8;
            MarginLeft = 8;
            MarginRight = 8;
        }

        public void SetTree(PoESkillTree tree)
        {
            _tree = tree;
            _allNodes = tree.Nodes ?? new List<PoESkillNode>();
            _allConnections.Clear();
            _nodeIndex.Clear();
            for (int i = 0; i < _allNodes.Count; i++)
                _nodeIndex[_allNodes[i].Id] = i;
            foreach (var node in _allNodes)
            {
                if (!_nodeIndex.TryGetValue(node.Id, out int fromIdx)) continue;
                foreach (var connId in node.Connections)
                    if (_nodeIndex.TryGetValue(connId, out int j))
                        _allConnections.Add((fromIdx, j));
            }
            if (_allNodes.Count == 0)
            {
                _minX = _maxX = _minY = _maxY = 0;
                _rangeX = _rangeY = 1;
                return;
            }
            _minX = float.MaxValue; _maxX = float.MinValue;
            _minY = float.MaxValue; _maxY = float.MinValue;
            foreach (var node in _allNodes)
            {
                _minX = Math.Min(_minX, node.X);
                _maxX = Math.Max(_maxX, node.X);
                _minY = Math.Min(_minY, node.Y);
                _maxY = Math.Max(_maxY, node.Y);
            }
            _rangeX = Math.Max(1, _maxX - _minX + 1);
            _rangeY = Math.Max(1, _maxY - _minY + 1);
            _zoom = 1f;
            _panOffset = Vector2.Zero;
            InitStars();
        }

        private void InitStars()
        {
            if (_stars != null) return;
            var rand = new Random(42);
            _stars = new Star[80];
            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i] = new Star
                {
                    Pos = new Vector2((float)rand.NextDouble(), (float)rand.NextDouble()),
                    Size = 1f + (float)rand.NextDouble() * 2f,
                    Twinkle = 0.5f + (float)rand.NextDouble() * 2f,
                    Phase = (float)rand.NextDouble() * MathF.PI * 2f,
                    Color = i % 3 == 0 ? new Color(245, 196, 81)
                          : i % 3 == 1 ? new Color(179, 136, 255)
                          : new Color(200, 220, 255),
                };
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _time += 0.016f;
            HandleInput();
        }

        private void HandleInput()
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            // Pan con click derecho (dentro de la vista)
            var dims = GetDimensions();
            Rectangle viewRect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
            bool mouseInView = viewRect.Contains(Main.mouseX, Main.mouseY);

            if (Main.mouseRight && mouseInView)
            {
                Vector2 curMouse = new(Main.mouseX, Main.mouseY);
                if (!_isPanning)
                {
                    _isPanning = true;
                    _lastMouse = curMouse;
                }
                else
                {
                    _panOffset += curMouse - _lastMouse;
                    _lastMouse = curMouse;
                }
            }
            else _isPanning = false;

            // Zoom con rueda
            int curScroll = Terraria.GameInput.PlayerInput.ScrollWheelValue;
            int scrollDelta = curScroll - _lastScrollValue;
            _lastScrollValue = curScroll;
            if (scrollDelta != 0 && mouseInView)
            {
                float zoomDelta = scrollDelta > 0 ? 0.1f : -0.1f;
                _zoom = MathHelper.Clamp(_zoom + zoomDelta, 0.4f, 2.5f);
            }
        }

        private Vector2 NodeToScreen(PoESkillNode node, Rectangle viewRect)
        {
            float cx = viewRect.X + viewRect.Width / 2f;
            float cy = viewRect.Y + viewRect.Height / 2f;
            if (_rangeX <= 0 || _rangeY <= 0) return new Vector2(cx, cy);
            float scale = Math.Min(viewRect.Width / _rangeX, viewRect.Height / _rangeY) * 0.4f * _zoom;
            return new Vector2(
                (node.X - _minX) * scale - (_rangeX * scale / 2f) + cx + _panOffset.X,
                (node.Y - _minY) * scale - (_rangeY * scale / 2f) + cy + _panOffset.Y);
        }

        private PoESkillNode? FindHoveredNode(Rectangle viewRect)
        {
            Vector2 mouse = new(Main.mouseX, Main.mouseY);
            PoESkillNode? closest = null;
            float closestDist = float.MaxValue;
            foreach (var node in _allNodes)
            {
                Vector2 pos = NodeToScreen(node, viewRect);
                float dist = Vector2.Distance(pos, mouse);
                float radius = node.Radius * 2.2f * _zoom + 8f;
                if (dist < radius && dist < closestDist) { closestDist = dist; closest = node; }
            }
            return closest;
        }

        private bool CanAllocate(PoESkillNode node, ShardPlayer sp)
        {
            if (sp.AllocatedNodes.Contains(node.Id)) return false;
            if (node.Id == "start" || node.Cost == 0) return true;
            foreach (var aid in sp.AllocatedNodes)
                if (node.Connections.Contains(aid)) return true;
            return false;
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            if (_tree == null) return;
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            var dims = GetDimensions();
            Rectangle viewRect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

            // Fondo oscuro de la vista
            sb.Draw(TextureAssets.MagicPixel.Value, viewRect, new Color(8, 6, 16, 245));

            // Estrellas de fondo
            if (_stars != null)
            {
                for (int i = 0; i < _stars.Length; i++)
                {
                    var s = _stars[i];
                    float sx = viewRect.X + s.Pos.X * viewRect.Width + _panOffset.X * 0.2f;
                    float sy = viewRect.Y + s.Pos.Y * viewRect.Height + _panOffset.Y * 0.2f;
                    if (sx < viewRect.X || sx > viewRect.Right || sy < viewRect.Y || sy > viewRect.Bottom) continue;

                    float twinkle = 0.5f + 0.5f * (float)Math.Sin(_time * s.Twinkle + s.Phase);
                    int alpha = (int)(180 * twinkle * 0.6f);
                    if (alpha < 0) alpha = 0; if (alpha > 255) alpha = 255;
                    Color c = new Color(s.Color.R, s.Color.G, s.Color.B, alpha);
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)sx, (int)sy, (int)s.Size, (int)s.Size), c);
                }
            }

            // Nebulosa central pulsante
            float pulse = 0.85f + (float)Math.Sin(_time * 0.5) * 0.15f;
            DrawRadial(sb, viewRect, 0.5f, 0.5f, 0.3f * pulse, new Color(80, 40, 160, 14));

            if (_allNodes.Count == 0)
            {
                Utils.DrawBorderString(sb, "No hay arbol disponible.",
                    new Vector2(viewRect.X + viewRect.Width / 2f, viewRect.Y + viewRect.Height / 2f),
                    new Color(255, 200, 100), 0.9f, 0.5f, 0.5f);
                return;
            }

            // === CONEXIONES ===
            foreach (var (fromIdx, toIdx) in _allConnections)
            {
                if (fromIdx < 0 || fromIdx >= _allNodes.Count || toIdx < 0 || toIdx >= _allNodes.Count) continue;
                Vector2 from = NodeToScreen(_allNodes[fromIdx], viewRect);
                Vector2 to = NodeToScreen(_allNodes[toIdx], viewRect);
                bool bothActive = sp.AllocatedNodes.Contains(_allNodes[fromIdx].Id) && sp.AllocatedNodes.Contains(_allNodes[toIdx].Id);
                bool oneActive = sp.AllocatedNodes.Contains(_allNodes[fromIdx].Id) || sp.AllocatedNodes.Contains(_allNodes[toIdx].Id);
                Color lc = bothActive ? new Color(245, 196, 81, 220) : oneActive ? new Color(180, 140, 80, 140) : new Color(60, 50, 90, 80);
                float th = bothActive ? 3f : oneActive ? 2f : 1.5f;
                DrawLine(sb, from, to, lc, th);
            }

            // === NODOS ===
            _hoveredNode = FindHoveredNode(viewRect);
            foreach (var node in _allNodes)
            {
                Vector2 pos = NodeToScreen(node, viewRect);
                bool allocated = sp.AllocatedNodes.Contains(node.Id);
                bool canAlloc = CanAllocate(node, sp);
                bool isHover = _hoveredNode != null && _hoveredNode.Id == node.Id;
                DrawNode(sb, pos, node, allocated, canAlloc, isHover);
            }

            // === CLICK IZQUIERDO PARA ASIGNAR/QUITAR ===
            if (Main.mouseLeft && Main.mouseLeftRelease && _hoveredNode != null && !_isPanning)
            {
                var node = _hoveredNode;
                if (sp.AllocatedNodes.Contains(node.Id))
                {
                    sp.AllocatedNodes.Remove(node.Id);
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
                    Vector2 nodePos = NodeToScreen(node, viewRect);
                    for (int i = 0; i < 8; i++)
                        Dust.NewDustPerfect(nodePos, Terraria.ID.DustID.PurpleTorch,
                            new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                            100, new Color(179, 136, 255), 1f);
                }
                else if (CanAllocate(node, sp))
                {
                    int avail = sp.CumulativeSkillPoints() - sp.AllocatedNodes.Count;
                    if (avail < node.Cost)
                    {
                        Main.NewText($"Necesitas {node.Cost} pts, tienes {avail}.", new Color(255, 120, 120));
                    }
                    else
                    {
                        sp.AllocatedNodes.Add(node.Id);
                        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
                        Vector2 nodePos = NodeToScreen(node, viewRect);
                        for (int i = 0; i < 16; i++)
                            Dust.NewDustPerfect(nodePos, Terraria.ID.DustID.GoldFlame,
                                new Vector2(Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4)),
                                100, new Color(245, 196, 81), 1.2f);
                    }
                }
            }

            // === TOOLTIP ===
            if (_hoveredNode != null)
            {
                DrawTooltip(sb, sp, viewRect);
            }

            // Bloquear input del juego dentro de la vista
            if (viewRect.Contains(Main.mouseX, Main.mouseY))
            {
                Main.mouseLeft = false;
                Main.mouseRight = false;
            }
        }

        private void DrawNode(SpriteBatch sb, Vector2 pos, PoESkillNode node, bool allocated, bool canAlloc, bool hovered)
        {
            int radius = (int)(node.Radius * 2.0f * _zoom);
            radius = Math.Max(6, radius);

            Color baseColor = node.Type switch
            {
                NodeType.Small => new Color(90, 90, 120),
                NodeType.Notable => new Color(60, 120, 210),
                NodeType.Keystone => new Color(245, 196, 81),
                NodeType.Ascendancy => new Color(150, 60, 230),
                NodeType.Cluster => new Color(120, 80, 180),
                _ => Color.White,
            };

            if (!allocated && !canAlloc) baseColor *= 0.18f;

            // Glow pulsante para keystones/ascendancy
            if (node.Type == NodeType.Keystone || node.Type == NodeType.Ascendancy)
            {
                float pulse = 0.7f + 0.3f * (float)Math.Sin(_time * 2f + pos.X * 0.01f);
                for (int i = 4; i > 0; i--)
                {
                    int gr = radius + i * 6;
                    int a = (int)((allocated ? 10 : 5) * pulse);
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(baseColor.R, baseColor.G, baseColor.B, a));
                }
            }

            // Glow dorado si asignado
            if (allocated)
                for (int i = 3; i > 0; i--)
                {
                    int gr = radius + i * 5;
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(245, 196, 81, 10 - i * 2));
                }

            // Glow blanco al hover
            if (hovered && (canAlloc || allocated))
                for (int i = 2; i > 0; i--)
                {
                    int gr = radius + i * 4;
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(255, 255, 255, 8 - i * 2));
                }

            // Relleno
            Color fill = allocated ? new Color(255, 225, 140) : (canAlloc ? baseColor : baseColor * 0.12f);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)pos.X - radius, (int)pos.Y - radius, radius * 2, radius * 2), fill);

            // Borde
            Color border = allocated ? new Color(255, 240, 190) : hovered ? Color.White : new Color(baseColor.R + 30, baseColor.G + 30, baseColor.B + 30);
            int b = Math.Max(2, radius / 8);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)pos.X - radius, (int)pos.Y - radius, radius * 2, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)pos.X - radius, (int)pos.Y + radius - b, radius * 2, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)pos.X - radius, (int)pos.Y - radius, b, radius * 2), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)pos.X + radius - b, (int)pos.Y - radius, b, radius * 2), border);

            // Punto interior para small nodes
            if (node.Type == NodeType.Small)
            {
                int dr = radius / 3;
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)pos.X - dr, (int)pos.Y - dr, dr * 2, dr * 2),
                    allocated ? new Color(255, 245, 200) : new Color(baseColor.R + 50, baseColor.G + 50, baseColor.B + 50));
            }

            // Nombre del notable/keystone/ascendancy
            if (node.Type == NodeType.Notable || node.Type == NodeType.Keystone || node.Type == NodeType.Ascendancy)
            {
                string shortName = node.Name.Length > 16 ? node.Name.Substring(0, 14) + "…" : node.Name;
                Utils.DrawBorderString(sb, shortName, new Vector2(pos.X, pos.Y - radius - 10),
                    allocated ? new Color(255, 240, 190) : new Color(220, 200, 240), 0.65f * _zoom, 0.5f, 1f);
            }

            // Costo debajo
            if (node.Cost > 0)
                Utils.DrawBorderString(sb, node.Cost.ToString(),
                    new Vector2(pos.X, pos.Y + radius + 5),
                    allocated ? new Color(255, 225, 140) : new Color(120, 110, 140), 0.6f, 0.5f, 0f);
        }

        private void DrawTooltip(SpriteBatch sb, ShardPlayer sp, Rectangle viewRect)
        {
            if (_hoveredNode == null) return;
            var node = _hoveredNode;
            bool alloc = sp.AllocatedNodes.Contains(node.Id);

            string typeStr = node.Type switch
            {
                NodeType.Small => "[Menor]",
                NodeType.Notable => "[Notable]",
                NodeType.Keystone => "[KEYSTONE]",
                NodeType.Ascendancy => "[Ascendencia]",
                NodeType.Cluster => "[Cluster]",
                _ => "",
            };

            string[] lines = {
                $"{typeStr} {node.Name}",
                node.Effect ?? "",
                $"Rama: {node.Branch}",
                $"Coste: {node.Cost} pts",
                alloc ? "(Asignado — click para quitar)" : (CanAllocate(node, sp) ? "(Click para asignar)" : "(Requiere nodo adyacente)"),
            };

            // Medir
            float maxW = 0;
            foreach (var line in lines)
            {
                var size = FontAssets.MouseText.Value.MeasureString(line);
                if (size.X > maxW) maxW = size.X;
            }
            float lineH = 18f;
            int padX = 10, padY = 8;
            int tw = (int)maxW + padX * 2;
            int th = (int)(lines.Length * lineH) + padY * 2;

            int tx = Main.mouseX + 16;
            int ty = Main.mouseY + 16;
            if (tx + tw > Main.screenWidth) tx = Main.mouseX - tw - 8;
            if (ty + th > Main.screenHeight) ty = Main.screenHeight - th - 4;
            if (ty < 0) ty = 4;

            // Fondo
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(tx, ty, tw, th), new Color(15, 10, 30, 245));
            // Borde
            Color borderC = node.Type == NodeType.Keystone ? new Color(245, 196, 81) : node.Type == NodeType.Ascendancy ? new Color(179, 136, 255) : new Color(120, 100, 160);
            int b = 2;
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(tx, ty, tw, b), borderC);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(tx, ty + th - b, tw, b), borderC);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(tx, ty, b, th), borderC);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(tx + tw - b, ty, b, th), borderC);

            for (int i = 0; i < lines.Length; i++)
            {
                Color col = i == 0 ? new Color(245, 196, 81) : i == lines.Length - 1 ? (alloc ? new Color(100, 220, 100) : new Color(179, 136, 255)) : new Color(220, 220, 230);
                Utils.DrawBorderString(sb, lines[i], new Vector2(tx + padX, ty + padY + i * lineH), col, 0.8f);
            }
        }

        private void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            float length = edge.Length();
            if (length < 1f) return;
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)start.X, (int)start.Y, (int)length, (int)thickness),
                null, color, angle, new Vector2(0, thickness / 2f), SpriteEffects.None, 0);
        }

        private void DrawRadial(SpriteBatch sb, Rectangle rect, float xPct, float yPct, float rPct, Color color)
        {
            float cx = rect.X + rect.Width * xPct;
            float cy = rect.Y + rect.Height * yPct;
            float r = rect.Width * rPct;
            if (r <= 0) return;
            for (int i = (int)r; i > 0; i -= 12)
            {
                int alpha = (int)(color.A * (1f - (float)i / r) * 0.3f);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)(cx - i), (int)(cy - i), i * 2, i * 2),
                    new Color(color.R, color.G, color.B, alpha));
            }
        }
    }

    /// <summary>
    /// Estado del arbol de habilidades — UIState que contiene un DraggablePanel
    /// con el SkillTreeView dentro. Se gestiona via UserInterface para que el
    /// renderizado funcione correctamente (no mas pantalla blanca).
    /// </summary>
    public class SkillTreeUIState : UIState
    {
        public bool IsVisible = false;
        private DraggablePanel? _panel;
        private SkillTreeView? _treeView;

        public SkillTreeUIState()
        {
        }

        public void Show()
        {
            if (IsVisible) { Hide(); return; }
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted)
            {
                Main.NewText("El Fragmento Genesis aun no tiene una rama.", new Color(180, 160, 220));
                return;
            }
            RemoveAllChildren();
            _panel = new DraggablePanel(700, 520, "★ ARBOL DE HABILIDADES ★");
            _panel.OnCloseClick += (evt, el) => Hide();

            // Vista del arbol dentro del panel
            _treeView = new SkillTreeView();
            _treeView.Width.Set(0, 1f);
            _treeView.Height.Set(0, 1f);
            _treeView.Top.Set(44, 0f);

            var tree = PoETreeCatalog.GetTree(sp.ActiveBranch);
            _treeView.SetTree(tree);

            _panel.Append(_treeView);

            // Info inferior del panel (nivel, puntos)
            _ = sp; // ya capturado

            Append(_panel);
            IsVisible = true;
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuOpen);
        }

        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            RemoveAllChildren();
            _panel = null;
            _treeView = null;
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (IsVisible)
            {
                var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
                if (sp != null && (!sp.IsImprinted || !Main.playerInventory))
                {
                    // Mantener visible aunque se abra el inventario
                }
            }
        }
    }
}
