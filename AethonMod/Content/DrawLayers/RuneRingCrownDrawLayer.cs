using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    /// <summary>
    /// RuneRingCrownDrawLayer — v6.25 — LA AUREOLA DEL SOL I SOBRE LA CABEZA.
    ///
    /// Capa de dibujado del jugador que pinta LA CORONA DE ANILLOS
    /// RÚNICOS: el ANILLO DEL SOL RÚNICO I ringiendo la CABEZA como una
    /// AUREOLA (v6.25 — la corrección del destinatario: esta corona, no
    /// la Estelar, es la que vive sobre la cabeza). Se dibuja tras las
    /// capas de cabeza/cara: la aureola vive ALREDEDOR de la cabeza.
    /// Los quads salen de RuneRingCrownRenderer a través de la
    /// biblioteca (AppendToPlayerDraw — el camino oficial de tML).
    /// </summary>
    public class RuneRingCrownDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            // Tras la cara/accesorios de cabeza: la aureola RODEA la cabeza.
            return new AfterParent(PlayerDrawLayers.FaceAcc);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead || p.whoAmI < 0) return false;
            return p.GetModPlayer<CosmeticPlayer>().RuneRingCrown;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead) return;

            // Ancla: el CENTRO de la CABEZA (la aureola la ringea).
            Vector2 head = p.Center - new Vector2(0f, p.height * 0.22f * p.gravDir);

            // La corona escala con el tamaño del sprite (humano = 1).
            float scale = p.height / 42f;

            VFXCore.Begin();
            RuneRingCrownRenderer.ComputeQuads(head, scale, Main.GlobalTimeWrappedHourly, 1f);
            VFXCore.AppendToPlayerDraw(ref drawInfo);
        }
    }
}
