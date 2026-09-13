using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    /// <summary>
    /// VFXWingsDrawLayer — v6.08 — LA CAPA DE LAS 8 ALAS DE LUZ.
    ///
    /// UNA sola capa para todo el sistema (v6.06 tenía dos separadas):
    /// pinta las alas JUSTO DESPUÉS de la capa vanilla de alas (el pase de
    /// ESPALDA: quedan detrás del cuerpo, como alas de verdad — y ancladas
    /// a la ESPALDA ALTA, a la altura de los omóplatos, donde el análisis
    /// de la captura del usuario pidió que nazcan). El slot de equipo
    /// equipado decide QUÉ estilo se renderiza (VFXWingSlots) y el estado
    /// de animación lo aporta WingAnimPlayer. Los cuadros salen por
    /// VFXCore.AppendToPlayerDraw — el camino oficial de DrawData, idéntico
    /// a las coronas.
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

            // Anclaje en la ESPALDA ALTA: omóplatos (6px sobre el centro del
            // torso, respetando la gravedad invertida — donde el usuario pidió
            // que nazcan las alas tras ver la captura mal anclada).
            Vector2 back = p.Center + new Vector2(0f, -6f * p.gravDir);

            // Luz del mundo en la posición del jugador: las alas arden solas
            // de noche pero se integran con la iluminación de día.
            Color light = Lighting.GetColor((int)(p.Center.X / 16f), (int)(p.Center.Y / 16f), Color.White);
            float lum = (light.R + light.G + light.B) / (3f * 255f);
            float alpha = 0.5f + 0.5f * lum;

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
