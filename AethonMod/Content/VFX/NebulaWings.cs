using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// NebulaWings — v6.08 — LAS ALAS DE NEBULOSA VIVA.
    ///
    /// Nubes de gas IGUAL que una nebulosa real (Pillars of Creation) pero
    /// en forma de alas: por lado hay 6 BLOBS de gas de distinto tamaño que
    /// DERIVAN cada uno con su propia fase determinista (la nube nunca está
    /// quieta — es VIVA), con filamentos brillantes serpenteando entre ellos
    /// y estrellas recién nacidas titilando dentro.
    ///
    /// La paleta es la de las nebulosas de emisión: magenta profundo (Hα)
    /// con bordes púrpura-lavanda y las estrellas en blanco cálido con una
    /// pizca de cian (las jóvenes y calientes).
    ///
    /// El aleteo NO mueve las alas como palas: la NUBE entera respira, se
    /// estira al empujar y se comprime al recoger — las alas de gas no tienen
    /// articulaciones, tienen TURBULENCIA.
    /// </summary>
    public static class NebulaWings
    {
        /// <summary>Blobs de gas por lado.</summary>
        private const int Clouds = 6;

        /// <summary>Estrellas incrustadas por lado.</summary>
        private const int Stars = 5;

        /// <summary>Filamentos serpenteantes por lado.</summary>
        private const int Filaments = 2;

        /// <summary>Pinta las alas de nebulosa en el buffer de VFXCore.</summary>
        public static void Render(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.15f);

            float flap = (float)Math.Sin(ctx.FlapPhase) * ctx.FlapAmp;
            float o = open;
            float sweep = MathHelper.Clamp(Math.Abs(ctx.SpeedX) / 9f, 0f, 1f) * 0.35f;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 root = ctx.Back + new Vector2(side * 5f, -2f * ctx.GravDir);

                // ============================================================
                //  LOS BLOBS DE GAS (la masa del ala)
                // ============================================================
                // Forma del ala: los blobs grandes y altos afuera, los pequeños
                // y bajos cerca del cuerpo — silueta de ala de gas en abanico.
                for (int c = 0; c < Clouds; c++)
                {
                    float c01 = c / (float)(Clouds - 1);   // 0 interior → 1 exterior
                    // Cada blob DERIVA con su propia fase (turbulencia viva).
                    float driftX = VFXCore.Sway(ctx.Time, 0.7f + 0.4f * c01, c * 2.3f + side) * 3.5f;
                    float driftY = VFXCore.Sway(ctx.Time, 0.9f + 0.3f * c01, c * 1.7f) * 2.5f;

                    float size = (14f + 15f * c01) * (0.42f + 0.58f * o)
                                 * VFXCore.Breathe(ctx.Time, 0.8f + 0.25f * c01, c * 3.1f, 0.12f);
                    float outX = (10f + 26f * c01) * (0.45f + 0.55f * o) * (1f + sweep * 0.5f);
                    float upY = (2f + 16f * c01) * (0.45f + 0.55f * o) * (1f + 0.08f * flap);

                    Vector2 center = root + new Vector2(
                        side * (outX + driftX),
                        -upY * ctx.GravDir + driftY * ctx.GravDir + flap * 2.0f);

                    // Gradiente de emisión: el CORAZÓN del blob en magenta vivo,
                    // el halo exterior púrpura-lavanda translúcido.
                    VFXCore.Quad(center, new Color(74, 18, 86) * (0.30f * ctx.Alpha * o),
                        new Vector2(size * 2.1f, size * 1.7f));
                    VFXCore.Quad(center, new Color(168, 40, 190) * (0.20f * ctx.Alpha * o),
                        new Vector2(size * 1.4f, size * 1.15f));
                    VFXCore.Quad(center, new Color(230, 95, 235) * (0.13f * ctx.Alpha * o),
                        new Vector2(size * 0.75f, size * 0.62f));
                }

                // ============================================================
                //  LOS FILAMENTOS (venas brillantes de gas)
                // ============================================================
                for (int f = 0; f < Filaments; f++)
                {
                    float f01 = f / (float)(Filaments - 1);
                    Vector2 prev = root;
                    for (int s = 0; s <= 12; s++)
                    {
                        float t = s / 12f;
                        // El filamento SERPENTEA: onda lenta viajando por la curva.
                        float wavePhase = ctx.Time * 1.4f - t * 3.2f + f * 2.6f;
                        float wig = (float)Math.Sin(wavePhase) * (3.5f + 3f * t);
                        float arcX = t * (44f + 16f * f01) * (0.45f + 0.55f * o) * (1f + sweep * 0.5f);
                        float arcY = (float)Math.Sin(t * MathHelper.Pi * 0.9f) * (20f + 14f * f01)
                                     * (0.45f + 0.55f * o) * (1f + 0.10f * flap)
                                     - t * t * 8f;
                        Vector2 pos = root + new Vector2(
                            side * arcX + wig * 0.4f * side,
                            -arcY * ctx.GravDir + wig * ctx.GravDir + flap * 2.0f);

                        Color fil = Color.Lerp(new Color(255, 130, 250),
                            new Color(255, 190, 255), t * 0.6f);
                        float th = (2.8f - 1.8f * t) * (0.6f + 0.4f * o);
                        VFXCore.Quad(pos, fil * (0.30f * ctx.Alpha * o),
                            new Vector2(th + 1.4f, th + 1.4f));
                        prev = pos;
                    }
                }

                // ============================================================
                //  LAS ESTRELLAS RECIÉN NACIDAS (titilan dentro del gas)
                // ============================================================
                for (int k = 0; k < Stars; k++)
                {
                    float u = VFXCore.Hash01(side, k, 21);
                    float v = VFXCore.Hash01(side, k, 22);
                    float outX = (8f + 38f * u) * (0.45f + 0.55f * o);
                    float upY = (4f + 26f * v) * (0.45f + 0.55f * o);
                    Vector2 starPos = root + new Vector2(
                        side * outX, -upY * ctx.GravDir + flap * 2.0f);

                    // Titileo estelar (agudo, con pausas) — cada estrella a su ritmo.
                    float phase = ctx.Time * (1.6f + 2.4f * v) + u * 41f;
                    float tw = (float)Math.Pow(0.5f + 0.5f * (float)Math.Sin(phase), 2.5);
                    if (tw > 0.04f)
                    {
                        // Las jóvenes arden cian-blanco; las viejas, cálido.
                        Color star = v > 0.55f
                            ? new Color(210, 250, 255)
                            : new Color(255, 244, 224);
                        VFXCore.Quad(starPos, star * (tw * 0.9f * ctx.Alpha),
                            new Vector2(2.6f, 2.6f));
                        // Cruz de difracción sutil en las más brillantes.
                        if (tw > 0.75f)
                        {
                            VFXCore.Quad(starPos, star * (tw * 0.35f * ctx.Alpha),
                                new Vector2(9.5f, 1.1f));
                            VFXCore.Quad(starPos, star * (tw * 0.35f * ctx.Alpha),
                                new Vector2(1.1f, 9.5f));
                        }
                    }
                }

                // El CORAZÓN de la nebulosa: cúmulo compacto en la raíz.
                float throb = 0.75f + 0.25f * (float)Math.Sin(ctx.Time * 1.9f + side);
                VFXCore.Quad(root, new Color(255, 140, 245) * (0.40f * throb * ctx.Alpha),
                    new Vector2(12f, 12f));
            }
        }
    }
}
