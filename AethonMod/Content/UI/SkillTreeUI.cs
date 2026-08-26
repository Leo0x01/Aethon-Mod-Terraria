using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// Arbol de habilidades a PANTALLA COMPLETA con estetica cosmica.
    /// Se dibuja en PostDrawInterface (garantizado que se llama cada frame).
    ///
    /// Estetica (basada en la pagina web del mod):
    /// - Fondo: espacio profundo (#0d0a1a) con radiales purpura/dorado
    /// - Estrellas: 120 puntos brillantes en dorado/violeta/blanco
    /// - Nodos: circulos con borde dorado/violeta, glow al asignar
    /// - Conexiones: lineas doradas (activas) / grises (inactivas)
    /// - Layout: radial desde centro, clusters tematicos
    /// </summary>
    public class SkillTreeUIState
    {
        public bool IsVisible;
        public PoESkillTree? CurrentTree;

        private float _zoom = 1f;
        private Vector2 _panOffset = Vector2.Zero;
        private bool _isDragging = false;
        private Vector2 _dragStart;
        private bool _mouseLeftPressed = false;

        private List<PoESkillNode> _allNodes = new();
        private List<(int fromIdx, int toIdx)> _allConnections = new();
        private Dictionary<string, int> _nodeIndex = new();
        private float _minX, _maxX, _minY, _maxY, _rangeX, _rangeY;

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
                    if (_nodeIndex.TryGetValue(connId, out int j))
                        _allConnections.Add((fromIdx, j));
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

        public void Hide() { IsVisible = false; }

        private Vector2 NodeToScreen(PoESkillNode node)
        {
            float cx = Main.screenWidth / 2f;
            float cy = Main.screenHeight / 2f;
            float scale = System.Math.Min(Main.screenWidth / _rangeX, Main.screenHeight / _rangeY) * 0.5f * _zoom;
            return new Vector2(
                (node.X - _minX) * scale - (_rangeX * scale / 2f) + cx + _panOffset.X,
                (node.Y - _minY) * scale - (_rangeY * scale / 2f) + cy + _panOffset.Y);
        }

        private PoESkillNode? FindHoveredNode()
        {
            Vector2 mouse = new(Main.mouseX, Main.mouseY);
            PoESkillNode? closest = null;
            float closestDist = float.MaxValue;
            foreach (var node in _allNodes)
            {
                Vector2 pos = NodeToScreen(node);
                float dist = Vector2.Distance(pos, mouse);
                float radius = node.Radius * 2.2f * _zoom + 8f;
                if (dist < radius && dist < closestDist) { closestDist = dist; closest = node; }
            }
            return closest;
        }

        /// <summary>Dibuja el arbol. Llamado desde PostDrawInterface cada frame.</summary>
        public void Draw()
        {
            if (!IsVisible || CurrentTree == null) return;
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            var sb = Main.spriteBatch;

            // === FONDO COSMICO (estetica de la web) ===
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                new Color(13, 10, 26, 250));

            // Radiales: purpura arriba-izquierda, dorado abajo-derecha
            DrawRadial(sb, 0.2f, 0.1f, 0.5f, new Color(120, 80, 200, 25));
            DrawRadial(sb, 0.9f, 0.8f, 0.45f, new Color(245, 196, 81, 20));
            DrawRadial(sb, 0.5f, 1.0f, 0.4f, new Color(179, 136, 255, 22));

            // Estrellas (dorado, violeta, blanco)
            for (int i = 0; i < 120; i++)
            {
                int seed = i * 73856093;
                float bx = (seed % 1920);
                float by = ((seed * 19349663) % 1080);
                float sx = (bx + _panOffset.X * 0.15f) % Main.screenWidth;
                float sy = (by + _panOffset.Y * 0.15f) % Main.screenHeight;
                if (sx < 0) sx += Main.screenWidth;
                if (sy < 0) sy += Main.screenHeight;
                int bright = 40 + ((seed * 83492791) % 50);
                int sz = 1 + ((seed * 1299721) % 2);
                int colorType = (seed * 3) % 3;
                Color starColor = colorType == 0 ? new Color(bright, bright, bright + 15, bright + 20)
                               : colorType == 1 ? new Color(bright + 20, (int)(bright * 0.8f), (int)(bright * 0.4f), bright + 20)
                               : new Color((int)(bright * 0.5f), (int)(bright * 0.4f), bright + 20, bright + 20);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)sx, (int)sy, sz, sz), starColor);
            }

            // Vignette radial central
            for (int r = 300; r > 0; r -= 20)
            {
                int alpha = (300 - r) / 15;
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(Main.screenWidth / 2 - r, Main.screenHeight / 2 - r, r * 2, r * 2),
                    new Color(20, 12, 40, alpha));
            }

            // === CONEXIONES ===
            foreach (var (fromIdx, toIdx) in _allConnections)
            {
                Vector2 from = NodeToScreen(_allNodes[fromIdx]);
                Vector2 to = NodeToScreen(_allNodes[toIdx]);
                bool bothActive = sp.AllocatedNodes.Contains(_allNodes[fromIdx].Id) && sp.AllocatedNodes.Contains(_allNodes[toIdx].Id);
                Color lc = bothActive ? new Color(245, 196, 81, 220) : new Color(50, 45, 75, 60);
                float th = bothActive ? 3f : 1.5f;
                DrawLine(sb, from, to, lc, th);
            }

            // === NODOS ===
            var hovered = FindHoveredNode();
            foreach (var node in _allNodes)
            {
                Vector2 pos = NodeToScreen(node);
                bool allocated = sp.AllocatedNodes.Contains(node.Id);
                bool canAlloc = CanAllocate(node, sp);
                bool isHover = hovered != null && hovered.Id == node.Id;
                DrawNode(sb, pos, node, allocated, canAlloc, isHover);
            }

            // === INPUT ===
            // Pan con click derecho
            if (Main.mouseRight)
            {
                if (!_isDragging) { _isDragging = true; _dragStart = new(Main.mouseX, Main.mouseY); }
                else { _panOffset += new Vector2(Main.mouseX, Main.mouseY) - _dragStart; _dragStart = new(Main.mouseX, Main.mouseY); }
            }
            else _isDragging = false;

            // Click izquierdo para asignar
            if (Main.mouseLeft && !_mouseLeftPressed && hovered != null && !_isDragging)
            {
                _mouseLeftPressed = true;
                if (sp.AllocatedNodes.Contains(hovered.Id))
                {
                    sp.AllocatedNodes.Remove(hovered.Id);
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
                }
                else if (CanAllocate(hovered, sp))
                {
                    sp.AllocatedNodes.Add(hovered.Id);
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
                }
            }
            if (!Main.mouseLeft) _mouseLeftPressed = false;

            // === TOOLTIP ===
            if (hovered != null)
            {
                string ts = hovered.Type switch
                {
                    NodeType.Small => "[Small]", NodeType.Notable => "[Notable]",
                    NodeType.Keystone => "[KEYSTONE]", NodeType.Ascendancy => "[Ascendancy]", _ => "",
                };
                bool alloc = sp.AllocatedNodes.Contains(hovered.Id);
                Main.instance.MouseText($"{ts} {hovered.Name}\n{hovered.Effect}\nCoste: {hovered.Cost} pts {(alloc ? "(Asignado)" : "(Disponible)")}");
            }

            // === UI OVERLAY ===
            int total = sp.CumulativeSkillPoints();
            int spent = sp.AllocatedNodes.Count;
            int avail = total - spent;

            // Barra superior
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, 55),
                new Color(10, 8, 20, 235));
            Utils.DrawBorderString(sb, "ARBOL DE HABILIDADES",
                new(Main.screenWidth / 2f, 15), new Color(245, 196, 81), 1.3f, 0.5f, 0.5f);
            Utils.DrawBorderString(sb, $"Puntos: {avail}  |  Asignados: {spent}/{total}",
                new(Main.screenWidth / 2f, 38), new Color(179, 136, 255), 0.9f, 0.5f, 0.5f);

            // Ayuda abajo
            Utils.DrawBorderString(sb,
                "Click izq: asignar  |  Click der: mover  |  K/Esc: cerrar",
                new(Main.screenWidth / 2f, Main.screenHeight - 15),
                new Color(100, 95, 120), 0.8f, 0.5f, 0.5f);

            // Cerrar con Escape
            if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape)) Hide();
        }

        private bool CanAllocate(PoESkillNode node, ShardPlayer sp)
        {
            if (sp.AllocatedNodes.Contains(node.Id)) return false;
            if (node.Id == "start" || node.Cost == 0) return true;
            foreach (var aid in sp.AllocatedNodes)
                if (node.Connections.Contains(aid)) return true;
            return false;
        }

        private void DrawRadial(SpriteBatch sb, float xPct, float yPct, float radiusPct, Color color)
        {
            float cx = Main.screenWidth * xPct;
            float cy = Main.screenHeight * yPct;
            float r = Main.screenWidth * radiusPct;
            for (int i = (int)r; i > 0; i -= 15)
            {
                int alpha = (int)(color.A * (1f - (float)i / r) * 0.3f);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)(cx - i), (int)(cy - i), i * 2, i * 2),
                    new Color(color.R, color.G, color.B, alpha));
            }
        }

        private void DrawNode(SpriteBatch sb, Vector2 pos, PoESkillNode node, bool allocated, bool canAlloc, bool hovered)
        {
            int radius = (int)(node.Radius * 2.0f * _zoom);
            radius = System.Math.Max(6, radius);

            // Color base por tipo (estetica web: dorado/violeta/teal)
            Color baseColor = node.Type switch
            {
                NodeType.Small => new Color(90, 90, 120),
                NodeType.Notable => new Color(60, 120, 210),
                NodeType.Keystone => new Color(245, 196, 81),
                NodeType.Ascendancy => new Color(150, 60, 230),
                _ => Color.White,
            };

            if (!allocated && !canAlloc) baseColor *= 0.12f;

            // Glow dorado si asignado (estetica web: text-glow-gold)
            if (allocated)
                for (int i = 3; i > 0; i--)
                {
                    int gr = radius + i * 5;
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(245, 196, 81, 8 - i * 2));
                }

            // Glow blanco al hover
            if (hovered && (canAlloc || allocated))
                for (int i = 2; i > 0; i--)
                {
                    int gr = radius + i * 4;
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X - gr, (int)pos.Y - gr, gr * 2, gr * 2),
                        new Color(255, 255, 255, 6 - i * 2));
                }

            // Relleno
            Color fill = allocated ? new Color(255, 225, 140) : (canAlloc ? baseColor : baseColor * 0.08f);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)pos.X - radius, (int)pos.Y - radius, radius * 2, radius * 2), fill);

            // Borde
            Color border = allocated ? new Color(255, 240, 190) : hovered ? Color.White : new(baseColor.R + 30, baseColor.G + 30, baseColor.B + 30);
            int b = 2;
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)pos.X - radius, (int)pos.Y - radius, radius * 2, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)pos.X - radius, (int)pos.Y + radius - b, radius * 2, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)pos.X - radius, (int)pos.Y - radius, b, radius * 2), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)pos.X + radius - b, (int)pos.Y - radius, b, radius * 2), border);

            // Punto interior small nodes
            if (node.Type == NodeType.Small)
            {
                int dr = radius / 3;
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)pos.X - dr, (int)pos.Y - dr, dr * 2, dr * 2),
                    allocated ? new Color(255, 245, 200) : new(baseColor.R + 50, baseColor.G + 50, baseColor.B + 50));
            }

            // Costo debajo
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
