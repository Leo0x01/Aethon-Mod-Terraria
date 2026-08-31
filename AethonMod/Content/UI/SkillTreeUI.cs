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
    // ================================================================
    // SkillPanel — nodo clickeable (adaptado de AnRPG Shared.cs SkillPanel)
    // ================================================================
    class SkillPanel : UIPanel
    {
        public PoESkillNode node;
        public Vector2 basePos;
        private Color color = Color.White;
        private float _time;

        public SkillPanel()
        {
            SetPadding(0);
            Width.Set(48, 0f);
            Height.Set(48, 0f);
            BackgroundColor = new Color(0, 0, 0, 0);
            BorderColor = new Color(0, 0, 0, 0);
        }

        public void SetColor(Color c, float time) { color = c; _time = time; }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dims = GetDimensions();
            Vector2 center = dims.Center();
            float radius = Math.Min(dims.Width, dims.Height) / 2f * 0.8f;

            // Glow para keystones
            if (node.Type == NodeType.Keystone || node.Type == NodeType.Ascendancy)
            {
                float pulse = 0.7f + 0.3f * (float)Math.Sin(_time * 2f + center.X * 0.01f);
                for (int i = 3; i > 0; i--)
                {
                    int gr = (int)(radius + i * 5);
                    int a = (int)(8 * pulse);
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)center.X - gr, (int)center.Y - gr, gr * 2, gr * 2),
                        new Color(color.R, color.G, color.B, a));
                }
            }

            // Círculo
            DrawCircle(spriteBatch, center, radius, color);

            // Borde
            Color border = IsMouseHovering ? Color.White : new Color(color.R + 40, color.G + 40, color.B + 40);
            DrawCircleOutline(spriteBatch, center, radius, border);
        }

        private void DrawCircle(SpriteBatch sb, Vector2 c, float r, Color col)
        {
            int ir = (int)r; if (ir < 1) return;
            for (int dy = -ir; dy <= ir; dy++)
            {
                int dx = (int)Math.Sqrt(ir * ir - dy * dy);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)c.X - dx, (int)c.Y + dy, dx * 2 + 1, 1), col);
            }
        }

        private void DrawCircleOutline(SpriteBatch sb, Vector2 c, float r, Color col)
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

    // ================================================================
    // Connection — línea entre nodos (adaptado de AnRPG Shared.cs Connection)
    // ================================================================
    class Connection : UIElement
    {
        public Vector2 basePos;
        public float rotation;
        public Color color = Color.Gray;

        public Connection(float rot, float distance, float height)
        {
            Width.Set(distance, 0f);
            Height.Set(height, 0f);
            rotation = rot;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dims = GetDimensions();
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height),
                null, color, rotation, new Vector2(0, dims.Height / 2f), SpriteEffects.None, 0f);
        }
    }

    // ================================================================
    // SkillTreeUIState — adaptado directamente de AnRPG SkillTreeUi.cs
    // ================================================================
    public class SkillTreeUIState : UIState
    {
        public bool IsVisible = false;

        private UIPanel backGround;
        private List<Connection> allConnection = new();
        private List<SkillPanel> allBasePanel = new();
        private UIText pointsText;
        private UIText closeText;

        private float Zoom = 1f;
        private float zoomMin = 0.25f;
        private float zoomMax = 2f;
        private float sizeMultplier = 1f;

        private Vector2 offSet;
        private bool dragging = false;
        private Vector2 regOffSet;
        private float _time;

        private PoESkillTree tree;
        private List<PoESkillNode> allNodes = new();
        private List<(int fi, int ti)> connections = new();
        private Dictionary<string, int> nodeIdx = new();
        private float minX, maxX, minY, maxY, rangeX, rangeY;

        public void Show()
        {
            if (IsVisible) { Hide(); return; }
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted)
            {
                Main.NewText("El Fragmento Genesis aun no tiene una rama.", new Color(180, 160, 220));
                return;
            }
            tree = PoETreeCatalog.GetTree(sp.ActiveBranch);
            BuildIndex();
            if (!sp.AllocatedNodes.Contains("start")) sp.AllocatedNodes.Add("start");
            Zoom = 0.8f;
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
            if (backGround != null)
            {
                backGround.RemoveAllChildren();
                backGround.Remove();
            }
            allConnection.Clear();
            allBasePanel.Clear();
        }

        private void BuildIndex()
        {
            allNodes = tree.Nodes ?? new();
            connections.Clear(); nodeIdx.Clear();
            for (int i = 0; i < allNodes.Count; i++) nodeIdx[allNodes[i].Id] = i;
            foreach (var n in allNodes)
            {
                if (!nodeIdx.TryGetValue(n.Id, out int fi)) continue;
                foreach (var c in n.Connections)
                    if (nodeIdx.TryGetValue(c, out int j)) connections.Add((fi, j));
            }
            if (allNodes.Count == 0) { rangeX = rangeY = 1; return; }
            minX = float.MaxValue; maxX = float.MinValue; minY = float.MaxValue; maxY = float.MinValue;
            foreach (var n in allNodes) { minX = Math.Min(minX, n.X); maxX = Math.Max(maxX, n.X); minY = Math.Min(minY, n.Y); maxY = Math.Max(maxY, n.Y); }
            rangeX = Math.Max(1, maxX - minX + 1); rangeY = Math.Max(1, maxY - minY + 1);
        }

        private Vector2 NodeToBase(PoESkillNode n)
        {
            float s = 0.3f;
            return new Vector2((n.X - minX) * s - rangeX * s / 2f, (n.Y - minY) * s - rangeY * s / 2f);
        }

        // === INIT — igual que AnRPG.Init() ===
        private void Init()
        {
            Erase();
            sizeMultplier = Zoom;

            backGround = new UIPanel();
            backGround.SetPadding(0);
            backGround.Left.Set(0, 0f);
            backGround.Top.Set(0, 0f);
            backGround.Width.Set(Main.screenWidth, 0f);
            backGround.Height.Set(Main.screenHeight, 0f);
            backGround.BackgroundColor = new Color(8, 6, 16, 200);
            backGround.BorderColor = new Color(0, 0, 0, 0);
            // Drag — igual que AnRPG: OnMouseDown/OnMouseUp en el background
            backGround.OnLeftMouseDown += (evt, el) => { dragging = true; regOffSet = evt.MousePosition; };
            backGround.OnLeftMouseUp += (evt, el) => { dragging = false; };
            // Scroll — igual que AnRPG: OnScrollWheel
            backGround.OnScrollWheel += (UIScrollWheelEvent evt, UIElement el) => { ScrollZoom(evt); };
            Append(backGround);

            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            int avail = sp?.AvailableSkillPoints() ?? 0;
            pointsText = new UIText($"★ ARBOL DE HABILIDADES ★  |  Puntos: {avail}  |  Nivel {sp?.ShardLevel ?? 1}", 0.85f);
            pointsText.Left.Set(Main.screenWidth / 2f - 200, 0f);
            pointsText.Top.Set(15, 0f);
            pointsText.TextColor = new Color(245, 196, 81);
            backGround.Append(pointsText);

            closeText = new UIText("X", 1.2f);
            closeText.Left.Set(Main.screenWidth - 50, 0f);
            closeText.Top.Set(10, 0f);
            closeText.TextColor = new Color(220, 80, 80);
            closeText.OnMouseOver += (e, el) => closeText.TextColor = Color.White;
            closeText.OnMouseOut += (e, el) => closeText.TextColor = new Color(220, 80, 80);
            closeText.OnLeftClick += (e, el) => Hide();
            backGround.Append(closeText);

            // Nodos — igual que AnRPG: SkillInit por cada nodo
            foreach (var node in allNodes)
                SkillInit(node);

            UpdatePositions();
            UpdateColors();
        }

        // === SkillInit — igual que AnRPG.SkillInit ===
        private void SkillInit(PoESkillNode node)
        {
            SkillPanel panel = new SkillPanel();
            panel.node = node;
            panel.basePos = NodeToBase(node);
            panel.OnLeftClick += (evt, el) => OnNodeClick(node);
            backGround.Append(panel);
            allBasePanel.Add(panel);

            // Conexiones — igual que AnRPG.DrawConnection
            if (!nodeIdx.TryGetValue(node.Id, out int fi)) return;
            foreach (var connId in node.Connections)
            {
                if (!nodeIdx.TryGetValue(connId, out int ti)) continue;
                if (ti <= fi) continue; // evita duplicados
                Vector2 p1 = NodeToBase(node), p2 = NodeToBase(allNodes[ti]);
                float angle = (float)Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                float dist = Vector2.Distance(p1, p2);
                Connection bg = new Connection(angle, dist, 8) { color = Color.DarkSlateGray, basePos = p1 };
                Connection fg = new Connection(angle, dist, 4) { color = Color.Gray, basePos = p1 };
                allConnection.Add(bg);
                allConnection.Add(fg);
                backGround.Append(bg);
                backGround.Append(fg);
            }
        }

        // === ScrollZoom — igual que AnRPG.ScrollUpDown ===
        private void ScrollZoom(UIScrollWheelEvent evt)
        {
            float preZoom = Zoom;
            if (evt.ScrollWheelValue > 0)
                Zoom = MathHelper.Clamp(1.1f * Zoom, zoomMin, zoomMax);
            else
                Zoom = MathHelper.Clamp(0.85f * Zoom, zoomMin, zoomMax);
            float ratio = Zoom / preZoom;
            offSet /= ratio;
            Init();
        }

        // === Update — igual que AnRPG.Update ===
        public override void Update(GameTime gameTime)
        {
            if (!IsVisible) return;
            _time += 0.016f;

            // Actualizar posiciones (igual que AnRPG.Update)
            for (int i = 0; i < allConnection.Count; i++)
            {
                allConnection[i].Left.Set((allConnection[i].basePos.X + offSet.X) * sizeMultplier, 0);
                allConnection[i].Top.Set((allConnection[i].basePos.Y + offSet.Y) * sizeMultplier, 0);
                allConnection[i].Width.Set(allConnection[i].Width.Pixels, 0);
            }
            for (int i = 0; i < allBasePanel.Count; i++)
            {
                allBasePanel[i].Left.Set((allBasePanel[i].basePos.X + offSet.X) * sizeMultplier - allBasePanel[i].Width.Pixels / 2f, 0);
                allBasePanel[i].Top.Set((allBasePanel[i].basePos.Y + offSet.Y) * sizeMultplier - allBasePanel[i].Height.Pixels / 2f, 0);
            }
            Recalculate();

            UpdateColors();

            // Cerrar con Esc
            if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
                Hide();
        }

        // === DrawSelf — igual que AnRPG.DrawSelf (mouseInterface + drag) ===
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Vector2 mousePos = new Vector2(Main.mouseX, Main.mouseY);
            if (backGround != null && backGround.ContainsPoint(mousePos))
                Main.LocalPlayer.mouseInterface = true;

            if (dragging)
            {
                offSet.X += (mousePos.X - regOffSet.X);
                offSet.Y += (mousePos.Y - regOffSet.Y);
                regOffSet = mousePos;
                Recalculate();
            }
        }

        private void UpdatePositions()
        {
            for (int i = 0; i < allConnection.Count; i++)
            {
                allConnection[i].Left.Set((allConnection[i].basePos.X + offSet.X) * sizeMultplier, 0);
                allConnection[i].Top.Set((allConnection[i].basePos.Y + offSet.Y) * sizeMultplier, 0);
            }
            for (int i = 0; i < allBasePanel.Count; i++)
            {
                allBasePanel[i].Left.Set((allBasePanel[i].basePos.X + offSet.X) * sizeMultplier - allBasePanel[i].Width.Pixels / 2f, 0);
                allBasePanel[i].Top.Set((allBasePanel[i].basePos.Y + offSet.Y) * sizeMultplier - allBasePanel[i].Height.Pixels / 2f, 0);
            }
            Recalculate();
        }

        private void UpdateColors()
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;
            for (int i = 0; i < allBasePanel.Count; i++)
            {
                bool alloc = sp.AllocatedNodes.Contains(allBasePanel[i].node.Id);
                bool can = CanAlloc(allBasePanel[i].node, sp);
                Color c;
                if (alloc) c = new Color(255, 225, 140, 255);
                else if (can) c = allBasePanel[i].node.Type switch
                {
                    NodeType.Small => new Color(100, 110, 160, 255),
                    NodeType.Notable => new Color(70, 140, 220, 255),
                    NodeType.Keystone => new Color(245, 196, 81, 255),
                    NodeType.Ascendancy => new Color(180, 100, 250, 255),
                    _ => new Color(100, 110, 160, 255),
                };
                else c = new Color(50, 40, 70, 200);
                allBasePanel[i].SetColor(c, _time);
            }
            // Conexiones
            for (int i = 0; i < allConnection.Count; i += 2)
            {
                if (i + 1 >= allConnection.Count) break;
                int fi = -1, ti = -1;
                // Buscar qué nodos conecta esta conexión
                foreach (var (f, t) in connections)
                {
                    if (allConnection[i].basePos == NodeToBase(allNodes[f]))
                    {
                        fi = f; ti = t; break;
                    }
                }
                if (fi >= 0 && ti >= 0 && fi < allNodes.Count && ti < allNodes.Count)
                {
                    bool both = sp.AllocatedNodes.Contains(allNodes[fi].Id) && sp.AllocatedNodes.Contains(allNodes[ti].Id);
                    bool one = sp.AllocatedNodes.Contains(allNodes[fi].Id) || sp.AllocatedNodes.Contains(allNodes[ti].Id);
                    allConnection[i + 1].color = both ? new Color(245, 196, 81, 200) : one ? new Color(160, 120, 70, 120) : new Color(50, 40, 70, 60);
                }
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
            if (sp.AllocatedNodes.Contains(node.Id))
            {
                sp.AllocatedNodes.Remove(node.Id);
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
            }
            else if (CanAlloc(node, sp))
            {
                int av = sp.AvailableSkillPoints();
                if (av < node.Cost) Main.NewText($"Necesitas {node.Cost} pts, tienes {av}.", new Color(255, 120, 120));
                else { sp.AllocatedNodes.Add(node.Id); Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick); }
            }
            UpdateColors();
            if (pointsText != null) pointsText.SetText($"★ ARBOL DE HABILIDADES ★  |  Puntos: {sp.AvailableSkillPoints()}  |  Nivel {sp.ShardLevel}");
        }
    }
}
