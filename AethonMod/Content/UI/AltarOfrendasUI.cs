using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.UI
{
    // ======================================================================
    //  AltarOfrendasUI — v6.86 — EL PANEL DEL ALTAR DE OFRENDAS.
    //
    //  EL PATRÓN INVESTIGADO (R5 §3.5-3.8, Heroic Weapons/BSWLmod): el
    //  tile abre una UI con slots y UN BOTÓN que valida, aplica y
    //  CONSUME (TurnToAir) — la Experience Rune Forge es el calco. El
    //  IDIOMA es el de la casa (El Cielo del Despertar): dibujado
    //  directo en una LegacyGameInterfaceLayer antes de "Vanilla: Mouse
    //  Text", sin TileEntity, estado estático de cliente.
    //
    //  LA MESA DEL PANEL:
    //   · EL LIBRO: no hay slot — el panel lee EL GRIMORIO ACTIVO (el
    //     patrón de El Cielo: AlmaEternoGlobalItem.Activo) y muestra
    //     SUS puntos.
    //   · EL RELLENO: UN slot (clic para meter/sacar con el cursor) —
    //     solo acepta lo que pasa el filtro AceptaRelleno (ni requisito
    //     ni corrupto ni sagrado).
    //   · EL BOTÓN OFRENDAR: valida de nuevo, devora la pila ENTERA,
    //     suma sus puntos al alma del libro (RegistrarOfrenda) y lo
    //     dice en verde. Rechazado: rojo + el tooltip del porqué.
    //  EL CIERRE: botón X, Esc, o alejarse del altar (> 280 px) — y
    //  SIEMPRE devuelve lo que quedó en el slot (al cursor o al suelo).
    //  LA PERSISTENCIA (§3.9): lo que quedó en el slot al guardar el
    //  MUNDO viaja con el mundo (SaveWorldData/LoadWorldData).
    // ======================================================================
    public class AltarOfrendasUI : ModSystem
    {
        // === EL ESTADO (cliente puro — el patrón El Cielo) ===
        private static bool _abierta;
        private static int _tileX, _tileY;
        private static Item _relleno = new Item();
        private static bool _ultimoClicIzq;
        private static bool _ultimoEsc;

        /// <summary>¿El panel está abierto?</summary>
        public static bool Abierta => _abierta;

        /// <summary>EL ALTAR SE ABRE (o se cierra): lo llama el RightClick
        /// del tile — con la posición del altar (para la ley de la
        /// distancia).</summary>
        public static void Alternar(int i, int j)
        {
            _abierta = !_abierta;
            _tileX = i; _tileY = j;
            if (!_abierta) DevolverElRelleno();
        }

        public override void OnWorldUnload() { CerrarDeVerdad(); }
        public override void Unload() { CerrarDeVerdad(); _relleno = new Item(); }

        /// <summary>EL CIERRE LIMPIO: nada queda en el slot (al cursor si
        /// está libre, al suelo si no).</summary>
        private static void CerrarDeVerdad()
        {
            if (!_abierta) { _relleno = new Item(); return; }
            _abierta = false;
            DevolverElRelleno();
        }

        private static void DevolverElRelleno()
        {
            try
            {
                if (_relleno == null || _relleno.IsAir) { _relleno = new Item(); return; }
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active) return;
                if (Main.mouseItem != null && Main.mouseItem.IsAir)
                    Main.mouseItem = (Item)_relleno.Clone();
                else
                    yo.QuickSpawnItem(yo.GetSource_Misc("AltarOfrendas"), _relleno, _relleno.stack);
                _relleno = new Item();
            }
            catch { _relleno = new Item(); }
        }

        // ================================================================
        //  LA PERSISTENCIA DEL SLOT (§3.9 — con el MUNDO, como Heroic
        //  Weapons: al guardar, lo que quedó en el panel viaja).
        // ================================================================
        public override void SaveWorldData(TagCompound tag)
        {
            try
            {
                if (_relleno != null && !_relleno.IsAir)
                    tag["altarRelleno"] = ItemIO.Save(_relleno);
            }
            catch { }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            try
            {
                _relleno = new Item();
                if (tag.ContainsKey("altarRelleno"))
                    _relleno = ItemIO.Load(tag.Get<TagCompound>("altarRelleno"));
                if (_relleno == null) _relleno = new Item();
            }
            catch { _relleno = new Item(); }
        }

        // ================================================================
        //  LA CAPA (la de la casa: antes de "Vanilla: Mouse Text")
        // ================================================================
        public override void ModifyInterfaceLayers(System.Collections.Generic.List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
            if (idx != -1)
            {
                layers.Insert(idx, new LegacyGameInterfaceLayer(
                    "AethonMod: El Altar de Ofrendas",
                    DibujarElPanel, InterfaceScaleType.UI));
            }
        }

        // ================================================================
        //  LA PALETA (la de la casa)
        // ================================================================
        private static readonly Color TintaFondo = new(10, 16, 18);
        private static readonly Color TintaPanel = new(16, 28, 30);
        private static readonly Color TintaTeal = new(90, 220, 180);
        private static readonly Color TintaDorado = new(255, 215, 120);
        private static readonly Color TintaTexto = new(220, 250, 245);
        private static readonly Color TintaApagado = new(110, 150, 145);
        private static readonly Color TintaRojo = new(255, 120, 120);
        private static string Clave(string c) => "Mods.AethonMod.Altar." + c;

        private static bool DibujarElPanel()
        {
            try
            {
                if (!_abierta || Main.gameMenu || Main.netMode == NetmodeID.Server)
                    return true;

                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active) { CerrarDeVerdad(); return true; }

                SpriteBatch sb = Main.spriteBatch;
                Texture2D pixel = TextureAssets.MagicPixel.Value;

                // LA LEY DE LA DISTANCIA: el altar manda — sin altar (roto
                // o lejos), el panel se cierra solo.
                Tile t = Framing.GetTileSafely(_tileX, _tileY);
                bool altarVivo = t.HasTile && t.TileType ==
                    (ushort)ModContent.TileType<global::AethonMod.Content.Tiles.AltarOfrendasTile>();
                Vector2 altarCentro = new Vector2(_tileX * 16f + 16f, _tileY * 16f + 8f);
                if (!altarVivo || yo.Center.DistanceSQ(altarCentro) > 280f * 280f)
                { CerrarDeVerdad(); return true; }

                // === LA GEOMETRÍA (a la derecha del inventario) ===
                int w = 380, h = 320;
                int x = Math.Min(Main.screenWidth - w - 10,
                    470 + Math.Max(0, (Main.screenWidth - 1100) / 3));
                int y = Main.screenHeight / 2 - h / 2;
                if (x < 10) x = 10;

                // EL FONDO + el marco de la casa.
                sb.Draw(pixel, new Rectangle(x - 3, y - 3, w + 6, h + 6), TintaFondo * 0.94f);
                Marco(sb, pixel, new Rectangle(x - 3, y - 3, w + 6, h + 6), TintaTeal * 0.80f);
                sb.Draw(pixel, new Rectangle(x, y, w, h), TintaPanel * 0.97f);

                // EL TITULAR.
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("Titulo")),
                    new Vector2(x + w / 2f, y + 22f), TintaTeal, 1.1f, 0.5f, 0.4f);
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("Subtitulo")),
                    new Vector2(x + w / 2f, y + 42f), TintaApagado, 0.75f, 0.5f, 0.4f);

                // === EL LIBRO ACTIVO (sin slot: el patrón de El Cielo) y
                //     sus puntos vivos. ===
                Item libro = Globals.AlmaEternoGlobalItem.Activo(yo);
                var alma = Globals.AlmaEternoGlobalItem.De(libro);
                int puntos = alma?.ProgresoOfrendas ?? 0;
                int credito = alma?.CreditoPuntosEsencias ?? 0;
                if (alma != null)
                {
                    Utils.DrawBorderString(sb, Language.GetTextValue(Clave("PuntosLibro"), puntos),
                        new Vector2(x + w - 16f, y + 22f), TintaDorado, 0.9f, 1f, 0.4f);
                }
                else
                {
                    Utils.DrawBorderString(sb, Language.GetTextValue(Clave("SinLibro")),
                        new Vector2(x + w - 16f, y + 22f), TintaRojo, 0.8f, 1f, 0.4f);
                }

                // === EL SLOT DEL RELLENO (la mesa del panel) ===
                Rectangle slot = new Rectangle(x + 24, y + 86, 52, 52);
                bool hoverSlot = PanelContiene(slot) && !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                sb.Draw(pixel, slot, TintaFondo * 0.9f);
                Marco(sb, pixel, slot, hoverSlot ? TintaTeal : TintaTeal * 0.45f);
                if (_relleno != null && !_relleno.IsAir)
                {
                    Texture2D tex = TextureAssets.Item[_relleno.type].Value;
                    float escala = Math.Min(44f / tex.Width, 44f / tex.Height);
                    sb.Draw(tex, slot.Center.ToVector2(), null, Color.White, 0f,
                        tex.Size() * 0.5f, escala, SpriteEffects.None, 0f);
                    if (_relleno.stack > 1)
                        Utils.DrawBorderString(sb, _relleno.stack.ToString(),
                            new Vector2(slot.Right - 6f, slot.Bottom - 8f), TintaTexto, 0.85f, 1f, 0.85f);
                }
                else
                {
                    Utils.DrawBorderString(sb, Language.GetTextValue(Clave("SlotVacio")),
                        new Vector2(slot.Center.X, slot.Center.Y), TintaApagado * 0.8f, 0.7f, 0.5f, 0.5f);
                }
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("EtiquetaRelleno")),
                    new Vector2(x + 92f, y + 96f), TintaTexto, 0.9f);
                string filtroTxt = Language.GetTextValue(Clave("FiltroRelleno"));
                Utils.DrawBorderString(sb, filtroTxt, new Vector2(x + 92f, y + 118f), TintaApagado, 0.72f);

                // === LOS PUNTOS EN VIVO del relleno del slot (lo que
                //     Ofrendar cobraría AHORA). ===
                int puntosVivos = (_relleno != null && !_relleno.IsAir) ?
                    Systems.DespertarGrimorio.ValorEnPuntos(_relleno) : 0;
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("PuntosVivos"), puntosVivos),
                    new Vector2(x + 92f, y + 158f), TintaDorado, 0.9f);
                if (credito > 0)
                    Utils.DrawBorderString(sb, Language.GetTextValue(Clave("CreditoVivo"), credito),
                        new Vector2(x + 92f, y + 180f), TintaTeal, 0.8f);

                // === EL BOTÓN OFRENDAR (la mesa se sirve) ===
                Rectangle btnOfrendar = new Rectangle(x + 24, y + 214, 160, 42);
                bool puedeOfrendar = puntosVivos > 0 && alma != null &&
                    Systems.DespertarGrimorio.AceptaRelleno(_relleno, yo, libro);
                bool hoverBtn = PanelContiene(btnOfrendar);
                sb.Draw(pixel, btnOfrendar,
                    (hoverBtn && puedeOfrendar ? TintaTeal : TintaTeal * 0.35f) * 0.30f);
                Marco(sb, pixel, btnOfrendar, puedeOfrendar ? TintaTeal : TintaTeal * 0.35f);
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("Ofrendar")),
                    new Vector2(btnOfrendar.Center.X, btnOfrendar.Center.Y),
                    puedeOfrendar ? TintaTexto : TintaApagado, 1.0f, 0.5f, 0.5f);

                // === EL BOTÓN CERRAR (la esquina de siempre) ===
                Rectangle btnCerrar = new Rectangle(x + w - 34, y + 8, 26, 26);
                bool hoverCerrar = PanelContiene(btnCerrar);
                sb.Draw(pixel, btnCerrar, (hoverCerrar ? TintaRojo : TintaApagado) * 0.25f);
                Utils.DrawBorderString(sb, "X",
                    new Vector2(btnCerrar.Center.X, btnCerrar.Center.Y),
                    hoverCerrar ? TintaRojo : TintaApagado, 1.0f, 0.5f, 0.5f);

                // ============================================================
                //  LA INTERACCIÓN (el borde del clic — TODO con
                //  mouseInterface: el juego NO usa el item al clicar).
                // ============================================================
                bool clicAhora = Main.mouseLeft && !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                bool clicNuevo = clicAhora && !_ultimoClicIzq;
                _ultimoClicIzq = clicAhora;

                // EL SLOT: meter/sacar con el cursor (solo lo que pasa el
                // filtro — el resto rebota con su porqué).
                if (hoverSlot && clicNuevo)
                {
                    if (!Main.mouseItem.IsAir)
                    {
                        if (Systems.DespertarGrimorio.AceptaRelleno(Main.mouseItem, yo, libro))
                        {
                            Item sale = _relleno.IsAir ? new Item() : (Item)_relleno.Clone();
                            _relleno = (Item)Main.mouseItem.Clone();
                            Main.mouseItem = sale;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                        }
                        else
                        {
                            Main.NewText(Language.GetTextValue(Clave("Rechazado")),
                                TintaRojo);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                        }
                    }
                    else if (!_relleno.IsAir)
                    {
                        Main.mouseItem = (Item)_relleno.Clone();
                        _relleno = new Item();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                    }
                }

                // EL BOTÓN OFRENDAR: valida, devora la pila ENTERA y suma
                // sus puntos al alma del libro.
                if (hoverBtn && clicNuevo && puedeOfrendar)
                {
                    int ganados = Systems.DespertarGrimorio.RegistrarOfrenda(_relleno, libro);
                    _relleno = new Item();
                    Main.NewText(Language.GetTextValue(Clave("OfrendaAceptada"),
                        ganados, puntos + ganados), TintaTeal);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113.WithPitchOffset(-0.35f));
                }

                // EL BOTÓN CERRAR / ESC (el borde del teclado).
                bool escAhora = Microsoft.Xna.Framework.Input.Keyboard
                    .GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape);
                if ((hoverCerrar && clicNuevo) || (escAhora && !_ultimoEsc))
                    CerrarDeVerdad();
                _ultimoEsc = escAhora;

                return true;
            }
            catch { return true; }
        }

        /// <summary>¿El ratón está encima de este rect DEL PANEL? (y come
        /// el clic: mouseInterface — el juego no usa el item).</summary>
        private static bool PanelContiene(Rectangle r)
        {
            bool dentro = r.Contains(Main.mouseX, Main.mouseY);
            if (dentro)
            {
                Main.LocalPlayer.mouseInterface = true;
                return true;
            }
            return false;
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
