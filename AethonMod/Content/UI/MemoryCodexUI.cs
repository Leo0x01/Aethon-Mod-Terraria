using System;
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
    /// Codex de Memoria — pantalla completa con estetica cosmica.
    /// Inspirado en el Bestiario de Terraria: panel central, scrollbar, fondo de estrellas.
    /// </summary>
    public class MemoryCodexUIState
    {
        public bool IsVisible = false;
        private int _scrollY = 0;
        private bool _mouseLeftPressed = false;
        private int _lastScrollValue = 0;
        private float _time = 0f;

        // Estrellas de fondo (mismo sistema que el skill tree)
        private struct Star
        {
            public Vector2 Pos;
            public float Size;
            public float Twinkle;
            public float Phase;
            public int Layer;
            public Color Color;
        }
        private Star[]? _stars;

        // Hovered entry (para tooltip)
        private MemoryCodexSystem.CodexEntry? _hoveredEntry;

        public void Show()
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted)
            {
                Main.NewText("El Fragmento Genesis aun no tiene una rama.", new Color(180, 160, 220));
                return;
            }
            _scrollY = 0;
            _mouseLeftPressed = false;
            InitStars();
            IsVisible = true;
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuOpen);
        }

        public void Hide()
        {
            IsVisible = false;
            _mouseLeftPressed = false;
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
        }

        private void InitStars()
        {
            if (_stars != null) return;
            var rand = new Random(42);
            _stars = new Star[150];
            for (int i = 0; i < _stars.Length; i++)
            {
                int layer = rand.Next(3);
                _stars[i] = new Star
                {
                    Pos = new Vector2((float)rand.NextDouble(), (float)rand.NextDouble()),
                    Size = 1f + (float)rand.NextDouble() * (layer == 2 ? 2.5f : layer == 1 ? 1.8f : 1.2f),
                    Twinkle = 0.5f + (float)rand.NextDouble() * 2f,
                    Phase = (float)rand.NextDouble() * MathF.PI * 2f,
                    Layer = layer,
                    Color = layer == 2
                        ? new Color(245, 196, 81)
                        : layer == 1
                            ? new Color(179, 136, 255)
                            : new Color(200, 220, 255),
                };
            }
        }

        public void Draw()
        {
            if (!IsVisible) return;
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            _time += 0.016f;
            var sb = Main.spriteBatch;

            try
            {
                var entries = MemoryCodexSystem.GetCodexForBranch(sp.ActiveBranch) ?? new List<MemoryCodexSystem.CodexEntry>();
                int slots = sp.RuneSlots();

                DrawBackground(sb);
                DrawStars(sb);

                // Titulo y stats
                Utils.DrawBorderString(sb, "★ CODEX DE MEMORIA ★",
                    new Vector2(Main.screenWidth / 2f, 22), new Color(179, 136, 255), 1.3f, 0.5f, 0.5f);
                string branchStr = sp.ActiveBranch switch
                {
                    BranchType.Distance => "Distancia",
                    BranchType.Melee => "Cuerpo a Cuerpo",
                    BranchType.Magic => "Artes Magicas",
                    _ => "?",
                };
                Utils.DrawBorderString(sb, $"Rama: {branchStr}  |  Runas: {sp.MemorizedRunes.Count}/{slots}  |  Resonancia: {sp.ResonanceShards} ✦",
                    new Vector2(Main.screenWidth / 2f, 46), new Color(245, 196, 81), 0.9f, 0.5f, 0.5f);

                // Panel central
                int panelW = Math.Min(780, Main.screenWidth - 80);
                int panelH = Main.screenHeight - 140;
                int panelX = (Main.screenWidth - panelW) / 2;
                int panelY = 80;

                // Fondo del panel con gradiente y bordes
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(panelX, panelY, panelW, panelH),
                    new Color(15, 10, 30, 230));

                // Borde doble (violeta externo, dorado interno)
                Color borderOut = new(179, 136, 255, 200);
                Color borderIn = new(245, 196, 81, 120);
                int bOut = 3, bIn = 1;
                // Externo
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(panelX, panelY, panelW, bOut), borderOut);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(panelX, panelY + panelH - bOut, panelW, bOut), borderOut);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(panelX, panelY, bOut, panelH), borderOut);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(panelX + panelW - bOut, panelY, bOut, panelH), borderOut);
                // Interno
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(panelX + bOut, panelY + bOut, panelW - bOut * 2, bIn), borderIn);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(panelX + bOut, panelY + panelH - bOut - bIn, panelW - bOut * 2, bIn), borderIn);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(panelX + bOut, panelY + bOut, bIn, panelH - bOut * 2), borderIn);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(panelX + panelW - bOut - bIn, panelY + bOut, bIn, panelH - bOut * 2), borderIn);

                // Encabezado del panel
                int headerH = 32;
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(panelX + bOut, panelY + bOut, panelW - bOut * 2, headerH),
                    new Color(245, 196, 81, 25));
                Utils.DrawBorderString(sb, "ARMAS MEMORIZABLES",
                    new Vector2(panelX + panelW / 2f, panelY + bOut + headerH / 2f - 6),
                    new Color(245, 196, 81), 0.9f, 0.5f, 0.5f);

                // Boton cerrar (X)
                int closeX = panelX + panelW - 30;
                int closeY = panelY + 6;
                int closeSize = 22;
                bool hoverClose = Main.mouseX >= closeX && Main.mouseX <= closeX + closeSize &&
                                  Main.mouseY >= closeY && Main.mouseY <= closeY + closeSize;
                Color closeBg = hoverClose ? new Color(220, 80, 80, 230) : new Color(40, 20, 30, 180);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(closeX, closeY, closeSize, closeSize), closeBg);
                Utils.DrawBorderString(sb, "X", new Vector2(closeX + closeSize / 2f, closeY + closeSize / 2f - 2),
                    hoverClose ? Color.White : new Color(220, 180, 180), 1.0f, 0.5f, 0.5f);
                if (hoverClose && Main.mouseLeft && !_mouseLeftPressed)
                {
                    _mouseLeftPressed = true;
                    // Consumir el click ANTES de cerrar para evitar input leak.
                    Main.mouseLeft = false;
                    Main.mouseRight = false;
                    Hide();
                    return;
                }

                // Lista de entradas
                int entryH = 44;
                int listTop = panelY + bOut + headerH + 8;
                int listBottom = panelY + panelH - bOut - 8;
                int listH = listBottom - listTop;
                int startY = listTop - _scrollY;

                int visibleStart = Math.Max(0, _scrollY / entryH);
                int visibleEnd = Math.Min(entries.Count, visibleStart + listH / entryH + 2);

                _hoveredEntry = null;
                for (int i = visibleStart; i < visibleEnd; i++)
                {
                    if (i < 0 || i >= entries.Count) continue;
                    var entry = entries[i];
                    int ey = startY + i * entryH;
                    if (ey + entryH < listTop || ey > listBottom) continue;

                    bool memorized = sp.MemorizedRunes.Contains(entry.Name);
                    bool hovered = Main.mouseX >= panelX + 12 && Main.mouseX <= panelX + panelW - 30 &&
                                   Main.mouseY >= ey && Main.mouseY <= ey + entryH - 6;
                    bool clicked = hovered && Main.mouseLeft && !_mouseLeftPressed;
                    if (hovered) _hoveredEntry = entry;

                    // Fila
                    Color rowColor;
                    if (memorized) rowColor = new Color(245, 196, 81, 35);
                    else if (hovered) rowColor = new Color(179, 136, 255, 35);
                    else rowColor = new Color(20, 15, 40, 120) * (i % 2 == 0 ? 1f : 0.7f);
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(panelX + 12, ey, panelW - 42, entryH - 6), rowColor);

                    // Borde izquierdo (indicador de estado)
                    Color indicator = memorized ? new Color(120, 220, 100, 200)
                                   : hovered ? new Color(245, 196, 81, 200)
                                   : new Color(80, 60, 120, 150);
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(panelX + 12, ey, 3, entryH - 6), indicator);

                    // Icono segun rama
                    string icon = sp.ActiveBranch == BranchType.Distance ? "🏹"
                                : sp.ActiveBranch == BranchType.Melee ? "⚔" : "📖";
                    Utils.DrawBorderString(sb, icon, new Vector2(panelX + 30, ey + 12),
                        memorized ? new Color(245, 196, 81) : Color.White, 1.1f);

                    // Nombre + signature
                    Utils.DrawBorderString(sb, entry.Name, new Vector2(panelX + 58, ey + 8),
                        memorized ? new Color(245, 196, 81) : Color.White, 0.9f);
                    Utils.DrawBorderString(sb, entry.Signature, new Vector2(panelX + 58, ey + 26),
                        new Color(160, 150, 180), 0.75f);

                    // Costo
                    string costText = $"{entry.ResonanceCost} ✦";
                    var costSize = FontAssets.MouseText.Value.MeasureString(costText);
                    Utils.DrawBorderString(sb, costText,
                        new Vector2(panelX + panelW - 175, ey + 18),
                        new Color(245, 196, 81), 0.85f, 0f, 0.5f);

                    // Boton Memorizar / Olvidar
                    int btnW = 110, btnH = 28;
                    int btnX = panelX + panelW - 145;
                    int btnY = ey + 8;
                    bool hoverBtn = Main.mouseX >= btnX && Main.mouseX <= btnX + btnW &&
                                    Main.mouseY >= btnY && Main.mouseY <= btnY + btnH;
                    Color btnColor;
                    if (memorized) btnColor = hoverBtn ? new Color(220, 80, 80, 230) : new Color(120, 50, 50, 200);
                    else if (hovered) btnColor = hoverBtn ? new Color(245, 196, 81, 230) : new Color(179, 136, 255, 200);
                    else btnColor = new Color(60, 40, 80, 180);
                    sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(btnX, btnY, btnW, btnH), btnColor);
                    Utils.DrawBorderString(sb, memorized ? "Olvidar" : "Memorizar",
                        new Vector2(btnX + btnW / 2f, btnY + btnH / 2f - 2),
                        memorized ? (hoverBtn ? Color.White : new Color(220, 180, 180))
                                  : (hoverBtn ? Color.Black : new Color(220, 220, 240)),
                        0.8f, 0.5f, 0.5f);

                    // Click en boton
                    if (clicked && hoverBtn)
                    {
                        if (!memorized)
                        {
                            if (sp.ResonanceShards >= entry.ResonanceCost && sp.MemorizedRunes.Count < slots)
                            {
                                MemoryCodexSystem.Memorize(Main.LocalPlayer, entry);
                                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
                                // Particulas
                                for (int p = 0; p < 16; p++)
                                    Dust.NewDustPerfect(Main.LocalPlayer.Center, Terraria.ID.DustID.GoldFlame,
                                        new(Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4)),
                                        100, new Color(245, 196, 81), 1.2f);
                            }
                            else if (sp.ResonanceShards < entry.ResonanceCost)
                            {
                                Main.NewText($"No tienes suficiente resonancia. Necesitas {entry.ResonanceCost}, tienes {sp.ResonanceShards}.",
                                    new Color(255, 100, 100));
                                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
                            }
                            else
                            {
                                Main.NewText($"No hay slots de runa disponibles. Maximo: {slots}.", new Color(255, 100, 100));
                                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
                            }
                        }
                        else
                        {
                            MemoryCodexSystem.Forget(Main.LocalPlayer, entry.Name);
                            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
                        }
                        _mouseLeftPressed = true;
                    }
                }

                // Scrollbar
                int totalH = entries.Count * entryH;
                if (totalH > listH)
                {
                    int scrollBarTrack = listH;
                    int scrollBarH = Math.Max(30, scrollBarTrack * listH / totalH);
                    int scrollBarY = listTop + (_scrollY * (listH - scrollBarH) / (totalH - listH));
                    // Track
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(panelX + panelW - 22, listTop, 8, listH),
                        new Color(30, 20, 50, 180));
                    // Thumb
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(panelX + panelW - 22, scrollBarY, 8, scrollBarH),
                        new Color(179, 136, 255, 220));

                    // Scroll con rueda (usar PlayerInput.ScrollWheelValue para detectar el delta)
                    int curScroll = Terraria.GameInput.PlayerInput.ScrollWheelValue;
                    int scrollDelta = curScroll - _lastScrollValue;
                    _lastScrollValue = curScroll;
                    if (scrollDelta != 0)
                    {
                        _scrollY -= System.Math.Sign(scrollDelta) * 30;
                        _scrollY = Math.Max(0, Math.Min(_scrollY, totalH - listH));
                    }
                }

                // Empty state
                if (entries.Count == 0)
                {
                    Utils.DrawBorderString(sb, "No hay armas memorizables para esta rama.",
                        new Vector2(panelX + panelW / 2f, listTop + listH / 2f),
                        new Color(180, 160, 220), 0.9f, 0.5f, 0.5f);
                }

                // Footer ayuda
                Utils.DrawBorderString(sb, "Click: memorizar/olvidar  |  Rueda: scroll  |  J / Esc: cerrar",
                    new Vector2(Main.screenWidth / 2f, Main.screenHeight - 18),
                    new Color(140, 130, 170), 0.8f, 0.5f, 0.5f);

                // Consumir clicks del mouse para no atacar
                if (Main.mouseLeft && !_mouseLeftPressed)
                {
                    _mouseLeftPressed = true;
                }
                if (!Main.mouseLeft) _mouseLeftPressed = false;

                // Cerrar con Esc o J
                var kb = Main.keyState;
                var oldKb = Main.oldKeyState;
                if (kb.IsKeyDown(Keys.Escape) && !oldKb.IsKeyDown(Keys.Escape)) Hide();
                var config = ModContent.GetInstance<Content.AethonConfig>();
                if (config != null && kb.IsKeyDown(config.CodexKey) && !oldKb.IsKeyDown(config.CodexKey)) Hide();

                // Bloquear input del juego
                Main.mouseLeft = false;
                Main.mouseRight = false;
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<AethonMod>()?.Logger?.Error("MemoryCodexUI.Draw error", ex);
                IsVisible = false;
            }
        }

        private void DrawBackground(SpriteBatch sb)
        {
            // Fondo base
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                new Color(6, 4, 14, 248));

            // Radiales cosmicos
            DrawRadial(sb, 0.3f, 0.2f, 0.5f, new Color(120, 80, 200, 22));
            DrawRadial(sb, 0.7f, 0.7f, 0.45f, new Color(245, 196, 81, 18));
            DrawRadial(sb, 0.5f, 1.0f, 0.4f, new Color(179, 136, 255, 20));

            // Nebulosa pulsante
            float pulse = 0.85f + (float)Math.Sin(_time * 0.5) * 0.15f;
            DrawRadial(sb, 0.5f, 0.5f, 0.3f * pulse, new Color(80, 40, 160, 16));

            // Vignette
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
                float sx = s.Pos.X * Main.screenWidth;
                float sy = s.Pos.Y * Main.screenHeight;

                float twinkle = 0.6f + 0.4f * (float)Math.Sin(_time * s.Twinkle + s.Phase);
                int alpha = (int)(255f * twinkle * (s.Layer == 2 ? 0.95f : s.Layer == 1 ? 0.7f : 0.45f));
                if (alpha < 0) alpha = 0; if (alpha > 255) alpha = 255;
                Color c = new Color(s.Color.R, s.Color.G, s.Color.B, alpha);

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

        private void DrawRadial(SpriteBatch sb, float xPct, float yPct, float rPct, Color color)
        {
            float cx = Main.screenWidth * xPct;
            float cy = Main.screenHeight * yPct;
            float r = Main.screenWidth * rPct;
            if (r <= 0) return;
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
