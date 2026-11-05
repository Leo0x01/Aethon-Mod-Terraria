using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Players;

namespace AethonMod.Content.DrawLayers
{
    // ======================================================================
    //  EstandarteBannerDrawLayer — v6.100 — LA TELA EN LA ESPALDA.
    //
    //  LA LETRA DEL USUARIO: «el estandarte del juicio no tiene nada
    //  que ver con el original y se ve peor». La investigación del
    //  original (research/README-ESTANDARTE.md) mostró que su versión
    //  llevaba EL ESTANDARTE ENTERO A LA ESPALDA (AutoloadEquip
    //  Balloon en el original) — nuestra v6.98 solo dibujaba un aura
    //  y nada más. ESTA capa es la tela de la casa: el pennant dorado
    //  con su sol del juicio y su fuego, FORJADO por
    //  tools/estandarte_v6100.py (5 marcos de 30×56), ondeando detrás
    //  del portador — se mece con el tiempo, se voltea con la
    //  dirección y se apaga si el portador pidió ocultar el visual.
    //
    //  EL ANCLAJE: detrás del accesorio de espalda (BeforeParent
    //  BackAcc), colgado del hombro (9 px detrás del centro, con el
    //  vaivén sinusoidal de 5.2 rad/s — la tela ondea con brisa
    //  propia).
    // ======================================================================
    public class EstandarteBannerDrawLayer : PlayerDrawLayer
    {
        /// <summary>La altura de cada marco de la tira (px).</summary>
        private const int ALTO = 56;

        /// <summary>El ancho de la tira (px).</summary>
        private const int ANCHO = 30;

        public override Position GetDefaultPosition()
        {
            // DETRÁS de todo el equipo de espalda — la tela es lo
            // primero que se ve al mirar al portador desde atrás.
            return new BeforeParent(PlayerDrawLayers.BackAcc);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player drawPlayer = drawInfo.drawPlayer;
            if (drawPlayer == null || drawPlayer.dead || drawPlayer.whoAmI < 0)
                return false;
            if (drawPlayer.invis)
                return false;
            JuicioPlayer modPlayer = drawPlayer.GetModPlayer<JuicioPlayer>();
            // LA TELA vive con el estandarte equipado y visible.
            return modPlayer.EstandarteJuicio && !modPlayer.EstandarteJuicioOculto;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player drawPlayer = drawInfo.drawPlayer;
            if (drawPlayer == null || drawPlayer.dead)
                return;

            Texture2D tex = ModContent.Request<Texture2D>(
                "AethonMod/Content/DrawLayers/EstandarteBannerTrasero",
                AssetRequestMode.ImmediateLoad).Value;

            float tiempo = Main.GlobalTimeWrappedHourly;

            // EL MARCO VIVO: 8.5 marcos/s (el ondear de la tira).
            int marco = (int)(tiempo * 8.5f) % 5;
            if (marco < 0) marco += 5;
            Rectangle frame = new Rectangle(0, marco * ALTO, ANCHO, ALTO);

            // EL VAIVÉN: la tela sube y baja 1.6 px con su brisa.
            float vaiven = MathF.Sin(tiempo * 5.2f) * 1.6f;

            // EL ANCLAJE: del hombro trasero, 9 px detrás del centro.
            Vector2 pos = drawPlayer.Center
                - new Vector2(0f, drawPlayer.height * 0.1f * drawPlayer.gravDir)
                - Main.screenPosition
                + new Vector2(-drawPlayer.direction * 9f, vaiven * 0.5f);

            // LA INCLINACIÓN de la tela con el vaivén (el péndulo).
            float rot = MathF.Sin(tiempo * 5.2f) * 0.055f;

            // LA LUZ del lugar (la tela se sombrea con el mundo).
            Color color = Lighting.GetColor(
                (int)(drawPlayer.Center.X / 16f), (int)(drawPlayer.Center.Y / 16f));

            drawInfo.DrawDataCache.Add(new DrawData(
                tex, pos, frame, color * 0.98f, rot,
                new Vector2(ANCHO * 0.5f, 48.16f), 1f,
                drawPlayer.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                0f));
        }
    }
}
