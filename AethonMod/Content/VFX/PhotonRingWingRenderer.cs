using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// PhotonRingWingRenderer — v6.06 — LAS ALAS DEL ANILLO DE FOTONES.
    ///
    /// Segunda ala de luz 100% procedural (técnica de las coronas, tema del
    /// agujero negro carmesí). Cada ala es un ABANICO DE HOJAS DE LUZ que
    /// brotan de una micro-singularidad en la espalda:
    ///
    ///   1. MICRO-SINGULARIDAD por lado: orbe negro pequeño con halo
    ///      púrpura y un pequeño anillo de fotones (Ring).
    ///   2. CINCO HOJAS DE LUZ por lado: cadenas de cuentas pálidas que se
    ///      curvan hacia arriba (fucsia hacia la punta, núcleo blanco
    ///      pegado a la raíz) — la membrana del ala.
    ///   3. PULSOS DE FOTONES: una cuenta BLANCA brillante viaja hacia
    ///      fuera por cada hoja (la luz escapando del horizonte) con su
    ///      eco pálido detrás — el latido del ala.
    ///   4. MINI-ANILLOS en las puntas: círculos de cuentas que rotan
    ///      lentamente (el motivo del anillo de Einstein).
    ///
    /// En reposo las hojas se COMPRIMEN hacia arriba (ala plegada); al
    /// volar se despliegan en abanico completo con la onda de aleteo.
    /// </summary>
    public static class PhotonRingWingRenderer
    {
        /// <summary>Hojas de luz por lado.</summary>
        private const int Blades = 5;

        /// <summary>Cuentas por hoja (suavidad de la membrana).</summary>
        private const int BladeBeads = 12;

        /// <summary>
        /// Calcula TODAS las alas como cuadros de luz en el buffer de VFXCore
        /// (coordenadas de MUNDO, ancladas a la espalda del jugador).
        /// </summary>
        public static void ComputeQuads(Vector2 back, float open, float flapAmp,
            float flapPhase, float time, int direction, float gravDir, float alpha)
        {
            if (alpha <= 0f) return;
            open = MathHelper.Clamp(open, 0f, 1.15f);

            float flap = (float)Math.Sin(flapPhase) * flapAmp;
            float breathe = VFXCore.Breathe(time, 2.1f, 1.1f, 0.03f);
            // Compresión del abanico: plegado → hojas casi verticales.
            float fan = 0.35f + 0.65f * open;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 root = back + new Vector2(side * 7f, flap * 2.0f * gravDir);

                // ==========================================================
                //  1. LA MICRO-SINGULARIDAD (la raíz del ala)
                // ==========================================================
                float pulse = 0.8f + 0.3f * (float)Math.Sin(time * 2.2f + side * 0.9f);
                VFXCore.Quad(root, VFXPalettes.VoidQueen.HaloDeep * (0.5f * alpha),
                    new Vector2(26f, 26f));
                VFXCore.Quad(root, VFXPalettes.VoidQueen.Pale * (0.40f * pulse * alpha),
                    VFXCore.RingQuadSize(7.5f * breathe), VFXCore.Ring);
                VFXCore.Quad(root, Color.Black * (0.97f * alpha),
                    new Vector2(9.5f, 9.5f), VFXCore.GlowOrb);

                // ==========================================================
                //  2+3+4. LAS HOJAS DE LUZ + PULSOS + MINI-ANILLOS
                // ==========================================================
                for (int k = 0; k < Blades; k++)
                {
                    float frac = k / (float)(Blades - 1);
                    // Ángulo base: 62° (empinada arriba) → -4° (casi horizontal).
                    float baseAng = MathHelper.ToRadians(62f - 66f * frac);
                    // Compresión del abanico alrededor de ~30°.
                    float ang = MathHelper.ToRadians(30f) + (baseAng - MathHelper.ToRadians(30f)) * fan;
                    ang += flap * 0.08f * (1f - frac);   // el aleteo mece las hojas altas
                    Vector2 d = new Vector2((float)Math.Cos(ang), -(float)Math.Sin(ang));

                    float L = (30f + 27f * (1f - Math.Abs(frac - 0.35f) * 1.1f))
                              * (0.5f + 0.5f * open) * breathe;
                    Vector2 bladeRoot = root + d * 2.5f;

                    // --- la hoja (membrana de cuentas) ---
                    for (int b = 0; b < BladeBeads; b++)
                    {
                        float t = b / (float)(BladeBeads - 1);
                        // curva: las puntas se rizan hacia arriba
                        Vector2 pos = bladeRoot + d * (L * t)
                                      + new Vector2(0f, -t * t * 6f * gravDir);
                        Color col;
                        if (t < 0.22f)
                            col = Color.Lerp(Color.White, VFXPalettes.VoidQueen.Pale, t / 0.22f);
                        else
                            col = Color.Lerp(VFXPalettes.VoidQueen.Pale,
                                             VFXPalettes.VoidQueen.Disk, (t - 0.22f) / 0.78f);
                        float w = (2.6f - 1.1f * t) * (0.7f + 0.3f * open);
                        VFXCore.Quad(pos, col * (0.8f * alpha), new Vector2(w * 2f, w * 2f));
                    }

                    // --- el PULSO DE FOTONES (viaja hacia fuera) ---
                    float pt = (time * 1.55f + k * 0.37f + side * 0.21f) % 1f;
                    Vector2 pulsePos = bladeRoot + d * (L * pt)
                                       + new Vector2(0f, -pt * pt * 6f * gravDir);
                    VFXCore.Quad(pulsePos, Color.White * (0.9f * alpha), new Vector2(6.4f, 6.4f));
                    float pe = Math.Max(0f, pt - 0.08f);
                    Vector2 echoPos = bladeRoot + d * (L * pe)
                                      + new Vector2(0f, -pe * pe * 6f * gravDir);
                    VFXCore.Quad(echoPos, VFXPalettes.VoidQueen.Pale * (0.5f * alpha),
                        new Vector2(4.2f, 4.2f));

                    // --- MINI-ANILLO en la punta (motivo de Einstein) ---
                    if (open > 0.4f)
                    {
                        float tipAlpha = (open - 0.4f) / 0.6f * alpha;
                        Vector2 tip = bladeRoot + d * L + new Vector2(0f, -6f * gravDir);
                        for (int i = 0; i < 5; i++)
                        {
                            float a = time * 1.8f + k * 0.8f + i * MathHelper.TwoPi / 5f;
                            Vector2 bead = tip + new Vector2(
                                (float)Math.Cos(a) * 3.4f,
                                (float)Math.Sin(a) * 3.4f * gravDir);
                            VFXCore.Quad(bead, VFXPalettes.VoidQueen.Pale * (0.7f * tipAlpha),
                                new Vector2(2.6f, 2.6f));
                        }
                    }
                }
            }
        }
    }
}
