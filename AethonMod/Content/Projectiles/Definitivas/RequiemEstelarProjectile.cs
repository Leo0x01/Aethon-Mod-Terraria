using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.Projectiles.Definitivas
{
    /// <summary>
    /// REQUIEMESTELARPROJECTILE — v6.54.7 — ARMA 2 DE LAS CINCO: EL
    /// RÉQUIEM ESTELAR.
    ///
    /// LA TRANSCENDENCIA (la letra: «tienes que mejorar aún más los
    /// efectos visuales… la combinación con algo más se vería genial»):
    /// los anillos de la .54.6 eran fundas de color sólido — ahora cada
    /// anillo es UN RÍO DE BRONCE: la pila texturizada con EL RUIDO
    /// FLUYENDO ALREDEDOR del círculo (la costura del loop queda cerrada
    /// POR CONSTRUCCIÓN: el periodo se ajusta a un número ENTERO de tiles
    /// de textura sobre la longitud del anillo — el ruido entra por un
    /// lado y sale por el otro SIN costura), más las técnicas nuevas:
    ///
    ///   CAPA 1 — EL RÍO DE BRONCE: cada anillo con TRES pasadas
    ///   texturizadas (aura 2.2× · cuerpo con el flujo alrededor ·
    ///   núcleo en banda) + la vena blanca heredada — el bronce CORRE.
    ///   CAPA 2 — EL ECO: el gemelo interior de la campana mayor (al
    ///   82 %, también texturizado, fluyendo EN CONTRA).
    ///   CAPA 3 — LAS NOTAS Y SUS ESTELAS: las estrellas de cuatro
    ///   puntas cabalgando con su ECO retardado (la nota de hace un
    ///   instante aún se ve — el rastro de la voz).
    ///   CAPA 4 — EL BADAJO: el anillo fino interior girando en contra.
    ///   CAPA 5 — LOS PÉTALOS DE RESONANCIA (nuevo): ocho picos de
    ///   estrella RADIALES en el frente del anillo, orientados hacia
    ///   afuera — la onda tiene DIENTES de verdad.
    ///   CAPA 6 — LA IMPLOSIÓN PREVIA (nuevo, técnica nº15): los 25 t
    ///   antes de cada tañido, chispas CONVERGEN al centro desde el
    ///   borde del radio — la campana ASPIRA antes de sonar.
    ///   CAPA 7 — EL ALIENTO DE LA CAMPANA (nuevo): al tañir, una nube
    ///   dorada de LA LIBRERÍA DE BRUMAS (BrumaFX en lote GLOW) respira
    ///   desde el centro — el bronce tiene aliento.
    ///   CAPA 8 — EL LATIDO + LA PALABRA + LA LLUVIA DE ESTRELLAS: los
    ///   halos apilados sincronizados, el flash/onda/temblor de cada
    ///   tañido y LAS ESTRELLAS DE VERDAD del cierre (estado propio:
    ///   posición y velocidad por estrella, estela de polvo, textura de
    ///   cuatro puntas girando) + EL AURA DE LA CAMPANA que queda
    ///   sonando 30 t después del último tañido.
    /// </summary>
    public class RequiemEstelarProjectile : ModProjectile
    {
        private const int NAnillos = 3;
        private const int Cadencia = 32;
        private const float RadioFinal = 1400f;
        private const int VidaAnillo = 46;
        private const int NEstrellas = 10;      // LAS ESTRELLAS DEL CIERRE (estado propio)

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

        // EL ESTADO DE LAS ESTRELLAS DEL CIERRE (visual, solo cliente)
        private readonly Vector2[] _estrellaPos = new Vector2[NEstrellas];
        private readonly Vector2[] _estrellaVel = new Vector2[NEstrellas];
        private readonly float[] _estrellaRot = new float[NEstrellas];
        private readonly bool[] _estrellaViva = new bool[NEstrellas];

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

            // EL TAÑIDO de cada anillo — LA PALABRA de la campana
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

            // ==================== CAPA 6 — LA IMPLOSIÓN PREVIA ====================
            // (los 25 t antes de cada tañido: chispas CONVERGEN al centro —
            // la campana ASPIRA su aire antes de sonar)
            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
            {
                for (int k = 0; k < NAnillos; k++)
                {
                    float momento = 6f + k * Cadencia;
                    float falta = momento - t;
                    if (falta > 0f && falta < 25f && Main.rand.NextBool(2))
                    {
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        float desde = 300f + falta * 26f;
                        Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * desde;
                        Vector2 hacia = (Projectile.Center - pos).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(9f, 16f);
                        Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, hacia, 0,
                            DefinitivasLib.GradienteJuicio(ang / MathHelper.TwoPi), 0.9f);
                        d.noGravity = true;
                    }
                }
            }

            // ==================== CAPA 8 — LA LLUVIA DE ESTRELLAS DEL CIERRE ====================
            // (el tercer anillo siembra ESTRELLAS DE VERDAD: estado propio por
            // estrella — posición, velocidad y giro — con su estela de polvo)
            float tCierre = 6f + (NAnillos - 1) * Cadencia;
            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
            {
                if (t > tCierre && t < tCierre + 26)
                {
                    for (int s = 0; s < 2; s++)
                    {
                        int slot = -1;
                        for (int i = 0; i < NEstrellas; i++) if (!_estrellaViva[i]) { slot = i; break; }
                        if (slot < 0) break;
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        float radio = Main.rand.NextFloat(200f, RadioFinal * 0.75f);
                        _estrellaPos[slot] = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio - new Vector2(0f, 360f);
                        _estrellaVel[slot] = new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(6f, 11f));
                        _estrellaRot[slot] = Main.rand.NextFloat(MathHelper.TwoPi);
                        _estrellaViva[slot] = true;
                    }
                }

                // LA VIDA DE LAS ESTRELLAS: caen, giran, dejan estela
                for (int i = 0; i < NEstrellas; i++)
                {
                    if (!_estrellaViva[i]) continue;
                    _estrellaPos[i] += _estrellaVel[i];
                    _estrellaRot[i] += 0.22f;
                    if (Main.rand.NextBool(2))
                    {
                        Dust d = Dust.NewDustPerfect(_estrellaPos[i], DustID.GoldFlame,
                            -_estrellaVel[i] * 0.2f + new Vector2(Main.rand.NextFloat(-1f, 1f), 0f), 0,
                            DefinitivasLib.GradienteJuicio(Main.rand.NextFloat(1f)), 0.8f);
                        d.noGravity = true;
                    }
                    if (_estrellaPos[i].Y > Projectile.Center.Y + 340f || _estrellaPos[i].Y > Main.screenPosition.Y + Main.screenHeight + 60f)
                        _estrellaViva[i] = false;
                }
                Lighting.AddLight(Projectile.Center, 1.6f, 1.4f, 1.0f);
            }

            // EL RASTRO DE LAS NOTAS (polvo dorado orbitando el frente de cada anillo vivo)
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
            Texture2D carne = DefinitivasLib.TexRuido;

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

                // ==================== CAPA 1 — EL RÍO DE BRONCE (CERRADO DE VERDAD) ====================
                // LA CURA DEL AGUJERO (.54.6): 56 puntos + 2 SOBLANTES (el 0 y
                // el 1 REPETIDOS al final). LA NOVEDAD (.54.7): el ruido FLUYE
                // alrededor — y el PERIODO se ajusta para que la longitud del
                // anillo sea un número ENTERO de tiles de textura: el ruido
                // entra por un lado y sale por el otro SIN COSTURA.
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

                if (carne != null)
                {
                    // EL PERIODO QUE CIERRA EL LOOP: la longitud de la
                    // circunferencia dividida en tiles ENTEROS (~170 px c/u)
                    float circun = MathHelper.TwoPi * radio;
                    int tiles = Math.Max(3, (int)MathF.Round(circun / 170f));
                    float periodo = circun / tiles;

                    RayoStrip.AbrirLote();
                    try
                    {
                        // EL AURA DEL BRONCE (2.2×, flujo lento alrededor)
                        RayoStrip.FajaTexturadaEnLote(pts, ArraySemis(anchoNodo, anchoBase * 1.1f),
                            DefinitivasLib.Alfa(c, 0.35f * alfaVida), carne,
                            periodo, 0.45f, tiempo, 0f, 1f);
                        // EL CUERPO DEL RÍO (la carne que CORRE alrededor)
                        RayoStrip.FajaTexturadaEnLote(pts, ArraySemis(anchoNodo, anchoBase * 0.5f),
                            DefinitivasLib.Alfa(c, 0.75f * alfaVida), carne,
                            periodo, 1.0f, tiempo, 0f, 1f);
                        // EL NÚCLEO EN BANDA (la vena caliente del bronce)
                        RayoStrip.FajaTexturadaEnLote(pts, ArraySemis(anchoNodo, anchoBase * 0.21f),
                            DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.85f * alfaVida), carne,
                            periodo, 1.8f, tiempo, 0.36f, 0.64f);
                        // LA VENA — el filo blanco que corre por el frente
                        RayoStrip.FilamentoEnLote(pts, anchoBase * 0.4f, DefinitivasLib.BlancoCalido, Color.White,
                            0.95f * alfaVida,
                            StormTaper.Flat, 102 + k, 0, true, true, true, null, anchoNodo);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }
                else
                {
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FilamentoEnLote(pts, anchoBase, c, DefinitivasLib.BlancoCalido,
                            0.85f * alfaVida, StormTaper.Flat, 101 + k, 0, true, true, false, null, anchoNodo);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }

                // ==================== CAPA 2 — EL ECO (el gemelo texturizado, fluyendo EN CONTRA) ====================
                if (doble && carne != null)
                {
                    var pts2 = new Vector2[SEG + 2];
                    for (int i = 0; i < SEG + 2; i++)
                    {
                        float ang = (i % SEG) / (float)SEG * MathHelper.TwoPi;
                        float ondula = MathF.Sin(ang * 5f - tiempo * 4f) * 12f;
                        pts2[i] = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (radio * 0.82f + ondula)
                            - Main.screenPosition;
                    }
                    float circun2 = MathHelper.TwoPi * radio * 0.82f;
                    int tiles2 = Math.Max(3, (int)MathF.Round(circun2 / 170f));
                    var semiEco = new float[SEG + 2];
                    for (int i = 0; i < SEG + 2; i++) semiEco[i] = anchoBase * 0.35f;
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FajaTexturadaEnLote(pts2, semiEco,
                            DefinitivasLib.Alfa(DefinitivasLib.Ambar, 0.7f * alfaVida), carne,
                            circun2 / tiles2, -0.8f, tiempo, 0f, 1f);
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

                // ============ CAPA 3 — LAS NOTAS CON SU ECO + CAPA 5 — LOS PÉTALOS ============
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
                    // EL ECO DE LA NOTA (la voz de hace un instante, más atrás en el giro)
                    Vector2 posEco = Projectile.Center + new Vector2(MathF.Cos(ang - 0.22f), MathF.Sin(ang - 0.22f)) * radio;
                    if (DefinitivasLib.TexStar != null)
                        VFXCore.Quad(posEco, DefinitivasLib.Alfa(c, 0.35f * (1f - v)),
                            new Vector2(esc * 0.6f, esc * 0.6f), tiempo * 2f + j - 0.4f, DefinitivasLib.TexStar);
                }

                // LOS PÉTALOS DE RESONANCIA: los picos radiales del frente (8
                // dientes de estrella orientados hacia afuera — la onda MUERDE)
                if (DefinitivasLib.TexStar != null)
                {
                    int nPetalos = doble ? 8 : 6;
                    for (int j = 0; j < nPetalos; j++)
                    {
                        float ang = tiempo * (0.8f + k * 0.2f) - j * MathHelper.TwoPi / nPetalos;
                        Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                        float largo = (doble ? 120f : 84f) * (1f - v * 0.4f) * (0.8f + 0.2f * MathF.Sin(tiempo * 7f + j));
                        VFXCore.Quad(pos, DefinitivasLib.Alfa(c, 0.5f * (1f - v)),
                            new Vector2(largo, largo * 0.16f), ang, DefinitivasLib.TexStar);
                    }
                }
                VFXCore.FlushAdditive();
            }

            // ==================== CAPA 8 — LAS ESTRELLAS DEL CIERRE ====================
            // (las de verdad: textura de cuatro puntas girando + estela de polvo)
            if (DefinitivasLib.TexStar != null)
            {
                VFXCore.Begin();
                for (int i = 0; i < NEstrellas; i++)
                {
                    if (!_estrellaViva[i]) continue;
                    Color cE = DefinitivasLib.GradienteJuicio(i * 0.13f + tiempo * 0.2f);
                    VFXCore.Quad(_estrellaPos[i], DefinitivasLib.Alfa(cE, 0.35f), new Vector2(90f, 90f));
                    VFXCore.Quad(_estrellaPos[i], DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.95f),
                        new Vector2(44f, 44f), _estrellaRot[i], DefinitivasLib.TexStar);
                }
                VFXCore.FlushAdditive();
            }

            // ==================== CAPA 8b — EL LATIDO CENTRAL ====================
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

            // ==================== CAPA 8c — EL AURA DE LA CAMPANA (queda SONANDO) ====================
            // (después del último tañido, la campana sigue resonando 30 t: un
            // aro grande que se disuelve lento — el último suspiro del bronce)
            float tUltimo = 6f + (NAnillos - 1) * Cadencia + VidaAnillo;
            if (t > tUltimo && t < tUltimo + 30f)
            {
                float q = (t - tUltimo) / 30f;
                if (VFXCore.Ring != null)
                    VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.5f * (1f - q)),
                        new Vector2(600f, 600f) * (0.5f + q * 0.5f), q * 0.6f, VFXCore.Ring);
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.3f * (1f - q)),
                    new Vector2(260f, 260f) * (1f + q * 0.4f));
            }
            VFXCore.FlushAdditive();

            // ==================== CAPA 7 — EL ALIENTO DE LA CAMPANA ====================
            // (al tañir, una nube dorada de LA LIBRERÍA DE BRUMAS respira desde
            // el centro — la letra: «para qué fabricar óvalos si hay brumas»)
            {
                float aliento = 0f;
                for (int k = 0; k < NAnillos; k++)
                {
                    float nace = 6f + k * Cadencia;
                    float tk = t - nace;
                    if (tk >= 0f && tk < 16f) aliento = MathF.Max(aliento, 1f - tk / 16f);
                }
                if (aliento > 0.05f)
                {
                    VFXCore.CerrarLoteSiAbierto();
                    try
                    {
                        BrumaFX.BeginGlow();
                        Vector2 centroP = Projectile.Center - Main.screenPosition;
                        BrumaFX.Cloud(centroP, 120f + 160f * (1f - aliento), new Color(255, 204, 120),
                            777, tiempo * 0.4f, puffs: 5, alpha: 0.28f * aliento);
                        Main.spriteBatch.End();
                    }
                    catch { VFXCore.CerrarLoteSiAbierto(); }
                    VFXCore.ReabrirLoteVanilla();
                }
            }

            // v6.54.5/.6 — EL CONTRATO DEL LOTE: TODO PreDraw sale con el lote VIVO.
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>LOS SEMIANCHOS de un perfil por un semiancho base.</summary>
        private static float[] ArraySemis(float[] perfil, float semi)
        {
            var r = new float[perfil.Length];
            for (int i = 0; i < perfil.Length; i++) r[i] = perfil[i] * semi;
            return r;
        }
    }
}
