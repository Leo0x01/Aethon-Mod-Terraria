using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
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
    /// Estado de UI del Códex de Memoria.
    /// Lista las armas absorbibles del juego base y permite memorizarlas.
    /// Se abre/cierra con la tecla 'J'.
    /// </summary>
    public class MemoryCodexUIState : UIState
    {
        public const int PanelWidth = 700;
        public const int PanelHeight = 500;

        private UIPanel _panel = null!;
        private UIText _titleText = null!;
        private UIText _infoText = null!;
        private UIList _entryList = null!;
        private UIScrollbar _scrollbar = null!;

        public override void OnInitialize()
        {
            _panel = new UIPanel();
            _panel.Width.Set(PanelWidth, 0f);
            _panel.Height.Set(PanelHeight, 0f);
            _panel.HAlign = 0.5f;
            _panel.VAlign = 0.5f;
            _panel.BackgroundColor = new Color(20, 15, 40, 240);
            _panel.BorderColor = new Color(179, 136, 255, 180);
            Append(_panel);

            _titleText = new UIText("Códex de Memoria — Absorción de Lore", 1.2f)
            {
                HAlign = 0.5f,
                Top = { Pixels = 10 },
                TextColor = new Color(179, 136, 255),
            };
            _panel.Append(_titleText);

            _infoText = new UIText("", 0.85f)
            {
                HAlign = 0.5f,
                Top = { Pixels = 40 },
                TextColor = new Color(245, 196, 81),
            };
            _panel.Append(_infoText);

            // Lista de entradas con scroll
            _entryList = new UIList
            {
                Width = { Pixels = PanelWidth - 60 },
                Height = { Pixels = PanelHeight - 120 },
                Top = { Pixels = 70 },
                Left = { Pixels = 20 },
                ListPadding = 4f,
            };
            _panel.Append(_entryList);

            _scrollbar = new UIScrollbar
            {
                Height = { Pixels = PanelHeight - 120 },
                Top = { Pixels = 70 },
                Left = { Pixels = PanelWidth - 30 },
            };
            _panel.Append(_scrollbar);
            _entryList.SetScrollbar(_scrollbar);

            // Botón cerrar
            var closeButton = new UITextPanel<string>("Cerrar (J)")
            {
                Width = { Pixels = 120 },
                Height = { Pixels = 30 },
                HAlign = 1f,
                Top = { Pixels = 8 },
                Left = { Pixels = -8 },
                BackgroundColor = new Color(60, 40, 80, 200),
                BorderColor = new Color(180, 120, 255, 120),
            };
            closeButton.OnLeftClick += (evt, el) => Hide();
            _panel.Append(closeButton);
        }

        public void Show()
        {
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted) return;

            // Construir lista de entradas para la rama activa.
            BuildEntries(sp);
            Visible = true;
        }

        public void Hide()
        {
            Visible = false;
        }

        private void BuildEntries(ShardPlayer sp)
        {
            _entryList.Clear();
            var entries = MemoryCodexSystem.GetCodexForBranch(sp.ActiveBranch);
            int slots = sp.RuneSlots();

            _infoText.SetText($"Runas equipadas: {sp.MemorizedRunes.Count} / {slots}    " +
                              $"Resonancia: {sp.ResonanceShards} ✦");

            foreach (var entry in entries)
            {
                var row = new CodexEntryRow(entry, sp.MemorizedRunes.Contains(entry.Name));
                _entryList.Add(row);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            // Actualizar info text
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp != null)
            {
                _infoText.SetText($"Runas equipadas: {sp.MemorizedRunes.Count} / {sp.RuneSlots()}    " +
                                  $"Resonancia: {sp.ResonanceShards} ✦");
            }
        }
    }

    /// <summary>Fila del códex: muestra un arma absorbible + botón memorizar.</summary>
    public class CodexEntryRow : UIElement
    {
        private MemoryCodexSystem.CodexEntry _entry;
        private bool _memorized;

        public CodexEntryRow(MemoryCodexSystem.CodexEntry entry, bool memorized)
        {
            _entry = entry;
            _memorized = memorized;
            Width.Set(0, 1f);
            Height.Set(32, 0f);
            MarginTop = 2f;
        }

        public override void OnInitialize()
        {
            var nameText = new UIText($"{_entry.Name} — {_entry.Signature}", 0.85f)
            {
                Left = { Pixels = 8 },
                Top = { Pixels = 6 },
                TextColor = _memorized ? new Color(245, 196, 81) : Color.White,
            };
            Append(nameText);

            var costText = new UIText($"{_entry.ResonanceCost} ✦", 0.8f)
            {
                HAlign = 1f,
                Top = { Pixels = 6 },
                Left = { Pixels = -120 },
                TextColor = new Color(245, 196, 81),
            };
            Append(costText);

            var button = new UITextPanel<string>(_memorized ? "✓" : "Memorizar")
            {
                Width = { Pixels = 80 },
                Height = { Pixels = 24 },
                HAlign = 1f,
                Top = { Pixels = 4 },
                Left = { Pixels = -8 },
                BackgroundColor = _memorized
                    ? new Color(60, 80, 60, 200)
                    : new Color(60, 40, 80, 200),
                BorderColor = new Color(180, 120, 255, 120),
            };
            if (!_memorized)
            {
                button.OnLeftClick += (evt, el) =>
                {
                    var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
                    if (sp == null) return;
                    if (MemoryCodexSystem.Memorize(Main.LocalPlayer, _entry))
                    {
                        // Refrescar la UI
                        var codexUI = ModContent.GetInstance<UISystem>()?.CodexUI;
                        codexUI?.Show();
                    }
                };
            }
            Append(button);
        }
    }
}
