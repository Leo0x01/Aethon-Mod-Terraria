using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// ESTALLIDOLUZLIB — v6.184 — LA EXPLOSIÓN DE LUZ CANÓNICA DE LA CASA.
    ///
    /// LA LETRA (la sesión del Sellado): «la explosion de luz real que
    /// debes usar es la explosion de luz de la mascota de luz Pequeño
    /// Recuerdo de Aethon, esa explosion de luz es la real y debe ser
    /// tomada como base y usada en cualquier cosa que requiera una
    /// explosion de luz, solo debes escalar su tamaño».
    ///
    /// LA FUENTE: el estallido del AethonMenorPet (MiniEstallidoPet — la
    /// mascota de luz «Pequeño Recuerdo de Aethon»), portado TAL CUAL a
    /// librería para que CUALQUIER consumidor tenga exactamente la misma
    /// explosión — los RAYOS (14: núcleo blanco + halo oro→ámbar→brasa),
    /// LA CRUZ ANAMÓRFICA, EL NÚCLEO que inunda (Bloom), LA ESTRELLA de
    /// 8 rayos (DestelloFinal), LA ONDA expansiva y LAS BOKEH — la
    /// receta COMPLETA de la mascota, solo que ESCALADA.
    ///
    /// CONTRATO: el llamador abre SU lote (VFXCore.Begin o el sprite
    /// batch listo) y `Dibujar` pinta la explosión de `edad` ticks
    /// (0-45 LA RECOGIDA: núcleo que se llena + 6 agujas; 45-165 EL
    /// ESPECTÁCULO: rayos, cruz, núcleo, estrella, onda y bokeh).
    /// `escala` 1.0 = la explosión de la mascota tal cual; el dios la
    /// usa a 2.2; la manifestación del Eterno a 6. TODO determinista:
    /// la misma `semilla` dibuja el MISMO estallido en todas las
    /// pantallas (R5 — Hash01, cero azar).
    /// </summary>
    public static class EstallidoLuzLib
    {
        // === LA PALETA DEL ESTALLIDO (la de la mascota — la fuente) ===
        private static readonly Color AmbarEstelar = new(255, 178, 96);
        private readonly static Color EmberPortador = new(255, 120, 60);
        private static readonly Color OroGrimorio = new(245, 196, 81);
        private static readonly Color BlancoCaliente = new(255, 240, 190);

        /// <summary>LA EXPLOSIÓN CANÓNICA (centro en COORDENADAS DE
        /// PANTALLA: `centro` ya con `- Main.screenPosition` restado).</summary>
        public static void Dibujar(SpriteBatch sb, Vector2 centro, float edad,
            float escala, int semilla)
        {
            if (sb == null || escala <= 0f) return;
            float esc = escala;

            // === FASE 1 — LA RECOGIDA (0-45 t): el núcleo que se llena
            //     y LAS 6 AGUJAS convergiendo (la carga de la mascota) ===
            if (edad <= 45f)
            {
                float c = edad / 45f;
                VFXCore.Quad(centro + Main.screenPosition, OroGrimorio * (0.20f + 0.25f * c),
                    new Vector2((30f + 34f * c) * esc, (30f + 34f * c) * esc));
                for (int i = 0; i < 6; i++)
                {
                    float angA = i * MathHelper.TwoPi / 6f + Main.GlobalTimeWrappedHourly * 0.35f;
                    float rA = MathHelper.Lerp(120f, 26f, c) * esc;
                    Vector2 punta = centro + Main.screenPosition +
                        new Vector2(MathF.Cos(angA), MathF.Sin(angA)) * rA;
                    VFXCore.Quad(punta, BlancoCaliente * (0.35f + 0.30f * c),
                        new Vector2(14f * esc, 5f * esc), angA);
                }
                return;
            }

            // === FASE 2 — EL ESPECTÁCULO (45-165 t) ===
            float tB = edad - 45f;
            float t = Main.GlobalTimeWrappedHourly;
            float crec = Math.Min(1f, tB / 12f);
            float fade = tB < 50f ? 1f : Math.Max(0f, 1f - (tB - 50f) / 70f);
            float giro = tB * 0.0045f;

            // (a) LOS RAYOS (14 — el corazón de la imagen de la mascota)
            for (int i = 0; i < 14; i++)
            {
                float h0 = VFXCore.Hash01(semilla, i, 10);
                float h1 = VFXCore.Hash01(semilla, i, 11);
                float h2 = VFXCore.Hash01(semilla, i, 12);
                float h3 = VFXCore.Hash01(semilla, i, 13);
                float ang = giro + i * MathHelper.TwoPi / 14f +
                    (h0 - 0.5f) * (MathHelper.TwoPi / 14f) * 1.15f;
                float largo = (34f + 96f * h1) * crec * esc;
                float vidaR = 60f + 40f * h2;
                float alfaR = fade * (tB < vidaR ? 1f : Math.Max(0f, 1f - (tB - vidaR) / 20f));
                if (alfaR <= 0.02f || largo < 4f) continue;
                Vector2 dirR = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
                Vector2 centroMundo = centro + Main.screenPosition;
                VFXCore.Quad(centroMundo + dirR * (7f + largo * 0.5f) * esc,
                    BlancoCaliente * (0.85f * alfaR),
                    new Vector2(largo * esc, (1.8f + 1.4f * h3) * esc), ang);
                Color cHalo = Color.Lerp(OroGrimorio,
                    Color.Lerp(AmbarEstelar, EmberPortador, h3 * 0.7f),
                    0.35f + 0.45f * h3);
                VFXCore.Quad(centroMundo + dirR * (5f + largo * 0.34f) * esc,
                    cHalo * (0.40f * alfaR),
                    new Vector2(largo * 0.68f * esc, (6.5f + 4.5f * h0) * esc), ang);
            }

            // (c) LA CRUZ ANAMÓRFICA (el bloom estirado — vive solo en
            //     el AUGE, muere a los 50 t)
            float alfaC = Math.Max(0f, 1f - tB / 50f) * 0.55f;
            if (alfaC > 0.02f)
            {
                VFXCore.Quad(centro + Main.screenPosition, BlancoCaliente * alfaC,
                    new Vector2(155f * crec * esc, 3f * esc));
                VFXCore.Quad(centro + Main.screenPosition, BlancoCaliente * alfaC,
                    new Vector2(3f * esc, 155f * crec * esc));
                VFXCore.Quad(centro + Main.screenPosition, OroGrimorio * (alfaC * 0.6f),
                    new Vector2(100f * crec * esc, 7f * esc));
                VFXCore.Quad(centro + Main.screenPosition, OroGrimorio * (alfaC * 0.6f),
                    new Vector2(7f * esc, 100f * crec * esc));
            }

            // EL NÚCLEO INUNDANDO (el blanco que crece RESPIRANDO)
            float respira = 0.95f + 0.05f * MathF.Sin(t * 4.2f);
            LumenLib.Bloom(sb, centro,
                (26f + 104f * crec) * respira * esc, BlancoCaliente, 0.95f * fade, 4);
            LumenLib.BloomPulse(sb, centro,
                (22f + 26f * crec) * esc, OroGrimorio, 0.55f * fade, t, 2.6f);

            // LA ESTRELLA DE DESTELLO (el flare de 8 rayos — el mismo
            // DestelloFinal de la mascota, girando LENTO)
            Texture2D dest = VFXCore.DestelloFinal;
            if (dest != null)
            {
                float escD = (210f * crec * esc) / dest.Width *
                    (0.92f + 0.08f * MathF.Sin(t * 3.4f));
                sb.Draw(dest, centro, null,
                    OrbitaLib.Tint(BlancoCaliente, 0.60f * fade),
                    t * 0.06f,
                    new Vector2(dest.Width, dest.Height) * 0.5f, escD,
                    SpriteEffects.None, 0f);
            }

            // LA ONDA EXPANSIVA (el frente que barre — una sola, clara)
            if (tB < 70f)
                OndaLib.Pulse(sb, centro, tB / 70f, 105f * esc,
                    OroGrimorio, 0.50f * (1f - tB / 70f), semilla);

            // LAS BOKEH (8 chispas fijas que giran despacito y titilan)
            for (int i = 0; i < 8; i++)
            {
                float h0 = VFXCore.Hash01(semilla, i, 30);
                float h1 = VFXCore.Hash01(semilla, i, 31);
                float tw = 0.35f + 0.65f *
                    (0.5f + 0.5f * MathF.Sin(t * 2.6f + i * 1.9f));
                float rB = (12f + 88f * h1) * crec * esc;
                float angB = h0 * MathHelper.TwoPi + t * 0.05f;
                Vector2 b = centro + new Vector2(MathF.Cos(angB), MathF.Sin(angB)) * rB;
                LumenLib.Bloom(sb, b, (4f + 6f * h1) * esc,
                    (i % 2 == 0) ? OroGrimorio : AmbarEstelar,
                    0.45f * fade * tw, 2);
            }
        }
    }
}
