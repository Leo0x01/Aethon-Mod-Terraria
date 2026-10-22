using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Weapons.Cosmic;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.Projectiles.Cosmic
{
    // ======================================================================
    //  v6.51.4 → v6.51.5 — LOS PROYECTILES DIGNOS, LA SEGUNDA AUDICIÓN.
    //
    //  LA LETRA DE LA .51.5: «sobre El Equinoccio Binario y las otras
    //  armas, bueno, están feas no me gustan y no tienen estilo, ni
    //  efectos, ni brillos, ni glow, ni nada, las puedes borrar, en
    //  cambio deja el El Equinoccio Binario y modificalo, para El
    //  Equinoccio Binario usa el mismo proyectil que El Equinoccio,
    //  pero dale el efecto que El Equinoccio Binario // y crea otras
    //  armas en consecuencia que creas que se sean buenas ideas,
    //  recuerda meterle tantos efectos chulos como puedas, tienes
    //  todas las librerias a tu disposicion».
    //
    //  LA PURGA: LAS COMAS QUE MUERDEN, LA PÁGINA ARRANCADA, EL OJO
    //  QUE CAZA, LA LÍNEA DE LECTURA y EL MARCAPÁGINAS murieron (sus
    //  cinco proyectiles y sus cinco iconos). EL EQUINOCCIO BINARIO
    //  vive RE-HECHO: dispara la MISMA PAREJA 931 de El Equinoccio
    //  (el dorado con su sol · el violeta con su eclipse — EmperatrizFX
    //  con las marcas binarias 9211/9212) — de día el dorado estalla
    //  en MINI ECLIPSE en área; de noche el violeta PERFORA 4 y DRENA
    //  vida (ver DignosWeapons + EmperatrizFX).
    //
    //  LAS TRES IDEAS NUEVAS (con TODAS las librerías de la casa):
    //    · EL ÍNDICE DEL GRIMORIO — el dedo de hueso que planta LA
    //      CITA: una línea de glifos… y la cita DETONA en VALLA de
    //      rayos de Perlin.
    //    · LA LLUVIA DE VERSOS — la grieta de bruma con costura
    //      dorada que llueve versos… y al cerrarse, EL CAPÍTULO SE
    //      CIERRA: las dos tapas y los dientes de garra.
    //    · EL OJO QUE JUZGA — el ojo colosal con velo de cine que
    //      mira… y su VEREDICTO: el rayo de la mirada.
    //    · EL CORAZÓN DEL MAGNETAR (la mezcla pedida: «una mezcla de
    //      Bastón del magnetar y el colapso del magnetar»): primero
    //      vive LA ÓRBITA del Bastón (aura + cadenas de rayo) y al
    //      soltar vive EL COLAPSO del padre (lanzamiento exacto, freno
    //      tembloroso, STARQUAKE). Los DOS PADRES quedan INTACTOS.
    //
    //  Convención de la casa: el daño manual de área va por GOLPEMOTOR
    //  (el cauce del motor, no SimpleStrikeNPC), el homing busca con
    //  VFXCore.EsObjetivo, y TODO PreDraw firma el contrato del lote
    //  (CerrarLoteSiAbierto → Begin → dibujar → FlushAdditive →
    //  ReabrirLoteVanilla, coords de MUNDO, try/catch de contención).
    // ======================================================================

    /// <summary>El puente común de los dignos: la presa más cercana y
    /// la paleta de la familia.</summary>
    internal static class DignosGrimorio
    {
        /// <summary>El ORO de la binaria (el disco del día).</summary>
        internal static readonly Color OroBinaria = new Color(255, 214, 120);

        /// <summary>El VIOLETA de la binaria (el disco de la noche).</summary>
        internal static readonly Color VioletaBinaria = new Color(178, 108, 240);

        /// <summary>El ORO CÁLIDO de los glifos (la tinta viva del libro).</summary>
        internal static readonly Color OroGlifo = new Color(255, 232, 170);

        /// <summary>El HUESO de las agujas del índice (la punta que cita).</summary>
        internal static readonly Color HuesoCita = new Color(235, 226, 205);

        /// <summary>La tinta VIOLETA de la grieta (el interior del libro).</summary>
        internal static readonly Color TintaGrieta = new Color(64, 34, 98);

        /// <summary>La presa válida más cercana dentro del rango (null si no hay).</summary>
        internal static NPC PresaCercana(Vector2 centro, float rango)
        {
            NPC mejor = null;
            float mejorD = rango * rango;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !VFXCore.EsObjetivo(n)) continue;
                float d = Vector2.DistanceSquared(n.Center, centro);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }
    }

    // ======================================================================
    //  v6.51.5 — EL TEATRO DE LOS DIGNOS: un solo proyectil cosmético
    //  con los modos de fuego que quedan VIVOS (el patrón
    //  BrumaNacimientoFX: cada cable la suya, netImportant = false, sin
    //  daño). Los dignos lo invocan desde sus golpes.
    //
    //    ai[0] = MODO: 1 ONDA DE ECLIPSE (binario día) · 2 DRENO DE
    //    LUNA (binario noche) · 5 EL PARPADEO (el destello del
    //    veredicto) · 8 EL CIERRE DEL CAPÍTULO (la lluvia de versos:
    //    las dos tapas y los dientes de garra).
    //    ai[1] = fuerza visual (0,7..1,4).
    // ======================================================================
    public class DignosFXProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;      // teatro: nunca daña
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 22;
            Projectile.netImportant = false;  // cosmético: cada cable la suya
        }

        /// <summary>LA APARICIÓN: nace el teatro donde el golpe lo pidió
        /// (nada en el servidor dedicado — es puro dibujo).</summary>
        public static void Nacer(Projectile origen, Vector2 donde, int modo, float fuerza = 1f)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return;
            try
            {
                int vida = modo switch
                {
                    1 => 22,   // la onda del eclipse
                    2 => 16,   // el dreno de luna
                    5 => 12,   // el parpadeo / el veredicto
                    8 => 26,   // el cierre del capítulo
                    _ => 12,
                };
                Projectile fx = Projectile.NewProjectileDirect(
                    origen.GetSource_FromAI(), donde, Vector2.Zero,
                    ModContent.ProjectileType<DignosFXProjectile>(),
                    0, 0f, Main.myPlayer, modo, fuerza);
                fx.timeLeft = vida;
            }
            catch { }
        }

        public override void AI()
        {
            // LA LUZ DEL TEATRO (el parpadeo y el cierre encienden).
            if (Main.netMode == NetmodeID.Server) return;
            int modo = (int)Projectile.ai[0];
            if (modo == 5) Lighting.AddLight(Projectile.Center, 1.2f, 1.0f, 0.6f);
            else if (modo == 8) Lighting.AddLight(Projectile.Center, 0.5f, 0.36f, 0.16f);
            else if (modo == 1)
                Lighting.AddLight(Projectile.Center, 0.5f, 0.38f, 0.16f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                int modo = (int)Projectile.ai[0];
                float vidaMax = Math.Max(1f, Projectile.timeLeft + Edad());
                float fase = Edad() / vidaMax;                       // 0 → 1
                float apagon = 1f - fase;                            // se muere apagándose
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 centro = Projectile.Center;
                int semilla = Projectile.identity % 97 + 7;

                if (modo == 8)
                {
                    // ========================================================
                    //  8 — EL CIERRE DEL CAPÍTULO: LAS DOS TAPAS (la de
                    //  arriba cae · la de abajo sube a su encuentro) con
                    //  sus LOMOS dorados… y al encontrarse, LOS NUEVE
                    //  DIENTES DE GARRA (SombrasLib.Garra — la librería
                    //  de la casa) mordiendo el plano. El libro se cierra
                    //  de golpe y lo atrapado dentro ya está leído.
                    // ========================================================
                    float cierre = MathHelper.Clamp(Edad() / 5f, 0f, 1f);   // el portazo
                    float golpe = 1f - cierre;
                    float fuerza = MathHelper.Clamp(Projectile.ai[1], 0.7f, 1.4f);
                    float alto = 86f * fuerza;

                    Vector2 tapaSup = centro + new Vector2(0f, -golpe * alto - 6f);
                    Vector2 tapaInf = centro + new Vector2(0f, golpe * alto + 6f);

                    VFXCore.CerrarLoteSiAbierto();

                    // LAS DOS TAPAS (capa ALFA — el negro no vive en el
                    // aditivo): portada y contratapa con su lomo.
                    VFXCore.Begin();
                    try
                    {
                        VFXCore.Quad(tapaSup, new Color(30, 20, 46) * 0.94f,
                            new Vector2(96f * fuerza, alto), 0f, VFXCore.Pixel);
                        VFXCore.Quad(tapaSup + new Vector2(2f, 3f),
                            new Color(18, 12, 30) * 0.85f,
                            new Vector2(96f * fuerza, alto), 0f, VFXCore.Pixel);
                        VFXCore.Quad(tapaInf, new Color(30, 20, 46) * 0.94f,
                            new Vector2(96f * fuerza, alto), 0f, VFXCore.Pixel);
                        VFXCore.Quad(tapaInf + new Vector2(-2f, -3f),
                            new Color(18, 12, 30) * 0.85f,
                            new Vector2(96f * fuerza, alto), 0f, VFXCore.Pixel);
                    }
                    catch { VFXCore.CerrarLoteSiAbierto(); }
                    VFXCore.FlushAlpha();

                    VFXCore.Begin();
                    try
                    {
                        // LOS DOS LOMOS dorados (la costura del capítulo).
                        VFXCore.Line(tapaSup + new Vector2(0f, -alto * 0.5f),
                            tapaSup + new Vector2(0f, alto * 0.5f),
                            DignosGrimorio.OroBinaria * 0.85f, 4f);
                        VFXCore.Line(tapaInf + new Vector2(0f, -alto * 0.5f),
                            tapaInf + new Vector2(0f, alto * 0.5f),
                            DignosGrimorio.OroBinaria * 0.85f, 4f);

                        // EL ARO DEL PORTAZO (la onda blanca del cierre).
                        float radio = 26f + 74f * fase;
                        VFXCore.Quad(centro, Color.White * 0.55f * apagon,
                            VFXCore.RingQuadSize(radio), 0f, VFXCore.Ring);
                        VFXCore.Quad(centro, DignosGrimorio.OroBinaria * 0.28f * apagon,
                            new Vector2(140f * fuerza, 120f * fuerza));
                    }
                    catch { VFXCore.CerrarLoteSiAbierto(); }
                    VFXCore.FlushAdditive();

                    // LOS NUEVE DIENTES: garras de la casa mordiendo desde
                    // ambas tapas al cerrarse (5 arriba · 4 abajo — la
                    // dentadura asimétrica del libro).
                    try
                    {
                        for (int k = 0; k < 9; k++)
                        {
                            bool arriba = k < 5;
                            float f = (arriba ? k : k - 5) / (arriba ? 4f : 3f);
                            float x = (f - 0.5f) * 88f * fuerza;
                            Vector2 baseG = centro + new Vector2(
                                x, (arriba ? -1f : 1f) * (alto * 0.5f + 10f));
                            // LA MORIDA: los dientes CRECEN al cerrarse.
                            float largo = (34f + 26f * cierre) * fuerza;
                            SombrasLib.Garra(baseG,
                                (arriba ? Vector2.UnitY : -Vector2.UnitY) *
                                largo / 34f,
                                largo, 0.92f * apagon,
                                (k % 2 == 0 ? 0.34f : -0.34f));
                        }
                    }
                    catch { VFXCore.CerrarLoteSiAbierto(); }

                    VFXCore.ReabrirLoteVanilla();
                    return false;
                }

                // === EL RESTO DE MODOS: un solo lote aditivo. ===
                VFXCore.CerrarLoteSiAbierto();
                VFXCore.Begin();
                try
                {
                    switch (modo)
                    {
                        case 1:
                        {
                            // 1 — LA ONDA DEL ECLIPSE (día): el aro dorado que
                            //     se traga la luz alrededor del golpe.
                            float radio = 16f + 78f * fase;
                            VFXCore.Quad(centro, DignosGrimorio.OroBinaria * 0.5f * apagon,
                                VFXCore.RingQuadSize(radio), 0f, VFXCore.Ring);
                            VFXCore.Quad(centro, DignosGrimorio.OroBinaria * 0.4f * apagon,
                                new Vector2(64f, 64f));
                            VFXCore.Quad(centro, Color.White * 0.5f * apagon,
                                new Vector2(22f, 22f));
                            break;
                        }
                        case 2:
                        {
                            // 2 — EL DRENO DE LUNA (noche): tres volutas
                            //     violetas que suben del mordisco.
                            for (int k = 0; k < 3; k++)
                            {
                                Vector2 pos = centro + new Vector2(
                                    MathF.Sin(t * 3.1f + k * 2.2f) * 7f,
                                    -(Edad() * (1.1f + k * 0.35f)) - k * 4f);
                                VFXCore.Quad(pos, DignosGrimorio.VioletaBinaria * (0.45f * apagon),
                                    new Vector2(9f, 15f),
                                    MathF.Sin(t * 2.4f + k) * 0.5f, VFXCore.Pixel);
                            }
                            VFXCore.Quad(centro, DignosGrimorio.VioletaBinaria * 0.35f * apagon,
                                new Vector2(30f, 30f));
                            break;
                        }
                        default:
                        {
                            // 5 — EL PARPADEO / EL VEREDICTO: el destello
                            //     blanco gordo + el aro dorado en DOS QUADS
                            //     CRUZADOS alargados.
                            float destello = MathHelper.Clamp(1f - fase * 1.6f, 0f, 1f);
                            VFXCore.Quad(centro, Color.White * 0.85f * destello,
                                new Vector2(120f, 120f));
                            float brazo = 30f + 60f * fase;
                            Vector2 dirA = new Vector2(MathF.Cos(0.7f), MathF.Sin(0.7f));
                            Vector2 dirB = new Vector2(-dirA.Y, dirA.X);
                            VFXCore.Line(centro - dirA * brazo, centro + dirA * brazo,
                                DignosGrimorio.OroBinaria * 0.6f * apagon, 5f);
                            VFXCore.Line(centro - dirB * brazo, centro + dirB * brazo,
                                DignosGrimorio.OroBinaria * 0.6f * apagon, 5f);
                            VFXCore.Quad(centro, Color.White * 0.6f * apagon,
                                new Vector2(18f, 18f));
                            break;
                        }
                    }
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.FlushAdditive();
                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.ReabrirLoteVanilla(); }
            return false;
        }

        /// <summary>La edad del teatro (ticks vivos).</summary>
        private float Edad()
        {
            float vidaMax = (int)Projectile.ai[0] switch
            {
                1 => 22f, 2 => 16f, 5 => 12f, 8 => 26f, _ => 12f,
            };
            return MathHelper.Clamp(vidaMax - Projectile.timeLeft, 0f, vidaMax - 1f);
        }
    }

    // ======================================================================
    //  1 · EL ÍNDICE DEL GRIMORIO — el dedo que cita (v6.51.5)
    //
    //  LA IDEA (la letra: «recuerda meterle tantos efectos chulos como
    //  puedas, tienes todas las librerias a tu disposicion»): el ÍNDICE
    //  — el dedo de hueso del libro que SEÑALA la cita. Vuela rápido
    //  con homing tenue dejando una estela de glifos; al CLAVARSE en la
    //  presa (o al agotarse), planta LA CITA: una línea de glifos
    //  dorados que se ESCRIBE a lo largo de su rumbo… y cuando la cita
    //  está completa, DETONA: LA VALLA — CINCO rayos de Perlin caen en
    //  fila sobre la línea (uno cada 6 ticks, de izquierda a derecha —
    //  el trueno CAMINANDO la frase), cada uno golpeando en 70 px a su
    //  paso. El libro lee en voz muy alta.
    //
    //  ai[0] = estado: 0 vuelo · 1 LA CITA (clavado, escribiendo) ·
    //  2 LA VALLA (detonando). ai[1] = el ángulo del rumbo al clavarse.
    // ======================================================================
    public class IndiceGrimorioProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const int EscrituraTicks = 26;   // LA CITA se escribe
        private const int Vallaticks = 34;       // LA VALLA detona
        private const int RayosValla = 5;        // cinco truenos en fila
        private const float RadioRayo = 70f;     // el golpe de cada trueno
        private const float LargoLinea = 168f;   // el largo de la cita

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.light = 0.25f;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            float estado = Projectile.ai[0];

            if (estado == 0f)
            {
                // === EL VUELO DEL DEDO: rápido y certero (homing tenue). ===
                Projectile.rotation = Projectile.velocity.ToRotation();
                NPC presa = DignosGrimorio.PresaCercana(Projectile.Center, 700f);
                if (presa != null)
                {
                    Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                    Projectile.velocity += hacia * 0.22f;
                    float v = Projectile.velocity.Length();
                    if (v > 15f) Projectile.velocity = Projectile.velocity / v * 15f;
                }
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                        -Projectile.velocity * 0.08f, 150, DignosGrimorio.OroGlifo, 0.8f);
                    d.noGravity = true;
                }
                if (Projectile.timeLeft <= 1)
                    Clavar(Projectile.velocity.LengthSquared() > 1f
                        ? Projectile.velocity.ToRotation() : -MathHelper.PiOver2);
            }
            else if (estado == 1f)
            {
                // === LA CITA SE ESCRIBE (el dedo clavado, la línea
                //     creciendo — el silencio antes del trueno). ===
                Projectile.velocity = Vector2.Zero;
                if (Main.netMode != NetmodeID.Server)
                    Lighting.AddLight(Projectile.Center, 0.4f, 0.32f, 0.16f);
                if (Projectile.localAI[0] >= EscrituraTicks)
                {
                    Projectile.ai[0] = 2f;
                    Projectile.localAI[0] = 0f;
                    Projectile.netUpdate = true;
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(-0.35f), Projectile.Center);
                }
            }
            else if (estado == 2f)
            {
                // === LA VALLA DETONA: un rayo cada 6 ticks caminando la
                //     cita (de un extremo al otro de la línea). ===
                Projectile.velocity = Vector2.Zero;
                float prog = MathHelper.Clamp(Projectile.localAI[0] / (float)(RayosValla * 6), 0f, 1f);
                if (Main.netMode != NetmodeID.Server)
                    Lighting.AddLight(Projectile.Center, 0.55f + 0.3f * prog,
                        0.42f + 0.2f * prog, 0.20f);

                int rayoActual = (int)(Projectile.localAI[0] / 6f);
                if (rayoActual != (int)Projectile.localAI[1] && rayoActual < RayosValla)
                {
                    Projectile.localAI[1] = rayoActual;
                    GolpearRayo(rayoActual);
                }
                if (Projectile.localAI[0] >= RayosValla * 6 + 8)
                    Projectile.Kill();
            }

            Projectile.localAI[0] += 1f;
        }

        /// <summary>EL CLAVADO: el dedo planta la cita (guarda su rumbo
        /// — la línea se escribe por DONDE VENÍA).</summary>
        private void Clavar(float angulo)
        {
            Projectile.ai[0] = 1f;
            Projectile.ai[1] = angulo;
            Projectile.localAI[0] = 0f;
            Projectile.netUpdate = true;
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item10.WithPitchOffset(0.4f), Projectile.Center);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.ai[0] == 0f)
                Clavar(Projectile.velocity.LengthSquared() > 1f
                    ? Projectile.velocity.ToRotation() : -MathHelper.PiOver2);
        }

        /// <summary>LA POSICIÓN del rayo k-ésimo de la valla (sobre la
        /// línea de la cita, de extremo a extremo).</summary>
        private Vector2 PosRayo(int k)
        {
            float f = RayosValla <= 1 ? 0.5f : k / (float)(RayosValla - 1);
            float along = (f - 0.5f) * LargoLinea;
            return Projectile.Center + new Vector2(
                MathF.Cos(Projectile.ai[1]) * along,
                MathF.Sin(Projectile.ai[1]) * along);
        }

        /// <summary>EL TRUENO k-ésimo: el golpe de la valla (GolpeMotor
        /// — el cauce del motor) + el estruendo.</summary>
        private void GolpearRayo(int k)
        {
            Vector2 pos = PosRayo(k);
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item122.WithPitchOffset(-0.15f + 0.09f * k), pos);
            DignosFXProjectile.Nacer(Projectile, pos, 5, 0.8f);

            if (Projectile.owner == Main.myPlayer)
            {
                int dano = Math.Max(1, (int)(Projectile.damage * 0.62f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if (Vector2.DistanceSquared(npc.Center, pos) > RadioRayo * RadioRayo) continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, dano, 2f, true);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                int semilla = Projectile.identity % 71 + 5;
                float estado = Projectile.ai[0];

                // ============================================================
                //  LA GRIETA DE BRUMA bajo la cita (los estados 1-2: el
                //  papel se ABRE por donde el dedo clavó — BrumaFX).
                // ============================================================
                if (estado >= 1f)
                {
                    try
                    {
                        Vector2 centroScr = Projectile.Center - Main.screenPosition;
                        BrumaFX.BeginMass();
                        BrumaFX.Cloud(centroScr, 40f, DignosGrimorio.TintaGrieta,
                            semilla, t, 4, alpha: 0.42f);
                    }
                    catch { }
                    finally
                    {
                        try { Main.spriteBatch.End(); } catch { }
                        VFXCore.CerrarLoteSiAbierto();
                    }
                }

                VFXCore.Begin();
                try
                {
                    if (estado == 0f)
                    {
                        // === EL DEDO DE HUESO: la aguja alineada al rumbo
                        //     (la textura Colmillo de la casa) + su halo y
                        //     la estela de glifos del vuelo. ===
                        float rumbo = Projectile.rotation;
                        Vector2 dir = new Vector2(MathF.Cos(rumbo), MathF.Sin(rumbo));
                        VFXCore.Quad(Projectile.Center, DignosGrimorio.OroGlifo * 0.30f,
                            new Vector2(34f, 34f));
                        VFXCore.Quad(Projectile.Center, DignosGrimorio.HuesoCita,
                            new Vector2(26f, 9f), rumbo, VFXCore.Colmillo);
                        for (int e = 1; e <= 3; e++)
                        {
                            Vector2 posE = Projectile.Center - dir * (e * 15f);
                            VFXCore.Quad(posE, DignosGrimorio.OroGlifo * (0.35f / e),
                                new Vector2(7f / e, 7f / e),
                                rumbo + e * 0.8f, VFXCore.Pixel);
                        }
                    }
                    else
                    {
                        // === LA CITA: la línea de glifos ESCRIBIÉNDOSE a lo
                        //     largo del rumbo (cada glifo a su compás). ===
                        float escritura = MathHelper.Clamp(
                            Projectile.localAI[0] / (float)EscrituraTicks, 0f, 1f);
                        float valla = estado == 2f
                            ? MathHelper.Clamp(Projectile.localAI[0] / (float)(RayosValla * 6), 0f, 1f)
                            : 0f;
                        int glifos = 9;
                        int escritos = (int)(glifos * escritura);
                        for (int i = 0; i < glifos; i++)
                        {
                            if (i >= escritos) break;
                            float f = i / (float)(glifos - 1);
                            float along = (f - 0.5f) * LargoLinea;
                            Vector2 posG = Projectile.Center + new Vector2(
                                MathF.Cos(Projectile.ai[1]) * along,
                                MathF.Sin(Projectile.ai[1]) * along);
                            float late = 0.7f + 0.3f * MathF.Sin(t * 5f + i * 1.9f);
                            float tam = 6.5f + 2.5f * VFXCore.Hash01(semilla, i, (int)(t * 10f));
                            VFXCore.Quad(posG, DignosGrimorio.OroGlifo * (0.85f * late),
                                new Vector2(tam, tam),
                                VFXCore.Hash01(semilla, i + 5, (int)(t * 10f)) * MathHelper.PiOver2,
                                VFXCore.Pixel);
                        }
                        // LA COSTURA dorada de la línea (donde la cita vive).
                        VFXCore.Line(
                            Projectile.Center + new Vector2(
                                MathF.Cos(Projectile.ai[1]), MathF.Sin(Projectile.ai[1])) * (-LargoLinea * 0.5f),
                            Projectile.Center + new Vector2(
                                MathF.Cos(Projectile.ai[1]), MathF.Sin(Projectile.ai[1])) * (LargoLinea * 0.5f),
                            DignosGrimorio.OroBinaria * 0.22f, 2f);

                        // === LOS RAYOS DE PERLIN DE LA VALLA (el trueno
                        //     caminando la frase): cada trueno VIVE 12
                        //     ticks tras su golpe — dibujados con la
                        //     técnica del rayo de la casa (segmentos con
                        //     temblor determinista). ===
                        if (estado == 2f)
                        {
                            for (int k = 0; k < RayosValla; k++)
                            {
                                float nacio = k * 6f;
                                float edadR = Projectile.localAI[0] - nacio;
                                if (edadR < 0f || edadR > 12f) continue;
                                float alfaR = 1f - edadR / 12f;
                                Vector2 baseR = PosRayo(k);
                                DibujarRayoPerlin(baseR, alfaR, semilla + k * 13, t);
                            }
                        }
                    }
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.FlushAdditive();
                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.ReabrirLoteVanilla(); }
            return false;
        }

        /// <summary>EL RAYO DE PERLIN de la valla: del cielo a la línea
        /// (un arco segmentado con temblor determinista — la técnica de
        /// la casa: doble pase, núcleo blanco + cuerpo dorado).</summary>
        private static void DibujarRayoPerlin(Vector2 basePos, float alfa, int semilla, float t)
        {
            const int Segs = 7;
            float alto = 150f;
            Vector2 arriba = basePos - Vector2.UnitY * alto;
            for (int s = 0; s < Segs; s++)
            {
                float f0 = s / (float)Segs;
                float f1 = (s + 1) / (float)Segs;
                float w0 = (VFXCore.Hash01(semilla, s, (int)(t * 30f)) - 0.5f) * 26f * (1f - f0 * 0.7f);
                float w1 = (VFXCore.Hash01(semilla, s + 1, (int)(t * 30f)) - 0.5f) * 26f * (1f - f1 * 0.7f);
                Vector2 p0 = Vector2.Lerp(arriba, basePos, f0) + new Vector2(w0, 0f);
                Vector2 p1 = Vector2.Lerp(arriba, basePos, f1) + new Vector2(w1, 0f);
                // EL CUERPO dorado (grueso)…
                VFXCore.Line(p0, p1, DignosGrimorio.OroBinaria * (0.55f * alfa), 5.5f);
                // …y EL NÚCLEO blanco (lo que ciega).
                VFXCore.Line(p0, p1, Color.White * (0.80f * alfa), 2.2f);
            }
            // EL IMPACTO: el destello en la base.
            VFXCore.Quad(basePos, DignosGrimorio.OroGlifo * (0.6f * alfa),
                new Vector2(40f * alfa + 12f, 40f * alfa + 12f));
        }
    }

    // ======================================================================
    //  2 · LA LLUVIA DE VERSOS — la grieta que llora el capítulo
    //  (v6.51.5)
    //
    //  LA IDEA: LA GRIETA — un desgarro vertical de BRUMA (BrumaFX de
    //  la casa) con su COSTURA DORADA latiendo, que vuela hacia el
    //  cursor LLOVIENDO VERSOS: cada 9 ticks un verso CAE de la grieta
    //  y estalla donde toca (glifos dorados en área). Cuando la grieta
    //  MUERDE a una presa (o se agota su vuelo), EL CAPÍTULO SE CIERRA
    //  DE GOLPE: LAS DOS TAPAS (la portada cayendo · la contratapa
    //  subiendo a su encuentro) con sus lomos dorados… y NUEVE DIENTES
    //  DE GARRA (SombrasLib.Garra) mordiendo el plano al cerrarse — el
    //  golpe gordo del cierre, en área.
    //
    //  ai[0] = estado: 0 vuelo lloviendo · 1 cerrado (el teatro vive
    //  en DignosFXProjectile modo 8).
    // ======================================================================
    public class LluviaVersosProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const int CadenaVerso = 9;      // un verso cada 9 ticks
        private const float RadioVerso = 56f;   // el área de cada verso
        private const float RadioCierre = 96f;  // el área del portazo

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.light = 0.3f;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            if (Projectile.ai[0] != 0f) { Projectile.Kill(); return; }

            // === EL VUELO DE LA GRIETA: deriva suave hacia su presa. ===
            NPC presa = DignosGrimorio.PresaCercana(Projectile.Center, 800f);
            if (presa != null)
            {
                Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Projectile.velocity += hacia * 0.10f;
                float v = Projectile.velocity.Length();
                if (v > 9.5f) Projectile.velocity = Projectile.velocity / v * 9.5f;
            }
            Projectile.rotation = Projectile.velocity.X * 0.012f;

            if (Main.netMode != NetmodeID.Server)
                Lighting.AddLight(Projectile.Center, 0.35f, 0.26f, 0.10f);

            // === LA LLUVIA DE VERSOS: cada 9 ticks un verso CAE de la
            //     grieta y estalla bajo ella (el área del verso). ===
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] % CadenaVerso == 0f)
            {
                Vector2 posVerso = Projectile.Center + new Vector2(
                    Main.rand.NextFloat(-14f, 14f), Main.rand.NextFloat(34f, 64f));
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item8.WithPitchOffset(0.35f), posVerso);

                if (Projectile.owner == Main.myPlayer)
                {
                    int dano = Math.Max(1, (int)(Projectile.damage * 0.55f));
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!VFXCore.EsObjetivo(npc)) continue;
                        if (Vector2.DistanceSquared(npc.Center, posVerso) > RadioVerso * RadioVerso) continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, dano, 1.2f, true);
                    }
                }
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Dust d = Dust.NewDustPerfect(posVerso, DustID.Enchanted_Gold,
                            new Vector2(Main.rand.NextFloat(-1.6f, 1.6f),
                                        Main.rand.NextFloat(-2.2f, -0.4f)),
                            160, DignosGrimorio.OroGlifo, 1.0f);
                        d.noGravity = true;
                    }
                }
            }

            if (Projectile.timeLeft <= 1)
                CerrarCapitulo();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CerrarCapitulo();
        }

        /// <summary>EL CIERRE DEL CAPÍTULO: el portazo — las dos tapas y
        /// los dientes (el teatro, modo 8) + EL GOLPE gordo en área.</summary>
        private void CerrarCapitulo()
        {
            if (Projectile.ai[0] != 0f) return;
            Projectile.ai[0] = 1f;

            // EL TEATRO (las tapas + las garras — cada cable la suya).
            DignosFXProjectile.Nacer(Projectile, Projectile.Center, 8, 1f);
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item122.WithPitchOffset(-0.5f), Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item14.WithPitchOffset(0.3f), Projectile.Center);

            // EL GOLPE DEL PORTAZO (GolpeMotor — el cauce del motor).
            if (Projectile.owner == Main.myPlayer)
            {
                int dano = Math.Max(1, (int)(Projectile.damage * 1.5f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > RadioCierre * RadioCierre) continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, dano, 3f, true);
                }
            }
            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                int semilla = Projectile.identity % 83 + 9;

                // ============================================================
                //  LA GRIETA DE BRUMA (BrumaFX de la casa): el desgarro
                //  vertical del papel — una nube oscura alargada que
                //  RESPIRA, con su corazón de tinta más denso.
                // ============================================================
                Vector2 centroScr = Projectile.Center - Main.screenPosition;
                try
                {
                    BrumaFX.BeginMass();
                    BrumaFX.Cloud(centroScr, 34f, DignosGrimorio.TintaGrieta,
                        semilla, t, 4, alpha: 0.55f);
                    BrumaFX.Cloud(centroScr + new Vector2(0f, -8f), 22f,
                        new Color(88, 52, 128), semilla + 31, t * 1.15f, 3, alpha: 0.42f);
                }
                catch { }
                finally
                {
                    try { Main.spriteBatch.End(); } catch { }
                    VFXCore.CerrarLoteSiAbierto();
                }

                // ============================================================
                //  LA COSTURA DORADA + LOS VERSOS CAYENDO (el lote aditivo
                //  de la casa): la grieta late por dentro… y los glifos
                //  caen de ella hacia su cita con el suelo.
                // ============================================================
                VFXCore.Begin();
                try
                {
                    // LA COSTURA: la línea dorada vertical que late.
                    float late = 0.65f + 0.35f * MathF.Sin(t * 4.2f);
                    VFXCore.Line(Projectile.Center + new Vector2(0f, -30f),
                        Projectile.Center + new Vector2(0f, 30f),
                        DignosGrimorio.OroBinaria * (0.55f * late), 3.2f);
                    VFXCore.Line(Projectile.Center + new Vector2(0f, -18f),
                        Projectile.Center + new Vector2(0f, 18f),
                        Color.White * (0.35f * late), 1.4f);

                    // LOS VERSOS CAYENDO (deterministas — tres glifos en
                    // caída, cada uno a su compás).
                    for (int k = 0; k < 3; k++)
                    {
                        float ciclo = (t * 1.3f + k * 0.37f) % 1f;      // su reloj de caída
                        Vector2 posV = Projectile.Center + new Vector2(
                            MathF.Sin(ciclo * 6f + k * 2.1f) * 10f,
                            18f + ciclo * 52f);
                        float alfaV = (1f - ciclo) * 0.8f;
                        float tam = 5.5f + 2.5f * VFXCore.Hash01(semilla, k, (int)(t * 12f));
                        VFXCore.Quad(posV, DignosGrimorio.OroGlifo * alfaV,
                            new Vector2(tam, tam),
                            MathF.Sin(t * 3f + k) * 0.4f, VFXCore.Pixel);
                    }

                    // EL HALO de la grieta (la lectura nocturna).
                    VFXCore.Quad(Projectile.Center, DignosGrimorio.OroBinaria * 0.14f,
                        new Vector2(64f, 92f));
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.FlushAdditive();
                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.ReabrirLoteVanilla(); }
            return false;
        }
    }

    // ======================================================================
    //  3 · EL OJO QUE JUZGA — el veredicto de la mirada (v6.51.5)
    //
    //  LA IDEA: EL OJO COLOSAL — la mirada del escuadrón crecida a
    //  juez. Nace donde apuntas, ABRE EL PÁRPADO (Cerrado→Medio→
    //  Abierto — las tres texturas del OjoEterno) tras su VELO DE CINE
    //  (dos cortinas de bruma que se corren a los lados), y JUZGA: el
    //  iris se clava en la presa más cercana mientras el tono sube…
    //  y a los 75 ticks, EL VEREDICTO — EL RAYO DE LA MIRADA: un haz
    //  violeta de DOS NÚCLEOS del ojo a su presa que atraviesa TODO lo
    //  que está en la línea, con la bruma exhalando a lo largo del
    //  rayo. Después el ojo PARPADEA (el destello del veredicto) y se
    //  va.
    //
    //  ai[0] = estado: 0 juzgando · 1 veredicto disparado. localAI[1]
    //  = el ángulo de la mirada.
    // ======================================================================
    public class OjoQueJuzgaProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/OjoEterno_Abierto";

        private const int JuicioTicks = 75;     // el ojo juzga (el tono sube)
        private const int RayoTicks = 16;       // el veredicto vive
        private const float AlcanceJuicio = 1200f;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = false;        // el daño es EL RAYO (manual)
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = JuicioTicks + RayoTicks + 14;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;     // el juicio es de todos los cables
        }

        /// <summary>La presa JUZGADA (la más cercana — la que el ojo fijó).</summary>
        private NPC _juzgado;

        public override void AI()
        {
            Projectile.localAI[0] += 1f;
            float edad = Projectile.localAI[0];

            // === LA FLOTACIÓN (el juez se installa y cabecea, quieto). ===
            Projectile.velocity *= 0.90f;
            Projectile.velocity.Y += MathF.Sin(Main.GlobalTimeWrappedHourly * 1.4f
                + Projectile.identity) * 0.015f;

            // === LA MIRADA: el iris se clava en la presa más cercana. ===
            NPC presa = DignosGrimorio.PresaCercana(Projectile.Center, AlcanceJuicio);
            _juzgado = presa;
            if (presa != null)
                Projectile.localAI[1] = (presa.Center - Projectile.Center).ToRotation();

            // LA LUZ DEL JUEZ (fría, creciendo con el juicio — y CIEGA en
            // el veredicto).
            if (Main.netMode != NetmodeID.Server)
            {
                float prog = MathHelper.Clamp(edad / JuicioTicks, 0f, 1f);
                if (Projectile.ai[0] == 0f)
                    Lighting.AddLight(Projectile.Center,
                        0.30f + 0.30f * prog, 0.22f + 0.22f * prog, 0.42f + 0.40f * prog);
                else
                    Lighting.AddLight(Projectile.Center, 1.1f, 0.9f, 1.3f);
            }

            // === EL TONO QUE SUBE (el compás del juicio — acelera al
            //     final, como el ritual de la casa). ===
            if (Main.netMode != NetmodeID.Server && Projectile.ai[0] == 0f && edad < JuicioTicks)
            {
                float restante = JuicioTicks - edad;
                int intervalo = restante > 40f ? 16 : (restante > 20f ? 9 : 5);
                if (edad % intervalo == 0f)
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(-0.3f + 0.5f * (edad / JuicioTicks)),
                        Projectile.Center);
            }

            // === EL VEREDICTO (al cumplir el juicio — o si la presa se
            //     acerca DEMASIADO: el juez no espera a los groseros). ===
            bool cerca = presa != null &&
                Vector2.DistanceSquared(presa.Center, Projectile.Center) <= 190f * 190f;
            if (Projectile.ai[0] == 0f && (edad >= JuicioTicks || (edad > 30f && cerca)))
                DispararVeredicto();

            // (tras el veredicto: el ojo PARPADEA y muere)
            if (Projectile.ai[0] == 1f && edad >= JuicioTicks + RayoTicks + 10)
                Projectile.Kill();
        }

        /// <summary>EL VEREDICTO: EL RAYO DE LA MIRADA — atraviesa TODO lo
        /// que vive en la línea ojo→presa (o hacia su última mirada si la
        /// presa murió — un juez nunca se queda sin veredicto).</summary>
        private void DispararVeredicto()
        {
            Projectile.ai[0] = 1f;
            Projectile.netUpdate = true;

            Vector2 rumbo = new Vector2(MathF.Cos(Projectile.localAI[1]),
                                        MathF.Sin(Projectile.localAI[1]));
            Vector2 destino = _juzgado != null && _juzgado.active
                ? _juzgado.Center
                : Projectile.Center + rumbo * 900f;

            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item122.WithPitchOffset(-0.6f), Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item60.WithPitchOffset(0.2f), Projectile.Center);
            DignosFXProjectile.Nacer(Projectile, Projectile.Center, 5, 1.3f);

            // EL GOLPE DEL RAYO: todo lo que vive en la línea (distancia
            // perpendicular ≤ 42 px), UNA vez — GolpeMotor (el cauce).
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 eje = (destino - Projectile.Center).SafeNormalize(Vector2.UnitX);
                float largo = Vector2.Distance(Projectile.Center, destino);
                int dano = Math.Max(1, (int)(Projectile.damage * 1.35f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    Vector2 rel = npc.Center - Projectile.Center;
                    float proye = Vector2.Dot(rel, eje);
                    if (proye < -20f || proye > largo + 20f) continue;
                    float perp = MathF.Abs(rel.X * -eje.Y + rel.Y * eje.X);
                    if (perp > 42f + Math.Max(npc.width, npc.height) * 0.3f) continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, dano, 2.5f, true);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                float edad = Projectile.localAI[0];
                int semilla = Projectile.identity % 59 + 3;
                bool veredicto = Projectile.ai[0] == 1f;
                float rayoEdad = veredicto ? edad - JuicioTicks : -1f;

                // ============================================================
                //  EL VELO DE CINE (BrumaFX de la casa): DOS CORTINAS de
                //  bruma a los lados del ojo — se corren al abrir el
                //  párpado (el telón del juicio).
                // ============================================================
                try
                {
                    float apertura = MathHelper.Clamp(edad / 30f, 0f, 1f);   // el telón se corre
                    float separa = MathHelper.Lerp(30f, 86f, apertura);
                    Vector2 centroScr = Projectile.Center - Main.screenPosition;
                    BrumaFX.BeginMass();
                    BrumaFX.Cloud(centroScr + new Vector2(-separa, 0f), 40f,
                        DignosGrimorio.TintaGrieta, semilla, t, 4, alpha: 0.40f);
                    BrumaFX.Cloud(centroScr + new Vector2(separa, 0f), 40f,
                        DignosGrimorio.TintaGrieta, semilla + 17, t * 0.9f, 4, alpha: 0.40f);
                }
                catch { }
                finally
                {
                    try { Main.spriteBatch.End(); } catch { }
                    VFXCore.CerrarLoteSiAbierto();
                }

                // ============================================================
                //  EL OJO (el lote de texturas de la casa — el patrón del
                //  Ojo que Caza .51.4, ahora COLOSAL ×1,35): el párpado
                //  se abre por fases (Cerrado→Medio→Abierto).
                // ============================================================
                float aperturaOjo = MathHelper.Clamp(edad / 26f, 0f, 1f);
                string texOjo = veredicto && rayoEdad > RayoTicks - 8
                    ? "AethonMod/Content/Projectiles/OjoEterno_Cerrado"          // el parpadeo final
                    : aperturaOjo >= 1f ? "AethonMod/Content/Projectiles/OjoEterno_Abierto"
                    : aperturaOjo > 0.45f ? "AethonMod/Content/Projectiles/OjoEterno_Medio"
                    : "AethonMod/Content/Projectiles/OjoEterno_Cerrado";
                Texture2D tex = ModContent.Request<Texture2D>(texOjo).Value;
                Vector2 posPantalla = Projectile.Center - Main.screenPosition;
                Color color = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    Main.spriteBatch.Draw(tex, posPantalla, null, color, 0f,
                        tex.Size() * 0.5f, 1.35f, SpriteEffects.None, 0f);
                    if (aperturaOjo >= 1f)
                    {
                        // EL IRIS DEL JUEZ: deslizado y ROTADO hacia la presa
                        // (la pupila del Grimorio Hambriento — 32×32).
                        Texture2D iris = ModContent.Request<Texture2D>(
                            "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                        float mirada = Projectile.localAI[1];
                        Vector2 hacia = new Vector2(MathF.Cos(mirada), MathF.Sin(mirada));
                        Main.spriteBatch.Draw(iris, posPantalla + hacia * 15f, null,
                            color, mirada, iris.Size() * 0.5f, 1.12f, SpriteEffects.None, 0f);
                    }
                }
                finally { Main.spriteBatch.End(); }

                // ============================================================
                //  EL HALO + EL RAYO DE LA MIRADA (el lote aditivo).
                // ============================================================
                VFXCore.Begin();
                try
                {
                    // EL HALO DEL JUEZ (frío, creciendo con el juicio).
                    float prog = MathHelper.Clamp(edad / JuicioTicks, 0f, 1f);
                    VFXCore.Quad(Projectile.Center,
                        new Color(210, 170, 255) * (0.16f + 0.20f * prog),
                        new Vector2(70f + 26f * prog, 70f + 26f * prog));

                    // === EL RAYO DE LA MIRADA — EL VEREDICTO: el haz de
                    //     DOS NÚCLEOS del ojo a su presa, con la bruma
                    //     exhalando a lo largo (la firma de la casa). ===
                    if (veredicto && rayoEdad >= 0f && rayoEdad <= RayoTicks)
                    {
                        float alfaR = rayoEdad < 3f ? rayoEdad / 3f
                            : 1f - MathHelper.Clamp((rayoEdad - RayoTicks + 5f) / 5f, 0f, 1f);
                        Vector2 rumbo = new Vector2(MathF.Cos(Projectile.localAI[1]),
                                                    MathF.Sin(Projectile.localAI[1]));
                        float largo = _juzgado != null && _juzgado.active
                            ? Vector2.Distance(Projectile.Center, _juzgado.Center)
                            : 900f;
                        Vector2 fin = Projectile.Center + rumbo * largo;
                        float vivo = 0.75f + 0.25f * MathF.Sin(t * 24f);

                        // EL CUERPO del rayo (violeta, gordo)…
                        VFXCore.Line(Projectile.Center, fin,
                            DignosGrimorio.VioletaBinaria * (0.55f * alfaR * vivo), 26f);
                        // …EL NÚCLEO blanco (lo que ciega)…
                        VFXCore.Line(Projectile.Center, fin,
                            Color.White * (0.85f * alfaR * vivo), 10f);
                        // …y EL ALIENTO (el rayo EXHALA bruma violeta a lo
                        // largo — tres volutas a su compás).
                        for (int k = 0; k < 4; k++)
                        {
                            float f = (k + 0.5f) / 4f;
                            Vector2 posB = Vector2.Lerp(Projectile.Center, fin, f) +
                                new Vector2(MathF.Sin(t * 3f + k * 2.2f) * 16f,
                                            MathF.Cos(t * 2.6f + k * 1.7f) * 12f);
                            VFXCore.Quad(posB,
                                DignosGrimorio.VioletaBinaria * (0.22f * alfaR),
                                new Vector2(40f, 40f));
                        }
                    }
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.FlushAdditive();

                // ============================================================
                //  LA BRUMA DEL RAYO (BrumaFX): el veredicto exhala su
                //  niebla violeta sobre la línea (encima del aditivo).
                // ============================================================
                if (veredicto && rayoEdad >= 0f && rayoEdad <= RayoTicks)
                {
                    try
                    {
                        Vector2 rumbo = new Vector2(MathF.Cos(Projectile.localAI[1]),
                                                    MathF.Sin(Projectile.localAI[1]));
                        float largo = _juzgado != null && _juzgado.active
                            ? Vector2.Distance(Projectile.Center, _juzgado.Center)
                            : 900f;
                        float alfaR = 1f - MathHelper.Clamp(rayoEdad / RayoTicks, 0f, 1f);
                        Vector2 posScr = (Projectile.Center + rumbo * largo * 0.5f)
                            - Main.screenPosition;
                        BrumaFX.BeginMass();
                        BrumaFX.Cloud(posScr, 44f, new Color(56, 28, 88),
                            semilla + 41, t, 3, alpha: 0.30f * alfaR);
                    }
                    catch { }
                    finally
                    {
                        try { Main.spriteBatch.End(); } catch { }
                        VFXCore.CerrarLoteSiAbierto();
                    }
                }

                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.ReabrirLoteVanilla(); }
            return false;
        }
    }

    // ======================================================================
    //  4 · EL CORAZÓN DEL MAGNETAR — la mezcla pedida (v6.51.5)
    //
    //  LA LETRA: «crea una nueva arma que sea una mezcla de Baston del
    //  magnetar y el colapso del magnetar e incluyela en la nueva bolsa
    //  de ideas».
    //
    //  LA MEZCLA (los DOS PADRES INTACTOS — el corazón solo los ORQUESTA):
    //   · FASE 1 — LA ÓRBITA (la vida del BASTÓN): mantén el clic y el
    //     corazón te ORBITA (el renderer del Magnetar de la casa — el
    //     campo retorcido y la arritmia) con su AURA magnética (140 px,
    //     45 %, cada 10 ticks) y sus CADENAS DE RAYO automáticas (cada
    //     20 ticks, hasta 3 presas en 240 px, 50 % + Electrificado) —
    //     LOS NÚMEROS EXACTOS DEL PADRE.
    //   · FASE 2 — EL COLAPSO (la muerte del COLAPSO): al SOLTAR, el
    //     corazón sale DISPARADO al cursor con el freno geométrico del
    //     padre (aterriza EXACTO)… y vive SU CICLO ENTERO: el freno
    //     tembloroso cargando, EL STARQUAKE (el anillo que golpea solo
    //     con el frente) y el overclock final — TODO el padre, invocado
    //     con sus propios números.
    //
    //  ai[0] = estado: 0 órbita. localAI[0] = edad. localAI[1] = el
    //  ángulo de la órbita.
    // ======================================================================
    public class CorazonMagnetarProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        // === LA ÓRBITA (los números del BASTÓN del Magnetar) ===
        private const float AuraRadius = 140f;
        private const int ChainTargets = 3;
        private const float RadioOrbita = 62f;

        // === LA VALLA DE SEGURIDAD de la órbita ===
        private const int OrbitaMax = 480;      // 8 s y colapsa sola

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = false;        // el daño es manual (aura + cadenas + el padre)
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = OrbitaMax + 30;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;     // el corazón es de todos los cables
        }

        /// <summary>Semilla determinista del corazón.</summary>
        private int Seed => Math.Max(1, Projectile.identity + 41);

        public override void AI()
        {
            Projectile.localAI[0] += 1f;
            float edad = Projectile.localAI[0];
            Player dueno = Main.player[Projectile.owner];
            bool duenoVivo = dueno != null && dueno.active && !dueno.dead;

            // ================================================================
            //  FASE 1 — LA ÓRBITA DEL BASTÓN: el corazón te rodea.
            // ================================================================
            float giro = Projectile.localAI[1] + 0.055f;      // ~1 vuelta / 2 s
            Projectile.localAI[1] = giro;
            float radio = RadioOrbita + MathF.Sin(edad * 0.031f) * 9f;   // la órbita respira
            if (duenoVivo)
            {
                Projectile.Center = dueno.Center + new Vector2(
                    MathF.Cos(giro) * radio, MathF.Sin(giro) * radio * 0.82f);
            }
            Projectile.velocity = Vector2.Zero;
            Projectile.scale = ElasticOut(MathHelper.Clamp(edad / 40f, 0f, 1f));

            // === EL AURA MAGNÉTICA (los números del Bastón: 140 px, 45 %,
            //     cada 10 ticks + Electrificado) — GolpeMotor. ===
            if (edad > 14f && edad % 10f == 0f && Projectile.scale > 0.25f)
            {
                int auraDamage = Math.Max(1, (int)(Projectile.damage * 0.45f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if ((npc.Center - Projectile.Center).Length() > AuraRadius) continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, auraDamage, 1f, true);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        try { npc.AddBuff(BuffID.Electrified, 150); } catch { }
                }
            }

            // === LAS CADENAS DE RAYO AUTOMÁTICAS (los números del Bastón:
            //     cada 20 ticks, hasta 3 en 240 px, 50 % + Electrificado). ===
            if (edad > 14f && edad % MagnetarRenderer.ChainPeriod == 0f && Projectile.scale > 0.25f)
            {
                int chainDamage = Math.Max(1, (int)(Projectile.damage * 0.5f));
                var targets = PresasEnCadena(MagnetarRenderer.ChainRange, ChainTargets);
                foreach (NPC npc in targets)
                {
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, chainDamage, 2f, true);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        try { npc.AddBuff(BuffID.Electrified, 150); } catch { }
                }
                if (Main.netMode != NetmodeID.Server && targets.Count > 0)
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item93.WithPitchOffset(0.25f), Projectile.Center);
            }

            // LA LUZ Y LAS CHISPAS (el padre: violeta con ARRITMIA).
            float arrhythmia = MathF.Sin(Main.GlobalTimeWrappedHourly * 6.3f +
                MathF.Sin(Main.GlobalTimeWrappedHourly * 2.17f + Seed * 0.7f) * 2.4f + Seed);
            float pulse = 0.55f + 0.45f * arrhythmia;
            float lightR = (0.45f + 0.55f * pulse) * Projectile.scale;
            Lighting.AddLight(Projectile.Center, 0.72f * lightR, 0.45f * lightR, 1.15f * lightR);
            if (Main.netMode != NetmodeID.Server && edad % 5f == 0f && Projectile.scale > 0.3f)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                    new Vector2(Main.rand.NextFloat(-2.2f, 2.2f), Main.rand.NextFloat(-2.2f, 2.2f)),
                    160, new Color(200, 150, 255), 0.7f);
                d.noGravity = true;
            }

            // ================================================================
            //  EL DESPEGUE — AL SOLTAR EL CLIC (o a la valla de seguridad):
            //  el corazón SE VA A VIVIR EL COLAPSO DEL PADRE.
            // ================================================================
            bool solto = edad > 16f && !DuenioCanaliza(dueno);
            bool valla = edad >= OrbitaMax;
            if ((solto || valla) && Projectile.owner == Main.myPlayer)
                LanzarColapso(duenoVivo ? dueno : null);
        }

        /// <summary>¿El dueño sigue manteniendo el clic con EL CORAZÓN en
        /// la mano? (el patrón del canal de la casa).</summary>
        private bool DuenioCanaliza(Player duenio)
        {
            return duenio != null && duenio.active && !duenio.dead &&
                duenio.channel && duenio.itemAnimation > 0 &&
                duenio.HeldItem != null &&
                duenio.HeldItem.type == ModContent.ItemType<DignosCorazonMagnetar>();
        }

        /// <summary>EL DESPEGUE: el corazón llama AL PADRE — un
        /// ColapsoMagnetarProjectile de verdad nace en su posición, con
        /// el cursor como punto de aterrizaje y LA SERIE GEOMÉTRICA del
        /// padre (aterriza EXACTO). El ciclo entero (vuelo · freno ·
        /// STARQUAKE · overclock) es SUYO — el corazón solo lo entrega.</summary>
        private void LanzarColapso(Player dueno)
        {
            // EL PUNTO OBJETIVO: donde está el cursor al soltar.
            Vector2 target = Main.MouseWorld;
            Vector2 delta = target - Projectile.Center;
            Vector2 v0 = delta / ColapsoMagnetarProjectile.TravelFactor;
            float speed = v0.Length();
            if (speed > 64f) v0 *= 64f / speed;
            else if (speed < 8f)
                v0 = speed > 0.001f ? v0 * (8f / speed) : -Vector2.UnitY * 8f;
            Vector2 aterrizaje = Projectile.Center + v0 * ColapsoMagnetarProjectile.TravelFactor;
            float faseMagnetica = Main.rand.NextFloat(MathHelper.TwoPi);

            Projectile.NewProjectile(
                Projectile.GetSource_FromAI(),
                Projectile.Center, v0,
                ModContent.ProjectileType<ColapsoMagnetarProjectile>(),
                Projectile.damage, Projectile.knockBack,
                Projectile.owner, aterrizaje.X, aterrizaje.Y, faseMagnetica);

            // LA DESPEDIDA (el corazón entrega el mando).
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item122.WithPitchOffset(-0.4f), Projectile.Center);
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 12; i++)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                        Main.rand.NextVector2Circular(4f, 4f), 180,
                        new Color(200, 150, 255), 1.1f);
                    d.noGravity = true;
                }
            }
            Projectile.Kill();
        }

        /// <summary>Las N presas más cercanas en rango (para las cadenas —
        /// el búfer reutilizado de la casa, cero GC).</summary>
        private static readonly List<NPC> _bufCadenas = new List<NPC>(8);
        private static readonly List<float> _bufDists = new List<float>(8);

        private List<NPC> PresasEnCadena(float maxRange, int count)
        {
            _bufCadenas.Clear();
            _bufDists.Clear();
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float dist = (npc.Center - Projectile.Center).Length();
                if (dist > maxRange) continue;
                int i = _bufCadenas.Count;
                while (i > 0 && _bufDists[i - 1] > dist) i--;
                _bufCadenas.Insert(i, npc);
                _bufDists.Insert(i, dist);
                if (_bufCadenas.Count > count)
                {
                    _bufCadenas.RemoveAt(count);
                    _bufDists.RemoveAt(count);
                }
            }
            return _bufCadenas;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // ============================================================
            //  CONTRATO DE BATCH v6.10: cerrar el lote del juego antes
            //  del pase propio (la lección de los agujeros).
            // ============================================================
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                // LA ESTRELLA DEL PADRE (el renderer del Magnetar de la
                // casa: el campo retorcido, la arritmia Y LAS CADENAS
                // dibujándose donde pegan).
                float lifeT = MathHelper.Clamp(Projectile.localAI[0] / (float)OrbitaMax, 0f, 1f);
                MagnetarRenderer.Draw(Projectile, lifeT, Seed);

                // EL TETHER (el hilo violeta que une el corazón con su
                // dueño — la órbita se VE): un lote aditivo corto.
                Player dueno = Main.player[Projectile.owner];
                if (dueno != null && dueno.active && !dueno.dead)
                {
                    VFXCore.Begin();
                    try
                    {
                        float t = Main.GlobalTimeWrappedHourly;
                        float late = 0.5f + 0.5f * MathF.Sin(t * 5f);
                        VFXCore.Line(dueno.Center, Projectile.Center,
                            new Color(168, 120, 255) * (0.16f + 0.08f * late), 2.4f);
                    }
                    catch { VFXCore.CerrarLoteSiAbierto(); }
                    VFXCore.FlushAdditive();
                }
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>Elastic ease-out (curva elástica de aparición).</summary>
        private static float ElasticOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c = (2f * (float)Math.PI) / 3f;
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c) + 1);
        }
    }
}
