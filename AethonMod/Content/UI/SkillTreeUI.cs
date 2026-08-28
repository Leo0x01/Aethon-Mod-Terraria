using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// Arbol de habilidades — rediseñado desde cero.
    /// Se dibuja DIRECTAMENTE con sb.Draw() (no UserInterface) para evitar
    /// los cuadros blancos que causaba el UserInterface.Draw().
    /// Patron: igual que BranchChoiceUI que SI funciona.
    /// </summary>
    public class SkillTreeUI
    {
        public bool IsVisible = false;
        private float _time = 0f;
        private float _zoom = 0.8f;
        private Vector2 _panOffset = Vector2.Zero;
        private bool _isPanning = false;
        private Vector2 _lastMouse = Vector2.Zero;
        private int _lastScrollValue = 0;
        private bool _mouseLeftPressed = false;

        private PoESkillTree? _tree;
        private List<PoESkillNode> _allNodes = new();
        private List<(int fromIdx, int toIdx)> _allConnections = new();
        private Dictionary<string, int> _nodeIndex = new();
        private float _minX, _maxX, _minY, _maxY, _rangeX, _rangeY;

        // Dimensiones de la ventana
        private const int WIN_W = 700;
        private const int WIN_H = 500;
        private const int TITLE_H = 40;

        private PoESkillNode? _hoveredNode;

        // Posicion de la ventana (centrada)
        private Rectangle WindowRect => new Rectangle(
            (Main.screenWidth - WIN_W) / 2,
            (Main.screenHeight - WIN_H) / 2,
            WIN_W, WIN_H);

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
            BuildNodeIndex();
            _zoom = 0.8f;
            _panOffset = Vector2.Zero;
            IsVisible = true;
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuOpen);
        }

        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            _isPanning = false;
            _mouseLeftPressed = false;
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
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
        }

        public void Update()
        {
            if (!IsVisible) return;
            _time += 0.016f;

            var winRect = WindowRect;
            bool mouseInWindow = winRect.Contains(Main.mouseX, Main.mouseY);

            // === PAN CON CLICK DERECHO ===
            if (Main.mouseRight && mouseInWindow)
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

            // === ZOOM CON RUEDA ===
            int curScroll = Terraria.GameInput.PlayerInput.ScrollWheelValue;
            int scrollDelta = curScroll - _lastScrollValue;
            _lastScrollValue = curScroll;
            if (scrollDelta != 0 && mouseInWindow)
            {
                float zoomDelta = scrollDelta > 0 ? 0.15f : -0.15f;
                _zoom = MathHelper.Clamp(_zoom + zoomDelta, 0.2f, 5.0f);
            }

            // Clamp el pan para que el arbol no se salga de la ventana
            ClampPanOffset(winRect);

            // === CLICK EN NODOS ===
            _hoveredNode = FindHoveredNode(winRect);
            if (Main.mouseLeft && Main.mouseLeftRelease && !_mouseLeftPressed && _hoveredNode != null && !_isPanning)
            {
                _mouseLeftPressed = true;
                var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
                if (sp != null)
                {
                    var node = _hoveredNode;
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
                }
            }
            if (!Main.mouseLeft) _mouseLeftPressed = false;

            // === BOTON CERRAR ===
            Rectangle closeRect = new Rectangle(winRect.Right - 36, winRect.Y + 4, 32, 32);
            if (closeRect.Contains(Main.mouseX, Main.mouseY) && Main.mouseLeft && Main.mouseLeftRelease && !_mouseLeftPressed)
            {
                Hide();
                return;
            }

            // === BLOQUEAR INTERACCION CON EL JUEGO (GLOBAL, no solo ventana) ===
            // Como el bestiario: toda interaccion con el juego se desactiva
            Main.mouseLeft = false;
            Main.mouseRight = false;
            Main.mouseLeftRelease = false;
            Main.mouseRightRelease = false;
            Terraria.GameInput.PlayerInput.ScrollWheelValue = Terraria.GameInput.PlayerInput.ScrollWheelValueOld;
        }

        public void Draw()
        {
            if (!IsVisible) return;
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            var sb = Main.spriteBatch;
            var winRect = WindowRect;

            // === FONDO OSCURO SEMI-TRANSPARENTE (como el bestiario) ===
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                new Color(0, 0, 0, 180));

            // === VENTANA ===
            // Sombra
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(winRect.X - 4, winRect.Y - 4, winRect.Width + 8, winRect.Height + 8),
                new Color(0, 0, 0, 100));
            // Fondo
            sb.Draw(TextureAssets.MagicPixel.Value, winRect, new Color(15, 10, 25, 240));

            // Estrellas de fondo
            DrawStars(sb, winRect);

            // === BORDE ===
            Color borderColor = new(179, 136, 255);
            int b = 2;
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(winRect.X, winRect.Y, winRect.Width, b), borderColor);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(winRect.X, winRect.Bottom - b, winRect.Width, b), borderColor);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(winRect.X, winRect.Y, b, winRect.Height), borderColor);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(winRect.Right - b, winRect.Y, b, winRect.Height), borderColor);

            // === BARRA DE TITULO ===
            Rectangle titleRect = new Rectangle(winRect.X + b, winRect.Y + b, winRect.Width - b * 2, TITLE_H);
            sb.Draw(TextureAssets.MagicPixel.Value, titleRect, new Color(20, 15, 40, 240));
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(titleRect.X, titleRect.Bottom, titleRect.Width, 2), new Color(245, 196, 81, 150));

            // Titulo centrado verticalmente en la barra
            string titleText = "★ ARBOL DE HABILIDADES ★";
            Utils.DrawBorderString(sb, titleText,
                new Vector2(titleRect.X + 14, titleRect.Y + 12), new Color(245, 196, 81), 0.9f);

            // Info de puntos (derecha) — misma alineacion vertical que el titulo
            int avail = sp.AvailableSkillPoints();
            string ptsText = $"Puntos: {avail}  |  Gastados: {sp.SpentSkillPoints()}";
            // Medir el texto para alinearlo a la derecha
            var ptsSize = FontAssets.MouseText.Value.MeasureString(ptsText) * 0.75f;
            Utils.DrawBorderString(sb, ptsText,
                new Vector2(titleRect.Right - ptsSize.X - 8, titleRect.Y + 14),
                avail > 0 ? new Color(120, 255, 150) : new Color(180, 180, 200), 0.75f);

            // === BOTON CERRAR ===
            Rectangle closeRect = new Rectangle(winRect.Right - 36, winRect.Y + 4, 32, 32);
            bool closeHover = closeRect.Contains(Main.mouseX, Main.mouseY);
            sb.Draw(TextureAssets.MagicPixel.Value, closeRect, closeHover ? new Color(220, 80, 80, 230) : new Color(40, 20, 30, 180));
            // X con lineas
            DrawX(sb, closeRect, closeHover ? Color.White : new Color(220, 180, 180), 2f);

            // === AREA DEL ARBOL ===
            Rectangle treeRect = new Rectangle(winRect.X + b, winRect.Y + TITLE_H + b, winRect.Width - b * 2, winRect.Height - TITLE_H - b * 2 - 24);

            if (_allNodes.Count > 0)
            {
                // Conexiones
                foreach (var (fromIdx, toIdx) in _allConnections)
                {
                    if (fromIdx < 0 || fromIdx >= _allNodes.Count || toIdx < 0 || toIdx >= _allNodes.Count) continue;
                    Vector2 from = NodeToScreen(_allNodes[fromIdx], treeRect);
                    Vector2 to = NodeToScreen(_allNodes[toIdx], treeRect);
                    bool bothActive = sp.AllocatedNodes.Contains(_allNodes[fromIdx].Id) && sp.AllocatedNodes.Contains(_allNodes[toIdx].Id);
                    bool oneActive = sp.AllocatedNodes.Contains(_allNodes[fromIdx].Id) || sp.AllocatedNodes.Contains(_allNodes[toIdx].Id);
                    Color lc = bothActive ? new Color(245, 196, 81, 200) : oneActive ? new Color(160, 120, 70, 120) : new Color(50, 40, 70, 60);
                    float th = bothActive ? 2f : oneActive ? 1.5f : 1f;
                    DrawLine(sb, from, to, lc, th);
                }

                // Nodos
                foreach (var node in _allNodes)
                {
                    Vector2 pos = NodeToScreen(node, treeRect);
                    bool allocated = sp.AllocatedNodes.Contains(node.Id);
                    bool canAlloc = CanAllocate(node, sp);
                    bool isHover = _hoveredNode != null && _hoveredNode.Id == node.Id;
                    DrawNode(sb, pos, node, allocated, canAlloc, isHover);
                }
            }

            // === TOOLTIP ===
            if (_hoveredNode != null)
            {
                DrawTooltip(sb, sp);
            }

            // === BARRA INFERIOR ===
            Rectangle footerRect = new Rectangle(winRect.X + b, winRect.Bottom - b - 22, winRect.Width - b * 2, 22);
            sb.Draw(TextureAssets.MagicPixel.Value, footerRect, new Color(8, 6, 16, 200));
            Utils.DrawBorderString(sb, "Click izq: asignar  |  Click der: mover  |  Rueda: zoom  |  K/Esc: cerrar",
                new Vector2(footerRect.X + footerRect.Width / 2f, footerRect.Y + 6),
                new Color(140, 130, 170), 0.7f, 0.5f, 0f);

            // === CERRAR SOLO CON ESC ===
            // NOTA: El toggle K/J ya se maneja en UISystem.PostUpdateInput.
            // Si lo cerramos aqui tambien, se cierra en el mismo frame que se abre.
            var kb = Main.keyState;
            var oldKb = Main.oldKeyState;
            if (kb.IsKeyDown(Keys.Escape) && !oldKb.IsKeyDown(Keys.Escape)) Hide();
        }

        private Vector2 NodeToScreen(PoESkillNode node, Rectangle treeRect)
        {
            float cx = treeRect.X + treeRect.Width / 2f;
            float cy = treeRect.Y + treeRect.Height / 2f;
            if (_rangeX <= 0 || _rangeY <= 0) return new Vector2(cx, cy);
            float scale = Math.Min(treeRect.Width / _rangeX, treeRect.Height / _rangeY) * 0.35f * _zoom;
            return new Vector2(
                (node.X - _minX) * scale - (_rangeX * scale / 2f) + cx + _panOffset.X,
                (node.Y - _minY) * scale - (_rangeY * scale / 2f) + cy + _panOffset.Y);
        }

        /// <summary>Clamp el pan offset para que el arbol no se salga de la ventana.</summary>
        private void ClampPanOffset(Rectangle treeRect)
        {
            if (_rangeX <= 0 || _rangeY <= 0) return;
            float scale = Math.Min(treeRect.Width / _rangeX, treeRect.Height / _rangeY) * 0.35f * _zoom;
            float treeW = _rangeX * scale;
            float treeH = _rangeY * scale;
            // Si el arbol es mas grande que la ventana, permitir pan dentro de limites
            if (treeW > treeRect.Width)
            {
                float maxPanX = (treeW - treeRect.Width) / 2f;
                _panOffset.X = MathHelper.Clamp(_panOffset.X, -maxPanX, maxPanX);
            }
            else
            {
                _panOffset.X = 0; // arbol mas pequeño que ventana, no hay pan
            }
            if (treeH > treeRect.Height)
            {
                float maxPanY = (treeH - treeRect.Height) / 2f;
                _panOffset.Y = MathHelper.Clamp(_panOffset.Y, -maxPanY, maxPanY);
            }
            else
            {
                _panOffset.Y = 0;
            }
        }

        private PoESkillNode? FindHoveredNode(Rectangle treeRect)
        {
            Vector2 mouse = new(Main.mouseX, Main.mouseY);
            PoESkillNode? closest = null;
            float closestDist = float.MaxValue;
            foreach (var node in _allNodes)
            {
                Vector2 pos = NodeToScreen(node, treeRect);
                float dist = Vector2.Distance(pos, mouse);
                float radius = GetNodeRadius(node) + 4f;
                if (dist < radius && dist < closestDist) { closestDist = dist; closest = node; }
            }
            return closest;
        }

        private float GetNodeRadius(PoESkillNode node)
        {
            return node.Type switch
            {
                NodeType.Small => 9f,        // más grande para mejor visibilidad
                NodeType.Notable => 14f,     // mediano
                NodeType.Keystone => 18f,    // grande
                NodeType.Ascendancy => 16f,  // mediano-grande
                _ => 9f,
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

        private void DrawNode(SpriteBatch sb, Vector2 pos, PoESkillNode node, bool allocated, bool canAlloc, bool hovered)
        {
            float radius = GetNodeRadius(node);

            Color baseColor = node.Type switch
            {
                NodeType.Small => new Color(100, 90, 130),
                NodeType.Notable => new Color(70, 130, 200),
                NodeType.Keystone => new Color(245, 196, 81),
                NodeType.Ascendancy => new Color(150, 60, 230),
                _ => new Color(100, 90, 130),
            };

            if (!allocated && !canAlloc) baseColor *= 0.25f;

            // Glow para keystones
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

            // Relleno circular
            DrawCircle(sb, pos, radius, allocated ? new Color(255, 225, 140) : (canAlloc ? baseColor : baseColor * 0.1f));

            // Borde
            Color border = allocated ? new Color(255, 240, 190) : hovered ? Color.White : new Color(baseColor.R + 40, baseColor.G + 40, baseColor.B + 40);
            DrawCircleOutline(sb, pos, radius, border);

            // Punto interior para small allocated
            if (node.Type == NodeType.Small && allocated)
                DrawCircle(sb, pos, radius * 0.4f, new Color(255, 245, 200));

            // Label solo para notable/keystone/ascendancy y solo si hover/allocated
            if ((node.Type == NodeType.Notable || node.Type == NodeType.Keystone || node.Type == NodeType.Ascendancy) && (hovered || allocated))
            {
                string shortName = node.Name.Length > 14 ? node.Name.Substring(0, 12) + "…" : node.Name;
                var textSize = FontAssets.MouseText.Value.MeasureString(shortName);
                int labelW = (int)textSize.X + 8;
                int labelH = 16;
                int lx = (int)(pos.X - labelW / 2f);
                int ly = (int)(pos.Y - radius - 18);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(lx, ly, labelW, labelH), new Color(10, 8, 20, 220));
                Color labelBorder = allocated ? new Color(245, 196, 81, 150) : new Color(179, 136, 255, 150);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(lx, ly, labelW, 1), labelBorder);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(lx, ly + labelH - 1, labelW, 1), labelBorder);
                Utils.DrawBorderString(sb, shortName, new Vector2(pos.X, ly + 4),
                    allocated ? new Color(255, 240, 190) : new Color(220, 200, 240), 0.6f, 0.5f, 0f);
            }
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

        private void DrawTooltip(SpriteBatch sb, ShardPlayer sp)
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
            int tw = (int)maxW + 20;
            int th = lines.Length * 16 + 16;

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
                Utils.DrawBorderString(sb, lines[i], new Vector2(tx + 10, ty + 8 + i * 16), col, 0.8f);
            }
        }

        private void DrawStars(SpriteBatch sb, Rectangle rect)
        {
            for (int i = 0; i < 40; i++)
            {
                int seed = i * 73856093;
                float bx = (seed % 1000) / 1000f * rect.Width;
                float by = ((seed * 19349663) % 1000) / 1000f * rect.Height;
                float sx = rect.X + bx;
                float sy = rect.Y + by;
                float twinkle = 0.5f + 0.5f * (float)Math.Sin(_time * 2 + i * 0.5f);
                int alpha = (int)(100 * twinkle);
                if (alpha < 0) alpha = 0; if (alpha > 255) alpha = 255;
                int sz = 1 + (seed % 2);
                Color c = i % 3 == 0 ? new Color(245, 196, 81, alpha) : i % 3 == 1 ? new Color(179, 136, 255, alpha) : new Color(200, 220, 255, alpha);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)sx, (int)sy, sz, sz), c);
            }
        }

        private void DrawX(SpriteBatch sb, Rectangle rect, Color color, float thickness)
        {
            int pad = 8;
            DrawLine(sb, new Vector2(rect.X + pad, rect.Y + pad), new Vector2(rect.Right - pad, rect.Bottom - pad), color, thickness);
            DrawLine(sb, new Vector2(rect.Right - pad, rect.Y + pad), new Vector2(rect.X + pad, rect.Bottom - pad), color, thickness);
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
}
