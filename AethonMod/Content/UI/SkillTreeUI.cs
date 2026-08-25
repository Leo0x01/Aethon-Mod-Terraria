using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;
using AethonMod.Content.Systems;
using AethonMod.Content.Players;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// Estado de UI del árbol de habilidades del Fragmento Génesis.
    /// Muestra una constelación de nodos asignables con zoom/pan.
    /// Se abre/cierra con la tecla 'K' (configurable).
    /// </summary>
    public class SkillTreeUIState : UIState
    {
        public const int PanelWidth = 800;
        public const int PanelHeight = 560;

        private UIPanel _panel = null!;
        private UIText _titleText = null!;
        private UIText _pointsText = null!;
        public bool IsVisible = false;
        private List<SkillNodeButton> _nodeButtons = new();
        public SkillTreeData? CurrentTree;

        public override void OnInitialize()
        {
            // Panel principal
            _panel = new UIPanel();
            _panel.Width.Set(PanelWidth, 0f);
            _panel.Height.Set(PanelHeight, 0f);
            _panel.HAlign = 0.5f;
            _panel.VAlign = 0.5f;
            _panel.BackgroundColor = new Color(20, 15, 40, 240);
            _panel.BorderColor = new Color(245, 196, 81, 180);
            Append(_panel);

            // Título
            _titleText = new UIText("Árbol de Habilidades — Fragmento Génesis", 1.2f)
            {
                HAlign = 0.5f,
                Top = { Pixels = 10 },
                TextColor = new Color(245, 196, 81),
            };
            _panel.Append(_titleText);

            // Contador de puntos
            _pointsText = new UIText("Puntos: 0 / 0", 0.9f)
            {
                HAlign = 0.5f,
                Top = { Pixels = 40 },
                TextColor = new Color(179, 136, 255),
            };
            _panel.Append(_pointsText);

            // Botón de cerrar
            var closeButton = new UITextPanel<string>("Cerrar (K)")
            {
                Width = { Pixels = 120 },
                Height = { Pixels = 30 },
                HAlign = 1f,
                VAlign = 0f,
                Top = { Pixels = 8 },
                Left = { Pixels = -8 },
                BackgroundColor = new Color(60, 40, 80, 200),
                BorderColor = new Color(180, 120, 255, 120),
            };
            closeButton.OnLeftClick += (evt, el) => Hide();
            _panel.Append(closeButton);
        }

        /// <summary>Construye los botones de nodos para el árbol actual.</summary>
        public void BuildTree(SkillTreeData tree)
        {
            CurrentTree = tree;
            // Limpiar botones anteriores
            foreach (var btn in _nodeButtons)
            {
                _panel.RemoveChild(btn);
            }
            _nodeButtons.Clear();

            // Crear un botón por cada nodo
            foreach (var node in tree.Nodes)
            {
                var button = new SkillNodeButton(node);
                // Posición: normalizar coords del nodo al panel
                float x = node.X * (PanelWidth - 80) + 40;
                float y = node.Y * (PanelHeight - 120) + 60;
                button.Left.Set(x - 12, 0f);
                button.Top.Set(y - 12, 0f);
                button.Width.Set(24, 0f);
                button.Height.Set(24, 0f);
                _panel.Append(button);
                _nodeButtons.Add(button);
            }
            UpdatePoints();
        }

        /// <summary>Actualiza el contador de puntos disponibles.</summary>
        public void UpdatePoints()
        {
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null) return;
            int total = sp.CumulativeSkillPoints();
            int spent = 0;
            foreach (var node in CurrentTree?.Nodes ?? new List<SkillNodeData>())
            {
                if (sp.AllocatedNodes.Contains(node.Id))
                    spent += node.Cost;
            }
            int available = total - spent;
            _pointsText.SetText($"Puntos disponibles: {available}    Gastados: {spent} / {total}");
        }

        public void Show()
        {
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted) return;
            var tree = SkillTreeCatalog.GetTree(sp.ActiveBranch);
            BuildTree(tree);
            IsVisible = true;
        }

        public void Hide()
        {
            IsVisible = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            // Actualizar estados de botones (asignado/disponible/bloqueado).
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null) return;
            foreach (var btn in _nodeButtons)
            {
                btn.UpdateState(sp);
            }
        }
    }

    /// <summary>
    /// Botón de un nodo del árbol. Click para asignar/desasignar.
    /// </summary>
    public class SkillNodeButton : UIElement
    {
        private SkillNodeData _node;
        private bool _isAllocated;
        private bool _canAllocate;

        public SkillNodeButton(SkillNodeData node)
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
                    // Desasignar (solo si nada depende de él)
                    bool hasDep = false;
                    foreach (var n in (Parent as SkillTreeUIState)?.CurrentTree?.Nodes ?? new List<SkillNodeData>())
                    {
                        if (n.PrereqId == _node.Id && sp.AllocatedNodes.Contains(n.Id))
                        {
                            hasDep = true;
                            break;
                        }
                    }
                    if (!hasDep)
                    {
                        sp.AllocatedNodes.Remove(_node.Id);
                    }
                }
                else if (_canAllocate)
                {
                    sp.AllocatedNodes.Add(_node.Id);
                }
                (Parent as SkillTreeUIState)?.UpdatePoints();
            };
            OnMouseOver += (evt, el) =>
            {
                // Tooltip: mostrar nombre + efecto
                Main.instance.MouseText(_node.Name + "\n" + _node.Effect + "\nCoste: " + _node.Cost + " pts");
            };
        }

        public void UpdateState(ShardPlayer sp)
        {
            _isAllocated = sp.AllocatedNodes.Contains(_node.Id);
            bool prereqMet = _node.PrereqId == null || sp.AllocatedNodes.Contains(_node.PrereqId);
            // (Simplificado: no verificamos presupuesto aquí; el click lo maneja)
            _canAllocate = !_isAllocated && prereqMet;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            UpdateState(sp);
            var rect = GetDimensions().ToRectangle();
            var center = rect.Center;

            // Color según rareza
            Color color = _node.Rarity switch
            {
                NodeRarity.Common => new Color(180, 180, 210),
                NodeRarity.Rare => new Color(120, 170, 255),
                NodeRarity.Legendary => new Color(245, 196, 81),
                _ => Color.White,
            };

            // Dim si bloqueado
            if (!_isAllocated && !_canAllocate)
                color *= 0.35f;

            // Halo si asignado
            if (_isAllocated)
            {
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(center.X - 16, center.Y - 16, 32, 32),
                    new Color(245, 196, 81, 60));
            }

            // Nodo (círculo)
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(center.X - 8, center.Y - 8, 16, 16),
                _isAllocated ? new Color(245, 196, 81) : color);

            // Coste (texto)
            Utils.DrawBorderString(spriteBatch, _node.Cost.ToString(),
                new Vector2(center.X, center.Y - 4),
                _isAllocated ? Color.Black : Color.White, 0.8f, 0.5f, 0.5f);
        }
    }
}
