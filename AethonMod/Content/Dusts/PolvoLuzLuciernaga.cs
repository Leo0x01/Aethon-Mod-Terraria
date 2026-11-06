using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Effects.AuraJuicio;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Dusts
{
    /// <summary>
    /// PolvoLuzLuciernaga — v6.104 — EL POLVO DE LUZ DORADA DEL AURA DE
    /// JUICIO (el “LightDust” del original, hecho casa).
    ///
    /// LA TÉCNICA (la hermana dorada del PolvoVacioInvertido): un
    /// NÚCLEO BLANCO-CÁLIDO (el corazón de la ascua) rodeado de un
    /// HALO GOLDENROD (la luz que respira) y una PERLA SÓLIDA al
    /// centro — dibujado a mano en 3 capas sobre el lote de polvos de
    /// vanilla (PreDraw devuelve false: el sprite declarado nunca se
    /// dibuja).
    ///
    /// EL TRUCO DE LA CONVERGENCIA (la técnica VelChangingSpark del
    /// original hecha ModDust): las chispas de la quemadura arrancan
    /// en ABANICO (startVel = rumbo ± 0.6·intensidad rad) y han de
    /// CONVERGER al rumbo recto que se aleja del portador. El ModDust
    /// no tiene campos libres… salvo uno: dust.rotation es INVISIBLE
    /// en un glow redondo — así que VIAJA COMO EL RUMBO de
    /// convergencia (quien lo spawnea lo escribe con
    /// velFin.ToRotation()); en Update la velocidad gira suavemente
    /// hacia él (la casa: cero alocaciones, cero arrays).
    ///
    /// Emite luz dorada ∝ escala: cada luciérnaga es una vela perdida
    /// (salvo noLightEmittence — el campo del aura pone SU luz).
    /// </summary>
    public class PolvoLuzLuciernaga : ModDust
    {
        public override string Texture => "AethonMod/Content/Dusts/PolvoLuzLuciernaga";

        public override void OnSpawn(Dust dust)
        {
            dust.scale *= Main.rand.NextFloat(0.8f, 1f);
        }

        public override bool Update(Dust dust)
        {
            // ==============================================================
            //  LA CONVERGENCIA — la velocidad gira hacia EL RUMBO que
            //  viaja en dust.rotation (el abanico de la quemadura se
            //  cierra en la línea que aleja del portador: la chispa
            //  EXPULSADA del original). Solo si el rumbo es válido
            //  (arranca en 0: sin rumbo, vuela libre).
            // ==============================================================
            float vel = dust.velocity.Length();
            if (vel > 0.02f && dust.rotation != 0f)
            {
                Vector2 rumbo = new Vector2(System.MathF.Cos(dust.rotation),
                    System.MathF.Sin(dust.rotation));
                dust.velocity = Vector2.Normalize(Vector2.Lerp(dust.velocity, rumbo * vel, 0.14f)) * vel;
            }

            // EL FRENADO suave de la ascua (pierde ímpetu, no muere de golpe).
            dust.velocity *= 0.97f;

            // LA VIDA: sin gravedad la ascua CRECE al consumirse; con
            // gravedad se deshace (la hermana del vacío invertido).
            if (dust.noGravity)
                dust.scale += 0.015f;
            else
                dust.scale -= 0.01f;

            // LA VELA DORADA — luz del color del halo (salvo silencio).
            float luz = MathHelper.Clamp(dust.scale * 0.7f, 0f, 1f);
            if (!dust.noLightEmittence)
                Lighting.AddLight(dust.position, dust.color.ToVector3() * luz);

            return true;
        }

        public override bool PreDraw(Dust dust)
        {
            Texture2D glow = RecursosAuraJuicio.BloomChico ?? VFXCore.SoftGlow;
            Texture2D solido = VFXCore.GlowOrb;
            if (glow == null)
                return false;

            float desvanecer = Utils.GetLerpValue(255, 0, dust.alpha);
            Vector2 origenGlow = new Vector2(glow.Width, glow.Height) * 0.5f;
            Vector2 origenSolido = new Vector2(solido.Width, solido.Height) * 0.5f;
            Vector2 pos = dust.position - Main.screenPosition;

            // === 1. EL HALO GOLDENROD: la luz que respira (aditiva por
            //     el tinte — el halo es la ascua vista de lejos). ===
            Color halo = dust.color;
            halo.A = 0;
            Main.spriteBatch.Draw(glow, pos, null,
                halo * (0.8f * desvanecer), 0f, origenGlow,
                dust.scale * 0.5f, SpriteEffects.None, 0f);

            // === 2. EL NÚCLEO BLANCO-CÁLIDO: el corazón de la ascua. ===
            Color nucleo = Color.Lerp(dust.color, Color.White, 0.65f);
            nucleo.A = 0;
            Main.spriteBatch.Draw(glow, pos, null,
                nucleo * (0.55f * desvanecer), 0f, origenGlow,
                dust.scale * 0.22f, SpriteEffects.None, 0f);

            // === 3. LA PERLA SÓLIDA: el punto más caliente. ===
            if (!dust.noLight)
                Main.spriteBatch.Draw(solido, pos, null,
                    nucleo * (0.75f * desvanecer), 0f, origenSolido,
                    dust.scale * 0.05f, SpriteEffects.None, 0f);

            return false;
        }
    }
}
