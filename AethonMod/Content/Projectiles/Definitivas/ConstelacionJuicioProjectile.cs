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
    /// CONSTELACIONJUICIOPROJECTILE — v6.50.66 — ARMA 8: LA
    /// CONSTELACIÓN DEL JUICIO.
    ///
    /// La receta de los «Star Signs» del Stellar Geliath de el dios antiguo (las
    /// estrellas-proyectil que se conectan con líneas formando
    /// constelaciones dañinas): cada clic SIEMBRA una estrella dorada en
    /// el cursor (máx SIETE, viven en DefinitivasLib.Constelacion) — y
    /// cuando la séptima cae (o clicas con la figura llena), EL JUICIO:
    ///
    ///   · LOS HILOS: las estrellas se CONECTAN en orden de siembra con
    ///     cables de luz (tiras de primitivas con vaivén).
    ///   · LOS RAYOS: cada estrella recibe SU columna del cielo (el rayo
    ///     de tormenta de la casa + la columna de luz real que golpea).
    ///   · LOS ARCOS: relámpagos Saltan de estrella a estrella (el Arco
    ///     de ArcSurge de la casa).
    ///   · EL CIERRE: la figura destella entera y las estrellas se
    ///     disuelven en chispas doradas.
    ///
    /// ai[1] = 0 (siembra) · 1 (juicio) · ai[2] = tick.
    /// </summary>
    public class ConstelacionJuicioProjectile : ModProjectile
    {
        private const int TJuicio = 90;

        public override string Texture => "AethonMod/Content/Effects/Procedural/Star";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = false;          // el daño lo hacen las columnas del juicio
            Projectile.penetrate = -1;
            Projectile.timeLeft = 140;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];

            if (t < 2f)
            {
                // MODO SENTENCIA (ai[0] = 1, invocado por El Testamento): la
                // figura se siembra SOLA — SIETE estrellas en círculo alrededor
                // de la presa — y el juicio cae de inmediato.
                if (Projectile.ai[0] >= 1f)
                {
                    NPC presa = DefinitivasLib.BuscarPresa(Projectile.Center, true) ??
                                DefinitivasLib.BuscarPresa(Projectile.Center, false);
                    Vector2 centro = presa != null ? presa.Center : Main.MouseWorld;
                    DefinitivasLib.Constelacion.Reiniciar();
                    for (int k = 0; k < DefinitivasLib.Constelacion.Max; k++)
                    {
                        float ang = k / (float)DefinitivasLib.Constelacion.Max * MathHelper.TwoPi;
                        DefinitivasLib.Constelacion.Sembrar(centro + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.8f) * 250f);
                    }
                    Projectile.ai[1] = 1f;
                    Projectile.Center = centro;
                }
                else
                {
                    bool llena = DefinitivasLib.Constelacion.Contador >= DefinitivasLib.Constelacion.Max;
                    if (!llena)
                    {
                        // ==================== LA SIEMBRA ====================
                        Vector2 pos = Main.MouseWorld;
                        DefinitivasLib.Constelacion.Sembrar(pos);
                        Projectile.ai[1] = 0f;
                        Projectile.Center = pos;
                        DefinitivasLib.Sonar(SoundID.Item4.WithPitchOffset(0.1f + DefinitivasLib.Constelacion.Contador * 0.06f)
                            .WithVolumeScale(0.6f), pos);

                        // la SIETE cierra la figura: este mismo proyectil juzga
                        if (DefinitivasLib.Constelacion.Contador >= DefinitivasLib.Constelacion.Max)
                            Projectile.ai[1] = 1f;
                    }
                    else
                    {
                        // ==================== EL JUICIO (clic con la figura llena) ====================
                        Projectile.ai[1] = 1f;
                        Projectile.Center = Main.MouseWorld;
                    }
                }
            }

            if (Projectile.ai[1] >= 1f)
            {
                // ==================== EL RITUAL DEL JUICIO ====================
                if (t == 6)
                {
                    DefinitivasLib.Sonar(SoundID.Item122.WithPitchOffset(-0.3f).WithVolumeScale(1.0f), Projectile.Center);
                    OndaLib.Kick(10, 14);
                    OndaLib.Flash(new Color(255, 236, 170), 0.26f, 12, Projectile.Center);
                }

                // LAS COLUMNAS: cada estrella recibe su rayo (escalonadas — el trueno de la casa)
                int n = DefinitivasLib.Constelacion.Contador;
                for (int k = 0; k < n; k++)
                {
                    float momento = 10f + k * 7f;
                    if (MathF.Abs(t - momento) < 0.5f)
                    {
                        Vector2 estrella = DefinitivasLib.Constelacion.Estrellas[k];

                        // EL CINE: el rayo del cielo (client-side)
                        if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                            RayoSistema.Lanzar(RayoParams.Tormenta, (uint)(Main.GameUpdateCount + k * 7919),
                                estrella, null, DefinitivasLib.Oro, DefinitivasLib.BlancoCalido,
                                26f, 34, AnimRayo.VenaTrueno, true, -1);

                        // EL DAÑO: la columna real (server-synced)
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                                estrella, Vector2.Zero,
                                ModContent.ProjectileType<EstrellaStrikeProjectile>(),
                                Projectile.damage, Projectile.knockBack, Projectile.owner, 0f);

                        DefinitivasLib.Sonar(SoundID.Item122.WithPitchOffset(-0.15f + k * 0.05f).WithVolumeScale(0.7f), estrella);
                    }
                }

                // EL CIERRE: disolver las estrellas y callar
                if (t > TJuicio)
                {
                    if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                    {
                        for (int k = 0; k < n; k++)
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                                Dust d = Dust.NewDustPerfect(DefinitivasLib.Constelacion.Estrellas[k],
                                    DustID.GoldFlame, new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(2f, 7f),
                                    0, DefinitivasLib.Oro, 0.9f);
                                d.noGravity = true;
                            }
                        }
                    }
                    DefinitivasLib.Constelacion.Reiniciar();
                    Projectile.Kill();
                }
            }
            else
            {
                // LA SEMILLA vuela 30 t (su flourish estelar) y muere
                if (t > 30) Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;
            int n = DefinitivasLib.Constelacion.Contador;

            // ==================== LAS ESTRELLAS SEMBRADAS (todas, siempre) ====================
            VFXCore.Begin();
            for (int k = 0; k < n; k++)
            {
                Vector2 pos = DefinitivasLib.Constelacion.Estrellas[k];
                float late = 0.75f + 0.25f * MathF.Sin(tiempo * 3.2f + k * 1.3f);
                float tam = 30f + 10f * MathF.Sin(tiempo * 2.1f + k);

                VFXCore.Quad(pos, DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.35f * late), new Vector2(tam * 2.4f, tam * 2.4f));
                if (DefinitivasLib.TexStar != null)
                    DefinitivasLib.Blit(Main.spriteBatch, DefinitivasLib.TexStar, pos - Main.screenPosition,
                        DefinitivasLib.Alfa(Color.White, 0.95f * late), new Vector2(tam, tam), tiempo * 1.2f + k);
            }
            VFXCore.FlushAdditive();

            // ==================== LOS HILOS (la figura) ====================
            if (n >= 2)
            {
                const int SEG = 14;
                var pts = new Vector2[SEG];
                Vector2 a = DefinitivasLib.Constelacion.Estrellas[n - 2];
                Vector2 b = DefinitivasLib.Constelacion.Estrellas[n - 1];
                // y en el juicio: TODOS los tramos de la figura cerrada
                int tramos = Projectile.ai[1] >= 1f ? n : 1;
                for (int s = 0; s < tramos; s++)
                {
                    Vector2 desde = DefinitivasLib.Constelacion.Estrellas[s];
                    Vector2 hasta = DefinitivasLib.Constelacion.Estrellas[(s + 1) % n];
                    for (int i = 0; i < SEG; i++)
                    {
                        float q = i / (float)(SEG - 1);
                        float onda = MathF.Sin(q * MathHelper.Pi) * MathF.Sin(tiempo * 6f + s) * 6f;
                        Vector2 dir = (hasta - desde).SafeNormalize(Vector2.UnitX);
                        Vector2 perp = new(-dir.Y, dir.X);
                        pts[i] = Vector2.Lerp(desde, hasta, q) + perp * onda - Main.screenPosition;
                    }
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FilamentoEnLote(pts, 8f, DefinitivasLib.Oro, DefinitivasLib.BlancoCalido,
                            Projectile.ai[1] >= 1f ? 0.9f : 0.45f, StormTaper.Flat, 60 + s, 0, true, true, true, null, null);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }

                // EN EL JUICIO: los ARCOS saltan entre estrellas consecutivas
                if (Projectile.ai[1] >= 1f && t > 14 && t % 9 == 0)
                {
                    int k = (int)(t / 9f) % n;
                    Vector2 desde = DefinitivasLib.Constelacion.Estrellas[k];
                    Vector2 hasta = DefinitivasLib.Constelacion.Estrellas[(k + 1) % n];
                    RayoSistema.Lanzar(RayoParams.Arco(desde, hasta),
                        (uint)Main.rand.Next(1, int.MaxValue),
                        hasta, desde - hasta,
                        DefinitivasLib.Oro, DefinitivasLib.BlancoCalido,
                        10f, 14, AnimRayo.Arco, false);
                }
            }

            // ==================== EL FLOURISH DE LA SEMILLA NUEVA ====================
            if (Projectile.ai[1] < 1f)
            {
                float vida = MathHelper.Clamp(t / 30f, 0f, 1f);
                VFXCore.Begin();
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.5f * (1f - vida)),
                    new Vector2(160f, 160f) * (0.4f + vida));
                VFXCore.FlushAdditive();
            }
            // v6.54.5 — el contrato del lote: salir SIEMPRE con el lote vivo.
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }

    /// <summary>
    /// ESTRELLASTRIKEPROJECTILE — LA COLUMNA del juicio: el golpe real de
    /// cada estrella (90 px de radio, 20 t de vida, golpe único) con su
    /// pilar de luz vertical y su onda.
    /// </summary>
    public class EstrellaStrikeProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 180;
            Projectile.height = 180;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 20;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float vida = 1f - Projectile.timeLeft / 20f;
            float tiempo = Main.GlobalTimeWrappedHourly;
            float potencia = 1f - vida;

            // LA COLUMNA (la receta del partidor, en corto)
            Vector2 arriba = Projectile.Center + new Vector2(0f, -900f);
            Vector2 abajo = Projectile.Center + new Vector2(0f, 60f);
            const int N = 14;
            var pts = new Vector2[N];
            var ancho = new float[N];
            for (int i = 0; i < N; i++)
            {
                float q = i / (float)(N - 1);
                pts[i] = Vector2.Lerp(arriba, abajo, q) - Main.screenPosition;
                ancho[i] = MathHelper.Clamp(MathF.Sin(q * MathHelper.Pi) * 1.3f, 0.25f, 1f);
            }
            RayoStrip.AbrirLote();
            try
            {
                RayoStrip.FilamentoEnLote(pts, 70f * potencia, DefinitivasLib.Oro, DefinitivasLib.BlancoCalido,
                    0.55f * potencia, StormTaper.Flat, 80, 0, false, false, false, null, ancho);
                RayoStrip.FilamentoEnLote(pts, 26f * potencia, DefinitivasLib.BlancoCalido, Color.White,
                    0.95f * potencia, StormTaper.Flat, 81, 0, false, false, true, null, ancho);
            }
            finally { RayoStrip.CerrarLote(); }

            // EL PIE (el destello de la estrella)
            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(DefinitivasLib.Oro, 0.5f * potencia), new Vector2(340f, 340f));
            if (VFXCore.DestelloFinal != null)
                DefinitivasLib.Blit(Main.spriteBatch, VFXCore.DestelloFinal, Projectile.Center - Main.screenPosition,
                    DefinitivasLib.Alfa(Color.White, 0.85f * potencia), new Vector2(300f, 300f), vida * 1.1f);
            VFXCore.FlushAdditive();

            if (Main.spriteBatch != null && vida < 1f)
            {
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    OndaLib.Shock(Main.spriteBatch, Projectile.Center - Main.screenPosition, vida, 240f,
                        DefinitivasLib.Oro, 0.9f, 82, 10f, OndaFalloff.Quadratic, false);
                    Main.spriteBatch.End();
                }
                catch { }
                VFXCore.ReabrirLoteVanilla();
            }
            // v6.54.5 — el contrato del lote (cubre el camino vida >= 1f, que
            // saltaba el bloque de arriba y salía con el lote CERRADO).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
