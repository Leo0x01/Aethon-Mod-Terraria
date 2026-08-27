using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using Terraria.ModLoader;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// Panel flotante arrastrable — base para el arbol de habilidades y el codex.
    /// Dibuja TODO manualmente (sin UIText para evitar superposiciones).
    /// Bloquea la interaccion con el juego mientras esta abierto.
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

        private bool _closeHovered = false;
        private bool _closeClicked = false;

        public event System.Action? OnCloseClick;

        public DraggablePanel(int width, int height, string title)
        {
            Title = title;
            Width.Set(width, 0f);
            Height.Set(height, 0f);
            HAlign = 0.5f;
            VAlign = 0.5f;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _time += 0.016f;

            var dims = GetDimensions();
            Rectangle titleBar = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, 40);

            // Drag de la barra de titulo
            bool mouseInTitle = titleBar.Contains(Main.mouseX, Main.mouseY);
            Rectangle closeRect = new Rectangle((int)dims.X + (int)dims.Width - 36, (int)dims.Y + 4, 32, 32);
            bool mouseInClose = closeRect.Contains(Main.mouseX, Main.mouseY);

            if (Main.mouseLeft && mouseInTitle && !mouseInClose && !_isDragging && Main.mouseLeftRelease)
            {
                _isDragging = true;
                _dragOffset = new Vector2(Main.mouseX - dims.X, Main.mouseY - dims.Y);
            }

            if (_isDragging)
            {
                float newX = Main.mouseX - _dragOffset.X;
                float newY = Main.mouseY - _dragOffset.Y;
                newX = MathHelper.Clamp(newX, -dims.Width + 100, Main.screenWidth - 100);
                newY = MathHelper.Clamp(newY, 0, Main.screenHeight - 50);
                Left.Set(newX, 0f);
                Top.Set(newY, 0f);
                Recalculate();
            }

            if (!Main.mouseLeft) _isDragging = false;

            // Hover del boton cerrar
            _closeHovered = mouseInClose;

            // Click del boton cerrar (edge detection)
            if (mouseInClose && Main.mouseLeft && Main.mouseLeftRelease && !_closeClicked)
            {
                _closeClicked = true;
                OnCloseClick?.Invoke();
            }
            if (!Main.mouseLeft) _closeClicked = false;

            // === BLOQUEAR INTERACCION CON EL JUEGO ===
            // Mientras el panel este abierto, consumir el input del mouse para que
            // el jugador no pueda atacar/moverse/usar items en el juego.
            if (dims.ToRectangle().Contains(Main.mouseX, Main.mouseY) || _isDragging)
            {
                Main.mouseLeft = false;
                Main.mouseRight = false;
                // Evitar que la rueda afecte el inventario (scroll del hotbar)
                Terraria.GameInput.PlayerInput.ScrollWheelValue = Terraria.GameInput.PlayerInput.ScrollWheelValueOld;
            }
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            var dims = GetDimensions();
            Rectangle panelRect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

            // === SOMBRA ===
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(panelRect.X - 4, panelRect.Y - 4, panelRect.Width + 8, panelRect.Height + 8),
                new Color(0, 0, 0, 100));

            // === FONDO DEL PANEL (oscuro cosmico) ===
            sb.Draw(TextureAssets.MagicPixel.Value, panelRect, BodyColor);

            // Radiales cosmicos sutiles
            DrawRadial(sb, panelRect, 0.3f, 0.2f, 0.5f, new Color(120, 80, 200, 12));
            DrawRadial(sb, panelRect, 0.7f, 0.8f, 0.4f, new Color(245, 196, 81, 10));

            // Estrellas de fondo
            DrawStars(sb, panelRect);

            // === BORDE DOBLE ===
            int bOut = 3;
            // Externo (violeta)
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
            Color innerColor = new Color(245, 196, 81, (int)(100 * pulse));
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
            // Linea dorada inferior
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(titleBar.X, titleBar.Bottom, titleBar.Width, 2),
                new Color(245, 196, 81, 150));

            // Texto del titulo (con glow sutil)
            Vector2 titlePos = new Vector2(titleBar.X + 14, titleBar.Y + 8);
            Utils.DrawBorderString(sb, Title, titlePos, TitleColor, 0.95f);

            // === BOTON CERRAR (X) ===
            Rectangle closeRect = new Rectangle(panelRect.Right - 36, panelRect.Y + 4, 32, 32);
            // Fondo del boton
            Color closeBg = _closeHovered ? new Color(220, 80, 80, 230) : new Color(40, 20, 30, 180);
            sb.Draw(TextureAssets.MagicPixel.Value, closeRect, closeBg);
            // Borde del boton
            Color closeBorder = _closeHovered ? Color.White : new Color(180, 80, 80, 150);
            int cb = 1;
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(closeRect.X, closeRect.Y, closeRect.Width, cb), closeBorder);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(closeRect.X, closeRect.Bottom - cb, closeRect.Width, cb), closeBorder);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(closeRect.X, closeRect.Y, cb, closeRect.Height), closeBorder);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(closeRect.Right - cb, closeRect.Y, cb, closeRect.Height), closeBorder);
            // X dibujada con lineas (no texto, para evitar superposicion)
            DrawX(sb, closeRect, _closeHovered ? Color.White : new Color(220, 180, 180), 2f);
        }

        /// <summary>Dibuja una X con lineas dentro de un rectangulo.</summary>
        private void DrawX(SpriteBatch sb, Rectangle rect, Color color, float thickness)
        {
            int pad = 8;
            // Linea diagonal 1 (esquina sup-izq a inf-der)
            DrawLine(sb, new Vector2(rect.X + pad, rect.Y + pad), new Vector2(rect.Right - pad, rect.Bottom - pad), color, thickness);
            // Linea diagonal 2 (esquina sup-der a inf-izq)
            DrawLine(sb, new Vector2(rect.Right - pad, rect.Y + pad), new Vector2(rect.X + pad, rect.Bottom - pad), color, thickness);
        }

        private void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)System.Math.Atan2(edge.Y, edge.X);
            float length = edge.Length();
            if (length < 1f) return;
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)start.X, (int)start.Y, (int)length, (int)thickness),
                null, color, angle, new Vector2(0, thickness / 2f), SpriteEffects.None, 0);
        }

        private void DrawStars(SpriteBatch sb, Rectangle rect)
        {
            for (int i = 0; i < 40; i++)
            {
                int seed = i * 73856093;
                float bx = (seed % 1000) / 1000f * rect.Width;
                float by = ((seed * 19349663) % 1000) / 1000f * rect.Height;
                float sx = rect.X + bx;
                float sy = rect.Y + by;
                float twinkle = 0.5f + 0.5f * (float)System.Math.Sin(_time * 2 + i * 0.5f);
                int alpha = (int)(120 * twinkle * 0.5f);
                if (alpha < 0) alpha = 0; if (alpha > 255) alpha = 255;
                int sz = 1 + (seed % 2);
                Color c = i % 3 == 0 ? new Color(245, 196, 81, alpha)
                        : i % 3 == 1 ? new Color(179, 136, 255, alpha)
                        : new Color(200, 220, 255, alpha);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)sx, (int)sy, sz, sz), c);
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
