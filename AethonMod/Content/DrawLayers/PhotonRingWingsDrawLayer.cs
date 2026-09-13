using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    /// <summary>
    /// PhotonRingWingsDrawLayer — v6.06 — LAS ALAS DEL ANILLO DE FOTONES.
    ///
    /// Capa de dibujado del jugador que pinta las alas del Anillo de
    /// Fotones justo después de la capa vanilla de alas (pase de ESPALDA).
    /// Mismo camino de biblioteca que las coronas (DrawData oficial vía
    /// AppendToPlayerDraw); la animación vive en WingAnimPlayer.
    /// </summary>
    public class PhotonRingWingsDrawLayer : PlayerDrawLayer
    {
        private int _slot = -1;

        private int GetSlot()
        {
            if (_slot < 0)
                _slot = EquipLoader.GetEquipSlot(Mod, "PhotonRingWings", EquipType.Wings);
            return _slot;
        }

        public override Position GetDefaultPosition()
        {
            return new AfterParent(PlayerDrawLayers.Wings);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead || p.whoAmI < 0) return false;
            return p.wings == GetSlot();
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead) return;

            var mp = p.GetModPlayer<WingAnimPlayer>();

            Vector2 back = p.Center + new Vector2(0f, -4f * p.gravDir);

            Color light = Lighting.GetColor((int)(p.Center.X / 16f), (int)(p.Center.Y / 16f), Color.White);
            float lum = (light.R + light.G + light.B) / (3f * 255f);
            float alpha = 0.5f + 0.5f * lum;

            VFXCore.Begin();
            PhotonRingWingRenderer.ComputeQuads(back, mp.PrOpen, mp.PrFlapAmp,
                mp.PrFlapPhase, Main.GlobalTimeWrappedHourly, p.direction, p.gravDir, alpha);
            VFXCore.AppendToPlayerDraw(ref drawInfo);
        }
    }
}
