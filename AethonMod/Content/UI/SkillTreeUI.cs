using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    /// - Panel oscuro con zoom/pan
    /// - Nodos circulares conectados por lineas
    /// - Click para asignar
    /// - Hover para tooltip
    /// </summary>
    public class SkillTreeUIState : UIState
    {
        public const int PanelWidth = 900;
        public const int PanelHeight = 600;
        public bool IsVisible;
        public PoESkillTree? CurrentTree;

        private UIPanel _panel = null!;
        private UIText _titleText = null!;
        private UIText _pointsText = null!;
        private List<PoESkillNodeButton> _nodeButtons = new();
        private RenderTarget2D? _connectionTarget;

        public override void OnInitialize()
        {
            _panel = new UIPanel();
            _panel.Width.Set(PanelWidth, 0f);
            _panel.Height.Set(PanelHeight, 0f);
            _panel.HAlign = 0.5f;
            _panel.VAlign = 0.5f;
            _panel.BackgroundColor = new Color(10, 8, 20, 245);
            _panel.BorderColor = new Color(120, 90, 200, 200);
            Append(_panel);

            _titleText = new UIText("Arbol de Habilidades", 1.3f);
            _titleText.HAlign = 0.5f;
            _titleText.Top.Set(8, 0f);
            _titleText.TextColor = new Color(245, 196, 81);
            _panel.Append(_titleText);

            _pointsText = new UIText("Puntos: 0", 0.9f);
            _pointsText.HAlign = 0.5f;
            _pointsText.Top.Set(35, 0f);
            _pointsText.TextColor = new Color(179, 136, 255);
            _panel.Append(_pointsText);

            var closeButton = new UITextPanel<string>("Cerrar");
            closeButton.Width.Set(100, 0f);
            closeButton.Height.Set(28, 0f);
            closeButton.HAlign = 1f;
            closeButton.Top.Set(8, 0f);
            closeButton.Left.Set(-8, 0f);
            closeButton.BackgroundColor = new Color(60, 30, 50, 200);
            closeButton.BorderColor = new Color(180, 80, 80, 150);
            closeButton.OnLeftClick += (evt, el) => Hide();
            _panel.Append(closeButton);
        }

        public void BuildPoETree(PoESkillTree tree)
        {
            CurrentTree = tree;
            foreach (var btn in _nodeButtons)
                _panel.RemoveChild(btn);
            _nodeButtons.Clear();

            // Calcular limites para normalizar coords al panel.
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

            // Padding dentro del panel
            float padX = 60f;
            float padY = 70f;
            float usableW = PanelWidth - padX * 2;
            float usableH = PanelHeight - padY * 2;

            foreach (var node in tree.Nodes)
            {
                var button = new PoESkillNodeButton(node);
                float x = (node.X - minX) / rangeX * usableW + padX;
                float y = (node.Y - minY) / rangeY * usableH + padY;
                float size = node.Radius * 1.8f;
                button.Left.Set(x - size / 2, 0f);
                button.Top.Set(y - size / 2, 0f);
                button.Width.Set(size, 0f);
                button.Height.Set(size, 0f);
                _panel.Append(button);
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

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;
            base.Draw(spriteBatch);

            // Dibujar conexiones entre nodos (lineas estilo PoE).
            if (CurrentTree == null) return;
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            // Calcular limites para mapear coords de nodos a coords de pantalla.
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var node in CurrentTree.Nodes)
            {
                minX = System.Math.Min(minX, node.X);
                maxX = System.Math.Max(maxX, node.X);
                minY = System.Math.Min(minY, node.Y);
                maxY = System.Math.Max(maxY, node.Y);
            }
            float rangeX = maxX - minX + 1;
            float rangeY = maxY - minY + 1;
            float padX = 60f, padY = 70f;
            float usableW = PanelWidth - padX * 2;
            float usableH = PanelHeight - padY * 2;

            // Posicion del panel en pantalla
            float panelX = (Main.screenWidth - PanelWidth) / 2f;
            float panelY = (Main.screenHeight - PanelHeight) / 2f;

            foreach (var node in CurrentTree.Nodes)
            {
                float nx = (node.X - minX) / rangeX * usableW + padX + panelX;
                float ny = (node.Y - minY) / rangeY * usableH + padY + panelY;

                foreach (var connId in node.Connections)
                {
                    var target = CurrentTree.Nodes.Find(n => n.Id == connId);
                    if (target == null) continue;

                    float tx = (target.X - minX) / rangeX * usableW + padX + panelX;
                    float ty = (target.Y - minY) / rangeY * usableH + padY + panelY;

                    // Linea mas gruesa si ambos nodos estan asignados.
                    bool bothAllocated = sp.AllocatedNodes.Contains(node.Id) && sp.AllocatedNodes.Contains(target.Id);
                    Color lineColor = bothAllocated
                        ? new Color(245, 196, 81, 200)
                        : new Color(80, 60, 120, 120);
                    float thickness = bothAllocated ? 3f : 1.5f;

                    // Dibujar linea manualmente (Terraria no tiene DrawLine nativo facil).
                    DrawLine(spriteBatch, new Vector2(nx, ny), new Vector2(tx, ty), lineColor, thickness);
                }
            }
        }

        /// <summary>Dibuja una linea entre dos puntos usando pixeles escalados.</summary>
        private void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)System.Math.Atan2(edge.Y, edge.X);
            float length = edge.Length();
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)start.X, (int)start.Y, (int)length, (int)thickness),
                null, color, angle, new Vector2(0, thickness / 2f), SpriteEffects.None, 0);
        }
    }

    /// <summary>
    /// Nodo del arbol PoE. Circulo interactivo.
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

            // Color segun tipo (estilo PoE)
            Color baseColor = _node.Type switch
            {
                NodeType.Small => new Color(120, 120, 150),
                NodeType.Notable => new Color(80, 140, 240),
                NodeType.Keystone => new Color(245, 196, 81),
                NodeType.Cluster => new Color(80, 200, 130),
                NodeType.Ascendancy => new Color(180, 80, 255),
                _ => Color.White,
            };

            // Dim si no disponible
            if (!_isAllocated && !_canAllocate)
                baseColor *= 0.25f;

            // Glow al hacer hover
            if (_isHovered && (_canAllocate || _isAllocated))
            {
                for (int i = 0; i < 3; i++)
                {
                    int glowR = radius + 6 + i * 4;
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(center.X - glowR, center.Y - glowR, glowR * 2, glowR * 2),
                        new Color(baseColor.R, baseColor.G, baseColor.B, 30 - i * 8));
                }
            }

            // Glow si asignado
            if (_isAllocated)
            {
                int glowR = radius + 8;
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(center.X - glowR, center.Y - glowR, glowR * 2, glowR * 2),
                    new Color(245, 196, 81, 40));
            }

            // Circulo del nodo (usar pixel escalado como circulo aproximado)
            Color fillColor = _isAllocated
                ? new Color(245, 220, 140)
                : (_canAllocate ? baseColor : baseColor * 0.3f);

            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2),
                fillColor);

            // Borde
            Color borderColor = _isAllocated
                ? new Color(255, 240, 180)
                : (_isHovered ? Color.White : new Color(baseColor.R + 40, baseColor.G + 40, baseColor.B + 40));

            // Dibujar borde como 4 rectangulos finos
            int b = 2;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(center.X - radius, center.Y - radius, radius * 2, b), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(center.X - radius, center.Y + radius - b, radius * 2, b), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(center.X - radius, center.Y - radius, b, radius * 2), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(center.X + radius - b, center.Y - radius, b, radius * 2), borderColor);

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
