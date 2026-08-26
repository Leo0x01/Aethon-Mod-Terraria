using System;
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
    /// Barra de XP del Fragmento Genesis — estilo cosmico con brillo dorado.
    /// Se dibuja en PostDrawInterface (siempre visible cuando el fragmento esta imprintado).
    /// </summary>
    public class ShardXPBarUI
    {
        public bool IsVisible;
        private float _time = 0f;

        public void Update()
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            IsVisible = sp != null && sp.IsImprinted;
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
                // Posicion: esquina superior derecha, debajo del icono de buff
                int barW = 220;
                int barH = 24;
                int barX = Main.screenWidth - barW - 20;
                int barY = 90;

                // Fondo con sombra
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(barX - 2, barY - 2, barW + 4, barH + 4),
                    new Color(0, 0, 0, 120));
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(barX, barY, barW, barH),
                    new Color(20, 15, 40, 220));

                // Relleno (dorado, escala con XP)
                int xpNeeded = sp.XPForNextLevel();
                float pct = xpNeeded > 0 ? (float)sp.ShardXP / xpNeeded : 0f;
                pct = Math.Clamp(pct, 0f, 1f);
                int fillW = (int)((barW - 4) * pct);
                if (fillW > 0)
                {
                    // Glow de fondo del relleno
                    for (int i = 3; i > 0; i--)
                    {
                        sb.Draw(TextureAssets.MagicPixel.Value,
                            new Rectangle(barX + 2 - i, barY + 2 - i, fillW + i * 2, barH - 4 + i * 2),
                            new Color(245, 196, 81, 4));
                    }
                    // Relleno principal
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(barX + 2, barY + 2, fillW, barH - 4),
                        new Color(245, 196, 81, 230));
                    // Brillo superior (gradiente)
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(barX + 2, barY + 2, fillW, (barH - 4) / 2),
                        new Color(255, 240, 200, 80));

                    // Brillo pulsante en el borde derecho del relleno (efecto de carga)
                    if (pct > 0.05f && pct < 0.99f)
                    {
                        int glowX = barX + 2 + fillW - 2;
                        int glowAlpha = (int)(120 + 100 * Math.Sin(_time * 4));
                        glowAlpha = Math.Clamp(glowAlpha, 60, 220);
                        sb.Draw(TextureAssets.MagicPixel.Value,
                            new Rectangle(glowX, barY + 2, 3, barH - 4),
                            new Color(255, 255, 220, glowAlpha));
                    }
                }

                // Borde dorado
                int b = 2;
                Color border = new(245, 196, 81, 180);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(barX, barY, barW, b), border);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(barX, barY + barH - b, barW, b), border);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(barX, barY, b, barH), border);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(barX + barW - b, barY, b, barH), border);

                // Texto del nivel (arriba)
                string branchIcon = sp.ActiveBranch == BranchType.Distance ? "🏹"
                                  : sp.ActiveBranch == BranchType.Melee ? "⚔"
                                  : sp.ActiveBranch == BranchType.Magic ? "📖" : "✦";
                Utils.DrawBorderString(sb, $"{branchIcon} Fragmento Lv {sp.ShardLevel}",
                    new Vector2(barX + barW / 2f, barY - 12),
                    new Color(245, 196, 81), 0.9f, 0.5f, 0.5f);

                // Texto de XP (abajo)
                Utils.DrawBorderString(sb, $"{sp.ShardXP} / {xpNeeded} XP",
                    new Vector2(barX + barW / 2f, barY + barH + 8),
                    new Color(179, 136, 255), 0.75f, 0.5f, 0.5f);

                // Texto de skill points disponibles (si hay)
                int avail = sp.CumulativeSkillPoints() - sp.AllocatedNodes.Count;
                if (avail > 0)
                {
                    // Pulso para llamar atencion
                    float pulse = 0.7f + 0.3f * (float)Math.Sin(_time * 3);
                    int alpha = (int)(255 * pulse);
                    string ptsText = $"★ {avail} pts disponibles (K)";
                    var textSize = FontAssets.MouseText.Value.MeasureString(ptsText);
                    int ptsY = barY + barH + 22;
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(barX + barW / 2 - (int)textSize.X / 2 - 6, ptsY - 2, (int)textSize.X + 12, 18),
                        new Color(120, 60, 30, 200));
                    Utils.DrawBorderString(sb, ptsText,
                        new Vector2(barX + barW / 2f, ptsY + 6),
                        new Color(255, (int)(220 * pulse), (int)(140 * pulse)), 0.8f, 0.5f, 0.5f);
                }
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<AethonMod>()?.Logger?.Error("ShardXPBarUI.Draw error", ex);
                IsVisible = false;
            }
        }
    }

    /// <summary>
    /// Tarjetas de eleccion de rama — estetica cosmica mejorada.
    /// Centradas en pantalla con animaciones y efectos.
    /// </summary>
    public class BranchChoiceUI
    {
        public bool IsVisible;
        private float _time = 0f;
        private float _appearProgress = 0f;
        private int _hoveredCard = -1;

        private struct CardInfo
        {
            public string Name;
            public string WeaponName;
            public string Desc;
            public string Icon;
            public Color Color;
            public BranchType Type;
        }
        private CardInfo[] _cards = new CardInfo[]
        {
            new()
            {
                Name = "DISTANCIA",
                WeaponName = "Lumina, la Arcoestelar",
                Desc = "Arcos, munición\ny armas arrojadizas.\n\nMaestria:\nCadencia y critical",
                Icon = "🏹",
                Color = new(245, 196, 81),
                Type = BranchType.Distance,
            },
            new()
            {
                Name = "CUERPO A CUERPO",
                WeaponName = "Solbrand, Filo del Alba",
                Desc = "Espadas, lanzas\ny yoyos.\n\nMaestria:\nCombo y defensa",
                Icon = "⚔",
                Color = new(255, 154, 60),
                Type = BranchType.Melee,
            },
            new()
            {
                Name = "ARTES MAGICAS",
                WeaponName = "Grimorio del Eterno",
                Desc = "Magia + Invocacion\nfusionadas.\n\nMaestria:\nManá y minions",
                Icon = "📖",
                Color = new(179, 136, 255),
                Type = BranchType.Magic,
            },
        };

        public void Show()
        {
            IsVisible = true;
            _appearProgress = 0f;
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item4);
        }

        public void Hide()
        {
            IsVisible = false;
            _appearProgress = 0f;
        }

        public void Draw()
        {
            if (!IsVisible) return;
            var sb = Main.spriteBatch;
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            _time += 0.016f;
            _appearProgress = Math.Min(1f, _appearProgress + 0.05f);

            try
            {
                // Fondo oscuro con fade-in
                int bgAlpha = (int)(200 * _appearProgress);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                    new Color(6, 4, 14, bgAlpha));

                // Radiales cosmicos
                DrawRadial(sb, 0.3f, 0.2f, 0.5f, new Color(120, 80, 200, 22));
                DrawRadial(sb, 0.7f, 0.7f, 0.45f, new Color(245, 196, 81, 18));

                // Titulo (aparece con fade-in)
                Color titleColor = new(245, 196, 81, (int)(255 * _appearProgress));
                Utils.DrawBorderString(sb, "★ ELIGE LA RAMA DE TU FRAGMENTO GENESIS ★",
                    new Vector2(Main.screenWidth / 2f, Main.screenHeight * 0.13f),
                    titleColor, 1.2f, 0.5f, 0.5f);
                Utils.DrawBorderString(sb, "Tu decision es permanente — elige sabiamente",
                    new Vector2(Main.screenWidth / 2f, Main.screenHeight * 0.13f + 28),
                    new Color(180, 160, 220, (int)(255 * _appearProgress)), 0.85f, 0.5f, 0.5f);

                // 3 tarjetas centradas
                float cw = 240f, ch = 320f, gap = 24f;
                float totalW = cw * 3 + gap * 2;
                float startX = (Main.screenWidth - totalW) / 2f;
                float startY = Main.screenHeight * 0.28f;

                _hoveredCard = -1;
                for (int i = 0; i < 3; i++)
                {
                    // Animacion de entrada escalonada (cada tarjeta aparece despues)
                    float cardProgress = Math.Clamp((_appearProgress - i * 0.1f) * 1.5f, 0f, 1f);
                    if (cardProgress <= 0f) continue;

                    float cx = startX + i * (cw + gap);
                    var card = _cards[i];
                    bool hovered = Main.mouseX >= cx && Main.mouseX <= cx + cw &&
                                   Main.mouseY >= startY && Main.mouseY <= startY + ch;
                    if (hovered) _hoveredCard = i;

                    // Offset para hover (levita)
                    float hoverOffset = hovered ? -8f : 0f;
                    float scale = cardProgress * (hovered ? 1.05f : 1f);
                    float cardAlpha = cardProgress;

                    // Glow si hovered
                    if (hovered)
                    {
                        float glowPulse = 0.7f + 0.3f * (float)Math.Sin(_time * 3);
                        for (int g = 4; g > 0; g--)
                        {
                            int gr = g * 6;
                            sb.Draw(TextureAssets.MagicPixel.Value,
                                new Rectangle((int)cx - gr, (int)(startY + hoverOffset) - gr,
                                    (int)cw + gr * 2, (int)ch + gr * 2),
                                new Color(card.Color.R, card.Color.G, card.Color.B, (int)(8 * glowPulse * cardAlpha)));
                        }
                    }

                    // Tarjeta (fondo)
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)cx, (int)(startY + hoverOffset), (int)cw, (int)ch),
                        new Color(20, 15, 40, (int)(235 * cardAlpha)));

                    // Borde
                    Color bc = hovered ? Color.White : card.Color;
                    int b = 2;
                    sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)cx, (int)(startY + hoverOffset), (int)cw, b), bc);
                    sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)cx, (int)(startY + hoverOffset + ch - b), (int)cw, b), bc);
                    sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)cx, (int)(startY + hoverOffset), b, (int)ch), bc);
                    sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)(cx + cw - b), (int)(startY + hoverOffset), b, (int)ch), bc);

                    // Banda superior con color de la rama
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)cx + b, (int)(startY + hoverOffset) + b, (int)cw - b * 2, 30),
                        new Color(card.Color.R, card.Color.G, card.Color.B, (int)(80 * cardAlpha)));

                    // Icono grande
                    float iconY = startY + hoverOffset + 50;
                    Utils.DrawBorderString(sb, card.Icon, new Vector2(cx + cw / 2f, iconY),
                        new Color(card.Color.R, card.Color.G, card.Color.B, (int)(255 * cardAlpha)),
                        1.6f, 0.5f, 0.5f);

                    // Nombre
                    Utils.DrawBorderString(sb, card.Name,
                        new Vector2(cx + cw / 2f, startY + hoverOffset + 100),
                        new Color(card.Color.R, card.Color.G, card.Color.B, (int)(255 * cardAlpha)),
                        0.95f, 0.5f, 0.5f);

                    // Arma
                    Utils.DrawBorderString(sb, card.WeaponName,
                        new Vector2(cx + cw / 2f, startY + hoverOffset + 122),
                        new Color(220, 220, 230, (int)(255 * cardAlpha)),
                        0.75f, 0.5f, 0.5f);

                    // Separador
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)cx + 30, (int)(startY + hoverOffset + 145), (int)cw - 60, 1),
                        new Color(card.Color.R, card.Color.G, card.Color.B, (int)(100 * cardAlpha)));

                    // Desc multilinea
                    string[] lines = card.Desc.Split('\n');
                    for (int j = 0; j < lines.Length; j++)
                        Utils.DrawBorderString(sb, lines[j],
                            new Vector2(cx + cw / 2f, startY + hoverOffset + 160 + j * 18),
                            new Color(200, 200, 220, (int)(255 * cardAlpha)),
                            0.8f, 0.5f, 0.5f);

                    // Boton Elegir
                    float btnX = cx + (cw - 140) / 2f;
                    float btnY = startY + hoverOffset + ch - 50;
                    Color btnBg = hovered
                        ? new Color(card.Color.R, card.Color.G, card.Color.B, (int)(220 * cardAlpha))
                        : new Color(card.Color.R / 4, card.Color.G / 4, card.Color.B / 4, (int)(200 * cardAlpha));
                    sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)btnX, (int)btnY, 140, 34), btnBg);
                    Utils.DrawBorderString(sb, "ELEGIR",
                        new Vector2(btnX + 70, btnY + 12),
                        hovered ? Color.Black : new Color(card.Color.R, card.Color.G, card.Color.B, (int)(255 * cardAlpha)),
                        0.95f, 0.5f, 0.5f);

                    // Click
                    bool clicked = hovered && Main.mouseLeft && Main.mouseLeftRelease;
                    if (clicked)
                    {
                        sp.ActiveBranch = card.Type;
                        sp.SubForm = card.Type switch
                        {
                            BranchType.Distance => WeaponSubForm.Bow,
                            BranchType.Melee => WeaponSubForm.Sword,
                            BranchType.Magic => WeaponSubForm.Spellbook,
                            _ => WeaponSubForm.None,
                        };
                        if (sp.SkillTreeSeed == 0) sp.SkillTreeSeed = Main.rand.Next(1, 1_000_000);
                        Main.NewText($"✦ El Fragmento Genesis se ha transformado — {card.Name}!", card.Color);
                        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item4);
                        Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item169);
                        for (int p = 0; p < 80; p++)
                            Dust.NewDustPerfect(Main.LocalPlayer.Center, Terraria.ID.DustID.GoldFlame,
                                new(Main.rand.NextFloat(-10, 10), Main.rand.NextFloat(-10, 10)),
                                100, card.Color, 2.2f);
                        ReplaceShardWithWeapon(Main.LocalPlayer, card.Type);
                        Hide();
                        return;
                    }
                }

                // Consumir clicks
                Main.mouseLeft = false;
                Main.mouseRight = false;
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<AethonMod>()?.Logger?.Error("BranchChoiceUI.Draw error", ex);
                IsVisible = false;
            }
        }

        private void ReplaceShardWithWeapon(Player player, BranchType branch)
        {
            int weaponType = branch switch
            {
                BranchType.Distance => ModContent.ItemType<Weapons.LuminaStarbow>(),
                BranchType.Melee => ModContent.ItemType<Weapons.SolbrandEdge>(),
                BranchType.Magic => ModContent.ItemType<Weapons.GrimoireEternal>(),
                _ => ModContent.ItemType<Items.GenesisShard>(),
            };
            for (int i = 0; i < 58; i++)
                if (player.inventory[i].type == ModContent.ItemType<Items.GenesisShard>())
                {
                    int p = player.inventory[i].prefix;
                    player.inventory[i].SetDefaults(weaponType);
                    player.inventory[i].prefix = (byte)p;
                    break;
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
