using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    /// <summary>
    /// AnilloRunicoDorsalDrawLayer — v6.36 — EL ANILLO RÚNICO EN LA
    /// ESPALDA.
    ///
    /// La capa de dibujado que pinta EL ANILLO RÚNICO DORSAL: el gran
    /// anillo con sus runas DE PIE detrás del cuerpo del portador — se
    /// dibuja tras la capa de accesorios de ESPALDA (BackAcc: la zona
    /// de alas y capas) y por tanto queda DETRÁS del sprite del
    /// jugador: el cuerpo tapa el tramo del anillo que pasa por
    /// delante de la espalda, exactamente como un círculo mágico
    /// colgado a la espalda. Los quads salen de
    /// AnilloDorsalRenderer por la puerta oficial (AppendToPlayerDraw:
    /// DrawData de tML, sin tocar el estado del renderer).
    /// </summary>
    public class AnilloRunicoDorsalDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            // EN LA ESPALDA: tras la capa de accesorios traseros (la zona
            // de alas y capas) — el anillo queda DETRÁS del cuerpo.
            return new AfterParent(PlayerDrawLayers.BackAcc);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead || p.whoAmI < 0) return false;
            return p.GetModPlayer<CosmeticPlayer>().AnilloDorsal;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead) return;

            float time = Main.GlobalTimeWrappedHourly;

            // El ancla: el centro de la espalda (consciente de la gravedad
            // invertida — el anillo viaja con el pecho).
            Vector2 espalda = p.Center - new Vector2(0f, p.height * 0.04f * p.gravDir);

            VFXCore.Begin();
            AnilloDorsalRenderer.ComputeQuads(espalda, p.height, time, 1f);
            VFXCore.AppendToPlayerDraw(ref drawInfo);
        }
    }
}
