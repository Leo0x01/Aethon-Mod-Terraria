using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    /// <summary>
    /// EventHorizonWingsDrawLayer — v6.06 — LAS ALAS DEL HORIZONTE DE SUCESOS.
    ///
    /// Capa de dibujado del jugador que pinta las alas de luz del Horizonte
    /// de Sucesos JUSTO DESPUÉS de la capa vanilla de alas (el pase de
    /// ESPALDA: quedan detrás del cuerpo, como unas alas de verdad). Los
    /// cuadros salen de EventHorizonWingRenderer a través de la biblioteca
    /// (AppendToPlayerDraw: el camino oficial de DrawData, idéntico a las
    /// coronas). La animación vive en WingAnimPlayer.
    /// </summary>
    public class EventHorizonWingsDrawLayer : PlayerDrawLayer
    {
        private int _slot = -1;

        private int GetSlot()
        {
            if (_slot < 0)
                _slot = EquipLoader.GetEquipSlot(Mod, "EventHorizonWings", EquipType.Wings);
            return _slot;
        }

        public override Position GetDefaultPosition()
        {
            // Después de la capa vanilla de alas → mismo pase (detrás del cuerpo).
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

            // Anclaje en la ESPALDA ALTA (por encima del centro del torso,
            // respetando la gravedad invertida).
            Vector2 back = p.Center + new Vector2(0f, -5f * p.gravDir);

            // Luz del mundo en la posición del jugador: las alas arden solas
            // de noche pero se integran con la iluminación de día.
            Color light = Lighting.GetColor((int)(p.Center.X / 16f), (int)(p.Center.Y / 16f), Color.White);
            float lum = (light.R + light.G + light.B) / (3f * 255f);
            float alpha = 0.5f + 0.5f * lum;

            VFXCore.Begin();
            EventHorizonWingRenderer.ComputeQuads(back, mp.EhOpen, mp.EhFlapAmp,
                mp.EhFlapPhase, Main.GlobalTimeWrappedHourly, p.direction, p.gravDir, alpha);
            VFXCore.AppendToPlayerDraw(ref drawInfo);
        }
    }
}
