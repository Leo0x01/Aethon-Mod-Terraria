using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    /// <summary>
    /// FireVeilDrawLayer — v6.22 — LA ENVOLTURA DE FUEGO SOBRE EL JUGADOR.
    ///
    /// Capa de dibujado que pinta el CAMPO DE FUEGO procedural: las
    /// llamas se dibujan en el pase de ESPALDA (tras las alas, detrás del
    /// cuerpo) para que la SILUETA del jugador siga legible ENVUELTA en
    /// el fuego — las llamas más altas suben por encima de la cabeza y
    /// las brasas cubren los pies: el abrazo completo.
    ///
    /// Los quads salen de FireVeilRenderer.ComputeQuads al buffer de
    /// VFXCore y de ahí al DrawDataCache oficial de tML (el camino de las
    /// coronas, probado desde v6.03).
    /// </summary>
    public class FireVeilDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            // El fuego vive DETRÁS del cuerpo: mismo pase que las alas
            // (tras la capa vanilla de alas — el pase de espalda).
            return new AfterParent(PlayerDrawLayers.Wings);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead || p.whoAmI < 0) return false;
            var fp = p.GetModPlayer<FireVeilPlayer>();
            return fp.FireVeil && fp.Grid != null;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead) return;

            var fp = p.GetModPlayer<FireVeilPlayer>();
            int[] grid = fp.Grid;
            if (grid == null) return;

            // ANCLA: los PIES del jugador (la base de la pira), consciente
            // de la gravedad invertida.
            Vector2 feet = p.Bottom;

            // La luz del mundo modula un 15%: el fuego es FUENTE DE LUZ,
            // arde igual de bien en la cueva más oscura.
            Color light = Lighting.GetColor((int)(p.Center.X / 16f), (int)(p.Center.Y / 16f), Color.White);
            float lum = (light.R + light.G + light.B) / (3f * 255f);
            float alpha = (0.85f + 0.15f * lum) * fp.Warmup;

            VFXCore.Begin();
            FireVeilRenderer.ComputeQuads(feet, grid, p.velocity, alpha);
            VFXCore.AppendToPlayerDraw(ref drawInfo);
        }
    }
}
