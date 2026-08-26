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
    /// Códex de Memoria — pantalla completa estilo árbol.
    /// Se dibuja directamente con Draw().
    /// </summary>
    public class MemoryCodexUIState
    {
        public bool IsVisible = false;
        private int _scrollY = 0;
        private bool _mouseLeftPressed = false;

        public void Show()
        {
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted) return;
            _scrollY = 0;
            IsVisible = true;
        }

        public void Hide() { IsVisible = false; }

        public void Draw()
        {
            if (!IsVisible) return;
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            var sb = Main.spriteBatch;
            var entries = MemoryCodexSystem.GetCodexForBranch(sp.ActiveBranch);
            int slots = sp.RuneSlots();

            // Fondo oscuro
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                new Color(6, 4, 14, 245));

            // Radiales
            DrawRadial(sb, 0.5f, 0.3f, 0.4f, new Color(120, 80, 200, 20));
            DrawRadial(sb, 0.8f, 0.7f, 0.35f, new Color(245, 196, 81, 15));

            // Titulo
            Utils.DrawBorderString(sb, "CODEX DE MEMORIA",
                new Vector2(Main.screenWidth / 2f, 20), new Color(179, 136, 255), 1.3f, 0.5f, 0.5f);
            Utils.DrawBorderString(sb, $"Runas: {sp.MemorizedRunes.Count}/{slots}  |  Resonancia: {sp.ResonanceShards} ✦",
                new Vector2(Main.screenWidth / 2f, 45), new Color(245, 196, 81), 0.9f, 0.5f, 0.5f);

            // Panel central
            int panelX = Main.screenWidth / 2 - 350;
            int panelY = 70;
            int panelW = 700;
            int panelH = Main.screenHeight - 100;

            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(panelX, panelY, panelW, panelH),
                new Color(15, 10, 30, 230));

            // Borde
            Color border = new(179, 136, 255, 150);
            int b = 2;
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(panelX, panelY, panelW, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(panelX, panelY + panelH - b, panelW, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(panelX, panelY, b, panelH), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(panelX + panelW - b, panelY, b, panelH), border);

            // Lista de entradas
            int entryH = 36;
            int startY = panelY + 10 - _scrollY;
            int visibleStart = System.Math.Max(0, _scrollY / entryH);
            int visibleEnd = System.Math.Min(entries.Count, visibleStart + panelH / entryH + 2);

            for (int i = visibleStart; i < visibleEnd; i++)
            {
                var entry = entries[i];
                int ey = startY + i * entryH;
                if (ey < panelY - entryH || ey > panelY + panelH) continue;

                bool memorized = sp.MemorizedRunes.Contains(entry.Name);
                bool hovered = Main.mouseX >= panelX + 10 && Main.mouseX <= panelX + panelW - 10 &&
                               Main.mouseY >= ey && Main.mouseY <= ey + entryH - 4;
                bool clicked = hovered && Main.mouseLeft && !_mouseLeftPressed;

                // Fila
                Color rowColor = memorized ? new Color(245, 196, 81, 30) : (hovered ? new Color(179, 136, 255, 25) : new Color(20, 15, 40, 100));
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(panelX + 10, ey, panelW - 20, entryH - 4), rowColor);

                // Icono
                string icon = sp.ActiveBranch == BranchType.Distance ? "🏹" : sp.ActiveBranch == BranchType.Melee ? "⚔" : "📖";
                Utils.DrawBorderString(sb, icon, new Vector2(panelX + 25, ey + 10),
                    memorized ? new Color(245, 196, 81) : Color.White, 1.0f);

                // Nombre + descripción
                Utils.DrawBorderString(sb, entry.Name, new Vector2(panelX + 50, ey + 8),
                    memorized ? new Color(245, 196, 81) : Color.White, 0.85f);
                Utils.DrawBorderString(sb, entry.Signature, new Vector2(panelX + 50, ey + 22),
                    new Color(150, 140, 170), 0.7f);

                // Costo
                Utils.DrawBorderString(sb, $"{entry.ResonanceCost} ✦", new Vector2(panelX + panelW - 120, ey + 12),
                    new Color(245, 196, 81), 0.8f);

                // Botón
                Color btnColor = memorized ? new Color(60, 80, 60, 200) : (hovered ? new Color(179, 136, 255, 200) : new Color(60, 40, 80, 180));
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(panelX + panelW - 90, ey + 6, 70, 24), btnColor);
                Utils.DrawBorderString(sb, memorized ? "✓" : "Memorizar",
                    new Vector2(panelX + panelW - 55, ey + 14),
                    memorized ? new Color(100, 200, 100) : (hovered ? Color.White : new Color(179, 136, 255)),
                    0.75f, 0.5f, 0.5f);

                // Click
                if (clicked && !memorized)
                {
                    if (sp.ResonanceShards >= entry.ResonanceCost && sp.MemorizedRunes.Count < slots)
                    {
                        MemoryCodexSystem.Memorize(Main.LocalPlayer, entry);
                        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
                    }
                    else if (sp.ResonanceShards < entry.ResonanceCost)
                    {
                        Main.NewText("No tienes suficiente resonancia.", new Color(255, 100, 100));
                    }
                    else
                    {
                        Main.NewText("No hay slots de runa disponibles.", new Color(255, 100, 100));
                    }
                }
            }

            // Scroll
            int totalH = entries.Count * entryH;
            if (totalH > panelH - 20)
            {
                int scrollBarH = (panelH - 20) * (panelH - 20) / totalH;
                int scrollBarY = panelY + 10 + (_scrollY * (panelH - 20 - scrollBarH) / (totalH - panelH + 20));
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(panelX + panelW - 12, scrollBarY, 6, scrollBarH),
                    new Color(179, 136, 255, 150));
            }

            if (Main.mouseLeft) _mouseLeftPressed = true;
            else _mouseLeftPressed = false;

            // Cerrar con Esc o J
            if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape)) Hide();

            // Ayuda
            Utils.DrawBorderString(sb, "Click: memorizar  |  J/Esc: cerrar",
                new Vector2(Main.screenWidth / 2f, Main.screenHeight - 15),
                new Color(100, 95, 120), 0.8f, 0.5f, 0.5f);
        }

        private void DrawRadial(SpriteBatch sb, float xPct, float yPct, float rPct, Color color)
        {
            float cx = Main.screenWidth * xPct;
            float cy = Main.screenHeight * yPct;
            float r = Main.screenWidth * rPct;
            for (int i = (int)r; i > 0; i -= 15)
            {
                int alpha = (int)(color.A * (1f - (float)i / r) * 0.3f);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)(cx - i), (int)(cy - i), i * 2, i * 2),
                    new Color(color.R, color.G, color.B, alpha));
            }
        }
    }
}
