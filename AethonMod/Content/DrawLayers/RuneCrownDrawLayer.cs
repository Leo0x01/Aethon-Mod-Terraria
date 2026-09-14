using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    /// <summary>
    /// RuneCrownDrawLayer — v6.03 — EL HALO RÚNICO SOBRE LA CABEZA.
    ///
    /// Capa de dibujado del jugador que pinta la CORONA RÚNICA ESTELAR:
    /// el arco de ocho glifos FLOTANDO por encima de la cabeza (se dibuja
    /// tras las capas de cabeza/cara: es un halo, vive por encima y no
    /// tapa nada). Los quads salen de RuneCrownRenderer a través de la
    /// biblioteca (AppendToPlayerDraw: DrawData oficial de tML).
    /// </summary>
    public class RuneCrownDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            // Un HALO: por delante de la cabeza/cara pero flotando por
            // ENCIMA del sprite — se dibuja tras la capa de accesorios de cara.
            return new AfterParent(PlayerDrawLayers.FaceAcc);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead || p.whoAmI < 0) return false;
            return p.GetModPlayer<CosmeticPlayer>().RuneCrown;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead) return;

            // Ancla: centro de la cabeza (mundo), consciente de la gravedad.
            Vector2 head = p.Center - new Vector2(0f, p.height * 0.22f * p.gravDir);

            // La corona escala con el tamaño del sprite (humano = 1).
            float scale = p.height / 42f;

            VFXCore.Begin();
            RuneCrownRenderer.ComputeQuads(head, scale, Main.GlobalTimeWrappedHourly, 1f);
            VFXCore.AppendToPlayerDraw(ref drawInfo);
        }
    }
}
