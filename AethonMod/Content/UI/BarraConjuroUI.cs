using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace AethonMod.Content.UI
{
    // ======================================================================
    //  v6.52.0 — BARRACONJURO: LA BARRA DE CONJURO del jefe (el patrón
    //  «cast bar» clásico del género — R3 §2.4: el telegraph más HONESTO
    //  que existe: el NOMBRE del ataque y SU reloj, a la vista).
    //
    //  LA RECETA: una barra 280×26 bajo el hotbar que se LLENA mientras
    //  el jefe telegrafía; el nombre del ataque en su color; y los
    //  ÚLTIMOS 10 ticks PARPADEAN blanca dos veces — el «AHORA» visual
    //  de la investigación R3 §1.1 (el jugador SIENTE cuándo llega).
    // ======================================================================
    public class BarraConjuroUI : ModSystem
    {
        // === EL ESTADO (marcas de frame — derivado, sin timers) ===
        private static string _texto = "";
        private static Color _color = new(150, 110, 230);
        private static uint _frameInicio;
        private static uint _duracion;
        private static bool _activa;

        /// <summary>MUESTRA la barra con un ataque (nombre por CLAVE de localización).</summary>
        public static void Mostrar(string claveNombre, Color color, float duracionTicks)
        {
            if (Main.netMode == NetmodeID.Server) return;
            _texto = Language.GetTextValue(claveNombre);
            _color = color;
            _frameInicio = Main.GameUpdateCount;
            _duracion = (uint)MathHelper.Max(10f, duracionTicks);
            _activa = true;
        }

        /// <summary>La barra se apaga (el golpe llegó o el ataque se canceló).</summary>
        public static void Ocultar() => _activa = false;

        public override void ModifyInterfaceLayers(System.Collections.Generic.List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Hotbar");
            if (idx != -1)
            {
                layers.Insert(idx + 1, new LegacyGameInterfaceLayer(
                    "AethonMod: Barra Conjuro",
                    () => { Dibujar(); return true; },
                    InterfaceScaleType.UI));
            }
        }

        private static void Dibujar()
        {
            try
            {
                if (!_activa || Main.gameMenu || Main.playerInventory) return;

                uint edad = Main.GameUpdateCount - _frameInicio;
                if (edad >= _duracion) { _activa = false; return; }

                float progreso = edad / (float)_duracion;
                float restante = _duracion - edad;

                SpriteBatch sb = Main.spriteBatch;
                Texture2D pixel = TextureAssets.MagicPixel.Value;

                // LA POSICIÓN: centrada, bajo el hotbar.
                int w = 280, h = 26;
                int x = Main.screenWidth / 2 - w / 2;
                int y = Main.screenHeight - 172;

                // EL FONDO + el borde doble de la casa.
                sb.Draw(pixel, new Rectangle(x - 2, y - 2, w + 4, h + 4), new Color(10, 7, 16) * 0.9f);
                Marco(sb, pixel, new Rectangle(x - 2, y - 2, w + 4, h + 4),
                    new Color(_color.R, _color.G, _color.B, 200));
                sb.Draw(pixel, new Rectangle(x, y, w, h), new Color(18, 12, 28) * 0.92f);

                // EL RELLENO: gradiente dos-tintes (oscuro→color vivo).
                int lleno = (int)(w * MathHelper.Clamp(progreso, 0f, 1f));
                for (int i = 0; i < lleno; i++)
                {
                    float t = i / (float)w;
                    Color c = Color.Lerp(_color * 0.45f, _color, t);
                    sb.Draw(pixel, new Rectangle(x + i, y, 1, h), c * 0.85f);
                }

                // EL BRILLO del borde de avance (la punta que corre).
                if (lleno > 0 && lleno < w)
                    sb.Draw(pixel, new Rectangle(x + lleno - 2, y, 2, h), Color.White * 0.5f);

                // EL PARPADEO FINAL: los últimos 10 t, blanca ×2 (el AHORA).
                bool destello = restante <= 10f && (edad / 5u) % 2u == 0u;
                if (destello)
                    sb.Draw(pixel, new Rectangle(x, y, w, h), Color.White * 0.30f);

                // EL NOMBRE (centrado, en el color del ataque).
                Utils.DrawBorderString(Main.spriteBatch, _texto,
                    new Vector2(x + w / 2f, y + h / 2f),
                    destello ? Color.White : new Color(240, 234, 255), 0.95f, 0.5f, 0.4f);
            }
            catch { }
        }

        private static void Marco(SpriteBatch sb, Texture2D pixel, Rectangle r, Color c)
        {
            sb.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, 2), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Bottom - 2, r.Width, 2), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Y, 2, r.Height), c);
            sb.Draw(pixel, new Rectangle(r.Right - 2, r.Y, 2, r.Height), c);
        }

        public override void OnWorldUnload() => _activa = false;
        public override void Unload() => _activa = false;
    }
}
