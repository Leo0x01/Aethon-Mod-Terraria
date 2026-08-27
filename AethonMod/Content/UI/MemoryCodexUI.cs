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
    /// Vista del Códex de Memoria — UIElement que dibuja la lista de armas memorizables
    /// dentro de un DraggablePanel. Scrollable con la rueda.
    /// </summary>
    public class CodexListView : UIElement
    {
        private List<MemoryCodexSystem.CodexEntry> _entries = new();
        private int _scrollY = 0;
        private int _lastScrollValue = 0;
        private bool _mouseLeftPressed = false;
        private float _time = 0f;

        // Estrellas de fondo
        private struct Star { public Vector2 Pos; public float Size; public float Twinkle; public float Phase; public Color Color; }
        private Star[]? _stars;

        private MemoryCodexSystem.CodexEntry? _hoveredEntry;

        public CodexListView()
        {
            MarginTop = 44;
            MarginBottom = 8;
            MarginLeft = 8;
            MarginRight = 8;
            InitStars();
        }

        public void SetEntries(List<MemoryCodexSystem.CodexEntry> entries)
        {
            _entries = entries ?? new List<MemoryCodexSystem.CodexEntry>();
            _scrollY = 0;
        }

        private void InitStars()
        {
            if (_stars != null) return;
            var rand = new Random(42);
            _stars = new Star[60];
            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i] = new Star
                {
                    Pos = new Vector2((float)rand.NextDouble(), (float)rand.NextDouble()),
                    Size = 1f + (float)rand.NextDouble() * 2f,
                    Twinkle = 0.5f + (float)rand.NextDouble() * 2f,
                    Phase = (float)rand.NextDouble() * MathF.PI * 2f,
                    Color = i % 3 == 0 ? new Color(245, 196, 81)
                          : i % 3 == 1 ? new Color(179, 136, 255)
                          : new Color(200, 220, 255),
                };
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _time += 0.016f;

            var dims = GetDimensions();
            Rectangle viewRect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

            // Scroll con rueda
            int curScroll = Terraria.GameInput.PlayerInput.ScrollWheelValue;
            int scrollDelta = curScroll - _lastScrollValue;
            _lastScrollValue = curScroll;
            if (scrollDelta != 0 && viewRect.Contains(Main.mouseX, Main.mouseY))
            {
                _scrollY -= Math.Sign(scrollDelta) * 30;
                int totalH = _entries.Count * 44;
                int maxScroll = Math.Max(0, totalH - (int)viewRect.Height);
                _scrollY = Math.Max(0, Math.Min(_scrollY, maxScroll));
            }
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            var dims = GetDimensions();
            Rectangle viewRect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

            // Fondo oscuro
            sb.Draw(TextureAssets.MagicPixel.Value, viewRect, new Color(8, 6, 16, 245));

            // Estrellas
            if (_stars != null)
            {
                for (int i = 0; i < _stars.Length; i++)
                {
                    var s = _stars[i];
                    float sx = viewRect.X + s.Pos.X * viewRect.Width;
                    float sy = viewRect.Y + s.Pos.Y * viewRect.Height;
                    float twinkle = 0.5f + 0.5f * (float)Math.Sin(_time * s.Twinkle + s.Phase);
                    int alpha = (int)(180 * twinkle * 0.6f);
                    if (alpha < 0) alpha = 0; if (alpha > 255) alpha = 255;
                    Color c = new Color(s.Color.R, s.Color.G, s.Color.B, alpha);
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)sx, (int)sy, (int)s.Size, (int)s.Size), c);
                }
            }

            // Nebulosa
            float pulse = 0.85f + (float)Math.Sin(_time * 0.5) * 0.15f;
            DrawRadial(sb, viewRect, 0.5f, 0.5f, 0.3f * pulse, new Color(80, 40, 160, 12));

            // Subheader: stats
            int slots = sp.RuneSlots();
            string statsText = $"Runas: {sp.MemorizedRunes.Count}/{slots}  |  Resonancia: {sp.ResonanceShards} ✦";
            Utils.DrawBorderString(sb, statsText,
                new Vector2(viewRect.X + viewRect.Width / 2f, viewRect.Y + 4),
                new Color(245, 196, 81), 0.8f, 0.5f, 0f);

            // Lista de entradas
            int entryH = 44;
            int listTop = viewRect.Y + 24 - _scrollY;
            int listBottom = viewRect.Bottom;
            int listH = listBottom - listTop - 4;

            _hoveredEntry = null;
            for (int i = 0; i < _entries.Count; i++)
            {
                int ey = listTop + i * entryH;
                if (ey + entryH < viewRect.Y + 24 || ey > listBottom) continue;

                var entry = _entries[i];
                bool memorized = sp.MemorizedRunes.Contains(entry.Name);
                int rowX = viewRect.X + 8;
                int rowW = viewRect.Width - 16;
                bool hovered = Main.mouseX >= rowX && Main.mouseX <= rowX + rowW &&
                               Main.mouseY >= ey && Main.mouseY <= ey + entryH - 6;
                if (hovered) _hoveredEntry = entry;

                // Fila
                Color rowColor;
                if (memorized) rowColor = new Color(245, 196, 81, 30);
                else if (hovered) rowColor = new Color(179, 136, 255, 30);
                else rowColor = new Color(20, 15, 40, 120) * (i % 2 == 0 ? 1f : 0.7f);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(rowX, ey, rowW, entryH - 6), rowColor);

                // Indicador izquierdo
                Color indicator = memorized ? new Color(120, 220, 100, 200) : hovered ? new Color(245, 196, 81, 200) : new Color(80, 60, 120, 150);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rowX, ey, 3, entryH - 6), indicator);

                // Icono de rama
                string icon = sp.ActiveBranch == BranchType.Distance ? "→" : sp.ActiveBranch == BranchType.Melee ? "⚔" : "✦";
                Utils.DrawBorderString(sb, icon, new Vector2(rowX + 14, ey + 10),
                    memorized ? new Color(245, 196, 81) : Color.White, 1.0f);

                // Nombre + signature
                Utils.DrawBorderString(sb, entry.Name, new Vector2(rowX + 36, ey + 6),
                    memorized ? new Color(245, 196, 81) : Color.White, 0.85f);
                Utils.DrawBorderString(sb, entry.Signature, new Vector2(rowX + 36, ey + 22),
                    new Color(160, 150, 180), 0.7f);

                // Costo
                string costText = $"{entry.ResonanceCost} ✦";
                Utils.DrawBorderString(sb, costText, new Vector2(rowX + rowW - 130, ey + 14),
                    new Color(245, 196, 81), 0.8f);

                // Boton Memorizar/Olvidar
                int btnW = 90, btnH = 26;
                int btnX = rowX + rowW - btnW - 8;
                int btnY = ey + 6;
                bool hoverBtn = Main.mouseX >= btnX && Main.mouseX <= btnX + btnW &&
                                Main.mouseY >= btnY && Main.mouseY <= btnY + btnH;
                Color btnColor = memorized
                    ? (hoverBtn ? new Color(220, 80, 80, 230) : new Color(120, 50, 50, 200))
                    : (hoverBtn ? new Color(245, 196, 81, 230) : new Color(60, 40, 80, 180));
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(btnX, btnY, btnW, btnH), btnColor);
                Utils.DrawBorderString(sb, memorized ? "Olvidar" : "Memorizar",
                    new Vector2(btnX + btnW / 2f, btnY + btnH / 2f - 8),
                    memorized ? (hoverBtn ? Color.White : new Color(220, 180, 180)) : (hoverBtn ? Color.Black : new Color(220, 220, 240)),
                    0.75f, 0.5f, 0.5f);

                // Click
                bool clicked = hoverBtn && Main.mouseLeft && !_mouseLeftPressed;
                if (clicked)
                {
                    _mouseLeftPressed = true;
                    if (!memorized)
                    {
                        if (sp.ResonanceShards >= entry.ResonanceCost && sp.MemorizedRunes.Count < slots)
                        {
                            MemoryCodexSystem.Memorize(Main.LocalPlayer, entry);
                            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
                            for (int p = 0; p < 12; p++)
                                Dust.NewDustPerfect(Main.LocalPlayer.Center, Terraria.ID.DustID.GoldFlame,
                                    new Vector2(Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4)),
                                    100, new Color(245, 196, 81), 1.2f);
                        }
                        else if (sp.ResonanceShards < entry.ResonanceCost)
                        {
                            Main.NewText($"Necesitas {entry.ResonanceCost} resonancia, tienes {sp.ResonanceShards}.", new Color(255, 100, 100));
                        }
                        else
                        {
                            Main.NewText($"Slots de runa llenos. Max: {slots}.", new Color(255, 100, 100));
                        }
                    }
                    else
                    {
                        MemoryCodexSystem.Forget(Main.LocalPlayer, entry.Name);
                        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
                    }
                }
            }
            if (!Main.mouseLeft) _mouseLeftPressed = false;

            // Scrollbar
            int totalH = _entries.Count * entryH;
            int contentH = listBottom - (viewRect.Y + 24) - 4;
            if (totalH > contentH)
            {
                int barTrack = contentH;
                int barH = Math.Max(30, barTrack * contentH / totalH);
                int barY = viewRect.Y + 24 + (_scrollY * (contentH - barH) / (totalH - contentH));
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(viewRect.Right - 12, viewRect.Y + 24, 8, contentH),
                    new Color(30, 20, 50, 180));
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(viewRect.Right - 12, barY, 8, barH),
                    new Color(179, 136, 255, 220));
            }

            // Empty state
            if (_entries.Count == 0)
            {
                Utils.DrawBorderString(sb, "No hay armas memorizables.",
                    new Vector2(viewRect.X + viewRect.Width / 2f, viewRect.Y + viewRect.Height / 2f),
                    new Color(180, 160, 220), 0.85f, 0.5f, 0.5f);
            }

            // Bloquear input del juego dentro de la vista
            if (viewRect.Contains(Main.mouseX, Main.mouseY))
            {
                Main.mouseLeft = false;
                Main.mouseRight = false;
            }
        }

        private void DrawRadial(SpriteBatch sb, Rectangle rect, float xPct, float yPct, float rPct, Color color)
        {
            float cx = rect.X + rect.Width * xPct;
            float cy = rect.Y + rect.Height * yPct;
            float r = rect.Width * rPct;
            if (r <= 0) return;
            for (int i = (int)r; i > 0; i -= 12)
            {
                int alpha = (int)(color.A * (1f - (float)i / r) * 0.3f);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)(cx - i), (int)(cy - i), i * 2, i * 2),
                    new Color(color.R, color.G, color.B, alpha));
            }
        }
    }

    /// <summary>
    /// Estado del Códex de Memoria — UIState que contiene un DraggablePanel
    /// con el CodexListView dentro.
    /// </summary>
    public class MemoryCodexUIState : UIState
    {
        public bool IsVisible = false;
        private DraggablePanel? _panel;
        private CodexListView? _listView;

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
            _panel = new DraggablePanel(600, 460, "★ CODEX DE MEMORIA ★");
            _panel.OnCloseClick += (evt, el) => Hide();

            _listView = new CodexListView();
            _listView.Width.Set(0, 1f);
            _listView.Height.Set(0, 1f);
            _listView.Top.Set(44, 0f);

            var entries = MemoryCodexSystem.GetCodexForBranch(sp.ActiveBranch);
            _listView.SetEntries(entries);

            _panel.Append(_listView);
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
            _listView = null;
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
        }
    }
}
