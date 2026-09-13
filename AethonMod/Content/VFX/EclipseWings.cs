using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// EclipseWings — v6.12 — LAS ALAS DE ECLIPSE TOTAL.
    ///
    /// El momento más hermoso del cielo: la luna negra tapando el sol con
    /// la corona brillando a su alrededor. Cada ala es un ECLIPSE en miniatura:
    ///
    ///   · El DISCO NEGRO (GlowOrb alpha 1.0): la luna, SÓLIDA, con la
    ///     silueta recortada en escalón (dos discos por lado: uno grande
    ///     arriba y uno menor debajo — la forma del ala).
    ///   · El ANILLO CROMOSFÉRICO: un aro fino blanco-rosado EXACTAMENTE en
    ///     el borde del disco (la cromosfera solar asomando) — NÍTIDO (0.9).
    ///   · LOS RAYOS DE LA CORONA: 5-6 penachos blancos de LONGITUDES
    ///     DESIGUALES radiando de detrás del disco, ONDEANDO cada uno con su
    ///     propia fase (alfas 0.72/0.55 — se leen a distancia).
    ///   · El PROMINENTE: una pequeña llamarada rosa asomando por el limbo.
    ///
    /// v6.12: discos MÁS GRANDES y todo el conjunto al alfa de corona.
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
                //  LA MEMBRANA DEL ECLIPSE (v6.12 — FIX VLM: "orbs sueltos")
                // ============================================================
                // El CUERPO del ala: una cuña de noche violeta que CONECTA la
                // raíz con los dos lóbulos (mayor arriba-afuera, menor
                // abajo-afuera). Sin ella los discos leen como orbs flotantes;
                // con ella, el conjunto lee como ALA NEGRA cuyas puntas son
                // eclipses. Relleno baricéntrico con quads que se afilan hacia
                // los extremos.
                Vector2 tipUp = root + new Vector2(
                    side * (9f + 13f * o), -(2f + 9f * o) * ctx.GravDir);
                Vector2 tipDn = root + new Vector2(
                    side * (20f + 17f * o), (10f + 9f * o) * ctx.GravDir);
                tipUp.Y += flap * 1.8f * ctx.GravDir;
                tipDn.Y += flap * 1.8f * ctx.GravDir;
                for (int m = 0; m < 30; m++)
                {
                    float mt = VFXCore.Hash01(side, m, 51);        // a lo largo del ala
                    float ms = VFXCore.Hash01(side, m, 52);        // entre los dos lóbulos
                    // Punto interior (más denso hacia la raíz, hueco hacia las puntas).
                    Vector2 a = Vector2.Lerp(root, tipUp, mt);
                    Vector2 b = Vector2.Lerp(root, tipDn, mt);
                    Vector2 p = Vector2.Lerp(a, b, 0.15f + 0.70f * ms);
                    float taper = 1f - mt * 0.45f;                 // afila hacia las puntas
                    Color vol = Color.Lerp(new Color(30, 16, 44), new Color(48, 26, 66),
                        VFXCore.Hash01(side, m, 53));
                    VFXCore.Quad(p, vol * (0.52f * taper * ctx.Alpha * (0.5f + 0.5f * o)),
                        new Vector2(13f * taper + 3f, 11f * taper + 3f));
                }
                // El FILO SUPERIOR de la membrana: cromosfera pálida de la
                // raíz al lóbulo mayor (la línea que grita "ala").
                Vector2 prevL = root;
                for (int e = 0; e <= 8; e++)
                {
                    float t = e / 8f;
                    Vector2 p = Vector2.Lerp(root, tipUp, t)
                        + new Vector2(0f, -4.5f * (1f - t * 0.4f) * ctx.GravDir);
                    Vector2 d = p - prevL;
                    float rot = d.LengthSquared() > 0.0001f ? (float)Math.Atan2(d.Y, d.X) : 0f;
                    Color ec = Color.Lerp(new Color(255, 230, 240), new Color(210, 190, 235), t);
                    VFXCore.Quad(p, ec * (0.85f * ctx.Alpha * (0.5f + 0.5f * o)),
                        new Vector2(7.5f, 3.4f), rot, VFXCore.SoftGlow);
                    prevL = p;
                }

                // ============================================================
                //  LOS DOS DISCOS DEL ECLIPSE (la silueta escalonada del ala)
                // ============================================================
                for (int disc = 0; disc < 2; disc++)
                {
                    bool major = disc == 0;
                    float R = (major ? 19f : 12.5f) * (0.42f + 0.58f * o);
                    // v6.12 — FIX DE GEOMETRÍA (VLM: "los discos flotan junto
                    // a la cabeza"): el disco mayor va AFUERA y apenas arriba
                    // (lóbulo superior de un ala de verdad, no un halo) y el
                    // menor claramente abajo-afuera (lóbulo inferior).
                    Vector2 center = root + (major
                        ? new Vector2(side * (9f + 13f * o) * (1f + sweep * 0.3f),
                                      -(2f + 9f * o) * ctx.GravDir)
                        : new Vector2(side * (20f + 17f * o) * (1f + sweep * 0.3f),
                                      (10f + 9f * o) * ctx.GravDir));
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
                        float thick = 3.2f * (1f - 0.35f * (r % 2)) * (0.6f + 0.4f * o);
                        Color rayCol = Color.Lerp(new Color(235, 240, 255), new Color(255, 225, 235),
                            VFXCore.Hash01(side + disc, r, 32));
                        VFXCore.Quad(rayPos, rayCol * (0.72f * ctx.Alpha * (0.5f + 0.5f * o)),
                            new Vector2(len, thick), rayRot, VFXCore.SoftGlow);
                        // Núcleo del rayo: línea fina más brillante.
                        VFXCore.Quad(rayPos, new Color(255, 250, 255) * (0.55f * ctx.Alpha * (0.5f + 0.5f * o)),
                            new Vector2(len * 0.85f, thick * 0.4f), rayRot, VFXCore.SoftGlow);
                    }

                    // --- EL ANILLO CROMOSFÉRICO (aro fino en el borde) ---
                    VFXCore.Quad(center, new Color(255, 214, 226) * (0.90f * ctx.Alpha * (0.5f + 0.5f * o)),
                        VFXCore.RingQuadSize(R * 1.03f), VFXCore.Ring);

                    // --- EL DISCO NEGRO (la luna: encima de todo lo demás) ---
                    VFXCore.Quad(center, Color.Black * (1.00f * ctx.Alpha),
                        new Vector2(R * 2f, R * 2f), VFXCore.GlowOrb);

                    // --- EL PROMINENTE (llamarada rosa en el limbo) ---
                    if (major)
                    {
                        float promPhase = ctx.Time * 0.9f;
                        float prom = 0.5f + 0.5f * (float)Math.Sin(promPhase);
                        Vector2 promPos = center + new Vector2(
                            side * R * 0.62f, -R * 0.78f * ctx.GravDir);
                        VFXCore.Quad(promPos, new Color(255, 140, 170) * (0.75f * prom * ctx.Alpha),
                            new Vector2(5.5f + 2.5f * prom, 4.0f + 1.8f * prom));
                    }
                }

                // La NOCHE que llevas encima: un halo de penumbra violácea MUY
                // sutil alrededor de todo el ala (la luz se apaga cerca de un
                // eclipse — pintado aditivo oscuro púrpura).
                Vector2 dusk = root + new Vector2(side * 16f * o, -5f * ctx.GravDir * o);
                VFXCore.Quad(dusk, new Color(38, 20, 60) * (0.45f * ctx.Alpha * o),
                    new Vector2(68f * (0.6f + 0.4f * o), 50f * (0.6f + 0.4f * o)));
            }
        }
    }
}
