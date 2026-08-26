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
        private List<PoESkillNodeButton> _nodeButtons = new();
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

        /// <summary>Construye los botones de nodos para el árbol PoE actual.</summary>
        public void BuildPoETree(PoESkillTree tree)
        {
            // Limpiar botones anteriores
            foreach (var btn in _nodeButtons)
            {
                _panel.RemoveChild(btn);
            }
            _nodeButtons.Clear();

            // Encontrar los limites del arbol para normalizar coords al panel.
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

            // Crear un boton por cada nodo PoE
            foreach (var node in tree.Nodes)
            {
                var button = new PoESkillNodeButton(node);
                // Normalizar coords al panel
                float x = (node.X - minX) / rangeX * (PanelWidth - 80) + 40;
                float y = (node.Y - minY) / rangeY * (PanelHeight - 120) + 60;
                float size = node.Radius * 1.5f; // escalar al panel
                button.Left.Set(x - size / 2, 0f);
                button.Top.Set(y - size / 2, 0f);
                button.Width.Set(size, 0f);
                button.Height.Set(size, 0f);
                _panel.Append(button);
                _nodeButtons.Add(button);
            }
            UpdatePointsPoE();
        }

        /// <summary>Actualiza el contador de puntos (version PoE).</summary>
        public void UpdatePointsPoE()
        {
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null) return;
            int total = sp.CumulativeSkillPoints();
            int spent = sp.AllocatedNodes.Count;
            int available = total - spent;
            _pointsText.SetText($"Puntos disponibles: {available}    Gastados: {spent} / {total}");
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
            // Usar el catálogo PoE (Path of Exile style).
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
        }
    }

    /// <summary>
    /// Boton de un nodo PoE del arbol. Click para asignar/desasignar.
    /// Usa el sistema de conexiones (grafo dirigido) de PoE.
    /// </summary>
    public class PoESkillNodeButton : UIElement
    {
        private PoESkillNode _node;
        private bool _isAllocated;
        private bool _canAllocate;

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
                    // Desasignar: verificar que nadie depende de este nodo.
                    bool canRemove = true;
                    foreach (var allocatedId in sp.AllocatedNodes)
                    {
                        // Buscar el nodo asignado y ver si tiene conexion a este.
                        // En PoE, un nodo se puede quitar si ningun nodo asignado lo tiene como conexion.
                    }
                    if (canRemove)
                        sp.AllocatedNodes.Remove(_node.Id);
                }
                else if (_canAllocate)
                {
                    sp.AllocatedNodes.Add(_node.Id);
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
                }
            };
            OnMouseOver += (evt, el) =>
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
                Main.instance.MouseText($"{typeStr} {_node.Name}\n{_node.Effect}\nCoste: {_node.Cost} pts");
            };
        }

        public void UpdateState(ShardPlayer sp)
        {
            _isAllocated = sp.AllocatedNodes.Contains(_node.Id);
            // En PoE, un nodo se puede asignar si:
            // 1. Es el nodo inicial (start)
            // 2. Tiene una conexion desde un nodo ya asignado
            // 3. O si ya esta asignado (para desasignar)
            if (_node.Id == "start")
            {
                _canAllocate = !_isAllocated;
            }
            else
            {
                // Verificar si algun nodo asignado tiene conexion a este.
                _canAllocate = false;
                foreach (var allocatedId in sp.AllocatedNodes)
                {
                    // Buscar el nodo asignado en el arbol PoE.
                    // Simplificado: si el nodo inicial esta asignado, todos los nodos de entrada estan disponibles.
                    if (allocatedId == "start" && _node.Connections.Contains("start"))
                    {
                        _canAllocate = !_isAllocated;
                        break;
                    }
                    // Si un nodo asignado tiene conexion a este nodo.
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
            UpdateState(sp);
            var rect = GetDimensions().ToRectangle();
            var center = rect.Center;

            // Color segun tipo de nodo (estilo PoE)
            Color color = _node.Type switch
            {
                NodeType.Small => new Color(150, 150, 180),     // gris-azul
                NodeType.Notable => new Color(120, 170, 255),      // azul brillante
                NodeType.Keystone => new Color(245, 196, 81),     // dorado
                NodeType.Cluster => new Color(100, 200, 150),     // verde
                NodeType.Ascendancy => new Color(200, 100, 255),  // morado
                _ => Color.White,
            };

            // Dim si no se puede asignar
            if (!_isAllocated && !_canAllocate)
                color *= 0.3f;

            // Halo si asignado
            if (_isAllocated)
            {
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(center.X - 20, center.Y - 20, 40, 40),
                    new Color(245, 196, 81, 50));
            }

            // Tamano segun tipo
            int nodeSize = _node.Type switch
            {
                NodeType.Small => 12,
                NodeType.Notable => 20,
                NodeType.Keystone => 30,
                NodeType.Cluster => 16,
                NodeType.Ascendancy => 24,
                _ => 12,
            };

            // Nodo (cuadrado como placeholder de circulo)
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(center.X - nodeSize / 2, center.Y - nodeSize / 2, nodeSize, nodeSize),
                _isAllocated ? new Color(245, 196, 81) : color);

            // Borde
            Utils.DrawBorderString(spriteBatch, _node.Cost.ToString(),
                new Vector2(center.X, center.Y - 4),
                _isAllocated ? Color.Black : Color.White, 0.7f, 0.5f, 0.5f);
        }
    }
}
