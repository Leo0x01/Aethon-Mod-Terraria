using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// EL RAYO CONVERGENTE (v6.51.2) — LA CEREMONIA DE LOS ATAQUES
    /// COMBINADOS. LA LETRA: «los rayos convergentes no [aparecen] o
    /// tal vez salen demasiado rápido, haz que para los soles rúnicos
    /// el rayo convergente tome 2 segundos en crearlo y lanzarlo al
    /// enemigo y para los agujeros negros rúnicos tome 3 segundos,
    /// esto es una animación que debe realizarse y deben aparecer
    /// encima del jugador».
    ///
    /// LA MÁQUINA: nace ENCIMA DEL JUGADOR (y lo SIGUE — el ritual
    /// camina con su dueño) y en su corona de anillos giratorios
    /// nacen LOS RAYOS DE PERLIN que CONVERGEN al punto focal: 2
    /// SEGUNDOS para el SOL RÚNICO (120 t) y 3 SEGUNDOS para los
    /// AGUJEROS NEGROS (180 t — el supremo también). En el punto
    /// focal la semilla CRECE (el sol con sus anillos dorados · el
    /// agujero como un vórtice violeta que OSCURECE) mientras el
    /// mundo entero le SOPA partículas. Al cumplir el plazo: EL
    /// LANZAMIENTO — el sol sale DISPARADO al enemigo con toda su
    /// fuerza; el agujero se ABRE encima de la presa. El daño viaja
    /// en ai[2] (el combinado de los ojos: ya lleva el grimorio Y las
    /// armaduras del portador).
    ///
    /// ai[0] = modo (1 SOL · 2 AGUJERO · 3 SUPREMO) · ai[1] = el
    /// whoAmI del blanco · ai[2] = el daño combinado.
    /// </summary>
    public class RayoConvergenteProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        private const int MODO_SOL = 1;
        private const int MODO_AGUJERO = 2;
        private const int MODO_SUPREMO = 3;

        /// <summary>2 SEGUNDOS para el sol (la letra).</summary>
        private const int T_SOL = 120;

        /// <summary>3 SEGUNDOS para los agujeros (la letra).</summary>
        private const int T_AGUJERO = 180;

        private int Modo => (int)Projectile.ai[0];
        private int Duracion => Modo == MODO_SOL ? T_SOL : T_AGUJERO;
        private bool EsSol => Modo == MODO_SOL;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;      // el ritual no daña: lo que LANZA daña
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = T_AGUJERO + 30;
            Projectile.netImportant = true;   // la ceremonia es de todos los cables
        }

        public override void AI()
        {
            Projectile.ai[0] = Math.Max(1f, Projectile.ai[0]);
            float t = Projectile.localAI[0];
            Projectile.localAI[0] = t + 1f;
            float edad = Projectile.localAI[0];

            // === EL RITUAL CAMINA CON SU DUEÑO: el punto focal flota
            //     ENCIMA del jugador (la letra) y lo persigue suave. ===
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active || dueno.dead) { Projectile.Kill(); return; }
            Vector2 objetivo = dueno.Center + new Vector2(0f, -150f);
            Projectile.Center = Vector2.Lerp(Projectile.Center, objetivo, 0.18f);
            Projectile.velocity = Vector2.Zero;

            // === EL BLANCO (la presa que los ojos eligieron — si murió,
            //     la más cercana al momento del lanzamiento). ===
            NPC blanco = BlancoVivo();

            // === LA LUZ DEL RITUAL: el sol DORA el aire; el agujero lo
            //     APAGA con su violeta. ===
            float prog = MathHelper.Clamp(edad / Duracion, 0f, 1f);
            if (EsSol)
                Lighting.AddLight(Projectile.Center,
                    0.55f * prog, 0.42f * prog, 0.14f * prog);
            else
                Lighting.AddLight(Projectile.Center,
                    0.22f * prog, 0.10f * prog, 0.40f * prog);

            // === EL MUNDO LE SOPA PARTÍCULAS (la carga convergente). ===
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                float lejos = Main.rand.NextFloat(90f, 260f);
                Vector2 posM = Projectile.Center +
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * lejos;
                Vector2 hacia = Projectile.Center - posM;
                Dust d = Dust.NewDustPerfect(posM,
                    EsSol ? DustID.GoldFlame : DustID.Shadowflame,
                    hacia.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2.5f, 6f),
                    140, EsSol ? new Color(255, 214, 120) : new Color(120, 60, 190), 0.9f);
                d.noGravity = true;
            }

            // === EL TIC-TAC DEL RITUAL (el compás que sube). ===
            if (!Main.dedServ && edad % 20f == 0f && edad < Duracion)
            {
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(-0.25f + 0.4f * prog),
                    Projectile.Center);
            }

            // === v6.51.3 — LA BRUMA DE LA GESTACIÓN (la letra: «agrega un
            //     poco de bruma en las animacions de nacimiento de
            //     agujeros negros runicos y de soles runicos, claro con la
            //     bruma del color correspondiente»): alrededor del punto
            //     focal — donde la semilla crece — flota UN POCO de niebla
            //     del color de lo que nace: ORO para el sol · VIOLETA para
            //     el agujero (jirones lentos que giran y se disuelven — no
            //     la niebla que CONVERGE de siempre: esta es la que
            //     RESPIRA alrededor de la cuna). ===
            if (Main.netMode != NetmodeID.Server && edad % 9f < 1f)
            {
                float angM = Main.rand.NextFloat(MathHelper.TwoPi);
                float lejosM = Main.rand.NextFloat(46f, 120f) * (0.4f + 0.6f * prog);
                Vector2 posM = Projectile.Center +
                    new Vector2(MathF.Cos(angM), MathF.Sin(angM) * 0.8f) * lejosM;
                Dust m = Dust.NewDustPerfect(posM,
                    EsSol ? DustID.GoldFlame : DustID.Shadowflame,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f),
                                Main.rand.NextFloat(-1.0f, -0.2f)),
                    70, EsSol ? new Color(255, 214, 120) : new Color(122, 60, 192),
                    Main.rand.NextFloat(1.5f, 2.4f));
                m.noGravity = true;
                m.fadeIn = 0f;
            }

            // === EL CUMPLIMIENTO: EL LANZAMIENTO. ===
            if (edad >= Duracion)
            {
                Lanzar(blanco, dueno);
                Projectile.Kill();
            }
        }

        /// <summary>El blanco guardado (o el más cercano vivo al
        /// despertar el ritual — la presa murió a media ceremonia).</summary>
        private NPC BlancoVivo()
        {
            int idx = (int)Projectile.ai[1];
            if (idx >= 0 && idx < Main.maxNPCs)
            {
                NPC n = Main.npc[idx];
                if (n != null && n.active && VFXCore.EsObjetivo(n)) return n;
            }
            NPC mejor = null;
            float mejorD = 1400f * 1400f;   // DistanceSQ: el radio al CUADRADO
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !VFXCore.EsObjetivo(n)) continue;
                float d = n.Center.DistanceSQ(Projectile.Center);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        /// <summary>EL LANZAMIENTO — lo que la ceremonia estaba
        /// creando sale al mundo: el SOL con sus anillos disparado al
        /// enemigo · el AGUJERO (uno de los rúnicos al azar, o el
        /// SUPREMO/AURORA) abierto sobre la presa.</summary>
        private void Lanzar(NPC blanco, Player dueno)
        {
            // v6.51.2 — SOLO LA MÁQUINA DUEÑA resuelve el lanzamiento (el
            // patrón de la casa): el dueño engendra y NewProjectile
            // sincroniza — las réplicas solo disfrutan el cine.
            if (Projectile.owner != Main.myPlayer) return;
            int dano = Math.Max(1, (int)Projectile.ai[2]);

            if (blanco == null || !blanco.active)
            {
                // sin presa: el ritual se disipa en la nada
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 18; i++)
                    {
                        Dust d = Dust.NewDustPerfect(Projectile.Center,
                            EsSol ? DustID.GoldFlame : DustID.Shadowflame,
                            Main.rand.NextVector2Circular(5f, 5f), 160,
                            EsSol ? new Color(255, 214, 120) : new Color(120, 60, 190), 1.1f);
                        d.noGravity = true;
                    }
                }
                return;
            }

            if (EsSol)
            {
                // === EL SOL RÚNICO: sus anillos por despertar, LANZADO
                //     CON FUERZA al enemigo (la velocidad fuerte de
                //     siempre + el homing del sol hace el resto). ===
                int despertar = Content.Systems.DespertarGrimorio.NivelDe(dueno);
                int anillos = Content.Systems.DespertarGrimorio.AnillosDelSol(despertar);
                Vector2 rumbo = blanco.Center - Projectile.Center;
                if (rumbo.LengthSquared() < 1f) rumbo = -Vector2.UnitY;
                rumbo.Normalize();
                Projectile sol = Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromAI(),
                    Projectile.Center, rumbo * 7.5f,
                    ModContent.ProjectileType<RuneSunProjectile>(),
                    dano, 4f, Projectile.owner, anillos,
                    (Projectile.owner * 31 + Main.rand.Next(9973)) * 1f);
                sol.timeLeft = 480;
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(-0.2f), Projectile.Center);

                // v6.51.3 — LA BRUMA DORADA DEL NACIMIENTO DEL SOL (la
                //     letra: bruma del color correspondiente en las
                //     animaciones de nacimiento): el sol estrena su vida
                //     EXHALANDO un anillo de niebla dorada.
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int b = 0; b < 14; b++)
                    {
                        float angB = b / 14f * MathHelper.TwoPi;
                        Dust m = Dust.NewDustPerfect(Projectile.Center +
                            new Vector2(MathF.Cos(angB), MathF.Sin(angB)) * 18f,
                            DustID.GoldFlame,
                            new Vector2(MathF.Cos(angB), MathF.Sin(angB)) *
                            Main.rand.NextFloat(1.2f, 2.8f),
                            100, new Color(255, 214, 120), Main.rand.NextFloat(1.6f, 2.6f));
                        m.noGravity = true;
                        m.fadeIn = 0f;
                    }
                }
            }
            else
            {
                // === v6.51.3 — EL NACIMIENTO CONTINUO (la letra: «los
                //     agujeros negros runicos no nacen correctamente donde
                //     tienen que nacer, aunque la animacion se hace este
                //     nace directamente en el enemigo»): el agujero YA NO
                //     se materializa de golpe sobre la presa — su SEMILLA
                //     nace AQUÍ, en el punto focal del ritual (donde la
                //     ceremonia la gestó), y VIAJA hasta su cita
                //     (AgujeroEnViajeProjectile): una semilla de vacío que
                //     arrastra su bruma violeta y al llegar SE ABRE con su
                //     niebla morada. Los 10 agujeros rúnicos quedan
                //     INTACTOS (el viaje es solo el cordón umbilical). ===
                int tipo;
                if (Modo == MODO_SUPREMO)
                {
                    tipo = Main.rand.NextBool()
                        ? ModContent.ProjectileType<SupremoBlackHoleProjectile>()
                        : ModContent.ProjectileType<SupremoAuroraBlackHoleProjectile>();
                }
                else
                {
                    bool ascendidos = Content.Systems.DespertarGrimorio
                        .AgujerosAscendidos(Content.Systems.DespertarGrimorio.NivelDe(dueno));
                    tipo = (Main.rand.Next(4)) switch
                    {
                        0 => ascendidos ? ModContent.ProjectileType<OlvidoAscendidoBlackHoleProjectile>()
                                        : ModContent.ProjectileType<OlvidoBlackHoleProjectile>(),
                        1 => ascendidos ? ModContent.ProjectileType<CosmicAscendidoBlackHoleProjectile>()
                                        : ModContent.ProjectileType<CosmicBlackHoleProjectile>(),
                        2 => ascendidos ? ModContent.ProjectileType<UmbralAscendidoBlackHoleProjectile>()
                                        : ModContent.ProjectileType<UmbralBlackHoleProjectile>(),
                        _ => ascendidos ? ModContent.ProjectileType<BrumaAscendidoBlackHoleProjectile>()
                                        : ModContent.ProjectileType<BrumaBlackHoleProjectile>(),
                    };
                }

                // LA SEMILLA NACE EN EL PUNTO FOCAL (donde la animación la
                // gestó) y vuela a su cita con la presa.
                Vector2 rumboA = blanco.Center - Projectile.Center;
                if (rumboA.LengthSquared() < 1f) rumboA = -Vector2.UnitY;
                rumboA.Normalize();
                Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromAI(),
                    Projectile.Center, rumboA * 13f,
                    ModContent.ProjectileType<AgujeroEnViajeProjectile>(),
                    0, 0f, Projectile.owner,
                    tipo * 1f,              // el agujero que abrirá al llegar
                    blanco.whoAmI * 1f,     // su cita
                    dano * 1f);             // el daño combinado del ritual
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(Modo == MODO_SUPREMO ? -0.5f : 0.35f),
                    Projectile.Center);
                if (Modo == MODO_SUPREMO)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, Projectile.Center);
            }

            // EL ESTALLIDO DEL LANZAMIENTO (la puerta que se abre).
            if (!Main.dedServ)
            {
                for (int i = 0; i < 26; i++)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center,
                        EsSol ? DustID.GoldFlame : DustID.PurpleTorch,
                        Main.rand.NextVector2Circular(7f, 7f), 170,
                        EsSol ? new Color(255, 214, 120) : new Color(190, 130, 255), 1.3f);
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                float edad = Projectile.localAI[0];
                float prog = MathHelper.Clamp(edad / Duracion, 0f, 1f);
                // LA CURVA DEL RITUAL: la convergencia ACELERA al final
                // (el mundo se cierra de golpe — la firma de la casa).
                float conver = prog * prog * (3f - 2f * prog);

                float time = Main.GlobalTimeWrappedHourly;
                Vector2 centro = Projectile.Center;
                int semilla = Projectile.identity % 89 + 7;

                VFXCore.CerrarLoteSiAbierto();
                VFXCore.Begin();
                try
                {
                    // === LOS RAYOS CONVERGENTES (la letra): nacen en una
                    //     corona de anillos giratorios y CONVERGEN al punto
                    //     focal — arrancan ANCHOS y lejanos, terminan
                    //     FINOS y pegados: la vista es un EMBUDO de luz. ===
                    int rayos = EsSol ? 5 : 7;
                    float lejania = MathHelper.Lerp(1.0f, 0.22f, conver);
                    float grosor = MathHelper.Lerp(9f, 3.5f, conver);
                    for (int i = 0; i < rayos; i++)
                    {
                        float ang = i / (float)rayos * MathHelper.TwoPi
                            + time * 0.55f * ((i % 2 == 0) ? 1f : -0.7f)
                            + semilla * 0.31f;
                        float radio = (150f + 60f * MathF.Sin(time * 0.9f + i * 1.7f)) * lejania;
                        Vector2 borde = centro + new Vector2(
                            MathF.Cos(ang) * radio, MathF.Sin(ang) * radio * 0.8f);
                        // EL RAYO DE PERLIN: vivo (el temblor de siempre) y
                        // CARGÁNDOSE (más opaco cuanto más cerca del final).
                        float vivo = 0.55f + 0.45f * MathF.Sin(time * 14f + i * 2.3f);
                        Color cRayo = EsSol
                            ? new Color(255, 214, 120)
                            : new Color(168, 96, 240);
                        VFXCore.Line(borde, centro, cRayo * (0.16f + 0.50f * prog) * vivo,
                            grosor * (0.65f + 0.35f * vivo));
                        VFXCore.Line(borde, centro,
                            (EsSol ? new Color(255, 246, 200) : new Color(226, 190, 255))
                            * (0.10f + 0.35f * prog),
                            grosor * 0.35f);
                    }

                    // === EL PUNTO FOCAL: la semilla que CRECE. ===
                    float nucleo = MathHelper.Lerp(14f, EsSol ? 46f : 40f, conver);

                    if (EsSol)
                    {
                        // EL SOL EN GESTACIÓN: el corazón dorado + sus
                        // ANILLOS rúnicos que giran (los que llevará).
                        VFXCore.Quad(centro, new Color(255, 236, 170) * (0.35f + 0.5f * prog),
                            new Vector2(nucleo * 2.4f, nucleo * 2.4f));
                        VFXCore.Quad(centro, new Color(255, 170, 60) * (0.3f + 0.4f * prog),
                            new Vector2(nucleo * 3.4f, nucleo * 3.4f));
                        for (int a = 0; a < 3; a++)
                        {
                            float rA = nucleo * (1.6f + a * 0.55f);
                            float giro = time * (1.1f - a * 0.25f) * (a % 2 == 0 ? 1f : -1f);
                            int runas = 6 + a * 2;
                            for (int k = 0; k < runas; k++)
                            {
                                float angR = giro + k / (float)runas * MathHelper.TwoPi;
                                Vector2 posR = centro + new Vector2(
                                    MathF.Cos(angR) * rA, MathF.Sin(angR) * rA * 0.82f);
                                VFXCore.Quad(posR, new Color(255, 208, 110) * (0.4f + 0.4f * prog),
                                    new Vector2(nucleo * 0.16f, nucleo * 0.30f), angR + MathHelper.PiOver2);
                            }
                        }
                    }
                    else
                    {
                        // EL AGUJERO EN GESTACIÓN: el vórtice violeta que
                        // TRAGA — aro exterior + vacío interior (la puerta
                        // que se abre a lo desconocido).
                        VFXCore.Quad(centro, new Color(96, 40, 160) * (0.30f + 0.45f * prog),
                            new Vector2(nucleo * 4.2f, nucleo * 4.2f));
                        VFXCore.Quad(centro, new Color(30, 10, 52) * (0.35f + 0.55f * prog),
                            new Vector2(nucleo * 2.6f, nucleo * 2.6f));
                        for (int a = 0; a < 2; a++)
                        {
                            float rA = nucleo * (1.8f + a * 0.9f);
                            float giro = -time * (0.9f + a * 0.4f);
                            int runas = 8;
                            for (int k = 0; k < runas; k++)
                            {
                                float angR = giro + k / (float)runas * MathHelper.TwoPi;
                                Vector2 posR = centro + new Vector2(
                                    MathF.Cos(angR) * rA, MathF.Sin(angR) * rA * 0.85f);
                                VFXCore.Quad(posR, new Color(190, 120, 255) * (0.35f + 0.4f * prog),
                                    new Vector2(nucleo * 0.14f, nucleo * 0.26f), angR + MathHelper.PiOver2);
                            }
                        }
                    }
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.FlushAdditive();

                // === EL VACÍO QUE OSCURECE (los agujeros comen la luz
                //     alrededor del ritual — pase ALFA propio). ===
                if (!EsSol)
                {
                    try
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                            Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                            null, Main.GameViewMatrix.TransformationMatrix);
                        Texture2D glow = ModContent.Request<Texture2D>(
                            "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                        Vector2 posScr = centro - Main.screenPosition;
                        float tama = MathHelper.Lerp(40f, 150f, conver);
                        Main.spriteBatch.Draw(glow, posScr, null,
                            new Color(18, 8, 30) * (0.55f * conver), 0f,
                            glow.Size() * 0.5f,
                            new Vector2(tama, tama) / glow.Size(),
                            SpriteEffects.None, 0f);
                    }
                    catch { }
                    finally { Main.spriteBatch.End(); }
                }

                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.ReabrirLoteVanilla(); }
            return false;
        }
    }
}
