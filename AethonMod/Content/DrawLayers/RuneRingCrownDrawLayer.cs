using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    /// <summary>
    /// RuneRingCrownDrawLayer — v6.22 — LOS ANILLOS QUE TE RODEAN.
    ///
    /// Capa de dibujado que pinta la CORONA DE ANILLOS RÚNICOS: los tres
    /// aros orbitando el CUERPO del jugador (se dibujan en el pase
    /// delantero sobre los accesorios de cara: los anillos son ENERGÍA y
    /// cruzan por delante del cuerpo — el ecuatorial abraza la cintura).
    /// Los quads salen de RuneRingCrownRenderer por VFXCore
    /// (AppendToPlayerDraw — el camino oficial de las coronas).
    /// </summary>
    public class RuneRingCrownDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            // Por delante de la cara/accesorios: los aros RODEAN al cuerpo
            // y cruzan por delante — es una envoltura, no un halo alto.
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

            // Ancla: el CENTRO del cuerpo (los anillos RODEAN el torso).
            Vector2 body = p.Center - new Vector2(0f, p.height * 0.05f * p.gravDir);

            // La corona escala con el tamaño del sprite (humano = 1).
            float scale = p.height / 42f;

            VFXCore.Begin();
            RuneRingCrownRenderer.ComputeQuads(body, scale, Main.GlobalTimeWrappedHourly, 1f);
            VFXCore.AppendToPlayerDraw(ref drawInfo);
        }
    }
}
