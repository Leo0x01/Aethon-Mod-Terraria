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
    /// Barra de XP del Fragmento Genesis.
    /// Se dibuja directamente en PostDrawInterface.
    /// </summary>
    public class ShardXPBarUI
    {
        public bool IsVisible;

        public void Update()
        {
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            IsVisible = sp != null && sp.IsImprinted;
        }

        public void Draw()
        {
            if (!IsVisible) return;
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            var sb = Main.spriteBatch;
            int barX = Main.screenWidth - 220;
            int barY = 80;
            int barW = 210;
            int barH = 22;

            // Fondo
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(barX, barY, barW, barH), new Color(20, 15, 40, 210));

            // Borde dorado
            int b = 2;
            Color border = new(245, 196, 81, 160);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(barX, barY, barW, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(barX, barY + barH - b, barW, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(barX, barY, b, barH), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(barX + barW - b, barY, b, barH), border);

            // Relleno (dorado, escala con XP)
            int xpNeeded = sp.XPForNextLevel();
            float pct = xpNeeded > 0 ? (float)sp.ShardXP / xpNeeded : 0f;
            pct = System.Math.Clamp(pct, 0f, 1f);
            int fillW = (int)((barW - 4) * pct);
            if (fillW > 0)
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(barX + 2, barY + 2, fillW, barH - 4), new Color(245, 196, 81, 220));

            // Textos
            Utils.DrawBorderString(sb, $"Fragmento Lv {sp.ShardLevel}", new(barX + barW / 2f, barY - 16), new Color(245, 196, 81), 0.85f, 0.5f, 0.5f);
            Utils.DrawBorderString(sb, $"{sp.ShardXP} / {xpNeeded} XP", new(barX + barW / 2f, barY + barH + 6), new Color(179, 136, 255), 0.75f, 0.5f, 0.5f);
        }
    }

    /// <summary>
    /// Tarjetas de eleccion de rama.
    /// Se dibuja directamente en PostDrawInterface.
    /// </summary>
    public class BranchChoiceUI
    {
        public bool IsVisible;

        private struct CardInfo { public string Name; public string Desc; public Color Color; public BranchType Type; }
        private CardInfo[] _cards = new CardInfo[]
        {
            new() { Name = "DISTANCIA", Desc = "Arcos, municion\ny armas arrojadizas\n\nLumina,\nla Arcoestelar", Color = new(245, 196, 81), Type = BranchType.Distance },
            new() { Name = "CUERPO A CUERPO", Desc = "Espadas, lanzas\ny yoyos\n\nSolbrand,\nFilo del Alba", Color = new(255, 154, 60), Type = BranchType.Melee },
            new() { Name = "ARTES MAGICAS", Desc = "Magia + Invocacion\nfusionadas\n\nGrimorio\ndel Eterno", Color = new(179, 136, 255), Type = BranchType.Magic },
        };

        public void Show() { IsVisible = true; }
        public void Hide() { IsVisible = false; }

        public void Draw()
        {
            if (!IsVisible) return;
            var sb = Main.spriteBatch;
            var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            // Fondo oscuro
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), new Color(6, 4, 14, 200));

            // Titulo
            Utils.DrawBorderString(sb, "Elige la rama de tu Fragmento Genesis",
                new(Main.screenWidth / 2f, Main.screenHeight * 0.15f),
                new Color(245, 196, 81), 1.2f, 0.5f, 0.5f);

            // 3 tarjetas centradas
            float cw = 220f, ch = 280f, gap = 20f;
            float totalW = cw * 3 + gap * 2;
            float startX = (Main.screenWidth - totalW) / 2f;
            float startY = Main.screenHeight * 0.3f;

            for (int i = 0; i < 3; i++)
            {
                float cx = startX + i * (cw + gap);
                var card = _cards[i];
                bool hovered = Main.mouseX >= cx && Main.mouseX <= cx + cw &&
                               Main.mouseY >= startY && Main.mouseY <= startY + ch;
                bool clicked = hovered && Main.mouseLeft && Main.mouseLeftRelease;

                // Tarjeta
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)cx, (int)startY, (int)cw, (int)ch),
                    new Color(20, 15, 40, 235));
                // Borde
                Color bc = hovered ? Color.White : card.Color;
                int b = 2;
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)cx, (int)startY, (int)cw, b), bc);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)cx, (int)(startY + ch - b), (int)cw, b), bc);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)cx, (int)startY, b, (int)ch), bc);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)(cx + cw - b), (int)startY, b, (int)ch), bc);

                // Nombre
                Utils.DrawBorderString(sb, card.Name, new(cx + cw / 2f, startY + 20), card.Color, 0.9f, 0.5f, 0.5f);
                // Desc
                string[] lines = card.Desc.Split('\n');
                for (int j = 0; j < lines.Length; j++)
                    Utils.DrawBorderString(sb, lines[j], new(cx + cw / 2f, startY + 55 + j * 18), new Color(200, 200, 220), 0.75f, 0.5f, 0.5f);

                // Boton Elegir
                float btnX = cx + (cw - 120) / 2f;
                float btnY = startY + ch - 45;
                Color btnBg = hovered ? new(card.Color.R, card.Color.G, card.Color.B, 180) : new(card.Color.R / 3, card.Color.G / 3, card.Color.B / 3, 200);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)btnX, (int)btnY, 120, 32), btnBg);
                Utils.DrawBorderString(sb, "Elegir", new(btnX + 60, btnY + 10), hovered ? Color.White : card.Color, 0.85f, 0.5f, 0.5f);

                // Click
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
                    for (int p = 0; p < 60; p++)
                        Dust.NewDustPerfect(Main.LocalPlayer.Center, Terraria.ID.DustID.GoldFlame, new(Main.rand.NextFloat(-8, 8), Main.rand.NextFloat(-8, 8)), 100, card.Color, 2f);
                    ReplaceShardWithWeapon(Main.LocalPlayer, card.Type);
                    Hide();
                }
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
                { int p = player.inventory[i].prefix; player.inventory[i].SetDefaults(weaponType); player.inventory[i].prefix = (byte)p; break; }
        }
    }
}
