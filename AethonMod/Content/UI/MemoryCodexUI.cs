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
    /// Vista del Codex de Memoria — lista scrolleable de armas con fondo de grimorio.
    /// Las armas se obtienen dinamicamente via MagicWeaponScanner (no hardcodeado).
    /// </summary>
    public class CodexListView : UIElement
    {
        private List<MemoryCodexSystem.CodexEntry> _entries = new();
        private int _scrollY = 0;
        private int _lastScrollValue = 0;
        private bool _mouseLeftPressed = false;
        private float _time = 0f;

        // Textura de fondo (grimorio)
        private Texture2D? _bgTexture;

        private MemoryCodexSystem.CodexEntry? _hoveredEntry;

        public CodexListView()
        {
            MarginTop = 44;
            MarginBottom = 8;
            MarginLeft = 8;
            MarginRight = 8;
        }

        public void SetEntries(List<MemoryCodexSystem.CodexEntry> entries)
        {
            _entries = entries ?? new List<MemoryCodexSystem.CodexEntry>();
            _scrollY = 0;
        }

        private void LoadBgTexture()
        {
            if (_bgTexture != null) return;
            try
            {
                _bgTexture = ModContent.Request<Texture2D>("AethonMod/Content/UI/Textures/Codex_Background", AssetRequestMode.ImmediateLoad).Value;
            }
            catch
            {
                _bgTexture = null;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _time += 0.016f;
            LoadBgTexture();

            var dims = GetDimensions();
            Rectangle viewRect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

            // Scroll con rueda
            int curScroll = Terraria.GameInput.PlayerInput.ScrollWheelValue;
            int scrollDelta = curScroll - _lastScrollValue;
            _lastScrollValue = curScroll;
            if (scrollDelta != 0 && viewRect.Contains(Main.mouseX, Main.mouseY))
            {
                _scrollY -= Math.Sign(scrollDelta) * 30;
                int totalH = _entries.Count * 42;
                int maxScroll = Math.Max(0, totalH - (int)viewRect.Height + 30);
                _scrollY = Math.Max(0, Math.Min(_scrollY, maxScroll));
            }
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            var dims = GetDimensions();
            Rectangle viewRect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

            // === FONDO ARTÍSTICO (textura grimorio) ===
            if (_bgTexture != null)
            {
                sb.Draw(_bgTexture, viewRect, new Color(255, 255, 255, 180));
            }
            else
            {
                sb.Draw(TextureAssets.MagicPixel.Value, viewRect, new Color(15, 10, 25, 245));
            }

            // Overlay oscuro para legibilidad
            sb.Draw(TextureAssets.MagicPixel.Value, viewRect, new Color(5, 3, 12, 80));

            // Estrellas sutiles
            DrawAnimatedStars(sb, viewRect);

            // Subheader: stats
            int slots = sp.RuneSlots();
            string statsText = $"Runas: {sp.MemorizedRunes.Count}/{slots}  |  Resonancia: {sp.ResonanceShards} ✦  |  Armas: {_entries.Count}";
            // Fondo del subheader
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(viewRect.X, viewRect.Y, viewRect.Width, 24),
                new Color(10, 8, 20, 220));
            Utils.DrawBorderString(sb, statsText,
                new Vector2(viewRect.X + viewRect.Width / 2f, viewRect.Y + 6),
                new Color(245, 196, 81), 0.8f, 0.5f, 0f);

            // === LISTA DE ENTRADAS ===
            int entryH = 42;
            int listTopY = viewRect.Y + 28 - _scrollY;
            int listBottom = viewRect.Bottom - 4;

            _hoveredEntry = null;
            for (int i = 0; i < _entries.Count; i++)
            {
                int ey = listTopY + i * entryH;
                if (ey + entryH < viewRect.Y + 28 || ey > listBottom) continue;

                var entry = _entries[i];
                bool memorized = sp.MemorizedRunes.Contains(entry.Name);
                int rowX = viewRect.X + 6;
                int rowW = viewRect.Width - 18;
                bool hovered = Main.mouseX >= rowX && Main.mouseX <= rowX + rowW &&
                               Main.mouseY >= ey && Main.mouseY <= ey + entryH - 4;
                if (hovered) _hoveredEntry = entry;

                // Fila (alternar colores zebra para legibilidad)
                Color rowColor;
                if (memorized) rowColor = new Color(245, 196, 81, 25);
                else if (hovered) rowColor = new Color(179, 136, 255, 35);
                else rowColor = new Color(20, 15, 35, 80) * (i % 2 == 0 ? 1f : 0.6f);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(rowX, ey, rowW, entryH - 4), rowColor);

                // Indicador izquierdo de estado
                Color indicator = memorized ? new Color(120, 220, 100, 200) : hovered ? new Color(245, 196, 81, 200) : new Color(80, 60, 120, 120);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rowX, ey, 3, entryH - 4), indicator);

                // Icono de la rama (símbolo pequeño)
                string icon = sp.ActiveBranch == BranchType.Distance ? "→"
                            : sp.ActiveBranch == BranchType.Melee ? "†"
                            : "✦";
                Utils.DrawBorderString(sb, icon, new Vector2(rowX + 14, ey + 8),
                    memorized ? new Color(245, 196, 81) : Color.White, 0.9f);

                // Nombre del arma
                string displayName = entry.Name.Length > 22 ? entry.Name.Substring(0, 20) + "…" : entry.Name;
                Utils.DrawBorderString(sb, displayName, new Vector2(rowX + 32, ey + 6),
                    memorized ? new Color(245, 196, 81) : Color.White, 0.8f);

                // Descripción
                string desc = entry.Signature;
                if (desc.Length > 30) desc = desc.Substring(0, 28) + "…";
                Utils.DrawBorderString(sb, desc, new Vector2(rowX + 32, ey + 22),
                    new Color(160, 150, 180), 0.65f);

                // Costo
                string costText = $"{entry.ResonanceCost} ✦";
                Utils.DrawBorderString(sb, costText, new Vector2(rowX + rowW - 110, ey + 14),
                    new Color(245, 196, 81), 0.75f);

                // Boton Memorizar/Olvidar
                int btnW = 80, btnH = 24;
                int btnX = rowX + rowW - btnW - 4;
                int btnY = ey + 6;
                bool hoverBtn = Main.mouseX >= btnX && Main.mouseX <= btnX + btnW &&
                                Main.mouseY >= btnY && Main.mouseY <= btnY + btnH;
                Color btnColor = memorized
                    ? (hoverBtn ? new Color(220, 80, 80, 230) : new Color(100, 40, 40, 200))
                    : (hoverBtn ? new Color(245, 196, 81, 230) : new Color(50, 35, 70, 180));
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(btnX, btnY, btnW, btnH), btnColor);
                // Borde del boton
                Color btnBorder = memorized ? new Color(220, 80, 80, 150) : new Color(245, 196, 81, 100);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(btnX, btnY, btnW, 1), btnBorder);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(btnX, btnY + btnH - 1, btnW, 1), btnBorder);
                Utils.DrawBorderString(sb, memorized ? "Olvidar" : "Memorizar",
                    new Vector2(btnX + btnW / 2f, btnY + btnH / 2f - 8),
                    memorized ? (hoverBtn ? Color.White : new Color(220, 180, 180)) : (hoverBtn ? Color.Black : new Color(220, 220, 240)),
                    0.7f, 0.5f, 0.5f);

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
            int contentH = listBottom - (viewRect.Y + 28);
            if (totalH > contentH)
            {
                int barH = Math.Max(30, contentH * contentH / totalH);
                int barY = viewRect.Y + 28 + (_scrollY * (contentH - barH) / (totalH - contentH));
                // Track
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(viewRect.Right - 10, viewRect.Y + 28, 6, contentH),
                    new Color(20, 15, 35, 180));
                // Thumb
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(viewRect.Right - 10, barY, 6, barH),
                    new Color(179, 136, 255, 200));
            }

            // Empty state
            if (_entries.Count == 0)
            {
                Utils.DrawBorderString(sb, "No hay armas memorizables para esta rama.",
                    new Vector2(viewRect.X + viewRect.Width / 2f, viewRect.Y + viewRect.Height / 2f),
                    new Color(180, 160, 220), 0.85f, 0.5f, 0.5f);
            }

            // Footer ayuda
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(viewRect.X, viewRect.Bottom - 22, viewRect.Width, 22),
                new Color(8, 6, 16, 200));
            Utils.DrawBorderString(sb, "Click: memorizar/olvidar  |  Rueda: scroll",
                new Vector2(viewRect.X + viewRect.Width / 2f, viewRect.Bottom - 16),
                new Color(140, 130, 170), 0.7f, 0.5f, 0.5f);

            // Bloquear input del juego dentro de la vista
            if (viewRect.Contains(Main.mouseX, Main.mouseY))
            {
                Main.mouseLeft = false;
                Main.mouseRight = false;
            }
        }

        private void DrawAnimatedStars(SpriteBatch sb, Rectangle rect)
        {
            for (int i = 0; i < 20; i++)
            {
                int seed = i * 73856093;
                float bx = (seed % 1000) / 1000f * rect.Width;
                float by = ((seed * 19349663) % 1000) / 1000f * rect.Height;
                float sx = rect.X + bx;
                float sy = rect.Y + by;
                float twinkle = 0.4f + 0.6f * (float)Math.Sin(_time * 1.5f + i * 0.3f);
                int alpha = (int)(80 * twinkle);
                if (alpha < 0) alpha = 0; if (alpha > 255) alpha = 255;
                Color c = i % 3 == 0 ? new Color(245, 196, 81, alpha)
                        : i % 3 == 1 ? new Color(179, 136, 255, alpha)
                        : new Color(200, 220, 255, alpha);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)sx, (int)sy, 1, 1), c);
            }
        }
    }

    /// <summary>
    /// Estado del Codex de Memoria — UIState con DraggablePanel + CodexListView.
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
            _panel = new DraggablePanel(560, 460, "★ CODEX DE MEMORIA ★");
            _panel.OnCloseClick += () => Hide();

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
