using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    /// <summary>
    /// VoidCrownDrawLayer — v6.03 — LA CORONA DETRÁS DE LA CABEZA.
    ///
    /// Capa de dibujado del jugador que pinta la CORONA DE LA REINA DEL
    /// VACÍO justo DETRÁS de la cabeza (antes de la capa Head: donde los
    /// lazos crucen el sprite, la cabeza los tapa — la corona "envuelve"
    /// la cabeza sin taparla). Los quads salen de ArcCrownRenderer a
    /// través de la biblioteca (AppendToPlayerDraw: el camino oficial de
    /// DrawData, sin tocar el batch del renderer de jugadores).
    /// </summary>
    public class VoidCrownDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            // DETRÁS de la cabeza: se dibuja justo antes de la capa Head.
            return new BeforeParent(PlayerDrawLayers.Head);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead || p.whoAmI < 0) return false;
            return p.GetModPlayer<CosmeticPlayer>().VoidCrown;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead) return;

            // Centro de la cabeza en coords de MUNDO (respeta gravedad
            // invertida: la cabeza del colgado está abajo).
            Vector2 head = p.Center - new Vector2(0f, p.height * 0.22f * p.gravDir);

            // El "horizonte" de la corona a escala de cabeza humana.
            float horizonPx = System.Math.Max(6f, p.width * 0.55f);

            VFXCore.Begin();
            ArcCrownRenderer.ComputeQuads(head, horizonPx, Main.GlobalTimeWrappedHourly, 1f);
            VFXCore.AppendToPlayerDraw(ref drawInfo);
        }
    }
}
