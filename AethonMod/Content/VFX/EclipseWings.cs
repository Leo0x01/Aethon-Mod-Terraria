using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// EclipseWings — v6.08 — LAS ALAS DE ECLIPSE TOTAL.
    ///
    /// El momento más hermoso del cielo: la luna negra tapando el sol con
    /// la corona brillando a su alrededor. Cada ala es un ECLIPS en miniatura:
    ///
    ///   · El DISCO NEGRO (GlowOrb alpha alta): la luna, sólida, con la
    ///     silueta recortada en escalón (dos discos por lado: uno grande
    ///     arriba y uno menor debajo — la forma del ala).
    ///   · El ANILLO CROMOSFÉRICO: un aro fino blanco-rosado EXACTAMENTE en
    ///     el borde del disco (la cromosfera solar asomando).
    ///   · LOS RAYOS DE LA CORONA: 5-6 penachos blancos de LONGITUDES
    ///     DESIGUALES (la corona real es irregular: hay streamers largos y
    ///     cortos) radiando de detrás del disco, ONDEANDO cada uno con su
    ///     propia fase, como banderas al viento solar.
    ///   · El PROMINENTE: una pequeña llamarada rosa asomando por el limbo.
    ///
    /// Vuelo MAJESTUOSO y lento (0.17 rad/tick): un eclipse no tiene prisa.
    /// </summary>
    public static class EclipseWings
    {
        /// <summary>Rayos de corona por disco.</summary>
        private const int Rays = 6;

        /// <summary>Pinta las alas de eclipse en el buffer de VFXCore.</summary>
        public static void Render(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.15f);

            float flap = (float)Math.Sin(ctx.FlapPhase) * ctx.FlapAmp;
            float o = open * VFXCore.Breathe(ctx.Time, 1.2f, 0f, 0.025f);
            float sweep = MathHelper.Clamp(Math.Abs(ctx.SpeedX) / 9f, 0f, 1f) * 0.40f;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 root = ctx.Back + new Vector2(side * 6f, 0f);

                // ============================================================
                //  LOS DOS DISCOS DEL ECLIPSE (la silueta escalonada del ala)
                // ============================================================
                for (int disc = 0; disc < 2; disc++)
                {
                    bool major = disc == 0;
                    float R = (major ? 16f : 10.5f) * (0.42f + 0.58f * o);
                    Vector2 center = root + (major
                        ? new Vector2(side * (6f + 10f * o) * (1f + sweep * 0.3f),
                                      -(6f + 12f * o) * ctx.GravDir)
                        : new Vector2(side * (15f + 16f * o) * (1f + sweep * 0.3f),
                                      (7f + 6f * o) * ctx.GravDir));
                    center.Y += flap * 1.8f * ctx.GravDir;

                    // --- LOS RAYOS DE LA CORONA (detrás del disco) ---
                    // Irregulares como la corona real: longitudes desiguales
                    // deterministas y ángulos abriéndose en abanico.
                    for (int r = 0; r < Rays; r++)
                    {
                        // Ángulo: abanico abierto hacia fuera y arriba.
                        float ang = MathHelper.Lerp(0.15f, MathHelper.Pi - 0.15f, r / (float)(Rays - 1));
                        // En pantalla: 0 = derecha del disco... giramos al espacio del ala.
                        float dirX = (float)Math.Cos(ang) * side;
                        float dirY = -(float)Math.Sin(ang) * ctx.GravDir;

                        // Longitud DESIGUAL (los streamers de la corona real).
                        float baseLen = (R * 1.05f + R * 0.95f * VFXCore.Hash01(side + disc, r, 31))
                                        * (0.5f + 0.5f * o);
                        // ONDEO: cada rayo ondea con su propia fase (viento solar).
                        float wave = VFXCore.Sway(ctx.Time, 1.0f + 0.35f * (r % 3), r * 1.9f + disc * 3.1f);
                        float len = baseLen * (1f + 0.10f * wave * ctx.GravDir);

                        // El rayo: cinta estrecha alargada desde el borde del disco.
                        Vector2 rayPos = center + new Vector2(dirX, dirY) * (R + len * 0.5f);
                        float rayRot = (float)Math.Atan2(dirY, dirX);
                        float thick = 2.6f * (1f - 0.35f * (r % 2)) * (0.6f + 0.4f * o);
                        Color rayCol = Color.Lerp(new Color(235, 240, 255), new Color(255, 225, 235),
                            VFXCore.Hash01(side + disc, r, 32));
                        VFXCore.Quad(rayPos, rayCol * (0.42f * ctx.Alpha * (0.5f + 0.5f * o)),
                            new Vector2(len, thick), rayRot, VFXCore.SoftGlow);
                        // Núcleo del rayo: línea fina más brillante.
                        VFXCore.Quad(rayPos, new Color(255, 250, 255) * (0.30f * ctx.Alpha * (0.5f + 0.5f * o)),
                            new Vector2(len * 0.85f, thick * 0.4f), rayRot, VFXCore.SoftGlow);
                    }

                    // --- EL ANILLO CROMOSFÉRICO (aro fino en el borde) ---
                    VFXCore.Quad(center, new Color(255, 214, 226) * (0.55f * ctx.Alpha * (0.5f + 0.5f * o)),
                        VFXCore.RingQuadSize(R * 1.03f), VFXCore.Ring);

                    // --- EL DISCO NEGRO (la luna: encima de todo lo demás) ---
                    VFXCore.Quad(center, Color.Black * (0.93f * ctx.Alpha),
                        new Vector2(R * 2f, R * 2f), VFXCore.GlowOrb);

                    // --- EL PROMINENTE (llamarada rosa en el limbo) ---
                    if (major)
                    {
                        float promPhase = ctx.Time * 0.9f;
                        float prom = 0.5f + 0.5f * (float)Math.Sin(promPhase);
                        Vector2 promPos = center + new Vector2(
                            side * R * 0.62f, -R * 0.78f * ctx.GravDir);
                        VFXCore.Quad(promPos, new Color(255, 140, 170) * (0.5f * prom * ctx.Alpha),
                            new Vector2(4.5f + 2.5f * prom, 3.2f + 1.8f * prom));
                    }
                }

                // La NOCHE que llevas encima: un halo de penumbra violácea MUY
                // sutil alrededor de todo el ala (la luz se apaga cerca de un
                // eclipse — pintado aditivo oscuro púrpura).
                Vector2 dusk = root + new Vector2(side * 14f * o, -4f * ctx.GravDir * o);
                VFXCore.Quad(dusk, new Color(38, 20, 60) * (0.35f * ctx.Alpha * o),
                    new Vector2(60f * (0.6f + 0.4f * o), 44f * (0.6f + 0.4f * o)));
            }
        }
    }
}
