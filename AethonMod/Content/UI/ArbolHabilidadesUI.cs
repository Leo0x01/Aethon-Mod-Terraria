using System;
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
    //  ArbolHabilidadesUI — v6.86 — LA VENTANA DEL ÁRBOL DE HABILIDADES.
    //
    //  LA LETRA: «recuerdas cuando estabamos intentando crear un arbol
    //  de habilidades y no lo logramos, bueno los mods de referencia
    //  tienen el arbol de habilidades en una ventana
    //  funcional justo como lo necesitabamos, asi que investigalo y
    //  crea un item que la use».
    //
    //  EL INFORME R6 (repos de referencia clonados): la ventana
    //  funcional son SIETE lecciones (§d.5) que aquí viven todas:
    //   1. La capa antes de "Vanilla: Mouse Text" (la de la casa).
    //   2. BLOQUEAR el clic del juego sobre la ventana (mouseInterface).
    //   3. LAS CADENAS antes que los nodos (eslabones de 16 px rotados
    //      por la pendiente — gris/pulso/blanco por estado).
    //   4. Paneo arrastrando + zoom con la rueda (0,55–1,7).
    //   5. Cerrar con Esc y con la X.
    //   6. Puntos vivos + reset con DOBLE clic de confirmación (PoT).
    //   7. El refund SOLO si el árbol queda CONECTADO al ancla (BFS —
    //      FullyLinkedWithout de PoT).
    //  La abre EL ITEM (El Códice de Habilidades — clic derecho, el
    //  patrón GrimoireItem de PoT) y los niveles viajan POR JUGADOR.
    // ======================================================================
    public class ArbolHabilidadesUI : ModSystem
    {
        // === EL ESTADO (cliente puro — el patrón El Cielo) ===
        private static bool _abierta;
        private static Vector2 _pan = Vector2.Zero;
        private static float _zoom = 1f;
        private static int _ultimaRueda = int.MinValue;
        private static bool _ultimoClicIzq, _ultimoClicDer, _ultimoEsc;
        private static Vector2 _ultimaPosRaton = Vector2.Zero;
        private static bool _arrastrando;
        private static int _resetArmado;    // ticks restantes del "¿seguro?" (PoT: 2 clics)

        /// <summary>¿La ventana está abierta?</summary>
        public static bool Abierta => _abierta;

        /// <summary>EL ÁRBOL SE ABRE (o se cierra) — lo llama el ítem.</summary>
        public static void Alternar()
        {
            _abierta = !_abierta;
            if (!_abierta) { _pan = Vector2.Zero; _zoom = 1f; _resetArmado = 0; }
        }

        public override void OnWorldUnload() { _abierta = false; _pan = Vector2.Zero; _zoom = 1f; }
        public override void Unload() { _abierta = false; _pan = Vector2.Zero; _zoom = 1f; }

        // ================================================================
        //  LA CAPA (la de la casa: antes de "Vanilla: Mouse Text")
        // ================================================================
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
            if (idx != -1)
            {
                layers.Insert(idx, new LegacyGameInterfaceLayer(
                    "AethonMod: El Árbol de Habilidades",
                    DibujarElArbol, InterfaceScaleType.UI));
            }
        }

        // ================================================================
        //  EL GRAFO (las SIETE lecciones usan esto) — el árbol de la casa:
        //  el ancla (0, gratis y siempre viva) + tres ramas (Filo/Cuerpo/
        //  Mente) + sus hojas + el KEYSTONE (el Eco de Aethon).
        // ================================================================
        private static readonly Vector2[] Posiciones =
        {
            new Vector2(0, -190),      // 0 EL ANCLA
            new Vector2(-180, -70),    // 1 Filo del Eco
            new Vector2(0, -70),       // 2 Cuerpo de Tinta
            new Vector2(180, -70),     // 3 Mente Despierta
            new Vector2(-280, 60),     // 4 Colmillo Crítico
            new Vector2(-90, 60),      // 5 Plumas Ligeras
            new Vector2(90, 60),       // 6 Piel de Tinta
            new Vector2(280, 60),      // 7 Tinta Barata
            new Vector2(90, 190),      // 8 Corazón de Tinta
            new Vector2(280, 190),     // 9 EL ECO DE AETHON (keystone)
        };

        private static readonly int[] Maximos = { 1, 3, 3, 3, 3, 2, 3, 3, 2, 1 };

        private static readonly (int a, int b)[] Aristas =
        {
            (0, 1), (0, 2), (0, 3),
            (1, 4),        // Filo → Colmillo
            (2, 5), (2, 6),// Cuerpo → Plumas / Piel
            (3, 7),        // Mente → Tinta Barata
            (6, 8),        // Piel → Corazón
            (7, 9),        // Tinta Barata → EL ECO (keystone)
        };

        /// <summary>¿Tiene este nodo un VECINO ACTIVO? (la regla de
        /// asignación de PoT: se crece desde lo vivo).</summary>
        private static bool VecinoActivo(int nodo, global::AethonMod.Content.Players.SkillTreePlayer st)
        {
            for (int e = 0; e < Aristas.Length; e++)
            {
                int otro = -1;
                if (Aristas[e].a == nodo) otro = Aristas[e].b;
                else if (Aristas[e].b == nodo) otro = Aristas[e].a;
                else continue;
                if (otro >= 0 && st.Niveles[otro] > 0) return true;
            }
            return false;
        }

        /// <summary>EL REFUND SIN ROMPER EL ÁRBOL (FullyLinkedWithout de
        /// PoT): BFS desde el ancla saltando al nodo que sale — si algún
        /// nodo ACTIVO queda huérfano, el refund se niega.</summary>
        private static bool ConectadoSin(int fuera, global::AethonMod.Content.Players.SkillTreePlayer st)
        {
            var visto = new bool[st.Niveles.Length];
            visto[0] = true;
            var cola = new Queue<int>();
            cola.Enqueue(0);
            while (cola.Count > 0)
            {
                int n = cola.Dequeue();
                for (int e = 0; e < Aristas.Length; e++)
                {
                    int otro = -1;
                    if (Aristas[e].a == n) otro = Aristas[e].b;
                    else if (Aristas[e].b == n) otro = Aristas[e].a;
                    else continue;
                    if (otro < 0 || otro == fuera || visto[otro]) continue;
                    if (st.Niveles[otro] > 0) { visto[otro] = true; cola.Enqueue(otro); }
                }
            }
            for (int i = 0; i < st.Niveles.Length; i++)
                if (i != fuera && st.Niveles[i] > 0 && !visto[i]) return false;
            return true;
        }

        // ================================================================
        //  LA PALETA (la de la casa) + LAS TEXTURAS
        // ================================================================
        private static readonly Color TintaFondo = new(12, 8, 20);
        private static readonly Color TintaPanel = new(18, 12, 28);
        private static readonly Color TintaVivo = new(196, 138, 255);
        private static readonly Color TintaDorado = new(255, 215, 120);
        private static readonly Color TintaVerde = new(120, 235, 150);
        private static readonly Color TintaTexto = new(232, 216, 255);
        private static readonly Color TintaApagado = new(120, 120, 140);
        private static readonly Color TintaRojo = new(255, 120, 120);
        private static string Clave(string c) => "Mods.AethonMod.Arbol." + c;

        /// <summary>EL RASTERIZADOR CON TIJERAS (el clip del lienzo del
        /// árbol — la lección de El Cielo).</summary>
        private static readonly RasterizerState TijerasUI = new RasterizerState
        {
            CullMode = CullMode.None,
            ScissorTestEnable = true,
        };

        private static bool DibujarElArbol()
        {
            try
            {
                if (!_abierta || Main.gameMenu || Main.netMode == NetmodeID.Server)
                    return true;

                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active) { _abierta = false; return true; }
                global::AethonMod.Content.Players.SkillTreePlayer st = yo.GetModPlayer<global::AethonMod.Content.Players.SkillTreePlayer>();
                if (st == null) return true;
                if (st.Niveles == null || st.Niveles.Length < global::AethonMod.Content.Players.ArbolUtil.TOTAL_NODOS)
                    return true;
                if (st.Niveles[0] < 1) st.Niveles[0] = 1;   // el ancla siempre viva

                SpriteBatch sb = Main.spriteBatch;
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Texture2D cadena = ModContent.Request<Texture2D>(
                    "AethonMod/Content/UI/ArbolHabilidades_Cadena").Value;
                Texture2D nodoTex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/UI/ArbolHabilidades_Nodo").Value;
                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                float t = Main.GlobalTimeWrappedHourly;

                // === LA GEOMETRÍA (centrada, un poco arriba) ===
                int w = 920, h = 620;
                if (Main.screenWidth < w + 20) w = Main.screenWidth - 20;
                if (Main.screenHeight < h + 20) h = Main.screenHeight - 20;
                int x = Main.screenWidth / 2 - w / 2;
                int y = Main.screenHeight / 2 - h / 2;

                // EL FONDO + el marco de la casa.
                sb.Draw(pixel, new Rectangle(x - 3, y - 3, w + 6, h + 6), TintaFondo * 0.94f);
                Marco(sb, pixel, new Rectangle(x - 3, y - 3, w + 6, h + 6), TintaVivo * 0.85f);
                sb.Draw(pixel, new Rectangle(x, y, w, h), TintaFondo * 0.96f);

                // EL TITULAR + LOS PUNTOS (el contador de PoT).
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("Titulo")),
                    new Vector2(x + w / 2f, y + 22f), TintaVivo, 1.1f, 0.5f, 0.4f);
                int libres = st.PuntosLibres();
                int totales = st.PuntosTotales();
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("Puntos"),
                        libres, libres + st.PuntosGastados(), totales),
                    new Vector2(x + w - 18f, y + 22f), TintaDorado, 0.95f, 1f, 0.4f);

                // ============================================================
                //  EL LIENZO (el clip con tijeras — la lección de El Cielo)
                // ============================================================
                Rectangle lienzo = new Rectangle(x + 14, y + 48, w - 28, h - 118);
                sb.Draw(pixel, lienzo, TintaPanel * 0.95f);
                Marco(sb, pixel, lienzo, TintaVivo * 0.30f);
                Rectangle scissorViejo = sb.GraphicsDevice.ScissorRectangle;
                try
                {
                    sb.End();
                    sb.GraphicsDevice.ScissorRectangle = lienzo;
                    sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        Main.DefaultSamplerState, DepthStencilState.None, TijerasUI,
                        null, Main.UIScaleMatrix);

                    // === LA ZOOM con la rueda (lección 4: 0,55–1,7). ===
                    int rueda = Mouse.GetState().ScrollWheelValue;
                    if (lienzo.Contains(Main.mouseX, Main.mouseY) && _ultimaRueda != int.MinValue)
                        _zoom = MathHelper.Clamp(_zoom + (rueda - _ultimaRueda) / 1200f, 0.55f, 1.7f);
                    _ultimaRueda = rueda;

                    // EL CENTRO del lienzo + pan/zoom.
                    Vector2 centro = lienzo.Center.ToVector2() + _pan;
                    float zz = _zoom;

                    // === LA FASE 1 — LAS CADENAS (lección 3: antes que
                    //     los nodos, eslabones de 16·zoom rotados). ===
                    for (int e = 0; e < Aristas.Length; e++)
                    {
                        int a = Aristas[e].a, b = Aristas[e].b;
                        Vector2 pa = centro + Posiciones[a] * zz;
                        Vector2 pb = centro + Posiciones[b] * zz;
                        bool activaA = st.Niveles[a] > 0, activaB = st.Niveles[b] > 0;
                        bool asignable = activaA && st.Niveles[b] < Maximos[b] && libres > 0;
                        Color cCadena;
                        if (activaA && activaB) cCadena = Color.White;
                        else if (asignable)
                            cCadena = Color.Lerp(new Color(120, 120, 140), Color.White,
                                (float)Math.Sin(t * 3.2f) * 0.5f + 0.5f) * 0.9f;
                        else cCadena = new Color(120, 120, 140) * 0.55f;

                        float largo = Vector2.Distance(pa, pb);
                        int eslabones = Math.Max(1, (int)(largo / (16f * zz)));
                        float paso = 1f / eslabones;
                        for (int k = 0; k <= eslabones; k++)
                        {
                            Vector2 pos = Vector2.Lerp(pa, pb, paso * k);
                            sb.Draw(cadena, pos, null, cCadena,
                                pa.DirectionTo(pb).ToRotation() + MathHelper.PiOver2,
                                cadena.Size() * 0.5f, zz * 0.9f, SpriteEffects.None, 0f);
                        }
                    }

                    // === LA FASE 2 — LOS NODOS (el ancla + la banda). ===
                    int hovered = -1;
                    for (int n = 0; n < Posiciones.Length; n++)
                    {
                        Vector2 pos = centro + Posiciones[n] * zz;
                        float radio = 30f * zz;
                        Rectangle area = new Rectangle((int)(pos.X - radio), (int)(pos.Y - radio),
                            (int)(radio * 2f), (int)(radio * 2f));
                        bool hover = area.Contains(Main.mouseX, Main.mouseY);
                        if (hover) hovered = n;

                        int nivel = st.Niveles[n];
                        bool asignable = n > 0 && nivel < Maximos[n] &&
                            libres > 0 && VecinoActivo(n, st);
                        Color tinte;
                        float brillo;
                        if (n == 0 || nivel > 0) { tinte = Color.White; brillo = 1f; }
                        else if (asignable)
                        {
                            brillo = (float)Math.Sin(t * 3.2f) * 0.5f + 0.5f;
                            tinte = Color.Lerp(new Color(150, 150, 165), TintaDorado, brillo);
                        }
                        else { tinte = new Color(120, 120, 140) * 0.6f; brillo = 0f; }

                        // EL GLOW de lo vivo (el keystone, más grande).
                        if (n == 0 || nivel > 0)
                            sb.Draw(glow, pos, null, (n == 9 ? TintaDorado : TintaVivo) * 0.30f,
                                0f, glow.Size() * 0.5f, (n == 9 ? 2.4f : 1.8f) * zz, SpriteEffects.None, 0f);
                        sb.Draw(nodoTex, pos, null, tinte, 0f, nodoTex.Size() * 0.5f,
                            (n == 9 ? 1.15f : 1f) * zz, SpriteEffects.None, 0f);

                        // EL CONTADOR de nivel (PoT: «x/y» bajo el nodo).
                        if (n > 0 && Maximos[n] > 1)
                            Utils.DrawBorderString(sb, $"{nivel}/{Maximos[n]}",
                                new Vector2(pos.X, pos.Y + 34f * zz),
                                nivel > 0 ? TintaTexto : TintaApagado, 0.8f * MathHelper.Max(0.8f, zz), 0.5f, 0.4f);
                        else if (n > 0 && nivel > 0)
                            Utils.DrawBorderString(sb, "✓", new Vector2(pos.X, pos.Y + 34f * zz),
                                TintaVerde, 0.9f, 0.5f, 0.4f);
                    }
                    if (hovered < 0 && lienzo.Contains(Main.mouseX, Main.mouseY))
                        yo.mouseInterface = true;   // el vacío también es de la UI (el arrastre)

                    sb.End();
                    sb.GraphicsDevice.ScissorRectangle = scissorViejo;
                    sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        Main.DefaultSamplerState, DepthStencilState.None,
                        RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
                }
                catch { }

                // ============================================================
                //  LA INTERACCIÓN (lección 2: el clic es DE LA VENTANA —
                //  mouseInterface lo come TODO; el juego no usa el item).
                // ============================================================
                bool clicIzq = Main.mouseLeft && !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                bool clicDer = Main.mouseRight && !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                bool clicIzqNuevo = clicIzq && !_ultimoClicIzq;
                bool clicDerNuevo = clicDer && !_ultimoClicDer;

                // EL HOVER del nodo (recién calculado dentro del clip): el
                // tooltip al pie + la asignación/devolución.
                int hoverFinal = -1;
                for (int n = 0; n < Posiciones.Length; n++)
                {
                    Vector2 pos = lienzo.Center.ToVector2() + _pan + Posiciones[n] * _zoom;
                    float radio = 34f * _zoom;
                    if (new Rectangle((int)(pos.X - radio), (int)(pos.Y - radio),
                        (int)(radio * 2f), (int)(radio * 2f)).Contains(Main.mouseX, Main.mouseY))
                    { hoverFinal = n; break; }
                }

                if (hoverFinal >= 0)
                {
                    yo.mouseInterface = true;
                    // LA ASIGNACIÓN (clic izquierdo: +1 si hay puntos y
                    // vecino vivo — PoT CanAllocate).
                    if (clicIzqNuevo && hoverFinal > 0 &&
                        st.Niveles[hoverFinal] < Maximos[hoverFinal] &&
                        libres > 0 && VecinoActivo(hoverFinal, st))
                    {
                        st.Niveles[hoverFinal]++;
                        libres--;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.ResearchComplete);
                    }
                    // LA DEVOLUCIÓN (clic derecho: −1 si el árbol queda
                    // CONECTADO — el refund de PoT).
                    else if (clicDerNuevo && hoverFinal > 0 && st.Niveles[hoverFinal] > 0 &&
                        ConectadoSin(hoverFinal, st))
                    {
                        st.Niveles[hoverFinal]--;
                        libres++;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.DD2_WitherBeastDeath);
                    }
                }

                // EL PANEO (arrastrar el vacío — lección 4).
                if (lienzo.Contains(Main.mouseX, Main.mouseY))
                {
                    Vector2 raton = new Vector2(Main.mouseX, Main.mouseY);
                    if (clicIzqNuevo && hoverFinal < 0) _arrastrando = true;
                    if (!clicIzq) _arrastrando = false;
                    if (_arrastrando && clicIzq)
                        _pan += raton - _ultimaPosRaton;
                    _ultimaPosRaton = raton;
                }

                // ============================================================
                //  EL TOOLTIP DEL NODO (al pie del lienzo — lección de
                //  Umbra: nombre + descripción + nivel).
                // ============================================================
                Rectangle pie = new Rectangle(x + 14, y + h - 64, w - 28, 56);
                sb.Draw(pixel, pie, TintaPanel * 0.95f);
                Marco(sb, pixel, pie, TintaVivo * 0.25f);
                if (hoverFinal >= 0)
                {
                    string nombre = Language.GetTextValue(Clave("Nodo" + hoverFinal));
                    string desc = Language.GetTextValue(Clave("Desc" + hoverFinal));
                    Utils.DrawBorderString(sb, nombre, new Vector2(pie.X + 14f, pie.Y + 12f),
                        hoverFinal == 9 ? TintaDorado : TintaVivo, 1.0f);
                    Utils.DrawBorderString(sb, desc, new Vector2(pie.X + 14f, pie.Y + 30f),
                        TintaApagado, 0.85f);
                    Utils.DrawBorderString(sb,
                        Language.GetTextValue(Clave("NivelDe"), st.Niveles[hoverFinal], Maximos[hoverFinal]),
                        new Vector2(pie.Right - 14f, pie.Y + 12f), TintaTexto, 0.9f, 1f, 0.4f);
                }
                else
                {
                    Utils.DrawBorderString(sb, Language.GetTextValue(Clave("Ayuda")),
                        new Vector2(pie.X + 14f, pie.Y + 20f), TintaApagado, 0.85f);
                }

                // === EL RESET (lección 6: DOBLE clic de confirmación —
                //     PoT: 120 ticks con el "¿seguro?"). ===
                Rectangle btnReset = new Rectangle(x + 18, y + h - 48, 170, 36);
                bool hoverReset = btnReset.Contains(Main.mouseX, Main.mouseY);
                if (hoverReset) yo.mouseInterface = true;
                if (_resetArmado > 0) _resetArmado--;
                Color cReset = _resetArmado > 0 ? TintaRojo : (hoverReset ? TintaDorado : TintaApagado);
                sb.Draw(pixel, btnReset, (_resetArmado > 0 ? TintaRojo : TintaDorado) * 0.22f);
                Marco(sb, pixel, btnReset, cReset * 0.8f);
                Utils.DrawBorderString(sb,
                    _resetArmado > 0 ? Language.GetTextValue(Clave("ResetSeguro"))
                                     : Language.GetTextValue(Clave("Reset")),
                    new Vector2(btnReset.Center.X, btnReset.Center.Y), cReset, 0.95f, 0.5f, 0.5f);
                if (hoverReset && clicIzqNuevo)
                {
                    if (_resetArmado > 0)
                    {
                        for (int n = 1; n < st.Niveles.Length; n++) st.Niveles[n] = 0;
                        _resetArmado = 0;
                        Main.NewText(Language.GetTextValue(Clave("ResetHecho")), TintaVerde);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
                    }
                    else _resetArmado = 150;
                }

                // === EL CIERRE (la X + Esc — lección 5). ===
                Rectangle btnCerrar = new Rectangle(x + w - 34, y + 8, 26, 26);
                bool hoverCerrar = btnCerrar.Contains(Main.mouseX, Main.mouseY);
                if (hoverCerrar) yo.mouseInterface = true;
                sb.Draw(pixel, btnCerrar, (hoverCerrar ? TintaRojo : TintaApagado) * 0.25f);
                Utils.DrawBorderString(sb, "X", new Vector2(btnCerrar.Center.X, btnCerrar.Center.Y),
                    hoverCerrar ? TintaRojo : TintaApagado, 1.0f, 0.5f, 0.5f);

                bool escAhora = Keyboard.GetState().IsKeyDown(Keys.Escape);
                if ((hoverCerrar && clicIzqNuevo) || (escAhora && !_ultimoEsc))
                { _abierta = false; _pan = Vector2.Zero; _zoom = 1f; _resetArmado = 0; }
                _ultimoEsc = escAhora;

                _ultimoClicIzq = clicIzq;
                _ultimoClicDer = clicDer;
                return true;
            }
            catch { return true; }
        }

        /// <summary>EL MARCO de la casa (el doble filete de El Cielo).</summary>
        private static void Marco(SpriteBatch sb, Texture2D pixel, Rectangle r, Color c)
        {
            sb.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, 2), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Y + r.Height - 2, r.Width, 2), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Y, 2, r.Height), c);
            sb.Draw(pixel, new Rectangle(r.X + r.Width - 2, r.Y, 2, r.Height), c);
        }
    }
}
