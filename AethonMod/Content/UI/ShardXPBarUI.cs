using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// Tarjetas de eleccion de rama — modal a pantalla completa con fade-in.
    /// Sigue siendo directa (no UIState) porque solo aparece una vez.
    /// </summary>
    public class BranchChoiceUI
    {
        public bool IsVisible;
        private float _time = 0f;
        private float _appearProgress = 0f;
        private int _hoveredCard = -1;
        private bool _mouseLeftPressed = false;

        private struct CardInfo
        {
            public string Name;
            public string WeaponName;
            public string Desc;
            public Color Color;
            public BranchType Type;
        }
        private CardInfo[] _cards = new CardInfo[]
        {
            new()
            {
                Name = "DISTANCIA",
                WeaponName = "Lumina, la Arcoestelar",
                Desc = "Arcos, municion\ny armas arrojadizas.\n\nMaestria:\nCadencia y critico",
                Color = new(245, 196, 81),
                Type = BranchType.Distance,
            },
            new()
            {
                Name = "CUERPO A CUERPO",
                WeaponName = "Solbrand, Filo del Alba",
                Desc = "Espadas, lanzas\ny yoyos.\n\nMaestria:\nCombo y defensa",
                Color = new(255, 154, 60),
                Type = BranchType.Melee,
            },
            new()
            {
                Name = "ARTES MAGICAS",
                WeaponName = "Grimorio del Eterno",
                Desc = "Magia + Invocacion\nfusionadas.\n\nMaestria:\nMana y minions",
                Color = new(179, 136, 255),
                Type = BranchType.Magic,
            },
        };

        public void Update()
        {
            // No necesita update — todo en Draw.
        }

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

                // Estrellas de fondo
                DrawStars(sb);

                // Titulo
                Color titleColor = new(245, 196, 81, (int)(255 * _appearProgress));
                Utils.DrawBorderString(sb, "★ ELIGE LA RAMA DE TU FRAGMENTO GENESIS ★",
                    new Vector2(Main.screenWidth / 2f, Main.screenHeight * 0.15f),
                    titleColor, 1.2f, 0.5f, 0.5f);
                Utils.DrawBorderString(sb, "Tu decision es permanente — elige sabiamente",
                    new Vector2(Main.screenWidth / 2f, Main.screenHeight * 0.15f + 28),
                    new Color(180, 160, 220, (int)(255 * _appearProgress)), 0.85f, 0.5f, 0.5f);

                // 3 tarjetas centradas
                float cw = 220f, ch = 300f, gap = 24f;
                float totalW = cw * 3 + gap * 2;
                float startX = (Main.screenWidth - totalW) / 2f;
                float startY = Main.screenHeight * 0.25f;

                _hoveredCard = -1;
                for (int i = 0; i < 3; i++)
                {
                    float cardProgress = Math.Clamp((_appearProgress - i * 0.1f) * 1.5f, 0f, 1f);
                    if (cardProgress <= 0f) continue;

                    float cx = startX + i * (cw + gap);
                    var card = _cards[i];
                    bool hovered = Main.mouseX >= cx && Main.mouseX <= cx + cw &&
                                   Main.mouseY >= startY && Main.mouseY <= startY + ch;
                    if (hovered) _hoveredCard = i;

                    float hoverOffset = hovered ? -6f : 0f;
                    float cardAlpha = cardProgress;

                    // Glow si hovered
                    if (hovered)
                    {
                        float glowPulse = 0.7f + 0.3f * (float)Math.Sin(_time * 3);
                        for (int g = 4; g > 0; g--)
                        {
                            int gr = g * 5;
                            sb.Draw(TextureAssets.MagicPixel.Value,
                                new Rectangle((int)cx - gr, (int)(startY + hoverOffset) - gr,
                                    (int)cw + gr * 2, (int)ch + gr * 2),
                                new Color(card.Color.R, card.Color.G, card.Color.B, (int)(8 * glowPulse * cardAlpha)));
                        }
                    }

                    // Tarjeta
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

                    // Nombre
                    Utils.DrawBorderString(sb, card.Name,
                        new Vector2(cx + cw / 2f, startY + hoverOffset + 50),
                        new Color(card.Color.R, card.Color.G, card.Color.B, (int)(255 * cardAlpha)),
                        0.9f, 0.5f, 0.5f);

                    // Arma
                    Utils.DrawBorderString(sb, card.WeaponName,
                        new Vector2(cx + cw / 2f, startY + hoverOffset + 76),
                        new Color(220, 220, 230, (int)(255 * cardAlpha)),
                        0.75f, 0.5f, 0.5f);

                    // Separador
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)cx + 30, (int)(startY + hoverOffset + 100), (int)cw - 60, 1),
                        new Color(card.Color.R, card.Color.G, card.Color.B, (int)(100 * cardAlpha)));

                    // Desc
                    string[] lines = card.Desc.Split('\n');
                    for (int j = 0; j < lines.Length; j++)
                        Utils.DrawBorderString(sb, lines[j],
                            new Vector2(cx + cw / 2f, startY + hoverOffset + 120 + j * 18),
                            new Color(200, 200, 220, (int)(255 * cardAlpha)),
                            0.78f, 0.5f, 0.5f);

                    // Boton Elegir
                    float btnX = cx + (cw - 130) / 2f;
                    float btnY = startY + hoverOffset + ch - 48;
                    Color btnBg = hovered
                        ? new Color(card.Color.R, card.Color.G, card.Color.B, (int)(220 * cardAlpha))
                        : new Color(card.Color.R / 4, card.Color.G / 4, card.Color.B / 4, (int)(200 * cardAlpha));
                    sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)btnX, (int)btnY, 130, 32), btnBg);
                    Utils.DrawBorderString(sb, "ELEGIR",
                        new Vector2(btnX + 65, btnY + 10),
                        hovered ? Color.Black : new Color(card.Color.R, card.Color.G, card.Color.B, (int)(255 * cardAlpha)),
                        0.9f, 0.5f, 0.5f);

                    // Click — edge detection manual
                    bool clicked = hovered && Main.mouseLeft && !_mouseLeftPressed;
                    if (clicked)
                    {
                        _mouseLeftPressed = true;
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
                        for (int p = 0; p < 60; p++)
                            Dust.NewDustPerfect(Main.LocalPlayer.Center, Terraria.ID.DustID.GoldFlame,
                                new(Main.rand.NextFloat(-8, 8), Main.rand.NextFloat(-8, 8)),
                                100, card.Color, 2f);
                        ReplaceShardWithWeapon(Main.LocalPlayer, card.Type);
                        Main.mouseLeft = false;
                        Main.mouseRight = false;
                        Hide();
                        return;
                    }
                }

                if (!Main.mouseLeft) _mouseLeftPressed = false;
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

        private void DrawStars(SpriteBatch sb)
        {
            for (int i = 0; i < 80; i++)
            {
                int seed = i * 73856093;
                float bx = (seed % 1000) / 1000f * Main.screenWidth;
                float by = ((seed * 19349663) % 1000) / 1000f * Main.screenHeight;
                float twinkle = 0.5f + 0.5f * (float)Math.Sin(_time * 2 + i * 0.5f);
                int alpha = (int)(120 * twinkle * _appearProgress);
                if (alpha < 0) alpha = 0; if (alpha > 255) alpha = 255;
                int sz = 1 + (seed % 2);
                Color c = i % 3 == 0 ? new Color(245, 196, 81, alpha)
                        : i % 3 == 1 ? new Color(179, 136, 255, alpha)
                        : new Color(200, 220, 255, alpha);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)bx, (int)by, sz, sz), c);
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
