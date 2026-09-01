using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.UI
{
    // SkillPanel — nodo clickeable (UIPanel, igual que AnRPG Shared.cs)
    class SkillPanel : UIPanel
    {
        public int NodeIndex;
        public Vector2 BasePos;
        public Color NodeColor = Color.White;
        private float _time;

        public SkillPanel(int nodeIdx, Vector2 basePos)
        {
            NodeIndex = nodeIdx;
            BasePos = basePos;
            float size = 40;
            Width.Set(size, 0f);
            Height.Set(size, 0f);
            SetPadding(0);
            BackgroundColor = new Color(0, 0, 0, 0);
            BorderColor = new Color(0, 0, 0, 0);
        }

        public void SetColor(Color c, float time) { NodeColor = c; _time = time; }

        protected override void DrawSelf(SpriteBatch sb)
        {
            var dims = GetDimensions();
            Vector2 pos = dims.Center();
            float r = Math.Min(dims.Width, dims.Height) / 2f * 0.7f;
            var node = SkillTreeCatalog.Nodes[NodeIndex];

            // Glow para keystones/ascendancy
            if (node.Type == SkillNodeType.Keystone || node.Type == SkillNodeType.Ascendancy)
            {
                float pulse = 0.7f + 0.3f * (float)Math.Sin(_time * 2f + pos.X * 0.01f);
                for (int i = 3; i > 0; i--)
                {
                    int gr = (int)(r + i * 5);
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(NodeColor.R, NodeColor.G, NodeColor.B, (int)(8 * pulse)));
                }
            }

            // Círculo
            DrawCircle(sb, pos, r, NodeColor);

            // Borde
            Color border = IsMouseHovering ? Color.White : new Color(NodeColor.R + 40, NodeColor.G + 40, NodeColor.B + 40);
            DrawCircleOutline(sb, pos, r, border);

            // Label si hover
            if (IsMouseHovering)
            {
                string name = node.Name.Length > 16 ? node.Name.Substring(0, 14) + "…" : node.Name;
                var ts = FontAssets.MouseText.Value.MeasureString(name);
                int lw = (int)ts.X + 8, lh = 16;
                int lx = (int)(pos.X - lw / 2f), ly = (int)(pos.Y - r - 18);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(lx, ly, lw, lh), new Color(10, 8, 20, 230));
                Utils.DrawBorderString(sb, name, new Vector2(pos.X, ly + 3),
                    new Color(245, 196, 81), 0.6f, 0.5f, 0f);
            }
        }

        void DrawCircle(SpriteBatch sb, Vector2 c, float r, Color col)
        {
            int ir = (int)r; if (ir < 1) return;
            for (int dy = -ir; dy <= ir; dy++)
            {
                int dx = (int)Math.Sqrt(ir * ir - dy * dy);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)c.X - dx, (int)c.Y + dy, dx * 2 + 1, 1), col);
            }
        }

        void DrawCircleOutline(SpriteBatch sb, Vector2 c, float r, Color col)
        {
            int ir = (int)r; if (ir < 1) return;
            int steps = Math.Max(8, ir * 4);
            for (int i = 0; i < steps; i++)
            {
                float a = (float)(i * Math.PI * 2 / steps);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)(c.X + Math.Cos(a) * r), (int)(c.Y + Math.Sin(a) * r), 2, 2), col);
            }
        }
    }

    // Connection — línea entre nodos (UIElement, igual que AnRPG Shared.cs)
    class Connection : UIElement
    {
        public Vector2 BasePos;
        public float Rotation;
        public Color LineColor = Color.Gray;

        public Connection(float rot, float dist)
        {
            Width.Set(dist, 0f);
            Height.Set(3f, 0f);
            Rotation = rot;
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            var dims = GetDimensions();
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height),
                null, LineColor, Rotation, new Vector2(0, dims.Height / 2f), SpriteEffects.None, 0f);
        }
    }

    // SkillTreeUIState — siguiendo AnRPG SkillTreeUi.cs exactamente
    public class SkillTreeUIState : UIState
    {
        public bool IsVisible = false;

        private UIPanel _bg;
        private List<SkillPanel> _panels = new();
        private List<Connection> _connections = new();
        private UIText _title;
        private UIText _closeBtn;

        private float Zoom = 0.6f;
        private float sizeMult = 1f;
        private Vector2 offSet;
        private bool dragging = false;
        private Vector2 dragStart;
        private float _time;

        public void Show()
        {
            if (IsVisible) { Hide(); return; }
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted)
            {
                Main.NewText("El Fragmento Genesis aun no tiene una rama.", new Color(180, 160, 220));
                return;
            }
            if (sp.SkillNodes == null) sp.SkillNodes = SkillTreeCatalog.GetInitialState();
            Zoom = 0.6f;
            offSet = new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            Init();
            IsVisible = true;
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuOpen);
        }

        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            dragging = false;
            Erase();
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
        }

        private void Erase()
        {
            if (_bg != null) { _bg.RemoveAllChildren(); _bg.Remove(); }
            _panels.Clear(); _connections.Clear();
        }

        private void Init()
        {
            Erase();
            sizeMult = Zoom;

            _bg = new UIPanel();
            _bg.SetPadding(0);
            _bg.Left.Set(0, 0f); _bg.Top.Set(0, 0f);
            _bg.Width.Set(Main.screenWidth, 0f); _bg.Height.Set(Main.screenHeight, 0f);
            _bg.BackgroundColor = new Color(8, 6, 16, 200);
            _bg.BorderColor = new Color(0, 0, 0, 0);
            _bg.OnLeftMouseDown += (evt, el) => { dragging = true; dragStart = evt.MousePosition; };
            _bg.OnLeftMouseUp += (evt, el) => { dragging = false; };
            _bg.OnScrollWheel += (UIScrollWheelEvent evt, UIElement el) => {
                float preZoom = Zoom;
                if (evt.ScrollWheelValue > 0) Zoom = MathHelper.Clamp(1.1f * Zoom, 0.2f, 2f);
                else Zoom = MathHelper.Clamp(0.85f * Zoom, 0.2f, 2f);
                offSet /= Zoom / preZoom;
                Init();
            };
            Append(_bg);

            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            _title = new UIText($"★ ARBOL DE HABILIDADES ★  |  Puntos: {sp?.AvailableSkillPoints() ?? 0}  |  Nivel {sp?.ShardLevel ?? 1}", 0.85f);
            _title.Left.Set(Main.screenWidth / 2f - 200, 0f); _title.Top.Set(15, 0f);
            _title.TextColor = new Color(245, 196, 81);
            _bg.Append(_title);

            _closeBtn = new UIText("X", 1.2f);
            _closeBtn.Left.Set(Main.screenWidth - 50, 0f); _closeBtn.Top.Set(10, 0f);
            _closeBtn.TextColor = new Color(220, 80, 80);
            _closeBtn.OnMouseOver += (e, el) => _closeBtn.TextColor = Color.White;
            _closeBtn.OnMouseOut += (e, el) => _closeBtn.TextColor = new Color(220, 80, 80);
            _closeBtn.OnLeftClick += (e, el) => Hide();
            _bg.Append(_closeBtn);

            // Crear nodos
            for (int i = 0; i < SkillTreeCatalog.Nodes.Length; i++)
            {
                var node = SkillTreeCatalog.Nodes[i];
                var panel = new SkillPanel(i, new Vector2(node.PosX, node.PosY));
                panel.OnLeftClick += (evt, el) => OnNodeClick(i);
                _bg.Append(panel);
                _panels.Add(panel);

                // Crear conexiones
                foreach (int ni in node.Neighbors)
                {
                    if (ni <= i || ni >= SkillTreeCatalog.Nodes.Length) continue;
                    var n2 = SkillTreeCatalog.Nodes[ni];
                    Vector2 p1 = new(node.PosX, node.PosY), p2 = new(n2.PosX, n2.PosY);
                    float angle = (float)Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                    float dist = Vector2.Distance(p1, p2);
                    var conn = new Connection(angle, dist);
                    conn.BasePos = p1;
                    _bg.Append(conn);
                    _connections.Add(conn);
                }
            }

            UpdatePositions();
            UpdateColors();
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsVisible) return;
            _time += 0.016f;

            // Drag continuo (igual que AnRPG.Update)
            for (int i = 0; i < _panels.Count; i++)
            {
                _panels[i].Left.Set((_panels[i].BasePos.X + offSet.X) * sizeMult - _panels[i].Width.Pixels / 2f, 0);
                _panels[i].Top.Set((_panels[i].BasePos.Y + offSet.Y) * sizeMult - _panels[i].Height.Pixels / 2f, 0);
            }
            for (int i = 0; i < _connections.Count; i++)
            {
                _connections[i].Left.Set((_connections[i].BasePos.X + offSet.X) * sizeMult, 0);
                _connections[i].Top.Set((_connections[i].BasePos.Y + offSet.Y) * sizeMult, 0);
            }
            Recalculate();
            UpdateColors();

            if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape)) Hide();
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            Vector2 mouse = new(Main.mouseX, Main.mouseY);
            if (_bg != null && _bg.ContainsPoint(mouse))
                Main.LocalPlayer.mouseInterface = true;
            if (dragging)
            {
                offSet.X += mouse.X - dragStart.X;
                offSet.Y += mouse.Y - dragStart.Y;
                dragStart = mouse;
                Recalculate();
            }
        }

        private void UpdatePositions()
        {
            for (int i = 0; i < _panels.Count; i++)
            {
                _panels[i].Left.Set((_panels[i].BasePos.X + offSet.X) * sizeMult - _panels[i].Width.Pixels / 2f, 0);
                _panels[i].Top.Set((_panels[i].BasePos.Y + offSet.Y) * sizeMult - _panels[i].Height.Pixels / 2f, 0);
            }
            for (int i = 0; i < _connections.Count; i++)
            {
                _connections[i].Left.Set((_connections[i].BasePos.X + offSet.X) * sizeMult, 0);
                _connections[i].Top.Set((_connections[i].BasePos.Y + offSet.Y) * sizeMult, 0);
            }
            Recalculate();
        }

        private void UpdateColors()
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null || sp.SkillNodes == null) return;
            for (int i = 0; i < _panels.Count; i++)
            {
                int idx = _panels[i].NodeIndex;
                bool active = sp.SkillNodes[idx].Level > 0;
                bool can = SkillTreeCatalog.CanActivate(idx, sp.SkillNodes, sp.ShardLevel, sp.AvailableSkillPoints());
                Color c = active ? new Color(255, 225, 140, 255)
                       : can ? _panels[i].NodeColor = new Color(100, 130, 200, 255)
                       : new Color(50, 40, 70, 200);
                _panels[i].SetColor(c, _time);
            }
        }

        private void OnNodeClick(int idx)
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null || sp.SkillNodes == null) return;
            if (sp.SkillNodes[idx].Level > 0)
            {
                // Quitar nivel
                sp.SkillNodes[idx].Level--;
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
            }
            else if (SkillTreeCatalog.CanActivate(idx, sp.SkillNodes, sp.ShardLevel, sp.AvailableSkillPoints()))
            {
                sp.SkillNodes[idx].Level++;
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
            }
            UpdateColors();
            if (_title != null) _title.SetText($"★ ARBOL DE HABILIDADES ★  |  Puntos: {sp.AvailableSkillPoints()}  |  Nivel {sp.ShardLevel}");
        }
    }
}
