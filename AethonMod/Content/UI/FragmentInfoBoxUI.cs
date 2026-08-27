using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Players;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// Caja de informacion del Fragmento Genesis — panel fijo (no arrastrable)
    /// que muestra el estado del fragmento: icono, nivel, barra de XP, puntos de habilidad.
    ///
    /// Posicion: esquina superior izquierda, debajo de los buffs (no interfiere con el minimapa).
    /// La barra de XP vive SOLO dentro de esta caja (no flotando en otro lado).
    /// </summary>
    public class FragmentInfoBoxUI : UIElement
    {
        private float _time = 0f;
        private int _lastScrollValue = 0;
        public bool IsVisible = false;

        public FragmentInfoBoxUI()
        {
            // Tamaño de la caja
            int boxW = 240;
            int boxH = 90;
            Width.Set(boxW, 0f);
            Height.Set(boxH, 0f);
            // Posicion: esquina superior izquierda, debajo de los buffs
            Left.Set(20f, 0f);
            Top.Set(80f, 0f);
        }

        public void UpdateVisibility()
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            IsVisible = sp != null && sp.IsImprinted;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _time += 0.016f;
            UpdateVisibility();
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            if (!IsVisible) return;
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            var dims = GetDimensions();
            Rectangle boxRect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

            // Sombra
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(boxRect.X - 2, boxRect.Y - 2, boxRect.Width + 4, boxRect.Height + 4),
                new Color(0, 0, 0, 100));

            // Fondo (oscuro cosmico)
            sb.Draw(TextureAssets.MagicPixel.Value, boxRect, new Color(15, 10, 30, 235));

            // Radial cosmico sutil
            DrawRadial(sb, boxRect, 0.2f, 0.1f, 0.4f, new Color(120, 80, 200, 10));

            // Borde dorado
            int b = 2;
            Color border = new(245, 196, 81, 200);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.X, boxRect.Y, boxRect.Width, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.X, boxRect.Bottom - b, boxRect.Width, b), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.X, boxRect.Y, b, boxRect.Height), border);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(boxRect.Right - b, boxRect.Y, b, boxRect.Height), border);

            // Icono de la rama (emoji-like dibujado con pixeles)
            int iconX = boxRect.X + 12;
            int iconY = boxRect.Y + 12;
            int iconSize = 40;
            DrawBranchIcon(sb, sp.ActiveBranch, iconX, iconY, iconSize);

            // Glow pulsante del icono
            float pulse = 0.7f + 0.3f * (float)System.Math.Sin(_time * 2);
            Color glowColor = sp.ActiveBranch == BranchType.Distance ? new Color(245, 196, 81, (int)(20 * pulse))
                          : sp.ActiveBranch == BranchType.Melee ? new Color(255, 154, 60, (int)(20 * pulse))
                          : new Color(179, 136, 255, (int)(20 * pulse));
            for (int i = 3; i > 0; i--)
            {
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(iconX - i * 2, iconY - i * 2, iconSize + i * 4, iconSize + i * 4),
                    glowColor);
            }

            // Texto del nivel
            string branchName = sp.ActiveBranch == BranchType.Distance ? "Distancia"
                              : sp.ActiveBranch == BranchType.Melee ? "C. a Cuerpo"
                              : sp.ActiveBranch == BranchType.Magic ? "Magica" : "?";
            Utils.DrawBorderString(sb, $"Fragmento Lv {sp.ShardLevel}",
                new Vector2(iconX + iconSize + 8, iconY + 2),
                new Color(245, 196, 81), 0.85f);
            Utils.DrawBorderString(sb, branchName,
                new Vector2(iconX + iconSize + 8, iconY + 18),
                new Color(179, 136, 255), 0.7f);

            // === BARRA DE XP (incrustada en la caja) ===
            int barX = boxRect.X + 12;
            int barY = boxRect.Y + boxRect.Height - 22;
            int barW = boxRect.Width - 24;
            int barH = 14;

            // Fondo de la barra
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(barX, barY, barW, barH), new Color(20, 15, 40, 220));

            // Relleno (dorado, escala con XP)
            int xpNeeded = sp.XPForNextLevel();
            float pct = xpNeeded > 0 ? (float)sp.ShardXP / xpNeeded : 0f;
            pct = System.Math.Clamp(pct, 0f, 1f);
            int fillW = (int)((barW - 4) * pct);
            if (fillW > 0)
            {
                // Glow del relleno
                for (int i = 2; i > 0; i--)
                {
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(barX + 2 - i, barY + 2 - i, fillW + i * 2, barH - 4 + i * 2),
                        new Color(245, 196, 81, 6));
                }
                // Relleno principal
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(barX + 2, barY + 2, fillW, barH - 4),
                    new Color(245, 196, 81, 230));
                // Brillo superior
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(barX + 2, barY + 2, fillW, (barH - 4) / 2),
                    new Color(255, 240, 200, 60));
            }

            // Borde de la barra
            Color barBorder = new(245, 196, 81, 160);
            int bb = 1;
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(barX, barY, barW, bb), barBorder);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(barX, barY + barH - bb, barW, bb), barBorder);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(barX, barY, bb, barH), barBorder);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(barX + barW - bb, barY, bb, barH), barBorder);

            // Texto de XP sobre la barra
            string xpText = $"{sp.ShardXP} / {xpNeeded} XP";
            Utils.DrawBorderString(sb, xpText,
                new Vector2(barX + barW / 2f, barY + 2),
                new Color(255, 255, 255, 220), 0.6f, 0.5f, 0f);

            // Indicador de puntos de habilidad disponibles (pulsa si hay puntos)
            int avail = sp.CumulativeSkillPoints() - sp.AllocatedNodes.Count;
            if (avail > 0)
            {
                float skillPulse = 0.7f + 0.3f * (float)System.Math.Sin(_time * 3);
                Color skillColor = new Color(120, 255, 150, (int)(255 * skillPulse));
                // Badge en la esquina superior derecha de la caja
                int badgeW = 50;
                int badgeH = 18;
                int badgeX = boxRect.Right - badgeW - 8;
                int badgeY = boxRect.Y + 6;
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(badgeX, badgeY, badgeW, badgeH),
                    new Color(40, 80, 50, 230));
                // Borde del badge
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(badgeX, badgeY, badgeW, 1), skillColor);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(badgeX, badgeY + badgeH - 1, badgeW, 1), skillColor);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(badgeX, badgeY, 1, badgeH), skillColor);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(badgeX + badgeW - 1, badgeY, 1, badgeH), skillColor);
                Utils.DrawBorderString(sb, $"{avail} pts",
                    new Vector2(badgeX + badgeW / 2f, badgeY + 4),
                    skillColor, 0.7f, 0.5f, 0f);
            }
        }

        private void DrawBranchIcon(SpriteBatch sb, BranchType branch, int x, int y, int size)
        {
            // Dibujar un icono simple segun la rama (con pixeles cosmicos)
            Color iconColor = branch == BranchType.Distance ? new Color(245, 196, 81)
                          : branch == BranchType.Melee ? new Color(255, 154, 60)
                          : branch == BranchType.Magic ? new Color(179, 136, 255)
                          : Color.White;

            // Fondo del icono (circulo)
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(x, y, size, size), new Color(30, 20, 50, 200));
            // Borde del icono
            int b = 2;
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, y, size, b), iconColor);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, y + size - b, size, b), iconColor);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, y, b, size), iconColor);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x + size - b, y, b, size), iconColor);

            // Simbolo segun rama
            int cx = x + size / 2;
            int cy = y + size / 2;
            if (branch == BranchType.Distance)
            {
                // Arco (arco cosifico): lineas doradas formando un arco
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 8, cy - 8, 2, 16), iconColor);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 8, cy - 8, 16, 2), iconColor);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx + 6, cy - 8, 2, 16), iconColor);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 8, cy + 6, 16, 2), iconColor);
                // Estrella central
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 2, cy - 2, 4, 4),
                    new Color(255, 240, 200));
            }
            else if (branch == BranchType.Melee)
            {
                // Espada: linea vertical + cruz
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 1, cy - 12, 2, 24), iconColor);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 6, cy - 4, 12, 2), iconColor);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 2, cy + 8, 4, 4), iconColor);
            }
            else if (branch == BranchType.Magic)
            {
                // Libro/runa: cuadrado con runa interior
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 8, cy - 8, 16, 16), iconColor);
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 6, cy - 6, 12, 12),
                    new Color(30, 20, 50));
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 4, cy - 4, 8, 2),
                    new Color(255, 240, 200));
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 4, cy, 8, 2),
                    new Color(255, 240, 200));
                sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(cx - 4, cy + 4, 8, 2),
                    new Color(255, 240, 200));
            }
        }

        private void DrawRadial(SpriteBatch sb, Rectangle rect, float xPct, float yPct, float rPct, Color color)
        {
            float cx = rect.X + rect.Width * xPct;
            float cy = rect.Y + rect.Height * yPct;
            float r = rect.Width * rPct;
            if (r <= 0) return;
            for (int i = (int)r; i > 0; i -= 10)
            {
                int alpha = (int)(color.A * (1f - (float)i / r) * 0.3f);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)(cx - i), (int)(cy - i), i * 2, i * 2),
                    new Color(color.R, color.G, color.B, alpha));
            }
        }
    }
}
