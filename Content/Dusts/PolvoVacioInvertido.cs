using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Dusts
{
    /// <summary>
    /// PolvoVacioInvertido — v6.41 — EL POLVO DEL VACÍO INVERTIDO.
    ///
    /// El polvo de la antimateria: un NÚCLEO NEGRO (el vacío que se come
    /// la luz — dibujado con alpha blend, OSCURECE) rodeado de un HALO
    /// DE COLOR (la materia desprendiéndose) y una PERLA SÓLIDA al centro
    /// (el resto condensado). Rueda con el signo de su velocidad, frena
    /// suave y CRECE si no pesa (el vacío engorda al absorber) o se
    /// deshace si pesa.
    ///
    /// Emite luz del color del halo (intensidad ∝ escala): el polvo del
    /// vacío es una vela perdida.
    ///
    /// Nota de tuberías: el PreDraw devuelve false (el sprite declarado
    /// nunca se dibuja — TODO el render es manual en 3 capas sobre el
    /// lote de polvos de vanilla, que va en alpha blend).
    /// </summary>
    public class PolvoVacioInvertido : ModDust
    {
        public override string Texture => "AethonMod/Content/Dusts/PolvoVacioInvertido";

        public override void OnSpawn(Dust dust)
        {
            dust.scale *= Main.rand.NextFloat(0.8f, 1f);
        }

        public override bool Update(Dust dust)
        {
            dust.rotation += System.MathF.Sign(dust.velocity.X);
            dust.velocity *= 0.98f;
            if (dust.noGravity)
                dust.scale += 0.02f;
            else
                dust.scale -= 0.01f;

            float light = MathHelper.Clamp(dust.scale * 0.8f, 0f, 1f);
            if (!dust.noLightEmittence)
                Lighting.AddLight(dust.position, dust.color.ToVector3() * light);

            return true;
        }

        public override bool PreDraw(Dust dust)
        {
            Texture2D glow = VFXCore.SoftGlow;
            Texture2D solido = VFXCore.GlowOrb;

            float fade = Utils.GetLerpValue(255, 0, dust.alpha);
            Vector2 origenGlow = new Vector2(glow.Width, glow.Height) * 0.5f;
            Vector2 origenSolido = new Vector2(solido.Width, solido.Height) * 0.5f;
            Vector2 pos = dust.position - Main.screenPosition;

            // === 1. EL NÚCLEO NEGRO: el vacío OSCURECE (alpha blend). ===
            //     (27px y 23px por unidad de escala — el corazón del efecto.)
            Main.spriteBatch.Draw(glow, pos, null,
                Color.Black * 0.4f * fade, dust.rotation, origenGlow,
                dust.scale * 0.425f, SpriteEffects.None, 0f);
            if (dust.alpha < 1)
                Main.spriteBatch.Draw(glow, pos, null,
                    Color.Black * fade, dust.rotation, origenGlow,
                    dust.scale * 0.356f, SpriteEffects.None, 0f);

            // === 2. EL HALO DE COLOR: la materia desprendiéndose. ===
            Color halo = dust.color;
            halo.A = 0;
            Main.spriteBatch.Draw(glow, pos, null,
                halo * fade, dust.rotation, origenGlow,
                dust.scale * 0.219f, SpriteEffects.None, 0f);

            // === 3. LA PERLA SÓLIDA: el resto condensado. ===
            if (!dust.noLight)
                Main.spriteBatch.Draw(solido, pos, null,
                    halo * 0.75f * fade, dust.rotation, origenSolido,
                    dust.scale * 0.0375f, SpriteEffects.None, 0f);

            return false;
        }
    }
}
