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
using Terraria.ModLoader.UI;
using AethonMod.Content.Skills;
using AethonMod.Content.Weapons.Escriba;

namespace AethonMod.Content.UI
{
    // ======================================================================
    //  v6.52.0 — EL ÁRBOL DE INSCRIPCIONES (la UI del arma viva).
    //
    //  LA LETRA: «ese mod propone que la conciencia de un NPC está en
    //  diferentes armas y además hay un árbol de habilidades completo y
    //  funcional para cada uno. Recuerda que intentamos crear algo así y
    //  no funcionó». ESTE es el árbol funcional: ábrelo con [P] mientras
    //  sostienes LA PLUMA CONSCIENTE.
    //
    //  LA RECETA (R1 §2.1 — el OniMeiUI de Calamity Overhaul, mapeado a
    //  la capa de interfaz de la casa como el HUD de XP):
    //   · TRES COLUMNAS (Filigrana / Verso / Colofón — una por ranura).
    //   · Nodos con TRES estados: EQUIPADA (anillo blanco + latido),
    //     DESBLOQUEADA (violeta, se puede grabar con clic) y BLOQUEADA
    //     (silueta oscura + «n/N» — la regla CWR: los nodos bloqueados
    //     muestran PROGRESO, nunca efectos).
    //   · Panel de lectura abajo: nombre, descripción, PODER (verde) y
    //     CARGA (rojo) — todo poder lleva su carga.
    //   · La fila de HECHOS con su progreso (la vía del desbloqueo).
    // ======================================================================
    public class ArbolInscripcionesSistema : ModSystem
    {
        public static ModKeybind TeclaArbol;

        // === EL ESTADO DE LA VENTANA (cliente) ===
        private static bool _abierto;
        private static InscripcionDefinicion _hover;
        private static InscripcionDefinicion _seleccion;

        // === LA GEOMETRÍA (una ventana centrada de 680×540) ===
        private const int PanelW = 680;
        private const int PanelH = 540;
        private const int ColumnasX = 128;      // separación entre columnas
        private const int NodoR = 15;           // radio del círculo
        private const int NodoPaso = 62;        // paso vertical

        public override void Load()
        {
            // LA TECLA: P por defecto («Pluma») — configurable en Ajustes.
            TeclaArbol = KeybindLoader.RegisterKeybind(Mod, "EscribaArbol", "P");
        }

        public override void Unload()
        {
            TeclaArbol = null;
            _abierto = false;
            _hover = null;
            _seleccion = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.gameMenu) { _abierto = false; return; }
            Player p = Main.LocalPlayer;
            if (p == null || !p.active) { _abierto = false; return; }

            // LA CONDICIÓN DE VIDA: solo con la pluma EN LA MANO.
            bool conPluma = p.HeldItem != null && !p.HeldItem.IsAir &&
                p.HeldItem.type == ModContent.ItemType<PlumaConsciente>();
            if (!conPluma) { _abierto = false; _hover = null; return; }

            if (TeclaArbol != null && TeclaArbol.JustPressed)
            {
                _abierto = !_abierto;
                _seleccion = null;
                Terraria.Audio.SoundEngine.PlaySound(_abierto ? SoundID.MenuOpen : SoundID.MenuClose);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // Justo ENCIMA del inventario: el árbol es su propia pantalla.
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
            if (idx == -1) idx = layers.FindIndex(l => l.Name == "Vanilla: Hotbar");
            if (idx != -1)
            {
                layers.Insert(idx, new LegacyGameInterfaceLayer(
                    "AethonMod: Arbol Inscripciones",
                    () => { DibujarArbol(); return true; },
                    InterfaceScaleType.UI));
            }
        }

