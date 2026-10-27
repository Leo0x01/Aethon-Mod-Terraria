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
    /// IRADEL CIELOPROJECTILE — v6.50.66 — ARMA 11: LA IRA DEL CIELO.
    ///
    /// La receta del «BurstLightning»/«GooLightning» de el dios antiguo sobre el
    /// filamento de rayos de la casa (RayoSistema + RayoStrip — «así son
    /// los rayos de verdad»): LA TORMENTA dura 150 t —
    ///
    ///   · EL CIELO SE CUBRE: el velo de los bordes + las nubes de bruma
    ///     dorada arriba.
    ///   · LOS OCHO JUICIOS: cada 17 t un rayo CAE del cielo sobre un
    ///     enemigo distinto (el más cercano aún no golpeado): el CINE
    ///     (RayoParams.Tormenta con animación VenaTrueno — el canal CAE
    ///     telegrafiado) + EL DAÑO (la columna real que golpea en 100 px).
    ///   · LAS RAMAS: de cada impacto saltan DOS arcos chispeantes a los
    ///     vecinos (el Arco de ArcSurge de la casa) — el daño secundario.
    ///   · LA COLUMNA FINAL: el enemigo más GRANDE recibe el remate: un
    ///     rayo DOBLE (dos canales entrelazados) + Flash + el Kick mayor.
    /// </summary>
    public class IraDelCieloProjectile : ModProjectile
    {
        private const int TTormenta = 150;
        private const int Cadencia = 17;
        private const int NJuicios = 8;

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

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;

            if (t < 2f) Projectile.Center = dueño.MountedCenter;

            // ==================== LOS JUICIOS ====================
            if (t > 10 && (t - 10f) % Cadencia == 0 && _golpeados < NJuicios)
            {
                NPC presa = BuscarNoGolpeado(dueño.Center);
                if (presa != null)
                {
                    _golpeados++;
                    bool final = _golpeados == NJuicios;
                    Golpear(presa, final);

                    // LAS RAMAS: dos arcos a los vecinos (CINE, daño menor vía mini-columnas)
                    if (!final)
                    {
                        NPC[] vecinos = BuscarVecinos(presa.Center, 2);
                        foreach (NPC v in vecinos)
                        {
                            if (v == null) continue;
                            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                                RayoSistema.Lanzar(RayoParams.Arco(presa.Center, v.Center),
                                    (uint)Main.rand.Next(1, int.MaxValue), v.Center, presa.Center - v.Center,
                                    DefinitivasLib.Oro, DefinitivasLib.BlancoCalido, 8f, 14, AnimRayo.Arco, false);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(), v.Center, Vector2.Zero,
                                    ModContent.ProjectileType<ColumnaIraProjectile>(),
                                    (int)(Projectile.damage * 0.45f), Projectile.knockBack * 0.4f, Projectile.owner, 1f);
                        }
                    }
                }
                else _golpeados++;      // no hay nadie: el juicio pasa igual (el conteo avanza)
            }

            // LA ATMÓSFERA de la tormenta (bruma dorada cayendo + el velo)
            if (!Main.dedServ && Main.netMode != NetmodeID.Server && t < TTormenta)
            {
                if (Main.rand.NextBool(3))
                {
                    Vector2 pos = new(Main.rand.NextFloat(Main.screenPosition.X, Main.screenPosition.X + Main.screenWidth),
                        Main.screenPosition.Y + Main.rand.NextFloat(0f, 140f));
                    Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, new Vector2(0f, Main.rand.NextFloat(2f, 5f)), 0,
                        DefinitivasLib.Ambar, 0.8f);
                    d.noGravity = true;
                }
            }
        }

        /// <summary>EL JUICIO sobre una presa: el CINE + EL DAÑO + EL SONIDO.</summary>
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

            // EL CINE: el canal que CAE (dos entrelazados si es el final)
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

            // EL DAÑO: la columna real
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), presa.Center, Vector2.Zero,
                    ModContent.ProjectileType<ColumnaIraProjectile>(),
                    Projectile.damage * (final ? 2 : 1), Projectile.knockBack, Projectile.owner, final ? 1f : 0f);
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

            // EL VELO DE LA TORMENTA (más denso mientras caen los juicios)
            if (t < TTormenta)
            {
                float intensidad = 0.10f + 0.16f * MathHelper.Clamp(t / 30f, 0f, 1f) *
                    MathHelper.Clamp((TTormenta - t) / 30f, 0f, 1f);
                SombrasLib.Vignette(intensidad);
            }
            // v6.54.5 — el contrato del lote: Vignette deja el lote CERRADO.
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }

    /// <summary>
    /// COLUMNAIRAPROJECTILE — LA COLUMNA del juicio: el daño real de cada
    /// rayo (110 px de radio, ~22 t, golpe único) con su pilar de luz.
    /// ai[0] = 1 si es LA COLUMNA FINAL (más gorda y más larga vida).
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
            bool final = Projectile.ai[0] >= 1f;
            float vida = 1f - Projectile.timeLeft / 22f;
            float potencia = (1f - vida) * (final ? 1.4f : 1f);

            // LA COLUMNA (la receta del partidor, escala de juicio)
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
            RayoStrip.AbrirLote();
            try
            {
                RayoStrip.FilamentoEnLote(pts, (final ? 90f : 54f) * potencia, c, DefinitivasLib.BlancoCalido,
                    0.5f * potencia, StormTaper.Flat, 110, 0, false, false, false, null, ancho);
                RayoStrip.FilamentoEnLote(pts, (final ? 34f : 20f) * potencia, DefinitivasLib.BlancoCalido, Color.White,
                    0.95f * potencia, StormTaper.Flat, 111, 0, false, false, true, null, ancho);
            }
            finally { RayoStrip.CerrarLote(); }

            // EL PIE del impacto
            VFXCore.Begin();
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(c, 0.5f * potencia), new Vector2(300f, 300f));
            if (VFXCore.DestelloFinal != null)
                DefinitivasLib.Blit(Main.spriteBatch, VFXCore.DestelloFinal, Projectile.Center - Main.screenPosition,
                    DefinitivasLib.Alfa(Color.White, 0.8f * potencia), new Vector2(340f, 340f), vida * 1.4f);
            VFXCore.FlushAdditive();
            // v6.54.5 — el contrato del lote: el Flush dejaba el lote CERRADO.
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
