using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace AethonMod.Content.UI
{
    // ======================================================================
    //  v6.67 — EL CIELO DEL DESPERTAR: LA VENTANA DE MISIONES (la letra
    //  del usuario: «necesitamos crear alguna interfaz nueva para algun
    //  item o algo asi que tenga una ventana nueva donde aparezca un
    //  checklist con todos los requisitos para el despertar, y que estos
    //  sean dados como una mision, por ejemplo matar todos los jefes
    //  necesarios con un checlist que lo marque […] y tambien con las
    //  armas que necesita el grimorio para despertar o lingotes, asi el
    //  jugador puede ir completando los requisitos de cualquier nivel de
    //  despertar aunque no este ahi»).
    //
    //  LA VENTANA (la abre EL OJO DEL CIELO — el ítem de la textura del
    //  cielo): dos paneles que son UNA LISTA DE MISIONES viva:
    //   · LA CAZA (izquierda): los 17 jefes del camino, con SU nivel y
    //     su estado — CAÍDO (dorado: la bandera del mundo o el registro
    //     del cazador: crédito retroactivo DE VERDAD) o pendiente.
    //   · LA ESCALERA (derecha, con rueda del ratón): los CIEN peldaños,
    //     cada uno con TODOS sus requisitos — el jefe de su puerta, el
    //     arma del peldaño (con sus sprites y «¿la llevas en la bolsa?»),
    //     la alternativa mineral, las esencias y el dinero del peldaño
    //     VIVO (con los contadores de la mesa en tiempo real), el hito
    //     que abre o el micropoder que entrega.
    //
    //  TODO es lectura: la ventana no toca al mundo (pura pantalla de
    //  cliente — el patrón BarraConjuro de la casa).
    // ======================================================================
    public class CieloDespiertoUI : ModSystem
    {
        // === EL ESTADO (cliente puro) ===
        private static bool _abierta;
        private static int _scrollEscala;          // px de desplazamiento del panel derecho
        private static int _ultimaRueda = int.MinValue;

        /// <summary>¿EL CIELO está abierto?</summary>
        public static bool Abierta => _abierta;

        /// <summary>EL CIELO SE ABRE (o se cierra — lo llama el Ojo del Cielo).</summary>
        public static void Alternar()
        {
            _abierta = !_abierta;
            if (!_abierta) { _scrollEscala = 0; _ultimaRueda = int.MinValue; }
        }

        public override void OnWorldUnload() { _abierta = false; _scrollEscala = 0; }
        public override void Unload() { _abierta = false; _scrollEscala = 0; }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // ENCIMA de todo (debajo solo del texto del cursor): la capa
            // de la casa al estilo BarraConjuro — insertada antes de
            // "Vanilla: Mouse Text", la última de vanilla.
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
            if (idx != -1)
            {
                layers.Insert(idx, new LegacyGameInterfaceLayer(
                    "AethonMod: El Cielo del Despertar",
                    DibujarElCielo, InterfaceScaleType.UI));
            }
        }

        // ================================================================
        //  LA PALETA DE LA CASA
        // ================================================================
        private static readonly Color TintaFondo = new(12, 8, 20);
        private static readonly Color TintaPanel = new(20, 13, 32);
        private static readonly Color TintaVivo = new(196, 138, 255);
        private static readonly Color TintaDorado = new(255, 215, 120);
        private static readonly Color TintaVerde = new(120, 235, 150);
        private static readonly Color TintaRojo = new(255, 120, 120);
        private static readonly Color TintaTexto = new(232, 216, 255);
        private static readonly Color TintaApagado = new(120, 120, 140);
        private static string Clave(string c) => "Mods.AethonMod.Despertar." + c;

        /// <summary>EL RASTERIZADOR CON TIJERAS (el clip del panel derecho:
        /// sin ScissorTestEnable el recorte NO ocurre — la lista entera
        /// se derramaría sobre el pie de la ventana).</summary>
        private static readonly RasterizerState TijerasUI = new RasterizerState
        {
            CullMode = CullMode.None,
            ScissorTestEnable = true,
        };

        // ================================================================
        //  EL DIBUJO ENTERO
        // ================================================================
        private static bool DibujarElCielo()
        {
            try
            {
                if (!_abierta || Main.gameMenu || Main.netMode == NetmodeID.Server)
                    return true;

                SpriteBatch sb = Main.spriteBatch;
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active) { _abierta = false; return true; }

                // EL LIBRO ACTIVO (la escalera que el cielo ve: sin el
                // Eterno encima, la ventana igual abre — pero enseñando
                // que no ve ningún libro).
                Item libro = Globals.AlmaEternoGlobalItem.Activo(yo);
                var alma = Globals.AlmaEternoGlobalItem.De(libro);
                int despertar = alma?.NivelDespertar ?? 0;
                int siguiente = System.Math.Min(despertar + 1,
                    Systems.DespertarGrimorio.NivelMax);

                // === LA GEOMETRÍA (centrada, un poco arriba) ===
                int w = 740;
                int h = 560;
                if (Main.screenWidth < w + 20) w = Main.screenWidth - 20;
                if (Main.screenHeight < h + 20) h = Main.screenHeight - 20;
                int x = Main.screenWidth / 2 - w / 2;
                int y = Main.screenHeight / 2 - h / 2;

                // === EL FONDO + el marco doble de la casa ===
                sb.Draw(pixel, new Rectangle(x - 3, y - 3, w + 6, h + 6), TintaFondo * 0.94f);
                Marco(sb, pixel, new Rectangle(x - 3, y - 3, w + 6, h + 6), TintaVivo * 0.85f);
                sb.Draw(pixel, new Rectangle(x, y, w, h), TintaFondo * 0.96f);

                // EL TITULAR
                string titulo = Language.GetTextValue(Clave("CieloTitulo"));
                Utils.DrawBorderString(sb, titulo, new Vector2(x + w / 2f, y + 22f),
                    TintaVivo, 1.15f, 0.5f, 0.4f);
                string subtitulo = Language.GetTextValue(Clave("CieloSubtitulo"));
                Utils.DrawBorderString(sb, subtitulo, new Vector2(x + w / 2f, y + 44f),
                    TintaApagado, 0.80f, 0.5f, 0.4f);

                // EL DESPERTAR ACTUAL (dorado) — o el aviso sin libro.
                if (alma != null)
                {
                    string nivelTxt = Language.GetTextValue(Clave("CieloDespertarActual"), despertar);
                    Utils.DrawBorderString(sb, nivelTxt, new Vector2(x + w - 70f, y + 22f),
                        TintaDorado, 0.95f, 1f, 0.4f);
                }
                else
                {
                    string sinLibro = Language.GetTextValue(Clave("CieloSinLibro"));
                    Utils.DrawBorderString(sb, sinLibro, new Vector2(x + w / 2f, y + 62f),
                        TintaRojo * 0.9f, 0.85f, 0.5f, 0.4f);
                }

                // EL BOTÓN [X] (cerrar) — el único botón de la ventana.
                Rectangle btnX = new Rectangle(x + w - 34, y + 6, 26, 26);
                sb.Draw(pixel, btnX, (EstaEnPantalla(btnX) ? TintaRojo : TintaApagado) * 0.35f);
                Utils.DrawBorderString(sb, "X", new Vector2(btnX.Center.X, btnY(btnX)),
                    TintaTexto, 0.9f, 0.5f, 0.4f);
                if (EstaEnPantalla(btnX) && Main.mouseLeft && Main.mouseLeftRelease)
                    _abierta = false;

                // CLIC DERECHO SOBRE LA VENTANA: cerrar (el gesto del
                // cielo — pero solo DENTRO: un clic derecho en el mundo
                // jugando no cierra nada).
                if (Main.mouseRight && Main.mouseRightRelease &&
                    EstaEnPantalla(new Rectangle(x, y, w, h)))
                    _abierta = false;

                // === LA PARTIDA DE LOS PANELES ===
                int cabecera = 72;
                int pie = 30;
                int panelH = h - cabecera - pie;
                Rectangle panelCaza = new Rectangle(x + 8, y + cabecera, 236, panelH);
                Rectangle panelEscala = new Rectangle(x + 8 + 236 + 8, y + cabecera,
                    w - 16 - 236 - 8, panelH);

                DibujarLaCaza(sb, pixel, panelCaza, yo, despertar);
                _scrollEscala = DibujarLaEscalera(sb, pixel, panelEscala, yo, alma, despertar, siguiente);

                // EL PIE (la pista de uso)
                string pista = Language.GetTextValue(Clave("CieloPista"));
                Utils.DrawBorderString(sb, pista, new Vector2(x + w / 2f, y + h - 14f),
                    TintaApagado, 0.80f, 0.5f, 0.4f);
            }
            catch { }
            return true;
        }

        private static float btnY(Rectangle r) => r.Y + r.Height / 2f;

        /// <summary>¿El cursor está sobre un rectángulo de pantalla?</summary>
        private static bool EstaEnPantalla(Rectangle r) => r.Contains(Main.mouseX, Main.mouseY);

        /// <summary>EL RECORTE CON ELIPSIS (la fuente de la casa): un texto
        /// que no cabe en su columna se corta limpio con «…» — la misión
        /// se lee siempre dentro de su hueco.</summary>
        private static string Recortar(string texto, float maxPx)
        {
            var fuente = FontAssets.MouseText.Value;
            if (fuente == null || fuente.MeasureString(texto).X <= maxPx) return texto;
            string corto = texto;
            while (corto.Length > 4 && fuente.MeasureString(corto + "…").X > maxPx)
                corto = corto.Substring(0, corto.Length - 1);
            return corto + "…";
        }

        /// <summary>EL DINERO COMPACTO (para la línea del peldaño vivo): el
        /// tramo más grande de la deuda («3 de oro…» en vez de la fila
        /// entera de monedas).</summary>
        private static string DineroCompacto(long cobre)
        {
            if (cobre <= 0L) return "0";
            string completo = Systems.DespertarGrimorioSistema.ItemToText(cobre);
            int corte = completo.IndexOf('·');
            return corte > 0 ? completo.Substring(0, corte).Trim() : completo;
        }

        /// <summary>EL MARCO DOBLE de la casa (BarraConjuro).</summary>
        private static void Marco(SpriteBatch sb, Texture2D pixel, Rectangle r, Color c)
        {
            sb.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, 2), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Bottom - 2, r.Width, 2), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Y, 2, r.Height), c);
            sb.Draw(pixel, new Rectangle(r.Right - 2, r.Y, 2, r.Height), c);
        }

        // ================================================================
        //  EL PANEL IZQUIERDO — LA CAZA (los 17 jefes del camino)
        // ================================================================
        private static void DibujarLaCaza(SpriteBatch sb, Texture2D pixel,
            Rectangle panel, Player yo, int despertar)
        {
            // EL FONDO DEL PANEL + su marco.
            sb.Draw(pixel, panel, TintaPanel * 0.92f);
            Marco(sb, pixel, panel, TintaVivo * 0.55f);

            string titular = Language.GetTextValue(Clave("CieloLaCaza"));
            Utils.DrawBorderString(sb, titular, new Vector2(panel.Center.X, panel.Y + 14f),
                TintaVivo, 0.92f, 0.5f, 0.4f);

            var jefes = Systems.DespertarGrimorio.LosJefesDelDespertar;
            float fila = panel.Y + 32f;
            float paso = (panel.Height - 40f) / jefes.Length;

            for (int i = 0; i < jefes.Length; i++)
            {
                var j = jefes[i];
                bool caido = Systems.DespertarGrimorio.JefeYaCaido(yo, j.Nivel);
                Color tinta = caido ? TintaDorado : (j.Nivel <= despertar ? TintaRojo : TintaTexto);

                // LA FILA: un velo sutil si está caído.
                Rectangle filaR = new Rectangle(panel.X + 3, (int)fila, panel.Width - 6, (int)paso);
                if (caido)
                    sb.Draw(pixel, filaR, TintaDorado * 0.06f);

                // EL RETRATO (la textura del NPC, ajustada al hueco).
                try
                {
                    Texture2D retrato = TextureAssets.Npc[j.Miniatura].Value;
                    if (retrato != null)
                    {
                        float esc = MathHelper.Min(20f / retrato.Width, 20f / retrato.Height);
                        sb.Draw(retrato, new Vector2(panel.X + 16f, fila + paso * 0.5f),
                            null, Color.White * 0.95f, 0f, retrato.Size() * 0.5f,
                            esc, SpriteEffects.None, 0f);
                    }
                }
                catch { }

                // EL NOMBRE y el estado (recortados a la columna angosta).
                string nombre = Recortar(Language.GetTextValue(j.Clave), 178f);
                Utils.DrawBorderString(sb, nombre, new Vector2(panel.X + 32f, fila + paso * 0.5f - 7f),
                    tinta, 0.88f, 0f, 0.4f);

                string exige = caido
                    ? Language.GetTextValue(Clave("CieloCaido"))
                    : Language.GetTextValue(Clave("CieloSeExigeEn"), j.Nivel);
                Utils.DrawBorderString(sb, exige, new Vector2(panel.X + 32f, fila + paso * 0.5f + 6f),
                    caido ? TintaVerde : TintaApagado, 0.78f, 0f, 0.4f);

                fila += paso;
            }
        }

        // ================================================================
        //  EL PANEL DERECHO — LA ESCALERA (los CIEN peldaños, con rueda)
        // ================================================================
        private static int DibujarLaEscalera(SpriteBatch sb, Texture2D pixel,
            Rectangle panel, Player yo, Globals.AlmaEternoGlobalItem alma,
            int despertar, int siguiente)
        {
            // EL FONDO DEL PANEL + su marco.
            sb.Draw(pixel, panel, TintaPanel * 0.92f);
            Marco(sb, pixel, panel, TintaVivo * 0.55f);

            string titular = Language.GetTextValue(Clave("CieloLaEscalera"));
            Utils.DrawBorderString(sb, titular, new Vector2(panel.Center.X, panel.Y + 14f),
                TintaVivo, 0.92f, 0.5f, 0.4f);

            // === EL CLIP (el recorte del contenido al panel) ===
            Rectangle clip = new Rectangle(panel.X + 2, panel.Y + 24,
                panel.Width - 4, panel.Height - 30);

            // EL SCISSOR EN COORDENADAS FÍSICAS (el recorte de la GPU no
            // pasa por la matriz de UI: las esquinas del clip viajan por
            // Main.UIScaleMatrix para caer en píxeles de pantalla de
            // verdad — con cualquier escala de interfaz).
            Vector2 esqA = Vector2.Transform(new Vector2(clip.X, clip.Y), Main.UIScaleMatrix);
            Vector2 esqB = Vector2.Transform(new Vector2(clip.Right, clip.Bottom), Main.UIScaleMatrix);
            Rectangle scissorClip = Rectangle.Intersect(
                new Rectangle((int)esqA.X, (int)esqA.Y,
                    (int)(esqB.X - esqA.X), (int)(esqB.Y - esqA.Y)),
                sb.GraphicsDevice.Viewport.Bounds);

            Rectangle scissor = sb.GraphicsDevice.ScissorRectangle;
            try
            {
                sb.End();
                sb.GraphicsDevice.ScissorRectangle = scissorClip;
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, TijerasUI,
                    null, Main.UIScaleMatrix);
            }
            catch { }

            // === LA RUEDA DEL RATÓN (solo con el cursor EN el panel) ===
            int rueda = Mouse.GetState().ScrollWheelValue;
            int scroll = _scrollEscala;
            if (EstaEnPantalla(panel) && _ultimaRueda != int.MinValue)
                scroll += _ultimaRueda - rueda;   // rueda arriba = positivo = subir
            _ultimaRueda = rueda;

            // === LAS FILAS (los CIEN peldaños) ===
            const float paso = 62f;
            // v6.86 — la escalera ahora son CIEN peldaños (antes 50).
            float altoTotal = Systems.DespertarGrimorio.NivelMax * paso;
            float maxScroll = MathHelper.Max(0f, altoTotal - clip.Height);
            scroll = (int)MathHelper.Clamp(scroll, 0f, maxScroll);
            _scrollEscala = scroll;

            float fy = clip.Y - scroll;
            for (int n = 1; n <= Systems.DespertarGrimorio.NivelMax; n++)
            {
                // ¿Se ve esta fila? (fuera del clip: ni se dibuja)
                if (fy + paso < clip.Y - 4f) { fy += paso; continue; }
                if (fy > clip.Bottom + 4f) break;

                bool completado = n <= despertar;
                bool vivo = n == siguiente && despertar < Systems.DespertarGrimorio.NivelMax;
                DibujarPeldano(sb, pixel, panel, clip, fy, paso, yo, alma, n, completado, vivo, despertar);
                fy += paso;
            }

            // === LA DEVOLUCIÓN DEL LOTE + el scissor (los parámetros del
            // fix v6.50.2 de PantallaSistema — el lote de UI que la capa
            // espera seguir usando) ===
            try
            {
                sb.End();
                sb.GraphicsDevice.ScissorRectangle = scissor;
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
            }
            catch { }

            // LA BARRA DE DESPLAZAMIENTO (el indicador de posición).
            if (maxScroll > 0f)
            {
                float proporcion = clip.Height / altoTotal;
                int barraH = (int)MathHelper.Max(24f, clip.Height * proporcion);
                int barraY = clip.Y + (int)(scroll / maxScroll * (clip.Height - barraH));
                sb.Draw(pixel, new Rectangle(clip.Right - 5, barraY, 4, barraH),
                    TintaVivo * 0.75f);
            }
            return _scrollEscala;
        }

        /// <summary>UN PELDAÑO DE LA ESCALERA (la misión de un nivel).</summary>
        private static void DibujarPeldano(SpriteBatch sb, Texture2D pixel,
            Rectangle panel, Rectangle clip, float fy, float paso, Player yo,
            Globals.AlmaEternoGlobalItem alma, int n, bool completado, bool vivo,
            int despertar)
        {
            // EL VELO DEL PELDAÑO: dorado tenue si está completo · violeta
            // vivo si es EL PELDAÑO VIVO (el que el libro está comiendo).
            Rectangle filaR = new Rectangle(clip.X + 2, (int)fy + 1, clip.Width - 8, (int)paso - 4);
            if (vivo)
                sb.Draw(pixel, filaR, TintaVivo * 0.14f);
            else if (completado)
                sb.Draw(pixel, filaR, TintaDorado * 0.05f);

            float izq = clip.X + 8f;
            float cy = fy + paso * 0.5f;

            // (1) EL NÚMERO + EL ESTADO.
            Color tintaN = completado ? TintaDorado : (vivo ? TintaVivo : TintaTexto);
            string etiqueta = Language.GetTextValue(Clave("CieloNivel"), n);
            Utils.DrawBorderString(sb, etiqueta, new Vector2(izq, fy + 12f), tintaN, 0.92f, 0f, 0.4f);

            string estado;
            if (completado) estado = Language.GetTextValue(Clave("CieloCompleto"));
            else if (vivo) estado = Language.GetTextValue(Clave("CieloVivo"));
            else estado = "";
            if (estado.Length > 0)
                Utils.DrawBorderString(sb, estado, new Vector2(izq + 78f, fy + 12f),
                    completado ? TintaVerde : TintaVivo, 0.78f, 0f, 0.4f);

            // (2) EL PODER DEL NIVEL (el hito que abre · el micropoder que
            //     entrega · la puerta de caza que exige) — RECORTADO a su
            //     columna (la misión se lee dentro de su hueco).
            string poder = TextoDelPoder(n);
            if (poder != null)
                Utils.DrawBorderString(sb, Recortar(poder, 250f), new Vector2(izq, fy + 30f),
                    TintaApagado, 0.80f, 0f, 0.4f);

            // (3) LA MISIÓN DEL JEFE (la puerta de caza — con su ✓ retroactivo).
            var jefeN = Systems.DespertarGrimorio.JefeDeNivel(n);
            if (jefeN != null)
            {
                bool caido = Systems.DespertarGrimorio.JefeYaCaido(yo, n);
                string textoJefe = Recortar((caido ? "[✓] " : "[✗] ") +
                    Language.GetTextValue(jefeN.Value.Clave), 250f);
                Utils.DrawBorderString(sb, textoJefe, new Vector2(izq, fy + 46f),
                    caido ? TintaVerde : TintaRojo, 0.84f, 0f, 0.4f);
            }

            // (4) EL ARMA DEL PELDAÑO + EL MATERIAL (v6.93 — LA LECTURA DEL
            //     BANCO: el pago es RETROACTIVO — lo devorado en OTRO
            //     peldaño cuenta para ESTE; disponible = devorado −
            //     cobrado. La mesa vieja de contadores por peldaño murió
            //     (ya nadie la escribe): la Escalera lee el banco VIVO).
            int[] armas = Systems.DespertarGrimorio.ArmasDelNivel[n];
            int[][] gruposMat = Systems.DespertarGrimorio.GruposMaterialesDe(n);
            bool armaEnBanco = vivo && alma != null && AlgunaArmaEnElBanco(alma, armas);
            bool materialPagado = vivo && alma != null && MaterialPagadoEnElBanco(alma, n, gruposMat);
            float ax = panel.Right - 210f;

            if (armas != null && armas.Length > 0)
            {
                bool armaEnBolsa = LlevaAlgunaAlternativa(yo, armas, out _);
                bool armaPagada = armaEnBanco;

                // LA FILA DE SPRITES (las alternativas del peldaño, 18 px).
                float sx = ax;
                int mostrados = 0;
                for (int k = 0; k < armas.Length && mostrados < 5; k++)
                {
                    try
                    {
                        Texture2D tex = TextureAssets.Item[armas[k]].Value;
                        if (tex == null) continue;
                        sb.Draw(tex, new Vector2(sx + 9f, fy + 14f), null,
                            Color.White * 0.92f, 0f, tex.Size() * 0.5f,
                            MathHelper.Min(18f / tex.Width, 18f / tex.Height),
                            SpriteEffects.None, 0f);
                        sx += 20f;
                        mostrados++;
                    }
                    catch { }
                }

                // v6.93 — SIN ESTADO «CORRUPTA»: pagada por el banco ✓ o
                //     en la bolsa (para consagrar) o falta.
                string estadoArma = armaPagada ? Language.GetTextValue(Clave("CieloArmaPagada"))
                    : (armaEnBolsa ? Language.GetTextValue(Clave("CieloEnLaBolsa"))
                    : Language.GetTextValue(Clave("CieloFaltaArma")));
                Color tintaArma = armaPagada ? TintaVerde
                    : (armaEnBolsa ? TintaDorado : TintaApagado);
                Utils.DrawBorderString(sb, estadoArma, new Vector2(ax, fy + 28f),
                    tintaArma, 0.80f, 0f, 0.4f);

                // v6.93 — EL MATERIAL DEL BANCO (retroactivo): la opción
                //     viva es la de MÁS disponible; su cuenta es
                //     disponible/cuota (los grupos son alternativas: sus
                //     bancos suman).
                int gMejor = -1; long mejorDisp = 0L; int cuotaViva = 0; int tipoVivo = -1;
                if (alma != null)
                {
                    for (int g = 0; g < gruposMat.Length; g++)
                    {
                        long disp = Globals.AlmaEternoGlobalItem
                            .BancoGrupoDisponible(alma, gruposMat[g]);
                        if (disp > mejorDisp)
                        {
                            mejorDisp = disp;
                            gMejor = g;
                            cuotaViva = Systems.DespertarGrimorio.CantidadMateriales(n, g);
                            tipoVivo = gruposMat[g] != null && gruposMat[g].Length > 0
                                ? gruposMat[g][0] : -1;
                        }
                    }
                }
                int enBolsaMin = tipoVivo > 0 ? CuentaEnLaBolsa(yo, tipoVivo) : 0;
                string textoMin = materialPagado
                    ? "[✓] " + Language.GetTextValue(Clave("CieloMaterialPagado"))
                    : "[•] " + string.Format(Language.GetTextValue(Clave("CieloMaterial")),
                        cuotaViva > 0 ? cuotaViva : 20,
                        tipoVivo > 0 ? Lang.GetItemNameValue(tipoVivo) : "?") +
                    (enBolsaMin > 0 && !materialPagado
                        ? string.Format(" · llevas {0}", enBolsaMin) : "") +
                    (gMejor >= 0 && !materialPagado && cuotaViva > 0
                        ? string.Format(" ({0}/{1})",
                            System.Math.Min(mejorDisp, cuotaViva), cuotaViva) : "");
                Utils.DrawBorderString(sb, textoMin, new Vector2(ax, fy + 44f),
                    materialPagado ? TintaVerde : (gMejor >= 0 ? TintaDorado : TintaApagado),
                    0.80f, 0f, 0.4f);
            }

            // (5) LAS ESENCIAS Y EL DINERO — SOLO del peldaño VIVO (las
            //     esencias de la mesa en tiempo real + el DINERO del
            //     BANCO, retroactivo: disponible = devorado − cobrado).
            if (vivo && alma != null)
            {
                int esenciasNecesarias = Systems.DespertarGrimorio.EsenciasPara(n);
                long monedasNecesarias = Systems.DespertarGrimorio.MonedasPara(n);
                bool esenciasOk = alma.OfrendaEsencias >= esenciasNecesarias;
                long disponible = Globals.AlmaEternoGlobalItem
                    .BancoMonedasDisponibles(alma);
                bool monedasOk = disponible >= monedasNecesarias;

                string txtEs = string.Format(Language.GetTextValue(Clave("CieloEsencias")),
                    alma.OfrendaEsencias, esenciasNecesarias);
                Utils.DrawBorderString(sb, (esenciasOk ? "[✓] " : "[•] ") + txtEs,
                    new Vector2(panel.Right - 210f, fy + 2f),
                    esenciasOk ? TintaVerde : TintaTexto, 0.78f, 0f, 0.4f);

                long falta = System.Math.Max(0L, monedasNecesarias - disponible);
                string txtMo = monedasOk
                    ? "[✓] " + Language.GetTextValue(Clave("CieloMonedasPagadas"))
                    : "[•] " + string.Format(Language.GetTextValue(Clave("CieloMonedas")),
                        DineroCompacto(falta));
                Utils.DrawBorderString(sb, txtMo,
                    new Vector2(panel.Right - 128f, fy + 2f),
                    monedasOk ? TintaVerde : TintaTexto, 0.78f, 0f, 0.4f);
            }
        }

        // ================================================================
        //  v6.93 — LOS OJOS DEL BANCO DE LA ESCALERA (las lecturas
        //  retroactivas de la ventana del Cielo).
        // ================================================================

        /// <summary>¿Alguna arma de las del peldaño está pagada en el
        /// banco? (devorada en CUALQUIER momento — retroactivo).</summary>
        private static bool AlgunaArmaEnElBanco(
            Globals.AlmaEternoGlobalItem alma, int[] armas)
        {
            if (alma == null || armas == null) return false;
            for (int i = 0; i < armas.Length; i++)
                if (Globals.AlmaEternoGlobalItem.BancoItemDisponible(alma, armas[i]) >= 1)
                    return true;
            return false;
        }

        /// <summary>¿Algún grupo de materiales del peldaño está pago en el
        /// banco? (la suma de sus alternativas alcanza la cuota).</summary>
        private static bool MaterialPagadoEnElBanco(
            Globals.AlmaEternoGlobalItem alma, int n, int[][] gruposMat)
        {
            if (alma == null || gruposMat == null) return false;
            for (int g = 0; g < gruposMat.Length; g++)
            {
                int cuota = Systems.DespertarGrimorio.CantidadMateriales(n, g);
                if (Globals.AlmaEternoGlobalItem
                    .BancoGrupoDisponible(alma, gruposMat[g]) >= cuota)
                    return true;
            }
            return false;
        }

        /// <summary>EL TEXTO DEL PODER de un nivel (el hito que abre ·
        /// null si es un peldaño de solo caza). v6.68 — EL MICROPOTENCIA
        /// (✧) MURIÓ con la letra nueva: los niveles llanos vuelven a
        /// ser llanos («eliminalo LOS MICROPODERES»).</summary>
        private static string TextoDelPoder(int n)
        {
            // EL HITO (el camino de siempre — la descripción corta).
            var camino = Systems.DespertarGrimorio.ElCaminoDelDespertar;
            for (int i = 0; i < camino.Length; i++)
                if (camino[i].Nivel == n)
                    return "★ " + Language.GetTextValue(camino[i].Clave);

            // LA PUERTA DE CAZA (nivel de solo jefe — sin hito).
            if (Systems.DespertarGrimorio.EsNivelDeCaza(n))
                return "⚔ " + Language.GetTextValue(Clave("CieloPuertaDeCaza"));

            return null;
        }

        /// <summary>¿LLEVA ALGUNA de las alternativas del peldaño en la bolsa?
        /// (y ¿ya CORRUPTA? — la que cumple de verdad).</summary>
        private static bool LlevaAlgunaAlternativa(Player yo, int[] armas, out bool corrupta)
        {
            corrupta = false;
            bool lleva = false;
            for (int k = 0; k < 58; k++)
            {
                Item it = yo.inventory[k];
                if (it == null || it.IsAir) continue;
                for (int a = 0; a < armas.Length; a++)
                {
                    if (it.type != armas[a]) continue;
                    lleva = true;
                    try
                    {
                        var sello = it.GetGlobalItem<Globals.CorruptoGlobalItem>();
                        if (sello != null && sello.Corrompido) corrupta = true;
                    }
                    catch { }
                }
            }
            return lleva;
        }

        /// <summary>¿CUÁNTOS lleva de este item en la bolsa?</summary>
        private static int CuentaEnLaBolsa(Player yo, int tipo)
        {
            int total = 0;
            for (int k = 0; k < 58; k++)
            {
                Item it = yo.inventory[k];
                if (it != null && !it.IsAir && it.type == tipo)
                    total += it.stack;
            }
            return total;
        }
    }
}
