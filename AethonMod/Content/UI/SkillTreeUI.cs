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
    /// <summary>
    /// Nodo del árbol de habilidades — UIElement clickeable.
    /// Basado en el patrón de AnRPG (SkillPanel).
    /// </summary>
    public class SkillNodeElement : UIElement
    {
        public PoESkillNode Node;
        public Vector2 BasePos;
        public bool Allocated;
        public bool CanAlloc;
        private float _time;

        public SkillNodeElement(PoESkillNode node, Vector2 basePos, float sizeMultiplier)
        {
            Node = node;
            BasePos = basePos;
            float size = GetNodeSize(node) * 2 * sizeMultiplier;
            Width.Set(size, 0f);
            Height.Set(size, 0f);
            // Margen para el glow
            MarginTop = 10;
            MarginBottom = 10;
            MarginLeft = 10;
            MarginRight = 10;
        }

        public static float GetNodeSize(PoESkillNode node)
        {
            return node.Type switch
            {
                NodeType.Small => 10f,
                NodeType.Notable => 16f,
                NodeType.Keystone => 20f,
                NodeType.Ascendancy => 18f,
                _ => 10f,
            };
        }

        public void UpdateState(bool allocated, bool canAlloc, float time)
        {
            Allocated = allocated;
            CanAlloc = canAlloc;
            _time = time;
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            var dims = GetDimensions();
            Vector2 pos = dims.Center();
            float radius = Width.Pixels / 2f;

            Color baseColor = Node.Type switch
            {
                NodeType.Small => new Color(100, 90, 130),
                NodeType.Notable => new Color(70, 130, 200),
                NodeType.Keystone => new Color(245, 196, 81),
                NodeType.Ascendancy => new Color(150, 60, 230),
                _ => new Color(100, 90, 130),
            };

            if (!Allocated && !CanAlloc) baseColor *= 0.25f;

            // Glow para keystones
            if (Node.Type == NodeType.Keystone || Node.Type == NodeType.Ascendancy)
            {
                float pulse = 0.7f + 0.3f * (float)Math.Sin(_time * 2f + pos.X * 0.01f);
                for (int i = 3; i > 0; i--)
                {
                    int gr = (int)(radius + i * 5);
                    int a = (int)((Allocated ? 12 : 6) * pulse);
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(baseColor.R, baseColor.G, baseColor.B, a));
                }
            }

            // Glow dorado si asignado
            if (Allocated)
                for (int i = 3; i > 0; i--)
                {
                    int gr = (int)(radius + i * 4);
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(245, 196, 81, 10 - i * 2));
                }

            // Relleno circular
            DrawCircle(sb, pos, radius * 0.8f, Allocated ? new Color(255, 225, 140) : (CanAlloc ? baseColor : baseColor * 0.1f));

            // Borde
            Color border = Allocated ? new Color(255, 240, 190) : IsMouseHovering ? Color.White : new Color(baseColor.R + 40, baseColor.G + 40, baseColor.B + 40);
            DrawCircleOutline(sb, pos, radius * 0.8f, border);

            // Punto interior para small allocated
            if (Node.Type == NodeType.Small && Allocated)
                DrawCircle(sb, pos, radius * 0.3f, new Color(255, 245, 200));
        }

        private void DrawCircle(SpriteBatch sb, Vector2 center, float radius, Color color)
        {
            int r = (int)radius;
            if (r < 1) return;
            for (int dy = -r; dy <= r; dy++)
            {
                int dx = (int)Math.Sqrt(r * r - dy * dy);
                int w = dx * 2 + 1;
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)center.X - dx, (int)center.Y + dy, w, 1), color);
            }
        }

        private void DrawCircleOutline(SpriteBatch sb, Vector2 center, float radius, Color color)
        {
            int r = (int)radius;
            if (r < 1) return;
            int steps = Math.Max(8, r * 4);
            for (int i = 0; i < steps; i++)
            {
                float angle = (float)(i * Math.PI * 2 / steps);
                float px = center.X + (float)Math.Cos(angle) * radius;
                float py = center.Y + (float)Math.Sin(angle) * radius;
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)px, (int)py, 2, 2), color);
            }
        }
    }

    /// <summary>
    /// Conexión entre nodos — UIElement que dibuja una línea.
    /// </summary>
    public class SkillConnectionElement : UIElement
    {
        public Vector2 BasePos;
        public float Rotation;
        public float Length;
        public Color LineColor;
        public float SizeMultiplier;

        public SkillConnectionElement(Vector2 basePos, float rotation, float length, float sizeMultiplier)
        {
            BasePos = basePos;
            Rotation = rotation;
            Length = length;
            SizeMultiplier = sizeMultiplier;
            Width.Set(length * sizeMultiplier, 0f);
            Height.Set(3f * sizeMultiplier, 0f);
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

    /// <summary>
    /// Árbol de habilidades usando UIState + UIElement (patrón de AnRPG).
    /// Los clicks funcionan nativamente porque UIElement maneja OnClick.
    /// </summary>
    public class SkillTreeUIState : UIState
    {
        public bool IsVisible = false;
        private float _time = 0f;
        private float _zoom = 0.5f;
        private Vector2 _offset = Vector2.Zero;
        private bool _isDragging = false;
        private Vector2 _dragStart = Vector2.Zero;
        private float _sizeMultiplier = 1f;

        private PoESkillTree? _tree;
        private List<PoESkillNode> _allNodes = new();
        private List<(int fromIdx, int toIdx)> _allConnections = new();
        private Dictionary<string, int> _nodeIndex = new();
        private float _minX, _maxX, _minY, _maxY, _rangeX, _rangeY;

        private UIPanel? _background;
        private List<SkillNodeElement> _nodeElements = new();
        private List<SkillConnectionElement> _connectionElements = new();
        private UIText? _pointsText;
        private UIText? _closeButton;
        private bool _closeHovered = false;

        // Textura de fondo
        private Texture2D? _bgTexture;

        public void Show()
        {
            if (IsVisible) { Hide(); return; }
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted)
            {
                Main.NewText("El Fragmento Genesis aun no tiene una rama.", new Color(180, 160, 220));
                return;
            }
            LoadBgTexture();
            _tree = PoETreeCatalog.GetTree(sp.ActiveBranch);
            BuildNodeIndex();
            if (!sp.AllocatedNodes.Contains("start"))
                sp.AllocatedNodes.Add("start");
            _zoom = 0.5f;
            _offset = new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            InitUI();
            IsVisible = true;
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuOpen);
        }

        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            _isDragging = false;
            RemoveAllChildren();
            _background = null;
            _nodeElements.Clear();
            _connectionElements.Clear();
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
        }

        private void LoadBgTexture()
        {
            if (_bgTexture != null) return;
            try
            {
                _bgTexture = ModContent.Request<Texture2D>("AethonMod/Content/UI/Textures/SkillTree_Background", AssetRequestMode.ImmediateLoad).Value;
            }
            catch { _bgTexture = null; }
        }

        private void BuildNodeIndex()
        {
            if (_tree == null) return;
            _allNodes = _tree.Nodes ?? new List<PoESkillNode>();
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
            if (_allNodes.Count == 0) { _rangeX = _rangeY = 1; return; }
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
        }

        private void InitUI()
        {
            RemoveAllChildren();
            _nodeElements.Clear();
            _connectionElements.Clear();
            _sizeMultiplier = _zoom;

            // Fondo
            _background = new UIPanel();
            _background.SetPadding(0);
            _background.Left.Set(0, 0f);
            _background.Top.Set(0, 0f);
            _background.Width.Set(Main.screenWidth, 0f);
            _background.Height.Set(Main.screenHeight, 0f);
            _background.BackgroundColor = new Color(10, 8, 20, 200);
            _background.OnMouseDown += (evt, el) => { _isDragging = true; _dragStart = new Vector2(Main.mouseX, Main.mouseY) - _offset; };
            _background.OnMouseUp += (evt, el) => { _isDragging = false; };
            _background.OnScrollWheel += (evt) =>
            {
                float zoomDelta = evt.ScrollWheelValue > 0 ? 0.1f : -0.1f;
                _zoom = MathHelper.Clamp(_zoom + zoomDelta, 0.2f, 3.0f);
                _sizeMultiplier = _zoom;
                UpdatePositions();
            };
            Append(_background);

            // Texto de puntos
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            int avail = sp?.AvailableSkillPoints() ?? 0;
            _pointsText = new UIText($"★ ARBOL DE HABILIDADES ★  |  Puntos: {avail}  |  Nivel {sp?.ShardLevel ?? 1}", 0.9f);
            _pointsText.Left.Set(Main.screenWidth / 2f - 200, 0f);
            _pointsText.Top.Set(20, 0f);
            _pointsText.TextColor = new Color(245, 196, 81);
            _background.Append(_pointsText);

            // Botón cerrar
            _closeButton = new UIText("X", 1.2f);
            _closeButton.Left.Set(Main.screenWidth - 50, 0f);
            _closeButton.Top.Set(10, 0f);
            _closeButton.TextColor = new Color(220, 80, 80);
            _closeButton.OnMouseOver += (evt, el) => { _closeHovered = true; _closeButton.TextColor = Color.White; };
            _closeButton.OnMouseOut += (evt, el) => { _closeHovered = false; _closeButton.TextColor = new Color(220, 80, 80); };
            _closeButton.OnClick += (evt, el) => { Hide(); };
            _background.Append(_closeButton);

            // Crear nodos
            foreach (var node in _allNodes)
            {
                Vector2 basePos = NodeToBasePos(node);
                var nodeEl = new SkillNodeElement(node, basePos, _sizeMultiplier);
                nodeEl.OnClick += (evt, el) => { OnNodeClick(node); };
                _background.Append(nodeEl);
                _nodeElements.Add(nodeEl);
            }

            // Crear conexiones
            foreach (var (fromIdx, toIdx) in _allConnections)
            {
                if (fromIdx < 0 || fromIdx >= _allNodes.Count || toIdx < 0 || toIdx >= _allNodes.Count) continue;
                Vector2 from = NodeToBasePos(_allNodes[fromIdx]);
                Vector2 to = NodeToBasePos(_allNodes[toIdx]);
                Vector2 diff = to - from;
                float rotation = (float)Math.Atan2(diff.Y, diff.X);
                float length = diff.Length();
                var connEl = new SkillConnectionElement(from, rotation, length, _sizeMultiplier);
                _background.Append(connEl);
                _connectionElements.Add(connEl);
            }

            UpdatePositions();
            UpdateNodeStates();
        }

        private Vector2 NodeToBasePos(PoESkillNode node)
        {
            float scale = 0.35f;
            return new Vector2(
                (node.X - _minX) * scale - (_rangeX * scale / 2f),
                (node.Y - _minY) * scale - (_rangeY * scale / 2f));
        }

        private void UpdatePositions()
        {
            foreach (var nodeEl in _nodeElements)
            {
                float x = (nodeEl.BasePos.X + _offset.X) * _sizeMultiplier;
                float y = (nodeEl.BasePos.Y + _offset.Y) * _sizeMultiplier;
                nodeEl.Left.Set(x - nodeEl.Width.Pixels / 2f, 0f);
                nodeEl.Top.Set(y - nodeEl.Height.Pixels / 2f, 0f);
            }
            foreach (var connEl in _connectionElements)
            {
                float x = (connEl.BasePos.X + _offset.X) * _sizeMultiplier;
                float y = (connEl.BasePos.Y + _offset.Y) * _sizeMultiplier;
                connEl.Left.Set(x, 0f);
                connEl.Top.Set(y, 0f);
                connEl.Width.Set(connEl.Length * _sizeMultiplier, 0f);
                connEl.Height.Set(3f * _sizeMultiplier, 0f);
            }
            Recalculate();
        }

        private void UpdateNodeStates()
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;
            foreach (var nodeEl in _nodeElements)
            {
                bool allocated = sp.AllocatedNodes.Contains(nodeEl.Node.Id);
                bool canAlloc = CanAllocate(nodeEl.Node, sp);
                nodeEl.UpdateState(allocated, canAlloc, _time);
            }
            // Actualizar colores de conexiones
            for (int i = 0; i < _connectionElements.Count && i < _allConnections.Count; i++)
            {
                var (fromIdx, toIdx) = _allConnections[i];
                if (fromIdx < 0 || fromIdx >= _allNodes.Count || toIdx < 0 || toIdx >= _allNodes.Count) continue;
                bool bothActive = sp.AllocatedNodes.Contains(_allNodes[fromIdx].Id) && sp.AllocatedNodes.Contains(_allNodes[toIdx].Id);
                bool oneActive = sp.AllocatedNodes.Contains(_allNodes[fromIdx].Id) || sp.AllocatedNodes.Contains(_allNodes[toIdx].Id);
                _connectionElements[i].LineColor = bothActive ? new Color(245, 196, 81, 200) : oneActive ? new Color(160, 120, 70, 120) : new Color(50, 40, 70, 60);
            }
        }

        private bool CanAllocate(PoESkillNode node, ShardPlayer sp)
        {
            if (sp.AllocatedNodes.Contains(node.Id)) return false;
            if (node.Id == "start" || node.Cost == 0) return true;
            foreach (var aid in sp.AllocatedNodes)
                if (node.Connections.Contains(aid)) return true;
            return false;
        }

        private void OnNodeClick(PoESkillNode node)
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;
            if (sp.AllocatedNodes.Contains(node.Id))
            {
                sp.AllocatedNodes.Remove(node.Id);
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
            }
            else if (CanAllocate(node, sp))
            {
                int avail = sp.AvailableSkillPoints();
                if (avail < node.Cost)
                {
                    Main.NewText($"Necesitas {node.Cost} pts, tienes {avail}.", new Color(255, 120, 120));
                }
                else
                {
                    sp.AllocatedNodes.Add(node.Id);
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
                }
            }
            UpdateNodeStates();
            // Actualizar texto de puntos
            if (_pointsText != null && sp != null)
            {
                _pointsText.SetText($"★ ARBOL DE HABILIDADES ★  |  Puntos: {sp.AvailableSkillPoints()}  |  Nivel {sp.ShardLevel}", 0.9f);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _time += 0.016f;

            // Drag (pan)
            if (_isDragging)
            {
                _offset = new Vector2(Main.mouseX, Main.mouseY) - _dragStart;
                UpdatePositions();
            }

            // Actualizar estados de nodos
            UpdateNodeStates();

            // Cerrar con Esc
            if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape) &&
                !Main.oldKeyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
            {
                Hide();
            }
        }
    }
}
