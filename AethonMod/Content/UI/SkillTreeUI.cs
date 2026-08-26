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
    /// <summary>
    /// UI del arbol de habilidades estilo Path of Exile.
    /// - Fondo estrellado oscuro
    /// - Nodos circulares con glow
    /// - Lineas de conexion doradas/oscuras
    /// - Zoom con rueda del raton
    /// - Pan arrastrando
    /// - Click para asignar nodos
    /// </summary>
    public class SkillTreeUIState : UIState
    {
        public bool IsVisible;
        public PoESkillTree? CurrentTree;

        private float _zoom = 1f;
        private Vector2 _panOffset = Vector2.Zero;
        private bool _isDragging = false;
        private Vector2 _dragStart = Vector2.Zero;

        private UIText _titleText = null!;
        private UIText _pointsText = null!;
        private List<PoESkillNodeButton> _nodeButtons = new();
        private List<(Vector2 from, Vector2 to, bool active)> _connections = new();

        public override void OnInitialize()
        {
            _titleText = new UIText("Arbol de Habilidades", 1.3f);
            _titleText.HAlign = 0.5f;
            _titleText.Top.Set(10, 0f);
            _titleText.TextColor = new Color(245, 196, 81);
            Append(_titleText);

            _pointsText = new UIText("Puntos: 0", 0.9f);
            _pointsText.HAlign = 0.5f;
            _pointsText.Top.Set(38, 0f);
            _pointsText.TextColor = new Color(179, 136, 255);
            Append(_pointsText);

            var closeButton = new UITextPanel<string>("Cerrar (K)");
            closeButton.Width.Set(120, 0f);
            closeButton.Height.Set(28, 0f);
            closeButton.HAlign = 1f;
            closeButton.Top.Set(10, 0f);
            closeButton.Left.Set(-10, 0f);
            closeButton.BackgroundColor = new Color(40, 20, 30, 220);
            closeButton.BorderColor = new Color(180, 80, 80, 150);
            closeButton.OnLeftClick += (evt, el) => Hide();
            Append(closeButton);
        }

        public void BuildPoETree(PoESkillTree tree)
        {
            CurrentTree = tree;
            foreach (var btn in _nodeButtons)
                RemoveChild(btn);
            _nodeButtons.Clear();
            _connections.Clear();

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var node in tree.Nodes)
            {
                minX = System.Math.Min(minX, node.X);
                maxX = System.Math.Max(maxX, node.X);
                minY = System.Math.Min(minY, node.Y);
                maxY = System.Math.Max(maxY, node.Y);
            }
            float rangeX = maxX - minX + 1;
            float rangeY = maxY - minY + 1;

            float cx = Main.screenWidth / 2f;
            float cy = Main.screenHeight / 2f;
            float scale = System.Math.Min(Main.screenWidth / rangeX, Main.screenHeight / rangeY) * 0.7f;

            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();

            // Calcular conexiones
            foreach (var node in tree.Nodes)
            {
                float nx = (node.X - minX) * scale - (rangeX * scale / 2f) + cx;
                float ny = (node.Y - minY) * scale - (rangeY * scale / 2f) + cy;
                foreach (var connId in node.Connections)
                {
                    var target = tree.Nodes.Find(n => n.Id == connId);
                    if (target == null) continue;
                    float tx = (target.X - minX) * scale - (rangeX * scale / 2f) + cx;
                    float ty = (target.Y - minY) * scale - (rangeY * scale / 2f) + cy;
                    bool active = sp != null && sp.AllocatedNodes.Contains(node.Id) && sp.AllocatedNodes.Contains(target.Id);
                    _connections.Add((new Vector2(nx, ny), new Vector2(tx, ty), active));
                }
            }

            // Crear botones
            foreach (var node in tree.Nodes)
            {
                float x = (node.X - minX) * scale - (rangeX * scale / 2f) + cx;
                float y = (node.Y - minY) * scale - (rangeY * scale / 2f) + cy;
                float size = node.Radius * 2.5f;

                var button = new PoESkillNodeButton(node);
                button.Left.Set(x - size / 2f, 0f);
                button.Top.Set(y - size / 2f, 0f);
                button.Width.Set(size, 0f);
                button.Height.Set(size, 0f);
                Append(button);
                _nodeButtons.Add(button);
            }
            UpdatePointsPoE();
        }

        public void UpdatePointsPoE()
        {
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null) return;
            int total = sp.CumulativeSkillPoints();
            int spent = sp.AllocatedNodes.Count;
            int available = total - spent;
            _pointsText.SetText($"Puntos disponibles: {available}    Asignados: {spent} / {total}");
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

            // Zoom con rueda del raton (usar input nativo de Terraria)
            if (IsVisible)
            {
                // Pan con click derecho arrastrando
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
                else
                {
                    _isDragging = false;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;

            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            // Fondo oscuro estrellado (estilo PoE)
            DrawStarfieldBackground(spriteBatch);

            // Dibujar conexiones (lineas entre nodos)
            foreach (var (from, to, active) in _connections)
            {
                Vector2 transformedFrom = TransformPoint(from);
                Vector2 transformedTo = TransformPoint(to);

                Color lineColor = active
                    ? new Color(245, 196, 81, 220)
                    : new Color(60, 50, 90, 100);
                float thickness = active ? 3f : 1.5f;

                DrawLine(spriteBatch, transformedFrom, transformedTo, lineColor, thickness);
            }

            base.Draw(spriteBatch);

            // Dibujar texto informativo abajo
            string helpText = "Click izq: asignar nodo | Click der: mover | Rueda: zoom | K: cerrar";
            Utils.DrawBorderString(spriteBatch, helpText,
                new Vector2(Main.screenWidth / 2f, Main.screenHeight - 30),
                new Color(150, 140, 170), 0.9f, 0.5f, 0.5f);
        }

        private Vector2 TransformPoint(Vector2 original)
        {
            Vector2 center = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
            return (original - center) * _zoom + center + _panOffset;
        }

        private void DrawStarfieldBackground(SpriteBatch spriteBatch)
        {
            // Fondo oscuro estilo PoE (carbón profundo)
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                new Color(12, 10, 18, 245));

            // Estrellas procedurales (simuladas con puntos fijos)
            // Usar posiciones pseudo-aleatorias pero estables.
            for (int i = 0; i < 200; i++)
            {
                int seed = i * 73856093;
                int x = (seed % 1920);
                int y = ((seed * 19349663) % 1080);
                int brightness = ((seed * 83492791) % 60) + 40;
                int size = ((seed * 1299721) % 2) + 1;

                // Mover estrellas con el pan
                float sx = (x + _panOffset.X * 0.3f) % Main.screenWidth;
                float sy = (y + _panOffset.Y * 0.3f) % Main.screenHeight;
                if (sx < 0) sx += Main.screenWidth;
                if (sy < 0) sy += Main.screenHeight;

                Color starColor = new Color(brightness, brightness, brightness + 20, brightness + 30);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)sx, (int)sy, size, size),
                    starColor);
            }

            // Gradiente radial central (mas claro en el centro, como PoE)
            for (int r = 300; r > 0; r -= 20)
            {
                int alpha = (300 - r) / 10;
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(
                        Main.screenWidth / 2 - r + (int)_panOffset.X,
                        Main.screenHeight / 2 - r + (int)_panOffset.Y,
                        r * 2, r * 2),
                    new Color(30, 20, 50, alpha));
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

    /// <summary>
    /// Nodo circular interactivo del arbol PoE.
    /// </summary>
    public class PoESkillNodeButton : UIElement
    {
        private PoESkillNode _node;
        private bool _isAllocated;
        private bool _canAllocate;
        private bool _isHovered;

        public PoESkillNodeButton(PoESkillNode node)
        {
            _node = node;
        }

        public override void OnInitialize()
        {
            OnLeftClick += (evt, el) =>
            {
                var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
                if (sp == null) return;
                if (_isAllocated)
                {
                    sp.AllocatedNodes.Remove(_node.Id);
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
                }
                else if (_canAllocate)
                {
                    sp.AllocatedNodes.Add(_node.Id);
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
                }
                (Parent as SkillTreeUIState)?.UpdatePointsPoE();
            };
            OnMouseOver += (evt, el) => { _isHovered = true; };
            OnMouseOut += (evt, el) => { _isHovered = false; };
        }

        public void UpdateState(ShardPlayer sp)
        {
            _isAllocated = sp.AllocatedNodes.Contains(_node.Id);
            if (_node.Id == "start" || _node.Cost == 0)
            {
                _canAllocate = !_isAllocated;
            }
            else
            {
                _canAllocate = false;
                foreach (var allocatedId in sp.AllocatedNodes)
                {
                    if (_node.Connections.Contains(allocatedId))
                    {
                        _canAllocate = !_isAllocated;
                        break;
                    }
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null) return;
            UpdateState(sp);

            var rect = GetDimensions().ToRectangle();
            var center = rect.Center;
            int radius = rect.Width / 2;

            // Color base segun tipo (estilo PoE: tonos apagados cuando inactivos)
            Color baseColor = _node.Type switch
            {
                NodeType.Small => new Color(100, 100, 130),
                NodeType.Notable => new Color(70, 130, 220),
                NodeType.Keystone => new Color(245, 196, 81),
                NodeType.Cluster => new Color(70, 180, 110),
                NodeType.Ascendancy => new Color(160, 70, 240),
                _ => Color.White,
            };

            // Dim si no disponible
            if (!_isAllocated && !_canAllocate)
                baseColor *= 0.2f;

            // Glow exterior si asignado (estilo PoE: nodos asignados brillan)
            if (_isAllocated)
            {
                for (int i = 3; i > 0; i--)
                {
                    int glowR = radius + i * 6;
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(center.X - glowR, center.Y - glowR, glowR * 2, glowR * 2),
                        new Color(245, 196, 81, 15 - i * 3));
                }
            }

            // Glow al hacer hover
            if (_isHovered && (_canAllocate || _isAllocated))
            {
                for (int i = 2; i > 0; i--)
                {
                    int glowR = radius + i * 5;
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(center.X - glowR, center.Y - glowR, glowR * 2, glowR * 2),
                        new Color(255, 255, 255, 10 - i * 3));
                }
            }

            // Circulo del nodo (fondo)
            Color fillColor = _isAllocated
                ? new Color(255, 230, 150)
                : (_canAllocate ? baseColor : baseColor * 0.15f);

            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2),
                fillColor);

            // Borde (estilo PoE: borde definido)
            Color borderColor = _isAllocated
                ? new Color(255, 245, 200)
                : _isHovered ? Color.White
                : new Color(baseColor.R + 30, baseColor.G + 30, baseColor.B + 30);

            int b = 2;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(center.X - radius, center.Y - radius, radius * 2, b), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(center.X - radius, center.Y + radius - b, radius * 2, b), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(center.X - radius, center.Y - radius, b, radius * 2), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(center.X + radius - b, center.Y - radius, b, radius * 2), borderColor);

            // Punto interior para small nodes (estilo PoE)
            if (_node.Type == NodeType.Small)
            {
                int dotR = radius / 3;
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(center.X - dotR, center.Y - dotR, dotR * 2, dotR * 2),
                    _isAllocated ? new Color(255, 245, 200) : new Color(baseColor.R + 50, baseColor.G + 50, baseColor.B + 50));
            }

            // Icono de costo
            if (_node.Cost > 0)
            {
                Utils.DrawBorderString(spriteBatch, _node.Cost.ToString(),
                    new Vector2(center.X, center.Y + radius + 4),
                    _isAllocated ? new Color(255, 230, 150) : new Color(150, 140, 170),
                    0.7f, 0.5f, 0f);
            }

            // Tooltip al hacer hover
            if (_isHovered)
            {
                string typeStr = _node.Type switch
                {
                    NodeType.Small => "[Small]",
                    NodeType.Notable => "[Notable]",
                    NodeType.Keystone => "[KEYSTONE]",
                    NodeType.Cluster => "[Cluster]",
                    NodeType.Ascendancy => "[Ascendancy]",
                    _ => "",
                };
                string status = _isAllocated ? "(Asignado)" : (_canAllocate ? "(Disponible)" : "(Bloqueado)");
                Main.instance.MouseText($"{typeStr} {_node.Name}\n{_node.Effect}\nCoste: {_node.Cost} pts {status}");
            }
        }
    }
}
