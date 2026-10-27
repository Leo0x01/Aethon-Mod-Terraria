using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Definitivas
{
    /// <summary>
    /// REQUIEMESTELARPROJECTILE — v6.54.6 — ARMA 2 DE LAS CINCO: EL
    /// RÉQUIEM ESTELAR.
    ///
    /// LA REFORMA (la letra del usuario: «su círculo está roto, hay una
    /// zona donde hay un agujero y no está conectado; además es
    /// visualmente simple, pero creo que en combinación con algo más se
    /// vería genial»):
    ///
    ///   · EL AGUJERO CURADO: la tira del anillo NO cerraba el loop —
    ///     los 42 puntos iban del 0 al 41 y el segmento 41→0 no existía
    ///     (y el suavizado Chaikin recortaba los extremos dejando la
    ///     herida a la vista). Ahora el anillo se teje con SOBLANTES: el
    ///     punto 0 y el 1 se REPITEN al final, así la costura queda
    ///     enterrada bajo segmentos vivos — un anillo DE VERDAD cerrado.
    ///
    ///   SUS SIETE CAPAS (la «combinación con algo más» que pediste):
    ///   CAPA 1 — LOS ANILLOS: la circunferencia con TRES fundas (halo,
    ///   cuerpo, vena), ondulando como bronce vivo.
    ///   CAPA 2 — EL ECO: el tercer anillo (LA CAMPANA MAYOR) lleva su
    ///   gemelo interior al 82%, desfasado — el eco del bronce.
    ///   CAPA 3 — LAS NOTAS: OCHO estrellas de cuatro puntas cabalgan la
    ///   circunferencia girando, con su rastro de polvo dorado.
    ///   CAPA 4 — EL BADAJO: un anillo interior fino (al 55%) girando
    ///   EN CONTRA — la campana tiene maquinaria dentro.
    ///   CAPA 5 — EL LATIDO CENTRAL: los CUATRO HALOS APILADOS del rayo
    ///   de fusión pulsando SINCRONIZADOS con cada tañido.
    ///   CAPA 6 — LA PALABRA DE CADA TAÑIDO: flash de pantalla suave +
    ///   ONDA DE CHOQUE radial + el temblor — la campana se SIENTE.
    ///   CAPA 7 — LA LLUVIA DE ESTRELLAS DEL CIERRE: al tercer tañido,
    ///   estrellas de verdad (la textura de cuatro puntas) caen por
    ///   todo el radio con su estela de polvo.
    /// </summary>
    public class RequiemEstelarProjectile : ModProjectile
    {
        private const int NAnillos = 3;
        private const int Cadencia = 32;
        private const float RadioFinal = 1400f;
        private const int VidaAnillo = 46;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Cadencia * NAnillos + 70;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 26;
            Projectile.netImportant = true;
        }

        /// <summary>El radio de un anillo k en su vida v (0..1).</summary>
        private static float RadioAnillo(float v) => RadioFinal * OndaLib.Expansion(v);

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;

            // v6.54.6 — el failsafe global de vida máxima.
            if (t > 1200f) { Projectile.Kill(); return; }

            // EL ORIGEN: donde estaba el portador al tañer (la campana no se mueve con él)
            if (t < 2f) Projectile.Center = dueño.MountedCenter;

            // EL TAÑIDO de cada anillo — CAPA 6: la palabra de la campana
            for (int k = 0; k < NAnillos; k++)
            {
                float momento = 6f + k * Cadencia;
                if (MathF.Abs(t - momento) < 0.5f)
                {
                    bool doble = k == NAnillos - 1;
                    DefinitivasLib.Sonar(SoundID.Item27.WithPitchOffset(-0.3f + k * 0.15f).WithVolumeScale(doble ? 1.1f : 0.8f),
                        Projectile.Center);
                    if (doble)
                    {
                        DefinitivasLib.Sonar(SoundID.Item122.WithPitchOffset(-0.2f).WithVolumeScale(0.9f), Projectile.Center);
                        OndaLib.Kick(11, 16);
                        OndaLib.Flash(new Color(255, 240, 200), 0.30f, 12, Projectile.Center);
                    }
                    else
                    {
                        OndaLib.Kick(5, 8);
                        OndaLib.Flash(DefinitivasLib.Oro, 0.10f, 7, Projectile.Center);
                    }
                }
            }

            // ==================== CAPA 7 — LA LLUVIA DE ESTRELLAS DEL CIERRE ====================
            // (el tercer anillo siembra ESTRELLAS de verdad con estela)
            float tCierre = 6f + (NAnillos - 1) * Cadencia;
            if (t > tCierre && t < tCierre + 30 && !Main.dedServ && Main.netMode != NetmodeID.Server)
            {
                int cuantas = Main.rand.NextBool() ? 2 : 1;
                for (int s = 0; s < cuantas; s++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    float radio = Main.rand.NextFloat(200f, RadioFinal * 0.75f);
                    Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio - new Vector2(0f, 340f);
                    float caida = Main.rand.NextFloat(4f, 9f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, new Vector2(0f, caida), 0,
                        DefinitivasLib.GradienteJuicio(Main.rand.NextFloat(1f)), 1.0f);
                    d.noGravity = true;
                    // LA ESTRELLA que cae (la textura de 4 puntas — polvo + trazo)
                    if (DefinitivasLib.TexStar != null && Main.rand.NextBool(2))
                    {
                        Dust e = Dust.NewDustPerfect(pos + new Vector2(0f, -10f), DustID.Enchanted_Pink,
                            new Vector2(0f, caida * 0.8f), 0, DefinitivasLib.BlancoCalido, 0.6f);
                        e.noGravity = true;
                    }
                }
            }

            // ==================== CAPA 3b — EL RASTRO DE LAS NOTAS ====================
            // (polvo dorado orbitando el frente de cada anillo vivo)
            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
            {
                for (int k = 0; k < NAnillos; k++)
                {
                    float nace = 6f + k * Cadencia;
                    float tk = t - nace;
                    if (tk < 0f || tk > VidaAnillo) continue;
                    float v = MathHelper.Clamp(tk / VidaAnillo, 0f, 1f);
                    float radio = RadioAnillo(v);
                    if (Main.rand.NextBool(3))
                    {
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                        Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                            new Vector2(MathF.Cos(ang + MathHelper.PiOver2), MathF.Sin(ang + MathHelper.PiOver2)) *
                            Main.rand.NextFloat(1f, 3f), 0,
                            DefinitivasLib.GradienteJuicio(ang / MathHelper.TwoPi), 0.8f);
                        d.noGravity = true;
                    }
                }
                Lighting.AddLight(Projectile.Center, 1.6f, 1.4f, 1.0f);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // EL FRENTE DE LA ONDA: solo la CIRCUNFERENCIA actual de cada anillo vivo golpea
            for (int k = 0; k < NAnillos; k++)
            {
                float nace = 6f + k * Cadencia;
                float t = Projectile.ai[2] - nace;
                if (t < 0f || t > VidaAnillo) continue;
                float v = MathHelper.Clamp(t / VidaAnillo, 0f, 1f);
                float radio = RadioAnillo(v);
                float dist = Vector2.Distance(targetHitbox.Center(), Projectile.Center);
                float margen = 60f + MathF.Max(targetHitbox.Width, targetHitbox.Height) * 0.5f;
                if (MathF.Abs(dist - radio) < margen) return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;

            for (int k = 0; k < NAnillos; k++)
            {
                float nace = 6f + k * Cadencia;
                float tk = t - nace;
                if (tk < 0f) continue;
                float v = MathHelper.Clamp(tk / VidaAnillo, 0f, 1f);
                if (v >= 1f) continue;

                bool doble = k == NAnillos - 1;
                float radio = RadioAnillo(v);
                float alfaVida = OndaLib.Falloff(v, OndaFalloff.Quadratic);
                Color c = doble ? DefinitivasLib.GradienteJuicio(tiempo * 0.06f) : DefinitivasLib.Oro;

                // ==================== CAPA 1 — EL ANILLO (CERRADO DE VERDAD) ====================
                // LA CURA DEL AGUJERO: 56 puntos + 2 SOBLANTES (el 0 y el 1
                // REPETIDOS al final) — la costura del loop queda enterrada
                // bajo segmentos vivos y el Chaikin ya no puede morderla.
                const int SEG = 56;
                var pts = new Vector2[SEG + 2];
                var anchoNodo = new float[SEG + 2];
                for (int i = 0; i < SEG + 2; i++)
                {
                    float ang = (i % SEG) / (float)SEG * MathHelper.TwoPi;
                    float ondula = MathF.Sin(ang * 7f + tiempo * 5f + k * 2f) * 10f;
                    pts[i] = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (radio + ondula)
                        - Main.screenPosition;
                    // el perfil: el anillo nace delgado y engorda su brillo al frente
                    anchoNodo[i] = 0.75f + 0.25f * MathF.Sin(ang * 3f + tiempo * 4f);
                }
                float anchoBase = (doble ? 34f : 22f) * (1f - v * 0.4f);

                RayoStrip.AbrirLote();
                try
                {
                    // LA FUNDA EXTERNA — el halo que envuelve al bronce
                    RayoStrip.FilamentoEnLote(pts, anchoBase * 2.4f, DefinitivasLib.Alfa(c, 0.35f),
                        DefinitivasLib.Oro, 0.35f * alfaVida,
                        StormTaper.Flat, 100 + k, 0, true, true, false, null, anchoNodo);
                    // EL CUERPO del anillo
                    RayoStrip.FilamentoEnLote(pts, anchoBase, c, DefinitivasLib.BlancoCalido,
                        0.85f * alfaVida,
                        StormTaper.Flat, 101 + k, 0, true, true, false, null, anchoNodo);
                    // LA VENA — el filo blanco que corre por el frente
                    RayoStrip.FilamentoEnLote(pts, anchoBase * 0.4f, DefinitivasLib.BlancoCalido, Color.White,
                        0.95f * alfaVida,
                        StormTaper.Flat, 102 + k, 0, true, true, true, null, anchoNodo);
                }
                finally { RayoStrip.CerrarLote(); }

                // ==================== CAPA 2 — EL ECO (el gemelo de la campana mayor) ====================
                if (doble)
                {
                    var pts2 = new Vector2[SEG + 2];
                    for (int i = 0; i < SEG + 2; i++)
                    {
                        float ang = (i % SEG) / (float)SEG * MathHelper.TwoPi;
                        float ondula = MathF.Sin(ang * 5f - tiempo * 4f) * 12f;
                        pts2[i] = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (radio * 0.82f + ondula)
                            - Main.screenPosition;
                    }
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FilamentoEnLote(pts2, anchoBase * 0.7f, DefinitivasLib.Ambar, DefinitivasLib.Oro,
                            0.7f * alfaVida,
                            StormTaper.Flat, 104, 0, true, true, true, null, null);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }

                // ==================== CAPA 4 — EL BADAJO (la maquinaria interior) ====================
                if (v > 0.15f)
                {
                    var pts3 = new Vector2[SEG + 2];
                    for (int i = 0; i < SEG + 2; i++)
                    {
                        float ang = (i % SEG) / (float)SEG * MathHelper.TwoPi;
                        pts3[i] = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (radio * 0.55f)
                            - Main.screenPosition;
                    }
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FilamentoEnLote(pts3, anchoBase * 0.35f, DefinitivasLib.Alfa(DefinitivasLib.Violeta, 0.6f),
                            DefinitivasLib.Alfa(DefinitivasLib.Cian, 0.6f), 0.5f * alfaVida,
                            StormTaper.Flat, 106 + k, 0, true, true, true, null, null);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }

                // ==================== CAPA 3 — LAS NOTAS (las estrellas que cabalgan) ====================
                VFXCore.Begin();
                int nNotas = doble ? 8 : 5;
                for (int j = 0; j < nNotas; j++)
                {
                    float ang = tiempo * (1.2f + k * 0.3f) + j * MathHelper.TwoPi / nNotas;
                    Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                    float esc = (doble ? 110f : 80f) * (1f - v * 0.3f);
                    // el halo suave de la nota
                    VFXCore.Quad(pos, DefinitivasLib.Alfa(c, 0.35f * (1f - v)), new Vector2(esc * 1.6f, esc * 1.6f));
                    // LA ESTRELLA de cuatro puntas (la nota de verdad)
                    if (DefinitivasLib.TexStar != null)
                        VFXCore.Quad(pos, DefinitivasLib.Alfa(Color.White, 0.9f * (1f - v)),
                            new Vector2(esc, esc), tiempo * 2f + j, DefinitivasLib.TexStar);
                }
                VFXCore.FlushAdditive();
            }

            // ==================== CAPA 5 — EL LATIDO CENTRAL ====================
            // (los CUATRO HALOS APILADOS del rayo de fusión, pulsando con el
            // tañido más reciente — la campana tiene corazón)
            VFXCore.Begin();
            float alAnillo = 0f;
            for (int k = 0; k < NAnillos; k++)
            {
                float nace = 6f + k * Cadencia;
                float tk = t - nace;
                if (tk >= 0f && tk < 10f) alAnillo = MathF.Max(alAnillo, 1f - tk / 10f);
            }
            float late = 0.55f + 0.25f * MathF.Sin(tiempo * 4f) + 0.35f * alAnillo;
            Color grad = DefinitivasLib.GradienteJuicio(tiempo * 0.08f);
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(grad, 0.10f * late), new Vector2(520f, 520f));
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.20f * late), new Vector2(300f, 300f));
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.38f * late), new Vector2(190f, 190f));
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.55f * late), new Vector2(110f, 110f));
            if (DefinitivasLib.TexStar != null)
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.White, 0.85f),
                    new Vector2(70f, 70f) * late, tiempo, DefinitivasLib.TexStar);
            VFXCore.FlushAdditive();

            // v6.54.5/.6 — EL CONTRATO DEL LOTE: TODO PreDraw sale con el lote VIVO.
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
