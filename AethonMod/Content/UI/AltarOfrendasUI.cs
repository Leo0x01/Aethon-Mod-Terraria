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
    //  AltarOfrendasUI — v6.88 — EL ALTAR DE LA CORRUPCIÓN.
    //
    //  LA LETRA: «la funcion del altar que implementaste no debe estar
    //  ligada al grimorio, asi que quita ese requisito de ofrendas en el
    //  despertar, el altar sirve para corromper las armas»: LA ECONOMÍA
    //  DE PUNTOS (relleno → puntos → canje de esencias del 30 %) MURIÓ
    //  con la v6.86. HOY el altar es EL CORRUPTOR: UN slot de ARMA
    //  LIMPIA y UN botón que la CORROMPE (Corrompido = true + ResetPrefix
    //  — el MISMO sello que dejaba la receta de corrupción de siempre:
    //  la vía de recetas SIGUE como alternativa). El arma corrupta gana
    //  su etiqueta «(Corrupto)» EN EL NOMBRE (negro — CorruptoGlobalItem)
    //  y su brillo morado-negro en el inventario y el mundo.
    //
    //  CERO VÍNCULO CON EL GRIMORIO: el panel ya NO lee el alma del
    //  libro (ni puntos, ni crédito, ni SinLibro) — el altar corrompe
    //  para CUALQUIERA. El patrón de UI es el de siempre (la casa: El
    //  Cielo del Despertar — LegacyGameInterfaceLayer antes de "Vanilla:
    //  Mouse Text", estado estático de cliente, la ley de la distancia a
    //  280 px y la persistencia del slot con el MUNDO).
    // ======================================================================
    public class AltarOfrendasUI : ModSystem
    {
        // === EL ESTADO (cliente puro — el patrón El Cielo) ===
        private static bool _abierta;
        private static int _tileX, _tileY;
        private static Item _arma = new Item();
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
            if (!_abierta) DevolverElArma();
        }

        public override void OnWorldUnload() { CerrarDeVerdad(); }
        public override void Unload() { CerrarDeVerdad(); _arma = new Item(); }

        /// <summary>EL CIERRE LIMPIO: nada queda en el slot (al cursor si
        /// está libre, al suelo si no).</summary>
        private static void CerrarDeVerdad()
        {
            if (!_abierta) { _arma = new Item(); return; }
            _abierta = false;
            DevolverElArma();
        }

        private static void DevolverElArma()
        {
            try
            {
                if (_arma == null || _arma.IsAir) { _arma = new Item(); return; }
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active) return;
                if (Main.mouseItem != null && Main.mouseItem.IsAir)
                    Main.mouseItem = (Item)_arma.Clone();
                else
                    yo.QuickSpawnItem(yo.GetSource_Misc("AltarCorrupcion"), _arma, _arma.stack);
                _arma = new Item();
            }
            catch { _arma = new Item(); }
        }

        // ================================================================
        //  LA PERSISTENCIA DEL SLOT (con el MUNDO — la clave de siempre
        //  «altarRelleno» se lee por compatibilidad con mundos v6.86).
        // ================================================================
        public override void SaveWorldData(TagCompound tag)
        {
            try
            {
                if (_arma != null && !_arma.IsAir)
                    tag["altarArma"] = ItemIO.Save(_arma);
            }
            catch { }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            try
            {
                _arma = new Item();
                if (tag.ContainsKey("altarArma"))
                    _arma = ItemIO.Load(tag.Get<TagCompound>("altarArma"));
                else if (tag.ContainsKey("altarRelleno"))   // mundos de la v6.86
                    _arma = ItemIO.Load(tag.Get<TagCompound>("altarRelleno"));
                if (_arma == null) _arma = new Item();
            }
            catch { _arma = new Item(); }
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
                    "AethonMod: El Altar de la Corrupción",
                    DibujarElPanel, InterfaceScaleType.UI));
            }
        }

        // ================================================================
        //  LA PALETA (la de la casa)
        // ================================================================
        private static readonly Color TintaFondo = new(10, 16, 18);
        private static readonly Color TintaPanel = new(16, 28, 30);
        private static readonly Color TintaTeal = new(90, 220, 180);
        private static readonly Color TintaVioleta = new(186, 96, 236);
        private static readonly Color TintaTexto = new(220, 250, 245);
        private static readonly Color TintaApagado = new(110, 150, 145);
        private static readonly Color TintaRojo = new(255, 120, 120);
        private static string Clave(string c) => "Mods.AethonMod.Altar." + c;

        // ================================================================
        //  v6.88 — EL FILTRO DEL CORRUPTOR: SOLO armas LIMPIAS (la copia
        //  local del EsArma de la casa — CERO dependencia del sistema del
        //  despertar: el altar no sabe nada del grimorio).
        // ================================================================
        private static bool EsArmaCorruptible(Item it)
        {
            try
            {
                if (it == null || it.IsAir) return false;
                if (it.damage <= 0) return false;
                if (it.accessory || it.vanity) return false;
                if (it.pick > 0 || it.axe > 0 || it.hammer > 0 || it.ammo > 0) return false;
                if (it.ModItem is global::AethonMod.Content.Weapons.GrimoireEternal) return false;
                var sello = it.GetGlobalItem
                    <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                if (sello != null && sello.Corrompido) return false;   // ya corrupta
                return true;
            }
            catch { return false; }
        }

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
                int w = 380, h = 300;
                int x = Math.Min(Main.screenWidth - w - 10,
                    470 + Math.Max(0, (Main.screenWidth - 1100) / 3));
                int y = Main.screenHeight / 2 - h / 2;
                if (x < 10) x = 10;

                // EL FONDO + el marco de la casa (el violeta de lo corrupto).
                sb.Draw(pixel, new Rectangle(x - 3, y - 3, w + 6, h + 6), TintaFondo * 0.94f);
                Marco(sb, pixel, new Rectangle(x - 3, y - 3, w + 6, h + 6), TintaVioleta * 0.80f);
                sb.Draw(pixel, new Rectangle(x, y, w, h), TintaPanel * 0.97f);

                // EL TITULAR (v6.88 — el corruptor).
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("Titulo")),
                    new Vector2(x + w / 2f, y + 22f), TintaVioleta, 1.1f, 0.5f, 0.4f);
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("Subtitulo")),
                    new Vector2(x + w / 2f, y + 42f), TintaApagado, 0.75f, 0.5f, 0.4f);

                // === EL SLOT DEL ARMA (la boca del corruptor) ===
                Rectangle slot = new Rectangle(x + 24, y + 86, 52, 52);
                bool hoverSlot = PanelContiene(slot) && !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                sb.Draw(pixel, slot, TintaFondo * 0.9f);
                Marco(sb, pixel, slot, hoverSlot ? TintaVioleta : TintaVioleta * 0.45f);
                if (_arma != null && !_arma.IsAir)
                {
                    Texture2D tex = TextureAssets.Item[_arma.type].Value;
                    float escala = Math.Min(44f / tex.Width, 44f / tex.Height);
                    sb.Draw(tex, slot.Center.ToVector2(), null, Color.White, 0f,
                        tex.Size() * 0.5f, escala, SpriteEffects.None, 0f);
                    if (_arma.stack > 1)
                        Utils.DrawBorderString(sb, _arma.stack.ToString(),
                            new Vector2(slot.Right - 6f, slot.Bottom - 8f), TintaTexto, 0.85f, 1f, 0.85f);
                }
                else
                {
                    Utils.DrawBorderString(sb, Language.GetTextValue(Clave("SlotVacio")),
                        new Vector2(slot.Center.X, slot.Center.Y), TintaApagado * 0.8f, 0.7f, 0.5f, 0.5f);
                }
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("EtiquetaArma")),
                    new Vector2(x + 92f, y + 96f), TintaTexto, 0.9f);
                string filtroTxt = Language.GetTextValue(Clave("FiltroArma"));
                Utils.DrawBorderString(sb, filtroTxt, new Vector2(x + 92f, y + 118f), TintaApagado, 0.72f);

                // === EL ESTADO DEL ARMA EN EL SLOT (lista / ya corrupta / no
                //     es arma — el porqué de cada rechazo) ===
                bool lista = EsArmaCorruptible(_arma);
                if (_arma != null && !_arma.IsAir)
                {
                    var selloArma = _arma.GetGlobalItem
                        <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                    bool yaEra = selloArma != null && selloArma.Corrompido;
                    string estado = yaEra ? Language.GetTextValue(Clave("YaCorrupta"))
                        : lista ? Language.GetTextValue(Clave("Lista"))
                        : Language.GetTextValue(Clave("NoArma"));
                    Color colEstado = yaEra ? TintaVioleta
                        : lista ? TintaTeal : TintaRojo;
                    Utils.DrawBorderString(sb, estado,
                        new Vector2(x + 92f, y + 158f), colEstado, 0.85f);
                }

                // === EL BOTÓN CORROMPER (el sello) ===
                Rectangle btnCorromper = new Rectangle(x + 24, y + 196, 170, 42);
                bool hoverBtn = PanelContiene(btnCorromper);
                sb.Draw(pixel, btnCorromper,
                    (hoverBtn && lista ? TintaVioleta : TintaVioleta * 0.35f) * 0.30f);
                Marco(sb, pixel, btnCorromper, lista ? TintaVioleta : TintaVioleta * 0.35f);
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("Corromper")),
                    new Vector2(btnCorromper.Center.X, btnCorromper.Center.Y),
                    lista ? TintaTexto : TintaApagado, 1.0f, 0.5f, 0.5f);

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

                // EL SLOT: meter/sacar con el cursor (solo armas limpias —
                // el resto rebota con su porqué).
                if (hoverSlot && clicNuevo)
                {
                    if (!Main.mouseItem.IsAir)
                    {
                        if (EsArmaCorruptible(Main.mouseItem))
                        {
                            Item sale = _arma.IsAir ? new Item() : (Item)_arma.Clone();
                            _arma = (Item)Main.mouseItem.Clone();
                            Main.mouseItem = sale;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                        }
                        else
                        {
                            var selloMouse = Main.mouseItem.GetGlobalItem
                                <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                            bool yaEraMouse = selloMouse != null && selloMouse.Corrompido;
                            Main.NewText(Language.GetTextValue(
                                yaEraMouse ? Clave("YaCorrupta") : Clave("NoArma")), TintaRojo);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                        }
                    }
                    else if (!_arma.IsAir)
                    {
                        Main.mouseItem = (Item)_arma.Clone();
                        _arma = new Item();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                    }
                }

                // EL BOTÓN CORROMPER: el sello de la casa (el MISMO de la
                // receta — Corrompido = true + ResetPrefix: el «Superior»
                // jamás vuelve) + el teatro.
                // v6.89 — EL PRECIO (la letra: «para corromper algo
                //     necesita esencia corrupta en dependencia de la
                //     calidad del arma, el tipo de arma, el daño del arma
                //     y sus estadisticas o efecto, no es lo mismo
                //     corromper una espada de cobre que una espada de
                //     adamantita, la de cobre pide menos esencia
                //     corrupta»): el altar cobra la fórmula de la casa
                //     (CorruptoGlobalItem.CosteCorrupcion) en Esencias
                //     del Eterno — y viste la rareza corrupta.
                if (hoverBtn && clicNuevo && lista && !_arma.IsAir)
                {
                    var sello = _arma.GetGlobalItem
                        <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                    if (sello != null)
                    {
                        int coste = global::AethonMod.Content.Globals.CorruptoGlobalItem
                            .CosteCorrupcion(_arma);
                        int llevaba = global::AethonMod.Content.Globals.CorruptoGlobalItem
                            .EsenciasEnLaBolsa(Main.LocalPlayer);
                        if (llevaba < coste)
                        {
                            Main.NewText(Language.GetTextValue(Clave("FaltanEsencias"),
                                coste, llevaba), TintaRojo);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                        }
                        else
                        {
                            global::AethonMod.Content.Globals.CorruptoGlobalItem
                                .PagarCorrupcion(Main.LocalPlayer, coste);
                            sello.Corrompido = true;
                            _arma.ResetPrefix();
                            global::AethonMod.Content.Globals.CorruptoGlobalItem
                                .VestirLaRareza(_arma);
                            Main.NewText(Language.GetTextValue(Clave("CorruptaHecha"),
                                _arma.Name), TintaVioleta);
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item113.WithPitchOffset(-0.45f));
                            // EL TEATRO DEL SELLO: la bruma violeta del altar.
                            if (!Main.dedServ)
                            {
                                for (int k = 0; k < 22; k++)
                                {
                                    Dust d = Dust.NewDustPerfect(
                                        altarCentro + new Vector2(Main.rand.NextFloat(-14f, 14f),
                                            Main.rand.NextFloat(-30f, 6f)),
                                        DustID.Shadowflame,
                                        Main.rand.NextVector2Circular(2.5f, 2.5f), 140,
                                        new Color(140, 60, 210), 1f);
                                    d.noGravity = true;
                                }
                            }
                            DevolverElArma();   // el arma corrupta vuelve al cursor
                        }
                    }
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
