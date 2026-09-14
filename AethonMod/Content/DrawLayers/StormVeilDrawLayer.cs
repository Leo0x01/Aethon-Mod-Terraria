using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    /// <summary>
    /// StormVeilDrawLayer — v6.23 — LA ENVOLTURA DE RAYOS SOBRE EL JUGADOR.
    ///
    /// Capa de dibujado que pinta la TORMENTA procedural: los chispazos
    /// se dibujan en el pase de ESPALDA (tras las alas, detrás del
    /// cuerpo — igual que la envoltura de fuego) para que la SILUETA del
    /// jugador siga legible ENVUELTA en la descarga: los arcos abrazan
    /// el contorno y los chispazos cruzan el cuerpo por detrás.
    ///
    /// Los quads salen de StormVeilRenderer.ComputeQuads al buffer de
    /// VFXCore y de ahí al DrawDataCache oficial de tML (el camino de las
    /// coronas, probado desde v6.03).
    /// </summary>
    public class StormVeilDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            // La tormenta vive DETRÁS del cuerpo: mismo pase que las alas
            // y que el fuego (el pase de espalda).
            return new AfterParent(PlayerDrawLayers.Wings);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead || p.whoAmI < 0) return false;
            var sp = p.GetModPlayer<StormVeilPlayer>();
            return sp.StormVeil;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead) return;

            var sp = p.GetModPlayer<StormVeilPlayer>();

            // ANCLA: el CENTRO del cuerpo (el carril es la silueta entera).
            Vector2 center = p.Center;

            // La luz del mundo modula un 15%: el rayo es FUENTE DE LUZ,
            // estalla igual de bien en la cueva más oscura.
            Color light = Lighting.GetColor((int)(p.Center.X / 16f), (int)(p.Center.Y / 16f), Color.White);
            float lum = (light.R + light.G + light.B) / (3f * 255f);
            float alpha = (0.85f + 0.15f * lum) * sp.Warmup;

            VFXCore.Begin();
            StormVeilRenderer.ComputeQuads(center, p.width, p.height, p.velocity,
                Main.GlobalTimeWrappedHourly, sp.StormEnergy, alpha,
                p.whoAmI * 37 + 7);
            VFXCore.AppendToPlayerDraw(ref drawInfo);
        }
    }
}
