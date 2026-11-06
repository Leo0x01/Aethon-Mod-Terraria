using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Dusts
{
    /// <summary>
    /// PolvoExpulsionJuicio — v6.107 — LA CHISPA EXPULSADA DEL CORAZÓN.
    ///
    /// LA LETRA DEL USUARIO (la corrección del aura de juicio 2): «las
    /// particulas deben nacer del centro de los enemigos y luego
    /// dispersarce hacia fuera pero siempre en la direccion opuesta al
    /// jugador».
    ///
    /// EL CONTRATO: cada chispa NACE EN EL CORAZÓN del impío (una cuna
    /// con un temblor mínimo) y sale EXPULSADA hacia FUERA, SIEMPRE en
    /// la dirección OPUESTA al portador (el jugador a la derecha → la
    /// chispa nace del pecho y vuela a la IZQUIERDA, abriéndose en
    /// abanico hacia afuera — así para TODAS las direcciones). La
    /// geometría queda CONGELADA al nacer (LIBERADA: aunque el impío
    /// camine, la chispa termina su vuelo sola) — el rumbo es RECTO y
    /// la velocidad ACELERA suavecito cada tick (el vacío la escupe
    /// cada vez con más rabia) hasta disolverse en la última cuarta
    /// parte del camino.
    ///
    /// Tuberías de la casa: el Update devuelve FALSE — TODO el
    /// movimiento lo escribe la geometría de la expulsión (la física
    /// vanilla no participa: ni gravedad ni velocidad). El dibujo es
    /// el de la casa: NÚCLEO NEGRO (el vacío que oscurece) + HALO
    /// VIOLETA ESTIRADO por el rumbo (la estela del vuelo — la chispa
    /// SE LEE en movimiento) + PERLA SÓLIDA al centro.
    /// </summary>
    public class PolvoExpulsionJuicio : ModDust
    {
        /// <summary>EL VUELO de una chispa liberada — la geometría de
        /// la expulsión congelada al nacer (lo que le toca recorrer).</summary>
        public class Chispa
        {
            /// <summary>El corazón del impío AL NACER la chispa —
            /// LIBERADA: ya no lo sigue, termina su vuelo sola.</summary>
            public Vector2 Centro;

            /// <summary>EL RUMBO — la dirección de la expulsión (SIEMPRE
            /// dentro del abanico OPUESTO al portador).</summary>
            public Vector2 Dir;

            /// <summary>Lo recorrido desde la cuna (crece cada tick).</summary>
            public float Dist;

            /// <summary>La velocidad VIVA (acelera cada tick).</summary>
            public float Vel;

            /// <summary>LA ACELERACIÓN — cada vez más rápida.</summary>
            public float Acel;

            /// <summary>El final del camino (hasta dónde vive).</summary>
            public float DistMax;

            /// <summary>El seguro de vida (nadie vive para siempre).</summary>
            public int Edad;
        }

        // El sprite declarado JAMÁS se dibuja (TODO el render es manual,
        // el patrón de la casa) — la textura es la del polvo hermano.
        public override string Texture => "AethonMod/Content/Dusts/PolvoVacioInvertido";

        public override bool Update(Dust dust)
        {
            if (dust.customData is not Chispa c)
            {
                dust.active = false;
                return false;
            }

            c.Edad++;

            // CADA VEZ MÁS RÁPIDA — la aceleración de la expulsión: la
            // chispa arranca suave y NUNCA frena (la velocidad sube cada
            // tick — el vacío la escupe con rabia creciente).
            c.Vel = MathF.Min(c.Vel * (1f + c.Acel), 7.5f);
            c.Dist += c.Vel;

            // EL VUELO RECTO — la posición la escribe LA GEOMETRÍA, no la
            // física: nació en el corazón del impío y se aleja en SU
            // rumbo (SIEMPRE el abanico opuesto al portador).
            dust.position = c.Centro + c.Dir * c.Dist;
            dust.velocity = Vector2.Zero;
            dust.rotation = c.Dir.ToRotation();  // la estela apunta por donde vuela

            // el crecimiento tibio de la casa (el vacío engorda al escapar)
            dust.scale += 0.004f;

            // EL FINAL DEL CAMINO — la chispa se disuelve en la ÚLTIMA
            // CUARTA parte de su vuelo (llega volando y se apaga).
            float recorrido = c.Dist / c.DistMax;
            if (float.IsNaN(recorrido) || recorrido >= 1f || c.Edad > 90)
            {
                dust.active = false;
                return false;
            }
            float apagado = MathHelper.Clamp((recorrido - 0.75f) / 0.25f, 0f, 1f);
            dust.alpha = (int)(240f * apagado);

            // LA VELA PERDIDA — la luz tenue del vacío violeta.
            if (!dust.noLightEmittence)
            {
                float luz = MathHelper.Clamp(dust.scale * 0.9f, 0f, 0.5f);
                Lighting.AddLight(dust.position, dust.color.ToVector3() * luz);
            }

            return false;   // TODO manual: manda la geometría, no la física
        }

        public override bool PreDraw(Dust dust)
        {
            Texture2D glow = VFXCore.SoftGlow;
            Texture2D solido = VFXCore.GlowOrb;
            if (glow == null || solido == null)
                return false;

            float fade = Utils.GetLerpValue(255, 0, dust.alpha);
            Vector2 origenGlow = new Vector2(glow.Width, glow.Height) * 0.5f;
            Vector2 origenSolido = new Vector2(solido.Width, solido.Height) * 0.5f;
            Vector2 pos = dust.position - Main.screenPosition;

            // === 1. EL NÚCLEO NEGRO: el vacío OSCURECE (alpha blend). ===
            Main.spriteBatch.Draw(glow, pos, null,
                Color.Black * (0.42f * fade), dust.rotation, origenGlow,
                dust.scale * 0.40f, SpriteEffects.None, 0f);

            // === 2. EL HALO VIOLETA ESTIRADO POR EL RUMBO — la
            //     estela del vuelo: la chispa SE LEE en movimiento
            //     (dos pasadas: la larga tenue y la corta viva). ===
            Color halo = dust.color;
            halo.A = 0;
            Main.spriteBatch.Draw(glow, pos, null,
                halo * (0.65f * fade), dust.rotation, origenGlow,
                new Vector2(dust.scale * 0.34f, dust.scale * 0.15f),
                SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glow, pos, null,
                halo * fade, dust.rotation, origenGlow,
                new Vector2(dust.scale * 0.19f, dust.scale * 0.10f),
                SpriteEffects.None, 0f);

            // === 3. LA PERLA SÓLIDA: el resto condensado. ===
            Main.spriteBatch.Draw(solido, pos, null,
                halo * (0.75f * fade), dust.rotation, origenSolido,
                dust.scale * 0.035f, SpriteEffects.None, 0f);

            return false;
        }
    }
}
