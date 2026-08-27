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
    /// Arbol de habilidades a PANTALLA COMPLETA con estetica cosmica.
    /// Implementado como UIState real (patron del Bestiario de Terraria):
    /// - Ciclo de vida adecuado (OnActivate/OnDeactivate)
    /// - Manejo de input via UIElement
    /// - Estrellas animadas como fondo (parallax con pan)
    /// - Nodos clicables con tooltips
    /// - Pan/zoom con click derecho y rueda
    ///
    /// Estetica (basada en la pagina web del mod):
    /// - Fondo: espacio profundo (#0d0a1a) con radiales purpura/dorado
    /// - Estrellas: capas con parallax, brillo pulsante
    /// - Nodos: circulos con borde dorado/violeta, glow al asignar
    /// - Conexiones: lineas doradas (activas) / grises (inactivas)
    /// </summary>
    public class SkillTreeUIState : UIState
    {
        public bool IsVisible;
        public PoESkillTree? CurrentTree;

        private float _zoom = 1f;
        private Vector2 _panOffset = Vector2.Zero;
        private bool _isDragging = false;
        private Vector2 _dragStart = Vector2.Zero;
        private Vector2 _lastMouse = Vector2.Zero;
        private bool _mouseLeftPressed = false;
        private int _lastScrollValue = 0;

        private List<PoESkillNode> _allNodes = new();
        private List<(int fromIdx, int toIdx)> _allConnections = new();
        private Dictionary<string, int> _nodeIndex = new();
        private float _minX, _maxX, _minY, _maxY, _rangeX, _rangeY;

        // Estrellas de fondo (pre-generadas, con parallax)
        private struct Star
        {
            public Vector2 Pos;     // posicion base en pantalla (0..1)
            public float Size;      // tamano en px
            public float Twinkle;   // velocidad de parpadeo
            public float Phase;     // fase inicial
            public int Layer;        // capa de parallax (0=lejos, 2=cerca)
            public Color Color;     // tinte
        }
        private Star[]? _stars;
        private float _time = 0f;

        // Meteoros (efecto cosmico de fondo)
        private struct Meteor
        {
            public Vector2 Pos;
            public Vector2 Vel;
            public float Life;
            public float MaxLife;
            public Color Color;
        }
        private List<Meteor> _meteors = new();
        private int _meteorTimer = 0;

        // Nodo hovered (para tooltip)
        private PoESkillNode? _hoveredNode;
        private float _hoverAlpha = 0f;

        public override void OnActivate()
        {
            // Llamado cuando el UIState se añade a la lista de interfaces.
            // Inicializar estrellas de fondo.
            InitStars();
            _meteors.Clear();
            _meteorTimer = 0;
        }

        public override void OnDeactivate()
        {
            _isDragging = false;
            _mouseLeftPressed = false;
        }

        private void InitStars()
        {
            if (_stars != null) return;
            var rand = new Random(42); // seed fijo para consistencia
            _stars = new Star[200];
            for (int i = 0; i < _stars.Length; i++)
            {
                int layer = rand.Next(3); // 0, 1, 2
                _stars[i] = new Star
                {
                    Pos = new Vector2((float)rand.NextDouble(), (float)rand.NextDouble()),
                    Size = 1f + (float)rand.NextDouble() * (layer == 2 ? 2.5f : layer == 1 ? 1.8f : 1.2f),
                    Twinkle = 0.5f + (float)rand.NextDouble() * 2f,
                    Phase = (float)rand.NextDouble() * MathF.PI * 2f,
                    Layer = layer,
                    Color = layer == 2
                        ? new Color(245, 196, 81)   // dorado (cerca)
                        : layer == 1
                            ? new Color(179, 136, 255) // violeta (medio)
                            : new Color(200, 220, 255), // blanco-azul (lejos)
                };
            }
        }

        public void BuildPoETree(PoESkillTree tree)
        {
            CurrentTree = tree;
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
        }

        public void Show()
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted)
            {
                Main.NewText("El Fragmento Genesis aun no tiene una rama. Derrota enemigos para despertarlo.", new Color(180, 160, 220));
                return;
            }
            try
            {
                var tree = PoETreeCatalog.GetTree(sp.ActiveBranch);
                BuildPoETree(tree);
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<AethonMod>()?.Logger?.Error("SkillTreeUI.Show: error al construir arbol", ex);
                Main.NewText("Error al cargar el arbol de habilidades. Revisa el registro del mod.", new Color(255, 120, 120));
                return;
            }
            _zoom = 1f;
            _panOffset = Vector2.Zero;
            IsVisible = true;
            // Inicializar estrellas si no estaban
            InitStars();
            OnActivate();
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuOpen);
        }

        public void Hide()
        {
            IsVisible = false;
            _isDragging = false;
            _mouseLeftPressed = false;
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
        }

        private Vector2 NodeToScreen(PoESkillNode node)
        {
            if (_rangeX <= 0 || _rangeY <= 0) return new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
            float cx = Main.screenWidth / 2f;
            float cy = Main.screenHeight / 2f;
            float scale = Math.Min(Main.screenWidth / _rangeX, Main.screenHeight / _rangeY) * 0.45f * _zoom;
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

        public void Draw()
        {
            if (!IsVisible) return;
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            _time += 0.016f; // ~60fps
            var sb = Main.spriteBatch;

            try
            {
                DrawBackground(sb);
                DrawStars(sb);
                UpdateMeteors();
                DrawMeteors(sb);

                if (CurrentTree == null || _allNodes.Count == 0)
                {
                    Utils.DrawBorderString(sb, "No hay arbol disponible para esta rama.",
                        new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f),
                        new Color(255, 200, 100), 1.0f, 0.5f, 0.5f);
                    DrawTopBar(sb, sp, 0, 0);
                    DrawBottomHelp(sb);
                    HandleInput(sp);
                    return;
                }

                DrawConnections(sb, sp);
                _hoveredNode = FindHoveredNode();
                _hoverAlpha = MathHelper.Lerp(_hoverAlpha, _hoveredNode != null ? 1f : 0f, 0.2f);
                DrawNodes(sb, sp);
                DrawTooltip(sb, sp);
                DrawTopBar(sb, sp, sp.CumulativeSkillPoints(), sp.AllocatedNodes.Count);
                DrawBottomHelp(sb);
                HandleInput(sp);
            }
            catch (Exception ex)
            {
                // Nunca propagar excepciones desde Draw — podria romper el render loop.
                ModContent.GetInstance<AethonMod>()?.Logger?.Error("SkillTreeUI.Draw error", ex);
                IsVisible = false;
            }
        }

        // ====================================================================
        // FONDO Y EFECTOS COSMICOS
        // ====================================================================
        private void DrawBackground(SpriteBatch sb)
        {
            // Fondo base: espacio profundo (#0d0a1a)
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                new Color(13, 10, 26, 250));

            // Radiales cosmicos (estetica de la web)
            DrawRadial(sb, 0.2f, 0.1f, 0.5f, new Color(120, 80, 200, 25));
            DrawRadial(sb, 0.9f, 0.8f, 0.45f, new Color(245, 196, 81, 20));
            DrawRadial(sb, 0.5f, 1.0f, 0.4f, new Color(179, 136, 255, 22));

            // Nebulosa central pulsante
            float pulse = 0.85f + (float)Math.Sin(_time * 0.5) * 0.15f;
            DrawRadial(sb, 0.5f, 0.5f, 0.3f * pulse, new Color(80, 40, 160, 18));

            // Vignette radial central (oscurece los bordes)
            for (int r = 320; r > 0; r -= 20)
            {
                int alpha = (320 - r) / 15;
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(Main.screenWidth / 2 - r, Main.screenHeight / 2 - r, r * 2, r * 2),
                    new Color(20, 12, 40, alpha));
            }
        }

        private void DrawStars(SpriteBatch sb)
        {
            if (_stars == null) return;
            for (int i = 0; i < _stars.Length; i++)
            {
                var s = _stars[i];
                // Parallax: las estrellas cercanas (layer 2) se mueven mas con el pan
                float parallax = s.Layer == 2 ? 0.4f : s.Layer == 1 ? 0.2f : 0.08f;
                float sx = (s.Pos.X * Main.screenWidth + _panOffset.X * parallax) % Main.screenWidth;
                float sy = (s.Pos.Y * Main.screenHeight + _panOffset.Y * parallax) % Main.screenHeight;
                if (sx < 0) sx += Main.screenWidth;
                if (sy < 0) sy += Main.screenHeight;

                // Twinkle (parpadeo)
                float twinkle = 0.6f + 0.4f * (float)Math.Sin(_time * s.Twinkle + s.Phase);
                int alpha = (int)(255f * twinkle * (s.Layer == 2 ? 0.95f : s.Layer == 1 ? 0.7f : 0.45f));
                if (alpha < 0) alpha = 0; if (alpha > 255) alpha = 255;
                Color c = new Color(s.Color.R, s.Color.G, s.Color.B, alpha);

                // Estrella con glow para las cercanas
                if (s.Layer == 2 && s.Size > 2f)
                {
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)sx - 2, (int)sy - 2, 5, 5),
                        new Color(c.R, c.G, c.B, alpha / 4));
                }
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)sx, (int)sy, (int)s.Size, (int)s.Size), c);
            }
        }

        private void UpdateMeteors()
        {
            _meteorTimer++;
            if (_meteorTimer > 180 + Main.rand.Next(120) && _meteors.Count < 3)
            {
                _meteorTimer = 0;
                var meteor = new Meteor
                {
                    Pos = new Vector2(Main.rand.NextFloat(Main.screenWidth), -20),
                    Vel = new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(4, 8)),
                    Life = 0,
                    MaxLife = 60 + Main.rand.Next(40),
                    Color = Main.rand.NextBool() ? new Color(245, 196, 81) : new Color(179, 136, 255),
                };
                _meteors.Add(meteor);
            }

            for (int i = _meteors.Count - 1; i >= 0; i--)
            {
                var m = _meteors[i];
                m.Pos += m.Vel;
                m.Life++;
                _meteors[i] = m;
                if (m.Life > m.MaxLife || m.Pos.Y > Main.screenHeight + 50)
                    _meteors.RemoveAt(i);
            }
        }

        private void DrawMeteors(SpriteBatch sb)
        {
            foreach (var m in _meteors)
            {
                float fade = 1f;
                if (m.Life < 5) fade = m.Life / 5f;
                else if (m.Life > m.MaxLife - 10) fade = (m.MaxLife - m.Life) / 10f;
                fade = MathHelper.Clamp(fade, 0, 1);
                // Cola del meteoro
                for (int i = 0; i < 8; i++)
                {
                    var trailPos = m.Pos - m.Vel * i * 1.5f;
                    int a = (int)(fade * (255 - i * 28));
                    if (a < 0) a = 0;
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)trailPos.X - 1, (int)trailPos.Y - 1, 3, 3),
                        new Color(m.Color.R, m.Color.G, m.Color.B, a));
                }
                // Cabeza
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)m.Pos.X - 2, (int)m.Pos.Y - 2, 4, 4),
                    new Color(m.Color.R, m.Color.G, m.Color.B, (int)(fade * 255)));
            }
        }

        // ====================================================================
        // CONEXIONES Y NODOS
        // ====================================================================
        private void DrawConnections(SpriteBatch sb, ShardPlayer sp)
        {
            foreach (var (fromIdx, toIdx) in _allConnections)
            {
                if (fromIdx < 0 || fromIdx >= _allNodes.Count || toIdx < 0 || toIdx >= _allNodes.Count) continue;
                Vector2 from = NodeToScreen(_allNodes[fromIdx]);
                Vector2 to = NodeToScreen(_allNodes[toIdx]);
                bool bothActive = sp.AllocatedNodes.Contains(_allNodes[fromIdx].Id) && sp.AllocatedNodes.Contains(_allNodes[toIdx].Id);
                bool oneActive = sp.AllocatedNodes.Contains(_allNodes[fromIdx].Id) || sp.AllocatedNodes.Contains(_allNodes[toIdx].Id);

                Color lc;
                float th;
                if (bothActive) { lc = new Color(245, 196, 81, 230); th = 3.5f; }
                else if (oneActive) { lc = new Color(180, 140, 80, 160); th = 2.2f; }
                else { lc = new Color(60, 50, 90, 90); th = 1.5f; }

                DrawLine(sb, from, to, lc, th);
            }
        }

        private void DrawNodes(SpriteBatch sb, ShardPlayer sp)
        {
            foreach (var node in _allNodes)
            {
                Vector2 pos = NodeToScreen(node);
                bool allocated = sp.AllocatedNodes.Contains(node.Id);
                bool canAlloc = CanAllocate(node, sp);
                bool isHover = _hoveredNode != null && _hoveredNode.Id == node.Id;
                DrawNode(sb, pos, node, allocated, canAlloc, isHover);
            }
        }

        private void DrawTooltip(SpriteBatch sb, ShardPlayer sp)
        {
            if (_hoveredNode == null) return;
            var node = _hoveredNode;
            if (node == null) return;
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

            // Tooltip con fondo
            string[] lines = {
                $"{typeStr} {node.Name}",
                node.Effect ?? "",
                $"Rama: {node.Branch}",
                $"Coste: {node.Cost} pts",
                alloc ? "(Asignado — click para quitar)" : (CanAllocate(node, sp) ? "(Click para asignar)" : "(Requiere nodo adyacente)"),
            };

            // Medir el tooltip
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

            // Posicionar tooltip cerca del cursor sin salirse de pantalla
            int tx = Main.mouseX + 16;
            int ty = Main.mouseY + 16;
            if (tx + tw > Main.screenWidth) tx = Main.mouseX - tw - 8;
            if (ty + th > Main.screenHeight) ty = Main.screenHeight - th - 4;
            if (ty < 0) ty = 4;

            // Fondo
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(tx, ty, tw, th),
                new Color(15, 10, 30, 245));
            // Borde dorado/violeta segun tipo
            Color border = node.Type == NodeType.Keystone ? new Color(245, 196, 81)
                          : node.Type == NodeType.Ascendancy ? new Color(179, 136, 255)
                          : new Color(120, 100, 160);
            int b = 2;
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(tx, ty, tw, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(tx, ty + th - b, tw, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(tx, ty, b, th), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(tx + tw - b, ty, b, th), border);

            // Texto
            for (int i = 0; i < lines.Length; i++)
            {
                Color col = i == 0 ? new Color(245, 196, 81)
                          : i == lines.Length - 1 ? (alloc ? new Color(100, 220, 100) : new Color(179, 136, 255))
                          : new Color(220, 220, 230);
                Utils.DrawBorderString(sb, lines[i], new Vector2(tx + padX, ty + padY + i * lineH), col, 0.85f);
            }
        }

        // ====================================================================
        // BARRAS SUPERIOR E INFERIOR
        // ====================================================================
        private void DrawTopBar(SpriteBatch sb, ShardPlayer sp, int total, int spent)
        {
            int avail = total - spent;
            // Barra superior con gradiente
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, 60),
                new Color(8, 6, 16, 240));
            // Linea dorada inferior
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 60, Main.screenWidth, 2),
                new Color(245, 196, 81, 200));

            string branchName = sp.ActiveBranch switch
            {
                BranchType.Distance => "DISTANCIA — Lumina, la Arcoestelar",
                BranchType.Melee => "CUERPO A CUERPO — Solbrand, Filo del Alba",
                BranchType.Magic => "ARTES MAGICAS — Grimorio del Eterno",
                _ => "FRAGMENTO GENESIS",
            };

            Utils.DrawBorderString(sb, "★ ARBOL DE HABILIDADES ★",
                new Vector2(Main.screenWidth / 2f, 16), new Color(245, 196, 81), 1.3f, 0.5f, 0.5f);
            Utils.DrawBorderString(sb, branchName,
                new Vector2(Main.screenWidth / 2f, 38), new Color(179, 136, 255), 0.85f, 0.5f, 0.5f);

            // Puntos de habilidad (lado izquierdo)
            string ptsText = $"Puntos: {avail}";
            Utils.DrawBorderString(sb, ptsText, new Vector2(20, 16),
                avail > 0 ? new Color(120, 255, 150) : new Color(180, 180, 200), 1.0f, 0f, 0f);
            Utils.DrawBorderString(sb, $"Asignados: {spent}/{total}", new Vector2(20, 38),
                new Color(179, 136, 255), 0.85f, 0f, 0f);

            // Nivel del fragmento (lado derecho)
            Utils.DrawBorderString(sb, $"Nivel {sp.ShardLevel}", new Vector2(Main.screenWidth - 20, 16),
                new Color(245, 196, 81), 1.0f, 1f, 0f);
            Utils.DrawBorderString(sb, $"XP: {sp.ShardXP}/{sp.XPForNextLevel()}", new Vector2(Main.screenWidth - 20, 38),
                new Color(179, 136, 255), 0.8f, 1f, 0f);

            // Boton cerrar (X) en la esquina superior derecha
            int closeX = Main.screenWidth - 50;
            int closeY = 70;
            int closeSize = 28;
            bool hoverClose = Main.mouseX >= closeX && Main.mouseX <= closeX + closeSize &&
                              Main.mouseY >= closeY && Main.mouseY <= closeY + closeSize;
            Color closeBg = hoverClose ? new Color(220, 80, 80, 230) : new Color(40, 20, 30, 180);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(closeX, closeY, closeSize, closeSize), closeBg);
            Utils.DrawBorderString(sb, "X", new Vector2(closeX + closeSize / 2f, closeY + closeSize / 2f - 2),
                hoverClose ? Color.White : new Color(220, 180, 180), 1.1f, 0.5f, 0.5f);
            if (hoverClose && Main.mouseLeft && !_mouseLeftPressed)
            {
                _mouseLeftPressed = true;
                Hide();
                return;
            }

            // Boton Reset (centrar vista)
            int resetX = Main.screenWidth - 90;
            int resetY = 70;
            int resetW = 32, resetH = 28;
            bool hoverReset = Main.mouseX >= resetX && Main.mouseX <= resetX + resetW &&
                              Main.mouseY >= resetY && Main.mouseY <= resetY + resetH;
            Color resetBg = hoverReset ? new Color(120, 100, 200, 230) : new Color(30, 20, 50, 180);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(resetX, resetY, resetW, resetH), resetBg);
            Utils.DrawBorderString(sb, "⟲", new Vector2(resetX + resetW / 2f, resetY + resetH / 2f - 2),
                hoverReset ? Color.White : new Color(200, 180, 240), 1.0f, 0.5f, 0.5f);
            if (hoverReset && Main.mouseLeft && !_mouseLeftPressed)
            {
                _mouseLeftPressed = true;
                _zoom = 1f;
                _panOffset = Vector2.Zero;
            }
        }

        private void DrawBottomHelp(SpriteBatch sb)
        {
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, Main.screenHeight - 30, Main.screenWidth, 30),
                new Color(8, 6, 16, 220));
            Utils.DrawBorderString(sb,
                "Click izq: asignar/quitar  |  Click der: mover  |  Rueda: zoom  |  K / Esc: cerrar",
                new Vector2(Main.screenWidth / 2f, Main.screenHeight - 15),
                new Color(140, 130, 170), 0.8f, 0.5f, 0.5f);
        }

        // ====================================================================
        // INPUT
        // ====================================================================
        private void HandleInput(ShardPlayer sp)
        {
            // Pan con click derecho
            if (Main.mouseRight)
            {
                Vector2 curMouse = new(Main.mouseX, Main.mouseY);
                if (!_isDragging)
                {
                    _isDragging = true;
                    _dragStart = curMouse;
                    _lastMouse = curMouse;
                }
                else
                {
                    _panOffset += curMouse - _lastMouse;
                    _lastMouse = curMouse;
                }
            }
            else _isDragging = false;

            // Zoom con rueda (usar PlayerInput.ScrollWheelValue para detectar el delta)
            int curScroll = Terraria.GameInput.PlayerInput.ScrollWheelValue;
            int scrollDelta = curScroll - _lastScrollValue;
            _lastScrollValue = curScroll;
            if (scrollDelta != 0)
            {
                float zoomDelta = scrollDelta > 0 ? 0.1f : -0.1f;
                _zoom = MathHelper.Clamp(_zoom + zoomDelta, 0.4f, 2.5f);
            }

            // Click izquierdo para asignar/quitar
            if (Main.mouseLeft && !_mouseLeftPressed && _hoveredNode != null && !_isDragging)
            {
                _mouseLeftPressed = true;
                var node = _hoveredNode;
                if (node == null) return;
                if (sp.AllocatedNodes.Contains(node.Id))
                {
                    sp.AllocatedNodes.Remove(node.Id);
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
                    // Particulas al quitar (en la posicion del nodo)
                    Vector2 nodePos = NodeToScreen(node);
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
                        Main.NewText($"No tienes suficientes puntos. Necesitas {node.Cost}, tienes {avail}.",
                            new Color(255, 120, 120));
                        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
                    }
                    else
                    {
                        sp.AllocatedNodes.Add(node.Id);
                        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
                        // Particulas de asignacion
                        Vector2 nodePos = NodeToScreen(node);
                        for (int i = 0; i < 16; i++)
                            Dust.NewDustPerfect(nodePos, Terraria.ID.DustID.GoldFlame,
                                new Vector2(Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4)),
                                100, new Color(245, 196, 81), 1.2f);
                    }
                }
            }
            if (!Main.mouseLeft) _mouseLeftPressed = false;

            // Cerrar con Escape o K
            var kb = Main.keyState;
            var oldKb = Main.oldKeyState;
            if (kb.IsKeyDown(Keys.Escape) && !oldKb.IsKeyDown(Keys.Escape)) Hide();

            var config = ModContent.GetInstance<Content.AethonConfig>();
            if (config != null && kb.IsKeyDown(config.SkillTreeKey) && !oldKb.IsKeyDown(config.SkillTreeKey))
                Hide();

            // Bloquear el input del juego del mouse y teclas mientras la UI este abierta
            // (evita que el jugador ataque/mueva mientras asigna habilidades)
            Main.mouseLeft = false;
            Main.mouseRight = false;
        }

        private bool CanAllocate(PoESkillNode node, ShardPlayer sp)
        {
            if (sp.AllocatedNodes.Contains(node.Id)) return false;
            if (node.Id == "start" || node.Cost == 0) return true;
            foreach (var aid in sp.AllocatedNodes)
                if (node.Connections.Contains(aid)) return true;
            return false;
        }

        // ====================================================================
        // HELPERS DE DIBUJO
        // ====================================================================
        private void DrawRadial(SpriteBatch sb, float xPct, float yPct, float radiusPct, Color color)
        {
            float cx = Main.screenWidth * xPct;
            float cy = Main.screenHeight * yPct;
            float r = Main.screenWidth * radiusPct;
            if (r <= 0) return;
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
            radius = Math.Max(6, radius);

            // Color base por tipo (estetica web: dorado/violeta/teal)
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

            // Glow pulsante para keystones y ascendancy
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
            Color border = allocated ? new Color(255, 240, 190)
                          : hovered ? Color.White
                          : new Color(baseColor.R + 30, baseColor.G + 30, baseColor.B + 30);
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
                string shortName = node.Name.Length > 18 ? node.Name.Substring(0, 16) + "…" : node.Name;
                Utils.DrawBorderString(sb, shortName, new Vector2(pos.X, pos.Y - radius - 10),
                    allocated ? new Color(255, 240, 190) : new Color(220, 200, 240), 0.7f * _zoom, 0.5f, 1f);
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
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            float length = edge.Length();
            if (length < 1f) return;
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)start.X, (int)start.Y, (int)length, (int)thickness),
                null, color, angle, new Vector2(0, thickness / 2f), SpriteEffects.None, 0);
        }
    }
}
