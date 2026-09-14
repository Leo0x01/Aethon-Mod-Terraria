using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    /// <summary>
    /// RunicHaloWingsDrawLayer — v6.22 — EL ANILLO RÚNICO EN LA ESPALDA.
    ///
    /// La capa de las alas-anillo: pinta el GRAN HALO RÚNICO tras la
    /// espalda del jugador (mismo pase que las alas — el pase de ESPALDA:
    /// el anillo queda DETRÁS del cuerpo como un halo de verdad). La
    /// INTENSIDAD la aporta RunicHaloPlayer (la energía de vuelo): quieta
    /// es un sello elegante; VOLANDO es un sol en tu espalda.
    ///
    /// Los quads salen de RunicHaloRenderer por VFXCore
    /// (AppendToPlayerDraw — el camino oficial de las coronas).
    /// </summary>
    public class RunicHaloWingsDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            // Después de la capa vanilla de alas → mismo pase (detrás del
            // cuerpo): el ANILLO es el halo de la espalda.
            return new AfterParent(PlayerDrawLayers.Wings);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead || p.whoAmI < 0) return false;
            return p.wings == EquipLoader.GetEquipSlot(Mod, "RunicHaloWings", EquipType.Wings);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead) return;

            // ANCLA: la ESPALDA ALTA (omóplatos), consciente de la gravedad.
            Vector2 back = p.Center + new Vector2(0f, -p.height * 0.145f * p.gravDir);

            // La corona escala con el tamaño del sprite (humano = 1).
            float scale = p.height / 42f;

            // LA ENERGÍA DE VUELO (el brillo del anillo).
            float flight = p.GetModPlayer<RunicHaloPlayer>().FlightEnergy;

            // v6.12 — Luz del mundo con PISO ALTO: el anillo es un CUERPO
            // DE LUZ — arde solo de noche y de día solo se aviva un 12%.
            Color light = Lighting.GetColor((int)(p.Center.X / 16f), (int)(p.Center.Y / 16f), Color.White);
            float lum = (light.R + light.G + light.B) / (3f * 255f);
            float alpha = 0.88f + 0.12f * lum;

            VFXCore.Begin();
            RunicHaloRenderer.ComputeQuads(back, scale, Main.GlobalTimeWrappedHourly,
                flight, alpha);
            VFXCore.AppendToPlayerDraw(ref drawInfo);
        }
    }
}
