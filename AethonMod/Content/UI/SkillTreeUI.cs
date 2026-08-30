using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.UI
{
    // ================================================================
    // SkillNodeElement — nodo clickeable (basado en SkillPanel de AnRPG)
    // ================================================================
    public class SkillNodeElement : UIPanel
    {
        public PoESkillNode Node;
        public Vector2 BasePos;
        public Color NodeColor = Color.White;
        private float _time;

        public SkillNodeElement(PoESkillNode node, Vector2 basePos)
        {
            Node = node;
            BasePos = basePos;
            float size = GetNodeSize(node) * 2;
            Width.Set(size, 0f);
            Height.Set(size, 0f);
            SetPadding(0);
            BackgroundColor = new Color(0, 0, 0, 0);
            BorderColor = new Color(0, 0, 0, 0);
        }

        public static float GetNodeSize(PoESkillNode node)
        {
            return node.Type switch
            {
                NodeType.Small => 10f,
                NodeType.Notable => 16f,
                NodeType.Keystone => 22f,
                NodeType.Ascendancy => 20f,
                _ => 10f,
            };
        }

        public void SetState(bool allocated, bool canAlloc, float time)
        {
            _time = time;
            if (allocated) NodeColor = new Color(255, 225, 140, 255);
            else if (canAlloc) NodeColor = node_type_color(Node);
            else NodeColor = new Color(60, 50, 80, 200);
        }

        private Color node_type_color(PoESkillNode n) => n.Type switch
        {
            NodeType.Small => new Color(100, 110, 160, 255),
            NodeType.Notable => new Color(70, 140, 220, 255),
            NodeType.Keystone => new Color(245, 196, 81, 255),
            NodeType.Ascendancy => new Color(180, 100, 250, 255),
            _ => new Color(100, 110, 160, 255),
        };

        protected override void DrawSelf(SpriteBatch sb)
        {
            var dims = GetDimensions();
            Vector2 pos = dims.Center();
            float radius = Math.Min(dims.Width, dims.Height) / 2f;

            // Glow para keystones/ascendancy
            if (Node.Type == NodeType.Keystone || Node.Type == NodeType.Ascendancy)
            {
                float pulse = 0.7f + 0.3f * (float)Math.Sin(_time * 2f + pos.X * 0.01f);
                for (int i = 3; i > 0; i--)
                {
                    int gr = (int)(radius + i * 5);
                    int a = (int)(8 * pulse);
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(NodeColor.R, NodeColor.G, NodeColor.B, a));
                }
            }

            // Relleno circular
            DrawCircle(sb, pos, radius * 0.8f, NodeColor);

            // Borde
            Color border = IsMouseHovering ? Color.White : new Color(NodeColor.R + 40, NodeColor.G + 40, NodeColor.B + 40);
            DrawCircleOutline(sb, pos, radius * 0.8f, border);

            // Punto interior
            if (Node.Type == NodeType.Small && NodeColor == new Color(255, 225, 140, 255))
                DrawCircle(sb, pos, radius * 0.3f, new Color(255, 245, 200));
        }

        private void DrawCircle(SpriteBatch sb, Vector2 center, float radius, Color color)
        {
            int r = (int)radius; if (r < 1) return;
            for (int dy = -r; dy <= r; dy++)
            {
                int dx = (int)Math.Sqrt(r * r - dy * dy);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)center.X - dx, (int)center.Y + dy, dx * 2 + 1, 1), color);
            }
        }

        private void DrawCircleOutline(SpriteBatch sb, Vector2 center, float radius, Color color)
        {
            int r = (int)radius; if (r < 1) return;
            int steps = Math.Max(8, r * 4);
            for (int i = 0; i < steps; i++)
            {
                float angle = (float)(i * Math.PI * 2 / steps);
                float px = center.X + (float)Math.Cos(angle) * radius;
                float py = center.Y + (float)Math.Sin(angle) * radius;
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)px, (int)py, 2, 2), color);
            }
        }
    }

    // ================================================================
    // ConnectionElement — línea entre nodos (basado en Connection de AnRPG)
    // ================================================================
    public class ConnectionElement : UIElement
    {
        public Vector2 BasePos;
        public float Rotation;
        public float Length;
        public Color LineColor;
        public float SizeMult;

        public ConnectionElement(Vector2 basePos, float rotation, float length, float sizeMult)
        {
            BasePos = basePos; Rotation = rotation; Length = length; SizeMult = sizeMult;
            Width.Set(length * sizeMult, 0f); Height.Set(3f * sizeMult, 0f);
            LineColor = new Color(50, 40, 70, 60);
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            var dims = GetDimensions();
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height),
                null, LineColor, Rotation, new Vector2(0, dims.Height / 2f), SpriteEffects.None, 0f);
        }
    }

    // ================================================================
    // SkillTreeUIState — el árbol completo (basado en SkillTreeUi de AnRPG)
    // ================================================================
    public class SkillTreeUIState : UIState
    {
        public bool IsVisible = false;

        private float _zoom = 1f;
        private float _sizeMult = 1f;
        private Vector2 _offset;
        private bool _dragging = false;
        private Vector2 _dragStart;
        private float _time;

        private PoESkillTree? _tree;
        private List<PoESkillNode> _allNodes = new();
        private List<(int fromIdx, int toIdx)> _allConnections = new();
        private Dictionary<string, int> _nodeIndex = new();
        private float _minX, _maxX, _minY, _maxY, _rangeX, _rangeY;

        private UIPanel _background = new();
        private List<SkillNodeElement> _nodeEls = new();
        private List<ConnectionElement> _connEls = new();
        private UIText _titleText = new("");
        private UIText _closeText = new("X", 1.2f);

        const float SKILL_SIZE = 48f;

        public void Show()
        {
            if (IsVisible) { Hide(); return; }
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted)
            {
                Main.NewText("El Fragmento Genesis aun no tiene una rama.", new Color(180, 160, 220));
                return;
            }
            _tree = PoETreeCatalog.GetTree(sp.ActiveBranch);
            BuildIndex();
            if (!sp.AllocatedNodes.Contains("start")) sp.AllocatedNodes.Add("start");
            _zoom = 0.8f;
            _offset = new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            Init();
            IsVisible = true;
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuOpen);
        }

        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            _dragging = false;
            Erase();
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
        }

        private void Erase()
        {
            _background.RemoveAllChildren();
            _background.Remove();
            _nodeEls.Clear();
            _connEls.Clear();
        }

        private void BuildIndex()
        {
            if (_tree == null) return;
            _allNodes = _tree.Nodes ?? new();
            _allConnections.Clear(); _nodeIndex.Clear();
            for (int i = 0; i < _allNodes.Count; i++) _nodeIndex[_allNodes[i].Id] = i;
            foreach (var node in _allNodes)
            {
                if (!_nodeIndex.TryGetValue(node.Id, out int fi)) continue;
                foreach (var c in node.Connections)
                    if (_nodeIndex.TryGetValue(c, out int j)) _allConnections.Add((fi, j));
            }
            if (_allNodes.Count == 0) { _rangeX = _rangeY = 1; return; }
            _minX = float.MaxValue; _maxX = float.MinValue; _minY = float.MaxValue; _maxY = float.MinValue;
            foreach (var n in _allNodes) { _minX = Math.Min(_minX, n.X); _maxX = Math.Max(_maxX, n.X); _minY = Math.Min(_minY, n.Y); _maxY = Math.Max(_maxY, n.Y); }
            _rangeX = Math.Max(1, _maxX - _minX + 1); _rangeY = Math.Max(1, _maxY - _minY + 1);
        }

        private Vector2 NodeToBase(PoESkillNode n)
        {
            float s = 0.3f;
            return new((n.X - _minX) * s - _rangeX * s / 2f, (n.Y - _minY) * s - _rangeY * s / 2f);
        }

        // === INIT — Construir toda la UI (igual que AnRPG.Init) ===
        private void Init()
        {
            Erase();
            _sizeMult = _zoom;

            _background.SetPadding(0);
            _background.Left.Set(0, 0f); _background.Top.Set(0, 0f);
            _background.Width.Set(Main.screenWidth, 0f); _background.Height.Set(Main.screenHeight, 0f);
            _background.BackgroundColor = new Color(8, 6, 16, 200);
            _background.BorderColor = new Color(0, 0, 0, 0);
            // Drag — igual que AnRPG: OnMouseDown/OnMouseUp en el background
            _background.OnLeftMouseDown += (evt, el) => { _dragging = true; _dragStart = evt.MousePosition; };
            _background.OnLeftMouseUp += (evt, el) => { _dragging = false; };
            // Scroll — igual que AnRPG: OnScrollWheel
            _background.OnScrollWheel += (UIScrollWheelEvent evt, UIElement el) => { ScrollZoom(evt); };
            Append(_background);

            // Título
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            int avail = sp?.AvailableSkillPoints() ?? 0;
            _titleText = new UIText($"★ ARBOL DE HABILIDADES ★  |  Puntos: {avail}  |  Nivel {sp?.ShardLevel ?? 1}", 0.85f);
            _titleText.Left.Set(Main.screenWidth / 2f - 200, 0f); _titleText.Top.Set(15, 0f);
            _titleText.TextColor = new Color(245, 196, 81);
            _background.Append(_titleText);

            // Botón cerrar
            _closeText = new UIText("X", 1.2f);
            _closeText.Left.Set(Main.screenWidth - 50, 0f); _closeText.Top.Set(10, 0f);
            _closeText.TextColor = new Color(220, 80, 80);
            _closeText.OnMouseOver += (e, el) => _closeText.TextColor = Color.White;
            _closeText.OnMouseOut += (e, el) => _closeText.TextColor = new Color(220, 80, 80);
            _closeText.OnLeftClick += (e, el) => Hide();
            _background.Append(_closeText);

            // Nodos — igual que AnRPG: crear SkillPanel + OnClick
            foreach (var node in _allNodes)
            {
                Vector2 bp = NodeToBase(node);
                var ne = new SkillNodeElement(node, bp);
                ne.OnLeftClick += (e, el) => OnNodeClick(node);
                _background.Append(ne);
                _nodeEls.Add(ne);
            }

            // Conexiones
            foreach (var (fi, ti) in _allConnections)
            {
                if (fi < 0 || fi >= _allNodes.Count || ti < 0 || ti >= _allNodes.Count) continue;
                Vector2 f = NodeToBase(_allNodes[fi]), t = NodeToBase(_allNodes[ti]);
                Vector2 d = t - f;
                float rot = (float)Math.Atan2(d.Y, d.X);
                float len = d.Length();
                var ce = new ConnectionElement(f, rot, len, _sizeMult);
                _background.Append(ce);
                _connEls.Add(ce);
            }

            UpdatePositions();
            UpdateStates();
        }

        // === SCROLL — Igual que AnRPG.ScrollUpDown ===
        private void ScrollZoom(UIScrollWheelEvent evt)
        {
            float preZoom = _zoom;
            if (evt.ScrollWheelValue > 0) _zoom = MathHelper.Clamp(1.1f * _zoom, 0.2f, 3f);
            else _zoom = MathHelper.Clamp(0.85f * _zoom, 0.2f, 3f);
            float ratio = _zoom / preZoom;
            _offset /= ratio;
            Init();
        }

        // === UPDATE — Igual que AnRPG.Update ===
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _time += 0.016f;

            // Drag — igual que AnRPG.DrawSelf: mover offset en tiempo real
            if (_dragging)
            {
                Vector2 mouse = new(Main.mouseX, Main.mouseY);
                _offset += mouse - _dragStart;
                _dragStart = mouse;
                UpdatePositions();
            }

            UpdateStates();

            // Cerrar con Esc
            if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape)) Hide();
        }

        // === DRAWSELF — Igual que AnRPG.DrawSelf: mouseInterface + drag ===
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (_background.ContainsPoint(new(Main.mouseX, Main.mouseY)))
                Main.LocalPlayer.mouseInterface = true;
        }

        private void UpdatePositions()
        {
            foreach (var ne in _nodeEls)
            {
                float x = (ne.BasePos.X + _offset.X) * _sizeMult;
                float y = (ne.BasePos.Y + _offset.Y) * _sizeMult;
                ne.Left.Set(x - ne.Width.Pixels / 2f, 0f);
                ne.Top.Set(y - ne.Height.Pixels / 2f, 0f);
            }
            foreach (var ce in _connEls)
            {
                float x = (ce.BasePos.X + _offset.X) * _sizeMult;
                float y = (ce.BasePos.Y + _offset.Y) * _sizeMult;
                ce.Left.Set(x, 0f); ce.Top.Set(y, 0f);
                ce.Width.Set(ce.Length * _sizeMult, 0f); ce.Height.Set(3f * _sizeMult, 0f);
                ce.SizeMult = _sizeMult;
            }
            Recalculate();
        }

        private void UpdateStates()
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;
            foreach (var ne in _nodeEls)
            {
                bool alloc = sp.AllocatedNodes.Contains(ne.Node.Id);
                bool can = CanAlloc(ne.Node, sp);
                ne.SetState(alloc, can, _time);
            }
            for (int i = 0; i < _connEls.Count && i < _allConnections.Count; i++)
            {
                var (fi, ti) = _allConnections[i];
                if (fi < 0 || fi >= _allNodes.Count || ti < 0 || ti >= _allNodes.Count) continue;
                bool both = sp.AllocatedNodes.Contains(_allNodes[fi].Id) && sp.AllocatedNodes.Contains(_allNodes[ti].Id);
                bool one = sp.AllocatedNodes.Contains(_allNodes[fi].Id) || sp.AllocatedNodes.Contains(_allNodes[ti].Id);
                _connEls[i].LineColor = both ? new(245, 196, 81, 200) : one ? new(160, 120, 70, 120) : new(50, 40, 70, 60);
            }
        }

        private bool CanAlloc(PoESkillNode n, ShardPlayer sp)
        {
            if (sp.AllocatedNodes.Contains(n.Id)) return false;
            if (n.Id == "start" || n.Cost == 0) return true;
            foreach (var a in sp.AllocatedNodes) if (n.Connections.Contains(a)) return true;
            return false;
        }

        private void OnNodeClick(PoESkillNode node)
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;
            if (sp.AllocatedNodes.Contains(node.Id)) { sp.AllocatedNodes.Remove(node.Id); Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose); }
            else if (CanAlloc(node, sp))
            {
                int av = sp.AvailableSkillPoints();
                if (av < node.Cost) Main.NewText($"Necesitas {node.Cost} pts, tienes {av}.", new Color(255, 120, 120));
                else { sp.AllocatedNodes.Add(node.Id); Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick); }
            }
            UpdateStates();
            if (_titleText != null) _titleText.SetText($"★ ARBOL DE HABILIDADES ★  |  Puntos: {sp.AvailableSkillPoints()}  |  Nivel {sp.ShardLevel}");
        }
    }
}
