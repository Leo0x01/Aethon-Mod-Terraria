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
    /// AethonMenorPet — v6.50.49 — EL AETHON MENOR (la mascota de luz) ·
    /// v6.50.53 — EL JEFE EXACTO EN MINIATURA: «hazlo que sea exactamente
    /// el jefe pero mas pequeño» — LAS SEIS SECCIONES DEL SOL DE CÓDIGO
    /// del PreDraw del jefe (velo violeta, halo dorado, rueda de rayos,
    /// dos coronas de perlas, chispas orbitantes y núcleo blanco con
    /// corazón dorado) a la ESCALA 0.22.
    ///
    /// EL PATRÓN DE MASCOTA DE LUZ de vanilla (verificado contra el
    /// tML real): Main.projPet (persiste) + ProjectileID.Sets.LightPet
    /// (la familia del fuego fatuo — no la matan las mascotas ajenas) +
    /// NeedsUUID (la instancia ÚNICA por jugador). La IA es la
    /// persecución suave de las mascotas de vanilla: orbita el hombro
    /// del dueño con su vaivén, sin prisa.
    /// </summary>
    public class AethonMenorPet : ModProjectile
    {
        public override string Texture => "AethonMod/Content/NPCs/AethonBoss";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            Main.projPet[Type] = true;                    // mascota: persiste
            ProjectileID.Sets.LightPet[Type] = true;      // MASCOTA DE LUZ (familia del fuego fatuo)
            ProjectileID.Sets.NeedsUUID[Type] = true;     // la instancia única del dueño
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;         // las mascotas son friendly (no daña: damage 0)
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;     // la luz atraviesa el mundo
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;     // tML la sincroniza
        }

        public override void AI()
        {
            // === LA VIDA DE LA MASCOTA (el patrón vanilla): el buff manda ===
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead ||
                !duenio.HasBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.AethonMenorBuff>()))
            {
                Projectile.Kill();
                return;
            }

            float t = Main.GlobalTimeWrappedHourly;

            // === LA PERSECUCIÓN SUAVE (el vaivén de las mascotas de
            //     vanilla): orbita el hombro del dueño — el lado cambia
            //     LENTO con el seno (pasea de un hombro al otro) y sube
            //     y baja con su propio compás ===
            float lado = MathF.Sin(t * 0.35f) * 46f - duenio.direction * 14f;
            float bob = MathF.Sin(t * 1.9f) * 9f;
            Vector2 meta = duenio.Center + new Vector2(lado, -46f + bob);

            // La búsqueda serena: nunca teletransporta, siempre alcanza.
            Vector2 hacia = meta - Projectile.Center;
            float dist = hacia.Length();
            if (dist > 900f)            // el dueño se fue muy lejos: le alcanza volando
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 0.05f, 0.20f);
            else
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 0.045f, 0.10f);

            // El freno al llegar (flota, no vibra encima del punto).
            if (dist < 24f)
                Projectile.velocity *= 0.80f;

            Projectile.rotation += 0.02f;   // el núcleo gira despacito (el sol en miniatura)

            // === LA LUZ DE MUNDO (v6.50.53 — exactamente el JEFE: su luz
            //     dorada-blanca (1.55, 1.35, 0.95) a escala de mascota —
            //     el prisma rotativo murió: el jefe NO es arcoíris, es
            //     ORO Y NÚCLEO BLANCO, y la mascota es SU retrato) ===
            Lighting.AddLight(Projectile.Center, 0.85f, 0.72f, 0.48f);

            // === LAS CHISPAS (el rastro de la criatura de luz) ===
            if (!Main.dedServ && Main.rand.NextBool(14))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + new Vector2(Main.rand.NextFloat(-8f, 8f),
                        Main.rand.NextFloat(-8f, 8f)),
                    DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f),
                        Main.rand.NextFloat(-0.8f, -0.2f)),
                    170, new Color(255, 244, 200), 0.8f);
                d.noGravity = true;
            }
        }

        // ==================================================================
        //  EL DIBUJADO — v6.50.53 — EL JEFE EXACTO, EN MINIATURA (la
        //  letra: «las mascota del jefe no se parece en nada al jefe,
        //  hazlo que sea exactamente el jefe pero mas pequeño»). EL
        //  DIAGNÓSTICO: el jefe EN JUEGO NO es su sprite png — es EL SOL
        //  DE CÓDIGO del PreDraw (el velo violeta, el halo dorado, LA
        //  RUEDA DE RAYOS RADIALES, LAS DOS CORONAS DE PERLAS, las
        //  chispas orbitantes y el NÚCLEO BLANCO con su corazón dorado);
        //  la mascota dibujaba el PNG crudo a 34 px = un disco chico que
        //  NO se parecía en NADA. LA CURA: LAS SEIS SECCIONES DEL JEFE
        //  replicadas UNA A UNA a la ESCALA 0.22 (el velo del jefe mide
        //  540 px; el de la mascota, 119) — el MISMO dibujo, la MISMA
        //  proporción, el MISMO compás (el latido 1.6 Hz, el giro 0.10
        //  de la rueda, los sentidos opuestos de las coronas: +0.55 y
        //  −0.38). El contrato del bool de la casa: PreDraw cierra el
        //  lote del pase, vuela el SUYO aditivo y lo DEVUELVE ABIERTO
        //  (v6.50.50 — el crash de la .49 murió así).
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 c = Projectile.Center;

                // === LA ESCALA: el jefe mide ~540 px de velo — la
                //     mascota es SU RETRATO a 0.22 ===
                const float S = 0.22f;

                // EL MISMO LATIDO y el MISMO BRILLO del jefe (su PreDraw).
                float latido = 0.84f + 0.16f * MathF.Sin(t * 1.6f);
                float brillo = 1f;

                // === 1. EL VELO VIOLETA (la profundidad del sol — la
                //     sección 1 del jefe: Bloom 540, VioletaLuz, 0.10) ===
                VFXCore.Quad(c, new Color(196, 150, 255, 255) * (0.055f * brillo),
                    new Vector2(540f * 1.5f * S, 540f * 1.5f * S), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(196, 150, 255, 255) * (0.10f * brillo),
                    new Vector2(540f * 0.8f * S, 540f * 0.8f * S), 0f, VFXCore.SoftGlow);

                // === 2. EL HALO DORADO (la corona del sol — la sección 2
                //     del jefe: BloomPulse 295·latido, OroLuz, 0.50) ===
                float pulsoH = 0.82f + 0.18f * MathF.Sin(t * 1.6f);
                float halo = 295f * latido * S;
                VFXCore.Quad(c, new Color(255, 240, 190, 255) * (0.32f * brillo * pulsoH),
                    new Vector2(halo * 2.85f, halo * 2.85f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(255, 248, 225, 255) * (0.42f * brillo * pulsoH),
                    new Vector2(halo * 1.5f, halo * 1.5f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(255, 252, 240, 255) * (0.50f * brillo * pulsoH),
                    new Vector2(halo * 0.8f, halo * 0.8f), 0f, VFXCore.SoftGlow);

                // === 3. LA RUEDA DE RAYOS RADIALES (la sección 3 del
                //     jefe: 10 rayos de la fase 1 girando a 0.10 — el sol
                //     de Aethon es una RUEDA DE LUZ; el quad mide 2×largo
                //     × 2×ancho como el Draw del jefe: el rayo sale del
                //     centro hacia AMBOS lados) ===
                {
                    int nRayos = 10;
                    float giro = t * 0.10f;
                    for (int i = 0; i < nRayos; i++)
                    {
                        float ang = giro + i * MathHelper.TwoPi / nRayos;
                        float largo = (255f + 165f * 0.55f) * S *          // faseInt 0.55 (la fase 2)
                            (0.78f + 0.22f * MathF.Sin(t * 2.1f + i * 1.9f));
                        float ancho = (34f - 12f * 0.55f) * S;
                        VFXCore.Quad(c, new Color(255, 240, 190, 255) * (0.30f * brillo),
                            new Vector2(largo * 2f, ancho * 2f), ang, VFXCore.SoftGlow);
                    }
                }

                // === 4. LAS DOS CORONAS DE PERLAS (la sección 4 del jefe:
                //     13 perlas por anillo, rx 218/ry 134 y rx 300/ry 90,
                //     sentidos OPUESTOS +0.55/−0.38 — el efecto 3D de
                //     Saturno) ===
                for (int anillo = 0; anillo < 2; anillo++)
                {
                    float rx = (anillo == 0 ? 218f : 300f) * S;
                    float ry = (anillo == 0 ? 134f : 90f) * S;
                    float w = anillo == 0 ? 0.55f : -0.38f;          // sentidos opuestos
                    Color cP = anillo == 0 ? new Color(255, 240, 190) : new Color(196, 150, 255);
                    for (int i = 0; i < 13; i++)
                    {
                        float ang = t * w + i * MathHelper.TwoPi / 13f;
                        Vector2 perla = c + new Vector2(MathF.Cos(ang) * rx,
                            MathF.Sin(ang) * ry);
                        float tw = 0.5f + 0.5f * MathF.Sin(t * 3f + i * 2.1f + anillo);
                        // LA PERLA (Bloom 12 → 2.6 px + su halo).
                        VFXCore.Quad(perla, new Color(cP.R, cP.G, cP.B, 255) *
                            (0.42f * (0.5f + 0.5f * tw) * brillo),
                            new Vector2(12f * 1.5f * S, 12f * 1.5f * S), 0f, VFXCore.SoftGlow);
                        VFXCore.Quad(perla, new Color(255, 250, 235, 255) *
                            (0.42f * (0.5f + 0.5f * tw) * brillo),
                            new Vector2(12f * 0.8f * S, 12f * 0.8f * S), 0f, VFXCore.SoftGlow);
                    }
                }

                // === 5. LAS CHISPAS ORBITANTES (la sección 5 del jefe:
                //     6 motas en r 176·0.72 elíptico) ===
                for (int m = 0; m < 6; m++)
                {
                    float ang = t * (1.1f + m * 0.17f) + m * 2.1f;
                    float r = 176f * S + 3.5f * MathF.Sin(t * 2.4f + m);
                    Vector2 mota = c + new Vector2(MathF.Cos(ang) * r,
                        MathF.Sin(ang) * r * 0.72f);
                    VFXCore.Quad(mota, new Color(255, 240, 190, 255) * (0.40f * brillo),
                        new Vector2(20f * 1.5f * S, 20f * 1.5f * S), 0f, VFXCore.SoftGlow);
                    VFXCore.Quad(mota, new Color(255, 252, 240, 255) * (0.40f * brillo),
                        new Vector2(20f * 0.8f * S, 20f * 0.8f * S), 0f, VFXCore.SoftGlow);
                }

                // === 6. EL NÚCLEO (la sección 6 del jefe: el corazón
                //     BLANCO de 130·latido al 1.0 + su alma DORADA de
                //     0.42× al 0.65 — el centro del dios) ===
                float tamNucleo = 130f * latido * S;
                VFXCore.Quad(c, new Color(255, 252, 240, 255) * (1.0f * brillo),
                    new Vector2(tamNucleo * 2.85f, tamNucleo * 2.85f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(255, 252, 240, 255) * (1.0f * brillo),
                    new Vector2(tamNucleo * 1.5f, tamNucleo * 1.5f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(255, 252, 240, 255) * (1.0f * brillo),
                    new Vector2(tamNucleo * 0.8f, tamNucleo * 0.8f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(255, 240, 190, 255) * (0.65f * brillo),
                    new Vector2(tamNucleo * 0.42f * 1.5f, tamNucleo * 0.42f * 1.5f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(255, 240, 190, 255) * (0.65f * brillo),
                    new Vector2(tamNucleo * 0.42f * 0.8f, tamNucleo * 0.42f * 0.8f), 0f, VFXCore.SoftGlow);

                if (VFXCore.QuadCount > 0)
                {
                    // v6.50.50 — EL CRASH DE LA MASCOTA, MUERTO DE RAÍZ:
                    // el contrato viejo (FlushAdditive + return true) dejaba
                    // el lote CERRADO y tML dibujaba encima → «Draw was
                    // called, but Begin has not yet been called» y el End
                    // final del bucle mataba el motor. EL CONTRATO DE LA
                    // CASA: salir SIEMPRE con el lote ABIERTO y vanilla —
                    // y el vuelco YA fue el dibujo (return false: tML no
                    // añade sprite encima).
                    VFXCore.FlushAdditive(null, true); // vuela el lote aditivo (queda cerrado)
                    VFXCore.ReabrirLoteVanilla();      // y SE REABRE — lote vivo para el siguiente
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
