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
    /// IRADELCIELOPROJECTILE — v6.54.7 — ARMA 4 DE LAS CINCO: LA IRA DEL
    /// CIELO.
    ///
    /// LA TRANSCENDENCIA (la letra del usuario: «sobre esta arma la ira
    /// del cielo, ¿para qué fabricar esos óvalos planos como nubes si
    /// tenemos las librerías de brumas?»): los óvalos planos de la .54.6
    /// (quads de SoftGlow estirados) SE FUERON — el cielo ahora se cubre
    /// con LA LIBRERÍA DE BRUMAS DE VERDAD (BrumaFX: racimos de puffs
    /// con deriva orgánica por curl noise + bandas de niebla con
    /// paralaje que respiran), y las nubes SE ILUMINAN cuando cae un
    /// relámpago — más las técnicas nuevas de la investigación:
    ///
    ///   CAPA 1 — LA CÚPULA DE LA TORMENTA (LA LIBRERÍA DE BRUMAS):
    ///   SIETE racimos de nubes oscuras arrastrándose con el viento +
    ///   DOS bandas de niebla con paralaje — la tormenta OCULTA el cielo.
    ///   CAPA 2 — LOS RELÁMPAGOS QUE ILUMINAN LAS NUBES (nuevo): cada
    ///   relámpago ambiental ENCIENDE los racimos cercanos (puffs GLOW
    ///   dorados que respiran y mueren) + el micro-flash de pantalla.
    ///   CAPA 3 — LA BRUMA DORADA: la lluvia fina del juicio (heredada).
    ///   CAPA 4 — EL ANUNCIO CONVERGENTE (nuevo, técnica nº15): cada
    ///   juicio se telegrafia con la línea fina descendente + CHISPAS
    ///   QUE CONVERGEN AL BLANCO desde una esfera de 400 px — el que
    ///   sabe, ve DÓNDE va a caer.
    ///   CAPA 5 — EL CANAL DOBLE + EL AURA DE CARNE (nuevo): el rayo
    ///   entrelazado de la .54.6 + una faja TEXTURIZADA de ruido
    ///   fluyendo hacia abajo envolviendo la caída.
    ///   CAPA 6 — LA COLUMNA DE CARNE: la columna de impacto con LA PILA
    ///   DE SEIS PASADAS (aura · cuerpo con el color que viaja · remolino
    ///   hirviendo hacia arriba · núcleo en banda · vena blanca ·
    ///   aberración cromática) + los halos + la nova + el anillo de
    ///   choque + EL HUMO DEL JUICIO (nuevo: BrumaFX.Column — columnas
    ///   de humo oscuro subiendo del punto de impacto) + EL RESPLANDOR
    ///   DEL SUELO que se queda ardiendo.
    ///   CAPA 7 — LAS RAMAS Y EL REMATE: los arcos a los vecinos + LA
    ///   COLUMNA FINAL: TRES venas en espiral (la tercera exterior,
    ///   nueva) + flash + el Kick mayor + la llovizna de brasas.
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
        private int _semillaNube;      // la firma de ESTA tormenta

        // EL RELÁMPAGO QUE ILUMINA: dónde cayó el último ambiental (pantalla) y su edad
        private Vector2 _xRelampago;
        private int _tRelampago = 99;

        // EL AURA DE CARNE de la última caída: dónde cayó el último juicio y su edad
        private Vector2 _zonaCaida;
        private float _zonaGrosor;
        private int _tZona = 99;

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;
            if (_semillaNube == 0) _semillaNube = (int)Main.GameUpdateCount + 1000;

            // v6.54.6 — el failsafe global de vida máxima.
            if (t > 1200f) { Projectile.Kill(); return; }

            if (t < 2f) Projectile.Center = dueño.MountedCenter;
            if (_tRelampago < 99) _tRelampago++;
            if (_tZona < 99) _tZona++;

            // ==================== CAPA 2 — LOS RELÁMPAGOS AMBIENTALES QUE ILUMINAN ====================
            // (micro-rayos decorativos en el cielo cubierto — y cada uno
            // ENCIENDE las nubes cercanas: la tormenta vive y ALUMBRA)
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
                // EL REGISTRO DEL RELÁMPAGO (para iluminar las nubes en el draw)
                _xRelampago = arriba;
                _tRelampago = 0;
            }

            // ==================== EL ANUNCIO + LOS JUICIOS ====================
            // (cada juicio se TELEGRAFIA TAnuncio ticks antes: la línea fina
            // desciende sobre la presa + LAS CHISPAS CONVERGEN AL BLANCO — y
            // LUEGO cae el trueno)
            if (t > 10 + TAnuncio && (t - 10f - TAnuncio) % Cadencia == 0 && _golpeados < NJuicios)
            {
                NPC presa = BuscarNoGolpeado(dueño.Center);
                if (presa != null)
                {
                    _golpeados++;
                    bool final = _golpeados == NJuicios;

                    // === CAPA 4 — EL ANUNCIO (la línea + la convergencia al blanco) ===
                    if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                    {
                        Vector2 cumbre = new(presa.Center.X + Main.rand.NextFloat(-30f, 30f), presa.Center.Y - 1300f);
                        Vector2 pie = new(presa.Center.X + Main.rand.NextFloat(-14f, 14f), presa.Center.Y - 30f);
                        RayoSistema.Lanzar(RayoParams.Arco(cumbre, pie),
                            (uint)Main.rand.Next(1, int.MaxValue), pie, cumbre - pie,
                            DefinitivasLib.Oro, DefinitivasLib.BlancoCalido, 3.5f, TAnuncio, AnimRayo.Arco, false);

                        // LAS CHISPAS CONVERGENTES (técnica nº15): la esfera de
                        // 400 px que se CIERRA sobre la presa durante el anuncio
                        for (int i = 0; i < 10; i++)
                        {
                            float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                            float radio = 380f;
                            Vector2 pos = presa.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                            Vector2 vel = (presa.Center - pos).SafeNormalize(Vector2.UnitX) * (radio / TAnuncio);
                            Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, vel, 0,
                                DefinitivasLib.Oro, 0.9f);
                            d.noGravity = true;
                        }
                    }

                    // EL JUICIO cae TAnuncio ticks después del anuncio
                    Golpear(presa, final);
                }
                else _golpeados++;      // no hay nadie: el juicio pasa igual (el conteo avanza)
            }

            // ==================== CAPA 3 — LA BRUMA DORADA (la lluvia fina) ====================
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

                // EL REGISTRO DE LA ZONA CAÍDA (el aura de carne del draw)
                _zonaCaida = presa.Center;
                _zonaGrosor = grosor;
                _tZona = 0;

                // LAS BRASAS DEL JUICIO (final): la llovizna ígnea del remate
                if (final)
                    for (int i = 0; i < 14; i++)
                    {
                        Dust d = Dust.NewDustPerfect(presa.Center + new Vector2(Main.rand.NextFloat(-200f, 200f), Main.rand.NextFloat(-240f, -40f)),
                            DustID.FireworkFountain_Yellow,
                            new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(2f, 6f)), 0,
                            DefinitivasLib.Ambar, 1.0f);
                        d.noGravity = false;
                    }
            }

            // ==================== CAPA 6 — LA COLUMNA DE IMPACTO ====================
            // (el daño real: la columna con su pila completa, la nova y el anillo)
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

            // ==================== CAPA 1 — LA CÚPULA DE LA TORMENTA ====================
            // (LA LIBRERÍA DE BRUMAS DE VERDAD — la letra: «¿para qué
            // fabricar esos óvalos planos como nubes si tenemos las librerías
            // de brumas?»: racimos de puffs con deriva orgánica + bandas de
            // niebla con paralaje — el cielo se OCULTA de verdad)
            if (t < TTormenta)
            {
                float intensidad = MathHelper.Clamp(t / 30f, 0f, 1f) *
                    MathHelper.Clamp((TTormenta - t) / 30f, 0f, 1f);
                float w = Main.screenWidth;

                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    // LA MASA: nubes oscuras que OCULTAN el cielo (AlphaBlend)
                    BrumaFX.BeginMass();
                    Color nubeFondo = new(30, 20, 10);        // umber profundo
                    Color nubeFrente = new(52, 34, 14);       // umber con oro
                    for (int i = 0; i < 7; i++)
                    {
                        float xN = ((tiempo * (9f + i * 4f) + i * w * 0.37f) % (w * 1.4f)) - w * 0.2f;
                        Vector2 posN = new(xN, 34f + (i % 3) * 42f + MathF.Sin(tiempo * 0.5f + i) * 9f);
                        BrumaFX.Cloud(posN, 150f + i * 24f, i % 2 == 0 ? nubeFondo : nubeFrente,
                            _semillaNube + i * 13, tiempo * 0.22f + i * 0.7f,
                            puffs: 6, alpha: 0.50f * intensidad);
                    }
                    // LAS BANDAS DE NIEBLA con paralaje (el fondo de la cúpula)
                    var area = new Rectangle(-100, -70, (int)w + 200, 320);
                    BrumaFX.MistBand(area, new Color(40, 26, 12), _semillaNube + 91, tiempo, 22f, 0, 0.16f * intensidad);
                    BrumaFX.MistBand(area, new Color(46, 30, 14), _semillaNube + 92, tiempo, 13f, 1, 0.13f * intensidad);
                    Main.spriteBatch.End();

                    // ==================== CAPA 2 — LAS NUBES ILUMINADAS ====================
                    // (el relámpago ambiental ENCIENDE los racimos cercanos:
                    // puffs GLOW dorados que respiran y mueren en 10 t)
                    if (_tRelampago < 10)
                    {
                        float luz = 1f - _tRelampago / 10f;
                        BrumaFX.BeginGlow();
                        Vector2 posP = _xRelampago - Main.screenPosition;
                        BrumaFX.Cloud(posP, 150f + 60f * luz, new Color(255, 214, 130),
                            _semillaNube + 71, tiempo * 0.6f, puffs: 4, alpha: 0.40f * luz);
                        BrumaFX.Puff(posP + new Vector2(60f, 20f), 90f, new Color(255, 236, 180),
                            _semillaNube + 72, tiempo * 0.8f, 0.30f * luz);
                        Main.spriteBatch.End();
                    }
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();

                SombrasLib.Vignette(0.10f + 0.16f * intensidad);
            }

            // ==================== CAPA 5b — EL AURA DE CARNE DE LA CAÍDA ====================
            // (la faja de ruido fluyendo hacia abajo que envuelve la caída del
            // juicio durante los 12 t posteriores — el aire todavía quebrado)
            if (_tZona < 12)
            {
                Texture2D carne = DefinitivasLib.TexRuido;
                if (carne != null)
                {
                    float q = 1f - _tZona / 12f;
                    Vector2 cumbre = _zonaCaida + new Vector2(0f, -900f);
                    Vector2 pie = _zonaCaida;
                    const int N = 8;
                    var pts = new Vector2[N];
                    for (int i = 0; i < N; i++)
                    {
                        float u = i / (float)(N - 1);
                        pts[i] = Vector2.Lerp(cumbre, pie, u) +
                            new Vector2(MathF.Sin(u * 6f + tiempo * 22f) * 10f * (1f - u), 0f) - Main.screenPosition;
                    }
                    var semis = new float[N];
                    for (int i = 0; i < N; i++) semis[i] = _zonaGrosor * (0.9f + 0.6f * (i / (float)(N - 1))) * q;
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FajaTexturadaEnLote(pts, semis,
                            DefinitivasLib.Alfa(DefinitivasLib.Ambar, 0.30f * q), carne,
                            260f, 1.6f, tiempo, 0f, 1f);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }
            }

            // v6.54.5/.6 — EL CONTRATO DEL LOTE: TODO PreDraw sale con el lote VIVO.
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }

    /// <summary>
    /// COLUMNAIRAPROJECTILE — LA COLUMNA del juicio: el daño real de cada
    /// rayo (110 px de radio, ~22 t, golpe único). v6.54.7 — LA COLUMNA DE
    /// CARNE: la pila de seis pasadas texturizadas con el flujo INVERTIDO
    /// (la carne hierve hacia arriba) + LA TERCERA ESPIRAL exterior +
    /// EL HUMO DEL JUICIO (BrumaFX.Column: columnas de humo oscuro que
    /// suben del punto de impacto) + EL RESPLANDOR DEL SUELO que se queda
    /// ardiendo mientras la columna vive.
    /// ai[0] = 1 si es rama (fina) · 2 si es LA COLUMNA FINAL (espiral triple).
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

            Vector2 arriba = Projectile.Center + new Vector2(0f, -1100f);
            Vector2 abajo = Projectile.Center + new Vector2(0f, 60f);
            Color c = final ? DefinitivasLib.Ambar : DefinitivasLib.Oro;
            Color gBorde = DefinitivasLib.GradienteJuicio(Main.GlobalTimeWrappedHourly * 0.1f);

            // ==================== LA COLUMNA DE CARNE (LA PILA DE SEIS PASADAS) ====================
            // (flujo NEGATIVO: la carne HIERVE hacia arriba — el calor del
            // juicio sube; el remolino y el núcleo corren en contra)
            {
                const int N = 16;
                var espina = new Vector2[N];
                var perfil = new float[N];
                float flicker = DefinitivasLib.Flicker(Main.GlobalTimeWrappedHourly);
                for (int i = 0; i < N; i++)
                {
                    float q = i / (float)(N - 1);
                    espina[i] = Vector2.Lerp(arriba, abajo, q);
                    perfil[i] = MathHelper.Clamp(MathF.Sin(q * MathHelper.Pi) * 1.3f, 0.25f, 1f) *
                        (rama ? 40f : final ? 72f : 52f) * potencia * flicker;
                }
                DefinitivasLib.HazFusionEspina(espina, perfil, c, potencia,
                    Main.GlobalTimeWrappedHourly, 110, true, -1.2f, !rama);
            }

            // ==================== LA ESPIRAL TRIPLE DEL FINAL ====================
            // (dos venas helicoidales + LA TERCERA EXTERIOR nueva — el remate
            // es un taladro de luz)
            if (final)
            {
                const int N = 16;
                float tiempo = Main.GlobalTimeWrappedHourly;
                for (int lado = 0; lado < 3; lado++)
                {
                    var ptsS = new Vector2[N];
                    for (int i = 0; i < N; i++)
                    {
                        float q = i / (float)(N - 1);
                        float fase = q * MathHelper.TwoPi * 2.5f + tiempo * 6f + lado * (MathHelper.TwoPi / 3f);
                        float radioS = (26f + 46f * MathF.Sin(q * MathHelper.Pi)) * (1f - vida * 0.5f) +
                            (lado == 2 ? 34f : 0f);
                        ptsS[i] = Vector2.Lerp(arriba, abajo, q) +
                            new Vector2(MathF.Cos(fase), MathF.Sin(fase) * 0.25f) * radioS - Main.screenPosition;
                    }
                    Color cS = lado == 0 ? DefinitivasLib.Ambar : lado == 1 ? DefinitivasLib.Oro : DefinitivasLib.BlancoCalido;
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FilamentoEnLote(ptsS, (lado == 2 ? 15f : 22f) * potencia, cS, DefinitivasLib.BlancoCalido,
                            (lado == 2 ? 0.6f : 0.75f) * potencia, StormTaper.Flat, 113 + lado, 0, false, false, true, null, null);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }
            }

            // ==================== EL PIE DEL IMPACTO (halos + nova + destello + difracción) ====================
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
            // LOS PINCHOS DE DIFRACCIÓN horizontales en la base
            DefinitivasLib.PinchosDifraccion(Projectile.Center, 0f, (final ? 700f : 420f) * potencia,
                DefinitivasLib.BlancoCalido, 0.45f * potencia, Main.GlobalTimeWrappedHourly);
            VFXCore.FlushAdditive();

            // ==================== EL HUMO DEL JUICIO (LA LIBRERÍA DE BRUMAS) ====================
            // (columnas de humo oscuro subiendo del punto de impacto mientras
            // la columna arde — el mundo se QUEDA quemado un instante)
            {
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    BrumaFX.BeginMass();
                    Vector2 baseP = Projectile.Center - Main.screenPosition;
                    BrumaFX.Column(baseP, 240f * potencia, 90f, new Color(36, 24, 12),
                        311, Main.GlobalTimeWrappedHourly, 0.30f * potencia, 0.6f);
                    Main.spriteBatch.End();
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();
            }

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
