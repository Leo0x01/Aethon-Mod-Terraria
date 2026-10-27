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
    /// IRADELCIELOPROJECTILE — v6.54.6 — ARMA 4 DE LAS CINCO: LA IRA DEL
    /// CIELO.
    ///
    /// LA REFORMA (la letra del usuario: «es muy buena pero visualmente
    /// deja que desear, tiene varios errores visuales y no es
    /// visualmente impresionante»): la tormenta ahora ES una tormenta —
    /// el cielo se cubre de verdad, los juicios se ANUNCIAN antes de
    /// caer, cada impacto tiene su pilar completo y el final es una
    /// columna espiral doble:
    ///
    ///   CAPA 1 — EL CIELO CUBIERTO: la banda superior de la pantalla
    ///   se oscurece con nubes vivas (quads que se arrastran) + el velo
    ///   de los bordes respirando.
    ///   CAPA 2 — LA BRUMA DORADA: polvo denso cayendo por toda la
    ///   pantalla durante la tormenta (la lluvia fina del juicio).
    ///   CAPA 3 — LOS RELÁMPAGOS AMBIENTALES: micro-rayos decorativos
    ///   que recorren el cielo cubierto entre juicio y juicio — la
    ///   tormenta está VIVA, no espera.
    ///   CAPA 4 — EL ANUNCIO: cada juicio se telegrafia 10 t antes con
    ///   una línea fina que DESCIENDE hasta la presa (el que sabe, huye).
    ///   CAPA 5 — EL CANAL DOBLE: dos rayos entrelazados con VenaTrueno
    ///   (el cine de caída) + EL NÚCLEO — la vena blanca central.
    ///   CAPA 6 — LA COLUMNA DE IMPACTO: pilar de tres fundas + halos
    ///   apilados + la nova + EL ANILLO de choque horizontal en el suelo.
    ///   CAPA 7 — LAS RAMAS Y EL REMATE: arcos chispeantes a los vecinos
    ///   con sus mini-columnas; el ÚLTIMO juicio es LA COLUMNA FINAL —
    ///   dos venas EN ESPIRAL alrededor del eje, flash y el Kick mayor.
    /// </summary>
    public class IraDelCieloProjectile : ModProjectile
    {
        private const int TTormenta = 150;
        private const int Cadencia = 17;
        private const int NJuicios = 8;
        private const int TAnuncio = 10;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = false;            // el daño lo hacen las columnas
            Projectile.penetrate = -1;
            Projectile.timeLeft = TTormenta + 40;
            Projectile.netImportant = true;
        }

        private int _golpeados;        // cuántos juicios ya cayeron
        private int _tDesdeAmbiente;   // el reloj de los relámpagos ambientales

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;

            // v6.54.6 — el failsafe global de vida máxima.
            if (t > 1200f) { Projectile.Kill(); return; }

            if (t < 2f) Projectile.Center = dueño.MountedCenter;

            // ==================== CAPA 3 — LOS RELÁMPAGOS AMBIENTALES ====================
            // (micro-rayos decorativos en el cielo cubierto: la tormenta vive)
            _tDesdeAmbiente++;
            if (!Main.dedServ && Main.netMode != NetmodeID.Server && t < TTormenta &&
                _tDesdeAmbiente > 12 && Main.rand.NextBool(5))
            {
                _tDesdeAmbiente = 0;
                float x = Main.rand.NextFloat(Main.screenPosition.X, Main.screenPosition.X + Main.screenWidth);
                Vector2 arriba = new(x, Main.screenPosition.Y + Main.rand.NextFloat(-40f, 120f));
                Vector2 abajo = new(x + Main.rand.NextFloat(-160f, 160f), Main.screenPosition.Y + Main.rand.NextFloat(220f, 420f));
                RayoSistema.Lanzar(RayoParams.Arco(arriba, abajo),
                    (uint)Main.rand.Next(1, int.MaxValue), abajo, arriba - abajo,
                    DefinitivasLib.Oro, DefinitivasLib.BlancoCalido, 5f, 10, AnimRayo.Arco, false);
                if (Main.rand.NextBool(3))
                    OndaLib.Flash(DefinitivasLib.Ambar, 0.06f, 4, arriba);
            }

            // ==================== EL ANUNCIO + LOS JUICIOS ====================
            // (cada juicio se TELEGRAFIA TAnuncio ticks antes: la línea fina
            // desciende sobre la presa — y LUEGO cae el trueno)
            if (t > 10 + TAnuncio && (t - 10f - TAnuncio) % Cadencia == 0 && _golpeados < NJuicios)
            {
                NPC presa = BuscarNoGolpeado(dueño.Center);
                if (presa != null)
                {
                    _golpeados++;
                    bool final = _golpeados == NJuicios;

                    // === CAPA 4 — EL ANUNCIO (la línea que desciende) ===
                    if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                    {
                        Vector2 cumbre = new(presa.Center.X + Main.rand.NextFloat(-30f, 30f), presa.Center.Y - 1300f);
                        Vector2 pie = new(presa.Center.X + Main.rand.NextFloat(-14f, 14f), presa.Center.Y - 30f);
                        RayoSistema.Lanzar(RayoParams.Arco(cumbre, pie),
                            (uint)Main.rand.Next(1, int.MaxValue), pie, cumbre - pie,
                            DefinitivasLib.Oro, DefinitivasLib.BlancoCalido, 3.5f, TAnuncio, AnimRayo.Arco, false);
                    }

                    // EL JUICIO cae TAnuncio ticks después del anuncio
                    Golpear(presa, final);
                }
                else _golpeados++;      // no hay nadie: el juicio pasa igual (el conteo avanza)
            }

            // ==================== CAPA 2 — LA BRUMA DORADA (la lluvia fina) ====================
            if (!Main.dedServ && Main.netMode != NetmodeID.Server && t < TTormenta)
            {
                int gotas = 3;
                for (int i = 0; i < gotas; i++)
                {
                    Vector2 pos = new(Main.rand.NextFloat(Main.screenPosition.X, Main.screenPosition.X + Main.screenWidth),
                        Main.screenPosition.Y + Main.rand.NextFloat(0f, 160f));
                    Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, new Vector2(0f, Main.rand.NextFloat(2f, 5f)), 0,
                        DefinitivasLib.Ambar, 0.8f);
                    d.noGravity = true;
                }
            }
        }

        /// <summary>EL JUICIO sobre una presa: EL CANAL + LA COLUMNA + EL SONIDO.</summary>
        private void Golpear(NPC presa, bool final)
        {
            float grosor = final ? 40f : 22f;
            DefinitivasLib.Sonar(SoundID.Item122.WithPitchOffset(final ? -0.5f : -0.1f).WithVolumeScale(final ? 1.2f : 0.8f),
                presa.Center);
            if (final)
            {
                OndaLib.Flash(new Color(255, 244, 210), 0.32f, 12, presa.Center);
                OndaLib.Kick(13, 18);
            }
            else OndaLib.Kick(6, 8);

            // ==================== CAPA 5 — EL CANAL DOBLE ====================
            // (dos rayos entrelazados que CAEN — el cine de la casa)
            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
            {
                RayoSistema.Lanzar(RayoParams.Tormenta, (uint)Main.rand.Next(1, int.MaxValue),
                    presa.Center, null, DefinitivasLib.Oro, DefinitivasLib.BlancoCalido,
                    grosor, final ? 40 : 26, AnimRayo.VenaTrueno, true, -1);
                if (final)
                    RayoSistema.Lanzar(RayoParams.Tormenta, (uint)Main.rand.Next(1, int.MaxValue),
                        presa.Center, null, DefinitivasLib.Ambar, Color.White,
                        grosor * 0.6f, 40, AnimRayo.VenaTrueno, false, -1);
            }

            // ==================== CAPA 6 — LA COLUMNA DE IMPACTO ====================
            // (el daño real: la columna con sus tres fundas, la nova y el anillo)
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), presa.Center, Vector2.Zero,
                    ModContent.ProjectileType<ColumnaIraProjectile>(),
                    Projectile.damage * (final ? 2 : 1), Projectile.knockBack, Projectile.owner, final ? 2f : 0f);

            // ==================== CAPA 7 — LAS RAMAS ====================
            // (dos arcos a los vecinos con su daño secundario)
            if (!final)
            {
                NPC[] vecinos = BuscarVecinos(presa.Center, 2);
                foreach (NPC v in vecinos)
                {
                    if (v == null) continue;
                    if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                    {
                        RayoSistema.Lanzar(RayoParams.Arco(presa.Center, v.Center),
                            (uint)Main.rand.Next(1, int.MaxValue), v.Center, presa.Center - v.Center,
                            DefinitivasLib.Oro, DefinitivasLib.BlancoCalido, 8f, 14, AnimRayo.Arco, false);
                        // LAS CHISPAS en el destino de la rama
                        for (int i = 0; i < 4; i++)
                        {
                            Dust d = Dust.NewDustPerfect(v.Center, DustID.GoldFlame,
                                new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-5f, 1f)), 0,
                                DefinitivasLib.GradienteJuicio(Main.rand.NextFloat(1f)), 0.9f);
                            d.noGravity = true;
                        }
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), v.Center, Vector2.Zero,
                            ModContent.ProjectileType<ColumnaIraProjectile>(),
                            (int)(Projectile.damage * 0.45f), Projectile.knockBack * 0.4f, Projectile.owner, 1f);
                }
            }
        }

        /// <summary>La presa más cercana aún SIN juzgar (los ya golpeados esperan su ronda).</summary>
        private NPC BuscarNoGolpeado(Vector2 desde)
        {
            NPC mejor = null;
            float mejorD = 1100f * 1100f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.life <= 0 || n.dontTakeDamage) continue;
                if (n.friendly || n.townNPC) continue;
                if (n.immune[255] > 0) continue;                     // ya comió rayo esta ronda
                float d = Vector2.DistanceSquared(n.Center, desde);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        /// <summary>Los DOS vecinos más cercanos a un punto (para las ramas).</summary>
        private static NPC[] BuscarVecinos(Vector2 centro, int cuantos)
        {
            var hallados = new NPC[cuantos];
            float[] dists = new float[cuantos];
            for (int i = 0; i < cuantos; i++) dists[i] = 520f * 520f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.life <= 0 || n.dontTakeDamage) continue;
                if (n.friendly || n.townNPC) continue;
                float d = Vector2.DistanceSquared(n.Center, centro);
                for (int k = 0; k < cuantos; k++)
                {
                    if (d < dists[k])
                    {
                        for (int j = cuantos - 1; j > k; j--) { dists[j] = dists[j - 1]; hallados[j] = hallados[j - 1]; }
                        dists[k] = d; hallados[k] = n;
                        break;
                    }
                }
            }
            return hallados;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;

            // ==================== CAPA 1 — EL CIELO CUBIERTO ====================
            // (la banda superior se oscurece con NUBES VIVAS que se arrastran +
            // el velo de los bordes — la tormenta se SIENTE arriba)
            if (t < TTormenta)
            {
                float intensidad = MathHelper.Clamp(t / 30f, 0f, 1f) *
                    MathHelper.Clamp((TTormenta - t) / 30f, 0f, 1f);
                float w = Main.screenWidth, h = Main.screenHeight;
                Vector2 o = Main.screenPosition;

                VFXCore.Begin();
                Color nube = DefinitivasLib.Alfa(new Color(38, 24, 8), 0.55f * intensidad);
                // EL TECHO: dos capas de bruma que se arrastran en sentidos opuestos
                for (int i = 0; i < 6; i++)
                {
                    float xN = o.X + ((tiempo * (14f + i * 5f) + i * w * 0.31f) % (w * 1.5f)) - w * 0.25f;
                    Vector2 posN = new(xN, o.Y + h * 0.05f + (i % 2) * 26f);
                    VFXCore.Quad(posN, nube, new Vector2(w * 0.75f, 300f + i * 24f), 0f, VFXCore.SoftGlow);
                }
                VFXCore.FlushAlpha();

                SombrasLib.Vignette(0.10f + 0.16f * intensidad);
            }

            // v6.54.5/.6 — EL CONTRATO DEL LOTE: TODO PreDraw sale con el lote VIVO.
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }

    /// <summary>
    /// COLUMNAIRAPROJECTILE — LA COLUMNA del juicio: el daño real de cada
    /// rayo (110 px de radio, ~22 t, golpe único). v6.54.6 — LA COLUMNA
    /// REFORMADA: ahora con TRES FUNDAS + LA VENA + los halos apilados +
    /// LA NOVA + EL ANILLO de choque horizontal; el modo final (ai[0]=1)
    /// añade LA ESPIRAL — dos venas helicoidales alrededor del eje.
    /// ai[0] = 1 si es rama (fina) · 2 si es LA COLUMNA FINAL (espiral).
    /// </summary>
    public class ColumnaIraProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 110;
            Projectile.height = 110;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 22;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            bool rama = Projectile.ai[0] >= 1f && Projectile.ai[0] < 2f;
            bool final = Projectile.ai[0] >= 2f;
            float vida = 1f - Projectile.timeLeft / 22f;
            float potencia = (1f - vida) * (rama ? 0.7f : final ? 1.4f : 1f);

            // ==================== LA COLUMNA (tres fundas + LA VENA) ====================
            Vector2 arriba = Projectile.Center + new Vector2(0f, -1100f);
            Vector2 abajo = Projectile.Center + new Vector2(0f, 60f);
            const int N = 16;
            var pts = new Vector2[N];
            var ancho = new float[N];
            for (int i = 0; i < N; i++)
            {
                float q = i / (float)(N - 1);
                pts[i] = Vector2.Lerp(arriba, abajo, q) - Main.screenPosition;
                ancho[i] = MathHelper.Clamp(MathF.Sin(q * MathHelper.Pi) * 1.3f, 0.25f, 1f);
            }
            Color c = final ? DefinitivasLib.Ambar : DefinitivasLib.Oro;
            Color gBorde = DefinitivasLib.GradienteJuicio(Main.GlobalTimeWrappedHourly * 0.1f);

            RayoStrip.AbrirLote();
            try
            {
                // LA FUNDA EXTERNA — el halo del trueno
                RayoStrip.FilamentoEnLote(pts, (final ? 130f : rama ? 60f : 84f) * potencia, gBorde, c,
                    0.32f * potencia, StormTaper.Flat, 110, 0, false, false, false, null, ancho);
                // EL CUERPO del trueno
                RayoStrip.FilamentoEnLote(pts, (final ? 90f : rama ? 40f : 54f) * potencia, c, DefinitivasLib.BlancoCalido,
                    0.5f * potencia, StormTaper.Flat, 111, 0, false, false, false, null, ancho);
                // LA VENA — el núcleo blanco
                RayoStrip.FilamentoEnLote(pts, (final ? 34f : rama ? 16f : 20f) * potencia, DefinitivasLib.BlancoCalido, Color.White,
                    0.95f * potencia, StormTaper.Flat, 112, 0, false, false, true, null, ancho);

                // ==================== LA ESPIRAL DEL FINAL (dos venas helicoidales) ====================
                if (final)
                {
                    float tiempo = Main.GlobalTimeWrappedHourly;
                    for (int lado = 0; lado < 2; lado++)
                    {
                        var ptsS = new Vector2[N];
                        for (int i = 0; i < N; i++)
                        {
                            float q = i / (float)(N - 1);
                            float fase = q * MathHelper.TwoPi * 2.5f + tiempo * 6f + lado * MathHelper.Pi;
                            float radioS = (26f + 46f * MathF.Sin(q * MathHelper.Pi)) * (1f - vida * 0.5f);
                            ptsS[i] = Vector2.Lerp(arriba, abajo, q) +
                                new Vector2(MathF.Cos(fase), MathF.Sin(fase) * 0.25f) * radioS - Main.screenPosition;
                        }
                        Color cS = lado == 0 ? DefinitivasLib.Ambar : DefinitivasLib.Oro;
                        RayoStrip.FilamentoEnLote(ptsS, 22f * potencia, cS, DefinitivasLib.BlancoCalido,
                            0.75f * potencia, StormTaper.Flat, 113 + lado, 0, false, false, true, null, null);
                    }
                }
            }
            finally { RayoStrip.CerrarLote(); }

            // ==================== EL PIE DEL IMPACTO (halos + nova + destello) ====================
            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(gBorde, 0.20f * potencia), new Vector2(560f, 560f) * potencia);
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(c, 0.5f * potencia), new Vector2(300f, 300f) * potencia);
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.White, 0.4f * potencia), new Vector2(150f, 150f) * potencia);
            if (VFXCore.NovaBurst != null)
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.White, 0.8f * potencia),
                    new Vector2(340f, 340f) * potencia, vida * 1.4f, VFXCore.NovaBurst);
            if (VFXCore.DestelloFinal != null && final)
                VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.White, 0.85f * potencia),
                    new Vector2(520f, 520f) * potencia, vida, VFXCore.DestelloFinal);
            VFXCore.FlushAdditive();

            // ==================== EL ANILLO DE CHOQUE (el suelo estalla) ====================
            if (!rama && Main.spriteBatch != null && vida < 1f)
            {
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    OndaLib.Shock(Main.spriteBatch, Projectile.Center - Main.screenPosition, vida,
                        final ? 900f : 480f, c, 0.8f, 5, 14f, OndaFalloff.Quadratic, final);
                    Main.spriteBatch.End();
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();
            }

            // v6.54.5/.6 — EL CONTRATO DEL LOTE: TODO PreDraw sale con el lote VIVO.
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
