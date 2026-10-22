using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Weapons.Cosmic;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    // ======================================================================
    //  v6.51.4 — LOS PROYECTILES DE LOS SEIS CANDIDATOS DIGNOS.
    //
    //  Petición del usuario: "crea un arma nueva por cada una de estas
    //  ideas que diste y crea una nueva bolsa para darsela al jugador".
    //  Las seis IDEAS de la v6.50.19 (el folio, la pluma, el sello, la
    //  lengua, el ojo y el verso) maduran aquí en seis ARMAS DIGNAS del
    //  Grimorio del Eterno — una por idea, cada una con su mecánica
    //  completa (nada de escaleras de nivel: la idea final, de una pieza):
    //
    //    1 · EL EQUINOCCIO BINARIO — la estrella doble que vive el reloj
    //        del mundo (día = oro y eclipse en área · noche = violeta,
    //        perfora y drena).
    //    2 · LAS COMAS QUE MUERDEN — el glifo-coma que se queda
    //        suspendido esperando el latigazo; seis en el mismo enemigo
    //        se alinean en UNA FRASE… y la frase DETONA.
    //    3 · LA PÁGINA ARRANCADA — la hoja-cuchilla de dos caras que se
    //        CLAVA flotando y arde en EL VERSO; las páginas cercanas se
    //        ENCADENAN.
    //    4 · EL OJO QUE CAZA — la mirada del escuadrón en miniatura:
    //        viaja dormido, ABRE el párpado y FIJA la pupila en su presa;
    //        el impacto es un PARPADEO.
    //    5 · LA LÍNEA DE LECTURA — hojea el libro cargando la frase; al
    //        soltar, un RAYO de glifos que atraviesa todo en recta.
    //    6 · EL MARCAPÁGINAS — los dardos-marca que se CLAVAN al enemigo;
    //        al quinto marcador: FIN DEL CAPÍTULO, el libro se cierra
    //        de golpe sobre él.
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
    //  v6.51.4 — EL TEATRO DE LOS DIGNOS: un solo proyectil cosmético con
    //  SIETE modos de fuego (el patrón BrumaNacimientoFX: cada cable la
    //  suya, netImportant = false, sin daño). Los dignos lo invocan desde
    //  sus golpes porque el que golpea MUERE (el parpadeo, el eclipse) o
    //  golpea desde un hook sin dibujo propio.
    //
    //    ai[0] = MODO: 1 ONDA DE ECLIPSE (equinoccio día) · 2 DRENO DE
    //    LUNA (equinoccio noche) · 3 LA FRASE DETONA (comas) · 4 EL VERSO
    //    (página) · 5 EL PARPADEO (ojo) · 6 EL GOLPE DEL LIBRO
    //    (marcapáginas) · 7 EL PUNTO FINAL (línea).
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
                    3 => 20,   // la frase detona
                    4 => 24,   // el verso
                    5 => 12,   // el parpadeo
                    6 => 20,   // el golpe del libro
                    7 => 10,   // el punto final
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
            // LA LUZ DEL TEATRO (el parpadeo y el libro encienden; el resto apenas).
            if (Main.netMode == NetmodeID.Server) return;
            int modo = (int)Projectile.ai[0];
            if (modo == 5) Lighting.AddLight(Projectile.Center, 1.2f, 1.0f, 0.6f);
            else if (modo == 6) Lighting.AddLight(Projectile.Center, 0.45f, 0.32f, 0.14f);
            else if (modo == 1 || modo == 4)
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

                if (modo == 6)
                {
                    // ========================================================
                    //  6 — EL GOLPE DEL LIBRO: la portada oscura cae y CIERRA
                    //  (capa ALFA para lo oscuro — el negro no existe en el
                    //  aditivo), lote aditivo para el lomo y el aro.
                    // ========================================================
                    float caida = MathHelper.Clamp(Edad() / 4f, 0f, 1f);   // el portazo
                    float golpe = 1f - caida;
                    Vector2 posLibro = centro + new Vector2(0f, -golpe * 30f);
                    VFXCore.CerrarLoteSiAbierto();
                    VFXCore.Begin();
                    try
                    {
                        // EL LIBRO CERRADO (dos capas de portada — el grosor).
                        VFXCore.Quad(posLibro, new Color(30, 20, 46) * 0.94f,
                            new Vector2(90f, 70f), 0f, VFXCore.Pixel);
                        VFXCore.Quad(posLibro + new Vector2(2f, 3f),
                            new Color(18, 12, 30) * 0.85f,
                            new Vector2(90f, 70f), 0f, VFXCore.Pixel);
                    }
                    catch { VFXCore.CerrarLoteSiAbierto(); }
                    VFXCore.FlushAlpha();
                    VFXCore.Begin();
                    try
                    {
                        // EL LOMO DORADO y el destello del portazo.
                        VFXCore.Line(posLibro + new Vector2(0f, -34f),
                            posLibro + new Vector2(0f, 34f),
                            DignosGrimorio.OroBinaria * 0.8f, 4f);
                        VFXCore.Quad(posLibro, DignosGrimorio.OroBinaria * 0.28f * apagon,
                            new Vector2(130f, 110f));
                        // EL ARO DEL GOLPE (la onda blanca del cierre).
                        float radio = 24f + 70f * fase;
                        VFXCore.Quad(centro, Color.White * 0.55f * apagon,
                            VFXCore.RingQuadSize(radio), 0f, VFXCore.Ring);
                    }
                    catch { VFXCore.CerrarLoteSiAbierto(); }
                    VFXCore.FlushAdditive();
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
                        case 3:
                        {
                            // 3 — LA FRASE DETONA: ocho glifos en FILA — la
                            //     palabra explotando como estaba escrita.
                            for (int i = 0; i < 8; i++)
                            {
                                Vector2 pos = centro + new Vector2(
                                    (i - 3.5f) * 12f,
                                    MathF.Sin(t * 8f + i * 1.9f) * 3f - fase * 8f);
                                float tam = 5.5f + 3.5f * VFXCore.Hash01(semilla, i, (int)(t * 12f));
                                VFXCore.Quad(pos,
                                    DignosGrimorio.OroGlifo * (0.85f * apagon),
                                    new Vector2(tam, tam),
                                    VFXCore.Hash01(semilla, i + 5, (int)(t * 12f)) * MathHelper.PiOver2,
                                    VFXCore.Pixel);
                            }
                            VFXCore.Quad(centro, DignosGrimorio.OroGlifo * 0.3f * apagon,
                                new Vector2(70f, 26f));
                            break;
                        }
                        case 4:
                        {
                            // 4 — EL VERSO: seis glifos que ascienden — la
                            //     frase quemándose hacia el cielo.
                            for (int i = 0; i < 6; i++)
                            {
                                Vector2 pos = centro + new Vector2(
                                    MathF.Sin(i * 1.7f + t * 1.4f) * 10f,
                                    -(fase * 58f + i * 8f));
                                VFXCore.Quad(pos, DignosGrimorio.OroGlifo * (0.8f * apagon),
                                    new Vector2(7f, 11f),
                                    MathF.Sin(t * 3f + i) * 0.35f, VFXCore.Pixel);
                            }
                            float radio = 14f + 44f * fase;
                            VFXCore.Quad(centro, DignosGrimorio.OroBinaria * 0.45f * apagon,
                                VFXCore.RingQuadSize(radio), 0f, VFXCore.Ring);
                            VFXCore.Quad(centro, DignosGrimorio.OroBinaria * 0.35f * apagon,
                                new Vector2(50f, 50f));
                            break;
                        }
                        case 5:
                        {
                            // 5 — EL PARPADEO: el destello blanco gordo + el
                            //     aro dorado en DOS QUADS CRUZADOS alargados.
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
                        default:
                        {
                            // 7 — EL PUNTO FINAL: el puntito que cierra la frase.
                            float tam = 12f * apagon + 2f;
                            VFXCore.Quad(centro, DignosGrimorio.OroGlifo * 0.8f * apagon,
                                new Vector2(tam, tam));
                            VFXCore.Quad(centro, Color.White * 0.5f * apagon,
                                new Vector2(tam * 0.45f, tam * 0.45f));
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
                1 => 22f, 2 => 16f, 3 => 20f, 4 => 24f, 5 => 12f, 6 => 20f, 7 => 10f, _ => 12f,
            };
            return MathHelper.Clamp(vidaMax - Projectile.timeLeft, 0f, vidaMax - 1f);
        }
    }

    // ======================================================================
    //  1 · EL EQUINOCCIO BINARIO — la estrella doble día/noche
    //
    //  UN proyectil = LA PAREJA: el disco dorado y el violeta orbitándose
    //  (radio 24 px, contrafase, ~2,6 rad/s) mientras vuelan con homing
    //  suave. El RELOJ DEL MUNDO decide quién guía: de DÍA el ORO va
    //  delante (el ángulo de la órbita más cercano al rumbo) y el golpe
    //  es un MINI ECLIPSE (estallido en área + onda dorada + muerte);
    //  de NOCHE el VIOLETA guía, el disparo PERFORA 4 y cada mordida
    //  DRENA vida al dueño.
    //
    //  ai[0] = modo: 1 DÍA · 2 NOCHE (lo pone el arma leyendo Main.dayTime).
    // ======================================================================
    public class EquinoccioBinarioProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private bool EsDia => Projectile.ai[0] == 1f;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 44;             // la hitbox es LA PAREJA
            Projectile.height = 44;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.light = 0.4f;
        }

        public override void AI()
        {
            // LA NOCHE PERFORA (una sola vez — la marca de localAI[1]):
            // se hace en la IA (no en el Shoot) para que TODAS las copias
            // de la red lo calzacen solas (el penetrate no viaja en los
            // mensajes de vanilla).
            if (!EsDia && Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                Projectile.penetrate = 4;
            }

            // EL HOMING SUAVE (accel 0,12 · vmax 11).
            NPC presa = DignosGrimorio.PresaCercana(Projectile.Center, 900f);
            if (presa != null)
            {
                Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Projectile.velocity += hacia * 0.12f;
                float v = Projectile.velocity.Length();
                if (v > 11f) Projectile.velocity = Projectile.velocity / v * 11f;
            }

            // LA LUZ DE LA BINARIA (día cálido · noche violeta).
            if (Main.netMode != NetmodeID.Server)
            {
                if (EsDia) Lighting.AddLight(Projectile.Center, 0.5f, 0.4f, 0.22f);
                else Lighting.AddLight(Projectile.Center, 0.34f, 0.22f, 0.55f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (EsDia)
            {
                // ============================================================
                //  EL MINI ECLIPSE (día): estallido dorado en área — el daño
                //  manual de la casa por GOLPEMOTOR (el cauce del motor:
                //  crítica real, varianza, on-hit, crédito de kill).
                // ============================================================
                DignosFXProjectile.Nacer(Projectile, Projectile.Center, 1);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(0.6f), Projectile.Center);
                if (Main.myPlayer == Projectile.owner)
                {
                    int danoArea = Math.Max(1, Projectile.damage / 2);
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!VFXCore.EsObjetivo(npc)) continue;
                        if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > 90f * 90f) continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, danoArea, 1.5f, true);
                    }
                }
                Projectile.Kill();
            }
            else
            {
                // ============================================================
                //  LA MORDIDA DE LUNA (noche): drena vida al dueño — la
                //  sanación de la casa (HealEffect + statLife, solo el
                //  cliente del dueño, el patrón del MiniEstallidoPet).
                // ============================================================
                DignosFXProjectile.Nacer(Projectile, Projectile.Center, 2);
                Player duenio = Main.player[Projectile.owner];
                if (duenio != null && duenio.active && !duenio.dead &&
                    duenio.whoAmI == Main.myPlayer)
                {
                    int cura = Math.Max(1, (int)(damageDone * 0.08f));
                    duenio.HealEffect(cura);
                    duenio.statLife += cura;
                    if (duenio.statLife > duenio.statLifeMax2)
                        duenio.statLife = duenio.statLifeMax2;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                // EL DISCO DE VANILLA (el Nightglow — el proyectil 931, la
                // semilla visual de la familia) teñido por cada estrella.
                Texture2D disco = ModContent.Request<Texture2D>(
                    "Terraria/Images/Projectile_931").Value;
                float t = Main.GlobalTimeWrappedHourly;
                float rumbo = Projectile.velocity.LengthSquared() > 0.1f
                    ? Projectile.velocity.ToRotation() : Projectile.rotation;
                float giro = Projectile.identity * 1.31f + t * 2.6f;

                // ¿Cuál de los DOS ángulos de la órbita va DELANTE (el más
                // cercano al rumbo)? Ese asiento es del color que MANDA.
                bool aLidera = Math.Abs(MathHelper.WrapAngle(giro - rumbo)) <=
                    Math.Abs(MathHelper.WrapAngle(giro + MathHelper.Pi - rumbo));
                float angLider = aLidera ? giro : giro + MathHelper.Pi;
                float angOro = EsDia ? angLider : angLider + MathHelper.Pi;
                float angVioleta = angOro + MathHelper.Pi;

                VFXCore.CerrarLoteSiAbierto();
                VFXCore.Begin();
                try
                {
                    DibujarDisco(angOro, DignosGrimorio.OroBinaria, disco);
                    DibujarDisco(angVioleta, DignosGrimorio.VioletaBinaria, disco);

                    // EL EJE ORBITAL (la estela tenue que une la par).
                    Vector2 pOro = Projectile.Center + Orbita(angOro);
                    Vector2 pVioleta = Projectile.Center + Orbita(angVioleta);
                    Color eje = Color.Lerp(DignosGrimorio.OroBinaria,
                        DignosGrimorio.VioletaBinaria, 0.5f);
                    VFXCore.Line(pOro, pVioleta, eje * 0.18f, 2f);
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.FlushAdditive();
                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.ReabrirLoteVanilla(); }
            return false;
        }

        /// <summary>El vector de la órbita (radio 24 px).</summary>
        private static Vector2 Orbita(float angulo) =>
            new Vector2(MathF.Cos(angulo), MathF.Sin(angulo)) * 24f;

        /// <summary>Una estrella de la par: halo SoftGlow + disco teñido
        /// girando + la estela del arco que deja al orbitar.</summary>
        private void DibujarDisco(float angulo, Color tinte, Texture2D disco)
        {
            Vector2 pos = Projectile.Center + Orbita(angulo);
            VFXCore.Quad(pos, tinte * 0.30f, new Vector2(42f, 42f));       // el halo
            VFXCore.Quad(pos, tinte, new Vector2(26f, 26f), angulo, disco); // el disco
            for (int k = 1; k <= 3; k++)                                    // la estela
            {
                float angE = angulo - k * 0.13f;
                Vector2 posE = Projectile.Center + Orbita(angE);
                VFXCore.Quad(posE, tinte * (0.22f / k), new Vector2(18f / k, 18f / k));
            }
        }
    }

    // ======================================================================
    //  2 · LAS COMAS QUE MUERDEN — el glifo suspendido y su latigazo
    //
    //  Vuela recta ~26 ticks, FRENA y se queda SUSPENDIDA como una coma
    //  (cabeceo suave, giro lento) esperando. Cuando el dueño suelta el
    //  clic (o guarda el arma), TODAS las comas descargan EL LATIGAZO:
    //  embestida a 26 contra el enemigo válido más cercano (1400 px,
    //  homing refrescado cada 4 ticks) dejando un rastro de látigo.
    //  Al AZOTAR: cada coma cuenta en el enemigo; la SEXTA coma en el
    //  mismo blanco (ventana de 90 ticks) alinea LA FRASE… y LA FRASE
    //  DETONA (golpe ×3 + ocho glifos dorados en fila).
    //
    //  ai[0] = estado: 0 vuelo · 1 suspendida · 3 latigazo.
    // ======================================================================
    public class ComaRunicaProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        /// <summary>EL CONTEO DE COMAS por enemigo: whoAmI → (comas
        /// clavadas, tick del último azote). Las entradas caducan a los
        /// 90 ticks (la frase se escribe ENCADENADA o no se escribe).</summary>
        private static readonly Dictionary<int, (int cuenta, uint tick)> ComasEnNpc =
            new Dictionary<int, (int, uint)>();

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;        // valla de seguridad: 10 s y se disuelve
            Projectile.light = 0.2f;
            Projectile.tileCollide = false;   // el latigazo atraviesa el papel
        }

        public override void AI()
        {
            float estado = Projectile.ai[0];

            if (estado == 0f)
            {
                // === EL VUELO CORTO (la coma se escribe y frena). ===
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.localAI[0]++;
                if (Projectile.localAI[0] >= 26f)
                {
                    Projectile.ai[0] = 1f;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.netUpdate = true;
                }
            }
            else if (estado == 1f)
            {
                // === LA COMA SUSPENDIDA (cabeceo + giro lento + espera). ===
                Projectile.velocity = Vector2.UnitY *
                    MathF.Sin(Main.GlobalTimeWrappedHourly * 2.2f + Projectile.identity) * 0.35f;
                Projectile.rotation += 0.03f;
                Projectile.localAI[1]++;

                // CADA 10 TICKS: ¿sigue el dueño manteniendo el clic y el arma?
                if (Projectile.localAI[1] % 10f == 0f && !DuenioCanaliza())
                {
                    NPC presa = DignosGrimorio.PresaCercana(Projectile.Center, 1400f);
                    if (presa != null)
                    {
                        // EL LATIGAZO: origen del rastro (localAI reutilizada)
                        // y embestida a 26 contra la presa.
                        Projectile.ai[0] = 3f;
                        Projectile.localAI[0] = Projectile.Center.X;
                        Projectile.localAI[1] = Projectile.Center.Y;
                        Projectile.velocity = (presa.Center - Projectile.Center)
                            .SafeNormalize(Vector2.UnitX) * 26f;
                        Projectile.netUpdate = true;
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item153.WithPitchOffset(0.2f), Projectile.Center);
                    }
                    else
                    {
                        // Sin presa a la vista: la coma se deshace en un suspiro.
                        Fizzlear();
                    }
                }
            }
            else if (estado == 3f)
            {
                // === EL LATIGAZO (homing refrescado cada 4 ticks). ===
                Projectile.rotation = Projectile.velocity.ToRotation();
                if (Projectile.timeLeft % 4f == 0f)
                {
                    NPC presa = DignosGrimorio.PresaCercana(Projectile.Center, 1600f);
                    if (presa != null)
                        Projectile.velocity = (presa.Center - Projectile.Center)
                            .SafeNormalize(Vector2.UnitX) * 26f;
                    else Fizzlear();
                }
                if (Main.rand.NextBool(2))
                {
                    int d = Dust.NewDust(Projectile.Center - new Vector2(5f, 5f), 10, 10,
                        DustID.Enchanted_Gold, -Projectile.velocity.X * 0.1f,
                        -Projectile.velocity.Y * 0.1f);
                    Main.dust[d].noGravity = true;
                }
            }
        }

        /// <summary>¿El dueño sigue manteniendo el clic con LAS COMAS en
        /// la mano? (el patrón del canal de la casa: channel + animación
        /// + el arma en la mano).</summary>
        private bool DuenioCanaliza()
        {
            Player duenio = Main.player[Projectile.owner];
            return duenio != null && duenio.active && !duenio.dead &&
                duenio.channel && duenio.itemAnimation > 0 &&
                duenio.HeldItem != null &&
                duenio.HeldItem.type == ModContent.ItemType<DignosComasQueMuerden>();
        }

        /// <summary>La coma se deshace sin azotar (puff y fuera).</summary>
        private void Fizzlear()
        {
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(5f, 5f), 10, 10,
                    DustID.Enchanted_Gold,
                    Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-1.5f, 0.3f));
                Main.dust[d].noGravity = true;
            }
            Projectile.Kill();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // ============================================================
            //  EL AZOTE CUENTA — y la SEXTA coma en el mismo enemigo
            //  (ventana de 90 ticks) alinea LA FRASE… y DETONA.
            // ============================================================
            uint ahora = Main.GameUpdateCount;
            int clave = target.whoAmI;
            int cuenta = 1;
            if (ComasEnNpc.TryGetValue(clave, out var entrada) && ahora - entrada.tick <= 90u)
                cuenta = entrada.cuenta + 1;          // la ventana corre con cada coma
            ComasEnNpc[clave] = (cuenta, ahora);

            if (cuenta >= 6)
            {
                ComasEnNpc.Remove(clave);             // la frase se leyó: página nueva
                DignosFXProjectile.Nacer(Projectile, target.Center, 3);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(0.45f), target.Center);
                // SOLO el dueño resuelve el golpe (el patrón GolpeMotor).
                if (Projectile.owner == Main.myPlayer)
                    Content.Systems.GolpeMotor.Golpear(Projectile, target,
                        Math.Max(1, Projectile.damage * 3), 2.5f, true);
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 6; i++)
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(6f, 6f), 12, 12,
                    DustID.Enchanted_Gold,
                    Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2.5f, 0.5f));
                Main.dust[d].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                float estado = Projectile.ai[0];
                // LA ROTACIÓN DE LA COMA: suspendida gira lenta; volando y
                // azotando apunta a donde va.
                float rot = estado == 1f
                    ? Projectile.rotation
                    : (Projectile.velocity.LengthSquared() > 0.1f
                        ? Projectile.velocity.ToRotation() : Projectile.rotation);

                VFXCore.CerrarLoteSiAbierto();
                VFXCore.Begin();
                try
                {
                    // EL RASTRO DEL LATIGAZO (del arranque del azote a aquí).
                    if (estado == 3f)
                    {
                        Vector2 origen = new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
                        Vector2 hasta = Projectile.Center;
                        VFXCore.Line(origen, hasta,
                            Color.Lerp(DignosGrimorio.VioletaBinaria, Color.White, 0.5f) * 0.35f, 3f);
                        for (int i = 0; i <= 6; i++)
                        {
                            float f = i / 6f;
                            Vector2 pos = Vector2.Lerp(origen, hasta, f);
                            VFXCore.Quad(pos,
                                Color.Lerp(DignosGrimorio.VioletaBinaria, Color.White, f) * 0.4f,
                                new Vector2(10f - f * 6f, 10f - f * 6f));
                        }
                    }

                    // LA COMA: arco de 5 quads afinándose (cabeza gorda →
                    // cola fina) — la COLA es violeta.
                    for (int k = 0; k < 5; k++)
                    {
                        float f = k / 4f;
                        float ang = rot - 0.9f + f * 2.0f;
                        Vector2 pos = Projectile.Center + new Vector2(
                            MathF.Cos(ang), MathF.Sin(ang)) * (9f - f * 2.5f);
                        float tam = 8.5f - f * 5.0f;
                        Color c = k == 4 ? DignosGrimorio.VioletaBinaria : DignosGrimorio.OroGlifo;
                        VFXCore.Quad(pos, c * (0.85f - f * 0.25f),
                            new Vector2(tam, tam), ang, VFXCore.Pixel);
                    }

                    // EL HALO DE LA CABEZA + el pulso de la espera (la coma
                    // suspendida "cargada" late).
                    float pulso = estado == 1f
                        ? 0.75f + 0.25f * MathF.Sin(t * 5f + Projectile.identity)
                        : 1f;
                    VFXCore.Quad(Projectile.Center, DignosGrimorio.OroGlifo * 0.28f * pulso,
                        new Vector2(26f, 26f));
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
    //  3 · LA PÁGINA ARRANCADA — la hoja-cuchilla de dos caras
    //
    //  Vuela GIRANDO (0,38 rad/tick) con gravedad tenue; una cara es ORO
    //  y la otra VIOLETA (el color de la cara depende de la fase del
    //  giro: la misma hoja, dos tintas). Al golpear enemigo o terreno la
    //  página SE CLAVA: queda FLOTANDO (aleteo, sin daño) 180 ticks de
    //  mecha… y arde en EL VERSO (estallido en área de 110 px + seis
    //  glifos ascendentes). LA CADENA: al clavarse, si hay OTRA página
    //  clavada a menos de 200 px, LA MÁS CERCANA DETONA inmediatamente.
    //
    //  ai[0] = estado: 0 vuelo · 1 clavada. La mecha es el timeLeft
    //  (clavarse lo fija en 180).
    // ======================================================================
    public class PaginaArrancadaProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            // LA PÁGINA SOBREVIVE A SU GOLPE (tiene que clavarse): vuela
            // con inmunidad local de un toque por enemigo — el "perfora 1
            // del enunciado" sin morir en el primer roce.
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 600;
            Projectile.light = 0.3f;
        }

        public override void AI()
        {
            if (Projectile.ai[0] == 0f)
            {
                // === EL VUELO (giro de cuchilla + gravedad tenue). ===
                Projectile.velocity.Y += 0.05f;
                Projectile.rotation += 0.38f;
                if (Main.rand.NextBool(5))
                {
                    int d = Dust.NewDust(Projectile.Center - new Vector2(4f, 4f), 8, 8,
                        DustID.Enchanted_Gold, 0f, -0.5f);
                    Main.dust[d].noGravity = true;
                }
            }
            else
            {
                // === CLAVADA (aleteo + mecha). ===
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = MathF.Sin(Main.GlobalTimeWrappedHourly * 9f +
                    Projectile.identity) * 0.22f;
                if (Projectile.timeLeft <= 1) Explotar();
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // LA HOJA SE CLAVA donde toca el suelo o la pared.
            if (Projectile.ai[0] == 0f) Clavarse();
            return false;                            // jamás muere por el terreno
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // La hoja muerde UNA vez y SE CLAVA en el punto del mordisco.
            if (Projectile.ai[0] == 0f) Clavarse();
        }

        /// <summary>LA PÁGINA SE CLAVA: frena, se desarma (sin daño), la
        /// mecha arranca (180 ticks) y LA CADENA busca compañía.</summary>
        private void Clavarse()
        {
            Projectile.ai[0] = 1f;
            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = false;             // clavada ya no muerde
            Projectile.timeLeft = 180;               // LA MECHA
            Projectile.netUpdate = true;
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item10.WithPitchOffset(-0.25f), Projectile.Center);

            // ============================================================
            //  LA CADENA DE VERSOS: la página recién clavada ENCIENDE a
            //  la más cercana de sus hermanas ya clavadas (≤ 200 px) —
            //  esa detona YA, con su daño completo.
            // ============================================================
            PaginaArrancadaProjectile hermana = null;
            float mejorD = 200f * 200f;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != Type || p.whoAmI == Projectile.whoAmI) continue;
                if (p.ai[0] != 1f) continue;         // solo páginas CLAVADAS
                float d = Vector2.DistanceSquared(p.Center, Projectile.Center);
                if (d < mejorD) { mejorD = d; hermana = p.ModProjectile as PaginaArrancadaProjectile; }
            }
            if (hermana != null) hermana.Explotar();
        }

        /// <summary>EL VERSO: la mecha (o la cadena) quema la página —
        /// estallido en área + glifos ascendentes. UNA sola vez.</summary>
        public void Explotar()
        {
            if (Projectile.localAI[1] == 1f) return; // ya ardieron
            Projectile.localAI[1] = 1f;

            DignosFXProjectile.Nacer(Projectile, Projectile.Center, 4);
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item29.WithPitchOffset(-0.35f), Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(8f, 8f), 16, 16,
                    DustID.Enchanted_Gold,
                    Main.rand.NextFloat(-3.5f, 3.5f), Main.rand.NextFloat(-4f, 1f));
                Main.dust[d].noGravity = true;
            }

            // EL ESTALLIDO (área 110 px, daño completo — GolpeMotor).
            if (Main.myPlayer == Projectile.owner)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > 110f * 110f) continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc,
                        Projectile.damage, 3f, true);
                }
            }
            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                // LA CARA QUE SE VE: la fase del giro decide la tinta
                // (seno > 0 = ORO · seno ≤ 0 = VIOLETA — la misma hoja).
                bool caraOro = MathF.Sin(Projectile.rotation) > 0f;
                Color cara = caraOro
                    ? new Color(255, 216, 130)
                    : new Color(168, 96, 240);
                float rot = Projectile.rotation;
                Vector2 perp = new Vector2(-MathF.Sin(rot), MathF.Cos(rot));

                VFXCore.CerrarLoteSiAbierto();
                VFXCore.Begin();
                try
                {
                    // LA HOJA: tres capas desplazadas (el grosor del papel —
                    // el paralelogramo) + el filo brillante.
                    VFXCore.Quad(Projectile.Center, cara * 0.6f,
                        new Vector2(30f, 22f), rot, VFXCore.Pixel);
                    VFXCore.Quad(Projectile.Center + perp * 1.6f, cara * 0.35f,
                        new Vector2(30f, 22f), rot, VFXCore.Pixel);
                    VFXCore.Quad(Projectile.Center + perp * 3.2f, cara * 0.22f,
                        new Vector2(30f, 22f), rot, VFXCore.Pixel);
                    VFXCore.Quad(Projectile.Center, Color.White * 0.4f,
                        new Vector2(24f, 16f), rot, VFXCore.Pixel);

                    // CLAVADA: la mecha arde (el halo crece con la prisa).
                    if (Projectile.ai[0] == 1f)
                    {
                        float prisa = 1f - Projectile.timeLeft / 180f;
                        VFXCore.Quad(Projectile.Center,
                            DignosGrimorio.OroBinaria * (0.2f + 0.45f * prisa),
                            new Vector2(30f + 26f * prisa, 30f + 26f * prisa));
                    }
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
    //  4 · EL OJO QUE CAZA — la mirada del escuadrón en miniatura
    //
    //  Nace CERRADO (el párpado del arte aprobado) y caza con homing
    //  medio; a los 40 ticks —o al acercarse a 260 px de su presa— ABRE
    //  el párpado: el iris (el del Grimorio Hambriento) se desliza y
    //  ROTA mirando EXACTAMENTE a su presa, y el tope de velocidad sube
    //  (12 → 16: el ojo fijó la mira). El impacto es EL PARPADEO: un
    //  destello blanco con aro dorado cruzado, un golpe de luz y el
    //  chasquido agudo del teletransporte de ojos.
    //
    //  ai[0] = párpado: 0 cerrado · 1 abierto.
    //  localAI[1] = el ángulo de la mirada (solo dibujo).
    // ======================================================================
    public class OjoQueCazaProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/OjoEterno_Abierto";

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.light = 0.35f;
        }

        public override void AI()
        {
            NPC presa = DignosGrimorio.PresaCercana(Projectile.Center, 900f);
            bool abierto = Projectile.ai[0] == 1f;

            // EL HOMING (medio: accel 0,2; el tope sube al abrir).
            if (presa != null)
            {
                Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Projectile.velocity += hacia * 0.2f;
                float vmax = abierto ? 16f : 12f;
                float v = Projectile.velocity.Length();
                if (v > vmax) Projectile.velocity = Projectile.velocity / v * vmax;
            }

            // LA MIRADA (el ángulo del iris hacia la presa).
            if (presa != null)
                Projectile.localAI[1] = (presa.Center - Projectile.Center).ToRotation();
            else if (Projectile.velocity.LengthSquared() > 0.1f)
                Projectile.localAI[1] = Projectile.velocity.ToRotation();

            // EL PÁRPADO SE ABRE: 40 ticks de caza o la presa a 260 px.
            if (!abierto)
            {
                Projectile.localAI[0]++;
                bool cerca = presa != null &&
                    Vector2.DistanceSquared(presa.Center, Projectile.Center) <= 260f * 260f;
                if (Projectile.localAI[0] >= 40f || cerca)
                {
                    Projectile.ai[0] = 1f;
                    Projectile.netUpdate = true;
                }
            }

            if (Main.netMode != NetmodeID.Server)
                Lighting.AddLight(Projectile.Center, 0.4f, 0.34f, 0.2f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // ============================================================
            //  EL PARPADEO: el destello (el teatro lo dibuja el FX — el
            //  ojo MUERE en este golpe), UN frame de luz a gritos y el
            //  chasquido agudo.
            // ============================================================
            DignosFXProjectile.Nacer(Projectile, Projectile.Center, 5);
            Lighting.AddLight(Projectile.Center, 1.2f, 1.0f, 0.6f);
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item4.WithPitchOffset(0.5f), Projectile.Center);
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 6; i++)
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(8f, 8f), 16, 16,
                    DustID.Enchanted_Gold,
                    Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2.5f, 0f));
                Main.dust[d].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                bool abierto = Projectile.ai[0] == 1f;

                // ========================================================
                //  FASE 1 — EL SPRITE DEL OJO (lote propio de texturas, el
                //  patrón del OjoEternoMinion: la mirada aprobada del
                //  escuadrón, 56×54, a media escala).
                // ========================================================
                VFXCore.CerrarLoteSiAbierto();
                Texture2D texOjo = ModContent.Request<Texture2D>(abierto
                    ? "AethonMod/Content/Projectiles/OjoEterno_Abierto"
                    : "AethonMod/Content/Projectiles/OjoEterno_Cerrado").Value;
                Vector2 posPantalla = Projectile.Center - Main.screenPosition;
                Color color = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    Main.spriteBatch.Draw(texOjo, posPantalla, null, color, 0f,
                        texOjo.Size() * 0.5f, 0.5f, SpriteEffects.None, 0f);
                    if (abierto)
                    {
                        // EL IRIS: deslizado y ROTADO hacia la presa — la
                        // pupila del Grimorio Hambriento (32×32).
                        Texture2D iris = ModContent.Request<Texture2D>(
                            "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                        float mirada = Projectile.localAI[1];
                        Vector2 hacia = new Vector2(MathF.Cos(mirada), MathF.Sin(mirada));
                        Main.spriteBatch.Draw(iris, posPantalla + hacia * 5.5f, null,
                            color, mirada, iris.Size() * 0.5f, 0.42f, SpriteEffects.None, 0f);
                    }
                }
                finally { Main.spriteBatch.End(); }

                // ========================================================
                //  FASE 2 — EL HALO (el lote aditivo de la casa): tenue
                //  dormido, ardiendo con el párpado abierto y el iris
                //  clavado en la presa.
                // ========================================================
                VFXCore.Begin();
                try
                {
                    VFXCore.Quad(Projectile.Center,
                        new Color(255, 220, 140) * (abierto ? 0.30f : 0.15f),
                        new Vector2(abierto ? 44f : 30f, abierto ? 44f : 30f));
                    if (abierto)
                    {
                        float mirada = Projectile.localAI[1];
                        Vector2 hacia = new Vector2(MathF.Cos(mirada), MathF.Sin(mirada));
                        VFXCore.Quad(Projectile.Center + hacia * 6f,
                            new Color(255, 240, 190) * 0.35f, new Vector2(14f, 14f));
                    }
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
    //  5 · LA LÍNEA DE LECTURA — la frase cargada y su rayo de texto
    //
    //  Mientras el dueño mantiene el clic, la CARGA vive en la boca del
    //  cañón (sigue la mira, cuenta ticks en localAI[0]) hojando el
    //  libro: partículas de vuelta de página y el vistazo de la frase
    //  creciendo. Al SOLTAR: LA FRASE DISPARA — la carga se vuelve RAYO
    //  (58 px/tick, atraviesa TODO en recta, cada enemigo UNA vez) con
    //  daño ×1 → ×3 (0 → 2,5 s de hojeo) y una estela de glifos cuya
    //  LARGO crece con la carga (la frase se escribió más larga).
    //
    //  ai[0] = estado: 0 carga · 1 rayo. ai[1] = la carga (0..1).
    // ======================================================================
    public class LineaLecturaProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private bool EsRayo => Projectile.ai[0] == 1f;

        /// <summary>La carga viva (0..1) — la del dibujo (localAI[0] / 150).</summary>
        private float CargaViva => MathHelper.Clamp(Projectile.localAI[0] / 150f, 0f, 1f);

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;      // la carga no daña: daña el rayo
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60;
            Projectile.light = 0.3f;
        }

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead) { Projectile.Kill(); return; }

            // EL RELOJ DE LA CARGA corre en TODAS las copias (el vistazo
            // crece en cada pantalla; la mira la pone el dueño).
            Projectile.localAI[0]++;

            if (!EsRayo)
            {
                if (Projectile.owner == Main.myPlayer)
                {
                    // === LA CARGA: pegada a la boca del cañón, siguiendo la mira. ===
                    Vector2 mira = Main.MouseWorld - duenio.Center;
                    mira = mira.LengthSquared() > 0.001f ? Vector2.Normalize(mira) : Vector2.UnitX;
                    Projectile.Center = duenio.Center + mira * 30f;
                    Projectile.rotation = mira.ToRotation();
                    Projectile.timeLeft = 60;      // viva mientras se hojea

                    // ¿SOLTÓ? → LA FRASE DISPARA.
                    if (!duenio.channel || duenio.itemAnimation <= 0)
                    {
                        float carga = MathHelper.Clamp(Projectile.localAI[0] / 150f, 0f, 1f);
                        Projectile.ai[0] = 1f;
                        Projectile.ai[1] = carga;
                        Projectile.velocity = mira * 58f;
                        Projectile.timeLeft = 40;
                        if (Main.myPlayer == Projectile.owner)
                        {
                            Projectile.damage = Math.Max(1,
                                (int)(Projectile.damage * (1f + carga * 2f)));
                            Projectile.netUpdate = true;
                        }
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item12.WithPitchOffset(0.35f), Projectile.Center);
                    }
                }
                else
                {
                    // Las copias remotas esperan a la red (posición y estado
                    // viajan solos en los mensajes del proyectil).
                    Projectile.velocity = Vector2.Zero;
                    Projectile.timeLeft = 60;
                }
            }
            else
            {
                // === EL RAYO: recto, cada enemigo UNA sola vez. Las banderas
                //     se reafirman (friendly/penetrate no viajan en los
                //     mensajes de vanilla — ai[0] sí, y de ahí se derivan). ===
                Projectile.friendly = true;
                Projectile.penetrate = -1;
                Projectile.usesLocalNPCImmunity = true;
                Projectile.localNPCHitCooldown = -1;
                Projectile.tileCollide = false;
            }

            if (Main.netMode != NetmodeID.Server)
                Lighting.AddLight(Projectile.Center, 0.45f, 0.36f, 0.18f);
        }

        public override void OnKill(int timeLeft)
        {
            // EL PUNTO FINAL: toda frase termina en un punto (el rayo muere
            // con un puntito de tinta).
            DignosFXProjectile.Nacer(Projectile, Projectile.Center, 7);
            for (int i = 0; i < 4; i++)
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(4f, 4f), 8, 8,
                    DustID.Enchanted_Gold,
                    Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 0.5f));
                Main.dust[d].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                int semilla = Projectile.identity % 89 + 11;

                VFXCore.CerrarLoteSiAbierto();
                VFXCore.Begin();
                try
                {
                    if (!EsRayo)
                    {
                        // ====================================================
                        //  LA CARGA — hojear: glifitos dorados saltando en la
                        //  boca del cañón + el VISTAZO de la frase creciendo.
                        // ====================================================
                        for (int i = 0; i < 5; i++)
                        {
                            float hx = VFXCore.Hash01(semilla, i, (int)(t * 14f)) - 0.5f;
                            float hy = VFXCore.Hash01(semilla, i + 31, (int)(t * 14f)) - 0.5f;
                            float parpadeo = VFXCore.Hash01(semilla, i, (int)(t * 14f) + 1);
                            if (parpadeo < 0.35f) continue;      // el flicker
                            VFXCore.Quad(Projectile.Center + new Vector2(hx * 26f, hy * 22f),
                                DignosGrimorio.OroGlifo * 0.6f, new Vector2(5f, 5f),
                                hx * MathHelper.Pi, VFXCore.Pixel);
                        }
                        float carga = CargaViva;
                        Vector2 dir = new Vector2(MathF.Cos(Projectile.rotation),
                            MathF.Sin(Projectile.rotation));
                        VFXCore.Line(Projectile.Center,
                            Projectile.Center + dir * (12f + carga * 44f),
                            DignosGrimorio.OroGlifo * (0.25f + 0.4f * carga), 2f);
                        VFXCore.Quad(Projectile.Center,
                            DignosGrimorio.OroGlifo * (0.3f + 0.5f * carga),
                            new Vector2(18f + carga * 16f, 18f + carga * 16f));
                    }
                    else
                    {
                        // ====================================================
                        //  EL RAYO — LA LÍNEA DE GLIFOS: la frase escrita
                        //  alrededor del proyectil (atrás y adelante de la
                        //  velocidad, ±vel·8) con CABEZA violeta y el núcleo
                        //  brillante. La LARGO de la frase creció con la carga.
                        // ====================================================
                        Vector2 vel = Projectile.velocity;
                        Vector2 dir = vel.LengthSquared() > 0.1f
                            ? Vector2.Normalize(vel) : Vector2.UnitX;
                        int glifos = 8 + (int)(MathHelper.Clamp(Projectile.ai[1], 0f, 1f) * 10f);
                        for (int i = 0; i <= glifos; i++)
                        {
                            float f = i / (float)glifos;         // 0 atrás → 1 delante
                            Vector2 pos = Projectile.Center + dir * ((f - 0.62f) * 928f);
                            float tam = 5f + 4f * VFXCore.Hash01(semilla, i, (int)(t * 11f));
                            bool esCabeza = i == glifos;
                            Color c = esCabeza
                                ? new Color(198, 130, 250)
                                : DignosGrimorio.OroGlifo;
                            float alfa = esCabeza ? 0.85f
                                : 0.55f * (1f - MathF.Abs(f - 0.55f) * 0.7f);
                            VFXCore.Quad(pos, c * alfa, new Vector2(tam, tam),
                                VFXCore.Hash01(semilla, i + 7, (int)(t * 11f)) * MathHelper.PiOver2,
                                VFXCore.Pixel);
                        }
                        // EL NÚCLEO: la línea brillante del rayo (de −vel·8 a +vel·8).
                        VFXCore.Line(Projectile.Center - vel * 8f, Projectile.Center + vel * 8f,
                            new Color(255, 240, 200) * 0.45f, 3f);
                        VFXCore.Quad(Projectile.Center, new Color(255, 250, 230) * 0.8f,
                            new Vector2(22f, 22f));
                    }
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
    //  6 · EL MARCAPÁGINAS — el dardo que marca y el libro que cierra
    //
    //  Dardo fino con homing suave; al golpear se CLAVA al enemigo (guarda
    //  SU whoAmI en ai[0] y el offset relativo en ai[1]/ai[2] — floats
    //  directos, el patrón de la casa para pasar Vector2 por los ai) y
    //  desde ahí VIAJA CON ÉL (Center = npc.Center + offset), brillando
    //  480 ticks. EL CONTEO: cinco marcadores en el mismo enemigo y
    //  llega FIN DEL CAPÍTULO — el golpe ×4 y EL LIBRO SE CIERRA de un
    //  portazo encima (la portada oscura cayendo + el aro del golpe),
    //  tragándose TODOS los marcadores de esa página.
    //
    //  ai[0] = el whoAmI del ancla (−1 = volando) · ai[1]/ai[2] = offset.
    // ======================================================================
    public class MarcapaginasProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        /// <summary>EL CONTEO DE MARCADORES por enemigo (whoAmI → (marca-
        /// dores, tick); caduca a los 560 ticks — más que la vida máxima
        /// de un marcador clavado, la ventana de un capítulo).</summary>
        private static readonly Dictionary<int, (int cuenta, uint tick)> ConteoMarcadores =
            new Dictionary<int, (int, uint)>();

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;       // el vuelo no se pierde
            Projectile.light = 0.25f;
        }

        public override void AI()
        {
            int ancla = (int)Projectile.ai[0];
            if (ancla < 0)
            {
                // === EL VUELO (homing suave: accel 0,1 · vmax 14). ===
                NPC presa = DignosGrimorio.PresaCercana(Projectile.Center, 800f);
                if (presa != null)
                {
                    Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                    Projectile.velocity += hacia * 0.1f;
                    float v = Projectile.velocity.Length();
                    if (v > 14f) Projectile.velocity = Projectile.velocity / v * 14f;
                }
                // El vaivén del listón (la cinta de tela ondeando).
                Projectile.rotation = Projectile.velocity.ToRotation() +
                    MathF.Sin(Main.GlobalTimeWrappedHourly * 6f + Projectile.identity) * 0.12f;
            }
            else
            {
                // === CLAVADO: viaja con su enemigo (o se deshace si murió). ===
                NPC presa = ancla < Main.maxNPCs ? Main.npc[ancla] : null;
                if (presa == null || !presa.active)
                {
                    Projectile.Kill();
                    return;
                }
                Projectile.Center = presa.Center + new Vector2(Projectile.ai[1], Projectile.ai[2]);
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = MathHelper.PiOver2;   // el listón colgando
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.ai[0] >= 0f) return;      // ya está clavado (no re-clava)

            // ============================================================
            //  EL CLAMP: el marcador queda CLAVADO al enemigo — whoAmI y
            //  offset relativos en los ai (floats directos: es como la
            //  casa pasa posiciones por los campos de red del proyectil).
            // ============================================================
            Projectile.ai[0] = target.whoAmI;
            Vector2 offset = Projectile.Center - target.Center;
            Projectile.ai[1] = offset.X;
            Projectile.ai[2] = offset.Y;
            Projectile.friendly = false;             // clavado ya no daña
            Projectile.timeLeft = 480;
            Projectile.netUpdate = true;

            // ============================================================
            //  EL CONTEO (ventana de un capítulo: 560 ticks).
            // ============================================================
            uint ahora = Main.GameUpdateCount;
            int clave = target.whoAmI;
            int cuenta = 1;
            if (ConteoMarcadores.TryGetValue(clave, out var entrada) &&
                ahora - entrada.tick <= 560u)
                cuenta = entrada.cuenta + 1;
            ConteoMarcadores[clave] = (cuenta, ahora);

            if (cuenta < 5) return;

            // ============================================================
            //  ¡FIN DEL CAPÍTULO! El libro se cierra de golpe: el conteo
            //  se lee (y se borra — la página se pasó), el golpe ×4 solo
            //  lo resuelve el dueño (GolpeMotor), el teatro del portazo
            //  corre en cada pantalla y TODOS los marcadores de esa
            //  página se los traga el libro.
            // ============================================================
            ConteoMarcadores.Remove(clave);
            DignosFXProjectile.Nacer(Projectile, target.Center + new Vector2(0f, -74f), 6);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, target.Center);
            if (Projectile.owner == Main.myPlayer)
                Content.Systems.GolpeMotor.Golpear(Projectile, target,
                    Math.Max(1, Projectile.damage * 4), 5f, true);
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != Type) continue;
                if (p.whoAmI == Projectile.whoAmI) continue;
                if (p.owner != Projectile.owner) continue;
                if ((int)p.ai[0] != clave) continue;
                p.Kill();
            }
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            // EL DESCUENTE: cada marcador que muere libera su asiento en
            // el conteo (la valla del diccionario — jamás bajo de cero).
            int clave = (int)Projectile.ai[0];
            if (clave >= 0 && ConteoMarcadores.TryGetValue(clave, out var e))
            {
                if (e.cuenta <= 1) ConteoMarcadores.Remove(clave);
                else ConteoMarcadores[clave] = (e.cuenta - 1, e.tick);
            }
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(4f, 4f), 8, 8,
                    DustID.Enchanted_Gold,
                    Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 0.5f));
                Main.dust[d].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                bool clavado = Projectile.ai[0] >= 0f;
                float t = Main.GlobalTimeWrappedHourly;

                VFXCore.CerrarLoteSiAbierto();
                VFXCore.Begin();
                try
                {
                    // EL LISTÓN: cinta fina (18×4) a lo largo del vuelo con
                    // PUNTA violeta y su brillo pequeño.
                    Vector2 dir = new Vector2(MathF.Cos(Projectile.rotation),
                        MathF.Sin(Projectile.rotation));
                    VFXCore.Quad(Projectile.Center, DignosGrimorio.OroBinaria * 0.9f,
                        new Vector2(18f, 4f), Projectile.rotation, VFXCore.Pixel);
                    VFXCore.Quad(Projectile.Center + dir * 9.5f,
                        DignosGrimorio.VioletaBinaria * 0.95f,
                        new Vector2(6f, 6f), Projectile.rotation, VFXCore.Pixel);
                    float pulso = clavado
                        ? 0.6f + 0.4f * MathF.Sin(t * 4.5f + Projectile.identity)
                        : 0.45f;
                    VFXCore.Quad(Projectile.Center, DignosGrimorio.OroBinaria * 0.28f * pulso,
                        new Vector2(20f, 20f));
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.FlushAdditive();
                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.ReabrirLoteVanilla(); }
            return false;
        }
    }
}
