using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// EMPERATRIZFX — los FX globales de las armas de la Emperatriz que
    /// disparan el 931 VANILLA (opción B: la IA de Terraria no se toca).
    ///
    /// El 931 no usa ai[2] (solo ai[0]=objetivo y ai[1]=matiz), así que
    /// las armas de la casa marcan sus orbes ahí y este global añade las
    /// capas de la casa DEBAJO del sprite vanilla (PreDraw devuelve
    /// true: el dibujo de Terraria queda encima, intacto):
    ///
    ///   · 9101 — EL ECO DE LA EMPERATRIZ: luz prismática del color del
    ///     matiz, destellos arcoíris y el anillo giratorio de la casa.
    ///   · 9201 — EL EQUINOCCIO, EL DORADO (el día): LUZ DE SOL intensa,
    ///     estela de llama dorada, halo pulsante y los OCHO RAYOS
    ///     giratorios de un sol en miniatura.
    ///   · 9202 — EL EQUINOCCIO, EL NEGRO (la noche): LA BRUMA DE
    ///     VERDAD de la casa (BrumaFX sobre el rastro), el DISCO DE
    ///     ECLIPSE (la capa alfa que oscurece — negro puro no existe en
    ///     el lote aditivo), el rim violeta pulsante que lo hace legible
    ///     de noche, humo y chispas de llama de sombra. El núcleo blanco
    ///     del 931 queda dentro: un eclipse con estrella atrapada.
    /// </summary>
    public class EmperatrizFX : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        {
            // solo el Nightglow (931 — el streak de la Emperatriz; la casa
            // usa el número, como GrimoireEternal y TestAdvancedFX)
            return projectile.type == 931;
        }

        // ================================================================
        //  LA AI — luces y dusts (gated: nada en servidor)
        // ================================================================
        public override void AI(Projectile projectile)
        {
            float marca = projectile.ai[2];
            if (marca == 0f || Main.netMode == NetmodeID.Server) return;

            switch (marca)
            {
                case 9101f:   // === EL ECO: luz prismática + destellos ===
                {
                    Color c = ColorDeHue(projectile.ai[1]);
                    Lighting.AddLight(projectile.Center,
                        new Vector3(c.R / 255f * 0.55f, c.G / 255f * 0.55f, c.B / 255f * 0.55f));
                    if (Main.rand.NextBool(5))
                    {
                        Dust d = Dust.NewDustPerfect(projectile.Center, DustID.RainbowTorch,
                            new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f)),
                            150, c, Main.rand.NextFloat(0.7f, 1.3f));
                        d.noGravity = true;
                        d.fadeIn = 0.8f;
                    }
                    break;
                }

                case 9201f:   // === EL DORADO: LUZ DE SOL + llama dorada ===
                {
                    // la luz que pediste: el orbe dorado ILUMINA de verdad
                    Lighting.AddLight(projectile.Center, new Vector3(1.05f, 0.86f, 0.42f));
                    if (Main.rand.NextBool(2))
                    {
                        Dust d = Dust.NewDustPerfect(projectile.Center, DustID.GoldFlame,
                            -projectile.velocity * 0.1f + new Vector2(
                                Main.rand.NextFloat(-0.8f, 0.8f), Main.rand.NextFloat(-0.8f, 0.8f)),
                            100, new Color(255, 217, 61), Main.rand.NextFloat(0.8f, 1.4f));
                        d.noGravity = true;
                        d.fadeIn = 0.9f;
                    }
                    if (Main.rand.NextBool(14))
                    {
                        Dust d = Dust.NewDustPerfect(projectile.Center, DustID.Enchanted_Gold,
                            Vector2.Zero, 220, new Color(255, 240, 180), 0.9f);
                        d.noGravity = true;
                        d.fadeIn = 1.4f;
                    }
                    break;
                }

                case 9202f:   // === EL NEGRO: bruma + llama de sombra ===
                {
                    // luz violeta tenue: la noche se ve, no ciega
                    Lighting.AddLight(projectile.Center, new Vector3(0.32f, 0.17f, 0.5f));
                    if (Main.rand.NextBool(2))
                    {
                        Dust d = Dust.NewDustPerfect(
                            projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                            DustID.Smoke,
                            new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-0.9f, -0.2f)),
                            130, new Color(30, 20, 44), Main.rand.NextFloat(1.3f, 2.2f));
                        d.noGravity = true;
                    }
                    if (Main.rand.NextBool(9))
                    {
                        Dust d = Dust.NewDustPerfect(projectile.Center, DustID.Shadowflame,
                            new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-1.2f, 1.2f)),
                            120, new Color(158, 96, 236), Main.rand.NextFloat(0.6f, 1.1f));
                        d.noGravity = true;
                        d.fadeIn = 0.8f;
                    }
                    break;
                }
            }
        }

        // ================================================================
        //  EL PRE-DRAW — las capas de la casa DEBAJO del vanilla
        // ================================================================
        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return true;

            float marca = projectile.ai[2];
            if (marca == 0f) return true;

            float opacidad = projectile.Opacity;
            float t = Main.GameUpdateCount;

            switch (marca)
            {
                case 9101f:   // === EL ECO: el anillo prismático giratorio ===
                {
                    Color c = ColorDeHue(projectile.ai[1]);
                    VFXCore.Begin();
                    VFXCore.Quad(projectile.Center, Color.White * (0.3f * opacidad), new Vector2(42f, 42f), VFXCore.SoftGlow);
                    VFXCore.Quad(projectile.Center, c * (0.28f * opacidad), new Vector2(58f, 58f), VFXCore.SoftGlow);
                    VFXCore.Quad(projectile.Center, c * (0.4f * opacidad), new Vector2(44f, 44f), t * 0.04f, VFXCore.Ring);
                    VFXCore.FlushAdditive();
                    break;
                }

                case 9201f:   // === EL DORADO: el sol en miniatura ===
                {
                    float pulso = 1f + 0.14f * MathF.Sin(t * 0.18f);
                    Color oro = new Color(255, 200, 90);
                    Color oroClaro = new Color(255, 236, 170);

                    VFXCore.Begin();
                    // el halo dorado pulsante
                    VFXCore.Quad(projectile.Center, oro * (0.55f * opacidad), new Vector2(62f * pulso, 62f * pulso), VFXCore.GlowOrb);
                    VFXCore.Quad(projectile.Center, oroClaro * (0.4f * opacidad), new Vector2(34f * pulso, 34f * pulso), VFXCore.GlowOrb);

                    // los OCHO RAYOS giratorios — el sol de la casa
                    float giro = t * 0.012f;
                    for (int i = 0; i < 8; i++)
                    {
                        float angulo = giro + MathHelper.TwoPi * i / 8f;
                        Vector2 dir = new Vector2(1f, 0f).RotatedBy(angulo);
                        float largo = (i % 2 == 0 ? 52f : 34f) * pulso;
                        VFXCore.Line(projectile.Center + dir * 14f,
                            projectile.Center + dir * largo,
                            (i % 2 == 0 ? oro : oroClaro) * (0.45f * opacidad), 5f);
                    }
                    VFXCore.FlushAdditive();
                    break;
                }

                case 9202f:   // === EL NEGRO: el eclipse con bruma ===
                {
                    // 1) LA BRUMA DE VERDAD sobre el rastro (BrumaFX —
                    // el humo fBm que se desgarran de la casa)
                    int paso = Math.Max(1, projectile.oldPos.Length / 10);
                    int puntos = 0;
                    for (int i = 0; i < projectile.oldPos.Length && puntos < 10; i += paso)
                    {
                        if (projectile.oldPos[i] != Vector2.Zero) puntos++;
                    }
                    if (puntos >= 3)
                    {
                        Vector2[] columna = new Vector2[puntos];
                        int n = 0;
                        for (int i = 0; i < projectile.oldPos.Length && n < puntos; i += paso)
                        {
                            if (projectile.oldPos[i] != Vector2.Zero)
                            {
                                columna[n] = projectile.oldPos[i] + projectile.Size * 0.5f;
                                n++;
                            }
                        }
                        SombrasLib.BrumaColumna(columna, 18f, 0.5f * opacidad,
                            t * 0.035f, projectile.whoAmI * 7, 7);
                    }

                    // 2) EL DISCO DE ECLIPSE — la capa alfa que OSCURECE
                    // (negro puro no existe en el lote aditivo: para
                    // tapar hay que salir del aditivo — FlushAlpha)
                    VFXCore.Begin();
                    VFXCore.Quad(projectile.Center, new Color(16, 9, 26) * (0.85f * opacidad),
                        new Vector2(46f, 46f), VFXCore.GlowOrb);
                    VFXCore.FlushAlpha();

                    // 3) EL RIM VIOLETA pulsante — la lectura nocturna
                    // del eclipse (lección de la casa: toda sombra
                    // necesita rim/aliento aditivo)
                    VFXCore.Begin();
                    float pulso = 0.85f + 0.15f * MathF.Sin(t * 0.2f);
                    VFXCore.Quad(projectile.Center, new Color(158, 96, 236) * (0.55f * opacidad),
                        new Vector2(46f * pulso, 46f * pulso), VFXCore.Ring);
                    VFXCore.Quad(projectile.Center, new Color(96, 56, 150) * (0.18f * opacidad),
                        new Vector2(64f, 64f), VFXCore.SoftGlow);
                    VFXCore.FlushAdditive();
                    break;
                }
            }

            return true;   // el sprite vanilla (núcleo blanco + estelas) ENCUENTRA
        }

        /// <summary>El color de render del 931 para un matiz (la fórmula vanilla: hue+0.5).</summary>
        private static Color ColorDeHue(float hue)
            => Main.hslToRgb((hue + 0.5f) % 1f, 1f, 0.5f);
    }
}
