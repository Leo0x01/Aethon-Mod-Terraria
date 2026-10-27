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
    /// PARTIDORDEMUNDOSPROJECTILE — v6.54.6 — ARMA 3 DE LAS CINCO: EL
    /// PARTIDOR DE MUNDOS.
    ///
    /// LA REFORMA (la letra del usuario: «me gusta su concepto pero es
    /// lo mismo que las otras, visualmente está a nivel principiante»):
    /// el botón de apocalipsis ahora tiene SU CINE COMPLETO en siete
    /// capas — la partición del mundo se anuncia, se contiene, estalla,
    /// AGRIETA el suelo a los lados y se desangra luz:
    ///
    ///   CAPA 1 — EL VELO DE LA CONTENCIÓN: la sala oscurece por los
    ///   bordes, respirando, cada vez más densa hasta el estallido.
    ///   CAPA 2 — EL TELEGRAFO VIVO: la línea vertical fina TIEMBLA
    ///   sobre el cursor mientras CINCO RUNAS se encienden una a una a
    ///   lo largo de su caída y las chispas CONVERGEN a la línea.
    ///   CAPA 3 — LA COLUMNA: cuatro fundas + LA VENA blanca, con el
    ///   perfil que engorda al centro — nace de golpe, blanca de rabia.
    ///   CAPA 4 — EL CRÁTER DE LUZ: los CUATRO HALOS APILADOS del rayo
    ///   de fusión + el destello de cuatro puntas girando en la base.
    ///   CAPA 5 — LAS TRES ONDAS DE CHOQUE: la grande dorada, la media
    ///   blanca, la fina ámbar retrasada — el suelo ESTALLA por triplicado.
    ///   CAPA 6 — LAS GRIETAS: dos líneas de luz se ABREN en el suelo a
    ///   ambos lados de la base creciendo con la vida, escupiendo polvo
    ///   — el mundo no solo se parte: se le rompen los bordes.
    ///   CAPA 7 — LA HEMORRAGIA: chispas con GRAVEDAD real saltan de la
    ///   herida todo el rato + el temblor rítmico + la doble luz.
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

            // ==================== CAPA 7 — LA HEMORRAGIA (chispas con gravedad) ====================
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
            // disuelve despacio durante la partición)
            {
                float ansiaVelo = particionado
                    ? MathHelper.Clamp(1f - Projectile.ai[2] / (float)(TParticion + 10), 0f, 1f) * 0.30f
                    : 0.20f + 0.22f * (t / (float)TContencion);
                SombrasLib.Vignette(ansiaVelo);
            }

            // ==================== LA CONTENCIÓN (el telegrafo vivo) ====================
            if (!particionado)
            {
                float ansia = t / (float)TContencion;
                Vector2 arriba = Projectile.Center + new Vector2(0f, -1400f);
                Vector2 abajo = Projectile.Center + new Vector2(0f, 420f);

                // === CAPA 2a — LA LÍNEA QUE TIEMBLA (dos fundas: fina blanca + halo) ===
                var pts = new Vector2[14];
                for (int i = 0; i < 14; i++)
                {
                    float q = i / 13f;
                    float tiemblo = MathF.Sin(tiempo * 30f + q * 20f) * (2f + 8f * ansia) * q;
                    pts[i] = Vector2.Lerp(arriba, abajo, q) + new Vector2(tiemblo, 0f) - Main.screenPosition;
                }
                RayoStrip.AbrirLote();
                try
                {
                    RayoStrip.FilamentoEnLote(pts, 4f + 10f * ansia, DefinitivasLib.Oro, DefinitivasLib.BlancoCalido,
                        0.5f + 0.5f * ansia, StormTaper.Flat, 91, (int)(t * 0.5f), false, true, true, null, null);
                }
                finally { RayoStrip.CerrarLote(); }

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

                // === CAPA 3 — LA COLUMNA (cuatro fundas + LA VENA, perfil que engorda al centro) ===
                const int N = 24;
                var pts = new Vector2[N];
                var ancho = new float[N];
                for (int i = 0; i < N; i++)
                {
                    float q = i / (float)(N - 1);
                    pts[i] = Vector2.Lerp(arriba, abajo, q) - Main.screenPosition;
                    ancho[i] = MathHelper.Clamp(MathF.Sin(q * MathHelper.Pi) * 1.4f, 0.25f, 1f);
                }
                Color gBorde = DefinitivasLib.GradienteJuicio(tiempo * 0.09f);

                RayoStrip.AbrirLote();
                try
                {
                    // LA FUNDA EXTERNA — el tsunami del gradiente
                    RayoStrip.FilamentoEnLote(pts, 300f * potencia, gBorde, DefinitivasLib.Oro, 0.26f * potencia,
                        StormTaper.Flat, 5, 0, false, false, false, null, ancho);
                    // LA FUNDA GRUESA
                    RayoStrip.FilamentoEnLote(pts, 210f * potencia, gBorde, DefinitivasLib.Oro, 0.34f * potencia,
                        StormTaper.Flat, 5, 0, false, false, false, null, ancho);
                    // LA FUNDA MEDIA — el cuerpo
                    RayoStrip.FilamentoEnLote(pts, 120f * potencia, DefinitivasLib.Oro, DefinitivasLib.BlancoCalido,
                        0.6f * potencia, StormTaper.Flat, 6, 0, false, false, false, null, ancho);
                    // LA VENA — blanca de rabia
                    RayoStrip.FilamentoEnLote(pts, 46f * potencia, DefinitivasLib.BlancoCalido, Color.White,
                        0.97f * potencia, StormTaper.Flat, 7, 0, false, false, true, null, ancho);
                }
                finally { RayoStrip.CerrarLote(); }

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
                            0.40f * potencia, StormTaper.Flat, 9 + lado, 0, false, false, false, null, ancho);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }

                // === CAPA 4 — EL CRÁTER DE LUZ (los cuatro halos apilados + el destello) ===
                VFXCore.Begin();
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(gBorde, 0.20f * potencia), new Vector2(1300f, 1300f) * potencia);
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(gBorde, 0.34f * potencia), new Vector2(700f, 700f) * potencia);
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.5f * potencia), new Vector2(380f, 380f) * potencia);
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.White, 0.65f * potencia), new Vector2(220f, 220f) * potencia);
                if (VFXCore.DestelloFinal != null)
                    VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.White, 0.9f * potencia),
                        new Vector2(620f, 620f) * potencia, vida * 0.8f, VFXCore.DestelloFinal);
                VFXCore.FlushAdditive();

                // === CAPA 5 — LAS TRES ONDAS DE CHOQUE (por triplicado) ===
                // === CAPA 6a — LAS GRIETAS (la luz se abre en el suelo a los lados) ===
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
                        Main.spriteBatch.End();
                    }
                    catch { VFXCore.CerrarLoteSiAbierto(); }
                    VFXCore.ReabrirLoteVanilla();
                }

                // === CAPA 6 — LAS GRIETAS DEL SUELO (crecen con la vida) ===
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
                }
            }

            // v6.54.5/.6 — EL CONTRATO DEL LOTE: cubre TODOS los caminos (el
            // Vignette del velo, la contención, la partición con vida>=1).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
