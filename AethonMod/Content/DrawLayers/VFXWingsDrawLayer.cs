using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    /// <summary>
    /// VFXWingsDrawLayer — v6.12 — LA CAPA DE LAS 8 ALAS DE LUZ (de vuelta,
    /// con la LECCIÓN de visibilidad aprendida).
    ///
    /// UNA sola capa para todo el sistema: pinta las alas JUSTO DESPUÉS de
    /// la capa vanilla de alas (el pase de ESPALDA: quedan detrás del
    /// cuerpo, como alas de verdad — ancladas a la ESPALDA ALTA, a la
    /// altura de los omóplatos). El slot de equipo equipado decide QUÉ
    /// estilo se renderiza (VFXWingSlots) y el estado de animación lo
    /// aporta WingAnimPlayer. Los cuadros salen por
    /// VFXCore.AppendToPlayerDraw — el camino oficial de DrawData, idéntico
    /// a las coronas (la técnica que el usuario pidió expresamente).
    ///
    /// v6.12 — LA LECCIÓN: el pase de jugador compone con AlphaBlend (NO
    /// aditivo). Las alfas tenues del v6.08 (pensadas para aditivo)
    /// quedaban casi invisibles y las alas se leían como “manchas”, no como
    /// alas. Ahora los renderizadores usan alfas ALTAS (como las coronas,
    /// que sí leen perfecto: pulse × alpha ≈ 0.75–1.0) y AQUÍ la luz del
    /// mundo solo modula un 12% (piso 0.88): las alas SON fuentes de luz,
    /// arden igual de bien en una cueva que a pleno sol.
    /// </summary>
    public class VFXWingsDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            // Después de la capa vanilla de alas → mismo pase (detrás del cuerpo).
            return new AfterParent(PlayerDrawLayers.Wings);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead || p.whoAmI < 0) return false;
            return VFXWingSlots.StyleFromSlot(Mod, p.wings) >= 0;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead) return;

            int style = VFXWingSlots.StyleFromSlot(Mod, p.wings);
            if (style < 0) return;

            var mp = p.GetModPlayer<WingAnimPlayer>();
            ref WingAnimPlayer.WingAnimState st = ref mp.State(style);

            // Anclaje en la ESPALDA ALTA: omóplatos (escala con el tamaño del
            // sprite: 6px para un humano de 42px), respetando la gravedad
            // invertida — donde el usuario pidió que nazcan las alas.
            Vector2 back = p.Center + new Vector2(0f, -p.height * 0.145f * p.gravDir);

            // v6.12 — Luz del mundo con PISO ALTO: las alas son CUERPOS DE
            // LUZ (como las coronas): arden solas de noche y de día solo se
            // avivan un 12% — nunca se apagan por estar en una cueva.
            Color light = Lighting.GetColor((int)(p.Center.X / 16f), (int)(p.Center.Y / 16f), Color.White);
            float lum = (light.R + light.G + light.B) / (3f * 255f);
            float alpha = 0.88f + 0.12f * lum;

            // El contexto COMPLETO del ala: apertura, aleteo, dirección,
            // gravedad, luz Y las velocidades para el sweep aerodinámico.
            WingDrawContext ctx = new WingDrawContext
            {
                Back = back,
                Open = st.Open,
                FlapPhase = st.FlapPhase,
                FlapAmp = st.FlapAmp,
                Time = Main.GlobalTimeWrappedHourly,
                Direction = p.direction,
                GravDir = p.gravDir,
                Alpha = alpha,
                // El sweep: la velocidad horizontal barre las alas atrás.
                SpeedX = p.velocity.X,
                // El diedro: -1 subiendo .. 1 cayendo (normalizado, saturado).
                Rise = MathHelper.Clamp(p.velocity.Y / 9f, -1f, 1f),
            };

            VFXCore.Begin();
            WingStyles.Renderer(style)(ref ctx);
            VFXCore.AppendToPlayerDraw(ref drawInfo);
        }
    }
}
