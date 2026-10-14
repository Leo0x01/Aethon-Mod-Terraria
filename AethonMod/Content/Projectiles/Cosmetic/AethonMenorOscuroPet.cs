using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmetic
{
    /// <summary>
    /// AethonMenorOscuroPet — v6.50.117 — EL GEMELO OSCURO DE LA MASCOTA
    /// (la letra del usuario: «esta mascota ya existe en su versión solo
    /// de luz, solo debes agregar a su gemelo»). ES EL CLARO en
    /// comportamiento — el mismo vuelo sereno de las mascotas de vanilla
    /// (persigue su meta con el mismo muelle del hermano) — PERO:
    ///
    /// · NO ES MASCOTA DE LUZ: sin ProjectileID.Sets.LightPet y SIN
    ///   Lighting.AddLight («la versión de oscuridad no ilumina») — el
    ///   gemelo es un agujero en el aire, NO una lámpara.
    /// · PALETA NEGRO Y MORADO OSCURO: núcleo (20, 8, 30), halos
    ///   (110, 40, 170) — el retrato INVERTIDO del sol de código del
    ///   hermano: su velo, su halo, su rueda de rayos, sus dos coronas
    ///   de perlas y sus chispas, todo en la oscuridad.
    /// · EL CUERPO NEGRO VIVE EN EL LOTE ALFA: el negro NO existe en el
    ///   blending aditivo (negro = suma 0 = invisible — la lección de la
    ///   casa de la v6.50.66), así que el disco del núcleo se dibuja con
    ///   FlushAlpha (apilado, para que el centro sea NEGRO de verdad) y
    ///   su RIM morado fino pasa al lote aditivo — sin rim, un disco
    ///   negro es INVISIBLE de noche.
    ///
    /// LA PAREJA (ideas 15/21): el CLARO es el LÍDER (el dueño lo sigue
    /// a él; el gemelo sigue AL CLARO). El ciclo real del mundo los une
    /// y separa — véase ParejaCerebro.Equinoccio(): en los equinoccios
    /// (amanecer/crepúsculo) el gemelo se coloca EXACTO sobre el claro
    /// (un offset orbital diminuto de ~6 px girando rápido entre ambos:
    /// UNO SOLO bicolor); el resto del día orbitan separados (~34 px).
    ///
    /// SUS ANILLOS (idea 15): 2 mini AGUJEROS NEGROS orbitan al gemelo
    /// (discos negros con borde morado fino, radios orbitales 30/38 px,
    /// GIRO CONTRARIO entre sí) — los dibuja ÉL (cada hermano dibuja lo
    /// suyo; el sol rúnico y los 3 mini ojos son del claro).
    /// </summary>
    public class AethonMenorOscuroPet : ModProjectile
    {
        // === LA PALETA DEL GEMELO (el retrato invertido del claro) ===
        private static readonly Color NucleoNegro = new(20, 8, 30);      // el corazón: NEGRO
        private static readonly Color HaloMorado = new(110, 40, 170);    // sus halos: MORADO OSCURO
        private static readonly Color VeloMorado = new(38, 16, 60);      // la profundidad
        private static readonly Color PerlaVioleta = new(150, 90, 225);  // las perlas de afuera

        public override string Texture => "AethonMod/Content/NPCs/AethonBoss";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            Main.projPet[Type] = true;                    // mascota: persiste
            // SIN ProjectileID.Sets.LightPet — la versión de oscuridad NO
            // ilumina (no es familia del fuego fatuo: es la sombra que le
            // sale al sol).
            ProjectileID.Sets.NeedsUUID[Type] = true;     // la instancia única del dueño
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;         // como el claro (no daña: damage 0)
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;     // la oscuridad atraviesa el mundo igual
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;     // tML la sincroniza
        }

        public override void AI()
        {
            // === LA VIDA DEL GEMELO (el mismo contrato del claro): el
            //     buff manda — AethonMenorBuff sostiene a LOS DOS. ===
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead ||
                !duenio.HasBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.AethonMenorBuff>()))
            {
                Projectile.Kill();
                return;
            }

            float t = Main.GlobalTimeWrappedHourly;

            // === EL HERMANO CLARO: el LÍDER de la pareja (NeedsUUID:
            //     hay UNO por dueño — el primero que aparezca es él). ===
            Projectile claro = ParejaCerebro.BuscarClaro(Projectile.owner);

            // === EL EQUINOCCIO (la letra de la idea 21): el día dura
            //     54000 ticks; en el amanecer (time < 1500) y el
            //     crepúsculo (time > 52500) el día y la noche pesan
            //     CASI igual — los «equinoccios» del mundo. Ahí los
            //     gemelos se FUSIONAN; el resto del tiempo, separados. ===
            bool equinoccio = ParejaCerebro.Equinoccio();

            Vector2 meta;
            if (claro == null)
            {
                // SIN CLARO (transición de nacimiento): el mismo vuelo de
                // hombro del hermano, pero en el LADO ESPEJO.
                float lado = MathF.Sin(t * 0.35f) * 46f - duenio.direction * 14f;
                float bob = MathF.Sin(t * 1.9f + 1.3f) * 9f;
                meta = duenio.Center + new Vector2(-lado, -46f + bob);
            }
            else if (equinoccio)
            {
                // FUSIONADOS: EXACTO sobre el claro — un offset orbital
                // DIMINUTO (~6 px) girando RÁPIDO entre ambos: leídos a
                // lo lejos son UNO SOLO bicolor (la rueda de perlas del
                // claro ENGORDA el punto de fusión: véase su PreDraw).
                float a = t * 2.6f;
                meta = claro.Center + new Vector2(MathF.Cos(a) * 6f, MathF.Sin(a) * 6f);
            }
            else
            {
                // SEPARADOS: el gemelo orbita al claro a ~34 px — se
                // orbitan mutuamente (el claro no se mueve por él: es el
                // líder, el dueño manda sobre él y él manda sobre el
                // gemelo — la cadena de la pareja).
                float a = t * 0.85f;
                meta = claro.Center + new Vector2(MathF.Cos(a) * 34f, MathF.Sin(a) * 34f * 0.75f);
            }

            // === LA PERSECUCIÓN SUAVE (el MISMO muelle del hermano — con
            //     un MODO ESCAPE: si el líder se fue volando al pueblo
            //     (el cerebro del claro lo pone a 12 px/t), el gemelo
            //     aprieta el tirón para NO PERDER LA FORMACIÓN — «el
            //     claro lidera, el oscuro sigue»). ===
            Vector2 hacia = meta - Projectile.Center;
            float dist = hacia.Length();
            if (dist > 900f)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 0.05f, 0.25f);
            else if (dist > 140f)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 0.06f, 0.18f);
            else
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 0.045f, 0.10f);

            // El freno al llegar (flota, no vibra encima del punto).
            if (dist < 24f)
                Projectile.velocity *= 0.80f;

            Projectile.rotation -= 0.016f;  // gira AL REVÉS que su hermano (los gemelos discuten)

            // === SIN LUZ DE MUNDO — «la versión de oscuridad no
            //     ilumina»: ni flag de LightPet ni Lighting.AddLight.
            //     Donde el claro es un sol pequeño, el gemelo es su
            //     mancha: come la luz, no la da. ===

            // === EL POLVILLO VIOLETA (la sombra también gotea — el
            //     espejo de las chispas doradas del claro) ===
            if (!Main.dedServ && Main.rand.NextBool(18))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + new Vector2(Main.rand.NextFloat(-8f, 8f),
                        Main.rand.NextFloat(-8f, 8f)),
                    DustID.Shadowflame,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f),
                        Main.rand.NextFloat(-0.8f, -0.2f)),
                    128, new Color(110, 40, 170), 0.65f);
                d.noGravity = true;
            }
        }

        // ==================================================================
        //  EL DIBUJADO — EL CLARO INVERTIDO: LAS MISMAS SEIS SECCIONES
        //  del sol de código del hermano, con la paleta NEGRO/MORADO
        //  OSCURO — más LA SECCIÓN 0 (el cuerpo negro, en el lote alfa)
        //  y SUS 2 MINI AGUJEROS NEGROS (idea 15). El contrato del bool
        //  de la casa es el del hermano: salir SIEMPRE con el lote
        //  vanilla ABIERTO (v6.50.50 — el crash de la .49 murió así).
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 c = Projectile.Center;

                // === LA ESCALA: el mismo retrato del jefe a 0.22 ===
                const float S = 0.22f;

                // EL MISMO LATIDO del hermano (su compás compartido).
                float latido = 0.84f + 0.16f * MathF.Sin(t * 1.6f);

                // === 0. EL CUERPO NEGRO + LOS AGUJEROS (LOTE ALFA — lo
                //     que el aditivo NO puede dibujar: OSCURECER). Tres
                //     capas apiladas de (20,8,30) hacen el centro NEGRO
                //     de verdad; en el lote aditivo de abajo le ponemos
                //     el RIM morado para que se lea sobre cualquier
                //     fondo (lección v6.50.66: negro puro sin rim =
                //     invisible de noche). ===
                float nucleo = 130f * latido * S;
                VFXCore.Begin();
                VFXCore.Quad(c, NucleoNegro * 0.40f, new Vector2(nucleo * 2.85f, nucleo * 2.85f));
                VFXCore.Quad(c, NucleoNegro * 0.65f, new Vector2(nucleo * 1.5f, nucleo * 1.5f));
                VFXCore.Quad(c, new Color(14, 5, 22, 255) * 0.92f, new Vector2(nucleo * 0.8f, nucleo * 0.8f));

                // LOS 2 MINI AGUJEROS NEGROS (idea 15): discos negros
                // apilados — radios orbitales 30/38 px, GIRO CONTRARIO
                // entre sí. (Sus rims y brillos van al lote aditivo.)
                Vector2[] agujeros = new Vector2[2];
                float[] tamAgujero = new float[2];
                for (int h = 0; h < 2; h++)
                {
                    float radio = h == 0 ? 30f : 38f;
                    float w = h == 0 ? 0.85f : -1.25f;              // el giro contrario
                    float ang = t * w + (h == 0 ? 0f : MathHelper.Pi);
                    agujeros[h] = c + new Vector2(MathF.Cos(ang) * radio, MathF.Sin(ang) * radio * 0.8f);
                    tamAgujero[h] = (h == 0 ? 6.5f : 8f) * (0.9f + 0.1f * MathF.Sin(t * 3f + h * 2.1f));
                    VFXCore.Quad(agujeros[h], new Color(6, 2, 10, 255) * 0.55f,
                        new Vector2(tamAgujero[h] * 2.6f, tamAgujero[h] * 2.6f));
                    VFXCore.Quad(agujeros[h], new Color(10, 4, 16, 255) * 0.92f,
                        new Vector2(tamAgujero[h] * 1.6f, tamAgujero[h] * 1.6f));
                }
                VFXCore.FlushAlpha();

                // === 1. EL VELO MORADO OSCURO (la profundidad del gemelo —
                //     las mismas medidas del hermano, SU color) ===
                VFXCore.Quad(c, VeloMorado * 0.055f,
                    new Vector2(540f * 1.5f * S, 540f * 1.5f * S), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, VeloMorado * 0.10f,
                    new Vector2(540f * 0.8f * S, 540f * 0.8f * S), 0f, VFXCore.SoftGlow);

                // === 2. EL HALO MORADO (la corona de la sombra) ===
                float pulsoH = 0.82f + 0.18f * MathF.Sin(t * 1.6f);
                float halo = 295f * latido * S;
                VFXCore.Quad(c, HaloMorado * (0.28f * pulsoH),
                    new Vector2(halo * 2.85f, halo * 2.85f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, HaloMorado * (0.36f * pulsoH),
                    new Vector2(halo * 1.5f, halo * 1.5f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, PerlaVioleta * (0.42f * pulsoH),
                    new Vector2(halo * 0.8f, halo * 0.8f), 0f, VFXCore.SoftGlow);

                // === 3. LA RUEDA DE RAYOS (morados — 10 rayos girando a
                //     0.10, el compás exacto del hermano) ===
                {
                    int nRayos = 10;
                    float giro = t * 0.10f;
                    for (int i = 0; i < nRayos; i++)
                    {
                        float ang = giro + i * MathHelper.TwoPi / nRayos;
                        float largo = (255f + 165f * 0.55f) * S *
                            (0.78f + 0.22f * MathF.Sin(t * 2.1f + i * 1.9f));
                        float ancho = (34f - 12f * 0.55f) * S;
                        VFXCore.Quad(c, HaloMorado * 0.26f,
                            new Vector2(largo * 2f, ancho * 2f), ang, VFXCore.SoftGlow);
                    }
                }

                // === 4. LAS DOS CORONAS DE PERLAS (13 por anillo, los
                //     mismos sentidos opuestos +0.55/−0.38 del hermano —
                //     el efecto 3D de Saturno, en morado) ===
                for (int anillo = 0; anillo < 2; anillo++)
                {
                    float rx = (anillo == 0 ? 218f : 300f) * S;
                    float ry = (anillo == 0 ? 134f : 90f) * S;
                    float w = anillo == 0 ? 0.55f : -0.38f;          // sentidos opuestos
                    Color cP = anillo == 0 ? HaloMorado : PerlaVioleta;
                    for (int i = 0; i < 13; i++)
                    {
                        float ang = t * w + i * MathHelper.TwoPi / 13f;
                        Vector2 perla = c + new Vector2(MathF.Cos(ang) * rx,
                            MathF.Sin(ang) * ry);
                        float tw = 0.5f + 0.5f * MathF.Sin(t * 3f + i * 2.1f + anillo);
                        VFXCore.Quad(perla, new Color(cP.R, cP.G, cP.B, 255) *
                            (0.40f * (0.5f + 0.5f * tw)),
                            new Vector2(12f * 1.5f * S, 12f * 1.5f * S), 0f, VFXCore.SoftGlow);
                        VFXCore.Quad(perla, new Color(230, 200, 255, 255) *
                            (0.30f * (0.5f + 0.5f * tw)),
                            new Vector2(12f * 0.8f * S, 12f * 0.8f * S), 0f, VFXCore.SoftGlow);
                    }
                }

                // === 5. LAS CHISPAS ORBITANTES (violetas — 6 motas en el
                //     mismo elipse del hermano) ===
                for (int m = 0; m < 6; m++)
                {
                    float ang = t * (1.1f + m * 0.17f) + m * 2.1f;
                    float r = 176f * S + 3.5f * MathF.Sin(t * 2.4f + m);
                    Vector2 mota = c + new Vector2(MathF.Cos(ang) * r,
                        MathF.Sin(ang) * r * 0.72f);
                    VFXCore.Quad(mota, HaloMorado * 0.36f,
                        new Vector2(20f * 1.5f * S, 20f * 1.5f * S), 0f, VFXCore.SoftGlow);
                    VFXCore.Quad(mota, new Color(210, 170, 255, 255) * 0.32f,
                        new Vector2(20f * 0.8f * S, 20f * 0.8f * S), 0f, VFXCore.SoftGlow);
                }

                // === 6. EL RIM DEL NÚCLEO + EL CORAZÓN MORADO — el espejo
                //     del corazón DORADO del claro: un Ring morado fino al
                //     borde del disco negro (para que el agujero EXISTA en
                //     la noche) y su alma morada al 0.42× ===
                VFXCore.Quad(c, HaloMorado * 0.5f, VFXCore.RingQuadSize(nucleo * 0.5f), 0f, VFXCore.Ring);
                VFXCore.Quad(c, HaloMorado * 0.65f,
                    new Vector2(nucleo * 0.42f * 1.5f, nucleo * 0.42f * 1.5f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, HaloMorado * 0.65f,
                    new Vector2(nucleo * 0.42f * 0.8f, nucleo * 0.42f * 0.8f), 0f, VFXCore.SoftGlow);

                // LOS RIMS DE LOS 2 AGUJEROS NEGROS: el borde morado fino
                // + un glint violeta (la acreción chiquita).
                for (int h = 0; h < 2; h++)
                {
                    VFXCore.Quad(agujeros[h], HaloMorado * 0.55f,
                        VFXCore.RingQuadSize(tamAgujero[h] * 0.85f), 0f, VFXCore.Ring);
                    VFXCore.Quad(agujeros[h], HaloMorado * 0.22f,
                        new Vector2(tamAgujero[h] * 3.4f, tamAgujero[h] * 3.4f), 0f, VFXCore.SoftGlow);
                    VFXCore.Quad(agujeros[h] + new Vector2(1.2f, -1.2f),
                        new Color(196, 150, 255, 255) * 0.5f, new Vector2(1.8f, 1.8f));
                }

                if (VFXCore.QuadCount > 0)
                {
                    // El contrato de la casa (v6.50.50): volar el lote
                    // aditivo y REABRIR el vanilla — jamás salir con el
                    // lote cerrado.
                    VFXCore.FlushAdditive(null, true);
                    VFXCore.ReabrirLoteVanilla();
                    return false;
                }
                return false;
            }
            catch
            {
                try { VFXCore.ReabrirLoteVanilla(); } catch { }
                return false;
            }
        }
    }
}