        // ------------------------------------------------------------------
        //  EL DIBUJO COMPLETO (todo con SpriteBatch de UI — este NO es un
        //  PreDraw del mundo: aquí el lote de UI es de la casa)
        // ------------------------------------------------------------------
        private static void DibujarArbol()
        {
            try
            {
                if (!_abierto || Main.gameMenu) return;
                Player p = Main.LocalPlayer;
                if (p == null || !p.active) return;

                InscripcionJugador pj = InscripcionJugador.De(p);
                Item pluma = p.HeldItem;
                InscripcionItem datos = InscripcionItem.De(pluma);
                if (pj == null || datos == null) return;

                // CON LA VENTANA ABIERTA el clic NO golpea con el arma.
                p.mouseInterface = true;

                SpriteBatch sb = Main.spriteBatch;
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                var font = FontAssets.MouseText.Value;

                int cx = Main.screenWidth / 2;
                int cy = Main.screenHeight / 2;
                var panel = new Rectangle(cx - PanelW / 2, cy - PanelH / 2, PanelW, PanelH);

                // === EL VELO (todo lo demás se apaga) ===
                sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                    new Color(8, 5, 14) * 0.82f);

                // === EL PANEL ===
                sb.Draw(pixel, panel, new Color(24, 16, 38) * 0.97f);
                // EL BORDE (doble línea: violeta fuera, tinta dentro).
                Borde(sb, pixel, panel, 2, new Color(140, 100, 210));
                Borde(sb, pixel, new Rectangle(panel.X + 4, panel.Y + 4, panel.Width - 8, panel.Height - 8),
                    1, new Color(70, 50, 105));

                // === EL TÍTULO + EL NIVEL DEL ARMA ===
                Utils.DrawBorderStringBig(sb,
                    Language.GetTextValue("Mods.AethonMod.UI.Arbol.Titulo"),
                    new Vector2(cx, panel.Y + 34), new Color(232, 216, 255), 0.85f, 0.5f, 0.4f);
                string nivel = Language.GetTextValue("Mods.AethonMod.UI.Arbol.Nivel", datos.Nivel);
                Utils.DrawBorderString(sb, nivel, new Vector2(cx, panel.Y + 62),
                    new Color(255, 226, 160), 1f, 0.5f);

                // === LAS TRES COLUMNAS ===
                string[] nombresRanura =
                {
                    Language.GetTextValue("Mods.AethonMod.UI.Arbol.Ranura0"),
                    Language.GetTextValue("Mods.AethonMod.UI.Arbol.Ranura1"),
                    Language.GetTextValue("Mods.AethonMod.UI.Arbol.Ranura2"),
                };

                _hover = null;
                Vector2 origen = new Vector2(cx - ColumnasX, panel.Y + 116);
                for (int col = 0; col < 3; col++)
                {
                    float x = origen.X + col * ColumnasX;
                    Utils.DrawBorderString(sb, nombresRanura[col], new Vector2(x, panel.Y + 96),
                        new Color(190, 170, 230), 1f, 0.5f);

                    var deLaColumna = NodosDeRanura((RanuraInscripcion)col);
                    for (int i = 0; i < deLaColumna.Count; i++)
                    {
                        var def = deLaColumna[i];
                        Vector2 centroNodo = new Vector2(x, origen.Y + i * NodoPaso);

                        // LA CONEXIÓN con el nodo de arriba (la espina del árbol).
                        if (i > 0)
                            sb.Draw(pixel,
                                new Rectangle((int)x - 1, (int)(centroNodo.Y - NodoPaso + NodoR), 2, NodoPaso - NodoR * 2),
                                new Color(90, 66, 135) * 0.8f);

                        bool desbloqueada = pj.Biblioteca.Contains(def.Clave);
                        bool equipada = datos.Equipada((int)def.Ranura, def.Clave);
                        bool encima = Vector2.Distance(centroNodo, new Vector2(Main.mouseX, Main.mouseY)) <= NodoR + 3f;
                        if (encima) _hover = def;

                        // EL NODO: círculo por glow (aditivo simulado con alfa).
                        Color relleno = equipada ? new Color(240, 232, 255)
                                    : desbloqueada ? new Color(128, 84, 210)
                                    : new Color(40, 32, 58);
                        float pulso = equipada ? 1f + 0.08f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3.5f) : 1f;
                        sb.Draw(glow, centroNodo, null, relleno * (equipada ? 0.95f : 0.65f) * pulso,
                            0f, glow.Size() * 0.5f, (NodoR * 2.2f) / glow.Width, SpriteEffects.None, 0f);

                        // EL ANILLO: blanco vivo si equipada, tenue si libre,
                        // DISCONTINUO si bloqueada (4 arcos = 4 rayitas).
                        if (desbloqueada || equipada)
                        {
                            Anillo(sb, pixel, centroNodo, NodoR,
                                equipada ? new Color(255, 250, 240) : new Color(168, 132, 226));
                        }
                        else
                        {
                            for (int a = 0; a < 4; a++)
                            {
                                float ang = a * MathHelper.PiOver2 + 0.35f;
                                Arco(sb, pixel, centroNodo, NodoR, ang, 0.55f, new Color(105, 88, 138));
                            }
                        }

                        // EL PROGRESO del bloqueado (la regla CWR: «n/N», no efectos).
                        if (!desbloqueada && !equipada)
                        {
                            string prog = ProgresoDe(pj, def);
                            if (prog != null)
                                Utils.DrawBorderString(sb, prog, centroNodo + new Vector2(0, -2),
                                    new Color(150, 140, 175), 0.72f, 0.5f, 0.5f);
                        }

                        // EL CLICK (consume el clic: el arma NO se usa).
                        if (encima && Main.mouseLeft && Main.mouseLeftRelease)
                        {
                            Main.mouseLeftRelease = false;
                            Main.mouseLeft = false;
                            if (equipada)
                            {
                                datos.Ranuras[(int)def.Ranura] = null;   // DESGRABAR
                                Terraria.Audio.SoundEngine.PlaySound(
                                    SoundID.MenuTick.WithPitchOffset(-0.3f));
                            }
                            else if (desbloqueada)
                            {
                                datos.Grabar(pluma, def.Clave);          // GRABAR
                                _seleccion = def;
                                Terraria.Audio.SoundEngine.PlaySound(
                                    SoundID.MenuTick.WithPitchOffset(0.35f));
                            }
                        }
                    }
                }

