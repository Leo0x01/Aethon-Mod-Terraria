using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// EMPERATRIZFX — los FX globales de los libros que disparan el 931
    /// VANILLA con la marca de la casa (opción B: la IA de Terraria no se
    /// toca): EL EQUINOCCIO (la elegida de las cuatro pruebas de la .99)
    /// y — desde la v6.50.102 — EL GRIMORIO DEL ETERNO, que heredó su
    /// disparo.
    ///
    /// El 931 no usa ai[2] (solo ai[0]=objetivo y ai[1]=matiz), así que
    /// las armas de la casa marcan sus orbes ahí y este global añade las
    /// capas de la casa DEBAJO del sprite vanilla (PreDraw devuelve
    /// true: el dibujo de Terraria queda encima, intacto):
    ///
    ///   · 9201 — EL DORADO (el día): LUZ DE SOL intensa, estela de llama
    ///     dorada, halo pulsante y los OCHO RAYOS giratorios de un sol
    ///     en miniatura.
    ///   · 9202 — EL NEGRO (la noche): el DISCO DE ECLIPSE (la capa alfa
    ///     que oscurece — negro puro no existe en el lote aditivo) y el
    ///     rim violeta pulsante que lo hace legible de noche, con chispas
    ///     de llama de sombra. El núcleo blanco del 931 queda dentro: un
    ///     eclipse con estrella atrapada.
    ///
    /// v6.50.102 — LA BRUMA MURIÓ (la letra del usuario: «a ese proyectil
    /// que ya no use bruma»): el negro ya no exhala el humo fBm de la
    /// casa sobre su rastro — queda el eclipse LIMPIO: disco, rim y
    /// llama de sombra.
    /// </summary>
    public class EmperatrizFX : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        {
            // solo el Nightglow (931 — el streak de la Emperatriz; la casa
            // usa el número, como el Grimorio del Eterno y TestAdvancedFX)
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

                case 9202f:   // === EL NEGRO: el eclipse limpio ===
                {
                    // luz violeta tenue: la noche se ve, no ciega
                    Lighting.AddLight(projectile.Center, new Vector3(0.32f, 0.17f, 0.5f));
                    // v6.50.102 — SIN BRUMA y SIN HUMO (la letra: «que ya no
                    // use bruma»): del negro quedan el disco de eclipse, el
                    // rim violeta y las chispas de llama de sombra
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
        //
        //  v6.50.101 — EL CONTRATO DEL LOTE (lección del client.log del
        //  usuario 05:38:06): los Flush de VFXCore cierran el lote del
        //  pase de proyectiles y dejan NINGUNO abierto → al devolver
        //  true, el EntitySpriteDraw del 931 lanza «Draw was called,
        //  but Begin has not yet been called» y el End final del bucle
        //  TUMBA EL MOTOR. El patrón de la casa (TestAdvancedFX): cerrar
        //  → dibujar → REABRIR SIEMPRE el lote vanilla antes de que
        //  vanilla dibuje su sprite.
        // ================================================================
        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return true;

            float marca = projectile.ai[2];
            if (marca == 0f) return true;

            float opacidad = projectile.Opacity;
            float t = Main.GameUpdateCount;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                switch (marca)
                {
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

                case 9202f:   // === EL NEGRO: el eclipse LIMPIO (sin bruma) ===
                {
                    // 1) EL DISCO DE ECLIPSE — la capa alfa que OSCURECE
                    //    (negro puro no existe en el lote aditivo: para
                    //    tapar hay que salir del aditivo — FlushAlpha)
                    VFXCore.Begin();
                    VFXCore.Quad(projectile.Center, new Color(16, 9, 26) * (0.85f * opacidad),
                        new Vector2(46f, 46f), VFXCore.GlowOrb);
                    VFXCore.FlushAlpha();

                    // 2) EL RIM VIOLETA pulsante — la lectura nocturna del
                    //    eclipse (lección de la casa: toda sombra necesita
                    //    rim/aliento aditivo)
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
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();   // ★ el contrato: el lote VIVO para el sprite vanilla

            return true;   // el sprite vanilla (núcleo blanco + estelas) ENCUENTRA
        }
    }
}
