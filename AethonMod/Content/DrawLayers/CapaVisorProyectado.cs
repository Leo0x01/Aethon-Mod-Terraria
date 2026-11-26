using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.DrawLayers
{
    // ======================================================================
    //  CapaVisorProyectado — v6.177 — EL VISOR VIVE EN LA CARA.
    //
    //  El plano del meme: «te quitas el visor, se te encienden los ojos
    //  y sueltas un rayo enorme» — la capa dibuja:
    //   · el VISOR en la cabeza del portador mientras lo lleva en la
    //     mano (con SU lente magenta y un vaivén sutil — respira);
    //   · LA PROYECCIÓN: durante el rayo grande, el visor SE LEVANTA
    //     flotando encima de la cabeza (el retiro de la animación) y el
    //     OJO de abajo SE ENCIENDE de rojo (el brillo — el que atraviesa
    //     TODO es el ojo, no el visor).
    // ======================================================================
    public class CapaVisorProyectado : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
            => new AfterParent(PlayerDrawLayers.FaceAcc);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (p == null || p.dead || p.whoAmI < 0 || p.invis) return false;
            return p.HeldItem != null && p.HeldItem.type == ModContent.ItemType
                <global::AethonMod.Content.Items.Investigaciones.VisorDelProyectado>();
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            try
            {
                Player p = drawInfo.drawPlayer;
                if (p == null) return;
                var pr = p.GetModPlayer<global::AethonMod.Content.Players.ProyectadoPlayer>();
                if (pr == null) return;

                Texture2D tex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Items/Investigaciones/VisorDelProyectado",
                    (AssetRequestMode)2).Value;
                float t = Main.GlobalTimeWrappedHourly;

                // LA POSICIÓN DE LA CABEZA (el ancla de las coronas de la casa)
                Vector2 cabeza = new Vector2(p.Center.X, p.position.Y + p.height * 0.18f) - Main.screenPosition;
                float retiro = pr.VisorFuera ? pr.Retiro : 0f;
                float brillo = pr.VisorFuera ? pr.BrilloOjos : 0f;

                // (1) EL OJO ENCENDIDO (la proyección): el brillo de abajo
                if (brillo > 0.05f)
                {
                    Vector2 ojo = cabeza + new Vector2(0f, -2f);
                    VFXCore.AbrirAdditive();
                    try
                    {
                        Main.spriteBatch.Draw(VFXCore.SoftGlow, ojo, null,
                            new Color(255, 45, 20) * (0.55f * brillo), 0f,
                            VFXCore.SoftGlow.Size() * 0.5f,
                            new Vector2(0.85f * brillo, 0.85f * brillo), SpriteEffects.None, 0f);
                        Main.spriteBatch.Draw(VFXCore.SoftGlow, ojo, null,
                            new Color(255, 220, 200) * (0.6f * brillo), 0f,
                            VFXCore.SoftGlow.Size() * 0.5f,
                            new Vector2(0.34f * brillo, 0.34f * brillo), SpriteEffects.None, 0f);
                    }
                    finally { VFXCore.RestaurarLoteVanilla(); }
                }

                // (2) EL VISOR: en la cara… o LEVITÁNDOSE (la proyección lo sube)
                float esc = 0.86f;
                Vector2 pos = cabeza + new Vector2(
                    p.direction * 1f,
                    -3f - retiro * 26f + MathF.Sin(t * 1.6f) * (0.8f + retiro * 3.2f));
                float giro = (p.direction < 0 ? -0.06f : 0.06f) - retiro * 0.45f * p.direction;
                Color luz = Lighting.GetColor(p.Center.ToTileCoordinates());
                Main.spriteBatch.Draw(tex, pos, null,
                    Color.Lerp(luz, Color.White, 0.35f) * (1f - retiro * 0.25f),
                    giro, tex.Size() * 0.5f, esc, p.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

                // (3) LA LENTE VIVA: el brillo magenta del ojo del visor (late)
                VFXCore.AbrirAdditive();
                try
                {
                    Vector2 lente = pos + new Vector2(p.direction * 1f, 0f);
                    float late = 0.5f + 0.5f * MathF.Sin(t * 3.1f + p.whoAmI);
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, lente, null,
                        new Color(255, 90, 200) * (0.22f + 0.10f * late) * (1f - retiro * 0.8f),
                        0f, VFXCore.SoftGlow.Size() * 0.5f,
                        new Vector2(0.42f, 0.24f), SpriteEffects.None, 0f);
                }
                finally { VFXCore.RestaurarLoteVanilla(); }
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("CapaVisorProyectado", exRegistro); }
        }
    }
}