                // === LA LECTURA (el panel de abajo: lo que señalas) ===
                var leido = _hover ?? _seleccion;
                var cajaLectura = new Rectangle(panel.X + 24, panel.Y + PanelH - 172, PanelW - 48, 96);
                sb.Draw(pixel, cajaLectura, new Color(16, 10, 26) * 0.9f);
                Borde(sb, pixel, cajaLectura, 1, new Color(80, 58, 120));
                if (leido != null)
                {
                    bool desbloqueada = pj.Biblioteca.Contains(leido.Clave);
                    bool equipada = datos.Equipada((int)leido.Ranura, leido.Clave);
                    if (desbloqueada || equipada)
                    {
                        Utils.DrawBorderStringBig(sb, leido.Nombre,
                            new Vector2(cajaLectura.X + 14, cajaLectura.Y + 10),
                            equipada ? new Color(240, 232, 255) : new Color(196, 170, 240), 0.72f);
                        Utils.DrawBorderString(sb, leido.Descripcion,
                            new Vector2(cajaLectura.X + 14, cajaLectura.Y + 34), new Color(200, 192, 215), 0.9f);
                        Utils.DrawBorderString(sb, leido.Poder,
                            new Vector2(cajaLectura.X + 14, cajaLectura.Y + 56), new Color(140, 230, 160), 0.9f);
                        Utils.DrawBorderString(sb, leido.Carga,
                            new Vector2(cajaLectura.X + 14, cajaLectura.Y + 74), new Color(240, 140, 140), 0.9f);
                    }
                    else
                    {
                        // BLOQUEADA: nombre oculto + la PISTA (la regla CWR).
                        Utils.DrawBorderStringBig(sb, "???",
                            new Vector2(cajaLectura.X + 14, cajaLectura.Y + 10),
                            new Color(120, 108, 145), 0.72f);
                        Utils.DrawBorderString(sb, PistaDe(pj, leido),
                            new Vector2(cajaLectura.X + 14, cajaLectura.Y + 40), new Color(185, 175, 205), 0.9f);
                    }
                }
                else
                {
                    Utils.DrawBorderString(sb,
                        Language.GetTextValue("Mods.AethonMod.UI.Arbol.Pista"),
                        new Vector2(cajaLectura.X + 14, cajaLectura.Y + 38), new Color(170, 158, 195), 0.9f);
                }

