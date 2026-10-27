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
    /// PARTIDORDEMUNDOSPROJECTILE — v6.50.66 — ARMA 5: EL PARTIDOR DE
    /// MUNDOS.
    ///
    /// La receta del «Universe Splitter» de Calamity (el botón de
    /// apocalipsis: UN clic = UNA línea que parte el mundo) hecha ritual:
    ///
    ///   · LA CONTENCIÓN (22 t): una línea VERTICAL fina tiembla sobre el
    ///     cursor — el telegrafo de la casa (anticipación AAA): el cielo
    ///     se oscurece por los bordes, el chisporroteo sube.
    ///   · LA PARTICIÓN (34 t): LA COLUMNA — 260 px de ancho, de 1400 px
    ///     ARRIBA del punto a 500 px debajo: nace de golpe, blanca con el
    ///     borde del gradiente, el suelo ESTALLA con la doble onda y el
    ///     temblor más fuerte de la bolsa (Kick 14).
    ///   · EL FRAGMENTO: chispas doradas saltan de la herida todo el rato.
    ///
    /// El golpe: la columna entera es el hitbox (un rectángulo gigante
    /// una sola vez). ai[0] = fase (0 contención · 1 partición · 2 cierre).
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

            // LA HERIDA VIVA: chispas que saltan de la columna todo el rato
            if (particionado && !Main.dedServ && Main.netMode != NetmodeID.Server && Projectile.ai[2] < TParticion)
            {
                float vida = Projectile.ai[2] / (float)TParticion;
                for (int i = 0; i < 4; i++)
                {
                    float y = Main.rand.NextFloat(-1100f, 300f);
                    Vector2 pos = Projectile.Center + new Vector2(0f, y);
                    Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                        new Vector2(Main.rand.NextFloat(-9f, 9f), Main.rand.NextFloat(-4f, 2f)), 0,
                        DefinitivasLib.GradienteJuicio(tiempo * 0.2f + y * 0.001f), 1.1f);
                    d.noGravity = true;
                }
                Lighting.AddLight(Projectile.Center, 2.2f, 2.0f, 1.5f);
                Lighting.AddLight(Projectile.Center + new Vector2(0f, -700f), 1.6f, 1.4f, 1.1f);
                if (Projectile.ai[2] % 6 == 0) OndaLib.Kick(9, 10);
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

            // ==================== LA CONTENCIÓN (el telegrafo) ====================
            if (!particionado)
            {
                float ansia = t / (float)TContencion;
                Vector2 arriba = Projectile.Center + new Vector2(0f, -1400f);
                Vector2 abajo = Projectile.Center + new Vector2(0f, 420f);

                var pts = new Vector2[14];
                for (int i = 0; i < 14; i++)
                {
                    float q = i / 13f;
                    float tiemblo = MathF.Sin(tiempo * 30f + q * 20f) * (2f + 8f * ansia) * q;
                    pts[i] = Vector2.Lerp(arriba, abajo, q) + new Vector2(tiemblo, 0f) - Main.screenPosition;
                }
                Color cTele = DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.3f + 0.5f * ansia);
                RayoStrip.AbrirLote();
                try
                {
                    RayoStrip.FilamentoEnLote(pts, 4f + 10f * ansia, DefinitivasLib.Oro, DefinitivasLib.BlancoCalido,
                        0.5f + 0.5f * ansia, StormTaper.Flat, 91, (int)(t * 0.5f), false, true, true, null, null);
                }
                finally { RayoStrip.CerrarLote(); }

                // EL VELO que oscurece los bordes (la sala contiene la respiración)
                SombrasLib.Vignette(0.20f + 0.22f * ansia);
            }

            // ==================== LA PARTICIÓN (la columna) ====================
            if (particionado)
            {
                float vida = MathHelper.Clamp(Projectile.ai[2] / (float)TParticion, 0f, 1f);
                float nace = DefinitivasLib.DeGolpe(MathHelper.Clamp(Projectile.ai[2] / 5f, 0f, 1f));
                float muere = MathHelper.Clamp(1f - (Projectile.ai[2] - TParticion) / 16f, 0f, 1f);
                float potencia = nace * muere;

                Vector2 arriba = Projectile.Center + new Vector2(0f, -1400f);
                Vector2 abajo = Projectile.Center + new Vector2(0f, 420f);

                // LAS TRES CAPAS + VENA (la receta del cono, en vertical)
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
                    RayoStrip.FilamentoEnLote(pts, 210f * potencia, gBorde, DefinitivasLib.Oro, 0.30f * potencia,
                        StormTaper.Flat, 5, 0, false, false, false, null, ancho);
                    RayoStrip.FilamentoEnLote(pts, 120f * potencia, DefinitivasLib.Oro, DefinitivasLib.BlancoCalido,
                        0.6f * potencia, StormTaper.Flat, 6, 0, false, false, false, null, ancho);
                    RayoStrip.FilamentoEnLote(pts, 46f * potencia, DefinitivasLib.BlancoCalido, Color.White,
                        0.97f * potencia, StormTaper.Flat, 7, 0, false, false, true, null, ancho);
                }
                finally { RayoStrip.CerrarLote(); }

                // LA BASE: el cráter de luz + las ondas de choque dobles
                VFXCore.Begin();
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(gBorde, 0.5f * potencia), new Vector2(700f, 700f));
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.White, 0.6f * potencia), new Vector2(300f, 300f));
                if (VFXCore.DestelloFinal != null)
                    DefinitivasLib.Blit(Main.spriteBatch, VFXCore.DestelloFinal, Projectile.Center - Main.screenPosition,
                        DefinitivasLib.Alfa(Color.White, 0.9f * potencia), new Vector2(620f, 620f), vida * 0.8f);
                VFXCore.FlushAdditive();

                if (Main.spriteBatch != null && vida < 1f)
                {
                    VFXCore.CerrarLoteSiAbierto();
                    try
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                            null, Main.GameViewMatrix.TransformationMatrix);
                        OndaLib.Shock(Main.spriteBatch, Projectile.Center - Main.screenPosition, vida, 1500f,
                            DefinitivasLib.Oro, 1f, 3, 20f, OndaFalloff.Quadratic, true);
                        OndaLib.Shock(Main.spriteBatch, Projectile.Center - Main.screenPosition, vida * 0.75f, 1100f,
                            DefinitivasLib.BlancoCalido, 0.7f, 4, 12f, OndaFalloff.Quadratic, false);
                        Main.spriteBatch.End();
                    }
                    catch { }
                    VFXCore.ReabrirLoteVanilla();
                }
            }
            // v6.54.5 — el contrato del lote: cubre la contención sin partición
            // (Vignette) y la partición con vida >= 1f — ambos salían CERRADOS.
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
