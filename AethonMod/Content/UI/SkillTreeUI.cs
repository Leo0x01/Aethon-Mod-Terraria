using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// Vista del arbol de habilidades — nodos pequeños e interactivos,
    /// fondo cosmico artístico (textura PNG), layout limpio sin texto superpuesto.
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

        // Textura de fondo cosmico (PNG cargado)
        private Texture2D? _bgTexture;

        // Texturas de nodos circulares (PNG)
        private Texture2D? _nodeSmallTex;
        private Texture2D? _nodeNotableTex;
        private Texture2D? _nodeKeystoneTex;
        private Texture2D? _nodeAscendancyTex;

        private PoESkillNode? _hoveredNode;

        public SkillTreeView()
        {
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
            // Zoom inicial ajustado para que todo quepa
            _zoom = 0.7f;
            _panOffset = Vector2.Zero;
        }

        private void LoadBgTexture()
        {
            if (_bgTexture != null) return;
            try
            {
                _bgTexture = ModContent.Request<Texture2D>("AethonMod/Content/UI/Textures/SkillTree_Background", AssetRequestMode.ImmediateLoad).Value;
            }
            catch
            {
                _bgTexture = null;
            }
        }

        private void LoadNodeTextures()
        {
            if (_nodeSmallTex != null) return;
            try
            {
                _nodeSmallTex = ModContent.Request<Texture2D>("AethonMod/Content/UI/Textures/Node_Small", AssetRequestMode.ImmediateLoad).Value;
                _nodeNotableTex = ModContent.Request<Texture2D>("AethonMod/Content/UI/Textures/Node_Notable", AssetRequestMode.ImmediateLoad).Value;
                _nodeKeystoneTex = ModContent.Request<Texture2D>("AethonMod/Content/UI/Textures/Node_Keystone", AssetRequestMode.ImmediateLoad).Value;
                _nodeAscendancyTex = ModContent.Request<Texture2D>("AethonMod/Content/UI/Textures/Node_Ascendancy", AssetRequestMode.ImmediateLoad).Value;
            }
            catch
            {
                _nodeSmallTex = null;
            }
        }

        private Texture2D? GetNodeTexture(NodeType type)
        {
            LoadNodeTextures();
            return type switch
            {
                NodeType.Small => _nodeSmallTex,
                NodeType.Notable => _nodeNotableTex,
                NodeType.Keystone => _nodeKeystoneTex,
                NodeType.Ascendancy => _nodeAscendancyTex,
                NodeType.Cluster => _nodeNotableTex,
                _ => _nodeSmallTex,
            };
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _time += 0.016f;
            LoadBgTexture();
            HandleInput();
        }

        private void HandleInput()
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            var dims = GetDimensions();
            Rectangle viewRect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
            bool mouseInView = viewRect.Contains(Main.mouseX, Main.mouseY);

            // Pan con click derecho
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

            // Zoom con rueda — SOLO cuando el mouse esta en la vista Y consumir el delta
            // para que no afecte el inventario
            int curScroll = Terraria.GameInput.PlayerInput.ScrollWheelValue;
            int scrollDelta = curScroll - _lastScrollValue;
            _lastScrollValue = curScroll;
            if (scrollDelta != 0 && mouseInView)
            {
                float zoomDelta = scrollDelta > 0 ? 0.15f : -0.15f;
                _zoom = MathHelper.Clamp(_zoom + zoomDelta, 0.4f, 3.0f);
                // Consumir el scroll para que no afecte el inventario
                Terraria.GameInput.PlayerInput.ScrollWheelValue = Terraria.GameInput.PlayerInput.ScrollWheelValueOld;
            }

            // === PROCESAR CLICK EN NODOS AQUI (en Update, no en Draw) ===
            // Encontrar nodo hovered
            _hoveredNode = FindHoveredNode(viewRect);

            // Click izquierdo para asignar/quitar
            if (Main.mouseLeft && Main.mouseLeftRelease && _hoveredNode != null && !_isPanning && mouseInView)
            {
                var node = _hoveredNode;
                if (sp.AllocatedNodes.Contains(node.Id))
                {
                    sp.AllocatedNodes.Remove(node.Id);
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
                }
                else if (CanAllocate(node, sp))
                {
                    int availPts = sp.CumulativeSkillPoints() - sp.AllocatedNodes.Count;
                    if (availPts < node.Cost)
                    {
                        Main.NewText($"Necesitas {node.Cost} pts, tienes {availPts}.", new Color(255, 120, 120));
                    }
                    else
                    {
                        sp.AllocatedNodes.Add(node.Id);
                        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
                    }
                }
            }

            // Bloquear input del juego dentro de la vista (pero NO mouseLeft — los nodos lo necesitan)
            if (mouseInView)
            {
                Main.mouseRight = false;
            }
        }

        private Vector2 NodeToScreen(PoESkillNode node, Rectangle viewRect)
        {
            float cx = viewRect.X + viewRect.Width / 2f;
            float cy = viewRect.Y + viewRect.Height / 2f;
            if (_rangeX <= 0 || _rangeY <= 0) return new Vector2(cx, cy);
            float scale = Math.Min(viewRect.Width / _rangeX, viewRect.Height / _rangeY) * 0.35f * _zoom;
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
                // Radio de click pequeño (nodos son pequeños)
                float clickRadius = GetNodeDrawRadius(node) + 4f;
                if (dist < clickRadius && dist < closestDist) { closestDist = dist; closest = node; }
            }
            return closest;
        }

        /// <summary>Radio de dibujo del nodo: pequeño pero visible (circular).</summary>
        private float GetNodeDrawRadius(PoESkillNode node)
        {
            return node.Type switch
            {
                NodeType.Small => 7f,        // pequeño
                NodeType.Notable => 11f,     // mediano
                NodeType.Keystone => 15f,     // grande
                NodeType.Ascendancy => 13f,  // mediano-grande
                NodeType.Cluster => 9f,
                _ => 7f,
            };
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

            // === FONDO ARTÍSTICO (textura PNG cósmica) ===
            // El DraggablePanel ya dibuja el fondo del panel. Aqui solo dibujamos
            // la textura cosmica SEMI-TRANSPARENTE para no crear una superposicion opaca.
            if (_bgTexture != null)
            {
                // Textura cosmica semi-transparente (alpha 180, no 255)
                sb.Draw(_bgTexture, viewRect, new Color(255, 255, 255, 120));
            }

            // Estrellas animadas (sutiles, encima del fondo)
            DrawAnimatedStars(sb, viewRect);

            if (_allNodes.Count == 0)
            {
                Utils.DrawBorderString(sb, "No hay arbol disponible.",
                    new Vector2(viewRect.X + viewRect.Width / 2f, viewRect.Y + viewRect.Height / 2f),
                    new Color(255, 200, 100), 0.9f, 0.5f, 0.5f);
                return;
            }

            // === CONEXIONES (líneas finas) ===
            foreach (var (fromIdx, toIdx) in _allConnections)
            {
                if (fromIdx < 0 || fromIdx >= _allNodes.Count || toIdx < 0 || toIdx >= _allNodes.Count) continue;
                Vector2 from = NodeToScreen(_allNodes[fromIdx], viewRect);
                Vector2 to = NodeToScreen(_allNodes[toIdx], viewRect);
                bool bothActive = sp.AllocatedNodes.Contains(_allNodes[fromIdx].Id) && sp.AllocatedNodes.Contains(_allNodes[toIdx].Id);
                bool oneActive = sp.AllocatedNodes.Contains(_allNodes[fromIdx].Id) || sp.AllocatedNodes.Contains(_allNodes[toIdx].Id);
                Color lc = bothActive ? new Color(245, 196, 81, 200) : oneActive ? new Color(160, 120, 70, 120) : new Color(50, 40, 70, 60);
                float th = bothActive ? 2f : oneActive ? 1.5f : 1f;
                DrawLine(sb, from, to, lc, th);
            }

            // === NODOS (pequeños, circulares, con glow) ===
            foreach (var node in _allNodes)
            {
                Vector2 pos = NodeToScreen(node, viewRect);
                bool allocated = sp.AllocatedNodes.Contains(node.Id);
                bool canAlloc = CanAllocate(node, sp);
                bool isHover = _hoveredNode != null && _hoveredNode.Id == node.Id;
                DrawNode(sb, pos, node, allocated, canAlloc, isHover);
            }

            // === INFO BAR INFERIOR ===
            int avail = sp.CumulativeSkillPoints() - sp.AllocatedNodes.Count;
            string infoText = $"Puntos: {avail}  |  Asignados: {sp.AllocatedNodes.Count}/{sp.CumulativeSkillPoints()}  |  Click izq: asignar  |  Click der: mover  |  Rueda: zoom";
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(viewRect.X, viewRect.Bottom - 22, viewRect.Width, 22),
                new Color(8, 6, 16, 200));
            Utils.DrawBorderString(sb, infoText,
                new Vector2(viewRect.X + viewRect.Width / 2f, viewRect.Bottom - 16),
                new Color(140, 130, 170), 0.7f, 0.5f, 0.5f);

            // === TOOLTIP (solo si hay nodo hovered) ===
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

        /// <summary>Dibuja un nodo PEQUEÑO circular con glow (no mas cuadros gigantes).</summary>
        private void DrawNode(SpriteBatch sb, Vector2 pos, PoESkillNode node, bool allocated, bool canAlloc, bool hovered)
        {
            float radius = GetNodeDrawRadius(node);

            // Color por tipo
            Color baseColor = node.Type switch
            {
                NodeType.Small => new Color(100, 90, 130),
                NodeType.Notable => new Color(70, 130, 200),
                NodeType.Keystone => new Color(245, 196, 81),
                NodeType.Ascendancy => new Color(150, 60, 230),
                _ => new Color(100, 90, 130),
            };

            if (!allocated && !canAlloc) baseColor *= 0.25f;

            // Glow pulsante para keystones/ascendancy
            if (node.Type == NodeType.Keystone || node.Type == NodeType.Ascendancy)
            {
                float pulse = 0.7f + 0.3f * (float)Math.Sin(_time * 2f + pos.X * 0.01f);
                for (int i = 3; i > 0; i--)
                {
                    int gr = (int)(radius + i * 4);
                    int a = (int)((allocated ? 8 : 4) * pulse);
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(baseColor.R, baseColor.G, baseColor.B, a));
                }
            }

            // Glow dorado si asignado
            if (allocated)
                for (int i = 2; i > 0; i--)
                {
                    int gr = (int)(radius + i * 3);
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(245, 196, 81, 8 - i * 2));
                }

            // Glow blanco al hover
            if (hovered && (canAlloc || allocated))
                for (int i = 2; i > 0; i--)
                {
                    int gr = (int)(radius + i * 3);
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(255, 255, 255, 6 - i * 2));
                }

            // === NODO CIRCULAR (usando textura PNG, no cuadrados) ===
            Texture2D? nodeTex = GetNodeTexture(node.Type);
            if (nodeTex != null)
            {
                // Color de tintado segun estado
                Color tintColor = allocated ? new Color(255, 225, 140)
                                : canAlloc ? baseColor
                                : baseColor * 0.2f;
                if (!allocated && !canAlloc) tintColor *= 0.3f;

                // Escalar la textura al tamaño del nodo
                float texScale = (radius * 2f) / nodeTex.Width;
                Vector2 texOrigin = new Vector2(nodeTex.Width / 2f, nodeTex.Height / 2f);
                sb.Draw(nodeTex, pos, null, tintColor, 0f, texOrigin, texScale, SpriteEffects.None, 0f);
            }
            else
            {
                // Fallback: DrawCircle si la textura no carga
                DrawCircle(sb, pos, radius, allocated ? new Color(255, 225, 140) : (canAlloc ? baseColor : baseColor * 0.1f));
            }

            // Nombre SOLO para notable/keystone/ascendancy (no para small)
            // Y solo si NO se solapa con otros (mostrar solo si hovered o allocated)
            if (node.Type == NodeType.Notable || node.Type == NodeType.Keystone || node.Type == NodeType.Ascendancy)
            {
                bool showLabel = hovered || allocated;
                if (showLabel)
                {
                    string shortName = node.Name.Length > 14 ? node.Name.Substring(0, 12) + "…" : node.Name;
                    // Fondo del label
                    var textSize = FontAssets.MouseText.Value.MeasureString(shortName);
                    int labelW = (int)textSize.X + 8;
                    int labelH = 16;
                    int lx = (int)(pos.X - labelW / 2f);
                    int ly = (int)(pos.Y - radius - 18);
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(lx, ly, labelW, labelH),
                        new Color(10, 8, 20, 220));
                    Color labelBorder = allocated ? new Color(245, 196, 81, 150) : new Color(179, 136, 255, 150);
                    sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(lx, ly, labelW, 1), labelBorder);
                    sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(lx, ly + labelH - 1, labelW, 1), labelBorder);
                    Utils.DrawBorderString(sb, shortName,
                        new Vector2(pos.X, ly + 4),
                        allocated ? new Color(255, 240, 190) : new Color(220, 200, 240), 0.6f, 0.5f, 0f);
                }
            }
        }

        /// <summary>Dibuja un círculo relleno (nodos pequeños, no cuadros).</summary>
        private void DrawCircle(SpriteBatch sb, Vector2 center, float radius, Color color)
        {
            int r = (int)radius;
            if (r < 1) return;
            // Dibujar como puntos en un patron circular
            for (int dy = -r; dy <= r; dy++)
            {
                int dx = (int)Math.Sqrt(r * r - dy * dy);
                int w = dx * 2 + 1;
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)center.X - dx, (int)center.Y + dy, w, 1), color);
            }
        }

        /// <summary>Dibuja el borde de un círculo.</summary>
        private void DrawCircleOutline(SpriteBatch sb, Vector2 center, float radius, Color color, float thickness)
        {
            int r = (int)radius;
            if (r < 1) return;
            // Dibujar circulo como puntos en el perimetro
            int steps = Math.Max(8, r * 4);
            for (int i = 0; i < steps; i++)
            {
                float angle = (float)(i * Math.PI * 2 / steps);
                float px = center.X + (float)Math.Cos(angle) * radius;
                float py = center.Y + (float)Math.Sin(angle) * radius;
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)px, (int)py, (int)thickness, (int)thickness), color);
            }
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
                _ => "",
            };

            string[] lines = {
                $"{typeStr} {node.Name}",
                node.Effect ?? "",
                $"Coste: {node.Cost} pts",
                alloc ? "(Asignado — click para quitar)" : (CanAllocate(node, sp) ? "(Click para asignar)" : "(Requiere nodo adyacente)"),
            };

            float maxW = 0;
            foreach (var line in lines)
            {
                var size = FontAssets.MouseText.Value.MeasureString(line);
                if (size.X > maxW) maxW = size.X;
            }
            float lineH = 16f;
            int padX = 10, padY = 8;
            int tw = (int)maxW + padX * 2;
            int th = (int)(lines.Length * lineH) + padY * 2;

            int tx = Main.mouseX + 16;
            int ty = Main.mouseY + 16;
            if (tx + tw > Main.screenWidth) tx = Main.mouseX - tw - 8;
            if (ty + th > Main.screenHeight) ty = Main.screenHeight - th - 4;
            if (ty < 0) ty = 4;

            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(tx, ty, tw, th), new Color(15, 10, 30, 245));
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

        private void DrawAnimatedStars(SpriteBatch sb, Rectangle rect)
        {
            // Estrellas animadas sutiles (pocas para no saturar)
            for (int i = 0; i < 30; i++)
            {
                int seed = i * 73856093;
                float bx = (seed % 1000) / 1000f * rect.Width;
                float by = ((seed * 19349663) % 1000) / 1000f * rect.Height;
                float sx = rect.X + bx;
                float sy = rect.Y + by;
                float twinkle = 0.4f + 0.6f * (float)Math.Sin(_time * 1.5f + i * 0.3f);
                int alpha = (int)(100 * twinkle);
                if (alpha < 0) alpha = 0; if (alpha > 255) alpha = 255;
                Color c = i % 3 == 0 ? new Color(245, 196, 81, alpha)
                        : i % 3 == 1 ? new Color(179, 136, 255, alpha)
                        : new Color(200, 220, 255, alpha);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)sx, (int)sy, 1, 1), c);
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
    }

    /// <summary>
    /// Estado del arbol de habilidades — UIState que contiene un DraggablePanel
    /// con el SkillTreeView dentro.
    /// </summary>
    public class SkillTreeUIState : UIState
    {
        public bool IsVisible = false;
        private DraggablePanel? _panel;
        private SkillTreeView? _treeView;

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
            _panel.OnCloseClick += () => Hide();

            _treeView = new SkillTreeView();
            _treeView.Width.Set(0, 1f);
            _treeView.Height.Set(0, 1f);
            _treeView.Top.Set(44, 0f);

            var tree = PoETreeCatalog.GetTree(sp.ActiveBranch);
            _treeView.SetTree(tree);

            _panel.Append(_treeView);
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
    }
}