                // === LA FILA DE HECHOS (la vía de desbloqueo, con progreso) ===
                int hx = panel.X + 24;
                int hy = panel.Y + PanelH - 64;
                for (int i = 0; i < HechosCatalogo.Todos.Length; i++)
                {
                    var h = HechosCatalogo.Todos[i];
                    int hecho = pj.ContarHecho(h);
                    bool listo = hecho >= h.Objetivo;
                    string texto = h.Nombre + " " + (listo ? "✔" : hecho + "/" + h.Objetivo);
                    Vector2 medido = font.MeasureString(texto) * 0.82f;
                    var cajaH = new Rectangle(hx, hy, (int)medido.X + 18, 26);
                    sb.Draw(pixel, cajaH, listo ? new Color(30, 60, 38) * 0.9f : new Color(20, 14, 32) * 0.9f);
                    Borde(sb, pixel, cajaH, 1, listo ? new Color(90, 200, 120) : new Color(70, 52, 105));
                    Utils.DrawBorderString(sb, texto, new Vector2(hx + 9, hy + 6),
                        listo ? new Color(160, 240, 175) : new Color(185, 175, 205), 0.82f);
                    hx += cajaH.Width + 10;
                    if (hx > panel.X + PanelW - 180) { hx = panel.X + 24; hy += 32; }
                }
            }
            catch { }
        }

        // ------------------------------------------------------------------
        //  LOS AYUDANTES DE DIBUJO
        // ------------------------------------------------------------------

        private static List<InscripcionDefinicion> NodosDeRanura(RanuraInscripcion r)
        {
            var lista = new List<InscripcionDefinicion>();
            foreach (var def in InscripcionDefinicion.Registro)
                if (def.Ranura == r) lista.Add(def);
            return lista;
        }

        /// <summary>El progreso «n/N» de un nodo bloqueado (null si no rastrea).</summary>
        private static string ProgresoDe(InscripcionJugador pj, InscripcionDefinicion def)
        {
            foreach (var h in HechosCatalogo.Todos)
            {
                if (h.ClaveInscripcion != def.Clave) continue;
                int n = Math.Min(pj.ContarHecho(h), h.Objetivo);
                return n + "/" + h.Objetivo;
            }
            return null;
        }

        /// <summary>La PISTA de desbloqueo (qué hecho o qué jefe la regala).</summary>
        private static string PistaDe(InscripcionJugador pj, InscripcionDefinicion def)
        {
            foreach (var h in HechosCatalogo.Todos)
                if (h.ClaveInscripcion == def.Clave)
                    return Language.GetTextValue("Mods.AethonMod.UI.Arbol.PistaHecho", h.Nombre);
            foreach (var (netID, insc) in InscripcionJugador.Regalos)
                if (insc == def.Clave)
                    return Language.GetTextValue("Mods.AethonMod.UI.Arbol.PistaRegalo",
                        Lang.GetNPCNameValue(netID));
            return Language.GetTextValue("Mods.AethonMod.UI.Arbol.PistaNacimiento");
        }

        private static void Borde(SpriteBatch sb, Texture2D pixel, Rectangle r, int grosor, Color c)
        {
            sb.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, grosor), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Bottom - grosor, r.Width, grosor), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Y, grosor, r.Height), c);
            sb.Draw(pixel, new Rectangle(r.Right - grosor, r.Y, grosor, r.Height), c);
        }

        private static void Anillo(SpriteBatch sb, Texture2D pixel, Vector2 c, float radio, Color color)
        {
            const int pasos = 26;
            for (int i = 0; i < pasos; i++)
            {
                float a0 = i / (float)pasos * MathHelper.TwoPi;
                float a1 = (i + 1.25f) / pasos * MathHelper.TwoPi;
                Vector2 p0 = c + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * radio;
                Vector2 p1 = c + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radio;
                LineaUI(sb, pixel, p0, p1, color);
            }
        }

        private static void Arco(SpriteBatch sb, Texture2D pixel, Vector2 c, float radio,
            float angulo, float amplitud, Color color)
        {
            const int pasos = 6;
            for (int i = 0; i < pasos; i++)
            {
                float a0 = angulo + i / (float)pasos * amplitud;
                float a1 = angulo + (i + 1.3f) / pasos * amplitud;
                Vector2 p0 = c + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * radio;
                Vector2 p1 = c + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radio;
                LineaUI(sb, pixel, p0, p1, color);
            }
        }

        private static void LineaUI(SpriteBatch sb, Texture2D pixel, Vector2 a, Vector2 b, Color c)
        {
            Vector2 delta = b - a;
            float len = delta.Length();
            if (len < 0.5f) return;
            sb.Draw(pixel, new Rectangle((int)a.X, (int)a.Y, (int)len, 2), null, c,
                delta.ToRotation(), Vector2.Zero, SpriteEffects.None, 0f);
        }
    }
}
