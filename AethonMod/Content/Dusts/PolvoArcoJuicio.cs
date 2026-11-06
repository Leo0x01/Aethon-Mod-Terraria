using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Dusts
{
    /// <summary>
    /// PolvoArcoJuicio — v6.106 — LA CHISPA QUE RECORRE LA MEDIALUNA.
    ///
    /// LA LETRA DEL USUARIO (la corrección del aura de juicio 2): «las
    /// particulas deben moverse en la direccion opuesto al jugador, o
    /// sea el jugador esta a la derecha del enemigo y las particulas
    /// del enemigo se mueven a la izquierda, asi para todas las
    /// direcciones, ademas cada particula se debe liberar y deben tener
    /// el mismo recorrido que el semcirculo, la velocidad de las
    /// particulas debe ir cada vez mas rapido a medida que se acercan
    /// al jugador».
    ///
    /// EL CONTRATO: cada chispa nace LIBERADA con su arco propio — el
    /// centro, el radio y los dos cuernos quedan congelados AL NACER
    /// (aunque el impío camine, la chispa termina SU camino) — y lo
    /// RECORRE ENTERO: EL MISMO CAMINO del semicírculo Perlin que
    /// dibuja el ancla. Parte del cuerno de arranque ALEJÁNDOSE del
    /// portador (el jugador a la derecha → la chispa corre a la
    /// IZQUIERDA), envuelve la cúpula por la espalda del impío y llega
    /// al cuerno de llegada ACELERANDO — cada vez más rápida en el
    /// tramo que VUELVE hacia el jugador, y allí, en la última quinta
    /// parte del arco, se disuelve.
    ///
    /// Tuberías de la casa: el Update devuelve FALSE — TODO el
    /// movimiento lo escribe la geometría del arco (la física vanilla
    /// no participa: ni gravedad ni velocidad). El dibujo es el de la
    /// casa: NÚCLEO NEGRO (el vacío que oscurece) + HALO VIOLETA
    /// ESTIRADO por la tangente (la estela del barrido — la chispa SE
    /// LEE en movimiento) + PERLA SÓLIDA al centro.
    /// </summary>
    public class PolvoArcoJuicio : ModDust
    {
        /// <summary>EL ARCO de una chispa liberada — la geometría del
        /// semicírculo congelada al nacer (lo que le toca recorrer).</summary>
        public class Arco
        {
            /// <summary>El corazón del impío AL NACER la chispa —
            /// LIBERADA: ya no lo sigue, termina su camino sola.</summary>
            public Vector2 Centro;

            /// <summary>El cuerno de ARRANQUE (el ángulo donde nace).</summary>
            public float Ang0;

            /// <summary>Dónde va ahora.</summary>
            public float Ang;

            /// <summary>El cuerno de LLEGADA — el fin del semicírculo.</summary>
            public float AngFin;

            /// <summary>La velocidad angular VIVA (sube cada tick).</summary>
            public float VelAng;

            /// <summary>LA ACELERACIÓN — cada vez más rápido.</summary>
            public float Acel;

            /// <summary>El radio del semicírculo que recorre.</summary>
            public float Radio;

            /// <summary>El seguro de vida (nadie vive para siempre).</summary>
            public int Edad;
        }

        // El sprite declarado JAMÁS se dibuja (TODO el render es manual,
        // el patrón de la casa) — la textura es la del polvo hermano.
        public override string Texture => "AethonMod/Content/Dusts/PolvoVacioInvertido";

        public override bool Update(Dust dust)
        {
            if (dust.customData is not Arco a)
            {
                dust.active = false;
                return false;
            }

            a.Edad++;

            // CADA VEZ MÁS RÁPIDO — la aceleración del juicio: la chispa
            // arranca suave y NUNCA frena (la velocidad angular sube cada
            // tick — el tramo final, el que VUELVE hacia el jugador, es el
            // más veloz de todo el camino).
            a.VelAng = MathF.Min(a.VelAng + a.Acel, 0.34f);
            a.Ang += a.VelAng;

            // EL RECORRIDO DEL SEMICÍRCULO — la posición la escribe LA
            // GEOMETRÍA, no la física: el mismo camino de la medialuna.
            dust.position = a.Centro + new Vector2(MathF.Cos(a.Ang), MathF.Sin(a.Ang)) * a.Radio;
            dust.velocity = Vector2.Zero;

            // LA TANGENTE del barrido — la estela (el halo se estira aquí).
            dust.rotation = new Vector2(-MathF.Sin(a.Ang), MathF.Cos(a.Ang)).ToRotation();

            // el crecimiento tibio de la casa (el vacío engorda al absorber)
            dust.scale += 0.004f;

            // EL FINAL DEL CAMINO — el cuerno de llegada: la chispa se
            // disuelve en la ÚLTIMA QUINTA parte del arco (el tramo que
            // se acerca al jugador: llega volando y se apaga).
            float recorrido = (a.Ang - a.Ang0) / (a.AngFin - a.Ang0);
            if (float.IsNaN(recorrido) || recorrido >= 1f || a.Edad > 130)
            {
                dust.active = false;
                return false;
            }
            float apagado = MathHelper.Clamp((recorrido - 0.80f) / 0.20f, 0f, 1f);
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

            // === 2. EL HALO VIOLETA ESTIRADO POR LA TANGENTE — la
            //     estela del barrido: la chispa SE LEE en movimiento
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
