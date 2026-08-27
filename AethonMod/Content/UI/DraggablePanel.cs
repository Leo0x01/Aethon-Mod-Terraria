using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.ModLoader;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// Panel flotante arrastrable — base para el arbol de habilidades y el codex.
    /// El usuario puede agarrar la barra de titulo y mover la tarjeta por la pantalla.
    /// Usa el patron correcto de tModLoader (UIElement) para que el renderizado funcione.
    /// </summary>
    public class DraggablePanel : UIElement
    {
        public string Title = "";
        public Color TitleColor = new(245, 196, 81);
        public Color BorderColor = new(179, 136, 255);
        public Color HeaderColor = new(20, 15, 40, 240);
        public Color BodyColor = new(10, 8, 20, 245);

        private bool _isDragging = false;
        private Vector2 _dragOffset = Vector2.Zero;
        private float _time = 0f;

        // Boton cerrar
        public UIText? CloseButton;
        private bool _closeHovered = false;

        public event UIElement.MouseEvent? OnCloseClick;

        public DraggablePanel(int width, int height, string title)
        {
            Title = title;
            Width.Set(width, 0f);
            Height.Set(height, 0f);
            HAlign = 0.5f;
            VAlign = 0.5f;

            // Boton cerrar
            CloseButton = new UIText("X", 1.0f)
            {
                HAlign = 1f,
                VAlign = 0f,
                Left = { Pixels = -30 },
                Top = { Pixels = 6 },
            };
            CloseButton.OnMouseOver += (evt, el) => { _closeHovered = true; };
            CloseButton.OnMouseOut += (evt, el) => { _closeHovered = false; };
            CloseButton.OnLeftClick += (evt, el) => { OnCloseClick?.Invoke(evt, el); };
            Append(CloseButton);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _time += 0.016f;

            // Drag de la barra de titulo (zona superior, 0-40px)
            var dims = GetDimensions();
            Rectangle titleBar = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, 40);

            if (Main.mouseLeft && titleBar.Contains(Main.mouseX, Main.mouseY) && !_isDragging)
            {
                // No iniciar drag si el click fue en el boton cerrar
                Rectangle closeBtn = new Rectangle((int)dims.X + (int)dims.Width - 40, (int)dims.Y + 2, 36, 36);
                if (!closeBtn.Contains(Main.mouseX, Main.mouseY))
                {
                    _isDragging = true;
                    _dragOffset = new Vector2(Main.mouseX - dims.X, Main.mouseY - dims.Y);
                }
            }

            if (_isDragging)
            {
                float newX = Main.mouseX - _dragOffset.X;
                float newY = Main.mouseY - _dragOffset.Y;
                // Clamp para mantener la tarjeta dentro de la pantalla
                newX = MathHelper.Clamp(newX, -dims.Width + 100, Main.screenWidth - 100);
                newY = MathHelper.Clamp(newY, 0, Main.screenHeight - 50);
                Left.Set(newX, 0f);
                Top.Set(newY, 0f);
                Recalculate();
            }

            if (!Main.mouseLeft) _isDragging = false;
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            var dims = GetDimensions();
            Rectangle panelRect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

            // === FONDO COSMICO (estrellas + radiales) ===
            // Sombra
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(panelRect.X - 4, panelRect.Y - 4, panelRect.Width + 8, panelRect.Height + 8),
                new Color(0, 0, 0, 120));

            // Fondo base
            sb.Draw(TextureAssets.MagicPixel.Value, panelRect, BodyColor);

            // Radiales cosmicos dentro del panel
            DrawRadial(sb, panelRect, 0.3f, 0.2f, 0.5f, new Color(120, 80, 200, 18));
            DrawRadial(sb, panelRect, 0.7f, 0.8f, 0.4f, new Color(245, 196, 81, 14));

            // Estrellas de fondo (efecto cosmico)
            DrawStars(sb, panelRect);

            // === BORDE DOBLE (violeta externo, dorado interno) ===
            int bOut = 3;
            // Externo
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, bOut), BorderColor);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(panelRect.X, panelRect.Bottom - bOut, panelRect.Width, bOut), BorderColor);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(panelRect.X, panelRect.Y, bOut, panelRect.Height), BorderColor);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(panelRect.Right - bOut, panelRect.Y, bOut, panelRect.Height), BorderColor);

            // Interno (dorado, pulsante)
            float pulse = 0.7f + 0.3f * (float)System.Math.Sin(_time * 2);
            Color innerColor = new Color(245, 196, 81, (int)(120 * pulse));
            int bIn = 1;
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(panelRect.X + bOut, panelRect.Y + bOut, panelRect.Width - bOut * 2, bIn), innerColor);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(panelRect.X + bOut, panelRect.Bottom - bOut - bIn, panelRect.Width - bOut * 2, bIn), innerColor);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(panelRect.X + bOut, panelRect.Y + bOut, bIn, panelRect.Height - bOut * 2), innerColor);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(panelRect.Right - bOut - bIn, panelRect.Y + bOut, bIn, panelRect.Height - bOut * 2), innerColor);

            // === BARRA DE TITULO ===
            Rectangle titleBar = new Rectangle(panelRect.X + bOut, panelRect.Y + bOut, panelRect.Width - bOut * 2, 36);
            sb.Draw(TextureAssets.MagicPixel.Value, titleBar, HeaderColor);

            // Linea dorada inferior de la barra de titulo
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(titleBar.X, titleBar.Bottom, titleBar.Width, 2),
                new Color(245, 196, 81, 150));

            // Texto del titulo (con glow)
            Vector2 titlePos = new Vector2(titleBar.X + 14, titleBar.Y + 8);
            // Glow
            for (int i = 0; i < 3; i++)
            {
                Utils.DrawBorderString(sb, Title, titlePos + new Vector2(i, 0),
                    new Color(TitleColor.R, TitleColor.G, TitleColor.B, 30), 1.0f);
                Utils.DrawBorderString(sb, Title, titlePos + new Vector2(-i, 0),
                    new Color(TitleColor.R, TitleColor.G, TitleColor.B, 30), 1.0f);
            }
            Utils.DrawBorderString(sb, Title, titlePos, TitleColor, 1.0f);

            // Boton cerrar con hover
            Rectangle closeRect = new Rectangle(panelRect.Right - 36, panelRect.Y + 4, 32, 32);
            Color closeBg = _closeHovered ? new Color(220, 80, 80, 230) : new Color(40, 20, 30, 180);
            sb.Draw(TextureAssets.MagicPixel.Value, closeRect, closeBg);
            Utils.DrawBorderString(sb, "X",
                new Vector2(closeRect.X + closeRect.Width / 2f, closeRect.Y + closeRect.Height / 2f - 8),
                _closeHovered ? Color.White : new Color(220, 180, 180), 1.0f, 0.5f, 0.5f);

            // Indicador de arrastre (si esta arrastrando)
            if (_isDragging)
            {
                Utils.DrawBorderString(sb, "⋯", new Vector2(panelRect.X + 4, panelRect.Y + 40),
                    new Color(245, 196, 81, 100), 0.7f);
            }
        }

        private void DrawStars(SpriteBatch sb, Rectangle rect)
        {
            // Estrellas pre-generadas con seed fijo (parallax sutil)
            for (int i = 0; i < 60; i++)
            {
                int seed = i * 73856093;
                float bx = (seed % 1000) / 1000f * rect.Width;
                float by = ((seed * 19349663) % 1000) / 1000f * rect.Height;
                float sx = rect.X + bx;
                float sy = rect.Y + by;

                float twinkle = 0.5f + 0.5f * (float)System.Math.Sin(_time * 2 + i * 0.5f);
                int alpha = (int)(180 * twinkle * 0.6f);
                if (alpha < 0) alpha = 0; if (alpha > 255) alpha = 255;

                int sz = 1 + (seed % 2);
                Color starColor = i % 3 == 0 ? new Color(245, 196, 81, alpha)
                               : i % 3 == 1 ? new Color(179, 136, 255, alpha)
                               : new Color(200, 220, 255, alpha);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)sx, (int)sy, sz, sz), starColor);
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

        public bool IsDragging => _isDragging;
    }
}
