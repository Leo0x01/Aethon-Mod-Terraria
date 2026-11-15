using System;
using System.Collections.Generic;
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
    /// <summary>
    /// PergaminoUI — v6.140 — EL PERGAMINO DEL GRIMORIO (la letra: «dame
    /// una idea sobre que se podria hacer con todo el texto del altar, es
    /// importante, tal vez creando un libro de informacion como otros
    /// mods, incluso creando la textura de un pergamino» + «el pergamino
    /// quedo genial, aunque el sello rojo de abajo a la derecha deberia
    /// ser el ojo del grimorio animado y mirando el cursor en todo
    /// momento» + «en este pergamino debes implementar toda la
    /// informacion importante»).
    ///
    /// EL DISEÑO: un botón «?» en el panel del altar abre EL PERGAMINO —
    /// la enciclopedia del mod dibujada como hoja antigua: el fondo de
    /// pergamino pintado PROCEDURALMENTE (el tono arena-oro quemado, las
    /// vetas de fibra, el borde quemado y las dos varillas), el texto en
    /// la fuente de manuscrito legible de la casa (MouseText — la única
    /// fuente que el juego garantiza en todas las pantallas; la
    /// «escritura a mano» vive en la TEXTURA y en el tono tinta) y EL
    /// OJO DEL GRIMORIO abajo a la derecha: la almendra violeta con el
    /// iris dorado que SIGUE AL CURSOR en todo momento (la pupila se
    /// desplaza hacia el ratón, el párpado parpadea solo cada pocos
    /// segundos — el sello VIVO del libro).
    ///
    /// EL CONTENIDO (la letra: «toda la informacion importante»):
    /// · EL ALTAR · EL MUNDO INTERIOR · LAS OLEADAS DE HAMBRE
    /// · EL GRIMORIO Y SUS MECÁNICAS · LA ESCALERA COMPLETA DEL
    ///   DESPERTAR (las 17 puertas de la caza + la cima 100 + la
    ///   escalera larga 101-1000 con las piezas de la Forma Eterna)
    /// · LOS ATAQUES DEL GRIMORIO Y SUS MINIONS
    /// · LA FORMA ETERNA Y EL AURA DEL VACÍO · SUS BUFFS Y DEBUFFS.
    /// Todo en hjson (los 3 idiomas de la casa) — el pergamino habla
    /// el idioma del juego.
    ///
    /// LA NAVEGACIÓN: rueda del ratón / flechas para desplazar, ESC o
    /// clic en la X para cerrar. Nada de inventario roto: mouseInterface
    /// come los clics como toda la casa.
    /// </summary>
    public class PergaminoUI : ModSystem
    {
        private static UserInterface _iface;
        private static PergaminoEstado _estado;
        internal static bool Abierto => _estado != null && _estado.Visible;

        public override void Load()
        {
            _iface = new UserInterface();
        }

        /// <summary>ABRE el pergamino (lo llama el botón «?» del altar).</summary>
        public static void Abrir()
        {
            if (_estado == null) _estado = new PergaminoEstado();
            _estado.Visible = true;
            _iface.SetState(_estado);
        }

        public static void Cerrar()
        {
            if (_estado != null) _estado.Visible = false;
            _iface.SetState(null);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (_estado != null && _estado.Visible)
                _iface.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(l => l.Name.Equals("Vanilla: Inventory"));
            if (idx < 0) idx = layers.Count - 1;
            layers.Insert(idx, new LegacyGameInterfaceLayer("AethonMod: Pergamino",
                () =>
                {
                    if (_estado != null && _estado.Visible)
                        _iface.Draw(Main.spriteBatch, new GameTime());
                    return true;
                }, InterfaceScaleType.UI));
        }
    }

    /// <summary>El estado del pergamino (la hoja viva).</summary>
    internal class PergaminoEstado : UIState
    {
        public bool Visible;
        private float _scroll;
        private float _scrollMax;
        private int _parpadeo;
        private int _parpadeoEn;

        // LA TINTA DE LA CASA (los tonos del pergamino quemado)
        private static readonly Color Tinta = new(58, 38, 28);
        private static readonly Color TintaTitulo = new(94, 44, 118);
        private static readonly Color TintaOro = new(150, 108, 34);
        private static readonly Color TintaApagada = new(96, 74, 52);

        private static Texture2D _pixel;
        private static Texture2D _glow;

        public override void OnInitialize()
        {
            _pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
            _glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            _parpadeoEn = 240 + Main.rand.Next(300);
        }

        private static string T(string c) => Language.GetTextValue("Mods.AethonMod.Pergamino." + c);

        public override void Update(GameTime gameTime)
        {
            // LA RUEDA Y LAS FLECHAS desplazan la hoja
            if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Up))
                _scroll -= 14f;
            if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Down))
                _scroll += 14f;
            // v6.140 — LA RUEDA DEL RATÓN (el camino de esta era del
            // tML: PlayerInput.ScrollWheelDelta — verificado en el
            // decompile; el delta se consume solo en el frame)
            int deltaRueda = Terraria.GameInput.PlayerInput.ScrollWheelDelta;
            if (deltaRueda != 0)
                _scroll -= deltaRueda * 0.45f;
            _scroll = MathHelper.Clamp(_scroll, 0f, Math.Max(0f, _scrollMax));
            if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                PergaminoUI.Cerrar();

            // EL PARPADEO DEL OJO (solo cada pocos segundos, rápido)
            if (_parpadeo > 0) _parpadeo--;
            else if (--_parpadeoEn <= 0)
            {
                _parpadeo = 8;
                _parpadeoEn = 280 + Main.rand.Next(360);
            }
        }

        public void Rueda(bool arriba)
        {
            _scroll = MathHelper.Clamp(_scroll + (arriba ? -56f : 56f),
                0f, Math.Max(0f, _scrollMax));
        }

        public override void Draw(SpriteBatch sb)
        {
            try
            {
                // EL OJO COME AL CURSOR mientras el pergamino vive
                Main.LocalPlayer.mouseInterface = true;
                Main.playerInventory = false;

                int w = Math.Min(980, Main.screenWidth - 60);
                int h = Math.Min(760, Main.screenHeight - 40);
                int x = (Main.screenWidth - w) / 2;
                int y = (Main.screenHeight - h) / 2;

                // ============ LA HOJA DEL PERGAMINO ============
                // EL CUERPO: el tono arena-oro del mockup aprobado
                sb.Draw(_pixel, new Rectangle(x - 8, y - 8, w + 16, h + 16),
                    new Color(84, 58, 36));                    // las varillas
                sb.Draw(_pixel, new Rectangle(x, y, w, h),
                    new Color(216, 186, 136));                 // la hoja
                // LAS DOS BANDAS de sombra (arriba/abajo — el enrollado)
                sb.Draw(_pixel, new Rectangle(x, y, w, 26),
                    new Color(196, 162, 112));
                sb.Draw(_pixel, new Rectangle(x, y + h - 26, w, 26),
                    new Color(196, 162, 112));
                // EL BORDE QUEMADO
                MarcoHoja(sb, new Rectangle(x, y, w, h), new Color(139, 106, 66));
                // EL VELO (la mancha de edad — una banda diagonal suave)
                sb.Draw(_pixel, new Rectangle(x + w / 2 - 130, y, 260, h),
                    new Color(196, 162, 112) * 0.25f);

                // ============ EL TITULAR ============
                float cy = y + 34;
                string tit = T("Titulo");
                var fuente = FontAssets.MouseText.Value;
                float escalaTit = EscalaQueCabe(tit, 1.15f, w - 80f);
                Vector2 med = fuente.MeasureString(tit) * escalaTit;
                Utils.DrawBorderString(sb, tit, new Vector2(x + w / 2f - med.X / 2f, cy),
                    TintaTitulo, escalaTit);
                cy += med.Y + 8f;
                string sub = T("Subtitulo");
                float escalaSub = EscalaQueCabe(sub, 0.8f, w - 80f);
                Vector2 medSub = fuente.MeasureString(sub) * escalaSub;
                Utils.DrawBorderString(sb, sub, new Vector2(x + w / 2f - medSub.X / 2f, cy),
                    TintaOro, escalaSub);
                cy += medSub.Y + 18f;

                // ============ EL CUERPO CON RECORTE (el scroll) ============
                int cuerpoY = (int)cy;
                int cuerpoH = h - (int)cy - y - 44;
                Rectangle recorte = new Rectangle(x + 12, cuerpoY, w - 24, cuerpoH);
                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null,
                    Matrix.CreateTranslation(0, 0, 0));

                float altoTotal = 0f;
                // LA MEDIDA PASADA 1 (para el scroll máximo)
                altoTotal = DibujarSecciones(sb, x, y, w, cuerpoY, recorte, true, ref _scroll);
                _scrollMax = Math.Max(0f, altoTotal - cuerpoH);
                // LA PASADA 2 (el dibujo real)
                DibujarSecciones(sb, x, y, w, cuerpoY, recorte, false, ref _scroll);

                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None,
                    Main.Rasterizer, null, Main.Transform);

                // ============ EL OJO DEL GRIMORIO (el sello vivo) ============
                DibujarElOjo(sb, x + w - 96, y + h - 96, 44f);

                // ============ LA X DE CIERRE ============
                var rectX = new Rectangle(x + w - 40, y + 10, 28, 28);
                bool hoverX = rectX.Contains(Main.mouseX, Main.mouseY);
                sb.Draw(_pixel, rectX, (hoverX ? new Color(220, 90, 90) : TintaApagada) * 0.35f);
                Utils.DrawBorderString(sb, "X",
                    new Vector2(rectX.Center.X, rectX.Center.Y),
                    hoverX ? new Color(220, 90, 90) : Tinta, 1f, 0.5f, 0.5f);
                if (hoverX && Main.mouseLeft && Main.mouseLeftRelease)
                    PergaminoUI.Cerrar();
            }
            catch { }
        }

        /// <summary>TODAS las secciones del pergamino (medir o dibujar).
        /// La estructura: las 8 partes de la letra, cada una con su
        /// título violeta, su línea dorada y sus líneas tinta.</summary>
        private float DibujarSecciones(SpriteBatch sb, int x, int y, int w,
            int cuerpoY, Rectangle recorte, bool soloMedir, ref float scroll)
        {
            var fuente = FontAssets.MouseText.Value;
            float cy = cuerpoY - scroll;
            float xizq = x + 40f;
            float maxAncho = w - 80f;

            // LAS SECCIONES — cada una: (clave título, claves líneas)
            string[][] secciones =
            {
                new[] { "SecAltar", "Altar1", "Altar2", "Altar3", "Altar4", "Altar5" },
                new[] { "SecInterior", "Interior1", "Interior2", "Interior3", "Interior4" },
                new[] { "SecOleadas", "Oleadas1", "Oleadas2", "Oleadas3", "Oleadas4" },
                new[] { "SecGrimorio", "Grimorio1", "Grimorio2", "Grimorio3", "Grimorio4", "Grimorio5" },
                new[] { "SecDespertar", "Despertar1", "Despertar2", "Despertar3", "Despertar4" },
                new[] { "SecAtaques", "Ataques1", "Ataques2", "Ataques3", "Ataques4", "Ataques5", "Ataques6" },
                new[] { "SecForma", "Forma1", "Forma2", "Forma3", "Forma4" },
                new[] { "SecAuras", "Auras1", "Auras2", "Auras3", "Auras4", "Auras5", "Auras6" },
            };

            foreach (var sec in secciones)
            {
                cy += 14f;
                // EL TÍTULO de la sección
                string titulo = T(sec[0]);
                var medT = fuente.MeasureString(titulo);
                if (!soloMedir && VisibleEn(cy, medT.Y, recorte))
                    Utils.DrawBorderString(sb, titulo, new Vector2(xizq, cy), TintaTitulo, 1.0f);
                // LA LÍNEA DORADA al lado del título
                if (!soloMedir && VisibleEn(cy, medT.Y, recorte))
                    sb.Draw(_pixel, new Rectangle(
                        (int)(xizq + medT.X + 14), (int)(cy + 11),
                        (int)(x + w - 40 - xizq - medT.X - 14), 2), TintaOro * 0.7f);
                cy += medT.Y + 10f;

                for (int i = 1; i < sec.Length; i++)
                {
                    cy = LineaPergamino(sb, T(sec[i]), xizq + 18f, cy, maxAncho - 18f,
                        Tinta, soloMedir, recorte);
                }
                cy += 6f;
            }
            return cy + scroll - cuerpoY + 30f;
        }

        /// <summary>Una línea del pergamino: envuelta por palabras y
        /// recortada a la hoja (solo se dibuja lo visible).</summary>
        private float LineaPergamino(SpriteBatch sb, string texto, float x, float cy,
            float maxAncho, Color c, bool soloMedir, Rectangle recorte)
        {
            var fuente = FontAssets.MouseText.Value;
            float altoLinea = fuente.MeasureString("Ag").Y * 0.88f + 3f;
            var palabras = texto.Split(' ');
            var linea = new System.Text.StringBuilder();
            foreach (string pal in palabras)
            {
                string prueba = linea.Length == 0 ? pal : linea + " " + pal;
                if (fuente.MeasureString(prueba).X * 0.88f > maxAncho && linea.Length > 0)
                {
                    if (!soloMedir && VisibleEn(cy, altoLinea, recorte))
                        Utils.DrawBorderString(sb, linea.ToString(), new Vector2(x, cy), c, 0.88f);
                    cy += altoLinea;
                    linea.Clear().Append(pal);
                }
                else linea.Clear().Append(prueba);
            }
            if (linea.Length > 0)
            {
                if (!soloMedir && VisibleEn(cy, altoLinea, recorte))
                    Utils.DrawBorderString(sb, linea.ToString(), new Vector2(x, cy), c, 0.88f);
                cy += altoLinea;
            }
            return cy + 4f;
        }

        private static bool VisibleEn(float cy, float alto, Rectangle recorte)
            => cy < recorte.Y + recorte.Height && cy + alto > recorte.Y;

        private static float EscalaQueCabe(string texto, float escala, float maxAncho)
        {
            var fuente = FontAssets.MouseText.Value;
            float e = escala;
            while (e > 0.55f && fuente.MeasureString(texto).X * e > maxAncho)
                e -= 0.05f;
            return e;
        }

        private static void MarcoHoja(SpriteBatch sb, Rectangle r, Color c)
        {
            sb.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, 3), c);
            sb.Draw(_pixel, new Rectangle(r.X, r.Y + r.Height - 3, r.Width, 3), c);
            sb.Draw(_pixel, new Rectangle(r.X, r.Y, 3, r.Height), c);
            sb.Draw(_pixel, new Rectangle(r.X + r.Width - 3, r.Y, 3, r.Height), c);
        }

        // ================================================================
        //  EL OJO DEL GRIMORIO — el sello vivo (la letra: «el sello rojo
        //  de abajo a la derecha deberia ser el ojo del grimorio animado
        //  y mirando el cursor en todo momento»): la almendra violeta,
        //  el iris dorado y la pupila que SIGUE AL CURSOR — con su
        //  párpado que parpadea SOLO cada pocos segundos.
        // ================================================================
        private void DibujarElOjo(SpriteBatch sb, float cx, float cy, float radio)
        {
            // EL RESPLANDOR DEL SELLO (el aura del ojo — la casa)
            sb.Draw(_glow, new Rectangle((int)(cx - radio * 2.4f), (int)(cy - radio * 2.4f),
                (int)(radio * 4.8f), (int)(radio * 4.8f)),
                new Color(150, 84, 235) * 0.30f);

            // LA ALMONDRA (la forma del ojo — dos arcos)
            float t = Main.GlobalTimeWrappedHourly;
            // EL PARPADEO: 8 ticks de cierre (la almendra se aplana)
            float apertura = 1f;
            if (_parpadeo > 0)
                apertura = Math.Abs(_parpadeo - 4) / 4f;      // cierra y abre

            for (int k = 0; k < 26; k++)
            {
                float f = k / 25f;
                float ang = MathHelper.Lerp(-1.1f, 1.1f + (float)Math.PI, f);
                // el contorno de la almendra: dos curvas
                float rx = (float)Math.Cos(ang) * radio;
                float ry = (float)Math.Sin(ang) * radio * 0.55f * apertura;
                var pos = new Vector2(cx + rx, cy + ry);
                sb.Draw(_pixel, new Rectangle((int)(pos.X - 2), (int)(pos.Y - 2), 4, 4),
                    new Color(58, 32, 92));
            }

            // EL IRIS DORADO — el centro que MIRA
            float irisR = radio * 0.42f;
            // LA DIRECCIÓN DE LA MIRADA: hacia el CURSOR (clamp al radio)
            Vector2 hacia = new Vector2(Main.mouseX, Main.mouseY) - new Vector2(cx, cy);
            if (hacia.LengthSquared() > 1f) hacia.Normalize();
            Vector2 mirada = hacia * (radio * 0.22f);
            sb.Draw(_glow, new Rectangle(
                (int)(cx + mirada.X - irisR), (int)(cy + mirada.Y - irisR),
                (int)(irisR * 2), (int)(irisR * 2)),
                new Color(255, 200, 90) * 0.95f);
            // EL ANILLO DEL IRIS
            for (int k = 0; k < 16; k++)
            {
                float ang = k / 16f * MathF.PI * 2f;
                var pos = new Vector2(
                    cx + mirada.X + MathF.Cos(ang) * irisR,
                    cy + mirada.Y + MathF.Sin(ang) * irisR * apertura);
                sb.Draw(_pixel, new Rectangle((int)(pos.X - 1), (int)(pos.Y - 1), 3, 3),
                    new Color(150, 108, 34));
            }
            // LA PUPILA (la punta negra que SIEMPRE mira al cursor)
            float pupila = irisR * 0.45f;
            sb.Draw(_glow, new Rectangle(
                (int)(cx + mirada.X * 1.25f - pupila), (int)(cy + mirada.Y * 1.25f - pupila),
                (int)(pupila * 2), (int)(pupila * 2)),
                new Color(24, 12, 38));

            // EL BRILLO (la chispa de vida del ojo)
            sb.Draw(_glow, new Rectangle(
                (int)(cx + mirada.X - irisR * 0.9f - 4), (int)(cy + mirada.Y - irisR * 0.9f - 4),
                8, 8), new Color(255, 246, 220) * 0.8f);
        }
    }
}
