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
    /// Codex de Memoria — rediseñado desde cero.
    /// Se dibuja DIRECTAMENTE con sb.Draw() (no UserInterface) para evitar
    /// los cuadros blancos. Mismo patron que SkillTreeUI y BranchChoiceUI.
    /// </summary>
    public class MemoryCodexUI
    {
        public bool IsVisible = false;
        private float _time = 0f;
        private int _scrollY = 0;
        private bool _mouseLeftPressed = false;

        private List<MemoryCodexSystem.CodexEntry> _entries = new();

        private const int WIN_W = 600;
        private const int WIN_H = 460;
        private const int TITLE_H = 40;

        private MemoryCodexSystem.CodexEntry? _hoveredEntry;

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
            if (!sp.CodexUnlocked)
            {
                Main.NewText("El Codex de Memoria no esta desbloqueado. Consigue un arma magica o de invocacion.",
                    new Color(180, 160, 220));
                return;
            }
            _entries = MemoryCodexSystem.GetCodexForBranch(sp.ActiveBranch);
            _scrollY = 0;
            IsVisible = true;
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuOpen);
        }

        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            _mouseLeftPressed = false;
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
        }

        public void Update()
        {
            if (!IsVisible) return;
            _time += 0.016f;

            var winRect = WindowRect;
            bool mouseInWindow = winRect.Contains(Main.mouseX, Main.mouseY);

            // === SCROLL CON RUEDA ===
            int curScroll = Terraria.GameInput.PlayerInput.ScrollWheelValue;
            int scrollDelta = curScroll - Terraria.GameInput.PlayerInput.ScrollWheelValueOld;
            if (scrollDelta != 0 && mouseInWindow)
            {
                _scrollY -= Math.Sign(scrollDelta) * 30;
                int totalH = _entries.Count * 42;
                int contentH = winRect.Height - TITLE_H - 60;
                int maxScroll = Math.Max(0, totalH - contentH);
                _scrollY = Math.Max(0, Math.Min(_scrollY, maxScroll));
            }

            // === BOTON CERRAR ===
            Rectangle closeRect = new Rectangle(winRect.Right - 36, winRect.Y + 4, 32, 32);
            if (closeRect.Contains(Main.mouseX, Main.mouseY) && Main.mouseLeft && Main.mouseLeftRelease && !_mouseLeftPressed)
            {
                _mouseLeftPressed = true;
                Hide();
                return;
            }

            // === PROCESAR CLICK EN BOTONES MEMORIZAR/OLVIDAR ===
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp != null && Main.mouseLeft && Main.mouseLeftRelease && !_mouseLeftPressed)
            {
                _hoveredEntry = FindHoveredButton(winRect);
                if (_hoveredEntry != null)
                {
                    _mouseLeftPressed = true;
                    bool memorized = sp.MemorizedRunes.Contains(_hoveredEntry.Name);
                    if (!memorized)
                    {
                        if (sp.ResonanceShards >= _hoveredEntry.ResonanceCost && sp.MemorizedRunes.Count < sp.RuneSlots())
                        {
                            MemoryCodexSystem.Memorize(Main.LocalPlayer, _hoveredEntry);
                            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
                        }
                        else if (sp.ResonanceShards < _hoveredEntry.ResonanceCost)
                        {
                            Main.NewText($"Necesitas {_hoveredEntry.ResonanceCost} resonancia, tienes {sp.ResonanceShards}.", new Color(255, 100, 100));
                        }
                        else
                        {
                            Main.NewText($"Slots de runa llenos. Max: {sp.RuneSlots()}.", new Color(255, 100, 100));
                        }
                    }
                    else
                    {
                        MemoryCodexSystem.Forget(Main.LocalPlayer, _hoveredEntry.Name);
                        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
                    }
                }
            }
            if (!Main.mouseLeft) _mouseLeftPressed = false;

            // === BLOQUEAR INTERACCION CON EL JUEGO ===
            if (mouseInWindow)
            {
                Main.mouseLeft = false;
                Main.mouseRight = false;
                Main.mouseLeftRelease = false;
                Main.mouseRightRelease = false;
                Terraria.GameInput.PlayerInput.ScrollWheelValue = Terraria.GameInput.PlayerInput.ScrollWheelValueOld;
            }
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
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(winRect.X - 4, winRect.Y - 4, winRect.Width + 8, winRect.Height + 8),
                new Color(0, 0, 0, 100));
            sb.Draw(TextureAssets.MagicPixel.Value, winRect, new Color(15, 10, 25, 240));

            // Estrellas
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

            Utils.DrawBorderString(sb, "★ CODEX DE MEMORIA ★",
                new Vector2(titleRect.X + 14, titleRect.Y + 10), new Color(245, 196, 81), 0.95f);

            // Stats
            int slots = sp.RuneSlots();
            string statsText = $"Runas: {sp.MemorizedRunes.Count}/{slots}  |  Resonancia: {sp.ResonanceShards} ✦  |  Armas: {_entries.Count}";
            Utils.DrawBorderString(sb, statsText,
                new Vector2(titleRect.Right - 280, titleRect.Y + 10),
                new Color(179, 136, 255), 0.75f);

            // === BOTON CERRAR ===
            Rectangle closeRect = new Rectangle(winRect.Right - 36, winRect.Y + 4, 32, 32);
            bool closeHover = closeRect.Contains(Main.mouseX, Main.mouseY);
            sb.Draw(TextureAssets.MagicPixel.Value, closeRect, closeHover ? new Color(220, 80, 80, 230) : new Color(40, 20, 30, 180));
            DrawX(sb, closeRect, closeHover ? Color.White : new Color(220, 180, 180), 2f);

            // === LISTA DE ENTRADAS ===
            Rectangle listRect = new Rectangle(winRect.X + b, winRect.Y + TITLE_H + b, winRect.Width - b * 2, winRect.Height - TITLE_H - b * 2 - 24);
            int entryH = 42;
            int listTop = listRect.Y - _scrollY;
            int listBottom = listRect.Bottom;

            _hoveredEntry = null;
            for (int i = 0; i < _entries.Count; i++)
            {
                int ey = listTop + i * entryH;
                if (ey + entryH < listRect.Y || ey > listBottom) continue;

                var entry = _entries[i];
                bool memorized = sp.MemorizedRunes.Contains(entry.Name);
                int rowX = listRect.X + 4;
                int rowW = listRect.Width - 8;
                bool hovered = Main.mouseX >= rowX && Main.mouseX <= rowX + rowW &&
                               Main.mouseY >= ey && Main.mouseY <= ey + entryH - 4;

                // Fila
                Color rowColor;
                if (memorized) rowColor = new Color(245, 196, 81, 25);
                else if (hovered) rowColor = new Color(179, 136, 255, 30);
                else rowColor = new Color(20, 15, 35, 80) * (i % 2 == 0 ? 1f : 0.6f);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rowX, ey, rowW, entryH - 4), rowColor);

                // Indicador izquierdo
                Color indicator = memorized ? new Color(120, 220, 100, 200) : hovered ? new Color(245, 196, 81, 200) : new Color(80, 60, 120, 120);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(rowX, ey, 3, entryH - 4), indicator);

                // Icono
                string icon = sp.ActiveBranch == BranchType.Distance ? "→" : sp.ActiveBranch == BranchType.Melee ? "†" : "✦";
                Utils.DrawBorderString(sb, icon, new Vector2(rowX + 14, ey + 8), memorized ? new Color(245, 196, 81) : Color.White, 0.9f);

                // Nombre
                string displayName = entry.Name.Length > 22 ? entry.Name.Substring(0, 20) + "…" : entry.Name;
                Utils.DrawBorderString(sb, displayName, new Vector2(rowX + 32, ey + 6), memorized ? new Color(245, 196, 81) : Color.White, 0.8f);

                // Descripcion
                string desc = entry.Signature;
                if (desc.Length > 30) desc = desc.Substring(0, 28) + "…";
                Utils.DrawBorderString(sb, desc, new Vector2(rowX + 32, ey + 22), new Color(160, 150, 180), 0.65f);

                // Costo
                string costText = $"{entry.ResonanceCost} ✦";
                Utils.DrawBorderString(sb, costText, new Vector2(rowX + rowW - 110, ey + 14), new Color(245, 196, 81), 0.75f);

                // Boton Memorizar/Olvidar
                int btnW = 80, btnH = 24;
                int btnX = rowX + rowW - btnW - 4;
                int btnY = ey + 6;
                bool hoverBtn = Main.mouseX >= btnX && Main.mouseX <= btnX + btnW &&
                                Main.mouseY >= btnY && Main.mouseY <= btnY + btnH;
                if (hoverBtn) _hoveredEntry = entry;

                Color btnColor = memorized
                    ? (hoverBtn ? new Color(220, 80, 80, 230) : new Color(100, 40, 40, 200))
                    : (hoverBtn ? new Color(245, 196, 81, 230) : new Color(50, 35, 70, 180));
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(btnX, btnY, btnW, btnH), btnColor);
                Color btnBorder = memorized ? new Color(220, 80, 80, 150) : new Color(245, 196, 81, 100);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(btnX, btnY, btnW, 1), btnBorder);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(btnX, btnY + btnH - 1, btnW, 1), btnBorder);
                Utils.DrawBorderString(sb, memorized ? "Olvidar" : "Memorizar",
                    new Vector2(btnX + btnW / 2f, btnY + btnH / 2f - 8),
                    memorized ? (hoverBtn ? Color.White : new Color(220, 180, 180)) : (hoverBtn ? Color.Black : new Color(220, 220, 240)),
                    0.7f, 0.5f, 0.5f);
            }

            // Scrollbar
            int totalH = _entries.Count * entryH;
            int contentH = listBottom - listRect.Y;
            if (totalH > contentH)
            {
                int barH = Math.Max(30, contentH * contentH / totalH);
                int barY = listRect.Y + (_scrollY * (contentH - barH) / (totalH - contentH));
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(listRect.Right - 8, listRect.Y, 6, contentH), new Color(20, 15, 35, 180));
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(listRect.Right - 8, barY, 6, barH), new Color(179, 136, 255, 200));
            }

            // Empty state
            if (_entries.Count == 0)
            {
                Utils.DrawBorderString(sb, "No hay armas memorizables.",
                    new Vector2(listRect.X + listRect.Width / 2f, listRect.Y + listRect.Height / 2f),
                    new Color(180, 160, 220), 0.85f, 0.5f, 0.5f);
            }

            // Footer
            Rectangle footerRect = new Rectangle(winRect.X + b, winRect.Bottom - b - 22, winRect.Width - b * 2, 22);
            sb.Draw(TextureAssets.MagicPixel.Value, footerRect, new Color(8, 6, 16, 200));
            Utils.DrawBorderString(sb, "Click: memorizar/olvidar  |  Rueda: scroll  |  J/Esc: cerrar",
                new Vector2(footerRect.X + footerRect.Width / 2f, footerRect.Y + 6),
                new Color(140, 130, 170), 0.7f, 0.5f, 0f);

            // Cerrar con J o Esc
            var kb = Main.keyState;
            var oldKb = Main.oldKeyState;
            if (kb.IsKeyDown(Keys.Escape) && !oldKb.IsKeyDown(Keys.Escape)) Hide();
            var config = ModContent.GetInstance<Content.AethonConfig>();
            if (config != null && kb.IsKeyDown(config.CodexKey) && !oldKb.IsKeyDown(config.CodexKey)) Hide();
        }

        private MemoryCodexSystem.CodexEntry? FindHoveredButton(Rectangle winRect)
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return null;

            int entryH = 42;
            int listTop = winRect.Y + TITLE_H + 4 - _scrollY;
            int listBottom = winRect.Bottom - 24;
            int rowX = winRect.X + 6;
            int rowW = winRect.Width - 12;

            for (int i = 0; i < _entries.Count; i++)
            {
                int ey = listTop + i * entryH;
                if (ey + entryH < winRect.Y + TITLE_H || ey > listBottom) continue;

                int btnW = 80, btnH = 24;
                int btnX = rowX + rowW - btnW - 4;
                int btnY = ey + 6;
                if (Main.mouseX >= btnX && Main.mouseX <= btnX + btnW &&
                    Main.mouseY >= btnY && Main.mouseY <= btnY + btnH)
                {
                    return _entries[i];
                }
            }
            return null;
        }

        private void DrawStars(SpriteBatch sb, Rectangle rect)
        {
            for (int i = 0; i < 30; i++)
            {
                int seed = i * 73856093;
                float bx = (seed % 1000) / 1000f * rect.Width;
                float by = ((seed * 19349663) % 1000) / 1000f * rect.Height;
                float sx = rect.X + bx;
                float sy = rect.Y + by;
                float twinkle = 0.5f + 0.5f * (float)Math.Sin(_time * 2 + i * 0.5f);
                int alpha = (int)(80 * twinkle);
                if (alpha < 0) alpha = 0; if (alpha > 255) alpha = 255;
                Color c = i % 3 == 0 ? new Color(245, 196, 81, alpha) : i % 3 == 1 ? new Color(179, 136, 255, alpha) : new Color(200, 220, 255, alpha);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)sx, (int)sy, 1, 1), c);
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
