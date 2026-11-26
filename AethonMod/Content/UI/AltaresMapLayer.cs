using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// v6.166 — EL FARO DEL ALTAR Y DEL PILAR (la letra: «el altar sigue
    /// sin aparecer en el mapa, no se ve, el pilar en el mundo interior
    /// tampoco se ve en el mapa»).
    ///
    /// LA AUTOPSIA: ambos tiles tienen AddMapEntry con nombre y color,
    /// pero el minimapa de vanilla solo ESCRIBE el color de un tile
    /// cuando su sección se actualiza por LUZ — y los muebles colocados
    /// POR CÓDIGO (la llegada del altar, la generación del interior)
    /// nacen en zonas que el mapa ya dio por reveladas (o que el
    /// renacer del minimapa de la casa, la cura v6.138 del crash, barre
    /// de nuevo en cada entrada). El resultado: la tile EXISTE, el
    /// AddMapEntry EXISTE… y el mapa nunca la pinta.
    ///
    /// LA CURA — EL CAMINO DE LOS NPC Y JEFES (la letra: «ademas los
    /// npc aparecen usa el mismo sistema que tienen npc y bosses para
    /// aparecer en el mapa»): un ModMapLayer, LA MISMA API que
    /// GrimorioLlegadaMapLayer usa para el faro del grimorio dormido —
    /// el icono se dibuja SOBRE el mapa SIEMPRE (minimapa y mapa
    /// grande), con su pulso, sin depender de la revelación por luz.
    /// El pilar usa el faro del propio mundo interior; el altar, el
    /// faro de LaCaida/AltarUnico.
    /// </summary>
    public class AltaresMapLayer : ModMapLayer
    {
        private static Asset<Texture2D> _iconoAltar;
        private static Asset<Texture2D> _iconoPilar;

        public override void Draw(ref MapOverlayDrawContext context, ref string text)
        {
            if (Main.gameMenu || Main.dedServ || Main.netMode == 2)
                return;

            // ── LOS ICONOS (las texturas propias de la casa, carga inmediata) ──
            try
            {
                if (_iconoAltar == null || !_iconoAltar.IsLoaded)
                    _iconoAltar = ModContent.Request<Texture2D>(
                        "AethonMod/Content/UI/AltaresMapLayer_Altar",
                        AssetRequestMode.ImmediateLoad);
                if (_iconoPilar == null || !_iconoPilar.IsLoaded)
                    _iconoPilar = ModContent.Request<Texture2D>(
                        "AethonMod/Content/UI/AltaresMapLayer_Pilar",
                        AssetRequestMode.ImmediateLoad);
            }
            catch { return; }
            if (_iconoAltar?.Value == null || _iconoPilar?.Value == null)
                return;

            float pulso = 0.70f + 0.30f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.2f);
            var frame1 = new SpriteFrame(1, 1);

            // ── EL ALTAR (el mundo REAL: todos los altares vivos) ──
            try
            {
                int tipoAltar = ModContent.TileType
                    <global::AethonMod.Content.Tiles.AltarOfrendasTile>();
                // LA POSICIÓN ACTIVA de la casa (AltarUnicoSistema) si
                // vive; si no, escaneo corto alrededor del spawn de la
                // llegada. El escaneo completo del mundo es caro — el
                // sistema YA lo mantiene.
                int ax = global::AethonMod.Content.Systems.AltarUnicoSistema.ActivoX;
                int ay = global::AethonMod.Content.Systems.AltarUnicoSistema.ActivoY;
                if (ax > 0 && ay > 0)
                {
                    Tile t = Terraria.Framing.GetTileSafely(ax, ay);
                    if (t != null && t.HasTile && t.TileType == (ushort)tipoAltar)
                    {
                        var pos = new Vector2(ax + 1f, ay + 1.5f);
                        var res = context.Draw(_iconoAltar.Value, pos,
                            Color.White * pulso, frame1, 0.8f, 0.8f, Alignment.Center);
                        if (res.IsMouseOver)
                            text = Terraria.Localization.Language.GetTextValue(
                                "Mods.AethonMod.Mapa.Altar");
                    }
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AltaresMapLayer", exRegistro); }

            // ── EL PILAR (el mundo INTERIOR: su faro propio) ──
            try
            {
                if (global::AethonMod.Content.Systems.SubmundoGrimorio.Activo)
                {
                    // PlazaX/LlanoY del interior (400, 140): el pilar
                    // vive al lado del nacimiento — coordenadas del
                    // sistema, no números mágicos.
                    var posPilar = new Vector2(
                        global::AethonMod.Content.Systems.SubmundoGrimorio.PosPilarX,
                        global::AethonMod.Content.Systems.SubmundoGrimorio.PosPilarY);
                    var res = context.Draw(_iconoPilar.Value, posPilar,
                        Color.White * pulso, frame1, 0.8f, 0.8f, Alignment.Center);
                    if (res.IsMouseOver)
                        text = Terraria.Localization.Language.GetTextValue(
                            "Mods.AethonMod.Mapa.Pilar");
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AltaresMapLayer", exRegistro); }
        }
    }
}
