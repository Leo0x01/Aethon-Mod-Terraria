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
    /// PARTIDORDEMUNDOSPROJECTILE — v6.54.7 — ARMA 3 DE LAS CINCO: EL
    /// PARTIDOR DE MUNDOS.
    ///
    /// LA TRANSCENDENCIA (la letra: «tienes que mejorar aún más los
    /// efectos visuales, hacerlos más impresionantes»): la columna de la
    /// .54.6 eran fundas de color sólido — ahora es LA CARNE DE RUIDO
    /// HIRVIENDO (la pila de seis pasadas texturizadas con el flujo
    /// INVERTIDO: la carne hierve HACIA ARRIBA, como el calor de una
    /// herida en el mundo), más las técnicas nuevas de la investigación:
    ///
    ///   CAPA 1 — EL VELO DE LA CONTENCIÓN (heredado, con el pulso a 12 Hz).
    ///   CAPA 2 — EL TELEGRAFO VIVO: la aguja TEXTURIZADA (la carne corre
    ///   por la línea antes del corte) + las cinco runas + las chispas
    ///   convergentes + LOS CORREDORES (técnica nº15).
    ///   CAPA 3 — LA COLUMNA DE CARNE: LA PILA DE SEIS PASADAS — aura ·
    ///   cuerpo con el color que viaja · REMOLINO hirviendo hacia arriba ·
    ///   núcleo en banda · la vena blanca · LA ABERRACIÓN CROMÁTICA (los
    ///   bordes rojo/cian de la técnica nº8).
    ///   CAPA 4 — EL CRÁTER DE LUZ: los cuatro halos apilados PULSANDO a
    ///   12 Hz + el destello girando + LOS PINCHOS DE DIFRACCIÓN
    ///   horizontales (técnica nº6 — el destello de lente del impacto).
    ///   CAPA 5 — LAS TRES ONDAS DE CHOQUE (heredadas) + EL SEGUNDO
    ///   GOLPE (nuevo): a los 8 t del corte la herida RECHINA — flash
    ///   blanco + Kick + una cuarta onda fina retrasada.
    ///   CAPA 6 — LAS GRIETAS: las dos de la .54.6 + DOS ECOS espectrales
    ///   más finos a ±150 px (el mundo se astilla alrededor del corte).
    ///   CAPA 7 — LA HEMORRAGIA: chispas con gravedad + LOS ESCOMBROS
    ///   (nuevo): esquirlas doradas que saltan de la fisura girando y
    ///   caen de verdad + EL HUMO DE LA HERIDA (nuevo, la librería de
    ///   brumas: BrumaFX.Column — columnas de humo oscuro que suben de la
    ///   base mientras el mundo sigue abierto) + el temblor rítmico.
    /// </summary>
    public class PartidorDeMundosProjectile : ModProjectile
    {
        private const int TContencion = 22;
        private const int TParticion = 34;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 260;
            Projectile.height = 1900;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TContencion + TParticion + 20;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 50;
            Projectile.netImportant = true;
        }

        private bool _segundoGolpe;    // EL RECHINIDO (la herida vuelve a quejarse a los 8 t)

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;

            // v6.54.6 — el failsafe global de vida máxima.
            if (t > 1200f) { Projectile.Kill(); return; }

            if (t < 2f)
            {
                // EL ANCLAJE: vuela hasta el cursor y se planta
                Projectile.Center = Main.MouseWorld;
                Projectile.velocity = Vector2.Zero;
            }

            if (t == TContencion)
            {
                // ¡LA PARTICIÓN!
                Projectile.ai[0] = 1f;
                Projectile.ai[2] = 0;
                _segundoGolpe = false;
                DefinitivasLib.Sonar(SoundID.Item122.WithPitchOffset(-0.2f).WithVolumeScale(1.2f), Projectile.Center);
                DefinitivasLib.Sonar(SoundID.Item88.WithPitchOffset(-0.5f).WithVolumeScale(0.9f), Projectile.Center);
                OndaLib.Flash(new Color(255, 250, 235), 0.44f, 14, Projectile.Center);
                OndaLib.Kick(14, 22);
                if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        float y = Main.rand.NextFloat(-1400f, 400f);
                        Vector2 pos = Projectile.Center + new Vector2(Main.rand.NextFloat(-130f, 130f), y);
                        Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                            new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-3f, 3f)), 0,
                            DefinitivasLib.GradienteJuicio(Main.rand.NextFloat(1f)), 1.4f);
                        d.noGravity = true;
                    }
                }
            }

            bool particionado = Projectile.ai[0] >= 1f;

            // ==================== CAPA 5b — EL SEGUNDO GOLPE (EL RECHINIDO) ====================
            // (nuevo: a los 8 t del corte la herida vuelve a quejarse — el
            // eco del apocalipsis, flash blanco + Kick + los escombros gruesos)
            if (particionado && !_segundoGolpe && Projectile.ai[2] >= 8f)
            {
                _segundoGolpe = true;
                DefinitivasLib.Sonar(SoundID.Item88.WithPitchOffset(-0.7f).WithVolumeScale(0.8f), Projectile.Center);
                if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                {
                    OndaLib.Flash(new Color(255, 244, 210), 0.26f, 10, Projectile.Center);
                    OndaLib.Kick(9, 14);
                    // LOS ESCOMBROS GRUESOS: esquirlas doradas que saltan girando
                    for (int i = 0; i < 16; i++)
                    {
                        Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-80f, 80f), -10f),
                            DustID.FireworkFountain_Yellow,
                            new Vector2(Main.rand.NextFloat(-11f, 11f), Main.rand.NextFloat(-13f, -4f)), 0,
                            DefinitivasLib.Ambar, 1.5f);
                        d.noGravity = false;        // giran y caen de verdad
                    }
                }
            }

            // ==================== CAPA 7 — LA HEMORRAGIA (chispas + escombros) ====================
            if (particionado && !Main.dedServ && Main.netMode != NetmodeID.Server && Projectile.ai[2] < TParticion)
            {
                for (int i = 0; i < 4; i++)
                {
                    float y = Main.rand.NextFloat(-1100f, 300f);
                    Vector2 pos = Projectile.Center + new Vector2(0f, y);
                    Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                        new Vector2(Main.rand.NextFloat(-9f, 9f), Main.rand.NextFloat(-4f, 2f)), 0,
                        DefinitivasLib.GradienteJuicio(tiempo * 0.2f + y * 0.001f), 1.1f);
                    d.noGravity = true;
                }
                // LAS CHISPAS DE LA BASE ahora con GRAVEDAD (saltan y caen de verdad)
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-60f, 60f), 0f),
                        DustID.FireworkFountain_Yellow, new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-9f, -3f)), 0,
                        DefinitivasLib.Ambar, 1.2f);
                    d.noGravity = false;          // la gravedad de verdad las trae de vuelta
                }
                Lighting.AddLight(Projectile.Center, 2.2f, 2.0f, 1.5f);
                Lighting.AddLight(Projectile.Center + new Vector2(0f, -700f), 1.6f, 1.4f, 1.1f);
                if (Projectile.ai[2] % 6 == 0) OndaLib.Kick(9, 10);

                // ==================== CAPA 6b — EL POLVO DE LAS GRIETAS ====================
                float vidaG = MathHelper.Clamp(Projectile.ai[2] / (float)TParticion, 0f, 1f);
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    if (Main.rand.NextBool(3))
                    {
                        float lejos = Main.rand.NextFloat(80f, 520f * vidaG);
                        Vector2 posG = Projectile.Center + new Vector2(lado * lejos, Main.rand.NextFloat(-8f, 8f));
                        Dust d = Dust.NewDustPerfect(posG, DustID.GoldFlame,
                            new Vector2(lado * Main.rand.NextFloat(1f, 4f), Main.rand.NextFloat(-4f, -1f)), 0,
                            DefinitivasLib.GradienteJuicio(lejos * 0.002f), 0.9f);
                        d.noGravity = true;
                    }
                }
            }

            // EL CIERRE: la columna se desvanece
            if (particionado && Projectile.ai[2] > TParticion + 16)
                Projectile.Kill();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // SOLO durante la partición (la contención no corta)
            if (Projectile.ai[0] >= 1f && Projectile.ai[2] < TParticion + 8)
            {
                var golpe = new Rectangle((int)(Projectile.Center.X - 130f), (int)(Projectile.Center.Y - 1400f),
                    260, 1900);
                return golpe.Intersects(targetHitbox);
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;
            bool particionado = Projectile.ai[0] >= 1f;

            // ==================== CAPA 1 — EL VELO DE LA CONTENCIÓN ====================
            // (la sala contiene la respiración: densa hasta el estallido, se
            // disuelve despacio durante la partición — y PULSA a 12 Hz con la herida abierta)
            {
                float latido = particionado ? 0.85f + 0.15f * MathF.Sin(tiempo * 12f) : 1f;
                float ansiaVelo = particionado
                    ? MathHelper.Clamp(1f - Projectile.ai[2] / (float)(TParticion + 10), 0f, 1f) * 0.30f * latido
                    : 0.20f + 0.22f * (t / (float)TContencion);
                SombrasLib.Vignette(ansiaVelo);
            }

            // ==================== LA CONTENCIÓN (el telegrafo vivo) ====================
            if (!particionado)
            {
                float ansia = t / (float)TContencion;
                Vector2 arriba = Projectile.Center + new Vector2(0f, -1400f);
                Vector2 abajo = Projectile.Center + new Vector2(0f, 420f);

                // === CAPA 2a — LA LÍNEA QUE TIEMBLA (la aguja TEXTURIZADA: la
                // carne ya corre por la línea antes del corte — técnica nº15) ===
                var pts = new Vector2[14];
                var semi = new float[14];
                for (int i = 0; i < 14; i++)
                {
                    float q = i / 13f;
                    float tiemblo = MathF.Sin(tiempo * 30f + q * 20f) * (2f + 8f * ansia) * q;
                    pts[i] = Vector2.Lerp(arriba, abajo, q) + new Vector2(tiemblo, 0f) - Main.screenPosition;
                    semi[i] = (2f + 5f * ansia);
                }
                Texture2D carne = DefinitivasLib.TexRuido;
                if (carne != null)
                {
                    RayoStrip.AbrirLote();
                    try
                    {
                        // EL HALO de la aguja
                        RayoStrip.FajaTexturadaEnLote(pts, semi,
                            DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.28f * (0.5f + 0.5f * ansia)),
                            carne, 300f, 0.8f + ansia * 2.4f, tiempo, 0f, 1f);
                        // LA AGUJA — la banda caliente que LATA
                        float latidoA = 0.55f + 0.45f * MathF.Sin(tiempo * MathHelper.Lerp(2f, 4f, ansia) * MathHelper.TwoPi);
                        RayoStrip.FajaTexturadaEnLote(pts, ArraySemis(semi, 0.4f),
                            DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, (0.30f + 0.40f * ansia) * latidoA),
                            carne, 240f, 1.4f + ansia * 3f, tiempo, 0.36f, 0.64f);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }

                // === CAPA 2b — LAS CINCO RUNAS (se encienden UNA A UNA por la línea) ===
                VFXCore.Begin();
                int encendidas = 1 + (int)(ansia * 4.999f);
                for (int r = 0; r < 5; r++)
                {
                    float q = 0.12f + r * 0.19f;
                    Vector2 posR = Vector2.Lerp(arriba, abajo, q) + new Vector2(MathF.Sin(tiempo * 26f + q * 20f) * (2f + 8f * ansia) * q, 0f);
                    bool viva = r < encendidas;
                    float alfaR = viva ? (0.55f + 0.35f * MathF.Sin(tiempo * 6f + r)) : 0.08f;
                    Color cR = viva ? DefinitivasLib.GradienteJuicio(r * 0.2f + tiempo * 0.1f) : DefinitivasLib.Oro;
                    VFXCore.Quad(posR, DefinitivasLib.Alfa(cR, alfaR * 0.35f), new Vector2(70f, 70f));
                    if (DefinitivasLib.TexStar != null)
                        VFXCore.Quad(posR, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, alfaR),
                            new Vector2(34f, 34f), tiempo * 1.5f + r, DefinitivasLib.TexStar);
                }
                // LOS CORREDORES: los paquetes de luz que RECORREN la aguja
                for (int i = 0; i < 3; i++)
                {
                    float prog = DefinitivasLib.Frac(tiempo * (0.5f + ansia * 1.8f) + i / 3f);
                    VFXCore.Quad(Vector2.Lerp(abajo, arriba, prog),
                        DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.35f * (1f - prog * 0.4f) * (0.4f + 0.6f * ansia)),
                        new Vector2(12f, 12f));
                }
                VFXCore.FlushAdditive();

                // === CAPA 2c — LAS CHISPAS CONVERGENTES (el aire se acerca a la línea) ===
                if (Main.rand.NextBool(2))
                {
                    Vector2 posC = Projectile.Center + new Vector2(Main.rand.NextFloat(-320f, 320f), Main.rand.NextFloat(-900f, 300f));
                    Dust d = Dust.NewDustPerfect(posC, DustID.GoldFlame,
                        (Projectile.Center - posC) * 0.012f, 0, DefinitivasLib.Oro, 0.7f);
                    d.noGravity = true;
                }
            }

            // ==================== LA PARTICIÓN (el cine completo) ====================
            if (particionado)
            {
                float vida = MathHelper.Clamp(Projectile.ai[2] / (float)TParticion, 0f, 1f);
                float nace = DefinitivasLib.DeGolpe(MathHelper.Clamp(Projectile.ai[2] / 5f, 0f, 1f));
                float muere = MathHelper.Clamp(1f - (Projectile.ai[2] - TParticion) / 16f, 0f, 1f);
                float potencia = nace * muere;

                Vector2 arriba = Projectile.Center + new Vector2(0f, -1400f);
                Vector2 abajo = Projectile.Center + new Vector2(0f, 420f);

                // === CAPA 3 — LA COLUMNA DE CARNE (LA PILA DE SEIS PASADAS con
                // el flujo INVERTIDO: la carne HIERVE hacia arriba — el calor
                // de la herida) + EL FLICKER inconmensurable de la técnica nº11 ===
                const int N = 20;
                var espina = new Vector2[N];
                var perfil = new float[N];
                float flicker = DefinitivasLib.Flicker(tiempo);
                for (int i = 0; i < N; i++)
                {
                    float q = i / (float)(N - 1);
                    espina[i] = Vector2.Lerp(arriba, abajo, q);
                    // v6.55.0 — LA CURA DE LAS ESQUINAS DEL CLAMP: el Clamp
                    // duro (0.25..1) dejaba DOS QUIEBRES bruscos por lado en
                    // la silueta de la columna — ahora la saturación es SUAVE
                    // (SmoothStep: derivada cero en ambos extremos).
                    float s = MathF.Sin(q * MathHelper.Pi) * 1.4f - 0.25f;
                    float sat = MathHelper.SmoothStep(0f, 0.75f, MathHelper.Clamp(s, 0f, 0.75f));
                    perfil[i] = (0.25f + 0.75f * sat) *
                        150f * potencia * flicker;
                }
                DefinitivasLib.HazFusionEspina(espina, perfil,
                    DefinitivasLib.GradienteJuicio(tiempo * 0.07f), potencia, tiempo, 5,
                    true, -1.15f, true);

                // === CAPA 3b — LAS MICRO-FRACTURAS (dos líneas espectrales a los lados) ===
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    var ptsF = new Vector2[N];
                    for (int i = 0; i < N; i++)
                    {
                        float q = i / (float)(N - 1);
                        float xF = lado * (90f + 40f * MathF.Sin(q * MathHelper.Pi));
                        ptsF[i] = Vector2.Lerp(arriba, abajo, q) + new Vector2(xF, 0f) - Main.screenPosition;
                    }
                    Color cF = lado < 0 ? DefinitivasLib.Alfa(DefinitivasLib.Cian, 0.5f) : DefinitivasLib.Alfa(DefinitivasLib.Violeta, 0.5f);
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FilamentoEnLote(ptsF, 26f * potencia, cF, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.5f),
                            0.40f * potencia, StormTaper.Flat, 9 + lado, 0, false, false, false, null, null);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }

                // === CAPA 4 — EL CRÁTER DE LUZ (los cuatro halos PULSANDO a
                // 12 Hz + el destello + LOS PINCHOS DE DIFRACCIÓN) ===
                Color gBorde = DefinitivasLib.GradienteJuicio(tiempo * 0.09f);
                float pulsoC = 0.8f + 0.2f * MathF.Sin(tiempo * 12f);
                VFXCore.Begin();
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(gBorde, 0.20f * potencia), new Vector2(1300f, 1300f) * potencia * pulsoC);
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(gBorde, 0.34f * potencia), new Vector2(700f, 700f) * potencia);
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.5f * potencia), new Vector2(380f, 380f) * potencia * pulsoC);
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.White, 0.65f * potencia), new Vector2(220f, 220f) * potencia);
                if (VFXCore.DestelloFinal != null)
                    VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.White, 0.9f * potencia),
                        new Vector2(620f, 620f) * potencia, vida * 0.8f, VFXCore.DestelloFinal);
                // LOS PINCHOS DE DIFRACCIÓN HORIZONTALES (el destello de lente del impacto)
                DefinitivasLib.PinchosDifraccion(Projectile.Center, 0f, 900f * potencia,
                    DefinitivasLib.BlancoCalido, 0.5f * potencia, tiempo);
                VFXCore.FlushAdditive();

                // === CAPA 7b — EL HUMO DE LA HERIDA (la LIBRERÍA DE BRUMAS:
                // columnas de humo oscuro subiendo de la base mientras el
                // mundo sigue abierto — la letra: «para qué fabricar óvalos
                // planos si tenemos las librerías de brumas») ===
                {
                    VFXCore.CerrarLoteSiAbierto();
                    try
                    {
                        BrumaFX.BeginMass();
                        Vector2 basePantalla = Projectile.Center - Main.screenPosition;
                        BrumaFX.Column(basePantalla + new Vector2(-46f, -8f), 320f * potencia, 120f,
                            new Color(34, 22, 12), 901, tiempo, 0.34f * potencia, 0.55f);
                        BrumaFX.Column(basePantalla + new Vector2(46f, -8f), 280f * potencia, 100f,
                            new Color(40, 26, 14), 902, tiempo * 1.1f, 0.30f * potencia, 0.65f);
                        Main.spriteBatch.End();
                    }
                    catch { VFXCore.CerrarLoteSiAbierto(); }
                    VFXCore.ReabrirLoteVanilla();
                }

                // === CAPA 5 — LAS ONDAS DE CHOQUE (por triplicado + la del RECHINIDO) ===
                if (Main.spriteBatch != null && vida < 1f)
                {
                    VFXCore.CerrarLoteSiAbierto();
                    try
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                            null, Main.GameViewMatrix.TransformationMatrix);
                        // LAS ONDAS: la grande dorada, la media blanca, la fina ámbar retrasada
                        OndaLib.Shock(Main.spriteBatch, Projectile.Center - Main.screenPosition, vida, 1500f,
                            DefinitivasLib.Oro, 1f, 3, 20f, OndaFalloff.Quadratic, true);
                        OndaLib.Shock(Main.spriteBatch, Projectile.Center - Main.screenPosition, vida * 0.75f, 1100f,
                            DefinitivasLib.BlancoCalido, 0.7f, 4, 12f, OndaFalloff.Quadratic, false);
                        OndaLib.Shock(Main.spriteBatch, Projectile.Center - Main.screenPosition, MathF.Max(0f, vida - 0.25f), 800f,
                            DefinitivasLib.Ambar, 0.5f, 8, 8f, OndaFalloff.Slow, false);
                        // LA ONDA DEL RECHINIDO (el segundo golpe: fina, blanca, desde el 8 t)
                        if (_segundoGolpe)
                            OndaLib.Shock(Main.spriteBatch, Projectile.Center - Main.screenPosition,
                                MathHelper.Clamp((Projectile.ai[2] - 8f) / 22f, 0f, 1f), 1900f,
                                DefinitivasLib.BlancoCalido, 0.45f, 6, 10f, OndaFalloff.Slow, false);
                        Main.spriteBatch.End();
                    }
                    catch { VFXCore.CerrarLoteSiAbierto(); }
                    VFXCore.ReabrirLoteVanilla();
                }

                // === CAPA 6 — LAS GRIETAS DEL SUELO (las dos de verdad + los DOS ECOS) ======
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    const int NG = 14;
                    var ptsG = new Vector2[NG];
                    var anchoG = new float[NG];
                    float largoG = 560f * MathHelper.Clamp(vida * 2.2f, 0f, 1f) * muere;
                    for (int i = 0; i < NG; i++)
                    {
                        float q = i / (float)(NG - 1);
                        float yG = MathF.Sin(q * 9f + lado * 3f + tiempo * 2f) * 6f;
                        ptsG[i] = Projectile.Center + new Vector2(lado * (60f + largoG * q), yG) - Main.screenPosition;
                        anchoG[i] = 1f - q * 0.8f;
                        if (anchoG[i] < 0.1f) anchoG[i] = 0.1f;
                    }
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FilamentoEnLote(ptsG, 34f * potencia, DefinitivasLib.GradienteJuicio(lado * 0.25f + tiempo * 0.1f),
                            DefinitivasLib.BlancoCalido, 0.55f * potencia,
                            StormTaper.Flat, 12 + lado, 0, false, true, true, null, anchoG);
                    }
                    finally { RayoStrip.CerrarLote(); }

                    // EL ECO DE LA GRIETA (más lejos, más fino, espectral)
                    var ptsE = new Vector2[NG];
                    for (int i = 0; i < NG; i++)
                    {
                        float q = i / (float)(NG - 1);
                        float yE = MathF.Sin(q * 7f - lado * 5f + tiempo * 1.6f) * 4f;
                        ptsE[i] = Projectile.Center + new Vector2(lado * (150f + 360f * vida * q), yE) - Main.screenPosition;
                    }
                    Color cE = lado < 0 ? DefinitivasLib.Alfa(DefinitivasLib.Cian, 0.55f) : DefinitivasLib.Alfa(DefinitivasLib.Violeta, 0.55f);
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FilamentoEnLote(ptsE, 16f * potencia * (1f - vida * 0.5f), cE,
                            DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.4f), 0.30f * potencia,
                            StormTaper.Flat, 16 + lado, 0, false, false, true, null, anchoG);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }
            }

            // v6.54.5/.6 — EL CONTRATO DEL LOTE: cubre TODOS los caminos (el
            // Vignette del velo, la contención, la partición con vida>=1).
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
